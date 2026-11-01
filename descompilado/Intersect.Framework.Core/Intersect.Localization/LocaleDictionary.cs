using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Intersect.Localization;

public class LocaleDictionary<TKey, TValue> : Localized, IDictionary<TKey, TValue>, ICollection<KeyValuePair<TKey, TValue>>, IEnumerable<KeyValuePair<TKey, TValue>>, IEnumerable where TValue : Localized
{
	private readonly IDictionary<TKey, TValue> mDefaults;

	private readonly IDictionary<TKey, TValue> mValues;

	private bool mDefaultsFrozen;

	private ICollection<KeyValuePair<TKey, TValue>> Pairs => Keys.Select(delegate(TKey key)
	{
		if (key == null)
		{
			throw new ArgumentNullException("key");
		}
		if (mValues.TryGetValue(key, out var value) || mDefaults.TryGetValue(key, out value))
		{
			return new KeyValuePair<TKey, TValue>(key, value);
		}
		throw new InvalidOperationException();
	}).ToList();

	public TValue this[TKey key]
	{
		get
		{
			if (!mValues.TryGetValue(key, out var value))
			{
				return mDefaults[key];
			}
			return value;
		}
		set
		{
			if (mDefaultsFrozen || mDefaults.ContainsKey(key))
			{
				mValues[key] = value;
			}
			else
			{
				mDefaults[key] = value;
			}
		}
	}

	public int Count => mDefaults.Count;

	public bool IsReadOnly => true;

	public ICollection<TKey> Keys => mDefaults.Keys;

	public ICollection<TValue> Values => Keys.Select(delegate(TKey key)
	{
		if (key == null)
		{
			throw new InvalidOperationException();
		}
		return this[key];
	}).ToList();

	public LocaleDictionary(IEnumerable<KeyValuePair<TKey, TValue>> defaults = null, IEnumerable<KeyValuePair<TKey, TValue>> values = null)
	{
		SortedDictionary<TKey, TValue> sortedDictionary;
		if (defaults != null)
		{
			IDictionary<TKey, TValue> dictionary3;
			if (!(defaults is IDictionary<TKey, TValue> dictionary))
			{
				IDictionary<TKey, TValue> dictionary2 = defaults.ToDictionary((KeyValuePair<TKey, TValue> pair) => pair.Key, (KeyValuePair<TKey, TValue> pair) => pair.Value);
				dictionary3 = dictionary2;
			}
			else
			{
				dictionary3 = dictionary;
			}
			sortedDictionary = new SortedDictionary<TKey, TValue>(dictionary3);
		}
		else
		{
			sortedDictionary = new SortedDictionary<TKey, TValue>();
		}
		mDefaults = sortedDictionary;
		SortedDictionary<TKey, TValue> sortedDictionary2;
		if (values != null)
		{
			IDictionary<TKey, TValue> dictionary5;
			if (!(values is IDictionary<TKey, TValue> dictionary4))
			{
				IDictionary<TKey, TValue> dictionary2 = values.ToDictionary((KeyValuePair<TKey, TValue> pair) => pair.Key, (KeyValuePair<TKey, TValue> pair) => pair.Value);
				dictionary5 = dictionary2;
			}
			else
			{
				dictionary5 = dictionary4;
			}
			sortedDictionary2 = new SortedDictionary<TKey, TValue>(dictionary5);
		}
		else
		{
			sortedDictionary2 = new SortedDictionary<TKey, TValue>();
		}
		mValues = sortedDictionary2;
	}

	public void Add(TKey key, TValue value)
	{
		if (mDefaultsFrozen || mDefaults.ContainsKey(key))
		{
			mValues.Add(key, value);
		}
		else
		{
			mDefaults.Add(key, value);
		}
	}

	public bool ContainsKey(TKey key)
	{
		return mDefaults.ContainsKey(key);
	}

	public void FreezeDefaults()
	{
		mDefaultsFrozen = true;
	}

	public bool Remove(TKey key)
	{
		return mValues.Remove(key);
	}

	public bool TryGetValue(TKey key, out TValue value)
	{
		if (!mValues.TryGetValue(key, out value))
		{
			return mDefaults.TryGetValue(key, out value);
		}
		return true;
	}

	public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
	{
		return Pairs.GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}

	public void Add(KeyValuePair<TKey, TValue> item)
	{
		if (mDefaultsFrozen || mDefaults.ContainsKey(item.Key))
		{
			mValues.Add(item);
		}
		else
		{
			mDefaults.Add(item);
		}
	}

	public void Clear()
	{
		mValues.Clear();
	}

	public bool Contains(KeyValuePair<TKey, TValue> item)
	{
		return mValues.Contains(item);
	}

	public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
	{
		Pairs.CopyTo(array, arrayIndex);
	}

	public bool Remove(KeyValuePair<TKey, TValue> item)
	{
		return mValues.Remove(item);
	}
}
