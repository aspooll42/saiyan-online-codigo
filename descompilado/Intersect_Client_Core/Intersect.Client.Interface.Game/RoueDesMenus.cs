using System;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.Framework.Gwen.Renderer;
using Intersect.Client.Framework.Gwen.Skin;
using Intersect.Client.Interface.Game.Techniques;
using Intersect.Client.Localization;

namespace Intersect.Client.Interface.Game;

public sealed class RoueDesMenus : Intersect.Client.Framework.Gwen.Control.Base
{
	private sealed record Entree(string Nom, string Icone, Action? Ouvrir, Entree[]? Groupe = null);

	private const int Rayon = 150;

	private const int Taille = 72;

	private const float SeuilVisee = 0.6f;

	private readonly Canvas _canvas;

	private readonly Entree[] _racine;

	private Entree[] _niveau;

	private Entree? _groupe;

	private int _choix = -1;

	public static RoueDesMenus? Courante { get; private set; }

	public bool EstOuverte => base.IsVisibleInTree;

	public RoueDesMenus(Canvas canvas, GameInterface ui)
		: base(canvas, "RoueDesMenus")
	{
		_canvas = canvas;
		base.MouseInputEnabled = false;
		base.IsHidden = true;
		Courante = this;
		_racine = new Entree[7]
		{
			new Entree(Strings.GameMenu.Items.ToString(), "hud_inventory.png", delegate
			{
				ui.GameMenu?.ToggleInventoryWindow();
			}),
			new Entree(Strings.GameMenu.Spells.ToString(), "hud_spell.png", delegate
			{
				ui.GameMenu?.ToggleSpellsWindow();
			}),
			new Entree(Strings.GameMenu.Character.ToString(), "hud_character.png", delegate
			{
				ui.GameMenu?.ToggleCharacterWindow();
			}),
			new Entree(Strings.MenuWheel.Quests.ToString(), "hud_quests.png", delegate
			{
				ui.GameMenu?.ToggleQuestsWindow();
			}),
			new Entree(Strings.MenuWheel.Map.ToString(), "hud_map.png", ui.ToggleWorldMapWindow),
			new Entree(Strings.GameMenu.Progression.ToString(), "hud_progression.png", null, new Entree[5]
			{
				new Entree(Strings.MenuWheel.Rewards.ToString(), "saiyan_menu_icone_cadeau.png", ui.ToggleDailyRewardWindow),
				new Entree(Strings.Achievements.Title.ToString(), "saiyan_star_gold.png", ui.ToggleAchievementsWindow),
				new Entree(Strings.Professions.Title.ToString(), "saiyan_menu_icone_metiers.png", ui.ToggleProfessionsWindow),
				new Entree(Strings.MenuWheel.Teachings.ToString(), "saiyan_star_gold.png", ui.ToggleTeachingsScreen),
				new Entree(Strings.Rankings.Title.ToString(), "saiyan_menu_icone_classements.png", ui.ToggleRankingsScreen)
			}),
			new Entree(Strings.GameMenu.Social.ToString(), "hud_social.png", null, new Entree[3]
			{
				new Entree(Strings.GameMenu.Friends.ToString(), "saiyan_carte_icone_ami.png", delegate
				{
					ui.GameMenu?.ToggleFriendsWindow();
				}),
				new Entree(Strings.GameMenu.Party.ToString(), "saiyan_carte_icone_groupe.png", delegate
				{
					ui.GameMenu?.TogglePartyWindow();
				}),
				new Entree(Strings.Guilds.Guild.ToString(), "saiyan_carte_icone_clan.png", delegate
				{
					ui.GameMenu?.ToggleGuildWindow();
				})
			})
		};
		_niveau = _racine;
	}

	protected override void Dispose(bool disposing)
	{
		if (Courante == this)
		{
			Courante = null;
		}
		base.Dispose(disposing);
	}

	public void Ouvrir()
	{
		_niveau = _racine;
		_groupe = null;
		_choix = -1;
		SetBounds(0, 0, _canvas.Width, _canvas.Height);
		base.IsHidden = false;
		BringToFront();
	}

	public void Fermer()
	{
		base.IsHidden = true;
		_choix = -1;
	}

