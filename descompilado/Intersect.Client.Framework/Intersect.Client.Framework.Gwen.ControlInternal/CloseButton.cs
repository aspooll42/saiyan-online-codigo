using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.Framework.Gwen.Skin;

namespace Intersect.Client.Framework.Gwen.ControlInternal;

public class CloseButton : Button
{
	private readonly WindowControl mWindow;

	public CloseButton(Intersect.Client.Framework.Gwen.Control.Base parent, WindowControl owner, string? name = null)
		: base(parent, name, disableText: true)
	{
		mWindow = owner;
	}

	protected override void Render(Intersect.Client.Framework.Gwen.Skin.Base skin)
	{
		skin.DrawWindowCloseButton(this);
	}
}
