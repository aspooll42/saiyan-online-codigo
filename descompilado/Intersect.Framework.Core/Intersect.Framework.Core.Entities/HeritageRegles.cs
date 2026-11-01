using System;
using Intersect.Config;
using Intersect.Enums;

namespace Intersect.Framework.Core.Entities;

public static class HeritageRegles
{
	public const int RetraitServeurMs = 60;

	public const byte TousLesEtats = byte.MaxValue;

	public const int LancerPlancherMs = 100;

	public const float EcartVerticalPx = 40f;

	private static readonly CoupHeritage[] CoupsAEcart = new CoupHeritage[6]
	{
		CoupHeritage.Direct,
		CoupHeritage.Crochet,
		CoupHeritage.CoupDEpaule,
		CoupHeritage.Montant,
		CoupHeritage.CoupDePied,
		CoupHeritage.FaucheHoraire
	};

	private const float SensMin = 0.001f;

	public static bool Actif
	{
		get
		{
			Options instance = Options.Instance;
			if ((object)instance != null)
			{
				CombatOptions combat = instance.Combat;
				if (combat != null && combat.ComboHits >= 2)
				{
					HeritageOptions heritage = combat.Heritage;
					if (heritage != null)
					{
						return heritage.Actif;
					}
				}
			}
			return false;
		}
	}

	private static HeritageOptions Reglages => Options.Instance.Combat.Heritage;

	public static bool SurtoutVertical(float ecartX, float ecartY)
	{
		return MathF.Abs(ecartY) > MathF.Abs(ecartX);
	}

	public static float EcartDuCoup(CoupHeritage coup, float ecartX, float ecartY)
	{
		int ecartPx = Table(coup).EcartPx;
		if (ecartPx <= 0 || !SurtoutVertical(ecartX, ecartY))
		{
			return ecartPx;
		}
		return MathF.Max(ecartPx, 40f);
	}

	public static float AllongeVerticale(CoupHeritage coup, float ecartX, float ecartY)
	{
		return EcartDuCoup(coup, ecartX, ecartY) - (float)Table(coup).EcartPx;
	}

	public static float AllongeVerticaleMax(float ecartX, float ecartY)
	{
		if (!SurtoutVertical(ecartX, ecartY))
		{
			return 0f;
		}
		float num = 0f;
		CoupHeritage[] coupsAEcart = CoupsAEcart;
		foreach (CoupHeritage coup in coupsAEcart)
		{
			num = MathF.Max(num, AllongeVerticale(coup, ecartX, ecartY));
		}
		return num;
	}

	public static CoupHeritageOptions Table(CoupHeritage coup)
	{
		CoupsHeritageOptions coups = Reglages.Coups;
		switch (coup)
		{
		case CoupHeritage.Crochet:
			return coups.Crochet;
		case CoupHeritage.CoupDEpaule:
			return coups.CoupDEpaule;
		case CoupHeritage.Montant:
			return coups.Montant;
		case CoupHeritage.CoupDePied:
			return coups.CoupDePied;
		case CoupHeritage.FaucheHoraire:
		case CoupHeritage.FaucheAntihoraire:
			return coups.Fauche;
		default:
			return coups.Direct;
		}
	}

	public static bool EstUneFauche(CoupHeritage coup)
	{
		if (coup - 6 <= CoupHeritage.Direct)
		{
			return true;
		}
		return false;
	}

	public static bool EstADirection(CoupHeritage coup)
	{
		if (coup - 3 <= CoupHeritage.Direct || coup - 6 <= CoupHeritage.Direct)
		{
			return true;
		}
		return false;
	}

	public static bool EstUnCoupJoue(byte octet)
	{
		if (octet >= 1)
		{
			return octet <= 7;
		}
		return false;
	}

	public static CoupHeritage Demande(byte octet, int niveau)
	{
		CoupHeritage coupHeritage = (CoupHeritage)(octet & 0xF);
		if (!EstADirection(coupHeritage))
		{
			return CoupHeritage.Aucun;
		}
		if (niveau < Table(coupHeritage).NiveauRequis)
		{
			return CoupHeritage.Aucun;
		}
		return coupHeritage;
	}

