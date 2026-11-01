using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Intersect.Client.Core;
using Intersect.Client.Core.Controls;
using Intersect.Client.Core.Sounds;
using Intersect.Client.Dungeons;
using Intersect.Client.Entities.Events;
using Intersect.Client.Entities.Projectiles;
using Intersect.Client.Framework.Content;
using Intersect.Client.Framework.Entities;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Input;
using Intersect.Client.Framework.Items;
using Intersect.Client.Framework.Maps;
using Intersect.Client.General;
using Intersect.Client.Housing;
using Intersect.Client.Interface;
using Intersect.Client.Interface.Game.Bank;
using Intersect.Client.Interface.Game.Chat;
using Intersect.Client.Interface.Game.Clan;
using Intersect.Client.Interface.Game.EntityPanel;
using Intersect.Client.Interface.Game.Techniques;
using Intersect.Client.Interface.Shared;
using Intersect.Client.Items;
using Intersect.Client.Localization;
using Intersect.Client.Maps;
using Intersect.Client.Networking;
using Intersect.Client.Spells;
using Intersect.Config;
using Intersect.Config.Guilds;
using Intersect.Configuration;
using Intersect.Core;
using Intersect.Enums;
using Intersect.Extensions;
using Intersect.Framework.Core;
using Intersect.Framework.Core.Entities;
using Intersect.Framework.Core.GameObjects.Events;
using Intersect.Framework.Core.GameObjects.Items;
using Intersect.Framework.Core.GameObjects.Maps;
using Intersect.Framework.Core.GameObjects.Maps.Attributes;
using Intersect.Framework.Core.GameObjects.Ornaments;
using Intersect.Framework.Core.GameObjects.PlayerClass;
using Intersect.Framework.Core.GameObjects.Resources;
using Intersect.Framework.Core.GameObjects.Titles;
using Intersect.GameObjects;
using Intersect.Models;
using Intersect.Network;
using Intersect.Network.Packets.Client;
using Intersect.Network.Packets.Server;
using Microsoft.Extensions.Logging;

namespace Intersect.Client.Entities;

public class Player : Entity, IPlayer, IEntity, IDisposable
{
	public readonly record struct Charge(int Porte, int Capacite, int ParLeNiveau, int ParLesMetiers, int NiveauxDeMetier, int ParLaCapsule, Guid Capsule)
	{
		public bool Surcharge => Porte > Capacite;

		public bool PresquePleine
		{
			get
			{
				if (!Surcharge && Capacite > 0)
				{
					return (long)Porte * 10L >= (long)Capacite * 9L;
				}
				return false;
			}
		}

		public int Pourcent
		{
			get
			{
				if (Capacite > 0)
				{
					return (int)Math.Round((double)Porte * 100.0 / (double)Capacite);
				}
				return 0;
			}
		}
	}

	public delegate void InventoryUpdatedEventHandler(Player player, int slotIndex);

	private class TargetInfo
	{
		public long LastTimeSelected;

		public int DistanceTo;
	}

	private enum EtapeDeRecolteAuClic
	{
		Aucune,
		EnRoute,
		Recolte
	}

	internal const int CibleTenueCases = 6;

	private Guid _adversaireDuMoment;

	private long _adversaireDuMomentJusqua;

	public const float BeamTurnDegreesPerSecond = 300f;

	private const long BeamAimSendMs = 100L;

	private const float BeamAimSendDegrees = 2f;

	private const float BeamAimDeadZone = 10f;

	private int _beamSlot = -1;

	private Guid _beamSpellId;

	private bool _beamWaitRelease;

	private float _beamAim;

	private float _beamAimGoal;

	private float _beamAimSent;

	private long _beamAimSentAt;

	private long _beamAimUpdatedAt;

	private bool _beamMouseLeads = true;

	private Vector2 _beamMouseSeen;

	private Direction _beamArrowSeen = Direction.None;

	private float _beamCourbeSent;

	private long _beamCourbeSentAt;

	private long _beamCourbeUpdatedAt;

	private bool _beamCourbeSouris;

	private const int ChargeRelueMs = 100;

	private const int SurchargeDiteMs = 3000;

	private long _chargeLueA = long.MinValue;

	private Charge _charge;

	private long _surchargeDiteA = long.MinValue;

	private byte _pasLocal;

	private Guid _pasCibleId;

	private long _pasExpireA;

	private bool _poseDeLAppuiMontree;

	private Guid _class;

	private Label? _serverHeaderLabel;

	private bool _showsAggressorLabel;

	private Entity? mLastBumpedEvent;

	private List<IPartyMember>? mParty;

	private long mlastTargetScanTime;

	private Guid mlastTargetScanMap = Guid.Empty;

	private Point mlastTargetScanLocation = new Point(-1, -1);

	private readonly Dictionary<Entity, TargetInfo> mlastTargetList = new Dictionary<Entity, TargetInfo>();

	private Entity? mLastEntitySelected;

	private readonly Dictionary<int, long> mLastHotbarUseTime = new Dictionary<int, long>();

	private readonly int mHotbarUseDelay = 150;

	public string GuildTag = string.Empty;

	public int GuildEmblem;

	public int GuildColor;

	public GuildMember[] GuildMembers = Array.Empty<GuildMember>();

	public string GuildMotd = string.Empty;

	public GuildActivityEntry[] GuildActivity = Array.Empty<GuildActivityEntry>();

	public GuildDirectoryEntry[] GuildDirectory = Array.Empty<GuildDirectoryEntry>();

	public string GuildDirectoryQuery = string.Empty;

	public int GuildDirectoryPage;

	public int GuildDirectoryTotal;

	public GuildApplication[] GuildApplications = Array.Empty<GuildApplication>();

	public string GuildAppliedTo = string.Empty;

	public GuildProfile? GuildProfile;

	public int GuildLevel = 1;

	public long GuildExperience;

	public long GuildExperienceToNext;

	public long GuildWeekExperience;

	public GuildObjective[] GuildObjectives = Array.Empty<GuildObjective>();

	public long GuildObjectivesEndAt;

	private const float ProjectileAimCone = 0.38f;

	private bool _sprintExhausted;

	private bool _chargeCoupee;

	private long _coupSuivi;

	private long _finDeLEngagement;

	private const long AttackBufferMs = 200L;

	private bool _attackHeldLastFrame;

	private long _attackBufferedUntil;

	private const long AttackApproachMs = 350L;

	private long _attackApproachUntil;

	private int _bufferedSpellSlot = -1;

	private long _bufferedSpellUntil;

	private bool _dernierCoupRecolte;

	private bool _recolteRefusee;

	private const int PorteeDeParole = 2;

	private const long DelaiDeParole = 500L;

	private long _prochaineParole;

	private bool _appuiRendu;

	private IEntity? _target;

	private long _lastWallBumpAt;

	private bool _dashTenuAvant;

	private long _dashAppuiDepuis = -1L;

	private long _dashPretLocal;

	private int _chargesDash = -1;

	private long _chargeDashProchaineA;

	private int _chargesDashMaximumConnu = -1;

	private long _dashAuNiveauDitA;

	private static readonly Color OrangeDash = new Color(255, 255, 170, 80);

	private byte _libreEpoque;

	private ushort _libreSequence;

	private long _libreProchainEnvoi;

	private bool _libreBougeait;

	private Guid _libreEnvoiCarte;

	private float _libreEnvoiX;

	private float _libreEnvoiY;

	private Direction _libreEnvoiDirection = Direction.None;

	private double _libreDerniereImage;

	private readonly Dictionary<(int, int), bool> _libreCasesDeLImage = new Dictionary<(int, int), bool>(32);

	private float _libreAvanceReportee;

	private readonly HashSet<(int, int)> _libreCasesEnvahies = new HashSet<(int, int)>(4);

	private const float VitesseDeSortieDesCorps = 240f;

	private readonly (float Distance, float X, float Y)[] _libreSorties = new(float, float, float)[4];

	private readonly List<(Guid Map, int X, int Y)> _casesDevant = new List<(Guid, int, int)>(3);

	private static readonly float[] SondesLaterales = new float[3] { 0f, 1f, -1f };

	private static readonly CorpsLibre.LignesDeLaCase LignesDuMonde = LignesDeLaCaseDuMonde;

	private long _rueePrete;

	private long _rueeEnCoursJusqua;

	private long _appuiDeCoupA;

	private bool _coupPartiDeCetAppui;

	private bool _appuiDeRecolte;

	private bool _chargeDeCetAppui;

	private bool _appuiEnAttente;

	private bool _appuiImmediat;

	private bool _coupAuRelacher;

	private bool _hRueeSurDirectionTapee;

	private long _hDernierFrontUtilise = long.MinValue;

	private bool _hCompteGarde;

	private byte _hPasAvant;

	private Guid _hPasCibleAvant;

	private long _hPasExpireAvant;

	private long _hGardeSousLeSonne;

	internal const int RueeAttenteMs = 400;

	private Guid _hRueeCibleId;

	private long _hRueeJusqua;

	private int _maitreSlot = -1;

	private Guid _maitreSortId;

	private bool _maitreAttendRelache;

	private float _maitreVisee;

	private float _maitreViseeEnvoyee;

	private long _maitreViseeEnvoyeeA;

	private long _maitreViseeA;

	private readonly List<(float X, float PiedsY, float Rayon)> _libreMonstres = new List<(float, float, float)>(8);

	private const float PorteeDuReleve = 120f;

	private Guid _classeDuPassif = Guid.Empty;

	private PassifDeClasse _passifDeLaClasse;

	private const float RamassageAutoRayon = 36f;

	private const int RamassageAutoRelanceMs = 1200;

	private const int RamassageAutoLacherMs = 1500;

	private const int RamassageAutoAvertissementMs = 6000;

	private readonly Dictionary<Guid, long> _ramassageEssais = new Dictionary<Guid, long>();

	private readonly HashSet<Guid> _objetsConnus = new HashSet<Guid>();

	private readonly HashSet<Guid> _objetsJetesParMoi = new HashSet<Guid>();

	private long _dernierLacher = -1L;

	private long _dernierAvertissementSacPlein = -6000L;

	private EtapeDeRecolteAuClic _recolteEtape;

	private Resource? _recolteCible;

	private readonly List<(float X, float Y)> _recolteChemin = new List<(float, float)>();

	private float _recolteDerniereX;

	private float _recolteDerniereY;

	private long _recolteDernierProgres;

	private int _recolteEchecs;

	private bool _recolteClicTenu;

	private const int RecolteBlocageMs = 1500;

	private const int RecolteNoeudsMax = 6000;

	public bool IsBeaming
	{
		get
		{
			if (!KiBeam.IsActive(this))
			{
				return Maitres.IsActive(this);
			}
			return true;
		}
	}

	public bool EstSurcharge
	{
		get
		{
			if (this == Globals.Me)
			{
				return LireLaCharge().Surcharge;
			}
			return false;
		}
	}

	private static int CoupsDuCombo => Math.Max(1, Options.Instance.Combat.ComboHits);

	public Guid Class
	{
		get
		{
			return _class;
		}
		set
		{
			if (!(_class == value))
			{
				_class = value;
				LoadAnimationTexture(string.IsNullOrWhiteSpace(TransformedSprite) ? Sprite : TransformedSprite, SpriteAnimations.Attack);
			}
		}
	}

	public Access AccessLevel { get; set; }

	public long Experience { get; set; }

	public long Zeni { get; set; }

	public Guid CurrencyItemId { get; set; }

	public int Karma { get; set; }

	public bool KarmaKnown { get; set; }

	public KarmaTier KarmaTier { get; set; }

	public long AggressorUntil { get; set; }

	public bool IsAggressor => AggressorUntil > Timing.Global.Milliseconds;

	public long HellSentenceUntil { get; set; }

	public long HellBail { get; set; }

	public DetentionPlace DetentionPlace { get; set; }

	public bool IsDetained => HellSentenceUntil > 0;

	public bool IsInHell
	{
		get
		{
			if (IsDetained)
			{
				return DetentionPlace == DetentionPlace.Hell;
			}
			return false;
		}
	}

	public long HellSentenceRemainingMs => Math.Max(0L, HellSentenceUntil - Timing.Global.Milliseconds);

	public long ExperienceToNextLevel { get; set; }

	public bool ExperienceReceived { get; set; }

	IReadOnlyList<IFriendInstance> IPlayer.Friends => Friends;

	public List<IFriendInstance> Friends { get; set; } = new List<IFriendInstance>();

	IReadOnlyList<IHotbarInstance> IPlayer.HotbarSlots => Hotbar.ToList();

	public HotbarInstance[] Hotbar { get; set; } = new HotbarInstance[Options.Instance.Player.HotbarSlotCount];

	IReadOnlyDictionary<Guid, long> IPlayer.ItemCooldowns => ItemCooldowns;

	public Dictionary<Guid, long> ItemCooldowns { get; set; } = new Dictionary<Guid, long>();

	public override Direction DirectionMoving
	{
		get
		{
			return base.DirectionMoving;
		}
		set
		{
			base.DirectionMoving = value;
		}
	}

	IReadOnlyDictionary<Guid, QuestProgress> IPlayer.QuestProgress => QuestProgress;

	public Dictionary<Guid, QuestProgress> QuestProgress { get; set; } = new Dictionary<Guid, QuestProgress>();

	public Guid[] HiddenQuests { get; set; } = Array.Empty<Guid>();

	IReadOnlyDictionary<Guid, long> IPlayer.SpellCooldowns => SpellCooldowns;

	public Dictionary<Guid, long> SpellCooldowns { get; set; } = new Dictionary<Guid, long>();

	public int StatPoints { get; set; }

	public EntityBox? TargetBox { get; set; }

	public PlayerStatusWindow? StatusWindow { get; set; }

	public Guid TargetId { get; set; }

	TargetType IPlayer.TargetType => (TargetType)TargetType;

	public int TargetType { get; set; }

	public long CombatTimer { get; set; }

	public long IsCastingCheckTimer { get; set; }

	public long GlobalCooldown { get; set; }

	public string? Guild { get; set; }

	string IPlayer.GuildName => Guild ?? string.Empty;

	public int Rank { get; set; }

	public bool IsInGuild => !string.IsNullOrWhiteSpace(Guild);

	public GuildRank? GuildRank
	{
		get
		{
			if (!IsInGuild)
			{
				return null;
			}
			return Options.Instance.Guild.Ranks[Math.Max(0, Math.Min(Rank, Options.Instance.Guild.Ranks.Length - 1))];
		}
	}

	IReadOnlyList<IPartyMember> IPlayer.PartyMembers => Party;

	public List<IPartyMember> Party
	{
		get
		{
			if (mParty == null)
			{
				mParty = new List<IPartyMember>();
			}
			return mParty;
		}
	}

	public override Guid MapId
	{
		get
		{
			return base.MapId;
		}
		set
		{
			if (!(value != base.MapId))
			{
				return;
			}
			MapInstance mapInstance = Intersect.Client.Maps.MapInstance.Get(base.MapId);
			MapInstance mapInstance2 = Intersect.Client.Maps.MapInstance.Get(value);
			base.MapId = value;
			if (Globals.Me == this)
			{
				if (Intersect.Client.Maps.MapInstance.Get(Globals.Me.MapId) != null)
				{
					Audio.PlayMusic(Intersect.Client.Maps.MapInstance.Get(Globals.Me.MapId).Music, ClientConfiguration.Instance.MusicFadeTimer, ClientConfiguration.Instance.MusicFadeTimer, loop: true);
				}
				if (mapInstance2 != null && mapInstance != null)
				{
					mapInstance2.CompareEffects(mapInstance);
				}
			}
		}
	}

	bool IPlayer.IsInParty => IsInParty();

	public bool IsBusy
	{
		get
		{
			if (this != Globals.Me || !base.IsDying)
			{
				if (Globals.EventHolds.Count == 0 && !Globals.MoveRouteActive && Globals.GameShop == null && !Globals.InBank && !Globals.InCraft && !Globals.InTrade)
				{
					return Intersect.Client.Interface.Interface.HasInputFocus();
				}
				return true;
			}
			return true;
		}
	}

	public Guid DernierInterlocuteur { get; private set; }

	public long DernierInterlocuteurA { get; private set; }

	public IEntity? Target
	{
		get
		{
			return _target;
		}
		set
		{
			if (value != _target)
			{
				_target = value;
				if (Heritage.Actif && value != null)
				{
					Heritage.Cible.CibleDesignee(this, value.Id);
				}
				if (value == null)
				{
					TargetId = default(Guid);
					TargetType = 0;
				}
				else
				{
					TargetId = value.Id;
					TargetType = ((value is Event) ? 1 : 0);
				}
				SetTargetBox(value as Entity);
			}
		}
	}

	public override bool CanBeAttacked
	{
		get
		{
			IMapInstance? mapInstance = base.MapInstance;
			if (mapInstance == null || mapInstance.ZoneType != MapZone.Safe)
			{
				Player? me = Globals.Me;
				if (me == null)
				{
					return true;
				}
				return me.MapInstance?.ZoneType != MapZone.Safe;
			}
			return false;
		}
	}

	protected override bool ShouldDrawHpBar
	{
		get
		{
			if (base.ShouldNotDrawHpBar)
			{
				return false;
			}
			if (base.IsHovered)
			{
				return true;
			}
			Player me = Globals.Me;
			if (me == null)
			{
				return false;
			}
			if (Globals.Database.MyOverheadHpBar && base.Id == me.Id)
			{
				return true;
			}
			if (Globals.Database.PlayerOverheadHpBar && base.Id != me.Id)
			{
				return true;
			}
			if (Globals.Database.PartyMemberOverheadHpBar && me.IsInMyParty(this))
			{
				return true;
			}
			if (Globals.Database.FriendOverheadHpBar && me.IsFriend(this))
			{
				return true;
			}
			if (Globals.Database.GuildMemberOverheadHpBar)
			{
				return me.IsGuildMate(this);
			}
			return false;
		}
	}

	public long NouvelleChargeDeDashA { get; private set; }

	public long DashRefuseA { get; private set; }

	public float RechargeDuDash
	{
		get
		{
			long milliseconds = Timing.Global.Milliseconds;
			if (Options.Instance.Combat.DashCharges > 0)
			{
				if (!ChargesDuDash(milliseconds, out var charges, out var _, out var recharge) || charges != 0)
				{
					return 0f;
				}
				return 1f - recharge;
			}
			int num = Math.Max(1, RechargeDuDashPassif(Options.Instance.Combat.DashRechargeMs));
			long num2 = _dashPretLocal - milliseconds;
			if (num2 > 0)
			{
				return Math.Clamp((float)num2 / (float)num, 0f, 1f);
			}
			return 0f;
		}
	}

	public bool EnDeplacementLibre => DeplacementLibre.EstLibre(MapId);

	private bool RueeEnCours => Timing.Global.Milliseconds < _rueeEnCoursJusqua;

	private bool CoupDemande
	{
		get
		{
			if (!_coupAuRelacher)
			{
				if (_appuiImmediat)
				{
					return CoupRepete;
				}
				return false;
			}
			return true;
		}
	}

	private bool CoupRepete
	{
		get
		{
			if (_coupPartiDeCetAppui)
			{
				return _appuiDeRecolte;
			}
			return true;
		}
	}

	private static bool ChargeActive => Options.Instance.Combat.CoupChargeActif;

	internal Guid EnnemiDeLaSerieHeritage
	{
		get
		{
			if (!SerieHeritageOuverte)
			{
				return Guid.Empty;
			}
			return _pasCibleId;
		}
	}

	internal bool RecolteDejaEnCours
	{
		get
		{
			if (!EnRecolteAuClic)
			{
				if (_attackHeldLastFrame && _appuiDeRecolte)
				{
					return _coupPartiDeCetAppui;
				}
				return false;
			}
			return true;
		}
	}

	private static long MemoireDAppuiHeritage => Math.Max(200L, Options.Instance.Combat.Heritage.MemoireAppuiMs);

	private static bool LectureTenue => Globals.Database?.HeritageDirectionTenue ?? false;

	internal bool SerieHeritageOuverte
	{
		get
		{
			if (CoupsDuCombo >= 2 && _pasLocal > 0)
			{
				return Timing.Global.Milliseconds <= _pasExpireA;
			}
			return false;
		}
	}

	internal long AppuiQuiAttendHeritage
	{
		get
		{
			if (!_appuiEnAttente)
			{
				return 0L;
			}
			return Math.Max(1L, _appuiDeCoupA);
		}
	}

	internal bool DernierCoupRefuseHeritage => !_hCompteGarde;

	internal int NamePlateBodyHeight => base.BodyHeight;

	public Guid OrnamentId { get; set; }

	public IReadOnlyList<Guid> OrnamentIds { get; private set; } = Array.Empty<Guid>();

	public OrnamentDescriptor? Ornament
	{
		get
		{
			if (!(OrnamentId != Guid.Empty) || !DatabaseObject<OrnamentDescriptor>.TryGet(OrnamentId, out var databaseObject))
			{
				return null;
			}
			return databaseObject;
		}
	}

	public PassifDeClasse Passif
	{
		get
		{
			if (_classeDuPassif == Class)
			{
				return _passifDeLaClasse;
			}
			if (!DatabaseObject<ClassDescriptor>.TryGet(Class, out var databaseObject))
			{
				return PassifDeClasse.Aucun;
			}
			_classeDuPassif = Class;
			_passifDeLaClasse = PassifsDeClasse.De(databaseObject.Name);
			return _passifDeLaClasse;
		}
	}

	private float AllongeDuPassif => PassifsDeClasse.AllongePx(Passif);

	public int[] ProfessionLevels { get; private set; } = Enumerable.Repeat(1, 9).ToArray();

	public long[] ProfessionExperience { get; private set; } = new long[9];

	public long[] ProfessionNextLevel { get; private set; } = new long[9];

	public int ProfessionMaximumLevel { get; private set; } = 50;

	public double ProfessionSpeedPerLevel { get; private set; } = 1.0;

	public double ProfessionRarityPerLevel { get; private set; } = 0.5;

	public double ProfessionAbundancePerLevel { get; private set; } = 1.0;

	public double ProfessionPreciseBase { get; private set; } = 5.0;

	public double ProfessionPrecisePerLevel { get; private set; } = 0.1;

	public double ProfessionCraftExperiencePerSecond { get; private set; } = 10.0;

	public int ProfessionsVersion { get; private set; }

	public bool EnRecolteAuClic => _recolteEtape != EtapeDeRecolteAuClic.Aucune;

	public static int CapaciteDuSac => Math.Max(1, Options.Instance.Player.MaxInventory - Options.Instance.Equipment.Slots.Count);

	public TeachingMaster TeachingMain { get; private set; }

	public TeachingMaster TeachingSecondary { get; private set; }

	public IReadOnlyDictionary<string, int> TeachingRanks { get; private set; } = new Dictionary<string, int>();

	public IReadOnlyDictionary<string, bool> PassedTrials { get; private set; } = new Dictionary<string, bool>();

	public int TeachingsVersion { get; private set; }

	public Guid TitleId { get; set; }

	public IReadOnlyList<Guid> TitleIds { get; private set; } = Array.Empty<Guid>();

	public TitleDescriptor? Title
	{
		get
		{
			if (!(TitleId != Guid.Empty) || !DatabaseObject<TitleDescriptor>.TryGet(TitleId, out var databaseObject))
			{
				return null;
			}
			return databaseObject;
		}
	}

	public event InventoryUpdatedEventHandler? InventoryUpdated;

	internal static bool GarderSaCible(bool cibleValide, int ecartEnCases)
	{
		if (cibleValide)
		{
			return ecartEnCases <= 6;
		}
		return false;
	}

	internal void EchangeAvec(Entity? autre)
	{
		int adversaireDuMomentMs = Options.Instance.Combat.AdversaireDuMomentMs;
		if (adversaireDuMomentMs <= 0 || this != Globals.Me || !(autre is Player player) || player == this || !player.CanBeAttacked || player.IsDying || IsAllyOf(player))
		{
			return;
		}
		long adversaireDuMomentJusqua = Timing.Global.Milliseconds + adversaireDuMomentMs;
		if (TargetId == player.Id)
		{
			if (_adversaireDuMoment == player.Id)
			{
				_adversaireDuMomentJusqua = adversaireDuMomentJusqua;
			}
			return;
		}
		if (Target is Entity entity)
		{
			(float X, float Y) tuple = TilesTo(entity);
			float item = tuple.X;
			float item2 = tuple.Y;
			bool cibleValide = !entity.IsDisposed && !entity.IsDying && entity.CanBeAttacked;
			if (GarderSaCible(cibleValide, (int)MathF.Ceiling(MathF.Max(MathF.Abs(item), MathF.Abs(item2)))))
			{
				return;
			}
		}
		if (TryTarget(player) && TargetId == player.Id)
		{
			_adversaireDuMoment = player.Id;
			_adversaireDuMomentJusqua = adversaireDuMomentJusqua;
		}
	}

	private void MajAdversaireDuMoment()
	{
		if (!(_adversaireDuMoment == Guid.Empty) && Timing.Global.Milliseconds >= _adversaireDuMomentJusqua)
		{
			if (TargetId == _adversaireDuMoment)
			{
				ClearTarget();
			}
			_adversaireDuMoment = Guid.Empty;
		}
	}

	private bool TryBeginBeam(int index, SpellDescriptor spell)
	{
		if (!Options.Instance.Beam.IsBeamSpell(spell.Id))
		{
			return false;
		}
		if (this != Globals.Me || IsBeaming || IsBusy || base.IsCasting || base.Dashing != null || base.FlightMode != FlightMode.None)
		{
			return true;
		}
		if (_beamWaitRelease && _beamSpellId == spell.Id)
		{
			return true;
		}
		long num = base.MaxVital[1];
		long num2 = (long)Math.Ceiling((double)((float)num * Math.Max(0f, Options.Instance.Beam.KiCostPercent[0])) / 100.0);
		if (base.Vital[1] < num2)
		{
			return true;
		}
		float degrees = (Heritage.Actif ? AngleLieHeritage() : ((float?)null)) ?? BeamAimFromStick() ?? BeamAimFromMouse() ?? KiBeam.AngleOf(base.DirectionFacing);
		long milliseconds = Timing.Global.Milliseconds;
		_beamSlot = index;
		_beamSpellId = spell.Id;
		_beamAim = (_beamAimGoal = (_beamAimSent = KiBeam.Normalize(degrees)));
		_beamAimSentAt = milliseconds;
		_beamAimUpdatedAt = milliseconds;
		_beamMouseLeads = true;
		_beamMouseSeen = Globals.InputManager.GetMousePosition();
		_beamArrowSeen = GetInputDirection();
		FaceBeam(_beamAim);
		KiBeam.StartCharge(this, _beamAim);
		PoserSaPositionAvantDeSeFiger();
		PacketSender.SendBeamRequest(index, charging: true, cancel: false, _beamAim, Heritage.Actif ? Heritage.Lien.CibleDuRayon : default(Guid));
		return true;
	}

	private void UpdateBeamInput()
	{
		if (this != Globals.Me)
		{
			return;
		}
		if (_beamWaitRelease && !IsBeamHotkeyHeld())
		{
			_beamWaitRelease = false;
		}
		if (_beamSlot < 0)
		{
			return;
		}
		switch (KiBeam.PhaseOf(this))
		{
		case BeamPhase.None:
			_beamSlot = -1;
			_beamWaitRelease = true;
			break;
		case BeamPhase.Firing:
			CourberLeRayonALaSouris();
			break;
		case BeamPhase.Charging:
			if (Controls.IsGameControlPressed(Control.Block))
			{
				PacketSender.SendBeamRequest(_beamSlot, charging: false, cancel: true, _beamAim);
				KiBeam.Cancel(this);
				_beamSlot = -1;
				_beamWaitRelease = true;
			}
			else
			{
				UpdateBeamAim();
				if (!IsBeamHotkeyHeld() || KiBeam.ChargedFor(this) >= Options.Instance.Beam.DepartForce)
				{
					FireBeamNow();
				}
			}
			break;
		}
	}

	public bool TryFireBeamOnClick()
	{
		if (TryRelacherMaitreAuClic())
		{
			return true;
		}
		if (Heritage.Actif && Heritage.Lien.RayonLie)
		{
			return true;
		}
		if (this != Globals.Me || _beamSlot < 0 || KiBeam.PhaseOf(this) != BeamPhase.Charging)
		{
			return false;
		}
		float? num = BeamAimFromMouse();
		if (num.HasValue)
		{
			float valueOrDefault = num.GetValueOrDefault();
			_beamAim = (_beamAimGoal = valueOrDefault);
			FaceBeam(_beamAim);
			KiBeam.Aim(this, _beamAim);
		}
		FireBeamNow();
		return true;
	}

	private void FireBeamNow()
	{
		PacketSender.SendBeamRequest(_beamSlot, charging: false, cancel: false, _beamAim);
		_beamAimSent = _beamAim;
		KiBeam.PredictFire(this);
		long milliseconds = Timing.Global.Milliseconds;
		_beamCourbeSent = 0f;
		_beamCourbeSentAt = milliseconds;
		_beamCourbeUpdatedAt = milliseconds;
		_beamCourbeSouris = _beamMouseLeads;
		_beamMouseSeen = Globals.InputManager.GetMousePosition();
	}

	private void CourberLeRayonALaSouris()
	{
		BeamOptions beam = Options.Instance.Beam;
		long milliseconds = Timing.Global.Milliseconds;
		long num = Math.Clamp(milliseconds - _beamCourbeUpdatedAt, 0L, 100L);
		_beamCourbeUpdatedAt = milliseconds;
		if (beam.CourbeMaxDegres <= 0f || (Heritage.Actif && Heritage.Lien.RayonLie) || Clash.BouleDeChoc(base.Id).HasValue)
		{
			return;
		}
		(float, float, float)? tuple = KiBeam.OrigineDuTir(this);
		if (!tuple.HasValue)
		{
			return;
		}
		(float, float, float) valueOrDefault = tuple.GetValueOrDefault();
		Vector2 mousePosition = Globals.InputManager.GetMousePosition();
		if (Vector2.DistanceSquared(mousePosition, _beamMouseSeen) > 4f)
		{
			_beamMouseSeen = mousePosition;
			_beamCourbeSouris = true;
		}
		float? num2 = null;
		float num3 = valueOrDefault.Item3;
		float? num4 = BeamAimFromStick();
		if (num4.HasValue)
		{
			float valueOrDefault2 = num4.GetValueOrDefault();
			num2 = valueOrDefault2;
		}
		else if (_beamCourbeSouris && !Intersect.Client.Interface.Interface.DoesMouseHitInterface())
		{
			Vector2 vector = Graphics.ConvertToWorldPoint(mousePosition);
			float num5 = vector.X - valueOrDefault.Item1;
			float num6 = vector.Y - valueOrDefault.Item2;
			num3 = MathF.Sqrt(num5 * num5 + num6 * num6);
			if (num3 >= 20f)
			{
				num2 = MathF.Atan2(num6, num5) * 180f / (float)Math.PI;
			}
		}
		float courbeMaxDegres = beam.CourbeMaxDegres;
		float num7 = KiBeam.CourbeOf(this);
		float num8 = num7;
		if (num2.HasValue)
		{
			float valueOrDefault3 = num2.GetValueOrDefault();
			float x = KiBeam.DeltaAngle(KiBeam.AngleOf(this), valueOrDefault3) * (float)Math.PI / 180f;
			num8 = ((MathF.Cos(x) < 0f) ? ((float)MathF.Sign(x) * courbeMaxDegres) : Math.Clamp(2f * MathF.Sin(x) / num3 * valueOrDefault.Item3 * 180f / (float)Math.PI, 0f - courbeMaxDegres, courbeMaxDegres));
			if (MathF.Abs(num8) < 1.5f)
			{
				num8 = 0f;
			}
		}
		float num9 = beam.CourbeDegresParSeconde * (float)num / 1000f;
		float num10 = num7 + Math.Clamp(num8 - num7, 0f - num9, num9);
		KiBeam.Courber(this, num10);
		if (milliseconds - _beamCourbeSentAt >= 100 && num10 != _beamCourbeSent && (MathF.Abs(num10 - _beamCourbeSent) >= 0.5f || num10 == num8))
		{
			_beamCourbeSentAt = milliseconds;
			_beamCourbeSent = num10;
			int beamSlot = _beamSlot;
			float beamAim = _beamAim;
			float courbe = num10;
			PacketSender.SendBeamRequest(beamSlot, charging: false, cancel: false, beamAim, default(Guid), courbe);
		}
	}

