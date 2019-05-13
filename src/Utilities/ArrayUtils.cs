using System;

namespace MopBotTwo
{
	public class ArrayUtils
	{
		public static void Add<T>(ref T[] array,T value)
		{
			if(array==null) {
				array = new[] { value };
			}else{
				int length = array.Length;
				Array.Resize(ref array,length+1);
				array[length] = value;
			}
		}
		public static void RemoveAt<T>(ref T[] array,int index)
		{
			int length = array.Length-1;
			T[] newArray = new T[length];
			int j = 0;
			for(int i = 0;i<length;i++,j++) {
				if(i==index) {
					j++;
				}
				newArray[i] = array[j];
			}
			array = newArray;
		}
	}
}
