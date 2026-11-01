using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Intersect.Client.Core;
using Intersect.Client.Core.Controls;
using Intersect.Client.Dungeons;
using Intersect.Client.Entities;
using Intersect.Client.Entities.Events;
using Intersect.Client.Framework.Content;
using Intersect.Client.Framework.Entities;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.Framework.Input;
using Intersect.Client.General;
using Intersect.Client.Localization;
using Intersect.Client.Maps;
using Intersect.Enums;
using Intersect.Framework.Core;
using Intersect.Framework.Core.GameObjects.Dungeons;
using Intersect.Framework.Core.GameObjects.Events;
using Intersect.Framework.Core.GameObjects.Maps;
using Intersect.Framework.Core.GameObjects.Maps.Attributes;
using Intersect.GameObjects;
using Intersect.Models;

namespace Intersect.Client.Interface.Game;

public static class LocalMap
{
	private sealed class MapCache
	{
		public required MapInstance Map;

		public required IGameRenderTexture Texture;

		public required int TileSize;
	}

	private const int ViewTilesX = 44;

	private const int ViewTilesY = 30;

	private const int BaseTileSize = 16;

	private const long FadeInMs = 110L;

	private const long FadeOutMs = 90L;

	private const int Padding = 14;

	private const int HeaderHeight = 46;

	private const int FooterHeight = 38;

	private static readonly Color Night = new Color(255, 11, 14, 23);

	private static readonly Color White = new Color(255, 242, 242, 242);

	private static readonly Color Muted = new Color(255, 176, 184, 200);

	private static readonly Color Gold = new Color(255, 245, 190, 90);

	private static readonly Color GoldLight = new Color(255, 255, 172, 82);

	private static readonly Color Blue = new Color(255, 120, 176, 255);

	private static readonly Color Orange = new Color(255, 255, 122, 26);

	private static readonly Color SafeBack = new Color(255, 30, 130, 70);

	private static readonly Color SafeText = new Color(255, 140, 235, 170);

	private static readonly Color PvpBack = new Color(255, 150, 24, 30);

	private static readonly Color PvpText = new Color(255, 255, 120, 110);

	private static readonly Dictionary<(Guid Map, int Tile), MapCache> Caches = new Dictionary<(Guid, int), MapCache>();

	private static int _cacheScreenWidth;

	private static int _cacheScreenHeight;

	private static bool _open;

	private static long _changedAt;

	private static IFont? _titleFont;

	private static IFont? _nameFont;

	private static IFont? _bodyFont;

	private static IFont? _smallFont;

	internal static IGameTexture? MapTexture(MapInstance map, int tile)
	{
		GameRenderer renderer = Graphics.Renderer;
		if (renderer == null)
		{
			return null;
		}
		return GetCache(renderer, map, tile);
	}

