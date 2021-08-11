using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace StreamHelper.Models
{
	public static class BytesToReadableExtension
	{
		public static string BytesToReadable (this long bytes)
		{
			if (bytes < 1 << 10)
			{
				return $"{bytes} B";
			}
			else if (bytes < 1 << 20)
			{
				return $"{(double)bytes / (1 << 10):F3} KB";
			}
			else if (bytes < 1 << 30)
			{
				return $"{(double)bytes / (1 << 20):F3} MB";
			}
			else
			{
				return $"{(double)bytes / (1 << 30):F3} GB";
			}
		}

		public static string BytesToReadable (this int bytes)
		{
			return BytesToReadable((long)bytes);
		}
	}
}
