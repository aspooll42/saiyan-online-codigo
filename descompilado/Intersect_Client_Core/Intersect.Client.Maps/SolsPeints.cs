using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Intersect.Client.Core;
using Intersect.Client.Framework.File_Management;
using Intersect.Client.Framework.Graphics;
using Intersect.Configuration;
using Intersect.Core;
using Microsoft.Extensions.Logging;

namespace Intersect.Client.Maps;

public static class SolsPeints
{
	private sealed class Sol
	{
		public IGameTexture? Texture;

		public long Octets;

		public long Vue;
	}

	private readonly record struct Decode(Guid Carte, string Couche, int Generation, byte[]? Rgba, int Largeur, int Hauteur, string? Fichier);

	private const int DecodagesSimultanes = 2;

	private const long OctetsGardes = 167772160L;

	private static readonly Dictionary<(Guid Carte, string Couche), Sol> Prets = new Dictionary<(Guid, string), Sol>();

	private static readonly Dictionary<Guid, int> Generations = new Dictionary<Guid, int>();

	private static readonly object Verrou = new object();

	private static readonly HashSet<(Guid Carte, string Couche)> EnCours = new HashSet<(Guid, string)>();

	private static readonly Queue<(Guid Carte, string Couche, int Generation)> AFaire = new Queue<(Guid, string, int)>();

	private static readonly Queue<Decode> APoser = new Queue<Decode>();

	private static int _decodages;

	private static long _image;

	private static long _octets;

	public static IGameTexture? Texture(Guid carte, string couche)
	{
		if (Prets.TryGetValue((carte, couche), out Sol value))
		{
			value.Vue = _image;
			return value.Texture;
		}
		Demander(carte, couche);
		return null;
	}

	public static void Preparer(Guid carte)
	{
		foreach (string item in Options.Instance.Map.Layers.All)
		{
			if (!Prets.ContainsKey((carte, item)))
			{
				Demander(carte, item);
			}
		}
	}

	public static bool ToutLu(Guid carte)
	{
		bool result = true;
		foreach (string item in Options.Instance.Map.Layers.All)
		{
			if (!Prets.ContainsKey((carte, item)))
			{
				Demander(carte, item);
				result = false;
			}
		}
		return result;
	}

	public static void Servir()
	{
		_image++;
		Decode decode;
		lock (Verrou)
		{
			if (APoser.Count == 0)
			{
				return;
			}
			decode = APoser.Dequeue();
			EnCours.Remove((decode.Carte, decode.Couche));
		}
		if (Generations.GetValueOrDefault(decode.Carte) != decode.Generation)
		{
			return;
		}
		IGameTexture texture = null;
		long num = 0L;
		string text = $"sols/{decode.Carte}_{decode.Couche}";
		byte[] rgba = decode.Rgba;
		if (rgba != null)
		{
			string source = decode.Fichier;
			if (source != null)
			{
				texture = Graphics.Renderer?.CreateTextureFromPixels(text, decode.Largeur, decode.Hauteur, rgba, () => Coffre.Ouvrir(source));
				num = (long)decode.Largeur * (long)decode.Hauteur * 4;
				goto IL_0160;
			}
		}
		string fichier = decode.Fichier;
		if (fichier != null)
		{
			texture = Graphics.Renderer?.LoadTexture(text, fichier);
		}
		goto IL_0160;
		IL_0160:
		if (Prets.Remove((decode.Carte, decode.Couche), out Sol value))
		{
			_octets -= value.Octets;
			Liberer(value.Texture);
		}
		Prets[(decode.Carte, decode.Couche)] = new Sol
		{
			Texture = texture,
			Octets = num,
			Vue = _image
		};
		_octets += num;
		Ranger();
	}

	public static void Oublier(Guid carte)
	{
		Generations[carte] = Generations.GetValueOrDefault(carte) + 1;
		(Guid, string)[] array = Prets.Keys.Where<(Guid, string)>(((Guid Carte, string Couche) cle) => cle.Carte == carte).ToArray();
		foreach ((Guid, string) key in array)
		{
			if (Prets.Remove(key, out Sol value))
			{
				_octets -= value.Octets;
				Liberer(value.Texture);
			}
		}
		lock (Verrou)
		{
			EnCours.RemoveWhere(((Guid Carte, string Couche) cle) => cle.Carte == carte);
		}
	}

	private static void Demander(Guid carte, string couche)
	{
		int valueOrDefault = Generations.GetValueOrDefault(carte);
		lock (Verrou)
		{
			if (EnCours.Add((carte, couche)))
			{
				AFaire.Enqueue((carte, couche, valueOrDefault));
				Lancer();
			}
		}
	}

	private static void Lancer()
	{
		while (_decodages < 2 && AFaire.Count > 0)
		{
			var (carte, couche, generation) = AFaire.Dequeue();
			_decodages++;
			Task.Run(delegate
			{
				Decoder(carte, couche, generation);
			});
		}
	}

	private static void Decoder(Guid carte, string couche, int generation)
	{
		byte[] rgba = null;
		int largeur = 0;
		int hauteur = 0;
		string fichier = null;
		try
		{
			string text = Path.Combine(ClientConfiguration.ResourcesDirectory, "sols", $"{carte}_{couche.Replace(' ', '_')}.png");
			if (!Coffre.Existe(text))
			{
				return;
			}
			fichier = text;
			using Stream flux = Coffre.Ouvrir(text);
			(byte[], int, int)? tuple = PngBrut.Lire(flux);
			if (tuple.HasValue)
			{
				(rgba, largeur, hauteur) = tuple.GetValueOrDefault();
			}
		}
		catch (Exception exception)
		{
			ApplicationContext.Context.Value?.Logger.LogWarning(exception, "Sol peint {Carte} / {Couche} illisible", carte, couche);
		}
		finally
		{
			lock (Verrou)
			{
				APoser.Enqueue(new Decode(carte, couche, generation, rgba, largeur, hauteur, fichier));
				_decodages--;
				Lancer();
			}
		}
	}

	private static void Ranger()
	{
		if (_octets <= 167772160)
		{
			return;
		}
		KeyValuePair<(Guid, string), Sol>[] array = (from entree in Prets
			where entree.Value.Octets > 0
			orderby entree.Value.Vue
			select entree).ToArray();
		for (int num = 0; num < array.Length; num++)
		{
			KeyValuePair<(Guid, string), Sol> keyValuePair = array[num];
			var (key, sol2) = keyValuePair;
			if (_octets > 167772160 && sol2.Vue <= _image - 60)
			{
				Prets.Remove(key);
				_octets -= sol2.Octets;
				Liberer(sol2.Texture);
				continue;
			}
			break;
		}
	}

	private static void Liberer(IGameTexture? texture)
	{
		try
		{
			texture?.Dispose();
		}
		catch (Exception exception)
		{
			ApplicationContext.Context.Value?.Logger.LogWarning(exception, "Sol peint non libere");
		}
	}
}
