using System.Reflection;
using Intersect.Plugins.Helpers;
using Intersect.Plugins.Interfaces;

namespace Intersect.Plugins.Contexts;

public abstract class PluginContext<TPluginContext, TLifecycleHelper> : IPluginContext<TPluginContext, TLifecycleHelper>, IPluginContext<TPluginContext>, IPluginContext, IPluginBaseContext where TPluginContext : IPluginContext<TPluginContext> where TLifecycleHelper : ILifecycleHelper
{
	private Plugin Plugin { get; }

	public Assembly Assembly => Plugin.Reference.Assembly;

	public PluginConfiguration Configuration => Plugin.Configuration;

	public IEmbeddedResourceHelper EmbeddedResources { get; }

	ILifecycleHelper IPluginContext.Lifecycle => Lifecycle;

	public abstract TLifecycleHelper Lifecycle { get; }

	public ILoggingHelper Logging => Plugin.Logging;

	public IManifestHelper Manifest => Plugin.Manifest;

	protected PluginContext(Plugin plugin)
	{
		Plugin = plugin;
		EmbeddedResources = new EmbeddedResourceHelper(Assembly);
	}

	public TConfiguration GetTypedConfiguration<TConfiguration>() where TConfiguration : PluginConfiguration
	{
		return Configuration as TConfiguration;
	}
}
