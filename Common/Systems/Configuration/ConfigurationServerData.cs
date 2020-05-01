using Discord.WebSocket;
using MopBot.Core.Systems.Memory;

namespace MopBot.Common.Systems.Configuration
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