using System;

namespace MopBotTwo.Core.Systems
{
	public class SystemConfiguration : Attribute
	{
		public bool EnabledByDefault { get; set; }
		public bool AlwaysEnabled { get; set; }
		public bool Hidden { get; set; }
		public string Description { get; set; }
	}
}