using Discord.WebSocket;
using MopBotTwo.Core.Systems.Memory;


namespace MopBotTwo.Common.Systems.Welcoming
{
	public class WelcomeServerData : ServerData
	{
		public ulong channel;
		public string messageJoin;
		public string messageRejoin;

		public override void Initialize(SocketGuild server)
		{
			messageJoin = "Welcome! Please enjoy your stay!";
			messageRejoin = "Welcome back! Were you get kicked or something? Anyway, enjoy your stay!";
		}
	}
}