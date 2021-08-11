using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DownloadShared
{
	public class PackMeta
	{
		public string Name { get; set; }
		public string Version { get; set; }
		public string Url { get; set; }
		public List<string> Serve { get; set; }
		public string Install { get; set; }
		public List<string> Views { get; set; }
		public Dictionary<string, string> SceneMap { get; set; }
	}
}
