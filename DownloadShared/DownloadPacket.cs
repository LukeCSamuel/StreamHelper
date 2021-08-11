using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DownloadShared
{
	public class DownloadPacket
	{
		public int Size { get; set; }
		public byte[] Bytes { get; set; }
		public string Filename { get; set; }
	}
}
