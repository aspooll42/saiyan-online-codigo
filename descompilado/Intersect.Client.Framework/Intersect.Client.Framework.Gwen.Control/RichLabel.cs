using System;
using System.Collections.Generic;
using System.Linq;
using Intersect.Client.Framework.File_Management;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.Framework.Gwen.Skin;
using Intersect.Client.Utilities;
using Intersect.Core;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace Intersect.Client.Framework.Gwen.Control;

public class RichLabel : Base
{
	protected class TextBlock
	{
		public BlockType Type;

		public string Text;

		public Color? Color;

		public IFont? Font;

		public int FontSize;

		public Alignments Alignment;

		public readonly List<Label> Labels = new List<Label>();
	}

	protected enum BlockType
	{
		Text,
		NewLine
	}

	private readonly string[] _newlines = new string[2]
	{
		Environment.NewLine,
		"\n"
	}.Distinct().ToArray();

	private readonly List<TextBlock> _textBlocks = new List<TextBlock>();

	private readonly List<Label> _formattedLabels = new List<Label>();

	private IFont? _font;

	private int _fontSize = 10;

	private string? _fontInfo;

	private bool _needsRebuild;

	public GwenEventHandler<EventArgs>? Rebuilt;

	public List<Label> FormattedLabels => _formattedLabels;

	public IFont? Font
	{
		get
		{
			return _font;
		}
		set
		{
			_font = value;
			object fontInfo;
			if (value != null)
			{
				fontInfo = $"{value.Name},{_fontSize}";
			}
			else
			{
				fontInfo = null;
			}
			_fontInfo = (string?)fontInfo;
		}
	}

	public string? FontName
	{
		get
		{
			return _font?.Name;
		}
		set
		{
			Font = GameContentManager.Current.GetFont(value);
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
			_fontSize = value;
			IFont font = _font;
			_fontInfo = ((font != null) ? $"{font.Name},{value}" : null);
		}
	}

	public string? Text
	{
		get
		{
			return string.Join('\n', _textBlocks.Select((TextBlock block) => block.Text));
		}
		set
		{
			ClearText();
			AddText(value ?? string.Empty, (Color?)null);
		}
	}

	public RichLabel(Base parent, string? name = null)
		: base(parent, name)
	{
	}

	public void AddLineBreak()
	{
		TextBlock item = new TextBlock
		{
			Type = BlockType.NewLine
		};
		_textBlocks.Add(item);
	}

	public override void LoadJson(JToken token, bool isRoot = false)
	{
		base.LoadJson(token, isRoot);
		if (token is JObject jObject)
		{
			if (jObject["Font"] != null && jObject["Font"].Type != JTokenType.Null)
			{
				string[] array = ((string?)jObject["Font"]).Split(',');
				_fontInfo = (string?)jObject["Font"];
				_fontSize = int.Parse(array[1]);
				_font = GameContentManager.Current.GetFont(array[0]);
			}
			if (jObject.TryGetValue("FontName", out JToken value) && value is JValue { Type: JTokenType.String } jValue)
			{
				FontName = jValue.Value<string>();
			}
			if (jObject.TryGetValue("FontSize", out JToken value2) && value2 is JValue { Type: JTokenType.Integer } jValue2)
			{
				FontSize = jValue2.Value<int>();
			}
		}
	}

	public override JObject? GetJson(bool isRoot = false, bool onlySerializeIfNotEmpty = false)
	{
		JObject json = base.GetJson(isRoot, onlySerializeIfNotEmpty);
		if (json == null)
		{
			return null;
		}
		json.Add("Font", _fontInfo);
		return base.FixJson(json);
	}

	public void AddText(string? text, Label? template)
	{
		AddText(text, template?.TextColor, template?.CurAlignments.FirstOrDefault(Alignments.Left) ?? Alignments.Left, template?.Font, template?.FontSize ?? 0);
	}

	public void AddText(string? text, Color? color)
	{
		AddText(text, color, Alignments.Left);
	}

	public void AddText(string? text, Color? color, Alignments alignment, IFont? font = null, int fontSize = 0)
	{
		if (string.IsNullOrEmpty(text))
		{
			return;
		}
		if (text.Contains("\\c"))
		{
			List<ColorizedText> list = TextColorParser.Parse(text, color);
			{
				foreach (ColorizedText item2 in list)
				{
					AddText(item2.Text, item2.Color ?? color, alignment, font);
				}
				return;
			}
		}
		if (font == null)
		{
			font = _font;
		}
		if (fontSize == 0)
		{
			fontSize = _fontSize;
		}
		string[] array = text.Split(_newlines, StringSplitOptions.None);
		for (int i = 0; i < array.Length; i++)
		{
			if (i > 0)
			{
				AddLineBreak();
			}
			TextBlock item = new TextBlock
			{
				Type = BlockType.Text,
				Text = array[i],
				Color = color,
				Font = font,
				FontSize = fontSize,
				Alignment = alignment
			};
			_textBlocks.Add(item);
			InvalidateRebuild();
		}
	}

