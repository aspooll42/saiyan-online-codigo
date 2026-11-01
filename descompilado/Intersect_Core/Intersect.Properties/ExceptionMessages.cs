using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Resources;
using System.Runtime.CompilerServices;

namespace Intersect.Properties;

[GeneratedCode("System.Resources.Tools.StronglyTypedResourceBuilder", "17.0.0.0")]
[DebuggerNonUserCode]
[CompilerGenerated]
internal class ExceptionMessages
{
	private static ResourceManager resourceMan;

	private static CultureInfo resourceCulture;

	[EditorBrowsable(EditorBrowsableState.Advanced)]
	internal static ResourceManager ResourceManager
	{
		get
		{
			if (resourceMan == null)
			{
				ResourceManager resourceManager = new ResourceManager("Intersect.Properties.ExceptionMessages", typeof(ExceptionMessages).Assembly);
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

	internal static string FoundPluginEntryTypeMissingDefaultConstructor => ResourceManager.GetString("FoundPluginEntryTypeMissingDefaultConstructor", resourceCulture);

	internal static string MissingPluginAssembly => ResourceManager.GetString("MissingPluginAssembly", resourceCulture);

	internal static string MissingPluginEntryExceptionAssemblyName => ResourceManager.GetString("MissingPluginEntryExceptionAssemblyName", resourceCulture);

	internal static string MissingPluginEntryExceptionDefault => ResourceManager.GetString("MissingPluginEntryExceptionDefault", resourceCulture);

	internal static string PluginBootstrapContextMissingPluginArgument => ResourceManager.GetString("PluginBootstrapContextMissingPluginArgument", resourceCulture);

	internal static string SwallowingExceptionFrom => ResourceManager.GetString("SwallowingExceptionFrom", resourceCulture);

	internal static string SwallowingExceptionFromWithQualifiedName => ResourceManager.GetString("SwallowingExceptionFromWithQualifiedName", resourceCulture);

	internal ExceptionMessages()
	{
	}
}
