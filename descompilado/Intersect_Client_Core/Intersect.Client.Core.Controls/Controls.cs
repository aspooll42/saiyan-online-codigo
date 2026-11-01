using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Input;
using Intersect.Client.General;
using Intersect.Client.Interface;
using Intersect.Config;
using Intersect.Core;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Intersect.Client.Core.Controls;

public class Controls : IControlSet
{
	private readonly Dictionary<Control, ControlMapping> _mappings = new Dictionary<Control, ControlMapping>();

	public static IControlSet ActiveControls
	{
		get
		{
			return GameInput.Current.ControlSet;
		}
		set
		{
			GameInput.Current.ControlSet = value;
		}
	}

	public ControlMapping? this[Control control]
	{
		get
		{
			return _mappings.GetValueOrDefault(control);
		}
		set
		{
			if (value == null)
			{
				_mappings.Remove(control);
			}
			else
			{
				_mappings[control] = value;
			}
		}
	}

	public IReadOnlyDictionary<Control, ControlMapping> Mappings => _mappings.AsReadOnly();

	public Controls(IControlSet? other = null)
	{
		if (other != null)
		{
			foreach (KeyValuePair<Control, ControlMapping> mapping in other.Mappings)
			{
				mapping.Deconstruct(out var key, out var value);
				Control control = key;
				ControlMapping controlMapping = value;
				this[control] = new ControlMapping(controlMapping);
			}
			return;
		}
		ResetDefaults();
		TryLoad();
	}

	private bool TryGetDefaultMappingFor(Control control, [NotNullWhen(true)] out ControlMapping? defaultMapping)
	{
		foreach (IControlsProvider controlsProvider in GameInput.Current.ControlsProviders)
		{
			if (controlsProvider.TryGetDefaultMapping(control, out defaultMapping))
			{
				return true;
			}
		}
		defaultMapping = null;
		return false;
	}

	public void ResetDefaults()
	{
		_mappings.Clear();
		foreach (IControlsProvider controlsProvider in GameInput.Current.ControlsProviders)
		{
			Control[] controls = controlsProvider.Controls;
			foreach (Control control in controls)
			{
				if (controlsProvider.TryGetDefaultMapping(control, out ControlMapping defaultMapping))
				{
					TryAdd(control, defaultMapping);
					continue;
				}
				TryAdd(control, ControlBinding.Default, ControlBinding.Default);
			}
		}
	}

	private static void MigrateControlBindings(Control control)
	{
		string controlId = control.GetControlId();
		if (Globals.Database.HasPreference(controlId + "_key1value"))
		{
			Globals.Database.SavePreference(controlId + "_binding0", Globals.Database.LoadPreference(controlId + "_key1value"));
			Globals.Database.SavePreference(controlId + "_binding1", Globals.Database.LoadPreference(controlId + "_key2value"));
			Globals.Database.DeletePreference(controlId + "_key1value");
			Globals.Database.DeletePreference(controlId + "_key2value");
		}
		else if (Globals.Database.HasPreference(controlId + "_key1"))
		{
			Keys key = JsonConvert.DeserializeObject<Keys>(Globals.Database.LoadPreference(controlId + "_key1"));
			Keys key2 = JsonConvert.DeserializeObject<Keys>(Globals.Database.LoadPreference(controlId + "_key2"));
			Globals.Database.SavePreference(controlId + "_binding0", JsonConvert.SerializeObject(new ControlBinding(Keys.None, key)));
			Globals.Database.SavePreference(controlId + "_binding1", JsonConvert.SerializeObject(new ControlBinding(Keys.None, key2)));
			Globals.Database.DeletePreference(controlId + "_key1");
			Globals.Database.DeletePreference(controlId + "_key2");
		}
	}

	public void UpdateControl(Control control, int keyNum, Keys modifier, Keys key)
	{
		if (Mappings.TryGetValue(control, out ControlMapping value))
		{
			value.Bindings[keyNum].Modifier = modifier;
			value.Bindings[keyNum].Key = key;
		}
	}

	public void ReloadFromOptions(Options options)
	{
		TryLoad();
		PlayerOptions player = options.Player;
		if (player == null)
		{
			return;
		}
		for (int i = 0; i < player.HotbarSlotCount; i++)
		{
			Control control = (Control)(4096 + i + 1);
			if (!Mappings.TryGetValue(control, out ControlMapping value))
			{
				MigrateControlBindings(control);
				value = new ControlMapping(ControlBinding.Default, ControlBinding.Default);
				_mappings[control] = value;
			}
		}
	}

	public bool TryAdd(Control control, params ControlBinding[] bindings)
	{
		if (bindings.Length >= 1)
		{
			return _mappings.TryAdd(control, new ControlMapping(bindings.First(), bindings.Skip(1).ToArray()));
		}
		return false;
	}

