using System;

namespace Intersect.Framework.Core.GameObjects.Conditions.ConditionMetadata;

public class KnowsSpellCondition : Condition
{
	public override ConditionType Type { get; } = ConditionType.KnowsSpell;

	public Guid SpellId { get; set; }
}
