using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using Intersect.Client.Core;
using Intersect.Client.Core.Controls;
using Intersect.Client.Core.Sounds;
using Intersect.Client.Entities.Projectiles;
using Intersect.Client.Framework.Content;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.Framework.Input;
using Intersect.Client.General;
using Intersect.Client.Interface;
using Intersect.Client.Interface.Game.Techniques;
using Intersect.Client.Localization;
using Intersect.Client.Maps;
using Intersect.Client.Networking;
using Intersect.Client.Spells;
using Intersect.Config;
using Intersect.Core;
using Intersect.Enums;
using Intersect.Framework.Core;
using Intersect.Framework.Core.Entities;
using Intersect.Framework.Core.GameObjects.Animations;
using Intersect.Framework.Core.GameObjects.Maps;
using Intersect.GameObjects;
using Intersect.Models;
using Intersect.Network.Packets.Server;
using Microsoft.Extensions.Logging;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Intersect.Client.Entities;

internal static class Heritage
{
	internal static class Appuis
	{
		internal const long Jamais = long.MinValue;

		internal const int AvaleMaxMs = 250;

		private static readonly long[] _presseA = new long[4] { -9223372036854775808L, -9223372036854775808L, -9223372036854775808L, -9223372036854775808L };

		private static readonly bool[] _tenues = new bool[4];

		private static bool _coupTenu;

		private static long _coupRelacheA = long.MinValue;

		private static bool _coupPasEncoreVu;

		private static bool _avale;

		private static bool _relachePasEncoreVu;

		private static bool _relacheAvale;

		private static long _relacheAvaleA = long.MinValue;

		private static long _dernierPassageA = long.MinValue;

		private static int _raccourciTenu = -1;

		private static int _raccourciPasEncoreVu = -1;

		private static int _raccourciAvale = -1;

		private static long _raccourciRelacheA = long.MinValue;

		internal const float MargeDuCorpsPx = 2f;

		internal const float GlisseMax = 0.3f;

		internal static long[] PresseA => _presseA;

		internal static bool[] Tenues => _tenues;

		internal static bool CoupTenu
		{
			get
			{
				if (_coupTenu)
				{
					return !_relacheAvale;
				}
				return false;
			}
		}

		internal static long CoupPresseA { get; private set; } = long.MinValue;

		internal static bool CoupPasEncoreVu(long maintenant)
		{
			if (_coupPasEncoreVu && _dernierPassageA != long.MinValue)
			{
				return maintenant - _dernierPassageA <= 250;
			}
			return false;
		}

		internal static void Relever()
		{
			if (!HeritageRegles.Actif)
			{
				return;
			}
			if (Globals.GameState == GameStates.InGame)
			{
				Player me = Globals.Me;
				if (me != null)
				{
					bool flag = !Intersect.Client.Interface.Interface.HasInputFocus() && !CharteScreen.EstOuvert;
					Noter(DeplacementLibre.MaintenantLocal, flag && Controls.IsGameControlPressed(Control.MoveUp), flag && Controls.IsGameControlPressed(Control.MoveDown), flag && Controls.IsGameControlPressed(Control.MoveLeft), flag && Controls.IsGameControlPressed(Control.MoveRight), Controls.IsGameControlPressed(Control.AttackInteract));
					if (me.IsBusy)
					{
						OublierLAppuiAvale();
						OublierLeRelacheAvale();
					}
					bool flag2 = flag && !FenetreCharte.RetientLesRaccourcis;
					NoterLeRaccourci(DeplacementLibre.MaintenantLocal, flag2 ? CaseTenueDeLaBarre() : (-1));
					if (!flag2)
					{
						OublierLeRaccourciAvale();
					}
					return;
				}
			}
			Vider();
		}

		internal static void Noter(long maintenant, bool haut, bool bas, bool gauche, bool droite, bool coup)
		{
			NoterUneDirection(0, haut, maintenant);
			NoterUneDirection(1, bas, maintenant);
			NoterUneDirection(2, gauche, maintenant);
			NoterUneDirection(3, droite, maintenant);
			if (coup && !_coupTenu)
			{
				CoupPresseA = maintenant;
				_coupPasEncoreVu = true;
				if (_relachePasEncoreVu)
				{
					_relachePasEncoreVu = false;
					_relacheAvale = true;
				}
			}
			else if (!coup && _coupTenu)
			{
				_coupRelacheA = maintenant;
				if (_coupPasEncoreVu)
				{
					_avale = true;
				}
				else if (!_relacheAvale)
				{
					_relachePasEncoreVu = true;
					_relacheAvaleA = maintenant;
				}
			}
			_coupTenu = coup;
		}

		private static void NoterUneDirection(int touche, bool tenue, long maintenant)
		{
			if (tenue && !_tenues[touche])
			{
				_presseA[touche] = maintenant;
			}
			_tenues[touche] = tenue;
		}

		internal static bool AppuiAvale()
		{
			return AppuiAvale(DeplacementLibre.MaintenantLocal);
		}

		internal static bool AppuiAvale(long maintenant)
		{
			if (_relacheAvale && maintenant - _relacheAvaleA <= 250)
			{
				return false;
			}
			bool result = _avale && maintenant - _coupRelacheA <= 250;
			OublierLAppuiAvale();
			return result;
		}

		private static void OublierLAppuiAvale()
		{
			_avale = false;
			_coupPasEncoreVu = false;
		}

		internal static bool RelacheAvale()
		{
			return RelacheAvale(DeplacementLibre.MaintenantLocal);
		}

		internal static bool RelacheAvale(long maintenant)
		{
			_dernierPassageA = maintenant;
			bool result = _relacheAvale && maintenant - _relacheAvaleA <= 250;
			OublierLeRelacheAvale();
			return result;
		}

		private static void OublierLeRelacheAvale()
		{
			_relacheAvale = false;
			_relachePasEncoreVu = false;
		}

		internal static long MemoireRendue(long memoire, bool gestePartiDeLAppui, bool empeche, bool ennemiADeuxPas)
		{
			if (!(gestePartiDeLAppui || empeche) && ennemiADeuxPas)
			{
				return memoire;
			}
			return 0L;
		}

		internal static bool AppuiGardePasseDevant(bool appuiNeufEnAttente, bool empeche, bool gesteEnCours, long gardeJusqua, long maintenant)
		{
			if (appuiNeufEnAttente && !empeche && !gesteEnCours)
			{
				return gardeJusqua > maintenant;
			}
			return false;
		}

		private static int CaseTenueDeLaBarre()
		{
			int hotbarSlotCount = Options.Instance.Player.HotbarSlotCount;
			int result = -1;
			int num = 0;
			for (int i = 0; i < hotbarSlotCount; i++)
			{
				if (Controls.IsControlPressed((Control)(4096 + i + 1)))
				{
					result = i;
					num++;
				}
			}
			if (num <= 1)
			{
				return result;
			}
			List<(int, Keys, Keys)> list = new List<(int, Keys, Keys)>(num);
			for (int j = 0; j < hotbarSlotCount; j++)
			{
				if (Controls.IsControlPressed((Control)(4096 + j + 1), out ControlMapping _, out ControlBinding activeBinding))
				{
					list.Add((j, activeBinding.Modifier, activeBinding.Key));
				}
			}
			return CaseRetenue(list);
		}

		internal static int CaseRetenue(IEnumerable<(int Case, Keys Modificateur, Keys Touche)> tenues)
		{
			int result = -1;
			foreach (IGrouping<Keys, (int, Keys, Keys)> item in from tenue in tenues
				orderby tenue.Modificateur descending
				group tenue by tenue.Touche)
			{
				result = item.First().Item1;
			}
			return result;
		}

		internal static void NoterLeRaccourci(long maintenant, int caseTenue)
		{
			if (caseTenue != _raccourciTenu)
			{
				if (_raccourciTenu >= 0 && _raccourciTenu == _raccourciPasEncoreVu)
				{
					_raccourciAvale = _raccourciTenu;
					_raccourciRelacheA = maintenant;
					_raccourciPasEncoreVu = -1;
				}
				if (caseTenue >= 0)
				{
					_raccourciPasEncoreVu = caseTenue;
				}
				_raccourciTenu = caseTenue;
			}
		}

		internal static int RaccourciAvale()
		{
			return RaccourciAvale(DeplacementLibre.MaintenantLocal);
		}

		internal static int RaccourciAvale(long maintenant)
		{
			int result = ((_raccourciAvale >= 0 && maintenant - _raccourciRelacheA <= 250) ? _raccourciAvale : (-1));
			OublierLeRaccourciAvale();
			return result;
		}

		private static void OublierLeRaccourciAvale()
		{
			_raccourciAvale = -1;
			_raccourciPasEncoreVu = -1;
		}

		internal static bool ButeSurCeCorps(float ecartX, float ecartY, float versX, float versY, float contact)
		{
			float num = MathF.Sqrt(versX * versX + versY * versY);
			float num2 = MathF.Sqrt(ecartX * ecartX + ecartY * ecartY);
			if (num < 0.001f || num2 > contact + 2f)
			{
				return false;
			}
			if (num2 < 0.01f)
			{
				return true;
			}
			float num3 = (versX * ecartX + versY * ecartY) / (num * num2);
			float num4 = MathF.Abs(versX * ecartY - versY * ecartX) / (num * num2);
			if (num3 > 0f)
			{
				return num4 < 0.3f;
			}
			return false;
		}

		internal static void Vider()
		{
			for (int i = 0; i < _presseA.Length; i++)
			{
				_presseA[i] = long.MinValue;
				_tenues[i] = false;
			}
			_coupTenu = false;
			CoupPresseA = long.MinValue;
			_coupRelacheA = long.MinValue;
			OublierLAppuiAvale();
			_relacheAvaleA = long.MinValue;
			_dernierPassageA = long.MinValue;
			OublierLeRelacheAvale();
			_raccourciTenu = -1;
			_raccourciRelacheA = long.MinValue;
			OublierLeRaccourciAvale();
		}
	}

	internal static class Bandes
	{
		internal readonly record struct Bande(string Nom, int Largeur, int Hauteur, int AncreX, int AncreY, int Images, int MsParImage, bool SeTourne, bool Verticale = false)
		{
			public string Fichier { get; } = "heritage_" + Nom + ".png";

			public int DureeMs => Images * MsParImage;
		}

		internal const string Direct = "direct";

		internal const string Crochet = "crochet";

		internal const string Pied = "pied";

		internal const string Souffle = "souffle";

		internal const string Fauche = "fauche";

		internal const string Langues = "langues";

		internal const string Couronne = "couronne";

		internal const string MurEcorce = "mur_ecorce";

		internal const string MurPierre = "mur_pierre";

		internal const string MurRoche = "mur_roche";

		internal const string BouleTrainee = "boule_trainee";

		internal const string Kiai = "kiai";

		internal const string KiaiV = "kiai_v";

		internal const string KiaiSol = "kiai_sol";

		internal const string KiaiSolV = "kiai_sol_v";

		internal const string OndeLangue = "onde_langue";

		internal const string OndeLangueV = "onde_langue_v";

		internal const string ChargeFond = "charge_fond";

		internal const string ChargeDevant = "charge_devant";

		internal const string ChargeFond2 = "charge_fond_2";

		internal const string ChargeDevant2 = "charge_devant_2";

		internal const string ChargeFond3 = "charge_fond_3";

		internal const string ChargeDevant3 = "charge_devant_3";

		internal const float EchelleSimple = 1f;

		internal const float EchelleForte = 1.5f;

		internal const float EchelleDouble = 2f;

		private const int SignaleesMax = 32;

		internal static readonly Bande[] Table = new Bande[23]
		{
			new Bande("direct", 104, 52, 18, 26, 6, 42, SeTourne: true),
			new Bande("crochet", 72, 120, 24, 60, 6, 42, SeTourne: true),
			new Bande("pied", 132, 156, 18, 78, 6, 56, SeTourne: true),
			new Bande("souffle", 156, 114, 6, 57, 6, 50, SeTourne: true),
			new Bande("fauche", 176, 100, 88, 68, 6, 50, SeTourne: false),
			new Bande("langues", 72, 96, 36, 90, 6, 60, SeTourne: false),
			new Bande("couronne", 112, 52, 56, 38, 6, 50, SeTourne: false),
			new Bande("mur_ecorce", 96, 96, 72, 48, 6, 55, SeTourne: true),
			new Bande("mur_pierre", 96, 96, 72, 48, 6, 55, SeTourne: true),
			new Bande("mur_roche", 96, 96, 72, 48, 6, 55, SeTourne: true),
			new Bande("boule_trainee", 96, 26, 94, 13, 4, 60, SeTourne: true),
			new Bande("kiai", 200, 80, 6, 58, 9, 50, SeTourne: false),
			new Bande("kiai_v", 96, 220, 48, 10, 9, 50, SeTourne: false, Verticale: true),
			new Bande("kiai_sol", 152, 40, 4, 18, 9, 50, SeTourne: false),
			new Bande("kiai_sol_v", 56, 152, 28, 4, 9, 50, SeTourne: false, Verticale: true),
			new Bande("onde_langue", 44, 48, 12, 46, 6, 50, SeTourne: false),
			new Bande("onde_langue_v", 32, 48, 12, 46, 6, 50, SeTourne: false),
			new Bande("charge_fond", 56, 52, 28, 42, 4, 60, SeTourne: false),
			new Bande("charge_devant", 56, 52, 28, 42, 4, 60, SeTourne: false),
			new Bande("charge_fond_2", 56, 52, 28, 42, 4, 60, SeTourne: false),
			new Bande("charge_devant_2", 56, 52, 28, 42, 4, 60, SeTourne: false),
			new Bande("charge_fond_3", 56, 52, 28, 42, 4, 60, SeTourne: false),
			new Bande("charge_devant_3", 56, 52, 28, 42, 4, 60, SeTourne: false)
		};

		private static readonly Dictionary<string, Bande> ParNom = Table.ToDictionary<Bande, string>((Bande bande) => bande.Nom, StringComparer.Ordinal);

		private static readonly HashSet<string> Signalees = new HashSet<string>(StringComparer.Ordinal);

		internal static int Signalements => Signalees.Count;

		internal static bool Trouver(string nom, out Bande bande)
		{
			if (nom != null && ParNom.TryGetValue(nom, out bande))
			{
				return true;
			}
			bande = default(Bande);
			Signaler(nom ?? string.Empty, "n'est pas dans la table des bandes");
			return false;
		}

		internal static IGameTexture? Planche(in Bande bande)
		{
			IGameTexture gameTexture = Globals.ContentManager?.GetTexture(TextureType.Misc, bande.Fichier);
			if (gameTexture == null)
			{
				Signaler(bande.Fichier, "est absente de resources\\misc");
				return null;
			}
			if (gameTexture.Width != bande.Images * bande.Largeur || gameTexture.Height != 2 * bande.Hauteur)
			{
				Signaler(bande.Fichier, $"ne fait pas {bande.Images * bande.Largeur} x {2 * bande.Hauteur}");
				return null;
			}
			return gameTexture;
		}

		private static void Signaler(string quoi, string faute)
		{
			if (Signalees.Count < 32 && Signalees.Add(quoi))
			{
				ApplicationContext.Context.Value?.Logger.LogWarning("Heritage : la bande {Bande} {Faute} : son effet ne sera pas dessine", quoi, faute);
			}
		}

		internal static void Oublier()
		{
			Signalees.Clear();
		}

		internal static float Echelle(float demandee)
		{
			if (!(demandee >= 1.75f))
			{
				if (!(demandee >= 1.25f))
				{
					return 1f;
				}
				return 1.5f;
			}
			return 2f;
		}

		internal static int Image(in Bande bande, long age)
		{
			if (age < 0 || bande.MsParImage <= 0)
			{
				return -1;
			}
			long num = age / bande.MsParImage;
			if (num >= bande.Images)
			{
				return -1;
			}
			return (int)num;
		}

		internal static int ImageEnBoucle(in Bande bande, long age, long finMs = long.MaxValue)
		{
			if (age < 0 || age >= finMs || bande.MsParImage <= 0 || bande.Images <= 0)
			{
				return -1;
			}
			return (int)(age / bande.MsParImage % bande.Images);
		}

		internal static (FloatRect Source, FloatRect Destination, float Rotation) Rectangle(in Bande bande, float x, float y, float sensX, float sensY, float echelle, int image = 0)
		{
			float num = Echelle(echelle);
			float num2 = MathF.Round((float)bande.Largeur * num);
			float num3 = MathF.Round((float)bande.Hauteur * num);
			int num4 = Math.Clamp(image, 0, Math.Max(0, bande.Images - 1)) * bande.Largeur;
			if (bande.Verticale)
			{
				bool flag = sensY < 0f;
				int num5 = (flag ? (bande.Hauteur - bande.AncreY) : bande.AncreY);
				return (Source: new FloatRect(num4, flag ? bande.Hauteur : 0, bande.Largeur, bande.Hauteur), Destination: new FloatRect(MathF.Round(x - (float)bande.AncreX * num), MathF.Round(y - (float)num5 * num), num2, num3), Rotation: 0f);
			}
			bool flag2 = MathF.Abs(sensX) >= MathF.Abs(sensY);
			if (flag2 || !bande.SeTourne)
			{
				bool flag3 = flag2 && sensX < 0f;
				int num6 = (flag3 ? (bande.Largeur - bande.AncreX) : bande.AncreX);
				return (Source: new FloatRect(num4, flag3 ? bande.Hauteur : 0, bande.Largeur, bande.Hauteur), Destination: new FloatRect(MathF.Round(x - (float)num6 * num), MathF.Round(y - (float)bande.AncreY * num), num2, num3), Rotation: 0f);
			}
			bool flag4 = sensY > 0f;
			float num7 = MathF.Round(flag4 ? (x - num3 + (float)bande.AncreY * num) : (x - (float)bande.AncreY * num));
			float num8 = MathF.Round(flag4 ? (y - (float)bande.AncreX * num) : (y + (float)bande.AncreX * num - num2));
			return (Source: new FloatRect(num4, 0f, bande.Largeur, bande.Hauteur), Destination: new FloatRect(num7 + (num3 - num2) / 2f, num8 + (num2 - num3) / 2f, num2, num3), Rotation: flag4 ? 90f : (-90f));
		}

		internal static (FloatRect Source, FloatRect Destination, float Rotation) AuCap(in Bande bande, float x, float y, float capX, float capY, int image, float visiblePx)
		{
			int num = Math.Clamp(image, 0, Math.Max(0, bande.Images - 1)) * bande.Largeur;
			int num2 = Math.Clamp((int)MathF.Round((float)bande.AncreX - visiblePx, MidpointRounding.AwayFromZero), 0, bande.AncreX);
			int num3 = bande.Largeur - num2;
			int hauteur = bande.Hauteur;
			float num4 = MathF.Atan2(capY, capX);
			if (!float.IsFinite(num4))
			{
				num4 = 0f;
			}
			float num5 = (float)(bande.AncreX - num2) - (float)num3 / 2f;
			float num6 = (float)bande.AncreY - (float)hauteur / 2f;
			float num7 = MathF.Cos(num4);
			float num8 = MathF.Sin(num4);
			float num9 = MathF.Round(x) - (num5 * num7 - num6 * num8);
			float num10 = MathF.Round(y) - (num5 * num8 + num6 * num7);
			return (Source: new FloatRect(num + num2, 0f, num3, hauteur), Destination: new FloatRect(num9 - (float)num3 / 2f, num10 - (float)hauteur / 2f, num3, hauteur), Rotation: num4 * 180f / (float)Math.PI);
		}
	}

	internal static class Boule
	{
		internal readonly record struct Dessin(string Planche, int Colonnes, int Rangees, int Images, int MsParImage);

		private struct Volante
		{
			public object Cle;

			public float X;

			public float Y;

			public float CapX;

			public float CapY;

			public Guid Tireur;

			public long NeeA;

			public float NeeX;

			public float NeeY;

			public int Passe;

			public Dessin Dessin;
		}

		private struct Retenue
		{
			public Entity Corps;

			public Guid Tireur;

			public float CapX;

			public float CapY;

			public float X;

			public float Y;

			public float CorpsX;

			public float CorpsY;

			public float EcartX;

			public float EcartY;

			public long RetenueA;

			public long AttendDepuis;

			public long NeeA;

			public float NeeX;

			public float NeeY;

			public Dash? Glissade;

			public bool Arrive;

			public long ArriveA;

			public Action<float, float, long> Eclater;

			public Dessin Dessin;
		}

		private struct Eclat
		{
			public string Planche;

			public float X;

			public float Y;

			public long A;

			public int Echelle;
		}

		internal const int EnVolMax = 64;

		internal const int RetenuesMax = 16;

		internal const int EclatsMax = 8;

		internal const int PassesSansNouvelles = 2;

		internal const float MemeBoulePx = 48f;

		internal const int TraineeNaitPx = 12;

		internal const float TraineePoussePxParMs = 0.5f;

		internal const int TraineeLongPx = 47;

		internal const int TraineeRetraitPx = 6;

		internal const float DerriereLeBordPx = 13f;

		internal const int ApresLeChocMs = 40;

		internal const int RetenueMaxMs = 600;

		internal const int PerimeMs = 2000;

		internal const int Etincelles = 8;

		internal static readonly Coups.Jet JetDesEtincelles = new Coups.Jet(5f, 70f, 200f, 120f, 420, Tirets: false);

		internal static readonly Color[] Ors = new Color[4]
		{
			new Color(255, 248, 248, 72),
			new Color(255, 248, 248, 248),
			new Color(255, 248, 176, 40),
			new Color(255, 248, 208, 0)
		};

		internal static readonly Color CerneDesOrs = new Color(255, 74, 28, 6);

		internal const int ChiffreAttenduMs = 300;

		internal const string PlancheDuJeu = "boule_energie.png";

		internal const int PlancheDuJeuImages = 4;

		internal const int PlancheDuJeuMs = 60;

		internal const float PlancheVersLeHautDegres = 90f;

		internal const int EclatImageMs = 60;

		private static readonly Color Blanc = new Color(255, 255, 255, 255);

		private static readonly Dessin DessinDuJeu = new Dessin("boule_energie.png", 4, 1, 4, 60);

		private static readonly Volante[] LesVolantes = new Volante[64];

		private static readonly Retenue[] LesRetenues = new Retenue[16];

		private static readonly Eclat[] LesEclats = new Eclat[8];

		private static int _volantes;

		private static int _retenues;

		private static int _eclats;

		private static int _passe;

		private static bool _enRappel;

		private static float _rappelCapX;

		private static float _rappelCapY;

		private static Guid _rappelTireur;

		internal static int EnVolSuivies => _volantes;

		internal static int Retenues => _retenues;

		internal static int Nombre => _volantes + _retenues + _eclats;

		private static bool Vues
		{
			get
			{
				HeritageOptions heritage = Options.Instance.Combat.Heritage;
				if (heritage.TechniquesVu.Actif)
				{
					return heritage.Allure.Actif;
				}
				return false;
			}
		}

		internal static void EnVol(object boule, float x, float y, float capX, float capY, Guid tireurId)
		{
			NoterEnVol(boule, x, y, capX, capY, tireurId, Timing.Global.Milliseconds);
		}

		internal static void NoterEnVol(object boule, float x, float y, float capX, float capY, Guid tireurId, long maintenant)
		{
			if (boule == null || !Vues)
			{
				return;
			}
			for (int i = 0; i < _volantes; i++)
			{
				ref Volante reference = ref LesVolantes[i];
				if (reference.Cle == boule)
				{
					ref float x2 = ref reference.X;
					ref float y2 = ref reference.Y;
					ref float capX2 = ref reference.CapX;
					ref float capY2 = ref reference.CapY;
					ref int passe = ref reference.Passe;
					int passe2 = _passe;
					x2 = x;
					y2 = y;
					capX2 = capX;
					capY2 = capY;
					passe = passe2;
					return;
				}
			}
			if (EstUnJoueur(tireurId))
			{
				if (_volantes == 64)
				{
					RetirerLaVolante(0);
				}
				LesVolantes[_volantes++] = new Volante
				{
					Cle = boule,
					X = x,
					Y = y,
					CapX = capX,
					CapY = capY,
					Tireur = tireurId,
					NeeA = maintenant,
					NeeX = x,
					NeeY = y,
					Passe = _passe,
					Dessin = DessinDe(boule)
				};
			}
		}

		internal static Dessin DessinDe(object boule)
		{
			if (boule is ProjectileSpawns projectileSpawns)
			{
				Animation anim = projectileSpawns.Anim;
				if (anim != null)
				{
					AnimationDescriptor descriptor = anim.Descriptor;
					if (descriptor != null)
					{
						AnimationLayer animationLayer = ((!string.IsNullOrEmpty(descriptor.Upper?.Sprite)) ? descriptor.Upper : descriptor.Lower);
						if (animationLayer == null || string.IsNullOrEmpty(animationLayer.Sprite) || animationLayer.XFrames <= 0 || animationLayer.YFrames <= 0)
						{
							return DessinDuJeu;
						}
						return new Dessin(animationLayer.Sprite, animationLayer.XFrames, animationLayer.YFrames, Math.Clamp(animationLayer.FrameCount, 1, animationLayer.XFrames * animationLayer.YFrames), Math.Max(1, animationLayer.FrameSpeed));
					}
				}
			}
			return DessinDuJeu;
		}

		internal static float TraineeVisible(long age)
		{
			return 6f + MathF.Min(47f, 12f + (float)Math.Max(0L, age) * 0.5f);
		}

		internal static float TraineeVisible(long age, float parcouru)
		{
			if (parcouru > 0f)
			{
				return MathF.Min(TraineeVisible(age), parcouru);
			}
			return 0f;
		}

		internal static float Parcouru(float x, float y, float neeX, float neeY, float capX, float capY)
		{
			return (x - neeX) * capX + (y - neeY) * capY;
		}

		internal static float Tournee(float capX, float capY)
		{
			return MathF.Atan2(capY, capX) * 180f / (float)Math.PI + 90f;
		}

		internal static bool Retenir(Entity touche, float x, float y, float capX, float capY, Guid tireurId, long maintenant, Action<float, float, long> eclater)
		{
			if (_enRappel || !Vues)
			{
				return false;
			}
			Dessin dessin = DessinDuJeu;
			long neeA = maintenant;
			float neeY = y;
			float neeX = x;
			if (LaPlusProche(tireurId, x, y, out var index))
			{
				Dessin dessin2 = LesVolantes[index].Dessin;
				long neeA2 = LesVolantes[index].NeeA;
				float neeX2 = LesVolantes[index].NeeX;
				neeY = LesVolantes[index].NeeY;
				neeX = neeX2;
				neeA = neeA2;
				dessin = dessin2;
				RetirerLaVolante(index);
			}
			if (touche == null || eclater == null || _retenues == 16 || !EstUnJoueur(tireurId) || !EstUnMonstre(touche) || touche.IsDying || touche.IsDisposed)
			{
				return false;
			}
			(float X, float Y) tuple = Effets.PlaceDe(touche);
			float item = tuple.X;
			float item2 = tuple.Y;
			ref Retenue reference = ref LesRetenues[_retenues++];
			reference = new Retenue
			{
				Corps = touche,
				Tireur = tireurId,
				CapX = capX,
				CapY = capY,
				EcartX = x - item,
				EcartY = y - item2,
				RetenueA = maintenant,
				AttendDepuis = maintenant,
				NeeA = neeA,
				NeeX = neeX,
				NeeY = neeY,
				Eclater = eclater,
				Dessin = dessin
			};
			Placer(ref reference);
			Player me = Globals.Me;
			if (me != null && tireurId == me.Id)
			{
				CombatFeedback.MarkPredictedHit(touche.Id);
				HeritageTechniquesVuOptions techniquesVu = Options.Instance.Combat.Heritage.TechniquesVu;
				Chiffres.Attendre(tireurId, touche.Id, techniquesVu.ChiffreBoulePt, Chiffres.Orange, 300, maintenant);
			}
			return true;
		}

		internal static void SurPoussee(Entity entite, Dash glissade)
		{
			if (entite == null || glissade == null)
			{
				return;
			}
			for (int i = 0; i < _retenues; i++)
			{
				ref Retenue reference = ref LesRetenues[i];
				if (reference.Corps == entite && reference.Glissade == null && EstSaPoussee(glissade, reference.Tireur))
				{
					reference.Glissade = glissade;
				}
			}
		}

		internal static void Suivre(long maintenant)
		{
			if (_retenues == 0)
			{
				return;
			}
			int bouleAttenteMs = Options.Instance.Combat.Heritage.TechniquesVu.BouleAttenteMs;
			bool fige = maintenant < CombatFeedback.ArretJusqua;
			int num = 0;
			while (num < _retenues)
			{
				if (Math.Abs(maintenant - LesRetenues[num].RetenueA) > 2000)
				{
					RetirerLaRetenue(num);
				}
				else if (!EstDue(ref LesRetenues[num], bouleAttenteMs, fige, maintenant))
				{
					num++;
				}
				else
				{
					Eclater(num, maintenant);
				}
			}
		}

		private static bool EstDue(ref Retenue retenue, int attenteMs, bool fige, long maintenant)
		{
			Entity corps = retenue.Corps;
			if (!EstLa(corps))
			{
				return true;
			}
			Placer(ref retenue);
			if (corps.IsDying)
			{
				return true;
			}
			Dash dashing = corps.Dashing;
			if (retenue.Glissade == null && dashing != null && EstSaPoussee(dashing, retenue.Tireur))
			{
				retenue.Glissade = dashing;
			}
			Dash glissade = retenue.Glissade;
			if (glissade != null)
			{
				if (!retenue.Arrive)
				{
					if (dashing == glissade && maintenant < glissade.EndsAt)
					{
						return false;
					}
					retenue.Arrive = true;
					retenue.ArriveA = Math.Min(maintenant, glissade.EndsAt);
				}
				return maintenant >= retenue.ArriveA + (Decor.SeJoue(glissade.ArretHeritage, glissade.DegatsDArretHeritage) ? 40 : 0);
			}
			if (fige || dashing != null)
			{
				retenue.AttendDepuis = maintenant;
			}
			if (maintenant - retenue.AttendDepuis < attenteMs)
			{
				return maintenant - retenue.RetenueA >= 600;
			}
			return true;
		}

		private static void Placer(ref Retenue retenue)
		{
			Entity corps = retenue.Corps;
			System.Numerics.Vector2 origin = corps.Origin;
			float num = Echanges.Hauteur(corps);
			ref float corpsX = ref retenue.CorpsX;
			ref float corpsY = ref retenue.CorpsY;
			float x = origin.X;
			float num2 = origin.Y - num;
			corpsX = x;
			corpsY = num2;
			(float X, float Y) tuple = Coups.Contact(origin.X, origin.Y, num, corps.RayonDuCorps, retenue.CapX, retenue.CapY);
			float item = tuple.X;
			float item2 = tuple.Y;
			retenue.X = item - retenue.CapX * 13f;
			retenue.Y = item2 - retenue.CapY * 13f;
		}

		private static void Eclater(int index, long maintenant)
		{
			Retenue retenue = LesRetenues[index];
			RetirerLaRetenue(index);
			_enRappel = true;
			float capX = retenue.CapX;
			float capY = retenue.CapY;
			Guid tireur = retenue.Tireur;
			_rappelCapX = capX;
			_rappelCapY = capY;
			_rappelTireur = tireur;
			try
			{
				retenue.Eclater(retenue.CorpsX + retenue.EcartX, retenue.CorpsY + retenue.EcartY, maintenant);
			}
			finally
			{
				_enRappel = false;
			}
		}

		internal static bool EclatDouble(string planche, float x, float y, long maintenant)
		{
			if (!_enRappel || !Vues)
			{
				return false;
			}
			HeritageTechniquesVuOptions techniquesVu = Options.Instance.Combat.Heritage.TechniquesVu;
			Particules.Lancer(x, y, 8, MathF.Atan2(_rappelCapY, _rappelCapX), JetDesEtincelles.Ouvert, JetDesEtincelles.VMin, JetDesEtincelles.VMax, JetDesEtincelles.Gravite, JetDesEtincelles.VieMs, Ors, JetDesEtincelles.Tirets, float.NaN, 0, CerneDesOrs, Globals.Random, maintenant);
			Player me = Globals.Me;
			if (me != null && _rappelTireur == me.Id)
			{
				CombatFeedback.ImpactHeritage(techniquesVu.BouleArretMs, (float)techniquesVu.BouleSecousseDixiemes / 10f, techniquesVu.BouleSecousseMs, _rappelCapX, _rappelCapY, maintenant);
			}
			if (techniquesVu.BouleEclatEchelle <= 1 || string.IsNullOrEmpty(planche) || Globals.ContentManager?.GetTexture(TextureType.Animation, planche) == null)
			{
				return false;
			}
			if (_eclats == 8)
			{
				Array.Copy(LesEclats, 1, LesEclats, 0, --_eclats);
			}
			LesEclats[_eclats++] = new Eclat
			{
				Planche = planche,
				X = x,
				Y = y,
				A = maintenant,
				Echelle = techniquesVu.BouleEclatEchelle
			};
			return true;
		}

		internal static void Lacher()
		{
			Lacher(Timing.Global.Milliseconds);
		}

		internal static void Lacher(long maintenant)
		{
			while (_retenues > 0)
			{
				Eclater(0, maintenant);
			}
			Oublier();
		}

		internal static void Oublier()
		{
			Array.Clear(LesVolantes, 0, _volantes);
			Array.Clear(LesRetenues, 0, _retenues);
			Array.Clear(LesEclats, 0, _eclats);
			_volantes = 0;
			_retenues = 0;
			_eclats = 0;
			_passe = 0;
		}

		internal static void Dessiner()
		{
			DessinerALHeure(Timing.Global.Milliseconds);
		}

		internal static void DessinerALHeure(long maintenant)
		{
			if (Nombre == 0)
			{
				return;
			}
			Bandes.Bande bande = default(Bandes.Bande);
			IGameTexture gameTexture = null;
			if (Options.Instance.Combat.Heritage.TechniquesVu.BouleTrainee && Bandes.Trouver("boule_trainee", out bande))
			{
				gameTexture = Bandes.Planche(in bande);
			}
			int num = 0;
			for (int i = 0; i < _volantes; i++)
			{
				ref Volante reference = ref LesVolantes[i];
				if (_passe - reference.Passe < 2)
				{
					if (gameTexture != null)
					{
						DessinerLaTrainee(in bande, gameTexture, reference.X, reference.Y, reference.CapX, reference.CapY, maintenant - reference.NeeA, Parcouru(reference.X, reference.Y, reference.NeeX, reference.NeeY, reference.CapX, reference.CapY));
					}
					if (num != i)
					{
						LesVolantes[num] = reference;
					}
					num++;
				}
			}
			Array.Clear(LesVolantes, num, _volantes - num);
			_volantes = num;
			if (maintenant >= CombatFeedback.ArretJusqua)
			{
				_passe++;
			}
			for (int j = 0; j < _retenues; j++)
			{
				ref Retenue reference2 = ref LesRetenues[j];
				if (EstLa(reference2.Corps))
				{
					Placer(ref reference2);
				}
				if (gameTexture != null)
				{
					DessinerLaTrainee(in bande, gameTexture, reference2.X, reference2.Y, reference2.CapX, reference2.CapY, maintenant - reference2.NeeA, Parcouru(reference2.X, reference2.Y, reference2.NeeX, reference2.NeeY, reference2.CapX, reference2.CapY));
				}
				DessinerLaBoule(in reference2, maintenant);
			}
			DessinerLesEclats(maintenant);
		}

		private static void DessinerLaTrainee(in Bandes.Bande bande, IGameTexture planche, float x, float y, float capX, float capY, long age, float parcouru)
		{
			int num = Bandes.ImageEnBoucle(in bande, age);
			if (num >= 0)
			{
				var (source, destination, rotation) = Bandes.AuCap(in bande, x, y, capX, capY, num, TraineeVisible(age, parcouru));
				Effets.Peindre(planche, source, destination, Blanc, rotation);
			}
		}

		private static void DessinerLaBoule(in Retenue retenue, long maintenant)
		{
			Dessin dessin = retenue.Dessin;
			IGameTexture gameTexture = Globals.ContentManager?.GetTexture(TextureType.Animation, dessin.Planche);
			if (gameTexture != null)
			{
				int num = gameTexture.Width / dessin.Colonnes;
				int num2 = gameTexture.Height / dessin.Rangees;
				if (num > 0 && num2 > 0)
				{
					int num3 = (int)(Math.Max(0L, maintenant - retenue.NeeA) / dessin.MsParImage % dessin.Images);
					Effets.Peindre(gameTexture, new FloatRect(num3 % dessin.Colonnes * num, num3 / dessin.Colonnes * num2, num, num2), new FloatRect(MathF.Round(retenue.X - (float)num / 2f), MathF.Round(retenue.Y - (float)num2 / 2f), num, num2), Blanc, Tournee(retenue.CapX, retenue.CapY));
				}
			}
		}

		private static void DessinerLesEclats(long maintenant)
		{
			int num = 0;
			for (int i = 0; i < _eclats; i++)
			{
				ref Eclat reference = ref LesEclats[i];
				IGameTexture gameTexture = Globals.ContentManager?.GetTexture(TextureType.Animation, reference.Planche);
				int num2 = ((gameTexture != null && gameTexture.Height > 0) ? (gameTexture.Width / gameTexture.Height) : 0);
				long num3 = maintenant - reference.A;
				long num4 = num3 / 60;
				if (gameTexture != null && num3 >= 0 && num4 < num2)
				{
					int height = gameTexture.Height;
					int num5 = height * reference.Echelle;
					Effets.Peindre(gameTexture, new FloatRect(num4 * height, 0f, height, height), new FloatRect(MathF.Round(reference.X - (float)num5 / 2f), MathF.Round(reference.Y - (float)num5 / 2f), num5, num5), Blanc, 0f);
					if (num != i)
					{
						LesEclats[num] = reference;
					}
					num++;
				}
			}
			Array.Clear(LesEclats, num, _eclats - num);
			_eclats = num;
		}

		private static bool EstUnJoueur(Guid id)
		{
			if (id != Guid.Empty && Globals.Entities.TryGetValue(id, out Entity value))
			{
				return value is Player;
			}
			return false;
		}

		private static bool EstUnMonstre(Entity corps)
		{
			if (!(corps is Player))
			{
				return corps.Type == EntityType.GlobalEntity;
			}
			return false;
		}

		private static bool EstLa(Entity corps)
		{
			if (corps != null && !corps.IsDisposed && Globals.Entities.TryGetValue(corps.Id, out Entity value))
			{
				return value == corps;
			}
			return false;
		}

		private static bool EstSaPoussee(Dash glissade, Guid tireur)
		{
			if (Mouvement.GenreDe(glissade) == Mouvement.Genre.Technique)
			{
				if (!(glissade.AuteurId == Guid.Empty))
				{
					return glissade.AuteurId == tireur;
				}
				return true;
			}
			return false;
		}

		private static bool LaPlusProche(Guid tireur, float x, float y, out int index)
		{
			index = -1;
			float num = 2304f;
			for (int i = 0; i < _volantes; i++)
			{
				ref Volante reference = ref LesVolantes[i];
				if (!(reference.Tireur != tireur))
				{
					float num2 = reference.X - x;
					float num3 = reference.Y - y;
					float num4 = num2 * num2 + num3 * num3;
					if (num4 <= num)
					{
						num = num4;
						index = i;
					}
				}
			}
			return index >= 0;
		}

		private static void RetirerLaVolante(int index)
		{
			Array.Copy(LesVolantes, index + 1, LesVolantes, index, _volantes - index - 1);
			LesVolantes[--_volantes] = default(Volante);
		}

		private static void RetirerLaRetenue(int index)
		{
			Array.Copy(LesRetenues, index + 1, LesRetenues, index, _retenues - index - 1);
			LesRetenues[--_retenues] = default(Retenue);
		}
	}

	internal static class Chiffres
	{
		internal enum Genre : byte
		{
			Coup,
			Critique,
			Decor
		}

		internal struct Chiffre
		{
			public Guid Cible;

			public Genre Genre;

			public string Texte;

			public int Points;

			public float X;

			public float MilieuY;

			public float TeteY;

			public float MonteePx;

			public Guid Auteur;

			public float EcartX;

			public float EcartMilieu;

			public float EcartTete;

			public float EcartPied;

			public float HauteurMin;

			public float DecaleX;

			public sbyte CoteDuDecale;

			public long Vu;

			public sbyte Cote;

			public long Voisin;

			public long Depart;

			public int VieMs;

			public bool DesAutres;

			public bool DeMoi;

			public Color? Couleur;

			public long Somme;

			public Mesure Mesure;

			public float MesurePour;
		}

		internal readonly record struct Mesure(float Largeur, float Haut, float Bas)
		{
			public float Hauteur => Bas - Haut;
		}

		internal readonly record struct Ecrit(string Texte, int Taille, FloatRect Encre, float Echelle, Color Couleur, Color Cerne);

		private struct Attendu
		{
			public Guid Attaquant;

			public Guid Cible;

			public int Points;

			public Color Couleur;

			public long Jusqua;
		}

		internal const int MotRetenuMs = 300;

		internal const float DecalagePx = 10f;

		internal const float EcartACotePx = 8f;

		internal const float EcartAuDessusDuMotPx = 2f;

		internal const float ACoteDeLAuteurPx = 4f;

		internal const float GlissePxParMs = 0.15f;

		internal const int CoupsEcritsMax = 64;

		internal const int MotsMesuresMax = 32;

		internal const float AuDessusPx = 12f;

		internal const float DecorAuDessusPx = 16f;

		internal const float DecorEnArrierePx = 4f;

		internal const float MargePx = 2f;

		internal const float NaissanceEchelle = 1.35f;

		internal const int RetourMs = 110;

		internal const float MonteePx = 14f;

		internal const float DecorMonteePx = 18f;

		internal const float PartDuFondu = 1f / 3f;

		internal const int OpaciteDesAutres = 60;

		internal const int ChiffresMax = 48;

		internal const int MotsMax = 64;

		internal const int AttendusMax = 16;

		internal const int SommePleine = 600;

		internal static readonly Color Casse = new Color(255, 244, 239, 226);

		internal static readonly Color CerneDuCoup = new Color(255, 26, 20, 16);

		internal static readonly Color Orange = new Color(255, 255, 176, 46);

		internal static readonly Color Or = new Color(255, 255, 210, 74);

		internal static readonly Color CerneBrun = new Color(255, 58, 26, 4);

		internal static readonly Color Rouge = new Color(255, 232, 64, 64);

		private static readonly Chiffre[] Places = new Chiffre[48];

		private static int _nombre;

		private static readonly Dictionary<Guid, long> MotsRetenus = new Dictionary<Guid, long>();

		private static readonly Dictionary<(Guid Auteur, Guid Cible), long> CoupsEcrits = new Dictionary<(Guid, Guid), long>();

		private static readonly FloatRect[] EncresVues = new FloatRect[48];

		private static int _encresVues;

		private static readonly Dictionary<(string Texte, int Taille), (float Ligne, float Haut)> MotsMesures = new Dictionary<(string, int), (float, float)>();

		private static readonly Attendu[] Attendus = new Attendu[16];

		private static int _attendus;

		internal static Action<Ecrit>? Temoin;

		internal static Func<string, int, Mesure>? Metre;

		internal static Func<string, int, (float Ligne, float Haut)>? MetreDesMots;

		internal static int Nombre => _nombre;

		internal static ReadOnlySpan<Chiffre> Liste => Places.AsSpan(0, _nombre);

		internal static int Mots => MotsRetenus.Count;

		internal static int EnAttente => _attendus;

		internal static bool Prendre(ActionMsgPacket paquet)
		{
			return Prendre(paquet, Timing.Global.Milliseconds);
		}

		internal static bool Prendre(ActionMsgPacket paquet, long maintenant)
		{
			HeritageOptions heritage = Options.Instance.Combat.Heritage;
			HeritageAllureOptions allure = heritage.Allure;
			if (!allure.Actif || !allure.Chiffres)
			{
				return false;
			}
			bool flag = EstUnDegat(paquet.Message);
			if (!flag && !EstLeMotDuCritique(paquet.Message))
			{
				return false;
			}
			if (!Combattant(paquet.EntityId, out Entity cible))
			{
				return false;
			}
			if (!flag)
			{
				RetenirLeMot(cible.Id, maintenant);
				return true;
			}
			if (paquet.AttackerId == Guid.Empty)
			{
				return false;
			}
			Player me = Globals.Me;
			bool flag2 = me != null && paquet.AttackerId == me.Id;
			bool flag3 = me != null && cible.Id == me.Id;
			float ux = 0f;
			float uy = 0f;
			CoupHeritage coupHeritage = CoupHeritage.Aucun;
			Guid auteur = Guid.Empty;
			bool duCoup = false;
			if (Globals.Entities.TryGetValue(paquet.AttackerId, out Entity value) && value != null && value != cible)
			{
				(float X, float Y) tuple = Coups.Sens(value, cible);
				ux = tuple.X;
				uy = tuple.Y;
				coupHeritage = Coups.CoupDe(value, maintenant);
				auteur = value.Id;
				bool flag4 = coupHeritage != CoupHeritage.Aucun && EstLeChiffreDuCoup(value, cible.Id);
				duCoup = flag4 && flag2 && ImpactsPredits.EnAttente(cible.Id, maintenant) > 0;
			}
			bool flag5 = PrendreLeMot(cible.Id, maintenant);
			int num = Points(coupHeritage, flag5, allure);
			Color couleur = null;
			if (PrendreLAttendu(paquet.AttackerId, cible.Id, maintenant, out int points, out Color couleur2, duCoup))
			{
				num = (flag5 ? Math.Max(num, points) : points);
				couleur = (flag5 ? null : couleur2);
			}
			if (flag3)
			{
				couleur = Rouge;
			}
			if (flag2)
			{
				HeritageTechniquesVuOptions techniquesVu = heritage.TechniquesVu;
				if (techniquesVu != null && techniquesVu.Actif && techniquesVu.RayonUnSeulChiffre && Rayon.CloueParMoi(cible.Id) && Valeur(paquet.Message, out var valeur))
				{
					PoserLaSomme(cible, valeur, ux, uy, heritage, maintenant);
					return true;
				}
			}
			if (allure.ChiffreVieMs <= 0)
			{
				return true;
			}
			(float X, float Milieu) tuple2 = Ecarts(ux, uy, 10f, 12f);
			float item = tuple2.X;
			float item2 = tuple2.Milieu;
			Chiffre chiffre = new Chiffre
			{
				Cible = cible.Id,
				Auteur = auteur,
				Genre = (flag5 ? Genre.Critique : Genre.Coup),
				Texte = (flag3 ? paquet.Message.Trim() : Texte(paquet.Message)),
				Points = num,
				EcartX = item,
				EcartMilieu = item2,
				HauteurMin = ((coupHeritage == CoupHeritage.Montant) ? ((float)heritage.SoulevePx) : 0f),
				MonteePx = 14f,
				Depart = maintenant,
				VieMs = allure.ChiffreVieMs,
				DesAutres = (!flag2 && !flag3 && (me == null || cible.Id != me.TargetId)),
				DeMoi = flag2,
				Couleur = couleur
			};
			Naitre(ref chiffre, cible.BodyWorldPos, Echanges.Hauteur(cible));
			Poser(in chiffre);
			return true;
		}

		private static bool EstLeChiffreDuCoup(Entity auteur, Guid cibleId)
		{
			long finDuDernierCoupHeritage = auteur.FinDuDernierCoupHeritage;
			(Guid, Guid) key = (auteur.Id, cibleId);
			long value;
			long? finEcrite = (CoupsEcrits.TryGetValue(key, out value) ? new long?(value) : ((long?)null));
			if (!AttendSonChiffre(auteur.CibleDuDernierCoupHeritage, cibleId, finDuDernierCoupHeritage, finEcrite))
			{
				return false;
			}
			if (CoupsEcrits.Count >= 64)
			{
				CoupsEcrits.Clear();
			}
			CoupsEcrits[key] = finDuDernierCoupHeritage;
			return true;
		}

		internal static bool AttendSonChiffre(Guid cibleDuCoup, Guid cible, long finDuCoup, long? finEcrite)
		{
			if (cible != Guid.Empty && cibleDuCoup == cible)
			{
				return finEcrite != finDuCoup;
			}
			return false;
		}

		internal static int PointsDuRayon(long somme, int coupPt, int rayonPt)
		{
			int num = Math.Max(1, coupPt);
			int num2 = Math.Max(num, rayonPt);
			float num3 = Math.Clamp((float)somme / 600f, 0f, 1f);
			return num + (int)MathF.Round((float)(num2 - num) * num3);
		}

		internal static bool Valeur(string degat, out int valeur)
		{
			if (int.TryParse(Texte(degat), NumberStyles.None, CultureInfo.InvariantCulture, out valeur))
			{
				return valeur > 0;
			}
			return false;
		}

		private static void PoserLaSomme(Entity cible, int touche, float ux, float uy, HeritageOptions reglages, long maintenant)
		{
			HeritageTechniquesVuOptions techniquesVu = reglages.TechniquesVu;
			if (techniquesVu.ChiffreRayonVieMs <= 0)
			{
				return;
			}
			long num = touche;
			(float X, float Milieu) tuple = Ecarts(0f - ux, 0f - uy, 4f, 16f);
			float ecartX = tuple.X;
			float ecartMilieu = tuple.Milieu;
			float ecartTete = 0f;
			float ecartPied = 0f;
			float monteePx = 18f;
			for (int i = 0; i < _nombre; i++)
			{
				ref Chiffre reference = ref Places[i];
				if (!(reference.Cible != cible.Id) && reference.Somme > 0)
				{
					long num2 = maintenant - reference.Depart;
					if (num2 >= 0 && num2 < reference.VieMs)
					{
						float num3 = Montee(num2, reference.VieMs, reference.MonteePx);
						num += reference.Somme;
						ecartX = reference.EcartX;
						ecartMilieu = reference.EcartMilieu - num3;
						ecartTete = reference.EcartTete - num3;
						ecartPied = reference.EcartPied;
						monteePx = reference.MonteePx - num3;
					}
					break;
				}
			}
			Chiffre chiffre = new Chiffre
			{
				Cible = cible.Id,
				Auteur = (Globals.Me?.Id ?? Guid.Empty),
				Genre = Genre.Coup,
				Texte = num.ToString(CultureInfo.InvariantCulture),
				Points = PointsDuRayon(num, reglages.Allure.ChiffreCoupPt, techniquesVu.ChiffreRayonPt),
				EcartX = ecartX,
				EcartMilieu = ecartMilieu,
				EcartTete = ecartTete,
				EcartPied = ecartPied,
				MonteePx = monteePx,
				Depart = maintenant,
				VieMs = techniquesVu.ChiffreRayonVieMs,
				DesAutres = false,
				DeMoi = true,
				Couleur = Or,
				Somme = num
			};
			Naitre(ref chiffre, cible.BodyWorldPos, Echanges.Hauteur(cible));
			Poser(in chiffre);
		}

		internal static void Attendre(Guid attaquantId, Guid cibleId, int points, Color couleur, int ms)
		{
			Attendre(attaquantId, cibleId, points, couleur, ms, Timing.Global.Milliseconds);
		}

		internal static bool Attendre(Guid attaquantId, Guid cibleId, int points, Color couleur, int ms, long maintenant)
		{
			if (attaquantId == Guid.Empty || points <= 0 || ms <= 0 || (object)couleur == null)
			{
				return false;
			}
			int num = 0;
			for (int i = 0; i < _attendus; i++)
			{
				ref Attendu reference = ref Attendus[i];
				if (Attend(in reference, maintenant) && (!(reference.Attaquant == attaquantId) || !(reference.Cible == cibleId)))
				{
					Attendus[num++] = reference;
				}
			}
			if (num == 16)
			{
				Array.Copy(Attendus, 1, Attendus, 0, --num);
			}
			Attendus[num++] = new Attendu
			{
				Attaquant = attaquantId,
				Cible = cibleId,
				Points = points,
				Couleur = couleur,
				Jusqua = maintenant + ms
			};
			if (num < _attendus)
			{
				Array.Clear(Attendus, num, _attendus - num);
			}
			_attendus = num;
			return true;
		}

		internal static bool PrendreLAttendu(Guid attaquantId, Guid cibleId, long maintenant, out int points, out Color couleur, bool duCoup = false)
		{
			int num = -1;
			for (int i = 0; i < _attendus; i++)
			{
				ref Attendu reference = ref Attendus[i];
				if (reference.Attaquant != attaquantId || !Attend(in reference, maintenant))
				{
					continue;
				}
				if (reference.Cible == Guid.Empty)
				{
					num = i;
				}
				else if (!(reference.Cible != cibleId))
				{
					if (duCoup)
					{
						points = 0;
						couleur = null;
						return false;
					}
					int points2 = reference.Points;
					Color couleur2 = reference.Couleur;
					points = points2;
					couleur = couleur2;
					_attendus--;
					Array.Copy(Attendus, i + 1, Attendus, i, _attendus - i);
					Attendus[_attendus] = default(Attendu);
					return true;
				}
			}
			if (num >= 0)
			{
				int points2 = Attendus[num].Points;
				Color couleur2 = Attendus[num].Couleur;
				points = points2;
				couleur = couleur2;
				return true;
			}
			points = 0;
			couleur = null;
			return false;
		}

		private static bool Attend(in Attendu attendu, long maintenant)
		{
			return maintenant <= attendu.Jusqua;
		}

		internal static Color CerneDe(Color couleur)
		{
			if (!(couleur == Casse))
			{
				return CerneBrun;
			}
			return CerneDuCoup;
		}

		internal static void Decor(Entity cible, long valeur, MatiereDArret matiere, float sensX, float sensY)
		{
			Decor(cible, valeur, matiere, sensX, sensY, Timing.Global.Milliseconds);
		}

		internal static bool Decor(Entity cible, long valeur, MatiereDArret matiere, float sensX, float sensY, long maintenant)
		{
			HeritageAllureOptions allure = Options.Instance.Combat.Heritage.Allure;
			FloatRect corps = cible.BodyWorldPos;
			if (valeur <= 0 || allure.ChiffreDecorVieMs <= 0 || !(corps.Width > 0f) || !(corps.Height > 0f))
			{
				return false;
			}
			float num = MathF.Sqrt(sensX * sensX + sensY * sensY);
			float num2 = ((num > 0f && float.IsFinite(num)) ? (sensX / num) : 0f);
			float num3 = ((num > 0f && float.IsFinite(num)) ? (sensY / num) : 0f);
			if (Heritage.Decor.Pieds(cible, out var x, out var y))
			{
				corps = new FloatRect(x - corps.Width / 2f, y - corps.Height, corps.Width, corps.Height);
			}
			Player me = Globals.Me;
			bool flag = me != null && (cible.Id == me.Id || cible.Id == me.TargetId || PorteLeChiffreDeMonCoup(cible.Id, maintenant));
			int index;
			bool flag2 = LeCoupSur(cible.Id, maintenant, out index);
			Guid auteur = (flag2 ? Places[index].Auteur : ((flag && cible.Id != me.Id) ? me.Id : Guid.Empty));
			(float X, float Milieu) tuple = Ecarts(0f - num2, 0f - num3, 4f, 16f);
			float item = tuple.X;
			float item2 = tuple.Milieu;
			Chiffre chiffre = new Chiffre
			{
				Cible = cible.Id,
				Auteur = auteur,
				Genre = Genre.Decor,
				Texte = "+" + valeur.ToString(CultureInfo.InvariantCulture),
				Points = PointsDuDecor(matiere, allure),
				EcartX = item,
				EcartMilieu = item2,
				Cote = CoteDuDecor(matiere, num2),
				Voisin = (flag2 ? Places[index].Depart : 0),
				MonteePx = 18f,
				Depart = maintenant,
				VieMs = allure.ChiffreDecorVieMs,
				DesAutres = !flag,
				DeMoi = false
			};
			Naitre(ref chiffre, corps, Echanges.Hauteur(cible));
			Poser(in chiffre);
			return true;
		}

		internal static sbyte CoteDuDecor(MatiereDArret matiere, float ux)
		{
			return (sbyte)((matiere != MatiereDArret.Corps || !(ux >= 0f)) ? 1 : (-1));
		}

		internal static void Oublier()
		{
			Array.Clear(Places, 0, _nombre);
			_nombre = 0;
			MotsRetenus.Clear();
			Array.Clear(Attendus, 0, _attendus);
			_attendus = 0;
			CoupsEcrits.Clear();
			MotsMesures.Clear();
			_encresVues = 0;
		}

		internal static bool EstUnDegat(string? texte)
		{
			if (texte != null && texte.Length > 1 && texte[0] == '-')
			{
				return char.IsDigit(texte[1]);
			}
			return false;
		}

		internal static bool EstLeMotDuCritique(string? texte)
		{
			if (!string.IsNullOrEmpty(texte))
			{
				return texte.Contains("crit", StringComparison.OrdinalIgnoreCase);
			}
			return false;
		}

		internal static string Texte(string degat)
		{
			return degat.Substring(1, degat.Length - 1).Trim();
		}

		private static bool Combattant(Guid id, out Entity cible)
		{
			cible = null;
			if (id == Guid.Empty || !Globals.Entities.TryGetValue(id, out Entity value) || value == null)
			{
				return false;
			}
			cible = value;
			FloatRect bodyWorldPos = value.BodyWorldPos;
			EntityType type = value.Type;
			bool flag = (uint)type <= 1u;
			if (flag && bodyWorldPos.Width > 0f)
			{
				return bodyWorldPos.Height > 0f;
			}
			return false;
		}

		internal static void RetenirLeMot(Guid cible, long maintenant)
		{
			if (MotsRetenus.Count >= 64 && !MotsRetenus.ContainsKey(cible))
			{
				MotsRetenus.Clear();
			}
			MotsRetenus[cible] = maintenant;
		}

		internal static bool PrendreLeMot(Guid cible, long maintenant)
		{
			if (MotsRetenus.Remove(cible, out var value))
			{
				return Math.Abs(maintenant - value) < 300;
			}
			return false;
		}

		internal static int Points(CoupHeritage coup, bool critique, HeritageAllureOptions allure)
		{
			int val = (critique ? allure.ChiffreCritiquePt : ((coup == CoupHeritage.CoupDePied) ? allure.ChiffrePiedPt : (HeritageRegles.EstUneFauche(coup) ? allure.ChiffreFauchePt : allure.ChiffreCoupPt)));
			return Math.Max(1, val);
		}

		internal static int PointsDuDecor(MatiereDArret matiere, HeritageAllureOptions allure)
		{
			return Math.Max(1, matiere switch
			{
				MatiereDArret.Rocher => allure.ChiffreRocherPt, 
				MatiereDArret.Falaise => allure.ChiffreFalaisePt, 
				_ => allure.ChiffreArbrePt, 
			});
		}

		internal static (float X, float Y) Milieu(FloatRect corps, float hauteur, float ux, float uy, float decalagePx, float auDessusPx)
		{
			var (ecartX, ecartMilieu) = Ecarts(ux, uy, decalagePx, auDessusPx);
			return Suivie(corps, corps.Y - hauteur, ecartX, ecartMilieu);
		}

		internal static (float X, float Milieu) Ecarts(float ux, float uy, float decalagePx, float auDessusPx)
		{
			return (X: ux * decalagePx, Milieu: MathF.Min(0f, uy) * decalagePx - auDessusPx);
		}

		internal static (float X, float Y) Suivie(FloatRect corps, float tete, float ecartX, float ecartMilieu)
		{
			return (X: corps.X + corps.Width / 2f + ecartX, Y: tete + ecartMilieu);
		}

		internal static float Portee(float ecartMilieu, float ecartTete, float hauteurDEncre, float monteePx)
		{
			return monteePx + hauteurDEncre * 1.35f - Pied(ecartMilieu, hauteurDEncre, ecartTete);
		}

		internal static bool Couvre(float x, float demiLargeur, float haut, float bas, FloatRect auteur, float hauteurDeLAuteur, float rayonDeLAuteur)
		{
			float num = auteur.Y - hauteurDeLAuteur;
			float num2 = num + auteur.Height;
			float num3 = auteur.X + auteur.Width / 2f;
			if (num < bas && num2 > haut)
			{
				return MathF.Abs(num3 - x) < rayonDeLAuteur + demiLargeur;
			}
			return false;
		}

		internal static int CoteDeLAuteur(float cibleX, float auteurX)
		{
			if (!(cibleX < auteurX))
			{
				return 1;
			}
			return -1;
		}

		internal static float ACoteDeLAuteur(float milieuDeLAuteur, float rayonDeLAuteur, float demiLargeur, int cote)
		{
			return milieuDeLAuteur + ((cote < 0) ? (-1f) : 1f) * (rayonDeLAuteur + demiLargeur + 4f);
		}

		internal static float Glisse(float depuis, float vers, float pas)
		{
			if (!(depuis < vers))
			{
				return MathF.Max(vers, depuis - pas);
			}
			return MathF.Min(vers, depuis + pas);
		}

		internal static float Pas(long vu, long maintenant)
		{
			if (vu != 0L)
			{
				return 0.15f * (float)Math.Max(0L, maintenant - vu);
			}
			return float.PositiveInfinity;
		}

		internal static float ACote(float xDuVoisin, float demiLargeurDuVoisin, float demiLargeur, int cote)
		{
			return xDuVoisin + ((cote < 0) ? (-1f) : 1f) * (demiLargeurDuVoisin + 8f + demiLargeur);
		}

		internal static float Ecarte(float x, float pied, in Mesure mesure, float echelle, int cote, ReadOnlySpan<FloatRect> deja)
		{
			float demiLargeur = mesure.Largeur * echelle / 2f;
			int num = 0;
			for (int i = 0; i < deja.Length; i++)
			{
				FloatRect a = Encre(x, pied, in mesure, echelle);
				int num2 = -1;
				for (int j = 0; j < deja.Length; j++)
				{
					if (num2 >= 0)
					{
						break;
					}
					if (SeCroisent(in a, in deja[j]))
					{
						num2 = j;
					}
				}
				if (num2 < 0)
				{
					break;
				}
				float num3 = deja[num2].X + deja[num2].Width / 2f;
				if (num == 0)
				{
					num = ((x > num3) ? 1 : ((x < num3) ? (-1) : ((cote >= 0) ? 1 : (-1))));
				}
				x = ACote(num3, deja[num2].Width / 2f, demiLargeur, num);
			}
			return x;
		}

		internal static bool SeCroisent(in FloatRect a, in FloatRect b)
		{
			if (a.X < b.X + b.Width && b.X < a.X + a.Width && a.Y < b.Y + b.Height)
			{
				return b.Y < a.Y + a.Height;
			}
			return false;
		}

		internal static float HautDuMot(float basDeLigne, float ligne, float hautDEncre, float echelle)
		{
			return basDeLigne - (ligne - hautDEncre) * echelle;
		}

		internal static float AuDessusDuMot(float pied, float hautDuMot)
		{
			return MathF.Min(pied, hautDuMot - 2f);
		}

		internal static float Releve(float pied, float voulu, float pas)
		{
			return MathF.Min(pied, MathF.Max(voulu, pied - pas));
		}

		internal static bool PasseAuDessusDuMot(string? mot, long motDepuis, long chiffreDepuis)
		{
			if (!CombatFeedback.EstUnAvertissement(mot))
			{
				return motDepuis <= chiffreDepuis;
			}
			return true;
		}

		internal static float AuDessusDuChiffre(float basDeLigne, float hautDuChiffre)
		{
			return MathF.Min(0f, hautDuChiffre - 2f - basDeLigne);
		}

		internal static bool Efface(string? mot, long motDepuis, long chiffreDepuis)
		{
			if (motDepuis >= chiffreDepuis)
			{
				return string.Equals(mot, Strings.Echanges.Choc.ToString(), StringComparison.Ordinal);
			}
			return false;
		}

		internal static float Pied(float milieuY, float hauteurDEncre, float teteY)
		{
			return MathF.Min(milieuY + hauteurDEncre / 2f, teteY - 2f);
		}

		internal static FloatRect Encre(float x, float pied, in Mesure mesure, float echelle)
		{
			return new FloatRect(x - mesure.Largeur * echelle / 2f, pied - mesure.Hauteur * echelle, mesure.Largeur * echelle, mesure.Hauteur * echelle);
		}

		internal static float Echelle(long age)
		{
			if (age >= 110)
			{
				return 1f;
			}
			return 1.35f - 0.35000002f * (float)Math.Max(0L, age) / 110f;
		}

		internal static float Montee(long age, int vieMs, float px)
		{
			if (vieMs <= 0)
			{
				return 0f;
			}
			return px * MathF.Sqrt(Math.Clamp((float)age / (float)vieMs, 0f, 1f));
		}

		internal static float Opacite(long age, int vieMs)
		{
			if (vieMs <= 0 || age >= vieMs)
			{
				return 0f;
			}
			float num = 1f - (float)Math.Max(0L, age) / (float)vieMs;
			if (!(num >= 1f / 3f))
			{
				return num / (1f / 3f);
			}
			return 1f;
		}

		internal static void Dessiner(long maintenant)
		{
			int num = 0;
			while (num < _nombre)
			{
				long num2 = maintenant - Places[num].Depart;
				if (num2 >= Places[num].VieMs || num2 <= -Places[num].VieMs || EffaceParLeChoc(in Places[num], maintenant))
				{
					Retirer(num);
				}
				else
				{
					num++;
				}
			}
			IFont actionMsgFont = Graphics.ActionMsgFont;
			_encresVues = 0;
			_encresVues = CombatFeedback.EncresDesAvertissements(EncresVues.AsSpan(0, 48 - _nombre));
			for (int i = 0; i < _nombre; i++)
			{
				Dessiner(ref Places[i], actionMsgFont, maintenant);
			}
		}

		private static void Dessiner(ref Chiffre chiffre, IFont? police, long maintenant)
		{
			long age = Math.Max(0L, maintenant - chiffre.Depart);
			int num = (int)MathF.Round(255f * Opacite(age, chiffre.VieMs));
			if (chiffre.DesAutres)
			{
				num = num * 60 / 100;
			}
			if (num <= 0)
			{
				return;
			}
			int taille = Mesuree(ref chiffre, police);
			float num2 = Echelle(age);
			float num3 = PiedDe(ref chiffre, Suivre(ref chiffre, maintenant), age, maintenant);
			if (chiffre.Genre == Genre.Decor && LeCoupSur(chiffre.Cible, maintenant, out var index))
			{
				ref Chiffre reference = ref Places[index];
				long age2 = Math.Max(0L, maintenant - reference.Depart);
				Mesuree(ref reference, police);
				float num4 = PiedDe(ref reference, Suivre(ref reference, maintenant), age2, maintenant);
				float num5 = ACote(reference.X, reference.Mesure.Largeur * Echelle(age2) / 2f, chiffre.Mesure.Largeur * num2 / 2f, chiffre.Cote);
				chiffre.EcartX += num5 - chiffre.X;
				chiffre.X = num5;
				if (reference.Depart == chiffre.Voisin)
				{
					chiffre.EcartPied += num4 - num3;
					num3 = num4;
				}
			}
			float num6 = Ecarte(chiffre.X, num3, in chiffre.Mesure, num2, chiffre.Cote, EncresVues.AsSpan(0, _encresVues));
			chiffre.EcartX += num6 - chiffre.X;
			chiffre.X = num6;
			EncresVues[_encresVues++] = Encre(chiffre.X, num3, in chiffre.Mesure, num2);
			Color couleur = chiffre.Couleur;
			Color pleine;
			Color pleine2;
			if ((object)couleur == null)
			{
				(pleine, pleine2) = Couleurs(chiffre.Genre);
			}
			else
			{
				Color color = CerneDe(couleur);
				pleine2 = color;
				pleine = couleur;
			}
			Ecrire(chiffre.Texte, police, taille, chiffre.X, num3, in chiffre.Mesure, num2, Teinte(pleine, num), Teinte(pleine2, num));
		}

		private static void Naitre(ref Chiffre chiffre, FloatRect corps, float hauteur)
		{
			Mesuree(ref chiffre, Graphics.ActionMsgFont);
			Placer(ref chiffre, corps, hauteur, chiffre.Depart);
		}

		private static int Mesuree(ref Chiffre chiffre, IFont? police)
		{
			int num = TexteMonde.Taille(chiffre.Points);
			float echelle = TexteMonde.Echelle;
			if (chiffre.MesurePour != echelle)
			{
				chiffre.Mesure = Mesurer(chiffre.Texte, police, num);
				chiffre.MesurePour = echelle;
			}
			return num;
		}

		private static Entity? Suivre(ref Chiffre chiffre, long maintenant)
		{
			if (!Globals.Entities.TryGetValue(chiffre.Cible, out Entity value) || value == null)
			{
				return null;
			}
			FloatRect bodyWorldPos = value.BodyWorldPos;
			if (bodyWorldPos.Width > 0f && bodyWorldPos.Height > 0f)
			{
				Placer(ref chiffre, bodyWorldPos, Echanges.Hauteur(value), maintenant);
			}
			return value;
		}

		private static void Placer(ref Chiffre chiffre, FloatRect corps, float hauteur, long maintenant)
		{
			if (chiffre.HauteurMin > 0f)
			{
				hauteur = MathF.Max(hauteur, chiffre.HauteurMin);
			}
			float num = corps.Y - hauteur;
			float num2 = corps.X + corps.Width / 2f;
			float vers = 0f;
			if (LAuteur(in chiffre, out Entity auteur))
			{
				FloatRect bodyWorldPos = auteur.BodyWorldPos;
				if (bodyWorldPos.Width > 0f && bodyWorldPos.Height > 0f)
				{
					float num3 = num2 + chiffre.EcartX;
					float demiLargeur = chiffre.Mesure.Largeur * 1.35f / 2f + 2f;
					float num4 = Portee(chiffre.EcartMilieu, chiffre.EcartTete, chiffre.Mesure.Hauteur, chiffre.MonteePx);
					if (Couvre(num3, demiLargeur, num - num4, num, bodyWorldPos, Echanges.Hauteur(auteur), auteur.RayonDuCorps))
					{
						float num5 = bodyWorldPos.X + bodyWorldPos.Width / 2f;
						if (chiffre.CoteDuDecale == 0)
						{
							chiffre.CoteDuDecale = (sbyte)CoteDeLAuteur(num2, num5);
						}
						vers = ACoteDeLAuteur(num5, auteur.RayonDuCorps, demiLargeur, chiffre.CoteDuDecale) - num3;
					}
				}
			}
			chiffre.DecaleX = Glisse(chiffre.DecaleX, vers, Pas(chiffre.Vu, maintenant));
			ref float x = ref chiffre.X;
			ref float milieuY = ref chiffre.MilieuY;
			(float, float) tuple = Suivie(corps, num, chiffre.EcartX + chiffre.DecaleX, chiffre.EcartMilieu);
			x = tuple.Item1;
			milieuY = tuple.Item2;
			chiffre.TeteY = num + chiffre.EcartTete;
		}

		private static bool LAuteur(in Chiffre chiffre, out Entity auteur)
		{
			auteur = null;
			if (chiffre.Auteur == Guid.Empty || chiffre.Auteur == chiffre.Cible || !Globals.Entities.TryGetValue(chiffre.Auteur, out Entity value) || value == null)
			{
				return false;
			}
			auteur = value;
			return true;
		}

		private static float PiedDe(ref Chiffre chiffre, Entity? cible, long age, long maintenant)
		{
			float num = Pied(chiffre.MilieuY, chiffre.Mesure.Hauteur, chiffre.TeteY) - Montee(age, chiffre.VieMs, chiffre.MonteePx) + chiffre.EcartPied;
			long vu = chiffre.Vu;
			chiffre.Vu = maintenant;
			if (cible == null || !HautDuMotDe(cible, chiffre.Depart, maintenant, out var haut))
			{
				return num;
			}
			float num2 = Releve(num, AuDessusDuMot(num, haut), Pas(vu, maintenant));
			chiffre.EcartPied += num2 - num;
			return num2;
		}

		private static bool HautDuMotDe(Entity cible, long depuis, long maintenant, out float haut)
		{
			haut = 0f;
			if (CombatFeedback.AvertissementDevant(cible.Id, maintenant))
			{
				return false;
			}
			if (!CombatFeedback.MotDe(cible.Id, maintenant, out var mot) || !PasseAuDessusDuMot(mot.Texte, mot.Depuis, depuis))
			{
				return false;
			}
			long age = maintenant - mot.Depuis;
			(float Ligne, float Haut) tuple = MesureDuMot(mot.Texte, mot.Taille);
			float item = tuple.Ligne;
			float item2 = tuple.Haut;
			haut = HautDuMot(CombatFeedback.BasDuMot(cible.WorldPos, age), item, item2, CombatFeedback.EchelleDuMot(age)) + CombatFeedback.ReleveDuMot(cible.Id, mot.Depuis);
			return true;
		}

		internal static bool HautDesChiffres(Guid cible, string mot, long neA, long maintenant, out float haut)
		{
			haut = float.PositiveInfinity;
			for (int i = 0; i < _nombre; i++)
			{
				ref Chiffre reference = ref Places[i];
				long num = maintenant - reference.Depart;
				if (!(reference.Cible != cible) && reference.Depart < neA && num >= 0 && num < reference.VieMs && (reference.Genre == Genre.Decor || reference.Somme != 0L || !Efface(mot, neA, reference.Depart)))
				{
					float num2 = Pied(reference.MilieuY, reference.Mesure.Hauteur, reference.TeteY) - Montee(num, reference.VieMs, reference.MonteePx) + reference.EcartPied;
					haut = MathF.Min(haut, num2 - reference.Mesure.Hauteur * Echelle(num));
				}
			}
			return haut < float.PositiveInfinity;
		}

		private static (float Ligne, float Haut) MesureDuMot(string texte, int taille)
		{
			if (MetreDesMots != null)
			{
				return MetreDesMots(texte, taille);
			}
			if (MotsMesures.TryGetValue((texte, taille), out (float, float) value))
			{
				return value;
			}
			GameRenderer renderer = Graphics.Renderer;
			if (renderer != null)
			{
				IFont policeDesMots = CombatFeedback.PoliceDesMots;
				if (policeDesMots != null)
				{
					(float, float) tuple = (renderer.MeasureText(texte, policeDesMots, taille, 1f).Y, renderer.MeasureTextInk(texte, policeDesMots, taille, 1f).Top);
					if (MotsMesures.Count >= 32)
					{
						MotsMesures.Clear();
					}
					MotsMesures[(texte, taille)] = tuple;
					return tuple;
				}
			}
			return default((float, float));
		}

		private static bool EffaceParLeChoc(in Chiffre chiffre, long maintenant)
		{
			if (chiffre.Genre != Genre.Decor && chiffre.Somme == 0L && CombatFeedback.MotDe(chiffre.Cible, maintenant, out var mot))
			{
				return Efface(mot.Texte, mot.Depuis, chiffre.Depart);
			}
			return false;
		}

		internal static (Color Couleur, Color Cerne) Couleurs(Genre genre)
		{
			return genre switch
			{
				Genre.Critique => (Couleur: Orange, Cerne: CerneBrun), 
				Genre.Decor => (Couleur: Or, Cerne: CerneBrun), 
				_ => (Couleur: Casse, Cerne: CerneDuCoup), 
			};
		}

		private static Color Teinte(Color pleine, int alpha)
		{
			if (alpha < 255)
			{
				return new Color(alpha, pleine.R, pleine.G, pleine.B);
			}
			return pleine;
		}

		private static Mesure Mesurer(string texte, IFont? police, int taille)
		{
			if (Metre != null)
			{
				return Metre(texte, taille);
			}
			GameRenderer renderer = Graphics.Renderer;
			if (renderer == null)
			{
				return default(Mesure);
			}
			float echelle = TexteMonde.Echelle;
			var (haut, bas) = renderer.MeasureTextInk(texte, police, taille, echelle);
			return new Mesure(renderer.MeasureText(texte, police, taille, echelle).X, haut, bas);
		}

		private static void Ecrire(string texte, IFont? police, int taille, float x, float pied, in Mesure mesure, float echelle, Color couleur, Color cerne)
		{
			Temoin?.Invoke(new Ecrit(texte, taille, Encre(x, pied, in mesure, echelle), echelle, couleur, cerne));
			GameRenderer moteur = Graphics.Renderer;
			if (moteur == null)
			{
				return;
			}
			float gauche = x - mesure.Largeur * echelle / 2f;
			float haut = pied - mesure.Bas * echelle;
			if (echelle == 1f)
			{
				TexteMonde.Ecrire(texte, police, taille, gauche, haut, couleur, cerne);
				return;
			}
			float grossi = TexteMonde.Echelle * echelle;
			TexteMonde.Lisse(delegate
			{
				moteur.DrawString(texte, police, taille, gauche, haut, grossi, couleur, worldPos: true, null, cerne);
			});
		}

		internal static void Poser(in Chiffre chiffre)
		{
			bool flag = chiffre.Genre == Genre.Decor;
			for (int i = 0; i < _nombre; i++)
			{
				if (Places[i].Cible == chiffre.Cible && Places[i].Genre == Genre.Decor == flag)
				{
					Retirer(i);
					break;
				}
			}
			if (_nombre == 48)
			{
				Retirer(0);
			}
			Places[_nombre++] = chiffre;
		}

		private static bool PorteLeChiffreDeMonCoup(Guid cible, long maintenant)
		{
			for (int i = 0; i < _nombre; i++)
			{
				ref Chiffre reference = ref Places[i];
				if (reference.Cible == cible && reference.Genre != Genre.Decor)
				{
					if (reference.DeMoi)
					{
						return maintenant - reference.Depart < reference.VieMs;
					}
					return false;
				}
			}
			return false;
		}

		private static bool LeCoupSur(Guid cible, long maintenant, out int index)
		{
			for (int i = 0; i < _nombre; i++)
			{
				ref Chiffre reference = ref Places[i];
				if (reference.Cible == cible && reference.Genre != Genre.Decor)
				{
					index = i;
					long num = maintenant - reference.Depart;
					if (num < reference.VieMs)
					{
						return num > -reference.VieMs;
					}
					return false;
				}
			}
			index = -1;
			return false;
		}

		private static void Retirer(int index)
		{
			_nombre--;
			Array.Copy(Places, index + 1, Places, index, _nombre - index);
			Places[_nombre] = default(Chiffre);
		}
	}

	internal static class Choix
	{
		internal static readonly Direction[] Touches = new Direction[4]
		{
			Direction.Up,
			Direction.Down,
			Direction.Left,
			Direction.Right
		};

		internal static (CoupHeritage Coup, Direction Touche, long Front, SensDeDirection Sens) Demande(long[] pressA, bool[] tenues, long appuiDeE, long depart, long dernierFrontUtilise, bool lectureTenue, float ecartX, float ecartY, int niveau)
		{
			Direction direction = Direction.None;
			long num = long.MinValue;
			SensDeDirection sensDeDirection = SensDeDirection.Aucun;
			for (int i = 0; i < Touches.Length; i++)
			{
				if (Tapee(pressA[i], tenues[i], appuiDeE, depart, dernierFrontUtilise) || (lectureTenue && tenues[i]))
				{
					SensDeDirection sensDeDirection2 = HeritageRegles.Sens(Touches[i], ecartX, ecartY);
					bool flag = sensDeDirection2 - 1 <= SensDeDirection.VersLui;
					bool flag2 = flag;
					flag = sensDeDirection - 1 <= SensDeDirection.VersLui;
					bool flag3 = flag;
					if ((!flag3 || flag2) && (direction == Direction.None || (flag2 && !flag3) || pressA[i] > num))
					{
						direction = Touches[i];
						num = pressA[i];
						sensDeDirection = sensDeDirection2;
					}
				}
			}
			CoupHeritage coupHeritage = sensDeDirection switch
			{
				SensDeDirection.VersLui => CoupHeritage.CoupDEpaule, 
				SensDeDirection.Oppose => CoupHeritage.Montant, 
				SensDeDirection.DeCote => HeritageRegles.FaucheDuCote(direction, ecartX, ecartY), 
				_ => CoupHeritage.Aucun, 
			};
			if (coupHeritage != CoupHeritage.Aucun && niveau < HeritageRegles.Table(coupHeritage).NiveauRequis)
			{
				coupHeritage = CoupHeritage.Aucun;
			}
			return (Coup: coupHeritage, Touche: direction, Front: num, Sens: sensDeDirection);
		}

		internal static (float X, float Y) DirectionTapee(long[] pressA, long appuiDeE, long depart, long dernierFrontUtilise, bool[]? tenues = null)
		{
			float num = 0f;
			float num2 = 0f;
			for (int i = 0; i < Touches.Length; i++)
			{
				if (Tapee(pressA[i], tenues != null && tenues[i], appuiDeE, depart, dernierFrontUtilise))
				{
					(float X, float Y) tuple = CorpsACorps.Vecteur(Touches[i]);
					float item = tuple.X;
					float item2 = tuple.Y;
					num += item;
					num2 += item2;
				}
			}
			float num3 = MathF.Sqrt(num * num + num2 * num2);
			if (!(num3 > 0f))
			{
				return (X: 0f, Y: 0f);
			}
			return (X: num / num3, Y: num2 / num3);
		}

		private static bool Tapee(long front, bool tenue, long appuiDeE, long depart, long dernierFrontUtilise)
		{
			if (front == long.MinValue || appuiDeE == long.MinValue || front <= dernierFrontUtilise)
			{
				return false;
			}
			HeritageOptions heritage = Options.Instance.Combat.Heritage;
			if (tenue)
			{
				if (front >= appuiDeE - heritage.DirectionEnMemeTempsMs)
				{
					return front <= Math.Min(appuiDeE, depart);
				}
				return false;
			}
			if (front >= appuiDeE - heritage.DirectionTapeeMs)
			{
				return front <= Math.Min(appuiDeE + heritage.DirectionEnMemeTempsMs, depart);
			}
			return false;
		}

		internal static (Direction Touche, long Front) DerniereTouche(long[] pressA, long depart)
		{
			Direction item = Direction.None;
			long num = long.MinValue;
			for (int i = 0; i < Touches.Length; i++)
			{
				if (pressA[i] != long.MinValue && pressA[i] <= depart && pressA[i] > num)
				{
					item = Touches[i];
					num = pressA[i];
				}
			}
			return (Touche: item, Front: num);
		}

		internal static int Avance(CoupHeritage coup, CoupHeritage demande, bool poseMontree, long poseDepuis, long finDuCoupPrecedent, long maintenant)
		{
			if (HeritageRegles.EstADirection(demande) || !poseMontree)
			{
				return 0;
			}
			long num = Math.Clamp(maintenant - Math.Max(poseDepuis, finDuCoupPrecedent), 0L, 1000L);
			return HeritageRegles.AvanceBornee(coup, (int)num) / 4 * 4;
		}

		internal static bool EchoDeCeGeste(CoupHeritage coupEnCours, byte coupDeLEcho)
		{
			if (coupEnCours != CoupHeritage.Aucun)
			{
				return (uint)coupDeLEcho == (uint)coupEnCours;
			}
			return false;
		}

		internal static (long Fin, int Duree) FinRecalee(long fin, int dureeLocale, int dureeDuServeur)
		{
			int num = Math.Max(1, dureeDuServeur + Options.Instance.Combat.Heritage.MargeClientMs);
			return (Fin: fin + num - dureeLocale, Duree: num);
		}

		internal static CoupHeritage DemandeProbable(Player moi)
		{
			return moi.DemandeHeritageProbable();
		}
	}

	internal static class Cible
	{
		internal const float ChassePx = 160f;

		internal const float ReferencePx = 96f;

		private static readonly float CosinusDuDemiArc = MathF.Cos((float)Math.PI * 7f / 18f);

		internal static bool EnCombat(Player moi)
		{
			if (!HeritageRegles.Actif)
			{
				return false;
			}
			if (!SerieEnCours(moi) && !ViseParUnCoupArme(moi) && !ChasseDePres(moi))
			{
				return VientDEtreFrappe();
			}
			return true;
		}

		internal static bool SerieEnCours(Player moi)
		{
			if (HeritageRegles.Actif)
			{
				return EnVie(moi.EnnemiDeLaSerieHeritage);
			}
			return false;
		}

		internal static bool ViseParUnCoupArme(Player moi)
		{
			return Telegraphe.ArmeContre(moi.Id);
		}

		internal static bool ChasseDePres(Player moi)
		{
			if (!moi.PositionMonde(out var mondeX, out var mondeY))
			{
				return false;
			}
			foreach (Entity value in Globals.Entities.Values)
			{
				if (value != null && value != moi && value.Type == EntityType.GlobalEntity && !(value is Critter) && value.Aggression == NpcAggression.Aggressive && !value.IsDying && !value.IsHidden && value.PositionMonde(out var mondeX2, out var mondeY2) && AMoinsDe(mondeX2 - mondeX, mondeY2 - mondeY, 160f))
				{
					return true;
				}
			}
			return false;
		}

		internal static bool VientDEtreFrappe()
		{
			return CoupRecuCompte(Timing.Global.Milliseconds, DernierCoupRecuA, Options.Instance.Combat.Heritage.EnCombatMs, EnVie(DernierAttaquantId));
		}

		internal static bool CoupRecuCompte(long maintenant, long recuA, int enCombatMs, bool attaquantEnVie)
		{
			if (attaquantEnVie && maintenant >= recuA)
			{
				return maintenant - recuA < enCombatMs;
			}
			return false;
		}

		internal static bool EnVie(Guid entiteId)
		{
			if (entiteId != Guid.Empty && Globals.Entities.TryGetValue(entiteId, out Entity value))
			{
				if (value != null && !value.IsDying)
				{
					return !value.IsHidden;
				}
				return false;
			}
			return false;
		}

		internal static bool AMoinsDe(float ecartX, float ecartY, float distance)
		{
			return ecartX * ecartX + ecartY * ecartY < distance * distance;
		}

		internal static bool IgnoreLesRessources(Player moi)
		{
			if (EnCombat(moi))
			{
				return !moi.RecolteDejaEnCours;
			}
			return false;
		}

		internal static Guid ReferenceDe(Player moi)
		{
			if (!HeritageRegles.Actif || !(ReferenceId != Guid.Empty) || !(moi.EnnemiDeLaSerieHeritage == ReferenceId))
			{
				return Guid.Empty;
			}
			return ReferenceId;
		}

		internal static bool ReferenceAPortee(float ecartX, float ecartY)
		{
			return AMoinsDe(ecartX, ecartY, 96f);
		}

		internal static T Reference<T>(T meilleure, bool meilleureDevant, T? reference) where T : class
		{
			if (meilleureDevant || reference == null)
			{
				return meilleure;
			}
			return reference;
		}

		internal static bool DansLArc(float ecartX, float ecartY, Direction regard)
		{
			(float X, float Y) tuple = CorpsACorps.Vecteur(regard);
			float item = tuple.X;
			float item2 = tuple.Y;
			float num = MathF.Sqrt(ecartX * ecartX + ecartY * ecartY);
			if (num < 0.001f || (item == 0f && item2 == 0f))
			{
				return true;
			}
			return (ecartX * item + ecartY * item2) / num >= CosinusDuDemiArc;
		}

		internal static Direction RegardVers(float ecartX, float ecartY, Direction regard)
		{
			if (DansLArc(ecartX, ecartY, regard))
			{
				return regard;
			}
			Direction direction = CorpsACorps.DirectionVers(ecartX, ecartY);
			if (direction != Direction.None)
			{
				return direction;
			}
			return regard;
		}

		internal static void CibleDesignee(Player joueur, Guid cibleId)
		{
			if (HeritageRegles.Actif && joueur == Globals.Me && cibleId != Guid.Empty && cibleId != ReferenceId)
			{
				ReferenceId = Guid.Empty;
			}
		}

		internal static float AllongePour(Player moi, Entity candidat, float ecartX, float ecartY)
		{
			if (!HeritageRegles.Actif)
			{
				return 0f;
			}
			float x = ((moi.DemandeHeritageContre(ecartX, ecartY).Coup == CoupHeritage.Montant) ? ((float)HeritageRegles.Table(CoupHeritage.Montant).AllongePx) : 0f);
			return MathF.Max(x, RecollePour(moi, candidat));
		}

		internal static float RecollePour(Player moi, Entity candidat)
		{
			int recollePx = Options.Instance.Combat.Heritage.Etats.RecollePx;
			if (recollePx <= 0 || !SerieEnCours(moi) || candidat.Id != ReferenceDe(moi) || moi.DirectionTenuePourLElan())
			{
				return 0f;
			}
			return recollePx;
		}
	}

	internal static class Corps
	{
		internal const int PenchePlafondPx = 16;

		internal const float PencheSensMin = 0.5f;

		internal const int PencheRetourMs = 120;

		internal const int PenchesMax = 64;

		internal const int TitubeBouffees = 8;

		internal const float TitubeBouffeePasPx = 5f;

		internal const float TitubeOnde = 3f;

		internal const float TitubeSecoussePx = 3f;

		internal const int TitubeSecousseMs = 170;

		internal const int EcrasesMax = 64;

		internal const int EcrasePlafondPourcent = 50;

		private static readonly Dictionary<Guid, (long Depart, long Fin)> Ecrases = new Dictionary<Guid, (long, long)>();

		private static readonly List<Guid> Finis = new List<Guid>();

		private static readonly Dictionary<Guid, (float Sens, long Fin)> Repousses = new Dictionary<Guid, (float, long)>();

		internal static int Nombre => Ecrases.Count;

		internal static int Penches => Repousses.Count;

		internal static FloatRect Pose(Entity entite, FloatRect rect)
		{
			if (Ecrases.Count != 0)
			{
				return Pose(entite.Id, rect, Timing.Global.Milliseconds);
			}
			return rect;
		}

		internal static FloatRect Pose(Guid id, FloatRect rect, long maintenant)
		{
			if (!Ecrases.TryGetValue(id, out (long, long) value))
			{
				return rect;
			}
			if (maintenant >= value.Item2 || maintenant < value.Item1)
			{
				Ecrases.Remove(id);
				return rect;
			}
			float num = Part(maintenant, value.Item1, value.Item2) * (float)Math.Clamp(Options.Instance.Combat.Heritage.Allure.EcrasePourcent, 0, 50) / 100f;
			float num2 = MathF.Round(rect.Height * num);
			float num3 = MathF.Round(rect.Width * num / 2f);
			return new FloatRect(rect.X - num3, rect.Y + num2, rect.Width + 2f * num3, rect.Height - num2);
		}

		internal static float Part(long maintenant, long depart, long fin)
		{
			if (fin > depart)
			{
				return Math.Clamp((float)(fin - maintenant) / (float)(fin - depart), 0f, 1f);
			}
			return 0f;
		}

		internal static float Penche(Entity entite)
		{
			if (Repousses.Count != 0)
			{
				return Penche(entite, Timing.Global.Milliseconds);
			}
			return 0f;
		}

		internal static float Penche(Entity entite, long maintenant)
		{
			if (!(entite is Player))
			{
				return Penche(entite.Id, maintenant);
			}
			return 0f;
		}

		internal static float Penche(Guid id, long maintenant)
		{
			if (!Repousses.TryGetValue(id, out (float, long) value))
			{
				return 0f;
			}
			long num = maintenant - value.Item2;
			if (num >= 120)
			{
				Repousses.Remove(id);
				return 0f;
			}
			HeritageAllureOptions allure = Options.Instance.Combat.Heritage.Allure;
			if (!allure.Actif)
			{
				return 0f;
			}
			float num2 = value.Item1 * (float)Math.Clamp(allure.PencheRepoussePx, 0, 16);
			if (num > 0)
			{
				return num2 * (1f - (float)num / 120f);
			}
			return num2;
		}

		internal static float SensDuPenche(float deX, float deY, float versX, float versY)
		{
			float num = versX - deX;
			float num2 = versY - deY;
			float num3 = MathF.Sqrt(num * num + num2 * num2);
			if (!(MathF.Abs(num) >= 0.5f * num3))
			{
				return 0f;
			}
			return MathF.Sign(num);
		}

		internal static void Pencher(Guid id, float sens, int dureeMs, long maintenant)
		{
			if (sens == 0f || float.IsNaN(sens) || dureeMs <= 0)
			{
				Redresser(id);
				return;
			}
			if (Repousses.Count >= 64 && !Repousses.ContainsKey(id))
			{
				FaireDeLaPlaceAuxPenches(maintenant);
			}
			Repousses[id] = ((sens < 0f) ? (-1f) : 1f, maintenant + dureeMs);
		}

		internal static void Redresser(Guid id)
		{
			if (Repousses.Count > 0)
			{
				Repousses.Remove(id);
			}
		}

		private static void FaireDeLaPlaceAuxPenches(long maintenant)
		{
			Finis.Clear();
			foreach (var (item, tuple2) in Repousses)
			{
				if (maintenant - tuple2.Item2 >= 120)
				{
					Finis.Add(item);
				}
			}
			if (Finis.Count == 0)
			{
				Repousses.Clear();
				return;
			}
			foreach (Guid fini in Finis)
			{
				Repousses.Remove(fini);
			}
			Finis.Clear();
		}

		internal static void SurEtat(Entity entite, EtatHeritage etat, int dureeMs, Guid par, long maintenant)
		{
			switch (etat)
			{
			case (EtatHeritage)255:
				Redresser(entite.Id);
				break;
			case EtatHeritage.Repousse:
				Pencher(entite.Id, (entite is Player) ? 0f : SensDepuis(par, entite), dureeMs, maintenant);
				break;
			case EtatHeritage.Titube:
				if (dureeMs > 0)
				{
					Tituber(entite, par);
				}
				break;
			case EtatHeritage.Plante:
				if (dureeMs > 0 && !(entite is Player))
				{
					Telegraphe.SurPlante(entite.Id);
				}
				break;
			}
		}

		private static float SensDepuis(Guid par, Entity entite)
		{
			if (!Globals.Entities.TryGetValue(par, out Entity value) || value == null)
			{
				return 0f;
			}
			System.Numerics.Vector2 origin = value.Origin;
			System.Numerics.Vector2 origin2 = entite.Origin;
			return SensDuPenche(origin.X, origin.Y, origin2.X, origin2.Y);
		}

		private static void Tituber(Entity entite, Guid par)
		{
			if (!Options.Instance.Combat.Heritage.Allure.Actif)
			{
				return;
			}
			Echanges.Poussiere(entite, 0, 3f);
			FloatRect worldPos = entite.WorldPos;
			if (worldPos.Width > 0f && worldPos.Height > 0f)
			{
				float num = worldPos.X + worldPos.Width / 2f;
				float bord = MathF.Max(entite.RayonDuCorps, worldPos.Width / 2f);
				for (int i = 0; i < 8; i++)
				{
					float num2 = EcartDeLaBouffee(i, bord);
					SprintDust.AddPuff(num + num2, worldPos.Y + worldPos.Height, (num2 < 0f) ? Direction.Right : Direction.Left);
				}
			}
			Guid? obj = Globals.Me?.Id;
			if (par == obj)
			{
				CombatFeedback.StartShake(3f, 170L);
			}
		}

		internal static float EcartDeLaBouffee(int rang, float bord)
		{
			return ((rang % 2 == 0) ? (-1f) : 1f) * (MathF.Max(0f, bord) + (float)(rang / 2) * 5f);
		}

		internal static void Ecraser(Entity entite, int ms)
		{
			Ecraser(entite.Id, ms, Timing.Global.Milliseconds);
		}

		internal static void Ecraser(Guid id, int ms, long maintenant)
		{
			if (ms > 0)
			{
				if (Ecrases.Count >= 64 && !Ecrases.ContainsKey(id))
				{
					FaireDeLaPlace(maintenant);
				}
				Ecrases[id] = (maintenant, maintenant + ms);
			}
		}

		private static void FaireDeLaPlace(long maintenant)
		{
			Finis.Clear();
			foreach (var (item, tuple2) in Ecrases)
			{
				if (maintenant >= tuple2.Item2 || maintenant < tuple2.Item1)
				{
					Finis.Add(item);
				}
			}
			if (Finis.Count == 0)
			{
				Ecrases.Clear();
				return;
			}
			foreach (Guid fini in Finis)
			{
				Ecrases.Remove(fini);
			}
			Finis.Clear();
		}

		internal static void Oublier()
		{
			Ecrases.Clear();
			Repousses.Clear();
			Finis.Clear();
		}
	}

	internal static class Coups
	{
		internal readonly record struct Jet(float Ouvert, float VMin, float VMax, float Gravite, int VieMs, bool Tirets = true);

		internal enum Repere : byte
		{
			Contact,
			Dos,
			PiedsDeLaCible,
			PiedsDeLAttaquant
		}

		internal enum Suivi : byte
		{
			Personne,
			LaCible,
			LAttaquant
		}

		internal readonly record struct Dessin(string Bande, Repere Repere, float LeLongDuCoup, float VersLeBas, CoucheDEffet Couche, Suivi Suit);

		private enum Genre : byte
		{
			Bouffee,
			Image
		}

		private struct PlusTard
		{
			public long A;

			public Genre Genre;

			public Guid Corps;

			public float EcartX;

			public float EcartY;

			public float Axe;

			public int Nombre;

			public Jet Jet;

			public int VieMs;

			public float Force;

			public float EcartMin;

			public Color? Couleur;
		}

		internal const float HauteurDuContact = 16f;

		internal const float SensMin = 0.5f;

		internal const float MarqueSousLesPieds = 1f;

		internal const float TalonsCourtsRetraitPx = 2f;

		internal const float TalonsLongsRetraitPx = 4f;

		internal const int TalonsLongsVieMs = 5000;

		internal const int TraitFinVieMs = 4000;

		internal const float FissuresSousLesPieds = 2f;

		internal const float EchelleDuCritique = 1.5f;

		internal const int BlancPlafondMs = 105;

		internal const int GrainsDuMontant = 4;

		internal const int BouffeesDeLEpaule = 3;

		internal const int GrainsParBouffee = 5;

		internal const int BouffeesEcartMs = 60;

		internal const float BouffeeDerrierePx = 6f;

		internal const float BouffeePenche = 0.5f;

		internal const int GrainsDeLElan = 5;

		internal const int ElanImageVieMs = 300;

		internal const float ElanImageForce = 0.85f;

		internal const float ElanImageEcartPx = 1f;

		internal const int ElanSecondeVieMs = 280;

		internal const float ElanSecondeForce = 0.7f;

		internal const float ElanSecondeEcartPx = 6f;

		internal const int ElanTraits = 4;

		internal const float ElanTraitsLargePx = 26f;

		internal const int ElanTraitsEnPlusMs = 140;

		internal const float ElanAMontrerPx = 6f;

		internal const int CoupFiniDepuisMs = 300;

		internal const int FaucheUneBandeMs = 200;

		internal const int SuitesMax = 32;

		internal const int SuitePerimeeMs = 500;

		internal const int PairesMax = 32;

		internal const int FauchesMax = 16;

		internal static readonly Color Or = new Color(255, 248, 208, 0);

		internal static readonly Color CremeDeLElan = new Color(255, 255, 233, 204);

		internal static readonly Jet JetDuDirect = new Jet(1f, 40f, 110f, 80f, 260);

		internal static readonly Jet JetDuCrochet = new Jet(1.2f, 40f, 110f, 80f, 260);

		internal static readonly Jet JetDeLEpaule = new Jet(0.8f, 30f, 100f, 120f, 300);

		internal static readonly Jet JetDuMontant = new Jet(1.6f, 60f, 150f, 300f, 380, Tirets: false);

		internal static readonly Jet JetDeLaFauche = new Jet(1.2f, 40f, 120f, 100f, 280);

		internal static readonly Jet JetDeLElan = new Jet(0.9f, 30f, 90f, 60f, 240);

		private static readonly Color[] PoussiereDeSable = new Color[4]
		{
			Particules.Sable[3],
			Particules.Sable[4],
			Particules.Sable[1],
			Particules.Sable[3]
		};

		private static readonly Color[] PoussiereDHerbe = new Color[4]
		{
			Particules.Herbe[3],
			Particules.Herbe[2],
			Particules.Herbe[1],
			Particules.Herbe[3]
		};

		private const long Jamais = -4611686018427387904L;

		private static readonly Dictionary<(Guid Attaquant, Guid Cible), (long Dernier, long Avant)> Portes = new Dictionary<(Guid, Guid), (long, long)>();

		private static readonly List<(Guid Attaquant, Guid Cible)> Perimees = new List<(Guid, Guid)>();

		private static readonly Dictionary<Guid, long> Fauches = new Dictionary<Guid, long>();

		private static readonly PlusTard[] Suites = new PlusTard[32];

		private static int _suites;

		internal static int Paires => Portes.Count;

		internal static int EnAttente => _suites;

		internal static bool DessinDe(CoupHeritage coup, out Dessin dessin)
		{
			Dessin dessin2;
			switch (coup)
			{
			case CoupHeritage.Direct:
				dessin2 = new Dessin("direct", Repere.Contact, -2f, 0f, CoucheDEffet.SousLesCimes, Suivi.Personne);
				break;
			case CoupHeritage.Crochet:
				dessin2 = new Dessin("crochet", Repere.Contact, -2f, -5f, CoucheDEffet.SousLesCimes, Suivi.Personne);
				break;
			case CoupHeritage.CoupDePied:
				dessin2 = new Dessin("pied", Repere.Contact, -4f, 0f, CoucheDEffet.SousLesCimes, Suivi.Personne);
				break;
			case CoupHeritage.CoupDEpaule:
				dessin2 = new Dessin("souffle", Repere.Dos, -2f, 2f, CoucheDEffet.DerriereUnCorps, Suivi.LaCible);
				break;
			case CoupHeritage.Montant:
				dessin2 = new Dessin("langues", Repere.PiedsDeLaCible, 0f, 3f, CoucheDEffet.DerriereUnCorps, Suivi.LaCible);
				break;
			case CoupHeritage.FaucheHoraire:
			case CoupHeritage.FaucheAntihoraire:
				dessin2 = new Dessin("fauche", Repere.PiedsDeLAttaquant, 0f, 2f, CoucheDEffet.Sol, Suivi.LAttaquant);
				break;
			default:
				dessin2 = default(Dessin);
				break;
			}
			dessin = dessin2;
			return dessin.Bande != null;
		}

		internal static CoucheDEffet CoucheSur(in Dessin dessin, CoupHeritage coup, bool lourd)
		{
			if (!lourd || coup != CoupHeritage.Montant)
			{
				return dessin.Couche;
			}
			return CoucheDEffet.DevantUnCorps;
		}

		internal static bool Enracinee(CoupHeritage coup, bool lourd)
		{
			if (coup == CoupHeritage.Montant)
			{
				return !lourd;
			}
			return false;
		}

		internal static (float X, float Y) DepartDesTalons(float piedsX, float solY, float reculX, float reculY, float rayon, bool lourd)
		{
			if (!lourd)
			{
				return (X: piedsX, Y: solY);
			}
			return (X: piedsX - reculX * rayon, Y: solY - reculY * rayon);
		}

		internal static float ElanAFaire(Entity attaquant, CoupHeritage coup)
		{
			Guid cibleDuDernierCoupHeritage = attaquant.CibleDuDernierCoupHeritage;
			if (cibleDuDernierCoupHeritage != Guid.Empty && Globals.Entities.TryGetValue(cibleDuDernierCoupHeritage, out Entity value) && value != null && attaquant.PositionMonde(out var mondeX, out var mondeY) && value.PositionMonde(out var mondeX2, out var mondeY2))
			{
				return ElanAFaire(mondeX2 - mondeX, mondeY2 - mondeY, coup, value.RayonDuCorps);
			}
			return float.NaN;
		}

		internal static float ElanAFaire(float distance, CoupHeritage coup, float rayonDeLaCible)
		{
			return distance - Elan.OuSArreteLElan(coup, rayonDeLaCible);
		}

		internal static float ElanAFaire(float ecartX, float ecartY, CoupHeritage coup, float rayonDeLaCible)
		{
			return MathF.Sqrt(ecartX * ecartX + ecartY * ecartY) - Elan.OuSArreteLElan(coup, rayonDeLaCible, ecartX, ecartY);
		}

		internal static bool LElanSeMontre(float elan)
		{
			return !(elan <= 6f);
		}

		internal static (float X, float Y) Ecart(float deX, float deY, float versX, float versY, Direction regard)
		{
			float num = versX - deX;
			float num2 = versY - deY;
			if (!(num * num + num2 * num2 >= 0.25f))
			{
				return CorpsACorps.Vecteur(regard);
			}
			return (X: num, Y: num2);
		}

		internal static (float X, float Y) Sens(float ecartX, float ecartY)
		{
			if (!(MathF.Abs(ecartX) >= MathF.Abs(ecartY)))
			{
				return (X: 0f, Y: (ecartY < 0f) ? (-1f) : 1f);
			}
			return (X: (ecartX < 0f) ? (-1f) : 1f, Y: 0f);
		}

		internal static (float X, float Y) Sens(Entity attaquant, Entity cible)
		{
			System.Numerics.Vector2 origin = attaquant.Origin;
			System.Numerics.Vector2 origin2 = cible.Origin;
			var (ecartX, ecartY) = Ecart(origin.X, origin.Y, origin2.X, origin2.Y, attaquant.DirectionFacing);
			return Sens(ecartX, ecartY);
		}

		internal static (float X, float Y) Contact(float piedsX, float piedsY, float hauteur, float rayon, float ux, float uy)
		{
			return (X: piedsX - ux * rayon, Y: piedsY - 16f - hauteur - uy * rayon);
		}

		internal static (float X, float Y) Place(in Dessin dessin, float cibleX, float cibleY, float hauteur, float rayon, float attaquantX, float attaquantY, float ux, float uy)
		{
			switch (dessin.Repere)
			{
			case Repere.PiedsDeLaCible:
				return (X: cibleX, Y: cibleY + dessin.VersLeBas);
			case Repere.PiedsDeLAttaquant:
				return (X: attaquantX, Y: attaquantY + dessin.VersLeBas);
			default:
			{
				float num = ((dessin.Repere == Repere.Dos) ? rayon : (0f - rayon)) + dessin.LeLongDuCoup;
				return (X: cibleX + ux * num, Y: cibleY - 16f - hauteur + uy * num + dessin.VersLeBas);
			}
			}
		}

		internal static Color[] Poussiere(float x, float y)
		{
			if (!Meteo.EstHerbe(x, y))
			{
				return PoussiereDeSable;
			}
			return PoussiereDHerbe;
		}

		private static bool DansLeFil(long maintenant, long coupA, int filMs)
		{
			if (maintenant >= coupA)
			{
				return maintenant - coupA < filMs;
			}
			return false;
		}

		internal static int Compter(Guid attaquant, Guid cible, long maintenant, int filMs)
		{
			(Guid, Guid) key = (attaquant, cible);
			int num = 1;
			long item = -4611686018427387904L;
			if (Portes.TryGetValue(key, out (long, long) value))
			{
				(item, _) = value;
				if (DansLeFil(maintenant, value.Item1, filMs))
				{
					num++;
					if (DansLeFil(maintenant, value.Item2, filMs))
					{
						num++;
					}
				}
			}
			else if (Portes.Count >= 32)
			{
				FaireDeLaPlace(maintenant, filMs);
			}
			Portes[key] = (maintenant, item);
			return num;
		}

		private static void FaireDeLaPlace(long maintenant, int filMs)
		{
			Perimees.Clear();
			foreach (var (item, tuple3) in Portes)
			{
				if (!DansLeFil(maintenant, tuple3.Item1, filMs))
				{
					Perimees.Add(item);
				}
			}
			if (Perimees.Count == 0)
			{
				Portes.Clear();
				return;
			}
			foreach (var perimee in Perimees)
			{
				Portes.Remove(perimee);
			}
			Perimees.Clear();
		}

		internal static int Grains(HeritageAllureOptions allure, int compte)
		{
			if (compte > 1)
			{
				if (compte != 2)
				{
					return allure.Grains3;
				}
				return allure.Grains2;
			}
			return allure.Grains1;
		}

		internal static int Blanc(HeritageAllureOptions allure)
		{
			return Math.Clamp(allure.BlancMs, 0, 105);
		}

		internal static CoupHeritage CoupDe(Entity entite, long maintenant)
		{
			CoupHeritage coupEnCours = entite.CoupEnCours;
			if (coupEnCours != CoupHeritage.Aucun)
			{
				return coupEnCours;
			}
			CoupHeritage dernierCoupHeritage = entite.DernierCoupHeritage;
			if (dernierCoupHeritage == CoupHeritage.Aucun || maintenant >= entite.FinDuDernierCoupHeritage + 300)
			{
				return CoupHeritage.Aucun;
			}
			return dernierCoupHeritage;
		}

		internal static bool PiedCritique(Entity cible, CoupHeritage coup, long maintenant)
		{
			HeritageOptions heritage = Options.Instance.Combat.Heritage;
			if (coup == CoupHeritage.CoupDePied && heritage.Allure.Actif && heritage.Etats.CritiqueSurFlanc)
			{
				return EtatsVus.Vif(cible.Id, EtatHeritage.Flanc, maintenant);
			}
			return false;
		}

		internal static bool ALaPorteeDeLaFauche(Entity attaquant, Entity cible)
		{
			System.Numerics.Vector2 origin = attaquant.Origin;
			System.Numerics.Vector2 origin2 = cible.Origin;
			float num = origin2.X - origin.X;
			float num2 = origin2.Y - origin.Y;
			float num3 = (float)Options.Instance.Combat.Heritage.Etats.FaucheRayonPx + cible.RayonDuCorps + (float)HeritageRegles.Table(CoupHeritage.FaucheHoraire).ReculPx;
			return num * num + num2 * num2 < num3 * num3;
		}

		private static bool LaFaucheSeMontre(Entity attaquant, Entity cible, long maintenant)
		{
			if (attaquant == Globals.Me)
			{
				return cible.Id == attaquant.CibleDuDernierCoupHeritage;
			}
			if (Fauches.TryGetValue(attaquant.Id, out var value) && maintenant >= value && maintenant - value < 200)
			{
				return false;
			}
			if (Fauches.Count >= 16)
			{
				Fauches.Clear();
			}
			Fauches[attaquant.Id] = maintenant;
			return true;
		}

		internal static void Jouer(Entity attaquant, Entity cible, CoupHeritage coup, int rang)
		{
			Jouer(attaquant, cible, coup, rang, Timing.Global.Milliseconds);
		}

		internal static bool Jouer(Entity attaquant, Entity cible, CoupHeritage coup, int rang, long maintenant)
		{
			HeritageOptions heritage = Options.Instance.Combat.Heritage;
			HeritageAllureOptions allure = heritage.Allure;
			if (!allure.Actif || !DessinDe(coup, out var dessin))
			{
				return false;
			}
			System.Numerics.Vector2 origin = attaquant.Origin;
			System.Numerics.Vector2 origin2 = cible.Origin;
			(float X, float Y) tuple = Ecart(origin.X, origin.Y, origin2.X, origin2.Y, attaquant.DirectionFacing);
			float item = tuple.X;
			float item2 = tuple.Y;
			(float X, float Y) tuple2 = Sens(item, item2);
			float item3 = tuple2.X;
			float item4 = tuple2.Y;
			float hauteur = Echanges.Hauteur(cible);
			float rayonDuCorps = cible.RayonDuCorps;
			bool flag = HeritageRegles.EstUneFauche(coup);
			bool flag2 = Echanges.EstLourd(cible);
			if (!flag || LaFaucheSeMontre(attaquant, cible, maintenant))
			{
				(float X, float Y) tuple3 = Place(in dessin, origin2.X, origin2.Y, hauteur, rayonDuCorps, origin.X, origin.Y, item3, item4);
				float item5 = tuple3.X;
				float item6 = tuple3.Y;
				bool flag3 = PiedCritique(cible, coup, maintenant);
				Guid corps = dessin.Suit switch
				{
					Suivi.LaCible => cible.Id, 
					Suivi.LAttaquant => attaquant.Id, 
					_ => Guid.Empty, 
				};
				if (Effets.Bande(dessin.Bande, item5, item6, item3, item4, CoucheSur(in dessin, coup, flag2), flag3 ? 1.5f : 1f, corps, 0, maintenant) && Enracinee(coup, flag2))
				{
					Effets.PlanterLaDerniere();
				}
				if (flag)
				{
					Mouvement.LaFaucheTouche(attaquant, item3, item4, maintenant);
				}
			}
			cible.HitWhiteUntil = maintenant + Blanc(allure);
			Corps.Ecraser(cible.Id, allure.EcraseMs, maintenant);
			if (Actif && Riposte.Tenace(cible.Id, maintenant))
			{
				return Riposte.LueurSurUnCorpsTenace(origin2.X, origin2.Y, hauteur, rayonDuCorps, item3, item4);
			}
			int nombre = Grains(allure, Compter(attaquant.Id, cible.Id, maintenant, heritage.Etats.FilMs));
			CoupHeritageOptions coupHeritageOptions = HeritageRegles.Table(coup);
			float num;
			float num2;
			float num3;
			if (!(cible is Player))
			{
				(num, num2, num3) = HeritageRegles.Recul(coup, item, item2, flag2);
			}
			else
			{
				num = 0f;
				num2 = 0f;
				num3 = 0f;
			}
			float num4 = origin2.Y + 1f;
			Color[] tons = Poussiere(origin2.X, origin2.Y);
			var (x, y) = DepartDesTalons(origin2.X, num4, num, num2, rayonDuCorps, flag2);
			switch (coup)
			{
			case CoupHeritage.Direct:
			{
				float num6 = num3 - 2f;
				if (num6 >= 1f)
				{
					Sol.Talons(x, y, num * num6, num2 * num6, coupHeritageOptions.ReculMs, allure.TalonsVieMs, maintenant);
				}
				Poudrer(origin2.X, origin2.Y, nombre, MathF.Atan2(item4, item3), in JetDuDirect, tons, maintenant);
				break;
			}
			case CoupHeritage.Crochet:
				if (num3 >= 1f)
				{
					Sol.Sillon(origin2.X, num4, num * num3, num2 * num3, coupHeritageOptions.ReculMs, 4000, fin: true, maintenant);
				}
				Poudrer(origin2.X, origin2.Y, nombre, (num3 > 0f) ? MathF.Atan2(num2, num) : MathF.Atan2(item4, item3), in JetDuCrochet, tons, maintenant);
				break;
			case CoupHeritage.CoupDEpaule:
			{
				float num7 = num3 - 4f;
				if (num7 >= 1f)
				{
					Sol.Talons(x, y, num * num7, num2 * num7, coupHeritageOptions.ReculMs, 5000, maintenant);
					Son.Talons(cible);
				}
				float axe = MathF.Atan2(0f - item4, 0f - item3) - 0.5f * ((item3 != 0f) ? item3 : 1f);
				for (int i = 0; i < 3; i++)
				{
					BouffeePlusTard(cible.Id, i * 60, (0f - item3) * 6f, (0f - item4) * 6f, 5, axe, in JetDeLEpaule, maintenant);
				}
				break;
			}
			case CoupHeritage.Montant:
				Sol.Fissures(origin2.X, origin2.Y + 2f, maintenant);
				Poudrer(origin2.X, origin2.Y, 4, -(float)Math.PI / 2f, in JetDuMontant, tons, maintenant);
				break;
			case CoupHeritage.FaucheHoraire:
			case CoupHeritage.FaucheAntihoraire:
			{
				float num5 = num3 - 4f;
				if (num5 >= 1f)
				{
					Sol.Sillon(origin2.X, num4, num * num5, num2 * num5, coupHeritageOptions.ReculMs, 4000, fin: true, maintenant);
				}
				Poudrer(origin2.X, origin2.Y, nombre, MathF.Atan2(item2, item), in JetDeLaFauche, tons, maintenant);
				break;
			}
			}
			var (x2, y2) = Contact(origin2.X, origin2.Y, hauteur, rayonDuCorps, item3, item4);
			KiLight.AddFlash(x2, y2, Or, small: true);
			return true;
		}

		private static void Poudrer(float x, float y, int nombre, float axe, in Jet jet, Color[] tons, long maintenant)
		{
			Particules.Lancer(x, y, nombre, axe, jet.Ouvert, jet.VMin, jet.VMax, jet.Gravite, jet.VieMs, tons, jet.Tirets, float.NaN, 0, null, Globals.Random, maintenant);
		}

		internal static void Partir(Entity attaquant, CoupHeritage coup)
		{
			Partir(attaquant, coup, Timing.Global.Milliseconds);
		}

		internal static bool Partir(Entity attaquant, CoupHeritage coup, long maintenant)
		{
			if (coup != CoupHeritage.CoupDEpaule || !Options.Instance.Combat.Heritage.Allure.Actif)
			{
				return false;
			}
			System.Numerics.Vector2 origin = attaquant.Origin;
			float x = origin.X;
			float y = origin.Y;
			float versX = x;
			Guid cibleDuDernierCoupHeritage = attaquant.CibleDuDernierCoupHeritage;
			if (cibleDuDernierCoupHeritage != Guid.Empty && Globals.Entities.TryGetValue(cibleDuDernierCoupHeritage, out Entity value) && value != null)
			{
				System.Numerics.Vector2 origin2 = value.Origin;
				float x2 = origin2.X;
				y = origin2.Y;
				versX = x2;
			}
			(float X, float Y) tuple = Ecart(origin.X, origin.Y, versX, y, attaquant.DirectionFacing);
			float item = tuple.X;
			float item2 = tuple.Y;
			(float X, float Y) tuple2 = Sens(item, item2);
			float item3 = tuple2.X;
			float item4 = tuple2.Y;
			int num = HeureDeLImpact(coup, attaquant.AvanceDuCoupEnCours);
			if (LElanSeMontre(ElanAFaire(attaquant, coup)))
			{
				Fantomes.Laisser(attaquant.Id, 300, 0.85f, CremeDeLElan, 1f, maintenant);
				ImagePlusTard(attaquant.Id, num / 2, 280, 0.7f, CremeDeLElan, 6f, maintenant);
				Traits.Lancer(attaquant.Id, item3, item4, num + 140, 4, 26f, maintenant);
			}
			Poudrer(origin.X, origin.Y, 5, MathF.Atan2(0f - item4, 0f - item3), in JetDeLElan, Poussiere(origin.X, origin.Y), maintenant);
			return true;
		}

		internal static void BouffeePlusTard(Guid corps, int retardMs, float ecartX, float ecartY, int nombre, float axe, in Jet jet, long maintenant)
		{
			PlusTard suite = new PlusTard
			{
				A = maintenant + Math.Max(0, retardMs),
				Genre = Genre.Bouffee,
				Corps = corps,
				EcartX = ecartX,
				EcartY = ecartY,
				Axe = axe,
				Nombre = nombre,
				Jet = jet
			};
			Plus(in suite, maintenant);
		}

		internal static void ImagePlusTard(Guid corps, int retardMs, int vieMs, float force, Color couleur, float ecartMin, long maintenant)
		{
			PlusTard suite = new PlusTard
			{
				A = maintenant + Math.Max(0, retardMs),
				Genre = Genre.Image,
				Corps = corps,
				VieMs = vieMs,
				Force = force,
				Couleur = couleur,
				EcartMin = ecartMin
			};
			Plus(in suite, maintenant);
		}

		private static void Plus(in PlusTard suite, long maintenant)
		{
			if (suite.A <= maintenant)
			{
				Faire(in suite, maintenant);
				return;
			}
			if (_suites == 32)
			{
				Array.Copy(Suites, 1, Suites, 0, 31);
				_suites--;
			}
			Suites[_suites++] = suite;
		}

		private static void Faire(in PlusTard suite, long maintenant)
		{
			switch (suite.Genre)
			{
			case Genre.Bouffee:
			{
				if (Globals.Entities.TryGetValue(suite.Corps, out Entity value) && value != null)
				{
					System.Numerics.Vector2 origin = value.Origin;
					float x = origin.X + suite.EcartX;
					float y = origin.Y + suite.EcartY;
					Poudrer(x, y, suite.Nombre, suite.Axe, in suite.Jet, Poussiere(x, y), maintenant);
				}
				break;
			}
			case Genre.Image:
			{
				Color couleur = suite.Couleur;
				if ((object)couleur != null)
				{
					Fantomes.Laisser(suite.Corps, suite.VieMs, suite.Force, couleur, suite.EcartMin, maintenant);
				}
				break;
			}
			}
		}

		internal static void Suivre(long maintenant)
		{
			if (_suites == 0)
			{
				return;
			}
			int num = 0;
			for (int i = 0; i < _suites; i++)
			{
				ref PlusTard reference = ref Suites[i];
				if (maintenant < reference.A)
				{
					if (num != i)
					{
						Suites[num] = reference;
					}
					num++;
				}
				else if (maintenant - reference.A <= 500)
				{
					Faire(in reference, maintenant);
				}
			}
			Array.Clear(Suites, num, _suites - num);
			_suites = num;
		}

		internal static void OublierLesSuites()
		{
			Array.Clear(Suites, 0, _suites);
			_suites = 0;
		}

		internal static void Oublier()
		{
			OublierLesSuites();
			Portes.Clear();
			Perimees.Clear();
			Fauches.Clear();
		}
	}

	internal static class Decor
	{
		internal enum ApresLeChoc : byte
		{
			Rien,
			Cime,
			GravatsAuPied,
			GravatsQuiTombent
		}

		internal readonly record struct LigneDeChoc(string? Bande, float Echelle, int Eclats, Color[]? Tons, int ArretMs, float SecoussePx, ApresLeChoc Apres, int Gravats);

		internal readonly record struct ChocDu(Entity Entite, Dash? Glissade, long NoteA, MatiereDArret Matiere, float SensX, float SensY, long Valeur, Guid AuteurId)
		{
			internal long Heure => Glissade?.EndsAt ?? NoteA;

			internal bool Gele
			{
				get
				{
					if (Glissade == null)
					{
						return Matiere != MatiereDArret.Corps;
					}
					return true;
				}
			}

			[CompilerGenerated]
			private bool PrintMembers(StringBuilder builder)
			{
				builder.Append("Entite = ");
				builder.Append(Entite);
				builder.Append(", Glissade = ");
				builder.Append(Glissade);
				builder.Append(", NoteA = ");
				builder.Append(NoteA.ToString());
				builder.Append(", Matiere = ");
				builder.Append(Matiere.ToString());
				builder.Append(", SensX = ");
				builder.Append(SensX.ToString());
				builder.Append(", SensY = ");
				builder.Append(SensY.ToString());
				builder.Append(", Valeur = ");
				builder.Append(Valeur.ToString());
				builder.Append(", AuteurId = ");
				builder.Append(AuteurId.ToString());
				return true;
			}
		}

		private enum GenreDeSuite : byte
		{
			Feuilles,
			Gravats
		}

		private sealed class Suite
		{
			public GenreDeSuite Genre;

			public long Debut;

			public int PasMs;

			public int Lots;

			public int Faits;

			public int ParLot;

			public int EnPlus;

			public float X;

			public float Y;

			public float SolY;

			public int ResteMs;
		}

		internal static class Son
		{
			private static readonly string[] Arbre = new string[2] { "heritage_arbre_1.wav", "heritage_arbre_2.wav" };

			private static readonly string[] Rocher = new string[2] { "heritage_rocher_1.wav", "heritage_rocher_2.wav" };

			private static readonly string[] Falaise = new string[1] { "heritage_falaise_1.wav" };

			private static readonly string[] Aucun = Array.Empty<string>();

			private const int DistanceDEcoute = 12;

			internal const int VoixMax = 6;

			private static readonly Random Hasard = new Random();

			internal static IReadOnlyList<string> Fichiers(MatiereDArret matiere)
			{
				return matiere switch
				{
					MatiereDArret.Arbre => Arbre, 
					MatiereDArret.Rocher => Rocher, 
					MatiereDArret.Falaise => Falaise, 
					_ => Aucun, 
				};
			}

			internal static string? Fichier(MatiereDArret matiere, double tirage)
			{
				IReadOnlyList<string> readOnlyList = Fichiers(matiere);
				if (readOnlyList.Count != 0)
				{
					return readOnlyList[Math.Clamp((int)(Math.Clamp(tirage, 0.0, 1.0) * (double)readOnlyList.Count), 0, readOnlyList.Count - 1)];
				}
				return null;
			}

			internal static int Voix()
			{
				return Math.Clamp(Options.Instance.Combat.Heritage.DecorVu.SonVoix, 0, 6);
			}

			internal static int Jouer(Entity autour, MatiereDArret matiere)
			{
				int num = Voix();
				if (num != 0)
				{
					string text = Fichier(matiere, Hasard.NextDouble());
					if (text != null)
					{
						int num2 = 0;
						for (int i = 0; i < num; i++)
						{
							if (Audio.AddMapSound(text, autour.X, autour.Y, autour.MapId, loop: false, 0, 12, autour) != null)
							{
								num2++;
							}
						}
						return num2;
					}
				}
				return 0;
			}
		}

		internal const int DusMax = 32;

		internal const int PerimeMs = 2000;

		internal const int MortEnVolToleranceMs = 80;

		private const float SensMinPx = 0.5f;

		private const float HauteurDuChocPx = 16f;

		internal const int BlancMs = 130;

		internal const int BlancAffichableMs = 130;

		internal const int EcraseMs = 160;

		private static readonly Color Lueur = Ton(16765514);

		internal const int ArretMaxMs = 400;

		internal const float SecousseMaxPx = 12f;

		internal const int SecousseMaxMs = 1000;

		private const float SecoussePetitPx = 3f;

		private const string BandeEcorce = "mur_ecorce";

		private const string BandePierre = "mur_pierre";

		private const string BandeRoche = "mur_roche";

		private const float EchelleFalaise = 1.5f;

		private const int EclatsArbre = 14;

		private const int EclatsRocher = 14;

		private const int EclatsFalaise = 22;

		private const int EclatsPetit = 8;

		private const int EclatsCorps = 6;

		private const int GravatsRocher = 14;

		private const int GravatsFalaise = 22;

		private static readonly Color[] TonsDeLEcorce = new Color[4]
		{
			Ton(8209445),
			Ton(11306867),
			Ton(5779482),
			Ton(13877173)
		};

		private static readonly Color[] TonsDeLaPierre = new Color[4]
		{
			Ton(9464422),
			Ton(12363427),
			Ton(6441029),
			Ton(14932697)
		};

		private static readonly Color[] TonsDeLaRoche = new Color[4]
		{
			Ton(9914665),
			Ton(12553850),
			Ton(5975065),
			Ton(14599611)
		};

		private static readonly Color[] TonsDuSable = new Color[4]
		{
			Ton(14266525),
			Ton(15587792),
			Ton(8014903),
			Ton(14266525)
		};

		private static readonly Color[] TonsDeLHerbe = new Color[4]
		{
			Ton(10589021),
			Ton(7889944),
			Ton(5522961),
			Ton(10589021)
		};

		internal static readonly Color[] TonsDesFeuilles = new Color[4]
		{
			Ton(13164656),
			Ton(15263904),
			Ton(10213450),
			Ton(15784032)
		};

		private const float EclatsOuvert = 2.6f;

		private const float EclatsVMin = 50f;

		private const float EclatsVMax = 170f;

		private const float EclatsGravite = 420f;

		private const int EclatsVolMs = 700;

		private const float EclatsSolPx = 3f;

		private const float GravatsReculPx = 8f;

		private const float GravatsHautPx = 6f;

		private const float GravatsOuvert = 2.2f;

		private const float GravatsVMin = 20f;

		private const float GravatsVMax = 70f;

		private const float GravatsGravite = 480f;

		private const int GravatsVolMs = 500;

		private const float GravatsSolPx = 5f;

		private const int Volees = 3;

		private const int VoleeDebutMs = 120;

		private const int VoleePasMs = 110;

		private const float VoleeLargePx = 50f;

		private const float VoleeHautPx = 44f;

		private const float VoleeEpaisPx = 30f;

		private const float VoleeOuvert = 0.8f;

		private const float VoleeVMin = 10f;

		private const float VoleeVMax = 50f;

		private const float VoleeGravite = 480f;

		private const int VoleeVolMs = 700;

		private const float VoleeSolPx = 6f;

		internal const int FeuillesMax = 48;

		private const float FeuillesParForceDuVent = 24f;

		internal const float CimeBalancePx = 6f;

		internal const float BalanceParForceDuVentPx = 3.45f;

		private const float SecouerAuDelaPx = 8f;

		private const float SecouerHautPx = 8f;

		private const float CimeAuDelaPx = 16f;

		private const float CimeHautPx = 56f;

		private const float CimeLargePx = 48f;

		private const float CimeEpaisPx = 28f;

		private const float FeuilleOuvert = 2f;

		private const float FeuilleVMin = 10f;

		private const float FeuilleVMax = 24f;

		private const float FeuilleGravite = 110f;

		private const int FeuilleVolMs = 1700;

		private const float FeuilleSolPx = 2f;

		private const float FeuilleSolLargePx = 14f;

		private const int FeuilleResteMs = 4000;

		internal const int SuitesMax = 16;

		private const float CorpsVoisinPx = 12f;

		private static readonly Random Hasard = new Random();

		private static readonly List<ChocDu> ChocsDus = new List<ChocDu>();

		private static readonly List<Suite> Suites = new List<Suite>();

		internal static IReadOnlyList<ChocDu> Dus => ChocsDus;

		internal static int SuitesEnCours => Suites.Count;

		internal static void Oublier()
		{
			ChocsDus.Clear();
			Suites.Clear();
		}

		internal static bool SeVoit(MatiereDArret matiere)
		{
			if (matiere - 1 <= MatiereDArret.Corps)
			{
				return true;
			}
			return false;
		}

		internal static bool SeJoue(MatiereDArret matiere, long valeur)
		{
			if (SeVoit(matiere))
			{
				if (valeur <= 0)
				{
					return HeritageRegles.PartDuDecor(matiere) <= 0;
				}
				return true;
			}
			return false;
		}

		internal static LigneDeChoc Ligne(MatiereDArret matiere)
		{
			HeritageDecorVuOptions decorVu = Options.Instance.Combat.Heritage.DecorVu;
			return matiere switch
			{
				MatiereDArret.Arbre => new LigneDeChoc("mur_ecorce", 1f, 14, TonsDeLEcorce, Arret(decorVu.ArretArbreMs), Secousse(decorVu.SecousseArbreDixiemes), ApresLeChoc.Cime, 0), 
				MatiereDArret.Rocher => new LigneDeChoc("mur_pierre", 1f, 14, TonsDeLaPierre, Arret(decorVu.ArretRocherMs), Secousse(decorVu.SecousseRocherDixiemes), ApresLeChoc.GravatsAuPied, 14), 
				MatiereDArret.Falaise => new LigneDeChoc("mur_roche", 1.5f, 22, TonsDeLaRoche, Arret(decorVu.ArretFalaiseMs), Secousse(decorVu.SecousseFalaiseDixiemes), ApresLeChoc.GravatsQuiTombent, 22), 
				MatiereDArret.Petit => new LigneDeChoc("mur_pierre", 1f, 8, null, Arret(decorVu.ArretPetitMs), 3f, ApresLeChoc.Rien, 0), 
				MatiereDArret.Corps => new LigneDeChoc(null, 1f, 6, null, Arret(decorVu.ArretPetitMs), 3f, ApresLeChoc.Rien, 0), 
				_ => default(LigneDeChoc), 
			};
		}

		private static int Arret(int ms)
		{
			return Math.Clamp(ms, 0, 400);
		}

		private static float Secousse(int dixiemes)
		{
			return Math.Clamp((float)dixiemes / 10f, 0f, 12f);
		}

		internal static Color[] TonsDuSol(float x, float y)
		{
			if (!Meteo.EstHerbe(x, y))
			{
				return TonsDuSable;
			}
			return TonsDeLHerbe;
		}

		private static Color Ton(int rvb)
		{
			return new Color(255, (rvb >> 16) & 0xFF, (rvb >> 8) & 0xFF, rvb & 0xFF);
		}

		internal static void SurGlissade(Entity entite, Dash glissade)
		{
			MatiereDArret arretHeritage = glissade.ArretHeritage;
			if (SeJoue(arretHeritage, glissade.DegatsDArretHeritage))
			{
				var (num, num2) = glissade.StartOffset;
				var (num3, num4) = glissade.EndOffset;
				if (!Unitaire(num3 - num, num4 - num2, out var sensX, out var sensY))
				{
					(sensX, sensY) = SensDepuis(glissade.AuteurId, entite);
				}
				Noter(new ChocDu(entite, glissade, glissade.EndsAt, arretHeritage, sensX, sensY, glissade.DegatsDArretHeritage, glissade.AuteurId));
			}
		}

		internal static void SurArret(Entity entite, EchangePacket paquet)
		{
			MatiereDArret index = (MatiereDArret)paquet.Index;
			if (SeJoue(index, paquet.Valeur))
			{
				var (sensX, sensY) = SensDepuis(paquet.AutreId, entite);
				Noter(new ChocDu(entite, null, Timing.Global.Milliseconds, index, sensX, sensY, paquet.Valeur, paquet.AutreId));
			}
		}

		private static void Noter(ChocDu choc)
		{
			if (ChocsDus.Count >= 32)
			{
				ChocsDus.RemoveAt(0);
			}
			ChocsDus.Add(choc);
		}

		internal static int JouerLesChocsDus()
		{
			return JouerLesChocsDus(Timing.Global.Milliseconds);
		}

		internal static int JouerLesChocsDus(long maintenant)
		{
			int num = 0;
			int num2 = 0;
			while (num2 < ChocsDus.Count)
			{
				ChocDu choc = ChocsDus[num2];
				if (EstPerdu(choc))
				{
					ChocsDus.RemoveAt(num2);
					continue;
				}
				long heure = choc.Heure;
				if (maintenant < heure && (choc.Glissade == null || choc.Entite.Dashing == choc.Glissade))
				{
					num2++;
					continue;
				}
				ChocsDus.RemoveAt(num2);
				if (maintenant - heure <= 2000)
				{
					Jouer(choc.Entite, choc.Matiere, choc.SensX, choc.SensY, choc.Valeur, choc.AuteurId, maintenant, choc.Gele);
					num++;
				}
			}
			SuivreLesSuites(maintenant);
			return num;
		}

		private static bool EstPerdu(ChocDu choc)
		{
			Entity entite = choc.Entite;
			if (entite.IsDisposed || !Globals.Entities.TryGetValue(entite.Id, out Entity value) || value != entite)
			{
				return true;
			}
			if (entite.IsDying && choc.Glissade != null)
			{
				return entite.DyingAt < choc.Glissade.FinAuServeur - 80;
			}
			return false;
		}

		internal static void Jouer(Entity entite, MatiereDArret matiere, float sensX, float sensY, long valeur, Guid auteurId, long maintenant, bool gele = true)
		{
			if (!SeVoit(matiere) || !Pieds(entite, out var x, out var y))
			{
				return;
			}
			LigneDeChoc ligneDeChoc = Ligne(matiere);
			HeritageDecorVuOptions decorVu = Options.Instance.Combat.Heritage.DecorVu;
			float rayonDuCorps = entite.RayonDuCorps;
			float num = x + sensX * rayonDuCorps;
			float y2 = y - 16f + sensY * rayonDuCorps;
			if (ligneDeChoc.Bande != null)
			{
				Effets.Bande(ligneDeChoc.Bande, num, y2, sensX, sensY, CoucheDEffet.SousLesCimes, ligneDeChoc.Echelle);
			}
			Color[] tons = ligneDeChoc.Tons ?? TonsDuSol(x, y);
			float axe = MathF.Atan2(0f - sensY, 0f - sensX);
			Effets.Grains(num, y2, ligneDeChoc.Eclats, axe, 2.6f, 50f, 170f, 420f, 700, tons, tirets: false, y + 3f, decorVu.EclatsVieMs);
			switch (ligneDeChoc.Apres)
			{
			case ApresLeChoc.Cime:
				SecouerLaCime(num, x, y, sensX, sensY, rayonDuCorps, maintenant);
				break;
			case ApresLeChoc.GravatsAuPied:
				Effets.Grains(num - sensX * 8f, y - 6f, ligneDeChoc.Gravats, axe, 2.2f, 20f, 70f, 480f, 500, tons, tirets: false, y + 5f, decorVu.EclatsVieMs);
				break;
			case ApresLeChoc.GravatsQuiTombent:
				Ajouter(new Suite
				{
					Genre = GenreDeSuite.Gravats,
					Debut = maintenant + 120,
					PasMs = 110,
					Lots = 3,
					ParLot = ligneDeChoc.Gravats / 3,
					EnPlus = ligneDeChoc.Gravats % 3,
					X = x,
					Y = y,
					SolY = y,
					ResteMs = decorVu.EclatsVieMs
				});
				break;
			}
			if (valeur > 0)
			{
				Chiffres.Decor(entite, valeur, matiere, sensX, sensY);
			}
			Blanchir(entite, maintenant);
			Corps.Ecraser(entite, 160);
			if (matiere == MatiereDArret.Corps)
			{
				Entity entity = LAutreCorps(entite, x, y, sensX, sensY);
				if (entity != null)
				{
					Blanchir(entity, maintenant);
				}
			}
			KiLight.AddFlash(num, y2, Lueur, small: true);
			Player me = Globals.Me;
			if (me != null && auteurId == me.Id)
			{
				if (gele && ligneDeChoc.ArretMs > 0)
				{
					CombatFeedback.HitStop(ligneDeChoc.ArretMs);
				}
				int num2 = Math.Min(decorVu.SecousseMs, 1000);
				if (ligneDeChoc.SecoussePx > 0f && num2 > 0)
				{
					CombatFeedback.StartShake(ligneDeChoc.SecoussePx, num2, sensX, sensY);
				}
			}
			Son.Jouer(entite, matiere);
		}

		private static void Blanchir(Entity entite, long maintenant)
		{
			entite.HitWhiteUntil = maintenant + Math.Min(130, 130);
		}

		internal static float ForceDesFeuilles(int feuilles)
		{
			return ((float)feuilles + 0.5f) / 24f;
		}

		internal static float PousseeDeLaCime(float force)
		{
			if (!(force > 0f) || !float.IsFinite(force))
			{
				return 1f;
			}
			return 6f / (3.45f * force);
		}

		private static void SecouerLaCime(float ox, float piedsX, float piedsY, float sensX, float sensY, float rayon, long maintenant)
		{
			HeritageDecorVuOptions decorVu = Options.Instance.Combat.Heritage.DecorVu;
			int num = Math.Min(decorVu.Feuilles, 48);
			if (num > 0)
			{
				float y = piedsY - 8f + sensY * (rayon + 8f);
				float force = ForceDesFeuilles(num);
				if (!Vent.Secouer(ox, y, force, PousseeDeLaCime(force)))
				{
					Ajouter(new Suite
					{
						Genre = GenreDeSuite.Feuilles,
						Debut = maintenant,
						PasMs = decorVu.CimeMs / num,
						Lots = num,
						ParLot = 1,
						X = piedsX + sensX * (rayon + 16f),
						Y = piedsY - 56f + sensY * (rayon + 16f),
						SolY = piedsY,
						ResteMs = 4000
					});
				}
			}
		}

		internal static Entity? LAutreCorps(Entity entite, float piedsX, float piedsY, float sensX, float sensY)
		{
			Entity result = null;
			float num = float.MaxValue;
			foreach (Entity value in Globals.Entities.Values)
			{
				bool flag = value == null || value == entite || value.IsDisposed || value.IsHidden || value.IsDying;
				bool flag2 = flag;
				if (!flag2)
				{
					EntityType type = value.Type;
					bool flag3 = (uint)type <= 1u;
					flag2 = !flag3;
				}
				if (!flag2 && Pieds(value, out var x, out var y))
				{
					float num2 = x - piedsX;
					float num3 = y - piedsY;
					float num4 = MathF.Sqrt(num2 * num2 + num3 * num3);
					if (!(num4 > entite.RayonDuCorps + value.RayonDuCorps + 12f) && !(num2 * sensX + num3 * sensY <= 0f) && !(num4 >= num))
					{
						num = num4;
						result = value;
					}
				}
			}
			return result;
		}

		private static void Ajouter(Suite suite)
		{
			if (Suites.Count >= 16)
			{
				Suites.RemoveAt(0);
			}
			Suites.Add(suite);
		}

		internal static int SuivreLesSuites(long maintenant)
		{
			int num = 0;
			int num2 = 0;
			while (num2 < Suites.Count)
			{
				Suite suite = Suites[num2];
				while (suite.Faits < suite.Lots)
				{
					long num3 = suite.Debut + (long)suite.Faits * (long)suite.PasMs;
					if (maintenant < num3)
					{
						break;
					}
					if (maintenant - num3 <= 2000)
					{
						Lacher(suite);
						num++;
					}
					suite.Faits++;
				}
				if (suite.Faits >= suite.Lots)
				{
					Suites.RemoveAt(num2);
				}
				else
				{
					num2++;
				}
			}
			return num;
		}

		private static void Lacher(Suite suite)
		{
			switch (suite.Genre)
			{
			case GenreDeSuite.Feuilles:
				Effets.Grains(suite.X + (Tirage() - 0.5f) * 48f, suite.Y - Tirage() * 28f, suite.ParLot, (float)Math.PI / 2f, 2f, 10f, 24f, 110f, 1700, TonsDesFeuilles, tirets: true, suite.SolY + 2f + Tirage() * 14f, suite.ResteMs);
				break;
			case GenreDeSuite.Gravats:
				Effets.Grains(suite.X + (Tirage() - 0.5f) * 50f, suite.Y - 44f - Tirage() * 30f, suite.ParLot + ((suite.Faits == 0) ? suite.EnPlus : 0), (float)Math.PI / 2f, 0.8f, 10f, 50f, 480f, 700, TonsDeLaRoche, tirets: false, suite.SolY + 6f, suite.ResteMs);
				break;
			}
		}

		private static float Tirage()
		{
			return (float)Hasard.NextDouble();
		}

		internal static bool Pieds(Entity entite, out float x, out float y)
		{
			if (!MapInstance.TryGet(entite.MapId, out MapInstance instance) || instance == null)
			{
				x = 0f;
				y = 0f;
				return false;
			}
			MapOptions map = Options.Instance.Map;
			x = (float)(instance.X + entite.X * map.TileWidth) + entite.OffsetX + (float)map.TileWidth / 2f;
			y = (float)(instance.Y + entite.Y * map.TileHeight) + entite.OffsetY + (float)map.TileHeight;
			return true;
		}

		private static bool Unitaire(float x, float y, out float sensX, out float sensY)
		{
			float num = MathF.Sqrt(x * x + y * y);
			if (!(num >= 0.5f))
			{
				sensX = 0f;
				sensY = 0f;
				return false;
			}
			sensX = x / num;
			sensY = y / num;
			return true;
		}

		private static (float X, float Y) SensDepuis(Guid auteurId, Entity entite)
		{
			if (Globals.Entities.TryGetValue(auteurId, out Entity value) && value != null && value != entite && Pieds(value, out var x, out var y) && Pieds(entite, out var x2, out var y2) && Unitaire(x2 - x, y2 - y, out var sensX, out var sensY))
			{
				return (X: sensX, Y: sensY);
			}
			var (num, num2) = CorpsACorps.Vecteur(entite.DirectionFacing);
			if (!Unitaire(0f - num, 0f - num2, out var sensX2, out var sensY2))
			{
				return (X: 0f, Y: 0f);
			}
			return (X: sensX2, Y: sensY2);
		}

		internal static float Progression(float t, byte part)
		{
			return HeritageRegles.Progression(t, (float)(int)part / 255f);
		}
	}

	internal static class Devant
	{
		internal const float Ecart = 0.5f;

		internal const int ApresLeCoupMs = 60;

		internal const int RangeesAuPlus = 2;

		private static Entity? _qui;

		private static Entity? _cible;

		internal static Entity? Cible => _cible;

		internal static float Pieds(float siens, float ceuxDeLaCible)
		{
			return MathF.Max(siens, ceuxDeLaCible + 0.5f);
		}

		internal static int Rangee(int sienne, int rangeesPlusBas)
		{
			return sienne + Math.Max(0, rangeesPlusBas);
		}

		internal static bool Passe(bool reglage, bool coupRecent, bool cibleLourde, bool enVol, int? rangeesPlusBas)
		{
			if (reglage && coupRecent && !cibleLourde && !enVol && rangeesPlusBas.HasValue)
			{
				int valueOrDefault = rangeesPlusBas.GetValueOrDefault();
				return valueOrDefault <= 2;
			}
			return false;
		}

		internal static int Ranger(Entity entite, int rangee, long maintenant)
		{
			if (entite != Globals.Me)
			{
				return rangee;
			}
			_qui = entite;
			_cible = null;
			Guid cibleId;
			bool flag = entite.CoupHeritageRecent(maintenant, 60, out cibleId);
			if (!flag || !Globals.Entities.TryGetValue(cibleId, out Entity value) || value == null || value == entite || value.IsDisposed)
			{
				return rangee;
			}
			int? rangeesPlusBas = entite.RangeesPlusBasPourLeTri(value, rangee);
			if (Passe(Options.Instance.Combat.Heritage.DevantSaCible, flag, Echanges.EstLourd(value), Flight.IsAirborne(entite) || Flight.IsAirborne(value), rangeesPlusBas) && rangeesPlusBas.HasValue)
			{
				int valueOrDefault = rangeesPlusBas.GetValueOrDefault();
				_cible = value;
				return Rangee(rangee, valueOrDefault);
			}
			return rangee;
		}

		internal static float Pieds(Entity entite)
		{
			if (entite == _qui)
			{
				Entity cible = _cible;
				if (cible != null)
				{
					return Pieds(entite.Origin.Y, cible.Origin.Y);
				}
			}
			return entite.Origin.Y;
		}

		internal static void Oublier()
		{
			_qui = null;
			_cible = null;
		}
	}

	internal enum CoucheDEffet : byte
	{
		Sol,
		DerriereUnCorps,
		DevantUnCorps,
		SousLesCimes,
		ParDessus
	}

	internal static class Effets
	{
		private struct Joue
		{
			public Bandes.Bande Bande;

			public IGameTexture Planche;

			public float X;

			public float Y;

			public float SensX;

			public float SensY;

			public CoucheDEffet Couche;

			public float Echelle;

			public Color? Teinte;

			public Guid Suivi;

			public bool Suit;

			public float EcartX;

			public float EcartY;

			public long Depart;
		}

		internal const int EffetsMax = 64;

		internal const string Etape = "heritage : effets";

		private const string EtapeDuSol = "sol : ki, sang, vol, sable";

		private const string EtapeDesCorps = "monde : personnages";

		private const string EtapeDuDessus = "monde : noms, barres et textes";

		private static readonly Color Blanc = new Color(255, 255, 255, 255);

		private static readonly Joue[] Joues = new Joue[64];

		private static int _nombre;

		private static int _accroches;

		internal static Action<IGameTexture?, FloatRect, FloatRect, Color, float>? Temoin;

		internal static int Nombre => _nombre;

		internal static int Accroches => _accroches;

		private static bool AllureActive => Options.Instance.Combat.Heritage.Allure.Actif;

		private static bool RienAMontrer
		{
			get
			{
				if (_nombre == 0 && Particules.Nombre == 0 && Sol.Nombre == 0 && Fantomes.Laissees == 0 && Traits.Nombre == 0 && Coups.EnAttente == 0 && Mouvement.EnAttente == 0 && Son.Frottements == 0 && Boule.Nombre == 0 && Rayon.Nombre == 0 && Lezardes.Nombre == 0)
				{
					return Garde.Nombre == 0;
				}
				return false;
			}
		}

		private static bool EstDuTri(CoucheDEffet couche)
		{
			if (couche - 1 <= CoucheDEffet.DerriereUnCorps)
			{
				return true;
			}
			return false;
		}

		internal static void Bande(string nom, float x, float y, float sensX, float sensY, CoucheDEffet couche, float echelle = 1f, Guid corps = default(Guid), int retardMs = 0)
		{
			Bande(nom, x, y, sensX, sensY, couche, echelle, corps, retardMs, Timing.Global.Milliseconds);
		}

		internal static bool Bande(string nom, float x, float y, float sensX, float sensY, CoucheDEffet couche, float echelle, Guid corps, int retardMs, long maintenant)
		{
			if (AllureActive && Bandes.Trouver(nom, out var bande))
			{
				IGameTexture gameTexture = Bandes.Planche(in bande);
				if (gameTexture != null)
				{
					if (_nombre == 64)
					{
						Balayer(maintenant);
						if (_nombre == 64)
						{
							Retirer(0);
						}
					}
					ref Joue reference = ref Joues[_nombre++];
					reference = new Joue
					{
						Bande = bande,
						Planche = gameTexture,
						X = x,
						Y = y,
						SensX = sensX,
						SensY = sensY,
						Couche = couche,
						Echelle = Bandes.Echelle(echelle),
						Suivi = corps,
						Depart = maintenant + Math.Max(0, retardMs)
					};
					if (corps != default(Guid) && Globals.Entities.TryGetValue(corps, out Entity value))
					{
						(float X, float Y) tuple = PlaceDe(value);
						float item = tuple.X;
						float item2 = tuple.Y;
						reference.Suit = true;
						reference.EcartX = x - item;
						reference.EcartY = y - item2;
					}
					if (EstDuTri(couche))
					{
						_accroches++;
					}
					return true;
				}
			}
			return false;
		}

		internal static bool Bande(string nom, float x, float y, float sensX, float sensY, CoucheDEffet couche, float echelle, Guid corps, int retardMs, long maintenant, Color teinte)
		{
			if (!Bande(nom, x, y, sensX, sensY, couche, echelle, corps, retardMs, maintenant))
			{
				return false;
			}
			Joues[_nombre - 1].Teinte = teinte;
			return true;
		}

		internal static void PlanterLaDerniere()
		{
			if (_nombre > 0)
			{
				Joues[_nombre - 1].Suit = false;
			}
		}

		internal static void Grains(float x, float y, int nombre, float axe, float ouvert, float vMin, float vMax, float gravite, int vieMs, Color[] tons, bool tirets = false, float solY = float.NaN, int resteMs = 0, Color? cerne = null)
		{
			if (AllureActive)
			{
				Particules.Lancer(x, y, nombre, axe, ouvert, vMin, vMax, gravite, vieMs, (tons != null && tons.Length > 0) ? tons : Particules.TonsDuSol(x, y), tirets, solY, resteMs, cerne, Globals.Random, Timing.Global.Milliseconds);
			}
		}

		internal static void Talons(float x, float y, float dx, float dy, int pousseMs, int vieMs)
		{
			if (AllureActive)
			{
				Sol.Talons(x, y, dx, dy, pousseMs, vieMs, Timing.Global.Milliseconds);
			}
		}

		internal static void Sillon(float x, float y, float dx, float dy, int pousseMs, int vieMs, bool fin = false)
		{
			if (AllureActive)
			{
				Sol.Sillon(x, y, dx, dy, pousseMs, vieMs, fin, Timing.Global.Milliseconds);
			}
		}

		internal static void ArcRacle(float x, float y, float rayon, float angle, int vieMs)
		{
			if (AllureActive)
			{
				Sol.Arc(x, y, rayon, angle, vieMs, Timing.Global.Milliseconds);
			}
		}

		internal static void Fissures(float x, float y)
		{
			if (AllureActive)
			{
				Sol.Fissures(x, y, Timing.Global.Milliseconds);
			}
		}

		internal static void TraitsDeVitesse(Entity corps, float sensX, float sensY, int dureeMs, int nombre, float large)
		{
			if (AllureActive)
			{
				Traits.Lancer(corps.Id, sensX, sensY, dureeMs, nombre, large, Timing.Global.Milliseconds);
			}
		}

		internal static void Oublier()
		{
			Array.Clear(Joues, 0, _nombre);
			_nombre = 0;
			_accroches = 0;
			Particules.Oublier();
			Sol.Oublier();
			Traits.Oublier();
			Fantomes.OublierLesLaissees();
			Coups.Oublier();
			Mouvement.Oublier();
			Son.CouperLesFrottements();
			Garde.Oublier();
			Son.OublierLesPieds();
			Son.OublierLesFauches();
			Chiffres.Oublier();
			Boule.Oublier();
			Kiai.Oublier();
			Rayon.Oublier();
			Lezardes.Oublier();
		}

		internal static void DessinerAuSol()
		{
			Marques.DessinerAuSol();
			if (!RienAMontrer)
			{
				if (!AllureActive)
				{
					Sol.Oublier();
					Coups.OublierLesSuites();
					Mouvement.Oublier();
					Son.CouperLesFrottements();
					Garde.Oublier();
					Boule.Lacher();
					Rayon.Oublier();
					Lezardes.Oublier();
				}
				else
				{
					DessinerAuSol(Timing.Global.Milliseconds);
				}
			}
		}

		internal static void DessinerAuSol(long maintenant)
		{
			ChronoImage.Tour("sol : ki, sang, vol, sable");
			Coups.Suivre(maintenant);
			Mouvement.Suivre(maintenant);
			Son.SuivreLesFrottements(maintenant);
			Garde.Suivre(maintenant);
			Boule.Suivre(maintenant);
			Rayon.Battre();
			Balayer(maintenant);
			Sol.Dessiner(maintenant);
			Lezardes.Dessiner(maintenant);
			Particules.DessinerPoses(maintenant);
			for (int i = 0; i < _nombre; i++)
			{
				ref Joue reference = ref Joues[i];
				if (reference.Couche == CoucheDEffet.Sol)
				{
					Suivre(ref reference);
					Dessiner(ref reference, maintenant);
				}
			}
			Fantomes.Dessiner(maintenant);
			ChronoImage.Tour("heritage : effets");
		}

		internal static void DessinerDansLeTri(Entity entite, bool derriere)
		{
			Marques.DessinerDansLeTri(entite, derriere);
			if ((_accroches != 0 || Rayon.Charges != 0) && AllureActive)
			{
				DessinerDansLeTri(entite, derriere, Timing.Global.Milliseconds);
			}
		}

		internal static void DessinerDansLeTri(Entity entite, bool derriere, long maintenant)
		{
			CoucheDEffet coucheDEffet = (derriere ? CoucheDEffet.DerriereUnCorps : CoucheDEffet.DevantUnCorps);
			Guid id = entite.Id;
			bool flag = false;
			for (int i = 0; i < _nombre; i++)
			{
				ref Joue reference = ref Joues[i];
				if (reference.Couche == coucheDEffet && !(reference.Suivi != id))
				{
					if (!flag)
					{
						ChronoImage.Tour("monde : personnages");
						flag = true;
					}
					if (reference.Suit)
					{
						Placer(ref reference, entite);
					}
					Dessiner(ref reference, maintenant);
				}
			}
			if (Rayon.ChargeVue(id))
			{
				if (!flag)
				{
					ChronoImage.Tour("monde : personnages");
					flag = true;
				}
				Rayon.DessinerLaCouronne(entite, derriere, maintenant);
			}
			if (flag)
			{
				ChronoImage.Tour("heritage : effets");
			}
		}

		internal static void DessinerSousLesCimes()
		{
			if (!RienAMontrer && AllureActive)
			{
				DessinerSousLesCimes(Timing.Global.Milliseconds);
			}
		}

		internal static void DessinerSousLesCimes(long maintenant)
		{
			ChronoImage.Tour("monde : personnages");
			for (int i = 0; i < _nombre; i++)
			{
				ref Joue reference = ref Joues[i];
				if (reference.Couche == CoucheDEffet.SousLesCimes)
				{
					Suivre(ref reference);
					Dessiner(ref reference, maintenant);
				}
				else if (EstDuTri(reference.Couche) && SonCorpsNEstPlusDansLeTri(ref reference))
				{
					Dessiner(ref reference, maintenant);
				}
			}
			Traits.Dessiner(maintenant);
			Garde.Dessiner(maintenant);
			Particules.DessinerEnLAir(maintenant);
			Boule.DessinerALHeure(maintenant);
			Rayon.DessinerLesCloues(maintenant);
			ChronoImage.Tour("heritage : effets");
		}

		internal static void DessinerParDessus()
		{
			if (Chiffres.Nombre != 0 || (_nombre != 0 && AllureActive))
			{
				DessinerParDessus(Timing.Global.Milliseconds);
			}
		}

		internal static void DessinerParDessus(long maintenant)
		{
			bool flag = false;
			for (int i = 0; i < _nombre; i++)
			{
				ref Joue reference = ref Joues[i];
				if (reference.Couche == CoucheDEffet.ParDessus)
				{
					if (!flag)
					{
						ChronoImage.Tour("monde : noms, barres et textes");
						flag = true;
					}
					Suivre(ref reference);
					Dessiner(ref reference, maintenant);
				}
			}
			if (Chiffres.Nombre > 0)
			{
				if (!flag)
				{
					ChronoImage.Tour("monde : noms, barres et textes");
					flag = true;
				}
				Chiffres.Dessiner(maintenant);
			}
			if (flag)
			{
				ChronoImage.Tour("heritage : effets");
			}
		}

		internal static void Peindre(IGameTexture? texture, FloatRect source, FloatRect destination, Color couleur, float rotation)
		{
			Temoin?.Invoke(texture, source, destination, couleur, rotation);
			if (texture != null)
			{
				Graphics.DrawGameTexture(texture, source, destination, couleur, null, GameBlendModes.None, null, rotation);
			}
		}

		private static void Dessiner(ref Joue joue, long maintenant)
		{
			int num = Bandes.Image(in joue.Bande, maintenant - joue.Depart);
			if (num >= 0)
			{
				var (source, destination, rotation) = Bandes.Rectangle(in joue.Bande, joue.X, joue.Y, joue.SensX, joue.SensY, joue.Echelle, num);
				Peindre(joue.Planche, source, destination, joue.Teinte ?? Blanc, rotation);
			}
		}

		internal static (float X, float Y) PlaceDe(Entity entite)
		{
			System.Numerics.Vector2 origin = entite.Origin;
			return (X: origin.X, Y: origin.Y - Echanges.Hauteur(entite));
		}

		private static void Placer(ref Joue joue, Entity corps)
		{
			(float X, float Y) tuple = PlaceDe(corps);
			float item = tuple.X;
			float item2 = tuple.Y;
			joue.X = item + joue.EcartX;
			joue.Y = item2 + joue.EcartY;
		}

		private static void Suivre(ref Joue joue)
		{
			if (joue.Suit && Globals.Entities.TryGetValue(joue.Suivi, out Entity value))
			{
				Placer(ref joue, value);
			}
		}

		private static bool SonCorpsNEstPlusDansLeTri(ref Joue joue)
		{
			if (!Globals.Entities.TryGetValue(joue.Suivi, out Entity value))
			{
				return true;
			}
			if (!Flight.IsAirborne(value))
			{
				return false;
			}
			if (joue.Suit)
			{
				Placer(ref joue, value);
			}
			return true;
		}

		private static void Balayer(long maintenant)
		{
			int num = 0;
			_accroches = 0;
			for (int i = 0; i < _nombre; i++)
			{
				ref Joue reference = ref Joues[i];
				if (maintenant < reference.Depart + reference.Bande.DureeMs)
				{
					if (EstDuTri(reference.Couche))
					{
						_accroches++;
					}
					if (num != i)
					{
						Joues[num] = reference;
					}
					num++;
				}
			}
			Array.Clear(Joues, num, _nombre - num);
			_nombre = num;
		}

		private static void Retirer(int index)
		{
			if (EstDuTri(Joues[index].Couche))
			{
				_accroches--;
			}
			Array.Copy(Joues, index + 1, Joues, index, _nombre - index - 1);
			Joues[--_nombre] = default(Joue);
		}

		internal static bool PrendLeCoup(Entity? attaquant, Entity cible, bool dejaMontre)
		{
			return PrendLeCoup(attaquant, cible, dejaMontre, Timing.Global.Milliseconds);
		}

		internal static bool PrendLeCoup(Entity? attaquant, Entity cible, bool dejaMontre, long maintenant)
		{
			if (!AllureActive || !(attaquant is Player player))
			{
				return false;
			}
			if (Kiai.Blesse(player.Id, maintenant))
			{
				return true;
			}
			CoupHeritage coupHeritage = Coups.CoupDe(player, maintenant);
			if (coupHeritage == CoupHeritage.Aucun)
			{
				return false;
			}
			if (player == Globals.Me && cible.Id != player.CibleDuDernierCoupHeritage && (!HeritageRegles.EstUneFauche(coupHeritage) || !Coups.ALaPorteeDeLaFauche(player, cible)))
			{
				return false;
			}
			if (!dejaMontre && !LImpactLocalAttend(player, cible))
			{
				Coups.Jouer(player, cible, coupHeritage, 1, maintenant);
			}
			return true;
		}

		internal static bool TaitLeCritique(Entity? attaquant)
		{
			return TaitLeCritique(attaquant, Timing.Global.Milliseconds);
		}

		internal static bool TaitLeCritique(Entity? attaquant, long maintenant)
		{
			if (attaquant != null && Coups.CoupDe(attaquant, maintenant) == CoupHeritage.CoupDePied && (Son.PiedGrave(attaquant.Id, maintenant) || LImpactSonneraGrave(attaquant)))
			{
				return !Kiai.Blesse(attaquant.Id, maintenant);
			}
			return false;
		}
	}

	internal static class Elan
	{
		internal struct Budget
		{
			public float Reste;

			public float Vitesse;

			public float Age;

			public float Ferme;

			public float Recul;

			public bool EnArriere;

			public readonly bool Ouvert
			{
				get
				{
					if (Reste > 0f)
					{
						return Age < Ferme;
					}
					return false;
				}
			}
		}

		internal const float JeuDeLEcart = 0.5f;

		private static Budget _budget;

		private static Guid _cibleId;

		private static long _glissadeAuDepart;

		internal const float AllureDAttente = 0.5f;

		private static long _attenteDe;

		private static float _reculDAttente;

		private static bool _pasDAttente;

		private static readonly CoupHeritage[] LesCoupsAElan = new CoupHeritage[5]
		{
			CoupHeritage.Direct,
			CoupHeritage.Crochet,
			CoupHeritage.CoupDEpaule,
			CoupHeritage.Montant,
			CoupHeritage.CoupDePied
		};

		internal const int RemanenceApresLElanMs = 30;

		internal const int RetombeeDuMontantMs = 80;

		private const int RetombeeMinMs = 16;

		internal static float Reste => _budget.Reste;

		internal static float ResteDeRecul
		{
			get
			{
				if (!(_budget.Reste > 0f))
				{
					return 0f;
				}
				return _budget.Recul;
			}
		}

		internal static Budget Nouveau(int elanPx, int elanMs, int impactAuTemps, int suiteMs, int reculMaxPx = 0)
		{
			if (elanPx <= 0)
			{
				return default(Budget);
			}
			int num = Math.Max(0, impactAuTemps) + Math.Max(0, suiteMs);
			return new Budget
			{
				Reste = elanPx,
				Vitesse = ((elanMs > 0) ? ((float)elanPx / (float)Math.Max(1, Math.Min(elanMs, num))) : 0f),
				Age = 0f,
				Ferme = num,
				Recul = Math.Max(0, reculMaxPx)
			};
		}

		internal static (float X, float Y) Pas(ref Budget budget, float ecoule, bool entrave, float x, float y, float cibleX, float cibleY, float contact)
		{
			budget.EnArriere = false;
			if (budget.Reste <= 0f)
			{
				return (X: 0f, Y: 0f);
			}
			if (budget.Age >= budget.Ferme)
			{
				budget.Reste = 0f;
				return (X: 0f, Y: 0f);
			}
			if (entrave)
			{
				budget.Age += Math.Max(0f, ecoule);
				return (X: 0f, Y: 0f);
			}
			float num = Math.Clamp(ecoule, 0f, budget.Ferme - budget.Age);
			budget.Age += Math.Max(0f, ecoule);
			float num2 = cibleX - x;
			float num3 = cibleY - y;
			float num4 = MathF.Sqrt(num2 * num2 + num3 * num3);
			if (num4 < 0.01f)
			{
				return (X: 0f, Y: 0f);
			}
			float num5 = contact - num4;
			if (num5 > 0.5f && budget.Recul > 0f)
			{
				float num6 = MathF.Min(budget.Recul, num5);
				if (budget.Vitesse > 0f)
				{
					num6 = MathF.Min(num6, budget.Vitesse * num);
				}
				if (num6 <= 0f)
				{
					return (X: 0f, Y: 0f);
				}
				budget.EnArriere = true;
				return (X: (0f - num2) / num4 * num6, Y: (0f - num3) / num4 * num6);
			}
			float x2 = ((budget.Vitesse > 0f) ? MathF.Min(budget.Reste, budget.Vitesse * num) : budget.Reste);
			x2 = MathF.Min(x2, num4 - contact);
			if (x2 <= 0f)
			{
				return (X: 0f, Y: 0f);
			}
			return (X: num2 / num4 * x2, Y: num3 / num4 * x2);
		}

		internal static void Consommer(ref Budget budget, float parcouru)
		{
			if (budget.EnArriere)
			{
				budget.Recul = MathF.Max(0f, budget.Recul - MathF.Max(0f, parcouru));
			}
			else
			{
				budget.Reste = MathF.Max(0f, budget.Reste - MathF.Max(0f, parcouru));
			}
		}

		internal static void Ouvrir(Player moi, Entity cible, CoupHeritage coup)
		{
			_budget = default(Budget);
			CoupHeritageOptions coupHeritageOptions = HeritageRegles.Table(coup);
			if (coupHeritageOptions.ElanPx > 0 && (HeritageRegles.EstADirection(coup) || !moi.DirectionTenuePourLElan()))
			{
				float ecartY;
				float ecartX;
				if (!moi.PositionMonde(out var mondeX, out var mondeY) || !cible.PositionMonde(out var mondeX2, out var mondeY2))
				{
					ecartY = 0f;
					ecartX = 1f;
				}
				else
				{
					float num = mondeX2 - mondeX;
					float num2 = mondeY2 - mondeY;
					ecartY = num2;
					ecartX = num;
				}
				HeritageOptions heritage = Options.Instance.Combat.Heritage;
				_budget = Nouveau(coupHeritageOptions.ElanPx, coupHeritageOptions.ElanMs, HeureDeLImpact(coup, moi.AvanceDuCoupEnCours), heritage.ElanSuiteMs, (coupHeritageOptions.EcartPx > 0) ? ReserveDeRecul(heritage.Etats.EcartReculMaxPx, HeritageRegles.AllongeVerticale(coup, ecartX, ecartY)) : 0);
				_cibleId = cible.Id;
				_glissadeAuDepart = moi.DashTimer;
			}
		}

		internal static int ReserveDeRecul(int reculMaxPx, float allongeVerticale)
		{
			if (reculMaxPx > 0)
			{
				return reculMaxPx + (int)MathF.Ceiling(MathF.Max(0f, allongeVerticale));
			}
			return 0;
		}

		internal static void Fermer()
		{
			_budget = default(Budget);
			_attenteDe = 0L;
			_reculDAttente = 0f;
			_pasDAttente = false;
		}

		internal static (float X, float Y) Pas(Player moi, float x, float y, float ecoule, bool entrave)
		{
			if (_budget.Reste <= 0f)
			{
				return PasDAttente(moi, x, y, ecoule, entrave);
			}
			_pasDAttente = false;
			if (!HeritageRegles.Actif || moi.CoupEnCours == CoupHeritage.Aucun || moi.CibleDuCoupEnCours != _cibleId || moi.DashTimer != _glissadeAuDepart || !Globals.Entities.TryGetValue(_cibleId, out Entity value) || value == null || value.IsDying || !value.PositionMonde(out var mondeX, out var mondeY))
			{
				_budget = default(Budget);
				return (X: 0f, Y: 0f);
			}
			if (entrave)
			{
				_budget.Age = MathF.Max(_budget.Age, (float)TempsDuGeste(moi) - MathF.Max(0f, ecoule));
			}
			return Pas(ref _budget, ecoule, entrave, x, y, mondeX, mondeY, OuSArreteLElan(moi.CoupEnCours, value.RayonDuCorps, mondeX - x, mondeY - y));
		}

		internal static float OuSArreteLElan(CoupHeritage coup, float rayonDeLaCible)
		{
			return OuSArreteLElan(coup, rayonDeLaCible, 1f, 0f);
		}

		internal static float OuSArreteLElan(CoupHeritage coup, float rayonDeLaCible, float ecartX, float ecartY)
		{
			return MathF.Max(10f + rayonDeLaCible, HeritageRegles.EcartDuCoup(coup, ecartX, ecartY));
		}

		internal static void Consommer(float parcouru)
		{
			if (_pasDAttente)
			{
				_pasDAttente = false;
				_reculDAttente = MathF.Max(0f, _reculDAttente - MathF.Max(0f, parcouru));
			}
			else
			{
				Consommer(ref _budget, parcouru);
			}
		}

		private static (float X, float Y) PasDAttente(Player moi, float x, float y, float ecoule, bool entrave)
		{
			_pasDAttente = false;
			long num = (HeritageRegles.Actif ? moi.AppuiQuiAttendHeritage : 0);
			if (num == 0L)
			{
				_attenteDe = 0L;
				return (X: 0f, Y: 0f);
			}
			if (entrave || moi.DirectionTenuePourLElan() || !CorpsTropPres(moi, x, y, out var cibleX, out var cibleY, out var ecart))
			{
				return (X: 0f, Y: 0f);
			}
			if (_attenteDe != num)
			{
				_attenteDe = num;
				_reculDAttente = ReserveDeRecul(Options.Instance.Combat.Heritage.Etats.EcartReculMaxPx, HeritageRegles.AllongeVerticaleMax(cibleX - x, cibleY - y));
			}
			(float X, float Y) tuple = PasEnArriere(_reculDAttente, 0.5f, ecoule, x, y, cibleX, cibleY, ecart);
			float item = tuple.X;
			float item2 = tuple.Y;
			_pasDAttente = item != 0f || item2 != 0f;
			return (X: item, Y: item2);
		}

		internal static (float X, float Y) PasEnArriere(float reserve, float allure, float ecoule, float x, float y, float cibleX, float cibleY, float ecart)
		{
			float num = cibleX - x;
			float num2 = cibleY - y;
			float num3 = MathF.Sqrt(num * num + num2 * num2);
			float num4 = ecart - num3;
			if (reserve <= 0f || num3 < 0.01f || num4 <= 0.5f)
			{
				return (X: 0f, Y: 0f);
			}
			float num5 = MathF.Min(MathF.Min(reserve, num4), MathF.Max(0f, allure * ecoule));
			if (!(num5 <= 0f))
			{
				return (X: (0f - num) / num3 * num5, Y: (0f - num2) / num3 * num5);
			}
			return (X: 0f, Y: 0f);
		}

		internal static float PlusPetitEcart(float rayonDeLaCible, float ecartX, float ecartY)
		{
			float num = 0f;
			CoupHeritage[] lesCoupsAElan = LesCoupsAElan;
			foreach (CoupHeritage coup in lesCoupsAElan)
			{
				CoupHeritageOptions coupHeritageOptions = HeritageRegles.Table(coup);
				if (coupHeritageOptions.ElanPx > 0 && coupHeritageOptions.EcartPx > 0)
				{
					float num2 = OuSArreteLElan(coup, rayonDeLaCible, ecartX, ecartY);
					num = ((num <= 0f) ? num2 : MathF.Min(num, num2));
				}
			}
			return num;
		}

		private static bool CorpsTropPres(Player moi, float x, float y, out float cibleX, out float cibleY, out float ecart)
		{
			cibleX = 0f;
			cibleY = 0f;
			ecart = 0f;
			float num = float.MaxValue;
			foreach (Entity value in Globals.Entities.Values)
			{
				if (value == null || value == moi || value.IsDying || !value.CanBeAttacked || value.Z != moi.Z || value is Resource || !value.PositionMonde(out var mondeX, out var mondeY))
				{
					continue;
				}
				bool flag = value.Type == EntityType.GlobalEntity && !(value is Critter);
				bool flag2 = flag;
				if (flag2)
				{
					NpcAggression aggression = value.Aggression;
					bool flag3 = (uint)(aggression - 3) <= 1u;
					flag2 = !flag3;
				}
				if (flag2 || !(value.Id != moi.TargetId))
				{
					float num2 = MathF.Sqrt((mondeX - x) * (mondeX - x) + (mondeY - y) * (mondeY - y));
					float num3 = PlusPetitEcart(value.RayonDuCorps, mondeX - x, mondeY - y);
					if (!(num2 >= num) && !(num2 >= num3 - 0.5f))
					{
						float num4 = mondeX;
						float num5 = mondeY;
						float num6 = num3;
						num = num2;
						cibleX = num4;
						cibleY = num5;
						ecart = num6;
					}
				}
			}
			return num < float.MaxValue;
		}

		internal static bool Remanent(CoupHeritage coup, long tempsDuGeste)
		{
			if (coup == CoupHeritage.CoupDEpaule && Options.Instance.Combat.Heritage.Remanences && tempsDuGeste >= 0)
			{
				return tempsDuGeste < HeritageRegles.Table(coup).ElanMs + 30;
			}
			return false;
		}

		internal static bool Remanent(Entity entite)
		{
			if (!HeritageRegles.Actif || !(entite is Player))
			{
				return false;
			}
			CoupHeritage coupEnCours = entite.CoupEnCours;
			if (coupEnCours == CoupHeritage.CoupDEpaule)
			{
				return Remanent(coupEnCours, TempsDuGeste(entite));
			}
			return false;
		}

		internal static float HauteurDuMontant(int ecoule, int duree, int avance)
		{
			int montantBondPx = Options.Instance.Combat.Heritage.MontantBondPx;
			if (montantBondPx <= 0 || duree <= 0 || ecoule < 0 || ecoule >= duree)
			{
				return 0f;
			}
			CoupHeritageOptions coupHeritageOptions = HeritageRegles.Table(CoupHeritage.Montant);
			avance = Math.Max(0, avance);
			int num = duree + avance;
			int num2 = ecoule + avance;
			int num3 = Math.Clamp(coupHeritageOptions.ImpactMs, 0, num);
			int num4 = Math.Clamp(coupHeritageOptions.ArmementTenuMs, 0, num3);
			if (num2 < num4)
			{
				return 0f;
			}
			if (num2 < num3)
			{
				return (float)(montantBondPx * (num2 - num4)) / (float)(num3 - num4);
			}
			int num5 = Math.Max(num3, Math.Min(num3 + Math.Max(0, coupHeritageOptions.TenueMs), num - 16));
			if (num2 < num5)
			{
				return montantBondPx;
			}
			int num6 = Math.Min(80, num - num5);
			if (num6 > 0)
			{
				return (float)montantBondPx * MathF.Max(0f, 1f - (float)(num2 - num5) / (float)num6);
			}
			return 0f;
		}

		internal static float HauteurDuMontant(Entity entite)
		{
			if (!(entite is Player) || entite.Dashing != null || entite.CoupEnCours != CoupHeritage.Montant)
			{
				return 0f;
			}
			return HauteurDuMontant((int)TempsDuGeste(entite), entite.DureeDuGeste(), entite.AvanceDuCoupEnCours);
		}
	}

	internal static class EtatsVus
	{
		private struct Vu
		{
			public long Depuis;

			public long Jusqua;

			public Guid Par;

			public long Rang;
		}

		internal const int EntitesMax = 64;

		private static readonly int Dernier = (int)Enum.GetValues<EtatHeritage>().Max();

		private static readonly Dictionary<Guid, Vu[]> ParEntite = new Dictionary<Guid, Vu[]>();

		private static long _rang;

		internal const uint Tous = uint.MaxValue;

		internal static int Entites => ParEntite.Count;

		internal static void Recevoir(Entity entite, EchangePacket paquet)
		{
			long milliseconds = Timing.Global.Milliseconds;
			EtatHeritage index = (EtatHeritage)paquet.Index;
			Noter(entite.Id, index, milliseconds, paquet.DureeMs, paquet.AutreId);
			Corps.SurEtat(entite, index, paquet.DureeMs, paquet.AutreId, milliseconds);
			if (Actif && index == EtatHeritage.Riposte)
			{
				Riposte.SurRiposte(entite);
			}
		}

		internal static void Noter(Guid id, EtatHeritage etat, long maintenant, int dureeMs, Guid par)
		{
			if (etat == (EtatHeritage)255)
			{
				ParEntite.Remove(id);
			}
			else
			{
				if ((int)etat < 1 || (int)etat > Dernier)
				{
					return;
				}
				if (dureeMs <= 0)
				{
					if (ParEntite.TryGetValue(id, out Vu[] value))
					{
						value[(uint)(etat - 1)] = default(Vu);
					}
					return;
				}
				if (!ParEntite.TryGetValue(id, out Vu[] value2))
				{
					FaireDeLaPlace(maintenant);
					value2 = new Vu[Dernier];
					ParEntite[id] = value2;
				}
				value2[(uint)(etat - 1)] = new Vu
				{
					Depuis = maintenant,
					Jusqua = maintenant + dureeMs,
					Par = par,
					Rang = ++_rang
				};
			}
		}

		internal static bool Vif(Guid id, EtatHeritage etat)
		{
			return Vif(id, etat, Timing.Global.Milliseconds);
		}

		internal static bool Vif(Guid id, EtatHeritage etat, long maintenant)
		{
			if ((int)etat >= 1 && (int)etat <= Dernier && ParEntite.Count > 0 && ParEntite.TryGetValue(id, out Vu[] value))
			{
				return maintenant < value[(uint)(etat - 1)].Jusqua;
			}
			return false;
		}

		internal static EtatHeritage LePlusRecent(Guid id, out long depuis, out long jusqua, out Guid par)
		{
			return LePlusRecent(id, Timing.Global.Milliseconds, out depuis, out jusqua, out par);
		}

		internal static EtatHeritage LePlusRecent(Guid id, long maintenant, out long depuis, out long jusqua, out Guid par)
		{
			return LePlusRecent(id, maintenant, uint.MaxValue, out depuis, out jusqua, out par);
		}

		internal static uint Bit(EtatHeritage etat)
		{
			if ((int)etat < 1 || (int)etat >= 32)
			{
				return 0u;
			}
			return (uint)(1 << (int)etat);
		}

		internal static EtatHeritage LePlusRecent(Guid id, long maintenant, uint parmi, out long depuis, out long jusqua, out Guid par)
		{
			depuis = 0L;
			jusqua = 0L;
			par = Guid.Empty;
			if (ParEntite.Count == 0 || !ParEntite.TryGetValue(id, out Vu[] value))
			{
				return EtatHeritage.Aucun;
			}
			int num = -1;
			bool flag = false;
			for (int i = 0; i < value.Length; i++)
			{
				if (maintenant < value[i].Jusqua)
				{
					flag = true;
					if (DansLeFiltre(parmi, i + 1) && (num < 0 || value[i].Rang > value[num].Rang))
					{
						num = i;
					}
				}
			}
			if (!flag)
			{
				ParEntite.Remove(id);
				return EtatHeritage.Aucun;
			}
			if (num < 0)
			{
				return EtatHeritage.Aucun;
			}
			depuis = value[num].Depuis;
			jusqua = value[num].Jusqua;
			par = value[num].Par;
			return (EtatHeritage)(num + 1);
		}

		private static bool DansLeFiltre(uint parmi, int numero)
		{
			if (parmi != uint.MaxValue)
			{
				if (numero < 32)
				{
					return (parmi & (uint)(1 << numero)) != 0;
				}
				return false;
			}
			return true;
		}

		internal static void Oublier()
		{
			ParEntite.Clear();
			_rang = 0L;
		}

		private static void FaireDeLaPlace(long maintenant)
		{
			if (ParEntite.Count < 64)
			{
				return;
			}
			Guid[] array = (from paire in ParEntite
				where FinDe(paire.Value) <= maintenant
				select paire.Key).ToArray();
			foreach (Guid key in array)
			{
				ParEntite.Remove(key);
			}
			while (ParEntite.Count >= 64)
			{
				ParEntite.Remove(ParEntite.MinBy<KeyValuePair<Guid, Vu[]>, long>((KeyValuePair<Guid, Vu[]> paire) => FinDe(paire.Value)).Key);
			}
		}

		private static long FinDe(Vu[] vus)
		{
			long num = 0L;
			for (int i = 0; i < vus.Length; i++)
			{
				Vu vu = vus[i];
				num = Math.Max(num, vu.Jusqua);
			}
			return num;
		}
	}

	internal static class Fantomes
	{
		private struct Laissee
		{
			public Guid Corps;

			public IGameTexture Planche;

			public FloatRect Source;

			public FloatRect Destination;

			public IGameTexture? Silhouette;

			public Color Couleur;

			public float Force;

			public long Nee;

			public int VieMs;
		}

		internal const int EntitesMax = 64;

		private static readonly Dictionary<Guid, (IGameTexture Texture, FloatRect Source, FloatRect Destination, long Rang)> Images = new Dictionary<Guid, (IGameTexture, FloatRect, FloatRect, long)>();

		private static long _rang;

		internal const int LaisseesMax = 32;

		internal const int ParCorpsMax = 3;

		private static readonly Laissee[] LesLaissees = new Laissee[32];

		private static int _laissees;

		internal static Func<IGameTexture, FloatRect, IGameTexture?>? Masque;

		internal static int Entites => Images.Count;

		internal static int Laissees => _laissees;

		internal static void Noter(Entity entite, IGameTexture texture, FloatRect source, FloatRect destination)
		{
			Noter(entite.Id, texture, source, destination);
		}

		internal static void Noter(Guid id, IGameTexture texture, FloatRect source, FloatRect destination)
		{
			if (Images.Count >= 64 && !Images.ContainsKey(id))
			{
				Guid key = Guid.Empty;
				long num = long.MaxValue;
				foreach (var (guid2, tuple2) in Images)
				{
					if (tuple2.Item4 < num)
					{
						num = tuple2.Item4;
						key = guid2;
					}
				}
				Images.Remove(key);
			}
			Images[id] = (texture, source, destination, ++_rang);
		}

		internal static bool DerniereImage(Guid id, out IGameTexture? texture, out FloatRect source, out FloatRect destination)
		{
			if (Images.Count > 0 && Images.TryGetValue(id, out (IGameTexture, FloatRect, FloatRect, long) value))
			{
				(texture, source, destination, _) = value;
				return true;
			}
			texture = null;
			source = default(FloatRect);
			destination = default(FloatRect);
			return false;
		}

		internal static bool SansEcho(Entity entite)
		{
			HeritageAllureOptions allure = Options.Instance.Combat.Heritage.Allure;
			if (allure.Actif && allure.Fantomes)
			{
				Dash dashing = entite.Dashing;
				if (dashing != null)
				{
					if ((dashing.FormeHeritage & FormeDeLancer.DeLHeritage) == 0)
					{
						return dashing.Nature == NatureDeGlissade.Fauche;
					}
					return true;
				}
			}
			return false;
		}

		internal static void Laisser(Entity corps, int vieMs, float force, Color couleur, float ecartMin = 14f)
		{
			Laisser(corps.Id, vieMs, force, couleur, ecartMin, Timing.Global.Milliseconds);
		}

		internal static bool Laisser(Guid id, int vieMs, float force, Color couleur, float ecartMin, long maintenant)
		{
			HeritageAllureOptions allure = Options.Instance.Combat.Heritage.Allure;
			if (!allure.Actif || !allure.Fantomes || vieMs <= 0 || !(force > 0f) || (object)couleur == null || !DerniereImage(id, out IGameTexture texture, out FloatRect source, out FloatRect destination) || texture == null)
			{
				return false;
			}
			int num = 0;
			int num2 = -1;
			int num3 = -1;
			for (int i = 0; i < _laissees; i++)
			{
				ref Laissee reference = ref LesLaissees[i];
				if (!(reference.Corps != id) && Age(in reference, maintenant) < reference.VieMs)
				{
					num++;
					num3 = i;
					if (num2 < 0)
					{
						num2 = i;
					}
				}
			}
			if (num3 >= 0)
			{
				float num4 = LesLaissees[num3].Destination.X - destination.X;
				float num5 = LesLaissees[num3].Destination.Y - destination.Y;
				if (MathF.Sqrt(num4 * num4 + num5 * num5) < ecartMin)
				{
					return false;
				}
			}
			if (num >= 3)
			{
				Retirer(num2);
			}
			if (_laissees == 32)
			{
				Retirer(0);
			}
			LesLaissees[_laissees++] = new Laissee
			{
				Corps = id,
				Planche = texture,
				Source = source,
				Destination = destination,
				Couleur = couleur,
				Force = MathF.Min(force, 1f),
				Nee = maintenant,
				VieMs = vieMs
			};
			return true;
		}

		internal static void Dessiner(long maintenant)
		{
			int num = 0;
			for (int i = 0; i < _laissees; i++)
			{
				ref Laissee reference = ref LesLaissees[i];
				long num2 = Age(in reference, maintenant);
				if (num2 < reference.VieMs)
				{
					ref IGameTexture silhouette = ref reference.Silhouette;
					if (silhouette == null)
					{
						silhouette = ((Masque != null) ? Masque(reference.Planche, reference.Source) : KiCharge.MaskOf(reference.Planche, reference.Source, 0));
					}
					IGameTexture silhouette2 = reference.Silhouette;
					if (silhouette2 != null)
					{
						float num3 = reference.Force * (1f - (float)num2 / (float)reference.VieMs);
						Effets.Peindre(silhouette2, new FloatRect(0f, 0f, silhouette2.Width, silhouette2.Height), reference.Destination, new Color((int)(255f * num3), reference.Couleur.R, reference.Couleur.G, reference.Couleur.B), 0f);
					}
					if (num != i)
					{
						LesLaissees[num] = reference;
					}
					num++;
				}
			}
			Array.Clear(LesLaissees, num, _laissees - num);
			_laissees = num;
		}

		private static long Age(in Laissee image, long maintenant)
		{
			return Math.Max(0L, maintenant - image.Nee);
		}

		private static void Retirer(int index)
		{
			Array.Copy(LesLaissees, index + 1, LesLaissees, index, _laissees - index - 1);
			LesLaissees[--_laissees] = default(Laissee);
		}

		internal static void OublierLesLaissees()
		{
			Array.Clear(LesLaissees, 0, _laissees);
			_laissees = 0;
		}

		internal static void Oublier()
		{
			Images.Clear();
			_rang = 0L;
			OublierLesLaissees();
		}
	}

	internal static class Garde
	{
		internal readonly record struct Tons(Color Tete, Color Milieu, Color Queue, Color Cerne);

		internal readonly record struct Lot(float LongMin, float LongMax, float VitMin, float VitMax, int VieMs, float AngleMin, float AngleMax, int Nombre);

		internal struct Etincelle
		{
			internal float X;

			internal float Y;

			internal float Angle;

			internal float Longueur;

			internal float Vitesse;

			internal int VieMs;

			internal float K1;

			internal float K2;

			internal long Depart;

			internal Tons Tons;
		}

		private readonly record struct Eclair((float X, float Y)[] Points, long Depart);

		private sealed class Jet
		{
			internal Guid Corps;

			internal Guid Attaquant;

			internal long Fin;

			internal long Prochain;

			internal int Cote;

			internal MapSound? Son;

			internal float Volume;
		}

		internal const float ContactRayonPx = 10f;

		internal const float ContactHautPx = 19f;

		internal const float EclairLongPx = 26f;

		internal const int EclairPleinMs = 60;

		internal const int EclairFonduMs = 50;

		internal const int TalonsVieMs = 6000;

		internal const int JetPasMs = 30;

		internal const int JetMargeMs = 120;

		internal const int FonduDuRayonMs = 150;

		internal const int BouleNoteeMs = 400;

		internal const int BouleAttenteMs = 200;

		internal const float TitubePx = 2f;

		internal const int EtincellesMax = 128;

		private const int DistanceDEcoute = 12;

		private static readonly Color Brun = new Color(255, 74, 28, 6);

		private static readonly Color Nuit = new Color(255, 10, 31, 102);

		internal static readonly Tons DuCoup = new Tons(new Color(255, 255, 255, 255), new Color(255, 255, 243, 176), new Color(255, 248, 216, 72), Brun);

		internal static readonly Tons DeLaBoule = new Tons(new Color(255, 248, 248, 72), new Color(255, 248, 208, 0), new Color(255, 248, 176, 40), Brun);

		internal static readonly Tons DuRayon = new Tons(new Color(255, 255, 255, 255), new Color(255, 207, 244, 255), new Color(255, 108, 203, 255), Nuit);

		internal static readonly Tons DeLaBrisee = new Tons(new Color(255, 255, 226, 184), new Color(255, 255, 138, 31), new Color(255, 232, 64, 28), new Color(255, 58, 8, 4));

		internal static readonly Color BlancDeLEclair = new Color(255, 255, 255, 255);

		internal static readonly Color CerneDeLEclair = Nuit;

		internal static readonly Lot DuLeger = new Lot(5f, 9f, 30f, 80f, 140, 0.5f, 1.75f, 5);

		internal static readonly Lot DuLourd = new Lot(7f, 12f, 40f, 95f, 170, 0.5f, 1.75f, 7);

		internal static readonly Lot DeBoule = new Lot(6f, 11f, 35f, 90f, 170, 0.5f, 1.75f, 7);

		internal static readonly Lot DeBrisee = new Lot(7f, 12f, 55f, 115f, 200, 0.3f, 1.7f, 7);

		internal static readonly Lot DuJet = new Lot(6f, 12f, 45f, 100f, 140, 0.6f, 1.3f, 2);

		private static readonly List<Etincelle> Etincelles = new List<Etincelle>();

		private static readonly List<Eclair> Eclairs = new List<Eclair>();

		private static readonly List<Jet> Jets = new List<Jet>();

		private static readonly Dictionary<Guid, (Tons Tons, long A)> BoulesNotees = new Dictionary<Guid, (Tons, long)>();

		private static readonly List<(Guid Corps, Guid Attaquant, long Depuis)> BoulesAttendues = new List<(Guid, Guid, long)>();

		private static readonly Dictionary<Guid, (float Sens, long Debut, long Fin)> Titubes = new Dictionary<Guid, (float, long, long)>();

		private static readonly List<(int X, int Y, float F)> Tampon = new List<(int, int, float)>();

		private static readonly Random Hasard = new Random();

		private static readonly FloatRect UnPixel = new FloatRect(0f, 0f, 1f, 1f);

		internal static Action<string, Entity, float>? Temoin;

		internal static bool Montre
		{
			get
			{
				if (Actif)
				{
					return Options.Instance.Combat.Heritage.Allure.Actif;
				}
				return false;
			}
		}

		internal static int Nombre => Etincelles.Count + Eclairs.Count + Jets.Count + BoulesAttendues.Count;

		internal static Tons? TonsDe(GuardEvent evenement, GenreDeCoupGarde genre, Tons? boule = null)
		{
			if (genre == GenreDeCoupGarde.Aucun)
			{
				return null;
			}
			return evenement switch
			{
				GuardEvent.Break => DeLaBrisee, 
				GuardEvent.Blocked => genre switch
				{
					GenreDeCoupGarde.Boule => boule ?? DeLaBoule, 
					GenreDeCoupGarde.Rayon => DuRayon, 
					_ => DuCoup, 
				}, 
				_ => null, 
			};
		}

		internal static Tons TonsDeLaBoule(Color clair, Color moyen, Color sombre)
		{
			return new Tons(clair, moyen, sombre, new Color(255, sombre.R * 3 / 10, sombre.G * 3 / 10, sombre.B * 3 / 10));
		}

		internal static Lot? LotDe(GuardEvent evenement, GenreDeCoupGarde genre)
		{
			if (genre == GenreDeCoupGarde.Aucun)
			{
				return null;
			}
			return evenement switch
			{
				GuardEvent.Break => DeBrisee, 
				GuardEvent.Blocked => genre switch
				{
					GenreDeCoupGarde.Lourd => DuLourd, 
					GenreDeCoupGarde.Boule => DeBoule, 
					GenreDeCoupGarde.Rayon => DuJet, 
					_ => DuLeger, 
				}, 
				_ => null, 
			};
		}

		internal static void Jeter(List<Etincelle> dans, float x, float y, int axeX, int axeY, in Lot lot, Tons tons, int decale, Random hasard, long maintenant)
		{
			float num = MathF.Atan2(-axeY, -axeX);
			for (int i = 0; i < lot.Nombre; i++)
			{
				float num2 = (((i + decale) % 2 == 1) ? 1f : (-1f));
				float num3 = (0.8f + (float)hasard.NextDouble()) * ((hasard.NextDouble() < 0.5) ? (-1f) : 1f);
				dans.Add(new Etincelle
				{
					X = x,
					Y = y,
					Angle = num + num2 * (lot.AngleMin + (float)hasard.NextDouble() * (lot.AngleMax - lot.AngleMin)),
					Longueur = lot.LongMin + (float)hasard.NextDouble() * (lot.LongMax - lot.LongMin),
					Vitesse = lot.VitMin + (float)hasard.NextDouble() * (lot.VitMax - lot.VitMin),
					VieMs = (int)((double)lot.VieMs * (0.75 + hasard.NextDouble() * 0.5)),
					K1 = num3,
					K2 = (float)(-MathF.Sign(num3)) * (0.6f + (float)hasard.NextDouble() * 0.9f),
					Depart = maintenant,
					Tons = tons
				});
			}
		}

		internal static bool Pixels(in Etincelle e, long maintenant, List<(int X, int Y, float F)> pixels, out float alpha)
		{
			pixels.Clear();
			alpha = 0f;
			long num = maintenant - e.Depart;
			if (e.VieMs <= 0 || num < 0 || num > e.VieMs)
			{
				return false;
			}
			float num2 = (float)num / (float)e.VieMs;
			float num3 = MathF.Cos(e.Angle);
			float num4 = MathF.Sin(e.Angle);
			float num5 = 1f + e.Vitesse * (float)num / 1000f;
			float num6 = e.Longueur * MathF.Min(1f, ((float)num + 17f) / 50f) * ((num2 > 0.6f) ? (1f - (num2 - 0.6f) * 1.2f) : 1f);
			_003C_003Ey__InlineArray4<float> buffer = default(_003C_003Ey__InlineArray4<float>);
			global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<_003C_003Ey__InlineArray4<float>, float>(ref buffer, 0) = 0f;
			global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<_003C_003Ey__InlineArray4<float>, float>(ref buffer, 1) = 0.4f;
			global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<_003C_003Ey__InlineArray4<float>, float>(ref buffer, 2) = 0.72f;
			global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<_003C_003Ey__InlineArray4<float>, float>(ref buffer, 3) = 1f;
			Span<float> span = global::_003CPrivateImplementationDetails_003E.InlineArrayAsSpan<_003C_003Ey__InlineArray4<float>, float>(ref buffer, 4);
			_003C_003Ey__InlineArray4<float> buffer2 = default(_003C_003Ey__InlineArray4<float>);
			global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<_003C_003Ey__InlineArray4<float>, float>(ref buffer2, 0) = 0f;
			global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<_003C_003Ey__InlineArray4<float>, float>(ref buffer2, 1) = e.K1;
			global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<_003C_003Ey__InlineArray4<float>, float>(ref buffer2, 2) = e.K2;
			global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<_003C_003Ey__InlineArray4<float>, float>(ref buffer2, 3) = 0f;
			Span<float> span2 = global::_003CPrivateImplementationDetails_003E.InlineArrayAsSpan<_003C_003Ey__InlineArray4<float>, float>(ref buffer2, 4);
			Span<float> span3 = stackalloc float[4];
			Span<float> span4 = stackalloc float[4];
			for (int i = 0; i < 4; i++)
			{
				span3[i] = e.X + num3 * (num5 + span[i] * num6) - num4 * span2[i];
				span4[i] = e.Y + num4 * (num5 + span[i] * num6) + num3 * span2[i];
			}
			int num7 = int.MinValue;
			int num8 = int.MinValue;
			for (int j = 0; j < 3; j++)
			{
				int num9 = Math.Max(1, (int)MathF.Ceiling(MathF.Sqrt((span3[j + 1] - span3[j]) * (span3[j + 1] - span3[j]) + (span4[j + 1] - span4[j]) * (span4[j + 1] - span4[j])) * 2f));
				for (int k = ((j != 0) ? 1 : 0); k <= num9; k++)
				{
					int num10 = (int)MathF.Floor(span3[j] + (span3[j + 1] - span3[j]) * (float)k / (float)num9 + 0.5f);
					int num11 = (int)MathF.Floor(span4[j] + (span4[j + 1] - span4[j]) * (float)k / (float)num9 + 0.5f);
					if (num10 != num7 || num11 != num8)
					{
						num7 = num10;
						num8 = num11;
						pixels.Add((num10, num11, ((float)j + (float)k / (float)num9) / 3f));
					}
				}
			}
			alpha = ((num2 > 0.65f) ? Math.Clamp((1f - num2) / 0.35f, 0f, 1f) : 1f);
			return true;
		}

		internal static (float X, float Y)[] PointsDeLEclair(float x, float y, int axeX, int axeY, Random hasard)
		{
			(float, float)[] array = new(float, float)[7];
			for (int i = 0; i <= 6; i++)
			{
				float num = (float)i / 6f - 0.5f;
				float num2 = ((i == 0 || i == 6) ? 0f : (((i % 2 == 1) ? 1f : (-1f)) * (1f + (float)hasard.NextDouble() * 2.2f)));
				array[i] = (x - (float)axeY * num * 26f - (float)axeX * num2, y + (float)axeX * num * 26f - (float)axeY * num2);
			}
			return array;
		}

		internal static float Titube(float sens, long age, long duree)
		{
			if (duree <= 0 || age < 0 || age >= duree)
			{
				return 0f;
			}
			float num = (float)age / (float)duree;
			return sens * (1f - num) * MathF.Sin(num * (float)Math.PI * 3f - 0.8f) * 2f;
		}

		internal static void SurGarde(GuardPacket paquet)
		{
			GenreDeCoupGarde genre = (GenreDeCoupGarde)paquet.Genre;
			if (!Montre || genre == GenreDeCoupGarde.Aucun || paquet.Event == GuardEvent.None || !Globals.Entities.TryGetValue(paquet.EntityId, out Entity value) || value == null)
			{
				return;
			}
			long milliseconds = Timing.Global.Milliseconds;
			Globals.Entities.TryGetValue(paquet.AttackerId, out Entity value2);
			(int X, int Y) tuple = AxeDuCoup(value2, value);
			int item = tuple.X;
			int item2 = tuple.Y;
			(float X, float Y) tuple2 = Contact(value, item, item2);
			float item3 = tuple2.X;
			float item4 = tuple2.Y;
			(string Fichier, float Volume, bool Boucle) tuple3 = GardeRefaite.Son(Options.Instance.Combat.Garde, paquet.Event, genre);
			string item5 = tuple3.Fichier;
			float item6 = tuple3.Volume;
			bool flag = false;
			switch (paquet.Event)
			{
			case GuardEvent.Perfect:
				Eclairs.Add(new Eclair(PointsDeLEclair(item3, item4, item, item2, Hasard), milliseconds));
				Jouer(value, item5, item6, boucle: false);
				break;
			case GuardEvent.Break:
				Lancer(item3, item4, item, item2, in DeBrisee, DeLaBrisee, milliseconds);
				Jouer(value, item5, item6, boucle: false);
				CouperLeJet(value.Id);
				if (value is Player && paquet.DurationMs > 180)
				{
					Titubes[value.Id] = ((item != 0) ? ((float)item) : 1f, milliseconds + 180, milliseconds + paquet.DurationMs);
				}
				break;
			case GuardEvent.Blocked:
				switch (genre)
				{
				case GenreDeCoupGarde.Rayon:
					flag = Presser(value, paquet.AttackerId, item5, item6, milliseconds);
					break;
				case GenreDeCoupGarde.Boule:
					BouleSurLaGarde(value.Id, paquet.AttackerId, item3, item4, item, item2, milliseconds);
					Jouer(value, item5, item6, boucle: false);
					break;
				default:
					Lancer(item3, item4, item, item2, (genre == GenreDeCoupGarde.Lourd) ? DuLourd : DuLeger, DuCoup, milliseconds);
					Jouer(value, item5, item6, boucle: false);
					break;
				}
				break;
			}
			if (paquet.Event != GuardEvent.Blocked || !(value is Player))
			{
				return;
			}
			value.GuardHitAt = 0L;
			if (value != Globals.Me)
			{
				return;
			}
			switch (genre)
			{
			case GenreDeCoupGarde.Lourd:
				CombatFeedback.StartShake(2.5f, 140L, item, item2);
				break;
			case GenreDeCoupGarde.Boule:
				CombatFeedback.StartShake(2f, 130L, item, item2);
				break;
			case GenreDeCoupGarde.Rayon:
				if (flag)
				{
					CombatFeedback.StartShake(2.5f, 180L, item, item2);
				}
				break;
			}
		}

		internal static bool EncaisseEnGarde(Entity cible, long maintenant)
		{
			if (!Montre || !(cible is Player) || (!cible.IsBlocking && (maintenant >= cible.GuardBreakUntil || maintenant - cible.GuardHitAt > 300)))
			{
				return false;
			}
			cible.HitWhiteUntil = 0L;
			cible.HitShakeAt = 0L;
			return true;
		}

		internal static bool NoterLaBoule(Entity? touche, Color clair, Color moyen, Color sombre, long maintenant)
		{
			if (!Montre || touche == null || !touche.IsBlocking)
			{
				return false;
			}
			Tons tons = TonsDeLaBoule(clair, moyen, sombre);
			for (int i = 0; i < BoulesAttendues.Count; i++)
			{
				if (!(BoulesAttendues[i].Corps != touche.Id))
				{
					(Guid, Guid, long) tuple = BoulesAttendues[i];
					BoulesAttendues.RemoveAt(i);
					LancerSurLeCorps(tuple.Item1, tuple.Item2, in DeBoule, tons, maintenant);
					return true;
				}
			}
			BoulesNotees[touche.Id] = (tons, maintenant);
			return true;
		}

		internal static void Talons(Entity corps, NatureDeGlissade nature, float dx, float dy, long dureeMs)
		{
			if (Montre && nature == NatureDeGlissade.Pousse && corps is Player && dx * dx + dy * dy >= 1f)
			{
				System.Numerics.Vector2 origin = corps.Origin;
				Effets.Talons(origin.X, origin.Y + 1f, dx, dy, (int)Math.Clamp(dureeMs, 1L, 2000L), 6000);
			}
		}

		internal static FloatRect Tituber(Entity entite, FloatRect rect, long maintenant)
		{
			if (!Montre || Titubes.Count == 0 || !Titubes.TryGetValue(entite.Id, out (float, long, long) value))
			{
				return rect;
			}
			if (maintenant >= value.Item3 || (maintenant > value.Item2 && !entite.IsGuardStunned))
			{
				Titubes.Remove(entite.Id);
				return rect;
			}
			float num = Titube(value.Item1, maintenant - value.Item2, value.Item3 - value.Item2);
			if (num != 0f)
			{
				return new FloatRect(MathF.Round(rect.X + num), rect.Y, rect.Width, rect.Height);
			}
			return rect;
		}

		internal static void Suivre(long maintenant)
		{
			for (int num = Jets.Count - 1; num >= 0; num--)
			{
				Jet jet = Jets[num];
				if (maintenant <= jet.Fin && Globals.Entities.TryGetValue(jet.Corps, out Entity value) && value != null)
				{
					int num2 = 0;
					while (jet.Prochain <= maintenant && num2++ < 4)
					{
						Globals.Entities.TryGetValue(jet.Attaquant, out Entity value2);
						(int X, int Y) tuple = AxeDuCoup(value2, value);
						int item = tuple.X;
						int item2 = tuple.Y;
						(float X, float Y) tuple2 = Contact(value, item, item2);
						float item3 = tuple2.X;
						float item4 = tuple2.Y;
						List<Etincelle> list = new List<Etincelle>(DuJet.Nombre);
						Jeter(list, item3, item4, item, item2, in DuJet, DuRayon, jet.Cote++, Hasard, jet.Prochain);
						Ajouter(list);
						jet.Prochain += 30L;
					}
					if (jet.Prochain <= maintenant)
					{
						jet.Prochain = maintenant + 30;
					}
				}
				else
				{
					float num3 = Son.Fondu(maintenant, jet.Fin, 150);
					if (jet.Son != null && num3 > 0f)
					{
						jet.Son.Fade = jet.Volume * num3;
					}
					else
					{
						jet.Son?.Stop();
						Jets.RemoveAt(num);
					}
				}
			}
			for (int num4 = BoulesAttendues.Count - 1; num4 >= 0; num4--)
			{
				(Guid, Guid, long) tuple3 = BoulesAttendues[num4];
				if (maintenant - tuple3.Item3 >= 200)
				{
					BoulesAttendues.RemoveAt(num4);
					LancerSurLeCorps(tuple3.Item1, tuple3.Item2, in DeBoule, DeLaBoule, maintenant);
				}
			}
		}

		internal static void Dessiner(long maintenant)
		{
			IGameTexture texture = Graphics.Renderer?.WhitePixel;
			Etincelles.RemoveAll((Etincelle etincelle) => maintenant - etincelle.Depart > etincelle.VieMs);
			Eclairs.RemoveAll((Eclair eclair) => maintenant - eclair.Depart > 110);
			for (int num = 0; num < 2; num++)
			{
				foreach (Etincelle etincelle in Etincelles)
				{
					Etincelle e = etincelle;
					if (!Pixels(in e, maintenant, Tampon, out var alpha))
					{
						continue;
					}
					foreach (var item8 in Tampon)
					{
						int item = item8.X;
						int item2 = item8.Y;
						float item3 = item8.F;
						int num2 = ((!(item3 > 0.55f)) ? 1 : 2);
						if (num == 0)
						{
							Effets.Peindre(texture, UnPixel, new FloatRect(item - 1, item2 - 1, num2 + 2, num2 + 2), Teinte(e.Tons.Cerne, alpha), 0f);
							continue;
						}
						Color ton = ((item3 > 0.78f) ? e.Tons.Tete : ((item3 > 0.4f) ? e.Tons.Milieu : e.Tons.Queue));
						Effets.Peindre(texture, UnPixel, new FloatRect(item, item2, num2, num2), Teinte(ton, alpha), 0f);
					}
				}
			}
			foreach (Eclair eclair in Eclairs)
			{
				long num3 = maintenant - eclair.Depart;
				float alpha2 = ((num3 < 60) ? 1f : Math.Clamp(1f - (float)(num3 - 60) / 50f, 0f, 1f));
				for (int num4 = 0; num4 < 2; num4++)
				{
					for (int num5 = 0; num5 < eclair.Points.Length - 1; num5++)
					{
						(float X, float Y) tuple = eclair.Points[num5];
						float item4 = tuple.X;
						float item5 = tuple.Y;
						(float X, float Y) tuple2 = eclair.Points[num5 + 1];
						float item6 = tuple2.X;
						float item7 = tuple2.Y;
						int num6 = Math.Max(1, (int)MathF.Ceiling(MathF.Sqrt((item6 - item4) * (item6 - item4) + (item7 - item5) * (item7 - item5)) * 2f));
						for (int num7 = 0; num7 <= num6; num7++)
						{
							float num8 = MathF.Floor(item4 + (item6 - item4) * (float)num7 / (float)num6 + 0.5f);
							float num9 = MathF.Floor(item5 + (item7 - item5) * (float)num7 / (float)num6 + 0.5f);
							if (num4 == 0)
							{
								Effets.Peindre(texture, UnPixel, new FloatRect(num8 - 1f, num9 - 1f, 4f, 4f), Teinte(CerneDeLEclair, alpha2), 0f);
							}
							else
							{
								Effets.Peindre(texture, UnPixel, new FloatRect(num8, num9, 2f, 2f), Teinte(BlancDeLEclair, alpha2), 0f);
							}
						}
					}
				}
			}
		}

		internal static void Oublier()
		{
			foreach (Jet jet in Jets)
			{
				jet.Son?.Stop();
			}
			Jets.Clear();
			Etincelles.Clear();
			Eclairs.Clear();
			BoulesNotees.Clear();
			BoulesAttendues.Clear();
			Titubes.Clear();
		}

		private static (int X, int Y) AxeDuCoup(Entity? attaquant, Entity corps)
		{
			if (attaquant != null && attaquant != corps)
			{
				(int, int) tuple = GardeRefaite.Axe(corps.Origin.X - attaquant.Origin.X, corps.Origin.Y - attaquant.Origin.Y);
				(int, int) tuple2 = tuple;
				if (tuple2.Item1 != 0 || tuple2.Item2 != 0)
				{
					return tuple;
				}
			}
			switch (corps.DirectionFacing)
			{
			case Direction.Up:
				return (X: 0, Y: 1);
			case Direction.Down:
				return (X: 0, Y: -1);
			case Direction.Left:
			case Direction.UpLeft:
			case Direction.DownLeft:
				return (X: 1, Y: 0);
			default:
				return (X: -1, Y: 0);
			}
		}

		private static (float X, float Y) Contact(Entity corps, int axeX, int axeY)
		{
			var (num, num2) = Effets.PlaceDe(corps);
			return (X: num - (float)axeX * 10f, Y: num2 - 19f - (float)axeY * 4f);
		}

		private static void Lancer(float x, float y, int axeX, int axeY, in Lot lot, Tons tons, long maintenant)
		{
			List<Etincelle> list = new List<Etincelle>(lot.Nombre);
			Jeter(list, x, y, axeX, axeY, in lot, tons, 0, Hasard, maintenant);
			Ajouter(list);
		}

		private static void LancerSurLeCorps(Guid corpsId, Guid attaquantId, in Lot lot, Tons tons, long maintenant)
		{
			if (Globals.Entities.TryGetValue(corpsId, out Entity value) && value != null)
			{
				Globals.Entities.TryGetValue(attaquantId, out Entity value2);
				var (axeX, axeY) = AxeDuCoup(value2, value);
				var (x, y) = Contact(value, axeX, axeY);
				Lancer(x, y, axeX, axeY, in lot, tons, maintenant);
			}
		}

		private static void Ajouter(List<Etincelle> nouveaux)
		{
			int num = Etincelles.Count + nouveaux.Count - 128;
			if (num > 0)
			{
				Etincelles.RemoveRange(0, Math.Min(num, Etincelles.Count));
			}
			Etincelles.AddRange(nouveaux);
		}

		private static void BouleSurLaGarde(Guid corpsId, Guid attaquantId, float x, float y, int axeX, int axeY, long maintenant)
		{
			if (BoulesNotees.Remove(corpsId, out (Tons, long) value) && maintenant - value.Item2 <= 400)
			{
				Lancer(x, y, axeX, axeY, in DeBoule, value.Item1, maintenant);
			}
			else
			{
				BoulesAttendues.Add((corpsId, attaquantId, maintenant));
			}
		}

		private static bool Presser(Entity corps, Guid attaquantId, string fichier, float volume, long maintenant)
		{
			long fin = maintenant + Math.Max(50, Options.Instance.Beam.TickMs) + 120;
			foreach (Jet jet in Jets)
			{
				if (!(jet.Corps != corps.Id))
				{
					jet.Attaquant = attaquantId;
					jet.Fin = fin;
					if (jet.Son != null)
					{
						jet.Son.Fade = jet.Volume;
					}
					return false;
				}
			}
			Jets.Add(new Jet
			{
				Corps = corps.Id,
				Attaquant = attaquantId,
				Fin = fin,
				Prochain = maintenant,
				Son = Jouer(corps, fichier, volume, boucle: true),
				Volume = volume
			});
			return true;
		}

		private static void CouperLeJet(Guid corps)
		{
			foreach (Jet jet in Jets)
			{
				if (jet.Corps == corps)
				{
					jet.Fin = Math.Min(jet.Fin, Timing.Global.Milliseconds);
				}
			}
		}

		private static MapSound? Jouer(Entity autour, string fichier, float volume, bool boucle)
		{
			if (string.IsNullOrEmpty(fichier) || volume <= 0f)
			{
				return null;
			}
			Temoin?.Invoke(fichier, autour, volume);
			MapSound mapSound = Audio.AddMapSound(fichier, autour.X, autour.Y, autour.MapId, boucle, 0, 12, autour);
			if (mapSound != null)
			{
				mapSound.Fade = volume;
			}
			return mapSound;
		}

		private static Color Teinte(Color ton, float alpha)
		{
			if (!(alpha >= 1f))
			{
				return new Color((int)((float)(int)ton.A * Math.Clamp(alpha, 0f, 1f)), ton.R, ton.G, ton.B);
			}
			return ton;
		}
	}

	internal enum PoseDuCoup : byte
	{
		Planche,
		Course,
		Vol
	}

	internal enum RepliDuCoup : byte
	{
		Aucun,
		SansImage,
		SansTenue
	}

	internal static class ImpactsPredits
	{
		internal const long FenetreMs = 900L;

		private static readonly Dictionary<Guid, Queue<long>> Files = new Dictionary<Guid, Queue<long>>();

		private static readonly Dictionary<Guid, long> CoupsDePied = new Dictionary<Guid, long>();

		internal static void Entrer(Guid cibleId, CoupHeritage coup, long maintenant)
		{
			if (Files.Count > 64)
			{
				Files.Clear();
			}
			if (!Files.TryGetValue(cibleId, out Queue<long> value))
			{
				value = (Files[cibleId] = new Queue<long>(4));
			}
			value.Enqueue(maintenant);
			if (coup == CoupHeritage.CoupDePied)
			{
				if (CoupsDePied.Count > 64)
				{
					CoupsDePied.Clear();
				}
				CoupsDePied[cibleId] = maintenant;
			}
		}

		internal static bool Sortir(Guid cibleId, long maintenant, out long joueA)
		{
			joueA = 0L;
			if (!Files.TryGetValue(cibleId, out Queue<long> value))
			{
				return false;
			}
			while (value.Count > 0 && maintenant - value.Peek() >= 900)
			{
				value.Dequeue();
			}
			bool flag = value.Count > 0;
			if (flag)
			{
				joueA = value.Dequeue();
			}
			if (value.Count == 0)
			{
				Files.Remove(cibleId);
			}
			return flag;
		}

		internal static void Retirer(Guid cibleId, long maintenant)
		{
			Sortir(cibleId, maintenant, out var _);
			CoupsDePied.Remove(cibleId);
		}

		internal static bool CoupDePiedEntendu(Guid cibleId, long maintenant)
		{
			if (CoupsDePied.Remove(cibleId, out var value))
			{
				return maintenant - value < 900;
			}
			return false;
		}

		internal static int EnAttente(Guid cibleId, long maintenant)
		{
			if (!Files.TryGetValue(cibleId, out Queue<long> value))
			{
				return 0;
			}
			int num = 0;
			foreach (long item in value)
			{
				if (maintenant - item < 900)
				{
					num++;
				}
			}
			return num;
		}

		internal static void Vider()
		{
			Files.Clear();
			CoupsDePied.Clear();
		}
	}

	internal static class Kiai
	{
		internal const float SousLesPiedsPx = 2f;

		internal const int Grains = 7;

		internal const float GrainsDevantPx = 14f;

		internal static readonly Coups.Jet JetDesGrains = new Coups.Jet(1.4f, 80f, 220f, 120f, 320);

		internal const float PresDeMoiPx = 160f;

		internal const int ChiffreAttenduMs = 300;

		internal const int PousseeMs = 450;

		internal static readonly float[] ImagesDeLaPoussee = new float[2] { 0.15f, 0.4f };

		internal const int ImageVieMs = 220;

		internal const float ImageForce = 0.5f;

		internal const float ImageEcartPx = 12f;

		internal const int CrisMax = 16;

		private static readonly (Guid Lanceur, long A)[] Cris = new(Guid, long)[16];

		private static int _cris;

		internal static int CrisRetenus => _cris;

		internal static bool Jouer(Entity lanceur, EchangePacket paquet)
		{
			return JouerALHeure(lanceur, paquet, Timing.Global.Milliseconds);
		}

		internal static bool JouerALHeure(Entity lanceur, EchangePacket paquet, long maintenant)
		{
			HeritageOptions heritage = Options.Instance.Combat.Heritage;
			HeritageTechniquesVuOptions techniquesVu = heritage.TechniquesVu;
			if (lanceur == null || paquet == null || !techniquesVu.Actif || !heritage.Allure.Actif || paquet.DureeMs <= 0)
			{
				return false;
			}
			var (num, num2) = AxeDe(SensDuFront(HeritageRegles.SensDuKiai(paquet.Index), lanceur.DirectionFacing));
			if (!Prete((num != 0f) ? "kiai" : "kiai_v"))
			{
				return false;
			}
			System.Numerics.Vector2 origin = lanceur.Origin;
			Front(lanceur, origin.X, origin.Y, num, num2, maintenant);
			CombatAudio.Play("strongcoup.wav", lanceur, 0.05f);
			if (PresDeMoi(lanceur, origin.X, origin.Y))
			{
				CombatFeedback.ImpactHeritage(techniquesVu.KiaiArretMs, (float)techniquesVu.KiaiSecousseDixiemes / 10f, techniquesVu.KiaiSecousseMs, num, num2, maintenant);
			}
			Chiffres.Attendre(lanceur.Id, default(Guid), techniquesVu.ChiffreKiaiPt, Chiffres.Casse, 300, maintenant);
			Noter(lanceur.Id, maintenant);
			return true;
		}

		internal static (float X, float Y) AxeDe(Direction sens)
		{
			var (ecartX, ecartY) = CorpsACorps.Vecteur(sens);
			return Coups.Sens(ecartX, ecartY);
		}

		internal static Direction SensDuFront(Direction dit, Direction regard)
		{
			if (!EstUnSens(dit))
			{
				if (!EstUnSens(regard))
				{
					return Direction.Down;
				}
				return regard;
			}
			return dit;
		}

		private static bool EstUnSens(Direction sens)
		{
			if (sens >= Direction.Up)
			{
				return sens <= Direction.DownLeft;
			}
			return false;
		}

		internal static CoucheDEffet CoucheDuFront(float ux, float uy)
		{
			if (ux != 0f || !(uy > 0f))
			{
				return CoucheDEffet.Sol;
			}
			return CoucheDEffet.DevantUnCorps;
		}

		private static bool Prete(string nom)
		{
			if (Bandes.Trouver(nom, out var bande))
			{
				return Bandes.Planche(in bande) != null;
			}
			return false;
		}

		private static void Front(Entity lanceur, float x, float y, float ux, float uy, long maintenant)
		{
			bool flag = ux != 0f;
			Effets.Bande(flag ? "kiai_sol" : "kiai_sol_v", x, y, ux, uy, CoucheDEffet.Sol, 1f, Guid.Empty, 0, maintenant);
			Effets.Bande(flag ? "kiai" : "kiai_v", x, y + 2f, ux, uy, CoucheDuFront(ux, uy), 1f, lanceur.Id, 0, maintenant);
			Particules.Lancer(x + ux * 14f, y, 7, MathF.Atan2(uy, ux), JetDesGrains.Ouvert, JetDesGrains.VMin, JetDesGrains.VMax, JetDesGrains.Gravite, JetDesGrains.VieMs, Coups.Poussiere(x, y), JetDesGrains.Tirets, float.NaN, 0, null, Globals.Random, maintenant);
		}

		private static bool PresDeMoi(Entity lanceur, float x, float y)
		{
			Player me = Globals.Me;
			if (me == null)
			{
				return false;
			}
			if (lanceur == me)
			{
				return true;
			}
			float num = me.WorldPos.X - x;
			float num2 = me.WorldPos.Y - y;
			return num * num + num2 * num2 < 25600f;
		}

		private static bool Court(long depuis, long maintenant)
		{
			if (maintenant >= depuis)
			{
				return maintenant - depuis < 450;
			}
			return false;
		}

		private static void Noter(Guid lanceur, long maintenant)
		{
			int num = 0;
			for (int i = 0; i < _cris; i++)
			{
				if (!(Cris[i].Lanceur == lanceur) && Court(Cris[i].A, maintenant))
				{
					Cris[num++] = Cris[i];
				}
			}
			if (num == 16)
			{
				Array.Copy(Cris, 1, Cris, 0, --num);
			}
			Cris[num++] = (Lanceur: lanceur, A: maintenant);
			if (num < _cris)
			{
				Array.Clear(Cris, num, _cris - num);
			}
			_cris = num;
		}

		internal static bool Pousse(Guid lanceur, long maintenant)
		{
			if (lanceur == Guid.Empty)
			{
				return false;
			}
			for (int i = 0; i < _cris; i++)
			{
				if (Cris[i].Lanceur == lanceur && Court(Cris[i].A, maintenant))
				{
					return true;
				}
			}
			return false;
		}

		internal static bool Blesse(Guid lanceur, long maintenant)
		{
			if (lanceur == Guid.Empty)
			{
				return false;
			}
			for (int i = 0; i < _cris; i++)
			{
				if (Cris[i].Lanceur == lanceur && maintenant >= Cris[i].A && maintenant - Cris[i].A < 300)
				{
					return true;
				}
			}
			return false;
		}

		internal static bool SurPoussee(Entity entite, Dash glissade, int dureeMs, long maintenant)
		{
			if (!Pousse(glissade.AuteurId, maintenant))
			{
				return false;
			}
			float[] imagesDeLaPoussee = ImagesDeLaPoussee;
			foreach (float num in imagesDeLaPoussee)
			{
				Coups.ImagePlusTard(entite.Id, (int)((float)dureeMs * num), 220, 0.5f, Mouvement.CremeDuLancer, 12f, maintenant);
			}
			return true;
		}

		internal static void Oublier()
		{
			Array.Clear(Cris, 0, _cris);
			_cris = 0;
		}
	}

	internal static class Lezardes
	{
		private struct Posee
		{
			public float X;

			public float Y;

			public int Nombre;

			public int Rayon;

			public long Nee;

			public IGameTexture? Texture;

			public int Gauche;

			public int Haut;
		}

		internal const int PoseesMax = 8;

		internal const int VieMs = 60000;

		internal const uint Graine = 5u;

		internal const double DepartPx = 12.0;

		internal const double DepartHaut = 1.5;

		internal const int MargePx = 15;

		internal const int LongueurMinPx = 4;

		internal const int LongueurMaxPx = 192;

		internal const int NombreMax = 24;

		internal static readonly Color Sombre = new Color(255, 51, 25, 14);

		internal static readonly Color Levre = new Color(255, 191, 142, 122);

		private static readonly Color Plein = new Color(255, 255, 255, 255);

		private static readonly Posee[] LesPosees = new Posee[8];

		private static int _nombre;

		private static long _fabriquees;

		internal static int Nombre => _nombre;

		private static int Arrondi(double valeur)
		{
			return (int)Math.Floor(valeur + 0.5);
		}

		internal static uint GraineDe(float x, float y)
		{
			return (uint)(39595 + (long)(int)x * 13L + (int)y);
		}

		internal static Sol.Toile Peindre(int nombre, int rayon, uint graine)
		{
			int num = 2 * rayon + 30;
			byte[] rgba = new byte[num * num * 4];
			double num2 = (double)num / 2.0;
			double num3 = (double)num / 2.0;
			Sol.Alea alea = new Sol.Alea(graine);
			List<(double, double)[]> list = new List<(double, double)[]>(nombre * 2);
			for (int i = 0; i < nombre; i++)
			{
				double num4 = (double)i / (double)nombre * Math.PI * 2.0;
				double num5 = num2 + Math.Cos(num4) * 12.0;
				double num6 = num3 + Math.Sin(num4) * 12.0 * 1.5;
				double num7 = (double)rayon * (0.5 + alea.Suivant() * 0.5);
				int num8 = 4 + (int)(alea.Suivant() * 2.0);
				(double, double)[] array = new(double, double)[num8 + 1];
				array[0] = (num5, num6);
				for (int j = 0; j < num8; j++)
				{
					double num9 = (alea.Suivant() - 0.5) * 0.3;
					double num10 = (double)((j % 2 == 1) ? 1 : (-1)) * (0.3 + alea.Suivant() * 0.5);
					double num11 = num4 + num9 + num10;
					double num12 = num7 / (double)num8 * (0.7 + alea.Suivant() * 0.6);
					num5 += Math.Cos(num11) * num12;
					num6 += Math.Sin(num11) * num12;
					array[j + 1] = (num5, num6);
					if (j == 1 && alea.Suivant() < 0.55)
					{
						int num13 = ((alea.Suivant() < 0.5) ? 1 : (-1));
						double num14 = num4 + (double)num13 * (0.8 + alea.Suivant() * 0.4);
						list.Add(new(double, double)[2]
						{
							(num5, num6),
							(num5 + Math.Cos(num14) * num7 * 0.3, num6 + Math.Sin(num14) * num7 * 0.3)
						});
					}
				}
				list.Add(array);
			}
			foreach ((double, double)[] item in list)
			{
				Tracer(rgba, num, item, Levre, 1, 2, 1);
			}
			foreach ((double, double)[] item2 in list)
			{
				Tracer(rgba, num, item2, Sombre, 2, 0, 0);
			}
			return new Sol.Toile(rgba, num, num, num2, num3);
		}

		private static void Tracer(byte[] rgba, int cote, (double X, double Y)[] points, Color ton, int epais, int dx, int dy)
		{
			for (int i = 1; i < points.Length; i++)
			{
				(double X, double Y) tuple = points[i - 1];
				double item = tuple.X;
				double item2 = tuple.Y;
				(double X, double Y) tuple2 = points[i];
				double item3 = tuple2.X;
				double item4 = tuple2.Y;
				int num = Math.Max(1, (int)Math.Ceiling(Math.Max(Math.Abs(item3 - item), Math.Abs(item4 - item2))));
				for (int j = 0; j <= num; j++)
				{
					double num2 = (double)j / (double)num;
					Remplir(rgba, cote, Arrondi(item + (item3 - item) * num2) + dx, Arrondi(item2 + (item4 - item2) * num2) + dy, epais, ton);
				}
			}
		}

		private static void Remplir(byte[] rgba, int cote, int x, int y, int epais, Color ton)
		{
			int num = Math.Min(cote, x + epais);
			int num2 = Math.Min(cote, y + epais);
			for (int i = Math.Max(0, y); i < num2; i++)
			{
				for (int j = Math.Max(0, x); j < num; j++)
				{
					int num3 = (i * cote + j) * 4;
					rgba[num3] = ton.R;
					rgba[num3 + 1] = ton.G;
					rgba[num3 + 2] = ton.B;
					rgba[num3 + 3] = byte.MaxValue;
				}
			}
		}

		internal static bool Poser(float x, float y, int nombre, int px, long maintenant)
		{
			if (nombre <= 0 || px < 4 || !float.IsFinite(x) || !float.IsFinite(y))
			{
				return false;
			}
			if (_nombre == 8)
			{
				Liberer(ref LesPosees[0]);
				Array.Copy(LesPosees, 1, LesPosees, 0, _nombre - 1);
				LesPosees[--_nombre] = default(Posee);
			}
			LesPosees[_nombre++] = new Posee
			{
				X = x,
				Y = y,
				Nombre = Math.Min(nombre, 24),
				Rayon = Math.Min(px, 192),
				Nee = maintenant
			};
			return true;
		}

		internal static void Oublier()
		{
			for (int i = 0; i < _nombre; i++)
			{
				Liberer(ref LesPosees[i]);
			}
			Array.Clear(LesPosees, 0, _nombre);
			_nombre = 0;
		}

		private static void Liberer(ref Posee posee)
		{
			posee.Texture?.Dispose();
			posee.Texture = null;
		}

		private static long Age(in Posee posee, long maintenant)
		{
			return Math.Max(0L, maintenant - posee.Nee);
		}

		internal static void Dessiner(long maintenant)
		{
			int num = 0;
			bool flag = false;
			for (int i = 0; i < _nombre; i++)
			{
				ref Posee reference = ref LesPosees[i];
				long num2 = Age(in reference, maintenant);
				if (num2 >= 60000)
				{
					Liberer(ref reference);
					continue;
				}
				if (reference.Texture == null && !flag)
				{
					flag = true;
					if (!Fabriquer(ref reference))
					{
						continue;
					}
				}
				IGameTexture texture = reference.Texture;
				if (texture != null)
				{
					long num3 = 60000 - num2;
					Effets.Peindre(texture, new FloatRect(0f, 0f, texture.Width, texture.Height), new FloatRect(reference.Gauche, reference.Haut, texture.Width, texture.Height), (num3 >= 600) ? Plein : new Color((int)(255 * num3 / 600), 255, 255, 255), 0f);
				}
				if (num != i)
				{
					LesPosees[num] = reference;
				}
				num++;
			}
			Array.Clear(LesPosees, num, _nombre - num);
			_nombre = num;
		}

		private static bool Fabriquer(ref Posee posee)
		{
			Sol.Toile toile = Peindre(posee.Nombre, posee.Rayon, GraineDe(posee.X, posee.Y));
			string text = $"heritage/lezardes/{++_fabriquees}";
			posee.Texture = ((Sol.Fabrique != null) ? Sol.Fabrique(text, toile.Largeur, toile.Hauteur, toile.Rgba) : Graphics.Renderer?.CreateTextureFromPixels(text, toile.Largeur, toile.Hauteur, toile.Rgba));
			if (posee.Texture == null)
			{
				return false;
			}
			posee.Gauche = Arrondi((double)posee.X - toile.OrigineX);
			posee.Haut = Arrondi((double)posee.Y - toile.OrigineY);
			return true;
		}
	}

	internal static class Lien
	{
		internal enum Decision : byte
		{
			AFroid,
			Gardee,
			Liee
		}

		internal const int PoseMs = 400;

		internal const int AbsenceMaxMs = 500;

		internal const float HauteurDuCentre = 0.65f;

		internal const float EcartMinPx = 1f;

		internal const FormeDeLancer FormesConsommees = FormeDeLancer.Long | FormeDeLancer.Critique | FormeDeLancer.Cueilli;

		private static int _case = -1;

		private static long _gardeeVueA;

		private static bool _pourUnCoupQuiAttend;

		private static bool _depart;

		private static bool _lie;

		private static Guid _cible;

		private static long _gesteDebut;

		private static Guid _coupCible;

		private static long _contactA;

		private static bool _contactVu;

		private static bool _contactPorte;

		private static Dash? _lancerVu;

		private static FormeDeLancer _forme;

		private static Guid _formeSur;

		private static long _formeA;

		private static bool _rayon;

		private static Guid _rayonCible;

		internal static bool EnDepart => _lie;

		internal static Guid CibleDuRayon
		{
			get
			{
				if (!_rayon)
				{
					return Guid.Empty;
				}
				return _rayonCible;
			}
		}

		internal static bool RayonLie
		{
			get
			{
				if (_rayon)
				{
					Player me = Globals.Me;
					if (me != null && KiBeam.PhaseOf(me) != BeamPhase.None)
					{
						return Presente(_rayonCible);
					}
				}
				return false;
			}
		}

		internal static long FormeConsommeeA => _formeA;

		internal static int CaseGardee => _case;

		internal static bool GardeePourUnCoupQuiAttend
		{
			get
			{
				if (_case >= 0)
				{
					return _pourUnCoupQuiAttend;
				}
				return false;
			}
		}

		internal static Decision Decider(bool gardee, bool gesteEnCours, bool glisse, long depuisLeContact, int departMs, int memoireMs, bool aPorte)
		{
			if (gesteEnCours && depuisLeContact < departMs)
			{
				return Decision.Gardee;
			}
			if (glisse && (gesteEnCours || gardee))
			{
				return Decision.Gardee;
			}
			bool flag = gardee || (depuisLeContact >= 0 && depuisLeContact <= memoireMs);
			if (!(aPorte && flag))
			{
				return Decision.AFroid;
			}
			return Decision.Liee;
		}

		internal static Decision Decider(bool pourUnCoupQuiAttend, bool ceCoupAttend, bool gardee, bool gesteEnCours, bool glisse, long depuisLeContact, int departMs, int memoireMs, bool aPorte)
		{
			if (pourUnCoupQuiAttend)
			{
				if (!ceCoupAttend)
				{
					return Decision.AFroid;
				}
				return Decision.Gardee;
			}
			return Decider(gardee, gesteEnCours, glisse, depuisLeContact, departMs, memoireMs, aPorte);
		}

		internal static int DepartApresLeContact(CoupHeritage coup, int departMs)
		{
			CoupHeritageOptions coupHeritageOptions = HeritageRegles.Table(coup);
			return HeritageRegles.DepartDuLien(coupHeritageOptions.TenueMs, coupHeritageOptions.ArretMs, departMs);
		}

		private static bool LImpactAttend(Player moi)
		{
			if (_impactEnAttente)
			{
				return LImpactEstCeluiDuGeste(moi);
			}
			return false;
		}

		private static void SuivreLeCoup(Player moi, long maintenant)
		{
			CoupHeritage coupEnCours = moi.CoupEnCours;
			Guid cibleDuCoupEnCours = moi.CibleDuCoupEnCours;
			if (coupEnCours != CoupHeritage.Aucun && !(cibleDuCoupEnCours == Guid.Empty))
			{
				long num = moi.AttackTimer - moi.DureeDuGeste();
				if (num != _gesteDebut || cibleDuCoupEnCours != _coupCible)
				{
					_gesteDebut = num;
					_coupCible = cibleDuCoupEnCours;
					_contactA = num + HeureDeLImpact(coupEnCours, moi.AvanceDuCoupEnCours);
					_contactVu = false;
					_contactPorte = false;
					_pourUnCoupQuiAttend = false;
				}
				if (!_contactVu && !LImpactAttend(moi))
				{
					_contactVu = true;
					_contactA = Math.Min(_contactA, maintenant);
					_contactPorte = ATouche(moi, cibleDuCoupEnCours);
				}
			}
		}

		private static bool ATouche(Player moi, Guid cibleId)
		{
			if (!EtatSubi(moi) && Globals.Entities.TryGetValue(cibleId, out Entity value) && value != null && !value.IsDying && !(value is Resource) && !value.IsBlocking)
			{
				if (value is Player en)
				{
					return !moi.IsAllyOf(en);
				}
				return true;
			}
			return false;
		}

		private static bool Presente(Guid id)
		{
			if (id != Guid.Empty && Globals.Entities.TryGetValue(id, out Entity value))
			{
				if (value != null && !value.IsDying)
				{
					return !value.IsDisposed;
				}
				return false;
			}
			return false;
		}

		private static bool APorte(Player moi)
		{
			if (_contactVu && _contactPorte && !moi.DernierCoupRefuseHeritage)
			{
				return Presente(_coupCible);
			}
			return false;
		}

		private static bool SeLie(Player moi, int caseDuLivre)
		{
			if (caseDuLivre >= 0 && caseDuLivre < moi.Spells.Length)
			{
				Spell spell = moi.Spells[caseDuLivre];
				if (spell != null && DatabaseObject<SpellDescriptor>.TryGet(spell.Id, out var databaseObject))
				{
					if (!Options.Instance.Beam.IsBeamSpell(databaseObject.Id))
					{
						if (databaseObject.SpellType == SpellType.CombatSpell)
						{
							return !databaseObject.Combat.Friendly;
						}
						return false;
					}
					return true;
				}
			}
			return false;
		}

		private static Decision Juger(Player moi, bool gardee, long maintenant)
		{
			HeritageTechniquesOptions techniques = Options.Instance.Combat.Heritage.Techniques;
			CoupHeritage coupEnCours = moi.CoupEnCours;
			bool flag = coupEnCours != CoupHeritage.Aucun && moi.CibleDuCoupEnCours != Guid.Empty;
			bool flag2 = moi.UnCoupAttendHeritage(maintenant);
			if (!gardee)
			{
				_pourUnCoupQuiAttend = flag2;
			}
			return Decider(_pourUnCoupQuiAttend, flag2, gardee, flag, moi.Dashing != null, maintenant - _contactA, flag ? DepartApresLeContact(coupEnCours, techniques.LienDepartMs) : 0, techniques.LienMemoireMs, APorte(moi));
		}

		private static void Lier(bool lie)
		{
			_lie = lie;
			_cible = (lie ? _coupCible : Guid.Empty);
		}

		internal static bool Garder(Player moi, int caseDuLivre)
		{
			return GarderA(moi, caseDuLivre, Timing.Global.Milliseconds);
		}

		internal static bool GarderA(Player moi, int caseDuLivre, long maintenant)
		{
			if (_depart)
			{
				return false;
			}
			Lier(lie: false);
			if (!Options.Instance.Combat.Heritage.Techniques.Lien || moi != Globals.Me || !SeLie(moi, caseDuLivre))
			{
				_case = -1;
				_pourUnCoupQuiAttend = false;
				return false;
			}
			bool gardee = _case == caseDuLivre && maintenant - _gardeeVueA <= 500;
			SuivreLeCoup(moi, maintenant);
			Decision decision = Juger(moi, gardee, maintenant);
			if (decision == Decision.Gardee)
			{
				_case = caseDuLivre;
				_gardeeVueA = maintenant;
				return true;
			}
			_case = -1;
			_pourUnCoupQuiAttend = false;
			Lier(decision == Decision.Liee);
			return false;
		}

		internal static void Suivre(Player moi)
		{
			SuivreA(moi, Timing.Global.Milliseconds);
		}

		internal static void SuivreA(Player moi, long maintenant)
		{
			if (_rayon && KiBeam.PhaseOf(moi) == BeamPhase.None)
			{
				_rayon = false;
				_rayonCible = Guid.Empty;
			}
			Lier(lie: false);
			HeritageTechniquesOptions techniques = Options.Instance.Combat.Heritage.Techniques;
			if (!techniques.Lien)
			{
				_case = -1;
				_pourUnCoupQuiAttend = false;
				return;
			}
			SuivreLeCoup(moi, maintenant);
			if (techniques.Moments)
			{
				SuivreLeLancer(moi, maintenant);
			}
			if (_case < 0)
			{
				_pourUnCoupQuiAttend = false;
				return;
			}
			if (maintenant - _gardeeVueA > 500)
			{
				_case = -1;
				_pourUnCoupQuiAttend = false;
				return;
			}
			_gardeeVueA = maintenant;
			Decision decision = Juger(moi, gardee: true, maintenant);
			if (decision != Decision.Gardee)
			{
				Partir(moi, decision == Decision.Liee);
			}
		}

		private static void Partir(Player moi, bool lie)
		{
			int index = _case;
			_case = -1;
			_pourUnCoupQuiAttend = false;
			_depart = true;
			Lier(lie);
			try
			{
				moi.TryUseSpell(index);
			}
			finally
			{
				_depart = false;
				Lier(lie: false);
			}
		}

		internal static bool Tourner(Player moi)
		{
			if (!_lie)
			{
				return false;
			}
			if (Globals.Entities.TryGetValue(_cible, out Entity value) && value != null && moi.PositionMonde(out var mondeX, out var mondeY) && value.PositionMonde(out var mondeX2, out var mondeY2))
			{
				Direction direction = CorpsACorps.DirectionVers(mondeX2 - mondeX, mondeY2 - mondeY);
				if (direction != Direction.None && direction != moi.DirectionFacing)
				{
					moi.DirectionFacing = direction;
					PacketSender.SendDirection(direction);
				}
			}
			moi.PlaySpellPose(400L);
			moi.GarderLeRangHeritage();
			return true;
		}

		internal static Guid CibleDe(Guid visee)
		{
			if (!_lie)
			{
				return visee;
			}
			return _cible;
		}

		internal static byte Octet()
		{
			if (!Options.Instance.Combat.Heritage.Techniques.Lien)
			{
				return 0;
			}
			return HeritageRegles.OctetDeLien(_lie, DirectionTenue());
		}

		internal static Direction DirectionTenue()
		{
			bool[] tenues = Appuis.Tenues;
			return DirectionTenue(tenues[0], tenues[1], tenues[2], tenues[3], Options.Instance.Map.EnableDiagonalMovement);
		}

		internal static Direction DirectionTenue(bool haut, bool bas, bool gauche, bool droite, bool diagonales)
		{
			int num = (droite ? 1 : 0) - (gauche ? 1 : 0);
			int num2 = (bas ? 1 : 0) - (haut ? 1 : 0);
			if (num2 == 0)
			{
				if (num >= 0)
				{
					if (num <= 0)
					{
						return Direction.None;
					}
					return Direction.Right;
				}
				return Direction.Left;
			}
			if (num == 0 || !diagonales)
			{
				if (num2 >= 0)
				{
					return Direction.Down;
				}
				return Direction.Up;
			}
			if (num2 >= 0)
			{
				if (num >= 0)
				{
					return Direction.DownRight;
				}
				return Direction.DownLeft;
			}
			if (num >= 0)
			{
				return Direction.UpRight;
			}
			return Direction.UpLeft;
		}

		internal static Guid CibleVisee(Player moi, bool debut)
		{
			if (debut)
			{
				_rayon = _lie && _cible != Guid.Empty;
				_rayonCible = (_rayon ? _cible : Guid.Empty);
				if (_rayon)
				{
					moi.GarderLeRangHeritage();
				}
			}
			if (!_rayon)
			{
				return Guid.Empty;
			}
			return _rayonCible;
		}

		internal static float? AngleVers(FloatRect de, FloatRect vers)
		{
			float num = vers.X + vers.Width / 2f - (de.X + de.Width / 2f);
			float num2 = vers.Y + vers.Height * 0.65f - (de.Y + de.Height * 0.65f);
			if (num * num + num2 * num2 < 1f)
			{
				return null;
			}
			return MathF.Atan2(num2, num) * 180f / (float)Math.PI;
		}

		private static void SuivreLeLancer(Player moi, long maintenant)
		{
			if (!(ReferenceId == Guid.Empty) && Globals.Entities.TryGetValue(ReferenceId, out Entity value))
			{
				Dash dash = value?.Dashing;
				if (dash != null && dash != _lancerVu && Moment.EstUnLancerDe(moi.Id, dash))
				{
					_lancerVu = dash;
					_forme = dash.FormeHeritage & (FormeDeLancer.Long | FormeDeLancer.Critique | FormeDeLancer.Cueilli);
					_formeSur = value.Id;
					_formeA = maintenant;
				}
			}
		}

		internal static FormeDeLancer FormeConsommee(Guid cibleId, long maintenant)
		{
			if (!(cibleId != Guid.Empty) || !(cibleId == _formeSur) || maintenant - _formeA >= Options.Instance.Combat.Heritage.Techniques.MomentAcquisMs)
			{
				return FormeDeLancer.Simple;
			}
			return _forme;
		}

		internal static void Oublier()
		{
			_case = -1;
			_gardeeVueA = 0L;
			_pourUnCoupQuiAttend = false;
			_depart = false;
			Lier(lie: false);
			_gesteDebut = 0L;
			_coupCible = Guid.Empty;
			_contactA = 0L;
			_contactVu = false;
			_contactPorte = false;
			_lancerVu = null;
			_forme = FormeDeLancer.Simple;
			_formeSur = Guid.Empty;
			_formeA = 0L;
			_rayon = false;
			_rayonCible = Guid.Empty;
		}
	}

	internal static class Lisere
	{
		internal const int PlafondPx = 6;

		internal const byte OpacitePleine = byte.MaxValue;

		internal const byte OpaciteBasse = 150;

		internal const int TrancheDuCorps = 2;

		internal const int LignesMin = 4;

		internal static readonly Color Jaune = new Color(255, 248, 248, 72);

		internal static readonly Color TeinteDeFlanc = new Color(255, 255, 106, 213);

		internal static readonly Color TeinteEbranle = new Color(255, 160, 112, 255);

		internal static readonly Color Bleu = new Color(255, 127, 208, 255);

		internal const int EcartMin = 80;

		private static readonly Color?[] Pleines = Teintes(byte.MaxValue);

		private static readonly Color?[] Basses = Teintes(150);

		internal static readonly uint EtatsMontres = (from etat in Enum.GetValues<EtatHeritage>()
			where Pleines[(uint)etat] != null
			select etat).Aggregate(0u, (uint filtre, EtatHeritage etat) => filtre | EtatsVus.Bit(etat));

		internal static Func<IGameTexture, FloatRect, int, IGameTexture?>? Masque;

		internal static int Ecart(Color a, Color b)
		{
			return Math.Max(Math.Abs(a.R - b.R), Math.Max(Math.Abs(a.G - b.G), Math.Abs(a.B - b.B)));
		}

		private static Color?[] Teintes(byte opacite)
		{
			Color[] array = new Color[Enum.GetValues<EtatHeritage>().Max((EtatHeritage etat) => (int)etat) + 1];
			array[1] = new Color(opacite, Jaune.R, Jaune.G, Jaune.B);
			array[2] = new Color(opacite, TeinteDeFlanc.R, TeinteDeFlanc.G, TeinteDeFlanc.B);
			array[3] = new Color(opacite, TeinteEbranle.R, TeinteEbranle.G, TeinteEbranle.B);
			array[4] = new Color(opacite, Bleu.R, Bleu.G, Bleu.B);
			return array;
		}

		internal static Color? Couleur(EtatHeritage etat, bool plein = true)
		{
			if ((int)etat >= Pleines.Length)
			{
				return null;
			}
			return (plein ? Pleines : Basses)[(uint)etat];
		}

		internal static bool Plein(long age, int periodeMs)
		{
			if (periodeMs >= 2)
			{
				return age % periodeMs < periodeMs / 2;
			}
			return true;
		}

		internal static int Epaisseur(HeritageAllureOptions allure)
		{
			if (!allure.Actif)
			{
				return 0;
			}
			return Math.Clamp(allure.LiserePx, 0, 6);
		}

		internal static EtatHeritage Montre(Entity entite, long maintenant, out long depuis)
		{
			long jusqua;
			Guid par;
			EtatHeritage etatHeritage = EtatsVus.LePlusRecent(entite.Id, maintenant, EtatsMontres, out depuis, out jusqua, out par);
			if (etatHeritage == EtatHeritage.Aucun)
			{
				return EtatHeritage.Aucun;
			}
			if (entite.IsDying)
			{
				return EtatHeritage.Aucun;
			}
			if (etatHeritage == EtatHeritage.Plante)
			{
				Dash dashing = entite.Dashing;
				if (dashing != null && dashing.Nature == NatureDeGlissade.Projection)
				{
					return EtatHeritage.Aucun;
				}
			}
			return etatHeritage;
		}

		internal static EtatHeritage Porte(Entity entite, long maintenant, out long depuis)
		{
			EtatHeritage etatHeritage = Montre(entite, maintenant, out depuis);
			if (etatHeritage != EtatHeritage.Aucun)
			{
				return etatHeritage;
			}
			depuis = 0L;
			if (!entite.IsDying)
			{
				Dash dashing = entite.Dashing;
				if (dashing != null && Moment.SeMontre && Moment.EstLance(entite))
				{
					depuis = dashing.StartedAt;
					return EtatHeritage.Plante;
				}
			}
			return EtatHeritage.Aucun;
		}

		internal static void Dessiner(Entity entite, FloatRect destination, IGameTexture? texture, FloatRect source)
		{
			if (EtatsVus.Entites != 0 || entite.Dashing != null)
			{
				Dessiner(entite, destination, texture, source, Timing.Global.Milliseconds);
			}
		}

		internal static bool Dessiner(Entity entite, FloatRect destination, IGameTexture? texture, FloatRect source, long maintenant)
		{
			HeritageAllureOptions allure = Options.Instance.Combat.Heritage.Allure;
			int num = Epaisseur(allure);
			if (num <= 0 || texture == null || source.Width <= 0f || source.Height <= 0f)
			{
				return false;
			}
			long depuis;
			EtatHeritage etat = Porte(entite, maintenant, out depuis);
			Color color = Couleur(etat, Plein(maintenant - depuis, allure.LiserePeriodeMs));
			if ((object)color == null)
			{
				return false;
			}
			IGameTexture gameTexture = ((Masque != null) ? Masque(texture, source, num) : KiCharge.MaskOf(texture, source, num));
			if (gameTexture == null)
			{
				return false;
			}
			var (penche, tremble) = PoseDuCorps(entite, maintenant);
			Tracer(gameTexture, destination, source, num, penche, tremble, color);
			return true;
		}

		internal static (float Penche, float Tremble) PoseDuCorps(Entity entite, long maintenant)
		{
			var (num, num2) = Knockback.Pose(entite);
			var (num3, num4) = Echanges.Pose(entite);
			return (Penche: num + num3 + Corps.Penche(entite, maintenant), Tremble: (num2 != 0f) ? num2 : num4);
		}

		internal static float Decalage(int ligne, int px, int lignes, float penche)
		{
			int num = Math.Clamp(ligne - px, 0, Math.Max(0, lignes - 1));
			int num2 = num - num % 2;
			return MathF.Round(penche * (1f - (float)num2 / (float)Math.Max(1, lignes)));
		}

		internal static void Tracer(IGameTexture silhouette, FloatRect destination, FloatRect source, int px, float penche, float tremble, Color couleur)
		{
			float num = destination.Width / source.Width;
			float num2 = destination.Height / source.Height;
			float num3 = destination.X - (float)px * num + tremble;
			float w = destination.Width + (float)(2 * px) * num;
			int num4 = (int)source.Height;
			if (num4 < 4)
			{
				penche = 0f;
			}
			int num5 = num4 + 2 * px;
			int num6 = 0;
			float num7 = Decalage(0, px, num4, penche);
			for (int i = 1; i <= num5; i++)
			{
				float num8 = ((i < num5) ? Decalage(i, px, num4, penche) : float.NaN);
				if (num8 != num7)
				{
					Effets.Peindre(silhouette, new FloatRect(0f, num6, silhouette.Width, i - num6), new FloatRect(num3 + num7, destination.Y + (float)(num6 - px) * num2, w, (float)(i - num6) * num2), couleur, 0f);
					num6 = i;
					num7 = num8;
				}
			}
		}
	}

	internal static class Marques
	{
		internal sealed class Marque
		{
			public Guid Carte;

			public float X0;

			public float Y0;

			public float X1;

			public float Y1;

			public MatiereDArret Matiere;

			public long Debut;

			public long Fin;

			public float SensX;

			public float SensY;

			public bool AgeeALArrivee;

			public bool Tranchee;

			public IGameTexture? Silhouette;

			public float SilhouetteLargeur;

			public float SilhouetteHauteur;

			public IGameTexture? Peinte;

			public bool Vide;

			public bool Cachee;

			public int EnfonceX;

			public int EnfonceY;

			public IGameTexture? Creuse;

			public Guid Ressource;

			public long ProchaineRecherche;

			public IGameTexture? PeintePour;

			public FloatRect PeinteSource;

			public int Essais;

			public bool Finie;

			public bool DeProfil => MathF.Abs(SensX) > MathF.Abs(SensY);
		}

		private sealed class Alea(uint graine)
		{
			private uint _etat = graine;

			public double Suivant()
			{
				graine = graine * 1664525 + 1013904223;
				return (double)graine / 4294967296.0;
			}
		}

		internal sealed record Grille(int X, int Y, int Largeur, int Hauteur, bool[] Pixels)
		{
			public bool En(int x, int y)
			{
				if (x >= X && y >= Y && x < X + Largeur && y < Y + Hauteur)
				{
					return Pixels[(y - Y) * Largeur + x - X];
				}
				return false;
			}
		}

		internal const int GardeesMax = 64;

		internal const int EffacementMs = 20000;

		private const float MemePx = 0.5f;

		private const float SensMinPx = 0.5f;

		internal const float CorpsPresPx = 24f;

		internal const int AttenteDuCorpsMs = 600;

		internal const float Agrandi = 1.2f;

		internal const float ProfilPx = 22f;

		internal const float AretePx = 2f;

		internal const int Fond = 3348750;

		internal const int Ombre = 5975065;

		internal const int Clair = 12553850;

		internal const float CentreAuDessusDesPiedsPx = 18f;

		internal const int LezardeRayonPx = 26;

		internal const int LezardesCotePx = 82;

		internal const int LezardesMin = 5;

		internal const int LezardesMax = 6;

		internal const double EventailDemiRad = 1.9477874452256718;

		internal const double DemiPlanRad = Math.PI * 9.0 / 20.0;

		private const double LezardeDepartPx = 12.0;

		private const double LezardeDepartHaut = 1.5;

		internal const int OvaleLargePx = 18;

		internal const int OvaleHautPx = 26;

		private const int SelDesLezardes = 15838;

		internal const byte Plein = 235;

		internal const int FenteAGauchePx = 6;

		internal const int FenteADroitePx = 8;

		private const int FenteBandePx = 2;

		internal const int FenteMinPx = 8;

		internal const int FenteLevre = 14932697;

		internal const int FenteSombre = 2827296;

		internal const int FenteNoyau = 1380366;

		private const int SelDeLaFente = 100;

		internal const float RocherPousseePx = 6f;

		internal const float RocherPresPx = 12f;

		internal const int RechercheMs = 500;

		internal const int LectureEssais = 30;

		private const int ImageMaxPx = 512;

		private static readonly object Verrou = new object();

		private static readonly List<Marque> Toutes = new List<Marque>();

		private static readonly Dictionary<Guid, Marque> Fendues = new Dictionary<Guid, Marque>();

		private static long _fabriquees;

		internal const int EnfoncementMaxPx = 8;

		internal const float EssentielSurLaRoche = 2f / 3f;

		internal static int Gardees
		{
			get
			{
				lock (Verrou)
				{
					return Toutes.Count;
				}
			}
		}

		internal static IReadOnlyList<Marque> Liste
		{
			get
			{
				lock (Verrou)
				{
					return Toutes.ToArray();
				}
			}
		}

		internal static bool EstFendue(Guid ressource)
		{
			lock (Verrou)
			{
				return Fendues.ContainsKey(ressource);
			}
		}

		internal static void Oublier()
		{
			lock (Verrou)
			{
				foreach (Marque toute in Toutes)
				{
					LibererLImage(toute);
				}
				Toutes.Clear();
				Fendues.Clear();
			}
		}

		internal static bool LaisseUneMarque(MatiereDArret matiere)
		{
			if (matiere - 2 <= MatiereDArret.Arbre)
			{
				return true;
			}
			return false;
		}

		internal static bool Recevoir(CasseSillonPacket paquet)
		{
			return Recevoir(paquet, Timing.Global.Milliseconds);
		}

		internal static bool Recevoir(CasseSillonPacket paquet, long maintenant)
		{
			if (paquet == null || paquet.Genre != CasseTrace.Empreinte)
			{
				return false;
			}
			if (Options.Instance.Combat.Heritage.DecorVu.Marques)
			{
				lock (Verrou)
				{
					Garder(paquet.MapId, paquet, maintenant, neuve: true);
				}
			}
			return true;
		}

		internal static CasseSillonPacket[] RecevoirLaCarte(Guid carte, CasseSillonPacket[] sillons)
		{
			return RecevoirLaCarte(carte, sillons, Timing.Global.Milliseconds);
		}

		internal static CasseSillonPacket[] RecevoirLaCarte(Guid carte, CasseSillonPacket[] sillons, long maintenant)
		{
			int num = 0;
			foreach (CasseSillonPacket casseSillonPacket in sillons)
			{
				if (casseSillonPacket != null && casseSillonPacket.Genre == CasseTrace.Empreinte)
				{
					num++;
				}
			}
			lock (Verrou)
			{
				if (num == 0 && !Toutes.Exists((Marque marque2) => marque2.Carte == carte))
				{
					return sillons;
				}
				bool marques = Options.Instance.Combat.Heritage.DecorVu.Marques;
				for (int num2 = Toutes.Count - 1; num2 >= 0; num2--)
				{
					Marque marque = Toutes[num2];
					if (marque.Carte == carte && (!marques || !Array.Exists(sillons, (CasseSillonPacket sillon) => EstLaMeme(marque, carte, sillon))))
					{
						Retirer(num2);
					}
				}
				if (marques)
				{
					foreach (CasseSillonPacket casseSillonPacket2 in sillons)
					{
						if (casseSillonPacket2 != null && casseSillonPacket2.Genre == CasseTrace.Empreinte)
						{
							Garder(carte, casseSillonPacket2, maintenant, neuve: false);
						}
					}
				}
			}
			if (num != 0)
			{
				return Array.FindAll(sillons, (CasseSillonPacket sillon) => sillon == null || sillon.Genre != CasseTrace.Empreinte);
			}
			return sillons;
		}

		private static void Garder(Guid carte, CasseSillonPacket paquet, long maintenant, bool neuve)
		{
			MatiereDArret courbe = (MatiereDArret)paquet.Courbe;
			if (!LaisseUneMarque(courbe) || paquet.VieMs <= 0 || paquet.AgeMs >= paquet.VieMs || !float.IsFinite(paquet.X0) || !float.IsFinite(paquet.Y0) || !float.IsFinite(paquet.X1) || !float.IsFinite(paquet.Y1))
			{
				return;
			}
			long num = maintenant - paquet.AgeMs;
			long num2 = num + paquet.VieMs;
			for (int i = 0; i < Toutes.Count; i++)
			{
				Marque marque = Toutes[i];
				if (EstLaMeme(marque, carte, paquet))
				{
					if (neuve && marque.Finie)
					{
						Retirer(i);
						break;
					}
					marque.Fin = Math.Max(marque.Fin, num2);
					return;
				}
			}
			if (courbe == MatiereDArret.Falaise)
			{
				Marque marque2 = Voisine(carte, paquet.X1, paquet.Y1, maintenant);
				if (marque2 != null)
				{
					marque2.Fin = Math.Max(marque2.Fin, num2);
					return;
				}
			}
			while (Toutes.Count >= 64)
			{
				Retirer(0);
			}
			var (sensX, sensY) = Sens(paquet.X0, paquet.Y0, paquet.X1, paquet.Y1);
			Toutes.Add(new Marque
			{
				Carte = carte,
				X0 = paquet.X0,
				Y0 = paquet.Y0,
				X1 = paquet.X1,
				Y1 = paquet.Y1,
				Matiere = courbe,
				Debut = num,
				Fin = num2,
				SensX = sensX,
				SensY = sensY,
				AgeeALArrivee = (paquet.AgeMs > 600)
			});
		}

		internal static Marque? Voisine(Guid carte, float piedsX, float piedsY, long maintenant)
		{
			float num = Options.Instance.Combat.Heritage.Decor.EmpreintePx;
			foreach (Marque toute in Toutes)
			{
				if (!(toute.Carte != carte) && toute.Matiere == MatiereDArret.Falaise && maintenant < toute.Fin)
				{
					float num2 = toute.X1 - piedsX;
					float num3 = toute.Y1 - piedsY;
					if (num2 * num2 + num3 * num3 < num * num)
					{
						return toute;
					}
				}
			}
			return null;
		}

		private static bool EstLaMeme(Marque marque, Guid carte, CasseSillonPacket? paquet)
		{
			if (paquet != null && paquet.Genre == CasseTrace.Empreinte && marque.Carte == carte && (uint)marque.Matiere == paquet.Courbe && MathF.Abs(marque.X0 - paquet.X0) < 0.5f && MathF.Abs(marque.Y0 - paquet.Y0) < 0.5f && MathF.Abs(marque.X1 - paquet.X1) < 0.5f)
			{
				return MathF.Abs(marque.Y1 - paquet.Y1) < 0.5f;
			}
			return false;
		}

		private static void Retirer(int rang)
		{
			LibererLImage(Toutes[rang]);
			Toutes.RemoveAt(rang);
		}

		private static void LibererLImage(Marque marque)
		{
			marque.Peinte?.Dispose();
			marque.Peinte = null;
			marque.Creuse?.Dispose();
			marque.Creuse = null;
			marque.PeintePour = null;
			marque.Silhouette = null;
			if (marque.Ressource != Guid.Empty && Fendues.TryGetValue(marque.Ressource, out Marque value) && value == marque)
			{
				Fendues.Remove(marque.Ressource);
			}
		}

		internal static (float X, float Y) Sens(float contactX, float contactY, float piedsX, float piedsY)
		{
			float num = piedsY - ((float)Options.Instance.Map.TileHeight / 2f - 6f);
			float num2 = contactX - piedsX;
			float num3 = contactY - num;
			float num4 = MathF.Sqrt(num2 * num2 + num3 * num3);
			if (num4 >= 0.5f)
			{
				return (X: num2 / num4, Y: num3 / num4);
			}
			return (X: 0f, Y: 0f);
		}

		internal static float Opacite(Marque marque, long maintenant)
		{
			if (maintenant < marque.Debut || maintenant >= marque.Fin)
			{
				return 0f;
			}
			long num = Math.Min(20000L, marque.Fin - marque.Debut);
			return Math.Clamp((float)(marque.Fin - maintenant) / (float)num, 0f, 1f);
		}

		internal static int Suivre(long maintenant)
		{
			lock (Verrou)
			{
				Fendues.Clear();
				if (Toutes.Count == 0)
				{
					return 0;
				}
				if (!Options.Instance.Combat.Heritage.DecorVu.Marques)
				{
					foreach (Marque toute in Toutes)
					{
						LibererLImage(toute);
					}
					Toutes.Clear();
					return 0;
				}
				int num = 0;
				int faites = 0;
				int num2 = 0;
				while (num2 < Toutes.Count)
				{
					Marque marque = Toutes[num2];
					if (maintenant >= marque.Fin)
					{
						Retirer(num2);
						continue;
					}
					num2++;
					if (maintenant >= marque.Debut && !marque.Finie && MapInstance.TryGet(marque.Carte, out MapInstance instance) && instance != null)
					{
						if (marque.Matiere == MatiereDArret.Falaise)
						{
							SuivreLeCreux(marque, instance, maintenant, ref faites);
						}
						else
						{
							SuivreLaFente(marque, instance, maintenant, ref faites);
						}
						if (!marque.Finie)
						{
							num++;
						}
					}
				}
				return num;
			}
		}

		private static void SuivreLeCreux(Marque marque, MapInstance carte, long maintenant, ref int faites)
		{
			if (!marque.Tranchee)
			{
				marque.Tranchee = Trancher(marque, carte.X, carte.Y, maintenant, SilhouetteDuJeu);
				if (!marque.Tranchee)
				{
					return;
				}
			}
			if (marque.Peinte != null || marque.Vide || faites > 0)
			{
				return;
			}
			if (Couverte(marque, carte))
			{
				marque.Cachee = true;
				marque.Vide = true;
				marque.Silhouette = null;
				return;
			}
			faites++;
			(int X, int Y) tuple = VersLaParoi(marque);
			int item = tuple.X;
			int item2 = tuple.Y;
			byte[] array = ImageDesLezardes(Graine(marque, 15838), item, item2, marque.Silhouette == null);
			if (!PoserDansLaRoche(marque, carte, array))
			{
				marque.Cachee = true;
				marque.Vide = true;
				marque.Silhouette = null;
			}
			else
			{
				marque.Peinte = Fabriquer(82, 82, array);
				marque.Vide = marque.Peinte == null;
			}
		}

		internal static (int X, int Y) VersLaParoi(Marque marque)
		{
			if (!marque.DeProfil)
			{
				return (X: 0, Y: MathF.Sign(marque.SensY));
			}
			return (X: MathF.Sign(marque.SensX), Y: 0);
		}

		internal static bool Couverte(Marque marque, MapInstance carte)
		{
			(int X, int Y) tuple = CaseQuiArrete(marque);
			int tx = tuple.X;
			int ty = tuple.Y;
			MapInstance mapInstance = CarteDeLaCase(carte, ref tx, ref ty);
			if (mapInstance != null)
			{
				return PeinteAuDessus(mapInstance.Layers, tx, ty);
			}
			return false;
		}

		internal static bool PeinteAuDessus(IReadOnlyDictionary<string, Tile[,]>? couches, int tx, int ty)
		{
			if (couches == null || tx < 0 || ty < 0)
			{
				return false;
			}
			LayerOptions layers = Options.Instance.Map.Layers;
			List<string>[] array = new List<string>[2] { layers.MiddleLayers, layers.UpperLayers };
			foreach (List<string> list in array)
			{
				foreach (string item in list)
				{
					if (couches.TryGetValue(item, out Tile[,] value) && value != null && tx < value.GetLength(0) && ty < value.GetLength(1) && value[tx, ty].TilesetId != Guid.Empty)
					{
						return true;
					}
				}
			}
			return false;
		}

		private static IGameTexture? SilhouetteDuJeu(IGameTexture texture, FloatRect source)
		{
			return KiCharge.MaskOf(texture, source, 0);
		}

		internal static bool Trancher(Marque marque, float carteX, float carteY, long maintenant, Func<IGameTexture, FloatRect, IGameTexture?> silhouetteDe)
		{
			long num = maintenant - marque.Debut;
			if (marque.AgeeALArrivee || num >= 1200)
			{
				return true;
			}
			bool flag = num >= 600;
			Entity entity = CorpsArrete(carteX + marque.X1, carteY + marque.Y1);
			if (entity != null && (entity.Dashing == null || flag) && Fantomes.DerniereImage(entity.Id, out IGameTexture texture, out FloatRect source, out FloatRect _) && texture != null && source.Width >= 1f && source.Height >= 1f)
			{
				IGameTexture gameTexture = silhouetteDe(texture, source);
				if (gameTexture != null)
				{
					marque.Silhouette = gameTexture;
					marque.SilhouetteLargeur = source.Width;
					marque.SilhouetteHauteur = source.Height;
					return true;
				}
			}
			return flag;
		}

		internal static Entity? CorpsArrete(float piedsX, float piedsY)
		{
			Entity result = null;
			float num = 24f;
			foreach (Entity value in Globals.Entities.Values)
			{
				bool flag = value == null || value.IsDisposed || value.IsHidden;
				bool flag2 = flag;
				if (!flag2)
				{
					EntityType type = value.Type;
					bool flag3 = (uint)type <= 1u;
					flag2 = !flag3;
				}
				if (!flag2 && Decor.Pieds(value, out var x, out var y))
				{
					float num2 = MathF.Sqrt((x - piedsX) * (x - piedsX) + (y - piedsY) * (y - piedsY));
					if (num2 < num)
					{
						num = num2;
						result = value;
					}
				}
			}
			return result;
		}

		internal static (float X, float Y) Pose(Marque marque, float carteX, float carteY)
		{
			return (X: MathF.Round(carteX + marque.X1 + (marque.DeProfil ? ((float)MathF.Sign(marque.SensX) * 22f) : 0f)) + (float)marque.EnfonceX, Y: MathF.Round(carteY + marque.Y1) + (float)marque.EnfonceY);
		}

		internal static FloatRect Creux(float piedsX, float piedsY, float largeur, float hauteur)
		{
			float num = MathF.Round(largeur * 1.2f);
			float num2 = MathF.Round(hauteur * 1.2f);
			return new FloatRect(MathF.Round(piedsX - num / 2f), piedsY - num2, num, num2);
		}

		internal static FloatRect ZoneDesLezardes(float piedsX, float piedsY)
		{
			return new FloatRect(piedsX - 41f, piedsY - 18f - 41f, 82f, 82f);
		}

		internal static int Branches(uint graine)
		{
			return (int)(5 + graine % 2);
		}

		internal static byte[] ImageDesLezardes(uint graine, bool autour, bool ovale)
		{
			return ImageDesLezardes(graine, autour ? ((double?)null) : new double?(-Math.PI / 2.0), ovale);
		}

		internal static byte[] ImageDesLezardes(uint graine, int versX, int versY, bool ovale)
		{
			return ImageDesLezardes(graine, (versX != 0 || versY != 0) ? Math.Atan2(versY, versX) : (-Math.PI / 2.0), ovale);
		}

		private static byte[] ImageDesLezardes(uint graine, double? milieu, bool ovale)
		{
			byte[] array = new byte[26896];
			double num = 41.0;
			double num2 = 41.0;
			if (ovale)
			{
				PeindreUnOvale(array, 82, num + 2.0, num2 + 2.0, 12553850);
				PeindreUnOvale(array, 82, num + 1.0, num2 + 1.0, 5975065);
				PeindreUnOvale(array, 82, num, num2, 3348750);
			}
			Alea alea = new Alea(graine);
			int num3 = Branches(graine);
			List<(double, double)[]> list = new List<(double, double)[]>();
			double num4 = Math.Min(1.9477874452256718, Math.PI * 9.0 / 20.0);
			for (int i = 0; i < num3; i++)
			{
				double num5;
				if (milieu.HasValue)
				{
					double valueOrDefault = milieu.GetValueOrDefault();
					num5 = valueOrDefault - num4 + (double)i / (double)(num3 - 1) * 2.0 * num4;
				}
				else
				{
					num5 = (double)i / (double)num3 * Math.PI * 2.0;
				}
				double num6 = num5;
				double x = num + Math.Cos(num6) * 12.0;
				double y = num2 + Math.Sin(num6) * 12.0 * 1.5;
				(double X, double Y) tuple = DansLeDemiPlan(x, y, num, num2, milieu);
				x = tuple.X;
				y = tuple.Y;
				List<(double, double)> list2 = new List<(double, double)> { (x, y) };
				double num7 = 26.0 * (0.5 + alea.Suivant() * 0.5);
				int num8 = 4 + (int)(alea.Suivant() * 2.0);
				for (int j = 0; j < num8; j++)
				{
					double num9 = num6 + (alea.Suivant() - 0.5) * 0.3 + (double)((j % 2 == 1) ? 1 : (-1)) * (0.3 + alea.Suivant() * 0.5);
					double num10 = num7 / (double)num8 * (0.7 + alea.Suivant() * 0.6);
					x += Math.Cos(num9) * num10;
					y += Math.Sin(num9) * num10;
					(x, y) = DansLeDemiPlan(x, y, num, num2, milieu);
					list2.Add((x, y));
					if (j == 1 && alea.Suivant() < 0.55)
					{
						double num11 = num6 + (double)((alea.Suivant() < 0.5) ? 1 : (-1)) * (0.8 + alea.Suivant() * 0.4);
						list.Add(new(double, double)[2]
						{
							(x, y),
							DansLeDemiPlan(x + Math.Cos(num11) * num7 * 0.3, y + Math.Sin(num11) * num7 * 0.3, num, num2, milieu)
						});
					}
				}
				list.Add(list2.ToArray());
			}
			foreach ((double, double)[] item in list)
			{
				Trait(array, 82, 82, item, 12553850, 1, 1, 2, 1, null);
			}
			foreach ((double, double)[] item2 in list)
			{
				Trait(array, 82, 82, item2, 3348750, 2, 2, 0, 0, null);
			}
			return array;
		}

		internal static (double X, double Y) DansLeDemiPlan(double x, double y, double cx, double cy, double? milieu)
		{
			if (milieu.HasValue)
			{
				double valueOrDefault = milieu.GetValueOrDefault();
				double num = Math.Cos(valueOrDefault);
				double num2 = Math.Sin(valueOrDefault);
				double num3 = (x - cx) * num + (y - cy) * num2;
				if (!(num3 >= 0.0))
				{
					return (X: x - 2.0 * num3 * num, Y: y - 2.0 * num3 * num2);
				}
				return (X: x, Y: y);
			}
			return (X: x, Y: y);
		}

		private static void PeindreUnOvale(byte[] image, int cote, double cx, double cy, int rvb)
		{
			double num = 9.0;
			double num2 = 13.0;
			for (int i = (int)Math.Floor(cy - num2); i <= (int)Math.Ceiling(cy + num2); i++)
			{
				for (int j = (int)Math.Floor(cx - num); j <= (int)Math.Ceiling(cx + num); j++)
				{
					double num3 = ((double)j + 0.5 - cx) / num;
					double num4 = ((double)i + 0.5 - cy) / num2;
					if (num3 * num3 + num4 * num4 <= 1.0)
					{
						Remplir(image, cote, cote, j, i, 1, 1, rvb, null);
					}
				}
			}
		}

		private static void SuivreLaFente(Marque marque, MapInstance carte, long maintenant, ref int faites)
		{
			if (marque.Ressource == Guid.Empty)
			{
				if (maintenant < marque.ProchaineRecherche)
				{
					return;
				}
				marque.ProchaineRecherche = maintenant + 500;
				Resource resource = LaRessource((float)carte.X + marque.X0, (float)carte.Y + marque.Y0, marque.SensX, marque.SensY);
				if (resource == null)
				{
					return;
				}
				marque.Ressource = resource.Id;
			}
			if (!Globals.Entities.TryGetValue(marque.Ressource, out Entity value) || !(value is Resource { IsDisposed: false } resource2))
			{
				return;
			}
			if (resource2.IsDead)
			{
				marque.Finie = true;
				LibererLImage(marque);
				return;
			}
			Fendues[resource2.Id] = marque;
			IGameTexture texture = resource2.Texture;
			FloatRect renderSource = resource2.RenderSource;
			if (texture == null || renderSource.Width < 1f || renderSource.Height < 1f || (marque.PeintePour == texture && MemeImage(marque.PeinteSource, renderSource)))
			{
				return;
			}
			marque.Peinte?.Dispose();
			marque.Peinte = null;
			marque.PeintePour = null;
			if (faites > 0)
			{
				return;
			}
			faites++;
			int num = (int)renderSource.Width;
			int num2 = (int)renderSource.Height;
			if (num > 512 || num2 > 512)
			{
				marque.PeintePour = texture;
				marque.PeinteSource = renderSource;
				return;
			}
			byte[] array = LireLOpacite(texture, renderSource, num, num2);
			if (array != null || ++marque.Essais >= 30)
			{
				marque.Essais = 0;
				marque.PeintePour = texture;
				marque.PeinteSource = renderSource;
				byte[] array2 = ImageDeLaFente(Graine(marque, 100), num, num2, array);
				if (array2 != null)
				{
					marque.Peinte = Fabriquer(num, num2, array2);
				}
			}
		}

		private static bool MemeImage(FloatRect a, FloatRect b)
		{
			if (a.X == b.X && a.Y == b.Y && a.Width == b.Width)
			{
				return a.Height == b.Height;
			}
			return false;
		}

		internal static Resource? LaRessource(float contactX, float contactY, float sensX, float sensY)
		{
			MapOptions map = Options.Instance.Map;
			float num = contactX + sensX * 6f;
			float num2 = contactY + sensY * 6f;
			Resource result = null;
			float num3 = 12f;
			foreach (Entity value in Globals.Entities.Values)
			{
				if (value is Resource { IsDead: false } resource && !value.IsDisposed && MapInstance.TryGet(resource.MapId, out MapInstance instance) && instance != null)
				{
					float num4 = instance.X + resource.X * map.TileWidth;
					float num5 = instance.Y + resource.Y * map.TileHeight;
					float num6 = MathF.Max(MathF.Max(num4 - num, 0f), num - (num4 + (float)map.TileWidth));
					float num7 = MathF.Max(MathF.Max(num5 - num2, 0f), num2 - (num5 + (float)map.TileHeight));
					float num8 = MathF.Sqrt(num6 * num6 + num7 * num7);
					if (num8 < num3)
					{
						num3 = num8;
						result = resource;
					}
				}
			}
			return result;
		}

		private static byte[]? LireLOpacite(IGameTexture texture, FloatRect source, int largeur, int hauteur)
		{
			try
			{
				Texture2D texture2 = texture.GetTexture<Texture2D>();
				if (texture2 != null)
				{
					AtlasReference atlasReference = texture.AtlasReference;
					if ((object)atlasReference == null || !atlasReference.IsRotated)
					{
						int num = (int)source.X;
						int num2 = (int)source.Y;
						AtlasReference atlasReference2 = texture.AtlasReference;
						if ((object)atlasReference2 != null)
						{
							num += atlasReference2.Bounds.X;
							num2 += atlasReference2.Bounds.Y;
						}
						Microsoft.Xna.Framework.Color[] array = new Microsoft.Xna.Framework.Color[largeur * hauteur];
						texture2.GetData(0, new Microsoft.Xna.Framework.Rectangle(num, num2, largeur, hauteur), array, 0, array.Length);
						byte[] array2 = new byte[array.Length];
						for (int i = 0; i < array.Length; i++)
						{
							array2[i] = array[i].A;
						}
						return array2;
					}
				}
				return null;
			}
			catch
			{
				return null;
			}
		}

		internal static bool Etendue(byte[] alphas, int largeur, int hauteur, out int centre, out int haut, out int bas)
		{
			int num = largeur;
			int num2 = -1;
			int num3 = -1;
			int num4 = -1;
			for (int i = 0; i < hauteur; i++)
			{
				for (int j = 0; j < largeur; j++)
				{
					if (alphas[i * largeur + j] >= 235)
					{
						num = Math.Min(num, j);
						num2 = Math.Max(num2, j);
						num3 = ((num3 < 0) ? i : num3);
						num4 = i;
					}
				}
			}
			centre = 0;
			haut = 0;
			bas = 0;
			if (num2 < 0)
			{
				return false;
			}
			centre = (num + num2 + 1) / 2;
			haut = -1;
			for (int k = num3; k <= num4; k++)
			{
				for (int l = Math.Max(0, centre - 2); l <= Math.Min(largeur - 1, centre + 2); l++)
				{
					if (alphas[k * largeur + l] >= 235)
					{
						haut = ((haut < 0) ? k : haut);
						bas = k;
						break;
					}
				}
			}
			if (haut < 0)
			{
				haut = num3;
				bas = num4;
			}
			return true;
		}

		internal static byte[]? ImageDeLaFente(uint graine, int largeur, int hauteur, byte[]? alphas)
		{
			if (largeur <= 0 || hauteur <= 0)
			{
				return null;
			}
			int centre = largeur / 2;
			int haut = 0;
			int bas = hauteur - 1;
			if (alphas != null && alphas.Length < largeur * hauteur)
			{
				alphas = null;
			}
			if ((alphas != null && !Etendue(alphas, largeur, hauteur, out centre, out haut, out bas)) || bas - haut + 1 < 8)
			{
				return null;
			}
			Alea alea = new Alea(graine);
			List<(double, double)> list = new List<(double, double)>();
			double num = centre - 1;
			double num2 = haut;
			int num3 = 1;
			while (num2 < (double)bas)
			{
				list.Add((num, num2));
				num2 += 4.0 + alea.Suivant() * 4.0;
				num3 = -num3;
				num = Math.Clamp(num + (double)num3 * (2.0 + alea.Suivant() * 3.0) + 0.4, centre - 6, centre + 8);
			}
			list.Add((num, bas + 1));
			byte[] array = new byte[largeur * hauteur * 4];
			Trait(array, largeur, hauteur, list, 14932697, 2, 1, 2, 0, alphas);
			Trait(array, largeur, hauteur, list, 2827296, 3, 1, 0, 0, alphas);
			Trait(array, largeur, hauteur, list, 1380366, 1, 1, 1, 0, alphas);
			(double, double) tuple = ((list.Count > 2) ? list[2] : list[1]);
			for (int i = 0; i < 6; i++)
			{
				Remplir(array, largeur, hauteur, Arrondi(tuple.Item1 - 2.0 - (double)i), Arrondi(tuple.Item2 + (double)i * 0.7), 2, 1, 2827296, alphas);
			}
			return array;
		}

		private static uint Graine(Marque marque, int sel)
		{
			return (uint)(sel + (int)marque.X1 * 13 + (int)marque.Y1);
		}

		private static int Arrondi(double valeur)
		{
			return (int)Math.Floor(valeur + 0.5);
		}

		private static void Trait(byte[] image, int largeur, int hauteur, IReadOnlyList<(double X, double Y)> points, int rvb, int epaisX, int epaisY, int dx, int dy, byte[]? alphas)
		{
			for (int i = 1; i < points.Count; i++)
			{
				(double X, double Y) tuple = points[i - 1];
				double item = tuple.X;
				double item2 = tuple.Y;
				(double X, double Y) tuple2 = points[i];
				double item3 = tuple2.X;
				double item4 = tuple2.Y;
				int num = Math.Max(1, (int)Math.Ceiling(Math.Max(Math.Abs(item3 - item), Math.Abs(item4 - item2))));
				for (int j = 0; j <= num; j++)
				{
					double num2 = (double)j / (double)num;
					Remplir(image, largeur, hauteur, Arrondi(item + (item3 - item) * num2) + dx, Arrondi(item2 + (item4 - item2) * num2) + dy, epaisX, epaisY, rvb, alphas);
				}
			}
		}

		private static void Remplir(byte[] image, int largeur, int hauteur, int x, int y, int l, int h, int rvb, byte[]? alphas)
		{
			for (int i = Math.Max(0, y); i < Math.Min(hauteur, y + h); i++)
			{
				for (int j = Math.Max(0, x); j < Math.Min(largeur, x + l); j++)
				{
					if (alphas == null || alphas[i * largeur + j] >= 235)
					{
						int num = (i * largeur + j) * 4;
						image[num] = (byte)((rvb >> 16) & 0xFF);
						image[num + 1] = (byte)((rvb >> 8) & 0xFF);
						image[num + 2] = (byte)(rvb & 0xFF);
						image[num + 3] = byte.MaxValue;
					}
				}
			}
		}

		private static IGameTexture? Fabriquer(int largeur, int hauteur, byte[] image)
		{
			return Graphics.Renderer?.CreateTextureFromPixels($"heritage/marque/{++_fabriquees}", largeur, hauteur, image);
		}

		private static Color Ton(int rvb, int alpha)
		{
			return new Color(alpha, (rvb >> 16) & 0xFF, (rvb >> 8) & 0xFF, rvb & 0xFF);
		}

		internal static void DessinerAuSol()
		{
			Decor.JouerLesChocsDus();
			if (Toutes.Count == 0)
			{
				return;
			}
			long milliseconds = Timing.Global.Milliseconds;
			if (Suivre(milliseconds) == 0)
			{
				return;
			}
			FloatRect worldViewport = Graphics.WorldViewport;
			lock (Verrou)
			{
				foreach (Marque toute in Toutes)
				{
					if (toute.Matiere == MatiereDArret.Falaise && toute.Tranchee && MapInstance.TryGet(toute.Carte, out MapInstance instance) && instance != null)
					{
						DessinerLeCreux(toute, instance.X, instance.Y, Opacite(toute, milliseconds), worldViewport);
					}
				}
			}
		}

		private static void DessinerLeCreux(Marque marque, float carteX, float carteY, float opacite, FloatRect vue)
		{
			(float X, float Y) tuple = Pose(marque, carteX, carteY);
			float item = tuple.X;
			float item2 = tuple.Y;
			FloatRect floatRect = ZoneDesLezardes(item, item2);
			if (opacite <= 0f || !vue.IntersectsWith(floatRect))
			{
				return;
			}
			int num = (int)MathF.Round(255f * opacite);
			IGameTexture creuse = marque.Creuse;
			if (creuse != null)
			{
				FloatRect floatRect2 = Creux(item, item2, marque.SilhouetteLargeur, marque.SilhouetteHauteur);
				FloatRect srcRectangle = new FloatRect(0f, 0f, floatRect2.Width + 2f, floatRect2.Height + 2f);
				Graphics.DrawGameTexture(creuse, srcRectangle, new FloatRect(floatRect2.X, floatRect2.Y, srcRectangle.Width, srcRectangle.Height), new Color(num, 255, 255, 255));
			}
			else
			{
				IGameTexture silhouette = marque.Silhouette;
				if (silhouette != null)
				{
					FloatRect source = new FloatRect(0f, 0f, marque.SilhouetteLargeur, marque.SilhouetteHauteur);
					FloatRect creux = Creux(item, item2, marque.SilhouetteLargeur, marque.SilhouetteHauteur);
					PoserLaSilhouette(silhouette, source, creux, 2f, 12553850, num);
					PoserLaSilhouette(silhouette, source, creux, 1f, 5975065, num);
					PoserLaSilhouette(silhouette, source, creux, 0f, 3348750, num);
				}
			}
			IGameTexture peinte = marque.Peinte;
			if (peinte != null)
			{
				Graphics.DrawGameTexture(peinte, new FloatRect(0f, 0f, 82f, 82f), floatRect, new Color(num, 255, 255, 255));
			}
		}

		private static void PoserLaSilhouette(IGameTexture silhouette, FloatRect source, FloatRect creux, float decale, int rvb, int alpha)
		{
			Graphics.DrawGameTexture(silhouette, source, new FloatRect(creux.X + decale, creux.Y + decale, creux.Width, creux.Height), Ton(rvb, alpha));
		}

		internal static void DessinerDansLeTri(Entity entite, bool derriere)
		{
			if (derriere || Fendues.Count == 0 || !(entite is Resource resource))
			{
				return;
			}
			lock (Verrou)
			{
				if (!Fendues.TryGetValue(resource.Id, out Marque value))
				{
					return;
				}
				IGameTexture peinte = value.Peinte;
				if (peinte != null && value.PeintePour == resource.Texture)
				{
					float num = Opacite(value, Timing.Global.Milliseconds);
					if (!(num <= 0f))
					{
						Graphics.DrawGameTexture(peinte, new FloatRect(0f, 0f, value.PeinteSource.Width, value.PeinteSource.Height), resource.RenderDestination, new Color((int)MathF.Round(255f * num), 255, 255, 255));
					}
				}
			}
		}

		internal static Grille MasqueDeRoche(int x, int y, int largeur, int hauteur, int tw, int th, Func<int, int, byte[]?> tuile)
		{
			bool[] array = new bool[largeur * hauteur];
			Dictionary<(int, int), byte[]> dictionary = new Dictionary<(int, int), byte[]>();
			for (int i = 0; i < hauteur; i++)
			{
				int num = y + i;
				int num2 = (int)MathF.Floor((float)num / (float)th);
				for (int j = 0; j < largeur; j++)
				{
					int num3 = x + j;
					int num4 = (int)MathF.Floor((float)num3 / (float)tw);
					if (!dictionary.TryGetValue((num4, num2), out var value))
					{
						value = tuile(num4, num2);
						dictionary[(num4, num2)] = value;
					}
					array[i * largeur + j] = value != null && value[(num - num2 * th) * tw + num3 - num4 * tw] >= 235;
				}
			}
			return new Grille(x, y, largeur, hauteur, array);
		}

		internal static Grille Sable(Grille roche, int versX, int versY, int bord)
		{
			bool[] array = new bool[roche.Largeur * roche.Hauteur];
			bool deProfil;
			int ligne;
			if (versX != 0 || versY != 0)
			{
				deProfil = versX != 0;
				int num = (deProfil ? versX : versY);
				int num2 = (deProfil ? roche.X : roche.Y);
				int num3 = (deProfil ? roche.Largeur : roche.Hauteur);
				int num4 = (deProfil ? roche.Hauteur : roche.Largeur);
				for (ligne = 0; ligne < num4; ligne++)
				{
					int? num5 = null;
					for (int i = bord - num2; i >= 0 && i < num3; i += num)
					{
						if (roche.Pixels[Rang(i)])
						{
							num5 = i;
							break;
						}
					}
					if (!num5.HasValue)
					{
						continue;
					}
					int valueOrDefault = num5.GetValueOrDefault();
					if (valueOrDefault != bord - num2)
					{
						for (int j = 0; j < num3; j++)
						{
							array[Rang(j)] = (j - valueOrDefault) * num < 0;
						}
					}
				}
			}
			return new Grille(roche.X, roche.Y, roche.Largeur, roche.Hauteur, array);
			int Rang(int k)
			{
				if (!deProfil)
				{
					return k * roche.Largeur + ligne;
				}
				return ligne * roche.Largeur + k;
			}
		}

		internal static float HorsDuSable(Grille forme, Grille sable, int dx, int dy)
		{
			int num = 0;
			int num2 = 0;
			for (int i = 0; i < forme.Hauteur; i++)
			{
				for (int j = 0; j < forme.Largeur; j++)
				{
					if (forme.Pixels[i * forme.Largeur + j])
					{
						num++;
						if (sable.En(forme.X + j + dx, forme.Y + i + dy))
						{
							num2++;
						}
					}
				}
			}
			if (num != 0)
			{
				return (float)(num - num2) / (float)num;
			}
			return 1f;
		}

		internal static (int X, int Y)? Enfoncement(Grille forme, Grille sable, float sensX, float sensY)
		{
			for (int i = 0; i <= 8; i++)
			{
				int num = (int)MathF.Round(sensX * (float)i);
				int num2 = (int)MathF.Round(sensY * (float)i);
				if (HorsDuSable(forme, sable, num, num2) >= 2f / 3f)
				{
					return (num, num2);
				}
			}
			return null;
		}

		internal static int Decouper(byte[] image, int largeur, int hauteur, int x, int y, Grille sable)
		{
			int num = 0;
			for (int i = 0; i < hauteur; i++)
			{
				for (int j = 0; j < largeur; j++)
				{
					int num2 = (i * largeur + j) * 4;
					if (image[num2 + 3] != 0 && sable.En(x + j, y + i))
					{
						image[num2] = (image[num2 + 1] = (image[num2 + 2] = (image[num2 + 3] = 0)));
						num++;
					}
				}
			}
			return num;
		}

		internal static bool[] Agrandir(byte[] alphas, int sw, int sh, int largeur, int hauteur)
		{
			bool[] array = new bool[largeur * hauteur];
			for (int i = 0; i < hauteur; i++)
			{
				int num = Math.Min(sh - 1, (int)(((float)i + 0.5f) * (float)sh / (float)hauteur));
				for (int j = 0; j < largeur; j++)
				{
					int num2 = Math.Min(sw - 1, (int)(((float)j + 0.5f) * (float)sw / (float)largeur));
					array[i * largeur + j] = alphas[num * sw + num2] >= 128;
				}
			}
			return array;
		}

		internal static byte[] CreuxPeint(bool[] forme, int largeur, int hauteur)
		{
			int num = 2;
			int num2 = 1;
			int num3 = largeur + num;
			int num4 = hauteur + num;
			byte[] array = new byte[num3 * num4 * 4];
			for (int i = 0; i < num4; i++)
			{
				for (int j = 0; j < num3; j++)
				{
					int num5 = (Dedans(j, i) ? 3348750 : (Dedans(j - num2, i - num2) ? 5975065 : (Dedans(j - num, i - num) ? 12553850 : (-1))));
					if (num5 >= 0)
					{
						Remplir(array, num3, num4, j, i, 1, 1, num5, null);
					}
				}
			}
			return array;
			bool Dedans(int num6, int num7)
			{
				if (num6 >= 0 && num7 >= 0 && num6 < largeur && num7 < hauteur)
				{
					return forme[num7 * largeur + num6];
				}
				return false;
			}
		}

		internal static Grille Ovale(float cx, float cy)
		{
			double num = 9.0;
			double num2 = 13.0;
			int num3 = (int)Math.Floor((double)cx - num);
			int num4 = (int)Math.Floor((double)cy - num2);
			int num5 = (int)Math.Ceiling((double)cx + num) - num3 + 1;
			int num6 = (int)Math.Ceiling((double)cy + num2) - num4 + 1;
			bool[] array = new bool[num5 * num6];
			for (int i = 0; i < num6; i++)
			{
				for (int j = 0; j < num5; j++)
				{
					double num7 = ((double)(num3 + j) + 0.5 - (double)cx) / num;
					double num8 = ((double)(num4 + i) + 0.5 - (double)cy) / num2;
					array[i * num5 + j] = num7 * num7 + num8 * num8 <= 1.0;
				}
			}
			return new Grille(num3, num4, num5, num6, array);
		}

		internal static (int X, int Y) CaseQuiArrete(Marque marque)
		{
			MapOptions map = Options.Instance.Map;
			return (X: (int)MathF.Floor((marque.X0 + marque.SensX * 6f) / (float)map.TileWidth), Y: (int)MathF.Floor((marque.Y0 + marque.SensY * 6f) / (float)map.TileHeight));
		}

		private static MapInstance? CarteDeLaCase(MapInstance carte, ref int tx, ref int ty)
		{
			MapOptions map = Options.Instance.Map;
			int num = ((tx < 0) ? (-1) : ((tx >= map.MapWidth) ? 1 : 0));
			int num2 = ((ty < 0) ? (-1) : ((ty >= map.MapHeight) ? 1 : 0));
			if (num == 0 && num2 == 0)
			{
				return carte;
			}
			int num3 = carte.GridX + num;
			int num4 = carte.GridY + num2;
			if (num3 >= 0 && num4 >= 0 && num3 < Globals.MapGridWidth && num4 < Globals.MapGridHeight)
			{
				Guid[,]? mapGrid = Globals.MapGrid;
				Guid? guid = ((mapGrid != null) ? new Guid?(mapGrid[num3, num4]) : ((Guid?)null));
				if (guid.HasValue)
				{
					Guid valueOrDefault = guid.GetValueOrDefault();
					if (!(valueOrDefault == Guid.Empty) && MapInstance.TryGet(valueOrDefault, out MapInstance instance) && instance != null)
					{
						tx -= num * map.MapWidth;
						ty -= num2 * map.MapHeight;
						return instance;
					}
				}
			}
			return null;
		}

		internal static List<string> CouchesDeLaFalaise(MapInstance carte, int tx, int ty)
		{
			List<string> list = new List<string>();
			MapInstance mapInstance = CarteDeLaCase(carte, ref tx, ref ty);
			if (mapInstance != null)
			{
				Dictionary<string, Tile[,]> layers = mapInstance.Layers;
				if (layers != null && tx >= 0 && ty >= 0)
				{
					List<string> lowerLayers = Options.Instance.Map.Layers.LowerLayers;
					for (int i = 1; i < lowerLayers.Count; i++)
					{
						if (layers.TryGetValue(lowerLayers[i], out var value) && value != null && tx < value.GetLength(0) && ty < value.GetLength(1) && value[tx, ty].TilesetId != Guid.Empty)
						{
							list.Add(lowerLayers[i]);
						}
					}
					return list;
				}
			}
			return list;
		}

		private static bool PoserDansLaRoche(Marque marque, MapInstance carte, byte[] lezardes)
		{
			marque.EnfonceX = (marque.EnfonceY = 0);
			var (num, num2) = VersLaParoi(marque);
			var (num3, num4) = CaseQuiArrete(marque);
			List<string> couches;
			FloatRect floatRect;
			Grille grille;
			if (num != 0 || num2 != 0)
			{
				couches = CouchesDeLaFalaise(carte, num3, num4);
				if (couches != null && couches.Count > 0)
				{
					(float X, float Y) tuple3 = Pose(marque, 0f, 0f);
					float item = tuple3.X;
					float item2 = tuple3.Y;
					floatRect = ZoneDesLezardes(item, item2);
					IGameTexture silhouette = marque.Silhouette;
					if (silhouette != null)
					{
						int num5 = (int)marque.SilhouetteLargeur;
						int num6 = (int)marque.SilhouetteHauteur;
						FloatRect floatRect2 = Creux(item, item2, num5, num6);
						int num7 = (int)floatRect2.Width;
						int num8 = (int)floatRect2.Height;
						if (num7 >= 1 && num8 >= 1)
						{
							byte[] array = LireLOpacite(silhouette, new FloatRect(0f, 0f, num5, num6), num5, num6);
							if (array != null)
							{
								grille = new Grille((int)floatRect2.X, (int)floatRect2.Y, num7, num8, Agrandir(array, num5, num6, num7, num8));
								goto IL_0167;
							}
						}
						return true;
					}
					grille = Ovale(floatRect.X + 41f, floatRect.Y + 41f);
					goto IL_0167;
				}
			}
			return true;
			IL_0167:
			int tileWidth = Options.Instance.Map.TileWidth;
			int tileHeight = Options.Instance.Map.TileHeight;
			int num9 = ((num > 0) ? (num3 * tileWidth) : ((num < 0) ? (num3 * tileWidth + tileWidth - 1) : ((num2 > 0) ? (num4 * tileHeight) : (num4 * tileHeight + tileHeight - 1))));
			int num10 = (int)MathF.Min(floatRect.X, grille.X) - 8;
			int num11 = (int)MathF.Min(floatRect.Y, grille.Y) - 8;
			int num12 = (int)MathF.Max(floatRect.Right, (float)(grille.X + grille.Largeur) + 2f) + 8;
			int num13 = (int)MathF.Max(floatRect.Bottom, (float)(grille.Y + grille.Hauteur) + 2f) + 8;
			if (num != 0)
			{
				num10 = Math.Min(num10, num9) - ((num < 0) ? tileWidth : 0);
				num12 = Math.Max(num12, num9 + 1) + ((num > 0) ? tileWidth : 0);
			}
			else
			{
				num11 = Math.Min(num11, num9) - ((num2 < 0) ? tileHeight : 0);
				num13 = Math.Max(num13, num9 + 1) + ((num2 > 0) ? tileHeight : 0);
			}
			bool illisible = false;
			Dictionary<(IGameTexture, int, int, int, int), byte[]?> lues = new Dictionary<(IGameTexture, int, int, int, int), byte[]>();
			Grille roche = MasqueDeRoche(num10, num11, num12 - num10, num13 - num11, tileWidth, tileHeight, (int tx, int ty) => (!illisible) ? OpaciteDeLaFalaise(carte, tx, ty, couches, lues, ref illisible) : null);
			if (illisible)
			{
				return true;
			}
			Grille sable = Sable(roche, num, num2, num9);
			(int, int)? tuple4 = Enfoncement(grille, sable, marque.SensX, marque.SensY);
			if (tuple4.HasValue)
			{
				(int, int) valueOrDefault = tuple4.GetValueOrDefault();
				(marque.EnfonceX, marque.EnfonceY) = valueOrDefault;
				Decouper(lezardes, 82, 82, (int)floatRect.X + valueOrDefault.Item1, (int)floatRect.Y + valueOrDefault.Item2, sable);
				if (marque.Silhouette != null)
				{
					byte[] image = CreuxPeint(grille.Pixels, grille.Largeur, grille.Hauteur);
					int largeur = grille.Largeur + 2;
					int hauteur = grille.Hauteur + 2;
					if (Decouper(image, largeur, hauteur, grille.X + valueOrDefault.Item1, grille.Y + valueOrDefault.Item2, sable) > 0)
					{
						marque.Creuse = Fabriquer(largeur, hauteur, image);
					}
				}
				return true;
			}
			return false;
		}

		private static byte[]? OpaciteDeLaFalaise(MapInstance carte, int tx, int ty, List<string> couches, Dictionary<(IGameTexture, int, int, int, int), byte[]?> lues, ref bool illisible)
		{
			MapInstance mapInstance = CarteDeLaCase(carte, ref tx, ref ty);
			if (mapInstance != null)
			{
				Dictionary<string, Tile[,]> layers = mapInstance.Layers;
				if (layers != null && tx >= 0 && ty >= 0)
				{
					int tileWidth = Options.Instance.Map.TileWidth;
					int tileHeight = Options.Instance.Map.TileHeight;
					byte[] array = null;
					{
						foreach (string couch in couches)
						{
							if (!layers.TryGetValue(couch, out var value) || value == null || tx >= value.GetLength(0) || ty >= value.GetLength(1) || value[tx, ty].TilesetId == Guid.Empty)
							{
								continue;
							}
							Tile tile = value[tx, ty];
							if (!(tile.TilesetTexture is IGameTexture gameTexture))
							{
								illisible = true;
								return null;
							}
							Dictionary<string, Autotile[,]> dictionary = mapInstance.Autotiles?.Layers;
							Autotile[,] value2;
							Autotile autotile = ((dictionary != null && dictionary.TryGetValue(couch, out value2) && value2 != null && tx < value2.GetLength(0) && ty < value2.GetLength(1)) ? value2[tx, ty] : null);
							byte b = autotile?.RenderState ?? 1;
							if (array == null)
							{
								array = new byte[tileWidth * tileHeight];
							}
							bool flag = true;
							switch (b)
							{
							case 1:
								flag = Superposer(array, tileWidth, gameTexture, tile.X * tileWidth, tile.Y * tileHeight, tileWidth, tileHeight, 0, 0, lues);
								break;
							case 2:
								if (autotile != null)
								{
									byte autotile2 = tile.Autotile;
									bool flag2 = (uint)(autotile2 - 4) <= 1u;
									int num = (flag2 ? (-tileHeight) : 0);
									for (int i = 1; i <= 4 && flag; i++)
									{
										Point16i point16i = autotile.QuarterTile[i];
										byte[] opacite = array;
										int tw = tileWidth;
										IGameTexture planche = gameTexture;
										int x = point16i.X;
										int sy = point16i.Y + num;
										int largeur = tileWidth / 2;
										int hauteur = tileHeight / 2;
										flag2 = ((i == 2 || i == 4) ? true : false);
										int dx = (flag2 ? (tileWidth / 2) : 0);
										bool flag3 = (uint)(i - 3) <= 1u;
										flag = Superposer(opacite, tw, planche, x, sy, largeur, hauteur, dx, flag3 ? (tileHeight / 2) : 0, lues);
									}
								}
								break;
							}
							if (!flag)
							{
								illisible = true;
								return null;
							}
						}
						return array;
					}
				}
			}
			return null;
		}

		private static bool Superposer(byte[] opacite, int tw, IGameTexture planche, int sx, int sy, int largeur, int hauteur, int dx, int dy, Dictionary<(IGameTexture, int, int, int, int), byte[]?> lues)
		{
			(IGameTexture, int, int, int, int) key = (planche, sx, sy, largeur, hauteur);
			if (!lues.TryGetValue(key, out byte[] value))
			{
				value = (lues[key] = LireLOpacite(planche, new FloatRect(sx, sy, largeur, hauteur), largeur, hauteur));
			}
			if (value == null)
			{
				return false;
			}
			for (int i = 0; i < hauteur; i++)
			{
				for (int j = 0; j < largeur; j++)
				{
					int num = (dy + i) * tw + dx + j;
					opacite[num] = Math.Max(opacite[num], value[i * largeur + j]);
				}
			}
			return true;
		}
	}

	internal static class Moment
	{
		internal const string NomDuKiai = "Kiai";

		internal const string NomDeLOnde = "Onde de choc";

		internal const byte OpacitePleine = byte.MaxValue;

		internal const byte OpaciteBasse = 150;

		private static readonly Color?[] Pleines = Teintes(byte.MaxValue);

		private static readonly Color?[] Basses = Teintes(150);

		internal static bool SeMontre
		{
			get
			{
				HeritageTechniquesOptions techniques = Options.Instance.Combat.Heritage.Techniques;
				if (techniques.Lien)
				{
					return techniques.Moments;
				}
				return false;
			}
		}

		private static Color?[] Teintes(byte opacite)
		{
			Color[] array = new Color[Enum.GetValues<MomentHeritage>().Max((MomentHeritage technique) => (int)technique) + 1];
			MomentHeritage[] values = Enum.GetValues<MomentHeritage>();
			foreach (MomentHeritage momentHeritage in values)
			{
				Color color = Lisere.Couleur(EtatDe(momentHeritage));
				if ((object)color != null)
				{
					array[(uint)momentHeritage] = new Color(opacite, color.R, color.G, color.B);
				}
			}
			return array;
		}

		internal static EtatHeritage EtatDe(MomentHeritage technique)
		{
			return technique switch
			{
				MomentHeritage.Boule => EtatHeritage.Repousse, 
				MomentHeritage.Kiai => EtatHeritage.Souleve, 
				MomentHeritage.Onde => EtatHeritage.Flanc, 
				MomentHeritage.Signature => EtatHeritage.Plante, 
				_ => EtatHeritage.Aucun, 
			};
		}

		internal static Color? Couleur(MomentHeritage technique, bool plein = true)
		{
			if ((int)technique >= Pleines.Length)
			{
				return null;
			}
			return (plein ? Pleines : Basses)[(uint)technique];
		}

		internal static bool EstUnLancerDe(Guid auteurId, Dash glissade)
		{
			if (auteurId != Guid.Empty && glissade.AuteurId == auteurId && glissade.Nature == NatureDeGlissade.Projection)
			{
				return (glissade.FormeHeritage & FormeDeLancer.DeLHeritage) != 0;
			}
			return false;
		}

		internal static bool EstLance(Entity entite)
		{
			Dash dashing = entite.Dashing;
			if (dashing != null)
			{
				Player me = Globals.Me;
				if (me != null)
				{
					return EstUnLancerDe(me.Id, dashing);
				}
			}
			return false;
		}

		internal static MomentHeritage De(Guid cibleId)
		{
			return DeA(cibleId, Timing.Global.Milliseconds);
		}

		internal static MomentHeritage DeA(Guid cibleId, long maintenant)
		{
			if (cibleId == Guid.Empty || !SeMontre || !Globals.Entities.TryGetValue(cibleId, out Entity value) || value == null || value.IsDying)
			{
				return MomentHeritage.Aucun;
			}
			return HeritageRegles.Moments(EtatsVus.Vif(cibleId, EtatHeritage.Repousse, maintenant), EtatsVus.Vif(cibleId, EtatHeritage.Flanc, maintenant), EtatsVus.Vif(cibleId, EtatHeritage.Souleve, maintenant), EtatsVus.Vif(cibleId, EtatHeritage.Plante, maintenant), EstLance(value), Lien.FormeConsommee(cibleId, maintenant));
		}

		internal static MomentHeritage TechniqueDe(Guid sortId)
		{
			if (sortId == Guid.Empty)
			{
				return MomentHeritage.Aucun;
			}
			if (HeritageRegles.EstLaBoule(sortId))
			{
				return MomentHeritage.Boule;
			}
			if (Options.Instance.Beam.IsBeamSpell(sortId))
			{
				return MomentHeritage.Signature;
			}
			if (!DatabaseObject<SpellDescriptor>.TryGet(sortId, out var databaseObject))
			{
				return MomentHeritage.Aucun;
			}
			return TechniqueAuNom(databaseObject.SpellType, databaseObject.Name);
		}

		internal static MomentHeritage TechniqueAuNom(SpellType genre, string? nom)
		{
			if (genre != SpellType.CombatSpell)
			{
				return MomentHeritage.Aucun;
			}
			if (!string.Equals(nom, "Kiai", StringComparison.OrdinalIgnoreCase))
			{
				if (!string.Equals(nom, "Onde de choc", StringComparison.OrdinalIgnoreCase))
				{
					return MomentHeritage.Aucun;
				}
				return MomentHeritage.Onde;
			}
			return MomentHeritage.Kiai;
		}

		private static long Depuis(Guid cibleId, long maintenant)
		{
			if (Globals.Entities.TryGetValue(cibleId, out Entity value) && value != null && Lisere.Porte(value, maintenant, out var depuis) != EtatHeritage.Aucun)
			{
				return depuis;
			}
			if (Lien.FormeConsommee(cibleId, maintenant) == FormeDeLancer.Simple)
			{
				return 0L;
			}
			return Lien.FormeConsommeeA;
		}

		internal static Color? Teinte(Guid sortId, Guid cibleId, long maintenant)
		{
			MomentHeritage momentHeritage = DeA(cibleId, maintenant);
			if (momentHeritage == MomentHeritage.Aucun)
			{
				return null;
			}
			MomentHeritage momentHeritage2 = TechniqueDe(sortId);
			if ((momentHeritage & momentHeritage2) == 0)
			{
				return null;
			}
			int liserePeriodeMs = Options.Instance.Combat.Heritage.Allure.LiserePeriodeMs;
			return Couleur(momentHeritage2, Lisere.Plein(maintenant - Depuis(cibleId, maintenant), liserePeriodeMs));
		}

		internal static void Eclairer(ImagePanel eclat, Guid sortId, bool enGlissement)
		{
			if (!enGlissement && !(sortId == Guid.Empty) && !(ReferenceId == Guid.Empty))
			{
				Color color = Teinte(sortId, ReferenceId, Timing.Global.Milliseconds);
				if ((object)color != null)
				{
					eclat.IsHidden = false;
					eclat.RenderColor = color;
				}
			}
		}
	}

	internal static class Mots
	{
		internal static bool Muet(EchangeEvent evenement, Entity entite)
		{
			if (entite is Player || !Tait(evenement))
			{
				return false;
			}
			HeritageAllureOptions allure = Options.Instance.Combat.Heritage.Allure;
			if (allure.Actif)
			{
				return allure.MotsCaches;
			}
			return false;
		}

		internal static bool Tait(EchangeEvent evenement)
		{
			switch (evenement)
			{
			case EchangeEvent.Resiste:
			case EchangeEvent.Moitie:
			case EchangeEvent.Immunise:
			case EchangeEvent.Quille:
			case EchangeEvent.Renverse:
				return true;
			default:
				return false;
			}
		}
	}

	internal static class Mouvement
	{
		internal enum Genre : byte
		{
			Aucun,
			Lancer,
			QuartDeTour,
			Technique
		}

		private struct Lance
		{
			public Guid Corps;

			public Dash Glissade;

			public float DepartX;

			public float DepartY;

			public float Ux;

			public float Uy;

			public float Longueur;

			public int Passees;

			public long Depuis;
		}

		private struct Arc
		{
			public Guid Corps;

			public Dash? Glissade;

			public Guid Cible;

			public long Depuis;

			public float SensX;

			public float SensY;

			public bool Pose;
		}

		internal const float TrajetMinPx = 4f;

		internal static readonly float[] ImagesDuLancer = new float[3] { 0.1f, 0.26f, 0.46f };

		internal const int ImageDuLancerVieMs = 230;

		internal const float ImageDuLancerForce = 0.8f;

		internal const float ImageDuLancerEcartPx = 14f;

		internal const int LancesMax = 16;

		internal const int LancePerimeMs = 3000;

		internal static readonly Color CremeDuLancer = new Color(255, 244, 227, 200);

		internal const int TraitsDuLancer = 5;

		internal const float TraitsLargePx = 30f;

		internal const int TraitsDuLancerEnPlusMs = 80;

		internal const float TraitsDepuisPx = 24f;

		internal const int BouffeesDuLancer = 4;

		internal const int GrainsParBouffee = 4;

		internal const float BouffeeDerrierePx = 8f;

		internal const float BouffeePenche = 0.6f;

		internal static readonly Coups.Jet JetDuLancer = new Coups.Jet(0.8f, 40f, 120f, 160f, 300);

		internal const float SillonAvancePx = 8f;

		internal const float SillonRetraitPx = 6f;

		internal const float SillonDepuisPx = 12f;

		internal const int TraitsDeLaTechnique = 4;

		internal const int TraitsDeLaTechniqueEnPlusMs = 20;

		internal const float SillonDeTechniqueDepuisPx = 8f;

		internal const float ArcRayonPx = 44f;

		internal const float ArcSousLesPieds = 2f;

		internal const int ArcAttenteMs = 250;

		internal const int ArcsMax = 16;

		private static readonly Lance[] Lances = new Lance[16];

		private static int _lances;

		private static readonly Arc[] Arcs = new Arc[16];

		private static int _arcs;

		internal static int LancesSuivis => _lances;

		internal static int EnAttente => _arcs + _lances;

		internal static int ArcsRetenus => _arcs;

		internal static Genre GenreDe(Dash glissade)
		{
			if (glissade.Nature == NatureDeGlissade.Fauche)
			{
				return Genre.QuartDeTour;
			}
			if ((glissade.FormeHeritage & FormeDeLancer.DeLHeritage) == 0)
			{
				return Genre.Aucun;
			}
			switch (glissade.Nature)
			{
			case NatureDeGlissade.Projection:
				return Genre.Lancer;
			case NatureDeGlissade.Ordinaire:
				if ((glissade.FormeHeritage & FormeDeLancer.Technique) != FormeDeLancer.Simple)
				{
					return Genre.Technique;
				}
				break;
			}
			return Genre.Aucun;
		}

		internal static bool AuSol(Entity entite)
		{
			Dash dashing = entite.Dashing;
			if (dashing != null && dashing.Nature == NatureDeGlissade.Projection && (dashing.FormeHeritage & FormeDeLancer.DeLHeritage) != FormeDeLancer.Simple)
			{
				return Options.Instance.Combat.Heritage.Allure.Actif;
			}
			return false;
		}

		internal static bool Trajet(Dash glissade, out float ux, out float uy, out float longueur)
		{
			(float X, float Y) startOffset = glissade.StartOffset;
			float item = startOffset.X;
			float item2 = startOffset.Y;
			(float X, float Y) endOffset = glissade.EndOffset;
			float item3 = endOffset.X;
			float item4 = endOffset.Y;
			float num = item3 - item;
			float num2 = item4 - item2;
			longueur = MathF.Sqrt(num * num + num2 * num2);
			if (!(longueur >= 4f) || float.IsInfinity(longueur))
			{
				ux = 0f;
				uy = 0f;
				return false;
			}
			float num3 = num / longueur;
			float num4 = num2 / longueur;
			ux = num3;
			uy = num4;
			return true;
		}

		internal static int Duree(Dash glissade)
		{
			return (int)Math.Clamp(glissade.EndsAt - glissade.StartedAt, 0L, 2147483647L);
		}

		internal static bool TraceDuSillon(float departX, float departY, float ux, float uy, float longueur, out float x, out float y, out float dx, out float dy)
		{
			float num = longueur - 6f;
			x = departX + ux * 8f;
			y = departY + 1f + uy * 8f;
			dx = ux * num;
			dy = uy * num;
			return longueur >= 12f;
		}

		internal static float AxeDeBouffee(float ux, float uy)
		{
			return MathF.Atan2(0f - uy, 0f - ux) - 0.6f * ((ux < 0f) ? (-1f) : 1f);
		}

		internal static void SurGlissade(Entity entite, Dash glissade)
		{
			SurGlissade(entite, glissade, Timing.Global.Milliseconds);
		}

		internal static Genre SurGlissade(Entity entite, Dash glissade, long maintenant)
		{
			Genre genre = GenreDe(glissade);
			HeritageAllureOptions allure = Options.Instance.Combat.Heritage.Allure;
			if (genre == Genre.Aucun || !allure.Actif)
			{
				return Genre.Aucun;
			}
			int dureeMs = Duree(glissade);
			if (genre == Genre.QuartDeTour)
			{
				MontrerLeQuartDeTour(entite, glissade, dureeMs, maintenant);
				return genre;
			}
			Rayon.NoterLaGlissade(entite.Id, glissade.ArretHeritage, Math.Max(maintenant, glissade.EndsAt), maintenant);
			if (genre == Genre.Technique)
			{
				Boule.SurPoussee(entite, glissade);
			}
			if (!Trajet(glissade, out var ux, out var uy, out var longueur))
			{
				return Genre.Aucun;
			}
			if (genre == Genre.Lancer)
			{
				MontrerLeLancer(entite, glissade, ux, uy, longueur, dureeMs, allure.SillonVieMs, maintenant);
			}
			else
			{
				MontrerLaPoussee(entite, ux, uy, longueur, dureeMs, allure.SillonVieMs, maintenant);
				Kiai.SurPoussee(entite, glissade, dureeMs, maintenant);
			}
			return genre;
		}

		private static void MontrerLeLancer(Entity entite, Dash glissade, float ux, float uy, float longueur, int dureeMs, int sillonVieMs, long maintenant)
		{
			System.Numerics.Vector2 origin = entite.Origin;
			SuivreLeLance(entite, glissade, ux, uy, longueur, maintenant);
			if (longueur >= 24f)
			{
				Traits.Lancer(entite.Id, ux, uy, dureeMs + 80, 5, 30f, maintenant);
			}
			float axe = AxeDeBouffee(ux, uy);
			for (int i = 0; i < 4; i++)
			{
				Coups.BouffeePlusTard(entite.Id, dureeMs * i / 4, (0f - ux) * 8f, (0f - uy) * 8f, 4, axe, in JetDuLancer, maintenant);
			}
			if (TraceDuSillon(origin.X, origin.Y, ux, uy, longueur, out var x, out var y, out var dx, out var dy))
			{
				Sol.Sillon(x, y, dx, dy, dureeMs, sillonVieMs, fin: false, maintenant);
				Son.Frotter(entite, glissade);
			}
		}

		private static void MontrerLaPoussee(Entity entite, float ux, float uy, float longueur, int dureeMs, int sillonVieMs, long maintenant)
		{
			System.Numerics.Vector2 origin = entite.Origin;
			if (longueur >= 8f)
			{
				Sol.Sillon(origin.X, origin.Y + 1f, ux * longueur, uy * longueur, dureeMs, sillonVieMs, fin: false, maintenant);
			}
			if (!(longueur < 24f))
			{
				Traits.Lancer(entite.Id, ux, uy, dureeMs + 20, 4, 30f, maintenant);
			}
		}

		private static void SuivreLeLance(Entity entite, Dash glissade, float ux, float uy, float longueur, long maintenant)
		{
			for (int i = 0; i < _lances; i++)
			{
				if (Lances[i].Corps == entite.Id)
				{
					RetirerLeLance(i);
					break;
				}
			}
			if (Fantomes.DerniereImage(entite.Id, out IGameTexture _, out FloatRect _, out FloatRect destination))
			{
				if (_lances == 16)
				{
					RetirerLeLance(0);
				}
				Lances[_lances++] = new Lance
				{
					Corps = entite.Id,
					Glissade = glissade,
					DepartX = destination.X,
					DepartY = destination.Y,
					Ux = ux,
					Uy = uy,
					Longueur = longueur,
					Depuis = maintenant
				};
			}
		}

		private static void RetirerLeLance(int index)
		{
			_lances--;
			if (index < _lances)
			{
				Array.Copy(Lances, index + 1, Lances, index, _lances - index);
			}
			Lances[_lances] = default(Lance);
		}

		internal static float TrajetFait(float departX, float departY, float x, float y, float ux, float uy)
		{
			return (x - departX) * ux + (y - departY) * uy;
		}

		internal static int PartsPassees(float fait, float longueur)
		{
			int i;
			for (i = 0; i < ImagesDuLancer.Length && fait >= ImagesDuLancer[i] * longueur; i++)
			{
			}
			return i;
		}

		private static void SuivreLesLances(long maintenant)
		{
			int num = 0;
			for (int i = 0; i < _lances; i++)
			{
				ref Lance reference = ref Lances[i];
				if (GarderLeLance(ref reference, maintenant))
				{
					if (num != i)
					{
						Lances[num] = reference;
					}
					num++;
				}
			}
			Array.Clear(Lances, num, _lances - num);
			_lances = num;
		}

		private static bool GarderLeLance(ref Lance lance, long maintenant)
		{
			if (Math.Abs(maintenant - lance.Depuis) > 3000 || !Globals.Entities.TryGetValue(lance.Corps, out Entity value) || value == null || value.Dashing != lance.Glissade || !Fantomes.DerniereImage(lance.Corps, out IGameTexture texture, out FloatRect source, out FloatRect destination) || texture == null)
			{
				return false;
			}
			float num = TrajetFait(lance.DepartX, lance.DepartY, destination.X, destination.Y, lance.Ux, lance.Uy);
			int num2 = PartsPassees(num, lance.Longueur);
			if (num2 <= lance.Passees)
			{
				return true;
			}
			while (lance.Passees < num2)
			{
				float num3 = num - ImagesDuLancer[lance.Passees] * lance.Longueur;
				Fantomes.Noter(lance.Corps, texture, source, new FloatRect(MathF.Round(destination.X - lance.Ux * num3), MathF.Round(destination.Y - lance.Uy * num3), destination.Width, destination.Height));
				Fantomes.Laisser(lance.Corps, 230, 0.8f, CremeDuLancer, 14f, maintenant);
				lance.Passees++;
			}
			Fantomes.Noter(lance.Corps, texture, source, destination);
			return lance.Passees < ImagesDuLancer.Length;
		}

		private static void MontrerLeQuartDeTour(Entity entite, Dash glissade, int dureeMs, long maintenant)
		{
			Fantomes.Laisser(entite.Id, 300, 0.85f, Coups.CremeDeLElan, 1f, maintenant);
			Coups.ImagePlusTard(entite.Id, dureeMs / 2, 280, 0.7f, Coups.CremeDeLElan, 6f, maintenant);
			float sensX = 0f;
			float sensY = 0f;
			if (Trouver(entite.Id, out var index))
			{
				if (Arcs[index].Glissade == null)
				{
					float sensX2 = Arcs[index].SensX;
					sensY = Arcs[index].SensY;
					sensX = sensX2;
				}
				Retirer(index);
			}
			Ajouter(new Arc
			{
				Corps = entite.Id,
				Glissade = glissade,
				Cible = glissade.AuteurId,
				Depuis = maintenant,
				SensX = sensX,
				SensY = sensY
			});
		}

		private static bool Recent(long maintenant, long depuis)
		{
			if (maintenant >= depuis)
			{
				return maintenant - depuis < 250;
			}
			return false;
		}

		private static bool Trouver(Guid corps, out int index)
		{
			for (index = 0; index < _arcs; index++)
			{
				if (Arcs[index].Corps == corps)
				{
					return true;
				}
			}
			return false;
		}

		private static void Retirer(int index)
		{
			_arcs--;
			if (index < _arcs)
			{
				Array.Copy(Arcs, index + 1, Arcs, index, _arcs - index);
			}
			Arcs[_arcs] = default(Arc);
		}

		private static void Ajouter(in Arc arc)
		{
			if (_arcs == 16)
			{
				Retirer(0);
			}
			Arcs[_arcs++] = arc;
		}

		internal static void LaFaucheTouche(Entity attaquant, float ux, float uy, long maintenant)
		{
			if (Trouver(attaquant.Id, out var index))
			{
				ref Arc reference = ref Arcs[index];
				if (reference.Glissade != null && (!reference.Pose || Recent(maintenant, reference.Depuis)))
				{
					return;
				}
				Retirer(index);
			}
			Ajouter(new Arc
			{
				Corps = attaquant.Id,
				Depuis = maintenant,
				SensX = ux,
				SensY = uy
			});
		}

		internal static void Suivre(long maintenant)
		{
			if (_lances > 0)
			{
				SuivreLesLances(maintenant);
			}
			if (_arcs == 0)
			{
				return;
			}
			int num = 0;
			for (int i = 0; i < _arcs; i++)
			{
				ref Arc reference = ref Arcs[i];
				if (Garder(ref reference, maintenant))
				{
					if (num != i)
					{
						Arcs[num] = reference;
					}
					num++;
				}
			}
			Array.Clear(Arcs, num, _arcs - num);
			_arcs = num;
		}

		private static bool Garder(ref Arc arc, long maintenant)
		{
			if (arc.Pose)
			{
				return Recent(maintenant, arc.Depuis);
			}
			Globals.Entities.TryGetValue(arc.Corps, out Entity value);
			Dash glissade = arc.Glissade;
			if (glissade != null)
			{
				if (value != null && value.Dashing == glissade && maintenant < glissade.EndsAt)
				{
					return true;
				}
				if (ALHeure(maintenant, glissade.EndsAt))
				{
					Poser(in arc, value, maintenant);
				}
				arc.Pose = true;
				return Recent(maintenant, arc.Depuis);
			}
			if (Recent(maintenant, arc.Depuis))
			{
				return true;
			}
			if (ALHeure(maintenant, arc.Depuis + 250))
			{
				Poser(in arc, value, maintenant);
			}
			return false;
		}

		private static bool ALHeure(long maintenant, long due)
		{
			return maintenant - due <= 500;
		}

		private static void Poser(in Arc arc, Entity? corps, long maintenant)
		{
			if (corps != null)
			{
				float sensX = arc.SensX;
				float num = arc.SensY;
				float num2 = sensX;
				if (arc.Cible != Guid.Empty && arc.Cible != arc.Corps && Globals.Entities.TryGetValue(arc.Cible, out Entity value) && value != null)
				{
					(num2, num) = Coups.Sens(corps, value);
				}
				else if (num2 == 0f && num == 0f)
				{
					var (ecartX, ecartY) = CorpsACorps.Vecteur(corps.DirectionFacing);
					(num2, num) = Coups.Sens(ecartX, ecartY);
				}
				System.Numerics.Vector2 origin = corps.Origin;
				Sol.Arc(origin.X, origin.Y + 2f, 44f, MathF.Atan2(num, num2), Options.Instance.Combat.Heritage.Allure.ArcVieMs, maintenant);
			}
		}

		internal static void Oublier()
		{
			Array.Clear(Arcs, 0, _arcs);
			_arcs = 0;
			Array.Clear(Lances, 0, _lances);
			_lances = 0;
		}
	}

	internal static class Onde
	{
		internal readonly record struct Langue(float X, float Y, float SensX, int RetardMs);

		internal const float DepartDeProfilPx = 12f;

		internal const float DepartDeFacePx = 8f;

		internal const float LongueurPx = 76f;

		internal const float Ecrasement = 0.6f;

		internal const int OuvreMs = 180;

		internal const int LanguesParLevre = 5;

		internal const float EchelleDesLangues = 1.5f;

		internal static readonly Color TeinteDesLangues = new Color(255, 255, 204, 150);

		internal const float LevrePx = 3f;

		internal const int Grains = 5;

		internal const float GrainsDevantPx = 14f;

		internal const float GrainsDeFaceDevantPx = 8f;

		internal static readonly Coups.Jet JetDesGrains = new Coups.Jet(1.2f, 70f, 190f, 120f, 300);

		private static readonly float[] Levres = new float[2] { -1f, 1f };

		internal const int ArretMs = 60;

		internal const float SecoussePx = 4f;

		internal const int SecousseMs = 200;

		internal const int ChiffreAttenduMs = 300;

		internal static (float X, float Y) AxeDe(Direction sens)
		{
			switch (sens)
			{
			case Direction.Left:
			case Direction.UpLeft:
			case Direction.DownLeft:
				return (X: -1f, Y: 0f);
			case Direction.Right:
			case Direction.UpRight:
			case Direction.DownRight:
				return (X: 1f, Y: 0f);
			case Direction.Up:
				return (X: 0f, Y: -1f);
			default:
				return (X: 0f, Y: 1f);
			}
		}

		internal static (float X, float Y, float Dx, float Dy) Fissure(float piedsX, float piedsY, float ux, float uy)
		{
			return (X: piedsX + ux * 12f, Y: piedsY + 1f + uy * 8f, Dx: ux * 76f, Dy: uy * 76f * 0.6f);
		}

		internal static Langue[] Langues(float x0, float y0, float dx, float dy, float ux, float uy)
		{
			List<Langue> list = new List<Langue>(10);
			float[] levres = Levres;
			foreach (float num in levres)
			{
				for (int j = 0; j < 5; j++)
				{
					float num2 = ((float)j + ((num < 0f) ? 0.5f : 1f)) / 5f;
					float num3 = x0 + dx * num2 - uy * num * 3f;
					float y1 = y0 + dy * num2 + ux * num * 3f;
					float sensX = ((ux != 0f) ? ux : ((num3 > x0) ? 1f : (-1f)));
					list.Add(new Langue(num3, y1, sensX, (int)MathF.Round(num2 * 180f)));
				}
			}
			return list.OrderBy((Langue langue) => langue.Y).ToArray();
		}

		internal static bool Jouer(Entity lanceur, EchangePacket paquet)
		{
			return JouerALHeure(lanceur, paquet, Timing.Global.Milliseconds);
		}

		internal static bool JouerALHeure(Entity lanceur, EchangePacket paquet, long maintenant)
		{
			HeritageOptions heritage = Options.Instance.Combat.Heritage;
			HeritageTechniquesVuOptions techniquesVu = heritage.TechniquesVu;
			if (lanceur == null || paquet == null || !techniquesVu.Actif || !techniquesVu.OndeRefaite || !heritage.Allure.Actif)
			{
				return false;
			}
			(float X, float Y) tuple = AxeDe((Direction)paquet.Valeur);
			float item = tuple.X;
			float item2 = tuple.Y;
			string nom = ((item != 0f) ? "onde_langue" : "onde_langue_v");
			if (!Bandes.Trouver(nom, out var bande) || Bandes.Planche(in bande) == null)
			{
				return false;
			}
			System.Numerics.Vector2 origin = lanceur.Origin;
			var (num, num2, dx, dy) = Fissure(origin.X, origin.Y, item, item2);
			Sol.Sillon(num, num2, dx, dy, 180, heritage.Allure.SillonVieMs, fin: false, maintenant);
			Langue[] array = Langues(num, num2, dx, dy, item, item2);
			for (int i = 0; i < array.Length; i++)
			{
				Langue langue = array[i];
				Effets.Bande(nom, langue.X, langue.Y, langue.SensX, 0f, CoucheDEffet.Sol, 1.5f, Guid.Empty, langue.RetardMs, maintenant, TeinteDesLangues);
			}
			Particules.Lancer(origin.X + item * 14f, origin.Y + item2 * 8f, 5, MathF.Atan2(item2, item), JetDesGrains.Ouvert, JetDesGrains.VMin, JetDesGrains.VMax, JetDesGrains.Gravite, JetDesGrains.VieMs, Coups.Poussiere(origin.X, origin.Y), JetDesGrains.Tirets, float.NaN, 0, null, Globals.Random, maintenant);
			CombatAudio.Play("strongcoup.wav", lanceur, 0.05f);
			if (lanceur == Globals.Me)
			{
				CombatFeedback.ImpactHeritage(60, 4f, 200, item, item2, maintenant);
			}
			Chiffres.Attendre(lanceur.Id, default(Guid), techniquesVu.ChiffreKiaiPt, Chiffres.Casse, 300, maintenant);
			return true;
		}
	}

	internal static class Particules
	{
		internal struct Grain
		{
			public float X;

			public float Y;

			public float Vx;

			public float Vy;

			public float Gravite;

			public long Ne;

			public int PoseMs;

			public int FinMs;

			public float PoseX;

			public float PoseY;

			public byte Large;

			public byte Haut;

			public Color Ton;

			public Color? Cerne;
		}

		internal const int GrainsMax = 300;

		internal const int CarrePx = 2;

		internal const int GrosCarrePx = 3;

		internal const float PartDeGros = 0.35f;

		internal const int TiretMinPx = 3;

		internal const int TiretMaxPx = 5;

		internal const int TiretEpaisPx = 2;

		internal const int VieEnPlusMs = 180;

		internal const float SolEcartPx = 4f;

		internal const float PalitDes = 0.7f;

		internal const int Jamais = int.MaxValue;

		internal static readonly Color[] Sable = new Color[5]
		{
			new Color(255, 80, 50, 36),
			new Color(255, 122, 76, 55),
			new Color(255, 190, 119, 86),
			new Color(255, 217, 176, 157),
			new Color(255, 237, 217, 208)
		};

		internal static readonly Color[] Herbe = new Color[4]
		{
			new Color(255, 54, 45, 11),
			new Color(255, 84, 70, 17),
			new Color(255, 120, 100, 24),
			new Color(255, 161, 147, 93)
		};

		private static readonly Grain[] LesGrains = new Grain[300];

		private static int _nombre;

		private static readonly FloatRect UnPixel = new FloatRect(0f, 0f, 1f, 1f);

		internal static int Nombre => _nombre;

		internal static ReadOnlySpan<Grain> Grains => LesGrains.AsSpan(0, _nombre);

		internal static Color[] TonsDuSol(float x, float y)
		{
			if (!Meteo.EstHerbe(x, y))
			{
				return Sable;
			}
			return Herbe;
		}

		internal static int Lancer(float x, float y, int nombre, float axe, float ouvert, float vMin, float vMax, float gravite, int vieMs, Color[] tons, bool tirets, float solY, int resteMs, Color? cerne, Random hasard, long maintenant)
		{
			if (nombre <= 0 || tons == null || tons.Length <= 0)
			{
				return 0;
			}
			nombre = Math.Min(nombre, 300);
			FaireDeLaPlace(nombre, maintenant);
			for (int i = 0; i < nombre; i++)
			{
				float x2 = axe + ((float)hasard.NextDouble() - 0.5f) * ouvert;
				float num = vMin + (float)hasard.NextDouble() * (vMax - vMin);
				int num2 = Math.Max(1, vieMs) + (int)(hasard.NextDouble() * 180.0);
				ref Grain reference = ref LesGrains[_nombre++];
				reference = new Grain
				{
					X = x,
					Y = y,
					Vx = MathF.Cos(x2) * num,
					Vy = MathF.Sin(x2) * num,
					Gravite = gravite,
					Ne = maintenant,
					PoseMs = int.MaxValue,
					FinMs = num2,
					Ton = tons[hasard.Next(tons.Length)],
					Cerne = cerne
				};
				if (tirets)
				{
					reference.Large = (byte)hasard.Next(3, 6);
					reference.Haut = 2;
				}
				else
				{
					reference.Large = (reference.Haut = (byte)((hasard.NextDouble() < 0.3499999940395355) ? 3u : 2u));
				}
				if (!float.IsNaN(solY))
				{
					float num3 = solY + (float)hasard.NextDouble() * 4f;
					float num4 = HeureDePose(y, reference.Vy, gravite, num3);
					if (!float.IsNaN(num4) && !(num4 * 1000f >= (float)num2))
					{
						reference.PoseMs = (int)(num4 * 1000f);
						reference.PoseX = x + reference.Vx * num4;
						reference.PoseY = num3;
						reference.FinMs = ((resteMs > 0) ? (reference.PoseMs + resteMs) : num2);
					}
				}
			}
			return nombre;
		}

		internal static float HeureDePose(float y, float vy, float gravite, float solY)
		{
			if (float.IsNaN(solY))
			{
				return float.NaN;
			}
			float num = y - solY;
			if (gravite == 0f)
			{
				if (!(vy > 0f))
				{
					return float.NaN;
				}
				return MathF.Max(0f, (0f - num) / vy);
			}
			if (gravite < 0f && vy <= 0f)
			{
				return float.NaN;
			}
			float num2 = vy * vy - 2f * gravite * num;
			if (num2 < 0f)
			{
				if (!(gravite > 0f))
				{
					return float.NaN;
				}
				return MathF.Max(0f, (0f - vy) / gravite);
			}
			return MathF.Max(0f, (0f - vy + MathF.Sqrt(num2)) / gravite);
		}

		internal static bool Etat(in Grain grain, long maintenant, out float x, out float y, out bool pose, out float opacite)
		{
			long num = Math.Max(0L, maintenant - grain.Ne);
			if (num >= grain.FinMs)
			{
				x = (y = (opacite = 0f));
				pose = false;
				return false;
			}
			float num2;
			if (num >= grain.PoseMs)
			{
				pose = true;
				x = grain.PoseX;
				y = grain.PoseY;
				num2 = (float)(num - grain.PoseMs) / (float)(grain.FinMs - grain.PoseMs);
			}
			else
			{
				pose = false;
				float num3 = (float)num / 1000f;
				x = grain.X + grain.Vx * num3;
				y = grain.Y + grain.Vy * num3 + 0.5f * grain.Gravite * num3 * num3;
				num2 = ((grain.PoseMs == int.MaxValue) ? ((float)num / (float)grain.FinMs) : 0f);
			}
			opacite = ((num2 > 0.7f) ? Math.Clamp((1f - num2) / 0.3f, 0f, 1f) : 1f);
			return true;
		}

		private static void FaireDeLaPlace(int nombre, long maintenant)
		{
			if (_nombre + nombre > 300)
			{
				Balayer(maintenant, dessinerLesPoses: false);
				int num = _nombre + nombre - 300;
				if (num > 0)
				{
					Array.Copy(LesGrains, num, LesGrains, 0, _nombre - num);
					Array.Clear(LesGrains, _nombre - num, num);
					_nombre -= num;
				}
			}
		}

		private static void Balayer(long maintenant, bool dessinerLesPoses)
		{
			int num = 0;
			for (int i = 0; i < _nombre; i++)
			{
				ref Grain reference = ref LesGrains[i];
				if (Etat(in reference, maintenant, out var x, out var y, out var pose, out var opacite))
				{
					if (pose && dessinerLesPoses)
					{
						Peindre(in reference, x, y, opacite);
					}
					if (num != i)
					{
						LesGrains[num] = reference;
					}
					num++;
				}
			}
			Array.Clear(LesGrains, num, _nombre - num);
			_nombre = num;
		}

		internal static void Oublier()
		{
			Array.Clear(LesGrains, 0, _nombre);
			_nombre = 0;
		}

		internal static void DessinerPoses(long maintenant)
		{
			Balayer(maintenant, dessinerLesPoses: true);
		}

		internal static void DessinerEnLAir(long maintenant)
		{
			for (int i = 0; i < _nombre; i++)
			{
				ref Grain grain = ref LesGrains[i];
				if (Etat(in grain, maintenant, out var x, out var y, out var pose, out var opacite) && !pose)
				{
					Peindre(in grain, x, y, opacite);
				}
			}
		}

		private static void Peindre(in Grain grain, float x, float y, float opacite)
		{
			float num = MathF.Round(x);
			float num2 = MathF.Round(y);
			IGameTexture texture = Graphics.Renderer?.WhitePixel;
			Color cerne = grain.Cerne;
			if ((object)cerne != null)
			{
				Effets.Peindre(texture, UnPixel, new FloatRect(num - 1f, num2 - 1f, (float)(int)grain.Large + 2f, (float)(int)grain.Haut + 2f), Palie(cerne, opacite), 0f);
			}
			Effets.Peindre(texture, UnPixel, new FloatRect(num, num2, (int)grain.Large, (int)grain.Haut), Palie(grain.Ton, opacite), 0f);
		}

		private static Color Palie(Color ton, float opacite)
		{
			if (!(opacite >= 1f))
			{
				return new Color((int)((float)(int)ton.A * opacite), ton.R, ton.G, ton.B);
			}
			return ton;
		}
	}

	internal static class Poussee
	{
		internal struct Decalage
		{
			private float _deX;

			private float _deY;

			private float _versX;

			private float _versY;

			private float _age;

			private float _duree;

			public bool EnCours { get; private set; }

			public bool EnRetrait { get; private set; }

			public readonly bool Resorbee
			{
				get
				{
					if (EnRetrait)
					{
						return _age >= _duree;
					}
					return false;
				}
			}

			public readonly (float X, float Y) Valeur
			{
				get
				{
					if (!EnCours)
					{
						return (X: 0f, Y: 0f);
					}
					float num = ((_duree <= 0f) ? 1f : Math.Clamp(_age / _duree, 0f, 1f));
					float num2 = 1f - (1f - num) * (1f - num);
					return (X: _deX + (_versX - _deX) * num2, Y: _deY + (_versY - _deY) * num2);
				}
			}

			public void Pousser(float x, float y, float dureeMs = 80f)
			{
				(float, float) valeur = Valeur;
				_deX = valeur.Item1;
				_deY = valeur.Item2;
				_versX = _deX + x;
				_versY = _deY + y;
				_age = 0f;
				_duree = MathF.Max(0f, dureeMs);
				EnCours = true;
				EnRetrait = false;
			}

			public void Retirer()
			{
				if (EnCours && !EnRetrait)
				{
					(float, float) valeur = Valeur;
					_deX = valeur.Item1;
					_deY = valeur.Item2;
					_versX = 0f;
					_versY = 0f;
					_age = 0f;
					_duree = 120f;
					EnRetrait = true;
				}
			}

			public void Avancer(float ecoule)
			{
				if (EnCours)
				{
					_age = MathF.Min(_duree, _age + MathF.Max(0f, ecoule));
				}
			}
		}

		internal const float MonteeMs = 80f;

		internal const float RetraitMs = 120f;

		private const float PasDeGlissade = 4f;

		private const float JeuDuPremierContact = 0.5f;

		private static readonly float[] PasDAffinage = new float[2] { 2f, 1f };

		private const float AvanceMinDHier = 0.2f;

		private static readonly List<Guid> Suivies = new List<Guid>(4);

		internal static int EnCours => Suivies.Count;

		internal static (float X, float Y) Borner(float x, float y, float sensX, float sensY, float distance, float rayon, int largeurCase, int hauteurCase, CorpsLibre.EstBloquee murs, CorpsLibre.LignesDeLaCase? lignes, bool premierContact = false)
		{
			float num = 0f;
			while (num < distance)
			{
				float num2 = MathF.Min(4f, distance - num);
				var (num3, num4) = Essayer(x, y, sensX, sensY, num2, rayon, largeurCase, hauteurCase, murs, lignes);
				if (premierContact)
				{
					if (!EstLibre(x, y, sensX, sensY, num2, num3, num4))
					{
						float num5 = num2;
						float[] pasDAffinage = PasDAffinage;
						foreach (float num6 in pasDAffinage)
						{
							if (!(num6 >= num5))
							{
								var (num7, num8) = Essayer(x, y, sensX, sensY, num6, rayon, largeurCase, hauteurCase, murs, lignes);
								if (EstLibre(x, y, sensX, sensY, num6, num7, num8))
								{
									x = num7;
									y = num8;
									num5 -= num6;
								}
							}
						}
						break;
					}
					x = num3;
					y = num4;
					num += num2;
				}
				else
				{
					float num9 = MathF.Sqrt((num3 - x) * (num3 - x) + (num4 - y) * (num4 - y));
					if (num9 < num2 * 0.2f)
					{
						break;
					}
					x = num3;
					y = num4;
					num += num9;
				}
			}
			return (X: x, Y: y);
		}

		private static (float X, float Y) Essayer(float x, float y, float sensX, float sensY, float pas, float rayon, int largeurCase, int hauteurCase, CorpsLibre.EstBloquee murs, CorpsLibre.LignesDeLaCase? lignes)
		{
			var (item, num) = CorpsLibre.Deplacer(x, y + 6f, sensX * pas, sensY * pas, largeurCase, hauteurCase, murs, rayon, 0f, null, lignes);
			return (X: item, Y: num - 6f);
		}

		private static bool EstLibre(float x, float y, float sensX, float sensY, float pas, float renduX, float renduY)
		{
			float num = renduX - (x + sensX * pas);
			float num2 = renduY - (y + sensY * pas);
			return num * num + num2 * num2 < 0.25f;
		}

		internal static float Duree(int reculMs, int pousseeMs, float parcouru, float longueur)
		{
			if (reculMs <= 0)
			{
				return 80f;
			}
			int num = Math.Max(reculMs, pousseeMs);
			if (!(longueur > 0f))
			{
				return num;
			}
			return (float)num * Math.Clamp(parcouru / longueur, 0f, 1f);
		}

		internal static bool PeutPousser(Entity cible)
		{
			bool flag = !cible.ReculeSousLesCoups;
			return HeritageRegles.PeutEtrePoussee(cible.EstMonstreAuPixel, !cible.IsDying && !cible.IsDisposed, Echanges.EstLourd(cible) || cible.MassiveSousLesCoups, cible.IsBlocking, flag, flag);
		}

		internal static bool Predire(Player moi, Entity cible, CoupHeritage coup)
		{
			CoupHeritageOptions coupHeritageOptions = HeritageRegles.Table(coup);
			if (!HeritageRegles.Actif || !coupHeritageOptions.PousseePredite || coupHeritageOptions.ReculPx <= 0 || cible == moi)
			{
				return false;
			}
			if (Actif && Riposte.Tenace(cible))
			{
				return false;
			}
			if (cible.Dashing != null || cible.DashQueue.Count > 0 || (!cible.EstPiloteLibrement && (cible.IsMoving || cible.SeRecentre)))
			{
				return false;
			}
			if (!PeutPousser(cible) || !moi.PositionMonde(out var mondeX, out var mondeY) || !cible.PositionMonde(out var mondeX2, out var mondeY2))
			{
				return false;
			}
			float num = mondeX2 - mondeX;
			float num2 = mondeY2 - mondeY;
			float num3 = MathF.Sqrt(num * num + num2 * num2);
			if (!(num3 > 0.5f))
			{
				(num, num2) = CorpsACorps.Vecteur(moi.DirectionFacing);
			}
			else
			{
				float num4 = num / num3;
				float num5 = num2 / num3;
				num2 = num5;
				num = num4;
			}
			(float, float, float) tuple2 = HeritageRegles.Recul(coup, num, num2, massif: false);
			if (tuple2.Item3 <= 0f)
			{
				return false;
			}
			(float X, float Y) tuple3 = moi.BornerUnePousseeHeritage(cible, mondeX2, mondeY2, tuple2.Item1, tuple2.Item2, tuple2.Item3);
			float item = tuple3.X;
			float item2 = tuple3.Y;
			float num6 = item - mondeX2;
			float num7 = item2 - mondeY2;
			float num8 = MathF.Sqrt(num6 * num6 + num7 * num7);
			if (num8 < 0.5f)
			{
				return false;
			}
			cible.CommencerUnePousseeHeritage(num6, num7, DeplacementLibre.MaintenantLocal, Duree(coupHeritageOptions.ReculMs, Options.Instance.Combat.Heritage.PousseeMs, num8, tuple2.Item3));
			if (!Suivies.Contains(cible.Id))
			{
				Suivies.Add(cible.Id);
			}
			return true;
		}

		internal static void Suivre(Player moi)
		{
			if (Suivies.Count == 0)
			{
				return;
			}
			float ecoule = Math.Clamp(moi.elapsedtime, 0f, 50f);
			long maintenantLocal = DeplacementLibre.MaintenantLocal;
			int pousseeAttenteMs = Options.Instance.Combat.Heritage.PousseeAttenteMs;
			for (int num = Suivies.Count - 1; num >= 0; num--)
			{
				if (!Globals.Entities.TryGetValue(Suivies[num], out Entity value) || value == null || value.IsDisposed || !value.AvancerLaPousseeHeritage(ecoule, maintenantLocal, pousseeAttenteMs))
				{
					Suivies.RemoveAt(num);
				}
			}
		}

		internal static void Retirer(Guid cibleId)
		{
			if (Suivies.Contains(cibleId) && Globals.Entities.TryGetValue(cibleId, out Entity value))
			{
				value?.RetirerLaPousseeHeritage();
			}
		}

		internal static void Reprise(Entity entite)
		{
			if (HeritageRegles.Actif)
			{
				if (entite.DashQueue.TryPeek(out Dash result) && result.Nature == NatureDeGlissade.Pousse)
				{
					entite.ReculeSousLesCoups = true;
					entite.MassiveSousLesCoups = (result.FormeHeritage & FormeDeLancer.Lourd) != 0;
				}
				if (entite.PousseeHeritageEnCours)
				{
					entite.OublierLaPousseeHeritage();
					Suivies.Remove(entite.Id);
				}
			}
		}

		internal static bool IgnoreLaPosition(Entity entite, long tempsServeur)
		{
			if (!HeritageRegles.Actif)
			{
				return false;
			}
			Dash dashing = entite.Dashing;
			if (dashing != null && dashing.Nature == NatureDeGlissade.Pousse && tempsServeur <= dashing.FinAuServeur)
			{
				return true;
			}
			foreach (Dash item in entite.DashQueue)
			{
				if (item.Nature == NatureDeGlissade.Pousse && tempsServeur <= item.FinAuServeur)
				{
					return true;
				}
			}
			entite.ReprendreSesPositionsSousLaPousseeHeritage(tempsServeur);
			return false;
		}

		internal static void Oublier()
		{
			foreach (Guid suivy in Suivies)
			{
				if (Globals.Entities.TryGetValue(suivy, out Entity value))
				{
					value?.OublierLaPousseeHeritage();
				}
			}
			Suivies.Clear();
		}
	}

	internal static class Rang
	{
		internal static void SurEsquive(VanishPacket paquet)
		{
			Player me = Globals.Me;
			bool flag = me == null || paquet.EntityId != me.Id;
			bool flag2 = flag;
			if (!flag2)
			{
				VanishEvent vanishEvent = paquet.Event;
				bool flag3 = vanishEvent <= VanishEvent.Evaded;
				flag2 = !flag3;
			}
			if (!flag2)
			{
				me.GarderLeRangHeritage();
			}
		}

		internal static void SurGarde(GuardPacket paquet)
		{
			Player me = Globals.Me;
			if (me != null && !(paquet.EntityId != me.Id) && paquet.Event == GuardEvent.Perfect)
			{
				me.GarderLeRangHeritage();
			}
		}

		internal static void SurGlissade(Entity entite, NatureDeGlissade nature)
		{
			if (nature == NatureDeGlissade.Dash)
			{
				Player me = Globals.Me;
				if (me != null && entite == me)
				{
					me.GarderLeRangHeritage();
				}
			}
		}
	}

	internal static class Rayon
	{
		private struct Charge
		{
			public Guid Tireur;

			public int Palier;

			public bool Vue;
		}

		private struct Tir
		{
			public Guid Tireur;

			public bool Vu;

			public bool DeMoi;

			public Guid Clouee;

			public long CloueeDepuis;

			public bool Choc;

			public long GrainsA;
		}

		private struct Arret
		{
			public Guid Corps;

			public long A;
		}

		internal static readonly Color Nuit = new Color(255, 10, 31, 102);

		private static readonly Color Blanc = new Color(255, 255, 255, 255);

		internal static readonly (string Fond, string Devant)[] Couronnes = new(string, string)[3]
		{
			("charge_fond", "charge_devant"),
			("charge_fond_2", "charge_devant_2"),
			("charge_fond_3", "charge_devant_3")
		};

		internal const int ChargesMax = 16;

		internal const int TirsMax = 16;

		internal const int ArretsMax = 32;

		internal const int TalonsVieMs = 5000;

		internal const float SecousseDuTirPx = 3f;

		internal const int SecousseDuTirMs = 200;

		internal const int CloueApresArretMs = 3000;

		internal const float TeteSurLaCiblePx = 2f;

		internal const int GrainsToutesLesMs = 110;

		internal const int Grains = 5;

		internal static readonly Coups.Jet JetDesGrains = new Coups.Jet(2.8f, 60f, 180f, 0f, 240, Tirets: false);

		internal static readonly Color[] TonsDesGrains = new Color[3]
		{
			new Color(255, 108, 203, 255),
			new Color(255, 207, 244, 255),
			new Color(255, 255, 255, 255)
		};

		internal const float LezardesAuDessusDesPiedsPx = 18f;

		internal const float LezardesVersLeMurPx = 6f;

		private static readonly Charge[] LesCharges = new Charge[16];

		private static int _charges;

		private static readonly Tir[] LesTirs = new Tir[16];

		private static int _tirs;

		private static readonly Arret[] LesArrets = new Arret[32];

		private static int _arrets;

		internal static bool SansReculDessine => Options.Instance.Combat.Heritage.Techniques.RayonReculPx > 0;

		internal static Color? Cerne
		{
			get
			{
				HeritageTechniquesVuOptions techniquesVu = Options.Instance.Combat.Heritage.TechniquesVu;
				if (techniquesVu == null || !techniquesVu.Actif || techniquesVu.RayonCerneDixiemes <= 0)
				{
					return null;
				}
				return Nuit;
			}
		}

		internal static bool SansAnneau
		{
			get
			{
				if (CouronneVoulue)
				{
					return CouronnesPretes();
				}
				return false;
			}
		}

		private static bool Suivi
		{
			get
			{
				HeritageOptions heritage = Options.Instance.Combat.Heritage;
				if (heritage.TechniquesVu.Actif)
				{
					return heritage.Allure.Actif;
				}
				return false;
			}
		}

		private static bool CouronneVoulue
		{
			get
			{
				if (Suivi)
				{
					return Options.Instance.Combat.Heritage.TechniquesVu.ChargeCouronne;
				}
				return false;
			}
		}

		internal static int Charges => _charges;

		internal static int Tirs => _tirs;

		internal static int Nombre => _charges + _tirs;

		internal static int Arrets => _arrets;

		internal static bool XAnnule(bool reglage, BeamPhase phase)
		{
			if (reglage)
			{
				return phase == BeamPhase.Charging;
			}
			return false;
		}

		internal static bool XAnnuleLaCharge(Entity tireur)
		{
			return XAnnule(Options.Instance.Combat.Heritage.Techniques.RayonXAnnule, KiBeam.PhaseOf(tireur));
		}

		internal static float Epaisseur(int palier, float dAujourdhui)
		{
			HeritageTechniquesVuOptions techniquesVu = Options.Instance.Combat.Heritage.TechniquesVu;
			if (!techniquesVu.Actif)
			{
				return dAujourdhui;
			}
			int num = ((palier <= 0) ? techniquesVu.RayonEpaisseur1Px : ((palier == 1) ? techniquesVu.RayonEpaisseur2Px : techniquesVu.RayonEpaisseur3Px));
			if (num <= 0)
			{
				return dAujourdhui;
			}
			return num;
		}

		private static bool CouronnesPretes()
		{
			(string, string)[] couronnes = Couronnes;
			for (int i = 0; i < couronnes.Length; i++)
			{
				var (nom, nom2) = couronnes[i];
				if (!Prete(nom) || !Prete(nom2))
				{
					return false;
				}
			}
			return true;
		}

		private static bool Prete(string nom)
		{
			if (Bandes.Trouver(nom, out var bande))
			{
				return Bandes.Planche(in bande) != null;
			}
			return false;
		}

		internal static void SuivreLaCharge(Entity tireur, long chargeMs)
		{
			if (tireur == null || !Suivi)
			{
				return;
			}
			int palier = Math.Clamp(Options.Instance.Beam.LevelFor(chargeMs), 0, Couronnes.Length - 1);
			for (int i = 0; i < _charges; i++)
			{
				if (LesCharges[i].Tireur == tireur.Id)
				{
					LesCharges[i].Palier = palier;
					LesCharges[i].Vue = true;
					return;
				}
			}
			if (_charges == 16)
			{
				Array.Copy(LesCharges, 1, LesCharges, 0, --_charges);
			}
			LesCharges[_charges++] = new Charge
			{
				Tireur = tireur.Id,
				Palier = palier,
				Vue = true
			};
		}

		private static int LaChargeDe(Guid tireur)
		{
			for (int i = 0; i < _charges; i++)
			{
				if (LesCharges[i].Tireur == tireur)
				{
					return i;
				}
			}
			return -1;
		}

		internal static bool ChargeVue(Guid tireur)
		{
			if (_charges == 0)
			{
				return false;
			}
			int num = LaChargeDe(tireur);
			if (num >= 0)
			{
				return LesCharges[num].Vue;
			}
			return false;
		}

		internal static void DessinerLaCouronne(Entity entite, bool derriere, long maintenant)
		{
			int num = LaChargeDe(entite.Id);
			if (num < 0 || !LesCharges[num].Vue || !CouronneVoulue)
			{
				return;
			}
			var (text, text2) = Couronnes[LesCharges[num].Palier];
			if (!Bandes.Trouver(derriere ? text : text2, out var bande))
			{
				return;
			}
			IGameTexture gameTexture = Bandes.Planche(in bande);
			if (gameTexture != null)
			{
				int num2 = Bandes.ImageEnBoucle(in bande, maintenant);
				if (num2 >= 0)
				{
					var (x, y) = Effets.PlaceDe(entite);
					var (source, destination, rotation) = Bandes.Rectangle(in bande, x, y, 1f, 0f, 1f, num2);
					Effets.Peindre(gameTexture, source, destination, Blanc, rotation);
				}
			}
		}

		internal static void Tete(Entity tireur, float x, float y, float cos, float sin, float epaisseur, Guid cibleId)
		{
			TeteALHeure(tireur, x, y, cos, sin, cibleId, Timing.Global.Milliseconds);
		}

		internal static void TeteALHeure(Entity tireur, float x, float y, float cos, float sin, Guid cibleId, long maintenant)
		{
			HeritageOptions heritage = Options.Instance.Combat.Heritage;
			HeritageTechniquesVuOptions techniquesVu = heritage.TechniquesVu;
			if (tireur == null || !Suivi)
			{
				return;
			}
			bool flag = tireur == Globals.Me;
			int num = LeTirDe(tireur.Id);
			if (num < 0)
			{
				if (_tirs == 16)
				{
					Array.Copy(LesTirs, 1, LesTirs, 0, --_tirs);
				}
				num = _tirs++;
				LesTirs[num] = new Tir
				{
					Tireur = tireur.Id,
					DeMoi = flag
				};
				AuDepart(tireur, cos, sin, flag, maintenant);
			}
			ref Tir reference = ref LesTirs[num];
			reference.Vu = true;
			if (cibleId == Guid.Empty || !Globals.Entities.TryGetValue(cibleId, out Entity value) || value == null || value.IsDisposed || !TeteSur(value, x, y, cos, sin))
			{
				reference.Clouee = Guid.Empty;
				return;
			}
			if (maintenant - reference.GrainsA >= 110 || maintenant < reference.GrainsA)
			{
				reference.GrainsA = maintenant;
				Particules.Lancer(x, y, 5, MathF.Atan2(0f - sin, 0f - cos), JetDesGrains.Ouvert, JetDesGrains.VMin, JetDesGrains.VMax, JetDesGrains.Gravite, JetDesGrains.VieMs, TonsDesGrains, JetDesGrains.Tirets, float.NaN, 0, Nuit, Globals.Random, maintenant);
			}
			if (!EstClouee(cibleId, maintenant))
			{
				reference.Clouee = Guid.Empty;
				return;
			}
			if (reference.Clouee != cibleId)
			{
				reference.Clouee = cibleId;
				reference.CloueeDepuis = maintenant;
			}
			if (!reference.Choc)
			{
				reference.Choc = true;
				if (flag)
				{
					CombatFeedback.ImpactHeritage(techniquesVu.RayonArretChocMs, (float)techniquesVu.RayonSecousseDixiemes / 10f, techniquesVu.RayonSecousseMs, cos, sin, maintenant);
				}
				System.Numerics.Vector2 origin = value.Origin;
				Lezardes.Poser(origin.X + cos * 6f, origin.Y - 18f, techniquesVu.LezardesNombre, techniquesVu.LezardesPx, maintenant);
			}
		}

		private static int LeTirDe(Guid tireur)
		{
			for (int i = 0; i < _tirs; i++)
			{
				if (LesTirs[i].Tireur == tireur)
				{
					return i;
				}
			}
			return -1;
		}

		private static void AuDepart(Entity tireur, float cos, float sin, bool deMoi, long maintenant)
		{
			HeritageOptions heritage = Options.Instance.Combat.Heritage;
			float num = (float)heritage.Techniques.RayonReculPx - 2f;
			if (num >= 1f)
			{
				System.Numerics.Vector2 origin = tireur.Origin;
				Sol.Talons(origin.X, origin.Y + 1f, (0f - cos) * num, (0f - sin) * num, DureeDuTir(tireur.Id), 5000, maintenant);
			}
			if (deMoi)
			{
				CombatFeedback.ImpactHeritage(heritage.TechniquesVu.RayonArretTirMs, 3f, 200, cos, sin, maintenant);
			}
		}

		private static int DureeDuTir(Guid tireur)
		{
			int[] holdMs = Options.Instance.Beam.HoldMs;
			if (holdMs == null || holdMs.Length <= 0)
			{
				return 0;
			}
			int num = LaChargeDe(tireur);
			return Math.Max(0, holdMs[Math.Clamp((num >= 0) ? LesCharges[num].Palier : 0, 0, holdMs.Length - 1)]);
		}

		internal static bool TeteSur(Entity cible, float x, float y, float cos, float sin)
		{
			FloatRect worldPos = cible.WorldPos;
			float num = worldPos.X + worldPos.Width * 0.5f;
			float num2 = worldPos.Y + worldPos.Height * 0.65f;
			float num3 = (num - x) * cos + (num2 - y) * sin - (MathF.Abs(cos) * worldPos.Width * 0.25f + MathF.Abs(sin) * worldPos.Height * 0.2f);
			return num3 <= 2f;
		}

		internal static void NoterLaGlissade(Guid corps, MatiereDArret arret, long arriveeA, long maintenant)
		{
			bool flag = ContreDuDur(corps, maintenant);
			int num = 0;
			for (int i = 0; i < _arrets; i++)
			{
				if (!(LesArrets[i].Corps == corps) && maintenant - LesArrets[i].A < 3000)
				{
					LesArrets[num++] = LesArrets[i];
				}
			}
			if (HeritageRegles.EstDur(arret) && corps != Guid.Empty)
			{
				if (num == 32)
				{
					Array.Copy(LesArrets, 1, LesArrets, 0, --num);
				}
				LesArrets[num++] = new Arret
				{
					Corps = corps,
					A = (flag ? Math.Min(arriveeA, maintenant) : arriveeA)
				};
			}
			if (num < _arrets)
			{
				Array.Clear(LesArrets, num, _arrets - num);
			}
			_arrets = num;
		}

		internal static bool ContreDuDur(Guid corps, long maintenant)
		{
			for (int i = 0; i < _arrets; i++)
			{
				if (LesArrets[i].Corps == corps)
				{
					if (maintenant >= LesArrets[i].A)
					{
						return maintenant - LesArrets[i].A < 3000;
					}
					return false;
				}
			}
			return false;
		}

		internal static bool EstClouee(Guid cibleId, long maintenant)
		{
			if (!EtatsVus.Vif(cibleId, EtatHeritage.Plante, maintenant))
			{
				return ContreDuDur(cibleId, maintenant);
			}
			return true;
		}

		internal static bool CloueParMoi(Guid cibleId)
		{
			if (cibleId == Guid.Empty)
			{
				return false;
			}
			for (int i = 0; i < _tirs; i++)
			{
				if (LesTirs[i].DeMoi && LesTirs[i].Clouee == cibleId)
				{
					return true;
				}
			}
			return false;
		}

		internal static void DessinerLesCloues(long maintenant)
		{
			int clouePeriodeMs = Options.Instance.Combat.Heritage.TechniquesVu.ClouePeriodeMs;
			if (clouePeriodeMs <= 0)
			{
				return;
			}
			for (int i = 0; i < _tirs; i++)
			{
				ref Tir reference = ref LesTirs[i];
				if (reference.Vu && !(reference.Clouee == Guid.Empty) && Fantomes.DerniereImage(reference.Clouee, out IGameTexture texture, out FloatRect source, out FloatRect destination) && texture != null)
				{
					IGameTexture gameTexture = ((Fantomes.Masque != null) ? Fantomes.Masque(texture, source) : KiCharge.MaskOf(texture, source, 0));
					if (gameTexture != null)
					{
						long num = Math.Max(0L, maintenant - reference.CloueeDepuis);
						Effets.Peindre(gameTexture, new FloatRect(0f, 0f, gameTexture.Width, gameTexture.Height), destination, (num / clouePeriodeMs % 2 == 0L) ? Nuit : Blanc, 0f);
					}
				}
			}
		}

		internal static void Battre()
		{
			int num = 0;
			for (int i = 0; i < _charges; i++)
			{
				if (LesCharges[i].Vue)
				{
					LesCharges[i].Vue = false;
					LesCharges[num++] = LesCharges[i];
				}
			}
			Array.Clear(LesCharges, num, _charges - num);
			_charges = num;
			int num2 = 0;
			for (int j = 0; j < _tirs; j++)
			{
				if (LesTirs[j].Vu)
				{
					LesTirs[j].Vu = false;
					LesTirs[num2++] = LesTirs[j];
				}
			}
			Array.Clear(LesTirs, num2, _tirs - num2);
			_tirs = num2;
		}

		internal static void Oublier()
		{
			Array.Clear(LesCharges, 0, _charges);
			_charges = 0;
			Array.Clear(LesTirs, 0, _tirs);
			_tirs = 0;
			Array.Clear(LesArrets, 0, _arrets);
			_arrets = 0;
		}
	}

	internal static class Riposte
	{
		internal static bool Allumee => Options.Instance.Combat.Heritage.Riposte?.Actif ?? false;

		internal static bool Tenace(Guid id, long maintenant)
		{
			if (Allumee)
			{
				return EtatsVus.Vif(id, EtatHeritage.Tenace, maintenant);
			}
			return false;
		}

		internal static bool Tenace(Entity entite)
		{
			return Tenace(entite.Id, Timing.Global.Milliseconds);
		}

		internal static bool LueurSurUnCorpsTenace(float piedsX, float piedsY, float hauteur, float rayon, float ux, float uy)
		{
			var (x, y) = Coups.Contact(piedsX, piedsY, hauteur, rayon, ux, uy);
			KiLight.AddFlash(x, y, Coups.Or, small: true);
			return true;
		}

		internal static void SurRiposte(Entity entite)
		{
			Player me = Globals.Me;
			if (me != null && entite == me)
			{
				me.RecommencerLaSerieHeritage();
			}
		}
	}

	internal static class Sol
	{
		internal struct Alea
		{
			private uint _etat;

			public Alea(uint graine)
			{
				_etat = graine;
			}

			public double Suivant()
			{
				_etat = _etat * 1664525 + 1013904223;
				return (double)_etat / 4294967296.0;
			}
		}

		internal readonly record struct Toile(byte[] Rgba, int Largeur, int Hauteur, double OrigineX, double OrigineY);

		internal enum Genre : byte
		{
			Sillon,
			SillonFin,
			Talons,
			Arc,
			Fissures
		}

		private struct Marque
		{
			public Genre Genre;

			public float X;

			public float Y;

			public float Dx;

			public float Dy;

			public Color[] Tons;

			public long Nee;

			public int PousseMs;

			public int VieMs;

			public IGameTexture? Texture;

			public bool ALaMarque;

			public int Gauche;

			public int Haut;
		}

		internal const int MarquesMax = 24;

		internal const int FonduMs = 600;

		internal const int MargePx = 10;

		internal const int TraitMaxPx = 384;

		internal const double TalonsDemiEcartPx = 3.0;

		internal const int TalonsGraineA = 3;

		internal const int TalonsGraineB = 5;

		internal const int SillonGraine = 1;

		internal const double ArcAplati = 0.36;

		internal const double ArcOuvertureRad = 5.4;

		internal const double ArcPasPx = 1.4;

		internal const int ArcMargePx = 6;

		internal const uint ArcGraine = 771u;

		internal const int ArcRayonMinPx = 4;

		internal const int ArcRayonMaxPx = 192;

		internal const string FissuresFichier = "ssj_cracks.png";

		internal const int FissuresLargeurPx = 64;

		internal const int FissuresMsParImage = 60;

		internal const int FissuresVieMs = 5000;

		private static readonly int[] LargeursDuSillon = new int[10] { 3, 4, 5, 4, 3, 4, 5, 5, 4, 3 };

		private static readonly int[] LargeursDuTraitFin = new int[5] { 1, 2, 2, 1, 2 };

		private static readonly (int Travers, int Long)[] Bourrelet = new(int, int)[7]
		{
			(-3, 1),
			(-2, 2),
			(-1, 3),
			(0, 3),
			(1, 3),
			(2, 2),
			(3, 1)
		};

		private static readonly Color Plein = new Color(255, 255, 255, 255);

		private static readonly Marque[] LesMarques = new Marque[24];

		private static int _nombre;

		private static long _fabriquees;

		internal static Func<string, int, int, byte[], IGameTexture?>? Fabrique;

		internal static int Nombre => _nombre;

		private static int Arrondi(double valeur)
		{
			return (int)Math.Floor(valeur + 0.5);
		}

		internal static Toile PeindreSillon(double dx, double dy, bool fin, int graine, Color[] tons)
		{
			int num = (int)Math.Ceiling(Math.Abs(dx)) + 20;
			int num2 = (int)Math.Ceiling(Math.Abs(dy)) + 20;
			double num3 = 10.0 - Math.Min(0.0, dx);
			double num4 = 10.0 - Math.Min(0.0, dy);
			byte[] rgba = new byte[num * num2 * 4];
			Tracer(rgba, num, num2, num3, num4, dx, dy, fin, graine, tons);
			return new Toile(rgba, num, num2, num3, num4);
		}

		internal static Toile PeindreTalons(double dx, double dy, Color[] tons)
		{
			int num = 10 + (int)Math.Ceiling(3.0);
			int num2 = (int)Math.Ceiling(Math.Abs(dx)) + 2 * num;
			int num3 = (int)Math.Ceiling(Math.Abs(dy)) + 2 * num;
			double num4 = (double)num - Math.Min(0.0, dx);
			double num5 = (double)num - Math.Min(0.0, dy);
			byte[] rgba = new byte[num2 * num3 * 4];
			double num6 = Math.Sqrt(dx * dx + dy * dy);
			double num7 = (0.0 - dy) / num6 * 3.0;
			double num8 = dx / num6 * 3.0;
			Tracer(rgba, num2, num3, num4 + num7, num5 + num8, dx, dy, fin: true, 3, tons);
			Tracer(rgba, num2, num3, num4 - num7, num5 - num8, dx, dy, fin: true, 5, tons);
			return new Toile(rgba, num2, num3, num4, num5);
		}

		internal static Toile PeindreArc(double rayon, double angle, Color[] tons)
		{
			double num = rayon * 0.36;
			int num2 = (int)Math.Ceiling(rayon * 2.0) + 12;
			int num3 = (int)Math.Ceiling(num * 2.0) + 12;
			byte[] rgba = new byte[num2 * num3 * 4];
			Color ton = Ton(tons, 0);
			Color ton2 = Ton(tons, 1);
			Color ton3 = Ton(tons, 4);
			Alea alea = new Alea(771u);
			int num4 = Arrondi(5.4 * rayon / 1.4);
			for (int i = 0; i < num4; i++)
			{
				double num5 = angle - 2.7 + (double)i / (double)num4 * 5.4;
				if (i * 7 % 11 >= 2)
				{
					int num6 = Arrondi(rayon + 6.0 + Math.Cos(num5) * rayon);
					int num7 = Arrondi(num + 6.0 + Math.Sin(num5) * num);
					int l = 2 + (int)(alea.Suivant() * 2.0);
					Remplir(rgba, num2, num3, num6 + 1, num7 + 1, l, 2, ton3);
					Remplir(rgba, num2, num3, num6, num7, l, 2, ton2);
					Remplir(rgba, num2, num3, num6, num7, l, 1, ton);
				}
			}
			return new Toile(rgba, num2, num3, rayon + 6.0, num + 6.0);
		}

		private static void Tracer(byte[] rgba, int largeur, int hauteur, double x0, double y0, double dx, double dy, bool fin, int graine, Color[] tons)
		{
			double num = Math.Sqrt(dx * dx + dy * dy);
			double num2 = dx / num;
			double num3 = dy / num;
			Color color = Ton(tons, 0);
			Color color2 = Ton(tons, 1);
			Color color3 = Ton(tons, 3);
			Alea alea = new Alea((uint)(graine * 7919 + Arrondi(num) * 13));
			int[] array = (fin ? LargeursDuTraitFin : LargeursDuSillon);
			int num4 = (int)Math.Floor(num) + 1;
			int[] array2 = new int[num4];
			int[] array3 = new int[num4];
			int num5 = 2;
			int num6 = 0;
			int num7 = 0;
			double num8 = 0.0;
			for (int i = 0; i < num4; i++)
			{
				if ((double)i >= num8)
				{
					num5 = ((i < 4) ? 2 : array[(num7 + (int)(alea.Suivant() * 3.0)) % array.Length]);
					num6 = ((i >= 8) ? ((int)(alea.Suivant() * 3.0) - 1) : 0);
					num8 = (double)(i + 7) + alea.Suivant() * 5.0;
					num7++;
				}
				array2[i] = num5;
				array3[i] = num6;
			}
			for (int j = 0; j < 3; j++)
			{
				Color ton = ((j == 0) ? color3 : ((j == 1 && !fin) ? color2 : color));
				int num9 = ((j == 2) ? 1 : 0);
				int num10 = ((j == 0) ? 1 : 0);
				for (int k = 0; k < num4; k++)
				{
					int num11 = array2[k] - num9 * 2;
					if (num11 >= 1)
					{
						Remplir(rgba, largeur, hauteur, Arrondi(x0 + num2 * (double)k - num3 * (double)array3[k] - (double)array2[k] / 2.0) + num9 + num10, Arrondi(y0 + num3 * (double)k + num2 * (double)array3[k] - (double)array2[k] / 2.0) + num9 + num10, num11, num11, ton);
					}
				}
			}
			if (fin)
			{
				return;
			}
			double num12 = 6.0 + alea.Suivant() * 5.0;
			int num13 = ((alea.Suivant() < 0.5) ? 1 : (-1));
			for (int l = 0; l < num4; l++)
			{
				if (!((double)l < num12))
				{
					double num14 = (double)num13 * ((double)array2[l] / 2.0 + 2.0 + alea.Suivant() * 2.0);
					int num15 = Arrondi(x0 + num2 * (double)l - num3 * num14);
					int num16 = Arrondi(y0 + num3 * (double)l + num2 * num14);
					int l2 = ((alea.Suivant() < 0.4) ? 3 : 2);
					Remplir(rgba, largeur, hauteur, num15 + 1, num16 + 1, l2, 1, color);
					Remplir(rgba, largeur, hauteur, num15, num16, l2, 1, color3);
					num12 = (double)(l + 9) + alea.Suivant() * 6.0;
					num13 = -num13;
				}
			}
			bool flag = Math.Abs(num2) >= Math.Abs(num3);
			(int, int)[] bourrelet = Bourrelet;
			for (int m = 0; m < bourrelet.Length; m++)
			{
				(int, int) tuple = bourrelet[m];
				int item = tuple.Item1;
				int item2 = tuple.Item2;
				double num17 = x0 + num2 * (num + 2.0) - num3 * (double)item;
				double num18 = y0 + num3 * (num + 2.0) + num2 * (double)item;
				int h;
				int l3;
				int num21;
				int num22;
				if (flag)
				{
					int num19 = Arrondi((num2 > 0.0) ? num17 : (num17 - (double)item2));
					int num20 = Arrondi(num18);
					h = 1;
					l3 = item2;
					num21 = num20;
					num22 = num19;
				}
				else
				{
					int num23 = Arrondi(num17);
					int num24 = Arrondi((num3 > 0.0) ? num18 : (num18 - (double)item2));
					h = item2;
					l3 = 1;
					num21 = num24;
					num22 = num23;
				}
				Remplir(rgba, largeur, hauteur, num22 + 1, num21 + 1, l3, h, color);
				Remplir(rgba, largeur, hauteur, num22, num21, l3, h, color3);
			}
		}

		private static Color Ton(Color[] tons, int rang)
		{
			return tons[Math.Min(rang, tons.Length - 1)];
		}

		private static void Remplir(byte[] rgba, int largeur, int hauteur, int x, int y, int l, int h, Color ton)
		{
			int num = Math.Min(largeur, x + l);
			int num2 = Math.Min(hauteur, y + h);
			for (int i = Math.Max(0, y); i < num2; i++)
			{
				for (int j = Math.Max(0, x); j < num; j++)
				{
					int num3 = (i * largeur + j) * 4;
					rgba[num3] = ton.R;
					rgba[num3 + 1] = ton.G;
					rgba[num3 + 2] = ton.B;
					rgba[num3 + 3] = byte.MaxValue;
				}
			}
		}

		internal static bool Sillon(float x, float y, float dx, float dy, int pousseMs, int vieMs, bool fin, long maintenant)
		{
			return PoserUnTrait(fin ? Genre.SillonFin : Genre.Sillon, x, y, dx, dy, pousseMs, vieMs, maintenant);
		}

		internal static bool Talons(float x, float y, float dx, float dy, int pousseMs, int vieMs, long maintenant)
		{
			return PoserUnTrait(Genre.Talons, x, y, dx, dy, pousseMs, vieMs, maintenant);
		}

		internal static bool Arc(float x, float y, float rayon, float angle, int vieMs, long maintenant)
		{
			if (vieMs <= 0 || !(rayon >= 4f) || float.IsNaN(angle) || float.IsInfinity(angle))
			{
				return false;
			}
			Ajouter(new Marque
			{
				Genre = Genre.Arc,
				X = x,
				Y = y,
				Dx = MathF.Min(rayon, 192f),
				Dy = angle,
				Tons = Particules.TonsDuSol(x, y),
				Nee = maintenant,
				VieMs = vieMs
			}, maintenant);
			return true;
		}

		internal static bool Fissures(float x, float y, long maintenant)
		{
			IGameTexture gameTexture = Globals.ContentManager?.GetTexture(TextureType.Misc, "ssj_cracks.png");
			if (gameTexture == null || gameTexture.Width < 64 || gameTexture.Height <= 0)
			{
				return false;
			}
			Ajouter(new Marque
			{
				Genre = Genre.Fissures,
				X = x,
				Y = y,
				Nee = maintenant,
				VieMs = 5000,
				Texture = gameTexture,
				Gauche = Arrondi((double)x - 32.0),
				Haut = Arrondi((double)y - (double)gameTexture.Height / 2.0)
			}, maintenant);
			return true;
		}

		private static bool PoserUnTrait(Genre genre, float x, float y, float dx, float dy, int pousseMs, int vieMs, long maintenant)
		{
			float num = MathF.Sqrt(dx * dx + dy * dy);
			if (vieMs <= 0 || !(num >= 1f) || float.IsInfinity(num))
			{
				return false;
			}
			if (num > 384f)
			{
				dx = dx / num * 384f;
				dy = dy / num * 384f;
			}
			Ajouter(new Marque
			{
				Genre = genre,
				X = x,
				Y = y,
				Dx = dx,
				Dy = dy,
				Tons = Particules.TonsDuSol(x, y),
				Nee = maintenant,
				PousseMs = Math.Max(0, pousseMs),
				VieMs = vieMs
			}, maintenant);
			return true;
		}

		private static void Ajouter(in Marque marque, long maintenant)
		{
			if (_nombre == 24)
			{
				Balayer(maintenant);
				if (_nombre == 24)
				{
					Liberer(ref LesMarques[0]);
					Array.Copy(LesMarques, 1, LesMarques, 0, _nombre - 1);
					LesMarques[--_nombre] = default(Marque);
				}
			}
			LesMarques[_nombre++] = marque;
		}

		internal static void Oublier()
		{
			for (int i = 0; i < _nombre; i++)
			{
				Liberer(ref LesMarques[i]);
			}
			Array.Clear(LesMarques, 0, _nombre);
			_nombre = 0;
		}

		private static void Liberer(ref Marque marque)
		{
			if (marque.ALaMarque)
			{
				marque.Texture?.Dispose();
			}
			marque.Texture = null;
		}

		private static void Balayer(long maintenant)
		{
			int num = 0;
			for (int i = 0; i < _nombre; i++)
			{
				ref Marque reference = ref LesMarques[i];
				if (Age(in reference, maintenant) >= reference.VieMs)
				{
					Liberer(ref reference);
					continue;
				}
				if (num != i)
				{
					LesMarques[num] = reference;
				}
				num++;
			}
			Array.Clear(LesMarques, num, _nombre - num);
			_nombre = num;
		}

		private static long Age(in Marque marque, long maintenant)
		{
			return Math.Max(0L, maintenant - marque.Nee);
		}

		internal static float Decouverte(long age, int pousseMs)
		{
			if (pousseMs > 0 && age < pousseMs)
			{
				return HeritageRegles.Progression((float)age / (float)pousseMs, 0f);
			}
			return 1f;
		}

		internal static FloatRect Source(int largeur, int hauteur, bool vertical, bool aRebours, float part)
		{
			if (part >= 1f)
			{
				return new FloatRect(0f, 0f, largeur, hauteur);
			}
			if (vertical)
			{
				int num = Math.Clamp(Arrondi((float)hauteur * part), 1, hauteur);
				return new FloatRect(0f, aRebours ? (hauteur - num) : 0, largeur, num);
			}
			int num2 = Math.Clamp(Arrondi((float)largeur * part), 1, largeur);
			return new FloatRect(aRebours ? (largeur - num2) : 0, 0f, num2, hauteur);
		}

		internal static void Dessiner(long maintenant)
		{
			int num = 0;
			bool flag = false;
			for (int i = 0; i < _nombre; i++)
			{
				ref Marque reference = ref LesMarques[i];
				long num2 = Age(in reference, maintenant);
				if (num2 >= reference.VieMs)
				{
					Liberer(ref reference);
					continue;
				}
				if (reference.Texture == null && !flag)
				{
					flag = true;
					if (!Fabriquer(ref reference))
					{
						continue;
					}
				}
				IGameTexture texture = reference.Texture;
				if (texture != null)
				{
					Peindre(in reference, texture, num2);
				}
				if (num != i)
				{
					LesMarques[num] = reference;
				}
				num++;
			}
			Array.Clear(LesMarques, num, _nombre - num);
			_nombre = num;
		}

		private static bool Fabriquer(ref Marque marque)
		{
			Toile toile = marque.Genre switch
			{
				Genre.Talons => PeindreTalons(marque.Dx, marque.Dy, marque.Tons), 
				Genre.Arc => PeindreArc(marque.Dx, marque.Dy, marque.Tons), 
				_ => PeindreSillon(marque.Dx, marque.Dy, marque.Genre == Genre.SillonFin, 1, marque.Tons), 
			};
			string text = $"heritage/sol/{++_fabriquees}";
			marque.Texture = ((Fabrique != null) ? Fabrique(text, toile.Largeur, toile.Hauteur, toile.Rgba) : Graphics.Renderer?.CreateTextureFromPixels(text, toile.Largeur, toile.Hauteur, toile.Rgba));
			if (marque.Texture == null)
			{
				return false;
			}
			marque.ALaMarque = true;
			marque.Gauche = Arrondi((double)marque.X - toile.OrigineX);
			marque.Haut = Arrondi((double)marque.Y - toile.OrigineY);
			return true;
		}

		private static void Peindre(in Marque marque, IGameTexture texture, long age)
		{
			long num = marque.VieMs - age;
			Color couleur = ((num >= 600) ? Plein : new Color((int)(255 * num / 600), 255, 255, 255));
			if (marque.Genre == Genre.Fissures)
			{
				int num2 = Math.Max(1, texture.Width / 64);
				int num3 = (int)Math.Min(num2 - 1, age / 60);
				Effets.Peindre(texture, new FloatRect(num3 * 64, 0f, 64f, texture.Height), new FloatRect(marque.Gauche, marque.Haut, 64f, texture.Height), couleur, 0f);
			}
			else
			{
				bool flag = MathF.Abs(marque.Dy) > MathF.Abs(marque.Dx);
				bool aRebours = (flag ? marque.Dy : marque.Dx) < 0f;
				FloatRect source = Source(texture.Width, texture.Height, flag, aRebours, Decouverte(age, marque.PousseMs));
				Effets.Peindre(texture, source, new FloatRect((float)marque.Gauche + source.X, (float)marque.Haut + source.Y, source.Width, source.Height), couleur, 0f);
			}
		}
	}

	internal static class Son
	{
		private struct Frottement
		{
			public MapSound Son;

			public Guid Corps;

			public Dash Glissade;

			public bool EnFondu;

			public long FonduDepuis;
		}

		private const string Coup = "coup.wav";

		private const string CoupFort = "strongcoup.wav";

		private const string Souffle = "miss.wav";

		internal const string Glisse1 = "heritage_glisse_1.wav";

		internal const string Glisse2 = "heritage_glisse_2.wav";

		internal const string FichierDesTalons = "heritage_talons_1.wav";

		private const int DistanceDEcoute = 12;

		internal const int FonduDuFrottementMs = 60;

		internal const int FrottementsMax = 8;

		internal const float FrottementMinPx = 24f;

		internal const int PiedsMax = 8;

		internal const int PiedEntenduMs = 600;

		private static readonly Random Hasard = new Random();

		internal static Action<string, Entity, float>? Temoin;

		private static readonly (Guid Frappeur, long A, bool Grave)[] Pieds = new(Guid, long, bool)[8];

		private static int _pieds;

		internal const int FauchesMax = 16;

		private static readonly Dictionary<Guid, long> Fauches = new Dictionary<Guid, long>();

		private static readonly Frottement[] LesFrottements = new Frottement[8];

		private static int _frottements;

		internal static int Frottements => _frottements;

		internal static float Hauteur(int centiemes)
		{
			return Math.Clamp((float)centiemes / 100f, -1f, 1f);
		}

		internal static float HauteurTiree(int centiemes, int hasardCentiemes, double tirage)
		{
			return Math.Clamp((float)centiemes / 100f + (float)(Math.Clamp(tirage, 0.0, 1.0) * 2.0 - 1.0) * (float)Math.Max(0, hasardCentiemes) / 100f, -1f, 1f);
		}

		internal static string FichierDuCoup(CoupHeritage coup)
		{
			if (coup != CoupHeritage.CoupDePied)
			{
				return "coup.wav";
			}
			return "strongcoup.wav";
		}

		internal static int CentiemesDeLImpact(CoupHeritage coup, bool critique)
		{
			if (!critique || coup != CoupHeritage.CoupDePied)
			{
				return HeritageRegles.Table(coup).SonCentiemes;
			}
			return Options.Instance.Combat.Heritage.Allure.SonCritiqueCentiemes;
		}

		internal static string FichierDuFrottement(double tirage)
		{
			if (!(tirage < 0.5))
			{
				return "heritage_glisse_2.wav";
			}
			return "heritage_glisse_1.wav";
		}

		internal static float Fondu(long maintenant, long depuis, int fonduMs)
		{
			if (maintenant <= depuis)
			{
				return 1f;
			}
			if (fonduMs > 0 && maintenant - depuis < fonduMs)
			{
				return 1f - (float)(maintenant - depuis) / (float)fonduMs;
			}
			return 0f;
		}

		private static MapSound? Jouer(string fichier, Entity autour, int centiemes, bool auHasard = true)
		{
			int hasardCentiemes = (auHasard ? Options.Instance.Combat.Heritage.SonHasardCentiemes : 0);
			float num = HauteurTiree(centiemes, hasardCentiemes, Hasard.NextDouble());
			Temoin?.Invoke(fichier, autour, num);
			MapSound mapSound = Audio.AddMapSound(fichier, autour.X, autour.Y, autour.MapId, loop: false, 0, 12, autour);
			if (mapSound != null)
			{
				mapSound.Pitch = num;
			}
			return mapSound;
		}

		internal static void Impact(Entity cible, CoupHeritage coup)
		{
			Impact(cible, coup, Timing.Global.Milliseconds);
		}

		internal static void Impact(Entity cible, CoupHeritage coup, long maintenant)
		{
			bool flag = Coups.PiedCritique(cible, coup, maintenant) || (coup == CoupHeritage.CoupDePied && ImpactPromisGrave(cible.Id));
			Jouer(FichierDuCoup(coup), cible, CentiemesDeLImpact(coup, flag));
			if (coup == CoupHeritage.CoupDePied)
			{
				Player me = Globals.Me;
				if (me != null)
				{
					NoterLePied(me.Id, flag, maintenant);
				}
			}
		}

		internal static void NoterLePied(Guid frappeur, bool grave, long maintenant)
		{
			if (!(frappeur == Guid.Empty))
			{
				int i;
				for (i = 0; i < _pieds && Pieds[i].Frappeur != frappeur; i++)
				{
				}
				if (i == _pieds && _pieds == 8)
				{
					i = 0;
				}
				if (i < _pieds)
				{
					_pieds--;
					Array.Copy(Pieds, i + 1, Pieds, i, _pieds - i);
				}
				Pieds[_pieds++] = (Frappeur: frappeur, A: maintenant, Grave: grave);
			}
		}

		internal static bool PiedGrave(Guid frappeur, long maintenant)
		{
			for (int num = _pieds - 1; num >= 0; num--)
			{
				if (Pieds[num].Frappeur == frappeur)
				{
					if (Pieds[num].Grave && maintenant >= Pieds[num].A)
					{
						return maintenant - Pieds[num].A < 600;
					}
					return false;
				}
			}
			return false;
		}

		internal static void OublierLesPieds()
		{
			Array.Clear(Pieds, 0, _pieds);
			_pieds = 0;
		}

		internal static bool PremierSonDeLaFauche(Guid frappeur, long finDuGeste)
		{
			if (Fauches.TryGetValue(frappeur, out var value) && value == finDuGeste)
			{
				return false;
			}
			if (Fauches.Count >= 16)
			{
				Fauches.Clear();
			}
			Fauches[frappeur] = finDuGeste;
			return true;
		}

		internal static void OublierLesFauches()
		{
			Fauches.Clear();
		}

		internal static void Elan(Entity attaquant, CoupHeritage coup)
		{
			Coups.Partir(attaquant, coup);
			CoupHeritageOptions coupHeritageOptions = HeritageRegles.Table(coup);
			if (coupHeritageOptions.Souffle)
			{
				Jouer("miss.wav", attaquant, coupHeritageOptions.SouffleCentiemes);
			}
		}

		internal static void Refus(Entity moi)
		{
			Jouer("miss.wav", moi, Options.Instance.Combat.Heritage.RefusSonCentiemes, auHasard: false);
		}

		internal static bool CoupConfirme(Entity? attaquant, Entity cible)
		{
			return CoupConfirme(attaquant, cible, Timing.Global.Milliseconds);
		}

		internal static bool CoupConfirme(Entity? attaquant, Entity cible, long maintenant)
		{
			if (attaquant != null && Kiai.Blesse(attaquant.Id, maintenant))
			{
				return true;
			}
			if (attaquant != null && attaquant == Globals.Me && HeritageRegles.EstUneFauche(Coups.CoupDe(attaquant, maintenant)))
			{
				return true;
			}
			if (LImpactLocalAttend(attaquant, cible))
			{
				return true;
			}
			if (!(attaquant is Player { CoupEnCours: not CoupHeritage.Aucun } player))
			{
				return false;
			}
			if (HeritageRegles.EstUneFauche(player.CoupEnCours) && !PremierSonDeLaFauche(player.Id, player.FinDuDernierCoupHeritage))
			{
				return true;
			}
			Jouer("coup.wav", cible, HeritageRegles.Table(player.CoupEnCours).SonCentiemes);
			return true;
		}

		internal static bool CoupFinalConfirme(Entity? attaquant, Entity cible)
		{
			long milliseconds = Timing.Global.Milliseconds;
			if (_impactCoup == CoupHeritage.CoupDePied && LImpactLocalAttend(attaquant, cible))
			{
				PromettreLeGrave(cible, milliseconds);
				return true;
			}
			if (attaquant != null && attaquant == Globals.Me && ImpactsPredits.CoupDePiedEntendu(cible.Id, milliseconds))
			{
				return true;
			}
			if (!(attaquant is Player { CoupEnCours: CoupHeritage.CoupDePied } player))
			{
				return false;
			}
			bool flag = Coups.PiedCritique(cible, CoupHeritage.CoupDePied, milliseconds);
			Jouer("strongcoup.wav", cible, CentiemesDeLImpact(CoupHeritage.CoupDePied, flag));
			NoterLePied(player.Id, flag, milliseconds);
			return true;
		}

		internal static void Talons(Entity cible)
		{
			Jouer("heritage_talons_1.wav", cible, 0, auHasard: false);
		}

		internal static void Frotter(Entity corps, Dash glissade)
		{
			Frotter(corps, glissade, Hasard.NextDouble());
		}

		internal static bool Racle(float dx, float dy)
		{
			return dx * dx + dy * dy >= 576f;
		}

		internal static bool Racle(Dash glissade)
		{
			var (num, num2) = glissade.StartOffset;
			var (num3, num4) = glissade.EndOffset;
			return Racle(num3 - num, num4 - num2);
		}

		internal static void Frotter(Entity corps, Dash glissade, double tirage)
		{
			if (!Racle(glissade))
			{
				return;
			}
			for (int i = 0; i < _frottements; i++)
			{
				if (LesFrottements[i].Corps == corps.Id)
				{
					Couper(i);
					break;
				}
			}
			MapSound mapSound = Jouer(FichierDuFrottement(tirage), corps, 0, auHasard: false);
			if (mapSound != null)
			{
				if (_frottements == 8)
				{
					Couper(0);
				}
				LesFrottements[_frottements++] = new Frottement
				{
					Son = mapSound,
					Corps = corps.Id,
					Glissade = glissade
				};
			}
		}

		internal static void SuivreLesFrottements(long maintenant)
		{
			for (int num = _frottements - 1; num >= 0; num--)
			{
				ref Frottement reference = ref LesFrottements[num];
				if (!reference.EnFondu)
				{
					if (maintenant >= reference.Glissade.EndsAt)
					{
						reference.FonduDepuis = reference.Glissade.EndsAt;
					}
					else
					{
						if (Globals.Entities.TryGetValue(reference.Corps, out Entity value) && value != null && value.Dashing == reference.Glissade)
						{
							continue;
						}
						reference.FonduDepuis = maintenant;
					}
					reference.EnFondu = true;
				}
				float num2 = Fondu(maintenant, reference.FonduDepuis, 60);
				if (num2 > 0f)
				{
					reference.Son.Fade = num2;
				}
				else
				{
					Couper(num);
				}
			}
		}

		internal static bool FrottementDe(Guid corps, out float volume)
		{
			for (int i = 0; i < _frottements; i++)
			{
				if (LesFrottements[i].Corps == corps)
				{
					volume = LesFrottements[i].Son.Fade;
					return true;
				}
			}
			volume = 0f;
			return false;
		}

		internal static void CouperLesFrottements()
		{
			for (int num = _frottements - 1; num >= 0; num--)
			{
				Couper(num);
			}
		}

		private static void Couper(int index)
		{
			LesFrottements[index].Son.Stop();
			_frottements--;
			if (index < _frottements)
			{
				Array.Copy(LesFrottements, index + 1, LesFrottements, index, _frottements - index);
			}
			LesFrottements[_frottements] = default(Frottement);
		}
	}

	internal static class Soulevement
	{
		private static readonly Dictionary<Guid, (long Debut, long Fin, int Px)> EnLAir = new Dictionary<Guid, (long, long, int)>();

		internal static int MonteeMs => Options.Instance.Combat.Heritage.Allure.SouleveMonteeMs;

		internal static int RetombeeMs => Options.Instance.Combat.Heritage.Allure.SouleveRetombeeMs;

		internal static float Hauteur(long age, long duree, int px)
		{
			return Hauteur(age, duree, px, MonteeMs, RetombeeMs);
		}

		internal static float Hauteur(long age, long duree, int px, int monteeMs, int retombeeMs)
		{
			if (px <= 0 || duree <= 0 || age <= 0 || age >= duree)
			{
				return 0f;
			}
			float num = ((monteeMs > 0) ? MathF.Min(1f, (float)age / (float)monteeMs) : 1f);
			float num2 = 1f - (1f - num) * (1f - num);
			long num3 = duree - retombeeMs;
			float num4 = ((retombeeMs <= 0 || age <= num3) ? 0f : ((float)(age - num3) / (float)retombeeMs));
			return (float)px * num2 * (1f - num4 * num4);
		}

		internal static void Commencer(Entity entite, int dureeMs, int px)
		{
			long maintenant = Timing.Global.Milliseconds;
			if (EnLAir.Count > 32)
			{
				Guid[] array = (from paire in EnLAir
					where maintenant >= paire.Value.Fin
					select paire.Key).ToArray();
				foreach (Guid key in array)
				{
					EnLAir.Remove(key);
				}
			}
			if (dureeMs <= 0 || px <= 0)
			{
				EnLAir.Remove(entite.Id);
			}
			else
			{
				EnLAir[entite.Id] = (maintenant, maintenant + dureeMs, px);
			}
		}

		internal static float Hauteur(Entity entite)
		{
			if (EnLAir.Count == 0 || !EnLAir.TryGetValue(entite.Id, out (long, long, int) value))
			{
				return 0f;
			}
			long milliseconds = Timing.Global.Milliseconds;
			Dash dashing = entite.Dashing;
			if (dashing != null && dashing.Nature == NatureDeGlissade.Projection)
			{
				int retombeeMs = RetombeeMs;
				if (value.Item2 > milliseconds + retombeeMs)
				{
					value.Item2 = milliseconds + retombeeMs;
					EnLAir[entite.Id] = value;
				}
			}
			if (milliseconds >= value.Item2)
			{
				EnLAir.Remove(entite.Id);
				return 0f;
			}
			return Hauteur(milliseconds - value.Item1, value.Item2 - value.Item1, value.Item3);
		}

		internal static void Oublier()
		{
			EnLAir.Clear();
		}
	}

	internal static class Tenues
	{
		private static readonly string[] DessinesParLeMoteur = new string[3] { "boules_cristal", "aura_roches", "aura_boules_ki" };

		internal static bool DessineParLeMoteur(string? calque)
		{
			string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(calque ?? string.Empty);
			string[] dessinesParLeMoteur = DessinesParLeMoteur;
			foreach (string b in dessinesParLeMoteur)
			{
				if (string.Equals(fileNameWithoutExtension, b, StringComparison.OrdinalIgnoreCase))
				{
					return true;
				}
			}
			return false;
		}

		internal static bool Suit(string? calque, string corps, string poings, string? forme, string planche, int largeur, int hauteur, Func<string, (int Largeur, int Hauteur)?> taille)
		{
			if (string.IsNullOrEmpty(calque) || DessineParLeMoteur(calque))
			{
				return true;
			}
			string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(calque);
			bool hasValue = taille(fileNameWithoutExtension + "-" + Paperdolls.PlancheDesEnveloppes(planche) + ".png").HasValue;
			if (taille(fileNameWithoutExtension + "-" + Paperdolls.PlancheDesEnveloppes(poings) + ".png").HasValue)
			{
				return hasValue;
			}
			string text = poings;
			int num = poings.IndexOf('_') + 1;
			if (!taille(fileNameWithoutExtension + "_" + text.Substring(num, text.Length - num) + ".png").HasValue && (string.IsNullOrEmpty(forme) || !taille(fileNameWithoutExtension + "_" + forme + "_Attack.png").HasValue) && !taille(fileNameWithoutExtension + "_Attack.png").HasValue && !taille(calque).HasValue)
			{
				return true;
			}
			if (hasValue)
			{
				return true;
			}
			if (string.IsNullOrEmpty(corps) || planche.Length <= corps.Length + 1 || !planche.StartsWith(corps + "_", StringComparison.OrdinalIgnoreCase))
			{
				return false;
			}
			text = planche;
			num = corps.Length + 1;
			(int, int)? tuple = taille(fileNameWithoutExtension + "_" + text.Substring(num, text.Length - num) + ".png");
			if (tuple.HasValue)
			{
				(int, int) valueOrDefault = tuple.GetValueOrDefault();
				if (valueOrDefault.Item1 == largeur)
				{
					return valueOrDefault.Item2 == hauteur;
				}
			}
			return false;
		}

		internal static bool Suivent(IEnumerable<string?> calques, string corps, string poings, string? forme, string planche, int largeur, int hauteur, Func<string, (int Largeur, int Hauteur)?> taille)
		{
			poings = Paperdolls.SansCouleur(poings);
			planche = Paperdolls.SansCouleur(planche);
			foreach (string calque in calques)
			{
				if (!Suit(calque, corps, poings, forme, planche, largeur, hauteur, taille))
				{
					return false;
				}
			}
			return true;
		}
	}

	internal static class Traits
	{
		private struct Jeu
		{
			public Guid Corps;

			public sbyte AxeX;

			public sbyte AxeY;

			public long Depart;

			public int DureeMs;

			public int Nombre;

			public float Large;

			public float DepartX;

			public float DepartY;

			public bool DepartConnu;
		}

		internal const int CorpsMax = 8;

		internal const int ParCorpsMax = 8;

		internal const float LongueurMinPx = 24f;

		internal const float LongueurEnPlusPx = 32f;

		internal const float ReculMinPx = 4f;

		internal const float ReculEnPlusPx = 10f;

		internal const float DemiCorpsPx = 12f;

		internal const float DeboutEnPlusPx = 8f;

		internal const float MiCorpsPx = 17f;

		internal const float PartBlanche = 0.35f;

		internal const float PartEpaisse = 0.55f;

		internal const int EpaisPx = 2;

		internal const int FinPx = 1;

		internal const uint Graine = 77u;

		private static readonly Color Blanc = new Color(255, 255, 255, 255);

		private static readonly Color Jaune = new Color(255, 248, 248, 72);

		private static readonly Color Ombre = new Color(255, 74, 28, 6);

		private static readonly FloatRect UnPixel = new FloatRect(0f, 0f, 1f, 1f);

		private static readonly Jeu[] Jeux = new Jeu[8];

		private static int _nombre;

		internal static int Nombre => _nombre;

		internal static bool Lancer(Guid corps, float sensX, float sensY, int dureeMs, int nombre, float large, long maintenant)
		{
			if (corps == Guid.Empty || dureeMs <= 0 || nombre <= 0 || float.IsNaN(sensX) || float.IsNaN(sensY) || float.IsNaN(large) || (sensX == 0f && sensY == 0f))
			{
				return false;
			}
			for (int i = 0; i < _nombre; i++)
			{
				if (Jeux[i].Corps == corps)
				{
					Retirer(i);
					break;
				}
			}
			if (_nombre == 8)
			{
				Retirer(0);
			}
			Entity value;
			bool flag = Globals.Entities.TryGetValue(corps, out value) && value != null;
			System.Numerics.Vector2 vector = (flag ? value.Origin : default(System.Numerics.Vector2));
			bool flag2 = MathF.Abs(sensX) >= MathF.Abs(sensY);
			Jeux[_nombre++] = new Jeu
			{
				Corps = corps,
				AxeX = (sbyte)(flag2 ? MathF.Sign(sensX) : 0),
				AxeY = (sbyte)((!flag2) ? MathF.Sign(sensY) : 0),
				Depart = maintenant,
				DureeMs = dureeMs,
				Nombre = Math.Min(nombre, 8),
				Large = MathF.Abs(large),
				DepartX = vector.X,
				DepartY = vector.Y,
				DepartConnu = flag
			};
			return true;
		}

		internal static float Borne(float longueur, float parcouru, float recul, bool debout)
		{
			float num = parcouru - recul - (debout ? 8f : 0f);
			if (num > 0f && longueur > 0f)
			{
				return MathF.Min(longueur, num);
			}
			return 0f;
		}

		internal static float Parcouru(float departX, float departY, float x, float y, int axeX, int axeY)
		{
			return (x - departX) * (float)axeX + (y - departY) * (float)axeY;
		}

		internal static void Oublier()
		{
			Array.Clear(Jeux, 0, _nombre);
			_nombre = 0;
		}

		private static void Retirer(int index)
		{
			Array.Copy(Jeux, index + 1, Jeux, index, _nombre - index - 1);
			Jeux[--_nombre] = default(Jeu);
		}

		internal static void Dessiner(long maintenant)
		{
			int num = 0;
			for (int i = 0; i < _nombre; i++)
			{
				ref Jeu reference = ref Jeux[i];
				long num2 = Math.Max(0L, maintenant - reference.Depart);
				if (num2 < reference.DureeMs && Globals.Entities.TryGetValue(reference.Corps, out Entity value))
				{
					if (!reference.DepartConnu)
					{
						System.Numerics.Vector2 origin = value.Origin;
						ref float departX = ref reference.DepartX;
						ref float departY = ref reference.DepartY;
						ref bool departConnu = ref reference.DepartConnu;
						float x = origin.X;
						float y = origin.Y;
						departX = x;
						departY = y;
						departConnu = true;
					}
					Peindre(in reference, value, num2);
					if (num != i)
					{
						Jeux[num] = reference;
					}
					num++;
				}
			}
			Array.Clear(Jeux, num, _nombre - num);
			_nombre = num;
		}

		private static void Peindre(in Jeu jeu, Entity corps, long age)
		{
			float num = (float)age / (float)jeu.DureeMs;
			float num2 = 1f - num * num;
			(float X, float Y) tuple = Effets.PlaceDe(corps);
			float item = tuple.X;
			float item2 = tuple.Y;
			item2 -= 17f;
			bool flag = jeu.AxeX != 0;
			IGameTexture pixel = Graphics.Renderer?.WhitePixel;
			System.Numerics.Vector2 origin = corps.Origin;
			float parcouru = Parcouru(jeu.DepartX, jeu.DepartY, origin.X, origin.Y, jeu.AxeX, jeu.AxeY);
			Sol.Alea alea = new Sol.Alea(77u);
			for (int i = 0; i < jeu.Nombre; i++)
			{
				float num3 = ((jeu.Nombre == 1) ? 0f : (((float)i / (float)(jeu.Nombre - 1) - 0.5f) * jeu.Large));
				float longueur = (24f + (float)alea.Suivant() * 32f) * num2;
				float num4 = 4f + (float)alea.Suivant() * 10f;
				float num5 = Borne(longueur, parcouru, num4, !flag);
				float num6;
				float num7;
				if (flag)
				{
					num6 = item - (float)jeu.AxeX * (12f + num4);
					num7 = item2 + (float)jeu.AxeX * num3;
				}
				else
				{
					num6 = item - (float)jeu.AxeY * num3;
					num7 = item2 - (float)jeu.AxeY * (20f + num4);
				}
				int num8 = (int)MathF.Ceiling(num5);
				int num9 = Math.Min(num8, (int)MathF.Ceiling(num5 * 0.35f));
				int num10 = Math.Min(num8, (int)MathF.Ceiling(num5 * 0.55f));
				int x = (int)MathF.Floor(num6 + 0.5f);
				int y = (int)MathF.Floor(num7 + 0.5f);
				sbyte axe = (flag ? jeu.AxeX : jeu.AxeY);
				Morceau(pixel, flag, axe, x, y, 0, num10, 2, 1, Ombre);
				Morceau(pixel, flag, axe, x, y, num10, num8, 1, 1, Ombre);
				Morceau(pixel, flag, axe, x, y, 0, num9, 0, 2, Blanc);
				Morceau(pixel, flag, axe, x, y, num9, num10, 0, 2, Jaune);
				Morceau(pixel, flag, axe, x, y, num10, num8, 0, 1, Jaune);
			}
		}

		private static void Morceau(IGameTexture? pixel, bool horizontal, int axe, int x, int y, int de, int a, int decalage, int epaisseur, Color ton)
		{
			if (a > de)
			{
				int num = ((axe > 0) ? (-(a - 1)) : de);
				FloatRect destination = (horizontal ? new FloatRect(x + num, y + decalage, a - de, epaisseur) : new FloatRect(x + decalage, y + num, epaisseur, a - de));
				Effets.Peindre(pixel, UnPixel, destination, ton, 0f);
			}
		}
	}

	internal const long ChocApresLeGesteMs = 400L;

	internal const long ChocUnSeulSonMs = 100L;

	private static long _coupFortDuChocA;

	private const long RefusSonEcartMs = 300L;

	private static long _dernierSonDeRefusA;

	internal const int ImagesDUnePlanche = 4;

	internal const int ColonnesDuVol = 16;

	internal const int DirectFrappe = 1;

	internal const int CrochetFrappe = 2;

	internal const int EpauleFoulee = 2;

	internal const int MontantFlexion = 1;

	internal const int MontantSaut = 2;

	internal const int MontantPoingLeve = 3;

	internal const int PiedGenou = 0;

	internal const int PiedFrappe = 1;

	internal const int FaucheFrappe = 2;

	private const string CorpsADeuxPlanchesDeCoup = "gohan_ado";

	internal const int ImageDeRetour = 3;

	internal const int RepliSansImage = 1;

	internal const int PlancheDuCoupSaute = 1;

	internal const int RepliSansTenueEpaule = 1;

	internal const int RepliSansTenueMontant = 2;

	private const int ImageMinMs = 16;

	private static readonly HashSet<string> CorpsAuVolTenu = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Gohan", "Gohan_ssj", "gohan_ssj2", "Gohan_ssg", "gohan_ui" };

	private static readonly HashSet<string> CorpsAuCoupSaute = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "trunk_futur", "trunk_futur_ssj1", "trunk_futur_ssj2", "trunk_futur_ssg", "trunk_futur_ui" };

	private static bool _impactEnAttente;

	private static Guid _impactCibleId;

	private static CoupHeritage _impactCoup;

	private static int _impactAuTemps;

	private static bool _impactGrave;

	internal static bool LeCoupFortADejaSonne => UnSeulCoupFort(_coupFortDuChocA, Timing.Global.Milliseconds);

	internal static bool Actif => HeritageRegles.Actif;

	internal static Guid ReferenceId { get; set; }

	internal static bool SerieOuverte
	{
		get
		{
			if (HeritageRegles.Actif)
			{
				return Globals.Me?.SerieHeritageOuverte ?? false;
			}
			return false;
		}
	}

	internal static long DernierCoupRecuA { get; private set; }

	internal static Guid DernierAttaquantId { get; private set; }

	internal static bool ImpactEnAttente => _impactEnAttente;

	internal static bool LeChocBriseLeCoupDe(EchangePacket packet, Guid moiId, out Guid cibleId)
	{
		Guid guid;
		Guid guid2;
		if (packet.Index != 1)
		{
			Guid entityId = packet.EntityId;
			Guid autreId = packet.AutreId;
			guid = autreId;
			guid2 = entityId;
		}
		else
		{
			Guid autreId2 = packet.AutreId;
			Guid autreId = packet.EntityId;
			guid = autreId;
			guid2 = autreId2;
		}
		cibleId = guid;
		if (moiId != Guid.Empty)
		{
			return guid2 == moiId;
		}
		return false;
	}

	internal static bool LeChocBriseCeCoup(CoupHeritage dernierCoup, Guid cibleDuDernierCoup, long finDuDernierCoup, long finDuGeste, Guid cibleDuChoc, long maintenant)
	{
		if (dernierCoup != CoupHeritage.Aucun && cibleDuDernierCoup == cibleDuChoc && finDuGeste == finDuDernierCoup)
		{
			return maintenant - finDuDernierCoup < 400;
		}
		return false;
	}

	internal static bool UnSeulCoupFort(long sonneA, long maintenant)
	{
		if (sonneA > 0 && maintenant >= sonneA)
		{
			return maintenant - sonneA < 100;
		}
		return false;
	}

	internal static void SurChoc(EchangePacket packet)
	{
		_coupFortDuChocA = 0L;
		if (!HeritageRegles.Actif)
		{
			return;
		}
		Player me = Globals.Me;
		if (me == null || !LeChocBriseLeCoupDe(packet, me.Id, out var cibleId))
		{
			return;
		}
		long milliseconds = Timing.Global.Milliseconds;
		if (LeChocBriseCeCoup(me.DernierCoupHeritage, me.CibleDuDernierCoupHeritage, me.FinDuDernierCoupHeritage, me.AttackTimer, cibleId, milliseconds))
		{
			bool flag = AnnulerLImpactEnAttente(cibleId);
			if (!flag)
			{
				bool flag2 = ImpactsPredits.CoupDePiedEntendu(cibleId, milliseconds);
				long joueA;
				bool flag3 = ImpactsPredits.Sortir(cibleId, milliseconds, out joueA);
				_coupFortDuChocA = ((flag2 && flag3) ? joueA : 0);
				CombatFeedback.CancelPredictedStrike(cibleId);
				Poussee.Retirer(cibleId);
			}
			me.RendreLeCompteDAvant(cibleId);
			Elan.Fermer();
			if (Options.Instance.Combat.Heritage.JournalDesAppuis)
			{
				ApplicationContext.Context.Value?.Logger.LogInformation("Heritage : coup brise par un choc ({Impact})", flag ? "impact annule avant son heure" : "impact deja joue");
			}
		}
	}

	internal static void NoterUnCoupRecu(Guid attaquantId)
	{
		if (HeritageRegles.Actif)
		{
			DernierCoupRecuA = Timing.Global.Milliseconds;
			DernierAttaquantId = attaquantId;
		}
	}

	internal static void Lancer(Player moi, Entity cible, CoupHeritage coup)
	{
		if (HeritageRegles.Actif)
		{
			ReferenceId = cible.Id;
			Son.Elan(moi, coup);
			Elan.Ouvrir(moi, cible, coup);
			ArmerLImpact(moi, cible, coup);
		}
	}

	internal static void LancerALArrivee(Player moi, Entity cible, CoupHeritage coup)
	{
		if (HeritageRegles.Actif)
		{
			ReferenceId = cible.Id;
			Elan.Fermer();
			Son.Elan(moi, coup);
			ArmerLImpact(moi, cible, coup);
			if (_impactEnAttente)
			{
				JouerLImpactEnAttente(moi);
			}
		}
	}

	internal static void SurGlissadeDeRuee(Entity entite, NatureDeGlissade nature, Guid cibleId, int dureeMs)
	{
		if (nature == NatureDeGlissade.Ruee && !(cibleId == Guid.Empty))
		{
			Player me = Globals.Me;
			if (me != null && entite == me)
			{
				me.NoterLaRueeHeritage(cibleId, dureeMs);
			}
		}
	}

	internal static void Suivre(Player joueur)
	{
		if (HeritageRegles.Actif && joueur == Globals.Me)
		{
			Poussee.Suivre(joueur);
			SuivreLImpact(joueur);
			Lien.Suivre(joueur);
		}
	}

	internal static void SouffleDUnAutre(Entity entite, byte coup)
	{
		if (!HeritageRegles.Actif || coup == 0 || !HeritageRegles.EstUnCoupJoue(coup))
		{
			CombatAudio.Swing(entite);
		}
		else
		{
			Son.Elan(entite, (CoupHeritage)coup);
		}
	}

	internal static bool EchoSansRelance(Entity entite, byte pas, byte coup, int duree)
	{
		if (!HeritageRegles.Actif || !(entite is Player player))
		{
			return false;
		}
		bool flag = player.CoupEnCours != CoupHeritage.Aucun;
		if (player != Globals.Me)
		{
			return coup == 0 && flag;
		}
		if (coup == 0 && !flag)
		{
			return false;
		}
		if (player.FrapperALArriveeDeLaRuee(pas, coup, duree))
		{
			return true;
		}
		player.EchoHeritage(pas, coup, duree);
		return true;
	}

	internal static void NoterLeGesteDeLEcho(Entity entite, byte coup)
	{
		if (HeritageRegles.Actif)
		{
			CoupHeritage coup2 = (CoupHeritage)(HeritageRegles.EstUnCoupJoue(coup) ? coup : 0);
			entite.NoterLeCoupHeritage(coup2, 0, Guid.Empty);
		}
	}

	internal static void OnRefus(EchangePacket packet)
	{
		if (!HeritageRegles.Actif)
		{
			return;
		}
		Player me = Globals.Me;
		if (me != null)
		{
			Guid guid = ((packet.AutreId != Guid.Empty) ? packet.AutreId : me.CibleDuDernierCoupHeritage);
			long milliseconds = Timing.Global.Milliseconds;
			if (!AnnulerLImpactEnAttente(guid))
			{
				CombatFeedback.CancelPredictedStrike(guid);
				ImpactsPredits.Retirer(guid, milliseconds);
				Poussee.Retirer(guid);
			}
			me.RendreLeCompteDAvant(guid);
			if (milliseconds - _dernierSonDeRefusA >= 300)
			{
				_dernierSonDeRefusA = milliseconds;
				Son.Refus(me);
			}
			if (Options.Instance.Combat.Heritage.JournalDesAppuis)
			{
				ApplicationContext.Context.Value?.Logger.LogInformation("Heritage : coup refuse par le serveur ({Raison})", (RaisonDeRefus)packet.Valeur);
			}
		}
	}

	internal static float PiedsPourLeTri(Entity entite)
	{
		if (!HeritageRegles.Actif)
		{
			return entite.Origin.Y;
		}
		return Devant.Pieds(entite);
	}

	internal static int RangeePourLeTri(Entity entite, int rangee)
	{
		if (!HeritageRegles.Actif)
		{
			return rangee;
		}
		return Devant.Ranger(entite, rangee, Timing.Global.Milliseconds);
	}

	internal static bool CorpsSansMontant(string? corps)
	{
		return EstDans(CorpsAuCoupSaute, corps);
	}

	internal static bool VolTenu(string? corps)
	{
		return EstDans(CorpsAuVolTenu, corps);
	}

	internal static int ColonneDuMontant(bool volTenu, int colonne)
	{
		if (!volTenu)
		{
			return colonne;
		}
		return 1;
	}

	private static bool EstDans(HashSet<string> corpsConnus, string? corps)
	{
		if (!string.IsNullOrEmpty(corps))
		{
			return corpsConnus.Contains(Paperdolls.SansCouleur(Path.GetFileNameWithoutExtension(corps)));
		}
		return false;
	}

	internal static (PoseDuCoup Pose, int Image, bool ImpactPasse)? Image(CoupHeritage coup, int ecoule, int duree, int avance, int imagesDeLaPlanche)
	{
		if (coup == CoupHeritage.Aucun || duree <= 0 || imagesDeLaPlanche != 4)
		{
			return null;
		}
		CoupHeritageOptions coupHeritageOptions = HeritageRegles.Table(coup);
		if (coupHeritageOptions.TenueMs <= 0)
		{
			return null;
		}
		avance = Math.Max(0, avance);
		int num = duree + avance;
		int num2 = Math.Clamp(ecoule, 0, duree) + avance;
		int num3 = Math.Clamp(coupHeritageOptions.ImpactMs, 0, num);
		bool flag = ((coup == CoupHeritage.Direct || coup == CoupHeritage.CoupDePied) ? true : false);
		int num4 = ((!flag) ? 1 : 2);
		int num5 = Math.Max(num3, Math.Min(num3 + Math.Min(coupHeritageOptions.TenueMs, num), num - num4 * 16));
		bool item = num2 >= num3;
		int num6 = num5 + (num - num5) / 2;
		switch (coup)
		{
		case CoupHeritage.Direct:
			return (num2 < num3) ? (PoseDuCoup.Planche, 0, false) : ((num2 < num5) ? (PoseDuCoup.Planche, 1, true) : (PoseDuCoup.Planche, (num2 < num6) ? 2 : 3, true));
		case CoupHeritage.Crochet:
			return (num2 < num3) ? (PoseDuCoup.Planche, (num2 >= num3 / 2) ? 1 : 0, false) : ((num2 < num5) ? (PoseDuCoup.Planche, 2, true) : (PoseDuCoup.Planche, 3, true));
		case CoupHeritage.CoupDEpaule:
			return (num2 < num5) ? (PoseDuCoup.Course, 2, item) : (PoseDuCoup.Planche, 3, true);
		case CoupHeritage.Montant:
			return (num2 < Math.Min(coupHeritageOptions.ArmementTenuMs, num3)) ? (PoseDuCoup.Vol, 1, false) : ((num2 < num3) ? (PoseDuCoup.Vol, 2, false) : ((num2 < num5) ? (PoseDuCoup.Vol, 3, true) : (PoseDuCoup.Planche, 3, true)));
		case CoupHeritage.CoupDePied:
			return (num2 < num3) ? (PoseDuCoup.Planche, 0, false) : ((num2 < num5) ? (PoseDuCoup.Planche, 1, true) : (PoseDuCoup.Planche, (num2 < num6) ? 2 : 3, true));
		case CoupHeritage.FaucheHoraire:
		case CoupHeritage.FaucheAntihoraire:
			return (num2 < num3) ? (PoseDuCoup.Planche, (num2 >= num3 / 2) ? 1 : 0, false) : ((num2 < num5) ? (PoseDuCoup.Planche, 2, true) : (PoseDuCoup.Planche, 3, true));
		default:
			return null;
		}
	}

	internal static int ImageDeRepli(CoupHeritage coup, RepliDuCoup repli)
	{
		if (repli != RepliDuCoup.SansTenue)
		{
			return 1;
		}
		if (coup != CoupHeritage.Montant)
		{
			return 1;
		}
		return 2;
	}

	internal static int FinDeLImageDeFrappe(CoupHeritage coup, int avance)
	{
		CoupHeritageOptions coupHeritageOptions = HeritageRegles.Table(coup);
		return Math.Max(0, coupHeritageOptions.ImpactMs - Math.Max(0, avance)) + coupHeritageOptions.TenueMs;
	}

	internal static long TempsDuGeste(Entity entite)
	{
		return entite.DureeDuGeste() - (entite.AttackTimer - Timing.Global.Milliseconds);
	}

	internal static int? ImageDuCoup(Entity entite, long tempsDuGeste)
	{
		if (!HeritageRegles.Actif)
		{
			return null;
		}
		return entite.ImageDuCoupHeritage(tempsDuGeste);
	}

	internal static bool TryPose(Entity entite, int rangee, ref IGameTexture texture, ref FloatRect source)
	{
		if (HeritageRegles.Actif)
		{
			return entite.PoseDuCoupHeritage(rangee, ref texture, ref source);
		}
		return false;
	}

	internal static bool SurLaPremierePlanche(Entity entite)
	{
		bool actif = HeritageRegles.Actif;
		bool flag = actif;
		if (flag)
		{
			CoupHeritage coupEnCours = entite.CoupEnCours;
			bool flag2 = coupEnCours - 3 <= CoupHeritage.Direct;
			flag = flag2;
		}
		return flag;
	}

	internal static int? PlancheDuMontant(Entity entite, int planches)
	{
		if (!HeritageRegles.Actif)
		{
			return null;
		}
		return entite.PlancheDuMontantHeritage(planches);
	}

	internal static bool SurLaPlancheDesPieds(Entity entite)
	{
		if (HeritageRegles.Actif)
		{
			return HeritageRegles.EstUneFauche(entite.CoupEnCours);
		}
		return false;
	}

	internal static int PlancheDesPieds(Entity entite, int planches)
	{
		return PlancheDesPieds(entite.Sprite, planches);
	}

	internal static int PlancheDesPieds(string? corps, int planches)
	{
		int num = planches;
		if (Path.GetFileNameWithoutExtension(corps ?? string.Empty).StartsWith("gohan_ado", StringComparison.OrdinalIgnoreCase))
		{
			num = Math.Min(num, 2);
		}
		if (num < 3)
		{
			return Math.Max(0, Math.Min(1, num - 1));
		}
		return 2;
	}

	internal static bool LImpactLocalAttend(Entity? attaquant, Entity cible)
	{
		if (_impactEnAttente && attaquant != null)
		{
			Player me = Globals.Me;
			if (me != null && attaquant == me && cible.Id == _impactCibleId)
			{
				return LImpactEstCeluiDuGeste(me);
			}
		}
		return false;
	}

	private static void PromettreLeGrave(Entity cible, long maintenant)
	{
		_impactGrave = _impactCoup == CoupHeritage.CoupDePied && Coups.PiedCritique(cible, CoupHeritage.CoupDePied, maintenant);
	}

	private static bool ImpactPromisGrave(Guid cibleId)
	{
		if (_impactGrave)
		{
			return cibleId == _impactCibleId;
		}
		return false;
	}

	internal static bool LImpactSonneraGrave(Entity? attaquant)
	{
		if (_impactEnAttente && _impactGrave && attaquant != null)
		{
			return attaquant == Globals.Me;
		}
		return false;
	}

	internal static int HeureDeLImpact(CoupHeritage coup, int avance)
	{
		return Math.Max(0, HeritageRegles.Table(coup).ImpactMs - Math.Max(0, avance));
	}

	private static void ArmerLImpact(Player moi, Entity cible, CoupHeritage coup)
	{
		_impactEnAttente = true;
		_impactGrave = false;
		_impactCibleId = cible.Id;
		_impactCoup = coup;
		_impactAuTemps = HeureDeLImpact(coup, moi.AvanceDuCoupEnCours);
		if (HeritageRegles.Table(coup).ImpactMs <= 0)
		{
			JouerLImpactEnAttente(moi);
		}
	}

	private static bool LImpactEstCeluiDuGeste(Player moi)
	{
		if (moi.CoupEnCours == _impactCoup)
		{
			return moi.CibleDuCoupEnCours == _impactCibleId;
		}
		return false;
	}

	private static void JouerLImpactEnAttente(Player moi)
	{
		_impactEnAttente = false;
		if (EtatSubi(moi) || !Globals.Entities.TryGetValue(_impactCibleId, out Entity value) || value == null || value.IsDying)
		{
			_impactGrave = false;
			return;
		}
		if (CombatFeedback.OnLocalStrikeHeritage(value, _impactCoup, moi.PasDuCoup))
		{
			Poussee.Predire(moi, value, _impactCoup);
		}
		_impactGrave = false;
	}

	internal static bool EtatSubi(Entity entite)
	{
		if (!entite.IsDying && !entite.IsGuardStunned && !Echanges.EstSonne(entite))
		{
			Dash dashing = entite.Dashing;
			if (dashing == null || !dashing.IsKnockback)
			{
				for (int i = 0; i < entite.Status.Count; i++)
				{
					SpellEffect type = entite.Status[i].Type;
					if ((type == SpellEffect.Stun || type == SpellEffect.Sleep) ? true : false)
					{
						return true;
					}
				}
				return false;
			}
		}
		return true;
	}

	internal static void AvantUneAutreAction()
	{
		if (!HeritageRegles.Actif)
		{
			return;
		}
		Elan.Fermer();
		if (!_impactEnAttente)
		{
			return;
		}
		Player me = Globals.Me;
		if (me != null)
		{
			if (LImpactEstCeluiDuGeste(me))
			{
				JouerLImpactEnAttente(me);
			}
			else
			{
				_impactEnAttente = false;
			}
		}
	}

	private static void SuivreLImpact(Player moi)
	{
		if (_impactEnAttente)
		{
			if (!LImpactEstCeluiDuGeste(moi))
			{
				_impactEnAttente = false;
			}
			else if (TempsDuGeste(moi) >= _impactAuTemps)
			{
				JouerLImpactEnAttente(moi);
			}
		}
	}

	private static bool AnnulerLImpactEnAttente(Guid cibleId)
	{
		if (!_impactEnAttente || (cibleId != Guid.Empty && cibleId != _impactCibleId))
		{
			return false;
		}
		_impactEnAttente = false;
		return true;
	}

	internal static void OublierLesImpacts()
	{
		_impactEnAttente = false;
		_impactGrave = false;
		ImpactsPredits.Vider();
	}

	internal static bool SansMarche(long tempsDuGeste, int sansMarcheMs, int finDeLImageDeFrappe)
	{
		if (sansMarcheMs > 0)
		{
			return tempsDuGeste < Math.Min(sansMarcheMs, finDeLImageDeFrappe);
		}
		return false;
	}

	internal static bool MarcheFigee(Player joueur)
	{
		if (!HeritageRegles.Actif)
		{
			return false;
		}
		CoupHeritage coupEnCours = joueur.CoupEnCours;
		if (coupEnCours == CoupHeritage.Aucun)
		{
			return false;
		}
		return SansMarche(TempsDuGeste(joueur), HeritageRegles.Table(coupEnCours).SansMarcheMs, FinDeLImageDeFrappe(coupEnCours, joueur.AvanceDuCoupEnCours));
	}

	internal static bool RegardTenu(Player joueur)
	{
		if (HeritageRegles.Actif && Options.Instance.Combat.Heritage.RegardTenu)
		{
			return joueur.CibleDuCoupEnCours != Guid.Empty;
		}
		return false;
	}

	internal static void Soulever(Entity entite, int dureeMs, int px)
	{
		if (HeritageRegles.Actif)
		{
			Soulevement.Commencer(entite, dureeMs, px);
		}
	}

	internal static float HauteurDe(Entity entite)
	{
		if (!HeritageRegles.Actif)
		{
			return 0f;
		}
		return MathF.Max(Soulevement.Hauteur(entite), Elan.HauteurDuMontant(entite));
	}
}
