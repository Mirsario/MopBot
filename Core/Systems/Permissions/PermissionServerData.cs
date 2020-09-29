using System.Collections.Generic;
using System.Collections.Concurrent;
using Discord.WebSocket;
using MopBot.Core.Systems.Memory;

namespace MopBot.Core.Systems.Permissions
{
	public class PermissionServerData : ServerData
	{
		public Dictionary<ulong, string> roleGroups;
		public ConcurrentDictionary<string, PermissionGroup> permissionGroups;

		public override void Initialize(SocketGuild server)
		{
			//permissionGroups
			permissionGroups = new ConcurrentDictionary<string, PermissionGroup>();

			foreach(var defGroup in PermissionSystem.defaultGroups) {
				permissionGroups[defGroup.Key] = defGroup.Value.Clone();
			}

			//rolePermissions
			roleGroups = new Dictionary<ulong, string>();

			if(server?.EveryoneRole != null) {
				roleGroups[server.EveryoneRole.Id] = "everyone";
			}
		}
	}
}