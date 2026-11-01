using System;
using System.Collections.Generic;
using Intersect.Core;
using Microsoft.Extensions.Logging;

namespace Intersect.Framework.Core.GameObjects.Maps;

public class MapAutotiles
{
	public const byte AUTO_TILE_FILL = 5;

	public const byte AUTO_TILE_HORIZONTAL = 3;

	public const byte AUTO_TILE_INNER = 1;

	public const byte AUTO_TILE_OUTER = 2;

	public const byte AUTO_TILE_VERTICAL = 4;

	public const byte AUTOTILE_ANIM = 3;

	public const byte AUTOTILE_ANIM_XP = 7;

	public const byte AUTOTILE_CLIFF = 4;

	public const byte AUTOTILE_FAKE = 2;

	public const byte AUTOTILE_NONE = 0;

	public const byte AUTOTILE_NORMAL = 1;

	public const byte AUTOTILE_WATERFALL = 5;

	public const byte AUTOTILE_XP = 6;

	public const byte RENDER_STATE_AUTOTILE = 2;

	public const byte RENDER_STATE_NONE = 0;

	public const byte RENDER_STATE_NORMAL = 1;

	public const byte XP_FILL = 1;

	public const byte XP_INNER = 2;

	public const byte XP_NE = 5;

	public const byte XP_NW = 3;

	public const byte XP_SE = 7;

	public const byte XP_SW = 9;

	public const byte XPE = 6;

	public const byte XPN = 4;

	public const byte XPS = 8;

	public const byte XPW = 10;

	public static Point16i[] AutoCxp = new Point16i[6];

	public static Point16i[] AutoExp = new Point16i[6];

	public static Point16i[] AutoInner = new Point16i[6];

	public static Point16i[] AutoInnerXp = new Point16i[6];

	public static Point16i[] AutoNe = new Point16i[6];

	public static Point16i[] AutoNeXp = new Point16i[6];

	public static Point16i[] AutoNw = new Point16i[6];

	public static Point16i[] AutoNwXp = new Point16i[6];

	public static Point16i[] AutoNxp = new Point16i[6];

	public static Point16i[] AutoSe = new Point16i[6];

	public static Point16i[] AutoSeXp = new Point16i[6];

	public static Point16i[] AutoSw = new Point16i[6];

	public static Point16i[] AutoSwXp = new Point16i[6];

	public static Point16i[] AutoSxp = new Point16i[6];

	public static Point16i[] AutoWxp = new Point16i[6];

	private static bool sLoadedTemplates = false;

	private readonly MapDescriptor mMyMap;

	public Dictionary<string, Autotile[,]> Layers;

	public MapAutotiles(MapDescriptor map)
	{
		mMyMap = map;
		if (!sLoadedTemplates)
		{
			InitVxAutotileTemplate();
			InitXpAutotileTemplate();
		}
	}

	private void InitVxAutotileTemplate()
	{
		AutoInner[1].X = (short)Options.Instance.Map.TileWidth;
		AutoInner[1].Y = 0;
		AutoInner[2].X = (short)(2 * Options.Instance.Map.TileWidth - Options.Instance.Map.TileWidth / 2);
		AutoInner[2].Y = 0;
		AutoInner[3].X = (short)Options.Instance.Map.TileWidth;
		AutoInner[3].Y = (short)(Options.Instance.Map.TileHeight / 2);
		AutoInner[4].X = (short)(2 * Options.Instance.Map.TileWidth - Options.Instance.Map.TileWidth / 2);
		AutoInner[4].Y = (short)(Options.Instance.Map.TileHeight / 2);
		AutoNw[1].X = 0;
		AutoNw[1].Y = (short)Options.Instance.Map.TileHeight;
		AutoNw[2].X = (short)(Options.Instance.Map.TileWidth / 2);
		AutoNw[2].Y = (short)Options.Instance.Map.TileHeight;
		AutoNw[3].X = 0;
		AutoNw[3].Y = (short)(2 * Options.Instance.Map.TileHeight - Options.Instance.Map.TileHeight / 2);
		AutoNw[4].X = (short)(Options.Instance.Map.TileWidth / 2);
		AutoNw[4].Y = (short)(2 * Options.Instance.Map.TileHeight - Options.Instance.Map.TileHeight / 2);
		AutoNe[1].X = (short)Options.Instance.Map.TileWidth;
		AutoNe[1].Y = (short)Options.Instance.Map.TileHeight;
		AutoNe[2].X = (short)(2 * Options.Instance.Map.TileWidth - Options.Instance.Map.TileWidth / 2);
		AutoNe[2].Y = (short)Options.Instance.Map.TileHeight;
		AutoNe[3].X = (short)Options.Instance.Map.TileWidth;
		AutoNe[3].Y = (short)(2 * Options.Instance.Map.TileHeight - Options.Instance.Map.TileHeight / 2);
		AutoNe[4].X = (short)(2 * Options.Instance.Map.TileWidth - Options.Instance.Map.TileWidth / 2);
		AutoNe[4].Y = (short)(2 * Options.Instance.Map.TileHeight - Options.Instance.Map.TileHeight / 2);
		AutoSw[1].X = 0;
		AutoSw[1].Y = (short)(2 * Options.Instance.Map.TileHeight);
		AutoSw[2].X = (short)(Options.Instance.Map.TileWidth / 2);
		AutoSw[2].Y = (short)(2 * Options.Instance.Map.TileHeight);
		AutoSw[3].X = 0;
		AutoSw[3].Y = (short)(2 * Options.Instance.Map.TileHeight + Options.Instance.Map.TileHeight / 2);
		AutoSw[4].X = (short)(Options.Instance.Map.TileWidth / 2);
		AutoSw[4].Y = (short)(2 * Options.Instance.Map.TileHeight + Options.Instance.Map.TileHeight / 2);
		AutoSe[1].X = (short)Options.Instance.Map.TileWidth;
		AutoSe[1].Y = (short)(2 * Options.Instance.Map.TileHeight);
		AutoSe[2].X = (short)(2 * Options.Instance.Map.TileWidth - Options.Instance.Map.TileWidth / 2);
		AutoSe[2].Y = (short)(2 * Options.Instance.Map.TileHeight);
		AutoSe[3].X = (short)Options.Instance.Map.TileWidth;
		AutoSe[3].Y = (short)(2 * Options.Instance.Map.TileHeight + Options.Instance.Map.TileHeight / 2);
		AutoSe[4].X = (short)(2 * Options.Instance.Map.TileWidth - Options.Instance.Map.TileWidth / 2);
		AutoSe[4].Y = (short)(2 * Options.Instance.Map.TileHeight + Options.Instance.Map.TileHeight / 2);
	}

