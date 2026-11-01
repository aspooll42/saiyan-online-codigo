using System;

namespace Intersect.Core;

public interface IApplicationService : IDisposable
{
	Type ServiceType { get; }

	string Name { get; }

	bool IsEnabled { get; }

	bool IsRunning { get; }

	bool Bootstrap(IApplicationContext applicationContext);

	bool Start(IApplicationContext applicationContext);

	bool Stop(IApplicationContext applicationContext);
}
