namespace MopBot.Core.DataStructures
{
	public struct Color
	{
		public byte r;
		public byte g;
		public byte b;

		public Color(byte r,byte g,byte b)
		{
			this.r = r;
			this.g = g;
			this.b = b;
		}

		public static implicit operator Discord.Color(Color col) => new Discord.Color(col.r,col.g,col.b);
		public static implicit operator Color(Discord.Color col) => new Color(col.R,col.G,col.B);
	}
}