using System;
using System.ComponentModel;
using Newtonsoft.Json;

namespace Intersect.Localization;

[Serializable]
public class LocaleArgument : LocaleDescribableToken
{
	[JsonProperty("ShortName", NullValueHandling = NullValueHandling.Ignore, DefaultValueHandling = DefaultValueHandling.Ignore)]
	[DefaultValue('\0')]
	private char mShortName;

	[JsonIgnore]
	public virtual char ShortName
	{
		get
		{
			return mShortName;
		}
		set
		{
			if (mShortName == '\0')
			{
				mShortName = value;
			}
		}
	}

	public LocaleArgument()
	{
	}

	public LocaleArgument(string name, char shortName = '\0', string description = null)
		: base(name, description)
	{
		mShortName = shortName;
	}
}
