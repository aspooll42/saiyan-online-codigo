using System;
using Intersect.Client.Framework.Gwen.Input;
using Intersect.Client.Framework.Gwen.Skin;
using Intersect.Client.Framework.Input;

namespace Intersect.Client.Framework.Gwen.Control;

public class HorizontalScrollBar : ScrollBar
{
	public override int BarSize
	{
		get
		{
			return mBar.Width;
		}
		set
		{
			mBar.Width = value;
		}
	}

	public override int BarPos => mBar.X - base.Height;

	public override bool IsHorizontal => true;

	public override int ButtonSize => base.Height;

	public override float NudgeAmount
	{
		get
		{
			if (!mDepressed)
			{
				return base.NudgeAmount;
			}
			return mViewableContentSize / mContentSize;
		}
		set
		{
			base.NudgeAmount = value;
		}
	}

	public HorizontalScrollBar(Base parent)
		: base(parent)
	{
		mBar.IsHorizontal = true;
		mScrollButton[0].SetDirectionLeft();
		mScrollButton[0].Clicked += NudgeLeft;
		mScrollButton[1].SetDirectionRight();
		mScrollButton[1].Clicked += NudgeRight;
		mBar.Dragged += OnBarMoved;
	}

	protected override void Layout(Intersect.Client.Framework.Gwen.Skin.Base skin)
	{
		base.Layout(skin);
		mScrollButton[0].Width = base.Height;
		mScrollButton[0].Dock = Pos.Left;
		mScrollButton[1].Width = base.Height;
		mScrollButton[1].Dock = Pos.Right;
		mBar.Height = ButtonSize;
		mBar.Margin = new Margin(ButtonSize, 0, ButtonSize, 0);
		float num = mViewableContentSize / mContentSize * (float)(base.Width - ButtonSize * 2);
		if (num < (float)ButtonSize * 0.5f)
		{
			num = (int)((float)ButtonSize * 0.5f);
		}
		mBar.Width = (int)num;
		mBar.IsHidden = (float)(base.Width - ButtonSize * 2) <= num;
		if (!mBar.IsActive)
		{
			SetScrollAmount(base.ScrollAmount, forceUpdate: true);
		}
	}

	public void NudgeLeft(Base control, EventArgs args)
	{
		if (!base.IsDisabled)
		{
			SetScrollAmount(base.ScrollAmount - NudgeAmount, forceUpdate: true);
			PlaySound(mBar.GetMouseUpSound());
		}
	}

	public void NudgeRight(Base control, EventArgs args)
	{
		if (!base.IsDisabled)
		{
			SetScrollAmount(base.ScrollAmount + NudgeAmount, forceUpdate: true);
			PlaySound(mBar.GetMouseUpSound());
		}
	}

	public override void ScrollToLeft()
	{
		SetScrollAmount(0f, forceUpdate: true);
	}

	public override void ScrollToRight()
	{
		SetScrollAmount(1f, forceUpdate: true);
	}

	protected override void OnMouseDown(MouseButton mouseButton, Point mousePosition, bool userAction = true)
	{
		base.OnMouseDown(mouseButton, mousePosition, userAction);
		if (mouseButton == MouseButton.Left)
		{
			mDepressed = true;
			InputHandler.MouseFocus = this;
		}
	}

	protected override void OnMouseUp(MouseButton mouseButton, Point mousePosition, bool userAction = true)
	{
		base.OnMouseUp(mouseButton, mousePosition, userAction);
		if (mouseButton == MouseButton.Left)
		{
			Point point = CanvasPosToLocal(mousePosition);
			if (point.X < mBar.X)
			{
				NudgeLeft(this, EventArgs.Empty);
			}
			else if (point.X > mBar.X + mBar.Width)
			{
				NudgeRight(this, EventArgs.Empty);
			}
			mDepressed = false;
			InputHandler.MouseFocus = null;
		}
	}

	protected override float CalculateScrolledAmount()
	{
		return (float)(mBar.X - ButtonSize) / (float)(base.Width - mBar.Width - ButtonSize * 2);
	}

	public override bool SetScrollAmount(float value, bool forceUpdate = false)
	{
		value = Util.Clamp(value, 0f, 1f);
		if (!base.SetScrollAmount(value, forceUpdate))
		{
			return false;
		}
		if (forceUpdate)
		{
			int x = (int)((float)ButtonSize + value * (float)(base.Width - mBar.Width - ButtonSize * 2));
			mBar.MoveTo(x, mBar.Y);
		}
		return true;
	}

	protected override void OnBarMoved(Base control, EventArgs args)
	{
		if (mBar.IsActive)
		{
			SetScrollAmount(CalculateScrolledAmount());
			base.OnBarMoved(control, args);
		}
		else
		{
			InvalidateParent();
		}
	}
}
