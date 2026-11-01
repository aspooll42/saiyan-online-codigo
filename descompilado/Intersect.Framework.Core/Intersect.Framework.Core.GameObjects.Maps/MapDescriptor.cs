using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using Intersect.Collections;
using Intersect.Compression;
using Intersect.Framework.Core.GameObjects.Animations;
using Intersect.Framework.Core.GameObjects.Events;
using Intersect.Framework.Core.GameObjects.Lighting;
using Intersect.Framework.Core.GameObjects.Maps.MapList;
using Intersect.Framework.Core.Serialization;
using Intersect.Models;
using Newtonsoft.Json;

namespace Intersect.Framework.Core.GameObjects.Maps;

public class MapDescriptor : DatabaseObject<MapDescriptor>
{
	public class MapControllers : DatabaseObjectLookup
	{
		private readonly DatabaseObjectLookup mBaseLookup;

		public MapControllers(DatabaseObjectLookup baseLookup)
			: base(baseLookup.StoredType)
		{
			if (baseLookup == null)
			{
				throw new ArgumentNullException();
			}
			mBaseLookup = baseLookup;
		}

		internal override bool InternalSet(IDatabaseObject value, bool overwrite)
		{
			mBaseLookup?.InternalSet(value, overwrite);
			return base.InternalSet(value, overwrite);
		}

		public override bool Delete(IDatabaseObject value)
		{
			mBaseLookup?.Delete(value);
			return base.Delete(value);
		}

		public override void Clear()
		{
			mBaseLookup?.Clear();
			base.Clear();
		}
	}

	[NotMapped]
	[JsonIgnore]
	public readonly Dictionary<Guid, EventDescriptor> LocalEvents = new Dictionary<Guid, EventDescriptor>();

	[JsonIgnore]
	[NotMapped]
	public MapAutotiles Autotiles;

	private string _region = string.Empty;

	[JsonIgnore]
	[NotMapped]
	public Dictionary<string, Tile[,]> Layers = new Dictionary<string, Tile[,]>();

	private MapAttribute[,] mAttributes = new MapAttribute[Options.Instance?.Map.MapWidth ?? 32, Options.Instance?.Map.MapHeight ?? 26];

	private byte[] mCachedAttributeData;

	[JsonIgnore]
	[NotMapped]
	protected object mMapLock = new object();

	private List<LigneDeBlocage> _lignes = new List<LigneDeBlocage>();

	private IndexDesLignes _indexDesLignes;

	[NotMapped]
	[JsonIgnore]
	protected JsonSerializerSettings mJsonSerializerSettings { get; } = new JsonSerializerSettings
	{
		SerializationBinder = new IntersectTypeSerializationBinder(),
		TypeNameHandling = TypeNameHandling.Auto,
		DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate,
		ObjectCreationHandling = ObjectCreationHandling.Replace
	};

	[NotMapped]
	public List<Guid> EventIds { get; set; } = new List<Guid>();

	[NotMapped]
	public string Region
	{
		get
		{
			try
			{
				foreach (MapListItem item in Intersect.Framework.Core.GameObjects.Maps.MapList.MapList.List.Items)
				{
					if (item is MapListFolder mapListFolder && mapListFolder.Children.TryFindMap(base.Id, out var _))
					{
						return mapListFolder.Name ?? string.Empty;
					}
				}
			}
			catch (InvalidOperationException)
			{
			}
			return _region;
		}
		set
		{
			_region = value ?? string.Empty;
		}
	}

	[JsonIgnore]
	public byte[] TileData { get; set; }

	public Guid Up { get; set; }

	public Guid Down { get; set; }

	public Guid Left { get; set; }

	public Guid Right { get; set; }

	public int Revision { get; set; }

	[Column("Attributes")]
	[JsonIgnore]
	public byte[] AttributeData
	{
		get
		{
			return GetAttributeData();
		}
		set
		{
			string text = LZ4.UnPickleString(value);
			mAttributes = JsonConvert.DeserializeObject<MapAttribute[,]>(LZ4.UnPickleString(value), mJsonSerializerSettings);
			mCachedAttributeData = value;
		}
	}

	[NotMapped]
	[JsonIgnore]
	public MapAttribute[,] Attributes
	{
		get
		{
			return mAttributes ?? (mAttributes = new MapAttribute[Options.Instance.Map.MapWidth, Options.Instance.Map.MapHeight]);
		}
		set
		{
			mAttributes = value;
			mCachedAttributeData = LZ4.PickleString(JsonConvert.SerializeObject(mAttributes, Formatting.None, mJsonSerializerSettings));
		}
	}

