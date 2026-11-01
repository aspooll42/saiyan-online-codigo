using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.Framework.Gwen.Skin;

namespace Intersect.Client.Framework.Gwen.ControlInternal;

public class TreeToggleButton : Button
{
	public TreeToggleButton(Intersect.Client.Framework.Gwen.Control.Base parent)
		: base(parent)
	{
		base.IsToggle = true;
		base.IsTabable = false;
	}

	protected override void RenderFocus(Intersect.Client.Framework.Gwen.Skin.Base skin)
	{
	}

	protected override void Render(Intersect.Client.Framework.Gwen.Skin.Base skin)
	{
		skin.DrawTreeButton(this, base.ToggleState);
	}
}
