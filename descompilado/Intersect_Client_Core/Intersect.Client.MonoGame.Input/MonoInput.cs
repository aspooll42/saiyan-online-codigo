using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Intersect.Client.Core;
using Intersect.Client.Core.Controls;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.Framework.Gwen.Input;
using Intersect.Client.Framework.Input;
using Intersect.Client.General;
using Intersect.Client.Interface;
using Intersect.Client.MonoGame.Graphics;
using Intersect.Client.ThirdParty;
using Intersect.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Intersect.Client.MonoGame.Input;

public class MonoInput : GameInput
{
	private readonly Dictionary<Intersect.Client.Framework.GenericClasses.Keys, Microsoft.Xna.Framework.Input.Keys> _intersectToMonoGameKeyMap;

	private GamePadState _currentGamePadState;

	private GamePadState _previousGamePadState;

	private KeyboardState _currentKeyboardState;

	private KeyboardState _previousKeyboardState;

	private MouseState _currentMouseState;

	private MouseState _previousMouseState;

	private int mMouseX;

	private int mMouseY;

	private int mMouseVScroll;

	private int mMouseHScroll;

	private Game mMyGame;

	private readonly GamePadCapabilities[] _gamePadCapabilities = new GamePadCapabilities[GamePad.MaximumGamePadCount];

	private int _activeGamePad;

	private float _curseurResteX;

	private float _curseurResteY;

	private (int X, int Y) _sourisAttendue = (X: -1, Y: -1);

	private long _curseurManetteMs;

	private int _moletteManette;

	private bool _keyboardOpened;

	private IControlSet _controlSet;

	private static readonly TimeSpan ZoomDelay = TimeSpan.FromMilliseconds(300.0);

	private DateTime _lastZoomTime = DateTime.MinValue;

	public override InputDeviceType CursorMovementDevice { get; set; }

	public override IControlSet ControlSet
	{
		get
		{
			return _controlSet;
		}
		set
		{
			_controlSet = value;
		}
	}

	public override bool MouseHitInterface => Intersect.Client.Interface.Interface.DoesMouseHitInterface();

	public override bool IsMouseInBounds => Intersect.Client.Interface.Interface.IsInBounds(_currentMouseState.X, _currentMouseState.Y);

	public MonoInput(Game myGame)
	{
#if !ANDROID
		myGame.Window.TextInput += Window_TextInput;
#endif
		mMyGame = myGame;
		_intersectToMonoGameKeyMap = new Dictionary<Intersect.Client.Framework.GenericClasses.Keys, Microsoft.Xna.Framework.Input.Keys>();
		Intersect.Client.Framework.GenericClasses.Keys[] values = Enum.GetValues<Intersect.Client.Framework.GenericClasses.Keys>();
		for (int i = 0; i < values.Length; i++)
		{
			Intersect.Client.Framework.GenericClasses.Keys keys = values[i];
			if (!_intersectToMonoGameKeyMap.ContainsKey(keys))
			{
				Microsoft.Xna.Framework.Input.Keys[] values2 = Enum.GetValues<Microsoft.Xna.Framework.Input.Keys>();
				for (int j = 0; j < values2.Length; j++)
				{
					Microsoft.Xna.Framework.Input.Keys value = values2[j];
					if (keys == Intersect.Client.Framework.GenericClasses.Keys.Shift)
					{
						_intersectToMonoGameKeyMap.Add(keys, Microsoft.Xna.Framework.Input.Keys.LeftShift);
						break;
					}
					if ((keys == Intersect.Client.Framework.GenericClasses.Keys.LControlKey || keys == Intersect.Client.Framework.GenericClasses.Keys.Control) ? true : false)
					{
						_intersectToMonoGameKeyMap.Add(keys, Microsoft.Xna.Framework.Input.Keys.LeftControl);
						break;
					}
					if (keys == Intersect.Client.Framework.GenericClasses.Keys.RControlKey)
					{
						_intersectToMonoGameKeyMap.Add(keys, Microsoft.Xna.Framework.Input.Keys.RightControl);
						break;
					}
					if ((keys == Intersect.Client.Framework.GenericClasses.Keys.LMenu || keys == Intersect.Client.Framework.GenericClasses.Keys.Alt) ? true : false)
					{
						_intersectToMonoGameKeyMap.Add(keys, Microsoft.Xna.Framework.Input.Keys.LeftAlt);
						break;
					}
					switch (keys)
					{
					case Intersect.Client.Framework.GenericClasses.Keys.RMenu:
						_intersectToMonoGameKeyMap.Add(keys, Microsoft.Xna.Framework.Input.Keys.RightAlt);
						break;
					case Intersect.Client.Framework.GenericClasses.Keys.Return:
						_intersectToMonoGameKeyMap.Add(keys, Microsoft.Xna.Framework.Input.Keys.Enter);
						break;
					default:
						if (keys.ToString() != value.ToString())
						{
							continue;
						}
						_intersectToMonoGameKeyMap.Add(keys, value);
						break;
					}
					break;
				}
			}
			if (!_intersectToMonoGameKeyMap.ContainsKey(keys))
			{
				ApplicationContext.Context.Value?.Logger.LogDebug("No matching MonoGame key for {IntersectKey}", keys);
			}
		}
		_controlSet = new Controls();
		InputHandler.FocusChanged += InputHandlerOnFocusChanged;
	}

