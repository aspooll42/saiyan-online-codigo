using System;
using System.Collections.Generic;
using Intersect.Client.Framework.Content;
using Intersect.Client.Framework.File_Management;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.Framework.Gwen.Input;
using Intersect.Client.Framework.Gwen.Skin;
using Intersect.Client.Framework.Input;
using Newtonsoft.Json.Linq;

namespace Intersect.Client.Framework.Gwen.ControlInternal;

public class Dragger : Intersect.Client.Framework.Gwen.Control.Base
{
	protected Point mHoldPos;

	private readonly Dictionary<ButtonSoundState, string> _stateSoundNames = new Dictionary<ButtonSoundState, string>();

	private DateTime _ignoreMouseUpSoundsUntil;

	private IGameTexture? mClickedImage;

	private string? mClickedImageFilename;

	private IGameTexture? mDisabledImage;

	private string? mDisabledImageFilename;

	private IGameTexture? mHoverImage;

	private string? mHoverImageFilename;

	private IGameTexture? mNormalImage;

	private string? mNormalImageFilename;

	protected Intersect.Client.Framework.Gwen.Control.Base? _target;

	internal Intersect.Client.Framework.Gwen.Control.Base Target
	{
		get
		{
			return _target;
		}
		set
		{
			_target = value;
		}
	}

	public event GwenEventHandler<EventArgs>? Dragged;

	public Dragger(Intersect.Client.Framework.Gwen.Control.Base parent, string? name = null)
		: base(parent, name)
	{
		base.MouseInputEnabled = true;
		base.KeepFocusOnMouseExit = true;
	}

	protected override void OnMouseEntered()
	{
		base.OnMouseEntered();
		if (base.ShouldDrawHover && _stateSoundNames.TryGetValue(ButtonSoundState.Hover, out string value))
		{
			PlaySound(value);
		}
	}

	protected override void OnMouseDown(MouseButton mouseButton, Point mousePosition, bool userAction = true)
	{
		base.OnMouseDown(mouseButton, mousePosition, userAction);
		if (_target != null)
		{
			if (userAction && _stateSoundNames.TryGetValue(ButtonSoundState.MouseDown, out string value))
			{
				PlaySound(value);
			}
			mHoldPos = _target.CanvasPosToLocal(mousePosition);
			InputHandler.MouseFocus = this;
		}
	}

	protected override void OnMouseUp(MouseButton mouseButton, Point mousePosition, bool userAction = true)
	{
		base.OnMouseUp(mouseButton, mousePosition, userAction);
		if (userAction && _stateSoundNames.TryGetValue(ButtonSoundState.MouseUp, out string value))
		{
			PlaySound(value);
		}
		InputHandler.MouseFocus = null;
	}

	protected override void OnMouseMoved(int x, int y, int dx, int dy)
	{
		if (_target != null && base.IsActive)
		{
			Point point = new Point(x - mHoldPos.X, y - mHoldPos.Y);
			Intersect.Client.Framework.Gwen.Control.Base parent = _target.Parent;
			if (parent != null)
			{
				point = parent.ToLocal(point.X, point.Y);
			}
			_target.MoveTo(point.X, point.Y);
			this.Dragged?.Invoke(this, EventArgs.Empty);
		}
	}

	protected override void Render(Intersect.Client.Framework.Gwen.Skin.Base skin)
	{
	}

	public override void Invalidate()
	{
		base.Invalidate();
	}

	protected override void Layout(Intersect.Client.Framework.Gwen.Skin.Base skin)
	{
		base.Layout(skin);
	}

	protected override void OnBoundsChanged(Rectangle oldBounds, Rectangle newBounds)
	{
		base.OnBoundsChanged(oldBounds, newBounds);
	}

	public override JObject? GetJson(bool isRoot = false, bool onlySerializeIfNotEmpty = false)
	{
		JObject json = base.GetJson(isRoot, onlySerializeIfNotEmpty);
		if (json == null)
		{
			return null;
		}
		json.Add("NormalImage", GetImageFilename(ComponentState.Normal));
		json.Add("HoveredImage", GetImageFilename(ComponentState.Hovered));
		json.Add("ClickedImage", GetImageFilename(ComponentState.Active));
		json.Add("DisabledImage", GetImageFilename(ComponentState.Disabled));
		json.Add("_stateSoundNames", JObject.FromObject(_stateSoundNames));
		return base.FixJson(json);
	}

