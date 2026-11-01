using System;

namespace Intersect.Client.Framework.Gwen.Control.Data;

public class CellDataChangedEventArgs : EventArgs
{
	public object? OldValue { get; }

	public object? NewValue { get; }

	public CellDataChangedEventArgs(object? oldValue, object? newValue)
	{
		OldValue = oldValue;
		NewValue = newValue;
	}
}
