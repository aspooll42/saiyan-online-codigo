using System;
using Intersect.Client.Framework.Items;
using Intersect.Framework.Core.GameObjects.Items;
using Newtonsoft.Json;

namespace Intersect.Client.Items;

public class MapItemInstance : Item, IMapItemInstance, IItem
{
	public Guid Id { get; set; }

	public int X { get; set; }

	public int Y { get; set; }

	[JsonIgnore]
	public int TileIndex => Y * Options.Instance.Map.MapWidth + X;

	public MapItemInstance()
	{
	}

	public MapItemInstance(int tileIndex, Guid uniqueId, Guid itemId, Guid? bagId, int quantity, ItemProperties itemProperties)
	{
		Id = uniqueId;
		X = tileIndex % Options.Instance.Map.MapWidth;
		Y = (int)Math.Floor((float)tileIndex / (float)Options.Instance.Map.MapWidth);
		base.ItemId = itemId;
		base.BagId = bagId;
		base.Quantity = quantity;
		base.ItemProperties = itemProperties;
	}
}
