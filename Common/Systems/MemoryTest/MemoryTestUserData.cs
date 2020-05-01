using Discord;
using MopBot.Core.Systems.Memory;
using MopBot.Extensions;
using System;

namespace MopBot.Common.Systems.MemoryTest
{
	[Serializable]
	public class MemoryTestUserData : UserData
	{
		public ulong randomValue;

		public override void Initialize(IUser user)
		{
			randomValue = MopBot.Random.NextULong();
		}
	}
}