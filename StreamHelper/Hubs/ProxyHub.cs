using Microsoft.AspNetCore.SignalR;
using OBSWebsocketDotNet.Types;
using StreamHelper.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace StreamHelper.Hubs
{
	public class ProxyHub : Hub
	{
		public async Task Proxy (object data) => await Clients.All.SendAsync("proxy", data);
	}
}
