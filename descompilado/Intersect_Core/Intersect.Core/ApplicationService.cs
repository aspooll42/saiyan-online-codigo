using System;
using Microsoft;

namespace Intersect.Core;

public abstract class ApplicationService<TServiceInterface, TServiceImplementation> : IApplicationService, IDisposable where TServiceImplementation : ApplicationService<TServiceInterface, TServiceImplementation>, TServiceInterface
{
	private bool mDisposed;

	private readonly object mLifecycleLock;

	public abstract bool IsEnabled { get; }

	public bool IsRunning { get; private set; }

	public string Name => typeof(TServiceImplementation).Name;

	public Type ServiceType => typeof(TServiceInterface);

	protected ApplicationService()
	{
		mLifecycleLock = new object();
	}

	public virtual bool Bootstrap(IApplicationContext applicationContext)
	{
		return true;
	}

	public void Dispose()
	{
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}

	protected virtual void Dispose(bool disposing)
	{
		if (!mDisposed)
		{
			mDisposed = true;
		}
	}

	public bool Start(IApplicationContext applicationContext)
	{
		lock (mLifecycleLock)
		{
			if (IsRunning)
			{
				throw new InvalidOperationException("Service '" + Name + "' already running.");
			}
			try
			{
				TaskStart(applicationContext);
				IsRunning = true;
			}
			catch (Exception innerException)
			{
				throw new ServiceLifecycleFailureException(ServiceLifecycleStage.Startup, Name, innerException);
			}
		}
		return IsRunning;
	}

	public bool Stop(IApplicationContext applicationContext)
	{
		lock (mLifecycleLock)
		{
			if (!IsRunning)
			{
				throw new InvalidOperationException("Service '" + Name + "' is not yet running.");
			}
			try
			{
				TaskStop(applicationContext);
				IsRunning = false;
			}
			catch (Exception innerException)
			{
				throw new ServiceLifecycleFailureException(ServiceLifecycleStage.Shutdown, Name, innerException);
			}
		}
		return !IsRunning;
	}

	protected abstract void TaskStart([ValidatedNotNull] IApplicationContext applicationContext);

	protected abstract void TaskStop([ValidatedNotNull] IApplicationContext applicationContext);
}
public abstract class ApplicationService<TApplicationContext, TServiceInterface, TServiceImplementation> : ApplicationService<TServiceInterface, TServiceImplementation> where TServiceImplementation : ApplicationService<TApplicationContext, TServiceInterface, TServiceImplementation>, TServiceInterface
{
	protected override void TaskStart([ValidatedNotNull] IApplicationContext applicationContext)
	{
		if (applicationContext is TApplicationContext applicationContext2)
		{
			TaskStart(applicationContext2);
			return;
		}
		throw new ArgumentException($"Invalid context, expected type {typeof(TApplicationContext).FullName} but received {applicationContext.GetType().FullName}.", "applicationContext");
	}

	protected override void TaskStop([ValidatedNotNull] IApplicationContext applicationContext)
	{
		if (applicationContext is TApplicationContext applicationContext2)
		{
			TaskStop(applicationContext2);
			return;
		}
		throw new ArgumentException($"Invalid context, expected type {typeof(TApplicationContext).FullName} but received {applicationContext.GetType().FullName}.", "applicationContext");
	}

	protected abstract void TaskStart(TApplicationContext applicationContext);

	protected abstract void TaskStop(TApplicationContext applicationContext);
}
