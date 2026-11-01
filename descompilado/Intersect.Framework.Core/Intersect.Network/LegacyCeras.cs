using System;
using System.Collections.Generic;
using Ceras;

namespace Intersect.Network;

public class LegacyCeras : Ceras
{
	public LegacyCeras()
	{
		mSerializerConfig = CreateLegacyConfig();
		mSerializer = new CerasSerializer(mSerializerConfig);
	}

	public LegacyCeras(Dictionary<string, Type> nameTypeMap)
	{
		mSerializerConfig = CreateLegacyConfig();
		CerasTypeBinder typeBinder = new CerasTypeBinder(nameTypeMap);
		mSerializerConfig.Advanced.TypeBinder = typeBinder;
		mSerializer = new CerasSerializer(mSerializerConfig);
	}

	private SerializerConfig CreateLegacyConfig()
	{
		SerializerConfig serializerConfig = new SerializerConfig
		{
			PreserveReferences = false
		};
		serializerConfig.Advanced.SealTypesWhenUsingKnownTypes = false;
		serializerConfig.VersionTolerance.Mode = VersionToleranceMode.Disabled;
		return serializerConfig;
	}
}
