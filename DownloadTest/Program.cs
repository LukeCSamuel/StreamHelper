using DownloadController;
using System;
using System.Threading.Tasks;

namespace DownloadTest
{
	class Program
	{
		static async Task Main (string[] args)
		{
			var service = new DownloadService("https://localhost:5001");
			await service.ConnectAsync();
			var progresser = new Progresser();
			progresser.OnReport += (_, val) =>
			{
				Console.WriteLine($"{val} bytes downloaded");
			};
			await service.DownloadFilesAsync(new string[] { "atextfile.txt", "subfolder/chorme.txt" }, "dl/cache.zip", progresser);
			await service.DisconnectAsync();
		}
	}

	class Progresser : IProgress<int>
	{

		public event EventHandler<int> OnReport;

		public void Report (int value)
		{
			OnReport?.Invoke(this, value);
		}
	}
}
