/*using System.Threading.Tasks;
using System.Text.RegularExpressions;
using Discord.Commands;
using MopBotTwo.Core.Systems;
using MopBotTwo.Core.Systems.Permissions;
using MopBotTwo.Core.Systems.Memory;
using System.Collections.Generic;
using MopBotTwo.Core;
using System.Net;

namespace MopBotTwo.Common.Systems.Issues
{
	[Group("githubissues")]
	[Summary("Shows open issues from a GitHub repository in a channel.")]
	[RequirePermission(SpecialPermission.Owner,"githubissues")]
	[SystemConfiguration(Hidden = true,Description = "Shows open issues from a GitHub repository in a channel.")]
	public partial class GithubIssueSystem : BotSystem
	{
		public static readonly Dictionary<string,(string prefix,string text)> StateInfo = new Dictionary<string,(string,string)> {
			{ "open",		(":exclamation:",		"To be fixed") },
			{ "closed",		(":white_check_mark:",	"Fixed for next release") }
		};

		public override void RegisterDataTypes()
		{
			//RegisterDataType<ServerMemory,IssueServerData>();
		}

		public static async Task GetOpenIssues(MessageExt context)
		{
			var memory = MemorySystem.memory[context.server].GetData<GithubIssueSystem,GithubIssueServerData>();

			if(!memory.IsRepositoryValid) {
				throw new BotError("Repository has not been set.");
			}

			var repository = memory.repository;

			using var webClient = new WebClient();

			
		}
	}
}*/