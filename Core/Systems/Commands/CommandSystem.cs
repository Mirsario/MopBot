using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Reflection;
using Discord;
using Discord.WebSocket;
using Discord.Commands;
using MopBotTwo.Extensions;
using MopBotTwo.TypeReaders;
using MopBotTwo.Core.Systems.Memory;

namespace MopBotTwo.Core.Systems.Commands
{
	[SystemConfiguration(AlwaysEnabled = true,Description = "Internal system that detects and executes commands.")]
	public partial class CommandSystem : BotSystem
	{
		public static CommandService commandService;
		public static Dictionary<string,BotSystem> commandGroupToSystem;
		public static Dictionary<string,BotSystem> commandToSystem;
		public static Dictionary<char,Regex> commandRegex = new Dictionary<char,Regex>();
		public static Dictionary<string,MessageExt> recentContexts = new Dictionary<string,MessageExt>();
		[ThreadStatic] public static MessageExt currentThreadContext;

		private static readonly Regex TempRegex = new Regex(@"Error occurred executing ("".+"" for .+#\d\d\d\d in .+\/[\w-_]+)\.");

		public override void RegisterDataTypes()
		{
			RegisterDataType<ServerMemory,CommandServerData>();
		}
		public override async Task Initialize()
		{
			commandGroupToSystem = new Dictionary<string,BotSystem>();
			commandToSystem = new Dictionary<string,BotSystem>();

			foreach(var type in MopBot.botTypes.Where(t => !t.IsAbstract && t.IsDerivedFrom(typeof(BotSystem)))) {
				string group = null;
				var system = MopBot.instance.systems.First(t => t.GetType()==type);
				if(system==null) {
					throw new Exception($"Couldn't find instance of System {type.Name}");
				}

				var groupAttribute = type.GetCustomAttribute<GroupAttribute>();
				if(groupAttribute!=null) {
					group = groupAttribute.Prefix;
					commandGroupToSystem[group] = system;
				}

				foreach(var method in type.GetMethods()) {
					var cmdAttribute = method.GetCustomAttribute<CommandAttribute>();
					if(cmdAttribute!=null) {
						commandToSystem[$"{(group==null ? null : $"{group} ")}{cmdAttribute.Text}"] = system;
					}
				}
			}
		}
		public override async Task OnMessageReceived(MessageExt message)
		{
			await ExecuteCommand(message);
		}

