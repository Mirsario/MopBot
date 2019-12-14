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

			client.MessageReceived += MessageSystem.MessageReceived;
			client.MessageDeleted += MessageSystem.MessageDeleted;
			client.ReactionAdded += MessageSystem.ReactionAdded;
			client.MessageUpdated += MessageSystem.MessageUpdated;
			client.UserJoined += MopBot.UserJoined;
			client.UserLeft += MopBot.UserLeft;
			client.Ready += MopBot.OnReady;

			await MopBot.TryCatchLogged("Attempting Login...",() => client.LoginAsync(TokenType.Bot,GlobalConfiguration.config.token.Trim()));
			
			await MopBot.TryCatchLogged("Attempting Connection...",() => client.StartAsync());
		}
		public override async Task<bool> Update()
		{
			/*if(client.LoginState!=LoginState.LoggedIn && client.LoginState!=LoginState.LoggingIn) {
				isFullyReady = false;

				await MopBot.TryCatchLogged("Attempting Login...",() => client.LoginAsync(TokenType.Bot,GlobalConfiguration.config.token.Trim()));
			}
			if(client.LoginState==LoginState.LoggedIn && client.ConnectionState!=ConnectionState.Connected && client.ConnectionState!=ConnectionState.Connecting) {
				if(started) {
					await MopBot.TryCatchLogged("Disposing client and reinitializing it...",async () => {
						client.Dispose();
						started = false;
						isFullyReady = false;

						await Initialize();
					});
					return false;
				}
			}*/

			if(client.LoginState!=LoginState.LoggedIn || client.ConnectionState!=ConnectionState.Connected) {
				if(started) {
					Console.WriteLine("Trying to restart client...");

					await MopBot.TryCatchLogged("Disposing it...",async () => {
						client.Dispose();
					});
					client = null;

					started = false;
					isFullyReady = false;

					await MopBot.TryCatchLogged("Reinitializing...",async () => {
						await Initialize();
					});
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
					bool wrote = false;
					void TryWrite()
					{
						if(!wrote) {
							SmartWrite($"Awaiting information about server {server.Id}...");
							wrote = true;
						}
					}

					if(server.MemberCount>server.DownloadedMemberCount) {
						TryWrite(); //Console.WriteLine("Downloading Users...");
						await server.DownloadUsersAsync();
					}

					var channels = server.Channels;
					if(channels==null || channels.Count==0 || channels.Any(c => c==null || c.Name==null || c.Users==null)) {
						TryWrite(); //Console.WriteLine("Awaiting channels...");
						return false;
					}

					if(server.EveryoneRole==null) {
						TryWrite(); //Console.WriteLine("Awaiting roles...");
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