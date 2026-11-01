using System;
using Intersect.Framework.Core.GameObjects.Animations;
using Intersect.GameObjects.Annotations;
using Intersect.Localization;

namespace Intersect.Framework.Core.GameObjects.Maps.Attributes;

public class MapAnimationAttribute : MapAttribute
{
	public override MapAttributeType Type => MapAttributeType.Animation;

	[EditorLabel("Attributes", "MapAnimation")]
	[EditorReference(typeof(AnimationDescriptor), "Name")]
	public Guid AnimationId { get; set; }

	[EditorLabel("Attributes", "MapAnimationBlock")]
	[EditorBoolean(Style = BooleanStyle.YesNo)]
	public bool IsBlock { get; set; }

	public override MapAttribute Clone()
	{
		MapAnimationAttribute mapAnimationAttribute = (MapAnimationAttribute)base.Clone();
		mapAnimationAttribute.AnimationId = AnimationId;
		mapAnimationAttribute.IsBlock = IsBlock;
		return mapAnimationAttribute;
	}
}
