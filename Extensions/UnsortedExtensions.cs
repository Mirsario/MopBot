using Discord;
using Discord.Net;
using Discord.Rest;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Text;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection;
using System.Threading.Tasks;
using DColor = Discord.Color;
using MopBot.Core.Systems.Memory;
using MopBot.Core.Systems.Permissions;
using MopBot.Core.Systems.Channels;
using MopBot.Core;

namespace MopBot.Extensions
{
	public enum PredefinedEmote
	{
		Success,
		Failure
	}

	public static class UnsortedExtensions
	{
		public static BindingFlags anyFlags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;

		public static string GetDisplayName(this IUser user)
			=> (user as IGuildUser)?.Nickname ?? user?.Username ?? "UnknownUser";

		public static SocketUserMessage ToSocketUserMessage(this RestUserMessage restMessage, SocketTextChannel channel)
		{
			SocketUserMessage msgNew = MopBot.Construct<SocketUserMessage>(new Type[] { typeof(DiscordSocketClient), typeof(ulong), typeof(ISocketMessageChannel), typeof(SocketUser), typeof(MessageSource) }, new object[] { MopBot.client, restMessage.Id, channel, restMessage.Author, restMessage.Source });

			typeof(SocketMessage).GetField("<Content>k__BackingField", anyFlags).SetValue(msgNew, restMessage.Content);
			typeof(SocketUserMessage).GetField("_embeds", anyFlags).SetValue(msgNew, (ImmutableArray<Embed>)restMessage.Embeds);
			typeof(SocketUserMessage).GetField("_attachments", anyFlags).SetValue(msgNew, (ImmutableArray<Attachment>)restMessage.Attachments);

			return msgNew;
		}

		public static async Task<SocketGuildUser[]> GetReactionUsersAsync(this SocketMessage message, SocketGuild server, IEmote emote, RequestOptions options = null)
		{
			//await MessageHelper.AddReactionAsync(this, emote, base.Discord, options);
			var assembly = typeof(DiscordRestClient).Assembly;
			var userType = assembly.GetType("Discord.API.User");
			Type discordRestApiClient = assembly.GetType("Discord.API.DiscordRestApiClient");
			var method = discordRestApiClient.GetMethod("GetReactionUsersAsync", BindingFlags.Public | BindingFlags.Instance);
			var property = typeof(BaseDiscordClient).GetProperty("ApiClient", BindingFlags.NonPublic | BindingFlags.Instance);
			object obj = property.GetValue(MopBot.client);

			var task = (Task)method.Invoke(obj, new object[] {
				message.Channel.Id,
				message.Id,
				emote is Emote e ? $"{e.Name}:{e.Id}" : emote.Name,
				Activator.CreateInstance(assembly.GetType("Discord.API.Rest.GetReactionUsersParams")),
				options
			});

			try {
				task.Wait();

				object list = task.GetType().GetProperty("Result", BindingFlags.Public | BindingFlags.Instance).GetValue(task);
				var enumerable = (IEnumerable)list;
				var result = new List<SocketGuildUser>();

				foreach (object value in enumerable) {
					result.Add(server.GetUser((ulong)userType.GetProperty("Id", BindingFlags.Public | BindingFlags.Instance).GetValue(value)));
				}

				return result.ToArray();
			}
			catch (HttpException exception) {
				if (MemorySystem.memory[server].GetData<ChannelSystem, ChannelServerData>().TryGetChannelByRoles(out var tempChannel, ChannelRole.Logs, ChannelRole.BotArea, ChannelRole.Default) && tempChannel is IMessageChannel logChannel) {
					await logChannel.SendMessageAsync($"Unable to add reactions in <#{message.Channel.Id}>, an HttpException has occured:\r\n{exception.Message}");
				}
			}
			catch (Exception exception) {
				await MopBot.HandleException(exception);
			}

			return null;
		}

		public static bool SetIfNull<T>(this object obj, ref T field, Func<T> func)
		{
			if (field == null) {
				field = func();

				return true;
			}

			return false;
		}

