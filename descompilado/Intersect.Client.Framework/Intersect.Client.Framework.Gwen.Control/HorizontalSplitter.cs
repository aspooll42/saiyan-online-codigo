using System;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Gwen.ControlInternal;
using Intersect.Client.Framework.Gwen.Skin;

namespace Intersect.Client.Framework.Gwen.Control;

public class HorizontalSplitter : Base
{
	private readonly Base[] mSections;

	private readonly SplitterBar mVSplitter;

	private int mBarSize;

	private float mVVal;

	private int mZoomedSection;

	public bool IsZoomed => mZoomedSection != -1;

	public bool SplittersVisible
	{
		get
		{
			return mVSplitter.ShouldDrawBackground;
		}
		set
		{
			mVSplitter.ShouldDrawBackground = value;
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

	public HorizontalSplitter(Base parent)
		: base(parent)
	{
		mSections = new Base[2];
		mVSplitter = new SplitterBar(this);
		mVSplitter.SetPosition(0, 128);
		mVSplitter.Dragged += OnVerticalMoved;
		mVSplitter.Cursor = Cursors.SizeNs;
		mVVal = 0.5f;
		SetPanel(0, null);
		SetPanel(1, null);
		SplitterSize = 5;
		SplittersVisible = false;
		mZoomedSection = -1;
	}

	public void CenterPanels()
	{
		mVVal = 0.5f;
		Invalidate();
	}

	private void UpdateVSplitter()
	{
		mVSplitter.MoveTo(mVSplitter.X, (float)(base.Height - mVSplitter.Height) * mVVal);
	}

	protected void OnVerticalMoved(Base control, EventArgs args)
	{
		mVVal = CalculateValueVertical();
		Invalidate();
	}

	private float CalculateValueVertical()
	{
		return (float)mVSplitter.Y / (float)(base.Height - mVSplitter.Height);
	}

	protected override void Layout(Intersect.Client.Framework.Gwen.Skin.Base skin)
	{
		mVSplitter.SetSize(base.Width, mBarSize);
		UpdateVSplitter();
		if (mZoomedSection == -1)
		{
			if (mSections[0] != null)
			{
				mSections[0].SetBounds(0, 0, base.Width, mVSplitter.Y);
			}
			if (mSections[1] != null)
			{
				mSections[1].SetBounds(0, mVSplitter.Y + mBarSize, base.Width, base.Height - (mVSplitter.Y + mBarSize));
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
