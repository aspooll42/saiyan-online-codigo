using System;
using System.Threading;
using System.Threading.Tasks;
using Intersect.Framework.Eventing;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Intersect.Framework.Services.ExceptionHandling;

public sealed class ExceptionHandlingService : IExceptionHandlingService, IHostedService
{
	private readonly IHostApplicationLifetime _applicationLifetime;

	private readonly ILogger<ExceptionHandlingService> _logger;

	private readonly IServiceProvider _serviceProvider;

	public event EventHandler<UnhandledExceptionEventArgs>? UnhandledException;

	public event EventHandler<UnobservedTaskExceptionEventArgs>? UnobservedTaskException;

	public ExceptionHandlingService(IHostApplicationLifetime applicationLifetime, ILogger<ExceptionHandlingService> logger, IServiceProvider serviceProvider)
	{
		_applicationLifetime = applicationLifetime;
		_logger = logger;
		_serviceProvider = serviceProvider;
	}

	public void DispatchUnhandledException(Exception exception, bool isTerminating)
	{
		DispatchUnhandledException(null, exception, isTerminating);
	}

	public void DispatchUnhandledException(object? sender, Exception exception, bool isTerminating)
	{
		HandleUnhandledTaskException(sender, new UnhandledExceptionEventArgs(exception, isTerminating));
	}

	private void HandleUnhandledTaskException(object? sender, UnhandledExceptionEventArgs args)
	{
		this.UnhandledException?.Invoke(new AggregateSender<IExceptionHandlingService>(this, sender), args);
		if (args.IsTerminating)
		{
			_applicationLifetime.StopApplication();
		}
	}

	private void HandleUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs args)
	{
		this.UnobservedTaskException?.Invoke(new AggregateSender<IExceptionHandlingService>(this, sender), args);
		_applicationLifetime.StopApplication();
	}

	public Task StartAsync(CancellationToken cancellationToken)
	{
		AppDomain.CurrentDomain.UnhandledException += HandleUnhandledTaskException;
		TaskScheduler.UnobservedTaskException += HandleUnobservedTaskException;
		return Task.CompletedTask;
	}

	public Task StopAsync(CancellationToken cancellationToken)
	{
		AppDomain.CurrentDomain.UnhandledException -= HandleUnhandledTaskException;
		TaskScheduler.UnobservedTaskException -= HandleUnobservedTaskException;
		return Task.CompletedTask;
	}
}
