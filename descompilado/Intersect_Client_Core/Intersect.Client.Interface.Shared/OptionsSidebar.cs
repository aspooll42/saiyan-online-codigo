using System;
using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.Framework.Gwen.Input;
using Intersect.Client.Framework.Gwen.Renderer;
using Intersect.Client.Framework.Gwen.Skin;
using Intersect.Client.Framework.Input;
using Intersect.Client.Interface.Game.EntityPanel;

namespace Intersect.Client.Interface.Shared;

internal sealed class OptionsSidebar : Intersect.Client.Framework.Gwen.Control.Base
{
	private const int ItemTop = 36;

	private const int ItemStep = 44;

	public string Heading { get; set; } = string.Empty;

	public (string Label, string Glyph)[] Items { get; set; } = Array.Empty<(string, string)>();

	public int Selected { get; set; }

	public Action<int>? Picked { get; set; }

	public OptionsSidebar(Intersect.Client.Framework.Gwen.Control.Base parent, string name)
		: base(parent, name)
	{
		base.MouseInputEnabled = true;
	}

	protected override void Render(Intersect.Client.Framework.Gwen.Skin.Base skin)
	{
		Intersect.Client.Framework.Gwen.Renderer.Base renderer = skin.Renderer;
		OptionsStyle.Caps(renderer, Heading, 14, 12, 18, OptionsStyle.Dim);
		int num = HoveredItem();
		for (int i = 0; i < Items.Length; i++)
		{
			int num2 = 36 + i * 44;
			bool flag = i == Selected;
			string name = (flag ? "saiyan_tab_selected.png" : ((i == num) ? "saiyan_tab_hover.png" : "saiyan_tab.png"));
			OptionsStyle.NinePatch(renderer, name, 8, num2, 168, 38, 10, SaiyanHud.White);
			OptionsStyle.Draw(renderer, "saiyan_option_glyphe_" + Items[i].Glyph + ".png", 18, num2 + 7, (flag || i == num) ? OptionsStyle.OrangeLight : OptionsStyle.Glyph);
			SaiyanHud.TextLeft(renderer, OptionsStyle.Body, 11, Items[i].Label, 52, num2, 38, flag ? OptionsStyle.White : ((i == num) ? OptionsStyle.OrangeLight : OptionsStyle.Soft), shadow: false);
		}
	}

	protected override void OnMouseClicked(MouseButton mouseButton, Point mousePosition, bool userAction = true)
	{
		base.OnMouseClicked(mouseButton, mousePosition, userAction);
		int num = ItemAt(CanvasPosToLocal(mousePosition));
		if (num >= 0)
		{
			Picked?.Invoke(num);
		}
	}

	private int HoveredItem()
	{
		if (!IsHovered)
		{
			return -1;
		}
		return ItemAt(CanvasPosToLocal(InputHandler.MousePosition));
	}

	private int ItemAt(Point local)
	{
		if (local.X < 8 || local.X >= 176 || local.Y < 36)
		{
			return -1;
		}
		int num = (local.Y - 36) / 44;
		if (num >= Items.Length || (local.Y - 36) % 44 >= 38)
		{
			return -1;
		}
		return num;
	}
}
