using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Intersect.Client.Core;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.General;
using Intersect.Client.Localization;
using Intersect.Framework.Core;

namespace Intersect.Client.Entities;

public static class ProfessionLevelUp
{
	private sealed record Moment(Guid EntityId, int Profession, int Level, long StartedAt);

	private const long AuraMs = 1300L;

	private const long TextMs = 2600L;

	private static readonly List<Moment> Moments = new List<Moment>();

	private static readonly Color Gold = new Color(255, 245, 190, 90);

	private static readonly Color Pale = new Color(255, 255, 241, 201);

	private static readonly Color Soil = new Color(255, 58, 42, 12);

	private static readonly Color Night = new Color(255, 11, 14, 23);

	private static readonly Color Glow = new Color(255, 255, 122, 26);

	private static readonly Color Entry = new Color(255, 196, 204, 220);

	private static IFont? _titleFont;

	private static IFont? _smallFont;

	public static void Begin(Entity entity, int profession, int level)
	{
		Moments.RemoveAll((Moment moment) => moment.EntityId == entity.Id);
		Moments.Add(new Moment(entity.Id, profession, level, Timing.Global.Milliseconds));
	}

	public static void DrawAura(Entity entity, FloatRect destination)
	{
		if (Moments.Count == 0)
		{
			return;
		}
		IGameTexture gameTexture = Graphics.Renderer?.WhitePixel;
		if (gameTexture == null)
		{
			return;
		}
		Moment moment = Moments.FirstOrDefault((Moment m) => m.EntityId == entity.Id);
		if (moment == null)
		{
			return;
		}
		long num = Timing.Global.Milliseconds - moment.StartedAt;
		if (num < 0 || num > 1300)
		{
			return;
		}
		float num2 = destination.X + destination.Width / 2f;
		float num3 = destination.Y + destination.Height - 1f;
		int num4 = (int)(num / 60);
		float num5 = Math.Min(1f, (float)num / 180f);
		float num6 = ((num > 850) ? (1f - (float)(num - 850) / 450f) : 1f);
		Random random = new Random(num4 * 131 + 7);
		FillEllipse(gameTexture, num2, num3, 15f, 4.5f, Fade(Soil, 0.28f * num6));
		(Color, float, float)[] array = new(Color, float, float)[2]
		{
			(Gold, 0.7f, 1.5f),
			(Pale, 0.95f, 0.95f)
		};
		for (int num7 = 0; num7 < array.Length; num7++)
		{
			(Color, float, float) tuple = array[num7];
			Color item = tuple.Item1;
			float item2 = tuple.Item2;
			float item3 = tuple.Item3;
			Color color = Fade(item, item2 * num6);
			for (int num8 = 0; num8 < 11; num8++)
			{
				float x = (float)Math.PI * ((float)num8 / 10f);
				float num9 = num2 - MathF.Cos(x) * 14f;
				float num10 = num3 + MathF.Sin(x) * 1.5f;
				float num11 = MathF.Sin((float)num / 70f + (float)num8 * 1.7f) * 0.25f + MathF.Sin((float)num / 130f + (float)num8 * 0.9f) * 0.2f;
				float height = (12f + MathF.Sin((float)num8 * 2.3f) * 5f + 16f * (1f - (float)Math.Abs(num8 - 5) / 5f)) * item3 * num5 * (0.8f + (float)random.NextDouble() * 0.4f + num11);
				float tip = num9 + (num9 - num2) * 0.25f + ((float)random.NextDouble() - 0.5f) * 3f;
				float half = 2.6f * item3;
				FillPolygon(gameTexture, Tongue(num9, num10, half, height, tip), color);
			}
		}
	}

