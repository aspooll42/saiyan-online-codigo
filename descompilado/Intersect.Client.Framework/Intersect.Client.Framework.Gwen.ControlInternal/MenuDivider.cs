using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.Framework.Gwen.Skin;

namespace Intersect.Client.Framework.Gwen.ControlInternal;

public class MenuDivider : Intersect.Client.Framework.Gwen.Control.Base
{
	public MenuDivider(Intersect.Client.Framework.Gwen.Control.Base parent)
		: base(parent)
	{
		base.Height = 1;
	}

	protected override void Render(Intersect.Client.Framework.Gwen.Skin.Base skin)
	{
		skin.DrawMenuDivider(this);
	}
}
