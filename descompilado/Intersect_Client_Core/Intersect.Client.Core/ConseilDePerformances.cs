using System;
using Intersect.Client.Framework.Database;
using Intersect.Client.General;
using Intersect.Client.Interface;
using Intersect.Client.Interface.Shared;
using Intersect.Client.Localization;

namespace Intersect.Client.Core;

internal static class ConseilDePerformances
{
	private const double ImageLenteMs = 25.0;

	private const int BilansLentsDeSuite = 3;

	private static int _bilansLents;

	public static void Bilan(double imageMoyenneMs)
	{
		GameDatabase database = Globals.Database;
		if (database == null || database.ConseilDePerformancesDonne || database.ModePerformances || Globals.GameState != GameStates.InGame)
		{
			_bilansLents = 0;
			return;
		}
		_bilansLents = ((imageMoyenneMs > 25.0) ? (_bilansLents + 1) : 0);
		if (_bilansLents >= 3)
		{
			database.ConseilDePerformancesDonne = true;
			database.SavePreferences();
			Intersect.Client.Interface.Interface.ShowAlert(Strings.OptionsWindow.PerformanceTip.ToString(Math.Round(1000.0 / imageMoyenneMs)), null, AlertType.Information);
		}
	}
}
