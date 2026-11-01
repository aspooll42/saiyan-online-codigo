using System;
using System.Collections.Generic;
using System.Linq;
using Intersect.Client.Core.Controls;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.Framework.Gwen;
using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.Framework.Gwen.Input;
using Intersect.Client.Framework.Gwen.Renderer;
using Intersect.Client.Framework.Gwen.Skin;
using Intersect.Client.Framework.Gwen.Skin.Texturing;
using Intersect.Client.Framework.Input;
using Intersect.Client.Interface.Game.EntityPanel;
using Intersect.Client.Localization;

namespace Intersect.Client.Interface.Game;

public sealed class DropdownPanel : Intersect.Client.Framework.Gwen.Control.Base
{
	public const int SideMargin = 10;

	private const int LabelLeft = 50;

	private const int IconCenter = 25;

	private const int RuleHeight = 9;

	private const int SeparatorHeight = 16;

	private const int BottomPadding = 8;

	private const int InfoGap = 16;

	private const int KeyGap = 8;

	private const int ChipHeight = 18;

	private const int PillPadding = 5;

	private const int LabelSize = 11;

	private const int InfoSize = 10;

	private const int KeySize = 9;

	private const int TitleSize = 9;

	internal static readonly Color Orange = new Color(255, 255, 122, 26);

	internal static readonly Color OrangeLight = new Color(255, 255, 172, 82);

	private static readonly Color Gold = new Color(255, 245, 190, 90);

	private static readonly Color LabelInk = new Color(255, 223, 229, 242);

	internal static readonly Color SoftInk = new Color(255, 176, 186, 206);

	private static readonly Color UnavailableInk = new Color(255, 150, 160, 182);

	internal static readonly Color RuleColor = new Color(255, 42, 51, 73);

	private static readonly Color GoodPill = new Color(255, 30, 90, 55);

	private static readonly Color GoodInk = new Color(255, 140, 230, 170);

	private static readonly Color RefusalPill = new Color(255, 90, 24, 30);

	private static readonly Color RefusalInk = new Color(255, 255, 120, 110);

	private static readonly Color FadedIcon = new Color(102, 255, 255, 255);

	private static readonly Color GlyphInk = new Color(255, 176, 184, 200);

	private static readonly Color FadedGlyph = new Color(110, 176, 184, 200);

	private static readonly Color DangerVeil = new Color(255, 232, 64, 64);

	private static readonly Color SeparatorColor = new Color(200, 62, 74, 104);

	private static readonly Color PlateShadow = new Color(120, 0, 0, 0);

	private static Bordered? _plate;

	private static IGameTexture? _plateTexture;

	private readonly List<DropdownRow> _rows = new List<DropdownRow>();

	private readonly List<int> _tops = new List<int>();

	private int _keyColumn;

	public string? Title { get; set; }

	public int HeaderHeight { get; set; } = 6;

	public int RowHeight { get; set; } = 42;

	public int MinimumWidth { get; set; } = 240;

	public bool RowRules { get; set; }

	public Func<Intersect.Client.Framework.Gwen.Renderer.Base, int>? MeasureHeader { get; set; }

	public Action<Intersect.Client.Framework.Gwen.Renderer.Base, int>? DrawHeader { get; set; }

	private static IFont? KeyFont => SaiyanHud.StrongFont ?? SaiyanHud.SmallFont;

	public DropdownPanel(Intersect.Client.Framework.Gwen.Control.Base parent, string name)
		: base(parent, name)
	{
		base.MouseInputEnabled = true;
	}

	public void SetRows(IEnumerable<DropdownRow> rows)
	{
		_rows.Clear();
		_rows.AddRange(rows);
	}

