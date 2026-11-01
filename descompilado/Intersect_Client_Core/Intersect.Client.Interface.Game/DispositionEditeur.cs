using System;
using System.Collections.Generic;
using System.Linq;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.Framework.Gwen.Input;
using Intersect.Client.Framework.Gwen.Renderer;
using Intersect.Client.Framework.Gwen.Skin;
using Intersect.Client.Framework.Input;
using Intersect.Client.Interface.Game.EntityPanel;
using Intersect.Client.Interface.Game.Techniques;
using Intersect.Client.Localization;
using Intersect.Framework.Core;
using Intersect.Localization;

namespace Intersect.Client.Interface.Game;

public sealed class DispositionEditeur : Intersect.Client.Framework.Gwen.Control.Base
{
	private readonly record struct Bouton(Rectangle Zone, Action Geste);

	private sealed class Quadrille : Intersect.Client.Framework.Gwen.Control.Base
	{
		public Quadrille(Canvas canvas)
			: base(canvas, "DispositionVoile")
		{
			base.MouseInputEnabled = false;
			base.IsHidden = true;
		}

		protected override void Render(Intersect.Client.Framework.Gwen.Skin.Base skin)
		{
			base.Render(skin);
			Intersect.Client.Framework.Gwen.Renderer.Base renderer = skin.Renderer;
			TechniqueArt.Fill(renderer, new Rectangle(0, 0, base.Width, base.Height), Voile);
			for (int i = 0; i < base.Width; i += 50)
			{
				HudShapes.Fill(renderer, i, 0, 1, base.Height, Quadrillage);
			}
			for (int j = 0; j < base.Height; j += 50)
			{
				HudShapes.Fill(renderer, 0, j, base.Width, 1, Quadrillage);
			}
			HudShapes.Fill(renderer, base.Width / 2, 0, 1, base.Height, Axe);
			HudShapes.Fill(renderer, 0, base.Height / 2, base.Width, 1, Axe);
		}
	}

	private const int Seuil = 7;

	private static readonly Color Voile = new Color(107, 4, 7, 14);

	private static readonly Color Quadrillage = new Color(18, 255, 255, 255);

	private static readonly Color Axe = new Color(140, 255, 172, 82);

	private static readonly Color Contour = new Color(191, 255, 172, 82);

	private static readonly Color Etiquette = new Color(240, 255, 122, 26);

	private static readonly Color Encre = new Color(255, 20, 10, 2);

	private static readonly Color Blanc = new Color(255, 255, 255, 255);

	private static readonly Color Texte = new Color(255, 233, 237, 246);

	private static readonly Color Doux = new Color(255, 147, 160, 186);

	private static readonly Color Bord = new Color(255, 86, 98, 126);

	private static readonly Color BordEteint = new Color(255, 44, 53, 72);

	private static readonly Color Fond = new Color(255, 11, 15, 26);

	private static readonly Color FondReleve = new Color(255, 23, 29, 46);

	private static readonly Color Bandeau = new Color(235, 7, 10, 18);

	private static readonly Color Orange = new Color(255, 255, 122, 26);

	private static readonly Color Guide = new Color(255, 90, 240, 255);

	private static readonly Color Fantome = new Color(153, 16, 22, 38);

	private static readonly Color Pointilles = new Color(140, 255, 255, 255);

	private readonly List<Bouton> _boutons = new List<Bouton>();

	private Rectangle _bandeau;

	private Disposition.Piece? _choisie;

	private Disposition.Piece? _prise;

	private Point _appui;

	private Point _ecartDePrise;

	private bool _bouge;

	private int? _guideX;

	private int? _guideY;

	private Point _souris;

	private readonly Quadrille _fond;

	public override bool IsBlockingInput => !base.IsHidden;

	protected override bool ShouldClip => false;

	public DispositionEditeur(Canvas canvas)
		: base(canvas, "DispositionEditeur")
	{
		base.MouseInputEnabled = true;
		base.IsHidden = true;
		_fond = new Quadrille(canvas);
		Interface.InputBlockingComponents.Add(this);
	}

