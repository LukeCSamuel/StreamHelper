using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace StreamHelper.Models
{
	public class SceneView
	{
		public string SceneName { get; set; }
		public string ViewName { get; set; }

		public bool HasView => ViewName is not null;
	}
}
