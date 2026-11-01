using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Intersect.Client.Core;
using Intersect.Client.Entities;
using Intersect.Client.Framework.Content;
using Intersect.Client.Framework.File_Management;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.Framework.Maps;
using Intersect.Client.General;
using Intersect.Config;
using Intersect.Configuration;
using Intersect.Core;
using Intersect.Framework.Core;
using Intersect.Framework.Core.GameObjects.Casse;
using Intersect.Framework.Core.GameObjects.Maps;
using Intersect.Framework.Core.GameObjects.Maps.Attributes;
using Intersect.Models;
using Intersect.Network.Packets.Server;
using Microsoft.Extensions.Logging;

namespace Intersect.Client.Maps;

public static class Casse
{
	private sealed class MorceauDeCime
	{
		public IGameTexture? Vert;

		public IGameTexture? Brule;

		public int L;

		public int H;

		public float ReposX;

		public float ReposY;

		public float ReculX;

		public float ReculY;

		public float X;

		public float Y;

		public float Z;

		public float Vx;

		public float Vy;

		public float Vz;

		public float Angle;

		public float Tour;

		public long Depart;

		public long FinA;

		public long PoseA;

		public long FiniA;

		public bool Ki;

		public bool Retombe;

		public bool Rebondi;

		public bool Defait;

		public Guid Carte;

		public int Objet;

		public int Partie;

		public int Graine;
	}

	private sealed class ArbreQuiBascule
	{
		public IGameTexture? Bloc;

		public int L;

		public int H;

		public int Sens;

		public int Objet;

		public int Partie;

		public int Graine;

		public float X0;

		public float Y0;

		public float PivotX;

		public float PivotY;

		public float Sol;

		public long Depart;

		public Guid Carte;

		public readonly List<(IGameTexture Texture, int L, int H, float Cx, float Cy)> Cellules = new List<(IGameTexture, int, int, float, float)>();
	}

	private sealed class TasDeFeuilles
	{
		public Guid Carte;

		public float X;

		public float Y;

		public long Ne;

		public int Objet;

		public int Partie;
	}

	private readonly record struct EclairDeChoc(float X, float Y, double T, float Rayon);

	internal sealed class EtatPartie
	{
		public CasseEtat Etat;

		public int Points;

		public int Max;

		public long Depuis;

		public readonly List<(float X, float Y, int Graine, CasseCoup Coup)> Coups = new List<(float, float, int, CasseCoup)>();

		public bool Tranche;

		public sbyte Couche;
	}

	private sealed class BlocDeRocher
	{
		public readonly List<int> Pixels = new List<int>();

		public int X0 = int.MaxValue;

		public int Y0 = int.MaxValue;

		public int X1 = -1;

		public int Y1 = -1;

		public int Dx;

		public int Dy;
	}

	private sealed class Particule
	{
		public int Genre;

		public float X;

		public float Y;

		public float Vx;

		public float Vy;

		public float Sol;

		public float Taille;

		public long Ne;

		public int Vie;

		public Color Couleur = Color.White;
	}

	private enum GenreDeGerbe
	{
		Feuilles,
		Poudre,
		Eclats
	}

	private readonly record struct Gerbe(GenreDeGerbe Genre, float X, float Y, double T, int Graine, byte R, byte G, byte B);

	private readonly record struct EclatsDeMur(float X, float Y, float Sol, byte R, byte G, byte B, int Graine, int N, double T, float Largeur, Guid Carte, int Objet);

	private sealed class Barre
	{
		public double T;

		public int Avant;

		public int Points;

		public int Max;

		public float X;

		public float Haut;

		public float Largeur;
	}

	private readonly record struct FlammeAuMonde(Flamme Taille, float X, float Y, int Phase, double T0, double T1);

	private readonly record struct Fumee(float X, float Y, int Graine, float Echelle, double P0, double P1, double W1);

	private readonly record struct Etincelles(float X, float Y, int Graine, int N, double T0, double T1);

	private readonly record struct Poussiere(float X0, float X1, float Y, float Haut, double T, int Graine);

	private enum Humeur
	{
		Feu,
		Braise,
		Fume
	}

	private sealed class Mulberry(int graine)
	{
		private uint _a = (uint)graine;

		public double Suivant()
		{
			_a += 1831565813u;
			uint num = (_a ^ (_a >> 15)) * (1 | _a);
			num = (num + (num ^ (num >> 7)) * (0x3D | num)) ^ num;
			return (double)(num ^ (num >> 14)) / 4294967296.0;
		}
	}

	private enum Flamme
	{
		S,
		M,
		L,
		XL,
		G
	}

	private sealed class Bande
	{
		public readonly IGameTexture?[] Images = new IGameTexture[8];

		public int L;

		public int H;

		public int Pied;
	}

	private sealed class Braises
	{
		public IGameTexture? Lueur;

		public readonly List<(int X, int Y, int N)> Amas = new List<(int, int, int)>();

		public int L;

		public int H;
	}

	internal sealed record MotifDeForet(string Planche, int ArbreX, int ArbreY, int ArbreL, int ArbreH, (int X, int Y) Pied, PieceDeForet[] Pieces);

	internal sealed record PieceDeForet(int TX, int TY, int L, int H, (int X, int Y)[] Arbres, int Periode = 0);

	internal sealed class Foret
	{
		private float _feuilleMin = -1f;

		private float _feuilleMax;

		private const float CroissantRoussi = 26f;

		private float[]? _profondeur;

		public required MotifDeForet Motif { get; init; }

		public required byte[] Arbre { get; init; }

		public required (int X, int Y)[] Arbres { get; init; }

		public required Dictionary<string, bool[]> DuMotif { get; init; }

		public Dictionary<string, bool[]> DuSeul { get; init; } = new Dictionary<string, bool[]>();

		public bool[] Seul { get; init; } = Array.Empty<bool>();

		public (int X, int Y, int L, int H) Tuiles { get; init; }

		public float[] Etat { get; private set; } = Array.Empty<float>();

		public List<SillonVu> Sillons { get; set; } = new List<SillonVu>();

		private float[] Profondeur => _profondeur ?? (_profondeur = ProfondeurDe(Arbre, Motif.ArbreL, Motif.ArbreH));

		public bool MargeDUnTombe(int X, int Y)
		{
			if (Etat.Length != Arbres.Length || Seul.Length != Arbres.Length)
			{
				return false;
			}
			for (int i = 0; i < Arbres.Length; i++)
			{
				if (Seul[i] && !(Etat[i] < 2f))
				{
					int num = X - Arbres[i].X;
					int num2 = Y - Arbres[i].Y;
					if (num >= -Tuiles.X && num2 >= -Tuiles.Y && num < Tuiles.L - Tuiles.X && num2 < Tuiles.H - Tuiles.Y && (num < 0 || num2 < 0 || num >= Motif.ArbreL || num2 >= Motif.ArbreH))
					{
						return true;
					}
				}
			}
			return false;
		}

		public void Etats(IReadOnlyList<Zone> zones)
		{
			Etat = new float[Arbres.Length];
			Array.Fill(Etat, -1f);
			foreach (Zone zone in zones)
			{
				float cx = zone.Cx;
				float num = zone.Cy + 50f;
				for (int i = 0; i < Arbres.Length; i++)
				{
					float num2 = (float)(Arbres[i].X + Motif.Pied.X) - cx;
					float num3 = (float)(Arbres[i].Y + Motif.Pied.Y) - num;
					if (num2 * num2 + num3 * num3 < 256f)
					{
						Etat[i] = ((zone.Etat == CasseEtat.Casse) ? 2f : Math.Max(Etat[i], zone.T));
					}
				}
			}
		}

		public (int X, int Y)? PiedDe(int x, int y)
		{
			(int, int)? result = null;
			float num = 256f;
			(int, int)[] arbres = Arbres;
			for (int i = 0; i < arbres.Length; i++)
			{
				(int, int) tuple = arbres[i];
				int item = tuple.Item1;
				int item2 = tuple.Item2;
				int num2 = item + Motif.Pied.X - x;
				int num3 = item2 + Motif.Pied.Y - y;
				if ((float)(num2 * num2 + num3 * num3) < num)
				{
					num = num2 * num2 + num3 * num3;
					result = (item + Motif.Pied.X, item2 + Motif.Pied.Y);
				}
			}
			return result;
		}

		public void Appliquer(byte[] px, int i, int X, int Y)
		{
			bool flag = false;
			bool flag2 = false;
			for (int j = 0; j < Arbres.Length; j++)
			{
				int num = X - Arbres[j].X;
				int num2 = Y - Arbres[j].Y;
				if (num < 0 || num2 < 0 || num >= Motif.ArbreL || num2 >= Motif.ArbreH)
				{
					if (num2 == -1 && num >= 0 && num < Motif.ArbreL && Etat[j] >= 2f)
					{
						flag2 = true;
					}
					continue;
				}
				int num3 = (num2 * Motif.ArbreL + num) * 4;
				byte b = Arbre[num3 + 3];
				if (b < 128)
				{
					if (b > 0 && Etat[j] >= 2f)
					{
						flag2 = true;
					}
					continue;
				}
				if (Etat[j] >= 2f)
				{
					flag = true;
					continue;
				}
				if (Etat[j] >= 0f)
				{
					Bruler(px, i, num3, Etat[j], X, Y);
					return;
				}
				if (flag)
				{
					px[i] = Arbre[num3];
					px[i + 1] = Arbre[num3 + 1];
					px[i + 2] = Arbre[num3 + 2];
					px[i + 3] = byte.MaxValue;
				}
				Roussir(px, i, j, X, Y);
				return;
			}
			if (flag || flag2)
			{
				px[i + 3] = 0;
			}
		}

		private float TonFeuille(float l)
		{
			if (_feuilleMin < 0f)
			{
				_feuilleMin = 1f;
				_feuilleMax = 0f;
				for (int i = 0; i < Arbre.Length; i += 4)
				{
					if (Arbre[i + 3] >= 128 && Arbre[i + 1] > Arbre[i] + 8 && Arbre[i + 1] > Arbre[i + 2] + 8)
					{
						float y = (0.3f * (float)(int)Arbre[i] + 0.59f * (float)(int)Arbre[i + 1] + 0.11f * (float)(int)Arbre[i + 2]) / 255f;
						_feuilleMin = MathF.Min(_feuilleMin, y);
						_feuilleMax = MathF.Max(_feuilleMax, y);
					}
				}
			}
			return Math.Clamp((l - _feuilleMin) / MathF.Max(0.01f, _feuilleMax - _feuilleMin), 0f, 1f);
		}

		private static float DepuisLeBord(SillonVu s, float piedX, float piedY, int X, int Y)
		{
			(float X, float Y) tuple = s.Proche(piedX, piedY);
			float item = tuple.X;
			float item2 = tuple.Y;
			float num = item - piedX;
			float num2 = item2 - piedY;
			float num3 = MathF.Sqrt(num * num + num2 * num2);
			if (num3 < 1f)
			{
				num = 0f;
				num2 = -1f;
			}
			else
			{
				num /= num3;
				num2 /= num3;
			}
			float num4 = ((float)X - piedX) / 40f;
			float num5 = ((float)Y - (piedY - 55f)) / 33f;
			float num6 = num / 40f;
			float num7 = num2 / 33f;
			float num8 = num6 * num6 + num7 * num7;
			float num9 = num4 * num6 + num5 * num7;
			float num10 = num9 * num9 - num8 * (num4 * num4 + num5 * num5 - 1f);
			if (!(num10 <= 0f))
			{
				return MathF.Max(0f, (0f - num9 + MathF.Sqrt(num10)) / num8);
			}
			return 0f;
		}

		private void Roussir(byte[] px, int i, int a, int X, int Y)
		{
			if (Sillons.Count == 0 || px[i + 1] <= px[i] + 8 || px[i + 1] <= px[i + 2] + 8)
			{
				return;
			}
			float num = Arbres[a].X + Motif.Pied.X;
			float num2 = Arbres[a].Y + Motif.Pied.Y;
			float num3 = float.MinValue;
			foreach (SillonVu sillon in Sillons)
			{
				float num4 = sillon.Distance(num, num2);
				if (num4 > sillon.Demi + 10f && num4 <= sillon.Demi + 60f)
				{
					float x = (sillon.Demi + 40f - num4) / 40f * 0.6f + (sillon.Demi + 30f - sillon.Distance(X, Y + 20)) / 30f * 0.6f;
					num3 = MathF.Max(num3, MathF.Min(x, 1.05f - DepuisLeBord(sillon, num, num2, X, Y) / 26f));
				}
			}
			if (num3 != float.MinValue)
			{
				float num5 = TonFeuille((0.3f * (float)(int)px[i] + 0.59f * (float)(int)px[i + 1] + 0.11f * (float)(int)px[i + 2]) / 255f);
				float num6 = num3 + (num5 - 0.5f) * 0.35f + (Bruit(X, Y, 88, 6f) - 0.5f) * 0.3f;
				(byte, byte, byte)? tuple = ((num6 > 0.95f) ? new(byte, byte, byte)?(Rampe(RampeCharbon, 0.35f + num5 * 0.6f)) : ((num6 > 0.65f) ? new(byte, byte, byte)?(Rampe(RampeAutomne, num5)) : ((num6 > 0.35f) ? new(byte, byte, byte)?(Rampe(RampeSec, num5)) : (((byte, byte, byte)?)null))));
				if (tuple.HasValue)
				{
					(byte, byte, byte) valueOrDefault = tuple.GetValueOrDefault();
					ref byte reference = ref px[i];
					ref byte reference2 = ref px[i + 1];
					ref byte reference3 = ref px[i + 2];
					(reference, reference2, reference3) = valueOrDefault;
				}
			}
		}

		private void Bruler(byte[] px, int i, int s, float t, int X, int Y)
		{
			float num = (int)Arbre[s];
			float num2 = (int)Arbre[s + 1];
			float num3 = (int)Arbre[s + 2];
			float num4 = (0.3f * num + 0.59f * num2 + 0.11f * num3) / 255f;
			float num5 = Math.Clamp((num4 - 0.42f) * 1.8f + 0.42f, 0f, 1f);
			int num6 = (int)(t * 5f);
			float num7 = Profondeur[s / 4] + (Bruit(X, Y, 877, 5f) - 0.5f) * 0.3f;
			float num8 = t * 1.15f;
			float value;
			float value2;
			float value3;
			if (!(num2 > num + 8f) || !(num2 > num3 + 8f))
			{
				float num9 = Math.Clamp(t * 1.3f, 0f, 1f);
				value = num + (18f + 62f * num5 - num) * num9;
				value2 = num2 + (13f + 40f * num5 - num2) * num9;
				value3 = num3 + (12f + 28f * num5 - num3) * num9;
				if (t > 0.3f && Hache(X, Y, 419 + num6) % 100 < 2)
				{
					value = 170f;
					value2 = 52f;
					value3 = 14f;
				}
			}
			else if (Profondeur[s / 4] < 0.07f)
			{
				float num10 = (1f - Math.Clamp((t - 0.75f) / 0.25f, 0f, 0.6f)) * Math.Clamp((Bruit(X, Y, 353 + num6, 4f) - 0.25f) * 2f, 0.15f, 1f);
				value = 150f + 105f * num10;
				value2 = 45f + 95f * num10 * (0.5f + num5 * 0.5f);
				value3 = 12f + 18f * num10;
			}
			else if (num7 < num8 - 0.14f)
			{
				value = 18f + 62f * num5;
				value2 = 13f + 40f * num5;
				value3 = 12f + 28f * num5;
				if (Hache(X, Y, 419 + num6) % 100 < 3)
				{
					value = 170f;
					value2 = 52f;
					value3 = 14f;
				}
			}
			else if (num7 < num8)
			{
				float num11 = 1f - (num8 - num7) / 0.14f;
				value = 150f + 105f * num11;
				value2 = 40f + (70f + 90f * num5) * num11;
				value3 = 10f + 30f * num11 * num11;
				if (Hache(X, Y, 911 + num6) % 100 < 12)
				{
					value = 255f;
					value2 = 220f;
					value3 = 90f;
				}
			}
			else
			{
				float num12 = Math.Clamp(t * 2.2f, 0f, 1f) * Math.Clamp(1f - (num7 - num8) / 0.45f, 0.25f, 1f);
				float num13 = 60f + 160f * num5;
				float num14 = 40f + 100f * num5;
				float num15 = 12f + 28f * num5;
				value = num + (num13 - num) * num12;
				value2 = num2 + (num14 - num2) * num12;
				value3 = num3 + (num15 - num3) * num12;
			}
			px[i] = (byte)Math.Clamp(value, 0f, 255f);
			px[i + 1] = (byte)Math.Clamp(value2, 0f, 255f);
			px[i + 2] = (byte)Math.Clamp(value3, 0f, 255f);
			px[i + 3] = byte.MaxValue;
		}
	}

	internal readonly record struct DessinTombe(byte[]? Rgba, IGameTexture? Image, int L, int H, int Sx, int Sy, int W, int Hh, int Dx, int Dy, string Couche);

	private sealed class Morceau
	{
		public IGameTexture? Texture;

		public float X0;

		public float Y0;

		public int L;

		public int H;

		public float Vx;

		public float Vy;

		public long Depart;

		public float Chute;
	}

	private readonly record struct DecorRange(MapInstance Carte, int Objet, CasseObjet O, Decor Decor, float Sol, int Rangee, int Ordre);

	internal sealed class Decor
	{
		public required string Image;

		public required string Couche;

		public FloatRect Source;

		public float X;

		public float Y;

		public int Partie = -1;

		public bool Debout;

		public float PiedY;

		public bool AuSol;
	}

	private sealed class Rendu
	{
		public bool Faite;

		public int Version = -1;

		public int PasDuFeu = -1;

		public int Signature;

		public long Image = -1L;

		public long Vue;

		public readonly Dictionary<string, IGameTexture> Couches = new Dictionary<string, IGameTexture>();

		public readonly Dictionary<string, byte[]> Pixels = new Dictionary<string, byte[]>();

		public readonly List<Decor> Decors = new List<Decor>();

		public IGameTexture? Fentes;

		public string? CoucheHaute;

		public int[]? Hauts;

		public void Liberer()
		{
			foreach (IGameTexture value in Couches.Values)
			{
				value.Dispose();
			}
			Couches.Clear();
			Pixels.Clear();
			Decors.Clear();
			Fentes?.Dispose();
			Fentes = null;
			CoucheHaute = null;
			Hauts = null;
		}
	}

	private sealed class PlancheLue
	{
		public volatile byte[]? Rgba;

		public int L;

		public int H;

		public volatile bool ParLaCarte;

		public long Vue;
	}

	internal readonly record struct Zone(float Cx, float Cy, CasseEtat Etat, float T, int Graine);

	internal sealed class SillonVu
	{
		public float X0;

		public float Y0;

		public float X1;

		public float Y1;

		public float K;

		public (float Cos, float Sin, float Longueur) Arc;

		public float Demi = 40f;

		public long Debut;

		public long Fin;

		public CasseTrace Genre;

		public int RevelerMs;

		public byte Courbe;

		public IGameTexture? Froid;

		public IGameTexture? Chaud;

		public int Gauche;

		public int Haut;

		public byte[]? Masque;

		public int MasqueL;

		public int MasqueH;

		public bool Vide;

		public IGameTexture? FroidDessus;

		public IGameTexture? ChaudDessus;

		public short[]? Dresses;

		public List<(int Objet, int Partie)> Porteurs = new List<(int, int)>();

		public byte[]? OctetsChauds;

		public int PartisDessus = -1;

		public int VersionDessus = int.MinValue;

		public long DessusA;

		public SillonVu? Suivant;

		public void Liberer()
		{
			Froid?.Dispose();
			Chaud?.Dispose();
			FroidDessus?.Dispose();
			ChaudDessus?.Dispose();
			Froid = null;
			Chaud = null;
			FroidDessus = null;
			ChaudDessus = null;
			Masque = null;
			Dresses = null;
			OctetsChauds = null;
		}

		public float Distance(float x, float y)
		{
			if (K != 0f)
			{
				return ArcDuRayon.Distance(X0, Y0, Arc.Cos, Arc.Sin, K, Arc.Longueur, x, y);
			}
			float num = X1 - X0;
			float num2 = Y1 - Y0;
			float num3 = x - X0;
			float num4 = y - Y0;
			float num5 = Math.Clamp((num * num3 + num2 * num4) / MathF.Max(1f, num * num + num2 * num2), 0f, 1f);
			float num6 = x - (X0 + num * num5);
			float num7 = y - (Y0 + num2 * num5);
			return MathF.Sqrt(num6 * num6 + num7 * num7);
		}

		public (float X, float Y) Proche(float x, float y)
		{
			if (K != 0f)
			{
				float item = ArcDuRayon.Projeter(X0, Y0, Arc.Cos, Arc.Sin, K, x, y).Le;
				var (item2, item3, _, _) = ArcDuRayon.Point(X0, Y0, Arc.Cos, Arc.Sin, K, Math.Clamp(item, 0f, Arc.Longueur));
				return (X: item2, Y: item3);
			}
			float num = X1 - X0;
			float num2 = Y1 - Y0;
			float num3 = x - X0;
			float num4 = y - Y0;
			float num5 = Math.Clamp((num * num3 + num2 * num4) / MathF.Max(1f, num * num + num2 * num2), 0f, 1f);
			return (X: X0 + num * num5, Y: Y0 + num2 * num5);
		}
	}

	private const string ImagePiedKi1 = "casse_pied_ki_1.png";

	private const string ImagePiedKi2 = "casse_pied_ki_2.png";

	private const string ImagePiedCorps = "casse_pied_corps.png";

	private const string ImagePiedCorpsGauche = "casse_pied_corps_g.png";

	private const string ImageTroncsSains = "casse_troncs_sains.png";

	private const string ImageTroncsSainsGauche = "casse_troncs_sains_g.png";

	private const int PiedImageX = 47;

	private const int PiedImageY = 109;

	private const int CoupeDuTronc = 64;

	private const int HautDeSouche = 26;

	private static readonly (int X, int Y)[] GermesDeCime = new(int, int)[6]
	{
		(15, 25),
		(37, 9),
		(63, 21),
		(21, 51),
		(53, 53),
		(39, 33)
	};

	private const float GraviteDesCimes = 520f;

	private static readonly List<MorceauDeCime> Cimes = new List<MorceauDeCime>();

	private static readonly List<ArbreQuiBascule> Bascules = new List<ArbreQuiBascule>();

	private static readonly List<TasDeFeuilles> TasPoses = new List<TasDeFeuilles>();

	private static long _cimesA;

	private const int DureeDeBascule = 600;

	private const float AngleDeBascule = 80f;

	private static readonly List<EclairDeChoc> EclairsDesChocs = new List<EclairDeChoc>();

	private const double EclairDuree = 0.15;

	private static CasseDonnees? _donnees;

	private static int _chargement;

	private static readonly Dictionary<Guid, Dictionary<(int Objet, int Partie), EtatPartie>> Etats = new Dictionary<Guid, Dictionary<(int, int), EtatPartie>>();

	private static readonly Dictionary<Guid, int> Versions = new Dictionary<Guid, int>();

	private static readonly Dictionary<(Guid Carte, int Objet), int> VersionsObjets = new Dictionary<(Guid, int), int>();

	private static readonly Dictionary<Guid, long> DernieresVues = new Dictionary<Guid, long>();

	private static string _genereServeur = string.Empty;

	private static bool _desaccordSignale;

	private static readonly object Verrou = new object();

	private static readonly Dictionary<Guid, (int Version, Dictionary<int, List<(int Partie, EtatPartie Etat)>> Objets)> Touches = new Dictionary<Guid, (int, Dictionary<int, List<(int, EtatPartie)>>)>();

	private static readonly List<(long A, CasseCoupPacket Paquet)> Differes = new List<(long, CasseCoupPacket)>();

	private const float PoussiereDesRochers = 0.16f;

	private static readonly List<Particule> Particules = new List<Particule>();

	private static readonly Random Hasard = new Random();

	private static readonly List<Gerbe> Gerbes = new List<Gerbe>();

	private static readonly List<EclatsDeMur> EclatsDesMurs = new List<EclatsDeMur>();

	private static readonly Dictionary<(Guid Carte, int Objet, int Partie), Barre> Barres = new Dictionary<(Guid, int, int), Barre>();

	private static readonly List<FlammeAuMonde> FlammesVues = new List<FlammeAuMonde>();

	private static readonly List<Fumee> FumeesVues = new List<Fumee>();

	private static readonly List<Etincelles> EtincellesVues = new List<Etincelles>();

	private static readonly List<Poussiere> PoussieresVues = new List<Poussiere>();

	private static readonly List<(float X, float Y, double T, float R)> FlashsVus = new List<(float, float, double, float)>();

	private static readonly List<FlammeAuMonde> FlammesDesCoups = new List<FlammeAuMonde>();

	private static readonly List<Fumee> FumeesDesCoups = new List<Fumee>();

	private static readonly List<Etincelles> EtincellesDesCoups = new List<Etincelles>();

	private static readonly Dictionary<(Guid Carte, int Objet), (float Gauche, float Droite, float Haut, float Bas)> Contours = new Dictionary<(Guid, int), (float, float, float, float)>();

	private static Bande?[]? _bandes;

	private static readonly (float H, float W, float R, float G, float B, float A, float Pointe)[] CouchesDuFeu = new(float, float, float, float, float, float, float)[3]
	{
		(1.22f, 1.6f, 255f, 178f, 72f, 90f, 0f),
		(1f, 1f, 236f, 92f, 18f, 230f, 70f),
		(0.58f, 0.55f, 255f, 226f, 128f, 240f, 90f)
	};

	private static IGameTexture? _haloChaud;

	private static IGameTexture? _haloBlanc;

	private static IGameTexture?[]? _bouffeesFeu;

	private static IGameTexture?[]? _bouffeesFroid;

	private static IGameTexture?[]? _bouffeesPoussiere;

	private static readonly Dictionary<string, Braises?> BraisesDes = new Dictionary<string, Braises>(StringComparer.OrdinalIgnoreCase);

	private const string ImageArbreCalcine = "casse_arbre_calcine.png";

	private const string ImageTroncsCalcines = "casse_troncs_calcines.png";

	private const string ImageFeuillesCalcinees = "casse_feuilles_calcinees.png";

	private const string ImageTerreBrulee = "casse_terre_brulee.png";

	private static readonly MotifDeForet[] Motifs = new MotifDeForet[1]
	{
		new MotifDeForet("11.png", 391, 19, 80, 93, (X: 40, Y: 90), new PieceDeForet[3]
		{
			new PieceDeForet(12, 0, 3, 4, new(int, int)[1] { (391, 19) }),
			new PieceDeForet(15, 0, 3, 3, new(int, int)[2]
			{
				(534, 1),
				(487, 51)
			}, 96),
			new PieceDeForet(18, 0, 4, 3, new(int, int)[1] { (599, 1) })
		})
	};

	private const float PiedProche = 16f;

	private static readonly Dictionary<(Guid Carte, int Objet), Foret?> Forets = new Dictionary<(Guid, int), Foret>();

	private static readonly Dictionary<string, byte[]> ArbresDesMotifs = new Dictionary<string, byte[]>();

	private static MapInstance? _derniereCarte;

	private static readonly Dictionary<Guid, (long Image, int Signature)> SignaturesDesTombes = new Dictionary<Guid, (long, int)>();

	private static readonly List<Morceau> Morceaux = new List<Morceau>();

	private const float Gravite = 600f;

	private const int RebondMs = 160;

	private const int EffacementMs = 420;

	private const int LueursMax = 48;

	private static readonly Color CouleurDuFeu = new Color(255, 255, 150, 62);

	private static readonly List<DecorRange> sDebout = new List<DecorRange>();

	private static int sProchainDebout;

	private static int sDerniereRangee;

	private static readonly Dictionary<(Guid Carte, int Objet), Rendu> Rendus = new Dictionary<(Guid, int), Rendu>();

	private static long _prochainePurge;

	private static long _image;

	private static int _refaitesCetteImage;

	private const int RefaitesParImage = 3;

	private const int RefaitesApresUnCoup = 40;

	private static long _presseJusqua;

	private static long _fabriquees;

	private static readonly Dictionary<string, PlancheLue> PlanchesLues = new Dictionary<string, PlancheLue>();

	private static readonly Dictionary<(string Planche, int X, int Y), byte[]?> Tuiles = new Dictionary<(string, int, int), byte[]>();

	internal const float CimeRx = 46f;

	internal const float CimeRy = 48f;

	internal const float CimeHaut = 50f;

	private static readonly Dictionary<(Guid Carte, int Objet), bool> Ronds = new Dictionary<(Guid, int), bool>();

	private static readonly Dictionary<Guid, (int Version, long Tranche, List<Zone> Zones, bool Completes)> ZonesGardees = new Dictionary<Guid, (int, long, List<Zone>, bool)>();

	private static readonly List<(int Partie, EtatPartie Etat)> AucunePartie = new List<(int, EtatPartie)>();

	internal static readonly (byte R, byte G, byte B)[] RampeCharbon = new(byte, byte, byte)[7]
	{
		(20, 17, 17),
		(33, 28, 27),
		(48, 41, 38),
		(65, 56, 51),
		(84, 74, 67),
		(106, 95, 86),
		(132, 120, 109)
	};

	internal static readonly (byte R, byte G, byte B)[] RampeCendre = new(byte, byte, byte)[6]
	{
		(78, 72, 66),
		(98, 91, 84),
		(118, 110, 101),
		(138, 129, 118),
		(158, 148, 135),
		(176, 166, 152)
	};

	internal static readonly (byte R, byte G, byte B)[] RampeTerre = new(byte, byte, byte)[4]
	{
		(38, 29, 23),
		(51, 39, 30),
		(66, 50, 37),
		(82, 63, 45)
	};

	internal static readonly (byte R, byte G, byte B)[] RampePaille = new(byte, byte, byte)[5]
	{
		(92, 80, 40),
		(116, 99, 47),
		(140, 119, 56),
		(163, 139, 67),
		(186, 161, 84)
	};

	internal static readonly (byte R, byte G, byte B)[] RampeSec = new(byte, byte, byte)[6]
	{
		(46, 50, 22),
		(64, 68, 26),
		(86, 88, 32),
		(110, 108, 40),
		(134, 128, 50),
		(158, 148, 64)
	};

	internal static readonly (byte R, byte G, byte B)[] RampeAutomne = new(byte, byte, byte)[7]
	{
		(60, 30, 14),
		(88, 42, 16),
		(120, 58, 18),
		(152, 80, 24),
		(184, 106, 34),
		(210, 140, 54),
		(230, 176, 86)
	};

	private const float DemiTranchee = 40f;

	private static readonly Dictionary<Guid, List<SillonVu>> SillonsVus = new Dictionary<Guid, List<SillonVu>>();

	private static long _dessusRefaitA = -1L;

	private const float AtterrissageTemps = 0.45f;

	private const float TraceFonduMs = 20000f;

	private static readonly Dictionary<Guid, (long Image, int Propre, int Version)> VoisinagesVus = new Dictionary<Guid, (long, int, int)>();

	private static CasseOptions Reglages => Options.Instance.Casse;

	private static CasseDonnees? Donnees
	{
		get
		{
			if (_donnees != null || Interlocked.Exchange(ref _chargement, 1) == 1)
			{
				return _donnees;
			}
			Task.Run(delegate
			{
				try
				{
					_donnees = CasseDonnees.Lire(Path.Combine(ClientConfiguration.ResourcesDirectory, Reglages.Fichier));
				}
				catch (Exception exception)
				{
					ApplicationContext.Context.Value?.Logger.LogWarning(exception, "Casse des decors : casse.json illisible");
				}
			});
			return null;
		}
	}

	private static double DureeDuFeu => (double)Math.Max(1000, Reglages.FeuDureeMs) / 1000.0;

	private static IGameTexture? HaloChaud => Halo(ref _haloChaud, "chaud", byte.MaxValue, 190, 100);

	private static IGameTexture? HaloBlanc => Halo(ref _haloBlanc, "blanc", 225, 245, byte.MaxValue);

	private static IGameTexture?[] BouffeesFeu => _bouffeesFeu ?? (_bouffeesFeu = Bouffees(new(byte, byte, byte)[3]
	{
		(104, 96, 92),
		(74, 68, 66),
		(52, 47, 46)
	}, 5, "feu"));

	private static IGameTexture?[] BouffeesFroid => _bouffeesFroid ?? (_bouffeesFroid = Bouffees(new(byte, byte, byte)[3]
	{
		(176, 174, 176),
		(144, 142, 146),
		(116, 114, 120)
	}, 9, "froid"));

	private static IGameTexture?[] BouffeesPoussiere => _bouffeesPoussiere ?? (_bouffeesPoussiere = Bouffees(new(byte, byte, byte)[3]
	{
		(228, 216, 192),
		(202, 188, 162),
		(170, 154, 130)
	}, 13, "poussiere"));

	private static int Case => Options.Instance.Map.TileWidth;

	private static int BudgetDeLImage
	{
		get
		{
			if (Timing.Global.Milliseconds >= _presseJusqua)
			{
				return 3;
			}
			return 40;
		}
	}

	private static bool AvecDessus => Options.Instance.Map.Layers.LowerLayers.Count > 1;

	private static void PoserLePied(List<Decor> decors, string basse, bool range, int x, int y, sbyte tombe)
	{
		if (tombe == 0)
		{
			string image = ((((x / 47 + y / 48) & 1) == 0) ? "casse_pied_ki_1.png" : "casse_pied_ki_2.png");
			AjouterDecor(decors, image, basse, x - 47, y - 109, 0f, 0f, 96f, 128f, -1, range, y);
			return;
		}
		if (tombe > 0)
		{
			AjouterDecor(decors, "casse_troncs_sains.png", basse, x + 12 - 6, y - 1 - 50, 0f, 0f, 96f, 64f, -1, range, y);
		}
		else
		{
			AjouterDecor(decors, "casse_troncs_sains_g.png", basse, x - 12 - 89, y - 1 - 50, 0f, 0f, 96f, 64f, -1, range, y);
		}
		AjouterDecor(decors, (tombe > 0) ? "casse_pied_corps.png" : "casse_pied_corps_g.png", basse, x - 47, y - 109, 0f, 0f, 96f, 128f, -1, range, y);
	}

	private static bool EmporterArbre(MapInstance carte, CasseCarte reperee, CasseCoupPacket paquet, CasseObjet o, long maintenant)
	{
		GameRenderer renderer = Graphics.Renderer;
		bool flag = renderer == null;
		bool flag2 = flag;
		if (!flag2)
		{
			CasseGenre genre = o.Genre;
			bool flag3 = (uint)(genre - 3) <= 1u;
			flag2 = !flag3;
		}
		byte[] arbre;
		int L;
		int H;
		int boiteX;
		int boiteY;
		List<(int X, int Y)> devant;
		Random hasard;
		if (!flag2 && paquet.Partie >= 0 && paquet.Partie < o.Parties.Length)
		{
			Foret foret = ForetGardee(carte, paquet.Objet, o);
			if (foret != null)
			{
				(int X, int Y) tuple = o.Parties[paquet.Partie];
				int item = tuple.X;
				int item2 = tuple.Y;
				(int, int)? tuple2 = foret.PiedDe(item, item2);
				if (tuple2.HasValue)
				{
					(int, int) valueOrDefault = tuple2.GetValueOrDefault();
					MotifDeForet motif = foret.Motif;
					arbre = foret.Arbre;
					L = motif.ArbreL;
					H = motif.ArbreH;
					if (arbre.Length < L * H * 4)
					{
						return false;
					}
					boiteX = valueOrDefault.Item1 - motif.Pied.X;
					boiteY = valueOrDefault.Item2 - motif.Pied.Y;
					float num = carte.X + boiteX;
					float num2 = carte.Y + boiteY;
					FloatRect worldViewport = Graphics.WorldViewport;
					if (num + (float)L < worldViewport.Left - 200f || num > worldViewport.Right + 200f || num2 + (float)H < worldViewport.Top - 200f || num2 > worldViewport.Bottom + 200f || Cimes.Count > 160)
					{
						return true;
					}
					int num3 = Array.FindIndex(foret.Arbres, ((int X, int Y) a) => a.X == boiteX && a.Y == boiteY);
					devant = new List<(int, int)>();
					for (int num4 = 0; num4 < num3; num4++)
					{
						var (num5, num6) = foret.Arbres[num4];
						if (Math.Abs(num5 - boiteX) < L && Math.Abs(num6 - boiteY) < H && (num4 >= foret.Etat.Length || foret.Etat[num4] < 2f))
						{
							devant.Add((num5, num6));
						}
					}
					List<int>[] array = new List<int>[GermesDeCime.Length];
					for (int num7 = 0; num7 < array.Length; num7++)
					{
						array[num7] = new List<int>();
					}
					for (int num8 = 0; num8 < H; num8++)
					{
						for (int num9 = 0; num9 < L; num9++)
						{
							int num10 = (num8 * L + num9) * 4;
							if (arbre[num10 + 3] < 128 || (arbre[num10 + 1] <= arbre[num10] + 10 && num8 > 64) || Cache(num9, num8))
							{
								continue;
							}
							int num11 = 0;
							int num12 = int.MaxValue;
							for (int num13 = 0; num13 < GermesDeCime.Length; num13++)
							{
								int num14 = num9 - GermesDeCime[num13].X;
								int num15 = num8 - GermesDeCime[num13].Y;
								int num16 = num14 * num14 + num15 * num15;
								if (num16 < num12)
								{
									num12 = num16;
									num11 = num13;
								}
							}
							array[num11].Add(num8 * L + num9);
						}
					}
					bool flag4 = paquet.Couche == 0;
					hasard = new Random(paquet.Graine);
					List<(IGameTexture Vert, IGameTexture? Brule, int L, int H, float Cx, float Cy)> pieces = new List<(IGameTexture, IGameTexture, int, int, float, float)>();
					List<int>[] array2 = array;
					foreach (List<int> list in array2)
					{
						if (list.Count < 12)
						{
							continue;
						}
						int num18 = int.MaxValue;
						int num19 = int.MaxValue;
						int num20 = 0;
						int num21 = 0;
						foreach (int item17 in list)
						{
							num18 = Math.Min(num18, item17 % L);
							num20 = Math.Max(num20, item17 % L);
							num19 = Math.Min(num19, item17 / L);
							num21 = Math.Max(num21, item17 / L);
						}
						int num22 = num20 - num18 + 1;
						int num23 = num21 - num19 + 1;
						byte[] array3 = new byte[num22 * num23 * 4];
						byte[] array4 = (flag4 ? new byte[num22 * num23 * 4] : null);
						foreach (int item18 in list)
						{
							int num24 = item18 % L;
							int num25 = item18 / L;
							int num26 = ((num25 - num19) * num22 + num24 - num18) * 4;
							Array.Copy(arbre, item18 * 4, array3, num26, 4);
							if (array4 != null)
							{
								float num27 = Math.Clamp(((0.3f * (float)(int)arbre[item18 * 4] + 0.59f * (float)(int)arbre[item18 * 4 + 1] + 0.11f * (float)(int)arbre[item18 * 4 + 2]) / 255f - 0.42f) * 1.8f + 0.42f, 0f, 1f);
								uint num28 = Hache(valueOrDefault.Item1 + num24, valueOrDefault.Item2 + num25, 613) % 100;
								ref byte reference = ref array4[num26];
								ref byte reference2 = ref array4[num26 + 1];
								ref byte reference3 = ref array4[num26 + 2];
								switch (num28)
								{
								default:
								{
									byte b = (byte)(18f + 62f * num27);
									byte b2 = (byte)(13f + 40f * num27);
									byte b3 = (byte)(12f + 28f * num27);
									reference = b;
									reference2 = b2;
									reference3 = b3;
									break;
								}
								case 2u:
								case 3u:
									reference = 170;
									reference2 = 52;
									reference3 = 14;
									break;
								case 0u:
								case 1u:
									reference = byte.MaxValue;
									reference2 = 150;
									reference3 = 50;
									break;
								}
								array4[num26 + 3] = byte.MaxValue;
							}
						}
						string text = $"casse/cime/{reperee.Id}/{paquet.Objet}/{paquet.Partie}/{++_fabriquees}";
						IGameTexture gameTexture = renderer.CreateTextureFromPixels(text, num22, num23, array3);
						if (gameTexture != null)
						{
							IGameTexture item3 = ((array4 == null) ? null : renderer.CreateTextureFromPixels(text + "/brule", num22, num23, array4));
							pieces.Add((gameTexture, item3, num22, num23, (float)num18 + (float)num22 / 2f, (float)num19 + (float)num23 / 2f));
						}
					}
					if (pieces.Count == 0)
					{
						return true;
					}
					float num29 = carte.X + valueOrDefault.Item1;
					float num30 = carte.Y + valueOrDefault.Item2;
					double num31 = Secondes(maintenant);
					if (!flag4)
					{
						byte[] array5 = new byte[L * H * 4];
						List<int>[] array6 = array;
						foreach (List<int> list2 in array6)
						{
							foreach (int item19 in list2)
							{
								Array.Copy(arbre, item19 * 4, array5, item19 * 4, 4);
							}
						}
						ArbreQuiBascule arbreQuiBascule = new ArbreQuiBascule
						{
							Bloc = renderer.CreateTextureFromPixels($"casse/cime/{reperee.Id}/{paquet.Objet}/{paquet.Partie}/bloc/{++_fabriquees}", L, H, array5),
							L = L,
							H = H,
							Sens = ((paquet.Couche > 0) ? 1 : (-1)),
							X0 = num,
							Y0 = num2,
							PivotX = num29,
							PivotY = num30 - 26f,
							Sol = num30 + 3f,
							Depart = maintenant + 90,
							Carte = carte.Id,
							Objet = paquet.Objet,
							Partie = paquet.Partie,
							Graine = paquet.Graine
						};
						foreach (var (item4, _, item5, item6, item7, item8) in pieces)
						{
							arbreQuiBascule.Cellules.Add((item4, item5, item6, item7, item8));
						}
						Bascules.Add(arbreQuiBascule);
						Gerbes.Add(new Gerbe(GenreDeGerbe.Eclats, num29, num30 - 26f, num31, paquet.Graine + 2, 196, 150, 96));
						return true;
					}
					float dx = paquet.Dx;
					float dy = paquet.Dy;
					bool flag5 = MathF.Abs(dx) + MathF.Abs(dy) < 0.01f;
					List<int> list3 = (from k in Enumerable.Range(0, pieces.Count)
						orderby (pieces[k].Cx - (float)motif.Pied.X) * dx + (pieces[k].Cy - (float)motif.Pied.Y) * dy
						select k).ToList();
					int num33 = list3[list3.Count - 1];
					for (int num34 = 0; num34 < list3.Count; num34++)
					{
						int num35 = list3[num34];
						(IGameTexture Vert, IGameTexture? Brule, int L, int H, float Cx, float Cy) tuple5 = pieces[num35];
						IGameTexture item9 = tuple5.Vert;
						IGameTexture item10 = tuple5.Brule;
						int item11 = tuple5.L;
						int item12 = tuple5.H;
						float item13 = tuple5.Cx;
						float item14 = tuple5.Cy;
						bool flag6 = num35 == num33;
						float num36 = (flag6 ? Entre(50f, 175f) : Entre(250f, 400f));
						float num37 = (flag6 ? Entre(-70f, 70f) : Entre(-90f, 90f));
						float num38 = item13 - (float)motif.Pied.X;
						float num39 = item14 - 33f;
						long num40 = maintenant + 55 + 6 + num34 * 12;
						Cimes.Add(new MorceauDeCime
						{
							Vert = item9,
							Brule = item10,
							L = item11,
							H = item12,
							ReposX = num + item13,
							ReposY = num2 + item14,
							ReculX = dx * 2f,
							ReculY = dy * 2f,
							X = num + item13 + dx * 2f,
							Y = num30 + dy * 2f,
							Z = (float)motif.Pied.Y - item14,
							Vx = dx * num36 - dy * num37 + num38 * (flag6 ? 0.4f : 1.4f) + (flag5 ? (num38 * 3f) : 0f),
							Vy = dy * num36 + dx * num37 * 0.6f + (flag5 ? (num39 * 2f) : 0f),
							Vz = (flag6 ? Entre(40f, 90f) : Entre(60f, 150f)),
							Tour = Entre(230f, 570f) * (float)((hasard.Next(2) != 0) ? 1 : (-1)),
							Depart = num40,
							FinA = num40 + (long)Entre(230f, 360f),
							Ki = true,
							Retombe = flag6,
							Carte = carte.Id,
							Objet = paquet.Objet,
							Partie = paquet.Partie,
							Graine = paquet.Graine + num35
						});
					}
					Gerbes.Add(new Gerbe(GenreDeGerbe.Eclats, num29, num30 - 26f, num31, paquet.Graine + 2, 96, 62, 34));
					Gerbes.Add(new Gerbe(GenreDeGerbe.Feuilles, num29, num30 - 50f, num31, paquet.Graine, 0, 0, 0));
					EtincellesDesCoups.Add(new Etincelles(num29, num30 - 26f - 2f, paquet.Graine + 4, 9, num31, num31 + 0.7));
					return true;
				}
				return false;
			}
		}
		return false;
		bool Cache(int sx, int sy)
		{
			foreach (var item20 in devant)
			{
				int item15 = item20.X;
				int item16 = item20.Y;
				int num41 = boiteX + sx - item15;
				int num42 = boiteY + sy - item16;
				if (num41 >= 0 && num42 >= 0 && num41 < L && num42 < H && arbre[(num42 * L + num41) * 4 + 3] >= 128)
				{
					return true;
				}
			}
			return false;
		}
		float Entre(float a, float num41)
		{
			return a + (float)hasard.NextDouble() * (num41 - a);
		}
	}

	private static void DessinerCime(IGameTexture texture, int l, int h, float cx, float cy, float angle, Color couleur)
	{
		Graphics.DrawGameTexture(texture, new FloatRect(0f, 0f, l, h), new FloatRect(MathF.Round(cx - (float)l / 2f), MathF.Round(cy - (float)h / 2f), l, h), couleur, null, GameBlendModes.None, null, angle);
	}

	private static (float X, float Y) Tourner(float x, float y, float pivotX, float pivotY, float degres)
	{
		float x2 = degres * (float)Math.PI / 180f;
		float num = MathF.Cos(x2);
		float num2 = MathF.Sin(x2);
		float num3 = x - pivotX;
		float num4 = y - pivotY;
		return (X: pivotX + num3 * num - num4 * num2, Y: pivotY + num3 * num2 + num4 * num);
	}

	private static void DessinerArbres(long maintenant)
	{
		float num = Math.Clamp((float)(maintenant - _cimesA) / 1000f, 0f, 0.05f);
		_cimesA = maintenant;
		if (Bascules.Count == 0 && Cimes.Count == 0)
		{
			return;
		}
		double num2 = Secondes(maintenant);
		int num3 = Bascules.Count - 1;
		while (num3 >= 0)
		{
			ArbreQuiBascule arbreQuiBascule = Bascules[num3];
			Random hasard;
			if (arbreQuiBascule.Bloc == null)
			{
				Bascules.RemoveAt(num3);
			}
			else
			{
				long num4 = maintenant - arbreQuiBascule.Depart;
				if (num4 < 600)
				{
					float num5 = ((num4 <= 0) ? 0f : ((float)arbreQuiBascule.Sens * 80f * MathF.Pow((float)num4 / 600f, 2.2f)));
					float num6 = ((num4 <= 0) ? ((float)arbreQuiBascule.Sens * 3f * Math.Clamp((float)(num4 + 90) / 90f, 0f, 1f)) : ((float)arbreQuiBascule.Sens * 3f));
					var (cx, cy) = Tourner(arbreQuiBascule.X0 + (float)arbreQuiBascule.L / 2f + num6, arbreQuiBascule.Y0 + (float)arbreQuiBascule.H / 2f, arbreQuiBascule.PivotX + num6, arbreQuiBascule.PivotY, num5);
					DessinerCime(arbreQuiBascule.Bloc, arbreQuiBascule.L, arbreQuiBascule.H, cx, cy, num5, Color.White);
				}
				else
				{
					hasard = new Random(arbreQuiBascule.Graine);
					float num7 = (float)arbreQuiBascule.Sens * 80f;
					foreach (var (vert, num8, h, num9, num10) in arbreQuiBascule.Cellules)
					{
						var (num11, num12) = Tourner(arbreQuiBascule.X0 + num9, arbreQuiBascule.Y0 + num10, arbreQuiBascule.PivotX, arbreQuiBascule.PivotY, num7);
						Cimes.Add(new MorceauDeCime
						{
							Vert = vert,
							L = num8,
							H = h,
							X = num11,
							Y = MathF.Max(arbreQuiBascule.Sol, num12),
							Z = MathF.Max(0f, arbreQuiBascule.Sol - num12),
							Vx = (float)arbreQuiBascule.Sens * Entre(30f, 100f) + (num11 - arbreQuiBascule.PivotX - (float)arbreQuiBascule.Sens * 50f) * 0.7f,
							Vy = Entre(-22f, 30f),
							Vz = Entre(40f, 110f),
							Angle = num7,
							Tour = Entre(-125f, 125f),
							Depart = maintenant,
							Carte = arbreQuiBascule.Carte,
							Objet = arbreQuiBascule.Objet,
							Partie = arbreQuiBascule.Partie,
							Graine = arbreQuiBascule.Graine + num8
						});
					}
					for (int i = 0; i < 3; i++)
					{
						Gerbes.Add(new Gerbe(GenreDeGerbe.Feuilles, arbreQuiBascule.PivotX + (float)(arbreQuiBascule.Sens * (30 + i * 24)), arbreQuiBascule.Sol - 12f, num2, arbreQuiBascule.Graine + i * 3, 0, 0, 0));
					}
					Gerbes.Add(new Gerbe(GenreDeGerbe.Poudre, arbreQuiBascule.PivotX + (float)(arbreQuiBascule.Sens * 54), arbreQuiBascule.Sol, num2, arbreQuiBascule.Graine + 11, 0, 0, 0));
					Gerbes.Add(new Gerbe(GenreDeGerbe.Poudre, arbreQuiBascule.PivotX + (float)(arbreQuiBascule.Sens * 22), arbreQuiBascule.Sol, num2, arbreQuiBascule.Graine + 12, 0, 0, 0));
					Player me = Globals.Me;
					if (me != null)
					{
						MapInstance mapInstance = MapInstance.Get(me.MapId);
						if (mapInstance != null && MathF.Abs((float)(mapInstance.X + me.X * Case) - arbreQuiBascule.PivotX) < 700f && MathF.Abs((float)(mapInstance.Y + me.Y * Case) - arbreQuiBascule.PivotY) < 500f)
						{
							CombatFeedback.StartShake(2.5f, 180L);
						}
					}
					arbreQuiBascule.Bloc.Dispose();
					Bascules.RemoveAt(num3);
				}
			}
			num3--;
			float Entre(float a, float z)
			{
				return a + (float)hasard.NextDouble() * (z - a);
			}
		}
		for (int num13 = Cimes.Count - 1; num13 >= 0; num13--)
		{
			MorceauDeCime morceauDeCime = Cimes[num13];
			if (morceauDeCime.Vert == null)
			{
				Cimes.RemoveAt(num13);
			}
			else if (maintenant < morceauDeCime.Depart)
			{
				DessinerCime(morceauDeCime.Vert, morceauDeCime.L, morceauDeCime.H, morceauDeCime.ReposX + morceauDeCime.ReculX, morceauDeCime.ReposY + morceauDeCime.ReculY, 0f, Color.White);
			}
			else
			{
				float num14 = 1f;
				if (morceauDeCime.PoseA > 0)
				{
					num14 = 1f - (float)(maintenant - morceauDeCime.PoseA) / 360f;
					if (!morceauDeCime.Defait && maintenant - morceauDeCime.PoseA > 60)
					{
						morceauDeCime.Defait = true;
						Gerbes.Add(new Gerbe(GenreDeGerbe.Feuilles, morceauDeCime.X, morceauDeCime.Y - 6f, num2, morceauDeCime.Graine, 0, 0, 0));
					}
				}
				else if (morceauDeCime.FiniA > 0)
				{
					num14 = 1f - (float)(maintenant - morceauDeCime.FiniA) / 90f;
				}
				else
				{
					morceauDeCime.X += morceauDeCime.Vx * num;
					morceauDeCime.Y += morceauDeCime.Vy * num;
					morceauDeCime.Z += morceauDeCime.Vz * num;
					morceauDeCime.Vz -= 520f * num;
					morceauDeCime.Angle += morceauDeCime.Tour * num;
					if (morceauDeCime.Ki && !morceauDeCime.Retombe && maintenant >= morceauDeCime.FinA)
					{
						morceauDeCime.FiniA = maintenant;
						EtincellesDesCoups.Add(new Etincelles(morceauDeCime.X, morceauDeCime.Y - morceauDeCime.Z, morceauDeCime.Graine, 6, num2, num2 + 0.55));
						FumeesDesCoups.Add(new Fumee(morceauDeCime.X, morceauDeCime.Y - morceauDeCime.Z, morceauDeCime.Graine + 1, 0.3f, num2, num2 + 0.5, num2 + 3.0));
					}
					else if (morceauDeCime.Z <= 0f)
					{
						morceauDeCime.Z = 0f;
						if (morceauDeCime.Ki)
						{
							MapInstance mapInstance2 = MapInstance.Get(morceauDeCime.Carte);
							if (mapInstance2 != null && TasPoses.Count < 80)
							{
								TasPoses.Add(new TasDeFeuilles
								{
									Carte = morceauDeCime.Carte,
									X = morceauDeCime.X - (float)mapInstance2.X,
									Y = morceauDeCime.Y - (float)mapInstance2.Y,
									Ne = maintenant,
									Objet = morceauDeCime.Objet,
									Partie = morceauDeCime.Partie
								});
							}
							Gerbes.Add(new Gerbe(GenreDeGerbe.Poudre, morceauDeCime.X, morceauDeCime.Y, num2, morceauDeCime.Graine, 0, 0, 0));
							EtincellesDesCoups.Add(new Etincelles(morceauDeCime.X, morceauDeCime.Y - 6f, morceauDeCime.Graine + 2, 8, num2, num2 + 1.6));
							FumeesDesCoups.Add(new Fumee(morceauDeCime.X, morceauDeCime.Y - 10f, morceauDeCime.Graine + 3, 0.35f, num2, num2 + 2.4, num2 + 7.0));
							num14 = 0f;
						}
						else if (!morceauDeCime.Rebondi && MathF.Abs(morceauDeCime.Vz) > 90f)
						{
							morceauDeCime.Rebondi = true;
							morceauDeCime.Vz = (0f - morceauDeCime.Vz) * 0.28f;
							morceauDeCime.Vx *= 0.5f;
							morceauDeCime.Vy *= 0.5f;
							morceauDeCime.Tour *= 0.4f;
						}
						else
						{
							morceauDeCime.PoseA = maintenant;
						}
					}
				}
				if (num14 <= 0f)
				{
					morceauDeCime.Vert.Dispose();
					morceauDeCime.Brule?.Dispose();
					Cimes.RemoveAt(num13);
				}
				else
				{
					long num15 = maintenant - morceauDeCime.Depart;
					IGameTexture texture = ((morceauDeCime.Ki && morceauDeCime.Brule != null && num15 >= 170) ? morceauDeCime.Brule : morceauDeCime.Vert);
					Color couleur = ((morceauDeCime.Ki && num15 >= 70 && num15 < 170) ? new Color(255, 200, 150, 90) : Color.White);
					DessinerCime(texture, morceauDeCime.L, morceauDeCime.H, morceauDeCime.X, morceauDeCime.Y - morceauDeCime.Z, morceauDeCime.Angle, Teinte(couleur, num14));
				}
			}
		}
	}

	internal static void DessinerTas(MapInstance carte)
	{
		if (TasPoses.Count == 0)
		{
			return;
		}
		long milliseconds = Timing.Global.Milliseconds;
		long num = (long)Math.Max(30, Reglages.TracesVieSecondes) * 1000L;
		IGameTexture gameTexture = null;
		for (int num2 = TasPoses.Count - 1; num2 >= 0; num2--)
		{
			TasDeFeuilles tasDeFeuilles = TasPoses[num2];
			if (tasDeFeuilles.Carte != carte.Id)
			{
				continue;
			}
			long num3 = milliseconds - tasDeFeuilles.Ne;
			if (num3 <= num + 1500)
			{
				EtatPartie etatPartie = EtatDe(tasDeFeuilles.Carte, tasDeFeuilles.Objet, tasDeFeuilles.Partie);
				if (etatPartie != null && etatPartie.Etat == CasseEtat.Casse)
				{
					if (gameTexture == null)
					{
						gameTexture = GameContentManager.Current.GetTexture(TextureType.Image, "casse_feuilles_calcinees.png");
					}
					if (gameTexture == null)
					{
						break;
					}
					float alpha = Math.Clamp((float)num3 / 80f, 0f, 1f) * Math.Clamp((float)(num + 1500 - num3) / 1500f, 0f, 1f);
					Graphics.DrawGameTexture(gameTexture, new FloatRect(0f, 0f, gameTexture.Width, gameTexture.Height), new FloatRect(MathF.Round((float)carte.X + tasDeFeuilles.X - 32f), MathF.Round((float)carte.Y + tasDeFeuilles.Y - 26f), gameTexture.Width, gameTexture.Height), Teinte(Color.White, alpha));
					continue;
				}
			}
			TasPoses.RemoveAt(num2);
		}
	}

	private static void OublierArbres()
	{
		foreach (MorceauDeCime cime in Cimes)
		{
			cime.Vert?.Dispose();
			cime.Brule?.Dispose();
		}
		foreach (ArbreQuiBascule bascule in Bascules)
		{
			bascule.Bloc?.Dispose();
			foreach (var cellule in bascule.Cellules)
			{
				IGameTexture item = cellule.Texture;
				item.Dispose();
			}
		}
		Cimes.Clear();
		Bascules.Clear();
		TasPoses.Clear();
	}

	private static bool Verdatre(byte r, byte g, byte b)
	{
		if (g > r + 8)
		{
			return g > b;
		}
		return false;
	}

	private static (float X, float Y) Accrocher(MapInstance carte, CasseObjet o, float px, float py, int rayon, bool ecorce)
	{
		(float, float)? tuple = null;
		(float, float)? tuple2 = null;
		float num = float.MaxValue;
		float num2 = float.MaxValue;
		for (int i = -rayon / 2; i <= rayon / 2; i++)
		{
			for (int j = -rayon; j <= rayon; j++)
			{
				float num3 = (float)(j * j) + 9f * (float)i * (float)i;
				if (num3 >= num)
				{
					continue;
				}
				(byte, byte, byte)? tuple3 = CouleurAu(carte, o, px + (float)j, py + (float)i);
				if (!tuple3.HasValue)
				{
					continue;
				}
				(byte, byte, byte) valueOrDefault = tuple3.GetValueOrDefault();
				if (ecorce && Verdatre(valueOrDefault.Item1, valueOrDefault.Item2, valueOrDefault.Item3))
				{
					if (num3 < num2)
					{
						num2 = num3;
						tuple2 = (px + (float)j, py + (float)i);
					}
				}
				else
				{
					num = num3;
					tuple = (px + (float)j, py + (float)i);
				}
			}
		}
		return tuple ?? tuple2 ?? (px, py);
	}

	private static void EffetDeChoc(MapInstance carte, CasseCarte reperee, CasseCoupPacket paquet, CasseEtat avant, int pointsAvant)
	{
		CasseObjet casseObjet = reperee.Objets[paquet.Objet];
		double num = Secondes(Timing.Global.Milliseconds);
		float num2 = (float)carte.X + paquet.X;
		float num3 = (float)carte.Y + paquet.Y;
		int graine = paquet.Graine;
		bool flag = paquet.Coup == CasseCoup.Corps;
		NoterBarre(carte, reperee, paquet, pointsAvant, num);
		EclairsDesChocs.Add(new EclairDeChoc(num2, num3, num, flag ? 7f : 3f));
		if (EclairsDesChocs.Count > 24)
		{
			EclairsDesChocs.RemoveAt(0);
		}
		var (num4, num5, _, num6) = Contour(reperee, paquet.Objet);
		if (EstVegetal(casseObjet.Genre))
		{
			float num8;
			float num9;
			if (paquet.Partie < 0 || paquet.Partie >= casseObjet.Parties.Length)
			{
				float num7 = num3 + 22f;
				num8 = num7;
				num9 = num2;
			}
			else
			{
				float num10 = carte.X + casseObjet.Parties[paquet.Partie].X;
				float num7 = carte.Y + casseObjet.Parties[paquet.Partie].Y;
				num8 = num7;
				num9 = num10;
			}
			Gerbes.Add(new Gerbe(GenreDeGerbe.Eclats, num2, num3, num, graine + 2, 112, 78, 46));
			if (flag && paquet.Etat != CasseEtat.Casse)
			{
				Gerbes.Add(new Gerbe(GenreDeGerbe.Feuilles, num9, num8 - ((casseObjet.Genre == CasseGenre.Buisson) ? 10f : 50f), num, graine, 0, 0, 0));
				Gerbes.Add(new Gerbe(GenreDeGerbe.Feuilles, num9 + 10f, num8 - ((casseObjet.Genre == CasseGenre.Buisson) ? 8f : 38f), num + 0.06, graine + 5, 0, 0, 0));
				Gerbes.Add(new Gerbe(GenreDeGerbe.Poudre, num2, num8 - 2f, num, graine + 3, 0, 0, 0));
				Vent.Secouer(num2, num8, 1.3f);
			}
		}
		else if (casseObjet.Genre == CasseGenre.Poubelle)
		{
			EtincellesDesCoups.Add(new Etincelles(num2, num3, graine, flag ? 10 : 5, num, num + 0.45));
			if (flag)
			{
				FumeesDesCoups.Add(new Fumee(num2, num3 - 3f, graine, 0.3f, num, num + 1.0, num + 4.0));
			}
		}
		else if (avant != CasseEtat.Casse)
		{
			var (r, g, b) = CouleurAu(carte, casseObjet, paquet.X, paquet.Y) ?? (150, 140, 125);
			EclatsDesMurs.Add(new EclatsDeMur(num2, num3, (float)carte.Y + num6 + 2f, r, g, b, graine, flag ? 24 : 6, num, MathF.Min(60f, num5 - num4), carte.Id, paquet.Objet));
			Gerbes.Add(new Gerbe(GenreDeGerbe.Poudre, num2, num3, num, graine, 0, 0, 0));
			if (!flag && casseObjet.Genre != CasseGenre.Bois)
			{
				EtincellesDesCoups.Add(new Etincelles(num2, num3, graine + 1, 4, num, num + 0.3));
			}
		}
		if (avant != CasseEtat.Casse && paquet.Etat == CasseEtat.Casse)
		{
			Tombe(carte, reperee, paquet.Objet, casseObjet, Timing.Global.Milliseconds, num);
			CasseGenre genre = casseObjet.Genre;
			bool flag2 = (uint)(genre - 6) <= 1u;
			if (flag2 || (casseObjet.Rocher && casseObjet.Genre == CasseGenre.Muret))
			{
				EnMorceaux(carte, reperee, paquet.Objet, paquet.X, Timing.Global.Milliseconds);
			}
		}
	}

	private static void DessinerEclairsDesChocs(IGameTexture pixel, double T)
	{
		if (EclairsDesChocs.Count == 0)
		{
			return;
		}
		EclairsDesChocs.RemoveAll((EclairDeChoc e) => T - e.T > 0.15);
		foreach (EclairDeChoc eclairsDesChoc in EclairsDesChocs)
		{
			double num = (T - eclairsDesChoc.T) / 0.15;
			if (!(num < 0.0))
			{
				double num2 = (double)eclairsDesChoc.Rayon * (0.6 + 0.6 * num);
				double alpha = 0.85 * (1.0 - num);
				int num3 = (int)Math.Ceiling(num2 * 0.8);
				for (int num4 = -num3; num4 <= num3; num4++)
				{
					double num5 = Math.Floor(num2 * Math.Sqrt(Math.Max(0.0, 1.0 - (double)(num4 * num4) / (double)(num3 * num3))));
					Point(pixel, (double)eclairsDesChoc.X - num5, eclairsDesChoc.Y + (float)num4, (float)(num5 * 2.0 + 1.0), 1f, alpha, 255, 250, 230);
				}
			}
		}
	}

	internal static CasseCarte? CarteDe(MapInstance carte)
	{
		if (Reglages.Active)
		{
			CasseDonnees donnees = Donnees;
			if (donnees != null && !Desaccord(donnees) && donnees.Cartes.TryGetValue(carte.Id, out var value))
			{
				if (!(value.Signature == carte.CasseSignature))
				{
					return null;
				}
				return value;
			}
		}
		return null;
	}

	private static bool Desaccord(CasseDonnees donnees)
	{
		if (string.IsNullOrEmpty(_genereServeur) || string.Equals(_genereServeur, donnees.Genere, StringComparison.Ordinal))
		{
			return false;
		}
		if (!_desaccordSignale)
		{
			_desaccordSignale = true;
			ApplicationContext.Context.Value?.Logger.LogWarning("Casse des decors coupee : casse.json du client ({Client}) different de celui du serveur ({Serveur})", donnees.Genere, _genereServeur);
		}
		return true;
	}

	internal static int Version(Guid carte)
	{
		lock (Verrou)
		{
			return Versions.GetValueOrDefault(carte);
		}
	}

	internal static int VersionObjet(Guid carte, int objet)
	{
		lock (Verrou)
		{
			return VersionsObjets.GetValueOrDefault((carte, objet));
		}
	}

	private static void Changer(Guid carte, int objet)
	{
		Versions[carte] = Versions.GetValueOrDefault(carte) + 1;
		VersionsObjets[(carte, objet)] = VersionsObjets.GetValueOrDefault((carte, objet)) + 1;
	}

	internal static void NoterVue(Guid carte, long maintenant)
	{
		DernieresVues[carte] = maintenant;
	}

	internal static EtatPartie? EtatDe(Guid carte, int objet, int partie)
	{
		lock (Verrou)
		{
			Dictionary<(int, int), EtatPartie> value;
			EtatPartie value2;
			return (Etats.TryGetValue(carte, out value) && value.TryGetValue((objet, partie), out value2)) ? value2 : null;
		}
	}

	internal static Dictionary<int, List<(int Partie, EtatPartie Etat)>> ObjetsTouches(Guid carte)
	{
		int num = Version(carte);
		if (Touches.TryGetValue(carte, out (int, Dictionary<int, List<(int, EtatPartie)>>) value) && value.Item1 == num)
		{
			return value.Item2;
		}
		Dictionary<int, List<(int, EtatPartie)>> dictionary = LireTouches(carte);
		Touches[carte] = (num, dictionary);
		return dictionary;
	}

	private static Dictionary<int, List<(int Partie, EtatPartie Etat)>> LireTouches(Guid carte)
	{
		Dictionary<int, List<(int, EtatPartie)>> dictionary = new Dictionary<int, List<(int, EtatPartie)>>();
		lock (Verrou)
		{
			if (!Etats.TryGetValue(carte, out Dictionary<(int, int), EtatPartie> value))
			{
				return dictionary;
			}
			foreach (KeyValuePair<(int, int), EtatPartie> item4 in value)
			{
				item4.Deconstruct(out var key, out var value2);
				(int, int) tuple = key;
				int item = tuple.Item1;
				int item2 = tuple.Item2;
				EtatPartie item3 = value2;
				if (!dictionary.TryGetValue(item, out var value3))
				{
					value3 = (dictionary[item] = new List<(int, EtatPartie)>());
				}
				value3.Add((item2, item3));
			}
			return dictionary;
		}
	}

	public static void Recevoir(CasseEtatsPacket paquet)
	{
		long milliseconds = Timing.Global.Milliseconds;
		if (!string.IsNullOrEmpty(paquet.Genere))
		{
			_genereServeur = paquet.Genere;
		}
		long value;
		bool flag = DernieresVues.TryGetValue(paquet.MapId, out value) && milliseconds - value < 3000;
		lock (Verrou)
		{
			Dictionary<(int, int), EtatPartie> dictionary = new Dictionary<(int, int), EtatPartie>();
			Dictionary<(int, int), EtatPartie> dictionary2 = (flag ? Etats.GetValueOrDefault(paquet.MapId) : null);
			CasseEtatPacket[] array = paquet.Etats ?? Array.Empty<CasseEtatPacket>();
			foreach (CasseEtatPacket casseEtatPacket in array)
			{
				EtatPartie value2;
				EtatPartie etatPartie = ((dictionary2 != null && dictionary2.TryGetValue((casseEtatPacket.Objet, casseEtatPacket.Partie), out value2)) ? value2 : new EtatPartie
				{
					Depuis = milliseconds - 60000
				});
				if (etatPartie.Etat != casseEtatPacket.Etat)
				{
					etatPartie.Depuis = ((casseEtatPacket.Etat == CasseEtat.Feu) ? (milliseconds - Math.Max(0, casseEtatPacket.FeuMs)) : (milliseconds - 60000));
				}
				etatPartie.Etat = casseEtatPacket.Etat;
				etatPartie.Points = casseEtatPacket.Points;
				etatPartie.Max = casseEtatPacket.PointsMax;
				etatPartie.Tranche = casseEtatPacket.Tranche;
				etatPartie.Couche = casseEtatPacket.Couche;
				dictionary[(casseEtatPacket.Objet, casseEtatPacket.Partie)] = etatPartie;
			}
			if (Heritage.Actif)
			{
				RecevoirSillons(paquet.MapId, Heritage.Marques.RecevoirLaCarte(paquet.MapId, paquet.Sillons ?? Array.Empty<CasseSillonPacket>()), milliseconds);
			}
			else
			{
				RecevoirSillons(paquet.MapId, paquet.Sillons ?? Array.Empty<CasseSillonPacket>(), milliseconds);
			}
			HashSet<int> hashSet = dictionary.Keys.Select(((int, int) cle) => cle.Item1).ToHashSet();
			if (Etats.TryGetValue(paquet.MapId, out Dictionary<(int, int), EtatPartie> value3))
			{
				hashSet.UnionWith(value3.Keys.Select<(int, int), int>(((int Objet, int Partie) cle) => cle.Objet));
			}
			if (dictionary.Count == 0)
			{
				Etats.Remove(paquet.MapId);
			}
			else
			{
				Etats[paquet.MapId] = dictionary;
			}
			Versions[paquet.MapId] = Versions.GetValueOrDefault(paquet.MapId) + 1;
			foreach (int item in hashSet)
			{
				VersionsObjets[(paquet.MapId, item)] = VersionsObjets.GetValueOrDefault((paquet.MapId, item)) + 1;
			}
		}
		MapInstance.Get(paquet.MapId)?.CasseChangee();
	}

	private static void SortirLesDifferes(long maintenant)
	{
		if (Differes.Count == 0)
		{
			return;
		}
		List<(long, CasseCoupPacket)> list = Differes.Where<(long, CasseCoupPacket)>(((long A, CasseCoupPacket Paquet) d) => d.A <= maintenant).ToList();
		if (list.Count == 0)
		{
			return;
		}
		Differes.RemoveAll(((long A, CasseCoupPacket Paquet) d) => d.A <= maintenant);
		foreach (var item2 in list)
		{
			CasseCoupPacket item = item2.Item2;
			Appliquer(item);
		}
	}

	public static void Recevoir(CasseCoupPacket paquet)
	{
		if (paquet.DelaiMs > 0 && Differes.Count < 256)
		{
			Differes.Add((Timing.Global.Milliseconds + paquet.DelaiMs, paquet));
		}
		else if (Differes.Count > 0 && Differes.Any<(long, CasseCoupPacket)>(((long A, CasseCoupPacket Paquet) d) => d.Paquet.MapId == paquet.MapId && d.Paquet.Objet == paquet.Objet))
		{
			Differes.Add((Differes.Where<(long, CasseCoupPacket)>(((long A, CasseCoupPacket Paquet) d) => d.Paquet.MapId == paquet.MapId && d.Paquet.Objet == paquet.Objet).Max<(long, CasseCoupPacket)>(((long A, CasseCoupPacket Paquet) d) => d.A), paquet));
		}
		else
		{
			Appliquer(paquet);
		}
	}

	private static void Appliquer(CasseCoupPacket paquet)
	{
		long milliseconds = Timing.Global.Milliseconds;
		Presser();
		bool flag = !paquet.Reflet;
		bool flag2 = flag;
		if (flag2)
		{
			CasseCoup coup = paquet.Coup;
			bool flag3 = coup - 6 <= CasseCoup.Rayon;
			flag2 = flag3;
		}
		if (flag2)
		{
			MapInstance mapInstance = MapInstance.Get(paquet.MapId);
			if (mapInstance != null)
			{
				CasseCarte casseCarte = CarteDe(mapInstance);
				if (casseCarte != null && paquet.Objet >= 0 && paquet.Objet < casseCarte.Objets.Length)
				{
					CasseObjet casseObjet = casseCarte.Objets[paquet.Objet];
					MapInstance carte = mapInstance;
					CasseObjet o = casseObjet;
					float x = paquet.X;
					float y = paquet.Y;
					int rayon = ((paquet.Coup == CasseCoup.Corps) ? 14 : 6);
					CasseGenre genre = casseObjet.Genre;
					bool flag3 = (uint)(genre - 2) <= 2u;
					(paquet.X, paquet.Y) = Accrocher(carte, o, x, y, rayon, flag3);
				}
			}
		}
		CasseEtat casseEtat;
		int pointsAvant;
		lock (Verrou)
		{
			if (!Etats.TryGetValue(paquet.MapId, out Dictionary<(int, int), EtatPartie> value))
			{
				value = (Etats[paquet.MapId] = new Dictionary<(int, int), EtatPartie>());
			}
			value.TryGetValue((paquet.Objet, paquet.Partie), out var value2);
			casseEtat = value2?.Etat ?? CasseEtat.Intact;
			pointsAvant = value2?.Points ?? paquet.PointsMax;
			if (paquet.Etat == CasseEtat.Intact)
			{
				value.Remove((paquet.Objet, paquet.Partie));
				if (value.Count == 0)
				{
					Etats.Remove(paquet.MapId);
				}
			}
			else
			{
				if (value2 == null)
				{
					Dictionary<(int, int), EtatPartie> dictionary2 = value;
					(int, int) key = (paquet.Objet, paquet.Partie);
					EtatPartie obj = new EtatPartie
					{
						Depuis = milliseconds
					};
					value2 = obj;
					dictionary2[key] = obj;
				}
				if (value2.Etat != paquet.Etat)
				{
					value2.Depuis = milliseconds;
				}
				value2.Etat = paquet.Etat;
				value2.Points = paquet.Points;
				value2.Max = paquet.PointsMax;
				if (paquet.Coup == CasseCoup.Tranche)
				{
					value2.Tranche = true;
					value2.Couche = paquet.Couche;
				}
				bool flag4 = !paquet.Reflet;
				bool flag5 = flag4;
				if (flag5)
				{
					CasseCoup coup = paquet.Coup;
					bool flag3 = ((coup <= CasseCoup.Rayon || coup - 6 <= CasseCoup.Rayon) ? true : false);
					flag5 = flag3;
				}
				if (flag5 && value2.Coups.Count < 12)
				{
					value2.Coups.Add((paquet.X, paquet.Y, paquet.Graine, paquet.Coup));
				}
			}
			Changer(paquet.MapId, paquet.Objet);
		}
		MapInstance mapInstance2 = MapInstance.Get(paquet.MapId);
		mapInstance2?.CasseChangee();
		if (paquet.Reflet)
		{
			if (casseEtat != CasseEtat.Casse && paquet.Etat == CasseEtat.Casse && mapInstance2 != null)
			{
				CasseCarte casseCarte2 = CarteDe(mapInstance2);
				if (casseCarte2 != null && paquet.Objet >= 0 && paquet.Objet < casseCarte2.Objets.Length)
				{
					Tombe(mapInstance2, casseCarte2, paquet.Objet, casseCarte2.Objets[paquet.Objet], milliseconds, Secondes(milliseconds));
				}
			}
		}
		else
		{
			if (mapInstance2 == null)
			{
				return;
			}
			CasseCarte casseCarte3 = CarteDe(mapInstance2);
			if (casseCarte3 == null || paquet.Objet < 0 || paquet.Objet >= casseCarte3.Objets.Length)
			{
				return;
			}
			Effet(mapInstance2, casseCarte3, paquet, casseEtat, pointsAvant);
			int num;
			switch (paquet.Coup)
			{
			case CasseCoup.Corps:
			case CasseCoup.Balle:
				num = 200;
				break;
			case CasseCoup.Boule:
			case CasseCoup.Rayon:
			{
				bool flag6 = paquet.Etat == CasseEtat.Casse;
				bool flag7 = flag6;
				if (flag7)
				{
					CasseGenre genre = casseCarte3.Objets[paquet.Objet].Genre;
					bool flag3 = (uint)(genre - 6) <= 1u;
					flag7 = flag3;
				}
				num = ((!flag7) ? 280 : 520);
				break;
			}
			case CasseCoup.Feu:
				if (paquet.Etat == CasseEtat.Feu)
				{
					num = 420;
					break;
				}
				goto default;
			default:
				num = 0;
				break;
			}
			int num2 = num;
			if (num2 > 0)
			{
				Figurants.Alerte(paquet.MapId, paquet.X, paquet.Y, num2);
			}
		}
	}

	public static void Oublier()
	{
		lock (Verrou)
		{
			Etats.Clear();
			Versions.Clear();
			VersionsObjets.Clear();
		}
		Differes.Clear();
		Touches.Clear();
		DernieresVues.Clear();
		_genereServeur = string.Empty;
		_desaccordSignale = false;
		OublierRendus();
		OublierEffets();
	}

	internal static string CoucheDesEboulis(CasseObjet o)
	{
		List<string> lowerLayers = Options.Instance.Map.Layers.LowerLayers;
		List<string> all = Options.Instance.Map.Layers.All;
		string text = null;
		(string, int, int)[] cases = o.Cases;
		for (int i = 0; i < cases.Length; i++)
		{
			string item = cases[i].Item1;
			if (text == null || all.IndexOf(item) < all.IndexOf(text))
			{
				text = item;
			}
		}
		if (text != null && lowerLayers.Contains(text))
		{
			return text;
		}
		string text2;
		if (lowerLayers.Count <= 0)
		{
			text2 = text;
			if (text2 == null)
			{
				return string.Empty;
			}
		}
		else
		{
			text2 = lowerLayers[lowerLayers.Count - 1];
		}
		return text2;
	}

	private static void Ebouler(CasseObjet o, Dictionary<string, byte[]> couches, int L, int H, int graine)
	{
		byte[] array = Aplatir(couches, Options.Instance.Map.Layers.All, L, H);
		foreach (byte[] value3 in couches.Values)
		{
			Array.Clear(value3);
		}
		byte[] array2 = new byte[L * H * 4];
		couches[CoucheDesEboulis(o)] = array2;
		int num = H;
		int num2 = -1;
		int num3 = L;
		int num4 = -1;
		for (int i = 0; i < H; i++)
		{
			for (int j = 0; j < L; j++)
			{
				if (array[(i * L + j) * 4 + 3] >= 128)
				{
					num = Math.Min(num, i);
					num2 = Math.Max(num2, i);
					num3 = Math.Min(num3, j);
					num4 = Math.Max(num4, j);
				}
			}
		}
		if (num2 < 0)
		{
			return;
		}
		Random random = new Random(graine ^ 0x2EB0);
		float num5 = ((o.Genre == CasseGenre.Petit) ? 0.45f : 0.3f);
		float num6 = (float)num2 - (float)(num2 - num + 1) * num5;
		float[] array3 = new float[L];
		float num7 = 0f;
		for (int k = 0; k < L; k++)
		{
			num7 = Math.Clamp(num7 + (float)(random.NextDouble() - 0.5) * 2.6f, -4f, 4f);
			array3[k] = num6 + num7;
		}
		int[] proprio = new int[L * H];
		Array.Fill(proprio, -1);
		int[] array4 = new int[L * H];
		Array.Fill(array4, -1);
		List<bool> list = new List<bool>();
		List<bool> list2 = new List<bool>();
		List<int> list3 = new List<int>();
		Stack<int> stack = new Stack<int>();
		int[] array5 = new int[4];
		for (int l = 0; l < L * H; l++)
		{
			if (array4[l] >= 0 || array[l * 4 + 3] == 0 || (float)(l / L) < array3[l % L])
			{
				continue;
			}
			int count = list.Count;
			int num8 = 0;
			bool flag = false;
			array4[l] = count;
			stack.Push(l);
			while (stack.Count > 0)
			{
				int num9 = stack.Pop();
				int num10 = num9 % L;
				int num11 = num9 / L;
				num8 = Math.Max(num8, num11);
				if (num11 > 0 && (float)(num11 - 1) < array3[num10] && array[(num9 - L) * 4 + 3] >= 128)
				{
					flag = true;
				}
				array5[0] = ((num10 > 0) ? (num9 - 1) : (-1));
				array5[1] = ((num10 < L - 1) ? (num9 + 1) : (-1));
				array5[2] = ((num11 > 0) ? (num9 - L) : (-1));
				array5[3] = ((num11 < H - 1) ? (num9 + L) : (-1));
				int[] array6 = array5;
				foreach (int num12 in array6)
				{
					if (num12 >= 0 && array4[num12] < 0 && array[num12 * 4 + 3] != 0 && (float)(num12 / L) >= array3[num12 % L])
					{
						array4[num12] = count;
						stack.Push(num12);
					}
				}
			}
			list.Add(num8 >= num2 - 3 || !flag);
			list2.Add(flag);
			list3.Add(num8);
		}
		int[] array7 = new int[L * H];
		for (int num13 = H - 1; num13 >= 0; num13--)
		{
			for (int n = 0; n < L; n++)
			{
				int num14 = num13 * L + n;
				array7[num14] = ((num13 < H - 1 && array4[num14] >= 0 && array4[num14 + L] == array4[num14]) ? array7[num14 + L] : num13);
			}
		}
		int num15 = Math.Max(4, (int)((float)(num2 - num + 1) * 0.12f));
		List<int> list4 = new List<int>();
		for (int num16 = 0; num16 < L * H; num16++)
		{
			byte b = array[num16 * 4 + 3];
			if (b != 0)
			{
				int num17 = array4[num16];
				if (num17 >= 0 && list[num17] && (!list2[num17] || array7[num16] >= list3[num17] - num15))
				{
					Array.Copy(array, num16 * 4, array2, num16 * 4, 4);
					proprio[num16] = -2;
				}
				else if (b >= 128)
				{
					list4.Add(num16);
				}
			}
		}
		int[] array8 = new int[L];
		Array.Fill(array8, int.MaxValue);
		for (int num18 = 0; num18 < L; num18++)
		{
			for (int num19 = 0; num19 < H; num19++)
			{
				if (proprio[num19 * L + num18] != -2)
				{
					continue;
				}
				array8[num18] = num19;
				if ((float)num19 <= array3[num18] + 2f)
				{
					Eclaircir(array2, (num19 * L + num18) * 4, 1.1f, 18);
					if (num19 + 1 < H && proprio[(num19 + 1) * L + num18] == -2)
					{
						Eclaircir(array2, ((num19 + 1) * L + num18) * 4, 1.04f, 6);
					}
				}
				break;
			}
		}
		if (list4.Count == 0)
		{
			return;
		}
		int num20 = Math.Clamp(list4.Count / 120, 3, 36);
		(int, int)[] array9 = new(int, int)[num20];
		for (int num21 = 0; num21 < num20; num21++)
		{
			int num22 = list4[random.Next(list4.Count)];
			array9[num21] = (num22 % L, num22 / L);
		}
		BlocDeRocher[] blocs = new BlocDeRocher[num20];
		for (int num23 = 0; num23 < num20; num23++)
		{
			blocs[num23] = new BlocDeRocher();
		}
		int[] blocDe = new int[L * H];
		Array.Fill(blocDe, -1);
		foreach (int item in list4)
		{
			int num24 = item % L;
			int num25 = item / L;
			int num26 = 0;
			float num27 = float.MaxValue;
			for (int num28 = 0; num28 < num20; num28++)
			{
				float num29 = num24 - array9[num28].Item1;
				float num30 = num25 - array9[num28].Item2;
				float num31 = num29 * num29 + num30 * num30;
				if (num31 < num27)
				{
					num27 = num31;
					num26 = num28;
				}
			}
			BlocDeRocher blocDeRocher = blocs[num26];
			blocDeRocher.Pixels.Add(item);
			blocDeRocher.X0 = Math.Min(blocDeRocher.X0, num24);
			blocDeRocher.X1 = Math.Max(blocDeRocher.X1, num24);
			blocDeRocher.Y0 = Math.Min(blocDeRocher.Y0, num25);
			blocDeRocher.Y1 = Math.Max(blocDeRocher.Y1, num25);
			blocDe[item] = num26;
		}
		int k2;
		for (int num32 = 0; num32 < 2; num32++)
		{
			List<int> list5 = new List<int>();
			foreach (int item2 in list4)
			{
				k2 = blocDe[item2];
				if (k2 >= 0)
				{
					int num33 = item2 % L;
					int num34 = item2 / L;
					bool flag2 = Meme(num33 - 1, num34);
					bool flag3 = Meme(num33 + 1, num34);
					bool flag4 = Meme(num33, num34 - 1);
					bool flag5 = Meme(num33, num34 + 1);
					int num35 = (flag2 ? 1 : 0) + (flag3 ? 1 : 0) + (flag4 ? 1 : 0) + (flag5 ? 1 : 0);
					if (num35 <= 1 || (num35 == 2 && flag2 != flag3 && flag4 != flag5 && Hache(num33, num34, graine) % 3 != 0))
					{
						list5.Add(item2);
					}
				}
			}
			foreach (int item3 in list5)
			{
				blocs[blocDe[item3]].Pixels.Remove(item3);
				blocDe[item3] = -1;
			}
		}
		BlocDeRocher[] array10 = blocs;
		int key;
		foreach (BlocDeRocher blocDeRocher2 in array10)
		{
			blocDeRocher2.X0 = (blocDeRocher2.Y0 = int.MaxValue);
			key = (blocDeRocher2.X1 = (blocDeRocher2.Y1 = -1));
			foreach (int pixel in blocDeRocher2.Pixels)
			{
				blocDeRocher2.X0 = Math.Min(blocDeRocher2.X0, pixel % L);
				blocDeRocher2.X1 = Math.Max(blocDeRocher2.X1, pixel % L);
				blocDeRocher2.Y0 = Math.Min(blocDeRocher2.Y0, pixel / L);
				blocDeRocher2.Y1 = Math.Max(blocDeRocher2.Y1, pixel / L);
			}
		}
		List<int> list6 = (from num98 in Enumerable.Range(0, num20)
			where blocs[num98].Pixels.Count > 0
			orderby blocs[num98].Pixels.Count descending
			select num98).ToList();
		List<int> list7 = new List<int>();
		for (int num37 = 0; num37 < list6.Count; num37++)
		{
			BlocDeRocher blocDeRocher3 = blocs[list6[num37]];
			if (blocDeRocher3.Pixels.Count >= 10 && (num37 < 2 || !(random.NextDouble() < 0.1599999964237213)))
			{
				list7.Add(list6[num37]);
			}
		}
		float num38 = (float)(num3 + num4) / 2f;
		float num39 = ((o.Genre == CasseGenre.Petit) ? 0.55f : 0.42f);
		List<int> list8 = new List<int>();
		foreach (int item4 in list7)
		{
			BlocDeRocher blocDeRocher4 = blocs[item4];
			float num40 = (float)(blocDeRocher4.X0 + blocDeRocher4.X1) / 2f;
			bool flag6 = blocDeRocher4.Pixels.Count < 90 && random.NextDouble() < 0.4;
			float num41 = num38 + (num40 - num38) * (flag6 ? 1.5f : 1.04f) + (float)(random.NextDouble() - 0.5) * 5f;
			float num42 = (flag6 ? ((float)(num2 + random.Next(-2, 5))) : ((float)num2 - (float)(num2 - blocDeRocher4.Y1) * num39 + (float)random.Next(-2, 3)));
			blocDeRocher4.Dx = Math.Clamp((int)MathF.Round(num41 - num40), 1 - blocDeRocher4.X0, L - 2 - blocDeRocher4.X1);
			blocDeRocher4.Dy = Math.Clamp((int)MathF.Round(num42 - (float)blocDeRocher4.Y1), -blocDeRocher4.Y0, H - 3 - blocDeRocher4.Y1);
			list8.Add(item4);
		}
		foreach (int item5 in list8.OrderByDescending((int num98) => blocs[num98].Y1 + blocs[num98].Dy))
		{
			BlocDeRocher blocDeRocher5 = blocs[item5];
			Dictionary<int, int> dictionary = new Dictionary<int, int>();
			int num43 = blocDeRocher5.X0 + blocDeRocher5.Dx + (blocDeRocher5.X1 - blocDeRocher5.X0) / 4;
			int num44 = blocDeRocher5.X1 + blocDeRocher5.Dx - (blocDeRocher5.X1 - blocDeRocher5.X0) / 4;
			foreach (int pixel2 in blocDeRocher5.Pixels)
			{
				int num45 = pixel2 % L + blocDeRocher5.Dx;
				int num46 = pixel2 / L + blocDeRocher5.Dy;
				if (num45 >= num43 && num45 <= num44)
				{
					dictionary[num45] = (dictionary.TryGetValue(num45, out var value) ? Math.Max(value, num46) : num46);
				}
			}
			int num47 = Math.Min(H - 2, num2 + 1 + (int)(Hache(item5, 3, graine) % 4));
			int num48 = int.MaxValue;
			bool flag7 = false;
			foreach (KeyValuePair<int, int> item6 in dictionary)
			{
				item6.Deconstruct(out key, out var value2);
				int num49 = key;
				int num50 = value2;
				int num51 = Math.Min(array8[num49], num47) - 1 - num50;
				if (num51 < num48 || (num51 == num48 && array8[num49] < num47))
				{
					num48 = num51;
					flag7 = array8[num49] < num47;
				}
			}
			if (num48 > 0)
			{
				blocDeRocher5.Dy += num48 + (flag7 ? Math.Clamp((int)((float)(blocDeRocher5.Y1 - blocDeRocher5.Y0 + 1) * 0.3f), 1, 5) : 0);
				blocDeRocher5.Dy = Math.Min(blocDeRocher5.Dy, H - 3 - blocDeRocher5.Y1);
			}
			foreach (int pixel3 in blocDeRocher5.Pixels)
			{
				int num52 = pixel3 % L + blocDeRocher5.Dx;
				int val = pixel3 / L + blocDeRocher5.Dy;
				array8[num52] = Math.Min(array8[num52], val);
			}
		}
		int[] array11 = new int[L * H];
		Array.Fill(array11, int.MinValue);
		for (int num53 = 0; num53 < L * H; num53++)
		{
			if (proprio[num53] == -2)
			{
				array11[num53] = num2;
			}
		}
		int k3;
		foreach (int item7 in list8.OrderBy((int num98) => blocs[num98].Y1 + blocs[num98].Dy))
		{
			k3 = item7;
			BlocDeRocher blocDeRocher6 = blocs[k3];
			int num54 = blocDeRocher6.Y1 + blocDeRocher6.Dy;
			foreach (int pixel4 in blocDeRocher6.Pixels)
			{
				int num55 = pixel4 % L;
				int num56 = pixel4 / L;
				int num57 = num55 + blocDeRocher6.Dx;
				int num58 = num56 + blocDeRocher6.Dy;
				if (num57 >= 0 && num58 >= 0 && num57 < L && num58 < H && array11[num58 * L + num57] <= num54)
				{
					array11[num58 * L + num57] = num54;
					int num59 = (num58 * L + num57) * 4;
					Array.Copy(array, pixel4 * 4, array2, num59, 4);
					array2[num59 + 3] = byte.MaxValue;
					if (!Sien(num55, num56 - 1))
					{
						bool flag8 = num56 > 0 && array[((num56 - 1) * L + num55) * 4 + 3] >= 128;
						Eclaircir(array2, num59, flag8 ? 1.05f : 1.02f, flag8 ? 5 : 2);
					}
					else if (!Sien(num55, num56 + 1))
					{
						Assombrir(array2, num59, 0.6f);
					}
					else if (!Sien(num55 + 1, num56))
					{
						Assombrir(array2, num59, 0.82f);
					}
					proprio[num58 * L + num57] = k3;
				}
			}
		}
		List<int> list9 = new List<int>();
		int y;
		int x;
		for (y = 1; y < H - 1; y++)
		{
			for (x = 1; x < L - 1; x++)
			{
				if (proprio[y * L + x] == -1 && Cote(-1, 0) && Cote(1, 0) && Cote(0, -1) && Cote(0, 1))
				{
					list9.Add(y * L + x);
				}
			}
		}
		foreach (int item8 in list9)
		{
			int num60 = item8 % L;
			int num61 = item8 / L;
			int num62 = 0;
			int num63 = 0;
			int num64 = 0;
			int num65 = 0;
			for (int num66 = -1; num66 <= 1; num66++)
			{
				for (int num67 = -1; num67 <= 1; num67++)
				{
					if (Pierre(num60 + num67, num61 + num66))
					{
						int num68 = ((num61 + num66) * L + num60 + num67) * 4;
						num62 += array2[num68];
						num63 += array2[num68 + 1];
						num64 += array2[num68 + 2];
						num65++;
					}
				}
			}
			int num69 = item8 * 4;
			num65 = Math.Max(1, num65);
			array2[num69] = (byte)((float)(num62 / num65) * 0.42f);
			array2[num69 + 1] = (byte)((float)(num63 / num65) * 0.42f);
			array2[num69 + 2] = (byte)((float)(num64 / num65) * 0.42f);
			array2[num69 + 3] = byte.MaxValue;
		}
		foreach (int item9 in list9)
		{
			proprio[item9] = -3;
		}
		bool[] array12 = new bool[L * H];
		for (int num70 = 0; num70 < H - 1; num70++)
		{
			for (int num71 = 0; num71 < L; num71++)
			{
				int num72 = proprio[num70 * L + num71];
				if (num72 < 0)
				{
					continue;
				}
				for (int num73 = 0; num73 <= 1 && num71 + num73 < L; num73++)
				{
					int num74 = (num70 + 1) * L + num71 + num73;
					int num75 = proprio[num74];
					bool flag9 = num75 != num72;
					bool flag10 = flag9;
					if (flag10)
					{
						bool flag11 = ((num75 == -3 || num75 == -1) ? true : false);
						flag10 = !flag11;
					}
					if (flag10 && !array12[num74])
					{
						array12[num74] = true;
						Assombrir(array2, num74 * 4, 0.74f);
					}
				}
			}
		}
		int num76 = L;
		int num77 = -1;
		for (int num78 = 0; num78 < L; num78++)
		{
			if (array8[num78] != int.MaxValue)
			{
				num76 = Math.Min(num76, num78);
				num77 = Math.Max(num77, num78);
			}
		}
		int num79 = Math.Clamp(L / 7, 4, 22);
		float num80 = Math.Max(6f, (float)(num77 - num76) / 2f + 6f);
		float num81 = (float)(num76 + num77) / 2f;
		for (int num82 = 0; num82 < num79; num82++)
		{
			float num83 = (float)(random.NextDouble() + random.NextDouble() + random.NextDouble() - 1.5) / 1.5f;
			int num84 = (int)MathF.Round(num81 + num83 * num80 * 1.15f);
			int num85 = num2 - 3 + random.Next(0, 9);
			var (num86, num87) = random.Next(4) switch
			{
				0 => (1, 1), 
				1 => (2, 1), 
				2 => (2, 2), 
				_ => (3, 2), 
			};
			if (num84 < 1 || num85 < 1 || num84 + num86 >= L - 1 || num85 + num87 >= H - 1)
			{
				continue;
			}
			bool flag12 = true;
			for (int num88 = -1; num88 <= num87 && flag12; num88++)
			{
				for (int num89 = -1; num89 <= num86; num89++)
				{
					if (array2[((num85 + num88) * L + num84 + num89) * 4 + 3] != 0)
					{
						flag12 = false;
						break;
					}
				}
			}
			if (!flag12)
			{
				continue;
			}
			int sourceIndex = list4[random.Next(list4.Count)] * 4;
			for (int num90 = 0; num90 < num87; num90++)
			{
				for (int num91 = 0; num91 < num86; num91++)
				{
					int num92 = ((num85 + num90) * L + num84 + num91) * 4;
					Array.Copy(array, sourceIndex, array2, num92, 4);
					array2[num92 + 3] = byte.MaxValue;
					if (num90 == num87 - 1 && num87 > 1)
					{
						Assombrir(array2, num92, 0.62f);
					}
					else if (num90 == 0 && num91 == 0)
					{
						Eclaircir(array2, num92, 1.1f, 8);
					}
					else
					{
						Assombrir(array2, num92, 0.88f);
					}
				}
			}
		}
		for (int num93 = 0; num93 < L; num93++)
		{
			int num94 = -1;
			for (int num95 = H - 1; num95 >= 0; num95--)
			{
				if (array2[(num95 * L + num93) * 4 + 3] >= 128)
				{
					num94 = num95;
					break;
				}
			}
			for (int num96 = 1; num96 <= 2; num96++)
			{
				if (num94 < 0)
				{
					break;
				}
				if (num94 + num96 >= H)
				{
					break;
				}
				int num97 = ((num94 + num96) * L + num93) * 4;
				if (array2[num97 + 3] != 0)
				{
					break;
				}
				array2[num97] = 22;
				array2[num97 + 1] = 18;
				array2[num97 + 2] = 14;
				array2[num97 + 3] = (byte)((num96 == 1) ? 72u : 30u);
			}
		}
		bool Cote(int ox, int oy)
		{
			for (int num98 = 1; num98 <= 3; num98++)
			{
				if (Pierre(x + ox * num98, y + oy * num98))
				{
					return true;
				}
			}
			return false;
		}
		bool Meme(int sx, int sy)
		{
			if (sx >= 0 && sy >= 0 && sx < L && sy < H)
			{
				return blocDe[sy * L + sx] == k2;
			}
			return false;
		}
		bool Pierre(int sx, int sy)
		{
			bool flag13 = sx >= 0 && sy >= 0 && sx < L && sy < H;
			bool flag14 = flag13;
			if (flag14)
			{
				int num98 = proprio[sy * L + sx];
				bool flag15 = ((num98 == -3 || num98 == -1) ? true : false);
				flag14 = !flag15;
			}
			return flag14;
		}
		bool Sien(int sx, int sy)
		{
			if (sx >= 0 && sy >= 0 && sx < L && sy < H)
			{
				return blocDe[sy * L + sx] == k3;
			}
			return false;
		}
	}

	private static void Eclaircir(byte[] px, int i, float facteur, int ajout)
	{
		px[i] = (byte)Math.Min(255f, (float)(int)px[i] * facteur + (float)ajout);
		px[i + 1] = (byte)Math.Min(255f, (float)(int)px[i + 1] * facteur + (float)ajout);
		px[i + 2] = (byte)Math.Min(255f, (float)(int)px[i + 2] * facteur + (float)ajout);
	}

	private static void Assombrir(byte[] px, int i, float facteur)
	{
		px[i] = (byte)((float)(int)px[i] * facteur);
		px[i + 1] = (byte)((float)(int)px[i + 1] * facteur);
		px[i + 2] = (byte)((float)(int)px[i + 2] * facteur);
	}

	private static void OublierEffets()
	{
		Particules.Clear();
		Gerbes.Clear();
		EclatsDesMurs.Clear();
		Barres.Clear();
		EclairsDesChocs.Clear();
		FlammesDesCoups.Clear();
		FumeesDesCoups.Clear();
		Contours.Clear();
		OublierMorceaux();
		OublierArbres();
	}

	private static float Entre(float min, float max)
	{
		return min + (float)Hasard.NextDouble() * (max - min);
	}

	private static Color[] PaletteDe(CasseGenre genre)
	{
		switch (genre)
		{
		case CasseGenre.Bois:
			return new Color[3]
			{
				new Color(255, 116, 78, 48),
				new Color(255, 92, 60, 36),
				new Color(255, 140, 98, 60)
			};
		case CasseGenre.Ville:
		case CasseGenre.Service:
			return new Color[3]
			{
				new Color(255, 196, 190, 180),
				new Color(255, 160, 155, 148),
				new Color(255, 120, 115, 110)
			};
		case CasseGenre.Buisson:
		case CasseGenre.Arbre:
		case CasseGenre.Foret:
			return new Color[3]
			{
				new Color(255, 70, 120, 40),
				new Color(255, 50, 90, 30),
				new Color(255, 96, 64, 34)
			};
		case CasseGenre.Poubelle:
			return new Color[3]
			{
				new Color(255, 60, 96, 40),
				new Color(255, 74, 78, 82),
				new Color(255, 38, 52, 30)
			};
		default:
			return new Color[3]
			{
				new Color(255, 150, 140, 125),
				new Color(255, 110, 100, 90),
				new Color(255, 180, 170, 150)
			};
		}
	}

	private static bool EstVegetal(CasseGenre genre)
	{
		if ((uint)(genre - 2) <= 2u)
		{
			return true;
		}
		return false;
	}

	private static void Effet(MapInstance carte, CasseCarte reperee, CasseCoupPacket paquet, CasseEtat avant, int pointsAvant)
	{
		CasseObjet casseObjet = reperee.Objets[paquet.Objet];
		long milliseconds = Timing.Global.Milliseconds;
		double num = Secondes(milliseconds);
		float num2 = (float)carte.X + paquet.X;
		float num3 = (float)carte.Y + paquet.Y;
		int graine = paquet.Graine;
		switch (paquet.Coup)
		{
		case CasseCoup.Boule:
		case CasseCoup.Rayon:
			if (paquet.Coup != CasseCoup.Boule || !EstVegetal(casseObjet.Genre))
			{
				NoterBarre(carte, reperee, paquet, pointsAvant, num);
			}
			if (EstVegetal(casseObjet.Genre))
			{
				if (paquet.Etat != CasseEtat.Casse)
				{
					float y;
					float x;
					if (paquet.Partie < 0 || paquet.Partie >= casseObjet.Parties.Length)
					{
						float num7 = num3 - 30f;
						y = num7;
						x = num2;
					}
					else
					{
						float num8 = carte.X + casseObjet.Parties[paquet.Partie].X;
						float num7 = (float)(carte.Y + casseObjet.Parties[paquet.Partie].Y) - ((casseObjet.Genre == CasseGenre.Buisson) ? 10f : 50f);
						y = num7;
						x = num8;
					}
					Gerbes.Add(new Gerbe(GenreDeGerbe.Feuilles, x, y, num, graine, 0, 0, 0));
				}
			}
			else if (casseObjet.Genre == CasseGenre.Poubelle)
			{
				EtincellesDesCoups.Add(new Etincelles(num2, num3, graine, 7, num, num + 0.45));
				FumeesDesCoups.Add(new Fumee(num2, num3 - 3f, graine, 0.3f, num, num + 1.0, num + 4.0));
				if (avant != CasseEtat.Feu && paquet.Etat == CasseEtat.Feu)
				{
					var (num9, num10, num11, _) = Contour(reperee, paquet.Objet);
					FumeesDesCoups.Add(new Fumee((float)carte.X + (num9 + num10) / 2f, (float)carte.Y + num11, graine + 3, 0.7f, num, num + 2.5, num + 9.0));
				}
			}
			else if (avant != CasseEtat.Casse)
			{
				var (r, g, b) = CouleurAu(carte, casseObjet, paquet.X, paquet.Y) ?? (150, 140, 125);
				var (num12, num13, _, num14) = Contour(reperee, paquet.Objet);
				if (paquet.Coup == CasseCoup.Boule || Hash01(graine) < 0.5)
				{
					EclatsDesMurs.Add(new EclatsDeMur(num2, num3, (float)carte.Y + num14 + 2f, r, g, b, graine, (paquet.Coup == CasseCoup.Boule) ? 18 : 8, num, num13 - num12, carte.Id, paquet.Objet));
					Gerbes.Add(new Gerbe(GenreDeGerbe.Poudre, num2, num3, num, graine, 0, 0, 0));
				}
				if (paquet.Coup == CasseCoup.Boule)
				{
					FumeesDesCoups.Add(new Fumee(num2, num3 - 4f, graine, 0.35f, num, num + 1.4, num + 7.0));
					if (casseObjet.Genre == CasseGenre.Bois && Hash01(graine) < 0.6 && paquet.Etat != CasseEtat.Casse)
					{
						FlammesDesCoups.Add(new FlammeAuMonde((Hash01((long)graine + 1L) < 0.5) ? Flamme.M : Flamme.S, num2, num3 + 4f, graine % 8, num + 0.15, num + 9.0));
					}
				}
			}
			if (avant != CasseEtat.Casse && paquet.Etat == CasseEtat.Casse)
			{
				Tombe(carte, reperee, paquet.Objet, casseObjet, milliseconds, num);
				CasseGenre genre = casseObjet.Genre;
				bool flag = (uint)(genre - 6) <= 1u;
				if (flag || (casseObjet.Rocher && casseObjet.Genre == CasseGenre.Muret))
				{
					EnMorceaux(carte, reperee, paquet.Objet, paquet.X, milliseconds);
				}
			}
			break;
		case CasseCoup.Corps:
		case CasseCoup.Balle:
			EffetDeChoc(carte, reperee, paquet, avant, pointsAvant);
			break;
		case CasseCoup.Tranche:
			if (!EmporterArbre(carte, reperee, paquet, casseObjet, milliseconds))
			{
				float num15;
				float num16;
				if (paquet.Partie < 0 || paquet.Partie >= casseObjet.Parties.Length)
				{
					float num7 = num3;
					num15 = num7;
					num16 = num2;
				}
				else
				{
					float num17 = carte.X + casseObjet.Parties[paquet.Partie].X;
					float num7 = carte.Y + casseObjet.Parties[paquet.Partie].Y;
					num15 = num7;
					num16 = num17;
				}
				float num18 = ((casseObjet.Genre == CasseGenre.Buisson) ? 10f : 50f);
				Gerbes.Add(new Gerbe(GenreDeGerbe.Feuilles, num16, num15 - num18, num, graine, 0, 0, 0));
				Gerbes.Add(new Gerbe(GenreDeGerbe.Feuilles, num16 + 12f, num15 - num18 * 0.7f, num, graine + 1, 0, 0, 0));
				Gerbes.Add(new Gerbe(GenreDeGerbe.Eclats, num16, num15 - 12f, num, graine + 2, 96, 62, 34));
				FumeesDesCoups.Add(new Fumee(num16, num15 - 20f, graine + 3, 0.45f, num, num + 0.8, num + 5.0));
			}
			break;
		case CasseCoup.Retour:
		{
			float num4 = (float)carte.X + ((float)casseObjet.X + (float)casseObjet.Largeur / 2f) * (float)Case;
			float num5 = (float)carte.Y + ((float)casseObjet.Y + (float)casseObjet.Hauteur / 2f) * (float)Case;
			int num6 = Math.Clamp(casseObjet.Largeur * casseObjet.Hauteur, 4, 18);
			for (int i = 0; i < num6; i++)
			{
				if (Particules.Count >= 900)
				{
					break;
				}
				Particules.Add(new Particule
				{
					Genre = 4,
					X = num4 + Entre(-casseObjet.Largeur * 12, casseObjet.Largeur * 12),
					Y = num5 + Entre(-casseObjet.Hauteur * 12, casseObjet.Hauteur * 12),
					Vx = Entre(-12f, 12f),
					Vy = Entre(-18f, -4f),
					Taille = Entre(10f, 18f),
					Ne = milliseconds + Hasard.Next(120),
					Vie = 700,
					Couleur = new Color(255, 250, 250, 245)
				});
			}
			EclatsDesMurs.RemoveAll((EclatsDeMur e) => e.Carte == carte.Id && e.Objet == paquet.Objet);
			break;
		}
		case CasseCoup.Feu:
		case CasseCoup.Repare:
			break;
		}
	}

	private static void Tombe(MapInstance carte, CasseCarte reperee, int objet, CasseObjet o, long maintenant, double T)
	{
		(float Gauche, float Droite, float Haut, float Bas) tuple = Contour(reperee, objet);
		float item = tuple.Gauche;
		float item2 = tuple.Droite;
		float item3 = tuple.Haut;
		float item4 = tuple.Bas;
		float num = (float)carte.X + item;
		float num2 = item2 - item;
		float num3 = (float)carte.Y + item3;
		float num4 = item4 - item3;
		Color[] array = PaletteDe(o.Genre);
		CasseGenre genre = o.Genre;
		if ((uint)(genre - 6) <= 1u)
		{
			for (int i = 0; i < 40; i++)
			{
				if (Particules.Count >= 900)
				{
					break;
				}
				float x = num + Entre(0f, num2);
				float y = num3 + Entre(0f, num4 * 0.6f);
				Particules.Add(new Particule
				{
					Genre = 0,
					X = x,
					Y = y,
					Vx = Entre(-90f, 90f),
					Vy = Entre(-140f, -20f),
					Sol = num3 + num4 - Entre(0f, 10f),
					Taille = Entre(2f, 4.5f),
					Ne = maintenant,
					Vie = 3200,
					Couleur = array[Hasard.Next(array.Length)]
				});
			}
			EclatsDesMurs.RemoveAll((EclatsDeMur e) => e.Carte == carte.Id && e.Objet == objet);
		}
		else if (!EstVegetal(o.Genre))
		{
			(byte, byte, byte) tuple2 = CouleurMoyenne(carte, o) ?? (array[0].R, array[0].G, array[0].B);
			float x2 = num + num2 / 2f;
			float num5 = (float)carte.Y + item4 - 8f;
			Gerbes.Add(new Gerbe(GenreDeGerbe.Eclats, x2, num5, T, (int)(maintenant & 0xFFFF), tuple2.Item1, tuple2.Item2, tuple2.Item3));
			FumeesDesCoups.Add(new Fumee(x2, num5 - 2f, (int)(maintenant & 0xFFFF) + 7, 0.4f, T, T + 1.5, T + 6.0));
		}
	}

	private static void NoterBarre(MapInstance carte, CasseCarte reperee, CasseCoupPacket paquet, int pointsAvant, double T)
	{
		CasseObjet casseObjet = reperee.Objets[paquet.Objet];
		if (paquet.PointsMax > 0 && paquet.Partie >= 0 && paquet.Partie < casseObjet.Parties.Length)
		{
			CasseGenre genre = casseObjet.Genre;
			float largeur;
			float haut;
			float x;
			if ((uint)(genre - 3) <= 1u)
			{
				(int X, int Y) tuple = casseObjet.Parties[paquet.Partie];
				int item = tuple.X;
				int item2 = tuple.Y;
				float num = item;
				float num2 = item2 - 96;
				largeur = 92f;
				haut = num2;
				x = num;
			}
			else
			{
				(float Gauche, float Droite, float Haut, float Bas) tuple2 = Contour(reperee, paquet.Objet);
				float item3 = tuple2.Gauche;
				float item4 = tuple2.Droite;
				float item5 = tuple2.Haut;
				float num3 = (item3 + item4) / 2f;
				largeur = item4 - item3;
				haut = item5;
				x = num3;
			}
			(Guid, int, int) key = (carte.Id, paquet.Objet, paquet.Partie);
			if (!Barres.TryGetValue(key, out Barre value))
			{
				value = (Barres[key] = new Barre());
			}
			value.T = T;
			value.Avant = Math.Clamp(pointsAvant, 0, paquet.PointsMax);
			value.Points = Math.Clamp(paquet.Points, 0, paquet.PointsMax);
			value.Max = paquet.PointsMax;
			value.X = x;
			value.Haut = haut;
			value.Largeur = largeur;
		}
	}

	private static (byte R, byte G, byte B)? CouleurAu(MapInstance carte, CasseObjet o, float px, float py)
	{
		int num = (int)(px / (float)Case);
		int num2 = (int)(py / (float)Case);
		List<string> all = Options.Instance.Map.Layers.All;
		for (int num3 = all.Count - 1; num3 >= 0; num3--)
		{
			string text = all[num3];
			if (o.Cases.Contains<(string, int, int)>((text, num, num2)) && carte.Layers.TryGetValue(text, out var value) && num >= 0 && num2 >= 0 && num < value.GetLength(0) && num2 < value.GetLength(1))
			{
				Tile tile = value[num, num2];
				if (tile.TilesetTexture is IGameTexture planche && TuileDe(planche, tile.X, tile.Y, out byte[] tuile) && tuile != null)
				{
					int value2 = (int)px - num * Case;
					int value3 = (int)py - num2 * Case;
					int num4 = (Math.Clamp(value3, 0, Case - 1) * Case + Math.Clamp(value2, 0, Case - 1)) * 4;
					if (tuile[num4 + 3] > 200)
					{
						return (tuile[num4], tuile[num4 + 1], tuile[num4 + 2]);
					}
				}
			}
		}
		return null;
	}

	private static (byte R, byte G, byte B)? CouleurMoyenne(MapInstance carte, CasseObjet o)
	{
		long num = 0L;
		long num2 = 0L;
		long num3 = 0L;
		long num4 = 0L;
		(string, int, int)[] cases = o.Cases;
		for (int i = 0; i < cases.Length; i++)
		{
			var (key, num5, num6) = cases[i];
			if (!carte.Layers.TryGetValue(key, out var value) || num5 < 0 || num6 < 0 || num5 >= value.GetLength(0) || num6 >= value.GetLength(1) || !(value[num5, num6].TilesetTexture is IGameTexture planche) || !TuileDe(planche, value[num5, num6].X, value[num5, num6].Y, out byte[] tuile) || tuile == null)
			{
				continue;
			}
			for (int j = 0; j < tuile.Length; j += 4)
			{
				if (tuile[j + 3] > 200)
				{
					num += tuile[j];
					num2 += tuile[j + 1];
					num3 += tuile[j + 2];
					num4++;
				}
			}
		}
		if (num4 <= 0)
		{
			return null;
		}
		return ((byte)(num / num4), (byte)(num2 / num4), (byte)(num3 / num4));
	}

	public static void DessinerAir()
	{
		NouvelleImage();
		long milliseconds = Timing.Global.Milliseconds;
		SortirLesDifferes(milliseconds);
		PurgerRendus(milliseconds);
		IGameTexture gameTexture = Graphics.Renderer?.WhitePixel;
		if (gameTexture == null)
		{
			return;
		}
		double t = Secondes(milliseconds);
		DessinerCible(gameTexture, milliseconds);
		Recenser(t, milliseconds);
		DessinerFlammes(t);
		DessinerEtincelles(gameTexture, t);
		DessinerMorceaux(milliseconds);
		DessinerArbres(milliseconds);
		DessinerGerbes(gameTexture, t);
		DessinerEclatsDesMurs(gameTexture, t);
		DessinerParticules(gameTexture, milliseconds);
		DessinerEclairsDesChocs(gameTexture, t);
		foreach (var flashsVu in FlashsVus)
		{
			DessinerFlash(flashsVu, t);
		}
		foreach (Poussiere poussieresVue in PoussieresVues)
		{
			DessinerPoussiere(poussieresVue, t);
		}
		foreach (Fumee fumeesVue in FumeesVues)
		{
			DessinerFumee(fumeesVue, t);
		}
		DessinerBarres(gameTexture, t);
	}

	private static void DessinerGerbes(IGameTexture pixel, double T)
	{
		Gerbes.RemoveAll((Gerbe gerbe) => T - gerbe.T > 1.2);
		foreach (Gerbe gerbe in Gerbes)
		{
			double num = T - gerbe.T;
			if (num < 0.0)
			{
				continue;
			}
			switch (gerbe.Genre)
			{
			case GenreDeGerbe.Feuilles:
			{
				for (int num9 = 0; num9 < 12; num9++)
				{
					double num10 = Hash01(gerbe.Graine + (long)num9 * 131L);
					double num11 = Hash01((long)gerbe.Graine * 3L + num9 * 17);
					double num12 = -Math.PI * (0.1 + 0.8 * num10);
					double num13 = 30.0 + 45.0 * num11;
					double x2 = (double)gerbe.X + Math.Cos(num12) * num13 * num + Math.Sin(num * 7.0 + (double)num9) * 3.0;
					double y = (double)gerbe.Y + Math.Sin(num12) * num13 * num + 60.0 * num * num;
					var (r, g, b) = (num9 % 3) switch
					{
						0 => (158, 211, 106), 
						1 => (60, 138, 46), 
						_ => (43, 94, 34), 
					};
					Point(pixel, x2, y, (num9 % 2 == 0) ? 1 : 2, 1f, Math.Max(0.0, 1.0 - num / 1.2), r, g, b);
				}
				break;
			}
			case GenreDeGerbe.Poudre:
			{
				for (int num14 = 0; num14 < 4; num14++)
				{
					double num15 = Hash01(gerbe.Graine + (long)num14 * 131L);
					double num16 = Math.Min(1.0, num / 0.8);
					float num17 = (float)(40.0 * (0.25 + 0.3 * Adoucir(num16)));
					double num18 = (double)gerbe.X + (num15 - 0.5) * 14.0 + ((double)num14 - 1.5) * 4.0 * num16;
					double num19 = (double)(gerbe.Y + 2f) - 10.0 * num16 * (0.5 + num15);
					IGameTexture gameTexture = BouffeesPoussiere[num14 % 3];
					if (gameTexture != null)
					{
						Graphics.DrawGameTexture(gameTexture, new FloatRect(0f, 0f, 40f, 40f), new FloatRect((float)num18 - num17 / 2f, (float)num19 - num17 * 0.55f, num17, num17), new Color((int)(255.0 * Math.Clamp(0.55 * (1.0 - num16), 0.0, 1.0)), 255, 255, 255));
					}
				}
				break;
			}
			case GenreDeGerbe.Eclats:
			{
				for (int num2 = 0; num2 < 10; num2++)
				{
					double num3 = Hash01(gerbe.Graine + (long)num2 * 131L);
					double num4 = Hash01((long)gerbe.Graine * 3L + num2 * 17);
					double num5 = -Math.PI * (0.1 + 0.8 * num3);
					double num6 = 40.0 + 50.0 * num4;
					double x = (double)gerbe.X + Math.Cos(num5) * num6 * num;
					double num7 = (double)gerbe.Y + Math.Sin(num5) * num6 * num + 220.0 * num * num;
					if (!(num7 > (double)(gerbe.Y + 10f)))
					{
						double num8 = 0.7 + 0.5 * num4;
						Point(pixel, x, num7, 2f, 2f, Math.Max(0.0, 1.0 - num / 1.2), (int)Math.Min(255.0, (double)(int)gerbe.R * num8), (int)Math.Min(255.0, (double)(int)gerbe.G * num8), (int)Math.Min(255.0, (double)(int)gerbe.B * num8));
					}
				}
				break;
			}
			}
		}
	}

	private static void DessinerEclatsDesMurs(IGameTexture pixel, double T)
	{
		EclatsDesMurs.RemoveAll((EclatsDeMur e) => T - e.T > 120.0);
		foreach (EclatsDeMur eclatsDesMur in EclatsDesMurs)
		{
			double num = T - eclatsDesMur.T;
			if (num < 0.0)
			{
				continue;
			}
			for (int num2 = 0; num2 < eclatsDesMur.N; num2++)
			{
				double num3 = Hash01(eclatsDesMur.Graine + (long)num2 * 131L);
				double num4 = Hash01((long)eclatsDesMur.Graine * 3L + num2 * 17);
				double num5 = Hash01((long)eclatsDesMur.Graine * 7L + num2 * 29);
				double num6 = (num3 - 0.5) * Math.Max(90.0, (double)eclatsDesMur.Largeur * 0.9);
				double num7 = -30.0 - 70.0 * num4;
				double num8 = Math.Max(eclatsDesMur.Y + 4f, (double)eclatsDesMur.Sol + num5 * 12.0);
				double num9 = (0.0 - num7 + Math.Sqrt(num7 * num7 + 840.0 * (num8 - (double)eclatsDesMur.Y))) / 420.0;
				double num10 = Math.Min(num, num9);
				double x = Math.Round((double)eclatsDesMur.X + num6 * num10);
				double num11 = Math.Round((double)eclatsDesMur.Y + num7 * num10 + 210.0 * num10 * num10);
				double num12 = 0.62 + 0.55 * num4;
				int num13 = ((num2 % 3 == 2) ? 1 : 2);
				if (num >= num9)
				{
					Point(pixel, x, num11 + (double)num13, num13 + 1, 1f, 0.55, 20, 14, 10);
				}
				Point(pixel, x, num11, num13, num13, 1.0, (int)Math.Min(255.0, (double)(int)eclatsDesMur.R * num12), (int)Math.Min(255.0, (double)(int)eclatsDesMur.G * num12), (int)Math.Min(255.0, (double)(int)eclatsDesMur.B * num12));
			}
		}
	}

	private static void DessinerParticules(IGameTexture pixel, long maintenant)
	{
		for (int num = Particules.Count - 1; num >= 0; num--)
		{
			Particule particule = Particules[num];
			long num2 = maintenant - particule.Ne;
			if (num2 >= 0)
			{
				if (num2 >= particule.Vie)
				{
					Particules.RemoveAt(num);
				}
				else
				{
					float num3 = (float)num2 / (float)particule.Vie;
					float num4 = (float)num2 / 1000f;
					if (particule.Genre == 0)
					{
						float x = particule.X + particule.Vx * MathF.Min(num4, 0.8f);
						float num5 = particule.Y + particule.Vy * num4 + 260f * num4 * num4;
						if (num5 >= particule.Sol && num4 > 0.1f)
						{
							num5 = particule.Sol;
							x = particule.X + particule.Vx * MathF.Min(num4, 0.8f) * 0.9f;
						}
						float alpha = ((num3 < 0.75f) ? 1f : ((1f - num3) / 0.25f));
						Graphics.DrawGameTexture(pixel, new FloatRect(0f, 0f, 1f, 1f), new FloatRect(MathF.Round(x), MathF.Round(num5), particule.Taille, particule.Taille), Teinte(particule.Couleur, alpha));
					}
					else
					{
						IGameTexture gameTexture = BouffeesFroid[num % 3];
						if (gameTexture != null)
						{
							float num6 = particule.Taille * (1f + num3 * 0.9f) * 1.4f;
							float num7 = particule.X + particule.Vx * num4 + MathF.Sin(num3 * 4f + (float)(particule.Ne % 11)) * 2f;
							float num8 = particule.Y + particule.Vy * num4;
							Graphics.DrawGameTexture(gameTexture, new FloatRect(0f, 0f, 40f, 40f), new FloatRect(num7 - num6 / 2f, num8 - num6 / 2f, num6, num6), Teinte(new Color(255, 255, 255, 255), 0.85f * (1f - num3)));
						}
					}
				}
			}
		}
	}

	private static void DessinerBarres(IGameTexture pixel, double T)
	{
		List<(Guid, int, int)> list = null;
		foreach (KeyValuePair<(Guid, int, int), Barre> barre2 in Barres)
		{
			barre2.Deconstruct(out var key, out var value);
			(Guid, int, int) item = key;
			Barre barre = value;
			double num = T - barre.T;
			if (num > 4.0 || (barre.Points <= 0 && num > 0.6) || !MapInstance.TryGet(item.Item1, out MapInstance instance))
			{
				(list ?? (list = new List<(Guid, int, int)>())).Add(item);
				continue;
			}
			double num2 = ((num < 3.0) ? 1.0 : (4.0 - num));
			float num3 = Math.Clamp(barre.Largeur * 0.5f, 24f, 56f);
			float num4 = MathF.Round((float)instance.X + barre.X - num3 / 2f);
			float num5 = MathF.Round((float)instance.Y + barre.Haut - 9f);
			double num6 = ((num < 0.45) ? ((double)barre.Avant + (double)(barre.Points - barre.Avant) * Adoucir(num / 0.45)) : ((double)barre.Points));
			double num7 = (double)barre.Points / (double)barre.Max;
			Point(pixel, num4 - 1f, num5 - 1f, num3 + 2f, 5f, 0.85 * num2, 6, 8, 14);
			Point(pixel, num4, num5, MathF.Round((float)((double)num3 * num6 / (double)barre.Max)), 3f, num2, 244, 241, 232);
			int r;
			int g;
			int b;
			if (!(num7 > 0.6))
			{
				if (!(num7 > 0.3))
				{
					r = 240;
					g = 80;
					b = 60;
				}
				else
				{
					r = 245;
					g = 197;
					b = 66;
				}
			}
			else
			{
				r = 98;
				g = 210;
				b = 111;
			}
			Point(pixel, num4, num5, MathF.Round((float)((double)num3 * num7)), 3f, num2, r, g, b);
		}
		if (list == null)
		{
			return;
		}
		foreach (var item2 in list)
		{
			Barres.Remove(item2);
		}
	}

	private static void Point(IGameTexture pixel, double x, double y, float largeur, float hauteur, double alpha, int r, int g, int b)
	{
		if (!(alpha <= 0.0) && !(largeur <= 0f) && !(hauteur <= 0f))
		{
			Graphics.DrawGameTexture(pixel, new FloatRect(0f, 0f, 1f, 1f), new FloatRect((float)Math.Round(x), (float)Math.Round(y), largeur, hauteur), new Color((int)(255.0 * Math.Clamp(alpha, 0.0, 1.0)), r, g, b));
		}
	}

	private static Color Teinte(Color couleur, float alpha)
	{
		return new Color((int)Math.Clamp(255f * alpha * (float)(int)couleur.A / 255f, 0f, 255f), couleur.R, couleur.G, couleur.B);
	}

	private static double HachePied(float x, float y, int n)
	{
		return Hash01((long)Math.Round(x) * 7919 + (long)Math.Round(y) * 131 + (long)n * 104729L);
	}

	private static int GrainePied(float x, float y, int n)
	{
		return (int)Math.Round(HachePied(x, y, n) * 99991.0);
	}

	private static Humeur HumeurDe(float x, float y)
	{
		double num = HachePied(x, y, 12);
		if (!(num < 0.4))
		{
			if (num < 0.7)
			{
				return Humeur.Braise;
			}
			return Humeur.Fume;
		}
		return Humeur.Feu;
	}

	private static double Secondes(long ms)
	{
		return (double)ms / 1000.0;
	}

	private static void Recenser(double T, long maintenant)
	{
		FlammesVues.Clear();
		FumeesVues.Clear();
		EtincellesVues.Clear();
		PoussieresVues.Clear();
		FlashsVus.Clear();
		foreach (var (guid2, num2) in DernieresVues)
		{
			if (maintenant - num2 > 250 || !MapInstance.TryGet(guid2, out MapInstance instance))
			{
				continue;
			}
			CasseCarte casseCarte = CarteDe(instance);
			if (casseCarte == null)
			{
				continue;
			}
			foreach (var (num4, list2) in ObjetsTouches(guid2))
			{
				if (num4 < 0 || num4 >= casseCarte.Objets.Length)
				{
					continue;
				}
				CasseObjet casseObjet = casseCarte.Objets[num4];
				foreach (var item3 in list2)
				{
					int item = item3.Item1;
					EtatPartie item2 = item3.Item2;
					CasseEtat etat = item2.Etat;
					bool flag = etat - 2 <= CasseEtat.Abime;
					if (!flag || item < 0 || item >= casseObjet.Parties.Length)
					{
						continue;
					}
					double num5 = Secondes(item2.Depuis);
					if ((item2.Etat == CasseEtat.Casse && T - num5 > 60.0) || Dehors(casseObjet, item))
					{
						continue;
					}
					var (num6, num7) = casseObjet.Parties[item];
					if (item2.Tranche)
					{
						if (item2.Couche == 0)
						{
							(int, int)? tuple2 = ForetGardee(instance, num4, casseObjet)?.PiedDe(num6, num7);
							if (tuple2.HasValue)
							{
								(int, int) valueOrDefault = tuple2.GetValueOrDefault();
								SoucheFumante(instance, valueOrDefault.Item1, valueOrDefault.Item2 - 26 + 14, num5);
							}
							else
							{
								SoucheFumante(instance, num6, num7, num5);
							}
						}
						continue;
					}
					switch (casseObjet.Genre)
					{
					case CasseGenre.Arbre:
					case CasseGenre.Foret:
						if (EstRond(instance, casseCarte, num4) != false)
						{
							ArbreRond(instance, num6, num7, item2, num5, T);
						}
						else
						{
							ArbreSec(instance, casseCarte, num4, num6, num7, item2, num5, T);
						}
						break;
					case CasseGenre.Buisson:
						BuissonEnFeu(instance, num6, num7, item2, num5, T);
						break;
					case CasseGenre.Poubelle:
						PoubelleEnFeu(instance, casseCarte, num4, num6, num7, item2, num5, T);
						break;
					case CasseGenre.Bois:
					case CasseGenre.Ville:
						if (item2.Etat == CasseEtat.Casse)
						{
							Ruine(instance, casseCarte, num4, casseObjet, num5, T);
						}
						break;
					}
				}
			}
		}
		FlammesDesCoups.RemoveAll((FlammeAuMonde f) => f.T1 < T);
		FumeesDesCoups.RemoveAll((Fumee f) => f.W1 < T - 4.0);
		EtincellesDesCoups.RemoveAll((Etincelles e) => e.T1 < T);
		FlammesVues.AddRange(FlammesDesCoups);
		FumeesVues.AddRange(FumeesDesCoups);
		EtincellesVues.AddRange(EtincellesDesCoups);
		FlammesVues.Sort((FlammeAuMonde p, FlammeAuMonde q) => p.Y.CompareTo(q.Y));
	}

	private static void SoucheFumante(MapInstance carte, float px, float py, double depuis)
	{
		float x = (float)carte.X + px;
		float num = (float)carte.Y + py;
		int num2 = GrainePied(px, py, 30);
		FumeesVues.Add(new Fumee(x, num - 14f, num2, 0.3f, depuis, depuis + 5.0, depuis + 11.0));
		EtincellesVues.Add(new Etincelles(x, num - 12f, num2 + 1, 5, depuis, depuis + 1.8));
	}

	private static void ArbreRond(MapInstance carte, float px, float py, EtatPartie etat, double depuis, double T)
	{
		float num = (float)carte.X + px;
		float num2 = (float)carte.Y + py;
		bool flag = etat.Etat == CasseEtat.Feu;
		double num3 = (flag ? depuis : (depuis - DureeDuFeu));
		double num4 = (flag ? Math.Max(depuis + DureeDuFeu, T + 0.6) : depuis);
		FlammesVues.Add(new FlammeAuMonde(Flamme.XL, num, num2 - 54f, Phase(40), num3 - 0.1, num4 + 0.5));
		FlammesVues.Add(new FlammeAuMonde(Flamme.M, num - 22f, num2 - 40f, Phase(41), num3 + 0.15, num4 + 0.3));
		FlammesVues.Add(new FlammeAuMonde(Flamme.M, num + 22f, num2 - 42f, Phase(42), num3 + 0.25, num4 + 0.3));
		Humeur humeur = HumeurDe(px, py);
		(float, float) tuple;
		(float, float) tuple2;
		if (Hache((int)px, (int)py, 8) % 8 == 0)
		{
			List<(int, int)> list = BoutsDesTroncs();
			float num5 = num - 48f;
			float num6 = num2 - 46f;
			tuple = ((list.Count > 0) ? (num5 + (float)list[0].Item1, num6 + (float)list[0].Item2 - 8f) : (num, num2 - 10f));
			tuple2 = (tuple.Item1, tuple.Item2 + 2f);
			if (!flag)
			{
				for (int i = 0; i < list.Count; i++)
				{
					if (HachePied(px, py, 4) < 0.6 || humeur == Humeur.Feu)
					{
						FlammesVues.Add(new FlammeAuMonde(Flamme.M, num5 + (float)list[i].Item1, num6 + (float)list[i].Item2, Phase(20 + i), num4, num4 + (double)((humeur == Humeur.Fume) ? 3 : 8)));
					}
				}
			}
		}
		else
		{
			((int X, int Y) Cime, (int X, int Y)? Gauche, (int X, int Y)? Droite, (int X, int Y)? Pied) tuple3 = TachesDeLArbre();
			(int, int) item = tuple3.Cime;
			(int, int)? item2 = tuple3.Gauche;
			(int, int)? item3 = tuple3.Droite;
			(int, int)? item4 = tuple3.Pied;
			float num7 = num - 46f;
			float num8 = num2 - 92f;
			tuple = (num7 + (float)item.Item1, num8 + (float)item.Item2 - 30f);
			tuple2 = (num7 + (float)item.Item1, num8 + (float)item.Item2 - 12f);
			if (!flag)
			{
				double num9 = ((humeur == Humeur.Feu) ? 7.0 : ((humeur == Humeur.Braise && HachePied(px, py, 17) < 0.6) ? 3.5 : 0.0));
				if (num9 > 0.0)
				{
					FlammesVues.Add(new FlammeAuMonde((humeur == Humeur.Feu) ? Flamme.XL : Flamme.L, num7 + (float)item.Item1, num8 + (float)item.Item2, Phase(20), num4, num4 + num9));
				}
				int num10 = ((!(HachePied(px, py, 16) < 0.5)) ? 1 : 0);
				(int, int)?[] array = new(int, int)?[2] { item2, item3 };
				for (int j = 0; j < 2; j++)
				{
					(int, int)? tuple4 = array[j];
					if (tuple4.HasValue)
					{
						(int, int) valueOrDefault = tuple4.GetValueOrDefault();
						int num11 = ((humeur == Humeur.Feu) ? 9 : ((humeur == Humeur.Braise && j == num10) ? 5 : 0));
						if (num11 > 0)
						{
							FlammesVues.Add(new FlammeAuMonde(Flamme.M, num7 + (float)valueOrDefault.Item1, num8 + (float)valueOrDefault.Item2, Phase(21 + j), num4, num4 + (double)num11));
						}
					}
				}
				if (item4.HasValue)
				{
					(int, int) valueOrDefault2 = item4.GetValueOrDefault();
					int num12 = ((humeur == Humeur.Feu && HachePied(px, py, 15) < 0.5) ? 5 : ((humeur == Humeur.Braise && HachePied(px, py, 15) < 0.3) ? 3 : 0));
					if (num12 > 0)
					{
						FlammesVues.Add(new FlammeAuMonde(Flamme.G, num7 + (float)valueOrDefault2.Item1, num8 + (float)valueOrDefault2.Item2, Phase(23), num4, num4 + (double)num12));
					}
				}
			}
		}
		if (!flag && Hash01((long)Math.Round(px) * 31 + (long)Math.Round(py - 4f) * 17 + 7919) < 0.22)
		{
			FlammesVues.Add(new FlammeAuMonde(Flamme.G, num, num2 - 7f, Phase(24), num4 + 0.5, num4 + humeur switch
			{
				Humeur.Braise => 3.0, 
				Humeur.Feu => 4.0, 
				_ => 2.5, 
			}));
		}
		var (num13, num14, echelle) = humeur switch
		{
			Humeur.Feu => (9.0, 26.0, 0.9f), 
			Humeur.Braise => (5.0, 18.0, 0.6f), 
			_ => (2.0, 12.0, 0.5f), 
		};
		FumeesVues.Add(new Fumee(tuple.Item1, tuple.Item2, GrainePied(px, py, 31), echelle, num3, num4 + num13, num4 + num14));
		double t = (flag ? num4 : ((humeur == Humeur.Fume) ? num4 : (num4 + ((humeur == Humeur.Feu) ? 7.0 : 3.5))));
		EtincellesVues.Add(new Etincelles(tuple2.Item1, tuple2.Item2, GrainePied(px, py, 30), (flag || humeur == Humeur.Feu) ? 9 : 4, num3 + 0.6, t));
		int Phase(int n)
		{
			return (int)(HachePied(px, py, n) * 8.0);
		}
	}

	private static void ArbreSec(MapInstance carte, CasseCarte reperee, int objet, float px, float py, EtatPartie etat, double depuis, double T)
	{
		float item = Contour(reperee, objet).Haut;
		float x = (float)carte.X + px;
		float num = (float)carte.Y + item;
		bool flag = etat.Etat == CasseEtat.Feu;
		double num2 = (flag ? depuis : (depuis - DureeDuFeu));
		double num3 = (flag ? Math.Max(depuis + DureeDuFeu, T + 0.6) : depuis);
		int phase = (int)(HachePied(px, py, 1) * 8.0);
		FlammesVues.Add(new FlammeAuMonde(Flamme.L, x, num + 14f, phase, num2 - 0.1, num3 + 1.5));
		if (HachePied(px, py, 3) < 0.3)
		{
			float y = num + ((float)carte.Y + py - num) * 0.45f;
			FlammesVues.Add(new FlammeAuMonde(Flamme.M, x, y, (int)(HachePied(px, py, 2) * 8.0), num3, num3 + 6.0));
		}
		FumeesVues.Add(new Fumee(x, num + 4f, GrainePied(px, py, 3), 0.6f, num2, num3 + 6.0, num3 + 20.0));
		EtincellesVues.Add(new Etincelles(x, num + 8f, GrainePied(px, py, 30), 4, num2 + 0.6, num3 + 2.0));
	}

	private static void BuissonEnFeu(MapInstance carte, float px, float py, EtatPartie etat, double depuis, double T)
	{
		float x = (float)carte.X + px;
		float num = (float)carte.Y + py;
		bool flag = etat.Etat == CasseEtat.Feu;
		double num2 = (flag ? depuis : (depuis - DureeDuFeu));
		double num3 = (flag ? Math.Max(depuis + DureeDuFeu, T + 0.6) : depuis);
		int phase = (int)(HachePied(px, py, 40) * 8.0);
		FlammesVues.Add(new FlammeAuMonde(Flamme.M, x, num - 6f, phase, num2 - 0.1, num3 + 0.4));
		if (!flag)
		{
			FlammesVues.Add(new FlammeAuMonde(Flamme.M, x, num - 4f, (int)(HachePied(px, py, 5) * 8.0), num3, num3 + 4.0));
		}
		FumeesVues.Add(new Fumee(x, num - 10f, GrainePied(px, py, 31), 0.45f, num2, num3 + 9.0, num3 + 26.0));
		EtincellesVues.Add(new Etincelles(x, num - 8f, GrainePied(px, py, 30), 9, num2 + 0.6, num3 + 7.0));
	}

	private static void PoubelleEnFeu(MapInstance carte, CasseCarte reperee, int objet, float px, float py, EtatPartie etat, double depuis, double T)
	{
		bool flag = etat.Etat == CasseEtat.Feu;
		double num = (flag ? depuis : (depuis - DureeDuFeu));
		double num2 = (flag ? Math.Max(depuis + DureeDuFeu, T + 0.6) : depuis);
		int num3 = (int)(HachePied(px, py, 60) * 8.0);
		var (num4, num5) = SurLeCouvercle(carte, reperee, objet, 0.3f);
		var (num6, num7) = SurLeCouvercle(carte, reperee, objet, 0.55f);
		var (x, num8) = SurLeCouvercle(carte, reperee, objet, 0.8f);
		FlammesVues.Add(new FlammeAuMonde(Flamme.L, num6, num7, num3, num - 0.05, num2 + 6.0));
		FlammesVues.Add(new FlammeAuMonde(Flamme.M, num4, num5, (num3 + 3) % 8, num + 0.25, num2 + 0.4));
		FlammesVues.Add(new FlammeAuMonde(Flamme.M, x, num8, (num3 + 5) % 8, num + 0.45, num2 + 0.2));
		FlammesVues.Add(new FlammeAuMonde(Flamme.S, (num4 + num6) / 2f, (num5 + num7) / 2f + 3f, (num3 + 6) % 8, num + 0.8, num2));
		FlammesVues.Add(new FlammeAuMonde(Flamme.M, num6, num7 + 1f, (num3 + 1) % 8, num2 + 5.6, num2 + 14.0));
		FlammesVues.Add(new FlammeAuMonde(Flamme.S, x, num8 + 1f, (num3 + 2) % 8, num2 + 1.5, num2 + 22.0));
		FlammesVues.Add(new FlammeAuMonde(Flamme.S, num4, num5 + 1f, (num3 + 7) % 8, num2 + 3.0, num2 + 12.0));
		FumeesVues.Add(new Fumee(num6, num7 - 22f, GrainePied(px, py, 61), 0.6f, num + 0.2, num2 + 16.0, num2 + 34.0));
		EtincellesVues.Add(new Etincelles(num6, num7 - 6f, GrainePied(px, py, 62), 10, num + 0.3, num2 + 10.0));
	}

	private static (float X, float Y) SurLeCouvercle(MapInstance carte, CasseCarte reperee, int objet, float u)
	{
		CasseObjet casseObjet = reperee.Objets[objet];
		float num = carte.X + casseObjet.X * Case;
		float num2 = carte.Y + casseObjet.Y * Case;
		if (Rendus.TryGetValue((carte.Id, objet), out Rendu value))
		{
			int[] hauts = value.Hauts;
			if (hauts != null)
			{
				int num3 = Array.FindIndex(hauts, (int h) => h >= 0);
				int num4 = Array.FindLastIndex(hauts, (int h) => h >= 0);
				if (num3 >= 0)
				{
					int num5 = (int)MathF.Round((float)num3 + (float)(num4 - num3) * u);
					int num6 = hauts.Where((int h) => h >= 0).Min();
					float num7 = Math.Clamp((float)(casseObjet.Hauteur * Case - num6) * 0.3f, 10f, 22f);
					return (X: num + (float)num5, Y: num2 + (float)Math.Max(0, hauts[num5]) + num7);
				}
			}
		}
		var (num8, num9, num10, num11) = Contour(reperee, objet);
		return (X: (float)carte.X + num8 + (num9 - num8) * u, Y: (float)carte.Y + num10 + Math.Clamp((num11 - num10) * 0.3f, 10f, 22f));
	}

	private static float? LueurDesFentes(Guid carte, int objet, double T)
	{
		EtatPartie etatPartie = null;
		if (ObjetsTouches(carte).TryGetValue(objet, out List<(int, EtatPartie)> value))
		{
			foreach (var item2 in value)
			{
				EtatPartie item = item2.Item2;
				CasseEtat etat = item.Etat;
				if (etat - 2 <= CasseEtat.Abime)
				{
					etatPartie = item;
					break;
				}
			}
		}
		if (etatPartie == null)
		{
			return null;
		}
		double num = Secondes(etatPartie.Depuis);
		double num2 = 0.5 + 0.5 * Math.Sin(T * 2.3 + (double)objet);
		double num3 = 0.85 + 0.15 * Math.Sin(T * 11.3 + (double)objet * 1.7);
		double num4 = ((etatPartie.Etat == CasseEtat.Feu) ? Math.Clamp((T - num) / 1.5, 0.0, 1.0) : Math.Clamp(1.0 - (T - num) / 25.0, 0.0, 1.0));
		return (float)Math.Clamp(num4 * (0.55 + 0.4 * num2) * num3, 0.0, 1.0);
	}

	private static void Ruine(MapInstance carte, CasseCarte reperee, int objet, CasseObjet o, double a, double T)
	{
		(float Gauche, float Droite, float Haut, float Bas) tuple = Contour(reperee, objet);
		float item = tuple.Gauche;
		float item2 = tuple.Droite;
		float item3 = tuple.Haut;
		float item4 = tuple.Bas;
		float num = (float)carte.X + item;
		float num2 = (float)carte.X + item2;
		float num3 = (float)carte.Y + item3;
		float base_ = (float)carte.Y + item4;
		float cx = (num + num2) / 2f;
		PoussieresVues.Add(new Poussiere(num, num2, base_, num3, a, (int)Math.Round(Hh(5) * 99991.0)));
		FlashsVus.Add((cx, (num3 + base_) / 2f, a, 70f));
		float num4 = ((o.Hauteur <= 4) ? 0.3f : 0.45f);
		float num5 = base_ - (base_ - num3) * num4;
		float num6 = Math.Clamp((num2 - num) / 96f, 0.6f, 1.6f);
		int num7 = ((o.Genre == CasseGenre.Bois) ? 2 : ((Hh(9) < 0.5) ? 1 : 0));
		for (int i = 0; i < num7; i++)
		{
			float num8 = ((i == 0) ? 0.36f : 0.62f);
			Flamme taille = ((!(num6 >= 1.2f)) ? Flamme.M : Flamme.L);
			FlammesVues.Add(new FlammeAuMonde(taille, num + num8 * (num2 - num), num5 + 3f, (i * 3 + 5) % 8, a + 0.2 + 0.15 * (double)i, a + 12.0 + 3.0 * Hh(10 + i)));
		}
		for (int j = 0; j < 2; j++)
		{
			float num9 = ((j == 0) ? 0.25f : 0.7f);
			FumeesVues.Add(new Fumee(num + num9 * (num2 - num), num5 + 6f, (int)Math.Round(Hh(20 + j) * 99991.0), Math.Max(0.7f, num6 * 1.1f), a, a + 16.0, a + 36.0));
		}
		double Hh(int n)
		{
			return Hash01((long)Math.Round(cx) * 7919 + (long)Math.Round(base_) * 131 + (long)n * 104729L);
		}
	}

	private static (float Gauche, float Droite, float Haut, float Bas) Contour(CasseCarte reperee, int objet)
	{
		if (Contours.TryGetValue((reperee.Id, objet), out (float, float, float, float) value))
		{
			return value;
		}
		CasseObjet casseObjet = reperee.Objets[objet];
		float item = casseObjet.X * Case;
		float item2 = (casseObjet.X + casseObjet.Largeur) * Case;
		float item3 = casseObjet.Y * Case;
		float item4 = (casseObjet.Y + casseObjet.Hauteur) * Case;
		if (casseObjet.Silhouette.Length != 0)
		{
			int num = casseObjet.Largeur * Case / 8;
			int num2 = casseObjet.Hauteur * Case / 8;
			int num3 = num;
			int num4 = -1;
			int num5 = num2;
			int num6 = -1;
			for (int i = 0; i < num2; i++)
			{
				for (int j = 0; j < num; j++)
				{
					int num7 = i * num + j;
					if (num7 >> 3 < casseObjet.Silhouette.Length && (casseObjet.Silhouette[num7 >> 3] & (1 << (num7 & 7))) != 0)
					{
						num3 = Math.Min(num3, j);
						num4 = Math.Max(num4, j);
						num5 = Math.Min(num5, i);
						num6 = Math.Max(num6, i);
					}
				}
			}
			if (num4 >= 0)
			{
				item = casseObjet.X * Case + num3 * 8;
				item2 = casseObjet.X * Case + (num4 + 1) * 8;
				item3 = casseObjet.Y * Case + num5 * 8;
				item4 = casseObjet.Y * Case + (num6 + 1) * 8;
			}
		}
		return Contours[(reperee.Id, objet)] = (item, item2, item3, item4);
	}

	private static List<(int X, int Y)> BoutsDesTroncs()
	{
		List<(int, int)> list = new List<(int, int)>();
		Braises braises = BraisesDe("casse_troncs_calcines.png");
		if (braises != null)
		{
			foreach (var ama in braises.Amas)
			{
				var (x, y, _) = ama;
				if (list.Count < 2 && list.All<(int, int)>(((int X, int Y) b) => MathF.Sqrt((b.X - x) * (b.X - x) + (b.Y - y) * (b.Y - y)) >= 24f))
				{
					list.Add((x, y));
				}
			}
		}
		return list;
	}

	private static float Vie(double t0, double t1, double T)
	{
		return (float)Math.Min(1.0, Math.Min((T - t0) / 0.15, (t1 - T) / 0.4));
	}

	private static void DessinerFlammes(double T)
	{
		if (FlammesVues.Count == 0)
		{
			return;
		}
		IGameTexture haloChaud = HaloChaud;
		if (haloChaud != null)
		{
			foreach (FlammeAuMonde flammesVue in FlammesVues)
			{
				bool flag = T < flammesVue.T0 || T >= flammesVue.T1;
				bool flag2 = flag;
				if (!flag2)
				{
					Flamme taille = flammesVue.Taille;
					bool flag3 = (uint)(taille - 2) <= 1u;
					flag2 = !flag3;
				}
				if (!flag2)
				{
					float num = ((flammesVue.Taille == Flamme.XL) ? 36f : 26f);
					float value = (0.2f + 0.06f * (float)Math.Sin(T * 9.0 + (double)flammesVue.Phase * 1.7)) * Vie(flammesVue.T0, flammesVue.T1, T);
					Graphics.DrawGameTexture(haloChaud, new FloatRect(0f, 0f, haloChaud.Width, haloChaud.Height), new FloatRect(flammesVue.X - num, flammesVue.Y - num * 1.2f, num * 2f, num * 2f), new Color((int)(255f * Math.Clamp(value, 0f, 1f)), 255, 255, 255), null, GameBlendModes.Add);
				}
			}
		}
		long image = (long)Math.Floor(T * 1000.0 / 70.0);
		foreach (FlammeAuMonde flammesVue2 in FlammesVues)
		{
			(IGameTexture, FloatRect, float)? tuple = FlammeVue(flammesVue2, T, image);
			if (tuple.HasValue)
			{
				(IGameTexture, FloatRect, float) valueOrDefault = tuple.GetValueOrDefault();
				Graphics.DrawGameTexture(valueOrDefault.Item1, new FloatRect(0f, 0f, valueOrDefault.Item2.Width, valueOrDefault.Item2.Height), valueOrDefault.Item2, new Color((int)(255f * valueOrDefault.Item3), 255, 255, 255));
			}
		}
	}

	private static void DessinerEtincelles(IGameTexture pixel, double T, bool dansLaLumiere = false)
	{
		foreach (Etincelles etincellesVue in EtincellesVues)
		{
			if (T < etincellesVue.T0 || T >= etincellesVue.T1)
			{
				continue;
			}
			for (int i = 0; i < etincellesVue.N; i++)
			{
				double num = Hash01((long)etincellesVue.Graine * 131L + i * 7);
				double num2 = Hash01((long)etincellesVue.Graine * 17L + i * 131);
				double num3 = 0.9 + 0.7 * num;
				double num4 = ((T + num2 * num3) % num3 + num3) % num3;
				double num5 = num4 / num3;
				double num6 = Math.Sin(T * 23.0 + (double)i * 3.1);
				double value = (1.0 - num5) * (0.55 + 0.45 * num6 * num6);
				(int, int, int) tuple = ((num5 < 0.35) ? (255, 233, 160) : ((num5 < 0.7) ? (255, 179, 71) : (224, 86, 28)));
				double num7 = Math.Round((double)etincellesVue.X + (num - 0.5) * 8.0 + Math.Sin(num4 * 5.0 + (double)i) * 2.0 * num5 + num4 * 6.0);
				double num8 = Math.Round((double)etincellesVue.Y - num4 * (20.0 + 14.0 * num2));
				Color color = new Color((int)(255.0 * Math.Clamp(value, 0.0, 1.0)), tuple.Item1, tuple.Item2, tuple.Item3);
				if (dansLaLumiere)
				{
					Graphics.AddLightTexture(pixel, new FloatRect(0f, 0f, 1f, 1f), new FloatRect((float)num7, (float)num8, 1f, 1f), color);
				}
				else
				{
					Graphics.DrawGameTexture(pixel, new FloatRect(0f, 0f, 1f, 1f), new FloatRect((float)num7, (float)num8, 1f, 1f), color);
				}
			}
		}
	}

	private static double Adoucir(double u)
	{
		return 1.0 - Math.Pow(1.0 - Math.Clamp(u, 0.0, 1.0), 3.0);
	}

	private static void DessinerFumee(Fumee e, double T)
	{
		double num = Math.Max(e.P0, T - 3.6);
		double num2 = Math.Min(e.W1, T);
		if (num > num2)
		{
			return;
		}
		for (long num3 = (long)Math.Ceiling(num / 0.17); (double)num3 * 0.17 <= num2; num3++)
		{
			double num4 = (double)num3 * 0.17;
			double num5 = T - num4;
			bool flag = num4 < e.P1;
			if (!flag && num3 % 2 != 0L)
			{
				continue;
			}
			double num6 = Hash01((long)e.Graine * 7919L + num3 * 131);
			double num7 = Hash01((long)e.Graine * 104729L + num3 * 17);
			double num8 = (flag ? 3.6 : 2.8800000000000003);
			double num9 = num5 / num8;
			if (!(num9 >= 1.0) && !(num5 < 0.0))
			{
				double num10 = (double)e.Echelle * (flag ? 1.0 : 0.6);
				double num11 = num10 * (0.22 + 0.78 * Adoucir(Math.Min(1.0, num9 * 1.3)));
				double num12 = (double)e.X + (num6 - 0.5) * 6.0 * num10 + num5 * (8.0 + 6.0 * num7) * num10 * Math.Min(1.0, num5 * 0.8) + Math.Sin(num5 * 1.7 + num6 * 6.0) * 2.0 * num10;
				double num13 = (double)e.Y - num5 * (17.0 + 8.0 * num6) * num10 * (1.0 - 0.25 * num9);
				double value = (flag ? 0.5 : 0.26) * Math.Min(1.0, num5 / 0.25) * Math.Pow(1.0 - num9, 1.3);
				IGameTexture gameTexture = (flag ? BouffeesFeu : BouffeesFroid)[num3 % 3];
				if (gameTexture != null)
				{
					float num14 = (float)(40.0 * num11);
					Graphics.DrawGameTexture(gameTexture, new FloatRect(0f, 0f, 40f, 40f), new FloatRect((float)num12 - num14 / 2f, (float)num13 - num14 * 0.55f, num14, num14), new Color((int)(255.0 * Math.Clamp(value, 0.0, 1.0)), 255, 255, 255));
				}
			}
		}
	}

	private static void DessinerPoussiere(Poussiere e, double T)
	{
		double num = T - e.T;
		if (num < -0.05 || num > 2.8)
		{
			return;
		}
		int num2 = (int)Math.Round(Math.Clamp((e.X1 - e.X0) / 6f, 16f, 40f));
		double num3 = Math.Max(0.0, num) / 2.8;
		double num4 = (double)(e.Y - e.Haut) * 0.7;
		for (int i = 0; i < num2; i++)
		{
			double num5 = Hash01(e.Graine + (long)i * 131L);
			double num6 = Hash01((long)e.Graine * 7L + i * 17);
			double num7 = Hash01((long)e.Graine * 3L + i * 29);
			int num8 = ((!(num7 < 0.5)) ? 1 : (-1));
			double num9 = (double)e.X0 + (double)(e.X1 - e.X0) * ((double)i + 0.5) / (double)num2 + (num5 - 0.5) * 12.0 + (double)num8 * Math.Max(0.0, num) * (14.0 + 26.0 * num6);
			double num10 = (double)(e.Y - 6f) - num4 * num6 * Adoucir(Math.Min(1.0, num3 * 3.0)) - Math.Max(0.0, num) * (8.0 + 10.0 * num5);
			double num11 = 0.7 + 1.1 * Adoucir(Math.Min(1.0, num3 * 1.6));
			float num12 = (float)(40.0 * num11);
			double value = 0.78 * Math.Pow(1.0 - num3, 1.1) * Math.Min(1.0, (num + 0.05) / 0.1);
			IGameTexture gameTexture = BouffeesPoussiere[i % 3];
			if (gameTexture != null)
			{
				Graphics.DrawGameTexture(gameTexture, new FloatRect(0f, 0f, 40f, 40f), new FloatRect((float)num9 - num12 / 2f, (float)num10 - num12 * 0.55f, num12, num12), new Color((int)(255.0 * Math.Clamp(value, 0.0, 1.0)), 255, 255, 255));
			}
		}
	}

	private static void DessinerFlash((float X, float Y, double T, float R) f, double T)
	{
		double num = T - f.T;
		if (!(num < 0.0) && !(num >= 0.3))
		{
			IGameTexture haloBlanc = HaloBlanc;
			if (haloBlanc != null)
			{
				double num2 = 0.9 * (1.0 - num / 0.3);
				Graphics.DrawGameTexture(haloBlanc, new FloatRect(0f, 0f, haloBlanc.Width, haloBlanc.Height), new FloatRect(f.X - f.R, f.Y - f.R, f.R * 2f, f.R * 2f), new Color((int)(255.0 * num2), 255, 255, 255), null, GameBlendModes.Add);
			}
		}
	}

	private static float? LueurDesBraises(Guid carte, int objet, int partie, float px, float py, double T)
	{
		EtatPartie etatPartie = EtatDe(carte, objet, partie);
		if (etatPartie == null || etatPartie.Etat != CasseEtat.Casse)
		{
			return null;
		}
		double num = Secondes(etatPartie.Depuis);
		if (T < num || T - num > 75.0)
		{
			return null;
		}
		double num2 = num;
		double num3 = num2 + (double)(HumeurDe(px, py) switch
		{
			Humeur.Feu => 9, 
			Humeur.Braise => 5, 
			_ => 2, 
		});
		double num4 = num + 30.0;
		double num5 = ((T < num3) ? 1.0 : ((T < num4) ? Math.Max(0.12, 1.0 - (T - num3) / (num4 - num3)) : ((T < num + 60.0) ? 0.12 : (0.12 * (1.0 - (T - num - 60.0) / 15.0)))));
		double num6 = HachePied(px, py, 50) * 6.0;
		double num7 = 0.5 + 0.5 * Math.Sin(T * 1.5 + num6);
		double num8 = 0.88 + 0.12 * Math.Sin(T * 6.7 + num6 * 2.0);
		return (float)Math.Clamp((0.06 + 0.3 * num7) * num8 * num5 * Math.Min(1.0, (T - num) / 0.5), 0.0, 1.0);
	}

	private static double Hash01(long n)
	{
		uint num = (uint)(((int)n ^ -1640531527) * -2048144789);
		num ^= num >> 13;
		num *= 3266489909u;
		num ^= num >> 16;
		return (double)num / 4294967296.0;
	}

	private static Flamme Moindre(Flamme f)
	{
		return f switch
		{
			Flamme.XL => Flamme.L, 
			Flamme.L => Flamme.M, 
			Flamme.M => Flamme.S, 
			_ => f, 
		};
	}

	private static Bande BandeDe(Flamme f)
	{
		if (_bandes == null)
		{
			_bandes = new Bande[5];
		}
		ref Bande reference = ref _bandes[(int)f];
		Bande bande = reference;
		Bande bande2 = bande;
		if (bande2 == null)
		{
			bande2 = (reference = f switch
			{
				Flamme.S => FaireBande(5f, 7f, 2, 11), 
				Flamme.M => FaireBande(9f, 11f, 3, 23), 
				Flamme.L => FaireBande(14f, 17f, 5, 37), 
				Flamme.XL => FaireBande(18f, 30f, 6, 53), 
				_ => FaireBande(13f, 8f, 4, 71), 
			});
		}
		return bande2;
	}

	private static Bande FaireBande(float largeur, float hauteur, int n, int graine)
	{
		int num = (int)Math.Ceiling(largeur + 14f);
		int num2 = (int)Math.Ceiling((double)hauteur * 1.8 + 3.0);
		int num3 = num2 - 2;
		Bande bande = new Bande
		{
			L = num,
			H = num2,
			Pied = num3
		};
		for (int i = 0; i < 8; i++)
		{
			Mulberry mulberry = new Mulberry(graine + i * 97);
			double num4 = (double)i * (Math.PI * 2.0) / 8.0;
			(float, float, float, float, float)[] array = new(float, float, float, float, float)[n];
			for (int j = 0; j < n; j++)
			{
				double num5 = ((n == 1) ? 0.5 : ((double)j / (double)(n - 1)));
				double num6 = 1.0 + 0.22 * Math.Sin(num4 + (double)j * 2.3) + 0.1 * Math.Sin(num4 * 2.0 + (double)j * 0.7) + (mulberry.Suivant() - 0.5) * 0.12;
				double num7 = ((j % 2 == 0) ? 1.18 : 0.72);
				double num8 = 1.0 - 0.3 * Math.Abs(num5 - 0.5) * 2.0;
				double num9 = Math.Abs(num5 - 0.5) * 2.0;
				double num10 = (double)num3 - num9 * num9 * (double)hauteur * 0.3;
				double num11 = (double)num / 2.0 + (num5 - 0.5) * (double)largeur;
				double num12 = (double)hauteur * num6 * num7 * num8;
				double num13 = 2.0 * Math.Sin(num4 + (double)j * 1.7) + (num5 - 0.5) * 2.0 + (mulberry.Suivant() - 0.5) * 0.8;
				double num14 = (double)(largeur / (float)n) * ((j % 2 == 0) ? 0.55 : 0.8) + 0.8;
				array[j] = ((float)num11, (float)num10, (float)num12, (float)num13, (float)num14);
			}
			float[] array2 = new float[num * num2 * 4];
			(float, float, float, float, float, float, float)[] couchesDuFeu = CouchesDuFeu;
			for (int k = 0; k < couchesDuFeu.Length; k++)
			{
				(float, float, float, float, float, float, float) couche = couchesDuFeu[k];
				(float, float, float, float, float)[] array3 = array;
				for (int l = 0; l < array3.Length; l++)
				{
					(float, float, float, float, float) tuple = array3[l];
					Langue(array2, num, num2, tuple.Item1, tuple.Item2, tuple.Item3 * couche.Item1, tuple.Item4 * couche.Item1, tuple.Item5 * couche.Item2, couche);
				}
			}
			float num15 = (float)num3 - Math.Max(3f, hauteur * 0.35f);
			for (int m = 0; m < num2; m++)
			{
				float num16 = Math.Clamp(((float)m + 0.5f - num15) / ((float)(num3 + 1) - num15), 0f, 1f);
				float num17 = 1f + -0.85f * num16;
				for (int num18 = 0; num18 < num; num18++)
				{
					int num19 = (m * num + num18) * 4;
					array2[num19] *= num17;
					array2[num19 + 1] *= num17;
					array2[num19 + 2] *= num17;
					array2[num19 + 3] *= num17;
				}
			}
			bande.Images[i] = Graphics.Renderer?.CreateTextureFromPixels($"casse/feu/{graine}/{i}", num, num2, Octets(array2));
		}
		return bande;
	}

	private static void Langue(float[] px, int W, int H, float fx, float fy, float h, float derive, float w, (float H, float W, float R, float G, float B, float A, float Pointe) couche)
	{
		float num = fx + derive * 0.12f;
		float item = fy - h * 0.3f;
		float num2 = fx + derive * 0.55f;
		float item2 = fy - h * 0.68f;
		(float, float)[] p = new(float, float)[7]
		{
			(fx - w, fy),
			(num - w * 1.08f, item),
			(num2 - w * 0.5f, item2),
			(fx + derive, fy - h),
			(num2 + w * 0.5f, item2),
			(num + w * 1.08f, item),
			(fx + w, fy)
		};
		List<(float, float)> contour = CourbeFermee(p, 0.5f);
		float haut = fy + 1f;
		float bas = fy - h - 1f;
		Remplir(px, W, H, contour, delegate(int y)
		{
			float num3 = Math.Clamp((haut - ((float)y + 0.5f)) / (haut - bas), 0f, 1f);
			return (couche.A + (couche.Pointe - couche.A) * num3) / 255f;
		}, couche.R / 255f, couche.G / 255f, couche.B / 255f);
	}

	private static List<(float X, float Y)> CourbeFermee((float X, float Y)[] p, float tension)
	{
		int num = p.Length;
		List<(float, float)> list = new List<(float, float)>(num * 10 + 1) { p[0] };
		for (int i = 0; i < num; i++)
		{
			(float X, float Y) tuple = p[(i - 1 + num) % num];
			(float X, float Y) tuple2 = p[i];
			(float X, float Y) tuple3 = p[(i + 1) % num];
			(float, float) tuple4 = p[(i + 2) % num];
			(float, float) tuple5 = tuple3;
			(float, float) tuple6 = tuple2;
			(float, float) tuple7 = tuple;
			float num2 = tuple6.Item1 + (tuple5.Item1 - tuple7.Item1) * tension / 3f;
			float num3 = tuple6.Item2 + (tuple5.Item2 - tuple7.Item2) * tension / 3f;
			float num4 = tuple5.Item1 - (tuple4.Item1 - tuple6.Item1) * tension / 3f;
			float num5 = tuple5.Item2 - (tuple4.Item2 - tuple6.Item2) * tension / 3f;
			for (int j = 1; j <= 10; j++)
			{
				float num6 = (float)j / 10f;
				float num7 = 1f - num6;
				list.Add((num7 * num7 * num7 * tuple6.Item1 + 3f * num7 * num7 * num6 * num2 + 3f * num7 * num6 * num6 * num4 + num6 * num6 * num6 * tuple5.Item1, num7 * num7 * num7 * tuple6.Item2 + 3f * num7 * num7 * num6 * num3 + 3f * num7 * num6 * num6 * num5 + num6 * num6 * num6 * tuple5.Item2));
			}
		}
		return list;
	}

	private static void Remplir(float[] px, int W, int H, List<(float X, float Y)> contour, Func<int, float> alphaDeLaRangee, float r, float g, float b)
	{
		float num = float.MaxValue;
		float num2 = float.MinValue;
		foreach (var item2 in contour)
		{
			float item = item2.Y;
			num = Math.Min(num, item);
			num2 = Math.Max(num2, item);
		}
		int num3 = Math.Max(0, (int)Math.Floor(num));
		int num4 = Math.Min(H - 1, (int)Math.Ceiling(num2));
		float[] array = new float[W];
		List<float> list = new List<float>(16);
		for (int i = num3; i <= num4; i++)
		{
			Array.Clear(array);
			for (int j = 0; j < 4; j++)
			{
				float num5 = (float)i + ((float)j + 0.5f) / 4f;
				list.Clear();
				for (int k = 0; k < contour.Count; k++)
				{
					var (num6, num7) = contour[k];
					var (num8, num9) = contour[(k + 1) % contour.Count];
					if ((num7 <= num5 && num9 > num5) || (num9 <= num5 && num7 > num5))
					{
						list.Add(num6 + (num5 - num7) / (num9 - num7) * (num8 - num6));
					}
				}
				list.Sort();
				for (int l = 0; l + 1 < list.Count; l += 2)
				{
					float num10 = Math.Max(0f, list[l]);
					float num11 = Math.Min(W, list[l + 1]);
					for (int m = (int)Math.Floor(num10); (float)m < num11 && m < W; m++)
					{
						float num12 = Math.Min(num11, m + 1) - Math.Max(num10, m);
						if (num12 > 0f)
						{
							array[m] += num12 / 4f;
						}
					}
				}
			}
			float num13 = alphaDeLaRangee(i);
			for (int n = 0; n < W; n++)
			{
				float num14 = Math.Min(1f, array[n]) * num13;
				if (!(num14 <= 0f))
				{
					int num15 = (i * W + n) * 4;
					px[num15] = r * num14 + px[num15] * (1f - num14);
					px[num15 + 1] = g * num14 + px[num15 + 1] * (1f - num14);
					px[num15 + 2] = b * num14 + px[num15 + 2] * (1f - num14);
					px[num15 + 3] = num14 + px[num15 + 3] * (1f - num14);
				}
			}
		}
	}

	private static byte[] Octets(float[] px)
	{
		byte[] array = new byte[px.Length];
		for (int i = 0; i < px.Length; i += 4)
		{
			float num = Math.Clamp(px[i + 3], 0f, 1f);
			if (!(num <= 0f))
			{
				array[i] = (byte)Math.Clamp(px[i] / num * 255f + 0.5f, 0f, 255f);
				array[i + 1] = (byte)Math.Clamp(px[i + 1] / num * 255f + 0.5f, 0f, 255f);
				array[i + 2] = (byte)Math.Clamp(px[i + 2] / num * 255f + 0.5f, 0f, 255f);
				array[i + 3] = (byte)Math.Clamp(num * 255f + 0.5f, 0f, 255f);
			}
		}
		return array;
	}

	private static IGameTexture? Halo(ref IGameTexture? garde, string nom, byte r, byte g, byte b)
	{
		if (garde != null)
		{
			return garde;
		}
		byte[] array = new byte[16384];
		for (int i = 0; i < 64; i++)
		{
			for (int j = 0; j < 64; j++)
			{
				float num = ((float)j + 0.5f - 32f) / 32f;
				float num2 = ((float)i + 0.5f - 32f) / 32f;
				float num3 = MathF.Sqrt(num * num + num2 * num2);
				if (!(num3 >= 1f))
				{
					int num4 = (i * 64 + j) * 4;
					array[num4] = r;
					array[num4 + 1] = g;
					array[num4 + 2] = b;
					array[num4 + 3] = (byte)(255f * MathF.Pow(1f - num3, 2.4f));
				}
			}
		}
		return garde = Graphics.Renderer?.CreateTextureFromPixels("casse/halo/" + nom, 64, 64, array);
	}

	private static IGameTexture?[] Bouffees((byte R, byte G, byte B)[] tons, int graine, string nom)
	{
		IGameTexture[] array = new IGameTexture[3];
		for (int i = 0; i < 3; i++)
		{
			Mulberry mulberry = new Mulberry(graine + i * 31);
			List<(double X, double Y, double R)> lobes = new List<(double, double, double)> { (20.0, 23.0, 10.5) };
			for (int j = 0; j < 4; j++)
			{
				double num = -Math.PI * (0.08 + 0.84 * mulberry.Suivant());
				double num2 = 7.0 + mulberry.Suivant() * 4.0;
				lobes.Add((20.0 + Math.Cos(num) * num2 * 1.25, 23.0 + Math.Sin(num) * num2, 5.5 + mulberry.Suivant() * 3.5));
			}
			byte[] array2 = new byte[6400];
			for (int k = 0; k < 40; k++)
			{
				for (int l = 0; l < 40; l++)
				{
					if (!Dedans(l, k))
					{
						continue;
					}
					bool flag = !Dedans(l - 1, k) || !Dedans(l + 1, k) || !Dedans(l, k - 1) || !Dedans(l, k + 1);
					if (!flag || !(Hash01(l * 31 + k * 17 + i * 7 + graine) < 0.4))
					{
						bool flag2 = !Dedans(l - 3, k - 3);
						bool flag3 = !Dedans(l + 2, k + 3);
						byte b;
						byte b2;
						byte b3;
						if (flag2)
						{
							(b, b2, b3) = tons[0];
						}
						else if (flag3)
						{
							(b, b2, b3) = tons[2];
						}
						else
						{
							(b, b2, b3) = tons[1];
						}
						int num3 = (k * 40 + l) * 4;
						array2[num3] = b;
						array2[num3 + 1] = b2;
						array2[num3 + 2] = b3;
						array2[num3 + 3] = (byte)(flag ? 150u : 255u);
					}
				}
			}
			array[i] = Graphics.Renderer?.CreateTextureFromPixels($"casse/bouffee/{nom}/{i}", 40, 40, array2);
			bool Dedans(int x, int y)
			{
				return lobes.Any(((double X, double Y, double R) tuple4) => ((double)x - tuple4.X) * ((double)x - tuple4.X) + ((double)y - tuple4.Y) * ((double)y - tuple4.Y) <= tuple4.R * tuple4.R);
			}
		}
		return array;
	}

	private static Braises? BraisesDe(string image)
	{
		if (BraisesDes.TryGetValue(image, out Braises value))
		{
			return value;
		}
		IGameTexture texture = GameContentManager.Current.GetTexture(TextureType.Image, image);
		if (texture == null || !PixelsPlanche.Lire(texture, out Color[] pixels) || pixels.Length < texture.Width * texture.Height)
		{
			return null;
		}
		int L = texture.Width;
		int height = texture.Height;
		byte[] array = new byte[L * height * 4];
		int num = 0;
		for (int i = 0; i < L * height; i++)
		{
			Color color = pixels[i];
			if (color.A >= 250 && color.R - color.B >= 100 && !((float)(int)color.G > (float)(int)color.R * 0.8f))
			{
				float num2 = Math.Clamp((float)(color.R - 150) / 90f, 0f, 1f);
				if (!(num2 <= 0f))
				{
					array[i * 4] = byte.MaxValue;
					array[i * 4 + 1] = 168;
					array[i * 4 + 2] = 64;
					array[i * 4 + 3] = (byte)Math.Round(255f * num2);
					num++;
				}
			}
		}
		Braises braises = new Braises
		{
			L = L,
			H = height
		};
		if (num > 0)
		{
			braises.Lueur = Graphics.Renderer?.CreateTextureFromPixels("casse/braises/" + image, L, height, array);
			bool[] array2 = new bool[L * height];
			Stack<int> stack = new Stack<int>();
			for (int j = 0; j < L * height; j++)
			{
				if (array2[j] || array[j * 4 + 3] <= 100)
				{
					continue;
				}
				List<int> list = new List<int>();
				array2[j] = true;
				stack.Push(j);
				while (stack.Count > 0)
				{
					int num3 = stack.Pop();
					list.Add(num3);
					int num4 = num3 % L;
					int num5 = num3 / L;
					for (int k = -1; k <= 1; k++)
					{
						for (int l = -1; l <= 1; l++)
						{
							int num6 = num4 + l;
							int num7 = num5 + k;
							if (num6 >= 0 && num7 >= 0 && num6 < L && num7 < height)
							{
								int num8 = num7 * L + num6;
								if (!array2[num8] && array[num8 * 4 + 3] > 100)
								{
									array2[num8] = true;
									stack.Push(num8);
								}
							}
						}
					}
				}
				if (list.Count >= 5)
				{
					int haut = list.Min((int num9) => num9 / L);
					List<int> source = list.Where((int num9) => num9 / L <= haut + 2).ToList();
					braises.Amas.Add(((int)Math.Round(source.Average((int num9) => num9 % L)), haut + 1, list.Count));
				}
			}
			braises.Amas.Sort(((int X, int Y, int N) p, (int X, int Y, int N) q) => q.N.CompareTo(p.N));
		}
		BraisesDes[image] = braises;
		return braises;
	}

	private static ((int X, int Y) Cime, (int X, int Y)? Gauche, (int X, int Y)? Droite, (int X, int Y)? Pied) TachesDeLArbre()
	{
		Braises braises = BraisesDe("casse_arbre_calcine.png");
		if (braises != null)
		{
			List<(int, int, int)> amas = braises.Amas;
			if (amas != null && amas.Count > 0)
			{
				(int X, int Y, int N) cime = braises.Amas.MinBy(((int X, int Y, int N) a) => a.Y);
				List<(int X, int Y, int N)> source = braises.Amas.Where(((int X, int Y, int N) a) => a.Y < 50 && Math.Abs(a.X - cime.X) > 16).ToList();
				(int, int)? item = (from a in source
					where a.X < cime.X
					orderby a.X
					select (a.X, a.Y)).FirstOrDefault();
				(int, int)? item2 = (from a in source
					where a.X > cime.X
					orderby a.X descending
					select (a.X, a.Y)).FirstOrDefault();
				(int, int)? item3 = ((IEnumerable<(int, int, int)>)(from a in braises.Amas
					where a.Y > 70
					orderby a.N descending
					select a)).Select((Func<(int, int, int), (int, int)?>)(((int X, int Y, int N) a) => (a.X, a.Y))).FirstOrDefault();
				return (Cime: (cime.X, cime.Y), Gauche: item, Droite: item2, Pied: item3);
			}
		}
		return (Cime: (46, 22), Gauche: (28, 36), Droite: (64, 36), Pied: (46, 80));
	}

	private static void OublierImagesDuFeu()
	{
		BraisesDes.Clear();
	}

	private static float[] ProfondeurDe(byte[] arbre, int L, int H)
	{
		int[] d = new int[L * H];
		for (int i = 0; i < d.Length; i++)
		{
			d[i] = ((arbre[i * 4 + 3] >= 128) ? 1073741823 : 0);
		}
		for (int j = 0; j < H; j++)
		{
			for (int k = 0; k < L; k++)
			{
				if (d[j * L + k] > 0)
				{
					d[j * L + k] = Math.Min(d[j * L + k], Math.Min(Math.Min(V(k - 1, j) + 3, V(k, j - 1) + 3), Math.Min(V(k - 1, j - 1) + 4, V(k + 1, j - 1) + 4)));
				}
			}
		}
		for (int num = H - 1; num >= 0; num--)
		{
			for (int num2 = L - 1; num2 >= 0; num2--)
			{
				if (d[num * L + num2] > 0)
				{
					d[num * L + num2] = Math.Min(d[num * L + num2], Math.Min(Math.Min(V(num2 + 1, num) + 3, V(num2, num + 1) + 3), Math.Min(V(num2 + 1, num + 1) + 4, V(num2 - 1, num + 1) + 4)));
				}
			}
		}
		int plus = Math.Max(1, d.Max());
		return d.Select((int v) => (float)v / (float)plus).ToArray();
		int V(int x, int y)
		{
			if (x >= 0 && y >= 0 && x < L && y < H)
			{
				return d[y * L + x];
			}
			return 0;
		}
	}

	internal static Foret? ForetGardee(MapInstance carte, int objet, CasseObjet o)
	{
		if (Forets.TryGetValue((carte.Id, objet), out Foret value))
		{
			return value;
		}
		bool definitive;
		Foret foret = ForetDe(carte, o, out definitive);
		if (definitive)
		{
			Forets[(carte.Id, objet)] = foret;
		}
		return foret;
	}

	internal static Foret? ForetDe(MapInstance carte, CasseObjet o, out bool definitive)
	{
		Dictionary<string, IGameTexture> planches = new Dictionary<string, IGameTexture>(StringComparer.OrdinalIgnoreCase);
		IGameTexture value;
		byte[] tuile;
		return ForetDe(o, delegate(string couche, int x, int y)
		{
			if (!carte.Layers.TryGetValue(couche, out Tile[,] grille) || x < 0 || y < 0 || x >= grille.GetLength(0) || y >= grille.GetLength(1))
			{
				return ((string Planche, int X, int Y)?)null;
			}
			Tile tile = grille[x, y];
			if (!(tile.TilesetTexture is IGameTexture gameTexture))
			{
				return ((string Planche, int X, int Y)?)null;
			}
			string fileName = Path.GetFileName(gameTexture.Name);
			planches[fileName] = gameTexture;
			return (fileName, tile.X, tile.Y);
		}, (string nom, int tx, int ty) => (!planches.TryGetValue(nom, out value) || !TuileDe(value, tx, ty, out tuile)) ? (Pret: false, Tuile: null) : (Pret: true, Tuile: tuile), out definitive);
	}

	internal static Foret? ForetDe(CasseObjet o, Func<string, int, int, (string Planche, int X, int Y)?> tuileDe, Func<string, int, int, (bool Pret, byte[]? Tuile)> lirePlanche, out bool definitive)
	{
		definitive = true;
		CasseGenre genre = o.Genre;
		if ((uint)(genre - 3) > 1u)
		{
			return null;
		}
		int num = o.Largeur * Case;
		int num2 = o.Hauteur * Case;
		int num3 = o.X * Case;
		int num4 = o.Y * Case;
		int num5 = num3 + num;
		int num6 = num4 + num2;
		MotifDeForet motif = null;
		Dictionary<string, bool[]> dictionary = new Dictionary<string, bool[]>();
		Dictionary<string, bool[]> dictionary2 = new Dictionary<string, bool[]>();
		(string, int, int)[] cases = o.Cases;
		for (int i = 0; i < cases.Length; i++)
		{
			(string, int, int) tuple = cases[i];
			string item = tuple.Item1;
			int item2 = tuple.Item2;
			int item3 = tuple.Item3;
			(string, int, int)? tuple2 = tuileDe(item, item2, item3);
			if (!tuple2.HasValue)
			{
				continue;
			}
			(string Planche, int X, int Y) tuile = tuple2.GetValueOrDefault();
			MotifDeForet motifDeForet = Motifs.FirstOrDefault((MotifDeForet m) => (motif == null || motif == m) && PieceDe(m, tuile) != null);
			if ((object)motifDeForet == null)
			{
				continue;
			}
			motif = motifDeForet;
			if (!dictionary.TryGetValue(item, out var value))
			{
				value = (dictionary[item] = new bool[num * num2]);
			}
			bool[] value2 = null;
			if (PieceDe(motifDeForet, tuile) == PieceSeule(motifDeForet) && !dictionary2.TryGetValue(item, out value2))
			{
				value2 = (dictionary2[item] = new bool[num * num2]);
			}
			int num7 = (item2 - o.X) * Case;
			int num8 = (item3 - o.Y) * Case;
			for (int num9 = 0; num9 < Case && num8 + num9 < num2; num9++)
			{
				for (int num10 = 0; num10 < Case && num7 + num10 < num; num10++)
				{
					value[(num8 + num9) * num + num7 + num10] = true;
					if (value2 != null)
					{
						value2[(num8 + num9) * num + num7 + num10] = true;
					}
				}
			}
		}
		if (motif == null)
		{
			return null;
		}
		HashSet<(int X, int Y)> hashSet = new HashSet<(int X, int Y)>();
		HashSet<(int, int)> hashSet2 = new HashSet<(int, int)>();
		PieceDeForet pieceDeForet = PieceSeule(motif);
		foreach (string item6 in Options.Instance.Map.Layers.All)
		{
			for (int num11 = o.Y - 3; num11 < o.Y + o.Hauteur + 3; num11++)
			{
				for (int num12 = o.X - 3; num12 < o.X + o.Largeur + 3; num12++)
				{
					(string, int, int)? tuple2 = tuileDe(item6, num12, num11);
					if (!tuple2.HasValue)
					{
						continue;
					}
					(string, int, int) valueOrDefault = tuple2.GetValueOrDefault();
					PieceDeForet pieceDeForet2 = PieceDe(motif, valueOrDefault);
					if ((object)pieceDeForet2 == null)
					{
						continue;
					}
					int num13 = (num12 - (valueOrDefault.Item2 - pieceDeForet2.TX)) * Case;
					int num14 = (num11 - (valueOrDefault.Item3 - pieceDeForet2.TY)) * Case;
					int num15 = ((pieceDeForet2.Periode > 0) ? 1 : 0);
					(int, int)[] arbres = pieceDeForet2.Arbres;
					for (int num16 = 0; num16 < arbres.Length; num16++)
					{
						(int, int) tuple3 = arbres[num16];
						int item4 = tuple3.Item1;
						int item5 = tuple3.Item2;
						for (int num17 = -num15; num17 <= num15; num17++)
						{
							for (int num18 = -num15; num18 <= num15; num18++)
							{
								int num19 = num13 + item4 - pieceDeForet2.TX * Case + num18 * pieceDeForet2.Periode;
								int num20 = num14 + item5 - pieceDeForet2.TY * Case + num17 * pieceDeForet2.Periode;
								if ((num15 == 0 || (num19 + motif.ArbreL > num13 && num19 < num13 + pieceDeForet2.L * Case && num20 + motif.ArbreH > num14 && num20 < num14 + pieceDeForet2.H * Case)) && num19 + motif.ArbreL > num3 && num19 < num5 && num20 + motif.ArbreH > num4 && num20 < num6)
								{
									hashSet.Add((num19, num20));
									if (pieceDeForet2 == pieceDeForet)
									{
										hashSet2.Add((num19, num20));
									}
								}
							}
						}
					}
				}
			}
		}
		byte[] array3 = ArbreDuMotif(lirePlanche, motif);
		if (array3 == null)
		{
			definitive = false;
			return null;
		}
		(int, int)[] array4 = (from a in hashSet
			orderby a.Y descending, a.X
			select a).ToArray();
		return new Foret
		{
			Motif = motif,
			Arbre = array3,
			Arbres = array4,
			DuMotif = dictionary,
			DuSeul = dictionary2,
			Seul = array4.Select(hashSet2.Contains).ToArray(),
			Tuiles = ((pieceDeForet == null) ? default((int, int, int, int)) : (X: motif.ArbreX - pieceDeForet.TX * Case, Y: motif.ArbreY - pieceDeForet.TY * Case, L: pieceDeForet.L * Case, H: pieceDeForet.H * Case))
		};
		static PieceDeForet? PieceDe(MotifDeForet m, (string Planche, int X, int Y) tuple4)
		{
			if (!string.Equals(tuple4.Planche, m.Planche, StringComparison.OrdinalIgnoreCase))
			{
				return null;
			}
			return m.Pieces.FirstOrDefault((PieceDeForet p) => tuple4.X >= p.TX && tuple4.X < p.TX + p.L && tuple4.Y >= p.TY && tuple4.Y < p.TY + p.H);
		}
		static PieceDeForet? PieceSeule(MotifDeForet m)
		{
			return m.Pieces.FirstOrDefault((PieceDeForet p) => p.Periode == 0 && m.ArbreX >= p.TX * Case && m.ArbreX < (p.TX + p.L) * Case && m.ArbreY >= p.TY * Case && m.ArbreY < (p.TY + p.H) * Case);
		}
	}

	private static byte[]? ArbreDuMotif(Func<string, int, int, (bool Pret, byte[]? Tuile)> lirePlanche, MotifDeForet motif)
	{
		if (ArbresDesMotifs.TryGetValue(motif.Planche, out byte[] value))
		{
			return value;
		}
		byte[] array = new byte[motif.ArbreL * motif.ArbreH * 4];
		int num = motif.ArbreX / Case;
		int num2 = motif.ArbreY / Case;
		int num3 = (motif.ArbreX + motif.ArbreL - 1) / Case;
		int num4 = (motif.ArbreY + motif.ArbreH - 1) / Case;
		for (int i = num2; i <= num4; i++)
		{
			for (int j = num; j <= num3; j++)
			{
				var (flag, array2) = lirePlanche(motif.Planche, j, i);
				if (!flag)
				{
					return null;
				}
				if (array2 == null)
				{
					continue;
				}
				for (int k = 0; k < Case; k++)
				{
					for (int l = 0; l < Case; l++)
					{
						int num5 = j * Case + l - motif.ArbreX;
						int num6 = i * Case + k - motif.ArbreY;
						if (num5 >= 0 && num6 >= 0 && num5 < motif.ArbreL && num6 < motif.ArbreH)
						{
							int num7 = (k * Case + l) * 4;
							int num8 = (num6 * motif.ArbreL + num5) * 4;
							array[num8] = array2[num7];
							array[num8 + 1] = array2[num7 + 1];
							array[num8 + 2] = array2[num7 + 2];
							array[num8 + 3] = array2[num7 + 3];
						}
					}
				}
			}
		}
		ArbresDesMotifs[motif.Planche] = array;
		return array;
	}

	private static MapInstance? CarteSous(float x, float y)
	{
		float largeur = Options.Instance.Map.MapWidth * Options.Instance.Map.TileWidth;
		float hauteur = Options.Instance.Map.MapHeight * Options.Instance.Map.TileHeight;
		MapInstance derniereCarte = _derniereCarte;
		if (derniereCarte != null && !derniereCarte.IsDisposed && derniereCarte.IsLoaded && Dedans(derniereCarte))
		{
			return derniereCarte;
		}
		foreach (IDatabaseObject value in MapInstance.Lookup.Values)
		{
			if (value is MapInstance { IsDisposed: false, IsLoaded: not false } mapInstance && Dedans(mapInstance))
			{
				_derniereCarte = mapInstance;
				return mapInstance;
			}
		}
		return null;
		bool Dedans(MapInstance carte)
		{
			if (x >= (float)carte.X && y >= (float)carte.Y && x < (float)carte.X + largeur)
			{
				return y < (float)carte.Y + hauteur;
			}
			return false;
		}
	}

	private static int PartieTouchee(CasseCarte reperee, int objet, float lx, float ly)
	{
		return reperee.PartiePres(objet, lx, ly);
	}

	internal static HashSet<(Guid Carte, int Objet)>? ObjetsSous(float x, float y)
	{
		MapInstance mapInstance = CarteSous(x, y);
		if (mapInstance != null)
		{
			CasseCarte casseCarte = CarteDe(mapInstance);
			if (casseCarte != null)
			{
				float num = x - (float)mapInstance.X;
				float num2 = y - (float)mapInstance.Y;
				if (!casseCarte.ParCase.TryGetValue(((int)(num / (float)Case), (int)(num2 / (float)Case)), out var value))
				{
					return null;
				}
				HashSet<(Guid, int)> hashSet = null;
				int[] array = value;
				foreach (int num3 in array)
				{
					if (casseCarte.Objets[num3].Opaque(num, num2, Case))
					{
						(hashSet ?? (hashSet = new HashSet<(Guid, int)>())).Add((mapInstance.Id, num3));
					}
				}
				return hashSet;
			}
		}
		return null;
	}

	internal static bool CaseBloquee(float x, float y)
	{
		MapInstance mapInstance = CarteSous(x, y);
		bool flag3;
		if (mapInstance != null)
		{
			MapAttribute[,] attributes = mapInstance.Attributes;
			if (attributes != null)
			{
				int num = (int)((x - (float)mapInstance.X) / (float)Case);
				int num2 = (int)((y - (float)mapInstance.Y) / (float)Options.Instance.Map.TileHeight);
				bool flag = num < 0 || num2 < 0 || num >= attributes.GetLength(0) || num2 >= attributes.GetLength(1);
				bool flag2 = flag;
				if (!flag2)
				{
					MapAttribute mapAttribute = attributes[num, num2];
					flag3 = ((mapAttribute is MapBlockedAttribute || mapAttribute is MapAnimationAttribute { IsBlock: not false }) ? true : false);
					flag2 = !flag3;
				}
				if (flag2)
				{
					return false;
				}
				CasseCarte casseCarte = CarteDe(mapInstance);
				if (casseCarte != null)
				{
					int num3 = casseCarte.ObjetA(num, num2);
					if (num3 >= 0 && casseCarte.Objets[num3].Genre != CasseGenre.Poubelle)
					{
						int partie = PartieTouchee(casseCarte, num3, ((float)num + 0.5f) * (float)Case, ((float)num2 + 0.5f) * (float)Options.Instance.Map.TileHeight);
						CasseEtat? casseEtat = EtatDe(mapInstance.Id, num3, partie)?.Etat;
						if (casseEtat.HasValue)
						{
							CasseEtat valueOrDefault = casseEtat.GetValueOrDefault();
							if (valueOrDefault - 2 <= CasseEtat.Abime)
							{
								flag3 = true;
								goto IL_0163;
							}
						}
						flag3 = false;
						goto IL_0163;
					}
				}
				return true;
			}
		}
		return false;
		IL_0163:
		return !flag3;
	}

	internal static bool PremierImpact(float x0, float y0, float x1, float y1, HashSet<(Guid Carte, int Objet)>? sous, out float impactX, out float impactY, bool traverseLesArbres = false)
	{
		impactX = x1;
		impactY = y1;
		if (!Reglages.Active)
		{
			return false;
		}
		float num = MathF.Sqrt((x1 - x0) * (x1 - x0) + (y1 - y0) * (y1 - y0));
		int num2 = Math.Max(1, (int)MathF.Ceiling(num / 4f));
		for (int i = 1; i <= num2; i++)
		{
			float num3 = x0 + (x1 - x0) * (float)i / (float)num2;
			float num4 = y0 + (y1 - y0) * (float)i / (float)num2;
			MapInstance mapInstance = CarteSous(num3, num4);
			if (mapInstance == null)
			{
				continue;
			}
			CasseCarte casseCarte = CarteDe(mapInstance);
			if (casseCarte == null)
			{
				continue;
			}
			float num5 = num3 - (float)mapInstance.X;
			float num6 = num4 - (float)mapInstance.Y;
			int num7 = casseCarte.ObjetAuPixel(num5, num6, Case);
			if (num7 < 0 || (sous != null && sous.Contains((mapInstance.Id, num7))))
			{
				continue;
			}
			CasseEtat? casseEtat = EtatDe(mapInstance.Id, num7, PartieTouchee(casseCarte, num7, num5, num6))?.Etat;
			bool flag;
			if (casseEtat.HasValue)
			{
				CasseEtat valueOrDefault = casseEtat.GetValueOrDefault();
				if (valueOrDefault - 2 <= CasseEtat.Abime)
				{
					flag = true;
					goto IL_0129;
				}
			}
			flag = false;
			goto IL_0129;
			IL_0129:
			if (!flag || casseCarte.Objets[num7].Genre == CasseGenre.Poubelle)
			{
				bool flag2 = traverseLesArbres;
				bool flag3 = flag2;
				if (flag3)
				{
					CasseGenre genre = casseCarte.Objets[num7].Genre;
					flag = (uint)(genre - 2) <= 2u;
					flag3 = flag;
				}
				if (!flag3)
				{
					impactX = num3;
					impactY = num4;
					return true;
				}
			}
		}
		return false;
	}

	internal static bool LaissePasser(Guid carteId, int x, int y, bool perce)
	{
		bool flag5;
		bool flag3;
		if (MapInstance.TryGet(carteId, out MapInstance instance))
		{
			CasseCarte casseCarte = CarteDe(instance);
			if (casseCarte != null)
			{
				int num = casseCarte.ObjetA(x, y);
				if (num >= 0)
				{
					CasseGenre genre = casseCarte.Objets[num].Genre;
					bool flag = perce;
					bool flag2 = flag;
					if (flag2)
					{
						flag3 = (uint)(genre - 2) <= 2u;
						flag2 = flag3;
					}
					if (flag2)
					{
						return true;
					}
					int partie = PartieTouchee(casseCarte, num, ((float)x + 0.5f) * (float)Case, ((float)y + 0.5f) * (float)Options.Instance.Map.TileHeight);
					bool flag4 = genre != CasseGenre.Poubelle;
					flag5 = flag4;
					if (flag5)
					{
						CasseEtat? casseEtat = EtatDe(carteId, num, partie)?.Etat;
						if (casseEtat.HasValue)
						{
							CasseEtat valueOrDefault = casseEtat.GetValueOrDefault();
							if (valueOrDefault - 2 <= CasseEtat.Abime)
							{
								flag3 = true;
								goto IL_00dd;
							}
						}
						flag3 = false;
						goto IL_00dd;
					}
					goto IL_00e1;
				}
			}
		}
		return false;
		IL_00e1:
		return flag5;
		IL_00dd:
		flag5 = flag3;
		goto IL_00e1;
	}

	internal static bool TombeIci(MapInstance carte, float x, float y)
	{
		if (!(x < 0f) && !(y < 0f))
		{
			CasseCarte casseCarte = CarteDe(carte);
			if (casseCarte != null)
			{
				Dictionary<int, List<(int, EtatPartie)>> dictionary = ObjetsTouches(carte.Id);
				if (dictionary.Count == 0 || !casseCarte.ParCase.TryGetValue(((int)(x / (float)Case), (int)(y / (float)Case)), out var value))
				{
					return false;
				}
				int[] array = value;
				foreach (int num in array)
				{
					if (dictionary.TryGetValue(num, out var value2) && EstTombe(casseCarte, num, value2))
					{
						return true;
					}
				}
				return false;
			}
		}
		return false;
	}

	internal static bool TombeIci(MapInstance carte, float x, float y, float largeur, float hauteur)
	{
		return TombeIci(carte, x + largeur / 2f, y + hauteur / 2f);
	}

	private static bool EstTombe(CasseCarte reperee, int objet, List<(int Partie, EtatPartie Etat)> parties)
	{
		bool flag = objet >= 0 && objet < reperee.Objets.Length;
		bool flag2 = flag;
		if (flag2)
		{
			CasseGenre genre = reperee.Objets[objet].Genre;
			bool flag3 = ((genre == CasseGenre.Foret || genre == CasseGenre.Poubelle) ? true : false);
			flag2 = !flag3;
		}
		if (flag2)
		{
			return parties.Any<(int, EtatPartie)>(((int Partie, EtatPartie Etat) p) => p.Etat.Etat == CasseEtat.Casse);
		}
		return false;
	}

	private static bool TombePret(CasseCarte reperee, int objet, List<(int Partie, EtatPartie Etat)> parties, Guid carte, out Rendu? rendu)
	{
		rendu = null;
		if (EstTombe(reperee, objet, parties) && Rendus.TryGetValue((carte, objet), out rendu) && rendu.Faite)
		{
			return rendu.Pixels.Count > 0;
		}
		return false;
	}

	internal static int SignatureDesTombes(MapInstance carte)
	{
		if (SignaturesDesTombes.TryGetValue(carte.Id, out (long, int) value) && value.Item1 == _image)
		{
			return value.Item2;
		}
		int num = 0;
		CasseCarte casseCarte = CarteDe(carte);
		if (casseCarte != null)
		{
			foreach (var (num3, parties) in ObjetsTouches(carte.Id))
			{
				if (TombePret(casseCarte, num3, parties, carte.Id, out Rendu rendu) && rendu != null)
				{
					num = num * 31 + HashCode.Combine(num3, rendu.Version, rendu.Signature, rendu.PasDuFeu) + 1;
				}
			}
		}
		SignaturesDesTombes[carte.Id] = (_image, num);
		return num;
	}

	internal static (HashSet<(string Couche, int X, int Y)> Cases, List<DessinTombe> Dessins) DessinsDesTombes(MapInstance carte)
	{
		HashSet<(string, int, int)> hashSet = new HashSet<(string, int, int)>();
		List<DessinTombe> list = new List<DessinTombe>();
		CasseCarte casseCarte = CarteDe(carte);
		if (casseCarte == null)
		{
			return (Cases: hashSet, Dessins: list);
		}
		foreach (var (num2, parties) in ObjetsTouches(carte.Id))
		{
			if (!TombePret(casseCarte, num2, parties, carte.Id, out Rendu rendu) || rendu == null)
			{
				continue;
			}
			CasseObjet casseObjet = casseCarte.Objets[num2];
			(string, int, int)[] cases = casseObjet.Cases;
			foreach ((string, int, int) item in cases)
			{
				hashSet.Add(item);
			}
			int num3 = casseObjet.Largeur * Case;
			int num4 = casseObjet.Hauteur * Case;
			foreach (var (couche, rgba) in rendu.Pixels)
			{
				list.Add(new DessinTombe(rgba, null, num3, num4, 0, 0, num3, num4, casseObjet.X * Case, casseObjet.Y * Case, couche));
			}
			foreach (Decor decor in rendu.Decors)
			{
				if (string.Equals(decor.Image, "casse_arbre_calcine.png", StringComparison.OrdinalIgnoreCase))
				{
					IGameTexture texture = GameContentManager.Current.GetTexture(TextureType.Image, decor.Image);
					if (texture != null)
					{
						list.Add(new DessinTombe(null, texture, texture.Width, texture.Height, (int)decor.Source.X, (int)decor.Source.Y, (int)decor.Source.Width, (int)decor.Source.Height, (int)Math.Round(decor.X), (int)Math.Round(decor.Y), decor.Couche));
					}
				}
			}
		}
		return (Cases: hashSet, Dessins: list);
	}

	internal static bool LampadaireTombe(MapInstance carte, int x, int y)
	{
		CasseCarte casseCarte = CarteDe(carte);
		if (casseCarte == null)
		{
			return false;
		}
		Dictionary<int, List<(int, EtatPartie)>> dictionary = ObjetsTouches(carte.Id);
		if (dictionary.Count == 0)
		{
			return false;
		}
		_003C_003Ey__InlineArray4<(int, int)> buffer = default(_003C_003Ey__InlineArray4<(int, int)>);
		global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<_003C_003Ey__InlineArray4<(int, int)>, (int, int)>(ref buffer, 0) = (x + 1, y + 1);
		global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<_003C_003Ey__InlineArray4<(int, int)>, (int, int)>(ref buffer, 1) = (x, y + 1);
		global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<_003C_003Ey__InlineArray4<(int, int)>, (int, int)>(ref buffer, 2) = (x + 1, y + 4);
		global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<_003C_003Ey__InlineArray4<(int, int)>, (int, int)>(ref buffer, 3) = (x, y + 4);
		ReadOnlySpan<(int, int)> readOnlySpan = global::_003CPrivateImplementationDetails_003E.InlineArrayAsReadOnlySpan<_003C_003Ey__InlineArray4<(int, int)>, (int, int)>(in buffer, 4);
		ReadOnlySpan<(int, int)> readOnlySpan2 = readOnlySpan;
		for (int i = 0; i < readOnlySpan2.Length; i++)
		{
			var (item, item2) = readOnlySpan2[i];
			if (!casseCarte.ParCase.TryGetValue((item, item2), out var value))
			{
				continue;
			}
			int[] array = value;
			foreach (int num in array)
			{
				if (casseCarte.Objets[num].Genre != CasseGenre.Petit)
				{
					continue;
				}
				if (dictionary.TryGetValue(num, out var value2))
				{
					return value2.Any<(int, EtatPartie)>(((int Partie, EtatPartie Etat) p) => p.Etat.Etat == CasseEtat.Casse);
				}
				return false;
			}
		}
		return false;
	}

	internal static bool Dehors(CasseObjet o, int partie)
	{
		return o.Dehors(partie, Options.Instance.Map.MapWidth * Case, Options.Instance.Map.MapHeight * Case);
	}

	private static byte[] Aplatir(Dictionary<string, byte[]> couches, List<string> ordre, int L, int H)
	{
		byte[] array = new byte[L * H * 4];
		foreach (KeyValuePair<string, byte[]> item in couches.OrderBy<KeyValuePair<string, byte[]>, int>((KeyValuePair<string, byte[]> paire) => ordre.IndexOf(paire.Key)))
		{
			item.Deconstruct(out var _, out var value);
			byte[] array2 = value;
			for (int num = 0; num < array.Length; num += 4)
			{
				byte b = array2[num + 3];
				if (b != 0)
				{
					float num2 = (float)(int)b / 255f;
					array[num] = (byte)((float)(int)array2[num] * num2 + (float)(int)array[num] * (1f - num2));
					array[num + 1] = (byte)((float)(int)array2[num + 1] * num2 + (float)(int)array[num + 1] * (1f - num2));
					array[num + 2] = (byte)((float)(int)array2[num + 2] * num2 + (float)(int)array[num + 2] * (1f - num2));
					array[num + 3] = Math.Max(array[num + 3], b);
				}
			}
		}
		return array;
	}

	private static void EnMorceaux(MapInstance carte, CasseCarte reperee, int objet, float impactX, long maintenant)
	{
		if (Graphics.Renderer == null || objet < 0 || objet >= reperee.Objets.Length)
		{
			return;
		}
		CasseObjet casseObjet = reperee.Objets[objet];
		List<(int Partie, EtatPartie Etat)> parties = ObjetsTouches(carte.Id).GetValueOrDefault(objet);
		if (parties == null || parties.Count == 0)
		{
			return;
		}
		int num = casseObjet.Largeur * Case;
		int num2 = casseObjet.Hauteur * Case;
		FloatRect worldViewport = Graphics.WorldViewport;
		float num3 = carte.X + casseObjet.X * Case;
		float num4 = carte.Y + casseObjet.Y * Case;
		if (num3 + (float)num < worldViewport.Left - 32f || num3 > worldViewport.Right + 32f || num4 + (float)num2 < worldViewport.Top - 32f || num4 > worldViewport.Bottom + 32f)
		{
			return;
		}
		List<string> ordre = Options.Instance.Map.Layers.All;
		int num5 = (reperee.Id.GetHashCode() * 397) ^ (objet * 7919);
		Dictionary<string, byte[]> dictionary = Composer(carte, casseObjet);
		if (dictionary == null || dictionary.Count <= 0)
		{
			return;
		}
		Dictionary<string, byte[]> dictionary2 = Composer(carte, casseObjet);
		if (dictionary2 == null)
		{
			return;
		}
		Abimer(casseObjet, dictionary, Copie(CasseEtat.Abime), (string c) => ordre.IndexOf(c), num5, maintenant, Reglages.FeuDureeMs, Array.Empty<Zone>());
		Abimer(casseObjet, dictionary2, Copie(CasseEtat.Casse), (string c) => ordre.IndexOf(c), num5, maintenant, Reglages.FeuDureeMs, Array.Empty<Zone>());
		byte[] array = Aplatir(dictionary, ordre, num, num2);
		byte[] array2 = Aplatir(dictionary2, ordre, num, num2);
		List<int> list = new List<int>();
		for (int num6 = 0; num6 < num * num2; num6++)
		{
			if (array[num6 * 4 + 3] >= 128 && array2[num6 * 4 + 3] < 128)
			{
				list.Add(num6);
			}
		}
		if (list.Count < 60)
		{
			return;
		}
		int[] array3 = new int[num];
		for (int num7 = 0; num7 < num; num7++)
		{
			array3[num7] = num2 - 4;
			for (int num8 = num2 / 2; num8 < num2; num8++)
			{
				if (array2[(num8 * num + num7) * 4 + 3] >= 128)
				{
					array3[num7] = num8;
					break;
				}
			}
		}
		Random random = new Random(num5 ^ (int)(maintenant & 0x7FFFFFFF));
		int num9 = Math.Clamp(list.Count / 350, 12, 40);
		(int, int)[] array4 = new(int, int)[num9];
		for (int num10 = 0; num10 < num9; num10++)
		{
			int num11 = list[random.Next(list.Count)];
			array4[num10] = (num11 % num, num11 / num);
		}
		List<int>[] array5 = new List<int>[num9];
		for (int num12 = 0; num12 < num9; num12++)
		{
			array5[num12] = new List<int>();
		}
		foreach (int item in list)
		{
			int num13 = item % num;
			int num14 = item / num;
			int num15 = 0;
			int num16 = int.MaxValue;
			for (int num17 = 0; num17 < num9; num17++)
			{
				int num18 = num13 - array4[num17].Item1;
				int num19 = num14 - array4[num17].Item2;
				int num20 = num18 * num18 + num19 * num19;
				if (num20 < num16)
				{
					num16 = num20;
					num15 = num17;
				}
			}
			array5[num15].Add(item);
		}
		float num21 = carte.X + casseObjet.X * Case;
		float num22 = carte.Y + casseObjet.Y * Case;
		float num23 = impactX - (float)(casseObjet.X * Case);
		for (int num24 = 0; num24 < num9; num24++)
		{
			List<int> list2 = array5[num24];
			if (list2.Count < 6)
			{
				continue;
			}
			int num25 = int.MaxValue;
			int num26 = int.MaxValue;
			int num27 = 0;
			int num28 = 0;
			foreach (int item2 in list2)
			{
				num25 = Math.Min(num25, item2 % num);
				num27 = Math.Max(num27, item2 % num);
				num26 = Math.Min(num26, item2 / num);
				num28 = Math.Max(num28, item2 / num);
			}
			int num29 = num27 - num25 + 1;
			int num30 = num28 - num26 + 1;
			byte[] array6 = new byte[num29 * num30 * 4];
			foreach (int item3 in list2)
			{
				int num31 = item3 % num - num25;
				int num32 = item3 / num - num26;
				int destinationIndex = (num32 * num29 + num31) * 4;
				Array.Copy(array, item3 * 4, array6, destinationIndex, 4);
			}
			IGameTexture gameTexture = Graphics.Renderer.CreateTextureFromPixels($"casse/morceau/{reperee.Id}/{objet}/{num24}/{maintenant}", num29, num30, array6);
			if (gameTexture != null)
			{
				float num33 = (float)(num25 + num27) / 2f;
				float num34 = (float)(num26 + num28) / 2f;
				bool flag = num34 < (float)num2 * 0.45f;
				float num35 = (flag ? (((float)num / 2f - num33) * 0.9f + (float)(random.NextDouble() - 0.5) * 20f) : ((float)Math.Sign(num33 - num23) * (10f + (float)random.NextDouble() * 30f)));
				float num36 = (flag ? ((0f - (float)random.NextDouble()) * 50f) : 0f);
				int num37 = Math.Clamp((int)(num33 + num35 * 0.8f), 0, num - 1);
				int num38 = Math.Max(array3[num37], num28 + 6);
				float num39 = Math.Max(1f, num38 - num28);
				float chute = (0f - num36 + MathF.Sqrt(num36 * num36 + 1200f * num39)) / 600f;
				Morceaux.Add(new Morceau
				{
					Texture = gameTexture,
					X0 = num21 + (float)num25,
					Y0 = num22 + (float)num26,
					L = num29,
					H = num30,
					Vx = num35,
					Vy = num36,
					Depart = maintenant + random.Next(0, 220) + (flag ? 70 : 0),
					Chute = chute
				});
			}
		}
		Player me = Globals.Me;
		if (me == null)
		{
			return;
		}
		MapInstance mapInstance = MapInstance.Get(me.MapId);
		if (mapInstance != null)
		{
			float num40 = mapInstance.X + me.X * Case;
			float num41 = mapInstance.Y + me.Y * Case;
			if (MathF.Abs(num40 - (num21 + (float)num / 2f)) < 700f && MathF.Abs(num41 - (num22 + (float)num2 / 2f)) < 500f)
			{
				CombatFeedback.StartShake(4f, 260L);
			}
		}
		List<(int Partie, EtatPartie Etat)> Copie(CasseEtat etat)
		{
			return parties.Select(delegate((int Partie, EtatPartie Etat) p)
			{
				EtatPartie etatPartie = new EtatPartie
				{
					Etat = etat,
					Points = ((etat != CasseEtat.Casse) ? 1 : 0),
					Max = p.Etat.Max,
					Depuis = p.Etat.Depuis
				};
				etatPartie.Coups.AddRange(p.Etat.Coups);
				return (Partie: p.Partie, copie: etatPartie);
			}).ToList();
		}
	}

	private static void DessinerMorceaux(long maintenant)
	{
		for (int num = Morceaux.Count - 1; num >= 0; num--)
		{
			Morceau morceau = Morceaux[num];
			if (morceau.Texture == null)
			{
				Morceaux.RemoveAt(num);
			}
			else
			{
				float num2 = (float)(maintenant - morceau.Depart) / 1000f;
				float x = morceau.X0;
				float num3 = morceau.Y0;
				float num4 = 1f;
				if (num2 > 0f)
				{
					float num5 = MathF.Min(num2, morceau.Chute);
					x = morceau.X0 + morceau.Vx * num5;
					num3 = morceau.Y0 + morceau.Vy * num5 + 300f * num5 * num5;
					if (num2 > morceau.Chute)
					{
						float num6 = (num2 - morceau.Chute) * 1000f;
						if (num6 < 160f)
						{
							num3 -= 4f * MathF.Sin((float)Math.PI * num6 / 160f);
						}
						else
						{
							num4 = 1f - (num6 - 160f) / 420f;
						}
					}
				}
				if (num4 <= 0f)
				{
					morceau.Texture.Dispose();
					Morceaux.RemoveAt(num);
				}
				else
				{
					Graphics.DrawGameTexture(morceau.Texture, new FloatRect(0f, 0f, morceau.L, morceau.H), new FloatRect(MathF.Round(x), MathF.Round(num3), morceau.L, morceau.H), Teinte(Color.White, num4));
				}
			}
		}
	}

	private static void DessinerCible(IGameTexture pixel, long maintenant)
	{
		RibbonContractEntryPacket ribbonContract = Globals.RibbonContract;
		if (ribbonContract == null || ribbonContract.Kind != RibbonContractKind.Sabotage || ribbonContract.Objet < 0)
		{
			return;
		}
		MapInstance mapInstance = MapInstance.Get(ribbonContract.TargetMapId);
		if (mapInstance == null)
		{
			return;
		}
		CasseCarte casseCarte = CarteDe(mapInstance);
		float cx;
		float cy;
		if (casseCarte != null && ribbonContract.Objet < casseCarte.Objets.Length)
		{
			List<(int, EtatPartie)> valueOrDefault = ObjetsTouches(mapInstance.Id).GetValueOrDefault(ribbonContract.Objet);
			if (valueOrDefault == null || !valueOrDefault.Any<(int, EtatPartie)>(((int Partie, EtatPartie Etat) p) => p.Etat.Etat == CasseEtat.Casse))
			{
				CasseObjet casseObjet = casseCarte.Objets[ribbonContract.Objet];
				cx = MathF.Round((float)mapInstance.X + ((float)casseObjet.X + (float)casseObjet.Largeur / 2f) * (float)Case);
				cy = MathF.Round((float)(mapInstance.Y + casseObjet.Y * Case - 18) + MathF.Sin((float)maintenant / 260f) * 4f);
				Color couleur = new Color(255, 236, 52, 44);
				Color couleur2 = new Color(200, 20, 6, 4);
				Croix(1f, 1f, couleur2);
				Croix(0f, 0f, couleur);
			}
		}
		void Croix(float ox, float oy, Color renderColor)
		{
			for (int i = -7; i <= 7; i++)
			{
				Graphics.DrawGameTexture(pixel, new FloatRect(0f, 0f, 1f, 1f), new FloatRect(cx + ox + (float)i - 1f, cy + oy + (float)i - 1f, 3f, 3f), renderColor);
				Graphics.DrawGameTexture(pixel, new FloatRect(0f, 0f, 1f, 1f), new FloatRect(cx + ox + (float)i - 1f, cy + oy - (float)i - 1f, 3f, 3f), renderColor);
			}
			for (int j = 0; j < 5; j++)
			{
				Graphics.DrawGameTexture(pixel, new FloatRect(0f, 0f, 1f, 1f), new FloatRect(cx + ox - (float)(4 - j), cy + oy + 11f + (float)j, (4 - j) * 2 + 1, 1f), renderColor);
			}
		}
	}

	private static void OublierMorceaux()
	{
		foreach (Morceau item in Morceaux)
		{
			item.Texture?.Dispose();
		}
		Morceaux.Clear();
	}

	internal static void DessinerDansLaLumiere()
	{
		if (FlammesVues.Count == 0 && EtincellesVues.Count == 0)
		{
			return;
		}
		double num = Secondes(Timing.Global.Milliseconds);
		long image = (long)Math.Floor(num * 1000.0 / 70.0);
		foreach (FlammeAuMonde flammesVue in FlammesVues)
		{
			(IGameTexture, FloatRect, float)? tuple = FlammeVue(flammesVue, num, image);
			if (tuple.HasValue)
			{
				(IGameTexture, FloatRect, float) valueOrDefault = tuple.GetValueOrDefault();
				Graphics.AddLightTexture(valueOrDefault.Item1, new FloatRect(0f, 0f, valueOrDefault.Item2.Width, valueOrDefault.Item2.Height), valueOrDefault.Item2, new Color((int)(255f * valueOrDefault.Item3), 255, 255, 255));
			}
		}
		IGameTexture gameTexture = Graphics.Renderer?.WhitePixel;
		if (gameTexture != null)
		{
			DessinerEtincelles(gameTexture, num, dansLaLumiere: true);
		}
	}

	internal static void AjouterLueurs(List<KiLight.Light> lumieres)
	{
		if (FlammesVues.Count == 0)
		{
			return;
		}
		double num = Secondes(Timing.Global.Milliseconds);
		FloatRect worldViewport = Graphics.WorldViewport;
		int num2 = 0;
		Flamme[] array = new Flamme[5]
		{
			Flamme.XL,
			Flamme.L,
			Flamme.M,
			Flamme.G,
			Flamme.S
		};
		foreach (Flamme flamme in array)
		{
			var (num3, num4) = flamme switch
			{
				Flamme.XL => (86f, 1f), 
				Flamme.L => (62f, 0.8f), 
				Flamme.M => (40f, 0.42f), 
				Flamme.G => (30f, 0.34f), 
				_ => (24f, 0.28f), 
			};
			foreach (FlammeAuMonde flammesVue in FlammesVues)
			{
				if (flammesVue.Taille == flamme && !(num < flammesVue.T0) && !(num >= flammesVue.T1) && !(flammesVue.X < worldViewport.Left - num3) && !(flammesVue.X > worldViewport.Right + num3) && !(flammesVue.Y < worldViewport.Top - num3) && !(flammesVue.Y > worldViewport.Bottom + num3))
				{
					if (num2++ >= 48)
					{
						return;
					}
					float num5 = 0.86f + 0.09f * MathF.Sin((float)(num * 9.0 + (double)flammesVue.Phase * 1.7)) + 0.05f * MathF.Sin((float)(num * 23.0 + (double)flammesVue.Phase));
					lumieres.Add(new KiLight.Light(flammesVue.X, flammesVue.Y - (float)BandeDe(flamme).H * 0.35f, num3 * num5, num4 * Vie(flammesVue.T0, flammesVue.T1, num), CouleurDuFeu));
				}
			}
		}
	}

	private static (IGameTexture Texture, FloatRect Place, float Alpha)? FlammeVue(FlammeAuMonde f, double T, long image)
	{
		if (T < f.T0 || T >= f.T1)
		{
			return null;
		}
		Flamme flamme = f.Taille;
		double num = f.T1 - T;
		double num2 = T - f.T0;
		if (flamme != Flamme.G)
		{
			if (num < 0.6 || num2 < 0.12)
			{
				flamme = Moindre(Moindre(flamme));
			}
			else if (num < 1.6 || num2 < 0.3)
			{
				flamme = Moindre(flamme);
			}
		}
		Bande bande = BandeDe(flamme);
		IGameTexture gameTexture = bande.Images[(int)((image + f.Phase) % 8)];
		if (gameTexture == null)
		{
			return null;
		}
		return (gameTexture, new FloatRect(MathF.Round(f.X - (float)bande.L / 2f), MathF.Round(f.Y - (float)bande.Pied), bande.L, bande.H), Math.Clamp(Vie(f.T0, f.T1, T), 0f, 1f));
	}

	public static void PreparerLesRangees(int derniereRangee, int grilleX, int grilleY)
	{
		sDebout.Clear();
		sProchainDebout = 0;
		sDerniereRangee = derniereRangee;
		if (Globals.MapGrid == null)
		{
			return;
		}
		IMapInstance mapInstance = Globals.Me?.MapInstance;
		if (mapInstance == null)
		{
			return;
		}
		int tileHeight = Options.Instance.Map.TileHeight;
		int mapHeight = Options.Instance.Map.MapHeight;
		for (int i = Math.Max(grilleX - 1, 0); i <= Math.Min(grilleX + 1, Globals.MapGridWidth - 1); i++)
		{
			for (int j = Math.Max(grilleY - 1, 0); j <= Math.Min(grilleY + 1, Globals.MapGridHeight - 1); j++)
			{
				MapInstance mapInstance2 = MapInstance.Get(Globals.MapGrid[i, j]);
				if (mapInstance2 == null || mapInstance2.CasseObjets.Count == 0)
				{
					continue;
				}
				CasseCarte casseCarte = CarteDe(mapInstance2);
				if (casseCarte == null)
				{
					continue;
				}
				foreach (int casseObjet in mapInstance2.CasseObjets)
				{
					if (casseObjet < 0 || casseObjet >= casseCarte.Objets.Length)
					{
						continue;
					}
					CasseObjet o = casseCarte.Objets[casseObjet];
					foreach (Decor decor in RenduDe(mapInstance2, casseCarte, casseObjet).Decors)
					{
						if (decor.Debout)
						{
							float num = (float)mapInstance2.Y + decor.PiedY;
							int value = (int)MathF.Floor((num - (float)mapInstance.Y - 1f) / (float)tileHeight) + mapHeight * 2;
							sDebout.Add(new DecorRange(mapInstance2, casseObjet, o, decor, num, Math.Clamp(value, 0, derniereRangee), sDebout.Count));
						}
						else if (decor.AuSol && !SousLesCorps(decor.Couche))
						{
							DessinerDecor(mapInstance2, casseObjet, o, decor);
						}
					}
				}
			}
		}
		if (sDebout.Count > 1)
		{
			sDebout.Sort((DecorRange a, DecorRange b) => (a.Sol == b.Sol) ? a.Ordre.CompareTo(b.Ordre) : a.Sol.CompareTo(b.Sol));
		}
	}

	public static void DessinerDansLaRangee(int rangee, float piedsSuivants)
	{
		while (sProchainDebout < sDebout.Count)
		{
			DecorRange decorRange = sDebout[sProchainDebout];
			if (decorRange.Rangee > rangee || (decorRange.Rangee == rangee && decorRange.Sol >= piedsSuivants && (rangee != sDerniereRangee || piedsSuivants != float.MaxValue)))
			{
				break;
			}
			sProchainDebout++;
			DessinerDecor(decorRange.Carte, decorRange.Objet, decorRange.O, decorRange.Decor);
		}
	}

	private static bool Peint(byte[] px, int L, int H, int x, int y, bool sansFeuilles = false)
	{
		if (x < 0 || y < 0 || x >= L || y >= H)
		{
			return false;
		}
		int num = (y * L + x) * 4;
		if (px[num + 3] > 0)
		{
			if (sansFeuilles)
			{
				return !Vert(px, num);
			}
			return true;
		}
		return false;
	}

	private static void Poser(byte[] px, int L, int x, int y, int r, int g, int b)
	{
		int num = (y * L + x) * 4;
		px[num] = (byte)Math.Clamp(r, 0, 255);
		px[num + 1] = (byte)Math.Clamp(g, 0, 255);
		px[num + 2] = (byte)Math.Clamp(b, 0, 255);
	}

	private static void Teinter(byte[] px, int L, int x, int y, float f, int plus = 0)
	{
		int num = (y * L + x) * 4;
		Poser(px, L, x, y, (int)((float)(int)px[num] * f) + plus, (int)((float)(int)px[num + 1] * f) + plus, (int)((float)(int)px[num + 2] * f) + plus);
	}

	private static void Ecorcher(byte[] px, int L, int H, int cx, int cy, int graine, bool corps)
	{
		Random random = new Random(graine);
		if (!corps)
		{
			for (int i = 0; i < 2; i++)
			{
				for (int j = 0; j < 3; j++)
				{
					if (Peint(px, L, H, cx - 1 + j, cy + i, sansFeuilles: true))
					{
						Poser(px, L, cx - 1 + j, cy + i, 214, 182, 134);
					}
				}
			}
			if (Peint(px, L, H, cx, cy, sansFeuilles: true))
			{
				Poser(px, L, cx, cy, 58, 36, 20);
			}
			return;
		}
		for (int k = cy - 7; k <= cy + 8; k++)
		{
			for (int l = cx - 4; l <= cx + 4; l++)
			{
				float num = (float)((l - cx) * (l - cx)) / 16f + (float)((k - cy) * (k - cy)) / 64f;
				if (num < 1f && Peint(px, L, H, l, k, sansFeuilles: true) && Bruit(l, k, graine, 3f) < 0.7f)
				{
					Teinter(px, L, l, k, 0.82f);
				}
			}
		}
		for (int m = 0; m < 3; m++)
		{
			float num2 = (float)(cx - 3 + m * 2) + (float)random.NextDouble();
			float num3 = cy - 8 + m * 5 + random.Next(2);
			int num4 = 7 + random.Next(3);
			for (int n = 0; n < num4; n++)
			{
				int num5 = (int)MathF.Round(num2);
				int y = (int)MathF.Round(num3);
				if (Peint(px, L, H, num5, y, sansFeuilles: true))
				{
					Poser(px, L, num5, y, 232, 204, 156);
					if (Peint(px, L, H, num5 + 1, y, sansFeuilles: true))
					{
						Poser(px, L, num5 + 1, y, 54, 34, 18);
					}
				}
				num2 += 0.42f;
				num3 += 1f;
			}
		}
	}

	private static void Enfoncer(byte[] px, int L, int H, int cx, int cy, int graine, CasseGenre genre)
	{
		Random random = new Random(graine);
		switch (genre)
		{
		case CasseGenre.Bois:
		{
			for (int i = cy - 4; i <= cy + 4; i++)
			{
				float num = 1.6f + (float)random.NextDouble() * 0.9f - MathF.Abs(i - cy) * 0.12f;
				for (int j = (int)MathF.Floor((float)cx - num); j <= (int)MathF.Ceiling((float)cx + num); j++)
				{
					if (Peint(px, L, H, j, i))
					{
						if ((MathF.Abs(j - cx) > num - 0.6f || Math.Abs(i - cy) == 4) && Hache(j, i, graine) % 100 < 55)
						{
							Poser(px, L, j, i, 240, 200, 144);
							continue;
						}
						int num2 = (int)(Hache(j, i, graine + 1) % 9);
						Poser(px, L, j, i, 42 + num2, 24 + num2, 10 + num2 / 2);
					}
				}
			}
			int num3 = 2 + random.Next(2);
			for (int k = 0; k < num3; k++)
			{
				int num4 = cx + ((k == 0) ? (-1) : (2 + k * 2 + random.Next(2)));
				int[] array = new int[2] { -1, 1 };
				foreach (int num5 in array)
				{
					int num6 = num4;
					int num7 = ((k == 0) ? 6 : 8) + random.Next((k == 0) ? 3 : 6);
					for (int m = 2; m < num7; m++)
					{
						int y = cy + num5 * m;
						if (m % 4 == 3 && random.Next(3) == 0)
						{
							num6 += ((random.Next(2) != 0) ? 1 : (-1));
						}
						if (!Peint(px, L, H, num6, y))
						{
							break;
						}
						Poser(px, L, num6, y, 42, 22, 8);
						if (Peint(px, L, H, num6 + 1, y))
						{
							Teinter(px, L, num6 + 1, y, 1.18f, 18);
						}
					}
				}
			}
			return;
		}
		case CasseGenre.Petit:
			Ecailler(px, L, H, cx, cy, graine, genre);
			return;
		}
		for (int n = cy - 3; n <= cy + 3; n++)
		{
			for (int num8 = cx - 5; num8 <= cx + 5; num8++)
			{
				if (Peint(px, L, H, num8, n))
				{
					float num9 = (float)((num8 - cx) * (num8 - cx)) / 25f + (float)((n - cy) * (n - cy)) / 9f;
					if (num9 < 0.32f)
					{
						int num10 = (int)(Hache(num8, n, graine) % 8);
						Poser(px, L, num8, n, 44 + num10, 44 + num10, 50 + num10);
					}
					else if (num9 < 1f - Bruit(num8, n, graine, 2f) * 0.3f)
					{
						Teinter(px, L, num8, n, 1.12f, 26);
					}
				}
			}
		}
		Fissurer(px, L, H, cx, cy, graine, 4 + random.Next(3), 6, 12);
	}

	private static void Ecailler(byte[] px, int L, int H, int cx, int cy, int graine, CasseGenre genre)
	{
		Random random = new Random(graine);
		switch (genre)
		{
		case CasseGenre.Petit:
		{
			for (int l = cy - 1; l <= cy + 1; l++)
			{
				for (int m = cx - 1; m <= cx + 1; m++)
				{
					if (Peint(px, L, H, m, l) && Hache(m, l, graine) % 100 < 70)
					{
						px[(l * L + m) * 4 + 3] = 0;
					}
				}
			}
			return;
		}
		case CasseGenre.Bois:
		{
			for (int i = cy; i <= cy + 1; i++)
			{
				for (int j = cx; j <= cx + 1; j++)
				{
					if (Peint(px, L, H, j, i))
					{
						Poser(px, L, j, i, 40, 22, 10);
					}
				}
			}
			int num = 3 + random.Next(3);
			for (int k = 1; k <= num && Peint(px, L, H, cx + 2, cy - k + 1); k++)
			{
				Poser(px, L, cx + 2, cy - k + 1, 236, 196, 140);
			}
			return;
		}
		}
		for (int n = cy - 2; n <= cy + 2; n++)
		{
			for (int num2 = cx - 3; num2 <= cx + 3; num2++)
			{
				float num3 = ((float)(num2 - cx) + 0.5f) * ((float)(num2 - cx) + 0.5f) / 9f + ((float)(n - cy) + 0.5f) * ((float)(n - cy) + 0.5f) / 4f;
				if (num3 < 1f && Peint(px, L, H, num2, n))
				{
					Teinter(px, L, num2, n, 1.1f, 26);
				}
			}
		}
		(int, int, float)[] array = new(int, int, float)[5]
		{
			(-1, -1, 0.3f),
			(0, -1, 0.26f),
			(-1, 0, 0.26f),
			(0, 0, 0.22f),
			(1, 0, 0.5f)
		};
		for (int num4 = 0; num4 < array.Length; num4++)
		{
			var (num5, num6, f) = array[num4];
			if (Peint(px, L, H, cx + num5, cy + num6))
			{
				Teinter(px, L, cx + num5, cy + num6, f);
			}
		}
		Fissurer(px, L, H, cx, cy, graine, 2 + random.Next(2), 4, 7);
	}

	private static void Fissurer(byte[] px, int L, int H, int cx, int cy, int graine, int nombre, int min, int max)
	{
		Random random = new Random(graine * 31 + 7);
		for (int i = 0; i < nombre; i++)
		{
			float num = (float)i / (float)nombre * (float)Math.PI * 2f + (float)(random.NextDouble() - 0.5) * 0.9f;
			int num2 = min + random.Next(Math.Max(1, max - min));
			float num3 = (float)cx + MathF.Cos(num) * 3f;
			float num4 = (float)cy + MathF.Sin(num) * 2f;
			for (int j = 0; j < num2; j++)
			{
				num += (float)(random.NextDouble() - 0.5) * 0.5f;
				num3 += MathF.Cos(num);
				num4 += MathF.Sin(num);
				int num5 = (int)MathF.Round(num3);
				int y = (int)MathF.Round(num4);
				if (!Peint(px, L, H, num5, y))
				{
					break;
				}
				Teinter(px, L, num5, y, 0.4f);
				if (Peint(px, L, H, num5 + 1, y))
				{
					Teinter(px, L, num5 + 1, y, 1.05f, 8);
				}
			}
		}
	}

	private static void Cabosser(byte[] px, int L, int H, int cx, int cy, int graine, bool corps)
	{
		if (!corps)
		{
			if (Peint(px, L, H, cx, cy))
			{
				Poser(px, L, cx, cy, 22, 22, 24);
			}
			(int, int)[] array = new(int, int)[4]
			{
				(-1, 0),
				(1, 0),
				(0, -1),
				(0, 1)
			};
			for (int i = 0; i < array.Length; i++)
			{
				var (num, num2) = array[i];
				if (Peint(px, L, H, cx + num, cy + num2))
				{
					Teinter(px, L, cx + num, cy + num2, 1.2f, 30);
				}
			}
			return;
		}
		for (int j = cy - 5; j <= cy + 5; j++)
		{
			for (int k = cx - 4; k <= cx + 4; k++)
			{
				if (Peint(px, L, H, k, j))
				{
					float num3 = (float)((k - cx) * (k - cx)) / 16f + (float)((j - cy) * (j - cy)) / 25f;
					if (num3 < 1f)
					{
						Teinter(px, L, k, j, 0.62f + 0.3f * num3);
					}
					else if (num3 < 1.35f && k < cx && j < cy)
					{
						Teinter(px, L, k, j, 1.25f, 24);
					}
				}
			}
		}
	}

	internal static void Presser()
	{
		_presseJusqua = Timing.Global.Milliseconds + 250;
	}

	private static void NouvelleImage()
	{
		_image++;
		_refaitesCetteImage = 0;
	}

	private static void PurgerRendus(long maintenant)
	{
		if (maintenant < _prochainePurge)
		{
			return;
		}
		_prochainePurge = maintenant + 5000;
		foreach (var (key, rendu2) in Rendus.ToList())
		{
			if (maintenant - rendu2.Vue > 15000)
			{
				MapInstance? mapInstance = MapInstance.Get(key.Item1);
				if (mapInstance == null || !mapInstance.CasseDessine(key.Item2))
				{
					rendu2.Liberer();
					Rendus.Remove(key);
				}
			}
		}
		foreach (var (key2, plancheLue2) in PlanchesLues.ToList())
		{
			if (plancheLue2.Rgba != null && maintenant - plancheLue2.Vue > 20000)
			{
				PlanchesLues.Remove(key2);
			}
		}
		if (Tuiles.Count > 8192)
		{
			Tuiles.Clear();
		}
	}

	private static void OublierRendus()
	{
		foreach (Rendu value in Rendus.Values)
		{
			value.Liberer();
		}
		Rendus.Clear();
		Ronds.Clear();
		Forets.Clear();
		ZonesGardees.Clear();
		Tuiles.Clear();
		PlanchesLues.Clear();
	}

	private static bool TuileDe(IGameTexture planche, int tx, int ty, out byte[]? tuile)
	{
		(string, int, int) key = (planche.Name, tx, ty);
		if (Tuiles.TryGetValue(key, out tuile))
		{
			return true;
		}
		long milliseconds = Timing.Global.Milliseconds;
		if (!PlanchesLues.TryGetValue(planche.Name, out PlancheLue value))
		{
			Dictionary<string, PlancheLue> planchesLues = PlanchesLues;
			string name = planche.Name;
			PlancheLue obj = new PlancheLue
			{
				Vue = milliseconds
			};
			value = obj;
			planchesLues[name] = obj;
			Decoder(planche, value);
		}
		byte[] array = value.Rgba;
		if (array == null)
		{
			if (!value.ParLaCarte)
			{
				return false;
			}
			array = LireParLaCarte(planche, value);
		}
		value.Vue = milliseconds;
		tuile = Couper(array, value.L, value.H, tx * Case, ty * Case);
		Tuiles[key] = tuile;
		return true;
	}

	internal static bool PixelsDeTuile(IGameTexture planche, int tx, int ty, out byte[]? tuile)
	{
		return TuileDe(planche, tx, ty, out tuile);
	}

	private static void Decoder(IGameTexture planche, PlancheLue lue)
	{
		int largeur = planche.Width;
		int hauteur = planche.Height;
		string nom = planche.Name;
		Task.Run(delegate
		{
			try
			{
				using Stream stream = planche.OuvrirSource();
				if (stream != null)
				{
					(byte[], int, int)? tuple = PngBrut.Lire(stream);
					if (tuple.HasValue)
					{
						(byte[], int, int) valueOrDefault = tuple.GetValueOrDefault();
						if (valueOrDefault.Item2 == largeur && valueOrDefault.Item3 == hauteur)
						{
							lue.L = valueOrDefault.Item2;
							lue.H = valueOrDefault.Item3;
							lue.Rgba = valueOrDefault.Item1;
							return;
						}
					}
				}
			}
			catch (Exception exception)
			{
				ApplicationContext.Context.Value?.Logger.LogWarning(exception, "Casse des decors : planche {Planche} illisible dans son fichier", nom);
			}
			lue.ParLaCarte = true;
		});
	}

	private static byte[] LireParLaCarte(IGameTexture planche, PlancheLue lue)
	{
		byte[] array = Array.Empty<byte>();
		GameRenderer renderer = Graphics.Renderer;
		if (renderer != null && renderer.TryReadPixels(planche, out byte[] rgba, out int width, out int height) && rgba.Length >= width * height * 4)
		{
			array = rgba;
			lue.L = width;
			lue.H = height;
		}
		lue.Rgba = array;
		return array;
	}

	private static byte[]? Couper(byte[] rgba, int L, int H, int sx, int sy)
	{
		if (rgba.Length < L * H * 4 || sx < 0 || sy < 0 || sx >= L || sy >= H)
		{
			return null;
		}
		byte[] array = new byte[Case * Case * 4];
		int num = Math.Min(Case, L - sx);
		for (int i = 0; i < Case && sy + i < H; i++)
		{
			Buffer.BlockCopy(rgba, ((sy + i) * L + sx) * 4, array, i * Case * 4, num * 4);
		}
		for (int j = 3; j < array.Length; j += 4)
		{
			if (array[j] != 0)
			{
				return array;
			}
		}
		return null;
	}

	internal static bool Preparer(MapInstance carte, CasseCarte reperee, int objet)
	{
		if (Rendus.TryGetValue((carte.Id, objet), out Rendu value) && value.Faite)
		{
			return true;
		}
		if (!Pret(carte, reperee.Objets[objet]) || _refaitesCetteImage >= BudgetDeLImage)
		{
			return false;
		}
		value = RenduDe(carte, reperee, objet);
		value.Vue = Timing.Global.Milliseconds;
		return value.Faite;
	}

	internal static bool Pret(MapInstance carte, CasseObjet o)
	{
		bool result = true;
		(string, int, int)[] cases = o.Cases;
		for (int i = 0; i < cases.Length; i++)
		{
			var (key, num, num2) = cases[i];
			if (carte.Layers.TryGetValue(key, out var value) && num >= 0 && num2 >= 0 && num < value.GetLength(0) && num2 < value.GetLength(1))
			{
				Tile tile = value[num, num2];
				if (tile.TilesetTexture is IGameTexture planche && !TuileDe(planche, tile.X, tile.Y, out byte[] _))
				{
					result = false;
				}
			}
		}
		return result;
	}

	internal static void DessinerObjet(MapInstance carte, CasseCarte reperee, int objet, string couche)
	{
		if (objet < 0 || objet >= reperee.Objets.Length)
		{
			return;
		}
		CasseObjet casseObjet = reperee.Objets[objet];
		Rendu rendu = RenduDe(carte, reperee, objet);
		rendu.Vue = Timing.Global.Milliseconds;
		if (rendu.Couches.TryGetValue(couche, out IGameTexture value))
		{
			Graphics.DrawGameTexture(value, carte.X + casseObjet.X * Case, carte.Y + casseObjet.Y * Case);
		}
		IGameTexture fentes = rendu.Fentes;
		if (fentes != null && couche == rendu.CoucheHaute)
		{
			float? num = LueurDesFentes(carte.Id, objet, (double)Timing.Global.Milliseconds / 1000.0);
			if (num.HasValue)
			{
				float valueOrDefault = num.GetValueOrDefault();
				if (valueOrDefault > 0.004f)
				{
					Graphics.DrawGameTexture(fentes, new FloatRect(0f, 0f, fentes.Width, fentes.Height), new FloatRect(carte.X + casseObjet.X * Case, carte.Y + casseObjet.Y * Case, fentes.Width, fentes.Height), new Color((int)(255f * valueOrDefault), 255, 255, 255), null, GameBlendModes.Add);
				}
			}
		}
		foreach (Decor decor in rendu.Decors)
		{
			if (!(decor.Couche != couche) && !decor.Debout && (!decor.AuSol || SousLesCorps(decor.Couche)))
			{
				DessinerDecor(carte, objet, casseObjet, decor);
			}
		}
	}

	private static bool SousLesCorps(string couche)
	{
		return Options.Instance.Map.Layers.LowerLayers.Contains(couche);
	}

	private static void DessinerDecor(MapInstance carte, int objet, CasseObjet o, Decor decor)
	{
		IGameTexture texture = GameContentManager.Current.GetTexture(TextureType.Image, decor.Image);
		if (texture == null)
		{
			return;
		}
		FloatRect targetRect = new FloatRect((float)carte.X + decor.X, (float)carte.Y + decor.Y, decor.Source.Width, decor.Source.Height);
		Graphics.DrawGameTexture(texture, decor.Source, targetRect, Color.White);
		if (decor.Partie < 0 || decor.Partie >= o.Parties.Length)
		{
			return;
		}
		IGameTexture gameTexture = BraisesDe(decor.Image)?.Lueur;
		if (gameTexture == null)
		{
			return;
		}
		float? num = LueurDesBraises(carte.Id, objet, decor.Partie, o.Parties[decor.Partie].X, o.Parties[decor.Partie].Y, (double)Timing.Global.Milliseconds / 1000.0);
		if (num.HasValue)
		{
			float valueOrDefault = num.GetValueOrDefault();
			if (valueOrDefault > 0.004f)
			{
				Graphics.DrawGameTexture(gameTexture, decor.Source, targetRect, new Color((int)(255f * valueOrDefault), 255, 255, 255), null, GameBlendModes.Add);
			}
		}
	}

	internal static bool Touche(Zone z, float x0, float y0, float x1, float y1)
	{
		if (x1 >= z.Cx - 46f - 8f && x0 <= z.Cx + 46f + 8f && y1 >= z.Cy - 48f - 8f)
		{
			return y0 <= z.Cy + 48f + 50f + 8f;
		}
		return false;
	}

	internal static List<Zone> Zones(MapInstance carte, CasseCarte reperee, long maintenant)
	{
		int num = VersionDuVoisinage(carte);
		long num2 = maintenant / 250;
		if (ZonesGardees.TryGetValue(carte.Id, out (int, long, List<Zone>, bool) value) && value.Item1 == num && value.Item2 == num2)
		{
			return value.Item3;
		}
		bool completes;
		List<Zone> list = LireZones(carte, reperee, maintenant, out completes);
		ZonesGardees[carte.Id] = (num, num2, list, completes);
		return list;
	}

	internal static bool ZonesIncompletes(Guid carte)
	{
		if (ZonesGardees.TryGetValue(carte, out (int, long, List<Zone>, bool) value))
		{
			return !value.Item4;
		}
		return false;
	}

	private static List<Zone> LireZones(MapInstance carte, CasseCarte reperee, long maintenant, out bool completes)
	{
		List<Zone> list = new List<Zone>();
		completes = true;
		AjouterZones(list, carte, reperee, maintenant, 0, 0, null, ref completes);
		AjouterZonesVoisines(list, carte, maintenant, ref completes);
		return list;
	}

	private static void AjouterZones(List<Zone> zones, MapInstance carte, CasseCarte reperee, long maintenant, int dx, int dy, (int L, int H)? bornes, ref bool completes)
	{
		foreach (KeyValuePair<int, List<(int, EtatPartie)>> item5 in ObjetsTouches(carte.Id))
		{
			item5.Deconstruct(out var key, out var value);
			int num = key;
			List<(int, EtatPartie)> list = value;
			bool flag = num < 0 || num >= reperee.Objets.Length;
			bool flag2 = flag;
			if (!flag2)
			{
				CasseGenre genre = reperee.Objets[num].Genre;
				bool flag3 = (uint)(genre - 3) <= 1u;
				flag2 = !flag3;
			}
			if (flag2)
			{
				continue;
			}
			bool? flag4 = EstRond(carte, reperee, num);
			if (!flag4.HasValue)
			{
				completes = false;
			}
			else
			{
				if (flag4 != true)
				{
					continue;
				}
				CasseObjet casseObjet = reperee.Objets[num];
				foreach (var item6 in list)
				{
					int item = item6.Item1;
					EtatPartie item2 = item6.Item2;
					bool flag5 = item < 0 || item >= casseObjet.Parties.Length;
					bool flag6 = flag5;
					if (!flag6)
					{
						CasseEtat etat = item2.Etat;
						bool flag3 = etat - 2 <= CasseEtat.Abime;
						flag6 = !flag3;
					}
					if (flag6)
					{
						continue;
					}
					float t = ((item2.Etat == CasseEtat.Casse) ? 1f : Math.Clamp((float)(maintenant - item2.Depuis) / (float)Math.Max(1000, Reglages.FeuDureeMs), 0f, 1f));
					(int X, int Y) tuple = casseObjet.Parties[item];
					int item3 = tuple.X;
					int item4 = tuple.Y;
					Zone zone = new Zone(item3 + dx, (float)(item4 + dy) - 50f, item2.Etat, t, (int)Hache(item3, item4, 5));
					if (bornes.HasValue)
					{
						(int, int) valueOrDefault = bornes.GetValueOrDefault();
						if (!Touche(zone, 0f, 0f, valueOrDefault.Item1, valueOrDefault.Item2))
						{
							continue;
						}
					}
					zones.Add(zone);
				}
			}
		}
	}

	private static bool? EstRond(MapInstance carte, CasseCarte reperee, int objet)
	{
		if (Ronds.TryGetValue((carte.Id, objet), out var value))
		{
			return value;
		}
		CasseObjet casseObjet = reperee.Objets[objet];
		if (Pret(carte, casseObjet))
		{
			Dictionary<string, byte[]> dictionary = Composer(carte, casseObjet);
			if (dictionary != null)
			{
				bool definitive;
				Foret foret = ForetDe(carte, casseObjet, out definitive);
				if (!definitive)
				{
					return null;
				}
				Forets[(carte.Id, objet)] = foret;
				bool value2 = foret != null || Rond(casseObjet, dictionary, casseObjet.Largeur * Case);
				Ronds[(carte.Id, objet)] = value2;
				return value2;
			}
		}
		return null;
	}

	internal static bool Rond(CasseObjet o, Dictionary<string, byte[]> couches, int L)
	{
		CasseGenre genre = o.Genre;
		if ((uint)(genre - 3) > 1u)
		{
			return false;
		}
		int num = 0;
		int num2 = 0;
		int num3 = int.MaxValue;
		int num4 = -1;
		int num5 = int.MaxValue;
		int num6 = -1;
		foreach (byte[] value in couches.Values)
		{
			for (int i = 0; i < value.Length; i += 4)
			{
				if (value[i + 3] >= 128)
				{
					num2++;
					int val = i / 4 % L;
					int val2 = i / 4 / L;
					num3 = Math.Min(num3, val);
					num4 = Math.Max(num4, val);
					num5 = Math.Min(num5, val2);
					num6 = Math.Max(num6, val2);
					if (Vert(value, i))
					{
						num++;
					}
				}
			}
		}
		bool flag = num4 >= num3 && (float)(num6 - num5 + 1) > (float)(num4 - num3 + 1) * 2f;
		if ((float)num > (float)num2 * 0.3f)
		{
			return !flag;
		}
		return false;
	}

	private static Rendu RenduDe(MapInstance carte, CasseCarte reperee, int objet)
	{
		(Guid, int) key = (carte.Id, objet);
		if (!Rendus.TryGetValue(key, out Rendu value))
		{
			value = (Rendus[key] = new Rendu());
		}
		if (value.Image == _image)
		{
			return value;
		}
		value.Image = _image;
		CasseObjet casseObjet = reperee.Objets[objet];
		int num = VersionObjet(carte.Id, objet);
		List<(int, EtatPartie)> list = ObjetsTouches(carte.Id).GetValueOrDefault(objet) ?? AucunePartie;
		long milliseconds = Timing.Global.Milliseconds;
		int num2 = 0;
		foreach (var item2 in list)
		{
			EtatPartie item = item2.Item2;
			if (item.Etat == CasseEtat.Feu)
			{
				num2 = Math.Max(num2, 1 + (int)Math.Clamp((milliseconds - item.Depuis) * 5 / Math.Max(1000, Reglages.FeuDureeMs), 0L, 4L));
			}
		}
		List<Zone> list2 = null;
		int num3 = 0;
		CasseGenre genre = casseObjet.Genre;
		if ((uint)(genre - 3) <= 1u)
		{
			float x = casseObjet.X * Case;
			float y = casseObjet.Y * Case;
			float x2 = (casseObjet.X + casseObjet.Largeur) * Case;
			float y2 = (casseObjet.Y + casseObjet.Hauteur) * Case;
			foreach (Zone item3 in Zones(carte, reperee, milliseconds))
			{
				if (Touche(item3, x, y, x2, y2))
				{
					(list2 ?? (list2 = new List<Zone>())).Add(item3);
					int num4 = ((item3.Etat == CasseEtat.Feu) ? (1 + (int)Math.Clamp(item3.T * 5f, 0f, 4f)) : 6);
					if (item3.Etat == CasseEtat.Feu)
					{
						num2 = Math.Max(num2, num4);
					}
					num3 += HashCode.Combine(item3.Cx, item3.Cy, num4);
				}
			}
		}
		genre = casseObjet.Genre;
		bool flag = (uint)(genre - 3) <= 1u;
		List<SillonVu> list3 = (flag ? SillonsDe(carte.Id) : null);
		if (list3 != null && list3.Count > 0)
		{
			num3 = num3 * 31 + list3.Count;
			foreach (SillonVu item4 in list3)
			{
				num3 = num3 * 31 + HashCode.Combine((int)item4.X1, (int)item4.Y1);
			}
		}
		if (value.Faite && value.Version == num && value.PasDuFeu == num2 && value.Signature == num3)
		{
			return value;
		}
		if (value.Faite && _refaitesCetteImage >= BudgetDeLImage)
		{
			return value;
		}
		Dictionary<string, byte[]> dictionary = Composer(carte, casseObjet);
		if (dictionary == null)
		{
			return value;
		}
		_refaitesCetteImage++;
		value.Liberer();
		value.Faite = true;
		value.Version = num;
		value.PasDuFeu = num2;
		value.Signature = num3;
		try
		{
			Foret foret = ForetGardee(carte, objet, casseObjet);
			if (foret != null)
			{
				foret.Sillons = list3 ?? new List<SillonVu>();
			}
			Fabriquer(reperee, objet, dictionary, list, list2 ?? new List<Zone>(), value, milliseconds, foret);
		}
		catch (Exception exception)
		{
			ApplicationContext.Context.Value?.Logger.LogWarning(exception, "Casse des decors : image d'un objet ratee");
		}
		return value;
	}

	private static Dictionary<string, byte[]>? Composer(MapInstance carte, CasseObjet o)
	{
		int num = o.Largeur * Case;
		int num2 = o.Hauteur * Case;
		Dictionary<string, byte[]> dictionary = new Dictionary<string, byte[]>();
		bool flag = true;
		(string, int, int)[] cases = o.Cases;
		for (int i = 0; i < cases.Length; i++)
		{
			var (key, num3, num4) = cases[i];
			if (!carte.Layers.TryGetValue(key, out var value) || num3 < 0 || num4 < 0 || num3 >= value.GetLength(0) || num4 >= value.GetLength(1))
			{
				continue;
			}
			Tile tile = value[num3, num4];
			if (!(tile.TilesetTexture is IGameTexture planche))
			{
				continue;
			}
			if (!TuileDe(planche, tile.X, tile.Y, out byte[] tuile))
			{
				flag = false;
			}
			else
			{
				if (tuile == null || !flag)
				{
					continue;
				}
				if (!dictionary.TryGetValue(key, out var value2))
				{
					value2 = (dictionary[key] = new byte[num * num2 * 4]);
				}
				int num5 = (num3 - o.X) * Case;
				int num6 = (num4 - o.Y) * Case;
				for (int j = 0; j < Case && num6 + j < num2; j++)
				{
					for (int k = 0; k < Case && num5 + k < num; k++)
					{
						int num7 = (j * Case + k) * 4;
						if (tuile[num7 + 3] != 0)
						{
							int num8 = ((num6 + j) * num + num5 + k) * 4;
							value2[num8] = tuile[num7];
							value2[num8 + 1] = tuile[num7 + 1];
							value2[num8 + 2] = tuile[num7 + 2];
							value2[num8 + 3] = tuile[num7 + 3];
						}
					}
				}
			}
		}
		if (!flag)
		{
			return null;
		}
		return dictionary;
	}

	private static void Fabriquer(CasseCarte reperee, int objet, Dictionary<string, byte[]> couches, List<(int Partie, EtatPartie Etat)> parties, List<Zone> zones, Rendu rendu, long maintenant, Foret? foret = null)
	{
		if (couches.Count == 0)
		{
			return;
		}
		CasseObjet casseObjet = reperee.Objets[objet];
		List<string> ordre = Options.Instance.Map.Layers.All;
		int graine = (reperee.Id.GetHashCode() * 397) ^ (objet * 7919);
		int num = casseObjet.Largeur * Case;
		int num2 = casseObjet.Hauteur * Case;
		if (casseObjet.Genre == CasseGenre.Poubelle)
		{
			rendu.Hauts = HautsDesColonnes(couches, num, num2);
			rendu.CoucheHaute = couches.Keys.OrderBy((string c) => ordre.IndexOf(c)).Last();
			if (parties.Any<(int, EtatPartie)>(delegate((int Partie, EtatPartie Etat) p)
			{
				CasseEtat etat = p.Etat.Etat;
				return etat - 2 <= CasseEtat.Abime;
			}))
			{
				byte[] array = FentesDuCouvercle(couches, num, num2, rendu.Hauts);
				if (array != null)
				{
					rendu.Fentes = Graphics.Renderer?.CreateTextureFromPixels($"casse/{reperee.Id}/{objet}/fentes/{++_fabriquees}", num, num2, array);
				}
			}
		}
		rendu.Decors.AddRange(Abimer(casseObjet, couches, parties, (string c) => ordre.IndexOf(c), graine, maintenant, Reglages.FeuDureeMs, zones, foret));
		bool flag = parties.Any<(int, EtatPartie)>(((int Partie, EtatPartie Etat) p) => p.Etat.Etat == CasseEtat.Casse);
		foreach (KeyValuePair<string, byte[]> couch in couches)
		{
			couch.Deconstruct(out var key, out var value);
			string text = key;
			byte[] array2 = value;
			IGameTexture gameTexture = Graphics.Renderer?.CreateTextureFromPixels($"casse/{reperee.Id}/{objet}/{text}/{++_fabriquees}", num, num2, array2);
			if (gameTexture != null)
			{
				rendu.Couches[text] = gameTexture;
				if (flag)
				{
					rendu.Pixels[text] = array2;
				}
			}
		}
	}

	internal static List<Decor> Abimer(CasseObjet o, Dictionary<string, byte[]> couches, List<(int Partie, EtatPartie Etat)> parties, Func<string, int> rangDeCouche, int graine, long maintenant, int feuDureeMs, IReadOnlyList<Zone> zones, Foret? foret = null)
	{
		List<Decor> list = new List<Decor>();
		if (couches.Count == 0)
		{
			return list;
		}
		int l = o.Largeur * Case;
		int h = o.Hauteur * Case;
		int ox = o.X * Case;
		int oy = o.Y * Case;
		string basse = couches.Keys.OrderBy(rangDeCouche).First();
		string haute = couches.Keys.OrderBy(rangDeCouche).Last();
		CasseGenre genre = o.Genre;
		if ((uint)(genre - 2) <= 2u)
		{
			Vegetal(o, couches, parties, list, basse, haute, l, h, ox, oy, graine, maintenant, feuDureeMs, zones, foret);
			list.RemoveAll((Decor d) => d.Partie >= 0 && Dehors(o, d.Partie));
		}
		else if (o.Genre == CasseGenre.Poubelle)
		{
			Metal(couches, parties, l, h, ox, oy, graine, maintenant, feuDureeMs);
		}
		else
		{
			Mineral(o, couches, parties, basse, l, h, ox, oy, graine);
		}
		return list;
	}

	private static float Luma(byte[] px, int i)
	{
		return (float)(int)px[i] * 0.3f + (float)(int)px[i + 1] * 0.55f + (float)(int)px[i + 2] * 0.15f;
	}

	private static bool Vert(byte[] px, int i)
	{
		if (px[i + 1] > px[i] + 8)
		{
			return px[i + 1] > px[i + 2] + 8;
		}
		return false;
	}

	private static float Bruit(int x, int y, int graine, float taille = 7f)
	{
		return Octave(x, y, graine, taille) * 0.65f + Octave(x, y, graine + 17, taille / 2.5f) * 0.35f;
		static float Octave(int num2, int num5, int c, float num3)
		{
			float num = (float)num2 / num3;
			float num4 = (float)num5 / num3;
			int num6 = (int)MathF.Floor(num);
			int num7 = (int)MathF.Floor(num4);
			float num8 = num - (float)num6;
			float num9 = num4 - (float)num7;
			num8 = num8 * num8 * (3f - 2f * num8);
			num9 = num9 * num9 * (3f - 2f * num9);
			float num10 = V(num6, num7) + (V(num6 + 1, num7) - V(num6, num7)) * num8;
			float num11 = V(num6, num7 + 1) + (V(num6 + 1, num7 + 1) - V(num6, num7 + 1)) * num8;
			return num10 + (num11 - num10) * num9;
			float V(int a, int b)
			{
				return (float)(Hache(a, b, c) & 0xFFFF) / 65535f;
			}
		}
	}

	private static uint Hache(int a, int b, int c)
	{
		uint num = (uint)((a * 73856093) ^ (b * 19349663) ^ (c * 83492791));
		num ^= num >> 13;
		num *= 1540483477;
		return num ^ (num >> 15);
	}

	private static void Charbon(byte[] px, int i, float t)
	{
		float num = Luma(px, i);
		float num2 = num * 0.34f + 12f;
		float num3 = num * 0.27f + 8f;
		float num4 = num * 0.24f + 7f;
		px[i] = (byte)((float)(int)px[i] + (num2 - (float)(int)px[i]) * t);
		px[i + 1] = (byte)((float)(int)px[i + 1] + (num3 - (float)(int)px[i + 1]) * t);
		px[i + 2] = (byte)((float)(int)px[i + 2] + (num4 - (float)(int)px[i + 2]) * t);
	}

	private static void Vegetal(CasseObjet o, Dictionary<string, byte[]> couches, List<(int Partie, EtatPartie Etat)> parties, List<Decor> decors, string basse, string haute, int L, int H, int ox, int oy, int graine, long maintenant, int feuDureeMs, IReadOnlyList<Zone> zones, Foret? foret)
	{
		bool rond = o.Genre == CasseGenre.Foret || foret != null || Rond(o, couches, L);
		int num = H;
		int num2 = -1;
		foreach (byte[] value2 in couches.Values)
		{
			for (int i = 0; i < value2.Length; i += 4)
			{
				if (value2[i + 3] >= 128)
				{
					int val = i / 4 / L;
					num = Math.Min(num, val);
					num2 = Math.Max(num2, val);
				}
			}
		}
		if (rond && zones.Count > 0)
		{
			foret?.Etats(zones);
			foreach (KeyValuePair<string, byte[]> couch in couches)
			{
				couch.Deconstruct(out var key, out var value);
				string key2 = key;
				byte[] array = value;
				bool[] array2 = foret?.DuMotif.GetValueOrDefault(key2);
				bool[] array3 = foret?.DuSeul.GetValueOrDefault(key2);
				for (int j = 0; j < H; j++)
				{
					for (int k = 0; k < L; k++)
					{
						int num3 = (j * L + k) * 4;
						if (array[num3 + 3] == 0)
						{
							continue;
						}
						int num4 = ox + k;
						int num5 = oy + j;
						if (array2 != null && array2[j * L + k])
						{
							if (array3 != null && array3[j * L + k] && foret.MargeDUnTombe(num4, num5))
							{
								array[num3 + 3] = 0;
							}
							else
							{
								foret.Appliquer(array, num3, num4, num5);
							}
							continue;
						}
						bool flag = false;
						float num6 = -1f;
						int num7 = 0;
						foreach (Zone zone in zones)
						{
							if (!(MathF.Abs((float)num4 - zone.Cx) > 59.8f) && !((float)num5 < zone.Cy - 62.399998f) && !((float)num5 > zone.Cy + 50f + 4f) && DansLaCime(zone.Cx, zone.Cy, zone.Graine, num4, num5))
							{
								if (zone.Etat == CasseEtat.Casse)
								{
									flag = true;
									break;
								}
								if (zone.T > num6)
								{
									num6 = zone.T;
									num7 = zone.Graine;
								}
							}
						}
						if (flag)
						{
							array[num3 + 3] = 0;
						}
						else if (!(num6 < 0f))
						{
							Charbon(array, num3, Math.Min(1f, num6 * 1.4f + 0.15f));
							if (num6 > 0.5f && Bruit(num4, num5, num7) < (num6 - 0.5f) * 1.1f)
							{
								array[num3 + 3] = 0;
							}
							else if (Hache(num4, num5, num7) % 100 < (uint)(2f + num6 * 5f))
							{
								array[num3] = byte.MaxValue;
								array[num3 + 1] = (byte)(110 + Hache(num5, num4, num7) % 60);
								array[num3 + 2] = 30;
							}
						}
					}
				}
			}
		}
		foreach (var (num8, etatPartie) in parties)
		{
			if (num8 < 0 || num8 >= o.Parties.Length)
			{
				continue;
			}
			int px0 = o.Parties[num8].X - ox;
			int py0 = o.Parties[num8].Y - oy;
			int num9 = graine + num8 * 131;
			int graineCime = (int)Hache(o.Parties[num8].X, o.Parties[num8].Y, 5);
			switch (etatPartie.Etat)
			{
			case CasseEtat.Abime:
				foreach (var (num13, num14, num15, casseCoup) in etatPartie.Coups)
				{
					if (casseCoup - 6 <= CasseCoup.Rayon)
					{
						foreach (byte[] value3 in couches.Values)
						{
							Ecorcher(value3, L, H, (int)num13 - ox, (int)num14 - oy, num15, casseCoup == CasseCoup.Corps);
						}
						if (casseCoup == CasseCoup.Balle)
						{
							continue;
						}
					}
					int num16 = (int)num13 - ox;
					int num17 = (int)num14 - oy - (rond ? 30 : 12);
					foreach (byte[] value4 in couches.Values)
					{
						for (int num18 = Math.Max(0, num17 - 12); num18 < Math.Min(H, num17 + 12); num18++)
						{
							for (int num19 = Math.Max(0, num16 - 12); num19 < Math.Min(L, num16 + 12); num19++)
							{
								int num20 = (num18 * L + num19) * 4;
								if (value4[num20 + 3] != 0 && Vert(value4, num20) && DansLaZone(num19, num18))
								{
									float num21 = num19 - num16;
									float num22 = num18 - num17;
									if (num21 * num21 + num22 * num22 < 60f + Bruit(num19, num18, num15, 4f) * 80f && Bruit(num19, num18, num15 + 1, 3f) < 0.55f)
									{
										value4[num20 + 3] = 0;
									}
								}
							}
						}
					}
				}
				break;
			case CasseEtat.Feu:
			{
				if (rond)
				{
					break;
				}
				float num23 = Math.Clamp((float)(maintenant - etatPartie.Depuis) / (float)Math.Max(1000, feuDureeMs), 0f, 1f);
				foreach (byte[] value5 in couches.Values)
				{
					for (int num24 = 0; num24 < H; num24++)
					{
						for (int num25 = 0; num25 < L; num25++)
						{
							int num26 = (num24 * L + num25) * 4;
							if (value5[num26 + 3] != 0)
							{
								Charbon(value5, num26, Math.Min(1f, num23 * 1.4f + 0.15f));
								if (num23 > 0.5f && Bruit(num25, num24, num9) < (num23 - 0.5f) * 1.1f)
								{
									value5[num26 + 3] = 0;
								}
								else if (Hache(num25, num24, num9) % 100 < (uint)(2f + num23 * 5f))
								{
									value5[num26] = byte.MaxValue;
									value5[num26 + 1] = (byte)(110 + Hache(num24, num25, num8) % 60);
									value5[num26 + 2] = 30;
								}
							}
						}
					}
				}
				break;
			}
			case CasseEtat.Casse:
			{
				(int, int) tuple2 = (ox + px0, oy + py0);
				bool flag2 = false;
				(int, int)? tuple3 = foret?.PiedDe(tuple2.Item1, tuple2.Item2);
				if (tuple3.HasValue)
				{
					(int, int) valueOrDefault = tuple3.GetValueOrDefault();
					tuple2 = valueOrDefault;
					flag2 = true;
				}
				if (etatPartie.Tranche)
				{
					if (!rond || o.Genre == CasseGenre.Buisson)
					{
						foreach (byte[] value6 in couches.Values)
						{
							for (int l = 3; l < value6.Length; l += 4)
							{
								value6[l] = 0;
							}
						}
					}
					if (o.Genre != CasseGenre.Buisson)
					{
						if (flag2)
						{
							PoserLePied(decors, basse, !SousLesCorps(haute), tuple2.Item1, tuple2.Item2, etatPartie.Couche);
						}
						else if (foret == null)
						{
							AjouterDecor(decors, "casse_souche.png", basse, tuple2.Item1 - 40, tuple2.Item2 - 22, 0f, 0f, 80f, 26f, num8, !SousLesCorps(haute), tuple2.Item2);
						}
					}
					break;
				}
				if (o.Genre == CasseGenre.Buisson)
				{
					foreach (byte[] value7 in couches.Values)
					{
						for (int m = 3; m < value7.Length; m += 4)
						{
							value7[m] = 0;
						}
					}
					AjouterDecor(decors, "casse_feuilles_calcinees.png", basse, tuple2.Item1 - 32, tuple2.Item2 - 26, 0f, 0f, 64f, 32f, num8, debout: false, 0f, auSol: true);
					break;
				}
				if (rond)
				{
					AjouterDecor(decors, "casse_terre_brulee.png", basse, tuple2.Item1 - 48, tuple2.Item2 - 46, 0f, 0f, 96f, 56f, num8, debout: false, 0f, auSol: true);
					AjouterDecor(decors, "casse_feuilles_calcinees.png", basse, tuple2.Item1 - 32, tuple2.Item2 - 20, 0f, 0f, 64f, 32f, num8, debout: false, 0f, auSol: true);
					if (Hache(tuple2.Item1, tuple2.Item2, 8) % 8 == 0)
					{
						AjouterDecor(decors, "casse_troncs_calcines.png", basse, tuple2.Item1 - 48, tuple2.Item2 - 46, 0f, 0f, 96f, 64f, num8, !SousLesCorps(haute), tuple2.Item2);
						break;
					}
					AjouterDecor(decors, "casse_arbre_calcine.png", haute, tuple2.Item1 - 46, tuple2.Item2 - 92, 0f, 0f, 92f, 62f, num8);
					AjouterDecor(decors, "casse_arbre_calcine.png", basse, tuple2.Item1 - 46, tuple2.Item2 - 92 + 96 - 34, 0f, 62f, 92f, 34f, num8);
					break;
				}
				foreach (byte[] value8 in couches.Values)
				{
					for (int n = 0; n < H; n++)
					{
						for (int num10 = 0; num10 < L; num10++)
						{
							int num11 = (n * L + num10) * 4;
							if (value8[num11 + 3] != 0)
							{
								Charbon(value8, num11, 1f);
								float num12 = ((num2 > num) ? ((float)(n - num) / (float)(num2 - num)) : 1f);
								if (num12 < 0.5f && Bruit(num10, n, num9, 5f) < 0.62f - num12)
								{
									value8[num11 + 3] = 0;
								}
								else if (Hache(num10, n, num9) % 100 < 2)
								{
									value8[num11] = 230;
									value8[num11 + 1] = 90;
									value8[num11 + 2] = 20;
								}
							}
						}
					}
				}
				break;
			}
			}
			bool DansLaZone(int x, int y)
			{
				if (rond)
				{
					return DansLaCime(ox + px0, (float)(oy + py0) - 50f, graineCime, ox + x, oy + y);
				}
				return true;
			}
		}
		static bool DansLaCime(float cx, float cy, int graineZone, int X, int Y)
		{
			float num27 = ((float)X - cx) / 46f;
			float num28 = ((float)Y - cy) / 48f;
			if (MathF.Sqrt(num27 * num27 + num28 * num28) < 1f + (Bruit(X, Y, graineZone, 6f) - 0.5f) * 0.5f)
			{
				return true;
			}
			if (MathF.Abs((float)X - cx) < 7f && (float)Y > cy)
			{
				return (float)Y <= cy + 50f + 3f;
			}
			return false;
		}
	}

	private static void AjouterDecor(List<Decor> decors, string image, string couche, float x, float y, float sx, float sy, float sl, float sh, int partie = -1, bool debout = false, float piedY = 0f, bool auSol = false)
	{
		decors.Add(new Decor
		{
			Image = image,
			Couche = couche,
			Source = new FloatRect(sx, sy, sl, sh),
			X = x,
			Y = y,
			Partie = partie,
			Debout = debout,
			PiedY = piedY,
			AuSol = auSol
		});
	}

	private static void Metal(Dictionary<string, byte[]> couches, List<(int Partie, EtatPartie Etat)> parties, int L, int H, int ox, int oy, int graine, long maintenant, int feuDureeMs)
	{
		EtatPartie etatPartie = ((parties.Count > 0) ? parties[0].Etat : null);
		if (etatPartie == null)
		{
			return;
		}
		foreach (var (num, num2, graine2, casseCoup) in etatPartie.Coups)
		{
			foreach (byte[] value in couches.Values)
			{
				if (casseCoup - 6 <= CasseCoup.Rayon)
				{
					Cabosser(value, L, H, (int)num - ox, (int)num2 - oy, graine2, casseCoup == CasseCoup.Corps);
				}
				else
				{
					Suie(value, L, H, (int)num - ox, (int)num2 - oy, graine2);
				}
			}
		}
		float num3 = etatPartie.Etat switch
		{
			CasseEtat.Feu => Math.Clamp((float)(maintenant - etatPartie.Depuis) / (float)Math.Max(1000, feuDureeMs), 0f, 1f), 
			CasseEtat.Casse => 1f, 
			_ => 0f, 
		};
		if (num3 <= 0f)
		{
			return;
		}
		int num4 = H;
		int num5 = 0;
		foreach (byte[] value2 in couches.Values)
		{
			for (int i = 0; i < H; i++)
			{
				for (int j = 0; j < L; j++)
				{
					if (value2[(i * L + j) * 4 + 3] > 0)
					{
						num4 = Math.Min(num4, i);
						num5 = Math.Max(num5, i);
					}
				}
			}
		}
		int num6 = Math.Max(1, num5 - num4);
		bool flag = etatPartie.Etat == CasseEtat.Casse;
		foreach (byte[] value3 in couches.Values)
		{
			for (int k = 0; k < H; k++)
			{
				float num7 = Math.Clamp((float)(k - num4) / (float)num6, 0f, 1f);
				for (int l = 0; l < L; l++)
				{
					int num8 = (k * L + l) * 4;
					if (value3[num8 + 3] != 0)
					{
						float num9 = Bruit(l, k, graine, 6f);
						float num10 = Math.Clamp((num3 * 1.35f - num7 * 0.9f + (num9 - 0.5f) * 0.45f) * 2.2f, 0f, 1f) * 0.86f;
						if (flag)
						{
							num10 = MathF.Max(num10, 0.74f + 0.18f * num9);
						}
						if (num10 > 0f)
						{
							Charbon(value3, num8, num10);
						}
					}
				}
			}
		}
	}

	internal static int[] HautsDesColonnes(Dictionary<string, byte[]> couches, int L, int H)
	{
		int[] array = new int[L];
		Array.Fill(array, -1);
		foreach (byte[] value in couches.Values)
		{
			for (int i = 0; i < L; i++)
			{
				for (int j = 0; j < H; j++)
				{
					if (value[(j * L + i) * 4 + 3] > 0)
					{
						if (array[i] < 0 || j < array[i])
						{
							array[i] = j;
						}
						break;
					}
				}
			}
		}
		return array;
	}

	internal static byte[]? FentesDuCouvercle(Dictionary<string, byte[]> couches, int L, int H, int[] hauts)
	{
		byte[] dessin = new byte[L * H * 4];
		foreach (byte[] value3 in couches.Values)
		{
			for (int i = 0; i < L * H; i++)
			{
				if (value3[i * 4 + 3] > 0)
				{
					Array.Copy(value3, i * 4, dessin, i * 4, 4);
				}
			}
		}
		int num = 0;
		int num2 = H;
		for (int j = 0; j < L; j++)
		{
			if (hauts[j] >= 0)
			{
				num2 = Math.Min(num2, hauts[j]);
			}
		}
		for (int k = 0; k < L * H; k++)
		{
			if (dessin[k * 4 + 3] > 0)
			{
				num = Math.Max(num, k / L);
			}
		}
		float num3 = Math.Max(8f, (float)(num - num2) * 0.5f);
		byte[] array = new byte[L * H * 4];
		int num4 = 0;
		for (int l = 0; l < L; l++)
		{
			if (hauts[l] < 0)
			{
				continue;
			}
			for (int m = hauts[l] + 2; (float)m < Math.Min(H, (float)hauts[l] + num3); m++)
			{
				int num5 = (m * L + l) * 4;
				if (Plein(l, m) && Plein(l - 1, m) && Plein(l + 1, m) && Plein(l, m - 1) && Plein(l, m + 1) && !(Luma(dessin, num5) > 72f))
				{
					float value = (float)(m - hauts[l]) / num3;
					float value2 = MathF.Sin((float)Math.PI * Math.Clamp(value, 0f, 1f)) * (0.75f + 0.25f * Bruit(l, m, 97, 4f));
					array[num5] = byte.MaxValue;
					array[num5 + 1] = 118;
					array[num5 + 2] = 34;
					array[num5 + 3] = (byte)Math.Round(255f * Math.Clamp(value2, 0f, 1f));
					num4++;
				}
			}
		}
		if (num4 <= 0)
		{
			return null;
		}
		return array;
		bool Plein(int x, int y)
		{
			if (x >= 0 && y >= 0 && x < L && y < H)
			{
				return dessin[(y * L + x) * 4 + 3] > 0;
			}
			return false;
		}
	}

	private static void Suie(byte[] px, int L, int H, int cx, int cy, int graine)
	{
		for (int i = Math.Max(0, cy - 7); i < Math.Min(H, cy + 7 + 1); i++)
		{
			for (int j = Math.Max(0, cx - 7); j < Math.Min(L, cx + 7 + 1); j++)
			{
				int num = (i * L + j) * 4;
				if (px[num + 3] != 0)
				{
					float num2 = MathF.Sqrt((float)((j - cx) * (j - cx)) + (float)((i - cy) * (i - cy)) * 1.3f) / 7f;
					float num3 = 0.7f + 0.35f * Bruit(j, i, graine, 3f);
					if (!(num2 >= num3))
					{
						float num4 = 1f - 0.58f * MathF.Pow(1f - num2 / num3, 0.7f);
						px[num] = (byte)((float)(int)px[num] * num4);
						px[num + 1] = (byte)((float)(int)px[num + 1] * num4);
						px[num + 2] = (byte)((float)(int)px[num + 2] * num4);
					}
				}
			}
		}
	}

	private static void Mineral(CasseObjet o, Dictionary<string, byte[]> couches, List<(int Partie, EtatPartie Etat)> parties, string basse, int L, int H, int ox, int oy, int graine)
	{
		EtatPartie etatPartie = ((parties.Count > 0) ? parties[0].Etat : null);
		if (etatPartie == null)
		{
			return;
		}
		if (etatPartie.Etat != CasseEtat.Casse)
		{
			foreach (var (num, num2, graine2, casseCoup) in etatPartie.Coups)
			{
				foreach (byte[] value in couches.Values)
				{
					switch (casseCoup)
					{
					case CasseCoup.Corps:
						Enfoncer(value, L, H, (int)num - ox, (int)num2 - oy, graine2, o.Genre);
						break;
					case CasseCoup.Balle:
						Ecailler(value, L, H, (int)num - ox, (int)num2 - oy, graine2, o.Genre);
						break;
					default:
						Trou(value, L, H, (int)num - ox, (int)num2 - oy, graine2, o.Genre);
						break;
					}
				}
			}
			return;
		}
		if (o.Rocher)
		{
			Ebouler(o, couches, L, H, graine);
			return;
		}
		float num3 = o.Genre switch
		{
			CasseGenre.Petit => 0.35f, 
			CasseGenre.Muret => 0.4f, 
			_ => (o.Hauteur <= 4) ? 0.3f : 0.45f, 
		};
		foreach (var (text2, array2) in couches.ToList())
		{
			if (text2 != basse)
			{
				Array.Clear(array2);
				continue;
			}
			int num4 = H;
			int num5 = 0;
			for (int i = 0; i < H; i++)
			{
				for (int j = 0; j < L; j++)
				{
					if (array2[(i * L + j) * 4 + 3] > 0)
					{
						num4 = Math.Min(num4, i);
						num5 = Math.Max(num5, i);
					}
				}
			}
			if (num5 <= num4)
			{
				continue;
			}
			float num6 = (float)num5 - (float)(num5 - num4) * num3;
			List<(byte, byte, byte)> list = new List<(byte, byte, byte)>();
			float num7 = 0f;
			Random random = new Random(graine);
			for (int k = 0; k < L; k++)
			{
				num7 = Math.Clamp(num7 + (float)(random.NextDouble() - 0.5) * 3f, -7f, 7f);
				float num8 = MathF.Max(0f, 10f - MathF.Abs(k % 37 - 18) * 1.2f) * ((Hache(k / 37, 0, graine) % 2 == 0) ? 1f : 0f);
				float num9 = num6 + num7 + num8;
				for (int l = 0; l < H; l++)
				{
					int num10 = (l * L + k) * 4;
					if (array2[num10 + 3] == 0)
					{
						continue;
					}
					if ((float)l < num9)
					{
						if (list.Count < 400 && (float)l > num9 - 22f && Hache(k, l, graine) % 5 == 0)
						{
							list.Add((array2[num10], array2[num10 + 1], array2[num10 + 2]));
						}
						array2[num10 + 3] = 0;
					}
					else
					{
						float num11 = Math.Clamp(1f - ((float)l - num9) / 26f, 0f, 1f);
						float num12 = 1f - 0.45f * num11 - 0.15f;
						array2[num10] = (byte)((float)(int)array2[num10] * num12);
						array2[num10 + 1] = (byte)((float)(int)array2[num10 + 1] * num12);
						array2[num10 + 2] = (byte)((float)(int)array2[num10 + 2] * num12);
					}
				}
			}
			CasseGenre genre = o.Genre;
			if ((uint)(genre - 5) <= 2u)
			{
				for (int m = 0; m < 3; m++)
				{
					Trou(array2, L, H, random.Next(8, Math.Max(9, L - 8)), (int)(num6 + ((float)num5 - num6) * (0.25f + 0.4f * (float)random.NextDouble())), graine + m, o.Genre);
				}
			}
			Gravats(array2, L, num5, (int)((float)(num5 - num4) * ((o.Genre == CasseGenre.Petit) ? 0.18f : 0.24f)), list, random);
		}
	}

	private static void Trou(byte[] px, int L, int H, int cx, int cy, int graine, CasseGenre genre)
	{
		Random random = new Random(graine);
		bool flag = genre == CasseGenre.Petit;
		bool flag2 = genre == CasseGenre.Bois;
		int num = (flag ? (3 + random.Next(3)) : (3 + random.Next(3)));
		float[] array = new float[16];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = (float)num * (0.75f + (float)random.NextDouble() * 0.5f);
		}
		for (int j = Math.Max(0, cy - num - 6); j < Math.Min(H, cy + num + 6); j++)
		{
			for (int k = Math.Max(0, cx - num - 6); k < Math.Min(L, cx + num + 6); k++)
			{
				int num2 = (j * L + k) * 4;
				if (px[num2 + 3] == 0)
				{
					continue;
				}
				float num3 = k - cx;
				float num4 = j - cy;
				float num5 = MathF.Sqrt(num3 * num3 + num4 * num4);
				int num6 = (int)((MathF.Atan2(num4, num3) + (float)Math.PI) / ((float)Math.PI * 2f) * (float)array.Length) % array.Length;
				float num7 = array[num6];
				if (num5 < num7)
				{
					if (flag)
					{
						px[num2 + 3] = 0;
						continue;
					}
					int num8 = (int)(Hache(k, j, graine) % 9);
					float num9 = Math.Clamp(num5 / Math.Max(1f, num7), 0f, 1f);
					if (flag2)
					{
						px[num2] = (byte)((float)(40 + num8) + num9 * 18f);
						px[num2 + 1] = (byte)((float)(26 + num8) + num9 * 12f);
						px[num2 + 2] = (byte)((float)(18 + num8 / 2) + num9 * 8f);
					}
					else
					{
						px[num2] = (byte)((float)(40 + num8) + num9 * 16f);
						px[num2 + 1] = (byte)((float)(38 + num8) + num9 * 16f);
						px[num2 + 2] = (byte)((float)(42 + num8) + num9 * 16f);
					}
				}
				else if (!flag && num5 < num7 + 1.4f && Hache(num6, 1, graine) % 100 < (uint)(flag2 ? 65 : 50))
				{
					if (flag2)
					{
						px[num2] = 198;
						px[num2 + 1] = 158;
						px[num2 + 2] = 106;
					}
					else
					{
						px[num2] = (byte)Math.Min(255, px[num2] + 30);
						px[num2 + 1] = (byte)Math.Min(255, px[num2 + 1] + 30);
						px[num2 + 2] = (byte)Math.Min(255, px[num2 + 2] + 30);
					}
				}
				else if (!flag && num5 < num7 + 4f)
				{
					float num10 = 0.62f + 0.28f * (num5 - num7) / 4f;
					px[num2] = (byte)((float)(int)px[num2] * num10);
					px[num2 + 1] = (byte)((float)(int)px[num2 + 1] * num10);
					px[num2 + 2] = (byte)((float)(int)px[num2 + 2] * num10);
				}
			}
		}
		if (flag)
		{
			return;
		}
		int num11 = 2 + random.Next(2);
		for (int l = 0; l < num11; l++)
		{
			float num12 = (flag2 ? (((random.Next(2) == 0) ? (-(float)Math.PI / 2f) : ((float)Math.PI / 2f)) + (float)(random.NextDouble() - 0.5) * 0.7f) : ((float)(random.NextDouble() * Math.PI * 2.0)));
			int num13 = (flag2 ? (3 + random.Next(3)) : (4 + random.Next(4)));
			float num14 = (float)cx + MathF.Cos(num12) * (float)(num + 1);
			float num15 = (float)cy + MathF.Sin(num12) * (float)(num + 1);
			for (int m = 0; m < num13; m++)
			{
				num12 += (float)(random.NextDouble() - 0.5) * 0.4f;
				num14 += MathF.Cos(num12);
				num15 += MathF.Sin(num12);
				int num16 = (int)num14;
				int num17 = (int)num15;
				if (num16 < 0 || num17 < 0 || num16 >= L || num17 >= H)
				{
					break;
				}
				int num18 = (num17 * L + num16) * 4;
				if (px[num18 + 3] == 0)
				{
					break;
				}
				if (flag2)
				{
					px[num18] = 188;
					px[num18 + 1] = 148;
					px[num18 + 2] = 98;
				}
				else
				{
					px[num18] = (byte)((float)(int)px[num18] * 0.45f);
					px[num18 + 1] = (byte)((float)(int)px[num18 + 1] * 0.45f);
					px[num18 + 2] = (byte)((float)(int)px[num18 + 2] * 0.45f);
				}
			}
		}
	}

	private static void Gravats(byte[] px, int L, int bas, int hauteur, List<(byte R, byte G, byte B)> palette, Random hasard)
	{
		if (palette.Count == 0 || hauteur < 4)
		{
			return;
		}
		int num = px.Length / 4 / L;
		int num2 = Math.Max(6, L / 4);
		for (int i = 0; i < num2; i++)
		{
			(byte R, byte G, byte B) tuple = palette[hasard.Next(palette.Count)];
			byte item = tuple.R;
			byte item2 = tuple.G;
			byte item3 = tuple.B;
			float num3 = (float)(item + item2 + item3) / 3f;
			float num4 = 0.72f + 0.25f * (float)hasard.NextDouble();
			byte b = (byte)(((float)(int)item * 0.8f + num3 * 0.2f) * num4);
			byte b2 = (byte)(((float)(int)item2 * 0.8f + num3 * 0.2f) * num4);
			byte b3 = (byte)(((float)(int)item3 * 0.8f + num3 * 0.2f) * num4);
			int num5 = 4 + hasard.Next(Math.Max(1, L - 8));
			int num6 = bas - hasard.Next(Math.Max(1, (int)((float)hauteur * 0.8f)));
			int num7 = 3 + hasard.Next(Math.Max(1, Math.Min(10, hauteur / 2)));
			int num8 = 5 + hasard.Next(3);
			(float, float)[] array = new(float, float)[num8];
			for (int j = 0; j < num8; j++)
			{
				float x = (float)j * (float)Math.PI * 2f / (float)num8 + (float)hasard.NextDouble() * 0.5f;
				float num9 = (float)num7 * (0.7f + (float)hasard.NextDouble() * 0.5f);
				array[j] = ((float)num5 + MathF.Cos(x) * num9, (float)num6 + MathF.Sin(x) * num9 * 0.7f);
			}
			for (int k = Math.Max(0, num6 - num7); k <= Math.Min(Math.Min(bas, num - 1), num6 + num7); k++)
			{
				for (int l = Math.Max(0, num5 - num7 - 1); l <= Math.Min(L - 1, num5 + num7 + 1); l++)
				{
					if (DansLePolygone(array, (float)l + 0.5f, (float)k + 0.5f))
					{
						int num10 = (k * L + l) * 4;
						float num11 = ((k < num6) ? 1.08f : 0.86f);
						px[num10] = (byte)Math.Min(255f, (float)(int)b * num11);
						px[num10 + 1] = (byte)Math.Min(255f, (float)(int)b2 * num11);
						px[num10 + 2] = (byte)Math.Min(255f, (float)(int)b3 * num11);
						px[num10 + 3] = byte.MaxValue;
					}
				}
			}
		}
		for (int m = 0; m < Math.Max(1, L / 64); m++)
		{
			float num12 = hasard.Next(L);
			float num13 = bas - hasard.Next(Math.Max(1, hauteur));
			float num14 = (float)(hasard.NextDouble() * Math.PI);
			for (int n = 0; n < 10; n++)
			{
				num14 += (float)(hasard.NextDouble() - 0.5) * 0.5f;
				num12 += MathF.Cos(num14);
				num13 -= MathF.Abs(MathF.Sin(num14));
				int num15 = (int)num12;
				int num16 = (int)num13;
				if (num15 < 0 || num16 < 0 || num15 >= L || num16 >= num)
				{
					break;
				}
				int num17 = (num16 * L + num15) * 4;
				px[num17] = 58;
				px[num17 + 1] = 50;
				px[num17 + 2] = 48;
				px[num17 + 3] = byte.MaxValue;
			}
		}
	}

	private static bool DansLePolygone((float X, float Y)[] p, float x, float y)
	{
		bool flag = false;
		int num = 0;
		int num2 = p.Length - 1;
		while (num < p.Length)
		{
			if (p[num].Y > y != p[num2].Y > y && x < (p[num2].X - p[num].X) * (y - p[num].Y) / (p[num2].Y - p[num].Y) + p[num].X)
			{
				flag = !flag;
			}
			num2 = num++;
		}
		return flag;
	}

	internal static (byte R, byte G, byte B) Rampe((byte R, byte G, byte B)[] rampe, float t)
	{
		return rampe[Math.Clamp((int)MathF.Round(Math.Clamp(t, 0f, 1f) * (float)(rampe.Length - 1)), 0, rampe.Length - 1)];
	}

	internal static bool PresDUnSillon(List<SillonVu> sillons, float x0, float y0, float x1, float y1)
	{
		foreach (SillonVu sillon in sillons)
		{
			float num = sillon.Demi + 60f;
			int num2 = Math.Max(1, (int)MathF.Ceiling(sillon.Arc.Longueur / 16f));
			for (int i = 0; i <= num2; i++)
			{
				(float X, float Y, float Cos, float Sin) tuple = ArcDuRayon.Point(sillon.X0, sillon.Y0, sillon.Arc.Cos, sillon.Arc.Sin, sillon.K, sillon.Arc.Longueur * (float)i / (float)num2);
				float item = tuple.X;
				float item2 = tuple.Y;
				float num3 = MathF.Max(MathF.Max(x0 - item, 0f), item - x1);
				float num4 = MathF.Max(MathF.Max(y0 - item2, 0f), item2 - y1);
				if (num3 * num3 + num4 * num4 <= num * num)
				{
					return true;
				}
			}
		}
		return false;
	}

	public static void Recevoir(CasseSillonPacket paquet)
	{
		long milliseconds = Timing.Global.Milliseconds;
		lock (Verrou)
		{
			if (!SillonsVus.TryGetValue(paquet.MapId, out List<SillonVu> value))
			{
				value = (SillonsVus[paquet.MapId] = new List<SillonVu>());
			}
			SillonVu sillonVu = Nouveau(paquet, milliseconds);
			if (sillonVu.Genre == CasseTrace.Tranchee)
			{
				var (num, num2, num3) = sillonVu.Arc;
				foreach (SillonVu item in value)
				{
					if (item.Suivant == null && item.Genre == CasseTrace.Tranchee && num3 > 1f && MathF.Abs(item.X0 - sillonVu.X0) < 0.5f && MathF.Abs(item.Y0 - sillonVu.Y0) < 0.5f && MathF.Abs(item.Demi - sillonVu.Demi) < 0.5f && MathF.Abs(item.K - sillonVu.K) <= 1E-05f && item.Arc.Cos * num + item.Arc.Sin * num2 >= 0.999f)
					{
						item.Suivant = sillonVu;
					}
				}
			}
			value.Add(sillonVu);
			Changer(paquet.MapId, -1);
		}
		Presser();
		MapInstance.Get(paquet.MapId)?.CasseChangee();
	}

	private static void RecevoirSillons(Guid carte, CasseSillonPacket[] paquets, long maintenant)
	{
		lock (Verrou)
		{
			List<SillonVu> list = SillonsVus.GetValueOrDefault(carte) ?? new List<SillonVu>();
			List<SillonVu> list2 = new List<SillonVu>();
			foreach (CasseSillonPacket p in paquets)
			{
				SillonVu sillonVu = list.FirstOrDefault((SillonVu s) => s.Genre == p.Genre && MathF.Abs(s.X0 - p.X0) < 0.5f && MathF.Abs(s.Y0 - p.Y0) < 0.5f && MathF.Abs(s.X1 - p.X1) < 0.5f && MathF.Abs(s.Y1 - p.Y1) < 0.5f && MathF.Abs(s.Demi - ((p.Demi > 1f) ? p.Demi : 40f)) < 0.5f && MathF.Abs(s.K - (float.IsFinite(p.K) ? p.K : 0f)) <= 1E-05f);
				if (sillonVu != null)
				{
					list.Remove(sillonVu);
					list2.Add(sillonVu);
				}
				else
				{
					list2.Add(Nouveau(p, maintenant));
				}
			}
			foreach (SillonVu item in list)
			{
				item.Liberer();
			}
			if (list2.Count == 0)
			{
				SillonsVus.Remove(carte);
			}
			else
			{
				SillonsVus[carte] = list2;
			}
		}
	}

	private static SillonVu Nouveau(CasseSillonPacket p, long maintenant)
	{
		int num = ((p.Genre == CasseTrace.Tranchee) ? Math.Max(0, p.AgeMs) : p.AgeMs);
		float k = ((p.Genre == CasseTrace.Tranchee && float.IsFinite(p.K)) ? p.K : 0f);
		return new SillonVu
		{
			X0 = p.X0,
			Y0 = p.Y0,
			X1 = p.X1,
			Y1 = p.Y1,
			K = k,
			Arc = ArcDuRayon.DepuisLaCorde(p.X0, p.Y0, p.X1, p.Y1, k),
			Demi = ((p.Genre != CasseTrace.Tranchee) ? Math.Max(0.5f, p.Demi) : ((p.Demi > 1f) ? p.Demi : 40f)),
			Debut = maintenant - num,
			Fin = maintenant - num + Math.Max(1000, p.VieMs),
			Genre = p.Genre,
			RevelerMs = Math.Max(0, p.RevelerMs),
			Courbe = p.Courbe
		};
	}

	internal static List<(int X, int Y, int L, int H, byte[] Octets)>? MasquesDuSol(Guid carte)
	{
		lock (Verrou)
		{
			if (!SillonsVus.TryGetValue(carte, out List<SillonVu> value) || value.Count == 0)
			{
				return null;
			}
			List<(int, int, int, int, byte[])> list = null;
			foreach (SillonVu item in value)
			{
				byte[] masque = item.Masque;
				if (masque != null && item.Froid != null)
				{
					(list ?? (list = new List<(int, int, int, int, byte[])>())).Add((item.Gauche, item.Haut, item.MasqueL, item.MasqueH, masque));
				}
			}
			return list;
		}
	}

	internal static bool SousUnSillon(List<(int X, int Y, int L, int H, byte[] Octets)> masques, int x, int y)
	{
		foreach (var masque in masques)
		{
			int item = masque.X;
			int item2 = masque.Y;
			int item3 = masque.L;
			int item4 = masque.H;
			byte[] item5 = masque.Octets;
			int num = x - item;
			int num2 = y - item2;
			if (num >= 0 && num2 >= 0 && num < item3 && num2 < item4 && item5[(num2 * item3 + num) * 4 + 3] != 0)
			{
				return true;
			}
		}
		return false;
	}

	internal static List<SillonVu> SillonsDe(Guid carte)
	{
		long maintenant = Timing.Global.Milliseconds;
		lock (Verrou)
		{
			List<SillonVu> value;
			return SillonsVus.TryGetValue(carte, out value) ? value.Where((SillonVu s) => s.Fin > maintenant && s.Genre == CasseTrace.Tranchee && s.Suivant == null).ToList() : new List<SillonVu>();
		}
	}

	internal static void DessinerSillons(MapInstance carte)
	{
		long maintenant = Timing.Global.Milliseconds;
		List<SillonVu> list;
		lock (Verrou)
		{
			if (!SillonsVus.TryGetValue(carte.Id, out List<SillonVu> value) || value.Count == 0)
			{
				return;
			}
			foreach (SillonVu item in value.Where(delegate(SillonVu s)
			{
				if (s.Fin > maintenant)
				{
					SillonVu suivant = s.Suivant;
					if (suivant != null)
					{
						if (suivant.Froid == null)
						{
							return suivant.Vide;
						}
						return true;
					}
					return false;
				}
				return true;
			}).ToList())
			{
				item.Liberer();
				value.Remove(item);
			}
			list = value.ToList();
		}
		int num = 0;
		foreach (SillonVu item2 in list)
		{
			if (item2.Genre != CasseTrace.Tranchee && maintenant < item2.Debut)
			{
				continue;
			}
			if (item2.Froid == null && !item2.Vide && num++ == 0)
			{
				if (item2.Genre == CasseTrace.Tranchee)
				{
					FabriquerSillon(carte, item2);
				}
				else
				{
					FabriquerTrace(carte, item2);
				}
			}
			IGameTexture froid = item2.Froid;
			if (froid == null)
			{
				continue;
			}
			if (item2.Genre != CasseTrace.Tranchee)
			{
				DessinerTrace(carte, item2, froid, maintenant);
				continue;
			}
			Graphics.DrawGameTexture(froid, carte.X + item2.Gauche, carte.Y + item2.Haut);
			if (AvecDessus)
			{
				continue;
			}
			float num2 = Chaleur(item2, maintenant);
			if (num2 > 0.004f)
			{
				IGameTexture chaud = item2.Chaud;
				if (chaud != null)
				{
					Graphics.DrawGameTexture(chaud, new FloatRect(0f, 0f, chaud.Width, chaud.Height), new FloatRect(carte.X + item2.Gauche, carte.Y + item2.Haut, chaud.Width, chaud.Height), new Color((int)(255f * num2), 255, 255, 255));
				}
			}
		}
	}

	private static void FabriquerSillon(MapInstance carte, SillonVu s)
	{
		int val = Options.Instance.Map.MapWidth * Case;
		int val2 = Options.Instance.Map.MapHeight * Case;
		float num = s.Demi + 24f + ((s.K != 0f) ? (s.Arc.Longueur * s.Arc.Longueur * MathF.Abs(s.K) / 8f + 2f) : 0f);
		int num2 = Math.Max(0, (int)MathF.Floor(MathF.Min(s.X0, s.X1) - num));
		int num3 = Math.Min(val, (int)MathF.Ceiling(MathF.Max(s.X0, s.X1) + num));
		int num4 = Math.Max(0, (int)MathF.Floor(MathF.Min(s.Y0, s.Y1) - num));
		int num5 = Math.Min(val2, (int)MathF.Ceiling(MathF.Max(s.Y0, s.Y1) + num));
		if (num3 <= num2 || num5 <= num4)
		{
			s.Vide = true;
			return;
		}
		string key = Options.Instance.Map.Layers.All.FirstOrDefault() ?? "Ground";
		if (!carte.Layers.TryGetValue(key, out var value))
		{
			s.Vide = true;
			return;
		}
		int num6 = num3 - num2;
		int num7 = num5 - num4;
		float[] array = new float[num6 * num7];
		Dictionary<(int, int), byte[]> dictionary = new Dictionary<(int, int), byte[]>();
		if (!LireCeQuiSeDresse(carte, s, num2, num4, num6, num7))
		{
			return;
		}
		float num8 = 1f;
		float num9 = 0f;
		for (int i = 0; i < num7; i++)
		{
			for (int j = 0; j < num6; j++)
			{
				int num10 = num2 + j;
				int num11 = num4 + i;
				int num12 = num10 / Case;
				int num13 = num11 / Case;
				if (!dictionary.TryGetValue((num12, num13), out var value2))
				{
					value2 = null;
					if (num12 < value.GetLength(0) && num13 < value.GetLength(1) && value[num12, num13].TilesetTexture is IGameTexture planche && !TuileDe(planche, value[num12, num13].X, value[num12, num13].Y, out value2))
					{
						return;
					}
					dictionary[(num12, num13)] = value2;
				}
				float num14 = 0.45f;
				if (value2 != null)
				{
					int num15 = (num11 % Case * Case + num10 % Case) * 4;
					num14 = (0.3f * (float)(int)value2[num15] + 0.59f * (float)(int)value2[num15 + 1] + 0.11f * (float)(int)value2[num15 + 2]) / 255f;
				}
				array[i * num6 + j] = num14;
				num8 = MathF.Min(num8, num14);
				num9 = MathF.Max(num9, num14);
			}
		}
		int largeur;
		byte[] array2 = Meteo.EauDeLaCarte(carte, out largeur);
		int largeur2;
		ushort[] herbeVue = Meteo.HerbeDeLaCarte(carte, out largeur2);
		byte[] array3 = new byte[num6 * num7 * 4];
		byte[] array4 = new byte[num6 * num7 * 4];
		bool flag = true;
		for (int k = 0; k < num7; k++)
		{
			for (int l = 0; l < num6; l++)
			{
				int num16 = num2 + l;
				int num17 = num4 + k;
				if (array2 != null && num17 * largeur + num16 < array2.Length && array2[num17 * largeur + num16] != 0)
				{
					continue;
				}
				int num18 = num16 + carte.X;
				int num19 = num17 + carte.Y;
				float num20 = (array[k * num6 + l] - num8) / MathF.Max(0.01f, num9 - num8);
				float num21 = s.Distance((float)num16 + 0.5f, (float)num17 + 0.5f) + (Bruit(num18, num19, 83, 3f) - 0.5f) * 2f;
				float num22 = s.Demi + 8f + (Bruit(num18, num19, 84, 9f) - 0.5f) * 16f + (num20 - 0.5f) * 6f;
				(byte, byte, byte)? tuple = ((num21 < 1.5f) ? new(byte, byte, byte)?(Rampe(RampeCharbon, 0.1f + num20 * 0.2f)) : ((num21 < 3f) ? new(byte, byte, byte)?(Rampe(RampeCharbon, 0.2f + num20 * 0.3f)) : ((num21 < 5f) ? new(byte, byte, byte)?(Rampe(RampeCharbon, 0.35f + num20 * 0.4f)) : ((num21 < 9f) ? new(byte, byte, byte)?(Rampe(RampeTerre, num20)) : ((num21 < num22 * 0.42f) ? new(byte, byte, byte)?(Rampe(RampeCendre, num20 * 1.1f - 0.05f)) : ((num21 < num22 * 0.62f) ? new(byte, byte, byte)?(Rampe(RampeTerre, num20 * 0.7f + 0.3f)) : ((num21 < num22) ? new(byte, byte, byte)?(EstHerbe(herbeVue, largeur2, dictionary, num16, num17) ? Rampe(RampePaille, num20) : Roussi(dictionary, num16, num17, num20)) : (((byte, byte, byte)?)null))))))));
				if (tuple.HasValue)
				{
					(byte, byte, byte) valueOrDefault = tuple.GetValueOrDefault();
					int num23 = (k * num6 + l) * 4;
					ref byte reference = ref array3[num23];
					ref byte reference2 = ref array3[num23 + 1];
					ref byte reference3 = ref array3[num23 + 2];
					ref byte reference4 = ref array3[num23 + 3];
					byte item = valueOrDefault.Item1;
					byte item2 = valueOrDefault.Item2;
					byte item3 = valueOrDefault.Item3;
					reference = item;
					reference2 = item2;
					reference3 = item3;
					reference4 = byte.MaxValue;
					flag = false;
					(byte, byte, byte)? tuple2 = ((num21 < 1.5f) ? new(byte, byte, byte)?((byte.MaxValue, 214, 130)) : ((num21 < 3f) ? new(byte, byte, byte)?((246, 132, 46)) : ((num21 < 5f) ? new(byte, byte, byte)?((150, 56, 26)) : ((num21 < 8f && Hache(num18, num19, 85) % 100 < 4) ? new(byte, byte, byte)?((byte.MaxValue, 170, 70)) : (((byte, byte, byte)?)null)))));
					if (tuple2.HasValue)
					{
						(byte, byte, byte) valueOrDefault2 = tuple2.GetValueOrDefault();
						reference3 = ref array4[num23];
						reference2 = ref array4[num23 + 1];
						reference = ref array4[num23 + 2];
						ref byte reference5 = ref array4[num23 + 3];
						item3 = valueOrDefault2.Item1;
						item2 = valueOrDefault2.Item2;
						item = valueOrDefault2.Item3;
						reference3 = item3;
						reference2 = item2;
						reference = item;
						reference5 = byte.MaxValue;
					}
				}
			}
		}
		if (flag)
		{
			s.Vide = true;
			return;
		}
		s.Gauche = num2;
		s.Haut = num4;
		s.Froid = Graphics.Renderer?.CreateTextureFromPixels($"casse/sillon/{carte.Id}/{++_fabriquees}", num6, num7, array3);
		byte[] masque = array3;
		int masqueL = num6;
		int masqueH = num7;
		s.Masque = masque;
		s.MasqueL = masqueL;
		s.MasqueH = masqueH;
		s.OctetsChauds = array4;
		s.Chaud = Graphics.Renderer?.CreateTextureFromPixels($"casse/sillon/{carte.Id}/{++_fabriquees}", num6, num7, array4);
		if (s.Froid == null)
		{
			s.Vide = true;
		}
		else if (AvecDessus)
		{
			FaireLeDessus(carte, s, Timing.Global.Milliseconds);
		}
	}

	private static float Chaleur(SillonVu s, long maintenant)
	{
		long num = maintenant - s.Debut;
		if (num >= 700)
		{
			return Math.Clamp(1f - (float)(num - 700) / 5300f, 0f, 1f);
		}
		return 1f;
	}

	private static bool EstHerbe(ushort[]? herbeVue, int largeur, Dictionary<(int, int), byte[]?> octets, int x, int y)
	{
		if (herbeVue != null)
		{
			int num = y * largeur + x;
			if (num >= 0 && num < herbeVue.Length)
			{
				return herbeVue[num] != 0;
			}
			return false;
		}
		if (!octets.TryGetValue((x / Case, y / Case), out byte[] value) || value == null)
		{
			return true;
		}
		int num2 = (y % Case * Case + x % Case) * 4;
		if (value[num2 + 1] > value[num2] + 4)
		{
			return value[num2 + 1] > value[num2 + 2] + 4;
		}
		return false;
	}

	private static (byte R, byte G, byte B) Roussi(Dictionary<(int, int), byte[]?> octets, int x, int y, float t)
	{
		if (!octets.TryGetValue((x / Case, y / Case), out byte[] value) || value == null)
		{
			return Rampe(RampeTerre, t * 0.7f + 0.3f);
		}
		int num = (y % Case * Case + x % Case) * 4;
		if (value[num + 1] > value[num] + 4 && value[num + 1] > value[num + 2] + 4)
		{
			return Rampe(RampeTerre, t * 0.7f + 0.3f);
		}
		return (R: (byte)((float)(int)value[num] * 0.72f), G: (byte)((float)(int)value[num + 1] * 0.64f), B: (byte)((float)(int)value[num + 2] * 0.56f));
	}

	private static bool LireCeQuiSeDresse(MapInstance carte, SillonVu s, int x0, int y0, int L, int H)
	{
		short[] array = new short[L * H];
		Array.Fill(array, (short)(-2));
		List<(int, int)> list = new List<(int, int)>();
		List<string> lowerLayers = Options.Instance.Map.Layers.LowerLayers;
		byte[] eau;
		int largeurEau;
		Dictionary<(int, int), bool> casesDEau;
		SillonVu sillonVu;
		short[] dresses2;
		List<(int, int)> porteurs;
		if (lowerLayers.Count >= 2)
		{
			Dictionary<string, Autotile[,]> dictionary = carte.Autotiles?.Layers;
			if (dictionary != null)
			{
				int num = x0 / Case;
				int num2 = y0 / Case;
				int num3 = (x0 + L - 1) / Case;
				int num4 = (y0 + H - 1) / Case;
				CasseCarte casseCarte = CarteDe(carte);
				Dictionary<(string, int, int), int> dictionary2 = new Dictionary<(string, int, int), int>();
				if (casseCarte != null)
				{
					for (int i = 0; i < casseCarte.Objets.Length; i++)
					{
						CasseObjet casseObjet = casseCarte.Objets[i];
						if (casseObjet.X <= num3 && casseObjet.Y <= num4 && casseObjet.X + casseObjet.Largeur > num && casseObjet.Y + casseObjet.Hauteur > num2)
						{
							(string, int, int)[] cases = casseObjet.Cases;
							foreach ((string, int, int) key in cases)
							{
								dictionary2.TryAdd(key, i);
							}
						}
					}
				}
				eau = Meteo.EauDeLaCarte(carte, out largeurEau);
				casesDEau = new Dictionary<(int, int), bool>();
				MapAttribute[,] attributes = carte.Attributes;
				Dictionary<(int, int), short> dictionary3 = new Dictionary<(int, int), short>();
				for (int k = 1; k < lowerLayers.Count; k++)
				{
					if (!carte.Layers.TryGetValue(lowerLayers[k], out var value) || !dictionary.TryGetValue(lowerLayers[k], out var value2))
					{
						continue;
					}
					for (int l = Math.Max(0, num2); l <= num4 && l < value.GetLength(1); l++)
					{
						for (int m = Math.Max(0, num); m <= num3 && m < value.GetLength(0); m++)
						{
							Tile tile = value[m, l];
							Autotile autotile = value2[m, l];
							if (!(tile.TilesetTexture is IGameTexture planche) || autotile.RenderState == 0)
							{
								continue;
							}
							bool horsHerbe = false;
							short value4;
							if (casseCarte != null && dictionary2.TryGetValue((lowerLayers[k], m, l), out var value3))
							{
								int item = PartieTouchee(casseCarte, value3, ((float)m + 0.5f) * (float)Case, ((float)l + 0.5f) * (float)Case);
								if (!dictionary3.TryGetValue((value3, item), out value4))
								{
									value4 = (short)Math.Min(32767, list.Count);
									dictionary3[(value3, item)] = value4;
									list.Add((value3, item));
								}
								goto IL_034f;
							}
							bool flag = attributes != null && m < attributes.GetLength(0) && l < attributes.GetLength(1);
							bool flag2 = flag;
							bool flag3;
							if (flag2)
							{
								MapAttribute mapAttribute = attributes[m, l];
								if (mapAttribute is MapBlockedAttribute)
								{
									goto IL_030d;
								}
								if (mapAttribute is MapAnimationAttribute mapAnimationAttribute)
								{
									if (mapAnimationAttribute.IsBlock)
									{
										goto IL_030d;
									}
								}
								else if (mapAttribute is MapWarpAttribute)
								{
									goto IL_030d;
								}
								flag3 = false;
								goto IL_0315;
							}
							goto IL_0319;
							IL_0315:
							flag2 = flag3;
							goto IL_0319;
							IL_030d:
							flag3 = true;
							goto IL_0315;
							IL_0319:
							if (flag2)
							{
								value4 = -1;
							}
							else
							{
								if (!AuBordDeLEau(m, l) && !Meteo.TuileDeTerrain(planche, tile.X, tile.Y))
								{
									continue;
								}
								value4 = -1;
								horsHerbe = true;
							}
							goto IL_034f;
							IL_034f:
							if (autotile.RenderState == 1)
							{
								if (!TuileDe(planche, tile.X, tile.Y, out byte[] tuile))
								{
									return false;
								}
								if (tuile != null)
								{
									Dresser(array, L, H, x0, y0, tuile, Case, 0, 0, m * Case, l * Case, Case, Case, value4, horsHerbe);
								}
								continue;
							}
							if (!PlancheDe(planche, out byte[] rgba, out int largeur))
							{
								return false;
							}
							if (rgba != null)
							{
								byte autotile2 = tile.Autotile;
								flag3 = (uint)(autotile2 - 4) <= 1u;
								int num5 = (flag3 ? (-Case) : 0);
								for (int n = 1; n <= 4; n++)
								{
									Point16i point16i = autotile.QuarterTile[n];
									short[] dresses = array;
									byte[] rgba2 = rgba;
									int pl = largeur;
									int x1 = point16i.X;
									int sy = point16i.Y + num5;
									int num6 = m * Case;
									flag3 = ((n == 2 || n == 4) ? true : false);
									int dx = num6 + (flag3 ? (Case / 2) : 0);
									int num7 = l * Case;
									bool flag4 = (uint)(n - 3) <= 1u;
									Dresser(dresses, L, H, x0, y0, rgba2, pl, x1, sy, dx, num7 + (flag4 ? (Case / 2) : 0), Case / 2, Case / 2, value4, horsHerbe);
								}
							}
						}
					}
				}
				sillonVu = s;
				dresses2 = array;
				porteurs = list;
				s.Dresses = dresses2;
				sillonVu.Porteurs = porteurs;
				return true;
			}
		}
		sillonVu = s;
		dresses2 = array;
		porteurs = list;
		s.Dresses = dresses2;
		sillonVu.Porteurs = porteurs;
		return true;
		bool AuBordDeLEau(int cx, int cy)
		{
			for (int num8 = -1; num8 <= 1; num8++)
			{
				for (int num9 = -1; num9 <= 1; num9++)
				{
					if (CaseDEau(cx + num9, cy + num8))
					{
						return true;
					}
				}
			}
			return false;
		}
		bool CaseDEau(int cx, int cy)
		{
			if (eau == null || cx < 0 || cy < 0)
			{
				return false;
			}
			if (casesDEau.TryGetValue((cx, cy), out var value5))
			{
				return value5;
			}
			bool flag5 = false;
			for (int num8 = cy * Case; num8 < cy * Case + Case; num8++)
			{
				if (flag5)
				{
					break;
				}
				for (int num9 = cx * Case; num9 < cx * Case + Case && num9 < largeurEau; num9++)
				{
					int num10 = num8 * largeurEau + num9;
					if (num10 < eau.Length && eau[num10] != 0)
					{
						flag5 = true;
						break;
					}
				}
			}
			casesDEau[(cx, cy)] = flag5;
			return flag5;
		}
	}

	private static void Dresser(short[] dresses, int L, int H, int x0, int y0, byte[] rgba, int pl, int sx, int sy, int dx, int dy, int w, int h, short code, bool horsHerbe = false)
	{
		int num = rgba.Length / 4 / Math.Max(1, pl);
		for (int i = 0; i < h; i++)
		{
			int num2 = dy + i - y0;
			int num3 = sy + i;
			if (num2 < 0 || num2 >= H || num3 < 0 || num3 >= num)
			{
				continue;
			}
			for (int j = 0; j < w; j++)
			{
				int num4 = dx + j - x0;
				int num5 = sx + j;
				if (num4 < 0 || num4 >= L || num5 < 0 || num5 >= pl || rgba[(num3 * pl + num5) * 4 + 3] < 128)
				{
					continue;
				}
				int num6 = (num3 * pl + num5) * 4;
				if (!horsHerbe || rgba[num6 + 1] <= rgba[num6] + 4 || rgba[num6 + 1] <= rgba[num6 + 2] + 4)
				{
					ref short reference = ref dresses[num2 * L + num4];
					if (reference != -1 && (code == -1 || reference == -2))
					{
						reference = code;
					}
				}
			}
		}
	}

	private static bool PlancheDe(IGameTexture planche, out byte[]? rgba, out int largeur)
	{
		rgba = null;
		largeur = 0;
		long milliseconds = Timing.Global.Milliseconds;
		if (!PlanchesLues.TryGetValue(planche.Name, out PlancheLue value))
		{
			Dictionary<string, PlancheLue> planchesLues = PlanchesLues;
			string name = planche.Name;
			PlancheLue obj = new PlancheLue
			{
				Vue = milliseconds
			};
			value = obj;
			planchesLues[name] = obj;
			Decoder(planche, value);
		}
		byte[] array = value.Rgba;
		if (array == null)
		{
			if (!value.ParLaCarte)
			{
				return false;
			}
			array = LireParLaCarte(planche, value);
		}
		value.Vue = milliseconds;
		if (array.Length >= value.L * value.H * 4 && value.L > 0)
		{
			byte[] array2 = array;
			int l = value.L;
			rgba = array2;
			largeur = l;
		}
		return true;
	}

	private static bool Parti(Guid carte, CasseCarte reperee, int objet, int partie)
	{
		EtatPartie etatPartie = EtatDe(carte, objet, partie);
		if (etatPartie == null)
		{
			return false;
		}
		bool flag = etatPartie.Tranche || etatPartie.Couche != 0;
		bool flag2 = flag;
		if (!flag2)
		{
			bool flag3 = etatPartie.Etat == CasseEtat.Casse && objet < reperee.Objets.Length;
			bool flag4 = flag3;
			if (flag4)
			{
				CasseGenre genre = reperee.Objets[objet].Genre;
				bool flag5 = ((genre == CasseGenre.Foret || genre == CasseGenre.Poubelle) ? true : false);
				flag4 = !flag5;
			}
			flag2 = flag4;
		}
		return flag2;
	}

	private static void FaireLeDessus(MapInstance carte, SillonVu s, long maintenant)
	{
		byte[] masque = s.Masque;
		if (masque == null)
		{
			return;
		}
		byte[] octetsChauds = s.OctetsChauds;
		if (octetsChauds == null)
		{
			return;
		}
		short[] dresses = s.Dresses;
		if (dresses == null)
		{
			return;
		}
		CasseCarte casseCarte = CarteDe(carte);
		bool[] array = new bool[s.Porteurs.Count];
		int num = 17;
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = casseCarte != null && Parti(carte.Id, casseCarte, s.Porteurs[i].Objet, s.Porteurs[i].Partie);
			num = num * 31 + (array[i] ? (i + 1) : 0);
		}
		s.VersionDessus = Version(carte.Id);
		s.DessusA = maintenant;
		if (num == s.PartisDessus && s.FroidDessus != null)
		{
			return;
		}
		byte[] array2 = (byte[])masque.Clone();
		byte[] array3 = (byte[])octetsChauds.Clone();
		for (int j = 0; j < dresses.Length; j++)
		{
			short num2 = dresses[j];
			if (num2 == -1 || (num2 >= 0 && num2 < array.Length && !array[num2]))
			{
				array2[j * 4 + 3] = 0;
				array3[j * 4 + 3] = 0;
			}
		}
		IGameTexture gameTexture = Graphics.Renderer?.CreateTextureFromPixels($"casse/sillon/{carte.Id}/{++_fabriquees}", s.MasqueL, s.MasqueH, array2);
		IGameTexture gameTexture2 = Graphics.Renderer?.CreateTextureFromPixels($"casse/sillon/{carte.Id}/{++_fabriquees}", s.MasqueL, s.MasqueH, array3);
		s.FroidDessus?.Dispose();
		s.ChaudDessus?.Dispose();
		IGameTexture froidDessus = gameTexture;
		IGameTexture chaudDessus = gameTexture2;
		int partisDessus = num;
		s.FroidDessus = froidDessus;
		s.ChaudDessus = chaudDessus;
		s.PartisDessus = partisDessus;
	}

	internal static void DessinerSillonsDessus(MapInstance carte)
	{
		long milliseconds = Timing.Global.Milliseconds;
		List<SillonVu> list;
		lock (Verrou)
		{
			if (!SillonsVus.TryGetValue(carte.Id, out List<SillonVu> value) || value.Count == 0)
			{
				return;
			}
			list = value.ToList();
		}
		int num = Version(carte.Id);
		foreach (SillonVu item in list)
		{
			if (item.Genre != CasseTrace.Tranchee || item.Froid == null || item.Fin <= milliseconds)
			{
				continue;
			}
			if (item.VersionDessus != num && milliseconds - item.DessusA >= 150 && _dessusRefaitA != _image)
			{
				_dessusRefaitA = _image;
				FaireLeDessus(carte, item, milliseconds);
			}
			IGameTexture froidDessus = item.FroidDessus;
			if (froidDessus == null)
			{
				continue;
			}
			Graphics.DrawGameTexture(froidDessus, carte.X + item.Gauche, carte.Y + item.Haut);
			float num2 = Chaleur(item, milliseconds);
			if (num2 > 0.004f)
			{
				IGameTexture chaudDessus = item.ChaudDessus;
				if (chaudDessus != null)
				{
					Graphics.DrawGameTexture(chaudDessus, new FloatRect(0f, 0f, chaudDessus.Width, chaudDessus.Height), new FloatRect(carte.X + item.Gauche, carte.Y + item.Haut, chaudDessus.Width, chaudDessus.Height), new Color((int)(255f * num2), 255, 255, 255));
				}
			}
		}
	}

	private static float AvanceeDeGlissade(float t, bool contreLeDecor)
	{
		float num = 1f - (1f - t) * (1f - t);
		if (!contreLeDecor)
		{
			return num;
		}
		return 0.55f * num + 0.45f * t;
	}

	private static float PartCreusee(SillonVu s, long maintenant)
	{
		if (s.RevelerMs <= 0)
		{
			return 1f;
		}
		float num = Math.Clamp((float)(maintenant - s.Debut) / (float)s.RevelerMs, 0f, 1f);
		switch (s.Courbe)
		{
		case 1:
			return 1f - (1f - num) * (1f - num);
		case 2:
		{
			float num2 = AvanceeDeGlissade(0.45f, contreLeDecor: true);
			float t = 0.45f + num * 0.55f;
			return Math.Clamp((AvanceeDeGlissade(t, contreLeDecor: true) - num2) / MathF.Max(0.01f, 1f - num2), 0f, 1f);
		}
		case 3:
			return AvanceeDeGlissade(num, contreLeDecor: true);
		default:
			return num;
		}
	}

	private static void DessinerTrace(MapInstance carte, SillonVu s, IGameTexture texture, long maintenant)
	{
		long num = s.Fin - maintenant;
		float num2 = (((float)num < 20000f) ? Math.Clamp((float)num / 20000f, 0f, 1f) : 1f);
		if (num2 <= 0.01f)
		{
			return;
		}
		float num3 = texture.Width;
		float num4 = texture.Height;
		float num5 = 0f;
		float num6 = 0f;
		float num7 = num3;
		float num8 = num4;
		float num9 = PartCreusee(s, maintenant);
		if (num9 < 1f)
		{
			float num10 = s.X1 - s.X0;
			float num11 = s.Y1 - s.Y0;
			float num12 = s.X0 + num10 * num9 - (float)s.Gauche;
			float num13 = s.Y0 + num11 * num9 - (float)s.Haut;
			if (MathF.Abs(num10) >= MathF.Abs(num11))
			{
				if (num10 >= 0f)
				{
					num7 = Math.Clamp(num12 + 2f, 0f, num3);
				}
				else
				{
					num5 = Math.Clamp(num12 - 2f, 0f, num3);
					num7 = num3 - num5;
				}
			}
			else if (num11 >= 0f)
			{
				num8 = Math.Clamp(num13 + 2f, 0f, num4);
			}
			else
			{
				num6 = Math.Clamp(num13 - 2f, 0f, num4);
				num8 = num4 - num6;
			}
		}
		if (!(num7 <= 0.5f) && !(num8 <= 0.5f))
		{
			Graphics.DrawGameTexture(texture, new FloatRect(MathF.Round(num5), MathF.Round(num6), MathF.Round(num7), MathF.Round(num8)), new FloatRect((float)(carte.X + s.Gauche) + MathF.Round(num5), (float)(carte.Y + s.Haut) + MathF.Round(num6), MathF.Round(num7), MathF.Round(num8)), new Color((int)(255f * num2), 255, 255, 255));
		}
	}

	private static void FabriquerTrace(MapInstance carte, SillonVu s)
	{
		int val = Options.Instance.Map.MapWidth * Case;
		int val2 = Options.Instance.Map.MapHeight * Case;
		float num = ((s.Genre == CasseTrace.Fissure) ? (18f + s.Demi) : 10f);
		int num2 = Math.Max(0, (int)MathF.Floor(MathF.Min(s.X0, s.X1) - num));
		int num3 = Math.Min(val, (int)MathF.Ceiling(MathF.Max(s.X0, s.X1) + num));
		int num4 = Math.Max(0, (int)MathF.Floor(MathF.Min(s.Y0, s.Y1) - num));
		int num5 = Math.Min(val2, (int)MathF.Ceiling(MathF.Max(s.Y0, s.Y1) + num));
		if (num3 <= num2 || num5 <= num4)
		{
			s.Vide = true;
			return;
		}
		string key = Options.Instance.Map.Layers.All.FirstOrDefault() ?? "Ground";
		if (!carte.Layers.TryGetValue(key, out var value))
		{
			s.Vide = true;
			return;
		}
		int num6 = num3 - num2;
		int num7 = num5 - num4;
		byte[] array = new byte[num6 * num7 * 3];
		Dictionary<(int, int), byte[]> dictionary = new Dictionary<(int, int), byte[]>();
		for (int i = 0; i < num7; i++)
		{
			for (int j = 0; j < num6; j++)
			{
				int num8 = num2 + j;
				int num9 = num4 + i;
				int num10 = num8 / Case;
				int num11 = num9 / Case;
				if (!dictionary.TryGetValue((num10, num11), out var value2))
				{
					value2 = null;
					if (num10 < value.GetLength(0) && num11 < value.GetLength(1) && value[num10, num11].TilesetTexture is IGameTexture planche && !TuileDe(planche, value[num10, num11].X, value[num10, num11].Y, out value2))
					{
						return;
					}
					dictionary[(num10, num11)] = value2;
				}
				int num12 = i * num6 + j;
				ref byte reference = ref array[num12 * 3];
				ref byte reference2 = ref array[num12 * 3 + 1];
				ref byte reference3 = ref array[num12 * 3 + 2];
				reference = 110;
				reference2 = 120;
				reference3 = 60;
				if (value2 != null)
				{
					int num13 = (num9 % Case * Case + num8 % Case) * 4;
					reference2 = ref array[num12 * 3];
					reference = ref array[num12 * 3 + 1];
					ref byte reference4 = ref array[num12 * 3 + 2];
					byte b = value2[num13];
					byte b2 = value2[num13 + 1];
					byte b3 = value2[num13 + 2];
					reference2 = b;
					reference = b2;
					reference4 = b3;
				}
			}
		}
		byte[] array2 = ImageDeTrace(s, array, num6, num7, num2, num4);
		if (array2 == null)
		{
			s.Vide = true;
			return;
		}
		s.Gauche = num2;
		s.Haut = num4;
		s.Froid = Graphics.Renderer?.CreateTextureFromPixels($"casse/trace/{carte.Id}/{++_fabriquees}", num6, num7, array2);
		byte[] masque = array2;
		int masqueL = num6;
		int masqueH = num7;
		s.Masque = masque;
		s.MasqueL = masqueL;
		s.MasqueH = masqueH;
		if (s.Froid == null)
		{
			s.Vide = true;
		}
	}

	internal static byte[]? ImageDeTrace(SillonVu s, byte[] fond, int L, int H, int x0, int y0)
	{
		float[] array = new float[L * H];
		float num = 1f;
		float num2 = 0f;
		for (int i = 0; i < L * H; i++)
		{
			float y1 = (array[i] = (0.3f * (float)(int)fond[i * 3] + 0.59f * (float)(int)fond[i * 3 + 1] + 0.11f * (float)(int)fond[i * 3 + 2]) / 255f);
			num = MathF.Min(num, y1);
			num2 = MathF.Max(num2, y1);
		}
		byte[] array2 = new byte[L * H];
		int num3 = (int)Hache((int)s.X0, (int)s.Y0, (int)s.X1 * 31 + (int)s.Y1);
		if (s.Genre == CasseTrace.Glissade)
		{
			CreuserGlissade(s, array2, L, H, x0, y0, num3);
		}
		else
		{
			CreuserFissure(s, array2, L, H, x0, y0, num3);
		}
		bool flag = s.Genre == CasseTrace.Fissure;
		bool flag2 = MathF.Abs(s.Y1 - s.Y0) > MathF.Abs(s.X1 - s.X0);
		byte[] array3 = new byte[L * H * 4];
		bool flag3 = true;
		for (int j = 0; j < H; j++)
		{
			for (int k = 0; k < L; k++)
			{
				int num4 = j * L + k;
				byte b = array2[num4];
				bool flag4 = b == 0 && ((j > 0 && (array2[num4 - L] == 1 || (flag && array2[num4 - L] == 2))) || (flag2 && k > 0 && array2[num4 - 1] == 1)) && Bruit(x0 + k, y0 + j, num3 + 7, 3f) > 0.3f;
				if (b == 0 && !flag4)
				{
					continue;
				}
				byte b2 = fond[num4 * 3];
				byte b3 = fond[num4 * 3 + 1];
				byte b4 = fond[num4 * 3 + 2];
				float num5 = (array[num4] - num) / MathF.Max(0.01f, num2 - num);
				bool flag5 = Verdatre(b2, b3, b4);
				(byte, byte, byte) tuple;
				if (flag4)
				{
					tuple = (flag5 ? Rampe(RampePaille, 0.5f + num5 * 0.3f) : ((byte)Math.Min(255f, (float)(int)b2 * 1.12f + 16f), (byte)Math.Min(255f, (float)(int)b3 * 1.12f + 16f), (byte)Math.Min(255f, (float)(int)b4 * 1.12f + 16f)));
				}
				else if (b == 2)
				{
					tuple = (flag5 ? Rampe(RampeTerre, 0.45f + num5 * 0.4f) : ((byte)((float)(int)b2 * 0.6f), (byte)((float)(int)b3 * 0.6f), (byte)((float)(int)b4 * 0.6f)));
				}
				else
				{
					float num6 = ((s.Genre == CasseTrace.Fissure) ? 0.2f : 0.45f);
					uint num7 = Hache(x0 + k, y0 + j, num3);
					if (!flag && flag5 && num7 % 100 < 12)
					{
						tuple = ((byte)((float)(int)b2 * 0.62f), (byte)((float)(int)b3 * 0.62f), (byte)((float)(int)b4 * 0.62f));
					}
					else
					{
						float num8 = num6 * (0.4f + num5) + (float)(int)(num7 % 3 - 1) * 0.12f;
						tuple = (flag5 ? Rampe(RampeTerre, num8) : ((byte)((float)(int)b2 * (num8 + 0.15f)), (byte)((float)(int)b3 * (num8 + 0.15f)), (byte)((float)(int)b4 * (num8 + 0.15f))));
					}
				}
				int num9 = num4 * 4;
				ref byte reference = ref array3[num9];
				ref byte reference2 = ref array3[num9 + 1];
				ref byte reference3 = ref array3[num9 + 2];
				ref byte reference4 = ref array3[num9 + 3];
				byte item = tuple.Item1;
				byte item2 = tuple.Item2;
				byte item3 = tuple.Item3;
				reference = item;
				reference2 = item2;
				reference3 = item3;
				reference4 = byte.MaxValue;
				flag3 = false;
			}
		}
		if (!flag3)
		{
			return array3;
		}
		return null;
	}

	private static void Creuser(byte[] creux, int L, int H, int x0, int y0, float X, float Y, byte valeur = 1)
	{
		int num = (int)MathF.Round(X) - x0;
		int num2 = (int)MathF.Round(Y) - y0;
		if (num >= 0 && num2 >= 0 && num < L && num2 < H && creux[num2 * L + num] != 1)
		{
			creux[num2 * L + num] = valeur;
		}
	}

	private static void CreuserGlissade(SillonVu s, byte[] creux, int L, int H, int x0, int y0, int graine)
	{
		float num = s.X1 - s.X0;
		float num2 = s.Y1 - s.Y0;
		float num3 = MathF.Sqrt(num * num + num2 * num2);
		if (num3 < 1f)
		{
			return;
		}
		float num4 = num / num3;
		float num5 = num2 / num3;
		float num6 = 0f - num5;
		float num7 = num4;
		float[] array = new float[2] { -1f, 1f };
		foreach (float num8 in array)
		{
			for (float num9 = 0f; num9 <= num3; num9 += 0.5f)
			{
				float num10 = num9 / num3;
				float num11 = (Bruit((int)(num9 * 2f), (num8 > 0f) ? 3 : 9, graine, 9f) - 0.5f) * 2.4f;
				float num12 = Bruit((int)(num9 * 2f), (num8 > 0f) ? 5 : 11, graine + 1, 3f);
				if (num10 < 0.25f && num12 < 0.4f)
				{
					continue;
				}
				float num13 = num8 * s.Demi + num11;
				float num14 = s.X0 + num4 * num9 + num6 * num13;
				float num15 = s.Y0 + num5 * num9 + num7 * num13;
				Creuser(creux, L, H, x0, y0, num14, num15, 1);
				if (num10 > 0.3f && num12 > 0.22f)
				{
					if (MathF.Abs(num4) >= MathF.Abs(num5))
					{
						Creuser(creux, L, H, x0, y0, num14, num15 - 1f, 1);
					}
					else
					{
						Creuser(creux, L, H, x0, y0, num14 - 1f, num15, 1);
					}
				}
			}
		}
		float[] array2 = new float[2] { -1f, 1f };
		foreach (float num16 in array2)
		{
			float num17 = s.X1 + num6 * num16 * s.Demi;
			float num18 = s.Y1 + num7 * num16 * s.Demi;
			Creuser(creux, L, H, x0, y0, num17 + num4, num18 + num5, 2);
			Creuser(creux, L, H, x0, y0, num17 + num4 * 2f, num18 + num5 * 2f, 2);
			Creuser(creux, L, H, x0, y0, num17 + num4 * 1.5f + num6, num18 + num5 * 1.5f + num7, 2);
			Creuser(creux, L, H, x0, y0, num17 + num4 * 1.5f - num6, num18 + num5 * 1.5f - num7, 2);
		}
		Random random = new Random(graine);
		for (int k = 0; k < 8; k++)
		{
			bool flag = k < 6;
			float num19 = (flag ? s.X1 : s.X0);
			float num20 = (flag ? s.Y1 : s.Y0);
			float num21 = num19 + ((float)random.NextDouble() - 0.5f) * (flag ? 12f : 8f) + num4 * (flag ? 3f : (-1f));
			float y1 = num20 + ((float)random.NextDouble() - 0.5f) * (flag ? 8f : 5f);
			Creuser(creux, L, H, x0, y0, num21, y1, 2);
			if (random.Next(3) == 0)
			{
				Creuser(creux, L, H, x0, y0, num21 + 1f, y1, 2);
			}
		}
	}

	private static void CreuserFissure(SillonVu s, byte[] creux, int L, int H, int x0, int y0, int graine)
	{
		Random random = new Random(graine);
		float demi = s.Demi;
		float num = s.Demi * 0.5f;
		for (int i = (int)MathF.Floor(s.Y0 - num - 1f); i <= (int)MathF.Ceiling(s.Y0 + num + 1f); i++)
		{
			for (int j = (int)MathF.Floor(s.X0 - demi - 1f); j <= (int)MathF.Ceiling(s.X0 + demi + 1f); j++)
			{
				float num2 = ((float)j - s.X0) * ((float)j - s.X0) / (demi * demi) + ((float)i - s.Y0) * ((float)i - s.Y0) / (num * num);
				if (num2 < 1f)
				{
					Creuser(creux, L, H, x0, y0, j, i, (byte)(((float)i <= s.Y0) ? 1u : 2u));
				}
			}
		}
		for (int k = 0; k < 6; k++)
		{
			float x1 = (float)k / 6f * (float)Math.PI * 2f + (float)random.NextDouble() * 0.6f;
			float num3 = demi + 2f + (float)random.NextDouble() * 4f;
			for (float num4 = demi * 0.9f; num4 <= num3; num4 += 0.5f)
			{
				Creuser(creux, L, H, x0, y0, s.X0 + MathF.Cos(x1) * num4, s.Y0 + MathF.Sin(x1) * num4 * 0.45f, 1);
			}
		}
		float num5 = s.X1 - s.X0;
		float num6 = s.Y1 - s.Y0;
		float num7 = MathF.Sqrt(num5 * num5 + num6 * num6);
		if (num7 < 2f)
		{
			return;
		}
		float num8 = (0f - num6) / num7;
		float num9 = num5 / num7;
		(float, float)[] array = new(float, float)[11];
		for (int l = 0; l <= 10; l++)
		{
			float num10 = (float)l / 10f;
			float num11 = ((l == 0 || l == 10) ? 0f : (((float)random.NextDouble() - 0.5f) * 6f * (1f - num10 * 0.4f)));
			array[l] = (s.X0 + num5 * num10 + num8 * num11, s.Y0 + num6 * num10 + num9 * num11 * 0.6f);
		}
		for (int m = 0; m < 10; m++)
		{
			(float, float) tuple = array[m];
			float item = tuple.Item1;
			float item2 = tuple.Item2;
			(float, float) tuple2 = array[m + 1];
			float item3 = tuple2.Item1;
			float item4 = tuple2.Item2;
			int num12 = Math.Max(1, (int)MathF.Ceiling(MathF.Max(MathF.Abs(item3 - item), MathF.Abs(item4 - item2)) * 2f));
			for (int n = 0; n <= num12; n++)
			{
				float x2 = item + (item3 - item) * (float)n / (float)num12;
				float num13 = item2 + (item4 - item2) * (float)n / (float)num12;
				Creuser(creux, L, H, x0, y0, x2, num13, 1);
				if (((float)m + (float)n / (float)num12) / 10f < 0.35f)
				{
					Creuser(creux, L, H, x0, y0, x2, num13 - 1f, 1);
				}
			}
		}
		float[] array2 = new float[4] { 0.18f, 0.3f, 0.52f, 0.7f };
		foreach (float num15 in array2)
		{
			(float, float) tuple3 = array[(int)MathF.Round(num15 * 10f)];
			float item5 = tuple3.Item1;
			float item6 = tuple3.Item2;
			float num16 = ((random.Next(2) == 0) ? 1f : (-1f));
			float num17 = 4f + (float)random.NextDouble() * 5f * (1f - num15);
			float num18 = num5 / num7 * 0.5f + num8 * num16;
			float num19 = (num6 / num7 * 0.5f + num9 * num16) * 0.5f;
			float num20 = MathF.Max(0.01f, MathF.Sqrt(num18 * num18 + num19 * num19));
			for (float num21 = 0f; num21 <= num17; num21 += 0.5f)
			{
				Creuser(creux, L, H, x0, y0, item5 + num18 / num20 * num21, item6 + num19 / num20 * num21, 1);
			}
		}
	}

	internal static int VersionDuVoisinage(MapInstance carte)
	{
		int num = Version(carte.Id);
		if (VoisinagesVus.TryGetValue(carte.Id, out (long, int, int) value) && value.Item1 == _image && value.Item2 == num)
		{
			return value.Item3;
		}
		int num2 = num;
		for (int i = carte.GridY - 1; i <= carte.GridY + 1; i++)
		{
			for (int j = carte.GridX - 1; j <= carte.GridX + 1; j++)
			{
				if (j != carte.GridX || i != carte.GridY)
				{
					Guid guid = CarteDeLaGrille(carte, j, i);
					MapInstance instance;
					bool value2 = guid != Guid.Empty && MapInstance.TryGet(guid, out instance) && CarteDe(instance) != null;
					num2 = num2 * 31 + HashCode.Combine(guid, Version(guid), value2);
				}
			}
		}
		num2 &= 0x7FFFFFFF;
		VoisinagesVus[carte.Id] = (_image, num, num2);
		return num2;
	}

	private static Guid CarteDeLaGrille(MapInstance carte, int gx, int gy)
	{
		Guid[,] mapGrid = Globals.MapGrid;
		if (mapGrid == null || gx < 0 || gy < 0 || gx >= Globals.MapGridWidth || gy >= Globals.MapGridHeight || carte.GridX < 0 || carte.GridY < 0 || carte.GridX >= Globals.MapGridWidth || carte.GridY >= Globals.MapGridHeight || mapGrid[carte.GridX, carte.GridY] != carte.Id)
		{
			return Guid.Empty;
		}
		return mapGrid[gx, gy];
	}

	private static void AjouterZonesVoisines(List<Zone> zones, MapInstance carte, long maintenant, ref bool completes)
	{
		(int, int) value = (Options.Instance.Map.MapWidth * Options.Instance.Map.TileWidth, Options.Instance.Map.MapHeight * Options.Instance.Map.TileHeight);
		for (int i = carte.GridY - 1; i <= carte.GridY + 1; i++)
		{
			for (int j = carte.GridX - 1; j <= carte.GridX + 1; j++)
			{
				if (j == carte.GridX && i == carte.GridY)
				{
					continue;
				}
				Guid guid = CarteDeLaGrille(carte, j, i);
				if (!(guid == Guid.Empty) && ObjetsTouches(guid).Count != 0 && MapInstance.TryGet(guid, out MapInstance instance))
				{
					CasseCarte casseCarte = CarteDe(instance);
					if (casseCarte != null)
					{
						AjouterZones(zones, instance, casseCarte, maintenant, instance.X - carte.X, instance.Y - carte.Y, value, ref completes);
					}
				}
			}
		}
	}
}
