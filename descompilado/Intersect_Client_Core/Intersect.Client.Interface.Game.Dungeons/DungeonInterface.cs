using Intersect.Client.Dungeons;
using Intersect.Client.Entities;
using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.General;
using Intersect.Framework.Core.GameObjects.Dungeons;

namespace Intersect.Client.Interface.Game.Dungeons;

internal static class DungeonInterface
{
	private static Canvas? _canvas;

	private static DungeonOverlay? _overlay;

	private static DungeonHud? _hud;

	private static DungeonEntryWindow? _entry;

	private static DungeonEndWindow? _end;

	internal static DungeonHud? Hud => _hud;

	internal static Base? Overlay => _overlay;

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
					_overlay = new DungeonOverlay(canvas2);
					_overlay.SendToBack();
					_hud = new DungeonHud(canvas2);
					_entry = null;
					_end = null;
				}
				canvas = canvas2;
				return true;
			}
		}
		return false;
	}

	public static void Update()
	{
		if (!Ready(out Canvas _))
		{
			return;
		}
		_overlay?.Update();
		_hud?.Update();
		DungeonEntryWindow entry = _entry;
		if (entry != null && !entry.IsHidden)
		{
			Player me = Globals.Me;
			if (me == null || Dungeon.InRun || Dungeon.EntranceNear(me)?.Id != entry.DungeonId)
			{
				entry.Hide();
			}
		}
	}

	public static void OpenEntry(DungeonDescriptor dungeon)
	{
		if (Ready(out Canvas canvas))
		{
			if (_entry == null)
			{
				_entry = new DungeonEntryWindow(canvas);
			}
			_entry.Open(dungeon);
		}
	}

	public static void CloseEntry()
	{
		_entry?.Hide();
	}

	public static void OpenEnd()
	{
		if (Ready(out Canvas canvas))
		{
			_entry?.Hide();
			if (_end == null)
			{
				_end = new DungeonEndWindow(canvas);
			}
			_end.Show();
		}
	}

	public static void CloseInside()
	{
		_entry?.Hide();
	}

	public static bool CloseOnEscape()
	{
		DungeonEntryWindow entry = _entry;
		if (entry != null && !entry.IsHidden)
		{
			_entry.Hide();
			return true;
		}
		DungeonEndWindow end = _end;
		if (end != null && !end.IsHidden)
		{
			_end.Hide();
			return true;
		}
		return false;
	}

	public static bool ConfirmOnEnter()
	{
		DungeonEntryWindow entry = _entry;
		if (entry != null && !entry.IsHidden)
		{
			return entry.TryConfirm();
		}
		return false;
	}
}
