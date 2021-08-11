using System;
using System.Reflection;
using System.Threading;

namespace DupGuard
{
	public static class DuplicationGuard
	{
		const string _sharedGuid = "{20173546-4c8a-4cdf-8a35-9139c24fec61}";

		/// <summary>
		/// Check if the application is already running.
		/// </summary>
		/// <returns>True if already running.</returns>
		public static bool IsAlreadyRunning ()
		{
			string location = Assembly.GetExecutingAssembly().Location;
			var mutex = new Mutex(true, $"Global\\{_sharedGuid}", out bool createdNew);
			if (createdNew)
			{
				mutex.ReleaseMutex();
			}

			return !createdNew;
		}
	}
}
