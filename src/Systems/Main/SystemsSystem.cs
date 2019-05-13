using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;
using MopBotTwo.Extensions;

#pragma warning disable CS1998

namespace MopBotTwo.Systems
{
	[Group("systems")]
	[Alias("system")]
	[RequirePermission(SpecialPermission.Owner,"managesystems")]
	[SystemConfiguration(AlwaysEnabled = true)]
	public class SystemsSystem : BotSystem
	{
		[Command("enable")]
		public async Task EnableSystem(string systemName)
		{
			if(!nameToSystem.TryGetValue(systemName,out BotSystem system)) {
				throw new BotError($"Couldn't find a system named '{systemName}'.");
			}

			var context = Context;

			system.GetMemory<ServerData>(context.server).isEnabled = true;
		}
		[Command("disable")]
		public async Task DisableSystem(string systemName)
		{
			if(!nameToSystem.TryGetValue(systemName,out BotSystem system)) {
				throw new BotError($"Couldn't find a system named '{systemName}'.");
			}

			var context = Context;

			system.GetMemory<ServerData>(context.server).isEnabled = false;
		}

		[Command("list")]
		public async Task ListSystems()
		{
			var server = Context.server;
			//more caching pls
			await Context.ReplyAsync($"```{string.Join('\n',systems.Where(s => !s.configuration.AlwaysEnabled).OrderBy(s => s.GetType().Name).Select(s => $"{s.GetType().Name} - {(s.IsEnabledForServer(server) ? "On" : "Off")}"))}```");
		}
	}
}