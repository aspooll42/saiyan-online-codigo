namespace Intersect.Plugins.Helpers;

public abstract class ContextHelper<TContext> where TContext : IPluginContext
{
	protected TContext Context { get; }

	protected ContextHelper(TContext context)
	{
		Context = context;
	}
}
