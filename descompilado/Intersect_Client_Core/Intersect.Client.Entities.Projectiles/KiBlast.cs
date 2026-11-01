using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using Intersect.Client.Core;
using Intersect.Client.Core.Sounds;
using Intersect.Client.Framework.Content;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.Framework.Maps;
using Intersect.Client.General;
using Intersect.Client.Maps;
using Intersect.Enums;
using Intersect.Framework.Core;
using Intersect.Framework.Core.GameObjects.Animations;
using Intersect.GameObjects;
using Intersect.Network.Packets.Server;

namespace Intersect.Client.Entities.Projectiles;

public static class KiBlast
{
	private sealed record Palette(Color Core, Color Light, Color Mid, Color Dark);

	private readonly record struct Mote(float X, float Y, float Vx, float Vy, long StartedAt, long Life, Color Color, float Size, bool Front);

	private readonly record struct Flash(string Texture, float X, float Y, float HeadingX, float HeadingY, long StartedAt);

	private readonly record struct Burst(string Texture, float X, float Y, long StartedAt, int FirstFrame, int Alpha, Color Glow, bool Ring);

	private readonly record struct Clash(string LeadingTexture, string TrailingTexture, float X, float Y, bool Vertical, long StartedAt, Palette LeadingPalette, Palette TrailingPalette, Guid MapId, int TileY, int Seed);

	private readonly record struct BouleRangee(ProjectileSpawns Spawn, Palette Palette, float Sol, int Rangee);

	private sealed class Scorch
	{
		public float X;

		public float Y;

		public long StartedAt;

		public int Variant;

		public int Seed;

		public bool Fatal;

		public long FatalAt;
	}

	private readonly record struct Chip(float X, float Y, float Vx, float Vy, long StartedAt, long Life, Color Color, int Size);

	private const long LaunchMs = 130L;

	private const long ImpactFrameMs = 45L;

	private const long ImpactGlowMs = 150L;

	private const long RingMs = 220L;

	private const long ClashFrameMs = 55L;

	private const long ClashGlowMs = 200L;

	private const long ClashFlickerMs = 240L;

	private const float ClashShakeTiles = 6f;

	private const float TrailSpacing = 3f;

	private const int TrailPerFrameMax = 8;

	private const float HaloSize = 28f;

	private const float ContactReach = 14f;

	private const float EndReach = 24f;

	private const float Drag = 5f;

	private const int MaxMotes = 700;

	private static readonly Random Noise = new Random();

	private static readonly Palette Warm = new Palette(new Color(255, 248, 248, 248), new Color(255, 248, 248, 72), new Color(255, 248, 208, 0), new Color(255, 248, 176, 40));

	private static readonly Dictionary<string, Palette> Palettes = new Dictionary<string, Palette>(StringComparer.OrdinalIgnoreCase);

	private static readonly Dictionary<ProjectileSpawns, Palette> Live = new Dictionary<ProjectileSpawns, Palette>();

	private static readonly List<Mote> Motes = new List<Mote>();

	private static readonly List<Flash> Flashes = new List<Flash>();

	private static readonly List<Burst> Bursts = new List<Burst>();

	private static readonly List<Clash> Clashes = new List<Clash>();

	private const float RattrapageMax = 64f;

	private const long ClashSparksMs = 480L;

	private static readonly List<BouleRangee> sARanger = new List<BouleRangee>();

	private static readonly HashSet<Animation> sDessineesDansLesRangees = new HashSet<Animation>();

	private static int sDerniereRangee;

	private const long RecoilMs = 160L;

	private const float RecoilPixels = 2.5f;

	private const float FlightGroundDrop = 14f;

	private const long ScorchLifeMs = 3000L;

	private const long FatalScorchLifeMs = 4000L;

	private const long ScorchFadeMs = 800L;

	private const long EmbersMs = 1800L;

	private const long CracksGrowMs = 700L;

	private const long KillWindowMs = 700L;

	private const float KillProjectionPixels = 48f;

	private const long KillProjectionMs = 380L;

	private const long KillDeathMs = 950L;

	private const long SlowMotionMs = 280L;

	private const float SlowMotionSpeed = 0.6f;

	private const float ChipGravity = 520f;

	private static readonly Dictionary<Guid, (long At, float X, float Y)> Recoils = new Dictionary<Guid, (long, float, float)>();

	private static readonly List<Scorch> Scorches = new List<Scorch>();

	private static readonly List<Chip> Chips = new List<Chip>();

	private static readonly Dictionary<Guid, (long At, float HeadingX, float HeadingY, Guid Owner)> LocalHits = new Dictionary<Guid, (long, float, float, Guid)>();

	private static readonly Color[] EarthColors = new Color[4]
	{
		new Color(255, 138, 106, 58),
		new Color(255, 164, 131, 78),
		new Color(255, 107, 82, 48),
		new Color(255, 194, 162, 106)
	};

	public static void OnFlight(ProjectileSpawns spawn, float x, float y, Guid ownerId, Guid targetId)
	{
		if (spawn.Anim == null || spawn.KiImpacted)
		{
			return;
		}
		string sprite = SpriteOf(spawn);
		Palette palette = PaletteOf(sprite);
		long milliseconds = Timing.Global.Milliseconds;
		float kiX = spawn.KiX;
		float kiY = spawn.KiY;
		bool kiStarted = spawn.KiStarted;
		float y2 = kiY;
		float x2 = kiX;
		spawn.KiTireur = ownerId;
		if (!spawn.KiStarted)
		{
			spawn.KiStarted = true;
			(float, float) tuple = InitialHeading(spawn, x, y, targetId);
			spawn.KiHeadingX = tuple.Item1;
			spawn.KiHeadingY = tuple.Item2;
			spawn.KiX = x;
			spawn.KiY = y;
			Live[spawn] = palette;
			Launch(spawn, sprite, palette, x, y, ownerId, milliseconds);
		}
		else
		{
			float num = x - spawn.KiX;
			float num2 = y - spawn.KiY;
			float num3 = MathF.Sqrt(num * num + num2 * num2);
			if (num3 > 0.5f)
			{
				spawn.KiHeadingX = num / num3;
				spawn.KiHeadingY = num2 / num3;
				Trail(spawn.KiX, spawn.KiY, x, y, spawn.KiHeadingX, spawn.KiHeadingY, num3, palette, milliseconds);
			}
			spawn.KiX = x;
			spawn.KiY = y;
			Live[spawn] = palette;
		}
		if (Heritage.Actif)
		{
			Heritage.Boule.EnVol(spawn, x, y, spawn.KiHeadingX, spawn.KiHeadingY, ownerId);
		}
		if (spawn.KiClashedAt > 0)
		{
			long num4 = milliseconds - spawn.KiClashedAt;
			if (num4 < 240 && num4 / 40 % 2 == 0L)
			{
				spawn.Anim.Hide();
			}
			else
			{
				spawn.Anim.Show();
			}
			if (num4 >= 240)
			{
				spawn.KiClashedAt = 0L;
			}
		}
		float impactX;
		float impactY;
		if (!kiStarted)
		{
			spawn.KiDecorSous = Casse.ObjetsSous(x, y);
		}
		else if (Casse.PremierImpact(x2, y2, x, y, spawn.KiDecorSous, out impactX, out impactY, Options.Instance.Casse.Percant(spawn.ProjectileDescriptor?.Id ?? Guid.Empty)))
		{
			spawn.KiImpacted = true;
			spawn.Anim.Hide();
			Live.Remove(spawn);
			Impact(sprite, palette, impactX, impactY, spawn.KiHeadingX, spawn.KiHeadingY, null, ownerId, spawn, milliseconds, surLeDecor: true);
			return;
		}
		Guid? obj = Globals.Me?.Id;
		if (ownerId == obj)
		{
			Entity entity = FindContact(x, y, ownerId, targetId, 14f);
			if (entity != null)
			{
				spawn.KiImpacted = true;
				spawn.Anim.Hide();
				Live.Remove(spawn);
				FloatRect worldPos = entity.WorldPos;
				float num5 = (x + worldPos.X + worldPos.Width / 2f) / 2f;
				float y3 = (y + worldPos.Y + worldPos.Height - 16f) / 2f;
				float x3 = num5;
				Impact(sprite, palette, x3, y3, spawn.KiHeadingX, spawn.KiHeadingY, entity, ownerId, spawn, milliseconds);
			}
		}
	}

