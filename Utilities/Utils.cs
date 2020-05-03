using System;

namespace MopBot
{
	public static class Utils
	{
		public static T[] GetEnumValues<T>() where T : Enum
			=> (T[])Enum.GetValues(typeof(T));

		public static T GetSafe<T>(ref T value) where T : new()
			=> value ??= new T();

		public static T Choose<T>(params T[] arr)
			=> arr[MopBot.Random.Next(arr.Length)];
		
		public static ulong SafeAdd(ulong valueA,ulong valueB)
		{
			unchecked { 
				ulong newValue = valueA+valueB;

				return newValue<valueA ? ulong.MaxValue : newValue;
			}
		}
	}
}