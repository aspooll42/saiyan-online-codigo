using System;
using Microsoft.EntityFrameworkCore;

namespace Intersect.GameObjects.Ranges;

[Owned]
public class ItemRange
{
	public int LowRange { get; set; }

	public int HighRange { get; set; }

	public ItemRange()
	{
	}

	public ItemRange(int lowRange, int highRange)
		: this()
	{
		LowRange = lowRange;
		HighRange = highRange;
	}

	private Random GetRandomInstance(int? seed = null)
	{
		if (seed.HasValue)
		{
			return new Random(seed.Value);
		}
		return Random.Shared;
	}

	public int Roll(int? seed = null)
	{
		Random randomInstance = GetRandomInstance(seed);
		return randomInstance.Next(LowRange, HighRange + 1);
	}

	public void Validate()
	{
		if (LowRange > HighRange)
		{
			int highRange = HighRange;
			int lowRange = LowRange;
			LowRange = highRange;
			HighRange = lowRange;
		}
	}
}