	public bool TryAdd(Control control, ControlMapping mapping)
	{
		return _mappings.TryAdd(control, mapping);
	}

	public bool TryGetMappingFor(Control control, [NotNullWhen(true)] out ControlMapping? mapping)
	{
		return Mappings.TryGetValue(control, out mapping);
	}

	public bool TryLoad()
	{
		bool result = true;
		try
		{
			Control[] allControls = GameInput.Current.AllControls;
			Control[] array = allControls;
			foreach (Control control in array)
			{
				try
				{
					MigrateControlBindings(control);
				}
				catch (Exception exception)
				{
					ApplicationContext.Context.Value?.Logger.LogError(exception, "Error while migrating bindings for control {Control}", control.GetControlId());
				}
				try
				{
					if (TryGetMappingFor(control, out ControlMapping mapping))
					{
						goto IL_0104;
					}
					if (!TryGetDefaultMappingFor(control, out ControlMapping defaultMapping))
					{
						ApplicationContext.Context.Value?.Logger.LogDebug("No existing or default mapping for {ControlId}", control.GetControlId());
						defaultMapping = new ControlMapping(ControlBinding.Default, ControlBinding.Default);
					}
					if (TryAdd(control, defaultMapping))
					{
						mapping = defaultMapping;
						goto IL_0104;
					}
					ApplicationContext.Context.Value?.Logger.LogWarning("Failed to add default mapping for {ControlId}", control.GetControlId());
					goto end_IL_005a;
					IL_0104:
					for (int j = 0; j < mapping.Bindings.Count; j++)
					{
						if (TryLoadBindingFor(control, j, out ControlBinding binding))
						{
							mapping.Bindings[j] = binding;
						}
						else
						{
							TrySaveBindingFor(control, j, mapping.Bindings[j]);
						}
					}
					end_IL_005a:;
				}
				catch (Exception exception2)
				{
					ApplicationContext.Context.Value?.Logger.LogError(exception2, "Error while loading bindings for control {Control}", control.GetControlId());
					result = false;
				}
			}
			MigrateTechniquesKey();
			if (AreAllControlsBroken())
			{
				ApplicationContext.Context.Value?.Logger.LogWarning("Detected broken control bindings (all None/None), resetting to defaults");
				ResetDefaults();
				TrySave();
			}
			return result;
		}
		catch (Exception exception3)
		{
			ApplicationContext.Context.Value?.Logger.LogError(exception3, "Error while getting controls");
			return false;
		}
	}

	private void MigrateTechniquesKey()
	{
		if (Globals.Database.HasPreference("Migration_TechniquesSurT"))
		{
			return;
		}
		Globals.Database.SavePreference("Migration_TechniquesSurT", bool.TrueString);
		if (!TryGetMappingFor(Control.OpenSpells, out ControlMapping mapping) || !TryGetMappingFor(Control.OpenCalendar, out ControlMapping mapping2))
		{
			return;
		}
		ControlBinding controlBinding = mapping.Bindings[0];
		if (controlBinding != null && controlBinding.Modifier == Keys.None && controlBinding.Key == Keys.K)
		{
			ControlBinding controlBinding2 = mapping2.Bindings[0];
			if (controlBinding2 != null && controlBinding2.Modifier == Keys.None && controlBinding2.Key == Keys.T)
			{
				controlBinding.Key = Keys.T;
				controlBinding2.Key = Keys.K;
				TrySaveBindingFor(Control.OpenSpells, 0, controlBinding);
				TrySaveBindingFor(Control.OpenCalendar, 0, controlBinding2);
			}
		}
	}

	private bool AreAllControlsBroken()
	{
		foreach (var (control2, controlMapping2) in Mappings)
		{
			foreach (ControlBinding binding in controlMapping2.Bindings)
			{
				if (binding.Modifier != Keys.None || binding.Key != Keys.None)
				{
					return false;
				}
			}
		}
		return true;
	}

	private bool TryLoadBindingFor(Control control, int bindingIndex, [NotNullWhen(true)] out ControlBinding? binding)
	{
		try
		{
			string preferenceKeyFor = GetPreferenceKeyFor(control, bindingIndex);
			string value = Globals.Database.LoadPreference(preferenceKeyFor);
			if (string.IsNullOrWhiteSpace(value))
			{
				binding = null;
				return false;
			}
			binding = JsonConvert.DeserializeObject<ControlBinding>(value);
			return binding != null;
		}
		catch (Exception exception)
		{
			ApplicationContext.Context.Value?.Logger.LogError(exception, "Error while loading binding {BindingIndex} for control {Control}", bindingIndex, control.GetControlId());
			binding = null;
			return false;
		}
	}

