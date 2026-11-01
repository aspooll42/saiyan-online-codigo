using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.Framework.Gwen.Skin;

namespace Intersect.Client.Framework.Gwen.ControlInternal;

internal class UpDownButtonDown : Button
{
	public UpDownButtonDown(Intersect.Client.Framework.Gwen.Control.Base parent)
		: base(parent)
	{
		SetSize(7, 7);
	}

	protected override void Render(Intersect.Client.Framework.Gwen.Skin.Base skin)
	{
		skin.DrawNumericUpDownButton(this, base.IsActive, up: false);
	}
}