	private void InitXpAutotileTemplate()
	{
		AutoInnerXp[1].X = (short)(Options.Instance.Map.TileWidth * 2);
		AutoInnerXp[1].Y = 0;
		AutoInnerXp[2].X = (short)(2 * Options.Instance.Map.TileWidth + Options.Instance.Map.TileWidth / 2);
		AutoInnerXp[2].Y = 0;
		AutoInnerXp[3].X = (short)(Options.Instance.Map.TileWidth * 2);
		AutoInnerXp[3].Y = (short)(Options.Instance.Map.TileHeight / 2);
		AutoInnerXp[4].X = (short)(2 * Options.Instance.Map.TileWidth + Options.Instance.Map.TileWidth / 2);
		AutoInnerXp[4].Y = (short)(Options.Instance.Map.TileHeight / 2);
		AutoNwXp[1].X = 0;
		AutoNwXp[1].Y = (short)Options.Instance.Map.TileHeight;
		AutoNwXp[2].X = (short)(Options.Instance.Map.TileWidth / 2);
		AutoNwXp[2].Y = (short)Options.Instance.Map.TileHeight;
		AutoNwXp[3].X = 0;
		AutoNwXp[3].Y = (short)(Options.Instance.Map.TileHeight + Options.Instance.Map.TileHeight / 2);
		AutoNwXp[4].X = (short)(Options.Instance.Map.TileWidth / 2);
		AutoNwXp[4].Y = (short)(Options.Instance.Map.TileHeight + Options.Instance.Map.TileHeight / 2);
		AutoNeXp[1].X = (short)(Options.Instance.Map.TileWidth * 2);
		AutoNeXp[1].Y = (short)Options.Instance.Map.TileHeight;
		AutoNeXp[2].X = (short)(2 * Options.Instance.Map.TileWidth + Options.Instance.Map.TileWidth / 2);
		AutoNeXp[2].Y = (short)Options.Instance.Map.TileHeight;
		AutoNeXp[3].X = (short)(Options.Instance.Map.TileWidth * 2);
		AutoNeXp[3].Y = (short)(Options.Instance.Map.TileHeight + Options.Instance.Map.TileHeight / 2);
		AutoNeXp[4].X = (short)(2 * Options.Instance.Map.TileWidth + Options.Instance.Map.TileWidth / 2);
		AutoNeXp[4].Y = (short)(Options.Instance.Map.TileHeight + Options.Instance.Map.TileHeight / 2);
		AutoSwXp[1].X = 0;
		AutoSwXp[1].Y = (short)(3 * Options.Instance.Map.TileHeight);
		AutoSwXp[2].X = (short)(Options.Instance.Map.TileWidth / 2);
		AutoSwXp[2].Y = (short)(3 * Options.Instance.Map.TileHeight);
		AutoSwXp[3].X = 0;
		AutoSwXp[3].Y = (short)(3 * Options.Instance.Map.TileHeight + Options.Instance.Map.TileHeight / 2);
		AutoSwXp[4].X = (short)(Options.Instance.Map.TileWidth / 2);
		AutoSwXp[4].Y = (short)(3 * Options.Instance.Map.TileHeight + Options.Instance.Map.TileHeight / 2);
		AutoSeXp[1].X = (short)(Options.Instance.Map.TileWidth * 2);
		AutoSeXp[1].Y = (short)(3 * Options.Instance.Map.TileHeight);
		AutoSeXp[2].X = (short)(2 * Options.Instance.Map.TileWidth + Options.Instance.Map.TileWidth / 2);
		AutoSeXp[2].Y = (short)(3 * Options.Instance.Map.TileHeight);
		AutoSeXp[3].X = (short)(Options.Instance.Map.TileWidth * 2);
		AutoSeXp[3].Y = (short)(3 * Options.Instance.Map.TileHeight + Options.Instance.Map.TileHeight / 2);
		AutoSeXp[4].X = (short)(2 * Options.Instance.Map.TileWidth + Options.Instance.Map.TileWidth / 2);
		AutoSeXp[4].Y = (short)(3 * Options.Instance.Map.TileHeight + Options.Instance.Map.TileHeight / 2);
		AutoCxp[1].X = (short)Options.Instance.Map.TileWidth;
		AutoCxp[1].Y = (short)(Options.Instance.Map.TileHeight * 2);
		AutoCxp[2].X = (short)(Options.Instance.Map.TileWidth + Options.Instance.Map.TileWidth / 2);
		AutoCxp[2].Y = (short)(Options.Instance.Map.TileHeight * 2);
		AutoCxp[3].X = (short)Options.Instance.Map.TileWidth;
		AutoCxp[3].Y = (short)(Options.Instance.Map.TileHeight * 2 + Options.Instance.Map.TileHeight / 2);
		AutoCxp[4].X = (short)(Options.Instance.Map.TileWidth + Options.Instance.Map.TileWidth / 2);
		AutoCxp[4].Y = (short)(Options.Instance.Map.TileHeight * 2 + Options.Instance.Map.TileHeight / 2);
		AutoNxp[1].X = (short)Options.Instance.Map.TileWidth;
		AutoNxp[1].Y = (short)Options.Instance.Map.TileHeight;
		AutoNxp[2].X = (short)(Options.Instance.Map.TileWidth + Options.Instance.Map.TileWidth / 2);
		AutoNxp[2].Y = (short)Options.Instance.Map.TileHeight;
		AutoNxp[3].X = (short)Options.Instance.Map.TileWidth;
		AutoNxp[3].Y = (short)(Options.Instance.Map.TileHeight + Options.Instance.Map.TileHeight / 2);
		AutoNxp[4].X = (short)(Options.Instance.Map.TileWidth + Options.Instance.Map.TileWidth / 2);
		AutoNxp[4].Y = (short)(Options.Instance.Map.TileHeight + Options.Instance.Map.TileHeight / 2);
		AutoExp[1].X = (short)(Options.Instance.Map.TileWidth * 2);
		AutoExp[1].Y = (short)(Options.Instance.Map.TileHeight * 2);
		AutoExp[2].X = (short)(Options.Instance.Map.TileWidth * 2 + Options.Instance.Map.TileWidth / 2);
		AutoExp[2].Y = (short)(Options.Instance.Map.TileHeight * 2);
		AutoExp[3].X = (short)(Options.Instance.Map.TileWidth * 2);
		AutoExp[3].Y = (short)(Options.Instance.Map.TileHeight * 2 + Options.Instance.Map.TileHeight / 2);
		AutoExp[4].X = (short)(Options.Instance.Map.TileWidth * 2 + Options.Instance.Map.TileWidth / 2);
		AutoExp[4].Y = (short)(Options.Instance.Map.TileHeight * 2 + Options.Instance.Map.TileHeight / 2);
		AutoWxp[1].X = 0;
		AutoWxp[1].Y = (short)(Options.Instance.Map.TileHeight * 2);
		AutoWxp[2].X = (short)(Options.Instance.Map.TileWidth / 2);
		AutoWxp[2].Y = (short)(Options.Instance.Map.TileHeight * 2);
		AutoWxp[3].X = 0;
		AutoWxp[3].Y = (short)(Options.Instance.Map.TileHeight * 2 + Options.Instance.Map.TileHeight / 2);
		AutoWxp[4].X = (short)(Options.Instance.Map.TileWidth / 2);
		AutoWxp[4].Y = (short)(Options.Instance.Map.TileHeight * 2 + Options.Instance.Map.TileHeight / 2);
		AutoSxp[1].X = (short)Options.Instance.Map.TileWidth;
		AutoSxp[1].Y = (short)(Options.Instance.Map.TileHeight * 3);
		AutoSxp[2].X = (short)(Options.Instance.Map.TileWidth + Options.Instance.Map.TileWidth / 2);
		AutoSxp[2].Y = (short)(Options.Instance.Map.TileHeight * 3);
		AutoSxp[3].X = (short)Options.Instance.Map.TileWidth;
		AutoSxp[3].Y = (short)(Options.Instance.Map.TileHeight * 3 + Options.Instance.Map.TileHeight / 2);
		AutoSxp[4].X = (short)(Options.Instance.Map.TileWidth + Options.Instance.Map.TileWidth / 2);
		AutoSxp[4].Y = (short)(Options.Instance.Map.TileHeight * 3 + Options.Instance.Map.TileHeight / 2);
	}