	[Column("Lights")]
	[JsonIgnore]
	public string LightsJson
	{
		get
		{
			return JsonConvert.SerializeObject(Lights);
		}
		set
		{
			Lights.Clear();
			List<LightDescriptor> list = JsonConvert.DeserializeObject<List<LightDescriptor>>(value);
			if (list != null)
			{
				Lights.AddRange(list);
			}
		}
	}

	[NotMapped]
	[JsonProperty]
	public List<LightDescriptor> Lights { get; private set; } = new List<LightDescriptor>();

	[Column("Lignes")]
	[JsonIgnore]
	public string LignesJson
	{
		get
		{
			if (_lignes.Count != 0)
			{
				return JsonConvert.SerializeObject(_lignes);
			}
			return null;
		}
		set
		{
			Lignes = (string.IsNullOrWhiteSpace(value) ? new List<LigneDeBlocage>() : JsonConvert.DeserializeObject<List<LigneDeBlocage>>(value));
		}
	}

	[NotMapped]
	[JsonProperty]
	public List<LigneDeBlocage> Lignes
	{
		get
		{
			return _lignes;
		}
		set
		{
			_lignes = value ?? new List<LigneDeBlocage>();
			_indexDesLignes = null;
		}
	}

	[NotMapped]
	[JsonIgnore]
	public IndexDesLignes IndexDesLignes
	{
		get
		{
			List<LigneDeBlocage> lignes = _lignes;
			if (lignes.Count == 0)
			{
				return IndexDesLignes.Vide;
			}
			IndexDesLignes indexDesLignes = _indexDesLignes;
			if (indexDesLignes != null && indexDesLignes.Source == lignes)
			{
				return indexDesLignes;
			}
			return _indexDesLignes = IndexDesLignes.Construire(lignes, Options.Instance.Map.MapWidth, Options.Instance.Map.MapHeight, Options.Instance.Map.TileWidth, Options.Instance.Map.TileHeight);
		}
	}

	[Column("Events")]
	[JsonIgnore]
	public string EventIdsJson
	{
		get
		{
			return JsonConvert.SerializeObject(EventIds);
		}
		set
		{
			EventIds = JsonConvert.DeserializeObject<List<Guid>>(value);
		}
	}

	[NotMapped]
	public string LocalEventsJson
	{
		get
		{
			return JsonConvert.SerializeObject(LocalEvents, new JsonSerializerSettings
			{
				SerializationBinder = new IntersectTypeSerializationBinder(),
				TypeNameHandling = TypeNameHandling.Auto,
				DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate,
				ObjectCreationHandling = ObjectCreationHandling.Replace
			});
		}
		set
		{
			JsonConvert.PopulateObject(value, LocalEvents, new JsonSerializerSettings
			{
				SerializationBinder = new IntersectTypeSerializationBinder(),
				TypeNameHandling = TypeNameHandling.Auto,
				DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate,
				ObjectCreationHandling = ObjectCreationHandling.Replace
			});
		}
	}

	[Column("NpcSpawns")]
	[JsonIgnore]
	public string NpcSpawnsJson
	{
		get
		{
			return JsonConvert.SerializeObject(Spawns);
		}
		set
		{
			Spawns.Clear();
			List<NpcSpawn> list = JsonConvert.DeserializeObject<List<NpcSpawn>>(value);
			if (list != null)
			{
				Spawns.AddRange(list);
			}
		}
	}

	[NotMapped]
	[JsonProperty]
	public List<NpcSpawn> Spawns { get; private set; } = new List<NpcSpawn>();

	public string Music { get; set; }

	public string Sound { get; set; }

	public bool IsIndoors { get; set; }

	public string Panorama { get; set; }

	public string Fog { get; set; }

	public int FogXSpeed { get; set; }

	public int FogYSpeed { get; set; }

	public int FogTransparency { get; set; }

	public int RHue { get; set; }

	public int GHue { get; set; }

	public int BHue { get; set; }

	public int AHue { get; set; }

	public int Brightness { get; set; } = 100;

	public MapZone ZoneType { get; set; }

	public int PlayerLightSize { get; set; } = 300;

	public byte PlayerLightIntensity { get; set; } = byte.MaxValue;

	public float PlayerLightExpand { get; set; }

	[Column("PlayerLightColor")]
	[JsonIgnore]
	public string PlayerLightColorJson
	{
		get
		{
			return JsonConvert.SerializeObject(PlayerLightColor);
		}
		set
		{
			PlayerLightColor = JsonConvert.DeserializeObject<Color>(value);
		}
	}

	[NotMapped]
	public Color PlayerLightColor { get; set; } = Color.White;

	public string OverlayGraphic { get; set; }

	[Column("WeatherAnimation")]
	[JsonProperty]
	public Guid WeatherAnimationId { get; protected set; }

