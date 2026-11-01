using System;
using Intersect.Client.Framework.Gwen.Control;

namespace Intersect.Client.Interface.Game.DescriptionWindows.Components;

public class KeyValueRowComponent : ComponentBase
{
	private readonly Label _keyLabel;

	private readonly Label _valueLabel;

	public KeyValueRowComponent(Base parent)
		: this(parent, string.Empty, string.Empty)
	{
	}

	public KeyValueRowComponent(Base parent, string key, string value)
		: base(parent, "KeyValueRow")
	{
		_keyLabel = new Label(this, "Key")
		{
			Text = key
		};
		_valueLabel = new Label(this, "Value")
		{
			Text = value
		};
	}

	public void AlignValueRight(int width)
	{
		_keyLabel.SizeToContents();
		_valueLabel.SizeToContents();
		_keyLabel.SetPosition(_keyLabel.X, Math.Max(0, (base.Height - _keyLabel.Height) / 2));
		int x = Math.Max(_keyLabel.X + _keyLabel.Width + 12, width - _valueLabel.Width);
		_valueLabel.SetPosition(x, Math.Max(0, (base.Height - _valueLabel.Height) / 2));
	}

	public void SetKeyTextColor(Color color)
	{
		_keyLabel.SetTextColor(color, ComponentState.Normal);
	}

	public void SetValueTextColor(Color color)
	{
		_valueLabel.SetTextColor(color, ComponentState.Normal);
	}
}
