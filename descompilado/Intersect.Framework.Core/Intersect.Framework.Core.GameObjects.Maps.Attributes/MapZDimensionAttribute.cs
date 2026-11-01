using Intersect.GameObjects.Annotations;

namespace Intersect.Framework.Core.GameObjects.Maps.Attributes;

public class MapZDimensionAttribute : MapAttribute
{
	public override MapAttributeType Type => MapAttributeType.ZDimension;

	[EditorLabel("Attributes", "ZGateway")]
	[EditorFormatted("Attributes", "FormatZLevel")]
	public byte GatewayTo { get; set; }

	[EditorLabel("Attributes", "ZBlock")]
	[EditorFormatted("Attributes", "FormatZLevel")]
	public byte BlockedLevel { get; set; }

	public override MapAttribute Clone()
	{
		MapZDimensionAttribute mapZDimensionAttribute = (MapZDimensionAttribute)base.Clone();
		mapZDimensionAttribute.GatewayTo = GatewayTo;
		mapZDimensionAttribute.BlockedLevel = BlockedLevel;
		return mapZDimensionAttribute;
	}
}
