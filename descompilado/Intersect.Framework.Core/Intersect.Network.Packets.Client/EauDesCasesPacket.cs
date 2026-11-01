using System;
using MessagePack;

namespace Intersect.Network.Packets.Client;

[MessagePackObject(false)]
public class EauDesCasesPacket : IntersectPacket
{
	[Key(0)]
	public Guid MapId { get; set; }

	[Key(1)]
	public byte[] Cases { get; set; } = Array.Empty<byte>();

	public EauDesCasesPacket()
	{
	}

	public EauDesCasesPacket(Guid mapId, byte[] cases)
	{
		MapId = mapId;
		Cases = cases;
	}
}
