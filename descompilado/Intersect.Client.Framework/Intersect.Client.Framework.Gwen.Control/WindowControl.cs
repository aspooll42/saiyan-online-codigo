using System;
using System.Diagnostics.CodeAnalysis;
using Intersect.Client.Framework.Content;
using Intersect.Client.Framework.File_Management;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.Framework.Gwen.Control.EventArguments;
using Intersect.Client.Framework.Gwen.ControlInternal;
using Intersect.Client.Framework.Gwen.Skin;
using Newtonsoft.Json.Linq;

namespace Intersect.Client.Framework.Gwen.Control;

public class WindowControl : ResizableControl
{
	public enum ControlState
	{
		Active,
		Inactive
	}

	private readonly Titlebar _titlebar;

	private Color? mActiveColor;

	private Color? mInactiveColor;

	private IGameTexture? mActiveImage;

	private IGameTexture? mInactiveImage;

	private string? mActiveImageFilename;

	private string? mInactiveImageFilename;

	private bool mDeleteOnClose;

	protected Base InnerPanel => _innerPanel ?? throw new InvalidOperationException("Windows must have inner panels");

	public ImagePanel IconContainer => _titlebar.Icon;

	public Dragger Titlebar => _titlebar;

	public Label TitleLabel => _titlebar.Label;

	public Padding InnerPanelPadding
	{
		get
		{
			return _innerPanel?.Padding ?? default(Padding);
		}
		set
		{
			if (_innerPanel != null)
			{
				_innerPanel.Padding = value;
			}
		}
	}

	public string? Title
	{
		get
		{
			return _titlebar.Title;
		}
		set
		{
			_titlebar.Title = value;
		}
	}

	public bool IsClosable
	{
		get
		{
			return !_titlebar.CloseButton.IsHidden;
		}
		set
		{
			_titlebar.CloseButton.IsVisibleInTree = value;
		}
	}

	public IGameTexture? Icon
	{
		get
		{
			return _titlebar.Icon.Texture;
		}
		set
		{
			_titlebar.Icon.Texture = value;
		}
	}

	public string? IconName
	{
		get
		{
			return _titlebar.Icon.TextureFilename;
		}
		set
		{
			_titlebar.Icon.TextureFilename = value;
		}
	}

	public bool DeleteOnClose
	{
		get
		{
			return mDeleteOnClose;
		}
		set
		{
			mDeleteOnClose = value;
		}
	}

	public bool DrawShadow { get; set; } = true;

	protected virtual bool CanClose => true;

	public Rectangle TitleBarBounds => _titlebar?.Bounds ?? default(Rectangle);

	public event GwenEventHandler<EventArgs>? Closed;

	public WindowControl(Base? parent, string? title = null, bool modal = false, string? name = null)
		: base(parent, name)
	{
		base.ClipContents = false;
		_titlebar = new Titlebar(this, CloseButtonOnClicked)
		{
			Title = title
		};
		_innerPanel = new Base(this, "_innerPanel");
		_innerPanel.Dock = Pos.Fill;
		base.ClampMovement = true;
		base.IsTabable = false;
		base.KeyboardInputEnabled = false;
		base.MinimumSize = new Point(100, 40);
		GetResizer(8).Hide();
		BringToFront();
		Focus();
		if (modal)
		{
			MakeModal();
		}
	}

	protected override void OnBoundsChanged(Rectangle oldBounds, Rectangle newBounds)
	{
		base.OnBoundsChanged(oldBounds, newBounds);
	}

	protected override Point InnerPanelSizeFrom(Point size)
	{
		return size - new Point(0, _titlebar.Height);
	}

	protected override void OnVisibilityChanged(object? sender, VisibilityChangedEventArgs eventArgs)
	{
		base.OnVisibilityChanged(sender, eventArgs);
		if (eventArgs.IsVisible)
		{
			BringToFront();
		}
	}

	protected override void Layout(Intersect.Client.Framework.Gwen.Skin.Base skin)
	{
		base.Layout(skin);
		_titlebar.SizeToChildren(resizeX: false, resizeY: true, recursive: true);
		int height = _titlebar.Height;
		Base innerPanel = _innerPanel;
		if (innerPanel != null)
		{
			Point minimumSize = innerPanel.MinimumSize;
			minimumSize.Y = base.InnerHeight - height;
			innerPanel.MinimumSize = minimumSize;
		}
		_titlebar.CloseButton.Size = new Point(height, height);
	}

	internal override void DoRender(Intersect.Client.Framework.Gwen.Skin.Base skin)
	{
		base.DoRender(skin);
	}

	protected override Rectangle ValidateJsonBounds(Rectangle bounds)
	{
		Rectangle result = base.ValidateJsonBounds(bounds);
		result.Position = base.Bounds.Position;
		return result;
	}

	public override JObject? GetJson(bool isRoot = false, bool onlySerializeIfNotEmpty = false)
	{
		JObject json = base.GetJson(isRoot, onlySerializeIfNotEmpty);
		if (json == null)
		{
			return null;
		}
		json.Add("DrawShadow", DrawShadow);
		json.Add("ActiveImage", GetImageFilename(ControlState.Active));
		json.Add("InactiveImage", GetImageFilename(ControlState.Inactive));
		json.Add("ActiveColor", Color.ToString(mActiveColor));
		json.Add("InactiveColor", Color.ToString(mInactiveColor));
		json.Add("IsClosable", IsClosable);
		return base.FixJson(json);
	}

