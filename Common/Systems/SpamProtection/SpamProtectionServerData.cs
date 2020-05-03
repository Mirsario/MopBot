using Discord.WebSocket;
using MopBot.Core.Systems.Memory;

namespace MopBot.Common.Systems.SpamProtection
{
	public class SpamProtectionServerData : ServerData
	{
		public float muteTimeInSeconds = 10f;
		public float spamDetectionTime = 3f;
		public ushort spamDetectionNumMessages = 3;

		public override void Initialize(SocketGuild server) { }
	}
}