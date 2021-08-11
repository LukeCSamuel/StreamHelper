using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using DownloadShared;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Packer
{
	class Program
	{
		const string connectionString = @"DefaultEndpointsProtocol=https;AccountName=lsamuel;AccountKey=FJ7qIV6/aQZqRfNGW2zBxAllOujkzOxLO8g7y8fi3efGHvEW27pkMH0iHhOKRFpDRt8Y0h9K2TzAxGbdec9hqw==;EndpointSuffix=core.windows.net";
		const string container = "stream-helper";

		static JsonSerializerOptions JsonOpts { get; } = new JsonSerializerOptions()
		{
			PropertyNameCaseInsensitive = true
		};

		static DirectoryInfo PackDir { get; set; }
		static List<string> Excludes { get; set; }

		static BlobContainerClient Client { get; set; }
		static string RemotePath { get; set; }
		static PackFile RemotePack { get; set; }
		static PackFile NewPack { get; set; }

		static void Main (string[] args)
		{
			var defaultColor = Console.ForegroundColor;

			// Get locals
			Console.ForegroundColor = ConsoleColor.Magenta;
			Console.WriteLine($"Welcome to Packer!{Environment.NewLine}{Environment.NewLine}");
			Console.ForegroundColor = defaultColor;
			Console.WriteLine("Please enter the directory to be packed:");
			var line = Console.ReadLine();
			PackDir = new DirectoryInfo(Path.Combine(Environment.CurrentDirectory, line));
			Console.WriteLine($"{Environment.NewLine}Please enter sub-directories which should be excluded from the pack.  When finished, enter a blank line.");
			Excludes = new List<string>();
			while (true)
			{
				line = Console.ReadLine();
				if (!string.IsNullOrEmpty(line))
				{
					Excludes.Add(Path.Combine(PackDir.FullName, line));
				}
				else
				{
					break;
				}
			}

			// Confirm locals
			Console.WriteLine($"{Environment.NewLine}Confirm that you wish to pack the following directory:{Environment.NewLine}");
			Console.ForegroundColor = ConsoleColor.Green;
			Console.WriteLine(PackDir.FullName);
			Console.ForegroundColor = defaultColor;
			Console.WriteLine($"{Environment.NewLine}And you wish to exclude the following directories from the pack:{Environment.NewLine}");
			Console.ForegroundColor = ConsoleColor.Red;
			foreach (var dir in Excludes)
			{
				Console.WriteLine(dir);
			}
			Console.ForegroundColor = defaultColor;

			Console.ForegroundColor = ConsoleColor.Gray;
			Console.WriteLine($"{Environment.NewLine}(press ENTER to continue)");
			if (Console.ReadKey().Key != ConsoleKey.Enter)
			{
				Console.WriteLine("Cancelling pack.");
				return;
			}
			Console.ForegroundColor = defaultColor;
			Console.Clear();

			// Get remote
			Console.WriteLine("Enter the path to the pack on the remote container:");
			RemotePath = Console.ReadLine().Trim();
			while (RemotePath.EndsWith("/"))
			{
				RemotePath = RemotePath[0..^1];
			}
			Console.WriteLine($"{Environment.NewLine}Confirm you would like to store this pack at the remote:");
			Console.ForegroundColor = ConsoleColor.Magenta;
			Console.WriteLine(RemotePath);
			Console.ForegroundColor = ConsoleColor.Gray;
			Console.WriteLine($"{Environment.NewLine}(press ENTER to continue)");
			if (Console.ReadKey().Key != ConsoleKey.Enter)
			{
				Console.WriteLine("Cancelling pack.");
				return;
			}
			Console.ForegroundColor = defaultColor;
			Console.Clear();

			PackAsync().GetAwaiter().GetResult();

			Console.WriteLine($"{Environment.NewLine}{Environment.NewLine}Done.");
		}

		public static async Task PackAsync ()
		{
			var serviceClient = new BlobServiceClient(connectionString);
			Client = serviceClient.GetBlobContainerClient(container);
			MemoryStream stream;

			// Start by getting pack file for the remote, if there is one
			Console.WriteLine("Fetching remote pack.");
			var remote = Client.GetBlobClient(RemotePath + "/.pack");
			if (await remote.ExistsAsync())
			{
				// Download the pack file
				stream = new MemoryStream();
				await remote.DownloadToAsync(stream);
				stream.Position = 0;
				RemotePack = await JsonSerializer.DeserializeAsync<PackFile>(stream, options: JsonOpts);
				stream.Dispose();

				// Lock the pack file
				RemotePack.IsLocked = true;
				stream = new MemoryStream();
				await JsonSerializer.SerializeAsync(stream, RemotePack, options: JsonOpts);
				stream.Position = 0;
				await remote.DeleteAsync();
				await remote.UploadAsync(stream);
				stream.Dispose();
			}
			else
			{
				RemotePack = new PackFile();
			}

			// We'll create this local pack as we go
			NewPack = new PackFile()
			{
				IsLocked = false,
				TotalBytes = 0
			};
			await RecUploadDirsAsync(PackDir);
			await DeleteRemovedFiles();

			// Upload the new pack
			Console.WriteLine("Uploading new .pack");
			var newPackBlob = Client.GetBlobClient(RemotePath + "/.pack");
			stream = new MemoryStream();
			await JsonSerializer.SerializeAsync(stream, NewPack, options: JsonOpts);
			stream.Position = 0;
			await newPackBlob.DeleteIfExistsAsync();
			await newPackBlob.UploadAsync(stream);
			stream.Dispose();
		}

		public static async Task RecUploadDirsAsync (DirectoryInfo directory)
		{
			foreach (var file in directory.EnumerateFiles())
			{
				var relativeName = file.FullName.Replace(PackDir.FullName, "");
				relativeName = relativeName.Replace("\\", "/");
				while (relativeName.StartsWith("/"))
				{
					relativeName = relativeName[1..^0];
				}
				var remote = RemotePack.Entries.SingleOrDefault(e => e.FileName == relativeName);
				var blob = Client.GetBlobClient(RemotePath + "/" + relativeName);
				int version = 1;
				if (remote is PackEntry)
				{
					if (await blob.ExistsAsync())
					{
						var oldBlobProps = await blob.GetPropertiesAsync();
						if (oldBlobProps.Value.LastModified.UtcDateTime > file.LastWriteTimeUtc)
						{
							// File hasn't been changed, we can keep it
							NewPack.Entries.Add(remote);
							continue;
						}
						else
						{
							await blob.DeleteAsync();
							version = remote.Version + 1;
						}
					}
				}
				else
				{
					await blob.DeleteIfExistsAsync();
				}

				var newEntry = new PackEntry()
				{
					Version = version,
					FileName = relativeName
				};
				NewPack.Entries.Add(newEntry);
				NewPack.TotalBytes += file.Length;
				Console.WriteLine($"Uploading {relativeName}");
				await blob.UploadAsync(file.OpenRead());
			}

			// Recursively upload subdirectories that are not excluded
			foreach (var dir in directory.EnumerateDirectories())
			{
				if (!Excludes.Contains(dir.FullName))
				{
					await RecUploadDirsAsync(dir);
				}
			}
		}

		public static async Task DeleteRemovedFiles ()
		{
			foreach (var entry in RemotePack.Entries)
			{
				if (!NewPack.Entries.Any(f => f.FileName == entry.FileName))
				{
					Console.WriteLine($"Deleting {entry.FileName}");
					var blob = Client.GetBlobClient(RemotePath + "/" + entry.FileName);
					await blob.DeleteIfExistsAsync();
				}
			}
		}
	}
}