	public override void LoadJson(JToken token, bool isRoot = false)
	{
		base.LoadJson(token, isRoot: false);
		if (token is JObject jObject)
		{
			JToken jToken = jObject["DrawShadow"];
			if (jToken != null)
			{
				DrawShadow = (bool)jToken;
			}
			if (jObject["ActiveImage"] != null)
			{
				SetImage(GameContentManager.Current.GetTexture(TextureType.Gui, (string?)jObject["ActiveImage"]), (string?)jObject["ActiveImage"], ControlState.Active);
			}
			if (jObject["InactiveImage"] != null)
			{
				SetImage(GameContentManager.Current.GetTexture(TextureType.Gui, (string?)jObject["InactiveImage"]), (string?)jObject["InactiveImage"], ControlState.Inactive);
			}
			if (!string.IsNullOrWhiteSpace((string?)jObject["ActiveColor"]))
			{
				mActiveColor = Color.FromString((string?)jObject["ActiveColor"]);
			}
			if (!string.IsNullOrWhiteSpace((string?)jObject["InactiveColor"]))
			{
				mInactiveColor = Color.FromString((string?)jObject["InactiveColor"]);
			}
			if (jObject.TryGetValue("IsClosable", out JToken value) && value is JValue { Type: JTokenType.Boolean } jValue)
			{
				IsClosable = jValue.Value<bool>();
			}
			if (jObject.TryGetValue("Titlebar", out JToken value2))
			{
				_titlebar.LoadJson(value2);
			}
			if (jObject.TryGetValue("InnerPanel", out JToken value3))
			{
				_innerPanel?.LoadJson(value3);
			}
		}
	}

	public override void ProcessAlignments()
	{
		base.ProcessAlignments();
	}

	public void Close()
	{
		Close(this, EventArgs.Empty);
	}

	private void Close(Base sender, EventArgs args)
	{
		if (CanClose)
		{
			try
			{
				base.IsHidden = true;
			}
			catch (ObjectDisposedException)
			{
			}
			if (_modal != null)
			{
				_modal.DelayedDelete();
				_modal = null;
			}
			if (mDeleteOnClose)
			{
				base.Parent?.RemoveChild(this, dispose: true);
			}
			OnClose(sender, args);
			this.Closed?.Invoke(sender, args);
		}
	}

	private void CloseButtonOnClicked(Base sender, MouseButtonState args)
	{
		Close(sender, args);
	}

	protected virtual void OnClose(Base sender, EventArgs args)
	{
	}

	protected override void Render(Intersect.Client.Framework.Gwen.Skin.Base skin)
	{
		bool isOnTop = base.IsOnTop;
		Color color = GetTextColor(ControlState.Active);
		if (color == null && !isOnTop)
		{
			color = GetTextColor(ControlState.Inactive);
		}
		if ((object)color == null)
		{
			color = base.Skin.Colors.Window.TitleInactive;
		}
		_titlebar.Label.TextColor = color;
		skin.DrawWindow(this, _titlebar.Bottom, isOnTop);
	}

	protected override void RenderUnder(Intersect.Client.Framework.Gwen.Skin.Base skin)
	{
		base.RenderUnder(skin);
		if (DrawShadow)
		{
			skin.DrawShadow(this);
		}
	}

	public override void Touch()
	{
		base.Touch();
		BringToFront();
	}

	protected override void RenderFocus(Intersect.Client.Framework.Gwen.Skin.Base skin)
	{
	}

	public void SetTextColor(Color clr, ControlState state)
	{
		switch (state)
		{
		case ControlState.Active:
			mActiveColor = clr;
			break;
		case ControlState.Inactive:
			mInactiveColor = clr;
			break;
		default:
			throw new ArgumentOutOfRangeException("state", state, null);
		}
	}

	public Color? GetTextColor(ControlState state)
	{
		return state switch
		{
			ControlState.Active => mActiveColor, 
			ControlState.Inactive => mInactiveColor, 
			_ => throw new ArgumentOutOfRangeException("state", state, "Invalid ControlState"), 
		};
	}

	public void SetImage(IGameTexture texture, string fileName, ControlState state)
	{
		switch (state)
		{
		case ControlState.Active:
			mActiveImageFilename = fileName;
			mActiveImage = texture;
			break;
		case ControlState.Inactive:
			mInactiveImageFilename = fileName;
			mInactiveImage = texture;
			break;
		default:
			throw new ArgumentOutOfRangeException("state", state, null);
		}
	}

	public IGameTexture? GetImage(ControlState state)
	{
		return state switch
		{
			ControlState.Active => mActiveImage, 
			ControlState.Inactive => mInactiveImage, 
			_ => null, 
		};
	}

	public bool TryGetTexture(ControlState controlState, [NotNullWhen(true)] out IGameTexture? texture)
	{
		texture = GetImage(controlState);
		return texture != null;
	}

	public string GetImageFilename(ControlState state)
	{
		return state switch
		{
			ControlState.Active => mActiveImageFilename, 
			ControlState.Inactive => mInactiveImageFilename, 
			_ => null, 
		};
	}
}
