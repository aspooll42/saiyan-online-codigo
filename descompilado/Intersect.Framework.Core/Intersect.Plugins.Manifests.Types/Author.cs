using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace Intersect.Plugins.Manifests.Types;

[JsonConverter(typeof(AuthorStringConverter))]
public struct Author : IComparable<Author>, IEquatable<Author>, IEquatable<string>, ICloneable, IComparable
{
	private class AuthorStringConverter : JsonConverter<Author>
	{
		public override bool CanRead => false;

		[ExcludeFromCodeCoverage]
		public override Author ReadJson(JsonReader reader, Type objectType, Author existingValue, bool hasExistingValue, JsonSerializer serializer)
		{
			throw new NotImplementedException();
		}

		public override void WriteJson(JsonWriter writer, Author value, JsonSerializer serializer)
		{
			writer?.WriteValue(value.ToString());
		}
	}

	public static readonly Regex AuthorStringExpression = new Regex("^\\s*(.+?)(?:\\s+<([^>]+)>)?(?:\\s+\\(([^\\\\)]+)\\))?\\s*$");

	public readonly string Email;

	public readonly string Homepage;

	public readonly string Name;

	private readonly int _hashCode;

	private string[]? _parts;

	public static Author Empty => default(Author);

	private string[] Parts
	{
		get
		{
			string[] array = _parts;
			if (array == null)
			{
				string[] obj = new string[3] { Name, Email, Homepage };
				string[] array2 = obj;
				_parts = obj;
				array = array2;
			}
			return array;
		}
	}

	public Author(string authorString)
	{
		_parts = null;
		if (string.IsNullOrWhiteSpace(authorString))
		{
			throw new ArgumentNullException("authorString", "Author string is null or whitespace.");
		}
		Match match = AuthorStringExpression.Match(authorString);
		Group obj = match.Groups[1];
		Group obj2 = match.Groups[2];
		Group obj3 = match.Groups[3];
		Name = obj.Value;
		Email = obj2.Value;
		Homepage = obj3.Value;
		_hashCode = HashCode.Combine(Name, Email, Homepage);
	}

	[JsonConstructor]
	public Author(string name, string email, string? homepage = null)
	{
		_parts = null;
		Name = name;
		Email = email;
		Homepage = homepage ?? string.Empty;
		_hashCode = HashCode.Combine(Name, Email, Homepage);
	}

	public Author Clone()
	{
		return new Author(Name, Email, Homepage);
	}

	object ICloneable.Clone()
	{
		return Clone();
	}

	public int CompareTo(object obj)
	{
		if (!(obj is Author other))
		{
			throw new NotSupportedException("Comparison only supported with Author.");
		}
		return CompareTo(other);
	}

	public int CompareTo(Author other)
	{
		string[] parts = Parts;
		string[] otherParts = other.Parts;
		int num = parts.Length.CompareTo(otherParts.Length);
		if (num == 0)
		{
			return parts.Select((string t, int index) => string.Compare(t, otherParts[index], StringComparison.Ordinal)).FirstOrDefault((int comparison) => comparison != 0);
		}
		return num;
	}

	public override bool Equals(object? obj)
	{
		if (obj is Author other)
		{
			return Equals(other);
		}
		return false;
	}

	public bool Equals(Author other)
	{
		return this == other;
	}

	public bool Equals(string? other)
	{
		if (other != null)
		{
			return (Author)other == this;
		}
		return false;
	}

	public override int GetHashCode()
	{
		return _hashCode;
	}

	public override string ToString()
	{
		return Name + (string.IsNullOrEmpty(Email) ? "" : (" <" + Email + ">")) + (string.IsNullOrEmpty(Homepage) ? "" : (" (" + Homepage + ")"));
	}

	public static implicit operator Author(string authorString)
	{
		if (authorString != null)
		{
			return new Author(authorString);
		}
		return Empty;
	}

	public static implicit operator string(Author author)
	{
		return author.ToString();
	}

	public static bool operator !=(Author a, Author b)
	{
		if (string.Equals(a.Name, b.Name, StringComparison.Ordinal) && string.Equals(a.Email, b.Email, StringComparison.Ordinal))
		{
			return !string.Equals(a.Homepage, b.Homepage, StringComparison.Ordinal);
		}
		return true;
	}

	public static bool operator ==(Author a, Author b)
	{
		if (string.Equals(a.Name, b.Name, StringComparison.Ordinal) && string.Equals(a.Email, b.Email, StringComparison.Ordinal))
		{
			return string.Equals(a.Homepage, b.Homepage, StringComparison.Ordinal);
		}
		return false;
	}
}
