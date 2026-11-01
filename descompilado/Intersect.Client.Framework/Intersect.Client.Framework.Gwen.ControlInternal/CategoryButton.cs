using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.Framework.Gwen.Skin;

namespace Intersect.Client.Framework.Gwen.ControlInternal;

public class CategoryButton : Button
{
	internal bool mAlt;

	public CategoryButton(Intersect.Client.Framework.Gwen.Control.Base parent)
		: base(parent)
	{
		base.TextAlign = Pos.Left | Pos.CenterV;
		mAlt = false;
		base.IsToggle = true;
		base.Padding = new Padding(3, 0, 3, 0);
	}

	protected override void Render(Intersect.Client.Framework.Gwen.Skin.Base skin)
	{
		if (mAlt)
		{
			if (base.IsActive || base.ToggleState)
			{
				base.Skin.Renderer.DrawColor = skin.Colors.Category.LineAlt.ButtonSelected;
			}
			else if (IsHovered)
			{
				base.Skin.Renderer.DrawColor = skin.Colors.Category.LineAlt.ButtonHover;
			}
			else
			{
				base.Skin.Renderer.DrawColor = skin.Colors.Category.LineAlt.Button;
			}
		}
		else if (base.IsActive || base.ToggleState)
		{
			base.Skin.Renderer.DrawColor = skin.Colors.Category.Line.ButtonSelected;
		}
		else if (IsHovered)
		{
			base.Skin.Renderer.DrawColor = skin.Colors.Category.Line.ButtonHover;
		}
		else
		{
			base.Skin.Renderer.DrawColor = skin.Colors.Category.Line.Button;
		}
		skin.Renderer.DrawFilledRect(base.RenderBounds);
	}

	public override void UpdateColors()
	{
		if (mAlt)
		{
			if (base.IsActive || base.ToggleState)
			{
				base.TextColor = base.Skin.Colors.Category.LineAlt.TextSelected;
			}
			else if (IsHovered)
			{
				base.TextColor = base.Skin.Colors.Category.LineAlt.TextHover;
			}
			else
			{
				base.TextColor = base.Skin.Colors.Category.LineAlt.Text;
			}
		}
		else if (base.IsActive || base.ToggleState)
		{
			base.TextColor = base.Skin.Colors.Category.Line.TextSelected;
		}
		else if (IsHovered)
		{
			base.TextColor = base.Skin.Colors.Category.Line.TextHover;
		}
		else
		{
			base.TextColor = base.Skin.Colors.Category.Line.Text;
		}
	}
}
