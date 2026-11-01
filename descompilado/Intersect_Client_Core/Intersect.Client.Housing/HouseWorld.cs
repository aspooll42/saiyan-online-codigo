using System;
using System.Collections.Generic;
using Intersect.Client.Core;
using Intersect.Client.Entities;
using Intersect.Client.Framework.Content;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.General;
using Intersect.Client.Maps;
using Intersect.Framework.Core;
using Intersect.Framework.Core.GameObjects.Housing;
using Intersect.Framework.Core.GameObjects.Items;
using Intersect.Framework.Core.GameObjects.Mapping.Tilesets;
using Intersect.Models;
using Intersect.Network.Packets.Server;

namespace Intersect.Client.Housing;

public static class HouseWorld
{
	private readonly record struct Drawable(int Row, Action Draw);

	private static readonly List<Drawable> Rows = new List<Drawable>();

	private static int TileWidth => Options.Instance.Map.TileWidth;

	private static int TileHeight => Options.Instance.Map.TileHeight;

	private static float Pixel => 1f / Math.Max(0.25f, Graphics.WorldScale);

	private static void Rect(float x, float y, float width, float height, Color color)
	{
		IGameTexture gameTexture = Graphics.Renderer?.WhitePixel;
		if (gameTexture != null && color.A != 0 && !(width <= 0f) && !(height <= 0f))
		{
			Graphics.DrawGameTexture(gameTexture, new FloatRect(0f, 0f, 1f, 1f), new FloatRect(x, y, width, height), color);
		}
	}

	private static void Outline(float x, float y, float width, float height, float thickness, Color color)
	{
		Rect(x, y, width, thickness, color);
		Rect(x, y + height - thickness, width, thickness, color);
		Rect(x, y + thickness, thickness, height - thickness * 2f, color);
		Rect(x + width - thickness, y + thickness, thickness, height - thickness * 2f, color);
	}

	private static int RowOf(MapInstance map, MapInstance playerMap, int tileY)
	{
		int num = map.GridY - playerMap.GridY;
		if ((num >= -1 && num <= 1) || 1 == 0)
		{
			return Options.Instance.Map.MapHeight * (2 + num) + tileY;
		}
		return -1;
	}

	public static IGameTexture? CapsuleTexture(CapsuleDescriptor capsule)
	{
		if (!string.IsNullOrWhiteSpace(capsule.Texture))
		{
			return Globals.ContentManager.GetTexture(TextureType.Image, capsule.Texture);
		}
		return null;
	}

	public static IGameTexture? TilesetTexture(Guid tilesetId)
	{
		if (!DatabaseObject<TilesetDescriptor>.TryGet(tilesetId, out var databaseObject))
		{
			return null;
		}
		return Globals.ContentManager.GetTexture(TextureType.Tileset, databaseObject.Name);
	}

	public static void DrawFurniture(MapInstance map, FurnitureDescriptor furniture, int variant, bool mirror, int x, int y, float alpha)
	{
		FurnitureVariant furnitureVariant = furniture.VariantAt(variant);
		IGameTexture gameTexture = TilesetTexture(furnitureVariant.TilesetId);
		if (gameTexture != null)
		{
			int num = Math.Max(1, furnitureVariant.Width) * TileWidth;
			int num2 = Math.Max(1, furnitureVariant.Height) * TileHeight;
			FloatRect srcRectangle = (mirror ? new FloatRect(furnitureVariant.X * TileWidth + num, furnitureVariant.Y * TileHeight, -num, num2) : new FloatRect(furnitureVariant.X * TileWidth, furnitureVariant.Y * TileHeight, num, num2));
			int num3 = HousingRules.FurnitureDrawTop(furniture, variant, y);
			Color renderColor = new Color((int)(255f * Math.Clamp(alpha, 0f, 1f)), 255, 255, 255);
			Graphics.DrawGameTexture(gameTexture, srcRectangle, new FloatRect(map.X + x * TileWidth, map.Y + num3 * TileHeight, num, num2), renderColor);
		}
	}

