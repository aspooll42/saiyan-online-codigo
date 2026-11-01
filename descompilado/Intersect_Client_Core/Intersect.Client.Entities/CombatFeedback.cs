using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Intersect.Client.Core;
using Intersect.Client.Core.Sounds;
using Intersect.Client.Dungeons;
using Intersect.Client.Entities.Projectiles;
using Intersect.Client.Framework.Content;
using Intersect.Client.Framework.File_Management;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.Framework.Maps;
using Intersect.Client.General;
using Intersect.Client.Interface.Game;
using Intersect.Client.Localization;
using Intersect.Client.Maps;
using Intersect.Config;
using Intersect.Enums;
using Intersect.Framework.Core;
using Intersect.Framework.Core.Entities;
using Intersect.Framework.Core.GameObjects.NPCs;
using Intersect.Network.Packets.Server;

namespace Intersect.Client.Entities;

public static class CombatFeedback
{
	private enum LabelKind
	{
		Combo,
		Guard,
		Experience
	}

	private enum SparkKind
	{
		Normal,
		Critical,
		Finisher,
		Death
	}

	public enum Poids
	{
		Coup,
		Critique,
		Final,
		Recu
	}

	private struct PlaceDUnMot
	{
		public long NeA;

		public float Releve;

		public sbyte Cote;

		public float DecaleX;

		public long Vu;
	}

	internal readonly record struct MotLu(string Texte, int Taille, long Depuis);

	private const long LabelShownMs = 700L;

	private const long FlashMs = 110L;

	private const long PerfectFlashMs = 250L;

	private const long BreakShownMs = 400L;

	private const long RippleMs = 260L;

	private const long RecoilMs = 140L;

	private const long StanceBlendMs = 90L;

	private const long SparkMs = 180L;

	private const float ContactHeight = 0.58f;

	private const float ContactRadiusX = 0.32f;

	private const float ContactRadiusY = 0.14f;

	private const float SparkSquash = 0.8f;

	private const float SparkLift = 3f;

	private const float BehindThreshold = 0.1f;

	private const long ShockwaveMs = 320L;

	private const long HitShakeMs = 200L;

	private const long WhiteFlashMs = 70L;

	private const long TrailMs = 150L;

	public const long DeathMs = 450L;

	private const long DeathFlashMs = 110L;

	private static readonly List<(Guid Entity, Guid Attacker, SparkKind Kind, long StartedAt, float OffsetX, float OffsetY, float Size)> Sparks = new List<(Guid, Guid, SparkKind, long, float, float, float)>();

	private static readonly HashSet<Guid> DrawnSparks = new HashSet<Guid>();

	private static readonly List<(float X, float Y, float Vx, float Vy, long StartedAt, long Life, Color Color, float Size)> Particles = new List<(float, float, float, float, long, long, Color, float)>();

	private static readonly List<(float FromX, float FromY, float ToX, float ToY, long StartedAt, Color Color)> Trails = new List<(float, float, float, float, long, Color)>();

	private static readonly Dictionary<Guid, long> LastCritical = new Dictionary<Guid, long>();

	private static readonly Color Orange = new Color(255, 255, 122, 26);

	private static readonly Color Gold = new Color(255, 245, 190, 90);

	private static readonly Color Cyan = new Color(255, 150, 225, 255);

	private static readonly Color Red = new Color(255, 255, 90, 80);

	private static readonly Dictionary<(Guid Entity, LabelKind Kind), (string Text, Color Color, int Size, long ShownAt)> Labels = new Dictionary<(Guid, LabelKind), (string, Color, int, long)>();

	private static readonly Dictionary<Guid, long> NewestWords = new Dictionary<Guid, long>();

	private static readonly Random Noise = new Random();

	private static long _shakeStartedAt;

	private static long _shakeUntil;

	private static float _shakeStrength;

	private static float _shakeDirX;

	private static float _shakeDirY;

	private static long _zoomStartedAt;

	private static long _zoomUntil;

	private static float _zoomStrength;

	private static readonly Dictionary<Guid, long> PredictedHits = new Dictionary<Guid, long>();

	private static readonly Dictionary<Guid, (int Step, long At)> ComboSteps = new Dictionary<Guid, (int, long)>();

	private static long _lastLocalStrikeAt;

	private static Guid _lastLocalStrikeTarget;

	private static int _localComboStep;

	private static long _hitStopUntil;

	private static IFont? _font;

	private static long _slowMotionUntil;

	private static float _slowMotionFactor = 1f;

	private const long DureeMaxDUnEffetMs = 10000L;

	private static long _horlogeVue;

	internal static bool MotsMuets;

	private const long EcartArretsMs = 150L;

	private static long _dernierArretA = -150L;

	private static long _dernierArretMs;

	private const int AvertissementsDevantMax = 64;

	private const int AvertissementsDifferesMax = 16;

	private static readonly Dictionary<Guid, long> AvertissementsDevant = new Dictionary<Guid, long>();

	private static readonly List<(string Texte, int Taille, float X, float Y, float Echelle, Color Couleur, Color Cerne, FloatRect Encre)> AvertissementsDifferes = new List<(string, int, float, float, float, Color, Color, FloatRect)>();

	internal const float PartDesMots = 0.74f;

	private const int PlacesDesMotsMax = 32;

	private static readonly Dictionary<Guid, PlaceDUnMot> PlacesDesMots = new Dictionary<Guid, PlaceDUnMot>();

	public static bool IsFrozen
	{
		get
		{
			long milliseconds = Timing.Global.Milliseconds;
			OublierCeQuiVientDUneAutreHorloge(milliseconds);
			return milliseconds < _hitStopUntil;
		}
	}

	public static float SlowMotionFactor
	{
		get
		{
			long milliseconds = Timing.Global.Milliseconds;
			OublierCeQuiVientDUneAutreHorloge(milliseconds);
			if (milliseconds >= _slowMotionUntil)
			{
				return 1f;
			}
			return _slowMotionFactor;
		}
	}

	private static bool CameraEffects => Globals.Database?.CameraEffects ?? true;

	public static (float X, float Y) ShakeOffset
	{
		get
		{
			long milliseconds = Timing.Global.Milliseconds;
			OublierCeQuiVientDUneAutreHorloge(milliseconds);
			if (!CameraEffects || milliseconds >= _shakeUntil || _shakeUntil <= _shakeStartedAt || milliseconds < _shakeStartedAt)
			{
				return (X: 0f, Y: 0f);
			}
			float num = (float)(milliseconds - _shakeStartedAt) / (float)(_shakeUntil - _shakeStartedAt);
			float num2 = _shakeStrength * (1f - num);
			if (_shakeDirX == 0f && _shakeDirY == 0f)
			{
				return (X: (float)Math.Round((Noise.NextDouble() * 2.0 - 1.0) * (double)num2), Y: (float)Math.Round((Noise.NextDouble() * 2.0 - 1.0) * (double)num2));
			}
			float num3 = MathF.Cos(num * (float)Math.PI * 3f) * num2;
			float num4 = num2 * 0.25f;
			return (X: MathF.Round(_shakeDirX * num3 + (float)(Noise.NextDouble() * 2.0 - 1.0) * num4), Y: MathF.Round(_shakeDirY * num3 + (float)(Noise.NextDouble() * 2.0 - 1.0) * num4));
		}
	}

	public static float ZoomFactor
	{
		get
		{
			long milliseconds = Timing.Global.Milliseconds;
			OublierCeQuiVientDUneAutreHorloge(milliseconds);
			if (!CameraEffects || milliseconds >= _zoomUntil || _zoomUntil <= _zoomStartedAt || milliseconds < _zoomStartedAt)
			{
				if (!CameraEffects)
				{
					return 1f;
				}
				return Transformation.CameraPush;
			}
			float num = (float)(milliseconds - _zoomStartedAt) / (float)(_zoomUntil - _zoomStartedAt);
			float num2 = ((num < 0.25f) ? (1f - (1f - num / 0.25f) * (1f - num / 0.25f)) : (1f - (num - 0.25f) / 0.75f));
			return (1f + _zoomStrength * num2 * num2) * Transformation.CameraPush;
		}
	}

	internal static long ArretJusqua => _hitStopUntil;

	internal static IFont? PoliceDesMots
	{
		get
		{
			IFont? font = _font;
			if (font == null)
			{
				GameContentManager? contentManager = Globals.ContentManager;
				if (contentManager == null)
				{
					return null;
				}
				font = contentManager.GetFont("rajdhanibold");
			}
			return font;
		}
	}

