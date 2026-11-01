using System;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Gwen.Skin;

namespace Intersect.Client.Framework.Gwen.Control;

public class TreeControl : TreeNode
{
	private readonly ScrollControl _scrollControl;

	public bool AllowMultiSelect { get; set; }

	public TreeControl(Base parent, string? name = null)
		: base(parent, name)
	{
		_treeControl = this;
		RemoveChild(base._toggleButton, dispose: true);
		RemoveChild(base._trigger, dispose: true);
		RemoveChild(_innerPanel, dispose: true);
		base._toggleButton = null;
		base._trigger = null;
		_innerPanel = null;
		AllowMultiSelect = false;
		_scrollControl = new ScrollControl(this, "_innerPanel")
		{
			Dock = Pos.Fill,
			Margin = Margin.One
		};
		_innerPanel = _scrollControl;
		_scrollControl.SetInnerSize(1000, 1000);
		base.Dock = Pos.None;
	}

	protected override void Render(Intersect.Client.Framework.Gwen.Skin.Base skin)
	{
		if (base.ShouldDrawBackground)
		{
			skin.DrawTreeControl(this);
		}
	}

	protected override void OnChildBoundsChanged(Base child, Rectangle oldChildBounds, Rectangle newChildBounds)
	{
		if (_scrollControl != null)
		{
			_scrollControl.UpdateScrollBars();
		}
	}

	public virtual void RemoveAll()
	{
		_scrollControl.DeleteAll();
	}

	public virtual void OnNodeAdded(TreeNode node)
	{
		node.LabelPressed += OnNodeSelected;
	}

	protected virtual void OnNodeSelected(Base control, EventArgs args)
	{
		if (!AllowMultiSelect)
		{
			UnselectAll();
		}
	}
}