	public void AppendText(string text, Label? template)
	{
		AppendText(text, template?.TextColor, template?.CurAlignments.FirstOrDefault(Alignments.Left) ?? Alignments.Left, template?.Font);
	}

	public void AppendText(string text)
	{
		AppendText(text, null, Alignments.Left);
	}

	public void AppendText(string text, Color? color, Alignments alignment, IFont? font = null, int fontSize = 0)
	{
		if (_textBlocks.Count < 1)
		{
			AddText(text, color, alignment, font);
			return;
		}
		if (text.StartsWith('\n'))
		{
			AddText(text, color, alignment, font);
			return;
		}
		if (text.StartsWith("\r\n"))
		{
			AddText(text, color, alignment, font);
			return;
		}
		TextBlock textBlock = _textBlocks.Last();
		string[] source = text.Split(_newlines, StringSplitOptions.TrimEntries);
		string text2 = source.FirstOrDefault();
		IEnumerable<string> enumerable = source.AsEnumerable();
		if (text2 != null)
		{
			Color color2 = color ?? textBlock.Color;
			IFont font2 = font ?? textBlock.Font;
			int fontSize2 = ((fontSize == 0) ? textBlock.FontSize : fontSize);
			if (alignment != textBlock.Alignment || color2 != textBlock.Color || font2 != textBlock.Font)
			{
				AddText(text, color, alignment, font, fontSize2);
				return;
			}
			enumerable = enumerable.Skip(1);
			textBlock.Text += text;
			Label label = textBlock.Labels.LastOrDefault();
			if (label == null)
			{
				InvalidateRebuild();
			}
			else
			{
				label.Text += text;
				label.SizeToContents();
				if (label.OuterBounds.Right > base.InnerBounds.Right)
				{
					InvalidateRebuild();
				}
			}
		}
		foreach (string item in enumerable)
		{
			AddText(item, color, alignment, font);
		}
	}

	public void InvalidateRebuild()
	{
		if (!_needsRebuild)
		{
			ApplicationContext.CurrentContext.Logger.LogTrace("Requesting rebuild of {NodeType} ({NodeName})", "RichLabel", base.ParentQualifiedName);
			_needsRebuild = true;
		}
		Invalidate();
	}

	public void ForceImmediateRebuild()
	{
		Rebuild();
	}

	protected override Point ApplyDockFillOnSizeToChildren(Point size, Point internalSize)
	{
		if (size.X >= internalSize.X && size.Y >= internalSize.Y)
		{
			return size;
		}
		return base.ApplyDockFillOnSizeToChildren(size, internalSize);
	}

	public override Point GetChildrenSize()
	{
		return base.GetChildrenSize();
	}

	protected void SplitLabel(string text, IFont font, int fontSize, TextBlock block, ref int x, ref int y, ref int lineHeight, int initialX, int availableWidth)
	{
		if (string.IsNullOrEmpty(text))
		{
			return;
		}
		int num = availableWidth - x;
		if (num >= base.Skin.Renderer.MeasureText(font, fontSize, text).X)
		{
			CreateLabel(text, block, ref x, ref y, ref lineHeight, initialX, availableWidth, noSplit: true);
			return;
		}
		string[] array = Util.SplitAndKeep(text, " ");
		if (array.Length == 0)
		{
			SplitLabelByChars(text, font, fontSize, block, ref x, ref y, ref lineHeight, initialX, availableWidth);
			return;
		}
		Point point = base.Skin.Renderer.MeasureText(font, fontSize, array[0]);
		if (point.X >= num && array[0].Length >= text.Length)
		{
			SplitLabelByChars(text, font, fontSize, block, ref x, ref y, ref lineHeight, initialX, availableWidth);
			return;
		}
		if (point.X >= num)
		{
			CreateLabel(array[0], block, ref x, ref y, ref lineHeight, initialX, availableWidth, noSplit: true);
			if (array[0].Length < text.Length)
			{
				string text2 = text.Substring(array[0].Length + 1);
				SplitLabel(text2, font, fontSize, block, ref x, ref y, ref lineHeight, initialX, availableWidth);
			}
			return;
		}
		string text3 = string.Empty;
		for (int i = 0; i < array.Length; i++)
		{
			if (base.Skin.Renderer.MeasureText(font, fontSize, text3 + array[i]).X > num)
			{
				if (text3.Length == 0 && x <= initialX)
				{
					SplitLabelByChars(array[i], font, fontSize, block, ref x, ref y, ref lineHeight, initialX, availableWidth);
					SplitLabel(text.Substring(array[i].Length), font, fontSize, block, ref x, ref y, ref lineHeight, initialX, availableWidth);
					return;
				}
				CreateLabel(text3, block, ref x, ref y, ref lineHeight, initialX, availableWidth, noSplit: true);
				x = 0;
				y += lineHeight;
				break;
			}
			text3 += array[i];
		}
		int length = text3.Length;
		if (length < text.Length)
		{
			string text2 = text.Substring(length);
			SplitLabel(text2, font, fontSize, block, ref x, ref y, ref lineHeight, initialX, availableWidth);
		}
	}

