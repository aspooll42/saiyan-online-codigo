using System;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;

namespace Intersect.GameObjects;

public class DaylightCycleDescriptor
{
	public static DaylightCycleDescriptor Instance { get; set; } = new DaylightCycleDescriptor();

	[NotMapped]
	public Color[]? DaylightHues { get; set; }

	[DatabaseGenerated(DatabaseGeneratedOption.None)]
	public Guid Id { get; protected set; } = Guid.NewGuid();

	[JsonIgnore]
	[Column("DaylightHues")]
	public string DaylightHuesJson
	{
		get
		{
			return JsonConvert.SerializeObject(DaylightHues, Formatting.None);
		}
		protected set
		{
			DaylightHues = JsonConvert.DeserializeObject<Color[]>(value);
		}
	}

	public int RangeInterval { get; set; } = 720;

	public float Rate { get; set; } = 1f;

	public bool SyncTime { get; set; } = true;

	public DaylightCycleDescriptor()
	{
		ResetColors();
	}

	public void LoadFromJson(string json)
	{
		JsonConvert.PopulateObject(json, this);
	}

	public string GetInstanceJson()
	{
		return JsonConvert.SerializeObject(this);
	}

	public static string GetTimeJson()
	{
		return JsonConvert.SerializeObject(Instance);
	}

	public void ResetColors()
	{
		DaylightHues = new Color[1440 / RangeInterval];
		for (int i = 0; i < 1440 / RangeInterval; i++)
		{
			DaylightHues[i] = new Color(255, 255, 255, 255);
		}
	}

	public static int GetIntervalIndex(int minutes)
	{
		return minutes switch
		{
			1440 => 0, 
			720 => 1, 
			480 => 2, 
			360 => 3, 
			240 => 4, 
			180 => 5, 
			120 => 6, 
			60 => 7, 
			45 => 8, 
			30 => 9, 
			15 => 10, 
			10 => 11, 
			_ => 5, 
		};
	}

	public static int GetTimeInterval(int index)
	{
		return index switch
		{
			0 => 1440, 
			1 => 720, 
			2 => 480, 
			3 => 360, 
			4 => 240, 
			5 => 180, 
			6 => 120, 
			7 => 60, 
			8 => 45, 
			9 => 30, 
			10 => 15, 
			11 => 10, 
			_ => 5, 
		};
	}
}
