using System;
using MessagePack;

namespace Intersect.Framework.Core.GameObjects.Dungeons;

[MessagePackObject(false)]
public sealed class DungeonLootInfo
{
	[Key(0)]
	public Guid ItemId { get; set; }

	[Key(1)]
	public int Quantity { get; set; }
}
