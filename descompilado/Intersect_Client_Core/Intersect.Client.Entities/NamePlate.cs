using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Intersect.Client.Core;
using Intersect.Client.Framework.Content;
using Intersect.Client.Framework.File_Management;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.Framework.Gwen.Renderer;
using Intersect.Client.General;
using Intersect.Framework.Core.GameObjects.Ornaments;
using Intersect.Framework.Core.GameObjects.Titles;

namespace Intersect.Client.Entities;

public static class NamePlate
{
	public sealed record Content(string? Header, Color HeaderColor, string? Clan, string? Rank, string Name, Color NameColor, TitleDescriptor? Title, OrnamentDescriptor? Ornament);

	private readonly record struct Line(string Text, IFont Font, int Size, int Height, Color Color);

	private sealed class Layout
	{
		public required List<Line> Lines { get; init; }

		public required int Width { get; init; }

		public required int Height { get; init; }

		public required int TextHeight { get; init; }

		public required string? Initials { get; init; }

		public required IGameTexture Frame { get; init; }

		public required IGameTexture? Emblem { get; init; }

		public required string? Style { get; init; }
	}

	private const int PaddingX = 12;

	private const int PaddingY = 7;

	private const int EmblemSize = 34;

	private const int EmblemGap = 10;

	private const int SmallLineHeight = 18;

	private const int NameLineHeight = 24;

	private const int FrameMargin = 12;

	private const int SideOverlap = 3;

	private const int SmallFontSize = 10;

	private const int NameFontSize = 16;

	private const int InitialsFontSize = 14;

	private static float _scale = 1f;

	private static readonly int[] CompiledFontSizes = new int[15]
	{
		8, 9, 10, 11, 12, 13, 14, 16, 18, 20,
		22, 24, 26, 28, 32
	};

	public const int GapAboveHead = 6;

	private static readonly Color ClanColor = new Color(255, 140, 235, 170);

	private static readonly Color TitleColor = new Color(255, 176, 186, 206);

	private static readonly Color InitialsColor = new Color(255, 255, 172, 82);

	private static readonly Color ShadowColor = new Color(170, 0, 0, 0);

	private static readonly HashSet<string> SmallWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
	{
		"l", "le", "la", "les", "de", "du", "des", "d", "et", "un",
		"une", "the", "of"
	};

	private static IFont? _smallFont;

	private static IFont? _nameFont;

	private static IFont? _initialsFont;

	private static readonly List<FloatRect> Placed = new List<FloatRect>();

	private static float WorldScale()
	{
		return Math.Clamp(Math.Max(0.25f, Graphics.WorldScale) / 2f, 0.6f, 1f);
	}

	private static int Px(int value)
	{
		return Math.Max(1, (int)MathF.Round((float)value * _scale));
	}

	private static int FontPx(int size)
	{
		float num = (float)size * _scale;
		int num2 = CompiledFontSizes[0];
		int[] compiledFontSizes = CompiledFontSizes;
		foreach (int num3 in compiledFontSizes)
		{
			if (Math.Abs((float)num3 - num) < Math.Abs((float)num2 - num))
			{
				num2 = num3;
			}
		}
		return num2;
	}

	public static Content ContentOf(Player player, Color nameColor)
	{
		return new Content(player.HeaderLabel.Text, player.HeaderLabel.Color, player.Guild?.Trim(), player.GuildRank?.Title, player.Name, nameColor, player.Title, player.Ornament);
	}

	public static bool Draw(Player player, Color nameColor)
	{
		GameRenderer renderer = Graphics.Renderer;
		if (renderer == null)
		{
			return false;
		}
		_scale = WorldScale();
		if (!TryCompose(ContentOf(player, nameColor), (string text, IFont font, int size) => renderer.MeasureText(text, font, size, 1f).X, out Layout layout))
		{
			return false;
		}
		FloatRect bodyWorldPos = player.BodyWorldPos;
		DrawInWorld(renderer, layout, bodyWorldPos.X + bodyWorldPos.Width / 2f, player.NamePlateBottom());
		return true;
	}

	public static void BeginFrame()
	{
		Placed.Clear();
	}

	public static bool DrawSimple(Entity entity, string name, Color nameColor)
	{
		GameRenderer renderer = Graphics.Renderer;
		if (renderer == null || string.IsNullOrWhiteSpace(name))
		{
			return false;
		}
		_scale = WorldScale();
		Content content = new Content(null, Color.White, null, null, name, nameColor, null, null);
		if (!TryCompose(content, (string text, IFont font, int size) => renderer.MeasureText(text, font, size, 1f).X, out Layout layout))
		{
			return false;
		}
		FloatRect bodyWorldPos = entity.BodyWorldPos;
		DrawInWorld(renderer, layout, bodyWorldPos.X + bodyWorldPos.Width / 2f, entity.PlateBottomAboveHead());
		return true;
	}

