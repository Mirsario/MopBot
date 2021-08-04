using Discord;
using Discord.Commands;
using Discord.WebSocket;
using MopBot.Core;
using MopBot.Core.Systems;
using MopBot.Core.Systems.Memory;
using MopBot.Extensions;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MopBot.Common.Systems.Logging
{
	[Group("logging")]
	[Summary("Group for controlling LoggingSystem")]
	[SystemConfiguration(Description = "Allows setting up logging of things like kicks, bans, role changes, message edits & deletes, etc.")]
	public partial class LoggingSystem : BotSystem
	{
		public override void RegisterDataTypes()
		{
			RegisterDataType<ServerMemory, LoggingServerData>();
		}

		public override async Task OnUserJoined(SocketGuildUser user)
		{
			var server = user.Guild;
			var serverData = server.GetMemory().GetData<LoggingSystem, LoggingServerData>();

			if(!serverData.TryGetLoggingChannel(server, out var loggingChannel)) {
				return;
			}

			var embed = GetUserEmbed(server, user)
				.WithTitle($"User joined")
				.WithDescription(user.Mention)
				.Build();

			await loggingChannel.SendMessageAsync(embed: embed);
		}
		public override async Task OnUserLeft(SocketGuildUser user)
		{
			var server = user.Guild;
			var serverData = server.GetMemory().GetData<LoggingSystem, LoggingServerData>();

			if(!serverData.TryGetLoggingChannel(server, out var loggingChannel)) {
				return;
			}

			var embed = GetUserEmbed(server, user)
				.WithTitle($"User left")
				.WithDescription(user.Mention)
				.Build();

			await loggingChannel.SendMessageAsync(embed: embed);
		}
		public override async Task OnUserUpdated(Cacheable<SocketGuildUser, ulong> oldUserCached, SocketGuildUser newUser)
		{
			if(!DiscordConnectionSystem.isFullyReady) {
				return;
			}

			if (!oldUserCached.HasValue) {
				return;
			}

			var oldUser = oldUserCached.Value;

			string mention = $"**Mention:** {newUser.Mention}";

			if(oldUser.Username != newUser.Username) {
				await TrySendEmbed(newUser.Guild, newUser, embed => embed
					  .WithTitle("Username updated")
					  .WithDescription($"**Username:** `{oldUser.Username}#{oldUser.Discriminator}` **->** `{newUser.Username}#{newUser.Discriminator}`\r\n{mention}")
				);
			}

			if(oldUser.Nickname != newUser.Nickname) {
				await TrySendEmbed(newUser.Guild, newUser, embed => embed
					  .WithTitle("Nickname updated")
					  .WithDescription($"**Nickname:** `{oldUser.Nickname}` **->** {(newUser.Nickname != null ? $"`{newUser.Nickname}`" : "**None**")}\r\n{mention}")
				);
			}

			if(oldUser.AvatarId != newUser.AvatarId) {
				await TrySendEmbed(newUser.Guild, newUser, embed => embed
					  .WithTitle("Avatar updated")
					  .WithDescription(mention)
					  .WithThumbnailUrl(newUser.GetAvatarUrl() ?? newUser.GetDefaultAvatarUrl())
				);
			}

			var oldRoles = oldUser.Roles;
			var newRoles = newUser.Roles;

			if(oldRoles.Count != newRoles.Count) {
				//TODO: Double double loops could be avoided.

				foreach(var role in oldRoles) {
					ulong roleId = role.Id;

					if(!newRoles.Any(r => r.Id == roleId)) {
						await TrySendEmbed(newUser.Guild, newUser, embed => embed
							  .WithTitle("Role removed from user")
							  .WithDescription($"**Role:** {role.Mention}\r\n{mention}")
						);
					}
				}

				foreach(var role in newRoles) {
					ulong roleId = role.Id;

					if(!oldRoles.Any(r => r.Id == roleId)) {
						await TrySendEmbed(newUser.Guild, newUser, embed => embed
							  .WithTitle("Role added to user")
							  .WithDescription($"**Role:** {role.Mention}\r\n{mention}")
						);
					}
				}
			}
		}
		public override async Task OnMessageUpdated(MessageContext context, IMessage oldMessage)
		{
			if(context.User.IsBot || MessageSystem.MessageIgnored(oldMessage.Id)) {
				return;
			}

			var newMessage = context.Message;

			if(newMessage.Content == oldMessage.Content) {
				return;
			}

			await TrySendEmbed(context, embed => embed
				 .WithTitle($"Message updated in #{context.Channel.Name}")
				 .WithDescription($"**Before:** {oldMessage.Content}\r\n**‎‎After:**  ឵ {newMessage.Content}\r\n[[Jump to message]]({newMessage.GetJumpUrl()})") //TODO: This uses blank characters that should be put into an util method.
			);
		}
		public override async Task OnMessageDeleted(MessageContext context)
		{
			if(!context.User.IsBot && !MessageSystem.MessageIgnored(context.Message.Id)) {
				await TrySendEmbed(context, embed => embed
					 .WithTitle($"Message deleted in #{context.Channel.Name}")
					 .WithDescription(context.Message.Content)
				);
			}
		}

		private static Task TrySendEmbed(MessageContext context, Func<EmbedBuilder, EmbedBuilder> embedFunc) => TrySendEmbed(context.server, context.user, embedFunc);
		private static async Task TrySendEmbed(SocketGuild server, IUser user, Func<EmbedBuilder, EmbedBuilder> embedFunc)
		{
			var serverData = server.GetMemory().GetData<LoggingSystem, LoggingServerData>();

			if(!serverData.TryGetLoggingChannel(server, out var loggingChannel)) {
				return;
			}

			var embed = embedFunc(GetUserEmbed(server, user))?.Build();

			if(embed != null) {
				await loggingChannel.SendMessageAsync(embed: embed);
			}
		}

		private static EmbedBuilder GetUserEmbed(SocketGuild server, IUser user) => MopBot.GetEmbedBuilder(server)
			.WithAuthor(user)
			.WithFooter($"User ID: {user.Id}")
			.WithCurrentTimestamp();
	}
}
