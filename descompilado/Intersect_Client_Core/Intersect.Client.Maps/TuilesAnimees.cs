using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Intersect.Client.Framework.File_Management;
using Intersect.Configuration;
using Intersect.Core;
using Intersect.Framework.Core;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace Intersect.Client.Maps;

public static class TuilesAnimees
{
	public sealed class Animation
	{
		public int Largeur { get; init; } = 1;

		public int Hauteur { get; init; } = 1;

		public int Duree { get; init; } = 250;

		public (string Planche, int X, int Y)[] Images { get; init; } = Array.Empty<(string, int, int)>();
	}

	private const string Fichier = "tuiles-animees.json";

	private static Dictionary<(string Planche, int X, int Y), (Animation Animation, int Dx, int Dy)> _index = new Dictionary<(string, int, int), (Animation, int, int)>();

	private static long _verification;

	private static DateTime _date = DateTime.MinValue;

	public static int Version { get; private set; }

	public static void Relire()
	{
		long milliseconds = Timing.Global.Milliseconds;
		if (milliseconds < _verification)
		{
			return;
		}
		_verification = milliseconds + 1000;
		string text = Path.Combine(ClientConfiguration.ResourcesDirectory, "tuiles-animees.json");
		try
		{
			DateTime dateTime = (Coffre.Existe(text) ? Coffre.DateUtc(text) : DateTime.MinValue);
			if (dateTime == _date)
			{
				return;
			}
			_date = dateTime;
			Dictionary<(string, int, int), (Animation, int, int)> dictionary = new Dictionary<(string, int, int), (Animation, int, int)>();
			if (dateTime != DateTime.MinValue)
			{
				foreach (JObject item4 in JArray.Parse(Coffre.LireTexte(text)).OfType<JObject>())
				{
					(string, int, int)[] array = (from i in (item4["images"] as JArray)?.OfType<JObject>()
						select (((string?)i["planche"]) ?? "", ((int?)i["x"]).GetValueOrDefault(), ((int?)i["y"]).GetValueOrDefault()) into i
						where i.Item1.Length > 0
						select i).ToArray() ?? Array.Empty<(string, int, int)>();
					if (array.Length < 2)
					{
						continue;
					}
					Animation animation = new Animation
					{
						Largeur = Math.Max(1, ((int?)item4["largeur"]) ?? 1),
						Hauteur = Math.Max(1, ((int?)item4["hauteur"]) ?? 1),
						Duree = Math.Clamp(((int?)item4["duree"]) ?? 250, 30, 60000),
						Images = array
					};
					(string, int, int) tuple = array[0];
					string item = tuple.Item1;
					int item2 = tuple.Item2;
					int item3 = tuple.Item3;
					for (int num = 0; num < animation.Largeur; num++)
					{
						for (int num2 = 0; num2 < animation.Hauteur; num2++)
						{
							dictionary[(item.ToLowerInvariant(), item2 + num, item3 + num2)] = (animation, num, num2);
						}
					}
				}
			}
			_index = dictionary;
			Version++;
		}
		catch (Exception exception)
		{
			ApplicationContext.Context.Value?.Logger.LogWarning(exception, "Lecture de {Fichier} impossible", text);
		}
	}

	public static bool Trouver(string planche, int x, int y, out Animation animation, out int dx, out int dy)
	{
		if (_index.TryGetValue((planche.ToLowerInvariant(), x, y), out (Animation, int, int) value))
		{
			(animation, dx, dy) = value;
			return true;
		}
		animation = null;
		dx = (dy = 0);
		return false;
	}

	public static (string Planche, int X, int Y) Image(Animation animation, int dx, int dy, long millisecondes)
	{
		var (item, num, num2) = animation.Images[(int)(millisecondes / animation.Duree % animation.Images.Length)];
		return (Planche: item, X: num + dx, Y: num2 + dy);
	}
}
