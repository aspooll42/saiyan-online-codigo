using System;
using Intersect.Client.Framework.Gwen.Control.EventArguments;
using Intersect.Client.Framework.Gwen.ControlInternal;
using Intersect.Client.Framework.Gwen.Skin;

namespace Intersect.Client.Framework.Gwen.Control;

public class CollapsibleCategory : Base
{
	private readonly Button mHeaderButton;

	private readonly CollapsibleList mList;

	public string Text
	{
		get
		{
			return mHeaderButton.Text;
		}
		set
		{
			mHeaderButton.Text = value;
		}
	}

	public bool IsCollapsed
	{
		get
		{
			return mHeaderButton.ToggleState;
		}
		set
		{
			mHeaderButton.ToggleState = value;
		}
	}

	public event GwenEventHandler<ItemSelectedEventArgs> Selected;

	public event GwenEventHandler<EventArgs> Collapsed;

	public CollapsibleCategory(CollapsibleList parent)
		: base(parent)
	{
		mHeaderButton = new CategoryHeaderButton(this);
		mHeaderButton.Text = "Category Title";
		mHeaderButton.Dock = Pos.Top;
		mHeaderButton.Height = 20;
		mHeaderButton.Toggled += OnHeaderToggle;
		mList = parent;
		base.Padding = new Padding(1, 0, 1, 5);
		SetSize(512, 512);
	}

	public Button GetSelectedButton()
	{
		foreach (Base child in base.Children)
		{
			if (child is CategoryButton { ToggleState: not false } categoryButton)
			{
				return categoryButton;
			}
		}
		return null;
	}

	protected virtual void OnHeaderToggle(Base control, EventArgs args)
	{
		if (this.Collapsed != null)
		{
			this.Collapsed(this, EventArgs.Empty);
		}
	}

	protected virtual void OnSelected(Base control, EventArgs args)
	{
		if (control is CategoryButton categoryButton)
		{
			if (mList != null)
			{
				mList.UnselectAll();
			}
			else
			{
				UnselectAll();
			}
			categoryButton.ToggleState = true;
			if (this.Selected != null)
			{
				this.Selected(this, new ItemSelectedEventArgs(control));
			}
		}
	}

	public Button Add(string name)
	{
		CategoryButton categoryButton = new CategoryButton(this)
		{
			Text = name,
			Dock = Pos.Top,
		};
		categoryButton.SizeToContents();
		categoryButton.SetSize(categoryButton.Width + 4, categoryButton.Height + 4);
		categoryButton.Padding = new Padding(5, 2, 2, 2);
		categoryButton.Clicked += OnSelected;
		return categoryButton;
	}

	protected override void Render(Intersect.Client.Framework.Gwen.Skin.Base skin)
	{
		skin.DrawCategoryInner(this, mHeaderButton.ToggleState);
		base.Render(skin);
	}

	public void UnselectAll()
	{
		foreach (Base child in base.Children)
		{
			if (child is CategoryButton categoryButton)
			{
				categoryButton.ToggleState = false;
			}
		}
	}

	protected override void DoPostlayout(Intersect.Client.Framework.Gwen.Skin.Base skin)
	{
		if (IsCollapsed)
		{
			base.Height = mHeaderButton.Height;
		}
		else
		{
			SizeToChildren(resizeX: false);
		}
		bool flag = true;
		foreach (Base child in base.Children)
		{
			if (child is CategoryButton categoryButton)
			{
				categoryButton.mAlt = flag;
				categoryButton.UpdateColors();
				flag = !flag;
			}
		}
	}
}
