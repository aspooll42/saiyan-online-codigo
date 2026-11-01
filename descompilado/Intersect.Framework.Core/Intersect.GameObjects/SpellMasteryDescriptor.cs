using System;

namespace Intersect.GameObjects;

public sealed class SpellMasteryDescriptor
{
	public TechniqueFamily Family { get; set; }

	public int ExperiencePerRank { get; set; }

	public Guid EvolvesIntoId { get; set; }

	public int EvolutionLevel { get; set; }

	public int DamagePerRank { get; set; }

	public int KiCostPerRank { get; set; }

	public int CastTimePerRankMs { get; set; }

	public int CooldownPerRankMs { get; set; }

	public int DurationPerRankMs { get; set; }

	public int LeakPercentPerRank { get; set; }
}
