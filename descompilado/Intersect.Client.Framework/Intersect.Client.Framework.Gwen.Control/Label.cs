using System;
using System.Linq;
using System.Runtime.CompilerServices;
using Intersect.Client.Framework.Content;
using Intersect.Client.Framework.File_Management;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.Framework.Gwen.Control.EventArguments;
using Intersect.Client.Framework.Gwen.ControlInternal;
using Intersect.Client.Framework.Gwen.Skin;
using Intersect.Core;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace Intersect.Client.Framework.Gwen.Control;

public class Label : Base, ILabel, IAutoSizeToContents, IColorableText, ITextContainer, IFontProvider
{
	protected readonly Text _textElement;

	private string? _fontInfo;

	private Pos _textAlign;

	private bool _autoSizeToContents;

	private string? mBackgroundTemplateFilename;

	private IGameTexture? mBackgroundTemplateTex;

	protected Color? mClickedTextColor;

	protected Color? mDisabledTextColor;

	protected Color? mHoverTextColor;

	protected Color? mNormalTextColor;

	private Padding _textPadding;

	private bool _textDisabled;

	private string? _displayedText;

	private string? _text;

	private string? _formatString;

	private Range _replacementRange;

	public const string BodyFaceName = "sourcesansproblack";

	public const string HeadingFaceName = "refuel";

	private const int HeadingSizeBonus = 2;

	private bool _usesHeadingFace;

	private int _requestedFontSize;

	private string? _textOverride;

	private WrappingBehavior _wrappingBehavior;

	private IGameTexture? _tooltipBackground;

	public GwenEventHandler<ValueChangedEventArgs<Point>>? SizedToContents;

	public string? FormatString
	{
		get
		{
			return _formatString;
		}
		set
		{
			if (!string.Equals(value, _formatString, StringComparison.Ordinal))
			{
				_replacementRange = GetRangeForFormatArgument(value, 0);
				_formatString = value;
			}
		}
	}

	public bool IsEmpty
	{
		get
		{
			if (base.Children.Count > 1)
			{
				return false;
			}
			Base obj = base.Children.FirstOrDefault();
			if (obj == null || (obj is Text && !obj.IsVisibleInParent))
			{
				return true;
			}
			return false;
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
			if (value != _wrappingBehavior)
			{
				_wrappingBehavior = value;
				_textElement.WrappingBehavior = value;
			}
		}
	}

	public IGameTexture? ToolTipBackground
	{
		get
		{
			return _tooltipBackground;
		}
		set
		{
			if (value != _tooltipBackground)
			{
				_tooltipBackground = value;
			}
		}
	}

	public bool IsTextDisabled
	{
		get
		{
			return _textElement.IsHidden;
		}
		set
		{
			_textElement.IsHidden = value;
		}
	}

	public Pos TextAlign
	{
		get
		{
			return _textAlign;
		}
		set
		{
			SetAndDoIfChanged(ref _textAlign, value, Base.Invalidate);
		}
	}

	public virtual string? Text
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get
		{
			return _text;
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set
		{
			SetText(value);
		}
	}

	public bool UsesHeadingFace
	{
		get
		{
			return _usesHeadingFace;
		}
		set
		{
			if (value != _usesHeadingFace)
			{
				_usesHeadingFace = value;
				int fontSize = FontSize;
				Font = Font;
				FontSize = fontSize;
			}
		}
	}

	private bool DrawsHeadingFace
	{
		get
		{
			if (_usesHeadingFace)
			{
				return _textElement.Font?.Name == "refuel";
			}
			return false;
		}
	}

	public virtual IFont? Font
	{
		get
		{
			return _textElement.Font;
		}
		set
		{
			int fontSize = FontSize;
			if (_usesHeadingFace && value?.Name == "sourcesansproblack")
			{
				value = GameContentManager.Current?.GetFont("refuel") ?? value;
			}
			if (value == _textElement.Font)
			{
				return;
			}
			IFont font = _textElement.Font;
			if (value != null)
			{
				_textElement.Font = value;
				if (fontSize > 0)
				{
					_requestedFontSize = fontSize;
					_textElement.FontSize = fontSize + (DrawsHeadingFace ? 2 : 0);
				}
				_fontInfo = $"{value.Name},{FontSize}";
			}
			else
			{
				_fontInfo = null;
			}
			OnFontChanged(this, font, value);
			if (_autoSizeToContents)
			{
				SizeToContents();
			}
			Invalidate();
			this.FontChanged?.Invoke(this, new ValueChangedEventArgs<IFont>
			{
				OldValue = font,
				Value = value
			});
		}
	}

