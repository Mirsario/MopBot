using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Discord.Commands;
using Discord.Rest;
using MopBot.Extensions;
using MopBot.Core.Systems.Commands;
using MopBot.Core.Systems;

namespace MopBot.Core
{
	public class MessageContext : ICommandContext
	{
		//TODO: how did this mess happen?

		public IMessage message;
		public SocketMessage socketMessage;
		public RestUserMessage restMessage;
		//User
		public IUser user;
		public SocketUser socketUser;
		public SocketGuildUser socketServerUser;
		//Message
		public ISocketMessageChannel socketMessageChannel;
		public SocketGuildChannel socketServerChannel;
		public SocketTextChannel socketTextChannel;
		public IMessageChannel messageChannel;
		//Server
		public SocketGuild server;
		//Etc
		public string content;
		public bool isCommand;
		public bool messageDeleted;

		public IDiscordClient Client => MopBot.client;
		public IGuild Guild => server;
		public IMessageChannel Channel => messageChannel;
		public IUser User => user;
		public IUserMessage Message => (IUserMessage)message;

		public MessageContext(IMessage msg)
		{
			switch (msg) {
				case RestUserMessage rMsg:
					Setup(rMsg);
					break;
				case SocketUserMessage sMsg:
					Setup(sMsg);
					break;
				case SocketSystemMessage sysMsg:
					Setup(sysMsg);
					break;
				default:
					throw new ArgumentException($"Not support message type: {msg.GetType().Name}");
			}
		}

		public MessageContext(SocketMessage message) => Setup(message);

		public MessageContext(RestUserMessage message) => Setup(message);

		public MessageContext(IMessage message, SocketGuild server, SocketGuildUser user, string content = null, bool? isCommand = null, SocketTextChannel channel = null)
		{
			this.message = message;
			this.server = server;
			this.user = user;
			this.content = content ?? "";

			messageChannel = channel;
			socketTextChannel = channel;

			Setup();

			this.isCommand = isCommand ?? this.content.StartsWith(server?.GetMemory()?.GetData<CommandSystem, CommandServerData>()?.commandPrefix ?? MopBot.DefaultCommandPrefix);
		}

		private void Setup()
		{
			socketServerUser = user as SocketGuildUser;
			socketUser = user as SocketUser;
		}

		private void Setup(SocketMessage message)
		{
			//Message
			this.message = socketMessage = message;
			//User
			user = socketUser = message.Author;
			socketServerUser = socketUser as SocketGuildUser;
			//Channel
			messageChannel = socketMessageChannel = message.Channel;
			socketServerChannel = socketMessageChannel as SocketGuildChannel;
			socketTextChannel = socketServerChannel as SocketTextChannel;
			//Server
			server = socketServerChannel?.Guild;
			//Other
			content = message.Content ?? "";
			isCommand = content.StartsWith(server?.GetMemory()?.GetData<CommandSystem, CommandServerData>()?.commandPrefix ?? MopBot.DefaultCommandPrefix);
		}

		private void Setup(RestUserMessage message)
		{
			//Message
			this.message = restMessage = message;
			//User
			user = message.Author;
			//Channel
			messageChannel = message.Channel;
			//Server
			server = MopBot.client.Guilds.FirstOrDefault(s => s.Channels.Any(c => c.Id == messageChannel.Id));
			//Other
			content = message.Content ?? "";
			isCommand = content.StartsWith(server?.GetMemory()?.GetData<CommandSystem, CommandServerData>()?.commandPrefix ?? MopBot.DefaultCommandPrefix);
		}

		public void AddInfo(SocketGuild server)
		{
			this.server = server;
		}

		public async Task Delete()
		{
			ulong id = message.Id;

			if (!MessageSystem.MessageIgnored(id)) {
				MessageSystem.IgnoreMessage(id);

				if (socketTextChannel == null || server.CurrentUser.HasChannelPermission(socketTextChannel, DiscordPermission.ManageMessages)) {
					await message.DeleteAsync();
				}
			}
		}
	}
}
