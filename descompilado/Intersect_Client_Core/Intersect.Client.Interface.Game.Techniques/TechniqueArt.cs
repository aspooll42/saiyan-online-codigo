using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Intersect.Client.Core;
using Intersect.Client.Framework.File_Management;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.Framework.Gwen;
using Intersect.Client.Framework.Gwen.Renderer;
using Intersect.Client.Framework.Gwen.Skin.Texturing;
using Intersect.Client.Interface.Game.EntityPanel;
using Intersect.Client.Localization;
using Intersect.GameObjects;

namespace Intersect.Client.Interface.Game.Techniques;

internal static class TechniqueArt
{
	public readonly record struct Stops(Color First, float FirstAt, Color Second, float SecondAt, Color Third)
	{
		public static Stops Solid(Color color)
		{
			return new Stops(color, 0f, color, 1f, color);
		}

		public Color At(float t)
		{
			if (!(t <= FirstAt))
			{
				if (!(t <= SecondAt))
				{
					return SaiyanHud.Lerp(Second, Third, (t - SecondAt) / Math.Max(0.0001f, 1f - SecondAt));
				}
				return SaiyanHud.Lerp(First, Second, (t - FirstAt) / Math.Max(0.0001f, SecondAt - FirstAt));
			}
			return First;
		}
	}

	public static readonly Color Void = new Color(255, 6, 8, 14);

	public static readonly Color Line = new Color(255, 36, 48, 73);

	public static readonly Color Line2 = new Color(255, 51, 65, 95);

	public static readonly Color Text = new Color(255, 238, 242, 250);

	public static readonly Color Soft = new Color(255, 200, 208, 224);

	public static readonly Color Dim = new Color(255, 174, 184, 204);

	public static readonly Color Ki = new Color(255, 255, 122, 26);

	public static readonly Color Gold = new Color(255, 255, 208, 106);

	public static readonly Color GoldPale = new Color(255, 255, 240, 194);

	public static readonly Color Cyan = new Color(255, 111, 211, 255);

	public static readonly Color Red = new Color(255, 255, 85, 102);

	public static readonly Color Green = new Color(255, 111, 227, 161);

	public static readonly Color White = new Color(255, 255, 255, 255);

	public static readonly Color Black = new Color(255, 0, 0, 0);

	public static readonly TechniqueFamily[] Families = new TechniqueFamily[6]
	{
		TechniqueFamily.KiBlast,
		TechniqueFamily.Beam,
		TechniqueFamily.Melee,
		TechniqueFamily.Support,
		TechniqueFamily.Transformation,
		TechniqueFamily.Ultimate
	};

	private static readonly Dictionary<string, Bordered> NinePatches = new Dictionary<string, Bordered>();

	private static IFont? _display;

	private static IFont? _heavy;

	private static IFont? _label;

	private static IFont? _body;

	public static IFont? Display
	{
		get
		{
			object obj = _display;
			if (obj == null)
			{
				obj = GameContentManager.Current.GetFont("sairaaffiche") ?? Heavy;
				_display = (IFont?)obj;
			}
			return (IFont?)obj;
		}
	}

	public static IFont? Heavy => _heavy ?? (_heavy = GameContentManager.Current.GetFont("rajdhanibold"));

	public static IFont? Label => _label ?? (_label = GameContentManager.Current.GetFont("chakrapetch"));

	public static IFont? Body
	{
		get
		{
			object obj = _body;
			if (obj == null)
			{
				obj = GameContentManager.Current.GetFont("chakrapetchmedium") ?? Label;
				_body = (IFont?)obj;
			}
			return (IFont?)obj;
		}
	}

	public static Color FamilyColor(TechniqueFamily family)
	{
		return family switch
		{
			TechniqueFamily.KiBlast => new Color(255, 255, 138, 42), 
			TechniqueFamily.Beam => new Color(255, 88, 200, 255), 
			TechniqueFamily.Melee => new Color(255, 255, 85, 102), 
			TechniqueFamily.Support => new Color(255, 255, 226, 122), 
			TechniqueFamily.Transformation => new Color(255, 255, 194, 61), 
			TechniqueFamily.Ultimate => new Color(255, 196, 132, 255), 
			_ => new Color(255, 255, 138, 42), 
		};
	}

	public static int Glyph(TechniqueFamily family)
	{
		return family switch
		{
			TechniqueFamily.KiBlast => 0, 
			TechniqueFamily.Beam => 1, 
			TechniqueFamily.Melee => 2, 
			TechniqueFamily.Support => 3, 
			TechniqueFamily.Transformation => 4, 
			_ => 5, 
		};
	}

