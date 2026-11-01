using System;
using System.Globalization;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.Framework.Gwen.Control.EventArguments;
using Intersect.Client.Framework.Gwen.ControlInternal;
using Intersect.Client.Framework.Gwen.Skin;
using Intersect.Framework;

namespace Intersect.Client.Framework.Gwen.Control;

public class LabeledSlider : Base, ISmartAutoSizeToContents, IAutoSizeToContents, INumericInput, ITextContainer
{
	private readonly Label _label;

	private readonly Slider _slider;

	private readonly TextBoxNumeric _sliderValue;

	private double _scale = 1.0;

	private int _rounding = -1;

	private bool _recomputeValueMinimumSize = true;

	private bool _autoSizeToContentWidth;

	private bool _autoSizeToContentHeight;

	private bool _autoSizeToContentWidthOnChildResize;

	private bool _autoSizeToContentHeightOnChildResize;

	public IGameTexture? BackgroundImage
	{
		get
		{
			return _slider.BackgroundImage;
		}
		set
		{
			_slider.BackgroundImage = value;
		}
	}

	public string? BackgroundImageName
	{
		get
		{
			return _slider.BackgroundImageName;
		}
		set
		{
			_slider.BackgroundImageName = value;
		}
	}

	public Point DraggerSize
	{
		get
		{
			return _slider.DraggerSize;
		}
		set
		{
			_slider.DraggerSize = value;
		}
	}

	public IFont? Font
	{
		get
		{
			return _label.Font;
		}
		set
		{
			_label.Font = value;
			_sliderValue.Font = value;
			_sliderValue.MinimumSize = ComputeMinimumSizeForSliderValue();
		}
	}

	public int FontSize
	{
		get
		{
			return _label.FontSize;
		}
		set
		{
			_label.FontSize = value;
			_sliderValue.FontSize = value;
			_sliderValue.MinimumSize = ComputeMinimumSizeForSliderValue();
		}
	}

	public Point SliderSize
	{
		get
		{
			return _slider.MinimumSize;
		}
		set
		{
			_slider.MinimumSize = value;
		}
	}

	public bool IsValueInputEnabled
	{
		get
		{
			return _sliderValue.IsVisibleInTree;
		}
		set
		{
			_sliderValue.IsVisibleInTree = value;
		}
	}

	public string? Label
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

	public Orientation Orientation
	{
		get
		{
			return _slider.Orientation;
		}
		set
		{
			_slider.Orientation = value;
		}
	}

	public int NotchCount
	{
		get
		{
			return _slider.NotchCount;
		}
		set
		{
			_slider.NotchCount = value;
		}
	}

	public double[]? Notches
	{
		get
		{
			return _slider.Notches;
		}
		set
		{
			_slider.Notches = value;
		}
	}

	public bool SnapToNotches
	{
		get
		{
			return _slider.SnapToNotches;
		}
		set
		{
			_slider.SnapToNotches = value;
		}
	}

	public int Rounding
	{
		get
		{
			return _rounding;
		}
		set
		{
			_rounding = value;
			if (_rounding > -1)
			{
				_sliderValue.Value = Math.Round(_sliderValue.Value, _rounding);
			}
		}
	}

	public double Minimum
	{
		get
		{
			return _slider.Minimum;
		}
		set
		{
			_slider.Minimum = value;
			_sliderValue.Minimum = value;
		}
	}

	public double Maximum
	{
		get
		{
			return _slider.Maximum;
		}
		set
		{
			_slider.Maximum = value;
			_sliderValue.Maximum = value;
			_sliderValue.MinimumSize = ComputeMinimumSizeForSliderValue(value);
		}
	}

	public double Scale
	{
		get
		{
			return _scale;
		}
		set
		{
			_scale = value;
			_sliderValue.Value = _slider.Value * _scale;
		}
	}

	public double Value
	{
		get
		{
			return _slider.Value;
		}
		set
		{
			_slider.Value = value;
			_sliderValue.Value = _slider.Value * _scale;
		}
	}

