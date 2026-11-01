using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Intersect.Client.Dungeons;
using Intersect.Client.Entities;
using Intersect.Client.Entities.Events;
using Intersect.Client.Framework.Content;
using Intersect.Client.Framework.Entities;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.Framework.Gwen.Renderer;
using Intersect.Client.Framework.Gwen.Skin;
using Intersect.Client.Framework.Input;
using Intersect.Client.General;
using Intersect.Client.Interface.Game.EntityPanel;
using Intersect.Client.Localization;
using Intersect.Client.Maps;
using Intersect.Enums;
using Intersect.Framework.Core;
using Intersect.Framework.Core.GameObjects.Dungeons;
using Intersect.Framework.Core.GameObjects.Events;
using Intersect.Framework.Core.GameObjects.Maps;
using Intersect.Models;

namespace Intersect.Client.Interface.Game;

public sealed class MiniMapPanel : Intersect.Client.Framework.Gwen.Control.Base
{
	public const int PanelWidth = 232;

	public const int PanelHeight = 320;

	private const int ClockTop = 270;

	private const int CounterTop = 304;

	private const int CounterHeight = 16;

	public const int ScreenMargin = 4;

	private const int CenterX = 116;

	private const int CenterY = 126;

	private const int Radius = 94;

	private const int ButtonDistance = 106;

	private const int ButtonSize = 22;

	private const int RibbonLeft = 23;

	private const int RibbonRight = 209;

	private const int RibbonTop = 240;

	private const int RibbonHeight = 24;

	private const long SweepMs = 4000L;

	private const long PulseMs = 2400L;

	private static readonly int[] TileSizes = new int[3] { 6, 8, 12 };

	private static int _zoom = 1;

	private static readonly Color White = new Color(255, 242, 242, 242);

	private static readonly Color Muted = new Color(255, 150, 160, 180);

	private static readonly Color NameColor = new Color(255, 246, 231, 207);

	private static readonly Color Night = new Color(255, 11, 14, 23);

	private static readonly Color Orange = new Color(255, 255, 122, 26);

	private static readonly Color Pulse = new Color(255, 255, 150, 60);

	private static readonly Color Gold = new Color(255, 255, 211, 106);

	private static readonly Color Blue = new Color(255, 120, 176, 255);

	private static readonly Color Safe = new Color(255, 111, 220, 152);

	private static readonly Color Pvp = new Color(255, 255, 110, 96);

	private static readonly Color WeatherColor = new Color(255, 201, 208, 222);

	private static IFont? _weatherFont;

	private readonly ImagePanel _zoomOut;

	private readonly ImagePanel _zoomIn;

	private readonly ImagePanel _clock;

	private long _clockTipAt;

	public static int JoueursEnLigne { get; set; }

	private static IFont? WeatherFont => _weatherFont ?? (_weatherFont = Globals.ContentManager?.GetFont("poppinsmedium"));

	protected override bool ShouldClip => false;

	public MiniMapPanel(Canvas canvas)
		: base(canvas, "MiniMapPanel")
	{
		base.MouseInputEnabled = false;
		SetSize(232, 320);
		_zoomOut = ZoomButton("ZoomOut", 0.75f, delegate
		{
			_zoom = Math.Max(0, _zoom - 1);
		});
		_zoomIn = ZoomButton("ZoomIn", 0.25f, delegate
		{
			_zoom = Math.Min(TileSizes.Length - 1, _zoom + 1);
		});
		_clock = new ImagePanel(this, "Clock")
		{
			TooltipFont = WeatherFont,
			TooltipFontSize = 9
		};
		_clock.SetBounds(23, 272, 186, 28);
		_clock.Clicked += delegate
		{
			Interface.GameUi?.ToggleCalendarWindow();
		};
	}

	private ImagePanel ZoomButton(string name, float turns, Action click)
	{
		float x = (float)Math.PI * turns;
		ImagePanel imagePanel = new ImagePanel(this, name);
		imagePanel.SetBounds((int)MathF.Round(116f + MathF.Cos(x) * 106f - 11f), (int)MathF.Round(126f + MathF.Sin(x) * 106f - 11f), 22, 22);
		imagePanel.Clicked += delegate
		{
			click();
		};
		return imagePanel;
	}

