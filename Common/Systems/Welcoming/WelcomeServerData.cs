using Discord.WebSocket;
using MopBotTwo.Core.Systems.Memory;


namespace MopBotTwo.Common.Systems.Welcoming
{
	public class WelcomeServerData : ServerData
	{
		public string messageJoin;
		public string messageRejoin;

		public override void Initialize(SocketGuild server)
		{
			messageJoin = "Welcome, {user}! Please check out {rules}, and enjoy your stay!";
			messageRejoin = "Welcome back, {user}! Did you get kicked or something? Anyway, check out {rules}, and enjoy your stay!";
		}
	}
}