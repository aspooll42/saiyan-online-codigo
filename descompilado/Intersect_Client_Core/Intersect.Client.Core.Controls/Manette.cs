using System;
using System.Collections.Generic;
using System.Linq;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.Framework.Gwen.Input;
using Intersect.Client.Framework.Input;
using Intersect.Client.General;
using Intersect.Client.Interface;
using Intersect.Client.Interface.Game;
using Intersect.Client.Interface.Game.Dungeons;
using Intersect.Client.Interface.Game.Housing;
using Intersect.Client.Interface.Shared;
using Intersect.Framework.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Intersect.Client.Core.Controls;

public static class Manette
{
	public enum Genre
	{
		Xbox,
		PlayStation
	}

	public enum Bouton
	{
		A,
		B,
		X,
		Y,
		LB,
		RB,
		LT,
		RT,
		Vue,
		Menu,
		StickGauche,
		StickDroit,
		Haut,
		Bas,
		Gauche,
		Droite
	}

	private enum Situation
	{
		Jeu,
		Dialogue,
		Fenetre,
		Chat
	}

	public sealed class Sortie
	{
		public readonly List<Intersect.Client.Framework.GenericClasses.Keys> Touches = new List<Intersect.Client.Framework.GenericClasses.Keys>();

		public bool Gauche;

		public bool Droit;

		public int Molette;

		public float CurseurX;

		public float CurseurY;

		public bool Active;
	}

	private const float ZoneMorte = 0.22f;

	private const float SeuilDepart = 0.35f;

	private const float DemiDroite = 25f;

	private const float Hysteresis = 12f;

	private const float SeuilGachette = 0.45f;

	private const long SprintLacheMs = 350L;

	private const long MoletteMs = 110L;

	private static readonly Bouton[] TousLesBoutons = Enum.GetValues<Bouton>();

	private static readonly HashSet<Bouton> Verrous = new HashSet<Bouton>();

	private static string _situationAvant = string.Empty;

	private static bool _sprint;

	private static int _direction = -1;

	private static long _immobileDepuis;

	private static long _derniereMolette;

	public static bool EnUsage { get; private set; }

	public static Genre Style { get; private set; } = Genre.Xbox;

	public static bool TechniquesArmees { get; private set; }

	public static float StickDroitX { get; private set; }

	public static float StickDroitY { get; private set; }

	public static float StickGaucheX { get; private set; }

	public static float StickGaucheY { get; private set; }

	public static void ClavierOuSouris()
	{
		EnUsage = false;
		TechniquesArmees = false;
	}

	public static void Reinitialiser()
	{
		TechniquesArmees = false;
		_sprint = false;
		_direction = -1;
		_situationAvant = string.Empty;
		Verrous.Clear();
		RoueDesMenus courante = RoueDesMenus.Courante;
		if (courante != null && courante.EstOuverte)
		{
			courante.Fermer();
		}
	}

	public static void Nommer(string? nom)
	{
		if (!string.IsNullOrWhiteSpace(nom))
		{
			string[] source = new string[7] { "PS3", "PS4", "PS5", "Sony", "DualShock", "DualSense", "PlayStation" };
			Style = (source.Any((string mot) => nom.Contains(mot, StringComparison.OrdinalIgnoreCase)) ? Genre.PlayStation : Genre.Xbox);
		}
	}

	public static string Glyphe(Bouton bouton)
	{
		bool flag = Style == Genre.PlayStation;
		return bouton switch
		{
			Bouton.A => flag ? "manette_ps_croix.png" : "manette_xbox_a.png", 
			Bouton.B => flag ? "manette_ps_rond.png" : "manette_xbox_b.png", 
			Bouton.X => flag ? "manette_ps_carre.png" : "manette_xbox_x.png", 
			Bouton.Y => flag ? "manette_ps_triangle.png" : "manette_xbox_y.png", 
			Bouton.LB => flag ? "manette_ps_l1.png" : "manette_xbox_lb.png", 
			Bouton.RB => flag ? "manette_ps_r1.png" : "manette_xbox_rb.png", 
			Bouton.LT => flag ? "manette_ps_l2.png" : "manette_xbox_lt.png", 
			Bouton.RT => flag ? "manette_ps_r2.png" : "manette_xbox_rt.png", 
			Bouton.Vue => flag ? "manette_ps_share.png" : "manette_xbox_view.png", 
			Bouton.Menu => flag ? "manette_ps_options.png" : "manette_xbox_menu.png", 
			Bouton.StickGauche => flag ? "manette_ps_l3.png" : "manette_xbox_ls.png", 
			Bouton.StickDroit => flag ? "manette_ps_r3.png" : "manette_xbox_rs.png", 
			Bouton.Haut => "manette_croix_haut.png", 
			Bouton.Bas => "manette_croix_bas.png", 
			Bouton.Gauche => "manette_croix_gauche.png", 
			_ => "manette_croix_droite.png", 
		};
	}