	public void Update()
	{
		Intersect.Client.Framework.Gwen.Control.Base parent = base.Parent;
		if (parent != null)
		{
			Point point = Disposition.Poser(Disposition.Piece.MiniCarte, 232, 320, parent.Width - 232 - 4, 4, parent);
			SetPosition(point.X, point.Y);
		}
		Player me = Globals.Me;
		base.IsHidden = me == null || !MapInstance.TryGet(me.MapId, out MapInstance _) || Disposition.Masquee(Disposition.Piece.MiniCarte);
		long milliseconds = Timing.Global.Milliseconds;
		if (!base.IsHidden && milliseconds >= _clockTipAt)
		{
			_clockTipAt = milliseconds + 1000;
			_clock.SetToolTipText(ClockTooltip());
		}
	}

	private static string ClockTooltip()
	{
		Calendrier.Date maintenant = Calendrier.Maintenant;
		List<string> list = new List<string>();
		list.Add(Strings.Calendar.ClockDate.ToString(Calendrier.NomDuJour(maintenant.JourDeSemaine), Calendrier.JourEtMois(maintenant.Jour, maintenant.Mois), maintenant.Annee));
		List<string> list2 = list;
		string nomTempsAffiche = Meteo.NomTempsAffiche;
		string nomSuivantAffiche = Meteo.NomSuivantAffiche;
		if (nomTempsAffiche.Length > 0)
		{
			list2.Add((nomSuivantAffiche.Length > 0) ? Strings.Calendar.NextWeather.ToString(nomTempsAffiche, nomSuivantAffiche, Calendrier.DureeCourte(Meteo.SecondesAvantSuivant)) : nomTempsAffiche);
		}
		string text = DropdownPanel.KeyOf(Control.OpenCalendar);
		list2.Add((text == null) ? Strings.Calendar.ClockClick.ToString(string.Empty).Replace(" ()", string.Empty) : Strings.Calendar.ClockClick.ToString(text));
		return string.Join(". ", list2) + ".";
	}

	protected override void Render(Intersect.Client.Framework.Gwen.Skin.Base skin)
	{
		base.Render(skin);
		Intersect.Client.Framework.Gwen.Renderer.Base renderer = skin.Renderer;
		Player me = Globals.Me;
		if (me == null || !MapInstance.TryGet(me.MapId, out MapInstance instance))
		{
			return;
		}
		Guid[,] mapGrid = Globals.MapGrid;
		int tile;
		int tileWidth;
		int tileHeight;
		float offsetX;
		float offsetY;
		if (mapGrid != null && Globals.GridMaps.TryGetValue(me.MapId, out var value))
		{
			long millisecondsUtc = Timing.Global.MillisecondsUtc;
			tile = TileSizes[_zoom];
			tileWidth = Options.Instance.Map.TileWidth;
			tileHeight = Options.Instance.Map.TileHeight;
			Vector2 center = me.Center;
			offsetX = MathF.Round(116f - center.X / (float)tileWidth * (float)tile);
			offsetY = MathF.Round(126f - center.Y / (float)tileHeight * (float)tile);
			DrawTerrain(renderer, instance, mapGrid, value, tile, ToScreen);
			Draw(renderer, "minicarte_voile.png", 22, 32, White);
			IGameTexture gameTexture = SaiyanHud.Texture("minicarte_balayage.png");
			if (gameTexture != null && renderer is IntersectRenderer intersectRenderer)
			{
				intersectRenderer.DrawColor = White;
				intersectRenderer.DrawTextureRotated(gameTexture, 22f, 32f, 188f, 188f, (float)(millisecondsUtc % 4000) * 360f / 4000f);
			}
			DrawCharacters(renderer, me, mapGrid, value, ToScreen);
			DessinerLesDonjons(renderer, mapGrid, ToScreen, tileWidth, tileHeight);
			DrawObjective(renderer, ToScreen, center, tileWidth, tileHeight);
			DrawPlayer(renderer, me, millisecondsUtc);
			Draw(renderer, "minicarte_radar.png", 0, 0, White);
			DrawRibbon(renderer, instance);
			DrawClock(renderer);
			DrawPlayersOnline(renderer);
			DrawButton(renderer, _zoomOut, "–", _zoom > 0);
			DrawButton(renderer, _zoomIn, "+", _zoom < TileSizes.Length - 1);
		}
		Vector2 ToScreen(float worldX, float worldY)
		{
			return new Vector2(offsetX + worldX / (float)tileWidth * (float)tile, offsetY + worldY / (float)tileHeight * (float)tile);
		}
	}

