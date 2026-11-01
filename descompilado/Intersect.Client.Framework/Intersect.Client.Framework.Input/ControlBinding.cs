using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Gwen.DragDrop;
using Newtonsoft.Json;

namespace Intersect.Client.Framework.Input;

[method: JsonConstructor]
public class ControlBinding(Keys modifier, Keys key)
{
	public static ControlBinding Default => new ControlBinding(Keys.None, Keys.None);

	public Keys Modifier { get; set; } = modifier;

	public Keys Key { get; set; } = key;

	public bool IsMouseKey
	{
		get
		{
			Keys key = Key;
			if ((uint)(key - 1) <= 1u || (uint)(key - 4) <= 2u)
			{
				return true;
			}
			return false;
		}
	}

	public ControlBinding(ControlBinding controlBinding)
		: this(controlBinding.Modifier, controlBinding.Key)
	{
	}

	public bool IsDown()
	{
		GameInput current = GameInput.Current;
		if (Modifier != Keys.None && !current.IsKeyDown(Modifier))
		{
			return false;
		}
		if (IsMouseKey && Key.TryGetMouseButton(out var mouseButton))
		{
			if (current.IsMouseButtonDown(mouseButton))
			{
				return !DragAndDrop.IsDragging;
			}
			return false;
		}
		return current.IsKeyDown(Key);
	}

	public bool WasDown()
	{
		GameInput current = GameInput.Current;
		if (Modifier != Keys.None && !current.WasKeyDown(Modifier))
		{
			return false;
		}
		if (IsMouseKey && Key.TryGetMouseButton(out var mouseButton))
		{
			if (current.WasMouseButtonDown(mouseButton))
			{
				return !DragAndDrop.IsDragging;
			}
			return false;
		}
		return current.WasKeyDown(Key);
	}
}
