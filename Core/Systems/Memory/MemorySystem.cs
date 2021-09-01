using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Collections.Generic;
using System.Net;
using Discord;
using Discord.Commands;
using Newtonsoft.Json;
using MopBot.Extensions;
using MopBot.Core.Systems.Permissions;

namespace MopBot.Core.Systems.Memory
{
	//TODO: Use a database instead of JSON.
	//TODO: Much of these get & set commands should be made into utilility methods, since TriviaSystem.cs has similar code repeated.

	[Group("memory")]
	[Summary("Group for controlling bot's memory and settings.")]
	[RequirePermission(SpecialPermission.BotMaster)] //Only bot's developers can access commands from this.
	[SystemConfiguration(AlwaysEnabled = true, Description = "Internal system that handles bot's memory.")]
	public class MemorySystem : BotSystem
	{
		public const string MemoryFile = "BotMemory.json";
		public const string BackupDirectory = "MemoryBackups";
		public const string TempMemoryFile = "TempMemoryCopy.json";

		public static Dictionary<(Type memoryType, string systemName), (BotSystem instance, Type dataType)> dataProvaiderInfo = new Dictionary<(Type, string), (BotSystem, Type)>();

		public static MemorySystem instance;
		public static Memory memory;
		public static bool forceSave;
		public static bool canSave;
		public static bool loadedMemory;

		public DateTime lastSave;

		public override async Task Initialize()
		{
			instance = this;
			lastSave = DateTime.Now;

			await Load();

			canSave = true;
		}

		public override async Task<bool> Update()
		{
			if (!loadedMemory) {
				return false;
			}

			if (forceSave || (DateTime.Now - lastSave).TotalSeconds >= 60) {
				await Save();

				forceSave = false;
			}

			return true;
		}

		public async Task Save()
		{
			if (!canSave) {
				return;
			}

			if (File.Exists(MemoryFile)) {
				Directory.CreateDirectory(BackupDirectory);
				File.Copy(MemoryFile, Path.Combine(BackupDirectory, $"{DateTime.Now:yyyy-MM-dd-HH:mm:ss}.json"), true);

				//Delete some backups if there's too many
				if (GlobalConfiguration.config.maxMemoryBackups > 0) {
					var directoryInfo = new DirectoryInfo(BackupDirectory);
					var files = directoryInfo.GetFiles("*.json");

					if (files != null && files.Length > GlobalConfiguration.config.maxMemoryBackups) {
						foreach (var file in files.OrderByDescending(f => f.LastWriteTime).TakeLast(files.Length - GlobalConfiguration.config.maxMemoryBackups)) {
							File.Delete(file.FullName);
						}
					}
				}
			}

			await MopBot.TryCatch(() => MemoryBase.Save(memory, MemoryFile));

			lastSave = DateTime.Now;
		}

		public async Task Load()
		{
			loadedMemory = false;

			var stopwatch = new Stopwatch();

			stopwatch.Start();

			await MopBot.TryCatchLogged("Loading memory...", async () => {
				memory = await MemoryBase.Load<Memory>(MemoryFile);
			});

			stopwatch.Stop();

			Console.WriteLine($"Took {stopwatch.ElapsedMilliseconds}ms.");

			if (memory == null) {
				Console.WriteLine("Looking for a memory backup...");

				if (Directory.Exists(BackupDirectory)) {
					var directoryInfo = new DirectoryInfo(BackupDirectory);
					var files = directoryInfo.GetFiles("*.json");

					if (files != null && files.Length > 0) {
						var sortedFiles = files.OrderByDescending(f => f.LastWriteTime).ToArray();

						for (int i = 0; i < sortedFiles.Length; i++) {
							var file = sortedFiles[i];

							Console.Write($"Trying backup '{file.Name}'... ");

							memory = await MemoryBase.Load<Memory>(file.FullName);

							if (memory != null) {
								Console.WriteLine("Success! We're saved?");
								break;
							}

							Console.WriteLine("Failure!");
						}
					}
				}

				if (memory == null) {
					Console.WriteLine("Out of backups! This is not good. Resetting memory...");

					memory = new Memory();
					memory.Initialize();
				}

			}

			loadedMemory = true;
		}

		[Command("get")]
		public Task GetMemoryCommand(bool sendInThisChannel = false)
		{
			return GetTextFileCommand(
				sendInThisChannel,
				Context.server.GetMemory().ToString(Formatting.Indented),
				"TempMemoryCopy.json",
				"Here's a portion of my current memory, specific to this server."
			);
		}

