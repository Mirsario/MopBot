using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;
using MopBot.Extensions;
using MopBot.Core.Systems.Permissions;
using MopBot.Core.Systems.Memory;
using System;
using Discord;

namespace MopBot.Common.Systems.CustomRoles
{
	//TODO: Old. To be re-reviewed.

	public partial class CustomRoleSystem
	{
		[Command("set")]
		[RequirePermission(SpecialPermission.Admin, "customrole.manage")]
		public async Task SetCustomRoleCommand(byte red, byte green, byte blue, [Remainder] string roleName)
		{
			var server = Context.server;
			var user = Context.socketServerUser;

			server.CurrentUser.RequirePermission(Context.socketServerChannel, DiscordPermission.ManageRoles);

			int topPos = -1;

			foreach (var tempRole in user.Roles) {
				if (!tempRole.IsEveryone) {
					topPos = topPos == -1 ? tempRole.Position : Math.Max(topPos, tempRole.Position);
				}
			}

			IRole role;
			var customRoleUserData = MemorySystem.memory[server][user].GetData<CustomRoleSystem, CustomRoleServerUserData>();
			var color = new Color(red, green, blue);

			if (customRoleUserData.colorRole != null && (role = server.GetRole(customRoleUserData.colorRole.Value)) != null) {
				await role.ModifyAsync(properties => {
					properties.Color = color;
					properties.Name = roleName;
				});
			} else {
				var tempRole = await server.CreateRoleAsync(roleName, color: color, isMentionable: false);

				await server.ReorderRolesAsync(new[] { new ReorderRoleProperties(tempRole.Id, topPos + 1) });
				await user.AddRoleAsync(tempRole);

				customRoleUserData.colorRole = tempRole.Id;
			}

			await Context.ReplyAsync("Role set! :ok_hand:");
		}

		[Command("remove")]
		[RequirePermission(SpecialPermission.Admin, "customrole.manage")]
		public async Task RemoveCustomRoleCommand()
		{
			var server = Context.server;

			server.CurrentUser.RequirePermission(Context.socketServerChannel, DiscordPermission.ManageRoles);

			var user = Context.socketServerUser;
			var userMemory = MemorySystem.memory[server][user].GetData<CustomRoleSystem, CustomRoleServerUserData>();

			if (userMemory.colorRole == null || !user.Roles.TryGetFirst(r => r.Id == userMemory.colorRole.Value, out var role)) {
				throw new BotError("You don't have a custom role set.");
			}

			await user.RemoveRoleAsync(role);

			await Context.ReplyAsync("Removed role.");
		}

		[Command("detect")]
		[RequirePermission(SpecialPermission.Admin, "customrole.admin")]
		public async Task DetectCustomRolesCommand()
		{
			var server = Context.server;

			if (server == null) {
				return;
			}

			string text = "";
			string unused = "";
			var serverMemory = MemorySystem.memory[server];

			foreach (var role in server.Roles) {
				if (role.IsEveryone) {
					continue;
				}

				var members = role.Members.ToArray();

				if (members.Length == 0) {
					unused += $"{role.Name} is unused.\r\n";

					continue;
				}

				if (members.Length == 1) {
					var user = members[0];
					var customRoleUserData = serverMemory[user].GetData<CustomRoleSystem, CustomRoleServerUserData>();

					if (customRoleUserData.colorRole != null) {
						continue;
					}

					if (user.Roles.OrderByDescending(r => r.Position).First().Id == role.Id) {
						customRoleUserData.colorRole = role.Id;

						string newText = $"Detected {user.GetDisplayName()}'s custom role to be ''{role.Name}''.\r\n";

						if (text.Length + newText.Length >= 2000) {
							await Context.ReplyAsync(text, false);

							text = "";
						}

						text += newText;
					}
				}
			}

			await Context.ReplyAsync(text, false);
			await Context.ReplyAsync(unused, false);
			await Context.ReplyAsync("Done.");
		}
	}
}
