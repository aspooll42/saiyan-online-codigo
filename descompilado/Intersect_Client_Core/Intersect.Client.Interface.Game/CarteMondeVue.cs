using System;
using System.Collections.Generic;
using System.Linq;
using Intersect.Client.Entities;
using Intersect.Client.Framework.Content;
using Intersect.Client.Framework.File_Management;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.Framework.Gwen.Input;
using Intersect.Client.Framework.Gwen.Renderer;
using Intersect.Client.Framework.Gwen.Skin;
using Intersect.Client.Framework.Input;
using Intersect.Client.General;
using Intersect.Client.Interface.Game.EntityPanel;
using Intersect.Client.Localization;
using Intersect.Enums;
using Intersect.Framework.Core;
using Intersect.Framework.Core.GameObjects.WorldMap;
using Intersect.GameObjects;
using Intersect.Models;

namespace Intersect.Client.Interface.Game;

internal sealed class CarteMondeVue : Intersect.Client.Framework.Gwen.Control.Base
{
	private readonly record struct Ligne(string Gauche, string Droite, IFont? Police, int Taille, Color Couleur, Color CouleurDroite, bool Separateur);

	private const float ZoomMax = 3f;

	private const float ZonesJusqua = 115f;

	private const float LieuxDe = 115f;

	private const float LieuxJusqua = 300f;

	private const float CartesDe = 230f;

	private const float RenduDe = 210f;

	private const float RenduJusqua = 320f;

	private const int EpingleLargeur = 32;

	private const int EpingleHauteur = 26;

	private const float FlotteAmplitude = 2.5f;

	private const float FlottePeriode = 1600f;

	private static readonly Color Ocean = new Color(255, 38, 94, 168);

	private static readonly Color Blanc = new Color(255, 255, 255, 255);

	private static readonly Color Creme = new Color(255, 255, 244, 220);

	private static readonly Color Or = new Color(255, 245, 190, 90);

	private static readonly Color Doux = new Color(255, 176, 186, 206);

	private static readonly Color Nuit = new Color(235, 13, 17, 25);

	private static readonly Color Filet = new Color(255, 44, 52, 70);

	private static readonly Color Eteint = new Color(120, 140, 145, 160);

	private static readonly Color Brume = new Color(236, 150, 158, 176);

	private static readonly Dictionary<string, bool> Filtres = new Dictionary<string, bool>
	{
		["lieux"] = true,
		["donjons"] = true,
		["tp"] = true,
		["objectif"] = true,
		["services"] = true,
		["recolte"] = false
	};

	private const int PanneauLargeur = 168;

	private const int PanneauLigne = 21;

	private const int PanneauTete = 24;

	private const int PanneauMarge = 8;

	private CarteMonde? _donnees;

	private IGameTexture? _peinte;

	private IGameTexture? _rendu;

	private IGameTexture? _epingle;

	private IGameTexture? _medaillons;

	private IGameTexture? _nuages;

	private float _camX;

	private float _camY;

	private float _zoom = 0.3f;

	private float _zoomMin = 0.1f;

	private bool _cadree;

	private Point? _appui;

	private float _appuiCamX;

	private float _appuiCamY;

	private bool _glisse;

	private Point _souris;

	private bool _survole;

	public WorldMapDestination[] Destinations { get; set; } = Array.Empty<WorldMapDestination>();

	public string Choisie { get; set; } = string.Empty;

	public event Action<string>? EpingleCliquee;

	public CarteMondeVue(Intersect.Client.Framework.Gwen.Control.Base parent)
		: base(parent, "CarteMondeVue")
	{
		base.MouseInputEnabled = true;
	}

	public void Ouvrir()
	{
		_donnees = CarteMonde.Charger();
		_peinte = GameContentManager.Current.GetTexture(TextureType.Image, "carte_monde_peinte.png");
		_rendu = GameContentManager.Current.GetTexture(TextureType.Image, "carte_monde_rendu.png");
		_epingle = GameContentManager.Current.GetTexture(TextureType.Image, "tp_worldmap.png");
		_medaillons = GameContentManager.Current.GetTexture(TextureType.Image, "carte_monde_lieux.png");
		_nuages = GameContentManager.Current.GetTexture(TextureType.Image, "carte_monde_nuages.png");
		_cadree = false;
		_appui = null;
		_glisse = false;
	}

	public void Centrer(string destinationId)
	{
		if (_donnees != null && _donnees.Destinations.TryGetValue(destinationId, out (float, float) value))
		{
			(_camX, _camY) = value;
			Borner();
		}
	}

	private void Cadrer()
	{
		if (_donnees != null && base.Width > 0 && base.Height > 0)
		{
			_zoomMin = Math.Min((float)base.Width / (float)_donnees.Largeur, (float)base.Height / (float)_donnees.Hauteur) * 0.94f;
			_zoom = _zoomMin;
			_camX = (float)_donnees.Largeur / 2f;
			_camY = (float)_donnees.Hauteur / 2f;
			_cadree = true;
		}
	}

	private void Borner()
	{
		if (_donnees != null)
		{
			_zoom = Math.Clamp(_zoom, _zoomMin * 0.9f, 3f);
			_camX = Math.Clamp(_camX, 0f, _donnees.Largeur);
			_camY = Math.Clamp(_camY, 0f, _donnees.Hauteur);
		}
	}

	private Point VersEcran(float x, float y)
	{
		return new Point((int)MathF.Round((float)base.Width / 2f + (x - _camX) * _zoom), (int)MathF.Round((float)base.Height / 2f + (y - _camY) * _zoom));
	}

	private (float X, float Y) VersMonde(Point ecran)
	{
		return (X: _camX + ((float)ecran.X - (float)base.Width / 2f) / _zoom, Y: _camY + ((float)ecran.Y - (float)base.Height / 2f) / _zoom);
	}

