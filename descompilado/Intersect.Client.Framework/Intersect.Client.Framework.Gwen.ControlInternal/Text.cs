using System;
using System.Collections.Generic;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.Framework.Gwen.Control.EventArguments;
using Intersect.Client.Framework.Gwen.Skin;

namespace Intersect.Client.Framework.Gwen.ControlInternal;

public class Text : Intersect.Client.Framework.Gwen.Control.Base
{
	public static readonly Color DefaultBoundsOutlineColor = Intersect.Color.FromHex("bf4060", Intersect.Color.Pink);

	public static readonly Color DefaultMarginOutlineColor = Intersect.Color.FromHex("1fad1f", Intersect.Color.ForestGreen);

	private IFont? _font;

	private float _scale = 1f;

	private string? _displayedText;

	private string[] _lines = Array.Empty<string>();

	private WrappingBehavior _wrappingBehavior;

	private int _lastParentInnerWidth;

	private bool _recalculateLines;

	private int _fontSize = 10;

	public IFont? Font
	{
		get
		{
			return _font;
		}
		set
		{
			SetAndDoIfChanged(ref _font, value, Intersect.Client.Framework.Gwen.Control.Base.Invalidate);
		}
	}

	public int FontSize
	{
		get
		{
			return _fontSize;
		}
		set
		{
			SetAndDoIfChanged(ref _fontSize, value, Intersect.Client.Framework.Gwen.Control.Base.Invalidate);
		}
	}

	public string? DisplayedText
	{
		get
		{
			return _displayedText;
		}
		set
		{
			Intersect.Client.Framework.Gwen.Control.Base.SetAndDoIfChanged(ref _displayedText, value, Intersect.Client.Framework.Gwen.Control.Base.Invalidate, this);
		}
	}

	public WrappingBehavior WrappingBehavior
	{
		get
		{
			return _wrappingBehavior;
		}
		set
		{
			SetAndDoIfChanged(ref _wrappingBehavior, value, Intersect.Client.Framework.Gwen.Control.Base.Invalidate);
		}
	}

	public Color? Color { get; set; }

	public int Length => _displayedText?.Length ?? 0;

	public Color? ColorOverride { get; set; }

	public Text(Intersect.Client.Framework.Gwen.Control.Base parent, string? name = null)
		: base(parent, name)
	{
		Intersect.Client.Framework.Gwen.Skin.Base safeSkin = base.SafeSkin;
		if (safeSkin != null)
		{
			InitializeFromSkin(this);
			Color = safeSkin.Colors.Label.Normal;
		}
		else
		{
			PreLayout.Enqueue(InitializeFromSkin, this);
		}
		base.MouseInputEnabled = false;
		ColorOverride = Intersect.Color.FromArgb(0, 255, 255, 255);
		base.BoundsOutlineColor = DefaultBoundsOutlineColor;
		base.MarginOutlineColor = DefaultMarginOutlineColor;
		_lastParentInnerWidth = SelectWidthFrom(parent);
		parent.SizeChanged += ParentOnSizeChanged;
	}

	private static void InitializeFromSkin(Text @this)
	{
		@this.Color = @this.Skin.Colors.Label.Normal;
	}

	private void ParentOnSizeChanged(Intersect.Client.Framework.Gwen.Control.Base @base, ValueChangedEventArgs<Point> eventArgs)
	{
		int num = SelectWidthFrom(@base);
		if (_lastParentInnerWidth != num)
		{
			RecalculateLines(num);
		}
	}

	public override void Invalidate()
	{
		base.Invalidate();
		_recalculateLines = true;
	}

	private static int SelectWidthFrom(Intersect.Client.Framework.Gwen.Control.Base? parent)
	{
		if (parent != null)
		{
			return Math.Max(0, Math.Max(parent.MaximumInnerWidth, parent.InnerWidth));
		}
		return 0;
	}

