using System;
using System.IO;
using System.Threading.Tasks;

namespace KotS12CustomInstaller
{
	class Program
	{
		static async Task Main (string[] args)
		{
			var profile = new FileInfo(@"install/KotS12.json");
			using var reader = new StreamReader(profile.OpenRead());
			var contents = await reader.ReadToEndAsync();
			contents = contents.Replace("[PACK_DIR]", Directory.GetCurrentDirectory().Replace("\\", "/"));
			string scenesDir = Environment.ExpandEnvironmentVariables(@"%AppData%\obs-studio\basic\scenes\") + profile.Name;

			// write file to OBS appdata
			await File.WriteAllTextAsync(scenesDir, contents);
			await File.WriteAllTextAsync(scenesDir + ".bak", contents);
		}
	}
}
