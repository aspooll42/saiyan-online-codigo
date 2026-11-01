using System;
using System.Linq;

namespace Intersect.Config;

public class CombatOptions
{
	public int ArmementMs { get; set; } = 400;

	public int ArmementLourdMs { get; set; } = 600;

	public int ArmementImparableMs { get; set; } = 800;

	public int ArmementImparableTousLes { get; set; } = 3;

	public int ArmementEcartMs { get; set; } = 350;

	public int ArmementMarqueApresMs { get; set; } = 8000;

	public int MonstresDegatsPourcent { get; set; } = 125;

	public int EsquiveJetons { get; set; } = 4;

	public int[] EsquiveJetonsNiveaux { get; set; } = new int[4] { 20, 55, 95, 120 };

	public int NiveauDeLEsquive
	{
		get
		{
			int[] esquiveJetonsNiveaux = EsquiveJetonsNiveaux;
			if (esquiveJetonsNiveaux == null || esquiveJetonsNiveaux.Length <= 0)
			{
				return 1;
			}
			return Math.Max(1, esquiveJetonsNiveaux.Min());
		}
	}

	public int EsquiveJetonRechargeMs { get; set; } = 3000;

	public int RetablirApresMs { get; set; } = 200;

	public int RetablirGratuitMs { get; set; } = 300;

	public int RetablirIntouchableMs { get; set; } = 300;

	public int GardeParfaiteRepriseMs { get; set; } = 400;

	public int CoupDureePourcent { get; set; } = 90;

	public int CoupFinalDureePourcent { get; set; } = 120;

	public int RueeApresProjectionMs { get; set; } = 1000;

	public string[] CombattantsSprites { get; set; } = new string[21]
	{
		"Gohan", "Piccolo", "ninja_bleu", "ninja_rouge", "kuma_brun", "kuma_roux", "kuma_bleu", "eggbot_rouge", "eggbot_bleu", "eggbot_vert",
		"eggbot_or", "destroyer_rouge", "destroyer_vert", "destroyer_or", "ten-shin-han", "Goku", "Vegeta", "Gohan_ado", "goten_kid", "trunk_kid",
		"trunk_futur"
	};

	public int ArmementEnchaineMs { get; set; } = 300;

	public int MonstresComboFenetreMs { get; set; } = 700;

	public int MonstresComboRecuperationMs { get; set; } = 400;

	public int MonstresCoupFinalPourcent { get; set; } = 130;

	public int ChocMonstreDelaiMs { get; set; } = 8000;

	public int AutresJoueursRetardMs { get; set; } = 50;

	public int AutresJoueursPredictionMs { get; set; } = 70;

	public int AdversaireDuMomentMs { get; set; } = 6000;

	public ClashOptions Clash { get; set; } = new ClashOptions();

	public int BlockingSlow { get; set; } = 30;

	public int AttackMovementSlow { get; set; }

	public int CombatTime { get; set; } = 10000;

	public int MaxAttackRate { get; set; } = 200;

	public int MaxDashSpeed { get; set; } = 200;

	public int MinAttackRate { get; set; } = 500;

	public int RegenTime { get; set; } = 3000;

	public int ComboHits { get; set; } = 3;

	public int ComboWindow { get; set; } = 700;

	public int ComboFinisherDamagePercent { get; set; } = 150;

	public int ComboFinisherKnockback { get; set; } = 1;

	public int ProjectionCases { get; set; } = 6;

	public int ProjectionMs { get; set; } = 520;

	public int ProjectionAuSolMs { get; set; } = 800;

	public int RueeNiveau { get; set; } = 1;

	public int RueeCases { get; set; } = 4;

	public int RueeDelaiMs { get; set; } = 1000;

	public int PoursuiteNiveau { get; set; } = 1;

	public int PoursuiteKiPourcent { get; set; } = 20;

	public int PoursuitesMax { get; set; } = 1;

	public int DisparitionKiPourcent { get; set; } = 10;

