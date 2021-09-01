using System;
using System.Linq;
using System.Collections.Generic;
using Discord.WebSocket;
using MopBot.Extensions;
using MopBot.Core.Systems.Memory;

namespace MopBot.Core.Systems.Channels
{
	public class ChannelServerData : ServerData
	{
		public Dictionary<ChannelRole, ulong> channelByRole;

		public override void Initialize(SocketGuild server)
		{
			CheckChannels(server);
		}

		public void CheckChannels(SocketGuild server)
		{
			if (channelByRole != null) {
				return;
			}

			channelByRole = new Dictionary<ChannelRole, ulong>();

			const string General = "general";

			var channels = server.Channels;

			if (!channels.TryGetFirst(c => c.Name == General, out SocketGuildChannel channel) && !channels.TryGetFirst(c => c.Name.Contains(General), out channel)) {
				int maxUsers = 0;

				foreach (var tempChannel in channels) {
					maxUsers = Math.Max(maxUsers, tempChannel.Users.Count);
				}

				channel = channels.Where(c => c.Users.Count == maxUsers).OrderBy(c => c.Position).FirstOrDefault();
			}

			channelByRole[ChannelRole.Default] = channel.Id;
			channelByRole[ChannelRole.Welcome] = channel.Id;
		}
		
		public SocketTextChannel GetChannelByRole(ChannelRole role) => channelByRole.TryGetValue(role, out ulong id) ? MopBot.client.GetChannel(id) as SocketTextChannel : null;
		
		public bool TryGetChannelByRole(ChannelRole role, out SocketTextChannel channel) => (channel = GetChannelByRole(role)) != null;
		
		public bool TryGetChannelByRoles(out SocketTextChannel channel, params ChannelRole[] roles)
		{
			for (int i = 0; i < roles.Length; i++) {
				if (TryGetChannelByRole(roles[i], out channel)) {
					return true;
				}
			}

			channel = null;

			return false;
		}
	}
}
