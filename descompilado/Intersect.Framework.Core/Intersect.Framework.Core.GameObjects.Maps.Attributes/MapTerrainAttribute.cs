using System;
using Intersect.Framework.Core.GameObjects.Housing;
using Intersect.GameObjects.Annotations;

namespace Intersect.Framework.Core.GameObjects.Maps.Attributes;

public class MapTerrainAttribute : MapAttribute
{
	public override MapAttributeType Type => MapAttributeType.Terrain;

	public Guid TerrainId { get; set; }

	[EditorLabel("Attributes", "TerrainName")]
	[EditorDisplay(EmptyBehavior = EmptyBehavior.ShowNoneOnNullOrEmpty, StringBehavior = StringBehavior.Trim)]
	public string Name { get; set; } = string.Empty;

	[EditorLabel("Attributes", "TerrainKind")]
	[EditorDictionary("Attributes", "TerrainKinds")]
	public TerrainKind Kind { get; set; }

	[EditorLabel("Attributes", "TerrainPrice")]
	[EditorDisplay]
	public long Price { get; set; }

	[EditorLabel("Attributes", "TerrainCapsule")]
	[EditorReference(typeof(CapsuleDescriptor), "Name")]
	public Guid CapsuleId { get; set; }

	public override MapAttribute Clone()
	{
		MapTerrainAttribute mapTerrainAttribute = (MapTerrainAttribute)base.Clone();
		mapTerrainAttribute.TerrainId = TerrainId;
		mapTerrainAttribute.Name = Name;
		mapTerrainAttribute.Kind = Kind;
		mapTerrainAttribute.Price = Price;
		mapTerrainAttribute.CapsuleId = CapsuleId;
		return mapTerrainAttribute;
	}
}
