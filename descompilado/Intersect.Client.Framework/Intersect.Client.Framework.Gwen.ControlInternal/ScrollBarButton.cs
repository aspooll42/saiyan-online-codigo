using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.Framework.Gwen.Skin;

namespace Intersect.Client.Framework.Gwen.ControlInternal;

public class ScrollBarButton : Button
{
	private Pos _direction = Pos.Top;

	public virtual Pos Direction => _direction;

	public ScrollBarButton(Intersect.Client.Framework.Gwen.Control.Base parent)
		: base(parent, null, disableText: true)
	{
		Font = null;
		base.MinimumSize = new Point(15, 15);
		base.Size = new Point(15, 15);
		base.MaximumSize = new Point(15, 15);
	}

	public void SetDirectionUp()
	{
		_direction = Pos.Top;
	}

	public void SetDirectionDown()
	{
		_direction = Pos.Bottom;
	}

	public void SetDirectionLeft()
	{
		_direction = Pos.Left;
	}

	public void SetDirectionRight()
	{
		_direction = Pos.Right;
	}

	protected override void Render(Intersect.Client.Framework.Gwen.Skin.Base skin)
	{
		skin.DrawScrollButton(this, _direction, base.IsActive, IsHovered, base.IsDisabled);
	}
}
