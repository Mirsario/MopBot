using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using Discord.WebSocket;
using Discord.Rest;
using Discord.Commands;
using MopBot.Extensions;
using MopBot.Core.Systems.Permissions;
using MopBot.Core.Systems;
using MopBot.Core.Systems.Commands;
using MopBot.Core;

#pragma warning disable CS1998 //Async method lacks 'await' operators and will run synchronously

namespace MopBot.Common.Systems.Dev
{
	[Group("dev")]
	[Summary("Group for commands that are only available to the bot's developer.")]
	[RequirePermission(SpecialPermission.BotMaster)]
	[SystemConfiguration(AlwaysEnabled = true,Description = "Contains some testing and totally overpowered commands, which only masters of the bot can use.")]
	public partial class DevSystem : BotSystem
	{
		//Makes the bot act as if an already existing message just got sent.
		[Command("notice")]
		public async Task NoticeMessageCommand(ulong messageId) => await NoticeMessageCommand(Context.socketTextChannel,messageId);

		[Command("notice")]
		public async Task NoticeMessageCommand(SocketTextChannel channel,ulong messageId)
		{
			var msg = await channel.GetMessageAsync(messageId);

			if(msg==null) {
				await Context.ReplyAsync($"Unable to find message with such id in channel `{channel.Name}`.");

				return;
			}

			if(!(msg is SocketMessage socketMessage)) {
				socketMessage = ((RestUserMessage)msg).ToSocketUserMessage(channel);
			}

			await Context.ReplyAsync($"Reprocessing message {messageId}...");
			await MessageSystem.MessageReceived(socketMessage);
		}

		[Command("sudo")]
		public async Task SudoCommand(SocketGuildUser user,[Remainder]string command)
			=> await SudoCommand(user,Context.socketTextChannel,command);
		
		[Command("sudo")]
		public async Task SudoCommand(SocketGuildUser user,SocketTextChannel channel,[Remainder]string command)
			=> await CommandSystem.ExecuteCommand(new MessageContext(Context.message,Context.server,user,command,true,channel),true);

		[Command("errortest")]
		public async Task ErrorTest() => throw new Exception("An error has occured, as you requested!");

		[Command("listservers")]
		public async Task ListServers() => await Context.ReplyAsync($"Currently running for the following servers: ```\r\n{string.Join('\r\n',MopBot.client.Guilds.Select(g => $"{g.Name} - {g.Id}"))}```");
		
		[Command("listchannels")]
		public async Task ListChannels() => await Context.ReplyAsync($"The following channels exist on this server: ```\r\n{string.Join('\r\n',Context.server.Channels.Select(c => $"{c.Name} - {c.Id}"))}```");
		
		[Command("listroles")]
		public async Task ListRoles() => await Context.ReplyAsync($"The following roles exist on this server: ```\r\n{string.Join('\r\n',Context.server.Roles.Select(r => $"{r.Name} - {r.Id}"))}```");

		[Command("bash")]
		public async Task BashCommand([Remainder]string command)
		{
			if(!GlobalConfiguration.config.enableBashCommand) {
				throw new BotError("This command is disabled.");
			}

			await Context.ReplyAsync("Executing...");

			var startInfo = new ProcessStartInfo {
				FileName = "sh",
				Arguments = $@"-c ""{Encoding.UTF8.GetString(Encoding.Default.GetBytes(command))}""",
				UseShellExecute = false,
				RedirectStandardOutput = true,
				RedirectStandardError = true
			};

			using var process = new Process {
				StartInfo = startInfo
			};

			process.Start();

			string result = process.StandardOutput.ReadToEnd(); //Doesn't always work.. Should this be moved after WaitForExist?

			process.WaitForExit();

			foreach(var text in StringUtils.SplitMessageText(Context.user.Mention+" Done."+(result.Length>0 ? $" Output:\r\n```{result}```" : ""))) {
				await Context.ReplyAsync(text,false);
			}
		}
	}
}