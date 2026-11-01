using System;
using MathNet.Numerics.Random;

namespace Intersect.Extensions;

public static class RandomExtensions
{
	public static long NextLong(this Random random)
	{
		byte[] array = new byte[8];
		random.NextBytes(array);
		return BitConverter.ToInt64(array, 0);
	}

	public static long NextLong(this Random random, long maximum)
	{
		if (maximum <= 0)
		{
			throw new ArgumentOutOfRangeException("maximum", "'maximum' must be greater than 0.");
		}
		return random.NextLong() % maximum;
	}

	public static long NextLong(this Random random, long minimum, long maximum)
	{
		if (minimum >= maximum)
		{
			throw new ArgumentOutOfRangeException("maximum", "'maximum' must be greater than 'minimum'.");
		}
		ulong num = random.NextULong((ulong)(maximum - minimum));
		if (num < long.MaxValue)
		{
			return minimum + (long)num;
		}
		return maximum - (-1L - (long)num);
	}

	public static ulong NextULong(this Random random)
	{
		byte[] array = new byte[8];
		random.NextBytes(array);
		return BitConverter.ToUInt64(array, 0);
	}

	public static ulong NextULong(this Random random, ulong maximum)
	{
		return random.NextULong() % maximum;
	}

	public static ulong NextULong(this Random random, ulong minimum, ulong maximum)
	{
		if (minimum >= maximum)
		{
			throw new ArgumentOutOfRangeException("maximum", "'maximum' must be greater than 'minimum'.");
		}
		return random.NextULong(maximum - minimum) + minimum;
	}

	public static decimal NextDecimal(this Random random, decimal maximum)
	{
		if (maximum <= 0m)
		{
			throw new ArgumentOutOfRangeException("maximum", "'maximum' must be greater than 0.");
		}
		return random.NextDecimal() * maximum;
	}

	public static decimal NextDecimal(this Random random, decimal minimum, decimal maximum)
	{
		if (minimum >= maximum)
		{
			throw new ArgumentOutOfRangeException("maximum", "'maximum' must be greater than 'minimum'.");
		}
		decimal num = minimum / 2m + maximum / 2m;
		decimal num2 = Math.Abs(maximum - num);
		return (random.NextDecimal(2m) - 1m) * num2 + num;
	}

	public static double NextDouble(this Random random, double maximum)
	{
		if (maximum <= 0.0)
		{
			throw new ArgumentOutOfRangeException("maximum", "'maximum' must be greater than 0.");
		}
		return random.NextDouble() * maximum;
	}

	public static double NextDouble(this Random random, double minimum, double maximum)
	{
		if (minimum >= maximum)
		{
			throw new ArgumentOutOfRangeException("maximum", "'maximum' must be greater than 'minimum'.");
		}
		double num = minimum / 2.0 + maximum / 2.0;
		double num2 = Math.Abs(maximum - num);
		return (random.NextDouble(2.0) - 1.0) * num2 + num;
	}

	public static float NextFloat(this Random random)
	{
		return (float)random.NextDouble();
	}

	public static float NextFloat(this Random random, float maximum)
	{
		if (maximum <= 0f)
		{
			throw new ArgumentOutOfRangeException("maximum", "'maximum' must be greater than 0.");
		}
		return (float)random.NextDouble(maximum);
	}

	public static float NextFloat(this Random random, float minimum, float maximum)
	{
		if (minimum >= maximum)
		{
			throw new ArgumentOutOfRangeException("maximum", "'maximum' must be greater than 'minimum'.");
		}
		return (float)random.NextDouble(minimum, maximum);
	}
}
