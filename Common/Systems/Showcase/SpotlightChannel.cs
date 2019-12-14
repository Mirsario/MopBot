using System;
using System.Collections.Generic;


namespace MopBotTwo.Common.Systems.Showcase
{
	[Serializable]
	public class SpotlightChannel : ChannelInfo
	{
		public List<ulong> spotlightedMessages;
	}
}