	private static void DrawTerrain(Intersect.Client.Framework.Gwen.Renderer.Base renderer, MapInstance here, Guid[,] grid, Point hereGrid, int tile, Func<float, float, Vector2> toScreen)
	{
		List<(Rectangle, IGameTexture, bool)> list = new List<(Rectangle, IGameTexture, bool)>();
		for (int i = hereGrid.Y - 1; i <= hereGrid.Y + 1; i++)
		{
			for (int j = hereGrid.X - 1; j <= hereGrid.X + 1; j++)
			{
				if (MapAt(grid, j, i, out MapInstance map))
				{
					Vector2 vector = toScreen(map.X, map.Y);
					Rectangle rectangle = new Rectangle((int)vector.X, (int)vector.Y, Options.Instance.Map.MapWidth * tile, Options.Instance.Map.MapHeight * tile);
					if (Rectangle.Intersect(rectangle, new Rectangle(22, 32, 188, 188)).Width > 0)
					{
						list.Add((rectangle, LocalMap.MapTexture(map, tile), map == here));
					}
				}
			}
		}
		IntersectRenderer intersectRenderer = renderer as IntersectRenderer;
		for (int k = 32; k < 220; k += 2)
		{
			int num = k + 1 - 126;
			float num2 = MathF.Sqrt(8836 - num * num);
			int num3 = (int)MathF.Floor(116f - num2);
			Rectangle rectangle2 = new Rectangle(num3, k, (int)MathF.Ceiling(116f + num2) - num3, 2);
			Fill(renderer, rectangle2, Night);
			if (intersectRenderer == null)
			{
				continue;
			}
			foreach (var item3 in list)
			{
				Rectangle item = item3.Item1;
				IGameTexture item2 = item3.Item2;
				Rectangle rectangle3 = Rectangle.Intersect(rectangle2, item);
				if (item2 != null && rectangle3.Width > 0 && rectangle3.Height > 0)
				{
					intersectRenderer.DrawColor = White;
					intersectRenderer.DrawTextureBand(item2, rectangle3.X - item.X, rectangle3.Y - item.Y, rectangle3.Width, rectangle3.Height, rectangle3.X, rectangle3.Y, rectangle3.Width, rectangle3.Height);
				}
			}
		}
		foreach (var item4 in list)
		{
			var (rect, _, _) = item4;
			if (item4.Item3)
			{
				DashedRect(renderer, rect, SaiyanHud.WithAlpha(White, 0.35f));
			}
		}
	}

	private static bool MapAt(Guid[,] grid, int gx, int gy, out MapInstance map)
	{
		map = null;
		if (gx >= 0 && gy >= 0 && gx < grid.GetLength(0) && gy < grid.GetLength(1) && grid[gx, gy] != Guid.Empty && MapInstance.TryGet(grid[gx, gy], out map))
		{
			return map.IsLoaded;
		}
		return false;
	}