		public static void StaticInit()
		{
			commandService = new CommandService(new CommandServiceConfig {
				DefaultRunMode = RunMode.Sync,
				CaseSensitiveCommands = false
			});
			commandService.Log += CommandServiceLogging;
			commandService.CommandExecuted += OnCommandExecuted;

			foreach(var type in MopBot.botTypes.Where(t => !t.IsAbstract && typeof(CustomTypeReader).IsAssignableFrom(t))) {
				var instance = (CustomTypeReader)Activator.CreateInstance(type);

				foreach(Type assignedType in instance.Types) {
					Console.WriteLine($"Registering {type.Name} for type {assignedType.Name}");
					commandService.AddTypeReader(assignedType,instance);
				}
			}
		}
		public static async Task ExecuteCommand(MessageExt context,bool skipRegex = false)
		{
			var server = context.server;
			if(server==null || !context.isCommand) {
				return;
			}

			Console.WriteLine($"ExecuteCommand called.");

			var commandServerData = server.GetMemory().GetData<CommandSystem,CommandServerData>();

			//Regex parsing
			char symbol = skipRegex ? ' ' : commandServerData.commandPrefix;

			//string GetRegexCode() => $@"(^{(skipRegex ? null : $@"[{symbol}](?=\S)")}|&&)((?:"".*?""|[^&]*?)+)(?=&&|$)";
			string GetRegexCode() => $@"({(skipRegex ? "$" : $"[{symbol}]")}|&&)((?:""[\s\S]*?""|[\s\S](?!&&))+)(?!&&)";

			if(!commandRegex.TryGetValue(symbol,out Regex regex)) {
				commandRegex[symbol] = regex = new Regex(GetRegexCode(),RegexOptions.Compiled);
			}

			Console.WriteLine($"Executing regex '{GetRegexCode()}'.");

			var matches = regex.Matches(context.content);

			Console.WriteLine($"Found {matches.Count} matches.");

			foreach(Match match in matches) {
				/*if(commandNum>0 && lastCmdFailed && match.Groups[1].Value=="&") {
					return;
				}*/
				
				string commandText = match.Groups[2].Value.Trim(); //Should try doing trimming in regex

				var searchResult = commandService.Search(context,commandText);
				if(!searchResult.IsSuccess) {
					return;
				}

				var commandMatches = searchResult.Commands;
				int numCommands = commandMatches.Count;

				Console.WriteLine($"Found {numCommands} commands.");

				bool anythingSucceeded = false;
				bool forceBreak = false;

				for(int i = 0;i<numCommands;i++) {
					var commandMatch = commandMatches[i];
					var command = commandMatch.Command;

					bool lastCommand = i==numCommands-1;

					if(!TryGetCommandsSystem(command,out BotSystem system) || !system.IsEnabledForServer(server)) {
						continue;
					}

					Console.WriteLine($"Executing command '{commandText}'.");

					//TODO: Find another solution to pass context to CommandServiceLogging(LogMessage)
					recentContexts[$@"""{command.Name}"" for {context.user.Id} in {context.server.Id}/{context.Channel.Id}"] = context;
					currentThreadContext = context;

					var preconditionResult = await commandMatch.CheckPreconditionsAsync(context,MopBot.serviceProvaider);
					var parseResult = await commandMatch.ParseAsync(context,searchResult,preconditionResult,MopBot.serviceProvaider);
					var result = await commandMatch.ExecuteAsync(context,parseResult,MopBot.serviceProvaider); //commandService.ExecuteAsync(context,commandText,MopBot.serviceProvaider);

					if(result.IsSuccess) {
						anythingSucceeded = true;
						break;
					} else {
						var e = result.Error;
						switch(e) {
							case CommandError.Exception:
								await context.ReplyAsync($"An error has occured when trying to execute the last command: {result.ErrorReason}");
								await context.Failure();

								forceBreak = true;
								break;
							case CommandError.UnmetPrecondition:
								if(lastCommand) {
									await context.ReplyAsync(result.ErrorReason);
								}
								break;
							default: {
								if(!lastCommand || e==CommandError.Unsuccessful) {
									break;
								}

								string text = e switch {
									CommandError.BadArgCount => "Invalid number of arguments.",
									_ => result.ErrorReason,
								};

								text += $"\r\n**Usage:** `{commandMatch.Alias+(command.Parameters.Count==0 ? "" : " "+string.Join(' ',command.Parameters.Select(p => $"<{p.Name}>")))}`";

								if(text!=null) {
									await context.ReplyAsync(text);
									await context.Failure();
								}

								break;
							}
						}

						continue;
					}
				}

				if(!anythingSucceeded || forceBreak) {
					break;
				}
			}
		}
		public static List<(string[] aliases,string description,bool isGroup)> GetAvailableCommands(SocketGuild server,SocketGuildUser user,bool fillNullDescription = false)
		{
			var result = new List<(string[] aliases,string description,bool isGroup)>();
			var shownCommands = new List<string>();
			var context = new MessageExt(null,server,user);
			foreach(var m in commandService.Modules) {
				string noDescription = fillNullDescription ? "No description provided." : null;
				if(m.Group!=null) {
					if(!shownCommands.Contains(m.Group) && commandGroupToSystem.TryGetValue(m.Group,out BotSystem system) && system.IsEnabledForServer(server) && m.Preconditions.PermissionsMet(context)) {
						result.Add((m.Aliases.ToArray(),m.Summary ?? noDescription,true));
						shownCommands.Add(m.Group);
					}
				} else {
					foreach(var c in m.Commands) {
						if(!shownCommands.Contains(c.Name) && commandToSystem.TryGetValue(c.Name,out BotSystem system) && system.IsEnabledForServer(server) && c.Preconditions.PermissionsMet(context,c)) {
							result.Add((c.Aliases.ToArray(),c.Summary ?? noDescription,false));
							shownCommands.Add(c.Name);
						}
					}
				}
			}
			return result;
		}
		public static bool TryGetCommandsSystem(CommandInfo command,out BotSystem system)
		{
			var group = command.Module.Group;
			
			string key;
			if(!(group==null ? commandToSystem : commandGroupToSystem).TryGetValue(key = group ?? command.Name,out system)) {
				throw new Exception($"Can't get system from module '{key}'!");
			}

			if(system==null) {
				throw new Exception($"System '{key}' is null!");
			}

			return true;
		}

		private static async Task CommandServiceLogging(LogMessage logMsg)
		{
			//Console.WriteLine($"CommandServiceLogging: exception is {arg.Exception?.GetType()?.Name ?? "NULL"}, message is '{arg.Message}', source is '{arg.Source}'");
			var exception = logMsg.Exception;
			if(exception is CommandException cmdException) {
				Console.WriteLine($"CommandServiceLogging: cmdException.Message: {cmdException.Message}");

				var cmdInnerException = cmdException.InnerException;
				var context = currentThreadContext;

				//TODO: Replace this shitcode with something else..
				var match = TempRegex.Match(cmdException.Message);
				if(match.Success) {
					string key = match.Groups[1].Value;
					if(recentContexts.TryGetValue(key,out var newContext)) {
						context = newContext;
						recentContexts.Remove(key);
					}
				}

				if(cmdInnerException is BotError botError) {
					if(context!=null) {
						string errorMsg = botError.Message;
						var errorInnerException = botError.InnerException;

						if(!string.IsNullOrEmpty(errorMsg)) {
							if(errorInnerException!=null) {
								await context.ReplyAsync($"{errorMsg}```\r\n{errorInnerException.Message}```");
							} else {
								await context.ReplyAsync(errorMsg);
							}
						} else if(errorInnerException!=null) {
							await context.ReplyAsync(errorInnerException.Message);
						}

						await context.Failure();
					} else {
						Console.WriteLine($"InnerException is BotError, but context is null. Message: '{cmdInnerException.Message}'.");
					}
				} else {
					Console.WriteLine($"InnerException is {(cmdInnerException!=null ? $"{cmdInnerException.GetType().Name}: '{cmdInnerException.Message}'." : "Null")}");
				}
			}
		}
		private static async Task OnCommandExecuted(Optional<CommandInfo> commandInfo,ICommandContext context,IResult result)
		{
			if(context is MessageExt c && !c.messageDeleted && c.message!=null && c.socketTextChannel!=null && await c.socketTextChannel.GetMessageAsync(c.message.Id)!=null) {
				await context.Success();
			}

			if(!commandInfo.IsSpecified) {
				return;
			}

			var user = context.User;
			recentContexts.Remove($@"""{commandInfo.Value.Name}"" for {user.Id} in {context.Guild.Id}/{context.Channel.Id}");
		}
	}
}