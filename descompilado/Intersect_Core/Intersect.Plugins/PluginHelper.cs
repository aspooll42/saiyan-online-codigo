using Microsoft.Extensions.Logging;

namespace Intersect.Plugins;

public abstract class PluginHelper
{
	protected ILogger Logger { get; }

	protected PluginHelper(ILogger logger)
	{
		Logger = logger;
	}
}
