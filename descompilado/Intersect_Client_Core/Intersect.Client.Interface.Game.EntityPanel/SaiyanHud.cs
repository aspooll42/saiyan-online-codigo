using System;
using System.Collections.Generic;
using System.Globalization;
using Intersect.Client.Entities;
using Intersect.Client.Framework.Content;
using Intersect.Client.Framework.Entities;
using Intersect.Client.Framework.File_Management;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.Framework.Gwen.Renderer;
using Intersect.Client.General;
using Intersect.Client.Maps;

namespace Intersect.Client.Interface.Game.EntityPanel;

internal static class SaiyanHud
{
	public readonly record struct Accent(Color Main, Color Light);

	public static readonly Color White = new Color(255, 255, 255, 255);

	public static readonly Color Night = new Color(255, 11, 14, 23);

	public static readonly Color Muted = new Color(255, 176, 184, 200);

	public static readonly Color Gold = new Color(255, 245, 190, 90);

	public static readonly Color GoldLight = new Color(255, 255, 226, 150);

	public static readonly Color Red = new Color(255, 232, 64, 64);

	public static readonly Color KiLight = new Color(255, 120, 176, 255);

	public static readonly Color Power = new Color(255, 150, 225, 255);

	public static readonly Color Shadow = new Color(170, 0, 0, 0);

	public static readonly Accent BaseAccent = new Accent(new Color(255, 255, 122, 26), new Color(255, 255, 172, 82));

	private static readonly Accent SuperSaiyan = new Accent(new Color(255, 245, 190, 90), new Color(255, 255, 226, 150));

	private static readonly Accent Blue = new Accent(new Color(255, 90, 210, 255), new Color(255, 170, 236, 255));

	private static readonly Accent God = new Accent(new Color(255, 255, 96, 140), new Color(255, 255, 170, 196));

	private static readonly Accent Kaioken = new Accent(new Color(255, 232, 64, 64), new Color(255, 255, 120, 110));

	private static readonly Accent Ultra = new Accent(new Color(255, 200, 212, 238), new Color(255, 255, 255, 255));

	private static readonly Dictionary<string, IGameTexture?> Textures = new Dictionary<string, IGameTexture>();

	private static readonly Dictionary<string, int> TopMargins = new Dictionary<string, int>();

	private static IFont? _strongFont;

	private static IFont? _smallFont;

	private static IFont? _nameFont;

	public static IFont? StrongFont => _strongFont ?? (_strongFont = GameContentManager.Current.GetFont("rajdhanibold"));

	public static IFont? NameFont
	{
		get
		{
			object obj = _nameFont;
			if (obj == null)
			{
				obj = GameContentManager.Current.GetFont("sairanom") ?? StrongFont;
				_nameFont = (IFont?)obj;
			}
			return (IFont?)obj;
		}
	}

	public static IFont? SmallFont => _smallFont ?? (_smallFont = GameContentManager.Current.GetFont("chakrapetch"));

	public static IGameTexture? Texture(string name)
	{
		if (!Textures.TryGetValue(name, out IGameTexture value) || value == null)
		{
			value = Globals.ContentManager.GetTexture(TextureType.Gui, name);
			Textures[name] = value;
		}
		return value;
	}

	public static Accent AccentOf(IEntity? entity)
	{
		string text = entity?.TransformedSprite;
		if (string.IsNullOrEmpty(text))
		{
			return BaseAccent;
		}
		if (Transformation.IsUltraForm(text))
		{
			return Ultra;
		}
		string text2 = text.ToLowerInvariant();
		if (text2.Contains("blue") || text2.Contains("ssb") || text2.Contains("ssgss"))
		{
			return Blue;
		}
		if (text2.Contains("god") || text2.Contains("ssg") || text2.Contains("rose"))
		{
			return God;
		}
		if (text2.Contains("kaioken"))
		{
			return Kaioken;
		}
		if (text2.Contains("ssj") || text2.Contains("super"))
		{
			return SuperSaiyan;
		}
		return BaseAccent;
	}

