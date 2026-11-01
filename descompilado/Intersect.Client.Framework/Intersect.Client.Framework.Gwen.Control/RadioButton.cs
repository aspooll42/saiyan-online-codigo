using Intersect.Client.Framework.Gwen.Skin;

namespace Intersect.Client.Framework.Gwen.Control;

public class RadioButton : Checkbox
{
	protected override bool AllowUncheck => false;

	public RadioButton(Base parent, string name = null, bool disableText = true)
		: base(parent, name, disableText)
	{
		SetSize(15, 15);
		base.MouseInputEnabled = true;
		base.IsTabable = false;
		base.IsToggle = true;
	}

	protected override void Render(Intersect.Client.Framework.Gwen.Skin.Base skin)
	{
		skin.DrawRadioButton(this, base.IsChecked, IsHovered, base.IsActive);
	}
}
