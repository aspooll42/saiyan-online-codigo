using System;
using MessagePack;

namespace Intersect.Network.Packets.Client;

[MessagePackObject(false)]
public class UseItemPacket : IntersectPacket
{
	[Key(0)]
	public int Slot { get; set; }

	[Key(1)]
	public Guid TargetId { get; set; }

	public UseItemPacket()
	{
	}

	public UseItemPacket(int slot, Guid targetId)
	{
		Slot = slot;
		TargetId = targetId;
	}
}