	private static float Lisse(float valeur, float de, float a)
	{
		return Math.Clamp((valeur - de) / (a - de), 0f, 1f);
	}

	private static bool Connue(CarteMonde.Case? carte)
	{
		if (carte != null)
		{
			return Globals.VisitedMaps.Contains(carte.Id);
		}
		return false;
	}

	private bool ConnueEn(float x, float y)
	{
		return Connue(_donnees?.CaseAu(x, y));
	}

	private bool DejaEcrit(string nom, float zones)
	{
		if (zones > 0.02f && _donnees != null)
		{
			return _donnees.Zones.Any((CarteMonde.Zone zone) => ZoneConnue(zone.Cle) && string.Equals(zone.Nom, nom, StringComparison.OrdinalIgnoreCase));
		}
		return false;
	}

	private bool ZoneConnue(string cle)
	{
		if (_donnees == null)
		{
			return false;
		}
		for (int i = 0; i < _donnees.Colonnes; i++)
		{
			for (int j = 0; j < _donnees.Rangees; j++)
			{
				CarteMonde.Case obj = _donnees.Grille[i, j];
				if (obj != null && obj.Zone == cle && Connue(obj))
				{
					return true;
				}
			}
		}
		return false;
	}

	private static (int Min, int Max) Tranche(string niveaux)
	{
		string[] array = (niveaux ?? string.Empty).Split('-');
		int result;
		int num = ((array.Length == 0 || !int.TryParse(array[0], out result)) ? 1 : result);
		int result2;
		int item = ((array.Length > 1 && int.TryParse(array[1], out result2)) ? result2 : num);
		return (Min: num, Max: item);
	}

	private static Color CouleurNiveau(int min, int max)
	{
		int num = Globals.Me?.Level ?? 1;
		if (num > max + 5)
		{
			return new Color(255, 154, 163, 178);
		}
		if (num > max)
		{
			return new Color(255, 111, 212, 111);
		}
		if (num >= min - 1)
		{
			return new Color(255, 245, 208, 90);
		}
		if (num >= min - 4)
		{
			return new Color(255, 255, 154, 61);
		}
		return new Color(255, 255, 90, 82);
	}

	private (float X, float Y)? PointDe(WorldMapDestination destination)
	{
		if (_donnees == null || !_donnees.Destinations.TryGetValue(destination.Id, out (float, float) value))
		{
			return null;
		}
		return value;
	}

	protected override void Render(Intersect.Client.Framework.Gwen.Skin.Base skin)
	{
		base.Render(skin);
		Intersect.Client.Framework.Gwen.Renderer.Base renderer = skin.Renderer;
		Remplir(renderer, new Rectangle(0, 0, base.Width, base.Height), Ocean);
		if (_donnees == null || _peinte == null)
		{
			SaiyanHud.TextCenter(renderer, SaiyanHud.SmallFont, 11, Strings.WorldMap.Missing, 0, 0, base.Width, base.Height, Doux);
			return;
		}
		if (!_cadree)
		{
			Cadrer();
		}
		float num = (float)_donnees.LargeurCarte * _zoom;
		float num2 = 1f - Lisse(num, 75f, 115f);
		float milieu = Lisse(num, 115f, 150f) * (1f - Lisse(num, 230f, 300f));
		float pres = Lisse(num, 230f, 300f);
		float num3 = Lisse(num, 210f, 320f);
		Point point = VersEcran(0f, 0f);
		Rectangle rectangle = new Rectangle(point.X, point.Y, (int)MathF.Round((float)_donnees.Largeur * _zoom), (int)MathF.Round((float)_donnees.Hauteur * _zoom));
		SaiyanHud.Draw(renderer, _peinte, rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height, Blanc);
		if (num3 > 0.01f && _rendu != null)
		{
			SaiyanHud.Draw(renderer, _rendu, rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height, SaiyanHud.WithAlpha(Blanc, num3));
			for (int i = 0; i < _donnees.Colonnes; i++)
			{
				for (int j = 0; j < _donnees.Rangees; j++)
				{
					if (!(_donnees.Grille[i, j] != null))
					{
						Point point2 = VersEcran(i * _donnees.LargeurCarte, j * _donnees.HauteurCarte);
						Remplir(renderer, new Rectangle(point2.X, point2.Y, (int)MathF.Ceiling((float)_donnees.LargeurCarte * _zoom) + 1, (int)MathF.Ceiling((float)_donnees.HauteurCarte * _zoom) + 1), SaiyanHud.WithAlpha(Ocean, num3));
					}
				}
			}
		}
		Brouillard(renderer);
		CarteMonde.Case obj = ((_survole && !_glisse) ? _donnees.CaseAu(VersMonde(_souris).X, VersMonde(_souris).Y) : null);
		if (obj != null)
		{
			Point point3 = VersEcran(obj.X * _donnees.LargeurCarte, obj.Y * _donnees.HauteurCarte);
			Point point4 = new Point((int)MathF.Round((float)_donnees.LargeurCarte * _zoom), (int)MathF.Round((float)_donnees.HauteurCarte * _zoom));
			Cadre(renderer, new Rectangle(point3.X, point3.Y, point4.X, point4.Y), SaiyanHud.WithAlpha(Or, 0.9f));
		}
		Etiquettes(renderer, num2, milieu, pres);
		float etiquette = Lisse(num, 115f, 150f);
		Reperes(renderer, etiquette, num2);
		Petits(renderer, num);
		Objectifs(renderer);
		Epingles(renderer, num2);
		Joueur(renderer);
		PanneauFiltres(renderer);
		if (obj != null && !CadreFiltres().Contains(_souris.X, _souris.Y))
		{
			Bulle(renderer, obj);
		}
	}

