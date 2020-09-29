﻿using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Reflection;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using MopBot.Extensions;
using MopBot.Core.Systems;
using MopBot.Core.Systems.Commands;
using MopBot.Core.Systems.Memory;
using MopBot.Core.Systems.Channels;
using MopBot.Core.Systems.Status;
using MopBot.Core;
using System.Runtime.ExceptionServices;

#pragma warning disable CS0162

//TODO: Needs major refactoring.

namespace MopBot
{
	public class MopBot : SystemContainer
	{
		public const char DefaultCommandPrefix = '!';

		public static readonly StringComparer StrComparerIgnoreCase = StringComparer.InvariantCultureIgnoreCase;
		public static readonly Random Random = new Random((int)DateTime.Now.Ticks);

		public static MopBot instance;
		public static Type[] botTypes;
		public static Assembly botAssembly;
		public static DiscordSocketClient client;
		public static IServiceProvider serviceProvaider;
		public static RequestOptions optAlwaysRetry;
		public static ConsoleColor defaultConsoleColor;
		public static string tempFolder;
		public static SocketTextChannel logChannel;

		static void Main()
		{
			defaultConsoleColor = Console.ForegroundColor;
			instance = new MopBot();

			try {
				instance.Run().Wait();
			}
			catch(Exception e) {
				HandleException(e).Wait();
			}

			Console.ReadLine();
		}

		public MopBot() : base()
		{
			AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
			AppDomain.CurrentDomain.FirstChanceException += OnFirstChanceException;
		}

		public async Task Run()
		{
			GlobalConfiguration.Initialize();

			tempFolder = Path.GetFullPath("Temp") + Path.DirectorySeparatorChar;

			Directory.CreateDirectory(tempFolder);

			optAlwaysRetry = RequestOptions.Default;
			optAlwaysRetry.RetryMode = RetryMode.AlwaysRetry;

			#region SystemInstancing

			//regions r bad
			//TODO: Add system priorities, replace this.

			await AddSystem<DiscordConnectionSystem>();
			await AddSystem<MemorySystem>();
			await AddSystem<ChannelSystem>();
			await AddSystem<CommandSystem>();
			await AddSystem<MessageSystem>();
			await AddSystem<StatusSystem>();

			botAssembly = Assembly.GetExecutingAssembly();
			botTypes = botAssembly.GetTypes();
			Type[] systemTypes = botTypes.Where(t => !t.IsAbstract && t.IsDerivedFrom(typeof(BotSystem))).ToArray();

			var toggles = GlobalConfiguration.config.systemToggles;
			var newToggles = new Dictionary<string, bool>();

			for(int i = 0; i < systemTypes.Length; i++) {
				var thisType = systemTypes[i];

				if(!systems.Any(q => q.GetType() == thisType)) {
					string name = thisType.Name;
					var config = BotSystem.GetConfiguration(thisType);

					bool isEnabled;

					if(config.AlwaysEnabled) {
						isEnabled = true;
					} else {
						if(toggles == null || !toggles.TryGetValue(name, out isEnabled)) {
							isEnabled = true;
						}

						newToggles[name] = isEnabled;
					}

					if(isEnabled) {
						await AddSystem(thisType); //TODO: Pass config, so this method doesn't have to search for it again?
					}
				}
			}

			if(toggles == null || newToggles.Count != toggles.Count || newToggles.Any(t => !toggles.ContainsKey(t.Key))) {
				GlobalConfiguration.config.systemToggles = newToggles;
				GlobalConfiguration.Save();
			}

			#endregion

			await InitializeSystems();

			//TODO: Unhardcode
			CommandSystem.StaticInit();

			serviceProvaider = new ServiceCollection()
				.AddSingleton(client)
				.AddSingleton(CommandSystem.commandService)
				.BuildServiceProvider();

			await CommandSystem.commandService.AddModulesAsync(Assembly.GetEntryAssembly(), serviceProvaider);

			while(true) {
				try {
					await UpdateSystems(true);
					await Task.Delay(1000);
				}
				catch(FatalException e) {
					await HandleException(e);

					return;
				}
				catch(Exception e) {
					await HandleException(e);
				}
			}
		}

		private static async void OnFirstChanceException(object sender, FirstChanceExceptionEventArgs args)
		{
			var e = args.Exception;

			if(e is BotError || e is TaskCanceledException) {
				return;
			}

			string checkString = (e.Message + e.StackTrace).ToLowerInvariant();

			if(checkString != null && checkString.Contains("internal server error") || ((checkString.Contains("discord") || checkString.Contains("websocket")) && !checkString.Contains("mopbot"))) {
				return;
			}

			await HandleException(e, "OnFirstChanceException - ");
		}
		private static void OnProcessExit(object sender, EventArgs e)
		{
			if(!StatusSystem.noActivityChanging) {
				StatusSystem.ForceStatus(ActivityType.Playing, "Offline", UserStatus.AFK, true);
			}

			if(MemorySystem.canSave) {
				Task.Run(async () => {
					var task = (instance?.systems?.FirstOrDefault(s => s?.GetType() == typeof(MemorySystem)) as MemorySystem)?.Save();

					if(task != null) {
						await task;
					}
				}).Wait();
			}

			Console.WriteLine("Tried saving on quit.");
		}

