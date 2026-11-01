using System;

namespace Intersect.Client.Framework.Gwen.Control.Property;

public class Base : Intersect.Client.Framework.Gwen.Control.Base
{
	public virtual string Value
	{
		get
		{
			return null;
		}
		set
		{
			SetValue(value);
		}
	}

	public virtual bool IsEditing => false;

	public event GwenEventHandler<EventArgs> ValueChanged;

	public Base(Intersect.Client.Framework.Gwen.Control.Base parent)
		: base(parent)
	{
		base.Height = 17;
	}

	protected virtual void DoChanged()
	{
		if (this.ValueChanged != null)
		{
			this.ValueChanged(this, EventArgs.Empty);
		}
	}

	protected virtual void OnValueChanged(Intersect.Client.Framework.Gwen.Control.Base control, EventArgs args)
	{
		DoChanged();
	}

	public virtual void SetValue(string value, bool fireEvents = false)
	{
	}
}
