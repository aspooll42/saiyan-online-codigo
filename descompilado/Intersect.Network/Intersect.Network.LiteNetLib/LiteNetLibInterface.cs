using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using Intersect.Core;
using Intersect.Framework.Core;
using Intersect.Framework.Reflection;
using Intersect.Memory;
using Intersect.Network.Events;
using Intersect.Network.Packets;
using LiteNetLib;
using LiteNetLib.Utils;
using Microsoft.Extensions.Logging;

namespace Intersect.Network.LiteNetLib;

public sealed class LiteNetLibInterface : INetworkLayerInterface, IDisposable, INetEventListener
{
	private sealed record InboundBuffer(IConnection Connection, IBuffer Buffer);

	private readonly ConcurrentDictionary<int, Guid> _connectionIdLookup = new ConcurrentDictionary<int, Guid>();

	private readonly NetManager _manager;

	private readonly INetwork _network;

	private readonly RSA _asymmetricServer;

	private readonly RSA _asymmetricUnconnected;

	private readonly ConcurrentQueue<InboundBuffer> _pendingInboundBuffers = new ConcurrentQueue<InboundBuffer>();

	private readonly ConcurrentDictionary<NetPeer, int> _fantomes = new ConcurrentDictionary<NetPeer, int>();

	private const int PaquetsDUnFantome = 20;

	private readonly Dictionary<string, (int Nombre, long Dit)> _illisibles = new Dictionary<string, (int, long)>();

	public event HandleConnectionEvent? OnConnected;

	public event HandleConnectionEvent? OnConnectionApproved;

	public event HandleConnectionEvent? OnConnectionDenied;

	public event HandleConnectionRequest? OnConnectionRequested;

	public event HandleConnectionEvent? OnDisconnected;

	public event HandlePacketAvailable? OnPacketAvailable;

	public event HandleUnconnectedMessage? OnUnconnectedMessage;

	private void DeconnecterLeFantome(NetPeer peer, string raison)
	{
		int num = _fantomes.AddOrUpdate(peer, 1, (NetPeer _, int n) => n + 1);
		if (num == 1 || num == 20)
		{
			ApplicationContext.Context.Value?.Logger.LogWarning($"{raison} : paquet {num} d'un pair inconnu");
		}
		if (num >= 20)
		{
			_fantomes.TryRemove(peer, out var _);
			ApplicationContext.Context.Value?.Logger.LogWarning($"Pair inconnu deconnecte apres {num} paquets : {peer}");
			peer.Disconnect();
		}
	}

	static LiteNetLibInterface()
	{
		NetDebug.Logger = new LiteNetLibLogger();
	}

	public LiteNetLibInterface(INetwork network, RSAParameters asymmetricParameters)
	{
		_network = network;
		_manager = new NetManager(this)
		{
			AllowPeerAddressChange = network.Configuration.IsServer,
			AutoRecycle = true,
			EnableStatistics = true,
			DisconnectTimeout = 60000,
			IPv6Enabled = true,
			PingInterval = 5000,
			UnconnectedMessagesEnabled = true,
			UnsyncedEvents = true,
			UnsyncedDeliveryEvent = true,
			UnsyncedReceiveEvent = true
		};
		if (HeritageDelaiDEssai.Ms > 0)
		{
			HeritageDelaiDEssai.Poser(_manager, network.Configuration.IsServer, network.Configuration.Host);
		}
		_asymmetricServer = RSA.Create(asymmetricParameters);
		_asymmetricUnconnected = RSA.Create(2048);
	}

	public void Dispose()
	{
	}

	public bool TryGetInboundBuffer(out IBuffer? buffer, out IConnection? connection)
	{
		if (!_pendingInboundBuffers.TryDequeue(out InboundBuffer result))
		{
			buffer = null;
			connection = null;
			return false;
		}
		buffer = result.Buffer;
		connection = result.Connection;
		return true;
	}

	public void ReleaseInboundBuffer(IBuffer buffer)
	{
		buffer.Dispose();
	}

