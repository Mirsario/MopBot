using System;
using System.Collections.Generic;


namespace MopBot.Common.Systems.Showcase
{
	[Serializable]
	public class SpotlightChannel : ChannelInfo
	{
		public List<ulong> spotlightedMessages;
	}
}