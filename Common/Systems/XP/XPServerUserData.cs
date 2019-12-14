using System;
using MopBotTwo.Core.Systems.Memory;

namespace MopBotTwo.Common.Systems.XP
{
	[Serializable]
	public class XPServerUserData : ServerUserData
	{
		public DateTime lastXPReceive;
		public ulong xp;
	}
}