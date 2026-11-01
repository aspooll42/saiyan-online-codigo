using System;

namespace Intersect.Framework.Core.Entities;

public sealed class HeritageTenacite
{
	private int _compte;

	private long _dernierCoupA;

	public long Debut;

	public long Fin;

	public bool EnAttente;

	public bool RiposteArmee;

	public int Compte => _compte;

	public bool Tenace(long maintenant)
	{
		if (Fin != 0L && maintenant >= Debut)
		{
			return maintenant < Fin;
		}
		return false;
	}

	public bool PeutRiposter(long maintenant)
	{
		if (Tenace(maintenant))
		{
			return !RiposteArmee;
		}
		return false;
	}

	public DebutDeTenacite Coup(long maintenant, bool lance, int filMs, int coupsAvant, int tenaceMs)
	{
		if (coupsAvant <= 0 || tenaceMs <= 0 || EnAttente || Tenace(maintenant))
		{
			return DebutDeTenacite.Aucun;
		}
		if (_compte > 0 && maintenant - _dernierCoupA > Math.Max(0, filMs))
		{
			_compte = 0;
		}
		_dernierCoupA = maintenant;
		if (++_compte < coupsAvant)
		{
			return DebutDeTenacite.Aucun;
		}
		_compte = 0;
		RiposteArmee = false;
		if (lance)
		{
			EnAttente = true;
			Debut = 0L;
			Fin = 0L;
			return DebutDeTenacite.Debout;
		}
		Debut = maintenant;
		Fin = maintenant + tenaceMs;
		return DebutDeTenacite.Maintenant;
	}

	public bool Commencer(long maintenant, int tenaceMs)
	{
		if (!EnAttente)
		{
			return false;
		}
		EnAttente = false;
		Debut = maintenant;
		Fin = maintenant + Math.Max(0, tenaceMs);
		return Fin > Debut;
	}

	public void Armer()
	{
		RiposteArmee = true;
	}

	public bool Finir(long maintenant)
	{
		bool result = EnAttente || Tenace(maintenant);
		EnAttente = false;
		if (Tenace(maintenant))
		{
			Fin = maintenant;
		}
		_compte = 0;
		return result;
	}
}
