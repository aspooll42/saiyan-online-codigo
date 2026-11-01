using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Intersect.Client.Core;
using Intersect.Client.Core.Sounds;
using Intersect.Client.Framework.Content;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.General;
using Intersect.Client.Interface.Game.Chat;
using Intersect.Client.Localization;
using Intersect.Client.Maps;
using Intersect.Enums;
using Intersect.Framework.Core;
using Intersect.Framework.Core.GameObjects.Items;
using Intersect.Framework.Core.GameObjects.Maps;
using Intersect.Framework.Core.GameObjects.Resources;
using Intersect.Models;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Intersect.Client.Entities;

public static class Recolte
{
	private sealed class PoissonQuiTourne
	{
		public float Angle;

		public float Rayon;

		public float Vitesse;

		public float Phase;

		public float X;

		public float Y;

		public float Cap;

		public bool Place;
	}

	private sealed class Bouillon
	{
		public float Force;

		public long ProchaineBulle;

		public readonly PoissonQuiTourne[] Poissons = new PoissonQuiTourne[4];
	}

	private enum PhaseDePeche
	{
		Lancer,
		Prise,
		Ramener
	}

	private enum EtatFlotteur
	{
		Range,
		Main,
		Vol,
		Eau,
		SurLigne,
		Ramene
	}

	private enum EtatPrise
	{
		Vol,
		Pendu,
		Panier
	}

	private sealed class PoissonPris
	{
		public EtatPrise Etat;

		public long Ne;

		public long Debat = -2305843009213693952L;

		public long DerniereGoutte;

		public long Panier;

		public float DepartX;

		public float DepartY;

		public float Cx;

		public float Cy;

		public float SolX;

		public float SolY;

		public float Hauteur;

		public float Angle;

		public bool Miroir;

		public int Image;

		public float Alpha = 1f;

		public float DepuisX;

		public float DepuisY;
	}

	private sealed class Pecheur
	{
		public Guid Coin;

		public Guid CoinMapId;

		public int CoinX;

		public int CoinY;

		public Guid MapId;

		public int TuileX;

		public int TuileY;

		public Direction Direction;

		public PhaseDePeche Phase;

		public long Debut;

		public long Ferrer = -2305843009213693952L;

		public bool FerrerPrecis;

		public bool LancerPrecis;

		public bool FerrerEnAttente;

		public bool FerrerEnAttentePrecis;

		public bool PriseEnAttente;

		public long Pose = -2305843009213693952L;

		public long DernierGeste;

		public long MordDepuis = -1L;

		public int MorsuresFaites;

		public long DernierTwitch = -2305843009213693952L;

		public long Precedent;

		public long DernierPas;

		public long DureeRamener = 320L;

		public float AngleAuRamener = 32f;

		public float PhiPrec = float.NaN;

		public float OmegaPrec;

		public readonly Corde Corde = new Corde();

		public bool CordePrete;

		public readonly Ressort Flexion = new Ressort(420f, 15f);

		public readonly Ressort Creux = new Ressort(260f, 11f);

		public EtatFlotteur Flotteur;

		public float Fx;

		public float Fy;

		public float Fh;

		public float Fvx;

		public float Fvy;

		public float Fvh;

		public float Fangle;

		public float Ftour;

		public Point3 RameneDepuis;

		public float LongueurAuRamener;

		public PoissonPris? Poisson;

		public Point3 Main;

		public Point3 Bout;

		public float CanneMainX;

		public float CanneMainY;

		public float CanneControleX;

		public float CanneControleY;

		public float CanneBoutX;

		public float CanneBoutY;

		public float Alpha;

		public bool Derriere;

		public bool Termine;
	}

	private readonly record struct Silhouette(int Gauche, int Droite, int Haut, int Bas);

	private readonly record struct Orientation(int AvantX, int AvantY, float Cote);

	private sealed class Etat
	{
		public bool FissuresReconstruites;

		public long Geste = -2305843009213693952L;

		public long Predit = -2305843009213693952L;

		public long Son = -2305843009213693952L;

		public long Precis = -2305843009213693952L;

		public long Secousse = -2305843009213693952L;

		public double Frisson = -4.4942328371557893E+307;

		public int SensFrisson = 1;

		public float ForceFrisson = 1f;

		public long Fin = -1L;

		public long Repousse = -1L;

		public int Gestes;

		public long DerniereProgression;

		public float Barre;

		public IGameTexture? Texture;

		public FloatRect Source;

		public FloatRect Destination;

		public bool Abaissee;

		public readonly List<Faille> Failles = new List<Faille>();

		public readonly List<(int X, int Y)> Impacts = new List<(int, int)>();

		public bool[]? Usure;

		public FrappeDeRoche? Frappe;

		public long Affaisse = -2305843009213693952L;

		public long EclairGeneral = -2305843009213693952L;

		public long CederDepuis = -1L;

		public long ProchainRuisseau;

		public long ProchaineFumee;

		public readonly List<Ruissellement> Ruisseaux = new List<Ruissellement>();

		public List<MorceauDeRoche>? Morceaux;

		public long Brise = -1L;

		public Banc? Banc;

		public Saut? Saut;

		public long ProchainSaut;

		public Prise? Prise;
	}

	private enum Genre
	{
		Bois,
		Sciure,
		Goutte,
		Feuille
	}

	private sealed class Eclat
	{
		public Genre Genre;

		public FormeBois Forme;

		public Guid MapId;

		public float X;

		public float Y;

		public float Vx;

		public float Vy;

		public float Sol;

		public float Angle;

		public float Tour;

		public int Rebonds;

		public long Ne;

		public long Repos = -1L;

		public Color Couleur = Color.White;

		public int Taille = 1;
	}

	private readonly record struct Onde(Guid MapId, float X, float Y, long Ne, long Duree, bool Poussiere, float Ampleur);

	private readonly record struct Gerbe(Guid MapId, float X, float Y, long Ne, int Graine, float Ampleur, float[]? Style = null);

	private readonly record struct Elan(long Ne, float Dx, float Dy, float Distance);

	private readonly record struct Attente(Guid RessourceId, long Valeur, long Recu);

	private enum FormeBois
	{
		Copeau,
		Eclat,
		Ecorce
	}

	private sealed record RondDEau(Guid MapId, float X, float Y, long Ne, float Rayon, long Duree, Color Couleur, float Alpha, int Graine, bool Epais);

	private sealed class GoutteDEau
	{
		public Guid MapId;

		public float X;

		public float Y;

		public float H;

		public float Vx;

		public float Vy;

		public float Vh;

		public long Ne;

		public int Taille = 1;

		public bool Claire;

		public float CoinX = float.NaN;

		public float CoinY;
	}

	private readonly record struct TraceDeGoutte(Guid MapId, float X, float Y, long Ne, bool Eau);

	private sealed class Faille
	{
		public readonly List<(int X, int Y)> Trait = new List<(int, int)>();

		public readonly List<(int X, int Y)> Branche = new List<(int, int)>();

		public long Ne;

		public long Eclair = 110L;
	}

	private sealed class FrappeDeRoche
	{
		public long Debut;

		public bool Precis;

		public bool Casse;

		public bool Touche;

		public bool Libere;

		public int Cote;

		public int CoteY;

		public int Ix;

		public int Iy;

		public Guid Frappeur;

		public readonly List<(int X, int Y)> Morsures = new List<(int, int)>();

		public int Butins;
	}

	private sealed class PoseDeFrappe
	{
		public long Debut;

		public bool Precis;

		public bool Casse;

		public float Elan;

		public int Dx;

		public int Dy;
	}

	private sealed class EclatDePierre
	{
		public Guid MapId;

		public float X;

		public float Y;

		public float H;

		public float Vx;

		public float Vy;

		public float Vh;

		public float Angle;

		public float Tour;

		public int Rebonds;

		public long Repos = -1L;

		public long Ne;

		public required Dictionary<(int, int), Color> Grille;

		public int Rayon;

		public bool Mort;
	}

	private sealed class GrainDePierre
	{
		public Guid MapId;

		public float X;

		public float Y;

		public float H;

		public float Vx;

		public float Vy;

		public float Vh;

		public required Color Couleur;

		public long Ne;

		public bool Rebond;

		public long Pose = -1L;
	}

	private sealed class Ruissellement
	{
		public int Px;

		public int Py;

		public required Color Couleur;

		public long Ne;

		public long Prochain;

		public bool Mort;
	}

	private sealed class PoussiereDeRoche
	{
		public Guid MapId;

		public float X;

		public float Y;

		public long Ne;

		public bool Sol;

		public int Sens;

		public int SensY;

		public required Color Couleur;

		public int Graine;

		public float Ampleur = 1f;

		public int Nombre = 7;

		public float Largeur = 24f;

		public bool Mort;
	}

	private sealed class MorceauDeRoche
	{
		public float X;

		public float Y;

		public float H;

		public float Vx;

		public float Vy;

		public float Vh;

		public float Angle;

		public float Tour;

		public int Rebonds;

		public bool Repos;

		public float Alpha = 1f;

		public float Cx;

		public float Cy;

		public int MinX;

		public int MaxX;

		public int MinY;

		public int MaxY;

		public int Masse;

		public int Largeur;

		public required Dictionary<int, Color> Grille;

		public int Rayon;

		public float HRepos;
	}

	private sealed class ButinDeRoche
	{
		public Guid MapId;

		public float X;

		public float Y;

		public long Ne;

		public required string[] Lignes;

		public required Func<char, Color> Palette;

		public Guid Joueur;

		public bool Cristal;

		public bool Sonne;

		public bool Mort;
	}

	private readonly record struct ChocDeFrappe(Guid MapId, float X, float Y, long Ne, long Duree, bool Fort);

	private sealed class Saut
	{
		public required float X0;

		public required float Y0;

		public required float X1;

		public required long Ne;

		public required int Sens;

		public long Goutte;
	}

	private sealed class Prise
	{
		public long Ne { get; }

		public Guid Pecheur { get; }

		public float X0 { get; }

		public float Y0 { get; }

		public long Goutte { get; set; }

		public Prise(long ne, Guid pecheur, float x0, float y0)
		{
			Ne = ne;
			Pecheur = pecheur;
			X0 = x0;
			Y0 = y0;
		}
	}

	private readonly record struct Point3(float X, float Y, float H);

	private sealed class Noeud
	{
		public float X;

		public float Y;

		public float H;

		public float Px;

		public float Py;

		public float Ph;
	}

	private sealed class Corde
	{
		public const int Segments = 16;

		public readonly Noeud[] Noeuds;

		public float Longueur = 6f;

		public Noeud Bout => Noeuds[16];

		public Corde()
		{
			Noeuds = new Noeud[17];
			for (int i = 0; i <= 16; i++)
			{
				Noeuds[i] = new Noeud();
			}
		}

		public void Placer(Point3 a, Point3 b)
		{
			for (int i = 0; i <= 16; i++)
			{
				float k = (float)i / 16f;
				Noeud noeud = Noeuds[i];
				noeud.X = (noeud.Px = Lerp(a.X, b.X, k));
				noeud.Y = (noeud.Py = Lerp(a.Y, b.Y, k));
				noeud.H = (noeud.Ph = Lerp(a.H, b.H, k));
			}
		}

		public void Deplacer(float dx, float dy)
		{
			Noeud[] noeuds = Noeuds;
			foreach (Noeud noeud in noeuds)
			{
				noeud.X += dx;
				noeud.Px += dx;
				noeud.Y += dy;
				noeud.Py += dy;
			}
		}

		public void Pas(float dt, Point3 a, Point3? b, float masse)
		{
			bool flag = !b.HasValue;
			float num = 760f * dt * dt;
			float num2 = (flag ? (1f / MathF.Max(0.01f, masse)) : 0f);
			Noeud noeud = Noeuds[0];
			noeud.Px = noeud.X;
			noeud.Py = noeud.Y;
			noeud.Ph = noeud.H;
			if (!flag)
			{
				Bout.Px = Bout.X;
				Bout.Py = Bout.Y;
				Bout.Ph = Bout.H;
			}
			for (int i = 1; i <= 16; i++)
			{
				if (i != 16 || flag)
				{
					Noeud noeud2 = Noeuds[i];
					float num3 = ((noeud2.H <= 0.05f) ? 0.8f : 0.978f);
					float num4 = (noeud2.X - noeud2.Px) * num3;
					float num5 = (noeud2.Y - noeud2.Py) * num3;
					float num6 = (noeud2.H - noeud2.Ph) * 0.978f;
					noeud2.Px = noeud2.X;
					noeud2.Py = noeud2.Y;
					noeud2.Ph = noeud2.H;
					noeud2.X += num4;
					noeud2.Y += num5;
					noeud2.H += num6 - num;
				}
			}
			Epingler(a, b);
			float num7 = Longueur / 16f;
			for (int j = 0; j < 14; j++)
			{
				for (int k = 0; k < 16; k++)
				{
					Noeud noeud3 = Noeuds[k];
					Noeud noeud4 = Noeuds[k + 1];
					float num8 = noeud4.X - noeud3.X;
					float num9 = noeud4.Y - noeud3.Y;
					float num10 = noeud4.H - noeud3.H;
					float num11 = MathF.Sqrt(num8 * num8 + num9 * num9 + num10 * num10);
					if (!(num11 <= num7) && !(num11 < 1E-06f))
					{
						float num12 = ((k == 0) ? 0f : 1f);
						float num13 = ((k + 1 == 16) ? num2 : 1f);
						float num14 = num12 + num13;
						if (!(num14 <= 0f))
						{
							float num15 = (num11 - num7) / num11;
							noeud3.X += num8 * num15 * num12 / num14;
							noeud3.Y += num9 * num15 * num12 / num14;
							noeud3.H += num10 * num15 * num12 / num14;
							noeud4.X -= num8 * num15 * num13 / num14;
							noeud4.Y -= num9 * num15 * num13 / num14;
							noeud4.H -= num10 * num15 * num13 / num14;
						}
					}
				}
				Epingler(a, b);
				for (int l = 1; l <= 16; l++)
				{
					if (Noeuds[l].H < 0f)
					{
						Noeuds[l].H = 0f;
					}
				}
			}
		}

		private void Epingler(Point3 a, Point3? b)
		{
			Noeud noeud = Noeuds[0];
			noeud.X = a.X;
			noeud.Y = a.Y;
			noeud.H = a.H;
			if (b.HasValue)
			{
				Point3 valueOrDefault = b.GetValueOrDefault();
				Bout.X = valueOrDefault.X;
				Bout.Y = valueOrDefault.Y;
				Bout.H = valueOrDefault.H;
			}
		}

		public void Impulsion(float dx, float dy, float dh)
		{
			Bout.Px -= dx;
			Bout.Py -= dy;
			Bout.Ph -= dh;
		}

		public float Tension()
		{
			Noeud noeud = Noeuds[0];
			return Distance3(noeud.X, noeud.Y, noeud.H, Bout.X, Bout.Y, Bout.H) / MathF.Max(1f, Longueur);
		}
	}

	private sealed class Ressort
	{
		private readonly float _raideur;

		private readonly float _amortissement;

		public float X;

		public float V;

		public Ressort(float raideur, float amortissement)
		{
			_raideur = raideur;
			_amortissement = amortissement;
		}

		public void Pas(float dt, float cible, float force = 0f)
		{
			for (float num = dt; num > 1E-06f; num -= 0.004166667f)
			{
				float num2 = MathF.Min(num, 0.004166667f);
				V += ((0f - _raideur) * (X - cible) - _amortissement * V + force) * num2;
				X += V * num2;
			}
		}
	}

	private enum EtatPoisson
	{
		Croisiere,
		Fuite,
		Curieux,
		Ferre
	}

	private sealed class PoissonDuBanc
	{
		public float X;

		public float Y;

		public float Vx;

		public float Vy;

		public float Angle;

		public float Phase;

		public float Alpha = 1f;

		public float AlphaCible = 1f;

		public float Vitesse = 30f;

		public float CibleX;

		public float CibleY;

		public EtatPoisson Etat;

		public long Pause;

		public long Jusqua;

		public long DerniereGrignote = -2305843009213693952L;

		public bool Approche;
	}

	private sealed class Banc
	{
		public readonly List<PoissonDuBanc> Poissons = new List<PoissonDuBanc>();

		public float CentreX;

		public float CentreY;

		public Action<PoissonDuBanc>? SurGrignote;

		public bool Visible => Poissons.Any((PoissonDuBanc poisson) => poisson.AlphaCible > 0f || poisson.Alpha > 0.02f);

		public Banc(float centreX, float centreY, bool epuise)
		{
			CentreX = centreX;
			CentreY = centreY;
			for (int i = 0; i < 3; i++)
			{
				PoissonDuBanc poissonDuBanc = Nouveau(centreX + (float)((i - 1) * 10), centreY + (float)((i - 1) * 3), i);
				if (epuise)
				{
					poissonDuBanc.Alpha = (poissonDuBanc.AlphaCible = 0f);
				}
				Poissons.Add(poissonDuBanc);
			}
		}

		private static PoissonDuBanc Nouveau(float x, float y, int i)
		{
			return new PoissonDuBanc
			{
				X = x,
				Y = y,
				Angle = i * 2,
				CibleX = x,
				CibleY = y,
				Phase = i
			};
		}

		private static void ChoisirCible(PoissonDuBanc poisson, float cx, float cy, float rx, float ry)
		{
			float x = (float)Hasard.NextDouble() * ((float)Math.PI * 2f);
			float num = MathF.Sqrt((float)Hasard.NextDouble());
			poisson.CibleX = cx + MathF.Cos(x) * rx * num;
			poisson.CibleY = cy + MathF.Sin(x) * ry * num;
			poisson.Vitesse = ((Hasard.NextDouble() < 0.35) ? 90 : 30);
		}

		public void Effrayer(float x, float y, long maintenant, float force)
		{
			foreach (PoissonDuBanc poisson in Poissons)
			{
				if (poisson.Etat != EtatPoisson.Ferre)
				{
					float num = poisson.X - x;
					float num2 = poisson.Y - y;
					if (num == 0f && num2 == 0f)
					{
						num = (float)Hasard.NextDouble() - 0.5f;
						num2 = (float)Hasard.NextDouble() - 0.5f;
					}
					float num3 = MathF.Max(0.001f, MathF.Sqrt(num * num + num2 * num2));
					float num4 = 130f * force * (0.8f + (float)Hasard.NextDouble() * 0.4f);
					poisson.Vx = num / num3 * num4;
					poisson.Vy = num2 / num3 * num4 * 0.55f;
					poisson.Etat = EtatPoisson.Fuite;
					poisson.Jusqua = maintenant + 380 + Hasard.Next(180);
				}
			}
		}

		public void Curiosite(long maintenant)
		{
			for (int i = 0; i < Poissons.Count; i++)
			{
				PoissonDuBanc poissonDuBanc = Poissons[i];
				EtatPoisson etat = poissonDuBanc.Etat;
				if ((uint)etat <= 1u)
				{
					poissonDuBanc.Etat = EtatPoisson.Curieux;
					poissonDuBanc.Pause = maintenant + i * 260;
					poissonDuBanc.Jusqua = maintenant + 1100 + i * 650;
				}
			}
		}

		public void Ferrer(float x, float y)
		{
			PoissonDuBanc poissonDuBanc = null;
			float num = float.MaxValue;
			foreach (PoissonDuBanc poisson in Poissons)
			{
				float num2 = (poisson.X - x) * (poisson.X - x) + (poisson.Y - y) * (poisson.Y - y);
				if (num2 < num)
				{
					num = num2;
					poissonDuBanc = poisson;
				}
			}
			if (poissonDuBanc != null)
			{
				poissonDuBanc.Etat = EtatPoisson.Ferre;
			}
		}

		public void SortirLaPrise(float x, float y, long maintenant)
		{
			int num = Poissons.FindIndex((PoissonDuBanc poisson) => poisson.Etat == EtatPoisson.Ferre);
			if (num >= 0)
			{
				Poissons.RemoveAt(num);
			}
			else if (Poissons.Count > 0)
			{
				Poissons.RemoveAt(0);
			}
			Effrayer(x, y, maintenant, 1.3f);
			foreach (PoissonDuBanc poisson in Poissons)
			{
				poisson.AlphaCible = 0f;
			}
		}

		public void Disparaitre()
		{
			foreach (PoissonDuBanc poisson in Poissons)
			{
				poisson.AlphaCible = 0f;
			}
		}

		public void Repeupler()
		{
			Poissons.Clear();
			for (int i = 0; i < 3; i++)
			{
				float x = (float)Math.PI * (0.1f + (float)i * 0.35f);
				PoissonDuBanc poissonDuBanc = Nouveau(CentreX + MathF.Cos(x) * 38f, CentreY + MathF.Sin(x) * 16f, i);
				poissonDuBanc.Alpha = 0f;
				poissonDuBanc.CibleX = CentreX + (float)((i - 1) * 9);
				poissonDuBanc.CibleY = CentreY + (float)((i - 1) * 3);
				poissonDuBanc.Vitesse = 42f;
				Poissons.Add(poissonDuBanc);
			}
		}

		public void Pas(float dt, long maintenant, float bouchonX, float bouchonY)
		{
			bool flag = !float.IsNaN(bouchonX);
			PoissonDuBanc[] array = Poissons.ToArray();
			foreach (PoissonDuBanc poissonDuBanc in array)
			{
				poissonDuBanc.Alpha += (poissonDuBanc.AlphaCible - poissonDuBanc.Alpha) * MathF.Min(1f, dt * 2.6f);
				if (poissonDuBanc.Etat == EtatPoisson.Ferre && flag)
				{
					float k = MathF.Min(1f, dt * 18f);
					poissonDuBanc.X = Lerp(poissonDuBanc.X, bouchonX + MathF.Sin((float)maintenant / 38f) * 1.6f, k);
					poissonDuBanc.Y = Lerp(poissonDuBanc.Y, bouchonY + 2f + MathF.Cos((float)maintenant / 51f) * 0.9f, k);
					poissonDuBanc.Angle += EcartAngle(poissonDuBanc.Angle, (float)Math.PI + MathF.Sin((float)maintenant / 70f) * 0.9f) * MathF.Min(1f, dt * 14f);
					poissonDuBanc.Phase += dt * 30f;
					continue;
				}
				if (poissonDuBanc.Etat == EtatPoisson.Ferre)
				{
					poissonDuBanc.Etat = EtatPoisson.Croisiere;
				}
				if (poissonDuBanc.Etat == EtatPoisson.Fuite)
				{
					float num = MathF.Exp((0f - dt) * 4.5f);
					poissonDuBanc.Vx *= num;
					poissonDuBanc.Vy *= num;
					poissonDuBanc.X += poissonDuBanc.Vx * dt;
					poissonDuBanc.Y += poissonDuBanc.Vy * dt;
					if (maintenant > poissonDuBanc.Jusqua)
					{
						poissonDuBanc.Etat = EtatPoisson.Croisiere;
						poissonDuBanc.Pause = maintenant + 120;
						ChoisirCible(poissonDuBanc, CentreX, CentreY, 22f, 9f);
					}
				}
				else
				{
					if (poissonDuBanc.Etat == EtatPoisson.Curieux && flag)
					{
						float num2 = poissonDuBanc.X - bouchonX;
						float num3 = poissonDuBanc.Y - bouchonY;
						float num4 = MathF.Max(0.001f, MathF.Sqrt(num2 * num2 + num3 * num3));
						if (poissonDuBanc.Pause != 0L && maintenant > poissonDuBanc.Pause)
						{
							poissonDuBanc.Pause = 0L;
							int num5 = (poissonDuBanc.Approche ? 10 : 4);
							poissonDuBanc.Approche = !poissonDuBanc.Approche;
							poissonDuBanc.CibleX = bouchonX + num2 / num4 * (float)num5;
							poissonDuBanc.CibleY = bouchonY + 2f + num3 / num4 * (float)num5 * 0.5f;
							poissonDuBanc.Vitesse = 22f;
						}
						if (num4 < 7f && maintenant - poissonDuBanc.DerniereGrignote > 650 && SurGrignote != null)
						{
							poissonDuBanc.DerniereGrignote = maintenant;
							SurGrignote(poissonDuBanc);
						}
						if (maintenant > poissonDuBanc.Jusqua)
						{
							poissonDuBanc.Etat = EtatPoisson.Croisiere;
						}
					}
					else if (poissonDuBanc.Pause != 0L && maintenant > poissonDuBanc.Pause)
					{
						poissonDuBanc.Pause = 0L;
						if (flag)
						{
							ChoisirCible(poissonDuBanc, bouchonX, bouchonY + 2f, 13f, 5f);
						}
						else
						{
							ChoisirCible(poissonDuBanc, CentreX, CentreY, 22f, 9f);
						}
					}
					if (poissonDuBanc.Pause != 0L && maintenant <= poissonDuBanc.Pause)
					{
						poissonDuBanc.Vx *= 0.8f;
						poissonDuBanc.Vy *= 0.8f;
					}
					else
					{
						float num6 = poissonDuBanc.CibleX - poissonDuBanc.X;
						float num7 = poissonDuBanc.CibleY - poissonDuBanc.Y;
						float num8 = MathF.Sqrt(num6 * num6 + num7 * num7);
						if (num8 < 1.2f)
						{
							poissonDuBanc.Pause = maintenant + ((poissonDuBanc.Etat == EtatPoisson.Curieux) ? (180 + Hasard.Next(260)) : (300 + Hasard.Next(900)));
							poissonDuBanc.Vx = (poissonDuBanc.Vy = 0f);
						}
						else
						{
							float num9 = MathF.Min(num8, poissonDuBanc.Vitesse * dt);
							poissonDuBanc.Vx = num6 / num8 * poissonDuBanc.Vitesse;
							poissonDuBanc.Vy = num7 / num8 * poissonDuBanc.Vitesse;
							poissonDuBanc.X += num6 / num8 * num9;
							poissonDuBanc.Y += num7 / num8 * num9;
						}
					}
				}
				float num10 = MathF.Sqrt(poissonDuBanc.Vx * poissonDuBanc.Vx + poissonDuBanc.Vy * poissonDuBanc.Vy);
				if (num10 > 2f)
				{
					poissonDuBanc.Angle += EcartAngle(poissonDuBanc.Angle, MathF.Atan2(poissonDuBanc.Vy, poissonDuBanc.Vx)) * MathF.Min(1f, dt / 0.12f);
				}
				poissonDuBanc.Phase += dt * (3f + num10 * 0.12f);
			}
		}

		public void Dessiner(float carteX, float carteY, float presence)
		{
			foreach (PoissonDuBanc poisson in Poissons)
			{
				float num = 0.32f * presence * Borne(poisson.Alpha);
				if (!(num <= 0.01f))
				{
					int num2 = (new int[4] { 0, 1, 0, 2 })[(int)MathF.Floor(poisson.Phase) & 3];
					DessinerMatriceTournee(OmbresDePoisson[num2], 6.5f, 2.5f, null, CouleurOmbreDePoisson, carteX + poisson.X, carteY + poisson.Y, poisson.Angle, miroir: false, num);
				}
			}
		}
	}

	private sealed class Masque
	{
		public required int Largeur;

		public required int Hauteur;

		public required byte[] Alphas;

		public required Color[] Couleurs;

		public byte Alpha(int x, int y)
		{
			if (x >= 0 && y >= 0 && x < Largeur && y < Hauteur)
			{
				return Alphas[y * Largeur + x];
			}
			return 0;
		}

		public Color Couleur(int x, int y)
		{
			return Couleurs[Math.Clamp(y, 0, Hauteur - 1) * Largeur + Math.Clamp(x, 0, Largeur - 1)];
		}
	}

	internal const float BouillonRx = 30f;

	internal const float BouillonRy = 13f;

	private const float BouillonSansMasque = 0.5f;

	private static readonly Color CouleurDuBouillon = Hex(862796);

	private static readonly float[] GerbeDuBouillon = new float[3] { 4f, 7f, 4f };

	private static readonly Dictionary<Guid, Bouillon> Bouillons = new Dictionary<Guid, Bouillon>();

	private static long _prochainMenageDesBouillons;

	public const int PorteeDeLancerAuCarre = 10;

	private const float LongueurCanne = 26f;

	private const float GraviteDuLancer = 420f;

	private const long SortirCanne = 110L;

	private const long ArmerCanne = 260L;

	private const long TenirCanne = 340L;

	private const long FouetCanne = 460L;

	private const long LacherBouchon = 440L;

	private const long SuiviCanne = 820L;

	private const long BouchonPose = 900L;

	private const long TirerCanne = 70L;

	private const long PlongeeBouchon = 55L;

	private const long RemonteeBouchon = 270L;

	private const long RetourCanne = 430L;

	private const long AvantMorsure = 480L;

	private const long SalveDeMorsures = 1400L;

	private const long DureeMorsure = 150L;

	private static readonly long[] InstantsDeMorsure = new long[4] { 0L, 180L, 350L, 480L };

	private const long PriseSortie = 70L;

	private const long PriseArc = 560L;

	private const long PriseAuSac = 1560L;

	private const long PriseRanger = 1800L;

	private const long PriseFin = 2150L;

	private static readonly long[] PriseDebats = new long[3] { 780L, 1040L, 1320L };

	private const long SansGesteMs = 4000L;

	private const long RamenerLigneMs = 320L;

	private const long RamenerViteMs = 200L;

	private const long RangerCanneMs = 350L;

	private static readonly Dictionary<Guid, Pecheur> Pecheurs = new Dictionary<Guid, Pecheur>();

	private static readonly Dictionary<(string, int, int, int, int), Silhouette> Silhouettes = new Dictionary<(string, int, int, int, int), Silhouette>();

	private static readonly string[] LignesDuFlotteur = new string[7] { "..a..", ".kRk.", "kRrrk", "krrrk", "kwWwk", ".kwk.", "..k.." };

	private static readonly Color FlotteurAntenne = Hex(16756874);

	private static readonly Color FlotteurContour = Hex(2299420);

	private static readonly Color FlotteurReflet = Hex(16740193);

	private static readonly Color FlotteurRouge = Hex(14102586);

	private static readonly Color FlotteurVentre = Hex(14542314);

	private static readonly Color EcumeDuFlotteur = Hex(15924223);

	private static readonly Color BambouFut = Hex(13870682);

	private static readonly Color BambouNoeud = Hex(9068332);

	private static readonly Color BambouScion = Hex(15784346);

	private static readonly Color BambouPoignee = Hex(8015402);

	private static readonly Color BambouPoigneeSombre = Hex(4861463);

	private static readonly Color BambouFlanc = Hex(7227938);

	private static readonly Color BambouOmbre = Hex(2364939);

	private static readonly Color LigneSurLEau = Hex(16055295);

	private static readonly Color LigneEnLAir = Hex(14543346);

	private static readonly Color AnneauDuPrecis = Hex(16765803);

	private static readonly Color OmbreDeLaPrise = Hex(662036);

	public const int Bucheronnage = 0;

	public const int Minage = 1;

	public const int Cueillette = 2;

	public const int Peche = 3;

	private const int HauteurDuPoing = 15;

	private const long AttenteMs = 40L;

	private const long PredictionMs = 450L;

	private const long BarreVisibleMs = 2500L;

	private const long RepousseMs = 900L;

	private static readonly Dictionary<Guid, Etat> Etats = new Dictionary<Guid, Etat>();

	private static readonly List<Attente> EnAttente = new List<Attente>();

	private static readonly List<Eclat> Eclats = new List<Eclat>();

	private static readonly List<Onde> Ondes = new List<Onde>();

	private static readonly List<Gerbe> Gerbes = new List<Gerbe>();

	private static readonly Dictionary<Guid, Elan> Elans = new Dictionary<Guid, Elan>();

	private static Random Hasard = new Random();

	private static Guid _ressourceDuJoueur;

	private static long _derniereImage;

	private static long _derniereImageSol;

	private static long _prochainMenage;

	private static long _dernierAvertissementDeNiveau;

	private static readonly string[] SonsBois = (from i in Enumerable.Range(1, 10)
		select $"recolte_bois_{i}.wav").ToArray();

	private static readonly string[] SonsBoisFin = new string[3] { "recolte_bois_fin_1.wav", "recolte_bois_fin_2.wav", "recolte_bois_fin_3.wav" };

	private static readonly string[] SonsRoche = (from i in Enumerable.Range(1, 5)
		select $"recolte_roche_{i}.wav").ToArray();

	private static readonly string[] SonsEau = new string[3] { "recolte_eau_1.wav", "recolte_eau_2.wav", "recolte_eau_3.wav" };

