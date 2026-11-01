using System;
using System.Linq;
using Intersect.Client.Framework.Items;
using Intersect.Framework.Core.GameObjects.Items;
using Intersect.Models;

namespace Intersect.Client.Items;

public class Item : IItem
{
	public Guid? BagId { get; set; }

	public Guid ItemId { get; set; }

	public int Quantity { get; set; }

	public ItemProperties ItemProperties { get; set; }

	public ItemDescriptor Descriptor => DatabaseObject<ItemDescriptor>.Get(ItemId);

	public void Load(Guid id, int quantity, Guid? bagId, ItemProperties itemProperties)
	{
		ItemId = id;
		Quantity = quantity;
		BagId = bagId;
		ItemProperties = itemProperties;
	}

	public static int FindQuantityOfItem<TItem>(Guid itemDescriptorId, TItem?[] slots) where TItem : IItem
	{
		return slots.Where((TItem slot) => slot != null && slot.ItemId == itemDescriptorId).Aggregate(0, (int totalQuantity, TItem slot) => totalQuantity + slot.Quantity);
	}

	public static int FindFirstPartialSlot<TItem>(Guid itemDescriptorId, TItem?[] slots, int maximumStack) where TItem : IItem
	{
		for (int i = 0; i < slots.Length; i++)
		{
			TItem val = slots[i];
			if (val != null && !(val.ItemId != itemDescriptorId) && val.Quantity < maximumStack)
			{
				return i;
			}
		}
		return -1;
	}

	public static int FindSpaceForItem<TItem>(Guid itemDescriptorId, ItemType itemType, int maximumStack, int slotHint, int searchQuantity, TItem?[] slots) where TItem : IItem
	{
		int num = 0;
		int num2 = 0;
		while (num < searchQuantity && num2 < slots.Length)
		{
			TItem val = slots[(num2 + Math.Max(0, slotHint)) % slots.Length];
			if (val == null || val.ItemId == default(Guid))
			{
				num += maximumStack;
			}
			else if (itemType != ItemType.Equipment && val.ItemId == itemDescriptorId)
			{
				num += maximumStack - val.Quantity;
			}
			num2++;
		}
		return Math.Min(num, searchQuantity);
	}
}
