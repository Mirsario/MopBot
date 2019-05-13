using System;
using System.IO;
using System.Net;
using System.Linq;
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
				case "⭐":
					return @"https://discordapp.com/assets/e4d52f4d69d7bba67e5fd70ffe26b70d.svg";
				case "?":
					return @"https://discordapp.com/assets/cef2d5ab02888e885953f945f9c39304.svg";
			}
			return "";
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