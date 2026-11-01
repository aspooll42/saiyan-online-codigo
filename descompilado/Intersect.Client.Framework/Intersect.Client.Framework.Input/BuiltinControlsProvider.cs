using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using Intersect.Client.Framework.Database;
using Intersect.Client.Framework.GenericClasses;
using Newtonsoft.Json;

namespace Intersect.Client.Framework.Input;

internal sealed class BuiltinControlsProvider : IControlsProvider
{
	private readonly Dictionary<Control, ControlMapping> _defaultMappings = LoadDefaultMappings();

	public Control[] Controls { get; } = (from control in Enum.GetValues<Control>()
		where control.IsValid()
		select control).ToArray();

	public bool TryGetDefaultMapping(Control control, [NotNullWhen(true)] out ControlMapping? defaultMapping)
	{
		if (_defaultMappings.TryGetValue(control, out ControlMapping value))
		{
			defaultMapping = new ControlMapping(value);
			return true;
		}
		defaultMapping = null;
		return false;
	}

	public void ReloadFromOptions(Options? options)
	{
	}

	public static Dictionary<Control, ControlMapping> LoadDefaultMappings()
	{
		Dictionary<Control, ControlMapping> hardcodedDefaults = GetHardcodedDefaults();
		string path = Path.Combine("resources", "default_settings.json");
		if (File.Exists(path))
		{
			JsonDatabase jsonDatabase = new JsonDatabase(path);
			foreach (Control key in hardcodedDefaults.Keys)
			{
				string controlId = key.GetControlId();
				List<ControlBinding> list = new List<ControlBinding>();
				if (jsonDatabase.HasPreference(controlId + "_binding0") && jsonDatabase.HasPreference(controlId + "_binding1"))
				{
					ControlBinding controlBinding = JsonConvert.DeserializeObject<ControlBinding>(jsonDatabase.LoadPreference(controlId + "_binding0"));
					ControlBinding controlBinding2 = JsonConvert.DeserializeObject<ControlBinding>(jsonDatabase.LoadPreference(controlId + "_binding1"));
					if (controlBinding != null && controlBinding2 != null)
					{
						hardcodedDefaults[key] = new ControlMapping(controlBinding, controlBinding2);
					}
				}
			}
		}
		return hardcodedDefaults;
	}

