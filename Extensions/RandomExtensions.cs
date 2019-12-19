using System;
using System.Collections.Generic;
using System.Text;

namespace MopBotTwo.Extensions
{
	public static class RandomExtensions
	{
		private static readonly byte[] RandomBuffer16 = new byte[sizeof(short)];
		private static readonly byte[] RandomBuffer64 = new byte[sizeof(long)];

		public static short NextShort(this Random random)
		{
			random.NextBytes(RandomBuffer16);

			return BitConverter.ToInt16(RandomBuffer16,0);
		}
		public static ushort NextUShort(this Random random)
		{
			random.NextBytes(RandomBuffer16);

			return BitConverter.ToUInt16(RandomBuffer16,0);
		}

		public static long NextLong(this Random random)
		{
			random.NextBytes(RandomBuffer64);

			return BitConverter.ToInt64(RandomBuffer64,0);
		}
		public static ulong NextULong(this Random random)
		{
			random.NextBytes(RandomBuffer64);

			return BitConverter.ToUInt64(RandomBuffer64,0);
		}
	}
}
