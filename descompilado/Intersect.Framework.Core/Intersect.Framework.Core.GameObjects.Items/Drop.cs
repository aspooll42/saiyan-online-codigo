using System;
using Newtonsoft.Json;

namespace Intersect.Framework.Core.GameObjects.Items;

public class Drop
{
	private int _maxQuantity;

	public double Chance { get; set; }

	public bool Hunting { get; set; }

	public Guid ItemId { get; set; }

	[JsonProperty]
	[Obsolete("Use MinQuantity instead, the Quantity property will be removed in 0.9-beta", true)]
	private int Quantity
	{
		set
		{
			MinQuantity = value;
		}
	}

	public int MinQuantity { get; set; } = 1;

	public int MaxQuantity
	{
		get
		{
			return Math.Max(_maxQuantity, MinQuantity);
		}
		set
		{
			_maxQuantity = value;
		}
	}
}
