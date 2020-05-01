using Discord.WebSocket;
using MopBot.Core.DataStructures;
using MopBot.Core.Systems.Memory;

namespace MopBot.Core.Systems.Commands
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