	public static void SlowMotion(long durationMs, float factor)
	{
		if (CameraEffects)
		{
			long milliseconds = Timing.Global.Milliseconds;
			OublierCeQuiVientDUneAutreHorloge(milliseconds);
			_slowMotionFactor = Math.Clamp(factor, 0.1f, 1f);
			_slowMotionUntil = Math.Max(_slowMotionUntil, milliseconds + durationMs);
		}
	}

	private static void OublierCeQuiVientDUneAutreHorloge(long now)
	{
		if (now < _horlogeVue - 10000)
		{
			OublierLesArrets();
			_slowMotionUntil = 0L;
			_shakeStartedAt = 0L;
			_shakeUntil = 0L;
			_zoomStartedAt = 0L;
			_zoomUntil = 0L;
			_lastLocalStrikeAt = 0L;
			Sparks.Clear();
			DrawnSparks.Clear();
			Particles.Clear();
			Trails.Clear();
			LastCritical.Clear();
			Labels.Clear();
			NewestWords.Clear();
			PredictedHits.Clear();
			ComboSteps.Clear();
		}
		_horlogeVue = now;
	}

	public static void PunchZoom(float strength, long durationMs)
	{
		long milliseconds = Timing.Global.Milliseconds;
		OublierCeQuiVientDUneAutreHorloge(milliseconds);
		if (milliseconds >= _zoomUntil || !(strength < _zoomStrength))
		{
			_zoomStrength = strength;
			_zoomStartedAt = milliseconds;
			_zoomUntil = milliseconds + durationMs;
		}
	}

	public static void OnLocalStrike(Entity target, long attackTimeMs)
	{
		if (target is Resource || target.IsBlocking)
		{
			return;
		}
		if (target is Player en)
		{
			Player? me = Globals.Me;
			if (me != null && me.IsAllyOf(en))
			{
				return;
			}
		}
		long milliseconds = Timing.Global.Milliseconds;
		_localComboStep = ((!(target.Id == _lastLocalStrikeTarget) || milliseconds - _lastLocalStrikeAt > attackTimeMs + Options.Instance.Combat.ComboWindow) ? 1 : (_localComboStep % Math.Max(1, Options.Instance.Combat.ComboHits) + 1));
		_lastLocalStrikeAt = milliseconds;
		_lastLocalStrikeTarget = target.Id;
		PredictedHits[target.Id] = milliseconds;
		if (PredictedHits.Count > 64)
		{
			PredictedHits.Clear();
			PredictedHits[target.Id] = milliseconds;
		}
		AddSpark(target.Id, Globals.Me?.Id ?? Guid.Empty, SparkKind.Normal, milliseconds, StepScale(_localComboStep));
		CombatAudio.Play("coup.wav", target);
		float num;
		float num2;
		if (Globals.Me != null)
		{
			(num, num2) = Between(Globals.Me, target);
		}
		else
		{
			num = 0f;
			num2 = 0f;
		}
		float num3 = MathF.Sqrt(num * num + num2 * num2);
		Impact(Poids.Coup, (num3 > 0.5f) ? (num / num3) : 0f, (num3 > 0.5f) ? (num2 / num3) : 0f);
	}

	public static void MarkPredictedHit(Guid entityId)
	{
		PredictedHits[entityId] = Timing.Global.Milliseconds;
		if (PredictedHits.Count > 64)
		{
			PredictedHits.Clear();
			PredictedHits[entityId] = Timing.Global.Milliseconds;
		}
	}

	public static void CancelPredictedStrike(Guid entityId)
	{
		long now = Timing.Global.Milliseconds;
		PredictedHits.Remove(entityId);
		Sparks.RemoveAll(((Guid Entity, Guid Attacker, SparkKind Kind, long StartedAt, float OffsetX, float OffsetY, float Size) spark) => spark.Entity == entityId && spark.Kind == SparkKind.Normal && now - spark.StartedAt < 400);
	}

	private static float StepScale(int step)
	{
		if (step > 1)
		{
			return 1f + 0.22f * (float)(step - 1);
		}
		return 1f;
	}

	public static void OnHit(Guid entityId, long damage = 0L, Guid attackerId = default(Guid))
	{
		if (!Globals.Entities.TryGetValue(entityId, out Entity value) || value == null)
		{
			return;
		}
		long now = Timing.Global.Milliseconds;
		Entity entity = Shudder(value, now, attackerId);
		value.HitWhiteUntil = now + 70;
		if (value.BossVisual != BossVisual.Standard)
		{
			DungeonWorld.BossFrappe(value, entity);
		}
		bool flag = Heritage.Actif && Heritage.Garde.EncaisseEnGarde(value, now);
		long value2;
		bool flag2 = PredictedHits.TryGetValue(entityId, out value2) && now - value2 < 400;
		if (flag2)
		{
			PredictedHits.Remove(entityId);
		}
		bool flag3 = Heritage.Actif && PreditParLHeritage(value, now, attackerId);
		if (flag3)
		{
			flag2 = true;
		}
		SparkKind item = Sparks.LastOrDefault(((Guid Entity, Guid Attacker, SparkKind Kind, long StartedAt, float OffsetX, float OffsetY, float Size) spark) => spark.Entity == entityId && spark.Kind != SparkKind.Normal && now - spark.StartedAt < 120).Kind;
		bool flag4 = item != SparkKind.Normal;
		int num;
		if (ComboSteps.TryGetValue(entityId, out (int, long) value3) && now - value3.Item2 < 200)
		{
			(num, _) = value3;
		}
		else
		{
			num = 1;
		}
		int num2 = num;
		long num3 = value.MaxVital[0];
		Blood.OnHit(value, flag4 || (num3 > 0 && (float)damage >= (float)num3 * 0.15f));
		if (value == Globals.Me)
		{
			LowHealth.OnLocalHit(value.HitRecoilX);
			Globals.Me.AnnulerLaRecolteAuClic();
			if (Heritage.Actif)
			{
				Heritage.NoterUnCoupRecu((attackerId != Guid.Empty) ? attackerId : (entity?.Id ?? Guid.Empty));
			}
		}
		bool flag5 = entity != null && Distance(entity, value) <= (float)Options.Instance.Map.TileWidth * 2.5f;
		bool flag6 = Heritage.Actif && !flag && Heritage.Effets.PrendLeCoup(entity, value, flag2);
		if (!flag6 && !flag)
		{
			Burst(value, entity, flag4 ? 12 : (6 + 2 * (num2 - 1)), flag4 ? Orange : new Color(255, 255, 240, 200), now);
		}
		if (entity != null && flag5)
		{
			if (!flag6 && !flag)
			{
				AddTrail(entity, value, flag4 ? Orange : new Color(255, 255, 250, 230), now);
			}
			if (!flag2 && (!Heritage.Actif || !Heritage.Son.CoupConfirme(entity, value)))
			{
				CombatAudio.Play("coup.wav", value);
			}
		}
		if (!flag)
		{
			CombatAudio.Pain(value, damage);
		}
		if (value == Globals.Me || entity == Globals.Me)
		{
			float num4 = ((num3 > 0) ? Math.Clamp((float)damage / (float)num3 / 0.25f, 0f, 1f) : 0f);
			Poids poids = item switch
			{
				SparkKind.Finisher => Poids.Final, 
				SparkKind.Critical => Poids.Critique, 
				_ => (value == Globals.Me) ? Poids.Recu : Poids.Coup, 
			};
			if (flag3)
			{
				flag4 = false;
			}
			if (!flag2 || flag4 || num4 >= 0.5f)
			{
				Impact(poids, value.HitRecoilX, value.HitRecoilY, num4);
			}
		}
		if (!(flag4 || flag2 || flag6 || flag))
		{
			AddSpark(entityId, entity?.Id ?? Guid.Empty, SparkKind.Normal, now, StepScale(num2));
		}
	}

	private static Entity? Shudder(Entity target, long now, Guid attackerId = default(Guid))
	{
		target.HitShakeAt = now;
		target.HitRecoilX = 0f;
		target.HitRecoilY = 0f;
		float num = (float)Options.Instance.Map.TileWidth * 2.5f;
		Entity entity = null;
		Player me = Globals.Me;
		long value;
		bool flag = me != null && (me.IsAttacking || (_lastLocalStrikeTarget == target.Id && now - _lastLocalStrikeAt < 600) || (PredictedHits.TryGetValue(target.Id, out value) && now - value < 400));
		if (attackerId != Guid.Empty && attackerId != target.Id && Globals.Entities.TryGetValue(attackerId, out Entity value2) && value2 != null)
		{
			entity = value2;
		}
		else if (me != null && target != me && flag && Distance(me, target) <= num)
		{
			entity = me;
		}
		else
		{
			float num2 = num;
			foreach (Entity value3 in Globals.Entities.Values)
			{
				if (value3 != null && value3 != target && value3.IsAttacking)
				{
					float num3 = Distance(value3, target);
					if (num3 <= num2)
					{
						num2 = num3;
						entity = value3;
					}
				}
			}
		}
		if (entity != null)
		{
			(float X, float Y) tuple = Between(entity, target);
			float item = tuple.X;
			float item2 = tuple.Y;
			float num4 = MathF.Sqrt(item * item + item2 * item2);
			if (num4 > 0.5f)
			{
				target.HitRecoilX = item / num4;
				target.HitRecoilY = item2 / num4;
			}
		}
		return entity;
	}

