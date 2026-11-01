using System;
using MessagePack;

namespace Intersect.Framework.Core.GameObjects.Housing;

[MessagePackObject(false)]
public sealed class HouseVisitorInfo
{
	[Key(0)]
	public Guid Id { get; set; }

	[Key(1)]
	public string Name { get; set; } = string.Empty;

	[Key(2)]
	public HouseVisitorRelation Relation { get; set; }

	[Key(3)]
	public bool Female { get; set; }

	[Key(4)]
	public long SecondsInside { get; set; }
}