	public void Viser(float x, float y)
	{
		if (!(x * x + y * y < 0.36f))
		{
			float num = (float)Math.PI * 2f / (float)_niveau.Length;
			int num2 = (int)MathF.Round(MathF.Atan2(x, y) / num);
			_choix = (num2 % _niveau.Length + _niveau.Length) % _niveau.Length;
		}
	}

	public void Relacher()
	{
		if (_choix >= 0)
		{
			Choisir();
		}
		else if (_groupe == null)
		{
			Fermer();
		}
	}

	public void Valider()
	{
		if (_choix >= 0)
		{
			Choisir();
		}
	}

	public void Retour()
	{
		if (_groupe == null)
		{
			Fermer();
			return;
		}
		_choix = Array.IndexOf<Entree>(_racine, _groupe);
		_niveau = _racine;
		_groupe = null;
	}

	private void Choisir()
	{
		Entree entree = _niveau[_choix];
		if (entree.Groupe != null)
		{
			_groupe = entree;
			_niveau = entree.Groupe;
			_choix = -1;
		}
		else
		{
			Fermer();
			entree.Ouvrir?.Invoke();
		}
	}

	protected override void Render(Intersect.Client.Framework.Gwen.Skin.Base skin)
	{
		Intersect.Client.Framework.Gwen.Renderer.Base renderer = skin.Renderer;
		TechniqueArt.Fill(renderer, new Rectangle(0, 0, base.Width, base.Height), TechniqueArt.Alpha(TechniqueArt.Void, 0.55f));
		int num = base.Width / 2;
		int num2 = base.Height / 2;
		TechniqueArt.Glow(renderer, num, num2, 240f, TechniqueArt.Alpha(TechniqueArt.Ki, 0.08f));
		for (int i = 0; i < _niveau.Length; i++)
		{
			Entree entree = _niveau[i];
			float x = (float)i * ((float)Math.PI * 2f) / (float)_niveau.Length;
			int num3 = num + (int)MathF.Round(MathF.Sin(x) * 150f);
			int num4 = num2 - (int)MathF.Round(MathF.Cos(x) * 150f);
			bool flag = i == _choix;
			Rectangle rect = new Rectangle(num3 - 36, num4 - 36, 72, 72);
			Rectangle rect2 = new Rectangle(rect.X + 3, rect.Y + 3, 66, 66);
			Color color = (flag ? TechniqueArt.Mix(TechniqueArt.Ki, TechniqueArt.White, 0.6f) : TechniqueArt.Line2);
			if (flag)
			{
				TechniqueArt.Glow(renderer, num3, num4, 68.4f, TechniqueArt.Alpha(TechniqueArt.Ki, 0.45f));
			}
			TechniqueArt.Image(renderer, "tech_hexa_bord.png", rect, color);
			TechniqueArt.Image(renderer, "tech_hexa.png", rect2, new Color(240, 8, 11, 19));
			if (flag)
			{
				TechniqueArt.Image(renderer, "tech_hexa_radial.png", rect2, TechniqueArt.Mix(TechniqueArt.Ki, new Color(255, 22, 28, 44), 0.35f));
			}
			TechniqueArt.Image(renderer, entree.Icone, new Rectangle(num3 - 18, num4 - 18, 36, 36), flag ? TechniqueArt.White : TechniqueArt.Alpha(TechniqueArt.White, 0.8f));
			TechniqueArt.Image(renderer, "tech_hexa_anneau.png", rect, color);
			TechniqueArt.TextCenter(renderer, TechniqueArt.Label, 10, entree.Nom, num3, num4 + 36 + 11, flag ? TechniqueArt.Text : TechniqueArt.Soft);
		}
		string text = ((_choix >= 0) ? _niveau[_choix].Nom : (_groupe?.Nom ?? Strings.MenuWheel.Title.ToString()));
		TechniqueArt.TextCenter(renderer, TechniqueArt.Heavy, 18, text.ToUpperInvariant(), num, num2 - 9, TechniqueArt.Text);
		string text2 = ((_groupe != null) ? Strings.MenuWheel.HelpGroup.ToString() : ((_choix >= 0 && _niveau[_choix].Groupe != null) ? Strings.MenuWheel.HelpOpenGroup.ToString() : Strings.MenuWheel.HelpRoot.ToString()));
		TechniqueArt.TextCenter(renderer, TechniqueArt.Body, 10, text2, num, num2 + 13, TechniqueArt.Soft);
	}
}