	public string? FontName
	{
		get
		{
			return _textElement.Font?.Name;
		}
		set
		{
			Font = GameContentManager.Current?.GetFont(value);
		}
	}

	public virtual int FontSize
	{
		get
		{
			if (_requestedFontSize <= 0)
			{
				return _textElement.FontSize;
			}
			return _requestedFontSize;
		}
		set
		{
			_requestedFontSize = value;
			_textElement.FontSize = value + (DrawsHeadingFace ? 2 : 0);
		}
	}

	public Color? TextColor
	{
		get
		{
			return _textElement.Color ?? Color.White;
		}
		set
		{
			if (!(value == _textElement.Color))
			{
				_textElement.Color = value;
			}
		}
	}

	public Color? TextColorOverride
	{
		get
		{
			return _textElement.ColorOverride;
		}
		set
		{
			_textElement.ColorOverride = value;
		}
	}

	public string? TextOverride
	{
		get
		{
			return _textOverride;
		}
		set
		{
			if (!string.Equals(value, _textOverride, StringComparison.Ordinal))
			{
				_textOverride = value;
				_textElement.DisplayedText = _textOverride ?? _text;
			}
		}
	}

	public int TextWidth => _textElement.Width;

	public int TextHeight => _textElement.Height;

	public int TextX => _textElement.X;

	public int TextY => _textElement.Y;

	public int TextLength => _text?.Length ?? 0;

	public int TextRight => _textElement.Right;

	public bool AutoSizeToContents
	{
		get
		{
			return _autoSizeToContents;
		}
		set
		{
			_autoSizeToContents = value;
			Invalidate();
			InvalidateParent();
		}
	}

	public Color? TextPaddingDebugColor { get; set; }

	public string? BackgroundTemplateName
	{
		get
		{
			return mBackgroundTemplateFilename;
		}
		set
		{
			string text = (string.IsNullOrWhiteSpace(value) ? null : value.Trim());
			if (!(text == mBackgroundTemplateFilename?.Trim()))
			{
				mBackgroundTemplateFilename = text;
				mBackgroundTemplateTex = ((text == null) ? null : GameContentManager.Current?.GetTexture(TextureType.Gui, text));
			}
		}
	}

	public event GwenEventHandler<ValueChangedEventArgs<IFont?>>? FontChanged;

	public override event GwenEventHandler<MouseButtonState> Clicked
	{
		add
		{
			base.Clicked += value;
			base.MouseInputEnabled = base.ClickEventAssigned;
		}
		remove
		{
			base.Clicked -= value;
			base.MouseInputEnabled = base.ClickEventAssigned;
		}
	}

	public override event GwenEventHandler<MouseButtonState> DoubleClicked
	{
		add
		{
			base.DoubleClicked += value;
			base.MouseInputEnabled = base.ClickEventAssigned;
		}
		remove
		{
			base.DoubleClicked -= value;
			base.MouseInputEnabled = base.ClickEventAssigned;
		}
	}

	public Label(Base parent, string? name = null, bool disableText = false)
		: base(parent, name)
	{
		_autoSizeToContents = true;
		_textDisabled = disableText;
		_textElement = new Text(this)
		{
			FontSize = 12,
			IsHidden = _textDisabled
		};
		base.MouseInputEnabled = false;
		base.Size = new Point(100, 10);
		TextAlign = Pos.Left | Pos.Top;
	}

	private static Range GetRangeForFormatArgument(string? format, int argumentIndex)
	{
		if (argumentIndex < 0)
		{
			throw new ArgumentOutOfRangeException("argumentIndex", argumentIndex, "Must be non-negative");
		}
		if (string.IsNullOrEmpty(format))
		{
			return default(Range);
		}
		int num = ((argumentIndex < 10) ? 1 : ((int)Math.Floor(Math.Log10(argumentIndex))));
		if (format.Length < 2 + num)
		{
			return default(Range);
		}
		char c = '{';
		int? num2 = null;
		Range result = default(Range);
		for (int i = 0; i < format.Length; i++)
		{
			char c2 = format[i];
			switch (c2)
			{
			case '{':
				c = '}';
				num2 = null;
				result = (i + 1)..(i + 1);
				break;
			case '}':
				if (c != '{')
				{
					c = '{';
					num2 = null;
					result = result.Start..(num2 ?? i);
					if (IsArgument(format, result, argumentIndex))
					{
						return (result.Start.Value - 1)..^(format.Length - (result.End.Value + 1));
					}
					result = default(Range);
				}
				break;
			case ':':
			{
				num2 = i;
				Range range = result.Start..i;
				if (!IsArgument(format, range, argumentIndex))
				{
					c = '{';
					num2 = null;
				}
				break;
			}
			default:
				if (!num2.HasValue && !char.IsAsciiDigit(c2))
				{
					c = '{';
				}
				break;
			}
		}
		return result;
	}

