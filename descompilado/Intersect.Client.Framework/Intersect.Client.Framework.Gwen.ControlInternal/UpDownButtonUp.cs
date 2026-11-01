using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.Framework.Gwen.Skin;

namespace Intersect.Client.Framework.Gwen.ControlInternal;

public class UpDownButtonUp : Button
{
	public UpDownButtonUp(Intersect.Client.Framework.Gwen.Control.Base parent)
		: base(parent)
	{
		SetSize(7, 7);
	}

	protected override void Render(Intersect.Client.Framework.Gwen.Skin.Base skin)
	{
		skin.DrawNumericUpDownButton(this, base.IsActive, up: true);
	}
}