	private static void DrawCharacters(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Player me, Guid[,] grid, Point hereGrid, Func<float, float, Vector2> toScreen)
	{
		IGameTexture gameTexture = SaiyanHud.Texture("hud_quete_marques.png");
		int tileWidth = Options.Instance.Map.TileWidth;
		int tileHeight = Options.Instance.Map.TileHeight;
		List<(Vector2, int)> list = new List<(Vector2, int)>();
		for (int i = hereGrid.Y - 1; i <= hereGrid.Y + 1; i++)
		{
			for (int j = hereGrid.X - 1; j <= hereGrid.X + 1; j++)
			{
				if (!MapAt(grid, j, i, out MapInstance map))
				{
					continue;
				}
				foreach (Entity value2 in map.LocalEntities.Values)
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
					Vector2 vector = toScreen((float)(map.X + obj.X * tileWidth) + (float)tileWidth / 2f, (float)(map.Y + obj.Y * tileHeight) + (float)tileHeight / 2f);
					if (OnScreen(vector, 5f))
					{
						Dot(renderer, vector, 3.5f, Night);
						Dot(renderer, vector, 2.2f, White);
						if (obj.QuestMarker != 0)
						{
							list.Add((vector, (obj.QuestMarker == 2) ? 1 : 0));
						}
					}
				}
			}
		}
		if (gameTexture != null)
		{
			foreach (var (vector2, num) in list)
			{
				SaiyanHud.Draw(renderer, gameTexture, (int)MathF.Round(vector2.X - 6f), (int)MathF.Round(vector2.Y - 19f), 12, 17, White, (float)num * 0.5f, 0f, (float)(num + 1) * 0.5f);
			}
		}
		foreach (IPartyMember item in me.Party)
		{
			if (!(item.Id == me.Id) && Globals.Entities.TryGetValue(item.Id, out Entity value) && value != null)
			{
				Vector2 vector3 = toScreen(value.Center.X, value.Center.Y);
				if (OnScreen(vector3, 5f))
				{
					Dot(renderer, vector3, 4.5f, Night);
					Dot(renderer, vector3, 3.2f, Blue);
				}
			}
		}
	}

	private static void DessinerLesDonjons(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Guid[,] grid, Func<float, float, Vector2> toScreen, int tileWidth, int tileHeight)
	{
		IGameTexture icone = Dungeon.Icone;
		if (icone == null)
		{
			return;
		}
		foreach (DungeonDescriptor item in DatabaseObject<DungeonDescriptor>.Lookup.Values.OfType<DungeonDescriptor>())
		{
			if (item.Rooms.Count != 0 && MapInstance.TryGet(item.EntranceMapId, out MapInstance instance) && grid.Cast<Guid>().Contains(instance.Id))
			{
				Vector2 vector = toScreen((float)instance.X + ((float)item.EntranceX + 0.5f) * (float)tileWidth, (float)instance.Y + ((float)item.EntranceY + 0.5f) * (float)tileHeight);
				if (OnScreen(vector, 9f))
				{
					Dot(renderer, vector, 9f, Night);
					SaiyanHud.Draw(renderer, icone, (int)MathF.Round(vector.X - 7f), (int)MathF.Round(vector.Y - 7f), 14, 14, White);
				}
			}
		}
	}

	private static void DrawObjective(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Func<float, float, Vector2> toScreen, Vector2 origin, int tileWidth, int tileHeight)
	{
		Guid guid = QuestMarkers.GuidedQuest();
		IGameTexture gameTexture = SaiyanHud.Texture("hud_quete_repere.png");
		if (guid == Guid.Empty || gameTexture == null)
		{
			return;
		}
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
			Vector2 vector3 = new Vector2(116f, 126f);
			Vector2 vector4 = toScreen(valueOrDefault.X, valueOrDefault.Y);
			if (!OnScreen(vector4, 14f))
			{
				Vector2 value2 = vector4 - vector3;
				vector4 = vector3 + ((value2.LengthSquared() > 0f) ? Vector2.Normalize(value2) : Vector2.UnitY) * 78f;
			}
			SaiyanHud.Draw(renderer, gameTexture, (int)MathF.Round(vector4.X - 7f), (int)MathF.Round(vector4.Y - 17f + 3f), 14, 17, White);
		}
	}

	private static void DrawPlayer(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Player me, long now)
	{
		float num = (float)(now % 2400) / 2400f;
		if (num < 0.6f)
		{
			IGameTexture gameTexture = SaiyanHud.Texture("minicarte_onde.png");
			if (gameTexture != null)
			{
				int num2 = (int)MathF.Round(12f + num * 44f);
				SaiyanHud.Draw(renderer, gameTexture, 116 - num2 / 2, 126 - num2 / 2, num2, num2, SaiyanHud.WithAlpha(Pulse, 0.55f * (1f - num / 0.6f)));
			}
		}
		IGameTexture texture = Globals.ContentManager.GetTexture(TextureType.Misc, "ki_halo.png");
		if (texture != null)
		{
			SaiyanHud.Draw(renderer, texture, 104, 114, 24, 24, SaiyanHud.WithAlpha(Orange, 0.45f));
		}
		IGameTexture gameTexture2 = SaiyanHud.Texture("carte_environs_joueur.png");
		if (gameTexture2 == null || !(renderer is IntersectRenderer intersectRenderer))
		{
			Dot(renderer, new Vector2(116f, 126f), 5f, Orange);
			return;
		}
		float degrees = me.DirectionFacing switch
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
		intersectRenderer.DrawColor = White;
		intersectRenderer.DrawTextureRotated(gameTexture2, 108f, 118f, 16f, 16f, degrees);
	}

	private static void DrawRibbon(Intersect.Client.Framework.Gwen.Renderer.Base renderer, MapInstance here)
	{
		int spacing = 0;
		string text = SaiyanHud.Fit(renderer, SaiyanHud.StrongFont, 13, here.Name ?? string.Empty, 128, ref spacing);
		SaiyanHud.TextCenter(renderer, SaiyanHud.StrongFont, 13, text, 39, 240, 136, 24, NameColor);
		bool flag = here.ZoneType == MapZone.Safe;
		Vector2 center = new Vector2(185f, 252f);
		Dot(renderer, center, 5.5f, SaiyanHud.WithAlpha(flag ? Safe : Pvp, 0.25f));
		Dot(renderer, center, 3.2f, flag ? Safe : Pvp);
	}

	private static void DrawClock(Intersect.Client.Framework.Gwen.Renderer.Base renderer)
	{
		IGameTexture gameTexture = SaiyanHud.Texture("minicarte_radar.png");
		if (gameTexture == null)
		{
			return;
		}
		int num = 34;
		SaiyanHud.Draw(renderer, gameTexture, 18, 236 + num, 196, gameTexture.Height - 236, White, 18f / (float)gameTexture.Width, 236f / (float)gameTexture.Height, 214f / (float)gameTexture.Width);
		int num2 = 240 + num;
		int num3 = num2 + 12;
		SaiyanHud.Draw(renderer, SaiyanHud.Texture(Meteo.Nuit ? "meteo_lune.png" : "meteo_soleil.png"), 36, num3 - 8, 16, 16, White);
		string text = Calendrier.TexteHeure(Meteo.HeureAffichee);
		Point point = SaiyanHud.Measure(renderer, SaiyanHud.StrongFont, 14, text);
		SaiyanHud.Text(renderer, SaiyanHud.StrongFont, 14, text, 58, num3 - point.Y / 2, White);
		string nomTempsAffiche = Meteo.NomTempsAffiche;
		if (nomTempsAffiche.Length != 0)
		{
			int num4 = Math.Max(104, 58 + point.X + 6);
			SaiyanHud.Draw(renderer, SaiyanHud.Texture("meteo_" + Meteo.IconeTemps + ".png"), num4, num3 - 8, 16, 16, White);
			IFont font = WeatherFont ?? SaiyanHud.StrongFont;
			int num5 = 203 - (num4 + 19);
			int num6 = 9;
			while (num6 > 8 && SaiyanHud.SpacedWidth(renderer, font, num6, nomTempsAffiche, 0) > num5)
			{
				num6--;
			}
			int spacing = 0;
			string text2 = SaiyanHud.Fit(renderer, font, num6, nomTempsAffiche, num5, ref spacing);
			SaiyanHud.Text(renderer, font, num6, text2, num4 + 19, num3 - SaiyanHud.Measure(renderer, font, num6, text2).Y / 2, WeatherColor);
		}
	}

	private static void DrawPlayersOnline(Intersect.Client.Framework.Gwen.Renderer.Base renderer)
	{
		if (JoueursEnLigne > 0)
		{
			IFont font = WeatherFont ?? SaiyanHud.StrongFont;
			string text = SaiyanHud.Amount(JoueursEnLigne);
			string text2 = ((JoueursEnLigne == 1) ? Strings.Calendar.PlayerOnline : Strings.Calendar.PlayersOnline).ToString();
			Point point = SaiyanHud.Measure(renderer, SaiyanHud.StrongFont, 11, text);
			Point point2 = SaiyanHud.Measure(renderer, font, 8, text2);
			int num = 10 + point.X + 4 + point2.X;
			int num2 = (int)MathF.Round(116f - (float)num / 2f);
			Fill(renderer, new Rectangle(num2 - 7, 304, (int)MathF.Ceiling(num) + 14, 16), SaiyanHud.WithAlpha(Night, 0.62f));
			Dot(renderer, new Vector2(num2 + 3, 312f), 5f, SaiyanHud.WithAlpha(Safe, 0.25f));
			Dot(renderer, new Vector2(num2 + 3, 312f), 2.8f, Safe);
			SaiyanHud.Text(renderer, SaiyanHud.StrongFont, 11, text, num2 + 10, 312 - point.Y / 2, White);
			SaiyanHud.Text(renderer, font, 8, text2, num2 + 10 + point.X + 4, 312 - point2.Y / 2, WeatherColor);
		}
	}

	private static void DrawButton(Intersect.Client.Framework.Gwen.Renderer.Base renderer, ImagePanel hit, string glyph, bool enabled)
	{
		Rectangle bounds = hit.Bounds;
		bool flag = enabled && hit.IsHovered;
		SaiyanHud.Draw(renderer, SaiyanHud.Texture("minicarte_bouton.png"), bounds.X, bounds.Y, 22, 22, White, flag ? 0.5f : 0f, 0f, flag ? 1f : 0.5f);
		SaiyanHud.TextCenter(renderer, SaiyanHud.StrongFont, 13, glyph, bounds.X, bounds.Y - 1, bounds.Width, bounds.Height, enabled ? White : Muted);
	}

	private static bool OnScreen(Vector2 point, float inset)
	{
		return Vector2.DistanceSquared(point, new Vector2(116f, 126f)) <= (94f - inset) * (94f - inset);
	}

	private static void Draw(Intersect.Client.Framework.Gwen.Renderer.Base renderer, string texture, int x, int y, Color color)
	{
		SaiyanHud.Draw(renderer, SaiyanHud.Texture(texture), x, y, color);
	}

	private static void Fill(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Rectangle rect, Color color)
	{
		if (rect.Width > 0 && rect.Height > 0 && color.A > 0)
		{
			renderer.DrawColor = color;
			renderer.DrawFilledRect(rect);
		}
	}

	private static void Dot(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Vector2 center, float radius, Color color)
	{
		int num = (int)MathF.Round(radius * 2f);
		Rectangle rect = new Rectangle((int)MathF.Round(center.X - (float)num / 2f), (int)MathF.Round(center.Y - (float)num / 2f), num, num);
		IGameTexture gameTexture = SaiyanHud.Texture("carte_environs_pastille.png");
		if (gameTexture != null)
		{
			SaiyanHud.Draw(renderer, gameTexture, rect.X, rect.Y, rect.Width, rect.Height, color);
		}
		else
		{
			Fill(renderer, rect, color);
		}
	}

	private static void DashedRect(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Rectangle rect, Color color)
	{
		for (int i = rect.X; i < rect.Right; i += 7)
		{
			Segment(i, rect.Y, Math.Min(4, rect.Right - i), 1);
			Segment(i, rect.Bottom - 1, Math.Min(4, rect.Right - i), 1);
		}
		for (int j = rect.Y; j < rect.Bottom; j += 7)
		{
			Segment(rect.X, j, 1, Math.Min(4, rect.Bottom - j));
			Segment(rect.Right - 1, j, 1, Math.Min(4, rect.Bottom - j));
		}
		void Segment(int x, int y, int width, int height)
		{
			if (OnScreen(new Vector2((float)x + (float)width / 2f, (float)y + (float)height / 2f), 3f))
			{
				Fill(renderer, new Rectangle(x, y, width, height), color);
			}
		}
	}
}