	public void Ouvrir()
	{
		_choisie = null;
		_prise = null;
		Disposition.EnEdition = true;
		base.IsHidden = false;
		_fond.IsHidden = false;
		Update();
	}

	public void Fermer()
	{
		_prise = null;
		_guideX = (_guideY = null);
		Disposition.EnEdition = false;
		base.IsHidden = true;
		_fond.IsHidden = true;
		Disposition.Garder();
	}

	public void Update()
	{
		if (!base.IsHidden)
		{
			Intersect.Client.Framework.Gwen.Control.Base parent = base.Parent;
			if (parent != null && (base.Width != parent.Width || base.Height != parent.Height || base.X != 0 || base.Y != 0))
			{
				SetBounds(0, 0, parent.Width, parent.Height);
				_fond.SetBounds(0, 0, parent.Width, parent.Height);
			}
			_fond.SendToBack();
			BringToFront();
		}
	}

	private static string Nom(Disposition.Piece piece)
	{
		return piece switch
		{
			Disposition.Piece.VieEtKi => Strings.Disposition.VieEtKi, 
			Disposition.Piece.Cible => Strings.Disposition.Cible, 
			Disposition.Piece.Raccourcis => Strings.Disposition.Raccourcis, 
			Disposition.Piece.Menus => Strings.Disposition.Menus, 
			Disposition.Piece.Tchat => Strings.Disposition.Tchat, 
			Disposition.Piece.MiniCarte => Strings.Disposition.MiniCarte, 
			Disposition.Piece.Suivi => Strings.Disposition.Suivi, 
			Disposition.Piece.Police => Strings.Disposition.Police, 
			Disposition.Piece.Effets => Strings.Disposition.Effets, 
			_ => Strings.Disposition.Groupe, 
		};
	}

	private static string Quand(Disposition.Piece piece)
	{
		return piece switch
		{
			Disposition.Piece.Cible => Strings.Disposition.QuandCible, 
			Disposition.Piece.Suivi => Strings.Disposition.QuandSuivi, 
			Disposition.Piece.Police => Strings.Disposition.QuandPolice, 
			Disposition.Piece.Effets => Strings.Disposition.QuandEffets, 
			Disposition.Piece.Groupe => Strings.Disposition.QuandGroupe, 
			_ => string.Empty, 
		};
	}

	private Rectangle Bornes(Disposition.Piece piece, out bool presente)
	{
		Disposition.Vue vue = Disposition.VueDe(piece);
		if (Timing.Global.MillisecondsUtc - vue.VueA < 400 && vue.Bornes.Width > 0)
		{
			presente = vue.Presente;
			return vue.Bornes;
		}
		presente = false;
		Rectangle result = Origine(piece);
		Disposition.Reglage reglage = Disposition.De(piece);
		if (reglage != null && reglage.Deplacee)
		{
			Point point = Disposition.Lieu(reglage, result.Width, result.Height, base.Width, base.Height);
			return new Rectangle(point.X, point.Y, result.Width, result.Height);
		}
		return result;
	}

	private Rectangle Origine(Disposition.Piece piece)
	{
		bool presente;
		switch (piece)
		{
		case Disposition.Piece.Cible:
		{
			float num = Math.Max(Math.Min((float)base.Width / 1920f, (float)base.Height / 1080f), 0.6958333f) * Disposition.Taille(Disposition.Piece.Cible);
			int w = (int)MathF.Round(485f * num);
			return new Rectangle(Math.Max(0, (base.Width - (int)MathF.Round(480f * num)) / 2), (int)MathF.Round(14f * num), w, (int)MathF.Round(130f * num));
		}
		case Disposition.Piece.Police:
			return new Rectangle(12, 12, 262, 74);
		case Disposition.Piece.Groupe:
			return new Rectangle(4, 98, 210, 184);
		case Disposition.Piece.Effets:
		{
			Rectangle rectangle2 = Bornes(Disposition.Piece.VieEtKi, out presente);
			return new Rectangle(rectangle2.X, Math.Max(0, rectangle2.Y - 48), 289, 44);
		}
		case Disposition.Piece.Suivi:
		{
			Rectangle rectangle = Bornes(Disposition.Piece.MiniCarte, out presente);
			return new Rectangle(rectangle.Right - 268, rectangle.Bottom + 12, 268, 65);
		}
		case Disposition.Piece.VieEtKi:
			return new Rectangle((base.Width - 562) / 2, base.Height - 80 - 86, 562, 86);
		case Disposition.Piece.Raccourcis:
			return new Rectangle((base.Width - 562) / 2, base.Height - 80, 562, 70);
		case Disposition.Piece.Menus:
			return new Rectangle(base.Width - 470, base.Height - 80, 466, 70);
		case Disposition.Piece.Tchat:
			return new Rectangle(4, base.Height - 110, 560, 100);
		default:
			return new Rectangle(base.Width - 232 - 4, 4, 232, 320);
		}
	}