	private static (float X, float Y) Between(Entity from, Entity to)
	{
		return (X: to.WorldPos.X + to.WorldPos.Width / 2f - (from.WorldPos.X + from.WorldPos.Width / 2f), Y: to.WorldPos.Y + to.WorldPos.Height - (from.WorldPos.Y + from.WorldPos.Height));
	}

	private static float Distance(Entity from, Entity to)
	{
		var (num, num2) = Between(from, to);
		return MathF.Sqrt(num * num + num2 * num2);
	}

	private static void Burst(Entity target, Entity? attacker, int count, Color color, long now)
	{
		FloatRect worldPos = target.WorldPos;
		float x = worldPos.X + worldPos.Width / 2f;
		float y = worldPos.Y + worldPos.Height * 0.55f;
		float? num = ((attacker != null && (target.HitRecoilX != 0f || target.HitRecoilY != 0f)) ? new float?(MathF.Atan2(target.HitRecoilY, target.HitRecoilX)) : ((float?)null));
		for (int i = 0; i < count; i++)
		{
			float x2 = (num.HasValue ? (num.Value + ((float)Noise.NextDouble() - 0.5f) * 2.1f) : ((float)(Noise.NextDouble() * Math.PI * 2.0)));
			float num2 = 45f + (float)Noise.NextDouble() * 85f;
			Color color2 = ((i % 2 == 0) ? color : new Color(255, 196, 170, 130));
			AddParticle(x, y, MathF.Cos(x2) * num2, MathF.Sin(x2) * num2 - 30f, now, 260 + Noise.Next(160), color2, 2f + (float)Noise.NextDouble() * 1.5f);
		}
	}

	private static void AddParticle(float x, float y, float vx, float vy, long now, long life, Color color, float size)
	{
		Particles.Add((x, y, vx, vy, now, life, color, size));
		if (Particles.Count > 400)
		{
			Particles.RemoveAt(0);
		}
	}

	private static void AddTrail(Entity attacker, Entity target, Color color, long now)
	{
		FloatRect worldPos = attacker.WorldPos;
		FloatRect worldPos2 = target.WorldPos;
		Trails.Add((worldPos.X + worldPos.Width / 2f, worldPos.Y + worldPos.Height * 0.55f, worldPos2.X + worldPos2.Width / 2f, worldPos2.Y + worldPos2.Height * 0.55f, now, color));
		if (Trails.Count > 32)
		{
			Trails.RemoveAt(0);
		}
	}

	private static void AddSpark(Guid entityId, Guid attackerId, SparkKind kind, long now, float scale = 1f)
	{
		float item = scale * kind switch
		{
			SparkKind.Critical => 48f, 
			SparkKind.Finisher => 64f, 
			_ => 32f, 
		};
		Sparks.Add((entityId, attackerId, kind, now, (float)(Noise.NextDouble() * 5.0 - 2.5), (float)(Noise.NextDouble() * 5.0 - 2.5), item));
		if (Sparks.Count > 48)
		{
			Sparks.RemoveAt(0);
		}
	}

	public static void OnImpact(CombatImpactPacket packet)
	{
		if (packet.Ecrase)
		{
			Echanges.OnEcrase(packet);
			return;
		}
		long now = Timing.Global.Milliseconds;
		Globals.Entities.TryGetValue(packet.AttackerId, out Entity value);
		Globals.Entities.TryGetValue(packet.TargetId, out Entity target);
		if (packet.IsFinisher && target != null)
		{
			Echanges.OnCoupFinal(value, target);
		}
		ComboSteps[packet.TargetId] = (packet.ComboStep, now);
		if (ComboSteps.Count > 128)
		{
			ComboSteps.Clear();
			ComboSteps[packet.TargetId] = (packet.ComboStep, now);
		}
		if (packet.ComboStep >= 2 && packet.IsFinisher && value != null && (!Heritage.Actif || !Options.Instance.Combat.Heritage.CacherCoupFinal))
		{
			Labels[(value.Id, LabelKind.Combo)] = (Strings.Combo.Finisher.ToString(packet.ComboStep), Gold, 14, now);
		}
		if (!packet.IsFinisher && !packet.Critical)
		{
			return;
		}
		if (packet.Critical)
		{
			LastCritical[packet.TargetId] = now;
			if (LastCritical.Count > 256)
			{
				LastCritical.Clear();
				LastCritical[packet.TargetId] = now;
			}
		}
		bool flag = Heritage.Actif && target != null && Heritage.Effets.PrendLeCoup(value, target, dejaMontre: true);
		if (target != null)
		{
			if (!flag)
			{
				target.HitFlashUntil = now + 110;
			}
			Sparks.RemoveAll(((Guid Entity, Guid Attacker, SparkKind Kind, long StartedAt, float OffsetX, float OffsetY, float Size) spark) => spark.Entity == target.Id && spark.Kind == SparkKind.Normal && now - spark.StartedAt < 400);
			if (!flag)
			{
				AddSpark(target.Id, packet.AttackerId, (!packet.IsFinisher) ? SparkKind.Critical : SparkKind.Finisher, now);
				Burst(target, value, packet.IsFinisher ? 16 : 10, packet.IsFinisher ? Gold : Orange, now);
			}
			if (packet.IsFinisher && (!Heritage.Actif || !Heritage.Son.CoupFinalConfirme(value, target)))
			{
				CombatAudio.Play("strongcoup.wav", target, 0.05f);
			}
			if (packet.Critical && (!flag || !Heritage.Effets.TaitLeCritique(value)))
			{
				CombatAudio.Play("crithit.wav", target, 0.06f);
			}
		}
		if (Involves(packet.AttackerId, packet.TargetId))
		{
			float strength = ((packet.IsFinisher && packet.Critical) ? 6f : (packet.IsFinisher ? 4.5f : 3f));
			float num;
			float num2;
			if (value != null && target != null)
			{
				(num, num2) = Between(value, target);
			}
			else
			{
				num = 0f;
				num2 = 0f;
			}
			float num3 = MathF.Sqrt(num * num + num2 * num2);
			if (num3 > 0.5f)
			{
				num /= num3;
				num2 /= num3;
			}
			else
			{
				num = (num2 = 0f);
			}
			if (!flag)
			{
				StartShake(strength, packet.IsFinisher ? 200 : 150, num, num2);
				PunchZoom(packet.IsFinisher ? 0.05f : 0.03f, packet.IsFinisher ? 240 : 160);
			}
		}
	}

	public static void OnGuard(GuardPacket packet)
	{
		if (!Globals.Entities.TryGetValue(packet.EntityId, out Entity value) || value == null)
		{
			return;
		}
		long milliseconds = Timing.Global.Milliseconds;
		if (value == Globals.Me && value.IsBlocking && !packet.IsBlocking && packet.Event == GuardEvent.None)
		{
			value.GuardLockedUntil = Math.Max(value.GuardLockedUntil, milliseconds + 300);
		}
		value.IsBlocking = packet.IsBlocking;
		value.GuardPercent = packet.GuardPercent;
		value.GuardShownUntil = milliseconds + 1500;
		switch (packet.Event)
		{
		case GuardEvent.Blocked:
			value.GuardHitAt = milliseconds;
			if (value == Globals.Me)
			{
				StartShake(1.5f, 90L);
			}
			break;
		case GuardEvent.Perfect:
		{
			value.GuardHitAt = milliseconds;
			value.GuardFlashUntil = milliseconds + 250;
			Labels[(value.Id, LabelKind.Guard)] = (Strings.Guard.Perfect, Cyan, 12, milliseconds);
			if (Globals.Entities.TryGetValue(packet.AttackerId, out Entity value2) && value2 != null)
			{
				value2.GuardStunnedUntil = Math.Max(value2.GuardStunnedUntil, milliseconds + packet.DurationMs);
			}
			if (Involves(packet.EntityId, packet.AttackerId))
			{
				StartShake(2.5f, 140L);
				HitStop(60L);
			}
			break;
		}
		case GuardEvent.Break:
			value.IsBlocking = false;
			value.GuardHitAt = milliseconds;
			value.GuardBreakUntil = milliseconds + 400;
			value.GuardStunnedUntil = milliseconds + packet.DurationMs;
			value.GuardLockedUntil = milliseconds + Options.Instance.Combat.GuardBreakCooldown;
			Labels[(value.Id, LabelKind.Guard)] = (Strings.Guard.Break, Red, 12, milliseconds);
			if (Involves(packet.EntityId, packet.AttackerId))
			{
				StartShake(5f, 220L);
				HitStop(90L);
			}
			break;
		}
	}

