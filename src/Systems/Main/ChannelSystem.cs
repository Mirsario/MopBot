using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using MopBotTwo.Extensions;

#pragma warning disable CS1998

namespace MopBotTwo.Systems
{
	public enum ChannelRole
	{
		Default,
		Welcome,
		Logs,
		BotArea,
		Rules
	}

	public class ChannelServerData : ServerData
	{
		public Dictionary<ChannelRole,ulong> channelByRole;

		public override void Initialize(SocketGuild server)
		{
			CheckChannels(server);
		}

		public void CheckChannels(SocketGuild server)
		{
			if(channelByRole==null) {
				channelByRole = new Dictionary<ChannelRole,ulong>();
				const string general = "general";
				var channels = server.Channels;
				if(!channels.TryGetFirst(c => c.Name==general,out SocketGuildChannel channel) && !channels.TryGetFirst(c => c.Name.Contains(general),out channel)) {
					int maxUsers = 0;
					foreach(var tempChannel in channels) {
						maxUsers = Math.Max(maxUsers,tempChannel.Users.Count);
					}
					channel = channels.Where(c => c.Users.Count==maxUsers).OrderBy(c => c.Position).FirstOrDefault();
				}
				channelByRole[ChannelRole.Default] = channel.Id;
				channelByRole[ChannelRole.Welcome] = channel.Id;
			}
		}

		public bool TryGetChannelByRoles(out SocketTextChannel channel,params ChannelRole[] roles)
		{
			for(int i = 0;i<roles.Length;i++) {
				if(TryGetChannelByRole(roles[i],out channel)) {
					return true;
				}
			}
			channel = null;
			return false;
		}
		public bool TryGetChannelByRole(ChannelRole role,out SocketTextChannel channel) => (channel = GetChannelByRole(role))!=null;
		public SocketTextChannel GetChannelByRole(ChannelRole role) => channelByRole.TryGetValue(role,out ulong id) ? (MopBot.client.GetChannel(id) as SocketTextChannel) : null;
	}

	[Group("channels")]
	[Alias("channel")]
	[RequirePermission(SpecialPermission.Owner,"managechannels")]
	[SystemConfiguration(AlwaysEnabled = true)]
	public class ChannelSystem : BotSystem
	{
		public override void RegisterDataTypes()
		{
			RegisterDataType<ServerMemory,ChannelServerData>();
		}

		[Command("assign")]
		[Alias("set")]
		public async Task SetChannelRole(ChannelRole role,IChannel channel)
		{
			Context.server.GetMemory().GetData<ChannelSystem,ChannelServerData>().channelByRole[role] = channel.Id;
			await Context.ReplyAsync("Success.");
		}

		[Command("list")]
		public async Task ListChannelRoles()
		{
			var server = Context.server;
			var dict = server.GetMemory().GetData<ChannelSystem,ChannelServerData>().channelByRole;
			string Pair(ChannelRole role)
			{
				//$"{e.ToString()} - {((dict.TryGetValue(e,out ulong? id) && id.HasValue) ? (server.GetChannel(id.Value)?.Name ?? "Null") : "Null")
				string name;
				SocketGuildChannel channel;
				if(dict.TryGetValue(role,out ulong id) && (channel = server.GetChannel(id))!=null) {
					name = $"#{channel.Name}";
				}else{
					name = "None";
				}
				return $"{role} - {name}";
			}
			await Context.ReplyAsync($"```{string.Join('\n',Utils.GetEnumValues<ChannelRole>().Select(Pair))}```");
		}
	}
}