	private void Brouillard(Intersect.Client.Framework.Gwen.Renderer.Base renderer)
	{
		if (_donnees == null)
		{
			return;
		}
		int num = (int)MathF.Ceiling((float)_donnees.LargeurCarte * _zoom) + 1;
		int num2 = (int)MathF.Ceiling((float)_donnees.HauteurCarte * _zoom) + 1;
		for (int i = 0; i < _donnees.Colonnes; i++)
		{
			for (int j = 0; j < _donnees.Rangees; j++)
			{
				CarteMonde.Case obj = _donnees.Grille[i, j];
				if (obj == null || Connue(obj))
				{
					continue;
				}
				Point point = VersEcran(i * _donnees.LargeurCarte, j * _donnees.HauteurCarte);
				if (point.X > base.Width || point.Y > base.Height || point.X + num < 0 || point.Y + num2 < 0)
				{
					continue;
				}
				CarteMonde.Silhouette silhouette = _donnees.SilhouetteAu(i, j);
				if ((object)silhouette != null)
				{
					IGameTexture texture = GameContentManager.Current.GetTexture(TextureType.Image, silhouette.Image);
					if (texture != null)
					{
						int x = VersEcran((i + 1) * _donnees.LargeurCarte, 0f).X;
						int y = VersEcran(0f, (j + 1) * _donnees.HauteurCarte).Y;
						float num3 = (float)(i - silhouette.Colonne) / (float)silhouette.Colonnes;
						float num4 = (float)(j - silhouette.Rangee) / (float)silhouette.Rangees;
						SaiyanHud.Draw(renderer, texture, point.X, point.Y, x - point.X, y - point.Y, Blanc, num3, num4, num3 + 1f / (float)silhouette.Colonnes, num4 + 1f / (float)silhouette.Rangees);
						continue;
					}
				}
				Remplir(renderer, new Rectangle(point.X, point.Y, num, num2), Brume);
				if (_nuages != null)
				{
					float num5 = (float)(i & 3) * 0.25f;
					float num6 = (float)(j & 3) * 0.25f;
					SaiyanHud.Draw(renderer, _nuages, point.X, point.Y, num, num2, Blanc, num5, num6, num5 + 0.25f, num6 + 0.25f);
				}
			}
		}
	}

	private void Reperes(Intersect.Client.Framework.Gwen.Renderer.Base renderer, float etiquette, float zones)
	{
		if (_donnees == null || _medaillons == null)
		{
			return;
		}
		int num = ((_zoom * (float)_donnees.LargeurCarte < 90f) ? 26 : 32);
		CarteMonde.Repere[] reperes = _donnees.Reperes;
		foreach (CarteMonde.Repere repere in reperes)
		{
			if (!Filtres.GetValueOrDefault(repere.Filtre, defaultValue: true) || !ConnueEn(repere.X, repere.Y))
			{
				continue;
			}
			Point point = VersEcran(repere.X, repere.Y);
			if (point.X >= -60 && point.Y >= -60 && point.X <= base.Width + 60 && point.Y <= base.Height + 60)
			{
				int height = _medaillons.Height;
				SaiyanHud.Draw(renderer, _medaillons, point.X - num / 2, point.Y - num / 2, num, num, Blanc, (float)(repere.Image * height) / (float)_medaillons.Width, 0f, (float)((repere.Image + 1) * height) / (float)_medaillons.Width);
				if (!DejaEcrit(repere.Nom, zones))
				{
					Texte(renderer, SaiyanHud.SmallFont, 10, repere.Nom, point.X, point.Y + num / 2 + 8, Creme, etiquette);
				}
			}
		}
	}

	private void Petits(Intersect.Client.Framework.Gwen.Renderer.Base renderer, float carteEcran)
	{
		if (_donnees == null)
		{
			return;
		}
		float num = Lisse(carteEcran, 70f, 100f);
		if (num <= 0.02f)
		{
			return;
		}
		if (Filtres.GetValueOrDefault("services"))
		{
			foreach (IGrouping<(float, float), CarteMonde.Service> item in from s in _donnees.Services
				group s by (X: s.X, Y: s.Y) into g
				where ConnueEn(g.Key.X, g.Key.Y)
				select g)
			{
				Point point = VersEcran(item.Key.Item1, item.Key.Item2);
				Rangee(renderer, item.Select((CarteMonde.Service s) => s.Icone).ToArray(), point.X, point.Y + 26, num);
			}
		}
		if (!Filtres.GetValueOrDefault("recolte"))
		{
			return;
		}
		foreach (CarteMonde.Recolte recolte in _donnees.Recoltes.Where((CarteMonde.Recolte r) => ConnueEn(r.X, r.Y)))
		{
			string[] icones = (from m in _donnees.Metiers
				where recolte.Metiers.ContainsKey(m.Cle)
				select m.Icone).ToArray();
			Point point2 = VersEcran(recolte.X, recolte.Y);
			Rangee(renderer, icones, point2.X, point2.Y - 26, num);
		}
	}

	private void Rangee(Intersect.Client.Framework.Gwen.Renderer.Base renderer, string[] icones, int x, int y, float alpha)
	{
		if (icones.Length == 0 || x < -60 || y < -60 || x > base.Width + 60 || y > base.Height + 60)
		{
			return;
		}
		int num = x - (icones.Length - 1) * 22 / 2;
		for (int i = 0; i < icones.Length; i++)
		{
			int num2 = num + i * 22;
			Pastille(renderer, num2, y, 11, SaiyanHud.WithAlpha(Nuit, alpha * 0.85f));
			IGameTexture gameTexture = Globals.ContentManager?.GetTexture(TextureType.Item, icones[i]);
			if (gameTexture != null)
			{
				SaiyanHud.Draw(renderer, gameTexture, num2 - 9, y - 9, 18, 18, SaiyanHud.WithAlpha(Blanc, alpha));
			}
		}
	}

