using System;
using Intersect.Framework.Core.GameObjects.Resources;
using Intersect.GameObjects.Annotations;

namespace Intersect.Framework.Core.GameObjects.Maps.Attributes;

public class MapResourceAttribute : MapAttribute
{
	public override MapAttributeType Type => MapAttributeType.Resource;

	[EditorLabel("Attributes", "Resource")]
	[EditorReference(typeof(ResourceDescriptor), "Name")]
	public Guid ResourceId { get; set; }

	[EditorLabel("Attributes", "ZDimension")]
	[EditorFormatted("Attributes", "FormatSpawnLevel")]
	public byte SpawnLevel { get; set; }

	public override MapAttribute Clone()
	{
		MapResourceAttribute mapResourceAttribute = (MapResourceAttribute)base.Clone();
		mapResourceAttribute.ResourceId = ResourceId;
		mapResourceAttribute.SpawnLevel = SpawnLevel;
		return mapResourceAttribute;
	}
}
