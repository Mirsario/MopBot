using Discord.WebSocket;
using MopBot.Extensions;
using System;

namespace MopBot.Core.DataStructures
{
	[Serializable]
	public struct ServerChannelIds
	{
		public ulong serverId;
		public ulong channelId;

		public ServerChannelIds(MessageContext context) : this(context.server.Id,context.Channel.Id) {}
		public ServerChannelIds(ulong serverId,ulong channelId)
		{
			this.serverId = serverId;
			this.channelId = channelId;
		}

		public bool TryGetServer(out SocketGuild server) => MopBot.client.TryGetServer(serverId,out server);
		public bool TryGetChannel(out SocketGuildChannel channel)
		{
			if(!MopBot.client.TryGetServer(serverId,out var server)) {
				channel = null;
				return false;
			}

			return server.TryGetChannel(channelId,out channel);
		}
		public bool TryGetTextChannel(out SocketTextChannel textChannel)
		{
			if(!MopBot.client.TryGetServer(serverId,out var server)) {
				textChannel = null;
				return false;
			}

			return server.TryGetTextChannel(channelId,out textChannel);
		}
	}
}
