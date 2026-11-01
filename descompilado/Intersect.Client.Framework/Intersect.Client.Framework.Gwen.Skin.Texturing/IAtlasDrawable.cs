using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Gwen.Renderer;

namespace Intersect.Client.Framework.Gwen.Skin.Texturing;

public interface IAtlasDrawable
{
	void Draw(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Rectangle targetBounds, Color color);
}
