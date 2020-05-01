using System.Threading.Tasks;
using Discord.Commands;
using MopBot.Core.Systems;
using MopBot.Core.Systems.Permissions;
using MopBot.Core.Systems.Memory;

#pragma warning disable CS1998 //Async method lacks 'await' operators and will run synchronously

namespace MopBot.Common.Systems.CustomRoles
{
	[RequirePermission(SpecialPermission.Owner,"customrole")]
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
	}
}