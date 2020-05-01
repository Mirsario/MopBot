using System;

namespace MopBot
{
	public static class Utils
	{
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
		public static T Choose<T>(params T[] arr) => arr[MopBot.Random.Next(arr.Length)];
		
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