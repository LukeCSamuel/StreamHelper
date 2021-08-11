using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DownloadShared
{
	public class PackListItem
	{
		public string Path { get; set; }
		public PackMeta Meta { get; set; }
		public bool? UpdateAvailable { get; set; }
	}
}