	private static bool Involves(Guid first, Guid second)
	{
		Player me = Globals.Me;
		if (me != null)
		{
			if (!(first == me.Id))
			{
				return second == me.Id;
			}
			return true;
		}
		return false;
	}

	public static void HitStop(long durationMs)
	{
		long milliseconds = Timing.Global.Milliseconds;
		OublierCeQuiVientDUneAutreHorloge(milliseconds);
		_hitStopUntil = Math.Max(_hitStopUntil, milliseconds + durationMs);
	}

	public static void CoupDeScene(Entity target)
	{
		long milliseconds = Timing.Global.Milliseconds;
		Shudder(target, milliseconds);
		target.HitWhiteUntil = milliseconds + 70;
		Blood.Gicler(target, heavy: true);
		HitStop(60L);
		StartShake(2.6f, 220L);
	}

	public static void Callout(Guid entityId, string text, Color color, int size)
	{
		if (!MotsMuets)
		{
			if (Heritage.Actif)
			{
				size = TailleDuMot(text, size);
			}
			Labels[(entityId, LabelKind.Guard)] = (text, color, size, Timing.Global.Milliseconds);
		}
	}

	public static void StartShake(float strength, long durationMs, float dirX = 0f, float dirY = 0f)
	{
		long milliseconds = Timing.Global.Milliseconds;
		OublierCeQuiVientDUneAutreHorloge(milliseconds);
		if (milliseconds >= _shakeUntil || !(strength < _shakeStrength * (1f - (float)(milliseconds - _shakeStartedAt) / (float)Math.Max(1L, _shakeUntil - _shakeStartedAt))))
		{
			_shakeStrength = strength;
			_shakeStartedAt = milliseconds;
			_shakeUntil = milliseconds + durationMs;
			_shakeDirX = dirX;
			_shakeDirY = dirY;
		}
	}

	private static (long Arret, float Secousse, long Duree) Plancher(Poids poids)
	{
		(int, float, int) tuple = poids switch
		{
			Poids.Critique => (70, 2.5f, 150), 
			Poids.Final => (90, 3f, 190), 
			Poids.Recu => (65, 3f, 160), 
			_ => (45, 1.6f, 110), 
		};
		return (Arret: tuple.Item1, Secousse: tuple.Item2, Duree: tuple.Item3);
	}

	public static void Impact(Poids poids, float dirX, float dirY, float part = 0f, float secousseMin = 0f)
	{
		(long Arret, float Secousse, long Duree) tuple = Plancher(poids);
		long item = tuple.Arret;
		float item2 = tuple.Secousse;
		long item3 = tuple.Duree;
		part = Math.Clamp(part, 0f, 1f);
		item = Math.Min(120L, Math.Max(item, (long)(15f + 45f * part)));
		item2 = Math.Max(Math.Max(item2, secousseMin), 1.2f + 1.8f * part);
		item3 = Math.Max(item3, 90 + (long)(60f * part));
		long milliseconds = Timing.Global.Milliseconds;
		OublierCeQuiVientDUneAutreHorloge(milliseconds);
		if (milliseconds - _dernierArretA >= 150 || item > _dernierArretMs)
		{
			HitStop(item);
			_dernierArretA = milliseconds;
			_dernierArretMs = item;
		}
		StartShake(item2, item3, dirX, dirY);
	}

	public static FloatRect AdjustGuardPose(Entity entity, FloatRect rect, bool hasGuardSheet)
	{
		if (Heritage.Actif)
		{
			rect = Heritage.Corps.Pose(entity, rect);
		}
		long milliseconds = Timing.Global.Milliseconds;
		float num = ((entity.IsBlocking && !hasGuardSheet) ? 1f : 0f);
		long num2 = ((entity.GuardBlendAt == 0L) ? 90 : (milliseconds - entity.GuardBlendAt));
		entity.GuardBlendAt = milliseconds;
		entity.GuardBlend += (num - entity.GuardBlend) * Math.Clamp((float)num2 / 90f, 0f, 1f);
		long num3 = milliseconds - entity.GuardHitAt;
		bool flag = entity.GuardHitAt > 0 && num3 < 140;
		long num4 = milliseconds - entity.HitShakeAt;
		bool flag2 = entity.HitShakeAt > 0 && num4 < 200;
		float num5 = 0f;
		if (entity.IsAttacking && !entity.IsBlocking)
		{
			int num6 = Math.Max(1, entity.CalculateAttackTime());
			float num7 = 1f - Math.Clamp((float)(entity.AttackTimer - milliseconds) / (float)num6, 0f, 1f);
			num5 = 4f * MathF.Sin((float)Math.PI * MathF.Min(1f, num7 * 2f));
		}
		var (num8, num9) = KiBlast.LaunchRecoil(entity);
		if (num8 != 0f || num9 != 0f)
		{
			rect = new FloatRect(MathF.Round(rect.X + num8), MathF.Round(rect.Y + num9), rect.Width, rect.Height);
		}
		rect = (Heritage.Actif ? Heritage.Garde.Tituber(entity, rect, milliseconds) : rect);
		if (entity.GuardBlend < 0.01f && !flag && !flag2 && num5 < 0.01f)
		{
			entity.GuardBlend = 0f;
			return rect;
		}
		float num10 = rect.Width * (1f + 0.04f * entity.GuardBlend);
		float num11 = rect.Height * (1f - 0.07f * entity.GuardBlend);
		float num12 = rect.X - (num10 - rect.Width) / 2f;
		float num13 = rect.Y + (rect.Height - num11);
		if (flag)
		{
			float num14 = 2.5f * (1f - (float)num3 / 140f);
			(float X, float Y) tuple2 = Backward(entity.DirectionFacing);
			float item = tuple2.X;
			float item2 = tuple2.Y;
			num12 += item * num14;
			num13 += item2 * num14;
		}
		if (flag2)
		{
			float num15 = (float)num4 / 200f;
			bool flag3 = entity.Dashing != null || entity.DashQueue.Count > 0;
			num12 += MathF.Sin((float)num4 * 0.35f) * (flag3 ? 0.6f : 1.6f) * (1f - num15);
			float num16 = (flag3 ? 0f : (5.5f * MathF.Sin((float)Math.PI * MathF.Min(1f, num15 * 1.6f))));
			num12 += entity.HitRecoilX * num16;
			num13 += entity.HitRecoilY * num16 * 0.6f;
		}
		if (num5 > 0f)
		{
			(float X, float Y) tuple3 = Backward(entity.DirectionFacing);
			float item3 = tuple3.X;
			float item4 = tuple3.Y;
			num12 -= item3 * num5;
			num13 -= item4 * num5;
		}
		return new FloatRect(MathF.Round(num12), MathF.Round(num13), MathF.Round(num10), MathF.Round(num11));
	}

	private static (float X, float Y) Backward(Direction facing)
	{
		return facing switch
		{
			Direction.Up => (X: 0f, Y: 1f), 
			Direction.Down => (X: 0f, Y: -1f), 
			Direction.Left => (X: 1f, Y: 0f), 
			Direction.Right => (X: -1f, Y: 0f), 
			Direction.UpLeft => (X: 0.7f, Y: 0.7f), 
			Direction.UpRight => (X: -0.7f, Y: 0.7f), 
			Direction.DownLeft => (X: 0.7f, Y: -0.7f), 
			Direction.DownRight => (X: -0.7f, Y: -0.7f), 
			_ => (X: 0f, Y: 0f), 
		};
	}

