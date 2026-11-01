using System;
using Intersect.GameObjects.Annotations;
using Intersect.Localization;

namespace Intersect.Framework.Core.GameObjects.Maps.Attributes;

public class MapWarpAttribute : MapAttribute
{
	public override MapAttributeType Type => MapAttributeType.Warp;

	[EditorLabel("Attributes", "Map")]
	[EditorReference(typeof(MapDescriptor), "Name")]
	public Guid MapId { get; set; }

	[EditorLabel("Attributes", "WarpX")]
	[EditorDisplay]
	public byte X { get; set; }

	[EditorLabel("Attributes", "WarpY")]
	[EditorDisplay]
	public byte Y { get; set; }

	[EditorLabel("Attributes", "WarpDirection")]
	[EditorDictionary("Direction", "WarpDirections")]
	public WarpDirection Direction { get; set; }

	[EditorLabel("Warping", "ChangeInstance")]
	[EditorBoolean(Style = BooleanStyle.YesNo)]
	public bool ChangeInstance { get; set; }

	[EditorLabel("Warping", "InstanceType")]
	[EditorDictionary("MapInstance", "InstanceTypes")]
	public MapInstanceType InstanceType { get; set; }

	[EditorLabel("Attributes", "WarpSound")]
	[EditorDisplay(EmptyBehavior = EmptyBehavior.ShowNoneOnNullOrEmpty, StringBehavior = StringBehavior.Trim)]
	public string WarpSound { get; set; }

	public override MapAttribute Clone()
	{
		MapWarpAttribute mapWarpAttribute = (MapWarpAttribute)base.Clone();
		mapWarpAttribute.MapId = MapId;
		mapWarpAttribute.X = X;
		mapWarpAttribute.Y = Y;
		mapWarpAttribute.Direction = Direction;
		mapWarpAttribute.ChangeInstance = ChangeInstance;
		mapWarpAttribute.InstanceType = InstanceType;
		mapWarpAttribute.WarpSound = WarpSound;
		return mapWarpAttribute;
	}
}
