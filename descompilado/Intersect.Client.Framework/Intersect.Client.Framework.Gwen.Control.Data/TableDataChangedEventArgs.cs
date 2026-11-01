namespace Intersect.Client.Framework.Gwen.Control.Data;

public class TableDataChangedEventArgs : RowDataChangedEventArgs
{
	public int Row { get; }

	public TableDataChangedEventArgs(int row, int column, object? oldValue, object newValue)
		: base(column, oldValue, newValue)
	{
		Row = row;
	}
}
