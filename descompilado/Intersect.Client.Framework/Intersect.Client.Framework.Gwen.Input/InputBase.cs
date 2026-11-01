using Intersect.Client.Framework.Gwen.Control;

namespace Intersect.Client.Framework.Gwen.Input;

public abstract class InputBase
{
	public bool HandleInput = true;

	private Canvas mCanvas;

	private int mMouseX;

	private int mMouseY;

	public InputBase()
	{
		mMouseX = 0;
		mMouseY = 0;
	}

	public abstract void Initialize(Canvas canvas);

	public abstract Key TranslateKeyCode(object sfKey);

	public abstract char TranslateChar(object sfKey);

	public abstract bool ProcessMessage(object message);
}
