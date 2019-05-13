using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Discord.Commands;
using MopBotTwo.Extensions;

#pragma warning disable 1998

namespace MopBotTwo.Systems
{
	//TODO: Incomplete and buggy.

	[Group("tag")] [Alias("tags")]
	[Summary(@"Group for managing and using shortcuts called ""tags"".")]
	[SystemConfiguration(EnabledByDefault = true)]
	public class TagSystem : BotSystem
	{
		public class TagGlobalData : GlobalData
		{
			public Dictionary<ulong,TagGroup> tagGroups = new Dictionary<ulong,TagGroup>();
			public Dictionary<ulong,Tag> tags = new Dictionary<ulong,Tag>();
			public ulong nextTagGroupId;
			public ulong nextTagId;
		}
		public class TagUserData : UserData
		{
			public List<ulong> subscribedTags = new List<ulong>();
			public List<ulong> subscribedTagGroups = new List<ulong>();
		}
		public class TagServerData : ServerData
		{
			public List<ulong> globalTagGroups = new List<ulong>();

			public override void Initialize(SocketGuild server) {}
		}

		public override void RegisterDataTypes()
		{
			RegisterDataType<Memory,TagGlobalData>();
			RegisterDataType<UserMemory,TagUserData>();
			RegisterDataType<ServerMemory,TagServerData>();
		}

		public static List<(ulong tagId,Tag tagInfo)> GetTagsWithName(IUser user,IGuild server,string name)
		{
			name = name.ToLowerInvariant();

			var memory = MemorySystem.memory;
			var globalData = memory.GetData<TagSystem,TagGlobalData>();
			
			var tagsFound = new List<(ulong tagId,Tag tagInfo)>();

			void AddTagAction(ulong tagId,Tag tag)
			{
				if(tag.name==name && !tagsFound.Any(tuple => tuple.tagId==tagId)) {
					tagsFound.Add((tagId,tag));
				} 
			}
			
			if(user!=null) {
				var userData = memory[user].GetData<TagSystem,TagUserData>();

				ForeachTag(globalData,userData.subscribedTags,AddTagAction);
				ForeachTagInGroups(globalData,userData.subscribedTagGroups,AddTagAction);
			}
			if(server!=null) {
				var serverData = memory[server].GetData<TagSystem,TagServerData>();

				ForeachTagInGroups(globalData,serverData.globalTagGroups,AddTagAction);
			}

			return tagsFound;
		}
		
		public static void ForeachTag(TagGlobalData globalData,List<ulong> tagIds,Action<ulong,Tag> action)
		{
			if(globalData==null) {
				globalData = MemorySystem.memory.GetData<TagSystem,TagGlobalData>();
			}
			for(int i = 0;i<tagIds.Count;i++) {
				ulong tagId = tagIds[i];
				if(globalData.tags.TryGetValue(tagId,out Tag tag)) {
					action(tagId,tag);
				}else{
					tagIds.RemoveAt(i--);
				}
			}
		}
		public static void ForeachTagInGroups(TagGlobalData globalData,List<ulong> groupIds,Action<ulong,Tag> action)
		{
			if(globalData==null) {
				globalData = MemorySystem.memory.GetData<TagSystem,TagGlobalData>();
			}

			for(int i = 0;i<groupIds.Count;i++) {
				ulong groupId = groupIds[i];
				if(globalData.tagGroups.TryGetValue(groupId,out TagGroup tagGroup)) {
					ForeachTag(globalData,tagGroup.tagIDs,action);
				}else{
					groupIds.RemoveAt(i--);
				}
			}
		}
		