	public static void DrawShield(Entity entity, FloatRect bounds, bool front)
	{
		if (entity is Player)
		{
			return;
		}
		long milliseconds = Timing.Global.Milliseconds;
		bool flag = milliseconds < entity.GuardBreakUntil;
		bool flag2 = milliseconds < entity.GuardFlashUntil;
		if (!entity.IsBlocking && !flag && !flag2)
		{
			return;
		}
		Direction directionFacing = entity.DirectionFacing;
		bool flag3 = ((directionFacing == Direction.Up || (uint)(directionFacing - 4) <= 1u) ? true : false);
		bool flag4 = flag3;
		if (flag4 == front)
		{
			return;
		}
		IGameTexture texture = Globals.ContentManager.GetTexture(TextureType.Misc, "guard_shield.png");
		if (texture == null)
		{
			return;
		}
		long num = milliseconds - entity.GuardHitAt;
		bool flag5 = entity.GuardHitAt > 0 && num < 260;
		float num3;
		Color color;
		if (flag)
		{
			float num2 = 1f - (float)(entity.GuardBreakUntil - milliseconds) / 400f;
			num3 = 1f + 0.4f * num2;
			color = new Color((int)(235f * (1f - num2)), 255, 90, 70);
		}
		else if (flag2)
		{
			float num4 = 1f - (float)(entity.GuardFlashUntil - milliseconds) / 250f;
			num3 = 1.2f - 0.2f * num4;
			color = new Color(255, 255, 255, 255);
		}
		else
		{
			float num5 = (flag5 ? (1f - (float)num / 260f) : 0f);
			num3 = 1f + 0.08f * num5 + 0.02f * MathF.Sin((float)milliseconds / 150f);
			int a = (int)Math.Min(255f, 175f + 80f * num5);
			float num6 = (float)entity.GuardPercent / 100f;
			color = ((num6 > 0.5f) ? new Color(a, 190, 230, 255) : ((num6 > 0.25f) ? new Color(a, 255, 200, 120) : new Color(a, 255, 120, 100)));
		}
		float num7 = bounds.Width * 0.62f;
		float num8 = bounds.Height * 0.6f;
		float num9 = bounds.X + bounds.Width / 2f;
		float num10 = bounds.Y + bounds.Height * 0.6f;
		float num11;
		switch (directionFacing)
		{
		case Direction.Left:
			num11 = num7 * 0.38f;
			num9 -= bounds.Width * 0.22f;
			break;
		case Direction.Right:
			num11 = num7 * 0.38f;
			num9 += bounds.Width * 0.22f;
			break;
		case Direction.UpLeft:
		case Direction.DownLeft:
			num11 = num7 * 0.7f;
			num9 -= bounds.Width * 0.13f;
			break;
		case Direction.UpRight:
		case Direction.DownRight:
			num11 = num7 * 0.7f;
			num9 += bounds.Width * 0.13f;
			break;
		default:
			num11 = num7;
			num10 += ((directionFacing == Direction.Down) ? (bounds.Height * 0.04f) : 0f);
			break;
		}
		num11 *= num3;
		float num12 = num8 * num3;
		Graphics.DrawGameTexture(texture, new FloatRect(0f, 0f, texture.Width, texture.Height), new FloatRect(num9 - num11 / 2f, num10 - num12 / 2f, num11, num12), color);
		if (!(!flag5 || flag))
		{
			IGameTexture texture2 = Globals.ContentManager.GetTexture(TextureType.Misc, "guard_ripple.png");
			if (texture2 != null)
			{
				float num13 = (float)num / 260f;
				float num14 = Math.Max(num11, num12) * (0.5f + 0.9f * num13);
				Color renderColor = new Color((int)(220f * (1f - num13)), color.R, color.G, color.B);
				Graphics.DrawGameTexture(texture2, new FloatRect(0f, 0f, texture2.Width, texture2.Height), new FloatRect(num9 - num14 / 2f, num10 - num14 / 2f, num14, num14), renderColor);
			}
		}
	}

	public static void Draw()
	{
		DrawGuardGauge();
		DrawImpacts();
		DrawLabels();
		if (Heritage.Actif)
		{
			Heritage.Effets.DessinerParDessus();
		}
		if (Heritage.Actif)
		{
			DessinerLesAvertissements();
		}
	}

	public static void DrawUnderCanopy()
	{
		DrawTrails();
		DrawParticles();
		if (Heritage.Actif)
		{
			Heritage.Effets.DessinerSousLesCimes();
		}
	}

	private static void DrawGuardGauge()
	{
		Player me = Globals.Me;
		if (me == null || !Options.Instance.Combat.EnableGuard)
		{
			return;
		}
		long milliseconds = Timing.Global.Milliseconds;
		if (!me.IsBlocking && me.GuardPercent >= 100 && milliseconds >= me.GuardShownUntil)
		{
			return;
		}
		IGameTexture texture = Globals.ContentManager.GetTexture(TextureType.Misc, "castbackground.png");
		IGameTexture texture2 = Globals.ContentManager.GetTexture(TextureType.Misc, "castbar.png");
		if (texture != null && texture2 != null)
		{
			FloatRect worldPos = me.WorldPos;
			float num = worldPos.X + worldPos.Width / 2f - (float)texture.Width / 2f;
			float num2 = worldPos.Y + worldPos.Height + 2f;
			float num3 = Math.Clamp((float)me.GuardPercent / 100f, 0f, 1f);
			Color renderColor = ((milliseconds >= me.GuardStunnedUntil) ? ((num3 > 0.5f) ? new Color(255, 200, 232, 255) : ((num3 > 0.25f) ? new Color(255, 255, 190, 90) : Red)) : ((milliseconds / 120 % 2 == 0L) ? Red : new Color(255, 120, 40, 40)));
			Graphics.DrawGameTexture(texture, new FloatRect(0f, 0f, texture.Width, texture.Height), new FloatRect(num, num2, texture.Width, texture.Height), Color.White);
			float num4 = (float)texture2.Width * num3;
			if (num4 >= 1f)
			{
				Graphics.DrawGameTexture(texture2, new FloatRect(0f, 0f, num4, texture2.Height), new FloatRect(num + (float)(texture.Width - texture2.Width) / 2f, num2 + (float)(texture.Height - texture2.Height) / 2f, num4, texture2.Height), renderColor);
			}
		}
	}

	private static (float X, float Y, bool Behind) ContactPoint(Entity target, Guid attackerId)
	{
		FloatRect worldPos = target.WorldPos;
		float num = worldPos.X + worldPos.Width / 2f;
		float num2 = worldPos.Y + worldPos.Height * 0.58f;
		if (attackerId == Guid.Empty || !Globals.Entities.TryGetValue(attackerId, out Entity value) || value == null || value == target)
		{
			return (X: num, Y: num2, Behind: false);
		}
		(float X, float Y) tuple = Between(value, target);
		float item = tuple.X;
		float item2 = tuple.Y;
		float num3 = MathF.Sqrt(item * item + item2 * item2);
		if (num3 <= 0.5f)
		{
			return (X: num, Y: num2, Behind: false);
		}
		float num4 = item / num3;
		float num5 = item2 / num3;
		num -= num4 * worldPos.Width * 0.32f;
		num2 -= num5 * worldPos.Height * 0.14f;
		num2 += num5 * 3f;
		return (X: num, Y: num2, Behind: num5 < -0.1f);
	}

	public static void DrawSparksBehind(Entity entity)
	{
		if (Heritage.Actif)
		{
			Heritage.Effets.DessinerDansLeTri(entity, derriere: true);
		}
		DrawSparksOn(entity, behind: true);
	}

	public static void DrawSparksInFront(Entity entity)
	{
		DrawSparksOn(entity, behind: false);
		if (Heritage.Actif)
		{
			Heritage.Effets.DessinerDansLeTri(entity, derriere: false);
		}
	}

	private static void DrawSparksOn(Entity entity, bool behind)
	{
		if (Sparks.Count == 0)
		{
			return;
		}
		long milliseconds = Timing.Global.Milliseconds;
		IGameTexture gameTexture = null;
		foreach (var spark in Sparks)
		{
			if (spark.Entity != entity.Id || spark.Kind == SparkKind.Death)
			{
				continue;
			}
			long num = milliseconds - spark.StartedAt;
			if (num >= 180)
			{
				continue;
			}
			DrawnSparks.Add(entity.Id);
			if (gameTexture == null)
			{
				gameTexture = Globals.ContentManager.GetTexture(TextureType.Misc, "hit_spark.png");
			}
			if (gameTexture != null)
			{
				var (num2, num3, flag) = ContactPoint(entity, spark.Attacker);
				if (flag == behind)
				{
					DrawSpark(gameTexture, spark.Kind, spark.Size, num2 + spark.OffsetX, num3 + spark.OffsetY, num);
				}
			}
		}
	}

