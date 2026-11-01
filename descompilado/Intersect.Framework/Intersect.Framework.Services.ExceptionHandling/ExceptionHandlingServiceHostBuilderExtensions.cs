using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Intersect.Framework.Services.ExceptionHandling;

public static class ExceptionHandlingServiceHostBuilderExtensions
{
	public static IHostBuilder UseExceptionHandling(this IHostBuilder hostBuilder)
	{
		return hostBuilder.ConfigureServices(delegate(HostBuilderContext _, IServiceCollection services)
		{
			services.AddHostedService<ExceptionHandlingService>();
		});
	}
}
