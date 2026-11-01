using Intersect.Client.Framework.Gwen;
using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.Framework.Gwen.DragDrop;

namespace Intersect.Client.Interface.Game;

public class SlotItem : ImagePanel
{
	public readonly int SlotIndex;

	public readonly Draggable Icon;

	protected readonly ContextMenu? _contextMenu;

	public SlotItem(Base parent, string name, int index, ContextMenu? contextMenu)
		: base(parent, name)
	{
		SlotIndex = index;
		base.MinimumSize = new Point(34, 34);
		base.Margin = new Margin(4);
		base.MouseInputEnabled = true;
		Icon = new Draggable(this, "Icon")
		{
			MinimumSize = new Point(32, 32),
			MouseInputEnabled = true,
			Alignment = new Alignments[1] { Alignments.Center },
			HoverSound = "menu_hover.wav"
		};
		_contextMenu = contextMenu;
	}

	public virtual void Update()
	{
	}

	public void OpenContextMenu()
	{
		ContextMenu contextMenu = _contextMenu;
		if (contextMenu != null)
		{
			OnContextMenuOpening(contextMenu);
		}
	}

	public override bool DragAndDrop_CanAcceptPackage(Package package)
	{
		return true;
	}

	protected virtual void OnContextMenuOpening(ContextMenu contextMenu)
	{
		if (contextMenu.Children.Count > 0)
		{
			contextMenu.Open(Pos.None);
		}
	}
}
