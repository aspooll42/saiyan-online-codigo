using System;

namespace Intersect.Framework.Core.GameObjects.NPCs;

public sealed class BossAbility
{
	public BossAbilityKind Kind { get; set; }

	public int Damage { get; set; } = 50;

	public int CooldownMs { get; set; } = 8000;

	public int TelegraphMs { get; set; } = 1600;

	public int Size { get; set; } = 6;

	public int Count { get; set; } = 3;

	public bool EnragedOnly { get; set; }

	public int Repeat { get; set; } = 1;

	public int StunMs { get; set; }

	public Guid StunSpellId { get; set; }
}
