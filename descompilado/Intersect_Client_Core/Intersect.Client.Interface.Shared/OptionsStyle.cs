using System;
using System.Collections.Generic;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.Framework.Gwen;
using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.Framework.Gwen.Input;
using Intersect.Client.Framework.Gwen.Renderer;
using Intersect.Client.Framework.Gwen.Skin.Texturing;
using Intersect.Client.Interface.Game.EntityPanel;

namespace Intersect.Client.Interface.Shared;

internal static class OptionsStyle
{
	public const int LabelSize = 11;

	public const int HelpSize = 10;

	public const int CapsSize = 9;

	public static readonly Color White = new Color(255, 242, 242, 242);

	public static readonly Color Label = new Color(255, 223, 229, 242);

	public static readonly Color Soft = new Color(255, 176, 186, 206);

	public static readonly Color Dim = new Color(255, 150, 160, 182);

	public static readonly Color Glyph = new Color(255, 176, 184, 200);

	public static readonly Color Orange = new Color(255, 255, 122, 26);

	public static readonly Color OrangeLight = new Color(255, 255, 172, 82);

	public static readonly Color GoldLight = new Color(255, 255, 226, 150);

	public static readonly Color Night = new Color(255, 11, 14, 23);

	public static readonly Color RuleColor = new Color(80, 62, 74, 104);

	public static readonly Color SectionLine = new Color(255, 62, 74, 104);

	public static readonly Color NoticeVeil = new Color(160, 255, 122, 26);

	private static readonly Dictionary<string, Bordered> NinePatches = new Dictionary<string, Bordered>();

	public static IFont? Body => SaiyanHud.SmallFont;

	public static IFont? Strong => SaiyanHud.StrongFont;

	public static IGameTexture? Texture(string name)
	{
		return SaiyanHud.Texture(name);
	}

	public static void Draw(Intersect.Client.Framework.Gwen.Renderer.Base renderer, string name, int x, int y, Color color)
	{
		SaiyanHud.Draw(renderer, Texture(name), x, y, color);
	}

	public static void NinePatch(Intersect.Client.Framework.Gwen.Renderer.Base renderer, string name, int x, int y, int width, int height, int margin, Color color)
	{
		IGameTexture gameTexture = Texture(name);
		if (gameTexture != null && gameTexture.Width > 0 && gameTexture.Height > 0 && width > 0 && height > 0)
		{
			string key = $"{name}|{margin}";
			if (!NinePatches.TryGetValue(key, out var value))
			{
				value = new Bordered(gameTexture, 0f, 0f, gameTexture.Width, gameTexture.Height, new Margin(margin));
				NinePatches[key] = value;
			}
			value.Draw(renderer, new Rectangle(x, y, width, height), color);
		}
	}

	public static void Caps(Intersect.Client.Framework.Gwen.Renderer.Base renderer, string text, int x, int y, int height, Color color)
	{
		int num = SaiyanHud.SpacedWidth(renderer, Body, 9, text, 2);
		SaiyanHud.TextSpaced(renderer, Body, 9, text, x + num, y, height, 2, color);
	}

	public static int CapsWidth(Intersect.Client.Framework.Gwen.Renderer.Base renderer, string text)
	{
		return SaiyanHud.SpacedWidth(renderer, Body, 9, text, 2);
	}

	public static void Section(Intersect.Client.Framework.Gwen.Renderer.Base renderer, string text, int x, int y, int width)
	{
		Caps(renderer, text, x, y + 4, 18, Soft);
		int num = x + CapsWidth(renderer, text) + 10;
		SaiyanHud.Draw(renderer, Texture("saiyan_option_filet.png"), num, y + 13, Math.Max(0, x + width - num), 1, SectionLine);
	}

	public static void Veil(Intersect.Client.Framework.Gwen.Renderer.Base renderer, int x, int y, int width, int height)
	{
		SaiyanHud.Draw(renderer, Texture("saiyan_menu_survol.png"), x + 1, y + 1, width - 2, height - 2, Orange);
		renderer.DrawColor = Orange;
		renderer.DrawFilledRect(new Rectangle(x + 1, y + 5, 3, Math.Max(0, height - 10)));
	}

	public static void Rule(Intersect.Client.Framework.Gwen.Renderer.Base renderer, int x, int y, int width)
	{
		if (width > 0)
		{
			renderer.DrawColor = RuleColor;
			renderer.DrawFilledRect(new Rectangle(x, y, width, 1));
		}
	}

	public static bool IsHoveredWithin(Intersect.Client.Framework.Gwen.Control.Base control)
	{
		for (Intersect.Client.Framework.Gwen.Control.Base obj = InputHandler.HoveredControl; obj != null; obj = obj.Parent)
		{
			if (obj == control)
			{
				return true;
			}
		}
		return false;
	}
}
