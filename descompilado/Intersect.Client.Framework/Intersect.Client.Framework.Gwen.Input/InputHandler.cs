using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.Framework.Gwen.DragDrop;
using Intersect.Client.Framework.Gwen.Platform;
using Intersect.Client.Framework.Input;

namespace Intersect.Client.Framework.Gwen.Input;

public static class InputHandler
{
	private static readonly KeyData KeyData = new KeyData();

	private static readonly Dictionary<MouseButton, float> LastClickTime = new Dictionary<MouseButton, float>();

	private static Base? _focusedKeyboard;

	private static Base? _focusedMouse;

	public static Point MousePosition;

	private static Point _lastClickPosition;

	public static readonly ImmutableArray<MouseButton> MouseButtons = (from mouseButton in Enum.GetValues<MouseButton>()
		where (mouseButton != MouseButton.None && (uint)mouseButton <= 4u) ? true : false
		select mouseButton).ToImmutableArray();

	private static Base? _hoveredControl;

	public static Base? HoveredControl
	{
		get
		{
			return _hoveredControl;
		}
		set
		{
			if (value != _hoveredControl)
			{
				Base hoveredControl = _hoveredControl;
				_hoveredControl = value;
			}
		}
	}

	public static Base? KeyboardFocus
	{
		get
		{
			return _focusedKeyboard;
		}
		set
		{
			if (value != _focusedKeyboard)
			{
				Base focusedKeyboard = _focusedKeyboard;
				_focusedKeyboard = value;
			}
		}
	}

	public static Base? MouseFocus
	{
		get
		{
			return _focusedMouse;
		}
		set
		{
			if (value != _focusedMouse)
			{
				Base focusedMouse = _focusedMouse;
				_focusedMouse = value;
			}
		}
	}

	public static int MaxMouseButtons => MouseButtons.Length;

	public static float DoubleClickSpeed => 0.5f;

	public static float KeyRepeatRate => 0.03f;

	public static float KeyRepeatDelay => 0.5f;

	public static bool IsLeftMouseDown => KeyData.IsMouseButtonDown(MouseButton.Left);

	public static bool IsRightMouseDown => KeyData.IsMouseButtonDown(MouseButton.Right);

	public static bool IsShiftDown => IsKeyDown(Key.Shift);

	public static bool IsControlDown => IsKeyDown(Key.Control);

	public static event Action<Base?, FocusSource>? FocusChanged;

	public static void Focus(FocusSource focusSource, Base? control, bool markHovered = false)
	{
		if (focusSource == FocusSource.Keyboard)
		{
			KeyboardFocus = control;
		}
		else
		{
			MouseFocus = control;
		}
		InputHandler.FocusChanged?.Invoke(control, focusSource);
	}

	public static bool IsMouseButtonDown(MouseButton mouseButton)
	{
		return KeyData.IsMouseButtonDown(mouseButton);
	}

	public static bool IsKeyDown(Key key)
	{
		return KeyData.KeyState[(int)key];
	}

	public static bool DoSpecialKeys(Base canvas, char chr, Keys msgKey)
	{
		if (KeyboardFocus == null)
		{
			return false;
		}
		if (KeyboardFocus.Canvas != canvas)
		{
			return false;
		}
		if (!KeyboardFocus.IsVisibleInTree)
		{
			return false;
		}
		if (IsControlDown)
		{
			switch (chr)
			{
			case 'C':
			case 'c':
				KeyboardFocus.InputCopy(null);
				return true;
			case 'V':
			case 'v':
				KeyboardFocus.InputPaste(null);
				return true;
			case 'X':
			case 'x':
				KeyboardFocus.InputCut(null);
				return true;
			case 'A':
			case 'a':
				KeyboardFocus.InputSelectAll(null);
				return true;
			default:
				return false;
			}
		}
		return false;
	}

	public static bool HandleAccelerator(Base canvas, char chr)
	{
		StringBuilder stringBuilder = new StringBuilder();
		if (IsControlDown)
		{
			stringBuilder.Append("CTRL+");
		}
		if (IsShiftDown)
		{
			stringBuilder.Append("SHIFT+");
		}
		stringBuilder.Append(chr);
		string accelerator = stringBuilder.ToString();
		if (KeyboardFocus != null && KeyboardFocus.HandleAccelerator(accelerator))
		{
			return true;
		}
		if (MouseFocus != null && MouseFocus.HandleAccelerator(accelerator))
		{
			return true;
		}
		if (canvas.HandleAccelerator(accelerator))
		{
			return true;
		}
		return false;
	}

	public static void OnMouseMoved(Canvas canvas, int x, int y, int dx, int dy)
	{
		MousePosition.X = x;
		MousePosition.Y = y;
		UpdateHoveredControl(canvas);
	}

	public static void OnCanvasThink(Canvas canvas)
	{
		Base mouseFocus = MouseFocus;
		if (mouseFocus != null && !mouseFocus.IsVisibleInTree)
		{
			MouseFocus = null;
		}
		if (KeyboardFocus != null && KeyboardFocus.IsVisibleInTree)
		{
			_ = KeyboardFocus.KeyboardInputEnabled;
		}
		if (KeyboardFocus == null || KeyboardFocus.Canvas != canvas)
		{
			return;
		}
		float timeInSeconds = Neutral.GetTimeInSeconds();
		for (int i = 0; i < 16; i++)
		{
			if (KeyData.KeyState[i] && KeyData.Target != KeyboardFocus)
			{
				KeyData.KeyState[i] = false;
			}
			else if (KeyData.KeyState[i] && timeInSeconds > KeyData.NextRepeat[i])
			{
				KeyData.NextRepeat[i] = Neutral.GetTimeInSeconds() + KeyRepeatRate;
				if (KeyboardFocus != null)
				{
					KeyboardFocus.InputKeyPressed((Key)i);
				}
			}
		}
		UpdateHoveredControl(canvas);
	}

