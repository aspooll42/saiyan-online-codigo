using System.Linq;
using Intersect.Client.Framework.Gwen.DragDrop;
using Intersect.Client.Framework.Gwen.Skin;

namespace Intersect.Client.Framework.Gwen.Control;

public class DockedTabControl : TabControl
{
	private readonly TabTitleBar mTitleBar;

	public bool TitleBarVisible
	{
		get
		{
			return !mTitleBar.IsHidden;
		}
		set
		{
			mTitleBar.IsHidden = !value;
		}
	}

	public DockedTabControl(Base parent)
		: base(parent)
	{
		base.Dock = Pos.Fill;
		mTitleBar = new TabTitleBar(this);
		mTitleBar.Dock = Pos.Top;
		mTitleBar.IsHidden = true;
	}

	protected override void Layout(Intersect.Client.Framework.Gwen.Skin.Base skin)
	{
		base.TabStrip.IsHidden = base.TabCount <= 1;
		UpdateTitleBar();
		base.Layout(skin);
	}

	private void UpdateTitleBar()
	{
		if (base.SelectedTab != null)
		{
			mTitleBar.UpdateFromTab(base.SelectedTab);
		}
	}

	public override void DragAndDrop_StartDragging(Package package, int x, int y)
	{
		base.DragAndDrop_StartDragging(package, x, y);
		base.IsHidden = true;
		base.Parent.IsHidden = true;
	}

	public override void DragAndDrop_EndDragging(bool success, int x, int y)
	{
		base.IsHidden = false;
		if (!success)
		{
			base.Parent.IsHidden = false;
		}
	}

	public void MoveTabsTo(DockedTabControl target)
	{
		Base[] array = base.TabStrip.Children.ToArray();
		Base[] array2 = array;
		foreach (Base obj in array2)
		{
			if (obj is TabButton button)
			{
				target.AddPage(button);
			}
		}
		Invalidate();
	}
}
