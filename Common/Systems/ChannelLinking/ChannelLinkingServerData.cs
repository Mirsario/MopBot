using System.Collections.Generic;
using MopBotTwo.Core.Systems.Memory;

namespace MopBotTwo.Common.Systems.ChannelLinking
{
	public class ChannelLinkingServerData : ServerData
	{
		public Dictionary<ulong,ulong> channelLinks = new Dictionary<ulong,ulong>(); // [ Channel ID -> Link ID ] Dictionary
	}
}