	private static void DrawSpark(IGameTexture texture, SparkKind kind, float size, float x, float y, long age)
	{
		int num = Math.Max(1, (int)MathF.Round((float)texture.Width / (float)Math.Max(1, texture.Height)));
		float num2 = (float)texture.Width / (float)num;
		int num3 = Math.Clamp((int)(age * num / 180), 0, num - 1);
		Color renderColor = kind switch
		{
			SparkKind.Critical => new Color(255, 255, 150, 60), 
			SparkKind.Finisher => new Color(255, 255, 215, 110), 
			_ => new Color(255, 255, 248, 215), 
		};
		float num4 = size * 0.8f;
		Graphics.DrawGameTexture(texture, new FloatRect((float)num3 * num2, 0f, num2, texture.Height), new FloatRect(x - size / 2f, y - num4 / 2f, size, num4), renderColor);
	}

	private static void DrawImpacts()
	{
		if (Sparks.Count == 0)
		{
			DrawnSparks.Clear();
			return;
		}
		long milliseconds = Timing.Global.Milliseconds;
		IGameTexture texture = Globals.ContentManager.GetTexture(TextureType.Misc, "hit_spark.png");
		IGameTexture texture2 = Globals.ContentManager.GetTexture(TextureType.Misc, "hit_shockwave.png");
		for (int num = Sparks.Count - 1; num >= 0; num--)
		{
			(Guid, Guid, SparkKind, long, float, float, float) tuple = Sparks[num];
			long num2 = milliseconds - tuple.Item4;
			SparkKind item = tuple.Item3;
			bool flag = (uint)(item - 2) <= 1u;
			long num3 = (flag ? 320 : 180);
			if (num2 >= num3 || !Globals.Entities.TryGetValue(tuple.Item1, out Entity value) || value == null)
			{
				Sparks.RemoveAt(num);
			}
			else
			{
				if (texture != null && num2 < 180 && tuple.Item3 != SparkKind.Death && !DrawnSparks.Contains(tuple.Item1))
				{
					var (num4, num5, _) = ContactPoint(value, tuple.Item2);
					DrawSpark(texture, tuple.Item3, tuple.Item7, num4 + tuple.Item5, num5 + tuple.Item6, num2);
				}
				item = tuple.Item3;
				flag = (uint)(item - 2) <= 1u;
				if (flag && texture2 != null)
				{
					FloatRect worldPos = value.WorldPos;
					float num6 = worldPos.X + worldPos.Width / 2f + tuple.Item5;
					float num7 = worldPos.Y + worldPos.Height * 0.55f + tuple.Item6;
					float num8 = (float)num2 / 320f;
					float num9 = 20f + 72f * (1f - (1f - num8) * (1f - num8));
					int a = (int)(230f * (1f - num8));
					Graphics.DrawGameTexture(texture2, new FloatRect(0f, 0f, texture2.Width, texture2.Height), new FloatRect(num6 - num9 / 2f, num7 - num9 / 2f, num9, num9), (tuple.Item3 == SparkKind.Death) ? new Color(a, 150, 225, 255) : new Color(a, 255, 215, 110));
				}
			}
		}
		DrawnSparks.Clear();
	}

	private static void DrawLabels()
	{
		if (Labels.Count == 0 || Graphics.Renderer == null)
		{
			return;
		}
		if (_font == null)
		{
			_font = Globals.ContentManager.GetFont("rajdhanibold");
		}
		long milliseconds = Timing.Global.Milliseconds;
		NewestWords.Clear();
		(Guid, LabelKind) key;
		(string, Color, int, long) value;
		foreach (KeyValuePair<(Guid, LabelKind), (string, Color, int, long)> label2 in Labels)
		{
			label2.Deconstruct(out key, out value);
			(Guid, LabelKind) tuple = key;
			(string, Color, int, long) tuple2 = value;
			if (tuple.Item2 != LabelKind.Experience && (!NewestWords.TryGetValue(tuple.Item1, out var value2) || tuple2.Item4 > value2))
			{
				NewestWords[tuple.Item1] = tuple2.Item4;
			}
		}
		KeyValuePair<(Guid, LabelKind), (string, Color, int, long)>[] array = Labels.ToArray();
		foreach (KeyValuePair<(Guid, LabelKind), (string, Color, int, long)> keyValuePair in array)
		{
			keyValuePair.Deconstruct(out key, out value);
			(Guid, LabelKind) key2 = key;
			(string Text, Color Color, int Size, long ShownAt) label = value;
			long num = milliseconds - label.ShownAt;
			long value3;
			bool flag = key2.Item2 != LabelKind.Experience && NewestWords.TryGetValue(key2.Item1, out value3) && value3 > label.ShownAt;
			if (num > 700 || flag || !Globals.Entities.TryGetValue(key2.Item1, out Entity value4) || value4 == null)
			{
				Labels.Remove(key2);
				continue;
			}
			if (Heritage.Actif && key2.Item2 != LabelKind.Experience && ChasseParLeMotDuServeur(value4, label.Text, label.ShownAt))
			{
				Labels.Remove(key2);
				continue;
			}
			float num2 = (float)num / 700f;
			float scale = ((num < 120) ? (1.4f - 0.4f * ((float)num / 120f)) : 1f);
			int a = (int)(255f * ((num2 < 0.6f) ? 1f : (1f - (num2 - 0.6f) / 0.4f)));
			Color color = new Color(a, label.Color.R, label.Color.G, label.Color.B);
			Color outline = new Color(a, 20, 10, 0);
			FloatRect worldPos = value4.WorldPos;
			Vector2 vector = Graphics.Renderer.MeasureText(label.Text, _font, label.Size, scale);
			float x;
			float y;
			if (key2.Item2 == LabelKind.Combo)
			{
				x = worldPos.X + worldPos.Width * 0.75f;
				y = worldPos.Y + worldPos.Height * 0.25f - 12f * MathF.Sqrt(num2) - vector.Y / 2f;
			}
			else if (key2.Item2 == LabelKind.Experience)
			{
				x = worldPos.X + worldPos.Width / 2f - vector.X / 2f;
				y = worldPos.Y - 6f - 26f * MathF.Sqrt(num2) - vector.Y;
			}
			else
			{
				x = worldPos.X + worldPos.Width / 2f - vector.X / 2f;
				y = worldPos.Y + worldPos.Height * 0.05f - 14f * MathF.Sqrt(num2) - vector.Y;
			}
			if (Heritage.Actif && key2.Item2 == LabelKind.Guard)
			{
				(x, y) = PlaceDuMot(value4, label.Text, label.ShownAt, x, y, vector.X, vector.Y, milliseconds);
			}
			if (!Heritage.Actif || key2.Item2 != LabelKind.Guard || !DifferLAvertissement(key2.Item1, label.Text, label.ShownAt, label.Size, x, y, scale, vector.X, vector.Y, color, outline))
			{
				TexteMonde.Lisse(delegate
				{
					Graphics.Renderer.DrawString(label.Text, _font, label.Size, x, y, scale, color, worldPos: true, null, outline);
				});
			}
		}
	}

	private static void DrawParticles()
	{
		IGameTexture gameTexture = Graphics.Renderer?.WhitePixel;
		if (Particles.Count == 0 || gameTexture == null)
		{
			return;
		}
		long milliseconds = Timing.Global.Milliseconds;
		for (int num = Particles.Count - 1; num >= 0; num--)
		{
			(float, float, float, float, long, long, Color, float) tuple = Particles[num];
			long num2 = milliseconds - tuple.Item5;
			if (num2 >= tuple.Item6)
			{
				Particles.RemoveAt(num);
			}
			else
			{
				float num3 = (float)num2 / 1000f;
				float num4 = tuple.Item1 + tuple.Item3 * num3;
				float num5 = tuple.Item2 + tuple.Item4 * num3 + 170f * num3 * num3;
				float num6 = (float)num2 / (float)tuple.Item6;
				float num7 = MathF.Max(1f, MathF.Round(tuple.Rest.Item1 * (1f - 0.4f * num6)));
				Graphics.DrawGameTexture(gameTexture, new FloatRect(0f, 0f, 1f, 1f), new FloatRect(MathF.Round(num4 - num7 / 2f), MathF.Round(num5 - num7 / 2f), num7, num7), new Color((int)((float)(int)tuple.Item7.A * (1f - num6 * num6)), tuple.Item7.R, tuple.Item7.G, tuple.Item7.B));
			}
		}
	}