	private void CreateFields()
	{
		Layers = new Dictionary<string, Autotile[,]>();
		foreach (string item in Options.Instance.Map.Layers.All)
		{
			Autotile[,] array = new Autotile[Options.Instance.Map.MapWidth, Options.Instance.Map.MapHeight];
			for (int i = 0; i < Options.Instance.Map.MapWidth; i++)
			{
				for (int j = 0; j < Options.Instance.Map.MapHeight; j++)
				{
					array[i, j] = new Autotile
					{
						QuarterTile = new Point16i[5]
					};
				}
			}
			Layers.Add(item, array);
		}
	}

	public void InitAutotiles(MapDescriptor[,] surroundingMaps)
	{
		lock (mMyMap.MapLock)
		{
			if (Layers == null)
			{
				CreateFields();
			}
			foreach (string item in Options.Instance.Map.Layers.All)
			{
				for (int i = 0; i < Options.Instance.Map.MapWidth; i++)
				{
					for (int j = 0; j < Options.Instance.Map.MapHeight; j++)
					{
						CalculateAutotile(i, j, item, surroundingMaps);
						CacheRenderState(i, j, item);
					}
				}
			}
		}
	}

	public bool UpdateAutoTiles(int x, int y, MapDescriptor[,] surroundingMaps)
	{
		bool result = false;
		lock (mMyMap.MapLock)
		{
			foreach (string item in Options.Instance.Map.Layers.All)
			{
				for (int i = x - 1; i < x + 2; i++)
				{
					if (i < 0 || i >= Options.Instance.Map.MapWidth)
					{
						continue;
					}
					for (int j = y - 1; j < y + 2; j++)
					{
						if (j >= 0 && j < Options.Instance.Map.MapHeight)
						{
							Autotile quarterTile = Layers[item][i, j].Copy();
							CalculateAutotile(i, j, item, surroundingMaps);
							CacheRenderState(i, j, item);
							if (!Layers[item][i, j].Equals(quarterTile))
							{
								result = true;
							}
						}
					}
				}
			}
			return result;
		}
	}

	public void UpdateAutoTiles(int x, int y, string layerName, MapDescriptor[,] surroundingMaps)
	{
		lock (mMyMap.MapLock)
		{
			for (int i = x - 1; i < x + 2; i++)
			{
				if (i < 0 || i >= Options.Instance.Map.MapWidth)
				{
					continue;
				}
				for (int j = y - 1; j < y + 2; j++)
				{
					if (j >= 0 && j < Options.Instance.Map.MapHeight)
					{
						CalculateAutotile(i, j, layerName, surroundingMaps);
						CacheRenderState(i, j, layerName);
					}
				}
			}
		}
	}

	public void UpdateCliffAutotiles(MapDescriptor curMap, string layerName)
	{
		if (!curMap.Layers.ContainsKey(layerName))
		{
			return;
		}
		MapDescriptor[,] array = curMap.GenerateAutotileGrid();
		foreach (MapDescriptor mapDescriptor in array)
		{
			if (mapDescriptor == null)
			{
				continue;
			}
			for (int k = 0; k < Options.Instance.Map.MapWidth; k++)
			{
				for (int l = 0; l < Options.Instance.Map.MapHeight; l++)
				{
					if (mapDescriptor.Layers[layerName][k, l].Autotile == 4)
					{
						mapDescriptor.Autotiles.CalculateAutotile(k, l, layerName, mapDescriptor.GenerateAutotileGrid());
						mapDescriptor.Autotiles.CacheRenderState(k, l, layerName);
					}
				}
			}
		}
	}

	public bool UpdateAutoTile(int x, int y, string layerName, MapDescriptor[,] surroundingMaps)
	{
		if (x < 0 || x >= Options.Instance.Map.MapWidth || y < 0 || y >= Options.Instance.Map.MapHeight)
		{
			return false;
		}
		Autotile quarterTile = Layers[layerName][x, y].Copy();
		lock (mMyMap.MapLock)
		{
			CalculateAutotile(x, y, layerName, surroundingMaps);
			CacheRenderState(x, y, layerName);
		}
		return !Layers[layerName][x, y].Equals(quarterTile);
	}

	public void CacheRenderState(int x, int y, string layerName)
	{
		if (x < 0 || x > Options.Instance.Map.MapWidth || y < 0 || y > Options.Instance.Map.MapHeight)
		{
			return;
		}
		if (mMyMap == null)
		{
			ApplicationContext.Context.Value?.Logger.LogError("mMyMap=null");
		}
		else if (mMyMap.Layers == null)
		{
			ApplicationContext.Context.Value?.Logger.LogError("Layers=null");
		}
		else
		{
			if (!mMyMap.Layers.ContainsKey(layerName))
			{
				return;
			}
			Tile[,] array = mMyMap.Layers[layerName];
			if (mMyMap.Layers[layerName] == null)
			{
				ApplicationContext.Context.Value?.Logger.LogError("layer=null");
				return;
			}
			Tile tile = array[x, y];
			Autotile autotile = Layers[layerName][x, y];
			if (tile.Autotile == 0 || tile.Autotile == 2)
			{
				autotile.RenderState = 1;
				return;
			}
			autotile.RenderState = 2;
			for (int i = 1; i < 5; i++)
			{
				autotile.QuarterTile[i].X = (short)(tile.X * Options.Instance.Map.TileWidth + autotile.QuarterTile[i].X);
				autotile.QuarterTile[i].Y = (short)(tile.Y * Options.Instance.Map.TileHeight + autotile.QuarterTile[i].Y);
			}
		}
	}