	public static void Draw()
	{
		GameRenderer renderer = Graphics.Renderer;
		int tile;
		FloatRect plan;
		int tileWidth;
		int tileHeight;
		Vector2 origin;
		if (renderer != null && Globals.GameState == GameStates.InGame)
		{
			Player me = Globals.Me;
			if (me != null)
			{
				Guid[,] mapGrid = Globals.MapGrid;
				if (mapGrid != null)
				{
					bool flag = !Interface.HasInputFocus() && Controls.IsControlPressed(Control.ShowLocalMap);
					long milliseconds = Timing.Global.Milliseconds;
					if (flag != _open)
					{
						_open = flag;
						_changedAt = milliseconds;
					}
					float num = (_open ? Math.Clamp((float)(milliseconds - _changedAt) / 110f, 0f, 1f) : (1f - Math.Clamp((float)(milliseconds - _changedAt) / 90f, 0f, 1f)));
					if (num <= 0f || !MapInstance.TryGet(me.MapId, out MapInstance instance) || !Globals.GridMaps.TryGetValue(me.MapId, out var value))
					{
						return;
					}
					if (_titleFont == null)
					{
						_titleFont = Globals.ContentManager.GetFont("sairatitre") ?? Globals.ContentManager.GetFont("rajdhanibold");
					}
					if (_nameFont == null)
					{
						_nameFont = Globals.ContentManager.GetFont("sairanom") ?? _titleFont;
					}
					if (_bodyFont == null)
					{
						_bodyFont = Globals.ContentManager.GetFont("poppinsmedium") ?? Globals.ContentManager.GetFont("chakrapetch");
					}
					if (_smallFont == null)
					{
						_smallFont = Globals.ContentManager.GetFont("poppinssemibold") ?? _bodyFont;
					}
					if (_titleFont == null || _nameFont == null || _bodyFont == null || _smallFont == null)
					{
						return;
					}
					if (renderer.ScreenWidth != _cacheScreenWidth || renderer.ScreenHeight != _cacheScreenHeight)
					{
						ClearCaches();
						_cacheScreenWidth = renderer.ScreenWidth;
						_cacheScreenHeight = renderer.ScreenHeight;
					}
					FloatRect currentView = Graphics.CurrentView;
					float num2 = Math.Max(0.1f, renderer.Scale);
					float num3 = (float)renderer.ScreenWidth / num2;
					float num4 = (float)renderer.ScreenHeight / num2;
					float num5 = Math.Clamp(MathF.Floor(MathF.Min(num3 / 1280f, num4 / 720f) * 2f) / 2f, 1f, 3f);
					tile = (int)(16f * num5);
					int num6 = Math.Min(44, (int)((num3 - 80f) / (float)tile));
					int num7 = Math.Min(30, (int)((num4 - 46f - 38f - 60f) / (float)tile));
					if (num6 < 12 || num7 < 8)
					{
						return;
					}
					int num8 = num6 * tile;
					int num9 = num7 * tile;
					int num10 = num8 + 28;
					float num11 = (float)(46 + num9 + 38) + 7f;
					float num12 = MathF.Round((num3 - (float)num10) / 2f);
					float num13 = MathF.Round((num4 - num11) / 2f);
					plan = new FloatRect(num12 + 14f, num13 + 46f, num8, num9);
					Rect(renderer, currentView, new FloatRect(0f, 0f, num3, num4), WithAlpha(Night, 120f * num));
					NineSlice(renderer, currentView, "carte_environs_panneau.png", new FloatRect(num12, num13, num10, num11), 16, WithAlpha(Color.White, 255f * num));
					DrawHeader(renderer, currentView, instance, num12, num13, num);
					tileWidth = Options.Instance.Map.TileWidth;
					tileHeight = Options.Instance.Map.TileHeight;
					int num14 = Options.Instance.Map.MapWidth * tileWidth;
					int num15 = Options.Instance.Map.MapHeight * tileHeight;
					origin = me.Center;
					Rect(renderer, currentView, plan, WithAlpha(Night, 255f * num));
					FloatRect floatRect = FloatRect.Empty;
					for (int i = value.Y - 1; i <= value.Y + 1; i++)
					{
						for (int j = value.X - 1; j <= value.X + 1; j++)
						{
							if (j < 0 || i < 0 || j >= mapGrid.GetLength(0) || i >= mapGrid.GetLength(1) || mapGrid[j, i] == Guid.Empty || !MapInstance.TryGet(mapGrid[j, i], out MapInstance instance2) || !instance2.IsLoaded)
							{
								continue;
							}
							Vector2 vector = ToPlan(instance2.X, instance2.Y);
							FloatRect floatRect2 = new FloatRect(vector.X, vector.Y, Options.Instance.Map.MapWidth * tile, Options.Instance.Map.MapHeight * tile);
							if (instance2 == instance)
							{
								floatRect = floatRect2;
							}
							FloatRect rect = Intersect(floatRect2, plan);
							if (rect.Width <= 0f || rect.Height <= 0f)
							{
								continue;
							}
							IGameTexture cache = GetCache(renderer, instance2, tile);
							if (cache != null)
							{
								Graphics.DrawGameTexture(cache, new FloatRect(MathF.Round(rect.X - floatRect2.X), MathF.Round(rect.Y - floatRect2.Y), MathF.Round(rect.Width), MathF.Round(rect.Height)), At(currentView, new FloatRect(MathF.Round(rect.X), MathF.Round(rect.Y), MathF.Round(rect.Width), MathF.Round(rect.Height))), WithAlpha(Color.White, 245f * num));
								if (instance2 != instance)
								{
									Rect(renderer, currentView, rect, WithAlpha(Night, 95f * num));
								}
							}
						}
					}
					if (floatRect.Width > 0f)
					{
						DashedRect(renderer, currentView, floatRect, plan, WithAlpha(White, 120f * num));
						DrawExits(renderer, currentView, instance, value, mapGrid, floatRect, plan, tile, num);
					}
					DrawCharacters(renderer, currentView, me, mapGrid, value, plan, ToPlan, num);
					DrawObjective(renderer, currentView, me, plan, ToPlan, origin, num);
					DrawPlayer(renderer, currentView, me, plan, num);
					Frame(renderer, currentView, plan, WithAlpha(new Color(255, 62, 74, 104), 220f * num));
					DrawFooter(renderer, currentView, plan, num);
					return;
				}
			}
		}
		_open = false;
		Vector2 ToPlan(float worldX, float worldY)
		{
			return new Vector2(plan.X + plan.Width / 2f + (worldX - origin.X) / (float)tileWidth * (float)tile, plan.Y + plan.Height / 2f + (worldY - origin.Y) / (float)tileHeight * (float)tile);
		}
	}