	private void RecalculateLines(int parentInnerWidth = 0)
	{
		if (_lines.Length != 0 || !string.IsNullOrEmpty(_displayedText))
		{
			Intersect.Client.Framework.Gwen.Control.Base parent = base.Parent;
			if (parentInnerWidth < 1)
			{
				parentInnerWidth = SelectWidthFrom(parent);
			}
			if (_lines.Length != 1 || (parentInnerWidth < _lastParentInnerWidth && parentInnerWidth >= 1) || parentInnerWidth <= base.Width)
			{
				_lastParentInnerWidth = parentInnerWidth;
				WrappingBehavior wrappingBehavior = (parent as Label)?.WrappingBehavior ?? WrappingBehavior.NoWrap;
				_lines = wrappingBehavior switch
				{
					WrappingBehavior.Wrapped => WrapText(_displayedText, parentInnerWidth, Font, FontSize, base.Skin.Renderer), 
					WrappingBehavior.NoWrap => (_displayedText == null) ? Array.Empty<string>() : new string[1] { _displayedText }, 
					_ => throw new NotImplementedException($"{"WrappingBehavior"} '{wrappingBehavior}' not implemented"), 
				};
				SizeToContents(skipRecalculateLines: true);
			}
		}
	}

	protected override void Render(Intersect.Client.Framework.Gwen.Skin.Base skin)
	{
		if (string.IsNullOrEmpty(_displayedText))
		{
			return;
		}
		IFont font = _font ?? base.SafeSkin?.DefaultFont;
		if (font == null)
		{
			return;
		}
		Color drawColor = Color ?? Intersect.Color.White;
		Color colorOverride = ColorOverride;
		if (colorOverride == null || colorOverride.A == 0)
		{
			skin.Renderer.DrawColor = drawColor;
		}
		else
		{
			skin.Renderer.DrawColor = colorOverride;
		}
		Point position = default(Point);
		string[] lines = _lines;
		foreach (string text in lines)
		{
			skin.Renderer.RenderText(font, _fontSize, position, text, _scale);
			if (_lines.Length > 1)
			{
				position.Y += skin.Renderer.MeasureText(font, _fontSize, text, _scale).Y;
				continue;
			}
			break;
		}
	}

	protected override void Layout(Intersect.Client.Framework.Gwen.Skin.Base skin)
	{
		if (_recalculateLines || _lines.Length < 1)
		{
			_lines = Array.Empty<string>();
			RecalculateLines();
			_recalculateLines = false;
		}
		SizeToContents();
		base.Layout(skin);
	}

	public override bool SizeToChildren(SizeToChildrenArgs args)
	{
		return SizeToContents();
	}

	protected override void OnScaleChanged()
	{
		Invalidate();
	}

	public void SetScale(float scale)
	{
		_scale = scale;
		OnScaleChanged();
	}

	public float GetScale()
	{
		return _scale;
	}

	private bool SizeToContents(bool skipRecalculateLines = false)
	{
		if (!base.HasSkin)
		{
			return false;
		}
		if (base.IsHidden)
		{
			return false;
		}
		IFont font = Font;
		if (font == null)
		{
			return false;
		}
		if (!skipRecalculateLines)
		{
			RecalculateLines();
		}
		Point point2;
		if (string.IsNullOrEmpty(_displayedText))
		{
			Point point = base.Skin.Renderer.MeasureText(font, _fontSize, "|", _scale);
			point.X = 0;
			point2 = point;
		}
		else
		{
			point2 = default(Point);
			string[] lines = _lines;
			foreach (string text in lines)
			{
				Point point3 = base.Skin.Renderer.MeasureText(font, _fontSize, text, _scale);
				point2 = new Point(Math.Max(point2.X, point3.X), point2.Y + point3.Y);
			}
		}
		point2.X = Math.Clamp(point2.X, base.MinimumSize.X, (base.MaximumSize.X > 0) ? base.MaximumSize.X : int.MaxValue);
		point2.Y = Math.Clamp(point2.Y, base.MinimumSize.Y, (base.MaximumSize.Y > 0) ? base.MaximumSize.Y : int.MaxValue);
		if (base.Size == point2)
		{
			return false;
		}
		base.Size = point2;
		Invalidate();
		return true;
	}