	public bool SendPacket(IPacket packet, IConnection? connection = null, TransmissionMode transmissionMode = TransmissionMode.All)
	{
		if (connection == null)
		{
			connection = _network.Connections.FirstOrDefault();
		}
		if (connection != null)
		{
			return connection.Send(packet, transmissionMode);
		}
		ApplicationContext.Context.Value?.Logger.LogDebug("No active connections.");
		return false;
	}

	public bool SendPacket(IPacket packet, ICollection<IConnection> connections, TransmissionMode transmissionMode = TransmissionMode.All)
	{
		NetDataWriter data = NetDataWriter.FromBytes(packet.Data, copy: false);
		foreach (IConnection connection in connections)
		{
			if (connection is LiteNetLibConnection liteNetLibConnection)
			{
				if (!liteNetLibConnection.Send(data, transmissionMode))
				{
					return false;
				}
			}
			else if (!connection.Send(packet, transmissionMode))
			{
				return false;
			}
		}
		return true;
	}

	public void Start()
	{
		if (!_network.Configuration.IsServer)
		{
			if (!_manager.Start())
			{
				ApplicationContext.Context.Value?.Logger.LogError("Failed to make the initial connection attempt.");
			}
		}
		else if (!_manager.Start(_network.Configuration.Port))
		{
			throw new Exception($"Failed to listen on port {_network.Configuration.Port}");
		}
	}

	public void Stop(string reason = "stopping")
	{
		ApplicationContext.Context.Value?.Logger.LogDebug($"Stopping {"LiteNetLibInterface"} (\"{reason}\")...");
		byte[] bytes = Encoding.UTF8.GetBytes(reason);
		_manager.DisconnectAll(bytes, 0, bytes.Length);
	}

	public bool Connect()
	{
		if (_network.Configuration.IsServer)
		{
			throw new NotSupportedException();
		}
		if (!_manager.IsRunning && !_manager.Start())
		{
			return false;
		}
		LiteNetLibConnection liteNetLibConnection = new LiteNetLibConnection(_network, _manager, _asymmetricServer);
		if (_network.AddConnection(liteNetLibConnection))
		{
			return true;
		}
		ApplicationContext.Context.Value?.Logger.LogError("Failed to add connection to list.");
		liteNetLibConnection.Dispose();
		return false;
	}

	public void Disconnect(IConnection connection, string? message)
	{
		Disconnect(new IConnection[1] { connection }, message);
	}

	public void Disconnect(ICollection<IConnection> connections, string? message)
	{
		foreach (IConnection connection in connections)
		{
			connection.Disconnect(message);
			_network.RemoveConnection(connection);
		}
	}

	public void OnPeerConnected(NetPeer peer)
	{
		ApplicationContext.Context.Value?.Logger.LogDebug($"CONN {peer}");
		if (!(peer.Tag is Guid guid))
		{
			ApplicationContext.Context.Value?.Logger.LogTrace("Peer tag is not a guid");
		}
		else if (!_connectionIdLookup.TryAdd(peer.Id, guid))
		{
			ApplicationContext.Context.Value?.Logger.LogTrace($"Failed to add connection ID {guid} for peer {peer.Id}");
		}
		else
		{
			this.OnConnected?.Invoke(this, new ConnectionEventArgs
			{
				Connection = _network.FindConnection(guid),
				NetworkStatus = NetworkStatus.Online
			});
		}
	}

