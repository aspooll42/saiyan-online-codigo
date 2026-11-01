using Intersect.Framework.Annotations;

namespace Intersect.Config;

public class MetricsOptions
{
	[RequiresRestart]
	public bool Enable { get; set; }
}
