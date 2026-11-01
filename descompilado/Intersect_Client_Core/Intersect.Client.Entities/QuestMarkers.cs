using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Intersect.Client.Core;
using Intersect.Client.Entities.Events;
using Intersect.Client.Framework.Content;
using Intersect.Client.Framework.Entities;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.General;
using Intersect.Client.Interface;
using Intersect.Client.Interface.Game;
using Intersect.Client.Interface.Game.EntityPanel;
using Intersect.Client.Localization;
using Intersect.Client.Maps;
using Intersect.Framework.Core;
using Intersect.Framework.Core.GameObjects.Events;
using Intersect.Framework.Core.GameObjects.Mapping.Tilesets;
using Intersect.Framework.Core.GameObjects.Maps;
using Intersect.Framework.Core.GameObjects.NPCs;
using Intersect.Framework.Core.GameObjects.Quests;
using Intersect.GameObjects;
using Intersect.Models;

namespace Intersect.Client.Entities;

public static class QuestMarkers
{
	private const int BadgeSize = 32;

	private const int Directions = 16;

	private const int EdgeMargin = 30;

	private const int LabelGap = 16;

	private const int LabelPaddingX = 5;

	private const int LabelPaddingY = 2;

	private const int NameFontSize = 11;

	private const int DistanceFontSize = 9;

	private const int GlyphWidth = 24;

	private const int GlyphHeight = 34;

	private const int PinWidth = 22;

	private const int PinHeight = 26;

	private const float BobPixels = 2f;

	private const float BobPeriodMs = 1200f;

	public static readonly Color Gold = new Color(255, 245, 190, 90);

	private static readonly Color DistanceColor = new Color(255, 176, 184, 200);

	private static readonly Color ShadowColor = new Color(180, 0, 0, 0);

	private static readonly Color BandColor = new Color(170, 11, 14, 23);

	private static IFont? _nameFont;

	private static IFont? _smallFont;

	private const int Jeu = 6;

	private static readonly List<FloatRect> ZonesEcran = new List<FloatRect>();

	private static readonly List<FloatRect> Obstacles = new List<FloatRect>();

	private static readonly List<Vector2> Ouverts = new List<Vector2>();

	private static readonly List<Vector2> Vus = new List<Vector2>();

	public static void Draw()
	{
		GameRenderer renderer = Graphics.Renderer;
		if (renderer == null)
		{
			return;
		}
		Player me = Globals.Me;
		if (me != null && me.MapInstance != null)
		{
			if (_nameFont == null)
			{
				_nameFont = Globals.ContentManager.GetFont("sairanom") ?? Globals.ContentManager.GetFont("rajdhanibold");
			}
			if (_smallFont == null)
			{
				_smallFont = Globals.ContentManager.GetFont("chakrapetch");
			}
			if (_nameFont != null && _smallFont != null)
			{
				float num = Math.Max(0.25f, Graphics.WorldScale);
				float num2 = 1f / num;
				float bob = MathF.Sin((float)Timing.Global.Milliseconds * 0.0052359877f) * 2f * num2;
				HashSet<(Guid, int, int)> marked = DrawEventMarkers(renderer, num2, num, bob);
				DrawObjective(renderer, me, num2, num, bob, marked);
			}
		}
	}

