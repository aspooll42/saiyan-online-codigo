using System;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.Framework.Gwen.Renderer;
using Intersect.Client.Framework.Gwen.Skin;
using Intersect.Client.Framework.Input;
using Intersect.Client.Interface.Game.EntityPanel;
using Intersect.Framework.Core;

namespace Intersect.Client.Interface.Game;

public sealed class MenuDockCase : Intersect.Client.Framework.Gwen.Control.Base
{
	public const int Side = 50;

	public new const int Height = 59;

	private const int KeyLine = 11;

	private const int Lift = 3;

	private const int StateGap = 3;

	private const int StateHeight = 3;

	private const int IconSide = 33;

	private const int KeySize = 9;

	private const int BadgeSize = 13;

	private static readonly Color Plate = new Color(255, 23, 31, 49);

	private static readonly Color PlateBottom = new Color(255, 17, 23, 37);

	private static readonly Color PlateHover = new Color(255, 30, 40, 62);

	private static readonly Color PlateOpen = new Color(255, 38, 50, 78);

	private static readonly Color Edge = new Color(255, 50, 64, 95);

	private static readonly Color EdgeHover = new Color(255, 51, 65, 95);

	private static readonly Color EdgeOpen = new Color(255, 74, 90, 128);

	private static readonly Color Shine = new Color(60, 255, 255, 255);

	private static readonly Color Orange = new Color(255, 255, 122, 26);

	private static readonly Color StateOff = new Color(255, 26, 33, 51);

	private static readonly Color OrangeLight = new Color(255, 255, 172, 82);

	private static readonly Color IconInk = new Color(255, 198, 208, 228);

	private static readonly Color IconInkHover = new Color(255, 238, 242, 250);

	private static readonly Color KeyInk = new Color(255, 95, 106, 130);

	private static readonly Color KeyInkHover = new Color(255, 143, 155, 180);

	private static readonly Color BadgeGoodFill = new Color(255, 14, 42, 28);

	private static readonly Color BadgeBadFill = new Color(255, 50, 15, 21);

	private static readonly Color Red = new Color(255, 255, 110, 124);

	private static readonly Color Green = new Color(255, 126, 226, 168);

	private float _lift;

	public string Label { get; set; } = string.Empty;

	public string Key { get; set; } = string.Empty;

	public string Icon { get; init; } = string.Empty;

	public bool IsOpen { get; set; }

	public bool IsDimmed { get; set; }

	public int Badge { get; set; }

	public bool BadgeIsGood { get; set; }

	public float DockAlpha { get; set; } = 1f;

	public Action? Action { get; init; }

	public int PlateTop => base.Y + 3;

	public MenuDockCase(Intersect.Client.Framework.Gwen.Control.Base parent, string name)
		: base(parent, name)
	{
		base.MouseInputEnabled = true;
		SetSize(50, 59);
	}

	public void Advance(float seconds)
	{
		int num = ((IsHovered && !IsDimmed) ? 3 : 0);
		float num2 = 1f - MathF.Exp((0f - seconds) * 18f);
		_lift += ((float)num - _lift) * num2;
	}

	protected override void Render(Intersect.Client.Framework.Gwen.Skin.Base skin)
	{
		Intersect.Client.Framework.Gwen.Renderer.Base renderer = skin.Renderer;
		bool flag = IsHovered && !IsDimmed;
		int num = 3 - (int)MathF.Round(_lift);
		Rectangle rectangle = new Rectangle(0, num, 50, 50);
		Color color = (IsOpen ? PlateOpen : (flag ? PlateHover : Plate));
		Fill(renderer, rectangle, color);
		Fill(renderer, new Rectangle(1, num + 25, 48, 24), SaiyanHud.WithAlpha(PlateBottom, 0.45f));
		Outline(renderer, rectangle, IsOpen ? EdgeOpen : (flag ? EdgeHover : Edge));
		Fill(renderer, new Rectangle(1, num + 1, 48, 1), Shine);
		DrawIcon(renderer, num, flag);
		if (!string.IsNullOrEmpty(Key))
		{
			Color color2 = (IsOpen ? OrangeLight : (flag ? KeyInkHover : KeyInk));
			SaiyanHud.TextCenter(renderer, SaiyanHud.SmallFont, 9, Key, 0, num + 50 - 11 - 2, 50, 11, A(color2), shadow: false);
		}
		Fill(renderer, new Rectangle(0, 56, 50, 3), IsOpen ? Orange : StateOff);
		DrawBadge(renderer, num);
	}

