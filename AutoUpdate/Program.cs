using DupGuard;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;

namespace AutoUpdate
{
	class Program
	{
		public static bool NoStart { get; set; } = false;
		public static string TargetDir { get; set; } = "..";

		[STAThread]
		public static void Main (string[] args)
		{
			if (args.Any(arg => Regex.IsMatch(arg, "--[Nn]o[Ss]tart")))
			{
				NoStart = true;
			}
			var targetRegex = new Regex("--[Tt]arget=\"*([^\"]*)\"*");
			var targetArg = args.FirstOrDefault(arg => targetRegex.IsMatch(arg));
			if (targetArg is not null)
			{
				TargetDir = targetRegex.Match(targetArg).Groups[1].Value;
			}

			//Determine if the application is already running
			if (DuplicationGuard.IsAlreadyRunning())
			{
				MessageBox.Show("An instance of the Stream Helper is already running.", "Stream Helper",
					MessageBoxButton.OK, MessageBoxImage.Error);
				return;
			}

			// Create app and run
			var app = new App();
			app.InitializeComponent();
			app.Run();
		}
	}
}
