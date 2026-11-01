using System;
using Intersect.Client.Framework.Gwen.Control.Property;
using Intersect.Client.Framework.Gwen.ControlInternal;
using Intersect.Client.Framework.Gwen.Skin;

namespace Intersect.Client.Framework.Gwen.Control;

public class PropertyRow : Base
{
	private readonly Label mLabel;

	private readonly Intersect.Client.Framework.Gwen.Control.Property.Base mProperty;

	private bool mLastEditing;

	private bool mLastHover;

	public bool IsEditing
	{
		get
		{
			if (mProperty != null)
			{
				return mProperty.IsEditing;
			}
			return false;
		}
	}

	public string Value
	{
		get
		{
			return mProperty.Value;
		}
		set
		{
			mProperty.Value = value;
		}
	}

	public override bool IsHovered
	{
		get
		{
			if (!base.IsHovered)
			{
				if (mProperty != null)
				{
					return mProperty.IsHovered;
				}
				return false;
			}
			return true;
		}
	}

	public string Label
	{
		get
		{
			return mLabel.Text;
		}
		set
		{
			mLabel.Text = value;
		}
	}

	public event GwenEventHandler<EventArgs> ValueChanged;

	public PropertyRow(Base parent, Intersect.Client.Framework.Gwen.Control.Property.Base prop)
		: base(parent)
	{
		mLabel = new PropertyRowLabel(this)
		{
			Dock = Pos.Left,
			TextAlign = (Pos.Left | Pos.Top),
			Margin = new Margin(2, 2, 0, 0)
		};
		mProperty = prop;
		mProperty.Parent = this;
		mProperty.Dock = Pos.Fill;
		mProperty.ValueChanged += OnValueChanged;
	}

	protected override void Render(Intersect.Client.Framework.Gwen.Skin.Base skin)
	{
		if (IsEditing != mLastEditing)
		{
			OnEditingChanged();
			mLastEditing = IsEditing;
		}
		if (IsHovered != mLastHover)
		{
			OnHoverChanged();
			mLastHover = IsHovered;
		}
		skin.DrawPropertyRow(this, mLabel.Right, IsEditing, IsHovered | mProperty.IsHovered);
	}

	protected override void Layout(Intersect.Client.Framework.Gwen.Skin.Base skin)
	{
		if (base.Parent is Properties properties)
		{
			mLabel.Width = properties.SplitWidth;
			if (mProperty != null)
			{
				base.Height = mProperty.Height;
			}
		}
	}

	protected virtual void OnValueChanged(Base control, EventArgs args)
	{
		if (this.ValueChanged != null)
		{
			this.ValueChanged(this, EventArgs.Empty);
		}
	}

	private void OnEditingChanged()
	{
		mLabel.Redraw();
	}

	private void OnHoverChanged()
	{
		mLabel.Redraw();
	}
}
