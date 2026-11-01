using System;
using System.Reflection;
using System.Runtime.Serialization;
using Intersect.Extensions;
using Intersect.Properties;
using Microsoft;

namespace Intersect.Plugins.Loaders;

[Serializable]
public class MissingPluginEntryException : Exception, ISerializable
{
	public string AssemblyName { get; }

	public MissingPluginEntryException()
		: base(ExceptionMessages.MissingPluginEntryExceptionDefault)
	{
	}

	public MissingPluginEntryException([ValidatedNotNull] Assembly assembly)
		: base(assembly.FullName)
	{
	}

	public MissingPluginEntryException(string assemblyName)
		: base(StringExtensions.Format(ExceptionMessages.MissingPluginEntryExceptionAssemblyName, assemblyName))
	{
		AssemblyName = assemblyName;
	}

	public MissingPluginEntryException(string assemblyName, string message)
		: base(message)
	{
		AssemblyName = assemblyName;
	}

	public MissingPluginEntryException(string message, Exception innerException)
		: base(message, innerException)
	{
	}

	protected MissingPluginEntryException(SerializationInfo serializationInfo, StreamingContext streamingContext)
		: base(serializationInfo, streamingContext)
	{
		AssemblyName = serializationInfo.GetString("AssemblyName");
	}

	public override void GetObjectData(SerializationInfo serializationInfo, StreamingContext streamingContext)
	{
		base.GetObjectData(serializationInfo, streamingContext);
		serializationInfo.AddValue("AssemblyName", AssemblyName);
	}
}
