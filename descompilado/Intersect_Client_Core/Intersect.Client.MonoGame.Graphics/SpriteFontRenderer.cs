using Intersect.Client.Framework.Graphics;
using Microsoft.Xna.Framework.Graphics;

namespace Intersect.Client.MonoGame.Graphics;

public sealed class SpriteFontRenderer : FontSizeRenderer<SpriteFont>
{
	public SpriteFontRenderer(SpriteFont platformObject)
		: base(platformObject)
	{
	}
}
