using MopBot.Core.Systems.Memory;
using MopBot.Extensions;
using System;

namespace MopBot.Common.Systems.MemoryTest
{
	[Serializable]
	public class MemoryTestServerUserData : ServerUserData
	{
		public ulong randomValue;

		public override void Initialize()
		{
			randomValue = MopBot.Random.NextULong();
		}
	}
}