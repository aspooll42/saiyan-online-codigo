using System;
using System.Collections.Generic;
using Intersect.Client.Core;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Graphics;
using Intersect.Framework.Core;
using Intersect.Network.Packets.Server;

namespace Intersect.Client.Entities;

public static class Afterimages
{
	private const long DashEveryMs = 45L;

	private const long DashLifeMs = 220L;

	private const long VanishLifeMs = 450L;

	private static readonly Dictionary<Guid, (IGameTexture Texture, FloatRect Source, FloatRect Destination, long LastEchoAt)> Frames = new Dictionary<Guid, (IGameTexture, FloatRect, FloatRect, long)>();

	private static readonly List<(IGameTexture Texture, FloatRect Source, FloatRect Destination, long StartedAt, long Life, float Alpha, bool Franche)> Echoes = new List<(IGameTexture, FloatRect, FloatRect, long, long, float, bool)>();

	public static void Remember(Entity entity, IGameTexture texture, FloatRect source, FloatRect destination)
	{
		long milliseconds = Timing.Global.Milliseconds;
		Frames.TryGetValue(entity.Id, out (IGameTexture, FloatRect, FloatRect, long) value);
		long num = value.Item4;
		bool flag = value.Item1 == null || value.Item3.X != destination.X || value.Item3.Y != destination.Y;
		Dash dashing = entity.Dashing;
		bool flag2 = dashing != null && dashing.Nature == NatureDeGlissade.Dash;
		if (entity.Dashing != null && flag && milliseconds - num >= (flag2 ? 40 : 45) && (!Heritage.Actif || !Heritage.Fantomes.SansEcho(entity)))
		{
			Add(texture, source, destination, milliseconds, flag2 ? 200 : 220, flag2 ? 0.5f : 0.55f, flag2);
			num = milliseconds;
		}
		if (Heritage.Actif && entity.Dashing == null && flag && milliseconds - num >= 45 && Heritage.Elan.Remanent(entity))
		{
			Add(texture, source, destination, milliseconds, 220L, 0.55f);
			num = milliseconds;
		}
		if (Frames.Count > 256 && !Frames.ContainsKey(entity.Id))
		{
			Frames.Clear();
		}
		Frames[entity.Id] = (texture, source, destination, num);
		if (Heritage.Actif)
		{
			Heritage.Fantomes.Noter(entity, texture, source, destination);
		}
	}

	public static void Vanish(Entity entity, float shiftX, float shiftY)
	{
		if (Frames.TryGetValue(entity.Id, out (IGameTexture, FloatRect, FloatRect, long) value) && value.Item1 != null)
		{
			long milliseconds = Timing.Global.Milliseconds;
			Add(value.Item1, value.Item2, value.Item3, milliseconds, 450L, 0.8f);
			for (int i = 1; i <= 3; i++)
			{
				float num = (float)i / 4f;
				Add(destination: new FloatRect(value.Item3.X + shiftX * num, value.Item3.Y + shiftY * num, value.Item3.Width, value.Item3.Height), texture: value.Item1, source: value.Item2, now: milliseconds, life: 160 + 50 * i, alpha: 0.5f - 0.1f * (float)i);
			}
		}
	}

	public static void Draw()
	{
		if (Echoes.Count == 0)
		{
			return;
		}
		long milliseconds = Timing.Global.Milliseconds;
		for (int num = Echoes.Count - 1; num >= 0; num--)
		{
			if (milliseconds - Echoes[num].StartedAt >= Echoes[num].Life)
			{
				Echoes.RemoveAt(num);
			}
		}
		foreach (var echo in Echoes)
		{
			float num2 = 1f - (float)(milliseconds - echo.StartedAt) / (float)echo.Life;
			int num3 = (int)(255f * echo.Alpha * num2 * num2);
			if (num3 > 0)
			{
				if (echo.Franche)
				{
					Graphics.DrawGameTexture(echo.Texture, echo.Source, echo.Destination, new Color(num3, 255, 255, 255));
					continue;
				}
				Graphics.DrawGameTexture(echo.Texture, echo.Source, echo.Destination, new Color(num3, 140, 200, 255));
				Graphics.DrawGameTexture(echo.Texture, echo.Source, echo.Destination, new Color(num3 / 3, 120, 190, 255), null, GameBlendModes.Add);
			}
		}
	}

	private static void Add(IGameTexture texture, FloatRect source, FloatRect destination, long now, long life, float alpha, bool franche = false)
	{
		Echoes.Add((texture, source, destination, now, life, alpha, franche));
		if (Echoes.Count > 96)
		{
			Echoes.RemoveAt(0);
		}
	}
}
