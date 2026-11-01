using System;
using System.Collections.Generic;
using System.Linq;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.Framework.Gwen.Input;
using Intersect.Client.Framework.Gwen.Renderer;
using Intersect.Client.Framework.Gwen.Skin;
using Intersect.Client.Framework.Input;
using Intersect.Client.Interface.Game.EntityPanel;

namespace Intersect.Client.Interface.Shared;

internal sealed class OptionsSheet : Intersect.Client.Framework.Gwen.Control.Base
{
	public const int SideMargin = 16;

	public const int ChipWidth = 124;

	public const int ChipGap = 10;

	public const int PairNameFromRight = 118;

	public const int PairBarFromRight = 44;

	private const int ToggleWidth = 42;

	private const int ToggleHeight = 22;

	private const int ChipHeight = 28;

	private const int SegmentHeight = 30;

	public const int SegmentsLine = 36;

	public const int KnobSize = 20;

	public const int ValueWidth = 44;

	private readonly List<OptionRow> _rows = new List<OptionRow>();

	public IReadOnlyList<OptionRow> Rows => _rows;

	public OptionRow? HoveredRow { get; private set; }

	public OptionRow? LastHoveredRow { get; private set; }

	public Func<Control, int, (string Text, bool Empty)>? KeyLabel { get; set; }

	public Func<Control, int, bool>? IsListening { get; set; }

	public Action<Control, int>? KeyPicked { get; set; }

	public OptionsSheet(Intersect.Client.Framework.Gwen.Control.Base parent, string name)
		: base(parent, name)
	{
		base.MouseInputEnabled = true;
	}

	public void SetRows(IEnumerable<OptionRow> rows, int width, int top = 4)
	{
		_rows.Clear();
		_rows.AddRange(rows);
		int num = top;
		for (int i = 0; i < _rows.Count; i++)
		{
			OptionRow optionRow = _rows[i];
			optionRow.Top = num;
			num += optionRow.Height;
			OptionRow optionRow2 = ((i + 1 < _rows.Count) ? _rows[i + 1] : null);
			OptionRow optionRow3 = optionRow;
			OptionRowKind kind = optionRow.Kind;
			bool flag = ((kind == OptionRowKind.Section || kind == OptionRowKind.PairHeader) ? true : false);
			optionRow3.RuleBelow = !flag && optionRow2 != null && optionRow2.Kind != OptionRowKind.Section;
		}
		HoveredRow = null;
		LastHoveredRow = _rows.FirstOrDefault((OptionRow row) => row.Kind == OptionRowKind.Pair);
		SetSize(width, num + 4);
		PlaceWidgets();
	}

	public static int MeasureHeight(IEnumerable<OptionRow> rows, int top = 4)
	{
		return top + rows.Sum((OptionRow row) => row.Height) + 4;
	}

	public override bool SizeToChildren(SizeToChildrenArgs args)
	{
		return false;
	}

	public void PlaceWidgets()
	{
		int num = base.Width - 16;
		foreach (OptionRow row in _rows)
		{
			Intersect.Client.Framework.Gwen.Control.Base widget = row.Widget;
			if (widget != null)
			{
				bool isHidden = row.Unavailable?.Invoke() != null;
				widget.IsHidden = isHidden;
				OptionRowKind kind = row.Kind;
				if (kind == OptionRowKind.Slider)
				{
					widget.SetSize(row.WidgetWidth, 42);
					widget.SetPosition(num - 44 - 12 - row.WidgetWidth, row.Top + row.Height / 2 - 10);
				}
				else
				{
					widget.SetSize(row.WidgetWidth, 30);
					widget.SetPosition(num - row.WidgetWidth, row.Top + (row.Height - 30) / 2);
				}
			}
		}
	}

	public override void Think()
	{
		base.Think();
		HoveredRow = null;
		if (OptionsStyle.IsHoveredWithin(this))
		{
			HoveredRow = RowAt(CanvasPosToLocal(InputHandler.MousePosition).Y);
			OptionRow hoveredRow = HoveredRow;
			if (hoveredRow != null && hoveredRow.Kind == OptionRowKind.Pair)
			{
				LastHoveredRow = HoveredRow;
			}
		}
	}

