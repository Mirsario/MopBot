#pragma warning disable CS1998

namespace MopBotTwo
{
	public class UserData : MemoryDataBase
	{
		//public virtual void Initialize(IUser user,IDataTypeProvaider provaider) {}
		//public virtual void OnAccessed(IUser user,IDataTypeProvaider provaider) {}
	}
	public class UserMemory : MemoryBase<UserData>
	{
		//public IUser User => MopBot.client.GetUser(id);

		//public override void OnDataObjectInitializing(UserData dataObj,IDataTypeProvaider dataProvaider) => dataObj.Initialize(User,dataProvaider);
		//public override void OnDataObjectAccessed(UserData dataObj,IDataTypeProvaider dataProvaider) => dataObj.OnAccessed(User,dataProvaider);
	}
}