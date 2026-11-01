using System;

namespace Intersect.Framework.Core.GameObjects.Lighting;

public class LightDescriptor
{
	public Color Color { get; set; }

	public float Expand { get; set; }

	public byte Intensity { get; set; }

	public int OffsetX { get; set; }

	public int OffsetY { get; set; }

	public int Size { get; set; }

	public int TileX { get; set; }

	public int TileY { get; set; }

	public LightDescriptor()
		: this(-1, -1)
	{
	}

	public LightDescriptor(int x, int y)
	{
		TileX = x;
		TileY = y;
		Color = Color.White;
		Intensity = byte.MaxValue;
	}

	public LightDescriptor(LightDescriptor copy)
	{
		TileX = copy.TileX;
		TileY = copy.TileY;
		OffsetX = copy.OffsetX;
		OffsetY = copy.OffsetY;
		Intensity = copy.Intensity;
		Size = copy.Size;
		Expand = copy.Expand;
		Color = Color.FromArgb(copy.Color.R, copy.Color.G, copy.Color.B);
	}

	public LightDescriptor(int tileX, int tileY, int offsetX, int offsetY, byte intensity, int size, float expand, Color color)
	{
		TileX = tileX;
		TileY = tileY;
		OffsetX = offsetX;
		OffsetY = offsetY;
		Intensity = intensity;
		Size = size;
		Expand = expand;
		Color = color;
	}

	public override int GetHashCode()
	{
		return HashCode.Combine(Intensity, Expand, Color, TileX, TileY);
	}

	public override bool Equals(object obj)
	{
		if (obj is LightDescriptor lightDescriptor)
		{
			if (Intensity == lightDescriptor.Intensity && Color == lightDescriptor.Color && Expand == lightDescriptor.Expand && TileX == lightDescriptor.TileX)
			{
				return TileY == lightDescriptor.TileY;
			}
			return false;
		}
		return false;
	}
}
