using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;
using MopBotTwo.Extensions;

#pragma warning disable CS1998

namespace MopBotTwo.Systems
{
	//TODO: Find a better name? Rofl.
	[Group("systems")] [Alias("system")]
	[RequirePermission(SpecialPermission.Owner,"managesystems")]
	[SystemConfiguration(AlwaysEnabled = true,Description = "Lets admins select which systems will be enabled for this server.")]
	public class SystemsSystem : BotSystem
	{
		[Command("enable")]
		public async Task EnableSystem(string systemName)
		{
			if(!nameToSystem.TryGetValue(systemName,out BotSystem system)) {
				throw new BotError($"Couldn't find a system named '{systemName}'.");
			}

			system.GetMemory<ServerData>(Context.server).isEnabled = true;
		}
		[Command("disable")]
		public async Task DisableSystem(string systemName)
		{
			if(!nameToSystem.TryGetValue(systemName,out BotSystem system)) {
				throw new BotError($"Couldn't find a system named '{systemName}'.");
			}

			system.GetMemory<ServerData>(Context.server).isEnabled = false;
		}

		[Command("list")]
		public async Task ListSystems()
		{
			var context = Context;
			var server = context.server;
			var builder = MopBot.GetEmbedBuilder(server);

			foreach(var system in systems.Where(s => !s.configuration.AlwaysEnabled).OrderBy(s => s.GetType().Name)) {
				builder.AddField($"{(system.IsEnabledForServer(server) ? ":white_check_mark:" : ":x:")} - {system.name}",system.configuration.Description ?? "`Configuration missing.`");
			}

			await ReplyAsync(embed:builder.Build());
		}
	}
}