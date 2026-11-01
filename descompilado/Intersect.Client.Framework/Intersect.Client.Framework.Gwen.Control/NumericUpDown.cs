using System;
using Intersect.Client.Framework.Gwen.Control.EventArguments;
using Intersect.Client.Framework.Gwen.Control.Layout;
using Intersect.Client.Framework.Gwen.ControlInternal;
using Intersect.Client.Framework.Input;

namespace Intersect.Client.Framework.Gwen.Control;

public class NumericUpDown : TextBoxNumeric
{
	private readonly UpDownButtonDown _downButton;

	private readonly UpDownButtonUp _upButton;

	private readonly Splitter _splitter;

	public double Step { get; set; }

	public NumericUpDown(Base parent)
		: base(parent)
	{
		SetSize(100, 20);
		_splitter = new Splitter(this);
		_splitter.Dock = Pos.Right;
		_splitter.SetSize(13, 13);
		_upButton = new UpDownButtonUp(_splitter);
		_upButton.Clicked += UpButtonOnClicked;
		_upButton.IsTabable = false;
		_splitter.SetPanel(0, _upButton);
		_downButton = new UpDownButtonDown(_splitter);
		_downButton.Clicked += DownButtonOnClicked;
		_downButton.IsTabable = false;
		_downButton.Padding = new Padding(0, 1, 1, 0);
		_splitter.SetPanel(1, _downButton);
		SetRange(0.0, 100.0, skipEvents: true);
	}

	protected override bool OnKeyUp(bool down)
	{
		if (down)
		{
			UpButtonOnClicked(null, EventArgs.Empty);
		}
		return true;
	}

	protected override bool OnKeyDown(bool down)
	{
		if (down)
		{
			DownButtonOnClicked(this, new MouseButtonState(MouseButton.Left, 0, 0, isPressed: true));
		}
		return true;
	}

	protected virtual void UpButtonOnClicked(Base control, EventArgs args)
	{
		Value += Step;
	}

	protected virtual void DownButtonOnClicked(Base control, MouseButtonState args)
	{
		Value -= Step;
	}
}
