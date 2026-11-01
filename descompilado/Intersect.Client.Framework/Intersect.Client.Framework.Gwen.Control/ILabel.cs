using Intersect.Client.Framework.Graphics;

namespace Intersect.Client.Framework.Gwen.Control;

public interface ILabel : IAutoSizeToContents, IColorableText, ITextContainer
{
	Pos TextAlign { get; set; }

	IFont? Font { get; set; }

	string? FontName { get; set; }

	int FontSize { get; set; }

	bool SizeToContents();

	void UpdateColors();
}
