using System;
using System.Collections.Generic;
using Intersect.Client.Core;
using Intersect.Client.Entities;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.General;
using Intersect.Enums;
using Intersect.Framework.Core;

namespace Intersect.Client.Maps;

internal static class CapsulePilier
{
	private const long DureeMs = 1000L;

	private static readonly List<(Guid Carte, float X, float Y, long Debut)> Piliers = new List<(Guid, float, float, long)>();

	private static readonly Color Or = new Color(255, 255, 220, 140);

	public static void Allumer()
	{
		Player me = Globals.Me;
		if (me == null)
		{
			return;
		}
		MapInstance mapInstance = MapInstance.Get(me.MapId);
		if (mapInstance == null)
		{
			return;
		}
		int num = 0;
		int num2 = 0;
		switch (me.DirectionFacing)
		{
		case Direction.Up:
			num2 = -1;
			break;
		case Direction.Down:
			num2 = 1;
			break;
		case Direction.Left:
			num = -1;
			break;
		case Direction.Right:
			num = 1;
			break;
		}
		int tileWidth = Options.Instance.Map.TileWidth;
		int tileHeight = Options.Instance.Map.TileHeight;
		lock (Piliers)
		{
			Piliers.Add((mapInstance.Id, (float)(mapInstance.X + (me.X + num) * tileWidth) + (float)tileWidth / 2f, (float)(mapInstance.Y + (me.Y + num2) * tileHeight) + (float)tileHeight * 0.8f, Timing.Global.MillisecondsUtc));
		}
	}

	public static void Dessiner(MapInstance carte, long maintenant)
	{
		(Guid, float, float, long)[] array;
		lock (Piliers)
		{
			Piliers.RemoveAll(((Guid Carte, float X, float Y, long Debut) pilier) => maintenant - pilier.Debut > 1000);
			if (Piliers.Count == 0)
			{
				return;
			}
			array = Piliers.ToArray();
		}
		IGameTexture gameTexture = Graphics.Renderer?.WhitePixel;
		if (gameTexture == null)
		{
			return;
		}
		FloatRect srcRectangle = new FloatRect(0f, 0f, 1f, 1f);
		(Guid, float, float, long)[] array2 = array;
		for (int num = 0; num < array2.Length; num++)
		{
			var (guid, num2, num3, num4) = array2[num];
			if (guid != carte.Id)
			{
				continue;
			}
			float num5 = Math.Clamp((float)(maintenant - num4) / 1000f, 0f, 1f);
			float num6 = Math.Clamp(num5 / 0.2f, 0f, 1f);
			float num7 = ((num5 < 0.2f) ? num6 : (1f - (num5 - 0.2f) / 0.8f));
			int num8 = (int)(150f * (1f - MathF.Pow(1f - num6, 3f)));
			float num9 = ((num5 < 0.2f) ? (4f + 10f * num6) : (14f - 10f * ((num5 - 0.2f) / 0.8f)));
			for (int num10 = 0; num10 < num8; num10 += 2)
			{
				float num11 = 1f - (float)num10 / 150f;
				int num12 = (int)(230f * num11 * num11 * num7);
				if (num12 > 2)
				{
					Graphics.DrawGameTexture(gameTexture, srcRectangle, new FloatRect(num2 - num9 / 2f, num3 - (float)num10 - 2f, num9, 2f), new Color(num12, Or.R, Or.G, Or.B), null, GameBlendModes.Add);
					Graphics.DrawGameTexture(gameTexture, srcRectangle, new FloatRect(num2 - num9 / 6f, num3 - (float)num10 - 2f, num9 / 3f, 2f), new Color(num12, 255, 250, 232), null, GameBlendModes.Add);
				}
			}
			float num13 = 1f - MathF.Pow(1f - Math.Clamp(num5 / 0.7f, 0f, 1f), 2f);
			int num14 = (int)(220f * (1f - num13));
			if (num14 > 2)
			{
				float num15 = 8f + 38f * num13;
				float num16 = num15 * 0.38f;
				for (int num17 = 0; num17 < 40; num17++)
				{
					float x = (float)num17 * ((float)Math.PI * 2f) / 40f;
					Graphics.DrawGameTexture(gameTexture, srcRectangle, new FloatRect(num2 + MathF.Cos(x) * num15 - 1f, num3 + MathF.Sin(x) * num16 - 1f, 2f, 2f), new Color(num14, Or.R, Or.G, Or.B), null, GameBlendModes.Add);
				}
			}
			for (int num18 = 0; num18 < 12; num18++)
			{
				float num19 = 0.12f + (float)num18 * 0.02f;
				float num20 = Math.Clamp((num5 - num19) / 0.75f, 0f, 1f);
				if ((!(num20 <= 0f) && !(num20 >= 1f)) || 1 == 0)
				{
					float x2 = -(float)Math.PI / 2f + ((float)num18 - 5.5f) * 0.42f;
					int num21 = 50 + num18 % 3 * 22;
					float num22 = num2 + MathF.Cos(x2) * (float)num21 * num20;
					float num23 = num3 - 22f + MathF.Sin(x2) * (float)(60 + num18 % 4 * 18) * num20 + 70f * num20 * num20;
					Graphics.DrawGameTexture(gameTexture, srcRectangle, new FloatRect(num22 - 1.5f, num23 - 1.5f, 3f, 3f), new Color((int)(255f * (1f - num20)), 255, 233, 168), null, GameBlendModes.Add);
				}
			}
		}
	}
}
