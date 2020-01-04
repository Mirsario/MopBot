using System.Linq;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using Discord;
using Discord.WebSocket;
using Discord.Commands;
using MopBotTwo.Extensions;
using MopBotTwo.Core.Systems.Permissions;
using MopBotTwo.Core.Systems;
using MopBotTwo.Core.Systems.Memory;
using MopBotTwo.Core.DataStructures;
using MopBotTwo.Core;

namespace MopBotTwo.Common.Systems.ChannelLinking
{
	//TODO: Rather old system. Needs a rewrite that'll use same code for reposting as quotes in MessageManagingSystem.

	[Group("channellink")]
	[Summary("Group for commands for managing channel linking, which lets admins of servers interconnect selected channels of their servers through the bot.")]
	[RequirePermission(SpecialPermission.Owner,"channellink")]
	[SystemConfiguration(Description = "Bit outdated, but it's a really cool system that lets admins of servers interconnect selected channels of their servers through the bot.")]
	public partial class ChannelLinkingSystem : BotSystem
	{
		public override void RegisterDataTypes()
		{
			RegisterDataType<Memory,ChannelLinkingGlobalData>();
			RegisterDataType<ServerMemory,ChannelLinkingServerData>();
		}
		public override async Task Initialize()
		{
			PermissionSystem.defaultGroups["superadmin"]["managechannellinking"] = true;
		}
		public override async Task OnMessageReceived(MessageExt message)
		{
			var localServerData = message.server.GetMemory().GetData<ChannelLinkingSystem,ChannelLinkingServerData>();
			var channelLinks = localServerData.channelLinks;

			if(channelLinks==null) {
				return;
			}

			ulong channelId = message.messageChannel.Id;

			if(!channelLinks.TryGetValue(channelId,out ulong linkId)) {
				return;
			}

			var channelLinkingData = MemorySystem.memory.GetData<ChannelLinkingSystem,ChannelLinkingGlobalData>();

			if(!channelLinkingData.links.TryGetValue(linkId,out var channelLink)) {
				channelLinks.Remove(channelId);

				return;
			}

			//string inviteUrl = await BotUtils.GetInviteUrl(message.server);
			//string linkedServerName = inviteUrl!=null ? $"({message.server.Name})[{inviteUrl}]" : message.server.Name;

			string authorStr = $"{message.user.Username}#{message.user.Discriminator} [{message.server.Name}/#{message.Channel.Name}]";
			string authorAvatarUrl = message.user.GetAvatarUrl();

			var builder = MopBot.GetEmbedBuilder(message)
				.WithColor(message.socketServerUser.Roles.OrderByDescending(r => r.Position).FirstOrDefault(r => !r.Color.ColorEquals(Discord.Color.Default))?.Color ?? Discord.Color.Default)
				.WithAuthor(authorStr,authorAvatarUrl) //,$@"https://discordapp.com/channels/@me/{message.user.Id}"
				.WithDescription(message.content);

			static bool IsImageUrl(string checkUrl)
			{
				return checkUrl.EndsWith(".png") || checkUrl.EndsWith(".jpg") || checkUrl.EndsWith(".jpeg") || checkUrl.EndsWith(".bmp") || checkUrl.EndsWith(".gif");
			}

			foreach(var test in message.message.Embeds) {
				switch(test.Type) {
					case EmbedType.Gifv:
					case EmbedType.Image:
						builder.Description = builder.Description.Replace(test.Url,"");
						builder.WithImageUrl(test.Url);
						break;
					case EmbedType.Video:
						//builder.Description = builder.Description.Replace(test.Url,"");
						builder.ThumbnailUrl = test.Thumbnail?.Url;
						break;
					case EmbedType.Tweet:
					case EmbedType.Article:
					case EmbedType.Link:
						//builder.Description = builder.Description.Replace(test.Url,"");
						//builder.WithUrl(test.Url);
						break;
				}
			}

			foreach(var attachment in message.message.Attachments) {
				if(IsImageUrl(attachment.Filename.ToLower())) {
					builder.Description = builder.Description.Replace(attachment.Url,"");
					builder.WithImageUrl(attachment.Url);
				}
			}

			ulong botId = MopBot.client.CurrentUser.Id;

			string embedDescription = builder.Description;

			for(int i = 0;i<channelLink.connectedChannels.Count;i++) {
				var channelInfo = channelLink.connectedChannels[i];

				if(channelInfo.channelId==channelId) {
					continue;
				}

				if(!MopBot.client.TryGetServer(channelInfo.serverId,out var server) || !server.TryGetTextChannel(channelInfo.channelId,out var channel)) {
					channelLink.connectedChannels.RemoveAt(i--);
					continue;
				}

				var asyncCollection = channel.GetMessagesAsync(1);
				var enumerable = await asyncCollection.FlattenAsync();

				//Try to append to the previous message instead of posting a new one, if possible.
				if(enumerable?.FirstOrDefault() is SocketUserMessage lastMsg && lastMsg.Author.Id==botId) {
					var lastEmbed = lastMsg.Embeds.FirstOrDefault();

					if(lastEmbed!=null && lastEmbed.Author.HasValue) {
						var author = lastEmbed.Author.Value;

						if(author.Name==authorStr && author.IconUrl==authorAvatarUrl) {
							if(!lastEmbed.Image.HasValue) {
								bool modified = false;

								string newDescription = lastEmbed.Description+"\r\n"+embedDescription;

								await lastMsg.ModifyAsync(p => {
									builder.Description = newDescription;

									p.Content = lastMsg.Content;
									p.Embed = builder.Build();

									modified = true;
								});

								if(modified) {
									continue;
								}
							} else {
								builder.WithAuthor(null,null); //Remove author field for new posts.
							}
						}
					}
				}

				MessageSystem.IgnoreMessage(await channel.SendMessageAsync(embed:builder.Build()));
			}
		}

