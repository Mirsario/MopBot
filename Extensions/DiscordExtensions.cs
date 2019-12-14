using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Text;

namespace MopBotTwo.Extensions
{
	public static class DiscordExtensions
	{
		public static bool TryGetServer(this DiscordSocketClient client,ulong id,out SocketGuild result) => (result = client.GetGuild(id))!=null;
		public static bool TryGetChannel(this SocketGuild server,ulong id,out SocketGuildChannel result) => (result = server.GetChannel(id))!=null;
		public static bool TryGetTextChannel(this SocketGuild server,ulong id,out SocketTextChannel result) => (result = server.GetChannel(id) as SocketTextChannel)!=null;
	}
}