	private static HashSet<(Guid MapId, int X, int Y)> DrawEventMarkers(GameRenderer renderer, float pixel, float zoom, float bob)
	{
		HashSet<(Guid, int, int)> hashSet = new HashSet<(Guid, int, int)>();
		IGameTexture texture = Globals.ContentManager.GetTexture(TextureType.Gui, "hud_quete_marques.png");
		if (texture == null)
		{
			return hashSet;
		}
		FloatRect currentView = Graphics.CurrentView;
		FloatRect worldViewport = Graphics.WorldViewport;
		int tileWidth = Options.Instance.Map.TileWidth;
		int tileHeight = Options.Instance.Map.TileHeight;
		foreach (MapInstance item3 in MapInstance.Lookup.Values.OfType<MapInstance>())
		{
			if (!Globals.GridMaps.ContainsKey(item3.Id))
			{
				continue;
			}
			foreach (Entity value in item3.LocalEntities.Values)
			{
				if (!(value is Event { QuestMarker: not 0, IsDisposed: false, ShouldDraw: not false } obj))
				{
					continue;
				}
				hashSet.Add((item3.Id, obj.X, obj.Y));
				EventGraphic graphic = obj.Graphic;
				float num;
				float num2;
				if (graphic != null && graphic.Type == EventGraphicType.Sprite && obj.Texture != null)
				{
					num = obj.Center.X;
					if (!obj.HideName && !string.IsNullOrWhiteSpace(obj.Name))
					{
						Player me = Globals.Me;
						if (me != null && (obj.IsHovered || me.TargetId == obj.Id || MathF.Max(MathF.Abs(obj.Center.X - me.Center.X) / (float)tileWidth, MathF.Abs(obj.Center.Y - me.Center.Y) / (float)tileHeight) <= 2.5f))
						{
							num2 = obj.GetLabelLocation(LabelType.Name) - 14f * pixel;
							goto IL_01fe;
						}
					}
					num2 = obj.GetTop() - 2f * pixel;
					goto IL_01fe;
				}
				num = (float)(item3.X + obj.X * tileWidth) + (float)tileWidth / 2f;
				(int Rows, int Margin) tuple = ObjectAbove(item3, obj.X, obj.Y, tileWidth, tileHeight);
				int item = tuple.Rows;
				int item2 = tuple.Margin;
				float num3 = item3.Y + (obj.Y - item) * tileHeight + item2 - 2;
				goto IL_0266;
				IL_01fe:
				num3 = num2;
				goto IL_0266;
				IL_0266:
				float num4 = 24f * pixel * Graphics.ReductionDesReperes * 1.3f;
				float num5 = 34f * pixel * Graphics.ReductionDesReperes * 1.3f;
				float num6 = Snap(num - num4 / 2f, currentView.X, zoom);
				float num7 = Snap(num3 - num5 + bob, currentView.Y, zoom);
				if (!(num6 + num4 < worldViewport.X) && !(num6 > worldViewport.X + worldViewport.Width) && !(num7 + num5 < worldViewport.Y) && !(num7 > worldViewport.Y + worldViewport.Height))
				{
					IGameTexture texture2 = Globals.ContentManager.GetTexture(TextureType.Misc, "ki_halo.png");
					if (texture2 != null)
					{
						float num8 = 0.5f + 0.5f * MathF.Sin((float)Timing.Global.Milliseconds * 0.0052359877f);
						FloatRect srcRectangle = new FloatRect(0f, 0f, texture2.Width, texture2.Height);
						float num9 = num5 * (0.95f + 0.2f * num8);
						Graphics.DrawGameTexture(texture2, srcRectangle, new FloatRect(num - num9, num7 + num5 / 2f - num9, num9 * 2f, num9 * 2f), new Color((int)(120f + 70f * num8), Gold.R, Gold.G, Gold.B), null, GameBlendModes.Add);
						float num10 = (float)(item3.Y + obj.Y * tileHeight) + (float)tileHeight * 0.75f;
						float num11 = (float)tileWidth * (1.5f + 0.25f * num8);
						float num12 = (float)tileHeight * 0.6f;
						Graphics.DrawGameTexture(texture2, srcRectangle, new FloatRect(num - num11 / 2f, num10 - num12 / 2f, num11, num12), new Color((int)(110f + 60f * num8), Gold.R, Gold.G, Gold.B), null, GameBlendModes.Add);
					}
					int num13 = ((obj.QuestMarker == 2) ? 1 : 0);
					Graphics.DrawGameTexture(texture, new FloatRect(num13 * 24, 0f, 24f, 34f), new FloatRect(num6, num7, num4, num5), Color.White);
				}
			}
		}
		return hashSet;
	}

