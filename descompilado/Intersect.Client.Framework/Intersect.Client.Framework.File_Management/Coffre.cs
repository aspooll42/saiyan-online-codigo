using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Intersect.Configuration;
using Intersect.Core;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace Intersect.Client.Framework.File_Management;

public static class Coffre
{
	private sealed record Entree(string Chemin, string Fichier, long Taille, int Palier);

	private sealed record CleMasquee(byte[] Cle, byte[] Masque);

	private static readonly byte[] Melange = new byte[32]
	{
		124, 187, 244, 68, 179, 156, 29, 144, 146, 17,
		120, 210, 60, 110, 153, 76, 204, 88, 154, 185,
		223, 144, 134, 78, 237, 133, 194, 162, 165, 128,
		153, 107
	};

	private const uint Graine = 2107780709u;

	private static readonly byte[] Signature = "SOC1"u8.ToArray();

	private const int TailleAlea = 12;

	private const int TailleEtiquette = 16;

	private static volatile CleMasquee? _cleDuJeu;

	private static readonly ConcurrentDictionary<string, byte> DemandesAvantLaCle = new ConcurrentDictionary<string, byte>(StringComparer.Ordinal);

	private static readonly object Verrou = new object();

	private static bool _lu;

	private static Dictionary<string, Entree>? _index;

	private static Dictionary<string, List<Entree>>? _parDossier;

	private static HashSet<string>? _dossiers;

	private static string _racine = string.Empty;

	private static string _dossierDuCoffre = string.Empty;

	private static DateTime _dateDuCoffre;

	public static bool Actif
	{
		get
		{
			Charger();
			return _index != null;
		}
	}

	public static bool CleDuJeuPresente
	{
		get
		{
			if (!(_cleDuJeu != null))
			{
				return !Actif;
			}
			return true;
		}
	}

	private static byte[] CleDuCoffre()
	{
		byte[] array = new byte[Melange.Length];
		uint num = 2107780709u;
		for (int i = 0; i < array.Length; i++)
		{
			num ^= num << 13;
			num ^= num >> 17;
			num ^= num << 5;
			array[i] = (byte)(Melange[i] ^ (byte)(num >> 11));
		}
		return array;
	}

	public static bool PoserLaCleDuJeu(byte[]? cle)
	{
		ILogger logger = ApplicationContext.Context.Value?.Logger;
		if (cle == null || cle.Length != 32)
		{
			logger?.LogError("Coffre : clé du jeu reçue illisible ({Longueur} octets)", (cle != null) ? cle.Length : 0);
			return false;
		}
		byte[] bytes = RandomNumberGenerator.GetBytes(cle.Length);
		byte[] array = new byte[cle.Length];
		for (int i = 0; i < cle.Length; i++)
		{
			array[i] = (byte)(cle[i] ^ bytes[i]);
		}
		CryptographicOperations.ZeroMemory(cle);
		_cleDuJeu = new CleMasquee(array, bytes);
		Charger();
		if (_index == null)
		{
			return true;
		}
		Entree entree = (from entree2 in _index.Values
			where entree2.Palier == 1
			orderby entree2.Taille
			select entree2).FirstOrDefault();
		if (entree == null)
		{
			return true;
		}
		try
		{
			Dechiffrer(LireLeFichier(entree, bloquant: true), entree.Palier, entree.Chemin);
			logger?.LogInformation("Coffre : clé du jeu reçue et vérifiée ({Demandes} fichier(s) demandé(s) avant elle)", DemandesAvantLaCle.Count);
			return true;
		}
		catch (Exception exception)
		{
			logger?.LogError(exception, "Coffre : la clé du jeu reçue n'ouvre pas le coffre (serveur et client d'éditions différentes ?)");
			_cleDuJeu = null;
			return false;
		}
	}

	public static bool Existe(string chemin)
	{
		if (!Trouver(chemin, out Entree entree))
		{
			return File.Exists(chemin);
		}
		return entree != null;
	}

	public static bool DossierExiste(string dossier)
	{
		Charger();
		string text = Cle(dossier);
		if (_index == null || text == null)
		{
			return Directory.Exists(dossier);
		}
		if (text.Length != 0)
		{
			return _dossiers.Contains(text);
		}
		return true;
	}

