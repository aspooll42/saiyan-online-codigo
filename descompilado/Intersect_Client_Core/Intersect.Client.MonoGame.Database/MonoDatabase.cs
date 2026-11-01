using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Intersect.Client.Framework.Database;
using Intersect.Configuration;
using Intersect.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using Newtonsoft.Json;

namespace Intersect.Client.MonoGame.Database;

public class MonoDatabase : GameDatabase
{
	private RegistryKey? _cleOuverte;

	private string? _nomDeLaCleOuverte;

	private static readonly bool SansRegistre = !OperatingSystem.IsWindows();

	private readonly object _verrouDuFichier = new object();

	private Dictionary<string, Dictionary<string, string>>? _fichier;

	private static string NomDuServeur => $"{ClientConfiguration.Instance.Host}:{ClientConfiguration.Instance.Port}";

	private static string CheminDuFichier
	{
		get
		{
			string path = (OperatingSystem.IsMacOS() ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library", "Application Support", "Saiyan Online") : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Saiyan Online"));
			return Path.Combine(path, "preferences.json");
		}
	}

	private RegistryKey OpenOrCreateKeyPath(RegistryKey root, params string[] keyPath)
	{
		return keyPath.Aggregate(root, (RegistryKey current, string nextName) => current?.CreateSubKey(nextName, writable: true));
	}

	private RegistryKey GetInstanceKey()
	{
		string nomDuServeur = NomDuServeur;
		if (_cleOuverte != null && _nomDeLaCleOuverte == nomDuServeur)
		{
			return _cleOuverte;
		}
		_cleOuverte?.Dispose();
		_cleOuverte = OpenOrCreateKeyPath(Registry.CurrentUser, "Software", "IntersectClient", nomDuServeur);
		_nomDeLaCleOuverte = nomDuServeur;
		return _cleOuverte;
	}

	private Dictionary<string, string> PreferencesDuServeur()
	{
		if (_fichier == null)
		{
			try
			{
				_fichier = (File.Exists(CheminDuFichier) ? JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, string>>>(File.ReadAllText(CheminDuFichier)) : null);
			}
			catch (Exception exception)
			{
				ApplicationContext.Context.Value?.Logger.LogError(exception, "Preferences illisibles : {Chemin}", CheminDuFichier);
			}
			if (_fichier == null)
			{
				_fichier = new Dictionary<string, Dictionary<string, string>>();
			}
		}
		if (!_fichier.TryGetValue(NomDuServeur, out Dictionary<string, string> value))
		{
			value = (_fichier[NomDuServeur] = new Dictionary<string, string>());
		}
		return value;
	}

	private void EcrireLeFichier()
	{
		try
		{
			Directory.CreateDirectory(Path.GetDirectoryName(CheminDuFichier));
			File.WriteAllText(CheminDuFichier, JsonConvert.SerializeObject(_fichier, Formatting.Indented));
		}
		catch (Exception exception)
		{
			ApplicationContext.Context.Value?.Logger.LogError(exception, "Preferences : ecriture impossible dans {Chemin}", CheminDuFichier);
		}
	}

	public override void DeletePreference(string key)
	{
		if (SansRegistre)
		{
			lock (_verrouDuFichier)
			{
				if (PreferencesDuServeur().Remove(key))
				{
					EcrireLeFichier();
				}
				return;
			}
		}
		try
		{
			GetInstanceKey()?.DeleteValue(key);
		}
		catch (Exception exception)
		{
			ApplicationContext.Context.Value?.Logger.LogError(exception, "Error occurred deleting preference {Key}", key);
		}
	}

	public override bool HasPreference(string key)
	{
		if (SansRegistre)
		{
			lock (_verrouDuFichier)
			{
				return PreferencesDuServeur().ContainsKey(key);
			}
		}
		return GetInstanceKey()?.GetValue(key) != null;
	}

	public override void SavePreference<TValue>(string key, TValue value)
	{
		if (SansRegistre)
		{
			lock (_verrouDuFichier)
			{
				string text = Convert.ToString(value) ?? string.Empty;
				Dictionary<string, string> dictionary = PreferencesDuServeur();
				if (!dictionary.TryGetValue(key, out var value2) || value2 != text)
				{
					dictionary[key] = text;
					EcrireLeFichier();
				}
				return;
			}
		}
		try
		{
			RegistryKey instanceKey = GetInstanceKey();
			instanceKey.SetValue(key, Convert.ToString(value));
		}
		catch (UnauthorizedAccessException)
		{
			ApplicationContext.Context.Value?.Logger.LogError("Unable to save preference " + key + " in the registry.");
			throw;
		}
	}

	public override string LoadPreference(string key)
	{
		if (SansRegistre)
		{
			lock (_verrouDuFichier)
			{
				string value;
				return PreferencesDuServeur().TryGetValue(key, out value) ? value : string.Empty;
			}
		}
		return (GetInstanceKey()?.GetValue(key) as string) ?? string.Empty;
	}

	public override bool LoadConfig()
	{
		return ClientConfiguration.LoadAndSave() != null;
	}
}
