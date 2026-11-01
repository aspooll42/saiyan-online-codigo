namespace Intersect.Config;

public class CoupsHeritageOptions
{
	public CoupHeritageOptions Direct { get; set; } = DirectDeDepart();

	public CoupHeritageOptions Crochet { get; set; } = CrochetDeDepart();

	public CoupHeritageOptions CoupDEpaule { get; set; } = CoupDEpauleDeDepart();

	public CoupHeritageOptions Montant { get; set; } = MontantDeDepart();

	public CoupHeritageOptions CoupDePied { get; set; } = CoupDePiedDeDepart();

	public CoupHeritageOptions Fauche { get; set; } = FaucheDeDepart();

	private static CoupHeritageOptions DirectDeDepart()
	{
		return new CoupHeritageOptions
		{
			NiveauRequis = 1,
			DureePourcent = 90,
			PlancherMs = 280,
			ImpactMs = 60,
			TenueMs = 100,
			ArmementTenuMs = 0,
			SansMarcheMs = 0,
			DegatsPourcent = 100,
			KiPourcent = 10,
			ReculPx = 16,
			ReculMs = 140,
			ReculCotePx = 0,
			ReculLourdPx = 5,
			PousseePredite = false,
			ElanPx = 40,
			ElanMs = 60,
			EcartPx = 28,
			AllongePx = 0,
			ArretMs = 50,
			SecousseDixiemes = 20,
			SecousseMs = 120,
			SonCentiemes = 11,
			Souffle = true,
			SouffleCentiemes = 14
		};
	}

	private static CoupHeritageOptions CrochetDeDepart()
	{
		return new CoupHeritageOptions
		{
			NiveauRequis = 1,
			DureePourcent = 90,
			PlancherMs = 280,
			ImpactMs = 110,
			TenueMs = 100,
			ArmementTenuMs = 0,
			SansMarcheMs = 0,
			DegatsPourcent = 100,
			KiPourcent = 10,
			ReculPx = 12,
			ReculMs = 140,
			ReculCotePx = 9,
			ReculLourdPx = 5,
			PousseePredite = false,
			ElanPx = 40,
			ElanMs = 110,
			EcartPx = 28,
			AllongePx = 0,
			ArretMs = 50,
			SecousseDixiemes = 20,
			SecousseMs = 120,
			SonCentiemes = -32,
			Souffle = true,
			SouffleCentiemes = 4
		};
	}

	private static CoupHeritageOptions CoupDEpauleDeDepart()
	{
		return new CoupHeritageOptions
		{
			NiveauRequis = 2,
			DureePourcent = 90,
			PlancherMs = 280,
			ImpactMs = 80,
			TenueMs = 130,
			ArmementTenuMs = 0,
			SansMarcheMs = 150,
			DegatsPourcent = 100,
			KiPourcent = 8,
			ReculPx = 40,
			ReculMs = 230,
			ReculCotePx = 0,
			ReculLourdPx = 15,
			PousseePredite = true,
			ElanPx = 40,
			ElanMs = 80,
			EcartPx = 26,
			AllongePx = 0,
			ArretMs = 60,
			SecousseDixiemes = 30,
			SecousseMs = 160,
			SonCentiemes = -32,
			Souffle = false,
			SouffleCentiemes = 0
		};
	}

	private static CoupHeritageOptions MontantDeDepart()
	{
		return new CoupHeritageOptions
		{
			NiveauRequis = 6,
			DureePourcent = 106,
			PlancherMs = 280,
			ImpactMs = 150,
			TenueMs = 130,
			ArmementTenuMs = 100,
			SansMarcheMs = 150,
			DegatsPourcent = 100,
			KiPourcent = 8,
			ReculPx = 0,
			ReculMs = 0,
			ReculCotePx = 0,
			ReculLourdPx = 0,
			PousseePredite = false,
			ElanPx = 40,
			ElanMs = 150,
			EcartPx = 26,
			AllongePx = 20,
			ArretMs = 70,
			SecousseDixiemes = 30,
			SecousseMs = 170,
			SonCentiemes = 26,
			Souffle = true,
			SouffleCentiemes = 58
		};
	}

	private static CoupHeritageOptions CoupDePiedDeDepart()
	{
		return new CoupHeritageOptions
		{
			NiveauRequis = 1,
			DureePourcent = 120,
			PlancherMs = 360,
			ImpactMs = 100,
			TenueMs = 130,
			ArmementTenuMs = 0,
			SansMarcheMs = 0,
			DegatsPourcent = 150,
			KiPourcent = 12,
			ReculPx = 0,
			ReculMs = 0,
			ReculCotePx = 0,
			ReculLourdPx = 24,
			PousseePredite = false,
			ElanPx = 40,
			ElanMs = 100,
			EcartPx = 30,
			AllongePx = 0,
			ArretMs = 90,
			SecousseDixiemes = 40,
			SecousseMs = 200,
			SonCentiemes = -15,
			Souffle = true,
			SouffleCentiemes = -32
		};
	}

	private static CoupHeritageOptions FaucheDeDepart()
	{
		return new CoupHeritageOptions
		{
			NiveauRequis = 4,
			DureePourcent = 110,
			PlancherMs = 280,
			ImpactMs = 90,
			TenueMs = 140,
			ArmementTenuMs = 0,
			SansMarcheMs = 150,
			DegatsPourcent = 90,
			KiPourcent = 8,
			ReculPx = 24,
			ReculMs = 200,
			ReculCotePx = 0,
			ReculLourdPx = 8,
			PousseePredite = false,
			ElanPx = 0,
			ElanMs = 0,
			EcartPx = 0,
			AllongePx = 0,
			ArretMs = 60,
			SecousseDixiemes = 30,
			SecousseMs = 170,
			SonCentiemes = 20,
			Souffle = true,
			SouffleCentiemes = 38
		};
	}

	internal void Borner()
	{
		if (Direct == null)
		{
			CoupHeritageOptions coupHeritageOptions = (Direct = DirectDeDepart());
		}
		if (Crochet == null)
		{
			CoupHeritageOptions coupHeritageOptions = (Crochet = CrochetDeDepart());
		}
		if (CoupDEpaule == null)
		{
			CoupHeritageOptions coupHeritageOptions = (CoupDEpaule = CoupDEpauleDeDepart());
		}
		if (Montant == null)
		{
			CoupHeritageOptions coupHeritageOptions = (Montant = MontantDeDepart());
		}
		if (CoupDePied == null)
		{
			CoupHeritageOptions coupHeritageOptions = (CoupDePied = CoupDePiedDeDepart());
		}
		if (Fauche == null)
		{
			CoupHeritageOptions coupHeritageOptions = (Fauche = FaucheDeDepart());
		}
		Direct.Borner();
		Crochet.Borner();
		CoupDEpaule.Borner();
		Montant.Borner();
		CoupDePied.Borner();
		Fauche.Borner();
	}
}
