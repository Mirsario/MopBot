using System.Threading.Tasks;
using Discord.WebSocket;

namespace MopBot.Common.Systems.Posting
{
	public class FilePostPiece : TextPostPiece
	{
		public string filePath;
		public bool deleteAfterPosting;

		public FilePostPiece(string filePath, bool deleteAfterPosting = false) : this(filePath, null, deleteAfterPosting) { }
		public FilePostPiece(string filePath, string text, bool deleteAfterPosting = false) : base(text)
		{
			this.filePath = filePath;
			this.deleteAfterPosting = deleteAfterPosting;
		}

		public override Task Execute(SocketTextChannel channel) => channel.SendFileAsync(filePath, text);
	}
}