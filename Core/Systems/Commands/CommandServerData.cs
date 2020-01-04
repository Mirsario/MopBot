using Discord.WebSocket;
using MopBotTwo.Core.DataStructures;
using MopBotTwo.Core.Systems.Memory;

namespace MopBotTwo.Core.Systems.Commands
{
	public class CommandServerData : ServerData
	{
		public char commandPrefix;
		public bool? showUnknownCommandMessage;
		public Color? embedColor;

		public override void Initialize(SocketGuild server)
		{
			commandPrefix = '!';
			showUnknownCommandMessage = true;
			embedColor = new Color(168,125,101);
		}
	}
}