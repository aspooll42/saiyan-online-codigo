using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Intersect.Client.Core;
using Intersect.Client.Framework.File_Management;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.General;
using Intersect.Configuration;
using Intersect.Core;
using Intersect.Framework.Core;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace Intersect.Client.Maps;

public static class Vent
{
	internal sealed class ArbreVivant
	{
		public readonly float[] Position = new float[3];

		public readonly float[] Vitesse = new float[3];

		public long Image = -1L;

		public double Vu;

		public double Graine;
	}

	private sealed class FeuilleVolante
	{
		public float X;

		public float Y;

		public float Sol;

		public float Vx;

		public float Vy;

		public float Phase;

		public float Repos;

		public float Duree = 0.9f;

		public bool Morte;

		public ModeleArbre Modele;
	}

	private const string FichierTuiles = "vent.json";

	private const string FichierCartes = "vent-cartes.json";

	private static readonly (float Arbre, float Fleur, float Periode, float Penche, float Feuilles)[] Reglages = new(float, float, float, float, float)[3]
	{
		(0f, 0f, 3.4f, 0f, 0f),
		(0.85f, 0.7f, 3.4f, 0.12f, 1.8f),
		(1.6f, 1.1f, 2.3f, 0.35f, 3.4f)
	};

	private static readonly float[] Raideur = new float[3] { 10f, 16f, 21f };

	private static readonly float[] Amorti = new float[3] { 4.2f, 5f, 6f };

	private static readonly float[] Gain = new float[3] { 1.12f, 1f, 0.9f };

	private static bool _charge;

	private static bool _enPanne;

	private static readonly Dictionary<string, (int Largeur, int Hauteur)> Arbres = new Dictionary<string, (int, int)>(StringComparer.OrdinalIgnoreCase);

	private static readonly Dictionary<string, HashSet<(int X, int Y)>> Fleurs = new Dictionary<string, HashSet<(int, int)>>(StringComparer.OrdinalIgnoreCase);

	private static readonly Dictionary<string, HashSet<(int X, int Y)>> Plantes = new Dictionary<string, HashSet<(int, int)>>(StringComparer.OrdinalIgnoreCase);

	private static readonly Dictionary<Guid, int> NiveauxCartes = new Dictionary<Guid, int>();

	private static DateTime _dateCartes;

	private static long _verificationCartes;

	private static readonly Dictionary<(string Planche, int Bx, int By), ModeleArbre?> Modeles = new Dictionary<(string, int, int), ModeleArbre>();

	private static readonly Dictionary<(ModeleArbre Modele, int X, int Y), ArbreVivant> Vivants = new Dictionary<(ModeleArbre, int, int), ArbreVivant>();

	private static readonly List<FeuilleVolante> Volantes = new List<FeuilleVolante>();

	private static readonly Random Hasard = new Random();

	private static long _image;

	private static double _menage;

	private static double _t;

	private static float _dt;

	private static double _rafale = -99.0;

	private static float _departRafale;

	private static double _prochaineRafale = 2.5;

	private static readonly (int X, int Y, int Ton)[][] Poses = new(int, int, int)[3][]
	{
		new(int, int, int)[4]
		{
			(0, 0, 0),
			(1, 0, 1),
			(1, 1, 1),
			(2, 1, 2)
		},
		new(int, int, int)[3]
		{
			(0, 0, 1),
			(1, 0, 1),
			(2, 0, 2)
		},
		new(int, int, int)[4]
		{
			(0, 1, 0),
			(1, 1, 1),
			(1, 0, 1),
			(2, 0, 2)
		}
	};

	internal static bool Actif
	{
		get
		{
			Charger();
			if (!_enPanne)
			{
				if (Arbres.Count <= 0 && Fleurs.Count <= 0)
				{
					return Plantes.Count > 0;
				}
				return true;
			}
			return false;
		}
	}

	internal static double Temps => _t;

	internal static bool EnPanne => _enPanne;

