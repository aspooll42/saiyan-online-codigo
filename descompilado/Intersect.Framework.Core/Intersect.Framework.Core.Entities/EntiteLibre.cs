using System;
using MessagePack;

namespace Intersect.Framework.Core.Entities;

[MessagePackObject(false)]
public sealed class EntiteLibre
{
	public const byte EnMouvement = 1;

	public const byte EnCourse = 2;

	[Key(0)]
	public Guid Id { get; set; }

	[Key(1)]
	public Guid MapId { get; set; }

	[Key(2)]
	public float X { get; set; }

	[Key(3)]
	public float Y { get; set; }

	[Key(4)]
	public byte Direction { get; set; }

	[Key(5)]
	public byte Etat { get; set; }
}
