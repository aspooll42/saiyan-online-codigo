using System;
using Newtonsoft.Json;

namespace Intersect.Localization;

[Serializable]
public class LocaleDescribableToken : LocaleToken
{
	[JsonProperty("Description", NullValueHandling = NullValueHandling.Ignore)]
	private LocalizedString mDescription;

	[JsonIgnore]
	public virtual LocalizedString Description
	{
		get
		{
			return mDescription ?? ((LocalizedString)"");
		}
		set
		{
			if (mDescription == null)
			{
				mDescription = value;
			}
		}
	}

	public LocaleDescribableToken()
	{
	}

	public LocaleDescribableToken(string name, string description = null)
		: base(name)
	{
		mDescription = description?.Trim();
	}
}
