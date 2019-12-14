using System.Collections.Generic;
using System.Collections.Concurrent;
using Discord.WebSocket;
using MopBotTwo.Core.Systems.Memory;


namespace MopBotTwo.Core.Systems.Permissions
{
	public class PermissionServerData : ServerData
	{
		public Dictionary<ulong,string> roleGroups;
		public ConcurrentDictionary<string,PermissionGroup> permissionGroups;

		public override void Initialize(SocketGuild server)
		{
			//permissionGroups
			permissionGroups = new ConcurrentDictionary<string,PermissionGroup>();
			foreach(var defGroup in PermissionSystem.defaultGroups) {
				permissionGroups[defGroup.Key] = defGroup.Value.Clone();
			}

			//rolePermissions
			roleGroups = new Dictionary<ulong,string>();
			if(server?.EveryoneRole!=null) {
				roleGroups[server.EveryoneRole.Id] = "everyone";
			}
		}

		public bool? GetRolePermission(ulong roleId,string permission)
		{
			if(!roleGroups.TryGetValue(roleId,out string groupName)) {
				return null;
			}

			var group = permissionGroups[groupName];
			if(!group.permissions.TryGetValue(permission,out bool? newResult)) {
				foreach(var pair in group.permissions) {
					string str = pair.Key;
					for(int i = 0;i<str.Length;i++) {
						var c = str[i];
						//if(c==
					}
				}
			}

			return null;
		}
	}
}