using System.Threading.Tasks;
using Discord.Commands;
using MopBot.Extensions;
using MopBot.Core.Systems;
using MopBot.Core.Systems.Commands;

#pragma warning disable CS1998 //Async method lacks 'await' operators and will run synchronously

namespace MopBot.Common.Systems.Configuration
{
	public partial class ConfigurationSystem : BotSystem
	{
		[Command("nickname")]
		[Alias("nick")]
		public async Task NicknameCommand([Remainder]string text) => Context.server.GetMemory().GetData<ConfigurationSystem,ConfigurationServerData>().forcedNickname = text;

		[Command("commandsymbol")]
		[Alias("cmdsymbol")]
		public async Task CommandPrefixCommand(char symbol) => Context.server.GetMemory().GetData<CommandSystem,CommandServerData>().commandPrefix = symbol;
	}
}