	private void InputHandlerOnFocusChanged(Base? control, FocusSource focusSource)
	{
		if (control != null && focusSource != FocusSource.Mouse && CursorMovementDevice != InputDeviceType.Mouse)
		{
			Microsoft.Xna.Framework.Vector2 vector = new Microsoft.Xna.Framework.Vector2((float)(control.GlobalBounds.Left + control.GlobalBounds.Right) / 2f, (float)(control.GlobalBounds.Bottom + control.GlobalBounds.Top) / 2f);
			float echelleDeLInterface = Intersect.Client.Core.Graphics.EchelleDeLInterface;
			Mouse.SetPosition((int)(vector.X * echelleDeLInterface), (int)(vector.Y * echelleDeLInterface));
			MouseState state = Mouse.GetState();
			Intersect.Client.Interface.Interface.GwenInput.ProcessMessage(new GwenInputMessage(IntersectInput.InputEvent.MouseMove, new System.Numerics.Vector2((float)state.X / echelleDeLInterface, (float)state.Y / echelleDeLInterface), MouseButton.None, Intersect.Client.Framework.GenericClasses.Keys.Alt));
		}
	}

	private System.Numerics.Vector2 SourisPourLInterface()
	{
		float echelleDeLInterface = Intersect.Client.Core.Graphics.EchelleDeLInterface;
		if (echelleDeLInterface == 1f)
		{
			return new System.Numerics.Vector2(mMouseX, mMouseY);
		}
		return new System.Numerics.Vector2((float)mMouseX / echelleDeLInterface, (float)mMouseY / echelleDeLInterface);
	}

	private void Window_TextInput(object sender, TextInputEventArgs e)
	{
		Intersect.Client.Interface.Interface.GwenInput.ProcessMessage(new GwenInputMessage(IntersectInput.InputEvent.TextEntered, SourisPourLInterface(), MouseButton.None, Intersect.Client.Framework.GenericClasses.Keys.Alt, alt: false, control: false, shift: false, e.Character.ToString()));
	}

	private bool MouseButtonDown(MouseState mouseState, MouseButton mb)
	{
		return mb switch
		{
			MouseButton.Left => mouseState.LeftButton == ButtonState.Pressed, 
			MouseButton.Right => mouseState.RightButton == ButtonState.Pressed, 
			MouseButton.Middle => mouseState.MiddleButton == ButtonState.Pressed, 
			MouseButton.X1 => mouseState.XButton1 == ButtonState.Pressed, 
			MouseButton.X2 => mouseState.XButton2 == ButtonState.Pressed, 
			_ => throw new ArgumentOutOfRangeException("mb", mb, null), 
		};
	}

	public override bool IsMouseButtonDown(MouseButton mb)
	{
		return MouseButtonDown(_currentMouseState, mb);
	}

	public override bool WasMouseButtonDown(MouseButton mb)
	{
		return MouseButtonDown(_previousMouseState, mb);
	}

	public override bool IsKeyDown(Intersect.Client.Framework.GenericClasses.Keys key)
	{
		if (_intersectToMonoGameKeyMap.TryGetValue(key, out var value))
		{
			return _currentKeyboardState.IsKeyDown(value);
		}
		return false;
	}

	public override bool WasKeyDown(Intersect.Client.Framework.GenericClasses.Keys key)
	{
		if (_intersectToMonoGameKeyMap.TryGetValue(key, out var value))
		{
			return _previousKeyboardState.IsKeyDown(value);
		}
		return false;
	}

	public override System.Numerics.Vector2 GetMousePosition()
	{
		return new System.Numerics.Vector2(mMouseX, mMouseY);
	}

