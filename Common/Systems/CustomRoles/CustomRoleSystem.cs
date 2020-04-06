using System.Threading.Tasks;
using Discord.Commands;
using MopBotTwo.Core.Systems;
using MopBotTwo.Core.Systems.Permissions;
using MopBotTwo.Core.Systems.Memory;

namespace MopBotTwo.Common.Systems.CustomRoles
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