	private void UpdateBeamAim()
	{
		long milliseconds = Timing.Global.Milliseconds;
		long num = Math.Clamp(milliseconds - _beamAimUpdatedAt, 0L, 100L);
		_beamAimUpdatedAt = milliseconds;
		Direction inputDirection = GetInputDirection();
		if (inputDirection != Direction.None && inputDirection != _beamArrowSeen)
		{
			_beamAimGoal = KiBeam.AngleOf(inputDirection);
			_beamMouseLeads = false;
		}
		_beamArrowSeen = inputDirection;
		Vector2 mousePosition = Globals.InputManager.GetMousePosition();
		if (Vector2.DistanceSquared(mousePosition, _beamMouseSeen) > 4f)
		{
			_beamMouseSeen = mousePosition;
			_beamMouseLeads = true;
		}
		float? num2;
		if (_beamMouseLeads)
		{
			num2 = BeamAimFromMouse();
			if (num2.HasValue)
			{
				float valueOrDefault = num2.GetValueOrDefault();
				_beamAimGoal = valueOrDefault;
			}
		}
		num2 = BeamAimFromStick();
		if (num2.HasValue)
		{
			float valueOrDefault2 = num2.GetValueOrDefault();
			_beamAimGoal = valueOrDefault2;
		}
		if (Heritage.Actif)
		{
			num2 = AngleLieHeritage();
			if (num2.HasValue)
			{
				float valueOrDefault3 = num2.GetValueOrDefault();
				_beamAimGoal = valueOrDefault3;
			}
		}
		float value = KiBeam.DeltaAngle(_beamAim, _beamAimGoal);
		float num3 = 300f * (float)num / 1000f;
		_beamAim = KiBeam.Normalize(_beamAim + Math.Clamp(value, 0f - num3, num3));
		KiBeam.Aim(this, _beamAim);
		FaceBeam(_beamAim);
		if (milliseconds - _beamAimSentAt >= 100 && MathF.Abs(KiBeam.DeltaAngle(_beamAimSent, _beamAim)) >= 2f)
		{
			_beamAimSentAt = milliseconds;
			_beamAimSent = _beamAim;
			PacketSender.SendBeamRequest(_beamSlot, charging: true, cancel: false, _beamAim);
		}
	}

	private float? BeamAimFromMouse()
	{
		if (Intersect.Client.Interface.Interface.DoesMouseHitInterface())
		{
			return null;
		}
		Vector2 vector = Graphics.ConvertToWorldPoint(Globals.InputManager.GetMousePosition());
		FloatRect worldPos = base.WorldPos;
		float num = vector.X - (worldPos.X + worldPos.Width / 2f);
		float num2 = vector.Y - (worldPos.Y + worldPos.Height * 0.65f);
		if (num * num + num2 * num2 < 100f)
		{
			return null;
		}
		return MathF.Atan2(num2, num) * 180f / (float)Math.PI;
	}

	private static float? BeamAimFromStick()
	{
		float stickDroitX = Manette.StickDroitX;
		float stickDroitY = Manette.StickDroitY;
		if (stickDroitX * stickDroitX + stickDroitY * stickDroitY < 0.122499995f)
		{
			return null;
		}
		return MathF.Atan2(0f - stickDroitY, stickDroitX) * 180f / (float)Math.PI;
	}

	private void FaceBeam(float angle)
	{
		Direction direction = KiBeam.Cardinal(angle);
		if (base.DirectionFacing != direction && !base.IsMoving)
		{
			base.DirectionFacing = direction;
			PickLastDirection(direction);
		}
	}

	private bool IsBeamHotkeyHeld()
	{
		for (int i = 0; i < Hotbar.Length; i++)
		{
			Guid? guid = Hotbar[i]?.ItemOrSpellId;
			Guid beamSpellId = _beamSpellId;
			if (guid.HasValue && guid.GetValueOrDefault() == beamSpellId && Controls.IsControlPressed((Control)(4096 + i + 1)))
			{
				return true;
			}
		}
		return false;
	}

	public Charge LireLaCharge()
	{
		long milliseconds = Timing.Global.Milliseconds;
		if (_chargeLueA != long.MinValue && milliseconds >= _chargeLueA && milliseconds - _chargeLueA < 100)
		{
			return _charge;
		}
		_chargeLueA = milliseconds;
		long num = 0L;
		int num2 = 0;
		Guid capsule = Guid.Empty;
		for (int i = 0; i < base.Inventory.Length; i++)
		{
			IItem item = base.Inventory[i];
			if (item != null && !(item.ItemId == Guid.Empty) && Array.IndexOf(base.MyEquipment, i) < 0 && DatabaseObject<ItemDescriptor>.TryGet(item.ItemId, out var databaseObject))
			{
				num += (long)ReglesDeCharge.PoidsUnitaire(databaseObject) * (long)Math.Max(0, item.Quantity);
				int num3 = ReglesDeCharge.BonusDeCapsule(databaseObject);
				if (num3 > num2)
				{
					num2 = num3;
					capsule = databaseObject.Id;
				}
			}
		}
		int num4 = 0;
		for (int j = 0; j < ProfessionLevels.Length; j++)
		{
			num4 += Math.Max(0, ProfessionLevels[j] - 1);
		}
		_charge = new Charge((int)Math.Min(2147483647L, num), ReglesDeCharge.Capacite(base.Level, ProfessionLevels, num2), 10 * Math.Max(0, base.Level), ReglesDeCharge.BonusDesMetiers(ProfessionLevels), num4, num2, capsule);
		return _charge;
	}

	private void DireSurcharge()
	{
		long milliseconds = Timing.Global.Milliseconds;
		if (_surchargeDiteA == long.MinValue || milliseconds < _surchargeDiteA || milliseconds - _surchargeDiteA >= 3000)
		{
			_surchargeDiteA = milliseconds;
			Charge charge = LireLaCharge();
			ChatboxMsg.AddMessage(new ChatboxMsg(Strings.Inventaire.OverloadedMove.ToString(Math.Max(1, charge.Porte - charge.Capacite)), CustomColors.Alerts.Error, ChatMessageType.Inventory));
		}
	}

	private byte PasPredit(Guid cibleId)
	{
		if (CoupsDuCombo < 2 || cibleId == Guid.Empty)
		{
			return 1;
		}
		return (byte)((!(cibleId == _pasCibleId) || Timing.Global.Milliseconds > _pasExpireA) ? 1u : ((uint)(_pasLocal % CoupsDuCombo + 1)));
	}

	private byte PasDuProchainCoup()
	{
		return (byte)((CoupsDuCombo < 2 || Timing.Global.Milliseconds > _pasExpireA) ? 1u : ((uint)(_pasLocal % CoupsDuCombo + 1)));
	}

	private int DureeLocaleDuCoup(byte pas)
	{
		int num = CalculateAttackTime();
		if (CoupsDuCombo < 2 || pas == 0)
		{
			return num;
		}
		CombatOptions combat = Options.Instance.Combat;
		int val = ((pas >= CoupsDuCombo) ? combat.CoupFinalDureePourcent : combat.CoupDureePourcent);
		return Math.Max(1, (int)Math.Round((double)(num * Math.Max(10, val)) / 100.0));
	}

	private void NoterLeCoupLocal(Guid cibleId, byte pas, int duree)
	{
		base.PasDuCoup = pas;
		base.DureeDuCoupLocal = duree;
		_pasCibleId = cibleId;
		_pasLocal = (byte)((pas < CoupsDuCombo) ? pas : 0);
		_pasExpireA = ((pas >= CoupsDuCombo) ? 0 : (Timing.Global.Milliseconds + duree + Math.Max(0, Options.Instance.Combat.ComboWindow)));
	}

	private void NoterUnGesteHorsCombo(int duree)
	{
		base.PasDuCoup = 1;
		base.DureeDuCoupLocal = duree;
	}

	internal void RecalerLePas(byte pas, int duree)
	{
		if (pas != 0)
		{
			base.DureeDuCoupLocal = duree;
			if (pas >= CoupsDuCombo)
			{
				_pasLocal = 0;
				_pasExpireA = 0L;
			}
			else if (pas != _pasLocal)
			{
				_pasLocal = pas;
				_pasExpireA = Math.Max(_pasExpireA, Timing.Global.Milliseconds + duree + Math.Max(0, Options.Instance.Combat.ComboWindow));
			}
		}
	}

	internal bool PoseDeLAppui(out byte pas)
	{
		pas = 0;
		if (this != Globals.Me || !_appuiEnAttente || Echanges.ChargeLocale(this) || (!_poseDeLAppuiMontree && !EnnemiAPorteeDeBras()))
		{
			return false;
		}
		if (HeritageRegles.Actif && Heritage.Choix.DemandeProbable(this) != CoupHeritage.Aucun)
		{
			return false;
		}
		pas = PasDuProchainCoup();
		_poseDeLAppuiMontree = true;
		return true;
	}

	private bool EnnemiAPorteeDeBras()
	{
		if (!base.EstPiloteLibrement || !PositionMonde(out var mondeX, out var mondeY))
		{
			return false;
		}
		foreach (Entity value in Globals.Entities.Values)
		{
			if (value == null || value == this || !value.CanBeAttacked || value.IsDying || value.Z != base.Z || !value.PositionMonde(out var mondeX2, out var mondeY2))
			{
				continue;
			}
			bool flag = value.Id == TargetId;
			bool flag2 = flag;
			if (!flag2)
			{
				bool flag3 = value.Type == EntityType.GlobalEntity && !(value is Critter);
				bool flag4 = flag3;
				if (flag4)
				{
					NpcAggression aggression = value.Aggression;
					bool flag5 = (uint)(aggression - 3) <= 1u;
					flag4 = !flag5;
				}
				flag2 = flag4;
			}
			if (flag2 && CorpsACorps.APortee(mondeX2 - mondeX, mondeY2 - mondeY, AllongeDuPassif, base.RayonDuCorps, value.RayonDuCorps))
			{
				return true;
			}
		}
		return false;
	}

	private bool ProchainCoupEnPleinCombo()
	{
		byte b = PasDuProchainCoup();
		if (b > 1)
		{
			return b < CoupsDuCombo;
		}
		return false;
	}

	private Entity? CibleAuCorpsACorpsLibre()
	{
		if (!PositionMonde(out var mondeX, out var mondeY))
		{
			return null;
		}
		Direction directionFacing = base.DirectionFacing;
		bool flag = !Controls.IsControlPressed(Control.TurnAround);
		Entity entity = null;
		float num = float.MaxValue;
		Direction direction = directionFacing;
		bool flag2 = Heritage.Actif && Heritage.Cible.IgnoreLesRessources(this);
		Guid guid = (Heritage.Actif ? Heritage.Cible.ReferenceDe(this) : Guid.Empty);
		Entity entity2 = null;
		Direction direction2 = directionFacing;
		foreach (Entity value in Globals.Entities.Values)
		{
			if (value == null || value == this || !value.CanBeAttacked || value.IsDying || value.Z != base.Z || !value.PositionMonde(out var mondeX2, out var mondeY2) || (flag2 && value is Resource))
			{
				continue;
			}
			float num2 = mondeX2 - mondeX;
			float num3 = mondeY2 - mondeY;
			if (!CorpsACorps.APortee(num2, num3, AllongeDuPassif, base.RayonDuCorps, value.RayonDuCorps) && (!Heritage.Actif || !DansLAllongeHeritage(value, num2, num3)))
			{
				continue;
			}
			bool flag3 = value.Id == TargetId;
			bool flag4 = value.Type == EntityType.GlobalEntity && !(value is Critter);
			bool flag5 = flag4;
			if (flag5)
			{
				NpcAggression aggression = value.Aggression;
				bool flag6 = (uint)(aggression - 3) <= 1u;
				flag5 = !flag6;
			}
			bool flag7 = flag5;
			bool flag8 = CorpsACorps.Devant(num2, num3, directionFacing, base.RayonDuCorps, value.RayonDuCorps);
			if (flag8 || (flag && (flag3 || flag7)))
			{
				Direction direction3 = (flag8 ? directionFacing : CorpsACorps.DirectionVers(num2, num3));
				float num4 = MathF.Sqrt(num2 * num2 + num3 * num3);
				float num5 = (flag3 ? 0f : (flag7 ? 10f : 20f)) + (flag8 ? 0f : (1f + (float)TurnSteps(directionFacing, direction3))) + num4 / 100f;
				if (num5 < num)
				{
					num = num5;
					entity = value;
					direction = direction3;
				}
				if (guid != Guid.Empty && value.Id == guid && Heritage.Cible.ReferenceAPortee(num2, num3))
				{
					entity2 = value;
					direction2 = direction3;
				}
			}
		}
		if (entity == null)
		{
			return null;
		}
		if (entity2 != null && Heritage.Cible.Reference(entity, direction == directionFacing, entity2) != entity)
		{
			entity = entity2;
			direction = direction2;
		}
		if (direction != directionFacing && direction != Direction.None)
		{
			if (EvenementDActionDevantMoi())
			{
				return null;
			}
			base.DirectionFacing = direction;
			PacketSender.SendDirection(direction);
		}
		if (Heritage.Actif && flag && direction == directionFacing)
		{
			TournerVersUnCorpsColleHeritage(entity, mondeX, mondeY);
		}
		return entity;
	}

	private bool EvenementDActionDevantMoi()
	{
		foreach (var (id, num, num2) in CasesDevantMoi())
		{
			if (!Intersect.Client.Maps.MapInstance.TryGet(id, out MapInstance instance))
			{
				continue;
			}
			foreach (Entity value in instance.LocalEntities.Values)
			{
				if (value is Event { Trigger: EventTrigger.ActionButton } obj && obj.X == num && obj.Y == num2)
				{
					return true;
				}
			}
		}
		return false;
	}

	public void UpdateInventory(int slotIndex, Guid descriptorId, int quantity, Guid? bagId, ItemProperties itemProperties)
	{
		base.Inventory[slotIndex].Load(descriptorId, quantity, bagId, itemProperties);
		this.InventoryUpdated?.Invoke(this, slotIndex);
	}

	public Player(Guid id, PlayerEntityPacket packet)
		: base(id, packet, EntityType.Player)
	{
		for (int i = 0; i < Options.Instance.Player.HotbarSlotCount; i++)
		{
			Hotbar[i] = new HotbarInstance();
		}
		mRenderPriority = 2;
	}

	public bool IsFriend(IPlayer player)
	{
		return Friends.Any((IFriendInstance friend) => player != null && string.Equals(player.Name, friend.Name, StringComparison.CurrentCultureIgnoreCase));
	}

	public bool IsGuildMate(IPlayer player)
	{
		return GuildMembers.Any((GuildMember guildMate) => player != null && string.Equals(player.Name, guildMate.Name, StringComparison.CurrentCultureIgnoreCase));
	}

	public bool IsInParty()
	{
		return Party.Count > 0;
	}

	public bool IsInMyParty(IPlayer player)
	{
		if (player != null)
		{
			return IsInMyParty(player.Id);
		}
		return false;
	}

	public bool IsInMyParty(Guid id)
	{
		return Party.Any((IPartyMember member) => member.Id == id);
	}

	public bool IsInMyGuild(IPlayer player)
	{
		if (IsInGuild && player != null)
		{
			return player.GuildName == Guild;
		}
		return false;
	}

	public override bool Update()
	{
		if (Globals.Me == this)
		{
			HandleInput();
			MajRecolteAuClic();
			MajRamassageAuto();
			MajDash();
			if (EnDeplacementLibre && !Globals.MoveRouteActive)
			{
				AvancerLibrement(IsBusy);
			}
			else if (base.EstPiloteLibrement)
			{
				if (Globals.MoveRouteActive)
				{
					OublierLeDeplacementLibre();
				}
				else
				{
					QuitterLeDeplacementLibre();
				}
			}
			if (IsBusy)
			{
				CaleSurSaCase();
			}
		}
		if (!IsBusy)
		{
			if (this == Globals.Me && !base.IsMoving && !base.EstPiloteLibrement && !EnDeplacementLibre)
			{
				ProcessDirectionalInput(base.TempsDepuisLaDerniereImage);
			}
			if (this == Globals.Me)
			{
				CaleSurSaCase();
			}
			long milliseconds = Timing.Global.Milliseconds;
			bool flag = this == Globals.Me && Controls.IsGameControlPressed(Control.AttackInteract) && !RecolteAuClicAbsorbeLeCoup();
			if (Heritage.Actif && AppuiDeCoupAvale())
			{
				flag = true;
			}
			if (Heritage.Actif && this == Globals.Me && Heritage.Appuis.RelacheAvale())
			{
				flag = false;
			}
			bool flag2 = flag && !_attackHeldLastFrame;
			if (!flag)
			{
				_appuiRendu = false;
			}
			_attackHeldLastFrame = flag;
			if (this == Globals.Me && Clash.LireLaTouche(this, flag2))
			{
				flag = false;
				flag2 = false;
			}
			bool flag3 = this == Globals.Me && ToucheDeCoupAuxEchanges(flag, flag2, milliseconds);
			if (Heritage.Actif && flag3 && AppuiGardePasseDevantHeritage(milliseconds))
			{
				flag3 = false;
			}
			bool flag4 = this == Globals.Me && !base.IsAttacking && _attackBufferedUntil > milliseconds && !flag3;
			long num = ((Heritage.Actif && flag4) ? _attackBufferedUntil : 0);
			if (flag4)
			{
				_attackBufferedUntil = 0L;
			}
			bool flag5 = this == Globals.Me && !base.IsAttacking && _attackApproachUntil > milliseconds && !flag3;
			if (this == Globals.Me && _bufferedSpellSlot >= 0 && GetSpellRemainingCooldown(_bufferedSpellSlot) <= 0)
			{
				int bufferedSpellSlot = _bufferedSpellSlot;
				_bufferedSpellSlot = -1;
				if (milliseconds <= _bufferedSpellUntil)
				{
					TryUseSpell(bufferedSpellSlot);
				}
			}
			if (!flag3 && (CoupDemande || flag4 || flag5) && !_appuiRendu && !base.IsCharging && !IsBeaming && !base.IsBlocking && !base.IsGuardStunned && !base.IsTransforming)
			{
				if (base.IsCasting)
				{
					if (IsCastingCheckTimer < Timing.Global.Milliseconds && Options.Instance.Combat.EnableCombatChatMessages)
					{
						ChatboxMsg.AddMessage(new ChatboxMsg(Strings.Combat.AttackWhileCastingDeny, CustomColors.Alerts.Declined, ChatMessageType.Combat));
						IsCastingCheckTimer = Timing.Global.Milliseconds + 350;
					}
				}
				else
				{
					Player me = Globals.Me;
					if (me != null)
					{
						if (me.TryAttack())
						{
							me.NoterLeCoupParti(me._dernierCoupRecolte);
						}
						else if (me._recolteRefusee || me._appuiDeRecolte)
						{
							me._recolteRefusee = false;
							me.FinirLAppuiDeRecolte();
						}
						else if (me.EnnemiEnApproche())
						{
							if (flag2)
							{
								_attackApproachUntil = milliseconds + 350;
							}
						}
						else if (!me.IsAttacking && (!base.IsMoving || Options.Instance.Player.AllowCombatMovement))
						{
							me.AttackTimer = Timing.Global.Milliseconds + me.CalculateAttackTime();
							me.NoterLeCoupParti(recolte: false);
						}
					}
				}
			}
			if (Heritage.Actif && num > 0)
			{
				_attackBufferedUntil = AppuiGardeApresLImage(num);
			}
			if (!Options.Instance.Combat.EnableGuard && Controls.IsGameControlPressed(Control.Block))
			{
				TryBlock();
			}
		}
		if (TargetBox == null && this == Globals.Me && Intersect.Client.Interface.Interface.HasInGameUI)
		{
			TargetBox = new EntityBox(Intersect.Client.Interface.Interface.GameUi.GameCanvas, EntityType.Player, null);
			TargetBox.Hide();
		}
		else if (TargetId != default(Guid))
		{
			if (!Globals.Entities.TryGetValue(TargetId, out Entity value))
			{
				value = TargetBox?.MyEntity?.MapInstance?.Entities.FirstOrDefault((IEntity entity) => entity.Id == TargetId) as Entity;
			}
			if (value == null || value.IsHidden || value.IsStealthed)
			{
				ClearTarget();
			}
		}
		if (this == Globals.Me)
		{
			MajAdversaireDuMoment();
		}
		TargetBox?.Update();
		StatusWindow?.Update();
		if (!Options.Instance.Combat.EnableGuard && base.IsBlocking && !base.IsAttacking && !base.IsMoving)
		{
			base.IsBlocking = false;
		}
		UpdateChargeState();
		UpdateBeamInput();
		UpdateMaitreInput();
		UpdateGuardState();
		Vanish.UpdateInput(this);
		Echanges.SuivreLaRuee(this);
		Echanges.SuivreLaCharge(this);
		if (Heritage.Actif)
		{
			Heritage.Suivre(this);
		}
		UpdateSprintState();
		UpdateAggressorLabel();
		bool result = base.Update();
		float? finishedStepOvershoot = base.FinishedStepOvershoot;
		base.FinishedStepOvershoot = null;
		if (finishedStepOvershoot.HasValue)
		{
			float valueOrDefault = finishedStepOvershoot.GetValueOrDefault();
			if (this == Globals.Me && !base.IsMoving && !IsBusy && !base.EstPiloteLibrement)
			{
				if (EnDeplacementLibre)
				{
					ReporterSurLeDeplacementLibre(valueOrDefault);
				}
				else
				{
					ProcessDirectionalInput(valueOrDefault * GetMovementTime() / (float)Math.Max(1, TileHeight));
					if (base.IsMoving)
					{
						base.OffsetX = Entity.StepTowardsZero(base.OffsetX, valueOrDefault);
						base.OffsetY = Entity.StepTowardsZero(base.OffsetY, valueOrDefault);
					}
				}
			}
		}
		SprintDust.Observe(this, SprintSkid.Observe(this));
		return result;
	}

	public override void Load(EntityPacket? packet)
	{
		base.Load(packet);
		if (!(packet is PlayerEntityPacket playerEntityPacket))
		{
			return;
		}
		base.Gender = playerEntityPacket.Gender;
		Class = playerEntityPacket.ClassId;
		AccessLevel = playerEntityPacket.AccessLevel;
		CombatTimer = playerEntityPacket.CombatTimeRemaining + Timing.Global.Milliseconds;
		Guild = playerEntityPacket.Guild;
		Rank = playerEntityPacket.GuildRank;
		GuildTag = playerEntityPacket.GuildTag ?? string.Empty;
		GuildEmblem = playerEntityPacket.GuildEmblem;
		GuildColor = playerEntityPacket.GuildColor;
		TitleId = playerEntityPacket.TitleId;
		OrnamentId = playerEntityPacket.OrnamentId;
		KarmaTier = (KarmaTier)playerEntityPacket.KarmaTier;
		Flight.SetState(this, (FlightMode)playerEntityPacket.FlightMode);
		AggressorUntil = ((playerEntityPacket.AggressorRemainingMs > 0) ? (Timing.Global.Milliseconds + playerEntityPacket.AggressorRemainingMs) : 0);
		_serverHeaderLabel = base.HeaderLabel;
		_showsAggressorLabel = false;
		if (playerEntityPacket.Equipment != null)
		{
			if (this == Globals.Me && playerEntityPacket.Equipment.InventorySlots != null)
			{
				base.MyEquipment = playerEntityPacket.Equipment.InventorySlots;
			}
			else if (playerEntityPacket.Equipment.ItemIds != null)
			{
				base.Equipment = playerEntityPacket.Equipment.ItemIds;
			}
		}
		if (this == Globals.Me && TargetBox == null && Intersect.Client.Interface.Interface.HasInGameUI)
		{
			TargetBox = new EntityBox(Intersect.Client.Interface.Interface.GameUi.GameCanvas, EntityType.Player, null);
			TargetBox.Hide();
		}
	}

	public void SwapItems(int fromSlotIndex, int toSlotIndex)
	{
		PacketSender.SendSwapInvItems(fromSlotIndex, toSlotIndex);
		IItem item = base.Inventory[fromSlotIndex];
		IItem item2 = base.Inventory[toSlotIndex];
		if (item.ItemId == item2.ItemId && DatabaseObject<ItemDescriptor>.TryGet(item2.ItemId, out var databaseObject) && databaseObject.IsStackable && item.Quantity < databaseObject.MaxInventoryStack && item2.Quantity < databaseObject.MaxInventoryStack)
		{
			int num = item.Quantity + item2.Quantity;
			int num2 = Math.Min(databaseObject.MaxInventoryStack, num);
			int num3 = num - num2;
			item2.Quantity = num2;
			item.Quantity = num3;
			if (num3 < 1)
			{
				base.Inventory[fromSlotIndex].ItemId = default(Guid);
			}
		}
		else
		{
			base.Inventory[fromSlotIndex] = item2;
			base.Inventory[toSlotIndex] = item;
		}
	}

	public void TryDropItem(int inventorySlotIndex)
	{
		IItem inventorySlot = base.Inventory[inventorySlotIndex];
		if (!DatabaseObject<ItemDescriptor>.TryGet(inventorySlot.ItemId, out var itemDescriptor))
		{
			return;
		}
		int quantity = inventorySlot.Quantity;
		bool canDropMultiple = quantity > 1;
		if (canDropMultiple)
		{
			new QuantityWindow(Strings.Inventory.DropItemTitle, itemDescriptor, Strings.Quantity.Drop, quantity, GetQuantityOfItemInInventory(itemDescriptor.Id), Drop);
			return;
		}
		ConfirmCardWindow.Open(new ConfirmCardRequest
		{
			Title = Strings.ConfirmCard.DropTitle,
			Heading = itemDescriptor.Name,
			Prompt = Strings.ConfirmCard.DropPrompt,
			Detail = Strings.ConfirmCard.DropDetail,
			ConfirmLabel = Strings.ConfirmCard.Drop,
			Danger = true,
			Emblem = Globals.ContentManager.GetTexture(TextureType.Item, itemDescriptor.Icon)
		}, delegate
		{
			Drop(1);
		});
		void Drop(int promptQuantity)
		{
			if (promptQuantity >= 1)
			{
				if (canDropMultiple && promptQuantity > quantity)
				{
					List<IItem> list = base.Inventory.Where((IItem s) => s.ItemId == itemDescriptor.Id).ToList();
					PacketSender.SendDropItem(inventorySlotIndex, quantity);
					promptQuantity -= quantity;
					list.Remove(inventorySlot);
					{
						foreach (IItem item in list)
						{
							int num = Math.Min(promptQuantity, item.Quantity);
							if (num <= 0)
							{
								break;
							}
							PacketSender.SendDropItem(base.Inventory.IndexOf<IItem>(item), num);
							promptQuantity -= num;
						}
						return;
					}
				}
				PacketSender.SendDropItem(inventorySlotIndex, (!canDropMultiple) ? 1 : promptQuantity);
			}
		}
	}

	public int FindItem(Guid itemId, int itemVal = 1)
	{
		for (int i = 0; i < Options.Instance.Player.MaxInventory; i++)
		{
			if (base.Inventory[i].ItemId == itemId && base.Inventory[i].Quantity >= itemVal)
			{
				return i;
			}
		}
		return -1;
	}

	private static int GetQuantityOfItemIn(IEnumerable<IItem> items, Guid itemId)
	{
		long num = 0L;
		foreach (IItem item in items ?? Array.Empty<IItem>())
		{
			if (item != null && item.ItemId == itemId)
			{
				num += item.Quantity;
			}
		}
		return (int)Math.Min(num, 2147483647L);
	}

	public static int GetQuantityOfItemInBank(Guid itemId)
	{
		return GetQuantityOfItemIn(Globals.BankSlots, itemId);
	}

	public int GetQuantityOfItemInInventory(Guid itemId)
	{
		return GetQuantityOfItemIn(base.Inventory, itemId);
	}

	public void TryUseItem(int index)
	{
		if (!IsItemOnCooldown(index) && index >= 0 && index < Globals.Me?.Inventory.Length)
		{
			IItem obj = Globals.Me.Inventory[index];
			if (obj != null && obj.Quantity > 0)
			{
				PacketSender.SendUseItem(index, TargetId);
			}
		}
	}

	public long GetItemCooldown(Guid id)
	{
		if (ItemCooldowns.TryGetValue(id, out var value))
		{
			return value;
		}
		return 0L;
	}

	public int FindHotbarItem(IHotbarInstance hotbarInstance)
	{
		int result = -1;
		if (hotbarInstance.ItemOrSpellId != Guid.Empty)
		{
			for (int i = 0; i < base.Inventory.Length; i++)
			{
				IItem item = base.Inventory[i];
				if (item == null || !(item.ItemId == hotbarInstance.ItemOrSpellId))
				{
					continue;
				}
				result = i;
				ItemDescriptor itemDescriptor = DatabaseObject<ItemDescriptor>.Get(item.ItemId);
				if (itemDescriptor == null)
				{
					continue;
				}
				if (itemDescriptor.ItemType == ItemType.Bag)
				{
					Guid bagId = hotbarInstance.BagId;
					Guid? bagId2 = item.BagId;
					if (bagId == bagId2)
					{
						break;
					}
					continue;
				}
				if (itemDescriptor.ItemType != ItemType.Equipment)
				{
					break;
				}
				if (hotbarInstance.PreferredStatBuffs == null)
				{
					continue;
				}
				bool flag = true;
				for (int j = 0; j < hotbarInstance.PreferredStatBuffs.Length; j++)
				{
					if (item.ItemProperties.StatModifiers[j] != hotbarInstance.PreferredStatBuffs[j])
					{
						flag = false;
					}
				}
				if (flag)
				{
					break;
				}
			}
		}
		return result;
	}

	public bool IsEquipped(int slot)
	{
		for (int i = 0; i < Options.Instance.Equipment.Slots.Count; i++)
		{
			if (base.MyEquipment[i] == slot)
			{
				return true;
			}
		}
		return false;
	}