	public static Bouton? BoutonDirectDeCase(int slot)
	{
		return slot switch
		{
			0 => Bouton.X, 
			1 => Bouton.Y, 
			_ => null, 
		};
	}

	public static Bouton? BoutonDeCase(int slot)
	{
		return slot switch
		{
			0 => Bouton.A, 
			1 => Bouton.B, 
			2 => Bouton.X, 
			3 => Bouton.Y, 
			4 => Bouton.Haut, 
			5 => Bouton.Droite, 
			6 => Bouton.Bas, 
			7 => Bouton.Gauche, 
			8 => Bouton.LB, 
			9 => Bouton.RB, 
			_ => null, 
		};
	}

	public static Sortie Lire(GamePadState etat, GamePadState avant, TimeSpan ecoule)
	{
		Sortie sortie = new Sortie();
		if (!etat.IsConnected)
		{
			StickDroitX = 0f;
			StickDroitY = 0f;
			StickGaucheX = 0f;
			StickGaucheY = 0f;
			Reinitialiser();
			return sortie;
		}
		Vector2 vector = Stick(etat.ThumbSticks.Left);
		Vector2 vector2 = Stick(etat.ThumbSticks.Right);
		StickDroitX = vector2.X;
		StickDroitY = vector2.Y;
		StickGaucheX = vector.X;
		StickGaucheY = vector.Y;
		sortie.Active = vector != Vector2.Zero || vector2 != Vector2.Zero || TousLesBoutons.Any((Bouton b) => Tenu(etat, b));
		if (sortie.Active)
		{
			EnUsage = true;
		}
		long milliseconds = Timing.Global.Milliseconds;
		Situation situation = SituationActuelle();
		RoueDesMenus courante = RoueDesMenus.Courante;
		bool flag = courante?.EstOuverte ?? false;
		bool flag2 = situation == Situation.Jeu && !flag && etat.Triggers.Left > 0.45f;
		string text = $"{situation}:{flag}:{flag2}";
		if (text != _situationAvant)
		{
			Bouton[] tousLesBoutons = TousLesBoutons;
			foreach (Bouton bouton in tousLesBoutons)
			{
				if (Tenu(etat, bouton))
				{
					Verrous.Add(bouton);
				}
			}
			_situationAvant = text;
		}
		Verrous.RemoveWhere((Bouton bouton2) => !Tenu(etat, bouton2));
		if (situation != Situation.Jeu)
		{
			_sprint = false;
			_direction = -1;
		}
		if (flag)
		{
			TechniquesArmees = false;
			Vector2 vector3 = ((vector.LengthSquared() > vector2.LengthSquared()) ? vector : vector2);
			courante.Viser(vector3.X, vector3.Y);
			if (!Tenu(etat, Bouton.Vue) && Tenu(avant, Bouton.Vue))
			{
				courante.Relacher();
			}
			else if (Presse(Bouton.A))
			{
				courante.Valider();
			}
			else if (Presse(Bouton.B))
			{
				courante.Retour();
			}
			else if (Presse(Bouton.Menu))
			{
				courante.Fermer();
			}
			return sortie;
		}
		bool flag3 = (uint)(situation - 2) <= 1u;
		Vector2 vector4 = ((!flag3) ? vector2 : ((vector.LengthSquared() > vector2.LengthSquared()) ? vector : vector2));
		if (vector4 != Vector2.Zero)
		{
			float num2 = vector4.Length();
			float num3 = Math.Min(1f, num2);
			float num4 = 260f + 1500f * num3 * num3;
			float num5 = (float)ecoule.TotalSeconds;
			sortie.CurseurX = vector4.X / num2 * num4 * num5;
			sortie.CurseurY = (0f - vector4.Y) / num2 * num4 * num5;
		}
		switch (situation)
		{
		case Situation.Jeu:
			TechniquesArmees = flag2;
			LireJeu(sortie, etat, vector, milliseconds, Presse, Tient);
			break;
		case Situation.Dialogue:
			TechniquesArmees = false;
			if (Tient(Bouton.B))
			{
				Commande(sortie, Control.AttackInteract);
			}
			if ((Globals.EventDialogs.FirstOrDefault()?.Options?.All(string.IsNullOrEmpty) ?? true) && Tient(Bouton.A))
			{
				sortie.Touches.Add(Intersect.Client.Framework.GenericClasses.Keys.Return);
			}
			Touche(sortie, Tient(Bouton.Haut), Intersect.Client.Framework.GenericClasses.Keys.D1);
			Touche(sortie, Tient(Bouton.Droite), Intersect.Client.Framework.GenericClasses.Keys.D2);
			Touche(sortie, Tient(Bouton.Bas), Intersect.Client.Framework.GenericClasses.Keys.D3);
			Touche(sortie, Tient(Bouton.Gauche), Intersect.Client.Framework.GenericClasses.Keys.D4);
			if (Tient(Bouton.Menu))
			{
				Commande(sortie, Control.OpenMenu);
			}
			break;
		case Situation.Fenetre:
			TechniquesArmees = false;
			LireFenetre(sortie, etat, milliseconds, Presse, Tient);
			break;
		default:
			TechniquesArmees = false;
			if (Tient(Bouton.B) || Tient(Bouton.Menu))
			{
				Commande(sortie, Control.OpenMenu);
			}
			break;
		}
		return sortie;
		bool Presse(Bouton b)
		{
			if (Tient(b))
			{
				return !Tenu(avant, b);
			}
			return false;
		}
		bool Tient(Bouton b)
		{
			if (Tenu(etat, b))
			{
				return !Verrous.Contains(b);
			}
			return false;
		}
	}

