using System;

namespace Intersect.Client.Framework.Gwen.Control;

public class EnumRadioButtonGroup<T> : RadioButtonGroup where T : struct, IConvertible
{
	public T SelectedValue
	{
		get
		{
			return (T)base.Selected.UserData;
		}
		set
		{
			foreach (Base child in base.Children)
			{
				if (child is LabeledRadioButton && child.UserData.Equals(value))
				{
					(child as LabeledRadioButton).RadioButton.Press();
				}
			}
		}
	}

	public EnumRadioButtonGroup(Base parent)
		: base(parent)
	{
		if (!typeof(T).IsEnum)
		{
			throw new Exception("T must be an enumerated type!");
		}
		Text = typeof(T).Name;
		for (int i = 0; i < Enum.GetValues(typeof(T)).Length; i++)
		{
			string text = Enum.GetNames(typeof(T))[i];
			LabeledRadioButton labeledRadioButton = AddOption(text);
			labeledRadioButton.UserData = Enum.GetValues(typeof(T)).GetValue(i);
		}
	}
}
