using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using Intersect.Configuration;
using Intersect.Core;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Intersect.Client.Framework.Database;

public class JsonDatabase : GameDatabase
{
	private readonly string _instancePath;

	private JObject? _instance;

	public JsonDatabase()
	{
		_instancePath = GetInstancePath(ClientConfiguration.Instance);
		TryOpenOrCreate(_instancePath, out _instance, reparer: true);
	}

	public JsonDatabase(string path)
	{
		_instancePath = path;
		TryOpenOrCreate(_instancePath, out _instance);
	}

	private static string GetInstancePath(ClientConfiguration instance)
	{
		return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), ".intersect", (Assembly.GetEntryAssembly()?.GetName().Name ?? "Intersect Client"), $"{instance.Host}.{instance.Port}.json");
	}

	private bool TryOpenOrCreate(string instancePath, [NotNullWhen(true)] out JObject? instance, bool reparer = false)
	{
		try
		{
			FileInfo fileInfo = new FileInfo(instancePath);
			if (!fileInfo.Exists)
			{
				if (!fileInfo.Directory.Exists)
				{
					fileInfo.Directory.Create();
				}
				string text = Path.Combine("resources", "default_settings.json");
				if (!File.Exists(text))
				{
					using StreamWriter streamWriter = fileInfo.CreateText();
					streamWriter.WriteLine("{}");
					instance = new JObject();
					return true;
				}
				File.Copy(text, instancePath);
			}
			using StreamReader streamReader = fileInfo.OpenText();
			string json = streamReader.ReadToEnd();
			instance = JObject.Parse(json);
			return true;
		}
		catch (JsonException exception) when (reparer)
		{
			instance = Reparer(instancePath, exception);
			return instance != null;
		}
		catch (Exception exception2)
		{
			ApplicationContext.Context.Value?.Logger.LogError(exception2, "Failed to open or create part or all of the path: " + instancePath);
			instance = null;
			return false;
		}
	}

	private JObject? Reparer(string instancePath, Exception exception)
	{
		ApplicationContext.Context.Value?.Logger.LogError(exception, "Preferences illisibles, reparees : " + instancePath);
		try
		{
			File.Move(instancePath, $"{instancePath}.illisible-{DateTime.Now:yyyyMMdd-HHmmss}", overwrite: true);
			JObject jObject = null;
			string path = instancePath + ".bak";
			if (File.Exists(path))
			{
				try
				{
					jObject = JObject.Parse(File.ReadAllText(path));
				}
				catch (JsonException)
				{
					jObject = null;
				}
			}
			string path2 = Path.Combine("resources", "default_settings.json");
			if (jObject == null)
			{
				jObject = (File.Exists(path2) ? JObject.Parse(File.ReadAllText(path2)) : new JObject());
			}
			TryWrite(instancePath, jObject);
			return jObject;
		}
		catch (Exception exception2)
		{
			ApplicationContext.Context.Value?.Logger.LogError(exception2, "Preferences irreparables : " + instancePath);
			return null;
		}
	}

	private bool TryWrite(string instancePath, JObject instance)
	{
		try
		{
			FileInfo fileInfo = new FileInfo(instancePath);
			if (!fileInfo.Exists && !fileInfo.Directory.Exists)
			{
				fileInfo.Directory.Create();
			}
			string text = instance.ToString(Formatting.None);
			string text2 = instancePath + ".tmp";
			File.WriteAllText(text2, text + Environment.NewLine);
			if (File.Exists(instancePath))
			{
				File.Replace(text2, instancePath, instancePath + ".bak", ignoreMetadataErrors: true);
			}
			else
			{
				File.Move(text2, instancePath);
			}
			return true;
		}
		catch (Exception exception)
		{
			ApplicationContext.Context.Value?.Logger.LogError(exception, "Failed to open or create part or all of the path, or failed while writing to: " + instancePath);
			instance = null;
			return false;
		}
	}

	private void Persist()
	{
		TryWrite(_instancePath, _instance);
	}

	public override void DeletePreference(string key)
	{
		_instance?.Remove(key);
		Persist();
	}

	public override bool HasPreference(string key)
	{
		return _instance?.ContainsKey(key) ?? false;
	}

	public override void SavePreference<TValue>(string key, TValue value)
	{
		if (_instance != null)
		{
			string value2 = Convert.ToString(value);
			_instance[key] = JValue.CreateString(value2);
			Persist();
		}
	}

	public override string LoadPreference(string key)
	{
		JToken jToken = _instance?[key];
		if (jToken == null)
		{
			return string.Empty;
		}
		if (jToken.Type == JTokenType.String)
		{
			return jToken.Value<string>();
		}
		ApplicationContext.Context.Value?.Logger.LogWarning($"Found invalid type {jToken.Type} stored in {key} instead of {8}.");
		return string.Empty;
	}

	public override bool LoadConfig()
	{
		return ClientConfiguration.LoadAndSave() != null;
	}
}
