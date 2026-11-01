using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.Housing;

namespace Intersect.Client.Interface.Game.Housing;

internal static class HouseInterface
{
	private static Canvas? _canvas;

	private static HouseOverlay? _overlay;

	private static HouseHud? _hud;

	private static HouseBuyWindow? _buy;

	private static HouseDoorWindow? _door;

	private static DecorationScreen? _decoration;

	private static bool Ready(out Canvas canvas)
	{
		canvas = null;
		if (Interface.HasInGameUI)
		{
			Canvas canvas2 = Interface.GameUi?.GameCanvas;
			if (canvas2 != null)
			{
				if (canvas2 != _canvas)
				{
					_canvas = canvas2;
					_overlay = new HouseOverlay(canvas2);
					_overlay.SendToBack();
					_hud = new HouseHud(canvas2);
					_buy = null;
					_door = null;
					_decoration = null;
				}
				canvas = canvas2;
				return true;
			}
		}
		return false;
	}

	public static void Update()
	{
		if (Ready(out Canvas _))
		{
			_overlay?.Update();
			HouseHud? hud = _hud;
			HouseDoorWindow door;
			if (hud != null)
			{
				door = _door;
				hud.Update(door != null && !door.IsHidden);
			}
			DecorationScreen decoration = _decoration;
			if (decoration != null && !decoration.IsHidden && !Decoration.Active)
			{
				_decoration.Hide();
			}
			door = _door;
			if (door != null && !door.IsHidden && (!Houses.InHouse || !Houses.Interior.IsOwner))
			{
				_door.Hide();
			}
			HouseBuyWindow buy = _buy;
			if (buy != null && !buy.IsHidden && buy.IsUpgrade && (!Houses.InHouse || !Houses.Interior.IsOwner || !Houses.Interior.CanUpgrade))
			{
				_buy.Hide();
			}
		}
	}

	public static void OpenBuy(Houses.Plot plot)
	{
		if (Ready(out Canvas canvas))
		{
			if (_buy == null)
			{
				_buy = new HouseBuyWindow(canvas);
			}
			_buy.Open(plot);
		}
	}

	public static void OpenUpgrade()
	{
		if (Ready(out Canvas canvas))
		{
			_door?.Hide();
			if (_buy == null)
			{
				_buy = new HouseBuyWindow(canvas);
			}
			_buy.OpenUpgrade();
		}
	}

	public static void CloseBuy()
	{
		_buy?.Hide();
	}

	public static void RefreshBuy()
	{
		_buy?.Refresh();
	}

	public static void ToggleDoor()
	{
		if (Ready(out Canvas canvas))
		{
			if (_door == null)
			{
				_door = new HouseDoorWindow(canvas);
			}
			if (_door.IsHidden)
			{
				_door.Show();
			}
			else
			{
				_door.Hide();
			}
		}
	}

	public static void ShowDecoration(bool show)
	{
		if (!Ready(out Canvas canvas))
		{
			return;
		}
		if (show)
		{
			_door?.Hide();
			if (_decoration == null)
			{
				_decoration = new DecorationScreen(canvas);
			}
			_decoration.Show();
		}
		else
		{
			_decoration?.Hide();
		}
	}

	public static void CloseInside()
	{
		_door?.Hide();
		if (Decoration.Active)
		{
			Decoration.Stop();
		}
		_decoration?.Hide();
	}

	public static bool CloseOnEscape()
	{
		DecorationScreen decoration = _decoration;
		if (decoration != null && !decoration.IsHidden)
		{
			if (!Decoration.Drop())
			{
				Decoration.Stop();
			}
			return true;
		}
		HouseBuyWindow buy = _buy;
		if (buy != null && !buy.IsHidden)
		{
			_buy.Hide();
			return true;
		}
		HouseDoorWindow door = _door;
		if (door != null && !door.IsHidden)
		{
			_door.Hide();
			return true;
		}
		return false;
	}

	public static bool ConfirmOnEnter()
	{
		HouseBuyWindow buy = _buy;
		if (buy != null && !buy.IsHidden)
		{
			return buy.TryConfirm();
		}
		return false;
	}
}
