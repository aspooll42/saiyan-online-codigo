using System;

namespace Intersect.Framework.Core;

public struct ColorF : IComparable<ColorF>, IEquatable<ColorF>
{
	public float A;

	public float R;

	public float G;

	public float B;

	public static ColorF Black => new ColorF(0f, 0f, 0f);

	public static ColorF Blue => new ColorF(0f, 0f, 255f);

	public static ColorF ForestGreen => new ColorF(34f, 139f, 34f);

	public static ColorF Green => new ColorF(0f, 255f, 0f);

	public static ColorF LightCoral => new ColorF(240f, 128f, 128f);

	public static ColorF Magenta => new ColorF(255f, 0f, 255f);

	public static ColorF Red => new ColorF(255f, 0f, 0f);

	public static ColorF Transparent => new ColorF(0f, 0f, 0f, 0f);

	public static ColorF White => new ColorF(255f, 255f, 255f);

	public static ColorF Yellow => new ColorF(255f, 255f, 0f);

	public ColorF()
	{
		A = 0f;
		R = 0f;
		G = 0f;
		B = 0f;
	}

	public ColorF(float r, float g, float b, float a = 255f)
	{
		A = a;
		R = r;
		G = g;
		B = b;
	}

	public static ColorF FromArgb(float a, float r, float g, float b)
	{
		return new ColorF(r, g, b, a);
	}

	public static ColorF FromArgb(int argb)
	{
		return new ColorF((uint)((argb >>> 16) & 0xFF), (uint)((argb >>> 8) & 0xFF), (uint)(argb & 0xFF), (uint)((argb >>> 24) & 0xFF));
	}

	public int CompareTo(ColorF other)
	{
		int num = R.CompareTo(other.R);
		if (num != 0)
		{
			return num;
		}
		num = G.CompareTo(other.G);
		if (num != 0)
		{
			return num;
		}
		num = B.CompareTo(other.B);
		if (num != 0)
		{
			return num;
		}
		return A.CompareTo(other.A);
	}

	public bool Equals(ColorF other)
	{
		if (R.Equals(other.R) && G.Equals(other.G) && B.Equals(other.B))
		{
			return A.Equals(other.A);
		}
		return false;
	}

	public override bool Equals(object obj)
	{
		if (obj is ColorF other)
		{
			return Equals(other);
		}
		return false;
	}

	public static bool operator ==(ColorF left, ColorF right)
	{
		return left.Equals(right);
	}

	public static bool operator !=(ColorF left, ColorF right)
	{
		return !(left == right);
	}
}
