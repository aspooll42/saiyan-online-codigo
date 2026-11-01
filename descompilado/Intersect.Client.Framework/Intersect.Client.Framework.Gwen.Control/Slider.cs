using System;
using System.Linq;
using System.Numerics;
using Intersect.Client.Framework.Content;
using Intersect.Client.Framework.File_Management;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.Framework.Gwen.Control.EventArguments;
using Intersect.Client.Framework.Gwen.ControlInternal;
using Intersect.Client.Framework.Gwen.Input;
using Intersect.Client.Framework.Gwen.Skin;
using Intersect.Client.Framework.Input;
using Intersect.Framework;
using Newtonsoft.Json.Linq;

namespace Intersect.Client.Framework.Gwen.Control;

public class Slider : Base
{
	private readonly SliderBar _sliderBar;

	private IGameTexture? _backgroundImage;

	private string? _backgroundImageName;

	private double _maximumValue;

	private double _minimumValue;

	private int _notchCount;

	private double[]? _notches;

	private Orientation _orientation;

	private bool _snapToNotches;

	private double _value;

	public Orientation Orientation
	{
		get
		{
			return _orientation;
		}
		set
		{
			if (value != _orientation)
			{
				_orientation = value;
				_sliderBar.Orientation = value;
				Invalidate();
			}
		}
	}

	public int NotchCount
	{
		get
		{
			return _notchCount;
		}
		set
		{
			_notchCount = value;
		}
	}

	public double[]? Notches
	{
		get
		{
			return _notches;
		}
		set
		{
			_notches = value;
		}
	}

	public bool SnapToNotches
	{
		get
		{
			return _snapToNotches;
		}
		set
		{
			_snapToNotches = value;
		}
	}

	public double Minimum
	{
		get
		{
			return _minimumValue;
		}
		set
		{
			SetRange(value, _maximumValue);
		}
	}

	public double Maximum
	{
		get
		{
			return _maximumValue;
		}
		set
		{
			SetRange(_minimumValue, value);
		}
	}

	public double Value
	{
		get
		{
			return ScaleValueToExternal(_value);
		}
		set
		{
			SetValue(value);
		}
	}

	public IGameTexture? BackgroundImage
	{
		get
		{
			return _backgroundImage;
		}
		set
		{
			if (value != _backgroundImage)
			{
				_backgroundImage = value;
				_backgroundImageName = value?.Name;
			}
		}
	}

	public string? BackgroundImageName
	{
		get
		{
			return _backgroundImageName;
		}
		set
		{
			if (!string.Equals(value, _backgroundImageName, StringComparison.Ordinal))
			{
				_backgroundImageName = value;
				_backgroundImage = (string.IsNullOrWhiteSpace(_backgroundImageName) ? null : GameContentManager.Current.GetTexture(TextureType.Gui, _backgroundImageName));
			}
		}
	}

	public Point DraggerSize
	{
		get
		{
			return _sliderBar.Size;
		}
		set
		{
			_sliderBar.Size = value;
		}
	}

	public event GwenEventHandler<ValueChangedEventArgs<double>>? ValueChanged;

	public Slider(Base parent, string? name = null)
		: base(parent, name)
	{
		base.Size = new Point(120, 20);
		_minimumValue = 0.0;
		_maximumValue = 1.0;
		_notchCount = 5;
		_orientation = Orientation.LeftToRight;
		_snapToNotches = false;
		_value = 0.0;
		_sliderBar = new SliderBar(this, "_sliderBar")
		{
			AnchorAxis = new Vector2(0f, 0.5f)
		};
		_sliderBar.Dragged += SliderBarOnDragged;
		base.KeyboardInputEnabled = true;
		base.IsTabable = true;
	}

	private double ScaleValueToExternal(double value)
	{
		double minimumValue = _minimumValue;
		return minimumValue + value * (_maximumValue - minimumValue);
	}

	public override bool SizeToChildren(SizeToChildrenArgs args)
	{
		return false;
	}

	public override JObject? GetJson(bool isRoot = false, bool onlySerializeIfNotEmpty = false)
	{
		JObject json = base.GetJson(isRoot, onlySerializeIfNotEmpty);
		if (json == null)
		{
			return null;
		}
		json.Add("BackgroundImage", BackgroundImageName);
		json.Add("SnapToNotches", SnapToNotches);
		json.Add("NotchCount", NotchCount);
		JArray value = ((Notches == null || Notches.Length < 1) ? null : new JArray(Notches.Cast<object>().ToArray()));
		json.Add("Notches", value);
		return base.FixJson(json);
	}

