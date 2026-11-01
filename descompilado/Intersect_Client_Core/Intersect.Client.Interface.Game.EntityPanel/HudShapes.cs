using System;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Gwen.Renderer;
using Intersect.Client.Interface.Game.Techniques;

namespace Intersect.Client.Interface.Game.EntityPanel;

internal static class HudShapes
{
	public readonly record struct Ramp(Color Top, Color Middle, Color Bottom, float MiddleAt = 0.5f)
	{
		public static Ramp Solid(Color color)
		{
			return new Ramp(color, color, color);
		}

		public static Ramp Two(Color top, Color bottom)
		{
			return new Ramp(top, SaiyanHud.Lerp(top, bottom, 0.5f), bottom);
		}

		public Color At(float t)
		{
			if (!(t <= MiddleAt))
			{
				return SaiyanHud.Lerp(Middle, Bottom, (t - MiddleAt) / Math.Max(0.0001f, 1f - MiddleAt));
			}
			return SaiyanHud.Lerp(Top, Middle, (MiddleAt <= 0f) ? 1f : (t / MiddleAt));
		}
	}

	private const float Sqrt3 = 1.7320508f;

	private const float HexHeight = 0.8660254f;

	public static void Fill(Base renderer, int x, int y, int width, int height, Color color, bool additive = false)
	{
		if (width > 0 && height > 0 && color.A > 0)
		{
			if (additive)
			{
				SaiyanHud.Draw(renderer, SaiyanHud.Texture("inv_plein.png"), x, y, width, height, color);
				return;
			}
			renderer.DrawColor = color;
			renderer.DrawFilledRect(new Rectangle(x, y, width, height));
		}
	}

	public static void Span(Base renderer, float left, float right, int y, int height, Color color, bool additive = false)
	{
		if (right - left <= 0.01f || color.A <= 0)
		{
			return;
		}
		int num = (int)MathF.Ceiling(left);
		int num2 = (int)MathF.Floor(right);
		if (num2 < num)
		{
			Fill(renderer, (int)MathF.Floor(left), y, 1, height, SaiyanHud.WithAlpha(color, right - left), additive);
			return;
		}
		Fill(renderer, num, y, num2 - num, height, color, additive);
		float num3 = (float)num - left;
		if (num3 > 0.03f)
		{
			Fill(renderer, num - 1, y, 1, height, SaiyanHud.WithAlpha(color, num3), additive);
		}
		float num4 = right - (float)num2;
		if (num4 > 0.03f)
		{
			Fill(renderer, num2, y, 1, height, SaiyanHud.WithAlpha(color, num4), additive);
		}
	}

	public static void SpanGradient(Base renderer, float left, float right, int y, int height, Color from, Color to, int chunks)
	{
		float num = right - left;
		if (!(num <= 0.01f))
		{
			chunks = Math.Max(1, chunks);
			for (int i = 0; i < chunks; i++)
			{
				float num2 = left + num * (float)i / (float)chunks;
				float num3 = left + num * (float)(i + 1) / (float)chunks;
				float left2 = ((i == 0) ? num2 : MathF.Round(num2));
				float right2 = ((i == chunks - 1) ? num3 : MathF.Round(num3));
				Span(renderer, left2, right2, y, height, SaiyanHud.Lerp(from, to, ((float)i + 0.5f) / (float)chunks));
			}
		}
	}

	public static void Slanted(Base renderer, float x, int top, float width, int height, float slant, Ramp ramp, float alpha = 1f, bool additive = false)
	{
		if (height > 0 && !(width <= 0f) && !(alpha <= 0f))
		{
			for (int i = 0; i < height; i++)
			{
				float num = ((float)i + 0.5f) / (float)height;
				float left = x + slant * (1f - num);
				float right = x + width - slant * num;
				Span(renderer, left, right, top + i, 1, SaiyanHud.WithAlpha(ramp.At(num), alpha), additive);
			}
		}
	}

	public static void SlantedOutline(Base renderer, float x, int top, float width, int height, float slant, Color color)
	{
		if (height > 0 && color.A > 0)
		{
			Span(renderer, x + slant, x + width, top, 1, color);
			Span(renderer, x, x + width - slant, top + height - 1, 1, color);
			for (int i = 1; i < height - 1; i++)
			{
				float num = ((float)i + 0.5f) / (float)height;
				float num2 = x + slant * (1f - num);
				float num3 = x + width - slant * num;
				Span(renderer, num2, num2 + 1f, top + i, 1, color);
				Span(renderer, num3 - 1f, num3, top + i, 1, color);
			}
		}
	}

