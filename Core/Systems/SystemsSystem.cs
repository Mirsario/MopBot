using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;
using MopBot.Core.Systems.Memory;
using MopBot.Core.Systems.Permissions;
using MopBot.Extensions;

#pragma warning disable CS1998 //Async method lacks 'await' operators and will run synchronously

namespace MopBot.Core.Systems
{
	//TODO: Find a better name?

	[Group("systems")]
	[Alias("system")]
	[Summary("Group for commands that let admins control which systems will be enabled for this server.")]
	[RequirePermission(SpecialPermission.Admin, "managesystems")]
	[SystemConfiguration(AlwaysEnabled = true, Description = "Lets admins select which systems will be enabled for this server.")]
	public class SystemsSystem : BotSystem
	{
		[Command("enable")]
		public async Task EnableSystem(string systemName)
		{
			if (!nameToSystem.TryGetValue(systemName, out BotSystem system) || (system.Configuration.Hidden && !Context.user.IsBotMaster())) {
				throw new BotError($"Couldn't find a system named '{systemName}'.");
			}

			system.GetMemory<ServerData>(Context.server).isEnabled = true;
		}

		[Command("disable")]
		public async Task DisableSystem(string systemName)
		{
			if (!nameToSystem.TryGetValue(systemName, out BotSystem system)) {
				throw new BotError($"Couldn't find a system named '{systemName}'.");
			}

			system.GetMemory<ServerData>(Context.server).isEnabled = false;
		}

		[Command("list")]
		public async Task ListSystems()
		{
			var server = Context.server;
			bool isMaster = Context.user.IsBotMaster();
			var builder = MopBot.GetEmbedBuilder(server);

			foreach (var system in allSystems.Where(s => (!s.Configuration.Hidden || isMaster) && !s.Configuration.AlwaysEnabled).OrderBy(s => s.GetType().Name)) {
				builder.AddField($"{(system.IsEnabledForServer(server) ? ":white_check_mark:" : ":x:")} - {system.Name}", system.Configuration.Description ?? "`Configuration missing.`");
			}

			await ReplyAsync(embed: builder.Build());
		}
	}
}
