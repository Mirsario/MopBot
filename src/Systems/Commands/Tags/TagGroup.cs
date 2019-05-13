using System;
using System.Collections.Generic;

namespace MopBotTwo
{
	[Serializable]
	public class TagGroup
	{
		public ulong id;
		public ulong owner;
		public string name;

		public List<ulong> tagIDs = new List<ulong>();

		public TagGroup(ulong id,ulong owner,string name)
		{
			this.id = id;
			this.owner = owner;
			this.name = name.ToLowerInvariant();
		}
	}
}