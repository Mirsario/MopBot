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

		public static bool MessageIgnored(ulong id) => messagesToIgnore.Contains(id);
		public static void IgnoreMessage(ulong id) => messagesToIgnore.Add(id);
		public static void IgnoreMessage(IMessage message)
		{
			if(message!=null) {
				IgnoreMessage(message.Id);
			}
		}

		public static async Task MessageReceived(SocketMessage message)
		{
			if(!DiscordConnectionSystem.isFullyReady || MessageIgnored(message.Id)) {
				return;
			}

			MessageExt newMessage = new MessageExt(message);

			if(newMessage.server==null) {
				return;
			}

			Console.ForegroundColor = ConsoleColor.Green;
			Console.WriteLine($"MessageReceived - '{newMessage.server.Name}' -> #{newMessage.messageChannel.Name} -> {newMessage.user.Username}#{newMessage.user.Discriminator}: {newMessage.content}");
			Console.ResetColor();

			await CallForEnabledSystems(newMessage.server,s => s.OnMessageReceived(newMessage));
		}
		public static async Task MessageUpdated(Cacheable<IMessage,ulong> cachedMessage,SocketMessage currentMessage,ISocketMessageChannel channel)
		{
			if(!DiscordConnectionSystem.isFullyReady || !cachedMessage.HasValue || MessageIgnored(currentMessage.Id)) {
				return;
			}

			var context = new MessageExt(currentMessage);

			if(context.server!=null) {
				await CallForEnabledSystems(context.server,s => s.OnMessageUpdated(context,cachedMessage.Value));
			}
		}
		public static async Task MessageDeleted(Cacheable<IMessage,ulong> cachedMessage,ISocketMessageChannel channel)
		{
			if(!DiscordConnectionSystem.isFullyReady || !cachedMessage.HasValue) {
				return;
			}

			var message = cachedMessage.Value;

			if(MessageIgnored(message.Id)) {
				return;
			}

			var context = new MessageExt(message);

			if(context.server!=null) {
				Console.WriteLine($"MessageDeleted - '{context.server.Name}' -> #{context.messageChannel.Name} -> {context.user.Username}#{context.user.Discriminator}: {context.content}");

				await CallForEnabledSystems(context.server,s => s.OnMessageDeleted(context));
			} else {
				Console.WriteLine($"MessageDeleted - PMs -> {context.user.Username}#{context.user.Discriminator}: {context.content}");
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
	}
}