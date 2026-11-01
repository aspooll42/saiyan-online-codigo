using System;
using System.Collections.Generic;
using Intersect.Client.Core;
using Intersect.Client.Core.Controls;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.Framework.Gwen.Input;
using Intersect.Client.Framework.Gwen.Renderer;
using Intersect.Client.Framework.Gwen.Skin;
using Intersect.Client.Framework.Input;
using Intersect.Client.Interface.Game.EntityPanel;
using Intersect.Client.Interface.Game.Hotbar;
using Intersect.Framework.Core;

namespace Intersect.Client.Interface.Game.Techniques;

internal abstract class FenetreCharte : Intersect.Client.Framework.Gwen.Control.Base
{
	protected readonly record struct LigneBulle(string Nom, string Valeur, long Ecart, string? TexteEcart);

	protected sealed class Bulle
	{
		public IGameTexture? Icone;

		public Color Teinte = TechniqueArt.White;

		public string Nom = string.Empty;

		public Color Couleur = TechniqueArt.Line2;

		public Color CouleurNom = TechniqueArt.Text;

		public string Sorte = string.Empty;

		public (string Texte, Color Couleur)? Exigence;

		public string? Texte;

		public readonly List<LigneBulle> Lignes = new List<LigneBulle>();

		public bool Ecarts;

		public string? Jauge;

		public float[]? Crans;

		public string? Compte;

		public bool JaugePleine;

		public string? Annonce;

		public readonly List<(string Texte, Color Couleur)> Paliers = new List<(string, Color)>();

		public readonly List<(string Texte, Color Couleur)> Verdict = new List<(string, Color)>();

		public string? Description;

		public readonly List<string> Notes = new List<string>();

		public readonly List<string> Aides = new List<string>();
	}

	private readonly record struct Zone(Rectangle Rect, string Cle, Action? Action, string? Son = null);

	private readonly record struct Lisse(IGameTexture Texture, Rectangle Rect, Color Couleur, float U1, float V1, float U2, float V2);

	protected readonly record struct Puce(string Cle, string Nom, string Compte, bool Active, Action Choisir, Action<Rectangle, Color> Glyphe, Color? Teinte = null);

	private static readonly float[] Espacements = new float[3] { 0.14f, 0.06f, 0f };

	private List<Zone> _zones = new List<Zone>();

	private List<Zone> _zonesEnCours = new List<Zone>();

	private readonly List<Lisse> _lisses = new List<Lisse>();

	private string? _cleChaude;

	private long _ouverteA;

	protected CharteScreen.Design _d = new CharteScreen.Design(1f, 0f, 0f);

	protected float _fade = 1f;

	private static Control? _touchePosee;

	private Point _decalage;

	private Point? _prise;

	private bool _deplacee;

	private static readonly Color JaugeEteinte = new Color(255, 34, 43, 67);

	private const long DelaiEtiquette = 350L;

	private string? _etiquetteCle;

	private string _etiquetteTexte = string.Empty;

	private Rectangle _etiquetteRect;

	private long _etiquetteDepuis;

	private bool _etiquetteVue;

	protected abstract Rectangle Place { get; }

	public virtual bool TientUnObjet => false;

	public virtual bool ObjetEnVol => false;

	public virtual Color CouleurEnVol => TechniqueArt.Soft;

	protected virtual bool PrendLesRaccourcis => false;

	public static bool RetientLesRaccourcis
	{
		get
		{
			Control? touchePosee = _touchePosee;
			if (touchePosee.HasValue)
			{
				Control valueOrDefault = touchePosee.GetValueOrDefault();
				if (Controls.IsControlPressed(valueOrDefault))
				{
					return true;
				}
				_touchePosee = null;
			}
			Intersect.Client.Framework.Gwen.Control.Base hoveredControl = InputHandler.HoveredControl;
			if (hoveredControl is FenetreCharte fenetreCharte && !hoveredControl.IsHidden)
			{
				return fenetreCharte.PrendLesRaccourcis;
			}
			return false;
		}
	}

	protected virtual string SonOuverture => "menu_open.wav";

	protected virtual int HauteurBarre => 45;

	public bool EnDeplacement => _prise.HasValue;

	protected bool Survolee
	{
		get
		{
			if (IsHovered)
			{
				return !SurvolSuspendu;
			}
			return false;
		}
	}

	protected virtual bool SurvolSuspendu => false;

