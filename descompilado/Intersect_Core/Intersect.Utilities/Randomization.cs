using System;
using Intersect.Core;
using Intersect.Enums;

namespace Intersect.Utilities;

public static class Randomization
{
	private static Random mRandom;

	static Randomization()
	{
		mRandom = new Random();
	}

	public static void Synchronize(IApplicationContext applicationContext)
	{
	}

	public static int Next()
	{
		lock (mRandom)
		{
			return mRandom.Next();
		}
	}

	public static int Next(int minValue, int maxValue)
	{
		lock (mRandom)
		{
			return mRandom.Next(minValue, maxValue);
		}
	}

	public static int Next(int maxValue)
	{
		lock (mRandom)
		{
			return mRandom.Next(maxValue);
		}
	}

	public static void NextBytes(byte[] buffer)
	{
		lock (mRandom)
		{
			mRandom.NextBytes(buffer);
		}
	}

	public static Direction NextDirection()
	{
		lock (mRandom)
		{
			return (Direction)mRandom.Next(Options.Instance.Map.MovementDirections);
		}
	}

	public static double NextDouble()
	{
		lock (mRandom)
		{
			return mRandom.NextDouble();
		}
	}
}
