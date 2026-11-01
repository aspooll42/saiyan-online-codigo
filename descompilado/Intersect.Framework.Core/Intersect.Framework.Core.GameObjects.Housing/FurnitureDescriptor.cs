using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Newtonsoft.Json;

namespace Intersect.Framework.Core.GameObjects.Housing;

public sealed class FurnitureDescriptor
{
	public FurniturePlacement Placement { get; set; }

	public FurnitureCategory Category { get; set; }

	[JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace)]
	public List<FurnitureVariant> Variants { get; set; }

	public bool NoMirror { get; set; }

	public string NoMirrorReason { get; set; }

	public FurnitureEffect Effect { get; set; }

	public int ChestSlots { get; set; }

	public FurnitureVariant VariantAt(int index)
	{
		if (Variants == null || Variants.Count == 0)
		{
			int num = 1;
			List<FurnitureVariant> list = new List<FurnitureVariant>(num);
			CollectionsMarshal.SetCount(list, num);
			Span<FurnitureVariant> span = CollectionsMarshal.AsSpan(list);
			int num2 = 0;
			span[num2] = new FurnitureVariant();
			num2++;
			Variants = list;
		}
		return Variants[(index % Variants.Count + Variants.Count) % Variants.Count];
	}

	public FurnitureDescriptor()
	{
		int num = 1;
		List<FurnitureVariant> list = new List<FurnitureVariant>(num);
		CollectionsMarshal.SetCount(list, num);
		Span<FurnitureVariant> span = CollectionsMarshal.AsSpan(list);
		int num2 = 0;
		span[num2] = new FurnitureVariant();
		num2++;
		Variants = list;
		NoMirrorReason = string.Empty;
		ChestSlots = 20;
	}
}
