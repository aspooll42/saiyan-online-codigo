using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Intersect.Client.Framework.File_Management;
using Intersect.Client.Localization;
using Intersect.Configuration;
using Newtonsoft.Json.Linq;

namespace Intersect.Client.Interface.Game;

internal sealed class CarteMonde
{
	public sealed record Zone(string Cle, string Nom, string Niveaux, Color Couleur, float X, float Y);

	public sealed record Lieu(string Nom, float X, float Y);

	public sealed record Donjon(string Nom, int NiveauMin, int NiveauMax, int Salles, string[] Difficultes);

	public sealed record Monstre(string Nom, int Niveau, int Nombre);

	public sealed record Case(Guid Id, int X, int Y, string Nom, string Zone, Monstre[] Monstres, Donjon? Donjon);

	public sealed record Repere(string Nom, string Genre, string Filtre, int Image, float X, float Y);

	public sealed record Service(string Nom, string Icone, float X, float Y);

	public sealed record Recolte(float X, float Y, Dictionary<string, int> Metiers);

	public sealed record Metier(string Cle, string Nom, string Icone);

	public sealed record Silhouette(string Image, int Colonne, int Rangee, int Colonnes, int Rangees);

	private const string Fichier = "carte_monde.json";

	private static CarteMonde? _chargee;

	private static DateTime _dateChargee;

	public int Colonnes { get; private init; }

	public int Rangees { get; private init; }

	public int LargeurCarte { get; private init; }

	public int HauteurCarte { get; private init; }

	public int PixelsParCase { get; private init; }

	public int OrigineX { get; private init; }

	public int OrigineY { get; private init; }

	public Zone[] Zones { get; private init; } = Array.Empty<Zone>();

	public Lieu[] Lieux { get; private init; } = Array.Empty<Lieu>();

	public Repere[] Reperes { get; private init; } = Array.Empty<Repere>();

	public Service[] Services { get; private init; } = Array.Empty<Service>();

	public Recolte[] Recoltes { get; private init; } = Array.Empty<Recolte>();

	public Metier[] Metiers { get; private init; } = Array.Empty<Metier>();

	public Silhouette[] Silhouettes { get; private init; } = Array.Empty<Silhouette>();

	public Case?[,] Grille { get; private init; } = new Case[0, 0];

	public Dictionary<Guid, Case> ParId { get; } = new Dictionary<Guid, Case>();

	public Dictionary<string, (float X, float Y)> Destinations { get; } = new Dictionary<string, (float, float)>(StringComparer.Ordinal);

	public int Largeur => Colonnes * LargeurCarte;

	public int Hauteur => Rangees * HauteurCarte;

	public Zone? ZoneDe(Case carte)
	{
		return Zones.FirstOrDefault((Zone zone) => zone.Cle == carte.Zone);
	}

	public Silhouette? SilhouetteAu(int colonne, int rangee)
	{
		return Silhouettes.FirstOrDefault((Silhouette s) => colonne >= s.Colonne && rangee >= s.Rangee && colonne < s.Colonne + s.Colonnes && rangee < s.Rangee + s.Rangees);
	}

	public Case? CaseAu(float x, float y)
	{
		int num = (int)MathF.Floor(x / (float)LargeurCarte);
		int num2 = (int)MathF.Floor(y / (float)HauteurCarte);
		if (num >= 0 && num2 >= 0 && num < Colonnes && num2 < Rangees)
		{
			return Grille[num, num2];
		}
		return null;
	}

	public static CarteMonde? Charger()
	{
		string chemin = Path.Combine(ClientConfiguration.ResourcesDirectory, "carte_monde.json");
		try
		{
			if (!Coffre.Existe(chemin))
			{
				return null;
			}
			DateTime dateTime = Coffre.DateUtc(chemin);
			if (_chargee != null && dateTime == _dateChargee)
			{
				return _chargee;
			}
			_chargee = Lire(JObject.Parse(Coffre.LireTexte(chemin)));
			_dateChargee = dateTime;
			return _chargee;
		}
		catch
		{
			return null;
		}
	}

	private static string T(string texte)
	{
		return Traduction.Contenu(texte);
	}

