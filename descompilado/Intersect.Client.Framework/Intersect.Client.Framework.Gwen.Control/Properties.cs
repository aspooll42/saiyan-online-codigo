using System;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Gwen.Control.Property;
using Intersect.Client.Framework.Gwen.ControlInternal;
using Intersect.Client.Framework.Gwen.Skin;

namespace Intersect.Client.Framework.Gwen.Control;

public class Properties : Base
{
	private readonly SplitterBar mSplitterBar;

	public int SplitWidth => mSplitterBar.X;

	public event GwenEventHandler<EventArgs> ValueChanged;

	public Properties(Base parent)
		: base(parent)
	{
		mSplitterBar = new SplitterBar(this);
		mSplitterBar.SetPosition(80, 0);
		mSplitterBar.Cursor = Cursors.SizeWe;
		mSplitterBar.Dragged += OnSplitterMoved;
		mSplitterBar.ShouldDrawBackground = false;
	}

	protected override void DoPostlayout(Intersect.Client.Framework.Gwen.Skin.Base skin)
	{
		mSplitterBar.Height = 0;
		if (SizeToChildren(resizeX: false))
		{
			InvalidateParent();
		}
		mSplitterBar.SetSize(3, base.Height);
	}

	protected virtual void OnSplitterMoved(Base control, EventArgs args)
	{
		InvalidateChildren();
	}

	public PropertyRow Add(string label, string value = "")
	{
		return Add(label, new Intersect.Client.Framework.Gwen.Control.Property.Text(this), value);
	}

	public PropertyRow Add(string label, Intersect.Client.Framework.Gwen.Control.Property.Base prop, string value = "")
	{
		PropertyRow propertyRow = new PropertyRow(this, prop)
		{
			Dock = Pos.Top,
			Label = label,
		};
		propertyRow.ValueChanged += OnRowValueChanged;
		prop.SetValue(value, fireEvents: true);
		mSplitterBar.BringToFront();
		return propertyRow;
	}

	private void OnRowValueChanged(Base control, EventArgs args)
	{
		if (this.ValueChanged != null)
		{
			this.ValueChanged(control, EventArgs.Empty);
		}
	}

	public void DeleteAll()
	{
		_innerPanel.DeleteAllChildren();
	}
}
