using System.Text.RegularExpressions;
using Discord.Commands;
using MopBotTwo.Core.Systems.Permissions;
using MopBotTwo.Core.Systems;
using MopBotTwo.Core.Systems.Memory;


namespace MopBotTwo.Common.Systems.Changelogs
{
	[Group("changelog")] [Alias("changes","cl")]
	[Summary("Helps managing project changelog channels")]
	[RequirePermission(SpecialPermission.Owner,"changelog")]
	[SystemConfiguration(Description = "Helps maintaning changelogs, which then can be converted to text lists in different formats: Discord, BBCode, Patreon, etc.")]
	public partial class ChangelogSystem : BotSystem
	{
		public override void RegisterDataTypes()
		{
			RegisterDataType<ServerMemory,ChangelogServerData>();
		}
	}
}