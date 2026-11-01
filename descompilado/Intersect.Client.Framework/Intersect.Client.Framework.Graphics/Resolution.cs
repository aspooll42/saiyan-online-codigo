using System;

namespace Intersect.Client.Framework.Graphics;

public struct Resolution : IComparable<Resolution>
{
	public static readonly Resolution Empty = default(Resolution);

	private static readonly char[] Separators = new char[8] { 'x', ',', ' ', '/', '-', '_', '.', '~' };

	public readonly ushort X;

	public readonly ushort Y;

	public Resolution(long x = 800L, long y = 600L)
	{
		X = (ushort)(x & 0xFFFF);
		Y = (ushort)(y & 0xFFFF);
	}

	public Resolution(ulong x = 800uL, ulong y = 600uL)
	{
		X = (ushort)(x & 0xFFFF);
		Y = (ushort)(y & 0xFFFF);
	}

	public Resolution(Resolution resolution, long overrideX = 0L, long overrideY = 0L)
		: this((overrideX > 0) ? overrideX : resolution.X, (overrideY > 0) ? overrideY : resolution.Y)
	{
	}

	public Resolution(Resolution resolution, Resolution? overrideResolution = null)
	{
		ushort num = overrideResolution?.X ?? resolution.X;
		X = ((num > 0) ? num : resolution.X);
		ushort num2 = overrideResolution?.Y ?? resolution.Y;
		Y = ((num2 > 0) ? num2 : resolution.Y);
	}

	public int CompareTo(Resolution other)
	{
		int num = X - other.X;
		if (num == 0)
		{
			return Y - other.Y;
		}
		return num;
	}

	public override bool Equals(object obj)
	{
		if (obj is Resolution other)
		{
			return Equals(other);
		}
		return false;
	}

	public bool Equals(Resolution other)
	{
		if (X == other.X)
		{
			return Y == other.Y;
		}
		return false;
	}

	public override int GetHashCode()
	{
		return (X << 16) & Y;
	}

	public override string ToString()
	{
		return $"{X},{Y}";
	}

	public static Resolution Parse(string resolution)
	{
		string[] array = resolution?.Split(Separators);
		string text = ((array != null) ? array[0] : null);
		string text2 = ((array != null) ? array[1] : null);
		if (string.IsNullOrWhiteSpace(text))
		{
			throw new ArgumentNullException("xString");
		}
		if (string.IsNullOrWhiteSpace(text2))
		{
			throw new ArgumentNullException("xString");
		}
		ushort num = ushort.Parse(text);
		ushort num2 = ushort.Parse(text2);
		return new Resolution(num, num2);
	}

	public static bool TryParse(string resolutionString, out Resolution resolution)
	{
		try
		{
			resolution = Parse(resolutionString);
			return true;
		}
		catch
		{
			resolution = default(Resolution);
			return false;
		}
	}

	public static bool operator ==(Resolution left, Resolution right)
	{
		if (left.X == right.X)
		{
			return left.Y == right.Y;
		}
		return false;
	}

	public static bool operator !=(Resolution left, Resolution right)
	{
		if (left.X != right.X)
		{
			return left.Y != right.Y;
		}
		return false;
	}
}
