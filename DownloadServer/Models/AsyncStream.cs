using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace DownloadServer.Models
{
	public class AsyncStream : Stream
	{
		public override bool CanRead => false;
		public override bool CanSeek => false;
		public override bool CanWrite => true;
		public override long Length => Packets.Sum(packet => packet.size);
		public override long Position {
			get => Length;
			set => throw new NotSupportedException();
		}
		
		public int PacketSize { get; }
		Queue<(byte[] bytes, int size)> Packets { get; }
		bool IsClosed { get; set; }

		public AsyncStream ()
		{
			Packets = new Queue<(byte[], int)>();
		}

		public async Task<bool> HasNextAsync ()
		{
			while (!IsClosed && !(Packets.Count > 0))
			{
				await Task.Delay(10);
			}

			if (Packets.Count > 0)
			{
				return true;
			}
			else if (IsClosed)
			{
				return false;
			}
			else
			{
				// This should never happen
				return false;
			}
		}

		public (byte[], int) ReadNext ()
		{
			return Packets.Count > 0 ? Packets.Dequeue() : throw new InvalidOperationException();
		}

		public override void Flush () { }

		public override int Read (byte[] buffer, int offset, int count)
		{
			throw new NotSupportedException();
		}

		public override long Seek (long offset, SeekOrigin origin)
		{
			throw new NotSupportedException();
		}

		public override void SetLength (long value)
		{
			throw new NotSupportedException();
		}

		public override void Write (byte[] buffer, int offset, int count)
		{
			byte[] data = new byte[count];
			Array.Copy(buffer, offset, data, 0, count);
			Packets.Enqueue((data, data.Length));
		}

		public override void Close ()
		{
			IsClosed = true;
			base.Close();
		}
	}
}