	private void Objectifs(Intersect.Client.Framework.Gwen.Renderer.Base renderer)
	{
		if (_donnees == null || !Filtres.GetValueOrDefault("objectif") || Globals.Me == null)
		{
			return;
		}
		IGameTexture texture = GameContentManager.Current.GetTexture(TextureType.Gui, "hud_quete_repere.png");
		long milliseconds = Timing.Global.Milliseconds;
		foreach (var (id, key, num, num2) in Globals.QuestTargets)
		{
			if (_donnees.ParId.TryGetValue(key, out CarteMonde.Case value) && Connue(value))
			{
				Point point = VersEcran(((float)(value.X * Options.Instance.Map.MapWidth + num) + 0.5f) * (float)_donnees.PixelsParCase, ((float)(value.Y * Options.Instance.Map.MapHeight + num2) + 0.5f) * (float)_donnees.PixelsParCase);
				if (point.X >= -60 && point.Y >= -60 && point.X <= base.Width + 60 && point.Y <= base.Height + 60)
				{
					int num3 = (int)MathF.Round(MathF.Sin((float)(milliseconds % 1600) / 1600f * ((float)Math.PI * 2f)) * 2f);
					SaiyanHud.Draw(renderer, texture, point.X - 11, point.Y - 26 + num3, 22, 26, Blanc);
					string texte = DatabaseObject<QuestDescriptor>.Get(id)?.Name ?? string.Empty;
					Texte(renderer, SaiyanHud.SmallFont, 10, texte, point.X, point.Y + 8, Or, 1f);
				}
			}
		}
	}

	private static (string Cle, string Nom)[] Categories()
	{
		return new(string, string)[6]
		{
			("lieux", Strings.WorldMap.FilterPlaces.ToString()),
			("donjons", Strings.WorldMap.FilterDungeons.ToString()),
			("tp", Strings.WorldMap.FilterTeleports.ToString()),
			("objectif", Strings.WorldMap.FilterObjective.ToString()),
			("services", Strings.WorldMap.FilterServices.ToString()),
			("recolte", Strings.WorldMap.FilterHarvest.ToString())
		};
	}

	private Rectangle CadreFiltres()
	{
		int num = 24 + Categories().Length * 21 + 8;
		return new Rectangle(10, Math.Max(10, base.Height - num - 10), 168, num);
	}

	private void PanneauFiltres(Intersect.Client.Framework.Gwen.Renderer.Base renderer)
	{
		Rectangle rect = CadreFiltres();
		Remplir(renderer, rect, Nuit);
		Remplir(renderer, new Rectangle(rect.X, rect.Y, rect.Width, 2), SaiyanHud.WithAlpha(Or, 0.8f));
		SaiyanHud.Text(renderer, SaiyanHud.NameFont, 12, Strings.WorldMap.Filters, rect.X + 8, rect.Y + 5, Creme);
		(string, string)[] array = Categories();
		for (int i = 0; i < array.Length; i++)
		{
			(string, string) tuple = array[i];
			string item = tuple.Item1;
			string item2 = tuple.Item2;
			int num = rect.Y + 24 + i * 21;
			bool valueOrDefault = Filtres.GetValueOrDefault(item);
			Rectangle rect2 = new Rectangle(rect.X + 8, num + 4, 12, 12);
			Remplir(renderer, rect2, valueOrDefault ? SaiyanHud.BaseAccent.Main : Filet);
			Cadre(renderer, rect2, SaiyanHud.WithAlpha(Doux, 0.8f));
			IconeDeFiltre(renderer, item, rect.X + 8 + 26, num + 10);
			SaiyanHud.Text(renderer, SaiyanHud.SmallFont, 10, item2, rect.X + 8 + 40, num + 3, valueOrDefault ? Creme : Doux);
		}
	}

	private void IconeDeFiltre(Intersect.Client.Framework.Gwen.Renderer.Base renderer, string cle, int x, int y)
	{
		switch (cle)
		{
		case "lieux":
		case "donjons":
			if (_medaillons != null && _donnees != null)
			{
				CarteMonde.Repere repere = _donnees.Reperes.FirstOrDefault((CarteMonde.Repere r) => r.Filtre == cle);
				if (!(repere == null))
				{
					int height = _medaillons.Height;
					SaiyanHud.Draw(renderer, _medaillons, x - 9, y - 9, 18, 18, Blanc, (float)(repere.Image * height) / (float)_medaillons.Width, 0f, (float)((repere.Image + 1) * height) / (float)_medaillons.Width);
				}
			}
			break;
		case "tp":
			SaiyanHud.Draw(renderer, _epingle, x - 11, y - 9, 22, 18, Blanc);
			break;
		case "objectif":
			SaiyanHud.Draw(renderer, GameContentManager.Current.GetTexture(TextureType.Gui, "hud_quete_repere.png"), x - 8, y - 9, 16, 18, Blanc);
			break;
		case "services":
			if (_donnees != null && _donnees.Services.Length != 0)
			{
				SaiyanHud.Draw(renderer, Globals.ContentManager?.GetTexture(TextureType.Item, _donnees.Services[0].Icone), x - 9, y - 9, 18, 18, Blanc);
			}
			break;
		case "recolte":
			if (_donnees != null && _donnees.Metiers.Length != 0)
			{
				SaiyanHud.Draw(renderer, Globals.ContentManager?.GetTexture(TextureType.Item, _donnees.Metiers[0].Icone), x - 9, y - 9, 18, 18, Blanc);
			}
			break;
		}
	}

