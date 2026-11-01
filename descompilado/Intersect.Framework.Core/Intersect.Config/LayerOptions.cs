using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace Intersect.Config;

public class LayerOptions
{
	public const string Attributes = "Attributes";

	public const string Npcs = "Npcs";

	public const string Lights = "Lights";

	public const string Events = "Events";

	[JsonProperty]
	public List<string> LowerLayers { get; private set; }

	[JsonProperty]
	public List<string> MiddleLayers { get; private set; }

	[JsonProperty]
	public List<string> UpperLayers { get; private set; }

	[JsonIgnore]
	public List<string> All { get; private set; }

	[JsonProperty]
	public bool DestroyOrphanedLayers { get; private set; }

	[OnDeserializing]
	internal void OnDeserializingMethod(StreamingContext context)
	{
		All.Clear();
		LowerLayers.Clear();
		MiddleLayers.Clear();
		UpperLayers.Clear();
	}

	[OnDeserialized]
	internal void OnDeserializedMethod(StreamingContext context)
	{
		Validate();
	}

	public void Validate()
	{
		List<string> list = new List<string>();
		list.AddRange(LowerLayers.Distinct());
		LowerLayers = list;
		List<string> list2 = new List<string>();
		list2.AddRange(MiddleLayers.Distinct());
		MiddleLayers = list2;
		List<string> list3 = new List<string>();
		list3.AddRange(UpperLayers.Distinct());
		UpperLayers = list3;
		string[] array = new string[4] { "Attributes", "Npcs", "Lights", "Events" };
		All.Clear();
		All.AddRange(LowerLayers);
		All.AddRange(MiddleLayers);
		All.AddRange(UpperLayers);
		if (All.Count() == 0)
		{
			throw new Exception("Config Error: You must have at least 1 map layer configured! Please update your server config.");
		}
		string[] array2 = array;
		foreach (string text in array2)
		{
			if (All.Contains(text))
			{
				throw new Exception("Config Error: Layer '" + text + "' is reserved for editor use. Please choose different naming for map layers in your server config.");
			}
		}
		if (All.Count != All.Distinct().Count())
		{
			throw new Exception("Config Error: Duplicate map layers detected! Map layers must be unique in name. Please update your server config.");
		}
	}

	public LayerOptions()
	{
		int num = 3;
		List<string> list = new List<string>(num);
		CollectionsMarshal.SetCount(list, num);
		Span<string> span = CollectionsMarshal.AsSpan(list);
		int num2 = 0;
		span[num2] = "Ground";
		num2++;
		span[num2] = "Mask 1";
		num2++;
		span[num2] = "Mask 2";
		num2++;
		LowerLayers = list;
		num2 = 1;
		List<string> list2 = new List<string>(num2);
		CollectionsMarshal.SetCount(list2, num2);
		span = CollectionsMarshal.AsSpan(list2);
		num = 0;
		span[num] = "Fringe 1";
		num++;
		MiddleLayers = list2;
		num = 1;
		List<string> list3 = new List<string>(num);
		CollectionsMarshal.SetCount(list3, num);
		span = CollectionsMarshal.AsSpan(list3);
		num2 = 0;
		span[num2] = "Fringe 2";
		num2++;
		UpperLayers = list3;
		All = new List<string>();
	}
}
