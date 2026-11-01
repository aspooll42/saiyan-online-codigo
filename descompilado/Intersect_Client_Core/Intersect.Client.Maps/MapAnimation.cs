using System;
using Intersect.Client.Entities;
using Intersect.Client.Framework.Entities;
using Intersect.Client.Framework.Maps;
using Intersect.Enums;
using Intersect.Framework.Core.GameObjects.Animations;

namespace Intersect.Client.Maps;

public class MapAnimation : Animation, IMapAnimation, IAnimation, IDisposable
{
	private Direction mDir;

	private int mTileX;

	private int mTileY;

	public Guid Id { get; } = Guid.NewGuid();

	public MapAnimation(AnimationDescriptor animationDescriptor, int tileX, int tileY, Direction dir, IEntity? owner = null, AnimationSource source = default(AnimationSource))
		: base(animationDescriptor, loopForever: false, autoRotate: false, -1, owner, source)
	{
		mTileX = tileX;
		mTileY = tileY;
		mDir = dir;
	}
}