	private string NomDeLAncre(Rectangle bornes)
	{
		(float Fx, float Fy) tuple = Disposition.Ancre(bornes, base.Width, base.Height);
		float item = tuple.Fx;
		float item2 = tuple.Fy;
		LocalizedString value = ((item2 <= 0f) ? Strings.Disposition.Haut : ((item2 >= 1f) ? Strings.Disposition.Bas : Strings.Disposition.Milieu));
		LocalizedString value2 = ((item <= 0f) ? Strings.Disposition.Gauche : ((item >= 1f) ? Strings.Disposition.Droite : Strings.Disposition.Centre));
		return $"{value} {value2}";
	}

	protected override void Render(Intersect.Client.Framework.Gwen.Skin.Base skin)
	{
		base.Render(skin);
		Intersect.Client.Framework.Gwen.Renderer.Base renderer = skin.Renderer;
		_boutons.Clear();
		Disposition.Piece[] pieces = Disposition.Pieces;
		foreach (Disposition.Piece piece in pieces)
		{
			if (piece != _choisie && !Disposition.Masquee(piece))
			{
				DessinerPiece(renderer, piece, choisie: false);
			}
		}
		int? guideX = _guideX;
		if (guideX.HasValue)
		{
			int valueOrDefault = guideX.GetValueOrDefault();
			HudShapes.Fill(renderer, Math.Min(base.Width - 1, valueOrDefault), 0, 1, base.Height, Guide);
		}
		guideX = _guideY;
		if (guideX.HasValue)
		{
			int valueOrDefault2 = guideX.GetValueOrDefault();
			HudShapes.Fill(renderer, 0, Math.Min(base.Height - 1, valueOrDefault2), base.Width, 1, Guide);
		}
		DessinerBandeau(renderer);
		Disposition.Piece? choisie = _choisie;
		if (choisie.HasValue)
		{
			Disposition.Piece valueOrDefault3 = choisie.GetValueOrDefault();
			if (!Disposition.Masquee(valueOrDefault3))
			{
				DessinerPiece(renderer, valueOrDefault3, choisie: true);
			}
		}
	}

	private void DessinerPiece(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Disposition.Piece piece, bool choisie)
	{
		bool presente;
		Rectangle rectangle = Bornes(piece, out presente);
		string text = Nom(piece).ToUpperInvariant();
		if (!presente)
		{
			TechniqueArt.Fill(renderer, rectangle, Fantome);
			Tirets(renderer, rectangle, Pointilles);
			int num = rectangle.Y + rectangle.Height / 2;
			string text2 = Quand(piece);
			bool flag = text2.Length > 0 && rectangle.Height >= 44 && SaiyanHud.Measure(renderer, TechniqueArt.Label, 10, text2).X + 12 <= rectangle.Width;
			int num2 = TechniqueArt.SpacedWidth(renderer, SaiyanHud.NameFont, 12, text, 1.6f);
			TechniqueArt.Spaced(renderer, SaiyanHud.NameFont, 12, text, rectangle.X + (rectangle.Width - num2) / 2, flag ? (num - 8) : num, 1.6f, Texte);
			if (flag)
			{
				SaiyanHud.TextCenter(renderer, TechniqueArt.Label, 10, text2, rectangle.X, num + 2, rectangle.Width, 18, Doux, shadow: false);
			}
		}
		if (choisie)
		{
			Cadre(renderer, Gonfler(rectangle, 4), 2, Blanc);
			DessinerOutils(renderer, piece, rectangle, text);
			return;
		}
		Cadre(renderer, Gonfler(rectangle, 3), 1, Contour);
		if (presente)
		{
			int num3 = TechniqueArt.SpacedWidth(renderer, SaiyanHud.NameFont, 10, text, 1f);
			TechniqueArt.Fill(renderer, new Rectangle(rectangle.X, rectangle.Y, num3 + 14, 20), Etiquette);
			TechniqueArt.Spaced(renderer, SaiyanHud.NameFont, 10, text, rectangle.X + 7, rectangle.Y + 10, 1f, Encre);
		}
	}

