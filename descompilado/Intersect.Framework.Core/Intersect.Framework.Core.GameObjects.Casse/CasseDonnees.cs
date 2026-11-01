using System;
using System.Collections.Generic;
using System.Linq;
using Intersect.Config;
using Intersect.Framework.Core.IO;
using Newtonsoft.Json;

namespace Intersect.Framework.Core.GameObjects.Casse;

public sealed class CasseDonnees
{
	private sealed class FichierDto
	{
		public int version { get; set; }

		public string genere { get; set; } = string.Empty;

		public string[] couches { get; set; } = Array.Empty<string>();

		public Dictionary<string, CarteDto> cartes { get; set; } = new Dictionary<string, CarteDto>();
	}

	private sealed class CarteDto
	{
		public string nom { get; set; } = string.Empty;

		public string sig { get; set; } = string.Empty;

		public int genre { get; set; }

		public List<ObjetDto> objets { get; set; } = new List<ObjetDto>();
	}

	private sealed class ObjetDto
	{
		public int g { get; set; }

		public int x { get; set; }

		public int y { get; set; }

		public int w { get; set; }

		public int h { get; set; }

		public int[] c { get; set; } = Array.Empty<int>();

		public int[][] p { get; set; } = Array.Empty<int[]>();

		public string? m { get; set; }

		public int r { get; set; }

		public List<List<object>>? l { get; set; }
	}

	public int Version { get; init; }

	public string Genere { get; init; } = string.Empty;

	public Dictionary<Guid, CasseCarte> Cartes { get; init; } = new Dictionary<Guid, CasseCarte>();

	public static CasseDonnees? Lire(string chemin)
	{
		if (!LectureDesRessources.Existe(chemin))
		{
			return null;
		}
		FichierDto fichierDto = JsonConvert.DeserializeObject<FichierDto>(LectureDesRessources.LireTexte(chemin));
		if (fichierDto?.cartes == null)
		{
			return null;
		}
		string[] array = fichierDto.couches ?? Array.Empty<string>();
		Dictionary<Guid, CasseCarte> dictionary = new Dictionary<Guid, CasseCarte>();
		foreach (var (input, carteDto2) in fichierDto.cartes)
		{
			if (!Guid.TryParse(input, out var result) || carteDto2?.objets == null)
			{
				continue;
			}
			CasseObjet[] array2 = new CasseObjet[carteDto2.objets.Count];
			Dictionary<(int, int), List<int>> dictionary2 = new Dictionary<(int, int), List<int>>();
			for (int i = 0; i < array2.Length; i++)
			{
				ObjetDto objetDto = carteDto2.objets[i];
				(string, int, int)[] array3 = new(string, int, int)[objetDto.c.Length / 3];
				for (int j = 0; j < array3.Length; j++)
				{
					int num = objetDto.c[j * 3];
					array3[j] = ((num >= 0 && num < array.Length) ? array[num] : string.Empty, objetDto.c[j * 3 + 1], objetDto.c[j * 3 + 2]);
					(int, int) key = (objetDto.c[j * 3 + 1], objetDto.c[j * 3 + 2]);
					if (!dictionary2.TryGetValue(key, out var value))
					{
						value = (dictionary2[key] = new List<int>());
					}
					if (!value.Contains(i))
					{
						value.Add(i);
					}
				}
				array2[i] = new CasseObjet
				{
					Genre = (CasseGenre)objetDto.g,
					X = objetDto.x,
					Y = objetDto.y,
					Largeur = objetDto.w,
					Hauteur = objetDto.h,
					Cases = array3,
					Parties = (from p in objetDto.p ?? Array.Empty<int[]>()
						where p != null && p.Length >= 2
						select (p[0], p[1])).ToArray(),
					Silhouette = LireSilhouette(objetDto.m),
					Rocher = (objetDto.r == 1),
					Liens = LireLiens(objetDto.l)
				};
			}
			dictionary[result] = new CasseCarte
			{
				Id = result,
				Nom = (carteDto2.nom ?? string.Empty),
				Signature = (carteDto2.sig ?? string.Empty),
				Genre = (CasseCarteGenre)Math.Clamp(carteDto2.genre, 0, 2),
				Objets = array2,
				ParCase = dictionary2.ToDictionary((KeyValuePair<(int, int), List<int>> p) => p.Key, (KeyValuePair<(int, int), List<int>> p) => p.Value.ToArray())
			};
		}
		return new CasseDonnees
		{
			Version = fichierDto.version,
			Genere = (fichierDto.genere ?? string.Empty),
			Cartes = dictionary
		};
	}

	private static (int, Guid, int, int)[] LireLiens(List<List<object>>? liens)
	{
		if (liens == null)
		{
			return Array.Empty<(int, Guid, int, int)>();
		}
		List<(int, Guid, int, int)> list = new List<(int, Guid, int, int)>();
		foreach (List<object> lien in liens)
		{
			if (lien != null && lien.Count >= 4 && Guid.TryParse(lien[1]?.ToString(), out var result))
			{
				list.Add((Convert.ToInt32(lien[0]), result, Convert.ToInt32(lien[2]), Convert.ToInt32(lien[3])));
			}
		}
		return list.ToArray();
	}

	private static byte[] LireSilhouette(string? base64)
	{
		if (string.IsNullOrEmpty(base64))
		{
			return Array.Empty<byte>();
		}
		try
		{
			return Convert.FromBase64String(base64);
		}
		catch (FormatException)
		{
			return Array.Empty<byte>();
		}
	}

	public static string Signature(byte[]? octets)
	{
		if (octets == null)
		{
			return string.Empty;
		}
		ulong num = 14695981039346656037uL;
		foreach (byte b in octets)
		{
			num ^= b;
			num *= 1099511628211L;
		}
		return num.ToString("x16");
	}
}
