using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Text;
using Intersect.Core;
using Intersect.Enums;
using Intersect.Framework.Reflection;
using Intersect.Models;
using Intersect.Utilities;
using Microsoft.Extensions.Logging;

namespace Intersect.Collections;

public class DatabaseObjectLookup : IGameObjectLookup<IDatabaseObject>, ILookup<Guid, IDatabaseObject>, IEnumerable<KeyValuePair<Guid, IDatabaseObject>>, IEnumerable
{
	public static readonly Dictionary<Type, DatabaseObjectLookup> LookupMap = (from gameObjectType in Enum.GetValues<GameObjectType>()
		select gameObjectType.GetObjectType()).ToArray().ToDictionary((Type type) => type, (Type type) => new DatabaseObjectLookup(type));

	private readonly SortedDictionary<Guid, IDatabaseObject> mIdMap;

	private Dictionary<Guid, IDatabaseObject> mCachedClone;

	private bool mIsDirty = true;

	private int mVersion;

	private List<IDatabaseObject>? mValeursTriees;

	private ReadOnlyCollection<IDatabaseObject>? mValeursTrieesEnLecture;

	private int mVersionTriee = -1;

	private readonly object mLock;

	public Type StoredType { get; }

	public virtual IDatabaseObject this[Guid id]
	{
		get
		{
			return Get(id);
		}
		set
		{
			Set(id, value);
		}
	}

	public List<Guid> KeyList
	{
		get
		{
			lock (mLock)
			{
				return mIdMap.Keys.ToList();
			}
		}
	}

	public List<IDatabaseObject> ValueList
	{
		get
		{
			lock (mLock)
			{
				try
				{
					return new List<IDatabaseObject>(ValeursTriees());
				}
				catch (Exception exception)
				{
					ApplicationContext.Context.Value?.Logger.LogWarning(exception, "{StoredType}[Count={Count}, Null={NullCount}]", StoredType.GetName(qualified: true), mIdMap.Count, mIdMap.Count((KeyValuePair<Guid, IDatabaseObject> pair) => pair.Value == null));
					throw;
				}
			}
		}
	}

	public Type IndexKeyType => typeof(int);

	public bool IsEmpty => Count < 1;

	public Type KeyType => typeof(Guid);

	public Type ValueType => typeof(IDatabaseObject);

	public virtual int Count
	{
		get
		{
			lock (mLock)
			{
				return mIdMap.Count;
			}
		}
	}

	public virtual IDictionary<Guid, IDatabaseObject> Clone
	{
		get
		{
			if (mIsDirty || mCachedClone == null)
			{
				lock (mLock)
				{
					mCachedClone = mIdMap.ToDictionary((KeyValuePair<Guid, IDatabaseObject> pair) => pair.Key, (KeyValuePair<Guid, IDatabaseObject> pair) => pair.Value);
					mIsDirty = false;
				}
			}
			return mCachedClone;
		}
	}

	public virtual ICollection<KeyValuePair<Guid, IDatabaseObject>> Pairs => Clone;

	public virtual ICollection<Guid> Keys
	{
		get
		{
			lock (mLock)
			{
				return mIdMap.Keys;
			}
		}
	}

	public virtual ICollection<IDatabaseObject> Values
	{
		get
		{
			lock (mLock)
			{
				ValeursTriees();
				return mValeursTrieesEnLecture;
			}
		}
	}

	public static DatabaseObjectLookup GetLookup(Type type)
	{
		if (!LookupMap.TryGetValue(type, out var value))
		{
			throw new InvalidOperationException($"The lookup for {type.FullName ?? type.Name} should have been populated in the initializer, is the {"GameObjectType"} enum not set up with this type?");
		}
		return value;
	}

	public DatabaseObjectLookup(Type storedType)
	{
		mLock = new object();
		mIdMap = new SortedDictionary<Guid, IDatabaseObject>();
		StoredType = storedType;
	}

	private List<IDatabaseObject> ValeursTriees()
	{
		lock (mLock)
		{
			if (mValeursTriees == null || mVersionTriee != mVersion)
			{
				mValeursTriees = mIdMap.Values.OrderBy((IDatabaseObject databaseObject) => databaseObject?.Name).ToList();
				mValeursTrieesEnLecture = mValeursTriees.AsReadOnly();
				mVersionTriee = mVersion;
			}
			return mValeursTriees;
		}
	}

	public virtual IDatabaseObject Get(Guid id)
	{
		if (!TryGetValue(id, out var value))
		{
			return null;
		}
		return value;
	}

	public virtual TObject Get<TObject>(Guid id) where TObject : IDatabaseObject
	{
		if (!TryGetValue(id, out TObject value))
		{
			return default(TObject);
		}
		return value;
	}

	public virtual bool TryGetValue<TObject>(Guid id, out TObject value) where TObject : IDatabaseObject
	{
		if (TryGetValue(id, out var value2))
		{
			value = (TObject)value2;
			return true;
		}
		value = default(TObject);
		return false;
	}

	public virtual bool TryGetValue(Guid id, out IDatabaseObject value)
	{
		if (!IsIdValid(id))
		{
			value = null;
			return false;
		}
		lock (mLock)
		{
			return mIdMap.TryGetValue(id, out value);
		}
	}

	public bool Add(IDatabaseObject value)
	{
		return InternalSet(value, overwrite: false);
	}

