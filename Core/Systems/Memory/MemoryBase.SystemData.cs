using System;


namespace MopBotTwo.Core.Systems.Memory
{
	public partial class MemoryBase<TPerSystemDataType>
	{
		public virtual void OnDataCreated(TPerSystemDataType data) {}

		public TDataType GetData<TSystem,TDataType>() where TSystem : BotSystem where TDataType : TPerSystemDataType => (TDataType)GetData(typeof(TSystem));
		public TDataType GetData<TDataType>(Type provaiderType) where TDataType : TPerSystemDataType => (TDataType)GetData(provaiderType);
		public TPerSystemDataType GetData(Type provaiderType)
		{
			string key = provaiderType.Name;
			var infoKey = (GetType(),provaiderType.Name);
			var (_,realDataType) = MemorySystem.dataProvaiderInfo[infoKey];

			if(!systemData.TryGetValue(key,out TPerSystemDataType dataObj) || dataObj==null) {
				systemData[key] = dataObj = (TPerSystemDataType)Activator.CreateInstance(realDataType);

				OnDataCreated(dataObj);
			}

			return dataObj;
		}
		public void SetData<TSystem,TDataType>(TDataType value) where TSystem : BotSystem where TDataType : TPerSystemDataType
		{
			var dataType = typeof(TDataType);

			string provaiderName = typeof(TSystem).Name;
			if(!MemorySystem.dataProvaiderInfo.TryGetValue((GetType(),provaiderName),out var tuple) || dataType!=tuple.dataType) {
				throw new ArgumentException($@"Incorrect TDataType generic: ""{dataType}""");
			}

			if(value==null) {
				systemData.Remove(provaiderName);
			} else {
				systemData[provaiderName] = value;
			}
		}
	}
}