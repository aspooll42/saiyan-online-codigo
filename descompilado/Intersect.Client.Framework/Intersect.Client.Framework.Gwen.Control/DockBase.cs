using System;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Gwen.ControlInternal;
using Intersect.Client.Framework.Gwen.DragDrop;
using Intersect.Client.Framework.Gwen.Renderer;
using Intersect.Client.Framework.Gwen.Skin;

namespace Intersect.Client.Framework.Gwen.Control;

public class DockBase : Base
{
	private DockBase mBottom;

	private DockedTabControl mDockedTabControl;

	private bool mDrawHover;

	private bool mDropFar;

	private Rectangle mHoverRect;

	private DockBase mLeft;

	private DockBase mRight;

	private Resizer mSizer;

	private DockBase mTop;

	public DockBase LeftDock => GetChildDock(Pos.Left);

	public DockBase RightDock => GetChildDock(Pos.Right);

	public DockBase TopDock => GetChildDock(Pos.Top);

	public DockBase BottomDock => GetChildDock(Pos.Bottom);

	public TabControl TabControl => mDockedTabControl;

	public virtual bool IsEmpty
	{
		get
		{
			if (mDockedTabControl != null && mDockedTabControl.TabCount > 0)
			{
				return false;
			}
			if (mLeft != null && !mLeft.IsEmpty)
			{
				return false;
			}
			if (mRight != null && !mRight.IsEmpty)
			{
				return false;
			}
			if (mTop != null && !mTop.IsEmpty)
			{
				return false;
			}
			if (mBottom != null && !mBottom.IsEmpty)
			{
				return false;
			}
			return true;
		}
	}

	public DockBase(Base parent)
		: base(parent)
	{
		base.Padding = Padding.One;
		SetSize(200, 200);
	}

	protected override bool OnKeySpace(bool down)
	{
		return false;
	}

	protected virtual void SetupChildDock(Pos pos)
	{
		if (mDockedTabControl == null)
		{
			mDockedTabControl = new DockedTabControl(this);
			mDockedTabControl.TabRemoved += OnTabRemoved;
			mDockedTabControl.TabStripPosition = Pos.Bottom;
			mDockedTabControl.TitleBarVisible = true;
		}
		base.Dock = pos;
		Pos pos2 = pos switch
		{
			Pos.Right => Pos.Left, 
			Pos.Left => Pos.Right, 
			Pos.Top => Pos.Bottom, 
			Pos.Bottom => Pos.Top, 
			_ => throw new ArgumentException("Invalid dock", "pos"), 
		};
		if (mSizer != null)
		{
			mSizer.Dispose();
		}
		mSizer = new Resizer(this);
		mSizer.Dock = pos2;
		mSizer.ResizeDir = pos2;
		mSizer.SetSize(2, 2);
	}

	protected override void Render(Intersect.Client.Framework.Gwen.Skin.Base skin)
	{
	}

	protected virtual DockBase GetChildDock(Pos pos)
	{
		DockBase dockBase = null;
		switch (pos)
		{
		case Pos.Left:
			if (mLeft == null)
			{
				mLeft = new DockBase(this);
				mLeft.SetupChildDock(pos);
			}
			dockBase = mLeft;
			break;
		case Pos.Right:
			if (mRight == null)
			{
				mRight = new DockBase(this);
				mRight.SetupChildDock(pos);
			}
			dockBase = mRight;
			break;
		case Pos.Top:
			if (mTop == null)
			{
				mTop = new DockBase(this);
				mTop.SetupChildDock(pos);
			}
			dockBase = mTop;
			break;
		case Pos.Bottom:
			if (mBottom == null)
			{
				mBottom = new DockBase(this);
				mBottom.SetupChildDock(pos);
			}
			dockBase = mBottom;
			break;
		}
		if (dockBase != null)
		{
			dockBase.IsHidden = false;
		}
		return dockBase;
	}

	protected virtual Pos GetDroppedTabDirection(int x, int y)
	{
		int width = base.Width;
		int height = base.Height;
		float num = (float)y / (float)height;
		float num2 = (float)x / (float)width;
		float num3 = (float)(width - x) / (float)width;
		float num4 = (float)(height - y) / (float)height;
		float num5 = Math.Min(Math.Min(Math.Min(num, num2), num3), num4);
		mDropFar = num5 < 0.2f;
		if (num5 > 0.3f)
		{
			return Pos.Fill;
		}
		if (num == num5 && (mTop == null || mTop.IsHidden))
		{
			return Pos.Top;
		}
		if (num2 == num5 && (mLeft == null || mLeft.IsHidden))
		{
			return Pos.Left;
		}
		if (num3 == num5 && (mRight == null || mRight.IsHidden))
		{
			return Pos.Right;
		}
		if (num4 == num5 && (mBottom == null || mBottom.IsHidden))
		{
			return Pos.Bottom;
		}
		return Pos.Fill;
	}

	public override bool DragAndDrop_CanAcceptPackage(Package p)
	{
		if (p.Name == "TabButtonMove")
		{
			return true;
		}
		if (p.Name == "TabWindowMove")
		{
			return true;
		}
		return false;
	}

