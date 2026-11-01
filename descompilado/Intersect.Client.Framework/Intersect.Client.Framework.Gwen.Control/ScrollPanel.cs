using System;
using Intersect.Client.Framework.GenericClasses;

namespace Intersect.Client.Framework.Gwen.Control;

public class ScrollPanel : Base
{
	private readonly ScrollControl _scroller;

	internal ScrollPanel(ScrollControl parent, string? name = "ScrollPanel")
		: base(parent, name)
	{
		_scroller = parent;
		base.MouseInputEnabled = false;
		base.Padding = Padding.Four;
	}

	protected override void ApplyDockFill(Base child, Point position, Point size)
	{
		Point size2 = child.Size;
		OverflowBehavior overflowX = _scroller.OverflowX;
		if ((overflowX == OverflowBehavior.Auto || overflowX == OverflowBehavior.Scroll) ? true : false)
		{
			size.X = Math.Max(size.X, size2.X);
		}
		overflowX = _scroller.OverflowY;
		if ((overflowX == OverflowBehavior.Auto || overflowX == OverflowBehavior.Scroll) ? true : false)
		{
			size.Y = Math.Max(size.Y, size2.Y);
		}
		base.ApplyDockFill(child, position, size);
	}

	public override Point GetChildrenSize()
	{
		return base.GetChildrenSize();
	}

	protected override void OnPositionChanged(Point oldPosition, Point newPosition)
	{
		base.OnPositionChanged(oldPosition, newPosition);
	}

	protected override void OnBoundsChanged(Rectangle oldBounds, Rectangle newBounds)
	{
		base.OnBoundsChanged(oldBounds, newBounds);
	}
}
