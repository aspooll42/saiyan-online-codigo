using System;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Gwen.ControlInternal;
using Intersect.Client.Framework.Gwen.Skin;

namespace Intersect.Client.Framework.Gwen.Control;

public class VerticalSplitter : Base
{
	private readonly SplitterBar mHSplitter;

	private readonly Base[] mSections;

	private int mBarSize;

	private float mHVal;

	private int mZoomedSection;

	public bool IsZoomed => mZoomedSection != -1;

	public bool SplittersVisible
	{
		get
		{
			return mHSplitter.ShouldDrawBackground;
		}
		set
		{
			mHSplitter.ShouldDrawBackground = value;
		}
	}

	public int SplitterSize
	{
		get
		{
			return mBarSize;
		}
		set
		{
			mBarSize = value;
		}
	}

	public event GwenEventHandler<EventArgs> PanelZoomed;

	public event GwenEventHandler<EventArgs> PanelUnZoomed;

	public event GwenEventHandler<EventArgs> ZoomChanged;

	public VerticalSplitter(Base parent)
		: base(parent)
	{
		mSections = new Base[2];
		mHSplitter = new SplitterBar(this);
		mHSplitter.SetPosition(128, 0);
		mHSplitter.Dragged += OnHorizontalMoved;
		mHSplitter.Cursor = Cursors.SizeWe;
		mHVal = 0.5f;
		SetPanel(0, null);
		SetPanel(1, null);
		SplitterSize = 5;
		SplittersVisible = false;
		mZoomedSection = -1;
	}

	public void CenterPanels()
	{
		mHVal = 0.5f;
		Invalidate();
	}

	public void SetHValue(float value)
	{
		if (value <= 1f || value >= 0f)
		{
			mHVal = value;
		}
	}

	private void UpdateHSplitter()
	{
		mHSplitter.MoveTo((float)(base.Width - mHSplitter.Width) * mHVal, mHSplitter.Y);
	}

	protected void OnHorizontalMoved(Base control, EventArgs args)
	{
		mHVal = CalculateValueHorizontal();
		Invalidate();
	}

	private float CalculateValueHorizontal()
	{
		return (float)mHSplitter.X / (float)(base.Width - mHSplitter.Width);
	}

	protected override void Layout(Intersect.Client.Framework.Gwen.Skin.Base skin)
	{
		mHSplitter.SetSize(mBarSize, base.Height);
		UpdateHSplitter();
		if (mZoomedSection == -1)
		{
			if (mSections[0] != null)
			{
				mSections[0].SetBounds(0, 0, mHSplitter.X, base.Height);
			}
			if (mSections[1] != null)
			{
				mSections[1].SetBounds(mHSplitter.X + mBarSize, 0, base.Width - (mHSplitter.X + mBarSize), base.Height);
			}
		}
		else
		{
			mSections[mZoomedSection].SetBounds(0, 0, base.Width, base.Height);
		}
	}

	public void SetPanel(int index, Base panel)
	{
		mSections[index] = panel;
		if (panel != null)
		{
			panel.Dock = Pos.None;
			panel.Parent = this;
		}
		Invalidate();
	}

	public Base GetPanel(int index)
	{
		return mSections[index];
	}

	protected void OnZoomChanged()
	{
		if (this.ZoomChanged != null)
		{
			this.ZoomChanged(this, EventArgs.Empty);
		}
		if (mZoomedSection == -1)
		{
			if (this.PanelUnZoomed != null)
			{
				this.PanelUnZoomed(this, EventArgs.Empty);
			}
		}
		else if (this.PanelZoomed != null)
		{
			this.PanelZoomed(this, EventArgs.Empty);
		}
	}

	public void Zoom(int section)
	{
		UnZoom();
		if (mSections[section] != null)
		{
			for (int i = 0; i < 2; i++)
			{
				if (i != section && mSections[i] != null)
				{
					mSections[i].IsHidden = true;
				}
			}
			mZoomedSection = section;
			Invalidate();
		}
		OnZoomChanged();
	}

	public void UnZoom()
	{
		mZoomedSection = -1;
		for (int i = 0; i < 2; i++)
		{
			if (mSections[i] != null)
			{
				mSections[i].IsHidden = false;
			}
		}
		Invalidate();
		OnZoomChanged();
	}
}
