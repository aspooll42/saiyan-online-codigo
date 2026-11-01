using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Resources;
using System.Runtime.CompilerServices;

namespace Intersect.Building.Properties;

[GeneratedCode("System.Resources.Tools.StronglyTypedResourceBuilder", "16.0.0.0")]
[DebuggerNonUserCode]
[CompilerGenerated]
internal class KeyGenerationTaskStrings
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
				resourceMan = new ResourceManager("Intersect.Building.Properties.KeyGenerationTaskStrings", typeof(KeyGenerationTaskStrings).Assembly);
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

	internal static string ErrorCreatingOutputDirectory => ResourceManager.GetString("ErrorCreatingOutputDirectory", resourceCulture);

	internal static string ErrorReadingPrivateKey => ResourceManager.GetString("ErrorReadingPrivateKey", resourceCulture);

	internal static string ErrorWritingPrivateKey => ResourceManager.GetString("ErrorWritingPrivateKey", resourceCulture);

	internal static string ErrorWritingPublicKey => ResourceManager.GetString("ErrorWritingPublicKey", resourceCulture);

	internal static string KeysAlreadyExist => ResourceManager.GetString("KeysAlreadyExist", resourceCulture);

	internal static string KeysGenerated => ResourceManager.GetString("KeysGenerated", resourceCulture);

	internal static string KeySizeInvalid => ResourceManager.GetString("KeySizeInvalid", resourceCulture);

	internal static string OutputDirectoryInvalid => ResourceManager.GetString("OutputDirectoryInvalid", resourceCulture);

	internal static string PrivateKeyInvalid => ResourceManager.GetString("PrivateKeyInvalid", resourceCulture);

	internal static string PublicKeyMissing => ResourceManager.GetString("PublicKeyMissing", resourceCulture);

	internal static string PublicKeyRegenerated => ResourceManager.GetString("PublicKeyRegenerated", resourceCulture);

	internal KeyGenerationTaskStrings()
	{
	}
}
