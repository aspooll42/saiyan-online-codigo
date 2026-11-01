using System;
using Intersect.Framework.Core.GameObjects.Items;
using Intersect.Models;

namespace Intersect.Framework.Core.GameObjects.Crafting;

public class CraftingRecipeIngredient
{
	public Guid ItemId { get; set; }

	public int Quantity { get; set; }

	public CraftingRecipeIngredient(Guid itemId, int quantity)
	{
		ItemId = itemId;
		Quantity = quantity;
	}

	public ItemDescriptor GetItem()
	{
		return DatabaseObject<ItemDescriptor>.Get(ItemId);
	}
}
