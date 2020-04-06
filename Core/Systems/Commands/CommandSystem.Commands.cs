using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Discord;
using Discord.Commands;
using MopBotTwo.Extensions;

namespace MopBotTwo.Core.Systems.Commands
{
	public partial class CommandSystem
	{
		private static readonly Regex GroupCommandRegex = new Regex(@"\s*(\w+)(?:\s+([\w\s]+))?",RegexOptions.Compiled);
		
		[Command("help")] [Alias("commands")]
		[Summary("Lists commands that are currently available to you.")]
		public async Task HelpCommand()
		{
			var context = Context;
			var builder = MopBot.GetEmbedBuilder(context)
				.WithAuthor($"Commands available to @{context.socketServerUser.GetDisplayName()}:",context.user.GetAvatarUrl())
				.WithFooter($"You can type {context.server.GetMemory().GetData<CommandSystem,CommandServerData>().commandPrefix}help <command> to see groups' commands and commands' syntaxes.");

			foreach((var aliases,string description,_) in GetAvailableCommands(context.server,context.socketServerUser,true)) {
				builder.AddField(string.Join("/",aliases),description);
			}

			await context.socketTextChannel.SendMessageAsync(embed:builder.Build());
		}
		[Command("help")] [Alias("commands")]
		[Summary("Lists commands of a group, or shows you syntax of a command.")]
		public async Task HelpCommand([Remainder]string cmdOrGroup)
		{
			var context = Context;
			
			cmdOrGroup = cmdOrGroup.Trim().ToLower();

			var match = GroupCommandRegex.Match(cmdOrGroup);

			if(!match.Success) {
				throw new BotError($"Invalid input: `{cmdOrGroup}`.");
			}

			string groupA = match.Groups[1].Value;
			string groupB = match.Groups[2].Value;

			var strComparer = MopBot.StrComparerIgnoreCase;
			var cmdPrefix = context.server.GetMemory().GetData<CommandSystem,CommandServerData>().commandPrefix;
			
			EmbedBuilder builder = null;

			EmbedBuilder PrepareBuilder() => builder = MopBot.GetEmbedBuilder(context);

			static IEnumerable<string> GetAliasesWithoutParent(IEnumerable<string> aliases)
			{
				var hashSet = new HashSet<string>();

				foreach(var alias in aliases) {
					var match = GroupCommandRegex.Match(alias);
					var groups = match.Groups;

					hashSet.Add(groups[2].Success ? groups[2].Value : groups[1].Value);
				}

				return hashSet;
			}

			static (string name,string description) GetCommandNameAndDescription(CommandInfo c,string prefix,bool oneName = false)
			{
				prefix = StringUtils.AppendIfNotNull(prefix," ");
				
				return (
				   $"{prefix}{(oneName ? c.Name : string.Join('/',GetAliasesWithoutParent(c.Aliases)))}",
				   $"{StringUtils.AppendIfNotNull(c.Summary,"\r\n\r\n")}**Usage:** `{prefix}{c.Name} {string.Join(' ',c.Parameters.Select(p => $"<{p.Name}>"))}`"
				);
			}

			bool postReady = false;

			foreach(var m in commandService.Modules) {
				var group = m.Group;
				var aliases = m.Aliases;
				
				if(aliases.Any(a => strComparer.Equals(a,cmdOrGroup))) {
					//List all of group's commands
					PrepareBuilder()
						.WithDescription($"**Group aliases:** {string.Join(", ",aliases.Select(a => '`'+a+'`'))}.")
						.WithAuthor($@"Commands in group ""{group}"":",context.user.GetAvatarUrl());

					foreach(var c in m.Commands) {
						var (name,description) = GetCommandNameAndDescription(c,group);

						builder.AddField($"• {name}",description);
					}

					postReady = true;
					break;
				}

				string checkedString;
				if(group==null) {
					checkedString = groupA;
				}else if(strComparer.Equals(group,groupA)) {
					checkedString = groupB;
				}else{
					continue;
				}

				if(m.Commands.TryGetFirst(c => strComparer.Equals(c.Name,checkedString),out var cmd)) {
					//List single command
					var (name,description) = GetCommandNameAndDescription(cmd,group,true);

					PrepareBuilder()
						.WithAuthor($@"Command ""{name}"":",context.user.GetAvatarUrl())
						.WithDescription(description);

					postReady = true;
					break;
				}
			}

			if(!postReady) {
				throw new BotError("Unable to find any commands or groups with such name.");
			}

			await context.socketTextChannel.SendMessageAsync(embed:builder.Build());
		}
	}
}
