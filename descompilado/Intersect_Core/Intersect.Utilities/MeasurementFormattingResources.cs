using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Resources;
using System.Runtime.CompilerServices;

namespace Intersect.Utilities;

[GeneratedCode("System.Resources.Tools.StronglyTypedResourceBuilder", "4.0.0.0")]
[DebuggerNonUserCode]
[CompilerGenerated]
internal class MeasurementFormattingResources
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
				ResourceManager resourceManager = new ResourceManager("Intersect.Utilities.MeasurementFormattingResources", typeof(MeasurementFormattingResources).Assembly);
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

	internal static string TimeSecondSuffix => ResourceManager.GetString("TimeSecondSuffix", resourceCulture);

	internal static string TimeMinuteSuffix => ResourceManager.GetString("TimeMinuteSuffix", resourceCulture);

	internal static string TimeNanosecondSuffix => ResourceManager.GetString("TimeNanosecondSuffix", resourceCulture);

	internal static string TimeMicrosecondSuffix => ResourceManager.GetString("TimeMicrosecondSuffix", resourceCulture);

	internal static string TimeMillisecondSuffix => ResourceManager.GetString("TimeMillisecondSuffix", resourceCulture);

	internal static string TimeDaySuffix => ResourceManager.GetString("TimeDaySuffix", resourceCulture);

	internal static string TimeMonthSuffix => ResourceManager.GetString("TimeMonthSuffix", resourceCulture);

	internal static string TimeYearSuffix => ResourceManager.GetString("TimeYearSuffix", resourceCulture);

	internal static string TimeWeekSuffix => ResourceManager.GetString("TimeWeekSuffix", resourceCulture);

	internal static string TimeHourSuffix => ResourceManager.GetString("TimeHourSuffix", resourceCulture);

	internal MeasurementFormattingResources()
	{
	}
}
