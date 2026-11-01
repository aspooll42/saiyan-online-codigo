using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Intersect.Client.Networking;
using Intersect.Client.Plugins.Contexts;
using Intersect.Configuration;
using Intersect.Core;
using Intersect.Factories;
using Intersect.Framework.Core.Security;
using Intersect.Framework.Net;
using Intersect.Framework.Reflection;
using Intersect.Network;
using Intersect.Plugins;
using Intersect.Plugins.Interfaces;
using Microsoft.Extensions.Logging;

namespace Intersect.Client.Core;

internal sealed class ClientContext : ApplicationContext<ClientContext, ClientCommandLineOptions>, IClientContext, IApplicationContext<ClientCommandLineOptions>, IApplicationContext, IDisposable
{
	private IPlatformRunner? mPlatformRunner;

	internal static bool IsSinglePlayer { get; set; }

	public bool IsDeveloper { get; private set; }

	protected override bool UsesMainThread => true;

	public IPlatformRunner PlatformRunner
	{
		get
		{
			return mPlatformRunner ?? throw new ArgumentNullException("PlatformRunner");
		}
		private set
		{
			mPlatformRunner = value;
		}
	}

	public event PermissionSetChangedHandler? PermissionsChanged;

	internal ClientContext(Assembly entryAssembly, ClientCommandLineOptions startupOptions, ClientConfiguration clientConfiguration, ILogger logger, IPacketHelper packetHelper)
		: base(entryAssembly, "Intersect", startupOptions, logger, packetHelper)
	{
		PermissionSet.ActivePermissionSetChanged += PermissionSetOnActivePermissionSetChanged;
		PermissionSet.PermissionSetUpdated += PermissionSetOnPermissionSetUpdated;
		string host = clientConfiguration.Host;
		try
		{
			IsDeveloper = !(Dns.GetHostAddresses(host).FirstOrDefault()?.IsPublic() ?? true);
		}
		catch (SocketException ex)
		{
			if (ex.SocketErrorCode != SocketError.HostNotFound)
			{
				throw;
			}
			ClientNetwork.UnresolvableHostNames.Add(host);
			ApplicationContext.Context.Value?.Logger.LogError(ex, "Failed to resolve host '{HostNameOrAddress}'", host);
			IsDeveloper = true;
		}
		FactoryRegistry<IPluginContext>.RegisterFactory(new ClientPluginContext.Factory());
	}

	private void PermissionSetOnPermissionSetUpdated(PermissionSet permissionSet)
	{
		if (permissionSet == PermissionSet.ActivePermissionSet)
		{
			this.PermissionsChanged?.Invoke(permissionSet);
		}
	}

	private void PermissionSetOnActivePermissionSetChanged(string activePermissionSetName)
	{
		PermissionSetOnPermissionSetUpdated(PermissionSet.ActivePermissionSet);
	}

	protected override void InternalStart()
	{
		Intersect.Client.Networking.Network.PacketHandler = new PacketHandler(this, base.PacketHelper.HandlerRegistry);
		PlatformRunner = typeof(ClientContext).Assembly.CreateInstanceOf<IPlatformRunner>(Array.Empty<object>());
		PlatformRunner.Start(this, PostStartup);
	}

	internal static void DispatchUnhandledException(Exception exception, bool isTerminating = true, bool wait = false)
	{
		Thread sender = Thread.CurrentThread;
		Task task = Task.Factory.StartNew(delegate
		{
			ApplicationContext<ClientContext, ClientCommandLineOptions>.HandleUnhandledException(sender, new UnhandledExceptionEventArgs(exception, isTerminating));
		});
		if (wait)
		{
			task.Wait();
		}
	}

	protected override void Dispose(bool disposing)
	{
		base.Dispose(disposing);
		if (disposing)
		{
			base.PacketHelper.HandlerRegistry.Dispose();
		}
	}
}
