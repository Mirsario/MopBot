using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Net;
using Discord.WebSocket;
using Discord.Commands;
using MopBotTwo.Extensions;

#pragma warning disable CS1998

namespace MopBotTwo.Systems
{
	[RequirePermission("customrole")]
	[Group("customrole")] [Alias("colorrole")]
	[Summary("Lets you manage your unique color role.")]
	[SystemConfiguration(Description = "Lets 'VIP' users make unique roles for themselves.")]
	public partial class CustomRoleSystem : BotSystem
	{
		public override void RegisterDataTypes()
		{
			RegisterDataType<ServerUserMemory,CustomRoleServerUserData>();
		}
		public override async Task Initialize()
		{
			PermissionSystem.defaultGroups["vip"].permissions["managecustomrole"] = true;
		}

		public static async Task SetCustomRole(SocketGuild server,SocketGuildUser user,Discord.Color color,string roleName,MessageExt message = null)
		{
			var userMemory = MemorySystem.memory[server][user];

			int topPos = -1;
			foreach(var tempRole in user.Roles) {
				if(!tempRole.IsEveryone) {
					topPos = topPos==-1 ? tempRole.Position : Math.Max(topPos,tempRole.Position);
				}
			}

			IRole role;
			var customRoleUserData = userMemory.GetData<CustomRoleSystem,CustomRoleServerUserData>();
			if(customRoleUserData.colorRole!=null && (role = server.GetRole(customRoleUserData.colorRole.Value))!=null) {
				await role.ModifyAsync(properties => {
					properties.Color = color;
					properties.Name = roleName;
				});
			} else {
				var tempRole = await server.CreateRoleAsync(roleName,null,color);
				await server.ReorderRolesAsync(new[] { new ReorderRoleProperties(tempRole.Id,topPos+1) });
				await user.AddRoleAsync(tempRole);
				customRoleUserData.colorRole = tempRole.Id;
			}

			await message.ReplyAsync("Role set! :ok_hand:");
		}
	}
}