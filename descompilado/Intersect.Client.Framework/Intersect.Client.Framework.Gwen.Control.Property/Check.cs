using Intersect.Client.Framework.Gwen.Control.EventArguments;

namespace Intersect.Client.Framework.Gwen.Control.Property;

public class Check : Base
{
	protected readonly Checkbox Checkbox;

	public override string Value
	{
		get
		{
			if (!Checkbox.IsChecked)
			{
				return "0";
			}
			return "1";
		}
		set
		{
			base.Value = value;
		}
	}

	public override bool IsEditing => Checkbox.HasFocus;

	public override bool IsHovered
	{
		get
		{
			if (!base.IsHovered)
			{
				return Checkbox.IsHovered;
			}
			return true;
		}
	}

	public Check(Intersect.Client.Framework.Gwen.Control.Base parent)
		: base(parent)
	{
		Checkbox = new Checkbox(this);
		Checkbox.ShouldDrawBackground = false;
		Checkbox.CheckChanged += OnCheckChanged;
		Checkbox.IsTabable = true;
		Checkbox.KeyboardInputEnabled = true;
		Checkbox.SetPosition(2, 1);
		base.Height = 18;
	}

	private void OnCheckChanged(ICheckbox checkbox, ValueChangedEventArgs<bool> args)
	{
		OnValueChanged(checkbox as Intersect.Client.Framework.Gwen.Control.Base, args);
	}

	public override void SetValue(string value, bool fireEvents = false)
	{
		if (value == "1" || value.ToLower() == "true" || value.ToLower() == "yes")
		{
			Checkbox.IsChecked = true;
		}
		else
		{
			Checkbox.IsChecked = false;
		}
	}
}
