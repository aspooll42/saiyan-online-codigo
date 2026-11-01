using System;
using System.ComponentModel.DataAnnotations.Schema;
using Intersect.Framework.Core.GameObjects.Items;
using Intersect.Models;
using Newtonsoft.Json;

namespace Intersect.GameObjects;

public class ShopItemDescriptor
{
	public Guid CostItemId { get; set; }

	public int CostItemQuantity { get; set; }

	public Guid ItemId { get; set; }

	[NotMapped]
	[JsonIgnore]
	public ItemDescriptor Item => DatabaseObject<ItemDescriptor>.Get(ItemId);

	[JsonConstructor]
	public ShopItemDescriptor(Guid itemId, Guid costItemId, int costVal)
	{
		ItemId = itemId;
		CostItemId = costItemId;
		CostItemQuantity = costVal;
	}
}
