using System;
using Intersect.Client.Framework.File_Management;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.Framework.Gwen.Control.EventArguments;
using Intersect.Client.Framework.Gwen.ControlInternal;

namespace Intersect.Client.Framework.Gwen.Control;

public class Titlebar : Dragger
{
	private readonly WindowControl _window;

	private readonly ImagePanel _icon;

	private readonly CloseButton _closeButton;

	private readonly Label _label;

	public CloseButton CloseButton => _closeButton;

	public ImagePanel Icon => _icon;

	public Label Label => _label;

	public string? Title
	{
		get
		{
			return _label.Text;
		}
		set
		{
			_label.Text = value;
		}
	}

	internal Titlebar(WindowControl window, GwenEventHandler<MouseButtonState> closeButtonHandler, string? name = "Titlebar")
		: base(window, name)
	{
		_window = window;
		base.ClipContents = false;
		base.Dock = Pos.Top;
		base.Height = 24;
		base.Margin = default(Margin);
		base.Padding = new Padding(4, 0, 0, 0);
		base.Target = window;
		IFont font = GameContentManager.Current?.GetFont("sourcesansproblack");
		_icon = new ImagePanel(this, "_icon")
		{
			Dock = Pos.Left,
			IsVisibleInTree = false,
			Margin = new Margin(0, 4, 0, 4),
			MaximumSize = new Point(24, 24),
			RestrictToParent = false,
			Size = new Point(24, 24)
		};
		_icon.TextureLoaded += IconOnTextureLoaded;
		_label = new Label(this, "_label")
		{
			AutoSizeToContents = false,
			Dock = (Pos.CenterV | Pos.Fill),
			UsesHeadingFace = true,
			Font = font,
			FontSize = 12,
			MouseInputEnabled = false,
			Padding = new Padding(4, 4),
			TextAlign = (Pos.Left | Pos.Bottom),
			TextColor = base.Skin.Colors.Window.TitleInactive
		};
		_closeButton = new CloseButton(this, window, "CloseButton")
		{
			Dock = Pos.Right,
			Font = null,
			IsTabable = false,
			Size = new Point(24, 24)
		};
		_closeButton.Clicked += closeButtonHandler;
	}

	private void IconOnTextureLoaded(Base @base, EventArgs eventArgs)
	{
		IGameTexture texture = _icon.Texture;
		_icon.IsVisibleInTree = texture != null;
		if (texture != null)
		{
			_icon.MinimumSize = texture.Dimensions;
			_icon.MaximumSize = texture.Dimensions;
			_icon.Size = texture.Dimensions;
			base.Height = _icon.Size.Y + _icon.Margin.Bottom + _icon.Margin.Top;
		}
	}

	protected override void OnBoundsChanged(Rectangle oldBounds, Rectangle newBounds)
	{
		base.OnBoundsChanged(oldBounds, newBounds);
	}
}