	public static Color WithAlpha(Color color, float alpha)
	{
		return new Color((int)Math.Clamp((float)(int)color.A * alpha, 0f, 255f), color.R, color.G, color.B);
	}

	public static Color Lerp(Color from, Color to, float t)
	{
		t = Math.Clamp(t, 0f, 1f);
		return new Color((int)((float)(int)from.A + (float)(to.A - from.A) * t), (int)((float)(int)from.R + (float)(to.R - from.R) * t), (int)((float)(int)from.G + (float)(to.G - from.G) * t), (int)((float)(int)from.B + (float)(to.B - from.B) * t));
	}

	public static string Amount(long value)
	{
		return value.ToString("#,0", CultureInfo.InvariantCulture).Replace(',', ' ');
	}

	public static void Draw(Base renderer, IGameTexture? texture, int x, int y, Color color)
	{
		Draw(renderer, texture, x, y, texture?.Width ?? 0, texture?.Height ?? 0, color);
	}

	public static void Draw(Base renderer, IGameTexture? texture, int x, int y, int width, int height, Color color, float u1 = 0f, float v1 = 0f, float u2 = 1f, float v2 = 1f)
	{
		if (texture != null && width > 0 && height > 0 && color.A > 0)
		{
			renderer.DrawColor = color;
			renderer.DrawTexturedRect(texture, new Rectangle(x, y, width, height), color, u1, v1, u2, v2);
		}
	}

	public static void DrawCrop(Base renderer, IGameTexture? texture, int x, int y, int width, Color color)
	{
		if (texture != null)
		{
			width = Math.Clamp(width, 0, texture.Width);
			if (width > 0)
			{
				Draw(renderer, texture, x, y, width, texture.Height, color, 0f, 0f, ((float)width + 0.5f) / (float)texture.Width);
			}
		}
	}

	public static void DrawSlanted(Base renderer, IGameTexture? texture, int x, int y, int width, int slant, Color color)
	{
		if (texture == null)
		{
			return;
		}
		width = Math.Clamp(width, 0, texture.Width);
		if (width <= 0)
		{
			return;
		}
		if (width >= texture.Width || slant <= 0)
		{
			Draw(renderer, texture, x, y, color);
			return;
		}
		int num = Math.Max(0, width - slant);
		if (num > 0)
		{
			DrawCrop(renderer, texture, x, y, num, color);
		}
		int num2 = texture.Width - slant;
		Draw(renderer, texture, x + num, y, slant, texture.Height, color, ((float)num2 + 0.25f) / (float)texture.Width, 0f, ((float)texture.Width + 0.75f) / (float)texture.Width);
	}

	public static void DrawBand(Base renderer, IGameTexture? texture, int x, int y, int from, int to, Color color)
	{
		if (texture != null)
		{
			from = Math.Clamp(from, 0, texture.Width);
			to = Math.Clamp(to, 0, texture.Width);
			if (to > from)
			{
				Draw(renderer, texture, x + from, y, to - from, texture.Height, color, ((float)from + 0.25f) / (float)texture.Width, 0f, ((float)to + 0.75f) / (float)texture.Width);
			}
		}
	}

	public static Point Measure(Base renderer, IFont? font, int size, string? text)
	{
		if (font != null && !string.IsNullOrEmpty(text))
		{
			return renderer.MeasureText(font, size, text);
		}
		return default(Point);
	}

	public static void Text(Base renderer, IFont? font, int size, string? text, int x, int y, Color color, bool shadow = true)
	{
		if (font != null && !string.IsNullOrEmpty(text))
		{
			if (shadow)
			{
				renderer.DrawColor = Shadow;
				renderer.RenderText(font, size, new Point(x + 1, y + 1), text);
			}
			renderer.DrawColor = color;
			renderer.RenderText(font, size, new Point(x, y), text);
		}
	}

	public static void TextLeft(Base renderer, IFont? font, int size, string? text, int x, int y, int height, Color color, bool shadow = true)
	{
		Text(renderer, font, size, text, x, y + (height - Measure(renderer, font, size, text).Y) / 2, color, shadow);
	}

