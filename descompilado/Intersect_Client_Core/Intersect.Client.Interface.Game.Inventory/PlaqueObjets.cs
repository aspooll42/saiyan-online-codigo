using System;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.Framework.Gwen.Renderer;
using Intersect.Client.Framework.Gwen.Skin;
using Intersect.Client.Framework.Input;
using Intersect.Client.Interface.Game.Techniques;
using Intersect.Client.Localization;

namespace Intersect.Client.Interface.Game.Inventory;

internal sealed class PlaqueObjets : ImagePanel
{
	private const int Marge = 12;

	private static readonly Color Encre = new Color(255, 26, 18, 0);

	private static readonly Color FondTouche = new Color(255, 17, 24, 39);

	private static readonly TechniqueArt.Stops Titre = new TechniqueArt.Stops(TechniqueArt.White, 0.28f, new Color(255, 255, 194, 122), 0.72f, TechniqueArt.Ki);

	public int Nombre { get; set; }

	public Button? Bouton { get; set; }

	public PlaqueObjets(Intersect.Client.Framework.Gwen.Control.Base parent)
		: base(parent, "MapItemWindow")
	{
	}

	protected override void Render(Intersect.Client.Framework.Gwen.Skin.Base skin)
	{
		base.Render(skin);
		Intersect.Client.Framework.Gwen.Renderer.Base renderer = skin.Renderer;
		string text = Strings.MapItemWindow.Title.ToString().ToUpperInvariant();
		Point point = TechniqueArt.Measure(renderer, TechniqueArt.Display, 20, text);
		TechniqueArt.Styled(renderer, TechniqueArt.Display, 20, text, 12, 9, Titre);
		if (Nombre > 0)
		{
			string text2 = Nombre.ToString();
			Point point2 = TechniqueArt.Measure(renderer, TechniqueArt.Display, 12, text2);
			Rectangle rect = new Rectangle(base.Width - 12 - point2.X - 14, 9 + (point.Y - point2.Y - 4) / 2, point2.X + 14, point2.Y + 4);
			TechniqueArt.Fill(renderer, rect, TechniqueArt.Gold);
			TechniqueArt.Write(renderer, TechniqueArt.Display, 12, text2, rect.X + 7, rect.Y + 2, Encre);
		}
		TechniqueArt.Fill(renderer, new Rectangle(12, 40, base.Width - 24, 1), TechniqueArt.Line);
		Button bouton = Bouton;
		Button button = ((bouton != null && !bouton.IsHidden) ? bouton : null);
		int num = (button?.Y ?? (base.Height - 12 - 30)) - 8 - 11;
		string text3 = Touches.NomDe(Control.PickUp) ?? string.Empty;
		Rectangle rect2 = new Rectangle(12, num - 11, Math.Max(26, TechniqueArt.Measure(renderer, TechniqueArt.Label, 12, text3).X + 14), 22);
		TechniqueArt.Fill(renderer, rect2, TechniqueArt.Line2);
		TechniqueArt.Fill(renderer, new Rectangle(rect2.X + 1, rect2.Y + 1, rect2.Width - 2, rect2.Height - 2), FondTouche);
		TechniqueArt.TextCenter(renderer, TechniqueArt.Label, 12, text3, rect2.X + rect2.Width / 2, num, TechniqueArt.Text);
		TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, 12, Strings.MapItemWindow.PickUpHint.ToString(), rect2.Right + 8, num, TechniqueArt.Soft);
		if (button != null)
		{
			bool isHovered = button.IsHovered;
			Rectangle rect3 = new Rectangle(button.X, button.Y - (isHovered ? 1 : 0), button.Width, button.Height);
			TechniqueArt.Patch(renderer, isHovered ? "tech_bouton_survol.png" : "tech_bouton.png", rect3, TechniqueArt.White, 12);
			string text4 = Strings.MapItemWindow.LootButton.ToString().ToUpperInvariant();
			Point point3 = TechniqueArt.Measure(renderer, TechniqueArt.Display, 14, text4);
			TechniqueArt.Italic(renderer, TechniqueArt.Display, 14, text4, rect3.X + (rect3.Width - point3.X) / 2, rect3.Y + (rect3.Height - point3.Y) / 2, Encre);
		}
	}
}