	public override bool DragAndDrop_HandleDrop(Package p, int x, int y)
	{
		Point point = CanvasPosToLocal(new Point(x, y));
		Pos droppedTabDirection = GetDroppedTabDirection(point.X, point.Y);
		DockedTabControl dockedTabControl = mDockedTabControl;
		if (droppedTabDirection == Pos.Fill && dockedTabControl == null)
		{
			return false;
		}
		if (droppedTabDirection != Pos.Fill)
		{
			DockBase childDock = GetChildDock(droppedTabDirection);
			dockedTabControl = childDock.mDockedTabControl;
			if (!mDropFar)
			{
				childDock.BringToFront();
			}
			else
			{
				childDock.SendToBack();
			}
		}
		if (p.Name == "TabButtonMove")
		{
			if (!(DragAndDrop.SourceControl is TabButton button))
			{
				return false;
			}
			dockedTabControl.AddPage(button);
		}
		if (p.Name == "TabWindowMove")
		{
			if (!(DragAndDrop.SourceControl is DockedTabControl dockedTabControl2))
			{
				return false;
			}
			if (dockedTabControl2 == dockedTabControl)
			{
				return false;
			}
			dockedTabControl2.MoveTabsTo(dockedTabControl);
		}
		Invalidate();
		return true;
	}

	protected virtual void OnTabRemoved(Base control, EventArgs args)
	{
		DoRedundancyCheck();
		DoConsolidateCheck();
	}

	protected virtual void DoRedundancyCheck()
	{
		if (IsEmpty && base.Parent is DockBase dockBase)
		{
			dockBase.OnRedundantChildDock(this);
		}
	}

	protected virtual void DoConsolidateCheck()
	{
		if (!IsEmpty && mDockedTabControl != null && mDockedTabControl.TabCount <= 0)
		{
			if (mBottom != null && !mBottom.IsEmpty)
			{
				mBottom.mDockedTabControl.MoveTabsTo(mDockedTabControl);
			}
			else if (mTop != null && !mTop.IsEmpty)
			{
				mTop.mDockedTabControl.MoveTabsTo(mDockedTabControl);
			}
			else if (mLeft != null && !mLeft.IsEmpty)
			{
				mLeft.mDockedTabControl.MoveTabsTo(mDockedTabControl);
			}
			else if (mRight != null && !mRight.IsEmpty)
			{
				mRight.mDockedTabControl.MoveTabsTo(mDockedTabControl);
			}
		}
	}

	protected virtual void OnRedundantChildDock(DockBase dock)
	{
		dock.IsHidden = true;
		DoRedundancyCheck();
		DoConsolidateCheck();
	}

	public override void DragAndDrop_HoverEnter(Package p, int x, int y)
	{
		mDrawHover = true;
	}

	public override void DragAndDrop_HoverLeave(Package p)
	{
		mDrawHover = false;
	}

	public override void DragAndDrop_Hover(Package p, int x, int y)
	{
		Point point = CanvasPosToLocal(new Point(x, y));
		Pos droppedTabDirection = GetDroppedTabDirection(point.X, point.Y);
		if (droppedTabDirection == Pos.Fill)
		{
			if (mDockedTabControl == null)
			{
				mHoverRect = Rectangle.Empty;
			}
			else
			{
				mHoverRect = base.InnerBounds;
			}
			return;
		}
		mHoverRect = base.RenderBounds;
		int num = 0;
		if (droppedTabDirection == Pos.Left)
		{
			num = (int)((float)mHoverRect.Width * 0.25f);
			mHoverRect.Width = num;
		}
		if (droppedTabDirection == Pos.Right)
		{
			num = (int)((float)mHoverRect.Width * 0.25f);
			mHoverRect.X = mHoverRect.Width - num;
			mHoverRect.Width = num;
		}
		if (droppedTabDirection == Pos.Top)
		{
			num = (int)((float)mHoverRect.Height * 0.25f);
			mHoverRect.Height = num;
		}
		if (droppedTabDirection == Pos.Bottom)
		{
			num = (int)((float)mHoverRect.Height * 0.25f);
			mHoverRect.Y = mHoverRect.Height - num;
			mHoverRect.Height = num;
		}
		if ((droppedTabDirection == Pos.Top || droppedTabDirection == Pos.Bottom) && !mDropFar)
		{
			if (mLeft != null && mLeft.IsVisibleInTree)
			{
				mHoverRect.X += mLeft.Width;
				mHoverRect.Width -= mLeft.Width;
			}
			if (mRight != null && mRight.IsVisibleInTree)
			{
				mHoverRect.Width -= mRight.Width;
			}
		}
		if ((droppedTabDirection == Pos.Left || droppedTabDirection == Pos.Right) && !mDropFar)
		{
			if (mTop != null && mTop.IsVisibleInTree)
			{
				mHoverRect.Y += mTop.Height;
				mHoverRect.Height -= mTop.Height;
			}
			if (mBottom != null && mBottom.IsVisibleInTree)
			{
				mHoverRect.Height -= mBottom.Height;
			}
		}
	}

	protected override void RenderOver(Intersect.Client.Framework.Gwen.Skin.Base skin)
	{
		if (mDrawHover)
		{
			Intersect.Client.Framework.Gwen.Renderer.Base renderer = skin.Renderer;
			renderer.DrawColor = Color.FromArgb(20, 255, 200, 255);
			renderer.DrawFilledRect(base.RenderBounds);
			if (mHoverRect.Width != 0)
			{
				renderer.DrawColor = Color.FromArgb(100, 255, 200, 255);
				renderer.DrawFilledRect(mHoverRect);
				renderer.DrawColor = Color.FromArgb(200, 255, 200, 255);
				renderer.DrawLinedRect(mHoverRect);
			}
		}
	}
}