	private void DessinerOutils(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Disposition.Piece piece, Rectangle bornes, string nom)
	{
		int num = (int)MathF.Round(Disposition.Taille(piece) * 100f);
		string text = Strings.Disposition.Fiche.ToString(nom, num, NomDeLAncre(bornes)).ToUpperInvariant();
		List<(string, Action)> list = new List<(string, Action)>();
		if (Disposition.Redimensionnable(piece))
		{
			list.Add(("–", delegate
			{
				Tailler(piece, -0.05f);
			}));
			list.Add(("+", delegate
			{
				Tailler(piece, 0.05f);
			}));
		}
		list.Add((Strings.Disposition.SaPlace.ToString().ToUpperInvariant(), delegate
		{
			Disposition.Remettre(piece);
			Disposition.Garder();
		}));
		list.Add((Strings.Disposition.Masquer.ToString().ToUpperInvariant(), delegate
		{
			Disposition.Masquer(piece, masquee: true);
			Disposition.Garder();
			_choisie = null;
		}));
		int num2 = TechniqueArt.SpacedWidth(renderer, SaiyanHud.NameFont, 11, text, 1.1f) + 18;
		List<int> list2 = list.Select<(string, Action), int>(((string Texte, Action Geste) outil) => Math.Max(24, TechniqueArt.SpacedWidth(renderer, SaiyanHud.NameFont, 11, outil.Texte, 0.8f) + 14)).ToList();
		int num3 = num2 + list2.Sum() + 2 * list.Count;
		int value = ((bornes.X + bornes.Width / 2 > base.Width / 2) ? (bornes.Right + 4 - num3) : (bornes.X - 4));
		value = Math.Clamp(value, 2, Math.Max(2, base.Width - num3 - 2));
		int value2 = ((bornes.Y < 35) ? (bornes.Bottom + 10) : (bornes.Y - 10 - 23));
		value2 = Math.Clamp(value2, 2, Math.Max(2, base.Height - 23 - 2));
		TechniqueArt.Fill(renderer, new Rectangle(value, value2, num2, 23), Blanc);
		TechniqueArt.Spaced(renderer, SaiyanHud.NameFont, 11, text, value + 9, value2 + 11, 1.1f, Encre);
		value += num2 + 2;
		for (int num4 = 0; num4 < list.Count; num4++)
		{
			Rectangle zone = new Rectangle(value, value2, list2[num4], 23);
			DessinerBouton(renderer, zone, list[num4].Item1, 11, plein: false);
			_boutons.Add(new Bouton(zone, list[num4].Item2));
			value += list2[num4] + 2;
		}
	}

