using System.Linq;
using Intersect.Config;

namespace Intersect.GameObjects;

public static class TeachingTrials
{
	public const int MainLevel = 10;

	public const int OpenLevel = 20;

	public const int SecondaryLevel = 40;

	public static readonly TeachingTrialInfo[] All = new TeachingTrialInfo[11]
	{
		new TeachingTrialInfo("piccolo-10", TeachingMaster.Piccolo, 10, "Le mur", Implemented: true),
		new TeachingTrialInfo("tortue-10", TeachingMaster.Tortue, 10, "L'entraînement de la Tortue", Implemented: true),
		new TeachingTrialInfo("tenshinhan-10", TeachingMaster.Tenshinhan, 10, "Les lanternes de ki", Implemented: true),
		new TeachingTrialInfo("krillin-10", TeachingMaster.Krillin, 10, "Le cercle des blessés", Implemented: true),
		new TeachingTrialInfo("piccolo-40", TeachingMaster.Piccolo, 40, "Le gardien de Gohan", Implemented: true),
		new TeachingTrialInfo("tortue-40", TeachingMaster.Tortue, 40, "Le tournoi de l'île", Implemented: true),
		new TeachingTrialInfo("tenshinhan-40", TeachingMaster.Tenshinhan, 40, "Le Tri-Faisceau", Implemented: true),
		new TeachingTrialInfo("krillin-40", TeachingMaster.Krillin, 40, "La meute du lac", Implemented: true),
		new TeachingTrialInfo("tenshinhan-eblouissement", TeachingMaster.Tenshinhan, 20, "L'éclair du soleil", Implemented: true, TechniqueDeMaitre.Eblouissement),
		new TeachingTrialInfo("krillin-kienzan", TeachingMaster.Krillin, 35, "Le disque", Implemented: true, TechniqueDeMaitre.Kienzan),
		new TeachingTrialInfo("piccolo-makankosappo", TeachingMaster.Piccolo, 50, "La file", Implemented: true, TechniqueDeMaitre.Makankosappo)
	};

	public static TeachingTrialInfo? ForAttaque(TechniqueDeMaitre attaque)
	{
		if (attaque != TechniqueDeMaitre.Aucune)
		{
			return All.FirstOrDefault((TeachingTrialInfo trial) => trial.Attaque == attaque);
		}
		return null;
	}

	public static TeachingTrialInfo? Find(string? id)
	{
		return All.FirstOrDefault((TeachingTrialInfo trial) => trial.Id == id);
	}

	public static TeachingTrialInfo? Find(TeachingMaster master, int level)
	{
		return All.FirstOrDefault((TeachingTrialInfo trial) => trial.Master == master && trial.Level == level);
	}

	public static TeachingTrialInfo? ForOath(TeachingMaster master, bool secondary)
	{
		return Find(master, secondary ? 40 : 10);
	}
}
