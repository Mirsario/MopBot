using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Discord.Commands;
using Discord.WebSocket;
using MopBotTwo.Core.Systems.Memory;


namespace MopBotTwo.Core.Systems
{
	public abstract class BotSystem : ModuleBase<MessageExt>
	{
		public static List<BotSystem> systems = new List<BotSystem>();
		public static Dictionary<Type,BotSystem> typeToSystem = new Dictionary<Type,BotSystem>();
		public static Dictionary<string,BotSystem> nameToSystem = new Dictionary<string,BotSystem>(StringComparer.InvariantCultureIgnoreCase);

		public readonly string name;
		public readonly SystemConfiguration configuration;

		public BotSystem()
		{
			var type = GetType();
			name = type.Name;
			configuration = GetConfiguration(type);
		}

		public virtual async Task PreInitialize() {}
		public virtual async Task Initialize() {}
		public virtual void RegisterDataTypes() {}
		public virtual async Task<bool> Update() => true;
		public virtual async Task ServerUpdate(SocketGuild server) {}

		public virtual async Task OnUserJoined(SocketGuildUser user) {}
		public virtual async Task OnMessageReceived(MessageExt message) {}
		public virtual async Task OnMessageDeleted(MessageExt message) {}
		public virtual async Task OnReactionAdded(MessageExt message,SocketReaction reaction) {}

		public T GetMemory<T>(SocketGuild server) where T : ServerData => MemorySystem.memory[server].GetData<T>(GetType());

		public void RegisterDataType<TMemoryType,TDataType>() where TMemoryType : MemoryBase where TDataType : MemoryDataBase
		{
			MemorySystem.dataProvaiderInfo[(typeof(TMemoryType),GetType().Name)] = (this,typeof(TDataType));
		}
		public bool IsEnabledForServer(SocketGuild server) => IsEnabledForServer(this,server);

		public static SystemConfiguration GetConfiguration(Type type)
		{
			var attributes = type.GetCustomAttributes(typeof(SystemConfiguration),true);
			if(attributes!=null && attributes.Length>0) {
				return (SystemConfiguration)attributes[0];
			}
			return new SystemConfiguration();
		}
		
		public static bool IsEnabled<T>() where T : BotSystem => typeToSystem.ContainsKey(typeof(T));
		public static bool IsEnabledForServer<T>(SocketGuild server) where T : BotSystem => typeToSystem.TryGetValue(typeof(T),out var system) && IsEnabledForServer(system,server);
		public static bool IsEnabledForServer(BotSystem system,SocketGuild server) => system.configuration.AlwaysEnabled || (system.GetMemory<ServerData>(server).isEnabled ?? system.configuration.EnabledByDefault);

		public static async Task CallForEnabledSystems(SocketGuild server,Func<BotSystem,Task> func)
		{
			foreach(var system in systems) {
				if(system.IsEnabledForServer(server)) {
					await func(system);
				}
			}
		}
	}
}