using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Intersect.Core;
using Intersect.Framework.Core;
using Intersect.Framework.Reflection;
using Intersect.Memory;
using Intersect.Plugins.Interfaces;
using Microsoft.Extensions.Logging;

namespace Intersect.Network;

public abstract class AbstractNetwork : INetwork, IDisposable
{
	private record struct PendingUnconnectedMessage(Guid MessageId, HandleUnconnectedMessage ResponseHandler);

	private bool mDisposed;

	private readonly object mDisposeLock;

	private readonly List<INetworkLayerInterface> mNetworkLayerInterfaces;

	private readonly PriorityQueue<PendingUnconnectedMessage, long> _pendingUnconnectedCallbacks = new PriorityQueue<PendingUnconnectedMessage, long>();

	public IApplicationContext ApplicationContext { get; }

	public IPacketHelper Helper { get; }

	public abstract bool IsConnected { get; }

	public ICollection<IConnection> Connections => ConnectionLookup.Values;

	public IDictionary<Guid, IConnection> ConnectionLookup { get; }

	public HandlePacket Handler { get; set; }

	public ShouldProcessPacket PreProcessHandler { get; set; }

	public int ConnectionCount => Connections.Count;

	public NetworkConfiguration Configuration { get; }

	public Guid Id { get; protected set; }

	public abstract event HandleConnectionEvent OnConnected;

	public abstract event HandleConnectionEvent OnConnectionApproved;

	public abstract event HandleConnectionEvent OnConnectionDenied;

	public abstract event HandleConnectionRequest OnConnectionRequested;

	public abstract event HandleConnectionEvent OnDisconnected;

	public abstract event HandlePacketAvailable OnPacketAvailable;

	public abstract event HandleUnconnectedMessage OnUnconnectedMessage;

	protected AbstractNetwork(IApplicationContext applicationContext, NetworkConfiguration configuration)
	{
		mDisposed = false;
		mDisposeLock = new object();
		ApplicationContext = applicationContext ?? throw new ArgumentNullException("applicationContext");
		Helper = applicationContext.PacketHelper ?? throw new ArgumentException("PacketHelper was null.", "applicationContext");
		mNetworkLayerInterfaces = new List<INetworkLayerInterface>();
		ConnectionLookup = new ConcurrentDictionary<Guid, IConnection>();
		if (configuration.Host?.ToLower() == "localhost")
		{
			configuration.Host = "127.0.0.1";
		}
		Configuration = configuration;
	}

	public bool AddConnection(IConnection connection)
	{
		if (ConnectionLookup.ContainsKey(connection.Guid))
		{
			return false;
		}
		ConnectionLookup.Add(connection.Guid, connection);
		return true;
	}

	public bool RemoveConnection(IConnection connection)
	{
		if (!connection.IsConnected)
		{
			return ConnectionLookup.Remove(connection.Guid);
		}
		return false;
	}

	public void Dispose()
	{
		lock (mDisposeLock)
		{
			if (mDisposed)
			{
				return;
			}
			mDisposed = true;
		}
		if (!Disconnect(NetworkStatus.Quitting.ToString()))
		{
			ApplicationContext.Logger.LogError("Error disconnecting while disposing.");
		}
		mNetworkLayerInterfaces?.ForEach(delegate(INetworkLayerInterface networkLayerInterface)
		{
			networkLayerInterface?.Dispose();
		});
		ConnectionLookup.Clear();
	}

	public abstract void Close();

	public bool Disconnect(string message = "")
	{
		return Disconnect(Connections, message);
	}

	public bool Disconnect(Guid guid, string message = "")
	{
		return Disconnect(FindConnection(guid), message);
	}

	public bool Disconnect(IConnection connection, string message = "")
	{
		return Disconnect(new IConnection[1] { connection }, message);
	}

	public bool Disconnect(ICollection<Guid> guids, string message = "")
	{
		return Disconnect(FindConnections(guids), message);
	}

	public bool Disconnect(ICollection<IConnection> connections, string message = "")
	{
		mNetworkLayerInterfaces?.ForEach(delegate(INetworkLayerInterface networkLayerInterface)
		{
			networkLayerInterface?.Disconnect(connections, message);
		});
		return true;
	}

	public bool SendUnconnected(IPEndPoint endPoint, UnconnectedPacket packet, HandleUnconnectedMessage? responseCallback = null, long timeout = 10000L)
	{
		if (!SendUnconnected(endPoint, packet))
		{
			return false;
		}
		if (responseCallback != null)
		{
			_pendingUnconnectedCallbacks.Enqueue(new PendingUnconnectedMessage(packet.MessageId, responseCallback), timeout);
		}
		return true;
	}

	protected abstract bool SendUnconnected(IPEndPoint endPoint, UnconnectedPacket packet);

	public void Update()
	{
		long millisecondsUtc = Timing.Global.MillisecondsUtc;
		PendingUnconnectedMessage element;
		long priority;
		long priority2;
		while (_pendingUnconnectedCallbacks.TryPeek(out element, out priority) && priority < millisecondsUtc && _pendingUnconnectedCallbacks.TryDequeue(out element, out priority2))
		{
		}
	}

	public abstract bool Send(IPacket packet, TransmissionMode mode = TransmissionMode.All);

