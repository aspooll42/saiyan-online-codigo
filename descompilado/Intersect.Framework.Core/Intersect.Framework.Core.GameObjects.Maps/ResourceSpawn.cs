using System;

namespace Intersect.Framework.Core.GameObjects.Maps;

public class ResourceSpawn
{
	public Guid ResourceId;

	public byte X;

	public byte Y;

	public byte Z;

	public Guid Id { get; set; } = Guid.NewGuid();
}