	private static readonly Color[] CouleursFeuilles = new Color[3]
	{
		Hex(4160030),
		Hex(7124012),
		Hex(10935370)
	};

	private static readonly Color OmbreEclat = Hex(1708040);

	private static readonly Color CouleurPoussiere = Hex(14206112);

	private static readonly Color CouleurRond = Hex(16777215);

	private static readonly Color BarreContour = Hex(724503);

	private static readonly Color BarreFond = Hex(2765641);

	private static readonly Color BarrePleine = Hex(16742938);

	private static readonly Color BarreReflet = Hex(16755794);

	private static readonly Color BarreGain = Hex(16765850);

	private static readonly Color BarreBord = Hex(16773328);

	private static IGameTexture? _pixel;

	private static readonly Color[] PaletteBois = new Color[7]
	{
		Hex(4139548, 110),
		Hex(5190437),
		Hex(7092511),
		Hex(9393966),
		Hex(10119733),
		Hex(11566657),
		Hex(12816204)
	};

	private const byte K = 1;

	private const byte E = 2;

	private const byte C = 3;

	private const byte R = 4;

	private const byte M = 5;

	private const byte L = 6;

	private const byte W = 7;

	private static readonly byte[][][,] ImagesBois = FabriquerCopeaux();

	private static readonly Color[] PalettePoisson = new Color[8]
	{
		Hex(724503),
		Hex(15233098),
		Hex(16228474),
		Hex(2909592),
		Hex(4887241),
		Hex(8831980),
		Hex(15004924),
		Hex(16777215)
	};

	private static readonly byte[][,] ImagesPoisson = new byte[3][,]
	{
		FabriquerPoisson(0),
		FabriquerPoisson(-1),
		FabriquerPoisson(1)
	};

	private static readonly float[] GerbeTouche = new float[7] { 9f, 13f, 17f, 19f, 16f, 12f, 8f };

	private static readonly float[] GerbePlouf = new float[5] { 5f, 8f, 10f, 8f, 5f };

	private static readonly float[] GerbeColonne = new float[9] { 8f, 12f, 18f, 26f, 30f, 25f, 17f, 11f, 7f };

	private static readonly List<RondDEau> RondsDEau = new List<RondDEau>();

	private static readonly List<GoutteDEau> GouttesDEau = new List<GoutteDEau>();

	private static readonly List<TraceDeGoutte> Traces = new List<TraceDeGoutte>();

	private static readonly Color CouleurTacheHumide = Hex(3029522);

	private static readonly Color OmbreDeGoutte = Hex(727594);

	private static readonly Color GoutteClaire = Hex(12576511);

	private static readonly (int Dx, int Dy)[][] MicroRond = new(int, int)[4][]
	{
		new(int, int)[1] { (0, 0) },
		new(int, int)[3]
		{
			(-1, 0),
			(0, 0),
			(1, 0)
		},
		new(int, int)[4]
		{
			(-2, 0),
			(-1, 0),
			(1, 0),
			(2, 0)
		},
		new(int, int)[2]
		{
			(-2, 0),
			(2, 0)
		}
	};

	private const double FrissonDuree = 0.55;

	private const double FrissonFrequence = 2.2;

	private static readonly Dictionary<(IGameTexture Texture, int X, int Y, int Largeur, int Hauteur), ModeleArbre?> ArbresRecolte = new Dictionary<(IGameTexture, int, int, int, int), ModeleArbre>();

	private const long AnticipationDuCoup = 110L;

	private const long ContactDuCoup = 130L;

	private const long EboulisApres = 780L;

	private const long EboulisFondu = 380L;

	private const long MorceauxFonduDebut = 850L;

	private const long MorceauxFonduDuree = 320L;

	private const long ButinApres = 420L;

	private const float GraviteDesDebris = 420f;

	private static readonly Dictionary<Guid, PoseDeFrappe> PosesDeFrappe = new Dictionary<Guid, PoseDeFrappe>();

	private static readonly List<EclatDePierre> EclatsDePierre = new List<EclatDePierre>();

	private static readonly List<GrainDePierre> GrainsDePierre = new List<GrainDePierre>();

	private static readonly List<PoussiereDeRoche> PoussieresDeRoche = new List<PoussiereDeRoche>();

	private static readonly List<ButinDeRoche> ButinsDeRoche = new List<ButinDeRoche>();

	private static readonly List<ChocDeFrappe> ChocsDeFrappe = new List<ChocDeFrappe>();

	private static readonly int[] Bayer = new int[16]
	{
		0, 8, 2, 10, 12, 4, 14, 6, 3, 11,
		1, 9, 15, 7, 13, 5
	};

	private static readonly Color LumiereDeFaille = new Color(255, 255, 250, 230);

	private static readonly Color TonDePoussiere = new Color(255, 228, 214, 186);

	private static readonly Color OmbreDeDebris = Hex(1840397);

	private static readonly Color ChocBlanc = Hex(16777215);

	private static readonly Color ChocChaud = Hex(16771248);

	private static long _dernierTic;

	private static readonly string[] LignesPierre = new string[7] { "..kkkkk..", ".kLLLMMk.", "kLLLMMMMk", "kLMMMMDDk", "kMMMDDDDk", ".kMDDDDk.", "..kkkkk.." };

	private static readonly string[] LignesCuivre = new string[8] { "..kkkkk..", ".kLLOLLk.", "kLLooLMMk", "kMMMMOoMk", "kOoMMooDk", "kMooMDDDk", ".kDDDDDk.", "..kkkkk.." };

	private static readonly string[] LignesCristal = new string[10] { "...k...", "..kWk..", "..kWBk.", ".kWWBk.", ".kWBBBk", "kkWBBBk", "kBWBBDk", "kBWBBDk", ".kBBDk.", ".kkkkk." };

	private static readonly Color ContourDeButin = Hex(1777453);

	private static readonly Color EauClaire = Hex(9425141);

	private static readonly Color[] CouleursGouttes = new Color[2]
	{
		Hex(16777215),
		Hex(12576511)
	};

	private static readonly string[][] OmbresDePoisson = new string[3][]
	{
		new string[5] { "....xxxx.....", "x.xxxxxxxxx..", "xxxxxxxxxxxxx", "x.xxxxxxxxx..", "....xxxx....." },
		new string[5] { "x...xxxx.....", "xxxxxxxxxxx..", ".xxxxxxxxxxxx", "..xxxxxxxxx..", "....xxxx....." },
		new string[5] { "....xxxx.....", "..xxxxxxxxx..", ".xxxxxxxxxxxx", "xxxxxxxxxxx..", "x...xxxx....." }
	};

	private static readonly Color CouleurOmbreDePoisson = Hex(862796);

	private static readonly Dictionary<(IGameTexture, int, int, int, int), Masque?> Masques = new Dictionary<(IGameTexture, int, int, int, int), Masque>();

	private static int TailleCase => Options.Instance.Map.TileWidth;

	private static int HauteurCase => Options.Instance.Map.TileHeight;

	internal static bool CentreDuCoin(Resource ressource, out float x, out float y)
	{
		x = 0f;
		y = 0f;
		if (!MapInstance.TryGet(ressource.MapId, out MapInstance instance))
		{
			return false;
		}
		x = (float)(instance.X + ressource.X * TailleCase) + (float)TailleCase / 2f;
		y = (float)(instance.Y + ressource.Y * HauteurCase) + (float)HauteurCase / 2f;
		return true;
	}

	internal static void SurvolerLesCoinsDePeche()
	{
		foreach (Entity value in Globals.Entities.Values)
		{
			if (value is Resource resource)
			{
				ResourceDescriptor descriptor = resource.Descriptor;
				if (descriptor != null && descriptor.Tool == 3)
				{
					resource.SurvolerLeCoinDePeche();
				}
			}
		}
	}

	internal static bool SurLeBouillon(Resource ressource, float x, float y)
	{
		if (!CentreDuCoin(ressource, out var x2, out var y2) || !MapInstance.TryGet(ressource.MapId, out MapInstance instance))
		{
			return false;
		}
		int largeur;
		byte[] array = Meteo.EauDeLaCarte(instance, out largeur);
		float num = ((array == null) ? 0.5f : 1f);
		float num2 = (x - x2) / (30f * num);
		float num3 = (y - y2 - 1f) / (13f * num);
		if (num2 * num2 + num3 * num3 <= 1f)
		{
			if (array != null)
			{
				return Meteo.EstDeLEau(x, y);
			}
			return true;
		}
		return false;
	}

	private static float DessinerBouillon(Resource ressource, MapInstance carte, float cx, float cy, bool onYPeche, long maintenant, float dt)
	{
		if (maintenant > _prochainMenageDesBouillons)
		{
			_prochainMenageDesBouillons = maintenant + 5000;
			Guid[] array = Bouillons.Keys.Where((Guid id) => !Globals.Entities.ContainsKey(id)).ToArray();
			foreach (Guid key in array)
			{
				Bouillons.Remove(key);
			}
		}
		if (!Bouillons.TryGetValue(ressource.Id, out Bouillon value))
		{
			value = new Bouillon
			{
				Force = (ressource.IsDead ? 0f : 1f)
			};
			for (int num2 = 0; num2 < value.Poissons.Length; num2++)
			{
				value.Poissons[num2] = new PoissonQuiTourne
				{
					Angle = (float)num2 / (float)value.Poissons.Length * ((float)Math.PI * 2f) + ((float)Hasard.NextDouble() - 0.5f) * 0.6f,
					Rayon = 7f + (float)Hasard.NextDouble() * 12f,
					Vitesse = 1.7f + (float)Hasard.NextDouble() * 0.9f,
					Phase = num2
				};
			}
			Bouillons[ressource.Id] = value;
		}
		float num3 = dt / 1000f;
		float num4 = (ressource.IsDead ? 0f : (onYPeche ? 0.35f : 1f));
		value.Force += (num4 - value.Force) * MathF.Min(1f, num3 * 2.6f);
		float force = value.Force;
		if (force <= 0.01f)
		{
			return 0f;
		}
		int largeur;
		byte[] array2 = Meteo.EauDeLaCarte(carte, out largeur);
		float num5 = ((array2 == null) ? 0.5f : 1f);
		float num6 = MathF.Sin((float)maintenant / 700f + (float)(ressource.X * 7) + (float)(ressource.Y * 13)) * 1.5f * num5;
		float num7 = cy + 1f;
		RemplirEllipseDansLEau(carte, array2, largeur, cx, num7, 30f * num5 + num6, 13f * num5 + num6 * 0.4f, Fondu(CouleurDuBouillon, 0.12f * force));
		RemplirEllipseDansLEau(carte, array2, largeur, cx, num7, 22f * num5 + num6, 9.5f * num5 + num6 * 0.4f, Fondu(CouleurDuBouillon, 0.12f * force));
		RemplirEllipseDansLEau(carte, array2, largeur, cx, num7, 13f * num5, 5.5f * num5, Fondu(CouleurDuBouillon, 0.11f * force));
		if (ressource.IsHovered || ressource.CibleDeRecolteAuClic)
		{
			Color couleurDuLisere = ressource.CouleurDuLisere;
			TracerEllipseDansLEau(carte, array2, largeur, cx, num7, 30f * num5 + 1f, 13f * num5 + 1f, new Color(220, couleurDuLisere.R, couleurDuLisere.G, couleurDuLisere.B));
		}
		float num8 = Borne((force - 0.35f) / 0.65f);
		for (int num9 = 0; num9 < value.Poissons.Length; num9++)
		{
			PoissonQuiTourne poissonQuiTourne = value.Poissons[num9];
			poissonQuiTourne.Angle += poissonQuiTourne.Vitesse * num3;
			float num10 = poissonQuiTourne.Rayon * num5 * (1f + 0.18f * MathF.Sin((float)maintenant / 770f + (float)num9 * 1.7f)) * (1f + (1f - force) * 1.5f);
			float num11 = cx + MathF.Cos(poissonQuiTourne.Angle) * num10;
			float num12 = num7 + MathF.Sin(poissonQuiTourne.Angle) * num10 * 0.42f;
			float num13 = 0f;
			if (!poissonQuiTourne.Place)
			{
				poissonQuiTourne.Place = true;
				poissonQuiTourne.Cap = poissonQuiTourne.Angle + (float)Math.PI / 2f;
			}
			else if (num3 > 0f)
			{
				float num14 = (num11 - poissonQuiTourne.X) / num3;
				float num15 = (num12 - poissonQuiTourne.Y) / num3;
				num13 = MathF.Sqrt(num14 * num14 + num15 * num15);
				if (num13 > 2f)
				{
					poissonQuiTourne.Cap += EcartAngle(poissonQuiTourne.Cap, MathF.Atan2(num15, num14)) * MathF.Min(1f, num3 / 0.12f);
				}
			}
			poissonQuiTourne.X = num11;
			poissonQuiTourne.Y = num12;
			poissonQuiTourne.Phase += num3 * (3f + MathF.Min(num13, 60f) * 0.12f);
			float num16 = 0.55f * num8;
			if (!(num16 <= 0.01f) && EauSous(carte, array2, largeur, (int)MathF.Floor(num11), (int)MathF.Floor(num12)))
			{
				int num17 = (new int[4] { 0, 1, 0, 2 })[(int)MathF.Floor(poissonQuiTourne.Phase) & 3];
				DessinerMatriceTournee(OmbresDePoisson[num17], 6.5f, 2.5f, null, CouleurOmbreDePoisson, (float)carte.X + num11, (float)carte.Y + num12, poissonQuiTourne.Cap, miroir: false, num16);
			}
		}
		if (!ressource.IsDead && !onYPeche && force > 0.6f && maintenant >= value.ProchaineBulle)
		{
			value.ProchaineBulle = maintenant + 160 + Hasard.Next(160);
			float x = (float)Hasard.NextDouble() * ((float)Math.PI * 2f);
			float num18 = MathF.Sqrt((float)Hasard.NextDouble());
			float x2 = cx + MathF.Cos(x) * 15f * num5 * num18;
			float num19 = num7 + MathF.Sin(x) * 6f * num5 * num18;
			if (EauSous(carte, array2, largeur, (int)MathF.Floor(x2), (int)MathF.Floor(num19)))
			{
				Gerbes.Add(new Gerbe(ressource.MapId, x2, num19, maintenant, Hasard.Next(), 0.7f, GerbeDuBouillon));
				Ondes.Add(new Onde(ressource.MapId, x2, num19, maintenant, 420L, Poussiere: false, 0.55f));
				for (int num20 = 0; num20 < 2; num20++)
				{
					Eclats.Add(new Eclat
					{
						Genre = Genre.Goutte,
						MapId = ressource.MapId,
						X = x2,
						Y = num19 - 2f,
						Vx = ((float)Hasard.NextDouble() - 0.5f) * 1.2f,
						Vy = -1.3f - (float)Hasard.NextDouble() * 0.6f,
						Sol = num19 + (float)Hasard.NextDouble(),
						Couleur = CouleursGouttes[Hasard.Next(2)],
						Taille = 1,
						Ne = maintenant + 30
					});
				}
				if (Gerbes.Count > 40)
				{
					Gerbes.RemoveAt(0);
				}
			}
		}
		return force;
	}

	private static bool EauSous(MapInstance carte, byte[]? masque, int largeur, int px, int py)
	{
		if (masque == null || largeur <= 0)
		{
			return true;
		}
		int num = masque.Length / largeur;
		if (px < 0 || py < 0 || px >= largeur || py >= num)
		{
			return Meteo.EstDeLEau((float)(carte.X + px) + 0.5f, (float)(carte.Y + py) + 0.5f);
		}
		return masque[py * largeur + px] != 0;
	}

	private static void RemplirEllipseDansLEau(MapInstance carte, byte[]? masque, int largeur, float cx, float cy, float rx, float ry, Color couleur)
	{
		if (rx <= 0f || ry <= 0f || couleur.A == 0)
		{
			return;
		}
		for (int i = (int)MathF.Floor(cy - ry); i <= (int)MathF.Ceiling(cy + ry); i++)
		{
			float num = ((float)i + 0.5f - cy) / ry;
			if (num * num > 1f)
			{
				continue;
			}
			float num2 = rx * MathF.Sqrt(1f - num * num);
			int num3 = (int)MathF.Round(cx - num2);
			int num4 = (int)MathF.Round(cx + num2);
			int j = num3;
			while (j < num4)
			{
				for (; j < num4 && !EauSous(carte, masque, largeur, j, i); j++)
				{
				}
				int num5 = j;
				for (; j < num4 && EauSous(carte, masque, largeur, j, i); j++)
				{
				}
				if (j > num5)
				{
					Rect(carte.X + num5, carte.Y + i, j - num5, 1f, couleur);
				}
			}
		}
	}

	private static void TracerEllipseDansLEau(MapInstance carte, byte[]? masque, int largeur, float cx, float cy, float rx, float ry, Color couleur)
	{
		HashSet<(int, int)> hashSet = new HashSet<(int, int)>();
		int num = Math.Max(12, (int)((float)Math.PI * (rx + ry) * 1.5f));
		for (int i = 0; i < num; i++)
		{
			float x = (float)i / (float)num * ((float)Math.PI * 2f);
			int num2 = (int)MathF.Floor(cx + MathF.Cos(x) * rx);
			int num3 = (int)MathF.Floor(cy + MathF.Sin(x) * ry);
			if (hashSet.Add((num2, num3)) && EauSous(carte, masque, largeur, num2, num3))
			{
				Rect(carte.X + num2, carte.Y + num3, 1f, 1f, couleur);
			}
		}
	}

	private static Color CouleurDuFlotteur(char lettre)
	{
		return lettre switch
		{
			'a' => FlotteurAntenne, 
			'k' => FlotteurContour, 
			'R' => FlotteurReflet, 
			'r' => FlotteurRouge, 
			'w' => FlotteurVentre, 
			_ => Color.White, 
		};
	}

	public static Resource? CoinDePecheDevant(Entity joueur)
	{
		Resource result = null;
		int num = int.MaxValue;
		foreach (Entity value in Globals.Entities.Values)
		{
			if (!(value is Resource { IsDead: false } resource))
			{
				continue;
			}
			ResourceDescriptor? descriptor = resource.Descriptor;
			if (descriptor == null || descriptor.Tool != 3 || resource.MapId != joueur.MapId)
			{
				continue;
			}
			int num2 = resource.X - joueur.X;
			int num3 = resource.Y - joueur.Y;
			if (DansLeConeDeLancer(joueur.DirectionFacing, num2, num3))
			{
				int num4 = num2 * num2 + num3 * num3;
				if (num4 < num)
				{
					num = num4;
					result = resource;
				}
			}
		}
		return result;
	}

	private static bool DansLeConeDeLancer(Direction direction, int dx, int dy)
	{
		int num = dx * dx + dy * dy;
		if (num == 0 || num > 10)
		{
			return false;
		}
		return direction switch
		{
			Direction.Right => dx >= 1 && Math.Abs(dy) <= dx, 
			Direction.Left => dx <= -1 && Math.Abs(dy) <= -dx, 
			Direction.Down => dy >= 1 && Math.Abs(dx) <= dy, 
			Direction.Up => dy <= -1 && Math.Abs(dx) <= -dy, 
			Direction.UpLeft => dx <= 0 && dy <= 0, 
			Direction.UpRight => dx >= 0 && dy <= 0, 
			Direction.DownLeft => dx <= 0 && dy >= 0, 
			Direction.DownRight => dx >= 0 && dy >= 0, 
			_ => false, 
		};
	}

	private static bool AttaquableDevant(Entity entite)
	{
		int num = entite.X;
		int num2 = entite.Y;
		switch (entite.DirectionFacing)
		{
		case Direction.Up:
			num2--;
			break;
		case Direction.Down:
			num2++;
			break;
		case Direction.Left:
			num--;
			break;
		case Direction.Right:
			num++;
			break;
		case Direction.UpLeft:
			num--;
			num2--;
			break;
		case Direction.UpRight:
			num++;
			num2--;
			break;
		case Direction.DownLeft:
			num--;
			num2++;
			break;
		case Direction.DownRight:
			num++;
			num2++;
			break;
		}
		foreach (Entity value in Globals.Entities.Values)
		{
			if (value == entite || value.MapId != entite.MapId || value.X != num || value.Y != num2 || !value.CanBeAttacked || value.IsDying)
			{
				continue;
			}
			if (value is Resource resource)
			{
				ResourceDescriptor? descriptor = resource.Descriptor;
				if (descriptor != null && descriptor.Tool == 3)
				{
					continue;
				}
			}
			return true;
		}
		return false;
	}

	public static int? PoseDePeche(Entity entite)
	{
		if (!(entite is Player) || entite.IsMoving)
		{
			return null;
		}
		if (Pecheurs.TryGetValue(entite.Id, out Pecheur value) && !value.Termine)
		{
			return ImageDuPecheur(value, Timing.Global.Milliseconds);
		}
		if (!entite.IsAttacking || CoinDePecheDevant(entite) == null || AttaquableDevant(entite))
		{
			return null;
		}
		return 0;
	}

	private static FloatRect DecalageDePeche(Entity entite, FloatRect rectangle)
	{
		if (Pecheurs.Count == 0 || !Pecheurs.TryGetValue(entite.Id, out Pecheur value) || value.Termine || entite.IsMoving)
		{
			return rectangle;
		}
		long milliseconds = Timing.Global.Milliseconds;
		float num = Arrondi(Recul(value, milliseconds));
		int num2 = SautDeJoie(value, milliseconds);
		if (num == 0f && num2 == 0)
		{
			return rectangle;
		}
		Orientation orientation = OrientationDe(DirectionDuSprite(value.Direction));
		return new FloatRect(rectangle.X - (float)orientation.AvantX * num, rectangle.Y - (float)orientation.AvantY * num - (float)num2, rectangle.Width, rectangle.Height);
	}

	private static Direction DirectionDuSprite(Direction direction)
	{
		switch (direction)
		{
		case Direction.Left:
		case Direction.UpLeft:
		case Direction.DownLeft:
			return Direction.Left;
		case Direction.Right:
		case Direction.UpRight:
		case Direction.DownRight:
			return Direction.Right;
		case Direction.Up:
			return Direction.Up;
		default:
			return Direction.Down;
		}
	}

	private static Orientation OrientationDe(Direction direction)
	{
		return direction switch
		{
			Direction.Left => new Orientation(-1, 0, 0f), 
			Direction.Right => new Orientation(1, 0, 0f), 
			Direction.Up => new Orientation(0, -1, 3f), 
			_ => new Orientation(0, 1, 2f), 
		};
	}

	private static Silhouette SilhouetteDe(IGameTexture? texture, FloatRect source)
	{
		int num = (int)source.Width;
		int num2 = (int)source.Height;
		Silhouette silhouette = new Silhouette((int)((float)num * 0.28f), (int)MathF.Ceiling((float)num * 0.75f), (int)((float)num2 * 0.35f), (int)MathF.Ceiling((float)num2 * 0.96f));
		if (texture == null || num <= 0 || num2 <= 0)
		{
			return silhouette;
		}
		(string, int, int, int, int) key = (texture.Name ?? string.Empty, (int)source.X, (int)source.Y, num, num2);
		if (Silhouettes.TryGetValue(key, out var value))
		{
			return value;
		}
		Masque masque = MasqueDe(texture, source);
		if (masque == null)
		{
			return silhouette;
		}
		int num3 = masque.Largeur;
		int num4 = 0;
		int num5 = masque.Hauteur;
		int num6 = 0;
		for (int i = 0; i < masque.Hauteur; i++)
		{
			for (int j = 0; j < masque.Largeur; j++)
			{
				if (masque.Alpha(j, i) >= 128)
				{
					num3 = Math.Min(num3, j);
					num4 = Math.Max(num4, j + 1);
					num5 = Math.Min(num5, i);
					num6 = Math.Max(num6, i + 1);
				}
			}
		}
		Silhouette silhouette2 = ((num4 > num3 && num6 > num5) ? new Silhouette(num3, num4, num5, num6) : silhouette);
		if (Silhouettes.Count > 256)
		{
			Silhouettes.Clear();
		}
		Silhouettes[key] = silhouette2;
		return silhouette2;
	}

	private static (float X, float Y) MainDansLImage(Silhouette silhouette, Direction direction)
	{
		int num = silhouette.Droite - silhouette.Gauche;
		int num2 = silhouette.Bas - silhouette.Haut;
		float num3 = (float)(silhouette.Gauche + silhouette.Droite) / 2f;
		float item = (float)silhouette.Bas - MathF.Round((float)num2 * 0.3f);
		return (X: direction switch
		{
			Direction.Right => MathF.Floor(num3 + (float)num * 0.1f), 
			Direction.Left => MathF.Floor(num3 - (float)num * 0.1f), 
			_ => silhouette.Droite - 2, 
		}, Y: item);
	}

	private static Pecheur NouveauLancer(Entity entite, Resource coin, long maintenant)
	{
		Pecheur pecheur = new Pecheur
		{
			Coin = coin.Id,
			CoinMapId = coin.MapId,
			CoinX = coin.X,
			CoinY = coin.Y,
			MapId = entite.MapId,
			TuileX = entite.X,
			TuileY = entite.Y,
			Direction = entite.DirectionFacing,
			Phase = PhaseDePeche.Lancer,
			Debut = maintenant,
			DernierGeste = maintenant,
			Precedent = maintenant - 1,
			DernierPas = maintenant
		};
		Pecheurs[entite.Id] = pecheur;
		return pecheur;
	}

	private static bool GesteDePeche(Resource coin, Entity? pecheurEntite, bool precis, long maintenant)
	{
		if (!(pecheurEntite is Player))
		{
			return false;
		}
		if (!Pecheurs.TryGetValue(pecheurEntite.Id, out Pecheur value) || value.Termine || value.Coin != coin.Id || value.Phase != PhaseDePeche.Lancer)
		{
			NouveauLancer(pecheurEntite, coin, maintenant).LancerPrecis = precis;
			return true;
		}
		value.DernierGeste = maintenant;
		value.MordDepuis = -1L;
		EtatFlotteur flotteur = value.Flotteur;
		if ((uint)flotteur <= 2u)
		{
			value.FerrerEnAttente = true;
			value.FerrerEnAttentePrecis |= precis;
			return true;
		}
		value.Ferrer = maintenant;
		value.FerrerPrecis = precis;
		return true;
	}

	private static bool PrecisDePeche(Resource coin, Entity? pecheurEntite, long maintenant)
	{
		if (pecheurEntite == null || !Pecheurs.TryGetValue(pecheurEntite.Id, out Pecheur value) || value.Termine || value.Coin != coin.Id)
		{
			return false;
		}
		if (value.Phase != PhaseDePeche.Lancer)
		{
			return true;
		}
		EtatFlotteur flotteur = value.Flotteur;
		if ((uint)flotteur <= 2u)
		{
			if (value.FerrerEnAttente)
			{
				value.FerrerEnAttentePrecis = true;
			}
			else
			{
				value.LancerPrecis = true;
			}
			return true;
		}
		if (maintenant - value.Ferrer < 400 && !value.FerrerPrecis)
		{
			value.FerrerPrecis = true;
			if (maintenant - value.Ferrer >= 55)
			{
				AnneauDore(value, maintenant);
				AjouterGerbe(value.CoinMapId, value.Fx, value.Fy, maintenant, GerbeTouche, 0.9f);
				ProjeterGouttes(value.CoinMapId, value.Fx, value.Fy, maintenant, 3, 1.1f, CentreDuCoinX(value), CentreDuCoinY(value));
			}
		}
		return true;
	}

	private static bool PriseDePeche(Resource coin, Entity? pecheurEntite, long maintenant)
	{
		if (!(pecheurEntite is Player))
		{
			return false;
		}
		if (!Pecheurs.TryGetValue(pecheurEntite.Id, out Pecheur value) || value.Termine || value.Coin != coin.Id || value.Phase != PhaseDePeche.Lancer)
		{
			NouveauLancer(pecheurEntite, coin, maintenant).PriseEnAttente = true;
			return true;
		}
		value.DernierGeste = maintenant;
		value.MordDepuis = -1L;
		EtatFlotteur flotteur = value.Flotteur;
		if ((uint)flotteur <= 2u)
		{
			value.PriseEnAttente = true;
			return true;
		}
		CommencerPrise(value, maintenant);
		return true;
	}

	private static void ArmerMorsure(Resource coin, Entity? pecheurEntite, long maintenant)
	{
		if (pecheurEntite != null && Pecheurs.TryGetValue(pecheurEntite.Id, out Pecheur value) && !value.Termine && !(value.Coin != coin.Id) && value.Phase == PhaseDePeche.Lancer && value.MordDepuis < 0)
		{
			long num = Math.Max(value.Ferrer, value.Debut + 900);
			value.MordDepuis = Math.Max(maintenant + 1, num + 480);
			value.MorsuresFaites = 0;
		}
	}

	private static void CommencerPrise(Pecheur pecheur, long debut)
	{
		pecheur.Phase = PhaseDePeche.Prise;
		pecheur.Debut = debut;
		pecheur.MordDepuis = -1L;
		pecheur.FerrerEnAttente = false;
		pecheur.PriseEnAttente = false;
	}

	private static void CommencerRamener(Pecheur pecheur, long maintenant, bool rapide)
	{
		if (pecheur.Phase == PhaseDePeche.Ramener)
		{
			return;
		}
		pecheur.AngleAuRamener = AngleCanne(pecheur, maintenant);
		pecheur.Phase = PhaseDePeche.Ramener;
		pecheur.Debut = maintenant;
		pecheur.DureeRamener = (rapide ? 200 : 320);
		pecheur.MordDepuis = -1L;
		pecheur.Poisson = null;
		if (pecheur.Flotteur == EtatFlotteur.Range || !pecheur.CordePrete)
		{
			pecheur.Flotteur = EtatFlotteur.Range;
			pecheur.DureeRamener = 0L;
			return;
		}
		if (pecheur.Flotteur == EtatFlotteur.Eau)
		{
			AjouterRond(pecheur.CoinMapId, pecheur.Fx, pecheur.Fy + 1f, maintenant, 7f, 380L, 0.6f);
		}
		Noeud bout = pecheur.Corde.Bout;
		pecheur.RameneDepuis = new Point3(bout.X, bout.Y, bout.H);
		pecheur.LongueurAuRamener = pecheur.Corde.Longueur;
		pecheur.Flotteur = EtatFlotteur.Ramene;
	}

	private static Pecheur? PecheurDuCoin(Guid coin)
	{
		foreach (Pecheur value in Pecheurs.Values)
		{
			if (!value.Termine && value.Coin == coin)
			{
				return value;
			}
		}
		return null;
	}

	private static float CentreDuCoinX(Pecheur pecheur)
	{
		return (float)(pecheur.CoinX * TailleCase) + (float)TailleCase / 2f;
	}

	private static float CentreDuCoinY(Pecheur pecheur)
	{
		return (float)(pecheur.CoinY * HauteurCase) + (float)HauteurCase / 2f;
	}

	private static (float X, float Y) CibleDuBouchon(Pecheur pecheur)
	{
		return (X: CentreDuCoinX(pecheur) - (float)(Math.Sign(pecheur.CoinX - pecheur.TuileX) * 5), Y: CentreDuCoinY(pecheur) - (float)(Math.Sign(pecheur.CoinY - pecheur.TuileY) * 3) + 1f);
	}

	private static void Grignoter(Guid coin)
	{
		Pecheur pecheur = PecheurDuCoin(coin);
		long milliseconds = Timing.Global.Milliseconds;
		if (pecheur != null && pecheur.Phase == PhaseDePeche.Lancer && pecheur.Flotteur == EtatFlotteur.Eau && milliseconds - Math.Max(pecheur.Ferrer, pecheur.Pose) >= 500 && milliseconds - pecheur.DernierTwitch >= 520)
		{
			pecheur.DernierTwitch = milliseconds;
			pecheur.Creux.V += 70f;
			AjouterRond(pecheur.CoinMapId, pecheur.Fx, pecheur.Fy + 1f, milliseconds, 6f, 380L, 0.55f);
		}
	}

	private static void SonDePeche(string fichier, Entity? source)
	{
		if (source != null)
		{
			CombatAudio.Play(fichier, source, 0.06f);
		}
	}

