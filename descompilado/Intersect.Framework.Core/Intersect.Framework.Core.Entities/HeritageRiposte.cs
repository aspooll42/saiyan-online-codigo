using System;
using Intersect.Config;

namespace Intersect.Framework.Core.Entities;

public static class HeritageRiposte
{
	public static bool Actif
	{
		get
		{
			if (HeritageRegles.Actif)
			{
				HeritageOptions heritage = Options.Instance.Combat.Heritage;
				if (heritage != null)
				{
					HeritageEtatsOptions etats = heritage.Etats;
					if (etats != null && etats.Actifs)
					{
						HeritageRiposteOptions riposte = heritage.Riposte;
						if (riposte != null && riposte.Actif && riposte.CoupsAvant > 0)
						{
							return riposte.TenaceMs > 0;
						}
					}
				}
				return false;
			}
			return false;
		}
	}

	public static int DureeDArmement(int armeMs, int plancherMs)
	{
		return Math.Max(Math.Max(0, plancherMs), armeMs);
	}

	public static bool Touche(long pvAvant, long pvApres)
	{
		return pvApres < pvAvant;
	}

	public static bool Lance(CoupHeritage coup, bool projeteeParCeJoueur, long projectionFin, long maintenant)
	{
		if (coup == CoupHeritage.CoupDePied && projeteeParCeJoueur)
		{
			return projectionFin >= maintenant;
		}
		return false;
	}
}
