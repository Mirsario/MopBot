using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using MopBot.Extensions;
using MopBot.Core.Systems.Memory;
using MopBot.Core.Systems.Permissions;

#pragma warning disable CS1998 //Async method lacks 'await' operators and will run synchronously

namespace MopBot.Core.Systems.Channels
{
	[Group("channels")]
	[Alias("channel")]
	[Summary("Group for managing channel roles, like `Rules`, `BotArea`, etc..")]
	[RequirePermission(SpecialPermission.Owner,"managechannels")]
	[SystemConfiguration(AlwaysEnabled = true,Description = "Manages channel roles, like 'Rules', 'BotArea', etc.")]
	public partial class ChannelSystem : BotSystem
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
				} else {
					name = "None";
				}

				return $"{role} - {name}";
			}

			await Context.ReplyAsync($"```{string.Join("\r\n",Utils.GetEnumValues<ChannelRole>().Select(Pair))}```");
		}
	}
}