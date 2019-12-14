using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;
using MopBotTwo.Extensions;
using MopBotTwo.Core.Systems.Memory;
using MopBotTwo.Core.Systems.Commands;
using Discord.WebSocket;

#pragma warning disable 1998

namespace MopBotTwo.Common.Systems.Tags
{
	public partial class TagSystem
	{
		[Command("add")]
		[Alias("new")]
		[Summary("Creates a new tag.")]
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
				char cmdSymbol = memory[server].GetData<CommandSystem,CommandServerData>().commandPrefix;

				throw new BotError($"You already own a tag named '{tagName}'.\nUse `{cmdSymbol}tag edit {tagName} <text>` to edit it,\nOR use `{cmdSymbol}tag remove {tagName}` to remove it.");
			}

			ulong tagId = BotUtils.GenerateUniqueId(globalData.tags.ContainsKey);

			//Create tag
			globalData.tags[tagId] = new Tag(user.Id,tagName,text);

			//Subscribe the user to it
			if(!userData.subscribedTags.Contains(tagId)) {
				userData.subscribedTags.Add(tagId);
			}
		}
		[Command("edit")]
		[Alias("modify")]
		[Summary("Edits a tag.")]
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
		[Command("remove")]
		[Alias("delete")]
		[Summary("Removes a tag.")]
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
					return (false, true);
				}
				return (false, true);
			});

			if(numRemoved==0) {
				throw new BotError("Couldn't find a tag with such name.");
			}
		}

		[Command]
		[Priority(-1)]
		public Task TagCommand(string tagName) => TagShowCommand(tagName);

		[Command]
		[Priority(-1)]
		public Task TagCommand(SocketUser user,string tagName) => TagShowCommand(user,tagName);

		[Command("show")]
		[Alias("use")]
		[Summary("Shows a tag.")]
		public Task TagShowCommand(string tagName) => ShowTagInternal(Context.socketServerUser,tagName);

		[Command("show")]
		[Alias("use")]
		[Summary("Shows another user's tag.")]
		public Task TagShowCommand(SocketUser user,string tagName) => ShowTagInternal(user,tagName);

		[Command("showraw")]
		[Alias("getraw")]
		[Summary("Shows source text of a tag.")]
		public Task TagShowRawCommand(string tagName) => ShowTagRawInternal(Context.socketServerUser,tagName);

		[Command("showraw")]
		[Alias("getraw")]
		[Summary("Shows source text of another user's tag.")]
		public Task TagShowRawCommand(SocketUser user,string tagName) => ShowTagRawInternal(user,tagName);

		private async Task ShowTagInternal(SocketUser tagOwner,string tagName)
		{
			var context = Context;
			var tagUser = context.socketServerUser;

			var tag = GetSingleTagInternal(tagOwner,tagName);

			var embed = MopBot.GetEmbedBuilder(context)
				.WithAuthor($"{tagUser.Name()}:",tagUser.GetAvatarUrl())
				.WithFooter($@"""{tagName}"" by {MopBot.client.GetUser(tag.owner)?.Name() ?? "Unknown user"}")
				.WithDescription(tag.text)
				.WithColor(tagUser.GetColor())
				.Build();

			await context.Channel.SendMessageAsync(embed:embed);

			await context.Delete();
		}
		private async Task ShowTagRawInternal(SocketUser tagOwner,string tagName)
		{
			var context = Context;
			var tagUser = context.socketServerUser;

			var tag = GetSingleTagInternal(tagOwner,tagName);

			var embed = MopBot.GetEmbedBuilder(context)
				.WithAuthor($"{tagUser.Name()}:",tagUser.GetAvatarUrl())
				.WithFooter($@"""{tagName}"" by {MopBot.client.GetUser(tag.owner)?.Name() ?? "Unknown user"}")
				.WithDescription($"```{StringUtils.EscapeDiscordText(tag.text)}```")
				.WithColor(tagUser.GetColor())
				.Build();

			await context.Channel.SendMessageAsync(embed: embed);

			await context.Delete();
		}
		private Tag GetSingleTagInternal(SocketUser tagOwner,string tagName)
		{
			var context = Context;

			tagName = tagName.ToLowerInvariant();

			var server = context.server;
			var tagsFound = GetTagsWithName(tagOwner,server,tagName);

			if(tagsFound.Count==0) {
				throw new BotError("Found no tags with such name that you're subscribed to.");
			}

			if(tagsFound.Count!=1) {
				const int MaxTextLength = 30;

				throw new BotError($"{tagsFound.Count} tags have been found: \r\n```{string.Join('\n',tagsFound.Select(t => $"{t.tagId} - {t.tagInfo.text.TruncateWithDots(MaxTextLength)}"))}```");
			}

			return tagsFound[0].tagInfo;
		}
	}
}