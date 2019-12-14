using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Discord;
using Discord.WebSocket;
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
		private async Task FixIssueInternal(uint issueId,string version,bool publish)
		{
			var context = Context;
			var server = context.server;
			var memory = context.server.GetMemory();
			var data = memory.GetData<IssueSystem,IssueServerData>();
			var channel = await data.GetIssueChannel(context);

			string realVersionStr;
			if(version==null) {
				if(data.defaultFixVersion==null) {
					throw new BotError("No version has been specified and the default version is not set too.");
				}
				realVersionStr = data.defaultFixVersion;
			} else if(!Version.TryParse(version,out Version realVersion)) {
				throw new BotError($"'{version}' is not a valid version number.");
			} else {
				realVersionStr = realVersion.ToString();
			}

			if(!data.issues.TryGetFirst(i => i.issueId==issueId,out var issue)) {
				throw new BotError($"An issue with Id #{issueId} could not be found.");
			}

			issue.status = IssueStatus.Closed;
			issue.version = realVersionStr;

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

		[Command("setchannel")]
		[RequirePermission(SpecialPermission.Owner,"issuesystem.manage.setchannel")]
		public async Task SetChannel(SocketGuildChannel channel)
		{
			var data = Context.server.GetMemory().GetData<IssueSystem,IssueServerData>();
			data.issueChannel = channel.Id;

			await RepublishAll();
		}
		[Command("setstatusprefix")]
		[RequirePermission(SpecialPermission.Owner,"issuesystem.manage.setstatusprefix")]
		public async Task SetStatusPrefix(IssueStatus status,string prefix)
		{
			var data = Context.server.GetMemory().GetData<IssueSystem,IssueServerData>();
			data.statusPrefix[status] = prefix;
		}
		[Command("setversion")] [Alias("setdefaultfixversion","setdefaultversion")]
		[RequirePermission(SpecialPermission.Owner,"issuesystem.manage.setdefaultversion")]
		public async Task SetDefaultVersion(string version)
		{
			if(!Version.TryParse(version,out Version realVersion)) {
				throw new BotError($"'{version}' is not a valid version number.");
			}

			var data = Context.server.GetMemory().GetData<IssueSystem,IssueServerData>();
			data.defaultFixVersion = realVersion.ToString();
		}
		[Command("upgradeversion")]
		[RequirePermission(SpecialPermission.Owner,"issuesystem.manage.upgradeversion")]
		public async Task UpgradeVersion()
		{
			var data = Context.server.GetMemory().GetData<IssueSystem,IssueServerData>();
			var channel = await data.GetIssueChannel(Context);
			
			foreach(var issue in data.OrderedIssues) {
				if(issue.version!=data.defaultFixVersion) {
					issue.version = data.defaultFixVersion;

					await data.PublishIssue(issue,channel);
				}
			}
		}
		[Command("setnextissueid")]
		[RequirePermission(SpecialPermission.Owner,"issuesystem.manage.setnextissueid")]
		public async Task SetNextIssueId(uint id)
		{
			var data = Context.server.GetMemory().GetData<IssueSystem,IssueServerData>();
			data.nextIssueId = id;
		}
		[Command("republishall")]
		[RequirePermission(SpecialPermission.Owner,"issuesystem.manage.republishall")]
		public async Task RepublishAll()
		{
			var data = Context.server.GetMemory().GetData<IssueSystem,IssueServerData>();
			
			var channel = await data.GetIssueChannel(Context);

			foreach(var issue in data.OrderedIssues) {
				await data.PublishIssue(issue,channel);
			}
		}

		[Command("new")] [Alias("add","open")]
		[RequirePermission(SpecialPermission.Owner,"issuesystem.issues.add")]
		public async Task New([Remainder]string issueText)
		{
			if(await NewIssueInternal(issueText,true)==null) {
				return;
			}
		}
		
		[Command("close")] [Alias("fix")]
		[RequirePermission(SpecialPermission.Owner,"issuesystem.issues.fix")]
		public async Task FixIssue(uint issueId,string version = null)
		{
			await FixIssueInternal(issueId,version,true);
		}
		
		[Command("newclosed")] [Alias("addclosed","openandclose","newfixed","addfixed","openandfix")]
		[RequirePermission(SpecialPermission.Owner,"issuesystem.issues.newclosed")]
		public async Task AddFixedIssue([Remainder]string issueText)
		{
			var data = Context.server.GetMemory().GetData<IssueSystem,IssueServerData>();
			if(string.IsNullOrWhiteSpace(data.defaultFixVersion)) {
				throw new BotError("This command requires a default fix version to be set. Use `!issues setdefaultversion <version>`.");
			}

			var newIssue = await NewIssueInternal(issueText,false);

			await FixIssueInternal(newIssue.issueId,data.defaultFixVersion,true);
		}
		
		[Command("edit")] [Alias("modify")]
		[RequirePermission(SpecialPermission.Owner,"issuesystem.issues.edit")]
		public async Task EditIssue(uint issueId,[Remainder]string issueText)
		{
			var data = Context.server.GetMemory().GetData<IssueSystem,IssueServerData>();
			var channel = await data.GetIssueChannel(Context);

			var issue = data.issues.FirstOrDefault(i => i.issueId==issueId);
			if(issue==null) {
				throw new BotError($"An issue with Id #{issueId} could not be found.");
			}

			issue.text = issueText;

			await data.PublishIssue(issue,channel);
		}

		[Command("show")] [Alias("see")]
		[RequirePermission(SpecialPermission.Owner,"issuesystem.issues.show")]
		public async Task ShowIssue(uint issueId)
		{
			var data = Context.server.GetMemory().GetData<IssueSystem,IssueServerData>();

			var issue = data.issues.FirstOrDefault(i => i.issueId==issueId);
			if(issue==null) {
				throw new BotError($"An issue with Id #{issueId} could not be found.");
			}

			var user = Context.user;
			var builder = MopBot.GetEmbedBuilder(Context)
				.WithAuthor($"Requested by {user.Name()}",user.GetAvatarUrl())
				.WithTitle($"Issue #{issue.issueId}")
				.WithDescription($"**Status:** {data.statusText[issue.status]?.Replace("{version}",issue.version)}```\n{issue.text}```");
			
			await Context.messageChannel.SendMessageAsync(embed:builder.Build());
		}
		
		[Command("remove")] [Alias("delete")]
		[RequirePermission(SpecialPermission.Owner,"issuesystem.issues.remove")]
		public async Task RemoveIssue(uint issueId)
		{
			var server = Context.server;
			var data = server.GetMemory().GetData<IssueSystem,IssueServerData>();

			var issue = data.issues.FirstOrDefault(i => i.issueId==issueId);
			if(issue==null) {
				throw new BotError($"An issue with Id #{issueId} could not be found.");
			}

			await data.UnpublishIssue(issue,server);

			data.issues.Remove(issue);
		}
		[Command("clearclosed")] [Alias("clearfixed")]
		[RequirePermission(SpecialPermission.Owner,"issuesystem.issues.clearfixed")]
		public async Task ClearFixedIssues(string justReleasedVersion)
		{
			var server = Context.server;
			var data = server.GetMemory().GetData<IssueSystem,IssueServerData>();
			int numClosed = 0;
			int numOpen = 0;
			var channel = await data.GetIssueChannel(Context);
			
			for(int i = 0;i<data.issues.Count;i++) {
				var issue = data.issues[i];

				if(issue.status==IssueStatus.Closed) {
					await data.UnpublishIssue(issue,server);

					data.issues.RemoveAt(i--);

					numClosed++;
				}else{
					numOpen++;
				}
			}
			if(justReleasedVersion!=null) {
				await channel.SendMessageAsync($"***{justReleasedVersion} has just released, channel cleared.***\n{numClosed} issues were fixed, ~{numOpen} remained.");
			}
		}

		[Command("parse")]
		[RequirePermission(SpecialPermission.Owner,"issuesystem.issues.parse")]
		public async Task Parse(SocketGuildChannel channel,SocketGuildUser fromUser = null,ulong? afterMessage = null)
		{
			if(!(channel is SocketTextChannel textChannel)) {
				throw new BotError("Channel must be a text channel.");
			}
			var data = Context.server.GetMemory().GetData<IssueSystem,IssueServerData>();
			var issueChannel = await data.GetIssueChannel(Context);
			
			if(string.IsNullOrWhiteSpace(data.defaultFixVersion)) {
				throw new BotError("Please assign a default issue version via `!issue setversion <version>` first.");
			}

			const int Limit = 1000;

			ulong channelId = channel.Id;
			ulong userId = fromUser?.Id ?? 0;
			ulong thisUserId = Context.Client.CurrentUser.Id;

			int numChecked = 0;
			int numGeneralFails = 0;
			int numIssueStatusParseFails = 0;
			int numOtherFails = 0;

			var regex = new Regex(@"(:[\w]+:) - #([\d*`~]+) - ([^:]+):[*`~\s]+(.+)");

			List<(Regex regex,IssueStatus issueStatus)> regexIssueStatusPairs = new List<(Regex,IssueStatus)>();

			foreach(var pair in data.statusText) {
				regexIssueStatusPairs.Add((
					new Regex(pair.Value.Replace(@"{version}",@"([.\d~*`]+)")),
					pair.Key
				));
			}

			static string RemoveFormatting(string original) => original.Replace("~","").Replace("*","").Replace("`","");

			var messages = afterMessage.HasValue ? textChannel.GetMessagesAsync(afterMessage.Value,Direction.After,Limit) : textChannel.GetMessagesAsync(Limit);
			await messages.ForEachAsync(async collection => {
				foreach(var msg in collection) {
					ulong authorId = msg.Author.Id;
					if(userId!=0 && authorId!=userId) {
						continue;
					}

					numChecked++;
					
					var content = msg.Content;
					var match = regex.Match(content);

					if(!match.Success) {
						numGeneralFails++;

						if(numGeneralFails==1) {
							await Context.ReplyAsync($"`{content}`");
						}

						continue;
					}

					var groups = match.Groups;

					string issueEmote = groups[1].Value;
					if(!uint.TryParse(RemoveFormatting(groups[2].Value),out uint issueId)) {
						numOtherFails++;
						continue;
					}

					string issueStatusText = RemoveFormatting(groups[3].Value);
					string issueText = groups[4].Value;

					string issueVersion = null;
					IssueStatus issueStatus = 0;
					bool success = false;

					foreach(var tuple in regexIssueStatusPairs) {
						match = tuple.regex.Match(issueStatusText);

						if(match.Success) {
							if(match.Groups.Count>=2) {
								var value = RemoveFormatting(match.Groups[1].Value);
								issueVersion = Version.TryParse(value,out _) ? value : data.defaultFixVersion;
							}

							issueStatus = tuple.issueStatus;
							success = true;
							break;
						}
					}
					if(!success) {
						numIssueStatusParseFails++;
						continue;
					}

					if(!data.issues.TryGetFirst(info => info.issueId==issueId,out var issue)) {
						data.issues.Add(issue = new IssueInfo {
							issueId = issueId,
						});
					}

					issue.status = issueStatus;
					issue.text = issueText;
					issue.version = issueVersion;

					if(authorId==thisUserId) {
						issue.messageId = msg.Id;
						issue.channelId = channelId;
					}else{
						await data.PublishIssue(issue,issueChannel);
					}
				}
			});

			await Context.ReplyAsync($"Successfully parsed {numChecked-numGeneralFails-numIssueStatusParseFails-numOtherFails}/{numChecked} messages.\r\n{numGeneralFails} 'general' failures.\r\n{numIssueStatusParseFails} 'issue status parsing' failures.\r\n{numOtherFails} 'other' failures.");
		}
	}
}