	private void DessinerBandeau(Intersect.Client.Framework.Gwen.Renderer.Base renderer)
	{
		string text = Strings.Disposition.Titre.ToString().ToUpperInvariant();
		string text2 = Strings.Disposition.Aide.ToString();
		string text3 = Strings.Disposition.PiecesAffichees.ToString().ToUpperInvariant();
		string text4 = Strings.Disposition.ToutRemettre.ToString().ToUpperInvariant();
		string text5 = Strings.Disposition.Terminer.ToString().ToUpperInvariant();
		int num = Math.Min(base.Width - 60, 780);
		List<(Disposition.Piece, string, int)> list = Disposition.Pieces.Select((Disposition.Piece piece2) => (Piece: piece2, Nom: Nom(piece2), Largeur: SaiyanHud.Measure(renderer, TechniqueArt.Label, 10, Nom(piece2)).X + 20)).ToList();
		List<List<(Disposition.Piece, string, int)>> list2 = new List<List<(Disposition.Piece, string, int)>>
		{
			new List<(Disposition.Piece, string, int)>()
		};
		int num2 = 0;
		foreach (var item in list)
		{
			if (num2 > 0 && num2 + item.Item3 > num)
			{
				list2.Add(new List<(Disposition.Piece, string, int)>());
				num2 = 0;
			}
			list2[list2.Count - 1].Add(item);
			num2 += item.Item3 + 6;
		}
		int num3 = SaiyanHud.Measure(renderer, SaiyanHud.NameFont, 18, text).X + 10;
		int x = SaiyanHud.Measure(renderer, TechniqueArt.Label, 10, text2).X;
		int val = list2.Max((List<(Disposition.Piece Piece, string Nom, int Largeur)> ligne) => ligne.Sum(((Disposition.Piece Piece, string Nom, int Largeur) puce) => puce.Largeur + 6) - 6);
		int num4 = TechniqueArt.SpacedWidth(renderer, SaiyanHud.NameFont, 11, text4, 1.1f) + 28;
		int num5 = TechniqueArt.SpacedWidth(renderer, SaiyanHud.NameFont, 11, text5, 1.1f) + 28;
		int num6 = Math.Max(Math.Max(num3, x), Math.Max(val, num4 + 8 + num5));
		int num7 = num6 + 36;
		int num8 = 90 + list2.Count * 30 + 8 + 30 + 14;
		int num9 = (base.Width - num7) / 2;
		int num10 = Math.Clamp(190, 4, Math.Max(4, base.Height - num8 - 4));
		_bandeau = new Rectangle(num9, num10, num7, num8);
		TechniqueArt.Fill(renderer, _bandeau, Bandeau);
		Cadre(renderer, _bandeau, 1, Bord);
		HudShapes.Fill(renderer, num9, num10, num7, 2, Orange);
		int num11 = num10 + 12;
		TechniqueArt.Styled(renderer, SaiyanHud.NameFont, 18, text, num9 + (num7 - num3) / 2, num11, TechniqueArt.Stops.Solid(Texte));
		num11 += 28;
		SaiyanHud.TextCenter(renderer, TechniqueArt.Label, 10, text2, num9, num11, num7, 20, Doux, shadow: false);
		num11 += 34;
		int num12 = TechniqueArt.SpacedWidth(renderer, TechniqueArt.Label, 8, text3, 1.6f);
		TechniqueArt.Spaced(renderer, TechniqueArt.Label, 8, text3, num9 + (num7 - num12) / 2, num11 + 6, 1.6f, Doux);
		num11 += 16;
		foreach (List<(Disposition.Piece, string, int)> item2 in list2)
		{
			int num13 = item2.Sum<(Disposition.Piece, string, int)>(((Disposition.Piece Piece, string Nom, int Largeur) puce) => puce.Largeur + 6) - 6;
			int num14 = num9 + (num7 - num13) / 2;
			foreach (var item3 in item2)
			{
				Rectangle rectangle = new Rectangle(num14, num11, item3.Item3, 24);
				bool affichee = !Disposition.Masquee(item3.Item1);
				bool flag = rectangle.Contains(_souris.X, _souris.Y);
				if (affichee)
				{
					TechniqueArt.Fill(renderer, rectangle, FondReleve);
				}
				Cadre(renderer, rectangle, 1, flag ? Blanc : (affichee ? Bord : BordEteint));
				SaiyanHud.TextCenter(renderer, TechniqueArt.Label, 10, item3.Item2, rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height, affichee ? Texte : Doux, shadow: false);
				var (piece, _, _) = item3;
				_boutons.Add(new Bouton(rectangle, delegate
				{
					Disposition.Masquer(piece, affichee);
					Disposition.Garder();
					if (affichee && _choisie == piece)
					{
						_choisie = null;
					}
				}));
				num14 += item3.Item3 + 6;
			}
			num11 += 30;
		}
		num11 += 8;
		int num15 = num9 + (num7 - (num4 + 8 + num5)) / 2;
		Rectangle zone = new Rectangle(num15, num11, num4, 30);
		DessinerBouton(renderer, zone, text4, 11, plein: false);
		_boutons.Add(new Bouton(zone, delegate
		{
			Disposition.ToutRemettre();
			Disposition.Garder();
			_choisie = null;
		}));
		Rectangle zone2 = new Rectangle(num15 + num4 + 8, num11, num5, 30);
		DessinerBouton(renderer, zone2, text5, 11, plein: true);
		_boutons.Add(new Bouton(zone2, Fermer));
	}

