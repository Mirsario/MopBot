using Discord;
using System;

namespace MopBot.Core.Systems.Memory
{
	[Serializable]
	public class UserData : MemoryDataBase
	{
		public virtual void Initialize(IUser user) {}
	}
}