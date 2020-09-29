using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Discord.Commands;
using MopBot.Extensions;
using MopBot.Core.Systems;
using MopBot.Core.Systems.Permissions;

namespace MopBot.Common.Systems.Moderation
{
	[Group("mod")]
	[Summary("Group for simple moderation commands, like `kick`, `ban` and `clear`.")]
	[SystemConfiguration(Description = "This system adds simple moderation commands, like `kick`, `ban` and `clear`.")]
	[RequirePermission(SpecialPermission.Owner, "moderation")]
	public class ModerationSystem : BotSystem
	{
		[Command("ban")]
		[Summary("Permamently bans a specified user.")]
		[RequirePermission(SpecialPermission.Owner, "moderation.ban")]
		public async Task BanCommand(IUser targetUser, [Remainder] string reason = "No reason provided.")
		{
			var context = Context;
			var user = context.socketServerUser;

			context.server.CurrentUser.RequirePermission(context.socketServerChannel, DiscordPermission.BanMembers);

			if(targetUser is SocketGuildUser serverUser && user.Roles.Max(r => r.Position) > serverUser.Roles.Max(r => r.Position)) {
				throw new BotError("You cannot ban a user who's above you in rights.");
			}

			await context.server.AddBanAsync(user, reason: reason);
		}

		[Command("kick")]
		[Summary("Kicks a specified user.")]
		[RequirePermission(SpecialPermission.Owner, "moderation.kick")]
		public async Task KickCommand(SocketGuildUser targetUser, [Remainder] string reason = "No reason provided.")
		{
			var context = Context;
			var user = context.socketServerUser;

			context.server.CurrentUser.RequirePermission(context.socketServerChannel, DiscordPermission.KickMembers);

			if(user.Roles.Max(r => r.Position) > targetUser.Roles.Max(r => r.Position)) {
				throw new BotError("You cannot kick a user who's above you in rights.");
			}

			await targetUser.KickAsync(reason: reason);
		}

		[Command("clear")]
		[Summary("Removes a specified amount of messages.")]
		[RequirePermission(SpecialPermission.Owner, "moderation.clear")]
		public Task ClearCommand(uint amount) => ClearCommand(Context.socketTextChannel, amount);

		[Command("clear")]
		[Summary("Removes a specified amount of messages.")]
		[RequirePermission(SpecialPermission.Owner, "moderation.clear")]
		public async Task ClearCommand(SocketTextChannel channel, uint amount)
		{
			var context = Context;
			var server = context.server;

			server.CurrentUser.RequirePermission(channel, DiscordPermission.ManageMessages);

			int highestRole = server.GetUser(MopBot.client.CurrentUser.Id).Roles.Max(r => r.Position);

			var utcNow = DateTime.UtcNow.AddMinutes(1); //+1 min
			var messages = (await channel.GetMessagesAsync((int)amount + 1).FlattenAsync())
				.Where(m => m != null && (m.Author?.Id == MopBot.client.CurrentUser.Id || ((utcNow - m.Timestamp.UtcDateTime).TotalDays < 14 && (m.Author as SocketGuildUser)?.Roles?.All(r => r.Position < highestRole) == true)));

			MessageSystem.IgnoreMessage(context.message.Id);

			await channel.DeleteMessagesAsync(messages);
		}
	}
}