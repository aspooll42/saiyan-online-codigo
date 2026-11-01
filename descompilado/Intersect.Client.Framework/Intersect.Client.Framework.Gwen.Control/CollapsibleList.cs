using System;
using Intersect.Client.Framework.Gwen.Control.EventArguments;
using Intersect.Client.Framework.Gwen.Skin;

namespace Intersect.Client.Framework.Gwen.Control;

public class CollapsibleList : ScrollControl
{
	public event GwenEventHandler<ItemSelectedEventArgs> ItemSelected;

	public event GwenEventHandler<EventArgs> CategoryCollapsed;

	public CollapsibleList(Base parent)
		: base(parent)
	{
		base.MouseInputEnabled = true;
	}

	public Button GetSelectedButton()
	{
		foreach (Base child in base.Children)
		{
			if (child is CollapsibleCategory collapsibleCategory)
			{
				Button selectedButton = collapsibleCategory.GetSelectedButton();
				if (selectedButton != null)
				{
					return selectedButton;
				}
			}
		}
		return null;
	}

	protected virtual void Add(CollapsibleCategory category)
	{
		category.Parent = this;
		category.Dock = Pos.Top;
		category.Margin = new Margin(1, 0, 1, 1);
		category.Selected += OnCategorySelected;
		category.Collapsed += OnCategoryCollapsed;
	}

	public virtual CollapsibleCategory Add(string categoryName)
	{
		CollapsibleCategory collapsibleCategory = new CollapsibleCategory(this)
		{
			Text = categoryName,
		};
		Add(collapsibleCategory);
		return collapsibleCategory;
	}

	protected override void Render(Intersect.Client.Framework.Gwen.Skin.Base skin)
	{
		skin.DrawCategoryHolder(this);
		base.Render(skin);
	}

	public virtual void UnselectAll()
	{
		foreach (Base child in base.Children)
		{
			if (child is CollapsibleCategory collapsibleCategory)
			{
				collapsibleCategory.UnselectAll();
			}
		}
	}

	protected virtual void OnCategorySelected(Base control, EventArgs args)
	{
		if (control is CollapsibleCategory selectedItem && this.ItemSelected != null)
		{
			this.ItemSelected(this, new ItemSelectedEventArgs(selectedItem));
		}
	}

	protected virtual void OnCategoryCollapsed(Base control, EventArgs args)
	{
		if (control is CollapsibleCategory && this.CategoryCollapsed != null)
		{
			this.CategoryCollapsed(control, EventArgs.Empty);
		}
	}
}