	public Point Fit()
	{
		int num = MinimumWidth;
		_keyColumn = 0;
		Intersect.Client.Framework.Gwen.Renderer.Base obj = base.SafeSkin?.Renderer;
		if (obj != null)
		{
			foreach (DropdownRow row in _rows)
			{
				if (!string.IsNullOrEmpty(row.Key))
				{
					_keyColumn = Math.Max(_keyColumn, KeyWidth(obj, row.Key) + 8);
				}
			}
			foreach (DropdownRow row2 in _rows)
			{
				int num2 = 50 + SaiyanHud.Measure(obj, SaiyanHud.SmallFont, 11, row2.Label).X + _keyColumn + 10;
				if (!string.IsNullOrEmpty(row2.Info))
				{
					num2 += 16 + InfoWidth(obj, row2);
				}
				num = Math.Max(num, num2);
			}
			if (!string.IsNullOrEmpty(Title))
			{
				num = Math.Max(num, 12 + SaiyanHud.SpacedWidth(obj, SaiyanHud.StrongFont, 9, Title, 1) + 10);
			}
			if (MeasureHeader != null)
			{
				num = Math.Max(num, MeasureHeader(obj));
			}
		}
		_tops.Clear();
		int num3 = HeaderHeight;
		foreach (DropdownRow row3 in _rows)
		{
			if (row3.RuleAbove)
			{
				num3 += 9;
			}
			if (row3.SeparatorAbove)
			{
				num3 += 16;
			}
			_tops.Add(num3);
			num3 += RowHeight;
		}
		Point result = new Point(num, num3 + 8);
		if (base.Width != result.X || base.Height != result.Y)
		{
			SetSize(result.X, result.Y);
		}
		return result;
	}

	public override bool SizeToChildren(SizeToChildrenArgs args)
	{
		return false;
	}

	public static string? KeyOf(Control control)
	{
		IControlSet activeControls = Controls.ActiveControls;
		if (activeControls != null && activeControls.TryGetMappingFor(control, out ControlMapping mapping))
		{
			ControlBinding controlBinding = mapping.Bindings.FirstOrDefault((ControlBinding binding) => binding.Key != Keys.None);
			if (controlBinding != null)
			{
				return Strings.Keys.FormatKeyName(controlBinding.Modifier, controlBinding.Key);
			}
		}
		return null;
	}

	internal static void DrawPlate(Intersect.Client.Framework.Gwen.Renderer.Base renderer, int width, int height)
	{
		IGameTexture gameTexture = SaiyanHud.Texture("saiyan_menu_plaque.png");
		if (gameTexture != null && gameTexture.Width > 0 && gameTexture.Height > 0 && width > 0 && height > 0)
		{
			if (!_plate.HasValue || _plateTexture != gameTexture)
			{
				_plate = new Bordered(gameTexture, 0f, 0f, gameTexture.Width, gameTexture.Height, new Margin(10, 16, 10, 16));
				_plateTexture = gameTexture;
			}
			_plate.Value.Draw(renderer, new Rectangle(2, 4, width, height), PlateShadow);
			_plate.Value.Draw(renderer, new Rectangle(0, 0, width, height), SaiyanHud.White);
			SaiyanHud.Draw(renderer, SaiyanHud.Texture("saiyan_menu_filet.png"), 7, 0, width - 14, 2, Orange);
		}
	}

	internal static void DrawNotch(Intersect.Client.Framework.Gwen.Renderer.Base renderer, int centerX, int height)
	{
		IGameTexture gameTexture = SaiyanHud.Texture("saiyan_menu_pointe.png");
		if (gameTexture != null && gameTexture.Width > 0)
		{
			SaiyanHud.Draw(renderer, gameTexture, centerX - gameTexture.Width / 2, height - 1, SaiyanHud.White);
		}
	}

