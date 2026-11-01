using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using Intersect.Core;
using Intersect.Framework.Reflection;
using Intersect.Memory;
using Intersect.Network.Packets;
using LiteNetLib;
using LiteNetLib.Utils;
using Microsoft.Extensions.Logging;

namespace Intersect.Network.LiteNetLib;

public sealed class LiteNetLibConnection : AbstractConnection
{
	private readonly RSA _asymmetric;

	private readonly NetPeer _peer;

	private readonly ISymmetricAlgorithm _symmetric;

	public override string Ip => _peer.Address.ToString();

	public override int Port => _peer.Port;

	internal LiteNetLibConnection(NetPeer peer, RSAParameters asymmetricParameters, byte symmetricVersion, ReadOnlySpan<byte> symmetricKey)
	{
		if (symmetricKey == default(ReadOnlySpan<byte>) || symmetricKey.Length != 32)
		{
			throw new ArgumentException($"Expected a 256-bit key but was {symmetricKey.Length << 3} bits.", "symmetricKey");
		}
		_asymmetric = RSA.Create(asymmetricParameters);
		_peer = peer;
		_peer.Tag = base.Guid;
		_symmetric = SymmetricAlgorithm.PickForVersion(symmetricVersion, symmetricKey);
		ApplicationContext.Context.Value?.Logger.LogDebug($"Created {_symmetric.GetFullishName()} for {_peer} ({base.Guid})");
	}

	internal LiteNetLibConnection(INetwork network, NetManager manager, RSA interfaceAsymmetric)
	{
		_asymmetric = RSA.Create(2048);
		byte[] bytes = RandomNumberGenerator.GetBytes(32);
		RSAParameters rsaParameters = _asymmetric.ExportParameters(includePrivateParameters: false);
		HailPacket hailPacket = new HailPacket(interfaceAsymmetric, bytes, SharedConstants.VersionData, rsaParameters);
		hailPacket.Encrypt();
		NetDataWriter connectionData = NetDataWriter.FromBytes(hailPacket.Data, copy: false);
		ApplicationContext.Context.Value?.Logger.LogInformation($"Connecting to {network.Configuration.Host}:{network.Configuration.Port}...");
		_peer = manager.Connect(network.Configuration.Host, network.Configuration.Port, connectionData);
		_peer.Tag = base.Guid;
		_symmetric = SymmetricAlgorithm.PickForPlatform();
		ApplicationContext.Context.Value?.Logger.LogDebug($"Created {_symmetric.GetFullishName()} for {_peer} ({base.Guid})");
	}

	internal bool TryProcessApproval(NetPeer peer, ApprovalPacket approvalPacket)
	{
		if (approvalPacket.Decrypt(_asymmetric))
		{
			return _symmetric.SetKey(approvalPacket.AesKey);
		}
		ApplicationContext.Context.Value?.Logger.LogError("Failed to decrypt approval response packet.");
		return false;
	}

	internal bool TryProcessInboundMessage(NetPeer peer, NetPacketReader reader, byte channelNumber, DeliveryMethod deliveryMethod, [NotNullWhen(true)] out IBuffer? buffer)
	{
		buffer = null;
		reader.Get(out int result);
		byte[] array = new byte[result];
		reader.GetBytes(array, result);
		if (array == null)
		{
			return false;
		}
		ReadOnlySpan<byte> plaindata;
		EncryptionResult encryptionResult = _symmetric.TryDecrypt(array, out plaindata);
		switch (encryptionResult)
		{
		case EncryptionResult.NoHeader:
		case EncryptionResult.InvalidVersion:
		case EncryptionResult.InvalidNonce:
		case EncryptionResult.InvalidTag:
		case EncryptionResult.EmptyInput:
		case EncryptionResult.SizeMismatch:
			ApplicationContext.Context.Value?.Logger.LogWarning($"SDER: {base.Guid} {encryptionResult}");
			return false;
		case EncryptionResult.Error:
			ApplicationContext.Context.Value?.Logger.LogWarning($"SDER: {base.Guid} {encryptionResult}");
			return false;
		default:
			throw new UnreachableException();
		case EncryptionResult.Success:
			buffer = new MemoryBuffer(plaindata.ToArray());
			return true;
		}
	}

	public override bool Send(IPacket packet, TransmissionMode transmissionMode = TransmissionMode.All)
	{
		byte[] data = packet.Data;
		ReadOnlySpan<byte> cipherdata;
		EncryptionResult encryptionResult = _symmetric.TryEncrypt(data, out cipherdata);
		switch (encryptionResult)
		{
		case EncryptionResult.NoHeader:
		case EncryptionResult.InvalidVersion:
		case EncryptionResult.InvalidNonce:
		case EncryptionResult.InvalidTag:
		case EncryptionResult.EmptyInput:
		case EncryptionResult.SizeMismatch:
		case EncryptionResult.Error:
			ApplicationContext.Context.Value?.Logger.LogWarning($"SEER: {base.Guid} {encryptionResult}");
			return false;
		default:
			throw new UnreachableException();
		case EncryptionResult.Success:
		{
			NetDataWriter netDataWriter = new NetDataWriter(autoResize: true, cipherdata.Length + 1);
			netDataWriter.Put((byte)32);
			netDataWriter.Put((byte)33);
			netDataWriter.Put((byte)34);
			netDataWriter.Put((byte)35);
			netDataWriter.Put(cipherdata.Length);
			netDataWriter.Put(cipherdata);
			return Send(netDataWriter, transmissionMode);
		}
		}
	}

	public bool Send(NetDataWriter data, TransmissionMode transmissionMode = TransmissionMode.All)
	{
		try
		{
			_peer.Send(data, transmissionMode.ToDeliveryMethod());
			return true;
		}
		catch (Exception exception)
		{
			ApplicationContext.Context.Value?.Logger.LogDebug(exception, "Failed to send data to {Endpoint}", _peer);
			return false;
		}
	}

	public override void Disconnect(string? message = null)
	{
		NetDataWriter netDataWriter = new NetDataWriter();
		netDataWriter.Put(string.IsNullOrEmpty(message) ? string.Empty : message);
		_peer.Disconnect(netDataWriter.CopyData());
	}

	public override void Dispose()
	{
		_peer.Disconnect();
		base.Dispose();
	}
}
