using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Intersect.Framework.Core.GameObjects.Maps;
using Intersect.Framework.Core.GameObjects.Maps.Attributes;

namespace Intersect.Framework.Core.GameObjects.Housing;

public static class HousingRules
{
	public const int InteractRange = 1;

	private static readonly ConditionalWeakTable<MapAttribute?[,], List<TerrainArea>> TerrainCache = new ConditionalWeakTable<MapAttribute[,], List<TerrainArea>>();

	public const int ResalePercent = 50;

	public const int AbandonDays = 30;

	public static List<TerrainArea> FindTerrains(MapAttribute?[,]? attributes)
	{
		List<TerrainArea> list = new List<TerrainArea>();
		if (attributes == null)
		{
			return list;
		}
		Dictionary<Guid, (MapTerrainAttribute, int, int, int, int)> dictionary = new Dictionary<Guid, (MapTerrainAttribute, int, int, int, int)>();
		int length = attributes.GetLength(0);
		int length2 = attributes.GetLength(1);
		for (int i = 0; i < length; i++)
		{
			for (int j = 0; j < length2; j++)
			{
				if (attributes[i, j] is MapTerrainAttribute mapTerrainAttribute && !(mapTerrainAttribute.TerrainId == Guid.Empty))
				{
					dictionary[mapTerrainAttribute.TerrainId] = (dictionary.TryGetValue(mapTerrainAttribute.TerrainId, out var value) ? (value.Item1, Math.Min(value.Item2, i), Math.Min(value.Item3, j), Math.Max(value.Item4, i), Math.Max(value.Item5, j)) : (mapTerrainAttribute, i, j, i, j));
				}
			}
		}
		foreach (var (id, tuple2) in dictionary)
		{
			list.Add(new TerrainArea(id, tuple2.Item1.Name ?? string.Empty, tuple2.Item1.Kind, tuple2.Item1.Price, tuple2.Item1.CapsuleId, tuple2.Item2, tuple2.Item3, tuple2.Item4 - tuple2.Item2 + 1, tuple2.Item5 - tuple2.Item3 + 1));
		}
		list.Sort((TerrainArea a, TerrainArea b) => (a.Y == b.Y) ? a.X.CompareTo(b.X) : a.Y.CompareTo(b.Y));
		return list;
	}

	public static List<TerrainArea> TerrainsOf(MapAttribute?[,]? attributes)
	{
		if (attributes != null)
		{
			return TerrainCache.GetValue(attributes, FindTerrains);
		}
		return new List<TerrainArea>();
	}

	public static bool TryFindTerrain(MapAttribute?[,]? attributes, Guid terrainId, out TerrainArea terrain)
	{
		foreach (TerrainArea item in TerrainsOf(attributes))
		{
			if (item.Id == terrainId)
			{
				terrain = item;
				return true;
			}
		}
		terrain = default(TerrainArea);
		return false;
	}

	public static long ResalePrice(long paid)
	{
		return Math.Max(0L, paid) * 50 / 100;
	}

	public static bool IsUpgrade(CapsuleDescriptor from, CapsuleDescriptor to)
	{
		if (to.Id != from.Id)
		{
			return to.Price > from.Price;
		}
		return false;
	}

	public static long UpgradePrice(CapsuleDescriptor from, CapsuleDescriptor to)
	{
		return Math.Max(0L, to.Price - Math.Max(0L, from.Price));
	}

	public static (int Dx, int Dy) RoomShift(CapsuleDescriptor from, CapsuleDescriptor to)
	{
		return (Dx: to.RoomX + (to.RoomWidth - from.RoomWidth) / 2 - from.RoomX, Dy: to.RoomY + Math.Max(0, to.WallRows) - (from.RoomY + Math.Max(0, from.WallRows)));
	}

	public static bool CapsuleFits(CapsuleDescriptor capsule, int terrainWidth, int terrainHeight)
	{
		if (capsule.Width <= terrainWidth)
		{
			return capsule.Height + 1 <= terrainHeight;
		}
		return false;
	}

	public static HouseFootprint Footprint(TerrainArea terrain, CapsuleDescriptor capsule, int offsetX = -1, int offsetY = -1)
	{
		int num = Math.Clamp(capsule.Width, 1, 8);
		int num2 = Math.Clamp(capsule.Height, 1, 8);
		(int MaxX, int MaxY) tuple = MaxOffsets(terrain, capsule);
		int item = tuple.MaxX;
		int item2 = tuple.MaxY;
		int num3 = terrain.X + ((offsetX < 0) ? (item / 2) : Math.Min(offsetX, item));
		int num4 = terrain.Y + ((offsetY < 0) ? item2 : Math.Min(offsetY, item2));
		return new HouseFootprint(num3, num4, num, num2, num3 + Math.Clamp(capsule.DoorX, 0, num - 1), num4 + num2 - 1);
	}

	public static (int MaxX, int MaxY) MaxOffsets(TerrainArea terrain, CapsuleDescriptor capsule)
	{
		return (MaxX: Math.Max(0, terrain.Width - Math.Clamp(capsule.Width, 1, 8)), MaxY: Math.Max(0, terrain.Height - 1 - Math.Clamp(capsule.Height, 1, 8)));
	}

	public static bool IsAtDoor(HouseFootprint house, int x, int y)
	{
		if (Math.Abs(x - house.FrontX) <= 1 && Math.Abs(y - house.FrontY) <= 1)
		{
			return !house.Contains(x, y);
		}
		return false;
	}

