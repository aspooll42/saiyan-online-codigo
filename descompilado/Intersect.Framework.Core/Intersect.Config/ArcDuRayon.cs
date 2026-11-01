using System;

namespace Intersect.Config;

public static class ArcDuRayon
{
	private const float Droit = 1E-06f;

	public static (float X, float Y, float Cos, float Sin) Point(float ox, float oy, float cos0, float sin0, float k, float s)
	{
		if (MathF.Abs(k) < 1E-06f)
		{
			return (X: ox + cos0 * s, Y: oy + sin0 * s, Cos: cos0, Sin: sin0);
		}
		float num = k * s;
		(float Sin, float Cos) tuple = MathF.SinCos(num);
		float item = tuple.Sin;
		float item2 = tuple.Cos;
		float num2 = ((MathF.Abs(num) < 0.001f) ? (s * (1f - num * num / 6f)) : (item / k));
		float num3 = ((MathF.Abs(num) < 0.001f) ? (s * num / 2f) : ((1f - item2) / k));
		return (X: ox + cos0 * num2 - sin0 * num3, Y: oy + sin0 * num2 + cos0 * num3, Cos: cos0 * item2 - sin0 * item, Sin: sin0 * item2 + cos0 * item);
	}

	public static (float Le, float Travers) Projeter(float ox, float oy, float cos0, float sin0, float k, float x, float y)
	{
		float num = x - ox;
		float num2 = y - oy;
		if (MathF.Abs(k) < 1E-06f)
		{
			return (Le: num * cos0 + num2 * sin0, Travers: num2 * cos0 - num * sin0);
		}
		float num3 = 1f / k;
		float num4 = (0f - sin0) * num3;
		float num5 = cos0 * num3;
		float num6 = MathF.Atan2(0f - num5, 0f - num4);
		float num7 = MathF.Atan2(num2 - num5, num - num4);
		float num8;
		for (num8 = num7 - num6; num8 > (float)Math.PI; num8 -= (float)Math.PI * 2f)
		{
		}
		for (; num8 <= -(float)Math.PI; num8 += (float)Math.PI * 2f)
		{
		}
		float num9 = num8 / k;
		var (num10, num11, num12, num13) = Point(ox, oy, cos0, sin0, k, num9);
		return (Le: num9, Travers: (y - num11) * num12 - (x - num10) * num13);
	}

	public static float Distance(float ox, float oy, float cos0, float sin0, float k, float longueur, float x, float y)
	{
		var (num, x2) = Projeter(ox, oy, cos0, sin0, k, x, y);
		if (num >= 0f && num <= longueur)
		{
			return MathF.Abs(x2);
		}
		var (num2, num3, _, _) = Point(ox, oy, cos0, sin0, k, (num < 0f) ? 0f : longueur);
		return MathF.Sqrt((x - num2) * (x - num2) + (y - num3) * (y - num3));
	}

	public static (float Cos, float Sin, float Longueur) DepuisLaCorde(float x0, float y0, float x1, float y1, float k)
	{
		float num = x1 - x0;
		float num2 = y1 - y0;
		float num3 = MathF.Sqrt(num * num + num2 * num2);
		if (num3 < 0.0001f)
		{
			return (Cos: 1f, Sin: 0f, Longueur: 0f);
		}
		if (MathF.Abs(k) < 1E-06f)
		{
			return (Cos: num / num3, Sin: num2 / num3, Longueur: num3);
		}
		float num4 = MathF.Asin(Math.Clamp(num3 * MathF.Abs(k) / 2f, 0f, 1f));
		float x2 = MathF.Atan2(num2, num) - (float)MathF.Sign(k) * num4;
		return (Cos: MathF.Cos(x2), Sin: MathF.Sin(x2), Longueur: 2f * num4 / MathF.Abs(k));
	}
}