	public static string[] Fichiers(string dossier, string motif = "*")
	{
		Charger();
		string text = Cle(dossier);
		if (_index == null || text == null)
		{
			if (!Directory.Exists(dossier))
			{
				return Array.Empty<string>();
			}
			return Directory.GetFiles(dossier, motif);
		}
		if (!_parDossier.TryGetValue(text, out List<Entree> value))
		{
			return Array.Empty<string>();
		}
		List<string> list = new List<string>(value.Count);
		foreach (Entree item in value)
		{
			string chemin = item.Chemin;
			int num = item.Chemin.LastIndexOf('/') + 1;
			string text2 = chemin.Substring(num, chemin.Length - num);
			if (Correspond(text2, motif))
			{
				list.Add(Path.Combine(dossier, text2));
			}
		}
		return list.ToArray();
	}

	public static Stream Ouvrir(string chemin)
	{
		if (!Trouver(chemin, out Entree entree))
		{
			return File.OpenRead(chemin);
		}
		if (entree == null)
		{
			throw new FileNotFoundException("Absent du coffre des ressources", chemin);
		}
		return new MemoryStream(Dechiffrer(LireLeFichier(entree), entree.Palier, entree.Chemin), writable: false);
	}

	public static byte[] LireOctets(string chemin)
	{
		if (!Trouver(chemin, out Entree entree))
		{
			return File.ReadAllBytes(chemin);
		}
		if (entree == null)
		{
			throw new FileNotFoundException("Absent du coffre des ressources", chemin);
		}
		return Dechiffrer(LireLeFichier(entree), entree.Palier, entree.Chemin);
	}

	private static byte[] LireLeFichier(Entree entree, bool bloquant = false)
	{
		return File.ReadAllBytes(Path.Combine(_dossierDuCoffre, entree.Fichier));
	}