	private static List<(float X, float Y)> Tongue(float bx, float by, float half, float height, float tip)
	{
		List<(float, float)> list = new List<(float, float)>(13);
		for (int i = 0; i <= 6; i++)
		{
			list.Add(Quad((X: bx - half, Y: by), (X: bx - half * 0.3f, Y: by - height * 0.55f), (X: tip, Y: by - height), (float)i / 6f));
		}
		for (int j = 1; j <= 6; j++)
		{
			list.Add(Quad((X: tip, Y: by - height), (X: bx + half * 0.3f, Y: by - height * 0.5f), (X: bx + half, Y: by), (float)j / 6f));
		}
		return list;
		static (float, float) Quad((float X, float Y) a, (float X, float Y) c, (float X, float Y) b, float t)
		{
			return ((1f - t) * (1f - t) * a.X + 2f * (1f - t) * t * c.X + t * t * b.X, (1f - t) * (1f - t) * a.Y + 2f * (1f - t) * t * c.Y + t * t * b.Y);
		}
	}

	private static void FillPolygon(IGameTexture pixel, IReadOnlyList<(float X, float Y)> points, Color color)
	{
		float x = points.Min(((float X, float Y) p) => p.Y);
		float x2 = points.Max(((float X, float Y) p) => p.Y);
		List<float> list = new List<float>(4);
		for (int num = (int)MathF.Floor(x); num <= (int)MathF.Ceiling(x2); num++)
		{
			float num2 = (float)num + 0.5f;
			list.Clear();
			for (int num3 = 0; num3 < points.Count; num3++)
			{
				(float, float) tuple = points[num3];
				(float, float) tuple2 = points[(num3 + 1) % points.Count];
				if ((tuple.Item2 <= num2 && tuple2.Item2 > num2) || (tuple2.Item2 <= num2 && tuple.Item2 > num2))
				{
					list.Add(tuple.Item1 + (num2 - tuple.Item2) / (tuple2.Item2 - tuple.Item2) * (tuple2.Item1 - tuple.Item1));
				}
			}
			list.Sort();
			for (int num4 = 0; num4 + 1 < list.Count; num4 += 2)
			{
				float num5 = MathF.Round(list[num4]);
				float num6 = MathF.Round(list[num4 + 1]);
				if (num6 > num5)
				{
					Graphics.DrawGameTexture(pixel, new FloatRect(0f, 0f, 1f, 1f), new FloatRect(num5, num, num6 - num5, 1f), color);
				}
			}
		}
	}

	private static void FillEllipse(IGameTexture pixel, float cx, float cy, float rx, float ry, Color color)
	{
		for (int i = (int)MathF.Floor(cy - ry); i <= (int)MathF.Ceiling(cy + ry); i++)
		{
			float num = ((float)i + 0.5f - cy) / ry;
			if (!(num * num > 1f))
			{
				float num2 = rx * MathF.Sqrt(1f - num * num);
				float num3 = MathF.Round(cx - num2);
				float num4 = MathF.Round(cx + num2);
				if (num4 > num3)
				{
					Graphics.DrawGameTexture(pixel, new FloatRect(0f, 0f, 1f, 1f), new FloatRect(num3, i, num4 - num3, 1f), color);
				}
			}
		}
	}

	private static Color Fade(Color color, float part)
	{
		return new Color((int)((float)(int)color.A * Math.Clamp(part, 0f, 1f)), color.R, color.G, color.B);
	}