	private static IGameTexture? GetCache(GameRenderer renderer, MapInstance map, int tile)
	{
		if (Caches.TryGetValue((map.Id, tile), out MapCache value) && value.Map == map)
		{
			return value.Texture;
		}
		if (value != null)
		{
			value.Texture.Dispose();
			Caches.Remove((map.Id, tile));
		}
		if (!map.PrepareLocalMapTextures())
		{
			return null;
		}
		int mapWidth = Options.Instance.Map.MapWidth;
		int mapHeight = Options.Instance.Map.MapHeight;
		IGameRenderTexture gameRenderTexture = renderer.CreateRenderTexture(mapWidth * tile, mapHeight * tile);
		gameRenderTexture.Clear(Color.Transparent);
		float scale = renderer.Scale;
		renderer.Scale = 1f;
		try
		{
			float num = (float)tile / 2f;
			int tileWidth = Options.Instance.Map.TileWidth;
			int tileHeight = Options.Instance.Map.TileHeight;
			foreach (string item3 in Options.Instance.Map.Layers.All)
			{
				if (!map.Layers.TryGetValue(item3, out var value2) || map.Autotiles?.Layers == null || !map.Autotiles.Layers.TryGetValue(item3, out var value3))
				{
					continue;
				}
				for (int i = 0; i < mapWidth; i++)
				{
					for (int j = 0; j < mapHeight; j++)
					{
						Tile tile2 = value2[i, j];
						if (!(tile2.TilesetTexture is IGameTexture gameTexture) || tile2.X < 0 || tile2.Y < 0 || tile2.X * tileWidth >= gameTexture.Width || tile2.Y * tileHeight >= gameTexture.Height)
						{
							continue;
						}
						Autotile autotile = value3[i, j];
						if (autotile.RenderState == 1)
						{
							Graphics.DrawGameTexture(gameTexture, new FloatRect(tile2.X * tileWidth, tile2.Y * tileHeight, tileWidth, tileHeight), new FloatRect(i * tile, j * tile, tile, tile), Color.White, gameRenderTexture);
						}
						else if (autotile.RenderState == 2)
						{
							(int, int) tuple = tile2.Autotile switch
							{
								5 => (0, -tileHeight), 
								4 => (0, -tileHeight), 
								_ => (0, 0), 
							};
							int item = tuple.Item1;
							int item2 = tuple.Item2;
							for (int k = 1; k <= 4; k++)
							{
								Point16i point16i = autotile.QuarterTile[k];
								bool flag = ((k == 2 || k == 4) ? true : false);
								float num2 = (flag ? num : 0f);
								flag = (uint)(k - 3) <= 1u;
								float num3 = (flag ? num : 0f);
								Graphics.DrawGameTexture(gameTexture, new FloatRect(point16i.X + item, point16i.Y + item2, (float)tileWidth / 2f, (float)tileHeight / 2f), new FloatRect((float)(i * tile) + num2, (float)(j * tile) + num3, num, num), Color.White, gameRenderTexture);
							}
						}
					}
				}
			}
			Color renderColor = WithAlpha(Night, 125f);
			for (int l = 0; l < mapWidth; l++)
			{
				for (int m = 0; m < mapHeight; m++)
				{
					if (!Walkable(map, l, m))
					{
						Graphics.DrawGameTexture(renderer.WhitePixel, new FloatRect(0f, 0f, 1f, 1f), new FloatRect(l * tile, m * tile, tile, tile), renderColor, gameRenderTexture);
					}
				}
			}
		}
		finally
		{
			renderer.Scale = scale;
		}
		if (Caches.Count > 96)
		{
			ClearCaches();
		}
		Caches[(map.Id, tile)] = new MapCache
		{
			Map = map,
			Texture = gameRenderTexture,
			TileSize = tile
		};
		return gameRenderTexture;
	}

	private static void ClearCaches()
	{
		foreach (MapCache value in Caches.Values)
		{
			value.Texture.Dispose();
		}
		Caches.Clear();
	}

	private static bool Walkable(MapInstance map, int x, int y)
	{
		MapAttribute[,] attributes = map.Attributes;
		bool flag = attributes == null || x < 0 || y < 0 || x >= attributes.GetLength(0) || y >= attributes.GetLength(1);
		bool flag2 = flag;
		if (!flag2)
		{
			MapAttribute mapAttribute = attributes[x, y];
			bool flag3 = ((mapAttribute is MapBlockedAttribute || mapAttribute is MapAnimationAttribute { IsBlock: not false }) ? true : false);
			flag2 = !flag3;
		}
		return flag2;
	}

	private static void DrawHeader(GameRenderer renderer, FloatRect view, MapInstance here, float panelX, float panelY, float fade)
	{
		string text = (here.Name ?? string.Empty).ToUpperInvariant();
		Vector2 vector = renderer.MeasureText(text, _titleFont, 20, 1f);
		float num = panelX + 14f + 2f;
		float y = panelY + (46f - vector.Y) / 2f;
		Text(renderer, view, text, _titleFont, 20, num, y, WithAlpha(White, 255f * fade));
		bool flag = here.ZoneType == MapZone.Safe;
		string text2 = (flag ? Strings.LocalMap.ZoneSafe : Strings.LocalMap.ZonePvp).ToString();
		Vector2 vector2 = renderer.MeasureText(text2, _smallFont, 10, 1f);
		FloatRect rect = new FloatRect(num + vector.X + 10f, panelY + 23f - 10f, vector2.X + 16f, 20f);
		NineSlice(renderer, view, "carte_environs_pastille.png", rect, 9, WithAlpha(flag ? SafeBack : PvpBack, 190f * fade));
		Text(renderer, view, text2, _smallFont, 10, rect.X + 8f, rect.Y + (rect.Height - vector2.Y) / 2f, WithAlpha(flag ? SafeText : PvpText, 255f * fade));
	}