	private void CheckMouseButton(Intersect.Client.Framework.GenericClasses.Keys modifier, ButtonState bs, MouseButton mb)
	{
		if (Globals.GameState == GameStates.Intro)
		{
			return;
		}
		if (bs == ButtonState.Pressed && !IsMouseButtonDown(mb))
		{
			if (!Intersect.Client.Core.Input.TestInterceptMouse(modifier, mb, down: true))
			{
				Intersect.Client.Interface.Interface.GwenInput.ProcessMessage(new GwenInputMessage(IntersectInput.InputEvent.MouseDown, SourisPourLInterface(), mb, Intersect.Client.Framework.GenericClasses.Keys.Alt));
				Intersect.Client.Core.Input.OnMouseDown(modifier, mb);
			}
		}
		else if (bs == ButtonState.Released && IsMouseButtonDown(mb) && !Intersect.Client.Core.Input.TestInterceptMouse(modifier, mb, down: false))
		{
			Intersect.Client.Interface.Interface.GwenInput.ProcessMessage(new GwenInputMessage(IntersectInput.InputEvent.MouseUp, SourisPourLInterface(), mb, Intersect.Client.Framework.GenericClasses.Keys.Alt));
			Intersect.Client.Core.Input.OnMouseUp(modifier, mb);
		}
	}

	private void CheckMouseScrollWheel(int scrlVValue, int scrlHValue)
	{
		System.Numerics.Vector2 vector = default(System.Numerics.Vector2);
		if (scrlVValue == mMouseVScroll && scrlHValue == mMouseHScroll)
		{
			return;
		}
		vector = new System.Numerics.Vector2(scrlHValue - mMouseHScroll, scrlVValue - mMouseVScroll);
		Intersect.Client.Interface.Interface.GwenInput.ProcessMessage(new GwenInputMessage(IntersectInput.InputEvent.MouseScroll, vector, MouseButton.Middle, Intersect.Client.Framework.GenericClasses.Keys.Alt));
		int num = Math.Sign(mMouseVScroll - scrlVValue);
		DateTime now = DateTime.Now;
		if (now - _lastZoomTime >= ZoomDelay && (InputHandler.HoveredControl == null || !InputHandler.HoveredControl.IsVisibleInTree || (InputHandler.HoveredControl is Canvas canvas && canvas == canvas.Canvas)) && Globals.GameState == GameStates.InGame && Globals.Database.EnableScrollingWorldZoom)
		{
			if (num < 0)
			{
				Intersect.Client.Core.Input.HandleZoomIn(wrap: false);
			}
			else
			{
				Intersect.Client.Core.Input.HandleZoomOut(wrap: false);
			}
			_lastZoomTime = now;
		}
		mMouseVScroll = scrlVValue;
		mMouseHScroll = scrlHValue;
	}