	private static float AngleCanne(Pecheur pecheur, long t)
	{
		float num = 32f + MathF.Sin((float)t / 760f) * 1.2f;
		switch (pecheur.Phase)
		{
		case PhaseDePeche.Prise:
		{
			long num6 = t - pecheur.Debut;
			if (num6 < 0)
			{
				return num;
			}
			if (num6 < 80)
			{
				return Lerp(30f, 98f, Sortie((float)num6 / 80f));
			}
			if (num6 < 900)
			{
				return Lerp(98f, 48f, Lisse((float)(num6 - 80) / 820f));
			}
			if (num6 < 1800)
			{
				return 48f + MathF.Sin((float)t / 500f);
			}
			return Lerp(48f, 118f, Lisse((float)(num6 - 1800) / 350f));
		}
		case PhaseDePeche.Ramener:
		{
			long num5 = t - pecheur.Debut;
			if (num5 < pecheur.DureeRamener)
			{
				return Lerp(pecheur.AngleAuRamener, 40f, Lisse((float)num5 / (float)Math.Max(1L, pecheur.DureeRamener)));
			}
			return Lerp(40f, 118f, Lisse((float)(num5 - pecheur.DureeRamener) / 350f));
		}
		default:
		{
			if (pecheur.Ferrer > pecheur.Debut && t >= pecheur.Ferrer)
			{
				long num2 = t - pecheur.Ferrer;
				float num3 = (pecheur.FerrerPrecis ? 78f : 64f);
				if (num2 < 70)
				{
					return Lerp(32f, num3, Sortie((float)num2 / 70f));
				}
				if (num2 < 430)
				{
					return Lerp(num3, num, Lisse((float)(num2 - 70) / 360f));
				}
				return num - 11f * Morsure(pecheur, t);
			}
			long num4 = t - pecheur.Debut;
			if (num4 < 110)
			{
				return Lerp(-55f, 72f, Sortie((float)num4 / 110f));
			}
			if (num4 < 260)
			{
				return Lerp(72f, 124f, Sortie((float)(num4 - 110) / 150f));
			}
			if (num4 < 340)
			{
				return Lerp(124f, 129f, Lisse((float)(num4 - 260) / 80f));
			}
			if (num4 < 460)
			{
				return Lerp(129f, -14f, Entree((float)(num4 - 340) / 120f));
			}
			if (num4 < 820)
			{
				return Lerp(-14f, num, Depasse((float)(num4 - 460) / 360f, 1.4f));
			}
			return num - 11f * Morsure(pecheur, t);
		}
		}
	}

	private static float AlphaCanne(Pecheur pecheur, long t)
	{
		switch (pecheur.Phase)
		{
		case PhaseDePeche.Lancer:
			return Borne((float)(t - pecheur.Debut) / 60f);
		case PhaseDePeche.Prise:
		{
			long num2 = t - pecheur.Debut;
			if (num2 <= 1950)
			{
				return 1f;
			}
			return Borne(1f - (float)(num2 - 1950) / 200f);
		}
		default:
		{
			long num = t - pecheur.Debut - pecheur.DureeRamener;
			if (num <= 0)
			{
				return 1f;
			}
			return Borne(1f - (float)num / 350f);
		}
		}
	}

	private static long InstantDeMorsure(Pecheur pecheur, int numero)
	{
		return pecheur.MordDepuis + (long)(numero / 4) * 1400L + InstantsDeMorsure[numero % 4];
	}

	private static float Morsure(Pecheur pecheur, long t)
	{
		if (pecheur.MordDepuis < 0 || pecheur.MorsuresFaites == 0)
		{
			return 0f;
		}
		long num = t - InstantDeMorsure(pecheur, pecheur.MorsuresFaites - 1);
		if (num < 0 || num >= 150)
		{
			return 0f;
		}
		return MathF.Sin((float)Math.PI * (float)num / 150f);
	}

	private static float Recul(Pecheur pecheur, long t)
	{
		switch (pecheur.Phase)
		{
		case PhaseDePeche.Prise:
		{
			long num3 = t - pecheur.Debut;
			if (num3 < 0)
			{
				return 0f;
			}
			if (num3 < 60)
			{
				return (float)(2 * num3) / 60f;
			}
			if (num3 >= 380)
			{
				return Lerp(2f, 0f, Lisse((float)(num3 - 380) / 220f));
			}
			return 2f;
		}
		case PhaseDePeche.Ramener:
			return 0f;
		default:
		{
			if (pecheur.Ferrer > pecheur.Debut && t >= pecheur.Ferrer)
			{
				long num = t - pecheur.Ferrer;
				if (num < 160)
				{
					if (num >= 40)
					{
						return 1f;
					}
					return (float)num / 40f;
				}
				if (num >= 280)
				{
					return 0f - Morsure(pecheur, t);
				}
				return 1f - (float)(num - 160) / 120f;
			}
			long num2 = t - pecheur.Debut;
			if (num2 < 110)
			{
				return 0f;
			}
			if (num2 < 340)
			{
				return Lerp(0f, 1f, Borne((float)(num2 - 110) / 120f));
			}
			if (num2 < 500)
			{
				return Lerp(1f, -2f, Borne((float)(num2 - 340) / 140f));
			}
			if (num2 >= 820)
			{
				return 0f - Morsure(pecheur, t);
			}
			return Lerp(-2f, 0f, Lisse((float)(num2 - 460 - 40) / 320f));
		}
		}
	}

	private static int SautDeJoie(Pecheur pecheur, long t)
	{
		if (pecheur.Phase != PhaseDePeche.Prise)
		{
			return 0;
		}
		long num = t - pecheur.Debut - 700;
		if (num < 0 || num >= 130)
		{
			return 0;
		}
		return (int)Arrondi(2f * MathF.Sin((float)Math.PI * (float)num / 130f));
	}

	private static int ImageDuPecheur(Pecheur pecheur, long t)
	{
		long num = t - pecheur.Debut;
		switch (pecheur.Phase)
		{
		case PhaseDePeche.Lancer:
			if (num >= 340 && num < 640)
			{
				return 1;
			}
			break;
		case PhaseDePeche.Prise:
			if (num >= 0 && num < 420)
			{
				return 1;
			}
			break;
		}
		return 0;
	}

	private static float Mou(Pecheur pecheur, long t)
	{
		if (pecheur.Ferrer > pecheur.Debut && t >= pecheur.Ferrer)
		{
			long num = t - pecheur.Ferrer;
			if (num < 40)
			{
				return Lerp(1.14f, 0.95f, (float)num / 40f);
			}
			if (num < 260)
			{
				return 0.95f;
			}
			if (num >= 700)
			{
				return 1.14f - 0.19f * Morsure(pecheur, t);
			}
			return Lerp(0.95f, 1.14f, Lisse((float)(num - 260) / 440f));
		}
		long num2 = t - pecheur.Debut;
		if (num2 >= 900)
		{
			return Lerp(1.01f, 1.14f, Lisse((float)(num2 - 900) / 380f)) - 0.19f * Morsure(pecheur, t);
		}
		return 1.01f;
	}

	private static float TirFlotteur(Pecheur pecheur, long t)
	{
		if (pecheur.Ferrer > pecheur.Debut && t >= pecheur.Ferrer)
		{
			long num = t - pecheur.Ferrer;
			float num2 = (pecheur.FerrerPrecis ? 6f : 4f);
			if (num < 60)
			{
				return num2 * Sortie((float)num / 60f);
			}
			return num2 * (1f - Lisse((float)(num - 60) / 700f)) - 2.5f * Morsure(pecheur, t);
		}
		return -2.5f * Morsure(pecheur, t);
	}

	private static float Enfoncement(Pecheur pecheur, long t)
	{
		if (pecheur.Ferrer > pecheur.Debut && t >= pecheur.Ferrer)
		{
			long num = t - pecheur.Ferrer;
			if (num >= 55 && num < 270)
			{
				return 8f;
			}
		}
		return 4.5f * Morsure(pecheur, t);
	}

	public static void DessinerCanne(Entity entite, IGameTexture? texture, FloatRect source, FloatRect destination, bool devant)
	{
		if (Pecheurs.Count == 0 || !Pecheurs.TryGetValue(entite.Id, out Pecheur value) || value.Termine)
		{
			return;
		}
		if (_pixel == null)
		{
			_pixel = Graphics.Renderer?.WhitePixel;
		}
		if (_pixel != null)
		{
			long milliseconds = Timing.Global.Milliseconds;
			if (!devant)
			{
				MettreAJourPecheur(value, entite, texture, source, destination, milliseconds);
			}
			if (!value.Termine && value.Derriere != devant && MapInstance.TryGet(value.CoinMapId, out MapInstance instance))
			{
				DessinerAttirail(value, instance.X, instance.Y);
			}
		}
	}

	private static void MettreAJourPecheur(Pecheur pecheur, Entity entite, IGameTexture? texture, FloatRect source, FloatRect destination, long maintenant)
	{
		float num = (float)Math.Clamp(maintenant - pecheur.DernierPas, 0L, 50L) / 1000f;
		pecheur.DernierPas = maintenant;
		if (!MapInstance.TryGet(pecheur.CoinMapId, out MapInstance instance) || !Globals.Entities.TryGetValue(pecheur.Coin, out Entity value) || !(value is Resource coin))
		{
			pecheur.Termine = true;
			return;
		}
		bool flag = entite.MapId != pecheur.MapId || entite.X != pecheur.TuileX || entite.Y != pecheur.TuileY || entite.IsMoving || DirectionDuSprite(entite.DirectionFacing) != DirectionDuSprite(pecheur.Direction);
		if (pecheur.Phase != PhaseDePeche.Ramener && (flag || (pecheur.Phase == PhaseDePeche.Lancer && pecheur.Flotteur == EtatFlotteur.Eau && maintenant - pecheur.DernierGeste > 4000)))
		{
			CommencerRamener(pecheur, maintenant, flag);
		}
		Direction direction = DirectionDuSprite(pecheur.Direction);
		Orientation orientation = OrientationDe(direction);
		Silhouette silhouette = SilhouetteDe(texture, source);
		(float X, float Y) tuple = MainDansLImage(silhouette, direction);
		float item = tuple.X;
		float item2 = tuple.Y;
		float num2 = destination.Y - (float)instance.Y + (float)silhouette.Bas + (float)SautDeJoie(pecheur, maintenant);
		float num3 = destination.X - (float)instance.X + item;
		float num4 = num2 - (destination.Y - (float)instance.Y + item2);
		pecheur.Main = new Point3(num3, num2, num4);
		float num5 = AngleCanne(pecheur, maintenant);
		float x = num5 * (float)Math.PI / 180f;
		float num6 = MathF.Cos(x);
		float num7 = MathF.Sin(x);
		Point3 point = new Point3(num3 + 26f * num6 * (float)orientation.AvantX + orientation.Cote * MathF.Max(0f, num7), num2 + 26f * num6 * (float)orientation.AvantY, num4 + 26f * num7);
		float num8 = num3;
		float num9 = num2 - num4;
		float x2 = point.X;
		float num10 = point.Y - point.H;
		float num11 = MathF.Atan2(num10 - num9, x2 - num8);
		if (num > 0f)
		{
			float num12 = (float.IsNaN(pecheur.PhiPrec) ? 0f : (EcartAngle(pecheur.PhiPrec, num11) / num));
			float num13 = (num12 - pecheur.OmegaPrec) / num;
			pecheur.PhiPrec = num11;
			pecheur.OmegaPrec = num12;
			float cible = 0f;
			if (pecheur.CordePrete && pecheur.Flotteur != EtatFlotteur.Range)
			{
				float num14 = pecheur.Corde.Tension();
				if (num14 > 0.96f)
				{
					Noeud noeud = pecheur.Corde.Noeuds[3];
					cible = Math.Clamp(EcartAngle(num11, MathF.Atan2(noeud.Y - noeud.H - num10, noeud.X - x2)), -1f, 1f) * Borne((num14 - 0.96f) * 5f) * 0.45f;
				}
			}
			pecheur.Flexion.Pas(num, cible, Math.Clamp((0f - num13) * 0.08f, -90f, 90f));
			pecheur.Flexion.X = Math.Clamp(pecheur.Flexion.X, -0.7f, 0.7f);
		}
		float num15 = MathF.Sqrt((x2 - num8) * (x2 - num8) + (num10 - num9) * (num10 - num9));
		float x3 = num11 + pecheur.Flexion.X;
		float num16 = num8 + MathF.Cos(x3) * num15;
		float num17 = num9 + MathF.Sin(x3) * num15;
		Point3 point2 = (pecheur.Bout = new Point3(point.X + (num16 - x2), point.Y, point.H - (num17 - num10)));
		pecheur.CanneMainX = num8;
		pecheur.CanneMainY = num9;
		pecheur.CanneControleX = num8 + (x2 - num8) * 0.55f;
		pecheur.CanneControleY = num9 + (num10 - num9) * 0.55f;
		pecheur.CanneBoutX = num16;
		pecheur.CanneBoutY = num17;
		pecheur.Derriere = direction == Direction.Up || num5 > 90f;
		pecheur.Alpha = AlphaCanne(pecheur, maintenant);
		Evenements(pecheur, coin, entite, maintenant);
		if (pecheur.Termine)
		{
			return;
		}
		Corde corde = pecheur.Corde;
		if (pecheur.Flotteur == EtatFlotteur.Range && pecheur.Phase == PhaseDePeche.Lancer && maintenant - pecheur.Debut < 440)
		{
			pecheur.Flotteur = EtatFlotteur.Main;
			corde.Placer(point2, point2 with
			{
				H = point2.H - 6f
			});
			pecheur.CordePrete = true;
		}
		if (pecheur.Flotteur == EtatFlotteur.Range)
		{
			return;
		}
		if (!pecheur.CordePrete)
		{
			corde.Placer(point2, new Point3(pecheur.Fx, pecheur.Fy, pecheur.Fh));
			pecheur.CordePrete = true;
		}
		Point3? b = null;
		float masse = 1f;
		switch (pecheur.Flotteur)
		{
		case EtatFlotteur.Main:
			corde.Longueur = 7f;
			masse = 1.5f;
			break;
		case EtatFlotteur.Vol:
			pecheur.Fx += pecheur.Fvx * num;
			pecheur.Fy += pecheur.Fvy * num;
			pecheur.Fh = MathF.Max(0f, pecheur.Fh + pecheur.Fvh * num);
			pecheur.Fvh -= 420f * num;
			pecheur.Fangle += pecheur.Ftour * num;
			corde.Longueur = MathF.Max(corde.Longueur, Distance3(point2.X, point2.Y, point2.H, pecheur.Fx, pecheur.Fy, pecheur.Fh) * 1.01f);
			b = new Point3(pecheur.Fx, pecheur.Fy, pecheur.Fh);
			break;
		case EtatFlotteur.Eau:
		{
			(float X, float Y) tuple2 = CibleDuBouchon(pecheur);
			float item3 = tuple2.X;
			float item4 = tuple2.Y;
			float num19 = pecheur.Main.X - item3;
			float num20 = pecheur.Main.Y - item4;
			float num21 = MathF.Max(0.001f, MathF.Sqrt(num19 * num19 + num20 * num20));
			float num22 = TirFlotteur(pecheur, maintenant);
			pecheur.Fx = item3 + num19 / num21 * num22;
			pecheur.Fy = item4 + num20 / num21 * num22 * 0.5f;
			pecheur.Creux.Pas(num, Enfoncement(pecheur, maintenant) + MathF.Sin((float)maintenant / 620f) * 0.5f);
			corde.Longueur = MathF.Max(4f, Distance3(point2.X, point2.Y, point2.H, pecheur.Fx, pecheur.Fy, 0f) * Mou(pecheur, maintenant));
			b = new Point3(pecheur.Fx, pecheur.Fy, 0f);
			break;
		}
		case EtatFlotteur.SurLigne:
			b = MettreAJourPrise(pecheur, point2, maintenant, num, out masse);
			break;
		case EtatFlotteur.Ramene:
		{
			float num18 = ((pecheur.DureeRamener <= 0) ? 1f : Borne((float)(maintenant - pecheur.Debut) / (float)pecheur.DureeRamener));
			float k = Lisse(num18);
			b = new Point3(Lerp(pecheur.RameneDepuis.X, point2.X, k), Lerp(pecheur.RameneDepuis.Y, point2.Y, k), Lerp(pecheur.RameneDepuis.H, point2.H - 3f, k) + MathF.Sin((float)Math.PI * num18) * 5f);
			corde.Longueur = MathF.Max(2f, Lerp(pecheur.LongueurAuRamener, 3f, k));
			if (num18 >= 1f)
			{
				pecheur.Flotteur = EtatFlotteur.Range;
				return;
			}
			break;
		}
		}
		if (num > 0f)
		{
			int num23 = Math.Max(1, (int)MathF.Ceiling(num * 120f));
			for (int i = 0; i < num23; i++)
			{
				corde.Pas(num / (float)num23, point2, b, masse);
			}
		}
		if (pecheur.Flotteur == EtatFlotteur.Main)
		{
			pecheur.Fx = corde.Bout.X;
			pecheur.Fy = corde.Bout.Y;
			pecheur.Fh = corde.Bout.H;
		}
	}

	private static void Evenements(Pecheur pecheur, Resource coin, Entity entite, long maintenant)
	{
		long avant = pecheur.Precedent;
		pecheur.Precedent = maintenant;
		Banc banc = EtatDe(coin).Banc;
		switch (pecheur.Phase)
		{
		case PhaseDePeche.Lancer:
			if (Franchi(pecheur.Debut + 340))
			{
				SonDePeche("recolte_peche_lancer.wav", entite);
			}
			if (Franchi(pecheur.Debut + 440))
			{
				Lacher(pecheur);
			}
			if (Franchi(pecheur.Debut + 900))
			{
				Amerrir(pecheur, coin, banc, pecheur.Debut + 900, maintenant);
			}
			if (Franchi(pecheur.Pose + 560) || Franchi(pecheur.Ferrer + 600))
			{
				banc?.Curiosite(maintenant);
			}
			if (pecheur.Flotteur == EtatFlotteur.Eau && Franchi(pecheur.Ferrer + 55))
			{
				Plonger(pecheur, coin, banc, pecheur.Ferrer + 55, maintenant);
			}
			while (pecheur.Phase == PhaseDePeche.Lancer && pecheur.MordDepuis >= 0 && pecheur.Flotteur == EtatFlotteur.Eau && InstantDeMorsure(pecheur, pecheur.MorsuresFaites) <= maintenant)
			{
				long num = InstantDeMorsure(pecheur, pecheur.MorsuresFaites);
				if (maintenant - num < 150)
				{
					Mordre(pecheur, coin, banc, pecheur.MorsuresFaites, num);
				}
				pecheur.MorsuresFaites++;
			}
			break;
		case PhaseDePeche.Prise:
		{
			if (Franchi(pecheur.Debut + 70))
			{
				SortirLaPrise(pecheur, coin, banc, pecheur.Debut + 70, maintenant);
			}
			if (Franchi(pecheur.Debut + 70 + 560) && pecheur.Poisson != null)
			{
				pecheur.Poisson.Etat = EtatPrise.Pendu;
			}
			long[] priseDebats = PriseDebats;
			foreach (long num2 in priseDebats)
			{
				if (Franchi(pecheur.Debut + num2))
				{
					Debattre(pecheur, entite, pecheur.Debut + num2);
				}
			}
			if (Franchi(pecheur.Debut + 1560))
			{
				PoissonPris poisson = pecheur.Poisson;
				if (poisson != null)
				{
					poisson.Etat = EtatPrise.Panier;
					poisson.Panier = pecheur.Debut + 1560;
					poisson.DepuisX = poisson.Cx;
					poisson.DepuisY = poisson.Cy;
				}
			}
			if (maintenant >= pecheur.Debut + 2150)
			{
				pecheur.Termine = true;
			}
			break;
		}
		default:
			if (maintenant >= pecheur.Debut + pecheur.DureeRamener + 350)
			{
				pecheur.Termine = true;
			}
			break;
		}
		bool Franchi(long instant)
		{
			if (avant < instant)
			{
				return instant <= maintenant;
			}
			return false;
		}
	}

	private static void Lacher(Pecheur pecheur)
	{
		(float X, float Y) tuple = CibleDuBouchon(pecheur);
		float item = tuple.X;
		float item2 = tuple.Y;
		Point3 point = (pecheur.CordePrete ? new Point3(pecheur.Corde.Bout.X, pecheur.Corde.Bout.Y, pecheur.Corde.Bout.H) : new Point3(item, item2, 0f));
		float num = 0.46f;
		pecheur.Flotteur = EtatFlotteur.Vol;
		pecheur.Fx = point.X;
		pecheur.Fy = point.Y;
		pecheur.Fh = point.H;
		pecheur.Fvx = (item - point.X) / num;
		pecheur.Fvy = (item2 - point.Y) / num;
		pecheur.Fvh = (0f - point.H + 210f * num * num) / num;
		pecheur.Fangle = 0f;
		pecheur.Ftour = 15f;
	}

	private static void Amerrir(Pecheur pecheur, Resource coin, Banc? banc, long instant, long maintenant)
	{
		(float X, float Y) tuple = CibleDuBouchon(pecheur);
		float item = tuple.X;
		float item2 = tuple.Y;
		pecheur.Flotteur = EtatFlotteur.Eau;
		pecheur.Fx = item;
		pecheur.Fy = item2;
		pecheur.Fh = 0f;
		pecheur.Pose = instant;
		pecheur.Creux.X = 5f;
		pecheur.Creux.V = 0f;
		Guid coinMapId = pecheur.CoinMapId;
		AjouterGerbe(coinMapId, item, item2, instant, GerbePlouf, 0.55f);
		AjouterRond(coinMapId, item, item2 + 1f, instant, 9f, 520L, 0.85f);
		AjouterRond(coinMapId, item, item2 + 1f, instant + 120, 14f, 680L, 0.5f);
		ProjeterGouttes(coinMapId, item, item2, instant, 4, 0.6f, CentreDuCoinX(pecheur), CentreDuCoinY(pecheur));
		banc?.Effrayer(item, item2, maintenant, 1f);
		SonDePeche($"recolte_peche_plouf_{1 + Hasard.Next(3)}.wav", coin);
		if (pecheur.LancerPrecis)
		{
			AnneauDore(pecheur, instant + 20);
			pecheur.LancerPrecis = false;
		}
		if (pecheur.PriseEnAttente)
		{
			CommencerPrise(pecheur, instant + 60);
		}
		else if (pecheur.FerrerEnAttente)
		{
			pecheur.Ferrer = instant + 60;
			pecheur.FerrerPrecis = pecheur.FerrerEnAttentePrecis;
			pecheur.FerrerEnAttente = false;
			pecheur.FerrerEnAttentePrecis = false;
		}
	}

	private static void Plonger(Pecheur pecheur, Resource coin, Banc? banc, long instant, long maintenant)
	{
		bool ferrerPrecis = pecheur.FerrerPrecis;
		Guid coinMapId = pecheur.CoinMapId;
		AjouterGerbe(coinMapId, pecheur.Fx, pecheur.Fy, instant, GerbeTouche, ferrerPrecis ? 1.15f : 0.85f, (int)(instant % 100000));
		AjouterRond(coinMapId, pecheur.Fx, pecheur.Fy, instant, 13f, 560L, 0.75f);
		AjouterRond(coinMapId, pecheur.Fx, pecheur.Fy, instant + 130, 17f, 620L, 0.45f);
		ProjeterGouttes(coinMapId, pecheur.Fx, pecheur.Fy, instant, ferrerPrecis ? 7 : 4, ferrerPrecis ? 1.15f : 0.9f, CentreDuCoinX(pecheur), CentreDuCoinY(pecheur));
		banc?.Effrayer(pecheur.Fx, pecheur.Fy, maintenant, 0.8f);
		SonDePeche(ferrerPrecis ? $"recolte_peche_precis_{1 + Hasard.Next(3)}.wav" : $"recolte_eau_{1 + Hasard.Next(3)}.wav", coin);
		if (ferrerPrecis)
		{
			AnneauDore(pecheur, instant + 20);
		}
	}

	private static void AnneauDore(Pecheur pecheur, long instant)
	{
		AjouterRond(pecheur.CoinMapId, pecheur.Fx, pecheur.Fy, instant, 19f, 420L, 0.95f, AnneauDuPrecis, epais: true);
	}

	private static void Mordre(Pecheur pecheur, Resource coin, Banc? banc, int numero, long instant)
	{
		if (numero == 0)
		{
			banc?.Ferrer(pecheur.Fx, pecheur.Fy);
		}
		Guid coinMapId = pecheur.CoinMapId;
		AjouterGerbe(coinMapId, pecheur.Fx, pecheur.Fy, instant, GerbePlouf, 0.42f + (float)(numero % 4) * 0.05f);
		AjouterRond(coinMapId, pecheur.Fx, pecheur.Fy + 1f, instant, 8f, 420L);
		ProjeterGouttes(coinMapId, pecheur.Fx, pecheur.Fy, instant, 2, 0.5f, CentreDuCoinX(pecheur), CentreDuCoinY(pecheur));
		SonDePeche($"recolte_peche_mord_{1 + Hasard.Next(3)}.wav", coin);
	}

	private static void SortirLaPrise(Pecheur pecheur, Resource coin, Banc? banc, long instant, long maintenant)
	{
		float num;
		float num2;
		if (pecheur.Flotteur != EtatFlotteur.Eau)
		{
			(num, num2) = CibleDuBouchon(pecheur);
		}
		else
		{
			float fx = pecheur.Fx;
			float fy = pecheur.Fy;
			num2 = fy;
			num = fx;
		}
		Guid coinMapId = pecheur.CoinMapId;
		AjouterGerbe(coinMapId, num, num2, instant, GerbeColonne, 1.2f);
		AjouterRond(coinMapId, num, num2, instant, 14f, 600L, 0.85f);
		AjouterRond(coinMapId, num, num2, instant + 110, 19f, 700L, 0.6f);
		AjouterRond(coinMapId, num, num2, instant + 230, 24f, 800L, 0.4f);
		ProjeterGouttes(coinMapId, num, num2, instant, 9, 1.25f, CentreDuCoinX(pecheur), CentreDuCoinY(pecheur));
		banc?.SortirLaPrise(num, num2, maintenant);
		pecheur.Poisson = new PoissonPris
		{
			Etat = EtatPrise.Vol,
			Ne = instant,
			DepartX = num,
			DepartY = num2,
			Cx = num,
			Cy = num2,
			SolX = num,
			SolY = num2
		};
		pecheur.Flotteur = EtatFlotteur.SurLigne;
		SonDePeche("recolte_eau_prise.wav", coin);
	}

	private static void Debattre(Pecheur pecheur, Entity entite, long instant)
	{
		PoissonPris poisson = pecheur.Poisson;
		if (poisson != null)
		{
			poisson.Debat = instant;
			if (poisson.Etat == EtatPrise.Pendu)
			{
				pecheur.Corde.Impulsion((float)((Hasard.Next(2) != 0) ? 1 : (-1)) * 1.6f, 0f, 0.9f);
			}
			for (int i = 0; i < 3; i++)
			{
				AjouterGoutteDEau(pecheur.CoinMapId, poisson.SolX + ((float)Hasard.NextDouble() - 0.5f) * 6f, poisson.SolY, poisson.Hauteur, ((float)Hasard.NextDouble() - 0.5f) * 30f, 0f, 20f + (float)Hasard.NextDouble() * 25f, instant + i * 30, 1, float.NaN, 0f);
			}
			SonDePeche($"recolte_peche_debat_{1 + Hasard.Next(2)}.wav", entite);
		}
	}

	private static Point3? MettreAJourPrise(Pecheur pecheur, Point3 bout, long maintenant, float dt, out float masse)
	{
		masse = 1f;
		Corde corde = pecheur.Corde;
		PoissonPris poisson = pecheur.Poisson;
		if (poisson == null)
		{
			corde.Longueur = Lerp(corde.Longueur, 2f, MathF.Min(1f, dt * 10f));
			return bout with
			{
				H = bout.H - 2f
			};
		}
		float num = Borne(1f - (float)(maintenant - poisson.Debat) / 240f);
		poisson.Image = (int)((num > 0f) ? (maintenant / 30 % 3) : (new int[4] { 0, 1, 0, 2 })[(int)(maintenant / 70 % 4)]);
		switch (poisson.Etat)
		{
		case EtatPrise.Vol:
		{
			float arriveeX = bout.X;
			float arriveeY = bout.Y;
			float arriveeH = bout.H - 11f;
			Point3 Arc(float k) => new Point3(Lerp(poisson.DepartX, arriveeX, k), Lerp(poisson.DepartY, arriveeY, k), Lerp(0f, arriveeH, k) + MathF.Sin(MathF.PI * k) * 30f);
			float num4 = Borne((float)(maintenant - poisson.Ne) / 560f);
			Point3 point = Arc(num4);
			Point3 point2 = Arc(MathF.Max(0f, num4 - 0.04f));
			float num5 = point.X - point2.X;
			float y = point.Y - point.H - (point2.Y - point2.H);
			poisson.Miroir = num5 < 0f;
			poisson.Angle = (poisson.Miroir ? MathF.Atan2(y, 0f - num5) : MathF.Atan2(y, num5));
			poisson.Cx = point.X;
			poisson.Cy = point.Y - point.H;
			poisson.SolX = point.X;
			poisson.SolY = point.Y;
			poisson.Hauteur = point.H;
			if (maintenant - poisson.DerniereGoutte > 60 && num4 < 0.8f)
			{
				poisson.DerniereGoutte = maintenant;
				AjouterGoutteDEau(pecheur.CoinMapId, point.X, point.Y, point.H, 0f, 0f, -10f, maintenant, 1, CentreDuCoinX(pecheur), CentreDuCoinY(pecheur));
			}
			(float X, float Y) tuple = BoucheDuPoisson(poisson.Cx, poisson.Cy, poisson.Angle, poisson.Miroir);
			float item = tuple.X;
			float item2 = tuple.Y;
			Point3 value = new Point3(item, point.Y, point.Y - item2);
			corde.Longueur = MathF.Max(4f, Distance3(bout.X, bout.Y, bout.H, value.X, value.Y, value.H) * 0.99f);
			return value;
		}
		case EtatPrise.Pendu:
		{
			Noeud bout2 = corde.Bout;
			Noeud noeud = corde.Noeuds[15];
			corde.Longueur = Lerp(corde.Longueur, 11f, MathF.Min(1f, dt * 6f));
			float num6 = MathF.Atan2(noeud.Y - noeud.H - (bout2.Y - bout2.H), noeud.X - bout2.X) + MathF.Sin((float)maintenant / 32f) * 0.55f * num;
			poisson.Miroir = false;
			poisson.Angle = num6;
			poisson.Cx = bout2.X - (8.5f * MathF.Cos(num6) + 0.5f * MathF.Sin(num6));
			poisson.Cy = bout2.Y - bout2.H - (8.5f * MathF.Sin(num6) - 0.5f * MathF.Cos(num6));
			poisson.SolX = bout2.X;
			poisson.SolY = bout2.Y;
			poisson.Hauteur = bout2.H;
			masse = 5f;
			return null;
		}
		default:
		{
			float num2 = Borne((float)(maintenant - poisson.Panier) / 240f);
			float x = pecheur.Main.X;
			float num3 = pecheur.Main.Y - pecheur.Main.H;
			poisson.Cx = Lerp(poisson.DepuisX, x, Lisse(num2));
			poisson.Cy = Lerp(poisson.DepuisY, num3 + 2f, Lisse(num2));
			poisson.Alpha = 1f - Sortie(Borne((num2 - 0.4f) / 0.6f));
			if (num2 >= 1f)
			{
				pecheur.Poisson = null;
			}
			corde.Longueur = Lerp(corde.Longueur, 6f, MathF.Min(1f, dt * 8f));
			return new Point3(poisson.Cx, pecheur.Main.Y, pecheur.Main.Y - poisson.Cy);
		}
		}
	}

	private static (float X, float Y) BoucheDuPoisson(float x, float y, float angle, bool miroir)
	{
		float num = MathF.Cos(angle);
		float num2 = MathF.Sin(angle);
		int num3 = ((!miroir) ? 1 : (-1));
		return (X: Arrondi(x) + (float)num3 * (8.5f * num + 0.5f * num2), Y: Arrondi(y) + 8.5f * num2 - 0.5f * num);
	}

