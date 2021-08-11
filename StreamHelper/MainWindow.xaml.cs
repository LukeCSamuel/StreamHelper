using DownloadShared;
using Microsoft.AspNetCore.SignalR;
using NuGet.Versioning;
using OBSWebsocketDotNet;
using OBSWebsocketDotNet.Types;
using StreamHelper.Controls;
using StreamHelper.Hubs;
using StreamHelper.Models;
using StreamHelper.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
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
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : DechromeWindow
	{
		IObsConnection Obs { get; }
		ISettings Config { get; }
		IHubContext<ProxyHub> ProxyHub { get; }

		MainWindowViewModel ViewModel { get; set; }

		public MainWindow ()
		{
			InitializeComponent();
			DataContext = ViewModel = new MainWindowViewModel();
			Obs = (IObsConnection)Program.ServiceProvider.GetService(typeof(IObsConnection));
			Config = (ISettings)Program.ServiceProvider.GetService(typeof(ISettings));
			ProxyHub = (IHubContext<ProxyHub>)Program.ServiceProvider.GetService(typeof(IHubContext<ProxyHub>));
		}

		public async void WindowLoaded (object sender, RoutedEventArgs e)
		{
			UpdateHelperAddress();
			Obs.ConnectionChanged += OnObsConnectionChange;
			Obs.SceneCollectionChanged += OnObsSceneCollectionChange;
			Obs.SceneChanged += OnObsSceneChange;

			// Check for updates
			await PromptUpdateIfAvailable();
		}

		private async Task PromptUpdateIfAvailable ()
		{
			var client = (HttpClient)Program.ServiceProvider.GetService(typeof(DownloadHost));
			var latestVersion = await client.GetStringAsync("Version/latest");

			using var versionFile = new FileStream(@"version.json", FileMode.OpenOrCreate);
			var currentVersion = await JsonSerializer.DeserializeAsync<VersionFile>(versionFile, options: new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });

			var semCurrent = SemanticVersion.Parse(currentVersion.Version);
			var semLatest = SemanticVersion.Parse(latestVersion);

			if (semLatest > semCurrent)
			{
				var updateWindow = new Update(semCurrent, semLatest);
				updateWindow.ShowDialog();
			}
		}

		private void ToggleObsConnection (object sender, RoutedEventArgs e)
		{
			if (Obs.IsConnected)
			{
				Obs.Disconnect();
			}
			else
			{
				try
				{
					Obs.Connect();
				}
				catch (Exception)
				{
					MessageBox.Show("Could not connect to OBS.  Make sure OBS is running, OBS Websocket is enabled, and that the connection information in the settings matches that in OBS.",
						"Error", MessageBoxButton.OK, MessageBoxImage.Error);
				}
			}
		}

		private void OnObsConnectionChange (object sender, bool isConnected)
		{
			ViewModel.ObsConnectionStatus = Obs.ConnectionStatus;
		}

		private void OnObsSceneCollectionChange (object sender, IEnumerable<OBSScene> e)
		{
			// TODO: This should be extrapolated into a different method and transform to a model that connects
			//   OBS scenes to overlay views
			try
			{
				ViewModel.ObsScenes.Clear();
				foreach (var scene in e)
				{
					ViewModel.ObsScenes.Add(new SceneView()
					{
						SceneName = scene.Name,
						ViewName = Config.Settings.SelectedPack?.Meta.SceneMap?.GetValueOrDefault(scene.Name)
					});
				}
			}
			// If the disconnect isn't handled by the dispatcher thread, this update will throw
			catch { }
		}

		private async void OnObsSceneChange (object sender, string scene)
		{
			// get the corresponding view from the scene map
			var sceneMap = Config.Settings.SelectedPack?.Meta.SceneMap;
			if (sceneMap is not null && scene is not null && sceneMap.ContainsKey(scene))
			{
				await ProxyHub.Clients.All.SendAsync("setView", sceneMap[scene]);
			}
		}

		private async void PreviewScene (object sender, RoutedEventArgs e)
		{
			var sceneView = ((Button)e.OriginalSource).DataContext as SceneView;
			if (sceneView?.ViewName is not null)
			{
				await ProxyHub.Clients.All.SendAsync("setPreview", sceneView.ViewName);
			}
		}

		private async void InstallPack (object sender, RoutedEventArgs e)
		{
			if (Config.Settings.SelectedPack?.Path is string && Config.Settings.SelectedPack.Meta?.Install is string)
			{
				var install = new Process();
				install.StartInfo.UseShellExecute = true;
				install.StartInfo.FileName = Path.Combine(Directory.GetCurrentDirectory(), Config.Settings.SelectedPack.Path, Config.Settings.SelectedPack.Meta.Install);
				install.StartInfo.WorkingDirectory = Path.Combine(Directory.GetCurrentDirectory(), Config.Settings.SelectedPack.Path);
				install.StartInfo.Verb = "runas";
				install.Start();
				await install.WaitForExitAsync();
			}
		}

		static Options _optionsWindow;
		private void OpenOptions (object sender, RoutedEventArgs e)
		{
			if (_optionsWindow is null)
			{
				_optionsWindow = new Options();
				_optionsWindow.Closed += (_, _) => _optionsWindow = null;
				_optionsWindow.Show();
			}
			else
			{
				_optionsWindow.Focus();
			}
		}

		static Browse _browseWindow;
		private void OpenBrowse (object sender, RoutedEventArgs e)
		{
			if (_browseWindow is null)
			{
				_browseWindow = new Browse();
				_browseWindow.Closed += async (_, _) =>
				{
					if (_browseWindow.SelectedPack is not null)
					{
						Config.Settings.SelectedPack = _browseWindow.SelectedPack;
						UpdateHelperAddress();
						_browser.GetBrowser().Reload(true);
						await Config.SaveAsync();
					}
					_browseWindow = null;
				};
				_browseWindow.Show();
			}
			else
			{
				_browseWindow.Focus();
			}
		}

		private void UpdateHelperAddress ()
		{
			ViewModel.HelperAddress = $"{Config.Settings.OverlayHost}/{Config.Settings.SelectedPack?.Meta.Url ?? "helper"}";
		}

		private void Close (object sender, RoutedEventArgs e)
		{
			Application.Current.Shutdown();
		}
	}

	public class MainWindowViewModel : INotifyPropertyChanged
	{
		string _helperAddress;
		public string HelperAddress
		{
			get => _helperAddress;
			set
			{
				_helperAddress = value;
				Changed(nameof(HelperAddress));
			}
		}

		ConnectionStatus _obsConnectionStatus;
		public ConnectionStatus ObsConnectionStatus
		{
			get => _obsConnectionStatus;
			set
			{
				_obsConnectionStatus = value;
				Changed(nameof(ObsConnectionStatus));
				Changed(nameof(ObsConnectionDescription));
				Changed(nameof(ObsConnectionVerb));
			}
		}
		public string ObsConnectionDescription => ObsConnectionStatus == ConnectionStatus.Connected ? "OBS Connected" : "OBS Disconnected";
		public string ObsConnectionVerb => ObsConnectionStatus == ConnectionStatus.Connected ? "Disconnect" : "Connect";


		ObservableCollection<SceneView> _scenes = new ObservableCollection<SceneView>();
		public ObservableCollection<SceneView> ObsScenes
		{
			get => _scenes;
			set
			{
				_scenes = value;
				Changed(nameof(ObsScenes));
			}
		}


		public event PropertyChangedEventHandler PropertyChanged;
		void Changed (string name)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
		}
	}
}
