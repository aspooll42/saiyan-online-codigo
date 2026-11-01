using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Gwen.Skin;

namespace Intersect.Client.Framework.Gwen.Control.Layout;

public class TableCell : Label
{
	public TableCell(TableRow row, string? name = "TableCell")
		: base(row, name)
	{
		_textElement.IsVisibleInParent = false;
	}

	protected override void OnTextChanged()
	{
		base.OnTextChanged();
		_textElement.IsVisibleInParent = !string.IsNullOrEmpty(Text);
	}

	protected override void OnDockChanged(Pos oldDock, Pos newDock)
	{
		base.OnDockChanged(oldDock, newDock);
	}

	protected override void Layout(Intersect.Client.Framework.Gwen.Skin.Base skin)
	{
		base.Layout(skin);
	}

	public override bool SizeToContents(out Point contentSize)
	{
		return base.SizeToContents(out contentSize);
	}

	public override Point GetChildrenSize()
	{
		return base.GetChildrenSize();
	}

	protected override Point GetContentSize()
	{
		return base.GetContentSize();
	}

	protected override void OnSizeChanged(Point oldSize, Point newSize)
	{
		base.OnSizeChanged(oldSize, newSize);
	}

	protected override void OnBoundsChanged(Rectangle oldBounds, Rectangle newBounds)
	{
		base.OnBoundsChanged(oldBounds, newBounds);
	}

	protected override void OnChildBoundsChanged(Base child, Rectangle oldChildBounds, Rectangle newChildBounds)
	{
		base.OnChildBoundsChanged(child, oldChildBounds, newChildBounds);
	}
}
