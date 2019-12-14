using System;
using Discord.Commands;

namespace MopBotTwo.TypeReaders
{
	public abstract class CustomTypeReader : TypeReader
	{
		public abstract Type[] Types { get; }

		protected CustomTypeReader() {}
	}
}
