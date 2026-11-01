using System;
using MessagePack;

namespace Intersect.Framework.Core.GameObjects.Housing;

[MessagePackObject(false)]
public sealed class HouseStateInfo
{
	[Key(0)]
	public Guid HouseId { get; set; }

	[Key(1)]
	public Guid MapId { get; set; }

	[Key(2)]
	public Guid TerrainId { get; set; }

	[Key(3)]
	public Guid CapsuleId { get; set; }

	[Key(4)]
	public Guid OwnerId { get; set; }

	[Key(5)]
	public string OwnerName { get; set; } = string.Empty;

	[Key(6)]
	public HouseAccess Access { get; set; }

	[Key(7)]
	public bool MayEnter { get; set; }

	[Key(8)]
	public int OffsetX { get; set; } = -1;

	[Key(9)]
	public int OffsetY { get; set; } = -1;
}
