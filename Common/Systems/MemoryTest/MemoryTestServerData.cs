using Discord.WebSocket;
using MopBot.Core.Systems.Memory;
using MopBot.Extensions;
using System;

namespace MopBot.Common.Systems.MemoryTest
{
	[Serializable]
	public class MemoryTestServerData : ServerData
	{
		public ulong randomValue;

		public override void Initialize(SocketGuild server)
		{
			randomValue = MopBot.Random.NextULong();
		}
	}
}