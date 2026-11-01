using System;
using System.Runtime.InteropServices;

namespace Intersect.Client.Framework.GenericClasses;

[StructLayout(LayoutKind.Explicit)]
public struct Rectangle : IEquatable<Rectangle>
{
	[FieldOffset(0)]
	public int X;

	[FieldOffset(4)]
	public int Y;

	[FieldOffset(8)]
	public int Width;

	[FieldOffset(12)]
	public int Height;

	[FieldOffset(0)]
	public Point Position;

	[FieldOffset(8)]
	public Point Size;

	public int Left => X;

	public int Top => Y;

	public int Bottom => Y + Height;

	public int Right => X + Width;

	public static Rectangle Empty => default(Rectangle);

	public Rectangle(int x, int y, int w, int h)
	{
		Position = default(Point);
		Size = default(Point);
		X = x;
		Y = y;
		Width = w;
		Height = h;
	}

	public Rectangle(Point position, Point size)
	{
		X = 0;
		Y = 0;
		Width = 0;
		Height = 0;
		Position = position;
		Size = size;
	}

	public Rectangle(Rectangle other)
	{
		Position = default(Point);
		Size = default(Point);
		X = other.X;
		Y = other.Y;
		Width = other.Width;
		Height = other.Height;
	}

	public static Rectangle Intersect(Rectangle a, Rectangle b)
	{
		if (!a.IntersectsWithInclusive(b))
		{
			return Empty;
		}
		return FromLtrb(Math.Max(a.Left, b.Left), Math.Max(a.Top, b.Top), Math.Min(a.Right, b.Right), Math.Min(a.Bottom, b.Bottom));
	}

	public static Rectangle FromLtrb(int left, int top, int right, int bottom)
	{
		return new Rectangle(left, top, right - left, bottom - top);
	}

	public bool IntersectsWith(Rectangle rect)
	{
		if (Left < rect.Right && Right > rect.Left && Top < rect.Bottom)
		{
			return Bottom > rect.Top;
		}
		return false;
	}

	private bool IntersectsWithInclusive(Rectangle r)
	{
		if (Left <= r.Right && Right >= r.Left && Top <= r.Bottom)
		{
			return Bottom >= r.Top;
		}
		return false;
	}

	public bool Contains(int x, int y)
	{
		if (x >= Left && x < Right && y >= Top)
		{
			return y < Bottom;
		}
		return false;
	}

	public bool Contains(Point pt)
	{
		return Contains(pt.X, pt.Y);
	}

	public override string ToString()
	{
		return $"{X},{Y},{Width},{Height}";
	}

	public static string ToString(Rectangle rect)
	{
		return rect.ToString();
	}

	public static Rectangle FromString(string rect)
	{
		if (string.IsNullOrEmpty(rect))
		{
			return Empty;
		}
		string[] array = rect.Split(",".ToCharArray());
		int[] array2 = new int[array.Length];
		for (int i = 0; i < array.Length; i++)
		{
			array2[i] = int.Parse(array[i]);
		}
		return new Rectangle(array2[0], array2[1], array2[2], array2[3]);
	}

	public bool Equals(Rectangle other)
	{
		if (X == other.X && Y == other.Y && Width == other.Width && Height == other.Height && Position.Equals(other.Position))
		{
			return Size.Equals(other.Size);
		}
		return false;
	}

	public override bool Equals(object? obj)
	{
		if (obj is Rectangle other)
		{
			return Equals(other);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return HashCode.Combine(X, Y, Width, Height, Position, Size);
	}

	public static bool operator ==(Rectangle lhs, Rectangle rhs)
	{
		return lhs.Equals(rhs);
	}

	public static bool operator !=(Rectangle lhs, Rectangle rhs)
	{
		return !lhs.Equals(rhs);
	}
}
