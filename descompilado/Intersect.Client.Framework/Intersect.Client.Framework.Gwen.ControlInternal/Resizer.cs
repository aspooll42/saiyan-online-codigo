using System;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Gwen.Control;

namespace Intersect.Client.Framework.Gwen.ControlInternal;

public class Resizer : Dragger
{
	private Pos mResizeDir;

	public Pos ResizeDir
	{
		set
		{
			mResizeDir = value;
			if (((value & Pos.Left) != Pos.None && (value & Pos.Top) != Pos.None) || ((value & Pos.Right) != Pos.None && (value & Pos.Bottom) != Pos.None))
			{
				base.Cursor = Cursors.SizeNwse;
			}
			else if (((value & Pos.Right) != Pos.None && (value & Pos.Top) != Pos.None) || ((value & Pos.Left) != Pos.None && (value & Pos.Bottom) != Pos.None))
			{
				base.Cursor = Cursors.SizeNesw;
			}
			else if ((value & Pos.Right) != Pos.None || (value & Pos.Left) != Pos.None)
			{
				base.Cursor = Cursors.SizeWe;
			}
			else if ((value & Pos.Top) != Pos.None || (value & Pos.Bottom) != Pos.None)
			{
				base.Cursor = Cursors.SizeNs;
			}
		}
	}

	public event GwenEventHandler<EventArgs> Resized;

	public Resizer(Base parent)
		: base(parent)
	{
		mResizeDir = Pos.Left;
		base.MouseInputEnabled = true;
		SetSize(6, 6);
		base.Target = parent;
	}

	protected override void OnMouseMoved(int x, int y, int dx, int dy)
	{
		if (_target == null || !base.IsActive)
		{
			return;
		}
		Rectangle bounds = _target.Bounds;
		Rectangle bounds2 = _target.Bounds;
		Point minimumSize = _target.MinimumSize;
		Point point = _target.CanvasPosToLocal(new Point(x, y));
		Point point2 = _target.ToCanvas(mHoldPos);
		point2.X -= x;
		point2.Y -= y;
		if ((mResizeDir & Pos.Left) != Pos.None)
		{
			bounds2.X -= point2.X;
			bounds2.Width += point2.X;
			if (bounds2.Width < minimumSize.X)
			{
				int num = minimumSize.X - bounds2.Width;
				bounds2.Width += num;
				bounds2.X -= num;
			}
		}
		if ((mResizeDir & Pos.Top) != Pos.None)
		{
			bounds2.Y -= point2.Y;
			bounds2.Height += point2.Y;
			if (bounds2.Height < minimumSize.Y)
			{
				int num2 = minimumSize.Y - bounds2.Height;
				bounds2.Height += num2;
				bounds2.Y -= num2;
			}
		}
		if ((mResizeDir & Pos.Right) != Pos.None)
		{
			int num3 = bounds2.Width - mHoldPos.X;
			int width = bounds2.Width;
			bounds2.Width = point.X + num3;
			if (bounds2.Width < minimumSize.X)
			{
				bounds2.Width = minimumSize.X;
			}
			width -= bounds2.Width;
			mHoldPos.X -= width;
		}
		if ((mResizeDir & Pos.Bottom) != Pos.None)
		{
			int num4 = bounds2.Height - mHoldPos.Y;
			int height = bounds2.Height;
			bounds2.Height = point.Y + num4;
			if (bounds2.Height < minimumSize.Y)
			{
				bounds2.Height = minimumSize.Y;
			}
			height -= bounds2.Height;
			mHoldPos.Y -= height;
		}
		_target.SetBounds(bounds2);
		if (this.Resized != null)
		{
			this.Resized(this, EventArgs.Empty);
		}
	}
}
