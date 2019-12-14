using System.Collections.Generic;
using Discord.WebSocket;
using MopBotTwo.Core.Systems.Memory;

namespace MopBotTwo.Common.Systems.ChannelLinking
{
	public class ChannelLinkingGlobalData : GlobalData
	{
		public Dictionary<ulong,ChannelLink> links = new Dictionary<ulong,ChannelLink>();
	}
}