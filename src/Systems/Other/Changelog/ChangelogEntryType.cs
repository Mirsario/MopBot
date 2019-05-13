using System;

#pragma warning disable CS1998

namespace MopBotTwo.Systems
{
	public partial class ChangelogSystem
	{
		[Serializable]
		public struct ChangelogEntryType
		{
			public string name;
			public string discordPrefix;

			public ChangelogEntryType(string name,string discordPrefix)
			{
				this.name = name;
				this.discordPrefix = discordPrefix;
			}
		}
	}
}