using System;
using MessagePack;

namespace Intersect.Framework.Core.GameObjects.Rankings;

[MessagePackObject(false)]
public sealed class RankingMemberInfo
{
	[Key(0)]
	public Guid PlayerId { get; set; }

	[Key(1)]
	public string Name { get; set; } = string.Empty;

	[Key(2)]
	public string Sprite { get; set; } = string.Empty;

	[Key(3)]
	public Guid ClassId { get; set; }
}
