using Intersect.Factories;
using Microsoft;

namespace Intersect.Plugins;

public class PluginInstance
{
	public IPluginEntry Entry { get; }

	public IPluginBootstrapContext BootstrapContext { get; }

	public IPluginContext Context { get; }

	public static PluginInstance Create([ValidatedNotNull] Plugin plugin)
	{
		IPluginBootstrapContext pluginBootstrapContext = FactoryRegistry<IPluginBootstrapContext>.Create(plugin);
		IPluginContext context = FactoryRegistry<IPluginContext>.Create(plugin, pluginBootstrapContext.Packet);
		IPluginEntry entry = plugin.Reference.CreateInstance();
		return new PluginInstance(entry, pluginBootstrapContext, context);
	}

	private PluginInstance(IPluginEntry entry, IPluginBootstrapContext bootstrapContext, IPluginContext context)
	{
		Entry = entry;
		BootstrapContext = bootstrapContext;
		Context = context;
	}
}