	[NotMapped]
	[JsonIgnore]
	public AnimationDescriptor WeatherAnimation
	{
		get
		{
			return DatabaseObject<AnimationDescriptor>.Get(WeatherAnimationId);
		}
		set
		{
			WeatherAnimationId = value?.Id ?? Guid.Empty;
		}
	}

	public int WeatherXSpeed { get; set; }

	public int WeatherYSpeed { get; set; }

	public int WeatherIntensity { get; set; }

	public bool HideEquipment { get; set; }

	public bool NoFlight { get; set; }

	[NotMapped]
	[JsonIgnore]
	public bool VolInterdit
	{
		get
		{
			if (!NoFlight)
			{
				return IsIndoors;
			}
			return true;
		}
	}

	[NotMapped]
	[JsonIgnore]
	public object MapLock => mMapLock;

	[JsonConstructor]
	public MapDescriptor(Guid id)
		: base(id)
	{
		base.Name = "New Map";
		if (Layers == null)
		{
			Layers = new Dictionary<string, Tile[,]>();
			TileData = LZ4.PickleString(JsonConvert.SerializeObject(Layers, Formatting.None, mJsonSerializerSettings));
			Layers = null;
		}
		else
		{
			TileData = LZ4.PickleString(JsonConvert.SerializeObject(Layers, Formatting.None, mJsonSerializerSettings));
		}
		mCachedAttributeData = LZ4.PickleString(JsonConvert.SerializeObject(Attributes, Formatting.None, mJsonSerializerSettings));
	}

	public MapDescriptor()
	{
		base.Name = "New Map";
	}

	public MapDescriptor(MapDescriptor mapDescriptor)
		: base(mapDescriptor?.Id ?? Guid.Empty)
	{
		if (mapDescriptor == null)
		{
			return;
		}
		lock (MapLock ?? throw new ArgumentNullException("MapLock", "this"))
		{
			lock (mapDescriptor.MapLock ?? throw new ArgumentNullException("MapLock", "mapDescriptor"))
			{
				base.Name = mapDescriptor.Name;
				Brightness = mapDescriptor.Brightness;
				IsIndoors = mapDescriptor.IsIndoors;
				if (Layers != null && mapDescriptor.Layers != null)
				{
					Layers.Clear();
					foreach (KeyValuePair<string, Tile[,]> layer in mapDescriptor.Layers)
					{
						Tile[,] array = new Tile[Options.Instance.Map.MapWidth, Options.Instance.Map.MapHeight];
						for (int i = 0; i < Options.Instance.Map.MapWidth; i++)
						{
							for (int j = 0; j < Options.Instance.Map.MapHeight; j++)
							{
								array[i, j] = new Tile
								{
									TilesetId = layer.Value[i, j].TilesetId,
									X = layer.Value[i, j].X,
									Y = layer.Value[i, j].Y,
									Autotile = layer.Value[i, j].Autotile
								};
							}
						}
						Layers.Add(layer.Key, array);
					}
				}
				for (int k = 0; k < Options.Instance.Map.MapWidth; k++)
				{
					for (int l = 0; l < Options.Instance.Map.MapHeight; l++)
					{
						if (Attributes != null)
						{
							MapAttribute[,] attributes = mapDescriptor.Attributes;
							if (((attributes != null) ? attributes[k, l] : null) == null)
							{
								Attributes[k, l] = null;
							}
							else
							{
								Attributes[k, l] = mapDescriptor.Attributes[k, l].Clone();
							}
						}
					}
				}
				for (int m = 0; m < mapDescriptor.Spawns?.Count; m++)
				{
					Spawns.Add(new NpcSpawn(mapDescriptor.Spawns[m]));
				}
				for (int n = 0; n < mapDescriptor.Lights?.Count; n++)
				{
					Lights.Add(new LightDescriptor(mapDescriptor.Lights[n]));
				}
				Lignes = mapDescriptor.Lignes.Select((LigneDeBlocage ligne) => new LigneDeBlocage(ligne)).ToList();
				foreach (KeyValuePair<Guid, EventDescriptor> localEvent in mapDescriptor.LocalEvents)
				{
					EventDescriptor value = new EventDescriptor(localEvent.Key, localEvent.Value?.JsonData)
					{
						MapId = base.Id
					};
					LocalEvents?.Add(localEvent.Key, value);
				}
				EventIds?.Clear();
				EventIds?.AddRange(mapDescriptor.EventIds?.ToArray() ?? Array.Empty<Guid>());
			}
		}
	}

	public void LignesChangees()
	{
		_indexDesLignes = null;
	}

	public virtual MapDescriptor[,] GenerateAutotileGrid()
	{
		return null;
	}

	public virtual byte[] GetAttributeData()
	{
		return mCachedAttributeData;
	}
}
