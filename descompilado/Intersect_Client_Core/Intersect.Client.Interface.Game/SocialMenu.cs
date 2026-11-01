using System;
using System.Collections.Generic;
using System.Linq;
using Intersect.Client.Entities;
using Intersect.Client.Framework.Entities;
using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.Framework.Input;
using Intersect.Client.General;
using Intersect.Client.Localization;

namespace Intersect.Client.Interface.Game;

public sealed class SocialMenu : AnchoredMenu
{
	public SocialMenu(Canvas gameCanvas)
		: base(gameCanvas, "SocialMenu", Strings.GameMenu.Social, 270)
	{
	}

	protected override IEnumerable<DropdownRow> BuildRows()
	{
		Player me = Globals.Me;
		MenuContainer menu = Interface.GameUi?.GameMenu;
		int num = me?.Friends.Count((IFriendInstance friend) => friend.Online) ?? 0;
		yield return new DropdownRow
		{
			Label = Strings.GameMenu.Friends,
			Icon = "saiyan_carte_icone_ami.png",
			Info = ((num > 0) ? Strings.DropdownMenus.Online.ToString(num) : null),
			InfoKind = DropdownInfoKind.Good,
			Key = DropdownPanel.KeyOf(Control.OpenFriends),
			HasNews = (menu?.HasNews(MenuBadge.Friends) ?? false),
			Action = delegate
			{
				Interface.GameUi?.GameMenu?.ToggleFriendsWindow();
			}
		};
		List<IPartyMember> list = me?.Party;
		yield return new DropdownRow
		{
			Label = Strings.GameMenu.Party,
			Icon = "saiyan_carte_icone_groupe.png",
			Info = ((list != null && list.Count > 0) ? Strings.DropdownMenus.Count.ToString(list.Count, Math.Max(1, Options.Instance.Party.MaximumMembers)) : null),
			InfoKind = DropdownInfoKind.Soft,
			Key = DropdownPanel.KeyOf(Control.OpenParties),
			HasNews = (menu?.HasNews(MenuBadge.Party) ?? false),
			Action = delegate
			{
				Interface.GameUi?.GameMenu?.TogglePartyWindow();
			}
		};
		yield return new DropdownRow
		{
			Label = Strings.Guilds.Guild,
			Icon = "saiyan_carte_icone_clan.png",
			Info = ((me != null && me.IsInGuild) ? me.Guild : null),
			InfoKind = DropdownInfoKind.Gold,
			Key = DropdownPanel.KeyOf(Control.OpenGuild),
			HasNews = (menu?.HasNews(MenuBadge.Guild) ?? false),
			Action = delegate
			{
				Interface.GameUi?.GameMenu?.ToggleGuildWindow();
			}
		};
	}
}
