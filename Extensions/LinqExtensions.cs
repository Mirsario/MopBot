using System;
using System.Collections.Generic;

namespace MopBot.Extensions
{
	public static class LinqExtensions
	{
		public static IEnumerable<T> ExceptIndex<T>(this IEnumerable<T> source, int index)
		{
			int i = 0;

			foreach (var value in source) {
				if (i++ != index) {
					yield return value;
				}
			}
		}

		public static int FirstIndex<T>(this IEnumerable<T> source, Func<T, bool> predicate)
		{
			int index = 0;

			foreach (var item in source) {
				if (predicate(item)) {
					return index;
				}

				index++;
			}

			return -1;
		}

		public static bool TryGetFirst<T>(this IEnumerable<T> source, out T result)
		{
			foreach (var value in source) {
				result = value;

				return true;
			}

			result = default;

			return false;
		}

		public static bool TryGetFirst<T>(this IEnumerable<T> source, Func<T, bool> predicate, out T result)
		{
			foreach (var value in source) {
				if (predicate(value)) {
					result = value;

					return true;
				}
			}

			result = default;

			return false;
		}

		public static IEnumerable<TResult> SelectIgnoreNull<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, TResult> selector)
		{
			if (source == null) {
				throw new ArgumentNullException(nameof(source));
			}

			if (selector == null) {
				throw new ArgumentNullException(nameof(selector));
			}

			return SelectIgnoreNullIterator(source, selector);
		}

		private static IEnumerable<TResult> SelectIgnoreNullIterator<TSource, TResult>(IEnumerable<TSource> source, Func<TSource, TResult> selector)
		{
			foreach (var element in source) {
				var result = selector(element);

				if (result != null) {
					yield return result;
				}
			}
		}
	}
}
