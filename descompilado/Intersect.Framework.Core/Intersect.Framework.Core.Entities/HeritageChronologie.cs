using System;

namespace Intersect.Framework.Core.Entities;

public static class HeritageChronologie
{
	public static bool Attend(CoupHeritage coup, int attenteMs)
	{
		if (HeritageRegles.Table(coup).ImpactMs > 0)
		{
			return attenteMs > 0;
		}
		return false;
	}

	public static SortALaReception ALaReception(bool briseParUnChoc, CoupHeritage coup, int attenteMs)
	{
		if (!briseParUnChoc)
		{
			if (!Attend(coup, attenteMs))
			{
				return SortALaReception.Frappe;
			}
			return SortALaReception.Pose;
		}
		return SortALaReception.Brise;
	}

	public static long HeureDImpact(long recuA, int attenteMs)
	{
		return recuA + Math.Max(0, attenteMs);
	}

	public static long CadencePendantLAttente(long cadence, long impactA)
	{
		return Math.Max(cadence, impactA);
	}

	public static bool EstDu(long impactA, long maintenant)
	{
		if (impactA != 0L)
		{
			return maintenant >= impactA;
		}
		return false;
	}

	public static long Avancer(long impactA, long maintenant)
	{
		if (impactA != 0L)
		{
			return Math.Max(1L, Math.Min(impactA, maintenant));
		}
		return 0L;
	}

	public static SortDuCoup Sort(bool joueurMort, bool cibleLa, bool etatSubi, long cadence, long attenteJusqua, bool gardeLevee)
	{
		if (joueurMort)
		{
			return SortDuCoup.Rien;
		}
		if (!cibleLa)
		{
			return SortDuCoup.SansCible;
		}
		if (etatSubi)
		{
			return SortDuCoup.RefusSonne;
		}
		if (cadence > attenteJusqua)
		{
			return SortDuCoup.RefusEtat;
		}
		if (!gardeLevee)
		{
			return SortDuCoup.Frappe;
		}
		return SortDuCoup.Abandonne;
	}

	public static long CadenceAvantLaFrappe(long cadence, long maintenant)
	{
		return Math.Min(cadence, maintenant);
	}

	public static long CadenceSansFrappe(long cadence, long finA)
	{
		return Math.Max(cadence, finA);
	}
}
