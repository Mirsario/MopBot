using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.IO;
using Discord;
using Discord.WebSocket;
using Discord.Commands;
using MopBotTwo.Extensions;

#pragma warning disable CS1998

namespace MopBotTwo.Systems
{
	[Group("changelog")]
	[Alias("changes","cl")]
	[Summary("Helps managing project changelog channels")]
	[RequirePermission(SpecialPermission.Owner,"managechangelog")]
	public partial class ChangelogSystem : BotSystem
	{
		public override void RegisterDataTypes()
		{
			RegisterDataType<ServerMemory,ChangelogServerData>();
		}
	}
}