		public delegate (bool doReturn,bool removeTag) TagEnumerationFunc(ulong id,Tag tag);
		public static bool ForeachTag(TagGlobalData globalData,List<ulong> tagIds,out (ulong id,Tag tag) tuple,TagEnumerationFunc func)
		{
			if(globalData==null) {
				globalData = MemorySystem.memory.GetData<TagSystem,TagGlobalData>();
			}

			for(int i = 0;i<tagIds.Count;i++) {
				ulong id = tagIds[i];
				if(globalData.tags.TryGetValue(id,out Tag tag)) {
					(bool doReturn,bool removeTag) = func(id,tag);
					if(removeTag) {
						tagIds.RemoveAt(i--);
					}
					if(doReturn) {
						tuple = (id,tag);
						return true;
					}
				}else{
					tagIds.RemoveAt(i--);
				}
			}

			tuple = default;
			return false;
		}
		public static bool ForeachTagInGroups(TagGlobalData globalData,List<ulong> groupIds,out (ulong id,Tag tag) tuple,TagEnumerationFunc func)
		{
			if(globalData==null) {
				globalData = MemorySystem.memory.GetData<TagSystem,TagGlobalData>();
			}

			for(int i = 0;i<groupIds.Count;i++) {
				ulong groupId = groupIds[i];
				if(globalData.tagGroups.TryGetValue(groupId,out TagGroup tagGroup)) {
					if(ForeachTag(globalData,tagGroup.tagIDs,out tuple,func)) {
						return true;
					}
				}else{
					groupIds.RemoveAt(i--);
				}
			}

			tuple = default;
			return false;
		}
		