	public override void Update(TimeSpan elapsed)
	{
		if (mMyGame.IsActive)
		{
#if ANDROID
			Intersect.Client.Android.TecladoAndroid.Actualizar(SourisPourLInterface());
#endif
			if (_keyboardOpened)
			{
				Steam.PumpEvents();
			}
			GamePadCapabilities[] array = Enumerable.Range(0, GamePad.MaximumGamePadCount).Select(GamePad.GetCapabilities).ToArray();
			Array.Copy(array, _gamePadCapabilities, Math.Min(array.Length, _gamePadCapabilities.Length));
			int num = -1;
			for (int i = _activeGamePad; i < GamePad.MaximumGamePadCount; i++)
			{
				if (_gamePadCapabilities[i].IsConnected)
				{
					num = i;
					break;
				}
			}
			GamePadState gamePadState = ((num >= 0) ? GamePad.GetState(num) : default(GamePadState));
			if (num >= 0)
			{
				Manette.Nommer(_gamePadCapabilities[num].DisplayName);
			}
			Manette.Sortie sortie = Manette.Lire(gamePadState, _currentGamePadState, elapsed);
			bool flag = false;
			if (sortie.CurseurX != 0f || sortie.CurseurY != 0f)
			{
				_curseurResteX += sortie.CurseurX;
				_curseurResteY += sortie.CurseurY;
				int num2 = (int)_curseurResteX;
				int num3 = (int)_curseurResteY;
				_curseurResteX -= num2;
				_curseurResteY -= num3;
				if (num2 != 0 || num3 != 0)
				{
					CursorMovementDevice = InputDeviceType.Controller;
					MouseState state = Mouse.GetState();
					Microsoft.Xna.Framework.Rectangle clientBounds = mMyGame.Window.ClientBounds;
					int num4 = Math.Clamp(state.X + num2, 0, Math.Max(0, clientBounds.Width - 1));
					int num5 = Math.Clamp(state.Y + num3, 0, Math.Max(0, clientBounds.Height - 1));
					Mouse.SetPosition(num4, num5);
					_sourisAttendue = (X: num4, Y: num5);
					flag = true;
				}
			}
			KeyboardState keyboardState = Keyboard.GetState();
#if !ANDROID
			MouseState mouseState = Mouse.GetState();
#else
			MouseState mouseState = Intersect.Client.Android.TactilAndroid.Leer(Mouse.GetState());
#endif
			long tickCount = Environment.TickCount64;
			if (flag)
			{
				_curseurManetteMs = tickCount;
			}
			else if (sortie.Active || _sourisAttendue.X < 0)
			{
				_sourisAttendue = (X: mouseState.X, Y: mouseState.Y);
			}
			bool flag2 = (Math.Abs(mouseState.X - _sourisAttendue.X) > 3 || Math.Abs(mouseState.Y - _sourisAttendue.Y) > 3) && tickCount - _curseurManetteMs > 150;
			if (keyboardState.GetPressedKeys().Length != 0 || mouseState.LeftButton == ButtonState.Pressed || mouseState.RightButton == ButtonState.Pressed || flag2)
			{
				Manette.ClavierOuSouris();
				_sourisAttendue = (X: mouseState.X, Y: mouseState.Y);
			}
			if (mouseState.X != mMouseX || mouseState.Y != mMouseY)
			{
				mMouseX = (int)((float)mouseState.X * ((MonoRenderer)Intersect.Client.Core.Graphics.Renderer).GetMouseOffset().X);
				mMouseY = (int)((float)mouseState.Y * ((MonoRenderer)Intersect.Client.Core.Graphics.Renderer).GetMouseOffset().Y);
				CursorMovementDevice = InputDeviceType.Mouse;
				Intersect.Client.Interface.Interface.GwenInput.ProcessMessage(new GwenInputMessage(IntersectInput.InputEvent.MouseMove, SourisPourLInterface(), MouseButton.None, Intersect.Client.Framework.GenericClasses.Keys.Alt));
			}
			_moletteManette += sortie.Molette * 120;
			if (gamePadState.IsConnected || _moletteManette != 0)
			{
				mouseState = new MouseState(mouseState.X, mouseState.Y, mouseState.ScrollWheelValue + _moletteManette, sortie.Gauche ? ButtonState.Pressed : mouseState.LeftButton, mouseState.MiddleButton, sortie.Droit ? ButtonState.Pressed : mouseState.RightButton, mouseState.XButton1, mouseState.XButton2, mouseState.HorizontalScrollWheelValue);
			}
			if (sortie.Touches.Count > 0)
			{
				Microsoft.Xna.Framework.Input.Keys value;
				IEnumerable<Microsoft.Xna.Framework.Input.Keys> second = from touche in sortie.Touches
					select _intersectToMonoGameKeyMap.TryGetValue(touche, out value) ? value : Microsoft.Xna.Framework.Input.Keys.None into mappedKey
					where mappedKey != Microsoft.Xna.Framework.Input.Keys.None
					select mappedKey;
				keyboardState = new KeyboardState(keyboardState.GetPressedKeys().Concat(second).Distinct()
					.ToArray(), keyboardState.CapsLock, keyboardState.NumLock);
			}
#if ANDROID
			keyboardState = Intersect.Client.Android.ControlesTactiles.Mezclar(keyboardState, _intersectToMonoGameKeyMap);
#endif
			Intersect.Client.Framework.GenericClasses.Keys pressedModifier = GetPressedModifier(keyboardState);
			if (Intersect.Client.Interface.Interface.IsInBounds(mouseState.X, mouseState.Y))
			{
				CheckMouseButton(pressedModifier, mouseState.LeftButton, MouseButton.Left);
				CheckMouseButton(pressedModifier, mouseState.RightButton, MouseButton.Right);
				CheckMouseButton(pressedModifier, mouseState.MiddleButton, MouseButton.Middle);
				CheckMouseButton(pressedModifier, mouseState.XButton1, MouseButton.X1);
				CheckMouseButton(pressedModifier, mouseState.XButton2, MouseButton.X2);
				CheckMouseScrollWheel(mouseState.ScrollWheelValue, mouseState.HorizontalScrollWheelValue);
			}
			foreach (KeyValuePair<Intersect.Client.Framework.GenericClasses.Keys, Microsoft.Xna.Framework.Input.Keys> item in _intersectToMonoGameKeyMap)
			{
				if (keyboardState.IsKeyDown(item.Value) && !_currentKeyboardState.IsKeyDown(item.Value))
				{
					ApplicationContext.Context.Value?.Logger.LogTrace("{0} -> {1}", item.Key, item.Value);
					Intersect.Client.Interface.Interface.GwenInput.ProcessMessage(new GwenInputMessage(IntersectInput.InputEvent.KeyDown, SourisPourLInterface(), MouseButton.None, item.Key));
					Intersect.Client.Core.Input.OnKeyPressed(pressedModifier, item.Key);
				}
				else if (!keyboardState.IsKeyDown(item.Value) && _currentKeyboardState.IsKeyDown(item.Value))
				{
					Intersect.Client.Interface.Interface.GwenInput.ProcessMessage(new GwenInputMessage(IntersectInput.InputEvent.KeyUp, SourisPourLInterface(), MouseButton.None, item.Key));
					Intersect.Client.Core.Input.OnKeyReleased(pressedModifier, item.Key);
				}
			}
			GamePadState currentGamePadState = _currentGamePadState;
			GamePadState currentGamePadState2 = gamePadState;
			_previousGamePadState = currentGamePadState;
			_currentGamePadState = currentGamePadState2;
			KeyboardState currentKeyboardState = _currentKeyboardState;
			KeyboardState currentKeyboardState2 = keyboardState;
			_previousKeyboardState = currentKeyboardState;
			_currentKeyboardState = currentKeyboardState2;
			MouseState currentMouseState = _currentMouseState;
			MouseState currentMouseState2 = mouseState;
			_previousMouseState = currentMouseState;
			_currentMouseState = currentMouseState2;
			return;
		}
		Manette.Reinitialiser();
		Intersect.Client.Framework.GenericClasses.Keys pressedModifier2 = GetPressedModifier(_currentKeyboardState);
		foreach (KeyValuePair<Intersect.Client.Framework.GenericClasses.Keys, Microsoft.Xna.Framework.Input.Keys> item2 in _intersectToMonoGameKeyMap)
		{
			if (_currentKeyboardState.IsKeyDown(item2.Value))
			{
				Intersect.Client.Interface.Interface.GwenInput.ProcessMessage(new GwenInputMessage(IntersectInput.InputEvent.KeyUp, SourisPourLInterface(), MouseButton.None, item2.Key));
				Intersect.Client.Core.Input.OnKeyReleased(pressedModifier2, item2.Key);
			}
		}
		CheckMouseButton(pressedModifier2, ButtonState.Released, MouseButton.Left);
		CheckMouseButton(pressedModifier2, ButtonState.Released, MouseButton.Right);
		CheckMouseButton(pressedModifier2, ButtonState.Released, MouseButton.Middle);
		CheckMouseButton(pressedModifier2, ButtonState.Released, MouseButton.X1);
		CheckMouseButton(pressedModifier2, ButtonState.Released, MouseButton.X2);
		_currentGamePadState = default(GamePadState);
		_previousGamePadState = default(GamePadState);
		_currentKeyboardState = default(KeyboardState);
		_previousKeyboardState = default(KeyboardState);
		_currentMouseState = default(MouseState);
		_previousMouseState = default(MouseState);
	}