	private static bool IsArgument(string format, Range range, int argumentIndex)
	{
		Range range2 = range;
		if (int.TryParse(format[range2.Start..range2.End], out var result))
		{
			return result == argumentIndex;
		}
		return false;
	}

	public override void Invalidate()
	{
		base.Invalidate();
	}

	protected virtual void OnFontChanged(Base sender, IFont? oldFont, IFont? newFont)
	{
	}

	public override JObject? GetJson(bool isRoot = false, bool onlySerializeIfNotEmpty = false)
	{
		JObject json = base.GetJson(isRoot, onlySerializeIfNotEmpty);
		if (json == null)
		{
			return null;
		}
		if (typeof(Label) == GetType())
		{
			json.Add("BackgroundTemplate", mBackgroundTemplateFilename);
		}
		json.Add("TextColor", mNormalTextColor?.ToString());
		json.Add("HoveredTextColor", mHoverTextColor?.ToString());
		json.Add("ClickedTextColor", mClickedTextColor?.ToString());
		json.Add("DisabledTextColor", mDisabledTextColor?.ToString());
		json.Add("TextAlign", TextAlign.ToString());
		json.Add("AutoSizeToContents", _autoSizeToContents);
		string fontName = FontName;
		if (fontName != null && !string.IsNullOrWhiteSpace(fontName))
		{
			json.Add("FontName", FontName);
			json.Add("FontSize", FontSize);
		}
		else
		{
			json.Add("FontName", null);
			json.Add("FontSize", null);
		}
		json.Add("TextScale", _textElement.GetScale());
		return base.FixJson(json);
	}

	public override void LoadJson(JToken token, bool isRoot = false)
	{
		base.LoadJson(token, isRoot);
		if (!(token is JObject jObject))
		{
			return;
		}
		if (typeof(Label) == GetType() && jObject["BackgroundTemplate"] != null)
		{
			SetBackgroundTemplate(GameContentManager.Current.GetTexture(TextureType.Gui, (string?)jObject["BackgroundTemplate"]), (string?)jObject["BackgroundTemplate"]);
		}
		if (jObject["TextColor"] != null)
		{
			mNormalTextColor = Color.FromString((string?)jObject["TextColor"]);
		}
		if (jObject["HoveredTextColor"] != null)
		{
			mHoverTextColor = Color.FromString((string?)jObject["HoveredTextColor"]);
		}
		if (jObject["ClickedTextColor"] != null)
		{
			mClickedTextColor = Color.FromString((string?)jObject["ClickedTextColor"]);
		}
		if (jObject["DisabledTextColor"] != null)
		{
			mDisabledTextColor = Color.FromString((string?)jObject["DisabledTextColor"]);
		}
		if (jObject["TextAlign"] != null)
		{
			TextAlign = (Pos)Enum.Parse(typeof(Pos), (string?)jObject["TextAlign"]);
		}
		if (jObject["AutoSizeToContents"] != null)
		{
			_autoSizeToContents = (bool)jObject["AutoSizeToContents"];
		}
		JToken jToken = jObject["Font"];
		if (jToken != null && jToken.Type != JTokenType.Null)
		{
			string text = (string?)jToken;
			if (!string.IsNullOrWhiteSpace(text))
			{
				string[] array = text.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
				if (array.Length > 1)
				{
					_fontInfo = text;
					try
					{
						FontSize = int.Parse(array[1]);
						FontName = array[0];
					}
					catch
					{
						throw;
					}
				}
			}
		}
		if (jObject.TryGetValue("FontName", out JToken value) && value is JValue { Type: JTokenType.String } jValue)
		{
			FontName = jValue.Value<string>();
		}
		if (jObject.TryGetValue("FontSize", out JToken value2) && value2 is JValue { Type: JTokenType.Integer } jValue2)
		{
			FontSize = jValue2.Value<int>();
		}
		if (jObject["TextScale"] != null)
		{
			_textElement.SetScale((float)jObject["TextScale"]);
		}
	}

	public IGameTexture GetTemplate()
	{
		return mBackgroundTemplateTex;
	}