	public static void OnEnd(ProjectileSpawns spawn, Guid ownerId, Guid targetId, Guid? touche = null)
	{
		Live.Remove(spawn);
		if (!spawn.KiStarted || spawn.KiImpacted)
		{
			return;
		}
		spawn.KiImpacted = true;
		string sprite = SpriteOf(spawn);
		Palette palette = PaletteOf(sprite);
		long milliseconds = Timing.Global.Milliseconds;
		if (touche.HasValue)
		{
			Guid valueOrDefault = touche.GetValueOrDefault();
			if (valueOrDefault != Guid.Empty && Globals.Entities.TryGetValue(valueOrDefault, out Entity value) && value != null && !value.IsDisposed)
			{
				FloatRect worldPos = value.WorldPos;
				float num = (spawn.KiX + worldPos.X + worldPos.Width / 2f) / 2f;
				float y = (spawn.KiY + worldPos.Y + worldPos.Height - 16f) / 2f;
				float x = num;
				Impact(sprite, palette, x, y, spawn.KiHeadingX, spawn.KiHeadingY, value, ownerId, spawn, milliseconds);
				return;
			}
		}
		if (!touche.HasValue)
		{
			Entity entity = FindContact(spawn.KiX, spawn.KiY, ownerId, targetId, 24f);
			if (entity != null)
			{
				Impact(sprite, palette, spawn.KiX, spawn.KiY, spawn.KiHeadingX, spawn.KiHeadingY, entity, ownerId, spawn, milliseconds);
				return;
			}
		}
		if (spawn.ProjectileDescriptor != null && spawn.Distance >= spawn.ProjectileDescriptor.Range + spawn.RangeBonus)
		{
			Meteo.GerbeDeKi(spawn.KiX, spawn.KiY + 14f, 22f);
			Fizzle(sprite, palette, spawn.KiX, spawn.KiY, spawn.KiHeadingX, spawn.KiHeadingY, milliseconds);
		}
		else
		{
			var (x2, y2) = JusquAuMur(spawn);
			Impact(sprite, palette, x2, y2, spawn.KiHeadingX, spawn.KiHeadingY, null, ownerId, spawn, milliseconds, surLeDecor: true);
		}
	}

	private static (float X, float Y) JusquAuMur(ProjectileSpawns spawn)
	{
		float kiX = spawn.KiX;
		float kiY = spawn.KiY;
		float kiHeadingX = spawn.KiHeadingX;
		float kiHeadingY = spawn.KiHeadingY;
		if (Casse.PremierImpact(kiX, kiY, kiX + kiHeadingX * 64f, kiY + kiHeadingY * 64f, spawn.KiDecorSous, out var impactX, out var impactY))
		{
			return (X: impactX, Y: impactY);
		}
		for (float num = 0f; num <= 64f; num += 4f)
		{
			float num2 = kiX + kiHeadingX * num;
			float num3 = kiY + kiHeadingY * num;
			if (Casse.CaseBloquee(num2, num3 + 14f))
			{
				return (X: num2, Y: num3);
			}
		}
		return (X: kiX, Y: kiY);
	}

	public static void OnClash(KiClashPacket packet)
	{
		long milliseconds = Timing.Global.Milliseconds;
		(Projectile? Projectile, ProjectileSpawns? Spawn) tuple = FindSpawn(packet.FirstProjectileId, packet.FirstSpawn);
		Projectile item = tuple.Projectile;
		ProjectileSpawns item2 = tuple.Spawn;
		(Projectile? Projectile, ProjectileSpawns? Spawn) tuple2 = FindSpawn(packet.SecondProjectileId, packet.SecondSpawn);
		Projectile item3 = tuple2.Projectile;
		ProjectileSpawns item4 = tuple2.Spawn;
		bool flag = item2 != null && item2.KiStarted && !item2.KiImpacted;
		bool flag2 = item4 != null && item4.KiStarted && !item4.KiImpacted;
		float num;
		float num2;
		if (flag && flag2)
		{
			num = (item2.KiX + item4.KiX) / 2f;
			num2 = (item2.KiY + item4.KiY) / 2f;
		}
		else
		{
			MapInstance mapInstance = MapInstance.Get(packet.MapId);
			if (mapInstance == null)
			{
				return;
			}
			num = (float)mapInstance.X + packet.X * (float)Options.Instance.Map.TileWidth + (float)Options.Instance.Map.TileWidth / 2f;
			num2 = (float)mapInstance.Y + packet.Y * (float)Options.Instance.Map.TileHeight + (float)Options.Instance.Map.TileHeight / 2f;
		}
		float num3 = 0f;
		float num4 = 1f;
		if (flag && flag2 && Math.Abs(item4.KiX - item2.KiX) + Math.Abs(item4.KiY - item2.KiY) > 2f)
		{
			float num5 = item4.KiX - item2.KiX;
			num3 = item4.KiY - item2.KiY;
			num4 = num5;
		}
		else if (flag)
		{
			float kiHeadingX = item2.KiHeadingX;
			num3 = item2.KiHeadingY;
			num4 = kiHeadingX;
		}
		else if (flag2)
		{
			float num6 = 0f - item4.KiHeadingX;
			num3 = 0f - item4.KiHeadingY;
			num4 = num6;
		}
		bool flag3 = Math.Abs(num3) > Math.Abs(num4);
		bool flag4 = (flag3 ? (num3 > 0f) : (num4 > 0f));
		string text = ((item2 != null) ? SpriteOf(item2) : ((item4 != null) ? SpriteOf(item4) : string.Empty));
		string text2 = ((item4 != null) ? SpriteOf(item4) : text);
		Palette palette = PaletteOf(text);
		Palette palette2 = PaletteOf(text2);
		Palette palette3 = palette;
		string sprite;
		string sprite2;
		if (!flag4)
		{
			string text3 = text;
			sprite = text3;
			sprite2 = text2;
		}
		else
		{
			string text3 = text2;
			sprite = text3;
			sprite2 = text;
		}
		Palette trailingPalette;
		Palette leadingPalette;
		if (!flag4)
		{
			Palette palette4 = palette3;
			trailingPalette = palette4;
			leadingPalette = palette2;
		}
		else
		{
			Palette palette4 = palette2;
			trailingPalette = palette4;
			leadingPalette = palette3;
		}
		switch (packet.Outcome)
		{
		case KiClashOutcome.Tie:
			Silence(item2);
			Silence(item4);
			break;
		case KiClashOutcome.FirstWins:
			Silence(item4);
			Weaken(item2, packet.WinnerStrength, milliseconds);
			break;
		case KiClashOutcome.SecondWins:
			Silence(item2);
			Weaken(item4, packet.WinnerStrength, milliseconds);
			break;
		}
		Clashes.Add(new Clash(ClashTexture(sprite2), ClashTexture(sprite), MathF.Round(num), MathF.Round(num2), flag3, milliseconds, leadingPalette, trailingPalette, packet.MapId, (int)MathF.Ceiling(packet.Y), Noise.Next()));
		if (Clashes.Count > 16)
		{
			Clashes.RemoveAt(0);
		}
		int x = (int)MathF.Round(packet.X);
		int y = (int)MathF.Round(packet.Y);
		MapSound mapSound = Audio.AddMapSound("explosion.wav", x, y, packet.MapId, loop: false, 0, 16);
		if (mapSound != null)
		{
			mapSound.Pitch = -0.3f;
		}
		MapSound mapSound2 = Audio.AddMapSound("kiblast.wav", x, y, packet.MapId, loop: false, 0, 16);
		if (mapSound2 != null)
		{
			mapSound2.Pitch = 0.2f;
		}
		Player me = Globals.Me;
		if (me == null)
		{
			return;
		}
		float dirY;
		float dirX;
		if (!flag3)
		{
			dirY = 0f;
			dirX = 1f;
		}
		else
		{
			float num7 = 1f;
			dirY = num7;
			dirX = 0f;
		}
		if (item?.OwnerId == me.Id || item3?.OwnerId == me.Id)
		{
			CombatFeedback.HitStop(45L);
			CombatFeedback.StartShake(3f, 220L, dirX, dirY);
			return;
		}
		FloatRect worldPos = me.WorldPos;
		float num8 = (worldPos.X + worldPos.Width / 2f - num) / (float)Options.Instance.Map.TileWidth;
		float num9 = (worldPos.Y + worldPos.Height / 2f - num2) / (float)Options.Instance.Map.TileHeight;
		if (num8 * num8 + num9 * num9 <= 36f)
		{
			CombatFeedback.StartShake(1.6f, 160L, dirX, dirY);
		}
	}