	private static CarteMonde Lire(JObject json)
	{
		int num = (int)json["colonnes"];
		int num2 = (int)json["rangees"];
		Recolte[] recoltes = (((JArray)json["recolte"]) ?? new JArray()).Select(delegate(JToken r)
		{
			Dictionary<string, int> dictionary = new Dictionary<string, int>();
			foreach (JProperty item in ((JObject)r).Properties())
			{
				string name = item.Name;
				if ((!(name == "x") && !(name == "y")) || 1 == 0)
				{
					dictionary[item.Name] = (int)item.Value;
				}
			}
			return new Recolte((float)r["x"], (float)r["y"], dictionary);
		}).ToArray();
		CarteMonde carteMonde = new CarteMonde()
		{
			Colonnes = num,
			Rangees = num2,
			LargeurCarte = (int)json["largeurCarte"],
			HauteurCarte = (int)json["hauteurCarte"],
			PixelsParCase = (int)json["pixelsParCase"],
			OrigineX = ((int?)json["origineX"]).GetValueOrDefault(),
			OrigineY = ((int?)json["origineY"]).GetValueOrDefault(),
			Zones = (((JArray)json["zones"]) ?? new JArray()).Select((JToken z) => new Zone((string?)z["cle"], T((string?)z["nom"]), (string?)z["niveaux"], Couleur((string?)z["couleur"]), (float)z["x"], (float)z["y"])).ToArray(),
			Lieux = (((JArray)json["lieux"]) ?? new JArray()).Select((JToken l) => new Lieu(T((string?)l["nom"]), (float)l["x"], (float)l["y"])).ToArray(),
			Reperes = (((JArray)json["reperes"]) ?? new JArray()).Select((JToken r) => new Repere(T((string?)r["nom"]), ((string?)r["genre"]) ?? string.Empty, ((string?)r["filtre"]) ?? "lieux", ((int?)r["image"]).GetValueOrDefault(), (float)r["x"], (float)r["y"])).ToArray(),
			Services = (((JArray)json["services"]) ?? new JArray()).Select((JToken s) => new Service(T((string?)s["nom"]), ((string?)s["icone"]) ?? string.Empty, (float)s["x"], (float)s["y"])).ToArray(),
			Recoltes = recoltes,
			Metiers = (((JArray)json["metiers"]) ?? new JArray()).Select((JToken m) => new Metier((string?)m["cle"], T(((string?)m["nom"]) ?? string.Empty), ((string?)m["icone"]) ?? string.Empty)).ToArray(),
			Silhouettes = (((JArray)json["silhouettes"]) ?? new JArray()).Select((JToken s) => new Silhouette((string?)s["image"], (int)s["colonne"], (int)s["rangee"], (int)s["colonnes"], (int)s["rangees"])).ToArray(),
			Grille = new Case[num, num2],
		};
		CarteMonde carteMonde2 = carteMonde;
		foreach (JToken item2 in ((JArray)json["cartes"]) ?? new JArray())
		{
			Monstre[] monstres = (((JArray)item2["monstres"]) ?? new JArray()).Select((JToken m) => new Monstre(T((string?)m[0]), (int)m[1], (int)m[2])).ToArray();
			Donjon donjon = null;
			if (item2["donjon"] is JObject jObject)
			{
				donjon = new Donjon(T((string?)jObject["nom"]), (int)jObject["niveauMin"], (int)jObject["niveauMax"], ((int?)jObject["salles"]).GetValueOrDefault(), (from s in (((JArray)jObject["difficultes"]) ?? new JArray()).Values<string>()
					select s ?? string.Empty).ToArray());
			}
			Case obj = new Case(Guid.Parse((string?)item2["id"]), (int)item2["x"], (int)item2["y"], T((string?)item2["nom"]), ((string?)item2["zone"]) ?? string.Empty, monstres, donjon);
			if (obj.X >= 0 && obj.Y >= 0 && obj.X < num && obj.Y < num2)
			{
				carteMonde2.Grille[obj.X, obj.Y] = obj;
				carteMonde2.ParId[obj.Id] = obj;
			}
		}
		if (json["destinations"] is JObject jObject2)
		{
			foreach (JProperty item3 in jObject2.Properties())
			{
				carteMonde2.Destinations[item3.Name] = ((float)item3.Value["x"], (float)item3.Value["y"]);
			}
		}
		return carteMonde2;
	}

	private static Color Couleur(string? hexa)
	{
		if (string.IsNullOrWhiteSpace(hexa) || hexa.Length != 7 || hexa[0] != '#' || !int.TryParse(hexa.AsSpan(1), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var result))
		{
			return new Color(255, 245, 190, 90);
		}
		return new Color(255, (result >> 16) & 0xFF, (result >> 8) & 0xFF, result & 0xFF);
	}
}
