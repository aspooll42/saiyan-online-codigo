using System;
using MessagePack;

namespace Intersect.Network.Packets.Client;

[MessagePackObject(false)]
public class TargetPacket : IntersectPacket
{
	[Key(0)]
	public Guid TargetId { get; set; }

	public TargetPacket()
	{
	}

	public TargetPacket(Guid targetId)
	{
		TargetId = targetId;
	}
}