	public Intersect.Client.Framework.GenericClasses.Keys GetPressedModifier(KeyboardState state)
	{
		Intersect.Client.Framework.GenericClasses.Keys result = Intersect.Client.Framework.GenericClasses.Keys.None;
		if (state.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftControl) || state.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.RightControl))
		{
			result = Intersect.Client.Framework.GenericClasses.Keys.Control;
		}
		if (state.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftShift) || state.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.RightShift))
		{
			result = Intersect.Client.Framework.GenericClasses.Keys.Shift;
		}
		if (state.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftAlt) || state.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.RightAlt))
		{
			result = Intersect.Client.Framework.GenericClasses.Keys.Alt;
		}
		return result;
	}

	public override void OpenKeyboard(KeyboardType type, string text, bool autoCorrection, bool multiLine, bool secure)
	{
	}

	public override void OpenKeyboard(KeyboardType keyboardType, Action<string?> inputHandler, string description, string text, bool multiline = false, uint maxLength = 1024u, Intersect.Client.Framework.GenericClasses.Rectangle? inputBounds = null)
	{
		if (!OperatingSystem.IsMacOS() && !OperatingSystem.IsAndroid() && Steam.SteamDeck)
		{
			_keyboardOpened = Steam.ShowKeyboard(inputHandler, description, text, keyboardType == KeyboardType.Password, maxLength, inputBounds);
		}
	}
}
