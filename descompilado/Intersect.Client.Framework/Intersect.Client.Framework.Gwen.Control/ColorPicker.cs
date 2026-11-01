using System;
using Intersect.Client.Framework.Gwen.ControlInternal;
using Intersect.Client.Framework.Gwen.Skin;

namespace Intersect.Client.Framework.Gwen.Control;

public class ColorPicker : Base, IColorPicker
{
	private Color mColor;

	public int R
	{
		get
		{
			return mColor.R;
		}
		set
		{
			mColor = Color.FromArgb(mColor.A, value, mColor.G, mColor.B);
		}
	}

	public int G
	{
		get
		{
			return mColor.G;
		}
		set
		{
			mColor = Color.FromArgb(mColor.A, mColor.R, value, mColor.B);
		}
	}

	public int B
	{
		get
		{
			return mColor.B;
		}
		set
		{
			mColor = Color.FromArgb(mColor.A, mColor.R, mColor.G, value);
		}
	}

	public int A
	{
		get
		{
			return mColor.A;
		}
		set
		{
			mColor = Color.FromArgb(value, mColor.R, mColor.G, mColor.B);
		}
	}

	public bool AlphaVisible
	{
		get
		{
			GroupBox groupBox = FindChildByName("Alphagroupbox", recursive: true) as GroupBox;
			return !groupBox.IsHidden;
		}
		set
		{
			GroupBox groupBox = FindChildByName("Alphagroupbox", recursive: true) as GroupBox;
			groupBox.IsHidden = !value;
			Invalidate();
		}
	}

	public Color SelectedColor
	{
		get
		{
			return mColor;
		}
		set
		{
			mColor = value;
			UpdateControls();
		}
	}

	public event GwenEventHandler<EventArgs> ColorChanged;

	public ColorPicker(Base parent)
		: base(parent)
	{
		base.MouseInputEnabled = true;
		SetSize(256, 150);
		CreateControls();
		SelectedColor = Color.FromArgb(255, 50, 60, 70);
	}

	private void CreateColorControl(string name, int y)
	{
		GroupBox groupBox = new GroupBox(this);
		groupBox.SetPosition(10, y);
		groupBox.SetText(name);
		groupBox.SetSize(160, 35);
		groupBox.Name = name + "groupbox";
		ColorDisplay colorDisplay = new ColorDisplay(groupBox)
		{
			Name = name,
		};
		colorDisplay.SetBounds(0, 10, 12, 12);
		TextBoxNumeric textBoxNumeric = new TextBoxNumeric(groupBox)
		{
			Name = name + "Box",
		};
		textBoxNumeric.SetPosition(105, 7);
		textBoxNumeric.SetSize(26, 16);
		textBoxNumeric.SelectAllOnFocus = true;
		textBoxNumeric.TextChanged += NumericTyped;
		Slider slider = new Slider(groupBox)
		{
			Orientation = Orientation.LeftToRight
		};
		slider.SetPosition(17, 10);
		slider.SetRange(0.0, 255.0);
		slider.SetSize(80, 12);
		slider.Name = name + "Slider";
		slider.ValueChanged += SlidersMoved;
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
			if (textBoxNumeric.Name.Contains("Red"))
			{
				R = num;
			}
			if (textBoxNumeric.Name.Contains("Green"))
			{
				G = num;
			}
			if (textBoxNumeric.Name.Contains("Blue"))
			{
				B = num;
			}
			if (textBoxNumeric.Name.Contains("Alpha"))
			{
				A = num;
			}
			UpdateControls();
		}
	}

	private void CreateControls()
	{
		CreateColorControl("Red", 5);
		CreateColorControl("Green", 40);
		CreateColorControl("Blue", 75);
		CreateColorControl("Alpha", 110);
		GroupBox groupBox = new GroupBox(this);
		groupBox.SetPosition(180, 40);
		groupBox.SetSize(60, 60);
		groupBox.SetText("Result");
		groupBox.Name = "ResultGroupBox";
		ColorDisplay colorDisplay = new ColorDisplay(groupBox)
		{
			Name = "Result",
		};
		colorDisplay.SetBounds(0, 10, 32, 32);
	}

	private void UpdateColorControls(string name, Color color, int value)
	{
		if (FindChildByName(name, recursive: true) is ColorDisplay colorDisplay)
		{
			colorDisplay.Color = color;
		}
		if (FindChildByName(name + "Slider", recursive: true) is Slider slider)
		{
			slider.Value = value;
		}
		if (FindChildByName(name + "Box", recursive: true) is TextBoxNumeric textBoxNumeric)
		{
			textBoxNumeric.Value = value;
		}
	}

	private void UpdateControls()
	{
		UpdateColorControls("Red", Color.FromArgb(255, SelectedColor.R, 0, 0), SelectedColor.R);
		UpdateColorControls("Green", Color.FromArgb(255, 0, SelectedColor.G, 0), SelectedColor.G);
		UpdateColorControls("Blue", Color.FromArgb(255, 0, 0, SelectedColor.B), SelectedColor.B);
		UpdateColorControls("Alpha", Color.FromArgb(SelectedColor.A, 255, 255, 255), SelectedColor.A);
		ColorDisplay colorDisplay = FindChildByName("Result", recursive: true) as ColorDisplay;
		colorDisplay.Color = SelectedColor;
		if (this.ColorChanged != null)
		{
			this.ColorChanged(this, EventArgs.Empty);
		}
	}

	private void SlidersMoved(Base control, EventArgs args)
	{
		if (control is Slider slider)
		{
			string colorFromName = GetColorFromName(slider.Name);
			SetColorByName(colorFromName, (int)slider.Value);
		}
		UpdateControls();
	}

	protected override void Layout(Intersect.Client.Framework.Gwen.Skin.Base skin)
	{
		base.Layout(skin);
		SizeToChildren(resizeX: false);
		SetSize(base.Width, base.Height + 5);
		if (FindChildByName("ResultGroupBox", recursive: true) is GroupBox groupBox)
		{
			groupBox.SetPosition(groupBox.X, (float)base.Height * 0.5f - (float)groupBox.Height * 0.5f);
		}
	}

	private int GetColorByName(string colorName)
	{
		return colorName switch
		{
			"Red" => SelectedColor.R, 
			"Green" => SelectedColor.G, 
			"Blue" => SelectedColor.B, 
			"Alpha" => SelectedColor.A, 
			_ => 0, 
		};
	}

	private static string GetColorFromName(string? name)
	{
		if (string.IsNullOrWhiteSpace(name))
		{
			return string.Empty;
		}
		if (name.Contains("Red"))
		{
			return "Red";
		}
		if (name.Contains("Green"))
		{
			return "Green";
		}
		if (name.Contains("Blue"))
		{
			return "Blue";
		}
		if (name.Contains("Alpha"))
		{
			return "Alpha";
		}
		return string.Empty;
	}

	private void SetColorByName(string colorName, int colorValue)
	{
		switch (colorName)
		{
		case "Red":
			R = colorValue;
			break;
		case "Green":
			G = colorValue;
			break;
		case "Blue":
			B = colorValue;
			break;
		case "Alpha":
			A = colorValue;
			break;
		}
	}
}