	public void CalculateAutotile(int x, int y, string layerName, MapDescriptor[,] surroundingMaps)
	{
		if (mMyMap == null)
		{
			ApplicationContext.Context.Value?.Logger.LogError("mMyMap=null");
		}
		else if (mMyMap.Layers == null)
		{
			ApplicationContext.Context.Value?.Logger.LogError("Layers=null");
		}
		else
		{
			if (!mMyMap.Layers.ContainsKey(layerName))
			{
				return;
			}
			Tile[,] array = mMyMap.Layers[layerName];
			if (mMyMap.Layers[layerName] == null)
			{
				ApplicationContext.Context.Value?.Logger.LogError("layer=null");
				return;
			}
			Tile tile = array[x, y];
			if (tile.Autotile == 0)
			{
				return;
			}
			switch (tile.Autotile)
			{
			case 1:
			case 3:
				CalculateNW_Normal(layerName, x, y, surroundingMaps);
				CalculateNE_Normal(layerName, x, y, surroundingMaps);
				CalculateSW_Normal(layerName, x, y, surroundingMaps);
				CalculateSE_Normal(layerName, x, y, surroundingMaps);
				break;
			case 4:
			{
				int cliffStart = 0;
				int num = CalculateCliffHeight(layerName, x, y, surroundingMaps, out cliffStart);
				int cliffStart2 = 0;
				int adjacentHeight = 0;
				if (CheckTileMatch(layerName, x, y, x - 1, y, surroundingMaps))
				{
					adjacentHeight = CalculateCliffHeight(layerName, x - 1, y, surroundingMaps, out cliffStart2);
				}
				int cliffStart3 = 0;
				int adjacentHeight2 = 0;
				if (CheckTileMatch(layerName, x, y, x + 1, y, surroundingMaps))
				{
					adjacentHeight2 = CalculateCliffHeight(layerName, x + 1, y, surroundingMaps, out cliffStart3);
				}
				bool flag = CheckTileMatch(layerName, x, y, x + 1, cliffStart, surroundingMaps) && !CheckTileMatch(layerName, x, y, x + 1, cliffStart - 1, surroundingMaps);
				bool flag2 = CheckTileMatch(layerName, x, y, x - 1, cliffStart, surroundingMaps) && !CheckTileMatch(layerName, x, y, x - 1, cliffStart - 1, surroundingMaps);
				int num2 = num;
				int num3 = x - 1;
				while (num3 > -Options.Instance.Map.MapWidth && CheckTileMatch(layerName, x, y, num3, cliffStart, surroundingMaps))
				{
					int cliffStart4 = 0;
					int num4 = CalculateCliffHeight(layerName, num3, cliffStart, surroundingMaps, out cliffStart4);
					if (cliffStart4 != cliffStart)
					{
						break;
					}
					if (num4 > num2)
					{
						num2 = num4;
					}
					num3--;
				}
				for (num3 = x + 1; num3 < Options.Instance.Map.MapWidth * 2 && CheckTileMatch(layerName, x, y, num3, cliffStart, surroundingMaps); num3++)
				{
					int cliffStart5 = 0;
					int num5 = CalculateCliffHeight(layerName, num3, cliffStart, surroundingMaps, out cliffStart5);
					if (cliffStart5 != cliffStart)
					{
						break;
					}
					if (num5 > num2)
					{
						num2 = num5;
					}
				}
				bool drawBottom = (!flag || num2 <= num) && (!flag2 || num2 <= num);
				if ((flag || flag2) && num == 1 && cliffStart != y)
				{
					drawBottom = true;
				}
				CalculateNW_Cliff(layerName, x, y, surroundingMaps, cliffStart, num, cliffStart2, adjacentHeight, flag2);
				CalculateNE_Cliff(layerName, x, y, surroundingMaps, cliffStart, num, cliffStart3, adjacentHeight2, flag);
				CalculateSW_Cliff(layerName, x, y, surroundingMaps, cliffStart, num, cliffStart2, adjacentHeight, flag2, drawBottom);
				CalculateSE_Cliff(layerName, x, y, surroundingMaps, cliffStart, num, cliffStart3, adjacentHeight2, flag, drawBottom);
				break;
			}
			case 5:
				CalculateNW_Waterfall(layerName, x, y, surroundingMaps);
				CalculateNE_Waterfall(layerName, x, y, surroundingMaps);
				CalculateSW_Waterfall(layerName, x, y, surroundingMaps);
				CalculateSE_Waterfall(layerName, x, y, surroundingMaps);
				break;
			case 6:
			case 7:
				CalculateNW_XP(layerName, x, y, surroundingMaps);
				CalculateNE_XP(layerName, x, y, surroundingMaps);
				CalculateSW_XP(layerName, x, y, surroundingMaps);
				CalculateSE_XP(layerName, x, y, surroundingMaps);
				break;
			case 2:
				break;
			}
		}
	}

	public void CalculateNW_Normal(string layerName, int x, int y, MapDescriptor[,] surroundingMaps)
	{
		bool[] array = new bool[4];
		byte b = 1;
		if (CheckTileMatch(layerName, x, y, x - 1, y - 1, surroundingMaps))
		{
			array[1] = true;
		}
		if (CheckTileMatch(layerName, x, y, x, y - 1, surroundingMaps))
		{
			array[2] = true;
		}
		if (CheckTileMatch(layerName, x, y, x - 1, y, surroundingMaps))
		{
			array[3] = true;
		}
		if (!array[2] && !array[3])
		{
			b = 1;
		}
		if (!array[2] && array[3])
		{
			b = 3;
		}
		if (array[2] && !array[3])
		{
			b = 4;
		}
		if (!array[1] && array[2] && array[3])
		{
			b = 2;
		}
		if (array[1] && array[2] && array[3])
		{
			b = 5;
		}
		switch (b)
		{
		case 1:
			PlaceAutotile(layerName, x, y, 1, "e");
			break;
		case 2:
			PlaceAutotile(layerName, x, y, 1, "a");
			break;
		case 3:
			PlaceAutotile(layerName, x, y, 1, "i");
			break;
		case 4:
			PlaceAutotile(layerName, x, y, 1, "m");
			break;
		case 5:
			PlaceAutotile(layerName, x, y, 1, "q");
			break;
		}
	}

	public void CalculateNE_Normal(string layerName, int x, int y, MapDescriptor[,] surroundingMaps)
	{
		bool[] array = new bool[4];
		byte b = 1;
		if (CheckTileMatch(layerName, x, y, x, y - 1, surroundingMaps))
		{
			array[1] = true;
		}
		if (CheckTileMatch(layerName, x, y, x + 1, y - 1, surroundingMaps))
		{
			array[2] = true;
		}
		if (CheckTileMatch(layerName, x, y, x + 1, y, surroundingMaps))
		{
			array[3] = true;
		}
		if (!array[1] && !array[3])
		{
			b = 1;
		}
		if (!array[1] && array[3])
		{
			b = 3;
		}
		if (array[1] && !array[3])
		{
			b = 4;
		}
		if (array[1] && !array[2] && array[3])
		{
			b = 2;
		}
		if (array[1] && array[2] && array[3])
		{
			b = 5;
		}
		switch (b)
		{
		case 1:
			PlaceAutotile(layerName, x, y, 2, "j");
			break;
		case 2:
			PlaceAutotile(layerName, x, y, 2, "b");
			break;
		case 3:
			PlaceAutotile(layerName, x, y, 2, "f");
			break;
		case 4:
			PlaceAutotile(layerName, x, y, 2, "r");
			break;
		case 5:
			PlaceAutotile(layerName, x, y, 2, "n");
			break;
		}
	}

	public void CalculateSW_Normal(string layerName, int x, int y, MapDescriptor[,] surroundingMaps)
	{
		bool[] array = new bool[4];
		byte b = 1;
		if (CheckTileMatch(layerName, x, y, x - 1, y, surroundingMaps))
		{
			array[1] = true;
		}
		if (CheckTileMatch(layerName, x, y, x - 1, y + 1, surroundingMaps))
		{
			array[2] = true;
		}
		if (CheckTileMatch(layerName, x, y, x, y + 1, surroundingMaps))
		{
			array[3] = true;
		}
		if (!array[1] && !array[3])
		{
			b = 1;
		}
		if (array[1] && !array[3])
		{
			b = 3;
		}
		if (!array[1] && array[3])
		{
			b = 4;
		}
		if (array[1] && !array[2] && array[3])
		{
			b = 2;
		}
		if (array[1] && array[2] && array[3])
		{
			b = 5;
		}
		switch (b)
		{
		case 1:
			PlaceAutotile(layerName, x, y, 3, "o");
			break;
		case 2:
			PlaceAutotile(layerName, x, y, 3, "c");
			break;
		case 3:
			PlaceAutotile(layerName, x, y, 3, "s");
			break;
		case 4:
			PlaceAutotile(layerName, x, y, 3, "g");
			break;
		case 5:
			PlaceAutotile(layerName, x, y, 3, "k");
			break;
		}
	}

