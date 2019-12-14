using Discord.WebSocket;
using MopBotTwo.Core.Systems.Memory;

namespace MopBotTwo.Common.Systems.Configuration
{
	public class ConfigurationServerData : ServerData
	{
		public string forcedNickname;

		public override void Initialize(SocketGuild server)
		{
			forcedNickname = "MopBot";
		}
	}
}