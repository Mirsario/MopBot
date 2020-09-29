using System.Collections.Generic;
using MopBot.Core.Systems.Memory;

namespace MopBot.Common.Systems.ChannelLinking
{
	public class ChannelLinkingGlobalData : GlobalData
	{
		public Dictionary<ulong, ChannelLink> links = new Dictionary<ulong, ChannelLink>();
	}
}