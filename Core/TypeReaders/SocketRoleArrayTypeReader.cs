using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace MopBotTwo.Core.TypeReaders
{
	public class SocketRoleArrayTypeReader : DiscordEntityArrayTypeReader<SocketRole>
	{
		public override Type[] Types => new[] { typeof(IRole[]),typeof(SocketRole[]) };

		protected Regex parseRegex;
		public override Regex ParseRegex => parseRegex ?? (parseRegex = new Regex($@"(?:(<\#\d+>|\d+)|#([\w-]+))\s*",RegexOptions.Compiled));

		public override async Task<SocketRole> GetFromId(ICommandContext context,ulong id)
			=> (SocketRole)context.Guild.GetRole(id);
		public override async Task<SocketRole> GetFromName(ICommandContext context,string name)
			=> (SocketRole)context.Guild.Roles.FirstOrDefault(r => MopBot.StrComparerIgnoreCase.Equals(r.Name,name));
	}
}
