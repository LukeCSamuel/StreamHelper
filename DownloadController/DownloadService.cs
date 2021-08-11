using DownloadShared;
using ICSharpCode.SharpZipLib.Zip;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DownloadController
{
	
	public interface IDownloadService
	{
		Task ConnectAsync ();
		Task DisconnectAsync ();
		Task<FileInfo> DownloadFilesAsync (IEnumerable<string> files, string destination, string relative = null, IProgress<int> progress = null);
	}

	public class DownloadService : IDownloadService, IDisposable
	{
		HubConnection HubConnection { get; }

		public DownloadService (string host)
		{
			HubConnection = new HubConnectionBuilder()
				.WithUrl($"{host}/download")
				.AddMessagePackProtocol()
				.Build();
		}

		public Task ConnectAsync ()
		{
			return HubConnection.StartAsync();
		}

		public Task DisconnectAsync ()
		{
			return HubConnection.StopAsync();
		}

		public async Task<FileInfo> DownloadFilesAsync (IEnumerable<string> files, string destination, string relative = null, IProgress<int> progress = null)
		{
			if (File.Exists(destination))
			{
				throw new IOException($"The file {destination} already exists. Canceling download.");
			}
			using var fs = File.Create(destination);
			using var outStream = new ZipOutputStream(fs);
			
			var cancellationTokenSource = new CancellationTokenSource();
			var stream = HubConnection.StreamAsync<DownloadPacket>("DownloadFiles", files, 1 << 12, cancellationTokenSource.Token);
			string current = null;
			await foreach (var packet in stream)
			{
				if (packet.Filename != current)
				{
					current = packet.Filename;
					outStream.PutNextEntry(new ZipEntry(current.Replace(relative ?? "", "")));
				}

				//var data = new MemoryStream(packet.Bytes);
				//using (var dstream = new DeflateStream(data, CompressionMode.Decompress))
				//{
				//	await dstream.CopyToAsync(outStream);
				//}
				await outStream.WriteAsync(packet.Bytes);

				if (progress is not null)
				{
					progress.Report(packet.Size);
				}
			}

			return new FileInfo(destination);
		}

		public void Dispose ()
		{
			DisconnectAsync().GetAwaiter().GetResult();
		}
	}

	public static class DownloadServiceProvider
	{
		public static IServiceCollection AddDownloadService (this IServiceCollection services, string host)
		{
			return services.AddSingleton<IDownloadService>(new DownloadService(host));
		}
	}
}
