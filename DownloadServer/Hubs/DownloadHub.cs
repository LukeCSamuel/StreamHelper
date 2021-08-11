using DownloadServer.Services;
using DownloadShared;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace DownloadServer.Hubs
{
	public class DownloadHub : Hub
	{
		IBlobService Blobs { get; }

		public DownloadHub (IBlobService blobService)
		{
			Blobs = blobService;
		}

		public async IAsyncEnumerable<DownloadPacket> DownloadFiles (
			IEnumerable<string> files,
			int packetSize,
			[EnumeratorCancellation] CancellationToken cancellationToken)
		{
			foreach (var file in files)
			{
				await foreach (var packet in Blobs.DownloadFileAsync(file, packetSize, cancellationToken))
				{
					yield return packet;
				}
			}
		}
	}
}
