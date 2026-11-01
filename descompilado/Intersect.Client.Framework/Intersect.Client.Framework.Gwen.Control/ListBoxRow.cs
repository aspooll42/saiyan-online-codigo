using Intersect.Client.Framework.Gwen.Control.Layout;
using Intersect.Client.Framework.Gwen.Skin;
using Intersect.Client.Framework.Input;

namespace Intersect.Client.Framework.Gwen.Control;

public class ListBoxRow : TableRow
{
	private bool mSelected;

	public bool IsSelected
	{
		get
		{
			return mSelected;
		}
		set
		{
			mSelected = value;
		}
	}

	public ListBoxRow(Base parent, int columns, int[]? columnWidths = null)
		: base(parent, columns, columnWidths)
	{
		base.MouseInputEnabled = true;
		IsSelected = false;
	}

	protected override void Render(Intersect.Client.Framework.Gwen.Skin.Base skin)
	{
		skin.DrawListBoxLine(this, IsSelected, base.EvenRow);
	}

	protected override void OnMouseDown(MouseButton mouseButton, Point mousePosition, bool userAction = true)
	{
		base.OnMouseDown(mouseButton, mousePosition, userAction);
		OnRowSelected();
	}
}
