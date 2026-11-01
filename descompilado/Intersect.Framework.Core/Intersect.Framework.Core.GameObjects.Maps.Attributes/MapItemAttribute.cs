using System;
using Intersect.Framework.Core.GameObjects.Items;
using Intersect.GameObjects.Annotations;

namespace Intersect.Framework.Core.GameObjects.Maps.Attributes;

public class MapItemAttribute : MapAttribute
{
	public override MapAttributeType Type => MapAttributeType.Item;

	[EditorLabel("Attributes", "Item")]
	[EditorReference(typeof(ItemDescriptor), "Name")]
	public Guid ItemId { get; set; }

	[EditorLabel("Attributes", "Quantity")]
	[EditorDisplay]
	public int Quantity { get; set; }

	public long RespawnTime { get; set; }

	public override MapAttribute Clone()
	{
		MapItemAttribute mapItemAttribute = (MapItemAttribute)base.Clone();
		mapItemAttribute.ItemId = ItemId;
		mapItemAttribute.Quantity = Quantity;
		mapItemAttribute.RespawnTime = RespawnTime;
		return mapItemAttribute;
	}
}
