using System;
using Newtonsoft.Json;

namespace Intersect.Localization;

[Serializable]
public class LocaleToken : LocaleNamespace
{
	[JsonIgnore]
	private LocalizedString mName;

	[JsonProperty("Name", NullValueHandling = NullValueHandling.Ignore)]
	protected LocalizedString JsonName
	{
		get
		{
			return mName;
		}
		set
		{
			if (value != null && value.ToString().Length < 2)
			{
				throw new ArgumentException($"Token names must be at least 2 characters long, but '{value}' was provided.");
			}
			mName = value;
		}
	}

	[JsonIgnore]
	public virtual LocalizedString Name
	{
		get
		{
			return mName ?? throw new InvalidOperationException();
		}
		set
		{
			if (mName == null)
			{
				mName = value;
			}
		}
	}

	public LocaleToken()
	{
	}

	public LocaleToken(string name)
	{
		if (string.IsNullOrWhiteSpace(name))
		{
			throw new ArgumentException("Parameter 'name' cannot be null or whitespace.");
		}
		mName = name.Trim();
	}
}
