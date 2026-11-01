using System;
using System.Globalization;
using System.Linq;

namespace Intersect.Client.Framework.Gwen;

public struct Margin : IEquatable<Margin>
{
	public int Top;

	public int Right;

	public int Bottom;

	public int Left;

	public static Margin Zero = new Margin(0, 0, 0, 0);

	public static Margin One = new Margin(1, 1, 1, 1);

	public static Margin Two = new Margin(2, 2, 2, 2);

	public static Margin Three = new Margin(3, 3, 3, 3);

	public static Margin Four = new Margin(4, 4, 4, 4);

	public static Margin Five = new Margin(5, 5, 5, 5);

	public static Margin Six = new Margin(6, 6, 6, 6);

	public static Margin Seven = new Margin(7, 7, 7, 7);

	public static Margin Eight = new Margin(8, 8, 8, 8);

	public static Margin Nine = new Margin(9, 9, 9, 9);

	public static Margin Ten = new Margin(10, 10, 10, 10);

	public Margin(int size)
		: this(size, size, size, size)
	{
	}

	public Margin(int horizontal, int vertical)
		: this(horizontal, vertical, horizontal, vertical)
	{
	}

	public Margin(int left, int top, int right, int bottom)
	{
		Top = top;
		Bottom = bottom;
		Left = left;
		Right = right;
	}

	public bool Equals(Margin other)
	{
		if (other.Top == Top && other.Bottom == Bottom && other.Left == Left)
		{
			return other.Right == Right;
		}
		return false;
	}

	public static Point operator +(Point point, Margin margin)
	{
		return new Point(point.X + margin.Left + margin.Right, point.Y + margin.Top + margin.Bottom);
	}

	public static Margin operator *(Margin margin, int scale)
	{
		return new Margin(margin.Left * scale, margin.Top * scale, margin.Right * scale, margin.Bottom * scale);
	}

	public static Margin operator *(Margin margin, float scale)
	{
		return new Margin((int)((float)margin.Left * scale), (int)((float)margin.Top * scale), (int)((float)margin.Right * scale), (int)((float)margin.Bottom * scale));
	}

	public static bool operator ==(Margin lhs, Margin rhs)
	{
		return lhs.Equals(rhs);
	}

	public static bool operator !=(Margin lhs, Margin rhs)
	{
		return !lhs.Equals(rhs);
	}

	public override bool Equals(object? other)
	{
		if (other is Margin other2)
		{
			return Equals(other2);
		}
		return false;
	}

	public override string ToString()
	{
		return string.Join(',', Left, Top, Right, Bottom);
	}

	public static Margin FromString(string rawMargin)
	{
		if (string.IsNullOrEmpty(rawMargin))
		{
			return default(Margin);
		}
		int result;
		int[] array = (from rawPart in rawMargin.Split(',')
			select int.TryParse(rawPart, CultureInfo.InvariantCulture, out result) ? result : 0).ToArray();
		int num = array.Length;
		if (num >= 1)
		{
			return num switch
			{
				1 => new Margin(array[0]), 
				2 => new Margin(array[0], array[1], array[0], array[1]), 
				3 => new Margin(array[0], array[1], array[2], array[1]), 
				_ => new Margin(array[0], array[1], array[2], array[3]), 
			};
		}
		return default(Margin);
	}
}
