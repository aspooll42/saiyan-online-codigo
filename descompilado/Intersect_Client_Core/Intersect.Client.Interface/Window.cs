using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.Framework.Gwen.Skin;

namespace Intersect.Client.Interface;

public abstract class Window : WindowControl
{
	private delegate void Initializer(Intersect.Client.Framework.Gwen.Skin.Base skin);

	private readonly object _initializationLock = new object();

	private volatile bool _initialized;

	private Initializer? _initializer;

	protected Window(Intersect.Client.Framework.Gwen.Control.Base parent, string? title = null, bool modal = false, string? name = null)
		: base(parent, title, modal, name)
	{
		_initializer = Initialize;
		SetTextColor(Color.White, ControlState.Active);
		SetTextColor(new Color(255, 191, 191, 191), ControlState.Inactive);
		SkipRender();
	}

	protected override void OnPreDraw(Intersect.Client.Framework.Gwen.Skin.Base skin)
	{
		base.OnPreDraw(skin);
		Initializer initializer = _initializer;
		if (initializer != null)
		{
			_initializer = null;
			initializer(skin);
		}
	}

	private void Initialize(Intersect.Client.Framework.Gwen.Skin.Base skin)
	{
		lock (_initializationLock)
		{
			EnsureInitialized();
			DoLayoutIfNeeded(skin);
		}
	}

	protected abstract void EnsureInitialized();
}