	private static void DrawExits(GameRenderer renderer, FloatRect view, MapInstance here, Point hereGrid, Guid[,] grid, FloatRect hereRect, FloatRect plan, int tile, float fade)
	{
		IGameTexture texture = Globals.ContentManager.GetTexture(TextureType.Gui, "carte_environs_sortie.png");
		int mapWidth = Options.Instance.Map.MapWidth;
		int mapHeight = Options.Instance.Map.MapHeight;
		(int, int, float)[] array = new(int, int, float)[4]
		{
			(0, -1, 0f),
			(0, 1, 180f),
			(-1, 0, 270f),
			(1, 0, 90f)
		};
		(int, int, float)[] array2 = array;
		for (int i = 0; i < array2.Length; i++)
		{
			(int, int, float) tuple = array2[i];
			int dx = tuple.Item1;
			int dy = tuple.Item2;
			float item = tuple.Item3;
			int num = hereGrid.X + dx;
			int num2 = hereGrid.Y + dy;
			if (num < 0 || num2 < 0 || num >= grid.GetLength(0) || num2 >= grid.GetLength(1) || grid[num, num2] == Guid.Empty || !MapInstance.TryGet(grid[num, num2], out MapInstance instance) || !instance.IsLoaded)
			{
				continue;
			}
			int num3 = ((dx == 0) ? mapWidth : mapHeight);
			List<(int, int)> list = new List<(int, int)>();
			int? num4 = null;
			for (int j = 0; j <= num3; j++)
			{
				bool flag = false;
				if (j < num3)
				{
					int y;
					int x;
					if (dx != 0)
					{
						int num5 = ((dx >= 0) ? (mapWidth - 1) : 0);
						int num6 = j;
						y = num6;
						x = num5;
					}
					else
					{
						int num7 = j;
						int num6 = ((dy >= 0) ? (mapHeight - 1) : 0);
						y = num6;
						x = num7;
					}
					int y2;
					int x2;
					if (dx != 0)
					{
						int num8 = ((dx < 0) ? (mapWidth - 1) : 0);
						int num6 = j;
						y2 = num6;
						x2 = num8;
					}
					else
					{
						int num9 = j;
						int num6 = ((dy < 0) ? (mapHeight - 1) : 0);
						y2 = num6;
						x2 = num9;
					}
					flag = Walkable(here, x, y) && Walkable(instance, x2, y2);
				}
				if (flag && !num4.HasValue)
				{
					num4 = j;
				}
				else if (!flag && num4.HasValue)
				{
					list.Add((num4.Value, j - 1));
					num4 = null;
				}
			}
			if (list.Count == 0)
			{
				continue;
			}
			foreach (var item4 in list)
			{
				int item2 = item4.Item1;
				int item3 = item4.Item2;
				Vector2 point = Anchor((float)(item2 + item3) / 2f);
				if (texture != null && Contains(plan, point, 6f))
				{
					Graphics.DrawGameTexture(texture, new FloatRect(0f, 0f, texture.Width, texture.Height), At(view, new FloatRect(MathF.Round(point.X - (float)texture.Width / 2f), MathF.Round(point.Y - (float)texture.Height / 2f), texture.Width, texture.Height)), WithAlpha(Color.White, 255f * fade), null, GameBlendModes.None, null, item);
				}
			}
			(int, int) tuple2 = list.OrderByDescending<(int, int), int>(((int Start, int End) p) => p.End - p.Start).First();
			string text = instance.Name ?? string.Empty;
			Vector2 vector = renderer.MeasureText(text, _nameFont, 14, 1f);
			Vector2 vector2 = Anchor((float)(tuple2.Item1 + tuple2.Item2) / 2f);
			float num10 = vector2.X - vector.X / 2f;
			float num11 = vector2.Y - vector.Y / 2f;
			if (item != 0f)
			{
				if (item != 180f)
				{
					num10 = ((item != 270f) ? (num10 + (vector.X / 2f + 20f)) : (num10 - (vector.X / 2f + 20f)));
				}
				else
				{
					num11 += 18f;
				}
			}
			else
			{
				num11 -= 18f;
			}
			num10 = Math.Clamp(num10, plan.X + 8f, plan.X + plan.Width - vector.X - 8f);
			num11 = Math.Clamp(num11, plan.Y + 4f, plan.Y + plan.Height - vector.Y - 4f);
			FloatRect rect = new FloatRect(num10 - 7f, num11 - 1f, vector.X + 14f, vector.Y + 2f);
			NineSlice(renderer, view, "carte_environs_pastille.png", rect, 9, WithAlpha(Night, 205f * fade));
			Text(renderer, view, text, _nameFont, 14, num10, num11, WithAlpha(GoldLight, 255f * fade));
			Vector2 Anchor(float along)
			{
				if (dx != 0)
				{
					return new Vector2((dx < 0) ? hereRect.X : (hereRect.X + hereRect.Width), hereRect.Y + (along + 0.5f) * (float)tile);
				}
				return new Vector2(hereRect.X + (along + 0.5f) * (float)tile, (dy < 0) ? hereRect.Y : (hereRect.Y + hereRect.Height));
			}
		}
	}