	private string? FiltreSous(Point point)
	{
		Rectangle rectangle = CadreFiltres();
		if (!rectangle.Contains(point.X, point.Y))
		{
			return null;
		}
		int num = (point.Y - rectangle.Y - 24) / 21;
		(string, string)[] array = Categories();
		if (num < 0 || num >= array.Length)
		{
			return null;
		}
		return array[num].Item1;
	}

	private void Etiquettes(Intersect.Client.Framework.Gwen.Renderer.Base renderer, float loin, float milieu, float pres)
	{
		if (_donnees == null)
		{
			return;
		}
		CarteMonde.Zone[] zones = _donnees.Zones;
		foreach (CarteMonde.Zone zone in zones)
		{
			Point point = VersEcran(zone.X, zone.Y);
			bool flag = ZoneConnue(zone.Cle);
			string texte = (flag ? zone.Nom.ToUpperInvariant() : Strings.WorldMap.Unexplored.ToString().ToUpperInvariant());
			float[] array = (from repere in _donnees.Reperes
				where Proche(repere.X, repere.Y, zone)
				select repere.Y).Concat(from d in Destinations.Where(delegate(WorldMapDestination d)
				{
					(float, float)? tuple2 = PointDe(d);
					if (tuple2.HasValue)
					{
						(float, float) valueOrDefault = tuple2.GetValueOrDefault();
						return Proche(valueOrDefault.Item1, valueOrDefault.Item2, zone);
					}
					return false;
				})
				select PointDe(d).Value.Y).ToArray();
			int num = ((array.Length != 0) ? ((int)MathF.Round(MathF.Max(0f, zone.Y - array.Min()) * _zoom) + 44) : 0);
			int num2 = Math.Max(18, point.Y - 10 - num);
			Texte(renderer, SaiyanHud.NameFont, 17, texte, point.X, num2, flag ? Creme : Doux, loin);
			(int, int) tuple = Tranche(zone.Niveaux);
			Texte(renderer, SaiyanHud.SmallFont, 11, Strings.WorldMap.Levels.ToString(zone.Niveaux), point.X, num2 + 20, CouleurNiveau(tuple.Item1, tuple.Item2), loin);
		}
		CarteMonde.Lieu[] lieux = _donnees.Lieux;
		foreach (CarteMonde.Lieu lieu in lieux)
		{
			if (!_donnees.Zones.Any((CarteMonde.Zone zone2) => string.Equals(zone2.Nom, lieu.Nom, StringComparison.OrdinalIgnoreCase)) && ConnueEn(lieu.X, lieu.Y))
			{
				Point point2 = VersEcran(lieu.X, lieu.Y);
				Texte(renderer, SaiyanHud.NameFont, 14, lieu.Nom, point2.X, point2.Y, Creme, milieu);
			}
		}
		if (pres <= 0.01f)
		{
			return;
		}
		for (int num4 = 0; num4 < _donnees.Colonnes; num4++)
		{
			for (int num5 = 0; num5 < _donnees.Rangees; num5++)
			{
				CarteMonde.Case carte = _donnees.Grille[num4, num5];
				if (!(carte == null) && !Decor(carte) && Connue(carte) && !Destinations.Any(delegate(WorldMapDestination d)
				{
					(float, float)? tuple2 = PointDe(d);
					if (tuple2.HasValue)
					{
						(float, float) valueOrDefault = tuple2.GetValueOrDefault();
						return _donnees.CaseAu(valueOrDefault.Item1, valueOrDefault.Item2) == carte;
					}
					return false;
				}))
				{
					Point point3 = VersEcran(((float)num4 + 0.5f) * (float)_donnees.LargeurCarte, num5 * _donnees.HauteurCarte + 16);
					if (point3.X >= -120 && point3.Y >= -20 && point3.X <= base.Width + 120 && point3.Y <= base.Height + 20)
					{
						Texte(renderer, SaiyanHud.SmallFont, 10, carte.Nom, point3.X, point3.Y, Blanc, pres);
					}
				}
			}
		}
	}

	private bool Proche(float x, float y, CarteMonde.Zone zone)
	{
		if (_donnees != null && MathF.Abs(x - zone.X) < (float)_donnees.LargeurCarte * 0.75f)
		{
			return MathF.Abs(y - zone.Y) < (float)_donnees.HauteurCarte * 0.75f;
		}
		return false;
	}

	private static bool Decor(CarteMonde.Case carte)
	{
		if (!carte.Nom.StartsWith("MER", StringComparison.Ordinal))
		{
			return carte.Nom == "New Map";
		}
		return true;
	}

	private void Epingles(Intersect.Client.Framework.Gwen.Renderer.Base renderer, float zones)
	{
		if (_epingle == null || !Filtres.GetValueOrDefault("tp"))
		{
			return;
		}
		long milliseconds = Timing.Global.Milliseconds;
		for (int i = 0; i < Destinations.Length; i++)
		{
			WorldMapDestination worldMapDestination = Destinations[i];
			if (!worldMapDestination.Unlocked)
			{
				continue;
			}
			(float, float)? tuple = PointDe(worldMapDestination);
			if (!tuple.HasValue)
			{
				continue;
			}
			(float, float) valueOrDefault = tuple.GetValueOrDefault();
			Point point = VersEcran(valueOrDefault.Item1, valueOrDefault.Item2);
			if (point.X < -60 || point.Y < -60 || point.X > base.Width + 60 || point.Y > base.Height + 60)
			{
				continue;
			}
			float num = (float)(worldMapDestination.Id.GetHashCode() & 0xFF) / 255f * ((float)Math.PI * 2f);
			float x = MathF.Sin((float)(milliseconds % 1600) / 1600f * ((float)Math.PI * 2f) + num) * 2.5f;
			int y = point.Y - 26 + (int)MathF.Round(x);
			if (worldMapDestination.Id == Choisie)
			{
				IGameTexture texture = GameContentManager.Current.GetTexture(TextureType.Misc, "ki_halo.png");
				if (texture != null)
				{
					SaiyanHud.Draw(renderer, texture, point.X - 20, point.Y - 24, 40, 40, SaiyanHud.WithAlpha(Or, 0.5f));
				}
			}
			SaiyanHud.Draw(renderer, _epingle, point.X - 16, y, 32, 26, worldMapDestination.Unlocked ? Blanc : Eteint);
			string text = (worldMapDestination.Unlocked ? worldMapDestination.Name : Strings.WorldMap.LockedMarker.ToString());
			if (!DejaEcrit(text, zones))
			{
				Texte(renderer, SaiyanHud.SmallFont, 10, text, point.X, point.Y + 9, worldMapDestination.Unlocked ? Creme : Doux, 1f);
			}
		}
	}

