using System;
using Newtonsoft.Json;

namespace Intersect.Localization;

[Serializable]
public class LocaleCommand : LocaleDescribableToken
{
	[JsonProperty("Help", NullValueHandling = NullValueHandling.Ignore)]
	private LocalizedString mHelp;

	[JsonIgnore]
	public LocalizedString Help
	{
		get
		{
			return mHelp ?? ((LocalizedString)"");
		}
		set
		{
			if (mHelp == null)
			{
				mHelp = value;
			}
		}
	}

	public LocaleCommand()
	{
	}

	public LocaleCommand(string name, string description = null, string help = null)
		: base(name, description)
	{
		mHelp = help?.Trim();
	}
}
