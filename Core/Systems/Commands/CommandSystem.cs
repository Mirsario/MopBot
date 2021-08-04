using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Reflection;
using Discord;
using Discord.WebSocket;
using Discord.Commands;
using MopBot.Extensions;
using MopBot.Core.Systems.Memory;
using MopBot.Core.TypeReaders;

#pragma warning disable CS1998 //Async method lacks 'await' operators and will run synchronously

namespace MopBot.Core.Systems.Commands
{
	[SystemConfiguration(AlwaysEnabled = true, Description = "Internal system that detects and executes commands.")]
	public partial class CommandSystem : BotSystem
	{
		public static CommandService commandService;
		public static Dictionary<string, BotSystem> commandGroupToSystem;
		public static Dictionary<string, BotSystem> commandToSystem;
		public static Dictionary<char, Regex> commandRegex = new Dictionary<char, Regex>();

		public override void RegisterDataTypes()
		{
			RegisterDataType<ServerMemory, CommandServerData>();
		}
		public override async Task Initialize()
		{
			commandGroupToSystem = new Dictionary<string, BotSystem>();
			commandToSystem = new Dictionary<string, BotSystem>();

			foreach(var type in MopBot.botTypes.Where(t => !t.IsAbstract && t.IsDerivedFrom(typeof(BotSystem)))) {
				string group = null;

				var system = MopBot.instance.systems.First(t => t.GetType() == type);

				if(system == null) {
					throw new Exception($"Couldn't find instance of System {type.Name}");
				}

				var groupAttribute = type.GetCustomAttribute<GroupAttribute>();

				if(groupAttribute != null) {
					group = groupAttribute.Prefix;
					commandGroupToSystem[group] = system;
				}

				foreach(var method in type.GetMethods()) {
					var cmdAttribute = method.GetCustomAttribute<CommandAttribute>();

					if(cmdAttribute != null) {
						commandToSystem[$"{(group == null ? null : $"{group} ")}{cmdAttribute.Text}"] = system;
					}
				}
			}
		}
		public override async Task OnMessageReceived(MessageContext message)
		{
			await ExecuteCommand(message);
		}

