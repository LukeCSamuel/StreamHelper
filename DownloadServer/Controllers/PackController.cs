using DownloadServer.Services;
using DownloadShared;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DownloadServer.Controllers
{
	[Route("[controller]")]
	[ApiController]
	public class PackController : ControllerBase
	{
		IBlobService Blob { get; }

		public PackController (IBlobService blob)
		{
			Blob = blob;
		}

		[HttpPost("{**location}")]
		public async Task<IActionResult> PackUpdate (string location, [FromBody] PackFile existingPack = null)
		{
			try
			{
				if (existingPack is null)
				{
					existingPack = new PackFile();
				}
				var remotePack = await Blob.GetPack(location);

				var result = new PackDiff();
				if (remotePack.IsLocked)
				{
					return Ok(result);
				}

				foreach (var existing in existingPack.Entries)
				{
					// If it's not in the remote pack, it should be removed
					if (!remotePack.Entries.Any(e => e.FileName == existing.FileName))
					{
						result.Delete.Add(existing.FileName);
					}
				}

				foreach (var remote in remotePack.Entries)
				{
					// If a recent version of it isn't in the existing pack, it should be updated
					if (!existingPack.Entries.Any(e => e.FileName == remote.FileName && e.Version >= remote.Version))
					{
						result.Update.Add(location + "/" + remote.FileName);
					}
				}
				result.Update.Add($"{location}/.pack");
				result.Relative = location;
				result.TotalBytes = await Blob.CalculateSize(result.Update);

				return Ok(result);
			}
			catch
			{
				return StatusCode(500);
			}
		}

		[HttpGet("list")]
		public async Task<IActionResult> ListPacks ()
		{
			try
			{
				var result = new List<PackListItem>();
				await foreach (var packPath in Blob.ListPackPathsASync())
				{
					var packMeta = await Blob.GetPackMetaAsync(packPath);
					result.Add(new PackListItem()
					{
						Path = packPath,
						Meta = packMeta
					});
				}
				return Ok(result);
			}
			catch
			{
				return StatusCode(500);
			}
		}
	}
}