	public void SetBackgroundTemplate(IGameTexture texture, string fileName)
	{
		if (texture == null && !string.IsNullOrWhiteSpace(fileName))
		{
			texture = GameContentManager.Current?.GetTexture(TextureType.Gui, fileName);
		}
		mBackgroundTemplateFilename = fileName;
		mBackgroundTemplateTex = texture;
	}

	public virtual void MakeColorNormal()
	{
		TextColor = base.Skin.Colors.Label.Normal;
	}

	public virtual void MakeColorBright()
	{
		TextColor = base.Skin.Colors.Label.Hover;
	}

	public virtual void MakeColorDark()
	{
		TextColor = base.Skin.Colors.Label.Disabled;
	}

	public virtual void MakeColorHighlight()
	{
		TextColor = base.Skin.Colors.Label.Active;
	}

	protected virtual Point GetClosestCharacter(int x, int y)
	{
		return GetClosestCharacter(new Point(x, y));
	}

	protected Point GetClosestCharacter(Point point)
	{
		point = _textElement.CanvasPosToLocal(point);
		int closestCharacter = _textElement.GetClosestCharacter(point);
		Point result = new Point(closestCharacter, 0);
		Range replacementRange = _replacementRange;
		if (_textOverride == null)
		{
			string displayedText = _displayedText;
			if (displayedText != null && !replacementRange.Equals(default(Range)))
			{
				int value = replacementRange.Start.Value;
				int num = replacementRange.End.Value;
				if (replacementRange.End.IsFromEnd)
				{
					num = displayedText.Length - num;
				}
				result.X = Math.Clamp(result.X, value, num);
				return result;
			}
		}
		return result;
	}

	protected void SetTextPosition(int x, int y)
	{
		_textElement.SetPosition(x, y);
	}

	protected virtual void OnTextChanged()
	{
	}

	protected override void Layout(Intersect.Client.Framework.Gwen.Skin.Base skin)
	{
		base.Layout(skin);
		if (_autoSizeToContents)
		{
			SizeToContents();
		}
		AlignTextElement(_textElement);
	}

	protected void AlignTextElement(Text textElement)
	{
		if (!_textDisabled || textElement != _textElement || !textElement.IsHidden)
		{
			Pos textAlign = TextAlign;
			int outerWidth = textElement.OuterWidth;
			int outerHeight = textElement.OuterHeight;
			Padding padding = base.Padding;
			int w = base.Width - (padding.Left + padding.Right);
			int h = base.Height - (padding.Top + padding.Bottom);
			Rectangle rectangle = new Rectangle(padding.Left, padding.Top, w, h);
			int x = padding.Left;
			int y = padding.Top;
			if (textAlign.HasFlag(Pos.CenterH))
			{
				x = padding.Left + (int)((float)(rectangle.Width - outerWidth) / 2f);
			}
			else if (textAlign.HasFlag(Pos.Right))
			{
				x = rectangle.Right - outerWidth;
			}
			if (textAlign.HasFlag(Pos.CenterV))
			{
				y = padding.Top + (int)((float)(rectangle.Height - outerHeight) / 2f);
			}
			else if (textAlign.HasFlag(Pos.Bottom))
			{
				y = rectangle.Bottom - outerHeight;
			}
			textElement.SetPosition(x, y);
		}
	}

	public virtual void SetText(string? text, bool doEvents = true)
	{
		if (!string.Equals(_text, text, StringComparison.Ordinal))
		{
			_text = text;
			_displayedText = (string.IsNullOrWhiteSpace(_formatString) ? _text : string.Format(_formatString, _text));
			_textElement.DisplayedText = _displayedText;
			if (_autoSizeToContents && SizeToContents())
			{
				Invalidate();
				InvalidateParent();
			}
			if (doEvents)
			{
				OnTextChanged();
			}
		}
	}

	protected override void OnChildBoundsChanged(Base child, Rectangle oldChildBounds, Rectangle newChildBounds)
	{
		base.OnChildBoundsChanged(child, oldChildBounds, newChildBounds);
		if (oldChildBounds.Size == newChildBounds.Size)
		{
			return;
		}
		if (!_autoSizeToContents && child is Text)
		{
			Point size = newChildBounds.Size;
			Point size2 = base.Size;
			if (size.X > size2.X || size.Y > size2.Y)
			{
				OnTextExceedsSize(size2, size);
			}
		}
		Invalidate();
	}

	protected virtual void OnTextExceedsSize(Point ownSize, Point textSize)
	{
		Invalidate();
	}