	public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
	{
		_fantomes.TryRemove(peer, out var _);
		if (disconnectInfo.Reason == DisconnectReason.Reconnect)
		{
			ApplicationContext.Context.Value?.Logger.LogDebug($"RECONNECT: {peer}");
			return;
		}
		string text = null;
		if (disconnectInfo.Reason != DisconnectReason.DisconnectPeerCalled)
		{
			try
			{
				if (!disconnectInfo.AdditionalData.EndOfData)
				{
					text = disconnectInfo.AdditionalData.GetString();
				}
				ApplicationContext.Context.Value?.Logger.LogDebug($"DCON: {peer} \"{disconnectInfo.Reason}\" ({disconnectInfo.SocketErrorCode}): {text ?? "N/A"}");
			}
			catch (Exception ex)
			{
				ApplicationContext.Context.Value?.Logger.LogDebug($"DCON[ERR]: {peer} \"{disconnectInfo.Reason}\" ({disconnectInfo.SocketErrorCode}): {ex.Message}");
			}
		}
		if (!_connectionIdLookup.TryRemove(peer.Id, out var value2))
		{
			ApplicationContext.Context.Value?.Logger.LogTrace($"No connection found for peer ID {peer.Id} ({_network.Connections.Count})");
		}
		else if (!(_network.FindConnection(value2) is LiteNetLibConnection liteNetLibConnection))
		{
			ApplicationContext.Context.Value?.Logger.LogTrace($"No connection found for {value2} ({_network.Connections.Count})");
		}
		else if (!_network.RemoveConnection(liteNetLibConnection))
		{
			ApplicationContext.Context.Value?.Logger.LogWarning($"Failed to remove connection {liteNetLibConnection.Guid}, was this already removed? ({_network.Connections.Count})");
		}
		else
		{
			Guid.TryParse(text, out var result);
			this.OnDisconnected?.Invoke(this, new ConnectionEventArgs
			{
				EventId = result,
				Connection = liteNetLibConnection,
				NetworkStatus = NetworkStatus.Offline
			});
		}
	}

	public void OnNetworkError(IPEndPoint endPoint, SocketError socketError)
	{
		ApplicationContext.Context.Value?.Logger.LogError($"NERR: {socketError} from {endPoint}");
	}