	public bool IsItemOnCooldown(int slot)
	{
		IItem item = base.Inventory[slot];
		if (item == null)
		{
			return false;
		}
		if (item.ItemId == Guid.Empty)
		{
			return false;
		}
		if (ItemCooldowns.TryGetValue(item.ItemId, out var value) && value > Timing.Global.Milliseconds)
		{
			return true;
		}
		if (DatabaseObject<ItemDescriptor>.TryGet(item.ItemId, out var databaseObject) && !databaseObject.IgnoreGlobalCooldown)
		{
			return Globals.Me?.GlobalCooldown > Timing.Global.Milliseconds;
		}
		return false;
	}

	public long GetItemRemainingCooldown(int slot)
	{
		IItem item = base.Inventory[slot];
		if (item == null)
		{
			return 0L;
		}
		if (item.ItemId == Guid.Empty)
		{
			return 0L;
		}
		if (ItemCooldowns.TryGetValue(item.ItemId, out var value) && value > Timing.Global.Milliseconds)
		{
			return value - Timing.Global.Milliseconds;
		}
		if (!DatabaseObject<ItemDescriptor>.TryGet(item.ItemId, out var databaseObject))
		{
			return 0L;
		}
		if (databaseObject.IgnoreGlobalCooldown)
		{
			return 0L;
		}
		Player me = Globals.Me;
		if (me == null)
		{
			return 0L;
		}
		long val = me.GlobalCooldown - Timing.Global.Milliseconds;
		return Math.Max(0L, val);
	}

	public bool IsSpellOnCooldown(int slot)
	{
		if (base.Spells[slot] != null)
		{
			Spell spell = base.Spells[slot];
			if (spell.Id != Guid.Empty)
			{
				if (SpellCooldowns.TryGetValue(spell.Id, out var value) && value > Timing.Global.Milliseconds)
				{
					return true;
				}
				if (DatabaseObject<SpellDescriptor>.TryGet(spell.Id, out var databaseObject) && !databaseObject.IgnoreGlobalCooldown && Globals.Me?.GlobalCooldown > Timing.Global.Milliseconds)
				{
					return true;
				}
			}
		}
		return false;
	}

	public long GetSpellRemainingCooldown(int slot)
	{
		if (slot < 0 || base.Spells.Length <= slot)
		{
			ApplicationContext.Context.Value?.Logger.LogWarning(new ArgumentOutOfRangeException("slot", slot, $"Slot was out of the range [0,{base.Spells.Length}"), "Tried to get remaining cooldown for spell in invalid slot {SlotIndex}", slot);
			return 0L;
		}
		Spell spell = base.Spells[slot];
		if (spell == null || spell.Id == Guid.Empty)
		{
			return 0L;
		}
		long milliseconds = Timing.Global.Milliseconds;
		if (SpellCooldowns.TryGetValue(spell.Id, out var value) && value > milliseconds)
		{
			return value - milliseconds;
		}
		if (DatabaseObject<SpellDescriptor>.TryGet(spell.Id, out var databaseObject) && !databaseObject.IgnoreGlobalCooldown)
		{
			Player? me = Globals.Me;
			if (me != null && me.GlobalCooldown > milliseconds)
			{
				return Globals.Me.GlobalCooldown - milliseconds;
			}
		}
		return 0L;
	}

	public void TrySellItem(int inventorySlotIndex)
	{
		IItem item = base.Inventory[inventorySlotIndex];
		if (!DatabaseObject<ItemDescriptor>.TryGet(item.ItemId, out var itemDescriptor))
		{
			return;
		}
		ShopDescriptor shop = Globals.GameShop;
		ShopDescriptor shopDescriptor = shop;
		if (shopDescriptor == null || !shopDescriptor.BuyingWhitelist || !shop.BuyingItems.Any((ShopItemDescriptor buyingItem) => buyingItem.ItemId == itemDescriptor.Id))
		{
			ShopDescriptor shopDescriptor2 = shop;
			if (shopDescriptor2 == null || shopDescriptor2.BuyingWhitelist || shop.BuyingItems.Any((ShopItemDescriptor buyingItem) => buyingItem.ItemId == itemDescriptor.Id))
			{
				goto IL_00b4;
			}
		}
		if (shop != null)
		{
			int quantity = item.Quantity;
			if (itemDescriptor.IsStackable || quantity > 1)
			{
				int quantityOfItemInInventory = GetQuantityOfItemInInventory(itemDescriptor.Id);
				if (quantityOfItemInInventory > 1)
				{
					new QuantityWindow(Strings.Shop.SellItem, itemDescriptor, Strings.Quantity.Sell, quantity, quantityOfItemInInventory, delegate(int amount)
					{
						PacketSender.SendSellItem(inventorySlotIndex, amount);
					}, SellPricing(shop, itemDescriptor), null, () => Globals.GameShop == shop);
					return;
				}
			}
			QuantityPricing quantityPricing = SellPricing(shop, itemDescriptor);
			ConfirmCardRequest confirmCardRequest = new ConfirmCardRequest()
			{
				Title = Strings.ConfirmCard.SellTitle,
				Heading = itemDescriptor.Name,
				Prompt = Strings.ConfirmCard.SellPrompt,
				Chip = ((quantityPricing.Currency != null) ? Strings.ConfirmCard.Earning.ToString(quantityPricing.UnitPrice.ToString("N0").Replace(',', ' '), quantityPricing.Currency.Name) : null),
				ConfirmLabel = Strings.ConfirmCard.Sell,
				Emblem = Globals.ContentManager.GetTexture(TextureType.Item, itemDescriptor.Icon),
			};
			ConfirmCardWindow.Open(confirmCardRequest, delegate
			{
				PacketSender.SendSellItem(inventorySlotIndex, 1);
			});
			return;
		}
		goto IL_00b4;
		IL_00b4:
		ConfirmCardWindow.OpenNotice(Strings.ConfirmCard.SellTitle, itemDescriptor.Name, Strings.Shop.CannotSell.ToString(itemDescriptor.Name), Globals.ContentManager.GetTexture(TextureType.Item, itemDescriptor.Icon));
	}

	private static QuantityPricing SellPricing(ShopDescriptor shop, ItemDescriptor item)
	{
		ShopItemDescriptor shopItemDescriptor = (shop.BuyingWhitelist ? shop.BuyingItems.FirstOrDefault((ShopItemDescriptor buying) => buying.ItemId == item.Id) : null);
		if (shopItemDescriptor == null)
		{
			return new QuantityPricing(DatabaseObject<ItemDescriptor>.Get(shop.DefaultCurrencyId), item.Price, Earning: true);
		}
		return new QuantityPricing(DatabaseObject<ItemDescriptor>.Get(shopItemDescriptor.CostItemId), shopItemDescriptor.CostItemQuantity, Earning: true);
	}

	public void TryBuyItem(int shopSlotIndex)
	{
		ShopItemDescriptor shopItemDescriptor = Globals.GameShop?.SellingItems[shopSlotIndex];
		if (shopItemDescriptor == null || !DatabaseObject<ItemDescriptor>.TryGet(shopItemDescriptor.ItemId, out var itemDescriptor))
		{
			return;
		}
		int num = base.Inventory.Count((IItem inventorySlot) => inventorySlot.ItemId == default(Guid));
		int num2 = base.Inventory.Count((IItem inventorySlot) => inventorySlot.ItemId == itemDescriptor.Id);
		int quantityOfItemInInventory = GetQuantityOfItemInInventory(shopItemDescriptor.ItemId);
		int num3 = ((!itemDescriptor.IsStackable) ? 1 : itemDescriptor.MaxInventoryStack);
		int num4 = num2 * num3 - quantityOfItemInInventory;
		int num5 = num * num3 + num4;
		int num6 = num5;
		ItemDescriptor itemDescriptor2 = DatabaseObject<ItemDescriptor>.Get(shopItemDescriptor.CostItemId);
		string limitHint = null;
		if (shopItemDescriptor.CostItemQuantity >= 1)
		{
			long num7 = ((itemDescriptor2 != null && itemDescriptor2.ItemType == ItemType.Currency) ? Zeni : GetQuantityOfItemInInventory(shopItemDescriptor.CostItemId));
			int num8 = (int)Math.Min(2147483647.0, Math.Floor((double)num7 / (double)shopItemDescriptor.CostItemQuantity));
			num6 = Math.Min(num8, num5);
			if (num8 < num5)
			{
				limitHint = Strings.Quantity.LimitedBy.ToString(itemDescriptor2?.Name ?? string.Empty);
			}
			else if (num5 < num8)
			{
				limitHint = Strings.Quantity.LimitedByBag;
			}
		}
		if (!itemDescriptor.IsStackable || num6 == 0)
		{
			PacketSender.SendBuyItem(shopSlotIndex, 1);
			return;
		}
		ShopDescriptor shop = Globals.GameShop;
		new QuantityWindow(Strings.Shop.BuyItem, itemDescriptor, Strings.Shop.Buy, 1, num6, delegate(int amount)
		{
			PacketSender.SendBuyItem(shopSlotIndex, amount);
		}, (shopItemDescriptor.CostItemQuantity >= 1) ? new QuantityPricing(itemDescriptor2, shopItemDescriptor.CostItemQuantity, Earning: false) : null, limitHint, () => Globals.GameShop == shop);
	}

	public bool TryStoreItemInBank(int inventorySlotIndex, IItem? slot = null, int bankSlotIndex = -1, int quantityHint = -1, bool skipPrompt = false)
	{
		if (Globals.IsGuildBank && !IsGuildBankDepositAllowed())
		{
			ChatboxMsg.AddMessage(new ChatboxMsg(Strings.Guilds.NotAllowedDeposit.ToString(Globals.Me?.Guild), CustomColors.Alerts.Error, ChatMessageType.Bank));
			BankWindow.Refuse(BankWindow.Refusal.Deposit);
			return false;
		}
		if (slot == null)
		{
			slot = base.Inventory[inventorySlotIndex];
		}
		if (!DatabaseObject<ItemDescriptor>.TryGet(slot.ItemId, out var databaseObject))
		{
			ApplicationContext.Context.Value?.Logger.LogWarning($"Tried to move item that does not exist from slot {inventorySlotIndex}: {databaseObject.Id}");
			return false;
		}
		if (quantityHint == 0)
		{
			ApplicationContext.Context.Value?.Logger.LogWarning($"Tried to move 0 of '{databaseObject.Name}' ({databaseObject.Id})");
			return false;
		}
		int maximumStack = ((!databaseObject.Stackable) ? 1 : databaseObject.MaxBankStack);
		int quantityOfItemInInventory = GetQuantityOfItemInInventory(databaseObject.Id);
		int num = ((quantityHint < 0) ? quantityOfItemInInventory : quantityHint);
		Item[] slots = Globals.BankSlots.ToArray();
		int num2 = Item.FindSpaceForItem(databaseObject.Id, databaseObject.ItemType, maximumStack, bankSlotIndex, (quantityHint < 0) ? quantityOfItemInInventory : quantityHint, slots);
		if (num2 < 1)
		{
			ChatboxMsg.AddMessage(new ChatboxMsg(Strings.Bank.NoSpace, CustomColors.Alerts.Error, ChatMessageType.Bank));
			BankWindow.Refuse(BankWindow.Refusal.BankFull);
			return false;
		}
		if (bankSlotIndex < 0 && databaseObject.IsStackable)
		{
			bankSlotIndex = Item.FindFirstPartialSlot(databaseObject.Id, slots, maximumStack);
		}
		if (skipPrompt)
		{
			PacketSender.SendDepositItem(inventorySlotIndex, num2, bankSlotIndex);
			return true;
		}
		int num3 = ((num2 < num) ? num2 : Item.FindSpaceForItem(databaseObject.Id, databaseObject.ItemType, maximumStack, bankSlotIndex, quantityOfItemInInventory, slots));
		if (num3 == 1)
		{
			PacketSender.SendDepositItem(inventorySlotIndex, num2, bankSlotIndex);
			return true;
		}
		int targetBankSlot = bankSlotIndex;
		new QuantityWindow(Strings.Bank.DepositItem, databaseObject, Strings.Quantity.Deposit, num2, num3, delegate(int amount)
		{
			PacketSender.SendDepositItem(inventorySlotIndex, amount, targetBankSlot);
		}, null, null, () => Globals.InBank);
		return true;
	}

	private static bool IsGuildBankDepositAllowed()
	{
		if (!string.IsNullOrWhiteSpace(Globals.Me?.Guild))
		{
			Player? me = Globals.Me;
			if (me == null || me.GuildRank?.Permissions.BankDeposit != true)
			{
				Player? me2 = Globals.Me;
				if (me2 == null)
				{
					return false;
				}
				return me2.Rank == 0;
			}
			return true;
		}
		return false;
	}

	public bool TryRetrieveItemFromBank(int bankSlotIndex, IItem? slot = null, int inventorySlotIndex = -1, int quantityHint = -1, bool skipPrompt = false)
	{
		if (Globals.IsGuildBank && !IsGuildBankWithdrawAllowed())
		{
			ChatboxMsg.AddMessage(new ChatboxMsg(Strings.Guilds.NotAllowedWithdraw.ToString(Globals.Me?.Guild), CustomColors.Alerts.Error, ChatMessageType.Bank));
			BankWindow.Refuse(BankWindow.Refusal.Withdraw);
			return false;
		}
		if (slot == null)
		{
			slot = Globals.BankSlots[bankSlotIndex];
		}
		if (!DatabaseObject<ItemDescriptor>.TryGet(slot.ItemId, out var databaseObject))
		{
			ApplicationContext.Context.Value?.Logger.LogWarning($"Tried to move item that does not exist from slot {bankSlotIndex}: {databaseObject.Id}");
			return false;
		}
		if (quantityHint == 0)
		{
			ApplicationContext.Context.Value?.Logger.LogWarning($"Tried to move 0 of '{databaseObject.Name}' ({databaseObject.Id})");
			return false;
		}
		int maximumStack = ((!databaseObject.Stackable) ? 1 : databaseObject.MaxInventoryStack);
		int quantityOfItemInBank = GetQuantityOfItemInBank(databaseObject.Id);
		int num = ((quantityHint < 0) ? quantityOfItemInBank : quantityHint);
		IItem[] slots = base.Inventory.ToArray();
		int num2 = Item.FindSpaceForItem(databaseObject.Id, databaseObject.ItemType, maximumStack, bankSlotIndex, (quantityHint < 0) ? quantityOfItemInBank : quantityHint, slots);
		if (num2 < 1)
		{
			ChatboxMsg.AddMessage(new ChatboxMsg(Strings.Bank.WithdrawItemNoSpace, CustomColors.Alerts.Error, ChatMessageType.Bank));
			BankWindow.Refuse(BankWindow.Refusal.BagFull);
			return false;
		}
		if (inventorySlotIndex < 0 && databaseObject.IsStackable)
		{
			inventorySlotIndex = Item.FindFirstPartialSlot(databaseObject.Id, slots, maximumStack);
		}
		if (skipPrompt)
		{
			PacketSender.SendWithdrawItem(bankSlotIndex, num2, inventorySlotIndex);
			return true;
		}
		int num3 = ((num2 < num) ? num2 : Item.FindSpaceForItem(databaseObject.Id, databaseObject.ItemType, maximumStack, inventorySlotIndex, quantityOfItemInBank, slots));
		if (num3 == 1)
		{
			PacketSender.SendWithdrawItem(bankSlotIndex, num2, inventorySlotIndex);
			return true;
		}
		int targetInventorySlot = inventorySlotIndex;
		new QuantityWindow(Strings.Bank.WithdrawItem, databaseObject, Strings.Quantity.Withdraw, num2, num3, delegate(int amount)
		{
			PacketSender.SendWithdrawItem(bankSlotIndex, amount, targetInventorySlot);
		}, null, null, () => Globals.InBank);
		return true;
	}

	private static bool IsGuildBankWithdrawAllowed()
	{
		if (!string.IsNullOrWhiteSpace(Globals.Me?.Guild))
		{
			Player? me = Globals.Me;
			if (me == null || me.GuildRank?.Permissions.BankRetrieve != true)
			{
				Player? me2 = Globals.Me;
				if (me2 == null)
				{
					return false;
				}
				return me2.Rank == 0;
			}
			return true;
		}
		return false;
	}

	public void TryStoreItemInBag(int inventorySlotIndex, int bagSlotIndex)
	{
		IItem item = base.Inventory[inventorySlotIndex];
		if (!DatabaseObject<ItemDescriptor>.TryGet(item.ItemId, out var databaseObject))
		{
			return;
		}
		int quantity = item.Quantity;
		int num = quantity;
		if (num < 2)
		{
			PacketSender.SendStoreBagItem(inventorySlotIndex, 1, bagSlotIndex);
			return;
		}
		new QuantityWindow(Strings.Bags.StoreItem, databaseObject, Strings.Quantity.Store, quantity, num, delegate(int amount)
		{
			PacketSender.SendStoreBagItem(inventorySlotIndex, amount, bagSlotIndex);
		}, null, null, () => Globals.InBag);
	}

	public void TryRetrieveItemFromBag(int bagSlotIndex, int inventorySlotIndex)
	{
		Item item = Globals.BagSlots[bagSlotIndex];
		if (item == null || !DatabaseObject<ItemDescriptor>.TryGet(item.ItemId, out var databaseObject))
		{
			return;
		}
		int quantity = item.Quantity;
		int num = quantity;
		if (num < 2)
		{
			PacketSender.SendRetrieveBagItem(bagSlotIndex, 1, inventorySlotIndex);
			return;
		}
		new QuantityWindow(Strings.Bags.RetrieveItem, databaseObject, Strings.Quantity.Retrieve, quantity, num, delegate(int amount)
		{
			PacketSender.SendRetrieveBagItem(bagSlotIndex, amount, inventorySlotIndex);
		}, null, null, () => Globals.InBag);
	}

	public void TryOfferItemToTrade(int index)
	{
		IItem item = base.Inventory[index];
		int quantity = item.Quantity;
		ItemDescriptor itemDescriptor = DatabaseObject<ItemDescriptor>.Get(item.ItemId);
		if (itemDescriptor == null)
		{
			return;
		}
		if (quantity == 1)
		{
			PacketSender.SendOfferTradeItem(index, 1);
			return;
		}
		new QuantityWindow(Strings.Trading.OfferItem, itemDescriptor, Strings.Quantity.Offer, quantity, quantity, delegate(int amount)
		{
			PacketSender.SendOfferTradeItem(index, amount);
		}, null, null, () => Globals.InTrade);
	}

	public void TryCancelOfferToTradeItem(int index)
	{
		Item item = Globals.Trade[0, index];
		int quantity = item.Quantity;
		ItemDescriptor itemDescriptor = DatabaseObject<ItemDescriptor>.Get(item.ItemId);
		if (itemDescriptor == null)
		{
			return;
		}
		if (quantity == 1)
		{
			PacketSender.SendRevokeTradeItem(index, 1);
			return;
		}
		new QuantityWindow(Strings.Trading.RevokeItem, itemDescriptor, Strings.Quantity.Revoke, quantity, quantity, delegate(int amount)
		{
			PacketSender.SendRevokeTradeItem(index, amount);
		}, null, null, () => Globals.InTrade);
	}

	public void SwapSpells(int fromSpellIndex, int toSpellIndex)
	{
		PacketSender.SendSwapSpells(fromSpellIndex, toSpellIndex);
		Spell[] spells = base.Spells;
		Spell[] spells2 = base.Spells;
		Spell spell = base.Spells[toSpellIndex];
		Spell spell2 = base.Spells[fromSpellIndex];
		spells[fromSpellIndex] = spell;
		spells2[toSpellIndex] = spell2;
	}

	public void TryForgetSpell(int spellIndex)
	{
		Spell spell = base.Spells[spellIndex];
		if (DatabaseObject<SpellDescriptor>.TryGet(spell.Id, out var databaseObject))
		{
			ConfirmCardWindow.Open(new ConfirmCardRequest
			{
				Title = Strings.ConfirmCard.ForgetTitle,
				Heading = databaseObject.Name,
				Prompt = Strings.ConfirmCard.ForgetPrompt,
				Detail = Strings.ConfirmCard.ForgetDetail,
				ConfirmLabel = Strings.ConfirmCard.Forget,
				Danger = true,
				Emblem = Globals.ContentManager.GetTexture(TextureType.Spell, databaseObject.Icon)
			}, delegate
			{
				PacketSender.SendForgetSpell(spellIndex);
			});
		}
	}

	public void TryUseSpell(int index)
	{
		if (index < 0 || base.Spells.Length <= index || base.IsCharging || base.IsBlocking || base.IsGuardStunned || base.IsTransforming || IsBeaming)
		{
			return;
		}
		Spell spell = base.Spells[index];
		if (spell.Id == default(Guid))
		{
			return;
		}
		long spellRemainingCooldown = GetSpellRemainingCooldown(index);
		if (spellRemainingCooldown > 0)
		{
			if (spellRemainingCooldown <= 200)
			{
				_bufferedSpellSlot = index;
				_bufferedSpellUntil = Timing.Global.Milliseconds + spellRemainingCooldown + 200;
			}
		}
		else
		{
			if ((Heritage.Actif && Heritage.Lien.Garder(this, index)) || !DatabaseObject<SpellDescriptor>.TryGet(spell.Id, out var databaseObject) || TryBeginBeam(index, databaseObject) || TryBeginMaitre(index, databaseObject))
			{
				return;
			}
			if (databaseObject.CastDuration > 0 && Options.Instance.Combat.MovementCancelsCast)
			{
				Player? me = Globals.Me;
				if (me != null && me.IsMoving)
				{
					return;
				}
			}
			if (!Heritage.Actif || !Heritage.Lien.Tourner(this))
			{
				AimTechnique(databaseObject);
			}
			Guid guid = TargetId;
			if (databaseObject.SpellType == SpellType.CombatSpell && databaseObject.Combat.TargetType == SpellTargetType.Projectile && (!Heritage.Actif || !Heritage.Lien.EnDepart))
			{
				guid = AimProjectile(databaseObject);
			}
			if (databaseObject.CastDuration > 0 && !Options.Instance.Combat.MovementCancelsCast)
			{
				PoserSaPositionAvantDeSeFiger();
			}
			PacketSender.SendUseSpell(index, Heritage.Actif ? Heritage.Lien.CibleDe(guid) : guid, (byte)(Heritage.Actif ? Heritage.Lien.Octet() : 0));
		}
	}

	private Guid AimProjectile(SpellDescriptor spell)
	{
		if (spell.Combat.Friendly)
		{
			return Guid.Empty;
		}
		int num = Math.Max(1, spell.Combat.Projectile?.Range ?? spell.Combat.CastRange);
		bool flag = base.IsMoving || DirectionMoving != Direction.None;
		var (num2, num3) = StepOf(base.DirectionFacing);
		if (num2 == 0 && num3 == 0)
		{
			num3 = 1;
		}
		float num4 = MathF.Sqrt(num2 * num2 + num3 * num3);
		if (Target is Entity entity && entity != this && !entity.IsDisposed && !entity.IsDying && entity.CanBeAttacked && (!(entity is Player en) || !IsAllyOf(en)))
		{
			var (num5, num6) = TilesTo(entity);
			if (Math.Max(Math.Abs(num5), Math.Abs(num6)) <= (float)(num + 2))
			{
				if (!flag)
				{
					FaceTowards(num5, num6);
					return entity.Id;
				}
				if (num5 * (float)num2 + num6 * (float)num3 > 0f)
				{
					return entity.Id;
				}
			}
		}
		Entity entity2 = null;
		Entity entity3 = null;
		float num7 = float.MaxValue;
		float num8 = float.MaxValue;
		foreach (Entity value in Globals.Entities.Values)
		{
			if (value == null || value == this || value.IsDisposed || value.IsDying || !value.CanBeAttacked)
			{
				continue;
			}
			bool flag2 = value.Type == EntityType.GlobalEntity && !(value is Critter);
			bool flag3 = flag2;
			if (flag3)
			{
				NpcAggression aggression = value.Aggression;
				bool flag4 = (uint)(aggression - 3) <= 1u;
				flag3 = !flag4;
			}
			if (!flag3)
			{
				continue;
			}
			(float X, float Y) tuple3 = TilesTo(value);
			float item = tuple3.X;
			float item2 = tuple3.Y;
			float num9 = MathF.Sqrt(item * item + item2 * item2);
			if (Math.Max(Math.Abs(item), Math.Abs(item2)) > (float)num || num9 < 0.01f)
			{
				continue;
			}
			float num10 = (item * (float)num2 + item2 * (float)num3) / (num9 * num4);
			if (num10 >= 0.38f)
			{
				float num11 = num9 * (1.5f - 0.5f * num10);
				if (num11 < num7)
				{
					num7 = num11;
					entity2 = value;
				}
			}
			if (num9 < num8)
			{
				num8 = num9;
				entity3 = value;
			}
		}
		Entity entity4 = entity2 ?? (flag ? null : entity3);
		if (entity4 == null)
		{
			return Guid.Empty;
		}
		if (!flag)
		{
			var (dx, dy) = TilesTo(entity4);
			FaceTowards(dx, dy);
		}
		return entity4.Id;
	}

	private (float X, float Y) TilesTo(Entity entity)
	{
		return (X: (entity.WorldPos.X + entity.WorldPos.Width / 2f - (base.WorldPos.X + base.WorldPos.Width / 2f)) / (float)Options.Instance.Map.TileWidth, Y: (entity.WorldPos.Y + entity.WorldPos.Height - (base.WorldPos.Y + base.WorldPos.Height)) / (float)Options.Instance.Map.TileHeight);
	}

	private void FaceTowards(float dx, float dy)
	{
		if (!(Math.Abs(dx) < 0.5f) || !(Math.Abs(dy) < 0.5f))
		{
			Direction direction = DirectionToward(dx, dy);
			if (direction != base.DirectionFacing)
			{
				base.DirectionFacing = direction;
				PacketSender.SendDirection(direction);
			}
		}
	}

	private static Direction DirectionToward(float dx, float dy)
	{
		if (Options.Instance.Map.EnableDiagonalMovement && Math.Abs(dx) > 0.01f && Math.Abs(dy) > 0.01f)
		{
			float num = Math.Abs(dy / dx);
			if (num > 0.4142f && num < 2.4142f)
			{
				bool flag = dx < 0f;
				bool flag2 = dy < 0f;
				if (flag)
				{
					if (flag2)
					{
						return Direction.UpLeft;
					}
					return Direction.DownLeft;
				}
				if (flag2)
				{
					return Direction.UpRight;
				}
				return Direction.DownRight;
			}
		}
		if (!(Math.Abs(dx) >= Math.Abs(dy)))
		{
			if (!(dy < 0f))
			{
				return Direction.Down;
			}
			return Direction.Up;
		}
		if (!(dx < 0f))
		{
			return Direction.Right;
		}
		return Direction.Left;
	}

	public long GetSpellCooldown(Guid id)
	{
		if (SpellCooldowns.TryGetValue(id, out var value) && value > Timing.Global.Milliseconds)
		{
			return value;
		}
		if (DatabaseObject<SpellDescriptor>.TryGet(id, out var databaseObject) && !databaseObject.IgnoreGlobalCooldown && Globals.Me?.GlobalCooldown > Timing.Global.Milliseconds)
		{
			return Globals.Me.GlobalCooldown;
		}
		return 0L;
	}

	private void AimTechnique(SpellDescriptor spell)
	{
		if (spell.SpellType != SpellType.CombatSpell || spell.Combat.Friendly || spell.Combat.TargetType != SpellTargetType.Single)
		{
			return;
		}
		int range = Math.Max(1, spell.Combat.CastRange);
		if (Target is Entity { IsDisposed: false, IsDying: false, CanBeAttacked: not false } entity && InRange(entity))
		{
			return;
		}
		(int X, int Y) tuple = StepOf(base.DirectionFacing);
		int item = tuple.X;
		int item2 = tuple.Y;
		Entity entity2 = null;
		double num = double.MaxValue;
		foreach (Entity value in Globals.Entities.Values)
		{
			if (value == null || value == this || value.IsDisposed || value.IsDying || !value.CanBeAttacked || !InRange(value))
			{
				continue;
			}
			bool flag = value.Type == EntityType.GlobalEntity && !(value is Critter);
			bool flag2 = flag;
			if (flag2)
			{
				NpcAggression aggression = value.Aggression;
				bool flag3 = (uint)(aggression - 3) <= 1u;
				flag2 = !flag3;
			}
			if (flag2)
			{
				int num2 = value.X - base.X;
				int num3 = value.Y - base.Y;
				double num4 = ((num2 * item + num3 * item2 > 0) ? 0.0 : 0.5);
				double num5 = Math.Sqrt(num2 * num2 + num3 * num3) + num4;
				if (num5 < num)
				{
					num = num5;
					entity2 = value;
				}
			}
		}
		if (entity2 != null)
		{
			TryTarget(entity2);
		}
		bool InRange(IEntity entity3)
		{
			if (entity3.MapId == MapId)
			{
				return Math.Max(Math.Abs(entity3.X - base.X), Math.Abs(entity3.Y - base.Y)) <= range;
			}
			return false;
		}
	}

	public void TryUseSpell(Guid spellId)
	{
		if (spellId == Guid.Empty)
		{
			return;
		}
		for (int i = 0; i < base.Spells.Length; i++)
		{
			if (base.Spells[i].Id == spellId)
			{
				TryUseSpell(i);
				break;
			}
		}
	}

	public int FindHotbarSpell(IHotbarInstance hotbarInstance)
	{
		if (hotbarInstance.ItemOrSpellId != Guid.Empty && DatabaseObject<SpellDescriptor>.Get(hotbarInstance.ItemOrSpellId) != null)
		{
			for (int i = 0; i < base.Spells.Length; i++)
			{
				if (base.Spells[i].Id == hotbarInstance.ItemOrSpellId)
				{
					return i;
				}
			}
		}
		return -1;
	}

	public void AddToHotbar(int hotbarSlot, sbyte itemType, int itemSlot)
	{
		Hotbar[hotbarSlot].ItemOrSpellId = Guid.Empty;
		Hotbar[hotbarSlot].PreferredStatBuffs = new int[Enum.GetValues<Stat>().Length];
		switch (itemType)
		{
		case 0:
		{
			IItem item = base.Inventory[itemSlot];
			if (item != null)
			{
				Hotbar[hotbarSlot].ItemOrSpellId = item.ItemId;
				Hotbar[hotbarSlot].PreferredStatBuffs = item.ItemProperties.StatModifiers;
			}
			break;
		}
		case 1:
		{
			Spell spell = base.Spells[itemSlot];
			if (spell != null)
			{
				Hotbar[hotbarSlot].ItemOrSpellId = spell.Id;
			}
			break;
		}
		}
		PacketSender.SendHotbarUpdate(hotbarSlot, itemType, itemSlot);
	}

	public void HotbarSwap(int index, int swapIndex)
	{
		Guid itemOrSpellId = Hotbar[index].ItemOrSpellId;
		Guid bagId = Hotbar[index].BagId;
		int[] preferredStatBuffs = Hotbar[index].PreferredStatBuffs;
		Hotbar[index].ItemOrSpellId = Hotbar[swapIndex].ItemOrSpellId;
		Hotbar[index].BagId = Hotbar[swapIndex].BagId;
		Hotbar[index].PreferredStatBuffs = Hotbar[swapIndex].PreferredStatBuffs;
		Hotbar[swapIndex].ItemOrSpellId = itemOrSpellId;
		Hotbar[swapIndex].BagId = bagId;
		Hotbar[swapIndex].PreferredStatBuffs = preferredStatBuffs;
		PacketSender.SendHotbarSwap(index, swapIndex);
	}

