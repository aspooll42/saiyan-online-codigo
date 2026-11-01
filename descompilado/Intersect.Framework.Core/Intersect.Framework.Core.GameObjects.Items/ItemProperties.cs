using System;
using Intersect.Enums;
using MessagePack;

namespace Intersect.Framework.Core.GameObjects.Items;

[MessagePackObject(false)]
public class ItemProperties
{
	[Key(0)]
	public int[] StatModifiers { get; set; } = new int[Enum.GetValues<Stat>().Length];

	public ItemProperties()
	{
	}

	public ItemProperties(ItemProperties other)
	{
		if (other == null)
		{
			throw new ArgumentNullException("other");
		}
		Array.Copy(other.StatModifiers, StatModifiers, Enum.GetValues<Stat>().Length);
	}
}