	public static int Avance(byte octet)
	{
		return (octet >> 4) * 4;
	}

	public static byte Octet(CoupHeritage demande, int avanceMs)
	{
		return (byte)((uint)(demande & (CoupHeritage)15) | (uint)(Math.Clamp(avanceMs / 4, 0, 15) << 4));
	}

	public static (byte Pas, CoupHeritage Coup) Suite(byte rangBrut, CoupHeritage demande, int comboHits)
	{
		if (EstADirection(demande))
		{
			return (Pas: (byte)Math.Max(1, Math.Min(rangBrut, comboHits - 1)), Coup: demande);
		}
		CoupHeritage item = ((rangBrut >= comboHits) ? CoupHeritage.CoupDePied : ((rangBrut <= 1) ? CoupHeritage.Direct : CoupHeritage.Crochet));
		return (Pas: rangBrut, Coup: item);
	}

	public static int Duree(CoupHeritage coup, int tempsDeBaseServeur)
	{
		CoupHeritageOptions coupHeritageOptions = Table(coup);
		return Math.Max(coupHeritageOptions.PlancherMs, (int)Math.Round((double)(tempsDeBaseServeur * coupHeritageOptions.DureePourcent) / 100.0));
	}

	public static int DureeClient(CoupHeritage coup, int tempsDeBaseClient)
	{
		return Duree(coup, tempsDeBaseClient - 60) + Reglages.MargeClientMs;
	}

	public static int AvanceBornee(CoupHeritage coup, int avanceMs)
	{
		return Math.Clamp(avanceMs, 0, Math.Max(0, Math.Min(Reglages.ArmementPendantLAppuiMs, Table(coup).ImpactMs - 16)));
	}

	public static int AttenteDImpact(CoupHeritage coup, int pingMs, int avanceMs)
	{
		int impactMs = Table(coup).ImpactMs;
		int value = impactMs - Math.Min(Math.Max(0, pingMs), Reglages.CompensationImpactMaxMs) - AvanceBornee(coup, avanceMs);
		return Math.Clamp(value, 0, Math.Max(0, impactMs));
	}

	public static SensDeDirection Sens(Direction touche, float ecartX, float ecartY)
	{
		var (num, num2) = CorpsACorps.Vecteur(touche);
		if (num == 0f && num2 == 0f)
		{
			return SensDeDirection.Aucun;
		}
		bool flag = MathF.Abs(ecartX) >= MathF.Abs(ecartY);
		float num3 = (flag ? num : num2);
		if (num3 == 0f)
		{
			return SensDeDirection.DeCote;
		}
		float num4 = (((flag ? ecartX : ecartY) < 0f) ? (-1f) : 1f);
		if (!(num3 * num4 > 0f))
		{
			return SensDeDirection.Oppose;
		}
		return SensDeDirection.VersLui;
	}

	public static CoupHeritage FaucheDuCote(Direction touche, float ecartX, float ecartY)
	{
		if (Sens(touche, ecartX, ecartY) != SensDeDirection.DeCote)
		{
			return CoupHeritage.Aucun;
		}
		(float X, float Y) tuple = CorpsACorps.Vecteur(touche);
		float item = tuple.X;
		float item2 = tuple.Y;
		bool flag = MathF.Abs(ecartX) >= MathF.Abs(ecartY);
		float num = ((!flag) ? 0f : ((ecartX < 0f) ? (-1f) : 1f));
		float num2 = (flag ? 0f : ((ecartY < 0f) ? (-1f) : 1f));
		float num3 = (0f - num) * item2 + num2 * item;
		if (!(num3 > 0f))
		{
			return CoupHeritage.FaucheAntihoraire;
		}
		return CoupHeritage.FaucheHoraire;
	}

	public static (float X, float Y) QuartDeTour(CoupHeritage fauche, float x, float y)
	{
		return fauche switch
		{
			CoupHeritage.FaucheHoraire => (X: 0f - y, Y: x), 
			CoupHeritage.FaucheAntihoraire => (X: y, Y: 0f - x), 
			_ => (X: x, Y: y), 
		};
	}

