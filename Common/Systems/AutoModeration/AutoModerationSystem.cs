using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using MopBotTwo.Core;
using MopBotTwo.Core.Systems;
using MopBotTwo.Core.Systems.Memory;
using MopBotTwo.Core.Systems.Permissions;
using MopBotTwo.Extensions;

namespace MopBotTwo.Common.Systems.AutoModeration
{
	[Group("automoderation")]
	[Alias("automod")]
	[Summary("Group for controlling AutoModerationSystem")]
	[SystemConfiguration(Description = "Automatically does moderation through various message filtering in a configurable way.")]
	[RequirePermission(SpecialPermission.Owner,"automod")]
	public class AutoModerationSystem : BotSystem
	{
		public override void RegisterDataTypes()
		{
			RegisterDataType<ServerMemory,AutoModerationServerData>();
		}
		public override async Task OnMessageReceived(MessageExt context)
		{
			await CheckMessagePings(context);
		}
		public override async Task<bool> Update()
		{
			foreach(var server in MopBot.client.Guilds) {
				var data = server.GetMemory().GetData<AutoModerationSystem,AutoModerationServerData>();

				if(data.mentionSpamAction==ModerationAction.None || data.userPingCounters==null || data.userPingCounters.Count<=0) {
					continue;
				}

				List<ulong> keysToRemove = null;

				foreach(var pair in data.userPingCounters) {
					var list = pair.Value;

					lock(list) {
						for(int i = 0;i<list.Count;i++) {
							if(--list[i]==0) {
								list.RemoveAt(i--);
							}
						}

						if(list.Count==0) {
							(keysToRemove ?? (keysToRemove = new List<ulong>())).Add(pair.Key);
						}
					}
				}

				if(keysToRemove!=null) {
					for(int i = 0;i<keysToRemove.Count;i++) {
						data.userPingCounters.TryRemove(keysToRemove[i],out _);
					}
				}
			}

			return true;
		}

		private async Task ExecuteAction(MessageExt context,ModerationAction action,string reason,SocketGuildUser user = null)
		{
			user ??= context.socketServerUser ?? throw new ArgumentNullException($"Both {nameof(user)} and {nameof(context)}.{nameof(MessageExt.socketServerUser)} are null.");

			var embedBuilder = MopBot.GetEmbedBuilder(user.Guild)
				.WithAuthor(user)
				.WithDescription($"**Reason:** `{reason}`");

			bool RequirePermission(DiscordPermission discordPermission)
			{
				if(!context.server.CurrentUser.HasChannelPermission(context.socketServerChannel,DiscordPermission.BanMembers)) {
					action = ModerationAction.Announce;

					embedBuilder.Title = $"{embedBuilder.Title}\r\n**Attempted to execute action '{action}', but the following permission was missing: `{DiscordPermission.BanMembers}`.";

					return false;
				}

				return true;
			}

			switch(action) {
				case ModerationAction.Kick:
					if(RequirePermission(DiscordPermission.KickMembers)) {
						await user.KickAsync(reason:reason);

						embedBuilder.Title = "User auto-kicked";
					}

					break;
				case ModerationAction.Ban:
					if(RequirePermission(DiscordPermission.KickMembers)) {
						await user.BanAsync(reason: reason);

						embedBuilder.Title = "User auto-banned";
					}
					break;
			}

			if(action==ModerationAction.Announce) {
				embedBuilder.Title = "User violation detected";
			}

			var data = context.server.GetMemory().GetData<AutoModerationSystem,AutoModerationServerData>();

			await context.socketTextChannel.SendMessageAsync(data.announcementPrefix,embed:embedBuilder.Build());
		}
		private async Task CheckMessagePings(MessageExt context)
		{
			if(context.socketServerUser.Id==context.server.OwnerId || context.socketServerUser.HasAnyPermissions("automod.immune","automod.pingban.immune")) {
				return;
			}

			int numMentions = context.Message.MentionedUserIds.Count;

			if(numMentions==0) {
				return;
			}

			var data = context.server.GetMemory().GetData<AutoModerationSystem,AutoModerationServerData>();

			if(data.mentionSpamAction==ModerationAction.None || data.minMentionsForAction==0) {
				return;
			}

			if(!data.userPingCounters.TryGetValue(context.user.Id,out var pingCounter)) {
				data.userPingCounters[context.user.Id] = pingCounter = new List<byte>();
			}

			int oldPingCount,newPingCount;

			lock(pingCounter) {
				oldPingCount = pingCounter.Count;
				newPingCount = oldPingCount+numMentions;

				if(newPingCount<data.minMentionsForAction) {
					Console.WriteLine($"Recording {numMentions} pings.");

					pingCounter.AddRange(Enumerable.Repeat(data.mentionCooldown,numMentions));
					return;
				}

				pingCounter.Clear();
				data.userPingCounters.TryRemove(context.user.Id,out _);
			}

			await ExecuteAction(context,data.mentionSpamAction,$"Exceeding maximum of {data.minMentionsForAction} user mentions in {data.mentionCooldown} seconds with {newPingCount} mentions.");
		}

		[Command("prefix")]
		[Summary("Lets you define what comes before any announcement of automatic moderation actions. You can use this to make the bot mention roles or specific users, like admins.")]
		public async Task PrefixCommand([Remainder]string prefix = null)
		{
			Context.server.GetMemory().GetData<AutoModerationSystem,AutoModerationServerData>().announcementPrefix = prefix;
		}

		[Command("mentionspam")]
		[Summary("Sets the moderation action (none/announce/kick/ban) that should be taken onto the user who does mention-spam.")]
		public async Task MentionSpamSetupCommand(ModerationAction moderationAction)
		{
			var data = Context.server.GetMemory().GetData<AutoModerationSystem,AutoModerationServerData>();

			data.mentionSpamAction = moderationAction;
		}

		[Command("mentionspam")]
		[Summary("Setups mention-spam moderation with the action (none/announce/kick/ban) and X minimal amount of pings in Y seconds needed to execute an action onto an user.")]
		public async Task MentionSpamSetupCommand(ModerationAction moderationAction,uint minPingsForBan,byte pingCooldownInSeconds)
		{
			if(minPingsForBan<3) {
				throw new BotError("For safety reasons, minimal amount of pings for a ban must be at least `3`.");
			}

			if(pingCooldownInSeconds<=0) {
				throw new BotError("Ping cooldown can't be `0` seconds.");
			}

			var data = Context.server.GetMemory().GetData<AutoModerationSystem,AutoModerationServerData>();

			data.mentionSpamAction = moderationAction;
			data.minMentionsForAction = minPingsForBan;
			data.mentionCooldown = pingCooldownInSeconds;
		}
	}
}
