using System;
using Intersect.Framework.Core.GameObjects.Housing;
using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject(false)]
public class HouseInteriorPacket : IntersectPacket
{
	[Key(0)]
	public Guid HouseId { get; set; }

	[Key(1)]
	public Guid MapId { get; set; }

	[Key(2)]
	public Guid CapsuleId { get; set; }

	[Key(3)]
	public Guid OwnerId { get; set; }

	[Key(4)]
	public string OwnerName { get; set; } = string.Empty;

	[Key(5)]
	public HouseAccess Access { get; set; }

	[Key(6)]
	public bool IsOwner { get; set; }

	[Key(7)]
	public HouseFurnitureInfo[] Furniture { get; set; } = Array.Empty<HouseFurnitureInfo>();

	[Key(8)]
	public HouseVisitorInfo[] Visitors { get; set; } = Array.Empty<HouseVisitorInfo>();

	[Key(9)]
	public string TerrainName { get; set; } = string.Empty;

	[Key(10)]
	public long ResalePrice { get; set; }

	[Key(11)]
	public bool CanUpgrade { get; set; }

	[Key(12)]
	public int TerrainWidth { get; set; }

	[Key(13)]
	public int TerrainHeight { get; set; }

	[Key(14)]
	public int OffsetX { get; set; } = -1;

	[Key(15)]
	public int OffsetY { get; set; } = -1;
}