	private static void Silence(ProjectileSpawns? spawn)
	{
		if (spawn != null)
		{
			spawn.KiImpacted = true;
			spawn.Anim?.Hide();
			Live.Remove(spawn);
		}
	}

	internal static void AddLiveLights(List<(float X, float Y, Color Color, float Strength)> lights)
	{
		foreach (var (projectileSpawns2, palette2) in Live)
		{
			if (projectileSpawns2.KiStarted && !projectileSpawns2.KiImpacted && projectileSpawns2.Anim != null && !projectileSpawns2.Anim.IsDisposed && !projectileSpawns2.Anim.Hidden)
			{
				lights.Add((projectileSpawns2.KiX, projectileSpawns2.KiY + 10f, palette2.Light, 0.5f + 0.5f * projectileSpawns2.KiStrength));
			}
		}
	}

	private static void Weaken(ProjectileSpawns? spawn, float strength, long now)
	{
		if (spawn != null && !spawn.KiImpacted)
		{
			spawn.KiStrength = Math.Clamp(strength, 0.2f, 1f);
			spawn.KiClashedAt = now;
		}
	}

	private static (Projectile? Projectile, ProjectileSpawns? Spawn) FindSpawn(Guid projectileId, int spawnIndex)
	{
		if (!Globals.Entities.TryGetValue(projectileId, out Entity value) || !(value is Projectile projectile))
		{
			return (Projectile: null, Spawn: null);
		}
		return (Projectile: projectile, Spawn: projectile.SpawnAt(spawnIndex));
	}

	private static string ClashTexture(string sprite)
	{
		string text = Companion(sprite, "choc");
		if (string.IsNullOrEmpty(text) || Globals.ContentManager?.GetTexture(TextureType.Animation, text) == null)
		{
			return Companion(string.IsNullOrEmpty(sprite) ? "boule_energie.png" : sprite, "impact");
		}
		return text;
	}

	private static void Launch(ProjectileSpawns spawn, string sprite, Palette palette, float x, float y, Guid ownerId, long now)
	{
		float kiHeadingX = spawn.KiHeadingX;
		float kiHeadingY = spawn.KiHeadingY;
		float num = kiHeadingX;
		float x2 = x + num * 12f;
		float y2 = y + kiHeadingY * 12f;
		Flashes.Add(new Flash(Companion(sprite, "depart"), x2, y2, num, kiHeadingY, now));
		if (Flashes.Count > 32)
		{
			Flashes.RemoveAt(0);
		}
		for (int i = 0; i < 8; i++)
		{
			float x3 = MathF.Atan2(kiHeadingY, num) + ((float)Noise.NextDouble() - 0.5f) * 1.3f;
			float num2 = 55f + (float)Noise.NextDouble() * 75f;
			AddMote(x2, y2, MathF.Cos(x3) * num2, MathF.Sin(x3) * num2, now, 150 + Noise.Next(130), Pick(palette, 0.35f), (i % 3 == 0) ? 2f : 1f, front: false);
		}
		if (Globals.Entities.TryGetValue(ownerId, out Entity value) && value != null)
		{
			CombatAudio.Play("ki_depart.wav", value, 0.06f);
			OnLaunchGesture(value, num, kiHeadingY, now);
			if (value == Globals.Me)
			{
				CombatFeedback.StartShake(1f, 80L, 0f - num, 0f - kiHeadingY);
			}
		}
		else
		{
			Audio.AddMapSound("ki_depart.wav", spawn.X, spawn.Y, spawn.MapId, loop: false, 0, 12);
		}
	}

	private static void Trail(float fromX, float fromY, float toX, float toY, float hx, float hy, float distance, Palette palette, long now)
	{
		int num = Math.Min(8, (int)(distance / 3f) + 1);
		for (int i = 0; i < num; i++)
		{
			float num2 = ((float)i + (float)Noise.NextDouble()) / (float)num;
			float num3 = ((float)Noise.NextDouble() - 0.5f) * 4f;
			float x = fromX + (toX - fromX) * num2 - hy * num3;
			float y = fromY + (toY - fromY) * num2 + hx * num3;
			float num4 = 8f + (float)Noise.NextDouble() * 18f;
			float num5 = ((float)Noise.NextDouble() - 0.5f) * 26f;
			float size = ((Noise.NextDouble() < 0.35) ? 2f : 1f);
			AddMote(x, y, (0f - hx) * num4 - hy * num5, (0f - hy) * num4 + hx * num5, now, 170 + Noise.Next(150), Pick(palette, 0.1f), size, front: false);
		}
	}

	private static void Impact(string sprite, Palette palette, float x, float y, float hx, float hy, Entity? struck, Guid ownerId, ProjectileSpawns spawn, long now, bool surLeDecor = false)
	{
		bool flag = Heritage.Actif && Heritage.Garde.NoterLaBoule(struck, palette.Light, palette.Mid, palette.Dark, now);
		if (Heritage.Actif && struck != null && !surLeDecor && RetenirLEclatHeritage(sprite, palette, x, y, hx, hy, struck, ownerId, spawn, now))
		{
			return;
		}
		if (!surLeDecor)
		{
			OnImpactGround(x, y, hx, hy, struck, ownerId, now);
		}
		KiLight.AddFlash(x, y + 14f, palette.Light);
		if (!Heritage.Actif || !Heritage.Boule.EclatDouble(Companion(sprite, "impact"), x, y, now))
		{
			Bursts.Add(new Burst(Companion(sprite, "impact"), x, y, now, 0, 255, palette.Light, Ring: true));
		}
		if (Bursts.Count > 32)
		{
			Bursts.RemoveAt(0);
		}
		for (int i = 0; i < 14; i++)
		{
			float x2 = (float)(Noise.NextDouble() * Math.PI * 2.0);
			float num = 45f + (float)Noise.NextDouble() * 95f;
			AddMote(x, y, MathF.Cos(x2) * num + hx * 30f, MathF.Sin(x2) * num + hy * 30f, now, 220 + Noise.Next(200), Pick(palette, 0.25f), (i % 4 == 0) ? 2f : 1f, front: true);
		}
		if (struck != null)
		{
			if (!flag)
			{
				CombatAudio.Play("ki_impact.wav", struck);
			}
		}
		else
		{
			Audio.AddMapSound("ki_impact.wav", spawn.X, spawn.Y, spawn.MapId, loop: false, 0, 12);
		}
		Player me = Globals.Me;
		if (struck != null && me != null && (ownerId == me.Id || struck == me))
		{
			CombatFeedback.MarkPredictedHit(struck.Id);
			CombatFeedback.Impact((struck == me) ? CombatFeedback.Poids.Recu : CombatFeedback.Poids.Coup, hx, hy, 0f, 2.2f);
		}
	}

