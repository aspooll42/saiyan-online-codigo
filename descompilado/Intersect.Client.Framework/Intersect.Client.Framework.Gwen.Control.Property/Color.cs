using System;
using Intersect.Client.Framework.Gwen.ControlInternal;
using Intersect.Client.Framework.Gwen.Input;

namespace Intersect.Client.Framework.Gwen.Control.Property;

public class Color : Text
{
	protected readonly ColorButton mButton;

	public override string Value
	{
		get
		{
			return mTextBox.Text;
		}
		set
		{
			base.Value = value;
		}
	}

	public override bool IsEditing => mTextBox == InputHandler.KeyboardFocus;

	public Color(Intersect.Client.Framework.Gwen.Control.Base parent)
		: base(parent)
	{
		mButton = new ColorButton(mTextBox);
		mButton.Dock = Pos.Right;
		mButton.Width = 20;
		mButton.Margin = new Margin(1, 1, 1, 2);
		mButton.Clicked += OnButtonPressed;
	}

	protected virtual void OnButtonPressed(Intersect.Client.Framework.Gwen.Control.Base control, EventArgs args)
	{
		Menu menu = new Menu(base.Canvas);
		menu.SetSize(256, 180);
		menu.DeleteOnClose = true;
		menu.IconMarginDisabled = true;
		HsvColorPicker hsvColorPicker = new HsvColorPicker(menu)
		{
			Dock = Pos.Fill,
		};
		hsvColorPicker.SetSize(256, 128);
		string[] array = mTextBox.Text.Split(' ');
		hsvColorPicker.SetColor(GetColorFromText(), onlyHue: false, reset: true);
		hsvColorPicker.ColorChanged += OnColorChanged;
		menu.Open(Pos.Right | Pos.Top);
	}

	protected virtual void OnColorChanged(Intersect.Client.Framework.Gwen.Control.Base control, EventArgs args)
	{
		HsvColorPicker hsvColorPicker = control as HsvColorPicker;
		SetTextFromColor(hsvColorPicker.SelectedColor);
		DoChanged();
	}

	public override void SetValue(string value, bool fireEvents = false)
	{
		mTextBox.SetText(value, fireEvents);
	}

	private void SetTextFromColor(Intersect.Color color)
	{
		mTextBox.Text = $"{color.R} {color.G} {color.B}";
	}

	private Intersect.Color GetColorFromText()
	{
		string[] array = mTextBox.Text.Split(' ');
		byte result = 0;
		byte result2 = 0;
		byte result3 = 0;
		byte a = byte.MaxValue;
		if (array.Length != 0 && array[0].Length > 0)
		{
			byte.TryParse(array[0], out result);
		}
		if (array.Length > 1 && array[1].Length > 0)
		{
			byte.TryParse(array[1], out result2);
		}
		if (array.Length > 2 && array[2].Length > 0)
		{
			byte.TryParse(array[2], out result3);
		}
		return Intersect.Color.FromArgb(a, result, result2, result3);
	}

	protected override void DoChanged()
	{
		base.DoChanged();
		mButton.Color = GetColorFromText();
	}
}
