using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.Framework.Gwen.Renderer;
using Intersect.Client.Framework.Gwen.Skin;
using Intersect.Client.Interface.Game.EntityPanel;

namespace Intersect.Client.Interface.Shared;

internal sealed class OptionsHeader : Intersect.Client.Framework.Gwen.Control.Base
{
	public string Title { get; set; } = string.Empty;

	public string Description { get; set; } = string.Empty;

	public string DescriptionEnd { get; set; } = string.Empty;

	public OptionsHeader(Intersect.Client.Framework.Gwen.Control.Base parent, string name)
		: base(parent, name)
	{
		base.MouseInputEnabled = false;
	}

	protected override void Render(Intersect.Client.Framework.Gwen.Skin.Base skin)
	{
		Intersect.Client.Framework.Gwen.Renderer.Base renderer = skin.Renderer;
		SaiyanHud.TextLeft(renderer, OptionsStyle.Strong, 14, Title, 2, 0, 30, OptionsStyle.White);
		SaiyanHud.TextLeft(renderer, OptionsStyle.Body, 10, Description, 2, 26, 22, OptionsStyle.Soft, shadow: false);
		if (!string.IsNullOrEmpty(DescriptionEnd))
		{
			int num = 2 + SaiyanHud.Measure(renderer, OptionsStyle.Body, 10, Description).X + 6;
			OptionsStyle.Draw(renderer, "saiyan_option_relance.png", num, 29, OptionsStyle.Glyph);
			SaiyanHud.TextLeft(renderer, OptionsStyle.Body, 10, DescriptionEnd, num + 20, 26, 22, OptionsStyle.Soft, shadow: false);
		}
	}
}
