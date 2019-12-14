using System;

namespace MopBotTwo.Core.Systems
{
	public class SystemConfiguration : Attribute
	{
		public bool EnabledByDefault;
		public bool AlwaysEnabled;
		public string Description;
	}
}