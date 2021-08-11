using DownloadController;
using DownloadShared;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace AutoUpdate
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{

		const string DownloadHost = "https://streamhelper.azurewebsites.net";
		//const string DownloadHost = "https://localhost:5001";

		const string ObsWebsocketLocation = "res/obs-websocket-4.9.0-Windows.zip";

		public MainWindow ()
		{
			InitializeComponent();
		}

		public void WindowMouseDown (object sender, MouseButtonEventArgs e)
		{
			if (e.ChangedButton == MouseButton.Left)
			{
				DragMove();
			}
		}

		public async void OnLoaded (object sender, RoutedEventArgs e)
		{
			// Get pack diff
			//Load existing pack for the main application from '..'
			using var client = new HttpClient();
			client.BaseAddress = new Uri(DownloadHost);

			Stream reqStream;
			if (File.Exists(Path.Combine(Program.TargetDir, ".pack")))
			{
				reqStream = new FileStream(Path.Combine(Program.TargetDir, ".pack"), FileMode.OpenOrCreate);
			}
			else
			{
				reqStream = new MemoryStream();
				await JsonSerializer.SerializeAsync(reqStream, new PackFile(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
				reqStream.Position = 0;
			}

			var content = new StreamContent(reqStream);
			content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
			var response = await client.PostAsync("Pack/dist", content);

			reqStream.Dispose();
			if (response.IsSuccessStatusCode)
			{
				try
				{
					var diff = await JsonSerializer.DeserializeAsync<PackDiff>(
						await response.Content.ReadAsStreamAsync(),
						new JsonSerializerOptions() { PropertyNameCaseInsensitive = true }
					);
					// Handle files
					var downloadService = new DownloadService(DownloadHost);
					var packService = new PackService(downloadService);

					long sum = 0;
					var progress = new Progress<int>(val =>
					{
						sum += val;
						_progressBar.Value = 7 + 93.0 * sum / diff.TotalBytes;
						_lblStatus.Content = $"{(int)_progressBar.Value}% - {sum.BytesToReadable()} / {diff.TotalBytes.BytesToReadable()}";
					});
					await packService.ResolveDiffAsync(diff, new DirectoryInfo(Program.TargetDir), progress);
					_progressBar.Value = 100;
				}
				catch (Exception ex)
				{
					MessageBox.Show("An error occurred during the download.\r\n" + ex.Message, "Stream Helper Installer", MessageBoxButton.OK, MessageBoxImage.Error);
					Application.Current.Shutdown();
				}
			}
			else
			{
				MessageBox.Show("Could not calculate diff, quitting.", "Stream Helper Installer", MessageBoxButton.OK, MessageBoxImage.Error);
				Application.Current.Shutdown();
			}

			_lblStatus.Content = "Installing files...";

			// Custom install steps

			string obsPath = null;
			try
			{
				using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\OBS Studio");
				obsPath = key?.GetValue("") as string;
			}
			catch { }

			if (obsPath is string && File.Exists(Path.Combine(Program.TargetDir, ObsWebsocketLocation)))
			{
				try
				{
					ZipFile.ExtractToDirectory(Path.Combine(Program.TargetDir, ObsWebsocketLocation), obsPath, overwriteFiles: true);
				}
				catch (Exception ex)
				{
					MessageBox.Show($"Failed to install OBS plugin obs-websocket.  This plugin may need to be installed manually.{Environment.NewLine}{ex.Message}", "Stream Helper Installer", MessageBoxButton.OK, MessageBoxImage.Warning);
				}
			}
			else
			{
				MessageBox.Show("Failed to determine OBS installation directory.  You may need to install obs-websocket plugin manually.", "Stream Helper Installer", MessageBoxButton.OK, MessageBoxImage.Warning);
			}

			if (!Program.NoStart)
			{
				// Start StreamHelper
				var streamHelper = new Process();
				streamHelper.StartInfo.UseShellExecute = true;
				streamHelper.StartInfo.FileName = Path.Combine(Directory.GetCurrentDirectory(), Path.Combine(Program.TargetDir, @"StreamHelper.exe"));
				streamHelper.StartInfo.WorkingDirectory = Path.Combine(Directory.GetCurrentDirectory(), Program.TargetDir);
				streamHelper.Start();
			}

			// Exit
			Application.Current.Shutdown();
		}
	}

	public static class BytesToReadableExtension
	{
		public static string BytesToReadable (this long bytes)
		{
			if (bytes < 1 << 10)
			{
				return $"{bytes} B";
			}
			else if (bytes < 1 << 20)
			{
				return $"{(double)bytes / (1 << 10):F3} KB";
			}
			else if (bytes < 1 << 30)
			{
				return $"{(double)bytes / (1 << 20):F3} MB";
			}
			else
			{
				return $"{(double)bytes / (1 << 30):F3} GB";
			}
		}

		public static string BytesToReadable (this int bytes)
		{
			return BytesToReadable((long)bytes);
		}
	}
}
