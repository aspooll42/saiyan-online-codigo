namespace Intersect.Framework.Core.Entities;

public sealed class HeritageFil
{
	private const int NombreDEtats = 6;

	public long OuvertJusqua;

	public long RepitJusqua;

	private readonly long[] _fin = new long[6];

	private byte _donnes;

	public void Toucher(long maintenant, int filMs, int repitMs)
	{
		if (OuvertJusqua != 0L && maintenant > OuvertJusqua)
		{
			RepitJusqua = OuvertJusqua + repitMs;
			_donnes = 0;
		}
		OuvertJusqua = maintenant + filMs;
	}

	public bool EnRepit(long maintenant, int repitMs)
	{
		if (maintenant >= RepitJusqua)
		{
			if (OuvertJusqua != 0L && maintenant > OuvertJusqua)
			{
				return maintenant < OuvertJusqua + repitMs;
			}
			return false;
		}
		return true;
	}

	public bool Prendre(EtatHeritage etat, long maintenant, int dureeMs, int filMs, int repitMs)
	{
		Toucher(maintenant, filMs, repitMs);
		int num = Rang(etat);
		if (num == 0 || EnRepit(maintenant, repitMs))
		{
			return false;
		}
		byte b = (byte)(1 << num);
		if ((_donnes & b) != 0)
		{
			return false;
		}
		_donnes |= b;
		_fin[num] = maintenant + dureeMs;
		return true;
	}

	public bool Vif(EtatHeritage etat, long maintenant, int toleranceMs)
	{
		long num = Fin(etat);
		if (num != 0L)
		{
			return maintenant < num + toleranceMs;
		}
		return false;
	}

	public long Fin(EtatHeritage etat)
	{
		return _fin[Rang(etat)];
	}

	public void Finir(EtatHeritage etat)
	{
		int num = Rang(etat);
		if (num != 0)
		{
			_fin[num] = 0L;
		}
	}

	public FormeDeLancer Formes(long maintenant, int toleranceMs)
	{
		FormeDeLancer formeDeLancer = FormeDeLancer.Simple;
		if (Vif(EtatHeritage.Repousse, maintenant, toleranceMs))
		{
			formeDeLancer |= FormeDeLancer.Long;
		}
		if (Vif(EtatHeritage.Flanc, maintenant, toleranceMs))
		{
			formeDeLancer |= FormeDeLancer.Critique;
		}
		if (Vif(EtatHeritage.Souleve, maintenant, toleranceMs))
		{
			formeDeLancer |= FormeDeLancer.Cueilli;
		}
		return formeDeLancer;
	}

	private static int Rang(EtatHeritage etat)
	{
		if ((int)etat <= 0 || (int)etat > 5)
		{
			return 0;
		}
		return (int)etat;
	}
}