	public void CalculateSE_Normal(string layerName, int x, int y, MapDescriptor[,] surroundingMaps)
	{
		bool[] array = new bool[4];
		byte b = 1;
		if (CheckTileMatch(layerName, x, y, x, y + 1, surroundingMaps))
		{
			array[1] = true;
		}
		if (CheckTileMatch(layerName, x, y, x + 1, y + 1, surroundingMaps))
		{
			array[2] = true;
		}
		if (CheckTileMatch(layerName, x, y, x + 1, y, surroundingMaps))
		{
			array[3] = true;
		}
		if (!array[1] && !array[3])
		{
			b = 1;
		}
		if (!array[1] && array[3])
		{
			b = 3;
		}
		if (array[1] && !array[3])
		{
			b = 4;
		}
		if (array[1] && !array[2] && array[3])
		{
			b = 2;
		}
		if (array[1] && array[2] && array[3])
		{
			b = 5;
		}
		switch (b)
		{
		case 1:
			PlaceAutotile(layerName, x, y, 4, "t");
			break;
		case 2:
			PlaceAutotile(layerName, x, y, 4, "d");
			break;
		case 3:
			PlaceAutotile(layerName, x, y, 4, "p");
			break;
		case 4:
			PlaceAutotile(layerName, x, y, 4, "l");
			break;
		case 5:
			PlaceAutotile(layerName, x, y, 4, "h");
			break;
		}
	}

	public void CalculateNW_Waterfall(string layerName, int x, int y, MapDescriptor[,] surroundingMaps)
	{
		if (CheckTileMatch(layerName, x, y, x - 1, y, surroundingMaps))
		{
			PlaceAutotile(layerName, x, y, 1, "i");
		}
		else
		{
			PlaceAutotile(layerName, x, y, 1, "e");
		}
	}

	public void CalculateNE_Waterfall(string layerName, int x, int y, MapDescriptor[,] surroundingMaps)
	{
		if (CheckTileMatch(layerName, x, y, x + 1, y, surroundingMaps))
		{
			PlaceAutotile(layerName, x, y, 2, "f");
		}
		else
		{
			PlaceAutotile(layerName, x, y, 2, "j");
		}
	}

	public void CalculateSW_Waterfall(string layerName, int x, int y, MapDescriptor[,] surroundingMaps)
	{
		if (CheckTileMatch(layerName, x, y, x - 1, y, surroundingMaps))
		{
			PlaceAutotile(layerName, x, y, 3, "k");
		}
		else
		{
			PlaceAutotile(layerName, x, y, 3, "g");
		}
	}

	public void CalculateSE_Waterfall(string layerName, int x, int y, MapDescriptor[,] surroundingMaps)
	{
		if (CheckTileMatch(layerName, x, y, x + 1, y, surroundingMaps))
		{
			PlaceAutotile(layerName, x, y, 4, "h");
		}
		else
		{
			PlaceAutotile(layerName, x, y, 4, "l");
		}
	}

	public void CalculateNW_Cliff(string layerName, int x, int y, MapDescriptor[,] surroundingMaps, int cliffStart, int cliffHeight, int adjacentStart, int adjacentHeight, bool assumeInterior)
	{
		bool[] array = new bool[5];
		byte b = 1;
		if (CheckTileMatch(layerName, x, y, x - 1, y - 1, surroundingMaps))
		{
			array[1] = true;
		}
		if (CheckTileMatch(layerName, x, y, x, y - 1, surroundingMaps))
		{
			array[2] = true;
		}
		if (adjacentHeight > 0)
		{
			if (cliffStart == adjacentStart)
			{
				array[3] = true;
			}
			else if (cliffStart > adjacentStart && cliffStart + cliffHeight <= adjacentStart + adjacentHeight)
			{
				array[3] = true;
			}
		}
		if (!array[2] && array[3])
		{
			b = 3;
		}
		if (array[2] && !array[3])
		{
			b = 4;
		}
		if (array[2] && array[3])
		{
			b = 5;
		}
		if (array[4])
		{
			b = 3;
		}
		if (b == 4 && assumeInterior)
		{
			b = 5;
		}
		switch (b)
		{
		case 1:
			PlaceAutotile(layerName, x, y, 1, "e");
			break;
		case 3:
			PlaceAutotile(layerName, x, y, 1, "i");
			break;
		case 4:
			PlaceAutotile(layerName, x, y, 1, "m");
			break;
		case 5:
			PlaceAutotile(layerName, x, y, 1, "q");
			break;
		case 2:
			break;
		}
	}

	public void CalculateNE_Cliff(string layerName, int x, int y, MapDescriptor[,] surroundingMaps, int cliffStart, int cliffHeight, int adjacentStart, int adjacentHeight, bool assumeInterior)
	{
		bool[] array = new bool[5];
		byte b = 1;
		if (CheckTileMatch(layerName, x, y, x, y - 1, surroundingMaps))
		{
			array[1] = true;
		}
		if (CheckTileMatch(layerName, x, y, x + 1, y + 1, surroundingMaps))
		{
			array[2] = true;
		}
		if (adjacentHeight > 0)
		{
			if (cliffStart == adjacentStart)
			{
				array[3] = true;
			}
			else if (cliffStart > adjacentStart && cliffStart + cliffHeight <= adjacentStart + adjacentHeight)
			{
				array[3] = true;
			}
		}
		if (!array[1] && array[3])
		{
			b = 3;
		}
		if (array[1] && !array[3])
		{
			b = 4;
		}
		if (array[1] && array[3])
		{
			b = 5;
		}
		if (array[4])
		{
			b = 3;
		}
		if (b == 4 && assumeInterior)
		{
			b = 5;
		}
		switch (b)
		{
		case 1:
			PlaceAutotile(layerName, x, y, 2, "j");
			break;
		case 3:
			PlaceAutotile(layerName, x, y, 2, "f");
			break;
		case 4:
			PlaceAutotile(layerName, x, y, 2, "r");
			break;
		case 5:
			PlaceAutotile(layerName, x, y, 2, "n");
			break;
		case 2:
			break;
		}
	}

	public void CalculateSW_Cliff(string layerName, int x, int y, MapDescriptor[,] surroundingMaps, int cliffStart, int cliffHeight, int adjacentStart, int adjacentHeight, bool assumeInterior, bool drawBottom)
	{
		bool[] array = new bool[5];
		byte b = 1;
		if (adjacentHeight > 0)
		{
			if (cliffStart == adjacentStart)
			{
				array[1] = true;
			}
			else if (cliffStart > adjacentStart && cliffStart + cliffHeight <= adjacentStart + adjacentHeight)
			{
				array[1] = true;
			}
		}
		if (CheckTileMatch(layerName, x, y, x - 1, y + 1, surroundingMaps))
		{
			array[2] = true;
		}
		if (CheckTileMatch(layerName, x, y, x, y + 1, surroundingMaps) || !drawBottom)
		{
			array[3] = true;
		}
		if (CheckTileMatch(layerName, x, y, x - 1, y, surroundingMaps) && !CheckTileMatch(layerName, x, y, x, y - 1, surroundingMaps))
		{
			array[4] = true;
		}
		if (!array[1] && array[3])
		{
			b = 4;
		}
		if (array[1] && array[3])
		{
			b = 5;
		}
		if (array[1] && !array[3])
		{
			b = 3;
		}
		if (!array[1] && !array[3])
		{
			b = 1;
		}
		if (b == 4 && assumeInterior)
		{
			b = 5;
		}
		if (b == 1 && assumeInterior)
		{
			b = 3;
		}
		switch (b)
		{
		case 1:
			PlaceAutotile(layerName, x, y, 3, "o");
			break;
		case 3:
			PlaceAutotile(layerName, x, y, 3, "s");
			break;
		case 4:
			PlaceAutotile(layerName, x, y, 3, "g");
			break;
		case 5:
			PlaceAutotile(layerName, x, y, 3, "k");
			break;
		case 2:
			break;
		}
	}