	public static bool DrawAt(string name, Color nameColor, string? header, Color headerColor, float centerX, float bottom)
	{
		GameRenderer renderer = Graphics.Renderer;
		if (renderer == null || string.IsNullOrWhiteSpace(name))
		{
			return false;
		}
		_scale = WorldScale();
		Content content = new Content(header, headerColor, null, null, name, nameColor, null, null);
		if (!TryCompose(content, (string text, IFont font, int size) => renderer.MeasureText(text, font, size, 1f).X, out Layout layout))
		{
			return false;
		}
		DrawInWorld(renderer, layout, centerX, bottom);
		return true;
	}

	private static void DrawInWorld(GameRenderer renderer, Layout layout, float centerX, float bottom)
	{
		float zoom = Math.Max(0.25f, Graphics.WorldScale);
		float pixel = 1f / zoom;
		FloatRect view = Graphics.CurrentView;
		FloatRect worldViewport = Graphics.WorldViewport;
		float num = (float)layout.Width * pixel;
		float num2 = (float)layout.Height * pixel;
		float num3 = 4f * pixel;
		float num4 = Math.Clamp(centerX - num / 2f, worldViewport.X + num3, Math.Max(worldViewport.X + num3, worldViewport.X + worldViewport.Width - num - num3));
		float num5 = Math.Max(worldViewport.Y + num3, bottom - num2);
		for (int i = 0; i < 8; i++)
		{
			bool flag = false;
			foreach (FloatRect item in Placed)
			{
				if (num4 < item.X + item.Width && num4 + num > item.X && num5 < item.Y + item.Height && num5 + num2 > item.Y)
				{
					num5 = item.Y - num2 - 2f * pixel;
					flag = true;
				}
			}
			if (!flag)
			{
				break;
			}
		}
		Placed.Add(new FloatRect(num4, num5, num, num2));
		float left = Snap(num4, view.X, zoom);
		float top = Snap(num5, view.Y, zoom);
		Render(layout, left, top, pixel, (float x) => Snap(x, view.X, zoom), (float y) => Snap(y, view.Y, zoom), delegate(IGameTexture texture, FloatRect source, float x, float y, float w, float h)
		{
			Graphics.DrawGameTexture(texture, source, new FloatRect(x, y, w, h), Color.White);
		}, (string text, IFont font, int size) => renderer.MeasureText(text, font, size, pixel), delegate(string text, IFont font, int size, float x, float y, Color color)
		{
			DrawWorldText(renderer, text, font, size, x, y, pixel, color);
		});
	}

	public static void DrawInterface(Content content, Base renderer, int centerX, int bottomY, float scale = 1f)
	{
		_scale = Math.Clamp(scale, 0.5f, 2f);
		if (TryCompose(content, (string text, IFont font, int size) => renderer.MeasureText(font, size, text).X, out Layout layout))
		{
			Render(layout, centerX - layout.Width / 2, bottomY - layout.Height, 1f, MathF.Round, MathF.Round, delegate(IGameTexture texture, FloatRect source, float x, float y, float w, float h)
			{
				renderer.DrawColor = Color.White;
				renderer.DrawTexturedRect(texture, new Rectangle((int)MathF.Round(x), (int)MathF.Round(y), (int)MathF.Round(w), (int)MathF.Round(h)), Color.White, source.X / (float)texture.Width, source.Y / (float)texture.Height, (source.X + source.Width) / (float)texture.Width, (source.Y + source.Height) / (float)texture.Height);
			}, delegate(string text, IFont font, int size)
			{
				Point point = renderer.MeasureText(font, size, text);
				return new Vector2(point.X, point.Y);
			}, delegate(string text, IFont font, int size, float x, float y, Color color)
			{
				renderer.DrawColor = ShadowColor;
				renderer.RenderText(font, size, new Point((int)x + 1, (int)y + 1), text);
				renderer.DrawColor = color;
				renderer.RenderText(font, size, new Point((int)x, (int)y), text);
			});
		}
	}

