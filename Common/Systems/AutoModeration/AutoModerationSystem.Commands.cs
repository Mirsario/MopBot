using System.Threading.Tasks;
using Discord.Commands;
using MopBot.Extensions;

#pragma warning disable CS1998 //Async method lacks 'await' operators and will run synchronously

namespace MopBot.Common.Systems.AutoModeration
{
	[Group("automoderation")]
	[Alias("automod")]
	[Summary("Group for controlling Automatic Moderation")]
	partial class AutoModerationSystem
	{
		[Command("prefix")]
		[Summary("Lets you define what comes before any announcement of automatic moderation actions. You can use this to make the bot mention roles or specific users, like admins.")]
		public async Task PrefixCommand([Remainder] string prefix = null)
		{
			Context.server.GetMemory().GetData<AutoModerationSystem, AutoModerationServerData>().announcementPrefix = prefix;
		}

		[Command("mentionspam")]
		[Summary("Sets the moderation punishment (none/announce/kick/ban) that should be taken onto the user who does mention-spam.")]
		public async Task MentionSpamSetupCommand(ModerationPunishment moderationAction)
		{
			var data = Context.server.GetMemory().GetData<AutoModerationSystem, AutoModerationServerData>();

			data.mentionSpamPunishment = moderationAction;
		}

		[Command("mentionspam")]
		[Summary("Setups mention-spam moderation with the punishment (none/announce/kick/ban) and X minimal amount of pings in Y seconds needed to use it upon an user.")]
		public async Task MentionSpamSetupCommand(ModerationPunishment moderationPunishment, uint minPingsForBan, byte pingCooldownInSeconds)
		{
			if(minPingsForBan < 3) {
				throw new BotError("For safety reasons, minimal amount of pings for a punishment must be at least `3`.");
			}

			if(pingCooldownInSeconds <= 0) {
				throw new BotError("Ping cooldown can't be `0` seconds.");
			}

			var data = Context.server.GetMemory().GetData<AutoModerationSystem, AutoModerationServerData>();

			data.mentionSpamPunishment = moderationPunishment;
			data.minMentionsForAction = minPingsForBan;
			data.mentionCooldown = pingCooldownInSeconds;
		}
	}
}
