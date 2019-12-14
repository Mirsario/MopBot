using System.Threading.Tasks;
using Discord.WebSocket;

namespace MopBotTwo.Common.Systems.Posting
{
	public class TextPostPiece : PostPiece
	{
		public string text;

		public TextPostPiece(string text)
		{
			this.text = text;
		}

		public override Task Execute(SocketTextChannel channel) => channel.SendMessageAsync(text);
	}
}