	private static void DrawTrails()
	{
		IGameTexture gameTexture = Graphics.Renderer?.WhitePixel;
		if (Trails.Count == 0 || gameTexture == null)
		{
			return;
		}
		long milliseconds = Timing.Global.Milliseconds;
		for (int num = Trails.Count - 1; num >= 0; num--)
		{
			(float, float, float, float, long, Color) tuple = Trails[num];
			long num2 = milliseconds - tuple.Item5;
			if (num2 >= 150)
			{
				Trails.RemoveAt(num);
			}
			else
			{
				float num3 = 1f - (float)num2 / 150f;
				for (int i = 0; i < 9; i++)
				{
					float num4 = 0.35f + 0.65f * (float)i / 8f;
					float num5 = tuple.Item1 + (tuple.Item3 - tuple.Item1) * num4;
					float num6 = tuple.Item2 + (tuple.Item4 - tuple.Item2) * num4;
					float num7 = MathF.Max(1f, MathF.Round(1.5f + 2.5f * num4 * num3));
					Graphics.DrawGameTexture(gameTexture, new FloatRect(0f, 0f, 1f, 1f), new FloatRect(MathF.Round(num5 - num7 / 2f), MathF.Round(num6 - num7 / 2f), num7, num7), new Color((int)((float)(int)tuple.Item6.A * num3 * num4), tuple.Item6.R, tuple.Item6.G, tuple.Item6.B), null, GameBlendModes.Add);
				}
			}
		}
	}

	public static void OnDeath(Entity entity)
	{
		if (!(entity is Player) && !entity.IsDying)
		{
			long num = (entity.DyingAt = Timing.Global.Milliseconds);
			FloatRect worldPos = entity.WorldPos;
			float x = worldPos.X + worldPos.Width / 2f;
			float y = worldPos.Y + worldPos.Height * 0.55f;
			bool flag = KiBlast.OnKiKill(entity, num);
			for (int i = 0; i < 22; i++)
			{
				float x2 = (float)(Noise.NextDouble() * Math.PI * 2.0);
				float num2 = 60f + (float)Noise.NextDouble() * 90f;
				Color color = ((i % 3 == 0) ? new Color(255, 255, 255, 255) : (flag ? Gold : Cyan));
				AddParticle(x, y, MathF.Cos(x2) * num2, MathF.Sin(x2) * num2 - 50f, num, 380 + Noise.Next(200), color, 2f + (float)Noise.NextDouble() * 2f);
			}
			Sparks.Add((entity.Id, Guid.Empty, SparkKind.Death, num, 0f, 0f, 0f));
		}
	}

	public static bool WasCritical(Guid entityId, long at)
	{
		if (LastCritical.TryGetValue(entityId, out var value))
		{
			return Math.Abs(value - at) < 300;
		}
		return false;
	}

	public static int WhiteFlash(Entity entity)
	{
		long milliseconds = Timing.Global.Milliseconds;
		if (entity.IsDying && entity is Player)
		{
			return Mort.Blanc(entity);
		}
		if (entity.IsDying)
		{
			long num = milliseconds - entity.DyingAt;
			if (num >= 110)
			{
				return 0;
			}
			return (int)(230f * (1f - (float)num / 110f));
		}
		if (Heritage.Actif && milliseconds < entity.HitWhiteUntil)
		{
			return Math.Min(255, (int)((float)(170 * (entity.HitWhiteUntil - milliseconds)) / 70f));
		}
		if (milliseconds >= entity.HitWhiteUntil)
		{
			return 0;
		}
		return (int)((float)(170 * (entity.HitWhiteUntil - milliseconds)) / 70f);
	}

	public static int DeathAlpha(Entity entity, int alpha)
	{
		long num = Timing.Global.Milliseconds - entity.DyingAt;
		if (num <= 110)
		{
			return alpha;
		}
		float num2 = Math.Clamp((float)(num - 110) / (float)(DeathDuration(entity) - 110), 0f, 1f);
		return (int)((float)alpha * (1f - num2));
	}

	public static long DeathDuration(Entity entity)
	{
		if (entity.DeathDurationMs <= 110)
		{
			return 450L;
		}
		return entity.DeathDurationMs;
	}

	public static void OnExperience(long gained)
	{
		if (gained > 0 && Globals.Me != null)
		{
			string item = Strings.Banners.ExperienceGained.ToString(MarketWindow.FormatAmount(gained));
			Labels[(Globals.Me.Id, LabelKind.Experience)] = (item, Gold, 12, Timing.Global.Milliseconds);
		}
	}

	internal static int TailleDeLAvertissement(int reglage, int dAvant)
	{
		if (reglage <= 0)
		{
			return dAvant;
		}
		return Math.Min(reglage, 32);
	}

	internal static bool GrandirLAvertissement(Guid entiteId, long depuis, int reglage)
	{
		(Guid, LabelKind) key = (entiteId, LabelKind.Guard);
		if (reglage <= 0 || !Labels.TryGetValue(key, out (string, Color, int, long) value) || !EstUnAvertissement(value.Item1) || value.Item4 < depuis)
		{
			return false;
		}
		Labels[key] = (value.Item1, value.Item2, TailleDeLAvertissement(reglage, value.Item3), value.Item4);
		if (AvertissementsDevant.Count >= 64)
		{
			AvertissementsDevant.Clear();
		}
		AvertissementsDevant[entiteId] = value.Item4;
		return true;
	}

	internal static bool EstDevant(Guid entiteId, string? texte, long neA)
	{
		if (EstUnAvertissement(texte) && AvertissementsDevant.TryGetValue(entiteId, out var value))
		{
			return value == neA;
		}
		return false;
	}

	internal static bool AvertissementDevant(Guid entiteId, long maintenant)
	{
		if (AvertissementsDevant.Count > 0 && MotDe(entiteId, maintenant, out var mot))
		{
			return EstDevant(entiteId, mot.Texte, mot.Depuis);
		}
		return false;
	}

	internal static FloatRect EncreDeLAvertissement(float x, float y, float largeur, float haut, float bas)
	{
		return new FloatRect(x, y + haut, Math.Max(0f, largeur), Math.Max(0f, bas - haut));
	}

	internal static bool DifferLAvertissement(Guid entiteId, string texte, long neA, int taille, float x, float y, float echelle, float largeur, float hauteur, Color couleur, Color cerne)
	{
		if (!EstDevant(entiteId, texte, neA))
		{
			return false;
		}
		if (AvertissementsDifferes.Count >= 16)
		{
			AvertissementsDifferes.Clear();
		}
		var (haut, bas) = Graphics.Renderer?.MeasureTextInk(texte, PoliceDesMots, taille, echelle) ?? (0f, hauteur);
		AvertissementsDifferes.Add((texte, taille, x, y, echelle, couleur, cerne, EncreDeLAvertissement(x, y, largeur, haut, bas)));
		return true;
	}

	internal static int EncresDesAvertissements(Span<FloatRect> place)
	{
		int num = Math.Min(place.Length, AvertissementsDifferes.Count);
		for (int i = 0; i < num; i++)
		{
			place[i] = AvertissementsDifferes[i].Rest.Item1;
		}
		return num;
	}

	internal static void DessinerLesAvertissements()
	{
		foreach (var signe in AvertissementsDifferes)
		{
			TexteMonde.Lisse(delegate
			{
				Graphics.Renderer?.DrawString(signe.Texte, _font, signe.Taille, signe.X, signe.Y, signe.Echelle, signe.Couleur, worldPos: true, null, signe.Cerne);
			});
		}
		AvertissementsDifferes.Clear();
	}

	internal static void OublierLesAvertissements()
	{
		AvertissementsDevant.Clear();
		AvertissementsDifferes.Clear();
	}

	internal static Poids PoidsDuCoup(CoupHeritage coup)
	{
		return coup switch
		{
			CoupHeritage.Montant => Poids.Critique, 
			CoupHeritage.CoupDePied => Poids.Final, 
			_ => Poids.Coup, 
		};
	}

