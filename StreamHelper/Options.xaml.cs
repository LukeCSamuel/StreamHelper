using StreamHelper.Controls;
using StreamHelper.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace StreamHelper
{
    /// <summary>
    /// Interaction logic for Options.xaml
    /// </summary>
    public partial class Options : DechromeWindow
    {
        ISettings Config { get; }

        OptionsViewModel ViewModel { get; set; }

        public Options()
        {
            InitializeComponent();
            DataContext = ViewModel = new OptionsViewModel();
            Config = (ISettings)Program.ServiceProvider.GetService(typeof(ISettings));
        }

        private void OnLoad (object sender, RoutedEventArgs e)
		{
            ViewModel.OverlayHost = Config.Settings.OverlayHost;
            ViewModel.ObsHost = Config.Settings.ObsConnectionHost;
            ViewModel.ObsPassword = Config.Settings.ObsConnectionPassword;
		}

        private async void Reset (object sender, RoutedEventArgs e)
		{
            Config.Settings = Settings.Default;
            await Config.SaveAsync();
            OnLoad(sender, e);
		}

        private async void Apply (object sender, RoutedEventArgs e)
		{
            Config.Settings.OverlayHost = ViewModel.OverlayHost;
            Config.Settings.ObsConnectionHost = ViewModel.ObsHost;
            if (Config.Settings.ObsConnectionPassword != ViewModel.ObsPassword)
			{
                Config.Settings.ObsConnectionPassword = ViewModel.ObsPassword;
			}
            await Config.SaveAsync();
		}

        private void Save (object sender, RoutedEventArgs e)
		{
            Apply(sender, e);
            Close();
		}

        private void Cancel (object sender, RoutedEventArgs e)
		{
            Close();
		}
    }

	public class OptionsViewModel : INotifyPropertyChanged
	{
        private string _overlayHost;
        public string OverlayHost {
            get => _overlayHost;
            set
			{
                _overlayHost = value;
                Changed(nameof(OverlayHost));
			}
        }

        private string _obsHost;
        public string ObsHost
		{
            get => _obsHost;
            set
			{
                _obsHost = value;
                Changed(nameof(ObsHost));
			}
		}

        private string _obsPassword;
        public string ObsPassword
		{
            get => _obsPassword;
            set
			{
                _obsPassword = value;
                Changed(nameof(ObsPassword));
			}
		}


        public event PropertyChangedEventHandler PropertyChanged;
        void Changed (string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
