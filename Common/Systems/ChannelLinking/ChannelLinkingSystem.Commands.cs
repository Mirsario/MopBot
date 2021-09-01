using System.Linq;
using System.Threading.Tasks;
using Discord.WebSocket;
using Discord.Commands;
using MopBot.Extensions;
using MopBot.Core.Systems.Memory;
using MopBot.Core.DataStructures;
using MopBot.Core.Systems;

namespace MopBot.Common.Systems.ChannelLinking
{
	//TODO: Rather old system. Needs a rewrite that'll use same code for reposting as quotes in MessageManagingSystem.

	public partial class ChannelLinkingSystem
	{
		[Command("add")]
		public Task LinkChannelCommand(ulong serverId, string remoteChannelName) => LinkChannelCommand(Context.socketTextChannel, serverId, remoteChannelName);
		
		[Command("add")]
		public async Task LinkChannelCommand(SocketTextChannel localChannel, ulong serverId, string remoteChannelName)
		{
			var remoteServer = await GetServerFromId(serverId);

			if (remoteServer == null) {
				return;
			}

			var remoteChannel = remoteServer.Channels.FirstOrDefault(c => c.Name == remoteChannelName);

			if (remoteChannel == null) {
				throw new BotError("Channel not found.");
			}

			if (remoteChannel is not SocketTextChannel textChannel) {
				throw new BotError("Channel must be a text channel.");
			}

			await LinkChannel(Context.socketServerUser, localChannel, textChannel);
		}
		
		[Command("accept")]
		public Task AcceptLinkInviteCommand(ulong linkId) => AcceptLinkInviteCommand(Context.socketTextChannel, linkId);
		
		[Command("accept")]
		public async Task AcceptLinkInviteCommand(SocketTextChannel localChannel, ulong linkId)
		{
			var globalData = MemorySystem.memory.GetData<ChannelLinkingSystem, ChannelLinkingGlobalData>();

			if (!globalData.links.TryGetValue(linkId, out var link)) {
				throw new BotError("Invalid link id.");
			}

			var localIds = new ServerChannelIds(localChannel.Guild.Id, localChannel.Id);

			if (!link.invitedChannels.Contains(localIds)) {
				throw new BotError("There are no invites to that link associated with this channel.");
			}

			var localServer = localChannel.Guild;
			var localServerData = localServer.GetMemory().GetData<ChannelLinkingSystem, ChannelLinkingServerData>();

			localServerData.channelLinks[localChannel.Id] = linkId;

			if (!link.connectedChannels.Contains(localIds)) {
				link.connectedChannels.Add(localIds);
			}

			link.invitedChannels.Remove(localIds);

			string Notification = $"This channel is now linked with channel `{localChannel.Name}` from server `{localServer.Name}`! :confetti_ball::confetti_ball::confetti_ball:";

			for (int i = 0; i < link.connectedChannels.Count; i++) {
				var ids = link.connectedChannels[i];

				if ((ids.serverId == localServer.Id && ids.channelId == localChannel.Id) || !ids.TryGetTextChannel(out var channel)) {
					continue;
				}

				await channel.SendMessageAsync(Notification);
			}

			string channelList = string.Join(
				"\r\n",
				link.connectedChannels
					.Where(ids => ids.channelId != localChannel.Id)
					.Select(ids => ids.TryGetChannel(out var channel) ? $"{channel.Guild.Name}/#{channel.Name}" : "Unknown")
			);

			MessageSystem.IgnoreMessage(await localChannel.SendMessageAsync($"This channel is now linked with the following channels: ```\r\n{channelList}\r\n```\r\n:confetti_ball::confetti_ball::confetti_ball:"));
		}
	}
}