	private static void DessinerAttirail(Pecheur pecheur, float ox, float oy)
	{
		float alpha = pecheur.Alpha;
		if (alpha <= 0f)
		{
			return;
		}
		Noeud[] noeuds = pecheur.Corde.Noeuds;
		bool flag = pecheur.CordePrete && pecheur.Flotteur != EtatFlotteur.Range;
		if (flag)
		{
			DessinerLigne(noeuds, ox, oy, alpha);
		}
		DessinerBambou(pecheur, ox, oy, alpha);
		if (flag)
		{
			switch (pecheur.Flotteur)
			{
			case EtatFlotteur.Main:
			case EtatFlotteur.Ramene:
			{
				Noeud noeud3 = noeuds[16];
				Noeud noeud4 = noeuds[15];
				DessinerFlotteur(ox + noeud3.X, oy + noeud3.Y - noeud3.H + 3f, MathF.Atan2(noeud4.X - noeud3.X, noeud3.Y - noeud3.H - (noeud4.Y - noeud4.H)), alpha);
				break;
			}
			case EtatFlotteur.Vol:
				DessinerFlotteur(ox + pecheur.Fx, oy + pecheur.Fy - pecheur.Fh, pecheur.Fangle, alpha);
				break;
			case EtatFlotteur.SurLigne:
			{
				Noeud noeud = noeuds[12];
				Noeud noeud2 = noeuds[11];
				DessinerFlotteur(ox + noeud.X, oy + noeud.Y - noeud.H, MathF.Atan2(noeud2.X - noeud.X, noeud.Y - noeud.H - (noeud2.Y - noeud2.H)), alpha);
				break;
			}
			case EtatFlotteur.Eau:
				break;
			}
		}
	}

	private static void DessinerLigne(Noeud[] noeuds, float ox, float oy, float alpha)
	{
		HashSet<(int, int)> hashSet = new HashSet<(int, int)>();
		int num = 0;
		Color couleur = Fondu(Color.White, alpha * 0.25f);
		for (int i = 0; i < noeuds.Length - 1; i++)
		{
			Noeud noeud = noeuds[i];
			Noeud noeud2 = noeuds[i + 1];
			bool flag = noeud.H < 0.6f && noeud2.H < 0.6f;
			Color couleur2 = Fondu(flag ? LigneSurLEau : LigneEnLAir, alpha * (flag ? 0.9f : 0.7f));
			foreach (var (num2, num3) in Segment((int)Arrondi(ox + noeud.X), (int)Arrondi(oy + noeud.Y - noeud.H), (int)Arrondi(ox + noeud2.X), (int)Arrondi(oy + noeud2.Y - noeud2.H)))
			{
				if (hashSet.Add((num2, num3)))
				{
					Rect(num2, num3, 1f, 1f, couleur2);
					if (flag && num % 3 == 0)
					{
						Rect(num2, num3 + 1, 1f, 1f, couleur);
					}
					num++;
				}
			}
		}
	}

	private static void DessinerBambou(Pecheur pecheur, float ox, float oy, float alpha)
	{
		float num = ox + pecheur.CanneMainX;
		float num2 = oy + pecheur.CanneMainY;
		float num3 = ox + pecheur.CanneControleX;
		float num4 = oy + pecheur.CanneControleY;
		float num5 = ox + pecheur.CanneBoutX;
		float num6 = oy + pecheur.CanneBoutY;
		HashSet<(int, int)> hashSet = new HashSet<(int, int)>();
		List<(int, int, float, int, int)> list = new List<(int, int, float, int, int)>(40);
		for (int i = 0; i <= 72; i++)
		{
			float num7 = (float)i / 72f;
			float num8 = 1f - num7;
			int item = (int)Arrondi(num8 * num8 * num + 2f * num8 * num7 * num3 + num7 * num7 * num5);
			int item2 = (int)Arrondi(num8 * num8 * num2 + 2f * num8 * num7 * num4 + num7 * num7 * num6);
			if (hashSet.Add((item, item2)))
			{
				float num9 = 2f * num8 * (num3 - num) + 2f * num7 * (num5 - num3);
				float num10 = 2f * num8 * (num4 - num2) + 2f * num7 * (num6 - num4);
				float num11 = MathF.Max(0.001f, MathF.Sqrt(num9 * num9 + num10 * num10));
				float num12 = (0f - num10) / num11;
				float num13 = num9 / num11;
				if (num13 < -0.2f || (MathF.Abs(num13) <= 0.2f && num12 < 0f))
				{
					num12 = 0f - num12;
					num13 = 0f - num13;
				}
				list.Add((item, item2, num7 * 26f, (int)Arrondi(num12), (int)Arrondi(num13)));
			}
		}
		foreach (var item3 in list)
		{
			if (!(item3.Item3 < 3f) && !(item3.Item3 > 23f))
			{
				bool flag = item3.Item3 < 11f;
				Rect(item3.Item1 + item3.Item4, item3.Item2 + item3.Item5, 1f, 1f, Fondu(flag ? BambouFlanc : BambouOmbre, alpha * (flag ? 1f : 0.5f)));
			}
		}
		foreach (var item4 in list)
		{
			Color couleur = ((item4.Item3 < 1.5f) ? BambouPoignee : ((item4.Item3 < 5f) ? BambouPoigneeSombre : ((item4.Item3 > 20f) ? BambouScion : ((MathF.Abs(item4.Item3 - 9f) < 0.8f || MathF.Abs(item4.Item3 - 15f) < 0.8f || MathF.Abs(item4.Item3 - 21f) < 0.8f) ? BambouNoeud : BambouFut))));
			Rect(item4.Item1, item4.Item2, 1f, 1f, Fondu(couleur, alpha));
		}
	}

	private static void DessinerFlotteur(float x, float y, float angle, float alpha)
	{
		DessinerMatriceTournee(LignesDuFlotteur, 2.5f, 3.5f, CouleurDuFlotteur, Color.White, x, y, angle, miroir: false, alpha);
	}

	private static void DessinerFlotteurDansLEau(float x, float y, float enfoncement, long maintenant, float alpha)
	{
		float num = Arrondi(x);
		float num2 = Arrondi(y);
		float num3 = Arrondi(enfoncement);
		for (int i = 0; i < LignesDuFlotteur.Length; i++)
		{
			float num4 = num2 + (float)(i - 4) + num3;
			if (num4 >= num2)
			{
				continue;
			}
			string text = LignesDuFlotteur[i];
			for (int j = 0; j < text.Length; j++)
			{
				char c = text[j];
				if (c != '.')
				{
					Color couleur = CouleurDuFlotteur(c);
					Rect(num + (float)j - 2f, num4, 1f, 1f, Fondu(couleur, alpha));
					if (c != 'k')
					{
						float num5 = Arrondi(MathF.Sin((float)maintenant / 180f + (float)i) * 0.6f);
						Rect(num + (float)j - 2f + num5, num2 + (num2 - num4), 1f, 1f, Fondu(couleur, alpha * 0.2f));
					}
				}
			}
		}
		if (num3 < 5f)
		{
			Color couleur2 = Fondu(EcumeDuFlotteur, alpha * 0.6f);
			Rect(num - 3f, num2, 2f, 1f, couleur2);
			Rect(num + 2f, num2, 2f, 1f, couleur2);
			Rect(num - 1f, num2, 3f, 1f, Fondu(EcumeDuFlotteur, alpha * 0.3f));
		}
	}

	private static void DessinerPecheAuSol(long maintenant)
	{
		foreach (Pecheur value in Pecheurs.Values)
		{
			if (value.Termine || value.Alpha <= 0f || !MapInstance.TryGet(value.CoinMapId, out MapInstance instance))
			{
				continue;
			}
			if (value.Flotteur == EtatFlotteur.Eau)
			{
				DessinerFlotteurDansLEau((float)instance.X + value.Fx, (float)instance.Y + value.Fy, value.Creux.X, maintenant, value.Alpha);
			}
			PoissonPris poisson = value.Poisson;
			if (poisson != null && poisson.Etat != EtatPrise.Panier && poisson.Hauteur > 1f)
			{
				float num = 0.26f * Borne(1f - poisson.Hauteur / 60f);
				if (num > 0.02f)
				{
					Color couleur = Fondu(OmbreDeLaPrise, num);
					float num2 = Arrondi((float)instance.X + poisson.SolX);
					float num3 = Arrondi((float)instance.Y + poisson.SolY);
					Rect(num2 - 2f, num3 - 1f, 5f, 1f, couleur);
					Rect(num2 - 3f, num3, 7f, 1f, couleur);
					Rect(num2 - 2f, num3 + 1f, 5f, 1f, couleur);
				}
			}
		}
	}

	private static void DessinerPoissonsPris()
	{
		foreach (Pecheur value in Pecheurs.Values)
		{
			if (!value.Termine)
			{
				PoissonPris poisson = value.Poisson;
				if (poisson != null && MapInstance.TryGet(value.CoinMapId, out MapInstance instance))
				{
					DessinerPoissonImage((float)instance.X + poisson.Cx, (float)instance.Y + poisson.Cy, poisson.Angle, poisson.Miroir, poisson.Image, poisson.Alpha);
				}
			}
		}
	}

	private static void NettoyerPecheurs(long maintenant)
	{
		if (Pecheurs.Count == 0)
		{
			return;
		}
		KeyValuePair<Guid, Pecheur>[] array = Pecheurs.ToArray();
		for (int i = 0; i < array.Length; i++)
		{
			KeyValuePair<Guid, Pecheur> keyValuePair = array[i];
			var (key, pecheur2) = keyValuePair;
			if (pecheur2.Termine || !Globals.Entities.ContainsKey(key) || maintenant - pecheur2.DernierPas > 10000)
			{
				Pecheurs.Remove(key);
			}
		}
	}

	public static int Metier(Resource ressource)
	{
		return ressource.Descriptor?.Tool switch
		{
			0 => 0, 
			1 => 1, 
			3 => 3, 
			_ => 2, 
		};
	}

	private static Etat EtatDe(Resource ressource)
	{
		if (!Etats.TryGetValue(ressource.Id, out Etat value))
		{
			value = new Etat();
			Etats[ressource.Id] = value;
		}
		return value;
	}

	public static bool NiveauTropBas(Resource ressource, Entity joueur, bool avertir)
	{
		if (joueur is Player player)
		{
			ResourceDescriptor descriptor = ressource.Descriptor;
			if (descriptor != null && descriptor.ProfessionLevel > 1)
			{
				int num = Professions.Of(descriptor);
				if (num >= 0 && player.ProfessionLevel(num) < descriptor.ProfessionLevel)
				{
					long milliseconds = Timing.Global.Milliseconds;
					if (avertir && milliseconds - _dernierAvertissementDeNiveau > 1000)
					{
						_dernierAvertissementDeNiveau = milliseconds;
						ChatboxMsg.AddMessage(new ChatboxMsg(Strings.Professions.LevelRequired.ToString(Professions.Name(num), descriptor.ProfessionLevel), CustomColors.Alerts.Declined, ChatMessageType.Error));
					}
					return true;
				}
			}
		}
		return false;
	}

	public static void SurCoupLocal(Resource ressource, Entity joueur)
	{
		if (!ressource.IsDead && !NiveauTropBas(ressource, joueur, avertir: false))
		{
			long milliseconds = Timing.Global.Milliseconds;
			Etat etat = EtatDe(ressource);
			etat.Predit = milliseconds;
			_ressourceDuJoueur = ressource.Id;
			bool flag = etat.DerniereProgression > 0 && ressource.Vital[0] <= etat.DerniereProgression;
			Geste(ressource, etat, joueur, precis: false, !flag, milliseconds);
		}
	}

	public static bool SurMessage(Resource ressource, string? message)
	{
		if (message != null && message.Length > 1)
		{
			if (message[0] == '-' && char.IsDigit(message[1]))
			{
				string s = new string(message.Skip(1).Where(char.IsDigit).ToArray());
				long.TryParse(s, out var result);
				EnAttente.Add(new Attente(ressource.Id, result, Timing.Global.Milliseconds));
				return true;
			}
		}
		EtatDe(ressource).Precis = Timing.Global.Milliseconds;
		return false;
	}

	public static void SurEpuisement(Resource ressource)
	{
		if (Etats.TryGetValue(ressource.Id, out Etat value))
		{
			long milliseconds = Timing.Global.Milliseconds;
			if (value.Fin < 0 && milliseconds - value.Geste < 1500)
			{
				Epuiser(ressource, value, TrouverRecolteur(ressource), milliseconds);
			}
		}
	}

	public static void SurRepousse(Resource ressource)
	{
		Etat etat = EtatDe(ressource);
		etat.Repousse = Timing.Global.Milliseconds;
		etat.Fin = -1L;
		etat.Prise = null;
		etat.Gestes = 0;
		etat.Barre = 0f;
		etat.Banc?.Repeupler();
		etat.Failles.Clear();
		etat.Impacts.Clear();
		etat.Ruisseaux.Clear();
		etat.Usure = null;
		etat.Frappe = null;
		etat.Morceaux = null;
		etat.Brise = -1L;
		etat.CederDepuis = -1L;
	}

	public static bool FaitFaceARessource(Entity entite)
	{
		int num = entite.X;
		int num2 = entite.Y;
		switch (entite.DirectionFacing)
		{
		case Direction.Up:
			num2--;
			break;
		case Direction.Down:
			num2++;
			break;
		case Direction.Left:
			num--;
			break;
		case Direction.Right:
			num++;
			break;
		default:
			return false;
		}
		foreach (Entity value in Globals.Entities.Values)
		{
			if (value is Resource { IsDead: false } resource && resource.MapId == entite.MapId && resource.X == num && resource.Y == num2)
			{
				return true;
			}
		}
		if (CoinDePecheDevant(entite) != null)
		{
			return !AttaquableDevant(entite);
		}
		return false;
	}

	private static void TraiterAttente(long maintenant)
	{
		for (int i = 0; i < EnAttente.Count; i++)
		{
			Attente attente = EnAttente[i];
			if (!Globals.Entities.TryGetValue(attente.RessourceId, out Entity value) || !(value is Resource resource))
			{
				EnAttente.RemoveAt(i--);
			}
			else
			{
				if (!resource.IsDead && maintenant - attente.Recu < 40)
				{
					continue;
				}
				EnAttente.RemoveAt(i--);
				Etat etat = EtatDe(resource);
				etat.DerniereProgression = attente.Valeur;
				bool flag = Metier(resource) == 3;
				bool flag2 = maintenant - etat.Precis < 400;
				Entity entity = TrouverRecolteur(resource);
				if (entity == null || entity != Globals.Me || maintenant - etat.Predit >= 450)
				{
					Geste(resource, etat, entity, flag2, !resource.IsDead, maintenant);
				}
				else
				{
					if (flag2)
					{
						Precision(resource, etat, entity, maintenant);
					}
					if (!resource.IsDead && etat.Son < etat.Predit && !flag && Metier(resource) != 1)
					{
						JouerSon(resource, fin: false);
						etat.Son = maintenant;
					}
				}
				if (resource.IsDead && etat.Fin < 0)
				{
					Epuiser(resource, etat, entity, maintenant);
				}
				if (!resource.IsDead && attente.Valeur > 0 && resource.Vital[0] <= attente.Valeur)
				{
					if (flag)
					{
						ArmerMorsure(resource, entity, maintenant);
					}
					else if (Metier(resource) == 1)
					{
						ArmerCeder(resource, etat, maintenant);
					}
				}
			}
		}
	}

	private static Entity? TrouverRecolteur(Resource ressource)
	{
		long milliseconds = Timing.Global.Milliseconds;
		Etat value;
		bool flag = Etats.TryGetValue(ressource.Id, out value) && milliseconds - value.Predit < 1800;
		Entity result = null;
		long num = long.MinValue;
		ResourceDescriptor? descriptor = ressource.Descriptor;
		bool flag2 = descriptor != null && descriptor.Tool == 3;
		foreach (Entity value2 in Globals.Entities.Values)
		{
			if (!(value2 is Player player) || player.MapId != ressource.MapId)
			{
				continue;
			}
			bool num2;
			if (!flag2)
			{
				if (Math.Abs(player.X - ressource.X) > 1)
				{
					continue;
				}
				num2 = Math.Abs(player.Y - ressource.Y) > 1;
			}
			else
			{
				num2 = !DansLeConeDeLancer(player.DirectionFacing, ressource.X - player.X, ressource.Y - player.Y);
			}
			if (!num2)
			{
				long num3 = player.AttackTimer;
				if (player == Globals.Me && flag)
				{
					num3 += 100000;
				}
				if (num3 > num)
				{
					num = num3;
					result = player;
				}
			}
		}
		return result;
	}

	private static void Geste(Resource ressource, Etat etat, Entity? recolteur, bool precis, bool jouerSon, long maintenant)
	{
		if (!MapInstance.TryGet(ressource.MapId, out MapInstance instance))
		{
			return;
		}
		etat.Geste = maintenant;
		etat.Gestes++;
		etat.Secousse = maintenant;
		if (!ressource.IsDead && ressource.Texture != null)
		{
			etat.Texture = ressource.Texture;
			etat.Source = ressource.RenderSource;
			etat.Destination = ressource.RenderDestination;
			etat.Abaissee = ressource.EstAbaissee;
		}
		if (recolteur == Globals.Me)
		{
			_ressourceDuJoueur = ressource.Id;
		}
		int num = Metier(ressource);
		(float X, float Y, int Sens) tuple = PointDImpact(ressource, recolteur, num);
		float item = tuple.X;
		float item2 = tuple.Y;
		int item3 = tuple.Sens;
		int num2 = instance.Y + (ressource.Y + 1) * HauteurCase;
		bool flag = false;
		switch (num)
		{
		case 0:
			LancerCopeaux(ressource.MapId, instance, item, item2, num2, item3, precis, maintenant);
			etat.Frisson = Vent.Temps;
			etat.SensFrisson = ((item3 != 0) ? (-item3) : ((Hasard.Next(2) != 0) ? 1 : (-1)));
			etat.ForceFrisson = (precis ? 1.35f : 1f);
			break;
		case 2:
			LancerFeuilles(ressource.MapId, instance, item, item2, num2, item3, precis ? 10 : 6, maintenant);
			break;
		case 1:
			FrapperRocher(ressource, etat, recolteur, precis, maintenant);
			flag = true;
			break;
		case 3:
			flag = GesteDePeche(ressource, recolteur, precis, maintenant);
			if (!flag)
			{
				Eclabousser(ressource.MapId, instance, item, item2, maintenant, etat.Gestes, precis ? 1.2f : 1f);
			}
			break;
		}
		if (flag)
		{
			etat.Son = maintenant;
			return;
		}
		if (jouerSon)
		{
			JouerSon(ressource, fin: false);
			etat.Son = maintenant;
		}
		if (recolteur != null)
		{
			Elancer(recolteur, ressource, instance, item, item2, item3, num, maintenant);
		}
	}

	private static void Precision(Resource ressource, Etat etat, Entity? recolteur, long maintenant)
	{
		if (!MapInstance.TryGet(ressource.MapId, out MapInstance instance))
		{
			return;
		}
		int num = Metier(ressource);
		(float X, float Y, int Sens) tuple = PointDImpact(ressource, recolteur, num);
		float item = tuple.X;
		float item2 = tuple.Y;
		int item3 = tuple.Sens;
		int num2 = instance.Y + (ressource.Y + 1) * HauteurCase;
		switch (num)
		{
		case 0:
			LancerCopeaux(ressource.MapId, instance, item, item2, num2, item3, fort: true, maintenant);
			etat.ForceFrisson = 1.35f;
			break;
		case 2:
			LancerFeuilles(ressource.MapId, instance, item, item2, num2, item3, 5, maintenant);
			break;
		case 1:
			PrecisSurRocher(ressource, etat, recolteur, maintenant);
			break;
		case 3:
			if (!PrecisDePeche(ressource, recolteur, maintenant))
			{
				Eclabousser(ressource.MapId, instance, item, item2, maintenant, etat.Gestes + 7, 0.8f);
			}
			break;
		}
	}

	private static void Epuiser(Resource ressource, Etat etat, Entity? recolteur, long maintenant)
	{
		if (!MapInstance.TryGet(ressource.MapId, out MapInstance instance))
		{
			return;
		}
		etat.Fin = maintenant;
		etat.Barre = 1f;
		int num = Metier(ressource);
		(float X, float Y, int Sens) tuple = PointDImpact(ressource, recolteur, num);
		float item = tuple.X;
		float item2 = tuple.Y;
		int item3 = tuple.Sens;
		int num2 = instance.Y + (ressource.Y + 1) * HauteurCase;
		switch (num)
		{
		case 0:
			LancerCopeaux(ressource.MapId, instance, item, item2, num2, item3, fort: true, maintenant);
			LancerCopeaux(ressource.MapId, instance, item, item2 - 6f, num2, -item3, fort: false, maintenant + 40);
			break;
		case 2:
			LancerFeuilles(ressource.MapId, instance, item, item2, num2, item3, 16, maintenant);
			break;
		case 1:
			BriserRocher(ressource, etat, recolteur, maintenant);
			etat.Son = maintenant;
			return;
		case 3:
			if (PriseDePeche(ressource, recolteur, maintenant))
			{
				etat.Son = maintenant;
				return;
			}
			Eclabousser(ressource.MapId, instance, item, item2, maintenant, etat.Gestes + 13, 1.35f);
			etat.Prise = new Prise(maintenant + 60, recolteur?.Id ?? Guid.Empty, item - (float)instance.X, item2 - (float)instance.Y);
			etat.Banc?.Disparaitre();
			break;
		}
		JouerSon(ressource, fin: true);
		etat.Son = maintenant;
	}

	private static (float X, float Y, int Sens) PointDImpact(Resource ressource, Entity? recolteur, int metier)
	{
		if (!MapInstance.TryGet(ressource.MapId, out MapInstance instance))
		{
			return (X: 0f, Y: 0f, Sens: 0);
		}
		float num = (float)(instance.X + ressource.X * TailleCase) + (float)TailleCase / 2f;
		int num2 = instance.Y + (ressource.Y + 1) * HauteurCase;
		int num3 = 0;
		int num4 = 0;
		if (recolteur != null && recolteur.Id != Guid.Empty)
		{
			FloatRect worldPos = recolteur.WorldPos;
			float num5 = ((worldPos.Width > 0f) ? (worldPos.X + worldPos.Width / 2f) : num);
			num3 = ((num5 < num - 6f) ? (-1) : ((num5 > num + 6f) ? 1 : 0));
			if (MapInstance.TryGet(recolteur.MapId, out MapInstance instance2))
			{
				int num6 = instance2.Y + (recolteur.Y + 1) * HauteurCase;
				num4 = ((num6 < num2 - 4) ? (-1) : ((num6 > num2 + 4) ? 1 : 0));
			}
		}
		if (metier == 3)
		{
			return (X: num + (float)(num3 * 8), Y: (float)num2 - (float)HauteurCase / 2f + (float)(num4 * 7) + 2f, Sens: num3);
		}
		int num7 = num2 - 15;
		if (num3 == 0)
		{
			return (X: num, Y: num7, Sens: 0);
		}
		return (X: BordOpaque(ressource, num7, num3) ?? (num + (float)(num3 * 8)), Y: num7, Sens: num3);
	}

	private static void Elancer(Entity recolteur, Resource ressource, MapInstance carte, float ix, float iy, int sens, int metier, long maintenant)
	{
		float num = 0f;
		float num2 = 0f;
		float num3;
		if (metier == 3)
		{
			num3 = 3f;
			num = Math.Sign(ressource.X - recolteur.X);
			num2 = Math.Sign(ressource.Y - recolteur.Y);
		}
		else if (sens != 0)
		{
			FloatRect worldPos = recolteur.WorldPos;
			float num4 = worldPos.X + worldPos.Width / 2f - (float)(sens * 13);
			num = -sens;
			num3 = Math.Clamp(MathF.Abs(ix - num4) - 1f, 0f, 10f);
		}
		else
		{
			num2 = Math.Sign(ressource.Y - recolteur.Y);
			num3 = 3f;
		}
		if (num3 > 0f && (num != 0f || num2 != 0f))
		{
			Elans[recolteur.Id] = new Elan(maintenant, num, num2, num3);
		}
	}

	public static FloatRect PoseElan(Entity entite, FloatRect rectangle)
	{
		rectangle = DecalageDePeche(entite, rectangle);
		rectangle = DecalageDeFrappe(entite, rectangle);
		if (Elans.Count == 0 || !Elans.TryGetValue(entite.Id, out var value))
		{
			return rectangle;
		}
		long num = Timing.Global.Milliseconds - value.Ne;
		if (num >= 280 || num < 0)
		{
			Elans.Remove(entite.Id);
			return rectangle;
		}
		float num2 = (new float[4] { 0f, 0.5f, 1f, 0.5f })[Math.Min(3, (int)(num / 70))];
		return new FloatRect(rectangle.X + MathF.Round(value.Dx * value.Distance * num2), rectangle.Y + MathF.Round(value.Dy * value.Distance * num2), rectangle.Width, rectangle.Height);
	}

	private static void JouerSon(Resource ressource, bool fin)
	{
		CombatAudio.Play(Metier(ressource) switch
		{
			1 => fin ? "recolte_roche_fin.wav" : SonsRoche[Hasard.Next(SonsRoche.Length)], 
			3 => fin ? "recolte_eau_prise.wav" : SonsEau[Hasard.Next(SonsEau.Length)], 
			_ => fin ? SonsBoisFin[Hasard.Next(SonsBoisFin.Length)] : SonsBois[Hasard.Next(SonsBois.Length)], 
		}, ressource, 0.06f);
	}

	private static void LancerCopeaux(Guid mapId, MapInstance carte, float ix, float iy, float pied, int sens, bool fort, long maintenant)
	{
		FormeBois[] array = ((!fort) ? new FormeBois[3]
		{
			FormeBois.Ecorce,
			FormeBois.Eclat,
			FormeBois.Copeau
		} : new FormeBois[5]
		{
			FormeBois.Ecorce,
			FormeBois.Eclat,
			FormeBois.Copeau,
			FormeBois.Ecorce,
			FormeBois.Eclat
		});
		int num = ((sens != 0) ? sens : ((Hasard.Next(2) != 0) ? 1 : (-1)));
		for (int i = 0; i < array.Length; i++)
		{
			int num2 = ((i % 3 == 2) ? (-num) : num);
			Eclats.Add(new Eclat
			{
				Genre = Genre.Bois,
				Forme = array[i],
				MapId = mapId,
				X = ix - (float)carte.X + (float)num2,
				Y = iy - (float)carte.Y + ((float)Hasard.NextDouble() - 0.5f) * 5f,
				Vx = (float)num2 * (0.6f + (float)Hasard.NextDouble() * 1.3f),
				Vy = -1.3f - (float)Hasard.NextDouble() * 1.5f,
				Angle = Hasard.Next(16),
				Tour = (float)num2 * (0.3f + (float)Hasard.NextDouble() * 0.4f),
				Sol = pied - (float)carte.Y + 4f - (float)Hasard.NextDouble() * 8f,
				Ne = maintenant + i * 15
			});
		}
		for (int j = 0; j < 5; j++)
		{
			Eclats.Add(new Eclat
			{
				Genre = Genre.Sciure,
				MapId = mapId,
				X = ix - (float)carte.X,
				Y = iy - (float)carte.Y + ((float)Hasard.NextDouble() - 0.5f) * 6f,
				Vx = (float)((Hasard.NextDouble() < 0.8) ? num : (-num)) * (0.3f + (float)Hasard.NextDouble() * 1.3f),
				Vy = -0.5f - (float)Hasard.NextDouble() * 0.9f,
				Couleur = ((Hasard.Next(2) == 0) ? PaletteBois[5] : PaletteBois[4]),
				Ne = maintenant
			});
		}
		Limiter();
	}

	private static void LancerFeuilles(Guid mapId, MapInstance carte, float ix, float iy, float pied, int sens, int nombre, long maintenant)
	{
		for (int i = 0; i < nombre; i++)
		{
			int num = ((Hasard.Next(3) == 0) ? (-sens) : sens);
			if (num == 0)
			{
				num = ((Hasard.Next(2) != 0) ? 1 : (-1));
			}
			Eclats.Add(new Eclat
			{
				Genre = Genre.Feuille,
				MapId = mapId,
				X = ix - (float)carte.X + ((float)Hasard.NextDouble() - 0.5f) * 10f,
				Y = iy - (float)carte.Y - (float)Hasard.NextDouble() * 10f,
				Vx = (float)num * (0.2f + (float)Hasard.NextDouble() * 0.9f),
				Vy = -0.6f - (float)Hasard.NextDouble() * 1f,
				Sol = pied - (float)carte.Y + 3f - (float)Hasard.NextDouble() * 8f,
				Couleur = CouleursFeuilles[Hasard.Next(CouleursFeuilles.Length)],
				Taille = ((Hasard.Next(3) != 0) ? 1 : 2),
				Ne = maintenant + i * 10
			});
		}
		Limiter();
	}

	private static void Limiter()
	{
		if (Eclats.Count > 400)
		{
			Eclats.RemoveRange(0, Eclats.Count - 400);
		}
	}

	private static void DessinerEclats(float k, long maintenant)
	{
		for (int num = Eclats.Count - 1; num >= 0; num--)
		{
			Eclat eclat = Eclats[num];
			if (maintenant >= eclat.Ne)
			{
				long num2 = maintenant - eclat.Ne;
				bool flag;
				switch (eclat.Genre)
				{
				case Genre.Sciure:
					flag = num2 > 480;
					break;
				case Genre.Goutte:
					flag = num2 > 900;
					break;
				case Genre.Bois:
				case Genre.Feuille:
					flag = eclat.Repos >= 0 && maintenant - eclat.Repos > 1300;
					break;
				default:
					flag = num2 > 1500;
					break;
				}
				if (flag || num2 > 5000 || !MapInstance.TryGet(eclat.MapId, out MapInstance instance))
				{
					Eclats.RemoveAt(num);
				}
				else
				{
					switch (eclat.Genre)
					{
					case Genre.Sciure:
						eclat.X += eclat.Vx * k;
						eclat.Y += eclat.Vy * k;
						eclat.Vy += 0.1f * k;
						eclat.Vx *= MathF.Pow(0.93f, k);
						Point((float)instance.X + eclat.X, (float)instance.Y + eclat.Y, Fondu(eclat.Couleur, 1f - (float)num2 / 480f));
						break;
					case Genre.Goutte:
						eclat.X += eclat.Vx * k;
						eclat.Y += eclat.Vy * k;
						eclat.Vy += 0.16f * k;
						if (eclat.Y >= eclat.Sol && eclat.Vy > 0f)
						{
							Eclats.RemoveAt(num);
						}
						else
						{
							Rect(MathF.Floor((float)instance.X + eclat.X), MathF.Floor((float)instance.Y + eclat.Y), eclat.Taille, eclat.Taille, eclat.Couleur);
						}
						break;
					default:
					{
						if (eclat.Repos < 0)
						{
							float num3 = ((eclat.Genre == Genre.Feuille) ? 0.05f : 0.16f);
							eclat.X += eclat.Vx * k;
							eclat.Y += eclat.Vy * k;
							eclat.Vy += num3 * k;
							eclat.Angle += eclat.Tour * k;
							if (eclat.Genre == Genre.Feuille)
							{
								eclat.Vy = MathF.Min(eclat.Vy, 0.7f);
								eclat.X += MathF.Sin((float)num2 / 120f + eclat.Sol) * 0.25f * k;
							}
							if (eclat.Y >= eclat.Sol && eclat.Vy > 0f)
							{
								eclat.Y = eclat.Sol;
								if (eclat.Rebonds == 0 && eclat.Vy > 1.2f && eclat.Genre != Genre.Feuille)
								{
									eclat.Vy = (0f - eclat.Vy) * 0.32f;
									eclat.Vx *= 0.4f;
									eclat.Tour *= 0.5f;
									eclat.Rebonds = 1;
								}
								else
								{
									eclat.Repos = maintenant;
									eclat.Angle = MathF.Round(eclat.Angle / 8f) * 8f + (float)Hasard.Next(2);
								}
							}
						}
						float num4 = ((eclat.Repos < 0) ? 1f : Borne((float)(1300 - (maintenant - eclat.Repos)) / 400f));
						float num5 = (float)instance.X + eclat.X;
						float num6 = (float)instance.Y + eclat.Y;
						float num7 = MathF.Max(0f, eclat.Sol - eclat.Y);
						switch (eclat.Genre)
						{
						case Genre.Bois:
						{
							Rect(MathF.Round(num5) - 2f, MathF.Round((float)instance.Y + eclat.Sol) + 3f, 5f, 1f, Fondu(OmbreEclat, num4 * 0.3f * (1f - MathF.Min(1f, num7 / 36f))));
							byte[][,] array = ImagesBois[(int)eclat.Forme];
							int num8 = ((int)MathF.Round(eclat.Angle) % 16 + 16) % 16;
							DessinerMatrice(array[num8], PaletteBois, num5, num6, num4 * 0.88f);
							break;
						}
						case Genre.Feuille:
							Rect(MathF.Round(num5), MathF.Round(num6), eclat.Taille, 1f, Fondu(eclat.Couleur, num4));
							if (eclat.Taille > 1)
							{
								Point(num5 + 1f, num6 - 1f, Fondu(eclat.Couleur, num4 * 0.8f));
							}
							break;
						}
						break;
					}
					}
				}
			}
		}
	}