	private void DrawIcon(Intersect.Client.Framework.Gwen.Renderer.Base renderer, int top, bool hovered)
	{
		IGameTexture gameTexture = SaiyanHud.Texture("dock_" + Icon + ".png");
		bool flag = gameTexture != null && gameTexture.Width > 0 && gameTexture.Height > 0;
		if (!flag)
		{
			string text = (IsOpen ? "_down" : (hovered ? "_hover" : string.Empty));
			gameTexture = SaiyanHud.Texture("hud_" + Icon + text + ".png") ?? SaiyanHud.Texture("hud_" + Icon + ".png");
		}
		if (gameTexture == null || gameTexture.Width <= 0 || gameTexture.Height <= 0)
		{
			DrawBars(renderer, top, hovered);
			return;
		}
		int num = Math.Min(33, 37);
		int x = (50 - num) / 2;
		int y = top + 3;
		Color color = ((!flag) ? SaiyanHud.White : (IsOpen ? OrangeLight : (hovered ? IconInkHover : IconInk)));
		SaiyanHud.Draw(renderer, gameTexture, x, y, num, num, A(color));
	}

	private void DrawBars(Intersect.Client.Framework.Gwen.Renderer.Base renderer, int top, bool hovered)
	{
		Color color = (IsOpen ? OrangeLight : (hovered ? IconInkHover : IconInk));
		int num = 20;
		int x = (50 - num) / 2;
		int num2 = top + 19 - 6;
		for (int i = 0; i < 3; i++)
		{
			Fill(renderer, new Rectangle(x, num2 + i * 5, num, 2), color);
		}
	}

	private void DrawBadge(Intersect.Client.Framework.Gwen.Renderer.Base renderer, int top)
	{
		if (Badge != 0)
		{
			if (Badge < 0)
			{
				float value = 0.72f + 0.28f * MathF.Sin((float)Timing.Global.Milliseconds / 260f);
				Rectangle rectangle = new Rectangle(40, top + 3, 7, 7);
				Fill(renderer, rectangle, Tint(Orange, value));
				Outline(renderer, rectangle, new Color(255, 7, 10, 18));
			}
			else
			{
				Color color = (BadgeIsGood ? Green : Red);
				string text = ((Badge > 9) ? "9+" : Badge.ToString());
				int num = Math.Max(13, SaiyanHud.Measure(renderer, SaiyanHud.SmallFont, 9, text).X + 7);
				Rectangle rectangle2 = new Rectangle(50 - num - 2, top + 2, num, 13);
				Fill(renderer, rectangle2, BadgeIsGood ? BadgeGoodFill : BadgeBadFill);
				Outline(renderer, rectangle2, color);
				SaiyanHud.TextCenter(renderer, SaiyanHud.SmallFont, 9, text, rectangle2.X, rectangle2.Y - 1, rectangle2.Width, rectangle2.Height, A(color), shadow: false);
			}
		}
	}

	protected override void OnMouseClicked(MouseButton mouseButton, Point mousePosition, bool userAction = true)
	{
		base.OnMouseClicked(mouseButton, mousePosition, userAction);
		if (mouseButton == MouseButton.Left && !IsDimmed)
		{
			Action?.Invoke();
		}
	}

	private Color A(Color color)
	{
		return SaiyanHud.WithAlpha(color, DockAlpha);
	}

	private static Color Tint(Color color, float value)
	{
		return new Color(color.A, (int)((float)(int)color.R * value), (int)((float)(int)color.G * value), (int)((float)(int)color.B * value));
	}

	private void Fill(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Rectangle rectangle, Color color)
	{
		renderer.DrawColor = A(color);
		renderer.DrawFilledRect(rectangle);
	}

	private void Outline(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Rectangle rectangle, Color color)
	{
		renderer.DrawColor = A(color);
		renderer.DrawFilledRect(new Rectangle(rectangle.X, rectangle.Y, rectangle.Width, 1));
		renderer.DrawFilledRect(new Rectangle(rectangle.X, rectangle.Bottom - 1, rectangle.Width, 1));
		renderer.DrawFilledRect(new Rectangle(rectangle.X, rectangle.Y + 1, 1, rectangle.Height - 2));
		renderer.DrawFilledRect(new Rectangle(rectangle.Right - 1, rectangle.Y + 1, 1, rectangle.Height - 2));
	}
}
