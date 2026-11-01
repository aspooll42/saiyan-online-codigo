using System;
using MessagePack;

namespace Intersect.Network.Packets.Client;

[MessagePackObject(false)]
public class CraftItemPacket : IntersectPacket
{
	[Key(0)]
	public Guid CraftId { get; set; }

	[Key(1)]
	public int Count { get; set; }

	public CraftItemPacket()
	{
	}

	public CraftItemPacket(Guid craftId, int count)
	{
		CraftId = craftId;
		Count = count;
	}
}
