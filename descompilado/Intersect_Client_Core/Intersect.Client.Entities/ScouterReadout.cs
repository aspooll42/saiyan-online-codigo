using System;
using System.Globalization;
using System.Numerics;
using Intersect.Client.Core;
using Intersect.Client.Framework.Content;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.General;
using Intersect.Client.Localization;
using Intersect.Enums;
using Intersect.Framework.Core;
using Intersect.Framework.Core.Entities;
using Intersect.Localization;

namespace Intersect.Client.Entities;

public static class ScouterReadout
{
	private const long ScanMs = 260L;

	private const long CountMs = 420L;

	private const int LabelSize = 8;

	private const int ValueSize = 18;

	private static readonly Color Green = new Color(255, 60, 255, 122);

	private static readonly Color Yellow = new Color(255, 255, 216, 74);

	private static readonly Color Red = new Color(255, 255, 74, 74);

	private static readonly Color RedDim = new Color(255, 150, 40, 40);

	private static readonly Color LabelColor = new Color(255, 120, 205, 150);

	private static readonly Random Noise = new Random();

	private static Guid _targetId;

	private static long _scanStartedAt;

	private static IFont? _labelFont;

	private static IFont? _valueFont;

	public static void Draw()
	{
		Entity entity = FindHovered();
		if (entity == null)
		{
			_targetId = Guid.Empty;
			return;
		}
		long milliseconds = Timing.Global.Milliseconds;
		if (entity.Id != _targetId)
		{
			_targetId = entity.Id;
			_scanStartedAt = milliseconds;
		}
		if (_labelFont == null)
		{
			_labelFont = Globals.ContentManager.GetFont("rajdhanibold");
		}
		if (_valueFont == null)
		{
			_valueFont = Globals.ContentManager.GetFont("rajdhanibold");
		}
		long num = Of(entity);
		long num2 = milliseconds - _scanStartedAt;
		bool flag = num2 < 260;
		string digits;
		if (flag)
		{
			digits = Scramble(num);
		}
		else if (num2 < 680)
		{
			float num3 = (float)(num2 - 260) / 420f;
			float num4 = 1f - MathF.Pow(1f - num3, 3f);
			digits = Format((long)((float)num * num4));
		}
		else
		{
			digits = Format(num);
		}
		Player me = Globals.Me;
		bool flag2 = me == null || entity.Id == me.Id;
		double num5 = (flag2 ? 1.0 : ((double)num / (double)Math.Max(1L, Of(me))));
		bool flag3 = !flag2 && num5 >= 3.0;
		Color color = ((flag2 || num5 < 0.75) ? Green : ((num5 < 1.5) ? Yellow : Red));
		if (flag3 && milliseconds / 250 % 2 == 0L)
		{
			color = RedDim;
		}
		LocalizedString localizedString = (flag ? Strings.Scouter.Scanning : (flag3 ? Strings.Scouter.Danger : Strings.Scouter.Label));
		FloatRect bodyWorldPos = entity.BodyWorldPos;
		FloatRect box = new FloatRect(bodyWorldPos.X + bodyWorldPos.Width * 0.2f, bodyWorldPos.Y + bodyWorldPos.Height * 0.1f, bodyWorldPos.Width * 0.6f, bodyWorldPos.Height * 0.9f);
		float num6 = Math.Clamp((float)num2 / 680f, 0f, 1f);
		DrawBrackets(box, flag2 ? Green : color, 10f * (1f - num6));
		DrawPanel(box, localizedString, digits, color);
	}

	private static Entity? FindHovered()
	{
		foreach (Entity value in Globals.Entities.Values)
		{
			if (value != null && value.IsHovered && (value is Player || value.Type == EntityType.GlobalEntity))
			{
				return value;
			}
		}
		return null;
	}

	internal static long Of(Entity entity)
	{
		long[] maxVital = entity.MaxVital;
		long maxHealth = ((maxVital != null && maxVital.Length > 0) ? maxVital[0] : 0);
		long maxKi = ((maxVital != null && maxVital.Length > 1) ? maxVital[1] : 0);
		return PowerLevel.Of(entity.Level, entity.Stat, maxHealth, maxKi);
	}

