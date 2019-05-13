using System;

namespace MopBotTwo
{
	public static class Utils
	{
		private static readonly byte[] RandomBuffer = new byte[8];
		
		public static T[] GetEnumValues<T>() where T : Enum
		{
			return (T[])Enum.GetValues(typeof(T));
		}
		public static T GetSafe<T>(ref T value) where T : new()
		{
			if(value==null) {
				value = new T();
			}
			return value;
		}

		public static ulong GetRandomULong()
		{
			MopBot.random.NextBytes(RandomBuffer);
			return BitConverter.ToUInt64(RandomBuffer,0);
		}
		
		//Is there a better way? Am I dumb?
		public static ulong SafeAdd(ulong valueA,ulong valueB)
		{
			unchecked { 
				ulong newValue = valueA+valueB;
				return newValue<valueA ? ulong.MaxValue : newValue;
			}
		}
	}
}