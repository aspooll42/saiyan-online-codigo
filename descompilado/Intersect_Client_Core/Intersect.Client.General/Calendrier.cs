using System;
using System.Globalization;
using Intersect.Client.Localization;

namespace Intersect.Client.General;

public static class Calendrier
{
	public enum Saison
	{
		Printemps,
		Ete,
		Automne,
		Hiver
	}

	public readonly record struct Date(long Numero, int Annee, int Mois, int Jour, double Heure)
	{
		public int JourDeSemaine => (Jour - 1) % 7 + 1;

		public Saison Saison => SaisonDu(Mois);

		public long MoisAbsolu => (long)(Annee - 761) * 12L + Mois;

		public double Instant => (double)Numero + Heure / 24.0;
	}

	public const int JoursParSemaine = 7;

	public const int JoursParMois = 28;

	public const int MoisParAnnee = 12;

	public const int JoursParAnnee = 336;

	public const int PremiereAnnee = 761;

	public const int JourDeLaPleineLune = 14;

	private const double HeureDeLaPleineLune = 21.0;

	public const int MoisDuBudokai = 4;

	public const int JourDuBudokai = 21;

	private const double HeureDuBudokai = 10.0;

	private static readonly DateTime Origine = new DateTime(2000, 1, 1).AddDays(3080.0);

	private static readonly CultureInfo Francais = CultureInfo.GetCultureInfo("fr-FR");

	public static Date Maintenant => DateDe(Time.TempsDuJeu);

	public static Date DateDe(DateTime temps)
	{
		double totalDays = (temps - Origine).TotalDays;
		return DateDuJour((long)Math.Floor(totalDays), (totalDays - Math.Floor(totalDays)) * 24.0);
	}

	private static Date DateDuJour(long numero, double heure)
	{
		long num = Division(numero, 336L);
		int num2 = (int)(numero - num * 336);
		return new Date(numero, 761 + (int)num, num2 / 28, num2 % 28 + 1, heure);
	}

	public static long PremierJourDu(long moisAbsolu)
	{
		return moisAbsolu * 28;
	}

	public static (int Annee, int Mois) MoisDe(long moisAbsolu)
	{
		long num = Division(moisAbsolu, 12L);
		return (Annee: 761 + (int)num, Mois: (int)(moisAbsolu - num * 12));
	}

	public static Saison SaisonDu(int mois)
	{
		return (Saison)(Math.Clamp(mois, 0, 11) / 3);
	}

	public static int PhaseDeLune(int jour)
	{
		int num = (int)Math.Round((double)(jour * 8) / 28.0) % 8;
		if (num != 4 || jour == 14)
		{
			return num;
		}
		if (jour >= 14)
		{
			return 5;
		}
		return 3;
	}

	public static string NomDuMois(int mois)
	{
		return mois switch
		{
			0 => Strings.Calendar.TurtleMonth, 
			1 => Strings.Calendar.CraneMonth, 
			2 => Strings.Calendar.CloudMonth, 
			3 => Strings.Calendar.DragonMonth, 
			4 => Strings.Calendar.TournamentMonth, 
			5 => Strings.Calendar.SunMonth, 
			6 => Strings.Calendar.MonkeyMonth, 
			7 => Strings.Calendar.MoonMonth, 
			8 => Strings.Calendar.SnakeMonth, 
			9 => Strings.Calendar.WolfMonth, 
			10 => Strings.Calendar.SnowMonth, 
			_ => Strings.Calendar.EnmaMonth, 
		};
	}

	public static string NomDeSaison(Saison saison)
	{
		return saison switch
		{
			Saison.Printemps => Strings.Calendar.Spring, 
			Saison.Ete => Strings.Calendar.Summer, 
			Saison.Automne => Strings.Calendar.Autumn, 
			_ => Strings.Calendar.Winter, 
		};
	}

	public static string NomDuJour(int jourDeSemaine)
	{
		if (jourDeSemaine > 1)
		{
			return Strings.Calendar.Stars.ToString(jourDeSemaine);
		}
		return Strings.Calendar.OneStar;
	}

	public static string JourEtMois(int jour, int mois)
	{
		return ((jour == 1) ? Strings.Calendar.FirstOfMonth : Strings.Calendar.DayOfMonth).ToString(jour, NomDuMois(mois));
	}

	public static string TexteHeure(double heure)
	{
		int num = (int)((heure % 24.0 + 24.0) % 24.0 * 60.0);
		return $"{num / 60:00}:{num % 60:00}";
	}

