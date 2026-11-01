using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Intersect.Framework.Reflection;
using Intersect.Network;
using Intersect.Plugins.Interfaces;
using Intersect.Properties;
using Intersect.Reflection;
using Intersect.Threading;
using Microsoft.Extensions.Logging;

namespace Intersect.Core;

public abstract class ApplicationContext<TContext, TStartupOptions> : IApplicationContext<TStartupOptions>, IApplicationContext, IDisposable where TContext : ApplicationContext<TContext, TStartupOptions> where TStartupOptions : ICommandLineOptions
{
	private bool _running;

	private bool _needsLockPulse;

	private readonly object _disposeLock = new object();

	private readonly object _shutdownLock = new object();

	private readonly IDictionary<Type, IApplicationService> mServices = new ConcurrentDictionary<Type, IApplicationService>();

	public string Name { get; }

	ICommandLineOptions IApplicationContext.StartupOptions => StartupOptions;

	public TStartupOptions StartupOptions { get; }

	public ILogger Logger { get; }

	public IPacketHelper PacketHelper { get; }

	public string VersionName => GetType().Assembly.GetMetadataVersionName();

	protected virtual bool UsesMainThread => false;

	protected bool IsDisposing { get; private set; }

	protected bool IsShutdownRequested { get; private set; }

	public bool IsDisposed { get; private set; }

	public bool IsStarted { get; private set; }

	public bool IsRunning
	{
		get
		{
			if (_running)
			{
				return !IsShutdownRequested;
			}
			return false;
		}
		private set
		{
			_running = value;
		}
	}

	public List<IApplicationService> Services => mServices.Values.ToList();

	public bool HasErrors { get; private set; }

	private TContext This => (this as TContext) ?? throw new InvalidOperationException();

	private static ConcurrentInstance<TContext> ConcurrentInstance { get; }

	public static TContext Instance => ConcurrentInstance;

	protected ApplicationContext(Assembly entryAssembly, string fallbackName, TStartupOptions startupOptions, ILogger logger, IPacketHelper packetHelper)
	{
		Name = entryAssembly.GetName().Name ?? fallbackName;
		ApplicationContext.Context.Value = this;
		StartupOptions = startupOptions;
		Logger = logger;
		PacketHelper = packetHelper;
		ConcurrentInstance.Set(This);
	}

	public TApplicationService GetService<TApplicationService>() where TApplicationService : IApplicationService
	{
		if (mServices.TryGetValue(typeof(TApplicationService), out var value))
		{
			return (TApplicationService)value;
		}
		return default(TApplicationService);
	}

	protected TApplicationService GetExpectedService<TApplicationService>() where TApplicationService : IApplicationService
	{
		TApplicationService val = (TApplicationService)mServices[typeof(TApplicationService)];
		if (val != null)
		{
			return val;
		}
		throw new AccessViolationException("Missing expected service of type " + typeof(TApplicationService).FullName + ".");
	}

	protected virtual IEnumerable<Assembly> GetAssemblies()
	{
		Type type = GetType();
		List<Assembly> list = new List<Assembly>();
		list.Add(typeof(IApplicationContext).Assembly);
		list.Add(typeof(SharedConstants).Assembly);
		list.Add(typeof(TContext).Assembly);
		List<Assembly> list2 = list;
		while (type != null && type != typeof(object) && type != typeof(TContext))
		{
			list2.Add(type.Assembly);
			type = type.BaseType;
		}
		return list2;
	}

	private void AddService(Type type, IApplicationService service)
	{
		if (!mServices.ContainsKey(type))
		{
			mServices.Add(type, service);
		}
	}

	protected virtual void DiscoverServices()
	{
		Type[] array = GetAssemblies().SelectMany(Intersect.Reflection.AssemblyExtensions.FindDefinedSubtypesOf<IApplicationService>).ToArray();
		Type[] array2 = array;
		foreach (Type type in array2)
		{
			ApplicationContext.Context.Value?.Logger.LogInformation("Discovered service type: " + type.FullName);
			if (!(Activator.CreateInstance(type) is IApplicationService applicationService))
			{
				throw new InvalidOperationException("Failed to create service of type " + type.FullName + ".");
			}
			AddService(applicationService.ServiceType, applicationService);
		}
	}

	private void RunOnAllServices(Action<IApplicationService> action, bool isRunning, bool force = true)
	{
		Services.Where((IApplicationService service) => service != null && service.IsEnabled && (force || isRunning == service.IsRunning)).ToList().ForEach(action);
	}

	protected virtual void BootstrapServices()
	{
		RunOnAllServices(delegate(IApplicationService service)
		{
			service?.Bootstrap(this);
		}, isRunning: false);
	}

	protected virtual void StartServices()
	{
		RunOnAllServices(delegate(IApplicationService service)
		{
			service?.Start(this);
		}, isRunning: false);
	}

	protected virtual void StopServices(bool force = false)
	{
		RunOnAllServices(delegate(IApplicationService service)
		{
			service?.Stop(this);
		}, isRunning: true, force);
	}

	public void Start(bool lockUntilShutdown = true)
	{
		IsStarted = true;
		DiscoverServices();
		try
		{
			BootstrapServices();
			PackedIntersectPacket.AddKnownTypes(PacketHelper.AvailablePacketTypes);
			try
			{
				InternalStart();
			}
			catch (Exception exception)
			{
				Logger.LogError(exception, "Failed to start application context");
				return;
			}
			if (UsesMainThread)
			{
				return;
			}
			PostStartup();
		}
		catch (ServiceLifecycleFailureException exception2)
		{
			Logger.LogError(exception2, "Error occurred during application startup");
			return;
		}
		_needsLockPulse = lockUntilShutdown;
		if (!_needsLockPulse)
		{
			return;
		}
		lock (_shutdownLock)
		{
			Monitor.Wait(_shutdownLock);
			ApplicationContext.Context.Value?.Logger.LogTrace(DeveloperStrings.ApplicationContextExited);
		}
	}

