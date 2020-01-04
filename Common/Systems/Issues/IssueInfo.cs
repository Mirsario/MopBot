using System;


namespace MopBotTwo.Common.Systems.Issues
{
	[Serializable]
	public class IssueInfo
	{
		public uint issueId;
		public ulong messageId;
		public ulong channelId;
		public IssueStatus status;
		public string text;
	}
}