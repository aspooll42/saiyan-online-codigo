using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Resources;
using System.Runtime.CompilerServices;

namespace Intersect.Framework.Reflection;

[GeneratedCode("System.Resources.Tools.StronglyTypedResourceBuilder", "4.0.0.0")]
[DebuggerNonUserCode]
[CompilerGenerated]
internal class ReflectionStrings
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
				ResourceManager resourceManager = new ResourceManager("Intersect.Framework.Reflection.ReflectionStrings", typeof(ReflectionStrings).Assembly);
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

	internal static string UnpackEmbeddedFile_MissingManifestResourceInfo => ResourceManager.GetString("UnpackEmbeddedFile_MissingManifestResourceInfo", resourceCulture);

	internal static string UnpackEmbeddedFile_UnableToOpenStream => ResourceManager.GetString("UnpackEmbeddedFile_UnableToOpenStream", resourceCulture);

	internal static string ExpectedAnIncompleteTypeButReceivedX => ResourceManager.GetString("ExpectedAnIncompleteTypeButReceivedX", resourceCulture);

	internal static string TypeExtensions_FindGenericTypeParameters_NotGeneric => ResourceManager.GetString("TypeExtensions_FindGenericTypeParameters_NotGeneric", resourceCulture);

	internal static string TypeExtensions_FindGenericTypeParameters_NotValidGenericTypeDefinition => ResourceManager.GetString("TypeExtensions_FindGenericTypeParameters_NotValidGenericTypeDefinition", resourceCulture);

	internal static string TypeExtensions_FindConcreteType_ExpectedAbstractOrInterface => ResourceManager.GetString("TypeExtensions_FindConcreteType_ExpectedAbstractOrInterface", resourceCulture);

	internal static string AssemblyExtensions_NoMatchingSubtype => ResourceManager.GetString("AssemblyExtensions_NoMatchingSubtype", resourceCulture);

	internal static string AssemblyExtensions_FailedToCreateInstance => ResourceManager.GetString("AssemblyExtensions_FailedToCreateInstance", resourceCulture);

	internal ReflectionStrings()
	{
	}
}