	private void TryToChangeDimension()
	{
		int tileX = base.TileX;
		if (tileX >= MapWidth || tileX < 0)
		{
			return;
		}
		int tileY = base.TileY;
		if (tileY >= MapHeight || tileY < 0 || !Intersect.Client.Maps.MapInstance.TryGet(MapId, out MapInstance instance))
		{
			return;
		}
		MapAttribute mapAttribute = instance.Attributes[tileX, tileY];
		if (!(mapAttribute is MapZDimensionAttribute { Type: MapAttributeType.ZDimension } mapZDimensionAttribute))
		{
			return;
		}
		int num = mapZDimensionAttribute.GatewayTo - 1;
		if (num >= 0)
		{
			int tileZ = base.TileZ;
			if (tileZ != num)
			{
				Vector3 position = base.Position;
				position.Z = mapZDimensionAttribute.GatewayTo - 1;
				base.Position = position;
			}
		}
	}

	private Direction GetInputDirection()
	{
		if (Intersect.Client.Interface.Interface.HasInputFocus() || CharteScreen.EstOuvert)
		{
			return Direction.None;
		}
		int num = 0;
		int num2 = 0;
		if (Controls.IsGameControlPressed(Control.MoveUp))
		{
			num2++;
		}
		if (Controls.IsGameControlPressed(Control.MoveDown))
		{
			num2--;
		}
		if (Controls.IsGameControlPressed(Control.MoveLeft))
		{
			num--;
		}
		if (Controls.IsGameControlPressed(Control.MoveRight))
		{
			num++;
		}
		if (num == 0 && num2 == 0)
		{
			return Direction.None;
		}
		bool flag = num != 0 && Options.Instance.Map.EnableDiagonalMovement;
		int num3 = Math.Sign(num);
		Direction direction = ((num3 < 0) ? Direction.Left : ((num3 <= 0) ? Direction.None : Direction.Right));
		Direction direction2 = direction;
		int num4 = Math.Sign(num2);
		direction = ((num4 < 0) ? Direction.Down : ((num4 <= 0) ? Direction.None : Direction.Up));
		Direction direction3 = direction;
		if (flag)
		{
			return direction3 switch
			{
				Direction.Down => direction2 switch
				{
					Direction.Left => Direction.DownLeft, 
					Direction.Right => Direction.DownRight, 
					_ => direction3, 
				}, 
				Direction.Up => direction2 switch
				{
					Direction.Left => Direction.UpLeft, 
					Direction.Right => Direction.UpRight, 
					_ => direction3, 
				}, 
				_ => direction2, 
			};
		}
		if (direction3 != Direction.None)
		{
			return direction3;
		}
		return direction2;
	}

	private void HandleInput()
	{
		if (Globals.Me != this || Intersect.Client.Interface.Interface.HasInputFocus() || CharteScreen.EstOuvert)
		{
			return;
		}
		Direction inputDirection = GetInputDirection();
		if (TryTurnAround(inputDirection))
		{
			DirectionMoving = Direction.None;
		}
		else
		{
			DirectionMoving = inputDirection;
		}
		if (!FenetreCharte.RetientLesRaccourcis)
		{
			int num = -1;
			ControlMapping activeMapping;
			ControlBinding activeBinding;
			IGrouping<Keys, (int, ControlBinding)>[] source = (from controlHit in (from slotIndex in Enumerable.Range(0, Options.Instance.Player.HotbarSlotCount)
					select ((int slotIndex, ControlBinding activeBinding))((!Controls.IsControlPressed((Control)(4096 + slotIndex + 1), out activeMapping, out activeBinding)) ? default((int, ControlBinding)) : (slotIndex: slotIndex, activeBinding: activeBinding))).Where(delegate((int slotIndex, ControlBinding activeBinding) controlHit)
				{
					var (num4, controlBinding) = controlHit;
					return num4 != 0 || controlBinding != null;
				})
				orderby controlHit.activeBinding.Modifier descending
				group controlHit by controlHit.activeBinding.Key).ToArray();
			int[] array = source.Select((IGrouping<Keys, (int slotIndex, ControlBinding activeBinding)> group) => group.FirstOrDefault().slotIndex).ToArray();
			int[] array2 = array;
			foreach (int num3 in array2)
			{
				mLastHotbarUseTime.TryAdd(num3, 0L);
				num = num3;
			}
			if (Heritage.Actif)
			{
				num = CaseDeLaBarreHeritage(num);
			}
			if (Intersect.Client.Interface.Interface.HasInGameUI && 0 <= num && num < Intersect.Client.Interface.Interface.GameUi.Hotbar?.Items?.Count && mLastHotbarUseTime[num] < Timing.Global.Milliseconds)
			{
				Intersect.Client.Interface.Interface.GameUi.Hotbar?.Items?[num]?.Activate();
				mLastHotbarUseTime[num] = Timing.Global.Milliseconds + mHotbarUseDelay;
			}
		}
	}

	protected int GetDistanceTo(IEntity target)
	{
		if (target != null)
		{
			MapInstance mapInstance = Intersect.Client.Maps.MapInstance.Get(MapId);
			MapInstance mapInstance2 = Intersect.Client.Maps.MapInstance.Get(target.MapId);
			if (mapInstance != null && mapInstance2 != null)
			{
				int num = base.X + mapInstance.GridX * MapWidth;
				int num2 = base.Y + mapInstance.GridY * MapHeight;
				int num3 = target.X + mapInstance2.GridX * MapWidth;
				int num4 = target.Y + mapInstance2.GridY * MapHeight;
				return (int)Math.Sqrt(Math.Pow(num - num3, 2.0) + Math.Pow(num2 - num4, 2.0));
			}
		}
		return 9999;
	}

	public void AutoTarget()
	{
		for (int i = 0; i < base.Status.Count; i++)
		{
			if (base.Status[i].Type == SpellEffect.Taunt)
			{
				return;
			}
		}
		if (Globals.Me?.MapInstance == null)
		{
			return;
		}
		bool flag = !(Globals.Me.MapInstance is MapInstance mapInstance) || mapInstance.ZoneType != MapZone.Safe;
		if (mlastTargetScanTime < Timing.Global.Milliseconds || mlastTargetScanMap != Globals.Me.MapId || mlastTargetScanLocation != new Point(base.X, base.Y))
		{
			foreach (KeyValuePair<Guid, Entity> entity in Globals.Entities)
			{
				if (entity.Value == null || entity.Value == Globals.Me || entity.Value.IsHidden || (entity.Value.IsStealthed && !Globals.Me.IsInMyParty(entity.Value.Id)) || (!flag && entity.Value.Type == EntityType.Player) || (flag && entity.Value.Type == EntityType.Player && entity.Value is Player player && IsInMyParty(player)))
				{
					continue;
				}
				EntityType type = entity.Value.Type;
				if ((uint)type <= 1u)
				{
					if (mlastTargetList.TryGetValue(entity.Value, out TargetInfo value))
					{
						value.DistanceTo = GetDistanceTo(entity.Value);
						continue;
					}
					mlastTargetList.Add(entity.Value, new TargetInfo
					{
						DistanceTo = GetDistanceTo(entity.Value),
						LastTimeSelected = 0L
					});
				}
			}
			KeyValuePair<Entity, TargetInfo>[] array = mlastTargetList.Where<KeyValuePair<Entity, TargetInfo>>((KeyValuePair<Entity, TargetInfo> en) => !Globals.Entities.ContainsValue(en.Key)).ToArray();
			KeyValuePair<Entity, TargetInfo>[] array2 = array;
			foreach (KeyValuePair<Entity, TargetInfo> keyValuePair in array2)
			{
				mlastTargetList.Remove(keyValuePair.Key);
			}
			mlastTargetScanTime = Timing.Global.Milliseconds + 300;
			mlastTargetScanMap = MapId;
			mlastTargetScanLocation = new Point(base.X, base.Y);
		}
		KeyValuePair<Entity, TargetInfo>[] validEntities = mlastTargetList.ToArray();
		validEntities = validEntities.Where((KeyValuePair<Entity, TargetInfo> en) => en.Value.DistanceTo <= Options.Instance.Combat.MaxPlayerAutoTargetRadius).ToArray();
		int num2 = 9999;
		long num3 = Timing.Global.Milliseconds;
		Entity key = mLastEntitySelected;
		KeyValuePair<Entity, TargetInfo>[] array3 = validEntities;
		for (int num4 = 0; num4 < array3.Length; num4++)
		{
			KeyValuePair<Entity, TargetInfo> keyValuePair2 = array3[num4];
			if (key == keyValuePair2.Key)
			{
				continue;
			}
			if (keyValuePair2.Value.DistanceTo == num2)
			{
				if (keyValuePair2.Value.LastTimeSelected < num3)
				{
					num3 = keyValuePair2.Value.LastTimeSelected;
					num2 = keyValuePair2.Value.DistanceTo;
					key = keyValuePair2.Key;
				}
			}
			else if (keyValuePair2.Value.DistanceTo < num2 && (keyValuePair2.Value.LastTimeSelected < num3 || keyValuePair2.Value.LastTimeSelected == num3))
			{
				num3 = keyValuePair2.Value.LastTimeSelected;
				num2 = keyValuePair2.Value.DistanceTo;
				key = keyValuePair2.Key;
			}
		}
		if (key == null)
		{
			key = validEntities.Where((KeyValuePair<Entity, TargetInfo> x) => x.Value.DistanceTo == validEntities.Min((KeyValuePair<Entity, TargetInfo> y) => y.Value.DistanceTo)).FirstOrDefault().Key;
			foreach (KeyValuePair<Entity, TargetInfo> mlastTarget in mlastTargetList)
			{
				mlastTarget.Value.LastTimeSelected = 0L;
			}
		}
		Entity value2;
		if (key == null)
		{
			mLastEntitySelected = null;
		}
		else if (Globals.Entities.TryGetValue(key.Id, out value2))
		{
			if (mlastTargetList.TryGetValue(value2, out TargetInfo value3))
			{
				value3.LastTimeSelected = Timing.Global.Milliseconds;
			}
			mLastEntitySelected = value2;
			if (TargetId != value2.Id)
			{
				Target = value2;
			}
		}
	}

	private void SetTargetBox(IEntity? targetEntity)
	{
		if (targetEntity != null)
		{
			if (!(targetEntity is Player))
			{
				if (targetEntity is Event)
				{
					TargetBox?.SetEntity(targetEntity, EntityType.Event);
				}
				else
				{
					TargetBox?.SetEntity(targetEntity, EntityType.GlobalEntity);
				}
			}
			else
			{
				TargetBox?.SetEntity(targetEntity, EntityType.Player);
			}
			TargetBox?.Show();
			PacketSender.SendTarget(targetEntity.Id);
			return;
		}
		EntityBox targetBox = TargetBox;
		if (targetBox != null)
		{
			TargetBox.SetEntity(null);
			if (targetBox.IsVisible)
			{
				TargetBox.Hide();
			}
		}
	}

	private void AutoTurnToTarget(Entity en)
	{
		if (en != this && Globals.Database.AutoTurnToTarget && Options.Instance.Player.EnableAutoTurnToTarget && base.CanTurnAround && !Controls.IsControlPressed(Control.TurnAround))
		{
			Direction direction = DirectionToTarget(en);
			if (base.IsMoving || base.DirectionFacing == DirectionMoving || base.DirectionFacing == direction)
			{
				base.AutoTurnToTargetTimer = Timing.Global.Milliseconds + Options.Instance.Player.AutoTurnToTargetDelay;
			}
			else if (base.AutoTurnToTargetTimer <= Timing.Global.Milliseconds && (!Options.Instance.Player.AutoTurnToTargetIgnoresEntitiesBehind || !IsTargetAtOppositeDirection(base.DirectionFacing, direction)))
			{
				DirectionMoving = Direction.None;
				base.DirectionFacing = direction;
				PacketSender.SendDirection(base.DirectionFacing);
				PickLastDirection(base.DirectionFacing);
			}
		}
	}

	private static void ToggleTargetContextMenu(Entity en)
	{
		if (Globals.InputManager.IsMouseButtonDown(MouseButton.Right))
		{
			Intersect.Client.Interface.Interface.GameUi.TargetContextMenu.ToggleHidden(en);
		}
	}

	public bool TryBlock()
	{
		if (Options.Instance.Combat.EnableGuard)
		{
			return false;
		}
		int shieldSlot = Options.Instance.Equipment.ShieldSlot;
		int num = base.MyEquipment[shieldSlot];
		if (base.IsAttacking || base.IsBlocking || shieldSlot < 0 || num < 0)
		{
			return false;
		}
		if (!DatabaseObject<ItemDescriptor>.TryGet(base.Inventory[num].ItemId, out var _))
		{
			return false;
		}
		base.IsBlocking = true;
		PacketSender.SendBlock(base.IsBlocking);
		return base.IsBlocking;
	}

	private void UpdateSprintState()
	{
		if (this == Globals.Me)
		{
			bool flag = Controls.IsGameControlPressed(Control.Sprint) || DashTenuPourCourir();
			if (!flag)
			{
				_sprintExhausted = false;
			}
			else if (!HasKiForSprintStep())
			{
				_sprintExhausted = true;
			}
			bool flag2 = Options.Instance.Player.EnableSprinting && !IsBusy && !base.IsCharging && !IsBeaming && flag && !_sprintExhausted;
			if (flag2 != base.IsSprinting)
			{
				base.IsSprinting = flag2;
				PacketSender.SendSprint(base.IsSprinting);
			}
		}
	}

	internal void ChargeCoupeeParLeServeur()
	{
		base.IsCharging = false;
		_chargeCoupee = true;
	}

	private bool HasKiForSprintStep()
	{
		if (Options.Instance.Player.SprintKiCost <= 0f || base.FlightMode == FlightMode.Cloud)
		{
			return true;
		}
		return base.Vital[1] >= 1;
	}

	private void UpdateChargeState()
	{
		if (this != Globals.Me)
		{
			return;
		}
		bool flag = Options.Instance.Player.EnableKiCharge && !IsBusy && !IsBeaming && !base.IsCasting && !base.IsTransforming && base.Dashing == null && base.FlightMode == FlightMode.None && Controls.IsGameControlPressed(Control.ChargeKi);
		if (!Controls.IsGameControlPressed(Control.ChargeKi))
		{
			_chargeCoupee = false;
		}
		else if (_chargeCoupee)
		{
			flag = false;
		}
		if (flag != base.IsCharging)
		{
			base.IsCharging = flag;
			if (base.IsCharging)
			{
				PoserSaPositionAvantDeSeFiger();
			}
			PacketSender.SendChargeKi(base.IsCharging);
		}
	}

	private void UpdateGuardState()
	{
		if (this != Globals.Me || !Options.Instance.Combat.EnableGuard)
		{
			return;
		}
		long milliseconds = Timing.Global.Milliseconds;
		bool flag = Controls.IsGameControlPressed(Control.Block);
		if (flag && Globals.InputManager.IsMouseButtonDown(MouseButton.Right) && RightClickBelongsElsewhere())
		{
			flag = Globals.InputManager.IsKeyDown(Keys.Q);
		}
		if (base.AttackTimer != _coupSuivi)
		{
			_coupSuivi = base.AttackTimer;
			if (base.AttackTimer > milliseconds)
			{
				_finDeLEngagement = Options.Instance.Combat.FinDeLEngagement(milliseconds, base.AttackTimer - milliseconds);
			}
		}
		bool flag2 = flag && milliseconds >= _finDeLEngagement && !IsBusy && !base.IsCharging && !IsBeaming && !base.IsCasting && base.Dashing == null && milliseconds >= base.GuardStunnedUntil && milliseconds >= base.GuardLockedUntil && milliseconds >= base.TransformingUntil;
		if (flag2 != base.IsBlocking)
		{
			base.IsBlocking = flag2;
			if (base.IsBlocking && base.IsAttacking)
			{
				base.AttackTimer = milliseconds;
			}
			PacketSender.SendBlock(base.IsBlocking);
		}
	}

	private static bool RightClickBelongsElsewhere()
	{
		if (Intersect.Client.Interface.Interface.DoesMouseHitInterface() || (Globals.InputManager.IsKeyDown(Keys.Shift) && Globals.InputManager.IsKeyDown(Keys.Control)))
		{
			return true;
		}
		foreach (Entity value in Globals.Entities.Values)
		{
			if (value is Player && value != Globals.Me && value.IsHovered)
			{
				return true;
			}
		}
		return false;
	}

	public bool TryAttack()
	{
		_dernierCoupRecolte = false;
		_recolteRefusee = false;
		if (base.IsAttacking || base.IsBlocking || base.IsCharging || IsBeaming || base.IsGuardStunned || base.IsTransforming || RueeEnCours || Echanges.EstSonne(this) || Echanges.ChargeLocale(this) || Echanges.ArmementEnCours(this) || Vanish.EstFige(this) || Clash.Retient(this) || (base.IsMoving && !Options.Instance.Player.AllowCombatMovement) || Globals.Me == null)
		{
			return false;
		}
		if (base.FlightMode != FlightMode.None)
		{
			if (Flight.IsDown(this))
			{
				return false;
			}
			if (TargetId == Guid.Empty || !Globals.Entities.ContainsKey(TargetId))
			{
				if (Timing.Global.Milliseconds > IsCastingCheckTimer)
				{
					ChatboxMsg.AddMessage(new ChatboxMsg(Strings.Flight.NeedTarget, CustomColors.Alerts.Declined, ChatMessageType.Combat));
					IsCastingCheckTimer = Timing.Global.Milliseconds + 900;
				}
				return false;
			}
			PacketSender.SendAttack(TargetId, ruee: false, GesteDeCoup.Ordinaire, 0);
			return true;
		}
		Entity entity = Echanges.PrendreLaCibleDuRenvoi();
		if (entity != null)
		{
			PacketSender.SendAttack(entity.Id, ruee: false, GesteDeCoup.Ordinaire, 0);
			base.AttackTimer = Timing.Global.Milliseconds + CalculateAttackTime();
			CombatAudio.Swing(this);
			return true;
		}
		Entity entity2 = null;
		if (base.EstPiloteLibrement)
		{
			entity2 = CibleAuCorpsACorpsLibre();
			if (entity2 == null && TenterLaRuee())
			{
				return true;
			}
		}
		else
		{
			FaceAdjacentFoe();
			foreach (var item4 in CasesDevantMoi())
			{
				Guid item = item4.Map;
				int item2 = item4.X;
				int item3 = item4.Y;
				entity2 = CibleSurCase(item, item2, item3, quiLaQuitte: false) ?? CibleSurCase(item, item2, item3, quiLaQuitte: true);
				if (entity2 != null)
				{
					break;
				}
			}
		}
		List<(Guid, int, int)> list = CasesDevantMoi();
		if (entity2 != null)
		{
			Entity entity3 = entity2;
			EchangeAvec(entity3);
			if (entity3 is Resource resource && (resource.IsDead || Recolte.NiveauTropBas(resource, this, avertir: true)))
			{
				_recolteRefusee = true;
				return false;
			}
			if (Heritage.Actif && !(entity3 is Resource) && LancerUnCoupHeritage(entity3))
			{
				return true;
			}
			PacketSender.SendAttack(entity3.Id, ruee: false, GesteDeCoup.Ordinaire, 0);
			int num = CalculateAttackTime();
			if (entity3 is Resource)
			{
				NoterUnGesteHorsCombo(num);
			}
			else
			{
				byte pas = PasPredit(entity3.Id);
				num = DureeLocaleDuCoup(pas);
				NoterLeCoupLocal(entity3.Id, pas, num);
			}
			base.AttackTimer = Timing.Global.Milliseconds + num;
			_attackApproachUntil = 0L;
			if (entity3 is Resource ressource)
			{
				_dernierCoupRecolte = true;
				Recolte.SurCoupLocal(ressource, this);
			}
			else
			{
				CombatAudio.Swing(this);
				CombatFeedback.OnLocalStrike(entity3, num);
			}
			return true;
		}
		if (Houses.TryInteract(this))
		{
			_appuiRendu = true;
			return true;
		}
		foreach (var (guid, num2, num3) in list)
		{
			foreach (MapInstance item5 in Intersect.Client.Maps.MapInstance.Lookup.Values.Cast<MapInstance>())
			{
				foreach (KeyValuePair<Guid, Entity> localEntity in item5.LocalEntities)
				{
					if (localEntity.Value != null && localEntity.Value.MapId == guid && localEntity.Value.X == num2 && localEntity.Value.Y == num3 && localEntity.Value is Event { Trigger: EventTrigger.ActionButton })
					{
						return ParlerA(localEntity.Key);
					}
				}
			}
		}
		if (Dungeon.TryInteract(this))
		{
			_appuiRendu = true;
			return true;
		}
		Resource resource2 = Recolte.CoinDePecheDevant(this);
		if (resource2 != null)
		{
			_dernierCoupRecolte = true;
			PacketSender.SendAttack(resource2.Id, ruee: false, GesteDeCoup.Ordinaire, 0);
			NoterUnGesteHorsCombo(CalculateAttackTime());
			base.AttackTimer = Timing.Global.Milliseconds + CalculateAttackTime();
			Recolte.SurCoupLocal(resource2, this);
			return true;
		}
		if (this == Globals.Me)
		{
			Guid guid2 = EvenementSousLeCurseur();
			if (guid2 != Guid.Empty)
			{
				return ParlerA(guid2);
			}
		}
		if (Heritage.Actif && TenterLaRueeSurDirectionTapee())
		{
			return true;
		}
		PacketSender.SendAttack(Guid.Empty, ruee: false, GesteDeCoup.Ordinaire, 0);
		NoterUnGesteHorsCombo(CalculateAttackTime());
		base.AttackTimer = Timing.Global.Milliseconds + CalculateAttackTime();
		CombatAudio.Swing(this);
		return true;
	}

	private void FaceAdjacentFoe()
	{
		if (this != Globals.Me || Controls.IsControlPressed(Control.TurnAround))
		{
			return;
		}
		Direction directionFacing = base.DirectionFacing;
		(int X, int Y) tuple = StepOf(directionFacing);
		int item = tuple.X;
		int item2 = tuple.Y;
		bool enableDiagonalMovement = Options.Instance.Map.EnableDiagonalMovement;
		Entity entity = null;
		Direction direction = directionFacing;
		int num = int.MaxValue;
		foreach (Entity value in Globals.Entities.Values)
		{
			if (value == null || value == this || value.MapId != MapId || !value.CanBeAttacked || value.IsDying || !CaseVoisineOuOnLeVoit(value, enableDiagonalMovement, out var dx, out var dy))
			{
				continue;
			}
			bool flag = value.Id == TargetId;
			bool flag2 = value.Type == EntityType.GlobalEntity && !(value is Critter);
			bool flag3 = flag2;
			if (flag3)
			{
				NpcAggression aggression = value.Aggression;
				bool flag4 = (uint)(aggression - 3) <= 1u;
				flag3 = !flag4;
			}
			bool flag5 = flag3;
			bool flag6 = dx == item && dy == item2;
			if (!flag && !flag5)
			{
				if (flag6)
				{
					return;
				}
				continue;
			}
			Direction direction2 = DirectionOfStep(dx, dy);
			int num2 = ((!flag) ? 10 : 0) + ((!flag6) ? (1 + TurnSteps(directionFacing, direction2)) : 0);
			if (num2 < num)
			{
				num = num2;
				entity = value;
				direction = direction2;
			}
		}
		if (entity != null && direction != directionFacing && !HasActionEventAt(base.X + item, base.Y + item2))
		{
			base.DirectionFacing = direction;
			PacketSender.SendDirection(direction);
		}
	}

	private static bool EstVoisine(int dx, int dy, bool diagonals)
	{
		if (Math.Abs(dx) <= 1 && Math.Abs(dy) <= 1 && (dx != 0 || dy != 0))
		{
			if (!diagonals && dx != 0)
			{
				return dy == 0;
			}
			return true;
		}
		return false;
	}

	private bool CaseVoisineOuOnLeVoit(Entity entity, bool diagonals, out int dx, out int dy)
	{
		dx = entity.X - base.X;
		dy = entity.Y - base.Y;
		if (EstVoisine(dx, dy, diagonals))
		{
			return true;
		}
		for (int i = -1; i <= 1; i++)
		{
			for (int j = -1; j <= 1; j++)
			{
				if (EstVoisine(j, i, diagonals) && entity.HoldsTile(base.X + j, base.Y + i))
				{
					dx = j;
					dy = i;
					return true;
				}
			}
		}
		return false;
	}

	private static Entity? CibleSurCase(Guid map, int x, int y, bool quiLaQuitte)
	{
		foreach (Entity value in Globals.Entities.Values)
		{
			if (value == null || value == Globals.Me || value.MapId != map || !value.CanBeAttacked || value.IsDying)
			{
				continue;
			}
			bool num;
			if (!quiLaQuitte)
			{
				if (value.X != x)
				{
					continue;
				}
				num = value.Y == y;
			}
			else
			{
				num = value.HoldsTile(x, y);
			}
			if (num)
			{
				return value;
			}
		}
		return null;
	}

	private bool EnnemiEnApproche()
	{
		foreach (Entity value in Globals.Entities.Values)
		{
			if (value == null || value == this || value.MapId != MapId || !value.CanBeAttacked || value.IsDying)
			{
				continue;
			}
			bool flag = value.Id == TargetId;
			bool flag2 = flag;
			if (!flag2)
			{
				bool flag3 = value.Type == EntityType.GlobalEntity && !(value is Critter);
				bool flag4 = flag3;
				if (flag4)
				{
					NpcAggression aggression = value.Aggression;
					bool flag5 = (uint)(aggression - 3) <= 1u;
					flag4 = !flag5;
				}
				flag2 = flag4;
			}
			if (flag2 && Math.Max(Math.Abs(value.X - base.X), Math.Abs(value.Y - base.Y)) <= 2)
			{
				return true;
			}
		}
		return false;
	}

	private bool HasActionEventAt(int x, int y)
	{
		foreach (MapInstance item in Intersect.Client.Maps.MapInstance.Lookup.Values.Cast<MapInstance>())
		{
			foreach (Entity value in item.LocalEntities.Values)
			{
				if (value is Event { Trigger: EventTrigger.ActionButton } obj && obj.MapId == MapId && obj.X == x && obj.Y == y)
				{
					return true;
				}
			}
		}
		return false;
	}

	private static (int X, int Y) StepOf(Direction direction)
	{
		return direction switch
		{
			Direction.Up => (X: 0, Y: -1), 
			Direction.Down => (X: 0, Y: 1), 
			Direction.Left => (X: -1, Y: 0), 
			Direction.Right => (X: 1, Y: 0), 
			Direction.UpLeft => (X: -1, Y: -1), 
			Direction.UpRight => (X: 1, Y: -1), 
			Direction.DownLeft => (X: -1, Y: 1), 
			Direction.DownRight => (X: 1, Y: 1), 
			_ => (X: 0, Y: 0), 
		};
	}

	private static Direction DirectionOfStep(int dx, int dy)
	{
		switch (dx)
		{
		case 0:
			switch (dy)
			{
			case -1:
				return Direction.Up;
			case 1:
				return Direction.Down;
			}
			break;
		case -1:
			switch (dy)
			{
			case 0:
				return Direction.Left;
			case -1:
				return Direction.UpLeft;
			case 1:
				return Direction.DownLeft;
			}
			break;
		case 1:
			switch (dy)
			{
			case 0:
				return Direction.Right;
			case -1:
				return Direction.UpRight;
			}
			break;
		}
		return Direction.DownRight;
	}

	private static int TurnSteps(Direction from, Direction to)
	{
		int num = Math.Abs(Octant(from) - Octant(to)) % 8;
		return Math.Min(num, 8 - num);
		static int Octant(Direction direction)
		{
			return direction switch
			{
				Direction.Up => 0, 
				Direction.UpRight => 1, 
				Direction.Right => 2, 
				Direction.DownRight => 3, 
				Direction.Down => 4, 
				Direction.DownLeft => 5, 
				Direction.Left => 6, 
				_ => 7, 
			};
		}
	}

	public bool TryGetRealLocation(ref int x, ref int y, ref Guid mapId)
	{
		if (Globals.MapGrid == null)
		{
			return false;
		}
		int num = x;
		int num2 = y;
		if (Intersect.Client.Maps.MapInstance.Get(mapId) != null)
		{
			int num3 = Intersect.Client.Maps.MapInstance.Get(mapId).GridX;
			int num4 = Intersect.Client.Maps.MapInstance.Get(mapId).GridY;
			if (x < 0)
			{
				num = MapWidth - x * -1;
				num3--;
			}
			if (y < 0)
			{
				num2 = MapHeight - y * -1;
				num4--;
			}
			if (y > MapHeight - 1)
			{
				num2 = y - MapHeight;
				num4++;
			}
			if (x > MapWidth - 1)
			{
				num = x - MapWidth;
				num3++;
			}
			if (num3 >= 0 && num3 < Globals.MapGridWidth && num4 >= 0 && num4 < Globals.MapGridHeight && Intersect.Client.Maps.MapInstance.Get(Globals.MapGrid[num3, num4]) != null)
			{
				x = (byte)num;
				y = (byte)num2;
				mapId = Globals.MapGrid[num3, num4];
				return true;
			}
		}
		return false;
	}

	private bool ParlerA(Guid evenement)
	{
		long milliseconds = Timing.Global.Milliseconds;
		if (milliseconds < _prochaineParole)
		{
			return true;
		}
		_prochaineParole = milliseconds + 500;
		_appuiRendu = true;
		DernierInterlocuteur = evenement;
		DernierInterlocuteurA = milliseconds;
		PacketSender.SendActivateEvent(evenement);
		return true;
	}

	private Guid EvenementSousLeCurseur()
	{
		Vector2 vector = Graphics.ConvertToWorldPoint(Globals.InputManager.GetMousePosition());
		FloatRect b = new FloatRect(vector.X - 8f, vector.Y - 8f, 16f, 16f);
		Guid result = Guid.Empty;
		Entity entity = null;
		float num = 0f;
		foreach (MapInstance item in Intersect.Client.Maps.MapInstance.Lookup.Values.Cast<MapInstance>())
		{
			foreach (KeyValuePair<Guid, Entity> localEntity in item.LocalEntities)
			{
				if (localEntity.Value is Event { Trigger: EventTrigger.ActionButton, DisablePreview: false } obj)
				{
					FloatRect floatRect = FloatRect.Intersect(obj.WorldPos, b);
					float num2 = floatRect.Width * floatRect.Height;
					if (num2 > num)
					{
						num = num2;
						entity = obj;
						result = localEntity.Key;
					}
				}
			}
		}
		if (entity == null || GetDistanceTo(entity) > 2)
		{
			return Guid.Empty;
		}
		return result;
	}

	public bool TryParlerAuCurseur()
	{
		if (base.IsBlocking || base.IsCharging || IsBeaming || base.IsTransforming || Intersect.Client.Interface.Interface.DoesMouseHitInterface())
		{
			return false;
		}
		Guid guid = EvenementSousLeCurseur();
		if (guid != Guid.Empty)
		{
			return ParlerA(guid);
		}
		return false;
	}

