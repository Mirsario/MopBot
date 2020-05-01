using System.Collections.Generic;
using MopBot.Core.Systems.Memory;

namespace MopBot.Common.Systems.ChannelLinking
{
	public class ChannelLinkingServerData : ServerData
	{
		public Dictionary<ulong,ulong> channelLinks = new Dictionary<ulong,ulong>(); // [ Channel ID -> Link ID ] Dictionary
	}
}