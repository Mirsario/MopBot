using MopBot.Core.DataStructures;
using System;
using System.Collections.Generic;

namespace MopBot.Common.Systems.ChannelLinking
{
	[Serializable]
	public struct ChannelLink
	{
		public ulong owner;
		public List<ServerChannelIds> connectedChannels;
		public List<ServerChannelIds> invitedChannels;

		public ChannelLink(ulong owner)
		{
			this.owner = owner;

			connectedChannels = new List<ServerChannelIds>();
			invitedChannels = new List<ServerChannelIds>();
		}
	}
}