	public static Point MeasureInterface(Content content, Base renderer, float scale = 1f)
	{
		_scale = Math.Clamp(scale, 0.5f, 2f);
		if (!TryCompose(content, (string text, IFont font, int size) => renderer.MeasureText(font, size, text).X, out Layout layout))
		{
			return default(Point);
		}
		return new Point(layout.Width, layout.Height);
	}

	private static bool TryCompose(Content content, Func<string, IFont, int, float> measureWidth, out Layout layout)
	{
		layout = null;
		GameContentManager contentManager = Globals.ContentManager;
		if (_smallFont == null)
		{
			_smallFont = contentManager.GetFont("chakrapetch");
		}
		if (_nameFont == null)
		{
			_nameFont = contentManager.GetFont("sairanom") ?? contentManager.GetFont("rajdhanibold");
		}
		if (_initialsFont == null)
		{
			_initialsFont = contentManager.GetFont("refuel");
		}
		if (_smallFont == null || _nameFont == null)
		{
			return false;
		}
		string style = content.Ornament?.Style;
		IGameTexture gameTexture = Part(style, "cadre") ?? contentManager.GetTexture(TextureType.Gui, "plaque_nom_1.png");
		if (gameTexture == null || gameTexture.Width <= 0)
		{
			return false;
		}
		bool flag = !string.IsNullOrWhiteSpace(content.Clan);
		List<Line> list = new List<Line>();
		if (!string.IsNullOrWhiteSpace(content.Header))
		{
			list.Add(new Line(content.Header, _smallFont, FontPx(10), Px(18), content.HeaderColor));
		}
		if (flag)
		{
			string text = (string.IsNullOrWhiteSpace(content.Rank) ? content.Clan : (content.Clan + " · " + content.Rank));
			list.Add(new Line(text, _smallFont, FontPx(10), Px(18), ClanColor));
		}
		list.Add(new Line(content.Name, _nameFont, FontPx(16), Px(24), content.NameColor));
		TitleDescriptor title = content.Title;
		if (title != null && !string.IsNullOrWhiteSpace(title.Name))
		{
			list.Add(new Line("« " + title.Name + " »", _smallFont, FontPx(10), Px(18), title.Color ?? TitleColor));
		}
		float num = 0f;
		int num2 = 0;
		foreach (Line item in list)
		{
			num = Math.Max(num, measureWidth(item.Text, item.Font, item.Size));
			num2 += item.Height;
		}
		int num3 = (flag ? (Px(34) + Px(10)) : 0);
		layout = new Layout
		{
			Lines = list,
			Width = (int)Math.Ceiling((float)(Px(12) + num3) + num + (float)Px(12) + (float)Px(6)),
			Height = Math.Max(num2, flag ? Px(34) : 0) + 2 * Px(7),
			TextHeight = num2,
			Initials = (flag ? Initials(content.Clan) : null),
			Frame = gameTexture,
			Emblem = (Part(style, "embleme") ?? contentManager.GetTexture(TextureType.Gui, "plaque_embleme_1.png")),
			Style = style
		};
		return true;
	}

