using System;
using System.Collections.Generic;
using Intersect.Client.Core;
using Intersect.Framework.Core;

namespace Intersect.Client.Entities;

public static class ChuteDansLeVide
{
	private const float Profondeur = 64f;

	private const int AttenteDuRetourMs = 1500;

	private static readonly Dictionary<Guid, (long Debut, long Fin)> Chutes = new Dictionary<Guid, (long, long)>();

	public static void Commencer(Entity entity, Dash dash)
	{
		long milliseconds = Timing.Global.Milliseconds;
		long num = Math.Max(200L, dash.EndsAt - dash.StartedAt);
		Chutes[entity.Id] = (milliseconds, milliseconds + num);
		Audio.AddMapSound("Esquive.wav", entity.X, entity.Y, entity.MapId, loop: false, 0, 12, entity);
	}

	private static float Avancement(Entity entity)
	{
		if (Chutes.Count == 0 || !Chutes.TryGetValue(entity.Id, out (long, long) value))
		{
			return -1f;
		}
		long milliseconds = Timing.Global.Milliseconds;
		if (milliseconds > value.Item2 + 1500)
		{
			Chutes.Remove(entity.Id);
			return -1f;
		}
		return Math.Clamp((float)(milliseconds - value.Item1) / (float)Math.Max(1L, value.Item2 - value.Item1), 0f, 1f);
	}

	public static float Enfoncement(Entity entity)
	{
		float num = Avancement(entity);
		if (!(num <= 0f))
		{
			return 64f * num * num;
		}
		return 0f;
	}

	public static byte Alpha(Entity entity, byte alpha)
	{
		float num = Avancement(entity);
		if (num < 0f)
		{
			return alpha;
		}
		float num2 = ((num < 0.35f) ? 1f : Math.Clamp(1f - (num - 0.35f) / 0.6f, 0f, 1f));
		return (byte)((float)(int)alpha * num2);
	}

	public static void Finir(Guid entityId)
	{
		if (Chutes.Count > 0 && Chutes.TryGetValue(entityId, out (long, long) value) && Timing.Global.Milliseconds >= value.Item2 - 60)
		{
			Chutes.Remove(entityId);
		}
	}
}