	public bool TryReload()
	{
		return TryLoad();
	}

	public bool TrySave()
	{
		bool flag = true;
		foreach (var (control2, controlMapping2) in Mappings)
		{
			try
			{
				ControlBinding[] array = controlMapping2.Bindings.ToArray();
				for (int i = 0; i < array.Length; i++)
				{
					flag &= TrySaveBindingFor(control2, i, array[i]);
				}
			}
			catch (Exception exception)
			{
				ApplicationContext.Context.Value?.Logger.LogError(exception, "Error while saving bindings for control {Control}", control2.GetControlId());
				flag = false;
			}
		}
		return flag;
	}

	private string GetPreferenceKeyFor(Control control, int bindingIndex)
	{
		string controlId = control.GetControlId();
		return $"{controlId}_binding{bindingIndex}";
	}

	private bool TrySaveBindingFor(Control control, int bindingIndex, ControlBinding binding)
	{
		try
		{
			string preferenceKeyFor = GetPreferenceKeyFor(control, bindingIndex);
			string value = JsonConvert.SerializeObject(binding);
			Globals.Database.SavePreference(preferenceKeyFor, value);
			return true;
		}
		catch (Exception exception)
		{
			ApplicationContext.Context.Value?.Logger.LogError(exception, "Error while saving binding {BindingIndex} for control {Control}", bindingIndex, control.GetControlId());
			return false;
		}
	}

	public static bool IsControlJustPressed(Control control)
	{
		ControlMapping activeMapping;
		ControlBinding activeBinding;
		return IsControlJustPressed(control, out activeMapping, out activeBinding);
	}

	public static bool IsControlJustPressed(Control control, [NotNullWhen(true)] out ControlMapping? activeMapping, [NotNullWhen(true)] out ControlBinding? activeBinding)
	{
		if (ActiveControls.Mappings.TryGetValue(control, out ControlMapping value) && value.IsActive(out activeBinding) && !activeBinding.WasDown())
		{
			activeMapping = value;
			return true;
		}
		activeMapping = null;
		activeBinding = null;
		return false;
	}

	public static bool IsControlPressed(Control control)
	{
		ControlMapping activeMapping;
		ControlBinding activeBinding;
		return IsControlPressed(control, out activeMapping, out activeBinding);
	}

	public static bool IsControlPressed(Control control, [NotNullWhen(true)] out ControlMapping? activeMapping, [NotNullWhen(true)] out ControlBinding? activeBinding)
	{
		if (ActiveControls.Mappings.TryGetValue(control, out ControlMapping value) && value.IsActive(out activeBinding))
		{
			activeMapping = value;
			return true;
		}
		activeMapping = null;
		activeBinding = null;
		return false;
	}

	public static bool IsGameControlPressed(Control control)
	{
		if (!IsControlPressed(control, out ControlMapping _, out ControlBinding activeBinding) || Intersect.Client.Interface.Interface.HasInputFocus())
		{
			return false;
		}
		if (activeBinding.IsMouseKey)
		{
			return !Intersect.Client.Interface.Interface.DoesMouseHitInterface();
		}
		return true;
	}

	public static List<Control> GetControlsFor(Keys modifier, Keys key)
	{
		List<Control> list = (from Control control in Enum.GetValues(typeof(Control))
			where ControlHasKey(control, modifier, key)
			select control).ToList();
		if (list.Count > 0 || modifier == Keys.None)
		{
			return list;
		}
		return (from Control control in Enum.GetValues(typeof(Control))
			where ControlHasKey(control, Keys.None, key)
			select control).ToList();
	}

	public static bool EstUnClicOrdinaire(Keys modifier, Keys key)
	{
		if (modifier == Keys.None)
		{
			return true;
		}
		return !Enum.GetValues(typeof(Control)).Cast<Control>().Any((Control control) => ControlHasKey(control, modifier, key));
	}

	public static bool ControlHasKey(Control control, Keys modifier, Keys key)
	{
		if (key == Keys.None)
		{
			return false;
		}
		bool flag = modifier == Keys.Alt;
		bool flag2 = flag;
		if (flag2)
		{
			Keys keys = key;
			bool flag3 = (uint)(keys - 164) <= 1u;
			flag2 = flag3;
		}
		if (flag2)
		{
			return ControlHasKey(control, Keys.None, key);
		}
		if (ActiveControls.Mappings.TryGetValue(control, out ControlMapping value))
		{
			return value.Bindings.Any((ControlBinding binding) => binding.Modifier == modifier && binding.Key == key);
		}
		return false;
	}
}