	public static double ProchainePleineLune(Date maintenant)
	{
		double num = (double)(PremierJourDu(maintenant.MoisAbsolu) + 14 - 1) + 0.875;
		if (!(maintenant.Instant > num + 1.0 / 3.0))
		{
			return num;
		}
		return num + 28.0;
	}

	public static double ProchainBudokai(Date maintenant)
	{
		long num = PremierJourDu(maintenant.MoisAbsolu - maintenant.Mois);
		double num2 = (double)(num + 112 + 21 - 1) + 5.0 / 12.0;
		if (!(maintenant.Instant > num2 + 5.0 / 12.0))
		{
			return num2;
		}
		return num2 + 336.0;
	}

	public static (Saison Saison, double Debut) SaisonSuivante(Date maintenant)
	{
		long moisAbsolu = maintenant.MoisAbsolu - maintenant.Mois % 3 + 3;
		return (Saison: SaisonDu(MoisDe(moisAbsolu).Mois), Debut: PremierJourDu(moisAbsolu));
	}

	public static Date DateA(double instant)
	{
		return DateDuJour((long)Math.Floor(instant), (instant - Math.Floor(instant)) * 24.0);
	}

	public static (string Nom, double Heures) ProchaineEtape(double heure)
	{
		heure = (heure % 24.0 + 24.0) % 24.0;
		(double, string)[] array = new(double, string)[4]
		{
			(4.0, Strings.Calendar.Dawn),
			(7.0, Strings.Calendar.Daylight),
			(19.0, Strings.Calendar.Sunset),
			(22.0, Strings.Calendar.Night)
		};
		(double, string)[] array2 = array;
		for (int i = 0; i < array2.Length; i++)
		{
			var (num, item) = array2[i];
			if (num > heure)
			{
				return (Nom: item, Heures: num - heure);
			}
		}
		return (Nom: array[0].Item2, Heures: array[0].Item1 + 24.0 - heure);
	}

	public static string Echeance(Date maintenant, double instant, bool laNuit)
	{
		long num = (long)Math.Floor(instant) - maintenant.Numero;
		if (num > 0)
		{
			if (num == 1)
			{
				return Strings.Calendar.Tomorrow;
			}
			return Strings.Calendar.InDays.ToString(num);
		}
		return laNuit ? ((string)Strings.Calendar.Tonight) : ((string)Strings.Calendar.TodayLower);
	}

	public static string EnTempsReel(double jours)
	{
		double num = Math.Max(0.0, jours) * 24.0 / (double)Time.Vitesse;
		if (num < 1.0)
		{
			int num2 = Math.Max(1, (int)Math.Round(num * 60.0));
			return ((num2 == 1) ? Strings.Calendar.RealOneMinute : Strings.Calendar.RealMinutes).ToString(num2);
		}
		if (num < 23.5)
		{
			int num3 = (int)Math.Round(num);
			return ((num3 == 1) ? Strings.Calendar.RealOneHour : Strings.Calendar.RealHours).ToString(num3);
		}
		double num4 = num / 24.0;
		return ((num4 < 2.0) ? Strings.Calendar.RealOneDay : Strings.Calendar.RealDays).ToString(num4.ToString("0.#", Francais));
	}

	public static string DureeCourte(double secondes)
	{
		int num = Math.Max(1, (int)Math.Ceiling(secondes / 60.0));
		if (num >= 60)
		{
			return Strings.Calendar.ShortHours.ToString(num / 60, num % 60);
		}
		return Strings.Calendar.ShortMinutes.ToString(num);
	}

	public static string Duree(double heures)
	{
		if (heures < 1.0)
		{
			return Strings.Calendar.DurationMinutes.ToString(Math.Max(1, (int)Math.Round(heures * 60.0)));
		}
		if (heures < 48.0)
		{
			if (!(Math.Abs(heures - 1.0) < 0.01))
			{
				return Strings.Calendar.DurationHours.ToString(heures.ToString("0.#", Francais));
			}
			return Strings.Calendar.DurationOneHour;
		}
		double num = heures / 24.0;
		double num2 = num / 7.0;
		if (num2 >= 2.0 && Math.Abs(num2 - Math.Round(num2)) < 0.01)
		{
			return Strings.Calendar.DurationWeeks.ToString((int)Math.Round(num2));
		}
		int num3 = (int)Math.Floor(num + 0.001);
		int num4 = (int)Math.Round((num - (double)num3) * 24.0);
		if (num4 > 0)
		{
			return Strings.Calendar.DurationDaysHours.ToString(num3, num4);
		}
		return Strings.Calendar.DurationDays.ToString(num3);
	}

	private static long Division(long a, long b)
	{
		if (a < 0)
		{
			return -((-a + b - 1) / b);
		}
		return a / b;
	}
}
