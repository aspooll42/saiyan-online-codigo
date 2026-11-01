using Intersect.GameObjects.Annotations;

namespace Intersect.Framework.Core.GameObjects.Maps.Attributes;

public class MapSoundAttribute : MapAttribute
{
	public override MapAttributeType Type => MapAttributeType.Sound;

	[EditorLabel("Attributes", "Sound")]
	[EditorDisplay(EmptyBehavior = EmptyBehavior.ShowNoneOnNullOrEmpty, StringBehavior = StringBehavior.Trim)]
	public string File { get; set; }

	[EditorLabel("Attributes", "SoundDistance")]
	[EditorFormatted("Attributes", "DistanceFormat")]
	public byte Distance { get; set; }

	[EditorLabel("Attributes", "SoundInterval")]
	[EditorTime]
	public int LoopInterval { get; set; }

	public override MapAttribute Clone()
	{
		MapSoundAttribute mapSoundAttribute = (MapSoundAttribute)base.Clone();
		mapSoundAttribute.File = File;
		mapSoundAttribute.Distance = Distance;
		mapSoundAttribute.LoopInterval = LoopInterval;
		return mapSoundAttribute;
	}
}
