using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Discord.Commands;
using Discord.WebSocket;
using MopBot.Core.Systems.Memory;
using Discord;

#pragma warning disable CS1998 //Async method lacks 'await' operators and will run synchronously

namespace MopBot.Core.Systems
{
	public abstract class BotSystem : ModuleBase<MessageContext>
	{
		public static List<BotSystem> allSystems = new List<BotSystem>();
		public static Dictionary<Type, BotSystem> typeToSystem = new Dictionary<Type, BotSystem>();
		public static Dictionary<string, BotSystem> nameToSystem = new Dictionary<string, BotSystem>(StringComparer.InvariantCultureIgnoreCase);

		public readonly string Name;
		public readonly SystemConfiguration Configuration;

		public SystemContainer Owner { get; internal set; }

		public BotSystem()
		{
			var type = GetType();

			Name = type.Name;
			Configuration = GetConfiguration(type);
		}

		public virtual async Task PreInitialize() { }
		
		public virtual async Task Initialize() { }
		
		public virtual void RegisterDataTypes() { }
		
		public virtual async Task<bool> Update() => true;
		
		public virtual async Task ServerUpdate(SocketGuild server) { }

		public virtual async Task OnUserJoined(SocketGuildUser user) { }
		
		public virtual async Task OnUserLeft(SocketGuildUser user) { }
		
		public virtual async Task OnUserUpdated(Cacheable<SocketGuildUser, ulong> oldUser, SocketGuildUser newUser) { }
		
		public virtual async Task OnMessageReceived(MessageContext context) { }
		
		public virtual async Task OnMessageDeleted(MessageContext context) { }
		
		public virtual async Task OnMessageUpdated(MessageContext context, IMessage oldMessage) { }

		public virtual async Task OnReactionAdded(MessageContext context, SocketReaction reaction) { }

		public T GetMemory<T>(SocketGuild server) where T : ServerData => MemorySystem.memory[server].GetData<T>(GetType());

		public void RegisterDataType<TMemoryType, TDataType>() where TMemoryType : MemoryBase where TDataType : MemoryDataBase
		{
			if (!Owner.RegisteringData) {
				throw new Exception($"Cannot register data types outside of '{nameof(RegisterDataTypes)}' method!");
			}

			MemorySystem.dataProvaiderInfo[(typeof(TMemoryType), GetType().Name)] = (this, typeof(TDataType));
		}

		public bool IsEnabledForServer(SocketGuild server)
			=> IsEnabledForServer(this, server);

		public static SystemConfiguration GetConfiguration(Type type)
		{
			object[] attributes = type.GetCustomAttributes(typeof(SystemConfiguration), true);

			if (attributes != null && attributes.Length > 0) {
				return (SystemConfiguration)attributes[0];
			}

			return new SystemConfiguration();
		}

		public static bool IsEnabled<T>() where T : BotSystem
			=> typeToSystem.ContainsKey(typeof(T));

		public static bool IsEnabledForServer<T>(SocketGuild server) where T : BotSystem
			=> typeToSystem.TryGetValue(typeof(T), out var system) && IsEnabledForServer(system, server);

		public static bool IsEnabledForServer(BotSystem system, SocketGuild server)
			=> system.Configuration.AlwaysEnabled || (system.GetMemory<ServerData>(server).isEnabled ?? system.Configuration.EnabledByDefault);

		public static async Task CallForEnabledSystems(SocketGuild server, Func<BotSystem, Task> func)
		{
			foreach (var system in allSystems) {
				if (system.IsEnabledForServer(server)) {
					await func(system);
				}
			}
		}
	}
}
