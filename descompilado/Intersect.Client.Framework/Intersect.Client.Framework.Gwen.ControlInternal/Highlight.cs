using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.Framework.Gwen.Skin;

namespace Intersect.Client.Framework.Gwen.ControlInternal;

public class Highlight : Intersect.Client.Framework.Gwen.Control.Base
{
	public Highlight(Intersect.Client.Framework.Gwen.Control.Base parent)
		: base(parent)
	{
	}

	protected override void Render(Intersect.Client.Framework.Gwen.Skin.Base skin)
	{
		skin.DrawHighlight(this);
	}
}