		private async Task<SocketGuild> GetServerFromId(ulong serverId)
		{
			var server = await Context.Client.GetGuildAsync(serverId);
			if(server==null) {
				await Context.ReplyAsync("Unknown server.");
				return null;
			}

			if(await server.GetCurrentUserAsync()==null) {
				await Context.ReplyAsync("I am not present in that server.");
				return null;
			}

			if(server is SocketGuild socketServer) {
				return socketServer;
			}

			await Context.ReplyAsync("Couldn't get the server.");

			return null;
		}
		private async Task LinkChannel(SocketGuildUser linkOwner,SocketTextChannel localChannel,SocketTextChannel remoteChannel)
		{
			var globalData = MemorySystem.memory.GetData<ChannelLinkingSystem,ChannelLinkingGlobalData>();

			var localServer = localChannel.Guild;
			var localServerMemory = localServer.GetMemory();
			var localServerData = localServerMemory.GetData<ChannelLinkingSystem,ChannelLinkingServerData>();

			var remoteServer = remoteChannel.Guild;
			var remoteServerMemory = remoteServer.GetMemory();
			var remoteServerData = remoteServerMemory.GetData<ChannelLinkingSystem,ChannelLinkingServerData>();

			if(remoteServerData.channelLinks.TryGetValue(remoteChannel.Id,out ulong remoteLinkId) && globalData.links.ContainsKey(remoteLinkId)) {
				throw new BotError("Remote channel already has a link.");
			}

			if(!localServerData.channelLinks.TryGetValue(localChannel.Id,out ulong linkId) || !globalData.links.TryGetValue(linkId,out ChannelLink link)) {
				linkId = BotUtils.GenerateUniqueId(globalData.links.ContainsKey);

				globalData.links[linkId] = link = new ChannelLink(linkOwner.Id);

				localServerData.channelLinks[localChannel.Id] = linkId;
			}

			var ids = new ServerChannelIds(localServer.Id,localChannel.Id);

			if(!link.connectedChannels.Contains(ids)) {
				link.connectedChannels.Add(ids);
			}

			var remoteIds = new ServerChannelIds(remoteServer.Id,remoteChannel.Id);

			if(link.invitedChannels.Contains(remoteIds)) {
				throw new BotError("Remote channel's owner has already been invited to link it.");
			}

			string notification = $"<@{remoteServer.OwnerId}>\r\nAn administrator of server `{localServer.Name}` is requesting to message-link this channel ({remoteChannel.Name}) with their channel `{localChannel.Name}`.\r\nType `!channellink accept {linkId}` to accept.";

			if(!IsEnabledForServer<ChannelLinkingSystem>(remoteServer)) {
				notification += $"\r\n\r\nNote: You need to enable the {nameof(ChannelLinkingSystem)} first. You can do that via this command:\r\n`!systems enable {nameof(ChannelLinkingSystem)}`";
			}

			MessageSystem.IgnoreMessage(await remoteChannel.SendMessageAsync(notification));
			MessageSystem.IgnoreMessage(await Context.ReplyAsync("Channel linking request has been sent to the owner of the remote server."));

			link.invitedChannels.Add(remoteIds);
		}
	}
}