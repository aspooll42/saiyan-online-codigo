using System;

namespace Intersect.Client.Framework.Input;

public static class ControlExtensions
{
	public static string GetControlId(this Control control)
	{
		int num = (int)(control - 4096);
		int num2 = Options.Instance?.Player.HotbarSlotCount ?? 10;
		if (10 < num && num <= num2)
		{
			return $"Hotkey{num}";
		}
		string name = Enum.GetName(control);
		if (!string.IsNullOrWhiteSpace(name))
		{
			return name;
		}
		throw new ArgumentException($"No name found for control value {control}", "control");
	}

	public static bool IsValid(this Control control)
	{
		if (control == Control.HotkeyOffset)
		{
			return false;
		}
		if (control > Control.HotkeyOffset)
		{
			int num = (int)(control - 4096);
			int num2 = Options.Instance?.Player.HotbarSlotCount ?? 10;
			if (num <= num2)
			{
				return true;
			}
			if (Enum.IsDefined(control) && num > 10)
			{
				return false;
			}
		}
		return true;
	}
}
