using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;

#pragma warning disable CS1998

namespace MopBotTwo
{
	//Used for important things, like configuring a list of developers, or global permament toggling for systems.
	public class GlobalConfiguration
	{
		public class Config
		{
			public string token;
			public ulong[] masterUsers;
			public ulong[] usersToPingForExceptions;
			public ulong? logChannel;
			public bool enableBashCommand;
			public Dictionary<string,bool> systemToggles;
			public int maxMemoryBackups = 10;
		}

		public const string ConfigurationFile = "BotConfig.json";
		
		public static Config config;

		public static void Initialize()
		{
			if(File.Exists(ConfigurationFile)) {
				config = JsonConvert.DeserializeObject<Config>(File.ReadAllText(ConfigurationFile));
			}
			if(config==null) {
				config = new Config();
				Save();
			}
		}

		public static void Save()
		{
			File.WriteAllText(ConfigurationFile,JsonConvert.SerializeObject(config,Formatting.Indented));
		}
	}
}
