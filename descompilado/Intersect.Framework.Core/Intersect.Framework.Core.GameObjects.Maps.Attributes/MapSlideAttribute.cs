using Intersect.Enums;
using Intersect.GameObjects.Annotations;

namespace Intersect.Framework.Core.GameObjects.Maps.Attributes;

public class MapSlideAttribute : MapAttribute
{
	public override MapAttributeType Type => MapAttributeType.Slide;

	[EditorLabel("Attributes", "Direction")]
	[EditorDictionary("Direction", "WarpDirections")]
	public Direction Direction { get; set; }

	public override MapAttribute Clone()
	{
		MapSlideAttribute mapSlideAttribute = (MapSlideAttribute)base.Clone();
		mapSlideAttribute.Direction = Direction;
		return mapSlideAttribute;
	}
}