	public override void LoadJson(JToken token, bool isRoot = false)
	{
		base.LoadJson(token, isRoot);
		if (!(token is JObject jObject))
		{
			return;
		}
		if (jObject["NormalImage"] != null)
		{
			SetImage(GameContentManager.Current.GetTexture(TextureType.Gui, (string?)jObject["NormalImage"]), (string?)jObject["NormalImage"], ComponentState.Normal);
		}
		if (jObject["HoveredImage"] != null)
		{
			SetImage(GameContentManager.Current.GetTexture(TextureType.Gui, (string?)jObject["HoveredImage"]), (string?)jObject["HoveredImage"], ComponentState.Hovered);
		}
		if (jObject["ClickedImage"] != null)
		{
			SetImage(GameContentManager.Current.GetTexture(TextureType.Gui, (string?)jObject["ClickedImage"]), (string?)jObject["ClickedImage"], ComponentState.Active);
		}
		if (jObject["DisabledImage"] != null)
		{
			SetImage(GameContentManager.Current.GetTexture(TextureType.Gui, (string?)jObject["DisabledImage"]), (string?)jObject["DisabledImage"], ComponentState.Disabled);
		}
		if (!jObject.TryGetValue("_stateSoundNames", out JToken value) || !(value is JObject jObject2))
		{
			return;
		}
		foreach (var (value2, jToken2) in jObject2)
		{
			if (Enum.TryParse<ButtonSoundState>(value2, out var result) && result != ButtonSoundState.None && jToken2 is JValue { Type: JTokenType.String } jValue)
			{
				string text2 = jValue.Value<string>()?.Trim();
				if (text2 != null && text2.Length > 0)
				{
					_stateSoundNames[result] = text2;
				}
				else
				{
					_stateSoundNames.Remove(result);
				}
			}
		}
	}

	public void SetSound(ButtonSoundState state, string? soundName)
	{
		soundName = soundName?.Trim();
		if (string.IsNullOrEmpty(soundName))
		{
			_stateSoundNames.Remove(state);
		}
		else
		{
			_stateSoundNames[state] = soundName;
		}
	}

	public string? GetMouseUpSound()
	{
		_stateSoundNames.TryGetValue(ButtonSoundState.MouseUp, out string value);
		return value;
	}

	public virtual void SetImage(IGameTexture? texture, string? name, ComponentState state)
	{
		switch (state)
		{
		case ComponentState.Normal:
			mNormalImageFilename = name;
			mNormalImage = texture;
			break;
		case ComponentState.Hovered:
			mHoverImageFilename = name;
			mHoverImage = texture;
			break;
		case ComponentState.Active:
			mClickedImageFilename = name;
			mClickedImage = texture;
			break;
		case ComponentState.Disabled:
			mDisabledImageFilename = name;
			mDisabledImage = texture;
			break;
		default:
			throw new ArgumentOutOfRangeException("state", state, null);
		}
	}

	public virtual IGameTexture? GetImage(ComponentState state)
	{
		return state switch
		{
			ComponentState.Normal => mNormalImage, 
			ComponentState.Hovered => mHoverImage, 
			ComponentState.Active => mClickedImage, 
			ComponentState.Disabled => mDisabledImage, 
			_ => null, 
		};
	}

	public virtual string GetImageFilename(ComponentState state)
	{
		return state switch
		{
			ComponentState.Normal => mNormalImageFilename, 
			ComponentState.Hovered => mHoverImageFilename, 
			ComponentState.Active => mClickedImageFilename, 
			ComponentState.Disabled => mDisabledImageFilename, 
			_ => null, 
		};
	}

	public void PlaySound(ButtonSoundState soundState)
	{
		if ((soundState != ButtonSoundState.MouseUp || !(_ignoreMouseUpSoundsUntil > DateTime.UtcNow)) && _stateSoundNames.TryGetValue(soundState, out string value) && PlaySound(value) && soundState == ButtonSoundState.MouseDown)
		{
			_ignoreMouseUpSoundsUntil = DateTime.UtcNow.AddMilliseconds(200.0);
		}
	}

	public void ClearSounds()
	{
		_stateSoundNames.Clear();
	}

	public void SetSound(string sound, ButtonSoundState state)
	{
		switch (state)
		{
		case ButtonSoundState.Hover:
			_stateSoundNames[ButtonSoundState.Hover] = sound;
			break;
		case ButtonSoundState.MouseDown:
			_stateSoundNames[ButtonSoundState.MouseDown] = sound;
			break;
		case ButtonSoundState.MouseUp:
			_stateSoundNames[ButtonSoundState.MouseUp] = sound;
			break;
		default:
			throw new ArgumentOutOfRangeException("state", state, null);
		case ButtonSoundState.None:
			break;
		}
	}
}
