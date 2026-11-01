using System;
using System.Collections.Generic;
using Intersect.Client.Framework.Content;
using Intersect.Client.Framework.File_Management;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.Framework.Gwen.Control.EventArguments;
using Intersect.Client.Framework.Gwen.ControlInternal;
using Intersect.Client.Framework.Gwen.Skin;
using Intersect.Client.Framework.Input;
using Intersect.Core;
using Intersect.Framework.Reflection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace Intersect.Client.Framework.Gwen.Control;

public class Button : Label
{
	private bool mCenterImage;

	private readonly Dictionary<ButtonSoundState, string> _stateSoundNames = new Dictionary<ButtonSoundState, string>();

	private readonly Dictionary<ComponentState, IGameTexture> _stateTextures = new Dictionary<ComponentState, IGameTexture>();

	private readonly Dictionary<ComponentState, string> _stateTextureNames = new Dictionary<ComponentState, string>();

	private bool mToggle;

	private bool mToggleStatus;

	private DateTime _ignoreMouseUpSoundsUntil;

	public bool IsToggle
	{
		get
		{
			return mToggle;
		}
		set
		{
			mToggle = value;
		}
	}

	public bool ToggleState
	{
		get
		{
			return mToggleStatus;
		}
		set
		{
			if (!mToggle || mToggleStatus == value)
			{
				return;
			}
			mToggleStatus = value;
			if (this.Toggled != null)
			{
				this.Toggled(this, EventArgs.Empty);
			}
			if (mToggleStatus)
			{
				if (this.ToggledOn != null)
				{
					this.ToggledOn(this, EventArgs.Empty);
				}
			}
			else if (this.ToggledOff != null)
			{
				this.ToggledOff(this, EventArgs.Empty);
			}
			Redraw();
		}
	}

	public override ComponentState ComponentState
	{
		get
		{
			ComponentState componentState = base.ComponentState;
			if (componentState == ComponentState.Disabled)
			{
				return componentState;
			}
			if (IsToggle && ToggleState)
			{
				componentState = ComponentState.Active;
			}
			return componentState;
		}
	}

	public event GwenEventHandler<EventArgs>? Toggled;

	public event GwenEventHandler<EventArgs>? ToggledOn;

	public event GwenEventHandler<EventArgs>? ToggledOff;

	public Button(Base parent, string? name = null, bool disableText = false)
		: base(parent, name, disableText)
	{
		base.AutoSizeToContents = false;
		base.MouseInputEnabled = true;
		base.TextAlign = Pos.Center;
		base.Size = new Point(100, 20);
		base.Padding = new Padding(3, 3, 3, 3);
		base.UsesHeadingFace = (!(this is MenuItem) && !(this is ComboBox) && !(this is CategoryButton) && !(this is TreeNodeLabel)) || 1 == 0;
		SetSound(ButtonSoundState.Hover, "menu_hover.wav");
		SetSound(ButtonSoundState.MouseDown, "menu_click.wav");
		SetSound(ButtonSoundState.MouseUp, "menu_click.wav");
	}

