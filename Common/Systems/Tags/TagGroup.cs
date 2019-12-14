using System;
using System.Collections.Generic;

namespace MopBotTwo.Common.Systems.Tags
{
	[Serializable]
	public class TagGroup
	{
		public ulong owner;
		public string name;

		public List<ulong> tagIDs = new List<ulong>();

		public TagGroup(ulong owner,string name)
		{
			this.owner = owner;
			this.name = name.ToLowerInvariant();
		}
	}
}