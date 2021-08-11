using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DownloadShared
{
	public class PackFile
	{
		public List<PackEntry> Entries { get; set; } = new List<PackEntry>();
		public bool IsLocked { get; set; }
		public long TotalBytes { get; set; }
	}

	public class PackEntry
	{
		public string FileName { get; set; }
		public int Version { get; set; }
	}
}
