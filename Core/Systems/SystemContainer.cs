using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Discord;
using MopBot.Core.Systems.Memory;

#pragma warning disable CS1998 //Async method lacks 'await' operators and will run synchronously

namespace MopBot.Core.Systems
{
	public abstract class SystemContainer
	{
		public List<BotSystem> systems;

		public bool RegisteringData { get; private set; }

		public SystemContainer()
		{
			systems = new List<BotSystem>();
		}

		public async Task InitializeSystems()
		{
			for (int i = 0; i < systems.Count; i++) {
				await systems[i].PreInitialize();
			}

			for (int i = 0; i < systems.Count; i++) {
				await systems[i].Initialize();
			}
		}

		public async Task UpdateSystems(bool allowBreak)
		{
			for (int i = 0; i < systems.Count; i++) {
				var system = systems[i];

				try {
					if (!await system.Update()) {
						if (allowBreak) {
							break;
						}
					} else if (MopBot.client?.ConnectionState == ConnectionState.Connected) {
						foreach (var server in MopBot.client.Guilds) {
							if (system.IsEnabledForServer(server)) {
								await system.ServerUpdate(server);
							}
						}
					}
				}
				catch (Exception e) {
					await MopBot.HandleException(e);
					break;
				}
			}
		}

		public async Task<BotSystem> AddSystem<T>() where T : BotSystem
			=> await AddSystem(typeof(T));

		public async Task<BotSystem> AddSystem(Type type)
		{
			BotSystem system = (BotSystem)Activator.CreateInstance(type);

			system.Owner = this;

			RegisteringData = true;

			system.RegisterDataType<ServerMemory, ServerData>();
			system.RegisterDataTypes();

			RegisteringData = false;

			systems.Add(system);

			BotSystem.allSystems.Add(system);

			BotSystem.typeToSystem[type] = system;
			BotSystem.nameToSystem[type.Name] = system;

			Console.WriteLine($"Added {type.Name} system to SystemOwner {GetType().Name}");

			return system;
		}
	}
}