	protected void DessinerBulle(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Bulle bulle, Rectangle cible)
	{
		int num = _d.L(268f);
		int num2 = PoserBulle(renderer, bulle, 0, 0, dessiner: false);
		int num3 = base.Parent?.Width ?? base.Width;
		int num4 = base.Parent?.Height ?? base.Height;
		int num5 = -num - _d.L(10f);
		int num6 = Math.Max(8 - base.Y, Math.Min(num4 - base.Y - num2 - 8, cible.Y - _d.L(8f)));
		if (base.X + num5 < 4)
		{
			num5 = Math.Max(8 - base.X, Math.Min(num3 - base.X - num - 8, cible.X + cible.Width / 2 - num / 2));
			num6 = cible.Bottom + _d.L(8f);
			if (base.Y + num6 + num2 > num4 - 4)
			{
				num6 = Math.Max(4 - base.Y, cible.Y - num2 - _d.L(8f));
			}
		}
		Rectangle rectangle = new Rectangle(num5, num6, num, num2);
		Ombre(renderer, rectangle, _d.L(14f), _d.L(6f), 0.34f);
		TechniqueArt.Fill(renderer, rectangle, A(new Color(250, 8, 11, 19)));
		TechniqueArt.Outline(renderer, rectangle, A(TechniqueArt.Line2));
		TechniqueArt.Fill(renderer, new Rectangle(num5, num6, _d.L(3f), num2), A(bulle.Couleur));
		PoserBulle(renderer, bulle, num5, num6, dessiner: true);
	}

	private int TailleQuiTient(Intersect.Client.Framework.Gwen.Renderer.Base renderer, IFont? police, string texte, int largeur, params float[] tailles)
	{
		foreach (float cssPixels in tailles)
		{
			if (TechniqueArt.Measure(renderer, police, Pt(cssPixels), texte).X <= largeur)
			{
				return Pt(cssPixels);
			}
		}
		return Pt(tailles[^1]);
	}

