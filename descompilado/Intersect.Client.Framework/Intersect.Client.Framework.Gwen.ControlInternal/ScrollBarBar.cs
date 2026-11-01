using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.Framework.Gwen.Skin;

namespace Intersect.Client.Framework.Gwen.ControlInternal;

public class ScrollBarBar : Dragger
{
	private bool mHorizontal;

	public bool IsHorizontal
	{
		get
		{
			return mHorizontal;
		}
		set
		{
			mHorizontal = value;
		}
	}

	public bool IsVertical
	{
		get
		{
			return !mHorizontal;
		}
		set
		{
			mHorizontal = !value;
		}
	}

	public ScrollBarBar(Intersect.Client.Framework.Gwen.Control.Base parent)
		: base(parent)
	{
		base.RestrictToParent = true;
		base.Target = this;
		SetSound(ButtonSoundState.Hover, "menu_hover.wav");
		SetSound(ButtonSoundState.MouseDown, "menu_click.wav");
		SetSound(ButtonSoundState.MouseUp, "menu_click.wav");
	}

	protected override void Render(Intersect.Client.Framework.Gwen.Skin.Base skin)
	{
		skin.DrawScrollBarBar(this);
		base.Render(skin);
	}

	protected override void OnMouseMoved(int x, int y, int dx, int dy)
	{
		base.OnMouseMoved(x, y, dx, dy);
		if (base.IsActive)
		{
			InvalidateParent();
		}
	}

	protected override void Layout(Intersect.Client.Framework.Gwen.Skin.Base skin)
	{
		if (base.Parent != null)
		{
			MoveTo(base.X, base.Y);
		}
	}

	protected override void OnBoundsChanged(Rectangle oldBounds, Rectangle newBounds)
	{
		base.OnBoundsChanged(oldBounds, newBounds);
		InvalidateParent();
	}
}
