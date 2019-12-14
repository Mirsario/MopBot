using System.Threading.Tasks;
using Discord.WebSocket;

namespace MopBotTwo.Common.Systems.Posting
{
	public abstract class PostPiece
	{
		public abstract Task Execute(SocketTextChannel channel);
	}
}