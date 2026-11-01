using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.Framework.Gwen.Skin;

namespace Intersect.Client.Framework.Gwen.ControlInternal;

public class ColorButton : Button
{
	private Color mColor;

	public Color Color
	{
		get
		{
			return mColor;
		}
		set
		{
			mColor = value;
		}
	}

	public ColorButton(Intersect.Client.Framework.Gwen.Control.Base parent)
		: base(parent)
	{
		mColor = Intersect.Color.Black;
		Text = string.Empty;
	}

	protected override void Render(Intersect.Client.Framework.Gwen.Skin.Base skin)
	{
		skin.Renderer.DrawColor = mColor;
		skin.Renderer.DrawFilledRect(base.RenderBounds);
	}
}