	public void CalculateSE_Cliff(string layerName, int x, int y, MapDescriptor[,] surroundingMaps, int cliffStart, int cliffHeight, int adjacentStart, int adjacentHeight, bool assumeInterior, bool drawBottom)
	{
		bool[] array = new bool[5];
		byte b = 1;
		if (CheckTileMatch(layerName, x, y, x, y + 1, surroundingMaps) || !drawBottom)
		{
			array[1] = true;
		}
		if (CheckTileMatch(layerName, x, y, x + 1, y + 1, surroundingMaps))
		{
			array[2] = true;
		}
		if (adjacentHeight > 0)
		{
			if (cliffStart == adjacentStart)
			{
				array[3] = true;
			}
			else if (cliffStart > adjacentStart && cliffStart + cliffHeight <= adjacentStart + adjacentHeight)
			{
				array[3] = true;
			}
		}
		if (CheckTileMatch(layerName, x, y, x + 1, y, surroundingMaps) && !CheckTileMatch(layerName, x, y, x, y - 1, surroundingMaps))
		{
			array[4] = true;
		}
		if (array[1] && !array[3])
		{
			b = 4;
		}
		if (array[1] && array[3])
		{
			b = 5;
		}
		if (!array[1] && array[3])
		{
			b = 3;
		}
		if (!array[1] && !array[3])
		{
			b = 1;
		}
		if (b == 4 && assumeInterior)
		{
			b = 5;
		}
		if (b == 1 && assumeInterior)
		{
			b = 3;
		}
		switch (b)
		{
		case 1:
			PlaceAutotile(layerName, x, y, 4, "t");
			break;
		case 3:
			PlaceAutotile(layerName, x, y, 4, "p");
			break;
		case 4:
			PlaceAutotile(layerName, x, y, 4, "l");
			break;
		case 5:
			PlaceAutotile(layerName, x, y, 4, "h");
			break;
		case 2:
			break;
		}
	}

	public void CalculateNW_XP(string layerName, int x, int y, MapDescriptor[,] surroundingMaps)
	{
		bool[,] array = new bool[4, 4];
		byte b = 1;
		for (int i = -1; i < 2; i++)
		{
			for (int j = -1; j < 2; j++)
			{
				if (CheckTileMatch(layerName, x, y, x + j, y + i, surroundingMaps))
				{
					array[j + 2, i + 2] = true;
				}
			}
		}
		if (array[1, 2] && !array[2, 3])
		{
			b = 8;
			if (!array[3, 2])
			{
				b = 7;
			}
		}
		if (array[1, 2] && !array[2, 1])
		{
			b = 4;
			if (!array[3, 2])
			{
				b = 5;
			}
		}
		if (!array[3, 2] && array[2, 1])
		{
			b = 6;
			if (!array[2, 3])
			{
				b = 7;
			}
		}
		if (!array[1, 2] && array[2, 1])
		{
			b = 10;
			if (!array[2, 3])
			{
				b = 9;
			}
		}
		if (!array[1, 2] && !array[2, 1])
		{
			b = 3;
		}
		if (array[1, 2] && array[2, 1] && !array[1, 1])
		{
			b = 2;
		}
		if (array[1, 1] && array[2, 1] && array[1, 2])
		{
			b = 1;
		}
		switch (b)
		{
		case 2:
			PlaceAutotileXp(layerName, x, y, 1, "a");
			break;
		case 1:
			PlaceAutotileXp(layerName, x, y, 1, "A");
			break;
		case 3:
			PlaceAutotileXp(layerName, x, y, 1, "e");
			break;
		case 4:
			PlaceAutotileXp(layerName, x, y, 1, "E");
			break;
		case 5:
			PlaceAutotileXp(layerName, x, y, 1, "i");
			break;
		case 6:
			PlaceAutotileXp(layerName, x, y, 1, "I");
			break;
		case 7:
			PlaceAutotileXp(layerName, x, y, 1, "q");
			break;
		case 8:
			PlaceAutotileXp(layerName, x, y, 1, "Q");
			break;
		case 9:
			PlaceAutotileXp(layerName, x, y, 1, "m");
			break;
		case 10:
			PlaceAutotileXp(layerName, x, y, 1, "M");
			break;
		}
	}

	public void CalculateNE_XP(string layerName, int x, int y, MapDescriptor[,] surroundingMaps)
	{
		bool[,] array = new bool[4, 4];
		byte b = 1;
		for (int i = -1; i < 2; i++)
		{
			for (int j = -1; j < 2; j++)
			{
				if (CheckTileMatch(layerName, x, y, x + j, y + i, surroundingMaps))
				{
					array[j + 2, i + 2] = true;
				}
			}
		}
		if (array[1, 2] && !array[2, 3])
		{
			b = 8;
			if (!array[3, 2])
			{
				b = 7;
			}
		}
		if (array[1, 2] && !array[2, 1])
		{
			b = 4;
			if (!array[3, 2])
			{
				b = 5;
			}
		}
		if (!array[1, 2] && array[2, 1])
		{
			b = 10;
			if (!array[2, 3])
			{
				b = 9;
			}
		}
		if (!array[3, 2] && array[2, 1])
		{
			b = 6;
			if (!array[2, 3])
			{
				b = 7;
			}
		}
		if (!array[1, 2] && !array[2, 1])
		{
			b = 3;
		}
		if (!array[2, 1] && !array[3, 2])
		{
			b = 5;
		}
		if (array[3, 2] && array[2, 1] && !array[3, 1])
		{
			b = 2;
		}
		if (array[2, 1] && array[3, 1] && array[3, 2])
		{
			b = 1;
		}
		switch (b)
		{
		case 2:
			PlaceAutotileXp(layerName, x, y, 2, "b");
			break;
		case 1:
			PlaceAutotileXp(layerName, x, y, 2, "B");
			break;
		case 3:
			PlaceAutotileXp(layerName, x, y, 2, "f");
			break;
		case 4:
			PlaceAutotileXp(layerName, x, y, 2, "F");
			break;
		case 5:
			PlaceAutotileXp(layerName, x, y, 2, "j");
			break;
		case 6:
			PlaceAutotileXp(layerName, x, y, 2, "J");
			break;
		case 7:
			PlaceAutotileXp(layerName, x, y, 2, "r");
			break;
		case 8:
			PlaceAutotileXp(layerName, x, y, 2, "R");
			break;
		case 9:
			PlaceAutotileXp(layerName, x, y, 2, "n");
			break;
		case 10:
			PlaceAutotileXp(layerName, x, y, 2, "N");
			break;
		}
	}

	public void CalculateSW_XP(string layerName, int x, int y, MapDescriptor[,] surroundingMaps)
	{
		bool[,] array = new bool[4, 4];
		byte b = 1;
		for (int i = -1; i < 2; i++)
		{
			for (int j = -1; j < 2; j++)
			{
				if (CheckTileMatch(layerName, x, y, x + j, y + i, surroundingMaps))
				{
					array[j + 2, i + 2] = true;
				}
			}
		}
		if (array[1, 2] && !array[2, 1])
		{
			b = 4;
			if (!array[3, 2])
			{
				b = 5;
			}
		}
		if (array[1, 2] && !array[2, 3])
		{
			b = 8;
			if (!array[3, 2])
			{
				b = 7;
			}
		}
		if (!array[3, 2] && array[2, 1])
		{
			b = 6;
			if (!array[2, 3])
			{
				b = 7;
			}
		}
		if (!array[1, 2] && array[2, 1])
		{
			b = 10;
			if (!array[2, 3])
			{
				b = 9;
			}
		}
		if (!array[1, 2] && !array[2, 1])
		{
			b = 3;
		}
		if (!array[1, 2] && !array[2, 3])
		{
			b = 9;
		}
		if (array[1, 2] && array[2, 3] && !array[1, 3])
		{
			b = 2;
		}
		if (array[1, 2] && array[1, 3] && array[2, 3])
		{
			b = 1;
		}
		switch (b)
		{
		case 2:
			PlaceAutotileXp(layerName, x, y, 3, "c");
			break;
		case 1:
			PlaceAutotileXp(layerName, x, y, 3, "C");
			break;
		case 3:
			PlaceAutotileXp(layerName, x, y, 3, "g");
			break;
		case 4:
			PlaceAutotileXp(layerName, x, y, 3, "G");
			break;
		case 5:
			PlaceAutotileXp(layerName, x, y, 3, "k");
			break;
		case 6:
			PlaceAutotileXp(layerName, x, y, 3, "K");
			break;
		case 7:
			PlaceAutotileXp(layerName, x, y, 3, "s");
			break;
		case 8:
			PlaceAutotileXp(layerName, x, y, 3, "S");
			break;
		case 9:
			PlaceAutotileXp(layerName, x, y, 3, "o");
			break;
		case 10:
			PlaceAutotileXp(layerName, x, y, 3, "O");
			break;
		}
	}

