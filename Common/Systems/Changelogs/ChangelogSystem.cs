using System.Text.RegularExpressions;
using Discord.Commands;
using MopBot.Core.Systems.Permissions;
using MopBot.Core.Systems;
using MopBot.Core.Systems.Memory;
using System.Threading.Tasks;
using MopBot.Extensions;

namespace MopBot.Common.Systems.Changelogs
{
	[Group("changelog")]
	[Alias("changes", "cl")]
	[Summary("Helps managing project changelog channels")]
	[RequirePermission(SpecialPermission.Owner, "changelog")]
	[SystemConfiguration(Description = "Helps maintaning changelogs, which then can be converted to text lists in different formats: Discord, BBCode, Patreon, etc.")]
	public partial class ChangelogSystem : BotSystem
	{
		public override void RegisterDataTypes()
		{
			RegisterDataType<ServerMemory, ChangelogServerData>();
		}

		private async Task<ChangelogEntry> NewEntryInternal(string type, string entryText, bool publish, ChangelogServerData data = null)
		{
			if(data == null) {
				data = Context.server.GetMemory().GetData<ChangelogSystem, ChangelogServerData>();
			}

			if(!data.GetChangelogChannel(out var channel)) {
				throw new BotError("Changelog channel has not been set!");
			}

			var newEntry = data.NewEntry(type, entryText);

			if(publish) {
				await data.PublishEntry(newEntry, channel);
			}

			return newEntry;
		}
	}
}