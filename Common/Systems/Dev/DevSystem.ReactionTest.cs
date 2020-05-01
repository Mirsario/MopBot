using Discord;
using Discord.Commands;
using Discord.WebSocket;
using MopBot.Extensions;
using System.Linq;
using System.Threading.Tasks;

namespace MopBot.Common.Systems.Dev
{
	public partial class DevSystem
	{
		[Command("addreaction")]
		public Task AddReaction(IEmote emote) => AddReaction(Context.server.Id,Context.socketTextChannel.Id,Context.socketTextChannel.GetCachedMessages(1).First().Id,emote);

		[Command("addreaction")]
		public Task AddReaction(ulong messageId,IEmote emote) => AddReaction(Context.server.Id,Context.socketTextChannel.Id,messageId,emote);

		[Command("addreaction")]
		public Task AddReaction(SocketTextChannel channel,ulong messageId,IEmote emote) => AddReaction(Context.server.Id,channel.Id,messageId,emote);

		[Command("addreaction")]
		public async Task AddReaction(ulong serverId,ulong channelId,ulong messageId,IEmote emote)
		{
			var server = MopBot.client.GetServer(serverId);
			var channel = server.GetTextChannel(channelId);
			var msg = ((await channel.GetMessageAsync(messageId)) as IUserMessage) ?? throw new BotError("Invalid message.");

			await msg.AddReactionAsync(emote);
		}
	}
}