	public static bool PeutEtrePoussee(bool pnj, bool vivante, bool lourde, bool gardeLevee, bool immuniseeAuRecul, bool immobile)
	{
		if (pnj && vivante && !lourde && !gardeLevee && !immuniseeAuRecul)
		{
			return !immobile;
		}
		return false;
	}

	public static bool PeutEtreSoulevee(bool pnj, bool vivante, bool lourde, bool gardeLevee, bool armeUnImparable, bool protegee, bool dejaSouleveeCetteSerie, bool enRepit)
	{
		if (pnj && vivante && !lourde && !gardeLevee && !armeUnImparable && !dejaSouleveeCetteSerie && !enRepit)
		{
			if (protegee)
			{
				return Reglages.SouleveMalgreLaProtection;
			}
			return true;
		}
		return false;
	}

	public static (float X, float Y, float Longueur) Recul(CoupHeritage coup, float sensX, float sensY, bool massif)
	{
		float num = MathF.Sqrt(sensX * sensX + sensY * sensY);
		if (!(num > 0.001f))
		{
			return (X: 0f, Y: 0f, Longueur: 0f);
		}
		sensX /= num;
		sensY /= num;
		CoupHeritageOptions coupHeritageOptions = Table(coup);
		if (massif)
		{
			return (X: sensX, Y: sensY, Longueur: coupHeritageOptions.ReculLourdPx);
		}
		if (coupHeritageOptions.ReculCotePx <= 0)
		{
			return (X: sensX, Y: sensY, Longueur: coupHeritageOptions.ReculPx);
		}
		float num2 = 0f - sensY;
		float num3 = sensX;
		if (num3 < 0f || (num3 == 0f && num2 < 0f))
		{
			num2 = 0f - num2;
			num3 = 0f - num3;
		}
		float num4 = (float)coupHeritageOptions.ReculPx * sensX + (float)coupHeritageOptions.ReculCotePx * num2;
		float num5 = (float)coupHeritageOptions.ReculPx * sensY + (float)coupHeritageOptions.ReculCotePx * num3;
		float num6 = MathF.Sqrt(num4 * num4 + num5 * num5);
		return (X: num4 / num6, Y: num5 / num6, Longueur: num6);
	}

	public static (int Px, int Ms) Lancer(FormeDeLancer forme, int simplePx, int simpleMs)
	{
		if ((forme & FormeDeLancer.Long) == 0)
		{
			return (Px: simplePx, Ms: simpleMs);
		}
		HeritageEtatsOptions etats = Reglages.Etats;
		return (Px: etats.LancerLongPx, Ms: Math.Max(100, etats.LancerLongMs));
	}

	private static float Freinee(float t)
	{
		return 1f - (1f - t) * (1f - t);
	}

	public static float PartDuTemps(float partDuTrajet)
	{
		if (!(partDuTrajet > 0f))
		{
			return 0f;
		}
		if (!(partDuTrajet >= 1f))
		{
			return partDuTrajet / (1f + MathF.Sqrt(1f - partDuTrajet));
		}
		return 1f;
	}

	public static float Progression(float t, float partDuTrajet)
	{
		t = Math.Clamp(t, 0f, 1f);
		if (!(partDuTrajet > 0f) || partDuTrajet >= 1f)
		{
			return Freinee(t);
		}
		if (t >= 1f)
		{
			return 1f;
		}
		float num = t * PartDuTemps(partDuTrajet);
		return MathF.Min(1f, num * (2f - num) / partDuTrajet);
	}

	public static int DureeCoupee(int dureeMs, float partDuTrajet, int plancherMs)
	{
		return Math.Max(plancherMs, (int)Math.Round((double)dureeMs * (double)PartDuTemps(partDuTrajet)));
	}

	public static int PartDuDecor(MatiereDArret matiere)
	{
		HeritageDecorOptions decor = Reglages.Decor;
		return matiere switch
		{
			MatiereDArret.Arbre => decor.PartArbre, 
			MatiereDArret.Rocher => decor.PartRocher, 
			MatiereDArret.Falaise => decor.PartFalaise, 
			MatiereDArret.Corps => decor.PartCorps, 
			MatiereDArret.Petit => decor.PartPetit, 
			MatiereDArret.Sol => decor.PartSol, 
			_ => 0, 
		};
	}