	private static List<Entity> MonstresVises(Guid questId)
	{
		List<Entity> list = new List<Entity>();
		Player me = Globals.Me;
		if (me != null && me.QuestProgress.TryGetValue(questId, out QuestProgress progress) && !(progress.TaskId == Guid.Empty))
		{
			QuestTaskDescriptor questTaskDescriptor = DatabaseObject<QuestDescriptor>.Get(questId)?.Tasks.FirstOrDefault((QuestTaskDescriptor t) => t.Id == progress.TaskId);
			if (questTaskDescriptor != null && questTaskDescriptor.Objective == QuestObjective.KillNpcs)
			{
				string name = DatabaseObject<NPCDescriptor>.GetName(questTaskDescriptor.TargetId);
				if (string.IsNullOrWhiteSpace(name))
				{
					return list;
				}
				string b = Traduction.Contenu(name);
				FloatRect worldViewport = Graphics.WorldViewport;
				{
					foreach (Entity value in Globals.Entities.Values)
					{
						bool flag = ((value == null || value is Player || value is Event) ? true : false);
						if (!flag && !value.IsDisposed && !value.IsDying && value.ShouldDraw && (string.Equals(value.Name, name, StringComparison.Ordinal) || string.Equals(value.Name, b, StringComparison.Ordinal)))
						{
							Vector2 center = value.Center;
							if (center.X > worldViewport.X && center.X < worldViewport.X + worldViewport.Width && center.Y > worldViewport.Y && center.Y < worldViewport.Y + worldViewport.Height)
							{
								list.Add(value);
							}
						}
					}
					return list;
				}
			}
		}
		return list;
	}

	internal static Guid GuidedQuest()
	{
		List<(Guid, Guid, int, int)> questTargets = Globals.QuestTargets;
		if (questTargets.Count == 0)
		{
			return Guid.Empty;
		}
		Guid followed = TrackedQuests.Followed;
		if (followed != Guid.Empty && questTargets.Any<(Guid, Guid, int, int)>(((Guid QuestId, Guid MapId, int X, int Y) t) => t.QuestId == followed && Globals.GridMaps.ContainsKey(t.MapId)))
		{
			return followed;
		}
		Guid story = Globals.StoryQuestId;
		if (story != Guid.Empty && questTargets.Any<(Guid, Guid, int, int)>(((Guid QuestId, Guid MapId, int X, int Y) t) => t.QuestId == story && Globals.GridMaps.ContainsKey(t.MapId)))
		{
			return story;
		}
		foreach (Guid questId in TrackedQuests.Pinned)
		{
			if (questTargets.Any<(Guid, Guid, int, int)>(((Guid QuestId, Guid MapId, int X, int Y) t) => t.QuestId == questId && Globals.GridMaps.ContainsKey(t.MapId)))
			{
				return questId;
			}
		}
		return questTargets.FirstOrDefault<(Guid, Guid, int, int)>(((Guid QuestId, Guid MapId, int X, int Y) t) => Globals.GridMaps.ContainsKey(t.MapId)).Item1;
	}

	internal static int TilesToTarget(Guid questId)
	{
		Player me = Globals.Me;
		if (me == null)
		{
			return -1;
		}
		int tileWidth = Options.Instance.Map.TileWidth;
		int tileHeight = Options.Instance.Map.TileHeight;
		int num = Options.Instance.Map.MapWidth * tileWidth;
		int num2 = Options.Instance.Map.MapHeight * tileHeight;
		Vector2 center = me.Center;
		int num3 = -1;
		foreach (var questTarget in Globals.QuestTargets)
		{
			if (!(questTarget.QuestId != questId) && Globals.GridMaps.TryGetValue(questTarget.MapId, out var value))
			{
				float num4 = (float)(value.X * num + questTarget.X * tileWidth) + (float)tileWidth / 2f;
				float num5 = (float)(value.Y * num2 + questTarget.Y * tileHeight) + (float)tileHeight / 2f;
				int num6 = (int)MathF.Max(MathF.Abs(num4 - center.X) / (float)tileWidth, MathF.Abs(num5 - center.Y) / (float)tileHeight);
				if (num3 < 0 || num6 < num3)
				{
					num3 = num6;
				}
			}
		}
		return num3;
	}

