using System;
using System.Collections.Generic;
using Intersect.Client.Framework.Gwen.Control;

namespace Intersect.Client.Interface.Menu;

public class MenuGuiBase : IMutableInterface
{
	private readonly Canvas _menuCanvas;

	private bool _shouldReset;

	public MainMenu MainMenu { get; }

	public IReadOnlyList<Base> Children => MainMenu.Children;

	public MenuGuiBase(Canvas myCanvas)
	{
		_menuCanvas = myCanvas;
		MainMenu = new MainMenu(_menuCanvas);
	}

	public void Update(TimeSpan elapsed, TimeSpan total)
	{
		if (_shouldReset)
		{
			MainMenu.Reset();
			_shouldReset = false;
		}
		MainMenu.Update(elapsed, total);
	}

	public void Draw(TimeSpan elapsed, TimeSpan total)
	{
		_menuCanvas.RenderCanvas(elapsed, total);
	}

	public void Reset()
	{
		_shouldReset = true;
	}

	public void Dispose()
	{
		MainMenu.DisposeScene();
		_menuCanvas.Dispose();
	}

	public TElement Create<TElement>(params object[] parameters) where TElement : Base
	{
		return MainMenu.Create<TElement>(parameters);
	}

	public TElement? Find<TElement>(string? name = null, bool recurse = false) where TElement : Base
	{
		return MainMenu.Find<TElement>(name, recurse);
	}

	public IEnumerable<TElement?> FindAll<TElement>(bool recurse = false) where TElement : Base
	{
		return MainMenu.FindAll<TElement>(recurse);
	}

	public void Remove<TElement>(TElement element, bool dispose = false) where TElement : Base
	{
		MainMenu.Remove(element, dispose);
	}
}