	protected override void Render(Intersect.Client.Framework.Gwen.Skin.Base skin)
	{
		Intersect.Client.Framework.Gwen.Renderer.Base renderer = skin.Renderer;
		int width = base.Width;
		int num = width - 16;
		Point point = CanvasPosToLocal(InputHandler.MousePosition);
		foreach (OptionRow row in _rows)
		{
			switch (row.Kind)
			{
			case OptionRowKind.Section:
				OptionsStyle.Section(renderer, row.Label, 16, row.Top + 4, width - 32);
				continue;
			case OptionRowKind.PairHeader:
				DrawCentered(renderer, row.Label, num - 118, row.Top, row.Height);
				DrawCentered(renderer, row.Help ?? string.Empty, num - 44, row.Top, row.Height);
				continue;
			}
			string text = row.Unavailable?.Invoke();
			bool flag = text != null;
			bool flag2 = row == HoveredRow && !flag;
			DrawChrome(renderer, row, width, flag2, text);
			int middle = (HelpBelow(row) ? (row.Top + 18) : (row.Top + row.Height / 2));
			switch (row.Kind)
			{
			case OptionRowKind.Toggle:
				DrawToggle(renderer, num - 42, middle, row.Get?.Invoke() ?? false, flag2, flag);
				break;
			case OptionRowKind.Segments:
				DrawSegments(renderer, row, num, middle, (flag2 && OnSegmentsLine(row, point.Y)) ? point.X : int.MinValue);
				break;
			case OptionRowKind.Slider:
				DrawSlider(renderer, row, num, middle, flag);
				break;
			case OptionRowKind.Keys:
				DrawKeys(renderer, row, num, middle, flag2 ? point.X : int.MinValue);
				break;
			case OptionRowKind.Pair:
			{
				bool hovered = flag2 && Math.Abs(point.X - (num - 118)) <= 30;
				bool hovered2 = flag2 && Math.Abs(point.X - (num - 44)) <= 30;
				DrawToggle(renderer, num - 118 - 21, middle, row.Get?.Invoke() ?? false, hovered, unavailable: false);
				DrawToggle(renderer, num - 44 - 21, middle, row.GetSecond?.Invoke() ?? false, hovered2, unavailable: false);
				break;
			}
			}
			if (row.RuleBelow)
			{
				OptionsStyle.Rule(renderer, 16, row.Top + row.Height, width - 32);
			}
		}
	}

	protected override void OnMouseClicked(MouseButton mouseButton, Point mousePosition, bool userAction = true)
	{
		base.OnMouseClicked(mouseButton, mousePosition, userAction);
		Point point = CanvasPosToLocal(mousePosition);
		OptionRow optionRow = RowAt(point.Y);
		if (optionRow == null || optionRow.Unavailable?.Invoke() != null)
		{
			return;
		}
		int num = base.Width - 16;
		switch (optionRow.Kind)
		{
		case OptionRowKind.Toggle:
		{
			Action<bool>? set2 = optionRow.Set;
			if (set2 != null)
			{
				Func<bool>? get2 = optionRow.Get;
				set2(get2 == null || !get2());
			}
			break;
		}
		case OptionRowKind.Segments:
		{
			int num2 = (OnSegmentsLine(optionRow, point.Y) ? SegmentAt(optionRow, num, point.X) : (-1));
			if (num2 >= 0)
			{
				optionRow.SetChoice?.Invoke(num2);
			}
			break;
		}
		case OptionRowKind.Keys:
		{
			int num3 = ((point.X >= num - 124) ? 1 : ((point.X < num - 248 - 10 || point.X >= num - 124 - 10) ? (-1) : 0));
			if (num3 >= 0)
			{
				KeyPicked?.Invoke(optionRow.KeyControl, num3);
			}
			break;
		}
		case OptionRowKind.Pair:
			if (Math.Abs(point.X - (num - 118)) <= 30)
			{
				Action<bool>? set = optionRow.Set;
				if (set != null)
				{
					Func<bool>? get = optionRow.Get;
					set(get == null || !get());
				}
			}
			else if (Math.Abs(point.X - (num - 44)) <= 30)
			{
				Action<bool>? setSecond = optionRow.SetSecond;
				if (setSecond != null)
				{
					Func<bool>? getSecond = optionRow.GetSecond;
					setSecond(getSecond == null || !getSecond());
				}
			}
			break;
		case OptionRowKind.Slider:
		case OptionRowKind.Combo:
			break;
		}
	}

	private OptionRow? RowAt(int y)
	{
		return _rows.FirstOrDefault(delegate(OptionRow row)
		{
			OptionRowKind kind = row.Kind;
			bool flag = ((kind == OptionRowKind.Section || kind == OptionRowKind.PairHeader) ? true : false);
			return !flag && y >= row.Top && y < row.Top + row.Height;
		});
	}

