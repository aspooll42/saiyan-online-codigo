namespace Intersect.Client.Framework.GenericClasses;

public static class Cursors
{
	public enum CursorTypes
	{
		Default,
		SizeAll,
		SizeNs,
		SizeWe,
		SizeNwse,
		SizeNesw,
		No
	}

	public static Cursor Default => new Cursor(CursorTypes.Default);

	public static Cursor SizeNs => new Cursor(CursorTypes.SizeNs);

	public static Cursor SizeWe => new Cursor(CursorTypes.SizeWe);

	public static Cursor SizeAll => new Cursor(CursorTypes.SizeAll);

	public static Cursor SizeNwse => new Cursor(CursorTypes.SizeNwse);

	public static Cursor SizeNesw => new Cursor(CursorTypes.SizeNesw);

	public static Cursor No => new Cursor(CursorTypes.No);
}
