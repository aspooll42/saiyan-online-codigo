using System;
using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject(false)]
public class EnterMapPacket : IntersectPacket
{
	[Key(0)]
	public Guid MapId { get; set; }

	public EnterMapPacket()
	{
	}

	public EnterMapPacket(Guid mapId)
	{
		MapId = mapId;
	}
}
