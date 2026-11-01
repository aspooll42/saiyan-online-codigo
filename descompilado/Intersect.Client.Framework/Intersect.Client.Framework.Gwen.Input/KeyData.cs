using System.Collections.Generic;
using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.Framework.Input;

namespace Intersect.Client.Framework.Gwen.Input;

public class KeyData
{
	public readonly bool[] KeyState;

	public readonly float[] NextRepeat;

	public Dictionary<MouseButton, bool> MouseButtonState = new Dictionary<MouseButton, bool>();

	public Base Target;

	public KeyData()
	{
		KeyState = new bool[16];
		NextRepeat = new float[16];
	}

	public bool IsMouseButtonDown(MouseButton mouseButton)
	{
		return MouseButtonState.GetValueOrDefault(mouseButton, defaultValue: false);
	}

	public bool SetMouseButtonState(MouseButton mouseButton, bool pressedState)
	{
		if (mouseButton != MouseButton.None && (uint)mouseButton <= 4u)
		{
			MouseButtonState[mouseButton] = pressedState;
			return true;
		}
		return false;
	}
}
