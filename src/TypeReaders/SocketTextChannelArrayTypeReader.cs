using System;
using System.Text;
using System.Linq;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace MopBotTwo.TypeReaders
{
	public class SocketTextChannelArrayTypeReader : CustomTypeReader
	{
		public override Type[] Types => new[] { typeof(IChannel[]),typeof(ITextChannel[]),typeof(SocketTextChannel[]) };
		
		public static Regex channelRegex = new Regex(@"(?:<#(\d+)>|(\d+)|#([\w-]+))\s*",RegexOptions.Compiled);
		
		public override async Task<TypeReaderResult> ReadAsync(ICommandContext context,string input,IServiceProvider services)
		{
			var matches = channelRegex.Matches(input);

			if(matches.Count==0) {
				return TypeReaderResult.FromError(CommandError.ParseFailed,"Parse failure. Expected a list of channels.");
			}

			var channels = new List<SocketTextChannel>();

			foreach(Match match in matches) {
				var groups = match.Groups;
				string idStr = groups[1].Value ?? groups[2].Value;
				string anyStr = idStr ?? groups[3].Value;
				 
				SocketTextChannel channel = null;

				if(idStr==null) {
					channel = (await context.Guild.GetTextChannelsAsync()).FirstOrDefault(c => c.Name==anyStr) as SocketTextChannel;
				}else if(ulong.TryParse(idStr,out ulong id)) {
					channel = await context.Guild.GetTextChannelAsync(id) as SocketTextChannel;
				}

				if(channel==null) {
					return TypeReaderResult.FromError(CommandError.ParseFailed,$"Invalid channel: `{anyStr}`.");
				}

				channels.Add(channel);
			}

			return TypeReaderResult.FromSuccess(channels.ToArray());
		}
	}
}