	public static string FamilyName(TechniqueFamily family)
	{
		return family switch
		{
			TechniqueFamily.KiBlast => Strings.Techniques.FamilyKiBlast, 
			TechniqueFamily.Beam => Strings.Techniques.FamilyBeam, 
			TechniqueFamily.Melee => Strings.Techniques.FamilyMelee, 
			TechniqueFamily.Support => Strings.Techniques.FamilySupport, 
			TechniqueFamily.Transformation => Strings.Techniques.FamilyTransformation, 
			_ => Strings.Techniques.FamilyUltimate, 
		};
	}

	public static string Roman(int stage)
	{
		return stage switch
		{
			1 => "I", 
			2 => "II", 
			3 => "III", 
			4 => "IV", 
			5 => "V", 
			_ => stage.ToString(), 
		};
	}

	public static Color Alpha(Color color, float alpha)
	{
		return SaiyanHud.WithAlpha(color, alpha);
	}

	public static Color Mix(Color a, Color b, float amountOfA)
	{
		return SaiyanHud.Lerp(b, a, amountOfA);
	}

	public static IGameTexture? Tex(string name)
	{
		return SaiyanHud.Texture(name);
	}

	public static void Fill(Base renderer, Rectangle rect, Color color)
	{
		if (color.A > 0 && rect.Width > 0 && rect.Height > 0)
		{
			renderer.DrawColor = color;
			renderer.DrawFilledRect(rect);
		}
	}

	public static void FillVertical(Base renderer, Rectangle rect, Color top, Color bottom)
	{
		if (rect.Height > 0)
		{
			int num = ((rect.Height <= 24) ? 1 : 2);
			for (int i = 0; i < rect.Height; i += num)
			{
				float t = ((rect.Height <= 1) ? 0f : (((float)i + (float)num / 2f) / (float)rect.Height));
				Fill(renderer, new Rectangle(rect.X, rect.Y + i, rect.Width, Math.Min(num, rect.Height - i)), SaiyanHud.Lerp(top, bottom, t));
			}
		}
	}

	public static void FillHorizontal(Base renderer, Rectangle rect, Color from, Color middle, Color to)
	{
		if (rect.Width > 0)
		{
			for (int i = 0; i < rect.Width; i += 2)
			{
				float num = ((float)i + 1f) / (float)rect.Width;
				Color color = ((num < 0.5f) ? SaiyanHud.Lerp(from, middle, num * 2f) : SaiyanHud.Lerp(middle, to, (num - 0.5f) * 2f));
				Fill(renderer, new Rectangle(rect.X + i, rect.Y, Math.Min(2, rect.Width - i), rect.Height), color);
			}
		}
	}

