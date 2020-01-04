using System.Threading.Tasks;
using System.Text.RegularExpressions;
using Discord.Commands;
using MopBotTwo.Extensions;
using MopBotTwo.Core.Systems;
using MopBotTwo.Core.Systems.Permissions;
using MopBotTwo.Core.Systems.Memory;
using MopBotTwo.Common.Systems.Changelogs;

namespace MopBotTwo.Common.Systems.Issues
{
	[Group("issue")]
	[Alias("issues","knownissues")]
	[Summary("Helps managing project issues channels")]
	[RequirePermission(SpecialPermission.Owner,"issuesystem")]
	[SystemConfiguration(Description = "In-discord bug tracker. Can write to ChangelogSystem when an issue gets fixed.")]
	public partial class IssueSystem : BotSystem
	{
		public override void RegisterDataTypes()
		{
			RegisterDataType<ServerMemory,IssueServerData>();
		}

		private async Task<IssueInfo> NewIssueInternal(string issueText,bool publish)
		{
			var data = Context.server.GetMemory().GetData<IssueSystem,IssueServerData>();
			var channel = await data.GetIssueChannel(Context);

			var newIssue = data.NewIssue(issueText);

			if(publish) {
				await data.PublishIssue(newIssue,channel);
			}

			return newIssue;
		}
		private async Task FixIssueInternal(uint issueId,bool publish)
		{
			var context = Context;
			var server = context.server;
			var memory = context.server.GetMemory();
			var data = memory.GetData<IssueSystem,IssueServerData>();
			var channel = await data.GetIssueChannel(context);

			if(!data.issues.TryGetFirst(i => i.issueId==issueId,out var issue)) {
				throw new BotError($"An issue with Id #{issueId} could not be found.");
			}

			issue.status = IssueStatus.Closed;

			if(publish) {
				await data.PublishIssue(issue,channel); //Will delete the old message
			}

			if(IsEnabledForServer<ChangelogSystem>(server)) {
				var changelogData = memory.GetData<ChangelogSystem,ChangelogServerData>();

				if(changelogData.GetChangelogChannel(out var clChannel)) {
					await changelogData.PublishEntry(changelogData.NewEntry("fixed",$"Issue #{issue.issueId} - {issue.text}"),clChannel);
				}
			}
		}
		private async Task RepublishAll()
		{
			var data = Context.server.GetMemory().GetData<IssueSystem,IssueServerData>();

			var channel = await data.GetIssueChannel(Context);

			foreach(var issue in data.OrderedIssues) {
				await data.PublishIssue(issue,channel);
			}
		}
	}
}