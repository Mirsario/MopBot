using System.Threading.Tasks;
using Discord.Commands;
using MopBotTwo.Extensions;
using MopBotTwo.Core.Systems;
using MopBotTwo.Core.Systems.Commands;


namespace MopBotTwo.Common.Systems.Configuration
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