	public bool Send(Guid guid, IPacket packet, TransmissionMode mode = TransmissionMode.All)
	{
		IConnection connection = FindConnection(guid);
		if (connection != null)
		{
			return Send(connection, packet, mode);
		}
		return false;
	}

	public abstract bool Send(IConnection connection, IPacket packet, TransmissionMode mode = TransmissionMode.All);

	public bool Send(ICollection<Guid> guids, IPacket packet, TransmissionMode mode = TransmissionMode.All)
	{
		return Send(FindConnections(guids), packet, mode);
	}

	public abstract bool Send(ICollection<IConnection> connections, IPacket packet, TransmissionMode mode = TransmissionMode.All);

	public IConnection FindConnection(Guid guid)
	{
		if (ConnectionLookup.TryGetValue(guid, out var value))
		{
			return value;
		}
		ApplicationContext.Logger.LogTrace($"Could not find connection {guid}.");
		return null;
	}

	public TConnection FindConnection<TConnection>(Guid guid) where TConnection : class, IConnection
	{
		return FindConnection(guid) as TConnection;
	}

	public TConnection FindConnection<TConnection>(Func<TConnection, bool> selector) where TConnection : class, IConnection
	{
		return FindConnections<TConnection>().FirstOrDefault(selector);
	}

	public ICollection<IConnection> FindConnections(ICollection<Guid> guids)
	{
		return (from connection in guids.Select(FindConnection)
			where connection != null
			select connection).ToList();
	}

	public ICollection<TConnection> FindConnections<TConnection>() where TConnection : class, IConnection
	{
		return Connections.OfType<TConnection>().ToList();
	}

	protected void AddNetworkLayerInterface(INetworkLayerInterface networkLayerInterface)
	{
		if (mNetworkLayerInterfaces == null)
		{
			throw new ArgumentNullException("mNetworkLayerInterfaces");
		}
		if (networkLayerInterface == null)
		{
			throw new ArgumentNullException("networkLayerInterface");
		}
		networkLayerInterface.OnPacketAvailable += HandleInboundMessageAvailable;
		mNetworkLayerInterfaces.Add(networkLayerInterface);
	}

	protected bool RunForInterface<TNetworkLayerInterface>(Func<TNetworkLayerInterface, bool> action)
	{
		if (action != null)
		{
			return mNetworkLayerInterfaces.OfType<TNetworkLayerInterface>().Any(action);
		}
		ApplicationContext.Logger.LogError(new ArgumentNullException("action"), "Failed to run action on {NetworkLayerInterfaceType}", typeof(TNetworkLayerInterface).GetName(qualified: true));
		return false;
	}

	private void HandleInboundMessageAvailable(INetworkLayerInterface sender)
	{
		if (sender == null)
		{
			throw new ArgumentNullException("sender");
		}
		if (sender.TryGetInboundBuffer(out var buffer, out var connection))
		{
			HandleInboundData(buffer, connection);
			sender.ReleaseInboundBuffer(buffer);
		}
	}

	protected virtual void HandleConnectionApproved(IConnection connection)
	{
	}

	protected virtual void HandleConnected(IConnection connection)
	{
		connection?.HandleConnected();
	}

	protected virtual void HandleDisconnected(IConnection connection)
	{
		connection?.HandleDisconnected();
	}

	private void HandleInboundData(IBuffer buffer, IConnection connection)
	{
		if (buffer == null || buffer.Length < 1 || (PreProcessHandler != null && !PreProcessHandler(connection, buffer.Length)))
		{
			return;
		}
		byte[] packetData = buffer.ToBytes();
		IPacket packet = (IPacket)MessagePacker.Instance.Deserialize(packetData);
		if (packet != null)
		{
			if (Handler != null)
			{
				Handler(connection, packet);
				return;
			}
			ApplicationContext.Logger.LogError("Handler == null, this shouldn't happen! Tell JC");
			Disconnect(connection, NetworkStatus.Unknown.ToString());
		}
		else
		{
			Disconnect(connection, NetworkStatus.VersionMismatch.ToString());
		}
	}

	protected abstract IDictionary<TKey, TValue> CreateDictionaryLegacy<TKey, TValue>();

	protected void StartInterfaces()
	{
		mNetworkLayerInterfaces?.ForEach(delegate(INetworkLayerInterface networkLayerInterface)
		{
			networkLayerInterface?.Start();
		});
	}

	protected void SendPacket(IPacket packet, IConnection connection, TransmissionMode transmissionMode = TransmissionMode.All)
	{
		mNetworkLayerInterfaces?.ForEach(delegate(INetworkLayerInterface networkLayerInterface)
		{
			networkLayerInterface?.SendPacket(packet, connection, transmissionMode);
		});
	}

	protected void SendPacket(IPacket packet, ICollection<IConnection> connections, TransmissionMode transmissionMode = TransmissionMode.All)
	{
		mNetworkLayerInterfaces?.ForEach(delegate(INetworkLayerInterface networkLayerInterface)
		{
			networkLayerInterface?.SendPacket(packet, connections, transmissionMode);
		});
	}

	protected void StopInterfaces(string reason = "stopping")
	{
		mNetworkLayerInterfaces?.ForEach(delegate(INetworkLayerInterface networkLayerInterface)
		{
			networkLayerInterface?.Stop(reason);
		});
	}
}
