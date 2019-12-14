using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using System.Text;
using System.Text.RegularExpressions;
using MopBotTwo.Extensions;
using MopBotTwo.Core.Systems;

namespace MopBotTwo.Common.Systems.MessageManagement
{
	[Group("msg")] [Alias("message")]
	[Summary("Group for commands for quoting, copying and moving existing messages, as well as making the bot post new ones.")]
	[SystemConfiguration(Description = "Contains commands for sending messages, as well as moving and copying existing messages. Useful!")]
	public partial class MessageManagementSystem : BotSystem
	{
		public static async Task QuoteMessages(ITextChannel textChannel,IEnumerable<IMessage> messages)
		{
			IMessage prevMessage = null;
			var messageGroups = new List<List<IMessage>>();
			int listIndex = -1;

			foreach(var message in messages) {
				if(listIndex<0 || prevMessage.Author.Id!=message.Author.Id || message.Attachments.Count>0) {
					messageGroups.Add(new List<IMessage> { message });

					listIndex++;
				}else{
					messageGroups[listIndex].Add(message);
				}

				prevMessage = message;
			}

			var text = new StringBuilder();
			for(int i = messageGroups.Count-1;i>=0;i--) {
				var group = messageGroups[i];
				//bool firstMessage = true;
				bool hasImage = false;
				bool forceSend = false;

				for(int j = group.Count-1;j>=0;j--) {
					var message = group[j];
					var author = message.Author;
					var builder = new EmbedBuilder()
						.WithColor(author.GetColor())
						.WithAuthor(author.Name(),author.GetAvatarUrl())
						.WithFooter("Sent at ")
						.WithTimestamp(message.Timestamp);
					
					string content = message.Content;

					if(!hasImage) {
						foreach(var attachment in message.Attachments) {
							string url = attachment.Url;
							if(url.EndsWithAny(".png",".jpg",".jpeg",".bmp",".gif")) {
								builder.WithImageUrl(url);
								hasImage = true;
							}
						}
					}

					if(!hasImage) {
						var match = Regex.Match(content,@"(?:http|https|ftp)\:\/\/[^\s]+\.(?:png|jpg|jpeg|bmp|gif|gifv)");
						if(match!=null && match.Success) {
							string url = match.Value;
							builder.WithImageUrl(url);
							content = content.Replace(url,"");
							hasImage = true;
							forceSend = true;
						}
					}

					foreach(var embed in message.Embeds) {
						switch(embed.Type) {
							case EmbedType.Video:
							case EmbedType.Tweet:
							case EmbedType.Article:
							case EmbedType.Link:
								content = content.Replace(embed.Url,"");
								builder.WithTitle(embed.Title);
								builder.WithUrl(embed.Url);
								builder.WithImageUrl(embed.Image?.Url);
								builder.WithThumbnailUrl(embed.Thumbnail?.Url);
								forceSend = true;
								break;
						}
					}

					text.AppendLine(content);
					
					if(j==0 || forceSend) {
						builder.WithDescription(text.ToString());
						text.Clear();
						await textChannel.SendMessageAsync(embed:builder.Build());
						forceSend = false;
					}
				}
			}
		}
		
		internal async Task<List<IMessage>> CopyMessagesInternal(int numMessages,ITextChannel toChannel,ulong bottomMessageId = 0)
		{
			const int MaxMessages = 200;

			if(numMessages>MaxMessages) {
				throw new BotError($"Won't copy more than {MaxMessages} messages.");
			}

			var fromChannel = Context.Channel;

			var messageList = new List<IMessage>();
			if(bottomMessageId!=0) {
				numMessages -= 1;
				messageList.Add(await fromChannel.GetMessageAsync(bottomMessageId));
			}

			if(numMessages>0) {
				await fromChannel.GetMessagesAsync(bottomMessageId==0 ? Context.message.Id : bottomMessageId,Direction.Before,numMessages).ForEachAsync(collection => messageList.AddRange(collection));
			}

			await QuoteMessages(toChannel,messageList);

			return messageList;
		}
	}
}