	private static void LireJeu(Sortie sortie, GamePadState etat, Vector2 gauche, long maintenant, Func<Bouton, bool> presse, Func<Bouton, bool> tient)
	{
		_direction = DirectionDuStick(gauche, _direction);
		bool flag = _direction >= 0;
		if (flag)
		{
			int direction = _direction;
			if (((uint)direction <= 1u || direction == 7) ? true : false)
			{
				Commande(sortie, Control.MoveRight);
			}
			else
			{
				direction = _direction;
				if ((uint)(direction - 3) <= 2u)
				{
					Commande(sortie, Control.MoveLeft);
				}
			}
			direction = _direction;
			if ((uint)(direction - 1) <= 2u)
			{
				Commande(sortie, Control.MoveUp);
			}
			else
			{
				direction = _direction;
				if ((uint)(direction - 5) <= 2u)
				{
					Commande(sortie, Control.MoveDown);
				}
			}
		}
		if (presse(Bouton.StickGauche))
		{
			_sprint = !_sprint;
			_immobileDepuis = maintenant;
		}
		if (flag)
		{
			_immobileDepuis = maintenant;
		}
		else if (maintenant - _immobileDepuis > 350)
		{
			_sprint = false;
		}
		if (_sprint && flag)
		{
			Commande(sortie, Control.Sprint);
		}
		if (RequestCardWindow.Current != null && presse(Bouton.Vue))
		{
			sortie.Touches.Add(Intersect.Client.Framework.GenericClasses.Keys.Return);
		}
		else if (presse(Bouton.Vue))
		{
			Intersect.Client.Interface.Interface.GameUi?.RoueDesMenus.Ouvrir();
		}
		if (TechniquesArmees)
		{
			for (int i = 0; i < 10; i++)
			{
				Bouton? bouton = BoutonDeCase(i);
				if (bouton.HasValue)
				{
					Bouton valueOrDefault = bouton.GetValueOrDefault();
					if (tient(valueOrDefault))
					{
						Commande(sortie, (Control)(4096 + i + 1));
					}
				}
			}
		}
		else
		{
			if (tient(Bouton.B))
			{
				Commande(sortie, Control.AttackInteract);
			}
			if (tient(Bouton.A))
			{
				Commande(sortie, Control.Dodge);
			}
			if (tient(Bouton.X))
			{
				Commande(sortie, Control.Hotkey1);
			}
			if (tient(Bouton.Y))
			{
				Commande(sortie, Control.Hotkey2);
			}
			if (tient(Bouton.RB))
			{
				Commande(sortie, Control.AutoTarget);
			}
			if (tient(Bouton.LB))
			{
				Commande(sortie, Control.ChargeKi);
			}
			if (tient(Bouton.Haut))
			{
				Commande(sortie, Control.ShowLocalMap);
			}
			if (tient(Bouton.Gauche))
			{
				Commande(sortie, Control.OpenInventory);
			}
			if (tient(Bouton.Droite))
			{
				Commande(sortie, Control.OpenSpells);
			}
			if (tient(Bouton.Bas))
			{
				Commande(sortie, Control.PickUp);
			}
			if (tient(Bouton.StickDroit))
			{
				Commande(sortie, Control.Dash);
			}
		}
		if (tient(Bouton.RT))
		{
			Commande(sortie, Control.Block);
		}
		if (tient(Bouton.Menu))
		{
			Commande(sortie, Control.OpenMenu);
		}
	}