	private static void Charger()
	{
		if (_charge)
		{
			return;
		}
		_charge = true;
		string text = Path.Combine(ClientConfiguration.ResourcesDirectory, "vent.json");
		try
		{
			if (!Coffre.Existe(text))
			{
				return;
			}
			JObject jObject = JObject.Parse(Coffre.LireTexte(text));
			if (jObject.Value<bool?>("actif") == false)
			{
				return;
			}
			foreach (JToken item in (jObject["arbres"] as JArray) ?? new JArray())
			{
				string text2 = item.Value<string>("planche");
				if (text2 != null && text2.Length > 0)
				{
					Arbres[text2] = (Math.Max(1, item.Value<int?>("blocLargeur") ?? 5), Math.Max(1, item.Value<int?>("blocHauteur") ?? 5));
				}
			}
			LireCases(jObject["fleurs"], "autotuiles", Fleurs);
			LireCases(jObject["plantes"], "tuiles", Plantes);
		}
		catch (Exception exception)
		{
			ApplicationContext.Context.Value?.Logger.LogError(exception, "Lecture de {Fichier} impossible : pas de vent", text);
			Arbres.Clear();
			Fleurs.Clear();
			Plantes.Clear();
		}
	}

	private static void LireCases(JToken? liste, string cle, Dictionary<string, HashSet<(int X, int Y)>> cible)
	{
		foreach (JToken item in (liste as JArray) ?? new JArray())
		{
			string text = item.Value<string>("planche");
			if (text == null || text.Length <= 0)
			{
				continue;
			}
			if (!cible.TryGetValue(text, out HashSet<(int, int)> value))
			{
				value = (cible[text] = new HashSet<(int, int)>());
			}
			foreach (JToken item2 in (item[cle] as JArray) ?? new JArray())
			{
				if (item2 is JArray { Count: >=2 } jArray)
				{
					value.Add((jArray[0].Value<int>(), jArray[1].Value<int>()));
				}
			}
		}
	}

	private static void RelireCartes()
	{
		long milliseconds = Timing.Global.Milliseconds;
		if (milliseconds < _verificationCartes)
		{
			return;
		}
		_verificationCartes = milliseconds + 1000;
		string text = Path.Combine(ClientConfiguration.ResourcesDirectory, "vent-cartes.json");
		try
		{
			DateTime dateTime = (Coffre.Existe(text) ? Coffre.DateUtc(text) : DateTime.MinValue);
			if (dateTime == _dateCartes)
			{
				return;
			}
			_dateCartes = dateTime;
			NiveauxCartes.Clear();
			if (dateTime == DateTime.MinValue)
			{
				return;
			}
			foreach (var (input, jToken2) in JObject.Parse(Coffre.LireTexte(text)))
			{
				if (Guid.TryParse(input, out var result) && jToken2 != null && jToken2.Type == JTokenType.Integer)
				{
					NiveauxCartes[result] = Math.Clamp(jToken2.Value<int>(), 0, 2);
				}
			}
		}
		catch (Exception exception)
		{
			ApplicationContext.Context.Value?.Logger.LogWarning(exception, "Lecture de {Fichier} impossible", text);
		}
	}

	internal static int NiveauCarte(MapInstance carte)
	{
		RelireCartes();
		if (!NiveauxCartes.TryGetValue(carte.Id, out var value))
		{
			return Meteo.NiveauVentAuto(carte);
		}
		return value;
	}

