using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.Framework.Gwen.Skin;

namespace Intersect.Client.Framework.Gwen.ControlInternal;

public class DownArrow : Intersect.Client.Framework.Gwen.Control.Base
{
	private readonly ComboBox mComboBox;

	public DownArrow(ComboBox parent, string? name = null)
		: base(parent, name)
	{
		base.MouseInputEnabled = false;
		base.MinimumSize = new Point(15, 15);
		mComboBox = parent;
	}

	protected override void Render(Intersect.Client.Framework.Gwen.Skin.Base skin)
	{
		skin.DrawComboBoxArrow(this, mComboBox.IsHovered, mComboBox.IsActive, mComboBox.IsOpen, mComboBox.IsDisabled);
	}
}
