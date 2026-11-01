using System.Linq;
using Intersect.Client.Entities;
using Intersect.Client.Localization;
using Intersect.Config;
using Intersect.GameObjects;

namespace Intersect.Client.Interface.Game.Enseignements;

internal static class TeachingTrialSteps
{
	private static string NomDuMaitre(TeachingMaster master)
	{
		return TeachingTexts.Of(master).Name;
	}

	public static string? ToOpenPath(Player? me, TeachingMaster master)
	{
		if (me != null)
		{
			TeachingTrialInfo teachingTrialInfo = TeachingTrials.ForOath(master, secondary: false);
			if ((object)teachingTrialInfo != null)
			{
				if (me.HasPassedTrial(teachingTrialInfo))
				{
					return Strings.TeachingsScreen.SwearNext.ToString(NomDuMaitre(master));
				}
				TeachingTrialInfo teachingTrialInfo2 = TeachingTrials.All.FirstOrDefault((TeachingTrialInfo trial) => trial.Master == master && trial.Attaque != TechniqueDeMaitre.Aucune && me.HasPassedTrial(trial));
				if (!(teachingTrialInfo2 != null))
				{
					return Strings.TeachingsScreen.WhyPassMasterTrial.ToString(Traduction.Contenu(teachingTrialInfo.Name), NomDuMaitre(master));
				}
				return Strings.TeachingsScreen.AttackNotPath.ToString(Traduction.Contenu(teachingTrialInfo2.Name), Traduction.Contenu(teachingTrialInfo.Name), NomDuMaitre(master));
			}
		}
		return null;
	}

	public static string? AfterSuccess(Player? me, string? trialId)
	{
		if (me != null)
		{
			TeachingTrialInfo teachingTrialInfo = TeachingTrials.Find(trialId);
			if ((object)teachingTrialInfo != null)
			{
				string text = NomDuMaitre(teachingTrialInfo.Master);
				if (teachingTrialInfo.Attaque != TechniqueDeMaitre.Aucune)
				{
					if (me.TeachingMain == TeachingMaster.None)
					{
						TeachingTrialInfo teachingTrialInfo2 = TeachingTrials.ForOath(teachingTrialInfo.Master, secondary: false);
						if ((object)teachingTrialInfo2 != null)
						{
							if (!me.HasPassedTrial(teachingTrialInfo2))
							{
								return Strings.TeachingsScreen.AttackNotPath.ToString(Traduction.Contenu(teachingTrialInfo.Name), Traduction.Contenu(teachingTrialInfo2.Name), text);
							}
							return Strings.TeachingsScreen.SwearNext.ToString(text);
						}
					}
					return null;
				}
				if (teachingTrialInfo.Id == TeachingTrials.ForOath(teachingTrialInfo.Master, secondary: false)?.Id && me.TeachingMain == TeachingMaster.None)
				{
					return Strings.TeachingsScreen.SwearNext.ToString(text);
				}
				if (teachingTrialInfo.Id == TeachingTrials.ForOath(teachingTrialInfo.Master, secondary: true)?.Id && me.TeachingMain != TeachingMaster.None && me.TeachingMain != teachingTrialInfo.Master && me.TeachingSecondary == TeachingMaster.None)
				{
					return Strings.TeachingsScreen.SwearNextSecondary.ToString(text);
				}
				return null;
			}
		}
		return null;
	}
}
