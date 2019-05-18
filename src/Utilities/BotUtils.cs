using System;
using System.IO;
using System.Net;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using MopBotTwo.Extensions;

namespace MopBotTwo
{
	public static class BotUtils
	{
		public static string GetMessageUrl(IGuild server,IChannel channel,IMessage message)
			=> $@"https://discordapp.com/channels/{server.Id}/{channel.Id}/{message.Id}";
		
		public static string GetValidFileName(string name)
		{
			foreach(char c in Path.GetInvalidFileNameChars()) {
			   name = name.Replace(c,'_');
			}
			return name;
		}

		public static bool TryGetEmote(string name,out string emoteText)
		{
			foreach(var server in MopBot.client.Guilds) {
				if(server.Emotes.TryGetFirst(e => MopBot.StrComparerIgnoreCase.Equals(name,e.Name),out var emote)) {
					emoteText = $"<{(emote.Animated ? "a" : null)}:{emote.Name}:{emote.Id}>";
					Console.WriteLine($"Emote found '{name}', emoteText is '{emoteText}'.");
					return true;
				}
			}
			Console.WriteLine($"Emote '{name}' not found.");
			
			emoteText = null;
			return false;
		}
		//TODO:
		public static string GetEmojiImageUrl(string name)
		{
			switch(name) {
				case "⭐": return @"https://i.imgur.com/Wh8s8Gp.png";
				case "?": return @"https://i.imgur.com/NDZdstw.png";
				default: return "";
			}
		}
		public static string NumberToEmotes(int number)
		{
			var sb = new StringBuilder();
			foreach(char c in number.ToString()) {
				switch(c) {
					case '0': sb.Append(":zero:");	break;
					case '1': sb.Append(":one:");	break;
					case '2': sb.Append(":two:");	break;
					case '3': sb.Append(":three:");	break;
					case '4': sb.Append(":four:");	break;
					case '5': sb.Append(":five:");	break;
					case '6': sb.Append(":six:");	break;
					case '7': sb.Append(":seven:");	break;
					case '8': sb.Append(":eight:");	break;
					case '9': sb.Append(":nine:");	break;
				}
			}
			return sb.ToString();
		}
		
		public static async Task<string> DownloadString(string url)
		{
			if(!Uri.TryCreate(url,UriKind.Absolute,out var realUrl)) {
				throw new BotError("Invalid url.");
			}

			using(var client = new WebClient()) {
				try {
					return await client.DownloadStringTaskAsync(realUrl);
				}
				catch(Exception e) {
					throw new BotError(e);
				}
			}
		}
		public static async Task DownloadFile(string url,string localPath,string[] validExtensions = null)
		{
			if(!Uri.TryCreate(url,UriKind.Absolute,out var realUrl)) {
				throw new BotError("Invalid url.");
			}

			if(validExtensions!=null && validExtensions.Any(ext => string.IsNullOrWhiteSpace(new FileInfo(realUrl.AbsolutePath).Extension))) {
				throw new BotError($"Url has no extension, or it's forbidden. The following extensions are allowed: {string.Join(",",validExtensions.Select(ext => ext.ToString()))}.");
			}

			using(var client = new WebClient()) {
				try {
					await client.DownloadFileTaskAsync(realUrl,localPath);
				}
				catch(Exception e) {
					throw new BotError(e);
				}
			}
		}
	}
}