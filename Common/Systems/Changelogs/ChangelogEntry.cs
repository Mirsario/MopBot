using System;


namespace MopBot.Common.Systems.Changelogs
{
	[Serializable]
	public class ChangelogEntry
	{
		public uint entryId;
		public ulong messageId;
		public ulong channelId;
		public string type;
		public string text;

		public ChangelogEntry(uint entryId,string type,string text)
		{
			this.entryId = entryId;
			this.type = type;
			this.text = text;
		}
	}
}