	private static void Fizzle(string sprite, Palette palette, float x, float y, float hx, float hy, long now)
	{
		Bursts.Add(new Burst(Companion(sprite, "impact"), x, y, now, 3, 190, palette.Light, Ring: false));
		KiLight.AddFlash(x, y + 14f, palette.Light, small: true);
		for (int i = 0; i < 7; i++)
		{
			float x2 = MathF.Atan2(hy, hx) + ((float)Noise.NextDouble() - 0.5f) * 2.4f;
			float num = 20f + (float)Noise.NextDouble() * 40f;
			AddMote(x, y, MathF.Cos(x2) * num, MathF.Sin(x2) * num, now, 200 + Noise.Next(150), Pick(palette, 0.1f), 1f, front: true);
		}
	}

	private static Entity? FindContact(float x, float y, Guid ownerId, Guid targetId, float reach)
	{
		Entity result = null;
		float num = float.MaxValue;
		foreach (Entity value in Globals.Entities.Values)
		{
			bool flag = value != null && value.IsDying && value.KiKillHeadingX * value.KiKillHeadingX + value.KiKillHeadingY * value.KiKillHeadingY > 0.1f;
			bool flag2 = value == null || value.IsDisposed || (value.IsDying && !flag) || value.Id == ownerId;
			bool flag3 = flag2;
			if (!flag3)
			{
				bool flag4 = ((value is Projectile || value is Resource) ? true : false);
				flag3 = flag4;
			}
			if (flag3)
			{
				continue;
			}
			bool flag5 = value.Type == EntityType.GlobalEntity && !(value is Critter);
			bool flag6 = flag5;
			if (flag6)
			{
				NpcAggression aggression = value.Aggression;
				bool flag4 = (uint)(aggression - 3) <= 1u;
				flag6 = !flag4;
			}
			bool flag7 = flag6;
			if (value.Id != targetId && (!flag7 || !value.CanBeAttacked))
			{
				continue;
			}
			FloatRect worldPos = value.WorldPos;
			float num2 = x - (worldPos.X + worldPos.Width / 2f);
			float num3 = y - (worldPos.Y + worldPos.Height - 16f);
			if (!(Math.Abs(num2) > reach) && !(Math.Abs(num3) > reach))
			{
				float num4 = num2 * num2 + num3 * num3;
				if (num4 < num)
				{
					num = num4;
					result = value;
				}
			}
		}
		return result;
	}

	private static (float X, float Y) InitialHeading(ProjectileSpawns spawn, float x, float y, Guid targetId)
	{
		Entity value = default(Entity);
		bool flag = targetId != Guid.Empty && Globals.Entities.TryGetValue(targetId, out value) && value != null;
		bool flag2 = flag;
		if (flag2)
		{
			ProjectileDescriptor projectileDescriptor = spawn.ProjectileDescriptor;
			bool flag3 = ((projectileDescriptor != null && (projectileDescriptor.HomingBehavior || projectileDescriptor.DirectShotBehavior)) ? true : false);
			flag2 = flag3;
		}
		if (flag2)
		{
			FloatRect worldPos = value.WorldPos;
			float num = worldPos.X + worldPos.Width / 2f - x;
			float num2 = worldPos.Y + worldPos.Height - 16f - y;
			float num3 = MathF.Sqrt(num * num + num2 * num2);
			if (num3 > 1f)
			{
				return (X: num / num3, Y: num2 / num3);
			}
		}
		return spawn.Dir switch
		{
			Direction.Up => (X: 0f, Y: -1f), 
			Direction.Down => (X: 0f, Y: 1f), 
			Direction.Left => (X: -1f, Y: 0f), 
			Direction.Right => (X: 1f, Y: 0f), 
			Direction.UpLeft => (X: -0.7071f, Y: -0.7071f), 
			Direction.UpRight => (X: 0.7071f, Y: -0.7071f), 
			Direction.DownLeft => (X: -0.7071f, Y: 0.7071f), 
			Direction.DownRight => (X: 0.7071f, Y: 0.7071f), 
			_ => (X: 0f, Y: -1f), 
		};
	}

	private static string SpriteOf(ProjectileSpawns spawn)
	{
		AnimationDescriptor animationDescriptor = spawn.Anim?.Descriptor;
		if (animationDescriptor == null)
		{
			return string.Empty;
		}
		string sprite;
		if (string.IsNullOrEmpty(animationDescriptor.Upper.Sprite))
		{
			sprite = animationDescriptor.Lower.Sprite;
			if (sprite == null)
			{
				return string.Empty;
			}
		}
		else
		{
			sprite = animationDescriptor.Upper.Sprite;
		}
		return sprite;
	}

	private static string Companion(string sprite, string suffix)
	{
		if (!string.IsNullOrEmpty(sprite))
		{
			return Path.GetFileNameWithoutExtension(sprite) + "_" + suffix + Path.GetExtension(sprite);
		}
		return string.Empty;
	}

	private static Palette PaletteOf(string sprite)
	{
		if (string.IsNullOrEmpty(sprite))
		{
			return Warm;
		}
		if (Palettes.TryGetValue(sprite, out Palette value))
		{
			return value;
		}
		IGameTexture gameTexture = Globals.ContentManager?.GetTexture(TextureType.Animation, sprite);
		if (gameTexture == null || !PixelsPlanche.Lire(gameTexture, out Color[] pixels) || pixels.Length < gameTexture.Width * gameTexture.Height)
		{
			return Warm;
		}
		Dictionary<int, int> dictionary = new Dictionary<int, int>();
		Color[] array = pixels;
		foreach (Color color in array)
		{
			if (!(color == null) && color.A >= 200)
			{
				int key = (color.R << 16) | (color.G << 8) | color.B;
				dictionary[key] = ((!dictionary.TryGetValue(key, out var value2)) ? 1 : (value2 + 1));
			}
		}
		List<Color> list = (from entry in dictionary.OrderByDescending((KeyValuePair<int, int> entry) => entry.Value).Take(4)
			select new Color(255, (entry.Key >> 16) & 0xFF, (entry.Key >> 8) & 0xFF, entry.Key & 0xFF) into color2
			orderby 0.299f * (float)(int)color2.R + 0.587f * (float)(int)color2.G + 0.114f * (float)(int)color2.B descending
			select color2).ToList();
		Palette palette = list.Count switch
		{
			0 => Warm, 
			1 => new Palette(list[0], list[0], list[0], list[0]), 
			2 => new Palette(list[0], list[0], list[1], list[1]), 
			3 => new Palette(list[0], list[1], list[1], list[2]), 
			_ => new Palette(list[0], list[1], list[2], list[3]), 
		};
		Palettes[sprite] = palette;
		return palette;
	}

	private static Color Pick(Palette palette, float core)
	{
		float num = (float)Noise.NextDouble();
		if (num < core)
		{
			return palette.Core;
		}
		num = (num - core) / Math.Max(0.001f, 1f - core);
		if (!(num < 0.45f))
		{
			if (!(num < 0.8f))
			{
				return palette.Dark;
			}
			return palette.Mid;
		}
		return palette.Light;
	}

	private static void AddMote(float x, float y, float vx, float vy, long now, long life, Color color, float size, bool front)
	{
		Motes.Add(new Mote(x, y, vx, vy, now, life, color, size, front));
		if (Motes.Count > 700)
		{
			Motes.RemoveAt(0);
		}
	}

