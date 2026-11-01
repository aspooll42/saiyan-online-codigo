namespace Intersect.Client.Framework.Gwen.Control.Property;

public class Text : Base
{
	protected readonly TextBox mTextBox;

	public override string Value
	{
		get
		{
			return mTextBox.Text;
		}
		set
		{
			base.Value = value;
		}
	}

	public override bool IsEditing => mTextBox.HasFocus;

	public override bool IsHovered => base.IsHovered | mTextBox.IsHovered;

	public Text(Intersect.Client.Framework.Gwen.Control.Base parent)
		: base(parent)
	{
		mTextBox = new TextBox(this);
		mTextBox.Dock = Pos.Fill;
		mTextBox.ShouldDrawBackground = false;
		mTextBox.TextChanged += OnValueChanged;
	}

	public override void SetValue(string value, bool fireEvents = false)
	{
		mTextBox.SetText(value, fireEvents);
	}
}