	private static void DrawBrackets(FloatRect box, Color color, float spread)
	{
		IGameTexture texture = Globals.ContentManager.GetTexture(TextureType.Misc, "scouter_corners.png");
		if (texture != null)
		{
			float num = (float)texture.Width / 2f;
			float x = box.X - spread;
			float y = box.Y - spread;
			float x2 = box.X + box.Width + spread - num;
			float y2 = box.Y + box.Height + spread - num;
			Graphics.DrawGameTexture(texture, new FloatRect(0f, 0f, num, num), new FloatRect(x, y, num, num), color);
			Graphics.DrawGameTexture(texture, new FloatRect(num, 0f, num, num), new FloatRect(x2, y, num, num), color);
			Graphics.DrawGameTexture(texture, new FloatRect(0f, num, num, num), new FloatRect(x, y2, num, num), color);
			Graphics.DrawGameTexture(texture, new FloatRect(num, num, num, num), new FloatRect(x2, y2, num, num), color);
		}
	}

	private static void DrawPanel(FloatRect box, string label, string digits, Color color)
	{
		IGameTexture texture = Globals.ContentManager.GetTexture(TextureType.Misc, "scouter_panel.png");
		float val = ((float?)texture?.Width) ?? 150f;
		float height = ((float?)texture?.Height) ?? 42f;
		int num = 18;
		Vector2 vector = Measure(digits, num);
		while (num > 10 && (12f + vector.X + 32f > 280f || vector.Y > 30f))
		{
			num -= 2;
			vector = Measure(digits, num);
		}
		float num2 = Math.Max(val, MathF.Ceiling(12f + vector.X + 32f));
		float num3 = box.X + box.Width + 12f;
		FloatRect worldViewport = Graphics.WorldViewport;
		if (num3 + num2 > worldViewport.X + worldViewport.Width)
		{
			num3 = box.X - num2 - 12f;
		}
		float num4 = Math.Max(worldViewport.Y + 4f, box.Y - 6f);
		if (texture != null)
		{
			DrawStretchedPanel(texture, num3, num4, num2, height);
		}
		TexteMonde.Ecrire(label, _labelFont, TexteMonde.Taille(8), num3 + 12f, num4 + 5f, LabelColor);
		float y = num4 + 14f + (24f - vector.Y) / 2f;
		TexteMonde.Ecrire(digits, _valueFont, TexteMonde.Taille(num), num3 + 12f, y, color);
	}

	private static Vector2 Measure(string text, int size)
	{
		Vector2 vector = TexteMonde.Mesurer(text, _valueFont, TexteMonde.Taille(size));
		return new Vector2(vector.X, vector.Y);
	}

	private static void DrawStretchedPanel(IGameTexture panel, float x, float y, float width, float height)
	{
		if (width <= (float)panel.Width + 0.5f)
		{
			Graphics.DrawGameTexture(panel, new FloatRect(0f, 0f, panel.Width, panel.Height), new FloatRect(x, y, width, height), Color.White);
			return;
		}
		float w = (float)panel.Width - 72f;
		Graphics.DrawGameTexture(panel, new FloatRect(0f, 0f, 36f, panel.Height), new FloatRect(x, y, 36f, height), Color.White);
		Graphics.DrawGameTexture(panel, new FloatRect(36f, 0f, w, panel.Height), new FloatRect(x + 36f, y, width - 72f, height), Color.White);
		Graphics.DrawGameTexture(panel, new FloatRect((float)panel.Width - 36f, 0f, 36f, panel.Height), new FloatRect(x + width - 36f, y, 36f, height), Color.White);
	}

	private static string Scramble(long value)
	{
		int length = Format(value).Length;
		char[] array = new char[length];
		for (int i = 0; i < length; i++)
		{
			array[i] = (char)(48 + Noise.Next(10));
		}
		return new string(array);
	}

	private static string Format(long value)
	{
		return value.ToString("#,0", CultureInfo.InvariantCulture).Replace(',', ' ');
	}
}