		#region TagCommands
		[Command("add")] [Alias("new")]
		public async Task TagAddCommand(string tagName,[Remainder]string text)
		{
			tagName = tagName.ToLowerInvariant();
			
			var user = Context.user;
			var server = Context.server;
			var memory = MemorySystem.memory;
			var globalData = memory.GetData<TagSystem,TagGlobalData>();
			var userData = memory[user].GetData<TagSystem,TagUserData>();

			ulong userId = user.Id;

			var tags = GetTagsWithName(user,server,tagName);
			if(tags.Count>0 && tags.Any(t => t.tagInfo.owner==userId)) {
				char cmdSymbol = memory[server].GetData<CommandSystem,CommandSystem.CommandServerData>().commandPrefix;
				throw new BotError($"You already own a tag named '{tagName}'.\nUse `{cmdSymbol}tag edit {tagName} <text>` to edit it,\nOR use `{cmdSymbol}tag remove {tagName}` to remove it.");
			}

			ulong tagId = globalData.nextTagId++;
			if(globalData.tags.ContainsKey(tagId)) {
				globalData.nextTagId = globalData.tags.Max(t => t.Key)+1;
				tagId = globalData.nextTagId++;
			}

			//Create tag
			var tag = new Tag(user.Id,tagName,text);
			globalData.tags[tagId] = tag;
			
			//Subscribe the user to it
			if(!userData.subscribedTags.Contains(tagId)) {
				userData.subscribedTags.Add(tagId);
			}
		}
		[Command("edit")] [Alias("modify")]
		public async Task TagEditCommand(string tagName,[Remainder]string text)
		{
			tagName = tagName.ToLowerInvariant();
			
			var user = Context.user;
			var server = Context.server;
			var memory = MemorySystem.memory;
			var globalData = memory.GetData<TagSystem,TagGlobalData>();
			var userData = memory[user].GetData<TagSystem,TagUserData>();

			ulong userId = user.Id;

			var tags = GetTagsWithName(user,server,tagName);
			if(tags.Count==0 || !tags.TryGetFirst(t => t.tagInfo.owner==userId,out var tag)) {
				throw new BotError("Couldn't find any tag with such name that you own.");
			}

			//Update tag text
			tag.tagInfo.text = text;
		}
		[Command("remove")] [Alias("delete")]
		public async Task TagRemoveCommand(string tagName)
		{
			tagName = tagName.ToLowerInvariant();
			
			var user = Context.user;
			var memory = MemorySystem.memory;
			var globalData = memory.GetData<TagSystem,TagGlobalData>();
			var userData = memory[user].GetData<TagSystem,TagUserData>();

			ulong userId = user.Id;
			int numRemoved = 0;

			ForeachTag(globalData,userData.subscribedTags,out _,(id,tag) => {
				if(tag.owner==userId && tag.name==tagName) {
					globalData.tags.Remove(id);
					numRemoved++;
					return (false,true);
				}
				return (false,true);
			});

			if(numRemoved==0) {
				throw new BotError("Couldn't find a tag with such name.");
			}
		}
		[Priority(-1)]
		[Command]
		public Task TagCommand(string tagName) => TagUseCommand(tagName);
		[Command("use")]
		public async Task TagUseCommand(string tagName)
		{
			var context = Context;
			
			tagName = tagName.ToLowerInvariant();

			var user = context.socketServerUser;
			var server = context.server;
			var tagsFound = GetTagsWithName(user,server,tagName);

			if(tagsFound.Count==0) {
				throw new BotError(); //"Couldn't find any tag with such name that you're subscribed to.");
			}

			if(tagsFound.Count!=1) {
				const int MaxTextLength = 30;

				throw new BotError($"{tagsFound.Count} tags have been found: \n```{string.Join('\n',tagsFound.Select(t => $"{t.tagId} - {t.tagInfo.text.TruncateWithDots(MaxTextLength)}"))}```");
			}

			var (_,tag) = tagsFound[0];
			var embed = MopBot.GetEmbedBuilder(context)
				.WithAuthor($"{user.Name()}:",user.GetAvatarUrl())
				.WithFooter($@"""{tagName}"" by {MopBot.client.GetUser(tag.owner)?.Name() ?? "Unknown user"}")
				.WithDescription(tag.text)
				.WithColor(user.GetColor())
				.Build();

			await context.Channel.SendMessageAsync(embed:embed);

			if(server.CurrentUser.HasChannelPermission(context.socketTextChannel,DiscordPermission.ManageMessages)) {
				await context.message.DeleteAsync();
			}else{
			}
		}
		#endregion

		#region GroupCommands

		[Command("group add")] [Alias("group new")]
		public async Task TagGroupAddCommand(string groupName)
		{
			groupName = groupName.ToLowerInvariant();

			var user = Context.user;
			var memory = MemorySystem.memory;
			var globalData = memory.GetData<TagSystem,TagGlobalData>();
			var userData = memory[user].GetData<TagSystem,TagUserData>();

			if(globalData.tagGroups.Any(g => g.Value.name==groupName)) {
				throw new BotError($@"Group `{groupName}` already exists. Pick an unique name.");
			}

			ulong tagId = globalData.nextTagId++;
			if(globalData.tagGroups.ContainsKey(tagId)) {
				globalData.nextTagId = globalData.tagGroups.Max(t => t.Key)+1;
				tagId = globalData.nextTagId++;
			}

			//Create the group
			var tagGroup = new TagGroup(tagId,user.Id,groupName);
			globalData.tagGroups.Add(tagId,tagGroup);
			
			//Subscribe the user to it
			userData.subscribedTagGroups.Add(tagId);
		}
		[Command("group addtag")]
		public async Task TagGroupAddTagCommand(string groupName,string tagName)
		{
			groupName = groupName.ToLowerInvariant();
			int groupNameHash = groupName.GetHashCode();

			var user = Context.user;
			var memory = MemorySystem.memory;
			var globalData = memory.GetData<TagSystem,TagGlobalData>();
			var userData = memory[user].GetData<TagSystem,TagUserData>();
			ulong userId = user.Id;

			if(!globalData.tagGroups.TryGetFirst(g => g.Value.name==groupName,out var idGroupPair)) {
				throw new BotError($@"Group `{groupName}` does not exist.");
			}
			var group = idGroupPair.Value;
			if(group.owner!=userId) {
				throw new BotError($@"Can't add tags to group `{groupName}`, you're neither that group's owner nor its maintainer.");
			}
			
			if(!ForeachTag(globalData,userData.subscribedTags,out (ulong tagId,Tag tag) result,(id,tag) => (tag.name==tagName,false))) {
				throw new BotError($"Couldn't find a tag named `{tagName}`.");
			}

			if(group.tagIDs.Contains(result.tagId)) {
				throw new BotError($"Group `{groupName}` already contains tag `{tagName}`.");
			}

			group.tagIDs.Add(result.tagId);
		}
		[Command("group subscribe")] [Alias("group sub")]
		public async Task TagGroupSubscribeCommand(string groupName)
		{
			groupName = groupName.ToLowerInvariant();

			var user = Context.user;
			var memory = MemorySystem.memory;
			var globalData = memory.GetData<TagSystem,TagGlobalData>();
			var userData = memory[user].GetData<TagSystem,TagUserData>();

			if(!globalData.tagGroups.TryGetFirst(g => g.Value.name==groupName,out var pair)) {
				throw new BotError($@"Group `{groupName}` does not exist.");
			}

			ulong groupId = pair.Key;

			if(userData.subscribedTagGroups.Contains(groupId)) {
				throw new BotError("You're already subscribed to that group.");
			}
			
			//Subscribe the user to it
			userData.subscribedTagGroups.Add(groupId);
		}
		[Command("group unsubscribe")] [Alias("group unsub")]
		public async Task TagGroupUnsubscribeCommand(string groupName)
		{
			groupName = groupName.ToLowerInvariant();

			var user = Context.user;
			var memory = MemorySystem.memory;
			var globalData = memory.GetData<TagSystem,TagGlobalData>();
			var userData = memory[user].GetData<TagSystem,TagUserData>();

			if(!globalData.tagGroups.TryGetFirst(g => g.Value.name==groupName,out var pair)) {
				throw new BotError($@"Group `{groupName}` does not exist.");
			}

			ulong groupId = pair.Key;

			if(!userData.subscribedTagGroups.Contains(groupId)) {
				throw new BotError("You're already not subscribed to that group.");
			}
			
			//Unsubscribe the user from it
			userData.subscribedTagGroups.Remove(groupId);
		}
		[Command("group global")] [Alias("group setglobal")]
		[RequirePermission("tagsystem.manageglobals")]
		public async Task TagGroupGlobalCommand(string groupName,bool value)
		{
			groupName = groupName.ToLowerInvariant();

			var globalData = MemorySystem.memory.GetData<TagSystem,TagGlobalData>();
			var serverData = Context.server.GetMemory().GetData<TagSystem,TagServerData>();

			if(!globalData.tagGroups.TryGetFirst(g => g.Value.name==groupName,out var pair)) {
				throw new BotError($@"Group `{groupName}` does not exist.");
			}

			ulong groupId = pair.Key;

			bool currentValue = serverData.globalTagGroups.Contains(groupId);
			if(currentValue==value) {
				throw new BotError($@"Group `{groupName}` is already {(value ? "global" : "not global")} for this server.");
			}

			if(value) {
				serverData.globalTagGroups.Add(groupId);
			}else{
				serverData.globalTagGroups.Remove(groupId);
			}
		}

		#endregion

		[Command("test")]
		[RequirePermission]
		public async Task TagTestCommand()
		{
			var memory = MemorySystem.memory;
			var globalData = memory.GetData<TagSystem,TagGlobalData>();
			var userData = memory[Context.user].GetData<TagSystem,TagUserData>();
			var serverData = memory[Context.server].GetData<TagSystem,TagServerData>();

			var dict = new Dictionary<string,string> {
				{ "Tags count",								globalData.tags?.Count.ToString() ?? "NULL" },
				{ "Tag groups count",						globalData.tagGroups?.Count.ToString() ?? "NULL" },

				{ "This server's global Tag Groups count",	serverData.globalTagGroups?.Count.ToString() ?? "NULL" },

				{ "Your tag subscribtion count",			userData.subscribedTags?.Count.ToString() ?? "NULL" },
				{ "Your tag group subscribtion count",		userData.subscribedTagGroups?.Count.ToString() ?? "NULL" },
			};
			
			var embed = MopBot.GetEmbedBuilder(Context)
				.WithDescription(string.Join("\r\n",dict.Select(p => $"**{p.Key}**: `{p.Value}`")))
				.Build();

			await ReplyAsync(embed:embed);
		}
		[Command("cleardata")]
		[RequirePermission]
		public async Task TagClearCommand()
		{
			var memory = MemorySystem.memory;
			var globalData = memory.GetData<TagSystem,TagGlobalData>();
			var userData = memory[Context.user].GetData<TagSystem,TagUserData>();
			var serverData = memory[Context.server].GetData<TagSystem,TagServerData>();

			globalData.tags?.Clear();
			globalData.tagGroups?.Clear();
			globalData.nextTagGroupId = 0;
			globalData.nextTagId = 0;

			serverData.globalTagGroups?.Clear();

			userData.subscribedTags?.Clear();
			userData.subscribedTagGroups?.Clear();
		}
	}
}