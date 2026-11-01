using System;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.Framework.Input;

namespace Intersect.Client.Framework.Gwen.Input;

public class IntersectInput : InputBase
{
	public enum InputEvent
	{
		MouseMove,
		MouseDown,
		MouseUp,
		KeyDown,
		KeyUp,
		TextEntered,
		MouseScroll
	}

	private Canvas mCanvas;

	private bool mInitialized;

	private int mMouseX;

	private int mMouseY;

	public override void Initialize(Canvas canvas)
	{
		mCanvas = canvas;
		mInitialized = true;
	}

	public override Key TranslateKeyCode(object sfKey)
	{
		return (Keys)sfKey switch
		{
			Keys.Back => Key.Backspace, 
			Keys.Return => Key.Return, 
			Keys.Escape => Key.Escape, 
			Keys.Tab => Key.Tab, 
			Keys.Space => Key.Space, 
			Keys.Up => Key.Up, 
			Keys.Down => Key.Down, 
			Keys.Left => Key.Left, 
			Keys.Right => Key.Right, 
			Keys.Home => Key.Home, 
			Keys.End => Key.End, 
			Keys.Delete => Key.Delete, 
			Keys.Control => Key.Control, 
			Keys.Alt => Key.Alt, 
			Keys.Shift => Key.Shift, 
			_ => Key.Invalid, 
		};
	}

	public override char TranslateChar(object sfKey)
	{
		Keys keys = (Keys)sfKey;
		if (keys >= Keys.A && keys <= Keys.Z)
		{
			return (char)(65 + (keys - 65));
		}
		return ' ';
	}

	public override bool ProcessMessage(object message)
	{
		if (!mInitialized || !HandleInput)
		{
			return false;
		}
		GwenInputMessage gwenInputMessage = (GwenInputMessage)message;
		switch (gwenInputMessage.Type)
		{
		case InputEvent.MouseMove:
		{
			int dx = (int)gwenInputMessage.MousePosition.X - mMouseX;
			int dy = (int)gwenInputMessage.MousePosition.Y - mMouseY;
			mMouseX = (int)gwenInputMessage.MousePosition.X;
			mMouseY = (int)gwenInputMessage.MousePosition.Y;
			return mCanvas.Input_MouseMoved(mMouseX, mMouseY, dx, dy);
		}
		case InputEvent.MouseDown:
			return mCanvas.Input_MouseButton(gwenInputMessage.MouseBtn, down: true);
		case InputEvent.MouseUp:
			return mCanvas.Input_MouseButton(gwenInputMessage.MouseBtn, down: false);
		case InputEvent.TextEntered:
			return mCanvas.Input_Character(gwenInputMessage.Unicode[0]);
		case InputEvent.KeyDown:
		{
			char chr = TranslateChar(gwenInputMessage.Key);
			if (gwenInputMessage.MouseBtn < MouseButton.Left && InputHandler.DoSpecialKeys(mCanvas, chr, gwenInputMessage.Key))
			{
				return false;
			}
			Key key = TranslateKeyCode(gwenInputMessage.Key);
			if (key == Key.Invalid || key == Key.Space)
			{
				return InputHandler.HandleAccelerator(mCanvas, chr);
			}
			return mCanvas.Input_Key(key, down: true, gwenInputMessage.Shift);
		}
		case InputEvent.KeyUp:
		{
			Key key = TranslateKeyCode(gwenInputMessage.Key);
			return mCanvas.Input_Key(key, down: false, gwenInputMessage.Shift);
		}
		case InputEvent.MouseScroll:
			return mCanvas.Input_MouseScroll((int)gwenInputMessage.MousePosition.X, (int)gwenInputMessage.MousePosition.Y);
		default:
			throw new ArgumentOutOfRangeException();
		}
	}
}