	public static void DrawBehind()
	{
		long milliseconds = Timing.Global.Milliseconds;
		IGameTexture gameTexture = Globals.ContentManager?.GetTexture(TextureType.Misc, "ki_halo.png");
		if (gameTexture != null)
		{
			foreach (var (projectileSpawns2, palette2) in Live)
			{
				if (projectileSpawns2.KiStarted && !projectileSpawns2.KiImpacted && projectileSpawns2.Anim != null && !projectileSpawns2.Anim.IsDisposed && !projectileSpawns2.Anim.Hidden && !DessineeDansLesRangees(projectileSpawns2.Anim))
				{
					DessinerLeHalo(gameTexture, projectileSpawns2, palette2, milliseconds);
				}
			}
		}
		DrawMotes(front: false, milliseconds);
		DrawFlashes(milliseconds);
	}

	public static void DrawFront()
	{
		long milliseconds = Timing.Global.Milliseconds;
		DrawBursts(milliseconds);
		DrawMotes(front: true, milliseconds);
	}

	private static void DrawFlashes(long now)
	{
		for (int num = Flashes.Count - 1; num >= 0; num--)
		{
			Flash flash = Flashes[num];
			long num2 = now - flash.StartedAt;
			if (num2 >= 130)
			{
				Flashes.RemoveAt(num);
			}
			else
			{
				IGameTexture gameTexture = Globals.ContentManager?.GetTexture(TextureType.Animation, flash.Texture);
				if (gameTexture != null)
				{
					float num3 = (float)num2 / 130f;
					int a = ((num3 < 0.45f) ? 255 : ((int)(255f * (1f - (num3 - 0.45f) / 0.55f))));
					float num4 = 5f * num3;
					float rotationDegrees = MathF.Atan2(flash.HeadingY, flash.HeadingX) * 180f / (float)Math.PI + 90f;
					Graphics.DrawGameTexture(gameTexture, new FloatRect(0f, 0f, gameTexture.Width, gameTexture.Height), new FloatRect(MathF.Round(flash.X + flash.HeadingX * num4 - (float)gameTexture.Width / 2f), MathF.Round(flash.Y + flash.HeadingY * num4 - (float)gameTexture.Height / 2f), gameTexture.Width, gameTexture.Height), new Color(a, 255, 255, 255), null, GameBlendModes.None, null, rotationDegrees);
				}
			}
		}
	}

	public static void DrawClashes(int row, int lastRow)
	{
		if (Clashes.Count == 0)
		{
			return;
		}
		long milliseconds = Timing.Global.Milliseconds;
		IGameTexture gameTexture = Globals.ContentManager?.GetTexture(TextureType.Misc, "ki_halo.png");
		for (int num = Clashes.Count - 1; num >= 0; num--)
		{
			Clash clash = Clashes[num];
			long num2 = milliseconds - clash.StartedAt;
			IGameTexture texture = Globals.ContentManager?.GetTexture(TextureType.Animation, clash.LeadingTexture);
			IGameTexture texture2 = Globals.ContentManager?.GetTexture(TextureType.Animation, clash.TrailingTexture);
			int num3 = Math.Max(FrameCount(texture), FrameCount(texture2));
			if (num2 >= Math.Max(Math.Max((long)num3 * 55L, 200L), 480L))
			{
				Clashes.RemoveAt(num);
			}
			else
			{
				int num4 = RowOf(clash);
				if (num4 == row || (row == lastRow && (num4 < 0 || num4 > lastRow)))
				{
					DrawClashSparks(clash, num2);
					if (gameTexture != null && num2 < 200)
					{
						float num5 = (float)num2 / 200f;
						float num6;
						float num7;
						if (!clash.Vertical)
						{
							num6 = 0f;
							num7 = 9f;
						}
						else
						{
							float num8 = 9f;
							num6 = num8;
							num7 = 0f;
						}
						float size = 60f + 44f * num5;
						int a = (int)(230f * (1f - num5));
						Color light = clash.LeadingPalette.Light;
						Color light2 = clash.TrailingPalette.Light;
						Color color = light;
						DrawCentered(gameTexture, clash.X - num7, clash.Y - num6, size, new Color(a, color.R, color.G, color.B), GameBlendModes.Add);
						DrawCentered(gameTexture, clash.X + num7, clash.Y + num6, size, new Color(a, light2.R, light2.G, light2.B), GameBlendModes.Add);
					}
					int frame = (int)(num2 / 55);
					DrawClashHalf(texture, frame, clash, leading: true);
					DrawClashHalf(texture2, frame, clash, leading: false);
				}
			}
		}
	}

	private static int RowOf(Clash clash)
	{
		IMapInstance mapInstance = Globals.Me?.MapInstance;
		if (mapInstance == null || !MapInstance.TryGet(clash.MapId, out MapInstance instance) || instance == null)
		{
			return -1;
		}
		int num = instance.GridY - mapInstance.GridY;
		if ((num >= -1 && num <= 1) || 1 == 0)
		{
			return Options.Instance.Map.MapHeight * (2 + num) + clash.TileY;
		}
		return -1;
	}

	private static void DrawClashSparks(Clash clash, long age)
	{
		IGameTexture gameTexture = Graphics.Renderer?.WhitePixel;
		if (gameTexture == null || age >= 480)
		{
			return;
		}
		float num;
		float num2;
		if (!clash.Vertical)
		{
			num = 0f;
			num2 = 1f;
		}
		else
		{
			float num3 = 1f;
			num = num3;
			num2 = 0f;
		}
		Random random = new Random(clash.Seed);
		float num4 = (float)age / 1000f;
		float num5 = (1f - MathF.Exp(-5f * num4)) / 5f;
		for (int i = 0; i < 22; i++)
		{
			float num6 = ((i % 2 == 0) ? (-1f) : 1f);
			Palette palette = ((num6 < 0f) ? clash.LeadingPalette : clash.TrailingPalette);
			float num7 = ((random.NextDouble() < 0.5) ? (-1f) : 1f);
			float x = MathF.Atan2(num2 * num7, num * num7) + ((float)random.NextDouble() - 0.5f) * 1.5f;
			float num8 = 60f + (float)random.NextDouble() * 100f;
			int num9 = 240 + random.Next(220);
			float num10 = (float)random.NextDouble();
			if (age < num9)
			{
				float num11 = MathF.Cos(x) * num8 + num2 * num6 * 25f;
				float num12 = MathF.Sin(x) * num8 + num * num6 * 25f;
				float num13 = ((i % 3 == 0 && (float)age < (float)num9 * 0.6f) ? 2f : 1f);
				float num14 = (float)age / (float)num9;
				Color color = ((num10 < 0.3f) ? palette.Core : ((num10 < 0.6f) ? palette.Light : ((num10 < 0.85f) ? palette.Mid : palette.Dark)));
				Graphics.DrawGameTexture(gameTexture, new FloatRect(0f, 0f, 1f, 1f), new FloatRect(MathF.Round(clash.X + num2 * num6 * 5f + num11 * num5 - num13 / 2f), MathF.Round(clash.Y + num * num6 * 5f + num12 * num5 - num13 / 2f), num13, num13), new Color((int)(255f * (1f - num14 * num14)), color.R, color.G, color.B), null, GameBlendModes.Add);
			}
		}
	}

	private static int FrameCount(IGameTexture? texture)
	{
		if (texture == null || texture.Height <= 0)
		{
			return 0;
		}
		return Math.Max(1, texture.Width / texture.Height);
	}