	public static float HexHalf(float radius, float distance)
	{
		return radius - distance / 1.7320508f;
	}

	public static void Hexagon(Base renderer, float centerX, float centerY, float radius, Ramp ramp, float alpha = 1f, bool additive = false, int band = 1)
	{
		if (radius <= 0f || alpha <= 0f)
		{
			return;
		}
		float num = radius * 0.8660254f;
		int num2 = (int)MathF.Floor(centerY - num);
		int num3 = (int)MathF.Ceiling(centerY + num);
		band = Math.Max(1, band);
		for (int i = num2; i < num3; i += band)
		{
			int num4 = Math.Min(band, num3 - i);
			float num5 = (float)i + (float)num4 / 2f;
			float num6 = MathF.Abs(num5 - centerY);
			float num7 = Math.Clamp(num - num6 + (float)num4 / 2f, 0f, 1f);
			if (!(num7 <= 0.02f))
			{
				float num8 = HexHalf(radius, Math.Min(num6, num));
				float t = Math.Clamp((num5 - (centerY - num)) / (2f * num), 0f, 1f);
				Span(renderer, centerX - num8, centerX + num8, i, num4, SaiyanHud.WithAlpha(ramp.At(t), alpha * num7), additive);
			}
		}
	}

	public static void HexagonRing(Base renderer, float centerX, float centerY, float radius, float thickness, Ramp ramp, float alpha = 1f, bool additive = false)
	{
		if (radius <= 0f || thickness <= 0f || alpha <= 0f)
		{
			return;
		}
		float num = radius - thickness / 0.8660254f;
		float num2 = radius * 0.8660254f;
		float num3 = Math.Max(0f, num) * 0.8660254f;
		int num4 = (int)MathF.Floor(centerY - num2);
		int num5 = (int)MathF.Ceiling(centerY + num2);
		for (int i = num4; i < num5; i++)
		{
			float num6 = (float)i + 0.5f;
			float num7 = MathF.Abs(num6 - centerY);
			float num8 = Math.Clamp(num2 - num7 + 0.5f, 0f, 1f);
			if (!(num8 <= 0.02f))
			{
				float t = Math.Clamp((num6 - (centerY - num2)) / (2f * num2), 0f, 1f);
				Color color = SaiyanHud.WithAlpha(ramp.At(t), alpha * num8);
				float num9 = HexHalf(radius, Math.Min(num7, num2));
				if (num <= 0f || num7 >= num3)
				{
					Span(renderer, centerX - num9, centerX + num9, i, 1, color, additive);
					continue;
				}
				float num10 = HexHalf(num, num7);
				Span(renderer, centerX - num9, centerX - num10, i, 1, color, additive);
				Span(renderer, centerX + num10, centerX + num9, i, 1, color, additive);
			}
		}
	}

	public static (float X, float Y) HexPoint(float centerX, float centerY, float radius, float t)
	{
		t = (t % 1f + 1f) % 1f;
		int num = (int)MathF.Floor(t * 6f);
		float num2 = t * 6f - (float)num;
		float x = (float)Math.PI / 3f * (float)num;
		float x2 = (float)Math.PI / 3f * (float)(num + 1);
		return (X: centerX + radius * (MathF.Cos(x) + (MathF.Cos(x2) - MathF.Cos(x)) * num2), Y: centerY + radius * (MathF.Sin(x) + (MathF.Sin(x2) - MathF.Sin(x)) * num2));
	}

	public static void Line(Base renderer, float x0, float y0, float x1, float y1, int thickness, Color color)
	{
		float x2 = MathF.Max(MathF.Abs(x1 - x0), MathF.Abs(y1 - y0));
		int num = Math.Max(1, (int)MathF.Ceiling(x2));
		for (int i = 0; i <= num; i++)
		{
			float num2 = (float)i / (float)num;
			Fill(renderer, (int)MathF.Round(x0 + (x1 - x0) * num2 - (float)thickness / 2f), (int)MathF.Round(y0 + (y1 - y0) * num2 - (float)thickness / 2f), thickness, thickness, color);
		}
	}

