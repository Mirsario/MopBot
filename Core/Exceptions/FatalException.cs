using System;

namespace MopBot
{
	public class FatalException : Exception
	{
		public FatalException(string message) : base("FATAL EXCEPTION: " + message) { }
	}
}