	public IDatabaseObject AddNew(Type type, Guid id)
	{
		ConstructorInfo constructor = type.GetConstructor(new Type[1] { KeyType });
		if (constructor == null)
		{
			throw new ArgumentNullException("idConstructor", MessageNoConstructor(type, KeyType.Name));
		}
		IDatabaseObject databaseObject = (IDatabaseObject)constructor.Invoke(new object[1] { id });
		if (databaseObject == null)
		{
			throw new ArgumentNullException($"Failed to create instance of '{ValueType.Name}' with the ({KeyType.Name}) constructor.");
		}
		if (!InternalSet(databaseObject, overwrite: false))
		{
			return null;
		}
		return databaseObject;
	}

	public virtual bool Set(Guid key, IDatabaseObject value)
	{
		if (key != (value?.Id ?? Guid.Empty))
		{
			throw new ArgumentException("Key does not match the Guid of the value.", "key");
		}
		return InternalSet(value, overwrite: true);
	}

	public virtual bool Delete(IDatabaseObject value)
	{
		if (value == null)
		{
			throw new ArgumentNullException();
		}
		if (!IsIdValid(value.Id))
		{
			throw new ArgumentOutOfRangeException();
		}
		lock (mLock)
		{
			mIsDirty = true;
			mVersion++;
			return mIdMap.Remove(value.Id);
		}
	}

	public virtual bool DeleteAt(Guid guid)
	{
		if (!IsIdValid(guid))
		{
			throw new ArgumentOutOfRangeException();
		}
		IDatabaseObject value;
		lock (mLock)
		{
			if (!mIdMap.TryGetValue(guid, out value))
			{
				return false;
			}
		}
		return Delete(value);
	}

	public virtual void Clear()
	{
		lock (mLock)
		{
			mIsDirty = true;
			mVersion++;
			mIdMap.Clear();
		}
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}

	public virtual IEnumerator<KeyValuePair<Guid, IDatabaseObject>> GetEnumerator()
	{
		return Clone.GetEnumerator();
	}

	public IDatabaseObject AddNew(Type type, int index)
	{
		if (type == null)
		{
			throw new ArgumentNullException("type", "No type specified.");
		}
		ConstructorInfo constructor = type.GetConstructor(new Type[2] { KeyType, IndexKeyType });
		if (constructor != null)
		{
			return AddNew(type, Guid.NewGuid(), index);
		}
		ConstructorInfo constructor2 = type.GetConstructor(new Type[1] { IndexKeyType });
		if (constructor2 == null)
		{
			throw new ArgumentNullException("indexConstructor", MessageNoConstructor(type, IndexKeyType.Name));
		}
		IDatabaseObject databaseObject = (IDatabaseObject)constructor2.Invoke(new object[1] { index });
		if (databaseObject == null)
		{
			throw new ArgumentNullException($"Failed to create instance of '{ValueType.Name}' with the ({IndexKeyType.Name}) constructor.");
		}
		if (!InternalSet(databaseObject, overwrite: false))
		{
			return null;
		}
		return databaseObject;
	}

	public IDatabaseObject AddNew(Type type, Guid id, int index)
	{
		if (type == null)
		{
			throw new ArgumentNullException("type", "No type specified.");
		}
		ConstructorInfo constructor = ValueType.GetConstructor(new Type[2] { KeyType, IndexKeyType });
		if (constructor == null)
		{
			throw new ArgumentNullException("mixedConstructor", MessageNoConstructor(type, KeyType.Name, IndexKeyType.Name));
		}
		IDatabaseObject databaseObject = (IDatabaseObject)constructor.Invoke(new object[2] { id, index });
		if (databaseObject == null)
		{
			throw new ArgumentNullException($"Failed to create instance of '{ValueType.Name}' with the ({KeyType.Name}, {IndexKeyType.Name}) constructor.");
		}
		if (!InternalSet(databaseObject, overwrite: false))
		{
			return null;
		}
		return databaseObject;
	}

	protected virtual bool IsIdValid(Guid id)
	{
		return id != Guid.Empty;
	}

	protected virtual bool IsIndexValid(int index)
	{
		return index > -1;
	}

	internal virtual bool InternalSet(IDatabaseObject value, bool overwrite)
	{
		if (value == null)
		{
			throw new ArgumentNullException("value");
		}
		if (!IsIdValid(value.Id))
		{
			throw new ArgumentOutOfRangeException("Id");
		}
		lock (mLock)
		{
			if (!overwrite && mIdMap.ContainsKey(value.Id))
			{
				return false;
			}
			mIsDirty = true;
			mVersion++;
			mIdMap[value.Id] = value;
			return true;
		}
	}

	private static string MessageNoConstructor(Type type, params string[] constructorMessage)
	{
		string value = string.Join(',', constructorMessage);
		StringBuilder stringBuilder = new StringBuilder();
		StringBuilder stringBuilder2 = stringBuilder;
		StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(30, 2, stringBuilder2);
		handler.AppendLiteral("No (");
		handler.AppendFormatted(value);
		handler.AppendLiteral(") constructor for type '");
		handler.AppendFormatted(type.Name);
		handler.AppendLiteral("'.");
		stringBuilder2.AppendLine(ref handler);
		stringBuilder.AppendLine(type.StringifyConstructors());
		return stringBuilder.ToString();
	}
}
