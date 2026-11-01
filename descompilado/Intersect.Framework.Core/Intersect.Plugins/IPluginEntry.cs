using System;
using Intersect.Framework.Reflection;

namespace Intersect.Plugins;

public interface IPluginEntry : IDisposable
{
	void OnBootstrap(IPluginBootstrapContext context);

	void OnStart(IPluginContext context);

	void OnStop(IPluginContext context);
}
public interface IPluginEntry<in TPluginContext> : IPluginEntry, IDisposable where TPluginContext : IPluginContext<TPluginContext>
{
	void IPluginEntry.OnStart(IPluginContext context)
	{
		if (!(context is TPluginContext context2))
		{
			string name = typeof(TPluginContext).GetName(qualified: true);
			string name2 = context.GetType().GetName(qualified: true);
			throw new ArgumentException("Expected " + name + " but received " + name2, "context");
		}
		OnStart(context2);
	}

	void IPluginEntry.OnStop(IPluginContext context)
	{
		if (!(context is TPluginContext context2))
		{
			string name = typeof(TPluginContext).GetName(qualified: true);
			string name2 = context.GetType().GetName(qualified: true);
			throw new ArgumentException("Expected " + name + " but received " + name2, "context");
		}
		OnStop(context2);
	}

	void OnStart(TPluginContext context);

	void OnStop(TPluginContext context);
}
