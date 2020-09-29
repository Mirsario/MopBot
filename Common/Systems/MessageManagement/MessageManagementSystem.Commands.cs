﻿using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Discord.Commands;
using MopBot.Extensions;
using MopBot.Core.Systems.Permissions;

namespace MopBot.Common.Systems.MessageManagement
{
	public partial class MessageManagementSystem
	{
		[Command("quote")]
		[RequirePermission(SpecialPermission.Owner, "messagemanaging.quote")]
		public async Task QuoteMessagesCommand(ulong messageId, int numMessages)
		{
			var channel = Context.socketTextChannel;

			await CopyMessagesInternal(numMessages, channel, messageId);
		}
		[Command("copy")]
		[RequirePermission(SpecialPermission.Owner, "messagemanaging.copy")]
		public async Task CopyMessagesCommand(int numMessages, SocketGuildChannel channel, ulong bottomMessageId = 0)
		{
			if(!(channel is ITextChannel toChannel)) {
				throw new BotError($"<#{channel.Id}> isn't a text channel.");
			}

			await CopyMessagesInternal(numMessages, toChannel, bottomMessageId);
		}
		[Command("move")]
		[RequirePermission(SpecialPermission.Owner, "messagemanaging.move")]
		public async Task MoveMessagesCommand(int numMessages, SocketGuildChannel channel, ulong bottomMessageId = 0)
		{
			var context = Context;
			context.server.CurrentUser.RequirePermission(channel, DiscordPermission.ManageMessages);

			if(!(channel is ITextChannel toChannel)) {
				throw new BotError($"<#{channel.Id}> isn't a text channel.");
			}

			var messageList = await CopyMessagesInternal(numMessages, toChannel, bottomMessageId);

			try {
				await context.socketTextChannel.DeleteMessagesAsync(messageList);
			}
			catch(Exception e) {
				await MopBot.HandleException(e);
				await context.ReplyAsync($"Error deleting messages: ```{string.Join("\r\n", messageList.Select(m => m == null ? "NULL" : m.Id.ToString()))}```");
			}
		}

		/*[Command("send")] [Alias("say")]
		[RequirePermission(SpecialPermission.Owner,"messagemanaging.send")]
		public async Task SendMessageCommand([Remainder]string text)
		{
			Utils.RemoveQuotemarks(ref text);
			await Context.socketTextChannel.SendMessageAsync(text);
		}*/
		[Command("send")]
		[Alias("say")]
		[Priority(-1)]
		[RequirePermission(SpecialPermission.Owner, "messagemanaging.send")]
		public Task SendMessageCommand([Remainder] string text) => SendMessageInternal(Context.socketTextChannel, text);

		[Command("send")]
		[Alias("say")]
		[RequirePermission(SpecialPermission.Owner, "messagemanaging.send")]
		public Task SendMessageCommand(SocketTextChannel textChannel, [Remainder] string text) => SendMessageInternal(textChannel, text);

		private async Task SendMessageInternal(SocketTextChannel textChannel, string text)
		{
			StringUtils.RemoveQuotemarks(ref text);

			await textChannel.SendMessageAsync(text);
		}
	}
}
