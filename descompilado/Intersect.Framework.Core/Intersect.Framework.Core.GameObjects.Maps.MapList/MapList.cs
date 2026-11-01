using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using Intersect.Collections;
using Intersect.Framework.Core.Serialization;
using Intersect.Models;
using Newtonsoft.Json;

namespace Intersect.Framework.Core.GameObjects.Maps.MapList;

public class MapList
{
	[DatabaseGenerated(DatabaseGeneratedOption.None)]
	public Guid Id { get; protected set; } = Guid.NewGuid();

	[NotMapped]
	public List<MapListItem> Items { get; set; } = new List<MapListItem>();

	public static MapList List { get; set; } = new MapList();

	public static List<MapListMap> OrderedMaps { get; } = new List<MapListMap>();

	[JsonIgnore]
	[Column("JsonData")]
	public string JsonData
	{
		get
		{
			return JsonConvert.SerializeObject(this, new JsonSerializerSettings
			{
				SerializationBinder = new IntersectTypeSerializationBinder(),
				TypeNameHandling = TypeNameHandling.Auto,
				DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate,
				ObjectCreationHandling = ObjectCreationHandling.Replace
			});
		}
		set
		{
			JsonConvert.PopulateObject(value, this, new JsonSerializerSettings
			{
				SerializationBinder = new IntersectTypeSerializationBinder(),
				TypeNameHandling = TypeNameHandling.Auto,
				DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate,
				ObjectCreationHandling = ObjectCreationHandling.Replace
			});
		}
	}

	public void PostLoad(DatabaseObjectLookup gameMaps, bool isServer = true, bool isTopLevel = false)
	{
		if (isTopLevel)
		{
			OrderedMaps.Clear();
		}
		MapListItem[] array = Items.ToArray();
		foreach (MapListItem mapListItem in array)
		{
			if (mapListItem.Type == 0)
			{
				MapListFolder mapListFolder = (MapListFolder)mapListItem;
				mapListFolder.PostLoad(gameMaps, isServer);
				continue;
			}
			MapListMap mapListMap = (MapListMap)mapListItem;
			bool flag = false;
			if (isServer && gameMaps.Get<MapDescriptor>(mapListMap.MapId) == null)
			{
				Items.Remove(mapListItem);
				flag = true;
			}
			if (!flag)
			{
				mapListMap.PostLoad(gameMaps, isServer);
				OrderedMaps.Add(mapListMap);
			}
		}
		if (isTopLevel)
		{
			OrderedMaps.Sort();
		}
	}

	public void AddMap(Guid mapId, long timeCreated, DatabaseObjectLookup gameMaps)
	{
		if (gameMaps.Keys.Contains(mapId))
		{
			MapListMap item = new MapListMap
			{
				Name = gameMaps[mapId].Name,
				MapId = mapId,
				TimeCreated = timeCreated
			};
			Items.Add(item);
		}
	}

	public void AddFolder(string folderName)
	{
		MapListFolder item = new MapListFolder
		{
			Name = folderName,
			FolderId = Guid.NewGuid()
		};
		Items.Add(item);
	}

	public MapListFolder FindFolder(Guid folderId)
	{
		foreach (MapListItem item in Items)
		{
			if (item is MapListFolder mapListFolder)
			{
				if (mapListFolder.FolderId == folderId)
				{
					return mapListFolder;
				}
				if (mapListFolder.Children.TryFindFolder(folderId, out var mapListFolder2))
				{
					return mapListFolder2;
				}
			}
		}
		return null;
	}

	public bool TryFindFolder(Guid folderId, out MapListFolder mapListFolder)
	{
		mapListFolder = FindFolder(folderId);
		return mapListFolder != null;
	}

	public MapListMap FindMap(Guid mapId)
	{
		foreach (MapListItem item in Items)
		{
			MapListMap mapListMap2;
			if (!(item is MapListFolder mapListFolder))
			{
				if (item is MapListMap mapListMap && mapListMap.MapId == mapId)
				{
					return mapListMap;
				}
			}
			else if (mapListFolder.Children.TryFindMap(mapId, out mapListMap2))
			{
				return mapListMap2;
			}
		}
		return null;
	}

	public bool TryFindMap(Guid mapId, out MapListMap mapListMap)
	{
		mapListMap = FindMap(mapId);
		return mapListMap != null;
	}

