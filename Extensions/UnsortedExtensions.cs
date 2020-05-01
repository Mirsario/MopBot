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
		public static BindingFlags anyFlags = BindingFlags.Instance|BindingFlags.Static|BindingFlags.NonPublic|BindingFlags.Public;
		public static string GetDisplayName(this IUser user) => (user as IGuildUser)?.Nickname ?? user?.Username ?? "UnknownUser";

		public static SocketUserMessage ToSocketUserMessage(this RestUserMessage restMessage,SocketTextChannel channel)
		{
			SocketUserMessage msgNew = MopBot.Construct<SocketUserMessage>(new Type[] { typeof(DiscordSocketClient),typeof(ulong),typeof(ISocketMessageChannel),typeof(SocketUser),typeof(MessageSource) },new object[] { MopBot.client,restMessage.Id,channel,restMessage.Author,restMessage.Source });
			
			typeof(SocketMessage).GetField("<Content>k__BackingField",anyFlags).SetValue(msgNew,restMessage.Content);
			typeof(SocketUserMessage).GetField("_embeds",anyFlags).SetValue(msgNew,(ImmutableArray<Embed>)restMessage.Embeds);
			typeof(SocketUserMessage).GetField("_attachments",anyFlags).SetValue(msgNew,(ImmutableArray<Attachment>)restMessage.Attachments);

			return msgNew;
		}
		public static async Task<SocketGuildUser[]> GetReactionUsersAsync(this SocketMessage message,SocketGuild server,IEmote emote,RequestOptions options = null)
		{
			//await MessageHelper.AddReactionAsync(this, emote, base.Discord, options);
			var assembly = typeof(DiscordRestClient).Assembly;
			var userType = assembly.GetType("Discord.API.User");
			Type discordRestApiClient = assembly.GetType("Discord.API.DiscordRestApiClient");
			var method = discordRestApiClient.GetMethod("GetReactionUsersAsync",BindingFlags.Public|BindingFlags.Instance);
			var property = typeof(BaseDiscordClient).GetProperty("ApiClient",BindingFlags.NonPublic|BindingFlags.Instance);
			var obj = property.GetValue(MopBot.client);
			var task = (Task)method.Invoke(obj,new object[] {
				message.Channel.Id,
				message.Id,
				emote is Emote e ? $"{e.Name}:{e.Id}" : emote.Name,
				Activator.CreateInstance(assembly.GetType("Discord.API.Rest.GetReactionUsersParams")),
				options
			});

			try {
				task.Wait();
				var list = task.GetType().GetProperty("Result",BindingFlags.Public|BindingFlags.Instance).GetValue(task);
				var enumerable = (IEnumerable)list;
				var result = new List<SocketGuildUser>();

				foreach(var value in enumerable) {
					result.Add(server.GetUser((ulong)userType.GetProperty("Id",BindingFlags.Public|BindingFlags.Instance).GetValue(value)));
				}

				return result.ToArray();
			}
			catch(HttpException exception) {
				if(MemorySystem.memory[server].GetData<ChannelSystem,ChannelServerData>().TryGetChannelByRoles(out var tempChannel,ChannelRole.Logs,ChannelRole.BotArea,ChannelRole.Default) && tempChannel is IMessageChannel logChannel) {
					await logChannel.SendMessageAsync($"Unable to add reactions in <#{message.Channel.Id}>, an HttpException has occured:\r\n{exception.Message}");
				}
			}
			catch(Exception exception) {
				await MopBot.HandleException(exception);
			}

			return null;
		}

		public static bool SetIfNull<T>(this object obj,ref T field,Func<T> func)
		{
			if(field==null) {
				field = func();

				return true;
			}

			return false;
		}
		public static bool AddIfMissing<TKey,TValue>(this IDictionary<TKey,TValue> dict,TKey key,Func<TValue> func)
		{
			if(!dict.ContainsKey(key)) {
				dict.Add(key,func());

				return true;
			}

			return false;
		}
		public static bool AddIfMissingOrNull<TKey,TValue>(this IDictionary<TKey,TValue> dict,TKey key,Func<TValue> func)
		{
			if(!dict.ContainsKey(key) || dict[key]==null) {
				dict.Add(key,func());

				return true;
			}

			return false;
		}

		#region MopBot
		public static bool HasRole(this SocketGuildUser user,IRole role) => user.Roles.Any(r => r.Id==role.Id);
		public static DColor GetColor(this IUser user)
		{
			var defaultColor = new DColor(240,240,240);
			uint defaultRawValue = defaultColor.RawValue;
			var color = defaultColor;

			if(!(user is SocketGuildUser serverUser)) {
				return color;
			}

			foreach(var role in serverUser.Roles.OrderBy(role => role.Position)) {
				var roleColor = role.Color;

				if(roleColor.RawValue!=defaultRawValue) {
					color = roleColor;
				}
			}

			return color;
		}

		public static bool IsBotMaster(this IUser user) => GlobalConfiguration.config.masterUsers?.Contains(user.Id)==true;
		public static bool HasDiscordPermission(this SocketGuildUser user,Func<GuildPermissions,bool> getPerm)
		{
			var roles = user.Roles.OrderBy(r => r.Position);

			foreach(var role in roles) {
				var permissions = role.Permissions;

				if(permissions.Administrator || getPerm(permissions)) {
					return true;
				}
			}

			return false;
		}
		public static bool HasChannelPermission(this SocketGuildUser user,SocketGuildChannel channel,DiscordPermission permission)
		{
			if(channel==null) {
				throw new ArgumentNullException(nameof(channel));
			}

			if(user.Roles.Any(r => r.Permissions.Administrator)) {
				return true;
			}

			bool? result = null;
			
			foreach(var role in user.Roles.OrderBy(r => r.Position)) {
				var rolePerms = role.Permissions;

				if(rolePerms.TryGetValueFromEnum(permission,out bool roleResult)) {
					result = roleResult;
				}

				var roleOverwrite = channel.GetPermissionOverwrite(role);

				if(roleOverwrite.HasValue && roleOverwrite.Value.TryGetValueFromEnum(permission,out bool? roleOverwriteResult) && roleOverwriteResult.HasValue) {
					result = roleOverwriteResult.Value;
				}
			}

			var userOverwrite = channel.GetPermissionOverwrite(user);

			if(userOverwrite.HasValue && userOverwrite.Value.TryGetValueFromEnum(permission,out bool? userOverwriteResult) && userOverwriteResult.HasValue) {
				result = userOverwriteResult.Value;
			}

			return result ?? false;
		}
		public static void RequirePermission(this SocketGuildUser user,SocketGuildChannel channel,DiscordPermission permission)
		{
			if(!user.HasChannelPermission(channel,permission)) {
				throw new BotError($"{(user.Id==MopBot.client.CurrentUser.Id ? "I'm" : "You're")} missing the following permission: `{permission}`");
			}
		}
		public static void RequirePermission(this SocketGuildUser user,string permission)
		{
			if(!user.HasAnyPermissions(permission)) {
				throw new BotError($"Missing the following permission: `{permission}`.");
			}
		}
		public static bool HasAnyPermissions(this SocketGuildUser user,params string[] permissions)
		{
			var serverData = MemorySystem.memory[user.Guild].GetData<PermissionSystem,PermissionServerData>();
			
			var roles = user.Roles.Select(role => (role.Position, role.Id)).OrderBy(tuple => tuple.Position).Select(tuple => tuple.Id).Concat(new List<ulong>() { 0 });
			foreach(var permission in permissions) {
				bool? result = null;

				foreach(ulong roleId in roles) {
					if(!serverData.roleGroups.TryGetValue(roleId,out string groupName)) {
						continue;
					}

					var group = serverData.permissionGroups[groupName];
					bool? newResult = group[permission];

					if(result==null || newResult!=null) {
						result = newResult;
					}
				}

				if(result==true) {
					return true;
				}
			}

			return false;
		}

		public static bool TryGetValueFromEnum(this GuildPermissions perms,DiscordPermission permission,out bool value)
		{
			switch(permission) {
				case DiscordPermission.Administrator:		value = perms.Administrator;		return true;
				case DiscordPermission.ManageServer:		value = perms.ManageGuild;			return true;
				case DiscordPermission.ManageMessages:		value = perms.ManageMessages;		return true;
				case DiscordPermission.ManageChannel:		value = perms.ManageChannels;		return true;
				case DiscordPermission.ManageEmojis:		value = perms.ManageEmojis;			return true;
				case DiscordPermission.ManageNicknames:		value = perms.ManageNicknames;		return true;
				case DiscordPermission.ManageRoles:			value = perms.ManageRoles;			return true;
				case DiscordPermission.ManageWebhooks:		value = perms.ManageWebhooks;		return true;

				case DiscordPermission.EmbedLinks:			value = perms.EmbedLinks;			return true;
				case DiscordPermission.AttachFiles:			value = perms.AttachFiles;			return true;
				case DiscordPermission.ReadMessageHistory:	value = perms.ReadMessageHistory;	return true;
				case DiscordPermission.UseExternalEmojis:	value = perms.UseExternalEmojis;	return true;
				case DiscordPermission.Connect:				value = perms.Connect;				return true;
				case DiscordPermission.Speak:				value = perms.Speak;				return true;
				case DiscordPermission.UseVAD:				value = perms.UseVAD;				return true;
				case DiscordPermission.ChangeNickname:		value = perms.ChangeNickname;		return true;
				case DiscordPermission.CreateInstantInvite:	value = perms.CreateInstantInvite;	return true;
				case DiscordPermission.AddReactions:		value = perms.AddReactions;			return true;
				case DiscordPermission.ViewAuditLog:		value = perms.ViewAuditLog;			return true;

				case DiscordPermission.ViewChannel:			value = perms.ViewChannel;			return true;
				case DiscordPermission.SendMessages:		value = perms.SendMessages;			return true;
				case DiscordPermission.SendTTSMessages:		value = perms.SendTTSMessages;		return true;
				case DiscordPermission.MentionEveryone:		value = perms.MentionEveryone;		return true;

				case DiscordPermission.DeafenMembers:		value = perms.DeafenMembers;		return true;
				case DiscordPermission.MoveMembers:			value = perms.MoveMembers;			return true;
				case DiscordPermission.MuteMembers:			value = perms.MuteMembers;			return true;
				case DiscordPermission.KickMembers:			value = perms.KickMembers;			return true;
				case DiscordPermission.BanMembers:			value = perms.BanMembers;			return true;
			}

			value = false;

			return false;
		}
		public static bool TryGetValueFromEnum(this OverwritePermissions perms,DiscordPermission permission,out bool? value)
		{
			bool? ToNBool(PermValue val) => val==PermValue.Allow ? true : (val==PermValue.Deny ? false : (bool?)null);
			
			switch(permission) {
				case DiscordPermission.EmbedLinks:			value = ToNBool(perms.EmbedLinks);			return true;
				case DiscordPermission.AttachFiles:			value = ToNBool(perms.AttachFiles);			return true;
				case DiscordPermission.ReadMessageHistory:	value = ToNBool(perms.ReadMessageHistory);	return true;
				case DiscordPermission.MentionEveryone:		value = ToNBool(perms.MentionEveryone);		return true;
				case DiscordPermission.UseExternalEmojis:	value = ToNBool(perms.UseExternalEmojis);	return true;
				case DiscordPermission.Connect:				value = ToNBool(perms.Connect);				return true;
				case DiscordPermission.ViewChannel:			value = ToNBool(perms.ViewChannel);			return true;
				case DiscordPermission.Speak:				value = ToNBool(perms.Speak);				return true;
				case DiscordPermission.MuteMembers:			value = ToNBool(perms.MuteMembers);			return true;
				case DiscordPermission.DeafenMembers:		value = ToNBool(perms.DeafenMembers);		return true;
				case DiscordPermission.MoveMembers:			value = ToNBool(perms.MoveMembers);			return true;
				case DiscordPermission.UseVAD:				value = ToNBool(perms.UseVAD);				return true;
				case DiscordPermission.ManageMessages:		value = ToNBool(perms.ManageMessages);		return true;
				case DiscordPermission.SendTTSMessages:		value = ToNBool(perms.SendTTSMessages);		return true;
				case DiscordPermission.ManageWebhooks:		value = ToNBool(perms.ManageWebhooks);		return true;
				case DiscordPermission.ManageRoles:			value = ToNBool(perms.ManageRoles);			return true;
				case DiscordPermission.AddReactions:		value = ToNBool(perms.AddReactions);		return true;
				case DiscordPermission.ManageChannel:		value = ToNBool(perms.ManageChannel);		return true;
				case DiscordPermission.CreateInstantInvite:	value = ToNBool(perms.CreateInstantInvite);	return true;
				case DiscordPermission.SendMessages:		value = ToNBool(perms.SendMessages);		return true;
			}

			value = null;

			return false;
		}
		
		public static Task Success(this ICommandContext context)
			=> context.AddPredefinedReaction(PredefinedEmote.Success);
		public static Task Failure(this ICommandContext context)
			=> context.AddPredefinedReaction(PredefinedEmote.Failure);
		
		public static async Task AddPredefinedReaction(this ICommandContext context,PredefinedEmote emote)
		{
			string emoji;
			switch(emote) {
				case PredefinedEmote.Success: emoji = "✅"; break;
				case PredefinedEmote.Failure: emoji = "❌"; break;
				default: return;
			}
			await context.Message.AddReactionAsync(new Emoji(emoji));
		}

		public static async Task<IUserMessage> ReplyAsync(this ICommandContext context,Embed embed,bool mention = true,ISocketMessageChannel channelOverride = null) => await (channelOverride ?? context.Channel).SendMessageAsync(mention ? context.User.Mention+" " : null,embed:embed);
		public static async Task<IUserMessage> ReplyAsync(this ICommandContext context,string text,EmbedBuilder embedBuilder,bool mention = true,ISocketMessageChannel channelOverride = null) => await (channelOverride ?? context.Channel).SendMessageAsync((mention ? context.User.Mention+" " : null)+text,embed:embedBuilder.Build());
		public static async Task<IUserMessage> ReplyAsync(this MessageExt context,string text,bool mention = true,ISocketMessageChannel channelOverride = null)
			=> await (channelOverride ?? context.Channel).SendMessageAsync((mention ? context.User.Mention+" " : null)+text);

		public static ServerMemory GetMemory(this SocketGuild server) => MemorySystem.memory[server];
		public static ServerUserMemory GetMemory(this SocketGuildUser user) => MemorySystem.memory[user.Guild][user];

		public static bool CheckSynced(this PreconditionAttribute precondition,ICommandContext context,CommandInfo command) => precondition.CheckSynced(context,command,MopBot.serviceProvaider);
		public static bool PermissionsMet(this IEnumerable<PreconditionAttribute> preconditions,ICommandContext context,CommandInfo command = null)
		{
			var permReqs = preconditions.SelectIgnoreNull(p => p as RequirePermissionAttribute);

			if(permReqs==null || permReqs.Count()==0) {
				return true;
			}

			return permReqs.All(p => p.CheckSynced(context,command))!=false;
		}
		public static bool AllMet(this IEnumerable<PreconditionAttribute> preconditions,ICommandContext context,CommandInfo command) => preconditions.AllMet(context,command,MopBot.serviceProvaider);
		public static async Task<bool> AllMetAsync(this IEnumerable<PreconditionAttribute> preconditions,ICommandContext context,CommandInfo command) => await preconditions.AllMetAsync(context,command,MopBot.serviceProvaider);
		#endregion
		#region Discord
		public static bool ColorEquals(this Discord.Color colorA,Discord.Color colorB) => colorA.R==colorB.R && colorA.G==colorB.G && colorA.B==colorB.B;
		public static bool CheckSynced(this PreconditionAttribute precondition,ICommandContext context,CommandInfo command,IServiceProvider provider)
		{
			var task = precondition.CheckPermissionsAsync(context,command,provider);

			if(!task.IsCompleted) {
				task.RunSynchronously();
			}

			return task.Result.IsSuccess;
		}
		public static bool AllMet(this IEnumerable<PreconditionAttribute> preconditions,ICommandContext context,CommandInfo command,IServiceProvider provider)
		{
			return preconditions.All(p => p.CheckSynced(context,command,provider));
		}
		public static async Task<bool> AllMetAsync(this IEnumerable<PreconditionAttribute> preconditions,ICommandContext context,CommandInfo command,IServiceProvider provider)
		{
			foreach(var precondition in preconditions) {
				if(!(await precondition.CheckPermissionsAsync(context,command,provider)).IsSuccess) {
					return false;
				}
			}
			return true;
		}

		public static async Task ModifyPermissions(this SocketGuildChannel channel,IRole role,Func<OverwritePermissions,OverwritePermissions> func)
		{
			var perms = channel.GetPermissionOverwrite(role) ?? OverwritePermissions.InheritAll;
			var newPerms = func(perms);

			if(perms.AllowValue!=newPerms.AllowValue || perms.DenyValue!=newPerms.DenyValue) {
				await channel.AddPermissionOverwriteAsync(role,newPerms);
			}
		}
		public static async Task ModifyPermissions(this SocketGuildChannel channel,IUser user,Func<OverwritePermissions,OverwritePermissions> func)
		{
			var perms = channel.GetPermissionOverwrite(user) ?? OverwritePermissions.InheritAll;
			var newPerms = func(perms);

			if(perms.AllowValue!=newPerms.AllowValue || perms.DenyValue!=newPerms.DenyValue) {
				await channel.AddPermissionOverwriteAsync(user,newPerms);
			}
		}
		#endregion

		#region Reflection
		public static bool IsDerivedFrom(this Type type,Type from)
		{
			return type!=from && from.IsAssignableFrom(type);
		}
		#endregion

		#region StringExtensions
		public static bool EndsWithAny(this string source,params string[] strings)
		{
			for(int i = 0;i<strings.Length;i++) {
				if(source.EndsWith(strings[i])) {
					return true;
				}
			}
			return false;
		}

		public static string TruncateWithDots(this string value,int maxLength)
		{
			if(string.IsNullOrEmpty(value) || value.Length<=maxLength) {
				return value;
			}
			if(maxLength>3) {
				return value.Substring(0,maxLength-3)+"...";
			}
			return value.Substring(0,maxLength);
		}

		public static string Repeat(this string str,int numTimes) => string.Concat(Enumerable.Repeat(str,numTimes));

		public static bool ContainsWord(this string str,params string[] words) => words.Any(q => str.ContainsWord(q));
		public static bool ContainsWord(this string str,string word)
		{
			if(str==word || str.StartsWith(word) && !char.IsLetterOrDigit(str[word.Length])) {
				return true;
			}

			if(str.IndexOf(" "+word,out int index)) {
				index += 1+word.Length;

				if(index>=str.Length || !char.IsLetterOrDigit(str[index])) {
					return true;
				}
			}

			return false;
		}
		public static bool IndexOf(this string str,string text,out int index)
		{
			index = str.IndexOf(text);

			return index!=-1;
		}
		public static bool Contains(this string str,char character)
		{
			for(int i = 0;i<str.Length;i++) {
				if(str[i]==character) {
					return true;
				}
			}

			return false;
		}
		public static string Capitalize(this string input)
		{
			switch(input) {
				case null:
					throw new ArgumentNullException(nameof(input));
				case "":
					throw new ArgumentException($"{nameof(input)} cannot be empty",nameof(input));
				default:
					return input.First().ToString().ToUpper()+input.Substring(1);
			}
		}
		public static string RemoveWhitespaces(this string input)
		{
			var sb = new StringBuilder(input);

			int pos = 0;
			for(int i = 0;i<input.Length;i++) {
				switch(input[i]) {
					case ' ':
					case '\t':
					case '\r':
					case '\n':
					case '\v':
						sb.Remove(pos,1);
						continue;
				}

				pos++;
			}

			return sb.ToString();
		}
		#endregion
	}
}