	public static void DrawGround()
	{
		Rows.Clear();
		Player me = Globals.Me;
		if (me == null || !MapInstance.TryGet(me.MapId, out MapInstance instance))
		{
			return;
		}
		long millisecondsUtc = Timing.Global.MillisecondsUtc;
		Guid[,] array = Globals.MapGrid ?? new Guid[0, 0];
		foreach (Guid guid in array)
		{
			if (guid == Guid.Empty || !MapInstance.TryGet(guid, out MapInstance map))
			{
				continue;
			}
			foreach (Houses.Plot item in Houses.PlotsOf(map))
			{
				CapsuleDescriptor capsule = item.Capsule;
				if (capsule == null)
				{
					continue;
				}
				IGameTexture texture = CapsuleTexture(capsule);
				if (texture == null)
				{
					continue;
				}
				int num = RowOf(map, instance, item.Footprint.Y + item.Footprint.Height - 1);
				if (num >= 0)
				{
					Guid? guid2 = item.State?.OwnerId;
					Guid id = me.Id;
					float alpha = ((guid2.HasValue && guid2.GetValueOrDefault() == id && millisecondsUtc - Houses.BoughtAt < 500) ? ((float)(millisecondsUtc - Houses.BoughtAt) / 500f) : 1f);
					HouseFootprint footprint = item.Footprint;
					Rows.Add(new Drawable(num, delegate
					{
						int num7 = map.Y + (footprint.Y + footprint.Height) * TileHeight;
						float num8 = (float)map.X + ((float)footprint.X + (float)footprint.Width / 2f) * (float)TileWidth;
						Graphics.DrawGameTexture(texture, new FloatRect(0f, 0f, texture.Width, texture.Height), new FloatRect(MathF.Round(num8 - (float)texture.Width / 2f), num7 - texture.Height, texture.Width, texture.Height), new Color((int)(255f * Math.Clamp(alpha, 0f, 1f)), 255, 255, 255));
						CityLife.DrawHouseWindows(texture, MathF.Round(num8 - (float)texture.Width / 2f), num7 - texture.Height);
					}));
				}
			}
		}
		if (!Houses.InHouse)
		{
			return;
		}
		HouseInteriorPacket interior = Houses.Interior;
		if (interior == null || !MapInstance.TryGet(interior.MapId, out MapInstance room))
		{
			return;
		}
		if (Decoration.Active)
		{
			CapsuleDescriptor interiorCapsule = Houses.InteriorCapsule;
			if (interiorCapsule != null)
			{
				DrawGrid(room, interiorCapsule);
			}
		}
		Guid? guid3 = Decoration.HeldFromRoom?.Id;
		FurniturePlacement[] array2 = new FurniturePlacement[2]
		{
			FurniturePlacement.Rug,
			FurniturePlacement.Wall
		};
		foreach (FurniturePlacement furniturePlacement in array2)
		{
			HouseFurnitureInfo[] furniture = interior.Furniture;
			foreach (HouseFurnitureInfo houseFurnitureInfo in furniture)
			{
				if (!(houseFurnitureInfo.Id == guid3))
				{
					FurnitureDescriptor furnitureDescriptor = DatabaseObject<ItemDescriptor>.Get(houseFurnitureInfo.ItemId)?.Furniture;
					if (furnitureDescriptor != null && furnitureDescriptor.Placement == furniturePlacement)
					{
						DrawFurniture(room, furnitureDescriptor, houseFurnitureInfo.Variant, houseFurnitureInfo.Mirror, houseFurnitureInfo.X, houseFurnitureInfo.Y, 1f);
					}
				}
			}
		}
		HouseFurnitureInfo[] furniture2 = interior.Furniture;
		FurnitureDescriptor descriptor = default(FurnitureDescriptor);
		foreach (HouseFurnitureInfo houseFurnitureInfo2 in furniture2)
		{
			int num5;
			if (!(houseFurnitureInfo2.Id == guid3))
			{
				descriptor = DatabaseObject<ItemDescriptor>.Get(houseFurnitureInfo2.ItemId)?.Furniture;
				num5 = ((descriptor == null) ? 1 : 0);
			}
			else
			{
				num5 = 1;
			}
			bool flag = (byte)num5 != 0;
			bool flag2 = flag;
			if (!flag2)
			{
				FurniturePlacement placement = descriptor.Placement;
				bool flag3 = placement - 2 <= FurniturePlacement.AgainstWall;
				flag2 = flag3;
			}
			if (flag2)
			{
				continue;
			}
			(int, int, int, int) tuple = HousingRules.FurnitureFootprint(descriptor, houseFurnitureInfo2.Variant, houseFurnitureInfo2.X, houseFurnitureInfo2.Y);
			int num6 = RowOf(room, instance, tuple.Item2 + tuple.Item4 - 1);
			if (num6 >= 0)
			{
				HouseFurnitureInfo captured = houseFurnitureInfo2;
				Rows.Add(new Drawable(num6, delegate
				{
					DrawFurniture(room, descriptor, captured.Variant, captured.Mirror, captured.X, captured.Y, 1f);
				}));
			}
		}
	}