	private static void DrawClashHalf(IGameTexture? texture, int frame, Clash clash, bool leading)
	{
		if (texture != null && frame < FrameCount(texture))
		{
			int height = texture.Height;
			int num = height / 2;
			FloatRect srcRectangle = new FloatRect(frame * height + ((!leading) ? num : 0), 0f, num, height);
			if (!clash.Vertical)
			{
				Graphics.DrawGameTexture(texture, srcRectangle, new FloatRect(leading ? (clash.X - (float)num) : clash.X, clash.Y - (float)num, num, height), Color.White);
				return;
			}
			float num2 = (leading ? (clash.Y - (float)height / 4f) : (clash.Y + (float)height / 4f));
			Graphics.DrawGameTexture(texture, srcRectangle, new FloatRect(MathF.Round(clash.X - (float)height / 4f), MathF.Round(num2 - (float)num), num, height), Color.White, null, GameBlendModes.None, null, 90f);
		}
	}

	private static void DrawBursts(long now)
	{
		IGameTexture gameTexture = Globals.ContentManager?.GetTexture(TextureType.Misc, "ki_halo.png");
		IGameTexture gameTexture2 = Globals.ContentManager?.GetTexture(TextureType.Misc, "hit_shockwave.png");
		for (int num = Bursts.Count - 1; num >= 0; num--)
		{
			Burst burst = Bursts[num];
			long num2 = now - burst.StartedAt;
			IGameTexture gameTexture3 = Globals.ContentManager?.GetTexture(TextureType.Animation, burst.Texture);
			int num3 = ((gameTexture3 == null || gameTexture3.Height <= 0) ? 1 : Math.Max(1, gameTexture3.Width / gameTexture3.Height));
			int num4 = num3 - burst.FirstFrame;
			long num5 = Math.Max((long)Math.Max(num4, 1) * 45L, burst.Ring ? 220 : 0);
			if (num2 >= num5)
			{
				Bursts.RemoveAt(num);
			}
			else
			{
				if (gameTexture != null && burst.FirstFrame == 0 && num2 < 150)
				{
					float num6 = (float)num2 / 150f;
					DrawCentered(gameTexture, burst.X, burst.Y, 40f + 30f * num6, new Color((int)(220f * (1f - num6)), burst.Glow.R, burst.Glow.G, burst.Glow.B), GameBlendModes.Add);
				}
				if (burst.Ring && gameTexture2 != null && num2 < 220)
				{
					float num7 = (float)num2 / 220f;
					float size = 14f + 46f * (1f - (1f - num7) * (1f - num7));
					DrawCentered(gameTexture2, burst.X, burst.Y, size, new Color((int)(200f * (1f - num7)), burst.Glow.R, burst.Glow.G, burst.Glow.B), GameBlendModes.Add);
				}
				if (gameTexture3 != null && num4 >= 1)
				{
					int num8 = burst.FirstFrame + (int)(num2 / 45);
					if (num8 < num3)
					{
						int height = gameTexture3.Height;
						Graphics.DrawGameTexture(gameTexture3, new FloatRect(num8 * height, 0f, height, height), new FloatRect(MathF.Round(burst.X - (float)height / 2f), MathF.Round(burst.Y - (float)height / 2f), height, height), new Color(burst.Alpha, 255, 255, 255));
					}
				}
			}
		}
	}

	private static void DrawMotes(bool front, long now)
	{
		IGameTexture gameTexture = Graphics.Renderer?.WhitePixel;
		if (Motes.Count == 0 || gameTexture == null)
		{
			return;
		}
		for (int num = Motes.Count - 1; num >= 0; num--)
		{
			Mote mote = Motes[num];
			long num2 = now - mote.StartedAt;
			if (num2 >= mote.Life)
			{
				Motes.RemoveAt(num);
			}
			else if (mote.Front == front && num2 >= 0)
			{
				float num3 = (float)num2 / 1000f;
				float num4 = (1f - MathF.Exp(-5f * num3)) / 5f;
				float num5 = (float)num2 / (float)mote.Life;
				float num6 = ((num5 > 0.6f && mote.Size > 1f) ? (mote.Size - 1f) : mote.Size);
				Graphics.DrawGameTexture(gameTexture, new FloatRect(0f, 0f, 1f, 1f), new FloatRect(MathF.Round(mote.X + mote.Vx * num4 - num6 / 2f), MathF.Round(mote.Y + mote.Vy * num4 - num6 / 2f), num6, num6), new Color((int)((float)(int)mote.Color.A * (1f - num5 * num5)), mote.Color.R, mote.Color.G, mote.Color.B), null, GameBlendModes.Add);
			}
		}
	}

	private static void DrawCentered(IGameTexture texture, float x, float y, float size, Color color, GameBlendModes blend)
	{
		Graphics.DrawGameTexture(texture, new FloatRect(0f, 0f, texture.Width, texture.Height), new FloatRect(x - size / 2f, y - size / 2f, size, size), color, null, blend);
	}

	private static bool RetenirLEclatHeritage(string sprite, Palette palette, float x, float y, float hx, float hy, Entity struck, Guid ownerId, ProjectileSpawns spawn, long now)
	{
		if (!Heritage.Boule.Retenir(struck, x, y, hx, hy, ownerId, now, delegate(float bx, float by, long quand)
		{
			Impact(sprite, palette, bx, by, hx, hy, struck, ownerId, spawn, quand);
		}))
		{
			return false;
		}
		NoterLeCoupRetenuHeritage(struck.Id, now, hx, hy, ownerId);
		return true;
	}

	private static void NoterLeCoupRetenuHeritage(Guid toucheId, long now, float hx, float hy, Guid ownerId)
	{
		if (LocalHits.Count > 64)
		{
			LocalHits.Clear();
		}
		LocalHits[toucheId] = (now, hx, hy, ownerId);
	}

	public static void PreparerLesRangees(int derniereRangee)
	{
		sARanger.Clear();
		sDessineesDansLesRangees.Clear();
		sDerniereRangee = derniereRangee;
		if (Live.Count == 0)
		{
			return;
		}
		IMapInstance mapInstance = Globals.Me?.MapInstance;
		if (mapInstance == null)
		{
			return;
		}
		int tileHeight = Options.Instance.Map.TileHeight;
		int mapHeight = Options.Instance.Map.MapHeight;
		foreach (var (projectileSpawns2, palette2) in Live)
		{
			if (projectileSpawns2.KiStarted && !projectileSpawns2.KiImpacted && projectileSpawns2.Anim != null && !projectileSpawns2.Anim.IsDisposed && !projectileSpawns2.Anim.Hidden)
			{
				float num = projectileSpawns2.KiY + 14f;
				int rangee = (int)MathF.Floor((num - (float)mapInstance.Y - 1f) / (float)tileHeight) + mapHeight * 2;
				sARanger.Add(new BouleRangee(projectileSpawns2, palette2, num, rangee));
			}
		}
		sARanger.Sort((BouleRangee a, BouleRangee b) => a.Sol.CompareTo(b.Sol));
	}

	public static void DessinerDansLaRangee(int rangee, float piedsSuivants, Guid corps = default(Guid))
	{
		if (sARanger.Count == 0)
		{
			return;
		}
		long milliseconds = Timing.Global.Milliseconds;
		IGameTexture gameTexture = Globals.ContentManager?.GetTexture(TextureType.Misc, "ki_halo.png");
		for (int i = 0; i < sARanger.Count; i++)
		{
			BouleRangee bouleRangee = sARanger[i];
			bool flag = bouleRangee.Rangee < 0 || bouleRangee.Rangee > sDerniereRangee;
			if ((bouleRangee.Rangee != rangee && !(rangee == sDerniereRangee && flag)) || (bouleRangee.Sol >= piedsSuivants && (!flag || piedsSuivants != float.MaxValue)) || (corps != default(Guid) && bouleRangee.Spawn.KiTireur == corps))
			{
				continue;
			}
			sARanger.RemoveAt(i--);
			Animation anim = bouleRangee.Spawn.Anim;
			if (anim != null && !anim.IsDisposed)
			{
				if (gameTexture != null)
				{
					DessinerLeHalo(gameTexture, bouleRangee.Spawn, bouleRangee.Palette, milliseconds);
				}
				anim.Draw(upper: false, alternate: true);
				anim.Draw(upper: true, alternate: true);
				sDessineesDansLesRangees.Add(anim);
			}
		}
	}

