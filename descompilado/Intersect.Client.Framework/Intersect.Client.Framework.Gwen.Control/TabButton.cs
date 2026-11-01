using System;
using Intersect.Client.Framework.File_Management;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Gwen.DragDrop;
using Intersect.Client.Framework.Gwen.Input;
using Intersect.Client.Framework.Gwen.Skin;
using Intersect.Core;
using Intersect.Framework.Reflection;
using Microsoft.Extensions.Logging;

namespace Intersect.Client.Framework.Gwen.Control;

public class TabButton : Button
{
	private TabControl? _tabControl;

	private Base _page;

	public bool IsTabActive => _page?.IsVisibleInTree ?? false;

	public TabControl? TabControl
	{
		get
		{
			return _tabControl;
		}
		set
		{
			if (value != _tabControl)
			{
				_tabControl?.OnLoseTab(this);
				_tabControl = value;
			}
		}
	}

	public Base Page
	{
		get
		{
			return _page;
		}
		set
		{
			_page = value;
		}
	}

	protected override bool ShouldClip => false;

	public TabButton(Base parent, string? name = null)
		: base(parent, name)
	{
		DragAndDrop_SetPackage(draggable: true, "TabButtonMove");
		Font = GameContentManager.Current?.GetFont("sourcesansproblack");
		FontSize = 10;
		base.KeyboardInputEnabled = true;
		base.Padding = new Padding(4, 2);
		base.TextAlign = Pos.Left | Pos.Top;
	}

	public override void DragAndDrop_StartDragging(Package package, int x, int y)
	{
		base.IsHidden = true;
	}

	public override void DragAndDrop_EndDragging(bool success, int x, int y)
	{
		base.IsHidden = false;
		base.IsActive = false;
	}

	public override bool DragAndDrop_ShouldStartDrag()
	{
		return _tabControl?.AllowReorder ?? false;
	}

	protected override void Render(Intersect.Client.Framework.Gwen.Skin.Base skin)
	{
		skin.DrawTabButton(this, IsTabActive, _tabControl.TabStrip.Dock);
	}

	protected override bool OnKeyDown(bool down)
	{
		OnKeyRight(down);
		return true;
	}

	protected override bool OnKeyUp(bool down)
	{
		OnKeyLeft(down);
		return true;
	}

	public virtual void Select()
	{
		TabControl?.OnTabPressed(this, EventArgs.Empty);
	}

	protected override bool OnKeyRight(bool down)
	{
		if (down)
		{
			int count = base.Parent.Children.Count;
			int num = base.Parent.IndexOf(this);
			if (num + 1 < count)
			{
				Base obj = base.Parent.Children[num + 1];
				TabControl.OnTabPressed(obj, EventArgs.Empty);
				InputHandler.KeyboardFocus = obj;
			}
		}
		return true;
	}

	protected override bool OnKeyLeft(bool down)
	{
		if (down)
		{
			int count = base.Parent.Children.Count;
			int num = base.Parent.IndexOf(this);
			if (num - 1 >= 0)
			{
				Base obj = base.Parent.Children[num - 1];
				TabControl.OnTabPressed(obj, EventArgs.Empty);
				InputHandler.KeyboardFocus = obj;
			}
		}
		return true;
	}

	public override void UpdateColors()
	{
		SkinColors._Label label = (IsTabActive ? base.Skin.Colors.Tab.Active : base.Skin.Colors.Tab.Inactive);
		Color color = GetTextColor(ComponentState.Normal) ?? label.Normal;
		if (IsDisabledByTree)
		{
			color = GetTextColor(ComponentState.Disabled) ?? label.Disabled;
		}
		else if (base.IsActive)
		{
			color = GetTextColor(ComponentState.Active) ?? label.Active;
		}
		else if (IsHovered)
		{
			color = GetTextColor(ComponentState.Hovered) ?? label.Hover;
		}
		if (color == null)
		{
			ApplicationContext.CurrentContext.Logger.LogError("Text color for the current control state of {ComponentType} '{ComponentName}' is somehow null IsDisabled={IsDisabled} IsActive={IsActive} IsHovered={IsHovered}", GetType().GetName(qualified: true), base.ParentQualifiedName, base.IsDisabled, base.IsActive, IsHovered);
			color = new Color(255, 0, 255);
		}
		base.TextColor = color;
	}

	protected override void OnChildBoundsChanged(Base child, Rectangle oldChildBounds, Rectangle newChildBounds)
	{
		base.OnChildBoundsChanged(child, oldChildBounds, newChildBounds);
	}

	protected override void OnTextExceedsSize(Point ownSize, Point textSize)
	{
		base.OnTextExceedsSize(ownSize, textSize);
		Invalidate(alsoInvalidateParent: true);
	}
}
