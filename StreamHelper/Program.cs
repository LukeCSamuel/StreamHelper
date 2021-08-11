using CefSharp;
using CefSharp.Wpf;
using DownloadController;
using DupGuard;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NuGet.Versioning;
using StreamHelper.Server;
using StreamHelper.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace StreamHelper
{
	class Program
	{
		const string DownloadHost = "https://streamhelper.azurewebsites.net";
		//const string DownloadHost = "https://localhost:5001";
		public static IServiceProvider ServiceProvider { get; private set; }

		[STAThread]
		public static void Main (string[] args)
		{
			// Determine if the application is already running
			if (DuplicationGuard.IsAlreadyRunning())
			{
				MessageBox.Show("An instance of the Stream Helper is already running.", "Stream Helper",
					MessageBoxButton.OK, MessageBoxImage.Error);
				return;
			}

			// Load application settings
			var config = new SettingsManager();
			bool settingsSuccess = config.LoadAsync().GetAwaiter().GetResult();
			if (!settingsSuccess)
			{
				var promptResult = MessageBox.Show("Failed to load settings file. Would you like to reset to defaults?", "Error", MessageBoxButton.YesNoCancel, MessageBoxImage.Warning);
				if (promptResult == MessageBoxResult.Yes)
				{
					config.SaveAsync().GetAwaiter().GetResult();
				}
				else if (promptResult == MessageBoxResult.Cancel)
				{
					return;
				}
			}

			// Initialize Cef for chromium embed
			var settings = new CefSettings();
			settings.CefCommandLineArgs.Add("disable-gpu");
			settings.CefCommandLineArgs.Add("disable-gpu-vsync");
			settings.CachePath = Directory.GetCurrentDirectory() + "/cef_cache";

			Try(() => Cef.Initialize(settings), $"CEF could not be initialized.  Try reinstalling, or contacting support for assistance.");

			var tokenSource = new CancellationTokenSource();
			try
			{
				// Create server and capture service provider
				var host = CreateHostBuilder(args, config).Build();
				ServiceProvider = host.Services;
				host.RunAsync(tokenSource.Token);

				// Create app and run
				var app = new App();
				app.InitializeComponent();
				app.Run();

				tokenSource.Cancel();
			}
			finally
			{
				tokenSource.Dispose();
			}
		}

		public static IHostBuilder CreateHostBuilder (string[] args, ISettings settings) =>
			Host.CreateDefaultBuilder(args)
				.ConfigureServices(services => 
					services
					.AddSingleton(settings)
					.AddDownloadService(DownloadHost)
					.AddDownloadHost(DownloadHost)
					.AddPackService()
				)
				.ConfigureWebHostDefaults(webBuilder =>
				{
					webBuilder.UseStartup<Startup>();
					webBuilder.UseUrls(settings.Settings.OverlayHost);
				});

		public static T Try<T> (Func<T> func, string message = null)
		{
#if ASPNETCORE_ENVIRONMENT != Development
			message ??= "An unrecoverable error occurred.  No description was provided.";
			try
			{
#endif
				return func();
#if ASPNETCORE_ENVIRONMENT != Development
			}
			catch (Exception)
			{
				MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
				return default;
			}
#endif
		}

		public static void Try (Action func, string message = null)
		{
#if ASPNETCORE_ENVIRONMENT != Development
			message ??= "An unrecoverable error occurred.  No description was provided.";
			try
			{
#endif
				func();
#if ASPNETCORE_ENVIRONMENT != Development
			}
			catch (Exception)
			{
				MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
				return;
			}
#endif
		}
	}
}
