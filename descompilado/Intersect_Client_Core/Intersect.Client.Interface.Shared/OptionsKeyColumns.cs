using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.Framework.Gwen.Renderer;
using Intersect.Client.Framework.Gwen.Skin;

namespace Intersect.Client.Interface.Shared;

internal sealed class OptionsKeyColumns : Intersect.Client.Framework.Gwen.Control.Base
{
	public string Action { get; set; } = string.Empty;

	public string Primary { get; set; } = string.Empty;

	public string Secondary { get; set; } = string.Empty;

	public int SheetWidth { get; set; }

	public OptionsKeyColumns(Intersect.Client.Framework.Gwen.Control.Base parent, string name)
		: base(parent, name)
	{
		base.MouseInputEnabled = false;
	}

	protected override void Render(Intersect.Client.Framework.Gwen.Skin.Base skin)
	{
		Intersect.Client.Framework.Gwen.Renderer.Base renderer = skin.Renderer;
		int num = SheetWidth - 16;
		OptionsStyle.Caps(renderer, Action, 16, 0, base.Height, OptionsStyle.Dim);
		Centered(renderer, Primary, num - 248 - 10 + 62);
		Centered(renderer, Secondary, num - 62);
	}

	private void Centered(Intersect.Client.Framework.Gwen.Renderer.Base renderer, string text, int center)
	{
		int num = OptionsStyle.CapsWidth(renderer, text);
		OptionsStyle.Caps(renderer, text, center - num / 2, 0, base.Height, OptionsStyle.Dim);
	}
}
