using System;
using Newtonsoft.Json;

namespace Intersect.Config;

public class PacketSecurityOptions
{
	public int MinimumPing { get; set; } = 10;

	public float ErrorMarginFactor { get; set; } = 0.25f;

	public int NaturalLowerMargin { get; set; }

	public int NaturalUpperMargin { get; set; } = 500;

	public int AllowedSpikePackets { get; set; } = 5;

	public int BaseDesyncForegiveness { get; set; } = 400;

	public float DesyncForgivenessFactor { get; set; } = 2f;

	public int DesyncForgivenessInterval { get; set; } = 5000;

	[JsonProperty]
	[Obsolete("Use EditorThresholds instead, the EditorFloodThreshholds property will be removed in 0.9-beta", true)]
	private FloodThresholdOptions EditorFloodThreshholds
	{
		set
		{
			EditorThresholds = value;
		}
	}

	[JsonProperty]
	[Obsolete("Use PlayerThresholds instead, the PlayerFloodThreshholds property will be removed in 0.9-beta", true)]
	private FloodThresholdOptions PlayerFloodThreshholds
	{
		set
		{
			PlayerThresholds = value;
		}
	}

	[JsonProperty]
	[Obsolete("Use ModAdminThresholds instead, the ModAdminFloodThreshholds property will be removed in 0.9-beta", true)]
	private FloodThresholdOptions ModAdminFloodThreshholds
	{
		set
		{
			ModAdminThresholds = value;
		}
	}

	[JsonProperty]
	[Obsolete("Use DefaultThresholds instead, the FloodThreshholds property will be removed in 0.9-beta", true)]
	private FloodThresholdOptions FloodThreshholds
	{
		set
		{
			DefaultThresholds = value;
		}
	}

	public FloodThresholdOptions EditorThresholds { get; set; } = FloodThresholdOptions.Editor();

	public FloodThresholdOptions PlayerThresholds { get; set; } = new FloodThresholdOptions();

	public FloodThresholdOptions ModAdminThresholds { get; set; } = FloodThresholdOptions.Editor();

	public FloodThresholdOptions DefaultThresholds { get; set; } = FloodThresholdOptions.NotLoggedIn();
}