	private int PoserBulle(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Bulle bulle, int x, int y, bool dessiner)
	{
		int num = x + _d.L(3f) + _d.L(15f);
		int num2 = x + _d.L(268f) - 1 - _d.L(14f);
		int num3 = num2 - num;
		int num4 = y + 1 + _d.L(12f);
		int num5 = _d.L(40f);
		int num6 = ((bulle.Icone != null) ? (num + num5 + _d.L(10f)) : num);
		List<string> list = TechniqueArt.Wrap(renderer, TechniqueArt.Heavy, Pt(19f), bulle.Nom, num2 - num6, 2);
		int num7 = _d.L(20.5f);
		int num8 = _d.L(16.5f);
		int num9 = list.Count * num7 + _d.L(3f) + num8;
		int num10 = ((bulle.Icone != null) ? Math.Max(num5, num9) : num9);
		if (dessiner)
		{
			IGameTexture icone = bulle.Icone;
			if (icone != null)
			{
				Rectangle rect = new Rectangle(num, num4 + (num10 - num5) / 2, num5, num5);
				TechniqueArt.Fill(renderer, rect, A(new Color(255, 7, 10, 18)));
				TechniqueArt.Outline(renderer, rect, A(TechniqueArt.Line));
				int num11 = TailleIcone(icone.Width);
				SaiyanHud.Draw(renderer, icone, rect.X + (num5 - num11) / 2, rect.Y + (num5 - num11) / 2, num11, num11, A(bulle.Teinte));
			}
			int num12 = num4 + (num10 - num9) / 2;
			foreach (string item3 in list)
			{
				TechniqueArt.TextMiddle(renderer, TechniqueArt.Heavy, Pt(19f), item3, num6, num12 + num7 / 2, A(bulle.CouleurNom));
				num12 += num7;
			}
			Capitales(renderer, bulle.Sorte, num6, num12 + _d.L(3f) + num8 / 2, num2 - num6, A(TechniqueArt.Soft));
		}
		num4 += num10;
		(string, Color)? exigence = bulle.Exigence;
		if (exigence.HasValue)
		{
			(string, Color) valueOrDefault = exigence.GetValueOrDefault();
			num4 += _d.L(8f);
			num4 += Paragraphe(renderer, valueOrDefault.Item1, TechniqueArt.Label, Pt(13f), num, num4, num3, _d.L(19.5f), 2, A(valueOrDefault.Item2), dessiner);
		}
		string texte = bulle.Texte;
		if (texte != null && texte.Length > 0)
		{
			num4 += _d.L(9f);
			num4 += Paragraphe(renderer, texte, TechniqueArt.Body, Pt(13f), num, num4, num3, _d.L(19f), 6, A(new Color(255, 195, 203, 220)), dessiner);
		}
		if (bulle.Lignes.Count > 0)
		{
			num4 += _d.L(10f);
			int num13 = _d.L(20f);
			int num14 = _d.L(3f);
			foreach (LigneBulle ligne in bulle.Lignes)
			{
				if (dessiner)
				{
					int num15 = num4 + num13 / 2;
					int num16 = (bulle.Ecarts ? (num2 - _d.L(52f)) : num2);
					int num17 = TechniqueArt.TextRight(renderer, TechniqueArt.Heavy, Pt(17f), ligne.Valeur, num16, num15, A(TechniqueArt.Text));
					int size = TailleQuiTient(renderer, TechniqueArt.Body, ligne.Nom, num16 - num17 - _d.L(10f) - num, 13.5f, 12.5f, 11.5f);
					TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, size, ligne.Nom, num, num15, A(TechniqueArt.Soft));
					if (ligne.TexteEcart != null)
					{
						Color color = A((ligne.Ecart > 0) ? TechniqueArt.Green : TechniqueArt.Red);
						int num18 = TechniqueArt.TextRight(renderer, TechniqueArt.Heavy, Pt(14f), ligne.TexteEcart, num2, num15, color);
						Triangle(renderer, num2 - num18 - _d.L(4f) - _d.L(8f), num15, _d.L(8f), _d.L(6f), ligne.Ecart > 0, color);
					}
				}
				num4 += num13 + num14;
			}
			num4 -= num14;
		}
		if (bulle.Jauge != null)
		{
			float[] crans = bulle.Crans;
			if (crans != null && crans.Length > 0)
			{
				num4 += _d.L(9f);
				if (dessiner)
				{
					TechniqueArt.Fill(renderer, new Rectangle(num, num4, num3, 1), A(TechniqueArt.White, 0.07f));
				}
				num4 += 1 + _d.L(8f);
				int num19 = _d.L(20f);
				if (dessiner)
				{
					int num20 = num4 + num19 / 2;
					TechniqueArt.TextMiddle(renderer, bulle.JaugePleine ? TechniqueArt.Label : TechniqueArt.Body, Pt(13.5f), bulle.Jauge, num, num20, A(bulle.JaugePleine ? TechniqueArt.Gold : TechniqueArt.Soft));
					int num21 = num2;
					if (bulle.Compte != null)
					{
						num21 -= TechniqueArt.TextRight(renderer, TechniqueArt.Heavy, Pt(15f), bulle.Compte, num2, num20, A(TechniqueArt.Text)) + _d.L(8f);
					}
					int largeurCran = _d.L(bulle.JaugePleine ? 16 : 20);
					int num22 = Math.Max(3, _d.L(5f));
					Jauge(renderer, num21 - LargeurJauge(largeurCran, crans.Length), num20 - num22 / 2, largeurCran, num22, crans, bulle.Couleur);
				}
				num4 += num19;
			}
		}
		if (bulle.Annonce != null || bulle.Paliers.Count > 0)
		{
			num4 += _d.L(8f);
			if (bulle.Annonce != null)
			{
				int num23 = _d.L(19.5f);
				if (dessiner)
				{
					List<string> list2 = TechniqueArt.Wrap(renderer, TechniqueArt.Label, Pt(13f), bulle.Annonce, num3, 1);
					TechniqueArt.TextMiddle(renderer, TechniqueArt.Label, Pt(13f), (list2.Count > 0) ? list2[0] : bulle.Annonce, num, num4 + num23 / 2, A(TechniqueArt.Gold));
				}
				num4 += num23;
			}
			foreach (var palier in bulle.Paliers)
			{
				string item = palier.Texte;
				Color item2 = palier.Couleur;
				num4 += Paragraphe(renderer, item, TechniqueArt.Body, Pt(12f), num, num4, num3, _d.L(18f), 3, A(item2), dessiner);
			}
		}
		if (bulle.Verdict.Count > 0)
		{
			num4 += _d.L(9f);
			if (dessiner)
			{
				TechniqueArt.Fill(renderer, new Rectangle(num, num4, num3, 1), A(TechniqueArt.White, 0.07f));
			}
			num4 += 1 + _d.L(8f);
			num4 += TexteColore(renderer, bulle.Verdict, TechniqueArt.Label, Pt(13f), num, num4, num3, _d.L(19.5f), dessiner);
		}
		if (bulle.Description != null || bulle.Notes.Count > 0)
		{
			num4 += _d.L(9f);
			if (dessiner)
			{
				TechniqueArt.Fill(renderer, new Rectangle(num, num4, num3, 1), A(TechniqueArt.White, 0.07f));
			}
			num4 += 1 + _d.L(8f);
			bool flag = true;
			if (bulle.Description != null)
			{
				int num24 = _d.L(17.5f);
				int y2 = TechniqueArt.Measure(renderer, TechniqueArt.Body, Pt(12.5f), "Hg").Y;
				foreach (string item4 in TechniqueArt.Wrap(renderer, TechniqueArt.Body, Pt(12.5f), bulle.Description, num3, 8))
				{
					if (dessiner)
					{
						TechniqueArt.Italic(renderer, TechniqueArt.Body, Pt(12.5f), item4, num, num4 + (num24 - y2) / 2, A(new Color(255, 195, 203, 220)));
					}
					num4 += num24;
				}
				flag = false;
			}
			foreach (string note in bulle.Notes)
			{
				if (!flag)
				{
					num4 += _d.L(6f);
				}
				flag = false;
				num4 += Paragraphe(renderer, note, TechniqueArt.Body, Pt(12.5f), num, num4, num3, _d.L(17.5f), 2, A(TechniqueArt.Soft), dessiner);
			}
		}
		for (int i = 0; i < bulle.Aides.Count; i++)
		{
			num4 += ((i == 0) ? _d.L(9f) : _d.L(1f));
			num4 += Paragraphe(renderer, bulle.Aides[i], TechniqueArt.Body, Pt(12f), num, num4, num3, _d.L(18f), 3, A(TechniqueArt.Dim), dessiner);
		}
		return num4 + _d.L(12f) + 1 - y;
	}

	protected static int Paragraphe(Intersect.Client.Framework.Gwen.Renderer.Base renderer, string texte, IFont? police, int taille, int x, int y, int largeur, int interligne, int maximum, Color couleur, bool dessiner)
	{
		List<string> list = TechniqueArt.Wrap(renderer, police, taille, texte, largeur, maximum);
		if (dessiner)
		{
			for (int i = 0; i < list.Count; i++)
			{
				TechniqueArt.TextMiddle(renderer, police, taille, list[i], x, y + i * interligne + interligne / 2, couleur);
			}
		}
		return list.Count * interligne;
	}

	protected int TexteColore(Intersect.Client.Framework.Gwen.Renderer.Base renderer, List<(string Texte, Color Couleur)> morceaux, IFont? police, int taille, int x, int y, int largeur, int interligne, bool dessiner)
	{
		int espace = Math.Max(1, TechniqueArt.Measure(renderer, police, taille, "a a").X - TechniqueArt.Measure(renderer, police, taille, "aa").X);
		int hauteurTexte = TechniqueArt.Measure(renderer, police, taille, "Hg").Y;
		List<(string Mot, Color Couleur, int Largeur)> grappe = new List<(string, Color, int)>();
		int largeurGrappe = 0;
		int avance = 0;
		int ligne = 0;
		bool ecrit = false;
		bool espaceAvant = false;
		foreach (var item3 in morceaux)
		{
			string item = item3.Texte;
			Color item2 = item3.Couleur;
			int num = 0;
			for (int i = 0; i <= item.Length; i++)
			{
				bool flag = i == item.Length;
				if (flag || item[i] == ' ')
				{
					if (i > num)
					{
						int num2 = num;
						string text = item.Substring(num2, i - num2);
						int x2 = TechniqueArt.Measure(renderer, police, taille, text).X;
						grappe.Add((text, item2, x2));
						largeurGrappe += x2;
					}
					if (!flag)
					{
						Poser(espaceAvant);
						espaceAvant = true;
					}
					num = i + 1;
				}
			}
		}
		Poser(espaceAvant);
		if (!ecrit)
		{
			return 0;
		}
		return (ligne + 1) * interligne;
		void Poser(bool flag2)
		{
			if (grappe.Count != 0)
			{
				int num3 = ((flag2 && avance > 0) ? espace : 0);
				if (avance > 0 && avance + num3 + largeurGrappe > largeur)
				{
					ligne++;
					avance = 0;
					num3 = 0;
				}
				avance += num3;
				foreach (var (text2, color, num4) in grappe)
				{
					if (dessiner)
					{
						TechniqueArt.Write(renderer, police, taille, text2, x + avance, y + ligne * interligne + (interligne - hauteurTexte) / 2, A(color));
					}
					avance += num4;
				}
				ecrit = true;
				grappe.Clear();
				largeurGrappe = 0;
			}
		}
	}

	protected void Capitales(Intersect.Client.Framework.Gwen.Renderer.Base renderer, string texte, int x, int centre, int largeur, Color couleur)
	{
		string text = texte.ToUpperInvariant();
		float[] espacements = Espacements;
		foreach (float em in espacements)
		{
			if (TechniqueArt.SpacedWidth(renderer, TechniqueArt.Label, Pt(11f), text, _d.Em(em, 11f)) <= largeur)
			{
				TechniqueArt.Spaced(renderer, TechniqueArt.Label, Pt(11f), text, x, centre, _d.Em(em, 11f), couleur);
				return;
			}
		}
		List<string> list = TechniqueArt.Wrap(renderer, TechniqueArt.Label, Pt(11f), text, largeur, 1);
		TechniqueArt.TextMiddle(renderer, TechniqueArt.Label, Pt(11f), (list.Count > 0) ? list[0] : text, x, centre, couleur);
	}

	protected FenetreCharte(Canvas gameCanvas, string name)
		: base(gameCanvas, name)
	{
		base.MouseInputEnabled = true;
		base.IsHidden = true;
	}

	protected static int? RaccourciSousLeCurseur()
	{
		for (Intersect.Client.Framework.Gwen.Control.Base obj = Interface.FindComponentUnderCursor(); obj != null; obj = obj.Parent)
		{
			if (obj is HotbarItem hotbarItem)
			{
				return hotbarItem.SlotIndex;
			}
		}
		return null;
	}

	public bool IsVisible()
	{
		return !base.IsHidden;
	}

	internal virtual bool HandleKey(Keys modifier, Keys key)
	{
		return false;
	}

	protected static void RetenirLaTouche(Control touche)
	{
		_touchePosee = touche;
	}

	public override void Show()
	{
		if (base.IsHidden)
		{
			_decalage = default(Point);
		}
		_prise = null;
		_deplacee = false;
		Placer();
		_ouverteA = Timing.Global.MillisecondsUtc;
		base.Show();
		BringToFront();
		Audio.AddGameSound(SonOuverture, loop: false);
	}

	protected virtual void Fermer()
	{
		Hide();
	}

	protected void Placer()
	{
		Intersect.Client.Framework.Gwen.Control.Base parent = base.Parent;
		if (parent != null)
		{
			CharteScreen.Design design = CharteScreen.Design.For(parent.Width, parent.Height);
			Rectangle place = Place;
			_d = new CharteScreen.Design(design.S, 0f, 0f);
			int num = _d.L(place.Width);
			int num2 = _d.L(place.Height);
			var (num3, num4) = Borner(parent, (parent.Width - num) / 2 + _decalage.X, (parent.Height - num2) / 2 + _decalage.Y, num, num2);
			if (base.X != num3 || base.Y != num4 || base.Width != num || base.Height != num2)
			{
				SetBounds(num3, num4, num, num2);
			}
		}
	}

	private static (int X, int Y) Borner(Intersect.Client.Framework.Gwen.Control.Base parent, int x, int y, int largeur, int hauteur)
	{
		return (X: Math.Clamp(x, 0, Math.Max(0, parent.Width - largeur)), Y: Math.Clamp(y, 0, Math.Max(0, parent.Height - hauteur)));
	}

	protected override void OnMouseDown(MouseButton mouseButton, Point mousePosition, bool userAction = true)
	{
		base.OnMouseDown(mouseButton, mousePosition, userAction);
		_prise = null;
		_deplacee = false;
		if (!InputHandler.IsLeftMouseDown)
		{
			return;
		}
		Point point = CanvasPosToLocal(mousePosition);
		if (point.Y >= 0 && point.Y < _d.L(HauteurBarre))
		{
			Zone? zone = ZoneSous(point);
			if (!zone.HasValue || zone.GetValueOrDefault().Action == null)
			{
				_prise = point;
			}
		}
	}

	protected override void OnMouseMoved(int x, int y, int dx, int dy)
	{
		base.OnMouseMoved(x, y, dx, dy);
		Point? prise = _prise;
		if (!prise.HasValue)
		{
			return;
		}
		Point valueOrDefault = prise.GetValueOrDefault();
		Intersect.Client.Framework.Gwen.Control.Base parent = base.Parent;
		if (parent == null)
		{
			return;
		}
		if (!InputHandler.IsLeftMouseDown)
		{
			_prise = null;
			return;
		}
		Point point = parent.CanvasPosToLocal(new Point(x, y));
		var (num, num2) = Borner(parent, point.X - valueOrDefault.X, point.Y - valueOrDefault.Y, base.Width, base.Height);
		if (_deplacee || Math.Abs(num - base.X) + Math.Abs(num2 - base.Y) >= 4)
		{
			_deplacee = true;
			_decalage = new Point(num - (parent.Width - base.Width) / 2, num2 - (parent.Height - base.Height) / 2);
			Placer();
		}
	}

	protected override void OnMouseUp(MouseButton mouseButton, Point mousePosition, bool userAction = true)
	{
		base.OnMouseUp(mouseButton, mousePosition, userAction);
		_prise = null;
		_deplacee = false;
	}

	protected Color A(Color color)
	{
		return TechniqueArt.Alpha(color, _fade);
	}

	protected Color A(Color color, float alpha)
	{
		return TechniqueArt.Alpha(color, alpha * _fade);
	}

	protected int Pt(float cssPixels)
	{
		return Math.Max(9, _d.Pt(cssPixels));
	}

	protected void AddHit(Rectangle rect, string cle, Action? action)
	{
		_zonesEnCours.Add(new Zone(rect, cle, action));
	}

	protected void AddHit(Rectangle rect, string cle, Action? action, string son)
	{
		_zonesEnCours.Add(new Zone(rect, cle, action, son));
	}

	protected Point LocalMouse()
	{
		return CanvasPosToLocal(InputHandler.MousePosition);
	}

	protected bool IsHot(string cle)
	{
		return _cleChaude == cle;
	}

	private Zone? ZoneSous(Point local)
	{
		for (int num = _zones.Count - 1; num >= 0; num--)
		{
			if (_zones[num].Rect.Contains(local.X, local.Y))
			{
				return _zones[num];
			}
		}
		return null;
	}

	protected virtual void OnEmptyClick(MouseButton mouseButton, Point local)
	{
	}

	protected override void OnMouseClicked(MouseButton mouseButton, Point mousePosition, bool userAction = true)
	{
		base.OnMouseClicked(mouseButton, mousePosition, userAction);
		if (_deplacee)
		{
			return;
		}
		Point local = CanvasPosToLocal(mousePosition);
		if (mouseButton == MouseButton.Left)
		{
			Zone? zone = ZoneSous(local);
			if (zone.HasValue)
			{
				Zone valueOrDefault = zone.GetValueOrDefault();
				Action action = valueOrDefault.Action;
				if (action != null)
				{
					if (!(valueOrDefault.Son == ""))
					{
						Audio.AddGameSound(valueOrDefault.Son ?? "menu_click.wav", loop: false);
					}
					action();
					return;
				}
			}
		}
		OnEmptyClick(mouseButton, local);
	}

	protected sealed override void RenderUnder(Intersect.Client.Framework.Gwen.Skin.Base skin)
	{
		base.RenderUnder(skin);
		Ombre(skin.Renderer, new Rectangle(0, 0, base.Width, base.Height), _d.L(24f), _d.L(8f), 0.3f);
	}

	protected sealed override void Render(Intersect.Client.Framework.Gwen.Skin.Base skin)
	{
		base.Render(skin);
		Intersect.Client.Framework.Gwen.Renderer.Base renderer = skin.Renderer;
		long millisecondsUtc = Timing.Global.MillisecondsUtc;
		float num = Math.Clamp((float)(millisecondsUtc - _ouverteA) / 160f, 0f, 1f);
		_fade = 1f - (1f - num) * (1f - num);
		_cleChaude = ((!Survolee) ? null : ZoneSous(LocalMouse())?.Cle);
		_zonesEnCours.Clear();
		_etiquetteVue = false;
		Dessiner(renderer, millisecondsUtc);
		if (!_etiquetteVue)
		{
			_etiquetteCle = null;
		}
		ViderLisses(renderer);
		List<Zone> zonesEnCours = _zonesEnCours;
		List<Zone> zones = _zones;
		_zones = zonesEnCours;
		_zonesEnCours = zones;
	}

	protected sealed override void RenderOver(Intersect.Client.Framework.Gwen.Skin.Base skin)
	{
		base.RenderOver(skin);
		Intersect.Client.Framework.Gwen.Renderer.Base renderer = skin.Renderer;
		DessinerDessus(renderer, Timing.Global.MillisecondsUtc);
		ViderLisses(renderer);
	}

	protected abstract void Dessiner(Intersect.Client.Framework.Gwen.Renderer.Base renderer, long now);

	protected virtual void DessinerDessus(Intersect.Client.Framework.Gwen.Renderer.Base renderer, long now)
	{
	}

	protected void Lisser(IGameTexture? texture, Rectangle rect, Color couleur, float u1 = 0f, float v1 = 0f, float u2 = 1f, float v2 = 1f)
	{
		if (texture != null && rect.Width > 0 && rect.Height > 0 && couleur.A > 0)
		{
			_lisses.Add(new Lisse(texture, rect, couleur, u1, v1, u2, v2));
		}
	}

	protected void Lisser(string texture, Rectangle rect, Color couleur)
	{
		Lisser(TechniqueArt.Tex(texture), rect, couleur);
	}

	private void ViderLisses(Intersect.Client.Framework.Gwen.Renderer.Base renderer)
	{
		if (_lisses.Count == 0)
		{
			return;
		}
		GameRenderer renderer2 = Graphics.Renderer;
		bool linearFiltering = renderer2?.LinearFiltering ?? false;
		if (renderer2 != null)
		{
			renderer2.LinearFiltering = true;
		}
		try
		{
			foreach (Lisse liss in _lisses)
			{
				SaiyanHud.Draw(renderer, liss.Texture, liss.Rect.X, liss.Rect.Y, liss.Rect.Width, liss.Rect.Height, liss.Couleur, liss.U1, liss.V1, liss.U2, liss.V2);
			}
		}
		finally
		{
			if (renderer2 != null)
			{
				renderer2.LinearFiltering = linearFiltering;
			}
			_lisses.Clear();
		}
	}

	protected void Ombre(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Rectangle cadre, int rayon, int bas, float force)
	{
		TechniqueArt.Fill(renderer, new Rectangle(cadre.X, cadre.Y + bas, cadre.Width, cadre.Height), A(TechniqueArt.Black, force));
		for (int i = 0; i < rayon; i += 2)
		{
			float num = 1f - (float)i / (float)rayon;
			Rectangle rect = new Rectangle(cadre.X - i - 2, cadre.Y + bas - i - 2, cadre.Width + 2 * (i + 2), cadre.Height + 2 * (i + 2));
			TechniqueArt.Outline(renderer, rect, A(TechniqueArt.Black, force * num * num), 2);
		}
	}

	protected void DessinerCadre(Intersect.Client.Framework.Gwen.Renderer.Base renderer)
	{
		Rectangle rect = new Rectangle(0, 0, base.Width, base.Height);
		TechniqueArt.Patch(renderer, "tech_chanfrein10.png", rect, A(new Color(255, 10, 14, 24)), 12);
		TechniqueArt.Patch(renderer, "tech_panneau.png", rect, A(TechniqueArt.White), 12);
		TechniqueArt.Patch(renderer, "tech_chanfrein10_bord.png", rect, A(TechniqueArt.Line2), 12);
	}

	protected int DessinerBarre(Intersect.Client.Framework.Gwen.Renderer.Base renderer, string titre)
	{
		Rectangle place = Place;
		int num = _d.Y(23f);
		TechniqueArt.Fill(renderer, new Rectangle(1, _d.Y(45f), base.Width - 2, 1), A(TechniqueArt.White, 0.07f));
		string text = titre.ToUpperInvariant();
		Point point = TechniqueArt.Measure(renderer, TechniqueArt.Display, Pt(24f), text);
		TechniqueArt.Styled(renderer, TechniqueArt.Display, Pt(24f), text, _d.X(21f), num - point.Y / 2, new TechniqueArt.Stops(TechniqueArt.White, 0.35f, new Color(255, 255, 194, 122), 0.8f, TechniqueArt.Ki), _fade);
		Rectangle rect = _d.R(place.Width - 41, 8f, 30f, 30f);
		bool flag = IsHot("fermer");
		if (flag)
		{
			TechniqueArt.Outline(renderer, rect, A(TechniqueArt.Line2));
		}
		int num2 = _d.L(14f);
		Lisser("tech_croix.png", new Rectangle(rect.X + (rect.Width - num2) / 2, rect.Y + (rect.Height - num2) / 2, num2, num2), A(flag ? TechniqueArt.White : TechniqueArt.Soft));
		AddHit(rect, "fermer", Fermer);
		return rect.X;
	}

	protected int Touche(Intersect.Client.Framework.Gwen.Renderer.Base renderer, string texte, int x, int centreY)
	{
		int num = LargeurTouche(renderer, texte);
		int num2 = _d.L(19f);
		Rectangle rect = new Rectangle(x, centreY - num2 / 2, num, num2);
		TechniqueArt.Fill(renderer, rect, A(new Color(255, 17, 24, 39)));
		TechniqueArt.Outline(renderer, rect, A(TechniqueArt.Line2));
		TechniqueArt.Fill(renderer, new Rectangle(rect.X, rect.Bottom - 2, rect.Width, 2), A(TechniqueArt.Line2));
		TechniqueArt.TextCenter(renderer, TechniqueArt.Label, Pt(11f), texte, rect.X + rect.Width / 2, rect.Y + (rect.Height - 1) / 2, A(TechniqueArt.Soft));
		return num;
	}

	protected static void Degrade(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Rectangle rect, Color haut, Color bas, int bandes)
	{
		bandes = Math.Clamp(bandes, 1, Math.Max(1, rect.Height));
		for (int i = 0; i < bandes; i++)
		{
			int num = rect.Height * i / bandes;
			int num2 = rect.Height * (i + 1) / bandes;
			TechniqueArt.Fill(renderer, new Rectangle(rect.X, rect.Y + num, rect.Width, num2 - num), SaiyanHud.Lerp(haut, bas, ((float)i + 0.5f) / (float)bandes));
		}
	}

	protected int LargeurTouche(Intersect.Client.Framework.Gwen.Renderer.Base renderer, string texte)
	{
		return TechniqueArt.Measure(renderer, TechniqueArt.Label, Pt(11f), texte).X + _d.L(12f);
	}

	protected static void Triangle(Intersect.Client.Framework.Gwen.Renderer.Base renderer, int gauche, int centreY, int largeur, int hauteur, bool versLeHaut, Color couleur)
	{
		int num = centreY - hauteur / 2;
		for (int i = 0; i < hauteur; i++)
		{
			int num2 = (versLeHaut ? i : (hauteur - 1 - i));
			int num3 = Math.Max(1, (int)MathF.Round((float)(largeur * (num2 + 1)) / (float)hauteur));
			TechniqueArt.Fill(renderer, new Rectangle(gauche + (largeur - num3) / 2, num + i, num3, 1), couleur);
		}
	}

	protected void Hexagone(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Rectangle hex, Color couleur, IGameTexture? icone, Color teinte, bool vide)
	{
		TechniqueArt.Image(renderer, "tech_hexa_bord.png", hex, A(vide ? couleur : TechniqueArt.Mix(couleur, TechniqueArt.White, 0.95f)));
		int num = Math.Max(1, _d.L(2f));
		Rectangle rect = new Rectangle(hex.X + num, hex.Y + num, hex.Width - num * 2, hex.Height - num * 2);
		TechniqueArt.Image(renderer, "tech_hexa.png", rect, A(new Color(255, 8, 12, 21)));
		if (!vide)
		{
			TechniqueArt.Image(renderer, "tech_hexa_radial.png", rect, A(TechniqueArt.Mix(couleur, new Color(255, 24, 32, 51), 0.22f)));
		}
		if (icone != null)
		{
			int num2 = TailleIcone(icone.Width);
			SaiyanHud.Draw(renderer, icone, hex.X + (hex.Width - num2) / 2, hex.Y + (hex.Height - num2) / 2, num2, num2, A(teinte));
		}
	}

	protected void Jauge(Intersect.Client.Framework.Gwen.Renderer.Base renderer, int x, int y, int largeurCran, int hauteur, ReadOnlySpan<float> crans, Color couleur)
	{
		int num = Math.Max(1, _d.L(2f));
		for (int i = 0; i < crans.Length; i++)
		{
			Rectangle rect = new Rectangle(x + i * (largeurCran + num), y, largeurCran, hauteur);
			TechniqueArt.Fill(renderer, rect, A(JaugeEteinte));
			int num2 = (int)MathF.Round((float)largeurCran * Math.Clamp(crans[i], 0f, 1f));
			if (num2 > 0)
			{
				TechniqueArt.Fill(renderer, new Rectangle(rect.X, rect.Y, num2, hauteur), A(couleur));
			}
		}
	}

	protected int LargeurJauge(int largeurCran, int crans)
	{
		return crans * largeurCran + (crans - 1) * Math.Max(1, _d.L(2f));
	}

	protected int TailleIcone(int origine = 32)
	{
		return origine * Math.Max(1, (int)MathF.Floor(_d.S + 0.25f));
	}

	protected void DessinerPuces(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Rectangle zone, IReadOnlyList<Puce> puces)
	{
		if (puces.Count == 0)
		{
			return;
		}
		int num = _d.L(9f);
		int num2 = _d.L(15f);
		int num3 = _d.L(5f);
		int num4 = _d.L(4f);
		float spacing = _d.Em(0.1f, 11f);
		int num5 = num4 * (puces.Count - 1);
		string text = string.Empty;
		foreach (Puce puce in puces)
		{
			num5 += num * 2 + num2 + num3 + TechniqueArt.Measure(renderer, TechniqueArt.Heavy, Pt(14f), puce.Compte).X;
			if (puce.Active)
			{
				text = puce.Nom.ToUpperInvariant();
			}
		}
		int num6 = TechniqueArt.SpacedWidth(renderer, TechniqueArt.Label, Pt(11f), text, spacing) + num3;
		bool flag = num5 + num6 <= zone.Width;
		bool flag2 = !flag && num5 > zone.Width;
		int num7 = (flag2 ? ((zone.Width - num4 * (puces.Count - 1)) / puces.Count) : 0);
		int num8 = zone.X;
		foreach (Puce puce2 in puces)
		{
			int x = TechniqueArt.Measure(renderer, TechniqueArt.Heavy, Pt(14f), puce2.Compte).X;
			int num9 = num2 + num3 + x + ((puce2.Active && flag) ? num6 : 0);
			int num10 = (flag2 ? num7 : (num9 + num * 2));
			Rectangle rect = new Rectangle(num8, zone.Y, num10, zone.Height);
			bool flag3 = IsHot(puce2.Cle);
			if (puce2.Active)
			{
				TechniqueArt.FillVertical(renderer, rect, A(TechniqueArt.Ki, 0.3f), A(new Color(230, 12, 17, 28)));
			}
			else
			{
				TechniqueArt.Fill(renderer, rect, A(new Color(255, 10, 14, 24)));
			}
			TechniqueArt.Outline(renderer, rect, A(puce2.Active ? TechniqueArt.Alpha(TechniqueArt.Ki, 0.75f) : (flag3 ? TechniqueArt.Line2 : TechniqueArt.Line)));
			Color color = A(puce2.Active ? TechniqueArt.White : (flag3 ? TechniqueArt.Text : TechniqueArt.Dim));
			int num11 = rect.X + (rect.Width - num9) / 2;
			int num12 = rect.Y + rect.Height / 2;
			Color teinte = puce2.Teinte;
			Color arg = (((object)teinte != null) ? A((puce2.Active || flag3) ? teinte : TechniqueArt.Mix(teinte, new Color(255, 95, 106, 130), 0.7f)) : color);
			puce2.Glyphe(new Rectangle(num11, num12 - num2 / 2, num2, num2), arg);
			num11 += num2 + num3;
			if (puce2.Active && flag)
			{
				num11 += TechniqueArt.Spaced(renderer, TechniqueArt.Label, Pt(11f), text, num11, num12, spacing, color) + num3;
			}
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Heavy, Pt(14f), puce2.Compte, num11, num12, color);
			if (flag3 && !(puce2.Active && flag))
			{
				Etiqueter(puce2.Cle, rect, puce2.Nom);
			}
			AddHit(rect, puce2.Cle, puce2.Choisir);
			num8 += num10 + num4;
		}
	}

	protected void Etiqueter(string cle, Rectangle rect, string texte)
	{
		_etiquetteVue = true;
		if (_etiquetteCle != cle)
		{
			_etiquetteCle = cle;
			_etiquetteDepuis = Timing.Global.MillisecondsUtc;
		}
		_etiquetteRect = rect;
		_etiquetteTexte = texte;
	}

	protected void DessinerEtiquette(Intersect.Client.Framework.Gwen.Renderer.Base renderer, long now)
	{
		if (_etiquetteCle != null && now - _etiquetteDepuis >= 350)
		{
			int num = TechniqueArt.Measure(renderer, TechniqueArt.Body, Pt(12f), _etiquetteTexte).X + _d.L(16f);
			int h = _d.L(24f);
			int x = Math.Max(_d.L(4f), Math.Min(base.Width - num - _d.L(4f), _etiquetteRect.X + _etiquetteRect.Width / 2 - num / 2));
			Rectangle rect = new Rectangle(x, _etiquetteRect.Bottom + _d.L(6f), num, h);
			TechniqueArt.Fill(renderer, rect, A(new Color(250, 8, 11, 19)));
			TechniqueArt.Outline(renderer, rect, A(TechniqueArt.Line2));
			TechniqueArt.TextCenter(renderer, TechniqueArt.Body, Pt(12f), _etiquetteTexte, rect.X + rect.Width / 2, rect.Y + rect.Height / 2, A(TechniqueArt.Text));
		}
	}
}