	public static void Outline(Base renderer, Rectangle rect, Color color, int thickness = 1)
	{
		Fill(renderer, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
		Fill(renderer, new Rectangle(rect.X, rect.Bottom - thickness, rect.Width, thickness), color);
		Fill(renderer, new Rectangle(rect.X, rect.Y + thickness, thickness, rect.Height - 2 * thickness), color);
		Fill(renderer, new Rectangle(rect.Right - thickness, rect.Y + thickness, thickness, rect.Height - 2 * thickness), color);
	}

	public static void Image(Base renderer, string texture, Rectangle rect, Color color)
	{
		SaiyanHud.Draw(renderer, Tex(texture), rect.X, rect.Y, rect.Width, rect.Height, color);
	}

	public static void Row(Base renderer, string texture, int index, int count, Rectangle rect, Color color)
	{
		IGameTexture gameTexture = Tex(texture);
		if (gameTexture != null && gameTexture.Height > 0 && count > 0)
		{
			float v = ((float)index + 0.002f) / (float)count;
			float v2 = ((float)(index + 1) - 0.002f) / (float)count;
			SaiyanHud.Draw(renderer, gameTexture, rect.X, rect.Y, rect.Width, rect.Height, color, 0f, v, 1f, v2);
		}
	}

	public static void Cell(Base renderer, string texture, int index, int cell, Rectangle rect, Color color)
	{
		IGameTexture gameTexture = Tex(texture);
		if (gameTexture != null && gameTexture.Width > 0 && gameTexture.Height > 0)
		{
			float u = ((float)(index * cell) + 0.25f) / (float)gameTexture.Width;
			float u2 = ((float)((index + 1) * cell) - 0.25f) / (float)gameTexture.Width;
			SaiyanHud.Draw(renderer, gameTexture, rect.X, rect.Y, rect.Width, rect.Height, color, u, 0f, u2);
		}
	}

	public static void Patch(Base renderer, string texture, Rectangle rect, Color color, int margin)
	{
		IGameTexture gameTexture = Tex(texture);
		if (gameTexture != null && gameTexture.Width > 0 && gameTexture.Height > 0 && rect.Width > 0 && rect.Height > 0 && color.A > 0)
		{
			string key = $"{texture}:{margin}:{gameTexture.GetHashCode()}";
			if (!NinePatches.TryGetValue(key, out var value))
			{
				value = new Bordered(gameTexture, 0f, 0f, gameTexture.Width, gameTexture.Height, new Margin(margin));
				NinePatches[key] = value;
			}
			value.Draw(renderer, rect, color);
		}
	}

	public static void Glow(Base renderer, float centerX, float centerY, float radius, Color color, bool additive = true)
	{
		GlowRect(renderer, new Rectangle((int)MathF.Round(centerX - radius), (int)MathF.Round(centerY - radius), (int)MathF.Round(radius * 2f), (int)MathF.Round(radius * 2f)), color, additive);
	}

	public static void GlowRect(Base renderer, Rectangle rect, Color color, bool additive = true, string texture = "tech_lueur.png")
	{
		if (rect.Width <= 1 || rect.Height <= 1 || color.A <= 0)
		{
			return;
		}
		if (additive)
		{
			Additive(renderer, delegate
			{
				Image(renderer, texture, rect, color);
			});
		}
		else
		{
			Image(renderer, texture, rect, color);
		}
	}

	public static void Additive(Base renderer, Action draw)
	{
		if (!(renderer is IntersectRenderer { BlendMode: var blendMode } intersectRenderer))
		{
			draw();
			return;
		}
		intersectRenderer.BlendMode = GameBlendModes.Add;
		try
		{
			draw();
		}
		finally
		{
			intersectRenderer.BlendMode = blendMode;
		}
	}

	public static Rectangle BeginClip(Base renderer, Rectangle area)
	{
		Rectangle clipRegion = renderer.ClipRegion;
		Rectangle b = new Rectangle(area.X + renderer.RenderOffset.X, area.Y + renderer.RenderOffset.Y, area.Width, area.Height);
		renderer.ClipRegion = Rectangle.Intersect(clipRegion, b);
		renderer.StartClip();
		return clipRegion;
	}

	public static void EndClip(Base renderer, Rectangle previous)
	{
		renderer.ClipRegion = previous;
		renderer.StartClip();
	}

	public static Point Measure(Base renderer, IFont? font, int size, string? text)
	{
		return SaiyanHud.Measure(renderer, font, size, text);
	}

	private static void WriteAt(Base renderer, IFont? font, int size, string? text, float x, int y, Color color)
	{
		if (font != null && !string.IsNullOrEmpty(text) && color.A > 0)
		{
			renderer.DrawColor = color;
			if (renderer is IntersectRenderer intersectRenderer)
			{
				intersectRenderer.RenderTextPrecise(font, size, x, y, text);
			}
			else
			{
				renderer.RenderText(font, size, new Point((int)MathF.Round(x), y), text);
			}
		}
	}

	public static void Write(Base renderer, IFont? font, int size, string? text, int x, int y, Color color)
	{
		if (font != null && !string.IsNullOrEmpty(text) && color.A > 0)
		{
			renderer.DrawColor = color;
			renderer.RenderText(font, size, new Point(x, y), text);
		}
	}

	public static int TextMiddle(Base renderer, IFont? font, int size, string? text, int x, int centerY, Color color)
	{
		Point point = Measure(renderer, font, size, text);
		Write(renderer, font, size, text, x, centerY - point.Y / 2, color);
		return point.X;
	}

	public static int TextRight(Base renderer, IFont? font, int size, string? text, int right, int centerY, Color color)
	{
		Point point = Measure(renderer, font, size, text);
		Write(renderer, font, size, text, right - point.X, centerY - point.Y / 2, color);
		return point.X;
	}

	public static int TextCenter(Base renderer, IFont? font, int size, string? text, int centerX, int centerY, Color color)
	{
		Point point = Measure(renderer, font, size, text);
		Write(renderer, font, size, text, centerX - point.X / 2, centerY - point.Y / 2, color);
		return point.X;
	}

	public static int SpacedWidth(Base renderer, IFont? font, int size, string? text, float spacing)
	{
		if (font == null || string.IsNullOrEmpty(text))
		{
			return 0;
		}
		float num = 0f;
		for (int i = 0; i < text.Length; i++)
		{
			char c = text[i];
			num += ((c == ' ') ? ((float)Measure(renderer, font, size, "n").X * 0.55f) : ((float)Measure(renderer, font, size, c.ToString()).X));
			num += spacing;
		}
		return (int)MathF.Round(num - spacing);
	}

	public static int Spaced(Base renderer, IFont? font, int size, string? text, int x, int centerY, float spacing, Color color)
	{
		if (font == null || string.IsNullOrEmpty(text))
		{
			return 0;
		}
		int y = Measure(renderer, font, size, "H").Y;
		float num = x;
		for (int i = 0; i < text.Length; i++)
		{
			char c = text[i];
			if (c == ' ')
			{
				num += (float)Measure(renderer, font, size, "n").X * 0.55f + spacing;
				continue;
			}
			string text2 = c.ToString();
			Write(renderer, font, size, text2, (int)MathF.Round(num), centerY - y / 2, color);
			num += (float)Measure(renderer, font, size, text2).X + spacing;
		}
		return (int)MathF.Round(num - spacing - (float)x);
	}

	public static int SpacedRight(Base renderer, IFont? font, int size, string? text, int right, int centerY, float spacing, Color color)
	{
		int num = SpacedWidth(renderer, font, size, text, spacing);
		Spaced(renderer, font, size, text, right - num, centerY, spacing, color);
		return num;
	}

	public static int SpacedCenter(Base renderer, IFont? font, int size, string? text, int centerX, int centerY, float spacing, Color color)
	{
		int num = SpacedWidth(renderer, font, size, text, spacing);
		Spaced(renderer, font, size, text, centerX - num / 2, centerY, spacing, color);
		return num;
	}

	public static void Styled(Base renderer, IFont? font, int size, string? text, int x, int y, Stops stops, float alpha = 1f, bool italic = true)
	{
		if (font == null || string.IsNullOrEmpty(text) || alpha <= 0f)
		{
			return;
		}
		Point point = Measure(renderer, font, size, text);
		int y2 = point.Y;
		float num = (float)size * 4f / 3f;
		float num2 = (float)y2 / 2f - num * 0.5f;
		float num3 = (float)y2 * 0.71f;
		int num4 = ((size >= 24) ? 1 : 2);
		int num5 = (int)((float)y2 * 0.18f);
		int num6 = (int)MathF.Ceiling((float)y2 * 0.86f);
		int num7 = (int)MathF.Ceiling((float)y2 * 0.2f) + 4;
		GameRenderer renderer2 = Graphics.Renderer;
		bool linearFiltering = renderer2?.LinearFiltering ?? false;
		if (renderer2 != null)
		{
			renderer2.LinearFiltering = true;
		}
		try
		{
			for (int i = num5; i < num6; i += num4)
			{
				float num8 = (float)i + (float)num4 / 2f;
				float num9 = (italic ? ((num3 - num8) * 0.2f) : 0f);
				Color color = Alpha(stops.At((num8 - num2) / num), alpha);
				Rectangle previous = BeginClip(renderer, new Rectangle(x - num7, y + i, point.X + num7 * 2, num4));
				WriteAt(renderer, font, size, text, (float)x + num9, y, color);
				EndClip(renderer, previous);
			}
		}
		finally
		{
			if (renderer2 != null)
			{
				renderer2.LinearFiltering = linearFiltering;
			}
		}
	}

	public static void Italic(Base renderer, IFont? font, int size, string? text, int x, int y, Color color)
	{
		Styled(renderer, font, size, text, x, y, Stops.Solid(color));
	}

	public static List<string> Wrap(Base renderer, IFont? font, int size, string? text, int width, int maxLines)
	{
		List<string> list = new List<string>();
		if (font == null || string.IsNullOrWhiteSpace(text) || width <= 0)
		{
			return list;
		}
		string[] array = text.Replace("\r", string.Empty).Split('\n');
		foreach (string text2 in array)
		{
			string text3 = string.Empty;
			string[] array2 = text2.Split(' ', StringSplitOptions.RemoveEmptyEntries);
			foreach (string text4 in array2)
			{
				string text5 = ((text3.Length == 0) ? text4 : (text3 + " " + text4));
				if (text3.Length > 0 && Measure(renderer, font, size, text5).X > width)
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
		if (list.Count > maxLines)
		{
			list = list.Take(maxLines).ToList();
			List<string> list2 = list;
			string text6 = list2[list2.Count - 1];
			while (text6.Length > 1 && Measure(renderer, font, size, text6 + "…").X > width)
			{
				string text7 = text6;
				text6 = text7.Substring(0, text7.Length - 1).TrimEnd();
			}
			List<string> list3 = list;
			list3[list3.Count - 1] = text6 + "…";
		}
		return list;
	}

	public static string Number(double value)
	{
		double num = Math.Round(value, 2);
		return num.ToString((num == Math.Floor(num)) ? "0" : "0.##", CultureInfo.InvariantCulture).Replace('.', ',');
	}

	public static string Signed(double value)
	{
		return ((value > 0.0) ? "+" : "-") + Number(Math.Abs(value));
	}
}