	internal static Vector2 HorsDeLInterface(Vector2 depart, FloatRect viewport, float pixel, Func<Vector2, FloatRect> boite)
	{
		if (!Intersect.Client.Interface.Interface.HasInGameUI)
		{
			return depart;
		}
		Intersect.Client.Interface.Interface.GameUi.ZonesDeLInterface(ZonesEcran);
		if (ZonesEcran.Count == 0)
		{
			return depart;
		}
		Obstacles.Clear();
		float num = 6f * pixel;
		foreach (FloatRect item in ZonesEcran)
		{
			Obstacles.Add(new FloatRect(viewport.X + item.X * pixel - num, viewport.Y + item.Y * pixel - num, item.Width * pixel + 2f * num, item.Height * pixel + 2f * num));
		}
		float num2 = 30f * pixel;
		float gauche = viewport.X + num2 - 0.01f;
		float droite = viewport.X + viewport.Width - num2 + 0.01f;
		float haut = viewport.Y + num2 - 0.01f;
		float bas = viewport.Y + viewport.Height - num2 + 0.01f;
		Ouverts.Clear();
		Vus.Clear();
		Ouverts.Add(depart);
		Vus.Add(depart);
		for (int i = 0; i < 48; i++)
		{
			if (Ouverts.Count <= 0)
			{
				break;
			}
			int index = 0;
			float num3 = float.MaxValue;
			for (int j = 0; j < Ouverts.Count; j++)
			{
				float num4 = Vector2.DistanceSquared(Ouverts[j], depart);
				if (num4 < num3)
				{
					num3 = num4;
					index = j;
				}
			}
			Vector2 vector = Ouverts[index];
			Ouverts.RemoveAt(index);
			FloatRect floatRect = boite(vector);
			FloatRect? floatRect2 = null;
			foreach (FloatRect obstacle in Obstacles)
			{
				if (floatRect.X < obstacle.X + obstacle.Width && floatRect.X + floatRect.Width > obstacle.X && floatRect.Y < obstacle.Y + obstacle.Height && floatRect.Y + floatRect.Height > obstacle.Y)
				{
					floatRect2 = obstacle;
					break;
				}
			}
			if (floatRect2.HasValue)
			{
				FloatRect valueOrDefault = floatRect2.GetValueOrDefault();
				Essayer(new Vector2(vector.X - (floatRect.X + floatRect.Width - valueOrDefault.X) - pixel, vector.Y));
				Essayer(new Vector2(vector.X + (valueOrDefault.X + valueOrDefault.Width - floatRect.X) + pixel, vector.Y));
				Essayer(new Vector2(vector.X, vector.Y - (floatRect.Y + floatRect.Height - valueOrDefault.Y) - pixel));
				Essayer(new Vector2(vector.X, vector.Y + (valueOrDefault.Y + valueOrDefault.Height - floatRect.Y) + pixel));
				continue;
			}
			return vector;
		}
		return depart;
		void Essayer(Vector2 place)
		{
			if (!(place.X < gauche) && !(place.X > droite) && !(place.Y < haut) && !(place.Y > bas))
			{
				foreach (Vector2 vu in Vus)
				{
					if (Vector2.DistanceSquared(vu, place) < pixel * pixel * 0.25f)
					{
						return;
					}
				}
				Vus.Add(place);
				Ouverts.Add(place);
			}
		}
	}

