namespace Intersect.Client.Framework.Graphics;

public enum VertexComponent
{
	Position = 2,
	Color = 4,
	TextureUV = 8,
	Normal = 0x10,
	Binormal = 0x20,
	Tangent = 0x40,
	BlendIndices = 0x80,
	BlendWeight = 0x100,
	Depth = 0x200,
	Fog = 0x400,
	PointSize = 0x800
}