	private static void DrawCharacters(GameRenderer renderer, FloatRect view, Player me, Guid[,] grid, Point hereGrid, FloatRect plan, Func<float, float, Vector2> toPlan, float fade)
	{
		IGameTexture texture = Globals.ContentManager.GetTexture(TextureType.Gui, "hud_quete_marques.png");
		int tileWidth = Options.Instance.Map.TileWidth;
		int tileHeight = Options.Instance.Map.TileHeight;
		List<(string, Vector2, Color)> list = new List<(string, Vector2, Color)>();
		for (int i = hereGrid.Y - 1; i <= hereGrid.Y + 1; i++)
		{
			for (int j = hereGrid.X - 1; j <= hereGrid.X + 1; j++)
			{
				if (j < 0 || i < 0 || j >= grid.GetLength(0) || i >= grid.GetLength(1) || grid[j, i] == Guid.Empty || !MapInstance.TryGet(grid[j, i], out MapInstance instance))
				{
					continue;
				}
				foreach (Entity value2 in instance.LocalEntities.Values)
				{
					if (!(value2 is Event { IsDisposed: false, ShouldDraw: not false } obj))
					{
						continue;
					}
					EventGraphic graphic = obj.Graphic;
					if (graphic == null || graphic.Type != EventGraphicType.Sprite || string.IsNullOrWhiteSpace(obj.Name))
					{
						continue;
					}
					Vector2 vector = toPlan((float)(instance.X + obj.X * tileWidth) + (float)tileWidth / 2f, (float)(instance.Y + obj.Y * tileHeight) + (float)tileHeight / 2f);
					if (Contains(plan, vector, 4f))
					{
						if (obj.QuestMarker != 0 && texture != null)
						{
							int num = ((obj.QuestMarker == 2) ? 1 : 0);
							Graphics.DrawGameTexture(texture, new FloatRect(num * 24, 0f, 24f, 34f), At(view, new FloatRect(MathF.Round(vector.X - 9f), MathF.Round(vector.Y - 22f), 18f, 25f)), WithAlpha(Color.White, 255f * fade));
							list.Add((obj.Name, vector, GoldLight));
						}
						else
						{
							Dot(renderer, view, vector, 4.5f, WithAlpha(Night, 255f * fade));
							Dot(renderer, view, vector, 3f, WithAlpha(White, 230f * fade));
						}
					}
				}
			}
		}
		IGameTexture icone = Dungeon.Icone;
		if (icone != null)
		{
			foreach (DungeonDescriptor item4 in DatabaseObject<DungeonDescriptor>.Lookup.Values.OfType<DungeonDescriptor>())
			{
				if (item4.Rooms.Count != 0 && MapInstance.TryGet(item4.EntranceMapId, out MapInstance instance2) && grid.Cast<Guid>().Contains(instance2.Id))
				{
					Vector2 vector2 = toPlan((float)instance2.X + ((float)item4.EntranceX + 0.5f) * (float)tileWidth, (float)instance2.Y + ((float)item4.EntranceY + 0.5f) * (float)tileHeight);
					if (Contains(plan, vector2, 10f))
					{
						Dot(renderer, view, vector2, 11f, WithAlpha(Night, 235f * fade));
						Graphics.DrawGameTexture(icone, new FloatRect(0f, 0f, icone.Width, icone.Height), At(view, new FloatRect(MathF.Round(vector2.X - 9f), MathF.Round(vector2.Y - 9f), 18f, 18f)), WithAlpha(Color.White, 255f * fade));
						list.Add((item4.Name, new Vector2(vector2.X, vector2.Y + 5f), GoldLight));
					}
				}
			}
		}
		foreach (IPartyMember item5 in me.Party)
		{
			if (!(item5.Id == me.Id) && Globals.Entities.TryGetValue(item5.Id, out Entity value) && value != null)
			{
				Vector2 vector3 = toPlan(value.Center.X, value.Center.Y);
				if (Contains(plan, vector3, 4f))
				{
					Dot(renderer, view, vector3, 6f, WithAlpha(Night, 255f * fade));
					Dot(renderer, view, vector3, 4.5f, WithAlpha(Blue, 255f * fade));
					list.Add((item5.Name, vector3, Blue));
				}
			}
		}
		foreach (var item6 in list)
		{
			string item = item6.Item1;
			Vector2 item2 = item6.Item2;
			Color item3 = item6.Item3;
			Vector2 vector4 = renderer.MeasureText(item, _smallFont, 10, 1f);
			float num2 = Math.Clamp(item2.X - vector4.X / 2f, plan.X + 4f, plan.X + plan.Width - vector4.X - 4f);
			float num3 = Math.Min(item2.Y + 8f, plan.Y + plan.Height - vector4.Y - 2f);
			NineSlice(renderer, view, "carte_environs_pastille.png", new FloatRect(num2 - 5f, num3 - 1f, vector4.X + 10f, vector4.Y + 2f), 9, WithAlpha(Night, 195f * fade));
			Text(renderer, view, item, _smallFont, 10, num2, num3, WithAlpha(item3, 255f * fade));
		}
	}