	private void DessinerBouton(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Rectangle zone, string texte, int corps, bool plein)
	{
		bool flag = zone.Contains(_souris.X, _souris.Y);
		TechniqueArt.Fill(renderer, zone, plein ? Orange : Fond);
		Cadre(renderer, zone, 1, flag ? Blanc : (plein ? Orange : Bord));
		int num = TechniqueArt.SpacedWidth(renderer, SaiyanHud.NameFont, corps, texte, 0.8f);
		TechniqueArt.Spaced(renderer, SaiyanHud.NameFont, corps, texte, zone.X + (zone.Width - num) / 2, zone.Y + zone.Height / 2, 0.8f, plein ? Encre : Texte);
	}

	private static Rectangle Gonfler(Rectangle bornes, int marge)
	{
		return new Rectangle(bornes.X - marge, bornes.Y - marge, bornes.Width + 2 * marge, bornes.Height + 2 * marge);
	}

	private static void Cadre(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Rectangle zone, int epaisseur, Color couleur)
	{
		HudShapes.Fill(renderer, zone.X, zone.Y, zone.Width, epaisseur, couleur);
		HudShapes.Fill(renderer, zone.X, zone.Bottom - epaisseur, zone.Width, epaisseur, couleur);
		HudShapes.Fill(renderer, zone.X, zone.Y + epaisseur, epaisseur, zone.Height - 2 * epaisseur, couleur);
		HudShapes.Fill(renderer, zone.Right - epaisseur, zone.Y + epaisseur, epaisseur, zone.Height - 2 * epaisseur, couleur);
	}

	private static void Tirets(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Rectangle zone, Color couleur)
	{
		for (int i = zone.X; i < zone.Right; i += 8)
		{
			int width = Math.Min(4, zone.Right - i);
			HudShapes.Fill(renderer, i, zone.Y, width, 1, couleur);
			HudShapes.Fill(renderer, i, zone.Bottom - 1, width, 1, couleur);
		}
		for (int j = zone.Y; j < zone.Bottom; j += 8)
		{
			int height = Math.Min(4, zone.Bottom - j);
			HudShapes.Fill(renderer, zone.X, j, 1, height, couleur);
			HudShapes.Fill(renderer, zone.Right - 1, j, 1, height, couleur);
		}
	}

	private void Tailler(Disposition.Piece piece, float pas)
	{
		Disposition.Tailler(piece, Disposition.Taille(piece) + pas);
		Disposition.Garder();
	}

	private Disposition.Piece? PieceSous(Point point)
	{
		Disposition.Piece? result = null;
		long num = long.MaxValue;
		Disposition.Piece[] pieces = Disposition.Pieces;
		foreach (Disposition.Piece piece in pieces)
		{
			if (!Disposition.Masquee(piece))
			{
				bool presente;
				Rectangle rectangle = Bornes(piece, out presente);
				if (rectangle.Contains(point.X, point.Y) && (long)rectangle.Width * (long)rectangle.Height < num)
				{
					result = piece;
					num = (long)rectangle.Width * (long)rectangle.Height;
				}
			}
		}
		return result;
	}