	private static void LireFenetre(Sortie sortie, GamePadState etat, long maintenant, Func<Bouton, bool> presse, Func<Bouton, bool> tient)
	{
		if (CarteOuverte())
		{
			Touche(sortie, tient(Bouton.A), Intersect.Client.Framework.GenericClasses.Keys.Return);
		}
		else
		{
			sortie.Gauche = tient(Bouton.A);
		}
		sortie.Droit = tient(Bouton.X);
		Touche(sortie, tient(Bouton.Y), Intersect.Client.Framework.GenericClasses.Keys.Shift);
		Base keyboardFocus = InputHandler.KeyboardFocus;
		if (keyboardFocus is TextBox textBox && keyboardFocus.IsVisibleInTree && presse(Bouton.B))
		{
			textBox.Blur();
			Verrous.Add(Bouton.B);
		}
		else if (tient(Bouton.B) || tient(Bouton.Menu))
		{
			Commande(sortie, Control.OpenMenu);
		}
		if (maintenant - _derniereMolette >= 110)
		{
			if (tient(Bouton.LT))
			{
				sortie.Molette = 1;
				_derniereMolette = maintenant;
			}
			else if (tient(Bouton.RT))
			{
				sortie.Molette = -1;
				_derniereMolette = maintenant;
			}
		}
		if (Globals.GameState == GameStates.InGame && presse(Bouton.Vue))
		{
			Intersect.Client.Interface.Interface.GameUi?.RoueDesMenus.Ouvrir();
		}
	}

	private static bool CarteOuverte()
	{
		if (QuantityWindow.Current == null && ConfirmCardWindow.Current == null && HellBailWindow.Current == null && TrackingRadarWindow.Current == null && WantedBoardWindow.Current == null && WantedTurnInWindow.Current == null && RubanRougeWindow.Current == null)
		{
			return Intersect.Client.Interface.Interface.InputBlockingComponents.Any(delegate(Base composant)
			{
				bool isVisibleInTree = composant.IsVisibleInTree;
				bool flag = isVisibleInTree;
				if (flag)
				{
					bool flag2 = ((composant is HouseBuyWindow || composant is DungeonEntryWindow || composant is InputBox) ? true : false);
					flag = flag2;
				}
				return flag;
			});
		}
		return true;
	}

	private static Situation SituationActuelle()
	{
		if (Globals.GameState == GameStates.InGame && Intersect.Client.Interface.Interface.HasInGameUI)
		{
			GameInterface gameUi = Intersect.Client.Interface.Interface.GameUi;
			if (gameUi != null)
			{
				if (gameUi.ChatFocussed)
				{
					return Situation.Chat;
				}
				EventWindow current = EventWindow.Current;
				if (current != null && !current.IsHidden && Globals.EventDialogs.Count > 0)
				{
					return Situation.Dialogue;
				}
				if (!gameUi.HasOpenWindows())
				{
					return Situation.Jeu;
				}
				return Situation.Fenetre;
			}
		}
		return Situation.Fenetre;
	}

