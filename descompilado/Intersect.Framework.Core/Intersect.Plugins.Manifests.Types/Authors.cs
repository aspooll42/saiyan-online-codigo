using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace Intersect.Plugins.Manifests.Types;

public struct Authors : IComparable<IEnumerable<Author>>, IComparable<IEnumerable<string>>, IEquatable<IEnumerable<Author>>, IEquatable<IEnumerable<string>>, IEquatable<string>, IReadOnlyList<Author>, IEnumerable<Author>, IEnumerable, IReadOnlyCollection<Author>, ICloneable, IComparable, IStructuralComparable, IStructuralEquatable
{
	public const string ExceptionComparisonNotSupported = "Comparison only supported with IEnumerable<Author> or IEnumerable<string>.";

	private readonly Author[] _authors;

	private readonly int _hashCode;

	public int Count => _authors.Length;

	public Author this[int index] => _authors[index];

	public Authors(params Author[] authors)
		: this((IEnumerable<Author>)authors)
	{
	}

	public Authors(IEnumerable<Author> authors)
	{
		_authors = authors.ToArray();
		HashCode hashCode = default(HashCode);
		Author[] authors2 = _authors;
		foreach (Author value in authors2)
		{
			hashCode.Add(value);
		}
		_hashCode = hashCode.ToHashCode();
	}

	public Authors Clone()
	{
		return new Authors(_authors.Select((Author author) => author.Clone()));
	}

	object ICloneable.Clone()
	{
		return Clone();
	}

	public int CompareTo(object other, IComparer comparer)
	{
		return comparer.Compare(this, other);
	}

	public int CompareTo(IEnumerable<Author> other)
	{
		return other?.Select(CompareElement).FirstOrDefault((int comparison) => comparison != 0) ?? 0;
	}

	public int CompareTo(IEnumerable<string> other)
	{
		return other?.Select(CompareElement).FirstOrDefault((int comparison) => comparison != 0) ?? 0;
	}

	public int CompareTo(object obj)
	{
		if (!(obj is IEnumerable<Author> other))
		{
			if (obj is IEnumerable<string> other2)
			{
				return CompareTo(other2);
			}
			throw new NotSupportedException("Comparison only supported with IEnumerable<Author> or IEnumerable<string>.");
		}
		return CompareTo(other);
	}

	public bool Equals(object other, IEqualityComparer comparer)
	{
		if (!(other is IEnumerable<Author> y))
		{
			if (other is IEnumerable<string> y2)
			{
				return comparer.Equals(this, y2);
			}
			throw new NotSupportedException("Comparison only supported with IEnumerable<Author> or IEnumerable<string>.");
		}
		return comparer.Equals(_authors, y);
	}

	public bool Equals(IEnumerable<Author> other)
	{
		return Equals(other, EqualsElement);
	}

	public bool Equals(IEnumerable<string> other)
	{
		return Equals(other, EqualsElement);
	}

	public override bool Equals(object obj)
	{
		if (!(obj is IEnumerable<Author> other))
		{
			if (obj is IEnumerable<string> other2)
			{
				return Equals(other2);
			}
			throw new NotSupportedException("Comparison only supported with IEnumerable<Author> or IEnumerable<string>.");
		}
		return Equals(other);
	}

	public bool Equals(string other)
	{
		return this == (Authors)other;
	}

	public IEnumerator<Author> GetEnumerator()
	{
		return ((IEnumerable<Author>)_authors).GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return _authors.GetEnumerator();
	}

	public int GetHashCode(IEqualityComparer comparer)
	{
		return comparer.GetHashCode(this);
	}

	public override int GetHashCode()
	{
		return _hashCode;
	}

	public override string ToString()
	{
		return JsonConvert.SerializeObject(_authors);
	}

	private static bool Equals<TEnumerableValue>(IEnumerable<TEnumerableValue> other, Func<TEnumerableValue, int, bool> comparisonFunc)
	{
		return other?.Select(comparisonFunc).All((bool equality) => equality) ?? false;
	}

	private int CompareElement(Author author, int index)
	{
		if (index >= _authors.Length)
		{
			return -1;
		}
		return _authors[index].CompareTo(author);
	}

	private int CompareElement(string authorString, int index)
	{
		if (index >= _authors.Length)
		{
			return -1;
		}
		return _authors[index].CompareTo(authorString);
	}

	private bool EqualsElement(Author author, int index)
	{
		if (index < _authors.Length)
		{
			return _authors[index].Equals(author);
		}
		return false;
	}

	private bool EqualsElement(string authorString, int index)
	{
		if (index < _authors.Length)
		{
			return _authors[index].Equals(authorString);
		}
		return false;
	}

	public static implicit operator Authors(string authorsString)
	{
		try
		{
			return JsonConvert.DeserializeObject<Authors>(authorsString);
		}
		catch
		{
			return new Author(authorsString);
		}
	}

	public static implicit operator Authors(Author author)
	{
		return new Authors(author);
	}

	public static implicit operator Authors(string[] authorStrings)
	{
		return new Authors(authorStrings.Select((string authorString) => new Author(authorString)));
	}

	public static implicit operator Authors(Author[] authors)
	{
		return new Authors(authors ?? Array.Empty<Author>());
	}

	public static implicit operator string(Authors authors)
	{
		return authors.ToString();
	}

	public static bool operator !=(Authors a, Authors b)
	{
		return !a.Equals((IEnumerable<Author>)b);
	}

	public static bool operator ==(Authors a, Authors b)
	{
		return a.Equals((IEnumerable<Author>)b);
	}
}