	public bool TryTarget()
	{
		for (int i = 0; i < base.Status.Count; i++)
		{
			if (base.Status[i].Type == SpellEffect.Taunt)
			{
				return false;
			}
		}
		Vector2 vector = Graphics.ConvertToWorldPoint(Globals.InputManager.GetMousePosition());
		int num = (int)vector.X;
		int num2 = (int)vector.Y;
		FloatRect b = new FloatRect(num - 8, num2 - 8, 16f, 16f);
		IEntity entity = null;
		float num3 = 0f;
		bool flag3;
		foreach (MapInstance item in Intersect.Client.Maps.MapInstance.Lookup.Values.Cast<MapInstance>())
		{
			if (num < item.X || num > item.X + MapWidth * TileWidth || num2 < item.Y || num2 > item.Y + MapHeight * TileHeight)
			{
				continue;
			}
			num -= item.X;
			num2 -= item.Y;
			num /= TileWidth;
			num2 /= TileHeight;
			Guid mapId = item.Id;
			if (!TryGetRealLocation(ref num, ref num2, ref mapId))
			{
				goto IL_037e;
			}
			foreach (KeyValuePair<Guid, Entity> entity2 in Globals.Entities)
			{
				bool flag = entity2.Value == null || entity2.Value.MapId != mapId;
				bool flag2 = flag;
				if (!flag2)
				{
					Entity value = entity2.Value;
					flag3 = ((value is Projectile || value is Resource) ? true : false);
					flag2 = flag3;
				}
				if (flag2)
				{
					continue;
				}
				if (entity2.Value.IsStealthed)
				{
					Player? me = Globals.Me;
					if (me != null && !me.IsInMyParty(entity2.Value.Id))
					{
						continue;
					}
				}
				FloatRect floatRect = FloatRect.Intersect(entity2.Value.WorldPos, b);
				if (floatRect.Width * floatRect.Height > num3)
				{
					num3 = floatRect.Width * floatRect.Height;
					entity = entity2.Value;
				}
			}
			foreach (MapInstance item2 in Intersect.Client.Maps.MapInstance.Lookup.Values.Cast<MapInstance>())
			{
				foreach (KeyValuePair<Guid, Entity> localEntity in item2.LocalEntities)
				{
					if (localEntity.Value != null && !(localEntity.Value.MapId != mapId) && !((Event)localEntity.Value).DisablePreview)
					{
						FloatRect floatRect2 = FloatRect.Intersect(localEntity.Value.WorldPos, b);
						if (floatRect2.Width * floatRect2.Height > num3)
						{
							num3 = floatRect2.Width * floatRect2.Height;
							entity = localEntity.Value;
						}
					}
				}
			}
			if (entity != null && entity.Id != TargetId)
			{
				Target = entity;
				if (entity is Player && Intersect.Client.Interface.Interface.GameUi.IsAdminWindowOpen)
				{
					Intersect.Client.Interface.Interface.GameUi.AdminWindowSelectName(entity.Name);
				}
				flag3 = true;
			}
			else if (Heritage.Actif && Heritage.Cible.SerieEnCours(this))
			{
				flag3 = false;
			}
			else
			{
				if (Globals.Database.StickyTarget)
				{
					goto IL_037e;
				}
				flag3 = ClearTarget();
			}
			goto IL_039f;
			IL_037e:
			flag3 = false;
			goto IL_039f;
		}
		return false;
		IL_039f:
		return flag3;
	}

	public bool TryTarget(IEntity entity, bool force = false)
	{
		for (int i = 0; i < base.Status.Count; i++)
		{
			if (base.Status[i].Type == SpellEffect.Taunt && !force)
			{
				return false;
			}
		}
		if (entity == null)
		{
			return false;
		}
		if (TargetBox != null && TargetBox.MyEntity == entity)
		{
			return true;
		}
		int num = ((entity is Event) ? 1 : 0);
		if (entity is Player && Intersect.Client.Interface.Interface.GameUi.IsAdminWindowOpen)
		{
			Intersect.Client.Interface.Interface.GameUi.AdminWindowSelectName(entity.Name);
		}
		if (TargetId != entity.Id)
		{
			Target = entity;
		}
		return true;
	}

	public bool ClearTarget()
	{
		_target = null;
		if (TargetId == default(Guid) && TargetType == -1)
		{
			return false;
		}
		if (TargetId != default(Guid))
		{
			PacketSender.SendTarget(default(Guid));
			SetTargetBox(null);
		}
		TargetId = default(Guid);
		TargetType = -1;
		return true;
	}

	public static bool TryPickupItem(Guid mapId, int tileIndex, Guid uniqueId = default(Guid), bool firstOnly = false)
	{
		MapInstance mapInstance = Intersect.Client.Maps.MapInstance.Get(mapId);
		if (mapInstance == null || tileIndex < 0 || tileIndex >= Options.Instance.Map.MapWidth * Options.Instance.Map.MapHeight)
		{
			return false;
		}
		if (uniqueId != Guid.Empty || firstOnly)
		{
			if (!mapInstance.MapItems.TryGetValue(tileIndex, out List<IMapItemInstance> value) || value.Count < 1)
			{
				return false;
			}
			foreach (IMapItemInstance item in value)
			{
				if (!(uniqueId != Guid.Empty) || !(item.Id != uniqueId))
				{
					PacketSender.SendPickupItem(mapId, tileIndex, item.Id);
					return true;
				}
			}
			return false;
		}
		PacketSender.SendPickupItem(mapId, tileIndex, uniqueId);
		return true;
	}

	public long GetNextLevelExperience()
	{
		return ExperienceToNextLevel;
	}

	public override int CalculateAttackTime()
	{
		ItemDescriptor itemDescriptor = null;
		int num = base.CalculateAttackTime();
		ClassDescriptor classDescriptor = DatabaseObject<ClassDescriptor>.Get(Class);
		if (classDescriptor != null && classDescriptor.AttackSpeedModifier == 1)
		{
			num = classDescriptor.AttackSpeedValue;
		}
		if (this == Globals.Me)
		{
			if (Options.Instance.Equipment.WeaponSlot > -1 && Options.Instance.Equipment.WeaponSlot < base.Equipment.Length && base.MyEquipment[Options.Instance.Equipment.WeaponSlot] >= 0)
			{
				itemDescriptor = DatabaseObject<ItemDescriptor>.Get(base.Inventory[base.MyEquipment[Options.Instance.Equipment.WeaponSlot]].ItemId);
			}
		}
		else if (Options.Instance.Equipment.WeaponSlot > -1 && Options.Instance.Equipment.WeaponSlot < base.Equipment.Length && base.Equipment[Options.Instance.Equipment.WeaponSlot] != Guid.Empty)
		{
			itemDescriptor = DatabaseObject<ItemDescriptor>.Get(base.Equipment[Options.Instance.Equipment.WeaponSlot]);
		}
		if (itemDescriptor != null)
		{
			if (itemDescriptor.AttackSpeedModifier == 1)
			{
				num = itemDescriptor.AttackSpeedValue;
			}
			else if (itemDescriptor.AttackSpeedModifier == 2)
			{
				num = (int)((float)num * (100f / (float)itemDescriptor.AttackSpeedValue));
			}
		}
		return num;
	}

	public virtual int CalculateAttackTime(int speed)
	{
		return (int)((float)Options.Instance.Combat.MaxAttackRate + (float)(Options.Instance.Combat.MinAttackRate - Options.Instance.Combat.MaxAttackRate) * (((float)Options.Instance.Player.MaxStat - (float)speed) / (float)Options.Instance.Player.MaxStat));
	}

	private void ProcessDirectionalInput(float avanceMs = 0f)
	{
		if (Globals.Me == null || Globals.MapGrid == null || Globals.InCraft)
		{
			return;
		}
		for (int i = 0; i < base.Status.Count; i++)
		{
			SpellEffect type = base.Status[i].Type;
			if (((uint)(type - 2) <= 1u || type == SpellEffect.Sleep) ? true : false)
			{
				return;
			}
		}
		if (base.Dashing != null || base.DashQueue.Count > 0 || base.DashTimer > Timing.Global.Milliseconds || base.IsCharging || IsBeaming || Clash.Retient(this) || base.IsGuardStunned || (Heritage.Actif && SeTournerEnGardeEnCases()) || base.IsTransforming || Flight.IsDown(this) || (base.IsAttacking && !Options.Instance.Player.AllowCombatMovement))
		{
			return;
		}
		Point position = new Point(base.X, base.Y);
		IEntity blockedBy = null;
		if (DirectionMoving <= Direction.None || Globals.EventDialogs.Count != 0)
		{
			return;
		}
		if (EstSurcharge)
		{
			DireSurcharge();
			return;
		}
		long num = Timing.Global.Ticks - (long)(Math.Clamp(avanceMs, 0f, 50f) * 10000f);
		long num2 = num / 10000;
		if (base.IsMoving || base.MoveTimer > num2 || (!Options.Instance.Combat.MovementCancelsCast && base.IsCasting))
		{
			return;
		}
		if (Options.Instance.Combat.MovementCancelsCast)
		{
			base.CastTime = 0L;
		}
		Direction directionFacing = base.DirectionFacing;
		Direction directionMoving = DirectionMoving;
		bool enableCrossingDiagonalBlocks = Options.Instance.Map.EnableCrossingDiagonalBlocks;
		Point? point = null;
		bool flag = base.FlightMode != FlightMode.None;
		if (directionMoving != Direction.None)
		{
			List<Direction> list = new List<Direction>(4) { directionMoving };
			if (directionFacing.IsAdjacent(directionMoving))
			{
				list.Add(directionFacing);
			}
			if (directionMoving.IsDiagonal())
			{
				list.AddRange(directionMoving.GetComponentDirections());
			}
			foreach (Direction item in list)
			{
				Point deltaPoint = item.GetDeltaPoint();
				Point point2 = position + deltaPoint;
				if (!(flag ? Flight.BlocksFlight(this, point2) : (IsTileBlocked(point2, base.Z, MapId, ref blockedBy) != -1)) && (flag || !LigneBarreMonPasEnCases(position, deltaPoint)) && (enableCrossingDiagonalBlocks || !item.IsDiagonal() || flag || !(from componentDirection in item.GetComponentDirections()
					select componentDirection.GetDeltaPoint()).All((Point componentDelta) => IsTileBlocked(componentDelta + position, base.Z, MapId, ref blockedBy) != -1 || LigneBarreMonPasEnCases(position, componentDelta))))
				{
					position.X += deltaPoint.X;
					position.Y += deltaPoint.Y;
					base.IsMoving = true;
					base.DirectionFacing = item;
					point = null;
					base.OffsetX -= deltaPoint.X * TileWidth;
					base.OffsetY -= deltaPoint.Y * TileHeight;
					ChoisirLeCoteDuPas(deltaPoint);
					break;
				}
			}
			if (!base.IsMoving && !flag && !directionMoving.IsDiagonal() && Options.Instance.Map.EnableDiagonalMovement)
			{
				IEntity blockedBy2 = null;
				Point deltaPoint2 = directionMoving.GetDeltaPoint();
				int num3 = IsTileBlocked(position + deltaPoint2, base.Z, MapId, ref blockedBy2);
				if ((uint)(num3 - -3) <= 1u)
				{
					List<(Direction, Point)> list2 = new List<(Direction, Point)>(2);
					Direction[] array = CornerDiagonals(directionMoving);
					foreach (Direction direction in array)
					{
						Point deltaPoint3 = direction.GetDeltaPoint();
						Point point3 = new Point(deltaPoint3.X - deltaPoint2.X, deltaPoint3.Y - deltaPoint2.Y);
						if (IsTileBlocked(position + deltaPoint3, base.Z, MapId, ref blockedBy2) == -1 && IsTileBlocked(position + point3, base.Z, MapId, ref blockedBy2) == -1 && !LigneBarreMonPasEnCases(position, deltaPoint3))
						{
							list2.Add((direction, deltaPoint3));
						}
					}
					if (list2.Count == 1)
					{
						var (directionFacing2, value) = list2[0];
						position.X += value.X;
						position.Y += value.Y;
						base.IsMoving = true;
						base.DirectionFacing = directionFacing2;
						point = value;
					}
				}
			}
			if (point.HasValue)
			{
				Point valueOrDefault = point.GetValueOrDefault();
				base.OffsetX -= valueOrDefault.X * TileWidth;
				base.OffsetY -= valueOrDefault.Y * TileHeight;
				ChoisirLeCoteDuPas(valueOrDefault);
			}
			if (!base.IsMoving && !flag && blockedBy == null && Timing.Global.Milliseconds - _lastWallBumpAt > 450)
			{
				IEntity blockedBy3 = null;
				int num3 = IsTileBlocked(position + directionMoving.GetDeltaPoint(), base.Z, MapId, ref blockedBy3);
				bool flag2 = (uint)(num3 - -3) <= 1u;
				if (flag2 || LigneBarreMonPasEnCases(position, directionMoving.GetDeltaPoint()))
				{
					_lastWallBumpAt = Timing.Global.Milliseconds;
					base.WallBumpAt = _lastWallBumpAt;
					base.WallBumpDirection = directionMoving;
					CombatAudio.Play("bump.wav", this, 0.1f);
				}
			}
		}
		if (blockedBy != mLastBumpedEvent)
		{
			mLastBumpedEvent = null;
		}
		if (base.IsMoving)
		{
			if (position.X < 0 || position.Y < 0 || position.X > MapWidth - 1 || position.Y > MapHeight - 1)
			{
				int num5 = Intersect.Client.Maps.MapInstance.Get(Globals.Me.MapId).GridX;
				int num6 = Intersect.Client.Maps.MapInstance.Get(Globals.Me.MapId).GridY;
				if (position.X < 0)
				{
					num5--;
					base.X = (byte)(MapWidth - 1);
				}
				else if (position.X >= MapWidth)
				{
					base.X = 0;
					num5++;
				}
				else
				{
					base.X = (byte)position.X;
				}
				if (position.Y < 0)
				{
					num6--;
					base.Y = (byte)(MapHeight - 1);
				}
				else if (position.Y >= MapHeight)
				{
					base.Y = 0;
					num6++;
				}
				else
				{
					base.Y = (byte)position.Y;
				}
				if (MapId != Globals.MapGrid[num5, num6])
				{
					MapId = Globals.MapGrid[num5, num6];
					FetchNewMaps();
				}
			}
			else
			{
				base.X = (byte)position.X;
				base.Y = (byte)position.Y;
			}
			if (!flag)
			{
				TryToChangeDimension();
			}
			PacketSender.SendMove(num);
			base.MoveTimer = num2 + (long)GetMovementTime();
		}
		else
		{
			if (DirectionMoving != base.DirectionFacing)
			{
				base.DirectionFacing = DirectionMoving;
				PacketSender.SendDirection(base.DirectionFacing);
				PickLastDirection(base.DirectionFacing);
			}
			if (blockedBy != null && mLastBumpedEvent != blockedBy && blockedBy is Event)
			{
				PacketSender.SendBumpEvent(blockedBy.MapId, blockedBy.Id);
				mLastBumpedEvent = (Entity)blockedBy;
			}
		}
	}

	private static Direction[] CornerDiagonals(Direction direction)
	{
		return direction switch
		{
			Direction.Up => new Direction[2]
			{
				Direction.UpLeft,
				Direction.UpRight
			}, 
			Direction.Down => new Direction[2]
			{
				Direction.DownLeft,
				Direction.DownRight
			}, 
			Direction.Left => new Direction[2]
			{
				Direction.UpLeft,
				Direction.DownLeft
			}, 
			Direction.Right => new Direction[2]
			{
				Direction.UpRight,
				Direction.DownRight
			}, 
			_ => Array.Empty<Direction>(), 
		};
	}

	public static void FetchNewMaps()
	{
		if (Globals.MapGridWidth != 0L && Globals.MapGridHeight != 0L)
		{
			PacketSender.SendNeedMapForGrid();
		}
	}

	public override void DrawEquipment(string filename, Color renderColor)
	{
		if (string.IsNullOrEmpty(TransformedSprite) || Transformation.KeepsPaperdolls(this))
		{
			base.DrawEquipment(filename, renderColor);
		}
	}

	public override void DrawName(Color? textColor, Color? borderColor, Color? backgroundColor)
	{
		if (textColor == null)
		{
			LabelColor labelColor = KarmaDisplay.NameColors(KarmaTier);
			textColor = labelColor.Name;
			borderColor = labelColor.Outline;
			backgroundColor = labelColor.Background;
		}
		Color nameColor = base.NameColor;
		if (nameColor != null && nameColor.A != 0)
		{
			textColor = nameColor;
		}
		Color nameColor2 = textColor ?? Intersect.Color.White;
		if (Globals.Me != null && Globals.Me.Id != base.Id)
		{
			LabelColor value;
			if (Globals.Me.IsInMyParty(this))
			{
				textColor = PartyMarkers.NameColor;
				borderColor = Intersect.Color.Black;
			}
			else if (Globals.Me.IsInGuild && Guild == Globals.Me.Guild && CustomColors.Names.Players.TryGetValue("Guild", out value))
			{
				borderColor = value.Name;
			}
		}
		if (!ShouldDrawName || !base.IsHovered || base.IsDying || !NamePlate.Draw(this, nameColor2))
		{
			DrawNameAndLabels(textColor, borderColor, backgroundColor);
		}
	}

	private void UpdateAggressorLabel()
	{
		bool isAggressor = IsAggressor;
		if (isAggressor != _showsAggressorLabel)
		{
			if (isAggressor)
			{
				_serverHeaderLabel = base.HeaderLabel;
				base.HeaderLabel = new Label(Strings.Karma.Aggressor, KarmaDisplay.AggressorColor);
			}
			else
			{
				base.HeaderLabel = _serverHeaderLabel ?? new Label(string.Empty, Intersect.Color.White);
			}
			_showsAggressorLabel = isAggressor;
		}
	}

	public override bool IsAllyOf(Player en)
	{
		if (base.IsAllyOf(en))
		{
			return true;
		}
		if (!IsInMyParty(en) && !IsInMyGuild(en))
		{
			IMapInstance? mapInstance = en.MapInstance;
			if (mapInstance == null)
			{
				return false;
			}
			return mapInstance.ZoneType == MapZone.Safe;
		}
		return true;
	}

	private void DrawNameAndLabels(Color textColor, Color? borderColor, Color? backgroundColor)
	{
		base.DrawName(textColor, borderColor, backgroundColor);
		DrawLabels(base.HeaderLabel.Text, 0, base.HeaderLabel.Color, textColor, borderColor, backgroundColor);
		DrawLabels(base.FooterLabel.Text, 1, base.FooterLabel.Color, textColor, borderColor, backgroundColor);
		DrawGuildName(textColor, borderColor, backgroundColor);
	}

	public virtual void DrawGuildName(Color textColor, Color? borderColor = null, Color? backgroundColor = null)
	{
		if (Graphics.Renderer == null || Globals.Me == null)
		{
			return;
		}
		string text = Guild?.Trim();
		if (ShouldDrawName && !string.IsNullOrWhiteSpace(text) && Options.Instance.Guild.ShowGuildNameTagsOverMembers && (!IsStealthed || IsInMyParty(Globals.Me)) && base.MapInstance != null)
		{
			Vector2 vector = TexteMonde.Mesurer(text, Graphics.EntityNameFont, TexteMonde.Taille(Graphics.EntityNameFontSize));
			int num = (int)Math.Ceiling(base.Origin.X);
			float labelLocation = GetLabelLocation(LabelType.Guild);
			if ((object)backgroundColor == null)
			{
				backgroundColor = Intersect.Color.Transparent;
			}
			if (backgroundColor != Intersect.Color.Transparent)
			{
				Graphics.DrawGameTexture(Graphics.Renderer.WhitePixel, new FloatRect(0f, 0f, 1f, 1f), new FloatRect((float)num - vector.X / 2f - 4f, labelLocation, vector.X + 8f, vector.Y), backgroundColor);
			}
			float y = vector.Y;
			ClanEmbleme.DessinerMonde((float)num - vector.X / 2f - y - 3f, labelLocation, y, GuildEmblem, GuildColor);
			if ((object)borderColor == null)
			{
				borderColor = Intersect.Color.Transparent;
			}
			TexteMonde.Ecrire(text, Graphics.EntityNameFont, TexteMonde.Taille(Graphics.EntityNameFontSize), (float)num - vector.X / 2f, labelLocation, Intersect.Color.FromArgb(textColor.ToArgb()), Intersect.Color.FromArgb(borderColor.ToArgb()));
		}
	}

	public void DrawTargets()
	{
		foreach (KeyValuePair<Guid, Entity> entity in Globals.Entities)
		{
			if (entity.Value == null || entity.Value.IsHidden)
			{
				continue;
			}
			if (entity.Value.IsStealthed)
			{
				if (!(entity.Value is Player player))
				{
					continue;
				}
				Player? me = Globals.Me;
				if (me != null && !me.IsInMyParty(player))
				{
					continue;
				}
			}
			Entity value = entity.Value;
			bool flag = ((value is Projectile || value is Resource) ? true : false);
			if (!flag && TargetType == 0 && !(TargetId != entity.Value.Id))
			{
				entity.Value.DrawTarget(1);
				AutoTurnToTarget(entity.Value);
				ToggleTargetContextMenu(entity.Value);
			}
		}
		foreach (MapInstance item in Intersect.Client.Maps.MapInstance.Lookup.Values.Cast<MapInstance>())
		{
			foreach (KeyValuePair<Guid, Entity> localEntity in item.LocalEntities)
			{
				if (localEntity.Value == null || localEntity.Value.MapId != item.Id || localEntity.Value is Event { DisablePreview: not false } || localEntity.Value.IsHidden)
				{
					continue;
				}
				if (localEntity.Value.IsStealthed)
				{
					if (!(localEntity.Value is Player player2))
					{
						continue;
					}
					Player? me2 = Globals.Me;
					if (me2 != null && !me2.IsInMyParty(player2))
					{
						continue;
					}
				}
				if (TargetType == 1 && !(TargetId != localEntity.Value.Id))
				{
					localEntity.Value.DrawTarget(1);
					AutoTurnToTarget(localEntity.Value);
					ToggleTargetContextMenu(localEntity.Value);
				}
			}
		}
		if (Intersect.Client.Interface.Interface.DoesMouseHitInterface())
		{
			return;
		}
		Vector2 vector = Graphics.ConvertToWorldPoint(Globals.InputManager.GetMousePosition());
		foreach (MapInstance item2 in Intersect.Client.Maps.MapInstance.Lookup.Values.Cast<MapInstance>())
		{
			if (!(vector.X >= (float)item2.X) || !(vector.X <= (float)(item2.X + MapWidth * TileWidth)) || !(vector.Y >= (float)item2.Y) || !(vector.Y <= (float)(item2.Y + MapHeight * TileHeight)))
			{
				continue;
			}
			Guid id = item2.Id;
			foreach (KeyValuePair<Guid, Entity> entity2 in Globals.Entities)
			{
				if (entity2.Value == null || !(entity2.Value.MapId == id) || entity2.Value.HideName)
				{
					continue;
				}
				if (entity2.Value.IsStealthed)
				{
					if (!(entity2.Value is Player player3))
					{
						continue;
					}
					Player? me3 = Globals.Me;
					if (me3 == null || !me3.IsInMyParty(player3))
					{
						continue;
					}
				}
				if (entity2.Value.WorldPos.Contains(vector.X, vector.Y))
				{
					Entity value = entity2.Value;
					bool flag = ((value is Projectile || value is Resource) ? true : false);
					if (!flag && (TargetType != 0 || TargetId != entity2.Value.Id))
					{
						entity2.Value.DrawTarget(0);
						ToggleTargetContextMenu(entity2.Value);
					}
				}
			}
			{
				foreach (MapInstance item3 in Intersect.Client.Maps.MapInstance.Lookup.Values.Cast<MapInstance>())
				{
					foreach (KeyValuePair<Guid, Entity> localEntity2 in item3.LocalEntities)
					{
						if (localEntity2.Value == null || !(localEntity2.Value.MapId == id) || ((Event)localEntity2.Value).DisablePreview || localEntity2.Value.IsHidden)
						{
							continue;
						}
						if (localEntity2.Value.IsStealthed)
						{
							if (!(localEntity2.Value is Player player4))
							{
								continue;
							}
							Player? me4 = Globals.Me;
							if (me4 == null || !me4.IsInMyParty(player4))
							{
								continue;
							}
						}
						if (localEntity2.Value.WorldPos.Contains(vector.X, vector.Y) && (TargetType != 1 || TargetId != localEntity2.Value.Id))
						{
							localEntity2.Value.DrawTarget(0);
							ToggleTargetContextMenu(localEntity2.Value);
						}
					}
				}
				break;
			}
		}
	}

	private bool TryTurnAround(Direction inputDirection)
	{
		if (inputDirection == Direction.None)
		{
			return false;
		}
		if (!base.CanTurnAround || IsBeaming)
		{
			return false;
		}
		if (!Controls.IsControlPressed(Control.TurnAround))
		{
			return false;
		}
		Direction directionFacing = base.DirectionFacing;
		if (inputDirection == directionFacing)
		{
			return true;
		}
		PacketSender.SendDirection(inputDirection);
		PickLastDirection(inputDirection);
		return true;
	}

	private static bool IsTargetAtOppositeDirection(Direction currentDir, Direction targetDir)
	{
		if (Options.Instance.Map.EnableDiagonalMovement)
		{
			switch (currentDir)
			{
			case Direction.Up:
				if (targetDir == Direction.Down || (uint)(targetDir - 6) <= 1u)
				{
					return true;
				}
				return false;
			case Direction.Down:
				if (targetDir == Direction.Up || (uint)(targetDir - 4) <= 1u)
				{
					return true;
				}
				return false;
			case Direction.Left:
				if (targetDir == Direction.Right || (uint)(targetDir - 5) <= 1u)
				{
					return true;
				}
				return false;
			case Direction.Right:
				if (targetDir == Direction.Left || targetDir == Direction.UpLeft || targetDir == Direction.DownLeft)
				{
					return true;
				}
				return false;
			}
			if (!Options.Instance.Map.EnableDiagonalMovement)
			{
				return false;
			}
		}
		switch (currentDir)
		{
		case Direction.UpLeft:
			return (targetDir == Direction.Down || targetDir == Direction.Right || targetDir == Direction.DownRight) ? true : false;
		case Direction.UpRight:
			return ((uint)(targetDir - 1) <= 1u || targetDir == Direction.DownLeft) ? true : false;
		case Direction.DownLeft:
			return (targetDir == Direction.Up || targetDir == Direction.Right || targetDir == Direction.UpRight) ? true : false;
		case Direction.DownRight:
		{
			bool result;
			switch (targetDir)
			{
			case Direction.Up:
			case Direction.Left:
			case Direction.UpLeft:
				result = true;
				break;
			default:
				result = false;
				break;
			}
			return result;
		}
		default:
			return false;
		}
	}

	public bool ChargesDuDash(long maintenant, out int charges, out int maximum, out float recharge)
	{
		CombatOptions combat = Options.Instance.Combat;
		maximum = combat.ChargesDeDashPourNiveau(base.Level);
		charges = maximum;
		recharge = 0f;
		if (combat.DashCharges <= 0)
		{
			return false;
		}
		if (_chargesDashMaximumConnu >= 0 && maximum > _chargesDashMaximumConnu)
		{
			if (_chargesDash >= 0)
			{
				_chargesDash += maximum - _chargesDashMaximumConnu;
			}
			NouvelleChargeDeDashA = maintenant;
			string text = ((_chargesDashMaximumConnu == 0) ? Strings.Vanish.DashDebloque.ToString() : Strings.Vanish.NouvelleChargeDash.ToString(maximum));
			CombatFeedback.Callout(base.Id, text, OrangeDash, 13);
		}
		_chargesDashMaximumConnu = maximum;
		if (maximum <= 0)
		{
			return false;
		}
		if (_chargesDash < 0)
		{
			return true;
		}
		int num = Math.Max(100, RechargeDuDashPassif(combat.DashChargeRechargeMs));
		charges = Math.Min(_chargesDash, maximum);
		while (charges < maximum && _chargeDashProchaineA != 0L && maintenant >= _chargeDashProchaineA)
		{
			charges++;
			_chargeDashProchaineA = ((charges < maximum) ? (_chargeDashProchaineA + num) : 0);
		}
		_chargesDash = ((charges >= maximum) ? (-1) : charges);
		if (charges < maximum && _chargeDashProchaineA != 0L)
		{
			recharge = Math.Clamp(1f - (float)(_chargeDashProchaineA - maintenant) / (float)num, 0f, 1f);
		}
		return true;
	}

	private bool UneChargeDeDashPrete(long maintenant)
	{
		CombatOptions combat = Options.Instance.Combat;
		if (combat.DashCharges <= 0)
		{
			return true;
		}
		if (!ChargesDuDash(maintenant, out var charges, out var maximum, out var _))
		{
			if (maximum <= 0 && maintenant - _dashAuNiveauDitA > 2000)
			{
				_dashAuNiveauDitA = maintenant;
				CombatFeedback.Callout(base.Id, Strings.Vanish.DashAuNiveau.ToString(combat.NiveauDuDash), OrangeDash, 12);
			}
			return false;
		}
		if (charges <= 0)
		{
			DashRefuseA = maintenant;
			return false;
		}
		return true;
	}

	private void PrendreUneChargeDeDash(long maintenant)
	{
		CombatOptions combat = Options.Instance.Combat;
		if (combat.DashCharges > 0 && ChargesDuDash(maintenant, out var charges, out var _, out var _) && charges > 0)
		{
			if (_chargesDash < 0 || _chargeDashProchaineA == 0L)
			{
				_chargeDashProchaineA = maintenant + Math.Max(100, RechargeDuDashPassif(combat.DashChargeRechargeMs));
			}
			_chargesDash = charges - 1;
		}
	}

	private void MajDash()
	{
		if (this == Globals.Me)
		{
			bool flag = Controls.IsGameControlPressed(Control.Dash);
			bool flag2 = flag && !_dashTenuAvant;
			_dashTenuAvant = flag;
			if (!flag)
			{
				_dashAppuiDepuis = -1L;
			}
			else if (flag2)
			{
				_dashAppuiDepuis = Timing.Global.Milliseconds;
				TenterLeDash();
			}
		}
	}

	private bool DashTenuPourCourir()
	{
		int dashCourseApresMs = Options.Instance.Combat.DashCourseApresMs;
		if (dashCourseApresMs > 0 && _dashAppuiDepuis >= 0 && Timing.Global.Milliseconds - _dashAppuiDepuis >= dashCourseApresMs)
		{
			return Controls.IsGameControlPressed(Control.Dash);
		}
		return false;
	}

