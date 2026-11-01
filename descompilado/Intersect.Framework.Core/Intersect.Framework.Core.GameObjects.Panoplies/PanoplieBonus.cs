using System;
using Intersect.Enums;
using Intersect.Framework.Core.GameObjects.Items;
using Newtonsoft.Json;

namespace Intersect.Framework.Core.GameObjects.Panoplies;

public class PanoplieBonus
{
	public int Pieces { get; set; } = 2;

	[JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace)]
	public int[] Stats { get; set; } = new int[Enum.GetValues<Stat>().Length];

	[JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace)]
	public long[] Vitals { get; set; } = new long[Enum.GetValues<Vital>().Length];

	public ItemEffect Effect { get; set; }

	public int EffectPercentage { get; set; }
}
