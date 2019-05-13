using Discord;
using Discord.Net;
using Discord.Rest;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Text;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection;
using System.Threading.Tasks;
using DColor = Discord.Color;
using MopBotTwo.Systems;

namespace MopBotTwo.Extensions
{
	public static class LinqExtensions
	{
		public static IEnumerable<T> ExceptIndex<T>(this IEnumerable<T> source,int index)
		{
			int i = 0;
			foreach(var value in source) {
				if(i++!=index) {
					yield return value;
				}
			}
		}
		
		public static int FirstIndex<T>(this IEnumerable<T> source,Func<T,bool> predicate)
		{
			int index = 0;
			foreach(var item in source) {
				if(predicate(item)) {
					return index;
				}
				index++;
			}
			return -1;
		}

		public static bool TryGetFirst<T>(this IEnumerable<T> source,out T result) => (result = source.FirstOrDefault())!=default;
		public static bool TryGetFirst<T>(this IEnumerable<T> source,Func<T,bool> predicate,out T result) => (result = source.FirstOrDefault(predicate))!=default;

		public static IEnumerable<TResult> SelectIgnoreNull<TSource,TResult>(this IEnumerable<TSource> source,Func<TSource,TResult> selector)
		{
			if(source==null) {
				throw new ArgumentNullException(nameof(source));
			}
			if(selector==null) {
				throw new ArgumentNullException(nameof(selector));
			}
			return SelectIgnoreNullIterator(source,selector);
		}
		private static IEnumerable<TResult> SelectIgnoreNullIterator<TSource,TResult>(IEnumerable<TSource> source,Func<TSource,TResult> selector)
		{
			foreach(var element in source) {
				var result = selector(element);
				if(result!=null) {
					yield return result;
				}
			}
		}
	}
}