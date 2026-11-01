using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.Framework.Gwen.Skin;

namespace Intersect.Client.Framework.Gwen.ControlInternal;

public class Modal : Intersect.Client.Framework.Gwen.Control.Base
{
	public Modal(Intersect.Client.Framework.Gwen.Control.Base parent)
		: base(parent)
	{
		base.KeyboardInputEnabled = true;
		base.MouseInputEnabled = true;
		base.ShouldDrawBackground = true;
		Canvas canvas = base.Canvas;
		if (canvas != null)
		{
			base.Size = canvas.Size;
		}
	}

	protected override void Layout(Intersect.Client.Framework.Gwen.Skin.Base skin)
	{
		Canvas canvas = base.Canvas;
		if (canvas != null)
		{
			base.Size = canvas.Size;
		}
	}

	protected override void Render(Intersect.Client.Framework.Gwen.Skin.Base skin)
	{
		skin.DrawModalControl(this);
	}
}
