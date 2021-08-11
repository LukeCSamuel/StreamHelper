using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DownloadShared
{
	public class PackDiff
	{
		public long TotalBytes { get; set; }
		public string Relative { get; set; }
		public List<string> Delete { get; set; } = new List<string>();
		public List<string> Update { get; set; } = new List<string>();
	}
}
