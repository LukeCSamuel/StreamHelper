using DownloadController;
using DownloadShared;
using NuGet.Versioning;
using StreamHelper.Controls;
using StreamHelper.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
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
    /// Interaction logic for Browse.xaml
    /// </summary>
    public partial class Browse : DechromeWindow
    {
		static JsonSerializerOptions Opts { get; } = new() { PropertyNameCaseInsensitive = true };
        BrowseViewModel ViewModel { get; set; }
        HttpClient Client { get; }
        IDownloadService DownloadService { get; }
        IPackService PackService { get; }

        public PackListItem SelectedPack { get; set; } = null;

        public Browse()
        {
            InitializeComponent();
            DataContext = ViewModel = new BrowseViewModel();
            Client = (HttpClient)Program.ServiceProvider.GetService(typeof(DownloadHost));
            DownloadService = (IDownloadService)Program.ServiceProvider.GetService(typeof(IDownloadService));
            PackService = (IPackService)Program.ServiceProvider.GetService(typeof(IPackService));
        }

        public async void OnLoad (object sender, RoutedEventArgs e)
		{
            var downloadedPacks = new List<PackListItem>();
            Directory.CreateDirectory("packs");
            var packDir = new DirectoryInfo("packs");
            foreach (var dir in packDir.EnumerateDirectories())
			{
                var metaPath = Path.Combine(dir.FullName, "pack.meta");
                if (File.Exists(metaPath))
				{
                    using var fs = new FileStream(metaPath, FileMode.Open);
                    var meta = await JsonSerializer.DeserializeAsync<PackMeta>(fs, Opts);
                    downloadedPacks.Add(new PackListItem()
                    {
                        Path = $"packs/{dir.Name}",
                        Meta = meta,
                        UpdateAvailable = false
                    });
				}
			}

            // List the available packs and add them to the model
            ViewModel.OnlinePacks.Clear();
            using var onlinePackStream = await Client.GetStreamAsync("Pack/list");
            var onlinePacks = await JsonSerializer.DeserializeAsync<List<PackListItem>>(onlinePackStream, Opts);
            foreach (var pack in onlinePacks)
            {
                var downloadedPack = downloadedPacks.SingleOrDefault(p => p.Path == pack.Path);
                if (downloadedPack is null)
                {
                    ViewModel.OnlinePacks.Add(pack);
                }
                else
				{
                    if (SemanticVersion.Parse(pack.Meta.Version) > SemanticVersion.Parse(downloadedPack.Meta.Version))
					{
                        downloadedPack.UpdateAvailable = true;
					}
				}
            }

            ViewModel.DownloadedPacks = new ObservableCollection<PackListItem>(downloadedPacks);
        }

        public void Select (object sender, RoutedEventArgs e)
		{
            SelectedPack = ((Button)e.OriginalSource).DataContext as PackListItem;
            Close();
		}

        public async void Download (object sender, RoutedEventArgs e)
		{
            try
            {
                CanClose = false;
                ViewModel.IsNotDownloading = false;

				var pack = ((Button)e.OriginalSource).DataContext as PackListItem;

				Stream reqStream;
				if (File.Exists(Path.Combine(pack.Path, ".pack")))
				{
					reqStream = new FileStream(Path.Combine(pack.Path, ".pack"), FileMode.OpenOrCreate);
				}
				else
				{
					reqStream = new MemoryStream();
					await JsonSerializer.SerializeAsync(reqStream, new PackFile(), Opts);
					reqStream.Position = 0;
				}

				var content = new StreamContent(reqStream);
				content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
				var response = await Client.PostAsync($"Pack/{pack.Path}", content);

				reqStream.Dispose();
				if (response.IsSuccessStatusCode)
				{
					var diff = await JsonSerializer.DeserializeAsync<PackDiff>(
						await response.Content.ReadAsStreamAsync(),
						Opts
					);

                    _progressBar.Value = 7;
                    _progressBar.Visibility = Visibility.Visible;
                    _lblProgress.Visibility = Visibility.Visible;

					// Handle files
					long sum = 0;
					var progress = new Progress<int>(val =>
					{
						sum += val;
						_progressBar.Value = 7 + 93.0 * sum / diff.TotalBytes;
						_lblProgress.Content = $"{(int)_progressBar.Value}% - {sum.BytesToReadable()} / {diff.TotalBytes.BytesToReadable()}";
					});
                    var target = Directory.CreateDirectory(pack.Path);
                    await PackService.ResolveDiffAsync(diff, target, progress);
                    _progressBar.Value = 100;
				}
                else
				{
                    MessageBox.Show("Could not caclulate diff, download failed.", "Pack Download Failed", MessageBoxButton.OK, MessageBoxImage.Error);
				}

                // Update collections
                SelectedPack = pack;
                OnLoad(sender, e);
			}
            finally
			{
                _progressBar.Visibility = Visibility.Hidden;
                _lblProgress.Visibility = Visibility.Hidden;
                ViewModel.IsNotDownloading = true;
				CanClose = true;
			}
		}
    }

    public class BrowseViewModel : INotifyPropertyChanged
    {
        ObservableCollection<PackListItem> _onlinePacks = new ObservableCollection<PackListItem>();
        public ObservableCollection<PackListItem> OnlinePacks
		{
            get => _onlinePacks;
            set
			{
                _onlinePacks = value;
                value.CollectionChanged += (_, _) => Changed(nameof(OnlinePackCount));
                Changed(nameof(OnlinePacks));
            }
		}
        public string OnlinePackCount => $"({OnlinePacks.Count})";

        ObservableCollection<PackListItem> _downloadedPacks = new ObservableCollection<PackListItem>();
        public ObservableCollection<PackListItem> DownloadedPacks
		{
            get => _downloadedPacks;
			set
			{
                _downloadedPacks = value;
                value.CollectionChanged += (_, _) => Changed(nameof(DownloadedPackCount));
                Changed(nameof(DownloadedPacks));
            }
		}
        public string DownloadedPackCount => $"({DownloadedPacks.Count})";

        bool _isNotDownloading = true;
        public bool IsNotDownloading
		{
            get => _isNotDownloading;
            set
			{
                _isNotDownloading = value;
                Changed(nameof(IsNotDownloading));
			}
		}

        public event PropertyChangedEventHandler PropertyChanged;
        void Changed (string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
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