using System;
using System.Threading.Tasks;
using Discord.WebSocket;
using Discord.Commands;
using MopBotTwo.Extensions;

namespace MopBotTwo.Systems
{
	[Group("roles")] [Alias("role")]
	public class RoleSystem : BotSystem
	{
		//TODO: Add discord permission checks before give/remove role calls, instead of just trycatching these calls

		//User stuff
		[Command("join")]
		[RequirePermission("roles.join")]
		public async Task JoinRoleCommand([Remainder]SocketRole role)
		{
			var user = Context.socketServerUser;
			string permission = $"roles.join.id.{role.Id}";

			user.RequirePermission(permission);

			try {
				await user.AddRoleAsync(role);
			}
			catch(Exception e) {
				throw new BotError(e);
			}
		}
		[Command("leave")]
		[RequirePermission("roles.leave")]
		public async Task LeaveRoleCommand([Remainder]SocketRole role)
		{
			var user = Context.socketServerUser;
			string permission = $"roles.leave.id.{role.Id}";

			user.RequirePermission(permission);

			if(!user.HasRole(role)) {
				throw new BotError("You don't have that role.");
			}

			try {
				await user.RemoveRoleAsync(role);
			}
			catch(Exception e) {
				throw new BotError(e);
			}
		}

		//Admin stuff
		[Command("give")] [Alias("grant")]
		[RequirePermission("roles.give")]
		public async Task GiveRoleCommand(SocketGuildUser user,[Remainder]SocketRole role)
		{
			try {
				await user.AddRoleAsync(role);
			}
			catch(Exception e) {
				throw new BotError(e);
			}
		}
	}
}