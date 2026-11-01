namespace Intersect.Client.Framework.GenericClasses;

public class Cursor
{
	private int mType;

	public static Cursor Current { get; set; }

	public Cursor()
	{
	}

	public Cursor(Cursors.CursorTypes t)
	{
		mType = (int)t;
	}
}
