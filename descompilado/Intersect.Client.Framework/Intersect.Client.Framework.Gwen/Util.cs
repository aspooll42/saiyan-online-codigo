using System;
using System.Text.RegularExpressions;
using Intersect.Client.Framework.GenericClasses;

namespace Intersect.Client.Framework.Gwen;

public static class Util
{
	public static int Round(float x)
	{
		return (int)Math.Round(x, MidpointRounding.AwayFromZero);
	}

	public static int Ceil(float x)
	{
		return (int)Math.Ceiling(x);
	}

	public static Rectangle FloatRect(float x, float y, float w, float h)
	{
		return new Rectangle((int)x, (int)y, (int)w, (int)h);
	}

	public static int Clamp(int x, int min, int max)
	{
		if (x < min)
		{
			return min;
		}
		if (x > max)
		{
			return max;
		}
		return x;
	}

	public static float Clamp(float x, float min, float max)
	{
		if (x < min)
		{
			return min;
		}
		if (x > max)
		{
			return max;
		}
		return x;
	}

	public static Rectangle ClampRectToRect(Rectangle inside, Rectangle outside, bool clampSize = false)
	{
		if (inside.X < outside.X)
		{
			inside.X = outside.X;
		}
		if (inside.Y < outside.Y)
		{
			inside.Y = outside.Y;
		}
		if (inside.Right > outside.Right)
		{
			if (clampSize)
			{
				inside.Width = outside.Width;
			}
			else
			{
				inside.X = outside.Right - inside.Width;
			}
		}
		if (inside.Bottom > outside.Bottom)
		{
			if (clampSize)
			{
				inside.Height = outside.Height;
			}
			else
			{
				inside.Y = outside.Bottom - inside.Height;
			}
		}
		return inside;
	}

	public static Hsv ToHsv(this Color color)
	{
		Hsv result = default(Hsv);
		int num = Math.Max(color.R, Math.Max(color.G, color.B));
		int num2 = Math.Min(color.R, Math.Min(color.G, color.B));
		result.H = 0f;
		result.s = ((num == 0) ? 0f : (1f - 1f * (float)num2 / (float)num));
		result.V = (float)num / 255f;
		return result;
	}

	public static Color HsvToColor(float h, float s, float v)
	{
		int num = Convert.ToInt32(Math.Floor(h / 60f)) % 6;
		float num2 = h / 60f - (float)Math.Floor(h / 60f);
		v *= 255f;
		int num3 = Convert.ToInt32(v);
		int num4 = Convert.ToInt32(v * (1f - s));
		int num5 = Convert.ToInt32(v * (1f - num2 * s));
		int num6 = Convert.ToInt32(v * (1f - (1f - num2) * s));
		return num switch
		{
			0 => Color.FromArgb(255, num3, num6, num4), 
			1 => Color.FromArgb(255, num5, num3, num4), 
			2 => Color.FromArgb(255, num4, num3, num6), 
			3 => Color.FromArgb(255, num4, num5, num3), 
			4 => Color.FromArgb(255, num6, num4, num3), 
			_ => Color.FromArgb(255, num3, num4, num5), 
		};
	}

	public static Color Subtract(this Color color, Color other)
	{
		return Color.FromArgb(color.A - other.A, color.R - other.R, color.G - other.G, color.B - other.B);
	}

	public static Color Add(this Color color, Color other)
	{
		return Color.FromArgb(color.A + other.A, color.R + other.R, color.G + other.G, color.B + other.B);
	}

	public static Color Multiply(this Color color, float amount)
	{
		return Color.FromArgb(color.A, (int)((float)(int)color.R * amount), (int)((float)(int)color.G * amount), (int)((float)(int)color.B * amount));
	}

	public static Rectangle Add(this Rectangle r, Rectangle other)
	{
		return new Rectangle(r.X + other.X, r.Y + other.Y, r.Width + other.Width, r.Height + other.Height);
	}

	public static string[] SplitAndKeep(string text, string separators)
	{
		return Regex.Split(text, "(?=[" + separators + "])");
	}
}
