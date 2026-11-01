using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Intersect.Client.Core;
using Intersect.Client.Core.Sounds;
using Intersect.Client.Entities;
using Intersect.Client.Framework.Content;
using Intersect.Client.Framework.File_Management;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.General;
using Intersect.Client.Localization;
using Intersect.Client.MonoGame.Graphics;
using Intersect.Client.Networking;
using Intersect.Config;
using Intersect.Configuration;
using Intersect.Core;
using Intersect.Framework.Core;
using Intersect.Framework.Core.GameObjects.Lighting;
using Intersect.Framework.Core.GameObjects.Mapping.Tilesets;
using Intersect.Framework.Core.GameObjects.Maps;
using Intersect.Framework.Core.GameObjects.Maps.Attributes;
using Intersect.Localization;
using Intersect.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json.Linq;

namespace Intersect.Client.Maps;

public static class Meteo
{
	internal sealed class Flaque
	{
		public int Cx;

		public int Cy;

		public (sbyte Dx, sbyte Dy, float V)[] Pixels = Array.Empty<(sbyte, sbyte, float)>();
	}

	internal sealed class Analyse
	{
		public required MapInstance Carte;

		public int L;

		public int H;

		public byte[] Eau = Array.Empty<byte>();

		public bool[]? EauDeChute;

		public byte[] Dist = Array.Empty<byte>();

		public byte[] Seuil = Array.Empty<byte>();

		public byte[] Relief = Array.Empty<byte>();

		public byte[] FlaqueMap = Array.Empty<byte>();

		public int[] PointsEau = Array.Empty<int>();

		public int[] PointsSol = Array.Empty<int>();

		public int[] PointsHerbe = Array.Empty<int>();

		public byte[] Sable = Array.Empty<byte>();

		public ushort[] Herbe = Array.Empty<ushort>();

		public int[] PointsFleurs = Array.Empty<int>();

		public byte[]? RgbaHerbeClaire;

		public IGameTexture? TexHerbeClaire;

		public byte[]? RgbaOmbres;

		public IGameTexture? TexOmbres;

		public int OmbresL;

		public int OmbresH;

		public byte[]? RgbaSolLibre;

		public IGameTexture? TexSolLibre;

		public int SignatureCasse;

		public bool OmbresEnCours;

		public long OmbresRefaitesA;

		public (short X, short Y, sbyte Nx, sbyte Ny, float Phase)[] Rive = Array.Empty<(short, short, sbyte, sbyte, float)>();

		public Flaque[] Flaques = Array.Empty<Flaque>();

		public (short X, short Y, float Phase, float Vitesse, bool Forte)[] Etoiles = Array.Empty<(short, short, float, float, bool)>();

		public bool AEau;

		public byte[]? RgbaEau;

		public byte[]? RgbaSol;

		public byte[]? RgbaFlou;

		public byte[]? RgbaTerre;

		public IGameTexture? TexEau;

		public IGameTexture? TexSol;

		public IGameTexture? TexEauFlou;

		public IGameTexture? TexTerre;

		public IGameTexture? TexNeige;

		public float NeigeNiveau = -1f;

		public bool NeigeEnCours;

		public byte[]? RgbaNeige;

		public float NeigeNiveauPret;

		public (int Rive, byte Course)[] Plages = Array.Empty<(int, byte)>();

		public (short X, short Y, byte Rayon)[] Rochers = Array.Empty<(short, short, byte)>();

		public (short X, short Y, float Phase, float Vitesse, byte Longueur)[] Traits = Array.Empty<(short, short, float, float, byte)>();

		public int[] PointsProfonds = Array.Empty<int>();

		public byte[]? RgbaProfondeur;

		public byte[]? RgbaReflet;

		public bool[] LignesReflet = Array.Empty<bool>();

		public IGameTexture? TexProfondeur;

		public IGameTexture? TexReflet;

		public DonneesGrotte? Grotte;

		public DonneesDesert? Desert;

		public DonneesVille? Ville;

		public void Liberer()
		{
			TexEau?.Dispose();
			TexSol?.Dispose();
			TexEauFlou?.Dispose();
			TexTerre?.Dispose();
			TexNeige?.Dispose();
			TexProfondeur?.Dispose();
			TexReflet?.Dispose();
			TexHerbeClaire?.Dispose();
			TexOmbres?.Dispose();
			TexOmbres = null;
			TexSolLibre?.Dispose();
			TexSolLibre = null;
			Ville?.TexVitres?.Dispose();
			Ville?.TexEnseignes?.Dispose();
			if (Ville != null)
			{
				Ville.TexVitres = (Ville.TexEnseignes = null);
			}
			TexEau = (TexSol = (TexEauFlou = (TexTerre = (TexNeige = (TexProfondeur = (TexReflet = (TexHerbeClaire = null)))))));
			Herbe = Array.Empty<ushort>();
			EntreeGrotte[] array = Grotte?.Entrees ?? Array.Empty<EntreeGrotte>();
			foreach (EntreeGrotte entreeGrotte in array)
			{
				entreeGrotte.Voile?.Dispose();
				entreeGrotte.Voile = null;
			}
		}
	}

	private sealed class CarteAuDela
	{
		public MapInstance Carte;

		public bool Mer;

		public bool AuDela;

		public ulong Signature;

		public bool Cuite;

		public IGameTexture? Serpent;

		public IGameTexture? Ombre;
	}

	private sealed class ChuteVue
	{
		public required Cascades.Chute Chute;

		public required string Couche;

		public required Cascades.Tampon Eau;

		public required Cascades.Tampon Riviere;

		public required Cascades.Tampon Sol;

		public required Cascades.Tampon Air;

		public IGameTexture? TexEau;

		public IGameTexture? TexRiviere;

		public IGameTexture? TexSol;

		public IGameTexture? TexAir;

		public required HashSet<(int X, int Y)> CasesCouvertes;

		public (float X, float Y)[] Lucioles = Array.Empty<(float, float)>();

		public void Liberer()
		{
			TexEau?.Dispose();
			TexRiviere?.Dispose();
			TexSol?.Dispose();
			TexAir?.Dispose();
			TexEau = (TexRiviere = (TexSol = (TexAir = null)));
		}
	}

	private sealed class ChutesDeCarte
	{
		public required MapInstance Carte;

		public required List<ChuteVue> Chutes;
	}

	private sealed class BoucleDeChute : Sound
	{
		public int Volume
		{
			set
			{
				if (mSound != null)
				{
					mSound.Volume = Math.Clamp(value, 0, 100);
				}
			}
		}

		public BoucleDeChute()
			: base("cascade", loop: true, 0)
		{
			Volume = 0;
		}
	}

	internal struct Reglages
	{
		public float Nuages;

		public float Pluie;

		public float Orage;

		public float Brume;

		public float BrumeEau;

		public float Sable;

		public float Vent;

		public float Chaleur;

		public float Rayons;

		public float Neige;

		public float Froid;

		public void Approcher(in Reglages cible, float k)
		{
			Nuages += (cible.Nuages - Nuages) * k;
			Pluie += (cible.Pluie - Pluie) * k;
			Orage += (cible.Orage - Orage) * Math.Min(1f, k * 1.6f);
			Brume += (cible.Brume - Brume) * k;
			BrumeEau += (cible.BrumeEau - BrumeEau) * k;
			Sable += (cible.Sable - Sable) * k;
			Vent += (cible.Vent - Vent) * k;
			Chaleur += (cible.Chaleur - Chaleur) * k;
			Rayons += (cible.Rayons - Rayons) * k;
			Neige += (cible.Neige - Neige) * k;
			Froid += (cible.Froid - Froid) * k;
		}

		public static Reglages Melange(in Reglages a, in Reglages b, float k)
		{
			Reglages result = a;
			result.Approcher(in b, k);
			result.Orage = a.Orage + (b.Orage - a.Orage) * k;
			return result;
		}
	}

	internal sealed record Temps(string Cle, string Nom, Reglages R)
	{
		public string Affiche
		{
			get
			{
				if (!Strings.Weather.Names.TryGetValue(Cle, out LocalizedString value))
				{
					return Nom;
				}
				return value.ToString();
			}
		}
	}

	internal enum GenreZone : byte
	{
		Tempere,
		Ile,
		Desert,
		Foret,
		Montagne,
		Interieur
	}

	internal enum Climat : byte
	{
		Dehors,
		Desert,
		Interieur
	}

	private sealed record Zone(GenreZone Genre, int Lucioles, bool Feuilles, bool Eclairs, bool Mer);

	private enum Ciel : byte
	{
		Beau,
		Nuageux,
		Venteux,
		Pluie,
		Orage,
		Brouillard
	}

	internal readonly record struct Vitre(float X, float Y, float Largeur, float Hauteur, float Force);

	internal sealed class DonneesDesert
	{
		public int[] Sables = Array.Empty<int>();

		public int[] Clairs = Array.Empty<int>();

		public byte[] Luma = Array.Empty<byte>();

		public byte[] Obstacles = Array.Empty<byte>();

		public (short X, short Y)[] Cretes = Array.Empty<(short, short)>();

		public (short X, short Y)[] Carcasses = Array.Empty<(short, short)>();

		public (short CacheX, short CacheY, short X, short Y)[] Cachettes = Array.Empty<(short, short, short, short)>();

		public int Colonnes;
	}

	private struct OndeEau
	{
		public float X;

		public float Y;

		public float Age;

		public float Vie;

		public float Rayon;

		public float Force;
	}

	private struct GerbeEau
	{
		public float X;

		public float Y;

		public float Age;

		public float Taille;
	}

	private struct GoutteEau
	{
		public float X;

		public float Y;

		public float H;

		public float Vx;

		public float Vy;

		public float Vh;

		public float Age;

		public bool Claire;
	}

	private sealed class Poisson
	{
		public float X;

		public float Y;

		public float Angle;

		public float Vitesse;

		public float Virage;

		public float Taille;

		public float Queue;

		public float Saut = -1f;

		public float DepartX;

		public float DepartY;

		public float ArriveeX;

		public float ArriveeY;
	}

	private sealed class Banc
	{
		public required MapInstance Carte;

		public required Poisson[] Poissons;

		public double ProchainSaut;
	}

	private sealed class DepartKi
	{
		public float X;

		public float Y;

		public long Vu;

		public long ProchainsEmbruns;
	}

	private sealed class ChargeEau
	{
		public required Entity Entite;

		public long Debut;

		public long Fin = -1L;

		public float X;

		public float Y;

		public int Graine;

		public bool AuBord;
	}

	private sealed class Personnage
	{
		public long Image = -10L;

		public readonly (IGameTexture Texture, FloatRect Source, FloatRect Destination, byte Alpha)[] Couches = new(IGameTexture, FloatRect, FloatRect, byte)[8];

		public int NbCouches;

		public bool PresDeLEau;

		public float PiedsX;

		public float PiedsY;

		public float Immersion;

		public int LignesImmergees;

		public float DirX = 1f;

		public float DirY;

		public long BougeA = -100000L;

		public long Vu;

		public long ProchainesGouttes;
	}

	private sealed class RayonEau
	{
		public long Vu;

		public long Tir;

		public long Arret;

		public long ProchainsEmbruns;

		public long ProchaineGerbe;

		public long ProchaineVapeur;

		public long ProchaineVague;

		public bool Retombe;

		public bool Dessus;

		public float X0;

		public float Y0;

		public float Cos;

		public float Sin;

		public float Demi;

		public float Force;

		public float Ferme;

		public float Premier;

		public float Dernier;

		public float K;

		public int Niveau;

		public float OuvertDe = float.MaxValue;

		public float OuvertA = float.MinValue;
	}

	private sealed class TraceEau
	{
		public float X0;

		public float Y0;

		public float Cos;

		public float Sin;

		public float De;

		public float A;

		public float Vie;

		public float Force;

		public float K;

		public long Debut;

		public bool Ecume;
	}

	private sealed class VapeurEau
	{
		public float X;

		public float Y;

		public float Derive;

		public float Vie;

		public long Debut;
	}

	internal sealed class DonneesGrotte
	{
		public (short X, short Haut, short Bas)[] Parois = Array.Empty<(short, short, short)>();

		public (short X, short Y)[][] Colonies = Array.Empty<(short, short)[]>();

		public (short X, short Y)[] Gouttes = Array.Empty<(short, short)>();

		public (short X, short Y, byte Rx, byte Ry)[] Bassins = Array.Empty<(short, short, byte, byte)>();

		public int[] Cailloux = Array.Empty<int>();

		public EntreeGrotte[] Entrees = Array.Empty<EntreeGrotte>();
	}

	internal sealed class EntreeGrotte
	{
		public short X;

		public short Y;

		public sbyte Dx;

		public sbyte Dy;

		public short X0;

		public short Y0;

		public short L;

		public short H;

		public byte[] Poids = Array.Empty<byte>();

		public IGameTexture? Voile;

		public int VoileCle = -1;

		public byte[]? RgbaVoile;
	}

	internal struct Etalonnage
	{
		public float R;

		public float G;

		public float B;

		public float VoileR;

		public float VoileG;

		public float VoileB;

		public float VoileA;

		public float Desaturation;

		public float Lampes;

		public float Jour;

		public float Soleil;

		public float NuitClaire;

		public float Coucher;

		public float Eclairage;
	}

	private struct Goutte
	{
		public float X;

		public float Y;

		public float YFin;

		public float Vitesse;

		public byte Couche;
	}

	private struct Anneau
	{
		public float X;

		public float Y;

		public float Age;

		public float Vie;

		public float RMax;

		public float Force;
	}

	private struct Eclab
	{
		public float X;

		public float Y;

		public float Age;

		public float Force;

		public sbyte Miroir;
	}

	private struct Flocon
	{
		public float X;

		public float Y;

		public float YFin;

		public float Vitesse;

		public float Phase;

		public byte Couche;
	}

	private struct Pose
	{
		public float X;

		public float Y;

		public float Age;
	}

	private struct Feuille
	{
		public float X;

		public float Y;

		public float Phase;

		public float Vx;

		public byte Couleur;
	}

	private struct Grain
	{
		public float X;

		public float Y;

		public float V;

		public float A;

		public float Phase;

		public byte Longueur;
	}

	private struct Mouton
	{
		public float X;

		public float Y;

		public float Age;

		public float Vie;

		public byte Longueur;
	}

	private struct Luciole
	{
		public float Cx;

		public float Cy;

		public float Ax;

		public float Ay;

		public float Ph;

		public float V;

		public float Clig;
	}

	private sealed class Tourbillon
	{
		public float X;

		public float Y;

		public float Age;

		public float Vie;
	}

	private sealed class Eclair
	{
		public double T0;

		public float X;

		public float Y;

		public bool Loin;

		public bool Eau;

		public bool Eclabousse;

		public List<(float X, float Y)> Tronc = new List<(float, float)>();

		public List<List<(float X, float Y)>> Branches = new List<List<(float, float)>>();
	}

	private enum Sol : byte
	{
		Hors,
		Terre,
		Eau,
		Flaque
	}

	private sealed class Boucle : Sound
	{
		public int Volume
		{
			get
			{
				return mSound?.Volume ?? 0;
			}
			set
			{
				if (mSound != null)
				{
					mSound.Volume = Math.Clamp(value, 0, 100);
				}
			}
		}

		public Boucle(string fichier)
			: base(fichier, loop: true, 0)
		{
			Volume = 0;
		}
	}

	internal sealed class DonneesVille
	{
		public bool Urbaine;

		public (short A, short B, bool Horizontale)[] Routes = Array.Empty<(short, short, bool)>();

		public Vitre2[] Vitres = Array.Empty<Vitre2>();

		public (short X0, short Y0, short L, short H, byte R, byte G, byte B)[] Enseignes = Array.Empty<(short, short, short, short, byte, byte, byte)>();

		public (short X, short Y)[] Tuyaux = Array.Empty<(short, short)>();

		public (short X, short Y)[] Pigeons = Array.Empty<(short, short)>();

		public bool[] Trottoir = Array.Empty<bool>();

		public int TrottoirL;

		public int TrottoirH;

		public (short X, short Y)[] Poules = Array.Empty<(short, short)>();

		public byte[]? RgbaVitres;

		public byte[]? RgbaEnseignes;

		public IGameTexture? TexVitres;

		public IGameTexture? TexEnseignes;
	}

	internal readonly record struct Vitre2(short X0, short Y0, short L, short H, float Seuil, float Extinction, bool Haut);

	internal sealed class ReperesConnus
	{
		public readonly List<(short X, short Y)> Cheminees = new List<(short, short)>();

		public readonly List<(short X, short Y, short L, short H, short CroixX, short CroixL, short CroixY, short CroixH)> Fenetres = new List<(short, short, short, short, short, short, short, short)>();

		public readonly List<(short X, short Y, short L, short H)> PasDesFenetres = new List<(short, short, short, short)>();
	}

	private static readonly Dictionary<Guid, Analyse> Analyses = new Dictionary<Guid, Analyse>();

	private static readonly HashSet<Guid> AnalysesEnCours = new HashSet<Guid>();

	private static readonly ConcurrentQueue<Analyse> AnalysesPretes = new ConcurrentQueue<Analyse>();

	private static readonly Dictionary<IGameTexture, (byte[] Rgba, int L, int H, long Vu)> PixelsPlanches = new Dictionary<IGameTexture, (byte[], int, int, long)>();

	private static readonly List<MapInstance> _visibles = new List<MapInstance>();

	private static readonly List<Analyse> AFinaliser = new List<Analyse>();

	private static double _tDeLaDerniereTexture = -1.0;

	private static double _tDuDernierLancement = -1.0;

	private static object? _eauEnvoyeePour;

	private static readonly HashSet<Guid> DejaDit = new HashSet<Guid>();

	private static float[]? _grain;

	private const string PlancheDesMorts = "tileset7.png";

	private const string PlancheDuSerpent = "route_serpent.png";

	private const int NuagesPourUneMer = 64;

	private const int TuilesPourLAuDela = 16;

	private static bool _auDelaEnPanne;

	private const string FichierAuDela = "au_dela.json";

	private static readonly HashSet<Guid> CartesDuRoyaume = new HashSet<Guid>();

	private static DateTime _dateAuDela;

	private static long _verificationAuDela;

	private static readonly Dictionary<Guid, CarteAuDela> CartesAuDela = new Dictionary<Guid, CarteAuDela>();

	private static readonly Dictionary<ulong, (IGameTexture? Serpent, IGameTexture? Ombre)> SerpentsCuits = new Dictionary<ulong, (IGameTexture, IGameTexture)>();

	private static readonly List<CarteAuDela> _voisinesAuDela = new List<CarteAuDela>();

	private static IGameTexture? _texMerProche;

	private static IGameTexture? _texMerLointaine;

	private static IGameTexture? _texVoileAuDela;

	private static IGameTexture? _texRoseAuDela;

	private static IGameTexture? _texAme;

	private static byte[][]? _auDelaPret;

	private static bool _auDelaEnCours;

	private static readonly float[] TrameAuDela = new float[16]
	{
		1f / 32f,
		17f / 32f,
		5f / 32f,
		21f / 32f,
		25f / 32f,
		9f / 32f,
		29f / 32f,
		13f / 32f,
		7f / 32f,
		23f / 32f,
		3f / 32f,
		19f / 32f,
		31f / 32f,
		15f / 32f,
		27f / 32f,
		11f / 32f
	};

	private static readonly (byte, byte, byte)[] PaletteProche = new(byte, byte, byte)[9]
	{
		(216, 120, 0),
		(240, 152, 0),
		(248, 176, 0),
		(248, 208, 0),
		(248, 208, 64),
		(248, 232, 64),
		(248, 232, 64),
		(248, 248, 112),
		(248, 248, 160)
	};

	private static readonly (byte, byte, byte)[] PaletteLointaine = new(byte, byte, byte)[7]
	{
		(104, 0, 0),
		(120, 0, 0),
		(144, 16, 0),
		(168, 48, 0),
		(192, 80, 0),
		(216, 112, 0),
		(232, 136, 0)
	};

	private static readonly string[] DessinDeLAme = new string[18]
	{
		"....bbbbb.....", "..bbwwwwwbb...", ".bwwwwwwwwwb..", ".bwwwwwwwwwb..", "bwwwwwwwwwwwb.", "bwwwwwwwwwwwb.", "bwwwwwwwwwwwb.", ".bwwwwwwwwwb..", ".bwwwwwwwwwb..", "..bbwwwwwwb...",
		"....bbwwwwb...", "......bwwwb...", ".......bwwb...", "........bwwb..", ".........bwb..", "..........bwb.", "...........bb.", "............b."
	};

	private const int VoileL = 256;

	private const int VoileH = 80;

	private const int RoseH = 64;

	private const string PlancheDesChutes = "majeur.png";

	private const int RangeeDeLaLevre = 43;

	private const int RangeeDuRideau = 44;

	private const int RangeeDuPied = 45;

	private const double VitesseDuRideau = 0.058;

	private const float PorteeDuSon = 640f;

	private const float PleinSon = 48f;

	private const int PlafondDuSon = 62;

	private static readonly Dictionary<Guid, ChutesDeCarte> ChutesParCarte = new Dictionary<Guid, ChutesDeCarte>();

	private static bool _chutesEnPanne;

	private static int _imageDesChutes;

	private static BoucleDeChute? _sonDesChutes;

	private static float _niveauDesChutes;

	private static float _silenceDesChutes;

	private static readonly ConcurrentQueue<(Analyse Cible, Analyse Calcul, int Signature)> OmbresRefaites = new ConcurrentQueue<(Analyse, Analyse, int)>();

	private static long _ombresRefaitesNumero;

	private const string Fichier = "meteo.json";

	private static readonly Dictionary<string, Temps> TousLesTemps = new Temps[21]
	{
		new Temps("soleil", "Grand soleil", R(0.2f, 0f, 0f, 0f, 0f, 0f, 0.15f, 0.12f)),
		new Temps("nuageux", "Nuageux", R(0.6f, 0f, 0f, 0f, 0f, 0f, 0.35f)),
		new Temps("venteux", "Grand vent", R(0.4f, 0f, 0f, 0f, 0f, 0f, 0.85f)),
		new Temps("ventfroid", "Vent glacial", R(0.45f, 0f, 0f, 0f, 0f, 0f, 0.9f, 0f, 0f, 0f, 1f)),
		new Temps("pluie", "Pluie", R(0.92f, 0.65f, 0f, 0f, 0f, 0f, 0.35f)),
		new Temps("orage", "Orage", R(1f, 1f, 1f, 0f, 0f, 0f, 0.95f)),
		new Temps("brumemer", "Brume de mer", R(0.4f, 0f, 0f, 0.9f, 1f, 0f, 0.06f)),
		new Temps("canicule", "Canicule", R(0f, 0f, 0f, 0f, 0f, 0f, 0.1f, 1f)),
		new Temps("ventsable", "Vent de sable", R(0.1f, 0f, 0f, 0f, 0f, 0.4f, 0.6f)),
		new Temps("tempete", "Tempête de sable", R(0.25f, 0f, 0f, 0f, 0f, 1f, 1f)),
		new Temps("averse", "Averse", R(0.8f, 0.5f, 0f, 0f, 0f, 0f, 0.3f)),
		new Temps("eclaircie", "Éclaircie", R(0.28f, 0f, 0f, 0f, 0f, 0f, 0.15f, 0f, 1f)),
		new Temps("couvert", "Couvert", R(0.66f, 0f, 0f, 0f, 0f, 0f, 0.33f)),
		new Temps("pluiefine", "Pluie fine", R(0.85f, 0.38f, 0f, 0f, 0f, 0f, 0.18f)),
		new Temps("brouillard", "Brouillard", R(0.5f, 0f, 0f, 1f, 0f, 0f, 0.05f)),
		new Temps("froid", "Ciel froid", R(0.18f, 0f, 0f, 0f, 0f, 0f, 0.2f, 0f, 0f, 0f, 1f)),
		new Temps("nuageuxfroid", "Nuageux et froid", R(0.65f, 0f, 0f, 0f, 0f, 0f, 0.3f, 0f, 0f, 0f, 1f)),
		new Temps("neige", "Neige", R(0.85f, 0f, 0f, 0f, 0f, 0f, 0.22f, 0f, 0f, 0.7f, 1f)),
		new Temps("blizzard", "Tempête de neige", R(1f, 0f, 0f, 0.5f, 0f, 0f, 1f, 0f, 0f, 1f, 1f)),
		new Temps("givre", "Brouillard givrant", R(0.5f, 0f, 0f, 0.9f, 0f, 0f, 0.05f, 0f, 0f, 0f, 1f)),
		new Temps("calme", "Aucune", R())
	}.ToDictionary<Temps, string>((Temps t) => t.Cle, StringComparer.OrdinalIgnoreCase);

	private static readonly int[] PoidsCiel = new int[6] { 30, 22, 11, 18, 8, 11 };

	private static readonly Dictionary<Climat, string[]> ParClimat = new Dictionary<Climat, string[]>
	{
		[Climat.Dehors] = new string[6] { "soleil", "nuageux", "venteux", "pluie", "orage", "brouillard" },
		[Climat.Desert] = new string[6] { "canicule", "soleil", "ventsable", "ventsable", "tempete", "soleil" },
		[Climat.Interieur] = new string[6] { "calme", "calme", "calme", "calme", "calme", "calme" }
	};

	private static readonly Dictionary<GenreZone, Zone> Zones = new Dictionary<GenreZone, Zone>
	{
		[GenreZone.Tempere] = new Zone(GenreZone.Tempere, 14, Feuilles: true, Eclairs: true, Mer: false),
		[GenreZone.Ile] = new Zone(GenreZone.Ile, 8, Feuilles: true, Eclairs: true, Mer: true),
		[GenreZone.Desert] = new Zone(GenreZone.Desert, 4, Feuilles: false, Eclairs: false, Mer: false),
		[GenreZone.Foret] = new Zone(GenreZone.Foret, 24, Feuilles: true, Eclairs: true, Mer: false),
		[GenreZone.Montagne] = new Zone(GenreZone.Montagne, 0, Feuilles: false, Eclairs: true, Mer: false),
		[GenreZone.Interieur] = new Zone(GenreZone.Interieur, 0, Feuilles: false, Eclairs: false, Mer: false)
	};

	private static bool _enPanne;

	private static bool _actif = true;

	private static float _minutesParMeteo = 8f;

	private static float _secondesTransition = 75f;

	private static readonly Dictionary<Guid, GenreZone> ZonesCartes = new Dictionary<Guid, GenreZone>();

	private static readonly Dictionary<Guid, List<Vitre>> VitresCartes = new Dictionary<Guid, List<Vitre>>();

	private static DateTime _dateFichier;

	private static long _verificationFichier;

	private static bool _initialise;

	private static double _t;

	private static Reglages _p;

	private static GenreZone _zone = GenreZone.Interieur;

	private static string _nomTemps = "";

	private static string _nomSuivant = "";

	private static float _secondesAvantSuivant;

	private static float _mouille;

	private static float _enneigement;

	private const string FichierMonde = "carte_monde.json";

	private static readonly Dictionary<Guid, string> PaysDesCartes = new Dictionary<Guid, string>();

	private static readonly Dictionary<string, GenreZone> GenreDesPays = new Dictionary<string, GenreZone>(StringComparer.OrdinalIgnoreCase);

	private static DateTime _dateMonde;

	private static long _verificationMonde;

	private static string _cleTemps = "calme";

	private static readonly DateTime OrigineDuJeu = new DateTime(2000, 1, 1);

	internal const float VentX = -0.99f;

	internal const float VentY = 0.12f;

	private static readonly Color Teinte = new Color(255, 255, 255, 255);

	private static readonly FloatRect UnPixel = new FloatRect(0f, 0f, 1f, 1f);

	private static IGameTexture? _texNuages;

	private static IGameTexture? _texBrume;

	private static IGameTexture? _texBrume2;

	private static IGameTexture? _texSable;

	private static IGameTexture? _texRayons;

	private static IGameTexture? _texVignette;

	private static IGameTexture? _texHalo;

	private static byte[][]? _voilesPrets;

	private static bool _voilesEnCours;

	private static IGameRenderTexture? _toileBrume;

	private const float BrumeNappes = 0.26f;

	private const float BrumeFilets = 0.16f;

	private const float BrumeSurLEau = 0.3f;

	private const float BrumeVoile = 0.07f;

	private const float BrumeDegageRayon = 112f;

	private const float BrumeDegageForce = 0.75f;

	private static readonly List<(MapInstance Carte, Analyse A)> _analysesVisibles = new List<(MapInstance, Analyse)>();

	private static readonly (sbyte X, sbyte Y)[][] Couronne = new(sbyte, sbyte)[4][]
	{
		new(sbyte, sbyte)[1] { (0, 0) },
		new(sbyte, sbyte)[3]
		{
			(-1, -1),
			(1, -1),
			(0, 0)
		},
		new(sbyte, sbyte)[4]
		{
			(-2, -1),
			(2, -2),
			(-1, -2),
			(1, 0)
		},
		new(sbyte, sbyte)[3]
		{
			(-3, -2),
			(3, -3),
			(0, -3)
		}
	};

	private static readonly (sbyte X, sbyte Y)[][] PosesFeuille = new(sbyte, sbyte)[3][]
	{
		new(sbyte, sbyte)[3]
		{
			(0, 0),
			(1, 0),
			(1, 1)
		},
		new(sbyte, sbyte)[3]
		{
			(0, 1),
			(1, 0),
			(1, 1)
		},
		new(sbyte, sbyte)[3]
		{
			(0, 0),
			(1, 1),
			(2, 1)
		}
	};

	private const int HauteurReflet = 28;

	private static readonly OndeEau[] OndesEau = new OndeEau[200];

	private static int _nbOndesEau;

	private static readonly GerbeEau[] GerbesEau = new GerbeEau[40];

	private static int _nbGerbesEau;

	private static readonly GoutteEau[] GouttesEau = new GoutteEau[500];

	private static int _nbGouttesEau;

	private static readonly Dictionary<Guid, Banc> Bancs = new Dictionary<Guid, Banc>();

	private const float DureeSaut = 540f;

	private static MapInstance? _carteEau;

	private static Analyse? _analyseEau;

	private static readonly (float Dx, float H)[] LanguesDeGerbe = new(float, float)[5]
	{
		(-7f, 0.45f),
		(-3f, 0.8f),
		(0f, 1f),
		(4f, 0.75f),
		(8f, 0.4f)
	};

	private static readonly float[] MonteeDeGerbe = new float[7] { 0.4f, 0.85f, 1f, 0.9f, 0.6f, 0.35f, 0.15f };

	private static readonly (int R, int G, int B, float A, int Epais, int Rab)[] CouchesDeGerbe = new(int, int, int, float, int, int)[3]
	{
		(120, 190, 240, 0.9f, 3, 1),
		(215, 240, 255, 0.95f, 2, -2),
		(255, 255, 255, 1f, 1, -5)
	};

	private const long ChargeRelachementMs = 900L;

	private static readonly Dictionary<object, DepartKi> DepartsKi = new Dictionary<object, DepartKi>(ReferenceEqualityComparer.Instance);

	private static readonly Dictionary<Guid, ChargeEau> ChargesEau = new Dictionary<Guid, ChargeEau>();

	private static readonly List<object> _departsOublies = new List<object>();

	private static readonly List<Guid> _chargesFinies = new List<Guid>();

	private const int CouchesMax = 8;

	private static readonly Dictionary<Entity, Personnage> Personnages = new Dictionary<Entity, Personnage>(ReferenceEqualityComparer.Instance);

	private static readonly List<Entity> _personnagesPartis = new List<Entity>();

	private static long _imageEau;

	private static IGameRenderTexture? _toileReflets;

	private static readonly Dictionary<object, RayonEau> RayonsEau = new Dictionary<object, RayonEau>(ReferenceEqualityComparer.Instance);

	private static readonly List<object> _rayonsOublies = new List<object>();

	private static readonly List<TraceEau> TracesEau = new List<TraceEau>();

	private static readonly List<VapeurEau> VapeursEau = new List<VapeurEau>();

	private static long _dehorsCalculeA = -1L;

	private static (int R, int G, int B, float Force) _dehors = (R: 255, G: 228, B: 176, Force: 1f);

	private const string PlancheLampadaire = "lampadaire.png";

	private const int LampadaireGlobeX = 33;

	private const int LampadaireGlobeY = 42;

	private const int LampadairePiedY = 150;

	private static readonly Dictionary<Guid, (MapInstance Carte, List<(int X, int Y)> Cases)> LampadairesParCarte = new Dictionary<Guid, (MapInstance, List<(int, int)>)>();

	private static Guid? _plancheLampadaire;

	private static bool _plancheLampadaireCherchee;

	private static readonly Dictionary<(Guid Carte, int X, int Y), (bool Tombe, long Depuis, long Vu)> EtatsDesLampadaires = new Dictionary<(Guid, int, int), (bool, long, long)>();

	private static readonly (float Heure, float R, float G, float B, float VR, float VG, float VB, float VA, float Desat, float Lampes)[] Cles = new(float, float, float, float, float, float, float, float, float, float)[14]
	{
		(0f, 70f, 86f, 146f, 0f, 0f, 0f, 0f, 0.36f, 1f),
		(4f, 72f, 88f, 150f, 0f, 0f, 0f, 0f, 0.34f, 1f),
		(5f, 118f, 108f, 160f, 60f, 30f, 90f, 0.06f, 0.25f, 0.75f),
		(6f, 236f, 164f, 150f, 255f, 150f, 120f, 0.08f, 0.06f, 0.3f),
		(7f, 255f, 214f, 186f, 255f, 190f, 140f, 0.05f, 0f, 0f),
		(8.2f, 255f, 244f, 230f, 0f, 0f, 0f, 0f, 0f, 0f),
		(12f, 255f, 255f, 255f, 0f, 0f, 0f, 0f, 0f, 0f),
		(16.8f, 255f, 247f, 232f, 0f, 0f, 0f, 0f, 0f, 0f),
		(18f, 255f, 222f, 176f, 255f, 180f, 90f, 0.04f, 0f, 0f),
		(19f, 255f, 170f, 106f, 255f, 140f, 60f, 0.09f, 0f, 0.15f),
		(20.2f, 192f, 122f, 138f, 200f, 90f, 110f, 0.06f, 0.08f, 0.55f),
		(21.2f, 102f, 96f, 158f, 60f, 40f, 110f, 0.04f, 0.22f, 0.9f),
		(22f, 70f, 86f, 146f, 0f, 0f, 0f, 0f, 0.36f, 1f),
		(24f, 70f, 86f, 146f, 0f, 0f, 0f, 0f, 0.36f, 1f)
	};

	private static Etalonnage _etal;

	private const float Douceur = 0.8f;

	private const float PoidsDuTemps = 0.75f;

	private static float _teinteR = 1f;

	private static float _teinteG = 1f;

	private static float _teinteB = 1f;

	private static double _tTeinte;

	internal const int MargeOmbres = 32;

	private static readonly HashSet<string> PlanchesDeTerrain = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "plateaux.png", "plateaux-vides.png", "sols.png", "ombres.png", "herbes.png", "ville.png" };

	private const int EchelleOmbres = 1;

	private const int OmbreDecalageX = 8;

	private const int OmbreDecalageY = 6;

	private static readonly Goutte[] Gouttes = new Goutte[2200];

	private static int _nbGouttes;

	private static readonly Anneau[] Anneaux = new Anneau[1100];

	private static int _nbAnneaux;

	private static readonly Eclab[] Eclabs = new Eclab[1100];

	private static int _nbEclabs;

	private static readonly Flocon[] Flocons = new Flocon[2600];

	private static int _nbFlocons;

	private static readonly Pose[] Poses = new Pose[900];

	private static int _nbPoses;

	private static readonly Pose[] Traces = new Pose[160];

	private static int _nbTraces;

	private static readonly Feuille[] Feuilles = new Feuille[90];

	private static int _nbFeuilles;

	private static readonly Grain[] Grains = new Grain[900];

	private static int _nbGrains;

	private static readonly Mouton[] Moutons = new Mouton[160];

	private static int _nbMoutons;

	private static readonly Luciole[] Lucioles = new Luciole[48];

	private static int _nbLucioles;

	private static Tourbillon? _tourbillon;

	private static Eclair? _eclair;

	private static Random Hasard = new Random();

	private static float _dernierX;

	private static float _dernierY;

	private static long _pasTrace;

	private static long _pasGohan;

	private static readonly (int R, int G, int B)[][] CouleursFeuilles = new(int, int, int)[2][]
	{
		new(int, int, int)[3]
		{
			(62, 128, 58),
			(96, 158, 70),
			(140, 170, 80)
		},
		new(int, int, int)[3]
		{
			(70, 140, 50),
			(118, 168, 58),
			(160, 128, 52)
		}
	};

	private const long CreneauEclairMs = 1500L;

	private static long _creneauEclair = long.MinValue;

	private static double _secousse;

	public const int BlocSable = 4;

	private static readonly string[] NomsBoucles = new string[5] { "meteo_pluie", "meteo_vent", "meteo_mer", "meteo_sable", "meteo_grillons" };

	private static readonly Boucle?[] Boucles = new Boucle[5];

	private static readonly float[] Niveaux = new float[5];

	private static readonly float[] Silences = new float[5];

	private static readonly List<(double Quand, float Force)> Tonnerres = new List<(double, float)>();

	private static int _dernierTonnerre;

	private static readonly (string Planche, int X, int Y)[] ChemineesConnues = new(string, int, int)[4]
	{
		("batiments.png", 290, 3307),
		("batiments.png", 516, 3277),
		("batiments.png", 96, 3512),
		("batiments.png", 375, 3529)
	};

	private static readonly (string Planche, int X, int Y, int L, int H, int CroixX, int CroixL, int CroixY, int CroixH)[] FenetresConnues = new(string, int, int, int, int, int, int, int, int)[8]
	{
		("batiments.png", 99, 3405, 21, 18, 9, 3, 8, 2),
		("batiments.png", 246, 3405, 20, 18, 9, 2, 8, 2),
		("batiments.png", 457, 3345, 13, 16, 5, 3, 7, 3),
		("batiments.png", 470, 3399, 28, 27, 12, 5, 11, 5),
		("batiments.png", 76, 3625, 25, 24, 11, 3, 10, 4),
		("batiments.png", 136, 3632, 22, 22, 9, 4, 9, 3),
		("batiments.png", 363, 3630, 24, 24, 10, 4, 10, 4),
		("batiments.png", 478, 3649, 9, 9, 0, 0, 0, 0)
	};

	private static readonly (string Planche, int X, int Y, int L, int H)[] PasDesFenetres = new(string, int, int, int, int)[1] { ("batiments.png", 421, 3639, 40, 38) };

	internal static int VersionAuDela { get; private set; }

	internal static Reglages P => _p;

	internal static float Heure => Time.HeureDuJour;

	internal static float Mouille => _mouille;

	internal static float Enneigement => _enneigement;

	internal static GenreZone ZoneActuelle => _zone;

	internal static double TempsMs => _t;

	private static long MaintenantPartage => (long)((Time.TempsDuJeu - OrigineDuJeu).TotalMilliseconds / (double)Time.Vitesse);

	internal static bool EnPanne => _enPanne;

	internal static bool AuDesert
	{
		get
		{
			if (!_enPanne && _actif && _initialise)
			{
				return _zone == GenreZone.Desert;
			}
			return false;
		}
	}

	internal static float ForceDuVent
	{
		get
		{
			if (!_enPanne && _initialise)
			{
				return Math.Clamp(_p.Vent, 0f, 1f);
			}
			return 0f;
		}
	}

	internal static float JourActuel
	{
		get
		{
			if (!_enPanne && _initialise)
			{
				return Math.Clamp(_etal.Jour, 0f, 1f);
			}
			return 1f;
		}
	}

	public static bool Dehors
	{
		get
		{
			if (!_enPanne && _actif && _initialise)
			{
				return _zone != GenreZone.Interieur;
			}
			return false;
		}
	}

	public static float PartDuJour
	{
		get
		{
			if (!_enPanne && _initialise)
			{
				return Math.Clamp(_etal.Jour, 0f, 1f);
			}
			return 1f;
		}
	}

	public static bool MauvaisTemps
	{
		get
		{
			if (!_enPanne && _initialise)
			{
				if (!(_p.Pluie > 0.25f) && !(_p.Neige > 0.25f) && !(_p.Sable > 0.5f))
				{
					return _p.Orage > 0.3f;
				}
				return true;
			}
			return false;
		}
	}

	public static float HeureAffichee => Heure;

	public static bool Nuit
	{
		get
		{
			float num = (Heure % 24f + 24f) % 24f;
			if (!(num < 5f))
			{
				return num >= 21f;
			}
			return true;
		}
	}

	public static string NomTempsAffiche
	{
		get
		{
			if (!_enPanne && _actif && _initialise && _zone != GenreZone.Interieur)
			{
				return _nomTemps;
			}
			return "";
		}
	}

	public static string NomSuivantAffiche
	{
		get
		{
			if (NomTempsAffiche.Length != 0)
			{
				return _nomSuivant;
			}
			return "";
		}
	}

	public static float SecondesAvantSuivant => _secondesAvantSuivant;

	public static string IconeTemps
	{
		get
		{
			switch (_cleTemps)
			{
			case "soleil":
			case "froid":
				return Nuit ? "lune" : "soleil";
			case "canicule":
				return Nuit ? "lune" : "canicule";
			case "eclaircie":
				return Nuit ? "lune" : "eclaircie";
			case "couvert":
			case "nuageux":
			case "nuageuxfroid":
				return "nuage";
			case "ventfroid":
			case "venteux":
				return "vent";
			case "averse":
			case "pluie":
			case "pluiefine":
				return "pluie";
			case "orage":
				return "orage";
			case "givre":
			case "brumemer":
			case "brouillard":
				return "brouillard";
			case "neige":
			case "blizzard":
				return "neige";
			case "ventsable":
			case "tempete":
				return "sable";
			default:
				return Nuit ? "lune" : "soleil";
			}
		}
	}

	public static float VentDeSable
	{
		get
		{
			if (!_enPanne && _actif && _initialise)
			{
				return Math.Clamp(_p.Sable, 0f, 1f);
			}
			return 0f;
		}
	}

	internal static float LampesActuelles
	{
		get
		{
			if (!_enPanne && _initialise)
			{
				return Math.Clamp(_etal.Lampes, 0f, 1f);
			}
			return 0f;
		}
	}

	internal static float HeureActuelle => (Heure % 24f + 24f) % 24f;

	internal static List<MapInstance> CartesVisibles()
	{
		_visibles.Clear();
		if (!(Globals.Me?.MapInstance is MapInstance mapInstance) || Globals.MapGrid == null)
		{
			return _visibles;
		}
		FloatRect worldViewport = Graphics.WorldViewport;
		int num = Options.Instance.Map.MapWidth * Options.Instance.Map.TileWidth;
		int num2 = Options.Instance.Map.MapHeight * Options.Instance.Map.TileHeight;
		for (int i = Math.Max(mapInstance.GridX - 1, 0); i <= Math.Min(mapInstance.GridX + 1, Globals.MapGridWidth - 1); i++)
		{
			for (int j = Math.Max(mapInstance.GridY - 1, 0); j <= Math.Min(mapInstance.GridY + 1, Globals.MapGridHeight - 1); j++)
			{
				Guid guid = Globals.MapGrid[i, j];
				if (!(guid == Guid.Empty) && MapInstance.TryGet(guid, out MapInstance instance) && instance.IsLoaded && !((float)(instance.X + num) < worldViewport.Left) && !((float)instance.X > worldViewport.Right) && !((float)(instance.Y + num2) < worldViewport.Top) && !((float)instance.Y > worldViewport.Bottom))
				{
					_visibles.Add(instance);
				}
			}
		}
		return _visibles;
	}

	internal static Analyse? AnalyseDe(MapInstance carte)
	{
		if (Globals.Me != null && _eauEnvoyeePour != Globals.Me)
		{
			_eauEnvoyeePour = Globals.Me;
			foreach (Analyse value2 in Analyses.Values)
			{
				EnvoyerLEauDesCases(value2);
			}
		}
		Analyse result;
		while (AnalysesPretes.TryDequeue(out result))
		{
			AFinaliser.Add(result);
		}
		FinaliserUnPeu();
		PoserLesOmbresRefaites();
		if (Analyses.TryGetValue(carte.Id, out Analyse value) && value.Carte == carte)
		{
			SuivreLaCasse(value);
			return value;
		}
		if (!AnalysesEnCours.Contains(carte.Id) && _tDuDernierLancement != _t)
		{
			using (ChronoImage.Etape("meteo : lancement d'une analyse"))
			{
				Lancer(carte);
			}
			if (AnalysesEnCours.Contains(carte.Id))
			{
				_tDuDernierLancement = _t;
			}
		}
		if (value?.Carte != carte)
		{
			return null;
		}
		return value;
	}

	private static void FinaliserUnPeu()
	{
		if (AFinaliser.Count == 0 || _tDeLaDerniereTexture == _t)
		{
			return;
		}
		_tDeLaDerniereTexture = _t;
		Analyse analyse = AFinaliser[0];
		if (!MapInstance.TryGet(analyse.Carte.Id, out MapInstance instance) || instance != analyse.Carte)
		{
			AFinaliser.RemoveAt(0);
			AnalysesEnCours.Remove(analyse.Carte.Id);
			analyse.Liberer();
			return;
		}
		bool flag;
		using (ChronoImage.Etape("meteo : texture d'analyse"))
		{
			flag = FinaliserUnPas(analyse);
		}
		if (flag)
		{
			AFinaliser.RemoveAt(0);
			AnalysesEnCours.Remove(analyse.Carte.Id);
			if (Analyses.Remove(analyse.Carte.Id, out Analyse value))
			{
				value.Liberer();
			}
			Analyses[analyse.Carte.Id] = analyse;
			EnvoyerLEauDesCases(analyse);
		}
	}

	private static void EnvoyerLEauDesCases(Analyse a)
	{
		int mapWidth = Options.Instance.Map.MapWidth;
		int mapHeight = Options.Instance.Map.MapHeight;
		int tileWidth = Options.Instance.Map.TileWidth;
		int tileHeight = Options.Instance.Map.TileHeight;
		if (Globals.Me == null || a.Eau.Length != a.L * a.H || a.L < mapWidth * tileWidth || a.H < mapHeight * tileHeight)
		{
			return;
		}
		byte[] array = new byte[(mapWidth * mapHeight + 7) / 8];
		for (int i = 0; i < mapHeight; i++)
		{
			for (int j = 0; j < mapWidth; j++)
			{
				int num = 0;
				int num2 = 0;
				for (int k = i * tileHeight + tileHeight / 2 - 4; k < (i + 1) * tileHeight; k += 2)
				{
					int num4;
					int num5;
					for (int l = j * tileWidth + 6; l < (j + 1) * tileWidth - 6; num = num4 + num5, l += 2)
					{
						num2++;
						int num3 = k * a.L + l;
						num4 = num;
						if (a.Eau[num3] == 0)
						{
							bool[] eauDeChute = a.EauDeChute;
							if (eauDeChute == null || !eauDeChute[num3])
							{
								num5 = 0;
								continue;
							}
						}
						num5 = 1;
					}
				}
				if (num > 0 && num * 10 >= num2)
				{
					int num6 = i * mapWidth + j;
					array[num6 >> 3] |= (byte)(1 << (num6 & 7));
				}
			}
		}
		PacketSender.SendEauDesCases(a.Carte.Id, array);
	}

	private static (byte[] Rgba, int L, int H)? PixelsDuFichier(IGameTexture texture, long maintenant)
	{
		lock (PixelsPlanches)
		{
			if (PixelsPlanches.TryGetValue(texture, out (byte[], int, int, long) value))
			{
				Dictionary<IGameTexture, (byte[] Rgba, int L, int H, long Vu)> pixelsPlanches = PixelsPlanches;
				(byte[], int, int, long) value2 = value;
				value2.Item4 = maintenant;
				pixelsPlanches[texture] = value2;
				return (value.Item1, value.Item2, value.Item3);
			}
		}
		(byte[], int, int)? result = null;
		try
		{
			using Stream stream = texture.OuvrirSource();
			if (stream != null)
			{
				result = PngBrut.Lire(stream);
			}
		}
		catch (Exception exception)
		{
			ApplicationContext.Context.Value?.Logger.LogWarning(exception, "Meteo : planche {Planche} illisible sans la carte graphique", texture.Name);
		}
		if (result.HasValue)
		{
			(byte[], int, int) valueOrDefault = result.GetValueOrDefault();
			lock (PixelsPlanches)
			{
				PixelsPlanches[texture] = (valueOrDefault.Item1, valueOrDefault.Item2, valueOrDefault.Item3, maintenant);
				return result;
			}
		}
		return null;
	}

	private static void Lancer(MapInstance carte)
	{
		if (!carte.PrepareLocalMapTextures() || carte.Autotiles?.Layers == null || !SolsPeints.ToutLu(carte.Id))
		{
			return;
		}
		int tw = Options.Instance.Map.TileWidth;
		int th = Options.Instance.Map.TileHeight;
		int mapWidth = Options.Instance.Map.MapWidth;
		int mapHeight = Options.Instance.Map.MapHeight;
		long maintenant = Timing.Global.Milliseconds;
		List<(byte[] Rgba, int PL, int PH, int Sx, int Sy, int W, int Hh, int Dx, int Dy)> poses = new List<(byte[], int, int, int, int, int, int, int, int)>(mapWidth * mapHeight * 2);
		List<(IGameTexture Planche, int Sx, int Sy, int W, int Hh, int Dx, int Dy, int Etage, bool Debout, bool Masque, bool Sol)> aPoser = new List<(IGameTexture, int, int, int, int, int, int, int, bool, bool, bool)>(mapWidth * mapHeight * 2);
		Dictionary<IGameTexture, (byte[] Rgba, int L, int H)?> dejaLues = new Dictionary<IGameTexture, (byte[], int, int)?>();
		bool grotte = EstGrotte(carte);
		bool desert = ZoneDe(carte) == GenreZone.Desert;
		GenreZone genreZone = ZoneDe(carte);
		bool flag = ((genreZone <= GenreZone.Ile || genreZone == GenreZone.Foret) ? true : false);
		bool ville = flag;
		List<(byte[] Rgba, int PL, int PH, int Sx, int Sy, int W, int Hh, int Dx, int Dy)> posesHaut = ((grotte || desert || ville) ? new List<(byte[], int, int, int, int, int, int, int, int)>() : null);
		ReperesConnus reperes = (ville ? new ReperesConnus() : null);
		List<(byte[] Rgba, int PL, int PH, int Sx, int Sy, int W, int Hh, int Dx, int Dy)> posesDebout = ((ZoneDe(carte) == GenreZone.Interieur || grotte) ? null : new List<(byte[], int, int, int, int, int, int, int, int)>());
		List<(byte[] Rgba, int PL, int PH, int Sx, int Sy, int W, int Hh, int Dx, int Dy)> posesMasque = ((posesDebout == null) ? null : new List<(byte[], int, int, int, int, int, int, int, int)>());
		List<(byte[] Rgba, int PL, int PH, int Sx, int Sy, int W, int Hh, int Dx, int Dy)> posesSol = ((posesDebout == null) ? null : new List<(byte[], int, int, int, int, int, int, int, int)>());
		List<(byte[] Rgba, int PL, int PH, int Sx, int Sy, int W, int Hh, int Dx, int Dy)> posesEauHaute = new List<(byte[], int, int, int, int, int, int, int, int)>();
		List<(byte[] Rgba, int PL, int PH, int Sx, int Sy, int W, int Hh, int Dx, int Dy)> posesFond = new List<(byte[], int, int, int, int, int, int, int, int)>();
		List<(int Pose, int De, int A)> posesDeChute = new List<(int, int, int)>();
		List<(byte[] Rgba, int PL, int PH, int Sx, int Sy, int W, int Hh, int Dx, int Dy)> posesObjets = new List<(byte[], int, int, int, int, int, int, int, int)>();
		bool[] array = ((posesDebout == null) ? null : TuilesDeSol(carte, mapWidth, mapHeight));
		List<(string, int)> list = new List<(string, int)>();
		List<string> lowerLayers = Options.Instance.Map.Layers.LowerLayers;
		for (int i = 0; i < lowerLayers.Count; i++)
		{
			list.Add((lowerLayers[i], (i == lowerLayers.Count - 1 && lowerLayers.Count > 1) ? 1 : 0));
		}
		if (posesHaut != null || posesDebout != null)
		{
			foreach (string item18 in Options.Instance.Map.Layers.MiddleLayers.Concat(Options.Instance.Map.Layers.UpperLayers))
			{
				list.Add((item18, 2));
			}
		}
		foreach (var (text, num) in list)
		{
			if (carte.Layers.TryGetValue(text, out var value) && carte.Autotiles.Layers.TryGetValue(text, out var value2))
			{
				for (int j = 0; j < mapWidth; j++)
				{
					for (int k = 0; k < mapHeight; k++)
					{
						Tile tile = value[j, k];
						if (!(tile.TilesetTexture is IGameTexture gameTexture) || tile.X < 0 || tile.Y < 0 || tile.X * tw >= gameTexture.Width || tile.Y * th >= gameTexture.Height || !Lisible(gameTexture))
						{
							continue;
						}
						Autotile autotile = value2[j, k];
						if (autotile.RenderState == 1)
						{
							bool flag2 = num == 1 && array != null && array[j + k * mapWidth];
							aPoser.Add((gameTexture, tile.X * tw, tile.Y * th, tw, th, j * tw, k * th, num, num > 0 && !TuileDeTerrain(gameTexture, tile.X, tile.Y), num > 0 && !flag2, flag2));
						}
						else if (autotile.RenderState == 2)
						{
							byte autotile2 = tile.Autotile;
							flag = (uint)(autotile2 - 4) <= 1u;
							int num2 = (flag ? (-th) : 0);
							for (int l = 1; l <= 4; l++)
							{
								Point16i point16i = autotile.QuarterTile[l];
								List<(IGameTexture, int, int, int, int, int, int, int, bool, bool, bool)> list2 = aPoser;
								IGameTexture item = gameTexture;
								int x = point16i.X;
								int item2 = point16i.Y + num2;
								int item3 = tw / 2;
								int item4 = th / 2;
								int num3 = j * tw;
								flag = ((l == 2 || l == 4) ? true : false);
								int item5 = num3 + (flag ? (tw / 2) : 0);
								int num4 = k * th;
								bool flag3 = (uint)(l - 3) <= 1u;
								list2.Add((item, x, item2, item3, item4, item5, num4 + (flag3 ? (th / 2) : 0), num, false, false, false));
							}
						}
					}
				}
			}
			IGameTexture gameTexture2 = SolsPeints.Texture(carte.Id, text);
			if (gameTexture2 != null && Lisible(gameTexture2))
			{
				aPoser.Add((gameTexture2, 0, 0, -1, -1, 0, 0, num, false, false, false));
			}
		}
		byte[] cases = null;
		if (grotte)
		{
			MapAttribute[,] attributes = carte.Attributes;
			if (attributes != null && attributes.GetLength(0) >= mapWidth && attributes.GetLength(1) >= mapHeight)
			{
				cases = new byte[mapWidth * mapHeight];
				for (int m = 0; m < mapWidth; m++)
				{
					for (int n = 0; n < mapHeight; n++)
					{
						byte[] array2 = cases;
						int num5 = m + n * mapWidth;
						MapAttribute mapAttribute = attributes[m, n];
						byte autotile2 = (byte)((mapAttribute is MapBlockedAttribute) ? 1 : ((mapAttribute is MapWarpAttribute) ? 2 : 0));
						array2[num5] = autotile2;
					}
				}
			}
		}
		lock (PixelsPlanches)
		{
			IGameTexture[] array3 = (from p in PixelsPlanches
				where maintenant - p.Value.Vu > 120000
				select p.Key).ToArray();
			foreach (IGameTexture key in array3)
			{
				PixelsPlanches.Remove(key);
			}
		}
		Analyse analyse = new Analyse
		{
			Carte = carte,
			L = mapWidth * tw,
			H = mapHeight * th
		};
		bool chaleur = ZoneDe(carte) == GenreZone.Desert;
		AnalysesEnCours.Add(carte.Id);
		Task.Run(delegate
		{
			try
			{
				foreach (var item19 in aPoser)
				{
					IGameTexture item6 = item19.Planche;
					int item7 = item19.Sx;
					int item8 = item19.Sy;
					int item9 = item19.W;
					int item10 = item19.Hh;
					int item11 = item19.Dx;
					int item12 = item19.Dy;
					int item13 = item19.Rest.Item1;
					bool item14 = item19.Rest.Item2;
					bool item15 = item19.Rest.Item3;
					bool item16 = item19.Rest.Item4;
					if (reperes != null && item9 == tw && item10 == th)
					{
						NoterLesReperes(item6, item7, item8, item9, item10, item11, item12, reperes);
					}
					if (!dejaLues.TryGetValue(item6, out (byte[], int, int)? value3))
					{
						value3 = PixelsDuFichier(item6, maintenant);
						dejaLues[item6] = value3;
					}
					if (value3.HasValue)
					{
						(byte[], int, int) valueOrDefault = value3.GetValueOrDefault();
						(byte[], int, int, int, int, int, int, int, int) item17 = (valueOrDefault.Item1, valueOrDefault.Item2, valueOrDefault.Item3, item7, item8, (item9 < 0) ? valueOrDefault.Item2 : item9, (item10 < 0) ? valueOrDefault.Item3 : item10, item11, item12);
						if (item13 == 1 && item14 && item9 == tw && item10 == th)
						{
							posesObjets.Add(item17);
						}
						if (item13 < 2)
						{
							if (item9 == tw && item10 == th)
							{
								(int, int)? tuple2 = LignesDeChute(item6, item7 / tw, item8 / th);
								if (tuple2.HasValue)
								{
									(int, int) valueOrDefault2 = tuple2.GetValueOrDefault();
									posesDeChute.Add((poses.Count, valueOrDefault2.Item1, valueOrDefault2.Item2));
								}
							}
							poses.Add(item17);
							if (item13 == 0)
							{
								posesFond.Add(item17);
							}
						}
						else
						{
							posesHaut?.Add(item17);
							if (item9 == tw && item10 == th)
							{
								posesEauHaute.Add(item17);
							}
						}
						if (item14)
						{
							posesDebout?.Add(item17);
						}
						if (item15)
						{
							posesMasque?.Add(item17);
						}
						if (item16)
						{
							posesSol?.Add(item17);
						}
					}
				}
			}
			catch (Exception exception)
			{
				ApplicationContext.Context.Value?.Logger.LogWarning(exception, "Lecture des planches de la carte {Carte} impossible", carte.Id);
			}
			try
			{
				Construire(analyse, poses, chaleur, posesEauHaute, posesFond, posesDeChute, posesObjets);
			}
			catch (Exception exception2)
			{
				ApplicationContext.Context.Value?.Logger.LogWarning(exception2, "Analyse meteo de la carte {Carte} impossible", carte.Id);
				analyse.Eau = Array.Empty<byte>();
			}
			if (posesDebout != null && posesMasque != null && posesSol != null)
			{
				try
				{
					ConstruireOmbres(analyse, posesDebout, posesMasque, posesSol);
				}
				catch (Exception exception3)
				{
					ApplicationContext.Context.Value?.Logger.LogWarning(exception3, "Ombres portees de la carte {Carte} impossibles", carte.Id);
					analyse.RgbaOmbres = null;
				}
			}
			if (ville && posesHaut != null)
			{
				try
				{
					analyse.Ville = ConstruireVille(analyse, poses, posesHaut, reperes);
				}
				catch (Exception exception4)
				{
					ApplicationContext.Context.Value?.Logger.LogWarning(exception4, "Lecture de la ville {Carte} impossible", carte.Id);
				}
			}
			if (desert && posesHaut != null)
			{
				try
				{
					analyse.Desert = ConstruireDesert(analyse, poses, posesHaut);
				}
				catch (Exception exception5)
				{
					ApplicationContext.Context.Value?.Logger.LogWarning(exception5, "Lecture du desert {Carte} impossible", carte.Id);
				}
			}
			if (grotte && posesHaut != null && cases != null)
			{
				try
				{
					analyse.Grotte = ConstruireGrotte(analyse, poses, posesHaut, cases, tw, th);
					DonneesGrotte grotte2 = analyse.Grotte;
					ApplicationContext.Context.Value?.Logger.LogInformation("Grotte {Carte} : {Parois} bords de paroi, {Colonies} colonies de chauves-souris, {Gouttes} gouttieres, {Bassins} bassins, {Cailloux} cailloux, {Entrees} entrees", carte.Name, grotte2.Parois.Length, grotte2.Colonies.Length, grotte2.Gouttes.Length, grotte2.Bassins.Length, grotte2.Cailloux.Length, grotte2.Entrees.Length);
				}
				catch (Exception exception6)
				{
					ApplicationContext.Context.Value?.Logger.LogWarning(exception6, "Lecture de la grotte {Carte} impossible", carte.Id);
				}
			}
			AnalysesPretes.Enqueue(analyse);
		});
		bool Lisible(IGameTexture texture)
		{
			AtlasReference atlasReference = texture.AtlasReference;
			if ((object)atlasReference == null)
			{
				return true;
			}
			if (atlasReference.IsRotated)
			{
				return false;
			}
			if (!dejaLues.TryGetValue(texture, out (byte[], int, int)? value3))
			{
				value3 = PlancheParLaCarteGraphique(texture);
				dejaLues[texture] = value3;
			}
			return value3.HasValue;
		}
		(byte[] Rgba, int L, int H)? PlancheParLaCarteGraphique(IGameTexture texture)
		{
			lock (PixelsPlanches)
			{
				if (PixelsPlanches.TryGetValue(texture, out (byte[], int, int, long) value3))
				{
					Dictionary<IGameTexture, (byte[] Rgba, int L, int H, long Vu)> pixelsPlanches = PixelsPlanches;
					(byte[], int, int, long) value4 = value3;
					value4.Item4 = maintenant;
					pixelsPlanches[texture] = value4;
					return (value3.Item1, value3.Item2, value3.Item3);
				}
			}
			Texture2D texture2 = texture.GetTexture<Texture2D>();
			if (texture2 != null)
			{
				AtlasReference atlasReference = texture.AtlasReference;
				if ((object)atlasReference == null || !atlasReference.IsRotated)
				{
					AtlasReference atlasReference2 = texture.AtlasReference;
					Microsoft.Xna.Framework.Rectangle value5 = (((object)atlasReference2 != null) ? new Microsoft.Xna.Framework.Rectangle(atlasReference2.Bounds.X, atlasReference2.Bounds.Y, atlasReference2.Bounds.Width, atlasReference2.Bounds.Height) : new Microsoft.Xna.Framework.Rectangle(0, 0, texture2.Width, texture2.Height));
					Microsoft.Xna.Framework.Color[] array4 = new Microsoft.Xna.Framework.Color[value5.Width * value5.Height];
					texture2.GetData(0, value5, array4, 0, array4.Length);
					byte[] array5 = new byte[array4.Length * 4];
					for (int num7 = 0; num7 < array4.Length; num7++)
					{
						array5[num7 * 4] = array4[num7].R;
						array5[num7 * 4 + 1] = array4[num7].G;
						array5[num7 * 4 + 2] = array4[num7].B;
						array5[num7 * 4 + 3] = array4[num7].A;
					}
					lock (PixelsPlanches)
					{
						PixelsPlanches[texture] = (array5, value5.Width, value5.Height, maintenant);
					}
					return (array5, value5.Width, value5.Height);
				}
			}
			return null;
		}
	}

	private static void DireCeQuElleVoit(Analyse a, byte[] eau, int L, int H)
	{
		MapInstance carte = a.Carte;
		if (carte == null)
		{
			return;
		}
		lock (DejaDit)
		{
			if (!DejaDit.Add(carte.Id))
			{
				return;
			}
		}
		int tileWidth = Options.Instance.Map.TileWidth;
		int tileHeight = Options.Instance.Map.TileHeight;
		StringBuilder stringBuilder = new StringBuilder(H / tileHeight * (L / tileWidth + 2) + 64);
		for (int i = 0; i + tileHeight <= H; i += tileHeight)
		{
			stringBuilder.Append('\n');
			for (int j = 0; j + tileWidth <= L; j += tileWidth)
			{
				int num = 0;
				for (int k = i; k < i + tileHeight; k++)
				{
					for (int l = j; l < j + tileWidth; l++)
					{
						num += eau[k * L + l];
					}
				}
				float num2 = (float)num / (float)(tileWidth * tileHeight);
				stringBuilder.Append((num2 > 0.85f) ? '~' : ((num2 > 0.15f) ? 'o' : '.'));
			}
		}
		ApplicationContext.Context.Value?.Logger.LogInformation("Analyse de l'eau : {Carte} ({Largeur}x{Hauteur} px){Dessin}", carte.Name, L, H, stringBuilder.ToString());
	}

	private static void Construire(Analyse a, List<(byte[] Rgba, int PL, int PH, int Sx, int Sy, int W, int Hh, int Dx, int Dy)> poses, bool chaleur, List<(byte[] Rgba, int PL, int PH, int Sx, int Sy, int W, int Hh, int Dx, int Dy)>? hautes = null, List<(byte[] Rgba, int PL, int PH, int Sx, int Sy, int W, int Hh, int Dx, int Dy)>? fonds = null, List<(int Pose, int De, int A)>? chutes = null, List<(byte[] Rgba, int PL, int PH, int Sx, int Sy, int W, int Hh, int Dx, int Dy)>? objets = null)
	{
		int L = a.L;
		int H = a.H;
		int num = L * H;
		byte[] px = new byte[num * 4];
		Poser(poses);
		int TX = (L + 32 - 1) / 32;
		int TY = (H + 32 - 1) / 32;
		float[] part = new float[TX * TY];
		Parts();
		if (hautes != null && hautes.Count > 0)
		{
			List<(byte[], int, int, int, int, int, int, int, int)> list = new List<(byte[], int, int, int, int, int, int, int, int)>();
			foreach (var haute in hautes)
			{
				int num2 = 0;
				int num3 = 0;
				for (int i = 0; i < haute.Hh; i++)
				{
					for (int j = 0; j < haute.W; j++)
					{
						int num4 = haute.Sx + j;
						int num5 = haute.Sy + i;
						if (num4 < 0 || num5 < 0 || num4 >= haute.PL || num5 >= haute.PH)
						{
							continue;
						}
						int num6 = (num5 * haute.PL + num4) * 4;
						if (haute.Rgba[num6 + 3] >= 128)
						{
							num3++;
							int num7 = haute.Rgba[num6];
							int num8 = haute.Rgba[num6 + 1];
							int num9 = haute.Rgba[num6 + 2];
							if (num9 > 140 && num9 - num7 >= 60 && num9 >= num8 - 10 && num8 + 5 >= num7)
							{
								num2++;
							}
						}
					}
				}
				if (num3 > 0 && num2 * 5 >= haute.W * haute.Hh)
				{
					list.Add(haute);
				}
			}
			bool flag = true;
			for (int k = 0; k < 6 && flag; k++)
			{
				if (list.Count <= 0)
				{
					break;
				}
				flag = false;
				List<(byte[], int, int, int, int, int, int, int, int)> list2 = new List<(byte[], int, int, int, int, int, int, int, int)>();
				List<(byte[], int, int, int, int, int, int, int, int)> list3 = new List<(byte[], int, int, int, int, int, int, int, int)>();
				foreach (var item28 in list)
				{
					int num10 = item28.Rest.Item1 / 32;
					int num11 = item28.Rest.Item2 / 32;
					bool flag2 = false;
					for (int l = -1; l <= 1; l++)
					{
						if (flag2)
						{
							break;
						}
						for (int m = -1; m <= 1; m++)
						{
							int num12 = num10 + m;
							int num13 = num11 + l;
							if (num12 >= 0 && num13 >= 0 && num12 < TX && num13 < TY && part[num13 * TX + num12] > 0.2f)
							{
								flag2 = true;
								break;
							}
						}
					}
					(flag2 ? list3 : list2).Add(item28);
				}
				if (list3.Count > 0)
				{
					Poser(list3);
					Parts();
					flag = true;
				}
				list = list2;
			}
		}
		byte[] array = new byte[num];
		byte[] array2 = new byte[num];
		for (int n = 0; n < H; n++)
		{
			for (int num14 = 0; num14 < L; num14++)
			{
				int num15 = n * L + num14;
				int num16 = num14 / 32;
				int num17 = n / 32;
				int num18 = num15 * 4;
				if (Pleine(num16, num17))
				{
					array[num15] = (byte)((Bleu(num15) || Blanc(num15) || px[num18 + 2] > px[num18] + 10) ? 1u : 0u);
				}
				else if (part[num17 * TX + num16] > 0.02f)
				{
					bool flag3 = false;
					for (int num19 = -1; num19 <= 1; num19++)
					{
						if (flag3)
						{
							break;
						}
						for (int num20 = -1; num20 <= 1; num20++)
						{
							if (Pleine(num16 + num20, num17 + num19))
							{
								flag3 = true;
								break;
							}
						}
					}
					if (flag3 && Bleu(num15))
					{
						array[num15] = 1;
					}
				}
				int num21 = px[num18];
				int num22 = px[num18 + 1];
				int num23 = px[num18 + 2];
				if (array[num15] == 0 && num21 > 185 && num22 > 150 && num23 < 205 && num21 > num23 + 30 && num22 <= num21)
				{
					array2[num15] = 1;
				}
			}
		}
		for (int num24 = 1; num24 < H - 1; num24++)
		{
			for (int num25 = 1; num25 < L - 1; num25++)
			{
				int num26 = num24 * L + num25;
				if (array[num26] == 0 && Blanc(num26) && (array[num26 - 1] == 1 || array[num26 + 1] == 1 || array[num26 - L] == 1 || array[num26 + L] == 1))
				{
					array[num26] = 2;
				}
			}
		}
		for (int num27 = 0; num27 < num; num27++)
		{
			if (array[num27] == 2)
			{
				array[num27] = 1;
				array2[num27] = 0;
			}
		}
		if (objets != null && objets.Count > 0)
		{
			foreach (var objet in objets)
			{
				byte[] item = objet.Rgba;
				int item2 = objet.PL;
				int item3 = objet.PH;
				int item4 = objet.Sx;
				int item5 = objet.Sy;
				int item6 = objet.W;
				int item7 = objet.Hh;
				int item8 = objet.Rest.Item1;
				int item9 = objet.Rest.Item2;
				for (int num28 = 0; num28 < item7; num28++)
				{
					int num29 = item9 + num28;
					int num30 = item5 + num28;
					if (num29 < 0 || num29 >= H || num30 < 0 || num30 >= item3)
					{
						continue;
					}
					for (int num31 = 0; num31 < item6; num31++)
					{
						int num32 = item8 + num31;
						int num33 = item4 + num31;
						if (num32 >= 0 && num32 < L && num33 >= 0 && num33 < item2 && item[(num30 * item2 + num33) * 4 + 3] >= 128)
						{
							array2[num29 * L + num32] = 0;
						}
					}
				}
			}
		}
		bool[] array3 = ((chutes != null && chutes.Count > 0) ? Cascades.Masque(poses, chutes, L, H) : null);
		if (array3 != null)
		{
			bool[] array4 = new bool[num];
			for (int num34 = 0; num34 < num; num34++)
			{
				if (array3[num34] && array[num34] != 0)
				{
					array[num34] = 0;
					array4[num34] = true;
				}
			}
			a.EauDeChute = array4;
		}
		RetirerFaussesEaux(px, array, L, H);
		if (fonds != null)
		{
			bool[] array5 = new bool[num];
			foreach (var fond in fonds)
			{
				byte[] item10 = fond.Rgba;
				int item11 = fond.PL;
				int item12 = fond.PH;
				int item13 = fond.Sx;
				int item14 = fond.Sy;
				int item15 = fond.W;
				int item16 = fond.Hh;
				int item17 = fond.Rest.Item1;
				int item18 = fond.Rest.Item2;
				for (int num35 = 0; num35 < item16; num35++)
				{
					int num36 = item18 + num35;
					int num37 = item14 + num35;
					if (num36 < 0 || num36 >= H || num37 < 0 || num37 >= item12)
					{
						continue;
					}
					for (int num38 = 0; num38 < item15; num38++)
					{
						int num39 = item17 + num38;
						int num40 = item13 + num38;
						if (num39 >= 0 && num39 < L && num40 >= 0 && num40 < item11)
						{
							int num41 = (num37 * item11 + num40) * 4;
							if (item10[num41 + 3] >= 128)
							{
								int num42 = item10[num41];
								int num43 = item10[num41 + 1];
								int num44 = item10[num41 + 2];
								array5[num36 * L + num39] = num44 > 140 && num44 - num42 >= 60 && num44 >= num43 - 10 && num43 + 5 >= num42;
							}
						}
					}
				}
			}
			bool[] array6 = new bool[num];
			int[] array7 = new int[num];
			for (int num45 = 0; num45 < num; num45++)
			{
				if (array[num45] == 0 || array6[num45])
				{
					continue;
				}
				int num46 = 0;
				int num47 = 0;
				bool flag4 = false;
				array7[num47++] = num45;
				array6[num45] = true;
				while (num46 < num47)
				{
					int num48 = array7[num46++];
					flag4 |= array5[num48];
					int num49 = num48 % L;
					if (num49 > 0 && array[num48 - 1] != 0 && !array6[num48 - 1])
					{
						array6[num48 - 1] = true;
						array7[num47++] = num48 - 1;
					}
					if (num49 < L - 1 && array[num48 + 1] != 0 && !array6[num48 + 1])
					{
						array6[num48 + 1] = true;
						array7[num47++] = num48 + 1;
					}
					if (num48 >= L && array[num48 - L] != 0 && !array6[num48 - L])
					{
						array6[num48 - L] = true;
						array7[num47++] = num48 - L;
					}
					if (num48 < num - L && array[num48 + L] != 0 && !array6[num48 + L])
					{
						array6[num48 + L] = true;
						array7[num47++] = num48 + L;
					}
				}
				if (!flag4 && num47 < 12288)
				{
					for (int num50 = 0; num50 < num47; num50++)
					{
						array[array7[num50]] = 0;
					}
				}
			}
		}
		a.Sable = MasqueSable(px, array, array2, L, H, chaleur);
		ConstruireHerbe(a, px, array, L, H);
		int num51 = 0;
		for (int num52 = 0; num52 < num; num52++)
		{
			num51 += array[num52];
		}
		byte[] dist = new byte[num];
		if (num51 > 0)
		{
			Array.Fill(dist, byte.MaxValue);
			int[] file = new int[num];
			int num53 = 0;
			int queue = 0;
			for (int num54 = 0; num54 < num; num54++)
			{
				if (array[num54] == 0)
				{
					dist[num54] = 0;
					file[queue++] = num54;
				}
			}
			while (num53 < queue)
			{
				int num55 = file[num53++];
				int num56 = num55 % L;
				int v = dist[num55];
				if (v < 40)
				{
					if (num56 > 0)
					{
						Voisin(num55 - 1);
					}
					if (num56 < L - 1)
					{
						Voisin(num55 + 1);
					}
					if (num55 >= L)
					{
						Voisin(num55 - L);
					}
					if (num55 < num - L)
					{
						Voisin(num55 + L);
					}
				}
				void Voisin(int num107)
				{
					if (dist[num107] > v + 1)
					{
						dist[num107] = (byte)(v + 1);
						file[queue++] = num107;
					}
				}
			}
			for (int num57 = 0; num57 < num; num57++)
			{
				if (dist[num57] == byte.MaxValue)
				{
					dist[num57] = 40;
				}
			}
		}
		Random random = new Random(a.Carte.Id.GetHashCode());
		List<(short, short, sbyte, sbyte, float)> list4 = new List<(short, short, sbyte, sbyte, float)>();
		for (int num58 = 1; num58 < H - 1; num58++)
		{
			for (int num59 = 1; num59 < L - 1; num59++)
			{
				int num60 = num58 * L + num59;
				if (array[num60] != 0)
				{
					int num61 = 0;
					int num62 = 0;
					if (array[num60 - 1] == 0)
					{
						num61++;
					}
					if (array[num60 + 1] == 0)
					{
						num61--;
					}
					if (array[num60 - L] == 0)
					{
						num62++;
					}
					if (array[num60 + L] == 0)
					{
						num62--;
					}
					if (num61 != 0 || num62 != 0)
					{
						list4.Add(((short)num59, (short)num58, (sbyte)Math.Sign(num61), (sbyte)Math.Sign(num62), (float)(random.NextDouble() * Math.PI * 2.0)));
					}
				}
			}
		}
		List<Flaque> list5 = new List<Flaque>();
		byte[] array8 = new byte[num];
		for (int num63 = 0; num63 < 6000; num63++)
		{
			if (list5.Count >= 12)
			{
				break;
			}
			int num64 = 5 + random.Next(7);
			int num65 = 2 + random.Next(3);
			int cx = num64 + 4 + random.Next(Math.Max(1, L - 2 * num64 - 8));
			int cy = num65 + 4 + random.Next(Math.Max(1, H - 2 * num65 - 8));
			int num66 = 0;
			int num67 = 0;
			bool flag5 = false;
			for (int num68 = cy - num65 - 2; num68 <= cy + num65 + 2; num68++)
			{
				for (int num69 = cx - num64 - 3; num69 <= cx + num64 + 3; num69++)
				{
					num67++;
					if (num69 >= 0 && num68 >= 0 && num69 < L && num68 < H)
					{
						num66 += array2[num68 * L + num69];
						flag5 |= array[num68 * L + num69] == 1;
					}
				}
			}
			if (flag5 || (float)num66 < (float)num67 * 0.9f || list5.Any((Flaque f) => Math.Sqrt((double)((f.Cx - cx) * (f.Cx - cx)) + (double)((f.Cy - cy) * (f.Cy - cy)) * 2.56) < 44.0))
			{
				continue;
			}
			List<(sbyte, sbyte, float)> list6 = new List<(sbyte, sbyte, float)>();
			for (int num70 = -num65 - 1; num70 <= num65 + 1; num70++)
			{
				for (int num71 = -num64 - 2; num71 <= num64 + 2; num71++)
				{
					float num72 = (float)num71 / (float)num64 * ((float)num71 / (float)num64) + (float)num70 / (float)num65 * ((float)num70 / (float)num65) + ((float)random.NextDouble() - 0.5f) * 0.35f;
					int num73 = cx + num71;
					int num74 = cy + num70;
					if (num72 < 1.15f && num73 >= 0 && num74 >= 0 && num73 < L && num74 < H && array2[num74 * L + num73] == 1)
					{
						list6.Add(((sbyte)num71, (sbyte)num70, num72));
						array8[num74 * L + num73] = (byte)Math.Max(1.0, Math.Round(num72 * 100f));
					}
				}
			}
			list5.Add(new Flaque
			{
				Cx = cx,
				Cy = cy,
				Pixels = list6.ToArray()
			});
		}
		List<(short, short, float, float, bool)> list7 = new List<(short, short, float, float, bool)>();
		List<int> list8 = new List<int>();
		List<int> list9 = new List<int>();
		List<int> list10 = new List<int>();
		for (int num75 = 0; num75 < 60000; num75++)
		{
			if ((list8.Count >= 1500 || num51 <= 0) && list9.Count >= 1500 && list10.Count >= 600)
			{
				break;
			}
			int num76 = random.Next(L);
			int num77 = random.Next(H);
			int num78 = num77 * L + num76;
			if (array[num78] == 1)
			{
				if (list8.Count < 1500)
				{
					list8.Add(num78);
				}
				if (list7.Count < 140 && dist[num78] > 5)
				{
					list7.Add(((short)num76, (short)num77, (float)(random.NextDouble() * Math.PI * 2.0), 0.6f + (float)random.NextDouble() * 1.4f, random.NextDouble() < 0.15));
				}
			}
			else if (array3 == null || !array3[num78])
			{
				if (list9.Count < 1500)
				{
					list9.Add(num78);
				}
				int num79 = px[num78 * 4];
				int num80 = px[num78 * 4 + 1];
				int num81 = px[num78 * 4 + 2];
				if (list10.Count < 600 && num80 > num79 + 8 && num80 > num81 + 20)
				{
					list10.Add(num78);
				}
			}
		}
		double num82 = 0.0;
		double num83 = 0.0;
		int num84 = 0;
		for (int num85 = 0; num85 < num; num85++)
		{
			if (array[num85] != 1 && (array3 == null || !array3[num85]))
			{
				double num86 = ((double)(int)px[num85 * 4] * 0.3 + (double)(int)px[num85 * 4 + 1] * 0.55 + (double)(int)px[num85 * 4 + 2] * 0.15) / 255.0;
				num82 += num86;
				num83 += num86 * num86;
				num84++;
			}
		}
		double num87 = num82 / (double)Math.Max(1, num84);
		double num88 = Math.Sqrt(Math.Max(0.0001, num83 / (double)Math.Max(1, num84) - num87 * num87));
		float[] array9 = GrainNeige();
		byte[] array10 = new byte[num];
		byte[] array11 = new byte[num];
		for (int num89 = 0; num89 < H; num89++)
		{
			for (int num90 = 0; num90 < L; num90++)
			{
				int num91 = num89 * L + num90;
				double num92 = ((double)(int)px[num91 * 4] * 0.3 + (double)(int)px[num91 * 4 + 1] * 0.55 + (double)(int)px[num91 * 4 + 2] * 0.15) / 255.0;
				double num93 = Math.Clamp((num92 - num87) / (3.0 * num88) + 0.5, 0.0, 1.0);
				array11[num91] = (byte)(num93 * 255.0);
				array10[num91] = ((array[num91] == 1 || (array3 != null && array3[num91])) ? byte.MaxValue : ((byte)Math.Min(254.0, (0.5 * (1.0 - num93) + 0.5 * (double)array9[(num89 & 0xFF) * 256 + (num90 & 0xFF)]) * 254.0)));
			}
		}
		a.Eau = array;
		a.Dist = dist;
		a.Seuil = array10;
		a.Relief = array11;
		a.FlaqueMap = array8;
		a.Rive = list4.ToArray();
		a.Flaques = list5.ToArray();
		a.Etoiles = list7.ToArray();
		a.PointsEau = list8.ToArray();
		a.PointsSol = list9.ToArray();
		a.PointsHerbe = list10.ToArray();
		a.AEau = num51 > 0;
		DireCeQuElleVoit(a, array, L, H);
		if (a.AEau)
		{
			byte[] array12 = new byte[num * 4];
			byte[] array13 = new byte[num * 4];
			for (int num94 = 0; num94 < num; num94++)
			{
				byte[] array14 = ((array[num94] == 1) ? array12 : array13);
				int num95 = num94 * 4;
				int num96 = num94 * 4 + 1;
				int num97 = num94 * 4 + 2;
				byte b;
				array14[num94 * 4 + 3] = (b = byte.MaxValue);
				array14[num95] = (array14[num96] = (array14[num97] = b));
			}
			a.RgbaEau = array12;
			a.RgbaSol = array13;
			int num98 = L / 2;
			int num99 = H / 2;
			float[] array15 = new float[num98 * num99];
			for (int num100 = 0; num100 < num99; num100++)
			{
				for (int num101 = 0; num101 < num98; num101++)
				{
					array15[num100 * num98 + num101] = (float)(array[num100 * 2 * L + num101 * 2] + array[num100 * 2 * L + Math.Min(L - 1, num101 * 2 + 1)] + array[Math.Min(H - 1, num100 * 2 + 1) * L + num101 * 2]) / 3f;
				}
			}
			for (int num102 = 0; num102 < 3; num102++)
			{
				array15 = Flou(array15, num98, num99, 3);
			}
			byte[] array16 = new byte[num98 * num99 * 4];
			for (int num103 = 0; num103 < array15.Length; num103++)
			{
				int num104 = num103 * 4;
				int num105 = num103 * 4 + 1;
				byte b;
				array16[num103 * 4 + 2] = (b = byte.MaxValue);
				array16[num104] = (array16[num105] = b);
				array16[num103 * 4 + 3] = (byte)(Math.Clamp(array15[num103], 0f, 1f) * 255f);
			}
			a.RgbaFlou = array16;
			ConstruireEau(a, px, array, dist, array2, random);
		}
		if (!chaleur)
		{
			return;
		}
		for (int num106 = 0; num106 < num; num106++)
		{
			if (array[num106] == 1)
			{
				px[num106 * 4 + 3] = 0;
			}
		}
		a.RgbaTerre = px;
		bool Blanc(int num107)
		{
			if (px[num107 * 4] > 205 && px[num107 * 4 + 1] > 215)
			{
				return px[num107 * 4 + 2] > 225;
			}
			return false;
		}
		bool Bleu(int num107)
		{
			if (px[num107 * 4 + 2] > 140 && px[num107 * 4 + 2] > px[num107 * 4] + 25)
			{
				return px[num107 * 4 + 2] >= px[num107 * 4 + 1] - 10;
			}
			return false;
		}
		bool BleuFranc(int num107)
		{
			if (Bleu(num107) && px[num107 * 4 + 2] - px[num107 * 4] >= 60)
			{
				return px[num107 * 4 + 1] + 5 >= px[num107 * 4];
			}
			return false;
		}
		void Parts()
		{
			for (int num107 = 0; num107 < TY; num107++)
			{
				for (int num108 = 0; num108 < TX; num108++)
				{
					int num109 = 0;
					int num110 = 0;
					for (int num111 = num107 * 32; num111 < Math.Min(H, num107 * 32 + 32); num111++)
					{
						for (int num112 = num108 * 32; num112 < Math.Min(L, num108 * 32 + 32); num112++)
						{
							num110++;
							if (BleuFranc(num111 * L + num112))
							{
								num109++;
							}
						}
					}
					part[num107 * TX + num108] = ((num110 == 0) ? 0f : ((float)num109 / (float)num110));
				}
			}
		}
		bool Pleine(int tx, int ty)
		{
			if (tx >= 0 && ty >= 0 && tx < TX && ty < TY)
			{
				return part[ty * TX + tx] > 0.6f;
			}
			return false;
		}
		void Poser(IEnumerable<(byte[] Rgba, int PL, int PH, int Sx, int Sy, int W, int Hh, int Dx, int Dy)> lot)
		{
			foreach (var item29 in lot)
			{
				byte[] item19 = item29.Rgba;
				int item20 = item29.PL;
				int item21 = item29.PH;
				int item22 = item29.Sx;
				int item23 = item29.Sy;
				int item24 = item29.W;
				int item25 = item29.Hh;
				int item26 = item29.Rest.Item1;
				int item27 = item29.Rest.Item2;
				for (int num107 = 0; num107 < item25; num107++)
				{
					int num108 = item27 + num107;
					int num109 = item23 + num107;
					if (num108 >= 0 && num108 < H && num109 >= 0 && num109 < item21)
					{
						for (int num110 = 0; num110 < item24; num110++)
						{
							int num111 = item26 + num110;
							int num112 = item22 + num110;
							if (num111 >= 0 && num111 < L && num112 >= 0 && num112 < item20)
							{
								int num113 = (num109 * item20 + num112) * 4;
								byte b2 = item19[num113 + 3];
								if (b2 != 0)
								{
									int num114 = (num108 * L + num111) * 4;
									if (b2 == byte.MaxValue)
									{
										px[num114] = item19[num113];
										px[num114 + 1] = item19[num113 + 1];
										px[num114 + 2] = item19[num113 + 2];
										px[num114 + 3] = byte.MaxValue;
									}
									else
									{
										float num115 = (float)(int)b2 / 255f;
										px[num114] = (byte)((float)(int)item19[num113] * num115 + (float)(int)px[num114] * (1f - num115));
										px[num114 + 1] = (byte)((float)(int)item19[num113 + 1] * num115 + (float)(int)px[num114 + 1] * (1f - num115));
										px[num114 + 2] = (byte)((float)(int)item19[num113 + 2] * num115 + (float)(int)px[num114 + 2] * (1f - num115));
										px[num114 + 3] = Math.Max(px[num114 + 3], b2);
									}
								}
							}
						}
					}
				}
			}
		}
	}

	private static float[] Flou(float[] source, int l, int h, int rayon)
	{
		float[] array = new float[source.Length];
		float[] array2 = new float[source.Length];
		for (int i = 0; i < h; i++)
		{
			float num = 0f;
			int num2 = 0;
			for (int j = -rayon; j < l; j++)
			{
				int num3 = j + rayon;
				if (num3 < l)
				{
					num += source[i * l + num3];
					num2++;
				}
				int num4 = j - rayon - 1;
				if (num4 >= 0)
				{
					num -= source[i * l + num4];
					num2--;
				}
				if (j >= 0)
				{
					array[i * l + j] = num / (float)Math.Max(1, num2);
				}
			}
		}
		for (int k = 0; k < l; k++)
		{
			float num5 = 0f;
			int num6 = 0;
			for (int m = -rayon; m < h; m++)
			{
				int num7 = m + rayon;
				if (num7 < h)
				{
					num5 += array[num7 * l + k];
					num6++;
				}
				int num8 = m - rayon - 1;
				if (num8 >= 0)
				{
					num5 -= array[num8 * l + k];
					num6--;
				}
				if (m >= 0)
				{
					array2[m * l + k] = num5 / (float)Math.Max(1, num6);
				}
			}
		}
		return array2;
	}

	private static bool FinaliserUnPas(Analyse a)
	{
		if (!(Graphics.Renderer is MonoRenderer monoRenderer))
		{
			return true;
		}
		string text = $"meteo/{a.Carte.Id}";
		if (a.RgbaEau != null)
		{
			a.TexEau = monoRenderer.CreerTexturePixels(text + "/eau", a.L, a.H, a.RgbaEau);
			a.RgbaEau = null;
			return false;
		}
		if (a.RgbaSol != null)
		{
			a.TexSol = monoRenderer.CreerTexturePixels(text + "/sol", a.L, a.H, a.RgbaSol);
			a.RgbaSol = null;
			return false;
		}
		if (a.RgbaFlou != null)
		{
			a.TexEauFlou = monoRenderer.CreerTexturePixels(text + "/flou", a.L / 2, a.H / 2, a.RgbaFlou);
			a.RgbaFlou = null;
			return false;
		}
		if (a.RgbaTerre != null)
		{
			a.TexTerre = monoRenderer.CreerTexturePixels(text + "/terre", a.L, a.H, a.RgbaTerre);
			a.RgbaTerre = null;
			return false;
		}
		if (a.RgbaProfondeur != null)
		{
			a.TexProfondeur = monoRenderer.CreerTexturePixels(text + "/profondeur", a.L, a.H, a.RgbaProfondeur);
			a.RgbaProfondeur = null;
			return false;
		}
		if (a.RgbaReflet != null)
		{
			a.TexReflet = monoRenderer.CreerTexturePixels(text + "/reflet", a.L, a.H, a.RgbaReflet);
			a.RgbaReflet = null;
			return false;
		}
		if (a.RgbaHerbeClaire != null)
		{
			a.TexHerbeClaire = monoRenderer.CreerTexturePixels(text + "/herbe", a.L, a.H, a.RgbaHerbeClaire);
			a.RgbaHerbeClaire = null;
			return false;
		}
		if (a.RgbaOmbres != null)
		{
			a.TexOmbres = monoRenderer.CreerTexturePixels(text + "/ombres", a.OmbresL, a.OmbresH, a.RgbaOmbres);
			a.RgbaOmbres = null;
			return false;
		}
		if (a.RgbaSolLibre != null)
		{
			a.TexSolLibre = monoRenderer.CreerTexturePixels(text + "/sol-libre", a.OmbresL, a.OmbresH, a.RgbaSolLibre);
			a.RgbaSolLibre = null;
			return false;
		}
		DonneesVille ville = a.Ville;
		if (ville != null)
		{
			if (ville.RgbaVitres != null)
			{
				ville.TexVitres = monoRenderer.CreerTexturePixels(text + "/vitres", a.L, a.H, ville.RgbaVitres);
				ville.RgbaVitres = null;
				return false;
			}
			if (ville.RgbaEnseignes != null)
			{
				ville.TexEnseignes = monoRenderer.CreerTexturePixels(text + "/enseignes", a.L, a.H, ville.RgbaEnseignes);
				ville.RgbaEnseignes = null;
				return false;
			}
		}
		return true;
	}

	internal static IGameTexture? NeigeDe(Analyse a)
	{
		float niveau = MathF.Round(_enneigement * 40f) / 40f;
		if (a.RgbaNeige != null && !a.NeigeEnCours)
		{
			if (Graphics.Renderer is MonoRenderer monoRenderer)
			{
				if (a.TexNeige == null)
				{
					a.TexNeige = monoRenderer.CreerTexturePixels($"meteo/{a.Carte.Id}/neige", a.L, a.H, a.RgbaNeige);
				}
				else
				{
					MonoRenderer.RemplacerPixels(a.TexNeige, a.RgbaNeige);
				}
			}
			a.NeigeNiveau = a.NeigeNiveauPret;
			a.RgbaNeige = null;
		}
		if (niveau != a.NeigeNiveau && !a.NeigeEnCours && a.RgbaNeige == null && a.Seuil.Length == a.L * a.H)
		{
			a.NeigeEnCours = true;
			Task.Run(delegate
			{
				try
				{
					byte[] array = new byte[a.L * a.H * 4];
					float num = (0.12f + niveau * 0.8f) * 254f;
					float num2 = 12.7f;
					for (int i = 0; i < a.H; i++)
					{
						for (int j = 0; j < a.L; j++)
						{
							int num3 = i * a.L + j;
							byte b = a.Seuil[num3];
							if (!((float)(int)b >= num))
							{
								bool flag = (float)(int)b > num - num2;
								if (!flag || ((j + i) & 1) != 1)
								{
									float num4 = (float)(int)a.Relief[num3] / 255f;
									array[num3 * 4] = (byte)(128f + 124f * num4);
									array[num3 * 4 + 1] = (byte)(146f + 106f * num4);
									array[num3 * 4 + 2] = (byte)(188f + 67f * num4);
									array[num3 * 4 + 3] = (byte)(flag ? 120u : 218u);
								}
							}
						}
					}
					a.NeigeNiveauPret = niveau;
					a.RgbaNeige = array;
				}
				finally
				{
					a.NeigeEnCours = false;
				}
			});
		}
		if (!(a.NeigeNiveau > 0.01f))
		{
			return null;
		}
		return a.TexNeige;
	}

	private static float[] GrainNeige()
	{
		return _grain ?? (_grain = Bruit(256, 12, 3, 91));
	}

	internal static float[] Bruit(int taille, int cellules, int octaves, int graine)
	{
		Random random = new Random(graine);
		float[] array = new float[taille * taille];
		float num = 1f;
		float num2 = 0f;
		int num3 = cellules;
		for (int i = 0; i < octaves; i++)
		{
			float[] array2 = new float[num3 * num3];
			for (int j = 0; j < array2.Length; j++)
			{
				array2[j] = (float)random.NextDouble();
			}
			for (int k = 0; k < taille; k++)
			{
				float num4 = (float)k / (float)taille * (float)num3;
				int num5 = (int)num4;
				float num6 = num4 - (float)num5;
				float num7 = num6 * num6 * (3f - 2f * num6);
				int num8 = (num5 + 1) % num3;
				for (int l = 0; l < taille; l++)
				{
					float num9 = (float)l / (float)taille * (float)num3;
					int num10 = (int)num9;
					float num11 = num9 - (float)num10;
					float num12 = num11 * num11 * (3f - 2f * num11);
					int num13 = (num10 + 1) % num3;
					float num14 = array2[num5 * num3 + num10] + (array2[num5 * num3 + num13] - array2[num5 * num3 + num10]) * num12;
					float num15 = array2[num8 * num3 + num10] + (array2[num8 * num3 + num13] - array2[num8 * num3 + num10]) * num12;
					array[k * taille + l] += (num14 + (num15 - num14) * num7) * num;
				}
			}
			num2 += num;
			num *= 0.5f;
			num3 *= 2;
		}
		for (int m = 0; m < array.Length; m++)
		{
			array[m] /= num2;
		}
		return array;
	}

	private static void MenageAnalyses()
	{
		if (Analyses.Count <= 12 || !(Globals.Me?.MapInstance is MapInstance mapInstance))
		{
			return;
		}
		KeyValuePair<Guid, Analyse>[] array = Analyses.ToArray();
		for (int i = 0; i < array.Length; i++)
		{
			KeyValuePair<Guid, Analyse> keyValuePair = array[i];
			var (guid2, analyse2) = keyValuePair;
			if (Math.Abs(analyse2.Carte.GridX - mapInstance.GridX) > 2 || Math.Abs(analyse2.Carte.GridY - mapInstance.GridY) > 2 || !MapInstance.TryGet(guid2, out MapInstance instance) || instance != analyse2.Carte)
			{
				analyse2.Liberer();
				Analyses.Remove(guid2);
			}
		}
	}

	public static bool EstAuDela(MapInstance carte)
	{
		return EtatAuDela(carte)?.AuDela ?? false;
	}

	private static void RelireLeRoyaume()
	{
		long milliseconds = Timing.Global.Milliseconds;
		if (milliseconds < _verificationAuDela)
		{
			return;
		}
		_verificationAuDela = milliseconds + 5000;
		try
		{
			string chemin = Path.Combine(ClientConfiguration.ResourcesDirectory, "au_dela.json");
			DateTime dateTime = (Coffre.Existe(chemin) ? Coffre.DateUtc(chemin) : default(DateTime));
			if (dateTime == _dateAuDela)
			{
				return;
			}
			_dateAuDela = dateTime;
			CartesDuRoyaume.Clear();
			if (dateTime != default(DateTime))
			{
				foreach (JToken item in JArray.Parse(Coffre.LireTexte(chemin)))
				{
					if (Guid.TryParse((string?)item, out var result))
					{
						CartesDuRoyaume.Add(result);
					}
				}
			}
			CartesAuDela.Clear();
		}
		catch (Exception exception)
		{
			ApplicationContext.Context.Value?.Logger.LogWarning(exception, "Liste des cartes du Royaume des morts illisible");
		}
	}

	private static bool EstPlanche(string? nom, string planche)
	{
		return nom?.EndsWith(planche, StringComparison.OrdinalIgnoreCase) ?? false;
	}

	private static bool TuileDeNuage(string? planche, int x, int y)
	{
		if (x >= 11 && x <= 13 && y >= 4 && y <= 11)
		{
			return EstPlanche(planche, "tileset7.png");
		}
		return false;
	}

	private static bool TuileDuSerpent(string? planche, int x, int y)
	{
		if (EstPlanche(planche, "route_serpent.png"))
		{
			return true;
		}
		if (EstPlanche(planche, "tileset7.png"))
		{
			if ((y < 2 || y > 11 || x > 9) && (y < 19 || y > 27 || x > 8))
			{
				if (y >= 175 && y <= 183)
				{
					return x <= 11;
				}
				return false;
			}
			return true;
		}
		return false;
	}

	private static CarteAuDela? EtatAuDela(MapInstance carte)
	{
		RelireLeRoyaume();
		if (CartesAuDela.TryGetValue(carte.Id, out CarteAuDela value) && value.Carte == carte)
		{
			return value;
		}
		if (!carte.IsLoaded)
		{
			return null;
		}
		int num = 0;
		int num2 = 0;
		bool flag = false;
		ulong num3 = 1469598103934665603uL;
		Guid guid = Guid.Empty;
		string text = null;
		List<string> all = Options.Instance.Map.Layers.All;
		for (int i = 0; i < all.Count; i++)
		{
			if (!carte.Layers.TryGetValue(all[i], out var value2))
			{
				continue;
			}
			int length = value2.GetLength(0);
			int length2 = value2.GetLength(1);
			for (int j = 0; j < length; j++)
			{
				for (int k = 0; k < length2; k++)
				{
					Tile tile = value2[j, k];
					if (!(tile.TilesetId == Guid.Empty) && tile.X >= 0 && tile.Y >= 0)
					{
						if (tile.TilesetId != guid)
						{
							guid = tile.TilesetId;
							text = (DatabaseObject<TilesetDescriptor>.TryGet(guid, out var databaseObject) ? databaseObject.Name : null);
							flag = EstPlanche(text, "tileset7.png") || EstPlanche(text, "route_serpent.png");
						}
						if (flag)
						{
							num2++;
						}
						if (TuileDeNuage(text, tile.X, tile.Y))
						{
							num++;
						}
						else if (tile.Autotile == 0 && TuileDuSerpent(text, tile.X, tile.Y))
						{
							ulong num4 = (uint)HashCode.Combine(i, j, k, tile.X, tile.Y, text.Length);
							num3 = (num3 ^ num4) * 1099511628211L;
						}
					}
				}
			}
		}
		bool auDela = ((CartesDuRoyaume.Count > 0) ? CartesDuRoyaume.Contains(carte.Id) : (num2 >= 16));
		value = new CarteAuDela
		{
			Carte = carte,
			Mer = (num >= 64),
			AuDela = auDela,
			Signature = num3
		};
		CartesAuDela[carte.Id] = value;
		return value;
	}

	internal static bool AuDelaRemplace(MapInstance carte, IGameTexture planche, int x, int y)
	{
		if (!_auDelaEnPanne && TuileDeNuage(planche.Name, x, y))
		{
			return EtatAuDela(carte)?.Mer ?? false;
		}
		return false;
	}

	private static void PanneAuDela(Exception exception)
	{
		if (!_auDelaEnPanne)
		{
			_auDelaEnPanne = true;
			VersionAuDela++;
			ApplicationContext.Context.Value?.Logger.LogWarning(exception, "Mer de nuages de l'au-dela coupee");
		}
	}

	private static double HasardAuDela(double n)
	{
		double num = Math.Sin(n * 127.1 + 311.7) * 43758.5453;
		return num - Math.Floor(num);
	}

	private static double Hasard2(int a, int b, int graine)
	{
		double num = Math.Sin((double)a * 127.1 + (double)b * 311.7 + (double)graine * 74.7) * 43758.5453;
		return num - Math.Floor(num);
	}

	private static double Domes(double x, double y, int taille, int pl, int ph, int graine)
	{
		int num = (int)Math.Floor(x / (double)taille);
		int num2 = (int)Math.Floor(y / (double)taille);
		int num3 = pl / taille;
		int num4 = ph / taille;
		double num5 = 0.0;
		for (int i = -1; i <= 1; i++)
		{
			for (int j = -1; j <= 1; j++)
			{
				int num6 = num + j;
				int num7 = num2 + i;
				int a = (num6 % num3 + num3) % num3;
				int b = (num7 % num4 + num4) % num4;
				double num8 = ((double)num6 + 0.15 + 0.7 * Hasard2(a, b, graine)) * (double)taille;
				double num9 = ((double)num7 + 0.15 + 0.7 * Hasard2(a, b, graine + 1)) * (double)taille;
				double num10 = (double)taille * (0.62 + 0.5 * Hasard2(a, b, graine + 2));
				double num11 = x - num8;
				double num12 = (y - num9) * 1.25;
				num5 += Math.Exp(9.0 * Math.Max(1.0 - (num11 * num11 + num12 * num12) / (num10 * num10), -1.0));
			}
		}
		return Math.Max(0.0, Math.Log(num5) / 9.0);
	}

	private static byte[] Nappe(int pl, int ph, int graine, (byte R, byte G, byte B)[] palette, float trouee, float contraste)
	{
		float[] h = new float[pl * ph];
		Parallel.For(0, ph, delegate(int y)
		{
			for (int i = 0; i < pl; i++)
			{
				double x = (double)i + 9.0 * Math.Sin((double)y / (double)ph * Math.PI * 2.0 * 5.0 + (double)graine);
				double y2 = (double)y + 7.0 * Math.Sin((double)i / (double)pl * Math.PI * 2.0 * 4.0 + (double)(graine * 2));
				h[y * pl + i] = (float)(0.38 * Domes(x, y2, 256, pl, ph, graine + 30) + 0.3 * Domes(x, y2, 128, pl, ph, graine) + 0.22 * Domes(x, y2, 64, pl, ph, graine + 10) + 0.1 * Domes(x, y2, 32, pl, ph, graine + 20));
			}
		});
		float[] array = (float[])h.Clone();
		Array.Sort(array);
		float num = array[(int)((float)array.Length * 0.005f)];
		float num2 = array[(int)((float)array.Length * 0.995f)];
		float num3 = ((trouee > 0f) ? Math.Clamp((array[(int)((float)array.Length * trouee)] - num) / (num2 - num), 0f, 1f) : (-1f));
		for (int num4 = 0; num4 < h.Length; num4++)
		{
			h[num4] = Math.Clamp((h[num4] - num) / (num2 - num), 0f, 1f);
		}
		byte[] array2 = new byte[pl * ph * 4];
		int num5 = palette.Length;
		for (int num6 = 0; num6 < ph; num6++)
		{
			for (int num7 = 0; num7 < pl; num7++)
			{
				float num8 = h[num6 * pl + num7];
				if (!(num8 < num3))
				{
					float num9 = (num8 - h[(num6 - 3 + ph) % ph * pl + (num7 - 3 + pl) % pl]) * contraste;
					float num10 = ((num3 >= 0f && num8 < num3 + 0.035f) ? 0f : (0.16f + 0.56f * num8 + num9));
					int num11 = Math.Clamp((int)MathF.Floor(num10 * (float)num5 + TrameAuDela[(num6 & 3) * 4 + (num7 & 3)] - 0.5f), 0, num5 - 1);
					int num12 = (num6 * pl + num7) * 4;
					array2[num12] = palette[num11].R;
					array2[num12 + 1] = palette[num11].G;
					array2[num12 + 2] = palette[num11].B;
					array2[num12 + 3] = byte.MaxValue;
				}
			}
		}
		return array2;
	}

	private static bool TexturesAuDela(MonoRenderer rendu)
	{
		if (_texMerProche != null)
		{
			return true;
		}
		byte[][] auDelaPret = _auDelaPret;
		if (auDelaPret != null)
		{
			_texMerProche = rendu.CreerTexturePixels("audela/mer-proche", 1024, 768, auDelaPret[0]);
			_texMerLointaine = rendu.CreerTexturePixels("audela/mer-lointaine", 768, 512, auDelaPret[1]);
			_texVoileAuDela = rendu.CreerTexturePixels("audela/voile", 256, 80, auDelaPret[2]);
			_texRoseAuDela = rendu.CreerTexturePixels("audela/rose", 2, 64, auDelaPret[3]);
			_texAme = rendu.CreerTexturePixels("audela/ame", 14, DessinDeLAme.Length, auDelaPret[4]);
			_auDelaPret = null;
			return true;
		}
		if (!_auDelaEnCours)
		{
			_auDelaEnCours = true;
			Task.Run(delegate
			{
				try
				{
					byte[] array = new byte[81920];
					(float, float, float, float, float)[] array2 = new(float, float, float, float, float)[2]
					{
						(150f, 58f, 150f, 42f, 0.4f),
						(235f, 48f, 110f, 30f, 0.32f)
					};
					for (int i = 0; i < 80; i++)
					{
						for (int j = 0; j < 256; j++)
						{
							float num = (float)j * 360f / 256f;
							float num2 = (float)i * 110f / 80f;
							float num3 = 1f;
							(float, float, float, float, float)[] array3 = array2;
							for (int k = 0; k < array3.Length; k++)
							{
								(float, float, float, float, float) tuple = array3[k];
								float item = tuple.Item1;
								float item2 = tuple.Item2;
								float item3 = tuple.Item3;
								float item4 = tuple.Item4;
								float item5 = tuple.Item5;
								float num4 = MathF.Sqrt((num - item) * (num - item) / (item3 * item3) + (num2 - item2) * (num2 - item2) / (item4 * item4));
								num3 *= 1f - item5 * Math.Clamp(1f - num4, 0f, 1f);
							}
							int num5 = (i * 256 + j) * 4;
							int num6 = num5 + 1;
							byte b;
							array[num5 + 2] = (b = byte.MaxValue);
							array[num5] = (array[num6] = b);
							array[num5 + 3] = (byte)((1f - num3) * 255f);
						}
					}
					byte[] array4 = new byte[512];
					for (int l = 0; l < 64; l++)
					{
						float num7 = ((float)l + 0.5f) / 64f;
						float num8 = ((num7 < 0.55f) ? (0.3f + -0.23000002f * (num7 / 0.55f)) : (0.07f * (1f - (num7 - 0.55f) / 0.45f)));
						for (int m = 0; m < 2; m++)
						{
							int num9 = (l * 2 + m) * 4;
							int num10 = num9 + 1;
							byte b;
							array4[num9 + 2] = (b = byte.MaxValue);
							array4[num9] = (array4[num10] = b);
							array4[num9 + 3] = (byte)(num8 * 255f);
						}
					}
					byte[] array5 = new byte[14 * DessinDeLAme.Length * 4];
					for (int n = 0; n < DessinDeLAme.Length; n++)
					{
						for (int num11 = 0; num11 < 14; num11++)
						{
							char c = DessinDeLAme[n][num11];
							if (c != '.')
							{
								int num12 = (n * 14 + num11) * 4;
								array5[num12] = (byte)((c == 'w') ? 255u : 191u);
								array5[num12 + 1] = (byte)((c == 'w') ? 255u : 228u);
								int num13 = num12 + 2;
								byte b;
								array5[num12 + 3] = (b = byte.MaxValue);
								array5[num13] = b;
							}
						}
					}
					_auDelaPret = new byte[5][]
					{
						Nappe(1024, 768, 3, PaletteProche, 0.075f, 2.4f),
						Nappe(768, 512, 11, PaletteLointaine, 0f, 2f),
						array,
						array4,
						array5
					};
				}
				catch (Exception exception)
				{
					ApplicationContext.Context.Value?.Logger.LogWarning(exception, "Mer de nuages impossible a fabriquer");
				}
			});
		}
		return false;
	}

	private static bool CuireLeSerpent(CarteAuDela etat, MonoRenderer rendu)
	{
		if (SerpentsCuits.TryGetValue(etat.Signature, out (IGameTexture, IGameTexture) value))
		{
			(etat.Serpent, etat.Ombre) = value;
			return etat.Cuite = true;
		}
		MapInstance carte = etat.Carte;
		int tileWidth = Options.Instance.Map.TileWidth;
		int tileHeight = Options.Instance.Map.TileHeight;
		int num = Options.Instance.Map.MapWidth * tileWidth;
		int num2 = Options.Instance.Map.MapHeight * tileHeight;
		int num3 = num / 2;
		int num4 = num2 / 2;
		float[] array = new float[num3 * num4];
		int num5 = 0;
		Guid guid = Guid.Empty;
		string text = null;
		IGameTexture gameTexture = null;
		foreach (string item in Options.Instance.Map.Layers.All)
		{
			if (!carte.Layers.TryGetValue(item, out var value2))
			{
				continue;
			}
			for (int i = 0; i < value2.GetLength(0); i++)
			{
				for (int j = 0; j < value2.GetLength(1); j++)
				{
					Tile tile = value2[i, j];
					if (tile.TilesetId == Guid.Empty || tile.X < 0 || tile.Y < 0 || tile.Autotile != 0)
					{
						continue;
					}
					if (tile.TilesetId != guid)
					{
						guid = tile.TilesetId;
						text = (DatabaseObject<TilesetDescriptor>.TryGet(guid, out var databaseObject) ? databaseObject.Name : null);
						gameTexture = ((text == null) ? null : Globals.ContentManager?.GetTexture(TextureType.Tileset, text));
					}
					if (gameTexture == null || !TuileDuSerpent(text, tile.X, tile.Y))
					{
						continue;
					}
					if (!Casse.PixelsDeTuile(gameTexture, tile.X, tile.Y, out byte[] tuile))
					{
						return false;
					}
					if (tuile == null || tuile.Length < tileWidth * tileHeight * 4)
					{
						continue;
					}
					for (int k = 0; k < tileHeight; k++)
					{
						for (int l = 0; l < tileWidth; l++)
						{
							int num6 = (k * tileWidth + l) * 4;
							if (tuile[num6 + 3] > 40 && tuile[num6] <= tuile[num6 + 2] + 20 && Math.Min(tuile[num6], Math.Min(tuile[num6 + 1], tuile[num6 + 2])) <= 150)
							{
								array[(j * tileHeight + k) / 2 * num3 + (i * tileWidth + l) / 2] = 1f;
								num5++;
							}
						}
					}
				}
			}
		}
		if (num5 > 0)
		{
			byte[] array2 = new byte[num3 * num4 * 4];
			for (int m = 0; m < array.Length; m++)
			{
				int num7 = m * 4;
				int num8 = m * 4 + 1;
				byte b;
				array2[m * 4 + 2] = (b = byte.MaxValue);
				array2[num7] = (array2[num8] = b);
				array2[m * 4 + 3] = (byte)(array[m] * 255f);
			}
			float[] array3 = new float[array.Length];
			for (int n = 0; n < 2; n++)
			{
				Adoucir(array, array3, num3, num4, 3, horizontal: true);
				Adoucir(array3, array, num3, num4, 3, horizontal: false);
			}
			byte[] array4 = new byte[num3 * num4 * 4];
			for (int num9 = 0; num9 < array.Length; num9++)
			{
				int num10 = num9 * 4;
				int num11 = num9 * 4 + 1;
				byte b;
				array4[num9 * 4 + 2] = (b = byte.MaxValue);
				array4[num10] = (array4[num11] = b);
				array4[num9 * 4 + 3] = (byte)(array[num9] * 255f);
			}
			value = (rendu.CreerTexturePixels($"audela/serpent/{etat.Signature:x}", num3, num4, array2), rendu.CreerTexturePixels($"audela/ombre/{etat.Signature:x}", num3, num4, array4));
		}
		SerpentsCuits[etat.Signature] = value;
		(etat.Serpent, etat.Ombre) = value;
		return etat.Cuite = true;
	}

	private static void Adoucir(float[] source, float[] cible, int l, int h, int rayon, bool horizontal)
	{
		int num = (horizontal ? l : h);
		int num2 = (horizontal ? h : l);
		int num3 = (horizontal ? 1 : l);
		float num4 = 1f / (float)(2 * rayon + 1);
		for (int i = 0; i < num2; i++)
		{
			int num5 = (horizontal ? (i * l) : i);
			float num6 = 0f;
			for (int j = -rayon; j <= rayon; j++)
			{
				num6 += source[num5 + Math.Clamp(j, 0, num - 1) * num3];
			}
			for (int k = 0; k < num; k++)
			{
				cible[num5 + k * num3] = num6 * num4;
				num6 += source[num5 + Math.Min(k + rayon + 1, num - 1) * num3] - source[num5 + Math.Max(k - rayon, 0) * num3];
			}
		}
	}

	private static double Tourner(double valeur, double tour)
	{
		return (valeur % tour + tour) % tour;
	}

	private static void PaverLaMer(IGameTexture nappe, FloatRect vue, double parallaxe, double deriveX, double deriveY)
	{
		double num = Tourner((double)vue.Left * (1.0 - parallaxe) + deriveX, nappe.Width);
		double num2 = Tourner((double)vue.Top * (1.0 - parallaxe) + deriveY, nappe.Height);
		Paves(nappe, 1f, 1f, (float)num, (float)num2, 255, 255, 255, 1f, GameBlendModes.None, null);
	}

	public static void DessinerAuDelaFond()
	{
		_voisinesAuDela.Clear();
		if (_auDelaEnPanne || !(Graphics.Renderer is MonoRenderer rendu))
		{
			return;
		}
		Player me = Globals.Me;
		if (me == null)
		{
			return;
		}
		Guid[,] mapGrid = Globals.MapGrid;
		if (mapGrid == null || !MapInstance.TryGet(me.MapId, out MapInstance instance) || !instance.IsLoaded)
		{
			return;
		}
		try
		{
			for (int i = Math.Max(instance.GridX - 1, 0); i <= Math.Min(instance.GridX + 1, Globals.MapGridWidth - 1); i++)
			{
				for (int j = Math.Max(instance.GridY - 1, 0); j <= Math.Min(instance.GridY + 1, Globals.MapGridHeight - 1); j++)
				{
					if (MapInstance.TryGet(mapGrid[i, j], out MapInstance instance2))
					{
						CarteAuDela carteAuDela = EtatAuDela(instance2);
						if (carteAuDela != null && carteAuDela.Mer)
						{
							_voisinesAuDela.Add(carteAuDela);
						}
					}
				}
			}
			if (_voisinesAuDela.Count == 0)
			{
				return;
			}
			FloatRect worldViewport = Graphics.WorldViewport;
			double num = (double)Timing.Global.Milliseconds / 1000.0;
			if (TexturesAuDela(rendu))
			{
				PaverLaMer(_texMerLointaine, worldViewport, 0.34, num * 3.0, num * 0.6);
				PaverLaMer(_texMerProche, worldViewport, 0.62, num * 8.0, num * 1.6);
				Texture(_texRoseAuDela, worldViewport.Left, worldViewport.Top, worldViewport.Width, worldViewport.Height * 0.62f, 248, 88, 200, 1f);
			}
			else
			{
				Carre(worldViewport.Left, worldViewport.Top, worldViewport.Width, worldViewport.Height, 248, 208, 64, 1f);
			}
			int num2 = Options.Instance.Map.MapWidth * Options.Instance.Map.TileWidth;
			int num3 = Options.Instance.Map.MapHeight * Options.Instance.Map.TileHeight;
			bool flag = false;
			foreach (CarteAuDela item in _voisinesAuDela)
			{
				if (!item.Cuite && !flag)
				{
					flag = true;
					CuireLeSerpent(item, rendu);
				}
				IGameTexture ombre = item.Ombre;
				if (ombre != null)
				{
					Texture(ombre, item.Carte.X + 34, item.Carte.Y + 60, num2, num3, 96, 24, 0, 0.36f);
				}
			}
			DessinerLesAmes(worldViewport, num);
		}
		catch (Exception exception)
		{
			PanneAuDela(exception);
		}
	}

	private static void DessinerLesAmes(FloatRect vue, double t)
	{
		IGameTexture texAme = _texAme;
		if (texAme == null)
		{
			return;
		}
		for (int i = 0; i < 9; i++)
		{
			double num = 14.0 + HasardAuDela(i) * 14.0;
			double num2 = Tourner(HasardAuDela(i + 20) * 3600.0 + t * num - (double)vue.Left * 0.86, 3600.0) - 200.0;
			double num3 = Tourner(60.0 + HasardAuDela(i + 40) * 700.0 + Math.Sin(t * (0.7 + HasardAuDela(i + 60)) + (double)i) * 7.0 - (double)vue.Top * 0.86, 900.0) - 60.0;
			if (!(num2 > (double)(vue.Width + 30f)) && !(num3 > (double)(vue.Height + 30f)))
			{
				float num4 = MathF.Round(vue.Left + (float)num2);
				float num5 = MathF.Round(vue.Top + (float)num3);
				Carre(num4 + 9f, num5 + 30f, 10f, 3f, 120, 40, 0, 0.22f);
				Carre(num4 + 11f, num5 + 29f, 6f, 5f, 120, 40, 0, 0.22f);
				IGameTexture texHalo = _texHalo;
				if (texHalo != null)
				{
					Texture(texHalo, num4 - 6f, num5 - 6f, 26f, 26f, 200, 230, 255, 0.16f, GameBlendModes.Add);
				}
				Texture(texAme, num4, num5, texAme.Width, texAme.Height, 255, 255, 255, 0.95f);
			}
		}
	}

	public static void DessinerAuDelaSol()
	{
		if (_auDelaEnPanne || _voisinesAuDela.Count == 0)
		{
			return;
		}
		try
		{
			double num = (double)Timing.Global.Milliseconds / 1000.0;
			float num2 = (float)(num % 6.5) / 2.6f;
			if (num2 >= 1f)
			{
				return;
			}
			FloatRect worldViewport = Graphics.WorldViewport;
			float num3 = worldViewport.Left - 260f + num2 * (worldViewport.Width + 520f);
			int num4 = Options.Instance.Map.MapWidth * Options.Instance.Map.TileWidth;
			int num5 = Options.Instance.Map.MapHeight * Options.Instance.Map.TileHeight;
			(float, float)[] array = new(float, float)[3]
			{
				(110f, 0.1f),
				(64f, 0.16f),
				(26f, 0.26f)
			};
			foreach (CarteAuDela item3 in _voisinesAuDela)
			{
				IGameTexture serpent = item3.Serpent;
				if (serpent == null)
				{
					continue;
				}
				float num6 = item3.Carte.X;
				float num7 = item3.Carte.Y;
				float num8 = MathF.Max(num7, MathF.Floor((worldViewport.Top - num7) / 8f) * 8f + num7);
				float num9 = MathF.Min(num7 + (float)num5, worldViewport.Bottom);
				for (float num10 = num8; num10 < num9; num10 += 8f)
				{
					float num11 = num3 + (worldViewport.Top + worldViewport.Height / 2f - num10) * 0.556f;
					(float, float)[] array2 = array;
					for (int i = 0; i < array2.Length; i++)
					{
						(float, float) tuple = array2[i];
						float item = tuple.Item1;
						float item2 = tuple.Item2;
						float num12 = MathF.Max(num6, MathF.Floor((num11 - item / 2f - num6) / 2f) * 2f + num6);
						float num13 = MathF.Min(num6 + (float)num4, num11 + item / 2f);
						if (!(num13 - num12 < 2f))
						{
							Graphics.DrawGameTexture(serpent, new FloatRect((num12 - num6) / 2f, (num10 - num7) / 2f, (num13 - num12) / 2f, 4f), new FloatRect(num12, num10, num13 - num12, 8f), Couleur(190, 230, 255, item2), null, GameBlendModes.Add);
						}
					}
				}
			}
		}
		catch (Exception exception)
		{
			PanneAuDela(exception);
		}
	}

	public static void DessinerAuDelaCiel()
	{
		if (_auDelaEnPanne || _voisinesAuDela.Count == 0)
		{
			return;
		}
		try
		{
			FloatRect worldViewport = Graphics.WorldViewport;
			double num = (double)Timing.Global.Milliseconds / 1000.0;
			IGameTexture texVoileAuDela = _texVoileAuDela;
			if (texVoileAuDela != null)
			{
				for (int i = 0; i < 8; i++)
				{
					float num2 = (float)(0.8 + HasardAuDela(i + 5) * 1.1);
					double num3 = Tourner(HasardAuDela(i + 11) * 4700.0 + num * (20.0 + HasardAuDela(i + 3) * 16.0) - (double)worldViewport.Left * 1.3, 4700.0) - 500.0;
					double num4 = Tourner(HasardAuDela(i + 31) * 1000.0 + Math.Sin(num * 0.25 + (double)i) * 8.0 - (double)worldViewport.Top * 1.3, 1100.0) - 220.0;
					if (!(num3 > (double)(worldViewport.Width + 60f)) && !(num4 > (double)(worldViewport.Height + 40f)))
					{
						Texture(texVoileAuDela, worldViewport.Left + (float)num3, worldViewport.Top + (float)num4, 360f * num2, 110f * num2, 255, 246, 206, 0.9f);
					}
				}
			}
			if (Voiles())
			{
				IGameTexture texRayons = _texRayons;
				if (texRayons != null)
				{
					float num5 = (float)(0.85 + 0.15 * Math.Sin(num * 0.4));
					Paves(texRayons, 2f, 2f, (float)Tourner(num * 2.0, texRayons.Width * 2), 0f, 255, 232, 160, 0.12f * num5, GameBlendModes.Add, null);
				}
			}
		}
		catch (Exception exception)
		{
			PanneAuDela(exception);
		}
	}

	private static long HeureDesChutes()
	{
		return Timing.Global.Milliseconds % 86400000;
	}

	private static bool EstTuileDeChute(Guid planche, int colonne, int rangee, int rangeeVoulue)
	{
		bool flag = rangee == rangeeVoulue;
		bool flag2 = flag;
		if (flag2)
		{
			bool flag3 = (uint)(colonne - 12) <= 1u;
			flag2 = flag3;
		}
		if (flag2 && DatabaseObject<TilesetDescriptor>.TryGet(planche, out var databaseObject))
		{
			return string.Equals(databaseObject.Name, "majeur.png", StringComparison.OrdinalIgnoreCase);
		}
		return false;
	}

	internal static (int De, int A, int Rangee)? DefilementDeChute(string planche, int colonne, int rangee)
	{
		bool chutesEnPanne = _chutesEnPanne;
		bool flag = chutesEnPanne;
		if (!flag)
		{
			bool flag2 = (uint)(colonne - 12) <= 1u;
			flag = !flag2;
		}
		bool flag3 = flag;
		if (!flag3)
		{
			bool flag2 = ((rangee < 43 || rangee > 45) ? true : false);
			flag3 = flag2;
		}
		if (flag3 || !string.Equals(planche, "majeur.png", StringComparison.OrdinalIgnoreCase))
		{
			return null;
		}
		var (item, item2) = Cascades.LignesQuiDefilent(rangee - 43);
		return (item, item2, 44);
	}

	internal static (int De, int A)? LignesDeChute(IGameTexture planche, int colonne, int rangee)
	{
		bool flag = (uint)(colonne - 12) <= 1u;
		if (!flag || rangee < 43 || rangee > 45 || !string.Equals(Path.GetFileName(planche.Name ?? ""), "majeur.png", StringComparison.OrdinalIgnoreCase))
		{
			return null;
		}
		return Cascades.LignesDeChute(rangee - 43);
	}

	internal static int GlissementDesChutes(int hauteurDeTuile)
	{
		return (int)(Math.Floor((double)HeureDesChutes() * 0.058) % (double)Math.Max(1, hauteurDeTuile));
	}

	private static List<ChuteVue>? ChutesDe(MapInstance carte)
	{
		if (ChutesParCarte.TryGetValue(carte.Id, out ChutesDeCarte value))
		{
			if (value.Carte == carte)
			{
				if (value.Chutes.Count != 0)
				{
					return value.Chutes;
				}
				return null;
			}
			foreach (ChuteVue chute in value.Chutes)
			{
				chute.Liberer();
			}
		}
		List<ChuteVue> list = Reperer(carte);
		ChutesParCarte[carte.Id] = new ChutesDeCarte
		{
			Carte = carte,
			Chutes = list
		};
		if (list.Count > 0)
		{
			ApplicationContext.Context.Value?.Logger.LogInformation("Cascades de {Carte} : {Nombre} chute(s), {Detail}", carte.Name, list.Count, string.Join(" ; ", list.Select((ChuteVue c) => $"{c.Couche} x {c.Chute.X0}-{c.Chute.X1}, rideau {c.Chute.Rideau}, pied {c.Chute.Pied}")));
		}
		MenageDesChutes();
		if (list.Count != 0)
		{
			return list;
		}
		return null;
	}

	private static List<ChuteVue> Reperer(MapInstance carte)
	{
		List<ChuteVue> list = new List<ChuteVue>();
		int tileWidth = Options.Instance.Map.TileWidth;
		int tileHeight = Options.Instance.Map.TileHeight;
		int mapWidth = Options.Instance.Map.MapWidth;
		int mapHeight = Options.Instance.Map.MapHeight;
		List<string> lowerLayers = Options.Instance.Map.Layers.LowerLayers;
		List<string> list2 = lowerLayers.Concat(Options.Instance.Map.Layers.MiddleLayers).Concat(Options.Instance.Map.Layers.UpperLayers).ToList();
		if (carte.Layers == null || tileWidth != 32 || tileHeight != 32)
		{
			return list;
		}
		foreach (string item5 in lowerLayers)
		{
			if (!carte.Layers.TryGetValue(item5, out var tuiles) || tuiles.GetLength(0) < mapWidth || tuiles.GetLength(1) < mapHeight)
			{
				continue;
			}
			foreach (var item6 in Cascades.Trouver(Genre, mapWidth, mapHeight))
			{
				int item = item6.X0;
				int item2 = item6.X1;
				int item3 = item6.Haut;
				int item4 = item6.Pied;
				Cascades.Chute chute = new Cascades.Chute(item * tileWidth, (item2 + 1) * tileWidth, item3 * tileHeight, item4 * tileHeight);
				HashSet<(int, int)> hashSet = new HashSet<(int, int)>();
				foreach (string item7 in list2.Skip(list2.IndexOf(item5) + 1))
				{
					if (!carte.Layers.TryGetValue(item7, out var value) || value.GetLength(0) < mapWidth || value.GetLength(1) < mapHeight)
					{
						continue;
					}
					for (int i = item3; i < item4; i++)
					{
						for (int j = item; j <= item2; j++)
						{
							if (value[j, i].TilesetId != Guid.Empty && value[j, i].X >= 0 && value[j, i].Y >= 0)
							{
								hashSet.Add((j, i));
							}
						}
					}
				}
				int num = (int)Math.Ceiling(chute.Mesure(256.0));
				int num2 = (chute.X0 + chute.X1) / 2;
				int num3 = Math.Min(chute.Rideau, chute.Ecume - 150);
				list.Add(new ChuteVue
				{
					Chute = chute,
					Couche = item5,
					CasesCouvertes = hashSet,
					Eau = new Cascades.Tampon(chute.X0, chute.Rideau, chute.Largeur, chute.Pied - chute.Rideau),
					Riviere = new Cascades.Tampon(chute.X0, chute.Riviere, chute.Largeur, chute.Rideau - chute.Riviere),
					Sol = new Cascades.Tampon(num2 - num, chute.Ecume - 48, Math.Min(2 * num, 1024), Math.Min(60 + (int)Math.Ceiling(chute.Mesure(150.0)), 512)),
					Air = new Cascades.Tampon(chute.X0 - 8, num3, chute.Largeur + 16, chute.Ecume + 56 - num3)
				});
			}
			int Genre(int x, int y)
			{
				if (!EstTuileDeChute(tuiles[x, y].TilesetId, tuiles[x, y].X, tuiles[x, y].Y, 44))
				{
					if (!EstTuileDeChute(tuiles[x, y].TilesetId, tuiles[x, y].X, tuiles[x, y].Y, 45))
					{
						return 0;
					}
					return 2;
				}
				return 1;
			}
		}
		return list;
	}

	private static void PoserLesLucioles(MapInstance carte, ChuteVue v)
	{
		if (v.Lucioles.Length != 0 || !Analyses.TryGetValue(carte.Id, out Analyse value) || value.Carte != carte || value.Eau.Length != value.L * value.H)
		{
			return;
		}
		Cascades.Chute chute = v.Chute;
		List<(float, float)> list = new List<(float, float)>();
		int num = chute.Nombre(18);
		for (int i = 0; i < 400; i++)
		{
			if (list.Count >= num)
			{
				break;
			}
			int num2 = (int)((double)(chute.X0 + chute.X1) / 2.0 + (Cascades.Tirage(i, carte.Id.GetHashCode()) - 0.5) * chute.Mesure(420.0));
			int num3 = (int)((double)(chute.Ecume - 240) + Cascades.Tirage(i, 977L) * 560.0);
			if (num2 >= 8 && num3 >= 8 && num2 < value.L - 8 && num3 < value.H - 8 && value.Eau[num3 * value.L + num2] == 0 && (num2 < chute.X0 - 12 || num2 >= chute.X1 + 12 || num3 < chute.Levre || num3 >= chute.Ecume + 8))
			{
				list.Add((num2, num3));
			}
		}
		v.Lucioles = list.ToArray();
	}

	private static void MenageDesChutes()
	{
		if (ChutesParCarte.Count <= 12 || !(Globals.Me?.MapInstance is MapInstance mapInstance))
		{
			return;
		}
		KeyValuePair<Guid, ChutesDeCarte>[] array = ChutesParCarte.ToArray();
		for (int i = 0; i < array.Length; i++)
		{
			KeyValuePair<Guid, ChutesDeCarte> keyValuePair = array[i];
			var (guid2, chutesDeCarte2) = keyValuePair;
			if (Math.Abs(chutesDeCarte2.Carte.GridX - mapInstance.GridX) <= 2 && Math.Abs(chutesDeCarte2.Carte.GridY - mapInstance.GridY) <= 2 && MapInstance.TryGet(guid2, out MapInstance instance) && instance == chutesDeCarte2.Carte)
			{
				continue;
			}
			foreach (ChuteVue chute in chutesDeCarte2.Chutes)
			{
				chute.Liberer();
			}
			ChutesParCarte.Remove(guid2);
		}
	}

	private static bool ChuteALEcran(MapInstance carte, Cascades.Tampon tampon)
	{
		FloatRect worldViewport = Graphics.WorldViewport;
		if ((float)(carte.X + tampon.X + tampon.L) >= worldViewport.Left && (float)(carte.X + tampon.X) <= worldViewport.Right && (float)(carte.Y + tampon.Y + tampon.H) >= worldViewport.Top)
		{
			return (float)(carte.Y + tampon.Y) <= worldViewport.Bottom;
		}
		return false;
	}

	private static bool ARefaire(IGameTexture? texture, int tour)
	{
		if (texture != null)
		{
			return (_imageDesChutes & 1) == tour;
		}
		return true;
	}

	private static void Envoyer(Cascades.Tampon tampon, ref IGameTexture? texture, string nom)
	{
		if (Graphics.Renderer is MonoRenderer monoRenderer && (texture == null || !MonoRenderer.RemplacerPixels(texture, tampon.Rgba)))
		{
			texture?.Dispose();
			texture = monoRenderer.CreerTexturePixels(nom, tampon.L, tampon.H, tampon.Rgba);
		}
	}

	private static void Poser(MapInstance carte, Cascades.Tampon tampon, IGameTexture? texture)
	{
		if (texture != null)
		{
			Texture(texture, carte.X + tampon.X, carte.Y + tampon.Y, tampon.L, tampon.H, 255, 255, 255, 1f);
		}
	}

	private static void PoserTampon(MapInstance carte, Cascades.Tampon tampon, ref IGameTexture? texture, string nom)
	{
		Envoyer(tampon, ref texture, nom);
		Poser(carte, tampon, texture);
	}

	private static Func<int, int, bool> EauDe(MapInstance carte)
	{
		if (!Analyses.TryGetValue(carte.Id, out Analyse a) || a.Carte != carte || a.Eau.Length != a.L * a.H)
		{
			return (int _, int _) => false;
		}
		return (int x, int y) => x >= 0 && y >= 0 && x < a.L && y < a.H && a.Eau[y * a.L + x] != 0;
	}

	private static void PanneDesChutes(Exception exception)
	{
		if (_chutesEnPanne)
		{
			return;
		}
		_chutesEnPanne = true;
		ApplicationContext.Context.Value?.Logger.LogError(exception, "Cascades coupees apres un incident");
		try
		{
			CouperSonDesChutes();
			foreach (ChutesDeCarte value in ChutesParCarte.Values)
			{
				foreach (ChuteVue chute in value.Chutes)
				{
					chute.Liberer();
				}
			}
			ChutesParCarte.Clear();
		}
		catch
		{
		}
	}

	internal static void DessinerChutesDeLaCouche(MapInstance carte, string couche)
	{
		if (_chutesEnPanne || !Pret() || _zone == GenreZone.Interieur)
		{
			return;
		}
		try
		{
			List<ChuteVue> list = ChutesDe(carte);
			if (list == null)
			{
				return;
			}
			long t = HeureDesChutes();
			float num = Math.Clamp(_etal.Lampes, 0f, 1f);
			if (num > 0.98f)
			{
				return;
			}
			foreach (ChuteVue item in list)
			{
				if (!(item.Couche != couche) && ChuteALEcran(carte, item.Eau))
				{
					item.Eau.Vider();
					Cascades.Filets(item.Eau, item.Chute, t, 0.62 * (double)(1f - num));
					PoserTampon(carte, item.Eau, ref item.TexEau, $"cascade/eau/{carte.Id}/{item.Chute.X0}/{item.Chute.Pied}");
				}
			}
		}
		catch (Exception exception)
		{
			PanneDesChutes(exception);
		}
	}

	private static void DessinerChutesAuSol()
	{
		if (_chutesEnPanne)
		{
			return;
		}
		try
		{
			_imageDesChutes++;
			long t = HeureDesChutes();
			foreach (MapInstance visible in _visibles)
			{
				List<ChuteVue> list = ChutesDe(visible);
				if (list == null)
				{
					continue;
				}
				foreach (ChuteVue item in list)
				{
					Func<int, int, bool> surLEau = EauDe(visible);
					if (ChuteALEcran(visible, item.Riviere))
					{
						item.Riviere.Vider();
						Cascades.Courant(item.Riviere, item.Chute, t, surLEau);
						PoserTampon(visible, item.Riviere, ref item.TexRiviere, $"cascade/riviere/{visible.Id}/{item.Chute.X0}/{item.Chute.Pied}");
					}
					if (ChuteALEcran(visible, item.Sol))
					{
						if (ARefaire(item.TexSol, 0))
						{
							item.Sol.Vider();
							Cascades.Ondes(item.Sol, item.Chute, t, surLEau);
							Cascades.Panache(item.Sol, item.Chute, t);
							Cascades.Embruns(item.Sol, item.Chute, t, surLEau);
							Envoyer(item.Sol, ref item.TexSol, $"cascade/sol/{visible.Id}/{item.Chute.X0}/{item.Chute.Pied}");
						}
						Poser(visible, item.Sol, item.TexSol);
					}
				}
			}
		}
		catch (Exception exception)
		{
			PanneDesChutes(exception);
		}
	}

	private static void DessinerChutesApresNuit(in Etalonnage e)
	{
		if (_chutesEnPanne)
		{
			return;
		}
		try
		{
			long t = HeureDesChutes();
			float num = Math.Clamp(e.Lampes, 0f, 1f);
			float num2 = Math.Clamp(_p.Pluie + _p.Orage + _p.Neige + _p.Sable, 0f, 1f);
			float num3 = Math.Clamp(e.Soleil, 0f, 1f) * (1f - num) * (1f - num2);
			float num4 = num * (1f - _p.Pluie) * (1f - _p.Sable) * (1f - _p.Orage) * (1f - _p.Neige);
			foreach (MapInstance visible in _visibles)
			{
				List<ChuteVue> list = ChutesDe(visible);
				if (list == null)
				{
					continue;
				}
				foreach (ChuteVue v in list)
				{
					if (ChuteALEcran(visible, v.Air))
					{
						if (ARefaire(v.TexAir, 1))
						{
							v.Air.Vider();
							if (num > 0.02f)
							{
								Cascades.Filets(v.Air, v.Chute, t, 0.5 * (double)num, (int x, int y) => !v.CasesCouvertes.Contains((x >> 5, y >> 5)));
							}
							Cascades.Brume(v.Air, v.Chute, t, num);
							Cascades.ArcEnCiel(v.Air, v.Chute, t, num3);
							Envoyer(v.Air, ref v.TexAir, $"cascade/air/{visible.Id}/{v.Chute.X0}/{v.Chute.Pied}");
						}
						Poser(visible, v.Air, v.TexAir);
					}
					if (!(num4 <= 0.05f) && _texHalo != null)
					{
						PoserLesLucioles(visible, v);
						for (int num5 = 0; num5 < v.Lucioles.Length; num5++)
						{
							(double X, double Y, double Eclat) tuple = Cascades.Luciole(num5, (float)visible.X + v.Lucioles[num5].X, (float)visible.Y + v.Lucioles[num5].Y, t);
							double item = tuple.X;
							double item2 = tuple.Y;
							double item3 = tuple.Eclat;
							float num6 = (float)item3 * num4;
							Texture(_texHalo, (float)item - 9f, (float)item2 - 9f, 18f, 18f, 200, 255, 120, 0.55f * num6, GameBlendModes.Add);
							Pt((float)item, (float)item2, 240, 255, 170, num6, GameBlendModes.Add);
						}
					}
				}
			}
		}
		catch (Exception exception)
		{
			PanneDesChutes(exception);
		}
	}

	private static void MajSonDesChutes(float dt)
	{
		if (_chutesEnPanne)
		{
			return;
		}
		try
		{
			float num = 0f;
			if (_zone != GenreZone.Interieur && _actif)
			{
				Player me = Globals.Me;
				if (me != null)
				{
					System.Numerics.Vector2 origin = me.Origin;
					foreach (ChutesDeCarte value in ChutesParCarte.Values)
					{
						if (!value.Carte.IsLoaded)
						{
							continue;
						}
						foreach (ChuteVue chute in value.Chutes)
						{
							float num2 = Math.Clamp(origin.X, value.Carte.X + chute.Chute.X0, value.Carte.X + chute.Chute.X1);
							float num3 = value.Carte.Y + chute.Chute.Ecume;
							float num4 = MathF.Sqrt((origin.X - num2) * (origin.X - num2) + (origin.Y - num3) * (origin.Y - num3));
							float x = 1f - Math.Clamp((num4 - 48f) / 592f, 0f, 1f);
							num = Math.Max(num, MathF.Pow(x, 1.6f));
						}
					}
				}
			}
			_niveauDesChutes += (num - _niveauDesChutes) * (1f - MathF.Exp((0f - dt) / 450f));
			int num5 = (int)MathF.Round(_niveauDesChutes * 62f);
			BoucleDeChute sonDesChutes;
			if (num5 > 0)
			{
				_silenceDesChutes = 0f;
				sonDesChutes = _sonDesChutes;
				if (sonDesChutes == null || !sonDesChutes.Loaded)
				{
					BoucleDeChute boucleDeChute = new BoucleDeChute();
					if (!boucleDeChute.Loaded || !Audio.AjouterSonMeteo(boucleDeChute))
					{
						boucleDeChute.Stop();
						boucleDeChute = null;
					}
					_sonDesChutes = boucleDeChute;
				}
				if (_sonDesChutes != null)
				{
					_sonDesChutes.Volume = num5;
				}
				return;
			}
			sonDesChutes = _sonDesChutes;
			if (sonDesChutes != null && sonDesChutes.Loaded)
			{
				_sonDesChutes.Volume = 0;
				_silenceDesChutes += dt;
				if (_silenceDesChutes > 2500f)
				{
					_sonDesChutes.Stop();
					_sonDesChutes = null;
				}
			}
		}
		catch (Exception exception)
		{
			PanneDesChutes(exception);
		}
	}

	private static void CouperSonDesChutes()
	{
		_sonDesChutes?.Stop();
		_sonDesChutes = null;
		_niveauDesChutes = 0f;
	}

	private static void SuivreLaCasse(Analyse a)
	{
		if (a.TexSolLibre == null || a.OmbresEnCours)
		{
			return;
		}
		int num = Casse.SignatureDesTombes(a.Carte);
		long milliseconds = Timing.Global.Milliseconds;
		if (num == a.SignatureCasse || milliseconds - a.OmbresRefaitesA < 1500)
		{
			return;
		}
		try
		{
			RefaireLesOmbres(a, num, milliseconds);
		}
		catch (Exception exception)
		{
			a.OmbresEnCours = false;
			a.SignatureCasse = num;
			ApplicationContext.Context.Value?.Logger.LogWarning(exception, "Ombres de la carte {Carte} : casse non suivie", a.Carte.Id);
		}
	}

	private static void RefaireLesOmbres(Analyse a, int signature, long maintenant)
	{
		MapInstance carte = a.Carte;
		if (!carte.PrepareLocalMapTextures() || carte.Autotiles?.Layers == null)
		{
			return;
		}
		(HashSet<(string, int, int)>, List<Casse.DessinTombe>) tuple = Casse.DessinsDesTombes(carte);
		HashSet<(string, int, int)> item = tuple.Item1;
		List<Casse.DessinTombe> dessins = tuple.Item2;
		int tw = Options.Instance.Map.TileWidth;
		int th = Options.Instance.Map.TileHeight;
		int mapWidth = Options.Instance.Map.MapWidth;
		int mapHeight = Options.Instance.Map.MapHeight;
		List<string> lowerLayers = Options.Instance.Map.Layers.LowerLayers;
		List<string> list = new List<string>();
		if (lowerLayers.Count > 1)
		{
			list.Add(lowerLayers[lowerLayers.Count - 1]);
		}
		list.AddRange(Options.Instance.Map.Layers.MiddleLayers);
		list.AddRange(Options.Instance.Map.Layers.UpperLayers);
		bool[] array = TuilesDeSol(carte, mapWidth, mapHeight);
		List<(IGameTexture Planche, int Sx, int Sy, int Dx, int Dy, bool Debout, bool Sol)> aLire = new List<(IGameTexture, int, int, int, int, bool, bool)>();
		foreach (string item11 in list)
		{
			int num;
			if (lowerLayers.Count > 1)
			{
				num = ((item11 == lowerLayers[lowerLayers.Count - 1]) ? 1 : 0);
			}
			else
			{
				num = 0;
			}
			bool flag = (byte)num != 0;
			if (!carte.Layers.TryGetValue(item11, out var value) || !carte.Autotiles.Layers.TryGetValue(item11, out var value2))
			{
				continue;
			}
			for (int i = 0; i < mapWidth; i++)
			{
				for (int j = 0; j < mapHeight; j++)
				{
					Tile tile = value[i, j];
					if (tile.TilesetTexture is IGameTexture gameTexture && tile.X >= 0 && tile.Y >= 0 && tile.X * tw < gameTexture.Width && tile.Y * th < gameTexture.Height && !(gameTexture.AtlasReference != null) && value2[i, j].RenderState == 1 && !item.Contains((item11, i, j)))
					{
						aLire.Add((gameTexture, tile.X * tw, tile.Y * th, i * tw, j * th, !TuileDeTerrain(gameTexture, tile.X, tile.Y), flag && array != null && array[i + j * mapWidth]));
					}
				}
			}
		}
		Analyse calcul = new Analyse
		{
			Carte = carte,
			L = a.L,
			H = a.H,
			Eau = a.Eau
		};
		a.OmbresEnCours = true;
		Task.Run(delegate
		{
			try
			{
				List<(byte[], int, int, int, int, int, int, int, int)> list2 = new List<(byte[], int, int, int, int, int, int, int, int)>(aLire.Count + dessins.Count);
				List<(byte[], int, int, int, int, int, int, int, int)> list3 = new List<(byte[], int, int, int, int, int, int, int, int)>(aLire.Count + dessins.Count);
				List<(byte[], int, int, int, int, int, int, int, int)> list4 = new List<(byte[], int, int, int, int, int, int, int, int)>();
				Dictionary<IGameTexture, (byte[] Rgba, int L, int H)?> lues = new Dictionary<IGameTexture, (byte[], int, int)?>();
				foreach (var item12 in aLire)
				{
					IGameTexture item2 = item12.Planche;
					int item3 = item12.Sx;
					int item4 = item12.Sy;
					int item5 = item12.Dx;
					int item6 = item12.Dy;
					bool item7 = item12.Debout;
					bool item8 = item12.Sol;
					(byte[], int, int)? tuple2 = Pixels(item2, lues);
					if (tuple2.HasValue)
					{
						(byte[], int, int) valueOrDefault = tuple2.GetValueOrDefault();
						(byte[], int, int, int, int, int, int, int, int) item9 = (valueOrDefault.Item1, valueOrDefault.Item2, valueOrDefault.Item3, item3, item4, tw, th, item5, item6);
						if (item8)
						{
							list4.Add(item9);
						}
						else
						{
							list3.Add(item9);
						}
						if (item7)
						{
							list2.Add(item9);
						}
					}
				}
				foreach (Casse.DessinTombe item13 in dessins)
				{
					(byte[], int, int)? tuple3 = ((byte[], int, int)?)((item13.Rgba != null) ? new(byte[], int, int)?((item13.Rgba, item13.L, item13.H)) : ((item13.Image != null) ? ((ValueType)Pixels(item13.Image, lues)) : ((ValueType)((byte[], int, int)?)null)));
					if (tuple3.HasValue)
					{
						(byte[], int, int) valueOrDefault2 = tuple3.GetValueOrDefault();
						(byte[], int, int, int, int, int, int, int, int) item10 = (valueOrDefault2.Item1, valueOrDefault2.Item2, valueOrDefault2.Item3, item13.Sx, item13.Sy, item13.W, item13.Hh, item13.Dx, item13.Dy);
						list3.Add(item10);
						list2.Add(item10);
					}
				}
				ConstruireOmbres(calcul, list2, list3, list4);
			}
			catch (Exception exception)
			{
				ApplicationContext.Context.Value?.Logger.LogWarning(exception, "Ombres de la carte {Carte} : casse non suivie", carte.Id);
				calcul.RgbaSolLibre = null;
				calcul.RgbaOmbres = null;
			}
			OmbresRefaites.Enqueue((a, calcul, signature));
		});
		(byte[] Rgba, int L, int H)? Pixels(IGameTexture texture, Dictionary<IGameTexture, (byte[] Rgba, int L, int H)?> lues)
		{
			if (!lues.TryGetValue(texture, out (byte[], int, int)? value3))
			{
				value3 = PixelsDuFichier(texture, maintenant);
				lues[texture] = value3;
			}
			return value3;
		}
	}

	private static void PoserLesOmbresRefaites()
	{
		(Analyse, Analyse, int) result;
		while (OmbresRefaites.TryDequeue(out result))
		{
			(Analyse, Analyse, int) tuple = result;
			Analyse item = tuple.Item1;
			Analyse item2 = tuple.Item2;
			int item3 = tuple.Item3;
			item.OmbresEnCours = false;
			item.OmbresRefaitesA = Timing.Global.Milliseconds;
			item.SignatureCasse = item3;
			if (item2.RgbaSolLibre != null && Analyses.TryGetValue(item.Carte.Id, out Analyse value) && value == item && Graphics.Renderer is MonoRenderer monoRenderer)
			{
				string text = $"meteo/{item.Carte.Id}/casse/{++_ombresRefaitesNumero}";
				IGameTexture texSolLibre = monoRenderer.CreerTexturePixels(text + "/sol-libre", item2.OmbresL, item2.OmbresH, item2.RgbaSolLibre);
				IGameTexture texOmbres = ((item2.RgbaOmbres != null) ? monoRenderer.CreerTexturePixels(text + "/ombres", item2.OmbresL, item2.OmbresH, item2.RgbaOmbres) : null);
				item.TexSolLibre?.Dispose();
				item.TexOmbres?.Dispose();
				item.TexSolLibre = texSolLibre;
				item.TexOmbres = texOmbres;
				item.OmbresL = item2.OmbresL;
				item.OmbresH = item2.OmbresH;
			}
		}
	}

	private static Reglages R(float nuages = 0f, float pluie = 0f, float orage = 0f, float brume = 0f, float brumeEau = 0f, float sable = 0f, float vent = 0f, float chaleur = 0f, float rayons = 0f, float neige = 0f, float froid = 0f)
	{
		return new Reglages
		{
			Nuages = nuages,
			Pluie = pluie,
			Orage = orage,
			Brume = brume,
			BrumeEau = brumeEau,
			Sable = sable,
			Vent = vent,
			Chaleur = chaleur,
			Rayons = rayons,
			Neige = neige,
			Froid = froid
		};
	}

	private static Climat ClimatDe(GenreZone genre)
	{
		return genre switch
		{
			GenreZone.Desert => Climat.Desert, 
			GenreZone.Interieur => Climat.Interieur, 
			_ => Climat.Dehors, 
		};
	}

	private static string TeinteDuLieu(GenreZone genre, string temps)
	{
		switch (genre)
		{
		case GenreZone.Ile:
			if (!(temps == "brouillard"))
			{
				break;
			}
			return "brumemer";
		case GenreZone.Foret:
			if (!(temps == "soleil"))
			{
				if (!(temps == "nuageux"))
				{
					break;
				}
				return "couvert";
			}
			return "eclaircie";
		}
		return temps;
	}

	private static bool Pluvieuse(GenreZone genre)
	{
		return ClimatDe(genre) == Climat.Dehors;
	}

	private static void Relire()
	{
		long milliseconds = Timing.Global.Milliseconds;
		if (milliseconds < _verificationFichier)
		{
			return;
		}
		_verificationFichier = milliseconds + 2000;
		string text = Path.Combine(ClientConfiguration.ResourcesDirectory, "meteo.json");
		try
		{
			DateTime dateTime = (Coffre.Existe(text) ? Coffre.DateUtc(text) : default(DateTime));
			if (dateTime == _dateFichier)
			{
				return;
			}
			_dateFichier = dateTime;
			ZonesCartes.Clear();
			VitresCartes.Clear();
			if (dateTime == default(DateTime))
			{
				return;
			}
			JObject jObject = JObject.Parse(Coffre.LireTexte(text));
			_actif = jObject.Value<bool?>("actif") ?? true;
			_minutesParMeteo = Math.Clamp(jObject.Value<float?>("minutesParMeteo") ?? 8f, 1f, 240f);
			_secondesTransition = Math.Clamp(jObject.Value<float?>("secondesDeTransition") ?? 75f, 5f, _minutesParMeteo * 30f);
			string key;
			JToken value;
			if (jObject["cartes"] is JObject jObject2)
			{
				foreach (KeyValuePair<string, JToken> item in jObject2)
				{
					item.Deconstruct(out key, out value);
					string input = key;
					JToken jToken = value;
					if (Guid.TryParse(input, out var result) && Enum.TryParse<GenreZone>(jToken?.Value<string>(), ignoreCase: true, out var result2))
					{
						ZonesCartes[result] = result2;
					}
				}
			}
			if (!(jObject["vitres"] is JObject jObject3))
			{
				return;
			}
			foreach (KeyValuePair<string, JToken> item2 in jObject3)
			{
				item2.Deconstruct(out key, out value);
				string input2 = key;
				JToken jToken2 = value;
				if (!Guid.TryParse(input2, out var result3) || !(jToken2 is JArray source))
				{
					continue;
				}
				List<Vitre> list = new List<Vitre>();
				foreach (JArray item3 in source.OfType<JArray>())
				{
					if (item3.Count >= 4)
					{
						list.Add(new Vitre(item3[0].Value<float>(), item3[1].Value<float>(), item3[2].Value<float>(), item3[3].Value<float>(), (item3.Count > 4) ? item3[4].Value<float>() : 1f));
					}
				}
				VitresCartes[result3] = list;
			}
		}
		catch (Exception exception)
		{
			ApplicationContext.Context.Value?.Logger.LogWarning(exception, "Lecture de {Fichier} impossible", text);
		}
	}

	internal static GenreZone ZoneDe(MapInstance carte)
	{
		Relire();
		if (EstAuDela(carte))
		{
			return GenreZone.Interieur;
		}
		if (!ZonesCartes.TryGetValue(carte.Id, out var value))
		{
			if (!carte.IsIndoors)
			{
				return GenreZone.Tempere;
			}
			return GenreZone.Interieur;
		}
		return value;
	}

	private static void RelireMonde()
	{
		long milliseconds = Timing.Global.Milliseconds;
		if (milliseconds < _verificationMonde)
		{
			return;
		}
		_verificationMonde = milliseconds + 5000;
		string text = Path.Combine(ClientConfiguration.ResourcesDirectory, "carte_monde.json");
		try
		{
			DateTime dateTime = (Coffre.Existe(text) ? Coffre.DateUtc(text) : default(DateTime));
			if (dateTime == _dateMonde)
			{
				return;
			}
			_dateMonde = dateTime;
			PaysDesCartes.Clear();
			GenreDesPays.Clear();
			if (dateTime == default(DateTime))
			{
				return;
			}
			JObject jObject = JObject.Parse(Coffre.LireTexte(text));
			if (!(jObject["cartes"] is JArray source))
			{
				return;
			}
			foreach (JObject item in source.OfType<JObject>())
			{
				string value = item.Value<string>("zone");
				if (Guid.TryParse(item.Value<string>("id"), out var result) && !string.IsNullOrWhiteSpace(value))
				{
					PaysDesCartes[result] = value;
				}
			}
			Dictionary<string, Dictionary<GenreZone, int>> dictionary = new Dictionary<string, Dictionary<GenreZone, int>>(StringComparer.OrdinalIgnoreCase);
			string key2;
			foreach (KeyValuePair<Guid, string> paysDesCarte in PaysDesCartes)
			{
				paysDesCarte.Deconstruct(out var key, out key2);
				Guid key3 = key;
				string key4 = key2;
				if (ZonesCartes.TryGetValue(key3, out var value2) && value2 != GenreZone.Interieur)
				{
					if (!dictionary.TryGetValue(key4, out var value3))
					{
						value3 = (dictionary[key4] = new Dictionary<GenreZone, int>());
					}
					value3[value2] = value3.GetValueOrDefault(value2) + 1;
				}
			}
			foreach (KeyValuePair<string, Dictionary<GenreZone, int>> item2 in dictionary)
			{
				item2.Deconstruct(out key2, out var value4);
				string key5 = key2;
				Dictionary<GenreZone, int> source2 = value4;
				GenreDesPays[key5] = (from v in source2
					orderby v.Value descending, (int)v.Key
					select v).First().Key;
			}
		}
		catch (Exception exception)
		{
			ApplicationContext.Context.Value?.Logger.LogWarning(exception, "Lecture de {Fichier} impossible", text);
		}
	}

	internal static GenreZone GenreDuCiel(MapInstance carte)
	{
		GenreZone genreZone = ZoneDe(carte);
		if (genreZone == GenreZone.Interieur)
		{
			return genreZone;
		}
		RelireMonde();
		if (!PaysDesCartes.TryGetValue(carte.Id, out string value) || !GenreDesPays.TryGetValue(value, out var value2))
		{
			return genreZone;
		}
		return value2;
	}

	internal static IReadOnlyList<Vitre> VitresDe(MapInstance carte)
	{
		if (!VitresCartes.TryGetValue(carte.Id, out List<Vitre> value))
		{
			return new List<Vitre>();
		}
		return value;
	}

	private static ulong Melanger(ulong x)
	{
		x ^= x >> 33;
		x *= 18397679294719823053uL;
		x ^= x >> 33;
		x *= 14181476777654086739uL;
		x ^= x >> 33;
		return x;
	}

	private static Ciel CielDuCreneau(long creneau)
	{
		long num = creneau;
		for (int i = 0; i < 24; i++)
		{
			if (Melanger((ulong)(num * -2960836687051489901L + 11)) % 100 < 50)
			{
				break;
			}
			num--;
		}
		int num2 = (int)(Melanger((ulong)(num * -7046029254386353131L + 7)) % (ulong)PoidsCiel.Sum());
		for (int j = 0; j < PoidsCiel.Length; j++)
		{
			if (num2 < PoidsCiel[j])
			{
				return (Ciel)j;
			}
			num2 -= PoidsCiel[j];
		}
		return Ciel.Beau;
	}

	private static Temps TempsDuCreneau(GenreZone genre, long creneau)
	{
		return TousLesTemps[TeinteDuLieu(genre, ParClimat[ClimatDe(genre)][(uint)CielDuCreneau(creneau)])];
	}

	private static Reglages CibleZone(GenreZone genre, bool nommer)
	{
		long num = (long)(_minutesParMeteo * 60000f);
		long maintenantPartage = MaintenantPartage;
		long num2 = maintenantPartage / num;
		long num3 = maintenantPartage % num;
		Temps temps = TempsDuCreneau(genre, num2);
		Temps temps2 = TempsDuCreneau(genre, num2 + 1);
		float num4 = _secondesTransition * 1000f;
		float num5 = Math.Clamp(((float)num3 - ((float)num - num4)) / num4, 0f, 1f);
		num5 = num5 * num5 * (3f - 2f * num5);
		if (nommer)
		{
			Temps temps3 = ((num5 < 0.5f) ? temps : temps2);
			_cleTemps = temps3.Cle;
			_nomTemps = temps3.Affiche;
			int i;
			for (i = 1; i < 24 && TempsDuCreneau(genre, num2 + i).Cle == temps.Cle; i++)
			{
			}
			_nomSuivant = TempsDuCreneau(genre, num2 + i).Affiche;
			_secondesAvantSuivant = (float)((num2 + i) * num - maintenantPartage) / 1000f;
		}
		return Reglages.Melange(temps.R, temps2.R, num5);
	}

	public static void NouvelleImage(TimeSpan duree)
	{
		if (_enPanne)
		{
			return;
		}
		try
		{
			Relire();
			float num = (float)Math.Clamp(duree.TotalMilliseconds, 0.0, 100.0);
			_t += num;
			MapInstance mapInstance = Globals.Me?.MapInstance as MapInstance;
			_zone = ((mapInstance == null) ? GenreZone.Interieur : ZoneDe(mapInstance));
			GenreZone genreZone = ((mapInstance == null) ? GenreZone.Interieur : GenreDuCiel(mapInstance));
			Reglages cible;
			if (!_actif || genreZone == GenreZone.Interieur)
			{
				cible = default(Reglages);
				_cleTemps = "calme";
				_nomTemps = TousLesTemps["calme"].Affiche;
				_nomSuivant = "";
			}
			else
			{
				cible = CibleZone(genreZone, nommer: true);
			}
			Reglages r = CibleZone(GenreZone.Tempere, nommer: false);
			Reglages r2 = CibleZone(GenreZone.Montagne, nommer: false);
			float heure = Heure;
			float num2 = 0.62f + 0.38f * Math.Max(Cloche(heure, 5.5f, 2f), NuitDe(heure));
			cible.Brume *= num2;
			cible.BrumeEau *= num2;
			if (!_initialise)
			{
				_p = cible;
				_mouille = EquilibreMouille(in r);
				_enneigement = EquilibreNeige(in r2);
				_initialise = true;
			}
			else
			{
				_p.Approcher(in cible, 1f - MathF.Exp((0f - num) / 3500f));
			}
			if (r.Pluie > 0.05f)
			{
				_mouille = Math.Min(1f, _mouille + num / 40000f * r.Pluie);
			}
			else
			{
				_mouille = Math.Max(0f, _mouille - num / 180000f);
			}
			if (r2.Neige > 0.08f)
			{
				_enneigement = Math.Min(1f, _enneigement + num / 90000f * r2.Neige);
			}
			else if (_enneigement > EquilibreNeige(in r2))
			{
				_enneigement = Math.Max(0f, _enneigement - num / 900000f);
			}
			using (ChronoImage.Etape("meteo : particules et analyses"))
			{
				MajParticules(num, mapInstance);
			}
			using (ChronoImage.Etape("meteo : eau"))
			{
				MajEau(num);
			}
			using (ChronoImage.Etape("meteo : orage"))
			{
				MajOrage(num);
			}
			using (ChronoImage.Etape("meteo : sons"))
			{
				MajSons(num);
			}
			using (ChronoImage.Etape("meteo : cascades"))
			{
				MajSonDesChutes(num);
			}
		}
		catch (Exception exception)
		{
			Panne(exception);
		}
	}

	public static void HorsDuJeu()
	{
		if (Globals.GameState != GameStates.InGame)
		{
			CouperSons();
		}
	}

	private static float EquilibreMouille(in Reglages r)
	{
		if (!(r.Pluie > 0.05f))
		{
			return 0f;
		}
		return 0.55f + 0.45f * r.Pluie;
	}

	private static float EquilibreNeige(in Reglages r)
	{
		if (!(r.Neige > 0.05f))
		{
			if (!(r.Froid > 0.5f))
			{
				return 0f;
			}
			return 0.45f;
		}
		return 0.8f;
	}

	internal static float Cloche(float heure, float centre, float largeur)
	{
		float num = Math.Abs(heure - centre);
		num = Math.Min(num, 24f - num);
		return Math.Clamp(1f - num / largeur, 0f, 1f);
	}

	private static float NuitDe(float heure)
	{
		return Math.Clamp((Math.Abs(heure - 13f) - 7.5f) / 1.5f, 0f, 1f);
	}

	internal static void Panne(Exception exception)
	{
		if (!_enPanne)
		{
			_enPanne = true;
			try
			{
				CouperSons();
				ViderParticules();
			}
			catch
			{
			}
			ApplicationContext.Context.Value?.Logger.LogError(exception, "Meteo coupee apres un incident");
		}
	}

	internal static int NiveauVentAuto(MapInstance carte)
	{
		if (_enPanne || ZoneDe(carte) == GenreZone.Interieur)
		{
			return (!carte.IsIndoors) ? 1 : 0;
		}
		if (!(_p.Vent > 0.62f))
		{
			return 1;
		}
		return 2;
	}

	internal static DonneesDesert? DesertDe(MapInstance carte)
	{
		if (!_enPanne)
		{
			return AnalyseDe(carte)?.Desert;
		}
		return null;
	}

	internal static (MapInstance Carte, DonneesDesert Desert)? DesertEn(float x, float y)
	{
		int num = Options.Instance.Map.TileWidth * Options.Instance.Map.MapWidth;
		int num2 = Options.Instance.Map.TileHeight * Options.Instance.Map.MapHeight;
		foreach (MapInstance visible in _visibles)
		{
			if (x < (float)visible.X || y < (float)visible.Y || x >= (float)(visible.X + num) || y >= (float)(visible.Y + num2))
			{
				continue;
			}
			(MapInstance, DonneesDesert)? result;
			if (Analyses.TryGetValue(visible.Id, out Analyse value) && value.Carte == visible)
			{
				DonneesDesert desert = value.Desert;
				if (desert != null)
				{
					result = (visible, desert);
					goto IL_00c5;
				}
			}
			result = null;
			goto IL_00c5;
			IL_00c5:
			return result;
		}
		return null;
	}

	internal static int BlocDesert(DonneesDesert desert, MapInstance carte, float x, float y)
	{
		return (int)(y - (float)carte.Y) / 4 * desert.Colonnes + (int)(x - (float)carte.X) / 4;
	}

	internal static void OnduleSol(float x, float y, float largeur, float hauteur, float force)
	{
		if (_enPanne || force <= 0.02f)
		{
			return;
		}
		float num = (float)_t;
		foreach (MapInstance visible in _visibles)
		{
			if (!Analyses.TryGetValue(visible.Id, out Analyse value) || value.Carte != visible)
			{
				continue;
			}
			IGameTexture texTerre = value.TexTerre;
			if (texTerre == null)
			{
				continue;
			}
			int num2 = (int)Math.Max(0f, x - (float)visible.X);
			int num3 = (int)Math.Min(value.L, x + largeur - (float)visible.X);
			int num4 = (int)Math.Max(0f, y - (float)visible.Y);
			int num5 = (int)Math.Min(value.H, y + hauteur - (float)visible.Y);
			for (int i = num4; i < num5; i += 2)
			{
				float num6 = MathF.Round(MathF.Sin((float)i * 0.7f + num * 0.011f) * 1.4f * force);
				if (num6 != 0f && num3 > num2)
				{
					Graphics.DrawGameTexture(texTerre, new FloatRect(num2, i, num3 - num2, 2f), new FloatRect((float)(visible.X + num2) + num6, visible.Y + i, num3 - num2, 2f), Color.White);
				}
			}
		}
	}

	private static DonneesDesert ConstruireDesert(Analyse a, List<(byte[] Rgba, int PL, int PH, int Sx, int Sy, int W, int Hh, int Dx, int Dy)> posesBas, List<(byte[] Rgba, int PL, int PH, int Sx, int Sy, int W, int Hh, int Dx, int Dy)> posesHaut)
	{
		int l = a.L;
		int h = a.H;
		int num = 4;
		int num2 = (l + num - 1) / num;
		int num3 = (h + num - 1) / num;
		byte[] px = new byte[l * h * 4];
		ComposerPoses(px, posesBas, l, h);
		ComposerPoses(px, posesHaut, l, h);
		DonneesDesert donneesDesert = new DonneesDesert()
		{
			Colonnes = num2,
			Luma = new byte[num2 * num3],
			Obstacles = new byte[num2 * num3],
		};
		DonneesDesert donneesDesert2 = donneesDesert;
		bool[] array = new bool[num2 * num3];
		List<int> list = new List<int>();
		List<int> list2 = new List<int>();
		for (int i = 0; i < num3; i++)
		{
			for (int j = 0; j < num2; j++)
			{
				int num4 = 0;
				int num5 = 0;
				float num6 = 0f;
				for (int k = i * num; k < Math.Min(h, (i + 1) * num); k++)
				{
					for (int m = j * num; m < Math.Min(l, (j + 1) * num); m++)
					{
						int i2 = k * l + m;
						num5++;
						num6 += Luma(i2);
						if (!Sable(i2))
						{
							num4++;
						}
					}
				}
				int num7 = i * num2 + j;
				donneesDesert2.Luma[num7] = (byte)Math.Clamp(num6 / (float)Math.Max(1, num5), 0f, 255f);
				donneesDesert2.Obstacles[num7] = ((num4 >= 6) ? ((byte)1) : ((byte)0));
				array[num7] = num4 <= 2;
				if (array[num7])
				{
					int num8 = j * num + 2;
					int num9 = i * num + 2;
					if ((j + i) % 2 == 0 && list.Count < 8000)
					{
						list.Add(num8 | (num9 << 16));
					}
					if (donneesDesert2.Luma[num7] > 212 && (j * 7 + i * 3) % 3 == 0 && list2.Count < 3000)
					{
						list2.Add((num8 + (j * 5 + i) % 3 - 1) | (num9 + (i * 3 + j) % 3 - 1 << 16));
					}
				}
			}
		}
		donneesDesert2.Sables = list.ToArray();
		donneesDesert2.Clairs = list2.ToArray();
		List<(short, short)> list3 = new List<(short, short)>();
		for (int n = 1; n < num2; n++)
		{
			int num10 = 0;
			for (int num11 = 0; num11 <= num3; num11++)
			{
				if (num11 < num3 && array[num11 * num2 + n] && donneesDesert2.Obstacles[num11 * num2 + n - 1] == 1)
				{
					num10++;
					continue;
				}
				if (num10 >= 5)
				{
					for (int num12 = num11 - num10; num12 < num11; num12 += 2)
					{
						list3.Add(((short)(n * num), (short)(num12 * num + 2)));
					}
				}
				num10 = 0;
			}
		}
		donneesDesert2.Cretes = list3.Take(400).ToArray();
		bool[] array2 = new bool[l * h];
		for (int num13 = 0; num13 < l * h; num13++)
		{
			if (px[num13 * 4 + 3] != 0)
			{
				int val = px[num13 * 4];
				int val2 = px[num13 * 4 + 1];
				int val3 = px[num13 * 4 + 2];
				array2[num13] = Luma(num13) > 232f && Math.Max(val, Math.Max(val2, val3)) - Math.Min(val, Math.Min(val2, val3)) < 22;
			}
		}
		bool[] array3 = new bool[l * h];
		Stack<int> stack = new Stack<int>();
		List<(int, int, int)> list4 = new List<(int, int, int)>();
		for (int num14 = 0; num14 < l * h; num14++)
		{
			if (!array2[num14] || array3[num14])
			{
				continue;
			}
			array3[num14] = true;
			stack.Push(num14);
			long num15 = 0L;
			long num16 = 0L;
			int num17 = 0;
			while (stack.Count > 0)
			{
				int num18 = stack.Pop();
				int num19 = num18 % l;
				int num20 = num18 / l;
				num17++;
				num15 += num19;
				num16 += num20;
				if (num19 > 0 && array2[num18 - 1] && !array3[num18 - 1])
				{
					array3[num18 - 1] = true;
					stack.Push(num18 - 1);
				}
				if (num19 < l - 1 && array2[num18 + 1] && !array3[num18 + 1])
				{
					array3[num18 + 1] = true;
					stack.Push(num18 + 1);
				}
				if (num20 > 0 && array2[num18 - l] && !array3[num18 - l])
				{
					array3[num18 - l] = true;
					stack.Push(num18 - l);
				}
				if (num20 < h - 1 && array2[num18 + l] && !array3[num18 + l])
				{
					array3[num18 + l] = true;
					stack.Push(num18 + l);
				}
			}
			if (num17 >= 8 && num17 <= 2000)
			{
				list4.Add(((int)(num15 / num17), (int)(num16 / num17), num17));
			}
		}
		List<(short, short)> list5 = new List<(short, short)>();
		foreach (var tache in list4.OrderByDescending<(int, int, int), int>(((int X, int Y, int N) t) => t.N))
		{
			if (list5.Count >= 3 || list5.Any<(short, short)>(((short X, short Y) c) => Math.Abs(c.X - tache.Item1) < 160 && Math.Abs(c.Y - tache.Item2) < 160))
			{
				continue;
			}
			List<(int, int, int)> list6 = list4.Where<(int, int, int)>(((int X, int Y, int N) t) => Math.Abs(t.X - tache.Item1) < 48 && Math.Abs(t.Y - tache.Item2) < 48).ToList();
			if (tache.Item3 >= 45 || list6.Count >= 3)
			{
				list5.Add(((short)list6.Average<(int, int, int)>(((int X, int Y, int N) t) => t.X), (short)list6.Average<(int, int, int)>(((int X, int Y, int N) t) => t.Y)));
			}
		}
		donneesDesert2.Carcasses = list5.ToArray();
		bool[] array4 = new bool[num2 * num3];
		List<(short, short, short, short)> list7 = new List<(short, short, short, short)>();
		Random random = new Random(a.Carte.Id.GetHashCode());
		for (int num21 = 0; num21 < num2 * num3; num21++)
		{
			if (list7.Count >= 14)
			{
				break;
			}
			if (donneesDesert2.Obstacles[num21] == 0 || array4[num21])
			{
				continue;
			}
			array4[num21] = true;
			stack.Push(num21);
			int num22 = 0;
			int num23 = int.MaxValue;
			int num24 = -1;
			int num25 = -1;
			bool flag = false;
			while (stack.Count > 0)
			{
				int num26 = stack.Pop();
				int num27 = num26 % num2;
				int num28 = num26 / num2;
				num22++;
				num23 = Math.Min(num23, num27);
				num24 = Math.Max(num24, num27);
				num25 = Math.Max(num25, num28);
				if (num22 > 80)
				{
					flag = true;
				}
				if (num27 > 0 && donneesDesert2.Obstacles[num26 - 1] == 1 && !array4[num26 - 1])
				{
					array4[num26 - 1] = true;
					stack.Push(num26 - 1);
				}
				if (num27 < num2 - 1 && donneesDesert2.Obstacles[num26 + 1] == 1 && !array4[num26 + 1])
				{
					array4[num26 + 1] = true;
					stack.Push(num26 + 1);
				}
				if (num28 > 0 && donneesDesert2.Obstacles[num26 - num2] == 1 && !array4[num26 - num2])
				{
					array4[num26 - num2] = true;
					stack.Push(num26 - num2);
				}
				if (num28 < num3 - 1 && donneesDesert2.Obstacles[num26 + num2] == 1 && !array4[num26 + num2])
				{
					array4[num26 + num2] = true;
					stack.Push(num26 + num2);
				}
			}
			if (flag || num22 < 3 || num22 > 60 || num25 + 1 >= num3)
			{
				continue;
			}
			int num29 = (num23 + num24 + 1) * num / 2;
			int num30 = (num25 + 1) * num;
			for (int num31 = 0; num31 < 12; num31++)
			{
				float x = (float)(random.NextDouble() * Math.PI);
				float num32 = 12f + (float)random.NextDouble() * 14f;
				float num33 = (float)num29 + MathF.Cos(x) * num32;
				float num34 = (float)num30 + MathF.Sin(x) * num32 * 0.6f;
				int num35 = (int)num33 / num;
				int num36 = (int)num34 / num;
				if (num35 >= 1 && num36 >= 1 && num35 < num2 - 1 && num36 < num3 - 1 && array[num36 * num2 + num35] && array[num36 * num2 + num35 - 1] && array[num36 * num2 + num35 + 1])
				{
					list7.Add(((short)num29, (short)(num30 - 2), (short)num33, (short)num34));
					break;
				}
			}
		}
		donneesDesert2.Cachettes = list7.ToArray();
		return donneesDesert2;
		float Luma(int num37)
		{
			return (float)(int)px[num37 * 4] * 0.3f + (float)(int)px[num37 * 4 + 1] * 0.55f + (float)(int)px[num37 * 4 + 2] * 0.15f;
		}
		bool Sable(int num37)
		{
			if (px[num37 * 4 + 3] == 0)
			{
				return false;
			}
			int num38 = px[num37 * 4];
			int num39 = px[num37 * 4 + 1];
			int num40 = px[num37 * 4 + 2];
			if (Luma(num37) > 150f && num38 >= num39 - 4 && num39 >= num40 - 4 && num38 - num40 < 95 && num38 - num39 < 42)
			{
				return num38 - num40 > 8;
			}
			return false;
		}
	}

	private static bool Pret()
	{
		if (!_enPanne && _actif && _initialise)
		{
			return Graphics.Renderer != null;
		}
		return false;
	}

	private static Color Couleur(int r, int g, int b, float a)
	{
		Teinte.A = (byte)Math.Clamp(a * 255f, 0f, 255f);
		Teinte.R = (byte)Math.Clamp(r, 0, 255);
		Teinte.G = (byte)Math.Clamp(g, 0, 255);
		Teinte.B = (byte)Math.Clamp(b, 0, 255);
		return Teinte;
	}

	private static void Carre(float x, float y, float l, float h, int r, int g, int b, float a, GameBlendModes mode = GameBlendModes.None)
	{
		if (!(a <= 0.004f) && !(l <= 0f) && !(h <= 0f))
		{
			IGameTexture gameTexture = Graphics.Renderer?.WhitePixel;
			if (gameTexture != null)
			{
				Graphics.DrawGameTexture(gameTexture, UnPixel, new FloatRect(x, y, l, h), Couleur(r, g, b, a), null, mode);
			}
		}
	}

	private static void Pt(float x, float y, int r, int g, int b, float a, GameBlendModes mode = GameBlendModes.None)
	{
		Carre(MathF.Floor(x), MathF.Floor(y), 1f, 1f, r, g, b, a, mode);
	}

	private static void PtL(float x, float y, int r, int g, int b, float a)
	{
		float eclairage = _etal.Eclairage;
		Carre(MathF.Floor(x), MathF.Floor(y), 1f, 1f, (int)((float)r * eclairage), (int)((float)g * eclairage), (int)((float)b * eclairage), a);
	}

	private static void Texture(IGameTexture texture, float x, float y, float l, float h, int r, int g, int b, float a, GameBlendModes mode = GameBlendModes.None, IGameRenderTexture? cible = null)
	{
		if (!(a <= 0.004f))
		{
			Graphics.DrawGameTexture(texture, new FloatRect(0f, 0f, texture.Width, texture.Height), new FloatRect(x, y, l, h), Couleur(r, g, b, a), cible, mode);
		}
	}

	private static bool Dedans(float x, float y, FloatRect vue, float marge = 2f)
	{
		if (x >= vue.Left - marge && x < vue.Right + marge && y >= vue.Top - marge)
		{
			return y < vue.Bottom + marge;
		}
		return false;
	}

	private static float Alea(ref uint etat)
	{
		etat = etat * 1664525 + 1013904223;
		return (float)etat / 4.2949673E+09f;
	}

	private static bool Voiles()
	{
		if (_texHalo != null)
		{
			return true;
		}
		byte[][] voilesPrets = _voilesPrets;
		if (voilesPrets != null && Graphics.Renderer is MonoRenderer monoRenderer)
		{
			_texNuages = monoRenderer.CreerTexturePixels("meteo/nuages", 512, 512, voilesPrets[0]);
			_texBrume = monoRenderer.CreerTexturePixels("meteo/brume", 512, 512, voilesPrets[1]);
			_texBrume2 = monoRenderer.CreerTexturePixels("meteo/brume2", 512, 512, voilesPrets[2]);
			_texSable = monoRenderer.CreerTexturePixels("meteo/sable", 512, 512, voilesPrets[3]);
			_texRayons = monoRenderer.CreerTexturePixels("meteo/rayons", 512, 1024, voilesPrets[4]);
			_texVignette = monoRenderer.CreerTexturePixels("meteo/vignette", 256, 160, voilesPrets[5]);
			_texHalo = monoRenderer.CreerTexturePixels("meteo/halo", 64, 64, voilesPrets[6]);
			_voilesPrets = null;
			return true;
		}
		if (!_voilesEnCours)
		{
			_voilesEnCours = true;
			Task.Run(delegate
			{
				try
				{
					byte[] array = new byte[2097152];
					(float, float)[] array2 = new(float, float)[5]
					{
						(40f, 30f),
						(140f, 46f),
						(240f, 24f),
						(330f, 40f),
						(440f, 28f)
					};
					for (int i = 0; i < 1024; i++)
					{
						for (int j = 0; j < 512; j++)
						{
							float num = (((float)j - (float)i * 0.5f) % 512f + 512f) % 512f;
							float num2 = 0f;
							(float, float)[] array3 = array2;
							for (int k = 0; k < array3.Length; k++)
							{
								(float, float) tuple = array3[k];
								float item = tuple.Item1;
								float item2 = tuple.Item2;
								float num3 = Math.Abs(num - item);
								num3 = Math.Min(num3, 512f - num3);
								num2 = Math.Max(num2, Math.Clamp(1f - num3 / (item2 / 2f), 0f, 1f));
							}
							int num4 = (i * 512 + j) * 4;
							array[num4] = byte.MaxValue;
							array[num4 + 1] = 236;
							array[num4 + 2] = 180;
							array[num4 + 3] = (byte)(num2 * num2 * (3f - 2f * num2) * 255f);
						}
					}
					byte[] array4 = new byte[163840];
					for (int l = 0; l < 160; l++)
					{
						for (int m = 0; m < 256; m++)
						{
							float num5 = ((float)m - 127.5f) / 128f;
							float num6 = ((float)l - 79.5f) / 80f;
							float num7 = MathF.Sqrt(num5 * num5 + num6 * num6);
							int num8 = (l * 256 + m) * 4;
							array4[num8] = 4;
							array4[num8 + 1] = 6;
							array4[num8 + 2] = 14;
							array4[num8 + 3] = (byte)(Lisse((num7 - 0.55f) / 0.5f) * 255f);
						}
					}
					byte[] array5 = new byte[16384];
					for (int n = 0; n < 64; n++)
					{
						for (int num9 = 0; num9 < 64; num9++)
						{
							float num10 = MathF.Sqrt(((float)num9 - 31.5f) * ((float)num9 - 31.5f) + ((float)n - 31.5f) * ((float)n - 31.5f)) / 32f;
							float num11 = Math.Clamp(1f - num10, 0f, 1f);
							int num12 = (n * 64 + num9) * 4;
							int num13 = num12 + 1;
							byte b;
							array5[num12 + 2] = (b = byte.MaxValue);
							array5[num12] = (array5[num13] = b);
							array5[num12 + 3] = (byte)(num11 * num11 * 255f);
						}
					}
					_voilesPrets = new byte[7][]
					{
						Voile(Bruit(512, 5, 4, 11), 512, 0.5f, 0.66f, 18, 26, 52),
						Voile(Bruit(512, 3, 4, 23), 512, 0.22f, 0.86f, 255, 255, 255),
						Voile(Bruit(512, 5, 3, 37), 512, 0.3f, 0.88f, 255, 255, 255),
						Voile(Bruit(512, 6, 4, 53), 512, 0.36f, 0.72f, 255, 255, 255),
						array,
						array4,
						array5
					};
				}
				catch (Exception exception)
				{
					ApplicationContext.Context.Value?.Logger.LogWarning(exception, "Voiles de la meteo impossibles");
				}
			});
		}
		return false;
		static byte[] Voile(float[] bruit, int taille, float bas, float haut, int r, int g, int b)
		{
			byte[] array = new byte[taille * taille * 4];
			for (int i = 0; i < bruit.Length; i++)
			{
				array[i * 4] = (byte)r;
				array[i * 4 + 1] = (byte)g;
				array[i * 4 + 2] = (byte)b;
				array[i * 4 + 3] = (byte)(Lisse((bruit[i] - bas) / (haut - bas)) * 255f);
			}
			return array;
		}
	}

	private static void Paves(IGameTexture texture, float echelleX, float echelleY, float ox, float oy, int r, int g, int b, float a, GameBlendModes mode, IGameRenderTexture? cible)
	{
		if (a <= 0.004f)
		{
			return;
		}
		FloatRect worldViewport = Graphics.WorldViewport;
		float num = (float)texture.Width * echelleX;
		float num2 = (float)texture.Height * echelleY;
		ox %= num;
		oy %= num2;
		float num3 = MathF.Floor((worldViewport.Left - ox) / num) * num + ox;
		float num4 = MathF.Floor((worldViewport.Top - oy) / num2) * num2 + oy;
		float num5 = ((cible == null) ? 0f : Graphics.CurrentView.Left);
		float num6 = ((cible == null) ? 0f : Graphics.CurrentView.Top);
		for (float num7 = num4; num7 < worldViewport.Bottom; num7 += num2)
		{
			for (float num8 = num3; num8 < worldViewport.Right; num8 += num)
			{
				Graphics.DrawGameTexture(texture, new FloatRect(0f, 0f, texture.Width, texture.Height), new FloatRect(MathF.Round(num8 - num5), MathF.Round(num7 - num6), num, num2), Couleur(r, g, b, a), cible, mode);
			}
		}
	}

	public static void DessinerSol()
	{
		if (!Pret())
		{
			return;
		}
		try
		{
			_etal = Calculer(Flash());
			if (_zone == GenreZone.Interieur)
			{
				return;
			}
			DessinerEau(Graphics.WorldViewport);
			DessinerOmbresPortees();
			DessinerFlaquesDeLumiere();
			DessinerChutesAuSol();
			foreach (MapInstance visible in _visibles)
			{
				Climat climat = ClimatDe(GenreDuCiel(visible));
				if (climat == Climat.Interieur || !Analyses.TryGetValue(visible.Id, out Analyse value) || value.Carte != visible || value.Eau.Length == 0)
				{
					continue;
				}
				if (climat == Climat.Desert)
				{
					DessinerChaleur(visible, value);
				}
				if (Pluvieuse(GenreDuCiel(visible)))
				{
					DessinerMouille(visible, value);
				}
				if (_enneigement > 0.01f)
				{
					IGameTexture gameTexture = NeigeDe(value);
					if (gameTexture != null)
					{
						Graphics.DrawGameTexture(gameTexture, new FloatRect(0f, 0f, value.L, value.H), new FloatRect(visible.X, visible.Y, value.L, value.H), Color.White);
					}
				}
			}
			if (_zone == GenreZone.Montagne)
			{
				for (int i = 0; i < _nbTraces; i++)
				{
					Pose pose = Traces[i];
					float num = 0.55f * (1f - pose.Age / 9000f);
					Carre(MathF.Floor(pose.X), MathF.Floor(pose.Y), 2f, 1f, 132, 146, 178, num);
					Pt(pose.X, pose.Y + 1f, 160, 172, 200, num * 0.6f);
				}
			}
		}
		catch (Exception exception)
		{
			Panne(exception);
		}
	}

	private static void DessinerChaleur(MapInstance carte, Analyse a)
	{
		float num = _p.Chaleur * _etal.Jour;
		if (num < 0.05f)
		{
			return;
		}
		IGameTexture texTerre = a.TexTerre;
		if (texTerre == null)
		{
			return;
		}
		FloatRect worldViewport = Graphics.WorldViewport;
		int num2 = (int)Math.Max(0f, worldViewport.Top - (float)carte.Y);
		int num3 = (int)Math.Min(a.H, worldViewport.Bottom - (float)carte.Y + 1f);
		int num4 = (int)Math.Max(0f, worldViewport.Left - (float)carte.X - 2f);
		int num5 = (int)Math.Min(a.L, worldViewport.Right - (float)carte.X + 2f);
		if (num5 <= num4)
		{
			return;
		}
		float num6 = (float)_t;
		for (int i = num2; i < num3; i++)
		{
			int num7 = carte.Y + i;
			float num8 = Lisse((MathF.Sin((float)num7 * 0.055f + num6 * 0.0024f) - 0.35f) / 0.65f);
			float num9 = MathF.Round(MathF.Sin((float)num7 * 0.5f + num6 * 0.009f) * 1.4f * num * num8);
			if (num9 != 0f)
			{
				Graphics.DrawGameTexture(texTerre, new FloatRect(num4, i, num5 - num4, 1f), new FloatRect((float)(carte.X + num4) + num9, num7, num5 - num4, 1f), Color.White);
			}
		}
	}

	private static void DessinerMouille(MapInstance carte, Analyse a)
	{
		float num = _p.Pluie * 0.3f + _p.Orage * 0.4f;
		if (num > 0.02f)
		{
			IGameTexture texEau = a.TexEau;
			if (texEau != null)
			{
				Texture(texEau, carte.X, carte.Y, a.L, a.H, 40, 55, 80, num * 0.55f * 0.75f);
			}
		}
		float num2 = CroissanceFlaques();
		if (num2 <= 0.02f)
		{
			return;
		}
		FloatRect worldViewport = Graphics.WorldViewport;
		int val = _zone switch
		{
			GenreZone.Ile => 7, 
			GenreZone.Desert => 5, 
			_ => 12, 
		};
		for (int i = 0; i < Math.Min(val, a.Flaques.Length); i++)
		{
			Flaque flaque = a.Flaques[i];
			float num3 = carte.X + flaque.Cx;
			float num4 = carte.Y + flaque.Cy;
			if (!Dedans(num3, num4, worldViewport, 20f))
			{
				continue;
			}
			(sbyte, sbyte, float)[] pixels = flaque.Pixels;
			for (int j = 0; j < pixels.Length; j++)
			{
				(sbyte, sbyte, float) tuple = pixels[j];
				sbyte item = tuple.Item1;
				sbyte item2 = tuple.Item2;
				float item3 = tuple.Item3;
				float x = num3 + (float)item;
				float y = num4 + (float)item2;
				if (item3 > num2)
				{
					if (item3 < num2 + 0.4f)
					{
						Pt(x, y, 52, 44, 40, 0.2f);
					}
					continue;
				}
				bool flag = item3 > num2 - 0.26f;
				if (flag && item2 <= 0 && item <= 1)
				{
					Pt(x, y, 205, 216, 230, 0.85f);
					continue;
				}
				if (flag)
				{
					Pt(x, y, 58, 60, 70, 0.6f);
					continue;
				}
				bool flag2 = Math.Abs((float)item + (float)item2 * 2.2f + 2f) < 1.1f;
				float k = Math.Clamp((float)(item2 + 4) / 8f, 0f, 1f);
				if (flag2)
				{
					Pt(x, y, 168, 182, 204, 0.92f);
				}
				else
				{
					Pt(x, y, (int)Lerp(104f, 72f, k), (int)Lerp(118f, 82f, k), (int)Lerp(140f, 102f, k), 0.92f);
				}
			}
		}
	}

	public static void DessinerCiel()
	{
		if (!Pret() || _zone == GenreZone.Interieur || !Voiles())
		{
			return;
		}
		try
		{
			FloatRect worldViewport = Graphics.WorldViewport;
			float num = (float)_t;
			float num2 = 0.22500001f * Math.Clamp((_p.Nuages - 0.3f) / 0.15f, 0f, 1f) * Math.Clamp(1f - Math.Max(0f, _p.Nuages - 0.45f) / 0.55f, 0f, 1f) * _etal.Jour * (1f - _p.Pluie) * (1f - _p.Brume * 0.7f);
			if (num2 > 0.01f)
			{
				Paves(_texNuages, 2f, 2f, (0f - num) * (0.004f + 0.02f * _p.Vent), num * 0.0018f, 255, 255, 255, num2, GameBlendModes.None, null);
			}
			float num3 = ((_zone == GenreZone.Foret) ? (_p.Rayons * _etal.Soleil) : 0f);
			if (num3 > 0.01f)
			{
				float ox = MathF.Sin(num * 0.00021f) * 22f;
				float num4 = 0.75f + 0.25f * MathF.Sin(num * 0.0013f);
				Paves(_texRayons, 2f, 2f, ox, 0f, 255, 255, 255, 0.42f * num3 * num4, GameBlendModes.Add, null);
			}
			float brume = _p.Brume;
			float sable = _p.Sable;
			if (brume <= 0.01f && sable <= 0.01f)
			{
				return;
			}
			GameRenderer renderer = Graphics.Renderer;
			if (renderer == null)
			{
				return;
			}
			if (_toileBrume == null || _toileBrume.Width != renderer.ScreenWidth || _toileBrume.Height != renderer.ScreenHeight)
			{
				_toileBrume?.Dispose();
				_toileBrume = renderer.CreateRenderTexture(renderer.ScreenWidth, renderer.ScreenHeight);
			}
			IGameRenderTexture toileBrume = _toileBrume;
			toileBrume.Clear(Color.Transparent);
			if (brume > 0.01f)
			{
				Paves(_texBrume, 3f, 2f, (0f - num) * (0.006f + 0.02f * _p.Vent), num * 0.002f, 234, 239, 245, 0.26f * brume, GameBlendModes.None, toileBrume);
				Paves(_texBrume2, 2f, 1.5f, (0f - num) * (0.012f + 0.03f * _p.Vent), (0f - num) * 0.001f, 234, 239, 245, 0.16f * brume, GameBlendModes.None, toileBrume);
				if (_p.BrumeEau > 0.01f)
				{
					foreach (MapInstance visible in _visibles)
					{
						if (Analyses.TryGetValue(visible.Id, out Analyse value) && value.Carte == visible)
						{
							IGameTexture texEauFlou = value.TexEauFlou;
							if (texEauFlou != null)
							{
								Texture(texEauFlou, (float)visible.X - Graphics.CurrentView.Left, (float)visible.Y - Graphics.CurrentView.Top, value.L, value.H, 234, 239, 245, 0.3f * _p.BrumeEau, GameBlendModes.None, toileBrume);
							}
						}
					}
				}
			}
			if (sable > 0.01f)
			{
				Paves(_texSable, 3f, 1.2f, (0f - num) * (0.05f + 0.09f * _p.Vent), 0f, 216, 178, 122, 0.495f * sable, GameBlendModes.None, toileBrume);
				Paves(_texSable, 1.7f, 0.75f, (0f - num) * (0.11f + 0.12f * _p.Vent), num * 0.004f, 216, 178, 122, 0.33749998f * sable, GameBlendModes.None, toileBrume);
			}
			Player me = Globals.Me;
			if (me != null)
			{
				float num5 = ((brume >= sable) ? 112f : 54f);
				float a = ((brume >= sable) ? 0.75f : 0.45f);
				Texture(_texHalo, me.Center.X - num5 - Graphics.CurrentView.Left, me.Center.Y - num5 - Graphics.CurrentView.Top, num5 * 2f, num5 * 2f, 255, 255, 255, a, GameBlendModes.Cutout, toileBrume);
			}
			toileBrume.End();
			if (brume > 0.01f)
			{
				Carre(worldViewport.X, worldViewport.Y, worldViewport.Width, worldViewport.Height, 225, 232, 240, 0.07f * brume);
			}
			if (sable > 0.01f)
			{
				Carre(worldViewport.X, worldViewport.Y, worldViewport.Width, worldViewport.Height, 236, 196, 140, 0.075f * sable);
			}
			Graphics.DrawGameTexture(toileBrume, toileBrume.Bounds, worldViewport, Color.White, null, GameBlendModes.Alpha);
		}
		catch (Exception exception)
		{
			Panne(exception);
		}
	}

	public static void DessinerApresNuit()
	{
		if (!Pret() || _zone == GenreZone.Interieur)
		{
			return;
		}
		try
		{
			FloatRect worldViewport = Graphics.WorldViewport;
			Etalonnage e = _etal;
			_analysesVisibles.Clear();
			foreach (MapInstance visible in _visibles)
			{
				if (Analyses.TryGetValue(visible.Id, out Analyse value) && value.Carte == visible && value.Eau.Length != 0)
				{
					_analysesVisibles.Add((visible, value));
				}
			}
			Carre(worldViewport.X, worldViewport.Y, worldViewport.Width, worldViewport.Height, (int)e.VoileR, (int)e.VoileG, (int)e.VoileB, e.VoileA * 0.8f, GameBlendModes.Add);
			if (e.Desaturation > 0.02f)
			{
				int num = (int)((e.R * 0.3f + e.G * 0.55f + e.B * 0.15f) * 0.5f);
				Carre(worldViewport.X, worldViewport.Y, worldViewport.Width, worldViewport.Height, num, num, num, e.Desaturation * 0.18f);
			}
			Carre(worldViewport.X, worldViewport.Y, worldViewport.Width, worldViewport.Height, 255, 236, 200, 0.1f * _p.Chaleur * e.Jour, GameBlendModes.Add);
			DessinerFenetres(in e);
			DessinerMer(worldViewport);
			DessinerPluie(worldViewport);
			DessinerEauDessus(worldViewport);
			DessinerFlocons(worldViewport);
			DessinerVent(worldViewport);
			DessinerReflets(worldViewport, in e);
			DessinerLucioles(in e);
			DessinerChutesApresNuit(in e);
			DessinerEclair(worldViewport);
			float num2 = Flash();
			Carre(worldViewport.X, worldViewport.Y, worldViewport.Width, worldViewport.Height, 185, 205, 255, 0.2f * num2, GameBlendModes.Add);
		}
		catch (Exception exception)
		{
			Panne(exception);
		}
	}

	private static void DessinerFenetres(in Etalonnage e)
	{
		if (e.Lampes < 0.01f || _texHalo == null)
		{
			return;
		}
		foreach (MapInstance visible in _visibles)
		{
			int num = 0;
			foreach (Vitre item in VitresDe(visible))
			{
				if (Casse.TombeIci(visible, item.X, item.Y, item.Largeur, item.Hauteur))
				{
					num++;
					continue;
				}
				float num2 = Math.Clamp(e.Lampes * item.Force * (0.9f + 0.1f * MathF.Sin((float)(_t * 0.013) + (float)num * 1.9f)), 0f, 1f);
				float num3 = (float)visible.X + item.X;
				float num4 = (float)visible.Y + item.Y;
				Carre(num3, num4, item.Largeur, item.Hauteur, 255, 205, 122, 0.78f * num2);
				Carre(num3, num4, item.Largeur, item.Hauteur, 255, 170, 80, 0.22f * num2, GameBlendModes.Add);
				float num5 = 20f + 16f * _p.Brume + 8f * _p.Pluie;
				Texture(_texHalo, num3 + item.Largeur / 2f - num5, num4 + item.Hauteur / 2f - num5, num5 * 2f, num5 * 2f, 255, 175, 85, (0.3f + 0.15f * _p.Brume) * num2, GameBlendModes.Add);
				num++;
			}
			foreach (LightDescriptor light in visible.Lights)
			{
				float num6 = (float)(visible.X + light.TileX * Options.Instance.Map.TileWidth + light.OffsetX) + (float)Options.Instance.Map.TileWidth / 2f;
				float num7 = (float)(visible.Y + light.TileY * Options.Instance.Map.TileHeight + light.OffsetY) + (float)Options.Instance.Map.TileHeight / 2f;
				if (!Casse.TombeIci(visible, num6 - (float)visible.X, num7 - (float)visible.Y))
				{
					float num8 = Math.Max(8f, (float)light.Size * (0.28f + 0.2f * _p.Brume));
					Texture(_texHalo, num6 - num8, num7 - num8, num8 * 2f, num8 * 2f, light.Color.R, light.Color.G, light.Color.B, (0.22f + 0.12f * _p.Brume) * e.Lampes * (float)(int)light.Intensity / 255f, GameBlendModes.Add);
				}
			}
		}
	}

	private static void DessinerMer(FloatRect vue)
	{
		if (!Zones[_zone].Mer)
		{
			return;
		}
		for (int i = 0; i < _nbMoutons; i++)
		{
			Mouton mouton = Moutons[i];
			float num = MathF.Sin((float)Math.PI * mouton.Age / mouton.Vie) * 0.75f;
			float eclairage = _etal.Eclairage;
			Carre(MathF.Floor(mouton.X), MathF.Floor(mouton.Y), (int)mouton.Longueur, 1f, (int)(246f * eclairage), (int)(250f * eclairage), (int)(255f * eclairage), num);
			Carre(MathF.Floor(mouton.X) + 1f, MathF.Floor(mouton.Y) + 1f, Math.Max(1, mouton.Longueur - 2), 1f, (int)(230f * eclairage), (int)(240f * eclairage), (int)(250f * eclairage), num * 0.55f);
		}
		float num2 = _p.Orage * 0.9f + _p.Vent * 0.2f;
		if (num2 <= 0.12f)
		{
			return;
		}
		float num3 = (float)_t;
		foreach (var analysesVisible in _analysesVisibles)
		{
			MapInstance item = analysesVisible.Carte;
			Analyse item2 = analysesVisible.A;
			(short, short, sbyte, sbyte, float)[] rive = item2.Rive;
			for (int j = 0; j < rive.Length; j++)
			{
				(short, short, sbyte, sbyte, float) tuple = rive[j];
				float num4 = item.X + tuple.Item1;
				float num5 = item.Y + tuple.Item2;
				if (Dedans(num4, num5, vue, 6f))
				{
					float num6 = 0.5f + 0.5f * MathF.Sin(num3 * 0.0034f + tuple.Item5 * 0.6f + num4 * 0.045f + num5 * 0.03f);
					int num7 = (int)((1f + 3.2f * num6) * num2 * 1.4f);
					for (int k = 1; k <= num7; k++)
					{
						PtL(num4 + (float)(tuple.Item3 * k), num5 + (float)(tuple.Item4 * k), 246, 250, 255, 0.6f * (1f - (float)k / (float)(num7 + 1)));
					}
				}
			}
		}
	}

	private static void DessinerPluie(FloatRect vue)
	{
		float num = Pente();
		for (int i = 0; i < _nbAnneaux; i++)
		{
			Anneau anneau = Anneaux[i];
			if (!(anneau.Age < 0f))
			{
				float num2 = anneau.Age / anneau.Vie;
				float num3 = 0.6f + anneau.RMax * (1f - (1f - num2) * (1f - num2));
				float a = 0.55f * (1f - num2) * anneau.Force;
				if (anneau.Age < 60f)
				{
					PtL(anneau.X, anneau.Y, 240, 248, 255, 0.75f * anneau.Force);
				}
				int num4 = Math.Max(4, (int)MathF.Round(num3 * 5f));
				for (int j = 0; j < num4; j++)
				{
					float x = (float)j / (float)num4 * (float)Math.PI * 2f;
					PtL(anneau.X + MathF.Cos(x) * num3, anneau.Y + MathF.Sin(x) * num3 * 0.5f, 228, 240, 255, a);
				}
			}
		}
		for (int k = 0; k < _nbEclabs; k++)
		{
			Eclab eclab = Eclabs[k];
			int num5 = Math.Min(3, (int)(eclab.Age / 60f));
			float a2 = num5 switch
			{
				0 => 0.85f, 
				1 => 0.75f, 
				2 => 0.55f, 
				_ => 0.3f, 
			} * eclab.Force;
			(sbyte, sbyte)[] array = Couronne[num5];
			for (int l = 0; l < array.Length; l++)
			{
				var (b, b2) = array[l];
				PtL(eclab.X + (float)(b * eclab.Miroir), eclab.Y + (float)b2, 230, 238, 248, a2);
			}
		}
		float eclairage = _etal.Eclairage;
		for (int m = 0; m < _nbGouttes; m++)
		{
			Goutte goutte = Gouttes[m];
			int num6 = goutte.Couche switch
			{
				0 => 3, 
				1 => 5, 
				_ => 8, 
			} + ((_p.Orage > 0.5f) ? 2 : 0);
			if (!Dedans(goutte.X, goutte.Y, vue, num6 + 2))
			{
				continue;
			}
			float num7 = goutte.Couche switch
			{
				0 => 0.3f, 
				1 => 0.5f, 
				_ => 0.72f, 
			};
			int num8 = 0;
			float num9 = MathF.Floor(goutte.X);
			float num10 = MathF.Floor(goutte.Y);
			for (int n = 1; n <= num6; n++)
			{
				float num11 = ((n < num6) ? MathF.Floor(goutte.X - num * (float)n) : float.NaN);
				if (n >= num6 || num11 != num9)
				{
					float a3 = num7 * (1f - (float)(num8 + n - 1) / 2f / (float)num6 * 0.6f);
					Carre(num9, num10 - (float)(n - 1), 1f, n - num8, (int)(215f * eclairage), (int)(228f * eclairage), (int)(246f * eclairage), a3);
					num8 = n;
					num9 = num11;
				}
			}
		}
		if (_p.Pluie > 0.05f && _analysesVisibles.Count > 0)
		{
			int num12 = (int)(_p.Pluie * 130f * Echelle());
			for (int num13 = 0; num13 < num12; num13++)
			{
				var (mapInstance, analyse) = _analysesVisibles[Hasard.Next(_analysesVisibles.Count)];
				if (analyse.PointsEau.Length != 0)
				{
					int num14 = analyse.PointsEau[Hasard.Next(analyse.PointsEau.Length)];
					float x2 = mapInstance.X + num14 % analyse.L + Hasard.Next(7) - 3;
					float y = mapInstance.Y + num14 / analyse.L + Hasard.Next(5) - 2;
					if (Dedans(x2, y, vue))
					{
						PtL(x2, y, 225, 238, 250, 0.26f);
					}
				}
			}
		}
		if (!(_mouille > 0.25f) || !(_p.Pluie > 0.05f) || _analysesVisibles.Count <= 0)
		{
			return;
		}
		int num15 = (int)(_mouille * 45f * Echelle());
		for (int num16 = 0; num16 < num15; num16++)
		{
			var (mapInstance2, analyse2) = _analysesVisibles[Hasard.Next(_analysesVisibles.Count)];
			if (analyse2.PointsSol.Length != 0)
			{
				int num17 = analyse2.PointsSol[Hasard.Next(analyse2.PointsSol.Length)];
				float x3 = mapInstance2.X + num17 % analyse2.L + Hasard.Next(9) - 4;
				float y2 = mapInstance2.Y + num17 / analyse2.L + Hasard.Next(9) - 4;
				if (Dedans(x3, y2, vue))
				{
					PtL(x3, y2, 236, 242, 250, 0.32f);
				}
			}
		}
	}

	private static void DessinerFlocons(FloatRect vue)
	{
		for (int i = 0; i < _nbPoses; i++)
		{
			Pose pose = Poses[i];
			PtL(pose.X, pose.Y, 246, 249, 255, 0.75f * (1f - pose.Age / 1400f));
		}
		int num = ((_p.Vent > 0.6f) ? ((int)MathF.Round((_p.Vent - 0.6f) * 7f)) : 0);
		for (int j = 0; j < _nbFlocons; j++)
		{
			Flocon flocon = Flocons[j];
			if (!Dedans(flocon.X, flocon.Y, vue, 3f))
			{
				continue;
			}
			if (flocon.Couche == 0)
			{
				PtL(flocon.X, flocon.Y, 225, 232, 245, 0.5f);
				continue;
			}
			bool flag = flocon.Couche == 2;
			PtL(flocon.X + 1f, flocon.Y + 1f, 96, 112, 150, flag ? 0.45f : 0.3f);
			PtL(flocon.X, flocon.Y, 250, 252, 255, flag ? 0.95f : 0.8f);
			for (int k = 1; k <= num; k++)
			{
				PtL(flocon.X + (float)k, flocon.Y - (float)k * 0.15f, 240, 245, 255, 0.5f * (1f - (float)k / (float)(num + 1)));
			}
			if (flag)
			{
				PtL(flocon.X - 1f, flocon.Y, 236, 242, 255, 0.45f);
				PtL(flocon.X + 1f, flocon.Y, 236, 242, 255, 0.45f);
				PtL(flocon.X, flocon.Y - 1f, 236, 242, 255, 0.45f);
				PtL(flocon.X, flocon.Y + 1f, 236, 242, 255, 0.45f);
			}
		}
	}

	private static void DessinerVent(FloatRect vue)
	{
		float num = (float)_t;
		for (int i = 0; i < _nbFeuilles; i++)
		{
			Feuille feuille = Feuilles[i];
			(int, int, int) tuple = CouleursFeuilles[feuille.Couleur / 3][feuille.Couleur % 3];
			(sbyte, sbyte)[] array = PosesFeuille[(int)(num / 120f + feuille.Phase * 3f) % 3];
			(sbyte, sbyte)[] array2 = array;
			for (int j = 0; j < array2.Length; j++)
			{
				var (b, b2) = array2[j];
				PtL(feuille.X + (float)b, feuille.Y + (float)b2, tuple.Item1, tuple.Item2, tuple.Item3, 0.95f);
			}
		}
		float eclairage = _etal.Eclairage;
		for (int k = 0; k < _nbGrains; k++)
		{
			Grain grain = Grains[k];
			int num2 = Math.Max(1, grain.Longueur / 2);
			float num3 = Math.Min(1f, grain.A * 1.3f);
			Carre(MathF.Floor(grain.X), MathF.Floor(grain.Y), num2, 1f, (int)(250f * eclairage), (int)(226f * eclairage), (int)(182f * eclairage), num3);
			Carre(MathF.Floor(grain.X) + (float)num2, MathF.Floor(grain.Y + (float)(int)grain.Longueur * 0.08f), grain.Longueur - num2, 1f, (int)(250f * eclairage), (int)(226f * eclairage), (int)(182f * eclairage), num3 * 0.5f);
		}
		Tourbillon tourbillon = _tourbillon;
		if (tourbillon == null)
		{
			return;
		}
		float num4 = tourbillon.Age / tourbillon.Vie;
		float num5 = Math.Min(1f, Math.Min(num4 * 6f, (1f - num4) * 5f));
		for (int l = -2; l <= 2; l++)
		{
			for (int m = -12; m <= 12; m++)
			{
				if ((float)m / 12f * ((float)m / 12f) + (float)l / 2.5f * ((float)l / 2.5f) < 1f)
				{
					Pt(tourbillon.X + (float)m, tourbillon.Y + (float)l, 90, 62, 36, 0.22f * num5);
				}
			}
		}
		for (int n = 0; n < 150; n++)
		{
			float num6 = (float)n / 150f;
			float num7 = 2f + 21f * num6 * num6 + 3f * num6;
			float x = num * 0.012f + (float)n * 2.39f;
			float x2 = tourbillon.X + MathF.Cos(x) * num7 + MathF.Sin(num * 0.003f + num6 * 4f) * 6f * num6;
			float y = tourbillon.Y - num6 * 76f + MathF.Sin(x) * num7 * 0.32f;
			if (MathF.Sin(x) > 0f)
			{
				PtL(x2, y, 252, 232, 196, (0.85f - 0.45f * num6) * num5);
			}
			else
			{
				PtL(x2, y, 150, 112, 72, (0.7f - 0.4f * num6) * num5);
			}
		}
	}

	private static void DessinerReflets(FloatRect vue, in Etalonnage e)
	{
		float num = (float)_t;
		float num2 = _zone switch
		{
			GenreZone.Ile => 0.8f, 
			GenreZone.Desert => 0.3f, 
			GenreZone.Foret => 0.18f, 
			GenreZone.Montagne => 0.2f, 
			_ => 0.4f, 
		};
		if (e.Soleil > 0.05f)
		{
			int num3 = (int)(num / 90f);
			foreach (var (mapInstance, analyse) in _analysesVisibles)
			{
				if (analyse.PointsEau.Length == 0)
				{
					continue;
				}
				for (int i = 0; i < 16; i++)
				{
					uint etat = (uint)(i * 977 + (num3 + i * 3) / 4 * 131 + mapInstance.Id.GetHashCode());
					int num4 = analyse.PointsEau[(int)(Alea(ref etat) * (float)analyse.PointsEau.Length)];
					if (analyse.Dist[num4] < 4)
					{
						continue;
					}
					float num5 = mapInstance.X + num4 % analyse.L;
					float y = mapInstance.Y + num4 / analyse.L;
					if (Dedans(num5, y, vue))
					{
						float num6 = MathF.Sin((float)((num3 + i * 3) % 4) / 4f * (float)Math.PI) * e.Soleil * num2;
						Pt(num5, y, 255, 250, 235, num6, GameBlendModes.Add);
						if (num6 > 0.5f)
						{
							Pt(num5 - 1f, y, 255, 250, 235, num6 * 0.4f, GameBlendModes.Add);
							Pt(num5 + 1f, y, 255, 250, 235, num6 * 0.4f, GameBlendModes.Add);
						}
					}
				}
			}
		}
		if (_analysesVisibles.Count > 0 && (e.Coucher > 0.03f || e.NuitClaire > 0.03f))
		{
			float num7 = vue.X + vue.Width * 0.5f;
			bool flag = e.Coucher > e.NuitClaire * 0.75f;
			float num8 = (flag ? (e.Coucher * 0.9f) : (e.NuitClaire * 0.75f));
			int num9 = (flag ? 2 : 3);
			uint num10 = (uint)(num / 150f);
			for (float num11 = MathF.Floor(vue.Top / (float)num9) * (float)num9; num11 < vue.Bottom; num11 += (float)num9)
			{
				uint etat2 = (uint)((int)num11 * 17) + num10 * 29;
				for (int j = 0; j < 3; j++)
				{
					float x = MathF.Round(num7 + (Alea(ref etat2) - 0.5f) * (22f + (num11 - vue.Top) * 0.1f) * 2f);
					float num12 = Alea(ref etat2);
					int num13 = 2 + (int)(Alea(ref etat2) * 3f);
					if (!(num12 < 0.45f) && SolEn(x, num11) == Sol.Eau)
					{
						if (flag)
						{
							Carre(x, num11, num13, 1f, 255, 205, 110, (0.25f + 0.5f * num12) * num8, GameBlendModes.Add);
						}
						else
						{
							Carre(x, num11, num13, 1f, 205, 222, 255, (0.25f + 0.5f * num12) * num8, GameBlendModes.Add);
						}
					}
				}
			}
		}
		if (e.NuitClaire > 0.05f)
		{
			foreach (var analysesVisible in _analysesVisibles)
			{
				MapInstance item = analysesVisible.Carte;
				Analyse item2 = analysesVisible.A;
				(short, short, float, float, bool)[] etoiles = item2.Etoiles;
				for (int k = 0; k < etoiles.Length; k++)
				{
					(short, short, float, float, bool) tuple2 = etoiles[k];
					float num14 = item.X + tuple2.Item1;
					float num15 = item.Y + tuple2.Item2;
					if (Dedans(num14, num15, vue))
					{
						float num16 = (0.45f + 0.55f * MathF.Sin(num * 0.002f * tuple2.Item4 + tuple2.Item3)) * e.NuitClaire * (tuple2.Item5 ? 0.95f : 0.55f);
						Pt(num14, num15, 225, 234, 255, num16, GameBlendModes.Add);
						if (tuple2.Item5)
						{
							Pt(num14 - 1f, num15, 200, 215, 255, num16 * 0.35f, GameBlendModes.Add);
							Pt(num14 + 1f, num15, 200, 215, 255, num16 * 0.35f, GameBlendModes.Add);
							Pt(num14, num15 - 1f, 200, 215, 255, num16 * 0.25f, GameBlendModes.Add);
						}
					}
				}
			}
		}
		if (_zone == GenreZone.Montagne && _enneigement > 0.2f && (e.Soleil > 0.05f || e.NuitClaire > 0.05f))
		{
			float num17 = Math.Max(e.Soleil, e.NuitClaire * 0.7f) * _enneigement;
			float num18 = (0.12f + _enneigement * 0.8f) * 254f;
			foreach (var analysesVisible2 in _analysesVisibles)
			{
				MapInstance item3 = analysesVisible2.Carte;
				Analyse item4 = analysesVisible2.A;
				for (int l = 0; l < 40; l++)
				{
					float num19 = num / 110f + (float)(l * 5);
					uint etat3 = (uint)(l * 613 + (int)(num19 / 6f) * 97 + item3.Id.GetHashCode());
					int num20 = item4.PointsSol[(int)(Alea(ref etat3) * (float)item4.PointsSol.Length)];
					if (!((float)(int)item4.Seuil[num20] >= num18))
					{
						float x2 = item3.X + num20 % item4.L;
						float y2 = item3.Y + num20 / item4.L;
						if (Dedans(x2, y2, vue))
						{
							Pt(x2, y2, 255, 255, 255, MathF.Sin(num19 % 6f / 6f * (float)Math.PI) * num17, GameBlendModes.Add);
						}
					}
				}
			}
		}
		float num21 = ((_zone == GenreZone.Foret) ? (_p.Rayons * e.Soleil) : 0f);
		if (num21 > 0.05f)
		{
			for (int m = 0; m < 50; m++)
			{
				uint etat4 = (uint)(m * 7919 + 3);
				float num22 = m;
				int num23 = 5;
				float num24 = 440f;
				float num25 = num22 % num24 / 512f * vue.Width * 1.2f - vue.Width * 0.1f;
				float num26 = vue.Top + (Alea(ref etat4) * vue.Height + num * 0.004f * (0.5f + Alea(ref etat4))) % vue.Height;
				float x3 = vue.X + num25 + (num26 - vue.Top) / vue.Height * vue.Width * 1.2f * 0.55f * 256f / 512f + Alea(ref etat4) * 30f + MathF.Sin(num * 0.0007f + (float)m) * 3f;
				Pt(x3, num26, 255, 244, 205, num21 * (0.35f + 0.35f * MathF.Sin(num * 0.003f + (float)m)), GameBlendModes.Add);
			}
		}
	}

	private static void DessinerLucioles(in Etalonnage e)
	{
		float num = e.Lampes * (1f - _p.Pluie) * (1f - _p.Sable) * (1f - _p.Orage) * (1f - _p.Neige);
		if (num <= 0.05f || _texHalo == null)
		{
			return;
		}
		float num2 = (float)_t;
		for (int i = 0; i < _nbLucioles; i++)
		{
			Luciole luciole = Lucioles[i];
			float num3 = luciole.Cx + MathF.Sin(num2 * 0.0004f * luciole.V + luciole.Ph) * luciole.Ax;
			float num4 = luciole.Cy + MathF.Sin(num2 * 0.00057f * luciole.V + luciole.Ph * 1.7f) * luciole.Ay;
			float num5 = MathF.Pow(Math.Max(0f, MathF.Sin(num2 * 0.001f * luciole.Clig + luciole.Ph)), 3f) * num;
			if (!(num5 < 0.02f))
			{
				Texture(_texHalo, num3 - 9f, num4 - 9f, 18f, 18f, 200, 255, 120, 0.55f * num5, GameBlendModes.Add);
				Pt(num3, num4, 240, 255, 170, num5, GameBlendModes.Add);
			}
		}
	}

	private static void DessinerEclair(FloatRect vue)
	{
		Eclair eclair = _eclair;
		if (eclair == null || eclair.Loin)
		{
			return;
		}
		float num = (float)(_t - eclair.T0);
		if (num < 100f || num > 340f)
		{
			return;
		}
		float visible = ((num < 150f) ? 1f : ((num < 185f) ? 0.3f : ((num < 270f) ? 1f : (1f - (num - 270f) / 70f))));
		for (int i = 1; i < eclair.Tronc.Count; i++)
		{
			Trait(eclair.Tronc[i - 1], eclair.Tronc[i], tronc: true);
		}
		foreach (List<(float, float)> branch in eclair.Branches)
		{
			for (int j = 1; j < branch.Count; j++)
			{
				Trait(branch[j - 1], branch[j], tronc: false);
			}
		}
		if (_texHalo != null && num < 185f)
		{
			Texture(_texHalo, eclair.X - 22f, eclair.Y - 22f, 44f, 44f, 225, 235, 255, 0.42f * visible, GameBlendModes.Add);
		}
		void Trait((float X, float Y) de, (float X, float Y) a2, bool tronc)
		{
			int num2 = Math.Max(1, (int)Math.Max(Math.Abs(a2.X - de.X), Math.Abs(a2.Y - de.Y)));
			for (int k = 0; k <= num2; k++)
			{
				float num3 = Lerp(de.X, a2.X, (float)k / (float)num2);
				float y = Lerp(de.Y, a2.Y, (float)k / (float)num2);
				if (tronc)
				{
					Pt(num3, y, 255, 255, 255, visible, GameBlendModes.Add);
					Pt(num3 + 1f, y, 255, 255, 255, visible * 0.85f, GameBlendModes.Add);
					Pt(num3 - 1f, y, 160, 192, 255, visible * 0.55f, GameBlendModes.Add);
					Pt(num3 + 2f, y, 160, 192, 255, visible * 0.55f, GameBlendModes.Add);
					Pt(num3 - 2f, y, 120, 150, 255, visible * 0.25f, GameBlendModes.Add);
					Pt(num3 + 3f, y, 120, 150, 255, visible * 0.25f, GameBlendModes.Add);
				}
				else
				{
					Pt(num3, y, 225, 235, 255, visible * 0.7f, GameBlendModes.Add);
					Pt(num3 + 1f, y, 150, 185, 255, visible * 0.25f, GameBlendModes.Add);
				}
			}
		}
	}

	private static void RetirerFaussesEaux(byte[] px, byte[] eau, int L, int H)
	{
		int num = L * H;
		bool[] array = new bool[num];
		int[] array2 = new int[num];
		for (int i = 0; i < num; i++)
		{
			if (eau[i] != 0 || array[i])
			{
				continue;
			}
			int num2 = 0;
			int num3 = 0;
			array2[num3++] = i;
			array[i] = true;
			while (num2 < num3)
			{
				int num4 = array2[num2++];
				int num5 = num4 % L;
				if (num5 > 0 && eau[num4 - 1] == 0 && !array[num4 - 1])
				{
					array[num4 - 1] = true;
					array2[num3++] = num4 - 1;
				}
				if (num5 < L - 1 && eau[num4 + 1] == 0 && !array[num4 + 1])
				{
					array[num4 + 1] = true;
					array2[num3++] = num4 + 1;
				}
				if (num4 >= L && eau[num4 - L] == 0 && !array[num4 - L])
				{
					array[num4 - L] = true;
					array2[num3++] = num4 - L;
				}
				if (num4 < num - L && eau[num4 + L] == 0 && !array[num4 + L])
				{
					array[num4 + L] = true;
					array2[num3++] = num4 + L;
				}
			}
			if (num3 <= 8)
			{
				for (int j = 0; j < num3; j++)
				{
					eau[array2[j]] = 1;
				}
			}
		}
		Array.Clear(array);
		for (int k = 0; k < num; k++)
		{
			if (eau[k] != 1 || array[k])
			{
				continue;
			}
			int num6 = 0;
			int num7 = 0;
			long num8 = 0L;
			long num9 = 0L;
			array2[num7++] = k;
			array[k] = true;
			while (num6 < num7)
			{
				int num10 = array2[num6++];
				int num11 = num10 % L;
				if (num11 < L - 1 && eau[num10 + 1] == 1)
				{
					num9++;
					if (px[num10 * 4] == px[(num10 + 1) * 4] && px[num10 * 4 + 1] == px[(num10 + 1) * 4 + 1] && px[num10 * 4 + 2] == px[(num10 + 1) * 4 + 2])
					{
						num8++;
					}
					if (!array[num10 + 1])
					{
						array[num10 + 1] = true;
						array2[num7++] = num10 + 1;
					}
				}
				if (num11 > 0 && eau[num10 - 1] == 1 && !array[num10 - 1])
				{
					array[num10 - 1] = true;
					array2[num7++] = num10 - 1;
				}
				if (num10 >= L && eau[num10 - L] == 1 && !array[num10 - L])
				{
					array[num10 - L] = true;
					array2[num7++] = num10 - L;
				}
				if (num10 < num - L && eau[num10 + L] == 1 && !array[num10 + L])
				{
					array[num10 + L] = true;
					array2[num7++] = num10 + L;
				}
			}
			if (num7 < 1024 || num8 * 2 > num9)
			{
				for (int l = 0; l < num7; l++)
				{
					eau[array2[l]] = 0;
				}
			}
		}
	}

	private static void ConstruireEau(Analyse a, byte[] px, byte[] eau, byte[] dist, byte[] plat, Random hasard)
	{
		int l = a.L;
		int h = a.H;
		int num = l * h;
		List<(int, byte)> list = new List<(int, byte)>();
		for (int i = 0; i < a.Rive.Length; i++)
		{
			(short X, short Y, sbyte Nx, sbyte Ny, float Phase) tuple = a.Rive[i];
			short item = tuple.X;
			short item2 = tuple.Y;
			sbyte item3 = tuple.Nx;
			sbyte item4 = tuple.Ny;
			int num2 = 0;
			for (int j = 1; j <= 9; j++)
			{
				int num3 = item - item3 * j;
				int num4 = item2 - item4 * j;
				if (num3 < 0 || num4 < 0 || num3 >= l || num4 >= h || plat[num4 * l + num3] == 0)
				{
					break;
				}
				num2 = j;
			}
			if (num2 >= 2)
			{
				list.Add((i, (byte)num2));
			}
		}
		bool[] etiquettes = new bool[num];
		int[] file = new int[num];
		List<(short, short, byte)> list2 = new List<(short, short, byte)>();
		for (int k = 0; k < num; k++)
		{
			if (eau[k] != 0 || etiquettes[k])
			{
				continue;
			}
			int num5 = 0;
			int queue = 0;
			int num6 = 0;
			int num7 = l;
			int num8 = -1;
			int num9 = -1;
			long num10 = 0L;
			long num11 = 0L;
			bool flag = false;
			file[queue++] = k;
			etiquettes[k] = true;
			while (num5 < queue)
			{
				int num12 = file[num5++];
				int num13 = num12 % l;
				int num14 = num12 / l;
				num6++;
				num10 += px[num12 * 4];
				num11 += px[num12 * 4 + 2];
				num7 = Math.Min(num7, num13);
				num8 = Math.Max(num8, num13);
				num9 = Math.Max(num9, num14);
				if (num13 == 0 || num14 == 0 || num13 == l - 1 || num14 == h - 1)
				{
					flag = true;
				}
				if (num13 > 0)
				{
					Voisin(num12 - 1);
				}
				if (num13 < l - 1)
				{
					Voisin(num12 + 1);
				}
				if (num14 > 0)
				{
					Voisin(num12 - l);
				}
				if (num14 < h - 1)
				{
					Voisin(num12 + l);
				}
			}
			if (!flag && num6 >= 12 && num6 <= 1600 && num11 <= num10 + (long)num6 * 20L && list2.Count < 48)
			{
				list2.Add(((short)((num7 + num8) / 2), (short)num9, (byte)Math.Clamp((num8 - num7) / 2, 2, 20)));
			}
			void Voisin(int num33)
			{
				if (eau[num33] == 0 && !etiquettes[num33])
				{
					etiquettes[num33] = true;
					file[queue++] = num33;
				}
			}
		}
		List<(short, short, float, float, byte)> list3 = new List<(short, short, float, float, byte)>();
		for (int m = 3; m < h - 3; m += 5)
		{
			for (int n = 2; n < l - 8; n += 9)
			{
				int num15 = n + hasard.Next(6);
				int num16 = m + hasard.Next(3);
				int num17 = num16 * l + num15;
				if (eau[num17] == 1 && dist[num17] >= 5)
				{
					list3.Add(((short)num15, (short)num16, (float)(hasard.NextDouble() * Math.PI * 2.0), 0.8f + (float)hasard.NextDouble() * 0.5f, (byte)(3 + hasard.Next(3))));
				}
			}
		}
		List<int> list4 = new List<int>();
		for (int num18 = 0; num18 < 30000; num18++)
		{
			if (list4.Count >= 400)
			{
				break;
			}
			int num19 = hasard.Next(num);
			if (eau[num19] == 1 && dist[num19] >= 14)
			{
				list4.Add(num19);
			}
		}
		byte[] array = new byte[num * 4];
		for (int num20 = 0; num20 < num; num20++)
		{
			if (eau[num20] == 1)
			{
				float num21 = Math.Clamp((float)(dist[num20] - 3) / 37f, 0f, 1f);
				array[num20 * 4] = 16;
				array[num20 * 4 + 1] = 74;
				array[num20 * 4 + 2] = 138;
				array[num20 * 4 + 3] = (byte)(num21 * num21 * 0.26f * 255f + num21 * 0.1f * 255f);
			}
		}
		byte[] array2 = new byte[num * 4];
		bool[] array3 = new bool[h];
		int[] array4 = new int[l];
		Array.Fill(array4, -1000);
		bool flag2 = false;
		for (int num22 = 0; num22 < h; num22++)
		{
			for (int num23 = 0; num23 < l; num23++)
			{
				int num24 = num22 * l + num23;
				if (eau[num24] != 1)
				{
					array4[num23] = num22;
					continue;
				}
				int num25 = array4[num23];
				int num26 = num22 - num25 - 1;
				int num27 = num25 - num26;
				if (num25 >= 0 && num26 < 28 && num27 >= 0)
				{
					int num28 = num27 * l + num23;
					int num29 = px[num28 * 4];
					int num30 = px[num28 * 4 + 1];
					int num31 = px[num28 * 4 + 2];
					if (eau[num28] == 0 && plat[num28] == 0 && (num30 <= num29 + 8 || num30 <= num31 + 20))
					{
						float num32 = 0.55f * (0.85f - 0.77f * (float)num26 / 28f);
						array2[num24 * 4] = (byte)((float)num29 * 0.55f + 18f);
						array2[num24 * 4 + 1] = (byte)((float)num30 * 0.55f + 49.5f);
						array2[num24 * 4 + 2] = (byte)((float)num31 * 0.55f + 85.5f);
						array2[num24 * 4 + 3] = (byte)(num32 * 255f);
						array3[num22] = true;
						flag2 = true;
					}
				}
			}
		}
		a.Plages = list.ToArray();
		a.Rochers = list2.ToArray();
		a.Traits = list3.ToArray();
		a.PointsProfonds = list4.ToArray();
		a.RgbaProfondeur = array;
		a.RgbaReflet = (flag2 ? array2 : null);
		a.LignesReflet = array3;
	}

	private static void ViderEau()
	{
		_nbOndesEau = (_nbGerbesEau = (_nbGouttesEau = 0));
		Bancs.Clear();
		Personnages.Clear();
		DepartsKi.Clear();
		ChargesEau.Clear();
		_carteEau = null;
		_analyseEau = null;
	}

	private static void AjouterOnde(float x, float y, float vie, float rayon, float force, float age = 0f)
	{
		if (_nbOndesEau < OndesEau.Length)
		{
			OndesEau[_nbOndesEau++] = new OndeEau
			{
				X = x,
				Y = y,
				Age = age,
				Vie = vie,
				Rayon = rayon,
				Force = force
			};
		}
	}

	private static void AjouterGerbe(float x, float y, float taille)
	{
		if (_nbGerbesEau < GerbesEau.Length)
		{
			GerbesEau[_nbGerbesEau++] = new GerbeEau
			{
				X = x,
				Y = y,
				Age = 0f,
				Taille = taille
			};
		}
	}

	private static void ProjeterGouttes(float x, float y, int nombre, float force)
	{
		for (int i = 0; i < nombre; i++)
		{
			if (_nbGouttesEau >= GouttesEau.Length)
			{
				break;
			}
			float x2 = H01() * ((float)Math.PI * 2f);
			float num = (14f + H01() * 30f) * force;
			GouttesEau[_nbGouttesEau++] = new GoutteEau
			{
				X = x + MathF.Cos(x2) * 2f,
				Y = y + MathF.Sin(x2),
				H = 1f + H01() * 3f,
				Vx = MathF.Cos(x2) * num,
				Vy = MathF.Sin(x2) * num * 0.4f,
				Vh = (60f + H01() * 70f) * force,
				Age = (0f - H01()) * 40f,
				Claire = (H01() < 0.5f)
			};
		}
	}

	private static void MajEau(float dt)
	{
		_carteEau = null;
		_analyseEau = null;
		float num = dt / 1000f;
		for (int num2 = _nbOndesEau - 1; num2 >= 0; num2--)
		{
			OndesEau[num2].Age += dt;
			if (OndesEau[num2].Age >= OndesEau[num2].Vie)
			{
				OndesEau[num2] = OndesEau[--_nbOndesEau];
			}
		}
		for (int num3 = _nbGerbesEau - 1; num3 >= 0; num3--)
		{
			GerbesEau[num3].Age += dt;
			if (GerbesEau[num3].Age > 385f)
			{
				GerbesEau[num3] = GerbesEau[--_nbGerbesEau];
			}
		}
		for (int num4 = _nbGouttesEau - 1; num4 >= 0; num4--)
		{
			ref GoutteEau reference = ref GouttesEau[num4];
			reference.Age += dt;
			if (!(reference.Age < 0f))
			{
				reference.X += reference.Vx * num;
				reference.Y += reference.Vy * num;
				reference.H += reference.Vh * num;
				reference.Vh -= 420f * num;
				if (!(reference.H > 0f) || !(reference.Age < 3000f))
				{
					if (EauMonde(reference.X, reference.Y))
					{
						AjouterOnde(reference.X, reference.Y, 260f, 2.5f, 0.6f);
					}
					GouttesEau[num4] = GouttesEau[--_nbGouttesEau];
				}
			}
		}
		MajPoissons(dt, num);
		MajPersonnages();
		MajKiEau();
	}

	private static bool Profond(Analyse a, float x, float y, int distance)
	{
		if (x < 0f || y < 0f || x >= (float)a.L || y >= (float)a.H)
		{
			return false;
		}
		int num = (int)y * a.L + (int)x;
		if (a.Eau[num] == 1)
		{
			return a.Dist[num] >= distance;
		}
		return false;
	}

	private static void MajPoissons(float dt, float s)
	{
		foreach (MapInstance visible in _visibles)
		{
			if (!Analyses.TryGetValue(visible.Id, out Analyse value) || value.Carte != visible || !value.AEau || value.PointsProfonds.Length < 20 || ZoneDe(visible) == GenreZone.Interieur)
			{
				continue;
			}
			if (!Bancs.TryGetValue(visible.Id, out Banc value2) || value2.Carte != visible)
			{
				int num = Math.Clamp(value.PointsProfonds.Length / 60, 2, 6);
				Poisson[] array = new Poisson[num];
				for (int i = 0; i < num; i++)
				{
					int num2 = value.PointsProfonds[Hasard.Next(value.PointsProfonds.Length)];
					array[i] = new Poisson
					{
						X = num2 % value.L,
						Y = num2 / value.L,
						Angle = H01() * ((float)Math.PI * 2f),
						Vitesse = 9f + H01() * 8f,
						Taille = 0.8f + H01() * 0.25f,
						Queue = H01() * 6f
					};
				}
				value2 = new Banc
				{
					Carte = visible,
					Poissons = array,
					ProchainSaut = _t + 4000.0 + (double)(H01() * 8000f)
				};
				Bancs[visible.Id] = value2;
			}
			Poisson[] poissons = value2.Poissons;
			foreach (Poisson poisson in poissons)
			{
				if (poisson.Saut >= 0f)
				{
					poisson.Saut += dt;
					if (poisson.Saut >= 540f)
					{
						float x = (float)visible.X + poisson.ArriveeX;
						float num3 = (float)visible.Y + poisson.ArriveeY;
						AjouterGerbe(x, num3, 9f);
						ProjeterGouttes(x, num3 - 2f, 8, 0.8f);
						for (int k = 0; k < 3; k++)
						{
							AjouterOnde(x, num3, 1200f, 20f, 1f, -k * 200);
						}
						poisson.X = poisson.ArriveeX;
						poisson.Y = poisson.ArriveeY;
						poisson.Saut = -1f;
					}
				}
				else
				{
					poisson.Virage = Math.Clamp(poisson.Virage + (H01() - 0.5f) * 3f * s, -0.8f, 0.8f);
					poisson.Angle += poisson.Virage * s;
					float x2 = poisson.X + MathF.Cos(poisson.Angle) * 12f;
					float y = poisson.Y + MathF.Sin(poisson.Angle) * 12f;
					if (!Profond(value, x2, y, 9))
					{
						poisson.Angle += (float)Math.PI * (0.6f + H01() * 0.8f);
						poisson.Virage = 0f - poisson.Virage;
					}
					else
					{
						poisson.X += MathF.Cos(poisson.Angle) * poisson.Vitesse * s;
						poisson.Y += MathF.Sin(poisson.Angle) * poisson.Vitesse * s;
					}
					poisson.Queue += s * (5f + poisson.Vitesse * 0.4f);
				}
			}
			if (_t < value2.ProchainSaut)
			{
				continue;
			}
			value2.ProchainSaut = _t + 9000.0 + (double)(H01() * 9000f);
			FloatRect worldViewport = Graphics.WorldViewport;
			Poisson[] poissons2 = value2.Poissons;
			foreach (Poisson poisson2 in poissons2)
			{
				float num4 = poisson2.X + MathF.Cos(poisson2.Angle) * 26f;
				float num5 = poisson2.Y + MathF.Sin(poisson2.Angle) * 10f;
				if (Dedans((float)visible.X + poisson2.X, (float)visible.Y + poisson2.Y, worldViewport, -24f) && Profond(value, poisson2.X, poisson2.Y, 10) && Profond(value, num4, num5, 8))
				{
					poisson2.Saut = 0f;
					poisson2.DepartX = poisson2.X;
					poisson2.DepartY = poisson2.Y;
					poisson2.ArriveeX = num4;
					poisson2.ArriveeY = num5;
					AjouterOnde((float)visible.X + poisson2.X, (float)visible.Y + poisson2.Y, 1200f, 14f, 1f);
					ProjeterGouttes((float)visible.X + poisson2.X, (float)visible.Y + poisson2.Y, 7, 0.7f);
					break;
				}
			}
		}
		if (Bancs.Count <= 12)
		{
			return;
		}
		KeyValuePair<Guid, Banc>[] array2 = Bancs.ToArray();
		for (int m = 0; m < array2.Length; m++)
		{
			KeyValuePair<Guid, Banc> keyValuePair = array2[m];
			var (key, banc2) = keyValuePair;
			if (!Analyses.TryGetValue(key, out Analyse value3) || value3.Carte != banc2.Carte)
			{
				Bancs.Remove(key);
			}
		}
	}

	private static Analyse? AnalyseEn(float x, float y, out MapInstance? carte)
	{
		int num = Options.Instance.Map.TileWidth * Options.Instance.Map.MapWidth;
		int num2 = Options.Instance.Map.TileHeight * Options.Instance.Map.MapHeight;
		MapInstance carteEau = _carteEau;
		if (carteEau != null && _analyseEau != null && x >= (float)carteEau.X && y >= (float)carteEau.Y && x < (float)(carteEau.X + num) && y < (float)(carteEau.Y + num2))
		{
			carte = carteEau;
			return _analyseEau;
		}
		foreach (MapInstance visible in _visibles)
		{
			if (!(x < (float)visible.X) && !(y < (float)visible.Y) && !(x >= (float)(visible.X + num)) && !(y >= (float)(visible.Y + num2)))
			{
				if (Analyses.TryGetValue(visible.Id, out Analyse value) && value.Carte == visible && value.Eau.Length == value.L * value.H && value.AEau)
				{
					_carteEau = visible;
					_analyseEau = value;
					carte = visible;
					return value;
				}
				break;
			}
		}
		carte = null;
		return null;
	}

	public static float ProfondeurEauAuPoint(float x, float y)
	{
		if (_enPanne || !_actif || !_initialise)
		{
			return 0f;
		}
		try
		{
			MapInstance carte;
			Analyse analyse = AnalyseEn(x, y, out carte);
			if (analyse == null || carte == null)
			{
				return 0f;
			}
			int num = (int)(x - (float)carte.X);
			int num2 = (int)(y - (float)carte.Y);
			if (num < 0 || num2 < 0 || num >= analyse.L || num2 >= analyse.H)
			{
				return 0f;
			}
			int num3 = num2 * analyse.L + num;
			return (analyse.Eau[num3] == 1) ? ((float)(int)analyse.Dist[num3]) : 0f;
		}
		catch (Exception exception)
		{
			Panne(exception);
			return 0f;
		}
	}

	private static bool EauMonde(float x, float y)
	{
		MapInstance carte;
		Analyse analyse = AnalyseEn(x, y, out carte);
		if (analyse == null || carte == null)
		{
			return false;
		}
		return analyse.Eau[(int)(y - (float)carte.Y) * analyse.L + (int)(x - (float)carte.X)] == 1;
	}

	private static float Sortie(float k)
	{
		return 1f - (1f - k) * (1f - k);
	}

	private static int PremierRive(Analyse a, int y)
	{
		int num = 0;
		int num2 = a.Rive.Length;
		while (num < num2)
		{
			int num3 = (num + num2) / 2;
			if (a.Rive[num3].Y < y)
			{
				num = num3 + 1;
			}
			else
			{
				num2 = num3;
			}
		}
		return num;
	}

	private static int PremiereTrait(Analyse a, int y)
	{
		int num = 0;
		int num2 = a.Traits.Length;
		while (num < num2)
		{
			int num3 = (num + num2) / 2;
			if (a.Traits[num3].Y < y)
			{
				num = num3 + 1;
			}
			else
			{
				num2 = num3;
			}
		}
		return num;
	}

	private static int PremierePlage(Analyse a, int y)
	{
		int num = 0;
		int num2 = a.Plages.Length;
		while (num < num2)
		{
			int num3 = (num + num2) / 2;
			if (a.Rive[a.Plages[num3].Rive].Y < y)
			{
				num = num3 + 1;
			}
			else
			{
				num2 = num3;
			}
		}
		return num;
	}

	private static void DessinerEau(FloatRect vue)
	{
		float t = (float)_t;
		_carteEau = null;
		_analyseEau = null;
		foreach (MapInstance visible in _visibles)
		{
			if (Analyses.TryGetValue(visible.Id, out Analyse value) && value.Carte == visible && value.AEau && value.Eau.Length == value.L * value.H && ZoneDe(visible) != GenreZone.Interieur)
			{
				IGameTexture texProfondeur = value.TexProfondeur;
				if (texProfondeur != null)
				{
					Graphics.DrawGameTexture(texProfondeur, new FloatRect(0f, 0f, value.L, value.H), new FloatRect(visible.X, visible.Y, value.L, value.H), Color.White);
				}
				IGameTexture texReflet = value.TexReflet;
				if (texReflet != null)
				{
					DessinerRefletDecor(visible, value, texReflet, vue, t);
				}
				DessinerPoissons(visible, value, vue, t);
				DessinerTraitsDuCiel(visible, value, vue, t);
				DessinerOndesDesRochers(visible, value, vue, t);
				DessinerEcume(visible, value, vue, t);
				DessinerVagues(visible, value, vue, t);
			}
		}
		for (int i = 0; i < _nbOndesEau; i++)
		{
			ref OndeEau reference = ref OndesEau[i];
			if (!(reference.Age < 0f))
			{
				float num = reference.Age / reference.Vie;
				float num2 = 3f + reference.Rayon * Sortie(num);
				TracerOnde(reference.X, reference.Y, num2, num2 * 0.42f, 0.55f * reference.Force * (1f - num), vue);
			}
		}
		DessinerSillages(vue);
		DessinerChargesSurLEau(vue);
		DessinerRefletsPersonnages(vue);
	}

	private static void DessinerRefletDecor(MapInstance carte, Analyse a, IGameTexture reflet, FloatRect vue, float t)
	{
		int num = (int)Math.Max(0f, vue.Top - (float)carte.Y - 2f);
		int num2 = (int)Math.Min(a.H, vue.Bottom - (float)carte.Y + 2f);
		int num3 = (int)Math.Max(0f, vue.Left - (float)carte.X - 2f);
		int num4 = (int)Math.Min(a.L, vue.Right - (float)carte.X + 2f);
		if (num4 <= num3 || num2 <= num)
		{
			return;
		}
		for (int i = num - num % 2; i < num2; i += 2)
		{
			if (a.LignesReflet[i] || (i + 1 < a.H && a.LignesReflet[i + 1]))
			{
				int num5 = Math.Min(2, a.H - i);
				float num6 = MathF.Round(MathF.Sin(t * 0.005f + (float)(carte.Y + i) * 0.55f));
				Graphics.DrawGameTexture(reflet, new FloatRect(num3, i, num4 - num3, num5), new FloatRect((float)(carte.X + num3) + num6, carte.Y + i, num4 - num3, num5), Color.White);
			}
		}
	}

	private static void DessinerPoissons(MapInstance carte, Analyse a, FloatRect vue, float t)
	{
		if (!Bancs.TryGetValue(carte.Id, out Banc value) || value.Carte != carte)
		{
			return;
		}
		float num = 0.45f + 0.55f * _etal.Jour;
		Poisson[] poissons = value.Poissons;
		foreach (Poisson poisson in poissons)
		{
			if (poisson.Saut >= 0f)
			{
				continue;
			}
			float num2 = (float)carte.X + poisson.X;
			float num3 = (float)carte.Y + poisson.Y;
			if (!Dedans(num2, num3, vue, 12f))
			{
				continue;
			}
			float num4 = MathF.Cos(poisson.Angle);
			float num5 = MathF.Sin(poisson.Angle);
			float num6 = MathF.Sin(poisson.Queue * 3.2f) * 1.6f;
			float taille = poisson.Taille;
			for (int j = -9; j <= 9; j++)
			{
				for (int k = -10; k <= 10; k++)
				{
					float num7 = (float)k * num4 + (float)j * num5;
					float num8 = (float)(-k) * num5 + (float)j * num4;
					float num9 = num7 / (6f * taille);
					float num10 = num8 / (2.2f * taille);
					bool flag = num9 * num9 + num10 * num10 <= 1f;
					bool flag2 = false;
					if (!flag && num7 < -4.5f * taille && num7 > -9f * taille)
					{
						float num11 = (0f - num7 - 5f * taille) / (4f * taille) * 2.2f * taille;
						flag2 = MathF.Abs(num8 - num6 * (0f - num7 - 5f * taille) / (4f * taille)) <= num11;
					}
					if (flag || flag2)
					{
						float num12 = num2 + (float)k;
						float num13 = num3 + (float)j;
						if (Profond(a, num12 - (float)carte.X, num13 - (float)carte.Y, 2))
						{
							Pt(num12, num13, 16, 42, 78, 0.34f * num);
						}
					}
				}
			}
		}
	}

	private static void DessinerTraitsDuCiel(MapInstance carte, Analyse a, FloatRect vue, float t)
	{
		float num = Math.Max(_p.Pluie, _p.Orage);
		if (num >= 0.3f || a.Traits.Length == 0)
		{
			return;
		}
		float num2 = (1f - num / 0.3f) * (0.35f + 0.65f * _etal.Jour) * (1f - _p.Brume * 0.6f) * (1f - _p.Sable * 0.8f);
		int num3 = (int)(vue.Bottom - (float)carte.Y) + 2;
		for (int i = PremiereTrait(a, (int)(vue.Top - (float)carte.Y) - 2); i < a.Traits.Length; i++)
		{
			(short, short, float, float, byte) tuple = a.Traits[i];
			if (tuple.Item2 > num3)
			{
				break;
			}
			float num4 = carte.X + tuple.Item1;
			float num5 = carte.Y + tuple.Item2;
			if (!(num4 < vue.Left - 8f) && !(num4 > vue.Right + 2f))
			{
				float num6 = MathF.Sin(t * 0.0021f * tuple.Item4 + tuple.Item3 + num4 * 0.02f - num5 * 0.035f);
				if (!(num6 < 0.55f))
				{
					float num7 = (num6 - 0.55f) / 0.45f;
					float num8 = MathF.Round(MathF.Sin(t * 0.0007f + tuple.Item3) * 2f);
					Carre(num4 + num8, num5, (int)tuple.Item5, 1f, 255, 255, 255, (0.1f + 0.42f * num7) * num2);
				}
			}
		}
	}

	private static void DessinerOndesDesRochers(MapInstance carte, Analyse a, FloatRect vue, float t)
	{
		for (int i = 0; i < a.Rochers.Length; i++)
		{
			(short, short, byte) tuple = a.Rochers[i];
			float num = carte.X + tuple.Item1;
			float num2 = carte.Y + tuple.Item2;
			if (Dedans(num, num2, vue, 40f))
			{
				for (int j = 0; j < 2; j++)
				{
					float num3 = ((t + (float)(i * 500) + (float)(j * 900)) % 1800f + 1800f) % 1800f;
					float num4 = num3 / 1800f;
					float num5 = (float)(int)tuple.Item3 * 0.8f + 3f + 14f * Sortie(num4);
					TracerOnde(num, num2, num5, num5 * 0.42f, 0.5f * (1f - num4), vue);
				}
			}
		}
	}

	private static void DessinerEcume(MapInstance carte, Analyse a, FloatRect vue, float t)
	{
		int num = (int)(vue.Bottom - (float)carte.Y) + 4;
		int num2 = (int)(t / 400f);
		for (int i = PremierRive(a, (int)(vue.Top - (float)carte.Y) - 4); i < a.Rive.Length; i++)
		{
			(short, short, sbyte, sbyte, float) tuple = a.Rive[i];
			if (tuple.Item2 > num)
			{
				break;
			}
			float num3 = carte.X + tuple.Item1;
			float num4 = carte.Y + tuple.Item2;
			if (num3 < vue.Left - 4f || num3 > vue.Right + 4f)
			{
				continue;
			}
			float num5 = MathF.Sin(t * 0.0018f + tuple.Item5 * 0.35f + (num3 + num4) * 0.045f);
			Carre(num3, num4, 1f, 1f, 255, 255, 255, 0.35f + 0.25f * (num5 + 1f) / 2f);
			if ((i + num2) % 3 != 0)
			{
				int num6 = 1 + (int)MathF.Round((num5 + 1f) * 1.2f);
				int num7 = tuple.Item1 + tuple.Item3 * num6;
				int num8 = tuple.Item2 + tuple.Item4 * num6;
				if (num7 >= 0 && num8 >= 0 && num7 < a.L && num8 < a.H && a.Eau[num8 * a.L + num7] == 1)
				{
					Carre(carte.X + num7, carte.Y + num8, 1f, 1f, 240, 250, 255, 0.28f + 0.3f * (1f - num5) / 2f);
				}
			}
		}
	}

	private static void DessinerVagues(MapInstance carte, Analyse a, FloatRect vue, float t)
	{
		if (a.Plages.Length == 0)
		{
			return;
		}
		int num = (int)(vue.Bottom - (float)carte.Y) + 10;
		for (int i = PremierePlage(a, (int)(vue.Top - (float)carte.Y) - 10); i < a.Plages.Length; i++)
		{
			(int Rive, byte Course) tuple = a.Plages[i];
			int item = tuple.Rive;
			byte item2 = tuple.Course;
			(short, short, sbyte, sbyte, float) tuple2 = a.Rive[item];
			if (tuple2.Item2 > num)
			{
				break;
			}
			float num2 = carte.X + tuple2.Item1;
			float num3 = carte.Y + tuple2.Item2;
			if (num2 < vue.Left - 10f || num2 > vue.Right + 10f)
			{
				continue;
			}
			float num4 = ((t + num2 * 1.4f + num3 * 0.9f) % 3600f + 3600f) % 3600f / 3600f;
			float num5 = ((num4 < 0.3f) ? Sortie(num4 / 0.3f) : ((num4 < 0.55f) ? (1f - (num4 - 0.3f) / 0.25f) : 0f));
			int num6 = (int)MathF.Round(num5 * 9f);
			float num7 = ((num4 < 0.3f) ? 1f : Math.Clamp(1f - (num4 - 0.3f) / 0.7f, 0f, 1f));
			for (int j = 1; j <= item2; j++)
			{
				float x = num2 - (float)(tuple2.Item3 * j);
				float y = num3 - (float)(tuple2.Item4 * j);
				if (j <= num6)
				{
					if (j == num6)
					{
						Carre(x, y, 1f, 1f, 255, 255, 255, 0.85f);
					}
					else
					{
						Carre(x, y, 1f, 1f, 170, 210, 235, 0.45f - 0.03f * (float)j);
					}
				}
				else if (num4 >= 0.3f)
				{
					Carre(x, y, 1f, 1f, 110, 82, 45, 0.22f * num7 * (1f - (float)j / 11f));
				}
			}
		}
	}

	private static void TracerOnde(float cx, float cy, float rx, float ry, float alpha, FloatRect vue)
	{
		if (alpha <= 0.01f || !Dedans(cx, cy, vue, rx + 4f))
		{
			return;
		}
		int num = Math.Max(12, (int)MathF.Ceiling((float)Math.PI * 2f * MathF.Max(rx, ry) * 1.2f));
		int num2 = int.MinValue;
		int num3 = int.MinValue;
		for (int i = 0; i < num; i++)
		{
			float x = (float)i / (float)num * ((float)Math.PI * 2f);
			int num4 = (int)MathF.Floor(cx + MathF.Cos(x) * rx);
			int num5 = (int)MathF.Floor(cy + MathF.Sin(x) * ry);
			if (num4 != num2 || num5 != num3)
			{
				num2 = num4;
				num3 = num5;
				if (EauMonde(num4, num5))
				{
					Carre(num4, num5, 1f, 1f, 235, 248, 255, alpha);
				}
			}
		}
	}

	private static void CarreL(float x, float y, float l, float h, int r, int g, int b, float a)
	{
		float eclairage = _etal.Eclairage;
		Carre(MathF.Floor(x), MathF.Floor(y), l, h, (int)((float)r * eclairage), (int)((float)g * eclairage), (int)((float)b * eclairage), a);
	}

	private static void DessinerEauDessus(FloatRect vue)
	{
		DessinerRayonsDessus(vue);
		for (int i = 0; i < _nbGerbesEau; i++)
		{
			ref GerbeEau reference = ref GerbesEau[i];
			if (Dedans(reference.X, reference.Y, vue, 30f))
			{
				DessinerGerbe(reference.X, reference.Y, reference.Age, reference.Taille);
			}
		}
		for (int j = 0; j < _nbGouttesEau; j++)
		{
			ref GoutteEau reference2 = ref GouttesEau[j];
			if (!(reference2.Age < 0f) && Dedans(reference2.X, reference2.Y - reference2.H, vue))
			{
				if (reference2.H > 3f)
				{
					Pt(reference2.X, reference2.Y, 11, 26, 42, 0.14f);
				}
				float a = Math.Clamp(1f - reference2.Age / 900f, 0.35f, 0.95f);
				if (reference2.Claire)
				{
					PtL(reference2.X, reference2.Y - reference2.H, 255, 255, 255, a);
				}
				else
				{
					PtL(reference2.X, reference2.Y - reference2.H, 191, 230, 255, a);
				}
			}
		}
		foreach (Banc value in Bancs.Values)
		{
			Poisson[] poissons = value.Poissons;
			foreach (Poisson poisson in poissons)
			{
				if (poisson.Saut >= 0f)
				{
					DessinerPoissonQuiSaute(value.Carte, poisson, vue);
				}
			}
		}
		DessinerGouttesDeCharge(vue);
	}

	private static void DessinerGerbe(float x, float y, float age, float taille)
	{
		int num = (int)(age / 55f);
		if (num > 6)
		{
			return;
		}
		float num2 = MonteeDeGerbe[num];
		(int, int, int, float, int, int)[] couchesDeGerbe = CouchesDeGerbe;
		for (int i = 0; i < couchesDeGerbe.Length; i++)
		{
			(int, int, int, float, int, int) tuple = couchesDeGerbe[i];
			for (int j = 0; j < LanguesDeGerbe.Length; j++)
			{
				(float Dx, float H) tuple2 = LanguesDeGerbe[j];
				float item = tuple2.Dx;
				float item2 = tuple2.H;
				int num3 = Math.Max(0, (int)MathF.Round(item2 * taille * num2 + (float)tuple.Item6));
				for (int k = 0; k < num3; k++)
				{
					float num4 = (float)k / (float)Math.Max(1, num3);
					int num5 = Math.Max(0, (int)MathF.Round((float)tuple.Item5 * (1f - num4)));
					int num6 = (((k + j) % 3 == 1) ? 1 : 0);
					CarreL(MathF.Round(x + item * (1f + (float)num * 0.08f)) - (float)num5 - (float)num6, MathF.Round(y) - (float)k, num5 * 2 + 1 + num6, 1f, tuple.Item1, tuple.Item2, tuple.Item3, tuple.Item4);
				}
			}
		}
	}

	private static void DessinerPoissonQuiSaute(MapInstance carte, Poisson p, FloatRect vue)
	{
		float num = Math.Clamp(p.Saut / 540f, 0f, 1f);
		float num2 = (float)carte.X + p.DepartX + (p.ArriveeX - p.DepartX) * num;
		float num3 = (float)carte.Y + p.DepartY + (p.ArriveeY - p.DepartY) * num - MathF.Sin((float)Math.PI * num) * 18f;
		if (!Dedans(num2, num3, vue, 12f))
		{
			return;
		}
		int num4 = ((p.ArriveeX >= p.DepartX) ? 1 : (-1));
		float x = (0f - MathF.Cos((float)Math.PI * num)) * 0.9f * (float)num4;
		float num5 = MathF.Cos(x);
		float num6 = MathF.Sin(x);
		for (int i = -6; i <= 6; i++)
		{
			for (int j = -9; j <= 9; j++)
			{
				float num7 = ((float)j * num5 + (float)i * num6) * (float)num4;
				float num8 = (float)(-j) * num6 + (float)i * num5;
				bool flag = num7 / 5f * (num7 / 5f) + num8 / 2f * (num8 / 2f) <= 1f;
				bool flag2 = num7 < -3.5f && num7 > -8f && MathF.Abs(num8) <= (0f - num7 - 3.5f) * 0.45f;
				if (flag || flag2)
				{
					if (flag && num8 < -0.8f && num7 > -2f && num7 < 2f)
					{
						PtL(num2 + (float)j, num3 + (float)i, 207, 232, 245, 1f);
					}
					else
					{
						PtL(num2 + (float)j, num3 + (float)i, 47, 95, 126, 1f);
					}
				}
			}
		}
	}

	public static bool KiSurLEau(object boule, float x, float y, float capX, float capY, float force, Color lumiere)
	{
		if (_enPanne || !_actif || !_initialise || !EauMonde(x, y))
		{
			return false;
		}
		try
		{
			long milliseconds = Timing.Global.Milliseconds;
			if (!DepartsKi.TryGetValue(boule, out DepartKi value))
			{
				value = new DepartKi
				{
					X = x,
					Y = y,
					ProchainsEmbruns = milliseconds
				};
				DepartsKi[boule] = value;
			}
			value.Vu = milliseconds;
			if (_texHalo != null)
			{
				float num = 0.85f + 0.15f * MathF.Sin((float)milliseconds / 60f);
				Texture(_texHalo, MathF.Round(x - 16f), MathF.Round(y - 5f), 32f, 10f, lumiere.R, lumiere.G, lumiere.B, 0.75f * force * num, GameBlendModes.Add);
				Texture(_texHalo, MathF.Round(x - 6f), MathF.Round(y - 2f), 12f, 4f, 255, 250, 225, 0.5f * force * num, GameBlendModes.Add);
			}
			float val = MathF.Sqrt((x - value.X) * (x - value.X) + (y - value.Y) * (y - value.Y));
			float num2 = Math.Min(70f, val);
			float num3 = 0f - capY;
			for (int i = -1; i <= 1; i += 2)
			{
				for (float num4 = 4f; num4 <= num2; num4 += 1f)
				{
					float num5 = (num4 - 4f) / 66f;
					float x2 = x - capX * num4 + num3 * (float)i * (1f + 4f * num5);
					float num6 = y - capY * num4 + capX * (float)i * (1f + 4f * num5) * 0.6f;
					if (EauMonde(x2, num6))
					{
						Carre(MathF.Floor(x2), MathF.Floor(num6), 1f, 1f, 240, 250, 255, 0.55f * (1f - num5 * 0.6f));
					}
				}
			}
			if (milliseconds >= value.ProchainsEmbruns)
			{
				value.ProchainsEmbruns = milliseconds + 240;
				ProjeterGouttes(x - capX * 10f, y - capY * 10f, 5, 0.6f);
			}
			return true;
		}
		catch (Exception exception)
		{
			Panne(exception);
			return false;
		}
	}

	public static bool GerbeDeKi(float x, float y, float taille = 16f)
	{
		if (_enPanne || !_actif || !_initialise || !EauMonde(x, y))
		{
			return false;
		}
		AjouterGerbe(x, y, taille);
		ProjeterGouttes(x, y - 4f, (int)(taille * 0.75f), 1.1f);
		for (int i = 0; i < 3; i++)
		{
			AjouterOnde(x, y, 1100f, taille * 1.4f + (float)(i * 6), 1f, -i * 220);
		}
		return true;
	}

	public static void ChargeDeKi(Entity entite, bool commence)
	{
		if (!_enPanne)
		{
			long milliseconds = Timing.Global.Milliseconds;
			ChargeEau value;
			if (commence)
			{
				ChargesEau[entite.Id] = new ChargeEau
				{
					Entite = entite,
					Debut = milliseconds,
					Graine = Hasard.Next(1000000)
				};
			}
			else if (ChargesEau.TryGetValue(entite.Id, out value) && value.Fin < 0)
			{
				value.Fin = milliseconds;
			}
		}
	}

	public static void ChargesEffacees()
	{
		ChargesEau.Clear();
	}

	private static void MajKiEau()
	{
		long milliseconds = Timing.Global.Milliseconds;
		_departsOublies.Clear();
		foreach (var (item, departKi2) in DepartsKi)
		{
			if (milliseconds - departKi2.Vu > 1000)
			{
				_departsOublies.Add(item);
			}
		}
		foreach (object departsOubly in _departsOublies)
		{
			DepartsKi.Remove(departsOubly);
		}
		_chargesFinies.Clear();
		foreach (var (item2, chargeEau2) in ChargesEau)
		{
			if ((chargeEau2.Fin >= 0 && milliseconds - chargeEau2.Fin > 1000) || chargeEau2.Entite.IsDisposed || milliseconds - chargeEau2.Debut > 600000)
			{
				_chargesFinies.Add(item2);
				continue;
			}
			if (chargeEau2.Fin < 0)
			{
				FloatRect worldPos = chargeEau2.Entite.WorldPos;
				chargeEau2.X = worldPos.X + worldPos.Width / 2f;
				chargeEau2.Y = worldPos.Y + worldPos.Height - 2f;
			}
			chargeEau2.AuBord = EauMonde(chargeEau2.X, chargeEau2.Y);
			for (int i = 0; i < 8; i++)
			{
				if (chargeEau2.AuBord)
				{
					break;
				}
				float x = (float)i / 8f * ((float)Math.PI * 2f);
				chargeEau2.AuBord = EauMonde(chargeEau2.X + MathF.Cos(x) * 36f, chargeEau2.Y + MathF.Sin(x) * 15f);
			}
		}
		foreach (Guid chargesFiny in _chargesFinies)
		{
			ChargesEau.Remove(chargesFiny);
		}
	}

	private static void DessinerChargesSurLEau(FloatRect vue)
	{
		long milliseconds = Timing.Global.Milliseconds;
		foreach (ChargeEau value in ChargesEau.Values)
		{
			if (!value.AuBord || !Dedans(value.X, value.Y, vue, 80f))
			{
				continue;
			}
			if (value.Fin < 0)
			{
				long num = milliseconds - value.Debut;
				float num2 = 34f + Math.Min(20f, (float)num * 0.004f);
				for (int i = 0; i < 4; i++)
				{
					float num3 = (float)((num + i * 330) % 1320) / 1320f;
					float num4 = 3f + num2 * Sortie(num3);
					TracerOnde(value.X, value.Y, num4, num4 * 0.42f, 0.495f * (1f - num3), vue);
				}
				continue;
			}
			long num5 = milliseconds - value.Fin;
			if (num5 >= 900)
			{
				continue;
			}
			float num6 = (float)num5 / 900f;
			float num7 = 26f * Sortie(Math.Min(1f, num6 * 3f)) * (1f - num6 * 0.6f);
			float num8 = 11f * (1f - num6 * 0.6f);
			for (int j = -(int)num8; j <= (int)num8; j++)
			{
				for (int k = -(int)num7; k <= (int)num7; k++)
				{
					if (!(num7 < 1f) && !((float)k / num7 * ((float)k / num7) + (float)j / num8 * ((float)j / num8) > 1f))
					{
						float x = value.X + (float)k;
						float y = value.Y + (float)j;
						if (EauMonde(x, y))
						{
							Carre(x, y, 1f, 1f, 10, 40, 80, 0.35f * (1f - num6));
						}
					}
				}
			}
			float num9 = 6f + 60f * Sortie(num6);
			for (int l = -1; l <= 1; l++)
			{
				TracerOnde(value.X, value.Y, num9 + (float)l, (num9 + (float)l) * 0.42f, 0.9f * (1f - num6), vue);
			}
		}
	}

	private static void DessinerGouttesDeCharge(FloatRect vue)
	{
		long milliseconds = Timing.Global.Milliseconds;
		float num = (float)_t;
		foreach (ChargeEau value in ChargesEau.Values)
		{
			if (!value.AuBord || !Dedans(value.X, value.Y, vue, 80f))
			{
				continue;
			}
			bool flag = value.Fin < 0;
			long num2 = (flag ? (-1) : (milliseconds - value.Fin));
			if (!flag && num2 >= 700)
			{
				continue;
			}
			uint etat = (uint)value.Graine;
			for (int i = 0; i < 30; i++)
			{
				float x = Alea(ref etat) * ((float)Math.PI * 2f);
				float num3 = 10f + Alea(ref etat) * 26f;
				float num4 = Alea(ref etat) * 1600f;
				float num5 = 10f + Alea(ref etat) * 24f;
				float num6 = Alea(ref etat) * 6f;
				float num7 = value.X + MathF.Cos(x) * num3;
				float num8 = value.Y + MathF.Sin(x) * num3 * 0.42f;
				if (!EauMonde(num7, num8))
				{
					continue;
				}
				float num9 = 0f;
				float num11;
				if (flag)
				{
					float num10 = (float)(milliseconds - value.Debut) - num4;
					if (num10 < 0f)
					{
						continue;
					}
					num11 = num5 * Sortie(Math.Min(1f, num10 / 900f)) + MathF.Sin(num * 0.006f + num6) * 1.5f;
				}
				else
				{
					float num12 = (float)num2 / 700f;
					num11 = num5 * (1f - num12 * num12) - num12 * 2f;
					if (num11 < 0f)
					{
						continue;
					}
					num9 = num12 * 18f;
				}
				int num13 = ((i % 5 != 0) ? 1 : 2);
				float x2 = num7 + MathF.Cos(x) * num9;
				if (i % 4 == 0)
				{
					CarreL(x2, num8 - num11, 1f, num13, 255, 255, 255, 0.95f);
				}
				else
				{
					CarreL(x2, num8 - num11, 1f, num13, 190, 230, 255, 0.9f);
				}
			}
			if (flag || num2 >= 600)
			{
				continue;
			}
			for (int j = 0; j < 8; j++)
			{
				float x3 = (float)j / 8f * ((float)Math.PI * 2f);
				float num14 = 10f + 48f * Sortie((float)num2 / 600f);
				float x4 = value.X + MathF.Cos(x3) * num14;
				float y = value.Y + MathF.Sin(x3) * num14 * 0.42f;
				if (EauMonde(x4, y))
				{
					DessinerGerbe(x4, y, num2, 8f);
				}
			}
		}
	}

	public static int DebutPersonnage(Entity entite, FloatRect corps)
	{
		if (_enPanne || !_actif || !_initialise || _zone == GenreZone.Interieur || corps.Height < 8f)
		{
			return 0;
		}
		try
		{
			long milliseconds = Timing.Global.Milliseconds;
			if (!Personnages.TryGetValue(entite, out Personnage value))
			{
				value = new Personnage
				{
					PiedsX = corps.X + corps.Width / 2f,
					PiedsY = corps.Y + corps.Height,
					Vu = milliseconds
				};
				Personnages[entite] = value;
			}
			float num = corps.X + corps.Width / 2f;
			float num2 = corps.Y + corps.Height;
			float num3 = num - value.PiedsX;
			float num4 = num2 - value.PiedsY;
			float num5 = MathF.Sqrt(num3 * num3 + num4 * num4);
			if (num5 > 0.3f && num5 < 24f)
			{
				value.DirX = num3 / num5;
				value.DirY = num4 / num5;
				value.BougeA = milliseconds;
			}
			long num6 = Math.Clamp(milliseconds - value.Vu, 0L, 200L);
			value.Vu = milliseconds;
			value.PiedsX = num;
			value.PiedsY = num2;
			value.Image = _imageEau;
			value.NbCouches = 0;
			float num7 = 0f;
			MapInstance carte;
			Analyse analyse = AnalyseEn(num, num2 - 2f, out carte);
			if (analyse != null && carte != null)
			{
				int num8 = (int)(num2 - 2f - (float)carte.Y) * analyse.L + (int)(num - (float)carte.X);
				if (analyse.Eau[num8] == 1)
				{
					num7 = Math.Clamp(2f + (float)(int)analyse.Dist[num8] * 0.5f, 2f, 11f);
				}
			}
			value.Immersion += (num7 - value.Immersion) * (1f - MathF.Exp((float)(-num6) / 90f));
			if (num7 == 0f && value.Immersion < 0.6f)
			{
				value.Immersion = 0f;
			}
			value.LignesImmergees = (int)MathF.Round(value.Immersion);
			value.PresDeLEau = value.LignesImmergees > 0;
			for (int i = 0; i < 6; i++)
			{
				if (value.PresDeLEau)
				{
					break;
				}
				float y = num2 + 2f + (float)(i * 8);
				value.PresDeLEau = EauMonde(num, y) || EauMonde(num - 8f, y) || EauMonde(num + 8f, y);
			}
			return value.LignesImmergees;
		}
		catch (Exception exception)
		{
			Panne(exception);
			return 0;
		}
	}

	public static void RetenirCouche(Entity entite, IGameTexture texture, FloatRect source, FloatRect destination, Color couleur)
	{
		if (!_enPanne && Personnages.TryGetValue(entite, out Personnage value) && value.Image == _imageEau && value.PresDeLEau && value.NbCouches < 8)
		{
			value.Couches[value.NbCouches++] = (Texture: texture, Source: source, Destination: destination, Alpha: couleur.A);
		}
	}

	public static void DessinerCouche(Entity entite, IGameTexture texture, FloatRect source, FloatRect destination, Color couleur, int immersion)
	{
		if (immersion <= 0 || _enPanne)
		{
			Graphics.DrawGameTexture(texture, source, destination, couleur);
		}
		else
		{
			int num = Math.Min(immersion, (int)source.Height);
			float num2 = destination.Height / source.Height;
			float num3 = source.Height - (float)num;
			if (num3 > 0f)
			{
				Graphics.DrawGameTexture(texture, new FloatRect(source.X, source.Y, source.Width, num3), new FloatRect(destination.X, destination.Y, destination.Width, num3 * num2), couleur);
			}
			DessinerImmerge(texture, source, destination, couleur, immersion);
		}
		RetenirCouche(entite, texture, source, destination, couleur);
	}

	public static void DessinerImmerge(IGameTexture texture, FloatRect source, FloatRect destination, Color couleur, int immersion)
	{
		if (immersion > 0 && !_enPanne && !(source.Height < 1f))
		{
			int num = Math.Min(immersion, (int)source.Height);
			float num2 = destination.Height / source.Height;
			Graphics.DrawGameTexture(texture, new FloatRect(source.X, source.Y + source.Height - (float)num, source.Width, num), new FloatRect(destination.X, destination.Y + destination.Height - (float)num * num2, destination.Width, (float)num * num2), new Color((int)(0.45f * (float)(int)couleur.A), 150, 200, 235));
		}
	}

	public static void FinPersonnage(Entity entite, FloatRect corps, int immersion)
	{
		if (immersion > 0 && !_enPanne)
		{
			float num = corps.X + corps.Width / 2f;
			float num2 = corps.Y + corps.Height - (float)immersion;
			float num3 = MathF.Round(MathF.Sin((float)_t * 0.01f + corps.X * 0.1f));
			float num4 = Math.Clamp((float)immersion / 4f, 0.4f, 1f);
			Carre(MathF.Round(num - 8f + num3), num2, 16f, 1f, 245, 252, 255, 0.85f * num4);
			Carre(MathF.Round(num - 10f - num3), num2 + 1f, 20f, 1f, 245, 252, 255, 0.45f * num4);
		}
	}

	private static void MajPersonnages()
	{
		_imageEau++;
		long milliseconds = Timing.Global.Milliseconds;
		_personnagesPartis.Clear();
		foreach (var (item, personnage2) in Personnages)
		{
			if (milliseconds - personnage2.Vu > 2000)
			{
				_personnagesPartis.Add(item);
			}
			else if (personnage2.LignesImmergees >= 2 && milliseconds - personnage2.BougeA < 150 && milliseconds >= personnage2.ProchainesGouttes)
			{
				personnage2.ProchainesGouttes = milliseconds + 180;
				ProjeterGouttes(personnage2.PiedsX - personnage2.DirX * 4f, personnage2.PiedsY - (float)personnage2.LignesImmergees, 3, 0.5f);
			}
		}
		foreach (Entity personnagesParti in _personnagesPartis)
		{
			Personnages.Remove(personnagesParti);
		}
	}

	private static void DessinerSillages(FloatRect vue)
	{
		long milliseconds = Timing.Global.Milliseconds;
		foreach (Personnage value in Personnages.Values)
		{
			if (value.LignesImmergees < 2 || value.Image < _imageEau - 2 || !Dedans(value.PiedsX, value.PiedsY, vue, 60f))
			{
				continue;
			}
			float num = value.PiedsY - (float)value.LignesImmergees + 1f;
			if (milliseconds - value.BougeA < 150)
			{
				float num2 = 0f - value.DirY;
				float dirX = value.DirX;
				for (int i = -1; i <= 1; i += 2)
				{
					for (int j = 6; j <= 42; j++)
					{
						float num3 = (float)(j - 6) / 36f;
						float x = value.PiedsX - value.DirX * (float)j + num2 * (float)i * (1f + 5f * num3);
						float num4 = num - value.DirY * (float)j * 0.7f + dirX * (float)i * (1f + 5f * num3) * 0.6f;
						if (EauMonde(x, num4))
						{
							Carre(MathF.Floor(x), MathF.Floor(num4), 1f, 1f, 240, 250, 255, 0.5f * (1f - num3 * 0.7f));
						}
					}
				}
				float num5 = (float)(milliseconds % 320) / 320f;
				float num6 = 3f + 7f * Sortie(num5);
				TracerOnde(value.PiedsX, num, num6, num6 * 0.42f, 0.44000003f * (1f - num5), vue);
			}
			else
			{
				for (int k = 0; k < 2; k++)
				{
					float num7 = (float)((milliseconds + k * 700) % 1400) / 1400f;
					float num8 = 3f + 12f * Sortie(num7);
					TracerOnde(value.PiedsX, num, num8, num8 * 0.42f, 0.385f * (1f - num7), vue);
				}
			}
		}
	}

	private static void DessinerRefletsPersonnages(FloatRect vue)
	{
		bool flag = false;
		foreach (Personnage value2 in Personnages.Values)
		{
			if (value2.PresDeLEau && value2.NbCouches > 0 && value2.Image >= _imageEau - 2 && Dedans(value2.PiedsX, value2.PiedsY, vue, 60f))
			{
				flag = true;
				break;
			}
		}
		if (!flag)
		{
			return;
		}
		GameRenderer renderer = Graphics.Renderer;
		if (renderer == null)
		{
			return;
		}
		IGameTexture whitePixel = renderer.WhitePixel;
		if (whitePixel == null)
		{
			return;
		}
		if (_toileReflets == null || _toileReflets.Width != renderer.ScreenWidth || _toileReflets.Height != renderer.ScreenHeight)
		{
			_toileReflets?.Dispose();
			_toileReflets = renderer.CreateRenderTexture(renderer.ScreenWidth, renderer.ScreenHeight);
		}
		IGameRenderTexture toileReflets = _toileReflets;
		toileReflets.Clear(Color.Transparent);
		float left = Graphics.CurrentView.Left;
		float top = Graphics.CurrentView.Top;
		float num = (float)_t;
		float num2 = 1f - _etal.Jour;
		Color color = new Color(255, 150, 190, 235);
		foreach (Personnage value3 in Personnages.Values)
		{
			if (!value3.PresDeLEau || value3.NbCouches == 0 || value3.Image < _imageEau - 2 || !Dedans(value3.PiedsX, value3.PiedsY, vue, 60f))
			{
				continue;
			}
			for (int i = 0; i < value3.NbCouches; i++)
			{
				(IGameTexture Texture, FloatRect Source, FloatRect Destination, byte Alpha) tuple = value3.Couches[i];
				IGameTexture item = tuple.Texture;
				FloatRect item2 = tuple.Source;
				FloatRect item3 = tuple.Destination;
				byte item4 = tuple.Alpha;
				int num3 = (int)item2.Height;
				if (num3 < 2)
				{
					continue;
				}
				float num4 = item3.Height / item2.Height;
				int num5 = Math.Min(value3.LignesImmergees, num3 - 1);
				float num6 = item3.Y + item3.Height - (float)num5 * num4;
				for (int j = num5; j < num3; j++)
				{
					int num7 = j - num5;
					float num8 = (float)num7 / (float)num3;
					float num9 = 0.62f * (0.85f - 0.77f * num8) * ((float)(int)item4 / 255f) * (1f - 0.25f * num2);
					if (!(num9 <= 0.01f))
					{
						float num10 = ((num7 > 4) ? MathF.Round(MathF.Sin(num * 0.005f + (float)num7 * 0.55f)) : 0f);
						color.A = (byte)(num9 * 255f);
						Graphics.DrawGameTexture(item, new FloatRect(item2.X, item2.Y + (float)num3 - 1f - (float)j, item2.Width, 1f), new FloatRect(MathF.Round(item3.X + num10 - left), MathF.Round(num6 + (float)num7 * num4 - top), item3.Width, num4), color, toileReflets);
					}
				}
			}
		}
		int num11 = Options.Instance.Map.TileWidth * Options.Instance.Map.MapWidth;
		int num12 = Options.Instance.Map.TileHeight * Options.Instance.Map.MapHeight;
		foreach (MapInstance visible in _visibles)
		{
			if (Analyses.TryGetValue(visible.Id, out Analyse value) && value.Carte == visible && value.AEau)
			{
				IGameTexture texSol = value.TexSol;
				if (texSol != null && ZoneDe(visible) != GenreZone.Interieur)
				{
					Graphics.DrawGameTexture(texSol, new FloatRect(0f, 0f, value.L, value.H), new FloatRect((float)visible.X - left, (float)visible.Y - top, value.L, value.H), Color.White, toileReflets, GameBlendModes.Cutout);
					continue;
				}
			}
			Graphics.DrawGameTexture(whitePixel, new FloatRect(0f, 0f, 1f, 1f), new FloatRect((float)visible.X - left, (float)visible.Y - top, num11, num12), Color.White, toileReflets, GameBlendModes.Cutout);
		}
		toileReflets.End();
		Graphics.DrawGameTexture(toileReflets, toileReflets.Bounds, vue, Color.White, null, GameBlendModes.Alpha);
	}

	internal static bool EstDeLEau(float x, float y)
	{
		if (_enPanne)
		{
			return false;
		}
		try
		{
			return EauMonde(x, y);
		}
		catch (Exception exception)
		{
			Panne(exception);
			return false;
		}
	}

	internal static byte[]? EauDeLaCarte(MapInstance carte, out int largeur)
	{
		largeur = 0;
		if (_enPanne || !Analyses.TryGetValue(carte.Id, out Analyse value) || value.Carte != carte || value.L <= 0 || value.Eau.Length != value.L * value.H || !value.AEau)
		{
			return null;
		}
		largeur = value.L;
		return value.Eau;
	}

	internal static ushort[]? HerbeDeLaCarte(MapInstance carte, out int largeur)
	{
		largeur = 0;
		if (_enPanne || !Analyses.TryGetValue(carte.Id, out Analyse value) || value.Carte != carte || value.L <= 0 || value.Eau.Length != value.L * value.H)
		{
			return null;
		}
		largeur = value.L;
		if (value.Herbe.Length != value.L * value.H)
		{
			return Array.Empty<ushort>();
		}
		return value.Herbe;
	}

	public static void RayonSurLEau(object rayon, float x0, float y0, float cos, float sin, float debut, float fin, float largeur, Color lumiere, float force, float k = 0f)
	{
		if (_enPanne || !_actif || !_initialise || fin <= debut)
		{
			return;
		}
		try
		{
			long milliseconds = Timing.Global.Milliseconds;
			OublierRayonsEau(milliseconds);
			if (!RayonsEau.TryGetValue(rayon, out RayonEau value))
			{
				Dictionary<object, RayonEau> rayonsEau = RayonsEau;
				RayonEau obj = new RayonEau
				{
					Tir = milliseconds
				};
				value = obj;
				rayonsEau[rayon] = obj;
			}
			value.Vu = milliseconds;
			value.Dessus = false;
			if (force < 0.999f && value.Arret == 0L)
			{
				value.Arret = milliseconds;
			}
			float num = -1f;
			float num2 = -1f;
			for (float num3 = MathF.Ceiling(MathF.Max(6f, debut)); num3 <= fin; num3 += 1f)
			{
				var (x1, y1, _, _) = ArcDuRayon.Point(x0, y0, cos, sin, k, num3);
				if (EauMonde(x1, y1))
				{
					if (num < 0f)
					{
						num = num3;
					}
					num2 = num3;
				}
			}
			value.Niveau = Math.Clamp((int)MathF.Round((largeur - 7f) / 4f), 0, 2);
			value.Demi = MathF.Max(3f, largeur * 0.75f);
			value.Ferme = ((value.Arret == 0L) ? 1f : (1f - Math.Clamp((float)(milliseconds - value.Arret) / 250f, 0f, 1f)));
			RayonEau rayonEau = value;
			RayonEau rayonEau2 = value;
			RayonEau rayonEau3 = value;
			RayonEau rayonEau4 = value;
			RayonEau rayonEau5 = value;
			RayonEau rayonEau6 = value;
			RayonEau rayonEau7 = value;
			float premier = num;
			float dernier = num2;
			rayonEau.X0 = x0;
			rayonEau2.Y0 = y0;
			rayonEau3.Cos = cos;
			rayonEau4.Sin = sin;
			rayonEau5.Force = force;
			rayonEau6.Premier = premier;
			rayonEau7.Dernier = dernier;
			value.K = k;
			if (num >= 0f)
			{
				value.OuvertDe = MathF.Min(value.OuvertDe, num);
				value.OuvertA = MathF.Max(value.OuvertA, num2);
			}
			if (value.Arret != 0L && !value.Retombe && value.OuvertDe <= value.OuvertA)
			{
				value.Retombe = true;
				Retomber(value, milliseconds);
			}
			if (!(num < 0f) && !(value.Ferme <= 0.01f))
			{
				DessinerTranchee(value, milliseconds, lumiere);
				value.Dessus = true;
				if (value.Arret == 0L)
				{
					Souffler(value, milliseconds, fin);
				}
			}
		}
		catch (Exception exception)
		{
			Panne(exception);
		}
	}

	public static bool GeyserDeRayon(float x, float y, int niveau)
	{
		if (!GerbeDeKi(x, y, 34f + 6f * (float)niveau))
		{
			return false;
		}
		AjouterGerbe(x - 6f, y + 2f, 24f);
		AjouterGerbe(x + 6f, y + 2f, 24f);
		ProjeterGouttes(x, y - 6f, 18, 1.5f);
		return true;
	}

	public static void DessinerTracesDesRayons()
	{
		if (_enPanne || !_actif || !_initialise || TracesEau.Count == 0)
		{
			return;
		}
		try
		{
			long maintenant = Timing.Global.Milliseconds;
			TracesEau.RemoveAll((TraceEau t) => (float)(maintenant - t.Debut) >= t.Vie);
			foreach (TraceEau item in TracesEau)
			{
				long num = maintenant - item.Debut;
				if (num >= 0)
				{
					if (item.Ecume)
					{
						DessinerEcumeDeRayon(item, num, maintenant);
					}
					else
					{
						DessinerVaguesDeRayon(item, num);
					}
				}
			}
		}
		catch (Exception exception)
		{
			Panne(exception);
		}
	}

	private static (bool Horizontal, float Pas, float Travers) Trame(float cos, float sin)
	{
		if (!(MathF.Abs(cos) >= MathF.Abs(sin)))
		{
			return (Horizontal: false, Pas: 1f / MathF.Max(0.01f, MathF.Abs(sin)), Travers: MathF.Abs((sin * sin + 0.5f * cos * cos) / sin));
		}
		return (Horizontal: true, Pas: 1f / MathF.Max(0.01f, MathF.Abs(cos)), Travers: MathF.Abs((0.5f * cos * cos + sin * sin) / cos));
	}

	private static float Gonflement(float u, float vague)
	{
		float num = (u % 90f - vague) / 8f;
		return MathF.Exp((0f - num) * num);
	}

	private static uint HacheEau(int a, int b)
	{
		uint num = (uint)(a * 374761393 + b * 668265263);
		num = (num ^ (num >> 13)) * 1274126177;
		return num ^ (num >> 16);
	}

	private static float AuHasardEau(int a, int b)
	{
		return (float)(HacheEau(a, b) % 10000) / 10000f;
	}

	private static void DessinerTranchee(RayonEau e, long maintenant, Color lumiere)
	{
		(bool Horizontal, float Pas, float Travers) tuple = Trame(e.Cos, e.Sin);
		bool item = tuple.Horizontal;
		float item2 = tuple.Pas;
		float item3 = tuple.Travers;
		int num = (int)(maintenant / 50);
		float vague = (float)(maintenant - e.Tir) * 0.3f % 90f - 10f;
		float num2 = MathF.Max(e.Force, 0.35f);
		if (_texHalo != null)
		{
			for (float num3 = e.Premier; num3 <= e.Dernier; num3 += 4f)
			{
				(float X, float Y, float Cos, float Sin) tuple2 = ArcDuRayon.Point(e.X0, e.Y0, e.Cos, e.Sin, e.K, num3);
				float item4 = tuple2.X;
				float item5 = tuple2.Y;
				float item6 = tuple2.Cos;
				float item7 = tuple2.Sin;
				float a = 0.3f * num2 * (0.8f + 0.4f * Gonflement(num3, vague)) * e.Ferme;
				if (MathF.Abs(item6) >= MathF.Abs(item7))
				{
					Texture(_texHalo, MathF.Round(item4 - 11f), MathF.Round(item5 - 8f), 22f, 16f, lumiere.R, lumiere.G, lumiere.B, a, GameBlendModes.Add);
				}
				else
				{
					Texture(_texHalo, MathF.Round(item4 - 8f), MathF.Round(item5 - 11f), 16f, 22f, lumiere.R, lumiere.G, lumiere.B, a, GameBlendModes.Add);
				}
			}
		}
		for (float num4 = e.Premier; num4 <= e.Dernier; num4 += item2)
		{
			var (x, num5, cos, sin) = ArcDuRayon.Point(e.X0, e.Y0, e.Cos, e.Sin, e.K, num4);
			(item, item2, item3) = Trame(cos, sin);
			if (!EauMonde(x, num5))
			{
				continue;
			}
			int num6 = (int)MathF.Floor(x);
			int num7 = (int)MathF.Floor(num5);
			float num8 = Gonflement(num4, vague);
			int num9 = (int)MathF.Round(e.Demi * (0.85f + 0.3f * num8) * e.Ferme * item3);
			if (num9 < 1)
			{
				continue;
			}
			float a2 = (0.45f + 0.4f * num8) * num2 * (0.75f + 0.25f * AuHasardEau((int)num4 >> 2, num));
			if (item)
			{
				Carre(num6, num7 - num9, 1f, 2 * num9 + 1, 10, 30, 62, 0.38f * num2);
				Carre(num6, num7 - num9 / 2, 1f, num9 + 1, 6, 22, 48, 0.3f * num2);
				Carre(num6, num7 - 1, 1f, 1f, 108, 203, 255, a2, GameBlendModes.Add);
				Carre(num6, num7, 1f, 1f, 207, 244, 255, (0.3f + 0.5f * num8) * num2, GameBlendModes.Add);
			}
			else
			{
				Carre(num6 - num9, num7, 2 * num9 + 1, 1f, 10, 30, 62, 0.38f * num2);
				Carre(num6 - num9 / 2, num7, num9 + 1, 1f, 6, 22, 48, 0.3f * num2);
				Carre(num6 - 1, num7, 1f, 1f, 108, 203, 255, a2, GameBlendModes.Add);
				Carre(num6, num7, 1f, 1f, 207, 244, 255, (0.3f + 0.5f * num8) * num2, GameBlendModes.Add);
			}
			for (int i = -1; i <= 1; i += 2)
			{
				int num10 = num9 + 1 + ((HacheEau((int)num4 >> 1, num * 3 + i + 7) % 3 == 0) ? 1 : 0);
				int num11 = (item ? num6 : (num6 + i * num10));
				int num12 = (item ? (num7 + i * num10) : num7);
				if (EauMonde(num11, num12))
				{
					Carre(num11, num12, 1f, 1f, 255, 255, 255, 0.95f * num2);
					if (item)
					{
						Carre(num11, num12 + i, 1f, 1f, 180, 225, 250, 0.7f * num2);
					}
					else
					{
						Carre(num11 + i, num12, 1f, 1f, 180, 225, 250, 0.7f * num2);
					}
					if (item ? (i < 0) : ((int)num4 % 3 == 0))
					{
						Rideau(e, num4, num11, num12, i, num8, num, maintenant, apresLaNuit: false);
					}
				}
			}
		}
	}

	private static void Rideau(RayonEau e, float u, int x, int pied, int cote, float g, int image, long maintenant, bool apresLaNuit)
	{
		float num = ((cote < 0) ? 9f : 7f) * (0.8f + 0.1f * (float)e.Niveau);
		float num2 = num * (0.5f + 0.5f * MathF.Sin(u * 0.41f + (float)maintenant * 0.018f + (float)cote * 1.3f)) * (0.75f + 0.5f * g) * e.Ferme + AuHasardEau((int)u, image) * 2f;
		int num3 = (int)num2;
		if (num3 >= 1)
		{
			float num4 = ((cote < 0) ? 0.75f : 0.6f) * MathF.Max(e.Force, 0.35f);
			int num5 = Math.Max(1, (int)((float)num3 * 0.45f));
			int num6 = Math.Max(0, num3 - num5 - 1);
			TraitDEmbruns(x, pied - num5, num5, 170, 220, 255, num4 * 0.85f, apresLaNuit);
			if (num6 > 0)
			{
				TraitDEmbruns(x, pied - num5 - num6, num6, 255, 255, 255, num4 * 0.6f, apresLaNuit);
			}
			if (AuHasardEau((int)u + 31, image) < 0.7f)
			{
				TraitDEmbruns(x, pied - num3, 1, 255, 255, 255, num4 * 0.3f, apresLaNuit);
			}
		}
	}

	private static void TraitDEmbruns(int x, int y, int h, int r, int g, int b, float a, bool apresLaNuit)
	{
		if (apresLaNuit)
		{
			CarreL(x, y, 1f, h, r, g, b, a);
		}
		else
		{
			Carre(x, y, 1f, h, r, g, b, a);
		}
	}

	private static void Souffler(RayonEau e, long maintenant, float fin)
	{
		float num = e.Dernier - e.Premier;
		if (maintenant >= e.ProchainsEmbruns)
		{
			e.ProchainsEmbruns = maintenant + 50;
			for (int i = 0; i < 4; i++)
			{
				float s = e.Premier + H01() * num;
				float num2 = ((i % 2 == 0) ? (-1f) : 1f);
				(float X, float Y, float Cos, float Sin) tuple = ArcDuRayon.Point(e.X0, e.Y0, e.Cos, e.Sin, e.K, s);
				float item = tuple.X;
				float item2 = tuple.Y;
				float item3 = tuple.Cos;
				float item4 = tuple.Sin;
				(bool Horizontal, float Pas, float Travers) tuple2 = Trame(item3, item4);
				bool item5 = tuple2.Horizontal;
				float item6 = tuple2.Travers;
				float num3 = e.Demi * item6 + 1f;
				Arracher(item5 ? item : (item + num2 * num3), item5 ? (item2 + num2 * num3) : item2, num2, item5, 12f + H01() * 18f, (90f + H01() * 80f) * e.Force);
			}
		}
		if (maintenant >= e.ProchaineVapeur)
		{
			e.ProchaineVapeur = maintenant + 85;
			float s2 = e.Premier + H01() * num;
			var (x, num4, _, _) = ArcDuRayon.Point(e.X0, e.Y0, e.Cos, e.Sin, e.K, s2);
			VapeursEau.Add(new VapeurEau
			{
				X = x,
				Y = num4 - 2f,
				Derive = (H01() - 0.5f) * 6f,
				Vie = 1300f + H01() * 500f,
				Debut = maintenant
			});
		}
		if (maintenant >= e.ProchaineVague)
		{
			e.ProchaineVague = maintenant + 380;
			TracesEau.Add(new TraceEau
			{
				X0 = e.X0,
				Y0 = e.Y0,
				Cos = e.Cos,
				Sin = e.Sin,
				K = e.K,
				De = e.Premier,
				A = e.Dernier,
				Vie = 1800f,
				Force = 0.75f,
				Debut = maintenant
			});
		}
		var (num5, num6, _, _) = ArcDuRayon.Point(e.X0, e.Y0, e.Cos, e.Sin, e.K, fin);
		if (maintenant >= e.ProchaineGerbe && EauMonde(num5, num6))
		{
			e.ProchaineGerbe = maintenant + 110;
			AjouterGerbe(num5 + (H01() - 0.5f) * 6f, num6 + 1f, 22f + 3f * (float)e.Niveau + H01() * 6f);
			ProjeterGouttes(num5, num6 - 2f, 6, 1.25f * e.Force);
		}
	}

	private static void Arracher(float x, float y, float cote, bool horizontal, float vitesse, float elan)
	{
		int nbGouttesEau = _nbGouttesEau;
		ProjeterGouttes(x, y, 1, 0.9f);
		if (_nbGouttesEau != nbGouttesEau)
		{
			ref GoutteEau reference = ref GouttesEau[_nbGouttesEau - 1];
			reference.Vx = (horizontal ? ((H01() - 0.5f) * 8f) : (cote * vitesse));
			reference.Vy = (horizontal ? (cote * vitesse) : ((H01() - 0.5f) * 8f));
			reference.Vh = elan;
		}
	}

	private static void Retomber(RayonEau e, long maintenant)
	{
		float ouvertDe = e.OuvertDe;
		float ouvertA = e.OuvertA;
		float num = ouvertA - ouvertDe;
		for (int i = 0; i < 46; i++)
		{
			float s = ouvertDe + H01() * num;
			float num2 = ((H01() < 0.5f) ? (-1f) : 1f);
			(float X, float Y, float Cos, float Sin) tuple = ArcDuRayon.Point(e.X0, e.Y0, e.Cos, e.Sin, e.K, s);
			float item = tuple.X;
			float item2 = tuple.Y;
			float item3 = tuple.Cos;
			float item4 = tuple.Sin;
			(bool Horizontal, float Pas, float Travers) tuple2 = Trame(item3, item4);
			bool item5 = tuple2.Horizontal;
			float item6 = tuple2.Travers;
			float num3 = e.Demi * item6;
			int nbGouttesEau = _nbGouttesEau;
			ProjeterGouttes(item5 ? item : (item + num2 * num3), item5 ? (item2 + num2 * num3) : item2, 1, 0.8f);
			if (_nbGouttesEau == nbGouttesEau)
			{
				break;
			}
			ref GoutteEau reference = ref GouttesEau[_nbGouttesEau - 1];
			reference.H = 4f + H01() * 8f;
			reference.Vh = 10f + H01() * 40f;
			if (item5)
			{
				reference.Vy = num2 * (4f + H01() * 10f);
			}
			else
			{
				reference.Vx = num2 * (4f + H01() * 10f);
			}
		}
		for (int j = 0; j < 5; j++)
		{
			float s2 = ouvertDe + H01() * num;
			var (x, y, _, _) = ArcDuRayon.Point(e.X0, e.Y0, e.Cos, e.Sin, e.K, s2);
			AjouterOnde(x, y, 1200f, 16f + H01() * 10f, 0.8f, -j * 90);
		}
		TracesEau.Add(new TraceEau
		{
			X0 = e.X0,
			Y0 = e.Y0,
			Cos = e.Cos,
			Sin = e.Sin,
			K = e.K,
			De = ouvertDe,
			A = ouvertA,
			Vie = 2000f,
			Force = 1f,
			Debut = maintenant
		});
		TracesEau.Add(new TraceEau
		{
			X0 = e.X0,
			Y0 = e.Y0,
			Cos = e.Cos,
			Sin = e.Sin,
			K = e.K,
			De = ouvertDe,
			A = ouvertA,
			Vie = 2600f,
			Force = 1f,
			Debut = maintenant + 250,
			Ecume = true
		});
	}

	private static void DessinerVaguesDeRayon(TraceEau t, long age)
	{
		float num = (float)age / t.Vie;
		float num2 = 4f + 26f * Sortie(num);
		float a = 0.65f * t.Force * (1f - num);
		int num3 = (int)(age / 60);
		for (int i = -1; i <= 1; i += 2)
		{
			int num4 = 0;
			float num5 = t.De - 4f;
			while (num5 <= t.A + 2f)
			{
				if ((num4 + num3) % 4 != 3)
				{
					float num6 = MathF.Sin(num5 * 0.22f + (float)age * 0.008f) * 0.8f;
					(float X, float Y, float Cos, float Sin) tuple = ArcDuRayon.Point(t.X0, t.Y0, t.Cos, t.Sin, t.K, num5);
					float item = tuple.X;
					float item2 = tuple.Y;
					float item3 = tuple.Cos;
					float item4 = tuple.Sin;
					float x = item - item4 * num2 * (float)i;
					float num7 = item2 + item3 * 0.5f * num2 * (float)i + num6;
					if (EauMonde(x, num7))
					{
						Carre(MathF.Floor(x), MathF.Floor(num7), 1f, 1f, 235, 248, 255, a);
					}
				}
				num5 += 1.5f;
				num4++;
			}
		}
		int num8 = (int)MathF.Ceiling(num2 * 3f);
		(float X, float Y, float Cos, float Sin) tuple2 = ArcDuRayon.Point(t.X0, t.Y0, t.Cos, t.Sin, t.K, t.A);
		float item5 = tuple2.X;
		float item6 = tuple2.Y;
		float item7 = tuple2.Cos;
		float item8 = tuple2.Sin;
		float num9 = 0f - item8;
		float num10 = item7 * 0.5f;
		for (int j = 0; j <= num8; j++)
		{
			float x2 = -(float)Math.PI / 2f + (float)j / (float)num8 * (float)Math.PI;
			float num11 = MathF.Cos(x2) * num2;
			float num12 = MathF.Sin(x2) * num2;
			float x3 = item5 + item7 * num11 + num9 * num12;
			float num13 = item6 + item8 * num11 + num10 * num12;
			if (EauMonde(x3, num13))
			{
				Carre(MathF.Floor(x3), MathF.Floor(num13), 1f, 1f, 235, 248, 255, a);
			}
		}
	}

	private static void DessinerEcumeDeRayon(TraceEau t, long age, long maintenant)
	{
		float num = (float)age / t.Vie;
		int b = (int)(maintenant / 80);
		(bool Horizontal, float Pas, float Travers) tuple = Trame(t.Cos, t.Sin);
		bool item = tuple.Horizontal;
		float item2 = tuple.Pas;
		float a = 0.85f * (1f - num * 0.6f);
		for (float num2 = t.De; num2 <= t.A; num2 += item2)
		{
			(float X, float Y, float Cos, float Sin) tuple2 = ArcDuRayon.Point(t.X0, t.Y0, t.Cos, t.Sin, t.K, num2);
			float item3 = tuple2.X;
			float item4 = tuple2.Y;
			float item5 = tuple2.Cos;
			float item6 = tuple2.Sin;
			(bool Horizontal, float Pas, float Travers) tuple3 = Trame(item5, item6);
			item = tuple3.Horizontal;
			item2 = tuple3.Pas;
			for (int i = -3; i <= 3; i++)
			{
				if (!(AuHasardEau((int)num2 * 7 + i, b) >= (1f - num) * (1f - MathF.Abs(i) / 4f) * 0.9f))
				{
					float x = (item ? item3 : (item3 + (float)i));
					float num3 = (item ? (item4 + (float)i) : item4);
					if (EauMonde(x, num3))
					{
						Carre(MathF.Floor(x), MathF.Floor(num3), 1f, 1f, 245, 252, 255, a);
					}
				}
			}
		}
	}

	private static void DessinerRayonsDessus(FloatRect vue)
	{
		long maintenant = Timing.Global.Milliseconds;
		foreach (RayonEau value in RayonsEau.Values)
		{
			if (!value.Dessus || maintenant - value.Vu > 100 || value.Premier < 0f)
			{
				continue;
			}
			int image = (int)(maintenant / 50);
			float vague = (float)(maintenant - value.Tir) * 0.3f % 90f - 10f;
			float num = 1f;
			for (float num2 = value.Premier; num2 <= value.Dernier; num2 += num)
			{
				var (x, num3, cos, sin) = ArcDuRayon.Point(value.X0, value.Y0, value.Cos, value.Sin, value.K, num2);
				bool flag;
				float num4;
				(flag, num, num4) = Trame(cos, sin);
				if (flag && EauMonde(x, num3))
				{
					float num5 = Gonflement(num2, vague);
					int num6 = (int)MathF.Round(value.Demi * (0.85f + 0.3f * num5) * value.Ferme * num4);
					int num7 = (int)MathF.Floor(x);
					int num8 = (int)MathF.Floor(num3) + num6 + 1;
					if (num6 >= 1 && EauMonde(num7, num8))
					{
						Rideau(value, num2, num7, num8, 1, num5, image, maintenant, apresLaNuit: true);
					}
				}
			}
		}
		VapeursEau.RemoveAll((VapeurEau v) => (float)(maintenant - v.Debut) >= v.Vie);
		if (_texHalo == null)
		{
			return;
		}
		float eclairage = _etal.Eclairage;
		foreach (VapeurEau item in VapeursEau)
		{
			float num9 = (float)(maintenant - item.Debut) / item.Vie;
			float num10 = 6f + 14f * num9;
			float num11 = 4f + 6f * num9;
			float num12 = item.X + item.Derive * num9;
			float num13 = item.Y - 22f * Sortie(num9);
			if (Dedans(num12, num13, vue, 30f))
			{
				float a = 0.26f * MathF.Sin((float)Math.PI * MathF.Min(1f, num9 * 1.4f)) * (1f - num9);
				Texture(_texHalo, MathF.Round(num12 - num10 / 2f), MathF.Round(num13 - num11 / 2f), num10, num11, (int)(236f * eclairage), (int)(244f * eclairage), (int)(250f * eclairage), a);
			}
		}
	}

	private static void OublierRayonsEau(long maintenant)
	{
		if (RayonsEau.Count == 0)
		{
			return;
		}
		_rayonsOublies.Clear();
		foreach (var (item, rayonEau2) in RayonsEau)
		{
			if (maintenant - rayonEau2.Vu > 1000)
			{
				_rayonsOublies.Add(item);
			}
		}
		foreach (object rayonsOubly in _rayonsOublies)
		{
			RayonsEau.Remove(rayonsOubly);
		}
	}

	public static bool SouffleDeVolSurLEau(float x, float y, float force = 1f)
	{
		if (_enPanne || !_actif || !_initialise || !EauMonde(x, y))
		{
			return false;
		}
		try
		{
			AjouterOnde(x, y, 1300f, 17f * force, 1.25f);
			AjouterOnde(x, y, 1300f, 10f * force, 1.25f, -260f);
			ProjeterGouttes(x, y - 2f, (int)MathF.Round(3f * force * force), 0.35f * force);
			return true;
		}
		catch (Exception exception)
		{
			Panne(exception);
			return false;
		}
	}

	internal static bool EstGrotte(MapInstance carte)
	{
		if (!_enPanne && carte.IsLoaded && ZoneDe(carte) == GenreZone.Interieur)
		{
			return carte.Brightness < 70;
		}
		return false;
	}

	internal static DonneesGrotte? GrotteDe(MapInstance carte)
	{
		if (!_enPanne)
		{
			return AnalyseDe(carte)?.Grotte;
		}
		return null;
	}

	internal static (int R, int G, int B, float Force) LumiereDuDehors()
	{
		long num = (long)_t;
		if (_dehorsCalculeA >= 0 && num - _dehorsCalculeA < 400)
		{
			return _dehors;
		}
		_dehorsCalculeA = num;
		Etalonnage etalonnage = Calculer(0f);
		Reglages reglages = CibleZone(GenreZone.Tempere, nommer: false);
		float num2 = Math.Clamp((reglages.Nuages - 0.35f) / 0.65f, 0f, 1f);
		float num3 = Math.Clamp(etalonnage.Jour, 0f, 1f);
		float item = (0.18f + 0.82f * num3) * Math.Clamp(1f - 0.4f * num2 - 0.25f * reglages.Pluie - 0.3f * reglages.Orage, 0.35f, 1f);
		_dehors = (R: (int)Math.Clamp(etalonnage.R, 0f, 255f), G: (int)Math.Clamp(etalonnage.G * (1f - 0.1f * num3), 0f, 255f), B: (int)Math.Clamp(etalonnage.B * (1f - 0.28f * num3), 0f, 255f), Force: item);
		return _dehors;
	}

	private static DonneesGrotte ConstruireGrotte(Analyse a, List<(byte[] Rgba, int PL, int PH, int Sx, int Sy, int W, int Hh, int Dx, int Dy)> posesBas, List<(byte[] Rgba, int PL, int PH, int Sx, int Sy, int W, int Hh, int Dx, int Dy)> posesHaut, byte[] cases, int tuileL, int tuileH)
	{
		int L = a.L;
		int H = a.H;
		int TX = L / tuileL;
		int TY = H / tuileH;
		byte[] px = new byte[L * H * 4];
		ComposerPoses(px, posesBas, L, H);
		ComposerPoses(px, posesHaut, L, H);
		DonneesGrotte donneesGrotte = new DonneesGrotte();
		Random random = new Random(a.Carte.Id.GetHashCode());
		List<(short X, short Haut, short Bas)> list = new List<(short X, short Haut, short Bas)>();
		for (int i = 2; i < L - 2; i += 3)
		{
			for (int j = 1; j < H - 3; j++)
			{
				if (!Vide(i, j - 1) || Vide(i, j) || Vide(i, j + 1) || Vide(i, j + 2))
				{
					continue;
				}
				int tx = i / tuileL;
				int num = -1;
				for (int k = j / tuileH; k <= j / tuileH + 2 && k < TY; k++)
				{
					if (Bloquee(tx, k))
					{
						num = k;
						break;
					}
				}
				if (num < 0)
				{
					continue;
				}
				int l;
				for (l = num; l + 1 < TY && l - num < 6 && Bloquee(tx, l + 1); l++)
				{
				}
				int num2 = (l + 1) * tuileH - 2;
				if (num2 - j < 20 || num2 - j > 220)
				{
					continue;
				}
				int num3 = 0;
				for (int m = j; m < num2; m++)
				{
					if (!Vide(i, m))
					{
						num3++;
					}
				}
				if ((float)num3 >= (float)(num2 - j) * 0.85f)
				{
					list.Add(((short)i, (short)j, (short)num2));
				}
			}
		}
		donneesGrotte.Parois = list.ToArray();
		List<List<(short, short, short)>> list2 = new List<List<(short, short, short)>>();
		foreach (var item in from tuple in list
			orderby tuple.X, tuple.Haut
			select tuple)
		{
			object obj;
			if (list2.Count <= 0)
			{
				obj = null;
			}
			else
			{
				obj = list2[list2.Count - 1];
			}
			List<(short, short, short)> list3 = (List<(short, short, short)>)obj;
			if (list3 != null)
			{
				if (item.Item1 - list3[list3.Count - 1].Item1 <= 6)
				{
					if (Math.Abs(item.Item2 - list3[list3.Count - 1].Item2) <= 10)
					{
						list3.Add(item);
						continue;
					}
				}
			}
			int num4 = 1;
			List<(short, short, short)> list4 = new List<(short, short, short)>(num4);
			CollectionsMarshal.SetCount(list4, num4);
			Span<(short, short, short)> span = CollectionsMarshal.AsSpan(list4);
			int num5 = 0;
			span[num5] = item;
			num5++;
			list2.Add(list4);
		}
		List<(short, short)[]> list5 = new List<(short, short)[]>();
		List<(int, int)> list6 = new List<(int, int)>();
		foreach (List<(short, short, short)> item2 in from s in list2
			where s.Count >= 10
			orderby s.Count descending
			select s)
		{
			(short X, short Haut, short Bas) milieu = item2[item2.Count / 2];
			if (list5.Count >= 3 || list6.Any<(int, int)>(((int X, int Y) c) => MathF.Sqrt((c.X - milieu.X) * (c.X - milieu.X) + (c.Y - milieu.Haut) * (c.Y - milieu.Haut)) < 180f))
			{
				continue;
			}
			List<(short, short)> list7 = new List<(short, short)>();
			int num6 = random.Next(0, Math.Max(1, item2.Count - 24));
			int num7 = 4 + random.Next(0, 4);
			for (int num8 = num6; num8 < item2.Count; num8 += 4)
			{
				if (list7.Count >= num7)
				{
					break;
				}
				list7.Add((item2[num8].Item1, (short)(item2[num8].Item2 + 2)));
			}
			if (list7.Count >= 3)
			{
				list5.Add(list7.ToArray());
				list6.Add((milieu.X, milieu.Haut));
			}
		}
		donneesGrotte.Colonies = list5.ToArray();
		List<(short, short)> list8 = new List<(short, short)>();
		foreach (var p in list)
		{
			int y = p.Item3 + 8;
			if (y < H && !Vide(p.Item1, y) && !Bloquee(p.Item1 / tuileL, y / tuileH) && !list8.Any<(short, short)>(((short X, short Y) o) => Math.Abs(o.X - p.Item1) < 24 && Math.Abs(o.Y - y) < 24))
			{
				list8.Add((p.Item1, (short)y));
				if (list8.Count >= 80)
				{
					break;
				}
			}
		}
		donneesGrotte.Gouttes = list8.ToArray();
		List<EntreeGrotte> list9 = new List<EntreeGrotte>();
		for (int num9 = 0; num9 < TY; num9++)
		{
			for (int num10 = 0; num10 < TX; num10++)
			{
				if (cases[num10 + num9 * TX] != 2)
				{
					continue;
				}
				int num11 = num10 * tuileL + tuileL / 2;
				int num12 = num9 * tuileH + tuileH / 2;
				int num13 = Math.Max(0, num11 - 48);
				int num14 = Math.Min(L - 1, num11 + 48);
				int num15 = Math.Max(0, num12 - 56);
				int num16 = Math.Min(H - 1, num12 + 56);
				byte[] array = new byte[(num14 - num13 + 1) * (num16 - num15 + 1)];
				int num17 = 0;
				int num18 = int.MaxValue;
				int num19 = int.MaxValue;
				int num20 = -1;
				int num21 = -1;
				long num22 = 0L;
				long num23 = 0L;
				for (int num24 = num15; num24 <= num16; num24++)
				{
					for (int num25 = num13; num25 <= num14; num25++)
					{
						int num26 = num24 * L + num25;
						int num27 = px[num26 * 4];
						int num28 = px[num26 * 4 + 2];
						if (num27 > num28 + 30)
						{
							float num29 = Lisse((Luma(num26) - 150f) / 60f) * ((num27 - num28 < 90) ? 1f : 0.3f);
							if (!(num29 <= 0.05f))
							{
								array[(num24 - num15) * (num14 - num13 + 1) + num25 - num13] = (byte)(num29 * 255f);
								num17++;
								num22 += num25;
								num23 += num24;
								num18 = Math.Min(num18, num25);
								num19 = Math.Min(num19, num24);
								num20 = Math.Max(num20, num25);
								num21 = Math.Max(num21, num24);
							}
						}
					}
				}
				if (num17 < 20)
				{
					continue;
				}
				int num30 = 0;
				int num31 = 0;
				int num32 = 0;
				int num33 = 0;
				for (int num34 = num12 - 56; num34 < num12 + 56; num34 += 2)
				{
					for (int num35 = num11 - 56; num35 < num11 + 56; num35 += 2)
					{
						if (Vide(num35, num34))
						{
							if (num34 > num12 + 8)
							{
								num31++;
							}
							if (num34 < num12 - 8)
							{
								num30++;
							}
							if (num35 < num11 - 8)
							{
								num32++;
							}
							if (num35 > num11 + 8)
							{
								num33++;
							}
						}
					}
				}
				int num36 = Math.Max(Math.Max(num30, num31), Math.Max(num32, num33));
				if (num36 < 40)
				{
					continue;
				}
				EntreeGrotte entreeGrotte = new EntreeGrotte();
				int num37 = (int)(num22 / num17);
				int num38 = (int)(num23 / num17);
				if (num36 == num31)
				{
					EntreeGrotte entreeGrotte2 = entreeGrotte;
					EntreeGrotte entreeGrotte3 = entreeGrotte;
					EntreeGrotte entreeGrotte4 = entreeGrotte;
					short x = (short)num37;
					short y2 = (short)(num19 + 8);
					entreeGrotte.Dx = 0;
					entreeGrotte2.Dy = -1;
					entreeGrotte3.X = x;
					entreeGrotte4.Y = y2;
				}
				else if (num36 == num30)
				{
					EntreeGrotte entreeGrotte4 = entreeGrotte;
					EntreeGrotte entreeGrotte3 = entreeGrotte;
					EntreeGrotte entreeGrotte2 = entreeGrotte;
					short y2 = (short)num37;
					short x = (short)(num21 - 8);
					entreeGrotte.Dx = 0;
					entreeGrotte4.Dy = 1;
					entreeGrotte3.X = y2;
					entreeGrotte2.Y = x;
				}
				else if (num36 == num32)
				{
					EntreeGrotte entreeGrotte2 = entreeGrotte;
					EntreeGrotte entreeGrotte3 = entreeGrotte;
					EntreeGrotte entreeGrotte4 = entreeGrotte;
					short x = (short)(num20 - 8);
					short y2 = (short)num38;
					entreeGrotte.Dx = 1;
					entreeGrotte2.Dy = 0;
					entreeGrotte3.X = x;
					entreeGrotte4.Y = y2;
				}
				else
				{
					EntreeGrotte entreeGrotte4 = entreeGrotte;
					EntreeGrotte entreeGrotte3 = entreeGrotte;
					EntreeGrotte entreeGrotte2 = entreeGrotte;
					short y2 = (short)(num18 + 8);
					short x = (short)num38;
					entreeGrotte.Dx = -1;
					entreeGrotte4.Dy = 0;
					entreeGrotte3.X = y2;
					entreeGrotte2.Y = x;
				}
				entreeGrotte.X0 = (short)num18;
				entreeGrotte.Y0 = (short)num19;
				entreeGrotte.L = (short)(num20 - num18 + 1);
				entreeGrotte.H = (short)(num21 - num19 + 1);
				entreeGrotte.Poids = new byte[entreeGrotte.L * entreeGrotte.H];
				for (int num39 = num19; num39 <= num21; num39++)
				{
					for (int num40 = num18; num40 <= num20; num40++)
					{
						entreeGrotte.Poids[(num39 - num19) * entreeGrotte.L + num40 - num18] = array[(num39 - num15) * (num14 - num13 + 1) + num40 - num13];
					}
				}
				list9.Add(entreeGrotte);
			}
		}
		donneesGrotte.Entrees = list9.ToArray();
		bool[] array2 = new bool[L * H];
		for (int num41 = 0; num41 < L * H; num41++)
		{
			float num42 = Luma(num41);
			array2[num41] = px[num41 * 4 + 3] > 0 && Sat(num41) <= 14 && num42 >= 100f && num42 <= 235f && !Vide(num41 % L, num41 / L);
		}
		bool[] array3 = new bool[L * H];
		Stack<int> stack = new Stack<int>();
		List<(short, short, byte, byte)> list10 = new List<(short, short, byte, byte)>();
		for (int num43 = 0; num43 < L * H; num43++)
		{
			if (list10.Count >= 12)
			{
				break;
			}
			if (!array2[num43] || array3[num43])
			{
				continue;
			}
			array3[num43] = true;
			stack.Push(num43);
			int num44 = 0;
			int num45 = int.MaxValue;
			int num46 = int.MaxValue;
			int num47 = -1;
			int num48 = -1;
			int num49 = 0;
			int num50 = 0;
			while (stack.Count > 0)
			{
				int num51 = stack.Pop();
				int num52 = num51 % L;
				int num53 = num51 / L;
				num44++;
				num45 = Math.Min(num45, num52);
				num46 = Math.Min(num46, num53);
				num47 = Math.Max(num47, num52);
				num48 = Math.Max(num48, num53);
				float num54 = Luma(num51);
				if (num54 > 195f)
				{
					num49++;
				}
				if (num54 < 130f)
				{
					num50++;
				}
				if (num52 > 0 && array2[num51 - 1] && !array3[num51 - 1])
				{
					array3[num51 - 1] = true;
					stack.Push(num51 - 1);
				}
				if (num52 < L - 1 && array2[num51 + 1] && !array3[num51 + 1])
				{
					array3[num51 + 1] = true;
					stack.Push(num51 + 1);
				}
				if (num53 > 0 && array2[num51 - L] && !array3[num51 - L])
				{
					array3[num51 - L] = true;
					stack.Push(num51 - L);
				}
				if (num53 < H - 1 && array2[num51 + L] && !array3[num51 + L])
				{
					array3[num51 + L] = true;
					stack.Push(num51 + L);
				}
			}
			int num55 = num47 - num45 + 1;
			int num56 = num48 - num46 + 1;
			if (num44 >= 120 && num44 <= 1600 && num55 >= 20 && num55 <= 90 && num56 >= 10 && num56 <= 50 && num55 >= num56 && num49 >= 4 && num50 >= 8 && (float)num44 >= (float)(num55 * num56) * 0.45f)
			{
				list10.Add(((short)((num45 + num47) / 2), (short)((num46 + num48) / 2), (byte)Math.Max(4, num55 / 2 - 3), (byte)Math.Max(3, num56 / 2 - 2)));
			}
		}
		donneesGrotte.Bassins = list10.ToArray();
		List<int> list11 = new List<int>();
		for (int num57 = 0; num57 < H; num57 += 3)
		{
			if (list11.Count >= 400)
			{
				break;
			}
			for (int num58 = 0; num58 < L; num58 += 3)
			{
				if (list11.Count >= 400)
				{
					break;
				}
				int num59 = num57 * L + num58;
				float num60 = Luma(num59);
				if (px[num59 * 4 + 3] > 0 && Sat(num59) <= 12 && num60 >= 55f && num60 <= 120f && !Vide(num58, num57))
				{
					list11.Add(num58 | (num57 << 16));
				}
			}
		}
		donneesGrotte.Cailloux = list11.ToArray();
		return donneesGrotte;
		bool Bloquee(int num61, int ty)
		{
			if (num61 >= 0 && ty >= 0 && num61 < TX && ty < TY)
			{
				return cases[num61 + ty * TX] == 1;
			}
			return false;
		}
		float Luma(int num61)
		{
			return (float)(int)px[num61 * 4] * 0.3f + (float)(int)px[num61 * 4 + 1] * 0.55f + (float)(int)px[num61 * 4 + 2] * 0.15f;
		}
		int Sat(int num61)
		{
			return Math.Max(px[num61 * 4], Math.Max(px[num61 * 4 + 1], px[num61 * 4 + 2])) - Math.Min(px[num61 * 4], Math.Min(px[num61 * 4 + 1], px[num61 * 4 + 2]));
		}
		bool Vide(int num61, int num62)
		{
			if (num61 < 0 || num62 < 0 || num61 >= L || num62 >= H)
			{
				return false;
			}
			int num63 = num62 * L + num61;
			if (px[num63 * 4 + 3] > 0 && Sat(num63) <= 6)
			{
				return Math.Max(px[num63 * 4], Math.Max(px[num63 * 4 + 1], px[num63 * 4 + 2])) <= 56;
			}
			return false;
		}
	}

	private static void ComposerPoses(byte[] px, List<(byte[] Rgba, int PL, int PH, int Sx, int Sy, int W, int Hh, int Dx, int Dy)> poses, int L, int H)
	{
		foreach (var pose in poses)
		{
			byte[] item = pose.Rgba;
			int item2 = pose.PL;
			int item3 = pose.PH;
			int item4 = pose.Sx;
			int item5 = pose.Sy;
			int item6 = pose.W;
			int item7 = pose.Hh;
			int item8 = pose.Rest.Item1;
			int item9 = pose.Rest.Item2;
			for (int i = 0; i < item7; i++)
			{
				int num = item9 + i;
				int num2 = item5 + i;
				if (num < 0 || num >= H || num2 < 0 || num2 >= item3)
				{
					continue;
				}
				for (int j = 0; j < item6; j++)
				{
					int num3 = item8 + j;
					int num4 = item4 + j;
					if (num3 >= 0 && num3 < L && num4 >= 0 && num4 < item2)
					{
						int num5 = (num2 * item2 + num4) * 4;
						byte b = item[num5 + 3];
						if (b != 0)
						{
							int num6 = (num * L + num3) * 4;
							float num7 = (float)(int)b / 255f;
							px[num6] = (byte)((float)(int)item[num5] * num7 + (float)(int)px[num6] * (1f - num7));
							px[num6 + 1] = (byte)((float)(int)item[num5 + 1] * num7 + (float)(int)px[num6 + 1] * (1f - num7));
							px[num6 + 2] = (byte)((float)(int)item[num5 + 2] * num7 + (float)(int)px[num6 + 2] * (1f - num7));
							px[num6 + 3] = Math.Max(px[num6 + 3], b);
						}
					}
				}
			}
		}
	}

	private static bool PixelHerbe(byte[] px, int i)
	{
		if (px[i * 4 + 3] != 0 && px[i * 4 + 1] + 10 >= px[i * 4] && px[i * 4 + 1] > px[i * 4 + 2] + 40)
		{
			return px[i * 4] + px[i * 4 + 1] + px[i * 4 + 2] > 90;
		}
		return false;
	}

	private static void ConstruireHerbe(Analyse a, byte[] px, byte[] eau, int L, int H)
	{
		int num = L * H;
		ushort[] herbe = new ushort[num];
		int num2 = 0;
		for (int i = 0; i < num; i++)
		{
			if (eau[i] == 0 && PixelHerbe(px, i))
			{
				ushort num3 = (ushort)((px[i * 4] >> 3 << 11) | (px[i * 4 + 1] >> 2 << 5) | (px[i * 4 + 2] >> 3));
				herbe[i] = (ushort)((num3 == 0) ? 1 : num3);
				num2++;
			}
		}
		if (num2 < num / 50)
		{
			return;
		}
		List<int> list = new List<int>();
		for (int j = 3; j < H - 3; j += 2)
		{
			if (list.Count >= 400)
			{
				break;
			}
			for (int k = 3; k < L - 3; k += 2)
			{
				if (list.Count >= 400)
				{
					break;
				}
				int num4 = j * L + k;
				if (eau[num4] == 0 && herbe[num4] == 0 && px[num4 * 4 + 3] != 0)
				{
					int num5 = px[num4 * 4];
					int num6 = px[num4 * 4 + 1];
					int num7 = px[num4 * 4 + 2];
					int num8 = Math.Max(num5, Math.Max(num6, num7));
					int num9 = Math.Min(num5, Math.Min(num6, num7));
					bool flag = num8 > 120 && (float)(num8 - num9) > (float)num8 * 0.55f && (num5 > num6 + 50 || num7 > num6 + 25);
					bool flag2 = num5 > 210 && num6 > 210 && num7 > 195;
					if ((flag || flag2) && (HerbeA(k - 3, j) || HerbeA(k + 3, j) || HerbeA(k, j - 3) || HerbeA(k, j + 3)))
					{
						list.Add(num4);
					}
				}
			}
		}
		a.PointsFleurs = list.ToArray();
		byte[] array = new byte[num * 4];
		for (int l = 0; l < num; l++)
		{
			if (herbe[l] != 0)
			{
				array[l * 4] = (byte)Math.Min(255f, (float)(int)px[l * 4] * 1.5f + 9f);
				array[l * 4 + 1] = (byte)Math.Min(255f, (float)(int)px[l * 4 + 1] * 1.5f + 11f);
				array[l * 4 + 2] = (byte)Math.Min(255f, (float)(int)px[l * 4 + 2] * 1.5f);
				array[l * 4 + 3] = byte.MaxValue;
			}
		}
		a.Herbe = herbe;
		a.RgbaHerbeClaire = array;
		bool HerbeA(int x, int y)
		{
			if (x >= 0 && y >= 0 && x < L && y < H)
			{
				return herbe[y * L + x] != 0;
			}
			return false;
		}
	}

	internal static Analyse? HerbeEn(float x, float y, out MapInstance? carte)
	{
		carte = null;
		if (_enPanne || !_actif || !_initialise)
		{
			return null;
		}
		int num = Options.Instance.Map.TileWidth * Options.Instance.Map.MapWidth;
		int num2 = Options.Instance.Map.TileHeight * Options.Instance.Map.MapHeight;
		foreach (MapInstance visible in _visibles)
		{
			if (!(x < (float)visible.X) && !(y < (float)visible.Y) && !(x >= (float)(visible.X + num)) && !(y >= (float)(visible.Y + num2)))
			{
				if (!Analyses.TryGetValue(visible.Id, out Analyse value) || value.Carte != visible || value.Herbe.Length != value.L * value.H)
				{
					return null;
				}
				carte = visible;
				return value;
			}
		}
		return null;
	}

	internal static void AjouterFleursDuSol(List<(float X, float Y)> sortie)
	{
		if (_enPanne || !_actif || !_initialise)
		{
			return;
		}
		foreach (MapInstance visible in _visibles)
		{
			if (Analyses.TryGetValue(visible.Id, out Analyse value) && value.Carte == visible)
			{
				int[] pointsFleurs = value.PointsFleurs;
				foreach (int num in pointsFleurs)
				{
					sortie.Add((visible.X + num % value.L, visible.Y + num / value.L));
				}
			}
		}
	}

	public static bool EstHerbe(float x, float y)
	{
		MapInstance carte;
		Analyse analyse = HerbeEn(x, y, out carte);
		if (analyse == null || carte == null)
		{
			return false;
		}
		int num = (int)(y - (float)carte.Y) * analyse.L + (int)(x - (float)carte.X);
		if (num >= 0 && num < analyse.Herbe.Length)
		{
			return analyse.Herbe[num] != 0;
		}
		return false;
	}

	public static void DessinerRafales()
	{
		if (_enPanne || !_actif || !_initialise || _zone == GenreZone.Interieur)
		{
			return;
		}
		try
		{
			FloatRect worldViewport = Graphics.WorldViewport;
			long milliseconds = Timing.Global.Milliseconds;
			float num = 0.5f + Math.Clamp(_p.Vent, 0f, 1f);
			foreach (MapInstance visible in _visibles)
			{
				if (!Analyses.TryGetValue(visible.Id, out Analyse value) || value.Carte != visible)
				{
					continue;
				}
				IGameTexture texHerbeClaire = value.TexHerbeClaire;
				if (texHerbeClaire == null)
				{
					continue;
				}
				int num2 = (int)Math.Max(0f, (worldViewport.Left - (float)visible.X) / 8f) * 8;
				int num3 = (int)Math.Max(0f, (worldViewport.Top - (float)visible.Y) / 8f) * 8;
				int num4 = (int)Math.Min(value.L, worldViewport.Right - (float)visible.X + 8f);
				int num5 = (int)Math.Min(value.H, worldViewport.Bottom - (float)visible.Y + 8f);
				List<(int, int, int, int, byte[])> list = Casse.MasquesDuSol(visible.Id);
				for (int i = num3; i < num5; i += 8)
				{
					for (int j = num2; j < num4; j += 8)
					{
						float num6 = (float)(visible.X + j) + 4f;
						float num7 = (float)(visible.Y + i) + 4f;
						float num8 = MathF.Pow(MathF.Max(0f, MathF.Sin(num6 * 0.045f + num7 * 0.02f - (float)milliseconds * 0.0021f)), 10f) * MathF.Max(0f, MathF.Sin(num6 * 0.011f - num7 * 0.017f + (float)milliseconds * 0.0004f) * 0.5f + 0.7f);
						float num9 = 0.22f * num8 * num;
						if (!(num9 < 0.012f) && (list == null || !Casse.SousUnSillon(list, j + 4, i + 4)))
						{
							Graphics.DrawGameTexture(texHerbeClaire, new FloatRect(j, i, Math.Min(8, value.L - j), Math.Min(8, value.H - i)), new FloatRect(visible.X + j, visible.Y + i, Math.Min(8, value.L - j), Math.Min(8, value.H - i)), new Color((int)(255f * Math.Min(1f, num9)), 255, 255, 255));
						}
					}
				}
			}
		}
		catch (Exception exception)
		{
			Panne(exception);
		}
	}

	public static void AjouterLampadaires(MapInstance carte)
	{
		if (_enPanne || !_actif || !_initialise || ZoneDe(carte) == GenreZone.Interieur || _etal.Lampes < 0.01f)
		{
			return;
		}
		try
		{
			Guid? guid = PlancheDesLampadaires();
			if (!guid.HasValue)
			{
				return;
			}
			Guid valueOrDefault = guid.GetValueOrDefault();
			float lampes = _etal.Lampes;
			int tileWidth = Options.Instance.Map.TileWidth;
			int tileHeight = Options.Instance.Map.TileHeight;
			long milliseconds = Timing.Global.Milliseconds;
			int num = 0;
			foreach (MapInstance item3 in CartesVisibles())
			{
				foreach (var item4 in LampadairesDe(item3, valueOrDefault))
				{
					int item = item4.X;
					int item2 = item4.Y;
					float num2 = Allumage(item3, item, item2, milliseconds);
					if (num2 <= 0f)
					{
						num++;
						continue;
					}
					float num3 = 0.94f + 0.06f * MathF.Sin((float)(_t * 0.009 + (double)num * 1.7)) * MathF.Sin((float)(_t * 0.0031 + (double)num));
					float num4 = lampes * num3 * num2;
					int x = item3.X + item * tileWidth + 33;
					int y = item3.Y + item2 * tileHeight + 42;
					Graphics.AddLight(x, y, 96, (byte)MathF.Min(255f, 215f * num4), 30f, Color.FromArgb(255, 255, 228, 170));
					Graphics.AddLight(x, item3.Y + item2 * tileHeight + 150, 84, (byte)MathF.Min(255f, 140f * num4), 20f, Color.FromArgb(255, 255, 214, 150));
					num++;
				}
			}
		}
		catch (Exception exception)
		{
			Panne(exception);
		}
	}

	private static float Allumage(MapInstance carte, int x, int y, long maintenant)
	{
		bool flag = Casse.LampadaireTombe(carte, x, y);
		(Guid, int, int) key = (carte.Id, x, y);
		if (!EtatsDesLampadaires.TryGetValue(key, out (bool, long, long) value) || maintenant - value.Item3 > 2000)
		{
			EtatsDesLampadaires[key] = (flag, long.MinValue, maintenant);
			if (!flag)
			{
				return 1f;
			}
			return 0f;
		}
		(bool, long, long) obj;
		if (value.Item1 == flag)
		{
			(bool, long, long) tuple = value;
			tuple.Item3 = maintenant;
			obj = tuple;
		}
		else
		{
			obj = (flag, maintenant, maintenant);
		}
		value = obj;
		EtatsDesLampadaires[key] = value;
		long age = ((value.Item2 == long.MinValue) ? long.MaxValue : (maintenant - value.Item2));
		if (!flag)
		{
			return Rallumage(age);
		}
		return Extinction(age);
	}

	private static float Extinction(long age)
	{
		if (age < 280)
		{
			if (age < 120)
			{
				if (age < 60)
				{
					return 1.35f;
				}
				return 0.15f;
			}
			if (age < 190)
			{
				return 0.9f;
			}
			return 0.1f;
		}
		if (age < 340)
		{
			return 0.45f;
		}
		return 0f;
	}

	private static float Rallumage(long age)
	{
		if (age < 330)
		{
			if (age >= 150)
			{
				if (age < 200)
				{
					return 0.6f;
				}
				return 0f;
			}
			return 0f;
		}
		if (age < 520)
		{
			if (age < 390)
			{
				return 0.85f;
			}
			return 0.2f;
		}
		if (age < 900)
		{
			return 0.6f + 0.4f * (float)(age - 520) / 380f;
		}
		return 1f;
	}

	private static Guid? PlancheDesLampadaires()
	{
		if (_plancheLampadaireCherchee)
		{
			return _plancheLampadaire;
		}
		foreach (IDatabaseObject value in DatabaseObject<TilesetDescriptor>.Lookup.Values)
		{
			if (value is TilesetDescriptor tilesetDescriptor && string.Equals(tilesetDescriptor.Name, "lampadaire.png", StringComparison.OrdinalIgnoreCase))
			{
				_plancheLampadaire = tilesetDescriptor.Id;
				break;
			}
		}
		_plancheLampadaireCherchee = _plancheLampadaire.HasValue || DatabaseObject<TilesetDescriptor>.Lookup.Count > 0;
		return _plancheLampadaire;
	}

	private static List<(int X, int Y)> LampadairesDe(MapInstance carte, Guid planche)
	{
		if (LampadairesParCarte.TryGetValue(carte.Id, out (MapInstance, List<(int, int)>) value) && value.Item1 == carte)
		{
			return value.Item2;
		}
		List<(int, int)> list = new List<(int, int)>();
		foreach (var (_, array2) in carte.Layers)
		{
			if (array2 == null)
			{
				continue;
			}
			int length = array2.GetLength(0);
			int length2 = array2.GetLength(1);
			for (int i = 0; i < length2; i++)
			{
				for (int j = 0; j < length; j++)
				{
					Tile tile = array2[j, i];
					if (tile.TilesetId == planche && tile.X == 0 && tile.Y == 0)
					{
						list.Add((j, i));
					}
				}
			}
		}
		LampadairesParCarte[carte.Id] = (carte, list);
		return list;
	}

	private static float Lisse(float k)
	{
		k = Math.Clamp(k, 0f, 1f);
		return k * k * (3f - 2f * k);
	}

	private static float Lerp(float a, float b, float k)
	{
		return a + (b - a) * k;
	}

	internal static Etalonnage Calculer(float flash)
	{
		float num = (Heure % 24f + 24f) % 24f;
		int i;
		for (i = 0; i < Cles.Length - 2 && Cles[i + 1].Heure <= num; i++)
		{
		}
		(float, float, float, float, float, float, float, float, float, float) tuple = Cles[i];
		(float, float, float, float, float, float, float, float, float, float) tuple2 = Cles[i + 1];
		float k = Lisse((num - tuple.Item1) / (tuple2.Item1 - tuple.Item1));
		Etalonnage result = new Etalonnage
		{
			R = Lerp(tuple.Item2, tuple2.Item2, k),
			G = Lerp(tuple.Item3, tuple2.Item3, k),
			B = Lerp(tuple.Item4, tuple2.Item4, k),
			VoileR = Lerp(tuple.Item5, tuple2.Item5, k),
			VoileG = Lerp(tuple.Item6, tuple2.Item6, k),
			VoileB = Lerp(tuple.Item7, tuple2.Item7, k),
			VoileA = Lerp(tuple.Rest.Item1, tuple2.Rest.Item1, k),
			Desaturation = Lerp(tuple.Rest.Item2, tuple2.Rest.Item2, k),
			Lampes = Lerp(tuple.Rest.Item3, tuple2.Rest.Item3, k)
		};
		Reglages p = _p;
		float num2 = Math.Clamp((p.Nuages - 0.35f) / 0.65f, 0f, 1f);
		float num3 = num2 * 0.75f;
		float num4 = p.Pluie * 0.75f;
		float num5 = p.Orage * 0.75f;
		float num6 = p.Brume * 0.75f;
		float num7 = p.Sable * 0.75f;
		float num8 = result.R * 0.3f + result.G * 0.55f + result.B * 0.15f;
		float k2 = Math.Min(0.85f, num3 * 0.75f + num4 * 0.35f + num6 * 0.45f);
		result.R = Lerp(result.R, num8 * 0.93f, k2);
		result.G = Lerp(result.G, num8 * 0.98f, k2);
		result.B = Lerp(result.B, Math.Min(255f, num8 * 1.1f), k2);
		float num9 = 0.35f + 0.65f * (1f - result.Lampes);
		result.R *= M(0.86f, num3) * M(0.8f, num4 * num9) * M(0.62f, num5 * num9) * M(0.96f, num6) * M(0.94f, p.Froid);
		result.G *= M(0.88f, num3) * M(0.84f, num4 * num9) * M(0.66f, num5 * num9) * M(0.97f, num6) * M(0.9f, num7) * M(0.98f, p.Chaleur) * M(0.98f, p.Froid);
		result.B *= M(0.93f, num3) * M(0.9f, num4 * num9) * M(0.78f, num5 * num9) * M(0.98f, num6) * M(0.72f, num7) * M(0.9f, p.Chaleur);
		result.B = Math.Min(255f, result.B * M(1.05f, p.Froid));
		float num10 = _enneigement * (0.35f + 0.65f * result.Lampes) * ((_zone == GenreZone.Montagne) ? 1f : 0f);
		result.R = Math.Min(255f, result.R + 26f * num10);
		result.G = Math.Min(255f, result.G + 30f * num10);
		result.B = Math.Min(255f, result.B + 40f * num10);
		result.Desaturation = Math.Min(0.8f, result.Desaturation + 0.26f * num3 + 0.12f * num4 + 0.16f * num5 + 0.22f * num6 + 0.28f * num7 + 0.1f * p.Froid);
		(float R, float G, float B) tuple3 = TeinteDuLieu(_zone);
		float item = tuple3.R;
		float item2 = tuple3.G;
		float item3 = tuple3.B;
		float num11 = 1f - MathF.Exp((0f - (float)(_t - _tTeinte)) / 600f);
		_tTeinte = _t;
		_teinteR += (item - _teinteR) * num11;
		_teinteG += (item2 - _teinteG) * num11;
		_teinteB += (item3 - _teinteB) * num11;
		result.R *= _teinteR;
		result.G *= _teinteG;
		result.B *= _teinteB;
		result.R = Lerp(255f, result.R, 0.8f);
		result.G = Lerp(255f, result.G, 0.8f);
		result.B = Lerp(255f, result.B, 0.8f);
		result.VoileA *= 0.8f;
		result.Desaturation *= 0.8f;
		float num12 = (result.R * 0.3f + result.G * 0.55f + result.B * 0.15f) / 255f;
		result.Lampes = Math.Max(result.Lampes, Math.Clamp((0.52f - num12) * 3f, 0f, 1f) * 0.9f);
		result.Jour = 1f - Lerp(tuple.Rest.Item3, tuple2.Rest.Item3, k);
		result.R = Lerp(result.R, 232f, flash * 0.75f);
		result.G = Lerp(result.G, 240f, flash * 0.75f);
		result.B = Lerp(result.B, 255f, flash * 0.75f);
		result.Desaturation *= 1f - flash;
		float num13 = (1f - num2) * (1f - p.Pluie) * (1f - p.Brume * 0.8f) * (1f - p.Sable * 0.8f) * (1f - p.Neige);
		result.Soleil = result.Jour * num13;
		result.NuitClaire = (1f - result.Jour) * num13;
		result.Coucher = Math.Max(Cloche(num, 19f, 1.3f), Cloche(num, 6f, 1f)) * num13;
		result.Eclairage = Math.Min(1.15f, (result.R * 0.3f + result.G * 0.55f + result.B * 0.15f) / 255f * 0.6f + 0.45f + flash * 0.5f);
		return result;
		static float M(float v, float x)
		{
			return Lerp(1f, v, x);
		}
	}

	private static (float R, float G, float B) TeinteDuLieu(GenreZone zone)
	{
		return zone switch
		{
			GenreZone.Foret => (R: 0.92f, G: 0.99f, B: 0.86f), 
			GenreZone.Tempere => (R: 0.99f, G: 1f, B: 0.95f), 
			GenreZone.Desert => (R: 1f, G: 0.975f, B: 0.9f), 
			GenreZone.Ile => (R: 0.965f, G: 0.99f, B: 1f), 
			GenreZone.Montagne => (R: 0.95f, G: 0.975f, B: 1f), 
			_ => (R: 1f, G: 1f, B: 1f), 
		};
	}

	public static Color? CouleurDuMonde(MapInstance carte)
	{
		if (_enPanne || !_actif || !_initialise || ZoneDe(carte) == GenreZone.Interieur)
		{
			return null;
		}
		_etal = Calculer(Flash());
		return new Color(255, (int)Math.Clamp(_etal.R, 0f, 255f), (int)Math.Clamp(_etal.G, 0f, 255f), (int)Math.Clamp(_etal.B, 0f, 255f));
	}

	public static void AjouterLumieres(MapInstance carte)
	{
		if (_enPanne || !_actif || !_initialise || ZoneDe(carte) == GenreZone.Interieur || _etal.Lampes < 0.01f)
		{
			return;
		}
		try
		{
			float lampes = _etal.Lampes;
			Player me = Globals.Me;
			if (me != null)
			{
				Graphics.AddLight((int)me.Center.X, (int)me.Center.Y - 4, 110, (byte)(125f * lampes), 40f, Color.FromArgb(255, 255, 244, 222));
			}
			foreach (MapInstance item in CartesVisibles())
			{
				int num = 0;
				foreach (Vitre item2 in VitresDe(item))
				{
					if (Casse.TombeIci(item, item2.X, item2.Y, item2.Largeur, item2.Hauteur))
					{
						num++;
						continue;
					}
					float num2 = 0.93f + 0.07f * MathF.Sin((float)(_t * 0.011 + (double)num * 2.1)) * MathF.Sin((float)(_t * 0.0037 + (double)num));
					float num3 = Math.Clamp(lampes * item2.Force * num2, 0f, 1f);
					float num4 = (float)item.X + item2.X + item2.Largeur / 2f;
					float num5 = (float)item.Y + item2.Y + item2.Hauteur / 2f;
					Graphics.AddLight((int)num4, (int)num5, (int)(40f + item2.Largeur), (byte)(255f * num3), 35f, Color.FromArgb(255, 255, 186, 100));
					Graphics.AddLight((int)num4, (int)(num5 + item2.Hauteur + 12f), (int)(28f + item2.Largeur * 0.8f), (byte)(150f * num3), 20f, Color.FromArgb(255, 255, 178, 92));
					num++;
				}
			}
		}
		catch (Exception exception)
		{
			Panne(exception);
		}
	}

	internal static bool PlancheDeTerrain(IGameTexture planche)
	{
		return PlanchesDeTerrain.Contains(Path.GetFileName(planche.Name ?? ""));
	}

	internal static bool TuileDeTerrain(IGameTexture planche, int colonne, int rangee)
	{
		if (!PlancheDeTerrain(planche))
		{
			if (string.Equals(Path.GetFileName(planche.Name ?? ""), "majeur.png", StringComparison.OrdinalIgnoreCase))
			{
				if (rangee < 19 || rangee > 30 || colonne < 0 || colonne > 11)
				{
					if (rangee >= 31 && rangee <= 33)
					{
						if (colonne >= 0)
						{
							return colonne <= 5;
						}
						return false;
					}
					return false;
				}
				return true;
			}
			return false;
		}
		return true;
	}

	internal static bool[]? TuilesDeSol(MapInstance carte, int largeur, int hauteur)
	{
		List<string> lowerLayers = Options.Instance.Map.Layers.LowerLayers;
		if (lowerLayers.Count >= 2)
		{
			Dictionary<string, Autotile[,]> dictionary = carte.Autotiles?.Layers;
			if (dictionary != null)
			{
				int tileWidth = Options.Instance.Map.TileWidth;
				int tileHeight = Options.Instance.Map.TileHeight;
				bool[] array = new bool[largeur * hauteur];
				bool[] array2 = new bool[largeur * hauteur];
				bool[] array3 = new bool[largeur * hauteur];
				if (carte.Layers.TryGetValue(lowerLayers[lowerLayers.Count - 1], out var value))
				{
					if (dictionary.TryGetValue(lowerLayers[lowerLayers.Count - 1], out var value2))
					{
						for (int i = 0; i < largeur; i++)
						{
							for (int j = 0; j < hauteur; j++)
							{
								Tile tile = value[i, j];
								array[i + j * largeur] = tile.TilesetTexture is IGameTexture gameTexture && tile.X >= 0 && tile.Y >= 0 && tile.X * tileWidth < gameTexture.Width && tile.Y * tileHeight < gameTexture.Height && value2[i, j].RenderState == 1;
							}
						}
					}
				}
				foreach (string item in Options.Instance.Map.Layers.MiddleLayers.Concat(Options.Instance.Map.Layers.UpperLayers))
				{
					if (!carte.Layers.TryGetValue(item, out var value3) || !dictionary.TryGetValue(item, out var value4))
					{
						continue;
					}
					for (int k = 0; k < largeur; k++)
					{
						for (int l = 0; l < hauteur; l++)
						{
							if (value3[k, l].TilesetTexture is IGameTexture && value4[k, l].RenderState != 0)
							{
								array2[k + l * largeur] = true;
							}
						}
					}
				}
				MapAttribute[,] attributes = carte.Attributes;
				if (attributes != null && attributes.GetLength(0) >= largeur && attributes.GetLength(1) >= hauteur)
				{
					for (int m = 0; m < largeur; m++)
					{
						for (int n = 0; n < hauteur; n++)
						{
							bool[] array4 = array3;
							int num = m + n * largeur;
							MapAttribute mapAttribute = attributes[m, n];
							bool flag = ((mapAttribute is MapBlockedAttribute || mapAttribute is MapWarpAttribute) ? true : false);
							array4[num] = flag;
						}
					}
				}
				return AmasDeSol(array, array2, array3, largeur, hauteur);
			}
		}
		return null;
	}

	internal static bool[] AmasDeSol(bool[] posees, bool[] dessus, bool[] fermees, int largeur, int hauteur)
	{
		bool[] array = new bool[largeur * hauteur];
		bool[] vues = new bool[largeur * hauteur];
		List<int> list = new List<int>();
		Stack<int> pile = new Stack<int>();
		for (int i = 0; i < array.Length; i++)
		{
			if (vues[i] || (!posees[i] && !dessus[i]))
			{
				continue;
			}
			bool flag = false;
			list.Clear();
			vues[i] = true;
			pile.Push(i);
			while (pile.Count > 0)
			{
				int num = pile.Pop();
				list.Add(num);
				flag |= dessus[num] || (posees[num] && fermees[num]);
				int num2 = num % largeur;
				if (num2 > 0)
				{
					Voisine(num - 1);
				}
				if (num2 < largeur - 1)
				{
					Voisine(num + 1);
				}
				if (num >= largeur)
				{
					Voisine(num - largeur);
				}
				if (num < array.Length - largeur)
				{
					Voisine(num + largeur);
				}
			}
			if (flag)
			{
				continue;
			}
			foreach (int item in list)
			{
				array[item] = true;
			}
		}
		return array;
		void Voisine(int j)
		{
			if (!vues[j] && (posees[j] || dessus[j]))
			{
				vues[j] = true;
				pile.Push(j);
			}
		}
	}

	private static int Silhouette(float[] grille, int L2, int H2, List<(byte[] Rgba, int PL, int PH, int Sx, int Sy, int W, int Hh, int Dx, int Dy)> poses)
	{
		int num = 1;
		int num2 = 0;
		foreach (var pose in poses)
		{
			byte[] item = pose.Rgba;
			int item2 = pose.PL;
			int item3 = pose.PH;
			int item4 = pose.Sx;
			int item5 = pose.Sy;
			int item6 = pose.W;
			int item7 = pose.Hh;
			int item8 = pose.Rest.Item1;
			int item9 = pose.Rest.Item2;
			for (int i = 0; i < item7; i++)
			{
				int num3 = item5 + i;
				int num4 = (item9 + i + 32) / num;
				if (num3 < 0 || num3 >= item3 || num4 < 0 || num4 >= H2)
				{
					continue;
				}
				for (int j = 0; j < item6; j++)
				{
					int num5 = item4 + j;
					int num6 = (item8 + j + 32) / num;
					if (num5 >= 0 && num5 < item2 && num6 >= 0 && num6 < L2 && item[(num3 * item2 + num5) * 4 + 3] > 40)
					{
						grille[num4 * L2 + num6] = 1f;
						num2++;
					}
				}
			}
		}
		return num2;
	}

	private static void ConstruireOmbres(Analyse a, List<(byte[] Rgba, int PL, int PH, int Sx, int Sy, int W, int Hh, int Dx, int Dy)> debout, List<(byte[] Rgba, int PL, int PH, int Sx, int Sy, int W, int Hh, int Dx, int Dy)> masque, List<(byte[] Rgba, int PL, int PH, int Sx, int Sy, int W, int Hh, int Dx, int Dy)> sol)
	{
		int num = 1;
		int num2 = (a.L + 64) / num;
		int num3 = (a.H + 64) / num;
		a.OmbresL = num2;
		a.OmbresH = num3;
		float[] array = new float[num2 * num3];
		Silhouette(array, num2, num3, masque);
		byte[] array2 = new byte[num2 * num3 * 4];
		byte[] array3 = ((a.Eau.Length == a.L * a.H) ? a.Eau : null);
		for (int i = 0; i < num3; i++)
		{
			for (int j = 0; j < num2; j++)
			{
				int num4 = j * num - 32;
				int num5 = i * num - 32;
				if (num4 >= 0 && num5 >= 0 && num4 < a.L && num5 < a.H)
				{
					int num6 = i * num2 + j;
					if (!(array[num6] > 0f) && (array3 == null || array3[num5 * a.L + num4] == 0))
					{
						int num7 = num6 * 4;
						int num8 = num6 * 4 + 1;
						int num9 = num6 * 4 + 2;
						byte b;
						array2[num6 * 4 + 3] = (b = byte.MaxValue);
						array2[num7] = (array2[num8] = (array2[num9] = b));
					}
				}
			}
		}
		a.RgbaSolLibre = array2;
		Silhouette(array, num2, num3, sol);
		float[] array4 = new float[num2 * num3];
		List<(byte[], int, int, int, int, int, int, int, int)> list = new List<(byte[], int, int, int, int, int, int, int, int)>(debout.Count);
		foreach (var item8 in debout)
		{
			(byte[] Rgba, int PL, int PH, int Sx, int Sy, int W, int Hh, int Dx, int Dy) tuple = item8;
			byte[] item = tuple.Rgba;
			int item2 = tuple.PL;
			int item3 = tuple.PH;
			int item4 = tuple.Sx;
			int item5 = tuple.Sy;
			int item6 = tuple.W;
			int item7 = tuple.Hh;
			int num10 = 0;
			int num11 = 0;
			for (int k = 0; k < item7; k++)
			{
				int num12 = item5 + k;
				if (num12 < 0 || num12 >= item3)
				{
					continue;
				}
				for (int l = 0; l < item6; l++)
				{
					int num13 = item4 + l;
					if (num13 >= 0 && num13 < item2)
					{
						byte b2 = item[(num12 * item2 + num13) * 4 + 3];
						if (b2 <= 40)
						{
							num10++;
						}
						else if (b2 >= 200)
						{
							num11++;
						}
					}
				}
			}
			if (num10 > 0 && num11 > 0)
			{
				list.Add(item8);
			}
		}
		if (Silhouette(array4, num2, num3, list) == 0)
		{
			return;
		}
		int num14 = 8 / num;
		int num15 = 6 / num;
		byte[] array5 = new byte[num2 * num3 * 4];
		for (int m = num15; m < num3; m++)
		{
			for (int n = num14; n < num2; n++)
			{
				int num16 = m * num2 + n;
				if (!(array4[(m - num15) * num2 + n - num14] <= 0f) && !(array4[num16] > 0f) && !(array[num16] > 0f))
				{
					array5[num16 * 4] = 40;
					array5[num16 * 4 + 1] = 36;
					array5[num16 * 4 + 2] = 58;
					array5[num16 * 4 + 3] = byte.MaxValue;
				}
			}
		}
		a.RgbaOmbres = array5;
	}

	private static void DessinerOmbresPortees()
	{
		float a = 0.26f * (0.28f + 0.72f * _etal.Soleil);
		foreach (MapInstance visible in _visibles)
		{
			if (Analyses.TryGetValue(visible.Id, out Analyse value) && value.Carte == visible)
			{
				IGameTexture texOmbres = value.TexOmbres;
				if (texOmbres != null && ClimatDe(GenreDuCiel(visible)) != Climat.Interieur)
				{
					Texture(texOmbres, visible.X - 32, visible.Y - 32, value.L + 64, value.H + 64, 255, 255, 255, a);
				}
			}
		}
	}

	private static void DessinerFlaquesDeLumiere()
	{
		float soleil = _etal.Soleil;
		if (soleil < 0.02f)
		{
			return;
		}
		IGameTexture texHalo = _texHalo;
		if (texHalo == null)
		{
			return;
		}
		FloatRect worldViewport = Graphics.WorldViewport;
		float num = (float)_t;
		foreach (MapInstance visible in _visibles)
		{
			if (ZoneDe(visible) != GenreZone.Foret || !Analyses.TryGetValue(visible.Id, out Analyse value) || value.Carte != visible || value.PointsHerbe.Length == 0)
			{
				continue;
			}
			for (int i = 0; i < value.PointsHerbe.Length; i += 25)
			{
				int num2 = value.PointsHerbe[i];
				float num3 = visible.X + num2 % value.L;
				float num4 = visible.Y + num2 / value.L;
				int num5 = 44 + i * 7 % 5 * 9;
				if (Dedans(num3, num4, worldViewport, num5))
				{
					float num6 = 0.8f + 0.2f * MathF.Sin(num * (0.0005f + 0.0002f * _p.Vent) + (float)i * 1.7f);
					float a = (0.09f + (float)(i * 3 % 4) * 0.02f) * soleil * num6;
					Texture(texHalo, num3 - (float)num5, num4 - (float)num5, num5 * 2, num5 * 2, 255, 232, 150, a, GameBlendModes.Add);
				}
			}
		}
	}

	private static float H01()
	{
		return (float)Hasard.NextDouble();
	}

	private static void ViderParticules()
	{
		_nbGouttes = (_nbAnneaux = (_nbEclabs = (_nbFlocons = (_nbPoses = (_nbTraces = (_nbFeuilles = (_nbGrains = (_nbMoutons = (_nbLucioles = 0)))))))));
		_tourbillon = null;
		_eclair = null;
		ViderEau();
	}

	private static float Echelle()
	{
		FloatRect worldViewport = Graphics.WorldViewport;
		return Math.Clamp(worldViewport.Width * worldViewport.Height / 256000f, 0.3f, 3.2f);
	}

	private static float Pente()
	{
		return 0f - (0.26f + 0.62f * _p.Vent);
	}

	private static Sol SolEn(float x, float y)
	{
		int num = Options.Instance.Map.TileWidth * Options.Instance.Map.MapWidth;
		int num2 = Options.Instance.Map.TileHeight * Options.Instance.Map.MapHeight;
		foreach (MapInstance visible in _visibles)
		{
			if (!(x < (float)visible.X) && !(y < (float)visible.Y) && !(x >= (float)(visible.X + num)) && !(y >= (float)(visible.Y + num2)))
			{
				if (!Analyses.TryGetValue(visible.Id, out Analyse value) || value.Carte != visible || value.Eau.Length == 0)
				{
					return Sol.Terre;
				}
				int num3 = (int)(y - (float)visible.Y) * value.L + (int)(x - (float)visible.X);
				if (value.Eau[num3] == 1)
				{
					return Sol.Eau;
				}
				byte b = value.FlaqueMap[num3];
				if (b > 0 && Pluvieuse(ZoneDe(visible)) && (float)(int)b / 100f < CroissanceFlaques() - 0.22f)
				{
					return Sol.Flaque;
				}
				return Sol.Terre;
			}
		}
		return Sol.Hors;
	}

	private static float CroissanceFlaques()
	{
		return Math.Clamp((_mouille - 0.12f) / 0.6f, 0f, 1f) * 1.12f;
	}

	private static bool DegageEn(float x, float y)
	{
		int num = Options.Instance.Map.TileWidth * Options.Instance.Map.MapWidth;
		int num2 = Options.Instance.Map.TileHeight * Options.Instance.Map.MapHeight;
		foreach (MapInstance visible in _visibles)
		{
			if (!(x < (float)visible.X) && !(y < (float)visible.Y) && !(x >= (float)(visible.X + num)) && !(y >= (float)(visible.Y + num2)))
			{
				int num3 = (int)((x - (float)visible.X) / (float)Options.Instance.Map.TileWidth);
				int num4 = (int)((y - (float)visible.Y) / (float)Options.Instance.Map.TileHeight);
				MapAttribute[,] attributes = visible.Attributes;
				if (attributes == null || num3 < 0 || num4 < 0 || num3 >= attributes.GetLength(0) || num4 >= attributes.GetLength(1))
				{
					return false;
				}
				MapAttribute mapAttribute = attributes[num3, num4];
				return mapAttribute == null || mapAttribute.Type != MapAttributeType.Blocked;
			}
		}
		return false;
	}

	private static void MajParticules(float dt, MapInstance? carte)
	{
		CartesVisibles();
		foreach (MapInstance visible in _visibles)
		{
			AnalyseDe(visible);
		}
		MenageAnalyses();
		FloatRect worldViewport = Graphics.WorldViewport;
		float echelle = Echelle();
		Zone zone = Zones[_zone];
		float s = dt / 1000f;
		MajPluie(s, worldViewport, echelle);
		MajNeige(s, worldViewport, echelle);
		MajVent(s, worldViewport, echelle, zone);
		MajMer(s, worldViewport, zone);
		MajLucioles(worldViewport, echelle, zone);
		Player me = Globals.Me;
		if (me != null)
		{
			float x = me.Origin.X;
			float num = me.Origin.Y - 1f;
			bool flag = Math.Abs(x - _dernierX) + Math.Abs(num - _dernierY) > 0.2f;
			_dernierX = x;
			_dernierY = num;
			long num2 = (long)(_t / 160.0);
			if (flag && _enneigement > 0.35f && _zone == GenreZone.Montagne && num2 != _pasTrace && _nbTraces < Traces.Length)
			{
				_pasTrace = num2;
				Traces[_nbTraces++] = new Pose
				{
					X = x + (float)((num2 % 2 == 0L) ? (-3) : 2),
					Y = num,
					Age = 0f
				};
			}
			if (_p.Pluie > 0.1f && H01() < _p.Pluie * 0.5f && _nbEclabs < Eclabs.Length)
			{
				Eclabs[_nbEclabs++] = new Eclab
				{
					X = x + (H01() - 0.5f) * 14f,
					Y = me.Origin.Y - 38f - H01() * 6f,
					Age = 0f,
					Miroir = 1,
					Force = 0.8f
				};
			}
			long num3 = (long)(_t / 150.0);
			if (flag && _p.Pluie > 0.1f && _mouille > 0.3f && num3 != _pasGohan && _nbEclabs < Eclabs.Length)
			{
				_pasGohan = num3;
				Eclabs[_nbEclabs++] = new Eclab
				{
					X = x + (float)((num3 % 2 == 0L) ? (-3) : 3),
					Y = num,
					Age = 60f,
					Miroir = (sbyte)((num3 % 2 != 0L) ? 1 : (-1)),
					Force = 0.9f
				};
			}
		}
		for (int num4 = _nbTraces - 1; num4 >= 0; num4--)
		{
			Traces[num4].Age += dt * (1f + _p.Neige * 2.5f);
			if (Traces[num4].Age > 9000f)
			{
				Traces[num4] = Traces[--_nbTraces];
			}
		}
	}

	private static void MajPluie(float s, FloatRect vue, float echelle)
	{
		int num = Math.Min(Gouttes.Length, (int)((_p.Pluie * 560f + _p.Orage * 300f) * echelle));
		float num2 = Pente();
		while (_nbGouttes < num)
		{
			NouvelleGoutte(ref Gouttes[_nbGouttes++], vue, num2, depart: true);
		}
		_nbGouttes = Math.Min(_nbGouttes, num);
		for (int i = 0; i < _nbGouttes; i++)
		{
			ref Goutte reference = ref Gouttes[i];
			float num3 = reference.Vitesse * (0.85f + 0.35f * _p.Pluie) * s;
			reference.Y += num3;
			reference.X += num3 * num2;
			if (reference.Y >= reference.YFin)
			{
				Impact(reference.X, reference.YFin, reference.Couche);
				NouvelleGoutte(ref reference, vue, num2, depart: false);
			}
			else if (reference.YFin < vue.Top - 40f || reference.YFin > vue.Bottom + 60f || reference.X < vue.Left - 300f || reference.X > vue.Right + 300f)
			{
				NouvelleGoutte(ref reference, vue, num2, depart: true);
			}
		}
		for (int num4 = _nbAnneaux - 1; num4 >= 0; num4--)
		{
			Anneaux[num4].Age += s * 1000f;
			if (Anneaux[num4].Age > Anneaux[num4].Vie)
			{
				Anneaux[num4] = Anneaux[--_nbAnneaux];
			}
		}
		for (int num5 = _nbEclabs - 1; num5 >= 0; num5--)
		{
			Eclabs[num5].Age += s * 1000f;
			if (Eclabs[num5].Age > 240f)
			{
				Eclabs[num5] = Eclabs[--_nbEclabs];
			}
		}
	}

	private static void NouvelleGoutte(ref Goutte g, FloatRect vue, float pente, bool depart)
	{
		g.Couche = (byte)((!(H01() < 0.42f)) ? ((H01() < 0.6f) ? 1u : 2u) : 0u);
		g.YFin = vue.Top + H01() * (vue.Height + 16f);
		float num = vue.Left - 8f + H01() * (vue.Width + 16f);
		float num2 = 90f + H01() * 170f;
		g.Y = (depart ? (g.YFin - H01() * num2) : (g.YFin - num2));
		g.X = num - pente * (g.YFin - g.Y);
		g.Vitesse = g.Couche switch
		{
			0 => 300, 
			1 => 430, 
			_ => 580, 
		};
	}

	private static void Impact(float x, float y, byte couche)
	{
		switch (SolEn(x, y))
		{
		case Sol.Hors:
			break;
		case Sol.Flaque:
			if (_nbAnneaux < Anneaux.Length)
			{
				int num3 = _nbAnneaux++;
				Anneau anneau2 = new Anneau
				{
					X = x,
					Y = y,
					Age = 0f,
					Vie = 300 + couche * 60,
					RMax = 1f + (float)(int)couche * 0.8f,
					Force = couche switch
					{
						0 => 0.5f, 
						1 => 0.75f, 
						_ => 1f, 
					}
				};
				Anneaux[num3] = anneau2;
			}
			break;
		case Sol.Eau:
			if (_nbAnneaux < Anneaux.Length)
			{
				int num2 = _nbAnneaux++;
				Anneau anneau = new Anneau
				{
					X = x,
					Y = y,
					Age = 0f,
					Vie = 380 + couche * 90,
					RMax = 1.5f + (float)(int)couche * 1.6f,
					Force = couche switch
					{
						0 => 0.45f, 
						1 => 0.7f, 
						_ => 1f, 
					}
				};
				Anneaux[num2] = anneau;
			}
			break;
		default:
			if (_nbEclabs < Eclabs.Length)
			{
				int num = _nbEclabs++;
				Eclab eclab = new Eclab
				{
					X = x,
					Y = y,
					Age = 0f,
					Miroir = (sbyte)((!(H01() < 0.5f)) ? 1 : (-1)),
					Force = couche switch
					{
						0 => 0.45f, 
						1 => 0.75f, 
						_ => 1f, 
					}
				};
				Eclabs[num] = eclab;
			}
			break;
		}
	}

	private static void MajNeige(float s, FloatRect vue, float echelle)
	{
		int num = Math.Min(Flocons.Length, (int)((_p.Neige * 650f + _p.Neige * _p.Vent * 450f) * echelle));
		while (_nbFlocons < num)
		{
			NouveauFlocon(ref Flocons[_nbFlocons++], vue, depart: true);
		}
		_nbFlocons = Math.Min(_nbFlocons, num);
		float num2 = 8f + 230f * MathF.Pow(_p.Vent, 1.6f);
		for (int i = 0; i < _nbFlocons; i++)
		{
			ref Flocon reference = ref Flocons[i];
			float num3 = reference.Couche switch
			{
				0 => 0.55f, 
				1 => 0.8f, 
				_ => 1f, 
			};
			reference.Y += reference.Vitesse * (1f + _p.Vent * 0.9f) * s;
			reference.X += ((0f - num2) * num3 + MathF.Sin((float)(_t * 0.0023) + reference.Phase) * 14f) * s;
			if (reference.X < vue.Left - 24f)
			{
				reference.X += vue.Width + 48f;
			}
			else if (reference.X > vue.Right + 400f)
			{
				reference.X -= vue.Width + 48f;
			}
			if (reference.Y >= reference.YFin)
			{
				bool flag = _nbPoses < Poses.Length && reference.X >= vue.Left && reference.X < vue.Right;
				bool flag2 = flag;
				if (flag2)
				{
					Sol sol = SolEn(reference.X, reference.YFin);
					bool flag3 = ((sol == Sol.Terre || sol == Sol.Flaque) ? true : false);
					flag2 = flag3;
				}
				if (flag2)
				{
					Poses[_nbPoses++] = new Pose
					{
						X = reference.X,
						Y = reference.YFin,
						Age = 0f
					};
				}
				NouveauFlocon(ref reference, vue, depart: false);
			}
			else if (reference.YFin < vue.Top - 40f || reference.YFin > vue.Bottom + 60f)
			{
				NouveauFlocon(ref reference, vue, depart: true);
			}
		}
		for (int num4 = _nbPoses - 1; num4 >= 0; num4--)
		{
			Poses[num4].Age += s * 1000f;
			if (Poses[num4].Age > 1400f)
			{
				Poses[num4] = Poses[--_nbPoses];
			}
		}
	}

	private static void NouveauFlocon(ref Flocon f, FloatRect vue, bool depart)
	{
		f.Couche = (byte)((!(H01() < 0.45f)) ? ((H01() < 0.6f) ? 1u : 2u) : 0u);
		f.YFin = vue.Top + H01() * (vue.Height + 10f);
		float num = 60f + H01() * 150f;
		f.Y = (depart ? (f.YFin - H01() * num) : (f.YFin - num));
		f.X = vue.Left - 20f + H01() * (vue.Width + 40f + 320f * _p.Vent);
		f.Phase = H01() * 6.28f;
		f.Vitesse = f.Couche switch
		{
			0 => 20, 
			1 => 32, 
			_ => 48, 
		};
	}

	private static void MajVent(float s, FloatRect vue, float echelle, Zone zone)
	{
		int num = (zone.Feuilles ? Math.Min(Feuilles.Length, (int)(Math.Max(0f, _p.Vent - 0.4f) * 34f * echelle)) : 0);
		int num2 = ((_zone == GenreZone.Foret) ? 1 : 0);
		while (_nbFeuilles < num)
		{
			Feuilles[_nbFeuilles++] = new Feuille
			{
				X = vue.Right + H01() * vue.Width * 0.5f,
				Y = vue.Top + H01() * vue.Height,
				Phase = H01() * 6.28f,
				Vx = 0.7f + H01() * 0.6f,
				Couleur = (byte)(num2 * 3 + Hasard.Next(3))
			};
		}
		_nbFeuilles = Math.Min(_nbFeuilles, num);
		for (int i = 0; i < _nbFeuilles; i++)
		{
			ref Feuille reference = ref Feuilles[i];
			reference.X -= (70f + 170f * _p.Vent) * reference.Vx * s;
			reference.Y += (MathF.Sin((float)(_t * 0.004) + reference.Phase) * 30f + 14f) * s;
			if (reference.X < vue.Left - 6f || reference.Y > vue.Bottom + 6f || reference.X > vue.Right + vue.Width)
			{
				reference.X = vue.Right + H01() * 60f;
				reference.Y = vue.Top + H01() * vue.Height * 0.9f;
			}
		}
		bool flag = _zone == GenreZone.Desert;
		int num3 = Math.Min(Grains.Length, (int)((_p.Sable * 320f + (flag ? (Math.Max(0f, _p.Vent - 0.5f) * 50f) : 0f)) * echelle));
		while (_nbGrains < num3)
		{
			Grains[_nbGrains++] = new Grain
			{
				X = vue.Left + H01() * (vue.Width + 60f),
				Y = vue.Top + H01() * vue.Height,
				Longueur = (byte)(2 + Hasard.Next(13)),
				V = 220f + H01() * 320f,
				A = 0.3f + H01() * 0.5f,
				Phase = H01() * 6.28f
			};
		}
		_nbGrains = Math.Min(_nbGrains, num3);
		for (int j = 0; j < _nbGrains; j++)
		{
			ref Grain reference2 = ref Grains[j];
			reference2.X -= reference2.V * (0.5f + 0.6f * _p.Vent) * s;
			reference2.Y += (MathF.Sin((float)(_t * 0.003) + reference2.Phase) * 10f + 6f) * s;
			if (reference2.X < vue.Left - 14f || reference2.X > vue.Right + 200f || reference2.Y > vue.Bottom + 4f || reference2.Y < vue.Top - 4f)
			{
				reference2.X = vue.Right + H01() * 40f;
				reference2.Y = vue.Top + H01() * vue.Height;
			}
		}
		if (flag && (_p.Sable > 0.3f || _p.Chaleur > 0.6f) && _tourbillon == null && H01() < s * 1000f / 9000f)
		{
			_tourbillon = new Tourbillon
			{
				X = vue.Right - 40f,
				Y = vue.Top + vue.Height * (0.55f + H01() * 0.35f),
				Age = 0f,
				Vie = 9000f
			};
		}
		if (_tourbillon != null)
		{
			_tourbillon.Age += s * 1000f;
			_tourbillon.X -= (18f + 26f * _p.Vent) * s;
			_tourbillon.Y += MathF.Sin((float)(_t * 0.001)) * 6f * s;
			if (_tourbillon.Age > _tourbillon.Vie || !flag || (_p.Sable < 0.2f && _p.Chaleur < 0.5f))
			{
				_tourbillon = null;
			}
		}
	}

	private static void MajMer(float s, FloatRect vue, Zone zone)
	{
		float num = (zone.Mer ? (_p.Orage * 0.9f + Math.Max(0f, _p.Vent - 0.5f) * 0.4f) : 0f);
		if (num > 0.08f && (float)_nbMoutons < Math.Min(Moutons.Length, 90f * num * Echelle()))
		{
			foreach (MapInstance visible in _visibles)
			{
				if (!Analyses.TryGetValue(visible.Id, out Analyse value) || value.Carte != visible || value.PointsEau.Length == 0)
				{
					continue;
				}
				int num2 = value.PointsEau[Hasard.Next(value.PointsEau.Length)];
				if (value.Dist[num2] > 9)
				{
					int num3 = visible.X + num2 % value.L;
					int num4 = visible.Y + num2 / value.L;
					if ((float)num3 >= vue.Left && (float)num3 < vue.Right && (float)num4 >= vue.Top && (float)num4 < vue.Bottom)
					{
						Moutons[_nbMoutons++] = new Mouton
						{
							X = num3,
							Y = num4,
							Age = 0f,
							Vie = 900f + H01() * 900f,
							Longueur = (byte)(3 + Hasard.Next(5))
						};
						break;
					}
				}
			}
		}
		for (int num5 = _nbMoutons - 1; num5 >= 0; num5--)
		{
			ref Mouton reference = ref Moutons[num5];
			reference.Age += s * 1000f;
			reference.X -= (6f + 18f * _p.Vent) * s;
			if (reference.Age > reference.Vie)
			{
				Moutons[num5] = Moutons[--_nbMoutons];
			}
		}
	}

	private static void MajLucioles(FloatRect vue, float echelle, Zone zone)
	{
		int num = Math.Min(Lucioles.Length, (int)((float)zone.Lucioles * echelle));
		for (int num2 = _nbLucioles - 1; num2 >= 0; num2--)
		{
			ref Luciole reference = ref Lucioles[num2];
			if (reference.Cx < vue.Left - 40f || reference.Cx > vue.Right + 40f || reference.Cy < vue.Top - 40f || reference.Cy > vue.Bottom + 40f)
			{
				Lucioles[num2] = Lucioles[--_nbLucioles];
			}
		}
		_nbLucioles = Math.Min(_nbLucioles, num);
		for (int i = 0; i < 6; i++)
		{
			if (_nbLucioles >= num)
			{
				break;
			}
			if (_visibles.Count == 0)
			{
				break;
			}
			MapInstance mapInstance = _visibles[Hasard.Next(_visibles.Count)];
			if (Analyses.TryGetValue(mapInstance.Id, out Analyse value) && value.Carte == mapInstance && value.PointsHerbe.Length != 0)
			{
				int num3 = value.PointsHerbe[Hasard.Next(value.PointsHerbe.Length)];
				int num4 = mapInstance.X + num3 % value.L;
				int num5 = mapInstance.Y + num3 / value.L;
				if (!((float)num4 < vue.Left) && !((float)num4 > vue.Right) && !((float)num5 < vue.Top) && !((float)num5 > vue.Bottom))
				{
					Lucioles[_nbLucioles++] = new Luciole
					{
						Cx = num4,
						Cy = num5 - 6,
						Ax = 8f + H01() * 22f,
						Ay = 5f + H01() * 14f,
						Ph = H01() * 6.28f,
						V = 0.5f + H01() * 0.8f,
						Clig = 1.2f + H01() * 2.2f
					};
				}
			}
		}
	}

	private static void MajOrage(float dt)
	{
		Zone zone = Zones[_zone];
		if (_p.Orage > 0.35f)
		{
			long maintenantPartage = MaintenantPartage;
			long num = maintenantPartage / 1500;
			if (num != _creneauEclair)
			{
				long num2 = maintenantPartage - (num * 1500 + (long)(Graine01(num, 1) * 1500f * 0.8f));
				if (num2 >= 0)
				{
					_creneauEclair = num;
					if (num2 < 300)
					{
						LancerEclair(zone, num);
					}
				}
			}
		}
		if (_secousse > 0.0 && _t >= _secousse)
		{
			_secousse = 0.0;
			CombatFeedback.StartShake(2f, 380L);
		}
		if (_eclair != null && _t - _eclair.T0 > 2200.0)
		{
			_eclair = null;
		}
		Eclair eclair = _eclair;
		if (eclair == null || eclair.Loin || eclair.Eclabousse || !(_t - eclair.T0 > 110.0))
		{
			return;
		}
		eclair.Eclabousse = true;
		if (!eclair.Eau)
		{
			return;
		}
		for (int i = 0; i < 3; i++)
		{
			if (_nbAnneaux >= Anneaux.Length)
			{
				break;
			}
			Anneaux[_nbAnneaux++] = new Anneau
			{
				X = eclair.X,
				Y = eclair.Y,
				Age = -i * 140,
				Vie = 900f,
				RMax = 9 + i * 5,
				Force = 1f
			};
		}
	}

	private static void LancerEclair(Zone zone, long creneau)
	{
		float num = Math.Min(0.9f, _p.Orage * 0.242f);
		FloatRect worldViewport = Graphics.WorldViewport;
		int num2 = Options.Instance.Map.TileWidth * Options.Instance.Map.MapWidth;
		int num3 = Options.Instance.Map.TileHeight * Options.Instance.Map.MapHeight;
		Eclair eclair = null;
		if (zone.Eclairs)
		{
			foreach (MapInstance visible in _visibles)
			{
				if (Graine01(creneau, visible.Id, 2) >= num * 0.5f)
				{
					continue;
				}
				for (int i = 0; i < 24; i++)
				{
					float num4 = (float)visible.X + Graine01(creneau, visible.Id, 10 + i * 2) * (float)num2;
					float num5 = (float)visible.Y + Graine01(creneau, visible.Id, 11 + i * 2) * (float)num3;
					Sol sol = SolEn(num4, num5);
					if (sol != Sol.Hors && (!zone.Mer || i >= 12 || sol == Sol.Eau) && (sol == Sol.Eau || DegageEn(num4, num5)))
					{
						bool flag = num4 >= worldViewport.Left && num4 < worldViewport.Left + worldViewport.Width && num5 >= worldViewport.Top && num5 < worldViewport.Top + worldViewport.Height;
						eclair = new Eclair
						{
							T0 = _t,
							X = num4,
							Y = num5,
							Loin = !flag,
							Eau = (sol == Sol.Eau)
						};
						break;
					}
				}
				if (eclair != null && !eclair.Loin)
				{
					break;
				}
			}
		}
		if (eclair == null)
		{
			if (Graine01(creneau, 3) >= num * 0.35f)
			{
				return;
			}
			eclair = new Eclair
			{
				T0 = _t,
				X = worldViewport.Left + Graine01(creneau, 4) * worldViewport.Width,
				Y = worldViewport.Top + worldViewport.Height * 0.5f,
				Loin = true
			};
		}
		_eclair = eclair;
		if (!eclair.Loin)
		{
			Random hasard = Hasard;
			Hasard = new Random((int)Melange((ulong)(creneau * 131 + 5)));
			GenererEclair(eclair, eclair.Y - 1000f);
			Hasard = hasard;
		}
		float num6 = (eclair.Loin ? (900f + Graine01(creneau, 6) * 1100f) : (200f + Graine01(creneau, 6) * 450f));
		Tonnerre(num6, eclair.Loin ? 0.45f : 1f);
		if (!eclair.Loin)
		{
			_secousse = _t + (double)num6;
		}
	}

	private static ulong Melange(ulong x)
	{
		x += 11400714819323198485uL;
		x = (x ^ (x >> 30)) * 13787848793156543929uL;
		x = (x ^ (x >> 27)) * 10723151780598845931uL;
		return x ^ (x >> 31);
	}

	private static float Graine01(long creneau, int sel)
	{
		return (float)(Melange((ulong)(creneau * 1000003 + sel)) >> 40) / 16777216f;
	}

	private static float Graine01(long creneau, Guid carte, int sel)
	{
		return (float)(Melange((ulong)(creneau * 1000003 + (long)(uint)carte.GetHashCode() * 7919L + sel)) >> 40) / 16777216f;
	}

	private static void GenererEclair(Eclair e, float haut)
	{
		float num = e.X + (H01() - 0.5f) * 70f;
		float num2 = haut - 10f;
		e.Tronc.Add((num, num2));
		while (num2 < e.Y)
		{
			num2 += 6f + H01() * 10f;
			float num3 = Math.Max(8f, e.Y - num2);
			num += (H01() - 0.5f) * 15f + (e.X - num) * Math.Min(1f, 12f / num3);
			if (num2 >= e.Y)
			{
				num2 = e.Y;
				num = e.X;
			}
			e.Tronc.Add((num, num2));
		}
		int num4 = 2 + Hasard.Next(2);
		for (int i = 0; i < num4; i++)
		{
			if (e.Tronc.Count <= 5)
			{
				break;
			}
			(float, float) tuple = e.Tronc[2 + Hasard.Next(e.Tronc.Count - 4)];
			int num5 = ((!(H01() < 0.5f)) ? 1 : (-1));
			List<(float, float)> list = new List<(float, float)> { tuple };
			(float, float) tuple2 = tuple;
			float num6 = tuple2.Item1;
			float num7 = tuple2.Item2;
			int j = 0;
			for (int num8 = 3 + Hasard.Next(4); j < num8; j++)
			{
				num6 += (float)num5 * (3f + H01() * 7f);
				num7 += 5f + H01() * 7f;
				list.Add((num6, num7));
			}
			e.Branches.Add(list);
		}
	}

	internal static float Flash()
	{
		if (_eclair == null)
		{
			return 0f;
		}
		float num = (float)(_t - _eclair.T0);
		float num2 = ((num < 50f) ? 0.3f : ((num < 95f) ? 0.06f : ((num < 210f) ? 0.95f : (0.95f * MathF.Exp((0f - (num - 210f)) / 260f)))));
		return num2 * (_eclair.Loin ? 0.32f : 1f);
	}

	private static byte[] MasqueSable(byte[] px, byte[] eau, byte[] plat, int L, int H, bool desert)
	{
		int num = (L + 4 - 1) / 4;
		int num2 = (H + 4 - 1) / 4;
		byte[] array = new byte[num * num2];
		for (int i = 0; i < num2; i++)
		{
			for (int j = 0; j < num; j++)
			{
				int num3 = 0;
				int num4 = 0;
				for (int k = i * 4; k < Math.Min(H, (i + 1) * 4); k++)
				{
					for (int l = j * 4; l < Math.Min(L, (j + 1) * 4); l++)
					{
						int num5 = k * L + l;
						num4++;
						if (eau[num5] != 0 || px[num5 * 4 + 3] == 0)
						{
							continue;
						}
						int num6 = px[num5 * 4];
						int num7 = px[num5 * 4 + 1];
						int num8 = px[num5 * 4 + 2];
						bool flag = num7 > num6 + 8 && num7 > num8 + 20;
						bool num9;
						if (!desert)
						{
							num9 = plat[num5] == 1;
						}
						else
						{
							if (flag || num6 + num7 + num8 <= 240)
							{
								continue;
							}
							num9 = num6 >= num8 + 15;
						}
						if (num9)
						{
							num3++;
						}
					}
				}
				array[i * num + j] = (byte)((num4 > 0 && num3 * 16 >= num4 * 9) ? 1u : 0u);
			}
		}
		return array;
	}

	public static bool EstSable(float x, float y)
	{
		if (_enPanne || !_actif || !_initialise)
		{
			return false;
		}
		try
		{
			int num = Options.Instance.Map.TileWidth * Options.Instance.Map.MapWidth;
			int num2 = Options.Instance.Map.TileHeight * Options.Instance.Map.MapHeight;
			foreach (MapInstance visible in _visibles)
			{
				if (!(x < (float)visible.X) && !(y < (float)visible.Y) && !(x >= (float)(visible.X + num)) && !(y >= (float)(visible.Y + num2)))
				{
					if (!Analyses.TryGetValue(visible.Id, out Analyse value) || value.Carte != visible || value.Sable.Length == 0)
					{
						return false;
					}
					int num3 = (value.L + 4 - 1) / 4;
					int num4 = (int)(y - (float)visible.Y) / 4 * num3 + (int)(x - (float)visible.X) / 4;
					return num4 >= 0 && num4 < value.Sable.Length && value.Sable[num4] == 1;
				}
			}
		}
		catch (Exception exception)
		{
			Panne(exception);
		}
		return false;
	}

	private static void MajSons(float dt)
	{
		bool flag = _zone != GenreZone.Interieur && _actif;
		Etalonnage etalonnage = Calculer(0f);
		Zone zone = Zones[_zone];
		float num = 0.5f + 0.5f * MathF.Sin((float)(_t * 0.0008));
		_003C_003Ey__InlineArray5<float> buffer = default(_003C_003Ey__InlineArray5<float>);
		global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<_003C_003Ey__InlineArray5<float>, float>(ref buffer, 0) = (flag ? Math.Min(1f, (_p.Pluie * 0.42f + _p.Orage * 0.2f) / 0.5f) : 0f);
		global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<_003C_003Ey__InlineArray5<float>, float>(ref buffer, 1) = (flag ? Math.Min(1f, (0.03f + _p.Vent * _p.Vent * 0.3f + _p.Sable * 0.22f + _p.Neige * _p.Vent * 0.2f) / 0.45f) : 0f);
		global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<_003C_003Ey__InlineArray5<float>, float>(ref buffer, 2) = ((flag && zone.Mer) ? Math.Min(1f, (0.08f + 0.1f * num) * (1f + _p.Orage * 2.2f + _p.Vent * 0.6f) / 0.5f) : 0f);
		global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<_003C_003Ey__InlineArray5<float>, float>(ref buffer, 3) = (flag ? Math.Min(1f, _p.Sable * 0.16f / 0.16f) : 0f);
		global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<_003C_003Ey__InlineArray5<float>, float>(ref buffer, 4) = (flag ? (Math.Clamp(etalonnage.Lampes * (1f - (_p.Pluie + _p.Sable + _p.Orage + _p.Neige)), 0f, 1f) * ((_zone == GenreZone.Montagne) ? 0f : 1f)) : 0f);
		Span<float> span = global::_003CPrivateImplementationDetails_003E.InlineArrayAsSpan<_003C_003Ey__InlineArray5<float>, float>(ref buffer, 5);
		_003C_003Ey__InlineArray5<int> buffer2 = default(_003C_003Ey__InlineArray5<int>);
		global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<_003C_003Ey__InlineArray5<int>, int>(ref buffer2, 0) = 85;
		global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<_003C_003Ey__InlineArray5<int>, int>(ref buffer2, 1) = 70;
		global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<_003C_003Ey__InlineArray5<int>, int>(ref buffer2, 2) = 60;
		global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<_003C_003Ey__InlineArray5<int>, int>(ref buffer2, 3) = 55;
		global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<_003C_003Ey__InlineArray5<int>, int>(ref buffer2, 4) = 35;
		Span<int> span2 = global::_003CPrivateImplementationDetails_003E.InlineArrayAsSpan<_003C_003Ey__InlineArray5<int>, int>(ref buffer2, 5);
		float num2 = 1f - MathF.Exp((0f - dt) / 450f);
		for (int i = 0; i < Boucles.Length; i++)
		{
			Niveaux[i] += (span[i] - Niveaux[i]) * num2;
			int num3 = (int)MathF.Round(Niveaux[i] * (float)span2[i]);
			Boucle boucle = Boucles[i];
			if (num3 > 0)
			{
				Silences[i] = 0f;
				if (boucle == null || !boucle.Loaded)
				{
					boucle = new Boucle(NomsBoucles[i]);
					if (!boucle.Loaded || !Audio.AjouterSonMeteo(boucle))
					{
						boucle.Stop();
						boucle = null;
					}
					Boucles[i] = boucle;
				}
				if (boucle != null)
				{
					boucle.Volume = num3;
				}
			}
			else if (boucle != null && boucle.Loaded)
			{
				boucle.Volume = 0;
				Silences[i] += dt;
				if (Silences[i] > 2500f)
				{
					boucle.Stop();
					Boucles[i] = null;
				}
			}
		}
		for (int num4 = Tonnerres.Count - 1; num4 >= 0; num4--)
		{
			if (!(Tonnerres[num4].Quand > _t))
			{
				bool flag2 = Tonnerres[num4].Force > 0.7f;
				_dernierTonnerre = (_dernierTonnerre + 1 + Hasard.Next(2)) % 3;
				string filename = (flag2 ? $"meteo_tonnerre_proche_{_dernierTonnerre + 1}" : $"meteo_tonnerre_loin_{_dernierTonnerre % 2 + 1}");
				if (flag)
				{
					Audio.AddGameSound(filename, loop: false);
				}
				Tonnerres.RemoveAt(num4);
			}
		}
	}

	private static void Tonnerre(float delaiMs, float force)
	{
		if (Tonnerres.Count < 6)
		{
			Tonnerres.Add((_t + (double)delaiMs, force));
		}
	}

	private static void CouperSons()
	{
		for (int i = 0; i < Boucles.Length; i++)
		{
			Boucles[i]?.Stop();
			Boucles[i] = null;
			Niveaux[i] = 0f;
		}
		Tonnerres.Clear();
		CouperSonDesChutes();
	}

	internal static DonneesVille? VilleDe(MapInstance carte)
	{
		if (!_enPanne)
		{
			return AnalyseDe(carte)?.Ville;
		}
		return null;
	}

	internal static (Vitre2[] Vitres, byte[] Masque, int L, int H)? FenetresDImage(IGameTexture texture)
	{
		Texture2D texture2 = texture.GetTexture<Texture2D>();
		Microsoft.Xna.Framework.Color[] couleurs;
		if (texture2 != null)
		{
			AtlasReference atlasReference = texture.AtlasReference;
			if ((object)atlasReference == null || !atlasReference.IsRotated)
			{
				AtlasReference atlasReference2 = texture.AtlasReference;
				Microsoft.Xna.Framework.Rectangle value = (((object)atlasReference2 != null) ? new Microsoft.Xna.Framework.Rectangle(atlasReference2.Bounds.X, atlasReference2.Bounds.Y, atlasReference2.Bounds.Width, atlasReference2.Bounds.Height) : new Microsoft.Xna.Framework.Rectangle(0, 0, texture2.Width, texture2.Height));
				int width = value.Width;
				int height = value.Height;
				int num = width * height;
				if (num == 0 || num > 1048576)
				{
					return null;
				}
				couleurs = new Microsoft.Xna.Framework.Color[num];
				texture2.GetData(0, value, couleurs, 0, num);
				bool[] array = new bool[num];
				for (int i = 0; i < num; i++)
				{
					Microsoft.Xna.Framework.Color color = couleurs[i];
					int num2 = i;
					int num4;
					if (color.A > 0 && color.B > 150 && color.B - color.R > 45 && color.B > color.G + 5 && color.G >= color.R - 10)
					{
						float num3 = Luma(i);
						num4 = ((num3 >= 90f && num3 <= 235f) ? 1 : 0);
					}
					else
					{
						num4 = 0;
					}
					array[num2] = (byte)num4 != 0;
				}
				List<(List<int>, int, int, int, int)> list = (from t in Taches(array, width, height, 2, 6, 1200)
					where t.X1 - t.X0 < 90 && t.Y1 - t.Y0 < 60
					select t).ToList();
				for (int num5 = 0; num5 < num; num5++)
				{
					Microsoft.Xna.Framework.Color color2 = couleurs[num5];
					array[num5] = color2.A > 0 && color2.R < 95 && color2.G < 85 && color2.B < 75 && Luma(num5) > 12f;
				}
				foreach (var item2 in Taches(array, width, height, 4, 20, 700))
				{
					int num6 = item2.X1 - item2.X0 + 1;
					int num7 = item2.Y1 - item2.Y0 + 1;
					bool flag = ((num6 < 10 || num6 > 44) ? true : false);
					bool flag2 = flag;
					bool flag3 = flag2;
					if (!flag3)
					{
						bool flag4 = ((num7 < 8 || num7 > 40) ? true : false);
						flag3 = flag4;
					}
					if (flag3 || (float)item2.Pts.Count < (float)(num6 * num7) * 0.25f)
					{
						continue;
					}
					int num8 = 0;
					int num9 = 0;
					for (int num10 = item2.Y0 - 4; num10 <= item2.Y1 + 4; num10++)
					{
						for (int num11 = item2.X0 - 4; num11 <= item2.X1 + 4; num11++)
						{
							if (num11 >= 0 && num10 >= 0 && num11 < width && num10 < height && (num11 < item2.X0 - 1 || num11 > item2.X1 + 1 || num10 < item2.Y0 - 1 || num10 > item2.Y1 + 1))
							{
								num8++;
								if (Bois(num10 * width + num11))
								{
									num9++;
								}
							}
						}
					}
					if (num8 > 0 && (float)num9 >= (float)num8 * 0.55f)
					{
						list.Add(item2);
					}
				}
				int num12 = 17;
				string name = texture.Name;
				foreach (char c in name)
				{
					num12 = num12 * 31 + c;
				}
				Random hasard = new Random(num12);
				byte[] rgba = new byte[num * 4];
				Vitre2[] item = list.Take(40).Select<(List<int>, int, int, int, int), Vitre2>(delegate((List<int> Pts, int X0, int Y0, int X1, int Y1) t)
				{
					foreach (int item3 in t.Pts)
					{
						byte[] array2 = rgba;
						int num14 = item3 * 4;
						byte[] array3 = rgba;
						int num15 = item3 * 4 + 1;
						byte[] array4 = rgba;
						int num16 = item3 * 4 + 2;
						byte b;
						rgba[item3 * 4 + 3] = (b = byte.MaxValue);
						array2[num14] = (array3[num15] = (array4[num16] = b));
					}
					float extinction = ((hasard.NextDouble() < 0.25) ? (0.5f + (float)hasard.NextDouble() * 4f) : (-1f));
					return new Vitre2((short)t.X0, (short)t.Y0, (short)(t.X1 - t.X0 + 1), (short)(t.Y1 - t.Y0 + 1), 0.12f + (float)hasard.NextDouble() * 0.33f, extinction, Haut: false);
				}).ToArray();
				return (item, rgba, width, height);
			}
		}
		return null;
		bool Bois(int num14)
		{
			if (couleurs[num14].A > 0 && couleurs[num14].R > couleurs[num14].G && couleurs[num14].G >= couleurs[num14].B && couleurs[num14].R - couleurs[num14].B > 40)
			{
				float num15 = Luma(num14);
				if (num15 > 90f)
				{
					return num15 < 225f;
				}
				return false;
			}
			return false;
		}
		float Luma(int num14)
		{
			return (float)(int)couleurs[num14].R * 0.3f + (float)(int)couleurs[num14].G * 0.55f + (float)(int)couleurs[num14].B * 0.15f;
		}
	}

	private static List<(List<int> Pts, int X0, int Y0, int X1, int Y1)> Taches(bool[] masque, int L, int H, int portee, int min, int max)
	{
		bool[] array = new bool[L * H];
		Stack<int> stack = new Stack<int>();
		List<(List<int>, int, int, int, int)> list = new List<(List<int>, int, int, int, int)>();
		for (int i = 0; i < L * H; i++)
		{
			if (!masque[i] || array[i])
			{
				continue;
			}
			array[i] = true;
			stack.Push(i);
			List<int> list2 = new List<int>();
			int num = int.MaxValue;
			int num2 = int.MaxValue;
			int num3 = -1;
			int num4 = -1;
			while (stack.Count > 0)
			{
				int num5 = stack.Pop();
				list2.Add(num5);
				int num6 = num5 % L;
				int num7 = num5 / L;
				num = Math.Min(num, num6);
				num2 = Math.Min(num2, num7);
				num3 = Math.Max(num3, num6);
				num4 = Math.Max(num4, num7);
				if (list2.Count > max * 3)
				{
					continue;
				}
				for (int j = -portee; j <= portee; j++)
				{
					for (int k = -portee; k <= portee; k++)
					{
						int num8 = num6 + k;
						int num9 = num7 + j;
						if (num8 >= 0 && num9 >= 0 && num8 < L && num9 < H)
						{
							int num10 = num9 * L + num8;
							if (masque[num10] && !array[num10])
							{
								array[num10] = true;
								stack.Push(num10);
							}
						}
					}
				}
			}
			if (list2.Count >= min && list2.Count <= max)
			{
				list.Add((list2, num, num2, num3, num4));
			}
		}
		return list;
	}

	private static void NoterLesReperes(IGameTexture planche, int sx, int sy, int w, int h, int dx, int dy, ReperesConnus reperes)
	{
		string fileName = Path.GetFileName(planche.Name);
		(string, int, int)[] chemineesConnues = ChemineesConnues;
		for (int i = 0; i < chemineesConnues.Length; i++)
		{
			var (b, num, num2) = chemineesConnues[i];
			if (num >= sx && num2 >= sy && num < sx + w && num2 < sy + h && string.Equals(fileName, b, StringComparison.OrdinalIgnoreCase))
			{
				(short, short) item = ((short)(dx + num - sx), (short)(dy + num2 - sy));
				if (!reperes.Cheminees.Contains(item))
				{
					reperes.Cheminees.Add(item);
				}
			}
		}
		(string, int, int, int, int, int, int, int, int)[] fenetresConnues = FenetresConnues;
		for (int j = 0; j < fenetresConnues.Length; j++)
		{
			(string, int, int, int, int, int, int, int, int) tuple2 = fenetresConnues[j];
			if (tuple2.Item2 >= sx && tuple2.Item3 >= sy && tuple2.Item2 < sx + w && tuple2.Item3 < sy + h && string.Equals(fileName, tuple2.Item1, StringComparison.OrdinalIgnoreCase))
			{
				(short, short, short, short, short, short, short, short) item2 = ((short)(dx + tuple2.Item2 - sx), (short)(dy + tuple2.Item3 - sy), (short)tuple2.Item4, (short)tuple2.Item5, (short)tuple2.Item6, (short)tuple2.Item7, (short)tuple2.Rest.Item1, (short)tuple2.Rest.Item2);
				if (!reperes.Fenetres.Contains(item2))
				{
					reperes.Fenetres.Add(item2);
				}
			}
		}
		(string, int, int, int, int)[] pasDesFenetres = PasDesFenetres;
		for (int k = 0; k < pasDesFenetres.Length; k++)
		{
			(string, int, int, int, int) tuple3 = pasDesFenetres[k];
			if (tuple3.Item2 >= sx && tuple3.Item3 >= sy && tuple3.Item2 < sx + w && tuple3.Item3 < sy + h && string.Equals(fileName, tuple3.Item1, StringComparison.OrdinalIgnoreCase))
			{
				(short, short, short, short) item3 = ((short)(dx + tuple3.Item2 - sx), (short)(dy + tuple3.Item3 - sy), (short)tuple3.Item4, (short)tuple3.Item5);
				if (!reperes.PasDesFenetres.Contains(item3))
				{
					reperes.PasDesFenetres.Add(item3);
				}
			}
		}
	}

	private static DonneesVille? ConstruireVille(Analyse a, List<(byte[] Rgba, int PL, int PH, int Sx, int Sy, int W, int Hh, int Dx, int Dy)> posesBas, List<(byte[] Rgba, int PL, int PH, int Sx, int Sy, int W, int Hh, int Dx, int Dy)> posesHaut, ReperesConnus? reperes = null)
	{
		List<(short, short)> list = reperes?.Cheminees;
		int L = a.L;
		int H = a.H;
		int num = L * H;
		byte[] px = new byte[num * 4];
		byte[] haut = new byte[num * 4];
		ComposerPoses(px, posesBas, L, H);
		ComposerPoses(haut, posesHaut, L, H);
		for (int i = 0; i < num; i++)
		{
			float num2 = (float)(int)haut[i * 4 + 3] / 255f;
			if (!(num2 <= 0f))
			{
				for (int j = 0; j < 3; j++)
				{
					px[i * 4 + j] = (byte)((float)(int)haut[i * 4 + j] * num2 + (float)(int)px[i * 4 + j] * (1f - num2));
				}
				px[i * 4 + 3] = byte.MaxValue;
			}
		}
		DonneesVille v = new DonneesVille();
		Random hasard = new Random(a.Carte.Id.GetHashCode());
		int num3 = 0;
		for (int k = 0; k < num; k++)
		{
			if (px[k * 4 + 3] > 0 && Pave(k))
			{
				num3++;
			}
		}
		v.Urbaine = (float)num3 >= (float)num * 0.12f;
		List<(List<int>, int, int, int, int)> list2 = new List<(List<int>, int, int, int, int)>();
		List<(short A, short B, bool Horizontale)> routes;
		bool[] masque;
		if (v.Urbaine)
		{
			int tileWidth = Options.Instance.Map.TileWidth;
			int tileHeight = Options.Instance.Map.TileHeight;
			v.TrottoirL = L / tileWidth;
			v.TrottoirH = H / tileHeight;
			v.Trottoir = new bool[v.TrottoirL * v.TrottoirH];
			for (int l = 0; l < v.TrottoirH; l++)
			{
				for (int m = 0; m < v.TrottoirL; m++)
				{
					int num4 = 0;
					int num5 = 0;
					int num6 = 0;
					for (int n = l * tileHeight + 1; n < (l + 1) * tileHeight; n += 2)
					{
						for (int num7 = m * tileWidth + 1; num7 < (m + 1) * tileWidth; num7 += 2)
						{
							int num8 = n * L + num7;
							num6++;
							if (Pave(num8) && !Route(num8))
							{
								num4++;
							}
							if (haut[num8 * 4 + 3] > 0)
							{
								num5++;
							}
						}
					}
					v.Trottoir[l * v.TrottoirL + m] = num4 * 2 >= num6 && num5 * 4 < num6;
				}
			}
			routes = new List<(short, short, bool)>();
			for (int num9 = 0; num9 < 2; num9++)
			{
				int num10 = ((num9 == 0) ? H : L);
				int num11 = ((num9 == 0) ? L : H);
				int num12 = -1;
				for (int num13 = 0; num13 <= num10; num13++)
				{
					bool flag = false;
					if (num13 < num10)
					{
						int num14 = 0;
						for (int num15 = 0; num15 < num11; num15++)
						{
							if (Route((num9 == 0) ? (num13 * L + num15) : (num15 * L + num13)))
							{
								num14++;
							}
						}
						flag = num14 >= num11 / 2;
					}
					if (flag)
					{
						if (num12 < 0)
						{
							num12 = num13;
						}
					}
					else if (num12 >= 0)
					{
						int num16 = num13 - num12;
						if (num16 >= 36 && num16 <= 160)
						{
							routes.Add(((short)num12, (short)(num13 - 1), num9 == 0));
						}
						num12 = -1;
					}
				}
			}
			v.Routes = routes.ToArray();
			masque = new bool[num];
			for (int num17 = 0; num17 < num; num17++)
			{
				bool[] array = masque;
				int num18 = num17;
				int num20;
				if (px[num17 * 4 + 3] > 0 && B(num17) > 150 && B(num17) - R(num17) > 45 && B(num17) > G(num17) + 5 && G(num17) >= R(num17) - 10)
				{
					float num19 = Luma(num17);
					num20 = ((num19 >= 90f && num19 <= 235f) ? 1 : 0);
				}
				else
				{
					num20 = 0;
				}
				array[num18] = (byte)num20 != 0;
			}
			list2.AddRange(from t in Taches(masque, L, H, 2, 6, 1200)
				where t.X1 - t.X0 < 90 && t.Y1 - t.Y0 < 60 && !SurRoute((t.X0 + t.X1) / 2, (t.Y0 + t.Y1) / 2) && !OmbreAuSol(t.Pts)
				select t);
			for (int num21 = 0; num21 < num; num21++)
			{
				masque[num21] = px[num21 * 4 + 3] > 0 && R(num21) > 190 && Sat(num21) > 110 && ((G(num21) > 170 && B(num21) < 120) || G(num21) < 80);
			}
			List<(short, short, short, short, byte, byte, byte)> list3 = new List<(short, short, short, short, byte, byte, byte)>();
			v.RgbaEnseignes = new byte[num * 4];
			foreach (var item3 in Taches(masque, L, H, 2, 30, 900))
			{
				int num22 = item3.X1 - item3.X0 + 1;
				int num23 = item3.Y1 - item3.Y0 + 1;
				if (num22 > 70 || num23 > 50 || num22 > 4 * num23 || num23 > 4 * num22 || (float)item3.Pts.Count < (float)(num22 * num23) * 0.25f)
				{
					continue;
				}
				int num24 = 0;
				int num25 = 0;
				for (int num26 = item3.Y0 - 3; num26 <= item3.Y1 + 3; num26++)
				{
					for (int num27 = item3.X0 - 3; num27 <= item3.X1 + 3; num27++)
					{
						if (num27 >= 0 && num26 >= 0 && num27 < L && num26 < H && (num27 < item3.X0 || num27 > item3.X1 || num26 < item3.Y0 || num26 > item3.Y1))
						{
							num24++;
							int i2 = num26 * L + num27;
							if (R(i2) > 170 && G(i2) > 140 && B(i2) < 150 && Sat(i2) > 60)
							{
								num25++;
							}
						}
					}
				}
				if (num24 == 0 || (float)num25 >= (float)num24 * 0.3f || list3.Count >= 40)
				{
					continue;
				}
				long num28 = 0L;
				long num29 = 0L;
				long num30 = 0L;
				foreach (int item4 in item3.Pts)
				{
					num28 += R(item4);
					num29 += G(item4);
					num30 += B(item4);
					Buffer.BlockCopy(px, item4 * 4, v.RgbaEnseignes, item4 * 4, 4);
				}
				list3.Add(((short)item3.X0, (short)item3.Y0, (short)num22, (short)num23, (byte)(num28 / item3.Pts.Count), (byte)(num29 / item3.Pts.Count), (byte)(num30 / item3.Pts.Count)));
			}
			v.Enseignes = list3.ToArray();
			if (list3.Count == 0)
			{
				v.RgbaEnseignes = null;
			}
			List<(short, short)> list4 = new List<(short, short)>();
			for (int y = 16; y < H - 16; y += 24)
			{
				if (list4.Count >= 3)
				{
					break;
				}
				int x;
				for (x = 16; x < L - 16; x += 24)
				{
					if (list4.Count >= 3)
					{
						break;
					}
					bool flag2 = true;
					for (int num31 = -12; num31 <= 12 && flag2; num31 += 3)
					{
						for (int num32 = -16; num32 <= 16 && flag2; num32 += 3)
						{
							flag2 = Pave((y + num31) * L + x + num32);
						}
					}
					if (flag2 && (x * 7 + y * 13) % 5 == 0 && !list4.Any<(short, short)>(((short X, short Y) p) => Math.Abs(p.X - x) + Math.Abs(p.Y - y) < 260))
					{
						list4.Add(((short)x, (short)y));
					}
				}
			}
			v.Pigeons = list4.ToArray();
		}
		else
		{
			bool[] array2 = new bool[num];
			for (int num33 = 0; num33 < num; num33++)
			{
				int num34 = num33;
				int num35;
				if (haut[num33 * 4 + 3] > 0 && Sat(num33) < 18)
				{
					float num19 = Luma(num33);
					num35 = ((num19 > 70f && num19 < 175f) ? 1 : 0);
				}
				else
				{
					num35 = 0;
				}
				array2[num34] = (byte)num35 != 0;
			}
			List<(short, short)> list5 = new List<(short, short)>();
			foreach (var item5 in Taches(array2, L, H, 1, 12, 160))
			{
				int val = item5.X1 - item5.X0 + 1;
				int num36 = item5.Y1 - item5.Y0 + 1;
				if ((float)Math.Max(val, num36) < 1.5f * (float)Math.Min(val, num36) || num36 < 5)
				{
					continue;
				}
				int num37 = 0;
				int num38 = 0;
				for (int num39 = item5.Y0 - 4; num39 <= item5.Y1 + 4; num39++)
				{
					for (int num40 = item5.X0 - 4; num40 <= item5.X1 + 4; num40++)
					{
						if (num40 >= 0 && num39 >= 0 && num40 < L && num39 < H && (num40 < item5.X0 || num40 > item5.X1 || num39 < item5.Y0 || num39 > item5.Y1))
						{
							num37++;
							if (Brun(num39 * L + num40))
							{
								num38++;
							}
						}
					}
				}
				if (num37 > 0 && (float)num38 >= (float)num37 * 0.2f && list5.Count < 12)
				{
					int num41 = item5.Pts.OrderBy((int num56) => num56 / L).First();
					list5.Add(((short)(num41 % L), (short)(num41 / L)));
				}
			}
			v.Tuyaux = list5.ToArray();
		}
		MapAttribute[,] attributs = a.Carte.Attributes;
		bool[] array3 = new bool[num];
		for (int num42 = 0; num42 < num; num42++)
		{
			array3[num42] = px[num42 * 4 + 3] > 0 && R(num42) < 95 && G(num42) < 85 && B(num42) < 75 && Luma(num42) > 12f;
		}
		foreach (var item6 in Taches(array3, L, H, 4, 20, 700))
		{
			int num43 = item6.X1 - item6.X0 + 1;
			int num44 = item6.Y1 - item6.Y0 + 1;
			bool flag3 = ((num43 < 10 || num43 > 44) ? true : false);
			bool flag4 = flag3;
			bool flag5 = flag4;
			if (!flag5)
			{
				bool flag6 = ((num44 < 8 || num44 > 40) ? true : false);
				flag5 = flag6;
			}
			if (flag5 || (float)item6.Pts.Count < (float)(num43 * num44) * 0.25f)
			{
				continue;
			}
			int num45 = 0;
			int num46 = 0;
			for (int num47 = item6.Y0 - 4; num47 <= item6.Y1 + 4; num47++)
			{
				for (int num48 = item6.X0 - 4; num48 <= item6.X1 + 4; num48++)
				{
					if (num48 >= 0 && num47 >= 0 && num48 < L && num47 < H && (num48 < item6.X0 - 1 || num48 > item6.X1 + 1 || num47 < item6.Y0 - 1 || num47 > item6.Y1 + 1))
					{
						num45++;
						if (Bois(num47 * L + num48))
						{
							num46++;
						}
					}
				}
			}
			if (num45 > 0 && (float)num46 >= (float)num45 * 0.55f && DansUnMur(item6))
			{
				list2.Add(item6);
			}
		}
		if (reperes != null)
		{
			List<(short, short, short, short)> pasDesFenetres = reperes.PasDesFenetres;
			if (pasDesFenetres != null && pasDesFenetres.Count > 0)
			{
				list2.RemoveAll(((List<int> Pts, int X0, int Y0, int X1, int Y1) t) => reperes.PasDesFenetres.Any(((short X, short Y, short L, short H) p) => t.X0 < p.X + p.L && t.X1 >= p.X && t.Y0 < p.Y + p.H && t.Y1 >= p.Y));
			}
		}
		if (reperes != null)
		{
			List<(short, short, short, short, short, short, short, short)> fenetres = reperes.Fenetres;
			if (fenetres != null && fenetres.Count > 0)
			{
				List<(List<int> Pts, int X0, int Y0, int X1, int Y1)> posees = new List<(List<int>, int, int, int, int)>();
				foreach (var item7 in reperes.Fenetres.Take(60))
				{
					List<int> list6 = new List<int>(item7.L * item7.H);
					for (int num49 = 0; num49 < item7.H; num49++)
					{
						for (int num50 = 0; num50 < item7.L; num50++)
						{
							int num51 = item7.X + num50;
							int num52 = item7.Y + num49;
							if ((num50 < item7.CroixX || num50 >= item7.CroixX + item7.CroixL) && (num49 < item7.CroixY || num49 >= item7.CroixY + item7.Rest.Item1) && num51 >= 0 && num52 >= 0 && num51 < L && num52 < H && px[(num52 * L + num51) * 4 + 3] > 0)
							{
								list6.Add(num52 * L + num51);
							}
						}
					}
					if (list6.Count > 0)
					{
						posees.Add((list6, item7.X, item7.Y, item7.X + item7.L - 1, item7.Y + item7.H - 1));
					}
				}
				list2.RemoveAll(((List<int> Pts, int X0, int Y0, int X1, int Y1) t) => posees.Any<(List<int>, int, int, int, int)>(((List<int> Pts, int X0, int Y0, int X1, int Y1) c) => t.X0 <= c.X1 + 2 && t.X1 >= c.X0 - 2 && t.Y0 <= c.Y1 + 2 && t.Y1 >= c.Y0 - 2));
				list2.InsertRange(0, posees);
			}
		}
		if (list2.Count > 0)
		{
			v.RgbaVitres = new byte[num * 4];
			v.Vitres = list2.Take(160).Select<(List<int>, int, int, int, int), Vitre2>(delegate((List<int> Pts, int X0, int Y0, int X1, int Y1) t)
			{
				int num56 = 0;
				foreach (int item8 in t.Pts)
				{
					byte[]? rgbaVitres = v.RgbaVitres;
					int num57 = item8 * 4;
					byte[]? rgbaVitres2 = v.RgbaVitres;
					int num58 = item8 * 4 + 1;
					byte[]? rgbaVitres3 = v.RgbaVitres;
					int num59 = item8 * 4 + 2;
					byte b;
					v.RgbaVitres[item8 * 4 + 3] = (b = byte.MaxValue);
					rgbaVitres[num57] = (rgbaVitres2[num58] = (rgbaVitres3[num59] = b));
					if (haut[item8 * 4 + 3] > 0)
					{
						num56++;
					}
				}
				float extinction = ((hasard.NextDouble() < 0.25) ? (0.5f + (float)hasard.NextDouble() * 4f) : (-1f));
				return new Vitre2((short)t.X0, (short)t.Y0, (short)(t.X1 - t.X0 + 1), (short)(t.Y1 - t.Y0 + 1), 0.12f + (float)hasard.NextDouble() * 0.33f, extinction, num56 * 2 > t.Pts.Count);
			}).ToArray();
		}
		v.Tuyaux = v.Tuyaux.Where(((short X, short Y) tuyau) => MaisonSousLeTuyau(tuyau.X, tuyau.Y).L > 0).ToArray();
		(short X, short Y)[] connues = ((list != null && list.Count > 0) ? list.Take(12).ToArray() : Array.Empty<(short, short)>());
		v.Tuyaux = v.Tuyaux.Where(((short X, short Y) tuyau) => !connues.Any(((short X, short Y) c) => Math.Abs(c.X - tuyau.X) < 16 && Math.Abs(c.Y - tuyau.Y) < 28)).ToArray();
		List<(short, short)> list7 = new List<(short, short)>();
		(short, short)[] tuyaux = v.Tuyaux;
		for (int num53 = 0; num53 < tuyaux.Length; num53++)
		{
			(short, short) tuple = tuyaux[num53];
			short item = tuple.Item1;
			short item2 = tuple.Item2;
			Vitre2 vitre = MaisonSousLeTuyau(item, item2);
			int num54 = vitre.X0 + vitre.L / 2 + 26;
			int num55 = vitre.Y0 + vitre.H + 40;
			if (num54 > 8 && num55 > 8 && num54 < L - 8 && num55 < H - 8)
			{
				list7.Add(((short)num54, (short)num55));
			}
		}
		v.Poules = list7.ToArray();
		v.Tuyaux = v.Tuyaux.Concat(connues).ToArray();
		if (!v.Urbaine && v.Tuyaux.Length == 0 && v.Vitres.Length == 0)
		{
			return null;
		}
		return v;
		int B(int num56)
		{
			return px[num56 * 4 + 2];
		}
		bool Bois(int i3)
		{
			if (R(i3) > G(i3) && G(i3) >= B(i3) && R(i3) - B(i3) > 40)
			{
				float num56 = Luma(i3);
				if (num56 > 90f)
				{
					return num56 < 225f;
				}
				return false;
			}
			return false;
		}
		bool Brun(int i3)
		{
			if (R(i3) > G(i3) && G(i3) > B(i3) && R(i3) - B(i3) > 45)
			{
				float num56 = Luma(i3);
				if (num56 > 70f)
				{
					return num56 < 190f;
				}
				return false;
			}
			return false;
		}
		bool DansUnMur((List<int> Pts, int X0, int Y0, int X1, int Y1) t)
		{
			if (attributs == null)
			{
				return true;
			}
			int num56 = (t.X0 + t.X1) / 2 / Options.Instance.Map.TileWidth;
			int num57 = (t.Y0 + t.Y1) / 2 / Options.Instance.Map.TileHeight;
			int num58 = 0;
			for (int num59 = num57 - 2; num59 <= num57 + 3; num59++)
			{
				for (int num60 = num56 - 2; num60 <= num56 + 2; num60++)
				{
					if (num60 >= 0 && num59 >= 0 && num60 < attributs.GetLength(0) && num59 < attributs.GetLength(1))
					{
						MapAttribute mapAttribute = attributs[num60, num59];
						if (mapAttribute != null && mapAttribute.Type == MapAttributeType.Blocked)
						{
							num58++;
						}
					}
				}
			}
			return num58 >= 5;
		}
		int G(int num56)
		{
			return px[num56 * 4 + 1];
		}
		float Luma(int i3)
		{
			return (float)R(i3) * 0.3f + (float)G(i3) * 0.55f + (float)B(i3) * 0.15f;
		}
		Vitre2 MaisonSousLeTuyau(short tx, short ty)
		{
			return (from f in v.Vitres
				where f.Y0 > ty && f.Y0 - ty < 190 && Math.Abs(f.X0 + f.L / 2 - tx) < 80
				orderby f.Y0 - ty
				select f).FirstOrDefault();
		}
		bool OmbreAuSol(List<int> pts)
		{
			Dictionary<int, int> dictionary = new Dictionary<int, int>();
			foreach (int pt in pts)
			{
				int key = (R(pt) << 16) | (G(pt) << 8) | B(pt);
				dictionary[key] = dictionary.GetValueOrDefault(key) + 1;
			}
			dictionary.MaxBy((KeyValuePair<int, int> e) => e.Value).Deconstruct(out var key2, out var value);
			int num56 = key2;
			int num57 = value;
			int num58 = num56 >> 16;
			int num59 = (num56 >> 8) & 0xFF;
			int num60 = num56 & 0xFF;
			HashSet<int> hashSet = new HashSet<int>();
			int num61 = 0;
			int num62 = 0;
			foreach (int pt2 in pts)
			{
				int num63 = pt2 % L;
				int num64 = pt2 / L;
				for (int num65 = -2; num65 <= 2; num65++)
				{
					for (int num66 = -2; num66 <= 2; num66++)
					{
						int num67 = num63 + num66;
						int num68 = num64 + num65;
						if (num67 >= 0 && num68 >= 0 && num67 < L && num68 < H)
						{
							int num69 = num68 * L + num67;
							if (!masque[num69] && hashSet.Add(num69))
							{
								if (Math.Abs(B(num69) - num60) <= 12 && R(num69) >= num58 + 40 && G(num69) >= num59 + 15)
								{
									num61++;
								}
								if (G(num69) > R(num69) + 15 && G(num69) > B(num69) + 15)
								{
									num62++;
								}
							}
						}
					}
				}
			}
			if (hashSet.Count > 0)
			{
				if (!((float)num62 >= (float)hashSet.Count * 0.6f))
				{
					if ((float)num57 >= (float)pts.Count * 0.6f)
					{
						return (float)num61 >= (float)hashSet.Count * 0.1f;
					}
					return false;
				}
				return true;
			}
			return false;
		}
		bool Pave(int i3)
		{
			if (Luma(i3) > 195f)
			{
				return Sat(i3) < 28;
			}
			return false;
		}
		int R(int num56)
		{
			return px[num56 * 4];
		}
		bool Route(int i3)
		{
			if (B(i3) > R(i3) + 22 && Luma(i3) > 185f)
			{
				return B(i3) > G(i3);
			}
			return false;
		}
		int Sat(int i3)
		{
			return Math.Max(R(i3), Math.Max(G(i3), B(i3))) - Math.Min(R(i3), Math.Min(G(i3), B(i3)));
		}
		bool SurRoute(int num56, int num57)
		{
			return routes.Any(delegate((short A, short B, bool Horizontale) r)
			{
				if (!r.Horizontale)
				{
					if (num56 >= r.A)
					{
						return num56 <= r.B;
					}
					return false;
				}
				return num57 >= r.A && num57 <= r.B;
			});
		}
	}
}