	public virtual void SetTextScale(float scale)
	{
		_textElement.SetScale(scale);
		if (_autoSizeToContents)
		{
			SizeToContents();
		}
		Invalidate();
		InvalidateParent();
	}

	protected virtual Point GetContentSize()
	{
		return _textElement.Size;
	}

	protected virtual Padding GetContentPadding()
	{
		return base.Padding + _textPadding;
	}

	protected override void OnBoundsChanged(Rectangle oldBounds, Rectangle newBounds)
	{
		base.OnBoundsChanged(oldBounds, newBounds);
		if (base.RestrictToParent)
		{
			Text textElement = _textElement;
			Point maximumSize = _textElement.MaximumSize;
			maximumSize.X = base.InnerWidth;
			textElement.MaximumSize = maximumSize;
		}
	}

	public bool SizeToContents()
	{
		Point contentSize;
		return SizeToContents(out contentSize);
	}

	public virtual bool SizeToContents(out Point contentSize)
	{
		_textElement.SizeToChildren();
		contentSize = MeasureShrinkToContents();
		Point size = base.Size;
		if (!SetSize(contentSize))
		{
			return false;
		}
		ProcessAlignments();
		InvalidateParent();
		SizedToContents?.Invoke(this, new ValueChangedEventArgs<Point>
		{
			Value = contentSize,
			OldValue = size
		});
		return true;
	}

	public virtual Point MeasureShrinkToContents()
	{
		Padding contentPadding = GetContentPadding();
		Point contentSize = GetContentSize();
		Point minimumSize = base.MinimumSize;
		int val = contentSize.X + contentPadding.Left + contentPadding.Right;
		val = Math.Max(val, minimumSize.X);
		int val2 = contentSize.Y + contentPadding.Top + contentPadding.Bottom;
		val2 = Math.Max(val2, minimumSize.Y);
		Point maximumSize = base.MaximumSize;
		if (maximumSize.X > 0)
		{
			val = Math.Min(maximumSize.X, val);
		}
		if (maximumSize.Y > 0)
		{
			val2 = Math.Min(maximumSize.Y, val2);
		}
		return new Point(val, val2);
	}

	public virtual Point GetCharacterPosition(int index)
	{
		Point characterPosition = _textElement.GetCharacterPosition(index);
		return new Point(characterPosition.X + _textElement.X, characterPosition.Y + _textElement.Y);
	}

	protected override void Render(Intersect.Client.Framework.Gwen.Skin.Base skin)
	{
		skin.DrawLabel(this);
	}

	public override void UpdateColors()
	{
		Color color = GetTextColor(ComponentState.Normal) ?? base.Skin.Colors.Label.Normal;
		if (IsDisabledByTree)
		{
			color = GetTextColor(ComponentState.Disabled) ?? base.Skin.Colors.Label.Disabled;
		}
		else if (base.IsActive)
		{
			color = GetTextColor(ComponentState.Active) ?? base.Skin.Colors.Label.Active;
		}
		else if (IsHovered)
		{
			color = GetTextColor(ComponentState.Hovered) ?? base.Skin.Colors.Label.Hover;
		}
		if (color == null)
		{
			ApplicationContext.CurrentContext.Logger.LogError("Text color for the current control state of '{ComponentName}' is somehow null IsDisabled={IsDisabled} IsActive={IsActive} IsHovered={IsHovered}", base.ParentQualifiedName, base.IsDisabled, base.IsActive, IsHovered);
			color = new Color(255, 0, 255);
		}
		TextColor = color;
	}

	public virtual void SetTextColor(Color clr, ComponentState state)
	{
		switch (state)
		{
		case ComponentState.Normal:
			mNormalTextColor = clr;
			break;
		case ComponentState.Hovered:
			mHoverTextColor = clr;
			break;
		case ComponentState.Active:
			mClickedTextColor = clr;
			break;
		case ComponentState.Disabled:
			mDisabledTextColor = clr;
			break;
		default:
			throw new ArgumentOutOfRangeException("state", state, null);
		}
		UpdateColors();
	}

	public virtual Color? GetTextColor(ComponentState state)
	{
		return state switch
		{
			ComponentState.Normal => mNormalTextColor, 
			ComponentState.Hovered => mHoverTextColor, 
			ComponentState.Active => mClickedTextColor, 
			ComponentState.Disabled => mDisabledTextColor, 
			_ => null, 
		};
	}

	public override string ToString()
	{
		return "Label (Text='" + Text + "')";
	}
}
