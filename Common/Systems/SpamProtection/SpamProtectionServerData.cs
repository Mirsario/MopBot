using Discord.WebSocket;
using MopBotTwo.Core.Systems.Memory;


namespace MopBotTwo.Common.Systems.SpamProtection
{
	public class SpamProtectionServerData : ServerData
	{
		public float muteTimeInSeconds = 10f;
		public float spamDetectionTime = 3f;
		public ushort spamDetectionNumMessages = 3;

		public override void Initialize(SocketGuild server) { }
	}
}