	protected override bool OnKeyRight(bool down)
	{
		if (down)
		{
			Value += 1.0;
		}
		return true;
	}

	protected override bool OnKeyUp(bool down)
	{
		if (down)
		{
			Value += 1.0;
		}
		return true;
	}

	protected override bool OnKeyLeft(bool down)
	{
		if (down)
		{
			Value -= 1.0;
		}
		return true;
	}

	protected override bool OnKeyDown(bool down)
	{
		if (down)
		{
			Value -= 1.0;
		}
		return true;
	}

	protected override bool OnKeyHome(bool down)
	{
		if (down)
		{
			Value = _minimumValue;
		}
		return true;
	}

	protected override bool OnKeyEnd(bool down)
	{
		if (down)
		{
			Value = _maximumValue;
		}
		return true;
	}

	protected virtual void SliderBarOnDragged(Base control, EventArgs args)
	{
		SetValueInternal(CalculateValue(), forceUpdate: true);
	}

	protected override void OnMouseDown(MouseButton mouseButton, Point mousePosition, bool userAction = true)
	{
		base.OnMouseDown(mouseButton, mousePosition, userAction);
		if (!IsDisabledByTree)
		{
			Point point = ToLocal(mousePosition);
			int x = _sliderBar.X;
			int y = _sliderBar.Y;
			Orientation orientation = _orientation;
			switch (orientation)
			{
			case Orientation.LeftToRight:
			case Orientation.RightToLeft:
				x = (int)((double)point.X - (double)_sliderBar.Width * 0.5);
				break;
			case Orientation.TopToBottom:
			case Orientation.BottomToTop:
				y = (int)((double)point.Y - (double)_sliderBar.Height * 0.5);
				break;
			default:
				throw Exceptions.UnreachableInvalidEnum(orientation);
			}
			_sliderBar.MoveTo(x, y);
			_sliderBar.InputNonUserMouseClicked(mouseButton, mousePosition, isPressed: true);
			SliderBarOnDragged(_sliderBar, EventArgs.Empty);
			InputHandler.PassMouseFocusTo(base.Canvas, _sliderBar);
		}
	}

	protected virtual float CalculateValue()
	{
		Orientation orientation = _orientation;
		return orientation switch
		{
			Orientation.LeftToRight => (float)_sliderBar.X / (float)(base.Width - _sliderBar.Width), 
			Orientation.RightToLeft => 1f - (float)_sliderBar.X / (float)(base.Width - _sliderBar.Width), 
			Orientation.TopToBottom => 1f - (float)_sliderBar.Y / (float)(base.Height - _sliderBar.Height), 
			Orientation.BottomToTop => (float)_sliderBar.Y / (float)(base.Height - _sliderBar.Height), 
			_ => throw Exceptions.UnreachableInvalidEnum(orientation), 
		};
	}

	protected virtual void UpdateBarFromValue()
	{
		Orientation orientation = _orientation;
		switch (orientation)
		{
		case Orientation.LeftToRight:
		{
			int x2 = (int)((double)(base.Width - _sliderBar.Width) * _value);
			_sliderBar.MoveTo(x2, _sliderBar.Y);
			break;
		}
		case Orientation.RightToLeft:
		{
			int x = (int)((double)(base.Width - _sliderBar.Width) * (1.0 - _value));
			_sliderBar.MoveTo(x, _sliderBar.Y);
			break;
		}
		case Orientation.TopToBottom:
			_sliderBar.MoveTo(_sliderBar.X, (int)((double)(base.Height - _sliderBar.Height) * (1.0 - _value)));
			break;
		case Orientation.BottomToTop:
			_sliderBar.MoveTo(_sliderBar.X, (int)((double)(base.Height - _sliderBar.Height) * _value));
			break;
		default:
			throw Exceptions.UnreachableInvalidEnum(orientation);
		}
	}

	protected override void Layout(Intersect.Client.Framework.Gwen.Skin.Base skin)
	{
		UpdateBarFromValue();
	}

