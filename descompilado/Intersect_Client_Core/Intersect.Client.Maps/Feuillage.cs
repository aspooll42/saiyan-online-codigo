using System;
using System.Collections.Generic;
using System.Numerics;
using Intersect.Client.Core;
using Intersect.Client.Entities;
using Intersect.Client.Entities.Events;
using Intersect.Client.Entities.Projectiles;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.General;
using Intersect.Client.MonoGame.Graphics;
using Intersect.Config;
using Intersect.Framework.Core;

namespace Intersect.Client.Maps;

public static class Feuillage
{
	private sealed class Masque
	{
		public IGameTexture? Texture;

		public readonly byte[] Pixels = new byte[107584];

		public int X = int.MinValue;

		public int Y;

		public float Retire = -1f;
	}

	private const int Rayon = 80;

	private const float Plein = 0.45f;

	private const float Opacite = 0.4f;

	private const float PorteeDuCombat = 192f;

	private const long SouvenirDuCombat = 3000L;

	private const float VitesseDuFondu = 4f;

	private const int Cote = 164;

	private const int TroueesMax = 4;

	private static readonly float[] Bayer = new float[16]
	{
		0f, 8f, 2f, 10f, 12f, 4f, 14f, 6f, 3f, 11f,
		1f, 9f, 15f, 7f, 13f, 5f
	};

	private static readonly Dictionary<Guid, float> Ouvertures = new Dictionary<Guid, float>();

	private static readonly Dictionary<Guid, long> DerniersCoups = new Dictionary<Guid, long>();

	private static readonly List<(float X, float Y, float Ouverture)> Trouees = new List<(float, float, float)>();

	private static readonly List<Masque> Masques = new List<Masque>();

	private static readonly List<MapInstance> CartesProches = new List<MapInstance>();

	private static readonly List<Guid> AOublier = new List<Guid>();

	private static long _derniereImage;

	public static bool Commencer()
	{
		long millisecondsUtc = Timing.Global.MillisecondsUtc;
		float num = ((_derniereImage == 0L) ? 0f : Math.Clamp((float)(millisecondsUtc - _derniereImage) / 1000f, 0f, 0.1f));
		_derniereImage = millisecondsUtc;
		Trouees.Clear();
		Player me = Globals.Me;
		if (me == null || !(Graphics.Renderer is MonoRenderer monoRenderer))
		{
			Ouvertures.Clear();
			return false;
		}
		ReperesCartesProches(me);
		Vector2 center = me.Center;
		foreach (Entity value3 in Globals.Entities.Values)
		{
			bool flag = value3 == null;
			bool flag2 = flag;
			if (!flag2)
			{
				bool flag3 = ((value3 is Resource || value3 is Projectile || value3 is Event) ? true : false);
				flag2 = flag3;
			}
			if (flag2)
			{
				continue;
			}
			Vector2 center2 = value3.Center;
			float num2 = 0f;
			if (!value3.IsHidden && !Flight.IsAirborne(value3))
			{
				bool flag4 = value3 == me;
				if (!flag4 && Vector2.Distance(center2, center) <= 192f)
				{
					if (value3.IsAttacking || value3.IsCasting)
					{
						DerniersCoups[value3.Id] = millisecondsUtc;
					}
					flag4 = value3.Id == me.TargetId || (DerniersCoups.TryGetValue(value3.Id, out var value) && millisecondsUtc - value < 3000);
				}
				if (flag4 && SousLesCimes(center2.X, center2.Y))
				{
					num2 = 1f;
				}
			}
			Ouvertures.TryGetValue(value3.Id, out var value2);
			value2 += (float)Math.Sign(num2 - value2) * Math.Min(Math.Abs(num2 - value2), num * 4f);
			if (value2 <= 0f)
			{
				Ouvertures.Remove(value3.Id);
				continue;
			}
			Ouvertures[value3.Id] = value2;
			Trouees.Add((center2.X, center2.Y, value2));
		}
		Oublier(millisecondsUtc);
		if (Trouees.Count == 0)
		{
			return false;
		}
		if (Trouees.Count > 4)
		{
			Trouees.Sort(((float X, float Y, float Ouverture) a, (float X, float Y, float Ouverture) b) => b.Ouverture.CompareTo(a.Ouverture));
			Trouees.RemoveRange(4, Trouees.Count - 4);
		}
		return monoRenderer.CommencerCimes();
	}

	public static void Finir(bool ouverte)
	{
		if (!ouverte || !(Graphics.Renderer is MonoRenderer monoRenderer))
		{
			return;
		}
		for (int i = 0; i < Trouees.Count; i++)
		{
			Masque masque = PreparerMasque(monoRenderer, i, Trouees[i]);
			if (masque != null)
			{
				IGameTexture texture = masque.Texture;
				if (texture != null)
				{
					Graphics.DrawGameTexture(texture, masque.X, masque.Y, null, GameBlendModes.Cutout);
				}
			}
		}
		monoRenderer.FinirCimes();
	}

