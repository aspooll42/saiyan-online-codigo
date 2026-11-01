using System;
using System.Collections.Generic;
using Intersect.Client.Interface.Game.DescriptionWindows.Components;
using Intersect.Client.Localization;
using Intersect.Enums;
using Intersect.Localization;

namespace Intersect.Client.Interface.Game.DescriptionWindows;

public class StatDescriptionWindow : DescriptionWindowBase
{
	public StatDescriptionWindow(Stat stat)
		: base(Interface.GameUi.GameCanvas, "DescriptionWindow")
	{
		HeaderComponent headerComponent = AddHeader();
		headerComponent.HideIcon();
		headerComponent.SetTitle(Text(Strings.ItemDescription.Stats, (int)stat));
		headerComponent.SetSubtitle(Text(Strings.Character.StatRoles, (int)stat));
		headerComponent.SetDescription(string.Empty);
		headerComponent.SizeToChildren(resizeX: true, resizeY: false);
		string text = Text(Strings.Character.StatDescriptions, (int)stat);
		if (text.Length > 0)
		{
			AddDivider();
			AddDescription().AddText(text);
		}
		string[] array = Text(Strings.Character.StatDetails, (int)stat).Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
		if (array.Length != 0)
		{
			AddDivider();
			RowContainerComponent rowContainerComponent = AddRowContainer();
			string[] array2 = array;
			foreach (string text2 in array2)
			{
				int num = text2.IndexOf('=');
				if (num < 0)
				{
					rowContainerComponent.AddKeyValueRow(text2, string.Empty);
					continue;
				}
				string key = text2.Substring(0, num).Trim();
				string text3 = text2;
				int num2 = num + 1;
				rowContainerComponent.AddKeyValueRow(key, text3.Substring(num2, text3.Length - num2).Trim());
			}
			rowContainerComponent.SizeToChildren();
		}
		FinalizeWindow();
	}

	private static string Text(Dictionary<int, LocalizedString> strings, int index)
	{
		if (!strings.TryGetValue(index, out LocalizedString value))
		{
			return string.Empty;
		}
		return value.ToString();
	}
}
