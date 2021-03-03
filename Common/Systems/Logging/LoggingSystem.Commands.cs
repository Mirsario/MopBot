using Discord.Commands;
using Discord.WebSocket;
using MopBot.Core.Systems.Permissions;
using MopBot.Extensions;
using System.Threading.Tasks;

#pragma warning disable CS1998 //Async method lacks 'await' operators and will run synchronously

namespace MopBot.Common.Systems.Logging
{
	[RequirePermission(SpecialPermission.Admin, "logging")]
	public partial class LoggingSystem
	{
		[Command("setchannel")]
		[RequirePermission(SpecialPermission.Admin, "logging.manage")]
		public async Task SetChannel(SocketGuildChannel channel)
		{
			Context.server.GetMemory().GetData<LoggingSystem, LoggingServerData>().loggingChannel = channel.Id;
		}
	}
}