	public static bool BlocksMovement(FurnitureDescriptor furniture)
	{
		FurniturePlacement placement = furniture.Placement;
		if (placement <= FurniturePlacement.AgainstWall)
		{
			return true;
		}
		return false;
	}

	public static (int X, int Y, int Width, int Height) FurnitureFootprint(FurnitureDescriptor furniture, int variant, int x, int y)
	{
		FurnitureVariant furnitureVariant = furniture.VariantAt(variant);
		int val = ((furniture.Placement == FurniturePlacement.Wall) ? furnitureVariant.Height : Math.Clamp(furnitureVariant.FootHeight, 1, Math.Max(1, furnitureVariant.Height)));
		return (X: x, Y: y, Width: Math.Max(1, furnitureVariant.Width), Height: Math.Max(1, val));
	}

	public static int FurnitureDrawTop(FurnitureDescriptor furniture, int variant, int y)
	{
		if (furniture.Placement == FurniturePlacement.Wall)
		{
			return y;
		}
		FurnitureVariant furnitureVariant = furniture.VariantAt(variant);
		return y - (Math.Max(1, furnitureVariant.Height) - Math.Clamp(furnitureVariant.FootHeight, 1, Math.Max(1, furnitureVariant.Height)));
	}

	public static bool FurnitureImageContains(FurnitureDescriptor furniture, int variant, int x, int y, int cellX, int cellY)
	{
		FurnitureVariant furnitureVariant = furniture.VariantAt(variant);
		int num = FurnitureDrawTop(furniture, variant, y);
		if (cellX >= x && cellX < x + Math.Max(1, furnitureVariant.Width) && cellY >= num)
		{
			return cellY < num + Math.Max(1, furnitureVariant.Height);
		}
		return false;
	}

	private static bool Overlaps((int X, int Y, int Width, int Height) a, (int X, int Y, int Width, int Height) b)
	{
		if (a.X < b.X + b.Width && b.X < a.X + a.Width && a.Y < b.Y + b.Height)
		{
			return b.Y < a.Y + a.Height;
		}
		return false;
	}

	private static bool SameLayer(FurnitureDescriptor a, FurnitureDescriptor b)
	{
		if (a.Placement == FurniturePlacement.Wall == (b.Placement == FurniturePlacement.Wall))
		{
			return a.Placement == FurniturePlacement.Rug == (b.Placement == FurniturePlacement.Rug);
		}
		return false;
	}

	public static FurnitureVerdict Verdict(CapsuleDescriptor capsule, FurnitureDescriptor furniture, int variant, int x, int y, IEnumerable<PlacedFurniture> placed, Guid ignoreId, Func<int, int, bool>? mapBlocked = null, Func<int, int, bool>? isExit = null)
	{
		(int, int, int, int) tuple = FurnitureFootprint(furniture, variant, x, y);
		if (tuple.Item1 < capsule.RoomX || tuple.Item2 < capsule.RoomY || tuple.Item1 + tuple.Item3 > capsule.RoomX + capsule.RoomWidth || tuple.Item2 + tuple.Item4 > capsule.RoomY + capsule.RoomHeight)
		{
			return FurnitureVerdict.OutsideRoom;
		}
		int num = capsule.RoomY + Math.Max(0, capsule.WallRows);
		if (furniture.Placement == FurniturePlacement.Wall)
		{
			if (tuple.Item2 + tuple.Item4 > num)
			{
				return FurnitureVerdict.WallOnly;
			}
		}
		else
		{
			if (tuple.Item2 < num)
			{
				return FurnitureVerdict.FloorOnly;
			}
			if (furniture.Placement == FurniturePlacement.AgainstWall && tuple.Item2 != num)
			{
				return FurnitureVerdict.AgainstWallOnly;
			}
			var (i, _, _, _) = tuple;
			for (; i < tuple.Item1 + tuple.Item3; i++)
			{
				for (int j = tuple.Item2; j < tuple.Item2 + tuple.Item4; j++)
				{
					if (((i == capsule.InteriorX && j == capsule.InteriorY) || (isExit != null && isExit(i, j))) && furniture.Placement != FurniturePlacement.Rug)
					{
						return FurnitureVerdict.KeepEntranceFree;
					}
					if (mapBlocked != null && mapBlocked(i, j))
					{
						return FurnitureVerdict.Occupied;
					}
				}
			}
		}
		foreach (PlacedFurniture item in placed)
		{
			if (!(item.Id == ignoreId) && item.Furniture != null && SameLayer(furniture, item.Furniture) && Overlaps(tuple, FurnitureFootprint(item.Furniture, item.Variant, item.X, item.Y)))
			{
				return FurnitureVerdict.Occupied;
			}
		}
		return FurnitureVerdict.Ok;
	}

	public static RotateOutcome Rotate(FurnitureDescriptor furniture, int variant, bool mirror, out int newVariant, out bool newMirror)
	{
		List<FurnitureVariant> variants = furniture.Variants;
		if (variants != null && variants.Count > 1)
		{
			newVariant = (Math.Max(0, variant) + 1) % furniture.Variants.Count;
			newMirror = false;
			return RotateOutcome.NextVariant;
		}
		if (!furniture.NoMirror)
		{
			newVariant = 0;
			newMirror = !mirror;
			if (!newMirror)
			{
				return RotateOutcome.Unmirrored;
			}
			return RotateOutcome.Mirrored;
		}
		newVariant = variant;
		newMirror = mirror;
		return RotateOutcome.Refused;
	}
}
