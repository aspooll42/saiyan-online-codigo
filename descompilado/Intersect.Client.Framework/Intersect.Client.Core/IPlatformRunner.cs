using System;

namespace Intersect.Client.Core;

public interface IPlatformRunner
{
	void Start(IClientContext context, Action postStartupAction);
}
