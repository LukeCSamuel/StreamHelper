using DownloadServer.Models;
using DownloadServer.Services;
using DownloadShared;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace DownloadServer.Controllers
{
	[ApiController]
	[Route("[controller]")]
	public class VersionController : ControllerBase
	{
		IBlobService Blob { get; }
		JsonSerializerOptions SerializerOptions { get; }


		public VersionController (IBlobService blob)
		{
			Blob = blob;
			SerializerOptions = new JsonSerializerOptions()
			{
				PropertyNameCaseInsensitive = true
			};
		}

		[HttpGet("latest")]
		public async Task<IActionResult> CurrentVersionAsync ()
		{
			try
			{
				var data = await Blob.ReadSingleFileAsync("dist/version.json");
				var v = JsonSerializer.Deserialize<VersionFile>(data, SerializerOptions);
				return Ok(v.Version);
			}
			catch
			{
				return StatusCode(500);
			}
		}

		[HttpGet("list/{**dir}")]
		public async Task<IActionResult> ListFilesAsync (string dir = null)
		{
			try
			{
				var files = new List<BlobFileInfo>();
				await foreach (var blob in Blob.ListFilesAsync(Uri.UnescapeDataString(dir)))
				{
					files.Add(new BlobFileInfo()
					{
						Name = blob.Name,
						Size = blob.Properties.ContentLength
					});
				}
				return Ok(files);
			}
			catch
			{
				return StatusCode(500);
			}
		}
	}
}
