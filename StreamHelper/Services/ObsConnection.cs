using Microsoft.Extensions.DependencyInjection;
using OBSWebsocketDotNet;
using OBSWebsocketDotNet.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace StreamHelper.Services
{
	public enum ConnectionStatus
	{
		Disconnected,
		Connected
	}

	public interface IObsConnection
	{
		bool IsConnected { get; }
		sealed ConnectionStatus ConnectionStatus => IsConnected ? ConnectionStatus.Connected : ConnectionStatus.Disconnected;

		public event EventHandler<bool> ConnectionChanged;
		public event EventHandler<IEnumerable<OBSScene>> SceneCollectionChanged;
		public event EventHandler<string> SceneChanged;

		void Connect ();
		void Disconnect ();
		IEnumerable<OBSScene> GetSceneList ();
	}

	public class ObsConnection : IObsConnection, IDisposable
	{
		OBSWebsocket Obs { get; }
		ISettings Config { get; }


		public ObsConnection (OBSWebsocket obs, ISettings config)
		{
			Obs = obs;
			Config = config;

			Obs.Connected += ObsConnectionChange;
			Obs.Disconnected += ObsConnectionChange;
			//Obs.SceneChanged += ObsSceneChanged;
			Obs.TransitionBegin += ObsTransitionBegin;
			Obs.SceneCollectionChanged += ObsSceneCollectionChange;
		}

		public event EventHandler<bool> ConnectionChanged;
		public event EventHandler<IEnumerable<OBSScene>> SceneCollectionChanged;
		public event EventHandler<string> SceneChanged;

		public bool IsConnected => Obs.IsConnected;

		public void Connect ()
		{
			Obs.Connect(Config.Settings.ObsConnectionHost, Config.Settings.ObsConnectionPassword);
		}

		public void Disconnect ()
		{
			Obs.Disconnect();
		}

		public IEnumerable<OBSScene> GetSceneList ()
		{
			if (IsConnected)
			{
				return Obs.GetSceneList().Scenes;
			}
			else
			{
				return new List<OBSScene>();
			}
		}

		public OBSScene GetCurrentScene ()
		{
			if (IsConnected)
			{
				return Obs.GetCurrentScene();
			}
			else
			{
				return null;
			}
		}

		void ObsConnectionChange (object sender, EventArgs e)
		{
			ConnectionChanged?.Invoke(this, Obs.IsConnected);
			SceneCollectionChanged?.Invoke(this, GetSceneList());
			SceneChanged?.Invoke(this, GetCurrentScene()?.Name);
		}

		void ObsSceneCollectionChange (object sender, EventArgs e)
		{
			SceneCollectionChanged?.Invoke(this, GetSceneList());
		}

		//void ObsSceneChanged (OBSWebsocket sender, string sceneName)
		//{
		//	SceneChanged?.Invoke(this, sceneName);
		//}

		// TODO: improve things by implementing this in a more robust way
		async void ObsTransitionBegin (OBSWebsocket sender, string transitionName, string transitionType, int duration, string fromScene, string toScene)
		{
			// Delay the transition to halfway point
			if (transitionName == "DelayCut")
			{
				// TODO: this has no business being hard-coded, but stingers don't give a duration...
				await Task.Delay(1000);
			}
			else
			{
				await Task.Delay(duration / 2);
			}
			SceneChanged?.Invoke(this, toScene);
		}

		public void Dispose ()
		{
			if (Obs.IsConnected)
			{
				Obs.Disconnect();
			}
		}
	}

	public static class ObsConnectionProvider
	{
		public static IServiceCollection AddObsConnection (this IServiceCollection services)
		{
			return services
				.AddSingleton(new OBSWebsocket())
				.AddSingleton<IObsConnection, ObsConnection>();
		}
	}
}
