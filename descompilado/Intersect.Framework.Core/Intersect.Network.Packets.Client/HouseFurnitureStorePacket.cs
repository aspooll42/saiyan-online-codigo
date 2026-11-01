using System;
using MessagePack;

namespace Intersect.Network.Packets.Client;

[MessagePackObject(false)]
public class HouseFurnitureStorePacket : IntersectPacket
{
	[Key(0)]
	public Guid FurnitureId { get; set; }

	public HouseFurnitureStorePacket()
	{
	}

	public HouseFurnitureStorePacket(Guid furnitureId)
	{
		FurnitureId = furnitureId;
	}
}
