using DownloadShared;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace StreamHelper.Services
{
	public class Settings
	{
		public string ObsConnectionHost { get; set; }
		public string ObsConnectionPasswordEncrypted { get; set; }
		public string OverlayHost { get; set; }
		public PackListItem SelectedPack { get; set; }

		[JsonIgnore]
		private byte[] Entropy => Encoding.ASCII.GetBytes(Assembly.GetExecutingAssembly().FullName);

		[JsonIgnore]
		public string ObsConnectionPassword
		{
			get
			{
				if (ObsConnectionPasswordEncrypted is null)
				{
					return "overlay";
				}
				byte[] protectedData = Convert.FromBase64String(ObsConnectionPasswordEncrypted);
				return Encoding.UTF8.GetString(ProtectedData.Unprotect(protectedData, Entropy, DataProtectionScope.CurrentUser));
			}

			set
			{
				byte[] data = Encoding.UTF8.GetBytes(value);
				ObsConnectionPasswordEncrypted = Convert.ToBase64String(ProtectedData.Protect(data, Entropy, DataProtectionScope.CurrentUser));
			}
		}

		public static Settings Default => new()
		{
			ObsConnectionPassword = "overlay",
			ObsConnectionHost = "ws://localhost:4201",
			OverlayHost = "http://localhost:4200",
			SelectedPack = null
		};
	}
	
	public interface ISettings
	{
		Settings Settings { get; set; }
		Task<bool> LoadAsync ();
		Task SaveAsync ();
	}

	public class SettingsManager : ISettings
	{
		public Settings Settings { get; set; }

		public async Task<bool> LoadAsync ()
		{
			using var settingsFile = new FileStream(@"settings.json", FileMode.OpenOrCreate);
			try
			{
				Settings = await JsonSerializer.DeserializeAsync<Settings>(settingsFile);
				return true;
			}
			catch (Exception)
			{
				Settings = Settings.Default;
				return false;
			}
		}

		public async Task SaveAsync ()
		{
			using var settingsFile = new FileStream(@"settings.json", FileMode.Create);
			await JsonSerializer.SerializeAsync(settingsFile, Settings);
		}
	}
}
