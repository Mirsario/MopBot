using Discord;
using MopBotTwo.Core.Systems.Memory;
using MopBotTwo.Extensions;
using System;

namespace MopBotTwo.Common.Systems.MemoryTest
{
	[Serializable]
	public class MemoryTestUserData : UserData
	{
		public ulong randomValue;

		public override void Initialize(IUser user)
		{
			randomValue = MopBot.random.NextULong();
		}
	}
}