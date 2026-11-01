using System;
using System.Collections.Generic;
using Intersect.Framework.Reflection;
using Intersect.Plugins.Interfaces;
using Intersect.Threading;
using Microsoft.Extensions.Logging;

namespace Intersect.Core;

public interface IApplicationContext : IDisposable
{
	bool HasErrors { get; }

	bool IsDebug => true;

	bool IsDeveloper => false;

	bool IsDisposed { get; }

	bool IsStarted { get; }

	bool IsRunning { get; }

	string Name { get; }

	ICommandLineOptions StartupOptions { get; }

	ILogger Logger { get; }

	IPacketHelper PacketHelper { get; }

	List<IApplicationService> Services { get; }

	string Version => GetType().Assembly.GetMetadataVersion();

	string VersionName => GetType().Assembly.GetMetadataVersionName();

	TApplicationService GetService<TApplicationService>() where TApplicationService : IApplicationService;

	void Start(bool lockUntilShutdown = true);

	ILockingActionQueue StartWithActionQueue();
}
public interface IApplicationContext<out TStartupOptions> : IApplicationContext, IDisposable where TStartupOptions : ICommandLineOptions
{
	new TStartupOptions StartupOptions { get; }
}
