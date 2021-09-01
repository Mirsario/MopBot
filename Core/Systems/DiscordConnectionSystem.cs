using System;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

#pragma warning disable CS1998 //Async method lacks 'await' operators and will run synchronously

namespace MopBot.Core.Systems
{
	[SystemConfiguration(AlwaysEnabled = true, Description = "Controls connection to discord.")]
	public class DiscordConnectionSystem : BotSystem
	{
		public static bool isFullyReady;

		public DiscordSocketClient client;
		public string lastConsoleWrite;

		public override async Task Initialize()
		{
			if (string.IsNullOrWhiteSpace(GlobalConfiguration.config.token)) {
				throw new ArgumentException($"Add bot's token to '{GlobalConfiguration.ConfigurationFile}' file.");
			}

			MopBot.client = client = new DiscordSocketClient(new DiscordSocketConfig() {
				LogLevel = LogSeverity.Debug,
				MessageCacheSize = 1000,
				AlwaysDownloadUsers = true
			});

			MopBot.OnClientInit(client);

			await MopBot.TryCatchLogged("Attempting Login...", () => client.LoginAsync(TokenType.Bot, GlobalConfiguration.config.token.Trim()));
			await MopBot.TryCatchLogged("Attempting Connection...", () => client.StartAsync());
		}

		public override async Task<bool> Update()
		{
			void Write(string text)
			{
				if (lastConsoleWrite != text) {
					Console.WriteLine(text);

					lastConsoleWrite = text;
				}
			}

			if (client.LoginState == LoginState.LoggedIn && !isFullyReady) {
				Write("Ready!");

				isFullyReady = true;
			}

			if (isFullyReady) {
				lastConsoleWrite = null;

				return true;
			}

			return false;
		}
	}
}