	private static void DrawObjective(GameRenderer renderer, Player me, float pixel, float zoom, float bob, HashSet<(Guid MapId, int X, int Y)> marked)
	{
		Guid guid = GuidedQuest();
		if (guid == Guid.Empty)
		{
			return;
		}
		int tileWidth = Options.Instance.Map.TileWidth;
		int tileHeight = Options.Instance.Map.TileHeight;
		int num = Options.Instance.Map.MapWidth * tileWidth;
		int num2 = Options.Instance.Map.MapHeight * tileHeight;
		Vector2 center = me.Center;
		(Guid, int, int)? tuple = null;
		Vector2 vector = Vector2.Zero;
		float num3 = float.MaxValue;
		foreach (var questTarget in Globals.QuestTargets)
		{
			if (!(questTarget.QuestId != guid) && Globals.GridMaps.TryGetValue(questTarget.MapId, out var value))
			{
				Vector2 vector2 = new Vector2((float)(value.X * num + questTarget.X * tileWidth) + (float)tileWidth / 2f, (float)(value.Y * num2 + questTarget.Y * tileHeight) + (float)tileHeight / 2f);
				float num4 = Vector2.DistanceSquared(vector2, center);
				if (num4 < num3)
				{
					num3 = num4;
					tuple = (questTarget.MapId, questTarget.X, questTarget.Y);
					vector = vector2;
				}
			}
		}
		if (!tuple.HasValue)
		{
			return;
		}
		(Guid, int, int) valueOrDefault = tuple.GetValueOrDefault();
		FloatRect currentView = Graphics.CurrentView;
		FloatRect viewport = Graphics.WorldViewport;
		bool flag = vector.X > viewport.X && vector.X < viewport.X + viewport.Width && vector.Y > viewport.Y && vector.Y < viewport.Y + viewport.Height;
		List<Entity> list = MonstresVises(guid);
		if (flag || list.Count > 0)
		{
			if (list.Count == 0 && marked.Contains(valueOrDefault))
			{
				return;
			}
			IGameTexture texture = Globals.ContentManager.GetTexture(TextureType.Gui, "hud_quete_repere.png");
			if (texture == null)
			{
				return;
			}
			if (list != null && list.Count > 0)
			{
				List<Entity> list2 = list;
				float num5 = 22f * pixel;
				float num6 = 26f * pixel;
				{
					foreach (Entity item in list2)
					{
						Graphics.DrawGameTexture(texture, new FloatRect(0f, 0f, 22f, 26f), new FloatRect(Snap(item.Center.X - num5 / 2f, currentView.X, zoom), Snap(item.GetTop() - num6 - 2f * pixel + bob, currentView.Y, zoom), num5, num6), Color.White);
					}
					return;
				}
			}
			float num7 = 22f * pixel;
			float num8 = 26f * pixel;
			Graphics.DrawGameTexture(texture, new FloatRect(0f, 0f, 22f, 26f), new FloatRect(Snap(vector.X - num7 / 2f, currentView.X, zoom), Snap(vector.Y - (float)tileHeight / 2f - num8 + bob, currentView.Y, zoom), num7, num8), Color.White);
			return;
		}
		IGameTexture texture2 = Globals.ContentManager.GetTexture(TextureType.Gui, "hud_quete_fleches.png");
		if (texture2 == null)
		{
			return;
		}
		Vector2 vector3 = new Vector2(viewport.X + viewport.Width / 2f, viewport.Y + viewport.Height / 2f);
		float num9 = viewport.Width / 2f - 30f * pixel;
		float num10 = viewport.Height / 2f - 30f * pixel;
		Vector2 direction = vector - vector3;
		float labelWidth;
		float labelHeight;
		bool onSide;
		if (!(num9 <= 0f) && !(num10 <= 0f) && !(direction.LengthSquared() < 1f))
		{
			float num11 = MathF.Min((direction.X == 0f) ? float.MaxValue : (num9 / MathF.Abs(direction.X)), (direction.Y == 0f) ? float.MaxValue : (num10 / MathF.Abs(direction.Y)));
			if (!(direction.Y < 0f) || !(MathF.Abs(direction.X) * num10 < MathF.Abs(direction.Y) * num9) || !Intersect.Client.Interface.Interface.HasInGameUI || !Intersect.Client.Interface.Interface.GameUi.IsAnnouncementShowing)
			{
				Vector2 vector4 = Vector2.Normalize(direction);
				float num12 = MathF.Atan2(direction.Y, direction.X);
				int num13 = ((int)MathF.Round(num12 / ((float)Math.PI / 8f)) % 16 + 16) % 16;
				int num14 = (int)MathF.Max(MathF.Abs(vector.X - center.X) / (float)tileWidth, MathF.Abs(vector.Y - center.Y) / (float)tileHeight);
				string text = DatabaseObject<QuestDescriptor>.Get(guid)?.Name ?? string.Empty;
				string text2 = Strings.Parties.Tiles.ToString(num14);
				Vector2 vector5 = renderer.MeasureText(text, _nameFont, 11, pixel);
				Vector2 vector6 = renderer.MeasureText(text2, _smallFont, 9, pixel);
				float num15 = 6f * pixel;
				labelWidth = vector5.X + num15 + vector6.X + 10f * pixel;
				labelHeight = MathF.Max(vector5.Y, vector6.Y) + 4f * pixel;
				onSide = MathF.Abs(direction.X) * num10 >= MathF.Abs(direction.Y) * num9;
				Vector2 insigne = HorsDeLInterface(vector3 + direction * num11, viewport, pixel, Couvert) + vector4 * bob;
				float x = Snap(insigne.X - 16f * pixel, currentView.X, zoom);
				float y = Snap(insigne.Y - 16f * pixel, currentView.Y, zoom);
				Graphics.DrawGameTexture(texture2, new FloatRect(num13 * 32, 0f, 32f, 32f), new FloatRect(x, y, 32f * pixel, 32f * pixel), Color.White);
				Vector2 vector7 = Etiquette(insigne);
				float num16 = Snap(vector7.X, currentView.X, zoom);
				float num17 = Snap(vector7.Y, currentView.Y, zoom);
				Graphics.DrawGameTexture(renderer.WhitePixel, new FloatRect(0f, 0f, 1f, 1f), new FloatRect(num16, num17, labelWidth, labelHeight), BandColor);
				Graphics.DrawGameTexture(renderer.WhitePixel, new FloatRect(0f, 0f, 1f, 1f), new FloatRect(num16, num17, 2f * pixel, labelHeight), Gold);
				float num18 = num16 + 5f * pixel;
				DrawText(renderer, text, _nameFont, 11, num18, Snap(num17 + (labelHeight - vector5.Y) / 2f, currentView.Y, zoom), pixel, Gold);
				DrawText(renderer, text2, _smallFont, 9, Snap(num18 + vector5.X + num15, currentView.X, zoom), Snap(num17 + (labelHeight - vector6.Y) / 2f, currentView.Y, zoom), pixel, DistanceColor);
			}
		}
		FloatRect Couvert(Vector2 insigne2)
		{
			Vector2 vector8 = Etiquette(insigne2);
			float num19 = 16f * pixel;
			float num20 = MathF.Min(insigne2.X - num19, vector8.X);
			float num21 = MathF.Min(insigne2.Y - num19, vector8.Y);
			float num22 = MathF.Max(insigne2.X + num19, vector8.X + labelWidth);
			float num23 = MathF.Max(insigne2.Y + num19, vector8.Y + labelHeight);
			return new FloatRect(num20, num21, num22 - num20, num23 - num21);
		}
		Vector2 Etiquette(Vector2 vector8)
		{
			float value2;
			float value3;
			if (onSide)
			{
				value2 = ((direction.X > 0f) ? (vector8.X - 16f * pixel - labelWidth) : (vector8.X + 16f * pixel));
				value3 = vector8.Y - labelHeight / 2f;
			}
			else
			{
				value2 = vector8.X - labelWidth / 2f;
				value3 = ((direction.Y > 0f) ? (vector8.Y - 16f * pixel - labelHeight) : (vector8.Y + 16f * pixel));
			}
			return new Vector2(Math.Clamp(value2, viewport.X + 2f * pixel, viewport.X + viewport.Width - labelWidth - 2f * pixel), Math.Clamp(value3, viewport.Y + 2f * pixel, viewport.Y + viewport.Height - labelHeight - 2f * pixel));
		}
	}

