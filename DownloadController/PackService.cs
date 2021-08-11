using DownloadShared;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DownloadController
{
	public interface IPackService
	{
		Task ResolveDiffAsync (PackDiff diff, DirectoryInfo packDir, IProgress<int> progress = null);
	}

	public class PackService : IPackService
	{
		IDownloadService Downloader { get; }

		public PackService (IDownloadService downloader)
		{
			Downloader = downloader;
		}

		public async Task ResolveDiffAsync (PackDiff diff, DirectoryInfo packDir, IProgress<int> progress = null)
		{
			// First delete any files to be deleted
			foreach (var file in diff.Delete)
			{
				var path = Path.Combine(packDir.FullName, file);
				if (File.Exists(path))
				{
					File.Delete(path);
				}
			}

			// Next, download all the new files
			var dlPath = Path.Combine(packDir.FullName, "dl.zip");
			if (File.Exists(dlPath))
			{
				File.Delete(dlPath);
			}
			await Downloader.ConnectAsync();
			var zip = await Downloader.DownloadFilesAsync(
				diff.Update,
				dlPath,
				relative: diff.Relative,
				progress: progress
			);
			ZipFile.ExtractToDirectory(zip.FullName, packDir.FullName, overwriteFiles: true);
			File.Delete(zip.FullName);
			await Downloader.DisconnectAsync();
		}
	}

	public static class PackServiceProvider
	{
		public static IServiceCollection AddPackService (this IServiceCollection services)
		{
			return services.AddSingleton<IPackService, PackService>();
		}
	}
}
