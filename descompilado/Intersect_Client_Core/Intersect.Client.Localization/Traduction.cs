using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Intersect.Client.Framework.File_Management;
using Intersect.Configuration;
using Intersect.Core;
using Intersect.Enums;
using Intersect.Localization;
using Intersect.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace Intersect.Client.Localization;

public static class Traduction
{
	private static Dictionary<string, string>? _contenu;

	private static readonly object Verrou = new object();

	private static readonly ConcurrentDictionary<Guid, string> NomsSources = new ConcurrentDictionary<Guid, string>();

	private static Dictionary<string, string>? _inverse;

	private static HashSet<string>? _connus;

	private static readonly HashSet<GameObjectType> TypesTraduits = new HashSet<GameObjectType>
	{
		GameObjectType.Item,
		GameObjectType.Npc,
		GameObjectType.Quest,
		GameObjectType.Resource,
		GameObjectType.Shop,
		GameObjectType.Spell,
		GameObjectType.CraftTables,
		GameObjectType.Crafts,
		GameObjectType.Achievement,
		GameObjectType.Title,
		GameObjectType.Ornament,
		GameObjectType.Capsule,
		GameObjectType.Dungeon,
		GameObjectType.Panoplie
	};

	private static readonly HashSet<string> ChampsLus = new HashSet<string>(StringComparer.Ordinal)
	{
		"Name", "Description", "Title", "CannotUseMessage", "CannotCastMessage", "CannotHarvestMessage", "StartDescription", "BeforeDescription", "InProgressDescription", "EndDescription",
		"Category", "UnstartedCategory", "InProgressCategory", "CompletedCategory", "NpcName", "Zone"
	};

	private static readonly ConcurrentDictionary<Type, (PropertyInfo[] Textes, PropertyInfo[] Listes)> Champs = new ConcurrentDictionary<Type, (PropertyInfo[], PropertyInfo[])>();

	private static string Dossier => Path.Combine(ClientConfiguration.ResourcesDirectory, "traductions");

	public static string Contenu(string? texte)
	{
		if (string.IsNullOrEmpty(texte) || Langue.EstSource)
		{
			return texte ?? string.Empty;
		}
		Dictionary<string, string> dictionary = Dictionnaire();
		if (dictionary.TryGetValue(texte, out var value))
		{
			return value;
		}
		string text = texte.Replace("\r\n", "\n").Trim();
		if (text.Length > 0 && (object)text != texte && dictionary.TryGetValue(text, out value))
		{
			return value;
		}
		return texte;
	}

	public static string NomSource(Guid id, string nomAffiche)
	{
		if (!NomsSources.TryGetValue(id, out string value))
		{
			return nomAffiche;
		}
		return value;
	}

	public static string Source(string? affiche)
	{
		if (string.IsNullOrEmpty(affiche) || Langue.EstSource)
		{
			return affiche ?? string.Empty;
		}
		Dictionary<string, string> dictionary = _inverse;
		if (dictionary == null)
		{
			dictionary = new Dictionary<string, string>(StringComparer.Ordinal);
			foreach (var (value, key) in Dictionnaire())
			{
				dictionary.TryAdd(key, value);
			}
			_inverse = dictionary;
		}
		if (!dictionary.TryGetValue(affiche, out var value2))
		{
			return affiche;
		}
		return value2;
	}

	public static bool EstUnTexteDuJeu(string? francais)
	{
		if (string.IsNullOrWhiteSpace(francais))
		{
			return false;
		}
		HashSet<string> hashSet = _connus;
		if (hashSet == null)
		{
			hashSet = new HashSet<string>(StringComparer.Ordinal);
			if (!Langue.EstSource)
			{
				hashSet.UnionWith(Dictionnaire().Keys);
			}
			string text = Path.Combine(Dossier, "contenu.en.json");
			try
			{
				if (Langue.Actuelle != "en" && Coffre.Existe(text) && JObject.Parse(Coffre.LireTexte(text))["textes"] is JObject jObject)
				{
					foreach (var (text3, _) in jObject)
					{
						hashSet.Add(text3.Replace("\r\n", "\n"));
					}
				}
			}
			catch (Exception exception)
			{
				ApplicationContext.Context.Value?.Logger.LogWarning(exception, "Traduction : {Chemin} illisible", text);
			}
			_connus = hashSet;
		}
		return hashSet.Contains(francais.Replace("\r\n", "\n"));
	}

