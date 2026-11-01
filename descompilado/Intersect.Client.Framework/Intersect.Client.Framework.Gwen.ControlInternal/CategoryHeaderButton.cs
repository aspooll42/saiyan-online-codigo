using Intersect.Client.Framework.Gwen.Control;

namespace Intersect.Client.Framework.Gwen.ControlInternal;

public class CategoryHeaderButton : Button
{
	public CategoryHeaderButton(Base parent)
		: base(parent)
	{
		base.ShouldDrawBackground = false;
		base.IsToggle = true;
		base.TextAlign = Pos.Center;
		base.Padding = new Padding(3, 0, 3, 0);
	}

	public override void UpdateColors()
	{
		if (base.IsActive || base.ToggleState)
		{
			base.TextColor = base.Skin.Colors.Category.HeaderClosed;
		}
		else
		{
			base.TextColor = base.Skin.Colors.Category.Header;
		}
	}
}
