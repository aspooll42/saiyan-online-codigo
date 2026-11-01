using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Numerics;
using Intersect.Client.Core;
using Intersect.Client.Framework.File_Management;
using Intersect.Client.Framework.Graphics;

namespace Intersect.Client.Interface.Menu;

internal static class MenuDraw
{
	public const float DesignWidth = 1920f;

	public const float DesignHeight = 1080f;

	private static readonly Stopwatch Horloge = Stopwatch.StartNew();

	private static readonly Dictionary<string, IFont?> Fonts = new Dictionary<string, IFont>();

	public static readonly Color Night = new Color(255, 11, 14, 23);

	public static readonly Color White = new Color(255, 242, 242, 242);

	public static readonly Color Mist = new Color(255, 222, 228, 240);

	public static readonly Color Steel = new Color(255, 190, 199, 217);

	public static readonly Color Ki = new Color(255, 255, 122, 26);

	public static readonly Color KiLight = new Color(255, 255, 172, 82);

	public static readonly Color KiDark = new Color(255, 200, 82, 8);

	public static readonly Color Gold = new Color(255, 245, 190, 90);

	public static readonly Color Green = new Color(255, 78, 200, 120);

	public static readonly Color Red = new Color(255, 255, 120, 110);

	private static IGameRenderTexture? _glassTarget;

	private const float ReadableScreenPx = 13f;

	public static long Maintenant => 10000 + Horloge.ElapsedMilliseconds;

	public static float Scale { get; private set; } = 1f;

	public static float Width { get; private set; } = 1920f;

	public static float Height { get; private set; } = 1080f;

	public static float MiddleOffset => (Height - 1080f) / 2f;

	public static GameRenderer Renderer => Graphics.Renderer;

	public static IGameTexture Pixel => Renderer.WhitePixel;

	public static IGameRenderTexture? GlassTarget
	{
		get
		{
			GameRenderer renderer = Renderer;
			if (_glassTarget == null || _glassTarget.Width != renderer.ScreenWidth || _glassTarget.Height != renderer.ScreenHeight)
			{
				_glassTarget?.Dispose();
				_glassTarget = renderer.CreateRenderTexture(renderer.ScreenWidth, renderer.ScreenHeight);
			}
			return _glassTarget;
		}
	}

	public static bool GlassReady { get; set; }

	public static void Begin()
	{
		GameRenderer renderer = Renderer;
		Scale = Math.Max(0.1f, Math.Min((float)renderer.ScreenWidth / 1920f, (float)renderer.ScreenHeight / 1080f));
		Width = (float)renderer.ScreenWidth / Scale;
		Height = (float)renderer.ScreenHeight / Scale;
		renderer.LinearFiltering = true;
	}

	public static void End()
	{
		Renderer.LinearFiltering = false;
	}

	public static Color Alpha(Color color, float alpha)
	{
		return new Color((int)Math.Clamp((float)(int)color.A * alpha, 0f, 255f), color.R, color.G, color.B);
	}

	public static Color Rgba(int r, int g, int b, float alpha)
	{
		return new Color((int)Math.Clamp(255f * alpha, 0f, 255f), r, g, b);
	}

	public static Color Mix(Color a, Color b, float k)
	{
		k = Math.Clamp(k, 0f, 1f);
		return new Color((int)((float)(int)a.A + (float)(b.A - a.A) * k), (int)((float)(int)a.R + (float)(b.R - a.R) * k), (int)((float)(int)a.G + (float)(b.G - a.G) * k), (int)((float)(int)a.B + (float)(b.B - a.B) * k));
	}

	public static float Clamp01(float value)
	{
		return Math.Clamp(value, 0f, 1f);
	}

	public static float EaseOut(float k)
	{
		return 1f - MathF.Pow(1f - Clamp01(k), 3f);
	}

	public static float EaseSlide(float k)
	{
		return 1f - MathF.Pow(1f - Clamp01(k), 4f);
	}

	public static float Lerp(float a, float b, float k)
	{
		return a + (b - a) * k;
	}

	public static float Approach(float current, float target, float dt, float rate)
	{
		return current + (target - current) * (1f - MathF.Exp((0f - dt) * rate));
	}

	public static void ScreenRect(float x, float y, float width, float height, Color color, GameBlendModes mode = GameBlendModes.None)
	{
		if (!(width <= 0f) && !(height <= 0f) && color.A != 0)
		{
			Renderer.DrawTexture(Pixel, 0f, 0f, 1f, 1f, x, y, width, height, color, null, mode);
		}
	}

	public static void Rect(float x, float y, float width, float height, Color color, GameBlendModes mode = GameBlendModes.None)
	{
		ScreenRect(x * Scale, y * Scale, width * Scale, height * Scale, color, mode);
	}

