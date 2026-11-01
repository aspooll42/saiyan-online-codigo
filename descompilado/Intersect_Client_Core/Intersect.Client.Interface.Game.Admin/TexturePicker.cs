using System;
using Intersect.Client.Entities;
using Intersect.Client.Framework.Content;
using Intersect.Client.Framework.File_Management;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.Framework.Gwen;
using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.Framework.Gwen.Control.EventArguments;
using Intersect.Client.Framework.Gwen.Skin;
using Intersect.Client.Localization;
using Intersect.Framework.Core.Sprites;

namespace Intersect.Client.Interface.Game.Admin;

public class TexturePicker : Panel, ITextContainer
{
	private readonly IFont? _defaultFont;

	private readonly Panel _previewPanel;

	private readonly ImagePanel _preview;

	private readonly Panel _inputPanel;

	private readonly LabeledComboBox _textureSelector;

	private readonly Button _submitButton;

	private TextureType _textureType;

	private bool _selectorDirty;

	private bool _allowNone;

	private MenuItem? _noneItem;

	public bool AllowNone
	{
		get
		{
			return _allowNone;
		}
		set
		{
			Intersect.Client.Framework.Gwen.Control.Base.SetAndDoIfChanged(ref _allowNone, value, UpdateNone, this);
		}
	}

	public IFont? ButtonFont
	{
		get
		{
			return _submitButton.Font;
		}
		set
		{
			_submitButton.Font = value;
		}
	}

	public IFont? LabelFont
	{
		get
		{
			return _textureSelector.LabelFont;
		}
		set
		{
			_textureSelector.LabelFont = value;
		}
	}

	public IFont? SelectorFont
	{
		get
		{
			return _textureSelector.ItemFont;
		}
		set
		{
			_textureSelector.ItemFont = value;
		}
	}

	public IFont? Font
	{
		get
		{
			return _textureSelector.Font;
		}
		set
		{
			_submitButton.Font = value;
			_textureSelector.Font = value;
		}
	}

	public int FontSize
	{
		get
		{
			return _textureSelector.FontSize;
		}
		set
		{
			_submitButton.FontSize = value;
			_textureSelector.FontSize = value;
		}
	}

	public string? ButtonText
	{
		get
		{
			return _submitButton.Text;
		}
		set
		{
			_submitButton.Text = value;
		}
	}

	public string? LabelText
	{
		get
		{
			return _textureSelector.Label;
		}
		set
		{
			_textureSelector.Label = value;
		}
	}

	public TextureType TextureType
	{
		get
		{
			return _textureType;
		}
		set
		{
			Intersect.Client.Framework.Gwen.Control.Base.SetAndDoIfChanged(ref _textureType, value, InvalidateSelector, this);
		}
	}

	string? ITextContainer.Text
	{
		get
		{
			return LabelText;
		}
		set
		{
			LabelText = value;
		}
	}

	public Color? TextPaddingDebugColor { get; set; }

	public event GwenEventHandler<TexturePicker, ItemSelectedEventArgs>? Selected;

	public event GwenEventHandler<TexturePicker, ValueChangedEventArgs<string?>>? Submitted;

	public TexturePicker(Intersect.Client.Framework.Gwen.Control.Base parent, string? name = "TexturePicker")
		: base(parent, name)
	{
		_allowNone = true;
		_defaultFont = GameContentManager.Current.GetFont("sourcesansproblack");
		base.DockChildSpacing = new Padding(8);
		base.ShouldDrawBackground = false;
		_previewPanel = new Panel(this, "_previewPanel")
		{
			Dock = Pos.Left,
			MinimumSize = new Point(68, 68),
			Padding = new Padding(4)
		};
		_preview = new ImagePanel(_previewPanel, "_preview")
		{
			Alignment = new Alignments[1] { Alignments.Center },
			MaintainAspectRatio = true
		};
		_inputPanel = new Panel(this, "_inputPanel")
		{
			Dock = Pos.Fill,
			DockChildSpacing = new Padding(8),
			ShouldDrawBackground = false
		};
		_textureSelector = new LabeledComboBox(_inputPanel, "_textureSelector")
		{
			AutoSizeToContents = false,
			Dock = Pos.Fill,
			Font = _defaultFont,
			FontSize = 12,
			TextPadding = new Padding(8, 4, 0, 4)
		};
		_textureSelector.ItemSelected += TextureSelectorOnItemSelected;
		_submitButton = new Button(_inputPanel, "_submitButton")
		{
			Dock = Pos.Bottom,
			Font = _defaultFont,
			FontSize = 12,
			Padding = new Padding(8, 4)
		};
		_submitButton.Clicked += SubmitButtonOnClicked;
		SizeToChildren(resizeX: true, resizeY: true, recursive: true);
	}

