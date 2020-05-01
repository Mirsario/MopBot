using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;
using MopBot.Extensions;
using MopBot.Core.Systems.Memory;
using MopBot.Core.Systems.Permissions;
using Discord.WebSocket;

#pragma warning disable 1998

namespace MopBot.Common.Systems.Tags
{
	public partial class TagSystem
	{
		[Command("group add")]
		[Alias("group new")]
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

			ulong groupId = BotUtils.GenerateUniqueId(globalData.tagGroups.ContainsKey);

			//Create the group
			globalData.tagGroups[groupId] = new TagGroup(user.Id,groupName);

			//Subscribe the user to it
			userData.subscribedTagGroups.Add(groupId);
		}

		[Command("group addtag")]
		public Task TagGroupAddTagCommand(string groupName,string tagName) => TagGroupAddTagInternal(Context.socketUser,groupName,Context.socketUser,tagName);

		[Command("group addtag")]
		public Task TagGroupAddTagCommand(string groupName,SocketUser tagOwner,string tagName) => TagGroupAddTagInternal(Context.socketUser,groupName,tagOwner,tagName);

		[Command("group subscribe")]
		[Alias("group sub")]
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

		[Command("group unsubscribe")]
		[Alias("group unsub")]
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
			var group = pair.Value;

			if(!userData.subscribedTagGroups.Contains(groupId)) {
				throw new BotError("You're already not subscribed to that group.");
			}

			if(group.owner==user.Id) {
				throw new BotError("You cannot unsubscribe from a group you own.");
			}

			//Unsubscribe the user from it
			userData.subscribedTagGroups.Remove(groupId);
		}

		[Command("group global")]
		[Alias("group setglobal")]
		[RequirePermission(SpecialPermission.Owner,"tagsystem.manageglobals")]
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
			} else {
				serverData.globalTagGroups.Remove(groupId);
			}
		}

		private async Task TagGroupAddTagInternal(SocketUser user,string groupName,SocketUser tagOwner,string tagName)
		{
			var (tagId,tag) = GetSingleTagInternal(Context.server,tagOwner,tagName);
			
			groupName = groupName.ToLowerInvariant();

			int groupNameHash = groupName.GetHashCode();

			var memory = MemorySystem.memory;
			var globalData = memory.GetData<TagSystem,TagGlobalData>();

			if(!globalData.tagGroups.TryGetFirst(g => g.Value.name==groupName,out var idGroupPair)) {
				throw new BotError($@"Group `{groupName}` does not exist.");
			}

			var group = idGroupPair.Value;

			if(user.Id!=group.owner) {
				throw new BotError($@"Can't add tags to group `{groupName}`, you're neither that group's owner, nor its maintainer.");
			}

			if(group.tagIDs.Contains(tagId)) {
				throw new BotError($"Group `{groupName}` already contains tag `{tagName}`.");
			}

			group.tagIDs.Add(tagId);
		}
	}
}