using Intersect.Plugins.Interfaces;

namespace Intersect.Plugins;

public interface IPluginContext : IPluginBaseContext
{
	ILifecycleHelper Lifecycle { get; }
}
public interface IPluginContext<TPluginContext> : IPluginContext, IPluginBaseContext where TPluginContext : IPluginContext<TPluginContext>
{
}
public interface IPluginContext<TPluginContext, out TLifecycleHelper> : IPluginContext<TPluginContext>, IPluginContext, IPluginBaseContext where TPluginContext : IPluginContext<TPluginContext> where TLifecycleHelper : ILifecycleHelper
{
	new TLifecycleHelper Lifecycle { get; }
}
