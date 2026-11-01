using System;
using System.Linq;
using System.Runtime.Serialization;
using Intersect.Framework.Core.Entities;
using Newtonsoft.Json;

namespace Intersect.Config;

public class BeamOptions
{
	public Guid[] SpellIds { get; set; } = new Guid[1]
	{
		new Guid("7C1D5A3E-4F21-4B6D-9A83-5E1C0D2E3F10")
	};

	public int[] ChargeMs { get; set; } = new int[3] { 0, 900, 1800 };

	public int MaxChargeMs { get; set; } = 3000;

	public int[] RangeTiles { get; set; } = new int[3] { 4, 6, 8 };

	public int[] HoldMs { get; set; } = new int[3] { 900, 1500, 2300 };

	public float HeadTilesPerSecond { get; set; } = 47f;

	public int TickMs { get; set; } = 300;

	public int[] DamagePercent { get; set; } = new int[3] { 45, 55, 70 };

	public float[] KiCostPercent { get; set; } = new float[3] { 12f, 20f, 30f };

	public int[] PushEveryMs { get; set; } = new int[3] { 900, 600, 450 };

	public int MaxPushTiles { get; set; } = 3;

	public int FadeMs { get; set; } = 260;

	public float CourbeMaxDegres { get; set; } = 60f;

	public float CourbeDegresParSeconde { get; set; } = 180f;

	[JsonIgnore]
	public int LevelCount => Math.Max(1, new int[6] { ChargeMs.Length, RangeTiles.Length, HoldMs.Length, DamagePercent.Length, KiCostPercent.Length, PushEveryMs.Length }.Min());

	private static HeritageTechniquesOptions PaliersHeritage
	{
		get
		{
			if (HeritageRegles.Actif)
			{
				HeritageTechniquesOptions techniques = Options.Instance.Combat.Heritage.Techniques;
				if (techniques != null && techniques.RayonPalier2Ms > 0)
				{
					return techniques;
				}
			}
			return null;
		}
	}

	[JsonIgnore]
	public int DepartForce
	{
		get
		{
			HeritageTechniquesOptions paliersHeritage = PaliersHeritage;
			if (paliersHeritage == null || paliersHeritage.RayonDepartForceMs <= 0)
			{
				return MaxChargeMs;
			}
			return paliersHeritage.RayonDepartForceMs;
		}
	}

	[JsonIgnore]
	public int DernierPalier => PaliersHeritage?.RayonPalier3Ms ?? ChargeMs[^1];

	[OnDeserialized]
	internal void OnDeserializedMethod(StreamingContext context)
	{
		BeamOptions beamOptions = new BeamOptions();
		if (SpellIds == null)
		{
			Guid[] array = (SpellIds = Array.Empty<Guid>());
		}
		int[] chargeMs = ChargeMs;
		ChargeMs = ((chargeMs != null && chargeMs.Length > 0) ? ChargeMs : beamOptions.ChargeMs);
		chargeMs = RangeTiles;
		RangeTiles = ((chargeMs != null && chargeMs.Length > 0) ? RangeTiles : beamOptions.RangeTiles);
		chargeMs = HoldMs;
		HoldMs = ((chargeMs != null && chargeMs.Length > 0) ? HoldMs : beamOptions.HoldMs);
		chargeMs = DamagePercent;
		DamagePercent = ((chargeMs != null && chargeMs.Length > 0) ? DamagePercent : beamOptions.DamagePercent);
		float[] kiCostPercent = KiCostPercent;
		KiCostPercent = ((kiCostPercent != null && kiCostPercent.Length > 0) ? KiCostPercent : beamOptions.KiCostPercent);
		chargeMs = PushEveryMs;
		PushEveryMs = ((chargeMs != null && chargeMs.Length > 0) ? PushEveryMs : beamOptions.PushEveryMs);
		CourbeMaxDegres = (float.IsFinite(CourbeMaxDegres) ? Math.Clamp(CourbeMaxDegres, 0f, 170f) : beamOptions.CourbeMaxDegres);
		CourbeDegresParSeconde = ((float.IsFinite(CourbeDegresParSeconde) && CourbeDegresParSeconde > 0f) ? CourbeDegresParSeconde : beamOptions.CourbeDegresParSeconde);
	}

	public float Courbure(float degres, int level, int tileWidth)
	{
		if (!float.IsFinite(degres) || RangeTiles.Length == 0)
		{
			return 0f;
		}
		int num = RangeTiles[Math.Clamp(level, 0, RangeTiles.Length - 1)] * Math.Max(1, tileWidth);
		float num2 = MathF.Abs(CourbeMaxDegres);
		if (num > 0)
		{
			return Math.Clamp(degres, 0f - num2, num2) * (float)Math.PI / 180f / (float)num;
		}
		return 0f;
	}

	public int LevelFor(long chargedMs)
	{
		HeritageTechniquesOptions paliersHeritage = PaliersHeritage;
		if (paliersHeritage != null)
		{
			int value = HeritageRegles.PalierDuRayon(chargedMs, paliersHeritage.RayonPalier2Ms, paliersHeritage.RayonPalier3Ms);
			return Math.Clamp(value, 0, LevelCount - 1);
		}
		int value2 = 0;
		for (int i = 1; i < ChargeMs.Length; i++)
		{
			if (chargedMs >= ChargeMs[i])
			{
				value2 = i;
			}
		}
		return Math.Clamp(value2, 0, LevelCount - 1);
	}

	public long TravelMs(int tiles)
	{
		return (long)Math.Ceiling((float)tiles * 1000f / Math.Max(1f, HeadTilesPerSecond));
	}

	public bool IsBeamSpell(Guid spellId)
	{
		if (spellId != Guid.Empty)
		{
			return Array.IndexOf(SpellIds, spellId) >= 0;
		}
		return false;
	}
}
