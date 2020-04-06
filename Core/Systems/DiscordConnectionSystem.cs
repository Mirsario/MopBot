using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;


namespace MopBotTwo.Core.Systems
{
	[SystemConfiguration(AlwaysEnabled = true,Description = "Controls connection to discord.")]
	public class DiscordConnectionSystem : BotSystem
	{
		public static bool isFullyReady;

		public DiscordSocketClient client;
		public string lastConsoleWrite;
		public bool started;

		public override async Task Initialize()
		{
			if(string.IsNullOrWhiteSpace(GlobalConfiguration.config.token)) {
				throw new ArgumentException($"Add bot's token to '{GlobalConfiguration.ConfigurationFile}' file.");
			}
			
			MopBot.client = client = new DiscordSocketClient(new DiscordSocketConfig() {
				LogLevel = LogSeverity.Debug,
				MessageCacheSize = 1000
			});

			await MopBot.OnClientInit(client);

			await MopBot.TryCatchLogged("Attempting Login...",() => client.LoginAsync(TokenType.Bot,GlobalConfiguration.config.token.Trim()));
			
			await MopBot.TryCatchLogged("Attempting Connection...",() => client.StartAsync());
		}
		public override async Task<bool> Update()
		{
			if(client.LoginState!=LoginState.LoggedIn || client.ConnectionState!=ConnectionState.Connected) {
				if(started) {
					Console.WriteLine("Trying to restart client...");

					Console.Write("Disposing it... ");

					try {
						client.Dispose();

						client = null;

						Console.WriteLine("Success.");
					}
					catch(Exception e) {
						Console.WriteLine("Fail.");

						Console.WriteLine($"{e.GetType().Name}: {e.Message}");
					}

					started = false;
					isFullyReady = false;

					Console.Write("Reinitializing... ");

					try {
						await Initialize();

						Console.WriteLine("Success.");
					}
					catch(Exception e) {
						Console.WriteLine("Fail.");

						Console.WriteLine($"{e.GetType().Name}: {e.Message}");
					}
				}
				
				return false;
			}

			started = true;

			void SmartWrite(string text)
			{
				if(lastConsoleWrite!=text) {
					Console.WriteLine(text);

					lastConsoleWrite = text;
				}
			}

			if(client.LoginState==LoginState.LoggedIn && !isFullyReady) {
				if(client.Guilds.Count==0 || client.Guilds.Contains(null)) {
					SmartWrite("Waiting for servers...");
					return false;
				}

				foreach(var server in client.Guilds) {
					void TryWrite(string part) => SmartWrite($"Awaiting information about server '{server.Name}' / {server.Id} ({part})...");

					var channels = server.Channels;

					if(channels==null || channels.Count==0 || channels.Any(c => c==null || c.Name==null || c.Users==null)) {
						TryWrite("Channels");

						return false;
					}

					if(server.EveryoneRole==null) {
						TryWrite("Roles");

						return false;
					}
				}

				SmartWrite("Ready!");

				isFullyReady = true;
			}

			if(isFullyReady) {
				lastConsoleWrite = null;
				return true;
			}

			return false;
		}
	}
}