	public static void Draw()
	{
		if (Moments.Count == 0)
		{
			return;
		}
		GameRenderer renderer = Graphics.Renderer;
		if (renderer == null)
		{
			return;
		}
		long now = Timing.Global.Milliseconds;
		Moments.RemoveAll((Moment moment) => now - moment.StartedAt > 2600);
		if (_titleFont == null)
		{
			_titleFont = Globals.ContentManager.GetFont("sairatitre") ?? Globals.ContentManager.GetFont("rajdhanibold");
		}
		if (_smallFont == null)
		{
			_smallFont = Globals.ContentManager.GetFont("chakrapetch");
		}
		if (_titleFont == null || _smallFont == null)
		{
			return;
		}
		float num = Math.Max(0.25f, Graphics.WorldScale);
		float num2 = 1f / num;
		FloatRect currentView = Graphics.CurrentView;
		foreach (Moment moment in Moments)
		{
			if (Globals.Entities.TryGetValue(moment.EntityId, out Entity value) && value != null)
			{
				long num3 = now - moment.StartedAt;
				float alpha = ((num3 < 200) ? ((float)num3 / 200f) : ((num3 > 2100) ? Math.Max(0f, (float)(2600 - num3) / 500f) : 1f));
				float num4 = ((num3 < 200) ? ((1f - (float)num3 / 200f) * 10f * num2) : 0f);
				FloatRect bodyWorldPos = value.BodyWorldPos;
				float centerX = bodyWorldPos.X + bodyWorldPos.Width / 2f;
				int num5 = ((value == Globals.Me) ? LootFeedback.StackHeight() : 6);
				float num6 = bodyWorldPos.Y - (float)(num5 + 6) * num2 + num4;
				string text = Strings.Professions.LevelUp.ToString(moment.Level);
				string text2 = Professions.Name(moment.Profession).ToUpperInvariant();
				string text3 = ((!(value is Player player)) ? string.Empty : (Professions.IsCrafting(moment.Profession) ? (Professions.SpeedLabel(moment.Profession) + " " + Professions.Percent(player.PreciseChance(moment.Profession)) + " %") : (Professions.SpeedLabel(moment.Profession) + " " + Strings.Professions.Bonus.ToString(Professions.Percent((double)moment.Level * player.ProfessionFirstBonusPerLevel(moment.Profession))))));
				float bottom = num6;
				if (text3.Length > 0)
				{
					bottom = Line(renderer, text3, _smallFont, Graphics.TailleDeRepere(11), centerX, bottom, num2, num, currentView, Entry, alpha, glow: false);
				}
				bottom = Line(renderer, text2, _smallFont, Graphics.TailleDeRepere(12), centerX, bottom, num2, num, currentView, Professions.Tint(moment.Profession), alpha, glow: false);
				Line(renderer, text, _titleFont, Graphics.TailleDeRepere(22), centerX, bottom - 2f * num2, num2, num, currentView, Pale, alpha, glow: true);
			}
		}
	}

	private static float Line(GameRenderer renderer, string text, IFont font, int size, float centerX, float bottom, float pixel, float zoom, FloatRect view, Color color, float alpha, bool glow)
	{
		Vector2 vector = renderer.MeasureText(text, font, size, pixel);
		float num = Snap(centerX - vector.X / 2f, view.X, zoom);
		float num2 = Snap(bottom - vector.Y, view.Y, zoom);
		if (glow)
		{
			(int, int)[] array = new(int, int)[8]
			{
				(-2, 0),
				(2, 0),
				(0, -2),
				(0, 2),
				(-1, -1),
				(1, -1),
				(-1, 1),
				(1, 1)
			};
			for (int i = 0; i < array.Length; i++)
			{
				var (num3, num4) = array[i];
				DrawText(renderer, text, font, size, num + (float)num3 * pixel, num2 + (float)num4 * pixel, pixel, Fade(Glow, 0.35f), alpha);
			}
		}
		(int, int)[] array2 = new(int, int)[4]
		{
			(-1, 0),
			(1, 0),
			(0, -1),
			(0, 1)
		};
		for (int j = 0; j < array2.Length; j++)
		{
			var (num5, num6) = array2[j];
			DrawText(renderer, text, font, size, num + (float)num5 * pixel, num2 + (float)num6 * pixel, pixel, Night, alpha);
		}
		DrawText(renderer, text, font, size, num, num2, pixel, color, alpha);
		return num2 - 2f * pixel;
	}

	private static void DrawText(GameRenderer renderer, string text, IFont font, int size, float x, float y, float pixel, Color color, float alpha)
	{
		FloatRect currentView = Graphics.CurrentView;
		renderer.DrawString(text, font, size, x - currentView.X, y - currentView.Y, pixel, new Color((int)((float)(int)color.A * alpha), color.R, color.G, color.B), worldPos: true, null, new FloatRect(0f, 0f, renderer.ScreenWidth, renderer.ScreenHeight));
	}

	private static float Snap(float world, float viewOrigin, float zoom)
	{
		return viewOrigin + MathF.Round((world - viewOrigin) * zoom) / zoom;
	}
}
