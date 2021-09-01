﻿using System;
using Discord.WebSocket;

namespace MopBot.Common.Systems.Showcase
{
	[Serializable]
	public class ShowcaseChannel : ChannelInfo
	{
		public ulong spotlightChannel;
		public uint minSpotlightScore = 20;

		public SocketTextChannel GetSpotlightChannel(SocketGuild server)
		{
			return spotlightChannel == 0 ? null : server.GetChannel(spotlightChannel) as SocketTextChannel;
		}
	}
}
