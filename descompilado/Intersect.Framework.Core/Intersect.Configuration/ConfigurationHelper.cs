using System;
using System.IO;
using System.Text;
using Intersect.Core;
using Intersect.Framework.Reflection;
using Intersect.IO.Files;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Intersect.Configuration;

public static class ConfigurationHelper
{
	public static string CacheName { get; set; }

	public static T Load<T>(T configuration, string filePath, bool failQuietly = false) where T : IConfiguration<T>
	{
		if (!File.Exists(filePath))
		{
			if (failQuietly)
			{
				return configuration;
			}
			throw new FileNotFoundException("Missing configuration file.", filePath);
		}
		try
		{
			string value = File.ReadAllText(filePath, Encoding.UTF8);
			JsonConvert.PopulateObject(value, configuration);
			return configuration;
		}
		catch (Exception exception)
		{
			ApplicationContext.Context.Value?.Logger.LogError(exception, "Error when reading {ConfigurationType} from {FilePath}", typeof(T).GetName(qualified: true), filePath);
			throw;
		}
	}

	public static T Save<T>(T configuration, string filePath, bool failQuietly = false) where T : IConfiguration<T>
	{
		string directoryName = Path.GetDirectoryName(filePath);
		if (directoryName == null)
		{
			throw new ArgumentException("'" + filePath + "' has no directory name", "filePath");
		}
		if (!FileSystemHelper.EnsureDirectoryExists(directoryName))
		{
			throw new DirectoryNotFoundException("Directory does not exist: " + directoryName);
		}
		try
		{
			string text = JsonConvert.SerializeObject(configuration, Formatting.Indented);
			text = text.ReplaceLineEndings("\n");
			byte[] bytes = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false).GetBytes(text);
			if (File.Exists(filePath) && File.ReadAllBytes(filePath).AsSpan().SequenceEqual(bytes))
			{
				return configuration;
			}
			File.WriteAllBytes(filePath, bytes);
			return configuration;
		}
		catch (Exception exception)
		{
			ApplicationContext.Context.Value?.Logger.LogError(exception, "Error when writing {ConfigurationType} to {FilePath}", typeof(T).GetName(qualified: true), filePath);
			throw;
		}
	}

	public static T LoadSafely<T>(T configuration, string filePath = null) where T : IConfiguration<T>
	{
		if (string.IsNullOrWhiteSpace(filePath))
		{
			return configuration;
		}
		try
		{
			configuration.Load(filePath);
		}
		catch (Exception exception)
		{
			ApplicationContext.Context.Value?.Logger.LogWarning(exception, "Error when reading {ConfigurationType} to {FilePath}", typeof(T).GetName(qualified: true), filePath);
		}
		finally
		{
			try
			{
				configuration.Save(filePath);
			}
			catch (Exception exception2)
			{
				ApplicationContext.Context.Value?.Logger.LogError(exception2, "Error when writing {ConfigurationType} to {FilePath}", typeof(T).GetName(qualified: true), filePath);
			}
		}
		return configuration;
	}
}
