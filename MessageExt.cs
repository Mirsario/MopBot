using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Discord.Commands;
using Discord.Rest;
using MopBotTwo.Extensions;
using MopBotTwo.Core.Systems.Commands;

namespace MopBotTwo
{
	public class MessageExt : ICommandContext
	{
		//TODO: how did this mess happen?
		
		public IMessage message;
		public SocketMessage socketMessage;
		public RestUserMessage restMessage;

		public IUser user;
		public SocketUser socketUser;
		public SocketGuildUser socketServerUser;

		public ISocketMessageChannel socketMessageChannel;
		public SocketGuildChannel socketServerChannel;
		public SocketTextChannel socketTextChannel;
		public IMessageChannel messageChannel;

		public SocketGuild server;
		public string content;
		public bool isCommand;
		public bool messageDeleted;
		
		public IDiscordClient Client => MopBot.client;
		public IGuild Guild => server;
		public IMessageChannel Channel => messageChannel;
		public IUser User => user;
		public IUserMessage Message => (IUserMessage)message;

		public MessageExt(IMessage msg)
		{
			switch(msg) {
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
		public MessageExt(SocketMessage message) => Setup(message);
		public MessageExt(RestUserMessage message) => Setup(message);
		public MessageExt(IMessage message,SocketGuild server,SocketGuildUser user,string content = null,bool? isCommand = null,SocketTextChannel channel = null)
		{
			this.message = message;
			this.server = server;
			this.user = user;
			messageChannel = channel;
			socketTextChannel = channel;
			this.content = content ?? "";
			Setup();
			this.isCommand = isCommand ?? this.content.StartsWith(server?.GetMemory()?.GetData<CommandSystem,CommandServerData>()?.commandPrefix ?? MopBot.DefaultCommandPrefix);
			//Unfinished
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
			isCommand = content.StartsWith(server?.GetMemory()?.GetData<CommandSystem,CommandServerData>()?.commandPrefix ?? MopBot.DefaultCommandPrefix);
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
			server = MopBot.client.Guilds.FirstOrDefault(s => s.Channels.Any(c => c.Id==messageChannel.Id));
			//Other
			content = message.Content ?? "";
			isCommand = content.StartsWith(server?.GetMemory()?.GetData<CommandSystem,CommandServerData>()?.commandPrefix ?? MopBot.DefaultCommandPrefix);
		}

		public void AddInfo(SocketGuild server)
		{
			this.server = server;
		}

		public async Task Delete()
		{
			if(!messageDeleted) {
				if(socketTextChannel==null || server.CurrentUser.HasChannelPermission(socketTextChannel,DiscordPermission.ManageMessages)) {
					await message.DeleteAsync();
				}
				messageDeleted = true;
			}
		}
	}
}
