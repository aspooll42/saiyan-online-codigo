using System;

namespace Intersect.Client.Framework.Gwen.Control.EventArguments;

public class VisibilityChangedEventArgs(bool isVisible) : EventArgs()
{
	public bool IsVisible { get; init; } = isVisible;

	public bool IsVisibleInTree { get; init; }

	public VisibilityChangedEventArgs(bool isVisibleInParent, bool isVisibleInTree)
		: this(isVisibleInParent)
	{
		IsVisibleInTree = isVisibleInTree;
	}
}
