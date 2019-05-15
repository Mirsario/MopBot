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
	//TODO: old. improv.

	public partial class CustomRoleSystem
	{
		[Command("set")]
		[RequirePermission("customrole.manage")]
		public async Task SetCustomRoleCommand(byte red,byte green,byte blue,[Remainder]string roleName)
		{
			await SetCustomRole(Context.server,Context.socketServerUser,new Discord.Color(red,green,blue),roleName,Context);
		}

		[Command("remove")]
		[RequirePermission("customrole.manage")]
		public async Task RemoveCustomRoleCommand()
		{
			var userMemory = MemorySystem.memory[Context.server][Context.user].GetData<CustomRoleSystem,CustomRoleServerUserData>();
			if(userMemory.colorRole==null) {
				await Context.ReplyAsync("You don't have a custom role set.");
				return;
			}

			if(!Context.socketServerUser.Roles.TryGetFirst(r => r.Id==userMemory.colorRole.Value,out var role)) {
				await Context.ReplyAsync("You don't have a custom role set.");
				return;
			}
			try {
				await Context.socketServerUser.RemoveRoleAsync(role);
				await Context.ReplyAsync("Removed role.");
			}
			catch(HttpException e) {
				if(e.DiscordCode==403) {
					await Context.ReplyAsync("An error has occured: Bot doesn't have enough permissions.");
				} else {
					throw;
				}
			}
		}

		[Command("detect")]
		[RequirePermission("customrole.admin")]
		public async Task DetectCustomRolesCommand()
		{
			var server = Context.server;
			if(server==null) {
				return;
			}
			string text = "";
			string unused = "";
			var serverMemory = MemorySystem.memory[server];

			foreach(var role in server.Roles) {
				if(role.IsEveryone) {
					continue;
				}

				var members = role.Members.ToArray();
				if(members.Length==1) {
					var user = members[0];
					var customRoleUserData = serverMemory[user].GetData<CustomRoleSystem,CustomRoleServerUserData>();
					if(customRoleUserData.colorRole!=null) {
						continue;
					}

					if(user.Roles.OrderByDescending(r => r.Position).First().Id==role.Id) {
						customRoleUserData.colorRole = role.Id;
						string newText = $"Detected {user.Name()}'s custom role to be ''{role.Name}''.\n";
						if(text.Length+newText.Length>=2000) {
							await Context.ReplyAsync(text,false);
							text = "";
						}
						text += newText;
					}
				} else if(members.Length==0) {
					unused += $"{role.Name} is unused.\n";
					//if(arguments.Length>0 && arguments[0]=="deleteunused") {
					//	await role.DeleteAsync();
					//}
				}
			}

			await Context.ReplyAsync(text,false);
			await Context.ReplyAsync(unused,false);
			await Context.ReplyAsync("Done.");
		}
	}
}