	internal static GenreVent Classer(string planche, IGameTexture texture, int tuileX, int tuileY, byte autotuile, out ModeleArbre? modele)
	{
		modele = null;
		Charger();
		if (_enPanne)
		{
			return GenreVent.Aucun;
		}
		if (autotuile == 0 && Arbres.TryGetValue(planche, out (int, int) value))
		{
			modele = Modele(planche, texture, tuileX / value.Item1, tuileY / value.Item2, value.Item1, value.Item2);
			if (modele != null)
			{
				return GenreVent.Arbre;
			}
			return GenreVent.Aucun;
		}
		if (autotuile == 1 && Fleurs.TryGetValue(planche, out HashSet<(int, int)> value2) && value2.Contains((tuileX, tuileY)))
		{
			return GenreVent.Fleur;
		}
		if (autotuile == 0 && Plantes.TryGetValue(planche, out HashSet<(int, int)> value3) && value3.Contains((tuileX, tuileY)))
		{
			return GenreVent.Plante;
		}
		return GenreVent.Aucun;
	}

	private static ModeleArbre? Modele(string planche, IGameTexture texture, int bx, int by, int largeur, int hauteur)
	{
		if (Modeles.TryGetValue((planche, bx, by), out ModeleArbre value))
		{
			return value;
		}
		value = ModeleArbre.Construire(planche, texture, bx, by, largeur, hauteur);
		Modeles[(planche, bx, by)] = value;
		return value;
	}

	public static void NouvelleImage(TimeSpan duree)
	{
		double num = Math.Clamp(duree.TotalSeconds, 0.0, 0.1);
		_dt = (float)Math.Min(num, 1.0 / 30.0);
		_t += num;
		_image++;
		if (_enPanne || !_charge)
		{
			return;
		}
		int num2 = ((Globals.Me?.MapInstance is MapInstance carte) ? NiveauCarte(carte) : 0);
		if (num2 > 0 && _t > _prochaineRafale)
		{
			_rafale = _t;
			_departRafale = Graphics.WorldViewport.X - 160f;
			_prochaineRafale = _t + ((num2 == 2) ? (4.0 + Hasard.NextDouble() * 2.0) : (6.0 + Hasard.NextDouble() * 3.0));
		}
		if (_t > _menage)
		{
			_menage = _t + 5.0;
			(ModeleArbre, int, int)[] array = (from v in Vivants
				where _t - v.Value.Vu > 5.0
				select v.Key).ToArray();
			foreach ((ModeleArbre, int, int) key in array)
			{
				Vivants.Remove(key);
			}
		}
	}

	internal static float Souffle(float xMonde)
	{
		double num = ((double)xMonde - ((_t - _rafale) * 250.0 + (double)_departRafale)) / 130.0;
		return (float)Math.Exp((0.0 - num) * num);
	}

	internal static void Glisse(float decalage, out float gauche, out float droite, out float part)
	{
		float num = ((Graphics.WorldScale <= 0f) ? 1f : Graphics.WorldScale);
		float num2 = decalage * num;
		float num3 = MathF.Floor(num2);
		part = num2 - num3;
		gauche = num3 / num;
		droite = (num3 + 1f) / num;
	}

	internal static (float Arbre, float Fleur, float Periode, float Penche, float Feuilles) Reglage(int niveau)
	{
		return Reglages[Math.Clamp(niveau, 0, 2)];
	}

	internal static float Pixel(float decalage)
	{
		float num = ((Graphics.WorldScale <= 0f) ? 1f : Graphics.WorldScale);
		return MathF.Round(decalage * num) / num;
	}

	internal static float Lisse(float u)
	{
		u = Math.Clamp(u, 0f, 1f);
		return u * u * (3f - 2f * u);
	}

	private static double Hash(double a, double b)
	{
		double num = Math.Sin(a * 127.1 + b * 311.7) * 43758.5453;
		return num - Math.Floor(num);
	}

	private static double Bruit(double x)
	{
		double num = Math.Floor(x);
		double num2 = x - num;
		num2 = num2 * num2 * (3.0 - 2.0 * num2);
		return (Hash(num, 0.0) * (1.0 - num2) + Hash(num + 1.0, 0.0) * num2) * 2.0 - 1.0;
	}