	private static void DessinerOndes(bool poussiere, long maintenant)
	{
		for (int num = Ondes.Count - 1; num >= 0; num--)
		{
			Onde onde = Ondes[num];
			long num2 = maintenant - onde.Ne;
			MapInstance instance;
			if (num2 > onde.Duree)
			{
				Ondes.RemoveAt(num);
			}
			else if (onde.Poussiere == poussiere && num2 >= 0 && MapInstance.TryGet(onde.MapId, out instance))
			{
				float num3 = (float)num2 / (float)onde.Duree;
				if (onde.Poussiere)
				{
					bool flag = onde.Ampleur > 1f;
					float rx = (flag ? (18f + 20f * num3) : (12f + 10f * num3));
					float ry = (flag ? (5f + 5f * num3) : (3f + 2f * num3));
					RemplirEllipse((float)instance.X + onde.X, (float)instance.Y + onde.Y, rx, ry, Fondu(CouleurPoussiere, (flag ? 0.5f : 0.3f) * (1f - num3)));
				}
				else
				{
					TracerEllipse((float)instance.X + onde.X, (float)instance.Y + onde.Y, (4f + 13f * num3) * onde.Ampleur, (1.5f + 4.5f * num3) * onde.Ampleur, Fondu(CouleurRond, 0.6f * (1f - num3)));
				}
			}
		}
	}

	private static void DessinerBarre(float dt, long maintenant)
	{
		if (_ressourceDuJoueur == Guid.Empty)
		{
			return;
		}
		Player me = Globals.Me;
		if (me == null || !Etats.TryGetValue(_ressourceDuJoueur, out Etat value))
		{
			return;
		}
		if (!Globals.Entities.TryGetValue(_ressourceDuJoueur, out Entity value2) || !(value2 is Resource resource))
		{
			_ressourceDuJoueur = Guid.Empty;
			return;
		}
		float num = 1f;
		if (value.Fin >= 0)
		{
			long num2 = maintenant - value.Fin;
			if (num2 > 450)
			{
				_ressourceDuJoueur = Guid.Empty;
				return;
			}
			num = ((num2 < 200) ? 1f : (1f - (float)(num2 - 200) / 250f));
		}
		else if (maintenant - Math.Max(value.Geste, value.Predit) > 2500)
		{
			return;
		}
		long num3 = resource.MaxVital[0];
		float num4 = ((value.Fin >= 0 || resource.IsDead || num3 <= 0) ? 1f : Borne(1f - (float)resource.Vital[0] / (float)num3));
		value.Barre += (num4 - value.Barre) * MathF.Min(1f, dt / 80f);
		FloatRect bodyWorldPos = me.BodyWorldPos;
		float num5 = MathF.Round(bodyWorldPos.X + bodyWorldPos.Width / 2f - 20f);
		float num6 = MathF.Round(bodyWorldPos.Y - 12f);
		Rect(num5 - 1f, num6 - 1f, 42f, 8f, Fondu(BarreContour, num));
		Rect(num5, num6, 40f, 6f, Fondu(BarreFond, num));
		float num7 = MathF.Round(40f * Borne(value.Barre));
		float num8 = MathF.Round(40f * num4);
		long num9 = maintenant - Math.Max(value.Geste, value.Predit);
		if (num8 > num7 && num9 < 500)
		{
			Rect(num5 + num7, num6, num8 - num7, 6f, Fondu((num9 < 90) ? Color.White : BarreGain, num));
		}
		Rect(num5, num6, num7, 6f, Fondu(BarrePleine, num));
		Rect(num5, num6, num7, 2f, Fondu(BarreReflet, num));
		if (num7 > 0f && num7 < 40f)
		{
			Rect(num5 + num7 - 1f, num6, 1f, 6f, Fondu(BarreBord, num));
		}
		long num10 = maintenant - value.Fin;
		if (value.Fin >= 0 && num10 < 120)
		{
			Rect(num5 - 1f, num6 - 1f, 42f, 8f, Fondu(Color.White, num * (1f - (float)num10 / 120f)));
		}
	}

	public static void DessinerSol()
	{
		if (_pixel == null)
		{
			_pixel = Graphics.Renderer?.WhitePixel;
		}
		if (_pixel != null)
		{
			long milliseconds = Timing.Global.Milliseconds;
			float num = ((_derniereImageSol == 0L) ? 16.7f : ((float)Math.Clamp(milliseconds - _derniereImageSol, 0L, 50L)));
			_derniereImageSol = milliseconds;
			MettreAJourGouttes(num / 1000f, milliseconds);
			DessinerOmbresDePoissons(milliseconds, num);
			DessinerOndes(poussiere: false, milliseconds);
			DessinerSurfaceDeLEau(milliseconds);
			DessinerPecheAuSol(milliseconds);
			DessinerPoussieresDeRoche(derriere: true, milliseconds);
		}
	}

	public static void Dessiner()
	{
		if (_pixel == null)
		{
			_pixel = Graphics.Renderer?.WhitePixel;
		}
		if (_pixel == null)
		{
			return;
		}
		long milliseconds = Timing.Global.Milliseconds;
		float num = ((_derniereImage == 0L) ? 16.7f : ((float)Math.Clamp(milliseconds - _derniereImage, 0L, 50L)));
		_derniereImage = milliseconds;
		TraiterAttente(milliseconds);
		MettreAJourMinage(milliseconds, num / 1000f);
		DessinerOndes(poussiere: true, milliseconds);
		DessinerMinage(milliseconds);
		DessinerGerbes(milliseconds);
		DessinerGouttesEnLAir(milliseconds);
		DessinerPoissonsPris();
		DessinerSautsEtPrises(milliseconds);
		DessinerEclats(num / 16.7f, milliseconds);
		DessinerBarre(num, milliseconds);
		NettoyerPecheurs(milliseconds);
		if (milliseconds > _prochainMenage)
		{
			_prochainMenage = milliseconds + 5000;
			Guid[] array = Etats.Keys.Where((Guid id) => !Globals.Entities.ContainsKey(id)).ToArray();
			foreach (Guid key in array)
			{
				Etats.Remove(key);
			}
		}
	}

	private static Color Hex(int rgb, int alpha = 255)
	{
		return new Color(alpha, (rgb >> 16) & 0xFF, (rgb >> 8) & 0xFF, rgb & 0xFF);
	}

	private static Color Fondu(Color couleur, float part)
	{
		return new Color((int)((float)(int)couleur.A * Math.Clamp(part, 0f, 1f)), couleur.R, couleur.G, couleur.B);
	}

	private static float Borne(float valeur)
	{
		return Math.Clamp(valeur, 0f, 1f);
	}

	private static void Rect(float x, float y, float largeur, float hauteur, Color couleur)
	{
		if (_pixel != null && couleur.A != 0 && !(largeur <= 0f) && !(hauteur <= 0f))
		{
			Graphics.DrawGameTexture(_pixel, new FloatRect(0f, 0f, 1f, 1f), new FloatRect(x, y, largeur, hauteur), couleur);
		}
	}

	private static void Point(float x, float y, Color couleur)
	{
		Rect(MathF.Floor(x), MathF.Floor(y), 1f, 1f, couleur);
	}

	private static Func<float> Alea(int graine)
	{
		return delegate
		{
			graine += 1831565813;
			uint num = (uint)graine;
			num = (num ^ (num >> 15)) * (1 | num);
			num = (num + (num ^ (num >> 7)) * (0x3D | num)) ^ num;
			return (float)((num ^ (num >> 14)) & 0xFFFFFFFFu) / 4.2949673E+09f;
		};
	}

	private static void RemplirPolygone(IReadOnlyList<(float X, float Y)> points, Color couleur)
	{
		if (points.Count < 3)
		{
			return;
		}
		float x = points.Min(((float X, float Y) p) => p.Y);
		float x2 = points.Max(((float X, float Y) p) => p.Y);
		List<float> list = new List<float>(8);
		for (int num = (int)MathF.Floor(x); num <= (int)MathF.Ceiling(x2); num++)
		{
			float num2 = (float)num + 0.5f;
			list.Clear();
			for (int num3 = 0; num3 < points.Count; num3++)
			{
				(float, float) tuple = points[num3];
				(float, float) tuple2 = points[(num3 + 1) % points.Count];
				if ((tuple.Item2 <= num2 && tuple2.Item2 > num2) || (tuple2.Item2 <= num2 && tuple.Item2 > num2))
				{
					list.Add(tuple.Item1 + (num2 - tuple.Item2) / (tuple2.Item2 - tuple.Item2) * (tuple2.Item1 - tuple.Item1));
				}
			}
			list.Sort();
			for (int num4 = 0; num4 + 1 < list.Count; num4 += 2)
			{
				int num5 = (int)MathF.Round(list[num4]);
				int num6 = (int)MathF.Round(list[num4 + 1]);
				Rect(num5, num, num6 - num5, 1f, couleur);
			}
		}
	}

	private static IEnumerable<(int Debut, int Fin)> Etendues(IReadOnlyList<(float X, float Y)> points, int y)
	{
		float num = (float)y + 0.5f;
		List<float> croisements = new List<float>(8);
		for (int i = 0; i < points.Count; i++)
		{
			(float, float) tuple = points[i];
			(float, float) tuple2 = points[(i + 1) % points.Count];
			if ((tuple.Item2 <= num && tuple2.Item2 > num) || (tuple2.Item2 <= num && tuple.Item2 > num))
			{
				croisements.Add(tuple.Item1 + (num - tuple.Item2) / (tuple2.Item2 - tuple.Item2) * (tuple2.Item1 - tuple.Item1));
			}
		}
		croisements.Sort();
		for (int j = 0; j + 1 < croisements.Count; j += 2)
		{
			yield return (Debut: (int)MathF.Round(croisements[j]), Fin: (int)MathF.Round(croisements[j + 1]));
		}
	}

	private static bool DansPolygone(IReadOnlyList<(float X, float Y)> points, float x, float y)
	{
		bool flag = false;
		int num = 0;
		int index = points.Count - 1;
		while (num < points.Count)
		{
			if (points[num].Y > y != points[index].Y > y && x < (points[index].X - points[num].X) * (y - points[num].Y) / (points[index].Y - points[num].Y) + points[num].X)
			{
				flag = !flag;
			}
			index = num++;
		}
		return flag;
	}

	private static void RemplirEllipse(float cx, float cy, float rx, float ry, Color couleur)
	{
		if (rx <= 0f || ry <= 0f)
		{
			return;
		}
		for (int i = (int)MathF.Floor(cy - ry); i <= (int)MathF.Ceiling(cy + ry); i++)
		{
			float num = ((float)i + 0.5f - cy) / ry;
			if (!(num * num > 1f))
			{
				float num2 = rx * MathF.Sqrt(1f - num * num);
				int num3 = (int)MathF.Round(cx - num2);
				int num4 = (int)MathF.Round(cx + num2);
				Rect(num3, i, num4 - num3, 1f, couleur);
			}
		}
	}

	private static void TracerEllipse(float cx, float cy, float rx, float ry, Color couleur)
	{
		HashSet<(int, int)> hashSet = new HashSet<(int, int)>();
		int num = Math.Max(12, (int)((float)Math.PI * (rx + ry) * 1.5f));
		for (int i = 0; i < num; i++)
		{
			float x = (float)i / (float)num * ((float)Math.PI * 2f);
			int num2 = (int)MathF.Floor(cx + MathF.Cos(x) * rx);
			int num3 = (int)MathF.Floor(cy + MathF.Sin(x) * ry);
			if (hashSet.Add((num2, num3)))
			{
				Rect(num2, num3, 1f, 1f, couleur);
			}
		}
	}

	private static IEnumerable<(int X, int Y)> Segment(int x0, int y0, int x1, int y1)
	{
		int dx = Math.Abs(x1 - x0);
		int sx = ((x0 < x1) ? 1 : (-1));
		int dy = -Math.Abs(y1 - y0);
		int sy = ((y0 < y1) ? 1 : (-1));
		int erreur = dx + dy;
		while (true)
		{
			yield return (X: x0, Y: y0);
			if (x0 == x1 && y0 == y1)
			{
				break;
			}
			int num = 2 * erreur;
			if (num >= dy)
			{
				erreur += dy;
				x0 += sx;
			}
			if (num <= dx)
			{
				erreur += dx;
				y0 += sy;
			}
		}
	}

	private static byte FormeDuBois(FormeBois forme, float u, float v)
	{
		switch (forme)
		{
		case FormeBois.Copeau:
		{
			if (MathF.Abs(v) > 1.3f || u < -3.5f + ((v > 0f) ? 1.2f : 0f) || u > 3.5f - ((v < 0f) ? 1.2f : 0f))
			{
				return 0;
			}
			float num7 = (v + 1.3f) / 2.6f;
			if (num7 < 0.34f)
			{
				if (!(MathF.Abs(u + 0.5f) < 1.4f))
				{
					return 6;
				}
				return 7;
			}
			if (num7 < 0.67f)
			{
				if ((int)MathF.Floor(u + 20f) % 3 != 0)
				{
					return 5;
				}
				return 6;
			}
			return 3;
		}
		case FormeBois.Eclat:
		{
			float num3 = (u + 2.5f) / 5f;
			if (num3 < 0f || num3 > 1f)
			{
				return 0;
			}
			float num4 = -1.4f;
			float num5 = 1.4f - 1.9599999f * num3;
			if (v < num4 || v > num5)
			{
				return 0;
			}
			float num6 = (v - num4) / MathF.Max(0.5f, num5 - num4);
			if (!(num6 < 0.45f))
			{
				return 5;
			}
			if (!(num3 < 0.5f))
			{
				return 6;
			}
			return 7;
		}
		default:
		{
			float num = v + 0.9f * (u * u) / 7.8399997f - 0.45f;
			if (MathF.Abs(u) > 2.8f || MathF.Abs(num) > 1.25f || (MathF.Abs(u) > 1.8f && MathF.Abs(num) > 0.55f))
			{
				return 0;
			}
			float num2 = (num + 1.25f) / 2.5f;
			if (num2 < 0.3f)
			{
				return 4;
			}
			if ((int)MathF.Floor(u + 20f) % 2 != 0)
			{
				return 2;
			}
			return 3;
		}
		}
	}

	private static byte[][][,] FabriquerCopeaux()
	{
		float[] array = new float[3] { 7f, 5f, 6f };
		byte[][][,] array2 = new byte[3][][,];
		for (int i = 0; i < 3; i++)
		{
			int num = (int)MathF.Ceiling(array[i] / 2f) + 2;
			int num2 = num * 2 + 1;
			array2[i] = new byte[16][,];
			for (int j = 0; j < 16; j++)
			{
				float x = (float)j / 16f * ((float)Math.PI * 2f);
				float num3 = MathF.Cos(x);
				float num4 = MathF.Sin(x);
				byte[,] array3 = new byte[num2, num2];
				for (int k = 0; k < num2; k++)
				{
					for (int l = 0; l < num2; l++)
					{
						float u = (float)(k - num) * num3 + (float)(l - num) * num4;
						float v = (float)(-(k - num)) * num4 + (float)(l - num) * num3;
						array3[k, l] = FormeDuBois((FormeBois)i, u, v);
					}
				}
				List<(int, int)> list = new List<(int, int)>();
				for (int m = 0; m < num2; m++)
				{
					for (int n = 0; n < num2; n++)
					{
						if (array3[m, n] == 0 && ((m > 0 && array3[m - 1, n] > 1) || (m < num2 - 1 && array3[m + 1, n] > 1) || (n > 0 && array3[m, n - 1] > 1) || (n < num2 - 1 && array3[m, n + 1] > 1)))
						{
							list.Add((m, n));
						}
					}
				}
				foreach (var (num5, num6) in list)
				{
					array3[num5, num6] = 1;
				}
				array2[i][j] = array3;
			}
		}
		return array2;
	}

	private static void DessinerMatrice(byte[,] matrice, Color[] palette, float centreX, float centreY, float alpha)
	{
		int length = matrice.GetLength(0);
		float num = MathF.Round(centreX - (float)length / 2f);
		float num2 = MathF.Round(centreY - (float)matrice.GetLength(1) / 2f);
		for (int i = 0; i < length; i++)
		{
			for (int j = 0; j < matrice.GetLength(1); j++)
			{
				byte b = matrice[i, j];
				if (b != 0)
				{
					Rect(num + (float)i, num2 + (float)j, 1f, 1f, (alpha >= 1f) ? palette[b - 1] : Fondu(palette[b - 1], alpha));
				}
			}
		}
	}

	private static byte[,] FabriquerPoisson(int decalage)
	{
		byte[,] array = new byte[19, 11];
		(int, int, int)[] array2 = new(int, int, int)[7]
		{
			(4, 4, 6),
			(3, 3, 7),
			(2, 2, 8),
			(1, 1, 4),
			(1, 6, 9),
			(0, 1, 2),
			(0, 8, 9)
		};
		(int, int, int)[] array3 = array2;
		for (int i = 0; i < array3.Length; i++)
		{
			(int, int, int) tuple = array3[i];
			int item = tuple.Item1;
			int item2 = tuple.Item2;
			int item3 = tuple.Item3;
			int num = item2 + decalage;
			int num2 = item3 + decalage;
			for (int j = num; j <= num2; j++)
			{
				if (j >= 0 && j < 11)
				{
					array[item, j] = (byte)((j == num || j == num2 || item == 0) ? 1 : ((item == 2 && Math.Abs(j - (5 + decalage)) <= 1) ? 3 : 2));
				}
			}
		}
		Dictionary<int, (int, int)> dictionary = new Dictionary<int, (int, int)>
		{
			[5] = (4, 6),
			[6] = (3, 7),
			[7] = (3, 7),
			[8] = (2, 8),
			[9] = (2, 8),
			[10] = (2, 8),
			[11] = (2, 8),
			[12] = (2, 8),
			[13] = (3, 7),
			[14] = (3, 7),
			[15] = (3, 7),
			[16] = (4, 6),
			[17] = (4, 6)
		};
		foreach (KeyValuePair<int, (int, int)> item6 in dictionary)
		{
			item6.Deconstruct(out var key, out var value);
			(int, int) tuple2 = value;
			int num3 = key;
			int item4 = tuple2.Item1;
			int item5 = tuple2.Item2;
			for (int k = item4; k <= item5; k++)
			{
				float num4 = (float)(k - item4) / (float)(item5 - item4);
				array[num3, k] = (byte)((k == item4 || k == item5) ? 1 : ((num4 < 0.34f) ? 4 : ((num4 < 0.56f) ? 5 : ((num4 < 0.78f) ? 6 : 7))));
			}
		}
		array[17, 4] = 1;
		array[17, 5] = 1;
		array[17, 6] = 1;
		array[15, 4] = 8;
		array[16, 4] = 1;
		array[14, 5] = 1;
		(int, int)[] array4 = new(int, int)[6]
		{
			(8, 1),
			(9, 1),
			(10, 1),
			(11, 1),
			(12, 1),
			(7, 2)
		};
		for (int l = 0; l < array4.Length; l++)
		{
			var (num5, num6) = array4[l];
			array[num5, num6] = 1;
		}
		array[9, 2] = 2;
		array[10, 2] = 3;
		array[11, 2] = 2;
		array[9, 9] = 1;
		array[10, 9] = 2;
		array[11, 9] = 1;
		array[10, 10] = 1;
		return array;
	}

	private static void DessinerPoisson(float x, float y, float angle, bool gauche, long maintenant, float alpha = 1f)
	{
		DessinerPoissonImage(x, y, angle, gauche, (new int[4] { 0, 1, 0, 2 })[(int)(maintenant / 70 % 4)], alpha);
	}

	private static void DessinerPoissonImage(float x, float y, float angle, bool gauche, int numero, float alpha = 1f)
	{
		byte[,] array = ImagesPoisson[Math.Clamp(numero, 0, ImagesPoisson.Length - 1)];
		float num = MathF.Cos(angle);
		float num2 = MathF.Sin(angle);
		float num3 = Arrondi(x);
		float num4 = Arrondi(y);
		for (int i = -12; i <= 12; i++)
		{
			for (int j = -12; j <= 12; j++)
			{
				float num5 = (float)j + 0.5f;
				float num6 = (float)i + 0.5f;
				if (gauche)
				{
					num5 = 0f - num5;
				}
				float num7 = num * num5 + num2 * num6;
				float num8 = (0f - num2) * num5 + num * num6;
				int num9 = (int)MathF.Floor(num7 + 10f);
				int num10 = (int)MathF.Floor(num8 + 6f);
				if (num9 >= 0 && num10 >= 0 && num9 < 19 && num10 < 11 && array[num9, num10] != 0)
				{
					Rect(num3 + (float)j, num4 + (float)i, 1f, 1f, Fondu(PalettePoisson[array[num9, num10] - 1], alpha));
				}
			}
		}
	}

	private static void AjouterRond(Guid mapId, float x, float y, long ne, float rayon = 13f, long duree = 620L, float alpha = 0.7f, Color? couleur = null, bool epais = false)
	{
		RondsDEau.Add(new RondDEau(mapId, x, y, ne, rayon, duree, couleur ?? Color.White, alpha, Hasard.Next(1000000), epais));
		if (RondsDEau.Count > 60)
		{
			RondsDEau.RemoveAt(0);
		}
	}

	private static void AjouterGerbe(Guid mapId, float x, float y, long ne, float[] style, float ampleur, int? graine = null)
	{
		Gerbes.Add(new Gerbe(mapId, x, y, ne, graine ?? Hasard.Next(1000), ampleur, style));
		if (Gerbes.Count > 24)
		{
			Gerbes.RemoveAt(0);
		}
	}

	private static void AjouterGoutteDEau(Guid mapId, float x, float y, float h, float vx, float vy, float vh, long ne, int taille, float coinX, float coinY)
	{
		GouttesDEau.Add(new GoutteDEau
		{
			MapId = mapId,
			X = x,
			Y = y,
			H = h,
			Vx = vx,
			Vy = vy,
			Vh = vh,
			Ne = ne,
			Taille = taille,
			Claire = (Hasard.Next(2) == 0),
			CoinX = coinX,
			CoinY = coinY
		});
		if (GouttesDEau.Count > 160)
		{
			GouttesDEau.RemoveAt(0);
		}
	}

	private static void ProjeterGouttes(Guid mapId, float x, float y, long ne, int nombre, float force, float coinX, float coinY)
	{
		for (int i = 0; i < nombre; i++)
		{
			float x2 = (float)Hasard.NextDouble() * ((float)Math.PI * 2f);
			float num = (18f + (float)Hasard.NextDouble() * 34f) * force;
			AjouterGoutteDEau(mapId, x + MathF.Cos(x2) * 2f, y + MathF.Sin(x2), 3f + (float)Hasard.NextDouble() * 4f, MathF.Cos(x2) * num, MathF.Sin(x2) * num * 0.4f, (70f + (float)Hasard.NextDouble() * 70f) * force, ne + Hasard.Next(40), (!(Hasard.NextDouble() < 0.35)) ? 1 : 2, coinX, coinY);
		}
	}

	private static bool TombeDansLEau(GoutteDEau goutte)
	{
		if (!float.IsNaN(goutte.CoinX))
		{
			float num = goutte.X - goutte.CoinX;
			float num2 = goutte.Y - goutte.CoinY;
			if (num * num + num2 * num2 < (float)(TailleCase * TailleCase) * 2.25f)
			{
				return true;
			}
		}
		if (MapInstance.TryGet(goutte.MapId, out MapInstance instance))
		{
			MapAttribute[,] attributes = instance.Attributes;
			if (attributes != null)
			{
				int num3 = (int)MathF.Floor(goutte.X / (float)TailleCase);
				int num4 = (int)MathF.Floor(goutte.Y / (float)HauteurCase);
				if (num3 >= 0 && num4 >= 0 && num3 < attributes.GetLength(0) && num4 < attributes.GetLength(1))
				{
					MapAttribute mapAttribute = attributes[num3, num4];
					if (mapAttribute == null)
					{
						return false;
					}
					return mapAttribute.Type == MapAttributeType.Blocked;
				}
				return false;
			}
		}
		return false;
	}

	private static void MettreAJourGouttes(float dt, long maintenant)
	{
		for (int num = GouttesDEau.Count - 1; num >= 0; num--)
		{
			GoutteDEau goutteDEau = GouttesDEau[num];
			if (maintenant >= goutteDEau.Ne)
			{
				goutteDEau.X += goutteDEau.Vx * dt;
				goutteDEau.Y += goutteDEau.Vy * dt;
				goutteDEau.H += goutteDEau.Vh * dt;
				goutteDEau.Vh -= 420f * dt;
				if (!(goutteDEau.H > 0f) || maintenant - goutteDEau.Ne >= 3000)
				{
					GouttesDEau.RemoveAt(num);
					Traces.Add(new TraceDeGoutte(goutteDEau.MapId, goutteDEau.X, goutteDEau.Y, maintenant, TombeDansLEau(goutteDEau)));
					if (Traces.Count > 80)
					{
						Traces.RemoveAt(0);
					}
				}
			}
		}
	}

	private static void DessinerSurfaceDeLEau(long maintenant)
	{
		for (int num = RondsDEau.Count - 1; num >= 0; num--)
		{
			RondDEau rond = RondsDEau[num];
			long num2 = maintenant - rond.Ne;
			MapInstance instance;
			if (num2 > rond.Duree)
			{
				RondsDEau.RemoveAt(num);
			}
			else if (num2 >= 0 && MapInstance.TryGet(rond.MapId, out instance))
			{
				float k = (float)num2 / (float)rond.Duree;
				float num3 = rond.Rayon * Sortie(k);
				float alpha = rond.Alpha * MathF.Pow(1f - k, 1.4f);
				TracerRond((float)instance.X + rond.X, (float)instance.Y + rond.Y, MathF.Max(1f, num3), MathF.Max(1f, num3 * 0.36f), rond.Couleur, alpha, delegate(float angle)
				{
					float num11 = ((MathF.Sin(angle) > 0f) ? 1f : 0.45f);
					float num12 = ((Bruit((int)MathF.Floor(angle / ((float)Math.PI * 2f) * 14f + k * 3f), rond.Graine) < 0.18f * (0.4f + k)) ? 0f : 1f);
					return num11 * num12;
				});
				if (rond.Epais && num3 > 2f)
				{
					TracerRond((float)instance.X + rond.X, (float)instance.Y + rond.Y + 1f, MathF.Max(1f, num3 - 1f), MathF.Max(1f, (num3 - 1f) * 0.36f), rond.Couleur, rond.Alpha * 0.35f * (1f - k) * (1f - k), (float angle) => (!(MathF.Sin(angle) > 0f)) ? 0f : 1f);
				}
			}
		}
		for (int num4 = Traces.Count - 1; num4 >= 0; num4--)
		{
			TraceDeGoutte traceDeGoutte = Traces[num4];
			long num5 = maintenant - traceDeGoutte.Ne;
			MapInstance instance2;
			if (num5 > (traceDeGoutte.Eau ? 200 : 420))
			{
				Traces.RemoveAt(num4);
			}
			else if (MapInstance.TryGet(traceDeGoutte.MapId, out instance2))
			{
				float num6 = Arrondi((float)instance2.X + traceDeGoutte.X);
				float num7 = Arrondi((float)instance2.Y + traceDeGoutte.Y);
				if (traceDeGoutte.Eau)
				{
					(int, int)[] array = MicroRond[Math.Min(3, (int)(num5 / 50))];
					for (int num8 = 0; num8 < array.Length; num8++)
					{
						var (num9, num10) = array[num8];
						Rect(num6 + (float)num9, num7 + (float)num10, 1f, 1f, Fondu(Color.White, 0.75f));
					}
				}
				else
				{
					Rect(num6, num7, 1f, 1f, Fondu(CouleurTacheHumide, 0.35f * (1f - (float)num5 / 420f)));
				}
			}
		}
	}

	private static void DessinerGouttesEnLAir(long maintenant)
	{
		foreach (GoutteDEau item in GouttesDEau)
		{
			if (maintenant >= item.Ne && MapInstance.TryGet(item.MapId, out MapInstance instance))
			{
				if (item.H > 3f)
				{
					Rect(Arrondi((float)instance.X + item.X), Arrondi((float)instance.Y + item.Y), 1f, 1f, Fondu(OmbreDeGoutte, 0.14f));
				}
				Rect(Arrondi((float)instance.X + item.X), Arrondi((float)instance.Y + item.Y - item.H), item.Taille, item.Taille, Fondu(item.Claire ? Color.White : GoutteClaire, 0.95f));
			}
		}
	}

	private static void TracerRond(float cx, float cy, float rx, float ry, Color couleur, float alpha, Func<float, float> alphaDe)
	{
		int num = Math.Max(12, (int)MathF.Ceiling((float)Math.PI * 2f * MathF.Max(rx, ry) * 1.4f));
		HashSet<(int, int)> hashSet = new HashSet<(int, int)>();
		for (int i = 0; i < num; i++)
		{
			float num2 = (float)i / (float)num * ((float)Math.PI * 2f);
			int num3 = (int)Arrondi(cx + MathF.Cos(num2) * rx);
			int num4 = (int)Arrondi(cy + MathF.Sin(num2) * ry);
			if (hashSet.Add((num3, num4)))
			{
				float num5 = alphaDe(num2);
				if (num5 > 0f)
				{
					Rect(num3, num4, 1f, 1f, Fondu(couleur, alpha * num5));
				}
			}
		}
	}

	private static bool DessinerFrisson(Etat etat, IGameTexture texture, FloatRect source, FloatRect destination, float repousse)
	{
		double age = Vent.Temps - etat.Frisson;
		if (age < 0.0 || age > 0.55)
		{
			return false;
		}
		(IGameTexture, int, int, int, int) key = (texture, (int)source.X, (int)source.Y, (int)source.Width, (int)source.Height);
		if (!ArbresRecolte.TryGetValue(key, out ModeleArbre modele))
		{
			modele = ModeleArbre.ConstruireZone(texture.Name ?? "recolte", texture, key.Item2, key.Item3, key.Item4, key.Item5);
			ArbresRecolte[key] = modele;
		}
		if (modele == null)
		{
			return false;
		}
		float enveloppe = (float)Math.Pow(1.0 - age / 0.55, 2.0);
		Color teinte = ((repousse >= 1f) ? Color.White : Fondu(Color.White, repousse));
		Calque(3, 0f);
		Calque(4, 0f);
		Calque(5, 0f);
		Calque(2, Decalage(0.8f, 0.0));
		Calque(1, Decalage(1.3f, 0.025));
		Calque(0, Decalage(1.8f, 0.05));
		return true;
		void Calque(int calque, float decalage)
		{
			FloatRect srcRectangle = new FloatRect(calque * modele.Largeur, 0f, modele.Largeur, modele.Hauteur);
			if (decalage == 0f)
			{
				Graphics.DrawGameTexture(modele.Atlas, srcRectangle, destination, teinte);
			}
			else
			{
				Vent.Glisse(decalage, out var gauche, out var droite, out var part);
				float num4;
				float num5;
				float num6;
				if (!(part <= 0.5f))
				{
					float num = droite;
					float num2 = gauche;
					float num3 = 1f - part;
					num4 = num3;
					num5 = num2;
					num6 = num;
				}
				else
				{
					float num7 = gauche;
					float num3 = droite;
					float num2 = part;
					num4 = num2;
					num5 = num3;
					num6 = num7;
				}
				Graphics.DrawGameTexture(modele.Atlas, srcRectangle, new FloatRect(destination.X + num6, destination.Y, destination.Width, destination.Height), teinte);
				if (num4 >= 1f / 64f)
				{
					Graphics.DrawGameTexture(modele.Atlas, srcRectangle, new FloatRect(destination.X + num5, destination.Y, destination.Width, destination.Height), Fondu(teinte, num4));
				}
			}
		}
		float Decalage(float ampleur, double retard)
		{
			return (float)etat.SensFrisson * etat.ForceFrisson * ampleur * enveloppe * (float)Math.Sin(13.823007675795091 * Math.Max(0.0, age - retard));
		}
	}

