using System;
using Discord.WebSocket;


namespace MopBotTwo.Common.Systems.Showcase
{
	[Serializable]
	public class ShowcaseChannel : ChannelInfo
	{
		public ulong spotlightChannel;
		public uint minSpotlightScore = 20;

		public SocketTextChannel GetSpotlightChannel(SocketGuild server) => spotlightChannel==0 ? null : server.GetChannel(spotlightChannel) as SocketTextChannel;
	}
}