	private static void Render(Layout layout, float left, float top, float pixel, Func<float, float> snapX, Func<float, float> snapY, Action<IGameTexture, FloatRect, float, float, float, float> image, Func<string, IFont, int, Vector2> measure, Action<string, IFont, int, float, float, Color> text)
	{
		float num = (float)layout.Width * pixel;
		float num2 = (float)layout.Height * pixel;
		string style = layout.Style;
		IGameTexture gameTexture = Part(style, "lueur");
		if (gameTexture != null)
		{
			Whole(gameTexture, left - num * 0.25f, top - num2 * 0.35f, num * 1.5f, num2 * 1.7f);
		}
		IGameTexture gameTexture2 = Part(style, "gauche");
		if (gameTexture2 != null)
		{
			Whole(gameTexture2, left - ((float)gameTexture2.Width * _scale - (float)Px(3)) * pixel, snapY(top + (num2 - (float)gameTexture2.Height * pixel * _scale) / 2f), (float)gameTexture2.Width * pixel * _scale, (float)gameTexture2.Height * pixel * _scale);
		}
		IGameTexture gameTexture3 = Part(style, "droite");
		if (gameTexture3 != null)
		{
			Whole(gameTexture3, left + num - (float)Px(3) * pixel, snapY(top + (num2 - (float)gameTexture3.Height * pixel * _scale) / 2f), (float)gameTexture3.Width * pixel * _scale, (float)gameTexture3.Height * pixel * _scale);
		}
		IGameTexture gameTexture4 = Part(style, "socle");
		if (gameTexture4 != null)
		{
			Centered(gameTexture4, left + num / 2f, top + num2);
		}
		DrawNinePatch(layout.Frame, left, top, num, num2, 12, pixel * _scale, image);
		float num3 = left + (float)Px(12) * pixel;
		string initials = layout.Initials;
		if (initials != null)
		{
			float num4 = left + (float)Px(12) * pixel;
			float num5 = snapY(top + (num2 - (float)Px(34) * pixel) / 2f);
			IGameTexture emblem = layout.Emblem;
			if (emblem != null && emblem.Width > 0)
			{
				Whole(emblem, num4, num5, (float)Px(34) * pixel, (float)Px(34) * pixel);
			}
			if (_initialsFont != null)
			{
				Vector2 vector = measure(initials, _initialsFont, FontPx(14));
				text(initials, _initialsFont, FontPx(14), snapX(num4 + ((float)Px(34) * pixel - vector.X) / 2f), snapY(num5 + ((float)Px(34) * pixel - vector.Y) / 2f), InitialsColor);
			}
			num3 += (float)(Px(34) + Px(10)) * pixel;
		}
		float num6 = left + num - (float)Px(12) * pixel - num3;
		float num7 = top + (num2 - (float)layout.TextHeight * pixel) / 2f;
		foreach (Line line in layout.Lines)
		{
			Vector2 vector2 = measure(line.Text, line.Font, line.Size);
			text(line.Text, line.Font, line.Size, snapX(num3 + (num6 - vector2.X) / 2f), snapY(num7 + ((float)line.Height * pixel - vector2.Y) / 2f), line.Color);
			num7 += (float)line.Height * pixel;
		}
		IGameTexture gameTexture5 = Part(style, "sommet");
		if (gameTexture5 != null)
		{
			Centered(gameTexture5, left + num / 2f, top);
		}
		void Centered(IGameTexture texture, float centerX, float centerY)
		{
			Whole(texture, snapX(centerX - (float)texture.Width * pixel * _scale / 2f), snapY(centerY - (float)texture.Height * pixel * _scale / 2f), (float)texture.Width * pixel * _scale, (float)texture.Height * pixel * _scale);
		}
		void Whole(IGameTexture texture, float x, float y, float w, float h)
		{
			image(texture, new FloatRect(0f, 0f, texture.Width, texture.Height), x, y, w, h);
		}
	}

	private static IGameTexture? Part(string? style, string part)
	{
		if (!string.IsNullOrWhiteSpace(style))
		{
			IGameTexture texture = Globals.ContentManager.GetTexture(TextureType.Gui, OrnamentDescriptor.TextureName(style, part));
			if (texture != null && texture.Width > 0)
			{
				return texture;
			}
		}
		return null;
	}

	private static string Initials(string clan)
	{
		List<string> list = (from word in clan.Split(new char[4] { ' ', '\'', '’', '-' }, StringSplitOptions.RemoveEmptyEntries)
			where !SmallWords.Contains(word)
			select word).ToList();
		if (list.Count == 0)
		{
			if (clan.Length <= 0)
			{
				return "?";
			}
			return clan.Substring(0, 1).ToUpperInvariant();
		}
		if (list.Count != 1)
		{
			return $"{list[0][0]}{list[1][0]}".ToUpperInvariant();
		}
		return list[0].Substring(0, Math.Min(2, list[0].Length)).ToUpperInvariant();
	}

	private static void DrawNinePatch(IGameTexture texture, float x, float y, float width, float height, int margin, float pixel, Action<IGameTexture, FloatRect, float, float, float, float> image)
	{
		float[] array = new float[4]
		{
			0f,
			margin,
			texture.Width - margin,
			texture.Width
		};
		float[] array2 = new float[4]
		{
			0f,
			margin,
			texture.Height - margin,
			texture.Height
		};
		float num = (float)margin * pixel;
		float[] array3 = new float[4]
		{
			x,
			x + num,
			x + width - num,
			x + width
		};
		float[] array4 = new float[4]
		{
			y,
			y + num,
			y + height - num,
			y + height
		};
		for (int i = 0; i < 3; i++)
		{
			for (int j = 0; j < 3; j++)
			{
				image(texture, new FloatRect(array[i], array2[j], array[i + 1] - array[i], array2[j + 1] - array2[j]), array3[i], array4[j], array3[i + 1] - array3[i], array4[j + 1] - array4[j]);
			}
		}
	}

	private static void DrawWorldText(GameRenderer renderer, string text, IFont font, int size, float x, float y, float pixel, Color color)
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
}