	private void Joueur(Intersect.Client.Framework.Gwen.Renderer.Base renderer)
	{
		if (_donnees == null)
		{
			return;
		}
		Player me = Globals.Me;
		if (me != null && _donnees.ParId.TryGetValue(me.MapId, out CarteMonde.Case value))
		{
			Point point = VersEcran(((float)(value.X * Options.Instance.Map.MapWidth + me.X) + 0.5f) * (float)_donnees.PixelsParCase, ((float)(value.Y * Options.Instance.Map.MapHeight + me.Y) + 0.5f) * (float)_donnees.PixelsParCase);
			IGameTexture texture = GameContentManager.Current.GetTexture(TextureType.Misc, "ki_halo.png");
			if (texture != null)
			{
				SaiyanHud.Draw(renderer, texture, point.X - 14, point.Y - 14, 28, 28, SaiyanHud.WithAlpha(SaiyanHud.BaseAccent.Main, 0.5f));
			}
			IGameTexture gameTexture = SaiyanHud.Texture("carte_environs_joueur.png");
			if (gameTexture != null && renderer is IntersectRenderer intersectRenderer)
			{
				float degrees = me.DirectionFacing switch
				{
					Direction.UpRight => 45f, 
					Direction.Right => 90f, 
					Direction.DownRight => 135f, 
					Direction.Down => 180f, 
					Direction.DownLeft => 225f, 
					Direction.Left => 270f, 
					Direction.UpLeft => 315f, 
					_ => 0f, 
				};
				intersectRenderer.DrawColor = Blanc;
				intersectRenderer.DrawTextureRotated(gameTexture, point.X - 10, point.Y - 10, 20f, 20f, degrees);
			}
			SaiyanHud.Text(renderer, SaiyanHud.SmallFont, 10, Strings.WorldMap.You, point.X + 13, point.Y - 7, SaiyanHud.BaseAccent.Light);
		}
	}