	public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channelNumber, DeliveryMethod deliveryMethod)
	{
		if (!_connectionIdLookup.TryGetValue(peer.Id, out var value))
		{
			if (!(peer.Tag is Guid guid) || !(_network.FindConnection(guid) is LiteNetLibConnection))
			{
				DeconnecterLeFantome(peer, $"RNIP: {peer} ({channelNumber}, {deliveryMethod})");
				return;
			}
			_connectionIdLookup.TryAdd(peer.Id, guid);
			value = guid;
			ApplicationContext.Context.Value?.Logger.LogWarning($"RNIP: {peer} ({channelNumber}, {deliveryMethod}) : connexion retrouvee ({guid})");
		}
		IConnection connection = _network.FindConnection(value);
		if (!(connection is LiteNetLibConnection liteNetLibConnection))
		{
			DeconnecterLeFantome(peer, $"RNCP: {peer} ({channelNumber}, {deliveryMethod}) ({connection?.Guid} / {connection?.GetFullishName()}");
			return;
		}
		if (!reader.TryGetByte(out var result))
		{
			ApplicationContext.Context.Value?.Logger.LogWarning($"No packet code from {peer} / {liteNetLibConnection.Guid}");
			return;
		}
		if (result > 1)
		{
			if (result != 32)
			{
				ApplicationContext.Context.Value?.Logger.LogWarning($"Invalid packet code from {peer} / {liteNetLibConnection.Guid}");
				return;
			}
			if (reader.GetByte() != 33)
			{
				ApplicationContext.Context.Value?.Logger.LogWarning($"Invalid packet code from {peer} / {liteNetLibConnection.Guid}");
				return;
			}
			if (reader.GetByte() != 34)
			{
				ApplicationContext.Context.Value?.Logger.LogWarning($"Invalid packet code from {peer} / {liteNetLibConnection.Guid}");
				return;
			}
			if (reader.GetByte() != 35)
			{
				ApplicationContext.Context.Value?.Logger.LogWarning($"Invalid packet code from {peer} / {liteNetLibConnection.Guid}");
				return;
			}
		}
		IBuffer buffer;
		if (result == 0)
		{
			ApprovalPacket approvalPacket = new ApprovalPacket
			{
				EncryptedData = reader.GetRemainingBytes()
			};
			if (liteNetLibConnection.TryProcessApproval(peer, approvalPacket))
			{
				this.OnConnectionApproved?.Invoke(this, new ConnectionEventArgs
				{
					Connection = liteNetLibConnection,
					NetworkStatus = NetworkStatus.Online
				});
			}
			else
			{
				ApplicationContext.Context.Value?.Logger.LogWarning($"Failed to process approval from {peer} / {liteNetLibConnection.Guid}");
			}
		}
		else if (!liteNetLibConnection.TryProcessInboundMessage(peer, reader, channelNumber, deliveryMethod, out buffer))
		{
			ApplicationContext.Context.Value?.Logger.LogWarning($"FPIM: {peer} / {liteNetLibConnection.Guid}");
		}
		else
		{
			_pendingInboundBuffers.Enqueue(new InboundBuffer(liteNetLibConnection, buffer));
			this.OnPacketAvailable?.Invoke(this);
		}
	}

	public bool SendUnconnectedPacket(IPEndPoint target, ReadOnlySpan<byte> data)
	{
		return _manager.SendUnconnectedMessage(data.ToArray(), target);
	}

	public bool SendUnconnectedPacket(IPEndPoint target, UnconnectedPacket packet)
	{
		using RSA rSA = RSA.Create(_asymmetricServer.ExportParameters(includePrivateParameters: false));
		if (!(packet is UnconnectedResponsePacket unconnectedResponsePacket))
		{
			if (packet is UnconnectedRequestPacket unconnectedRequestPacket && unconnectedRequestPacket.ResponseKey.Length == 0)
			{
				unconnectedRequestPacket.ResponseKey = _asymmetricUnconnected.ExportRSAPublicKey();
			}
		}
		else
		{
			rSA.ImportRSAPublicKey(unconnectedResponsePacket.ResponseKey, out var _);
		}
		AsymmetricEncryptedPacket asymmetricEncryptedPacket = new AsymmetricEncryptedPacket(packet, rSA.ExportParameters(includePrivateParameters: false));
		byte[] encryptedData = asymmetricEncryptedPacket.EncryptedData;
		if (encryptedData == null)
		{
			ApplicationContext.Context.Value?.Logger.LogDebug("Failed to encrypt data for " + packet.GetFullishName());
			return false;
		}
		NetDataWriter netDataWriter = new NetDataWriter(autoResize: false, encryptedData.Length + 1);
		netDataWriter.Put((byte)1);
		netDataWriter.Put(encryptedData);
		return _manager.SendUnconnectedMessage(netDataWriter, target);
	}

	private void SignalerIllisible(IPEndPoint expediteur, int octets)
	{
		string key = expediteur.ToString();
		lock (_illisibles)
		{
			long tickCount = Environment.TickCount64;
			(int, long) valueOrDefault = _illisibles.GetValueOrDefault(key);
			int num = valueOrDefault.Item1 + 1;
			if (valueOrDefault.Item2 != 0L && tickCount - valueOrDefault.Item2 < 60000)
			{
				_illisibles[key] = (num, valueOrDefault.Item2);
				return;
			}
			_illisibles[key] = (0, tickCount);
			ApplicationContext.Context.Value?.Logger.LogWarning("Paquet non connecte illisible de {Expediteur} ({Octets} octets), {Nombre} depuis la derniere fois", expediteur, octets, num);
		}
	}

	private void HandleAsymmetricEncryptedUnconnectedPacket(IPEndPoint remoteEndPoint, NetDataReader reader, UnconnectedMessageType messageType)
	{
		try
		{
			byte[] remainingBytes = reader.GetRemainingBytes();
			RSA rSA = (_network.Configuration.IsServer ? _asymmetricServer : _asymmetricUnconnected);
			AsymmetricEncryptedPacket asymmetricEncryptedPacket = new AsymmetricEncryptedPacket
			{
				EncryptedData = remainingBytes,
				Parameters = rSA.ExportParameters(includePrivateParameters: true)
			};
			IntersectPacket innerPacket = asymmetricEncryptedPacket.InnerPacket;
			HandlePacketGeneric handlerInstance;
			if (innerPacket == null)
			{
				SignalerIllisible(remoteEndPoint, remainingBytes.Length);
			}
			else if (!(innerPacket is UnconnectedPacket packet))
			{
				ApplicationContext.Context.Value?.Logger.LogDebug($"Inner packet is not of type {typeof(UnconnectedPacket)}: {innerPacket.GetFullishName()}");
			}
			else if (!_network.Helper.HandlerRegistry.TryGetHandler(innerPacket, out handlerInstance))
			{
				ApplicationContext.Context.Value?.Logger.LogWarning("No handler for " + innerPacket.GetFullishName());
			}
			else
			{
				LiteNetLibUnconnectedPacketSender packetSender = new LiteNetLibUnconnectedPacketSender(this, remoteEndPoint, _network);
				if (!handlerInstance(packetSender, packet))
				{
					ApplicationContext.Context.Value?.Logger.LogWarning("Packet handler failed for " + innerPacket.GetFullishName());
				}
			}
		}
		catch (Exception exception)
		{
			ApplicationContext.Context.Value?.Logger.LogDebug(exception, "Failure in HandleAsymmetricEncryptedUnconnectedPacket");
		}
	}

	public void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType)
	{
		try
		{
			if (reader.TryGetByte(out var result))
			{
				UnconnectedPacketType unconnectedPacketType = (UnconnectedPacketType)result;
				if (!Enum.IsDefined(typeof(UnconnectedPacketType), result))
				{
					unconnectedPacketType = UnconnectedPacketType.AsymmetricEncrypted;
				}
				switch (unconnectedPacketType)
				{
				case UnconnectedPacketType.Plaintext:
					this.OnUnconnectedMessage?.Invoke(new UnconnectedMessageSender(this, remoteEndPoint, null), new LiteNetLibInboundBuffer(reader));
					break;
				case UnconnectedPacketType.AsymmetricEncrypted:
					HandleAsymmetricEncryptedUnconnectedPacket(remoteEndPoint, reader, messageType);
					break;
				default:
					throw new UnreachableException();
				}
			}
		}
		catch (Exception exception)
		{
			ApplicationContext.Context.Value?.Logger.LogDebug(exception, "Failure in OnNetworkReceiveUnconnected");
		}
	}

	public void OnNetworkLatencyUpdate(NetPeer peer, int latency)
	{
		if (HeritageDelaiDEssai.Ms > 0)
		{
			HeritageDelaiDEssai.DireLaMesure(latency);
		}
		if (latency < 0)
		{
			return;
		}
		if (latency > 0)
		{
			ApplicationContext.CurrentContext.Logger.LogTrace("LATENCY {Peer} {Latency}ms", peer, latency);
		}
		if (!_connectionIdLookup.TryGetValue(peer.Id, out var value))
		{
			ApplicationContext.CurrentContext.Logger.LogWarning("Missing connection for {PeerId}", peer.Id);
			return;
		}
		IConnection connection = _network.FindConnection(value);
		if (!(connection is LiteNetLibConnection liteNetLibConnection))
		{
			string name = typeof(LiteNetLibConnection).GetName(qualified: true);
			string name2 = connection.GetType().GetName(qualified: true);
			ApplicationContext.CurrentContext.Logger.LogWarning("Connection is not {ExpectedType} for {PeerId}, was {ActualType}", name, peer.Id, name2);
		}
		else
		{
			liteNetLibConnection.Statistics.Ping = latency;
		}
	}

	public void OnConnectionRequest(ConnectionRequest request)
	{
		NetDataWriter netDataWriter = new NetDataWriter();
		LiteNetLibInboundBuffer buffer = new LiteNetLibInboundBuffer(request.Data);
		object obj = MessagePacker.Instance.Deserialize(buffer);
		if (!(obj is HailPacket hailPacket) || !hailPacket.Decrypt(_asymmetricServer))
		{
			ApplicationContext.Context.Value?.Logger.LogDebug($"Rejecting (bad hail) {request.RemoteEndPoint}");
			netDataWriter.Put((ushort)400);
			request.Reject(netDataWriter);
			return;
		}
		if (!SharedConstants.VersionData.SequenceEqual(hailPacket.VersionData))
		{
			ApplicationContext.Context.Value?.Logger.LogDebug($"Rejecting (bad version) {request.RemoteEndPoint}");
			netDataWriter.Put((ushort)400);
			netDataWriter.Put(NetworkStatus.VersionMismatch.ToString());
			request.Reject(netDataWriter);
			return;
		}
		ApplicationContext.Context.Value?.Logger.LogDebug($"hail Time={hailPacket.Adjusted / 10000} Offset={hailPacket.Offset / 10000} Real={hailPacket.UTC / 10000}");
		ApplicationContext.Context.Value?.Logger.LogDebug($"local Time={Timing.Global.Milliseconds} Offset={Timing.Global.MillisecondsOffset} Real={Timing.Global.MillisecondsUtc}");
		ApplicationContext.Context.Value?.Logger.LogDebug($"real delta={(Timing.Global.TicksUtc - hailPacket.UTC) / 10000}");
		if (_network.ConnectionCount >= Options.Instance.MaxClientConnections)
		{
			ApplicationContext.Context.Value?.Logger.LogDebug($"Rejecting (full) {request.RemoteEndPoint}");
			netDataWriter.Put((ushort)503);
			netDataWriter.Put(NetworkStatus.ServerFull.ToString());
			request.Reject(netDataWriter);
			return;
		}
		NetPeer netPeer = request.Accept();
		ApplicationContext.Context.Value?.Logger.LogDebug($"NCPing={netPeer.Ping}");
		byte[] bytes = RandomNumberGenerator.GetBytes(32);
		LiteNetLibConnection liteNetLibConnection = new LiteNetLibConnection(netPeer, hailPacket.RsaParameters, hailPacket.SymmetricVersion, bytes);
		HandleConnectionRequest? handleConnectionRequest = this.OnConnectionRequested;
		if (handleConnectionRequest != null && !handleConnectionRequest(this, liteNetLibConnection))
		{
			ApplicationContext.Context.Value?.Logger.LogDebug($"Rejecting (ban) {netPeer} ({liteNetLibConnection.Guid})");
			netDataWriter.Put((ushort)403);
			netDataWriter.Put(NetworkStatus.Failed.ToString());
			netPeer.Disconnect(netDataWriter);
			return;
		}
		if (!_network.AddConnection(liteNetLibConnection))
		{
			ApplicationContext.Context.Value?.Logger.LogDebug($"Rejecting (error connection map) {netPeer} ({liteNetLibConnection.Guid})");
			netDataWriter.Put((ushort)500);
			netDataWriter.Put(NetworkStatus.Failed.ToString());
			netPeer.Disconnect(netDataWriter);
			return;
		}
		if (!_connectionIdLookup.TryAdd(netPeer.Id, liteNetLibConnection.Guid))
		{
			ApplicationContext.Context.Value?.Logger.LogDebug($"Rejecting (error connection id lookup) {netPeer} ({liteNetLibConnection.Guid})");
			netDataWriter.Put((ushort)500);
			netDataWriter.Put(NetworkStatus.Failed.ToString());
			netPeer.Disconnect(netDataWriter);
			return;
		}
		ApprovalPacket approvalPacket = new ApprovalPacket(hailPacket.RsaParameters, hailPacket.HandshakeSecret, bytes, liteNetLibConnection.Guid);
		approvalPacket.Encrypt();
		byte[] encryptedData = approvalPacket.EncryptedData;
		NetDataWriter netDataWriter2 = new NetDataWriter(autoResize: false, approvalPacket.EncryptedData.Length + 1);
		netDataWriter2.Put((byte)0);
		netDataWriter2.Put(encryptedData);
		netPeer.Send(netDataWriter2, DeliveryMethod.ReliableOrdered);
		this.OnConnectionApproved?.Invoke(this, new ConnectionEventArgs
		{
			Connection = liteNetLibConnection,
			NetworkStatus = NetworkStatus.Online
		});
		this.OnConnected?.Invoke(this, new ConnectionEventArgs
		{
			Connection = liteNetLibConnection,
			NetworkStatus = NetworkStatus.Online
		});
		ApplicationContext.Context.Value?.Logger.LogDebug($"Approved {netPeer} ({liteNetLibConnection.Guid})");
	}
}
