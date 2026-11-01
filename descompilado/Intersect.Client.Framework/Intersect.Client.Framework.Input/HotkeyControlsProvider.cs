using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Intersect.Client.Framework.Input;

internal sealed class HotkeyControlsProvider : IControlsProvider
{
	private Dictionary<Control, ControlMapping>? _defaultMappings;

	public Control[] Controls { get; private set; }

	public HotkeyControlsProvider()
	{
		Controls = ControlsFromOptions(Options.Instance);
	}

	public bool TryGetDefaultMapping(Control control, [NotNullWhen(true)] out ControlMapping? defaultMapping)
	{
		if (_defaultMappings == null)
		{
			_defaultMappings = BuiltinControlsProvider.LoadDefaultMappings();
		}
		return _defaultMappings.TryGetValue(control, out defaultMapping);
	}

	private static Control[] ControlsFromOptions(Options? options)
	{
		return (from hotkeyValue in Enumerable.Range(1, options?.Player.HotbarSlotCount ?? 10)
			select (Control)(4096 + hotkeyValue)).ToArray();
	}

	public void ReloadFromOptions(Options? options)
	{
		Controls = ControlsFromOptions(options);
		_defaultMappings = null;
	}
}
