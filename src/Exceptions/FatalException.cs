using System;

namespace MopBotTwo
{
	public class FatalException : Exception
	{
		public FatalException(string message) : base("FATAL EXCEPTION: "+message) {}
	}
}