	public static bool DessineeDansLesRangees(Animation animation)
	{
		return sDessineesDansLesRangees.Contains(animation);
	}

	private static void DessinerLeHalo(IGameTexture halo, ProjectileSpawns spawn, Palette palette, long now)
	{
		float num = MathF.Sin((float)now / 45f + (float)(spawn.GetHashCode() % 7));
		float num2 = 0.5f + 0.5f * spawn.KiStrength;
		float num3 = (28f + 3f * num) * (0.1f + 0.9f * num2);
		DrawCentered(halo, spawn.KiX, spawn.KiY, num3, new Color((int)(150f * num2), palette.Light.R, palette.Light.G, palette.Light.B), GameBlendModes.Add);
		DrawCentered(halo, spawn.KiX, spawn.KiY, num3 * 0.42f, new Color((int)(120f * num2), palette.Core.R, palette.Core.G, palette.Core.B), GameBlendModes.Add);
	}

	private static void OnLaunchGesture(Entity owner, float headingX, float headingY, long now)
	{
		if (Recoils.Count > 64)
		{
			Recoils.Clear();
		}
		Recoils[owner.Id] = (now, 0f - headingX, 0f - headingY);
		if (!(owner.MapInstance is MapInstance { IsIndoors: not false }) && !owner.IsDying)
		{
			Vector2 origin = owner.Origin;
			SprintDust.AddPuff(origin.X, origin.Y - 2f, Direction.Right);
			SprintDust.AddPuff(origin.X, origin.Y - 2f, Direction.Left);
		}
	}

	public static (float X, float Y) LaunchRecoil(Entity entity)
	{
		if (Recoils.Count == 0 || !Recoils.TryGetValue(entity.Id, out (long, float, float) value))
		{
			return (X: 0f, Y: 0f);
		}
		long num = Timing.Global.Milliseconds - value.Item1;
		if (num < 0 || num >= 160)
		{
			if (num >= 160)
			{
				Recoils.Remove(entity.Id);
			}
			return (X: 0f, Y: 0f);
		}
		float num2 = 2.5f * MathF.Sin((float)Math.PI * (float)num / 160f);
		return (X: value.Item2 * num2, Y: value.Item3 * num2);
	}

	private static void OnImpactGround(float x, float y, float headingX, float headingY, Entity? struck, Guid ownerId, long now)
	{
		float num;
		float x3;
		if (struck != null && struck.LatestMap != null)
		{
			Vector2 origin = struck.Origin;
			float x2 = origin.X;
			num = origin.Y - 3f;
			x3 = x2;
		}
		else
		{
			num = y + 14f;
			x3 = x;
		}
		if (!Meteo.GerbeDeKi(x3, num))
		{
			Scorches.Add(new Scorch
			{
				X = x3,
				Y = num,
				StartedAt = now,
				Variant = Noise.Next(3),
				Seed = Noise.Next()
			});
			if (Scorches.Count > 24)
			{
				Scorches.RemoveAt(0);
			}
			for (int i = 0; i < 8; i++)
			{
				float vx = headingX * 40f + ((float)Noise.NextDouble() - 0.5f) * 80f;
				float vy = 0f - (60f + (float)Noise.NextDouble() * 90f);
				Color color = EarthColors[Noise.Next(EarthColors.Length)];
				Chips.Add(new Chip(x3, num - 2f, vx, vy, now, 420 + Noise.Next(160), color, (!(Noise.NextDouble() < 0.3)) ? 1 : 2));
			}
			if (Chips.Count > 96)
			{
				Chips.RemoveRange(0, Chips.Count - 96);
			}
			WorldMarks.OnKiImpact(x3, num, big: false);
		}
		if (struck != null)
		{
			if (LocalHits.Count > 64)
			{
				LocalHits.Clear();
			}
			LocalHits[struck.Id] = (now, headingX, headingY, ownerId);
		}
	}

	public static bool OnKiKill(Entity entity, long now)
	{
		if (!LocalHits.TryGetValue(entity.Id, out (long, float, float, Guid) value) || now - value.Item1 > 700)
		{
			(float, float, Guid)? tuple = FindBallNear(entity);
			if (!tuple.HasValue)
			{
				return false;
			}
			(float, float, Guid) valueOrDefault = tuple.GetValueOrDefault();
			value = (now, valueOrDefault.Item1, valueOrDefault.Item2, valueOrDefault.Item3);
		}
		LocalHits.Remove(entity.Id);
		entity.KiKillHeadingX = value.Item2;
		entity.KiKillHeadingY = value.Item3;
		entity.DeathDurationMs = 950L;
		Guid item = value.Item4;
		Guid? obj = Globals.Me?.Id;
		if (item == obj)
		{
			CombatFeedback.SlowMotion(280L, 0.6f);
			CombatFeedback.PunchZoom(0.035f, 420L);
			CombatFeedback.StartShake(3f, 260L, value.Item2, value.Item3);
		}
		Audio.AddMapSound("ki_fatal.wav", entity.X, entity.Y, entity.MapId, loop: false, 0, 16);
		Vector2 feet = entity.Origin;
		Scorch scorch = Scorches.LastOrDefault((Scorch s) => MathF.Abs(s.X - feet.X) < 28f && MathF.Abs(s.Y - (feet.Y - 3f)) < 20f && now - s.StartedAt < 1200);
		if (scorch == null)
		{
			scorch = new Scorch
			{
				X = feet.X,
				Y = feet.Y - 3f,
				StartedAt = now,
				Seed = Noise.Next()
			};
			Scorches.Add(scorch);
		}
		scorch.Fatal = true;
		scorch.FatalAt = now;
		WorldMarks.OnKiImpact(feet.X, feet.Y - 3f, big: true);
		FloatRect worldPos = entity.WorldPos;
		for (int num = 0; num < 30; num++)
		{
			int num2 = Noise.Next(450);
			float num3 = MathF.Min(1f, (float)num2 / 380f);
			float num4 = 48f * (1f - (1f - num3) * (1f - num3));
			float x = worldPos.X + 6f + (float)Noise.NextDouble() * MathF.Max(1f, worldPos.Width - 12f) + value.Item2 * num4;
			float y = worldPos.Y + worldPos.Height * 0.2f + (float)Noise.NextDouble() * worldPos.Height * 0.7f + value.Item3 * num4 * 0.6f;
			Color color = new Color(255, 255, 210 + Noise.Next(45), 120);
			AddMote(x, y, ((float)Noise.NextDouble() - 0.5f) * 30f, 0f - (90f + (float)Noise.NextDouble() * 90f), now + num2, 520 + Noise.Next(320), color, (num % 5 == 0) ? 2f : 1f, front: true);
		}
		return true;
	}

	private static (float X, float Y, Guid Owner)? FindBallNear(Entity entity)
	{
		FloatRect worldPos = entity.WorldPos;
		float num = worldPos.X + worldPos.Width / 2f;
		float num2 = worldPos.Y + worldPos.Height - 16f;
		foreach (Entity value in Globals.Entities.Values)
		{
			if (!(value is Projectile projectile) || projectile.OwnerId == entity.Id)
			{
				continue;
			}
			for (int i = 0; i < 8; i++)
			{
				ProjectileSpawns projectileSpawns = projectile.SpawnAt(i);
				if (projectileSpawns != null && projectileSpawns.KiStarted)
				{
					float num3 = projectileSpawns.KiX - num;
					float num4 = projectileSpawns.KiY - num2;
					if (num3 * num3 + num4 * num4 <= 2304f)
					{
						return (projectileSpawns.KiHeadingX, projectileSpawns.KiHeadingY, projectile.OwnerId);
					}
				}
			}
		}
		return null;
	}

