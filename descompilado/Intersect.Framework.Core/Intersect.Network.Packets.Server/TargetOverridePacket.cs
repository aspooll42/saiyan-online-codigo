using System;
using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject(false)]
public class TargetOverridePacket : IntersectPacket
{
	[Key(0)]
	public Guid TargetId { get; set; }

	public TargetOverridePacket()
	{
	}

	public TargetOverridePacket(Guid targetId)
	{
		TargetId = targetId;
	}
}