	private static void Commande(Sortie sortie, Control control)
	{
		if (!Controls.ActiveControls.TryGetMappingFor(control, out ControlMapping mapping))
		{
			return;
		}
		ControlBinding controlBinding = null;
		foreach (ControlBinding binding in mapping.Bindings)
		{
			if (binding.Key == Intersect.Client.Framework.GenericClasses.Keys.None)
			{
				continue;
			}
			if (binding.IsMouseKey)
			{
				if (controlBinding == null)
				{
					controlBinding = binding;
				}
				continue;
			}
			if (binding.Modifier != Intersect.Client.Framework.GenericClasses.Keys.None)
			{
				sortie.Touches.Add(binding.Modifier);
			}
			sortie.Touches.Add(binding.Key);
			return;
		}
		if (controlBinding != null && controlBinding.Key.TryGetMouseButton(out var mouseButton))
		{
			switch (mouseButton)
			{
			case MouseButton.Left:
				sortie.Gauche = true;
				break;
			case MouseButton.Right:
				sortie.Droit = true;
				break;
			}
		}
	}

	private static void Touche(Sortie sortie, bool tenue, Intersect.Client.Framework.GenericClasses.Keys touche)
	{
		if (tenue)
		{
			sortie.Touches.Add(touche);
		}
	}

	private static Vector2 Stick(Vector2 valeur)
	{
		if (!(valeur.Length() < 0.22f))
		{
			return valeur;
		}
		return Vector2.Zero;
	}

	private static int DirectionDuStick(Vector2 stick, int actuelle)
	{
		if (stick.Length() < ((actuelle < 0) ? 0.35f : 0.22f))
		{
			return -1;
		}
		float num = MathF.Atan2(stick.Y, stick.X) * 180f / (float)Math.PI;
		if (num < 0f)
		{
			num += 360f;
		}
		int num2 = DirectionBrute(num);
		if (actuelle < 0 || num2 == actuelle)
		{
			return num2;
		}
		float num3 = MathF.Abs(((num - (float)actuelle * 45f) % 360f + 540f) % 360f - 180f);
		float num4 = ((actuelle % 2 == 0) ? 25f : 20f);
		if (!(num3 > num4 + 12f))
		{
			return actuelle;
		}
		return num2;
	}

	private static int DirectionBrute(float degres)
	{
		float num = MathF.Round(degres / 90f) * 90f;
		if (MathF.Abs(degres - num) <= 25f)
		{
			return (int)(num / 45f) % 8;
		}
		return ((int)MathF.Floor(degres / 90f) * 2 + 1) % 8;
	}

	private static bool Tenu(GamePadState etat, Bouton bouton)
	{
		return bouton switch
		{
			Bouton.A => etat.Buttons.A == ButtonState.Pressed, 
			Bouton.B => etat.Buttons.B == ButtonState.Pressed, 
			Bouton.X => etat.Buttons.X == ButtonState.Pressed, 
			Bouton.Y => etat.Buttons.Y == ButtonState.Pressed, 
			Bouton.LB => etat.Buttons.LeftShoulder == ButtonState.Pressed, 
			Bouton.RB => etat.Buttons.RightShoulder == ButtonState.Pressed, 
			Bouton.LT => etat.Triggers.Left > 0.45f, 
			Bouton.RT => etat.Triggers.Right > 0.45f, 
			Bouton.Vue => etat.Buttons.Back == ButtonState.Pressed, 
			Bouton.Menu => etat.Buttons.Start == ButtonState.Pressed, 
			Bouton.StickGauche => etat.Buttons.LeftStick == ButtonState.Pressed, 
			Bouton.StickDroit => etat.Buttons.RightStick == ButtonState.Pressed, 
			Bouton.Haut => etat.DPad.Up == ButtonState.Pressed, 
			Bouton.Bas => etat.DPad.Down == ButtonState.Pressed, 
			Bouton.Gauche => etat.DPad.Left == ButtonState.Pressed, 
			_ => etat.DPad.Right == ButtonState.Pressed, 
		};
	}
}