	public int BondPixels { get; set; } = 26;

	public int BondMs { get; set; } = 170;

	public int ProtectionMs { get; set; } = 5000;

	public bool CoupChargeActif { get; set; } = true;

	public int CoupChargeDebutMs { get; set; } = 220;

	public int CoupChargePleineMs { get; set; } = 680;

	public int CoupChargeLacherMs { get; set; } = 1400;

	public int CoupChargeArmementMs { get; set; } = 140;

	public int CoupChargeDegatsPourcent { get; set; } = 250;

	public int CoupChargeSonneMs { get; set; } = 500;

	public int CoupChargeReculPixels { get; set; } = 20;

	public int ChocEcartMs { get; set; } = 150;

	public int ChocSimultaneMs { get; set; } = 60;

	public int ChocReculPixels { get; set; } = 32;

	public int ChocDemiPasPixels { get; set; } = 16;

	public int ChocSonneMs { get; set; } = 300;

	public int ChocPauseMs { get; set; } = 200;

	public int ChocDelaiMs { get; set; } = 1200;

	public int AvantageMs { get; set; } = 400;

	public int KiParCoupPourcent { get; set; } = 8;

	public int KiParCoupFinalPourcent { get; set; } = 12;

	public int KiParEcrasementPourcent { get; set; } = 15;

	public int ChargeKiSurprisPourcent { get; set; } = 25;

	public int RendementsDecroissantsMs { get; set; } = 10000;

	public int JcJDegatsPourcent { get; set; } = 70;

	public int RecuperationKiPourcent { get; set; } = 15;

	public int RenvoiDegatsPourcent { get; set; } = 150;

	public int QuillesRayonPixels { get; set; } = 18;

	public int QuillesSonneMs { get; set; } = 500;

	public int KiaiRayonPixels { get; set; } = 56;

	public int KiaiReculPixels { get; set; } = 44;

	public int KiaiSonneMs { get; set; } = 450;

	public int OndeDeChocPorteePixels { get; set; } = 76;

	public int OndeDeChocLargeurPixels { get; set; } = 30;

	public int OndeDeChocReculPixels { get; set; } = 34;

	public int OndeDeChocSonneMs { get; set; } = 600;

	public int RiposteMs { get; set; } = 1000;

	public int ExplosionKiMinPourcent { get; set; } = 30;

	public float ExplosionDegatsParPointDeKi { get; set; } = 0.8f;

	public int ExplosionRayonPixels { get; set; } = 70;

	public int ExplosionReculPixels { get; set; } = 52;

	public int ExplosionSonneMs { get; set; } = 500;

	public int EncerclementAttaquants { get; set; } = 2;

	public int EncerclementRayonPixels { get; set; } = 70;

	public int EncerclementJetonMs { get; set; } = 2200;

	public int EncerclementPauseMs { get; set; } = 1500;

	public int EncerclementArmementMs { get; set; } = 400;

	public int ElitesKiTirPourcent { get; set; } = 60;

	public int ElitesKiChargePourcent { get; set; } = 20;

	public int ElitesChargeMs { get; set; } = 2600;

	public bool EnableGuard { get; set; } = true;

	public int GuardBase { get; set; } = 100;

	public float GuardPerDefense { get; set; } = 2f;

	public int GuardReduction { get; set; } = 60;

	public float GuardReductionPerDefense { get; set; } = 0.25f;

	public int GuardReductionMax { get; set; } = 85;

	public int GuardDrainBase { get; set; } = 10;

	public float GuardDrainFromDamage { get; set; } = 1f;

	public int GuardRegenDelay { get; set; } = 1200;

	public int GuardRegenPercentPerSecond { get; set; } = 30;

	public int PerfectGuardWindow { get; set; } = 200;

	public int PerfectGuardStagger { get; set; } = 700;

	public int RiposteWindow { get; set; } = 1200;

	public int GuardBreakStun { get; set; } = 900;

	public int GuardBreakCooldown { get; set; } = 2500;