	public void CalculateSE_XP(string layerName, int x, int y, MapDescriptor[,] surroundingMaps)
	{
		bool[,] array = new bool[4, 4];
		byte b = 1;
		for (int i = -1; i < 2; i++)
		{
			for (int j = -1; j < 2; j++)
			{
				if (CheckTileMatch(layerName, x, y, x + j, y + i, surroundingMaps))
				{
					array[j + 2, i + 2] = true;
				}
			}
		}
		if (array[1, 2] && !array[2, 1])
		{
			b = 4;
			if (!array[3, 2])
			{
				b = 5;
			}
		}
		if (array[1, 2] && !array[2, 3])
		{
			b = 8;
			if (!array[3, 2])
			{
				b = 7;
			}
		}
		if (!array[1, 2] && array[2, 1])
		{
			b = 10;
			if (!array[2, 3])
			{
				b = 9;
			}
		}
		if (!array[3, 2] && array[2, 1])
		{
			b = 6;
			if (!array[2, 3])
			{
				b = 7;
			}
		}
		if (!array[1, 2] && !array[2, 1])
		{
			b = 3;
		}
		if (!array[1, 2] && !array[2, 3])
		{
			b = 9;
		}
		if (!array[2, 1] && !array[3, 2])
		{
			b = 5;
		}
		if (!array[2, 3] && !array[3, 2])
		{
			b = 7;
		}
		if (array[3, 2] && array[2, 3] && !array[3, 3])
		{
			b = 2;
		}
		if (array[3, 2] && array[3, 3] && array[2, 3])
		{
			b = 1;
		}
		switch (b)
		{
		case 2:
			PlaceAutotileXp(layerName, x, y, 4, "d");
			break;
		case 1:
			PlaceAutotileXp(layerName, x, y, 4, "D");
			break;
		case 3:
			PlaceAutotileXp(layerName, x, y, 4, "h");
			break;
		case 4:
			PlaceAutotileXp(layerName, x, y, 4, "H");
			break;
		case 5:
			PlaceAutotileXp(layerName, x, y, 4, "l");
			break;
		case 6:
			PlaceAutotileXp(layerName, x, y, 4, "L");
			break;
		case 7:
			PlaceAutotileXp(layerName, x, y, 4, "t");
			break;
		case 8:
			PlaceAutotileXp(layerName, x, y, 4, "T");
			break;
		case 9:
			PlaceAutotileXp(layerName, x, y, 4, "p");
			break;
		case 10:
			PlaceAutotileXp(layerName, x, y, 4, "P");
			break;
		}
	}

	public bool CheckTileMatch(string layerName, int x1, int y1, int x2, int y2, MapDescriptor[,] surroundingMaps)
	{
		Tile tile = default(Tile);
		tile.TilesetId = Guid.Empty;
		tile.X = -1;
		tile.Y = -1;
		tile.Autotile = 0;
		int num = 0;
		int num2 = 0;
		if (x2 < 0)
		{
			num = -1;
			x2 += Options.Instance.Map.MapWidth;
		}
		if (y2 < 0)
		{
			num2 = -1;
			y2 += Options.Instance.Map.MapHeight;
		}
		if (x2 >= Options.Instance.Map.MapWidth)
		{
			num = 1;
			x2 -= Options.Instance.Map.MapWidth;
		}
		if (y2 >= Options.Instance.Map.MapHeight)
		{
			num2 = 1;
			y2 -= Options.Instance.Map.MapHeight;
		}
		if (surroundingMaps[num + 1, num2 + 1] != null)
		{
			Dictionary<string, Tile[,]> layers = surroundingMaps[num + 1, num2 + 1].Layers;
			if (!layers.ContainsKey(layerName))
			{
				return true;
			}
			Tile[,] array = layers[layerName];
			tile = array[x2, y2];
		}
		if (!mMyMap.Layers.ContainsKey(layerName))
		{
			return true;
		}
		Tile tile2 = mMyMap.Layers[layerName][x1, y1];
		if (tile.X == -1)
		{
			return true;
		}
		if (tile.Autotile == 2)
		{
			return true;
		}
		if (tile.Autotile == 0)
		{
			return false;
		}
		if (tile2.TilesetId != tile.TilesetId)
		{
			return false;
		}
		if (tile2.X != tile.X)
		{
			return false;
		}
		if (tile2.Y != tile.Y)
		{
			return false;
		}
		return true;
	}

	private int CalculateCliffHeight(string layerName, int x, int y, MapDescriptor[,] surroundingMaps, out int cliffStart)
	{
		Tile tile = default(Tile);
		tile.TilesetId = Guid.Empty;
		tile.X = -1;
		tile.Y = -1;
		tile.Autotile = 0;
		cliffStart = y;
		int num = 0;
		int num2 = 0;
		if (x < 0)
		{
			num = -1;
			x += Options.Instance.Map.MapWidth;
		}
		if (y < 0)
		{
			num2 = -1;
			y += Options.Instance.Map.MapHeight;
		}
		if (x >= Options.Instance.Map.MapWidth)
		{
			num = 1;
			x -= Options.Instance.Map.MapWidth;
		}
		if (y >= Options.Instance.Map.MapHeight)
		{
			num2 = 1;
			y -= Options.Instance.Map.MapHeight;
		}
		if (surroundingMaps[num + 1, num2 + 1] != null)
		{
			Dictionary<string, Tile[,]> layers = surroundingMaps[num + 1, num2 + 1].Layers;
			Tile[,] array = layers[layerName];
			tile = array[x, y];
		}
		if (tile.Autotile == 4)
		{
			int num3 = 1;
			int num4 = y - 1;
			while (num4 > -Options.Instance.Map.MapHeight && CheckTileMatch(layerName, x, y, x, num4, surroundingMaps))
			{
				num3++;
				cliffStart--;
				num4--;
			}
			for (num4 = y + 1; num4 < Options.Instance.Map.MapHeight * 2 && CheckTileMatch(layerName, x, y, x, num4, surroundingMaps); num4++)
			{
				num3++;
			}
			return num3;
		}
		return 0;
	}

