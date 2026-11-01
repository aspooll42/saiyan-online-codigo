using System;
using MessagePack;

namespace Intersect.Network.Packets.Client;

[MessagePackObject(false)]
public class HouseFurniturePlacePacket : IntersectPacket
{
	[Key(0)]
	public Guid FurnitureId { get; set; }

	[Key(1)]
	public Guid ItemId { get; set; }

	[Key(2)]
	public int X { get; set; }

	[Key(3)]
	public int Y { get; set; }

	[Key(4)]
	public int Variant { get; set; }

	[Key(5)]
	public bool Mirror { get; set; }

	public HouseFurniturePlacePacket()
	{
	}

	public HouseFurniturePlacePacket(Guid furnitureId, Guid itemId, int x, int y, int variant, bool mirror)
	{
		FurnitureId = furnitureId;
		ItemId = itemId;
		X = x;
		Y = y;
		Variant = variant;
		Mirror = mirror;
	}
}