	public Point GetCharacterPosition(int index)
	{
		if (index < 1)
		{
			return default(Point);
		}
		IFont font = _font ?? base.SafeSkin?.DefaultFont;
		if (font == null)
		{
			return default(Point);
		}
		string displayedText = _displayedText;
		if (string.IsNullOrEmpty(displayedText))
		{
			return default(Point);
		}
		if (index > displayedText.Length)
		{
			return default(Point);
		}
		string text = displayedText.Substring(0, index);
		Point point = base.Skin.Renderer.MeasureText(font, _fontSize, text, _scale);
		point.Y = 0;
		Point result = point;
		if (result.X <= 0 && text.Length > 0)
		{
			int num = base.Skin.Renderer.MeasureText(font, _fontSize, "n", _scale).X;
			if (num <= 0)
			{
				num = (int)((float)_fontSize * 0.6f);
			}
			int num2 = (int)((float)num * 0.4f);
			int num3 = 0;
			for (int i = 0; i < text.Length; i++)
			{
				string text2 = text[i].ToString();
				Point point2 = base.Skin.Renderer.MeasureText(font, _fontSize, text2, _scale);
				num3 += ((point2.X > 0) ? point2.X : num2);
			}
			return new Point(num3, 0);
		}
		return result;
	}

	public int GetClosestCharacter(Point point)
	{
		string displayedText = _displayedText;
		if (string.IsNullOrEmpty(displayedText))
		{
			return -1;
		}
		int num = int.MaxValue;
		int result = 0;
		for (int i = 0; i < displayedText.Length + 1; i++)
		{
			int num2 = Math.Abs(GetCharacterPosition(i).X - point.X);
			if (num2 <= num)
			{
				num = num2;
				result = i;
			}
		}
		return result;
	}

	public static string[] WrapText(string? input, int width, IFont? font, int size, ITextHelper textHelper)
	{
		string text = input?.ReplaceLineEndings("\n");
		string[] array = (text ?? string.Empty).Split('\n', StringSplitOptions.TrimEntries);
		if (string.IsNullOrWhiteSpace(text) || width < 1)
		{
			return array;
		}
		if (font == null)
		{
			return array;
		}
		List<string> list = new List<string>();
		string[] array2 = array;
		foreach (string text2 in array2)
		{
			if (text2.Length < 1)
			{
				list.Add(text2);
				continue;
			}
			float x = textHelper.MeasureText(text2, font, size, 1f).X;
			if (x <= (float)width)
			{
				list.Add(text2);
				continue;
			}
			text2.Split(". ");
			int num = 0;
			int num2 = 0;
			int j = 1;
			int num3 = 0;
			for (; num2 + j < text2.Length; j++)
			{
				string text3 = text2.Substring(num2, j);
				x = textHelper.MeasureText(text3, font, size, 1f).X;
				if (x < (float)width)
				{
					num3 = num;
					char c = text2[num2 + j];
					int num4 = ((c != ' ' && c != '-') ? num : j);
					num = num4;
					continue;
				}
				if (num3 == 0)
				{
					num3 = j - 1;
				}
				text3 = text2.Substring(num2, num3).Trim();
				list.Add(text3);
				num2 += num3;
				num3 = 0;
				num = 0;
				j = 1;
			}
			string text4 = text2.Substring(num2, text2.Length - num2).Trim();
			if (text4.Length > 1 && textHelper.MeasureText(text4, font, size, 1f).X > (float)width)
			{
				int num5 = text4.LastIndexOfAny(new char[2] { ' ', '-' });
				if (num5 > 0)
				{
					list.Add(text4.Substring(0, num5).Trim());
					string text5 = text4;
					int num4 = num5;
					text4 = text5.Substring(num4, text5.Length - num4).Trim();
				}
			}
			list.Add(text4);
		}
		return list.ToArray();
	}
}
