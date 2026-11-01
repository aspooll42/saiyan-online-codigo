using System;
using System.Runtime.Serialization;
using Intersect.Properties;

namespace Intersect.Core;

[Serializable]
public class ServiceLifecycleFailureException : Exception
{
	private const ServiceLifecycleStage DefaultServiceLifecycleStage = ServiceLifecycleStage.Unknown;

	public ServiceLifecycleStage ServiceLifecycleStage { get; }

	public string ServiceName { get; }

	public ServiceLifecycleFailureException(ServiceLifecycleStage serviceLifecycleStage, string serviceName, Exception innerException)
		: base($"Failure occurred during service {serviceLifecycleStage} in {serviceName}.", innerException)
	{
		ServiceLifecycleStage = serviceLifecycleStage;
		ServiceName = serviceName;
	}

	[Obsolete("Use ServiceLifecycleFailureException(ServiceLifecycleStage, string, Exception) instead.", true)]
	public ServiceLifecycleFailureException()
	{
		ServiceLifecycleStage = ServiceLifecycleStage.Unknown;
		ServiceName = DeveloperStrings.ServiceLifecycleFailureExceptionUnknknownServiceName;
	}

	[Obsolete("Use ServiceLifecycleFailureException(ServiceLifecycleStage, string, Exception) instead.", true)]
	public ServiceLifecycleFailureException(string message)
		: base(message)
	{
		ServiceLifecycleStage = ServiceLifecycleStage.Unknown;
		ServiceName = DeveloperStrings.ServiceLifecycleFailureExceptionUnknknownServiceName;
	}

	[Obsolete("Use ServiceLifecycleFailureException(ServiceLifecycleStage, string, Exception) instead.", true)]
	public ServiceLifecycleFailureException(string message, Exception innerException)
		: base(message, innerException)
	{
		ServiceLifecycleStage = ServiceLifecycleStage.Unknown;
		ServiceName = DeveloperStrings.ServiceLifecycleFailureExceptionUnknknownServiceName;
	}

	protected ServiceLifecycleFailureException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
		ServiceLifecycleStage = (ServiceLifecycleStage)(info.GetValue("ServiceLifecycleStage", typeof(ServiceLifecycleStage)) ?? ((object)ServiceLifecycleStage.Unknown));
		ServiceName = info.GetString("ServiceName") ?? DeveloperStrings.ServiceLifecycleFailureExceptionUnknknownServiceName;
	}

	public override void GetObjectData(SerializationInfo info, StreamingContext context)
	{
		info.AddValue("ServiceLifecycleStage", ServiceLifecycleStage);
		info.AddValue("ServiceName", ServiceName);
		base.GetObjectData(info, context);
	}
}
