using Intersect.Client.Framework.Gwen.Skin;

namespace Intersect.Client.Framework.Gwen.Control;

public class ProgressBar : Label
{
	private bool mAutoLabel;

	private bool mHorizontal;

	private float mProgress;

	public bool IsHorizontal
	{
		get
		{
			return mHorizontal;
		}
		set
		{
			mHorizontal = value;
		}
	}

	public float Value
	{
		get
		{
			return mProgress;
		}
		set
		{
			if (value < 0f)
			{
				value = 0f;
			}
			if (value > 1f)
			{
				value = 1f;
			}
			mProgress = value;
			if (mAutoLabel)
			{
				Text = (int)(mProgress * 100f) + "%";
			}
		}
	}

	public bool AutoLabel
	{
		get
		{
			return mAutoLabel;
		}
		set
		{
			mAutoLabel = value;
		}
	}

	public ProgressBar(Base parent)
		: base(parent)
	{
		base.AutoSizeToContents = false;
		SetSize(128, 32);
		base.Padding = Padding.Three;
		IsHorizontal = true;
		base.TextAlign = Pos.Center;
		mProgress = 0f;
		mAutoLabel = true;
	}

	protected override void Render(Intersect.Client.Framework.Gwen.Skin.Base skin)
	{
		skin.DrawProgressBar(this, mHorizontal, mProgress);
	}
}