	private void TenterLeDash()
	{
		CombatOptions combat = Options.Instance.Combat;
		long milliseconds = Timing.Global.Milliseconds;
		if (combat.DashPixels <= 0 || milliseconds < _dashPretLocal || !base.EstPiloteLibrement || IsBusy || base.IsDying || base.IsBlocking || base.IsCharging || IsBeaming || base.IsCasting || base.IsTransforming || milliseconds < base.GuardStunnedUntil || base.Dashing != null || base.DashQueue.Count > 0 || base.FlightMode != FlightMode.None || Vanish.EstFige(this) || !PositionMonde(out var mondeX, out var mondeY))
		{
			return;
		}
		if (EstSurcharge)
		{
			DireSurcharge();
		}
		else if (UneChargeDeDashPrete(milliseconds))
		{
			(float X, float Y) tuple = DirectionVoulueLibre();
			float item = tuple.X;
			float item2 = tuple.Y;
			Entity entity = CibleDuDash(mondeX, mondeY, item, item2, milliseconds);
			PoserSaPositionAvantDeSeFiger();
			Echanges.AbandonnerLaCharge(this);
			AnnulerLaRecolteAuClic();
			if (Heritage.Actif && _appuiEnAttente)
			{
				CoupAvantLeDashHeritage();
			}
			PacketSender.SendDashRequest(item, item2, entity?.Id ?? Guid.Empty);
			DashAnime.Appui(this, item, item2);
			_dashPretLocal = milliseconds + Math.Max(0, (combat.DashCharges > 0) ? combat.DashEcartMs : RechargeDuDashPassif(combat.DashRechargeMs));
			PrendreUneChargeDeDash(milliseconds);
			if (entity != null)
			{
				_rueeEnCoursJusqua = milliseconds + 300;
			}
		}
	}

	private Entity? CibleDuDash(float moiX, float moiY, float versX, float versY, long maintenant)
	{
		CombatOptions combat = Options.Instance.Combat;
		if (base.Level < combat.RueeNiveau)
		{
			return null;
		}
		float num = (float)(Math.Max(1, combat.RueeCases) * TileWidth) + 16f;
		Entity entity = Echanges.ProjeteeRecemment(maintenant, combat.RueeApresProjectionMs);
		if (entity != null && entity.PositionMonde(out var mondeX, out var mondeY))
		{
			float num2 = MathF.Sqrt((mondeX - moiX) * (mondeX - moiX) + (mondeY - moiY) * (mondeY - moiY));
			if (num2 >= 40f && num2 <= num)
			{
				return entity;
			}
		}
		if (versX == 0f && versY == 0f)
		{
			return null;
		}
		Entity result = null;
		float num3 = float.MaxValue;
		foreach (Entity value in Globals.Entities.Values)
		{
			if (value == null || value == this || !value.CanBeAttacked || value.IsDying || value.Z != base.Z || !value.PositionMonde(out var mondeX2, out var mondeY2))
			{
				continue;
			}
			bool flag = value.Id == TargetId;
			bool flag2 = value.Type == EntityType.GlobalEntity && !(value is Critter);
			bool flag3 = flag2;
			if (flag3)
			{
				NpcAggression aggression = value.Aggression;
				bool flag4 = (uint)(aggression - 3) <= 1u;
				flag3 = !flag4;
			}
			bool flag5 = flag3;
			if (!flag && !flag5)
			{
				continue;
			}
			float num4 = mondeX2 - moiX;
			float num5 = mondeY2 - moiY;
			float num6 = MathF.Sqrt(num4 * num4 + num5 * num5);
			if (!(num6 < 40f) && !(num6 > num) && !((versX * num4 + versY * num5) / num6 < 0.5f))
			{
				float num7 = (flag ? 0f : 1000f) + num6;
				if (num7 < num3)
				{
					num3 = num7;
					result = value;
				}
			}
		}
		return result;
	}

	private void AvancerLibrement(bool occupe)
	{
		double maintenantPrecis = DeplacementLibre.MaintenantPrecis;
		float num = ((_libreDerniereImage == 0.0) ? 0f : ((float)Math.Clamp(maintenantPrecis - _libreDerniereImage, 0.0, 100.0)));
		_libreDerniereImage = maintenantPrecis;
		long maintenantLocal = DeplacementLibre.MaintenantLocal;
		float libreAvanceReportee = _libreAvanceReportee;
		_libreAvanceReportee = 0f;
		if (!base.EstPiloteLibrement)
		{
			if (base.IsMoving && (base.OffsetX != 0f || base.OffsetY != 0f))
			{
				return;
			}
			base.IsMoving = false;
			base.EstPiloteLibrement = true;
			base.FinishedStepOvershoot = null;
		}
		if (base.Dashing != null || base.DashQueue.Count > 0 || !PositionMonde(out var x, out var y))
		{
			return;
		}
		bool flag = base.FlightMode != FlightMode.None;
		bool flag2 = EstEntraveLibrement();
		if (flag2 && EstSurcharge)
		{
			var (num2, num3) = DirectionVoulueLibre();
			if (num2 != 0f || num3 != 0f)
			{
				DireSurcharge();
			}
		}
		flag2 |= Heritage.Actif && SeTournerEnGarde(occupe || flag2);
		float num4 = 0f;
		_libreCasesEnvahies.Clear();
		if (!flag && !flag2)
		{
			num4 = SortirDesCorpsEnDouceur(ref x, ref y, num);
		}
		ReleverLesMonstresAuPixel(x, y, flag);
		if (!flag2)
		{
			SortirDesMonstresEnDouceur(ref x, ref y, num);
		}
		float num5;
		float num6;
		if (!(occupe || flag2))
		{
			(num5, num6) = DirectionVoulueLibre();
		}
		else
		{
			num5 = 0f;
			num6 = 0f;
		}
		if (Heritage.Actif && Heritage.MarcheFigee(this))
		{
			num5 = 0f;
			num6 = 0f;
		}
		float num7 = x;
		float num8 = y;
		float num9 = 0f;
		if (num5 != 0f || num6 != 0f)
		{
			if (Options.Instance.Combat.MovementCancelsCast)
			{
				base.CastTime = 0L;
			}
			float num10 = CorpsLibre.Vitesse(TempsDePasLibre(), TileWidth);
			if (Echanges.ChargeLocale(this))
			{
				num10 *= 0.35f;
			}
			float num11 = num5 * (num10 * num / 1000f + libreAvanceReportee);
			float num12 = num6 * (num10 * num / 1000f + libreAvanceReportee);
			num9 = MathF.Sqrt(num11 * num11 + num12 * num12);
			_libreCasesDeLImage.Clear();
			(float X, float Y) tuple3 = CorpsLibre.Deplacer(x, y + 6f, num11, num12, TileWidth, TileHeight, flag ? new CorpsLibre.EstBloquee(BloqueEnVol) : new CorpsLibre.EstBloquee(BloquePourMoi), 10f, 32f, flag ? null : new CorpsLibre.ProfondeurEau(ProfondeurDeLEau), flag ? null : LignesDuMonde);
			num7 = tuple3.X;
			num8 = tuple3.Y;
			num8 -= 6f;
			if (!flag && _libreCasesEnvahies.Count > 0)
			{
				(num7, num8) = NePasSEnfoncer(x, y, num7, num8, num4);
			}
			(float X, float Y) tuple5 = ContournerLesMonstresAuPixel(x, y, num7, num8);
			num7 = tuple5.X;
			num8 = tuple5.Y;
			Direction direction = DirectionDuVecteur(num5, num6);
			if (Heritage.Actif && Heritage.RegardTenu(this))
			{
				direction = Direction.None;
			}
			if (direction != Direction.None)
			{
				base.DirectionFacing = direction;
				PickLastDirection(direction);
			}
		}
		if (Heritage.Actif)
		{
			(num7, num8) = AvancerDElanHeritage(num7, num8, num, occupe || flag2 || flag, num4);
		}
		Guid mapId = MapId;
		byte x2 = base.X;
		byte y2 = base.Y;
		PoserAuPointDuMonde(num7, num8, 6f, out var carteChangee);
		if (carteChangee)
		{
			FetchNewMaps();
		}
		float num13 = MathF.Sqrt((num7 - x) * (num7 - x) + (num8 - y) * (num8 - y));
		AvancerLaMarche(num13);
		base.IsMoving = ((!(num < 0.5f)) ? (num13 > 0.01f) : (base.IsMoving && (num5 != 0f || num6 != 0f)));
		if (!flag && (MapId != mapId || base.X != x2 || base.Y != y2))
		{
			TryToChangeDimension();
		}
		bool flag3 = !flag && num9 > 0f && num13 < num9 * 0.3f;
		if (Heritage.Actif && flag3 && ArreteParUnCorpsHeritage(num7, num8, num5, num6))
		{
			_lastWallBumpAt = Timing.Global.Milliseconds;
		}
		if (flag3 && Timing.Global.Milliseconds - _lastWallBumpAt > 450)
		{
			_lastWallBumpAt = Timing.Global.Milliseconds;
			base.WallBumpAt = _lastWallBumpAt;
			base.WallBumpDirection = base.DirectionFacing;
			CombatAudio.Play("bump.wav", this, 0.1f);
		}
		if (flag3)
		{
			Entity entity = EvenementDevantMoi();
			if (entity != null && entity != mLastBumpedEvent)
			{
				PacketSender.SendBumpEvent(entity.MapId, entity.Id);
				mLastBumpedEvent = entity;
			}
		}
		else if (num9 > 0f)
		{
			mLastBumpedEvent = null;
		}
		if (!DeplacementLibre.EstLibre(MapId))
		{
			QuitterLeDeplacementLibre();
		}
		else
		{
			EnvoyerMouvementLibre(maintenantLocal, forcer: false);
		}
	}

	public override void HoldForHitStop(long milliseconds)
	{
		base.HoldForHitStop(milliseconds);
		if (milliseconds > 0 && _libreDerniereImage != 0.0)
		{
			_libreDerniereImage = Math.Min(_libreDerniereImage + (double)milliseconds, DeplacementLibre.MaintenantPrecis);
		}
	}

	internal void PoserSaPositionAvantDeSeFiger()
	{
		if (this == Globals.Me && base.IsMoving && base.EstPiloteLibrement && EnDeplacementLibre)
		{
			base.IsMoving = false;
			EnvoyerMouvementLibre(DeplacementLibre.MaintenantLocal, forcer: true);
		}
	}

	private void QuitterLeDeplacementLibre()
	{
		base.IsMoving = false;
		EnvoyerMouvementLibre(DeplacementLibre.MaintenantLocal, forcer: true);
		OublierLeDeplacementLibre();
		_libreDerniereImage = 0.0;
		_libreAvanceReportee = 0f;
	}

	private void ChoisirLeCoteDuPas(Point delta)
	{
		ResorptionX = ((delta.X == 0 && delta.Y != 0) ? 0.5f : 1f);
		ResorptionY = ((delta.Y == 0 && delta.X != 0) ? 0.5f : 1f);
	}

	private void CaleSurSaCase()
	{
		if (!base.IsMoving && !base.EstPiloteLibrement && !base.SeRecentre && !EnDeplacementLibre && base.Dashing == null && base.DashQueue.Count <= 0 && (base.OffsetX != 0f || base.OffsetY != 0f))
		{
			RepasserEnCasesEnDouceur();
		}
	}

	private void ReporterSurLeDeplacementLibre(float distance)
	{
		_libreAvanceReportee = Math.Clamp(distance, 0f, TileWidth);
	}

	public void AppliquerCorrectionLibre(CorrectionLibrePacket correction)
	{
		_libreEpoque = correction.Epoque;
		if (MapId != correction.MapId)
		{
			MapId = correction.MapId;
			FetchNewMaps();
		}
		PoserSurLaCarte(correction.X, correction.Y);
		base.DirectionFacing = correction.Direction;
		base.IsMoving = false;
		_libreEnvoiCarte = MapId;
		_libreEnvoiX = correction.X;
		_libreEnvoiY = correction.Y;
		_libreEnvoiDirection = base.DirectionFacing;
		_libreBougeait = false;
		_libreProchainEnvoi = 0L;
	}

	private void EnvoyerMouvementLibre(long maintenant, bool forcer)
	{
		float num = (float)(base.X * TileWidth) + (float)TileWidth / 2f + base.OffsetX;
		float num2 = (float)(base.Y * TileHeight) + (float)TileHeight / 2f + base.OffsetY;
		bool flag = MapId != _libreEnvoiCarte || MathF.Abs(num - _libreEnvoiX) > 0.25f || MathF.Abs(num2 - _libreEnvoiY) > 0.25f || base.DirectionFacing != _libreEnvoiDirection;
		bool flag2 = !base.IsMoving && _libreBougeait;
		bool flag3 = MapId != _libreEnvoiCarte;
		if (forcer || flag2 || flag3 || (flag && maintenant >= _libreProchainEnvoi))
		{
			_libreSequence++;
			Intersect.Client.Networking.Network.SendPacket(new MouvementLibrePacket
			{
				Epoque = _libreEpoque,
				Sequence = _libreSequence,
				MapId = MapId,
				X = num,
				Y = num2,
				Direction = ((base.DirectionFacing <= Direction.None) ? Direction.Down : base.DirectionFacing),
				EnMouvement = base.IsMoving
			}, (base.IsMoving && !flag3) ? TransmissionMode.Any : TransmissionMode.All);
			_libreEnvoiCarte = MapId;
			_libreEnvoiX = num;
			_libreEnvoiY = num2;
			_libreEnvoiDirection = base.DirectionFacing;
			_libreBougeait = base.IsMoving;
			_libreProchainEnvoi = maintenant + 50;
		}
	}

	private bool EstEntraveLibrement()
	{
		if (Globals.InCraft || Globals.EventDialogs.Count != 0)
		{
			return true;
		}
		for (int i = 0; i < base.Status.Count; i++)
		{
			SpellEffect type = base.Status[i].Type;
			if (((uint)(type - 2) <= 1u || type == SpellEffect.Sleep) ? true : false)
			{
				return true;
			}
		}
		if (base.DashTimer > Timing.Global.Milliseconds || base.IsCharging || IsBeaming || base.IsGuardStunned || base.IsTransforming || Flight.IsDown(this) || Flight.IsGliding(this) || Echanges.EstSonne(this) || Vanish.EstFige(this) || Clash.Retient(this) || Mort.Gisant(this))
		{
			return true;
		}
		if (base.IsAttacking && !Options.Instance.Player.AllowCombatMovement)
		{
			return true;
		}
		if (Options.Instance.Combat.MovementCancelsCast || !base.IsCasting)
		{
			return EstSurcharge;
		}
		return true;
	}

	private (float X, float Y) DirectionVoulueLibre()
	{
		float stickGaucheX = Manette.StickGaucheX;
		float num = 0f - Manette.StickGaucheY;
		float num2 = MathF.Sqrt(stickGaucheX * stickGaucheX + num * num);
		if (num2 > 0.25f)
		{
			return (X: stickGaucheX / num2, Y: num / num2);
		}
		if (DirectionMoving <= Direction.None)
		{
			return DirectionDeRecolteAuClic();
		}
		Point deltaPoint = DirectionMoving.GetDeltaPoint();
		float num3 = MathF.Sqrt(deltaPoint.X * deltaPoint.X + deltaPoint.Y * deltaPoint.Y);
		if (!(num3 > 0f))
		{
			return (X: 0f, Y: 0f);
		}
		return (X: (float)deltaPoint.X / num3, Y: (float)deltaPoint.Y / num3);
	}

	private static Direction DirectionDuVecteur(float x, float y)
	{
		if (MathF.Abs(x) < 0.001f && MathF.Abs(y) < 0.001f)
		{
			return Direction.None;
		}
		float num = MathF.Atan2(y, x) * 180f / (float)Math.PI;
		if (!Options.Instance.Map.EnableDiagonalMovement)
		{
			if (!(MathF.Abs(x) >= MathF.Abs(y)))
			{
				if (!(y > 0f))
				{
					return Direction.Up;
				}
				return Direction.Down;
			}
			if (!(x > 0f))
			{
				return Direction.Left;
			}
			return Direction.Right;
		}
		if (num >= -22.5f)
		{
			if (num < 22.5f)
			{
				return Direction.Right;
			}
			if (num < 67.5f)
			{
				return Direction.DownRight;
			}
			if (num < 112.5f)
			{
				return Direction.Down;
			}
			if (num < 157.5f)
			{
				return Direction.DownLeft;
			}
		}
		else
		{
			if (num >= -67.5f)
			{
				return Direction.UpRight;
			}
			if (num >= -112.5f)
			{
				return Direction.Up;
			}
			if (num >= -157.5f)
			{
				return Direction.UpLeft;
			}
		}
		return Direction.Left;
	}

	private float TempsDePasLibre()
	{
		float num = GetMovementTime();
		if (base.DirectionFacing > Direction.Right && Options.Instance.Map.DiagonalMovementCost > 0f)
		{
			num /= Options.Instance.Map.DiagonalMovementCost;
		}
		return num;
	}

	private float ProfondeurDeLEau(float mondeX, float mondeY)
	{
		int grilleX;
		int grilleY;
		Guid guid = DeplacementLibre.CarteAuPoint(mondeX, mondeY, out grilleX, out grilleY);
		if (guid == Guid.Empty || !Intersect.Client.Maps.MapInstance.TryGet(guid, out MapInstance instance))
		{
			return 0f;
		}
		float x = (float)instance.X + mondeX - (float)(grilleX * DeplacementLibre.LargeurCarte);
		float y = (float)instance.Y + mondeY - (float)(grilleY * DeplacementLibre.HauteurCarte);
		return Meteo.ProfondeurEauAuPoint(x, y);
	}

	private bool BloqueEnVol(int caseMondeX, int caseMondeY)
	{
		int x;
		int y;
		Guid guid = CarteDeLaCase(caseMondeX, caseMondeY, out x, out y);
		if (!(guid == Guid.Empty) && Intersect.Client.Maps.MapInstance.TryGet(guid, out MapInstance instance))
		{
			return instance.VolInterdit;
		}
		return true;
	}

	private bool BloquePourMoi(int caseMondeX, int caseMondeY)
	{
		if (_libreCasesEnvahies.Count > 0 && _libreCasesEnvahies.Contains((caseMondeX, caseMondeY)))
		{
			return false;
		}
		if (_libreCasesDeLImage.TryGetValue((caseMondeX, caseMondeY), out var value))
		{
			return value;
		}
		bool flag = MurPourMoi(caseMondeX, caseMondeY) || CorpsPourMoi(caseMondeX, caseMondeY);
		_libreCasesDeLImage[(caseMondeX, caseMondeY)] = flag;
		return flag;
	}

	private bool MurPourMoi(int caseMondeX, int caseMondeY)
	{
		int x;
		int y;
		Guid guid = CarteDeLaCase(caseMondeX, caseMondeY, out x, out y);
		if (guid == Guid.Empty || !Intersect.Client.Maps.MapInstance.TryGet(guid, out MapInstance instance) || instance.Attributes == null)
		{
			return true;
		}
		MapAttribute mapAttribute = instance.Attributes[x, y];
		if (!(mapAttribute is MapBlockedAttribute))
		{
			if (mapAttribute is MapAnimationAttribute mapAnimationAttribute)
			{
				if (mapAnimationAttribute.IsBlock)
				{
					goto IL_0089;
				}
			}
			else if (mapAttribute is MapZDimensionAttribute { BlockedLevel: >0 } mapZDimensionAttribute && mapZDimensionAttribute.BlockedLevel - 1 == base.Z)
			{
				goto IL_0089;
			}
			return Houses.IsBlocked(guid, x, y);
		}
		goto IL_0089;
		IL_0089:
		return true;
	}

	private bool CorpsPourMoi(int caseMondeX, int caseMondeY)
	{
		int x;
		int y;
		Guid guid = CarteDeLaCase(caseMondeX, caseMondeY, out x, out y);
		if (guid == Guid.Empty || !Intersect.Client.Maps.MapInstance.TryGet(guid, out MapInstance instance))
		{
			return false;
		}
		bool flag3;
		foreach (Entity value in Globals.Entities.Values)
		{
			bool flag = value == null || value == this;
			bool flag2 = flag;
			if (!flag2)
			{
				flag3 = ((value is Player || value is Projectile) ? true : false);
				flag2 = flag3;
			}
			if (flag2 || value.MapId != guid || value.Z != base.Z || value.EstMonstreAuPixel || ((value.X != x || value.Y != y) && (value.EstPiloteLibrement || !value.HoldsTile(x, y))))
			{
				continue;
			}
			if (value is Resource resource)
			{
				ResourceDescriptor descriptor = resource.Descriptor;
				if (descriptor == null || (descriptor.WalkableAfter && resource.IsDead) || (descriptor.WalkableBefore && !resource.IsDead))
				{
					continue;
				}
			}
			flag3 = true;
			goto IL_021b;
		}
		foreach (Entity value2 in instance.LocalEntities.Values)
		{
			if (value2 == null || value2.X != x || value2.Y != y || value2.Z != base.Z || value2.Passable)
			{
				continue;
			}
			flag3 = true;
			goto IL_021b;
		}
		foreach (Critter value3 in instance.LocalCritters.Values)
		{
			if (value3 == null || value3.X != x || value3.Y != y || value3.Z != base.Z || value3.Passable)
			{
				continue;
			}
			flag3 = true;
			goto IL_021b;
		}
		return false;
		IL_021b:
		return flag3;
	}

	private float SortirDesCorpsEnDouceur(ref float x, ref float y, float ecoule)
	{
		_libreCasesEnvahies.Clear();
		_libreCasesDeLImage.Clear();
		float num = 10f;
		float num2 = y + 6f;
		int num3 = (int)MathF.Floor((x - num) / (float)TileWidth);
		int num4 = (int)MathF.Floor((x + num) / (float)TileWidth);
		int num5 = (int)MathF.Floor((num2 - num) / (float)TileHeight);
		int num6 = (int)MathF.Floor((num2 + num) / (float)TileHeight);
		float sortieX;
		float sortieY;
		for (int i = num5; i <= num6; i++)
		{
			for (int j = num3; j <= num4; j++)
			{
				if (EnfoncementDansLaCase(x, num2, j, i, out sortieX, out sortieY) > 0.05f && !MurPourMoi(j, i) && CorpsPourMoi(j, i))
				{
					_libreCasesEnvahies.Add((j, i));
				}
			}
		}
		if (_libreCasesEnvahies.Count == 0)
		{
			return 0f;
		}
		float sortieX2;
		float sortieY2;
		int pireX;
		int pireY;
		float num7 = EnfoncementDansLesCorps(x, num2, out sortieX2, out sortieY2, out pireX, out pireY);
		float num8 = MathF.Min(num7 + 0.01f, 240f * ecoule / 1000f);
		if (num8 > 0f && !EssayerDeSortir(ref x, ref y, sortieX2, sortieY2, num8, num7, parLeBord: false))
		{
			float num9 = pireX * TileWidth;
			float num10 = pireY * TileHeight;
			_libreSorties[0] = (Distance: x + num - num9, X: -1f, Y: 0f);
			_libreSorties[1] = (Distance: num9 + (float)TileWidth - (x - num), X: 1f, Y: 0f);
			_libreSorties[2] = (Distance: num2 + num - num10, X: 0f, Y: -1f);
			_libreSorties[3] = (Distance: num10 + (float)TileHeight - (num2 - num), X: 0f, Y: 1f);
			for (int k = 1; k < _libreSorties.Length; k++)
			{
				int num11 = k;
				while (num11 > 0 && _libreSorties[num11].Distance < _libreSorties[num11 - 1].Distance)
				{
					ref(float, float, float) reference = ref _libreSorties[num11];
					ref(float Distance, float X, float Y) reference2 = ref _libreSorties[num11 - 1];
					(float, float, float) tuple = _libreSorties[num11 - 1];
					(float, float, float) tuple2 = _libreSorties[num11];
					reference = tuple;
					reference2 = tuple2;
					num11--;
				}
			}
			(float, float, float)[] libreSorties = _libreSorties;
			for (int l = 0; l < libreSorties.Length; l++)
			{
				(float, float, float) tuple3 = libreSorties[l];
				float item = tuple3.Item2;
				float item2 = tuple3.Item3;
				if (EssayerDeSortir(ref x, ref y, item, item2, num8, num7, parLeBord: true))
				{
					break;
				}
			}
		}
		int pireX2;
		int pireY2;
		return MathF.Max(0f, EnfoncementDansLesCorps(x, y + 6f, out sortieY, out sortieX, out pireX2, out pireY2));
	}

	private bool EssayerDeSortir(ref float x, ref float y, float versX, float versY, float pas, float enfoncement, bool parLeBord)
	{
		(float X, float Y) tuple = CorpsLibre.Deplacer(x, y + 6f, versX * pas, versY * pas, TileWidth, TileHeight, BloquePourMoi, 10f, 0f, ProfondeurDeLEau, LignesDuMonde);
		float item = tuple.X;
		float item2 = tuple.Y;
		float num = (item - x) * versX + (item2 - y - 6f) * versY;
		float sortieX;
		float sortieY;
		int pireX;
		int pireY;
		float num2 = EnfoncementDansLesCorps(item, item2, out sortieX, out sortieY, out pireX, out pireY);
		bool num3;
		if (!parLeBord)
		{
			num3 = num2 > enfoncement - pas * 0.3f;
		}
		else
		{
			if (num < pas * 0.3f)
			{
				goto IL_00ad;
			}
			num3 = num2 > enfoncement + 0.01f;
		}
		if (!num3)
		{
			x = item;
			y = item2 - 6f;
			return true;
		}
		goto IL_00ad;
		IL_00ad:
		return false;
	}

	private (float X, float Y) NePasSEnfoncer(float departX, float departY, float x, float y, float enfoncementPermis)
	{
		float num = y + 6f;
		float sortieX;
		float sortieY;
		int pireX;
		int pireY;
		float num2 = EnfoncementDansLesCorps(x, num, out sortieX, out sortieY, out pireX, out pireY);
		float num3 = num2 - enfoncementPermis;
		if (num3 <= 0.01f)
		{
			return (X: x, Y: y);
		}
		var (num4, num5) = CorpsLibre.Deplacer(x, num, sortieX * num3, sortieY * num3, TileWidth, TileHeight, BloquePourMoi, 10f, 0f, null, LignesDuMonde);
		if (!(EnfoncementDansLesCorps(num4, num5, out var _, out var _, out pireY, out pireX) > enfoncementPermis + 0.5f))
		{
			return (X: num4, Y: num5 - 6f);
		}
		return (X: departX, Y: departY);
	}

	private float EnfoncementDansLesCorps(float x, float piedsY, out float sortieX, out float sortieY, out int pireX, out int pireY)
	{
		float num = float.MinValue;
		sortieX = 0f;
		sortieY = 0f;
		pireX = 0;
		pireY = 0;
		foreach (var libreCasesEnvahy in _libreCasesEnvahies)
		{
			int item = libreCasesEnvahy.Item1;
			int item2 = libreCasesEnvahy.Item2;
			float sortieX2;
			float sortieY2;
			float num2 = EnfoncementDansLaCase(x, piedsY, item, item2, out sortieX2, out sortieY2);
			if (num2 > num)
			{
				num = num2;
				sortieX = sortieX2;
				sortieY = sortieY2;
				pireX = item;
				pireY = item2;
			}
		}
		return num;
	}

	private float EnfoncementDansLaCase(float x, float piedsY, int caseX, int caseY, out float sortieX, out float sortieY)
	{
		float num = 10f;
		float num2 = caseX * TileWidth;
		float num3 = num2 + (float)TileWidth;
		float num4 = caseY * TileHeight;
		float num5 = num4 + (float)TileHeight;
		float num6 = x - Math.Clamp(x, num2, num3);
		float num7 = piedsY - Math.Clamp(piedsY, num4, num5);
		float num8 = MathF.Sqrt(num6 * num6 + num7 * num7);
		if (num8 > 0.0001f)
		{
			sortieX = num6 / num8;
			sortieY = num7 / num8;
			return num - num8;
		}
		float num9 = x - num2;
		float num10 = num3 - x;
		float num11 = piedsY - num4;
		float y = num5 - piedsY;
		float num12 = MathF.Min(MathF.Min(num9, num10), MathF.Min(num11, y));
		if (num12 != num9)
		{
			if (num12 != num10)
			{
				if (num12 != num11)
				{
					float num13 = 1f;
					sortieX = 0f;
					sortieY = num13;
				}
				else
				{
					float num13 = -1f;
					sortieX = 0f;
					sortieY = num13;
				}
			}
			else
			{
				float num13 = 1f;
				sortieX = num13;
				sortieY = 0f;
			}
		}
		else
		{
			float num13 = -1f;
			sortieX = num13;
			sortieY = 0f;
		}
		return num + num12;
	}

	internal List<(Guid Map, int X, int Y)> CasesDevantMoi()
	{
		_casesDevant.Clear();
		Point point = ((base.DirectionFacing > Direction.None) ? base.DirectionFacing.GetDeltaPoint() : default(Point));
		if (!base.EstPiloteLibrement || !PositionMonde(out var mondeX, out var mondeY))
		{
			int x = base.X + point.X;
			int y = base.Y + point.Y;
			Guid mapId = MapId;
			if (TryGetRealLocation(ref x, ref y, ref mapId))
			{
				_casesDevant.Add((mapId, x, y));
			}
			return _casesDevant;
		}
		float num = MathF.Sqrt(point.X * point.X + point.Y * point.Y);
		if (num <= 0f)
		{
			return _casesDevant;
		}
		float num2 = (float)point.X / num;
		float num3 = (float)point.Y / num;
		float[] sondesLaterales = SondesLaterales;
		foreach (float num4 in sondesLaterales)
		{
			float num5 = mondeX + num2 * 24f - num3 * num4 * 11f;
			float num6 = mondeY + num3 * 24f + num2 * num4 * 11f;
			int x2;
			int y2;
			Guid guid = CarteDeLaCase((int)MathF.Floor(num5 / (float)TileWidth), (int)MathF.Floor(num6 / (float)TileHeight), out x2, out y2);
			if (guid != Guid.Empty && !_casesDevant.Contains((guid, x2, y2)))
			{
				_casesDevant.Add((guid, x2, y2));
			}
		}
		return _casesDevant;
	}

	private Entity? EvenementDevantMoi()
	{
		foreach (var (id, num, num2) in CasesDevantMoi())
		{
			if (!Intersect.Client.Maps.MapInstance.TryGet(id, out MapInstance instance))
			{
				continue;
			}
			foreach (Entity value in instance.LocalEntities.Values)
			{
				if (value != null && value.X == num && value.Y == num2 && value.Z == base.Z && !value.Passable)
				{
					return value;
				}
			}
		}
		return null;
	}

	public (Guid MapId, int TileIndex) CaseDeRamassage()
	{
		int mapWidth = Options.Instance.Map.MapWidth;
		(Guid, int) tuple = (MapId, base.Y * mapWidth + base.X);
		if (!base.EstPiloteLibrement || !PositionMonde(out var mondeX, out var mondeY))
		{
			return tuple;
		}
		float num = mondeY + 6f;
		int num2 = (int)MathF.Floor(mondeX / (float)TileWidth);
		int num3 = (int)MathF.Floor(num / (float)TileHeight);
		(Guid, int) result = tuple;
		float num4 = float.MaxValue;
		for (int i = -1; i <= 1; i++)
		{
			for (int j = -1; j <= 1; j++)
			{
				int x;
				int y;
				Guid guid = CarteDeLaCase(num2 + j, num3 + i, out x, out y);
				if (guid == Guid.Empty || !Intersect.Client.Maps.MapInstance.TryGet(guid, out MapInstance instance))
				{
					continue;
				}
				int num5 = y * mapWidth + x;
				if (instance.MapItems.TryGetValue(num5, out List<IMapItemInstance> value) && value.Count != 0)
				{
					float num6 = (float)((num2 + j) * TileWidth) + (float)TileWidth / 2f - mondeX;
					float num7 = (float)((num3 + i) * TileHeight) + (float)TileHeight / 2f - num;
					float num8 = num6 * num6 + num7 * num7;
					if (num8 <= 784f && num8 < num4)
					{
						result = (guid, num5);
						num4 = num8;
					}
				}
			}
		}
		return result;
	}

