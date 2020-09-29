using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;
using MopBot.Core.Systems.Permissions;
using MopBot.Extensions;

#pragma warning disable CS1998 //Async method lacks 'await' operators and will run synchronously

namespace MopBot.Common.Systems.Welcoming
{
	[Group("welcoming")]
	[Alias("welcome")]
	[Summary("Group for controlling WelcomeSystem.")]
	[RequirePermission("welcome.manage")]
	partial class WelcomeSystem
	{
		[Command("setchannel")]
		[Summary("Sets the channel into which welcomes should be sent.")]
		public async Task SetJoinMessage(SocketTextChannel channel)
			=> Context.server.GetMemory().GetData<WelcomeSystem, WelcomeServerData>().channel = channel.Id;

		[Command("setjoinmessage")]
		[Alias("setjoinmsg")]
		[Summary("Sets the message that users are first greeted with.")]
		public async Task SetJoinMessage([Remainder] string message = null)
			=> Context.server.GetMemory().GetData<WelcomeSystem, WelcomeServerData>().messageJoin = message;

		[Command("setrejoinmessage")]
		[Alias("setrejoinmsg")]
		[Summary("Sets the message that users who already visited this server before will see.")]
		public async Task SetRejoinMessage([Remainder] string message = null)
			=> Context.server.GetMemory().GetData<WelcomeSystem, WelcomeServerData>().messageRejoin = message;
	}
}