	protected override void Render(Intersect.Client.Framework.Gwen.Skin.Base skin)
	{
		Intersect.Client.Framework.Gwen.Renderer.Base renderer = skin.Renderer;
		int width = base.Width;
		if (!string.IsNullOrEmpty(Title))
		{
			int num = SaiyanHud.SpacedWidth(renderer, SaiyanHud.StrongFont, 9, Title, 1);
			SaiyanHud.TextSpaced(renderer, SaiyanHud.StrongFont, 9, Title, 12 + num, 2, 24, 1, Gold);
			DrawRule(renderer, 10, 26, width - 10);
		}
		DrawHeader?.Invoke(renderer, width);
		int num2 = HoveredRow();
		for (int i = 0; i < _rows.Count && i < _tops.Count; i++)
		{
			DropdownRow dropdownRow = _rows[i];
			int num3 = _tops[i];
			if (dropdownRow.RuleAbove)
			{
				DrawRule(renderer, 50, num3 - 9 + 4, width - 10);
			}
			if (dropdownRow.SeparatorAbove)
			{
				SaiyanHud.Draw(renderer, SaiyanHud.Texture("saiyan_menu_filet.png"), 16, num3 - 8, width - 20 - 12, 1, SeparatorColor);
			}
			else if (RowRules && i > 0 && !dropdownRow.RuleAbove)
			{
				DrawRule(renderer, 50, num3, width - 10);
			}
			DrawRow(renderer, dropdownRow, num3, width, i == num2);
		}
	}

	protected override void OnMouseClicked(MouseButton mouseButton, Point mousePosition, bool userAction = true)
	{
		base.OnMouseClicked(mouseButton, mousePosition, userAction);
		int num = RowAt(CanvasPosToLocal(mousePosition).Y);
		if (num >= 0 && num < _rows.Count)
		{
			DropdownRow dropdownRow = _rows[num];
			if (!dropdownRow.IsUnavailable)
			{
				base.Canvas?.CloseMenus();
				dropdownRow.Action?.Invoke();
			}
		}
	}

	private int HoveredRow()
	{
		if (!IsHovered)
		{
			return -1;
		}
		return RowAt(CanvasPosToLocal(InputHandler.MousePosition).Y);
	}

	private int RowAt(int y)
	{
		for (int i = 0; i < _tops.Count; i++)
		{
			if (y >= _tops[i] && y < _tops[i] + RowHeight)
			{
				return i;
			}
		}
		return -1;
	}

	private void DrawRow(Intersect.Client.Framework.Gwen.Renderer.Base renderer, DropdownRow row, int top, int width, bool hovered)
	{
		hovered &= !row.IsUnavailable;
		int num = top + RowHeight / 2;
		if (hovered)
		{
			Color color = (row.IsDanger ? DangerVeil : Orange);
			SaiyanHud.Draw(renderer, SaiyanHud.Texture("saiyan_menu_survol.png"), 0, top + 1, width, RowHeight - 2, color);
			renderer.DrawColor = color;
			renderer.DrawFilledRect(new Rectangle(0, top + 4, 3, RowHeight - 8));
		}
		SaiyanHud.Draw(renderer, SaiyanHud.Texture(hovered ? "saiyan_menu_case_survol.png" : "saiyan_menu_case.png"), 10, num - 15, SaiyanHud.White);
		if (!string.IsNullOrEmpty(row.Icon))
		{
			IGameTexture gameTexture = SaiyanHud.Texture(row.Icon);
			if (gameTexture != null && gameTexture.Width > 0 && gameTexture.Height > 0)
			{
				SaiyanHud.Draw(renderer, gameTexture, 25 - gameTexture.Width / 2, num - gameTexture.Height / 2, (!row.TintIcon) ? (row.IsUnavailable ? FadedIcon : SaiyanHud.White) : (row.IsUnavailable ? FadedGlyph : (row.IsDanger ? RefusalInk : (hovered ? OrangeLight : GlyphInk))));
			}
		}
		if (row.HasNews)
		{
			SaiyanHud.Draw(renderer, SaiyanHud.Texture("saiyan_menu_point.png"), 33, num - 19, SaiyanHud.White);
		}
		Color color2 = (row.IsUnavailable ? UnavailableInk : (row.IsDanger ? RefusalInk : (hovered ? OrangeLight : LabelInk)));
		SaiyanHud.TextLeft(renderer, SaiyanHud.SmallFont, 11, row.Label, 50, top, RowHeight, color2, shadow: false);
		int num2 = width - 10;
		if (!string.IsNullOrEmpty(row.Key))
		{
			DrawKey(renderer, row.Key, num2, num);
		}
		num2 -= _keyColumn;
		if (!string.IsNullOrEmpty(row.Info))
		{
			switch (row.InfoKind)
			{
			case DropdownInfoKind.Good:
				DrawPill(renderer, row.Info, num2, num, GoodPill, GoodInk);
				break;
			case DropdownInfoKind.Refusal:
				DrawPill(renderer, row.Info, num2, num, RefusalPill, RefusalInk);
				break;
			case DropdownInfoKind.Gold:
				SaiyanHud.TextRight(renderer, SaiyanHud.SmallFont, 10, row.Info, num2, top, RowHeight, Gold, shadow: false);
				break;
			default:
				SaiyanHud.TextRight(renderer, SaiyanHud.SmallFont, 10, row.Info, num2, top, RowHeight, SoftInk, shadow: false);
				break;
			}
		}
	}

