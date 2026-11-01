using System;
using System.Collections.Generic;
using System.Linq;

namespace Intersect.Collections;

public struct Sanitizer
{
	private Dictionary<string, SanitizedValue<object>>? _sanitizedValues;

	public Dictionary<string, SanitizedValue<object>> Sanitized => _sanitizedValues?.ToDictionary() ?? new Dictionary<string, SanitizedValue<object>>();

	public Sanitizer Add<T>(string name, T before, T after)
	{
		if (string.IsNullOrWhiteSpace(name))
		{
			throw new ArgumentNullException("name");
		}
		if (_sanitizedValues == null)
		{
			_sanitizedValues = new Dictionary<string, SanitizedValue<object>>(8);
		}
		_sanitizedValues.Add(name, new SanitizedValue<object>(before, after));
		return this;
	}

	public T IsNotNull<T>(string name, T value, T defaultValue) where T : class
	{
		if (value == null)
		{
			Add(name, null, defaultValue);
		}
		return defaultValue;
	}

	public T IsNull<T>(string name, T value) where T : class
	{
		if (value != null)
		{
			Add(name, value, null);
		}
		return null;
	}

	public T Is<T>(string name, T actualValue, T expectedValue)
	{
		if (expectedValue != null || actualValue != null)
		{
			ref T reference = ref expectedValue;
			T val = default(T);
			if (val == null)
			{
				val = reference;
				if (val == null)
				{
					goto IL_004b;
				}
			}
			object obj = actualValue;
			if (!reference.Equals(obj))
			{
				goto IL_004b;
			}
		}
		return actualValue;
		IL_004b:
		Add(name, actualValue, expectedValue);
		return expectedValue;
	}

	public T IsNot<T>(string name, T actualValue, T expectedValue)
	{
		if (expectedValue != null || actualValue == null)
		{
			ref T reference = ref expectedValue;
			T val = default(T);
			if (val == null)
			{
				val = reference;
				if (val == null)
				{
					goto IL_0049;
				}
			}
			object obj = actualValue;
			if (reference.Equals(obj))
			{
				Add(name, actualValue, expectedValue);
				return expectedValue;
			}
		}
		goto IL_0049;
		IL_0049:
		return actualValue;
	}

	public T MaximumExclusive<T>(string name, T actualValue, T expectedMinimum) where T : IComparable<T>
	{
		if (expectedMinimum.CompareTo(actualValue) < 0)
		{
			return actualValue;
		}
		Add(name, actualValue, expectedMinimum);
		return expectedMinimum;
	}

	public T Maximum<T>(string name, T actualValue, T expectedMinimum) where T : IComparable<T>
	{
		if (expectedMinimum.CompareTo(actualValue) <= 0)
		{
			return actualValue;
		}
		Add(name, actualValue, expectedMinimum);
		return expectedMinimum;
	}

	public T MinimumExclusive<T>(string name, T actualValue, T expectedMaximum) where T : IComparable<T>
	{
		if (expectedMaximum.CompareTo(actualValue) > 0)
		{
			return actualValue;
		}
		Add(name, actualValue, expectedMaximum);
		return expectedMaximum;
	}

	public T Minimum<T>(string name, T actualValue, T expectedMaximum) where T : IComparable<T>
	{
		if (expectedMaximum.CompareTo(actualValue) >= 0)
		{
			return actualValue;
		}
		Add(name, actualValue, expectedMaximum);
		return expectedMaximum;
	}

	public T Clamp<T>(string name, T actualValue, T expectedMinimum, T expectedMaximum) where T : IComparable<T>
	{
		if (expectedMinimum.CompareTo(actualValue) > 0)
		{
			Add(name, actualValue, expectedMinimum);
			return expectedMinimum;
		}
		if (expectedMaximum.CompareTo(actualValue) >= 0)
		{
			return actualValue;
		}
		Add(name, actualValue, expectedMaximum);
		return expectedMaximum;
	}

	public T ClampExclusive<T>(string name, T actualValue, T expectedMinimum, T expectedMaximum) where T : IComparable<T>
	{
		if (expectedMinimum.CompareTo(actualValue) >= 0)
		{
			Add(name, actualValue, expectedMinimum);
			return expectedMinimum;
		}
		if (expectedMaximum.CompareTo(actualValue) > 0)
		{
			return actualValue;
		}
		Add(name, actualValue, expectedMaximum);
		return expectedMaximum;
	}

	public T ClampExclusiveMinimum<T>(string name, T actualValue, T expectedMinimum, T expectedMaximum) where T : IComparable<T>
	{
		if (expectedMinimum.CompareTo(actualValue) > 0)
		{
			Add(name, actualValue, expectedMinimum);
			return expectedMinimum;
		}
		if (expectedMaximum.CompareTo(actualValue) >= 0)
		{
			return actualValue;
		}
		Add(name, actualValue, expectedMaximum);
		return expectedMaximum;
	}

	public T ClampExclusiveMaximum<T>(string name, T actualValue, T expectedMinimum, T expectedMaximum) where T : IComparable<T>
	{
		if (expectedMinimum.CompareTo(actualValue) >= 0)
		{
			Add(name, actualValue, expectedMinimum);
			return expectedMinimum;
		}
		if (expectedMaximum.CompareTo(actualValue) > 0)
		{
			return actualValue;
		}
		Add(name, actualValue, expectedMaximum);
		return expectedMaximum;
	}
}
