using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Discord.WebSocket;
using Discord.Commands;
using MopBot.Extensions;
using MopBot.Core.Systems.Memory;

#pragma warning disable CS1998 //Async method lacks 'await' operators and will run synchronously

namespace MopBot.Core.Systems.Permissions
{
	public partial class PermissionSystem : BotSystem
	{
		//TODO: Too much repeating code around throws in commands below.

		[Command("listfull")] [Alias("viewfull","showfull","listall","viewall","showall")]
		[RequirePermission(SpecialPermission.Owner,"permissions.view")]
		public async Task ListFullCommand()
		{
			var data = MemorySystem.memory[Context.server].GetData<PermissionSystem,PermissionServerData>();

			if(data.permissionGroups.Count==0) {
				throw new BotError($"This server currently has no permission groups. Has something went wrong? This shouldn't ever be the case.");
			}

			var roles = Context.server.Roles;
			string listStr = "";

			foreach(var pair in data.permissionGroups) {
				string roleList = string.Join("\r\n",roles.SelectIgnoreNull(r => data.roleGroups.TryGetValue(r.Id,out string groupName) && groupName==pair.Key ? "\t\t"+r.Name.Replace("@","") : null));
				string roleStr = $"\tAssociated roles:\r\n{(roleList.Length==0 ? "\t\tNo role associations." : roleList)}";
				
				string permList = string.Join("\r\n",pair.Value.permissions.Where(p => p.Value!=null).Select(p => $"\t\t{(p.Value.Value ? "✓" : "✗")} - {p.Key}"));
				string permStr = $"\tPermission overrides:\r\n{(permList.Length==0 ? "\t\tNo permission overrides." : permList)}";

				listStr += $"{pair.Key}:\r\n{roleStr}\r\n{permStr}\r\n\r\n";
			}

			await Context.ReplyAsync($"Full permission setup: ```\r\n{listStr}```");
		}

		//Permission Groups

		[Command("listgroups")] [Alias("viewgroups","showgroups","listpermissiongroups","viewpermissiongroups","showpermissiongroups")]
		public async Task ListPermissionGroupsCommand()
		{
			var data = MemorySystem.memory[Context.server].GetData<PermissionSystem,PermissionServerData>();
			
			if(data.permissionGroups.Count==0) {
				throw new BotError($"This server currently has no permission groups. Has something went wrong? This shouldn't ever be the case.");
			}

			await Context.ReplyAsync($"This server has the following permission groups defined:```\r\n{string.Join("\r\n",data.permissionGroups.Keys)}```");
		}

		[Command("addgroup")] [Alias("creategroup","newgroup","addpermissiongroup","createpermissiongroup","newpermissiongroup")]
		public async Task AddPermissionGroupCommand(string permGroup)
		{
			var data = MemorySystem.memory[Context.server].GetData<PermissionSystem,PermissionServerData>();
			
			if(data.permissionGroups.ContainsKey(permGroup)) {
				throw new BotError($"Permission group `{permGroup}` already exists.");
			}
			
			data.permissionGroups.TryAdd(permGroup,new PermissionGroup(permGroup));
			
			await Context.ReplyAsync($"Created permission group `{permGroup}`.");
		}

		[Command("removegroup")] [Alias("deletegroup","delgroup","removepermissiongroup","deletepermissiongroup","delpermissiongroup")]
		public async Task RemovePermissionGroupCommand(string permGroup)
		{
			var data = MemorySystem.memory[Context.server].GetData<PermissionSystem,PermissionServerData>();
			
			if(!data.permissionGroups.ContainsKey(permGroup)) {
				throw new BotError($"Permission group `{permGroup}` doesn't exist.");
			}
			
			if(permGroup=="everyone") {
				throw new BotError($"`everyone` permission group cannot be removed.");
			}

			data.permissionGroups.TryRemove(permGroup,out _);
		}

		//Permission Groups' Roles

