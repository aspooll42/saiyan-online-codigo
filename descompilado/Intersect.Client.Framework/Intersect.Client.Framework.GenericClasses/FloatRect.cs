using System;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Intersect.Client.Framework.GenericClasses;

public struct FloatRect
{
	private float mX;

	private float mY;

	private float mWidth;

	private float mHeight;

	public float X
	{
		get
		{
			return mX;
		}
		set
		{
			mX = value;
		}
	}

	public float Y
	{
		get
		{
			return mY;
		}
		set
		{
			mY = value;
		}
	}

	public float Width
	{
		get
		{
			return mWidth;
		}
		set
		{
			mWidth = value;
		}
	}

	public float Height
	{
		get
		{
			return mHeight;
		}
		set
		{
			mHeight = value;
		}
	}

	public Vector2 Position
	{
		get
		{
			return new Vector2(X, Y);
		}
		set
		{
			X = value.X;
			Y = value.Y;
		}
	}

	public Vector2 Size
	{
		get
		{
			return new Vector2(Width, Height);
		}
		set
		{
			Width = value.X;
			Height = value.Y;
		}
	}

	public Vector2 Center => Position + Size / 2f;

	public float Left => X;

	public float Top => Y;

	public float Bottom => Y + Height;

	public float Right => X + Width;

	public static FloatRect Empty => default(FloatRect);

	public FloatRect(float x, float y, float w, float h)
	{
		mX = x;
		mY = y;
		mWidth = w;
		mHeight = h;
	}

	public FloatRect(Vector2 position, Vector2 size)
		: this(position.X, position.Y, size.X, size.Y)
	{
	}

	public FloatRect(Rectangle rectangle)
		: this(rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height)
	{
	}

	public static FloatRect Intersect(FloatRect a, FloatRect b)
	{
		if (!a.IntersectsWithInclusive(b))
		{
			return Empty;
		}
		return FromLtrb(Math.Max(a.Left, b.Left), Math.Max(a.Top, b.Top), Math.Min(a.Right, b.Right), Math.Min(a.Bottom, b.Bottom));
	}

	public static FloatRect FromLtrb(float left, float top, float right, float bottom)
	{
		return new FloatRect(left, top, right - left, bottom - top);
	}

	public bool IntersectsWith(FloatRect rect)
	{
		if (!(Left >= rect.Right) && !(Right <= rect.Left) && !(Top >= rect.Bottom))
		{
			return !(Bottom <= rect.Top);
		}
		return false;
	}

	private bool IntersectsWithInclusive(FloatRect r)
	{
		if (!(Left > r.Right) && !(Right < r.Left) && !(Top > r.Bottom))
		{
			return !(Bottom < r.Top);
		}
		return false;
	}

	public void Reset()
	{
		X = 0f;
		Y = 0f;
		Width = 0f;
		Height = 0f;
	}

	public bool Contains(float x, float y)
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

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static FloatRect operator *(FloatRect lhs, float rhs)
	{
		return new FloatRect(lhs.X * rhs, lhs.Y * rhs, lhs.Width * rhs, lhs.Height * rhs);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static FloatRect operator *(float lhs, FloatRect rhs)
	{
		return rhs * lhs;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static FloatRect operator /(FloatRect lhs, float rhs)
	{
		return new FloatRect(lhs.X / rhs, lhs.Y / rhs, lhs.Width / rhs, lhs.Height / rhs);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static FloatRect operator /(float lhs, FloatRect rhs)
	{
		return rhs / lhs;
	}

	public override string ToString()
	{
		return $"{Left},{Top} + {Width},{Height} -> {Right},{Bottom}";
	}
}
