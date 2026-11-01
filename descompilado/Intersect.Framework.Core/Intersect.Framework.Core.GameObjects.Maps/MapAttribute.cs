using Intersect.Framework.Core.GameObjects.Maps.Attributes;
using Intersect.GameObjects.Annotations;
using Newtonsoft.Json;

namespace Intersect.Framework.Core.GameObjects.Maps;

public abstract class MapAttribute
{
	[EditorLabel("Attributes", "AttributeType")]
	[EditorDictionary("Attributes", "AttributeTypes", FieldType = EditorFieldType.Pivot)]
	public abstract MapAttributeType Type { get; }

	public static MapAttribute CreateAttribute(MapAttributeType type)
	{
		return type switch
		{
			MapAttributeType.Walkable => null, 
			MapAttributeType.Blocked => new MapBlockedAttribute(), 
			MapAttributeType.Item => new MapItemAttribute(), 
			MapAttributeType.ZDimension => new MapZDimensionAttribute(), 
			MapAttributeType.NpcAvoid => new MapNpcAvoidAttribute(), 
			MapAttributeType.Warp => new MapWarpAttribute(), 
			MapAttributeType.Sound => new MapSoundAttribute(), 
			MapAttributeType.Resource => new MapResourceAttribute(), 
			MapAttributeType.Animation => new MapAnimationAttribute(), 
			MapAttributeType.GrappleStone => new MapGrappleStoneAttribute(), 
			MapAttributeType.Slide => new MapSlideAttribute(), 
			MapAttributeType.Critter => new MapCritterAttribute(), 
			MapAttributeType.Terrain => new MapTerrainAttribute(), 
			_ => null, 
		};
	}

	public virtual MapAttribute Clone()
	{
		return CreateAttribute(Type);
	}

	public string Data()
	{
		return JsonConvert.SerializeObject(this);
	}
}
