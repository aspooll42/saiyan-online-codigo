namespace Intersect.Client.Framework.Gwen.Control.Data;

public class RowDataChangedEventArgs : CellDataChangedEventArgs
{
	public int Column { get; }

	public RowDataChangedEventArgs(int column, object oldValue, object newValue)
		: base(oldValue, newValue)
	{
		Column = column;
	}
}