	public MapListFolder FindMapParent(Guid mapId, MapListFolder parent)
	{
		foreach (MapListItem item in Items)
		{
			if (!(item is MapListFolder mapListFolder))
			{
				if (!(item is MapListMap mapListMap) || !(mapListMap.MapId == mapId))
				{
					continue;
				}
				return parent;
			}
			MapListFolder mapListFolder2 = mapListFolder.Children.FindMapParent(mapId, mapListFolder);
			if (mapListFolder2 == null)
			{
				continue;
			}
			return mapListFolder2;
		}
		return null;
	}

	public MapListFolder FindFolderParent(Guid folderId, MapListFolder parent)
	{
		foreach (MapListItem item in Items)
		{
			if (item is MapListFolder mapListFolder)
			{
				if (mapListFolder.FolderId == folderId)
				{
					return parent;
				}
				MapListFolder mapListFolder2 = mapListFolder.Children.FindFolderParent(folderId, mapListFolder);
				if (mapListFolder2 != null)
				{
					return mapListFolder2;
				}
			}
		}
		return null;
	}

	public void HandleMove(int srcType, Guid srcId, int destType, Guid destId)
	{
		MapListFolder mapListFolder = null;
		MapListFolder mapListFolder2 = null;
		MapList mapList = null;
		MapList mapList2 = null;
		MapListItem mapListItem = null;
		MapListItem mapListItem2 = null;
		if (destType == 0)
		{
			mapListFolder2 = FindFolderParent(destId, null);
			mapList = ((mapListFolder2 != null) ? mapListFolder2.Children : List);
			mapListItem2 = FindFolder(destId);
		}
		else
		{
			mapListFolder2 = FindMapParent(destId, null);
			mapList = ((mapListFolder2 != null) ? mapListFolder2.Children : List);
			mapListItem2 = FindMap(destId);
		}
		if (srcType == 0)
		{
			mapListFolder = FindFolderParent(srcId, null);
			mapList2 = ((mapListFolder != null) ? mapListFolder.Children : List);
			mapListItem = FindFolder(srcId);
		}
		else
		{
			mapListFolder = FindMapParent(srcId, null);
			mapList2 = ((mapListFolder != null) ? mapListFolder.Children : List);
			mapListItem = FindMap(srcId);
		}
		if (mapList != null && mapListItem2 != null && mapList2 != null && mapListItem != null)
		{
			if (destType == 0)
			{
				((MapListFolder)mapListItem2).Children.Items.Add(mapListItem);
				mapList2.Items.Remove(mapListItem);
			}
			else
			{
				mapList2.Items.Remove(mapListItem);
				mapList.Items.Insert(mapList.Items.IndexOf(mapListItem2), mapListItem);
			}
		}
		else if (mapList != null && mapList2 != null && mapListItem != null && destType == -1)
		{
			mapList.Items.Add(mapListItem);
			mapList2.Items.Remove(mapListItem);
		}
	}

	public void DeleteFolder(Guid folderId)
	{
		MapListFolder mapListFolder = FindFolderParent(folderId, null);
		MapListFolder mapListFolder2 = FindFolder(folderId);
		if (mapListFolder == null)
		{
			List.Items.AddRange(mapListFolder2.Children.Items);
			List.Items.Remove(mapListFolder2);
		}
		else
		{
			mapListFolder.Children.Items.AddRange(mapListFolder2.Children.Items);
			mapListFolder.Children.Items.Remove(mapListFolder2);
		}
	}

	public void DeleteMap(Guid mapid)
	{
		MapListFolder mapListFolder = FindMapParent(mapid, null);
		MapListMap item = FindMap(mapid);
		if (mapListFolder == null)
		{
			List.Items.Remove(item);
		}
		else
		{
			mapListFolder.Children.Items.Remove(item);
		}
	}

	public void UpdateMap(Guid mapId)
	{
		MapListMap mapListMap = FindMap(mapId);
		MapDescriptor mapDescriptor = DatabaseObject<MapDescriptor>.Get(mapId);
		if (mapListMap != null && mapDescriptor != null)
		{
			mapListMap.Name = mapDescriptor.Name;
			mapListMap.TimeCreated = mapDescriptor.TimeCreated;
		}
	}

	public Guid FindFirstMap()
	{
		Guid result = Guid.Empty;
		if (OrderedMaps.Count > 0)
		{
			result = OrderedMaps.OrderBy((MapListMap m) => m.TimeCreated).FirstOrDefault()?.MapId ?? default(Guid);
		}
		return result;
	}
}
