using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using Discord;
using Discord.WebSocket;
using Discord.Rest;
using Discord.Commands;
using MopBotTwo.Extensions;

#pragma warning disable CS1998

namespace MopBotTwo.Systems
{
	[Group("dev")]
	[Summary("Group for commands that are only available to the bot's developer.")]
	[RequirePermission]
	[SystemConfiguration(AlwaysEnabled = true)]
	public class DevCommandsSystem : BotSystem
	{
		//Reprocesses messages.. Basically makes the bot act as if an already existing message just got sent.
		[RequirePermission]
		[Command("notice")]
		public async Task NoticeMessageCommand(ulong messageId) => await NoticeMessageCommand(Context.socketTextChannel,messageId);
		[RequirePermission]
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

		[RequirePermission]
		[Command("sudo")]
		public async Task SudoCommand(SocketGuildUser user,[Remainder]string command)
		{
			await CommandSystem.ExecuteCommand(new MessageExt(Context.message,Context.server,user,command,true,Context.socketTextChannel),true);
		}
		[RequirePermission]
		[Command("sudo")]
		public async Task SudoCommand(SocketGuildUser user,SocketTextChannel channel,[Remainder]string command)
		{
			await CommandSystem.ExecuteCommand(new MessageExt(Context.message,Context.server,user,command,true,channel),true);
		}

		[RequirePermission]
		[Command("addreaction")]
		public Task AddReaction(SocketTextChannel channel,ulong messageId,string emote)
			=> AddReaction(Context.server.Id,channel.Id,messageId,emote);
		[RequirePermission]
		[Command("addreaction")]
		public async Task AddReaction(ulong serverId,ulong channelId,ulong messageId,string emote)
		{
			var server = MopBot.client.GetGuild(serverId);
			if(server==null) {
				throw new BotError("Invalid server.");
			}
			var channel = server.GetChannel(channelId);
			if(channel==null || !(channel is SocketTextChannel txtChannel)) {
				throw new BotError("Invalid channel.");
			}
			var msg = await txtChannel.GetMessageAsync(messageId);

			if(BotUtils.TryGetEmote(emote,out string emoteText)) {
				emote = emoteText;
			}

			if(!Emote.TryParse(emote,out Emote realEmote)) {
				throw new BotError($"Unable to parse emote `{emote}`.");
			}
			
			switch(msg) {
				case SocketMessage sMsg:
					await sMsg.AddReactionAsync(Context.server,realEmote);
					break;
				case RestUserMessage rMsg:
					await rMsg.AddReactionAsync(realEmote);
					break;
			}
		}
		
		[RequirePermission]
		[Command("listroles")]
		public async Task ListRoles()
		{
			await Context.ReplyAsync($"The following roles exist on this server: ```\n{string.Join('\n',Context.server.Roles.Select(r => $"{r.Name} - {r.Id}"))}```");
		}
		
		[RequirePermission]
		[Command("deletemessage")] [Alias("deletemsg","delmsg","delmessage")]
		public async Task DeleteMessage(SocketTextChannel channel,params ulong[] messageIds)
		{
			foreach(var messageId in messageIds) {
				try {
					var msg = await channel.GetMessageAsync(messageId);
					if(msg==null) {
						await Context.ReplyAsync($"Unable to find message with such id in channel `{channel.Name}`.");
						return;
					}

					try {
						await msg.DeleteAsync();
					}
					catch {
						await Context.ReplyAsync("Fail. Not enough permissions?");
					}
				}
				catch(Exception e) {
					await MopBot.HandleException(e);
				}
			}
		}

		[Command("bash")]
		[RequirePermission]
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

			var process = new Process {
				StartInfo = startInfo
			};
			process.Start();

			string result = process.StandardOutput.ReadToEnd(); //Doesn't always work.. Should this be moved after WaitForExist?

			process.WaitForExit();

			foreach(var text in StringUtils.SplitMessageText(Context.user.Mention+" Done."+(result.Length>0 ? $" Output:\n```{result}```" : ""))) {
				await Context.ReplyAsync(text,false);
			}
		}
	}
}