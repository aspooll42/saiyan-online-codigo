using System;
using MessagePack;
using Newtonsoft.Json;

namespace Intersect.Framework.Core.GameObjects.WorldMap;

[MessagePackObject(false)]
public class WorldMapDestination
{
	[Key(0)]
	[JsonProperty("id")]
	public string Id { get; set; } = string.Empty;

	[Key(1)]
	[JsonProperty("nom")]
	public string Name { get; set; } = string.Empty;

	[Key(2)]
	[JsonProperty("marqueurX")]
	public int MarkerX { get; set; }

	[Key(3)]
	[JsonProperty("marqueurY")]
	public int MarkerY { get; set; }

	[Key(4)]
	[JsonIgnore]
	public bool Unlocked { get; set; }

	[IgnoreMember]
	[JsonProperty("carte")]
	public string Map { get; set; } = string.Empty;

	[IgnoreMember]
	[JsonProperty("x")]
	public byte X { get; set; }

	[IgnoreMember]
	[JsonProperty("y")]
	public byte Y { get; set; }

	[IgnoreMember]
	[JsonProperty("variableDeblocage")]
	public Guid UnlockVariableId { get; set; }
}
