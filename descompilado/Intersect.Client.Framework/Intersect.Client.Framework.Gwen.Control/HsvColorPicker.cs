using System;
using Intersect.Client.Framework.Gwen.ControlInternal;

namespace Intersect.Client.Framework.Gwen.Control;

public class HsvColorPicker : Base, IColorPicker
{
	private readonly ColorDisplay mAfter;

	private readonly ColorDisplay mBefore;

	private readonly ColorSlider mColorSlider;

	private readonly ColorLerpBox mLerpBox;

	public Color DefaultColor
	{
		get
		{
			return mBefore.Color;
		}
		set
		{
			mBefore.Color = value;
		}
	}

	public Color SelectedColor => mLerpBox.SelectedColor;

	public event GwenEventHandler<EventArgs> ColorChanged;

	public HsvColorPicker(Base parent)
		: base(parent)
	{
		base.MouseInputEnabled = true;
		SetSize(256, 128);
		mLerpBox = new ColorLerpBox(this);
		mLerpBox.ColorChanged += ColorBoxChanged;
		mLerpBox.Dock = Pos.Left;
		mColorSlider = new ColorSlider(this);
		mColorSlider.SetPosition(mLerpBox.Width + 15, 5);
		mColorSlider.ColorChanged += ColorSliderChanged;
		mColorSlider.Dock = Pos.Left;
		mAfter = new ColorDisplay(this);
		mAfter.SetSize(48, 24);
		mAfter.SetPosition(mColorSlider.X + mColorSlider.Width + 15, 5);
		mBefore = new ColorDisplay(this);
		mBefore.SetSize(48, 24);
		mBefore.SetPosition(mAfter.X, 28);
		int x = mBefore.X;
		int num = mBefore.Y + 30;
		Label label = new Label(this);
		label.SetText("R:");
		label.SizeToContents();
		label.SetPosition(x, num);
		TextBoxNumeric textBoxNumeric = new TextBoxNumeric(this)
		{
			Name = "RedBox",
		};
		textBoxNumeric.SetPosition(x + 15, num - 1);
		textBoxNumeric.SetSize(26, 16);
		textBoxNumeric.SelectAllOnFocus = true;
		textBoxNumeric.TextChanged += NumericTyped;
		num += 20;
		Label label2 = new Label(this);
		label2.SetText("G:");
		label2.SizeToContents();
		label2.SetPosition(x, num);
		TextBoxNumeric textBoxNumeric2 = new TextBoxNumeric(this)
		{
			Name = "GreenBox",
		};
		textBoxNumeric2.SetPosition(x + 15, num - 1);
		textBoxNumeric2.SetSize(26, 16);
		textBoxNumeric2.SelectAllOnFocus = true;
		textBoxNumeric2.TextChanged += NumericTyped;
		num += 20;
		Label label3 = new Label(this);
		label3.SetText("B:");
		label3.SizeToContents();
		label3.SetPosition(x, num);
		TextBoxNumeric textBoxNumeric3 = new TextBoxNumeric(this)
		{
			Name = "BlueBox",
		};
		textBoxNumeric3.SetPosition(x + 15, num - 1);
		textBoxNumeric3.SetSize(26, 16);
		textBoxNumeric3.SelectAllOnFocus = true;
		textBoxNumeric3.TextChanged += NumericTyped;
		SetColor(DefaultColor);
	}

	private void NumericTyped(Base control, EventArgs args)
	{
		if (control is TextBoxNumeric textBoxNumeric && !(textBoxNumeric.Text == string.Empty))
		{
			int num = (int)textBoxNumeric.Value;
			if (num < 0)
			{
				num = 0;
			}
			if (num > 255)
			{
				num = 255;
			}
			Color color = SelectedColor;
			if (textBoxNumeric.Name.Contains("Red"))
			{
				color = Color.FromArgb(SelectedColor.A, num, SelectedColor.G, SelectedColor.B);
			}
			else if (textBoxNumeric.Name.Contains("Green"))
			{
				color = Color.FromArgb(SelectedColor.A, SelectedColor.R, num, SelectedColor.B);
			}
			else if (textBoxNumeric.Name.Contains("Blue"))
			{
				color = Color.FromArgb(SelectedColor.A, SelectedColor.R, SelectedColor.G, num);
			}
			else if (textBoxNumeric.Name.Contains("Alpha"))
			{
				color = Color.FromArgb(num, SelectedColor.R, SelectedColor.G, SelectedColor.B);
			}
			SetColor(color);
		}
	}

	private void UpdateControls(Color color)
	{
		if (FindChildByName("RedBox") is TextBoxNumeric textBoxNumeric)
		{
			textBoxNumeric.SetText(color.R.ToString(), doEvents: false);
		}
		if (FindChildByName("GreenBox") is TextBoxNumeric textBoxNumeric2)
		{
			textBoxNumeric2.SetText(color.G.ToString(), doEvents: false);
		}
		if (FindChildByName("BlueBox") is TextBoxNumeric textBoxNumeric3)
		{
			textBoxNumeric3.SetText(color.B.ToString(), doEvents: false);
		}
		mAfter.Color = color;
		if (this.ColorChanged != null)
		{
			this.ColorChanged(this, EventArgs.Empty);
		}
	}

	public void SetColor(Color color, bool onlyHue = false, bool reset = false)
	{
		UpdateControls(color);
		if (reset)
		{
			mBefore.Color = color;
		}
		mColorSlider.SelectedColor = color;
		mLerpBox.SetColor(color, onlyHue);
		mAfter.Color = color;
	}

	private void ColorBoxChanged(Base control, EventArgs args)
	{
		UpdateControls(SelectedColor);
		Invalidate();
	}

	private void ColorSliderChanged(Base control, EventArgs args)
	{
		if (mLerpBox != null)
		{
			mLerpBox.SetColor(mColorSlider.SelectedColor);
		}
		Invalidate();
	}
}