	public bool EnableVanish { get; set; } = true;

	public int VanishWindow { get; set; } = 250;

	public int VanishCooldown { get; set; } = 1500;

	public int VanishKiCostPercent { get; set; } = 10;

	public int VanishBehindRange { get; set; } = 2;

	public int VanishSafeTime { get; set; } = 300;

	public bool EnableKiClash { get; set; } = true;

	public int KiClashTiePercent { get; set; } = 80;

	public int KiClashAbsorbPercent { get; set; } = 50;

	public bool EnableCombatChatMessages { get; set; }

	public bool MovementCancelsCast { get; set; }

	public bool MatchGroupCooldowns { get; set; } = true;

	public bool MatchGroupCooldownHighest { get; set; } = true;

	public bool LinkSpellAndItemCooldowns { get; set; } = true;

	public bool EnableGlobalCooldowns { get; set; }

	public int GlobalCooldownDuration { get; set; } = 1500;

	public int MaxPlayerAutoTargetRadius { get; set; } = 15;

	public bool RegenVitalsInCombat { get; set; }

	public bool EnableTurnAroundWhileCasting { get; set; }

	public bool EnableTargetWindow { get; set; } = true;

	public bool EnableAutoSelfCastFriendlySpellsWhenTargetingHostile { get; set; }

	public bool EnableAllPlayersFriendlyInSafeZone { get; set; }

	public int DashPixels { get; set; } = 96;

	public bool DashTombeDansLeVide { get; set; } = true;

	public int DashMs { get; set; } = 150;

	public int DashRechargeMs { get; set; } = 1000;

	public int DashCharges { get; set; } = 3;

	public int[] DashChargesNiveaux { get; set; } = new int[3] { 10, 35, 75 };

	public int DashChargeRechargeMs { get; set; } = 2000;

	public int DashEcartMs { get; set; } = 250;

	public int NiveauDuDash
	{
		get
		{
			int[] dashChargesNiveaux = DashChargesNiveaux;
			if (dashChargesNiveaux == null || dashChargesNiveaux.Length <= 0)
			{
				return 1;
			}
			return Math.Max(1, dashChargesNiveaux.Min());
		}
	}

	public int DashKiPourcent { get; set; }

	public int DashCourseApresMs { get; set; } = 200;

	public int EngagementDuCoupPourcent { get; set; } = 100;

	public int RecuperationApresLeCoupMs { get; set; } = 150;

	public int EngagementMargeMs { get; set; } = 60;

	public HeritageOptions Heritage { get; set; } = new HeritageOptions();

	public GardeOptions Garde { get; set; } = new GardeOptions();

	public PassifsOptions Passifs { get; set; } = new PassifsOptions();

	public int JetonsDEsquivePourNiveau(int niveau)
	{
		if (EsquiveJetons <= 0)
		{
			return 0;
		}
		int[] esquiveJetonsNiveaux = EsquiveJetonsNiveaux;
		int value = ((esquiveJetonsNiveaux != null && esquiveJetonsNiveaux.Length > 0) ? esquiveJetonsNiveaux.Count((int seuil) => niveau >= seuil) : EsquiveJetons);
		return Math.Clamp(value, 0, EsquiveJetons);
	}

	public int ChargesDeDashPourNiveau(int niveau)
	{
		if (DashCharges <= 0)
		{
			return 0;
		}
		int[] dashChargesNiveaux = DashChargesNiveaux;
		int value = ((dashChargesNiveaux != null && dashChargesNiveaux.Length > 0) ? dashChargesNiveaux.Count((int seuil) => niveau >= seuil) : DashCharges);
		return Math.Clamp(value, 0, DashCharges);
	}

	public long FinDeLEngagement(long debut, long dureeDuCoup)
	{
		return debut + Math.Max(0L, dureeDuCoup) * Math.Clamp(EngagementDuCoupPourcent, 0, 100) / 100 + Math.Max(0, RecuperationApresLeCoupMs);
	}
}
