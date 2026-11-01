namespace Intersect.Client.Framework.Input;

public abstract class GameClipboard
{
	public static GameClipboard Instance;

	public abstract bool IsEmpty { get; }

	public abstract bool IsEnabled { get; }

	public abstract void SetText(string data);

	public abstract string GetText();
}
