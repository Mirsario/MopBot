using System;
using System.Collections.Generic;
using System.Text;
using Discord.Commands;

namespace MopBotTwo.TypeReaders
{
	public abstract class CustomTypeReader : TypeReader
	{
		public abstract Type[] Types { get; }

		protected CustomTypeReader() {}
	}
}
