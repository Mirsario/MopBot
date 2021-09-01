using Discord.WebSocket;
using MopBot.Core.Systems.Memory;
using MopBot.Extensions;

namespace MopBot.Common.Systems.Logging
{
	class LoggingServerData : ServerData
	{
		public ulong loggingChannel;

		public override void Initialize(SocketGuild server) { }

		public bool TryGetLoggingChannel(SocketGuild server, out SocketTextChannel result)
			=> server.TryGetTextChannel(loggingChannel, out result);
	}
}
