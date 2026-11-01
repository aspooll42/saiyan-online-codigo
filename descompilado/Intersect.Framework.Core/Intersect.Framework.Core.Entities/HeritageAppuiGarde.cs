using System;

namespace Intersect.Framework.Core.Entities;

public static class HeritageAppuiGarde
{
	public const int MargeDuTourMs = 60;

	public const int TropTardPourLeDireMs = 1000;

	public static long FinDuCoupEnCours(long cadence, long finDuCoupPose)
	{
		return Math.Max(cadence, finDuCoupPose);
	}

	public static bool PeutGarder(int memoireMs, bool rejeuEnCours, bool cibleDeLHeritage, long finDuCoupEnCours, long maintenant)
	{
		if (memoireMs > 0 && !rejeuEnCours && cibleDeLHeritage)
		{
			return finDuCoupEnCours - maintenant <= memoireMs;
		}
		return false;
	}

	public static bool EstPerime(long gardeA, long maintenant, int memoireMs)
	{
		return maintenant - gardeA > Math.Max(0, memoireMs) + 60;
	}

	public static bool RefusDit(long gardeA, long maintenant, int memoireMs, bool etatSubi)
	{
		if (!etatSubi)
		{
			return maintenant - gardeA <= Math.Max(0, memoireMs) + 60 + 1000;
		}
		return false;
	}

	public static SortDeLAppui Sort(bool garde, long gardeA, long maintenant, int memoireMs, bool etatSubi, bool coupEnAttente, bool attaque)
	{
		if (!garde)
		{
			return SortDeLAppui.Rien;
		}
		if (EstPerime(gardeA, maintenant, memoireMs))
		{
			if (!RefusDit(gardeA, maintenant, memoireMs, etatSubi))
			{
				return SortDeLAppui.PerimeMuet;
			}
			return SortDeLAppui.PerimeDit;
		}
		if (!(coupEnAttente || attaque))
		{
			return SortDeLAppui.Rejoue;
		}
		return SortDeLAppui.Attend;
	}

	public static int AttenteApresLaGarde(int attenteMs, long gardeMs)
	{
		return (int)Math.Max(0L, attenteMs - Math.Max(0L, gardeMs));
	}
}
