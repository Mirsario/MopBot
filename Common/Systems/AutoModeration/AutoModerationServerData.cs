using System.Collections.Concurrent;
using System.Collections.Generic;
using MopBot.Core.Systems.Memory;

namespace MopBot.Common.Systems.AutoModeration
{
	public class AutoModerationServerData : ServerData
	{
		public ModerationPunishment mentionSpamPunishment;
		public byte mentionCooldown = 30;
		public uint minMentionsForAction = 10;
		public string announcementPrefix;
		public ConcurrentDictionary<ulong,List<byte>> userPingCounters = new ConcurrentDictionary<ulong,List<byte>>();
	}
}
