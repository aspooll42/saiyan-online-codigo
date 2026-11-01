using Intersect.Client.Framework.Gwen.DragDrop;
using Intersect.Client.Framework.Gwen.Skin;

namespace Intersect.Client.Framework.Gwen.Control;

public class TabTitleBar : Label
{
	public TabTitleBar(Base parent)
		: base(parent)
	{
		base.AutoSizeToContents = false;
		base.MouseInputEnabled = true;
		base.Padding = new Padding(5, 2, 5, 2);
		DragAndDrop_SetPackage(draggable: true, "TabWindowMove");
	}

	protected override void Render(Intersect.Client.Framework.Gwen.Skin.Base skin)
	{
		skin.DrawTabTitleBar(this);
	}

	public override void DragAndDrop_StartDragging(Package package, int x, int y)
	{
		DragAndDrop.SourceControl = base.Parent;
		DragAndDrop.SourceControl.DragAndDrop_StartDragging(package, x, y);
	}

	public void UpdateFromTab(TabButton button)
	{
		Text = button.Text;
		SizeToContents();
	}
}
