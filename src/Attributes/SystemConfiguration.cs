using System;

#pragma warning disable CS1998

namespace MopBotTwo.Systems
{
	public class SystemConfiguration : Attribute
	{
		public bool EnabledByDefault;
		public bool AlwaysEnabled;
		public string Description;
	}
}