	internal static double Bruit2(double x, double y)
	{
		double num = Math.Floor(x);
		double num2 = Math.Floor(y);
		double num3 = x - num;
		double num4 = y - num2;
		num3 = num3 * num3 * (3.0 - 2.0 * num3);
		num4 = num4 * num4 * (3.0 - 2.0 * num4);
		return (Hash(num, num2) * (1.0 - num3) + Hash(num + 1.0, num2) * num3) * (1.0 - num4) + (Hash(num, num2 + 1.0) * (1.0 - num3) + Hash(num + 1.0, num2 + 1.0) * num3) * num4;
	}

	internal static ArbreVivant Arbre(ModeleArbre modele, int origineX, int origineY, int niveau)
	{
		if (!Vivants.TryGetValue((modele, origineX, origineY), out ArbreVivant value))
		{
			value = new ArbreVivant
			{
				Graine = Hash((double)origineX * 0.37, (double)origineY * 0.71) * 97.0
			};
			Vivants[(modele, origineX, origineY)] = value;
		}
		if (value.Image == _image)
		{
			return value;
		}
		value.Image = _image;
		value.Vu = _t;
		(float, float, float, float, float) tuple = Reglage(niveau);
		double num = 3.4 / (double)tuple.Item3;
		float xMonde = (float)(origineX * modele.LargeurTuile) + (float)modele.Largeur / 2f;
		float num2 = Souffle(xMonde);
		float num3 = (float)((double)tuple.Item1 * (0.62 * (0.72 * Bruit(_t * 0.42 * num + value.Graine) + 0.28 * Bruit(_t * 1.15 * num + value.Graine * 1.9)) + (double)tuple.Item4 + 1.1 * (double)num2));
		for (int i = 0; i < 3; i++)
		{
			value.Vitesse[i] += (Raideur[i] * (num3 * Gain[i] - value.Position[i]) - Amorti[i] * value.Vitesse[i]) * _dt;
			value.Position[i] += value.Vitesse[i] * _dt;
		}
		float num4 = num2 + ((niveau == 2) ? 0.2f : 0f);
		if (niveau > 0 && modele.Points.Length != 0 && Volantes.Count < 60 && Hasard.NextDouble() < (double)(tuple.Item5 * num4 * _dt))
		{
			var (num5, num6) = modele.Points[Hasard.Next(modele.Points.Length)];
			Volantes.Add(new FeuilleVolante
			{
				X = (float)(origineX * modele.LargeurTuile + num5) + value.Position[0],
				Y = origineY * modele.HauteurTuile + num6,
				Sol = (float)(origineY * modele.HauteurTuile + modele.Pied - 4) + (float)Hasard.NextDouble() * 26f,
				Vx = 16f + (float)Hasard.NextDouble() * 14f,
				Vy = 9f + (float)Hasard.NextDouble() * 7f,
				Phase = (float)(Hasard.NextDouble() * Math.PI * 2.0),
				Modele = modele
			});
		}
		return value;
	}

	internal static bool Secouer(float x, float y, float force, float poussee = 1f)
	{
		if (_enPanne || Vivants.Count == 0)
		{
			return false;
		}
		bool result = false;
		foreach (KeyValuePair<(ModeleArbre, int, int), ArbreVivant> vivant in Vivants)
		{
			vivant.Deconstruct(out var key, out var value);
			(ModeleArbre, int, int) tuple = key;
			ModeleArbre item = tuple.Item1;
			int item2 = tuple.Item2;
			int item3 = tuple.Item3;
			ArbreVivant arbreVivant = value;
			if (_t - arbreVivant.Vu > 1.0)
			{
				continue;
			}
			int num = item2 * item.LargeurTuile;
			int num2 = item3 * item.HauteurTuile;
			float num3 = (float)num + (float)item.Largeur / 2f;
			int num4 = num2 + item.Pied;
			if (MathF.Abs(x - num3) > (float)item.Largeur * 0.3f + 6f || y < (float)num4 - 56f || y > (float)num4 + 10f)
			{
				continue;
			}
			result = true;
			float num5 = ((x < num3) ? 1f : (-1f));
			for (int i = 0; i < 3; i++)
			{
				arbreVivant.Vitesse[i] += num5 * 22f * force * poussee * Gain[i];
			}
			int num6 = (int)(24f * force);
			for (int j = 0; j < num6; j++)
			{
				if (Volantes.Count >= 120)
				{
					break;
				}
				if (item.Points.Length == 0)
				{
					break;
				}
				var (num7, num8) = item.Points[Hasard.Next(item.Points.Length)];
				Volantes.Add(new FeuilleVolante
				{
					X = (float)(num + num7) + arbreVivant.Position[0],
					Y = num2 + num8,
					Sol = (float)(num4 - 6) + (float)Hasard.NextDouble() * 34f,
					Vx = ((float)Hasard.NextDouble() - 0.5f) * 20f,
					Vy = 8f + (float)Hasard.NextDouble() * 10f,
					Phase = (float)(Hasard.NextDouble() * Math.PI * 2.0),
					Duree = 2.5f + (float)Hasard.NextDouble() * 1.5f,
					Modele = item
				});
			}
		}
		return result;
	}

