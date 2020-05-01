using Discord.WebSocket;

namespace MopBot.Extensions
{
	public static class DiscordExtensions
	{
		public static bool TryGetServer(this DiscordSocketClient client,ulong id,out SocketGuild result) => (result = client.GetGuild(id))!=null;
		public static bool TryGetChannel(this SocketGuild server,ulong id,out SocketGuildChannel result) => (result = server.GetChannel(id))!=null;
		public static bool TryGetTextChannel(this SocketGuild server,ulong id,out SocketTextChannel result) => (result = server.GetChannel(id) as SocketTextChannel)!=null;

		public static bool TryGetServer(this DiscordSocketClient client,string name,out SocketGuild result) => client.Guilds.TryGetFirst(c => c.Name==name,out result);
		public static bool TryGetChannel(this SocketGuild server,string name,out SocketGuildChannel result) => server.Channels.TryGetFirst(c => c.Name==name,out result);
		public static bool TryGetTextChannel(this SocketGuild server,string channelName,out SocketTextChannel result)
		{
			if(TryGetChannel(server,channelName,out var channel)) {
				return (result = channel as SocketTextChannel)!=null;
			}

			result = null;

			return false;
		}

		public static SocketGuild GetServer(this DiscordSocketClient client,ulong id) => client.TryGetServer(id,out var result) ? result : throw new BotError($"Unknown server id: `{id}`.");
		public static SocketGuildChannel GetChannel(this SocketGuild server,ulong id) => server.TryGetChannel(id,out var result) ? result : throw new BotError($"Unknown channel id: `{id}`.");
		public static SocketTextChannel GetTextChannel(this SocketGuild server,ulong id) => server.TryGetTextChannel(id,out var result) ? result : throw new BotError($"Unknown text channel id: `{id}`.");

		public static SocketGuild GetServer(this DiscordSocketClient client,string name) => client.TryGetServer(name,out var result) ? result : throw new BotError($"Unknown server: `{name}`.");
		public static SocketGuildChannel GetChannel(this SocketGuild server,string name) => server.TryGetChannel(name,out var result) ? result : throw new BotError($"Unknown channel: `{name}`.");
		public static SocketTextChannel GetTextChannel(this SocketGuild server,string name) => server.TryGetTextChannel(name,out var result) ? result : throw new BotError($"Unknown text channel: `{name}`.");
	}
}