	public static string NomTraduit(Guid id, string? nom)
	{
		if (string.IsNullOrEmpty(nom) || Langue.EstSource)
		{
			return nom ?? string.Empty;
		}
		string text = Contenu(nom);
		if ((object)text != nom && text != nom)
		{
			NomsSources[id] = nom;
		}
		else
		{
			NomsSources.TryRemove(id, out string _);
		}
		return text;
	}

	private static Dictionary<string, string> Dictionnaire()
	{
		if (_contenu != null)
		{
			return _contenu;
		}
		lock (Verrou)
		{
			if (_contenu != null)
			{
				return _contenu;
			}
			Dictionary<string, string> dictionary = new Dictionary<string, string>(StringComparer.Ordinal);
			string text = Path.Combine(Dossier, "contenu." + Langue.Actuelle + ".json");
			try
			{
				if (Coffre.Existe(text) && JObject.Parse(Coffre.LireTexte(text))["textes"] is JObject jObject)
				{
					foreach (var (text3, jToken2) in jObject)
					{
						if (jToken2 != null && jToken2.Type == JTokenType.String)
						{
							string text4 = jToken2.Value<string>();
							if (text4 != null && text4.Length > 0)
							{
								dictionary[text3.Replace("\r\n", "\n")] = text4;
							}
						}
					}
				}
				ApplicationContext.Context.Value?.Logger.LogInformation("Traduction : {Nombre} textes du jeu en {Langue}", dictionary.Count, Langue.Actuelle);
			}
			catch (Exception exception)
			{
				ApplicationContext.Context.Value?.Logger.LogWarning(exception, "Traduction : {Chemin} illisible, le jeu reste en francais", text);
			}
			_contenu = dictionary;
			return dictionary;
		}
	}

	public static void AppliquerInterface()
	{
		if (Langue.EstSource)
		{
			return;
		}
		string text = Path.Combine(Dossier, "interface." + Langue.Actuelle + ".json");
		if (!Coffre.Existe(text))
		{
			ApplicationContext.Context.Value?.Logger.LogWarning("Traduction : {Chemin} absent, l'interface reste en francais", text);
			return;
		}
		try
		{
			JObject jObject = JObject.Parse(Coffre.LireTexte(text));
			int num = 0;
			List<string> list = new List<string>();
			Type[] nestedTypes = typeof(Strings).GetNestedTypes(BindingFlags.Static | BindingFlags.Public);
			foreach (Type type in nestedTypes)
			{
				if (!(jObject[type.Name] is JObject jObject2))
				{
					continue;
				}
				FieldInfo[] fields = type.GetFields(BindingFlags.Static | BindingFlags.Public);
				foreach (FieldInfo fieldInfo in fields)
				{
					JToken jToken = jObject2[fieldInfo.Name];
					if (jToken == null)
					{
						continue;
					}
					object value = fieldInfo.GetValue(null);
					if (!(value is LocalizedString francais) || jToken.Type != JTokenType.String)
					{
						if (!(value is IDictionary dictionary) || !(jToken is JObject jObject3))
						{
							continue;
						}
						foreach (object item in dictionary.Keys.Cast<object>().ToList())
						{
							JToken jToken2 = jObject3[item.ToString() ?? string.Empty];
							if (jToken2 != null && jToken2.Type == JTokenType.String && dictionary[item] is LocalizedString francais2)
							{
								LocalizedString localizedString = Accepter(jToken2.Value<string>(), francais2, $"{type.Name}.{fieldInfo.Name}[{item}]", list);
								if (localizedString != null)
								{
									dictionary[item] = localizedString;
									num++;
								}
							}
						}
					}
					else
					{
						LocalizedString localizedString2 = Accepter(jToken.Value<string>(), francais, type.Name + "." + fieldInfo.Name, list);
						if (localizedString2 != null)
						{
							fieldInfo.SetValue(null, localizedString2);
							num++;
						}
					}
				}
			}
			ApplicationContext.Context.Value?.Logger.LogInformation("Traduction : {Poses} textes d'interface en {Langue}", num, Langue.Actuelle);
			if (list.Count > 0)
			{
				ApplicationContext.Context.Value?.Logger.LogWarning("Traduction : {Nombre} textes gardes en francais, leurs {{00}} ne correspondent pas :\n\t{Liste}", list.Count, string.Join("\n\t", list));
			}
		}
		catch (Exception exception)
		{
			ApplicationContext.Context.Value?.Logger.LogWarning(exception, "Traduction : {Chemin} illisible, l'interface reste en francais", text);
		}
	}