	internal static int Reflet(ModeleArbre modele, ArbreVivant arbre, int origineX, int niveau)
	{
		if (niveau == 0)
		{
			return -1;
		}
		float num = Souffle((float)(origineX * modele.LargeurTuile) + (float)modele.Largeur / 2f) + ((niveau == 2) ? 0.2f : 0f);
		if (!(num > 0.28f))
		{
			return -1;
		}
		return (int)(Math.Floor(_t / 0.14 + arbre.Graine) % 6.0);
	}

	public static void DessinerFeuilles()
	{
		if (Volantes.Count == 0 || _enPanne)
		{
			return;
		}
		IGameTexture gameTexture = Graphics.Renderer?.WhitePixel;
		if (gameTexture == null)
		{
			return;
		}
		try
		{
			float num = (float)_t;
			foreach (FeuilleVolante volante in Volantes)
			{
				if (volante.Repos > 0f)
				{
					volante.Repos -= _dt;
					volante.Morte = volante.Repos <= 0f;
				}
				else
				{
					volante.X += (volante.Vx * (0.35f + Souffle(volante.X)) + MathF.Sin(num * 4.2f + volante.Phase) * 10f) * _dt;
					volante.Y += (volante.Vy + MathF.Sin(num * 2.6f + volante.Phase) * 5f) * _dt;
					if (volante.Y >= volante.Sol)
					{
						volante.Y = volante.Sol;
						volante.Repos = volante.Duree;
					}
					FloatRect worldViewport = Graphics.WorldViewport;
					volante.Morte = volante.X > worldViewport.Right + 32f || volante.Y > worldViewport.Bottom + 32f;
				}
				float num2 = MathF.Sin(num * 7f + volante.Phase);
				(int, int, int)[] array = Poses[(!(volante.Repos > 0f)) ? ((!(num2 > 0.35f)) ? ((!(num2 < -0.35f)) ? 1 : 2) : 0) : 0];
				float num3 = Pixel(volante.X);
				float num4 = Pixel(volante.Y);
				(int, int, int)[] array2 = array;
				for (int i = 0; i < array2.Length; i++)
				{
					var (num5, num6, num7) = array2[i];
					Graphics.DrawGameTexture(gameTexture, new FloatRect(0f, 0f, 1f, 1f), new FloatRect(num3 + (float)num5, num4 + (float)num6, 1f, 1f), volante.Modele.Tons[num7]);
				}
			}
			Volantes.RemoveAll((FeuilleVolante feuille) => feuille.Morte);
		}
		catch (Exception exception)
		{
			Panne(exception);
		}
	}

	internal static void Panne(Exception exception)
	{
		if (!_enPanne)
		{
			_enPanne = true;
			Volantes.Clear();
			ApplicationContext.Context.Value?.Logger.LogError(exception, "Vent dans les cartes coupe apres un incident");
		}
	}
}