	private static void DrawObjective(GameRenderer renderer, FloatRect view, Player me, FloatRect plan, Func<float, float, Vector2> toPlan, Vector2 origin, float fade)
	{
		Guid guid = QuestMarkers.GuidedQuest();
		IGameTexture texture = Globals.ContentManager.GetTexture(TextureType.Gui, "hud_quete_repere.png");
		if (guid == Guid.Empty || texture == null)
		{
			return;
		}
		int tileWidth = Options.Instance.Map.TileWidth;
		int tileHeight = Options.Instance.Map.TileHeight;
		int num = Options.Instance.Map.MapWidth * tileWidth;
		int num2 = Options.Instance.Map.MapHeight * tileHeight;
		Vector2? vector = null;
		float num3 = float.MaxValue;
		foreach (var questTarget in Globals.QuestTargets)
		{
			if (!(questTarget.QuestId != guid) && Globals.GridMaps.TryGetValue(questTarget.MapId, out var value))
			{
				Vector2 vector2 = new Vector2((float)(value.X * num + questTarget.X * tileWidth) + (float)tileWidth / 2f, (float)(value.Y * num2 + questTarget.Y * tileHeight) + (float)tileHeight / 2f);
				float num4 = Vector2.DistanceSquared(vector2, origin);
				if (num4 < num3)
				{
					num3 = num4;
					vector = vector2;
				}
			}
		}
		if (vector.HasValue)
		{
			Vector2 valueOrDefault = vector.GetValueOrDefault();
			Vector2 vector3 = toPlan(valueOrDefault.X, valueOrDefault.Y);
			Vector2 vector4 = new Vector2(plan.X + plan.Width / 2f, plan.Y + plan.Height / 2f);
			if (!Contains(plan, vector3, 14f))
			{
				Vector2 vector5 = vector3 - vector4;
				float num5 = plan.Width / 2f - 16f;
				float num6 = plan.Height / 2f - 18f;
				float num7 = MathF.Min((vector5.X == 0f) ? float.MaxValue : (num5 / MathF.Abs(vector5.X)), (vector5.Y == 0f) ? float.MaxValue : (num6 / MathF.Abs(vector5.Y)));
				vector3 = vector4 + vector5 * num7;
			}
			Graphics.DrawGameTexture(texture, new FloatRect(0f, 0f, texture.Width, texture.Height), At(view, new FloatRect(MathF.Round(vector3.X - (float)texture.Width / 2f), MathF.Round(vector3.Y - (float)texture.Height + 4f), texture.Width, texture.Height)), WithAlpha(Color.White, 255f * fade));
			int num8 = (int)MathF.Max(MathF.Abs(valueOrDefault.X - origin.X) / (float)tileWidth, MathF.Abs(valueOrDefault.Y - origin.Y) / (float)tileHeight);
			string text = (DatabaseObject<QuestDescriptor>.Get(guid)?.Name ?? string.Empty) + "  ·  " + Strings.Parties.Tiles.ToString(num8);
			Vector2 vector6 = renderer.MeasureText(text, _smallFont, 10, 1f);
			float num9 = ((vector3.X + vector6.X + 30f > plan.X + plan.Width) ? (vector3.X - vector6.X - 22f) : (vector3.X + 16f));
			float num10 = Math.Clamp(vector3.Y - (float)texture.Height / 2f - vector6.Y / 2f, plan.Y + 3f, plan.Y + plan.Height - vector6.Y - 3f);
			NineSlice(renderer, view, "carte_environs_pastille.png", new FloatRect(num9 - 6f, num10 - 1f, vector6.X + 12f, vector6.Y + 2f), 9, WithAlpha(Night, 205f * fade));
			Text(renderer, view, text, _smallFont, 10, num9, num10, WithAlpha(Gold, 255f * fade));
		}
	}

	private static void DrawPlayer(GameRenderer renderer, FloatRect view, Player me, FloatRect plan, float fade)
	{
		Vector2 center = new Vector2(plan.X + plan.Width / 2f, plan.Y + plan.Height / 2f);
		IGameTexture texture = Globals.ContentManager.GetTexture(TextureType.Misc, "ki_halo.png");
		if (texture != null)
		{
			Graphics.DrawGameTexture(texture, new FloatRect(0f, 0f, texture.Width, texture.Height), At(view, new FloatRect(center.X - 18f, center.Y - 18f, 36f, 36f)), WithAlpha(Orange, 150f * fade), null, GameBlendModes.Add);
		}
		IGameTexture texture2 = Globals.ContentManager.GetTexture(TextureType.Gui, "carte_environs_joueur.png");
		if (texture2 == null)
		{
			Dot(renderer, view, center, 6f, WithAlpha(Orange, 255f * fade));
			return;
		}
		float rotationDegrees = me.DirectionFacing switch
		{
			Direction.UpRight => 45f, 
			Direction.Right => 90f, 
			Direction.DownRight => 135f, 
			Direction.Down => 180f, 
			Direction.DownLeft => 225f, 
			Direction.Left => 270f, 
			Direction.UpLeft => 315f, 
			_ => 0f, 
		};
		Graphics.DrawGameTexture(texture2, new FloatRect(0f, 0f, texture2.Width, texture2.Height), At(view, new FloatRect(MathF.Round(center.X - (float)texture2.Width / 2f), MathF.Round(center.Y - (float)texture2.Height / 2f), texture2.Width, texture2.Height)), WithAlpha(Color.White, 255f * fade), null, GameBlendModes.None, null, rotationDegrees);
	}

