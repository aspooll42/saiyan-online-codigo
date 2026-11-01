using Intersect.Client.Entities;
using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.Framework.Gwen.Skin;

namespace Intersect.Client.Interface.Game.Inventory;

public sealed class NamePlatePreview : Intersect.Client.Framework.Gwen.Control.Base
{
	public NamePlate.Content? Content { get; set; }

	public int PlateBottom { get; set; }

	public NamePlatePreview(Intersect.Client.Framework.Gwen.Control.Base parent, string name)
		: base(parent, name)
	{
		base.MouseInputEnabled = false;
	}

	protected override void Render(Intersect.Client.Framework.Gwen.Skin.Base skin)
	{
		base.Render(skin);
		NamePlate.Content content = Content;
		if ((object)content != null)
		{
			NamePlate.DrawInterface(content, skin.Renderer, base.Width / 2, PlateBottom);
		}
	}
}
