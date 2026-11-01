using System.Globalization;
using System.Resources;

namespace Intersect.Framework.Resources;

public static class ResourceManagerExtensions
{
	public static string? GetStringWithFallback(this ResourceManager resourceManager, string name, CultureInfo? cultureInfo = null, bool fallbackToResourceName = false)
	{
		if (cultureInfo == null)
		{
			cultureInfo = CultureInfo.CurrentCulture;
		}
		while (cultureInfo.LCID != CultureInfo.InvariantCulture.LCID)
		{
			string text = resourceManager.GetString(name, cultureInfo);
			if (!string.IsNullOrWhiteSpace(text))
			{
				return text;
			}
			cultureInfo = cultureInfo.Parent;
		}
		if (!fallbackToResourceName)
		{
			return null;
		}
		return name;
	}
}
