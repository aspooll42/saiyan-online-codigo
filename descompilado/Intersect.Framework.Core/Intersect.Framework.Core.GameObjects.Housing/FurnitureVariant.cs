using System;

namespace Intersect.Framework.Core.GameObjects.Housing;

public sealed class FurnitureVariant
{
	public Guid TilesetId { get; set; }

	public int X { get; set; }

	public int Y { get; set; }

	public int Width { get; set; } = 1;

	public int Height { get; set; } = 1;

	public int FootHeight { get; set; } = 1;

	public string Label { get; set; } = string.Empty;
}
