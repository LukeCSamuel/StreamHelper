using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using StreamHelper.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace StreamHelper
{
	[Route("[controller]")]
	[ApiController]
	public class Package : ControllerBase
	{
		FileExtensionContentTypeProvider TypeProvider { get; } = new();
		DirectoryInfo PackDirectory => Config.Settings.SelectedPack is null ? null : new(Config.Settings.SelectedPack.Path);
		ISettings Config { get; }

		public Package (ISettings config)
		{
			Config = config;
		}

		[HttpGet("style")]
		public IActionResult GetStyles () => GetFile(new Regex(@"styles.*\.css$"), "text/css");

		[HttpGet("runtime")]
		public IActionResult GetRuntime () => GetFile(new Regex(@"runtime.*\.js$"), "application/javascript");

		[HttpGet("polyfills")]
		public IActionResult GetPolyfills () => GetFile(new Regex(@"polyfills.*\.js$"), "application/javascript");

		[HttpGet("main")]
		public IActionResult GetMain () => GetFile(new Regex(@"main.*\.js$"), "application/javascript");

		[HttpGet("f/{**location}")]
		public IActionResult GetFile (string location)
		{
			if (Config.Settings.SelectedPack?.Meta?.Serve?.Any(f => location.StartsWith(f)) ?? false)
			{
				var fileName = Path.Combine(PackDirectory.FullName, location);
				if (System.IO.File.Exists(fileName))
				{
					var fs = new FileInfo(fileName).OpenRead();
					if (TypeProvider.TryGetContentType(fileName, out string contentType))
					{
						return new FileStreamResult(fs, contentType);
					}
					else
					{
						return new FileStreamResult(fs, "application/octet-stream");
					}
				}
			}
			
			return NotFound();
		}

		FileStreamResult GetFile (Regex query, string type)
		{
			if (PackDirectory is null)
			{
				return new FileStreamResult(new MemoryStream(), type);
			}
			else
			{
				var file = PackDirectory.EnumerateFiles().First(file => query.IsMatch(file.Name));
				return new FileStreamResult(file.OpenRead(), type);
			}
		}
	}
}