	public override JObject? GetJson(bool isRoot = false, bool onlySerializeIfNotEmpty = false)
	{
		JObject json = base.GetJson(isRoot, onlySerializeIfNotEmpty);
		if (json == null)
		{
			return null;
		}
		if (!(this is Checkbox))
		{
			json.Add("NormalImage", GetStateTextureName(ComponentState.Normal));
			json.Add("HoveredImage", GetStateTextureName(ComponentState.Hovered));
			json.Add("ClickedImage", GetStateTextureName(ComponentState.Active));
			json.Add("DisabledImage", GetStateTextureName(ComponentState.Disabled));
		}
		json.Add("_stateSoundNames", JObject.FromObject(_stateSoundNames));
		json.Add("CenterImage", mCenterImage);
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
			SetStateTexture(GameContentManager.Current.GetTexture(TextureType.Gui, (string?)jObject["NormalImage"]), (string?)jObject["NormalImage"], ComponentState.Normal);
		}
		if (jObject["HoveredImage"] != null)
		{
			SetStateTexture(GameContentManager.Current.GetTexture(TextureType.Gui, (string?)jObject["HoveredImage"]), (string?)jObject["HoveredImage"], ComponentState.Hovered);
		}
		if (jObject["ClickedImage"] != null)
		{
			SetStateTexture(GameContentManager.Current.GetTexture(TextureType.Gui, (string?)jObject["ClickedImage"]), (string?)jObject["ClickedImage"], ComponentState.Active);
		}
		if (jObject["DisabledImage"] != null)
		{
			SetStateTexture(GameContentManager.Current.GetTexture(TextureType.Gui, (string?)jObject["DisabledImage"]), (string?)jObject["DisabledImage"], ComponentState.Disabled);
		}
		if (jObject["CenterImage"] != null)
		{
			mCenterImage = (bool)jObject["CenterImage"];
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

	public void PlaySound(ButtonSoundState soundState)
	{
		if ((soundState != ButtonSoundState.MouseUp || !(_ignoreMouseUpSoundsUntil > DateTime.UtcNow)) && _stateSoundNames.TryGetValue(soundState, out string value) && PlaySound(value) && soundState == ButtonSoundState.MouseDown)
		{
			_ignoreMouseUpSoundsUntil = DateTime.UtcNow.AddMilliseconds(200.0);
		}
	}

	public virtual void Toggle()
	{
		ToggleState = !ToggleState;
	}

	public virtual void Press(Base? control = null)
	{
		OnMouseClicked(MouseButton.Left, default(Point));
	}

	protected override void Render(Intersect.Client.Framework.Gwen.Skin.Base skin)
	{
		if (base.ShouldDrawBackground)
		{
			skin.DrawButton(this);
		}
		base.Render(skin);
	}

	protected override void OnMouseDown(MouseButton mouseButton, Point mousePosition, bool userAction = true)
	{
		base.OnMouseDown(mouseButton, mousePosition, userAction);
		PlaySound(ButtonSoundState.MouseDown);
	}

	protected override void OnMouseUp(MouseButton mouseButton, Point mousePosition, bool userAction = true)
	{
		base.OnMouseUp(mouseButton, mousePosition, userAction);
		PlaySound(ButtonSoundState.MouseUp);
	}

	protected override void OnMouseClicked(MouseButton mouseButton, Point mousePosition, bool userAction = true)
	{
		if (!IsDisabledByTree)
		{
			if (IsToggle)
			{
				Toggle();
			}
			base.OnMouseClicked(mouseButton, mousePosition, userAction);
			PlaySound(ButtonSoundState.MouseClicked);
		}
	}

	protected override bool OnKeySpace(bool down)
	{
		if (!down)
		{
			EmitVirtualClick(new MouseButtonState(MouseButton.Left, default(Point), isPressed: false));
			return true;
		}
		return base.OnKeySpace(down);
	}

	protected override bool OnKeyReturn(bool down)
	{
		if (!down)
		{
			EmitVirtualClick(new MouseButtonState(MouseButton.Left, default(Point), isPressed: false));
			return true;
		}
		return base.OnKeyReturn(down);
	}

	protected override void OnAccelerator()
	{
		OnMouseClicked(MouseButton.Left, default(Point));
	}

	protected override void Layout(Intersect.Client.Framework.Gwen.Skin.Base skin)
	{
		base.Layout(skin);
	}

	public override void UpdateColors()
	{
		Color color = GetTextColor(ComponentState.Normal) ?? base.Skin.Colors.Button.Normal;
		if (IsDisabledByTree)
		{
			color = GetTextColor(ComponentState.Disabled) ?? base.Skin.Colors.Button.Disabled;
		}
		else if (base.IsActive)
		{
			color = GetTextColor(ComponentState.Active) ?? base.Skin.Colors.Button.Active;
		}
		else if (IsHovered)
		{
			color = GetTextColor(ComponentState.Hovered) ?? base.Skin.Colors.Button.Hover;
		}
		if (color == null)
		{
			ApplicationContext.CurrentContext.Logger.LogError("Text color for the current control state of {ComponentType} '{ComponentName}' is somehow null IsDisabled={IsDisabled} IsActive={IsActive} IsHovered={IsHovered}", GetType().GetName(qualified: true), base.ParentQualifiedName, base.IsDisabled, base.IsActive, IsHovered);
			color = new Color(255, 0, 255);
		}
		Color textColor = base.TextColor;
		base.TextColor = color;
	}

	protected override void OnMouseDoubleClicked(MouseButton mouseButton, Point mousePosition, bool userAction = true)
	{
		base.OnMouseDoubleClicked(mouseButton, mousePosition, userAction);
		OnMouseClicked(mouseButton, mousePosition, userAction);
	}

	public void SetAllStatesTexture(IGameTexture? texture)
	{
		ComponentState[] values = Enum.GetValues<ComponentState>();
		foreach (ComponentState componentState in values)
		{
			SetStateTexture(texture, componentState);
		}
	}

	public void SetStateTexture(ComponentState componentState, string textureName)
	{
		IGameTexture texture = GameContentManager.Current.GetTexture(TextureType.Gui, textureName);
		SetStateTexture(texture, textureName, componentState);
	}

	public void SetStateTexture(IGameTexture? texture, ComponentState componentState)
	{
		SetStateTexture(texture, texture?.Name, componentState);
	}

	public void SetStateTexture(IGameTexture? texture, string? name, ComponentState state)
	{
		if (texture == null && !string.IsNullOrWhiteSpace(name))
		{
			texture = GameContentManager.Current.GetTexture(TextureType.Gui, name);
		}
		if (texture == null)
		{
			_stateTextures.Remove(state);
		}
		else
		{
			_stateTextures[state] = texture;
		}
		if (name == null)
		{
			_stateTextureNames.Remove(state);
		}
		else
		{
			_stateTextureNames[state] = name;
		}
	}

	public IGameTexture? GetStateTexture(ComponentState state)
	{
		return _stateTextures.GetValueOrDefault(state);
	}

	public string? GetStateTextureName(ComponentState state)
	{
		return _stateTextureNames.GetValueOrDefault(state);
	}

	protected override void OnMouseEntered()
	{
		base.OnMouseEntered();
		if (base.ShouldDrawHover)
		{
			PlaySound(ButtonSoundState.Hover);
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
}
