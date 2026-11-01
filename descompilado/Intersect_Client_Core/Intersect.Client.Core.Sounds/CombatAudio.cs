using System;
using System.Collections.Generic;
using Intersect.Client.Entities;
using Intersect.Framework.Core;

namespace Intersect.Client.Core.Sounds;

public static class CombatAudio
{
	private const int HearingTiles = 12;

	private const float PainHealthShare = 0.12f;

	private const long PainCooldownMs = 2500L;

	private static readonly Random Noise = new Random();

	private static readonly Dictionary<Guid, long> LastPain = new Dictionary<Guid, long>();

	public static void Play(string filename, Entity entity, float spread = 0.08f)
	{
		MapSound mapSound = Audio.AddMapSound(filename, entity.X, entity.Y, entity.MapId, loop: false, 0, 12, entity);
		if (mapSound != null && spread > 0f)
		{
			mapSound.Pitch = (float)(Noise.NextDouble() * 2.0 - 1.0) * spread;
		}
	}

	public static void Swing(Entity attacker)
	{
		Play("miss.wav", attacker, 0.15f);
	}

	public static void Pain(Entity target, long damage)
	{
		if (!(target is Player) || target.IsDying)
		{
			return;
		}
		long num = target.MaxVital[0];
		if (num <= 0 || (float)damage < (float)num * 0.12f)
		{
			return;
		}
		long milliseconds = Timing.Global.Milliseconds;
		if (!LastPain.TryGetValue(target.Id, out var value) || milliseconds - value >= 2500)
		{
			if (LastPain.Count > 64)
			{
				LastPain.Clear();
			}
			LastPain[target.Id] = milliseconds;
			Play("pain.wav", target, 0.1f);
		}
	}
}