	private static bool HelpBelow(OptionRow row)
	{
		if (row.Kind == OptionRowKind.Segments && !string.IsNullOrEmpty(row.Help))
		{
			return row.Height >= 52;
		}
		return false;
	}

	private static bool OnSegmentsLine(OptionRow row, int y)
	{
		if (HelpBelow(row))
		{
			return y < row.Top + 36;
		}
		return true;
	}

	private void DrawChrome(Intersect.Client.Framework.Gwen.Renderer.Base renderer, OptionRow row, int width, bool hovered, string? reason)
	{
		int top = row.Top;
		int height = row.Height;
		if (hovered && row.Kind != OptionRowKind.Slider)
		{
			OptionsStyle.Veil(renderer, 0, top, width, height);
		}
		int num = 16;
		Color dot = row.Dot;
		if ((object)dot != null)
		{
			OptionsStyle.Draw(renderer, "saiyan_option_point.png", num + 2, top + height / 2 - 4, dot);
			num += 18;
		}
		Color color = ((reason != null) ? OptionsStyle.Dim : ((hovered && row.Kind != OptionRowKind.Slider) ? OptionsStyle.OrangeLight : OptionsStyle.Label));
		string text = reason ?? row.Help;
		int num2;
		if (HelpBelow(row))
		{
			SaiyanHud.TextLeft(renderer, OptionsStyle.Body, 11, row.Label, num, top, 36, color, shadow: false);
			SaiyanHud.TextLeft(renderer, OptionsStyle.Body, 10, text ?? string.Empty, num, top + 36 - 3, 18, OptionsStyle.Dim, shadow: false);
			num2 = top + 18;
		}
		else if (!string.IsNullOrEmpty(text))
		{
			SaiyanHud.TextLeft(renderer, OptionsStyle.Body, 11, row.Label, num, top + 3, 21, color, shadow: false);
			SaiyanHud.TextLeft(renderer, OptionsStyle.Body, 10, text, num, top + 22, 18, OptionsStyle.Dim, shadow: false);
			num2 = top + 13;
		}
		else
		{
			SaiyanHud.TextLeft(renderer, OptionsStyle.Body, 11, row.Label, num, top, height, color, shadow: false);
			num2 = top + height / 2;
		}
		if (row.AppliesOnConfirm)
		{
			int x = SaiyanHud.Measure(renderer, OptionsStyle.Body, 11, row.Label).X;
			OptionsStyle.Draw(renderer, "saiyan_option_relance.png", num + x + 8, num2 - 8, OptionsStyle.Glyph);
		}
	}

	private static void DrawToggle(Intersect.Client.Framework.Gwen.Renderer.Base renderer, int x, int middle, bool on, bool hovered, bool unavailable)
	{
		string name = (unavailable ? "saiyan_option_bascule_eteint.png" : ((!on) ? (hovered ? "saiyan_option_bascule_survol.png" : "saiyan_option_bascule.png") : (hovered ? "saiyan_option_bascule_actif_survol.png" : "saiyan_option_bascule_actif.png")));
		OptionsStyle.Draw(renderer, name, x, middle - 11, SaiyanHud.White);
	}

	private static int[] SegmentWidths(Intersect.Client.Framework.Gwen.Renderer.Base? renderer, OptionRow row)
	{
		return row.Choices.Select((string choice) => Math.Max(34, ((renderer == null) ? (choice.Length * 7) : SaiyanHud.Measure(renderer, OptionsStyle.Body, 10, choice).X) + 18)).ToArray();
	}

	private int SegmentAt(OptionRow row, int right, int x)
	{
		int[] array = SegmentWidths(base.SafeSkin?.Renderer, row);
		int num = right - array.Sum() - 6 + 3;
		for (int i = 0; i < array.Length; i++)
		{
			if (x >= num && x < num + array[i])
			{
				return i;
			}
			num += array[i];
		}
		return -1;
	}