	private void TextureSelectorOnItemSelected(Intersect.Client.Framework.Gwen.Control.Base sender, ItemSelectedEventArgs arguments)
	{
		if (!(arguments.SelectedUserData is string text))
		{
			_preview.Texture = null;
			return;
		}
		IGameTexture texture = GameContentManager.Current.GetTexture(_textureType, text);
		_preview.Texture = texture;
		if (texture == null)
		{
			return;
		}
		if (_textureType == TextureType.Entity)
		{
			SpriteSheet spriteSheet = SpriteSheets.ForEntity(text);
			if (spriteSheet != null)
			{
				var (x, y, num, num2) = spriteSheet.PreviewRect();
				_preview.SetTextureRect(x, y, num, num2);
				_preview.SetSize(Math.Min(num, 48), Math.Min(num2, 48));
				this.Selected?.Invoke(this, arguments);
				return;
			}
		}
		int num3 = texture.Width;
		int num4 = texture.Height;
		if (_textureType == TextureType.Entity)
		{
			num3 = texture.Width / Options.Instance.Sprites.NormalFrames;
			num4 = texture.Height / Options.Instance.Sprites.Directions;
		}
		_preview.SetTextureRect((_preview.Width - num3) / 2, (_preview.Height - num4) / 2, num3, num4);
		_preview.SetSize(Math.Min(num3, 48), Math.Min(num4, 48));
		this.Selected?.Invoke(this, arguments);
	}

	private void SubmitButtonOnClicked(Intersect.Client.Framework.Gwen.Control.Base sender, MouseButtonState arguments)
	{
		this.Submitted?.Invoke(this, new ValueChangedEventArgs<string>
		{
			Value = (_textureSelector.SelectedItem?.UserData as string)
		});
	}

	private static void UpdateNone(TexturePicker @this)
	{
		if (@this._allowNone)
		{
			if (@this._noneItem == null)
			{
				@this._noneItem = @this.AddNone();
			}
		}
		else if (@this._noneItem != null)
		{
			@this.RemoveChild(@this._noneItem, dispose: true);
			@this._noneItem = null;
		}
	}

	public void InvalidateSelector()
	{
		RunOnMainThread(InvalidateSelector, this);
	}

	private static void InvalidateSelector(TexturePicker @this)
	{
		if (!@this._selectorDirty)
		{
			@this._selectorDirty = true;
		}
		@this.Invalidate();
	}

	private MenuItem AddNone()
	{
		MenuItem menuItem = _textureSelector.AddItem(Strings.General.None);
		menuItem.SendToBack();
		return menuItem;
	}

	protected override void Layout(Intersect.Client.Framework.Gwen.Skin.Base skin)
	{
		if (_selectorDirty)
		{
			_selectorDirty = false;
			string[] textureNames = GameContentManager.Current.GetTextureNames(_textureType);
			Array.Sort(textureNames, new AlphanumComparatorFast());
			_noneItem = null;
			_textureSelector.ClearItems();
			UpdateNone(this);
			string[] array = textureNames;
			foreach (string text in array)
			{
				_textureSelector.AddItem(text, null, text);
			}
		}
		base.Layout(skin);
	}
}
