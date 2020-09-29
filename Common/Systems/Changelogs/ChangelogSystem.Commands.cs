using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.IO;
using Discord;
using Discord.WebSocket;
using Discord.Commands;
using MopBot.Extensions;
using MopBot.Core.Systems.Permissions;
using MopBot.Common.Systems.Issues;

#pragma warning disable CS1998 //Async method lacks 'await' operators and will run synchronously

namespace MopBot.Common.Systems.Changelogs
{
	//TODO: Improve code quality, and reduce amount of code repeated.
	public partial class ChangelogSystem
	{
		[Command("setchannel")]
		public async Task SetChannel(SocketGuildChannel channel)
		{
			Context.server.GetMemory().GetData<ChangelogSystem, ChangelogServerData>().changelogChannel = channel.Id;

			await RepublishAll();
		}

		[Command("setentrytype")]
		public async Task SetStatusPrefix(string entryId, string name, string discordPrefix)
		{
			var data = Context.server.GetMemory().GetData<ChangelogSystem, ChangelogServerData>();

			entryId = entryId.ToLower();
			data.entryTypes[entryId] = new ChangelogEntryType(name, discordPrefix);
		}

		[Command("setcurrentversion")]
		[Alias("setversion")]
		public async Task SetDefaultVersion(string version)
		{
			if(!Version.TryParse(version, out Version realVersion)) {
				throw new BotError($"'{version}' is not a valid version number.");
			}

			var data = Context.server.GetMemory().GetData<ChangelogSystem, ChangelogServerData>();

			data.currentVersion = realVersion.ToString();
		}

		[Command("setnextid")]
		public async Task SetNextID(uint id)
		{
			var data = Context.server.GetMemory().GetData<ChangelogSystem, ChangelogServerData>();

			data.nextEntryId = id;
		}

		[Command("republishall")]
		public async Task RepublishAll()
		{
			var data = Context.server.GetMemory().GetData<ChangelogSystem, ChangelogServerData>();

			if(!data.GetChangelogChannel(out var channel)) {
				throw new BotError("Changelog channel has not been set!");
			}

			foreach(var entry in data.entries) {
				await data.PublishEntry(entry, channel);
			}
		}

		[Command("addfixedissues")]
		public async Task AddFixedIssues(string asEntryType)
		{
			var context = Context;
			var data = context.server.GetMemory().GetData<ChangelogSystem, ChangelogServerData>();

			if(!data.entryTypes.ContainsKey(asEntryType)) {
				throw new BotError($"Couldn't find an entry type with name `{asEntryType}`");
			}

			var issueData = context.server.GetMemory().GetData<IssueSystem, IssueServerData>();

			if(!data.GetChangelogChannel(out var channel)) {
				throw new BotError("Changelog channel has not been set!");
			}

			foreach(var issue in issueData.issues) {
				if(issue.status == IssueStatus.Closed) {
					var entry = data.NewEntry(asEntryType, $"Issue #{issue.issueId} - {issue.text}");

					await data.PublishEntry(entry, channel);
				}
			}
		}

		[Command("new")]
		[Alias("add")]
		public async Task New(string type, [Remainder] string entryText)
		{
			var data = Context.server.GetMemory().GetData<ChangelogSystem, ChangelogServerData>();

			if(!data.entryTypes.ContainsKey(type)) {
				throw new BotError($"Couldn't find an entry type with name `{type}`");
			}

			await NewEntryInternal(type, entryText, true, data);
		}

		[Command("edit")]
		[Alias("modify")]
		public async Task EditEntry(uint entryId, [Remainder] string entryText)
		{
			var data = Context.server.GetMemory().GetData<ChangelogSystem, ChangelogServerData>();

			if(!data.GetChangelogChannel(out var channel)) {
				throw new BotError("Changelog channel has not been set!");
			}

			var entry = data.entries.FirstOrDefault(i => i.entryId == entryId);

			if(entry == null) {
				throw new BotError($"An entry with Id #{entryId} could not be found.");
			}

			entry.text = entryText;

			await data.PublishEntry(entry, channel);
		}