	private static void DrawGrid(MapInstance room, CapsuleDescriptor capsule)
	{
		float pixel = Pixel;
		int num = room.X + capsule.RoomX * TileWidth;
		int num2 = room.Y + capsule.RoomY * TileHeight;
		int num3 = capsule.RoomWidth * TileWidth;
		int num4 = capsule.RoomHeight * TileHeight;
		Color color = new Color(26, 255, 255, 255);
		for (int i = 1; i < capsule.RoomWidth; i++)
		{
			Rect(num + i * TileWidth, num2, pixel, num4, color);
		}
		for (int j = 1; j < capsule.RoomHeight; j++)
		{
			Rect(num, num2 + j * TileHeight, num3, pixel, color);
		}
		int num5 = num2 + Math.Max(0, capsule.WallRows) * TileHeight;
		float num6 = 6f * pixel;
		Color color2 = new Color(115, 255, 179, 92);
		for (float num7 = 0f; num7 < (float)num3; num7 += num6 * 2f)
		{
			Rect((float)num + num7, num5, Math.Min(num6, (float)num3 - num7), pixel, color2);
		}
	}

	public static void DrawRow(int row)
	{
		foreach (Drawable row2 in Rows)
		{
			if (row2.Row == row)
			{
				row2.Draw();
			}
		}
	}

	public static void DrawOverlay()
	{
		long millisecondsUtc = Timing.Global.MillisecondsUtc;
		if (millisecondsUtc - Houses.BoughtAt < 600)
		{
			float num = 1f - (float)(millisecondsUtc - Houses.BoughtAt) / 600f;
			FloatRect worldViewport = Graphics.WorldViewport;
			Rect(worldViewport.X, worldViewport.Y, worldViewport.Width, worldViewport.Height, new Color((int)(191f * num), 255, 255, 255));
		}
		if (!Decoration.Active)
		{
			return;
		}
		HouseInteriorPacket interior = Houses.Interior;
		if (interior == null || !MapInstance.TryGet(interior.MapId, out MapInstance instance))
		{
			return;
		}
		float pixel = Pixel;
		FurnitureDescriptor heldDescriptor = Decoration.HeldDescriptor;
		if (heldDescriptor != null)
		{
			(int, int)? cursor = Decoration.Cursor;
			if (cursor.HasValue)
			{
				(int, int) valueOrDefault = cursor.GetValueOrDefault();
				bool flag = Decoration.CurrentVerdict() != FurnitureVerdict.Ok;
				(int, int, int, int) tuple = HousingRules.FurnitureFootprint(heldDescriptor, Decoration.Variant, valueOrDefault.Item1, valueOrDefault.Item2);
				int num2 = instance.X + tuple.Item1 * TileWidth;
				int num3 = instance.Y + tuple.Item2 * TileHeight;
				int num4 = tuple.Item3 * TileWidth;
				int num5 = tuple.Item4 * TileHeight;
				Rect(num2, num3, num4, num5, flag ? new Color(71, 255, 85, 102) : new Color(66, 111, 227, 161));
				Outline(num2, num3, num4, num5, 3f * pixel, flag ? new Color(242, 255, 85, 102) : new Color(242, 111, 227, 161));
				DrawFurniture(instance, heldDescriptor, Decoration.Variant, Decoration.Mirror, valueOrDefault.Item1, valueOrDefault.Item2, 0.72f);
				return;
			}
		}
		HouseFurnitureInfo hovered = Decoration.Hovered;
		if (hovered != null)
		{
			FurnitureDescriptor furnitureDescriptor = DatabaseObject<ItemDescriptor>.Get(hovered.ItemId)?.Furniture;
			if (furnitureDescriptor != null)
			{
				FurnitureVariant furnitureVariant = furnitureDescriptor.VariantAt(hovered.Variant);
				int num6 = HousingRules.FurnitureDrawTop(furnitureDescriptor, hovered.Variant, hovered.Y);
				Outline(instance.X + hovered.X * TileWidth, instance.Y + num6 * TileHeight, Math.Max(1, furnitureVariant.Width) * TileWidth, Math.Max(1, furnitureVariant.Height) * TileHeight, 2f * pixel, new Color(230, 255, 208, 106));
			}
		}
	}
}