	public static void Line(float x1, float y1, float x2, float y2, float thickness, Color color, GameBlendModes mode = GameBlendModes.None)
	{
		float num = x2 - x1;
		float num2 = y2 - y1;
		float num3 = MathF.Sqrt(num * num + num2 * num2);
		if (!(num3 < 0.01f) && color.A != 0)
		{
			float num4 = (x1 + x2) / 2f;
			float num5 = (y1 + y2) / 2f;
			float rotationDegrees = MathF.Atan2(num2, num) * 180f / (float)Math.PI;
			Renderer.DrawTexture(Pixel, 0f, 0f, 1f, 1f, (num4 - num3 / 2f) * Scale, (num5 - thickness / 2f) * Scale, num3 * Scale, thickness * Scale, color, null, mode, null, rotationDegrees);
		}
	}

	public static void Texture(IGameTexture? texture, float x, float y, float width, float height, Color color, GameBlendModes mode = GameBlendModes.None, float rotationDegrees = 0f)
	{
		if (texture != null && color.A != 0)
		{
			Renderer.DrawTexture(texture, 0f, 0f, texture.Width, texture.Height, x * Scale, y * Scale, width * Scale, height * Scale, color, null, mode, null, rotationDegrees);
		}
	}

	public static void TexturePart(IGameTexture? texture, float sx, float sy, float sw, float sh, float x, float y, float width, float height, Color color, GameBlendModes mode = GameBlendModes.None)
	{
		if (texture != null && color.A != 0 && !(sw <= 0f) && !(sh <= 0f))
		{
			Renderer.DrawTexture(texture, sx, sy, sw, sh, x * Scale, y * Scale, width * Scale, height * Scale, color, null, mode);
		}
	}

	public static void Bevel(float x, float y, float width, float height, float cut, Color color, GameBlendModes mode = GameBlendModes.None)
	{
		if (color.A == 0)
		{
			return;
		}
		foreach (var (x2, y2, width2, height2) in BevelRows(x, y, width, height, cut))
		{
			ScreenRect(x2, y2, width2, height2, color, mode);
		}
	}

	public static IEnumerable<(float X, float Y, float Width, float Height)> BevelRows(float x, float y, float width, float height, float cut)
	{
		float left = MathF.Round(x * Scale);
		float top = MathF.Round(y * Scale);
		float num = MathF.Round((x + width) * Scale);
		float bottom = MathF.Round((y + height) * Scale);
		int cutRows = (int)Math.Min(MathF.Round(cut * Scale), (bottom - top) / 2f);
		float fullWidth = num - left;
		for (int row = 0; row < cutRows; row++)
		{
			yield return (X: left, Y: top + (float)row, Width: fullWidth - (float)(cutRows - row), Height: 1f);
		}
		if (bottom - top - (float)(2 * cutRows) > 0f)
		{
			yield return (X: left, Y: top + (float)cutRows, Width: fullWidth, Height: bottom - top - (float)(2 * cutRows));
		}
		for (int row = 0; row < cutRows; row++)
		{
			int num2 = row + 1;
			yield return (X: left + (float)num2, Y: bottom - (float)cutRows + (float)row, Width: fullWidth - (float)num2, Height: 1f);
		}
	}

	public static void BevelOutline(float x, float y, float width, float height, float cut, Color color, float thickness = 1.5f)
	{
		float num = x + width;
		float num2 = y + height;
		Line(x, y + thickness / 2f, num - cut, y + thickness / 2f, thickness, color);
		Line(num - cut, y + thickness / 2f, num - thickness / 2f, y + cut, thickness, color);
		Line(num - thickness / 2f, y + cut, num - thickness / 2f, num2, thickness, color);
		Line(num, num2 - thickness / 2f, x + cut, num2 - thickness / 2f, thickness, color);
		Line(x + cut, num2 - thickness / 2f, x + thickness / 2f, num2 - cut, thickness, color);
		Line(x + thickness / 2f, num2 - cut, x + thickness / 2f, y, thickness, color);
	}

	public static void HorizontalRamp(IGameTexture? ramp, float x, float y, float width, float height, Color tint, GameBlendModes mode = GameBlendModes.None)
	{
		if (ramp != null)
		{
			Renderer.DrawTexture(ramp, 0f, 0f, ramp.Width, 1f, x * Scale, y * Scale, width * Scale, height * Scale, tint, null, mode);
		}
	}

	public static void Glass(float x, float y, float width, float height, float cut, Color tint, float alpha = 1f)
	{
		if (alpha <= 0f)
		{
			return;
		}
		IGameRenderTexture gameRenderTexture = (GlassReady ? _glassTarget : null);
		foreach (var (num, num2, num3, num4) in BevelRows(x, y, width, height, cut))
		{
			if (gameRenderTexture != null)
			{
				Renderer.DrawTexture(gameRenderTexture, num, num2, num3, num4, num, num2, num3, num4, Alpha(Color.White, alpha));
			}
			ScreenRect(num, num2, num3, num4, Alpha(tint, alpha));
		}
	}

	private static IFont? Font(string name)
	{
		if (!Fonts.TryGetValue(name, out IFont value) || value == null)
		{
			value = GameContentManager.Current.GetFont(name);
			Fonts[name] = value;
		}
		return value;
	}