		[Command("remove")]
		[Alias("delete")]
		[RequirePermission(SpecialPermission.Owner, "changelog.entries.manage")]
		public async Task RemoveEntry(params uint[] ids)
		{
			var server = Context.server;
			var data = server.GetMemory().GetData<ChangelogSystem, ChangelogServerData>();

			List<uint> failedIDs = null;

			foreach(uint id in ids) {
				var entry = data.entries.FirstOrDefault(i => i.entryId == id);

				if(entry != null) {
					await data.UnpublishEntry(entry, server);

					data.entries.Remove(entry);
				} else {
					(failedIDs ??= new List<uint>()).Add(id);
				}
			}
		}

		[Command("clear")]
		[Alias("empty")]
		[RequirePermission(SpecialPermission.Owner, "changelog.entries.manage")]
		public async Task ClearEntries([Remainder] string confirmation)
		{
			var context = Context;

			if(confirmation.ToLower() != "yes, do it.") {
				await context.ReplyAsync("All changelog entries will be removed and you won't be able to use `!cl get` to compile them into text files.\r\nConfirm this action by typing `!cl clear Yes, do it.`");
				return;
			}

			var server = context.server;
			var data = server.GetMemory().GetData<ChangelogSystem, ChangelogServerData>();

			while(data.entries.Count > 0) {
				var entry = data.entries[0];

				if(entry != null) {
					await data.UnpublishEntry(entry, server);
				}

				data.entries.RemoveAt(0);
			}
		}

		[Command("show")]
		[Alias("see")]
		[RequirePermission(SpecialPermission.Owner, "changelog.entries.show")]
		public async Task ShowEntry(uint entryId)
		{
			var context = Context;
			var data = context.server.GetMemory().GetData<ChangelogSystem, ChangelogServerData>();
			var entry = data.entries.FirstOrDefault(i => i.entryId == entryId);

			if(entry == null) {
				throw new BotError($"An entry with Id #{entryId} could not be found.");
			}

			var user = context.user;
			var builder = MopBot.GetEmbedBuilder(context)
				.WithAuthor($"Requested by {user.GetDisplayName()}", user.GetAvatarUrl())
				.WithTitle($"Entry #{entry.entryId}")
				.WithDescription($"**Status:** {(data.entryTypes.TryGetValue(entry.type, out var entryType) ? $"{entryType.discordPrefix} - {entryType.name}" : $"UNKNOWN (`{entry.type}`)")}```\r\n{entry.text}```");

			await context.messageChannel.SendMessageAsync(embed: builder.Build());
		}

		[Command("showentrytypes")]
		public async Task ShowStatusPrefixes()
		{
			var context = Context;
			var data = context.server.GetMemory().GetData<ChangelogSystem, ChangelogServerData>();
			var user = context.user;
			var builder = MopBot.GetEmbedBuilder(context)
				.WithAuthor($"Requested by {user.GetDisplayName()}", user.GetAvatarUrl())
				.WithTitle($"Changelog Entry Types:");

			string text = null;
			bool addLineBreak = false;

			foreach(var pair in data.entryTypes) {
				var val = pair.Value;

				text += $"{(addLineBreak ? "\r\n" : null)}`{pair.Key}` - {val.discordPrefix} - {val.name}";

				addLineBreak = true;
			}

			builder.WithDescription(text);

			await context.messageChannel.SendMessageAsync(embed: builder.Build());
		}