		public static void OnClientInit(DiscordSocketClient client)
		{
			client.MessageReceived += MessageSystem.MessageReceived;
			client.MessageUpdated += MessageSystem.MessageUpdated;
			client.MessageDeleted += MessageSystem.MessageDeleted;
			client.ReactionAdded += MessageSystem.ReactionAdded;
			client.UserJoined += async user => await BotSystem.CallForEnabledSystems(user.Guild, s => s.OnUserJoined(user));
			client.UserLeft += async user => await BotSystem.CallForEnabledSystems(user.Guild, s => s.OnUserLeft(user));

			client.GuildMemberUpdated += async (oldUser, newUser) => {
				await BotSystem.CallForEnabledSystems(newUser.Guild, s => s.OnUserUpdated(oldUser, newUser));
			};
		}
		public static async Task OnReady()
		{
			if(GlobalConfiguration.config.logChannel.HasValue) {
				ulong id = GlobalConfiguration.config.logChannel.Value;

				logChannel = client.GetChannel(id) as SocketTextChannel;

				if(logChannel == null) {
					//Not exception-worthy.
					await HandleError($"(!!!) Could not find channel with id '{id}'. Is the bot not in that server anymore, or has it been configured wrong?");
				}
			}
		}

		public static EmbedBuilder GetEmbedBuilder(MessageContext context) => GetEmbedBuilder(context.server);
		public static EmbedBuilder GetEmbedBuilder(SocketGuild server) => new EmbedBuilder().WithColor(MemorySystem.memory[server].GetData<CommandSystem, CommandServerData>().embedColor.Value);
		public static async Task TryCatch(Func<Task> func)
		{
			try {
				await func();
			}
			catch(Exception e) {
				await HandleException(e);
			}
		}
		public static async Task TryCatchLogged(string actionText, Func<Task> func, bool dontHandle = false)
		{
			Console.Write(actionText);

			try {
				await func();
				Console.ForegroundColor = ConsoleColor.Green;
				Console.Write(" Success!\r\n");
			}
			catch(Exception e) {
				Console.ForegroundColor = ConsoleColor.Red;
				Console.Write(" ERROR: " + e.GetType().Name + "\r\n");

				if(!dontHandle) {
					await HandleException(e);
				}
			}

			Console.ForegroundColor = defaultConsoleColor;
		}
		public static async Task HandleException(Exception e, string prefix = null, bool mentionMasters = true, bool noDiscordPosts = false)
		{
			await HandleError($"{prefix}{e.GetType().Name}: {e.Message}\r\n```\r\n{e.StackTrace}```", mentionMasters, noDiscordPosts);

			if(e.InnerException != null) {
				await HandleException(e.InnerException, prefix: "Previous exception's InnerException:\r\n");
			}
		}
		public static async Task HandleError(string errorText, bool mentionMasters = true, bool noDiscordPosts = false)
		{
			string logText = $"Exception from `{DateTime.UtcNow}` (UTC):\r\n##### Exception Start #####\r\n{errorText}\r\n##### Exception End #####";

			Console.WriteLine(logText);

			File.AppendAllText("BotErrors.log", logText);

			if(!noDiscordPosts && logChannel != null && client?.ConnectionState == ConnectionState.Connected) {
				if(mentionMasters) {
					var usersToPing = GlobalConfiguration.config.usersToPingForExceptions ?? GlobalConfiguration.config.masterUsers;

					if(usersToPing != null && usersToPing.Length > 0) {
						errorText = $"{string.Join(", ", usersToPing.Select(id => $"<@{id}>"))}\r\n{errorText}";
					}
				}

				//TODO: This is lazy
				string[] texts = SplitIfNeeded(errorText, 1900).ToArray();

				for(int i = 0; i < texts.Length; i++) {
					var tempText = texts[i];

					if(i > 0) {
						tempText = "```" + tempText;
					}

					if(i < texts.Length - 1) {
						tempText += "```";
					}

					await logChannel.SendMessageAsync(tempText);
				}
			}
		}
		public static T Construct<T>(Type[] paramTypes, object[] paramValues)
		{
			var constructorInfo = typeof(T).GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null, paramTypes, null);

			return (T)constructorInfo.Invoke(paramValues);
		}
		public static string GetTempFileName(string baseName, string ext)
		{
			string name;

			do {
				name = Path.Combine(tempFolder, $"{baseName}_{Random.Next(1000)}{ext}");
			}
			while(File.Exists(name));

			return name;
		}
		public static void CheckForNull(object obj, string argName)
		{
			if(obj == null) {
				throw new BotError($"Argument `{argName}` cannot be null.");
			}
		}
		public static void CheckForNullOrEmpty(string str, string argName)
		{
			if(string.IsNullOrWhiteSpace(str)) {
				throw new BotError($"Argument `{argName}` cannot be null or empty.");
			}
		}

		private static IEnumerable<string> SplitIfNeeded(string str, int maxChunkSize)
		{
			for(int i = 0; i < str.Length; i += maxChunkSize) {
				yield return str.Substring(i, Math.Min(maxChunkSize, str.Length - i));
			}
		}
	}
}
