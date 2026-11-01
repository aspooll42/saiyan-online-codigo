using Intersect.Framework.Core.GameObjects.Maps;

namespace Intersect.Framework.Core.GameObjects.Conditions.ConditionMetadata;

public class MapZoneTypeIs : Condition
{
	public override ConditionType Type { get; } = ConditionType.MapZoneTypeIs;

	public MapZone ZoneType { get; set; }
}
