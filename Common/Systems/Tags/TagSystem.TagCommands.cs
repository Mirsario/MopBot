using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;
using MopBotTwo.Extensions;
using MopBotTwo.Core.Systems.Memory;
using MopBotTwo.Core.Systems.Commands;
using Discord.WebSocket;

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

			EnsureTagIsNotDefined(tagName);

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

			var (_,tag) = GetSingleTagInternal(Context.server,Context.socketUser,tagName);

			//Update tag text
			tag.text = text;
		}

		[Command("rename")]
		[Summary("Renames a tag.")]
		public async Task TagRenameCommand(string tagOldName,string tagNewName)
		{
			tagOldName = tagOldName.ToLowerInvariant();
			tagNewName = tagNewName.ToLowerInvariant();

			var (_,tag) = GetSingleTagInternal(Context.server,Context.socketUser,tagOldName);

			EnsureTagIsNotDefined(tagNewName);

			tag.name = tagNewName;
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

					return (false,true);
				}

				return (false,true);
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

		[Command("use")]
		[Alias("show")]
		[Summary("Shows a tag.")]
		public Task TagShowCommand(string tagName) => ShowTagInternal(Context.socketServerUser,tagName);

		[Command("use")]
		[Alias("show")]
		[Summary("Shows another user's tag.")]
		public Task TagShowCommand(SocketUser user,string tagName) => ShowTagInternal(user,tagName);

		[Command("useraw")]
		[Alias("showraw","getraw","raw")]
		[Summary("Shows source text of a tag.")]
		public Task TagShowRawCommand(string tagName) => ShowTagInternal(Context.socketServerUser,tagName,true);

		[Command("useraw")]
		[Alias("showraw","getraw","raw")]
		[Summary("Shows source text of another user's tag.")]
		public Task TagShowRawCommand(SocketUser user,string tagName) => ShowTagInternal(user,tagName,true);

		private void EnsureTagIsNotDefined(string tagName)
		{
			var user = Context.socketUser;
			var tags = GetTagsWithName(user,null,tagName);
			ulong userId = user.Id;

			if(tags.Count>0 && tags.Any(t => t.tagInfo.owner==userId)) {
				char cmdSymbol = Context.server.GetMemory().GetData<CommandSystem,CommandServerData>().commandPrefix;

				throw new BotError($"You already own a tag named '{tagName}'.\r\nUse `{cmdSymbol}tag edit {tagName} <text>` to edit it,\r\nOR use `{cmdSymbol}tag remove {tagName}` to remove it.");
			}
		}

		private async Task ShowTagInternal(SocketUser tagOwner,string tagName,bool raw = false)
		{
			var context = Context;
			var tagUser = context.socketServerUser;

			var tag = GetSingleTagInternal(Context.server,tagOwner,tagName).tag;

			var embed = MopBot.GetEmbedBuilder(context)
				.WithAuthor($"{tagUser.GetDisplayName()}:",tagUser.GetAvatarUrl())
				.WithFooter($@"""{tagName}"" by {MopBot.client.GetUser(tag.owner)?.GetDisplayName() ?? "Unknown user"}")
				.WithDescription(raw ? ($"```{StringUtils.EscapeDiscordText(tag.text,true)}\r\n```") : tag.text)
				.WithColor(tagUser.GetColor())
				.Build();

			await context.Channel.SendMessageAsync(embed:embed);

			await context.Delete();
		}
	}
}