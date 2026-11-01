using System;
using System.Collections.Generic;
using System.Linq;
using Intersect.Client.Entities;
using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.Framework.Input;
using Intersect.Client.General;
using Intersect.Client.Interface.Game.Rankings;
using Intersect.Client.Localization;
using Intersect.Framework.Core.GameObjects.Achievements;

namespace Intersect.Client.Interface.Game;

public sealed class ProgressionMenu : AnchoredMenu
{
	public ProgressionMenu(Canvas gameCanvas)
		: base(gameCanvas, "ProgressionMenu", Strings.GameMenu.Progression, 340)
	{
	}

	protected override IEnumerable<DropdownRow> BuildRows()
	{
		bool dailyRewardCanClaim = Globals.DailyRewardCanClaim;
		yield return new DropdownRow
		{
			Label = Strings.DailyRewards.Title,
			Icon = "saiyan_menu_icone_cadeau.png",
			Info = (dailyRewardCanClaim ? Strings.DropdownMenus.Available.ToString() : null),
			InfoKind = DropdownInfoKind.Good,
			Key = DropdownPanel.KeyOf(Control.OpenDailyRewards),
			HasNews = dailyRewardCanClaim,
			Action = delegate
			{
				Interface.GameUi?.ToggleDailyRewardWindow();
			}
		};
		AchievementProgress[] achievements = Globals.Achievements;
		yield return new DropdownRow
		{
			Label = Strings.Achievements.Title,
			Icon = "saiyan_star_gold.png",
			Info = ((achievements.Length != 0) ? Strings.DropdownMenus.Count.ToString(achievements.Count((AchievementProgress achievement) => achievement.Completed), achievements.Length) : null),
			InfoKind = DropdownInfoKind.Soft,
			Key = DropdownPanel.KeyOf(Control.OpenAchievements),
			Action = delegate
			{
				Interface.GameUi?.ToggleAchievementsWindow();
			}
		};
		Player me = Globals.Me;
		int num = ((me != null) ? Enumerable.Range(0, 9).Max((Func<int, int>)me.ProfessionLevel) : 0);
		yield return new DropdownRow
		{
			Label = Strings.Professions.Title,
			Icon = "saiyan_menu_icone_metiers.png",
			Info = ((num > 0) ? Strings.DropdownMenus.LevelShort.ToString(num) : null),
			InfoKind = DropdownInfoKind.Soft,
			Key = DropdownPanel.KeyOf(Control.OpenProfessions),
			Action = delegate
			{
				Interface.GameUi?.ToggleProfessionsWindow();
			}
		};
		int powerPlace = RankingState.PowerPlace;
		yield return new DropdownRow
		{
			Label = Strings.Rankings.Title,
			Icon = "saiyan_menu_icone_classements.png",
			Info = ((powerPlace > 0) ? Strings.Rankings.MenuPlace.ToString(powerPlace) : null),
			InfoKind = DropdownInfoKind.Soft,
			Key = DropdownPanel.KeyOf(Control.OpenRankings),
			Action = delegate
			{
				Interface.GameUi?.ToggleRankingsScreen();
			}
		};
	}
}
