using System;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Gwen.ControlInternal;
using Intersect.Client.Framework.Gwen.Skin;

namespace Intersect.Client.Framework.Gwen.Control;

public class CrossSplitter : Base
{
	private readonly SplitterBar mCSplitter;

	private readonly SplitterBar mHSplitter;

	private readonly Base[] mSections;

	private readonly SplitterBar mVSplitter;

	private int mBarSize;

	private float mHVal;

	private float mVVal;

	private int mZoomedSection;

	public bool IsZoomed => mZoomedSection != -1;

	public bool SplittersVisible
	{
		get
		{
			return mCSplitter.ShouldDrawBackground;
		}
		set
		{
			mCSplitter.ShouldDrawBackground = value;
			mVSplitter.ShouldDrawBackground = value;
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

	public CrossSplitter(Base parent)
		: base(parent)
	{
		mSections = new Base[4];
		mVSplitter = new SplitterBar(this);
		mVSplitter.SetPosition(0, 128);
		mVSplitter.Dragged += OnVerticalMoved;
		mVSplitter.Cursor = Cursors.SizeNs;
		mHSplitter = new SplitterBar(this);
		mHSplitter.SetPosition(128, 0);
		mHSplitter.Dragged += OnHorizontalMoved;
		mHSplitter.Cursor = Cursors.SizeWe;
		mCSplitter = new SplitterBar(this);
		mCSplitter.SetPosition(128, 128);
		mCSplitter.Dragged += OnCenterMoved;
		mCSplitter.Cursor = Cursors.SizeAll;
		mHVal = 0.5f;
		mVVal = 0.5f;
		SetPanel(0, null);
		SetPanel(1, null);
		SetPanel(2, null);
		SetPanel(3, null);
		SplitterSize = 5;
		SplittersVisible = false;
		mZoomedSection = -1;
	}

	public void CenterPanels()
	{
		mHVal = 0.5f;
		mVVal = 0.5f;
		Invalidate();
	}

	private void UpdateVSplitter()
	{
		mVSplitter.MoveTo(mVSplitter.X, (float)(base.Height - mVSplitter.Height) * mVVal);
	}

	private void UpdateHSplitter()
	{
		mHSplitter.MoveTo((float)(base.Width - mHSplitter.Width) * mHVal, mHSplitter.Y);
	}

	private void UpdateCSplitter()
	{
		mCSplitter.MoveTo((float)(base.Width - mCSplitter.Width) * mHVal, (float)(base.Height - mCSplitter.Height) * mVVal);
	}

	protected void OnCenterMoved(Base control, EventArgs args)
	{
		CalculateValueCenter();
		Invalidate();
	}

	protected void OnVerticalMoved(Base control, EventArgs args)
	{
		mVVal = CalculateValueVertical();
		Invalidate();
	}

	protected void OnHorizontalMoved(Base control, EventArgs args)
	{
		mHVal = CalculateValueHorizontal();
		Invalidate();
	}

	private void CalculateValueCenter()
	{
		mHVal = (float)mCSplitter.X / (float)(base.Width - mCSplitter.Width);
		mVVal = (float)mCSplitter.Y / (float)(base.Height - mCSplitter.Height);
	}

	private float CalculateValueVertical()
	{
		return (float)mVSplitter.Y / (float)(base.Height - mVSplitter.Height);
	}

	private float CalculateValueHorizontal()
	{
		return (float)mHSplitter.X / (float)(base.Width - mHSplitter.Width);
	}

	protected override void Layout(Intersect.Client.Framework.Gwen.Skin.Base skin)
	{
		mVSplitter.SetSize(base.Width, mBarSize);
		mHSplitter.SetSize(mBarSize, base.Height);
		mCSplitter.SetSize(mBarSize, mBarSize);
		UpdateVSplitter();
		UpdateHSplitter();
		UpdateCSplitter();
		if (mZoomedSection == -1)
		{
			if (mSections[0] != null)
			{
				mSections[0].SetBounds(0, 0, mHSplitter.X, mVSplitter.Y);
			}
			if (mSections[1] != null)
			{
				mSections[1].SetBounds(mHSplitter.X + mBarSize, 0, base.Width - (mHSplitter.X + mBarSize), mVSplitter.Y);
			}
			if (mSections[2] != null)
			{
				mSections[2].SetBounds(0, mVSplitter.Y + mBarSize, mHSplitter.X, base.Height - (mVSplitter.Y + mBarSize));
			}
			if (mSections[3] != null)
			{
				mSections[3].SetBounds(mHSplitter.X + mBarSize, mVSplitter.Y + mBarSize, base.Width - (mHSplitter.X + mBarSize), base.Height - (mVSplitter.Y + mBarSize));
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
			for (int i = 0; i < 4; i++)
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
		for (int i = 0; i < 4; i++)
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
