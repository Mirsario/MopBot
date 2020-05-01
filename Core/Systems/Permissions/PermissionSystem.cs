using System.Threading.Tasks;
using System.Collections.Concurrent;
using Discord.Commands;
using MopBot.Core.Systems.Memory;

#pragma warning disable CS1998 //Async method lacks 'await' operators and will run synchronously

namespace MopBot.Core.Systems.Permissions
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
	}
}