	private static long ArretDuCoup(bool precis, bool casse)
	{
		return casse ? 130 : (precis ? 95 : 70);
	}

	private static Color Melange(Color a, Color b, float k)
	{
		k = Borne(k);
		return new Color(255, (int)((float)(int)a.R + (float)(b.R - a.R) * k), (int)((float)(int)a.G + (float)(b.G - a.G) * k), (int)((float)(int)a.B + (float)(b.B - a.B) * k));
	}

	private static Color Assombrir(Color c, float k)
	{
		return new Color(255, (int)((float)(int)c.R * (1f - k)), (int)((float)(int)c.G * (1f - k)), (int)((float)(int)c.B * (1f - k)));
	}

	private static Color Eclaircir(Color c, float k)
	{
		return Melange(c, Color.White, k);
	}

	private static bool EstPierre(Masque masque, Etat etat, int x, int y)
	{
		if (masque.Alpha(x, y) >= 128)
		{
			if (etat.Usure != null)
			{
				return !etat.Usure[y * masque.Largeur + x];
			}
			return true;
		}
		return false;
	}

	private static Masque? MasqueDeLaRoche(Resource ressource, Etat etat)
	{
		IGameTexture texture = etat.Texture ?? ressource.Texture;
		FloatRect source = ((etat.Texture != null) ? etat.Source : ressource.RenderSource);
		return MasqueDe(texture, source);
	}

	private static FloatRect DestinationDeLaRoche(Resource ressource, Etat etat)
	{
		if (etat.Texture == null)
		{
			return ressource.RenderDestination;
		}
		return ressource.DestinationPour(etat.Source, etat.Abaissee);
	}

	private static int BasDeLaPierre(Masque masque)
	{
		for (int num = masque.Hauteur - 1; num >= 0; num--)
		{
			for (int i = 0; i < masque.Largeur; i++)
			{
				if (masque.Alpha(i, num) >= 128)
				{
					return num;
				}
			}
		}
		return masque.Hauteur - 1;
	}

	private static (Color Sombre, Color Moyen, Color Clair) TonsDeLaPierre(Masque masque)
	{
		List<Color> tons = new List<Color>();
		for (int i = 0; i < masque.Hauteur; i++)
		{
			for (int j = 0; j < masque.Largeur; j++)
			{
				if (masque.Alpha(j, i) >= 128)
				{
					tons.Add(masque.Couleur(j, i));
				}
			}
		}
		if (tons.Count == 0)
		{
			return (Sombre: Hex(6249559), Moyen: Hex(9210501), Clair: Hex(12894649));
		}
		tons.Sort((Color a, Color b) => (a.R + a.G + a.B).CompareTo(b.R + b.G + b.B));
		return (Sombre: Ton(0.12f), Moyen: Ton(0.5f), Clair: Ton(0.88f));
		Color Ton(float k)
		{
			return tons[Math.Min(tons.Count - 1, (int)((float)tons.Count * k))];
		}
	}

	private static (int X, int Y) PointDImpactSurLaPierre(Masque masque, Etat etat, int cote, int coteY)
	{
		int largeur = masque.Largeur;
		int hauteur = masque.Hauteur;
		if (cote != 0)
		{
			int num = Math.Clamp(hauteur - 15 + (new int[5] { 0, -2, 1, -1, 0 })[etat.Gestes % 5], 0, hauteur - 1);
			for (int i = 0; i < hauteur; i++)
			{
				int num2 = Math.Clamp(num + ((i % 2 == 0) ? (i / 2) : (-(i / 2 + 1))), 0, hauteur - 1);
				for (int j = 0; j < largeur; j++)
				{
					int num3 = ((cote < 0) ? j : (largeur - 1 - j));
					if (EstPierre(masque, etat, num3, num2))
					{
						return (X: num3, Y: num2);
					}
				}
			}
			return (X: largeur / 2, Y: num);
		}
		int num4 = Math.Clamp(largeur / 2 + Hasard.Next(-3, 4), 0, largeur - 1);
		if (coteY < 0)
		{
			for (int k = 0; k < hauteur; k++)
			{
				if (EstPierre(masque, etat, num4, k))
				{
					return (X: num4, Y: k);
				}
			}
		}
		else
		{
			for (int num5 = BasDeLaPierre(masque) - 7; num5 >= 0; num5--)
			{
				if (EstPierre(masque, etat, num4, num5))
				{
					return (X: num4, Y: num5);
				}
			}
		}
		return (X: num4, Y: hauteur / 2);
	}

	private static void Mordre(Masque masque, Etat etat, FrappeDeRoche frappe, int nombre)
	{
		if (etat.Usure == null)
		{
			etat.Usure = new bool[masque.Largeur * masque.Hauteur];
		}
		int num = ((frappe.Cote != 0) ? (-frappe.Cote) : 0);
		for (int i = 0; i < 60; i++)
		{
			if (frappe.Morsures.Count >= nombre)
			{
				break;
			}
			int num2 = frappe.Ix + (int)MathF.Round(((float)Hasard.NextDouble() - 0.5f) * 3f + (float)num * (float)Hasard.NextDouble() * 1.5f);
			int num3 = frappe.Iy + (int)MathF.Round(((float)Hasard.NextDouble() - 0.5f) * 6f);
			if (EstPierre(masque, etat, num2, num3) && (!EstPierre(masque, etat, num2 - 1, num3) || !EstPierre(masque, etat, num2 + 1, num3) || !EstPierre(masque, etat, num2, num3 - 1) || !EstPierre(masque, etat, num2, num3 + 1)))
			{
				etat.Usure[num3 * masque.Largeur + num2] = true;
				frappe.Morsures.Add((num2, num3));
			}
		}
	}

	private static Faille AjouterFaille(Masque masque, Etat etat, int ix, int iy, long ne)
	{
		float num = 0f;
		float num2 = 0f;
		int num3 = 0;
		for (int i = 0; i < masque.Hauteur; i++)
		{
			for (int j = 0; j < masque.Largeur; j++)
			{
				if (EstPierre(masque, etat, j, i))
				{
					num += (float)j;
					num2 += (float)i;
					num3++;
				}
			}
		}
		float num4 = ((num3 > 0) ? (num / (float)num3) : ((float)masque.Largeur / 2f));
		float num5 = ((num3 > 0) ? (num2 / (float)num3) : ((float)masque.Hauteur / 2f));
		int count = etat.Failles.Count;
		float num6 = ix;
		float num7 = iy;
		float num8 = MathF.Atan2(num5 - (float)iy, num4 - (float)ix) + (-0.62f + (float)(count % 4) * 0.4f + ((float)Hasard.NextDouble() - 0.5f) * 0.35f);
		List<(float, float)> list = new List<(float, float)> { (num6, num7) };
		int num9 = 5 + Hasard.Next(3);
		for (int k = 0; k < num9; k++)
		{
			bool flag = false;
			for (int l = 0; l < 4; l++)
			{
				if (flag)
				{
					break;
				}
				float num10 = MathF.Atan2(num5 - num7, num4 - num6) - num8;
				float num11 = num8 + ((float)Hasard.NextDouble() - 0.5f) * 0.9f + ((l > 0) ? ((float)(MathF.Sign((num10 == 0f) ? 1f : num10) * l) * 0.5f) : 0f);
				float num12 = 2.2f + (float)Hasard.NextDouble() * 2.4f;
				float num13 = num6 + MathF.Cos(num11) * num12;
				float num14 = num7 + MathF.Sin(num11) * num12;
				if (EstPierre(masque, etat, (int)MathF.Round(num13), (int)MathF.Round(num14)))
				{
					num8 = num11;
					num6 = num13;
					num7 = num14;
					flag = true;
				}
			}
			if (!flag)
			{
				break;
			}
			list.Add((num6, num7));
		}
		Faille faille = new Faille
		{
			Ne = ne
		};
		RasteriserSurLaPierre(list, masque, etat, faille.Trait);
		if (list.Count >= 4)
		{
			(float, float) item = list[2 + Hasard.Next(list.Count - 3)];
			List<(float, float)> list2 = new List<(float, float)> { item };
			float num15 = num8 + ((Hasard.Next(2) == 0) ? 0.95f : (-0.95f));
			float num16 = item.Item1;
			float num17 = item.Item2;
			for (int m = 0; m < 3; m++)
			{
				num15 += ((float)Hasard.NextDouble() - 0.5f) * 0.8f;
				float num18 = 2f + (float)Hasard.NextDouble() * 2.5f;
				float num19 = num16 + MathF.Cos(num15) * num18;
				float num20 = num17 + MathF.Sin(num15) * num18;
				if (!EstPierre(masque, etat, (int)MathF.Round(num19), (int)MathF.Round(num20)))
				{
					break;
				}
				num16 = num19;
				num17 = num20;
				list2.Add((num16, num17));
			}
			RasteriserSurLaPierre(list2, masque, etat, faille.Branche);
		}
		etat.Failles.Add(faille);
		etat.Impacts.Add((ix, iy));
		return faille;
	}

	private static void RasteriserSurLaPierre(List<(float X, float Y)> points, Masque masque, Etat etat, List<(int X, int Y)> pixels)
	{
		HashSet<(int, int)> hashSet = new HashSet<(int, int)>();
		for (int i = 0; i + 1 < points.Count; i++)
		{
			foreach (var item in Segment((int)MathF.Round(points[i].X), (int)MathF.Round(points[i].Y), (int)MathF.Round(points[i + 1].X), (int)MathF.Round(points[i + 1].Y)))
			{
				if (EstPierre(masque, etat, item.X, item.Y) && hashSet.Add(item))
				{
					pixels.Add(item);
				}
			}
		}
	}

	private static IEnumerable<(int X, int Y)> PixelsVisibles(Faille faille, long maintenant)
	{
		float num = Borne((float)(maintenant - faille.Ne) / 90f);
		IEnumerable<(int, int)> first = faille.Trait.Take(Math.Max(1, (int)MathF.Round((float)faille.Trait.Count * num)));
		IEnumerable<(int, int)> second = (IEnumerable<(int, int)>)((num < 0.5f) ? ((IEnumerable)Enumerable.Empty<(int, int)>()) : ((IEnumerable)faille.Branche.Take((int)MathF.Round((float)faille.Branche.Count * (num * 2f - 1f)))));
		return first.Concat(second);
	}

	private static void FrapperRocher(Resource ressource, Etat etat, Entity? recolteur, bool precis, long maintenant)
	{
		Masque masque = MasqueDeLaRoche(ressource, etat);
		if (masque == null || !MapInstance.TryGet(ressource.MapId, out MapInstance _))
		{
			return;
		}
		FloatRect floatRect = DestinationDeLaRoche(ressource, etat);
		int num = 0;
		int coteY = 0;
		if (recolteur != null)
		{
			FloatRect worldPos = recolteur.WorldPos;
			float num2 = floatRect.X + floatRect.Width / 2f;
			float num3 = ((worldPos.Width > 0f) ? (worldPos.X + worldPos.Width / 2f) : num2);
			num = ((num3 < num2 - 6f) ? (-1) : ((num3 > num2 + 6f) ? 1 : 0));
			if (num == 0)
			{
				coteY = ((recolteur.Y >= ressource.Y) ? 1 : (-1));
			}
		}
		(int X, int Y) tuple = PointDImpactSurLaPierre(masque, etat, num, coteY);
		int item = tuple.X;
		int item2 = tuple.Y;
		etat.CederDepuis = -1L;
		etat.Frappe = new FrappeDeRoche
		{
			Debut = maintenant,
			Precis = precis,
			Cote = num,
			CoteY = coteY,
			Ix = item,
			Iy = item2,
			Frappeur = (recolteur?.Id ?? Guid.Empty)
		};
		if (recolteur != null)
		{
			float elan = 3f;
			if (num != 0)
			{
				FloatRect worldPos2 = recolteur.WorldPos;
				float num4 = worldPos2.X + worldPos2.Width / 2f - (float)(num * 13);
				float num5 = floatRect.X + (float)item + (float)((num > 0) ? 1 : 0);
				elan = Math.Clamp(MathF.Abs(num5 - num4) - 1f, 0f, 10f);
			}
			PosesDeFrappe[recolteur.Id] = new PoseDeFrappe
			{
				Debut = maintenant,
				Precis = precis,
				Elan = elan,
				Dx = -num,
				Dy = ((num == 0) ? Math.Sign(ressource.Y - recolteur.Y) : 0)
			};
		}
	}

	private static void PrecisSurRocher(Resource ressource, Etat etat, Entity? recolteur, long maintenant)
	{
		FrappeDeRoche frappe = etat.Frappe;
		if (frappe == null || frappe.Precis)
		{
			return;
		}
		frappe.Precis = true;
		if (recolteur != null && PosesDeFrappe.TryGetValue(recolteur.Id, out PoseDeFrappe value) && value.Debut == frappe.Debut)
		{
			value.Precis = true;
		}
		if (frappe.Libere)
		{
			Masque masque = MasqueDeLaRoche(ressource, etat);
			if (masque != null && MapInstance.TryGet(ressource.MapId, out MapInstance instance))
			{
				FloatRect floatRect = DestinationDeLaRoche(ressource, etat);
				LancerEclatsDePierre(ressource, etat, frappe, masque, floatRect.X - (float)instance.X, floatRect.Y - (float)instance.Y, 2, 4, maintenant);
			}
		}
	}

	private static void BriserRocher(Resource ressource, Etat etat, Entity? recolteur, long maintenant)
	{
		etat.CederDepuis = -1L;
		FrappeDeRoche frappe = etat.Frappe;
		if (frappe != null && !frappe.Libere)
		{
			frappe.Casse = true;
			if (frappe.Touche)
			{
				etat.EclairGeneral = maintenant;
				Guid frappeur = frappe.Frappeur;
				Guid? obj = Globals.Me?.Id;
				if (frappeur == obj)
				{
					CombatFeedback.StartShake(2f, 220L, -frappe.Cote, -frappe.CoteY);
				}
				SonDeRoche("recolte_roche_fin.wav", ressource);
				SonDeRoche("recolte_roche_masse.wav", ressource);
			}
		}
		else
		{
			FrapperRocher(ressource, etat, recolteur, precis: false, maintenant - 130);
			if (etat.Frappe != null)
			{
				etat.Frappe.Casse = true;
			}
		}
		FrappeDeRoche frappe2 = etat.Frappe;
		if (frappe2 != null && recolteur != null && PosesDeFrappe.TryGetValue(recolteur.Id, out PoseDeFrappe value) && value.Debut == frappe2.Debut)
		{
			value.Casse = true;
		}
	}

	private static void ArmerCeder(Resource ressource, Etat etat, long maintenant)
	{
		if (etat.CederDepuis < 0 && !ressource.IsDead && etat.Failles.Count != 0)
		{
			etat.CederDepuis = maintenant + 250;
			etat.ProchainRuisseau = etat.CederDepuis;
			etat.ProchaineFumee = etat.CederDepuis + 200;
			SonDeRoche("recolte_roche_grince.wav", ressource);
		}
	}

	private static void SonDeRoche(string fichier, Entity? source)
	{
		if (source != null)
		{
			CombatAudio.Play(fichier, source, 0.06f);
		}
	}

	public static int? ImageDeFrappe(Entity entite)
	{
		if (PosesDeFrappe.Count == 0 || !PosesDeFrappe.TryGetValue(entite.Id, out PoseDeFrappe value))
		{
			return null;
		}
		long num = Timing.Global.Milliseconds - value.Debut;
		long num2 = ArretDuCoup(value.Precis, value.Casse);
		if (num < 0)
		{
			return null;
		}
		if (num < 110)
		{
			return 0;
		}
		if (num < 130 + num2 + 30)
		{
			return 1;
		}
		long num3 = num - 130 - num2 - 30;
		if (num3 >= 150)
		{
			if (num3 >= 320)
			{
				return null;
			}
			return 3;
		}
		return 2;
	}

	private static FloatRect DecalageDeFrappe(Entity entite, FloatRect rectangle)
	{
		if (PosesDeFrappe.Count == 0 || !PosesDeFrappe.TryGetValue(entite.Id, out PoseDeFrappe value))
		{
			return rectangle;
		}
		long num = Timing.Global.Milliseconds - value.Debut;
		long num2 = ArretDuCoup(value.Precis, value.Casse);
		float num3 = (value.Casse ? 2f : 1f);
		if (num < 0 || num > 130 + num2 + 30 + 150 || entite.IsMoving)
		{
			if (num > 2000)
			{
				PosesDeFrappe.Remove(entite.Id);
			}
			return rectangle;
		}
		float x = ((num < 110) ? ((0f - num3) * Sortie((float)num / 110f)) : ((num < 130) ? Lerp(0f - num3, value.Elan, (float)(num - 110) / 20f) : ((num >= 130 + num2 + 30) ? Lerp(value.Elan, 0f, Sortie((float)(num - 130 - num2 - 30) / 150f)) : value.Elan)));
		float num4 = MathF.Round(x);
		return new FloatRect(rectangle.X + (float)value.Dx * num4, rectangle.Y + (float)value.Dy * num4, rectangle.Width, rectangle.Height);
	}

	private static void MettreAJourMinage(long maintenant, float dt)
	{
		foreach (var (key, etat2) in Etats)
		{
			if ((etat2.Frappe == null && etat2.CederDepuis < 0 && etat2.Morceaux == null && etat2.Ruisseaux.Count == 0) || !Globals.Entities.TryGetValue(key, out Entity value) || !(value is Resource resource) || Metier(resource) != 1 || !MapInstance.TryGet(resource.MapId, out MapInstance instance))
			{
				continue;
			}
			Masque masque = MasqueDeLaRoche(resource, etat2);
			if (masque == null)
			{
				continue;
			}
			FloatRect floatRect = DestinationDeLaRoche(resource, etat2);
			float num = floatRect.X - (float)instance.X;
			float num2 = floatRect.Y - (float)instance.Y;
			FrappeDeRoche frappe = etat2.Frappe;
			if (frappe != null)
			{
				if (!frappe.Touche && maintenant >= frappe.Debut + 130)
				{
					frappe.Touche = true;
					Toucher(resource, etat2, frappe, masque, num, num2, frappe.Debut + 130);
				}
				long num3 = frappe.Debut + 130 + ArretDuCoup(frappe.Precis, frappe.Casse);
				if (frappe.Touche && !frappe.Libere && maintenant >= num3)
				{
					frappe.Libere = true;
					Liberer(resource, etat2, frappe, masque, num, num2, num3, maintenant);
				}
			}
			if (etat2.CederDepuis >= 0 && !resource.IsDead && maintenant >= etat2.CederDepuis)
			{
				if (maintenant >= etat2.ProchainRuisseau && etat2.Failles.Count > 0)
				{
					etat2.ProchainRuisseau = maintenant + 190 + Hasard.Next(120);
					List<(int, int)> list = etat2.Failles.SelectMany((Faille f) => f.Trait).ToList();
					if (list.Count > 0)
					{
						var (px, py) = list[Hasard.Next(list.Count)];
						etat2.Ruisseaux.Add(new Ruissellement
						{
							Px = px,
							Py = py,
							Couleur = Eclaircir(TonsDeLaPierre(masque).Clair, 0.1f),
							Ne = maintenant
						});
					}
				}
				if (maintenant >= etat2.ProchaineFumee)
				{
					etat2.ProchaineFumee = maintenant + 650;
					PoussieresDeRoche.Add(new PoussiereDeRoche
					{
						MapId = resource.MapId,
						X = num + (float)masque.Largeur / 2f + (float)Hasard.Next(-6, 7),
						Y = num2 + (float)BasDeLaPierre(masque) + 1f,
						Sol = true,
						Sens = ((Hasard.Next(2) == 0) ? 1 : (-1)),
						Couleur = Melange(TonsDeLaPierre(masque).Clair, TonDePoussiere, 0.5f),
						Graine = (int)(maintenant % 100000),
						Ne = maintenant,
						Ampleur = 0.4f,
						Nombre = 3,
						Largeur = 12f
					});
				}
			}
			int num4 = BasDeLaPierre(masque);
			foreach (Ruissellement item in etat2.Ruisseaux)
			{
				if (item.Mort || maintenant < item.Ne)
				{
					continue;
				}
				if (item.Prochain == 0L)
				{
					item.Prochain = item.Ne;
				}
				while (maintenant >= item.Prochain && !item.Mort)
				{
					item.Prochain += 40 + Hasard.Next(30);
					if (EstPierre(masque, etat2, item.Px, item.Py + 1))
					{
						item.Py++;
						continue;
					}
					if (EstPierre(masque, etat2, item.Px - 1, item.Py + 1))
					{
						item.Px--;
						item.Py++;
						continue;
					}
					if (EstPierre(masque, etat2, item.Px + 1, item.Py + 1))
					{
						item.Px++;
						item.Py++;
						continue;
					}
					item.Mort = true;
					GrainsDePierre.Add(new GrainDePierre
					{
						MapId = resource.MapId,
						X = num + (float)item.Px + 0.5f,
						Y = num2 + (float)num4 + 1f + (float)Hasard.NextDouble() * 2f,
						H = MathF.Max(0f, num4 - item.Py),
						Vx = ((float)Hasard.NextDouble() - 0.5f) * 8f,
						Couleur = item.Couleur,
						Ne = maintenant
					});
				}
			}
			etat2.Ruisseaux.RemoveAll((Ruissellement r) => r.Mort);
			List<MorceauDeRoche> morceaux = etat2.Morceaux;
			if (morceaux != null)
			{
				foreach (MorceauDeRoche item2 in morceaux)
				{
					AvancerMorceau(item2, resource, dt);
				}
				float fondu = Borne(1f - (float)(maintenant - etat2.Brise - 850) / 320f);
				morceaux.ForEach(delegate(MorceauDeRoche m)
				{
					m.Alpha = fondu;
				});
				if (fondu <= 0f)
				{
					etat2.Morceaux = null;
				}
			}
			FrappeDeRoche frappe2 = etat2.Frappe;
			if (frappe2 != null && frappe2.Libere && maintenant - frappe2.Debut > 4000)
			{
				etat2.Frappe = null;
			}
		}
		foreach (EclatDePierre item3 in EclatsDePierre)
		{
			AvancerEclat(item3, dt, maintenant);
		}
		foreach (GrainDePierre item4 in GrainsDePierre)
		{
			if (maintenant < item4.Ne || item4.Pose >= 0)
			{
				continue;
			}
			item4.Vh -= 924f * dt;
			item4.X += item4.Vx * dt;
			item4.Y += item4.Vy * dt;
			item4.H += item4.Vh * dt;
			if (!(item4.H > 0f))
			{
				item4.H = 0f;
				if (!item4.Rebond && item4.Vh < -30f)
				{
					item4.Rebond = true;
					item4.Vh = (0f - item4.Vh) * 0.3f;
					item4.Vx *= 0.5f;
				}
				else
				{
					item4.Pose = maintenant;
				}
			}
		}
		EclatsDePierre.RemoveAll((EclatDePierre e) => e.Mort || maintenant - e.Ne > 6000);
		GrainsDePierre.RemoveAll((GrainDePierre g) => (g.Pose >= 0 && maintenant - g.Pose > 380) || maintenant - g.Ne > 5000);
		PoussieresDeRoche.RemoveAll((PoussiereDeRoche p) => p.Mort || maintenant - p.Ne > 2000);
		ButinsDeRoche.RemoveAll((ButinDeRoche b) => b.Mort || maintenant - b.Ne > 4000);
		ChocsDeFrappe.RemoveAll((ChocDeFrappe c) => maintenant - c.Ne > c.Duree);
		if (EclatsDePierre.Count > 120)
		{
			EclatsDePierre.RemoveRange(0, EclatsDePierre.Count - 120);
		}
		if (GrainsDePierre.Count > 400)
		{
			GrainsDePierre.RemoveRange(0, GrainsDePierre.Count - 400);
		}
		if (PosesDeFrappe.Count > 0)
		{
			Guid[] array = (from kv in PosesDeFrappe
				where maintenant - kv.Value.Debut > 2000
				select kv.Key).ToArray();
			foreach (Guid key2 in array)
			{
				PosesDeFrappe.Remove(key2);
			}
		}
	}

	private static void Toucher(Resource ressource, Etat etat, FrappeDeRoche frappe, Masque masque, float ox, float oy, long instant)
	{
		etat.Affaisse = instant;
		bool flag = frappe.Precis || frappe.Casse;
		float x = ox + (float)frappe.Ix + (float)((frappe.Cote < 0) ? (-1) : ((frappe.Cote > 0) ? 1 : 0));
		float y = oy + (float)frappe.Iy + (float)((frappe.Cote == 0) ? ((frappe.CoteY >= 0) ? 1 : (-1)) : 0);
		ChocsDeFrappe.Add(new ChocDeFrappe(ressource.MapId, x, y, instant, ArretDuCoup(frappe.Precis, frappe.Casse), flag));
		SonDeRoche($"recolte_roche_{1 + Hasard.Next(5)}.wav", ressource);
		SonDeRoche(flag ? $"recolte_roche_craque_fort_{1 + Hasard.Next(2)}.wav" : $"recolte_roche_craque_{1 + Hasard.Next(3)}.wav", ressource);
		if (frappe.Casse)
		{
			etat.EclairGeneral = instant;
			Guid frappeur = frappe.Frappeur;
			Guid? obj = Globals.Me?.Id;
			if (frappeur == obj)
			{
				CombatFeedback.StartShake(2f, 220L, -frappe.Cote, -frappe.CoteY);
			}
			SonDeRoche("recolte_roche_fin.wav", ressource);
			SonDeRoche("recolte_roche_masse.wav", ressource);
		}
		else
		{
			Random hasard = Hasard;
			Hasard = new Random(ressource.Id.GetHashCode() * 31 + etat.Failles.Count);
			Faille faille = AjouterFaille(masque, etat, frappe.Ix, frappe.Iy, instant);
			faille.Eclair = ArretDuCoup(frappe.Precis, casse: false) + 40;
			Mordre(masque, etat, frappe, frappe.Precis ? 4 : 2);
			Hasard = hasard;
		}
	}

	private static void Liberer(Resource ressource, Etat etat, FrappeDeRoche frappe, Masque masque, float ox, float oy, long instant, long maintenant)
	{
		(Color, Color, Color) tuple = TonsDeLaPierre(masque);
		Color couleur = Melange(tuple.Item3, TonDePoussiere, 0.5f);
		int num = BasDeLaPierre(masque);
		float num2 = oy + (float)num + 1f;
		int num3 = ((frappe.Cote != 0) ? (-frappe.Cote) : 0);
		int num4 = ((frappe.Cote == 0) ? (-frappe.CoteY) : 0);
		if (!frappe.Casse)
		{
			LancerEclatsDePierre(ressource, etat, frappe, masque, ox, oy, frappe.Precis ? 5 : 3, frappe.Precis ? 11 : 7, instant);
			PoussieresDeRoche.Add(new PoussiereDeRoche
			{
				MapId = ressource.MapId,
				X = ox + (float)frappe.Ix - (float)num3,
				Y = oy + (float)frappe.Iy,
				Sens = num3,
				SensY = num4,
				Couleur = couleur,
				Graine = (int)(instant % 100000),
				Ne = instant,
				Ampleur = (frappe.Precis ? 1.3f : 1f)
			});
			float x = ((frappe.Cote == 0) ? (ox + (float)frappe.Ix) : (ox + (float)BordDeLaRangee(masque, etat, num, frappe.Cote) - (float)(frappe.Cote * 5)));
			PoussieresDeRoche.Add(new PoussiereDeRoche
			{
				MapId = ressource.MapId,
				X = x,
				Y = num2,
				Sol = true,
				Sens = num3,
				Couleur = couleur,
				Graine = (int)(instant % 100000) + 3,
				Ne = instant + 20,
				Ampleur = (frappe.Precis ? 0.9f : 0.7f),
				Nombre = 5,
				Largeur = 14f
			});
			if (etat.Failles.Count <= 0)
			{
				return;
			}
			List<Faille> failles = etat.Failles;
			List<(int, int)> trait = failles[failles.Count - 1].Trait;
			if (trait != null && trait.Count > 0)
			{
				for (int i = 0; i < 2; i++)
				{
					var (px, py) = trait[Math.Min(trait.Count - 1, 1 + Hasard.Next(trait.Count))];
					etat.Ruisseaux.Add(new Ruissellement
					{
						Px = px,
						Py = py,
						Couleur = Eclaircir(tuple.Item3, 0.1f),
						Ne = instant + 120 + i * 160
					});
				}
			}
			return;
		}
		Fracturer(ressource, etat, frappe, masque, ox, oy, instant);
		for (int j = 0; j < 16; j++)
		{
			GrainsDePierre.Add(new GrainDePierre
			{
				MapId = ressource.MapId,
				X = ox + (float)frappe.Ix + (float)Hasard.NextDouble() * 4f,
				Y = num2 + (float)Hasard.NextDouble() * 4f,
				H = (float)(num - frappe.Iy) + ((float)Hasard.NextDouble() - 0.5f) * 6f,
				Vx = (float)frappe.Cote * (50f + (float)Hasard.NextDouble() * 100f) + ((frappe.Cote == 0) ? (((float)Hasard.NextDouble() - 0.5f) * 100f) : 0f),
				Vy = ((float)Hasard.NextDouble() - 0.5f) * 36f + (float)(num4 * -40),
				Vh = 50f + (float)Hasard.NextDouble() * 120f,
				Couleur = (new Color[3] { tuple.Item1, tuple.Item2, tuple.Item3 })[Hasard.Next(3)],
				Ne = instant
			});
		}
		float num5 = ox + (float)masque.Largeur / 2f;
		PoussieresDeRoche.Add(new PoussiereDeRoche
		{
			MapId = ressource.MapId,
			X = num5,
			Y = num2,
			Sol = true,
			Sens = num3,
			Couleur = couleur,
			Graine = (int)(instant % 100000),
			Ne = instant,
			Ampleur = 1.35f,
			Nombre = 11,
			Largeur = masque.Largeur + 4
		});
		PoussieresDeRoche.Add(new PoussiereDeRoche
		{
			MapId = ressource.MapId,
			X = num5 + (float)(num3 * -2),
			Y = num2 - 2f,
			Sol = true,
			Sens = num3,
			Couleur = couleur,
			Graine = (int)(instant % 100000) + 11,
			Ne = instant + 110,
			Ampleur = 1.15f,
			Nombre = 9,
			Largeur = masque.Largeur - 2
		});
		PoussieresDeRoche.Add(new PoussiereDeRoche
		{
			MapId = ressource.MapId,
			X = ox + (float)frappe.Ix - (float)num3,
			Y = oy + (float)frappe.Iy,
			Sens = num3,
			SensY = num4,
			Couleur = couleur,
			Graine = (int)(instant % 100000) + 5,
			Ne = instant,
			Ampleur = 1.5f
		});
	}

	private static int BordDeLaRangee(Masque masque, Etat etat, int y, int cote)
	{
		for (int i = 0; i < masque.Largeur; i++)
		{
			int num = ((cote < 0) ? i : (masque.Largeur - 1 - i));
			if (EstPierre(masque, etat, num, y))
			{
				return num;
			}
		}
		return masque.Largeur / 2;
	}

