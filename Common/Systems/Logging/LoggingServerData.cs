using Discord.WebSocket;
using MopBotTwo.Core.Systems.Memory;
using MopBotTwo.Extensions;

namespace MopBotTwo.Common.Systems.Logging
{
	class LoggingServerData : ServerData
	{
		public ulong loggingChannel;

		public override void Initialize(SocketGuild server) { }

		public bool TryGetLoggingChannel(SocketGuild server,out SocketTextChannel result) => server.TryGetTextChannel(loggingChannel,out result);
	}
}
