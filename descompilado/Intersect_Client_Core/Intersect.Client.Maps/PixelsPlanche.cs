using System;
using System.IO;
using Intersect.Client.Framework.Graphics;
using Intersect.Core;
using Microsoft.Extensions.Logging;

namespace Intersect.Client.Maps;

internal static class PixelsPlanche
{
	public static bool Lire(IGameTexture? texture, out Color[] pixels)
	{
		pixels = Array.Empty<Color>();
		if (texture == null)
		{
			return false;
		}
		try
		{
			using Stream stream = texture.OuvrirSource();
			if (stream != null)
			{
				(byte[], int, int)? tuple = PngBrut.Lire(stream);
				if (tuple.HasValue)
				{
					(byte[], int, int) valueOrDefault = tuple.GetValueOrDefault();
					if (valueOrDefault.Item2 == texture.Width && valueOrDefault.Item3 == texture.Height)
					{
						byte[] item = valueOrDefault.Item1;
						Color[] array = new Color[valueOrDefault.Item2 * valueOrDefault.Item3];
						for (int i = 0; i < array.Length; i++)
						{
							int num = i * 4;
							array[i] = new Color(item[num + 3], item[num], item[num + 1], item[num + 2]);
						}
						pixels = array;
						return true;
					}
				}
			}
		}
		catch (Exception exception)
		{
			ApplicationContext.Context.Value?.Logger.LogWarning(exception, "Planche {Planche} illisible sans la carte graphique", texture.Name);
		}
		return texture.TryGetPixels(out pixels);
	}
}
