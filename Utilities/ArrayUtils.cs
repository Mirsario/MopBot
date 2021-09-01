using System;
using System.Linq;
using System.Collections.Generic;

namespace MopBot
{
	public class ArrayUtils
	{
		public static void Add<T>(ref T[] array, T value)
		{
			if (array == null) {
				array = new[] { value };

				return;
			}

			int length = array.Length;

			Array.Resize(ref array, length + 1);

			array[length] = value;
		}

		public static void RemoveWhere<T>(ref T[] array, Func<T, bool> predicate)
		{
			List<T> list = null;
			int offset = 0;

			for (int i = 0; i < array.Length; i++) {
				var element = array[i];

				if (predicate(element)) {
					if (list == null) {
						list = array.ToList();
					}

					list.RemoveAt(i + offset--);
				}
			}

			if (list != null) {
				array = list.ToArray();
			}
		}

		public static void RemoveAt<T>(ref T[] array, int index)
		{
			int length = array.Length - 1;
			T[] newArray = new T[length];

			for (int i = 0, j = 0; i < length; i++, j++) {
				if (i == index) {
					j++;
				}

				newArray[i] = array[j];
			}

			array = newArray;
		}
	}
}