	private static void DrawRule(Intersect.Client.Framework.Gwen.Renderer.Base renderer, int left, int y, int right)
	{
		if (right > left)
		{
			renderer.DrawColor = RuleColor;
			renderer.DrawFilledRect(new Rectangle(left, y, right - left, 1));
		}
	}

	private static int KeyWidth(Intersect.Client.Framework.Gwen.Renderer.Base renderer, string key)
	{
		return Math.Max(18, SaiyanHud.Measure(renderer, KeyFont, 9, key).X + 10);
	}

	private static int InfoWidth(Intersect.Client.Framework.Gwen.Renderer.Base renderer, DropdownRow row)
	{
		int x = SaiyanHud.Measure(renderer, SaiyanHud.SmallFont, 10, row.Info).X;
		DropdownInfoKind infoKind = row.InfoKind;
		if ((uint)(infoKind - 2) > 1u)
		{
			return x;
		}
		return Math.Max(18, x + 10);
	}

	internal static int DrawKey(Intersect.Client.Framework.Gwen.Renderer.Base renderer, string key, int right, int middle)
	{
		int num = KeyWidth(renderer, key);
		int x = right - num;
		int y = middle - 9;
		DrawThreeSlice(renderer, SaiyanHud.Texture("saiyan_menu_touche.png"), x, y, num, 18, 8, SaiyanHud.White);
		SaiyanHud.TextCenter(renderer, KeyFont, 9, key, x, y, num, 18, SoftInk, shadow: false);
		return num;
	}

	private static void DrawPill(Intersect.Client.Framework.Gwen.Renderer.Base renderer, string text, int right, int middle, Color fill, Color ink)
	{
		int num = Math.Max(18, SaiyanHud.Measure(renderer, SaiyanHud.SmallFont, 10, text).X + 10);
		int x = right - num;
		int y = middle - 9;
		DrawThreeSlice(renderer, SaiyanHud.Texture("saiyan_menu_pastille.png"), x, y, num, 18, 9, fill);
		SaiyanHud.TextCenter(renderer, SaiyanHud.SmallFont, 10, text, x, y, num, 18, ink, shadow: false);
	}

	private static void DrawThreeSlice(Intersect.Client.Framework.Gwen.Renderer.Base renderer, IGameTexture? texture, int x, int y, int width, int height, int cap, Color color)
	{
		if (texture != null && texture.Width > 0)
		{
			width = Math.Max(width, cap * 2);
			float num = (float)cap / (float)texture.Width;
			SaiyanHud.Draw(renderer, texture, x, y, cap, height, color, 0f, 0f, num);
			SaiyanHud.Draw(renderer, texture, x + cap, y, width - cap * 2, height, color, num, 0f, 1f - num);
			SaiyanHud.Draw(renderer, texture, x + width - cap, y, cap, height, color, 1f - num);
		}
	}
}
