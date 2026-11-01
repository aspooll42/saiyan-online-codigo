using System;
using System.Globalization;
using Intersect.Client.Framework.Gwen.Control.EventArguments;

namespace Intersect.Client.Framework.Gwen.Control;

public class TextBoxNumeric : TextBox, INumericInput
{
	private double _value;

	private double _maximum = double.NaN;

	private double _minimum = double.NaN;

	public double Maximum
	{
		get
		{
			return _maximum;
		}
		set
		{
			SetRange(_minimum, value);
		}
	}

	public double Minimum
	{
		get
		{
			return _minimum;
		}
		set
		{
			SetRange(value, _maximum);
		}
	}

	public virtual double Value
	{
		get
		{
			return _value;
		}
		set
		{
			SetValue(value);
		}
	}

	public event GwenEventHandler<TextBoxNumeric, ValueChangedEventArgs<double>>? ValueChanged;

	public TextBoxNumeric(Base parent, string? name = null)
		: base(parent, name)
	{
		base.AutoSizeToContents = false;
		InsertText("0", skipEvents: true);
	}

	protected virtual bool IsTextAllowed(string? str)
	{
		str = str?.Trim();
		if (string.IsNullOrEmpty(str))
		{
			return true;
		}
		double minimum = _minimum;
		if (str == "-")
		{
			if (!double.IsNaN(minimum))
			{
				return minimum < 0.0;
			}
			return true;
		}
		if (!double.TryParse(str, out var result))
		{
			return false;
		}
		if (!double.IsNaN(minimum) && minimum > result)
		{
			return false;
		}
		double maximum = _maximum;
		if (!double.IsNaN(maximum))
		{
			return maximum >= result;
		}
		return true;
	}

	public void SetRange(double minimum, double maximum, bool skipEvents = false)
	{
		double minimum2 = _minimum;
		double maximum2 = _maximum;
		bool flag = false;
		if (!maximum.Equals(maximum2))
		{
			flag = true;
			_maximum = maximum;
			if (maximum < _value)
			{
				SetValue(maximum, skipEvents);
			}
		}
		if (!minimum.Equals(minimum2))
		{
			flag = true;
			_minimum = minimum;
			if (minimum > _value)
			{
				SetValue(minimum, skipEvents);
			}
		}
		if (flag && !skipEvents)
		{
			OnRangeChanged(minimum2, maximum2, minimum, maximum);
		}
	}

	protected virtual void OnRangeChanged(double oldMinimum, double oldMaximum, double newMinimum, double newMaximum)
	{
	}

	protected override bool IsTextAllowed(string text, int position)
	{
		string text2 = Text?.Insert(position, text.Trim()).Trim();
		return IsTextAllowed(text2 ?? string.Empty);
	}

	protected override void OnTextChanged()
	{
		string text = Text;
		if (string.IsNullOrWhiteSpace(text) || text == "-")
		{
			Value = 0.0;
		}
		else
		{
			Value = double.Parse(text);
		}
		base.OnTextChanged();
	}

	public override void SetText(string? text, bool doEvents = true)
	{
		text = text?.Trim();
		if (IsTextAllowed(text))
		{
			base.SetText(text, doEvents);
		}
	}

	public void SetValue(double value, bool skipEvents = false)
	{
		double num = value;
		double minimum = _minimum;
		if (!double.IsNaN(minimum))
		{
			num = Math.Max(num, minimum);
		}
		double maximum = _maximum;
		if (!double.IsNaN(maximum))
		{
			num = Math.Min(num, maximum);
		}
		if (!num.Equals(_value))
		{
			_value = num;
			SetText(num.ToString(CultureInfo.CurrentCulture), !skipEvents);
			if (!skipEvents)
			{
				this.ValueChanged?.Invoke(this, new ValueChangedEventArgs<double>
				{
					Value = num
				});
			}
		}
	}
}
