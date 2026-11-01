using System;
using Newtonsoft.Json;

namespace Intersect.Framework.Core.GameObjects.Maps;

public struct Tile
{
	public Guid TilesetId;

	public int X;

	public int Y;

	public byte Autotile;

	[JsonIgnore]
	public object TilesetTexture;
}
