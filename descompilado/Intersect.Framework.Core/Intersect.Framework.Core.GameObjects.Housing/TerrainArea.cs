using System;
using Intersect.Framework.Core.GameObjects.Maps.Attributes;

namespace Intersect.Framework.Core.GameObjects.Housing;

public readonly record struct TerrainArea(Guid Id, string Name, TerrainKind Kind, long Price, Guid CapsuleId, int X, int Y, int Width, int Height)
{
	public bool Contains(int x, int y)
	{
		if (x >= X && y >= Y && x < X + Width)
		{
			return y < Y + Height;
		}
		return false;
	}

	public int DistanceTo(int x, int y)
	{
		return Math.Max(Math.Max(X - x, x - (X + Width - 1)), Math.Max(Math.Max(Y - y, y - (Y + Height - 1)), 0));
	}
}