		public static bool AddIfMissing<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key, Func<TValue> func)
		{
			if (!dict.ContainsKey(key)) {
				dict.Add(key, func());

				return true;
			}

			return false;
		}

		public static bool AddIfMissingOrNull<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key, Func<TValue> func)
		{
			if (!dict.ContainsKey(key) || dict[key] == null) {
				dict.Add(key, func());

				return true;
			}

			return false;
		}

		// MopBot

		public static bool HasRole(this SocketGuildUser user, IRole role)
			=> user.Roles.Any(r => r.Id == role.Id);

		public static DColor GetColor(this IUser user)
		{
			var defaultColor = new DColor(240, 240, 240);
			uint defaultRawValue = defaultColor.RawValue;
			var color = defaultColor;

			if (user is not SocketGuildUser serverUser) {
				return color;
			}

			foreach (var role in serverUser.Roles.OrderBy(role => role.Position)) {
				var roleColor = role.Color;

				if (roleColor.RawValue != defaultRawValue) {
					color = roleColor;
				}
			}

			return color;
		}

		public static bool IsBotMaster(this IUser user)
			=> GlobalConfiguration.config.masterUsers?.Contains(user.Id) == true;

		public static bool HasDiscordPermission(this SocketGuildUser user, Func<GuildPermissions, bool> getPerm)
		{
			var roles = user.Roles.OrderBy(r => r.Position);

			foreach (var role in roles) {
				var permissions = role.Permissions;

				if (permissions.Administrator || getPerm(permissions)) {
					return true;
				}
			}

			return false;
		}

		public static bool HasChannelPermission(this SocketGuildUser user, SocketGuildChannel channel, DiscordPermission permission)
		{
			if (channel == null) {
				throw new ArgumentNullException(nameof(channel));
			}

			if (user.Roles.Any(r => r.Permissions.Administrator)) {
				return true;
			}

			bool? result = null;

			foreach (var role in user.Roles.OrderBy(r => r.Position)) {
				var rolePerms = role.Permissions;

				if (rolePerms.TryGetValueFromEnum(permission, out bool roleResult)) {
					result = roleResult;
				}

				var roleOverwrite = channel.GetPermissionOverwrite(role);

				if (roleOverwrite.HasValue && roleOverwrite.Value.TryGetValueFromEnum(permission, out bool? roleOverwriteResult) && roleOverwriteResult.HasValue) {
					result = roleOverwriteResult.Value;
				}
			}

			var userOverwrite = channel.GetPermissionOverwrite(user);

			if (userOverwrite.HasValue && userOverwrite.Value.TryGetValueFromEnum(permission, out bool? userOverwriteResult) && userOverwriteResult.HasValue) {
				result = userOverwriteResult.Value;
			}

			return result ?? false;
		}

		public static void RequirePermission(this SocketGuildUser user, SocketGuildChannel channel, DiscordPermission permission)
		{
			if (!user.HasChannelPermission(channel, permission)) {
				throw new BotError($"{(user.Id == MopBot.client.CurrentUser.Id ? "I'm" : "You're")} missing the following permission: `{permission}`");
			}
		}

		public static void RequirePermission(this SocketGuildUser user, string permission)
		{
			if (!user.HasAnyPermissions(permission)) {
				throw new BotError($"Missing the following permission: `{permission}`.");
			}
		}

		public static bool HasAnyPermissions(this SocketGuildUser user, params string[] permissions)
		{
			var serverData = MemorySystem.memory[user.Guild].GetData<PermissionSystem, PermissionServerData>();

			var roles = user.Roles.Select(role => (role.Position, role.Id)).OrderBy(tuple => tuple.Position).Select(tuple => tuple.Id).Concat(new List<ulong>() { 0 });
			foreach (var permission in permissions) {
				bool? result = null;

				foreach (ulong roleId in roles) {
					if (!serverData.roleGroups.TryGetValue(roleId, out string groupName)) {
						continue;
					}

					var group = serverData.permissionGroups[groupName];
					bool? newResult = group[permission];

					if (result == null || newResult != null) {
						result = newResult;
					}
				}

				if (result == true) {
					return true;
				}
			}

			return false;
		}

		public static bool TryGetValueFromEnum(this GuildPermissions perms, DiscordPermission permission, out bool value)
		{
			bool result = true;

			value = permission switch {
				DiscordPermission.Administrator => perms.Administrator,
				DiscordPermission.ManageServer => perms.ManageGuild,
				DiscordPermission.ManageMessages => perms.ManageMessages,
				DiscordPermission.ManageChannel => perms.ManageChannels,
				DiscordPermission.ManageEmojis => perms.ManageEmojis,
				DiscordPermission.ManageNicknames => perms.ManageNicknames,
				DiscordPermission.ManageRoles => perms.ManageRoles,
				DiscordPermission.ManageWebhooks => perms.ManageWebhooks,

				DiscordPermission.EmbedLinks => perms.EmbedLinks,
				DiscordPermission.AttachFiles => perms.AttachFiles,
				DiscordPermission.ReadMessageHistory => perms.ReadMessageHistory,
				DiscordPermission.UseExternalEmojis => perms.UseExternalEmojis,
				DiscordPermission.Connect => perms.Connect,
				DiscordPermission.Speak => perms.Speak,
				DiscordPermission.UseVAD => perms.UseVAD,
				DiscordPermission.ChangeNickname => perms.ChangeNickname,
				DiscordPermission.CreateInstantInvite => perms.CreateInstantInvite,
				DiscordPermission.AddReactions => perms.AddReactions,
				DiscordPermission.ViewAuditLog => perms.ViewAuditLog,

				DiscordPermission.ViewChannel => perms.ViewChannel,
				DiscordPermission.SendMessages => perms.SendMessages,
				DiscordPermission.SendTTSMessages => perms.SendTTSMessages,
				DiscordPermission.MentionEveryone => perms.MentionEveryone,

				DiscordPermission.DeafenMembers => perms.DeafenMembers,
				DiscordPermission.MoveMembers => perms.MoveMembers,
				DiscordPermission.MuteMembers => perms.MuteMembers,
				DiscordPermission.KickMembers => perms.KickMembers,
				DiscordPermission.BanMembers => perms.BanMembers,

				_ => result = false
			};

			return result;
		}

		public static bool TryGetValueFromEnum(this OverwritePermissions perms, DiscordPermission permission, out bool? value)
		{
			static bool? GetNBool(PermValue val) => val == PermValue.Allow ? true : (val == PermValue.Deny ? false : (bool?)null);

			bool result = true;

			value = permission switch {
				DiscordPermission.EmbedLinks => GetNBool(perms.EmbedLinks),
				DiscordPermission.AttachFiles => GetNBool(perms.AttachFiles),
				DiscordPermission.ReadMessageHistory => GetNBool(perms.ReadMessageHistory),
				DiscordPermission.MentionEveryone => GetNBool(perms.MentionEveryone),
				DiscordPermission.UseExternalEmojis => GetNBool(perms.UseExternalEmojis),
				DiscordPermission.Connect => GetNBool(perms.Connect),
				DiscordPermission.ViewChannel => GetNBool(perms.ViewChannel),
				DiscordPermission.Speak => GetNBool(perms.Speak),
				DiscordPermission.MuteMembers => GetNBool(perms.MuteMembers),
				DiscordPermission.DeafenMembers => GetNBool(perms.DeafenMembers),
				DiscordPermission.MoveMembers => GetNBool(perms.MoveMembers),
				DiscordPermission.UseVAD => GetNBool(perms.UseVAD),
				DiscordPermission.ManageMessages => GetNBool(perms.ManageMessages),
				DiscordPermission.SendTTSMessages => GetNBool(perms.SendTTSMessages),
				DiscordPermission.ManageWebhooks => GetNBool(perms.ManageWebhooks),
				DiscordPermission.ManageRoles => GetNBool(perms.ManageRoles),
				DiscordPermission.AddReactions => GetNBool(perms.AddReactions),
				DiscordPermission.ManageChannel => GetNBool(perms.ManageChannel),
				DiscordPermission.CreateInstantInvite => GetNBool(perms.CreateInstantInvite),
				DiscordPermission.SendMessages => GetNBool(perms.SendMessages),
				_ => result = false
			};

			if (!result) {
				value = null;
			}

			return result;
		}

		public static Task Success(this ICommandContext context)
			=> context.AddPredefinedReaction(PredefinedEmote.Success);

		public static Task Failure(this ICommandContext context)
			=> context.AddPredefinedReaction(PredefinedEmote.Failure);

		public static async Task AddPredefinedReaction(this ICommandContext context, PredefinedEmote emote)
		{
			string emoji;

			switch (emote) {
				case PredefinedEmote.Success:
					emoji = "✅";
					break;
				case PredefinedEmote.Failure:
					emoji = "❌";
					break;
				default:
					return;
			}

			await context.Message.AddReactionAsync(new Emoji(emoji));
		}

		public static async Task<IUserMessage> ReplyAsync(this ICommandContext context, Embed embed, bool mention = true, ISocketMessageChannel channelOverride = null)
			=> await (channelOverride ?? context.Channel).SendMessageAsync(mention ? context.User.Mention + " " : null, embed: embed);
		
		public static async Task<IUserMessage> ReplyAsync(this ICommandContext context, string text, EmbedBuilder embedBuilder, bool mention = true, ISocketMessageChannel channelOverride = null)
			=> await (channelOverride ?? context.Channel).SendMessageAsync((mention ? context.User.Mention + " " : null) + text, embed: embedBuilder.Build());
		
		public static async Task<IUserMessage> ReplyAsync(this MessageContext context, string text, bool mention = true, ISocketMessageChannel channelOverride = null)
			=> await (channelOverride ?? context.Channel).SendMessageAsync((mention ? context.User.Mention + " " : null) + text);

		public static ServerMemory GetMemory(this SocketGuild server)
			=> MemorySystem.memory[server];
		
		public static ServerUserMemory GetMemory(this SocketGuildUser user)
			=> MemorySystem.memory[user.Guild][user];

		public static bool CheckSynced(this PreconditionAttribute precondition, ICommandContext context, CommandInfo command)
			=> precondition.CheckSynced(context, command, MopBot.serviceProvaider);

		public static bool PermissionsMet(this IEnumerable<PreconditionAttribute> preconditions, ICommandContext context, CommandInfo command = null)
		{
			var permReqs = preconditions.SelectIgnoreNull(p => p as RequirePermissionAttribute);

			if (permReqs == null || permReqs.Count() == 0) {
				return true;
			}

			return permReqs.All(p => p.CheckSynced(context, command)) != false;
		}

		public static bool AllMet(this IEnumerable<PreconditionAttribute> preconditions, ICommandContext context, CommandInfo command)
			=> preconditions.AllMet(context, command, MopBot.serviceProvaider);

		public static async Task<bool> AllMetAsync(this IEnumerable<PreconditionAttribute> preconditions, ICommandContext context, CommandInfo command)
			=> await preconditions.AllMetAsync(context, command, MopBot.serviceProvaider);

		// Discord

		public static bool ColorEquals(this Color colorA, Color colorB)
			=> colorA.R == colorB.R && colorA.G == colorB.G && colorA.B == colorB.B;

		public static bool CheckSynced(this PreconditionAttribute precondition, ICommandContext context, CommandInfo command, IServiceProvider provider)
		{
			var task = precondition.CheckPermissionsAsync(context, command, provider);

			if (!task.IsCompleted) {
				task.RunSynchronously();
			}

			return task.Result.IsSuccess;
		}

		public static bool AllMet(this IEnumerable<PreconditionAttribute> preconditions, ICommandContext context, CommandInfo command, IServiceProvider provider)
		{
			return preconditions.All(p => p.CheckSynced(context, command, provider));
		}

		public static async Task<bool> AllMetAsync(this IEnumerable<PreconditionAttribute> preconditions, ICommandContext context, CommandInfo command, IServiceProvider provider)
		{
			foreach (var precondition in preconditions) {
				if (!(await precondition.CheckPermissionsAsync(context, command, provider)).IsSuccess) {
					return false;
				}
			}

			return true;
		}

		public static async Task ModifyPermissions(this SocketGuildChannel channel, IRole role, Func<OverwritePermissions, OverwritePermissions> func)
		{
			var perms = channel.GetPermissionOverwrite(role) ?? OverwritePermissions.InheritAll;
			var newPerms = func(perms);

			if (perms.AllowValue != newPerms.AllowValue || perms.DenyValue != newPerms.DenyValue) {
				await channel.AddPermissionOverwriteAsync(role, newPerms);
			}
		}

		public static async Task ModifyPermissions(this SocketGuildChannel channel, IUser user, Func<OverwritePermissions, OverwritePermissions> func)
		{
			var perms = channel.GetPermissionOverwrite(user) ?? OverwritePermissions.InheritAll;
			var newPerms = func(perms);

			if (perms.AllowValue != newPerms.AllowValue || perms.DenyValue != newPerms.DenyValue) {
				await channel.AddPermissionOverwriteAsync(user, newPerms);
			}
		}

		// Reflection

		public static bool IsDerivedFrom(this Type type, Type from)
		{
			return type != from && from.IsAssignableFrom(type);
		}

		// StringExtensions

		public static bool EndsWithAny(this string source, params string[] strings)
		{
			for (int i = 0; i < strings.Length; i++) {
				if (source.EndsWith(strings[i])) {
					return true;
				}
			}

			return false;
		}

		public static string TruncateWithDots(this string value, int maxLength)
		{
			if (string.IsNullOrEmpty(value) || value.Length <= maxLength) {
				return value;
			}

			if (maxLength > 3) {
				return value.Substring(0, maxLength - 3) + "...";
			}

			return value.Substring(0, maxLength);
		}

		public static string Repeat(this string str, int numTimes)
			=> string.Concat(Enumerable.Repeat(str, numTimes));

		public static bool ContainsWord(this string str, params string[] words)
			=> words.Any(q => str.ContainsWord(q));

		public static bool ContainsWord(this string str, string word)
		{
			if (str == word || str.StartsWith(word) && !char.IsLetterOrDigit(str[word.Length])) {
				return true;
			}

			if (str.IndexOf(" " + word, out int index)) {
				index += 1 + word.Length;

				if (index >= str.Length || !char.IsLetterOrDigit(str[index])) {
					return true;
				}
			}

			return false;
		}

		public static bool IndexOf(this string str, string text, out int index)
		{
			index = str.IndexOf(text);

			return index != -1;
		}

		public static bool Contains(this string str, char character)
		{
			for (int i = 0; i < str.Length; i++) {
				if (str[i] == character) {
					return true;
				}
			}

			return false;
		}

		public static string Capitalize(this string input)
		{
			switch (input) {
				case null:
					throw new ArgumentNullException(nameof(input));
				case "":
					throw new ArgumentException($"{nameof(input)} cannot be empty", nameof(input));
				default:
					return input.First().ToString().ToUpper() + input.Substring(1);
			}
		}

		public static string RemoveWhitespaces(this string input)
		{
			var sb = new StringBuilder(input);

			int pos = 0;
			for (int i = 0; i < input.Length; i++) {
				switch (input[i]) {
					case ' ':
					case '\t':
					case '\r':
					case '\n':
					case '\v':
						sb.Remove(pos, 1);

						continue;
				}

				pos++;
			}

			return sb.ToString();
		}
	}
}
