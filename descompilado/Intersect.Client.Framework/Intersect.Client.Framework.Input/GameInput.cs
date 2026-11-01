using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Core;
using Intersect.Framework.Reflection;
using Microsoft.Extensions.Logging;

namespace Intersect.Client.Framework.Input;

public abstract class GameInput : IGameInput
{
	private static GameInput? _current;

	private readonly HashSet<IControlsProvider> _controlsProviders = new HashSet<IControlsProvider>
	{
		new BuiltinControlsProvider(),
		new HotkeyControlsProvider()
	};

	public static GameInput Current => _current ?? throw new InvalidOperationException("GameInput not initialized");

	public abstract IControlSet ControlSet { get; set; }

	public IReadOnlySet<IControlsProvider> ControlsProviders => _controlsProviders;

	public Control[] AllControls => ControlsProviders.SelectMany((IControlsProvider provider) => provider.Controls).Distinct().ToArray();

	public abstract bool MouseHitInterface { get; }

	public abstract bool IsMouseInBounds { get; }

	public abstract InputDeviceType CursorMovementDevice { get; set; }

	public Vector2 MousePosition => GetMousePosition();

	protected GameInput(bool forceGlobal = false)
	{
		if (forceGlobal)
		{
			_current = this;
		}
		else if (_current == null)
		{
			_current = this;
		}
		Options.OptionsLoaded += OnOptionsOnOptionsLoaded;
	}

	private void OnOptionsOnOptionsLoaded(Options options)
	{
		foreach (IControlsProvider controlsProvider in _controlsProviders)
		{
			controlsProvider.ReloadFromOptions(options);
		}
		ControlSet.ReloadFromOptions(options);
	}

	public bool AddControlsProviders(params IControlsProvider[] controlsProviders)
	{
		bool flag = true;
		foreach (IControlsProvider controlsProvider in controlsProviders)
		{
			try
			{
				if (!_controlsProviders.Add(controlsProvider))
				{
					flag &= _controlsProviders.Contains(controlsProvider);
				}
			}
			catch (Exception exception)
			{
				ApplicationContext.Context.Value?.Logger.LogError(exception, "Failed to add controls provider of type {ControlsProviderType} to input instance", controlsProvider.GetType().GetName(qualified: true));
				flag = false;
			}
		}
		return flag;
	}

	public bool RemoveControlsProviders(params IControlsProvider[] controlsProviders)
	{
		bool flag = true;
		foreach (IControlsProvider controlsProvider in controlsProviders)
		{
			try
			{
				if (!_controlsProviders.Remove(controlsProvider))
				{
					flag &= !_controlsProviders.Contains(controlsProvider);
				}
			}
			catch (Exception exception)
			{
				ApplicationContext.Context.Value?.Logger.LogError(exception, "Failed to remove controls provider of type {ControlsProviderType} to input instance", controlsProvider.GetType().GetName(qualified: true));
				flag = false;
			}
		}
		return flag;
	}

	public abstract bool IsMouseButtonDown(MouseButton mb);

	public abstract bool WasMouseButtonDown(MouseButton mb);

	public abstract bool IsKeyDown(Keys key);

	public abstract bool WasKeyDown(Keys key);

	public abstract Vector2 GetMousePosition();

	public abstract void Update(TimeSpan elapsed);

	public abstract void OpenKeyboard(KeyboardType type, string text, bool autoCorrection, bool multiLine, bool secure);

	public abstract void OpenKeyboard(KeyboardType keyboardType, Action<string?> inputHandler, string description, string text, bool multiline = false, uint maxLength = 1024u, Rectangle? inputBounds = null);
}
