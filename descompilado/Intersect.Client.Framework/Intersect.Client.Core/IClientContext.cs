using System;
using Intersect.Core;

namespace Intersect.Client.Core;

public interface IClientContext : IApplicationContext<ClientCommandLineOptions>, IApplicationContext, IDisposable
{
	IPlatformRunner PlatformRunner { get; }
}