	public static void Circle(Base renderer, float centerX, float centerY, float radius, float thickness, Color color)
	{
		float num = radius - thickness;
		int num2 = (int)MathF.Floor(centerY - radius);
		int num3 = (int)MathF.Ceiling(centerY + radius);
		for (int i = num2; i < num3; i++)
		{
			float num4 = MathF.Abs((float)i + 0.5f - centerY);
			if (!(num4 >= radius))
			{
				float num5 = MathF.Sqrt(radius * radius - num4 * num4);
				if (num4 >= num)
				{
					Span(renderer, centerX - num5, centerX + num5, i, 1, color);
					continue;
				}
				float num6 = MathF.Sqrt(num * num - num4 * num4);
				Span(renderer, centerX - num5, centerX - num6, i, 1, color);
				Span(renderer, centerX + num6, centerX + num5, i, 1, color);
			}
		}
	}

	public static void Glow(Base renderer, float centerX, float centerY, float radiusX, float radiusY, Color color, bool additive = true)
	{
		if (color.A > 0 && !(radiusX <= 1f) && !(radiusY <= 1f))
		{
			TechniqueArt.GlowRect(renderer, new Rectangle((int)MathF.Round(centerX - radiusX), (int)MathF.Round(centerY - radiusY), (int)MathF.Round(radiusX * 2f), (int)MathF.Round(radiusY * 2f)), color, additive);
		}
	}

	public static void Flames(Base renderer, float centerX, float centerY, float radius, Color light, Color? alternate, long now, float scale)
	{
		float half = radius * 0.8660254f;
		float bottom = centerY + half;
		int band = Math.Max(1, (int)MathF.Round(2f * scale));
		long time = now % 600000;
		TechniqueArt.Additive(renderer, delegate
		{
			for (int num = 0; num < 9; num++)
			{
				float num2 = centerX - radius + ((float)num + 0.5f) * (2f * radius) / 9f;
				float num3 = radius * (0.8f + 0.35f * MathF.Sin((float)time / 150f + (float)num * 1.9f) + 0.15f * MathF.Sin((float)time / 67f + (float)num * 3.1f));
				float num4 = 2f * radius / 9f * 1.2f;
				float num5 = MathF.Sin((float)time / 210f + (float)num) * 5f * scale;
				Color color2;
				if ((object)alternate != null)
				{
					Color color = alternate;
					if (num % 2 == 1)
					{
						color2 = color;
						goto IL_00df;
					}
				}
				color2 = light;
				goto IL_00df;
				IL_00df:
				Color color3 = color2;
				for (int i = (int)MathF.Floor(bottom - num3); (float)i < bottom; i += band)
				{
					float num6 = Math.Clamp((bottom - ((float)i + (float)band / 2f)) / num3, 0f, 1f);
					float num7 = Edge(num6, 0f - num4, -0.35f * num4 + num5 * 0.4f, 0.45f, -0.6f * num4 + num5 * 0.7f, 0.6f, num5);
					float num8 = Edge(num6, num4, 0.3f * num4 + num5 * 0.3f, 0.4f, 0.55f * num4 + num5 * 0.6f, 0.55f, num5);
					float num9 = MathF.Abs((float)i + (float)band / 2f - centerY);
					if (!(num9 >= half))
					{
						float num10 = HexHalf(radius, num9) - 1f;
						float left = Math.Max(num2 + num7, centerX - num10);
						float right = Math.Min(num2 + num8, centerX + num10);
						Span(renderer, left, right, i, band, SaiyanHud.WithAlpha(color3, 0.46f * (1f - num6)), additive: true);
					}
				}
			}
			for (int j = 0; j < 12; j++)
			{
				int num11 = 1400 + j * 97;
				float num12 = (float)((now + j * 331) % num11) / (float)num11;
				float num13 = centerX - radius * 0.8f + (float)(j * 53) % (radius * 1.6f);
				float num14 = bottom - num12 * radius * 1.9f;
				float num15 = MathF.Abs(num14 - centerY);
				if (!(num15 >= half) && !(MathF.Abs(num13 - centerX) > HexHalf(radius, num15) - 2f))
				{
					int num16 = Math.Max(1, (int)MathF.Round(2f * scale));
					Fill(renderer, (int)num13, (int)num14, num16, num16, SaiyanHud.WithAlpha(light, 0.85f * (1f - num12)), additive: true);
				}
			}
		});
		static float Edge(float u, float at0, float atA, float a, float atB, float b, float atTop)
		{
			if (u <= a)
			{
				return at0 + (atA - at0) * (u / a);
			}
			if (u <= b)
			{
				return atA + (atB - atA) * ((u - a) / (b - a));
			}
			return atB + (atTop - atB) * ((u - b) / (1f - b));
		}
	}
}
