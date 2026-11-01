using System;

namespace Intersect.Core;

public enum ServiceLifecycleStage
{
	Bootstrap,
	Startup,
	Shutdown,
	[Obsolete("This should only be used by ServiceLifecycleFailureException.")]
	Unknown
}
