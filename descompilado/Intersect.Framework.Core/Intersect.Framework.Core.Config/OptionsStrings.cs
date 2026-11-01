using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Resources;
using System.Runtime.CompilerServices;

namespace Intersect.Framework.Core.Config;

[GeneratedCode("System.Resources.Tools.StronglyTypedResourceBuilder", "4.0.0.0")]
[DebuggerNonUserCode]
[CompilerGenerated]
internal class OptionsStrings
{
	private static ResourceManager resourceMan;

	private static CultureInfo resourceCulture;

	[EditorBrowsable(EditorBrowsableState.Advanced)]
	internal static ResourceManager ResourceManager
	{
		get
		{
			if (object.Equals(null, resourceMan))
			{
				ResourceManager resourceManager = new ResourceManager("Intersect.Framework.Core.Config.OptionsStrings", typeof(OptionsStrings).Assembly);
				resourceMan = resourceManager;
			}
			return resourceMan;
		}
	}

	[EditorBrowsable(EditorBrowsableState.Advanced)]
	internal static CultureInfo Culture
	{
		get
		{
			return resourceCulture;
		}
		set
		{
			resourceCulture = value;
		}
	}

	internal static string Category_General => ResourceManager.GetString("Category_General", resourceCulture);

	internal static string Category_Logging => ResourceManager.GetString("Category_Logging", resourceCulture);

	internal static string Category_Metrics => ResourceManager.GetString("Category_Metrics", resourceCulture);

	internal static string Category_GameDatabase => ResourceManager.GetString("Category_GameDatabase", resourceCulture);

	internal static string Category_LoggingDatabase => ResourceManager.GetString("Category_LoggingDatabase", resourceCulture);

	internal static string Category_PlayerDatabase => ResourceManager.GetString("Category_PlayerDatabase", resourceCulture);

	internal static string Category_Security => ResourceManager.GetString("Category_Security", resourceCulture);

	internal static string Category_SmtpSettings => ResourceManager.GetString("Category_SmtpSettings", resourceCulture);

	internal static string Category_Chat => ResourceManager.GetString("Category_Chat", resourceCulture);

	internal static string Category_Packets => ResourceManager.GetString("Category_Packets", resourceCulture);

	internal static string Category_Combat => ResourceManager.GetString("Category_Combat", resourceCulture);

	internal OptionsStrings()
	{
	}
}
