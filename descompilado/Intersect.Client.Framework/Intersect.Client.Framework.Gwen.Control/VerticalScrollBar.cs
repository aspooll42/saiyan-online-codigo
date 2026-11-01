using System;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Gwen.Input;
using Intersect.Client.Framework.Gwen.Skin;
using Intersect.Client.Framework.Input;

namespace Intersect.Client.Framework.Gwen.Control;

public class VerticalScrollBar : ScrollBar
{
	private bool _wasHeld;

	public override int BarSize
	{
		get
		{
			return mBar.Height;
		}
		set
		{
			mBar.Height = value;
		}
	}

	public override int BarPos => mBar.Y - base.Width;

	public override int ButtonSize => base.Width;

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

	public VerticalScrollBar(Base parent)
		: base(parent)
	{
		mBar.IsVertical = true;
		mScrollButton[0].SetDirectionUp();
		mScrollButton[0].Clicked += NudgeUp;
		mScrollButton[1].SetDirectionDown();
		mScrollButton[1].Clicked += NudgeDown;
		mBar.Dragged += OnBarMoved;
	}

	protected override void OnBoundsChanged(Rectangle oldBounds, Rectangle newBounds)
	{
		base.OnBoundsChanged(oldBounds, newBounds);
	}

	protected override void Layout(Intersect.Client.Framework.Gwen.Skin.Base skin)
	{
		mScrollButton[0].Height = base.Width;
		mScrollButton[0].Dock = Pos.Top;
		mScrollButton[1].Height = base.Width;
		mScrollButton[1].Dock = Pos.Bottom;
		mBar.Width = ButtonSize;
		mBar.Margin = new Margin(0, ButtonSize, 0, ButtonSize);
		float num = 0f;
		if (mContentSize > 0f)
		{
			num = mViewableContentSize / mContentSize * (float)(base.Height - ButtonSize * 2);
		}
		if (num < (float)ButtonSize * 0.5f)
		{
			num = (int)((float)ButtonSize * 0.5f);
		}
		mBar.Height = (int)num;
		mBar.IsHidden = (float)(base.Height - ButtonSize * 2) <= num;
		bool isActive = mBar.IsActive;
		if (!isActive)
		{
			SetScrollAmount(base.ScrollAmount, _wasHeld != isActive);
		}
		_wasHeld = isActive;
		base.Layout(skin);
	}

	public virtual void NudgeUp(Base control, EventArgs args)
	{
		if (!base.IsDisabled)
		{
			SetScrollAmount(base.ScrollAmount - NudgeAmount, forceUpdate: true);
			PlaySound(mBar.GetMouseUpSound());
		}
	}

	public virtual void NudgeDown(Base control, EventArgs args)
	{
		if (!base.IsDisabled)
		{
			SetScrollAmount(base.ScrollAmount + NudgeAmount, forceUpdate: true);
			PlaySound(mBar.GetMouseUpSound());
		}
	}

	public override void ScrollToTop()
	{
		SetScrollAmount(0f, forceUpdate: true);
	}

	public override void ScrollToBottom()
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
			if (point.Y < mBar.Y)
			{
				NudgeUp(this, EventArgs.Empty);
			}
			else if (point.Y > mBar.Y + mBar.Height)
			{
				NudgeDown(this, EventArgs.Empty);
			}
			mDepressed = false;
			InputHandler.MouseFocus = null;
		}
	}

	protected override float CalculateScrolledAmount()
	{
		return (float)(mBar.Y - ButtonSize) / (float)(base.Height - mBar.Height - ButtonSize * 2);
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
			int y = (int)((float)ButtonSize + value * (float)(base.Height - mBar.Height - ButtonSize * 2));
			mBar.MoveTo(mBar.X, y);
		}
		return true;
	}

	protected override void OnBarMoved(Base control, EventArgs args)
	{
		if (mBar.IsActive)
		{
			SetScrollAmount(CalculateScrolledAmount());
			base.OnBarMoved(control, EventArgs.Empty);
		}
		else
		{
			InvalidateParent();
		}
	}
}
