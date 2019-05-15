using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;
using Discord.WebSocket;
using Discord.Commands;
using MopBotTwo.Extensions;

#pragma warning disable CS1998

namespace MopBotTwo.Systems
{
	[Group("permission")] [Alias("permissions","perms","perm")]
	[Summary("Group for configuring permissions.")]
	[RequirePermission(SpecialPermission.Owner,"modifypermissions")]
	[SystemConfiguration(AlwaysEnabled = true)]
	public class PermissionSystem : BotSystem
	{
		public class PermissionGroup : ICloneable
		{
			public string name;
			public Dictionary<string,bool?> permissions;

			public bool? this[string permission] {
				get => permissions.TryGetValue(permission,out var result) ? result : null;
				set => permissions[permission] = value;
			}

			private PermissionGroup() {}

			public PermissionGroup(string name)
			{
				this.name = name;
				permissions = new Dictionary<string,bool?>();
			}

			object ICloneable.Clone() => Clone();
			public PermissionGroup Clone()
			{
				var result = new PermissionGroup {
					name = (string)name.Clone(),
					permissions = new Dictionary<string,bool?>()
				};
				foreach(var pair in permissions) {
					result.permissions.Add(pair.Key,pair.Value);
				}
				return result;
			}
		}
		public class PermissionServerData : ServerData
		{
			public Dictionary<ulong,string> roleGroups;
			public ConcurrentDictionary<string,PermissionGroup> permissionGroups;

			public override void Initialize(SocketGuild server)
			{
				//permissionGroups
				permissionGroups = new ConcurrentDictionary<string,PermissionGroup>();
				foreach(var defGroup in defaultGroups) {
					permissionGroups[defGroup.Key] = defGroup.Value.Clone();
				}

				//rolePermissions
				roleGroups = new Dictionary<ulong,string>();
				if(server?.EveryoneRole!=null) {
					roleGroups[server.EveryoneRole.Id] = "everyone";
				}
			}
		}
		
		public static Dictionary<string,PermissionGroup> defaultGroups = new Dictionary<string,PermissionGroup>() {
			{ "everyone",	new PermissionGroup("everyone") },
			{ "vip",		new PermissionGroup("vip") },
			{ "admin",		new PermissionGroup("admin") },
			{ "superadmin",	new PermissionGroup("superadmin") }
		};

		public override void RegisterDataTypes()
		{
			RegisterDataType<ServerMemory,PermissionServerData>();
		}
		public override async Task Initialize()
		{
			defaultGroups["superadmin"]["modifypermissions"] = true;
		}

		//TODO: Too much repeating code around throws in commands below.

		[Command("listfull")]
		[Alias("viewfull","showfull","fulllist","listgroups full","viewgroups full","showgroups full")]
		public async Task ListFullCommand()
		{
			var data = MemorySystem.memory[Context.server].GetData<PermissionSystem,PermissionServerData>();
			if(data.permissionGroups.Count==0) {
				throw new BotError($"This server currently has no permission groups. Has something went wrong? This shouldn't ever be the case.");
			}

			var roles = Context.server.Roles;
			string listStr = "";
			foreach(var pair in data.permissionGroups) {
				string roleList = string.Join("\n",roles.SelectIgnoreNull(r => data.roleGroups.TryGetValue(r.Id,out string groupName) && groupName==pair.Key ? "\t\t"+r.Name.Replace("@","") : null));
				string roleStr = $"\tAssociated roles:\n{(roleList.Length==0 ? "\t\tNo role associations." : roleList)}";
				string permList = string.Join("\n",pair.Value.permissions.Where(p => p.Value!=null).Select(p => $"\t\t{(p.Value.Value ? "✓" : "✗")} - {p.Key}"));
				string permStr = $"\tPermission overrides:\n{(permList.Length==0 ? "\t\tNo permission overrides." : permList)}";
				listStr += $"{pair.Key}:\n{roleStr}\n{permStr}\n\n";
			}
			await Context.ReplyAsync($"Full permission setup: ```\n{listStr}```");
		}

		#region PermissionGroup
		[Command("listgroups")]
		[Alias("viewgroups","showgroups","listpermissiongroups","viewpermissiongroups","showpermissiongroups")]
		public async Task ListPermissionGroupsCommand()
		{
			var data = MemorySystem.memory[Context.server].GetData<PermissionSystem,PermissionServerData>();
			if(data.permissionGroups.Count==0) {
				throw new BotError($"This server currently has no permission groups. Has something went wrong? This shouldn't ever be the case.");
			}
			await Context.ReplyAsync($"This server has the following permission groups defined:```\n{string.Join("\n",data.permissionGroups.Keys)}```");
		}
		[Command("addgroup")]
		[Alias("creategroup","newgroup","addpermissiongroup","createpermissiongroup","newpermissiongroup")]
		public async Task AddPermissionGroupCommand(string permGroup)
		{
			var data = MemorySystem.memory[Context.server].GetData<PermissionSystem,PermissionServerData>();
			if(data.permissionGroups.ContainsKey(permGroup)) {
				throw new BotError($"Permission group `{permGroup}` already exists.");
			}
			data.permissionGroups.TryAdd(permGroup,new PermissionGroup(permGroup));
			await Context.ReplyAsync($"Created permission group `{permGroup}`.");
		}
		[Command("removegroup")]
		[Alias("deletegroup","delgroup","removepermissiongroup","deletepermissiongroup","delpermissiongroup")]
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
		#endregion

		#region PermissionGroupRole
		[Command("setrolegroup")]
		[Alias("setrolepermissiongroup")]
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
		#endregion

		#region PermissionGroupPermissions
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

			await Context.ReplyAsync($"`{permGroup}` permission group has the following permissions overrides:```\n{string.Join("\n",group.permissions.Where(p => p.Value!=null).Select(p => (p.Value.Value ? "✓ - " : "✗ - ")+p.Key))}```");
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
		[Command("setmultiple")]
		[Alias("setmany")]
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
		[Command("remove")]
		[Alias("delete","del")]
		public async Task RemovePermissionsCommand(string permGroup,params string[] permissions)
		{
			var data = MemorySystem.memory[Context.server].GetData<PermissionSystem,PermissionServerData>();
			if(!data.permissionGroups.TryGetValue(permGroup,out var perms)) {
				throw new BotError($"Permission group `{permGroup}` does not exist.");
			}

			List<string> removed = new List<string>();
			List<string> skipped = new List<string>();
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
				text += $"Removed the following permissions: ```\n{string.Join("\n",removed)}```\n";
			}

			if(skipped.Count>0) {
				text += $"The following permissions already weren't present: ```\n{string.Join("\n",skipped)}```\n";
			}

			await Context.ReplyAsync(text);
		}
		#endregion
	}
}