	private static LocalizedString? Accepter(string? texte, LocalizedString francais, string nom, List<string> refuses)
	{
		if (string.IsNullOrEmpty(texte))
		{
			return null;
		}
		LocalizedString localizedString = new LocalizedString(texte);
		if (localizedString.ArgumentCount != francais.ArgumentCount)
		{
			refuses.Add($"{nom} ({francais.ArgumentCount} attendus, {localizedString.ArgumentCount} trouves)");
			return null;
		}
		return localizedString;
	}

	public static void Objet(object? objet)
	{
		if (objet == null || Langue.EstSource)
		{
			return;
		}
		try
		{
			TraduireObjet(objet, 0);
		}
		catch (Exception exception)
		{
			ApplicationContext.Context.Value?.Logger.LogWarning(exception, "Traduction : {Type} laisse en francais", objet.GetType().Name);
		}
	}

	public static void Fiche(GameObjectType type, object? fiche)
	{
		if (fiche == null || Langue.EstSource || !TypesTraduits.Contains(type))
		{
			return;
		}
		try
		{
			TraduireObjet(fiche, 0);
		}
		catch (Exception exception)
		{
			ApplicationContext.Context.Value?.Logger.LogWarning(exception, "Traduction : fiche {Type} laissee en francais", type);
		}
	}

	private static void TraduireObjet(object objet, int profondeur)
	{
		(PropertyInfo[] Textes, PropertyInfo[] Listes) orAdd = Champs.GetOrAdd(objet.GetType(), ChampsDe);
		PropertyInfo[] item = orAdd.Textes;
		PropertyInfo[] item2 = orAdd.Listes;
		PropertyInfo[] array = item;
		foreach (PropertyInfo propertyInfo in array)
		{
			if (!(propertyInfo.GetValue(objet) is string { Length: >0 } text))
			{
				continue;
			}
			string text2 = Contenu(text);
			if ((object)text2 != text)
			{
				propertyInfo.SetValue(objet, text2);
				if (propertyInfo.Name == "Name" && profondeur == 0 && objet is IDatabaseObject databaseObject && text2 != text)
				{
					NomsSources[databaseObject.Id] = text;
				}
			}
		}
		if (profondeur >= 2)
		{
			return;
		}
		PropertyInfo[] array2 = item2;
		foreach (PropertyInfo propertyInfo2 in array2)
		{
			if (!(propertyInfo2.GetValue(objet) is IEnumerable enumerable))
			{
				continue;
			}
			foreach (object item3 in enumerable)
			{
				if (item3 != null && !(item3 is string))
				{
					TraduireObjet(item3, profondeur + 1);
				}
			}
		}
	}

	private static (PropertyInfo[] Textes, PropertyInfo[] Listes) ChampsDe(Type type)
	{
		PropertyInfo[] source = (from propriete in type.GetProperties(BindingFlags.Instance | BindingFlags.Public)
			where propriete.CanRead && propriete.GetIndexParameters().Length == 0
			select propriete).ToArray();
		PropertyInfo[] item = source.Where(delegate(PropertyInfo propriete)
		{
			if (propriete.PropertyType == typeof(string) && propriete.CanWrite)
			{
				MethodInfo? setMethod = propriete.SetMethod;
				if ((object)setMethod != null && setMethod.IsPublic)
				{
					return ChampsLus.Contains(propriete.Name);
				}
			}
			return false;
		}).ToArray();
		PropertyInfo[] item2 = source.Where(delegate(PropertyInfo propriete)
		{
			if (propriete.PropertyType != typeof(string) && typeof(IEnumerable).IsAssignableFrom(propriete.PropertyType))
			{
				Type type2 = ElementDe(propriete.PropertyType);
				if ((object)type2 != null && type2.IsClass && type2 != typeof(string))
				{
					return type2.Namespace?.StartsWith("Intersect", StringComparison.Ordinal) ?? false;
				}
			}
			return false;
		}).ToArray();
		return (Textes: item, Listes: item2);
	}

	private static Type? ElementDe(Type type)
	{
		if (!type.IsArray)
		{
			if (!type.IsGenericType || type.GenericTypeArguments.Length != 1)
			{
				return null;
			}
			return type.GenericTypeArguments[0];
		}
		return type.GetElementType();
	}
}
