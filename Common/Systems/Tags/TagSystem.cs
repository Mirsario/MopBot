using System;
using System.Linq;
using System.Collections.Generic;
using Discord;
using Discord.Commands;
using MopBot.Core.Systems;
using MopBot.Core.Systems.Memory;
using Discord.WebSocket;
using MopBot.Extensions;

namespace MopBot.Common.Systems.Tags
{
	[Group("tag")] [Alias("tags")]
	[Summary(@"Group for managing and using shortcuts called ""tags"".")]
	[SystemConfiguration(EnabledByDefault = true,Description = "Lets people create and use cross-server tags (aka message shortcuts). Supports tag groups, tag/tag group subscription and globalization. Is cool.")]
	public partial class TagSystem : BotSystem
	{
		public delegate (bool doReturn,bool removeTag) TagEnumerationFunc(ulong id,Tag tag);

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

			var tagsFound = new List<(ulong tagId, Tag tagInfo)>();

			void AddTagAction(ulong tagId,Tag tag)
			{
				if(tag.name==name && !tagsFound.Any(tuple => tuple.tagId==tagId)) {
					tagsFound.Add((tagId, tag));
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
		public static (ulong tagId,Tag tag) GetSingleTagInternal(SocketGuild server,SocketUser tagOwner,string tagName)
		{
			tagName = tagName.ToLowerInvariant();

			var tagsFound = GetTagsWithName(tagOwner,server,tagName);

			if(tagsFound.Count==0) {
				throw new BotError("Found no tags with such name that you're subscribed to.");
			}

			if(tagsFound.Count!=1) {
				const int MaxTextLength = 30;

				throw new BotError($"{tagsFound.Count} tags have been found: \r\n```{string.Join('\r\n',tagsFound.Select(t => $"{t.tagId} - {t.tagInfo.text.TruncateWithDots(MaxTextLength)}"))}```");
			}

			return tagsFound[0];
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
				} else {
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
				} else {
					groupIds.RemoveAt(i--);
				}
			}
		}

		public static bool ForeachTag(TagGlobalData globalData,List<ulong> tagIds,out (ulong id,Tag tag) tuple,TagEnumerationFunc func)
		{
			if(globalData==null) {
				globalData = MemorySystem.memory.GetData<TagSystem,TagGlobalData>();
			}

			for(int i = 0;i<tagIds.Count;i++) {
				ulong id = tagIds[i];

				if(!globalData.tags.TryGetValue(id,out Tag tag)) {
					tagIds.RemoveAt(i--);

					continue;
				}

				(bool doReturn, bool removeTag) = func(id,tag);

				if(removeTag) {
					tagIds.RemoveAt(i--);
				}

				if(doReturn) {
					tuple = (id, tag);

					return true;
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

				if(!globalData.tagGroups.TryGetValue(groupId,out TagGroup tagGroup)) {
					groupIds.RemoveAt(i--);

					continue;
				}
				
				if(ForeachTag(globalData,tagGroup.tagIDs,out tuple,func)) {
					return true;
				}
			}

			tuple = default;

			return false;
		}
	}
}