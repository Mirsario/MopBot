using System;
using System.IO;
using System.Net;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using MopBot.Extensions;
using Discord.WebSocket;
using Discord.Rest;
using System.Collections.Generic;

namespace MopBot
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

		public static async Task<string> GetInviteUrl(SocketGuild server)
		{
			//TODO: Require.. what permission?

			IEnumerable<RestInviteMetadata> invites = await server.GetInvitesAsync();

			invites = invites.Where(invite => {
				int maxUses = invite.MaxUses ?? 0;

				return !invite.IsRevoked && invite.Uses.HasValue && (maxUses==0 || maxUses<invite.Uses);
			});

			return invites.OrderByDescending(invite => ((invite.IsTemporary || (invite.MaxUses ?? 0)>0) ? int.MinValue : 0)+invite.Uses).FirstOrDefault()?.Url;
		}

		public static ulong GenerateUniqueId(Func<ulong,bool> idExistsCheck)
		{
			//This looks pretty eh, but the chances that this'll loop more than twice are pretty much zero.

			ulong result;

			do {
				result = MopBot.Random.NextULong();
			}
			while(idExistsCheck(result));

			return result;
		}
	}
}