	public static void TextRight(Base renderer, IFont? font, int size, string? text, int right, int y, int height, Color color, bool shadow = true)
	{
		Point point = Measure(renderer, font, size, text);
		Text(renderer, font, size, text, right - point.X, y + (height - point.Y) / 2, color, shadow);
	}

	public static void TextCenter(Base renderer, IFont? font, int size, string? text, int x, int y, int width, int height, Color color, bool shadow = true)
	{
		Point point = Measure(renderer, font, size, text);
		Text(renderer, font, size, text, x + (width - point.X) / 2, y + (height - point.Y) / 2, color, shadow);
	}

	public static int SpacedWidth(Base renderer, IFont? font, int size, string? text, int spacing)
	{
		if (font == null || string.IsNullOrEmpty(text))
		{
			return 0;
		}
		int num = 0;
		for (int i = 0; i < text.Length; i++)
		{
			string text2 = text[i].ToString();
			num += ((text2 == " ") ? (size / 2) : renderer.MeasureText(font, size, text2).X);
			if (i < text.Length - 1)
			{
				num += spacing;
			}
		}
		return num;
	}

	public static string Fit(Base renderer, IFont? font, int size, string text, int width, ref int spacing)
	{
		if (SpacedWidth(renderer, font, size, text, spacing) <= width)
		{
			return text;
		}
		spacing = 0;
		if (SpacedWidth(renderer, font, size, text, 0) <= width)
		{
			return text;
		}
		string text2 = text.TrimEnd();
		while (text2.Length > 1)
		{
			string text3 = text2;
			text2 = text3.Substring(0, text3.Length - 1).TrimEnd();
			string text4 = text2 + "…";
			if (SpacedWidth(renderer, font, size, text4, 0) <= width)
			{
				return text4;
			}
		}
		return "…";
	}

	public static int TextSpaced(Base renderer, IFont? font, int size, string? text, int right, int y, int height, int spacing, Color color)
	{
		if (font == null || string.IsNullOrEmpty(text))
		{
			return 0;
		}
		int[] array = new int[text.Length];
		int num = 0;
		int num2 = 0;
		for (int i = 0; i < text.Length; i++)
		{
			string text2 = text[i].ToString();
			Point point = ((text2 == " ") ? new Point(size / 2, 0) : renderer.MeasureText(font, size, text2));
			array[i] = point.X;
			num2 = Math.Max(num2, point.Y);
			num += point.X + ((i < text.Length - 1) ? spacing : 0);
		}
		int num3 = right - num;
		int y2 = y + (height - num2) / 2;
		for (int j = 0; j < text.Length; j++)
		{
			if (text[j] != ' ')
			{
				Text(renderer, font, size, text[j].ToString(), num3, y2, color);
			}
			num3 += array[j] + spacing;
		}
		return num;
	}

	public static int TopMargin(IGameTexture texture, Rectangle frame)
	{
		string key = $"{texture.Name}:{frame.X},{frame.Y},{frame.Width},{frame.Height}";
		if (TopMargins.TryGetValue(key, out var value))
		{
			return value;
		}
		if (frame.X < 0 || frame.Y < 0 || frame.Width < 1 || frame.Height < 1 || frame.X + frame.Width > texture.Width || frame.Y + frame.Height > texture.Height || !PixelsPlanche.Lire(texture, out Color[] pixels) || pixels.Length < texture.Width * texture.Height)
		{
			return 0;
		}
		value = frame.Height;
		for (int i = 0; i < frame.Height; i++)
		{
			if (value != frame.Height)
			{
				break;
			}
			int num = (frame.Y + i) * texture.Width + frame.X;
			for (int j = 0; j < frame.Width; j++)
			{
				if (pixels[num + j].A > 8)
				{
					value = i;
					break;
				}
			}
		}
		value = ((value < frame.Height) ? value : 0);
		TopMargins[key] = value;
		return value;
	}
}
