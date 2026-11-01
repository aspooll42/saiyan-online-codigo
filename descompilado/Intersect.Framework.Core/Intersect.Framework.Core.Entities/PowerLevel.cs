using System;
using System.Collections.Generic;

namespace Intersect.Framework.Core.Entities;

public static class PowerLevel
{
	private const int StatWeight = 5;

	private const double PerLevel = 0.1;

	public static long Of(int level, IReadOnlyList<int>? stats, long maxHealth, long maxKi)
	{
		long num = 0L;
		if (stats != null)
		{
			foreach (int stat in stats)
			{
				num += Math.Max(0, stat);
			}
		}
		long num2 = num * 5 + Math.Max(0L, maxHealth) + Math.Max(0L, maxKi);
		double num3 = 1.0 + (double)Math.Max(0, level - 1) * 0.1;
		return Math.Max(1L, (long)Math.Round((double)num2 * num3));
	}
}