	private void Bulle(Intersect.Client.Framework.Gwen.Renderer.Base renderer, CarteMonde.Case carte)
	{
		List<Ligne> list = new List<Ligne>();
		bool flag = Connue(carte);
		string gauche = ((!flag) ? Strings.WorldMap.Unexplored.ToString() : ((carte.Nom == "New Map") ? Strings.WorldMap.UnnamedMap.ToString() : (carte.Nom.StartsWith("MER", StringComparison.Ordinal) ? Strings.WorldMap.AtSea.ToString() : carte.Nom)));
		string droite = Strings.WorldMap.Coordinates.ToString(carte.X - (_donnees?.OrigineX ?? 0), carte.Y - (_donnees?.OrigineY ?? 0));
		list.Add(new Ligne(gauche, droite, SaiyanHud.NameFont, 14, Blanc, Doux, Separateur: false));
		CarteMonde.Zone zone = _donnees?.ZoneDe(carte);
		if ((object)zone != null)
		{
			(int, int) tuple = Tranche(zone.Niveaux);
			bool flag2 = ZoneConnue(zone.Cle);
			list.Add(new Ligne(flag2 ? zone.Nom : Strings.WorldMap.UnknownZone.ToString(), Strings.WorldMap.Levels.ToString(zone.Niveaux), SaiyanHud.SmallFont, 10, flag2 ? zone.Couleur : Doux, CouleurNiveau(tuple.Item1, tuple.Item2), Separateur: false));
		}
		if (!flag)
		{
			Panneau(renderer, list);
			return;
		}
		CarteMonde.Donjon donjon = carte.Donjon;
		if ((object)donjon != null)
		{
			list.Add(new Ligne(Strings.WorldMap.Dungeon, string.Empty, SaiyanHud.StrongFont, 10, SaiyanHud.BaseAccent.Light, Doux, Separateur: true));
			list.Add(new Ligne(donjon.Nom, string.Empty, SaiyanHud.SmallFont, 10, Blanc, Doux, Separateur: false));
			list.Add(new Ligne(Strings.WorldMap.DungeonLevel.ToString(donjon.NiveauMin, donjon.NiveauMax), string.Empty, SaiyanHud.SmallFont, 10, CouleurNiveau(donjon.NiveauMin, donjon.NiveauMax), Doux, Separateur: false));
			list.Add(new Ligne(Strings.WorldMap.DungeonRooms.ToString(donjon.Salles, string.Join(" · ", donjon.Difficultes)), string.Empty, SaiyanHud.SmallFont, 10, Doux, Doux, Separateur: false));
		}
		WorldMapDestination[] destinations = Destinations;
		foreach (WorldMapDestination worldMapDestination in destinations)
		{
			(float, float)? tuple2 = PointDe(worldMapDestination);
			if (tuple2.HasValue)
			{
				(float, float) valueOrDefault = tuple2.GetValueOrDefault();
				if (!(_donnees?.CaseAu(valueOrDefault.Item1, valueOrDefault.Item2) != carte))
				{
					list.Add(new Ligne(Strings.WorldMap.Teleport, string.Empty, SaiyanHud.StrongFont, 10, SaiyanHud.BaseAccent.Light, Doux, Separateur: true));
					list.Add(new Ligne(worldMapDestination.Unlocked ? Strings.WorldMap.TeleportKnown : Strings.WorldMap.TeleportLocked, string.Empty, SaiyanHud.SmallFont, 10, worldMapDestination.Unlocked ? Blanc : Doux, Doux, Separateur: false));
				}
			}
		}
		CarteMonde.Service[] array = _donnees?.Services.Where((CarteMonde.Service service2) => _donnees.CaseAu(service2.X, service2.Y) == carte).ToArray() ?? Array.Empty<CarteMonde.Service>();
		if (array.Length != 0)
		{
			list.Add(new Ligne(Strings.WorldMap.Services, string.Empty, SaiyanHud.StrongFont, 10, SaiyanHud.BaseAccent.Light, Doux, Separateur: true));
			CarteMonde.Service[] array2 = array;
			foreach (CarteMonde.Service service in array2)
			{
				list.Add(new Ligne(service.Nom, string.Empty, SaiyanHud.SmallFont, 10, Creme, Doux, Separateur: false));
			}
		}
		CarteMonde.Recolte recolte = _donnees?.Recoltes.FirstOrDefault((CarteMonde.Recolte r) => _donnees.CaseAu(r.X, r.Y) == carte);
		if ((object)recolte != null)
		{
			list.Add(new Ligne(Strings.WorldMap.Harvest, string.Empty, SaiyanHud.StrongFont, 10, SaiyanHud.BaseAccent.Light, Doux, Separateur: true));
			foreach (CarteMonde.Metier item in _donnees.Metiers.Where((CarteMonde.Metier m) => recolte.Metiers.ContainsKey(m.Cle)))
			{
				list.Add(new Ligne(item.Nom, Strings.WorldMap.MonsterCount.ToString(recolte.Metiers[item.Cle]), SaiyanHud.SmallFont, 10, Creme, Doux, Separateur: false));
			}
		}
		string[] array3 = (from cible in Globals.QuestTargets
			where cible.MapId == carte.Id
			select DatabaseObject<QuestDescriptor>.Get(cible.QuestId)?.Name ?? string.Empty into nomQuete
			where nomQuete.Length > 0
			select nomQuete).Distinct().ToArray();
		if (array3.Length != 0)
		{
			list.Add(new Ligne(Strings.WorldMap.Objective, string.Empty, SaiyanHud.StrongFont, 10, SaiyanHud.BaseAccent.Light, Doux, Separateur: true));
			string[] array4 = array3;
			foreach (string gauche2 in array4)
			{
				list.Add(new Ligne(gauche2, string.Empty, SaiyanHud.SmallFont, 10, Or, Doux, Separateur: false));
			}
		}
		if (carte.Monstres.Length != 0)
		{
			bool separateur = true;
			foreach (CarteMonde.Monstre item2 in carte.Monstres.Take(5))
			{
				list.Add(new Ligne(Strings.WorldMap.Monster.ToString(item2.Nom, item2.Niveau), Strings.WorldMap.MonsterCount.ToString(item2.Nombre), SaiyanHud.SmallFont, 10, CouleurNiveau(item2.Niveau, item2.Niveau), Doux, separateur));
				separateur = false;
			}
		}
		else if (!carte.Nom.StartsWith("MER", StringComparison.Ordinal))
		{
			list.Add(new Ligne(Strings.WorldMap.NoMonsters, string.Empty, SaiyanHud.SmallFont, 10, Doux, Doux, Separateur: true));
		}
		Panneau(renderer, list);
	}

	private void Panneau(Intersect.Client.Framework.Gwen.Renderer.Base renderer, List<Ligne> lignes)
	{
		int val = 150;
		int num = 20;
		foreach (Ligne ligne in lignes)
		{
			Point point = SaiyanHud.Measure(renderer, ligne.Police, ligne.Taille, ligne.Gauche);
			Point point2 = SaiyanHud.Measure(renderer, ligne.Police, ligne.Taille, ligne.Droite);
			val = Math.Max(val, point.X + ((point2.X > 0) ? (point2.X + 16) : 0) + 20);
			num += Math.Max(point.Y, point2.Y) + 3 + (ligne.Separateur ? 7 : 0);
		}
		val = Math.Min(val, 320);
		int num2 = Math.Clamp(_souris.X + 16, 4, Math.Max(4, base.Width - val - 4));
		int num3 = Math.Clamp(_souris.Y + 16, 4, Math.Max(4, base.Height - num - 4));
		Rectangle rect = new Rectangle(num2, num3, val, num);
		Remplir(renderer, rect, Nuit);
		Remplir(renderer, new Rectangle(num2, num3, val, 2), Or);
		int num4 = num3 + 10;
		foreach (Ligne ligne2 in lignes)
		{
			if (ligne2.Separateur)
			{
				Remplir(renderer, new Rectangle(num2 + 10, num4 + 2, val - 20, 1), Filet);
				num4 += 7;
			}
			SaiyanHud.Text(renderer, ligne2.Police, ligne2.Taille, ligne2.Gauche, num2 + 10, num4, ligne2.Couleur);
			if (ligne2.Droite.Length > 0)
			{
				SaiyanHud.TextRight(renderer, ligne2.Police, ligne2.Taille, ligne2.Droite, num2 + val - 10, num4, SaiyanHud.Measure(renderer, ligne2.Police, ligne2.Taille, ligne2.Gauche).Y, ligne2.CouleurDroite);
			}
			num4 += Math.Max(SaiyanHud.Measure(renderer, ligne2.Police, ligne2.Taille, ligne2.Gauche).Y, SaiyanHud.Measure(renderer, ligne2.Police, ligne2.Taille, ligne2.Droite).Y) + 3;
		}
	}

