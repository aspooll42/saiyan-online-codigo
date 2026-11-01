using System;
using System.Collections.Generic;
using System.Linq;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.Maps;

namespace Intersect.Client.Entities;

public static class RunStride
{
	public const float CycleTiles = 4f;

	public const long MinFrameMs = 135L;

	private const float LeanPixels = 1.5f;

	private const long LeanInMs = 150L;

	private const int HeadRows = 16;

	private const int SearchX = 4;

	private const int SearchY = 6;

	private const int MaxOffset = 3;

	private static readonly Dictionary<string, (int X, int Y)[]> Offsets = new Dictionary<string, (int, int)[]>();

	public static (int X, int Y) Offset(IGameTexture texture, FloatRect source, int frame, int frames)
	{
		if (frames < 2 || frame < 0 || frame >= frames || source.Width < 1f || source.Height < 1f)
		{
			return (X: 0, Y: 0);
		}
		int num = (int)source.Width;
		int num2 = (int)source.Height;
		int num3 = (int)source.X - frame * num;
		int num4 = (int)source.Y;
		string key = $"{texture.Name}:{num3},{num4},{num}x{num2}:{frames}";
		if (!Offsets.TryGetValue(key, out (int, int)[] value))
		{
			value = Measure(texture, num3, num4, num, num2, frames);
			Offsets[key] = value;
		}
		return value[frame];
	}

	public static float Lean(int spriteRow, long runningSince, long now)
	{
		return spriteRow switch
		{
			1 => -1f, 
			2 => 1f, 
			_ => 0f, 
		} * 1.5f * Math.Clamp((float)(now - runningSince) / 150f, 0f, 1f);
	}

	private static (int X, int Y)[] Measure(IGameTexture texture, int originX, int originY, int width, int height, int frames)
	{
		(int, int)[] array = new(int, int)[frames];
		if (originX < 0 || originY < 0 || originX + frames * width > texture.Width || originY + height > texture.Height || !PixelsPlanche.Lire(texture, out Color[] pixels) || pixels.Length < texture.Width * texture.Height)
		{
			return array;
		}
		int stride = texture.Width;
		int num = height;
		for (int i = 0; i < height; i++)
		{
			if (num != height)
			{
				break;
			}
			for (int j = 0; j < width; j++)
			{
				if (Pixel(0, j, i) != null)
				{
					num = i;
					break;
				}
			}
		}
		if (num == height)
		{
			return array;
		}
		(int, int)[] array2 = new(int, int)[frames];
		for (int k = 1; k < frames; k++)
		{
			(int, int) tuple = (0, 0);
			float num2 = float.MaxValue;
			for (int l = -6; l <= 6; l++)
			{
				for (int m = -4; m <= 4; m++)
				{
					float num3 = 0f;
					int num4 = 0;
					for (int n = num; n < Math.Min(height, num + 16); n++)
					{
						for (int num5 = 0; num5 < width; num5++)
						{
							Color color = Pixel(0, num5, n);
							if ((object)color != null)
							{
								num4++;
								float num6 = num3;
								Color color2 = Pixel(k, num5 - m, n - l);
								num3 = num6 + (float)(((object)color2 != null) ? (Math.Abs(color.R - color2.R) + Math.Abs(color.G - color2.G) + Math.Abs(color.B - color2.B)) : 300);
							}
						}
					}
					float num7 = num3 / (float)Math.Max(1, num4) + (float)(Math.Abs(m) + Math.Abs(l)) * 0.5f;
					if (num7 < num2)
					{
						num2 = num7;
						tuple = (m, l);
					}
				}
			}
			array2[k] = tuple;
		}
		double num8 = array2.Average(((int X, int Y) offset) => offset.X);
		double num9 = array2.Average(((int X, int Y) offset) => offset.Y);
		for (int num10 = 0; num10 < frames; num10++)
		{
			array[num10] = (Math.Clamp((int)Math.Floor((double)array2[num10].Item1 - num8 + 0.5), -3, 3), Math.Clamp((int)Math.Floor((double)array2[num10].Item2 - num9 + 0.5), -3, 3));
		}
		return array;
		Color? Pixel(int frame, int x, int y)
		{
			if (x < 0 || y < 0 || x >= width || y >= height)
			{
				return null;
			}
			Color color3 = pixels[(originY + y) * stride + originX + frame * width + x];
			if (color3.A >= 128)
			{
				return color3;
			}
			return null;
		}
	}
}
