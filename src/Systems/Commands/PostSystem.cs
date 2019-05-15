using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Discord.Commands;
using Discord.WebSocket;

#pragma warning disable CS1998 //The async method lacks 'await' operator.

namespace MopBotTwo.Systems
{
	[Group("post")]
	[RequirePermission("postsystem")]
	[SystemConfiguration(EnabledByDefault = true,Description = "Commands to simplify large multi-message posts. [[Split]] in input marks a message split.")]
	public class PostSystem : BotSystem
	{
		public enum SourceType
		{
			Text,
			Link
		}
		public enum PostPieceType
		{
			Text,
			Image
		}
		public abstract class PostPiece
		{
			public abstract Task Execute(SocketTextChannel channel);
		}
		public class TextPostPiece : PostPiece
		{
			public string text;

			public TextPostPiece(string text)
			{
				this.text = text;
			}

			public override Task Execute(SocketTextChannel channel) => channel.SendMessageAsync(text);
		}
		public class FilePostPiece : TextPostPiece
		{
			public string filePath;
			public bool deleteAfterPosting;

			public FilePostPiece(string filePath,bool deleteAfterPosting = false) : this(filePath,null,deleteAfterPosting) {}
			public FilePostPiece(string filePath,string text,bool deleteAfterPosting = false) : base(text)
			{
				this.filePath = filePath;
				this.deleteAfterPosting = deleteAfterPosting;
			}

			public override Task Execute(SocketTextChannel channel) => channel.SendFileAsync(filePath,text);
		}

		private static Regex regexTags;
		private static Regex regexEmotes;
		
		public override async Task Initialize()
		{
			regexTags = new Regex(@"\[\[(\w+)=*([\w\d.:/]*)\]\]");
			regexEmotes = new Regex(@":(\w+):");
		}

		public static async Task<PostPiece[]> ParseToPost(SourceType sourceType,string source)
		{
			string text;
			switch(sourceType) {
				case SourceType.Link:
					text = await BotUtils.DownloadString(source);
					break;
				default:
					text = source;
					break;
			}

			//Parse emotes
			text = regexEmotes.Replace(text,match => BotUtils.TryGetEmote(match.Groups[1].Value,out string emoteText) ? emoteText : match.Value);

			var postPieces = new List<PostPiece> {
				new TextPostPiece(text)
			};

			int tagsParsed = 0;
			
			for(int i = 0;i<postPieces.Count;i++) {
				var piece = postPieces[i];
				if(piece.GetType()!=typeof(TextPostPiece) || !(piece is TextPostPiece textPiece)) {
					continue;
				}

				var match = regexTags.Match(textPiece.text);
				if(!match.Success) {
					continue;
				}

				if(textPiece.text==null) {
					throw new BotError($"textPiece.text is null, i is {i}");
				}

				int matchIndex = match.Index;
				string type = match.Groups[1].Value.ToLower();
				textPiece.text = textPiece.text.Remove(matchIndex,match.Value.Length);

				string GetGroup(int index)
				{
					var group = match.Groups[index];
					if(!group.Success) {
						throw new BotError($"Failed to parse tag #{1+tagsParsed} ({type}): Missing group #{index}.");
					}
					return group.Value;
				}

				bool split = false;
				PostPiece newPiece = null;

				switch(type) {
					case "split":
						split = true;
						break;
					case "image":
						string url = GetGroup(2);
						string filename = BotUtils.GetValidFileName($"TempFile_{url}");
						await BotUtils.DownloadFile(GetGroup(2),filename);
						newPiece = new FilePostPiece(filename,true);
						split = true;
						break;
					default:
						throw new BotError($"Invalid tag type: '{type}'.");
				}
				tagsParsed++;

				if(split) {
					string leftText = matchIndex>=textPiece.text.Length ? null : textPiece.text.Remove(matchIndex);
					string rightText = matchIndex>=textPiece.text.Length ? null : textPiece.text.Substring(matchIndex);

					if(rightText==null) {
						if(newPiece!=null) {
							postPieces[i] = newPiece;
						}
						continue;
					}

					if(string.IsNullOrWhiteSpace(leftText)) {
						if(newPiece!=null) {
							postPieces.Insert(i,newPiece);
						}
						textPiece.text = rightText;
					}else{
						textPiece.text = leftText;
						if(newPiece!=null) {
							postPieces.Add(newPiece);
						}
						postPieces.Add(new TextPostPiece(rightText));
					}
				}
			}

			return postPieces.ToArray();
		}

		[Command("new")] [Alias("create")]
		[RequirePermission("postsystem.manage")]
		public async Task NewPost(SocketTextChannel inChannel,SourceType sourceType,[Remainder]string source)
		{
			var pieces = await ParseToPost(sourceType,source);

			foreach(var piece in pieces) {
				await piece.Execute(inChannel);
			}
		}
	}
}
