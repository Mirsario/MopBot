using System.Threading.Tasks;
using System.Collections.Concurrent;
using Discord.Commands;
using MopBotTwo.Core.Systems.Memory;

namespace MopBotTwo.Core.Systems.Permissions
{
	[Group("permission")] [Alias("permissions","perms","perm")]
	[Summary("Group for configuring permissions.")]
	[SystemConfiguration(AlwaysEnabled = true)]
	[RequirePermission(SpecialPermission.Owner,"permissions")]
	public partial class PermissionSystem : BotSystem
	{
		public static ConcurrentDictionary<string,PermissionGroup> defaultGroups;

		public override void RegisterDataTypes()
		{
			RegisterDataType<ServerMemory,PermissionServerData>();
		}
		public override async Task Initialize()
		{
			if(defaultGroups==null) {
				defaultGroups = new ConcurrentDictionary<string,PermissionGroup>();
				defaultGroups.TryAdd("everyone",	new PermissionGroup("everyone"));
				defaultGroups.TryAdd("vip",			new PermissionGroup("vip"));
				defaultGroups.TryAdd("admin",		new PermissionGroup("admin"));
				defaultGroups.TryAdd("superadmin",	new PermissionGroup("superadmin"));
			}
			
			defaultGroups["superadmin"]["modifypermissions"] = true;
		}

		/*public static bool? GetRolePermission(SocketRole role,string permission,PermissionServerData serverData = null)
		{
			serverData ??= role.Guild.GetMemory().GetData<PermissionSystem,PermissionServerData>();

			if(!serverData.roleGroups.TryGetValue(role.Id,out string groupName)) {
				return null;
			}

			var group = serverData.permissionGroups[groupName];
			if(group.permissions.TryGetValue(permission,out bool? newResult)) {
				return newResult;
			}

			foreach(var pair in group.permissions) {
				string str = pair.Key;
				
			}
		}*/
	}
}