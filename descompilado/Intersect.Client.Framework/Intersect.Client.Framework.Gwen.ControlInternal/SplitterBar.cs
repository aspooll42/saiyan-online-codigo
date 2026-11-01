using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.Framework.Gwen.Skin;

namespace Intersect.Client.Framework.Gwen.ControlInternal;

public class SplitterBar : Dragger
{
	public SplitterBar(Intersect.Client.Framework.Gwen.Control.Base parent)
		: base(parent)
	{
		base.Target = this;
		base.RestrictToParent = true;
	}

	protected override void Render(Intersect.Client.Framework.Gwen.Skin.Base skin)
	{
		if (base.ShouldDrawBackground)
		{
			skin.DrawSplitter(this);
		}
	}

	protected override void Layout(Intersect.Client.Framework.Gwen.Skin.Base skin)
	{
		MoveTo(base.X, base.Y);
	}
}