	private static LigneDeBlocage[]? LignesDeLaCaseDuMonde(int caseMondeX, int caseMondeY, out float origineX, out float origineY)
	{
		int x;
		int y;
		IndexDesLignes indexDesLignes = IndexDeLaCase(caseMondeX, caseMondeY, out x, out y);
		origineX = (caseMondeX - x) * Options.Instance.Map.TileWidth;
		origineY = (caseMondeY - y) * Options.Instance.Map.TileHeight;
		return indexDesLignes.DeLaCase(x, y);
	}

	private static IndexDesLignes IndexDeLaCase(int caseMondeX, int caseMondeY, out int x, out int y)
	{
		Guid guid = CarteDeLaCase(caseMondeX, caseMondeY, out x, out y);
		if (!(guid != Guid.Empty) || !Intersect.Client.Maps.MapInstance.TryGet(guid, out MapInstance instance))
		{
			return IndexDesLignes.Vide;
		}
		return instance.IndexDesLignes;
	}

	private static bool LigneBarreLePas(int caseMondeX, int caseMondeY, int dx, int dy)
	{
		int x;
		int y;
		IndexDesLignes depart = IndexDeLaCase(caseMondeX, caseMondeY, out x, out y);
		int x2;
		int y2;
		IndexDesLignes arrivee = IndexDeLaCase(caseMondeX + dx, caseMondeY + dy, out x2, out y2);
		return IndexDesLignes.BarreLePas(depart, x, y, arrivee, x2, y2, IndexDesLignes.DuPas(dx, dy));
	}

	private bool LigneBarreMonPasEnCases(Point depuis, Point pas)
	{
		if (!Intersect.Client.Maps.MapInstance.TryGet(MapId, out MapInstance instance))
		{
			return false;
		}
		return LigneBarreLePas(instance.GridX * Options.Instance.Map.MapWidth + depuis.X, instance.GridY * Options.Instance.Map.MapHeight + depuis.Y, pas.X, pas.Y);
	}

	private static Guid CarteDeLaCase(int caseMondeX, int caseMondeY, out int x, out int y)
	{
		int mapWidth = Options.Instance.Map.MapWidth;
		int mapHeight = Options.Instance.Map.MapHeight;
		int num = (int)Math.Floor((double)caseMondeX / (double)mapWidth);
		int num2 = (int)Math.Floor((double)caseMondeY / (double)mapHeight);
		x = caseMondeX - num * mapWidth;
		y = caseMondeY - num2 * mapHeight;
		Guid[,] mapGrid = Globals.MapGrid;
		if (mapGrid == null || num < 0 || num2 < 0 || num >= Globals.MapGridWidth || num2 >= Globals.MapGridHeight || num >= mapGrid.GetLength(0) || num2 >= mapGrid.GetLength(1))
		{
			return Guid.Empty;
		}
		return mapGrid[num, num2];
	}

	private bool TenterLaRuee()
	{
		CombatOptions combat = Options.Instance.Combat;
		long milliseconds = Timing.Global.Milliseconds;
		if (this != Globals.Me || base.Level < combat.RueeNiveau || !base.EstPiloteLibrement || base.FlightMode != FlightMode.None || base.Dashing != null || !PositionMonde(out var mondeX, out var mondeY))
		{
			return false;
		}
		float num = (float)(Math.Max(1, combat.RueeCases) * TileWidth) + 16f;
		Entity entity = Echanges.ProjeteeRecemment(milliseconds, combat.RueeApresProjectionMs);
		if (entity != null && entity.PositionMonde(out var mondeX2, out var mondeY2))
		{
			float num2 = mondeX2 - mondeX;
			float num3 = mondeY2 - mondeY;
			float num4 = MathF.Sqrt(num2 * num2 + num3 * num3);
			if (num4 >= 40f && num4 <= num)
			{
				PoserSaPositionAvantDeSeFiger();
				PacketSender.SendAttack(entity.Id, ruee: true, GesteDeCoup.Ordinaire, 0);
				_rueeEnCoursJusqua = milliseconds + 300;
				return true;
			}
		}
		if (milliseconds < _rueePrete)
		{
			return false;
		}
		var (num5, num6) = DirectionVoulueLibre();
		if (Heritage.Actif && _hRueeSurDirectionTapee && num5 == 0f && num6 == 0f)
		{
			(num5, num6) = DirectionTapeeHeritage();
		}
		if (num5 == 0f && num6 == 0f)
		{
			return false;
		}
		Entity entity2 = null;
		float num7 = float.MaxValue;
		foreach (Entity value in Globals.Entities.Values)
		{
			if (value == null || value == this || !value.CanBeAttacked || value.IsDying || value.Z != base.Z || !value.PositionMonde(out var mondeX3, out var mondeY3))
			{
				continue;
			}
			bool flag = value.Id == TargetId;
			bool flag2 = value.Type == EntityType.GlobalEntity && !(value is Critter);
			bool flag3 = flag2;
			if (flag3)
			{
				NpcAggression aggression = value.Aggression;
				bool flag4 = (uint)(aggression - 3) <= 1u;
				flag3 = !flag4;
			}
			bool flag5 = flag3;
			if (!flag && !flag5)
			{
				continue;
			}
			float num8 = mondeX3 - mondeX;
			float num9 = mondeY3 - mondeY;
			float num10 = MathF.Sqrt(num8 * num8 + num9 * num9);
			if (!(num10 < 40f) && !(num10 > num) && !((num5 * num8 + num6 * num9) / num10 < 0.5f))
			{
				float num11 = (flag ? 0f : 1000f) + num10;
				if (num11 < num7)
				{
					num7 = num11;
					entity2 = value;
				}
			}
		}
		if (entity2 == null)
		{
			return false;
		}
		PoserSaPositionAvantDeSeFiger();
		PacketSender.SendAttack(entity2.Id, ruee: true, GesteDeCoup.Ordinaire, 0);
		_rueePrete = milliseconds + Math.Max(0, combat.RueeDelaiMs);
		_rueeEnCoursJusqua = milliseconds + 300;
		if (Heritage.Actif)
		{
			ConsommerLesDirectionsTapees();
		}
		return true;
	}

	internal (sbyte X, sbyte Y) DirectionTenuePourLeBond()
	{
		var (num, num2) = DirectionVoulueLibre();
		return (X: (sbyte)((num > 0.38f) ? 1 : ((num < -0.38f) ? (-1) : 0)), Y: (sbyte)((num2 > 0.38f) ? 1 : ((num2 < -0.38f) ? (-1) : 0)));
	}

	private bool ToucheDeCoupAuxEchanges(bool tenue, bool appuiNeuf, long maintenant)
	{
		_coupAuRelacher = false;
		if (appuiNeuf)
		{
			_appuiDeCoupA = maintenant;
			_coupPartiDeCetAppui = false;
			_appuiDeRecolte = false;
			_chargeDeCetAppui = false;
			_appuiEnAttente = false;
			_appuiImmediat = false;
			_poseDeLAppuiMontree = false;
		}
		if (Heritage.Actif && Echanges.EstSonne(this) && AppuiSousLeSonneHeritage(tenue, appuiNeuf, maintenant))
		{
			return true;
		}
		CombatOptions combat = Options.Instance.Combat;
		if (Echanges.ChargeLocale(this))
		{
			if (!PeutCharger())
			{
				Echanges.AbandonnerLaCharge(this);
				return true;
			}
			if (!tenue || Echanges.ChargeDepuis(this) >= Math.Max(1, combat.CoupChargeLacherMs))
			{
				LacherLeCoupCharge();
			}
			return true;
		}
		if (appuiNeuf)
		{
			_appuiDeRecolte = !PeutCharger() || FrappeSansAttendre();
			_appuiImmediat = _appuiDeRecolte || ProchainCoupEnPleinCombo() || !ChargeActive;
			_appuiEnAttente = !_appuiImmediat;
			return _appuiEnAttente;
		}
		if (_appuiEnAttente)
		{
			if (!tenue)
			{
				_appuiEnAttente = false;
				if (base.IsAttacking)
				{
					_attackBufferedUntil = base.AttackTimer + 200;
					if (Heritage.Actif)
					{
						_attackBufferedUntil = base.AttackTimer + MemoireDAppuiHeritage;
					}
					return true;
				}
				_coupAuRelacher = true;
				return false;
			}
			if (ChargeActive && maintenant - _appuiDeCoupA >= Math.Max(1, combat.CoupChargeDebutMs) && !base.IsAttacking && !_chargeDeCetAppui && PeutCharger())
			{
				_appuiEnAttente = false;
				Echanges.CommencerLaCharge(this);
				PacketSender.SendAttack(Guid.Empty, ruee: false, GesteDeCoup.ChargeDebut, 0);
				ChoisirLaPlancheDuPas(PasDuProchainCoup());
				base.DureeDuCoupLocal = 0;
				_pasExpireA = 0L;
			}
			return true;
		}
		return Echanges.ArmementEnCours(this);
	}

	private bool FrappeSansAttendre()
	{
		if (base.FlightMode != FlightMode.None)
		{
			return true;
		}
		if (base.EstPiloteLibrement)
		{
			return CibleAuCorpsACorpsLibre() is Resource;
		}
		foreach (var (map, x, y) in CasesDevantMoi())
		{
			if ((CibleSurCase(map, x, y, quiLaQuitte: false) ?? CibleSurCase(map, x, y, quiLaQuitte: true)) is Resource)
			{
				return true;
			}
		}
		return Recolte.CoinDePecheDevant(this) != null;
	}

	private void NoterLeCoupParti(bool recolte)
	{
		_coupPartiDeCetAppui = true;
		_appuiDeRecolte |= recolte;
	}

	private void FinirLAppuiDeRecolte()
	{
		_appuiDeRecolte = false;
		_coupPartiDeCetAppui = true;
	}

	private bool PeutCharger()
	{
		if (this == Globals.Me && !base.IsBlocking && !base.IsCharging && !IsBeaming && !base.IsGuardStunned && !base.IsTransforming && !base.IsCasting && !IsBusy && !base.IsDying && base.FlightMode == FlightMode.None && base.Dashing == null && !Echanges.EstSonne(this))
		{
			return !Vanish.EstFige(this);
		}
		return false;
	}

	private void LacherLeCoupCharge()
	{
		Entity entity = null;
		if (base.EstPiloteLibrement)
		{
			entity = CibleAuCorpsACorpsLibre();
		}
		else
		{
			FaceAdjacentFoe();
			foreach (var item4 in CasesDevantMoi())
			{
				Guid item = item4.Map;
				int item2 = item4.X;
				int item3 = item4.Y;
				entity = CibleSurCase(item, item2, item3, quiLaQuitte: false) ?? CibleSurCase(item, item2, item3, quiLaQuitte: true);
				if (entity != null)
				{
					break;
				}
			}
		}
		if (entity is Resource)
		{
			entity = null;
		}
		PoserSaPositionAvantDeSeFiger();
		PacketSender.SendAttack(entity?.Id ?? Guid.Empty, ruee: false, GesteDeCoup.ChargeLacher, 0);
		ChoisirLaPlancheDuPas((byte)CoupsDuCombo);
		Echanges.LacherLaChargeLocale(this, entity);
		_chargeDeCetAppui = true;
	}

	private bool DansLAllongeHeritage(Entity candidat, float ecartX, float ecartY)
	{
		float num = HeritageRegles.AllongeVerticaleMax(ecartX, ecartY);
		float num2 = (float)Math.Max(HeritageRegles.Table(CoupHeritage.Montant).AllongePx, Options.Instance.Combat.Heritage.Etats.RecollePx) + num;
		if (num2 <= 0f || candidat is Resource || !CorpsACorps.APortee(ecartX, ecartY, num2, base.RayonDuCorps, candidat.RayonDuCorps))
		{
			return false;
		}
		if (this != Globals.Me || base.FlightMode != FlightMode.None || Echanges.ChargeLocale(this) || ArmeAProjectile())
		{
			return false;
		}
		float num3 = Heritage.Cible.AllongePour(this, candidat, ecartX, ecartY) + num;
		if (num3 > 0f)
		{
			return CorpsACorps.APortee(ecartX, ecartY, num3, base.RayonDuCorps, candidat.RayonDuCorps);
		}
		return false;
	}

	private void TournerVersUnCorpsColleHeritage(Entity cible, float moiX, float moiY)
	{
		if (cible.PositionMonde(out var mondeX, out var mondeY))
		{
			Direction direction = Heritage.Cible.RegardVers(mondeX - moiX, mondeY - moiY, base.DirectionFacing);
			if (direction != base.DirectionFacing && !EvenementDActionDevantMoi())
			{
				base.DirectionFacing = direction;
				PacketSender.SendDirection(direction);
			}
		}
	}

	private bool TenterLaRueeSurDirectionTapee()
	{
		if (!HeritageRegles.Actif || this != Globals.Me || !base.EstPiloteLibrement)
		{
			return false;
		}
		var (num, num2) = DirectionTapeeHeritage();
		if (num == 0f && num2 == 0f)
		{
			return false;
		}
		_hRueeSurDirectionTapee = true;
		try
		{
			return TenterLaRuee();
		}
		finally
		{
			_hRueeSurDirectionTapee = false;
		}
	}

	private void ConsommerLesDirectionsTapees()
	{
		var (direction, val) = Heritage.Choix.DerniereTouche(Heritage.Appuis.PresseA, DeplacementLibre.MaintenantLocal);
		if (direction != Direction.None)
		{
			_hDernierFrontUtilise = Math.Max(_hDernierFrontUtilise, val);
		}
	}

	private bool AppuiDeCoupAvale()
	{
		if (this != Globals.Me)
		{
			return false;
		}
		if (Heritage.Appuis.AppuiAvale())
		{
			return !EnRecolteAuClic;
		}
		return false;
	}

	internal (CoupHeritage Coup, Direction Touche, long Front, SensDeDirection Sens) DemandeHeritageContre(float ecartX, float ecartY)
	{
		return Heritage.Choix.Demande(Heritage.Appuis.PresseA, Heritage.Appuis.Tenues, Heritage.Appuis.CoupPresseA, DeplacementLibre.MaintenantLocal, _hDernierFrontUtilise, LectureTenue, ecartX, ecartY, base.Level);
	}

	internal (float X, float Y) DirectionTapeeHeritage()
	{
		if (!HeritageRegles.Actif)
		{
			return (X: 0f, Y: 0f);
		}
		return Heritage.Choix.DirectionTapee(Heritage.Appuis.PresseA, Heritage.Appuis.CoupPresseA, DeplacementLibre.MaintenantLocal, _hDernierFrontUtilise, Heritage.Appuis.Tenues);
	}

	internal CoupHeritage DemandeHeritageProbable()
	{
		if (!HeritageRegles.Actif || !base.EstPiloteLibrement || !PositionMonde(out var mondeX, out var mondeY))
		{
			return CoupHeritage.Aucun;
		}
		bool flag = false;
		float num = float.MaxValue;
		float ecartX = 0f;
		float ecartY = 0f;
		foreach (Entity value in Globals.Entities.Values)
		{
			if (value == null || value == this || !value.CanBeAttacked || value.IsDying || value.Z != base.Z || !value.PositionMonde(out var mondeX2, out var mondeY2))
			{
				continue;
			}
			bool flag2 = value.Id == TargetId;
			bool flag3 = flag2;
			bool flag4 = flag3;
			if (!flag4)
			{
				bool flag5 = value.Type == EntityType.GlobalEntity && !(value is Critter);
				bool flag6 = flag5;
				if (flag6)
				{
					NpcAggression aggression = value.Aggression;
					bool flag7 = (uint)(aggression - 3) <= 1u;
					flag6 = !flag7;
				}
				flag4 = flag6;
			}
			bool flag8 = flag4;
			float num2 = mondeX2 - mondeX;
			float num3 = mondeY2 - mondeY;
			if (flag8 && CorpsACorps.APortee(num2, num3, AllongeDuPassif, base.RayonDuCorps, value.RayonDuCorps))
			{
				float num4 = (flag2 ? (-1f) : (num2 * num2 + num3 * num3));
				if (num4 < num)
				{
					flag = true;
					num = num4;
					ecartX = num2;
					ecartY = num3;
				}
			}
		}
		if (!flag)
		{
			return CoupHeritage.Aucun;
		}
		return DemandeHeritageContre(ecartX, ecartY).Coup;
	}

	private byte PasPreditHeritage(Guid cibleId)
	{
		if (!Options.Instance.Combat.Heritage.Etats.SerieSurPlusieursCibles)
		{
			return PasPredit(cibleId);
		}
		if (CoupsDuCombo < 2 || cibleId == Guid.Empty)
		{
			return 1;
		}
		return (byte)((Timing.Global.Milliseconds > _pasExpireA) ? 1u : ((uint)(_pasLocal % CoupsDuCombo + 1)));
	}

	internal void GarderLeRangHeritage()
	{
		long milliseconds = Timing.Global.Milliseconds;
		if (_pasLocal > 0 && milliseconds <= _pasExpireA)
		{
			_pasExpireA = Math.Max(_pasExpireA, milliseconds + Options.Instance.Combat.Heritage.Etats.RangGardeMs);
		}
	}

	private bool ArmeAProjectile()
	{
		int weaponSlot = Options.Instance.Equipment.WeaponSlot;
		if (weaponSlot < 0 || weaponSlot >= base.MyEquipment.Length)
		{
			return false;
		}
		int num = base.MyEquipment[weaponSlot];
		if (num >= 0 && num < base.Inventory.Length)
		{
			return DatabaseObject<ItemDescriptor>.Get(base.Inventory[num].ItemId)?.Projectile != null;
		}
		return false;
	}

	internal bool LancerUnCoupHeritage(Entity en)
	{
		if (!HeritageRegles.Actif || this != Globals.Me || base.FlightMode != FlightMode.None || !base.EstPiloteLibrement || ArmeAProjectile() || !PositionMonde(out var mondeX, out var mondeY) || !en.PositionMonde(out var mondeX2, out var mondeY2))
		{
			return false;
		}
		long milliseconds = Timing.Global.Milliseconds;
		float ecartX = mondeX2 - mondeX;
		float ecartY = mondeY2 - mondeY;
		var (coupHeritage, touche, num, sens) = DemandeHeritageContre(ecartX, ecartY);
		if (HeritageRegles.EstADirection(coupHeritage))
		{
			_hDernierFrontUtilise = Math.Max(_hDernierFrontUtilise, num);
		}
		(byte Pas, CoupHeritage Coup) tuple2 = HeritageRegles.Suite(PasPreditHeritage(en.Id), coupHeritage, CoupsDuCombo);
		byte item = tuple2.Pas;
		CoupHeritage item2 = tuple2.Coup;
		int num2 = Heritage.Choix.Avance(item2, coupHeritage, _coupAuRelacher, _appuiDeCoupA, base.AttackTimer, milliseconds);
		if (HeritageRegles.EstADirection(coupHeritage))
		{
			PoserSaPositionAvantDeSeFiger();
		}
		PacketSender.SendAttack(en.Id, ruee: false, GesteDeCoup.Ordinaire, HeritageRegles.Octet(coupHeritage, num2));
		int num3 = HeritageRegles.DureeClient(item2, CalculateAttackTime());
		OublierLaRueeHeritage();
		_hCompteGarde = true;
		_hPasAvant = _pasLocal;
		_hPasCibleAvant = _pasCibleId;
		_hPasExpireAvant = _pasExpireA;
		NoterLeCoupLocal(en.Id, item, num3);
		base.AttackTimer = milliseconds + num3;
		NoterLeCoupHeritage(item2, num2, en.Id);
		_attackApproachUntil = 0L;
		Heritage.Lancer(this, en, item2);
		EcrireAuJournalDesAppuis(item2, item, touche, num, sens, ecartX, ecartY, num2);
		return true;
	}

	internal void EchoHeritage(byte pas, byte coup, int duree)
	{
		if (coup == 0)
		{
			return;
		}
		if (Heritage.Choix.EchoDeCeGeste(base.CoupEnCours, coup) && base.DureeDuCoupLocal > 0)
		{
			var (num, dureeDuCoupLocal) = Heritage.Choix.FinRecalee(base.AttackTimer, base.DureeDuCoupLocal, duree);
			if (_pasExpireA > 0)
			{
				_pasExpireA += num - base.AttackTimer;
			}
			RecalerLaFinDuCoupHeritage(num);
			base.DureeDuCoupLocal = dureeDuCoupLocal;
		}
		RecalerLeRang(pas, duree);
	}

	private void RecalerLeRang(byte pas, int duree)
	{
		if (pas != 0)
		{
			if (pas >= CoupsDuCombo)
			{
				_pasLocal = 0;
				_pasExpireA = 0L;
			}
			else if (pas != _pasLocal)
			{
				_pasLocal = pas;
				_pasExpireA = Math.Max(_pasExpireA, Timing.Global.Milliseconds + duree + Math.Max(0, Options.Instance.Combat.ComboWindow));
			}
		}
	}

	internal void RendreLeCompteDAvant(Guid cibleId)
	{
		if (_hCompteGarde && (!(cibleId != Guid.Empty) || !(cibleId != base.CibleDuDernierCoupHeritage)))
		{
			_hCompteGarde = false;
			_pasLocal = _hPasAvant;
			_pasCibleId = _hPasCibleAvant;
			_pasExpireA = _hPasExpireAvant;
		}
	}

	private void EcrireAuJournalDesAppuis(CoupHeritage coup, byte pas, Direction touche, long front, SensDeDirection sens, float ecartX, float ecartY, int avance)
	{
		if (Options.Instance.Combat.Heritage.JournalDesAppuis)
		{
			bool flag = touche != Direction.None;
			if (!flag)
			{
				(Direction Touche, long Front) tuple = Heritage.Choix.DerniereTouche(Heritage.Appuis.PresseA, DeplacementLibre.MaintenantLocal);
				touche = tuple.Touche;
				front = tuple.Front;
				sens = HeritageRegles.Sens(touche, ecartX, ecartY);
			}
			long coupPresseA = Heritage.Appuis.CoupPresseA;
			string text = touche switch
			{
				Direction.Up => "Haut", 
				Direction.Down => "Bas", 
				Direction.Left => "Gauche", 
				Direction.Right => "Droite", 
				_ => "aucune", 
			};
			if (touche != Direction.None && front != long.MinValue && coupPresseA != long.MinValue)
			{
				text += $" tapee il y a {coupPresseA - front} ms{(flag ? string.Empty : " (non retenue)")}";
			}
			string text2 = sens switch
			{
				SensDeDirection.VersLui => "vers lui", 
				SensDeDirection.Oppose => "oppose", 
				SensDeDirection.DeCote => "de cote", 
				_ => "aucun", 
			};
			ApplicationContext.Context.Value?.Logger.LogInformation("Heritage : coup {Coup} rang {Rang} ; touche {Touche} ; sens lu {Sens} ; ecart ({EcartX}, {EcartY}) px ; lecture {Lecture} ; avance {Avance} ms", coup, pas, text, text2, (int)MathF.Round(ecartX), (int)MathF.Round(ecartY), LectureTenue ? "tenue" : "tapee", avance);
		}
	}

	private bool AppuiGardePasseDevantHeritage(long maintenant)
	{
		return Heritage.Appuis.AppuiGardePasseDevant(_appuiEnAttente, Echanges.ChargeLocale(this) || Echanges.EstSonne(this), base.IsAttacking, _attackBufferedUntil, maintenant);
	}

	private void CoupAvantLeDashHeritage()
	{
		if (_appuiEnAttente && !Echanges.ChargeLocale(this) && !base.IsAttacking && EnnemiAPorteeDeBras())
		{
			_coupAuRelacher = true;
			bool flag = TryAttack();
			_coupAuRelacher = false;
			if (flag)
			{
				_appuiEnAttente = false;
				NoterLeCoupParti(_dernierCoupRecolte);
			}
		}
	}

	private bool AppuiSousLeSonneHeritage(bool tenue, bool appuiNeuf, long maintenant)
	{
		if (Echanges.ChargeLocale(this))
		{
			return false;
		}
		if (appuiNeuf)
		{
			if (!base.EstPiloteLibrement || base.FlightMode != FlightMode.None)
			{
				return false;
			}
			_appuiDeRecolte = false;
			_appuiImmediat = false;
			_appuiEnAttente = true;
			return true;
		}
		if (_appuiEnAttente)
		{
			if (!tenue)
			{
				_appuiEnAttente = false;
				_attackBufferedUntil = (_hGardeSousLeSonne = Math.Max(maintenant, base.AttackTimer) + MemoireDAppuiHeritage);
			}
			return true;
		}
		if (_attackBufferedUntil > maintenant && _attackBufferedUntil == _hGardeSousLeSonne)
		{
			_attackBufferedUntil = (_hGardeSousLeSonne = Math.Max(maintenant, base.AttackTimer) + MemoireDAppuiHeritage);
			return true;
		}
		return false;
	}

	internal bool DirectionTenuePourLElan()
	{
		var (num, num2) = DirectionVoulueLibre();
		if (num == 0f)
		{
			return num2 != 0f;
		}
		return true;
	}

	private (float X, float Y) AvancerDElanHeritage(float x, float y, float ecoule, bool entrave, float enfoncement)
	{
		var (num, num2) = Heritage.Elan.Pas(this, x, y, ecoule, entrave);
		if (num == 0f && num2 == 0f)
		{
			return (X: x, Y: y);
		}
		(float X, float Y) tuple2 = CorpsLibre.Deplacer(x, y + 6f, num, num2, TileWidth, TileHeight, BloquePourMoi, 10f, 0f, ProfondeurDeLEau, LignesDuMonde);
		float x2 = tuple2.X;
		float item = tuple2.Y;
		float y2 = item - 6f;
		if (_libreCasesEnvahies.Count > 0)
		{
			(x2, y2) = NePasSEnfoncer(x, y, x2, y2, enfoncement);
		}
		(float X, float Y) tuple4 = ContournerLesMonstresAuPixel(x, y, x2, y2);
		x2 = tuple4.X;
		y2 = tuple4.Y;
		float num3 = MathF.Sqrt(num * num + num2 * num2);
		Heritage.Elan.Consommer(((x2 - x) * num + (y2 - y) * num2) / num3);
		return (X: x2, Y: y2);
	}

	internal (float X, float Y) BornerUnePousseeHeritage(Entity cible, float x, float y, float sensX, float sensY, float distance)
	{
		HeritageDecorOptions decor = Options.Instance.Combat.Heritage.Decor;
		return Heritage.Poussee.Borner(x, y, sensX, sensY, distance, MathF.Min(cible.RayonDuCorps, 10f), TileWidth, TileHeight, MurPourUnePoussee, LignesDuMonde, decor.Actif && decor.PremierContact);
	}

	private bool MurPourUnePoussee(int caseMondeX, int caseMondeY)
	{
		if (MurPourMoi(caseMondeX, caseMondeY))
		{
			return true;
		}
		int x;
		int y;
		Guid guid = CarteDeLaCase(caseMondeX, caseMondeY, out x, out y);
		foreach (Entity value in Globals.Entities.Values)
		{
			if (value is Resource resource && !(resource.MapId != guid) && resource.X == x && resource.Y == y && resource.Z == base.Z)
			{
				ResourceDescriptor descriptor = resource.Descriptor;
				if (descriptor != null && (!descriptor.WalkableAfter || !resource.IsDead) && (!descriptor.WalkableBefore || resource.IsDead))
				{
					return true;
				}
			}
		}
		return false;
	}

	internal bool GardeQuiPlante()
	{
		if (Heritage.Actif)
		{
			return GardeRefaite.Immobile(Options.Instance.Combat.Garde, Options.Instance.Combat.EnableGuard, base.IsBlocking);
		}
		return false;
	}

	private bool SeTournerEnGarde(bool empeche)
	{
		if (!GardeQuiPlante())
		{
			return false;
		}
		if (!empeche && !Heritage.RegardTenu(this))
		{
			(float X, float Y) tuple = DirectionVoulueLibre();
			float item = tuple.X;
			float item2 = tuple.Y;
			Direction direction = ((item != 0f || item2 != 0f) ? DirectionDuVecteur(item, item2) : Direction.None);
			if (direction != Direction.None)
			{
				base.DirectionFacing = direction;
				PickLastDirection(direction);
			}
		}
		return true;
	}

	private bool SeTournerEnGardeEnCases()
	{
		if (!GardeQuiPlante())
		{
			return false;
		}
		Direction directionMoving = DirectionMoving;
		if (directionMoving > Direction.None && directionMoving != base.DirectionFacing && Globals.EventDialogs.Count == 0)
		{
			base.DirectionFacing = directionMoving;
			PacketSender.SendDirection(directionMoving);
		}
		return true;
	}

	private float? AngleLieHeritage()
	{
		Guid guid = Heritage.Lien.CibleVisee(this, KiBeam.PhaseOf(this) == BeamPhase.None);
		if (guid == Guid.Empty || !Globals.Entities.TryGetValue(guid, out Entity value) || value == null || value.IsDying || value.IsDisposed)
		{
			return null;
		}
		return Heritage.Lien.AngleVers(base.WorldPos, value.WorldPos);
	}

	internal bool UnCoupAttendHeritage(long maintenant)
	{
		if (this != Globals.Me)
		{
			return false;
		}
		bool flag = _appuiEnAttente && (!Heritage.Appuis.CoupTenu || maintenant - _appuiDeCoupA < Math.Max(1, Options.Instance.Combat.CoupChargeDebutMs));
		bool flag2 = Heritage.Appuis.CoupPasEncoreVu(DeplacementLibre.MaintenantLocal) && PeutCharger() && ChargeActive && !ProchainCoupEnPleinCombo();
		if ((flag2 || flag || (_coupAuRelacher && !base.IsAttacking) || _attackBufferedUntil > maintenant) && !IsBusy)
		{
			return UnCorpsAFrapperHeritage();
		}
		return false;
	}

	private bool UnCorpsAFrapperHeritage()
	{
		if (!PositionMonde(out var mondeX, out var mondeY))
		{
			return false;
		}
		foreach (Entity value in Globals.Entities.Values)
		{
			if (value != null && value != this && !(value is Resource) && value.CanBeAttacked && !value.IsDying && value.Z == base.Z && value.PositionMonde(out var mondeX2, out var mondeY2) && CorpsACorps.APortee(mondeX2 - mondeX, mondeY2 - mondeY, TileWidth, base.RayonDuCorps, value.RayonDuCorps))
			{
				return true;
			}
		}
		return false;
	}

	private int CaseDeLaBarreHeritage(int tenue)
	{
		int num = Heritage.Appuis.RaccourciAvale();
		if (tenue >= 0 || num < 0)
		{
			return tenue;
		}
		mLastHotbarUseTime.TryAdd(num, 0L);
		return num;
	}

	private long AppuiGardeApresLImage(long memoire)
	{
		return Heritage.Appuis.MemoireRendue(memoire, _coupPartiDeCetAppui, _appuiRendu || base.IsBlocking || base.IsCharging || IsBeaming || base.IsGuardStunned || base.IsTransforming || base.IsCasting || Echanges.EstSonne(this), EnnemiEnApproche());
	}

