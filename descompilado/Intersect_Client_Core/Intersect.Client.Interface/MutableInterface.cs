using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.Interface.Debugging;
using Intersect.Reflection;

namespace Intersect.Client.Interface;

public abstract class MutableInterface : IMutableInterface
{
	private static DebugWindow? _debugWindow;

	private static DebugWindow? _debugWindow2;

	internal Base Root { get; }

	public int NodeCount => Root.NodeCount;

	public IReadOnlyList<Base> Children => Root.Children ?? Array.Empty<Base>();

	public static void ReparentDebugWindow(Base parent)
	{
		DebugWindow debugWindow = _debugWindow;
		if (debugWindow != null)
		{
			debugWindow.Parent = parent;
		}
		DebugWindow debugWindow2 = _debugWindow2;
		if (debugWindow2 != null)
		{
			debugWindow2.Parent = parent;
		}
	}

	public static void DetachDebugWindow()
	{
		if (_debugWindow != null)
		{
			_debugWindow.Parent = null;
		}
		if (_debugWindow2 != null)
		{
			_debugWindow2.Parent = null;
		}
	}

	internal static void DisposeDebugWindow()
	{
		_debugWindow?.Dispose();
		_debugWindow2?.Dispose();
	}

	private static DebugWindow EnsureDebugWindowInitialized(Base parent)
	{
		if (_debugWindow == null)
		{
			_debugWindow = new DebugWindow(parent);
		}
		_debugWindow.Parent = parent;
		return _debugWindow;
	}

	protected internal MutableInterface(Base root)
	{
		Root = root;
	}

	public TElement Create<TElement>(params object[] parameters) where TElement : Base
	{
		List<object> list = new List<object> { Root };
		list.AddRange(parameters);
		object[] array = list.ToArray();
		ConstructorInfo constructorInfo = typeof(TElement).FindConstructors(array).FirstOrDefault();
		if (constructorInfo != null)
		{
			ParameterInfo[] parameters2 = constructorInfo.GetParameters();
			if (array.Length != parameters2.Length)
			{
				List<object> list2 = new List<object>();
				list2.AddRange(array);
				list2.AddRange(from _ in Enumerable.Range(0, parameters2.Length - array.Length)
					select (object)null);
				array = list2.ToArray();
			}
			if (constructorInfo.Invoke(array) is TElement result)
			{
				return result;
			}
			throw new NullReferenceException("Failed to invoke constructor that matches parameters.");
		}
		throw new NullReferenceException("Failed to find constructor that matches parameters.");
	}

	public TElement? Find<TElement>(string? name = null, bool recurse = false) where TElement : Base
	{
		if (string.IsNullOrWhiteSpace(name))
		{
			return Root.Find((Base child) => child is TElement) as TElement;
		}
		return Root.FindChildByName(name, recurse) as TElement;
	}

	public IEnumerable<TElement?> FindAll<TElement>(bool recurse = false) where TElement : Base
	{
		return from child in Root.FindAll((Base child) => child is TElement, recurse)
			select child as TElement;
	}

	public void Remove<TElement>(TElement element, bool dispose = false) where TElement : Base
	{
		Root.RemoveChild(element, dispose);
	}

	public bool ToggleDebug()
	{
		DebugWindow debugWindow = EnsureDebugWindowInitialized(Root);
		debugWindow.ToggleHidden();
		DebugWindow debugWindow2 = _debugWindow2;
		if (debugWindow2 != null)
		{
			debugWindow2.IsVisibleInTree = debugWindow.IsVisibleInTree;
		}
		return debugWindow.IsVisibleInTree;
	}
}
