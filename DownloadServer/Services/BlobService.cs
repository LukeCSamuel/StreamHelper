using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using DownloadServer.Models;
using DownloadShared;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace DownloadServer.Services
{
	public interface IBlobService
	{
		IAsyncEnumerable<BlobItem> ListFilesAsync (string name = null);
		IAsyncEnumerable<DownloadPacket> DownloadFileAsync (string filename, int packetSize, CancellationToken cancellationToken);
		Task<string> ReadSingleFileAsync (string filename);
		Task<PackFile> GetPack (string path = "");
		Task<long> CalculateSize (IEnumerable<string> files);
		IAsyncEnumerable<string> ListPackPathsASync ();
		Task<PackMeta> GetPackMetaAsync (string path);
	}

	public class BlobService : IBlobService
	{
		BlobContainerClient Client { get; }

		public BlobService (IConfiguration configs)
		{
			string connectionString = configs["BlobStorage:ConnectionString"];
			var serviceClient = new BlobServiceClient(connectionString);
			Client = serviceClient.GetBlobContainerClient(configs["BlobStorage:Container"]);
		}

		public async IAsyncEnumerable<DownloadPacket> DownloadFileAsync (string filename, int packetSize, [EnumeratorCancellation] CancellationToken cancellationToken = default)
		{
			long allocated = 0;
			var blob = Client.GetBlobClient(filename);
			if (await blob.ExistsAsync())
			{
				var stream = new AsyncStream();
				var _ = Task.Run(async () =>
				{
					var download = await blob.DownloadToAsync(
						stream,
						transferOptions: new Azure.Storage.StorageTransferOptions()
						{
							MaximumTransferSize = packetSize
						},
						cancellationToken: cancellationToken);
					await stream.DisposeAsync();
				}, cancellationToken);
				while (await stream.HasNextAsync())
				{
					var (bytes, size) = stream.ReadNext();
					allocated += size;

					// Compress bytes using deflate
					//var output = new MemoryStream();
					//var dstream = new DeflateStream(output, CompressionLevel.Optimal);
					//dstream.Write(bytes, 0, bytes.Length);

					yield return new DownloadPacket()
					{
						//Bytes = output.ToArray(),
						Bytes = bytes,
						Size = size,
						Filename = filename
					};

					//dstream.Dispose();
					//output.Dispose();

					if (allocated > 1 << 24)
					{
						GC.Collect();
						allocated = 0;
						GC.WaitForPendingFinalizers();
					}
				}

				GC.Collect();
				GC.WaitForPendingFinalizers();
			}
		}

		public async Task<string> ReadSingleFileAsync (string filename)
		{
			var blob = Client.GetBlobClient(filename);
			if (await blob.ExistsAsync())
			{
				var data = new MemoryStream();
				await blob.DownloadToAsync(data);
				return Encoding.UTF8.GetString(data.ToArray());
			}
			else
			{
				throw new KeyNotFoundException();
			}
		}

		public IAsyncEnumerable<BlobItem> ListFilesAsync (string dir = null)
		{
			return Client.GetBlobsAsync(prefix: dir);
		}

		public async Task<long> CalculateSize (IEnumerable<string> files)
		{
			long result = 0;
			foreach (var file in files)
			{
				var blob = Client.GetBlobClient(file);
				if (await blob.ExistsAsync())
				{
					var props = await blob.GetPropertiesAsync();
					result += props.Value.ContentLength;
				}
			}
			return result;
		}

		public async Task<PackFile> GetPack (string path = "")
		{
			var blob = Client.GetBlobClient(path + "/.pack");
			if (await blob.ExistsAsync())
			{
				var stream = new MemoryStream();
				await blob.DownloadToAsync(stream);
				stream.Position = 0;
				return await JsonSerializer.DeserializeAsync<PackFile>(stream, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
			}
			else
			{
				throw new KeyNotFoundException();
			}
		}

		public async IAsyncEnumerable<string> ListPackPathsASync ()
		{
			await foreach (var blobHier in Client.GetBlobsByHierarchyAsync(delimiter: "/", prefix: "packs/"))
			{
				if (blobHier.IsPrefix)
				{
					// This is the path to the pack
					yield return blobHier.Prefix.TrimEnd('/');
				}
			}
		}

		public async Task<PackMeta> GetPackMetaAsync (string path)
		{
			var blob = Client.GetBlobClient(path + "/pack.meta");
			if (await blob.ExistsAsync())
			{
				var stream = new MemoryStream();
				await blob.DownloadToAsync(stream);
				stream.Position = 0;
				return await JsonSerializer.DeserializeAsync<PackMeta>(stream, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
			}
			else
			{
				throw new KeyNotFoundException();
			}

		}
	}

	public static class BlobServiceProvider
	{
		public static IServiceCollection AddBlobService (this IServiceCollection services)
		{
			return services.AddSingleton<IBlobService, BlobService>();
		}
	}
}
