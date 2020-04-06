using Discord.Commands;
using Discord.WebSocket;
using MopBotTwo.Core.Systems.Permissions;
using MopBotTwo.Extensions;
using System.Threading.Tasks;

namespace MopBotTwo.Common.Systems.Logging
{
	[RequirePermission(SpecialPermission.Owner,"logging")]
	public partial class LoggingSystem
	{
		[Command("setchannel")]
		[RequirePermission(SpecialPermission.Owner,"logging.manage")]
		public async Task SetChannel(SocketGuildChannel channel)
		{
			Context.server.GetMemory().GetData<LoggingSystem,LoggingServerData>().loggingChannel = channel.Id;
		}
	}
}
