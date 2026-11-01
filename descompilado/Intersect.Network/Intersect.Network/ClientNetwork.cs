using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Security.Cryptography;
using Intersect.Core;
using Intersect.Framework.Core.Security;
using Intersect.Network.Events;
using Intersect.Network.LiteNetLib;
using Microsoft.Extensions.Logging;

namespace Intersect.Network;

public class ClientNetwork : AbstractNetwork, IClient, INetwork, IDisposable
{
	public static readonly HashSet<string> UnresolvableHostNames = new HashSet<string>();

	private static readonly NetworkLayerInterfaceFactory DefaultNetworkLayerInterfaceFactory = (INetwork network, RSAParameters parameters) => new LiteNetLibInterface(network, parameters);

	private readonly INetworkLayerInterface _interface;

	private readonly Ping _ping = new Ping();

	private bool _isConnected;

	internal static NetworkLayerInterfaceFactory? NetworkLayerInterfaceFactory { get; set; }

	public IConnection? Connection => base.Connections.FirstOrDefault();

	public override bool IsConnected => _isConnected;

	public bool IsServerOnline => IsConnected;

	public int Ping
	{
		get
		{
			IConnection connection = Connection;
			if (connection != null)
			{
				return (int)connection.Statistics.Ping;
			}
			string host = base.Configuration.Host;
			if (host == null || UnresolvableHostNames.Contains(host))
			{
				return -2;
			}
			return -1;
		}
	}

	public override event HandleConnectionEvent? OnConnected;

	public override event HandleConnectionEvent? OnConnectionApproved;

	public override event HandleConnectionEvent? OnConnectionDenied;

	public override event HandleConnectionRequest? OnConnectionRequested;

	public override event HandleConnectionEvent? OnDisconnected;

	public override event HandlePacketAvailable? OnPacketAvailable;

	public override event HandleUnconnectedMessage? OnUnconnectedMessage;

	public ClientNetwork(IApplicationContext applicationContext, NetworkConfiguration configuration, RSAParameters rsaParameters)
		: base(applicationContext, configuration)
	{
		base.Id = Guid.Empty;
		_isConnected = false;
		_interface = (NetworkLayerInterfaceFactory ?? DefaultNetworkLayerInterfaceFactory)(this, rsaParameters);
		_interface.OnConnected += HandleInterfaceOnConnected;
		_interface.OnConnectionApproved += HandleInterfaceOnConnectonApproved;
		_interface.OnConnectionDenied += HandleInterfaceOnConnectonDenied;
		_interface.OnDisconnected += HandleInterfaceOnDisconnected;
		AddNetworkLayerInterface(_interface);
		StartInterfaces();
	}

	public bool Connect()
	{
		if (!IsConnected)
		{
			return _interface.Connect();
		}
		return true;
	}

	protected override bool SendUnconnected(IPEndPoint endPoint, UnconnectedPacket packet)
	{
		return _interface.SendUnconnectedPacket(endPoint, packet);
	}

	public override bool Send(IPacket packet, TransmissionMode mode = TransmissionMode.All)
	{
		return _interface?.SendPacket(packet, (IConnection)null, mode) ?? false;
	}

	public override bool Send(IConnection connection, IPacket packet, TransmissionMode mode = TransmissionMode.All)
	{
		return Send(packet, mode);
	}

	public override bool Send(ICollection<IConnection> connections, IPacket packet, TransmissionMode mode = TransmissionMode.All)
	{
		return Send(packet, mode);
	}

	protected virtual void HandleInterfaceOnConnected(INetworkLayerInterface sender, ConnectionEventArgs connectionEventArgs)
	{
		base.ApplicationContext.Logger.LogInformation($"Connected [{connectionEventArgs.Connection?.Guid}].");
		_isConnected = true;
		OnConnected?.Invoke(sender, connectionEventArgs);
	}

	protected virtual void HandleInterfaceOnConnectonApproved(INetworkLayerInterface sender, ConnectionEventArgs connectionEventArgs)
	{
		base.ApplicationContext.Logger.LogInformation($"Connection approved [{connectionEventArgs.Connection?.Guid}].");
		OnConnectionApproved?.Invoke(sender, connectionEventArgs);
	}

	protected virtual void HandleInterfaceOnConnectonDenied(INetworkLayerInterface sender, ConnectionEventArgs connectionEventArgs)
	{
		base.ApplicationContext.Logger.LogInformation($"Connection denied [{connectionEventArgs.Connection?.Guid}].");
		OnConnectionDenied?.Invoke(sender, connectionEventArgs);
	}

	protected virtual void HandleInterfaceOnDisconnected(INetworkLayerInterface sender, ConnectionEventArgs connectionEventArgs)
	{
		base.ApplicationContext.Logger.LogInformation($"Disconnected [{connectionEventArgs.Connection?.Guid ?? Guid.Empty}].");
		_isConnected = false;
		OnDisconnected?.Invoke(sender, connectionEventArgs);
		PermissionSet.ActivePermissionSet = PermissionSet.Default;
	}

	public override void Close()
	{
		StopInterfaces("closing");
	}

	internal void AssignId(Guid id)
	{
		base.Id = id;
	}

	protected override IDictionary<TKey, TValue> CreateDictionaryLegacy<TKey, TValue>()
	{
		return new ConcurrentDictionary<TKey, TValue>();
	}
}
