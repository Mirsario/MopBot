using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Discord.Commands;
using MopBotTwo.Extensions;

#pragma warning disable CS1998

namespace MopBotTwo.Systems
{
	public class ModerationCommandsSystem : BotSystem
	{
		[Command("ban")]
		[Summary("Bans a user")]
		[RequirePermission(SpecialPermission.Owner,"ban")]
		public async Task BanCommand(SocketGuildUser user,[Remainder]string reason = "No reason provided.")
		{
			await user.BanAsync(reason:reason);
			await Context.ReplyAsync($"User `{user.Name()}` has been banned with reason `{reason}`.");
		}

		[Command("clear")]
		[Summary("Removes a specified amount of messages.")]
		[RequirePermission(SpecialPermission.Owner,"clear")]
		public Task ClearCommand(uint amount)
			=> ClearCommand(Context.socketTextChannel,amount);

		[Command("clear")]
		[Summary("Removes a specified amount of messages.")]
		[RequirePermission(SpecialPermission.Owner,"clear")]
		public async Task ClearCommand(SocketTextChannel channel,uint amount)
		{
			var context = Context;
			var server = context.server;
			server.CurrentUser.RequirePermission(channel,DiscordPermission.ManageMessages);

			int highestRole = server.GetUser(MopBot.client.CurrentUser.Id).Roles.Max(r => r.Position);
			var utcNow = DateTime.UtcNow.AddMinutes(1); //+1 min
			var messages = 
				(await channel.GetMessagesAsync((int)amount+1).FlattenAsync())
				.Where(m => m!=null && (utcNow-m.Timestamp.UtcDateTime).TotalDays<14 && (m.Author?.Id==MopBot.client.CurrentUser.Id || (m.Author as SocketGuildUser)?.Roles?.All(r => r.Position<highestRole)==true));

			await channel.DeleteMessagesAsync(messages);
			context.messageDeleted = true;
		}
	}
}