	public Point LabelMinimumSize
	{
		get
		{
			return _label.MinimumSize;
		}
		set
		{
			_label.MinimumSize = value;
		}
	}

	public string? ValueFormatString
	{
		get
		{
			return _sliderValue.FormatString;
		}
		set
		{
			_sliderValue.FormatString = value;
		}
	}

	public bool AutoSizeToContents
	{
		get
		{
			if (!AutoSizeToContentWidth)
			{
				return AutoSizeToContentHeight;
			}
			return true;
		}
		set
		{
			AutoSizeToContentWidth = value;
			AutoSizeToContentHeight = value;
		}
	}

	public bool AutoSizeToContentWidth
	{
		get
		{
			return _autoSizeToContentWidth;
		}
		set
		{
			SetAndDoIfChanged(ref _autoSizeToContentWidth, value, Base.Invalidate);
		}
	}

	public bool AutoSizeToContentHeight
	{
		get
		{
			return _autoSizeToContentHeight;
		}
		set
		{
			SetAndDoIfChanged(ref _autoSizeToContentHeight, value, Base.Invalidate);
		}
	}

	public bool AutoSizeToContentWidthOnChildResize
	{
		get
		{
			return _autoSizeToContentWidthOnChildResize;
		}
		set
		{
			SetAndDoIfChanged(ref _autoSizeToContentWidthOnChildResize, value, Base.Invalidate);
		}
	}

	public bool AutoSizeToContentHeightOnChildResize
	{
		get
		{
			return _autoSizeToContentHeightOnChildResize;
		}
		set
		{
			SetAndDoIfChanged(ref _autoSizeToContentHeightOnChildResize, value, Base.Invalidate);
		}
	}

	public string? Text
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

	public Color? TextPaddingDebugColor { get; set; }

	public event GwenEventHandler<ValueChangedEventArgs<double>>? ValueChanged;

	public LabeledSlider(Base parent, string? name = null)
		: base(parent, name)
	{
		base.DockChildSpacing = new Padding(4);
		_label = new Label(this, "_label")
		{
			Alignment = new Alignments[1] { Alignments.CenterV },
			TextAlign = (Pos.Right | Pos.CenterV)
		};
		_slider = new Slider(this, "_slider");
		_sliderValue = new TextBoxNumeric(this, "_sliderValue")
		{
			Alignment = new Alignments[1] { Alignments.CenterV },
			AutoSizeToContents = true
		};
		_slider.ValueChanged += delegate(Base sender, ValueChangedEventArgs<double> arguments)
		{
			if (sender != _sliderValue)
			{
				double value = _slider.Value * _scale;
				if (_rounding > -1)
				{
					value = Math.Round(value, _rounding);
				}
				if (!value.Equals(_sliderValue.Value))
				{
					_slider.SetValue(value, skipEvents: true);
					_sliderValue.SetValue(value, skipEvents: true);
					this.ValueChanged?.Invoke(sender, new ValueChangedEventArgs<double>
					{
						Value = value
					});
				}
			}
		};
		_sliderValue.TextChanged += delegate(TextBox sender, ValueChangedEventArgs<string> _)
		{
			double num = _sliderValue.Value / _scale;
			double value = Math.Clamp(num, Minimum, Maximum);
			if (!value.Equals(num))
			{
				_sliderValue.Value = value;
			}
			_slider.Value = value;
			_slider.SetValue(value, skipEvents: true);
			this.ValueChanged?.Invoke(sender, new ValueChangedEventArgs<double>
			{
				Value = value
			});
		};
		AutoSizeToContentHeight = true;
		base.KeyboardInputEnabled = true;
		base.IsTabable = true;
	}

	private Point ComputeMinimumSizeForSliderValue(double? value = null)
	{
		string text = (value ?? Maximum).ToString(CultureInfo.CurrentUICulture);
		string valueFormatString = ValueFormatString;
		if (!string.IsNullOrWhiteSpace(valueFormatString))
		{
			text = string.Format(valueFormatString, text);
		}
		return base.Skin.Renderer.MeasureText(_sliderValue.Font, _sliderValue.FontSize, text) + _sliderValue.Padding + _sliderValue.Padding;
	}