	protected override void Render(Intersect.Client.Framework.Gwen.Skin.Base skin)
	{
		Orientation orientation = _orientation;
		bool flag = (uint)orientation <= 1u;
		int barSize = (flag ? _sliderBar.Width : _sliderBar.Height);
		skin.DrawSlider(this, orientation, Notches, NotchCount, barSize);
	}

	protected virtual void SetValueInternal(double newInternalValue, bool forceUpdate = false, bool skipEvents = false)
	{
		if (_snapToNotches)
		{
			if (_notches == null || _notches.Length < 1)
			{
				newInternalValue = Math.Floor(newInternalValue * (double)_notchCount + 0.5);
				newInternalValue /= _notchCount;
			}
			else
			{
				double notchMin = _notches.Min();
				double num = _notches.Max();
				double notchRange = num - notchMin;
				double[] source = _notches.OrderBy((double notchValue) => Math.Abs(notchMin + newInternalValue * notchRange - notchValue)).ToArray();
				double num2 = source.First();
				newInternalValue = (num2 - notchMin) / notchRange;
			}
		}
		if (!_value.Equals(newInternalValue))
		{
			_value = newInternalValue;
			if (!skipEvents)
			{
				this.ValueChanged?.Invoke(this, new ValueChangedEventArgs<double>
				{
					Value = ScaleValueToExternal(newInternalValue)
				});
			}
		}
		else if (!forceUpdate)
		{
			return;
		}
		UpdateBarFromValue();
	}

	public void SetRange(double min, double max)
	{
		double num = Math.Min(min, max);
		double num2 = Math.Max(min, max);
		if (num > _maximumValue)
		{
			_maximumValue = num2;
			_minimumValue = num;
		}
		else if (num2 < _minimumValue)
		{
			_minimumValue = num;
			_maximumValue = num2;
		}
		else
		{
			_minimumValue = num;
			_maximumValue = num2;
		}
	}

	protected override void RenderFocus(Intersect.Client.Framework.Gwen.Skin.Base skin)
	{
		if (InputHandler.KeyboardFocus == this)
		{
			_ = base.IsTabable;
		}
	}

	public void SetDraggerImage(IGameTexture? texture, ComponentState state)
	{
		_sliderBar.SetImage(texture, texture?.Name, state);
	}

	public IGameTexture? GetDraggerImage(ComponentState state)
	{
		return _sliderBar.GetImage(state);
	}

	public void SetSound(string? sound, ButtonSoundState state)
	{
		_sliderBar.SetSound(sound, state);
	}

	public override void LoadJson(JToken token, bool isRoot = false)
	{
		base.LoadJson(token);
		if (!(token is JObject jObject))
		{
			return;
		}
		if (jObject.TryGetValue("BackgroundImage", out JToken value) && value is JValue { Type: JTokenType.String } jValue)
		{
			string text = jValue.Value<string>();
			if (text != null)
			{
				BackgroundImage = GameContentManager.Current.GetTexture(TextureType.Gui, text);
				goto IL_005b;
			}
		}
		BackgroundImage = null;
		goto IL_005b;
		IL_005b:
		if (jObject["SnapToNotches"] != null)
		{
			_snapToNotches = (bool)jObject["SnapToNotches"];
		}
		if (jObject["NotchCount"] != null)
		{
			_notchCount = (int)jObject["NotchCount"];
		}
		JToken jToken = jObject["Notches"];
		if (jToken != null && jToken.Type != JTokenType.Null)
		{
			Notches = ((JArray)jToken).Select((JToken jToken2) => (double)jToken2).ToArray();
			if (Notches.Length < 1)
			{
				Notches = null;
			}
		}
		else
		{
			Notches = null;
		}
		if (jObject["SliderBar"] != null)
		{
			_sliderBar.LoadJson(jObject["SliderBar"]);
		}
	}

	public void SetValue(double value, bool skipEvents = false)
	{
		if (value > _maximumValue)
		{
			value = _maximumValue;
		}
		if (value < _minimumValue)
		{
			value = _minimumValue;
		}
		double newInternalValue = (value - _minimumValue) / Math.Max(1.0, _maximumValue - _minimumValue);
		SetValueInternal(newInternalValue, forceUpdate: false, skipEvents);
		Redraw();
	}
}
