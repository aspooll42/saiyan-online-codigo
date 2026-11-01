using System;
using MessagePack;

namespace Intersect.Network.Packets.Client;

[MessagePackObject(false)]
public class HouseFurnitureUsePacket : IntersectPacket
{
	[Key(0)]
	public Guid FurnitureId { get; set; }

	public HouseFurnitureUsePacket()
	{
	}

	public HouseFurnitureUsePacket(Guid furnitureId)
	{
		FurnitureId = furnitureId;
	}
}
