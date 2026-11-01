using System.Collections.Generic;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.Maps;

namespace Intersect.Client.Entities;

internal static class SpriteMeasure
{
	private static readonly Dictionary<string, int> TopMargins = new Dictionary<string, int>();

	public static int TopMargin(IGameTexture? texture, int columns, int rows)
	{
		if (texture == null || columns < 1 || rows < 1)
		{
			return 0;
		}
		string key = $"{texture.Name}:{columns}x{rows}";
		if (TopMargins.TryGetValue(key, out var value))
		{
			return value;
		}
		value = Measure(texture, columns, rows);
		TopMargins[key] = value;
		return value;
	}

	private static int Measure(IGameTexture texture, int columns, int rows)
	{
		if (!PixelsPlanche.Lire(texture, out Color[] pixels) || pixels.Length < texture.Width * texture.Height)
		{
			return 0;
		}
		int num = texture.Width / columns;
		int num2 = texture.Height / rows;
		if (num < 1 || num2 < 1)
		{
			return 0;
		}
		int num3 = num2;
		for (int i = 0; i < rows; i++)
		{
			for (int j = 0; j < columns; j++)
			{
				int num4 = FrameTop(pixels, texture.Width, j * num, i * num2, num, num2);
				if (num4 < num3)
				{
					num3 = num4;
				}
			}
		}
		if (num3 < num2)
		{
			return num3;
		}
		return 0;
	}

	private static int FrameTop(Color[] pixels, int stride, int originX, int originY, int width, int height)
	{
		for (int i = 0; i < height; i++)
		{
			int num = (originY + i) * stride + originX;
			for (int j = 0; j < width; j++)
			{
				if (pixels[num + j].A > 8)
				{
					return i;
				}
			}
		}
		return height;
	}
}
