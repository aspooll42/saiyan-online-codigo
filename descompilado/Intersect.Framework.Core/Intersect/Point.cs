using System.Numerics;
using MessagePack;

namespace Intersect;

[MessagePackObject(false)]
public struct Point
{
	[Key(0)]
	public int X { get; set; }

	[Key(1)]
	public int Y { get; set; }

	public static Point Empty => default(Point);

	public Point(int x, int y)
	{
		X = x;
		Y = y;
	}

	public override bool Equals(object obj)
	{
		if (obj is Point)
		{
			return (Point)obj == this;
		}
		return false;
	}

	public bool Equals(Point other)
	{
		if (X == other.X)
		{
			return Y == other.Y;
		}
		return false;
	}

	public override int GetHashCode()
	{
		return X.GetHashCode() ^ Y.GetHashCode();
	}

	public override string ToString()
	{
		return $"{X},{Y}";
	}

	public static Point FromString(string val)
	{
		if (string.IsNullOrEmpty(val))
		{
			return Empty;
		}
		string[] array = val.Split(",".ToCharArray());
		int[] array2 = new int[array.Length];
		for (int i = 0; i < array.Length; i++)
		{
			array2[i] = int.Parse(array[i]);
		}
		return new Point(array2[0], array2[1]);
	}

	public static bool operator !=(Point left, Point right)
	{
		if (left.X == right.X)
		{
			return left.Y != right.Y;
		}
		return true;
	}

	public static bool operator ==(Point left, Point right)
	{
		if (left.X == right.X)
		{
			return left.Y == right.Y;
		}
		return false;
	}

	public static Point operator +(Point left, Point right)
	{
		return new Point(left.X + right.X, left.Y + right.Y);
	}

	public static Vector2 operator +(Point left, Vector2 right)
	{
		return new Vector2((float)left.X + right.X, (float)left.Y + right.Y);
	}

	public static Vector2 operator +(Vector2 left, Point right)
	{
		return new Vector2(left.X + (float)right.X, left.Y + (float)right.Y);
	}

	public static Point operator -(Point left, Point right)
	{
		return new Point(left.X - right.X, left.Y - right.Y);
	}

	public static Point operator *(Point point, float scalar)
	{
		return new Point((int)((float)point.X * scalar), (int)((float)point.Y * scalar));
	}

	public static Point operator /(Point point, float scalar)
	{
		return new Point((int)((float)point.X / scalar), (int)((float)point.Y / scalar));
	}

	public void Deconstruct(out int x, out int y)
	{
		x = X;
		y = Y;
	}

	public static implicit operator Point(Vector2 vector)
	{
		return new Point((int)vector.X, (int)vector.Y);
	}

	public static implicit operator Vector2(Point point)
	{
		return new Vector2(point.X, point.Y);
	}
}
