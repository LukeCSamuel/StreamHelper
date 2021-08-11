using DownloadController;
using DownloadShared;
using NuGet.Versioning;
using StreamHelper.Controls;
using StreamHelper.Models;
using StreamHelper.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace StreamHelper
{
	/// <summary>
	/// Interaction logic for Update.xaml
	/// </summary>
	public partial class Update : DechromeWindow
	{
		public Update (SemanticVersion current, SemanticVersion latest)
		{
			InitializeComponent();

			_prompt.Text = $"An update for Stream Helper is available.  Would you like to update now?" +
				$"{Environment.NewLine}Current Version: {current.ToNormalizedString()}" +
				$"{Environment.NewLine}Latest Version: {latest.ToNormalizedString()}";
		}

		public async void UpdateApplication (object sender, RoutedEventArgs e)
		{
			// Set text and hide buttons
			_prompt.Text = $"Preparing to update, please wait...";
			_btnNo.Visibility = Visibility.Hidden;
			_btnYes.Visibility = Visibility.Hidden;
			_downloadProgress.Visibility = Visibility.Visible;

			// Download the latest auto-update package
			var client = (HttpClient)Program.ServiceProvider.GetService(typeof(DownloadHost));
			var updateFilesStream = await client.GetStreamAsync("Version/list/dist/auto-update");

			var updateFiles = await JsonSerializer.DeserializeAsync<List<BlobFileInfo>>(updateFilesStream, options: new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });

			var downloadService = (IDownloadService)Program.ServiceProvider.GetService(typeof(IDownloadService));
			await downloadService.ConnectAsync();

			const string updaterDl = @"auto-update/cache.zip";
			if (File.Exists(updaterDl))
			{
				File.Delete(updaterDl);
			}

			long totalSize = updateFiles.Sum(file => file.Size ?? 0);
			long totalDownloaded = 0;

			BackgroundWorker updater = new BackgroundWorker();
			updater.ProgressChanged += (_, change) =>
			{
				_prompt.Text = $"Preparing... {totalDownloaded.BytesToReadable()} / {totalSize.BytesToReadable()}";
				_downloadProgress.Value = change.ProgressPercentage;
			};
			updater.DoWork += (sender, _) =>
			{
				var worker = sender as BackgroundWorker;
				var progresser = new Progress<int>(value =>
				{
					totalDownloaded += value;
					worker.ReportProgress((int)(93.0 * totalDownloaded / totalSize + 7), value);
				});

				Directory.CreateDirectory(@"auto-update");
				// Background Worker doesn't respect await
				downloadService.DownloadFilesAsync(
					updateFiles.Select(file => file.Name).ToList(),
					updaterDl,
					relative: "dist/",
					progress: progresser
				).GetAwaiter().GetResult();
			};
			updater.RunWorkerCompleted += (_, _) =>
			{
				if (File.Exists(updaterDl))
				{
					downloadService.DisconnectAsync();
					ZipFile.ExtractToDirectory(updaterDl, @"./", overwriteFiles: true);
					File.Delete(updaterDl);
					var autoUpdate = new Process();
					autoUpdate.StartInfo.UseShellExecute = true;
					autoUpdate.StartInfo.FileName = Path.Combine(Directory.GetCurrentDirectory(), @"\auto-update\AutoUpdate.exe");
					autoUpdate.StartInfo.WorkingDirectory = Path.Combine(Directory.GetCurrentDirectory(), @"\auto-update");
					autoUpdate.StartInfo.Verb = "runas";
					autoUpdate.Start();
					Application.Current.Shutdown();
				}
				else
				{
					MessageBox.Show("Failed to download update.");
					Close();
				}
			};
			updater.WorkerReportsProgress = true;
			updater.RunWorkerAsync();
		}

		public void DeclineUpdate (object sender, RoutedEventArgs e)
		{
			Close();
		}
	}
}
