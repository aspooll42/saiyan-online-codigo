using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.Framework.Gwen.Renderer;
using Intersect.Client.Framework.Gwen.Skin;
using Intersect.Client.Interface.Game.EntityPanel;

namespace Intersect.Client.Interface.Shared;

internal sealed class OptionsKeyNotice : Intersect.Client.Framework.Gwen.Control.Base
{
	public string? Notice { get; set; }

	public string Hint { get; set; } = string.Empty;

	public OptionsKeyNotice(Intersect.Client.Framework.Gwen.Control.Base parent, string name)
		: base(parent, name)
	{
		base.MouseInputEnabled = false;
	}

	protected override void Render(Intersect.Client.Framework.Gwen.Skin.Base skin)
	{
		Intersect.Client.Framework.Gwen.Renderer.Base renderer = skin.Renderer;
		if (string.IsNullOrEmpty(Notice))
		{
			SaiyanHud.TextLeft(renderer, OptionsStyle.Body, 10, Hint, 14, 0, base.Height, OptionsStyle.Dim, shadow: false);
			return;
		}
		SaiyanHud.Draw(renderer, OptionsStyle.Texture("saiyan_menu_survol.png"), 0, 0, base.Width, base.Height, OptionsStyle.NoticeVeil);
		renderer.DrawColor = OptionsStyle.Orange;
		renderer.DrawFilledRect(new Rectangle(0, 0, 3, base.Height));
		OptionsStyle.Draw(renderer, "saiyan_option_alerte.png", 14, base.Height / 2 - 9, SaiyanHud.White);
		SaiyanHud.TextLeft(renderer, OptionsStyle.Body, 10, Notice, 40, 0, base.Height, OptionsStyle.Label, shadow: false);
	}
}
