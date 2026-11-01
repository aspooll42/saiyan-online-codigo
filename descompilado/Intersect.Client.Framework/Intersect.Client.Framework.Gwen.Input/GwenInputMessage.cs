using System.Numerics;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Input;

namespace Intersect.Client.Framework.Gwen.Input;

public class GwenInputMessage
{
	public bool Alt;

	public bool Control;

	public Keys Key;

	public MouseButton MouseBtn;

	public Vector2 MousePosition;

	public bool Shift;

	public IntersectInput.InputEvent Type;

	public string Unicode = string.Empty;

	public GwenInputMessage(IntersectInput.InputEvent type, Vector2 mousePos, MouseButton mousebtn, Keys keyAction, bool alt = false, bool control = false, bool shift = false, string unicode = "")
	{
		Type = type;
		MousePosition = mousePos;
		Key = keyAction;
		MouseBtn = mousebtn;
		Alt = alt;
		Control = control;
		Shift = shift;
		Unicode = unicode;
	}
}
