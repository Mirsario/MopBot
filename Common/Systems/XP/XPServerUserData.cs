using System;
using MopBot.Core.Systems.Memory;

namespace MopBot.Common.Systems.XP
{
	[Serializable]
	public class XPServerUserData : ServerUserData
	{
		public DateTime lastXPReceive;
		public ulong xp;
	}
}