	private static Dictionary<Control, ControlMapping> GetHardcodedDefaults()
	{
		Dictionary<Control, ControlMapping> dictionary = new Dictionary<Control, ControlMapping>();
		dictionary.Add(Control.MoveUp, new ControlMapping(new ControlBinding(Keys.None, Keys.Up), new ControlBinding(Keys.None, Keys.W)));
		dictionary.Add(Control.MoveDown, new ControlMapping(new ControlBinding(Keys.None, Keys.Down), new ControlBinding(Keys.None, Keys.S)));
		dictionary.Add(Control.MoveLeft, new ControlMapping(new ControlBinding(Keys.None, Keys.Left), new ControlBinding(Keys.None, Keys.A)));
		dictionary.Add(Control.MoveRight, new ControlMapping(new ControlBinding(Keys.None, Keys.Right), new ControlBinding(Keys.None, Keys.D)));
		dictionary.Add(Control.AttackInteract, new ControlMapping(new ControlBinding(Keys.None, Keys.E), new ControlBinding(Keys.None, Keys.LButton)));
		dictionary.Add(Control.Block, new ControlMapping(new ControlBinding(Keys.None, Keys.Q), new ControlBinding(Keys.None, Keys.RButton)));
		dictionary.Add(Control.AutoTarget, new ControlMapping(new ControlBinding(Keys.None, Keys.Tab), ControlBinding.Default));
		dictionary.Add(Control.HoldToSoftRetargetOnSelfCast, new ControlMapping(new ControlBinding(Keys.None, Keys.LMenu), ControlBinding.Default));
		dictionary.Add(Control.ToggleAutoSoftRetargetOnSelfCast, new ControlMapping(ControlBinding.Default, ControlBinding.Default));
		dictionary.Add(Control.PickUp, new ControlMapping(new ControlBinding(Keys.None, Keys.Space), ControlBinding.Default));
		dictionary.Add(Control.Enter, new ControlMapping(new ControlBinding(Keys.None, Keys.Return), ControlBinding.Default));
		dictionary.Add(Control.Screenshot, new ControlMapping(new ControlBinding(Keys.None, Keys.F12), ControlBinding.Default));
		dictionary.Add(Control.OpenMenu, new ControlMapping(new ControlBinding(Keys.None, Keys.Escape), ControlBinding.Default));
		dictionary.Add(Control.OpenInventory, new ControlMapping(new ControlBinding(Keys.None, Keys.I), ControlBinding.Default));
		dictionary.Add(Control.OpenQuests, new ControlMapping(new ControlBinding(Keys.None, Keys.L), ControlBinding.Default));
		dictionary.Add(Control.OpenCharacterInfo, new ControlMapping(new ControlBinding(Keys.None, Keys.C), ControlBinding.Default));
		dictionary.Add(Control.OpenParties, new ControlMapping(new ControlBinding(Keys.None, Keys.P), ControlBinding.Default));
		dictionary.Add(Control.OpenSpells, new ControlMapping(new ControlBinding(Keys.None, Keys.T), ControlBinding.Default));
		dictionary.Add(Control.OpenFriends, new ControlMapping(new ControlBinding(Keys.None, Keys.F), ControlBinding.Default));
		dictionary.Add(Control.OpenGuild, new ControlMapping(new ControlBinding(Keys.None, Keys.G), ControlBinding.Default));
		dictionary.Add(Control.OpenSettings, new ControlMapping(new ControlBinding(Keys.None, Keys.O), ControlBinding.Default));
		dictionary.Add(Control.OpenDebugger, new ControlMapping(new ControlBinding(Keys.None, Keys.F2), ControlBinding.Default));
		dictionary.Add(Control.OpenAdminPanel, new ControlMapping(new ControlBinding(Keys.None, Keys.Insert), ControlBinding.Default));
		dictionary.Add(Control.ToggleGui, new ControlMapping(new ControlBinding(Keys.None, Keys.F11), ControlBinding.Default));
		dictionary.Add(Control.TurnAround, new ControlMapping(new ControlBinding(Keys.None, Keys.Control), ControlBinding.Default));
		dictionary.Add(Control.ToggleZoomIn, new ControlMapping(ControlBinding.Default, ControlBinding.Default));
		dictionary.Add(Control.ToggleZoomOut, new ControlMapping(ControlBinding.Default, ControlBinding.Default));
		dictionary.Add(Control.HoldToZoomIn, new ControlMapping(ControlBinding.Default, ControlBinding.Default));
		dictionary.Add(Control.HoldToZoomOut, new ControlMapping(ControlBinding.Default, ControlBinding.Default));
		dictionary.Add(Control.ToggleFullscreen, new ControlMapping(new ControlBinding(Keys.Alt, Keys.Return), ControlBinding.Default));
		dictionary.Add(Control.Sprint, new ControlMapping(new ControlBinding(Keys.None, Keys.Shift), ControlBinding.Default));
		dictionary.Add(Control.OpenWorldMap, new ControlMapping(new ControlBinding(Keys.None, Keys.M), ControlBinding.Default));
		dictionary.Add(Control.OpenAchievements, new ControlMapping(new ControlBinding(Keys.None, Keys.J), ControlBinding.Default));
		dictionary.Add(Control.OpenMarket, new ControlMapping(new ControlBinding(Keys.None, Keys.H), ControlBinding.Default));
		dictionary.Add(Control.OpenDailyRewards, new ControlMapping(new ControlBinding(Keys.None, Keys.B), ControlBinding.Default));
		dictionary.Add(Control.ChargeKi, new ControlMapping(new ControlBinding(Keys.None, Keys.R), ControlBinding.Default));
		dictionary.Add(Control.Dodge, new ControlMapping(new ControlBinding(Keys.None, Keys.X), new ControlBinding(Keys.None, Keys.XButton1)));
		dictionary.Add(Control.Dash, new ControlMapping(new ControlBinding(Keys.None, Keys.Space), ControlBinding.Default));
		dictionary.Add(Control.ShowLocalMap, new ControlMapping(new ControlBinding(Keys.None, Keys.V), ControlBinding.Default));
		dictionary.Add(Control.OpenProfessions, new ControlMapping(new ControlBinding(Keys.None, Keys.N), ControlBinding.Default));
		dictionary.Add(Control.OpenCalendar, new ControlMapping(new ControlBinding(Keys.None, Keys.K), ControlBinding.Default));
		dictionary.Add(Control.OpenTeachings, new ControlMapping(new ControlBinding(Keys.None, Keys.U), ControlBinding.Default));
		dictionary.Add(Control.OpenRankings, new ControlMapping(new ControlBinding(Keys.None, Keys.Y), ControlBinding.Default));
		dictionary.Add(Control.Hotkey1, new ControlMapping(new ControlBinding(Keys.None, Keys.D1), ControlBinding.Default));
		dictionary.Add(Control.Hotkey2, new ControlMapping(new ControlBinding(Keys.None, Keys.D2), ControlBinding.Default));
		dictionary.Add(Control.Hotkey3, new ControlMapping(new ControlBinding(Keys.None, Keys.D3), ControlBinding.Default));
		dictionary.Add(Control.Hotkey4, new ControlMapping(new ControlBinding(Keys.None, Keys.D4), ControlBinding.Default));
		dictionary.Add(Control.Hotkey5, new ControlMapping(new ControlBinding(Keys.None, Keys.D5), ControlBinding.Default));
		dictionary.Add(Control.Hotkey6, new ControlMapping(new ControlBinding(Keys.None, Keys.D6), ControlBinding.Default));
		dictionary.Add(Control.Hotkey7, new ControlMapping(new ControlBinding(Keys.None, Keys.D7), ControlBinding.Default));
		dictionary.Add(Control.Hotkey8, new ControlMapping(new ControlBinding(Keys.None, Keys.D8), ControlBinding.Default));
		dictionary.Add(Control.Hotkey9, new ControlMapping(new ControlBinding(Keys.None, Keys.D9), ControlBinding.Default));
		dictionary.Add(Control.Hotkey10, new ControlMapping(new ControlBinding(Keys.None, Keys.D0), ControlBinding.Default));
		return dictionary;
	}
}
