﻿using System.Linq;
using System.Threading.Tasks;
using Discord.WebSocket;
using Discord.Commands;
using MopBot.Extensions;
using MopBot.Core.Systems.Permissions;

namespace MopBot.Common.Systems.Issues
{
	//TODO: Make issue-getting code less repetitive.

	public partial class IssueSystem
	{
		[Command("setchannel")]
		[RequirePermission(SpecialPermission.Admin, "issuesystem.configure")]
		public async Task SetChannel(SocketGuildChannel channel)
		{
			var data = Context.server.GetMemory().GetData<IssueSystem, IssueServerData>();

			data.issueChannel = channel.Id;

			await RepublishAll();
		}

		[Command("new")]
		[Alias("add", "open")]
		[RequirePermission(SpecialPermission.Admin, "issuesystem.issues.manage")]
		public async Task New([Remainder] string issueText)
		{
			if (await NewIssueInternal(issueText, true) == null) {
				return;
			}
		}

		[Command("close")]
		[Alias("fix")]
		[RequirePermission(SpecialPermission.Admin, "issuesystem.issues.manage")]
		public Task FixIssue(uint issueId) => FixIssueInternal(issueId, true);

		[Command("newclosed")]
		[Alias("addclosed", "openandclose", "newfixed", "addfixed", "openandfix")]
		[RequirePermission(SpecialPermission.Admin, "issuesystem.issues.manage")]
		public async Task AddFixedIssue([Remainder] string issueText)
		{
			var newIssue = await NewIssueInternal(issueText, false);

			await FixIssueInternal(newIssue.issueId, true);
		}

		[Command("edit")]
		[Alias("modify")]
		[RequirePermission(SpecialPermission.Admin, "issuesystem.issues.manage")]
		public async Task EditIssue(uint issueId, [Remainder] string issueText)
		{
			var data = Context.server.GetMemory().GetData<IssueSystem, IssueServerData>();
			var channel = await data.GetIssueChannel(Context);
			var issue = data.issues.FirstOrDefault(i => i.issueId == issueId) ?? throw new BotError($"An issue with Id #{issueId} could not be found.");

			issue.text = issueText;

			await data.PublishIssue(issue, channel);
		}

		[Command("show")]
		[Alias("see")]
		[RequirePermission(SpecialPermission.Admin, "issuesystem.issues.show")]
		public async Task ShowIssue(uint issueId)
		{
			var data = Context.server.GetMemory().GetData<IssueSystem, IssueServerData>();
			var issue = data.issues.FirstOrDefault(i => i.issueId == issueId) ?? throw new BotError($"An issue with Id #{issueId} could not be found.");

			var user = Context.user;
			var builder = MopBot.GetEmbedBuilder(Context)
				.WithAuthor($"Requested by {user.GetDisplayName()}", user.GetAvatarUrl())
				.WithTitle($"Issue #{issue.issueId}")
				.WithDescription($"**Status:** {StatusText[issue.status]}```\r\n{issue.text}```");

			await Context.messageChannel.SendMessageAsync(embed: builder.Build());
		}

		[Command("remove")]
		[Alias("delete")]
		[RequirePermission(SpecialPermission.Admin, "issuesystem.issues.manage")]
		public async Task RemoveIssue(uint issueId)
		{
			var server = Context.server;
			var data = server.GetMemory().GetData<IssueSystem, IssueServerData>();
			var issue = data.issues.FirstOrDefault(i => i.issueId == issueId) ?? throw new BotError($"An issue with Id #{issueId} could not be found.");

			await data.UnpublishIssue(issue, server);

			data.issues.Remove(issue);
		}

		[Command("clearclosed")]
		[Alias("clearfixed")]
		[RequirePermission(SpecialPermission.Admin, "issuesystem.issues.manage")]
		public async Task ClearFixedIssues(string justReleasedVersion)
		{
			var server = Context.server;
			var data = server.GetMemory().GetData<IssueSystem, IssueServerData>();
			int numClosed = 0;
			var channel = await data.GetIssueChannel(Context);

			for (int i = 0; i < data.issues.Count; i++) {
				var issue = data.issues[i];

				if (issue.status == IssueStatus.Closed) {
					await data.UnpublishIssue(issue, server);

					data.issues.RemoveAt(i--);

					numClosed++;
				}
			}

			if (justReleasedVersion != null) {
				await channel.SendMessageAsync($"***{justReleasedVersion} has just been released. Channel has been cleared.***\r\n{numClosed}+ issues were fixed.");
			}
		}
	}
}