	public static FloatRect KillProjection(Entity entity, FloatRect rectangle)
	{
		if (!entity.IsDying || entity.KiKillHeadingX * entity.KiKillHeadingX + entity.KiKillHeadingY * entity.KiKillHeadingY < 0.1f)
		{
			return rectangle;
		}
		float num = Math.Clamp((float)(Timing.Global.Milliseconds - entity.DyingAt) / 380f, 0f, 1f);
		float num2 = 48f * (1f - (1f - num) * (1f - num));
		return new FloatRect(MathF.Round(rectangle.X + entity.KiKillHeadingX * num2), MathF.Round(rectangle.Y + entity.KiKillHeadingY * num2 * 0.6f), rectangle.Width, rectangle.Height);
	}

	public static void DrawGround()
	{
		long milliseconds = Timing.Global.Milliseconds;
		DrawFlightGround(milliseconds);
		DrawScorches(milliseconds);
		DrawChips(milliseconds);
	}

	private static void DrawFlightGround(long now)
	{
		if (Live.Count == 0)
		{
			return;
		}
		IGameTexture gameTexture = Globals.ContentManager?.GetTexture(TextureType.Misc, "ki_halo.png");
		if (gameTexture == null)
		{
			return;
		}
		FloatRect srcRectangle = new FloatRect(0f, 0f, gameTexture.Width, gameTexture.Height);
		foreach (var (projectileSpawns2, palette2) in Live)
		{
			if (projectileSpawns2.KiStarted && !projectileSpawns2.KiImpacted && projectileSpawns2.Anim != null && !projectileSpawns2.Anim.IsDisposed && !projectileSpawns2.Anim.Hidden)
			{
				float kiX = projectileSpawns2.KiX;
				float num = projectileSpawns2.KiY + 14f;
				float num2 = MathF.Sin((float)now / 60f + (float)(projectileSpawns2.GetHashCode() % 5));
				float num3 = 0.5f + 0.5f * projectileSpawns2.KiStrength;
				if (!Meteo.KiSurLEau(projectileSpawns2, kiX, num, projectileSpawns2.KiHeadingX, projectileSpawns2.KiHeadingY, num3, palette2.Light))
				{
					Graphics.DrawGameTexture(gameTexture, srcRectangle, new FloatRect(MathF.Round(kiX - 17f), MathF.Round(num - 5.5f), 34f, 11f), new Color((int)((95f + 15f * num2) * num3), palette2.Light.R, palette2.Light.G, palette2.Light.B), null, GameBlendModes.Add);
					Graphics.DrawGameTexture(gameTexture, srcRectangle, new FloatRect(MathF.Round(kiX - 7f), MathF.Round(num - 2f), 14f, 5f), new Color((int)(90f * num3), 0, 0, 0));
				}
			}
		}
	}

	private static void DrawScorches(long now)
	{
		if (Scorches.Count == 0)
		{
			return;
		}
		Scorches.RemoveAll((Scorch scorch) => now - scorch.StartedAt >= (scorch.Fatal ? 4000 : 3000));
		IGameTexture gameTexture = Globals.ContentManager?.GetTexture(TextureType.Misc, "ki_brulure.png");
		IGameTexture gameTexture2 = Globals.ContentManager?.GetTexture(TextureType.Misc, "ki_brulure_grande.png");
		IGameTexture gameTexture3 = Globals.ContentManager?.GetTexture(TextureType.Misc, "ssj_cracks.png");
		IGameTexture gameTexture4 = Graphics.Renderer?.WhitePixel;
		foreach (Scorch scorch in Scorches)
		{
			long num = now - scorch.StartedAt;
			long num2 = (scorch.Fatal ? 4000 : 3000);
			float num3 = MathF.Min(1f, (float)num / 60f) * (Meteo.EstSable(scorch.X, scorch.Y) ? 0.45f : 1f);
			if (num > num2 - 800)
			{
				num3 = MathF.Min(num3, (float)(num2 - num) / 800f);
			}
			if (num3 <= 0f)
			{
				continue;
			}
			Color renderColor = new Color((int)(255f * num3), 255, 255, 255);
			if (scorch.Fatal && gameTexture2 != null)
			{
				Graphics.DrawGameTexture(gameTexture2, new FloatRect(0f, 0f, gameTexture2.Width, gameTexture2.Height), new FloatRect(MathF.Round(scorch.X - (float)gameTexture2.Width / 2f), MathF.Round(scorch.Y - (float)gameTexture2.Height / 2f), gameTexture2.Width, gameTexture2.Height), renderColor);
			}
			else if (gameTexture != null)
			{
				int num4 = gameTexture.Width / 3;
				Graphics.DrawGameTexture(gameTexture, new FloatRect(scorch.Variant * num4, 0f, num4, gameTexture.Height), new FloatRect(MathF.Round(scorch.X - (float)num4 / 2f), MathF.Round(scorch.Y - (float)gameTexture.Height / 2f), num4, gameTexture.Height), renderColor);
			}
			if (scorch.Fatal && gameTexture3 != null && gameTexture3.Height > 0)
			{
				int num5 = Math.Max(1, gameTexture3.Width / 64);
				int num6 = (int)Math.Min(num5 - 1, (now - scorch.FatalAt) * num5 / 700);
				Graphics.DrawGameTexture(gameTexture3, new FloatRect(num6 * 64, 0f, 64f, gameTexture3.Height), new FloatRect(MathF.Round(scorch.X - 32f), MathF.Round(scorch.Y - (float)gameTexture3.Height / 2f), 64f, gameTexture3.Height), renderColor);
			}
			if (gameTexture4 == null || num >= 1800)
			{
				continue;
			}
			float num7 = 1f - (float)num / 1800f;
			Random random = new Random(((int)(now / 90) * 131) ^ scorch.Seed);
			float num8 = (scorch.Fatal ? 24f : 15f);
			float num9 = (scorch.Fatal ? 9f : 6f);
			for (int num10 = 0; num10 < 14; num10++)
			{
				double num11 = random.NextDouble();
				int g = 140 + random.Next(80);
				float num12 = 0.5f + 0.5f * (float)random.NextDouble();
				if (!(num11 < 0.35))
				{
					float x = (float)num10 / 14f * (float)Math.PI * 2f;
					Graphics.DrawGameTexture(gameTexture4, new FloatRect(0f, 0f, 1f, 1f), new FloatRect(MathF.Round(scorch.X + MathF.Cos(x) * num8), MathF.Round(scorch.Y + MathF.Sin(x) * num9), 1f, 1f), new Color((int)(255f * num7 * num12 * num3), 255, g, 40), null, GameBlendModes.Add);
				}
			}
		}
	}

	private static void DrawChips(long now)
	{
		IGameTexture gameTexture = Graphics.Renderer?.WhitePixel;
		if (gameTexture == null || Chips.Count == 0)
		{
			return;
		}
		Chips.RemoveAll((Chip chip) => now - chip.StartedAt >= chip.Life);
		foreach (Chip chip in Chips)
		{
			float num = (float)(now - chip.StartedAt) / 1000f;
			float x = chip.X + chip.Vx * num;
			float x2 = MathF.Min(chip.Y + 2f, chip.Y + chip.Vy * num + 260f * num * num);
			float num2 = (float)(now - chip.StartedAt) / (float)chip.Life;
			float num3 = ((num2 < 0.6f) ? 1f : (1f - (num2 - 0.6f) / 0.4f));
			Graphics.DrawGameTexture(gameTexture, new FloatRect(0f, 0f, 1f, 1f), new FloatRect(MathF.Round(x), MathF.Round(x2), chip.Size, chip.Size), new Color((int)((float)(int)chip.Color.A * num3 * 0.9f), chip.Color.R, chip.Color.G, chip.Color.B));
		}
	}
}
