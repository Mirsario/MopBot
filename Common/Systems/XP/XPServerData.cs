using System;
using System.Collections.Generic;
using Discord.WebSocket;
using MopBotTwo.Core.Systems.Memory;

namespace MopBotTwo.Common.Systems.XP
{
	[Serializable]
	public class XPServerData : ServerData
	{
		public bool? onlyAnnounceRoleRewards;
		public ulong? xpReceiveDelay;
		public Dictionary<uint,ulong[]> levelRewards;

		public override void Initialize(SocketGuild server)
		{
			xpReceiveDelay = (ulong?)300;
			levelRewards = new Dictionary<uint,ulong[]>();
			onlyAnnounceRoleRewards = true;
		}
	}
}