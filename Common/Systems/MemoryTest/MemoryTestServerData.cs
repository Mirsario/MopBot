using Discord.WebSocket;
using MopBotTwo.Core.Systems.Memory;
using MopBotTwo.Extensions;
using System;

namespace MopBotTwo.Common.Systems.MemoryTest
{
	[Serializable]
	public class MemoryTestServerData : ServerData
	{
		public ulong randomValue;

		public override void Initialize(SocketGuild server)
		{
			randomValue = MopBot.random.NextULong();
		}
	}
}