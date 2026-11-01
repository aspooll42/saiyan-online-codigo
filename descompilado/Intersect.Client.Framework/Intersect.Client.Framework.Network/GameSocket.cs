using Intersect.Network;
using Intersect.Network.Events;

namespace Intersect.Client.Framework.Network;

public abstract class GameSocket
{
	public abstract bool IsConnected { get; }

	public abstract int Ping { get; }

	public abstract INetwork Network { get; }

	public event DataReceivedHandler DataReceived;

	public event ConnectedHandler Connected;

	public event ConnectionFailedHandler ConnectionFailed;

	public event DisconnectedHandler Disconnected;

	public abstract void Connect(string host, int port);

	public abstract void SendPacket(object packet);

	public virtual void SendPacket(object packet, TransmissionMode mode)
	{
		SendPacket(packet);
	}

	public abstract void Update();

	public abstract void Disconnect(string reason);

	public abstract void Dispose();

	protected void OnDataReceived(IPacket packet)
	{
		this.DataReceived?.Invoke(packet);
	}

	protected void OnConnected(INetworkLayerInterface sender, ConnectionEventArgs connectionEventArgs)
	{
		this.Connected?.Invoke(sender, connectionEventArgs);
	}

	protected void OnConnectionFailed(INetworkLayerInterface sender, ConnectionEventArgs connectionEventArgs, bool denied)
	{
		this.ConnectionFailed?.Invoke(sender, connectionEventArgs, denied);
	}

	protected void OnDisconnected(INetworkLayerInterface sender, ConnectionEventArgs connectionEventArgs)
	{
		this.Disconnected?.Invoke(sender, connectionEventArgs);
	}
}
