using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Discord;
using Discord.WebSocket;
using MopBot.Core.Systems.Channels;
using MopBot.Core.Systems.Memory;

namespace MopBot.Core.Systems
{
	[SystemConfiguration(AlwaysEnabled = true, Description = "Internal system that forwards message events to other systems.")]
	public class MessageSystem : BotSystem
	{
		public static List<ulong> messagesToIgnore = new List<ulong>();

		public bool notifiedAboutStart;

		public override async Task<bool> Update()
		{
			if (!notifiedAboutStart && MopBot.client.Guilds.Count > 0) {
				foreach (var server in MopBot.client.Guilds) {
					if (MemorySystem.memory[server].GetData<ChannelSystem, ChannelServerData>().GetChannelByRole(ChannelRole.Logs) is ITextChannel logsChannel) {
						await logsChannel.SendMessageAsync($"MopBot started. {Utils.Choose("Greetings.", "Howdy, pardner!", "Heya!", "Heyooo!", "hi.", "oh hey, didn't see ya there.", "Soo, how are things?", "quack.", "I am here now.")}");
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
			if (message != null) {
				IgnoreMessage(message.Id);
			}
		}

		public static async Task MessageReceived(SocketMessage message)
		{
			if (!DiscordConnectionSystem.isFullyReady || MessageIgnored(message.Id)) {
				return;
			}

			MessageContext newMessage = new MessageContext(message);

			if (newMessage.server == null) {
				Console.WriteLine($"Message Received - PMs -> {newMessage.user.Username}#{newMessage.user.Discriminator}: {newMessage.content}");
				return;
			}

			await CallForEnabledSystems(newMessage.server, s => s.OnMessageReceived(newMessage));
		}

		public static async Task MessageUpdated(Cacheable<IMessage, ulong> cachedMessage, SocketMessage currentMessage, ISocketMessageChannel channel)
		{
			if (!DiscordConnectionSystem.isFullyReady || !cachedMessage.HasValue || MessageIgnored(currentMessage.Id)) {
				return;
			}

			var context = new MessageContext(currentMessage);

			if (context.server != null) {
				await CallForEnabledSystems(context.server, s => s.OnMessageUpdated(context, cachedMessage.Value));
			}
		}

		public static async Task MessageDeleted(Cacheable<IMessage, ulong> cachedMessage, Cacheable<IMessageChannel, ulong> channel)
		{
			if (!DiscordConnectionSystem.isFullyReady || !cachedMessage.HasValue) {
				return;
			}

			var message = cachedMessage.Value;

			if (MessageIgnored(message.Id)) {
				return;
			}

			var context = new MessageContext(message);

			if (context.server == null) {
				Console.WriteLine($"Message Deleted - PMs -> {context.user.Username}#{context.user.Discriminator}: {context.content}");
				return;
			}

			await CallForEnabledSystems(context.server, s => s.OnMessageDeleted(context));
		}

		public static async Task ReactionAdded(Cacheable<IUserMessage, ulong> cachedMessage, Cacheable<IMessageChannel, ulong> channel, SocketReaction reaction)
		{
			if (!DiscordConnectionSystem.isFullyReady) {
				return;
			}

			var userMessage = await cachedMessage.GetOrDownloadAsync();

			if (userMessage == null) {
				return;
			}

			var newMessage = new MessageContext(userMessage);

			await CallForEnabledSystems(newMessage.server, s => s.OnReactionAdded(newMessage, reaction));
		}
	}
}
