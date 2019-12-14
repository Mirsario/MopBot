using System;

namespace MopBotTwo
{
	public class BotError : Exception
	{
		public BotError() : base("") {}
		public BotError(string message) : base(message) {}
		public BotError(Exception subException,string message = null) : base(message ?? $"An exception of type {subException.GetType().Name} has occured when executing the last command.",subException) {}
	}
}
