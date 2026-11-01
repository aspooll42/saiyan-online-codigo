using System;
using System.Collections.Generic;
using Intersect.Client.Core;
using Intersect.Client.Framework.Content;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.General;
using Intersect.Client.Maps;
using Intersect.Framework.Core;

namespace Intersect.Client.Entities;

public static class Aureole
{
	private const string Fichier = "aureole.png";

	private static IGameTexture? _texture;

	private static bool _cherchee;

	private static readonly Dictionary<(string Planche, int X, int Y, int L, int H), int> Hauts = new Dictionary<(string, int, int, int, int), int>();

	public static void Dessiner(Entity entity, IGameTexture? sprite, FloatRect source, FloatRect corps, byte alpha)
	{
		if (entity is Player && alpha != 0 && sprite != null && MapInstance.TryGet(entity.MapId, out MapInstance instance) && Meteo.EstAuDela(instance))
		{
			if (!_cherchee)
			{
				_cherchee = true;
				_texture = Globals.ContentManager?.GetTexture(TextureType.Misc, "aureole.png");
			}
			IGameTexture texture = _texture;
			if (texture != null)
			{
				float num = MathF.Round(MathF.Sin((float)Timing.Global.Milliseconds / 420f + (float)(entity.Id.GetHashCode() & 0xFF)));
				float x = MathF.Round(corps.X + corps.Width / 2f - (float)texture.Width / 2f);
				float y = MathF.Round(corps.Y + (float)HautDuDessin(sprite, source) - (float)texture.Height - 2f + num);
				Graphics.DrawGameTexture(texture, new FloatRect(0f, 0f, texture.Width, texture.Height), new FloatRect(x, y, texture.Width, texture.Height), new Color(alpha, 255, 255, 255));
			}
		}
	}

	private static int HautDuDessin(IGameTexture sprite, FloatRect source)
	{
		(string, int, int, int, int) tuple = (sprite.Name ?? string.Empty, (int)source.X, (int)source.Y, (int)source.Width, (int)source.Height);
		if (Hauts.TryGetValue(tuple, out var value))
		{
			return value;
		}
		value = 0;
		(string, int, int, int, int) tuple2 = tuple;
		int item = tuple2.Item2;
		int item2 = tuple2.Item3;
		int item3 = tuple2.Item4;
		int item4 = tuple2.Item5;
		if (item >= 0 && item2 >= 0 && item3 > 0 && item4 > 0 && item + item3 <= sprite.Width && item2 + item4 <= sprite.Height && PixelsPlanche.Lire(sprite, out Color[] pixels) && pixels.Length >= sprite.Width * sprite.Height)
		{
			for (int i = 0; i < item4; i++)
			{
				if (value != 0)
				{
					break;
				}
				for (int j = 0; j < item3; j++)
				{
					if (pixels[(item2 + i) * sprite.Width + item + j].A >= 128)
					{
						value = i;
						break;
					}
				}
			}
		}
		Hauts[tuple] = value;
		return value;
	}
}
