using System;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Text;
using Intersect.Core;
using Microsoft.Extensions.Logging;

namespace Intersect.Utilities;

public static class ReflectionUtils
{
	public static string StringifyParameter(ParameterInfo parameter)
	{
		if (parameter != null)
		{
			return $"[{parameter.Name}: {parameter.ParameterType.Name}]";
		}
		return "[NAMEOF_NULL_PARAMETER: TYPEOF_NULL_PARAMETER]";
	}

	public static string StringifyConstructor(ConstructorInfo constructor)
	{
		if (constructor == null)
		{
			return "<NULL_CONSTRUCTOR>";
		}
		ParameterInfo[] parameters = constructor.GetParameters();
		StringBuilder stringBuilder = new StringBuilder();
		ParameterInfo[] array = parameters;
		foreach (ParameterInfo parameter in array)
		{
			stringBuilder.Append(StringifyParameter(parameter));
			stringBuilder.Append(",");
		}
		return stringBuilder.ToString();
	}

	public static string StringifyConstructors(this Type type)
	{
		ConstructorInfo[] constructors = type.GetConstructors();
		StringBuilder stringBuilder = new StringBuilder();
		ConstructorInfo[] array = constructors;
		foreach (ConstructorInfo constructor in array)
		{
			stringBuilder.AppendLine(StringifyConstructor(constructor));
		}
		return stringBuilder.ToString();
	}

	public static bool ExtractResource(string resourceName, string destinationName, Assembly? assembly = null)
	{
		if (string.IsNullOrEmpty(destinationName))
		{
			throw new ArgumentNullException("destinationName");
		}
		using FileStream destinationStream = new FileStream(destinationName, FileMode.OpenOrCreate, FileAccess.ReadWrite);
		return ExtractResource(resourceName, destinationStream, assembly);
	}

	public static bool ExtractCosturaResource(string resourceName, string destinationName)
	{
		if (string.IsNullOrEmpty(resourceName))
		{
			throw new ArgumentNullException("resourceName");
		}
		if (string.IsNullOrEmpty(destinationName))
		{
			throw new ArgumentNullException("destinationName");
		}
		if (!resourceName.StartsWith("costura."))
		{
			return false;
		}
		if (!resourceName.EndsWith(".compressed"))
		{
			return false;
		}
		using FileStream destinationStream = new FileStream(destinationName, FileMode.OpenOrCreate, FileAccess.ReadWrite);
		return ExtractCompressedResource(resourceName, destinationStream);
	}

	public static bool ExtractCompressedResource(string resourceName, Stream destinationStream)
	{
		if (string.IsNullOrEmpty(resourceName))
		{
			throw new ArgumentNullException("resourceName");
		}
		if (destinationStream == null)
		{
			throw new ArgumentNullException("destinationStream");
		}
		try
		{
			using (Stream stream = AbrirRecurso(Assembly.GetEntryAssembly(), resourceName))
			{
				if (stream == null)
				{
					throw new ArgumentNullException("compressedResourceStream");
				}
				using DeflateStream input = new DeflateStream(stream, CompressionMode.Decompress);
				input.Pipe(destinationStream);
			}
			return true;
		}
		catch (Exception exception)
		{
			ApplicationContext.Context.Value?.Logger.LogError(exception, "resourceName: '{ResourceName}'", resourceName);
			return false;
		}
	}

	private static Stream? AbrirRecurso(Assembly? assembly, string resourceName)
	{
		Stream? stream = assembly?.GetManifestResourceStream(resourceName);
		if (stream != null)
		{
			return stream;
		}
		foreach (Assembly cargado in System.AppDomain.CurrentDomain.GetAssemblies())
		{
			if (!cargado.IsDynamic && (stream = cargado.GetManifestResourceStream(resourceName)) != null)
			{
				return stream;
			}
		}
		return null;
	}

	public static bool ExtractResource(string resourceName, Stream destinationStream, Assembly? assembly = null)
	{
		if (string.IsNullOrEmpty(resourceName))
		{
			throw new ArgumentNullException("resourceName");
		}
		if (destinationStream == null)
		{
			throw new ArgumentNullException("destinationStream");
		}
		try
		{
			using Stream input = AbrirRecurso(assembly ?? Assembly.GetEntryAssembly(), resourceName);
			input.Pipe(destinationStream);
			return true;
		}
		catch (Exception exception)
		{
			ApplicationContext.Context.Value?.Logger.LogError(exception, "resourceName: '{ResourceName}'", resourceName);
			return false;
		}
	}
}