	public void PlaceAutotile(string layerName, int x, int y, byte tileQuarter, string autoTileLetter)
	{
		Point16i point16i = default(Point16i);
		switch (autoTileLetter)
		{
		case "a":
			point16i.X = AutoInner[1].X;
			point16i.Y = AutoInner[1].Y;
			break;
		case "b":
			point16i.X = AutoInner[2].X;
			point16i.Y = AutoInner[2].Y;
			break;
		case "c":
			point16i.X = AutoInner[3].X;
			point16i.Y = AutoInner[3].Y;
			break;
		case "d":
			point16i.X = AutoInner[4].X;
			point16i.Y = AutoInner[4].Y;
			break;
		case "e":
			point16i.X = AutoNw[1].X;
			point16i.Y = AutoNw[1].Y;
			break;
		case "f":
			point16i.X = AutoNw[2].X;
			point16i.Y = AutoNw[2].Y;
			break;
		case "g":
			point16i.X = AutoNw[3].X;
			point16i.Y = AutoNw[3].Y;
			break;
		case "h":
			point16i.X = AutoNw[4].X;
			point16i.Y = AutoNw[4].Y;
			break;
		case "i":
			point16i.X = AutoNe[1].X;
			point16i.Y = AutoNe[1].Y;
			break;
		case "j":
			point16i.X = AutoNe[2].X;
			point16i.Y = AutoNe[2].Y;
			break;
		case "k":
			point16i.X = AutoNe[3].X;
			point16i.Y = AutoNe[3].Y;
			break;
		case "l":
			point16i.X = AutoNe[4].X;
			point16i.Y = AutoNe[4].Y;
			break;
		case "m":
			point16i.X = AutoSw[1].X;
			point16i.Y = AutoSw[1].Y;
			break;
		case "n":
			point16i.X = AutoSw[2].X;
			point16i.Y = AutoSw[2].Y;
			break;
		case "o":
			point16i.X = AutoSw[3].X;
			point16i.Y = AutoSw[3].Y;
			break;
		case "p":
			point16i.X = AutoSw[4].X;
			point16i.Y = AutoSw[4].Y;
			break;
		case "q":
			point16i.X = AutoSe[1].X;
			point16i.Y = AutoSe[1].Y;
			break;
		case "r":
			point16i.X = AutoSe[2].X;
			point16i.Y = AutoSe[2].Y;
			break;
		case "s":
			point16i.X = AutoSe[3].X;
			point16i.Y = AutoSe[3].Y;
			break;
		case "t":
			point16i.X = AutoSe[4].X;
			point16i.Y = AutoSe[4].Y;
			break;
		}
		Layers[layerName][x, y].QuarterTile[tileQuarter] = point16i;
	}

	public void PlaceAutotileXp(string layerName, int x, int y, byte tileQuarter, string autoTileLetter)
	{
		Point16i point16i = default(Point16i);
		switch (autoTileLetter)
		{
		case "a":
			point16i.X = AutoInnerXp[1].X;
			point16i.Y = AutoInnerXp[1].Y;
			break;
		case "b":
			point16i.X = AutoInnerXp[2].X;
			point16i.Y = AutoInnerXp[2].Y;
			break;
		case "c":
			point16i.X = AutoInnerXp[3].X;
			point16i.Y = AutoInnerXp[3].Y;
			break;
		case "d":
			point16i.X = AutoInnerXp[4].X;
			point16i.Y = AutoInnerXp[4].Y;
			break;
		case "e":
			point16i.X = AutoNwXp[1].X;
			point16i.Y = AutoNwXp[1].Y;
			break;
		case "f":
			point16i.X = AutoNwXp[2].X;
			point16i.Y = AutoNwXp[2].Y;
			break;
		case "g":
			point16i.X = AutoNwXp[3].X;
			point16i.Y = AutoNwXp[3].Y;
			break;
		case "h":
			point16i.X = AutoNwXp[4].X;
			point16i.Y = AutoNwXp[4].Y;
			break;
		case "i":
			point16i.X = AutoNeXp[1].X;
			point16i.Y = AutoNeXp[1].Y;
			break;
		case "j":
			point16i.X = AutoNeXp[2].X;
			point16i.Y = AutoNeXp[2].Y;
			break;
		case "k":
			point16i.X = AutoNeXp[3].X;
			point16i.Y = AutoNeXp[3].Y;
			break;
		case "l":
			point16i.X = AutoNeXp[4].X;
			point16i.Y = AutoNeXp[4].Y;
			break;
		case "m":
			point16i.X = AutoSwXp[1].X;
			point16i.Y = AutoSwXp[1].Y;
			break;
		case "n":
			point16i.X = AutoSwXp[2].X;
			point16i.Y = AutoSwXp[2].Y;
			break;
		case "o":
			point16i.X = AutoSwXp[3].X;
			point16i.Y = AutoSwXp[3].Y;
			break;
		case "p":
			point16i.X = AutoSwXp[4].X;
			point16i.Y = AutoSwXp[4].Y;
			break;
		case "q":
			point16i.X = AutoSeXp[1].X;
			point16i.Y = AutoSeXp[1].Y;
			break;
		case "r":
			point16i.X = AutoSeXp[2].X;
			point16i.Y = AutoSeXp[2].Y;
			break;
		case "s":
			point16i.X = AutoSeXp[3].X;
			point16i.Y = AutoSeXp[3].Y;
			break;
		case "t":
			point16i.X = AutoSeXp[4].X;
			point16i.Y = AutoSeXp[4].Y;
			break;
		case "A":
			point16i.X = AutoCxp[1].X;
			point16i.Y = AutoCxp[1].Y;
			break;
		case "B":
			point16i.X = AutoCxp[2].X;
			point16i.Y = AutoCxp[2].Y;
			break;
		case "C":
			point16i.X = AutoCxp[3].X;
			point16i.Y = AutoCxp[3].Y;
			break;
		case "D":
			point16i.X = AutoCxp[4].X;
			point16i.Y = AutoCxp[4].Y;
			break;
		case "E":
			point16i.X = AutoNxp[1].X;
			point16i.Y = AutoNxp[1].Y;
			break;
		case "F":
			point16i.X = AutoNxp[2].X;
			point16i.Y = AutoNxp[2].Y;
			break;
		case "G":
			point16i.X = AutoNxp[3].X;
			point16i.Y = AutoNxp[3].Y;
			break;
		case "H":
			point16i.X = AutoNxp[4].X;
			point16i.Y = AutoNxp[4].Y;
			break;
		case "I":
			point16i.X = AutoExp[1].X;
			point16i.Y = AutoExp[1].Y;
			break;
		case "J":
			point16i.X = AutoExp[2].X;
			point16i.Y = AutoExp[2].Y;
			break;
		case "K":
			point16i.X = AutoExp[3].X;
			point16i.Y = AutoExp[3].Y;
			break;
		case "L":
			point16i.X = AutoExp[4].X;
			point16i.Y = AutoExp[4].Y;
			break;
		case "M":
			point16i.X = AutoWxp[1].X;
			point16i.Y = AutoWxp[1].Y;
			break;
		case "N":
			point16i.X = AutoWxp[2].X;
			point16i.Y = AutoWxp[2].Y;
			break;
		case "O":
			point16i.X = AutoWxp[3].X;
			point16i.Y = AutoWxp[3].Y;
			break;
		case "P":
			point16i.X = AutoWxp[4].X;
			point16i.Y = AutoWxp[4].Y;
			break;
		case "Q":
			point16i.X = AutoSxp[1].X;
			point16i.Y = AutoSxp[1].Y;
			break;
		case "R":
			point16i.X = AutoSxp[2].X;
			point16i.Y = AutoSxp[2].Y;
			break;
		case "S":
			point16i.X = AutoSxp[3].X;
			point16i.Y = AutoSxp[3].Y;
			break;
		case "T":
			point16i.X = AutoSxp[4].X;
			point16i.Y = AutoSxp[4].Y;
			break;
		}
		Layers[layerName][x, y].QuarterTile[tileQuarter] = point16i;
	}
}
