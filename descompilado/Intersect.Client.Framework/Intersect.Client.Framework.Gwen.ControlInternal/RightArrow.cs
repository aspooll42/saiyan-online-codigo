using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.Framework.Gwen.Skin;

namespace Intersect.Client.Framework.Gwen.ControlInternal;

public class RightArrow : Intersect.Client.Framework.Gwen.Control.Base
{
	public RightArrow(Intersect.Client.Framework.Gwen.Control.Base parent)
		: base(parent)
	{
		base.MouseInputEnabled = false;
	}

	protected override void Render(Intersect.Client.Framework.Gwen.Skin.Base skin)
	{
		skin.DrawMenuRightArrow(this);
	}
}
