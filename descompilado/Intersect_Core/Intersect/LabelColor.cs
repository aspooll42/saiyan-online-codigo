namespace Intersect;

public struct LabelColor
{
	public Color Name;

	public Color Outline;

	public Color Background;

	public LabelColor(Color nameColor, Color outlineColor, Color backgroundColor)
	{
		Name = nameColor;
		Outline = outlineColor;
		Background = backgroundColor;
	}
}
