using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Discord;
using Discord.WebSocket;
using MopBotTwo.Core.Systems.Channels;
using MopBotTwo.Core.Systems.Memory;


namespace MopBotTwo.Core.Systems
{
	[SystemConfiguration(AlwaysEnabled = true,Description = "Internal system that forwards message events to other systems.")]
	public class MessageSystem : BotSystem
	{
		public static List<ulong> messagesToIgnore = new List<ulong>();

		public bool notifiedAboutStart;

		/*public override async Task Initialize()
		{
			newMessageBuffer = new List<Message>();
			newMessages = new Dictionary<SocketGuild,List<Message>>();
			newReactionsBuffer = new List<(Message,SocketReaction,SocketGuild)>();
			newReactions = new Dictionary<SocketGuild,List<(Message,SocketReaction,SocketGuild)>>();
		}*/
		public override async Task<bool> Update()
		{
			if(!notifiedAboutStart && MopBot.client.Guilds.Count>0) {
				foreach(var server in MopBot.client.Guilds) {
					if(MemorySystem.memory[server].GetData<ChannelSystem,ChannelServerData>().GetChannelByRole(ChannelRole.Logs) is ITextChannel logsChannel) {
						await logsChannel.SendMessageAsync($"MopBot started. {Utils.Choose("Howdy, pardner!","Heya!","Heyooo!","hi.","o hey, didn't se ya ther.","Soo, how are things?","quack.")}");
					}
				}

				notifiedAboutStart = true;
			}

			return true;
		}

		public static void IgnoreMessage(IMessage message)
		{
			if(message!=null) {
				messagesToIgnore.Add(message.Id);
			}
		}

		public static async Task MessageReceived(SocketMessage message)
		{
			if(!DiscordConnectionSystem.isFullyReady || messagesToIgnore.Contains(message.Id)) {
				return;
			}

			MessageExt newMessage = new MessageExt(message);
			if(newMessage.server==null) {
				return;
			}

			Console.WriteLine($"MessageReceived - '{newMessage.server.Name}' -> #{newMessage.messageChannel.Name} -> {newMessage.user.Username}#{newMessage.user.Discriminator}: {newMessage.content}");
			
			await CallForEnabledSystems(newMessage.server,s => s.OnMessageReceived(newMessage));
		}
		public static async Task MessageDeleted(Cacheable<IMessage,ulong> cachedMessage,ISocketMessageChannel channel)
		{
			if(!DiscordConnectionSystem.isFullyReady) {
				return;
			}

			IMessage message;

			try {
				message = await cachedMessage.GetOrDownloadAsync();
				if(message==null) {
					return;
				}
			}
			catch {
				return;
			}

			MessageExt newMessage = new MessageExt(message);

			if(newMessage.server!=null) {
				Console.WriteLine($"MessageDeleted - '{newMessage.server.Name}' -> #{newMessage.messageChannel.Name} -> {newMessage.user.Username}#{newMessage.user.Discriminator}: {newMessage.content}");

				await CallForEnabledSystems(newMessage.server,s => s.OnMessageDeleted(newMessage));
			} else {
				Console.WriteLine($"MessageDeleted - PMs -> {newMessage.user.Username}#{newMessage.user.Discriminator}: {newMessage.content}");
			}
		}

		public static async Task ReactionAdded(Cacheable<IUserMessage,ulong> cachedMessage,ISocketMessageChannel channel,SocketReaction reaction)
		{
			if(!DiscordConnectionSystem.isFullyReady) {
				return;
			}

			var userMessage = await cachedMessage.GetOrDownloadAsync();
			if(userMessage==null) {
				return;
			}
			
			var newMessage = new MessageExt(userMessage);
			
			Console.WriteLine($"ReactionAdded - '{newMessage.server.Name}' -> #{newMessage.messageChannel.Name} -> {newMessage.user.Username}#{newMessage.user.Discriminator}: {reaction.Emote.Name}");
			
			await CallForEnabledSystems(newMessage.server,s => s.OnReactionAdded(newMessage,reaction));
		}
		public static async Task MessageUpdated(Cacheable<IMessage,ulong> arg1,SocketMessage arg2,ISocketMessageChannel arg3)
		{
			/*if(!DiscordConnectionSystem.isFullyReady) {
				return;
			}*/
		}
	}
}