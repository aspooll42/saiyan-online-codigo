using System;
using MessagePack;

namespace Intersect.Network;

public abstract class UnconnectedRequestPacket : UnconnectedPacket
{
	[Key(1)]
	public byte[] ResponseKey { get; set; } = Array.Empty<byte>();

	protected UnconnectedRequestPacket()
	{
	}

	protected UnconnectedRequestPacket(byte[] responseKey)
	{
		ResponseKey = responseKey;
	}
}