	protected override void OnMouseDown(MouseButton mouseButton, Point mousePosition, bool userAction = true)
	{
		base.OnMouseDown(mouseButton, mousePosition, userAction);
		if (mouseButton != MouseButton.Left)
		{
			return;
		}
		Point point = (_souris = CanvasPosToLocal(mousePosition));
		for (int num = _boutons.Count - 1; num >= 0; num--)
		{
			if (_boutons[num].Zone.Contains(point.X, point.Y))
			{
				_boutons[num].Geste();
				return;
			}
		}
		if (!_bandeau.Contains(point.X, point.Y))
		{
			_choisie = PieceSous(point);
			_prise = _choisie;
			_bouge = false;
			Disposition.Piece? choisie = _choisie;
			if (choisie.HasValue)
			{
				Disposition.Piece valueOrDefault = choisie.GetValueOrDefault();
				bool presente;
				Rectangle rectangle = Bornes(valueOrDefault, out presente);
				_appui = point;
				_ecartDePrise = new Point(point.X - rectangle.X, point.Y - rectangle.Y);
			}
		}
	}

	protected override void OnMouseMoved(int x, int y, int dx, int dy)
	{
		base.OnMouseMoved(x, y, dx, dy);
		_souris = CanvasPosToLocal(InputHandler.MousePosition);
		Disposition.Piece? prise = _prise;
		if (prise.HasValue)
		{
			Disposition.Piece valueOrDefault = prise.GetValueOrDefault();
			if (_bouge || Math.Abs(_souris.X - _appui.X) + Math.Abs(_souris.Y - _appui.Y) >= 4)
			{
				_bouge = true;
				bool presente;
				Rectangle rectangle = Bornes(valueOrDefault, out presente);
				Point point = Aimanter(valueOrDefault, _souris.X - _ecartDePrise.X, _souris.Y - _ecartDePrise.Y, rectangle.Width, rectangle.Height);
				Disposition.Deplacer(valueOrDefault, new Rectangle(point.X, point.Y, rectangle.Width, rectangle.Height), base.Width, base.Height);
			}
		}
	}

	protected override void OnMouseUp(MouseButton mouseButton, Point mousePosition, bool userAction = true)
	{
		base.OnMouseUp(mouseButton, mousePosition, userAction);
		if (mouseButton == MouseButton.Left)
		{
			if (_prise.HasValue && _bouge)
			{
				Disposition.Garder();
			}
			_prise = null;
			_bouge = false;
			_guideX = (_guideY = null);
		}
	}

	private Point Aimanter(Disposition.Piece piece, int x, int y, int largeur, int hauteur)
	{
		List<int> list = new List<int>
		{
			0,
			8,
			base.Width / 2,
			base.Width - 8,
			base.Width
		};
		List<int> list2 = new List<int>
		{
			0,
			10,
			base.Height / 2,
			base.Height - 10,
			base.Height
		};
		Disposition.Piece[] pieces = Disposition.Pieces;
		foreach (Disposition.Piece piece2 in pieces)
		{
			if (piece2 != piece && !Disposition.Masquee(piece2))
			{
				bool presente;
				Rectangle rectangle = Bornes(piece2, out presente);
				list.Add(rectangle.X);
				list.Add(rectangle.X + rectangle.Width / 2);
				list.Add(rectangle.Right);
				list2.Add(rectangle.Y);
				list2.Add(rectangle.Y + rectangle.Height / 2);
				list2.Add(rectangle.Bottom);
			}
		}
		(int, int)? tuple = Meilleur(list, x, largeur);
		(int, int)? tuple2 = Meilleur(list2, y, hauteur);
		x += tuple?.Item1 ?? 0;
		y += tuple2?.Item1 ?? 0;
		_guideX = tuple?.Item2;
		_guideY = tuple2?.Item2;
		return new Point(Math.Clamp(x, 0, Math.Max(0, base.Width - largeur)), Math.Clamp(y, 0, Math.Max(0, base.Height - hauteur)));
		static (int Ecart, int Ligne)? Meilleur(List<int> lignes, int debut, int taille)
		{
			(int, int)? result = null;
			foreach (int ligne in lignes)
			{
				int[] array = new int[3]
				{
					debut,
					debut + taille / 2,
					debut + taille
				};
				foreach (int num in array)
				{
					int num2 = ligne - num;
					if (Math.Abs(num2) <= 7 && (!result.HasValue || Math.Abs(num2) < Math.Abs(result.Value.Item1)))
					{
						result = (num2, ligne);
					}
				}
			}
			return result;
		}
	}
}
