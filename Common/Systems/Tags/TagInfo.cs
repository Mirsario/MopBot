using System;

namespace MopBot.Common.Systems.Tags
{
	[Serializable]
	public class Tag
	{
		public ulong owner;
		public string name;
		public string text;

		public Tag(ulong owner,string name,string text)
		{
			this.owner = owner;
			this.name = name.ToLowerInvariant();
			this.text = text;
		}
	}
}