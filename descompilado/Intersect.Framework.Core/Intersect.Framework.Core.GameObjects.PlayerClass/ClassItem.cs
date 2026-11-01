using System;
using Intersect.Framework.Core.GameObjects.Items;
using Intersect.Models;
using Newtonsoft.Json;

namespace Intersect.Framework.Core.GameObjects.PlayerClass;

public class ClassItem
{
	[JsonProperty]
	public Guid Id { get; set; }

	public int Quantity { get; set; }

	public ItemDescriptor Get()
	{
		return DatabaseObject<ItemDescriptor>.Get(Id);
	}
}