	private static void DrawText(GameRenderer renderer, string text, IFont font, int size, float x, float y, float pixel, Color color)
	{
		FloatRect currentView = Graphics.CurrentView;
		FloatRect clipRect = new FloatRect(0f, 0f, renderer.ScreenWidth, renderer.ScreenHeight);
		renderer.DrawString(text, font, size, x + pixel - currentView.X, y + pixel - currentView.Y, pixel, ShadowColor, worldPos: true, null, clipRect);
		renderer.DrawString(text, font, size, x - currentView.X, y - currentView.Y, pixel, color, worldPos: true, null, clipRect);
	}

	private static float Snap(float world, float viewOrigin, float zoom)
	{
		return viewOrigin + MathF.Round((world - viewOrigin) * zoom) / zoom;
	}

	private static (int Rows, int Margin) ObjectAbove(MapInstance map, int x, int y, int tileWidth, int tileHeight)
	{
		if (map.Layers == null || !TopTileAt(map, x, y, out var tile))
		{
			return (Rows: 0, Margin: 0);
		}
		if (DatabaseObject<TilesetDescriptor>.TryGet(tile.TilesetId, out var databaseObject))
		{
			IGameTexture texture = Globals.ContentManager.GetTexture(TextureType.Tileset, databaseObject.Name);
			if (texture != null && Meteo.TuileDeTerrain(texture, tile.X, tile.Y))
			{
				return (Rows: 0, Margin: 0);
			}
		}
		int i;
		for (i = 0; i < 6 && y - i - 1 >= 0 && tile.Y - i - 1 >= 0 && HasTileAt(map, x, y - i - 1, tile.TilesetId, tile.X, tile.Y - i - 1); i++)
		{
		}
		if (i >= 6)
		{
			return (Rows: 0, Margin: 0);
		}
		int item = 0;
		if (DatabaseObject<TilesetDescriptor>.TryGet(tile.TilesetId, out var databaseObject2))
		{
			IGameTexture texture2 = Globals.ContentManager.GetTexture(TextureType.Tileset, databaseObject2.Name);
			if (texture2 != null)
			{
				Rectangle frame = new Rectangle(tile.X * tileWidth, (tile.Y - i) * tileHeight, tileWidth, tileHeight);
				item = Math.Clamp(SaiyanHud.TopMargin(texture2, frame), 0, tileHeight - 1);
			}
		}
		return (Rows: i, Margin: item);
	}

	private static bool TopTileAt(MapInstance map, int x, int y, out Tile tile)
	{
		List<string> all = Options.Instance.Map.Layers.All;
		for (int num = all.Count - 1; num >= 1; num--)
		{
			if (map.Layers.TryGetValue(all[num], out var value) && value != null && x >= 0 && y >= 0 && x < value.GetLength(0) && y < value.GetLength(1) && value[x, y].TilesetId != Guid.Empty)
			{
				tile = value[x, y];
				return true;
			}
		}
		tile = default(Tile);
		return false;
	}

	private static bool HasTileAt(MapInstance map, int x, int y, Guid tilesetId, int tileX, int tileY)
	{
		foreach (var (_, array2) in map.Layers)
		{
			if (array2 != null && x < array2.GetLength(0) && y < array2.GetLength(1))
			{
				Tile tile = array2[x, y];
				if (tile.TilesetId == tilesetId && tile.X == tileX && tile.Y == tileY)
				{
					return true;
				}
			}
		}
		return false;
	}
}