	protected override void OnMouseDown(MouseButton mouseButton, Point mousePosition, bool userAction = true)
	{
		base.OnMouseDown(mouseButton, mousePosition, userAction);
		if (mouseButton == MouseButton.Left)
		{
			Point point = CanvasPosToLocal(mousePosition);
			string text = FiltreSous(point);
			if (text != null)
			{
				Filtres[text] = !Filtres.GetValueOrDefault(text);
			}
			else if (!CadreFiltres().Contains(point.X, point.Y))
			{
				_appui = point;
				_appuiCamX = _camX;
				_appuiCamY = _camY;
				_glisse = false;
			}
		}
	}

	protected override void OnMouseMoved(int x, int y, int dx, int dy)
	{
		base.OnMouseMoved(x, y, dx, dy);
		_souris = CanvasPosToLocal(InputHandler.MousePosition);
		_survole = true;
		Point? appui = _appui;
		if (appui.HasValue)
		{
			Point valueOrDefault = appui.GetValueOrDefault();
			int num = _souris.X - valueOrDefault.X;
			int num2 = _souris.Y - valueOrDefault.Y;
			if (!_glisse && MathF.Sqrt(num * num + num2 * num2) > 4f)
			{
				_glisse = true;
			}
			if (_glisse)
			{
				_camX = _appuiCamX - (float)num / _zoom;
				_camY = _appuiCamY - (float)num2 / _zoom;
				Borner();
			}
		}
	}

	protected override void OnMouseUp(MouseButton mouseButton, Point mousePosition, bool userAction = true)
	{
		base.OnMouseUp(mouseButton, mousePosition, userAction);
		if (mouseButton != MouseButton.Left)
		{
			return;
		}
		if (_appui.HasValue && !_glisse)
		{
			WorldMapDestination worldMapDestination = EpingleSous(CanvasPosToLocal(mousePosition));
			if (worldMapDestination != null)
			{
				this.EpingleCliquee?.Invoke(worldMapDestination.Id);
			}
		}
		_appui = null;
		_glisse = false;
	}

	protected override void OnMouseLeft()
	{
		base.OnMouseLeft();
		_survole = false;
		_appui = null;
		_glisse = false;
	}

	protected override bool OnMouseWheeled(int delta)
	{
		if (_donnees == null)
		{
			return base.OnMouseWheeled(delta);
		}
		Point ecran = CanvasPosToLocal(InputHandler.MousePosition);
		(float, float) tuple = VersMonde(ecran);
		_zoom = Math.Clamp(_zoom * ((delta > 0) ? 1.2f : (5f / 6f)), _zoomMin * 0.9f, 3f);
		(float, float) tuple2 = VersMonde(ecran);
		_camX += tuple.Item1 - tuple2.Item1;
		_camY += tuple.Item2 - tuple2.Item2;
		Borner();
		return true;
	}

	private WorldMapDestination? EpingleSous(Point point)
	{
		WorldMapDestination[] destinations = Destinations;
		foreach (WorldMapDestination worldMapDestination in destinations)
		{
			if (!worldMapDestination.Unlocked)
			{
				continue;
			}
			(float, float)? tuple = PointDe(worldMapDestination);
			if (tuple.HasValue)
			{
				(float, float) valueOrDefault = tuple.GetValueOrDefault();
				Point point2 = VersEcran(valueOrDefault.Item1, valueOrDefault.Item2);
				int num = point.X - point2.X;
				int num2 = point.Y - (point2.Y - 13);
				if (MathF.Sqrt(num * num + num2 * num2) <= 18f)
				{
					return worldMapDestination;
				}
			}
		}
		return null;
	}

	private static void Remplir(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Rectangle rect, Color couleur)
	{
		if (couleur.A > 0 && rect.Width > 0 && rect.Height > 0)
		{
			renderer.DrawColor = couleur;
			renderer.DrawFilledRect(rect);
		}
	}

	private static void Pastille(Intersect.Client.Framework.Gwen.Renderer.Base renderer, int x, int y, int rayon, Color couleur)
	{
		IGameTexture gameTexture = SaiyanHud.Texture("carte_environs_pastille.png");
		if (gameTexture != null)
		{
			SaiyanHud.Draw(renderer, gameTexture, x - rayon, y - rayon, rayon * 2, rayon * 2, couleur);
		}
	}

	private static void Cadre(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Rectangle rect, Color couleur)
	{
		Remplir(renderer, new Rectangle(rect.X, rect.Y, rect.Width, 2), couleur);
		Remplir(renderer, new Rectangle(rect.X, rect.Y + rect.Height - 2, rect.Width, 2), couleur);
		Remplir(renderer, new Rectangle(rect.X, rect.Y, 2, rect.Height), couleur);
		Remplir(renderer, new Rectangle(rect.X + rect.Width - 2, rect.Y, 2, rect.Height), couleur);
	}

	private static void Texte(Intersect.Client.Framework.Gwen.Renderer.Base renderer, IFont? police, int taille, string texte, int x, int y, Color couleur, float alpha)
	{
		if (!(alpha <= 0.02f) && police != null && !string.IsNullOrEmpty(texte))
		{
			Point point = SaiyanHud.Measure(renderer, police, taille, texte);
			int num = x - point.X / 2;
			int num2 = y - point.Y / 2;
			renderer.DrawColor = SaiyanHud.WithAlpha(SaiyanHud.Shadow, alpha);
			renderer.RenderText(police, taille, new Point(num + 1, num2 + 1), texte);
			renderer.DrawColor = SaiyanHud.WithAlpha(couleur, alpha);
			renderer.RenderText(police, taille, new Point(num, num2), texte);
		}
	}
}