	private static void DrawFooter(GameRenderer renderer, FloatRect view, FloatRect plan, float fade)
	{
		float num = plan.Y + plan.Height + 9f;
		float x = plan.X;
		string text = Strings.LocalMap.Release.ToString();
		Vector2 vector = renderer.MeasureText(text, _bodyFont, 11, 1f);
		Text(renderer, view, text, _bodyFont, 11, x, num + (20f - vector.Y) / 2f, WithAlpha(Muted, 255f * fade));
		x += vector.X + 6f;
		string text2 = KeyName();
		Vector2 vector2 = renderer.MeasureText(text2, _smallFont, 11, 1f);
		FloatRect rect = new FloatRect(x, num, Math.Max(24f, vector2.X + 12f), 22f);
		NineSlice(renderer, view, "carte_environs_touche.png", rect, 7, WithAlpha(Color.White, 255f * fade));
		Text(renderer, view, text2, _smallFont, 11, rect.X + (rect.Width - vector2.X) / 2f, rect.Y + (18f - vector2.Y) / 2f, WithAlpha(White, 255f * fade));
		x += rect.Width + 6f;
		string text3 = Strings.LocalMap.ToClose.ToString();
		Vector2 vector3 = renderer.MeasureText(text3, _bodyFont, 11, 1f);
		Text(renderer, view, text3, _bodyFont, 11, x, num + (20f - vector3.Y) / 2f, WithAlpha(Muted, 255f * fade));
		float num2 = plan.X + plan.Width;
		(string, string)[] array = new(string, string)[4]
		{
			(Strings.LocalMap.LegendExit, "exit"),
			(Strings.LocalMap.LegendTeammate, "mate"),
			(Strings.LocalMap.LegendNpc, "npc"),
			(Strings.LocalMap.LegendYou, "you")
		};
		(string, string)[] array2 = array;
		for (int i = 0; i < array2.Length; i++)
		{
			(string, string) tuple = array2[i];
			string item = tuple.Item1;
			string item2 = tuple.Item2;
			Vector2 vector4 = renderer.MeasureText(item, _bodyFont, 11, 1f);
			num2 -= vector4.X;
			Text(renderer, view, item, _bodyFont, 11, num2, num + (20f - vector4.Y) / 2f, WithAlpha(Muted, 255f * fade));
			num2 -= 14f;
			Vector2 center = new Vector2(num2 + 5f, num + 10f);
			switch (item2)
			{
			case "exit":
			{
				IGameTexture texture2 = Globals.ContentManager.GetTexture(TextureType.Gui, "carte_environs_sortie.png");
				if (texture2 != null)
				{
					Graphics.DrawGameTexture(texture2, new FloatRect(0f, 0f, texture2.Width, texture2.Height), At(view, new FloatRect(center.X - 6f, center.Y - 5f, 12f, 10.5f)), WithAlpha(Color.White, 255f * fade), null, GameBlendModes.None, null, 90f);
				}
				break;
			}
			case "mate":
				Dot(renderer, view, center, 4f, WithAlpha(Blue, 255f * fade));
				break;
			case "npc":
				Dot(renderer, view, center, 3f, WithAlpha(White, 230f * fade));
				break;
			case "you":
			{
				IGameTexture texture = Globals.ContentManager.GetTexture(TextureType.Gui, "carte_environs_joueur.png");
				if (texture != null)
				{
					Graphics.DrawGameTexture(texture, new FloatRect(0f, 0f, texture.Width, texture.Height), At(view, new FloatRect(center.X - 7f, center.Y - 7f, 14f, 14f)), WithAlpha(Color.White, 255f * fade));
				}
				break;
			}
			}
			num2 -= 18f;
		}
	}

	private static string KeyName()
	{
		IControlSet activeControls = Controls.ActiveControls;
		if (activeControls != null && activeControls.TryGetMappingFor(Control.ShowLocalMap, out ControlMapping mapping))
		{
			ControlBinding controlBinding = mapping.Bindings.FirstOrDefault((ControlBinding binding) => binding.Key != Keys.None);
			if (controlBinding != null)
			{
				return Strings.Keys.FormatKeyName(controlBinding.Modifier, controlBinding.Key);
			}
		}
		return "V";
	}

	private static FloatRect At(FloatRect view, FloatRect rect)
	{
		return new FloatRect(view.X + rect.X, view.Y + rect.Y, rect.Width, rect.Height);
	}

	private static Color WithAlpha(Color color, float alpha)
	{
		return new Color((int)Math.Clamp(alpha * (float)(int)color.A / 255f, 0f, 255f), color.R, color.G, color.B);
	}

	private static void Rect(GameRenderer renderer, FloatRect view, FloatRect rect, Color color)
	{
		Graphics.DrawGameTexture(renderer.WhitePixel, new FloatRect(0f, 0f, 1f, 1f), At(view, rect), color);
	}

	private static void Frame(GameRenderer renderer, FloatRect view, FloatRect rect, Color color)
	{
		Rect(renderer, view, new FloatRect(rect.X, rect.Y, rect.Width, 1f), color);
		Rect(renderer, view, new FloatRect(rect.X, rect.Y + rect.Height - 1f, rect.Width, 1f), color);
		Rect(renderer, view, new FloatRect(rect.X, rect.Y, 1f, rect.Height), color);
		Rect(renderer, view, new FloatRect(rect.X + rect.Width - 1f, rect.Y, 1f, rect.Height), color);
	}

