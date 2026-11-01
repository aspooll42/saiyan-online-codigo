using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Runtime.CompilerServices;
using Intersect.Collections;
using Intersect.Enums;
using Intersect.Framework.Core.Serialization;
using Newtonsoft.Json;

namespace Intersect.Models;

public abstract class DatabaseObject<TObject> : IDatabaseObject, INamedObject, IObject where TObject : DatabaseObject<TObject>
{
	public const string Deleted = "ERR_DELETED";

	private string mBackup;

	public static readonly DatabaseObjectLookup Lookup = DatabaseObjectLookup.GetLookup(typeof(TObject));

	public static readonly GameObjectType ObjectType;

	public static KeyValuePair<Guid, string>[] ItemPairs => (from pair in Lookup
		orderby pair.Value?.Name
		select new KeyValuePair<Guid, string>(pair.Key, pair.Value?.Name ?? "ERR_DELETED")).ToArray();

	public static string[] Names => (from pair in Lookup
		orderby pair.Value?.Name
		select pair.Value?.Name ?? "ERR_DELETED").ToArray();

	[DatabaseGenerated(DatabaseGeneratedOption.None)]
	public Guid Id { get; protected set; } = Guid.NewGuid();

	public long TimeCreated { get; set; }

	[NotMapped]
	public GameObjectType Type
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get
		{
			return ObjectType;
		}
	}

	[JsonIgnore]
	[NotMapped]
	public string DatabaseTable => Type.GetTable();

	[JsonProperty(Order = -4)]
	[Column(Order = 0)]
	public string Name { get; set; }

	[NotMapped]
	[JsonIgnore]
	public bool IsDeleted
	{
		get
		{
			if (Lookup.TryGetValue(Id, out var value))
			{
				return value != this;
			}
			return true;
		}
	}

	[JsonIgnore]
	[NotMapped]
	public virtual string JsonData => JsonConvert.SerializeObject(this, Formatting.Indented);

	protected DatabaseObject()
	{
	}

	[JsonConstructor]
	protected DatabaseObject(Guid descriptorId)
	{
		Id = descriptorId;
		TimeCreated = DateTime.Now.ToBinary();
	}

	public virtual void Load(string json, bool keepCreationTime = false)
	{
		long timeCreated = TimeCreated;
		JsonConvert.PopulateObject(json, this, new JsonSerializerSettings
		{
			SerializationBinder = new IntersectTypeSerializationBinder(),
			ObjectCreationHandling = ObjectCreationHandling.Replace
		});
		if (keepCreationTime)
		{
			TimeCreated = timeCreated;
		}
	}

	public void MakeBackup()
	{
		mBackup = JsonData;
	}

	public void DeleteBackup()
	{
		mBackup = null;
	}

	public void RestoreBackup()
	{
		if (mBackup != null)
		{
			Load(mBackup);
		}
	}

	public virtual void Delete()
	{
		Lookup.Delete(this as TObject);
	}

	public static Guid IdFromList(int listIndex)
	{
		if (listIndex < 0 || listIndex >= Lookup.KeyList.Count)
		{
			return Guid.Empty;
		}
		return Lookup.KeyList.OrderBy((Guid pairs) => Lookup[pairs]?.Name).ToArray()[listIndex];
	}

	public static TObject FromList(int listIndex)
	{
		if (listIndex < 0 || listIndex >= Lookup.ValueList.Count)
		{
			return null;
		}
		return (TObject)Lookup.ValueList.OrderBy((IDatabaseObject databaseObject) => databaseObject?.Name).ToArray()[listIndex];
	}

	public int ListIndex()
	{
		return ListIndex(Id);
	}

	public static int ListIndex(Guid id)
	{
		return Lookup.KeyList.OrderBy((Guid pairs) => Lookup[pairs]?.Name).ToList().IndexOf(id);
	}

	public static TObject Get(Guid id)
	{
		return Lookup.Get<TObject>(id);
	}

	public static string GetName(Guid id)
	{
		return Lookup.Get(id)?.Name ?? "ERR_DELETED";
	}

	public static string[] GetNameList()
	{
		return Lookup.Select((KeyValuePair<Guid, IDatabaseObject> pair) => pair.Value?.Name ?? "ERR_DELETED").ToArray();
	}

	public static bool TryGet(Guid id, out TObject databaseObject)
	{
		return Lookup.TryGetValue(id, out databaseObject);
	}

	static DatabaseObject()
	{
		if (!typeof(TObject).TryGetGameObjectType(out var gameObjectType))
		{
			throw new InvalidOperationException(typeof(TObject).Name + " not set up correctly");
		}
		ObjectType = gameObjectType;
	}
}