		[Command("setrolegroup")] [Alias("setrolepermissiongroup")]
		public async Task SetRolePermissionGroupCommand(SocketRole role,string permGroup)
		{
			string properName = role.Name.Replace("@","");

			if(properName=="everyone" && !role.IsEveryone) {
				role = Context.server.EveryoneRole;
			}

			var data = MemorySystem.memory[Context.server].GetData<PermissionSystem,PermissionServerData>();
			
			if(!data.permissionGroups.ContainsKey(permGroup)) {
				throw new BotError($"Permission group `{permGroup}` doesn't exists.");
			}

			if(role.Id==Context.server.EveryoneRole.Id) {
				throw new BotError($"Cannot reassign `everyone` role's group.");
			}

			if(permGroup=="everyone") {
				throw new BotError($"Cannot assign `everyone` permission group to any roles.");
			}

			data.roleGroups.TryGetValue(role.Id,out string prevGroup);
			data.roleGroups[role.Id] = permGroup;
		}

		[Command("getrolegroup")]
		public async Task GetRolePermissionGroupCommand(SocketRole role)
		{
			string properName = role.Name.Replace("@","");

			if(properName=="everyone" && !role.IsEveryone) {
				role = Context.server.EveryoneRole;
			}

			var data = MemorySystem.memory[Context.server].GetData<PermissionSystem,PermissionServerData>();

			if(!data.roleGroups.TryGetValue(role.Id,out string permGroup) || !data.permissionGroups.ContainsKey(permGroup)) {
				throw new BotError($"`{properName}` role currently isn't linked with any permission groups.");
			}

			await Context.ReplyAsync($"`{properName}` role is currently linked with permission group `{permGroup}`.");
		}

		//Permission Groups' Permissions

		[Command("list")]
		public async Task ListPermissionsCommand(string permGroup)
		{
			var data = MemorySystem.memory[Context.server].GetData<PermissionSystem,PermissionServerData>();

			if(!data.permissionGroups.TryGetValue(permGroup,out var group)) {
				throw new BotError($"Permission group `{permGroup}` does not exist.");
			}

			if(group.permissions.Count==0) {
				throw new BotError($"Permission group `{permGroup}` doesn't have any permission overrides defined.");
			}

			await Context.ReplyAsync($"`{permGroup}` permission group has the following permissions overrides:```\r\n{string.Join("\r\n",group.permissions.Where(p => p.Value!=null).Select(p => (p.Value.Value ? "✓ - " : "✗ - ")+p.Key))}```");
		}

		[Command("set")]
		public async Task SetPermissionCommand(string permGroup,string permission,bool? value)
		{
			var data = MemorySystem.memory[Context.server].GetData<PermissionSystem,PermissionServerData>();

			if(!data.permissionGroups.TryGetValue(permGroup,out var perms)) {
				throw new BotError($"Permission group `{permGroup}` does not exist.");
			}

			perms[permission] = value;
		}

		[Command("setmultiple")] [Alias("setmany")]
		public async Task SetPermissionsCommand(string permGroup,bool? value,params string[] permissions)
		{
			var data = MemorySystem.memory[Context.server].GetData<PermissionSystem,PermissionServerData>();

			if(!data.permissionGroups.TryGetValue(permGroup,out var perms)) {
				throw new BotError($"Permission group `{permGroup}` does not exist.");
			}

			for(int i = 0;i<permissions.Length;i++) {
				perms[permissions[i]] = value;
			}
		}

		[Command("remove")] [Alias("delete","del")]
		public async Task RemovePermissionsCommand(string permGroup,params string[] permissions)
		{
			var data = MemorySystem.memory[Context.server].GetData<PermissionSystem,PermissionServerData>();

			if(!data.permissionGroups.TryGetValue(permGroup,out var perms)) {
				throw new BotError($"Permission group `{permGroup}` does not exist.");
			}

			var removed = new List<string>();
			var skipped = new List<string>();

			for(int i = 0;i<permissions.Length;i++) {
				var perm = permissions[i];

				if(perms.permissions.ContainsKey(perm)) {
					perms[perm] = null;

					removed.Add(perm);
				} else {
					skipped.Add(perm);
				}
			}

			string text = "";

			if(removed.Count>0) {
				text += $"Removed the following permissions: ```\r\n{string.Join("\r\n",removed)}```\r\n";
			}

			if(skipped.Count>0) {
				text += $"The following permissions already weren't present: ```\r\n{string.Join("\r\n",skipped)}```\r\n";
			}

			await Context.ReplyAsync(text);
		}
	}
}
