using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Discord;
using Discord.WebSocket;
using Discord.Commands;
using MopBotTwo.Extensions;

#pragma warning disable CS1998

namespace MopBotTwo.Systems
{
	//TODO: Rather old system. Needs a rewrite that'll use same code for reposting as quotes in MessageManagingSystem.

	[Group("channellink")]
	[RequirePermission(SpecialPermission.Owner,"managechannellinking")]
	public class ChannelLinkingSystem : BotSystem
	{
		public struct ChannelLink
		{
			public ulong serverId;
			public ulong channelId;

			public ChannelLink(ulong serverId,ulong channelId)
			{
				this.serverId = serverId;
				this.channelId = channelId;
			}
		}
		public class ChannelLinkingServerData : ServerData
		{
			public Dictionary<ulong,List<ChannelLink>> linkedServerChannels;

			public override void Initialize(SocketGuild server)
			{
				linkedServerChannels = new Dictionary<ulong,List<ChannelLink>>();
			}
		}
		
		public override void RegisterDataTypes()
		{
			RegisterDataType<ServerMemory,ChannelLinkingServerData>();
		}
		public override async Task Initialize()
		{
			PermissionSystem.defaultGroups["superadmin"]["managechannellinking"] = true;
		}
		public override async Task OnMessageReceived(MessageExt message)
		{
			var localServerData = message.server.GetMemory().GetData<ChannelLinkingSystem,ChannelLinkingServerData>();
			var linkedChannels = localServerData.linkedServerChannels;
			if(linkedChannels==null || !linkedChannels.TryGetValue(message.messageChannel.Id,out var channelList)) {
				return;
			}

			List<ChannelLink> readyLinks = new List<ChannelLink>();
			for(int i = 0;i<channelList.Count;i++) {
				var link = channelList[i];
				var server = MopBot.client.GetGuild(link.serverId);
				var channel = server?.GetChannel(link.channelId);
				if(server==null || channel==null) {
					channelList.RemoveAt(i);
					i--;
				}
				var remoteServerData = server.GetMemory().GetData<ChannelLinkingSystem,ChannelLinkingServerData>();
				if(remoteServerData.linkedServerChannels.Any(p => p.Value.Any(l => l.serverId==message.server.Id && l.channelId==message.messageChannel.Id))) {
					readyLinks.Add(link);
				}
			}
			if(readyLinks.Count==0) {
				return;
			}

			var builder = MopBot.GetEmbedBuilder(message);
			builder.WithColor(message.socketServerUser.Roles.OrderByDescending(r => r.Position).FirstOrDefault(r => !r.Color.ColorEquals(Discord.Color.Default))?.Color ?? Discord.Color.Default);
			builder.WithAuthor(message.user.Username,message.user.GetAvatarUrl());
			builder.WithDescription(message.content);
			bool IsImageUrl(string checkUrl)
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
						builder.Description = builder.Description.Replace(test.Url,"");
						break;
					case EmbedType.Tweet:
					case EmbedType.Article:
					case EmbedType.Link:
						builder.Description = builder.Description.Replace(test.Url,"");
						builder.WithUrl(test.Url);
						break;
				}
			}

			foreach(var attachment in message.message.Attachments) {
				if(IsImageUrl(attachment.Filename.ToLower())) {
					builder.Description = builder.Description.Replace(attachment.Url,"");
					builder.WithImageUrl(attachment.Url);
				}
			}

			foreach(var link in readyLinks) {
				var channel = MopBot.client.GetGuild(link.serverId).GetChannel(link.channelId) as ITextChannel;
				ulong? id = (await channel.SendMessageAsync("",embed:builder.Build()))?.Id;
				if(id!=null) {
					MessageSystem.messagesToIgnore.Add(id.Value);
				}
			}
		}

		[Command("add")]
		public async Task LinkChannelCommand(ulong serverId,string channelName)
		{
			var server = await GetServerFromId(serverId);
			if(server==null) {
				return;
			}
			var channel = server.Channels.FirstOrDefault(c => c.Name==channelName);
			if(channel==null) {
				await Context.ReplyAsync("Channel does not exist, or I cannot see it.");
				return;
			}
			await LinkChannel(Context.server,Context.socketTextChannel,server,(SocketTextChannel)channel);
		}
		[Command("add")]
		public async Task LinkChannelCommand(ulong serverId,ulong channelId)
		{
			var server = await GetServerFromId(serverId);
			if(server==null) {
				return;
			}
			var channel = server.GetChannel(channelId);
			if(channel==null) {
				await Context.ReplyAsync("Channel does not exist, or I cannot see it.");
				return;
			}
			await LinkChannel(Context.server,Context.socketTextChannel,server,(SocketTextChannel)channel);
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
			await Context.ReplyAsync("Couldn't get the server right.");
			return null;
		}
		private async Task LinkChannel(SocketGuild localServer,SocketTextChannel localChannel,SocketGuild remoteServer,SocketTextChannel remoteChannel)
		{
			try {
				var localServerData = localServer.GetMemory().GetData<ChannelLinkingSystem,ChannelLinkingServerData>();
				if(!localServerData.linkedServerChannels.TryGetValue(localChannel.Id,out var localChannelList)) {
					localServerData.linkedServerChannels[localChannel.Id] = localChannelList = new List<ChannelLink>();
				}
				var remoteServerData = remoteServer.GetMemory().GetData<ChannelLinkingSystem,ChannelLinkingServerData>();
				if(!remoteServerData.linkedServerChannels.TryGetValue(localChannel.Id,out var remoteChannelList)) {
					remoteServerData.linkedServerChannels[localChannel.Id] = remoteChannelList = new List<ChannelLink>();
				}
				var link = new ChannelLink(remoteServer.Id,remoteChannel.Id);
				if(!localChannelList.Contains(link)) {
					localChannelList.Add(link);
				}
				if(remoteChannelList.Any(l => l.serverId==localServer.Id && l.channelId==localChannel.Id)) {
					await localChannel.SendMessageAsync($"This channel is now linked with channel `{remoteChannel.Name}` from server `{remoteServer.Name}`! :confetti_ball::confetti_ball::confetti_ball:");
					await remoteChannel.SendMessageAsync($"This channel is now linked with channel `{localChannel.Name}` from server `{localServer.Name}`! :confetti_ball::confetti_ball::confetti_ball:");
				} else {
					await localChannel.SendMessageAsync("Channel linking request will be sent to the owner of the server that we're trying to link to.");
					await remoteChannel.SendMessageAsync($"<@{remoteServer.OwnerId}>\nAn administrator of server `{localServer.Name}` is requesting to message-link this channel ({remoteChannel.Name}) with their channel `{localChannel.Name}`.\nType `!channellink add {localServer.Id} {localChannel.Name}` to accept this offer");
				}
			}
			catch(Exception e) {
				await MopBot.HandleException(e);
			}
		}
	}
}