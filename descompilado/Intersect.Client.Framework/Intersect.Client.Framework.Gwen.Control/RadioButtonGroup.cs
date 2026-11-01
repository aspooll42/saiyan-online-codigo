using System;
using System.Linq;
using Intersect.Client.Framework.Gwen.Control.EventArguments;

namespace Intersect.Client.Framework.Gwen.Control;

public class RadioButtonGroup : GroupBox
{
	private LabeledRadioButton mSelected;

	public LabeledRadioButton Selected => mSelected;

	public string SelectedName => mSelected.Name;

	public string SelectedLabel => mSelected.Text;

	public int SelectedIndex => IndexOf(mSelected);

	public event GwenEventHandler<ItemSelectedEventArgs> SelectionChanged;

	public RadioButtonGroup(Base parent)
		: base(parent)
	{
		base.AutoSizeToContents = true;
		base.IsTabable = false;
		base.KeyboardInputEnabled = true;
		Text = string.Empty;
	}

	public virtual LabeledRadioButton AddOption(string text)
	{
		return AddOption(text, string.Empty);
	}

	public virtual LabeledRadioButton AddOption(string text, string optionName)
	{
		LabeledRadioButton labeledRadioButton = new LabeledRadioButton(this)
		{
			Name = optionName,
			Text = text,
		};
		labeledRadioButton.RadioButton.Checked += OnCheckChanged;
		labeledRadioButton.Dock = Pos.Top;
		labeledRadioButton.Margin = new Margin(0, 0, 0, 1);
		labeledRadioButton.KeyboardInputEnabled = false;
		labeledRadioButton.IsTabable = true;
		Invalidate();
		return labeledRadioButton;
	}

	private void OnCheckChanged(ICheckbox checkbox, EventArgs args)
	{
		OnRadioClicked(checkbox as Base, args);
	}

	protected virtual void OnRadioClicked(Base fromPanel, EventArgs args)
	{
		RadioButton radioButton = fromPanel as RadioButton;
		foreach (LabeledRadioButton item in base.Children.OfType<LabeledRadioButton>())
		{
			if (item.RadioButton == radioButton)
			{
				mSelected = item;
			}
			else
			{
				item.RadioButton.IsChecked = false;
			}
		}
		OnChanged(mSelected);
	}

	protected virtual void OnChanged(Base newTarget)
	{
		if (this.SelectionChanged != null)
		{
			this.SelectionChanged(this, new ItemSelectedEventArgs(newTarget));
		}
	}

	public void SetSelection(int index)
	{
		if (index >= 0 && index < base.Children.Count)
		{
			(base.Children[index] as LabeledRadioButton).RadioButton.Press();
		}
	}
}
