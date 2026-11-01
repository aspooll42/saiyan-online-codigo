using System;
using Intersect.Framework.Core.GameObjects.Animations;
using Intersect.GameObjects.Annotations;
using Intersect.Localization;

namespace Intersect.Framework.Core.GameObjects.Maps.Attributes;

public class MapCritterAttribute : MapAttribute
{
	public override MapAttributeType Type => MapAttributeType.Critter;

	[EditorLabel("Attributes", "CritterSprite")]
	[EditorDisplay(EmptyBehavior = EmptyBehavior.ShowNoneOnNullOrEmpty, StringBehavior = StringBehavior.Trim)]
	public string Sprite { get; set; }

	[EditorLabel("Attributes", "CritterAnimation")]
	[EditorReference(typeof(AnimationDescriptor), "Name")]
	public Guid AnimationId { get; set; }

	[EditorLabel("Attributes", "CritterMovement")]
	[EditorDictionary("Attributes", "CritterMovements")]
	public byte Movement { get; set; }

	[EditorLabel("Attributes", "CritterSpeed")]
	[EditorTime]
	public int Speed { get; set; }

	[EditorLabel("Attributes", "CritterFrequency")]
	[EditorTime]
	public int Frequency { get; set; }

	[EditorLabel("Attributes", "CritterLayer")]
	[EditorDictionary("Attributes", "CritterLayers")]
	public byte Layer { get; set; }

	[EditorLabel("Attributes", "CritterDirection")]
	[EditorDictionary("Direction", "CritterDirection")]
	public byte Direction { get; set; }

	[EditorLabel("Attributes", "CritterIgnoreNpcAvoids")]
	[EditorBoolean(Style = BooleanStyle.YesNo)]
	public bool IgnoreNpcAvoids { get; set; }

	[EditorLabel("Attributes", "CritterBlockPlayers")]
	[EditorBoolean(Style = BooleanStyle.YesNo)]
	public bool BlockPlayers { get; set; }

	public override MapAttribute Clone()
	{
		MapCritterAttribute mapCritterAttribute = (MapCritterAttribute)base.Clone();
		mapCritterAttribute.Sprite = Sprite;
		mapCritterAttribute.AnimationId = AnimationId;
		mapCritterAttribute.Movement = Movement;
		mapCritterAttribute.Speed = Speed;
		mapCritterAttribute.Frequency = Frequency;
		mapCritterAttribute.Layer = Layer;
		mapCritterAttribute.IgnoreNpcAvoids = IgnoreNpcAvoids;
		return mapCritterAttribute;
	}
}
