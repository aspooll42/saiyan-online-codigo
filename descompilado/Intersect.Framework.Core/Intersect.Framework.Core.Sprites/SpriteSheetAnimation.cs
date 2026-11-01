using Newtonsoft.Json;

namespace Intersect.Framework.Core.Sprites;

public sealed class SpriteSheetAnimation
{
	public string Name { get; set; } = string.Empty;

	public string Animation { get; set; } = string.Empty;

	public int X { get; set; }

	public int Y { get; set; }

	public int Frames { get; set; } = 1;

	public int? FrameWidth { get; set; }

	public int? FrameHeight { get; set; }

	public int OffsetX { get; set; }

	public int OffsetY { get; set; }

	public string Playback { get; set; } = string.Empty;

	public int? FrameTime { get; set; }

	[JsonIgnore]
	public bool Holds
	{
		get
		{
			switch (Playback.Trim().ToLowerInvariant())
			{
			case "hold":
			case "tenir":
			case "once":
			case "une-fois":
				return true;
			default:
				return false;
			}
		}
	}

	[JsonIgnore]
	public bool Loops
	{
		get
		{
			string text = Playback.Trim().ToLowerInvariant();
			if (text == "loop" || text == "boucle")
			{
				return true;
			}
			return false;
		}
	}
}
