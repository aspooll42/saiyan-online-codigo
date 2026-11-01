using Intersect.Client.Framework.Gwen.Control;

namespace Intersect.Client.Interface.Game.Enseignements;

internal static class TrialInterface
{
	private static Canvas? _canvas;

	private static TrialHud? _hud;

	internal static Base? Hud => _hud;

	public static void Update()
	{
		if (!Interface.HasInGameUI)
		{
			return;
		}
		Canvas canvas = Interface.GameUi?.GameCanvas;
		if (canvas != null)
		{
			if (canvas != _canvas)
			{
				_canvas = canvas;
				_hud = new TrialHud(canvas);
			}
			_hud?.Update();
		}
	}
}
