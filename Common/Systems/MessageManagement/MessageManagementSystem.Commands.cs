using Discord.Commands;
using Discord.WebSocket;
using MopBot.Core.Systems.Permissions;
using MopBot.Extensions;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MopBot.Common.Systems.MessageManagement
{
	public partial class MessageManagementSystem
	{
		[Command("quote")]
		[RequirePermission(SpecialPermission.Admin, "messagemanaging.quote")]
		public async Task QuoteMessagesCommand(ulong messageId, int numMessages, bool allowGrouping = true)
		{
			var channel = Context.socketTextChannel;

			await CopyMessagesInternal(channel, numMessages, channel, messageId, allowGrouping);
		}

		[Command("copy")]
		[RequirePermission(SpecialPermission.Admin, "messagemanaging.copy")]
		public async Task CopyMessagesCommand(int numMessages, SocketTextChannel destinationChannel, ulong bottomMessageId = 0, bool allowGrouping = true)
			=> await CopyMessagesCommand(Context.socketTextChannel, numMessages, destinationChannel, bottomMessageId, allowGrouping);

		[Command("copy")]
		[RequirePermission(SpecialPermission.Admin, "messagemanaging.copy")]
		public async Task CopyMessagesCommand(SocketTextChannel sourceChannel, int numMessages, SocketTextChannel destinationChannel, ulong bottomMessageId = 0, bool allowGrouping = true)
			=> await CopyMessagesInternal(sourceChannel, numMessages, destinationChannel, bottomMessageId, allowGrouping);

		[Command("move")]
		[RequirePermission(SpecialPermission.Admin, "messagemanaging.move")]
		public async Task MoveMessagesCommand(int numMessages, SocketTextChannel destinationChannel, ulong bottomMessageId = 0, bool allowGrouping = true)
			=> await MoveMessagesCommand(Context.socketTextChannel, numMessages, destinationChannel, bottomMessageId, allowGrouping);

		[Command("move")]
		[RequirePermission(SpecialPermission.Admin, "messagemanaging.move")]
		public async Task MoveMessagesCommand(SocketTextChannel sourceChannel, int numMessages, SocketTextChannel destinationChannel, ulong bottomMessageId = 0, bool allowGrouping = true)
		{
			var context = Context;

			context.server.CurrentUser.RequirePermission(destinationChannel, DiscordPermission.ManageMessages);

			var messageList = await CopyMessagesInternal(sourceChannel, numMessages, destinationChannel, bottomMessageId, allowGrouping);

			try {
				await context.socketTextChannel.DeleteMessagesAsync(messageList);
			}
			catch(Exception e) {
				await MopBot.HandleException(e);
				await context.ReplyAsync($"Error deleting messages: ```{string.Join("\r\n", messageList.Select(m => m == null ? "NULL" : m.Id.ToString()))}```");
			}
		}

		[Command("send")]
		[Alias("say")]
		[Priority(-1)]
		[RequirePermission(SpecialPermission.Admin, "messagemanaging.send")]
		public Task SendMessageCommand([Remainder] string text) => SendMessageInternal(Context.socketTextChannel, text);

		[Command("send")]
		[Alias("say")]
		[RequirePermission(SpecialPermission.Admin, "messagemanaging.send")]
		public Task SendMessageCommand(SocketTextChannel textChannel, [Remainder] string text) => SendMessageInternal(textChannel, text);

		private async Task SendMessageInternal(SocketTextChannel textChannel, string text)
		{
			StringUtils.RemoveQuotemarks(ref text);

			await textChannel.SendMessageAsync(text);
		}
	}
}
