﻿using System.Threading.Tasks;
using Discord.WebSocket;
using Discord.Commands;
using MopBot.Extensions;
using MopBot.Core.Systems;
using MopBot.Core.Systems.Permissions;
using Discord;

namespace MopBot.Common.Systems.Roles
{
	[Group("roles")]
	[Alias("role")]
	[Summary("Group for commands for giving, taking, joining and mentioning roles.")]
	[SystemConfiguration(Description = "Contains commands for giving, taking, and mentioning roles. It's also possible to let users join selected roles on their own, with per-role permissions.")]
	public class RoleSystem : BotSystem
	{
		//TODO: Add discord permission checks before give/remove role calls, instead of just trycatching these calls

		//User stuff

		[Command("join")]
		[RequirePermission(SpecialPermission.Admin, "roles.join")]
		public async Task JoinRoleCommand([Remainder] SocketRole role)
		{
			var context = Context;
			var user = context.socketServerUser;
			string permission = $"roles.join.id.{role.Id}";

			user.RequirePermission(permission);
			context.server.CurrentUser.RequirePermission(context.socketServerChannel, DiscordPermission.ManageRoles);

			if (user.HasRole(role)) {
				throw new BotError("You already have that role.");
			}

			await user.AddRoleAsync(role);
		}

		[Command("leave")]
		[RequirePermission(SpecialPermission.Admin, "roles.leave")]
		public async Task LeaveRoleCommand([Remainder] SocketRole role)
		{
			var context = Context;
			var user = context.socketServerUser;
			string permission = $"roles.leave.id.{role.Id}";

			user.RequirePermission(permission);
			context.server.CurrentUser.RequirePermission(context.socketServerChannel, DiscordPermission.ManageRoles);

			if (!user.HasRole(role)) {
				throw new BotError("You don't have that role.");
			}

			await user.RemoveRoleAsync(role);
		}

		//Admin stuff

		[Command("give")]
		[RequirePermission(SpecialPermission.Admin, "roles.give")]
		public async Task GiveRoleCommand(SocketGuildUser user, [Remainder] SocketRole role)
		{
			var context = Context;
			context.server.CurrentUser.RequirePermission(context.socketServerChannel, DiscordPermission.ManageRoles);

			if (user.HasRole(role)) {
				throw new BotError("User already has that role.");
			}

			await user.AddRoleAsync(role);
		}

		[Command("take")]
		[RequirePermission(SpecialPermission.Admin, "roles.take")]
		public async Task TakeRoleCommand(SocketGuildUser user, [Remainder] SocketRole role)
		{
			var context = Context;
			context.server.CurrentUser.RequirePermission(context.socketServerChannel, DiscordPermission.ManageRoles);

			if (!user.HasRole(role)) {
				throw new BotError("User doesn't have that role.");
			}

			await user.RemoveRoleAsync(role);
		}

		[Command("mentionrole")]
		[Alias("pingrole", "mention")]
		[RequirePermission(SpecialPermission.Admin, "mentionroles")]
		public async Task MentionRoleCommand(IRole role)
		{
			bool wasMentionable = role.IsMentionable;

			if (!wasMentionable) {
				await role.ModifyAsync(rp => rp.Mentionable = true);
			}

			await Context.Channel.SendMessageAsync(role.Mention);

			if (!wasMentionable) {
				await role.ModifyAsync(rp => rp.Mentionable = false);
			}

			await Context.Delete();
		}
	}
}