	internal static bool OnLocalStrikeHeritage(Entity target, CoupHeritage coup, int pas)
	{
		if (!(target is Resource) && !target.IsBlocking)
		{
			if (target is Player en)
			{
				Player? me = Globals.Me;
				if (me != null && me.IsAllyOf(en))
				{
					goto IL_002e;
				}
			}
			long milliseconds = Timing.Global.Milliseconds;
			_localComboStep = Math.Clamp(pas, 1, Math.Max(1, Options.Instance.Combat.ComboHits));
			_lastLocalStrikeAt = milliseconds;
			_lastLocalStrikeTarget = target.Id;
			Heritage.ImpactsPredits.Entrer(target.Id, coup, milliseconds);
			if (Options.Instance.Combat.Heritage.Allure.Actif)
			{
				Player me2 = Globals.Me;
				if (me2 != null)
				{
					Heritage.Son.Impact(target, coup);
					Heritage.Coups.Jouer(me2, target, coup, pas, milliseconds);
					CoupHeritageOptions coupHeritageOptions = HeritageRegles.Table(coup);
					float dx;
					float dy;
					if (!HeritageRegles.EstUneFauche(coup))
					{
						(dx, dy) = Heritage.Coups.Sens(me2, target);
					}
					else
					{
						dx = 0f;
						dy = 0f;
					}
					ImpactHeritage(coupHeritageOptions.ArretMs, (float)coupHeritageOptions.SecousseDixiemes / 10f, coupHeritageOptions.SecousseMs, dx, dy, milliseconds);
					return true;
				}
			}
			AddSpark(target.Id, Globals.Me?.Id ?? Guid.Empty, SparkKind.Normal, milliseconds, StepScale(_localComboStep));
			Heritage.Son.Impact(target, coup);
			target.HitWhiteUntil = milliseconds + 70;
			float num;
			float num2;
			if (Globals.Me != null)
			{
				(num, num2) = Between(Globals.Me, target);
			}
			else
			{
				num = 0f;
				num2 = 0f;
			}
			float num3 = MathF.Sqrt(num * num + num2 * num2);
			Impact(PoidsDuCoup(coup), (num3 > 0.5f) ? (num / num3) : 0f, (num3 > 0.5f) ? (num2 / num3) : 0f);
			return true;
		}
		goto IL_002e;
		IL_002e:
		return false;
	}

	internal static void ImpactHeritage(int arretMs, float secoussePx, int secousseMs, float dx, float dy)
	{
		ImpactHeritage(arretMs, secoussePx, secousseMs, dx, dy, Timing.Global.Milliseconds);
	}

	internal static bool ImpactHeritage(int arretMs, float secoussePx, int secousseMs, float dx, float dy, long maintenant)
	{
		bool flag = arretMs > 0 && (maintenant - _dernierArretA >= 150 || arretMs > _dernierArretMs);
		if (flag)
		{
			_hitStopUntil = Math.Max(_hitStopUntil, maintenant + arretMs);
			_dernierArretA = maintenant;
			_dernierArretMs = arretMs;
		}
		if (secoussePx > 0f && secousseMs > 0)
		{
			StartShake(secoussePx, secousseMs, dx, dy);
		}
		return flag;
	}

	internal static bool EstUnAvertissement(string? texte)
	{
		if (texte == "!" || texte == "!!")
		{
			return true;
		}
		return false;
	}

	internal static bool RetirerLAvertissement(Guid entiteId, long depuis)
	{
		(Guid, LabelKind) key = (entiteId, LabelKind.Guard);
		if (Labels.TryGetValue(key, out (string, Color, int, long) value) && EstUnAvertissement(value.Item1) && value.Item4 >= depuis)
		{
			return Labels.Remove(key);
		}
		return false;
	}

	internal static string? MotDe(Guid entiteId)
	{
		if (!Labels.TryGetValue((entiteId, LabelKind.Guard), out (string, Color, int, long) value))
		{
			return null;
		}
		return value.Item1;
	}

	internal static void OublierLesMots()
	{
		Labels.Clear();
		PlacesDesMots.Clear();
	}

	internal static int TailleDuMot(string? texte, int taille)
	{
		if (!EstUnAvertissement(texte))
		{
			return Math.Max(1, (int)MathF.Round((float)taille * 0.74f));
		}
		return taille;
	}

	internal static (float X, float Y) PlaceDuMot(Entity entite, string texte, long neA, float x, float y, float largeur, float hauteur, long maintenant)
	{
		if (!PlacesDesMots.TryGetValue(entite.Id, out var value) || value.NeA != neA)
		{
			OublierLesPlacesFinies(maintenant);
			value = new PlaceDUnMot
			{
				NeA = neA
			};
			if (!EstUnAvertissement(texte) && Heritage.Chiffres.HautDesChiffres(entite.Id, texte, neA, maintenant, out var haut))
			{
				value.Releve = Heritage.Chiffres.AuDessusDuChiffre(y + hauteur, haut);
			}
		}
		y += value.Releve;
		float vers = 0f;
		Player me = Globals.Me;
		if (me != null && me.Id != entite.Id)
		{
			FloatRect bodyWorldPos = me.BodyWorldPos;
			float num = x + largeur / 2f;
			if (bodyWorldPos.Width > 0f && bodyWorldPos.Height > 0f && Heritage.Chiffres.Couvre(num, largeur / 2f, y, y + hauteur, bodyWorldPos, Echanges.Hauteur(me), me.RayonDuCorps))
			{
				float num2 = bodyWorldPos.X + bodyWorldPos.Width / 2f;
				if (value.Cote == 0)
				{
					FloatRect bodyWorldPos2 = entite.BodyWorldPos;
					value.Cote = (sbyte)Heritage.Chiffres.CoteDeLAuteur(bodyWorldPos2.X + bodyWorldPos2.Width / 2f, num2);
				}
				vers = Heritage.Chiffres.ACoteDeLAuteur(num2, me.RayonDuCorps, largeur / 2f, value.Cote) - num;
			}
		}
		value.DecaleX = Heritage.Chiffres.Glisse(value.DecaleX, vers, Heritage.Chiffres.Pas(value.Vu, maintenant));
		value.Vu = maintenant;
		PlacesDesMots[entite.Id] = value;
		return (X: x + value.DecaleX, Y: y);
	}

	internal static float ReleveDuMot(Guid entiteId, long neA)
	{
		if (!PlacesDesMots.TryGetValue(entiteId, out var value) || value.NeA != neA)
		{
			return 0f;
		}
		return value.Releve;
	}

	private static void OublierLesPlacesFinies(long maintenant)
	{
		if (PlacesDesMots.Count < 32)
		{
			return;
		}
		foreach (var (key, placeDUnMot2) in PlacesDesMots)
		{
			if (maintenant - placeDUnMot2.NeA > 700 || maintenant < placeDUnMot2.NeA)
			{
				PlacesDesMots.Remove(key);
			}
		}
	}

	internal static bool ChasseParLeMotDuServeur(Entity entite, string mot, long neA)
	{
		if (entite.MapInstance is MapInstance mapInstance && mapInstance.ActionMessages.Count > 0)
		{
			return Departager(mapInstance.ActionMessages, entite.Id, EstUnAvertissement(mot), neA);
		}
		return false;
	}

	internal static bool Departager(List<IActionMessage> textes, Guid entiteId, bool avertissement, long neA)
	{
		bool result = false;
		for (int num = textes.Count - 1; num >= 0; num--)
		{
			if (textes[num] is ActionMessage actionMessage && !(actionMessage.EntityId != entiteId) && Heritage.Chiffres.EstLeMotDuCritique(actionMessage.Text))
			{
				if (!avertissement && actionMessage.CreatedAt > neA)
				{
					result = true;
				}
				else
				{
					textes.RemoveAt(num);
				}
			}
		}
		return result;
	}

	internal static void OublierLesArrets()
	{
		_hitStopUntil = 0L;
		_dernierArretA = -150L;
		_dernierArretMs = 0L;
	}

	private static bool PreditParLHeritage(Entity target, long now, Guid attackerId)
	{
		if (attackerId != Guid.Empty)
		{
			Guid? obj = Globals.Me?.Id;
			if (attackerId != obj)
			{
				goto IL_005a;
			}
		}
		if (Heritage.ImpactsPredits.Sortir(target.Id, now, out var joueA))
		{
			HeritageAllureOptions allure = Options.Instance.Combat.Heritage.Allure;
			target.HitWhiteUntil = joueA + (allure.Actif ? Heritage.Coups.Blanc(allure) : 70);
			return true;
		}
		goto IL_005a;
		IL_005a:
		return false;
	}

	internal static bool MotDe(Guid entiteId, long maintenant, out MotLu mot)
	{
		mot = default(MotLu);
		if (!Labels.TryGetValue((entiteId, LabelKind.Guard), out (string, Color, int, long) value) || string.IsNullOrEmpty(value.Item1))
		{
			return false;
		}
		long num = maintenant - value.Item4;
		if (num < 0 || num > 700)
		{
			return false;
		}
		if (Labels.TryGetValue((entiteId, LabelKind.Combo), out (string, Color, int, long) value2) && value2.Item4 > value.Item4)
		{
			return false;
		}
		mot = new MotLu(value.Item1, value.Item3, value.Item4);
		return true;
	}

	internal static float BasDuMot(FloatRect cadre, long age)
	{
		return cadre.Y + cadre.Height * 0.05f - 14f * MathF.Sqrt(Math.Clamp((float)age / 700f, 0f, 1f));
	}

	internal static float EchelleDuMot(long age)
	{
		if (age >= 120)
		{
			return 1f;
		}
		return 1.4f - 0.4f * ((float)Math.Max(0L, age) / 120f);
	}
}
