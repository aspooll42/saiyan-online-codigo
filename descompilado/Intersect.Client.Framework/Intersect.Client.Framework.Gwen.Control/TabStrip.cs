using System;
using System.Linq;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Gwen.ControlInternal;
using Intersect.Client.Framework.Gwen.DragDrop;
using Intersect.Client.Framework.Gwen.Skin;

namespace Intersect.Client.Framework.Gwen.Control;

public class TabStrip : Base
{
	private bool mAllowReorder;

	private Base mTabDragControl;

	public bool AllowReorder
	{
		get
		{
			return mAllowReorder;
		}
		set
		{
			mAllowReorder = value;
		}
	}

	protected override bool ShouldClip => false;

	public Pos StripPosition
	{
		get
		{
			return base.Dock;
		}
		set
		{
			base.Dock = value;
			if (base.Dock == Pos.Top)
			{
				base.Padding = new Padding(5, 0, 0, 0);
			}
			if (base.Dock == Pos.Left)
			{
				base.Padding = new Padding(0, 5, 0, 0);
			}
			if (base.Dock == Pos.Bottom)
			{
				base.Padding = new Padding(5, 0, 0, 0);
			}
			if (base.Dock == Pos.Right)
			{
				base.Padding = new Padding(0, 5, 0, 0);
			}
		}
	}

	public TabStrip(Base parent)
		: base(parent)
	{
		mAllowReorder = false;
	}

	public override bool DragAndDrop_HandleDrop(Package p, int x, int y)
	{
		Point point = CanvasPosToLocal(new Point(x, y));
		TabButton tabButton = DragAndDrop.SourceControl as TabButton;
		if (base.Parent is TabControl tabControl && tabButton != null && tabButton.TabControl != tabControl)
		{
			tabControl.AddPage(tabButton);
		}
		Base componentAt = GetComponentAt(point.X, point.Y);
		if (componentAt != null)
		{
			if (componentAt.CanvasPosToLocal(new Point(x, y)).X > componentAt.Width / 2)
			{
				DragAndDrop.SourceControl.MoveBefore(componentAt);
			}
			else
			{
				DragAndDrop.SourceControl.MoveAfter(componentAt);
			}
		}
		else
		{
			DragAndDrop.SourceControl.BringToFront();
		}
		return true;
	}

	public override bool DragAndDrop_CanAcceptPackage(Package p)
	{
		if (!mAllowReorder)
		{
			return false;
		}
		if (p.Name == "TabButtonMove")
		{
			return true;
		}
		return false;
	}

	protected override void Layout(Intersect.Client.Framework.Gwen.Skin.Base skin)
	{
		Point point = new Point(5, 5);
		Base[] array = base.Children.ToArray();
		for (int i = 0; i < array.Length; i++)
		{
			Base obj = array[i];
			if (obj is TabButton tabButton)
			{
				tabButton.SizeToContents(out var contentSize);
				Margin margin = default(Margin);
				int num = ((i > 0) ? (-1) : 0);
				switch (base.Dock)
				{
				case Pos.Top:
					margin.Left = num;
					tabButton.Dock = Pos.Left;
					break;
				case Pos.Left:
					margin.Top = num;
					tabButton.Dock = Pos.Top;
					break;
				case Pos.Right:
					margin.Top = num;
					tabButton.Dock = Pos.Top;
					break;
				case Pos.Bottom:
					margin.Left = num;
					tabButton.Dock = Pos.Left;
					break;
				}
				point.X = Math.Max(point.X, contentSize.X);
				point.Y = Math.Max(point.Y, contentSize.Y);
				tabButton.Margin = margin;
			}
		}
		Pos dock = base.Dock;
		if ((dock == Pos.Top || dock == Pos.Bottom) ? true : false)
		{
			SetSize(base.Width, point.Y);
		}
		dock = base.Dock;
		if ((dock == Pos.Left || dock == Pos.Right) ? true : false)
		{
			SetSize(point.X, base.Height);
		}
		base.Layout(skin);
	}

	public override void DragAndDrop_HoverEnter(Package p, int x, int y)
	{
		if (mTabDragControl != null)
		{
			throw new InvalidOperationException("ERROR! TabStrip::DragAndDrop_HoverEnter");
		}
		mTabDragControl = new Highlight(this);
		mTabDragControl.MouseInputEnabled = false;
		mTabDragControl.SetSize(3, base.Height);
	}

	public override void DragAndDrop_HoverLeave(Package p)
	{
		if (mTabDragControl != null)
		{
			RemoveChild(mTabDragControl, dispose: false);
			mTabDragControl.Dispose();
		}
		mTabDragControl = null;
	}

	public override void DragAndDrop_Hover(Package p, int x, int y)
	{
		Point point = CanvasPosToLocal(new Point(x, y));
		Base componentAt = GetComponentAt(point.X, point.Y);
		if (componentAt != null && componentAt != this)
		{
			Point point2 = componentAt.CanvasPosToLocal(new Point(x, y));
			mTabDragControl.SetBounds(new Rectangle(0, 0, 3, base.Height));
			mTabDragControl.BringToFront();
			mTabDragControl.SetPosition(componentAt.X - 1, 0);
			if (point2.X > componentAt.Width / 2)
			{
				mTabDragControl.MoveBy(componentAt.Width - 1, 0);
			}
			mTabDragControl.Dock = Pos.None;
		}
		else
		{
			mTabDragControl.Dock = Pos.Left;
			mTabDragControl.BringToFront();
		}
	}
}
