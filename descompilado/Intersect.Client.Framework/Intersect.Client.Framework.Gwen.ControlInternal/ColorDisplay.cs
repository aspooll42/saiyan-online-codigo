using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.Framework.Gwen.Skin;

namespace Intersect.Client.Framework.Gwen.ControlInternal;

public class ColorDisplay : Intersect.Client.Framework.Gwen.Control.Base
{
	private Color mColor;

	public Color Color
	{
		get
		{
			return mColor;
		}
		set
		{
			mColor = value;
		}
	}

	public int R
	{
		get
		{
			return mColor.R;
		}
		set
		{
			mColor = Intersect.Color.FromArgb(mColor.A, value, mColor.G, mColor.B);
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
			mColor = Intersect.Color.FromArgb(mColor.A, mColor.R, value, mColor.B);
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
			mColor = Intersect.Color.FromArgb(mColor.A, mColor.R, mColor.G, value);
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
			mColor = Intersect.Color.FromArgb(value, mColor.R, mColor.G, mColor.B);
		}
	}

	public ColorDisplay(Intersect.Client.Framework.Gwen.Control.Base parent)
		: base(parent)
	{
		SetSize(32, 32);
		mColor = Intersect.Color.FromArgb(255, 255, 0, 0);
	}

	protected override void Render(Intersect.Client.Framework.Gwen.Skin.Base skin)
	{
		skin.DrawColorDisplay(this, mColor);
	}
}
