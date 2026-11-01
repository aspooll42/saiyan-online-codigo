using System;
using MessagePack;

namespace Intersect.Framework.Core.GameObjects.Housing;

[MessagePackObject(false)]
public sealed class HouseFurnitureInfo
{
	[Key(0)]
	public Guid Id { get; set; }

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
}
