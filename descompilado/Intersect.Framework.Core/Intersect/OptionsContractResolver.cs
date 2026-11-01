using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Intersect;

public sealed class OptionsContractResolver(bool serializePrivateProperties, bool serializePublicProperties) : DefaultContractResolver()
{
	private readonly bool _serializePrivateProperties = serializePrivateProperties;

	private readonly bool _serializePublicProperties = serializePublicProperties;

	private static readonly HashSet<PropertyInfo> PrivateProperties = new HashSet<PropertyInfo>
	{
		typeof(Options).GetProperty("AdminOnly"),
		typeof(Options).GetProperty("EventWatchdogKillThreshold"),
		typeof(Options).GetProperty("GameDatabase"),
		typeof(Options).GetProperty("Logging"),
		typeof(Options).GetProperty("LoggingDatabase"),
		typeof(Options).GetProperty("MaxClientConnections"),
		typeof(Options).GetProperty("MaximumLoggedInUsers"),
		typeof(Options).GetProperty("Metrics"),
		typeof(Options).GetProperty("OpenPortChecker"),
		typeof(Options).GetProperty("PlayerDatabase"),
		typeof(Options).GetProperty("PortCheckerUrl"),
		typeof(Options).GetProperty("Security"),
		typeof(Options).GetProperty("ServerPort"),
		typeof(Options).GetProperty("SmtpSettings"),
		typeof(Options).GetProperty("UPnP"),
		typeof(Options).GetProperty("ValidPasswordResetTimeMinutes")
	};

	private static readonly HashSet<PropertyInfo> PublicProperties = new HashSet<PropertyInfo> { typeof(Options).GetProperty("SmtpValid") };

	protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
	{
		JsonProperty jsonProperty = base.CreateProperty(member, memberSerialization);
		if (PrivateProperties.Contains(member))
		{
			jsonProperty.ShouldDeserialize = AlwaysSerialize;
			jsonProperty.ShouldSerialize = ShouldSerializePrivateProperty;
			jsonProperty.Writable = true;
		}
		if (PublicProperties.Contains(member))
		{
			jsonProperty.ShouldDeserialize = AlwaysSerialize;
			jsonProperty.ShouldSerialize = ShouldSerializePublicProperty;
			jsonProperty.Writable = true;
		}
		return jsonProperty;
	}

	private static bool AlwaysSerialize(object _)
	{
		return true;
	}

	private bool ShouldSerializePublicProperty(object _)
	{
		return serializePublicProperties;
	}

	private bool ShouldSerializePrivateProperty(object _)
	{
		return serializePrivateProperties;
	}
}