	private static (IFont? Font, int Size, float FontScale) Resolve(MenuFont role, float px)
	{
		float num = px * Scale * 0.75f;
		IFont font = role switch
		{
			MenuFont.Saira => ((num > 30f) ? Font("sairamenu") : null) ?? Font("rajdhanibold"), 
			MenuFont.ChakraMedium => ((num >= 18f) ? Font("chakrapetchmedium") : null) ?? Font("chakrapetch"), 
			_ => Font("chakrapetch"), 
		};
		if (font == null || font.SupportedSizes.Count == 0)
		{
			return (Font: font, Size: (int)MathF.Round(num), FontScale: 1f);
		}
		int num2 = 0;
		foreach (int supportedSize in font.SupportedSizes)
		{
			num2 = supportedSize;
			if ((float)supportedSize >= num)
			{
				break;
			}
		}
		return (Font: font, Size: num2, FontScale: num / (float)num2);
	}

	public static string Upper(string text)
	{
		return text.ToUpper(CultureInfo.GetCultureInfo("fr-FR"));
	}

	private static float Readable(float px)
	{
		if (!(Scale > 0f) || !(px * Scale < 13f))
		{
			return px;
		}
		return MathF.Min(13f / Scale, px * 1.35f);
	}

	public static (float Width, float Height) Measure(string text, MenuFont role, float px, float spacingEm = 0f)
	{
		px = Readable(px);
		var (font, size, num) = Resolve(role, px);
		if (font == null || string.IsNullOrEmpty(text))
		{
			return (Width: 0f, Height: px);
		}
		float extraSpacing = spacingEm * px * Scale / num;
		Vector2 vector = Renderer.MeasureTextScaled(text, font, size, num, extraSpacing);
		return (Width: vector.X / Scale, Height: vector.Y / Scale);
	}

	public static float Text(string text, MenuFont role, float px, float x, float top, Color color, MenuAlign align = MenuAlign.Left, float spacingEm = 0f, float lineHeight = 0f)
	{
		if (string.IsNullOrEmpty(text) || color.A == 0)
		{
			return 0f;
		}
		float num = Readable(px);
		var (font, size, num2) = Resolve(role, num);
		if (font == null)
		{
			return 0f;
		}
		float extraSpacing = spacingEm * num * Scale / num2;
		Vector2 vector = Renderer.MeasureTextScaled(text, font, size, num2, extraSpacing);
		float num3 = vector.X / Scale;
		float num4 = vector.Y / Scale;
		float num5 = ((lineHeight > 0f) ? lineHeight : (px * 1.2f));
		float num6 = top + (num5 - num4) / 2f;
		float num7 = align switch
		{
			MenuAlign.Center => x - num3 / 2f, 
			MenuAlign.Right => x - num3, 
			_ => x, 
		};
		int num8 = (color.R * 299 + color.G * 587 + color.B * 114) / 1000;
		if (num8 > 140 && color.A > 24)
		{
			float num9 = MathF.Round(num7 * Scale);
			float num10 = MathF.Round(num6 * Scale);
			Color color2 = new Color((int)((float)(int)color.A * 0.72f), Night.R, Night.G, Night.B);
			Renderer.DrawStringScaled(text, font, size, num9 + 1f, num10 + 1f, num2, color2, extraSpacing);
			if (num * Scale < 26f)
			{
				Renderer.DrawStringScaled(text, font, size, num9 + 2f, num10 + 1f, num2, new Color((int)((float)(int)color.A * 0.45f), Night.R, Night.G, Night.B), extraSpacing);
				Renderer.DrawStringScaled(text, font, size, num9 + 1f, num10, num2, new Color((int)((float)(int)color.A * 0.6f), color.R, color.G, color.B), extraSpacing);
			}
		}
		Renderer.DrawStringScaled(text, font, size, MathF.Round(num7 * Scale), MathF.Round(num6 * Scale), num2, color, extraSpacing);
		return num3;
	}

	public static List<string> Wrap(string text, MenuFont role, float px, float maxWidth, float spacingEm = 0f)
	{
		List<string> list = new List<string>();
		string[] array = text.Split('\n');
		foreach (string text2 in array)
		{
			string text3 = string.Empty;
			string[] array2 = text2.Split(' ');
			foreach (string text4 in array2)
			{
				string text5 = ((text3.Length == 0) ? text4 : (text3 + " " + text4));
				if (text3.Length > 0 && Measure(text5, role, px, spacingEm).Width > maxWidth)
				{
					list.Add(text3);
					text3 = text4;
				}
				else
				{
					text3 = text5;
				}
			}
			list.Add(text3);
		}
		return list;
	}

	public static float Paragraph(string text, MenuFont role, float px, float x, float top, float maxWidth, float lineHeight, Color color)
	{
		List<string> list = Wrap(text, role, px, maxWidth);
		for (int i = 0; i < list.Count; i++)
		{
			Text(list[i], role, px, x, top + (float)i * lineHeight, color, MenuAlign.Left, 0f, lineHeight);
		}
		return (float)list.Count * lineHeight;
	}
}