	private static void LancerEclatsDePierre(Resource ressource, Etat etat, FrappeDeRoche frappe, Masque masque, float ox, float oy, int eclats, int grains, long instant)
	{
		(Color, Color, Color) tuple = TonsDeLaPierre(masque);
		int num = BasDeLaPierre(masque);
		float num2 = oy + (float)num + 1f;
		float num3 = frappe.Cote;
		float num4 = ((frappe.Cote == 0) ? frappe.CoteY : 0);
		List<(int, int)> list = frappe.Morsures.ToList();
		while (list.Count < eclats)
		{
			list.Add((frappe.Ix - frappe.Cote * Hasard.Next(3), frappe.Iy + Hasard.Next(-2, 3)));
		}
		for (int i = 0; i < eclats; i++)
		{
			(int, int) tuple2 = list[i];
			int item = tuple2.Item1;
			int item2 = tuple2.Item2;
			Dictionary<(int, int), Color> grille = FormerEclat(masque, item, item2, 2 + Hasard.Next(3));
			EclatsDePierre.Add(new EclatDePierre
			{
				MapId = ressource.MapId,
				X = ox + (float)item,
				Y = num2 + (float)Hasard.NextDouble() * 3f,
				H = num - item2,
				Vx = num3 * (55f + (float)Hasard.NextDouble() * 75f) + ((num3 == 0f) ? (((float)Hasard.NextDouble() - 0.5f) * 70f) : 0f),
				Vy = num4 * (40f + (float)Hasard.NextDouble() * 50f) + ((float)Hasard.NextDouble() - 0.5f) * 26f,
				Vh = 70f + (float)Hasard.NextDouble() * 80f,
				Tour = ((float)Hasard.NextDouble() - 0.5f) * 18f,
				Ne = instant + i * 12,
				Grille = grille,
				Rayon = 3
			});
		}
		for (int j = 0; j < grains; j++)
		{
			GrainsDePierre.Add(new GrainDePierre
			{
				MapId = ressource.MapId,
				X = ox + (float)frappe.Ix + (float)Hasard.NextDouble() * 2f,
				Y = num2 + (float)Hasard.NextDouble() * 3f,
				H = (float)(num - frappe.Iy) + ((float)Hasard.NextDouble() - 0.5f) * 3f,
				Vx = num3 * (40f + (float)Hasard.NextDouble() * 90f) + ((num3 == 0f) ? (((float)Hasard.NextDouble() - 0.5f) * 80f) : 0f),
				Vy = num4 * (30f + (float)Hasard.NextDouble() * 60f) + ((float)Hasard.NextDouble() - 0.5f) * 30f,
				Vh = 40f + (float)Hasard.NextDouble() * 110f,
				Couleur = (new Color[3] { tuple.Item1, tuple.Item2, tuple.Item3 })[Hasard.Next(3)],
				Ne = instant
			});
		}
	}

	private static Dictionary<(int, int), Color> FormerEclat(Masque masque, int px, int py, int taille)
	{
		(int, int)[] array = new(int, int)[7]
		{
			(1, 0),
			(0, 1),
			(1, 1),
			(-1, 0),
			(0, -1),
			(-1, 1),
			(1, -1)
		};
		List<(int, int)> list = new List<(int, int)> { (0, 0) };
		while (list.Count < taille)
		{
			(int, int) item = array[Hasard.Next(array.Length)];
			if (!list.Contains(item))
			{
				list.Add(item);
			}
		}
		Dictionary<(int, int), Color> dictionary = new Dictionary<(int, int), Color>();
		foreach (var item4 in list)
		{
			int item2 = item4.Item1;
			int item3 = item4.Item2;
			Color color = masque.Couleur(Math.Clamp(px + item2, 0, masque.Largeur - 1), Math.Clamp(py + item3, 0, masque.Hauteur - 1));
			dictionary[(item2, item3)] = ((item3 < 0) ? Eclaircir(color, 0.14f) : ((item3 > 0) ? Assombrir(color, 0.18f) : new Color(255, color.R, color.G, color.B)));
		}
		return dictionary;
	}

	private static void AvancerEclat(EclatDePierre eclat, float dt, long maintenant)
	{
		if (maintenant < eclat.Ne || eclat.Repos >= 0)
		{
			return;
		}
		eclat.Vh -= 966f * dt;
		eclat.X += eclat.Vx * dt;
		eclat.Y += eclat.Vy * dt;
		eclat.H += eclat.Vh * dt;
		eclat.Angle += eclat.Tour * dt;
		if (eclat.H > 0f)
		{
			return;
		}
		eclat.H = 0f;
		if (eclat.Vh < -45f && eclat.Rebonds < 2)
		{
			eclat.Vh = (0f - eclat.Vh) * 0.34f;
			eclat.Vx *= 0.55f;
			eclat.Vy *= 0.55f;
			eclat.Tour *= 0.45f;
			if (eclat.Rebonds++ == 0 && maintenant - _dernierTic > 40)
			{
				Player me = Globals.Me;
				if (me != null)
				{
					_dernierTic = maintenant;
					CombatAudio.Play($"recolte_roche_tic_{1 + Hasard.Next(3)}.wav", me, 0.1f);
				}
			}
		}
		else
		{
			eclat.Vh = 0f;
			float num = MathF.Exp((0f - dt) * 9f);
			eclat.Vx *= num;
			eclat.Vy *= num;
			eclat.Tour *= num;
			if (MathF.Sqrt(eclat.Vx * eclat.Vx + eclat.Vy * eclat.Vy) < 3f)
			{
				eclat.Repos = maintenant;
				eclat.Angle = MathF.Round(eclat.Angle / ((float)Math.PI / 2f)) * ((float)Math.PI / 2f);
			}
		}
	}

	private static void Fracturer(Resource ressource, Etat etat, FrappeDeRoche frappe, Masque masque, float ox, float oy, long instant)
	{
		int largeur = masque.Largeur;
		List<(float, float)> list = new List<(float, float)>();
		foreach (var impact in etat.Impacts)
		{
			int item = impact.X;
			int item2 = impact.Y;
			(float, float) g = (item - frappe.Cote * 4, item2);
			if (!list.Any<(float, float)>(((float X, float Y) q) => MathF.Sqrt((q.X - g.Item1) * (q.X - g.Item1) + (q.Y - g.Item2) * (q.Y - g.Item2)) < 6f))
			{
				list.Add(g);
			}
		}
		List<(int, int)> list2 = new List<(int, int)>();
		for (int num = 0; num < masque.Hauteur; num++)
		{
			for (int num2 = 0; num2 < largeur; num2++)
			{
				if (EstPierre(masque, etat, num2, num))
				{
					list2.Add((num2, num));
				}
			}
		}
		if (list2.Count == 0)
		{
			etat.Brise = instant;
			return;
		}
		for (int num3 = 0; num3 < 80; num3++)
		{
			if (list.Count >= 5)
			{
				break;
			}
			var (px, py) = list2[Hasard.Next(list2.Count)];
			if (!list.Any<(float, float)>(((float X, float Y) q) => MathF.Sqrt((q.X - (float)px) * (q.X - (float)px) + (q.Y - (float)py) * (q.Y - (float)py)) < 8f))
			{
				list.Add((px, py));
			}
		}
		HashSet<(int, int)> hashSet = new HashSet<(int, int)>(etat.Failles.SelectMany((Faille f) => f.Trait.Concat(f.Branche)));
		List<List<(int, int)>> list3 = list.Select<(float, float), List<(int, int)>>(((float X, float Y) _) => new List<(int, int)>()).ToList();
		(Color, Color, Color) tuple2 = TonsDeLaPierre(masque);
		int num4 = BasDeLaPierre(masque);
		float num5 = oy + (float)num4 + 1f;
		int num6 = ((frappe.Cote != 0) ? (-frappe.Cote) : 0);
		foreach (var (num7, num8) in list2)
		{
			if (hashSet.Contains((num7, num8)) && Hasard.NextDouble() < 0.75)
			{
				GrainsDePierre.Add(new GrainDePierre
				{
					MapId = ressource.MapId,
					X = ox + (float)num7,
					Y = num5 + (float)Hasard.NextDouble() * 4f,
					H = num4 - num8,
					Vx = (float)((num7 - frappe.Ix) * 3) + ((float)Hasard.NextDouble() - 0.3f) * 30f * (float)((num6 == 0) ? 1 : num6),
					Vy = ((float)Hasard.NextDouble() - 0.5f) * 36f,
					Vh = 50f + (float)Hasard.NextDouble() * 100f,
					Couleur = masque.Couleur(num7, num8),
					Ne = instant
				});
				continue;
			}
			int index = 0;
			float num9 = float.MaxValue;
			for (int num10 = 0; num10 < list.Count; num10++)
			{
				float num11 = (list[num10].Item1 - (float)num7) * (list[num10].Item1 - (float)num7) + (list[num10].Item2 - (float)num8) * (list[num10].Item2 - (float)num8) * 1.3f;
				if (num11 < num9)
				{
					num9 = num11;
					index = num10;
				}
			}
			list3[index].Add((num7, num8));
		}
		List<List<(int, int)>> source = list3.Where((List<(int X, int Y)> p) => p.Count > 0).ToList();
		List<List<(int, int)>> list4 = source.Where((List<(int X, int Y)> p) => p.Count >= 16).ToList();
		foreach (List<(int, int)> item5 in source.Where((List<(int X, int Y)> p) => p.Count < 16))
		{
			if (list4.Count == 0)
			{
				list4.Add(item5);
				continue;
			}
			var (cx, cy) = Centre(item5);
			list4.OrderBy(delegate(List<(int X, int Y)> p)
			{
				var (num20, num21) = Centre(p);
				return (num20 - cx) * (num20 - cx) + (num21 - cy) * (num21 - cy);
			}).First().AddRange(item5);
		}
		List<MorceauDeRoche> list5 = new List<MorceauDeRoche>();
		foreach (List<(int, int)> item6 in list4)
		{
			(float Cx, float Cy) tuple5 = Centre(item6);
			float item3 = tuple5.Cx;
			float item4 = tuple5.Cy;
			int num12 = item6.Min<(int, int)>(((int X, int Y) p) => p.X);
			int num13 = item6.Max<(int, int)>(((int X, int Y) p) => p.X);
			int num14 = item6.Min<(int, int)>(((int X, int Y) p) => p.Y);
			int num15 = item6.Max<(int, int)>(((int X, int Y) p) => p.Y);
			int count = item6.Count;
			float num16 = item3 - (float)frappe.Ix;
			float num17 = item4 - (float)frappe.Iy;
			float num18 = MathF.Max(0.001f, MathF.Sqrt(num16 * num16 + num17 * num17));
			float num19 = (16f + (float)Hasard.NextDouble() * 18f) / MathF.Sqrt((float)count / 70f);
			list5.Add(new MorceauDeRoche
			{
				X = ox + item3,
				Y = num5 - 1f + (float)Hasard.NextDouble() * 3f,
				H = (float)num4 - item4,
				Vx = num16 / num18 * num19 + (float)(num6 * 6),
				Vy = num17 / num18 * num19 * 0.3f + ((float)Hasard.NextDouble() - 0.5f) * 10f - (float)((frappe.Cote == 0) ? (frappe.CoteY * 5) : 0),
				Vh = 18f + (1f - item4 / (float)masque.Hauteur) * 46f,
				Tour = (float)MathF.Sign((num16 == 0f) ? 1f : num16) * (1.2f + (float)Hasard.NextDouble() * 2.6f) / MathF.Sqrt((float)count / 70f),
				Cx = item3,
				Cy = item4,
				MinX = num12,
				MaxX = num13,
				MinY = num14,
				MaxY = num15,
				Masse = count,
				Largeur = largeur,
				Grille = item6.ToDictionary<(int, int), int, Color>(((int X, int Y) p) => p.Y * largeur + p.X, ((int X, int Y) p) => masque.Couleur(p.X, p.Y)),
				Rayon = (int)MathF.Ceiling(MathF.Sqrt(MathF.Pow(MathF.Max((float)num13 - item3, item3 - (float)num12), 2f) + MathF.Pow(MathF.Max((float)num15 - item4, item4 - (float)num14), 2f))) + 1,
				HRepos = MathF.Max(1.5f, (float)(num15 - num14) / 2f)
			});
		}
		etat.Morceaux = list5;
		etat.Brise = instant;
		static (float Cx, float Cy) Centre(List<(int X, int Y)> p)
		{
			return (Cx: (float)p.Average(((int X, int Y) q) => q.X), Cy: (float)p.Average(((int X, int Y) q) => q.Y));
		}
	}

	private static void AvancerMorceau(MorceauDeRoche morceau, Resource ressource, float dt)
	{
		if (morceau.Repos || dt <= 0f)
		{
			return;
		}
		morceau.Vh -= 525f * dt;
		morceau.X += morceau.Vx * dt;
		morceau.Y += morceau.Vy * dt;
		morceau.H += morceau.Vh * dt;
		morceau.Angle += morceau.Tour * dt;
		if (morceau.H > morceau.HRepos)
		{
			return;
		}
		morceau.H = morceau.HRepos;
		if (morceau.Vh < -40f && morceau.Rebonds < 2)
		{
			morceau.Vh = (0f - morceau.Vh) * 0.22f;
			morceau.Vx *= 0.55f;
			morceau.Vy *= 0.55f;
			morceau.Tour *= 0.45f;
			if (morceau.Rebonds++ == 0)
			{
				SonDeRoche((morceau.Masse > 70) ? "recolte_roche_morceau_gros.wav" : "recolte_roche_morceau_petit.wav", ressource);
				long milliseconds = Timing.Global.Milliseconds;
				PoussieresDeRoche.Add(new PoussiereDeRoche
				{
					MapId = ressource.MapId,
					X = morceau.X,
					Y = morceau.Y + 1f,
					Sol = true,
					Sens = -MathF.Sign((morceau.Vx == 0f) ? 1f : morceau.Vx),
					Couleur = TonDePoussiere,
					Graine = (int)(morceau.X * 7f),
					Ne = milliseconds,
					Ampleur = 0.45f,
					Nombre = 3,
					Largeur = 8f
				});
			}
		}
		else
		{
			morceau.Vh = 0f;
			float num = MathF.Exp((0f - dt) * 7f);
			morceau.Vx *= num;
			morceau.Vy *= num;
			morceau.Tour *= num;
			if (MathF.Sqrt(morceau.Vx * morceau.Vx + morceau.Vy * morceau.Vy) < 2f && MathF.Abs(morceau.Tour) < 0.2f)
			{
				morceau.Repos = true;
			}
		}
	}

	private static Func<char, Color> PaletteDeButin(Color clair, Color moyen, Color sombre, Color? eclat = null, Color? veine = null)
	{
		return delegate(char lettre)
		{
			switch (lettre)
			{
			case 'k':
				return ContourDeButin;
			case 'L':
			case 'W':
				return clair;
			case 'B':
			case 'M':
				return moyen;
			case 'D':
				return sombre;
			case 'O':
				return eclat ?? clair;
			case 'o':
				return veine ?? moyen;
			default:
				return moyen;
			}
		};
	}

	public static void SurButin(Guid itemId)
	{
		Player me = Globals.Me;
		if (me == null || !DatabaseObject<ItemDescriptor>.TryGet(itemId, out var databaseObject))
		{
			return;
		}
		long milliseconds = Timing.Global.Milliseconds;
		foreach (KeyValuePair<Guid, Etat> etat2 in Etats)
		{
			etat2.Deconstruct(out var key, out var value);
			Guid key2 = key;
			Etat etat = value;
			FrappeDeRoche frappe = etat.Frappe;
			if (frappe != null && frappe.Casse && !(frappe.Frappeur != me.Id) && milliseconds - frappe.Debut <= 2500 && Globals.Entities.TryGetValue(key2, out Entity value2) && value2 is Resource resource && MapInstance.TryGet(resource.MapId, out MapInstance instance))
			{
				FloatRect floatRect = DestinationDeLaRoche(resource, etat);
				long num = frappe.Debut + 130 + ArretDuCoup(frappe.Precis, casse: true);
				int num2 = frappe.Butins++;
				string text = (databaseObject.Icon ?? string.Empty).ToLowerInvariant();
				bool cristal = false;
				string[] lignes;
				Func<char, Color> palette;
				if (text.Contains("cristal"))
				{
					lignes = LignesCristal;
					palette = PaletteDeButin(Hex(15268095), Hex(4499434), Hex(2189241));
					cristal = true;
				}
				else if (text.Contains("cuivre"))
				{
					lignes = LignesCuivre;
					palette = PaletteDeButin(Hex(14209734), Hex(9407363), Hex(6249559), Hex(16164958), Hex(12739882));
				}
				else if (text.Contains("pierre"))
				{
					lignes = LignesPierre;
					palette = PaletteDeButin(Hex(12894649), Hex(9210501), Hex(6249559));
				}
				else
				{
					(Color Sombre, Color Moyen, Color Clair) tuple = TonsDeLIcone(databaseObject.Icon);
					Color item = tuple.Sombre;
					Color item2 = tuple.Moyen;
					Color item3 = tuple.Clair;
					lignes = LignesPierre;
					palette = PaletteDeButin(item3, item2, item);
				}
				ButinsDeRoche.Add(new ButinDeRoche
				{
					MapId = resource.MapId,
					X = floatRect.X - (float)instance.X + floatRect.Width / 2f + (float)(num2 * 3),
					Y = floatRect.Y - (float)instance.Y + floatRect.Height * 0.8f,
					Ne = Math.Max(milliseconds, num + 420 + num2 * 180),
					Lignes = lignes,
					Palette = palette,
					Joueur = me.Id,
					Cristal = cristal
				});
				break;
			}
		}
	}

	private static (Color Sombre, Color Moyen, Color Clair) TonsDeLIcone(string? icone)
	{
		List<Color> tons;
		if (!string.IsNullOrEmpty(icone))
		{
			IGameTexture gameTexture = Globals.ContentManager?.GetTexture(TextureType.Item, icone);
			if (gameTexture != null && gameTexture.Width > 0 && gameTexture.Height > 0)
			{
				Masque masque = MasqueDe(gameTexture, new FloatRect(0f, 0f, gameTexture.Width, gameTexture.Height));
				if (masque != null)
				{
					tons = new List<Color>();
					for (int i = 0; i < masque.Hauteur; i++)
					{
						for (int j = 0; j < masque.Largeur; j++)
						{
							Color color = masque.Couleur(j, i);
							if (masque.Alpha(j, i) >= 200 && color.R + color.G + color.B > 120)
							{
								tons.Add(color);
							}
						}
					}
					if (tons.Count == 0)
					{
						return (Sombre: Hex(6249559), Moyen: Hex(9210501), Clair: Hex(12894649));
					}
					tons.Sort((Color a, Color b) => (a.R + a.G + a.B).CompareTo(b.R + b.G + b.B));
					return (Sombre: Ton(0.15f), Moyen: Ton(0.5f), Clair: Ton(0.85f));
				}
			}
		}
		return (Sombre: Hex(6249559), Moyen: Hex(9210501), Clair: Hex(12894649));
		Color Ton(float k)
		{
			return tons[Math.Min(tons.Count - 1, (int)((float)tons.Count * k))];
		}
	}

	private static void ReconstruireLesFissures(Resource ressource, Etat etat, IGameTexture texture, FloatRect source)
	{
		long num = ressource.MaxVital[0];
		if (etat.FissuresReconstruites || num <= 0)
		{
			return;
		}
		etat.FissuresReconstruites = true;
		long num2 = ressource.Vital[0];
		if (etat.Failles.Count > 0 || num2 >= num || ressource.IsDead)
		{
			return;
		}
		Masque masque = MasqueDe(texture, source);
		if (masque != null)
		{
			int num3 = Math.Clamp((int)MathF.Round((1f - (float)num2 / (float)num) * 6f), 1, 6);
			Random hasard = Hasard;
			for (int i = 0; i < num3; i++)
			{
				Hasard = new Random(ressource.Id.GetHashCode() * 31 + i);
				var (ix, iy) = PointDImpactSurLaPierre(masque, etat, (i % 2 != 0) ? 1 : (-1), 0);
				AjouterFaille(masque, etat, ix, iy, -2305843009213693952L);
			}
			Hasard = hasard;
		}
	}

	private static bool DessinerRocher(Resource ressource, Etat etat, IGameTexture texture, FloatRect source, FloatRect destination, float alpha, long maintenant)
	{
		ReconstruireLesFissures(ressource, etat, texture, source);
		if (etat.Usure == null && etat.Failles.Count == 0 && maintenant - etat.Affaisse >= 90 && etat.Ruisseaux.Count == 0)
		{
			return false;
		}
		Masque masque = MasqueDe(texture, source);
		if (masque == null)
		{
			return false;
		}
		int num = ((maintenant - etat.Affaisse < 90) ? 1 : 0);
		float x = destination.X;
		float num2 = destination.Y + (float)num;
		Color renderColor = ((alpha >= 1f) ? Color.White : Fondu(Color.White, alpha));
		if (etat.Usure == null)
		{
			Graphics.DrawGameTexture(texture, source, new FloatRect(x, num2, destination.Width, destination.Height), renderColor);
		}
		else
		{
			for (int i = 0; i < masque.Hauteur; i++)
			{
				int num3 = -1;
				for (int j = 0; j <= masque.Largeur; j++)
				{
					bool flag = j < masque.Largeur && !etat.Usure[i * masque.Largeur + j];
					if (flag && num3 < 0)
					{
						num3 = j;
					}
					else if (!flag && num3 >= 0)
					{
						Graphics.DrawGameTexture(texture, new FloatRect(source.X + (float)num3, source.Y + (float)i, j - num3, 1f), new FloatRect(x + (float)num3, num2 + (float)i, j - num3, 1f), renderColor);
						num3 = -1;
					}
				}
			}
		}
		if (alpha >= 1f)
		{
			DessinerFailles(etat, masque, x, num2, maintenant);
			foreach (Ruissellement item in etat.Ruisseaux)
			{
				if (!item.Mort && maintenant >= item.Ne)
				{
					Rect(x + (float)item.Px, num2 + (float)item.Py, 1f, 1f, item.Couleur);
				}
			}
		}
		return true;
	}

	private static void DessinerFailles(Etat etat, Masque masque, float x0, float y0, long maintenant)
	{
		if (etat.Failles.Count == 0)
		{
			return;
		}
		List<List<(int, int)>> list = etat.Failles.Select((Faille f) => (from p in PixelsVisibles(f, maintenant)
			where EstPierre(masque, etat, p.X, p.Y)
			select p).ToList()).ToList();
		HashSet<(int, int)> hashSet = new HashSet<(int, int)>(list.SelectMany((List<(int X, int Y)> v) => v));
		long num = maintenant - etat.EclairGeneral;
		float x1 = ((num < 0 || num >= 190) ? 0f : ((num < 140) ? 1f : (1f - (float)(num - 140) / 50f)));
		(int, int)[] array = new(int, int)[4]
		{
			(-1, 0),
			(1, 0),
			(0, -1),
			(0, 1)
		};
		for (int num2 = 0; num2 < etat.Failles.Count; num2++)
		{
			Faille faille = etat.Failles[num2];
			List<(int, int)> list2 = list[num2];
			long num3 = maintenant - faille.Ne;
			float num4 = MathF.Max(x1, (num3 < faille.Eclair) ? 1f : Borne(1f - (float)(num3 - faille.Eclair) / 140f));
			bool flag = num2 < etat.Failles.Count - 1;
			for (int num5 = 0; num5 < list2.Count; num5++)
			{
				var (num6, num7) = list2[num5];
				if (num4 > 0.4f)
				{
					(int, int)[] array2 = array;
					for (int num8 = 0; num8 < array2.Length; num8++)
					{
						var (num9, num10) = array2[num8];
						if (EstPierre(masque, etat, num6 + num9, num7 + num10) && !hashSet.Contains((num6 + num9, num7 + num10)))
						{
							Rect(x0 + (float)num6 + (float)num9, y0 + (float)num7 + (float)num10, 1f, 1f, Fondu(Assombrir(masque.Couleur(num6 + num9, num7 + num10), 0.5f), 0.7f * num4));
						}
					}
				}
				if (num4 < 1f && EstPierre(masque, etat, num6, num7 + 1) && !hashSet.Contains((num6, num7 + 1)))
				{
					Rect(x0 + (float)num6, y0 + (float)num7 + 1f, 1f, 1f, Fondu(Eclaircir(masque.Couleur(num6, num7 + 1), 0.32f), 0.85f * (1f - num4)));
				}
				if (flag && (float)num5 < (float)list2.Count * 0.35f && EstPierre(masque, etat, num6, num7 - 1) && !hashSet.Contains((num6, num7 - 1)))
				{
					Rect(x0 + (float)num6, y0 + (float)num7 - 1f, 1f, 1f, Fondu(Melange(Assombrir(masque.Couleur(num6, num7 - 1), 0.45f), LumiereDeFaille, num4), 0.9f));
				}
				Rect(x0 + (float)num6, y0 + (float)num7, 1f, 1f, Melange(Assombrir(masque.Couleur(num6, num7), 0.64f), LumiereDeFaille, num4));
			}
		}
	}

	private static bool DessinerRocherBrise(Resource ressource, Etat etat, IGameTexture texture, FloatRect source, FloatRect destination, long maintenant)
	{
		FrappeDeRoche frappe = etat.Frappe;
		if (frappe != null && frappe.Casse && !frappe.Libere && etat.Texture != null)
		{
			FloatRect floatRect = DestinationDeLaRoche(ressource, etat);
			if (!DessinerRocher(ressource, etat, etat.Texture, etat.Source, floatRect, 1f, maintenant))
			{
				Graphics.DrawGameTexture(etat.Texture, etat.Source, floatRect, Color.White);
			}
			return true;
		}
		if (etat.Brise < 0 || maintenant - etat.Brise > 1550)
		{
			return false;
		}
		long num = maintenant - etat.Brise;
		float num2 = Borne((float)(num - 780) / 380f);
		if (num2 > 0f)
		{
			Graphics.DrawGameTexture(texture, source, destination, (num2 >= 1f) ? Color.White : Fondu(Color.White, num2));
		}
		List<MorceauDeRoche> morceaux = etat.Morceaux;
		if (morceaux != null && MapInstance.TryGet(ressource.MapId, out MapInstance instance))
		{
			foreach (MorceauDeRoche item in morceaux)
			{
				DessinerOmbreDeMorceau(item, instance.X, instance.Y);
			}
			foreach (MorceauDeRoche item2 in morceaux)
			{
				DessinerMorceau(item2, instance.X, instance.Y);
			}
		}
		return true;
	}

	private static void DessinerOmbreDeMorceau(MorceauDeRoche morceau, float carteX, float carteY)
	{
		float num = MathF.Max(3f, (float)(morceau.MaxX - morceau.MinX) / 2f + 1f);
		float num2 = MathF.Max(1f, num * 0.34f);
		float num3 = 0.28f * morceau.Alpha * Borne(1f - (morceau.H - morceau.HRepos) / 40f);
		if (num3 <= 0.01f)
		{
			return;
		}
		float num4 = Arrondi(carteX + morceau.X);
		float num5 = Arrondi(carteY + morceau.Y);
		Color couleur = Fondu(OmbreDeDebris, num3);
		for (int i = -(int)MathF.Ceiling(num2); i <= (int)MathF.Ceiling(num2); i++)
		{
			float num6 = MathF.Round(num * MathF.Sqrt(MathF.Max(0f, 1f - (float)i / (num2 + 0.5f) * ((float)i / (num2 + 0.5f)))));
			if (num6 > 0f)
			{
				Rect(num4 - num6, num5 + (float)i, num6 * 2f, 1f, couleur);
			}
		}
	}

	private static void DessinerMorceau(MorceauDeRoche morceau, float carteX, float carteY)
	{
		if (morceau.Alpha <= 0f)
		{
			return;
		}
		float num = Arrondi(carteX + morceau.X);
		float num2 = Arrondi(carteY + morceau.Y - morceau.H);
		float num3 = MathF.Cos(0f - morceau.Angle);
		float num4 = MathF.Sin(0f - morceau.Angle);
		for (int i = -morceau.Rayon; i <= morceau.Rayon; i++)
		{
			for (int j = -morceau.Rayon; j <= morceau.Rayon; j++)
			{
				float num5 = num3 * ((float)j + 0.5f) - num4 * ((float)i + 0.5f);
				float num6 = num4 * ((float)j + 0.5f) + num3 * ((float)i + 0.5f);
				int num7 = (int)MathF.Floor(morceau.Cx + 0.5f + num5);
				int num8 = (int)MathF.Floor(morceau.Cy + 0.5f + num6);
				if (num7 >= 0 && num8 >= 0 && num7 < morceau.Largeur && morceau.Grille.TryGetValue(num8 * morceau.Largeur + num7, out Color value))
				{
					Rect(num + (float)j, num2 + (float)i, 1f, 1f, (morceau.Alpha >= 1f) ? value : Fondu(value, morceau.Alpha));
				}
			}
		}
	}

	private static List<(float X, float Y)> LangueOrientee(float bx, float by, float dx, float dy, float large, float haut, float courbe)
	{
		float num = 0f - dy;
		List<(float, float)> list = new List<(float, float)>(13);
		(float, float) a = (bx - num * large / 2f, by - dx * large / 2f);
		(float, float) b = (bx + num * large / 2f, by + dx * large / 2f);
		(float, float) tuple = (bx + dx * haut + num * courbe, by + dy * haut + dx * courbe);
		(float, float) c = (bx + dx * haut * 0.6f - num * large * 0.3f, by + dy * haut * 0.6f - dx * large * 0.3f);
		(float, float) c2 = (bx + dx * haut * 0.55f + num * large * 0.3f, by + dy * haut * 0.55f + dx * large * 0.3f);
		for (int i = 0; i <= 6; i++)
		{
			list.Add(Q(a, c, tuple, (float)i / 6f));
		}
		for (int j = 1; j <= 6; j++)
		{
			list.Add(Q(tuple, c2, b, (float)j / 6f));
		}
		return list;
		static (float X, float Y) Q((float X, float Y) tuple2, (float X, float Y) tuple3, (float X, float Y) tuple4, float t)
		{
			return (X: (1f - t) * (1f - t) * tuple2.X + 2f * (1f - t) * t * tuple3.X + t * t * tuple4.X, Y: (1f - t) * (1f - t) * tuple2.Y + 2f * (1f - t) * t * tuple3.Y + t * t * tuple4.Y);
		}
	}

	private static void RemplirTrame(IReadOnlyList<(float X, float Y)> points, Color couleur, float dissolution)
	{
		float x = points.Min(((float X, float Y) p) => p.Y);
		float x2 = points.Max(((float X, float Y) p) => p.Y);
		List<float> list = new List<float>(8);
		for (int num = (int)MathF.Floor(x); num <= (int)MathF.Ceiling(x2); num++)
		{
			float num2 = (float)num + 0.5f;
			list.Clear();
			for (int num3 = 0; num3 < points.Count; num3++)
			{
				(float, float) tuple = points[num3];
				(float, float) tuple2 = points[(num3 + 1) % points.Count];
				if ((tuple.Item2 <= num2 && tuple2.Item2 > num2) || (tuple2.Item2 <= num2 && tuple.Item2 > num2))
				{
					list.Add(tuple.Item1 + (num2 - tuple.Item2) / (tuple2.Item2 - tuple.Item2) * (tuple2.Item1 - tuple.Item1));
				}
			}
			list.Sort();
			for (int num4 = 0; num4 + 1 < list.Count; num4 += 2)
			{
				int num5 = (int)MathF.Ceiling(list[num4] - 0.5f);
				int num6 = (int)MathF.Floor(list[num4 + 1] - 0.5f);
				if (num6 < num5)
				{
					continue;
				}
				if (dissolution <= 0f)
				{
					Rect(num5, num, num6 - num5 + 1, 1f, couleur);
					continue;
				}
				for (int num7 = num5; num7 <= num6; num7++)
				{
					if ((float)Bayer[(num & 3) * 4 + (num7 & 3)] / 16f >= dissolution)
					{
						Rect(num7, num, 1f, 1f, couleur);
					}
				}
			}
		}
	}