	private void SplitLabelByChars(string text, IFont font, int fontSize, TextBlock block, ref int x, ref int y, ref int lineHeight, int initialX, int availableWidth)
	{
		string text2 = string.Empty;
		for (int i = 0; i < text.Length; i++)
		{
			string text3 = text2 + text[i];
			if (base.Skin.Renderer.MeasureText(font, fontSize, text3).X > availableWidth - x && text2.Length > 0)
			{
				CreateLabel(text2, block, ref x, ref y, ref lineHeight, initialX, availableWidth, noSplit: true);
				x = initialX;
				y += lineHeight;
				text2 = text[i].ToString();
			}
			else
			{
				text2 = text3;
			}
		}
		if (text2.Length > 0)
		{
			CreateLabel(text2, block, ref x, ref y, ref lineHeight, initialX, availableWidth, noSplit: true);
		}
	}

	protected void CreateLabel(string text, TextBlock block, ref int x, ref int y, ref int lineHeight, int initialX, int availableWidth, bool noSplit)
	{
		IFont font = base.Skin.DefaultFont;
		if (block.Font != null)
		{
			font = block.Font;
		}
		int fontSize = block.FontSize;
		Point point = base.Skin.Renderer.MeasureText(font, fontSize, text);
		if (lineHeight == -1)
		{
			lineHeight = point.Y;
		}
		if (!noSplit && x + point.X > base.Width)
		{
			SplitLabel(text, font, fontSize, block, ref x, ref y, ref lineHeight, initialX, availableWidth);
			return;
		}
		if (x + point.X > base.Width)
		{
			CreateNewline(ref x, ref y, lineHeight, initialX);
		}
		Label label = new Label(this)
		{
			X = x,
			Y = y,
			Alignment = new Alignments[1] { block.Alignment },
			AutoSizeToContents = true,
			Font = font,
			FontSize = block.FontSize,
			RestrictToParent = false,
			Text = ((x == 0) ? text.TrimStart(' ') : text),
			TextAlign = Pos.None,
			TextColorOverride = block.Color,
		};
		Label label2 = label;
		label2.SizeToContents();
		block.Labels.Add(label2);
		_formattedLabels.Add(label2);
		x += label2.Width;
		if (x > availableWidth)
		{
			CreateNewline(ref x, ref y, lineHeight, initialX);
		}
	}

	protected void CreateNewline(ref int x, ref int y, int lineHeight, int initialX)
	{
		x = initialX;
		y += lineHeight;
	}

	public void ClearText()
	{
		_textBlocks.Clear();
		InvalidateRebuild();
	}

	private void Rebuild()
	{
		_needsRebuild = false;
		DeleteAllChildren();
		_formattedLabels.Clear();
		Rectangle bounds = base.Bounds;
		Padding padding = base.Padding;
		int left = padding.Left;
		int availableWidth = bounds.Width - (left + padding.Right);
		int x = left;
		int y = padding.Top;
		int lineHeight = -1;
		foreach (TextBlock textBlock in _textBlocks)
		{
			textBlock.Labels.Clear();
			if (textBlock.Type == BlockType.NewLine)
			{
				CreateNewline(ref x, ref y, lineHeight, left);
			}
			else if (textBlock.Type == BlockType.Text)
			{
				CreateLabel(textBlock.Text, textBlock, ref x, ref y, ref lineHeight, left, availableWidth, noSplit: false);
			}
		}
		bool resizeX = !base.Dock.HasFlag(Pos.Fill) || _dockFillSize == default(Point) || _dockFillSize.X != base.Width;
		SizeToChildren(resizeX);
		Rebuilt?.Invoke(this, EventArgs.Empty);
	}

	protected override void OnSizeChanged(Point oldSize, Point newSize)
	{
		base.OnSizeChanged(oldSize, newSize);
		if (oldSize.X != newSize.X || newSize.X != _dockFillSize.X || newSize.Y > _dockFillSize.Y)
		{
			ApplicationContext.CurrentContext.Logger.LogDebug("Rebuilding due to size change from {OldSize} to {NewSize} (_dockFillSize={DockFillSize})", oldSize, newSize, _dockFillSize);
			InvalidateRebuild();
		}
	}

	protected override void Layout(Intersect.Client.Framework.Gwen.Skin.Base skin)
	{
		base.Layout(skin);
		if (_needsRebuild)
		{
			Rebuild();
		}
		Base obj = null;
		foreach (Base child in base.Children)
		{
			if (obj != null && child.Y == obj.Y)
			{
				Align.PlaceRightBottom(child, obj);
			}
			obj = child;
		}
	}
}