	private static Masque? PreparerMasque(MonoRenderer rendu, int indice, (float X, float Y, float Ouverture) trouee)
	{
		while (Masques.Count <= indice)
		{
			Masques.Add(new Masque());
		}
		Masque masque = Masques[indice];
		int num = (int)MathF.Round(trouee.X);
		int num2 = (int)MathF.Round(trouee.Y);
		float num3 = trouee.Ouverture * 0.6f;
		int num4 = num - 82;
		int num5 = num2 - 82;
		if (masque.Texture != null && masque.X == num4 && masque.Y == num5 && MathF.Abs(masque.Retire - num3) < 0.004f)
		{
			return masque;
		}
		byte[] pixels = masque.Pixels;
		for (int i = 0; i < 164; i++)
		{
			float num6 = (float)(num5 + i) + 0.5f - trouee.Y;
			int num7 = ((num5 + i) % 4 + 4) % 4 * 4;
			for (int j = 0; j < 164; j++)
			{
				float num8 = (float)(num4 + j) + 0.5f - trouee.X;
				float num9 = MathF.Sqrt(num8 * num8 + num6 * num6) / 80f;
				float num10 = ((num9 <= 0.45f) ? 1f : ((num9 >= 1f) ? 0f : (1f - (num9 - 0.45f) / 0.55f)));
				num10 = num10 * num10 * (3f - 2f * num10);
				float num11 = (Bayer[num7 + ((num4 + j) % 4 + 4) % 4] + 0.5f) / 16f;
				int num12 = (i * 164 + j) * 4;
				bool flag = num11 < num10 * num3;
				int num13 = num12 + 1;
				byte b;
				pixels[num12 + 2] = (b = byte.MaxValue);
				pixels[num12] = (pixels[num13] = b);
				pixels[num12 + 3] = (byte)(flag ? byte.MaxValue : 0);
			}
		}
		if (masque.Texture == null)
		{
			masque.Texture = rendu.CreerTexturePixels($"feuillage/trouee/{indice}", 164, 164, pixels);
		}
		else if (!MonoRenderer.RemplacerPixels(masque.Texture, pixels))
		{
			return null;
		}
		masque.X = num4;
		masque.Y = num5;
		masque.Retire = num3;
		return masque;
	}

	private static void ReperesCartesProches(Entity moi)
	{
		CartesProches.Clear();
		if (!MapInstance.TryGet(moi.MapId, out MapInstance instance))
		{
			return;
		}
		for (int i = instance.GridX - 1; i <= instance.GridX + 1; i++)
		{
			for (int j = instance.GridY - 1; j <= instance.GridY + 1; j++)
			{
				if (i < 0 || j < 0 || i >= Globals.MapGridWidth || j >= Globals.MapGridHeight)
				{
					continue;
				}
				Guid[,]? mapGrid = Globals.MapGrid;
				Guid? guid = ((mapGrid != null) ? new Guid?(mapGrid[i, j]) : ((Guid?)null));
				if (guid.HasValue)
				{
					Guid valueOrDefault = guid.GetValueOrDefault();
					if (valueOrDefault != Guid.Empty && MapInstance.TryGet(valueOrDefault, out MapInstance instance2))
					{
						CartesProches.Add(instance2);
					}
				}
			}
		}
	}

	private static bool SousLesCimes(float x, float y)
	{
		return (CimeEn(x, y - 14f) ? 1 : 0) + (CimeEn(x, y) ? 1 : 0) + (CimeEn(x, y + 12f) ? 1 : 0) >= 2;
	}

	private static bool CimeEn(float x, float y)
	{
		MapOptions map = Options.Instance.Map;
		int num = map.MapWidth * map.TileWidth;
		int num2 = map.MapHeight * map.TileHeight;
		foreach (MapInstance cartesProch in CartesProches)
		{
			if (x < (float)cartesProch.X || y < (float)cartesProch.Y || x >= (float)(cartesProch.X + num) || y >= (float)(cartesProch.Y + num2))
			{
				continue;
			}
			int num3 = (int)((x - (float)cartesProch.X) / (float)map.TileWidth);
			int num4 = (int)((y - (float)cartesProch.Y) / (float)map.TileHeight);
			foreach (string upperLayer in map.Layers.UpperLayers)
			{
				if (cartesProch.Layers != null && cartesProch.Layers.TryGetValue(upperLayer, out var value) && num3 < value.GetLength(0) && num4 < value.GetLength(1) && value[num3, num4].TilesetId != Guid.Empty)
				{
					return true;
				}
			}
			return false;
		}
		return false;
	}

	private static void Oublier(long maintenant)
	{
		AOublier.Clear();
		foreach (var (guid2, num2) in DerniersCoups)
		{
			if (maintenant - num2 >= 3000 || !Globals.Entities.ContainsKey(guid2))
			{
				AOublier.Add(guid2);
			}
		}
		foreach (Guid key in Ouvertures.Keys)
		{
			if (!Globals.Entities.ContainsKey(key))
			{
				AOublier.Add(key);
			}
		}
		foreach (Guid item in AOublier)
		{
			DerniersCoups.Remove(item);
			Ouvertures.Remove(item);
		}
	}
}