	private static void DessinerPoussieresDeRoche(bool derriere, long maintenant)
	{
		foreach (PoussiereDeRoche item in PoussieresDeRoche)
		{
			long num = maintenant - item.Ne;
			if (num < 0 || !MapInstance.TryGet(item.MapId, out MapInstance instance))
			{
				continue;
			}
			int num2 = (item.Sol ? 78 : 58);
			int num3 = (item.Sol ? 8 : 6);
			int num4 = (int)(num / num2);
			if (num4 >= num3)
			{
				item.Mort = true;
				continue;
			}
			float[] array = ((!item.Sol) ? new float[6] { 0.55f, 1f, 1f, 0.85f, 0.62f, 0.4f } : new float[8] { 0.3f, 0.7f, 0.95f, 1f, 0.95f, 0.82f, 0.62f, 0.42f });
			float[] array2 = ((!item.Sol) ? new float[6] { 0f, 0f, 0.18f, 0.42f, 0.66f, 0.86f } : new float[8] { 0f, 0f, 0f, 0.08f, 0.25f, 0.45f, 0.66f, 0.84f });
			Func<float> func = Alea(item.Graine * 31 + num4 * 97 + (derriere ? 7 : 0));
			Color couleur = Fondu(derriere ? Eclaircir(item.Couleur, 0.18f) : item.Couleur, derriere ? 0.34f : 0.62f);
			float num5 = (derriere ? 1.3f : 1f);
			float num6 = (float)instance.X + item.X;
			float num7 = (float)instance.Y + item.Y;
			if (item.Sol)
			{
				int nombre = item.Nombre;
				for (int i = 0; i < nombre; i++)
				{
					float num8 = ((nombre > 1) ? (((float)i - (float)(nombre - 1) / 2f) / ((float)(nombre - 1) / 2f)) : 0f);
					float bx = num6 + num8 * item.Largeur / 2f + (func() - 0.5f) * 2f;
					float num9 = num7 - MathF.Abs(num8) * 1.5f + (float)(derriere ? (-1) : 0);
					float num10 = (float)(-item.Sens) * (0.25f + 0.2f * MathF.Abs(num8)) + num8 * 0.25f;
					float num11 = MathF.Sqrt(num10 * num10 + 1f);
					bool flag = i % 2 == 0;
					float haut = (flag ? (11f + 7f * (1f - MathF.Abs(num8))) : (5f + 4f * (1f - MathF.Abs(num8)))) * array[num4] * item.Ampleur * num5 * (0.85f + func() * 0.3f);
					float large = (flag ? (3.5f + func() * 1.5f) : (6.5f + func() * 2f)) * num5;
					RemplirTrame(LangueOrientee(bx, num9, num10 / num11, -1f / num11, large, haut, (func() - 0.5f) * 3f), couleur, array2[num4]);
				}
			}
			else
			{
				float num12 = ((item.Sens != 0) ? MathF.Atan2(-0.35f, -item.Sens) : (-(float)Math.PI / 2f));
				for (int j = 0; j < 5; j++)
				{
					float x = num12 + ((float)j - 2f) * 0.34f + (func() - 0.5f) * 0.15f;
					float haut2 = (5f + 4f * (1f - MathF.Abs(j - 2) / 2f)) * array[num4] * item.Ampleur * num5 * (0.8f + func() * 0.4f);
					float large2 = (5f + func() * 2.5f) * num5;
					float bx2 = num6 + (func() - 0.5f) * 2f;
					float num13 = num7 + (float)(j - 2) * 1.4f;
					RemplirTrame(LangueOrientee(bx2, num13, MathF.Cos(x), MathF.Sin(x), large2, haut2, (func() - 0.5f) * 2f), couleur, array2[num4]);
				}
			}
		}
	}

	private static void DessinerMinage(long maintenant)
	{
		foreach (ChocDeFrappe item in ChocsDeFrappe)
		{
			long num = maintenant - item.Ne;
			if (num >= 0 && num < item.Duree && MapInstance.TryGet(item.MapId, out MapInstance instance))
			{
				(int, int)[] array = ((num >= 40) ? new(int, int)[4]
				{
					(0, 0),
					(-1, -1),
					(-1, 1),
					(-2, 0)
				} : ((!item.Fort) ? new(int, int)[7]
				{
					(0, 0),
					(-1, 0),
					(0, -1),
					(0, 1),
					(-2, -1),
					(1, -2),
					(-1, 2)
				} : new(int, int)[10]
				{
					(0, 0),
					(-1, 0),
					(1, 0),
					(0, -1),
					(0, 1),
					(-2, -1),
					(1, -2),
					(-1, 2),
					(2, 1),
					(-3, 0)
				}));
				float num2 = Arrondi((float)instance.X + item.X);
				float num3 = Arrondi((float)instance.Y + item.Y);
				for (int i = 0; i < array.Length; i++)
				{
					Rect(num2 + (float)array[i].Item1, num3 + (float)array[i].Item2, 1f, 1f, (i == 0) ? ChocBlanc : Fondu(ChocChaud, 0.85f));
				}
			}
		}
		foreach (EclatDePierre item2 in EclatsDePierre)
		{
			if (maintenant < item2.Ne || !MapInstance.TryGet(item2.MapId, out MapInstance instance2))
			{
				continue;
			}
			float num4 = ((item2.Repos < 0) ? 1f : Borne(1f - (float)(maintenant - item2.Repos - 1000) / 350f));
			if (num4 <= 0f)
			{
				item2.Mort = true;
				continue;
			}
			Rect(Arrondi((float)instance2.X + item2.X) - 1f, Arrondi((float)instance2.Y + item2.Y) + 1f, 3f, 1f, Fondu(OmbreDeDebris, 0.24f * num4 * Borne(1f - item2.H / 30f)));
			float num5 = Arrondi((float)instance2.X + item2.X);
			float num6 = Arrondi((float)instance2.Y + item2.Y - item2.H);
			float num7 = MathF.Cos(0f - item2.Angle);
			float num8 = MathF.Sin(0f - item2.Angle);
			for (int j = -item2.Rayon; j <= item2.Rayon; j++)
			{
				for (int k = -item2.Rayon; k <= item2.Rayon; k++)
				{
					(int, int) key = ((int)MathF.Round(num7 * (float)k - num8 * (float)j), (int)MathF.Round(num8 * (float)k + num7 * (float)j));
					if (item2.Grille.TryGetValue(key, out Color value))
					{
						Rect(num5 + (float)k, num6 + (float)j, 1f, 1f, Fondu(value, 0.93f * num4));
					}
				}
			}
		}
		foreach (GrainDePierre item3 in GrainsDePierre)
		{
			if (maintenant >= item3.Ne && MapInstance.TryGet(item3.MapId, out MapInstance instance3))
			{
				float part = ((item3.Pose < 0) ? 0.95f : (0.6f * Borne(1f - (float)(maintenant - item3.Pose) / 380f)));
				Rect(Arrondi((float)instance3.X + item3.X), Arrondi((float)instance3.Y + item3.Y - item3.H), 1f, 1f, Fondu(item3.Couleur, part));
			}
		}
		DessinerPoussieresDeRoche(derriere: false, maintenant);
		foreach (ButinDeRoche item4 in ButinsDeRoche)
		{
			DessinerButinDeRoche(item4, maintenant);
		}
	}

	private static void DessinerButinDeRoche(ButinDeRoche butin, long maintenant)
	{
		long num = maintenant - butin.Ne;
		if (num < 0 || !MapInstance.TryGet(butin.MapId, out MapInstance instance))
		{
			return;
		}
		if (num >= 1080 || !Globals.Entities.TryGetValue(butin.Joueur, out Entity value))
		{
			butin.Mort = true;
			return;
		}
		if (!butin.Sonne)
		{
			butin.Sonne = true;
			SonDeRoche(butin.Cristal ? "recolte_butin_cristal.wav" : ((butin.Lignes == LignesCuivre) ? "recolte_butin_cuivre.wav" : "recolte_butin_pierre.wav"), value);
		}
		FloatRect worldPos = value.WorldPos;
		Direction directionFacing = value.DirectionFacing;
		float num2 = worldPos.X + worldPos.Width / 2f + (float)(directionFacing switch
		{
			Direction.Left => -1, 
			Direction.Right => 1, 
			_ => 0, 
		} * 5);
		float num3 = worldPos.Y + worldPos.Height * 0.6f;
		float b = worldPos.Y + worldPos.Height - 2f;
		float a = (float)instance.X + butin.X;
		float num4 = (float)instance.Y + butin.Y;
		float part = 1f;
		float num6;
		float num7;
		if (num < 540)
		{
			float num5 = (float)num / 540f;
			num6 = Lerp(a, num2, num5);
			num7 = Lerp(num4 - 3f, num3, num5) - MathF.Sin((float)Math.PI * num5) * 26f;
			float valeur = Lerp(num4, b, num5);
			Rect(Arrondi(num6) - 2f, Arrondi(valeur), 5f, 1f, Fondu(OmbreDeDebris, 0.25f * Borne(1f - MathF.Sin((float)Math.PI * num5) * 0.8f)));
		}
		else if (num < 840)
		{
			num6 = num2;
			num7 = num3 + MathF.Round(MathF.Sin((float)(num - 540) / 70f) * 0.8f);
		}
		else
		{
			float num8 = (float)(num - 540 - 300) / 240f;
			num6 = Lerp(num2, worldPos.X + worldPos.Width / 2f - 3f, Lisse(num8));
			num7 = Lerp(num3, worldPos.Y + worldPos.Height * 0.77f, Lisse(num8));
			part = 1f - Sortie(Borne((num8 - 0.35f) / 0.65f));
		}
		int length = butin.Lignes[0].Length;
		int num9 = butin.Lignes.Length;
		float num10 = Arrondi(num6 - (float)length / 2f);
		float num11 = Arrondi(num7 - (float)num9 / 2f);
		for (int i = 0; i < num9; i++)
		{
			for (int j = 0; j < length; j++)
			{
				char c = butin.Lignes[i][j];
				if (c != '.')
				{
					Rect(num10 + (float)j, num11 + (float)i, 1f, 1f, Fondu(butin.Palette(c), part));
				}
			}
		}
		float num12 = (float)num - 216f;
		if (num12 >= 0f && num12 < 180f)
		{
			int num13 = (new int[3] { 1, 2, 1 })[(int)(num12 / 60f)];
			float num14 = Arrondi(num6 + 4f);
			float num15 = Arrondi(num7 - 4f);
			Color couleur = (butin.Cristal ? Hex(15268095) : Hex(16774872));
			Rect(num14 - (float)num13, num15, num13 * 2 + 1, 1f, couleur);
			Rect(num14, num15 - (float)num13, 1f, num13 * 2 + 1, couleur);
		}
	}

	private static void Eclabousser(Guid mapId, MapInstance carte, float ix, float iy, long maintenant, int graine, float ampleur)
	{
		float x = ix - (float)carte.X;
		float num = iy - (float)carte.Y;
		Gerbes.Add(new Gerbe(mapId, x, num, maintenant, graine, ampleur));
		Ondes.Add(new Onde(mapId, x, num, maintenant, 560L, Poussiere: false, ampleur));
		Ondes.Add(new Onde(mapId, x, num, maintenant + 130, 560L, Poussiere: false, ampleur));
		for (int i = 0; i < 4; i++)
		{
			Eclats.Add(new Eclat
			{
				Genre = Genre.Goutte,
				MapId = mapId,
				X = x,
				Y = num - 6f,
				Vx = ((float)Hasard.NextDouble() - 0.5f) * 2.2f,
				Vy = -2.2f - (float)Hasard.NextDouble(),
				Sol = num + (float)Hasard.NextDouble() * 2f,
				Couleur = CouleursGouttes[Hasard.Next(2)],
				Taille = 2,
				Ne = maintenant + 60
			});
		}
		if (Gerbes.Count > 24)
		{
			Gerbes.RemoveAt(0);
		}
	}

	private static void DessinerGerbes(long maintenant)
	{
		for (int num = Gerbes.Count - 1; num >= 0; num--)
		{
			Gerbe gerbe = Gerbes[num];
			long num2 = maintenant - gerbe.Ne;
			MapInstance instance;
			if (num2 >= 330)
			{
				Gerbes.RemoveAt(num);
			}
			else if (num2 >= 0 && MapInstance.TryGet(gerbe.MapId, out instance))
			{
				int num3 = (int)(num2 / 55);
				float num4 = (new float[6] { 0.45f, 0.95f, 1f, 0.8f, 0.5f, 0.22f })[num3] * gerbe.Ampleur;
				Func<float> func = Alea(gerbe.Graine * 31 + num3 * 97);
				float num5 = (float)instance.X + gerbe.X;
				float y = (float)instance.Y + gerbe.Y;
				float[] array = gerbe.Style ?? GerbeTouche;
				float num6 = (float)(array.Length - 1) / 2f;
				float num7 = 3.4f * MathF.Min(1.25f, 0.8f + gerbe.Ampleur * 0.2f);
				(Color, float, float, float)[] array2 = new(Color, float, float, float)[2]
				{
					(EauClaire, 0.55f, 1.25f, 4.2f),
					(Color.White, 0.9f, 1f, 2.6f)
				};
				for (int i = 0; i < array2.Length; i++)
				{
					(Color, float, float, float) tuple = array2[i];
					Color item = tuple.Item1;
					float item2 = tuple.Item2;
					float item3 = tuple.Item3;
					float item4 = tuple.Item4;
					Color couleur = Fondu(item, item2 * ((num3 >= 4) ? 0.7f : 1f));
					for (int j = 0; j < array.Length; j++)
					{
						float num8 = num5 + ((float)j - num6) * num7;
						float haut = array[j] * num4 * item3 * (0.85f + func() * 0.3f);
						float tx = num8 + ((float)j - num6) * 1.7f * num4 + (func() - 0.5f) * 2f;
						RemplirPolygone(Langue(num8, y, item4, haut, tx), couleur);
					}
				}
			}
		}
	}

	private static List<(float X, float Y)> Langue(float bx, float y, float large, float haut, float tx)
	{
		List<(float, float)> list = new List<(float, float)>(14);
		(float, float) a = (bx - large / 2f, y);
		(float, float) tuple = (tx, y - haut);
		(float, float) b = (bx + large / 2f, y);
		for (int i = 0; i <= 6; i++)
		{
			list.Add(Quadratique(a, (X: bx - large * 0.2f, Y: y - haut * 0.55f), tuple, (float)i / 6f));
		}
		for (int j = 1; j <= 6; j++)
		{
			list.Add(Quadratique(tuple, (X: bx + large * 0.2f, Y: y - haut * 0.5f), b, (float)j / 6f));
		}
		return list;
		static (float X, float Y) Quadratique((float X, float Y) tuple2, (float X, float Y) c, (float X, float Y) tuple3, float t)
		{
			return (X: (1f - t) * (1f - t) * tuple2.X + 2f * (1f - t) * t * c.X + t * t * tuple3.X, Y: (1f - t) * (1f - t) * tuple2.Y + 2f * (1f - t) * t * c.Y + t * t * tuple3.Y);
		}
	}

	private static void DessinerOmbresDePoissons(long maintenant, float dt)
	{
		foreach (Entity value in Globals.Entities.Values)
		{
			if (!(value is Resource resource))
			{
				continue;
			}
			ResourceDescriptor? descriptor = resource.Descriptor;
			if (descriptor == null || descriptor.Tool != 3 || !MapInstance.TryGet(resource.MapId, out MapInstance instance))
			{
				continue;
			}
			Etat etat = EtatDe(resource);
			float num = (float)(resource.X * TailleCase) + (float)TailleCase / 2f;
			float num2 = (float)(resource.Y * HauteurCase) + (float)HauteurCase / 2f;
			Guid coin = resource.Id;
			Etat etat2 = etat;
			if (etat2.Banc == null)
			{
				etat2.Banc = new Banc(num, num2, resource.IsDead)
				{
					SurGrignote = delegate
					{
						Grignoter(coin);
					}
				};
			}
			etat.Banc.CentreX = num;
			etat.Banc.CentreY = num2;
			Pecheur pecheur = PecheurDuCoin(coin);
			float bouchonX = float.NaN;
			float bouchonY = 0f;
			if (pecheur != null && pecheur.Phase == PhaseDePeche.Lancer && pecheur.Flotteur == EtatFlotteur.Eau)
			{
				bouchonX = pecheur.Fx;
				bouchonY = pecheur.Fy;
			}
			etat.Banc.Pas(dt / 1000f, maintenant, bouchonX, bouchonY);
			float num3 = DessinerBouillon(resource, instance, num, num2, pecheur != null, maintenant, dt);
			etat.Banc.Dessiner(instance.X, instance.Y, 1f + 0.7f * num3);
			float num4 = ((etat.Repousse >= 0) ? Borne((float)(maintenant - etat.Repousse) / 900f) : 1f);
			if (resource.IsDead || pecheur != null || etat.Saut != null || etat.Banc.Poissons.Count == 0 || num4 < 1f || maintenant <= etat.ProchainSaut || maintenant - etat.Geste <= 400)
			{
				continue;
			}
			if (etat.ProchainSaut == 0L)
			{
				etat.ProchainSaut = maintenant + 1500 + Hasard.Next(2500);
				continue;
			}
			PoissonDuBanc poissonDuBanc = etat.Banc.Poissons[Hasard.Next(etat.Banc.Poissons.Count)];
			if (!(poissonDuBanc.Alpha < 0.9f))
			{
				int num5 = ((Hasard.Next(2) != 0) ? 1 : (-1));
				etat.Saut = new Saut
				{
					X0 = poissonDuBanc.X,
					Y0 = poissonDuBanc.Y,
					X1 = poissonDuBanc.X + (float)(num5 * 16),
					Ne = maintenant,
					Sens = num5
				};
				etat.ProchainSaut = maintenant + 2600 + Hasard.Next(1800);
				Ondes.Add(new Onde(resource.MapId, poissonDuBanc.X, poissonDuBanc.Y, maintenant, 450L, Poussiere: false, 1f));
				Ondes.Add(new Onde(resource.MapId, poissonDuBanc.X + (float)(num5 * 16), poissonDuBanc.Y, maintenant + 460, 450L, Poussiere: false, 1f));
			}
		}
	}

	private static void DessinerSautsEtPrises(long maintenant)
	{
		foreach (var (key, etat2) in Etats)
		{
			if ((etat2.Saut == null && etat2.Prise == null) || !Globals.Entities.TryGetValue(key, out Entity value) || !(value is Resource resource) || !MapInstance.TryGet(resource.MapId, out MapInstance instance))
			{
				continue;
			}
			Saut saut = etat2.Saut;
			if (saut != null)
			{
				long num = maintenant - saut.Ne;
				float num2 = (float)num / 460f;
				if (num2 >= 0f && num2 < 1f)
				{
					float num3 = (float)instance.X + saut.X0 + (saut.X1 - saut.X0) * num2;
					float num4 = (float)instance.Y + saut.Y0 - MathF.Sin((float)Math.PI * num2) * 22f;
					float angle = MathF.Atan2((0f - MathF.Cos((float)Math.PI * num2)) * (float)Math.PI * 22f, MathF.Abs(saut.X1 - saut.X0));
					DessinerPoisson(num3, num4, angle, saut.Sens < 0, maintenant);
					if (maintenant - saut.Goutte > 80)
					{
						saut.Goutte = maintenant;
						AjouterGoutte(resource.MapId, num3 - (float)instance.X - (float)(saut.Sens * 6), num4 - (float)instance.Y + 2f, (float)(-saut.Sens) * 0.2f, saut.Y0, maintenant);
					}
				}
				if (num > 1000)
				{
					etat2.Saut = null;
				}
			}
			Prise prise = etat2.Prise;
			if (prise == null)
			{
				continue;
			}
			long num5 = maintenant - prise.Ne;
			if (num5 < 0)
			{
				continue;
			}
			float num6 = (float)instance.X + prise.X0;
			float num7 = (float)instance.Y + prise.Y0 - 20f;
			if (prise.Pecheur != Guid.Empty && Globals.Entities.TryGetValue(prise.Pecheur, out Entity value2))
			{
				FloatRect bodyWorldPos = value2.BodyWorldPos;
				num6 = bodyWorldPos.X + bodyWorldPos.Width / 2f + 1f;
				num7 = bodyWorldPos.Y + bodyWorldPos.Height * 0.55f;
			}
			float num8 = (float)instance.X + prise.X0 + 6f;
			float num9 = (float)instance.Y + prise.Y0;
			float num10 = (float)num5 / 480f;
			if (num10 < 1f)
			{
				float num11 = num8 + (num6 - num8) * num10;
				float num12 = num9 + (num7 - num9) * num10 - MathF.Sin((float)Math.PI * num10) * 30f;
				float angle2 = MathF.Atan2(num7 - num9 - MathF.Cos((float)Math.PI * num10) * (float)Math.PI * 30f, MathF.Abs(num6 - num8) + 1f);
				DessinerPoisson(num11, num12, angle2, num6 < num8, maintenant);
				if (maintenant - prise.Goutte > 70)
				{
					prise.Goutte = maintenant;
					AjouterGoutte(resource.MapId, num11 - (float)instance.X, num12 - (float)instance.Y + 2f, 0f, prise.Y0, maintenant);
				}
			}
			else if (num5 < 840)
			{
				float angle3 = MathF.Sin((float)num5 / 45f) * 0.3f;
				DessinerPoisson(num6, num7 + MathF.Round(MathF.Sin((float)num5 / 60f)), angle3, gauche: false, maintenant);
			}
			else
			{
				etat2.Prise = null;
			}
		}
	}

	private static void AjouterGoutte(Guid mapId, float x, float y, float vx, float sol, long maintenant)
	{
		Eclats.Add(new Eclat
		{
			Genre = Genre.Goutte,
			MapId = mapId,
			X = x,
			Y = y,
			Vx = vx,
			Vy = 0.2f,
			Sol = sol,
			Couleur = CouleursGouttes[Hasard.Next(2)],
			Taille = 1,
			Ne = maintenant
		});
	}

	private static float Arrondi(float valeur)
	{
		return MathF.Floor(valeur + 0.5f);
	}

	private static float Lerp(float a, float b, float k)
	{
		return a + (b - a) * k;
	}

	private static float Lisse(float k)
	{
		k = Borne(k);
		return k * k * (3f - 2f * k);
	}

	private static float Sortie(float k)
	{
		return 1f - MathF.Pow(1f - Borne(k), 3f);
	}

	private static float Entree(float k)
	{
		return MathF.Pow(Borne(k), 3f);
	}

	private static float Depasse(float k, float force)
	{
		k = Borne(k) - 1f;
		return k * k * ((force + 1f) * k + force) + 1f;
	}

	private static float EcartAngle(float a, float b)
	{
		return MathF.Atan2(MathF.Sin(b - a), MathF.Cos(b - a));
	}

	private static float Distance3(float ax, float ay, float ah, float bx, float by, float bh)
	{
		float num = bx - ax;
		float num2 = by - ay;
		float num3 = bh - ah;
		return MathF.Sqrt(num * num + num2 * num2 + num3 * num3);
	}

	private static float Bruit(int x, int y)
	{
		uint num = (uint)(x * 374761393 + y * 668265263);
		num = (num ^ (num >> 13)) * 1274126177;
		return (float)(num ^ (num >> 16)) / 4.2949673E+09f;
	}

	private static void DessinerMatriceTournee(string[] lignes, float cx, float cy, Func<char, Color>? palette, Color teinteUnique, float x, float y, float angle, bool miroir, float alpha)
	{
		float num = MathF.Cos(angle);
		float num2 = MathF.Sin(angle);
		float num3 = Arrondi(x);
		float num4 = Arrondi(y);
		int length = lignes[0].Length;
		int num5 = lignes.Length;
		int num6 = (int)MathF.Ceiling(MathF.Sqrt(length * length + num5 * num5) / 2f) + 1;
		for (int i = -num6; i <= num6; i++)
		{
			for (int j = -num6; j <= num6; j++)
			{
				float num7 = (float)j + 0.5f;
				float num8 = (float)i + 0.5f;
				if (miroir)
				{
					num7 = 0f - num7;
				}
				int num9 = (int)MathF.Floor(num * num7 + num2 * num8 + cx);
				int num10 = (int)MathF.Floor((0f - num2) * num7 + num * num8 + cy);
				if (num9 >= 0 && num10 >= 0 && num9 < length && num10 < num5)
				{
					char c = lignes[num10][num9];
					if (c != '.')
					{
						Rect(num3 + (float)j, num4 + (float)i, 1f, 1f, Fondu(palette?.Invoke(c) ?? teinteUnique, alpha));
					}
				}
			}
		}
	}

	private static Masque? MasqueDe(IGameTexture? texture, FloatRect source)
	{
		if (texture == null || source.Width <= 0f || source.Height <= 0f)
		{
			return null;
		}
		(IGameTexture, int, int, int, int) key = (texture, (int)source.X, (int)source.Y, (int)source.Width, (int)source.Height);
		if (Masques.TryGetValue(key, out Masque value))
		{
			return value;
		}
		value = null;
		try
		{
			Texture2D texture2 = texture.GetTexture<Texture2D>();
			if (texture2 != null)
			{
				AtlasReference atlasReference = texture.AtlasReference;
				if ((object)atlasReference == null || !atlasReference.IsRotated)
				{
					int num = key.Item2;
					int num2 = key.Item3;
					AtlasReference atlasReference2 = texture.AtlasReference;
					if ((object)atlasReference2 != null)
					{
						num += atlasReference2.Bounds.X;
						num2 += atlasReference2.Bounds.Y;
					}
					Microsoft.Xna.Framework.Color[] array = new Microsoft.Xna.Framework.Color[key.Item4 * key.Item5];
					texture2.GetData(0, new Microsoft.Xna.Framework.Rectangle(num, num2, key.Item4, key.Item5), array, 0, array.Length);
					byte[] array2 = new byte[array.Length];
					Color[] array3 = new Color[array.Length];
					for (int i = 0; i < array.Length; i++)
					{
						array2[i] = array[i].A;
						array3[i] = new Color(255, array[i].R, array[i].G, array[i].B);
					}
					value = new Masque
					{
						Largeur = key.Item4,
						Hauteur = key.Item5,
						Alphas = array2,
						Couleurs = array3
					};
				}
			}
		}
		catch
		{
			value = null;
		}
		if (value == null)
		{
			return null;
		}
		if (Masques.Count > 64)
		{
			Masques.Clear();
		}
		Masques[key] = value;
		return value;
	}

	private static float? BordOpaque(Resource ressource, float yMonde, int sens)
	{
		FloatRect renderDestination = ressource.RenderDestination;
		Masque masque = MasqueDe(ressource.Texture, ressource.RenderSource);
		if (masque == null)
		{
			return null;
		}
		int num = (int)MathF.Floor(yMonde - renderDestination.Y);
		if (num < 0 || num >= masque.Hauteur)
		{
			return null;
		}
		int num2 = masque.Largeur / 2;
		int num3 = -1;
		for (int i = 0; i <= 8; i++)
		{
			if (num3 >= 0)
			{
				break;
			}
			if (masque.Alpha(num2 + i, num) >= 235)
			{
				num3 = num2 + i;
			}
			else if (masque.Alpha(num2 - i, num) >= 235)
			{
				num3 = num2 - i;
			}
		}
		if (num3 < 0)
		{
			return null;
		}
		int j;
		for (j = num3; masque.Alpha(j + sens, num) >= 235; j += sens)
		{
		}
		return renderDestination.X + (float)j + (float)((sens > 0) ? 1 : 0);
	}

	public static bool DessinerRessource(Resource ressource, IGameTexture texture, FloatRect source, FloatRect destination)
	{
		if (!Etats.TryGetValue(ressource.Id, out Etat value))
		{
			if (ressource.IsDead || Metier(ressource) != 0)
			{
				return false;
			}
			value = EtatDe(ressource);
		}
		if (_pixel == null)
		{
			_pixel = Graphics.Renderer?.WhitePixel;
		}
		long milliseconds = Timing.Global.Milliseconds;
		int num = Metier(ressource);
		if (num == 1 && ressource.IsDead)
		{
			return DessinerRocherBrise(ressource, value, texture, source, destination, milliseconds);
		}
		float num2 = ((value.Repousse >= 0) ? Borne((float)(milliseconds - value.Repousse) / 900f) : 1f);
		if (num == 0 && !ressource.IsDead && (DessinerArbreAuVent(ressource, value, texture, source, destination, num2) || DessinerFrisson(value, texture, source, destination, num2)))
		{
			return true;
		}
		if (num2 < 1f && ressource.TrouverImageEpuisee(out IGameTexture texture2, out FloatRect source2, out FloatRect destination2))
		{
			Graphics.DrawGameTexture(texture2, source2, destination2, Fondu(Color.White, 1f - num2));
		}
		if (num == 1)
		{
			if (num2 < 1f)
			{
				Graphics.DrawGameTexture(texture, source, destination, Fondu(Color.White, num2));
				return true;
			}
			return DessinerRocher(ressource, value, texture, source, destination, 1f, milliseconds);
		}
		long num3 = milliseconds - value.Secousse;
		bool flag = !ressource.IsDead;
		bool flag2 = flag;
		if (flag2)
		{
			bool flag3 = ((num == 0 || num == 2) ? true : false);
			flag2 = flag3;
		}
		float num4 = ((flag2 && num3 < 180) ? MathF.Round(MathF.Sin((float)num3 / 180f * (float)Math.PI * 4f) * 2f * (1f - (float)num3 / 180f)) : 0f);
		if (num2 >= 1f && num4 == 0f)
		{
			return false;
		}
		FloatRect targetRect = new FloatRect(destination.X + num4, destination.Y, destination.Width, destination.Height);
		Graphics.DrawGameTexture(texture, source, targetRect, (num2 >= 1f) ? Color.White : Fondu(Color.White, num2));
		return true;
	}

	private static bool DessinerArbreAuVent(Resource ressource, Etat etat, IGameTexture texture, FloatRect source, FloatRect destination, float repousse)
	{
		if (!Vent.Actif || Vent.EnPanne || !MapInstance.TryGet(ressource.MapId, out MapInstance instance))
		{
			return false;
		}
		(IGameTexture, int, int, int, int) key = (texture, (int)source.X, (int)source.Y, (int)source.Width, (int)source.Height);
		if (!ArbresRecolte.TryGetValue(key, out ModeleArbre modele))
		{
			modele = ModeleArbre.ConstruireZone(texture.Name ?? "recolte", texture, key.Item2, key.Item3, key.Item4, key.Item5);
			ArbresRecolte[key] = modele;
		}
		if (modele == null)
		{
			return false;
		}
		try
		{
			int origineX = (int)MathF.Floor(destination.X / (float)TailleCase);
			int origineY = (int)MathF.Floor(destination.Y / (float)HauteurCase);
			int niveau = Vent.NiveauCarte(instance);
			Vent.ArbreVivant arbreVivant = Vent.Arbre(modele, origineX, origineY, niveau);
			double age = Vent.Temps - etat.Frisson;
			float enveloppe = ((age >= 0.0 && age <= 0.55) ? ((float)Math.Pow(1.0 - age / 0.55, 2.0)) : 0f);
			float decalage = arbreVivant.Position[0] + Frisson(1.8f, 0.05);
			float decalage2 = arbreVivant.Position[1] + Frisson(1.3f, 0.025);
			float decalage3 = arbreVivant.Position[2] + Frisson(0.8f, 0.0);
			if (repousse < 1f && ressource.TrouverImageEpuisee(out IGameTexture texture2, out FloatRect source2, out FloatRect destination2))
			{
				Graphics.DrawGameTexture(texture2, source2, destination2, Fondu(Color.White, 1f - repousse));
			}
			Color teinte = ((repousse >= 1f) ? Color.White : Fondu(Color.White, repousse));
			Calque(3, 0f);
			Calque(4, 0f);
			Calque(5, 0f);
			Calque(2, decalage3);
			Calque(1, decalage2);
			Calque(0, decalage);
			int num = Vent.Reflet(modele, arbreVivant, origineX, niveau);
			if (num >= 0)
			{
				Calque(12 + num, decalage2);
				Calque(6 + num, decalage);
			}
			return true;
			void Calque(int calque, float num2)
			{
				FloatRect srcRectangle = new FloatRect(calque * modele.Largeur, 0f, modele.Largeur, modele.Hauteur);
				if (num2 == 0f)
				{
					Graphics.DrawGameTexture(modele.Atlas, srcRectangle, destination, teinte);
				}
				else
				{
					Vent.Glisse(num2, out var gauche, out var droite, out var part);
					float num6;
					float num7;
					float num8;
					if (!(part <= 0.5f))
					{
						float num3 = droite;
						float num4 = gauche;
						float num5 = 1f - part;
						num6 = num5;
						num7 = num4;
						num8 = num3;
					}
					else
					{
						float num9 = gauche;
						float num5 = droite;
						float num4 = part;
						num6 = num4;
						num7 = num5;
						num8 = num9;
					}
					Graphics.DrawGameTexture(modele.Atlas, srcRectangle, new FloatRect(destination.X + num8, destination.Y, destination.Width, destination.Height), teinte);
					if (num6 >= 1f / 64f)
					{
						Graphics.DrawGameTexture(modele.Atlas, srcRectangle, new FloatRect(destination.X + num7, destination.Y, destination.Width, destination.Height), Fondu(teinte, num6));
					}
				}
			}
			float Frisson(float ampleur, double retard)
			{
				if (enveloppe != 0f)
				{
					return (float)etat.SensFrisson * etat.ForceFrisson * ampleur * enveloppe * (float)Math.Sin(13.823007675795091 * Math.Max(0.0, age - retard));
				}
				return 0f;
			}
		}
		catch (Exception exception)
		{
			Vent.Panne(exception);
			return false;
		}
	}
}
