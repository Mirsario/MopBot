using System;

#pragma warning disable CS1998

namespace MopBotTwo.Systems
{
	public partial class ChangelogSystem
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
}