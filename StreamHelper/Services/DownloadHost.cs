using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace StreamHelper.Services
{
	public class DownloadHost : HttpClient
	{
		public DownloadHost (string host) : base()
		{
			BaseAddress = new Uri(host);
		}
	}
	
	public static class DownloadHostProvider
	{
		public static IServiceCollection AddDownloadHost (this IServiceCollection services, string host)
		{
			return services.AddSingleton(new DownloadHost(host));
		}
	}
}