	public void SetDraggerImage(IGameTexture? texture, ComponentState state)
	{
		_slider.SetDraggerImage(texture, state);
	}

	public IGameTexture? GetDraggerImage(ComponentState state)
	{
		return _slider.GetDraggerImage(state);
	}

	public void SetSound(string? sound, ButtonSoundState state)
	{
		_slider.SetSound(sound, state);
	}

	protected override void OnDisabledChanged(bool oldValue, bool newValue)
	{
		base.OnDisabledChanged(oldValue, newValue);
	}

	protected override void Layout(Intersect.Client.Framework.Gwen.Skin.Base skin)
	{
		if (_recomputeValueMinimumSize)
		{
			_sliderValue.MinimumSize = ComputeMinimumSizeForSliderValue(Maximum);
		}
		if (AutoSizeToContents)
		{
			SizeToChildren(AutoSizeToContentWidth, AutoSizeToContentHeight);
		}
		Orientation orientation = _slider.Orientation;
		switch (orientation)
		{
		case Orientation.LeftToRight:
			_label.Dock = Pos.Left;
			_label.Alignment = new Alignments[1] { Alignments.CenterV };
			_slider.Dock = Pos.Fill;
			_slider.Alignment = new Alignments[1] { Alignments.CenterV };
			_slider.Margin = new Margin(4, 0, 0, 0);
			_sliderValue.Dock = Pos.Right;
			_sliderValue.Alignment = new Alignments[1] { Alignments.CenterV };
			_sliderValue.Margin = new Margin(4, 0, 0, 0);
			break;
		case Orientation.RightToLeft:
			_label.Dock = Pos.Right | Pos.CenterV;
			_slider.Dock = Pos.Fill;
			_slider.Margin = new Margin(0, 0, 4, 0);
			_sliderValue.Dock = Pos.Left;
			_sliderValue.Margin = new Margin(0, 0, 4, 0);
			break;
		case Orientation.TopToBottom:
			_label.Dock = Pos.Top;
			_slider.Dock = Pos.Fill;
			_slider.Margin = new Margin(0, 4, 0, 0);
			_sliderValue.Dock = Pos.Bottom;
			_sliderValue.Margin = new Margin(0, 4, 0, 0);
			break;
		case Orientation.BottomToTop:
			_label.Dock = Pos.Bottom;
			_slider.Dock = Pos.Fill;
			_slider.Margin = new Margin(0, 0, 0, 4);
			_sliderValue.Dock = Pos.Top;
			_sliderValue.Margin = new Margin(0, 0, 0, 4);
			break;
		default:
			throw Exceptions.UnreachableInvalidEnum(orientation);
		}
		base.Layout(skin);
	}

	public override void SetToolTipText(string? text)
	{
		_label.SetToolTipText(text);
		_slider.SetToolTipText(text);
		_sliderValue.SetToolTipText(text);
	}

	protected override void OnSizeChanged(Point oldSize, Point newSize)
	{
		base.OnSizeChanged(oldSize, newSize);
	}

	protected override void OnBoundsChanged(Rectangle oldBounds, Rectangle newBounds)
	{
		base.OnBoundsChanged(oldBounds, newBounds);
	}

	protected override void OnChildSizeChanged(Base child, Point oldChildSize, Point newChildSize)
	{
		base.OnChildSizeChanged(child, oldChildSize, newChildSize);
		SizeToChildren(AutoSizeToContentWidthOnChildResize, AutoSizeToContentHeightOnChildResize);
	}

	public void SetRange(double min, double max)
	{
		double num = Math.Min(min, max);
		double num2 = Math.Max(min, max);
		if (num > Maximum)
		{
			Maximum = num2;
			Minimum = num;
		}
		else if (num2 < Minimum)
		{
			Minimum = num;
			Maximum = num2;
		}
		else
		{
			Minimum = num;
			Maximum = num2;
		}
	}

	public override void Focus(bool moveMouse = false)
	{
		base.Focus(moveMouse);
	}
}