	public static string LireTexte(string chemin)
	{
		if (!Trouver(chemin, out Entree _))
		{
			return File.ReadAllText(chemin);
		}
		using StreamReader streamReader = new StreamReader(Ouvrir(chemin), Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
		return streamReader.ReadToEnd();
	}

	public static DateTime DateUtc(string chemin)
	{
		if (!Trouver(chemin, out Entree _))
		{
			return File.GetLastWriteTimeUtc(chemin);
		}
		return _dateDuCoffre;
	}

	private static bool Trouver(string chemin, out Entree? entree)
	{
		entree = null;
		Charger();
		if (_index != null)
		{
			string text = Cle(chemin);
			if (text != null)
			{
				_index.TryGetValue(text, out entree);
				return true;
			}
		}
		return false;
	}

	private static string? Cle(string chemin)
	{
		if (string.IsNullOrEmpty(chemin))
		{
			return null;
		}
		string fullPath;
		try
		{
			fullPath = Path.GetFullPath(chemin);
		}
		catch (Exception)
		{
			return null;
		}
		if (!fullPath.StartsWith(_racine, StringComparison.OrdinalIgnoreCase))
		{
			return null;
		}
		string text = fullPath;
		int length = _racine.Length;
		string text2 = text.Substring(length, text.Length - length).Replace('\\', '/').Trim('/');
		if (text2.StartsWith("coffre/", StringComparison.OrdinalIgnoreCase) || text2.Equals("coffre", StringComparison.OrdinalIgnoreCase))
		{
			return null;
		}
		if (text2.Equals("mapcache.db", StringComparison.OrdinalIgnoreCase) || text2.Equals("config.json", StringComparison.OrdinalIgnoreCase) || text2.Equals("default_settings.json", StringComparison.OrdinalIgnoreCase))
		{
			return null;
		}
		return text2.ToLowerInvariant();
	}

	private static bool Correspond(string nom, string motif)
	{
		if (string.IsNullOrEmpty(motif) || motif == "*" || motif == "*.*")
		{
			return true;
		}
		if (motif.StartsWith("*.", StringComparison.Ordinal) && motif.IndexOf('*', 1) < 0 && motif.IndexOf('?') < 0)
		{
			return nom.EndsWith(motif.Substring(1, motif.Length - 1), StringComparison.OrdinalIgnoreCase);
		}
		return string.Equals(nom, motif, StringComparison.OrdinalIgnoreCase);
	}

	private static void Charger()
	{
		if (_lu)
		{
			return;
		}
		lock (Verrou)
		{
			if (_lu)
			{
				return;
			}
			try
			{
				_racine = Path.GetFullPath(ClientConfiguration.ResourcesDirectory).TrimEnd('\\', '/') + Path.DirectorySeparatorChar;
				_dossierDuCoffre = Path.Combine(_racine, "coffre");
				string path = Path.Combine(_dossierDuCoffre, "index.bin");
				if (!File.Exists(path))
				{
					return;
				}
				_dateDuCoffre = File.GetLastWriteTimeUtc(path);
				JObject jObject = JObject.Parse(Encoding.UTF8.GetString(Dechiffrer(File.ReadAllBytes(path), 0, "index")));
				Dictionary<string, Entree> dictionary = new Dictionary<string, Entree>(StringComparer.Ordinal);
				Dictionary<string, List<Entree>> dictionary2 = new Dictionary<string, List<Entree>>(StringComparer.Ordinal);
				HashSet<string> hashSet = new HashSet<string>(StringComparer.Ordinal);
				foreach (JToken item in (JArray)jObject["f"])
				{
					string text = (string?)item[0];
					Entree entree = new Entree(text, (string?)item[1], (long)item[2], (((JArray)item).Count > 3) ? ((int)item[3]) : 0);
					string text2 = text.ToLowerInvariant();
					dictionary[text2] = entree;
					int num = text2.LastIndexOf('/');
					string text3 = ((num < 0) ? string.Empty : text2.Substring(0, num));
					if (!dictionary2.TryGetValue(text3, out var value))
					{
						value = (dictionary2[text3] = new List<Entree>());
					}
					value.Add(entree);
					string text4 = text3;
					while (text4.Length > 0 && hashSet.Add(text4))
					{
						int num2 = text4.LastIndexOf('/');
						text4 = ((num2 > 0) ? text4.Substring(0, num2) : string.Empty);
					}
				}
				_parDossier = dictionary2;
				_dossiers = hashSet;
				_index = dictionary;
			}
			catch (Exception)
			{
				_index = null;
			}
			finally
			{
				_lu = true;
			}
		}
	}

	private static byte[] Dechiffrer(byte[] donnees, int palier, string chemin)
	{
		if (donnees.Length < Signature.Length + 12 + 16 || !donnees.AsSpan(0, Signature.Length).SequenceEqual(Signature))
		{
			throw new InvalidDataException("Fichier du coffre abîmé");
		}
		Span<byte> span = donnees.AsSpan(Signature.Length, 12);
		Span<byte> span2 = donnees.AsSpan(Signature.Length + 12, 16);
		Span<byte> span3 = donnees.AsSpan(Signature.Length + 12 + 16);
		byte[] array = new byte[span3.Length];
		byte[] array2 = ((palier == 0) ? CleDuCoffre() : CleDuJeu(chemin));
		try
		{
			using AesGcm aesGcm = new AesGcm(array2, 16);
			aesGcm.Decrypt(span, span3, span2, array);
			return array;
		}
		finally
		{
			CryptographicOperations.ZeroMemory(array2);
		}
	}

	private static byte[] CleDuJeu(string chemin)
	{
		CleMasquee cleDuJeu = _cleDuJeu;
		if ((object)cleDuJeu == null)
		{
			if (DemandesAvantLaCle.Count < 50 && DemandesAvantLaCle.TryAdd(chemin, 0))
			{
				ApplicationContext.Context.Value?.Logger.LogWarning("Coffre : {Chemin} demandé avant la clé du jeu", chemin);
			}
			throw new CoffreFermeException(chemin);
		}
		byte[] array = new byte[cleDuJeu.Cle.Length];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = (byte)(cleDuJeu.Cle[i] ^ cleDuJeu.Masque[i]);
		}
		return array;
	}
}