	protected virtual void PostStartup()
	{
		IsRunning = true;
		StartServices();
	}

	public ILockingActionQueue StartWithActionQueue()
	{
		Start(lockUntilShutdown: false);
		_needsLockPulse = true;
		return new LockingActionQueue(_shutdownLock);
	}

	protected abstract void InternalStart();

	public void RequestShutdown(bool join = false)
	{
		Task task;
		lock (_disposeLock)
		{
			if (IsDisposed || IsDisposing || IsShutdownRequested)
			{
				return;
			}
			IsShutdownRequested = true;
			task = new Task(delegate
			{
				Dispose();
				lock (_shutdownLock)
				{
					Monitor.PulseAll(_shutdownLock);
				}
			});
			task.Start();
		}
		if (join)
		{
			task.Wait();
		}
	}

	public static void AttachHandlers()
	{
		AppDomain.CurrentDomain.AssemblyResolve += HandleAssemblyResolve;
		AppDomain.CurrentDomain.UnhandledException += HandleUnhandledException;
		TaskScheduler.UnobservedTaskException += HandleUnobservedTaskException;
	}

	public static void DetachHandlers()
	{
		AppDomain.CurrentDomain.AssemblyResolve -= HandleAssemblyResolve;
		AppDomain.CurrentDomain.UnhandledException -= HandleUnhandledException;
		TaskScheduler.UnobservedTaskException -= HandleUnobservedTaskException;
	}

	protected virtual void NotifyNonTerminatingExceptionOccurred()
	{
	}

	internal static void ProcessUnhandledException(object sender, Exception exception)
	{
		ApplicationContext.Context.Value?.Logger.LogError(exception, "Received an unhandled exception from {Sender}", sender);
	}

	private void SafeAbort(bool hasErrors)
	{
		HasErrors |= hasErrors;
		if (!IsDisposed)
		{
			RequestShutdown(join: true);
		}
	}

	protected static void HandleUnhandledException(object sender, UnhandledExceptionEventArgs unhandledExceptionEvent)
	{
		if (!(unhandledExceptionEvent.ExceptionObject is Exception exception))
		{
			throw new ArgumentNullException("ExceptionObject");
		}
		ProcessUnhandledException(sender, exception);
		if (ConcurrentInstance.HasInstance)
		{
			if (!unhandledExceptionEvent.IsTerminating)
			{
				ConcurrentInstance.Instance.NotifyNonTerminatingExceptionOccurred();
			}
			ConcurrentInstance.Instance.SafeAbort(hasErrors: true);
		}
	}

	private static void HandleUnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs unobservedTaskExceptionEvent)
	{
		ProcessUnhandledException(sender, unobservedTaskExceptionEvent.Exception ?? throw new ArgumentNullException("Exception"));
		if (ConcurrentInstance.HasInstance)
		{
			ConcurrentInstance.Instance.SafeAbort(hasErrors: true);
		}
	}

	private static Assembly HandleAssemblyResolve(object sender, ResolveEventArgs args)
	{
		string name = args.Name;
		if (name != null && name.Contains(".resources"))
		{
			return null;
		}
		Assembly assembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault((Assembly fodAssembly) => fodAssembly?.FullName == args.Name);
		if (assembly != null)
		{
			return assembly;
		}
		string name2 = args.Name;
		string text = ((name2 != null) ? name2.Split(',')[0] : null) + ".dll";
		string path = Path.Combine(AppDomain.CurrentDomain.SetupInformation.ApplicationBase, "libs", "server");
		if (File.Exists(Path.Combine(path, text)))
		{
			return Assembly.LoadFile(Path.Combine(path, text));
		}
		string path2 = Path.Combine(AppDomain.CurrentDomain.SetupInformation.ApplicationBase, Environment.Is64BitProcess ? Path.Combine("libs", "server", "x64") : Path.Combine("libs", "server", "x86"), text);
		if (!File.Exists(path2))
		{
			return null;
		}
		return Assembly.LoadFile(path2);
	}

	static ApplicationContext()
	{
		AttachHandlers();
		ConcurrentInstance = new ConcurrentInstance<TContext>();
	}

	public void Dispose()
	{
		if (IsDisposed)
		{
			throw new ObjectDisposedException(typeof(TContext).Name);
		}
		lock (_disposeLock)
		{
			if (IsDisposing)
			{
				return;
			}
			IsDisposing = true;
		}
		IsRunning = false;
		ConcurrentInstance.ClearWith(This, InternalDispose);
	}

	private void InternalDispose()
	{
		try
		{
			StopServices(IsRunning);
		}
		catch (ServiceLifecycleFailureException exception)
		{
			Logger.LogError(exception, "Error occurred while stopping services during context disposal");
		}
		Stopwatch stopwatch = new Stopwatch();
		stopwatch.Start();
		ApplicationContext.Context.Value?.Logger.LogInformation($"Beginning context dispose. ({stopwatch.ElapsedMilliseconds}ms)");
		Dispose(disposing: true);
		ApplicationContext.Context.Value?.Logger.LogInformation($"GC.SuppressFinalize ({stopwatch.ElapsedMilliseconds}ms)");
		GC.SuppressFinalize(this);
		ApplicationContext.Context.Value?.Logger.LogInformation($"InternalDispose() completed. ({stopwatch.ElapsedMilliseconds}ms)");
		IsDisposed = true;
	}

	protected virtual void Dispose(bool disposing)
	{
	}
}