		public static void StaticInit()
		{
			commandService = new CommandService(new CommandServiceConfig {
				DefaultRunMode = RunMode.Sync,
				CaseSensitiveCommands = false
			});

			//commandService.AddTypeReader(new UserTypeReader<
			commandService.AddTypeReader<SocketGuildUser>(new DownloadCapableUserTypeReader<SocketGuildUser>(), true);

			foreach(var type in MopBot.botTypes.Where(t => !t.IsAbstract && typeof(CustomTypeReader).IsAssignableFrom(t))) {
				var instance = (CustomTypeReader)Activator.CreateInstance(type);

				foreach(Type assignedType in instance.Types) {
					commandService.AddTypeReader(assignedType, instance);
				}
			}
		}
		public static async Task ExecuteCommand(MessageContext context, bool skipRegex = false)
		{
			var server = context.server;

			if(server == null || !context.isCommand) {
				return;
			}

			var commandServerData = server.GetMemory().GetData<CommandSystem, CommandServerData>();

			//Regex parsing
			char symbol = skipRegex ? ' ' : commandServerData.commandPrefix;

			if(!commandRegex.TryGetValue(symbol, out Regex regex)) {
				//										([!]|&&)((?:"[\s\S]*?"|[\s\S](?!&&))+)(?!&&)
				commandRegex[symbol] = regex = new Regex($@"({(skipRegex ? "$" : $"[{symbol}]")}|&&)((?:""[\s\S]*?""|[\s\S](?!&&))+)(?!&&)", RegexOptions.Compiled);
			}

			var matches = regex.Matches(context.content);

			if(matches.Count == 0) {
				return;
			}

			bool fail = false;

			for(int i = 0; i < matches.Count; i++) {
				Match match = matches[i];
				string commandText = match.Groups[2].Value.Trim();

				var searchResult = commandService.Search(context, commandText);

				if(!searchResult.IsSuccess) {
					Console.WriteLine($"Search for '{commandText}' failed.");
					return;
				}

				var commandMatches = searchResult.Commands;
				int numCommands = commandMatches.Count;

				bool matchSucceeded = false;
				bool forceBreak = false;

				for(int j = 0; j < numCommands; j++) {
					var commandMatch = commandMatches[j];
					var command = commandMatch.Command;

					bool lastCommand = j == numCommands - 1;

					if(!TryGetCommandsSystem(command, out BotSystem system) || !system.IsEnabledForServer(server)) {
						continue;
					}

					Console.WriteLine($"Executing command [{i + 1}/{matches.Count}] '{commandText}'.");

					var preconditionResult = await commandMatch.CheckPreconditionsAsync(context, MopBot.serviceProvaider);
					var parseResult = await commandMatch.ParseAsync(context, searchResult, preconditionResult, MopBot.serviceProvaider);
					var result = (ExecuteResult)await commandMatch.ExecuteAsync(context, parseResult, MopBot.serviceProvaider);

					if(result.IsSuccess) {
						matchSucceeded = true;
						break;
					} else {
						var e = result.Error;

						EmbedBuilder embedBuilder;

						switch(e) {
							case CommandError.Exception:
								var exception = result.Exception;

								embedBuilder = MopBot.GetEmbedBuilder(server);

								if(exception is BotError botError) {
									embedBuilder.Title = $"❌ - {botError.Message}";
									embedBuilder.Color = Color.Orange;
								} else {
									embedBuilder.Title = "❗ - Unhandled Exception";
									embedBuilder.Description = $"`{result.ErrorReason}`";
									embedBuilder.Color = Color.Red;
								}

								await context.ReplyAsync(embedBuilder.Build());

								forceBreak = true;

								break;
							case CommandError.UnmetPrecondition:
								if(lastCommand) {
									await context.ReplyAsync(MopBot.GetEmbedBuilder(server)
										.WithTitle($"❌ - {result.ErrorReason}")
										.WithColor(Color.Orange)
										.Build()
									);
								}

								break;
							default: {
									if(!lastCommand || e == CommandError.Unsuccessful) {
										break;
									}

									embedBuilder = MopBot.GetEmbedBuilder(server)
										.WithTitle($"❌ - {(e == CommandError.BadArgCount ? "Invalid number of arguments." : result.ErrorReason)}")
										.WithDescription($"**Usage:** `{commandMatch.Alias + (command.Parameters.Count == 0 ? "" : " " + string.Join(' ', command.Parameters.Select(p => $"<{p.Name}>")))}`")
										.WithColor(Color.Orange);

									await context.ReplyAsync(embedBuilder.Build());

									break;
								}
						}

						if(e != CommandError.Unsuccessful && (lastCommand || forceBreak)) {
							await context.Failure();
						}

						if(forceBreak) {
							break;
						}
					}
				}

				if(!matchSucceeded || forceBreak) {
					fail = true;
					break;
				}
			}

			if(!fail && context is MessageContext c && c.message != null && !MessageSystem.MessageIgnored(c.message.Id) && c.socketTextChannel != null && await c.socketTextChannel.GetMessageAsync(c.message.Id) != null) {
				await context.Success();
			}
		}
		public static List<(string[] aliases, string description, bool isGroup)> GetAvailableCommands(SocketGuild server, SocketGuildUser user, bool fillNullDescription = false)
		{
			var result = new List<(string[] aliases, string description, bool isGroup)>();
			var shownCommands = new List<string>();
			var context = new MessageContext(null, server, user);

			foreach(var m in commandService.Modules) {
				string noDescription = fillNullDescription ? "No description provided." : null;

				if(m.Group != null) {
					if(!shownCommands.Contains(m.Group) && commandGroupToSystem.TryGetValue(m.Group, out BotSystem system) && system.IsEnabledForServer(server) && m.Preconditions.PermissionsMet(context)) {
						result.Add((m.Aliases.ToArray(), m.Summary ?? noDescription, true));
						shownCommands.Add(m.Group);
					}
				} else {
					foreach(var c in m.Commands) {
						if(!shownCommands.Contains(c.Name) && commandToSystem.TryGetValue(c.Name, out BotSystem system) && system.IsEnabledForServer(server) && c.Preconditions.PermissionsMet(context, c)) {
							result.Add((c.Aliases.ToArray(), c.Summary ?? noDescription, false));
							shownCommands.Add(c.Name);
						}
					}
				}
			}

			return result;
		}
		public static bool TryGetCommandsSystem(CommandInfo command, out BotSystem system)
		{
			var group = command.Module.Group;

			string key;

			if(!(group == null ? commandToSystem : commandGroupToSystem).TryGetValue(key = group ?? command.Name, out system)) {
				throw new Exception($"Can't get system from module '{key}'!");
			}

			if(system == null) {
				throw new Exception($"System '{key}' is null!");
			}

			return true;
		}
	}
}
