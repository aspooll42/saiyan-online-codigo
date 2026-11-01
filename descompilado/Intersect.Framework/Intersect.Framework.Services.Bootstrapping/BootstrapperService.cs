using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Intersect.Framework.Services.Bootstrapping;

public sealed class BootstrapperService : IBootstrapperService, IHostedService
{
	private readonly IHostApplicationLifetime _applicationLifetime;

	private readonly BootstrapServiceOptions _options;

	private readonly IServiceProvider _serviceProvider;

	private CancellationTokenSource? _cancellationTokenSource;

	public BootstrapperService(IHostApplicationLifetime applicationLifetime, BootstrapServiceOptions options, IServiceProvider serviceProvider)
	{
		_applicationLifetime = applicationLifetime;
		_options = options;
		_serviceProvider = serviceProvider;
	}

	public Task StartAsync(CancellationToken cancellationToken)
	{
		_cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
		IEnumerable<IBootstrapTask> services = _serviceProvider.GetServices<IBootstrapTask>();
		return Task.WhenAll(services.Select((IBootstrapTask task) => task.ExecuteAsync(_cancellationTokenSource.Token))).ContinueWith(delegate
		{
			_applicationLifetime.StopApplication();
			return Task.CompletedTask;
		}, _cancellationTokenSource.Token);
	}

	public Task StopAsync(CancellationToken cancellationToken)
	{
		_cancellationTokenSource?.Cancel();
		return Task.CompletedTask;
	}
}
