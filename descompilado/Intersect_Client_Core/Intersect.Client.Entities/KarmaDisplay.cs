using Intersect.Client.Localization;
using Intersect.Config;

namespace Intersect.Client.Entities;

public static class KarmaDisplay
{
	public static readonly Color AggressorColor = new Color(255, 232, 64, 64);

	public static LabelColor NameColors(KarmaTier tier)
	{
		if (!CustomColors.Names.Players.TryGetValue(tier switch
		{
			KarmaTier.Hero => "Hero", 
			KarmaTier.Suspect => "Suspect", 
			KarmaTier.Criminal => "Criminal", 
			_ => "Normal", 
		}, out var value))
		{
			return CustomColors.Names.Players["Normal"];
		}
		return value;
	}

	public static string TierName(KarmaTier tier)
	{
		return tier switch
		{
			KarmaTier.Hero => Strings.Karma.Hero, 
			KarmaTier.Suspect => Strings.Karma.Suspect, 
			KarmaTier.Criminal => Strings.Karma.Criminal, 
			_ => Strings.Karma.Neutral, 
		};
	}
}
