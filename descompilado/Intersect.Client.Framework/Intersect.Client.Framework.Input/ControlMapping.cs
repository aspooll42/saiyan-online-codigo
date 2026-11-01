using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Newtonsoft.Json;

namespace Intersect.Client.Framework.Input;

public class ControlMapping
{
	public List<ControlBinding> Bindings { get; }

	[JsonConstructor]
	public ControlMapping(ControlBinding binding, params ControlBinding[] alternateBindings)
	{
		Bindings = new List<ControlBinding>(1 + ((alternateBindings != null) ? alternateBindings.Length : 0)) { binding };
		Bindings.AddRange(alternateBindings ?? Array.Empty<ControlBinding>());
		if (Bindings.Count < 2)
		{
			Bindings.Add(ControlBinding.Default);
		}
	}

	public ControlMapping(ControlMapping controlMapping)
	{
		ArgumentNullException.ThrowIfNull(controlMapping, "controlMapping");
		Bindings = controlMapping.Bindings.Select((ControlBinding binding) => new ControlBinding(binding)).ToList();
	}

	public bool WasActive([NotNullWhen(true)] out ControlBinding? activeBinding)
	{
		foreach (ControlBinding binding in Bindings)
		{
			if (binding.WasDown())
			{
				if (!binding.IsMouseKey)
				{
					activeBinding = binding;
					return true;
				}
				if (!GameInput.Current.MouseHitInterface)
				{
					activeBinding = binding;
					return true;
				}
			}
		}
		activeBinding = null;
		return false;
	}

	public bool IsActive([NotNullWhen(true)] out ControlBinding? activeBinding)
	{
		foreach (ControlBinding binding in Bindings)
		{
			if (binding.IsDown())
			{
				if (!binding.IsMouseKey)
				{
					activeBinding = binding;
					return true;
				}
				if (GameInput.Current.IsMouseInBounds && !GameInput.Current.MouseHitInterface)
				{
					activeBinding = binding;
					return true;
				}
			}
		}
		activeBinding = null;
		return false;
	}
}
