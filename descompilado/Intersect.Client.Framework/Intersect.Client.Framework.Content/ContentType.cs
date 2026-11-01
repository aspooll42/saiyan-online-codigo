using System;
using Intersect.Client.Framework.Audio;
using Intersect.Client.Framework.Graphics;

namespace Intersect.Client.Framework.Content;

public enum ContentType
{
	[AssetType(typeof(IGameTexture))]
	Animation = 0,
	[AssetType(typeof(IGameTexture))]
	Entity = 1,
	[AssetType(typeof(IGameTexture))]
	Face = 2,
	[AssetType(typeof(IGameTexture))]
	Fog = 3,
	[AssetType(typeof(IFont))]
	Font = 4,
	[AssetType(typeof(IGameTexture))]
	Image = 5,
	[AssetType(typeof(IGameTexture))]
	Interface = 6,
	[AssetType(typeof(IGameTexture))]
	Item = 7,
	[AssetType(typeof(IGameTexture))]
	Miscellaneous = 8,
	[AssetType(typeof(GameAudioSource))]
	Music = 9,
	[AssetType(typeof(IGameTexture))]
	Paperdoll = 10,
	[AssetType(typeof(IGameTexture))]
	Resource = 11,
	[AssetType(typeof(GameShader))]
	Shader = 12,
	[AssetType(typeof(GameAudioSource))]
	Sound = 13,
	[AssetType(typeof(IGameTexture))]
	Spell = 14,
	[AssetType(typeof(IGameTexture))]
	TextureAtlas = 15,
	[AssetType(typeof(IGameTexture))]
	Tileset = 16,
	[Obsolete]
	Gui = 6
}
