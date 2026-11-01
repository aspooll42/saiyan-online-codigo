using System;

namespace Intersect.Plugins;

public abstract class PluginEntry : IPluginEntry, IDisposable
{
	private bool mDisposed;

	public virtual void OnBootstrap(IPluginBootstrapContext context)
	{
	}

	public virtual void OnStart(IPluginContext context)
	{
	}

	public virtual void OnStop(IPluginContext context)
	{
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
}
public abstract class PluginEntry<TPluginContext> : PluginEntry, IPluginEntry<TPluginContext>, IPluginEntry, IDisposable where TPluginContext : IPluginContext<TPluginContext>
{
	public override void OnStart(IPluginContext context)
	{
		if (context is TPluginContext context2)
		{
			OnStart(context2);
		}
	}

	public override void OnStop(IPluginContext context)
	{
		if (context is TPluginContext context2)
		{
			OnStop(context2);
		}
	}

	public abstract void OnStart(TPluginContext context);

	public abstract void OnStop(TPluginContext context);
}
