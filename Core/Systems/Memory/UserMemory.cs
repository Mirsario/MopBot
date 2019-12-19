#pragma warning disable CS1998 //The async method lacks 'await' operator.

using Discord;

namespace MopBotTwo.Core.Systems.Memory
{
	public sealed class UserMemory : MemoryBase<UserData>
	{
		public IUser User => MopBot.client.GetUser(id);

		//public override void OnDataObjectInitializing(UserData dataObj,IDataTypeProvaider dataProvaider) => dataObj.Initialize(User,dataProvaider);
		//public override void OnDataObjectAccessed(UserData dataObj,IDataTypeProvaider dataProvaider) => dataObj.OnAccessed(User,dataProvaider);

		public override void OnDataCreated(UserData data)
		{
			data.Initialize(User);
		}
	}
}