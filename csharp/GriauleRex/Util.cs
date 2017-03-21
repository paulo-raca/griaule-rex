using System;
using System.Linq;
using System.Collections.Generic;

namespace GriauleRex
{
	public static class Util
	{
		public static byte[] parseHex(string hex) {
			return Enumerable.Range(0, hex.Length)
				.Where(x => x % 2 == 0)
				.Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
				.ToArray();
		}
	}

	public class DefaultDictionary<TKey, TValue> : Dictionary<TKey, TValue> 
	{
		public new TValue this[TKey key]
		{
			get
			{
				TValue val;
				if (!TryGetValue(key, out val))
				{
					val = default(TValue);
					Add(key, val);
				}
				return val;
			}

			set 
			{ 
				base[key] = value; 
			}
		}
	}
}