	public static long DegatsDArret(long degatsDUnDirect, MatiereDArret matiere)
	{
		if (degatsDUnDirect <= 0)
		{
			return 0L;
		}
		double num = (double)PartDuDecor(matiere) * (double)Reglages.Decor.PartGlobalePourcent;
		return (long)Math.Round((double)degatsDUnDirect * num / 10000.0, MidpointRounding.AwayFromZero);
	}

	public static bool EstDur(MatiereDArret matiere)
	{
		if (matiere - 1 <= MatiereDArret.Rocher)
		{
			return true;
		}
		return false;
	}

	public static byte OctetDeLien(bool lie, Direction tenue)
	{
		int num = (int)((tenue >= Direction.Up && tenue <= Direction.DownLeft) ? (tenue + 1) : Direction.Up);
		return (byte)((lie ? 1u : 0u) | (uint)(num << 1));
	}

	public static bool EstLie(byte octet)
	{
		return (octet & 1) != 0;
	}

	public static Direction DirectionTenue(byte octet)
	{
		int num = (octet >> 1) & 0xF;
		if (num < 1 || num > 8)
		{
			return Direction.None;
		}
		return (Direction)(num - 1);
	}

	public static MomentHeritage Moments(bool repousse, bool flanc, bool souleve, bool plante, bool lance, FormeDeLancer consommee)
	{
		MomentHeritage momentHeritage = MomentHeritage.Aucun;
		if (repousse || (consommee & FormeDeLancer.Long) != FormeDeLancer.Simple)
		{
			momentHeritage |= MomentHeritage.Boule;
		}
		if (souleve || (consommee & FormeDeLancer.Cueilli) != FormeDeLancer.Simple)
		{
			momentHeritage |= MomentHeritage.Kiai;
		}
		if (flanc || (consommee & FormeDeLancer.Critique) != FormeDeLancer.Simple)
		{
			momentHeritage |= MomentHeritage.Onde;
		}
		if (lance || plante)
		{
			momentHeritage |= MomentHeritage.Signature;
		}
		return momentHeritage;
	}

	public static bool LienValable(long maintenant, long coupPorteA, int memoireMs, int toleranceMs)
	{
		if (coupPorteA > 0)
		{
			return maintenant - coupPorteA <= memoireMs + toleranceMs;
		}
		return false;
	}

	public static int DepartDuLien(int tenueMs, int arretMs, int departMs)
	{
		if (departMs > 0)
		{
			return Math.Clamp(departMs, Math.Min(arretMs, tenueMs), tenueMs);
		}
		return tenueMs;
	}

	public static bool EstLaBoule(Guid sortId)
	{
		if (sortId != Guid.Empty && Guid.TryParse(Reglages.Techniques.BouleSortId, out var result))
		{
			return result == sortId;
		}
		return false;
	}

	public static byte OctetDuKiai(Direction sens, bool plein)
	{
		int num = (int)((sens >= Direction.Up && sens <= Direction.DownLeft) ? (sens + 1) : Direction.Up);
		return (byte)(num | (plein ? 128 : 0));
	}

	public static Direction SensDuKiai(byte octet)
	{
		int num = octet & 0xF;
		if (num < 1 || num > 8)
		{
			return Direction.None;
		}
		return (Direction)(num - 1);
	}

	public static bool KiaiPlein(byte octet)
	{
		return (octet & 0x80) != 0;
	}

	public static int PalierDuRayon(long chargeMs, int palier2Ms, int palier3Ms)
	{
		if (chargeMs < palier3Ms)
		{
			return (chargeMs >= palier2Ms) ? 1 : 0;
		}
		return 2;
	}

	public static float FacteurDuRayon(int pourcentDuPalier, bool plein, int pleinPourcent)
	{
		return Math.Clamp((float)pourcentDuPalier / 100f * (plein ? ((float)pleinPourcent / 100f) : 1f), 0.01f, 1.5f);
	}
}