		[Command("get")]
		[Alias("getfile", "gettext", "output")]
		public async Task GetChangelog(ChangelogFormatType formatType)
		{
			var context = Context;

			try {
				var server = context.server;
				var data = server.GetMemory().GetData<ChangelogSystem, ChangelogServerData>();

				string categoryStr;
				string entryStr;

				switch(formatType) {
					default:
						categoryStr = "{0}:\r\n{1}\r\n";
						entryStr = "- {0}\r\n";
						break;
					case ChangelogFormatType.Discord:
						categoryStr = "**{0}:**```\r\n{1}```\r\n";
						entryStr = "- {0}\r\n";
						break;
					case ChangelogFormatType.BBCode:
						categoryStr = "[b]{0}:[/b][list]\r\n{1}[/list]\r\n";
						entryStr = "[*]{0}\r\n";
						break;
					case ChangelogFormatType.Patreon:
						categoryStr = "{0}:\r\n{1}\r\n";
						entryStr = "{0}\r\n";
						break;
				}

				string text = "";

				foreach(var pair in data.entryTypes) {
					string id = pair.Key;
					var info = pair.Value;
					var entries = data.entries.Where(e => e.type == id);

					if(entries.Count() == 0) {
						continue;
					}

					string listStr = "";

					foreach(var entry in entries) {
						listStr += string.Format(entryStr, entry.text);
					}

					text += string.Format(categoryStr, info.name, listStr);
				}

				if(string.IsNullOrWhiteSpace(text)) {
					await context.ReplyAsync("There's nothing in the changelog.");
					return;
				}

				string filename = $"changelog-output-{formatType.ToString().ToLower()}.txt";

				await File.WriteAllTextAsync(filename, text);
				await context.Channel.SendFileAsync(filename);
			}
			catch(Exception e) {
				await MopBot.HandleException(e);
			}
		}

		[Command("parse")]
		[RequirePermission(SpecialPermission.Owner, "changelog.entries.parse")]
		public async Task Parse(SocketTextChannel channel, SocketGuildUser fromUser = null, ulong? afterMessage = null)
		{
			var context = Context;
			var data = context.server.GetMemory().GetData<ChangelogSystem, ChangelogServerData>();
			var clChannel = await data.TryGetChangelogChannel(context);

			if(clChannel == null) {
				return;
			}

			const int Limit = 1000;

			ulong channelId = channel.Id;
			ulong userId = fromUser?.Id ?? 0;
			ulong thisUserId = context.Client.CurrentUser.Id;

			int numChecked = 0;
			int numGeneralFails = 0;
			int numOtherFails = 0;

			var regex = new Regex(@"([\S]+) - #([\d*`~]+) - ([^:]+):[*`~\s]+(.+)");

			string RemoveFormatting(string original) => original.Replace("~", "").Replace("*", "").Replace("`", "");

			var messages = afterMessage.HasValue ? channel.GetMessagesAsync(afterMessage.Value, Direction.After, Limit) : channel.GetMessagesAsync(Limit);

			await messages.ForEachAsync(async collection => {
				foreach(var msg in collection) {
					ulong authorId = msg.Author.Id;

					if(userId != 0 && authorId != userId) {
						continue;
					}

					numChecked++;

					var content = msg.Content;
					var match = regex.Match(content);

					if(!match.Success) {
						numGeneralFails++;

						if(numGeneralFails == 1) {
							await context.ReplyAsync($"`{content}`");
						}

						continue;
					}

					var groups = match.Groups;

					if(!uint.TryParse(RemoveFormatting(groups[2].Value), out uint entryId)) {
						numOtherFails++;

						continue;
					}

					string entryTypeName = RemoveFormatting(groups[3].Value);
					string entryText = groups[4].Value;

					if(!data.entryTypes.TryGetFirst(pair => MopBot.StrComparerIgnoreCase.Equals(pair.Value.name, entryTypeName), out var resultPair)) {
						throw new BotError($"Unknown entry type: `{entryTypeName}`.");
					}

					var entryTypeId = resultPair.Key;

					if(!data.entries.TryGetFirst(info => info.entryId == entryId, out var entry)) {
						data.entries.Add(entry = new ChangelogEntry(entryId, entryTypeId, entryText));
					} else {
						entry.type = entryTypeId;
						entry.text = entryText;
					}

					if(authorId == thisUserId) {
						entry.messageId = msg.Id;
						entry.channelId = channelId;
					} else {
						await data.PublishEntry(entry, clChannel);
					}
				}
			});

			await context.ReplyAsync($"Successfully parsed {numChecked - numGeneralFails - numOtherFails}/{numChecked} messages.\r\n{numGeneralFails} 'general' failures.\r\n{numOtherFails} 'other' failures.");
		}
	}
}