	private bool ArreteParUnCorpsHeritage(float x, float y, float versX, float versY)
	{
		float num = y + 6f;
		foreach (var (num2, num3, num4) in _libreMonstres)
		{
			if (Heritage.Appuis.ButeSurCeCorps(num2 - x, num3 - num, versX, versY, 10f + num4))
			{
				return true;
			}
		}
		return false;
	}

	internal void RecommencerLaSerieHeritage()
	{
		_pasLocal = 0;
		_pasExpireA = 0L;
	}

	internal void NoterLaRueeHeritage(Guid cibleId, int dureeMs)
	{
		_hRueeCibleId = cibleId;
		_hRueeJusqua = Timing.Global.Milliseconds + Math.Max(0, dureeMs) + 400;
	}

	internal void OublierLaRueeHeritage()
	{
		_hRueeCibleId = Guid.Empty;
	}

	internal static bool EstLaFrappeDeLaRuee(Guid rueeCibleId, long rueeJusqua, long maintenant, byte coup, bool gesteEnCours)
	{
		if (rueeCibleId != Guid.Empty && maintenant <= rueeJusqua && HeritageRegles.EstUnCoupJoue(coup))
		{
			return !gesteEnCours;
		}
		return false;
	}

	internal bool FrapperALArriveeDeLaRuee(byte pas, byte coup, int duree)
	{
		long milliseconds = Timing.Global.Milliseconds;
		Guid hRueeCibleId = _hRueeCibleId;
		bool flag = EstLaFrappeDeLaRuee(hRueeCibleId, _hRueeJusqua, milliseconds, coup, base.CoupEnCours != CoupHeritage.Aucun);
		if (HeritageRegles.EstUnCoupJoue(coup))
		{
			_hRueeCibleId = Guid.Empty;
		}
		if (!flag || pas == 0 || duree <= 0 || !Globals.Entities.TryGetValue(hRueeCibleId, out Entity value) || value == null || value.IsDying)
		{
			return false;
		}
		CoupHeritage coupHeritage = (CoupHeritage)coup;
		_hCompteGarde = true;
		_hPasAvant = _pasLocal;
		_hPasCibleAvant = _pasCibleId;
		_hPasExpireAvant = _pasExpireA;
		NoterLeCoupLocal(hRueeCibleId, pas, duree);
		base.AttackTimer = milliseconds + duree;
		NoterLeCoupHeritage(coupHeritage, HeritageRegles.Table(coupHeritage).ImpactMs, hRueeCibleId);
		_attackApproachUntil = 0L;
		Heritage.LancerALArrivee(this, value, coupHeritage);
		if (Options.Instance.Combat.Heritage.JournalDesAppuis)
		{
			ApplicationContext.Context.Value?.Logger.LogInformation("Heritage : coup {Coup} rang {Rang} ; a l'arrivee d'une ruee, sur l'echo du serveur ; duree {Duree} ms", coupHeritage, pas, duree);
		}
		return true;
	}

	private bool TryBeginMaitre(int index, SpellDescriptor sort)
	{
		TechniqueDeMaitre technique = Options.Instance.Maitres.TechniqueDe(sort.Id);
		if (!MaitresOptions.SeTient(technique))
		{
			return false;
		}
		if (this != Globals.Me || IsBeaming || IsBusy || base.IsCasting || base.Dashing != null || base.FlightMode != FlightMode.None)
		{
			return true;
		}
		if (_maitreAttendRelache && _maitreSortId == sort.Id)
		{
			return true;
		}
		float degrees = BeamAimFromStick() ?? BeamAimFromMouse() ?? KiBeam.AngleOf(base.DirectionFacing);
		long milliseconds = Timing.Global.Milliseconds;
		_maitreSlot = index;
		_maitreSortId = sort.Id;
		_maitreVisee = (_maitreViseeEnvoyee = KiBeam.Normalize(degrees));
		_maitreViseeEnvoyeeA = milliseconds;
		_maitreViseeA = milliseconds;
		FaceBeam(_maitreVisee);
		Maitres.CommencerLocal(this, technique, _maitreVisee);
		PoserSaPositionAvantDeSeFiger();
		PacketSender.SendBeamRequest(index, charging: true, cancel: false, _maitreVisee);
		return true;
	}

	private void UpdateMaitreInput()
	{
		if (this != Globals.Me)
		{
			return;
		}
		if (_maitreAttendRelache && !MaitreToucheTenue())
		{
			_maitreAttendRelache = false;
		}
		if (_maitreSlot < 0)
		{
			return;
		}
		if (Maitres.ChargeDe(this) == TechniqueDeMaitre.Aucune)
		{
			_maitreSlot = -1;
			_maitreAttendRelache = true;
		}
		else if (Controls.IsGameControlPressed(Control.Block))
		{
			PacketSender.SendBeamRequest(_maitreSlot, charging: false, cancel: true, _maitreVisee);
			Maitres.AnnulerLocal(this);
			_maitreSlot = -1;
			_maitreAttendRelache = true;
		}
		else
		{
			ViserLAttaque();
			if (!MaitreToucheTenue())
			{
				RelacherLAttaque();
			}
		}
	}

	private bool TryRelacherMaitreAuClic()
	{
		if (this != Globals.Me || _maitreSlot < 0 || Maitres.ChargeDe(this) == TechniqueDeMaitre.Aucune)
		{
			return false;
		}
		float? num = BeamAimFromMouse();
		if (num.HasValue)
		{
			float valueOrDefault = num.GetValueOrDefault();
			_maitreVisee = valueOrDefault;
			FaceBeam(_maitreVisee);
			Maitres.Viser(this, _maitreVisee);
		}
		RelacherLAttaque();
		return true;
	}

	private void RelacherLAttaque()
	{
		PacketSender.SendBeamRequest(_maitreSlot, charging: false, cancel: false, _maitreVisee);
		_maitreSlot = -1;
		_maitreAttendRelache = true;
	}

	private void ViserLAttaque()
	{
		long milliseconds = Timing.Global.Milliseconds;
		long num = Math.Clamp(milliseconds - _maitreViseeA, 0L, 100L);
		_maitreViseeA = milliseconds;
		float? num2 = BeamAimFromStick() ?? BeamAimFromMouse();
		if (num2.HasValue)
		{
			float valueOrDefault = num2.GetValueOrDefault();
			float value = KiBeam.DeltaAngle(_maitreVisee, valueOrDefault);
			float num3 = 300f * (float)num / 1000f;
			_maitreVisee = KiBeam.Normalize(_maitreVisee + Math.Clamp(value, 0f - num3, num3));
		}
		Maitres.Viser(this, _maitreVisee);
		FaceBeam(_maitreVisee);
		if (milliseconds - _maitreViseeEnvoyeeA >= 100 && MathF.Abs(KiBeam.DeltaAngle(_maitreViseeEnvoyee, _maitreVisee)) >= 2f)
		{
			_maitreViseeEnvoyeeA = milliseconds;
			_maitreViseeEnvoyee = _maitreVisee;
			PacketSender.SendBeamRequest(_maitreSlot, charging: true, cancel: false, _maitreVisee);
		}
	}

	private bool MaitreToucheTenue()
	{
		for (int i = 0; i < Hotbar.Length; i++)
		{
			Guid? guid = Hotbar[i]?.ItemOrSpellId;
			Guid maitreSortId = _maitreSortId;
			if (guid.HasValue && guid.GetValueOrDefault() == maitreSortId && Controls.IsControlPressed((Control)(4096 + i + 1)))
			{
				return true;
			}
		}
		return false;
	}

	public bool TryMenuDuJoueurAuCurseur()
	{
		if (base.IsBlocking || base.IsCharging || IsBeaming || base.IsTransforming || Intersect.Client.Interface.Interface.DoesMouseHitInterface())
		{
			return false;
		}
		Vector2 vector = Graphics.ConvertToWorldPoint(Globals.InputManager.GetMousePosition());
		Player player = null;
		foreach (Entity value in Globals.Entities.Values)
		{
			if (value is Player player2 && player2 != this && !(player2.MapId == Guid.Empty) && !player2.IsHidden && (!player2.IsStealthed || IsInMyParty(player2)) && !player2.CanBeAttacked && player2.WorldPos.Contains(vector.X, vector.Y) && (player == null || player2.WorldPos.Bottom > player.WorldPos.Bottom))
			{
				player = player2;
			}
		}
		if (player == null)
		{
			return false;
		}
		if (TargetId != player.Id)
		{
			Target = player;
		}
		_appuiRendu = true;
		Intersect.Client.Interface.Interface.GameUi.TargetContextMenu.ToggleHidden(player);
		return true;
	}

	private void ReleverLesMonstresAuPixel(float x, float y, bool vol)
	{
		_libreMonstres.Clear();
		if (vol)
		{
			return;
		}
		foreach (Entity value in Globals.Entities.Values)
		{
			if (value != null && value != this && value.EstMonstreAuPixel && !value.IsDying && !value.Passable && value.Z == base.Z && value.PositionMonde(out var mondeX, out var mondeY) && !(MathF.Abs(mondeX - x) > 120f) && !(MathF.Abs(mondeY - y) > 120f))
			{
				_libreMonstres.Add((mondeX, mondeY + 6f, value.RayonDuCorps));
			}
		}
	}

	private void SortirDesMonstresEnDouceur(ref float x, ref float y, float ecoule)
	{
		if (_libreMonstres.Count == 0 || ecoule <= 0f)
		{
			return;
		}
		float num = y + 6f;
		float num2 = 0f;
		float num3 = 0f;
		float num4 = 0f;
		foreach (var libreMonstre in _libreMonstres)
		{
			float item = libreMonstre.X;
			float item2 = libreMonstre.PiedsY;
			float item3 = libreMonstre.Rayon;
			float num5 = x - item;
			float num6 = num - item2;
			float num7 = 10f + item3;
			float num8 = num5 * num5 + num6 * num6;
			if (!(num8 >= num7 * num7))
			{
				float num9 = MathF.Sqrt(num8);
				if (num9 < 0.01f)
				{
					num6 = 1f;
					num5 = 0f;
				}
				else
				{
					num5 /= num9;
					num6 /= num9;
				}
				float num10 = num7 - num9;
				num2 += num5 * num10;
				num3 += num6 * num10;
				num4 = MathF.Max(num4, num10);
			}
		}
		float num11 = MathF.Sqrt(num2 * num2 + num3 * num3);
		if (!(num4 <= 0.05f) && !(num11 < 0.01f))
		{
			float num12 = MathF.Min(num4, 240f * ecoule / 1000f);
			(float X, float Y) tuple = CorpsLibre.Deplacer(x, num, num2 / num11 * num12, num3 / num11 * num12, TileWidth, TileHeight, BloquePourMoi, 10f, 0f, ProfondeurDeLEau, LignesDuMonde);
			float item4 = tuple.X;
			float item5 = tuple.Y;
			x = item4;
			y = item5 - 6f;
		}
	}

	private (float X, float Y) ContournerLesMonstresAuPixel(float departX, float departY, float x, float y)
	{
		if (_libreMonstres.Count == 0)
		{
			return (X: x, Y: y);
		}
		float num = departY + 6f;
		float num2 = y + 6f;
		bool flag = false;
		foreach (var libreMonstre in _libreMonstres)
		{
			float item = libreMonstre.X;
			float item2 = libreMonstre.PiedsY;
			float item3 = libreMonstre.Rayon;
			float num3 = 10f + item3;
			float num4 = x - item;
			float num5 = num2 - item2;
			float num6 = MathF.Sqrt(num4 * num4 + num5 * num5);
			if (num6 >= num3)
			{
				continue;
			}
			float num7 = departX - item;
			float num8 = num - item2;
			float num9 = MathF.Min(num3, MathF.Sqrt(num7 * num7 + num8 * num8));
			if (num6 >= num9 - 0.001f)
			{
				continue;
			}
			if (num6 < 0.01f)
			{
				float num10 = MathF.Sqrt(num7 * num7 + num8 * num8);
				if (!(num10 < 0.01f))
				{
					float num11 = num7 / num10;
					float num12 = num8 / num10;
					num5 = num12;
					num4 = num11;
				}
				else
				{
					float num12 = 1f;
					num5 = num12;
					num4 = 0f;
				}
			}
			else
			{
				num4 /= num6;
				num5 /= num6;
			}
			x = item + num4 * num9;
			num2 = item2 + num5 * num9;
			flag = true;
		}
		if (!flag)
		{
			return (X: x, Y: y);
		}
		if (CorpsLibre.Chevauche(x, num2, 9.5f, TileWidth, TileHeight, BloquePourMoi, LignesDuMonde))
		{
			return (X: departX, Y: departY);
		}
		return (X: x, Y: num2 - 6f);
	}

	internal float NamePlateBottom()
	{
		float num = GetTop() - 6f;
		if (ShouldDrawHpBar)
		{
			num -= (float)(GetBoundingHpBarTexture().Height + 2);
		}
		return num;
	}

	internal Color NamePlateColor()
	{
		Color result = KarmaDisplay.NameColors(KarmaTier).Name ?? Intersect.Color.White;
		Color nameColor = base.NameColor;
		if ((object)nameColor == null || nameColor.A <= 0)
		{
			return result;
		}
		return nameColor;
	}

	public void SetOrnaments(Guid[]? ornamentIds, Guid activeOrnamentId)
	{
		OrnamentIds = ornamentIds ?? Array.Empty<Guid>();
		OrnamentId = activeOrnamentId;
	}

	private int RechargeDuDashPassif(int millisecondes)
	{
		return PassifsDeClasse.RechargeDuDash(Passif, millisecondes);
	}

	public double PreciseChance(int profession)
	{
		if (!Professions.IsCrafting(profession))
		{
			return 0.0;
		}
		return Math.Clamp(ProfessionPreciseBase + ProfessionPrecisePerLevel * (double)ProfessionLevel(profession), 0.0, 100.0);
	}

	public double ProfessionFirstBonusPerLevel(int profession)
	{
		if (profession == 4)
		{
			return ProfessionAbundancePerLevel;
		}
		if (Professions.IsCrafting(profession))
		{
			return ProfessionPrecisePerLevel;
		}
		return ProfessionSpeedPerLevel;
	}

	public int ProfessionLevel(int profession)
	{
		if (!Professions.IsValid(profession))
		{
			return 0;
		}
		return ProfessionLevels[profession];
	}

	public void SetProfessions(PlayerProfessionsPacket packet)
	{
		int[] professionLevels = ProfessionLevels;
		bool flag = ProfessionsVersion == 0;
		ProfessionLevels = Copy(packet.Levels, 1);
		ProfessionExperience = Copy(packet.Experience, 0L);
		ProfessionNextLevel = Copy(packet.NextLevel, 0L);
		ProfessionMaximumLevel = Math.Max(1, packet.MaximumLevel);
		ProfessionSpeedPerLevel = packet.SpeedPerLevel;
		ProfessionRarityPerLevel = packet.RarityPerLevel;
		ProfessionAbundancePerLevel = packet.AbundancePerLevel;
		ProfessionPreciseBase = packet.PreciseBase;
		ProfessionPrecisePerLevel = packet.PrecisePerLevel;
		ProfessionCraftExperiencePerSecond = packet.CraftExperiencePerSecond;
		ProfessionsVersion++;
		int gainedProfession = packet.GainedProfession;
		if (!flag && Professions.IsValid(gainedProfession) && packet.Gained > 0)
		{
			LootFeedback.OnProfessionExperience(gainedProfession, packet.Gained);
			if (ProfessionLevels[gainedProfession] > professionLevels[gainedProfession])
			{
				ProfessionLevelUp.Begin(this, gainedProfession, ProfessionLevels[gainedProfession]);
			}
		}
	}

	private static T[] Copy<T>(T[]? values, T fallback)
	{
		T[] array = new T[9];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = ((values != null && i < values.Length) ? values[i] : fallback);
		}
		return array;
	}

	public void NoterUnLacher()
	{
		_dernierLacher = Timing.Global.Milliseconds;
	}

	private void MajRamassageAuto()
	{
		if (this != Globals.Me || base.IsDying || IsBusy || Globals.MoveRouteActive || base.FlightMode != FlightMode.None || !PositionMonde(out var mondeX, out var mondeY))
		{
			return;
		}
		long milliseconds = Timing.Global.Milliseconds;
		float num = mondeY + 6f;
		int tileWidth = Options.Instance.Map.TileWidth;
		int tileHeight = Options.Instance.Map.TileHeight;
		int mapWidth = Options.Instance.Map.MapWidth;
		int num2 = (int)MathF.Floor(mondeX / (float)tileWidth);
		int num3 = (int)MathF.Floor(num / (float)tileHeight);
		if (_objetsConnus.Count > 400)
		{
			_objetsConnus.Clear();
			_ramassageEssais.Clear();
		}
		for (int i = -1; i <= 1; i++)
		{
			for (int j = -1; j <= 1; j++)
			{
				int x;
				int y;
				Guid guid = CarteDeLaCase(num2 + j, num3 + i, out x, out y);
				if (guid == Guid.Empty || !Intersect.Client.Maps.MapInstance.TryGet(guid, out MapInstance instance))
				{
					continue;
				}
				int num4 = y * mapWidth + x;
				if (!instance.MapItems.TryGetValue(num4, out List<IMapItemInstance> value) || value.Count == 0)
				{
					continue;
				}
				float num5 = (float)(instance.X + x * tileWidth) + (float)tileWidth / 2f;
				float num6 = (float)(instance.Y + y * tileHeight) + (float)tileHeight / 2f;
				float num7 = MathF.Sqrt((num5 - mondeX) * (num5 - mondeX) + (num6 - num) * (num6 - num));
				IMapItemInstance[] array = value.ToArray();
				foreach (IMapItemInstance mapItemInstance in array)
				{
					if (_objetsConnus.Add(mapItemInstance.Id) && milliseconds - _dernierLacher < 1500 && num7 < 36f)
					{
						_objetsJetesParMoi.Add(mapItemInstance.Id);
					}
					if (num7 > 36f || _objetsJetesParMoi.Contains(mapItemInstance.Id) || (_ramassageEssais.TryGetValue(mapItemInstance.Id, out var value2) && milliseconds - value2 < 1200))
					{
						continue;
					}
					if (!PeutRecevoir(mapItemInstance.ItemId))
					{
						if (milliseconds - _dernierAvertissementSacPlein > 6000)
						{
							_dernierAvertissementSacPlein = milliseconds;
							ChatboxMsg.AddMessage(new ChatboxMsg(Strings.Housing.ResultInventoryFull, CustomColors.Alerts.Error, ChatMessageType.Inventory));
						}
					}
					else
					{
						_ramassageEssais[mapItemInstance.Id] = milliseconds;
						PacketSender.SendPickupItem(guid, num4, mapItemInstance.Id);
					}
				}
			}
		}
	}

	private bool PeutRecevoir(Guid objet)
	{
		if (objet == CurrencyItemId || ObjetsDansLeSac() < CapaciteDuSac)
		{
			return true;
		}
		if (!DatabaseObject<ItemDescriptor>.TryGet(objet, out var databaseObject) || !databaseObject.IsStackable)
		{
			return false;
		}
		for (int i = 0; i < base.Inventory.Length; i++)
		{
			IItem item = base.Inventory[i];
			if (item != null && item.ItemId == objet && item.Quantity < databaseObject.MaxInventoryStack && Array.IndexOf(base.MyEquipment, i) < 0)
			{
				return true;
			}
		}
		return false;
	}

	public bool TryRecolterAuClic()
	{
		if (this != Globals.Me || !EnDeplacementLibre || base.IsDying || Globals.MoveRouteActive)
		{
			return false;
		}
		Resource resource = null;
		foreach (Entity value in Globals.Entities.Values)
		{
			if (value is Resource resource2 && value.IsHovered && !resource2.IsDead)
			{
				resource = resource2;
				break;
			}
		}
		if (resource == null)
		{
			return false;
		}
		AnnulerLaRecolteAuClic();
		if (Recolte.NiveauTropBas(resource, this, avertir: true))
		{
			_recolteClicTenu = true;
			return true;
		}
		if (!TrouverLeChemin(resource))
		{
			_recolteClicTenu = true;
			return true;
		}
		_recolteCible = resource;
		resource.CibleDeRecolteAuClic = true;
		_recolteEtape = EtapeDeRecolteAuClic.EnRoute;
		_recolteEchecs = 0;
		_recolteClicTenu = true;
		_recolteDernierProgres = Timing.Global.Milliseconds;
		PositionMonde(out _recolteDerniereX, out _recolteDerniereY);
		return true;
	}

	public void AnnulerLaRecolteAuClic()
	{
		if (_recolteCible != null)
		{
			_recolteCible.CibleDeRecolteAuClic = false;
		}
		_recolteCible = null;
		_recolteChemin.Clear();
		_recolteEtape = EtapeDeRecolteAuClic.Aucune;
	}

	private bool RecolteAuClicAbsorbeLeCoup()
	{
		if (!_recolteClicTenu)
		{
			return false;
		}
		if (!Controls.IsGameControlPressed(Control.AttackInteract))
		{
			_recolteClicTenu = false;
			return false;
		}
		return true;
	}

	private void MajRecolteAuClic()
	{
		if (_recolteClicTenu && !Controls.IsGameControlPressed(Control.AttackInteract))
		{
			_recolteClicTenu = false;
		}
		if (!EnRecolteAuClic)
		{
			return;
		}
		Resource recolteCible = _recolteCible;
		if (recolteCible == null || recolteCible.IsDead || recolteCible.MapInstance == null || !Globals.Entities.ContainsKey(recolteCible.Id) || base.IsDying || !EnDeplacementLibre || Globals.MoveRouteActive || IsBusy || EstEntraveLibrement())
		{
			AnnulerLaRecolteAuClic();
			return;
		}
		if (GetInputDirection() != Direction.None || Controls.IsGameControlPressed(Control.Dodge) || Controls.IsGameControlPressed(Control.Block) || Controls.IsGameControlPressed(Control.ChargeKi) || (!_recolteClicTenu && Controls.IsGameControlPressed(Control.AttackInteract)))
		{
			AnnulerLaRecolteAuClic();
			return;
		}
		long milliseconds = Timing.Global.Milliseconds;
		if (_recolteEtape == EtapeDeRecolteAuClic.EnRoute)
		{
			if (!PositionMonde(out var mondeX, out var mondeY))
			{
				AnnulerLaRecolteAuClic();
				return;
			}
			while (_recolteChemin.Count > 0 && Distance(mondeX, mondeY + 6f, _recolteChemin[0]) < 5f)
			{
				_recolteChemin.RemoveAt(0);
			}
			if (Math.Abs(mondeX - _recolteDerniereX) + Math.Abs(mondeY - _recolteDerniereY) > 0.5f)
			{
				_recolteDerniereX = mondeX;
				_recolteDerniereY = mondeY;
				_recolteDernierProgres = milliseconds;
			}
			else if (_recolteChemin.Count > 0 && milliseconds - _recolteDernierProgres > 1500)
			{
				AnnulerLaRecolteAuClic();
				return;
			}
			if (_recolteChemin.Count == 0)
			{
				SeTournerVers(recolteCible);
				_recolteEtape = EtapeDeRecolteAuClic.Recolte;
				_recolteDernierProgres = milliseconds;
			}
		}
		else if (!base.IsAttacking && !base.IsMoving)
		{
			SeTournerVers(recolteCible);
			if (TryAttack())
			{
				_recolteEchecs = 0;
			}
			else if (++_recolteEchecs >= 3)
			{
				AnnulerLaRecolteAuClic();
			}
		}
	}

	private (float X, float Y) DirectionDeRecolteAuClic()
	{
		if (_recolteEtape != EtapeDeRecolteAuClic.EnRoute || _recolteChemin.Count == 0 || !PositionMonde(out var mondeX, out var mondeY))
		{
			return (X: 0f, Y: 0f);
		}
		(float X, float Y) tuple = _recolteChemin[0];
		float item = tuple.X;
		float item2 = tuple.Y;
		float num = item - mondeX;
		float num2 = item2 - (mondeY + 6f);
		float num3 = MathF.Sqrt(num * num + num2 * num2);
		if (!(num3 < 0.001f))
		{
			return (X: num / num3, Y: num2 / num3);
		}
		return (X: 0f, Y: 0f);
	}

	private void SeTournerVers(Resource cible)
	{
		if (PositionMonde(out var mondeX, out var mondeY) && cible.PositionMonde(out var mondeX2, out var mondeY2))
		{
			Direction direction = DirectionDuVecteur(mondeX2 - mondeX, mondeY2 - mondeY);
			if (direction != Direction.None && direction != base.DirectionFacing)
			{
				PacketSender.SendDirection(direction);
				PickLastDirection(direction);
			}
		}
	}

	private static float Distance(float x, float y, (float X, float Y) point)
	{
		float num = point.X - x;
		float num2 = point.Y - y;
		return MathF.Sqrt(num * num + num2 * num2);
	}

	private bool TrouverLeChemin(Resource cible)
	{
		_recolteChemin.Clear();
		if (!PositionMonde(out var mondeX, out var mondeY) || !(cible.MapInstance is MapInstance mapInstance))
		{
			return false;
		}
		int tileWidth = Options.Instance.Map.TileWidth;
		int tileHeight = Options.Instance.Map.TileHeight;
		int num = (int)MathF.Floor(mondeX / (float)tileWidth);
		int num2 = (int)MathF.Floor((mondeY + 6f) / (float)tileHeight);
		int num3 = mapInstance.GridX * Options.Instance.Map.MapWidth + cible.X;
		int num4 = mapInstance.GridY * Options.Instance.Map.MapHeight + cible.Y;
		ResourceDescriptor? descriptor = cible.Descriptor;
		bool flag = descriptor != null && descriptor.Tool == 3;
		HashSet<(int, int)> hashSet = new HashSet<(int, int)>();
		if (flag)
		{
			for (int i = -3; i <= 3; i++)
			{
				for (int j = -3; j <= 3; j++)
				{
					if ((j != 0 || i != 0) && j * j + i * i <= 10 && CaseLibrePourLaRecolte(num3 + j, num4 + i))
					{
						hashSet.Add((num3 + j, num4 + i));
					}
				}
			}
		}
		else
		{
			(int, int)[] array = new(int, int)[4]
			{
				(0, 1),
				(-1, 0),
				(1, 0),
				(0, -1)
			};
			for (int k = 0; k < array.Length; k++)
			{
				var (num5, num6) = array[k];
				if (CaseLibrePourLaRecolte(num3 + num5, num4 + num6))
				{
					hashSet.Add((num3 + num5, num4 + num6));
				}
			}
		}
		if (hashSet.Count == 0)
		{
			return false;
		}
		if (hashSet.Contains((num, num2)))
		{
			return true;
		}
		Dictionary<(int, int), (int, int)> dictionary = new Dictionary<(int, int), (int, int)>();
		Dictionary<(int, int), int> dictionary2 = new Dictionary<(int, int), int> { [(num, num2)] = 0 };
		PriorityQueue<(int, int), int> priorityQueue = new PriorityQueue<(int, int), int>();
		priorityQueue.Enqueue((num, num2), 0);
		(int, int)? tuple2 = null;
		int num7 = 0;
		while (priorityQueue.Count > 0 && num7++ < 6000)
		{
			(int, int) tuple3 = priorityQueue.Dequeue();
			if (hashSet.Contains(tuple3))
			{
				tuple2 = tuple3;
				break;
			}
			int num8 = dictionary2[tuple3];
			for (int l = -1; l <= 1; l++)
			{
				for (int m = -1; m <= 1; m++)
				{
					if (m == 0 && l == 0)
					{
						continue;
					}
					(int, int) tuple4 = (tuple3.Item1 + m, tuple3.Item2 + l);
					if (CaseLibrePourLaRecolte(tuple4.Item1, tuple4.Item2) && (m == 0 || l == 0 || (CaseLibrePourLaRecolte(tuple3.Item1 + m, tuple3.Item2) && CaseLibrePourLaRecolte(tuple3.Item1, tuple3.Item2 + l))) && !LigneBarreLePas(tuple3.Item1, tuple3.Item2, m, l))
					{
						int num9 = num8 + ((m != 0 && l != 0) ? 14 : 10);
						if (!dictionary2.TryGetValue(tuple4, out var value) || value > num9)
						{
							dictionary2[tuple4] = num9;
							dictionary[tuple4] = tuple3;
							int num10 = 10 * Math.Max(Math.Abs(tuple4.Item1 - num3), Math.Abs(tuple4.Item2 - num4));
							priorityQueue.Enqueue(tuple4, num9 + num10);
						}
					}
				}
			}
		}
		if (tuple2.HasValue)
		{
			(int, int) valueOrDefault = tuple2.GetValueOrDefault();
			List<(int, int)> list = new List<(int, int)>();
			(int, int) tuple5 = valueOrDefault;
			while (true)
			{
				(int, int) key = tuple5;
				int num11 = num;
				int num12 = num2;
				if (key.Item1 == num11 && key.Item2 == num12)
				{
					break;
				}
				list.Add(tuple5);
				tuple5 = dictionary[tuple5];
			}
			list.Reverse();
			foreach (var (num13, num14) in list)
			{
				_recolteChemin.Add(((float)(num13 * tileWidth) + (float)tileWidth / 2f, (float)(num14 * tileHeight) + (float)tileHeight / 2f));
			}
			return true;
		}
		return false;
	}

	private bool CaseLibrePourLaRecolte(int caseMondeX, int caseMondeY)
	{
		if (caseMondeX < 0 || caseMondeY < 0 || BloquePourMoi(caseMondeX, caseMondeY))
		{
			return false;
		}
		int tileWidth = Options.Instance.Map.TileWidth;
		int tileHeight = Options.Instance.Map.TileHeight;
		return ProfondeurDeLEau((float)(caseMondeX * tileWidth) + (float)tileWidth / 2f, (float)(caseMondeY * tileHeight) + (float)tileHeight / 2f) <= 0f;
	}

	public int ObjetsDansLeSac()
	{
		int num = 0;
		for (int i = 0; i < base.Inventory.Length; i++)
		{
			IItem item = base.Inventory[i];
			if (item != null && item.ItemId != Guid.Empty && Array.IndexOf(base.MyEquipment, i) < 0)
			{
				num++;
			}
		}
		return num;
	}

	public bool HasPassedTrial(TeachingTrialInfo? trial)
	{
		if (trial != null)
		{
			return PassedTrials.ContainsKey(trial.Id);
		}
		return false;
	}

	public void SetTeachings(TeachingsPacket packet)
	{
		TeachingMain = (Enum.IsDefined((TeachingMaster)packet.Main) ? ((TeachingMaster)packet.Main) : TeachingMaster.None);
		TeachingSecondary = (Enum.IsDefined((TeachingMaster)packet.Secondary) ? ((TeachingMaster)packet.Secondary) : TeachingMaster.None);
		TeachingRanks = packet.Ranks ?? new Dictionary<string, int>();
		PassedTrials = packet.Trials ?? new Dictionary<string, bool>();
		TeachingsVersion++;
	}

	public void SetTitles(Guid[]? titleIds, Guid activeTitleId)
	{
		TitleIds = titleIds ?? Array.Empty<Guid>();
		TitleId = activeTitleId;
	}
}
