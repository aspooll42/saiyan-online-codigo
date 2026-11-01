using System;
using Intersect.Client.Framework.Gwen.Skin;

namespace Intersect.Client.Framework.Gwen.Control.Layout;

public class Splitter : Base
{
	private readonly Base[] mPanel;

	private readonly bool[] mScale;

	public Splitter(Base parent)
		: base(parent)
	{
		mPanel = new Base[2];
		mScale = new bool[2];
		mScale[0] = true;
		mScale[1] = true;
	}

	public void SetPanel(int panelIndex, Base panel, bool noScale = false)
	{
		if (panelIndex < 0 || panelIndex > 1)
		{
			throw new ArgumentException("Invalid panel index", "panelIndex");
		}
		mPanel[panelIndex] = panel;
		mScale[panelIndex] = !noScale;
		if (mPanel[panelIndex] != null)
		{
			mPanel[panelIndex].Parent = this;
		}
	}

	private Base GetPanel(int panelIndex)
	{
		if (panelIndex < 0 || panelIndex > 1)
		{
			throw new ArgumentException("Invalid panel index", "panelIndex");
		}
		return mPanel[panelIndex];
	}

	protected override void Layout(Intersect.Client.Framework.Gwen.Skin.Base skin)
	{
		LayoutVertical(skin);
	}

	protected virtual void LayoutVertical(Intersect.Client.Framework.Gwen.Skin.Base skin)
	{
		int width = base.Width;
		int height = base.Height;
		if (mPanel[0] != null)
		{
			Margin margin = mPanel[0].Margin;
			if (mScale[0])
			{
				mPanel[0].SetBounds(margin.Left, margin.Top, width - margin.Left - margin.Right, (float)height * 0.5f - (float)margin.Top - (float)margin.Bottom);
			}
			else
			{
				mPanel[0].Position(Pos.Center, 0, (int)((float)height * -0.25f));
			}
		}
		if (mPanel[1] != null)
		{
			Margin margin2 = mPanel[1].Margin;
			if (mScale[1])
			{
				mPanel[1].SetBounds(margin2.Left, (float)margin2.Top + (float)height * 0.5f, width - margin2.Left - margin2.Right, (float)height * 0.5f - (float)margin2.Top - (float)margin2.Bottom);
			}
			else
			{
				mPanel[1].Position(Pos.Center, 0, (int)((float)height * 0.25f));
			}
		}
	}

	protected virtual void LayoutHorizontal(Intersect.Client.Framework.Gwen.Skin.Base skin)
	{
		throw new NotImplementedException();
	}
}
