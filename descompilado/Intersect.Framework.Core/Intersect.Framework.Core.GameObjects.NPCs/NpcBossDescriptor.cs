using System.Collections.Generic;
using Newtonsoft.Json;

namespace Intersect.Framework.Core.GameObjects.NPCs;

public sealed class NpcBossDescriptor
{
	public bool Enabled { get; set; }

	public int EnrageHealthPercent { get; set; } = 50;

	public BossVisual Visual { get; set; }

	public int GapMs { get; set; }

	[JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace)]
	public List<BossAbility> Abilities { get; set; } = new List<BossAbility>();
}
