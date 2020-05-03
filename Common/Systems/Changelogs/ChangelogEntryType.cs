using System;

namespace MopBot.Common.Systems.Changelogs
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