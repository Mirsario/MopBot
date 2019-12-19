using MopBotTwo.Core.Systems.Memory;
using MopBotTwo.Extensions;
using System;

namespace MopBotTwo.Common.Systems.MemoryTest
{
	[Serializable]
	public class MemoryTestServerUserData : ServerUserData
	{
		public ulong randomValue;

		public override void Initialize()
		{
			randomValue = MopBot.random.NextULong();
		}
	}
}