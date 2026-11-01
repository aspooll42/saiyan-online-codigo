using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Gwen.Skin;

namespace Intersect.Client.Framework.Gwen.Control;

public class Panel : Base
{
	private Color? _backgroundColor;

	public Color? BackgroundColor
	{
		get
		{
			return _backgroundColor;
		}
		set
		{
			SetAndDoIfChanged(ref _backgroundColor, value, Base.Invalidate);
		}
	}

	public Panel(Base parent, string? name = null)
		: base(parent, name)
	{
	}

	protected override void Render(Intersect.Client.Framework.Gwen.Skin.Base skin)
	{
		base.Render(skin);
		if (base.ShouldDrawBackground)
		{
			Color backgroundColor = BackgroundColor;
			if ((object)backgroundColor == null)
			{
				skin.DrawPanel(this);
				return;
			}
			Rectangle bounds = base.Bounds;
			bounds.X = 0;
			bounds.Y = 0;
			skin.DrawRectFill(bounds, backgroundColor);
		}
	}
}