	public static bool OnMouseButtonStateChanged(Base canvas, MouseButton mouseButton, bool pressed)
	{
		Base hoveredControl = HoveredControl;
		if (pressed && (hoveredControl == null || !hoveredControl.IsMenuComponent))
		{
			canvas.CloseMenus();
		}
		if (hoveredControl == null || hoveredControl == canvas)
		{
			return false;
		}
		if (hoveredControl.Canvas != canvas)
		{
			return false;
		}
		if (!hoveredControl.IsVisibleInTree)
		{
			return false;
		}
		if (!Enum.IsDefined(mouseButton))
		{
			return false;
		}
		Point mousePosition = MousePosition;
		if (KeyData.SetMouseButtonState(mouseButton, pressed))
		{
			hoveredControl.InputMouseButtonState(MouseButton.Left, mousePosition, pressed);
		}
		bool flag = pressed && _lastClickPosition == mousePosition && GetDeltaClickTime(mouseButton) < DoubleClickSpeed;
		if (pressed)
		{
			if (!flag)
			{
				LastClickTime[mouseButton] = Neutral.GetTimeInSeconds();
				_lastClickPosition = mousePosition;
			}
			FindKeyboardFocus(hoveredControl);
		}
		hoveredControl.UpdateCursor();
		if (pressed)
		{
			hoveredControl.Touch();
		}
		if (mouseButton == MouseButton.Left && DragAndDrop.OnMouseButton(hoveredControl, mousePosition.X, mousePosition.Y, pressed))
		{
			return true;
		}
		if (flag)
		{
			hoveredControl.InputMouseDoubleClicked(mouseButton, mousePosition);
		}
		return false;
	}

	private static float GetDeltaClickTime(MouseButton mouseButton)
	{
		return Neutral.GetTimeInSeconds() - LastClickTime.GetValueOrDefault(mouseButton, 0f);
	}

	public static bool OnMouseScroll(Base? canvas, int deltaX, int deltaY)
	{
		if (canvas == null || HoveredControl == null || HoveredControl.Canvas != canvas || !canvas.IsVisibleInTree)
		{
			return false;
		}
		if (deltaY != 0)
		{
			HoveredControl.InputMouseWheeled(deltaY);
		}
		if (deltaX != 0)
		{
			HoveredControl.InputMouseHWheeled(deltaX);
		}
		return true;
	}

	public static bool OnKeyEvent(Base canvas, Key key, bool down)
	{
		if (KeyboardFocus == null)
		{
			return false;
		}
		if (KeyboardFocus.Canvas != canvas)
		{
			return false;
		}
		if (!KeyboardFocus.IsVisibleInTree)
		{
			return false;
		}
		if (down)
		{
			if (!KeyData.KeyState[(int)key])
			{
				KeyData.KeyState[(int)key] = true;
				KeyData.NextRepeat[(int)key] = Neutral.GetTimeInSeconds() + KeyRepeatDelay;
				KeyData.Target = KeyboardFocus;
				return KeyboardFocus.InputKeyPressed(key);
			}
		}
		else if (KeyData.KeyState[(int)key])
		{
			KeyData.KeyState[(int)key] = false;
			return KeyboardFocus.InputKeyPressed(key, down: false);
		}
		return false;
	}

	private static void UpdateHoveredControl(Canvas canvas)
	{
		Base componentAt = canvas.GetComponentAt(MousePosition.X, MousePosition.Y);
		UpdateMouseFocus(canvas, componentAt, passing: false);
	}

	public static void PassMouseFocusTo(Canvas canvas, Base? nextHoveredControl)
	{
		MouseFocus = nextHoveredControl;
		UpdateMouseFocus(canvas, nextHoveredControl, passing: true);
	}

	private static void UpdateMouseFocus(Canvas canvas, Base? nextNode, bool passing)
	{
		Base obj = MouseFocus;
		if (obj?.Canvas != canvas)
		{
			obj = null;
		}
		nextNode = obj ?? nextNode;
		Base hoveredControl = HoveredControl;
		if (nextNode != hoveredControl)
		{
			HoveredControl = nextNode;
			hoveredControl?.Redraw();
			hoveredControl?.InputMouseLeft();
			if (nextNode != obj)
			{
				nextNode?.InputMouseEntered();
			}
			nextNode?.Redraw();
		}
		if (nextNode != null && (passing || !nextNode.KeepFocusOnMouseExit))
		{
			ImmutableArray<MouseButton>.Enumerator enumerator = MouseButtons.GetEnumerator();
			while (enumerator.MoveNext())
			{
				MouseButton current = enumerator.Current;
				bool pressed = KeyData.IsMouseButtonDown(current);
				nextNode.InputMouseButtonState(current, MousePosition, pressed);
			}
		}
	}

	private static void FindKeyboardFocus(Base control)
	{
		if (control == null)
		{
			return;
		}
		if (control.KeyboardInputEnabled)
		{
			if (!control.Children.Any((Base child) => child == KeyboardFocus))
			{
				control.Focus();
			}
		}
		else
		{
			FindKeyboardFocus(control.Parent);
		}
	}
}
