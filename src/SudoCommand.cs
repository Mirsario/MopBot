using System;
using System.Threading.Tasks;
using MopBotTwo.Systems;

namespace MopBotTwo
{
	[Serializable]
	public class SudoCommand
	{
		public string command;
		public ulong user;

		public SudoCommand(string command,ulong user)
		{
			this.command = command;
			this.user = user;
		}

		public async Task Execute(MessageExt context,Func<StringComparison,string,string> commandFilter = null)
		{
			if(user==0 && !string.IsNullOrWhiteSpace(command)) {
				return;
			}
			
			var executeAs = context.server.GetUser(user);
			if(executeAs==null) {
				throw new BotError($"Sudo user has left the server; Command cannot be executed.");
			}

			var sc = StringComparison.InvariantCultureIgnoreCase;
			string filteredCommand = command
				.Replace("{user}",$"{context.user.Username}#{context.user.Discriminator}",sc)
				.Replace("{userId}",context.user.Id.ToString(),sc)
				.Replace("{userMention}",context.user.Mention,sc)
				.Replace("{channel}",$"<#{context.Channel.Id}>",sc);

			if(commandFilter!=null) {
				filteredCommand = commandFilter(sc,filteredCommand);
			}

			//!shop additem trivia Hoodie 100 "msg #shop-orders \"User {userMention} ({user} / {userId}) has ordered item `{item}`.\""

			try {
				await CommandSystem.ExecuteCommand(new MessageExt(context.message,context.server,executeAs,filteredCommand,true,context.socketTextChannel),true);
			}
			catch(Exception e) {
				throw new BotError(e);
			}
		}
	}
}
