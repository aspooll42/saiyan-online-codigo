using System;
using System.Globalization;
using System.Linq;

namespace Intersect.Client.Framework.Gwen;

public struct Padding : IEquatable<Padding>
{
	public int Top;

	public int Bottom;

	public int Left;

	public int Right;

	public static Padding Zero = new Padding(0, 0, 0, 0);

	public static Padding One = new Padding(1, 1, 1, 1);

	public static Padding Two = new Padding(2, 2, 2, 2);

	public static Padding TwoV = new Padding(0, 2);

	public static Padding Three = new Padding(3, 3, 3, 3);

	public static Padding Four = new Padding(4, 4, 4, 4);

	public static Padding Five = new Padding(5, 5, 5, 5);

	public static Padding FourH = new Padding(4, 0);

	public Padding(int size)
		: this(size, size, size, size)
	{
	}

	public Padding(int horizontal, int vertical)
		: this(horizontal, vertical, horizontal, vertical)
	{
	}

	public Padding(int left, int top, int right, int bottom)
	{
		Top = top;
		Bottom = bottom;
		Left = left;
		Right = right;
	}

	public bool Equals(Padding other)
	{
		if (other.Top == Top && other.Bottom == Bottom && other.Left == Left)
		{
			return other.Right == Right;
		}
		return false;
	}

	public static Padding operator +(Padding lhs, Padding rhs)
	{
		return new Padding(lhs.Left + rhs.Left, lhs.Top + rhs.Top, lhs.Right + rhs.Right, lhs.Bottom + rhs.Bottom);
	}

	public static Padding operator -(Padding lhs, Padding rhs)
	{
		return new Padding(lhs.Left - rhs.Left, lhs.Top - rhs.Top, lhs.Right - rhs.Right, lhs.Bottom - rhs.Bottom);
	}

	public static Padding operator *(Padding lhs, int rhs)
	{
		return new Padding(lhs.Left * rhs, lhs.Top * rhs, lhs.Right * rhs, lhs.Bottom * rhs);
	}

	public static Padding operator /(Padding lhs, int rhs)
	{
		return new Padding(lhs.Left / rhs, lhs.Top / rhs, lhs.Right / rhs, lhs.Bottom / rhs);
	}

	public static Point operator +(Point point, Padding padding)
	{
		return new Point(point.X + padding.Left + padding.Right, point.Y + padding.Top + padding.Bottom);
	}

	public static bool operator ==(Padding lhs, Padding rhs)
	{
		return lhs.Equals(rhs);
	}

	public static bool operator !=(Padding lhs, Padding rhs)
	{
		return !lhs.Equals(rhs);
	}

	public override bool Equals(object obj)
	{
		if (obj == null)
		{
			return false;
		}
		if (obj.GetType() != typeof(Padding))
		{
			return false;
		}
		return Equals((Padding)obj);
	}

	public override int GetHashCode()
	{
		int top = Top;
		top = (top * 397) ^ Bottom;
		top = (top * 397) ^ Left;
		return (top * 397) ^ Right;
	}

	public override string ToString()
	{
		return $"{Left},{Top},{Right},{Bottom}";
	}

	public static string ToString(Padding pad)
	{
		return pad.ToString();
	}

	public static Padding FromString(string rawPadding)
	{
		if (string.IsNullOrEmpty(rawPadding))
		{
			return default(Padding);
		}
		int result;
		int[] array = (from rawPart in rawPadding.Split(',')
			select int.TryParse(rawPart, CultureInfo.InvariantCulture, out result) ? result : 0).ToArray();
		int num = array.Length;
		if (num >= 1)
		{
			return num switch
			{
				1 => new Padding(array[0]), 
				2 => new Padding(array[0], array[1], array[0], array[1]), 
				3 => new Padding(array[0], array[1], array[2], array[1]), 
				_ => new Padding(array[0], array[1], array[2], array[3]), 
			};
		}
		return default(Padding);
	}
}