	private static void DrawSegments(Intersect.Client.Framework.Gwen.Renderer.Base renderer, OptionRow row, int right, int middle, int mouseX)
	{
		int[] array = SegmentWidths(renderer, row);
		int num = array.Sum();
		int num2 = right - num - 6;
		int num3 = middle - 15;
		OptionsStyle.NinePatch(renderer, "saiyan_option_segments.png", num2, num3, num + 6, 30, 8, SaiyanHud.White);
		int num4 = row.GetChoice?.Invoke() ?? (-1);
		int num5 = num2 + 3;
		for (int i = 0; i < array.Length; i++)
		{
			bool flag = mouseX >= num5 && mouseX < num5 + array[i];
			if (i == num4)
			{
				OptionsStyle.NinePatch(renderer, "saiyan_option_segment_actif.png", num5, num3 + 3, array[i], 24, 8, SaiyanHud.White);
			}
			else if (flag)
			{
				OptionsStyle.NinePatch(renderer, "saiyan_option_segment_survol.png", num5, num3 + 3, array[i], 24, 8, SaiyanHud.White);
			}
			else if (i > 0 && i != num4 + 1)
			{
				renderer.DrawColor = new Color(120, 62, 74, 104);
				renderer.DrawFilledRect(new Rectangle(num5, num3 + 9, 1, 12));
			}
			Color color = ((i == num4) ? OptionsStyle.GoldLight : (flag ? OptionsStyle.OrangeLight : OptionsStyle.Glyph));
			SaiyanHud.TextCenter(renderer, OptionsStyle.Body, 10, row.Choices[i], num5, num3, array[i], 30, color, shadow: false);
			num5 += array[i];
		}
	}

	private static void DrawSlider(Intersect.Client.Framework.Gwen.Renderer.Base renderer, OptionRow row, int right, int middle, bool unavailable)
	{
		if (unavailable || !(row.Widget is Slider { X: var x, Width: var width } slider))
		{
			return;
		}
		double num = slider.Maximum - slider.Minimum;
		double num2 = ((num <= 0.0) ? 0.0 : Math.Clamp((slider.Value - slider.Minimum) / num, 0.0, 1.0));
		OptionsStyle.NinePatch(renderer, "saiyan_bar_track.png", x, middle - 5, width, 10, 5, SaiyanHud.White);
		int num3 = 10 + (int)Math.Round((double)(width - 20) * num2);
		OptionsStyle.NinePatch(renderer, "saiyan_bar_fill.png", x, middle - 5, Math.Max(10, num3 + 4), 10, 5, SaiyanHud.White);
		double[] array = row.Notches?.Invoke();
		if (array != null && array.Length > 1 && num > 0.0)
		{
			renderer.DrawColor = new Color(90, 255, 255, 255);
			double[] array2 = array;
			foreach (double num4 in array2)
			{
				int x2 = x + 10 + (int)Math.Round((double)(width - 20) * Math.Clamp((num4 - slider.Minimum) / num, 0.0, 1.0));
				renderer.DrawFilledRect(new Rectangle(x2, middle + 9, 1, 4));
			}
		}
		string text = row.Value?.Invoke();
		if (text != null)
		{
			SaiyanHud.TextRight(renderer, OptionsStyle.Strong, 13, text, right, middle - 14, 28, OptionsStyle.GoldLight, shadow: false);
		}
	}

	private void DrawKeys(Intersect.Client.Framework.Gwen.Renderer.Base renderer, OptionRow row, int right, int middle, int mouseX)
	{
		for (int i = 0; i < 2; i++)
		{
			int num = ((i == 1) ? (right - 124) : (right - 248 - 10));
			bool flag = IsListening?.Invoke(row.KeyControl, i) ?? false;
			(string Text, bool Empty) obj = KeyLabel?.Invoke(row.KeyControl, i) ?? (string.Empty, true);
			string item = obj.Text;
			bool item2 = obj.Empty;
			bool flag2 = mouseX >= num && mouseX < num + 124;
			string name = (flag ? "saiyan_option_touche_ecoute.png" : (item2 ? (flag2 ? "saiyan_option_touche_vide_survol.png" : "saiyan_option_touche_vide.png") : (flag2 ? "saiyan_option_touche_survol.png" : "saiyan_option_touche.png")));
			OptionsStyle.Draw(renderer, name, num, middle - 14, SaiyanHud.White);
			Color color = (flag ? OptionsStyle.GoldLight : (item2 ? OptionsStyle.Dim : SaiyanHud.White));
			SaiyanHud.TextCenter(renderer, OptionsStyle.Body, 10, item, num, middle - 14 - 1, 124, 28, color, shadow: false);
		}
	}

	private static void DrawCentered(Intersect.Client.Framework.Gwen.Renderer.Base renderer, string text, int center, int top, int height)
	{
		int num = OptionsStyle.CapsWidth(renderer, text);
		OptionsStyle.Caps(renderer, text, center - num / 2, top, height, OptionsStyle.Dim);
	}
}