	private static void DashedRect(GameRenderer renderer, FloatRect view, FloatRect rect, FloatRect clip, Color color)
	{
		Horizontal(rect.Y);
		Horizontal(rect.Y + rect.Height - 1f);
		Vertical(rect.X);
		Vertical(rect.X + rect.Width - 1f);
		void Horizontal(float y)
		{
			if (!(y < clip.Y) && !(y > clip.Y + clip.Height))
			{
				for (float num = rect.X; num < rect.X + rect.Width; num += 9f)
				{
					float num2 = MathF.Max(num, clip.X);
					float num3 = MathF.Min(MathF.Min(num + 5f, rect.X + rect.Width), clip.X + clip.Width);
					if (num3 > num2)
					{
						Rect(renderer, view, new FloatRect(MathF.Round(num2), MathF.Round(y), MathF.Round(num3 - num2), 1f), color);
					}
				}
			}
		}
		void Vertical(float x)
		{
			if (!(x < clip.X) && !(x > clip.X + clip.Width))
			{
				for (float num = rect.Y; num < rect.Y + rect.Height; num += 9f)
				{
					float num2 = MathF.Max(num, clip.Y);
					float num3 = MathF.Min(MathF.Min(num + 5f, rect.Y + rect.Height), clip.Y + clip.Height);
					if (num3 > num2)
					{
						Rect(renderer, view, new FloatRect(MathF.Round(x), MathF.Round(num2), 1f, MathF.Round(num3 - num2)), color);
					}
				}
			}
		}
	}

	private static void Dot(GameRenderer renderer, FloatRect view, Vector2 center, float radius, Color color)
	{
		IGameTexture texture = Globals.ContentManager.GetTexture(TextureType.Gui, "carte_environs_pastille.png");
		if (texture != null)
		{
			Graphics.DrawGameTexture(texture, new FloatRect(0f, 0f, texture.Width, texture.Height), At(view, new FloatRect(center.X - radius, center.Y - radius, 2f * radius, 2f * radius)), color);
		}
		else
		{
			Rect(renderer, view, new FloatRect(center.X - radius, center.Y - radius, 2f * radius, 2f * radius), color);
		}
	}

	private static void NineSlice(GameRenderer renderer, FloatRect view, string textureName, FloatRect rect, int margin, Color color)
	{
		IGameTexture texture = Globals.ContentManager.GetTexture(TextureType.Gui, textureName);
		if (texture == null)
		{
			Rect(renderer, view, rect, color);
			return;
		}
		int num = Math.Min(margin, (int)Math.Min(rect.Width, rect.Height) / 2);
		int width = texture.Width;
		int height = texture.Height;
		float[] array = new float[4]
		{
			0f,
			margin,
			width - margin,
			width
		};
		float[] array2 = new float[4]
		{
			0f,
			margin,
			height - margin,
			height
		};
		float[] array3 = new float[4]
		{
			rect.X,
			rect.X + (float)num,
			rect.X + rect.Width - (float)num,
			rect.X + rect.Width
		};
		float[] array4 = new float[4]
		{
			rect.Y,
			rect.Y + (float)num,
			rect.Y + rect.Height - (float)num,
			rect.Y + rect.Height
		};
		for (int i = 0; i < 3; i++)
		{
			for (int j = 0; j < 3; j++)
			{
				float num2 = array3[j + 1] - array3[j];
				float num3 = array4[i + 1] - array4[i];
				if (!(num2 <= 0f) && !(num3 <= 0f))
				{
					Graphics.DrawGameTexture(texture, new FloatRect(array[j], array2[i], array[j + 1] - array[j], array2[i + 1] - array2[i]), At(view, new FloatRect(array3[j], array4[i], num2, num3)), color);
				}
			}
		}
	}

	private static void Text(GameRenderer renderer, FloatRect view, string text, IFont font, int size, float x, float y, Color color)
	{
		FloatRect clipRect = new FloatRect(0f, 0f, renderer.ScreenWidth, renderer.ScreenHeight);
		renderer.DrawString(text, font, size, MathF.Round(x) + 1f, MathF.Round(y) + 1f, 1f, WithAlpha(Color.Black, (float)(int)color.A * 0.7f), worldPos: true, null, clipRect);
		renderer.DrawString(text, font, size, MathF.Round(x), MathF.Round(y), 1f, color, worldPos: true, null, clipRect);
	}

	private static FloatRect Intersect(FloatRect a, FloatRect b)
	{
		float num = MathF.Max(a.X, b.X);
		float num2 = MathF.Max(a.Y, b.Y);
		float num3 = MathF.Min(a.X + a.Width, b.X + b.Width);
		float num4 = MathF.Min(a.Y + a.Height, b.Y + b.Height);
		if (!(num3 > num) || !(num4 > num2))
		{
			return FloatRect.Empty;
		}
		return new FloatRect(num, num2, num3 - num, num4 - num2);
	}

	private static bool Contains(FloatRect rect, Vector2 point, float inset)
	{
		if (point.X >= rect.X + inset && point.X <= rect.X + rect.Width - inset && point.Y >= rect.Y + inset)
		{
			return point.Y <= rect.Y + rect.Height - inset;
		}
		return false;
	}
}