		[Command("set")]
		public async Task SetMemoryCommand(string url = null)
		{
			var server = Context.server;

			if (server == null) {
				return;
			}

			if (!Context.socketMessage.Attachments.TryGetFirst(a => a.Filename.EndsWith(".txt") || a.Filename.EndsWith(".json"), out Attachment file) && url == null) {
				await Context.ReplyAsync("Expected a .json file attachment or a link to it.");
				return;
			}

			string urlString = file?.Url ?? url;

			if (!Uri.TryCreate(urlString, UriKind.Absolute, out Uri uri)) {
				await Context.ReplyAsync($"Invalid Url: `{urlString}`.");
				return;
			}

			using (var client = new WebClient()) {
				try {
					client.DownloadFile(uri, TempMemoryFile);
				}
				catch (Exception e) {
					await Context.ReplyAsync("An exception has occured during file download.");
					await MopBot.HandleException(e);

					if (File.Exists(TempMemoryFile)) {
						File.Delete(TempMemoryFile);
					}

					return;
				}
			}

			var serverMemory = memory[server];

			try {
				memory[server] = await MemoryBase.Load<ServerMemory>(TempMemoryFile, serverMemory.id, false) ?? throw new InvalidDataException();
			}
			catch (Exception e) {
				await Context.ReplyAsync("There was something wrong with the json file you provided.");

				if (!(e is InvalidDataException)) {
					await MopBot.HandleException(e);
				}

				return;
			}

			forceSave = true;

			File.Delete(TempMemoryFile);

			await Context.ReplyAsync("Done!");
		}

		[Command("getall")]
		public Task GetAllMemoryCommand(bool sendInThisChannel = false)
		{
			return GetTextFileCommand(
				sendInThisChannel,
				memory.ToString(Formatting.Indented),
				"TempMemoryCopy.json",
				"Here's my whole current memory file."
			);
		}

		[Command("setall")]
		public async Task SetAllMemoryCommand(string url = null)
		{
			var server = Context.server;

			if (server == null) {
				return;
			}

			if (!Context.socketMessage.Attachments.TryGetFirst(a => a.Filename.EndsWith(".txt") || a.Filename.EndsWith(".json"), out Attachment file) && url == null) {
				await Context.ReplyAsync("Expected a .json file attachment or a link to it.");
				return;
			}

			string urlString = file?.Url ?? url;

			if (!Uri.TryCreate(urlString, UriKind.Absolute, out Uri uri)) {
				await Context.ReplyAsync($"Invalid Url: `{urlString}`.");
				return;
			}

			using (var client = new WebClient()) {
				try {
					client.DownloadFile(uri, TempMemoryFile);
				}
				catch (Exception e) {
					await Context.ReplyAsync("An exception has occured during file download.");
					await MopBot.HandleException(e);

					if (File.Exists(TempMemoryFile)) {
						File.Delete(TempMemoryFile);
					}

					return;
				}
			}

			var serverMemory = memory[server];

			try {
				memory = await MemoryBase.Load<Memory>(TempMemoryFile, serverMemory.id, false) ?? throw new InvalidDataException();
				GC.Collect();
			}
			catch (Exception e) {
				await Context.ReplyAsync("There was something wrong with the json file you provided.");

				if (!(e is InvalidDataException)) {
					await MopBot.HandleException(e);
				}

				return;
			}

			forceSave = true;

			File.Delete(TempMemoryFile);

			await Context.ReplyAsync("Done!");
		}

		[Command("save")]
		public async Task SaveMemoryCommand()
		{
			await Save();
			await Context.ReplyAsync(canSave ? "Memory has been successfully saved." : "Failed to save memory.");
		}

		[Command("reload")]
		public async Task ReloadMemoryCommand()
		{
			await Load();
			await Context.ReplyAsync("Memory has been successfully reloaded.");
		}

		private async Task GetTextFileCommand(bool postHere, string text, string fileName, string message)
		{
			var dmChannel = postHere ? null : await Context.socketUser.CreateDMChannelAsync();
			IMessageChannel textChannel;

			if (!postHere && dmChannel == null) {
				await Context.ReplyAsync("Unable to send you a private message.");
				return;
			}

			textChannel = postHere ? Context.socketServerChannel as IMessageChannel : dmChannel;

			var bytes = Encoding.UTF8.GetBytes(text);

			using MemoryStream stream = new MemoryStream(bytes);

			await textChannel.SendFileAsync(stream, "TempMemoryCopy.json", message);
		}
	}
}
