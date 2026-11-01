using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Runtime.InteropServices;
using Intersect.Framework.Core.Serialization;
using Intersect.Models;
using Newtonsoft.Json;

namespace Intersect.Framework.Core.GameObjects.Events;

public class EventDescriptor : DatabaseObject<EventDescriptor>, IFolderable
{
	private string mCachedPagesData;

	private List<EventPage> mPages = new List<EventPage>();

	public Guid MapId { get; set; }

	public int SpawnX { get; set; } = -1;

	public int SpawnY { get; set; } = -1;

	public bool CommonEvent { get; set; }

	public bool CanRunInParallel { get; set; }

	public bool Global { get; set; }

	[JsonIgnore]
	[Column("Pages")]
	public string PagesJson
	{
		get
		{
			return mCachedPagesData;
		}
		protected set
		{
			Pages = JsonConvert.DeserializeObject<List<EventPage>>(value, new JsonSerializerSettings
			{
				SerializationBinder = new IntersectTypeSerializationBinder(),
				TypeNameHandling = TypeNameHandling.Auto,
				DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate,
				ObjectCreationHandling = ObjectCreationHandling.Replace
			});
		}
	}

	[NotMapped]
	public List<EventPage> Pages
	{
		get
		{
			return mPages;
		}
		set
		{
			mPages = value;
			mCachedPagesData = JsonConvert.SerializeObject(Pages, new JsonSerializerSettings
			{
				SerializationBinder = new IntersectTypeSerializationBinder(),
				TypeNameHandling = TypeNameHandling.Auto,
				DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate,
				ObjectCreationHandling = ObjectCreationHandling.Replace
			});
		}
	}

	public new static string[] Names => (from pair in DatabaseObject<EventDescriptor>.Lookup
		where ((EventDescriptor)pair.Value)?.CommonEvent ?? false
		orderby pair.Value?.Name
		select pair.Value?.Name ?? "ERR_DELETED").ToArray();

	public new static KeyValuePair<Guid, string>[] ItemPairs => (from pair in DatabaseObject<EventDescriptor>.Lookup
		where ((EventDescriptor)pair.Value)?.CommonEvent ?? false
		orderby pair.Value?.Name
		select new KeyValuePair<Guid, string>(pair.Key, pair.Value?.Name ?? "ERR_DELETED")).ToArray();

	[JsonIgnore]
	[NotMapped]
	public override string JsonData => JsonConvert.SerializeObject(this, Formatting.Indented, new JsonSerializerSettings
	{
		SerializationBinder = new IntersectTypeSerializationBinder(),
		TypeNameHandling = TypeNameHandling.Auto,
		DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate,
		ObjectCreationHandling = ObjectCreationHandling.Replace
	});

	public string Folder { get; set; } = string.Empty;

	public EventDescriptor()
	{
	}

	[JsonConstructor]
	public EventDescriptor(Guid id, Guid mapId, int x, int y, bool isCommon = false, bool isGlobal = false)
		: base(id)
	{
		base.Name = "New Event";
		MapId = mapId;
		if (isCommon)
		{
			base.Name = "New Common Event";
		}
		SpawnX = x;
		SpawnY = y;
		CommonEvent = isCommon;
		Global = isGlobal;
		int num = 1;
		List<EventPage> list = new List<EventPage>(num);
		CollectionsMarshal.SetCount(list, num);
		Span<EventPage> span = CollectionsMarshal.AsSpan(list);
		int num2 = 0;
		span[num2] = new EventPage();
		num2++;
		Pages = list;
	}

	public EventDescriptor(Guid id, bool isCommon = false)
		: base(id)
	{
		base.Name = "New Event";
		Pages = new List<EventPage>();
		CommonEvent = isCommon;
	}

	public EventDescriptor(Guid id, EventDescriptor copy)
		: base(id)
	{
		base.Name = "New Event";
		Pages = new List<EventPage>();
		Load(copy.JsonData);
		CommonEvent = copy.CommonEvent;
	}

	public EventDescriptor(Guid id, string json, bool isCommon = false)
		: base(id)
	{
		base.Name = "New Event";
		CommonEvent = isCommon;
		Pages = new List<EventPage>();
		Load(json);
	}

	public new static Guid IdFromList(int listIndex)
	{
		if (listIndex < 0)
		{
			return Guid.Empty;
		}
		KeyValuePair<Guid, IDatabaseObject>[] array = (from p in DatabaseObject<EventDescriptor>.Lookup
			where ((EventDescriptor)p.Value)?.CommonEvent ?? false
			orderby p.Value?.Name
			select p).ToArray();
		if (listIndex > array.Length)
		{
			return Guid.Empty;
		}
		return array[listIndex].Value?.Id ?? Guid.Empty;
	}

	public new static EventDescriptor FromList(int listIndex)
	{
		return DatabaseObject<EventDescriptor>.Get(IdFromList(listIndex));
	}

	public new static int ListIndex(Guid id)
	{
		KeyValuePair<Guid, IDatabaseObject>[] array = (from p in DatabaseObject<EventDescriptor>.Lookup
			where ((EventDescriptor)p.Value)?.CommonEvent ?? false
			orderby p.Value?.Name
			select p).ToArray();
		for (int num = 0; num < array.Length; num++)
		{
			if (array[num].Key == id)
			{
				return num;
			}
		}
		return -1;
	}

	public new int ListIndex()
	{
		return ListIndex(base.Id);
	}

	public override void Load(string json, bool keepCreationTime = false)
	{
		long timeCreated = base.TimeCreated;
		JsonConvert.PopulateObject(json, this, new JsonSerializerSettings
		{
			SerializationBinder = new IntersectTypeSerializationBinder(),
			TypeNameHandling = TypeNameHandling.Auto,
			DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate,
			ObjectCreationHandling = ObjectCreationHandling.Replace
		});
		if (keepCreationTime)
		{
			base.TimeCreated = timeCreated;
		}
	}
}
