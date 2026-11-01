using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.Framework.Gwen.Skin;

namespace Intersect.Client.Framework.Gwen.ControlInternal;

public class TabControlInner : Intersect.Client.Framework.Gwen.Control.Base
{
	internal TabControlInner(Intersect.Client.Framework.Gwen.Control.Base parent, string? name = null)
		: base(parent, name)
	{
	}

	protected override void Render(Intersect.Client.Framework.Gwen.Skin.Base skin)
	{
		skin.DrawTabControl(this);
	}
}
