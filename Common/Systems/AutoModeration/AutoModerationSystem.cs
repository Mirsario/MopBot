using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using MopBot.Core;
using MopBot.Core.Systems;
using MopBot.Core.Systems.Memory;
using MopBot.Core.Systems.Permissions;
using MopBot.Extensions;

#pragma warning disable CS1998 //Async method lacks 'await' operators and will run synchronously

namespace MopBot.Common.Systems.AutoModeration
{
	[SystemConfiguration(Description = "Automatically does moderation through various message filtering in a configurable way.")]
	[RequirePermission(SpecialPermission.Admin, "automod")]
	public partial class AutoModerationSystem : BotSystem
	{
		public override void RegisterDataTypes()
		{
			RegisterDataType<ServerMemory, AutoModerationServerData>();
		}
		public override async Task OnMessageReceived(MessageContext context)
		{
			await CheckMessagePings(context);
		}
		public override async Task<bool> Update()
		{
			foreach(var server in MopBot.client.Guilds) {
				var data = server.GetMemory().GetData<AutoModerationSystem, AutoModerationServerData>();

				if(data.mentionSpamPunishment == ModerationPunishment.None || data.userPingCounters == null || data.userPingCounters.Count <= 0) {
					continue;
				}

				List<ulong> keysToRemove = null;

				foreach(var pair in data.userPingCounters) {
					var list = pair.Value;

					lock(list) {
						for(int i = 0; i < list.Count; i++) {
							if(--list[i] == 0) {
								list.RemoveAt(i--);
							}
						}

						if(list.Count == 0) {
							(keysToRemove ??= new List<ulong>()).Add(pair.Key);
						}
					}
				}

				if(keysToRemove != null) {
					for(int i = 0; i < keysToRemove.Count; i++) {
						data.userPingCounters.TryRemove(keysToRemove[i], out _);
					}
				}
			}

			return true;
		}

		private async Task ExecuteAction(MessageContext context, ModerationPunishment action, string reason, SocketGuildUser user = null)
		{
			user ??= context.socketServerUser ?? throw new ArgumentNullException($"Both {nameof(user)} and {nameof(context)}.{nameof(MessageContext.socketServerUser)} are null.");

			var embedBuilder = MopBot.GetEmbedBuilder(user.Guild)
				.WithAuthor(user)
				.WithDescription($"**Reason:** `{reason}`");

			bool RequirePermission(DiscordPermission discordPermission)
			{
				if(!context.server.CurrentUser.HasChannelPermission(context.socketServerChannel, DiscordPermission.BanMembers)) {
					action = ModerationPunishment.Announce;

					embedBuilder.Title = $"{embedBuilder.Title}\r\n**Attempted to execute action '{action}', but the following permission was missing: `{DiscordPermission.BanMembers}`.";

					return false;
				}

				return true;
			}

			switch(action) {
				case ModerationPunishment.Kick:
					if(RequirePermission(DiscordPermission.KickMembers)) {
						await user.KickAsync(reason: reason);

						embedBuilder.Title = "User auto-kicked";
					}

					break;
				case ModerationPunishment.Ban:
					if(RequirePermission(DiscordPermission.KickMembers)) {
						await user.BanAsync(reason: reason);

						embedBuilder.Title = "User auto-banned";
					}

					break;
			}

			if(action == ModerationPunishment.Announce) {
				embedBuilder.Title = "User violation detected";
			}

			var data = context.server.GetMemory().GetData<AutoModerationSystem, AutoModerationServerData>();

			await context.socketTextChannel.SendMessageAsync(data.announcementPrefix, embed: embedBuilder.Build());
		}
		private async Task CheckMessagePings(MessageContext context)
		{
			if(context.message is not IUserMessage) {
				return;
			}

			if(context.socketServerUser.Id == context.server.OwnerId || context.socketServerUser.HasAnyPermissions("automod.immune", "automod.pingban.immune")) {
				return;
			}

			int numMentions = context.Message.MentionedUserIds.Count;

			if(numMentions == 0) {
				return;
			}

			var data = context.server.GetMemory().GetData<AutoModerationSystem, AutoModerationServerData>();

			if(data.mentionSpamPunishment == ModerationPunishment.None || data.minMentionsForAction == 0) {
				return;
			}

			if(!data.userPingCounters.TryGetValue(context.user.Id, out var pingCounter)) {
				data.userPingCounters[context.user.Id] = pingCounter = new List<byte>();
			}

			int oldPingCount, newPingCount;

			lock(pingCounter) {
				oldPingCount = pingCounter.Count;
				newPingCount = oldPingCount + numMentions;

				if(newPingCount < data.minMentionsForAction) {
					pingCounter.AddRange(Enumerable.Repeat(data.mentionCooldown, numMentions));
					return;
				}

				pingCounter.Clear();
				data.userPingCounters.TryRemove(context.user.Id, out _);
			}

			await ExecuteAction(context, data.mentionSpamPunishment, $"Exceeding maximum of {data.minMentionsForAction} user mentions in {data.mentionCooldown} seconds with {newPingCount} mentions.");
		}
	}
}
