using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Numerics;
using Intersect.Client.Core;
using Intersect.Client.Dungeons;
using Intersect.Client.Entities.Events;
using Intersect.Client.Entities.Projectiles;
using Intersect.Client.Framework.Content;
using Intersect.Client.Framework.Entities;
using Intersect.Client.Framework.File_Management;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.Framework.Items;
using Intersect.Client.Framework.Maps;
using Intersect.Client.General;
using Intersect.Client.Housing;
using Intersect.Client.Interface;
using Intersect.Client.Interface.Game;
using Intersect.Client.Items;
using Intersect.Client.Localization;
using Intersect.Client.Maps;
using Intersect.Client.Networking;
using Intersect.Client.Spells;
using Intersect.Config;
using Intersect.Core;
using Intersect.Enums;
using Intersect.Framework.Core;
using Intersect.Framework.Core.Entities;
using Intersect.Framework.Core.GameObjects.Animations;
using Intersect.Framework.Core.GameObjects.Items;
using Intersect.Framework.Core.GameObjects.Maps;
using Intersect.Framework.Core.GameObjects.Maps.Attributes;
using Intersect.Framework.Core.GameObjects.NPCs;
using Intersect.Framework.Core.GameObjects.PlayerClass;
using Intersect.Framework.Core.GameObjects.Resources;
using Intersect.Framework.Core.Sprites;
using Intersect.GameObjects;
using Intersect.Models;
using Intersect.Network.Packets.Server;
using Intersect.Utilities;
using Microsoft.Extensions.Logging;

namespace Intersect.Client.Entities;

public class Entity : IEntity, IDisposable
{
	private readonly record struct InstantaneLibre(double Temps, float MondeX, float MondeY, Direction Direction, byte Etat);

	protected readonly int MapWidth = Options.Instance.Map.MapWidth;

	protected readonly int MapHeight = Options.Instance.Map.MapHeight;

	protected readonly int TileWidth = Options.Instance.Map.TileWidth;

	protected readonly int TileHeight = Options.Instance.Map.TileHeight;

	private readonly List<Animation> _animations = new List<Animation>();

	private readonly Dictionary<AnimationSource, Animation> _animationsBySource = new Dictionary<AnimationSource, Animation>();

	private bool _isCharging;

	protected readonly bool EnableTurnAroundWhileCasting = Options.Instance.Combat.EnableTurnAroundWhileCasting;

	private Guid[] _equipment = new Guid[Options.Instance.Equipment.Slots.Count];

	protected Vector2 mOrigin = Vector2.Zero;

	private readonly List<ChatBubble> mChatBubbles = new List<ChatBubble>();

	private Direction _directionFacing;

	protected bool mDisposed;

	private double mLastUpdate;

	protected string _sprite = string.Empty;

	protected byte mRenderPriority = 1;

	protected string mTransformedSprite = string.Empty;

	private float _walkedSinceFrame;

	private long _lastStepMilliseconds;

	private const float WalkCycleTiles = 2f;

	private long _lastWalkFrameAt;

	private long _runPoseSince;

	private float _runLean;

	private (int X, int Y) _runShift;

	private const long WalkSettleDelay = 90L;

	private const float RemoteStepStretch = 1.2f;

	private Guid _spellCast;

	private readonly Dictionary<SpriteAnimations, List<SpriteSheetAnimation>> _sheetVariants = new Dictionary<SpriteAnimations, List<SpriteSheetAnimation>>();

	private readonly Dictionary<SpriteAnimations, SpriteSheetAnimation> _sheetBands = new Dictionary<SpriteAnimations, SpriteSheetAnimation>();

	private Vector3 _position;

	protected float ResorptionX = 1f;

	protected float ResorptionY = 1f;

	private Direction _lastDirection = Direction.Down;

	private int _immersion;

	private FloatRect? _bodyDestination;

	private string? _bodyTextureName;

	private long _variantsRolledForAttackTimer = -1L;

	private Guid _variantsRolledForSpellCast = Guid.Empty;

	private bool _wasIdle;

	private bool _wasAfk;

	private long _lastMovementTime = -1L;

	private long _remoteStepAt = -1L;

	private float _remoteStepLateMs;

	private float _remoteStepDistance;

	private long _spellPoseStartedAt;

	private readonly List<InstantaneLibre> _instantanesLibres = new List<InstantaneLibre>(12);

	private long _instantaneLibreRecuA;

	private const float CorrectionLibreMs = 60f;

	private const float CorrectionLibreMaxPx = 48f;

	private float _correctionLibreX;

	private float _correctionLibreY;

	private double _correctionLibreA;

	private bool _recentrageLibre;

	private CoupHeritage _hCoup;

	private long _hFinDuCoup;

	private int _hAvance;

	private Guid _hCibleId;

	private int _hRepliCleEpaule;

	private int _hRepliCleMontant;

	private Heritage.RepliDuCoup _hRepliEpaule;

	private Heritage.RepliDuCoup _hRepliMontant;

	private bool _hVolTenu;

	private List<string>? _hCalques;

	private Heritage.Poussee.Decalage _hPoussee;

	private float _hPousseeX;

	private float _hPousseeY;

	private long _hPousseeA;

	protected long _horsDeVueDepuis;

	public byte PasDuCoup { get; set; }

	public int DureeDuCoupLocal { get; set; }

	public int AnimationFrame { get; set; }

	public long AnimationTimer { get; set; }

	public long AttackTimer { get; set; }

	public long HitFlashUntil { get; set; }

	public long HitWhiteUntil { get; set; }

	public long DyingAt { get; set; }

	public bool IsDying => DyingAt > 0;

	public long DeathDurationMs { get; set; }

	public float KiKillHeadingX { get; set; }

	public float KiKillHeadingY { get; set; }

	public int GuardPercent { get; set; } = 100;

	public long GuardStunnedUntil { get; set; }

	public long GuardLockedUntil { get; set; }

	public long GuardFlashUntil { get; set; }

	public long GuardBreakUntil { get; set; }

	public long GuardShownUntil { get; set; }

	public long GuardHitAt { get; set; }

	public float GuardBlend { get; set; }

	public long GuardBlendAt { get; set; }

	public long HitShakeAt { get; set; }

	public float HitRecoilX { get; set; }

	public float HitRecoilY { get; set; }

	public bool IsGuardStunned => Timing.Global.Milliseconds < GuardStunnedUntil;

	public long TransformingUntil { get; set; }

	public bool IsTransforming => Timing.Global.Milliseconds < TransformingUntil;

	public int AttackTime { get; set; } = -1;

	public long CastTime { get; set; }

	public int CastDuration { get; set; }

	public bool IsAttacking => AttackTimer > Timing.Global.Milliseconds;

	public bool IsBlocking { get; set; }

	public bool IsSprinting { get; set; }

	public FlightMode FlightMode { get; set; }

	public bool IsCharging
	{
		get
		{
			return _isCharging;
		}
		set
		{
			if (_isCharging != value)
			{
				_isCharging = value;
				if (_isCharging)
				{
					KiCharge.Start(this);
				}
				else
				{
					KiCharge.Stop(this);
				}
			}
		}
	}

	public bool IsCasting => CastTime > Timing.Global.Milliseconds;

	public bool CanTurnAround
	{
		get
		{
			if (!IsMoving)
			{
				if (IsCasting)
				{
					return EnableTurnAroundWhileCasting;
				}
				return true;
			}
			return false;
		}
	}

	public bool IsDashing => Dashing != null;

	public Dash? Dashing { get; set; }

	public IDash? CurrentDash => Dashing;

	public Queue<Dash> DashQueue { get; set; } = new Queue<Dash>();

	public EntityMovePacket? PendingDashMove { get; set; }

	public long DashTimer { get; set; }

	public float elapsedtime { get; set; }

	public bool HasAnimations => _animations.Count > 0;

	public Guid[] Equipment
	{
		get
		{
			return _equipment;
		}
		set
		{
			if (_equipment != value)
			{
				_equipment = value;
				LoadAnimationTexture(string.IsNullOrWhiteSpace(TransformedSprite) ? Sprite : Transformation.TextureNameFor(Sprite, TransformedSprite), SpriteAnimations.Weapon);
			}
		}
	}

	IReadOnlyList<int> IEntity.EquipmentSlots => new global::_003C_003Ez__ReadOnlyArray<int>(new ReadOnlySpan<int>(MyEquipment).ToArray());

	public Animation?[] EquipmentAnimations { get; set; } = new Animation[Options.Instance.Equipment.Slots.Count];

	public string Face { get; set; } = string.Empty;

	public Label FooterLabel { get; set; } = new Label(string.Empty, Intersect.Color.White);

	public Gender Gender { get; set; }

	public Label HeaderLabel { get; set; } = new Label(string.Empty, Intersect.Color.White);

	public bool IsHidden { get; set; }

	public bool HideName { get; set; }

	public Guid Id { get; set; }

	public IItem[] Inventory { get; } = new IItem[Options.Instance.Player.MaxInventory];

	IReadOnlyList<IItem> IEntity.Items => new global::_003C_003Ez__ReadOnlyArray<IItem>(new ReadOnlySpan<IItem>(Inventory).ToArray());

	public bool InView { get; set; } = true;

	public bool IsMoving { get; set; }

	public IMapInstance? LatestMap { get; set; }

	public int Level { get; set; } = 1;

	public long[] MaxVital { get; set; } = new long[Enum.GetValues<Vital>().Length];

	IReadOnlyDictionary<Vital, long> IEntity.MaxVitals => Enum.GetValues<Vital>().ToDictionary((Vital vital) => vital, (Vital vital) => MaxVital[(int)vital]);

	protected float TempsDepuisLaDerniereImage
	{
		get
		{
			if (mLastUpdate != 0.0)
			{
				return (float)Math.Clamp(DeplacementLibre.MaintenantPrecis - mLastUpdate, 0.0, 50.0);
			}
			return 0f;
		}
	}

	public Color Color { get; set; } = new Color(255, 255, 255, 255);

	public virtual Direction DirectionMoving { get; set; } = Direction.None;

	public long MoveTimer { get; set; }

	public int[] MyEquipment { get; set; } = new int[Options.Instance.Equipment.Slots.Count];

	public string Name { get; set; } = string.Empty;

	public Color? NameColor { get; set; }

	public bool Passable { get; set; }

	public HashSet<Entity>? RenderList { get; set; }

	public Guid SpellCast
	{
		get
		{
			return _spellCast;
		}
		set
		{
			if (!(value == SpellCast))
			{
				_spellCast = value;
				LoadAnimationTexture(string.IsNullOrWhiteSpace(TransformedSprite) ? Sprite : Transformation.TextureNameFor(Sprite, TransformedSprite), SpriteAnimations.Cast);
			}
		}
	}

	public Spell[] Spells { get; set; } = new Spell[Options.Instance.Player.MaxSpells];

	IReadOnlyList<Guid> IEntity.Spells => Spells.Select((Spell x) => x.Id).ToList();

	public int[] Stat { get; set; } = new int[Enum.GetValues<Stat>().Length];

	IReadOnlyDictionary<Stat, int> IEntity.Stats => Enum.GetValues<Stat>().ToDictionary((Stat stat) => stat, (Stat stat) => Stat[(int)stat]);

	public IGameTexture? Texture { get; set; }

	public SpriteAnimations SpriteAnimation { get; set; }

	public Dictionary<SpriteAnimations, IGameTexture> AnimatedTextures { get; set; } = new Dictionary<SpriteAnimations, IGameTexture>();

	public Dictionary<SpriteAnimations, List<IGameTexture>> AnimationVariants { get; set; } = new Dictionary<SpriteAnimations, List<IGameTexture>>();

	protected SpriteSheet? Sheet { get; private set; }

	protected SpriteSheetAnimation? CurrentBand
	{
		get
		{
			object obj;
			if (Sheet != null)
			{
				if (_sheetBands.TryGetValue(SpriteAnimation, out SpriteSheetAnimation value))
				{
					return value;
				}
				obj = _sheetBands.GetValueOrDefault(SpriteAnimations.Normal);
				if (obj == null)
				{
					return _sheetBands.Values.FirstOrDefault();
				}
			}
			else
			{
				obj = null;
			}
			return (SpriteSheetAnimation?)obj;
		}
	}

	protected int BodyFrameHeight
	{
		get
		{
			SpriteSheet sheet = Sheet;
			if (sheet != null)
			{
				SpriteSheetAnimation spriteSheetAnimation = _sheetBands.GetValueOrDefault(SpriteAnimations.Normal) ?? _sheetBands.GetValueOrDefault(SpriteAnimations.Idle) ?? _sheetBands.Values.FirstOrDefault();
				if (spriteSheetAnimation != null)
				{
					return sheet.HeightOf(spriteSheetAnimation);
				}
				return sheet.FrameHeight;
			}
			return (Texture?.Height ?? 0) / Options.Instance.Sprites.Directions;
		}
	}

	protected int BodyTopMargin
	{
		get
		{
			if (Sheet == null && Texture != null)
			{
				return SpriteMeasure.TopMargin(Texture, Options.Instance.Sprites.NormalFrames, Options.Instance.Sprites.Directions);
			}
			return 0;
		}
	}

	protected int BodyHeight => Math.Max(1, BodyFrameHeight - BodyTopMargin);

	public FloatRect BodyWorldPos
	{
		get
		{
			int bodyTopMargin = BodyTopMargin;
			if (bodyTopMargin > 0)
			{
				return new FloatRect(WorldPos.X, WorldPos.Y + (float)bodyTopMargin, WorldPos.Width, Math.Max(1f, WorldPos.Height - (float)bodyTopMargin));
			}
			return WorldPos;
		}
	}

	public int SpriteFrame { get; set; }

	public long SpriteFrameTimer { get; set; } = -1L;

	public long LastActionTime { get; set; } = -1L;

	public long LastInputTime { get; set; } = -1L;

	private long DerniereActiviteAfk => Math.Max(LastActionTime, LastInputTime);

	public long AutoTurnToTargetTimer { get; set; } = -1L;

	public EntityType Type { get; }

	public NpcAggression Aggression { get; set; }

	public bool Bleeds { get; set; } = true;

	public int BossEnragePercent { get; set; }

	public BossVisual BossVisual { get; set; }

	public long[] Vital { get; set; } = new long[Enum.GetValues<Vital>().Length];

	IReadOnlyDictionary<Vital, long> IEntity.Vitals => Enum.GetValues<Vital>().ToDictionary((Vital vital) => vital, (Vital vital) => Vital[(int)vital]);

	public int WalkFrame { get; set; }

	public FloatRect WorldPos { get; set; }

	public bool IsHovered { get; set; }

	public Vector3 Position
	{
		get
		{
			return _position;
		}
		set
		{
			if (!(_position == value))
			{
				Vector3 position = _position;
				Vector3 vector = value - position;
				_position = value;
				OnPositionChanged(value, position);
			}
		}
	}

	public int TileX => (int)float.Floor(Position.X);

	public int TileY => (int)float.Floor(Position.Y);

	public int TileZ => (int)float.Floor(Position.Z);

	public byte X
	{
		get
		{
			return (byte)float.Floor(Position.X);
		}
		set
		{
			Vector3 position = Position;
			position.X = (int)value;
			Position = position;
		}
	}

	public byte Y
	{
		get
		{
			return (byte)float.Floor(Position.Y);
		}
		set
		{
			Vector3 position = Position;
			position.Y = (int)value;
			Position = position;
		}
	}

	public byte Z
	{
		get
		{
			return (byte)float.Floor(Position.Z);
		}
		set
		{
			Vector3 position = Position;
			position.Z = (int)value;
			Position = position;
		}
	}

	public float OffsetX { get; set; }

	public float OffsetY { get; set; }

	public float? FinishedStepOvershoot { get; set; }

	public List<IStatus> Status { get; private set; } = new List<IStatus>();

	IReadOnlyList<IStatus> IEntity.Status => Status;

	public Vector2 Origin
	{
		get
		{
			if (LatestMap != null)
			{
				return mOrigin;
			}
			return Vector2.Zero;
		}
	}

	protected virtual Vector2 CenterOffset
	{
		get
		{
			if (Texture != null)
			{
				if (Sheet == null)
				{
					return Vector2.UnitY * Texture.Center.Y / Options.Instance.Sprites.Directions;
				}
				return Vector2.UnitY * BodyFrameHeight / 2f;
			}
			return Vector2.Zero;
		}
	}

	public Vector2 Center => Origin - CenterOffset;

	public Direction DirectionFacing
	{
		get
		{
			return _directionFacing;
		}
		set
		{
			_directionFacing = (Direction)((int)(value + Options.Instance.Map.MovementDirections) % Options.Instance.Map.MovementDirections);
		}
	}

	public virtual string TransformedSprite
	{
		get
		{
			return mTransformedSprite;
		}
		set
		{
			if (!(mTransformedSprite == value))
			{
				mTransformedSprite = value;
				string textureName = (string.IsNullOrEmpty(mTransformedSprite) ? _sprite : Transformation.TextureNameFor(_sprite, mTransformedSprite));
				LoadTextures(textureName);
			}
		}
	}

	public virtual string Sprite
	{
		get
		{
			return _sprite;
		}
		set
		{
			if (!(_sprite == value))
			{
				_sprite = value;
				LoadTextures(_sprite);
			}
		}
	}

	public virtual int SpriteFrames => CurrentBand?.Frames ?? (SpriteAnimation switch
	{
		SpriteAnimations.Normal => Options.Instance.Sprites.NormalFrames, 
		SpriteAnimations.Idle => SheetColumns(AnimatedTextures.GetValueOrDefault(SpriteAnimations.Idle), Texture, Options.Instance.Sprites.IdleFrames), 
		SpriteAnimations.Attack => Options.Instance.Sprites.AttackFrames, 
		SpriteAnimations.Shoot => Options.Instance.Sprites.ShootFrames, 
		SpriteAnimations.Cast => Options.Instance.Sprites.CastFrames, 
		SpriteAnimations.Weapon => Options.Instance.Sprites.WeaponFrames, 
		SpriteAnimations.Run => Options.Instance.Sprites.RunFrames, 
		SpriteAnimations.Afk => Options.Instance.Sprites.AfkFrames, 
		SpriteAnimations.Ki => Options.Instance.Sprites.KiFrames, 
		SpriteAnimations.Block => Options.Instance.Sprites.BlockFrames, 
		SpriteAnimations.Transform => Options.Instance.Sprites.TransformFrames, 
		SpriteAnimations.Spell => SpellPoseColumns(), 
		SpriteAnimations.Levite => SheetColumns(AnimatedTextures.GetValueOrDefault(SpriteAnimations.Levite), Texture, 1), 
		SpriteAnimations.Eveil => SheetColumns(AnimatedTextures.GetValueOrDefault(SpriteAnimations.Eveil), Texture, 1), 
		_ => Options.Instance.Sprites.NormalFrames, 
	});

	public IMapInstance? MapInstance => Intersect.Client.Maps.MapInstance.Get(MapId);

	public virtual Guid MapId { get; set; }

	public bool IsDisposed => mDisposed;

	public float FlightSpeedBoost
	{
		get
		{
			switch (FlightMode)
			{
			case FlightMode.Cloud:
				if (IsSprinting)
				{
					return Math.Clamp(Options.Instance.Flight.CloudSprintSpeedBoost, 0f, 0.9f);
				}
				return Math.Clamp(Options.Instance.Flight.CloudSpeedBoost, 0f, 0.9f);
			case FlightMode.Ki:
				if (IsSprinting)
				{
					return Math.Clamp(Options.Instance.Flight.KiSprintSpeedBoost, 0f, 0.9f);
				}
				return Math.Clamp(Options.Instance.Flight.KiSpeedBoost, 0f, 0.9f);
			default:
				return 0f;
			}
		}
	}

	protected static float AttackMovementSlow => (float)Math.Clamp(Options.Instance.Combat.AttackMovementSlow, 0, 90) / 100f;

	protected static float BlockingSlow => (float)Math.Clamp(Options.Instance.Combat.BlockingSlow, 0, 90) / 100f;

	protected static float SprintSpeedBoost
	{
		get
		{
			if (!Options.Instance.Player.EnableSprinting)
			{
				return 0f;
			}
			return Math.Clamp(Options.Instance.Player.SprintSpeedBoost, 0f, 0.9f);
		}
	}

	public virtual bool IsStealthed
	{
		get
		{
			if (this != Globals.Me)
			{
				return Status.Any((IStatus t) => t.Type == SpellEffect.Stealth);
			}
			return false;
		}
	}

	public virtual bool ShouldDraw
	{
		get
		{
			if (!IsHidden)
			{
				if (IsStealthed && this != Globals.Me)
				{
					return Globals.Me?.IsInMyParty(Id) ?? false;
				}
				return true;
			}
			return false;
		}
	}

	public virtual bool ShouldDrawName
	{
		get
		{
			if (MapInstance == null || string.IsNullOrWhiteSpace(Name) || HideName || !ShouldDraw)
			{
				return false;
			}
			Vector2 vector = Graphics.ConvertToWorldPoint(Globals.InputManager.MousePosition);
			IsHovered = BodyWorldPos.Contains(vector.X, vector.Y) && !Intersect.Client.Interface.Interface.DoesMouseHitInterface();
			if (!(this is Projectile) && !(this is Resource))
			{
				if (!(this is Event))
				{
					if (this is Player player)
					{
						if (IsHovered)
						{
							return true;
						}
						Player me = Globals.Me;
						if (me == null)
						{
							return false;
						}
						if (Globals.Database.MyOverheadInfo && player.Id == me.Id)
						{
							return true;
						}
						if (Globals.Database.PlayerOverheadInfo && player.Id != me.Id)
						{
							return true;
						}
						if (Globals.Database.PartyMemberOverheadInfo && me.IsInMyParty(player))
						{
							return true;
						}
						if (Globals.Database.FriendOverheadInfo && me.IsFriend(player))
						{
							return true;
						}
						if (Globals.Database.GuildMemberOverheadInfo)
						{
							return me.IsGuildMate(player);
						}
						return false;
					}
					if (!IsHovered)
					{
						return Globals.Database.NpcOverheadInfo;
					}
					return true;
				}
				return true;
			}
			return false;
		}
	}

	protected int RangeeDesPieds => (int)MathF.Floor(((float)(Y * TileHeight) + OffsetY + (float)TileHeight - 1f) / (float)TileHeight);

	private int NormalSpriteAnimationFrame
	{
		get
		{
			int num = WalkFrame;
			int? num2 = Recolte.PoseDePeche(this);
			if (num2.HasValue)
			{
				int valueOrDefault = num2.GetValueOrDefault();
				num = valueOrDefault;
			}
			else if (IsCharging)
			{
				num = Options.Instance.Sprites.NormalChargeFrame;
			}
			else if (IsBlocking)
			{
				num = Options.Instance.Sprites.NormalBlockFrame;
			}
			else if (IsCasting)
			{
				num = Options.Instance.Sprites.NormalCastFrame;
			}
			else
			{
				num2 = Recolte.ImageDeFrappe(this);
				if (num2.HasValue)
				{
					int valueOrDefault2 = num2.GetValueOrDefault();
					bool flag = (uint)(valueOrDefault2 - 1) <= 1u;
					num = (flag ? Options.Instance.Sprites.NormalAttackFrame : WalkFrame);
				}
				else
				{
					num2 = Artisanat.ImageDeTravail(this, SpriteFrames);
					if (num2.HasValue)
					{
						int valueOrDefault3 = num2.GetValueOrDefault();
						bool flag = (uint)(valueOrDefault3 - 1) <= 1u;
						num = (flag ? Options.Instance.Sprites.NormalAttackFrame : WalkFrame);
					}
					else if (Telegraphe.EnCours(this))
					{
						num = Options.Instance.Sprites.NormalAttackFrame;
					}
					else if (AttackTimer - CalculateAttackTime() / 2 > Timing.Global.Milliseconds)
					{
						num = Options.Instance.Sprites.NormalAttackFrame;
					}
				}
			}
			if (num >= SpriteFrames)
			{
				num = SpriteFrames / 2;
			}
			return num;
		}
	}

	public long ShieldSize => Status.Sum((IStatus status) => (status.Type != SpellEffect.Shield) ? 0 : status.Shield[0]);

	protected virtual bool ShouldDrawHpBar
	{
		get
		{
			if (ShouldNotDrawHpBar)
			{
				return false;
			}
			if (IsHovered)
			{
				return true;
			}
			if (ShieldSize > 0)
			{
				return true;
			}
			if (Vital[0] != MaxVital[0])
			{
				return true;
			}
			if (GetType() == typeof(Entity))
			{
				return Globals.Database.NpcOverheadHpBar;
			}
			return false;
		}
	}

	protected bool ShouldNotDrawHpBar
	{
		get
		{
			if (LatestMap != null && ShouldDraw)
			{
				return Vital[0] < 1;
			}
			return true;
		}
	}

	public bool ShouldDrawCastingBar
	{
		get
		{
			if (ShouldDraw && IsCasting)
			{
				return LatestMap != null;
			}
			return false;
		}
	}

	public bool ShouldDrawTarget
	{
		get
		{
			if (!(this is Projectile))
			{
				return LatestMap != null;
			}
			return false;
		}
	}

	public virtual bool CanBeAttacked => true;

	public long WallBumpAt { get; set; } = -1L;

	public Direction WallBumpDirection { get; set; }

	public long SpellPoseUntil { get; private set; }

	public bool EstPiloteLibrement { get; set; }

	public bool EstInterpoleeLibrement
	{
		get
		{
			if (!EstPiloteLibrement)
			{
				return _instantanesLibres.Count > 0;
			}
			return false;
		}
	}

	public float RayonDuCorps { get; set; } = 10f;

	public bool EstMonstreAuPixel
	{
		get
		{
			if (Type == EntityType.GlobalEntity && !(this is Critter))
			{
				return DeplacementLibre.MonstresLibres(MapId);
			}
			return false;
		}
	}

	public bool SeRecentre => _recentrageLibre;

	internal CoupHeritage CoupEnCours
	{
		get
		{
			if (!IsAttacking || AttackTimer != _hFinDuCoup)
			{
				return CoupHeritage.Aucun;
			}
			return _hCoup;
		}
	}

	internal int AvanceDuCoupEnCours
	{
		get
		{
			if (CoupEnCours == CoupHeritage.Aucun)
			{
				return 0;
			}
			return _hAvance;
		}
	}

	internal Guid CibleDuCoupEnCours
	{
		get
		{
			if (CoupEnCours == CoupHeritage.Aucun)
			{
				return Guid.Empty;
			}
			return _hCibleId;
		}
	}

	internal Guid CibleDuDernierCoupHeritage => _hCibleId;

	internal CoupHeritage DernierCoupHeritage => _hCoup;

	internal long FinDuDernierCoupHeritage => _hFinDuCoup;

	internal bool PousseeHeritageEnCours => _hPoussee.EnCours;

	internal (float X, float Y) PousseeHeritagePosee => (X: _hPousseeX, Y: _hPousseeY);

	internal bool PousseeHeritageEnRetrait => _hPoussee.EnRetrait;

	internal bool ReculeSousLesCoups { get; set; }

	internal bool MassiveSousLesCoups { get; set; }

	internal int DureeDuGeste()
	{
		if (this != Globals.Me || DureeDuCoupLocal <= 0)
		{
			return CalculateAttackTime();
		}
		return DureeDuCoupLocal;
	}

	internal int? PlancheDuPas(int planches)
	{
		if (planches <= 0 || (!(this is Player) && PasDuCoup == 0))
		{
			return null;
		}
		if (Heritage.Actif)
		{
			int? num = Heritage.PlancheDuMontant(this, planches);
			if (num.HasValue)
			{
				return num.GetValueOrDefault();
			}
		}
		if (Heritage.Actif && Heritage.SurLaPremierePlanche(this))
		{
			return 0;
		}
		if (Heritage.Actif && Heritage.SurLaPlancheDesPieds(this))
		{
			return Heritage.PlancheDesPieds(this, planches);
		}
		int num2 = planches;
		if (Path.GetFileNameWithoutExtension(Sprite ?? string.Empty).StartsWith("gohan_ado", StringComparison.OrdinalIgnoreCase))
		{
			num2 = Math.Min(num2, 2);
		}
		return PasDuCoup switch
		{
			2 => (num2 >= 3) ? 1 : 0, 
			3 => (num2 >= 3) ? 2 : Math.Min(1, num2 - 1), 
			_ => 0, 
		};
	}

	internal void ChoisirLaPlancheDuPas(byte pas)
	{
		if (PasDuCoup != pas)
		{
			PasDuCoup = pas;
			PickAnimationVariant(SpriteAnimations.Attack);
		}
	}

	protected virtual void OnPositionChanged(Vector3 newPosition, Vector3 oldPosition)
	{
	}

	public Entity(Guid id, EntityPacket? packet, EntityType entityType)
	{
		Id = id;
		Type = entityType;
		MapId = Guid.Empty;
		if (Id != Guid.Empty && Type != EntityType.Event)
		{
			for (int i = 0; i < Options.Instance.Player.MaxInventory; i++)
			{
				Inventory[i] = new Item();
			}
			for (int j = 0; j < Options.Instance.Player.MaxSpells; j++)
			{
				Spells[j] = new Spell();
			}
			for (int k = 0; k < Options.Instance.Equipment.Slots.Count; k++)
			{
				Equipment[k] = Guid.Empty;
				MyEquipment[k] = -1;
			}
		}
		AnimationTimer = Timing.Global.MillisecondsUtc + Globals.Random.Next(0, 500);
		if (Options.Instance.Equipment.Slots.Count == 0)
		{
			throw new Exception("What the fuck is going on!?!?!?!?!?!");
		}
		Load(packet);
	}

	public virtual void Load(EntityPacket? packet)
	{
		if (packet == null)
		{
			return;
		}
		MapId = packet.MapId;
		Name = ((this is Player) ? packet.Name : Traduction.NomTraduit(Id, packet.Name));
		Sprite = packet.Sprite;
		Color = packet.Color;
		Face = packet.Face;
		Level = packet.Level;
		if (this != Globals.Me || !EstPiloteLibrement)
		{
			float num = MathF.Floor(packet.Position.X);
			float num2 = MathF.Floor(packet.Position.Y);
			Vector3 position = packet.Position;
			position.X = num;
			position.Y = num2;
			Position = position;
			if (packet.Position.X != num || packet.Position.Y != num2)
			{
				OffsetX = (packet.Position.X - num) * (float)TileWidth;
				OffsetY = (packet.Position.Y - num2) * (float)TileHeight;
			}
		}
		DirectionFacing = (Direction)packet.Dir;
		Passable = packet.Passable;
		HideName = packet.HideName;
		IsHidden = packet.HideEntity;
		NameColor = packet.NameColor;
		HeaderLabel = new Label(packet.HeaderLabel.Label, packet.HeaderLabel.Color);
		FooterLabel = new Label(packet.FooterLabel.Label, packet.FooterLabel.Color);
		List<Animation> list = new List<Animation>();
		List<AnimationDescriptor> list2 = new List<AnimationDescriptor>();
		for (int i = 0; i < packet.Animations.Length; i++)
		{
			AnimationDescriptor animationDescriptor = DatabaseObject<AnimationDescriptor>.Get(packet.Animations[i]);
			if (animationDescriptor != null)
			{
				list2.Add(animationDescriptor);
			}
		}
		foreach (Animation animation2 in _animations)
		{
			list.Add(animation2);
			if (!animation2.InfiniteLoop)
			{
				list.Remove(animation2);
				continue;
			}
			foreach (AnimationDescriptor item in list2)
			{
				Guid id = item.Id;
				Guid? obj = animation2?.Descriptor?.Id;
				if (id == obj)
				{
					list.Remove(animation2);
					list2.Remove(item);
					break;
				}
			}
			Animation[] equipmentAnimations = EquipmentAnimations;
			foreach (Animation animation in equipmentAnimations)
			{
				if (animation == animation2 && animation2 != null)
				{
					list.Remove(animation2);
				}
			}
		}
		RemoveAnimations(list);
		AddAnimations(list2);
		Vital = packet.Vital;
		MaxVital = packet.MaxVital;
		Status.Clear();
		if (packet.StatusEffects == null)
		{
			ApplicationContext.Context.Value?.Logger.LogWarning("'packet.StatusEffects' is null.");
		}
		else
		{
			StatusPacket[] statusEffects = packet.StatusEffects;
			foreach (StatusPacket statusPacket in statusEffects)
			{
				Status status = new Status(statusPacket.SpellId, statusPacket.Type, statusPacket.TransformSprite, statusPacket.TimeRemaining, statusPacket.TotalDuration)
				{
					Sequence = statusPacket.Sequence
				};
				Status?.Add(status);
				if (status.Type == SpellEffect.Shield)
				{
					status.Shield = statusPacket.VitalShields;
				}
			}
		}
		SortStatuses();
		Stat = packet.Stats;
		mDisposed = false;
		if (Globals.Me == null)
		{
			ApplicationContext.Context.Value?.Logger.LogWarning("'Me' is null.");
		}
		else if (Id == Globals.Me.Id)
		{
			Intersect.Client.Interface.Interface.EnqueueInGame(delegate(GameInterface gameInterface)
			{
				if (gameInterface.PlayerStatusWindow == null)
				{
					ApplicationContext.Context.Value?.Logger.LogWarning("'PlayerStatusWindow' is null.");
				}
				else
				{
					gameInterface.PlayerStatusWindow.ShouldUpdateStatuses = true;
				}
			}, delegate(Guid entityId, string entityName)
			{
				ApplicationContext.CurrentContext.Logger.LogWarning("Tried to load entity {EntityId} ({EntityName}) from packet before in-game UI was ready", entityId, entityName);
			}, packet.EntityId, packet.Name);
		}
		else if (Id != Guid.Empty && Id == Globals.Me.TargetId)
		{
			if (Globals.Me.TargetBox == null)
			{
				ApplicationContext.Context.Value?.Logger.LogWarning("'TargetBox' is null.");
			}
			else
			{
				Globals.Me.TargetBox.ShouldUpdateStatuses = true;
			}
		}
	}

	public void AddAnimations(List<AnimationDescriptor> animationDescriptors)
	{
		foreach (AnimationDescriptor animationDescriptor in animationDescriptors)
		{
			TryAddAnimation(new Animation(animationDescriptor, loopForever: true, autoRotate: false, -1, this)
			{
				SousLesCimes = true
			});
		}
	}

	public virtual bool IsAllyOf(Player en)
	{
		if (en == null || MapInstance == null || en.MapInstance == null)
		{
			return false;
		}
		if (Type == EntityType.Resource)
		{
			return false;
		}
		if (Type == EntityType.Event)
		{
			return true;
		}
		if (en.Id == Id)
		{
			return true;
		}
		return false;
	}

	public void ClearAnimations()
	{
		RemoveAnimations(_animations);
	}

	public void RemoveAnimations(IEnumerable<Animation> animations, bool dispose = true)
	{
		Animation[] array = animations.ToArray();
		Animation[] array2 = array;
		foreach (Animation animation in array2)
		{
			if (dispose && !animation.IsDisposed)
			{
				animation.Dispose();
			}
			TryRemoveAnimation(animation);
		}
	}

	public virtual void Dispose()
	{
		if (RenderList != null)
		{
			RenderList.Remove(this);
		}
		ClearAnimations();
		GC.SuppressFinalize(this);
		mDisposed = true;
	}

	private void UpdateWalkFrame(long timingMilliseconds)
	{
		int num = Math.Max(1, SpriteFrames);
		bool flag = SpriteAnimation == SpriteAnimations.Run;
		float num2 = Math.Max(1f, (float)TileHeight * (flag ? 4f : 2f) / (float)num);
		if (IsMoving)
		{
			_lastStepMilliseconds = timingMilliseconds;
			int num3 = 0;
			while (_walkedSinceFrame >= num2 && num3 < num)
			{
				if (flag && timingMilliseconds - _lastWalkFrameAt < 135)
				{
					_walkedSinceFrame = num2;
					break;
				}
				_walkedSinceFrame -= num2;
				WalkFrame = (WalkFrame + 1) % num;
				_lastWalkFrameAt = timingMilliseconds;
				SandSteps.OnWalkFrame(this, WalkFrame, flag);
				if (BossVisual != BossVisual.Standard)
				{
					DungeonWorld.PasDuBoss(this, WalkFrame);
				}
				num3++;
			}
		}
		else if (timingMilliseconds - _lastStepMilliseconds >= 90)
		{
			WalkFrame = ((WalkFrame > 0) ? (num / 2) : 0);
			_walkedSinceFrame = num2;
		}
	}

	protected static float StepTowardsZero(float offset, float distance)
	{
		if (offset > 0f)
		{
			return Math.Max(0f, offset - distance);
		}
		if (!(offset < 0f))
		{
			return 0f;
		}
		return Math.Min(0f, offset + distance);
	}

	public virtual float GetMovementTime()
	{
		float num = 1000f / (float)(1.0 + Math.Log(Stat[4]));
		if (DirectionFacing > Direction.Right)
		{
			num *= Options.Instance.Map.DiagonalMovementCost;
		}
		if (IsBlocking)
		{
			num += num * BlockingSlow;
		}
		else
		{
			if (IsAttacking)
			{
				num += num * AttackMovementSlow;
			}
			float flightSpeedBoost = FlightSpeedBoost;
			if (flightSpeedBoost > 0f)
			{
				num -= num * flightSpeedBoost;
			}
			else if (IsSprinting)
			{
				num -= num * SprintSpeedBoost;
			}
		}
		return Math.Min(1000f, num);
	}

	public override string ToString()
	{
		return Name;
	}

	public virtual void HoldForHitStop(long milliseconds)
	{
		if (milliseconds > 0)
		{
			if (mLastUpdate != 0.0)
			{
				mLastUpdate += milliseconds;
			}
			Dashing?.Delay(milliseconds);
			if (Heritage.Actif && DashQueue.Count > 0)
			{
				RetenirLesReculsEnAttente(milliseconds);
			}
		}
	}

	public virtual bool Update()
	{
		if (mDisposed)
		{
			LatestMap = null;
			return false;
		}
		if (LatestMap?.Id != MapId)
		{
			LatestMap = Intersect.Client.Maps.MapInstance.Get(MapId);
		}
		if (LatestMap == null)
		{
			EntitesInconnues.NoterPerte(Id, 2);
			Globals.EntitiesToDispose.Add(Id);
			return false;
		}
		if (!LatestMap.InView())
		{
			return AttendreHorsDeVue();
		}
		_horsDeVueDepuis = 0L;
		RenderList = DetermineRenderOrder(RenderList, LatestMap);
		double maintenantPrecis = DeplacementLibre.MaintenantPrecis;
		if (mLastUpdate == 0.0)
		{
			mLastUpdate = maintenantPrecis;
		}
		float num = (elapsedtime = (float)Math.Max(0.0, maintenantPrecis - mLastUpdate));
		if (Dashing != null)
		{
			WalkFrame = Options.Instance.Sprites.NormalDashFrame;
		}
		else if (DashQueue.Count > 0)
		{
			IsMoving = false;
			if (this != Globals.Me && EstPiloteLibrement)
			{
				OublierLeDeplacementLibre();
			}
			if (Heritage.Actif)
			{
				Heritage.Poussee.Reprise(this);
			}
			Dashing = DashQueue.Dequeue();
			Dashing.Start(this);
			Dash dashing = Dashing;
			if (dashing != null && dashing.IsKnockback && LatestMap != null)
			{
				Knockback.OnStart(this, dashing, LatestMap);
			}
			if (Dashing != null)
			{
				Echanges.OnDash(this, Dashing);
			}
			if (!IsMoving)
			{
				OffsetX = 0f;
				OffsetY = 0f;
			}
			DashTimer = Timing.Global.Milliseconds + Options.Instance.Combat.MaxDashSpeed;
		}
		else
		{
			UpdateWalkFrame(Timing.Global.Milliseconds);
		}
		if (Dashing != null)
		{
			if (Dashing.Update(this))
			{
				OffsetX = Dashing.GetXOffset();
				OffsetY = Dashing.GetYOffset();
			}
			else if (!IsMoving && !DeplacementLibre.EstLibre(MapId))
			{
				OffsetX = 0f;
				OffsetY = 0f;
			}
			if (LatestMap?.Id != MapId)
			{
				LatestMap = Intersect.Client.Maps.MapInstance.Get(MapId);
			}
			Knockback.Observe(this, LatestMap);
		}
		else if (EstPiloteLibrement)
		{
			if (this != Globals.Me)
			{
				InterpolerLibrement();
				if (LatestMap?.Id != MapId)
				{
					LatestMap = Intersect.Client.Maps.MapInstance.Get(MapId);
				}
			}
		}
		else if (SeRecentre)
		{
			RecentrerEnDouceur(num);
		}
		else if (IsMoving)
		{
			float num3 = GetMovementTime();
			if (this != Globals.Me && !(this is Critter))
			{
				num3 *= 1.2f;
			}
			float num4 = ((this != Globals.Me && !(this is Critter)) ? Math.Max(TileHeight, _remoteStepDistance) : ((float)TileHeight));
			float num5 = num * num4 / num3;
			_walkedSinceFrame += num5;
			float num6 = Math.Max(Math.Abs(OffsetX), Math.Abs(OffsetY));
			PickLastDirection(DirectionFacing);
			OffsetX = StepTowardsZero(OffsetX, num5 * ResorptionX);
			OffsetY = StepTowardsZero(OffsetY, num5 * ResorptionY);
			if (OffsetX == 0f && OffsetY == 0f)
			{
				IsMoving = false;
				FinishedStepOvershoot = Math.Max(0f, num5 - num6);
			}
		}
		if (Equipment.Length == Options.Instance.Equipment.Slots.Count)
		{
			for (int i = 0; i < Options.Instance.Equipment.Slots.Count; i++)
			{
				Animation animation = EquipmentAnimations[i];
				if (Equipment[i] != Guid.Empty && (this != Globals.Me || MyEquipment[i] < Options.Instance.Player.MaxInventory))
				{
					Guid id = ((this == Globals.Me && MyEquipment[i] > -1) ? Inventory[MyEquipment[i]].ItemId : Equipment[i]);
					if (DatabaseObject<ItemDescriptor>.TryGet(id, out var databaseObject))
					{
						AnimationDescriptor equipmentAnimation = databaseObject.EquipmentAnimation;
						if (equipmentAnimation != null)
						{
							if (animation == null || animation.Descriptor != equipmentAnimation || animation.IsDisposed)
							{
								if (animation != null)
								{
									TryRemoveAnimation(animation, dispose: true);
								}
								Animation animation2 = new Animation(equipmentAnimation, loopForever: true, autoRotate: true, -1, this)
								{
									SousLesCimes = true
								};
								EquipmentAnimations[i] = animation2;
								_animations.Add(animation2);
							}
							continue;
						}
					}
					if (animation != null)
					{
						TryRemoveAnimation(animation, dispose: true);
						EquipmentAnimations[i] = null;
					}
				}
				else if (animation != null)
				{
					TryRemoveAnimation(animation, dispose: true);
					EquipmentAnimations[i] = null;
				}
			}
		}
		ChatBubble[] array = mChatBubbles.ToArray();
		ChatBubble[] array2 = array;
		foreach (ChatBubble chatBubble in array2)
		{
			if (!chatBubble.Update())
			{
				mChatBubbles.Remove(chatBubble);
			}
		}
		if (AnimationTimer < Timing.Global.MillisecondsUtc)
		{
			AnimationTimer = Timing.Global.MillisecondsUtc + 200;
			AnimationFrame++;
			if (AnimationFrame >= SpriteFrames)
			{
				AnimationFrame = 0;
			}
		}
		CalculateOrigin();
		List<Animation> list = null;
		foreach (Animation animation3 in _animations)
		{
			animation3.Update();
			if (animation3.IsDisposed)
			{
				if (list == null)
				{
					list = new List<Animation>();
				}
				list.Add(animation3);
				continue;
			}
			if (IsStealthed || IsHidden)
			{
				animation3.Hide();
			}
			else
			{
				animation3.Show();
			}
			Direction dir = (animation3.AutoRotate ? DirectionFacing : Direction.Up);
			animation3.SetPosition((int)Math.Ceiling(Center.X), (int)Math.Ceiling(Center.Y), X, Y, MapId, dir, Z);
		}
		if (list != null)
		{
			RemoveAnimations(list);
		}
		mLastUpdate = maintenantPrecis;
		UpdateSpriteAnimation();
		return true;
	}

	public bool TryAddAnimation(Animation animation, AnimationSource animationSource = default(AnimationSource))
	{
		if (animationSource != default(AnimationSource) && _animationsBySource.TryGetValue(animationSource, out Animation value) && !TryRemoveAnimation(value, animationSource))
		{
			return false;
		}
		_animationsBySource[animationSource] = animation;
		_animations.Add(animation);
		return true;
	}

	public bool TryRemoveAnimation(AnimationSource animationSource, bool dispose = false)
	{
		Animation animation;
		return TryRemoveAnimation(animationSource, dispose, out animation);
	}

	public bool TryRemoveAnimation(AnimationSource animationSource, [NotNullWhen(true)] out Animation? animation)
	{
		return TryRemoveAnimation(animationSource, dispose: false, out animation);
	}

	public bool TryRemoveAnimation(AnimationSource animationSource, bool dispose, [NotNullWhen(true)] out Animation? animation)
	{
		if (!_animationsBySource.Remove(animationSource, out animation))
		{
			return false;
		}
		_animations.Remove(animation);
		if (dispose)
		{
			animation.Dispose();
		}
		return true;
	}

	public bool RemoveAnimationIfExists(AnimationSource animationSource, bool dispose = false)
	{
		if (_animationsBySource.ContainsKey(animationSource))
		{
			return TryRemoveAnimation(animationSource, dispose);
		}
		return true;
	}

	public bool TryRemoveAnimation(Animation animation, bool dispose = false)
	{
		return TryRemoveAnimation(animation, animation.Source, dispose);
	}

	public bool TryRemoveAnimation(Animation animation, AnimationSource animationSource, bool dispose = false)
	{
		bool flag = animationSource != default(AnimationSource) && _animationsBySource.Remove(animationSource);
		bool flag2 = _animations.Remove(animation);
		if (dispose)
		{
			animation.Dispose();
		}
		return flag2 || flag;
	}

	public virtual int CalculateAttackTime()
	{
		if (this != Globals.Me && AttackTime > -1)
		{
			return AttackTime;
		}
		return (int)((float)Options.Instance.Combat.MaxAttackRate + (float)(Options.Instance.Combat.MinAttackRate - Options.Instance.Combat.MaxAttackRate) * (((float)Options.Instance.Player.MaxStat - (float)Stat[4]) / (float)Options.Instance.Player.MaxStat));
	}

	public virtual HashSet<Entity>? DetermineRenderOrder(HashSet<Entity>? existingRenderSet, IMapInstance? map)
	{
		existingRenderSet?.Remove(this);
		if (map == null)
		{
			return null;
		}
		Guid[,] mapGrid = Globals.MapGrid;
		if (mapGrid == null)
		{
			return null;
		}
		HashSet<Entity>[,] renderingEntities = Graphics.RenderingEntities;
		if (renderingEntities == null)
		{
			return null;
		}
		IMapInstance mapInstance = Globals.Me?.MapInstance;
		if (mapInstance == null)
		{
			return null;
		}
		int gridX = mapInstance.GridX;
		int gridY = mapInstance.GridY;
		int num = Math.Max(0, gridX - 1);
		int num2 = Math.Max(0, gridY - 1);
		long mapGridWidth = Globals.MapGridWidth;
		long mapGridHeight = Globals.MapGridHeight;
		int length = mapGrid.GetLength(0);
		int length2 = mapGrid.GetLength(1);
		long num3 = Math.Min(Math.Min(mapGridWidth, length) - 1, gridX + 1);
		long num4 = Math.Min(Math.Min(mapGridHeight, length2) - 1, gridY + 1);
		if (mapGridWidth != length)
		{
			ApplicationContext.CurrentContext.Logger.LogWarning("Possible map grid race condition detected, MapGrid's X dimension is {XLength} but MapGridWidth is {Width}", length, mapGridWidth);
		}
		if (mapGridHeight != length2)
		{
			ApplicationContext.CurrentContext.Logger.LogWarning("Possible map grid race condition detected, MapGrid's Y dimension is {YLength} but MapGridHeight is {Height}", length2, mapGridHeight);
		}
		for (int i = num; i <= num3; i++)
		{
			for (int j = num2; j <= num4; j++)
			{
				Guid guid = mapGrid[i, j];
				if (!(guid != MapId))
				{
					byte b = mRenderPriority;
					if (Z != 0)
					{
						b += 3;
					}
					HashSet<Entity> result = null;
					int num5 = ((j == gridY - 1) ? (MapHeight + RangeeDesPieds) : ((j != gridY) ? (MapHeight * 3 + RangeeDesPieds) : (MapHeight * 2 + RangeeDesPieds)));
					if (Heritage.Actif)
					{
						num5 = Heritage.RangeePourLeTri(this, num5);
					}
					if (b >= renderingEntities.GetLength(0) || num5 < 0 || num5 >= renderingEntities.GetLength(1))
					{
						return result;
					}
					result = renderingEntities[b, num5];
					result.Add(this);
					return result;
				}
			}
		}
		return existingRenderSet;
	}

	public virtual void Draw()
	{
		if (IsHidden)
		{
			return;
		}
		WorldPos.Reset();
		if (!Globals.GridMaps.ContainsKey(MapId) || !Intersect.Client.Maps.MapInstance.TryGet(MapId, out MapInstance instance) || Echanges.Cache(this) || Clash.Cache(this))
		{
			return;
		}
		Echanges.Observer(this);
		string text = string.Empty;
		Color color = new Color(Color.A, Color.R, Color.G, Color.B);
		if (Timing.Global.Milliseconds < HitFlashUntil)
		{
			color = new Color(color.A, 255, 140, 140);
		}
		if (IsDying)
		{
			int a = ((this is Player) ? Mort.Alpha(this, color.A) : CombatFeedback.DeathAlpha(this, color.A));
			color = new Color(a, color.R, color.G, color.B);
		}
		color.A = Vanish.Flicker(this, color.A);
		color.A = ChuteDansLeVide.Alpha(this, color.A);
		string text2 = string.Empty;
		for (int i = 0; i < Status.Count; i++)
		{
			IStatus status = Status[i];
			switch (status.Type)
			{
			case SpellEffect.Transform:
				text2 = (text = status.Data);
				break;
			case SpellEffect.Stealth:
				if (this != Globals.Me)
				{
					if (!(this is Player player))
					{
						return;
					}
					Player? me = Globals.Me;
					if (me == null || !me.IsInMyParty(player))
					{
						return;
					}
				}
				color.A /= 2;
				break;
			}
		}
		if (text2 != TransformedSprite)
		{
			Transformation.OnSpriteChanged(this, TransformedSprite, text2);
			TransformedSprite = text2;
		}
		if (string.IsNullOrEmpty(text))
		{
			text = (Sprite = Sprite);
		}
		if (!AnimatedTextures.TryGetValue(SpriteAnimation, out IGameTexture value))
		{
			value = Texture;
		}
		value = Transformation.SpriteTexture(this, value, Sheet != null, AnimatedTextures.ContainsKey(SpriteAnimations.Ki));
		if (value == null)
		{
			WorldPos = new FloatRect((float)(instance.X + X * TileWidth) + OffsetX, (float)(instance.Y + Y * TileHeight) + OffsetY, TileWidth, TileHeight);
			return;
		}
		int num = PickSpriteRow(DirectionFacing);
		int num2 = SpriteFrame;
		if (Options.Instance.AnimatedSprites.Contains(text.ToLower()))
		{
			num2 = AnimationFrame;
		}
		else
		{
			SpriteAnimations spriteAnimation = SpriteAnimation;
			if ((spriteAnimation == SpriteAnimations.Normal || spriteAnimation == SpriteAnimations.Run) ? true : false)
			{
				num2 = NormalSpriteAnimationFrame;
			}
		}
		int num3 = 0;
		int num4 = 0;
		SpriteSheet sheet = Sheet;
		FloatRect source;
		if (sheet != null)
		{
			SpriteSheetAnimation currentBand = CurrentBand;
			if (currentBand != null)
			{
				(int X, int Y, int Width, int Height) tuple = sheet.FrameRect(currentBand, num2, num);
				int item = tuple.X;
				int item2 = tuple.Y;
				int item3 = tuple.Width;
				int item4 = tuple.Height;
				source = new FloatRect(item, item2, item3, item4);
				num3 = currentBand.OffsetX;
				num4 = currentBand.OffsetY;
				goto IL_0380;
			}
		}
		int num5 = value.Width / SpriteFrames;
		int num6 = value.Height / Options.Instance.Sprites.Directions;
		source = new FloatRect(num2 * num5, num * num6, num5, num6);
		goto IL_0380;
		IL_0380:
		bool flag = Mort.TryPose(this, num, ref value, ref source);
		float lift = 0f;
		bool flag2 = !flag && Flight.TryPose(this, num, ref value, ref source, out lift);
		bool flag3 = !flag2 && !flag && (KiBeam.TryPose(this, num, ref value, ref source) || Maitres.TryPose(this, num, ref value, ref source));
		bool flag4 = Heritage.Actif && !flag2 && !flag3 && !flag && Heritage.TryPose(this, num, ref value, ref source);
		FloatRect ombre;
		float hauteur;
		bool flag5 = Levitation.Couper(this, ref source, out ombre, out hauteur);
		FloatRect rectangle = new FloatRect((int)Math.Ceiling(Origin.X - source.Width / 2f) + num3, (int)Math.Ceiling(Origin.Y - source.Height) + num4, source.Width, source.Height);
		rectangle = (WorldPos = KiBlast.KillProjection(this, rectangle));
		rectangle = KiBeam.Pose(this, rectangle);
		rectangle = Maitres.Pose(this, rectangle);
		rectangle = Mort.Pose(this, rectangle);
		GroundShadow.Draw(this, rectangle, color.A);
		if (flag5)
		{
			Levitation.DessinerLOmbre(value, ombre, rectangle, color);
		}
		bool flag6 = SpriteAnimation == SpriteAnimations.Run && !flag2 && !flag3 && !flag && Dashing == null && !Options.Instance.AnimatedSprites.Contains(text.ToLower());
		_runShift = (flag6 ? RunStride.Offset(value, source, num2, SpriteFrames) : (X: 0, Y: 0));
		_runPoseSince = ((!flag6) ? 0 : ((_runPoseSince == 0L) ? Timing.Global.Milliseconds : _runPoseSince));
		_runLean = ((flag6 && string.IsNullOrEmpty(TransformedSprite)) ? RunStride.Lean(num, _runPoseSince, Timing.Global.Milliseconds) : 0f);
		_runLean += Flight.CloudLean(this);
		_runLean += DashAnime.Penche(this);
		(int, int) runShift = _runShift;
		if (runShift.Item1 != 0 || runShift.Item2 != 0)
		{
			rectangle = new FloatRect(rectangle.X + (float)_runShift.X, rectangle.Y + (float)_runShift.Y, rectangle.Width, rectangle.Height);
		}
		rectangle = new FloatRect(rectangle.X + Flight.RiderSway(this), rectangle.Y - lift - hauteur - MathF.Round(Echanges.Hauteur(this)), rectangle.Width, rectangle.Height);
		bool flag7 = Flight.IsAirborne(this);
		rectangle = CombatFeedback.AdjustGuardPose(this, rectangle, AnimatedTextures.ContainsKey(SpriteAnimations.Block));
		rectangle = WallBumpPose(rectangle);
		rectangle = Recolte.PoseElan(this, rectangle);
		rectangle = Artisanat.PoseElan(this, rectangle);
		DashAnime.Observer(this);
		rectangle = DashAnime.Pose(this, rectangle);
		if (!flag7)
		{
			KarmaAura.DrawCriminalFlames(this, rectangle);
		}
		Flight.DrawCloudBehind(this);
		KiCharge.DrawAura(this, rectangle, value, source);
		Transformation.DrawAura(this, rectangle);
		FormAura.DrawBehind(this, rectangle, value, source);
		Flight.DrawSprintBehind(this, rectangle, value, source);
		ProfessionLevelUp.DrawAura(this, rectangle);
		Telegraphe.DessinerContour(this, rectangle, value, source);
		if (BossVisual != BossVisual.Standard)
		{
			DungeonWorld.DerriereLeBoss(this, rectangle, value, source);
		}
		BoulesDeCristal.DrawBehind(this, rectangle, color);
		AurasEquipees.DrawBehind(this, rectangle, color);
		CombatFeedback.DrawShield(this, rectangle, front: false);
		Recolte.DessinerCanne(this, value, source, rectangle, devant: false);
		_bodyDestination = rectangle;
		_bodyTextureName = value.Name;
		_immersion = ((!flag7) ? Meteo.DebutPersonnage(this, rectangle) : 0);
		bool flag8 = flag && Mort.AGenoux(this);
		if (flag8 && (text == Sprite || Transformation.KeepsPaperdolls(this)) && Equipment.Length == Options.Instance.Equipment.Slots.Count)
		{
			foreach (string item9 in Options.Instance.Equipment.Paperdoll.Directions[(int)_lastDirection])
			{
				(string, Color)? tuple2 = CalquePorte(Options.Instance.Equipment.Slots.IndexOf(item9));
				if (tuple2.HasValue)
				{
					(string, Color) valueOrDefault = tuple2.GetValueOrDefault();
					DrawWrapEquipment(valueOrDefault.Item1, valueOrDefault.Item2 * color, value, source);
				}
			}
		}
		for (int j = 0; j < Options.Instance.Equipment.Paperdoll.Directions[(int)_lastDirection].Count; j++)
		{
			string text3 = Options.Instance.Equipment.Paperdoll.Directions[(int)_lastDirection][j];
			int num7 = Options.Instance.Equipment.Slots.IndexOf(text3);
			if (string.Equals("Player", text3, StringComparison.Ordinal))
			{
				(float Lean, float Shiver) tuple3 = Knockback.Pose(this);
				float item5 = tuple3.Lean;
				float item6 = tuple3.Shiver;
				(float Penche, float Tremble) tuple4 = Echanges.Pose(this);
				float item7 = tuple4.Penche;
				float item8 = tuple4.Tremble;
				item5 += item7;
				if (Heritage.Actif)
				{
					item5 += Heritage.Corps.Penche(this);
				}
				item6 = ((item6 != 0f) ? item6 : item8);
				float step = ((_runLean != 0f) ? 0.5f : 1f);
				item5 += _runLean;
				FloatRect floatRect2 = ((item6 != 0f) ? new FloatRect(rectangle.X + item6, rectangle.Y, rectangle.Width, rectangle.Height) : rectangle);
				DrawLeaning(value, source, floatRect2, color, item5, GameBlendModes.None, _immersion, step);
				Meteo.DessinerImmerge(value, source, floatRect2, color, _immersion);
				if (!flag7)
				{
					Meteo.RetenirCouche(this, value, source, floatRect2, color);
				}
				Afterimages.Remember(this, value, source, rectangle);
				int num8 = CombatFeedback.WhiteFlash(this);
				if (num8 > 0)
				{
					DrawLeaning(value, source, floatRect2, new Color(num8, 255, 255, 255), item5, GameBlendModes.Add, _immersion, step);
				}
				KiCharge.DrawGlow(this, value, source, rectangle);
				Transformation.DrawGlow(this, value, source, rectangle);
				if (BossVisual != BossVisual.Standard)
				{
					DungeonWorld.DevantLeBoss(this, value, source, floatRect2);
				}
				FormAura.DrawFront(this, rectangle);
				Flight.DrawSprintFront(this, rectangle, value, source);
			}
			else
			{
				if (num7 <= -1 || (!(text == Sprite) && !Transformation.KeepsPaperdolls(this)) || Equipment.Length != Options.Instance.Equipment.Slots.Count || ((!(Equipment[num7] != Guid.Empty) || this == Globals.Me) && MyEquipment[num7] >= Options.Instance.Player.MaxInventory))
				{
					continue;
				}
				Guid id = Guid.Empty;
				if (this == Globals.Me)
				{
					int num9 = MyEquipment[num7];
					if (num9 > -1)
					{
						id = Inventory[num9].ItemId;
					}
				}
				else
				{
					id = Equipment[num7];
				}
				ItemDescriptor itemDescriptor = DatabaseObject<ItemDescriptor>.Get(id);
				if (!DatabaseObject<ItemDescriptor>.TryGet(id, out var databaseObject))
				{
					continue;
				}
				string filename = Paperdolls.ForClass((Gender == Gender.Male) ? databaseObject.MalePaperdoll : databaseObject.FemalePaperdoll, (this is Player player2) ? player2.Class : Guid.Empty);
				if (!(flag8 ? AUneEnveloppe(filename, value) : DrawWrapEquipment(filename, itemDescriptor.Color * color, value, source)))
				{
					if (flag4)
					{
						DrawPoseEquipment(filename, itemDescriptor.Color * color, value, source);
					}
					else if (flag2 || flag3 || flag)
					{
						DrawPoseEquipment(filename, itemDescriptor.Color * color, value, source);
					}
					else
					{
						DrawEquipment(filename, itemDescriptor.Color * color);
					}
				}
			}
		}
		Meteo.FinPersonnage(this, rectangle, _immersion);
		if (Mort.Gisant(this))
		{
			Mort.DessinerAme(this, value, source, rectangle, color.A);
		}
		else
		{
			Aureole.Dessiner(this, value, source, rectangle, color.A);
		}
		Recolte.DessinerCanne(this, value, source, rectangle, devant: true);
		BoulesDeCristal.DrawFront(this, rectangle, color);
		AurasEquipees.DrawFront(this, rectangle, color);
		CombatFeedback.DrawShield(this, rectangle, front: true);
		Flight.DrawCloud(this, rectangle);
	}

	private static void DrawLeaning(IGameTexture texture, FloatRect source, FloatRect destination, Color color, float lean, GameBlendModes blend, int immersion = 0, float step = 1f)
	{
		if (immersion > 0)
		{
			int num = (int)source.Height - immersion;
			if (num <= 0)
			{
				return;
			}
			float num2 = destination.Height / source.Height;
			source = new FloatRect(source.X, source.Y, source.Width, num);
			destination = new FloatRect(destination.X, destination.Y, destination.Width, (float)num * num2);
		}
		if (MathF.Abs(lean) < 0.5f || source.Height < 4f)
		{
			if (blend == GameBlendModes.None)
			{
				Graphics.DrawGameTexture(texture, source, destination, color);
			}
			else
			{
				Graphics.DrawGameTexture(texture, source, destination, color, null, blend);
			}
			return;
		}
		int num3 = (int)source.Height;
		for (int i = 0; i < num3; i += 2)
		{
			int num4 = Math.Min(2, num3 - i);
			float num5 = MathF.Round(lean * (1f - (float)i / (float)num3) / step) * step;
			FloatRect srcRectangle = new FloatRect(source.X, source.Y + (float)i, source.Width, num4);
			FloatRect targetRect = new FloatRect(destination.X + num5, destination.Y + (float)i * (destination.Height / source.Height), destination.Width, (float)num4 * (destination.Height / source.Height));
			if (blend == GameBlendModes.None)
			{
				Graphics.DrawGameTexture(texture, srcRectangle, targetRect, color);
			}
			else
			{
				Graphics.DrawGameTexture(texture, srcRectangle, targetRect, color, null, blend);
			}
		}
	}

	private int PickSpriteRow(Direction direction)
	{
		switch (direction)
		{
		case Direction.DownLeft:
			if (_lastDirection != Direction.Left)
			{
				if (_lastDirection != Direction.Left)
				{
				}
				break;
			}
			goto case Direction.Left;
		case Direction.UpLeft:
			if (_lastDirection == Direction.Left)
			{
				goto case Direction.Left;
			}
			if (_lastDirection == Direction.Left)
			{
				break;
			}
			goto case Direction.Up;
		case Direction.Left:
			return 1;
		case Direction.DownRight:
			if (_lastDirection != Direction.Right)
			{
				_ = _lastDirection;
				_ = 3;
				break;
			}
			goto case Direction.Right;
		case Direction.UpRight:
			if (_lastDirection == Direction.Right)
			{
				goto case Direction.Right;
			}
			if (_lastDirection == Direction.Right)
			{
				break;
			}
			goto case Direction.Up;
		case Direction.Right:
			return 2;
		case Direction.Up:
			return 3;
		}
		return 0;
	}

	public void PickLastDirection(Direction direction)
	{
		switch (direction)
		{
		case Direction.DownLeft:
			if (_lastDirection != Direction.Left)
			{
				if (_lastDirection != Direction.Left)
				{
				}
				break;
			}
			goto case Direction.Left;
		case Direction.UpLeft:
			if (_lastDirection == Direction.Left)
			{
				goto case Direction.Left;
			}
			if (_lastDirection == Direction.Left)
			{
				break;
			}
			goto case Direction.Up;
		case Direction.Left:
			_lastDirection = Direction.Left;
			return;
		case Direction.DownRight:
			if (_lastDirection != Direction.Right)
			{
				_ = _lastDirection;
				_ = 3;
				break;
			}
			goto case Direction.Right;
		case Direction.UpRight:
			if (_lastDirection == Direction.Right)
			{
				goto case Direction.Right;
			}
			if (_lastDirection == Direction.Right)
			{
				break;
			}
			goto case Direction.Up;
		case Direction.Right:
			_lastDirection = Direction.Right;
			return;
		case Direction.Up:
			_lastDirection = Direction.Up;
			return;
		}
		_lastDirection = Direction.Down;
	}

	public void DrawChatBubbles()
	{
		if (!IsHidden && ShouldDraw)
		{
			ChatBubble[] array = mChatBubbles.ToArray();
			float yoffset = 0f;
			for (int num = array.Length - 1; num > -1; num--)
			{
				yoffset = array[num].Draw(yoffset);
			}
		}
	}

	private string? DrawnForm()
	{
		if (string.IsNullOrEmpty(TransformedSprite) || string.IsNullOrEmpty(_bodyTextureName) || !Transformation.KeepsPaperdolls(this))
		{
			return null;
		}
		string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(Sprite);
		string fileNameWithoutExtension2 = Path.GetFileNameWithoutExtension(Transformation.TextureNameFor(Sprite, TransformedSprite));
		string fileNameWithoutExtension3 = Path.GetFileNameWithoutExtension(_bodyTextureName);
		if (fileNameWithoutExtension2.Length <= fileNameWithoutExtension.Length + 1 || !fileNameWithoutExtension3.StartsWith(fileNameWithoutExtension2, StringComparison.OrdinalIgnoreCase))
		{
			return null;
		}
		string text = fileNameWithoutExtension2;
		int num = fileNameWithoutExtension.Length + 1;
		return text.Substring(num, text.Length - num);
	}

	public virtual void DrawEquipment(string filename, Color renderColor)
	{
		MapInstance mapInstance = Intersect.Client.Maps.MapInstance.Get(MapId);
		if (mapInstance == null || mapInstance.HideEquipment)
		{
			return;
		}
		SpriteSheet sheet = Sheet;
		SpriteAnimations spriteAnimation;
		if (sheet != null)
		{
			SpriteSheetAnimation currentBand = CurrentBand;
			if (currentBand != null)
			{
				IGameTexture texture = Globals.ContentManager.GetTexture(TextureType.Paperdoll, filename);
				if (texture != null)
				{
					spriteAnimation = SpriteAnimation;
					bool flag = ((spriteAnimation == SpriteAnimations.Normal || spriteAnimation == SpriteAnimations.Run) ? true : false);
					int num = (flag ? NormalSpriteAnimationFrame : SpriteFrame);
					int num2 = PickSpriteRow(DirectionFacing);
					int num3 = 0;
					int num4 = 0;
					float x;
					float y;
					float num9;
					float num10;
					if (Texture != null && texture.Width == Texture.Width && texture.Height == Texture.Height)
					{
						(int X, int Y, int Width, int Height) tuple = sheet.FrameRect(currentBand, num, num2);
						float num5 = tuple.X;
						float num6 = tuple.Y;
						float num7 = tuple.Width;
						float num8 = tuple.Height;
						x = num5;
						y = num6;
						num9 = num7;
						num10 = num8;
						num3 = currentBand.OffsetX;
						num4 = currentBand.OffsetY;
					}
					else
					{
						int num11 = Math.Max(1, Options.Instance.Sprites.NormalFrames);
						num9 = (float)texture.Width / (float)num11;
						num10 = (float)texture.Height / (float)Options.Instance.Sprites.Directions;
						x = (float)Math.Clamp(num, 0, num11 - 1) * num9;
						y = (float)num2 * num10;
					}
					DrawOutfitLayer(texture, new FloatRect(x, y, num9, num10), new FloatRect((int)Math.Ceiling(Origin.X - num9 / 2f) + num3 + _runShift.X, (int)Math.Ceiling(Origin.Y - num10) + num4 + _runShift.Y, num9, num10), renderColor);
				}
				return;
			}
		}
		IGameTexture gameTexture = null;
		int num12 = SpriteFrames;
		string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(filename);
		spriteAnimation = SpriteAnimation;
		if ((uint)(spriteAnimation - 2) <= 3u)
		{
			string text = Paperdolls.SansCouleur(Path.GetFileNameWithoutExtension(AnimatedTextures[SpriteAnimation].Name));
			int num13 = text.IndexOf('_') + 1;
			string text2 = text;
			int num14 = num13;
			string text3 = text2.Substring(num14, text2.Length - num14);
			IGameTexture texture2 = Globals.ContentManager.GetTexture(TextureType.Paperdoll, fileNameWithoutExtension + "_" + text3 + ".png");
			if (texture2 != null)
			{
				gameTexture = texture2;
			}
		}
		if (gameTexture == null)
		{
			string text4 = DrawnForm();
			if (text4 != null)
			{
				gameTexture = Globals.ContentManager.GetTexture(TextureType.Paperdoll, $"{fileNameWithoutExtension}_{text4}_{SpriteAnimation}.png");
				if (gameTexture == null && SpriteAnimation == SpriteAnimations.Normal)
				{
					gameTexture = Globals.ContentManager.GetTexture(TextureType.Paperdoll, fileNameWithoutExtension + "_" + text4 + ".png");
					num12 = Options.Instance.Sprites.NormalFrames;
				}
			}
		}
		if (gameTexture == null && !string.IsNullOrEmpty($"{SpriteAnimation}"))
		{
			gameTexture = Globals.ContentManager.GetTexture(TextureType.Paperdoll, $"{fileNameWithoutExtension}_{SpriteAnimation}.png");
		}
		bool flag2 = false;
		if (gameTexture == null)
		{
			gameTexture = Globals.ContentManager.GetTexture(TextureType.Paperdoll, filename);
			num12 = Options.Instance.Sprites.NormalFrames;
			flag2 = true;
		}
		if (gameTexture == null)
		{
			return;
		}
		int num15 = PickSpriteRow(DirectionFacing);
		int num16 = gameTexture.Width / num12;
		int num17 = gameTexture.Height / Options.Instance.Sprites.Directions;
		int num18 = SpriteFrame;
		spriteAnimation = SpriteAnimation;
		if ((spriteAnimation == SpriteAnimations.Normal || spriteAnimation == SpriteAnimations.Run) ? true : false)
		{
			num18 = NormalSpriteAnimationFrame;
		}
		else if (flag2)
		{
			num18 = Math.Clamp(NormalSpriteAnimationFrame, 0, num12 - 1);
		}
		FloatRect source = new FloatRect(num18 * num16, num15 * num17, num16, num17);
		FloatRect? bodyDestination = _bodyDestination;
		FloatRect floatRect;
		if (bodyDestination.HasValue)
		{
			FloatRect valueOrDefault = bodyDestination.GetValueOrDefault();
			if (Math.Abs(valueOrDefault.Width - (float)num16) < 0.5f && Math.Abs(valueOrDefault.Height - (float)num17) < 0.5f)
			{
				floatRect = new FloatRect(valueOrDefault.X, valueOrDefault.Y, num16, num17);
				goto IL_0501;
			}
		}
		floatRect = new FloatRect((int)Math.Ceiling(Center.X - (float)num16 / 2f), (int)Math.Ceiling(Center.Y - (float)num17 / 2f), source.Width, source.Height);
		goto IL_0501;
		IL_0501:
		FloatRect destination = floatRect;
		DrawOutfitLayer(gameTexture, source, destination, renderColor);
	}

	private void DrawPoseEquipment(string filename, Color renderColor, IGameTexture body, FloatRect source)
	{
		MapInstance mapInstance = Intersect.Client.Maps.MapInstance.Get(MapId);
		if (mapInstance == null || mapInstance.HideEquipment)
		{
			return;
		}
		FloatRect? bodyDestination = _bodyDestination;
		if (!bodyDestination.HasValue)
		{
			return;
		}
		FloatRect valueOrDefault = bodyDestination.GetValueOrDefault();
		if (!string.IsNullOrEmpty(TransformedSprite) && !Transformation.KeepsPaperdolls(this))
		{
			return;
		}
		string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(Sprite);
		string fileNameWithoutExtension2 = Path.GetFileNameWithoutExtension(body.Name);
		if (!string.IsNullOrEmpty(fileNameWithoutExtension) && fileNameWithoutExtension2 != null && fileNameWithoutExtension2.Length > fileNameWithoutExtension.Length + 1 && fileNameWithoutExtension2.StartsWith(fileNameWithoutExtension + "_", StringComparison.OrdinalIgnoreCase))
		{
			GameContentManager? contentManager = Globals.ContentManager;
			string? fileNameWithoutExtension3 = Path.GetFileNameWithoutExtension(filename);
			string text = fileNameWithoutExtension2;
			int num = fileNameWithoutExtension.Length + 1;
			IGameTexture texture = contentManager.GetTexture(TextureType.Paperdoll, fileNameWithoutExtension3 + "_" + text.Substring(num, text.Length - num) + ".png");
			if (texture != null && texture.Width == body.Width && texture.Height == body.Height)
			{
				DrawOutfitLayer(texture, source, valueOrDefault, renderColor);
			}
		}
	}

	private bool DrawWrapEquipment(string filename, Color renderColor, IGameTexture body, FloatRect source)
	{
		if (!string.IsNullOrEmpty(filename))
		{
			FloatRect? bodyDestination = _bodyDestination;
			if (bodyDestination.HasValue)
			{
				FloatRect valueOrDefault = bodyDestination.GetValueOrDefault();
				if (!(source.Width < 1f) && !(source.Height < 1f))
				{
					IGameTexture texture = Globals.ContentManager.GetTexture(TextureType.Paperdoll, Path.GetFileNameWithoutExtension(filename) + "-" + Paperdolls.PlancheDesEnveloppes(Path.GetFileNameWithoutExtension(body.Name)) + ".png");
					if (texture == null)
					{
						return false;
					}
					MapInstance mapInstance = Intersect.Client.Maps.MapInstance.Get(MapId);
					if (mapInstance == null || mapInstance.HideEquipment)
					{
						return true;
					}
					int num = (int)Math.Round((float)body.Width / source.Width);
					int num2 = (int)Math.Round((float)body.Height / source.Height);
					if (num < 1 || num2 < 1)
					{
						return true;
					}
					float num3 = (float)texture.Width / (float)num;
					float num4 = (float)texture.Height / (float)num2;
					float num5 = (num3 - source.Width) / 2f;
					float num6 = (num4 - source.Height) / 2f;
					float num7 = valueOrDefault.Width / source.Width;
					float num8 = valueOrDefault.Height / source.Height;
					float num9 = num4 - num6;
					DrawOutfitLayer(texture, new FloatRect(MathF.Round(source.X / source.Width) * num3, MathF.Round(source.Y / source.Height) * num4, num3, num9), new FloatRect(valueOrDefault.X - num5 * num7, valueOrDefault.Y - num6 * num8, num3 * num7, num9 * num8), renderColor, num9 / source.Height);
					return true;
				}
			}
		}
		return false;
	}

	private static bool AUneEnveloppe(string filename, IGameTexture body)
	{
		if (!string.IsNullOrEmpty(filename))
		{
			return Globals.ContentManager.GetTexture(TextureType.Paperdoll, Path.GetFileNameWithoutExtension(filename) + "-" + Paperdolls.PlancheDesEnveloppes(Path.GetFileNameWithoutExtension(body.Name)) + ".png") != null;
		}
		return false;
	}

	private (string Paperdoll, Color Color)? CalquePorte(int equipSlot)
	{
		if (equipSlot < 0 || equipSlot >= Equipment.Length)
		{
			return null;
		}
		Guid id = Guid.Empty;
		if (this == Globals.Me)
		{
			int num = MyEquipment[equipSlot];
			if (num > -1 && num < Options.Instance.Player.MaxInventory)
			{
				id = Inventory[num].ItemId;
			}
		}
		else
		{
			id = Equipment[equipSlot];
		}
		if (!DatabaseObject<ItemDescriptor>.TryGet(id, out var databaseObject))
		{
			return null;
		}
		string text = Paperdolls.ForClass((Gender == Gender.Male) ? databaseObject.MalePaperdoll : databaseObject.FemalePaperdoll, (this is Player player) ? player.Class : Guid.Empty);
		if (!string.IsNullOrEmpty(text))
		{
			return (text, databaseObject.Color);
		}
		return null;
	}

	private void DrawOutfitLayer(IGameTexture outfit, FloatRect source, FloatRect destination, Color color, float leanScale = 1f)
	{
		if (_runLean == 0f)
		{
			Meteo.DessinerCouche(this, outfit, source, destination, color, _immersion);
			return;
		}
		DrawLeaning(outfit, source, destination, color, _runLean * leanScale, GameBlendModes.None, _immersion, 0.5f);
		Meteo.DessinerImmerge(outfit, source, destination, color, _immersion);
		Meteo.RetenirCouche(this, outfit, source, destination, color);
	}

	protected virtual void CalculateOrigin()
	{
		IMapInstance? latestMap = LatestMap;
		if (latestMap != null && latestMap.IsDisposed)
		{
			LatestMap = (Intersect.Client.Maps.MapInstance.TryGet(MapId, out MapInstance instance) ? instance : null);
		}
		if (LatestMap == null)
		{
			mOrigin = default(Vector2);
		}
		else
		{
			mOrigin = new Vector2((float)Math.Round((float)(LatestMap.X + X * TileWidth) + OffsetX + (float)TileWidth / 2f + Flight.ShiftX(this)), (float)Math.Round((float)(LatestMap.Y + Y * TileHeight) + OffsetY + (float)TileHeight + Flight.ShiftY(this) - Flight.Altitude(this)));
		}
	}

	public virtual float GetTop(int overrideHeight = -1)
	{
		if (LatestMap == null)
		{
			return 0f;
		}
		int num = (int)Math.Ceiling(Origin.Y);
		if (overrideHeight > -1)
		{
			num -= overrideHeight / Options.Instance.Sprites.Directions;
		}
		else if (Texture != null)
		{
			num -= BodyHeight;
		}
		return num;
	}

	public void DrawLabels(string label, int position, Color labelColor, Color textColor, Color? borderColor = null, Color? backgroundColor = null)
	{
		if (!ShouldDrawName || string.IsNullOrWhiteSpace(label) || Graphics.Renderer == null)
		{
			return;
		}
		IMapInstance mapInstance = MapInstance;
		if (mapInstance != null)
		{
			if (borderColor == null)
			{
				borderColor = Intersect.Color.Transparent;
			}
			if (backgroundColor == null)
			{
				backgroundColor = Intersect.Color.Transparent;
			}
			if (labelColor != null && labelColor.A != 0)
			{
				textColor = labelColor;
			}
			Vector2 vector = TexteMonde.Mesurer(label, Graphics.EntityNameFont, TexteMonde.Taille(Graphics.EntityNameFontSize));
			int num = (int)Math.Ceiling(Origin.X);
			float y = ((position == 0) ? GetLabelLocation(LabelType.Header) : GetLabelLocation(LabelType.Footer));
			if (backgroundColor != Intersect.Color.Transparent)
			{
				Graphics.DrawGameTexture(Graphics.Renderer.WhitePixel, new FloatRect(0f, 0f, 1f, 1f), new FloatRect((float)num - vector.X / 2f - 4f, y, vector.X + 8f, vector.Y), backgroundColor);
			}
			TexteMonde.Ecrire(label, Graphics.EntityNameFont, TexteMonde.Taille(Graphics.EntityNameFontSize), (float)num - vector.X / 2f, y, Intersect.Color.FromArgb(textColor.ToArgb()), Intersect.Color.FromArgb(borderColor.ToArgb()));
		}
	}

	public virtual void DrawName(Color? textColor, Color? borderColor = null, Color? backgroundColor = null)
	{
		if (!ShouldDrawName || IsDying || Graphics.Renderer == null)
		{
			return;
		}
		if (textColor == null)
		{
			LabelColor? labelColor = Aggression switch
			{
				NpcAggression.Aggressive => CustomColors.Names.Npcs["Aggressive"], 
				NpcAggression.AttackWhenAttacked => CustomColors.Names.Npcs["AttackWhenAttacked"], 
				NpcAggression.AttackOnSight => CustomColors.Names.Npcs["AttackOnSight"], 
				NpcAggression.Guard => CustomColors.Names.Npcs["Guard"], 
				_ => CustomColors.Names.Npcs["Neutral"], 
			};
			if (labelColor.HasValue)
			{
				textColor = labelColor?.Name;
				if ((object)backgroundColor == null)
				{
					backgroundColor = labelColor?.Background;
				}
				if ((object)borderColor == null)
				{
					borderColor = labelColor?.Outline;
				}
			}
		}
		if (borderColor == null)
		{
			borderColor = Intersect.Color.Transparent;
		}
		if (backgroundColor == null)
		{
			backgroundColor = Intersect.Color.Transparent;
		}
		string text = Name;
		if (this is Player player && !string.IsNullOrWhiteSpace(player.GuildTag))
		{
			text = "[" + player.GuildTag + "] " + Name;
		}
		if ((this is Player && Options.Instance.Player.ShowLevelByName) || (Type == EntityType.GlobalEntity && Options.Instance.Npc.ShowLevelByName))
		{
			text = Strings.GameWindow.EntityNameAndLevel.ToString(text, Level);
		}
		if (this is Player || (WantsNamePlate() && !NamePlate.DrawSimple(this, text, textColor ?? Intersect.Color.White)))
		{
			Vector2 vector = TexteMonde.Mesurer(text, Graphics.EntityNameFont, TexteMonde.Taille(Graphics.EntityNameFontSize));
			int num = (int)Math.Ceiling(Origin.X);
			float labelLocation = GetLabelLocation(LabelType.Name);
			if (backgroundColor != Intersect.Color.Transparent)
			{
				Graphics.DrawGameTexture(Graphics.Renderer.WhitePixel, new FloatRect(0f, 0f, 1f, 1f), new FloatRect((float)num - vector.X / 2f - 4f, labelLocation, vector.X + 8f, vector.Y), backgroundColor);
			}
			TexteMonde.Ecrire(text, Graphics.EntityNameFont, TexteMonde.Taille(Graphics.EntityNameFontSize), (float)num - vector.X / 2f, labelLocation, textColor, Intersect.Color.FromArgb(borderColor.ToArgb()));
		}
	}

	private bool WantsNamePlate()
	{
		if (IsHovered)
		{
			return true;
		}
		Player me = Globals.Me;
		if (me == null)
		{
			return false;
		}
		if (me.TargetId == Id)
		{
			return true;
		}
		if (me.MapId == MapId)
		{
			return Math.Max(Math.Abs(me.X - X), Math.Abs(me.Y - Y)) <= 2;
		}
		return false;
	}

	internal float PlateBottomAboveHead()
	{
		float num = GetTop() - 6f;
		if (!(this is Event) && ShouldDrawHpBar)
		{
			num -= (float)(GetBoundingHpBarTexture().Height + 2);
		}
		return num;
	}

	public float GetLabelLocation(LabelType type)
	{
		if (Graphics.Renderer == null)
		{
			return 0f;
		}
		float num = GetTop() - 8f;
		if (!(this is Event) && ShouldDrawHpBar)
		{
			num -= (float)(GetBoundingHpBarTexture().Height + 2);
		}
		switch (type)
		{
		case LabelType.Header:
			num = GetLabelLocation(LabelType.Name);
			if (!string.IsNullOrWhiteSpace(HeaderLabel.Text))
			{
				num -= TexteMonde.Mesurer(HeaderLabel.Text, Graphics.EntityNameFont, TexteMonde.Taille(Graphics.EntityNameFontSize)).Y + 2f;
			}
			break;
		case LabelType.Footer:
			if (!string.IsNullOrWhiteSpace(FooterLabel.Text))
			{
				num -= TexteMonde.Mesurer(FooterLabel.Text, Graphics.EntityNameFont, TexteMonde.Taille(Graphics.EntityNameFontSize)).Y - 6f;
			}
			break;
		case LabelType.Name:
			num = GetLabelLocation(LabelType.Footer);
			num -= TexteMonde.Mesurer(Name, Graphics.EntityNameFont, TexteMonde.Taille(Graphics.EntityNameFontSize)).Y + (float)(string.IsNullOrEmpty(FooterLabel.Text) ? (-6) : 2);
			break;
		case LabelType.ChatBubble:
			num = (ShouldDrawName ? (GetLabelLocation(LabelType.Guild) - 2f) : (num - 2f));
			break;
		case LabelType.Guild:
			if (this is Player player)
			{
				num = ((!string.IsNullOrWhiteSpace(HeaderLabel.Text)) ? GetLabelLocation(LabelType.Header) : GetLabelLocation(LabelType.Name));
				if (!string.IsNullOrWhiteSpace(player.Guild))
				{
					num -= 2f + TexteMonde.Mesurer(player.Guild, Graphics.EntityNameFont, TexteMonde.Taille(Graphics.EntityNameFontSize)).Y;
				}
			}
			break;
		}
		return num;
	}

	public IGameTexture GetBoundingHpBarTexture()
	{
		return GameTexture.GetBoundingTexture(BoundsComparison.Height, Globals.ContentManager.GetTexture(TextureType.Misc, "hpbackground.png"), Globals.ContentManager.GetTexture(TextureType.Misc, "hpbar.png"), Globals.ContentManager.GetTexture(TextureType.Misc, "shieldbar.png"));
	}

	public void DrawHpBar()
	{
		if (ShouldDrawHpBar && !IsDying)
		{
			IGameTexture texture = Globals.ContentManager.GetTexture(TextureType.Misc, "hpbackground.png");
			IGameTexture texture2 = Globals.ContentManager.GetTexture(TextureType.Misc, "hpbar.png");
			IGameTexture texture3 = Globals.ContentManager.GetTexture(TextureType.Misc, "shieldbar.png");
			IGameTexture boundingTexture = GameTexture.GetBoundingTexture(BoundsComparison.Height, texture, texture2, texture3);
			IGameTexture gameTexture = texture2;
			long num = MaxVital[0];
			long shieldSize = ShieldSize;
			if (shieldSize + Vital[0] > num)
			{
				num = shieldSize + Vital[0];
			}
			float val = (float)Vital[0] / (float)num;
			val = Math.Min(1f, Math.Max(0f, val));
			int num2 = (int)Math.Round(val * (float)gameTexture.Width);
			float num3 = (float)(gameTexture.Width - num2) / (float)gameTexture.Width;
			int num4 = (int)Math.Floor((float)gameTexture.Width * num3);
			if (gameTexture.Width < num2 + num4)
			{
				num2 = Math.Max(1, gameTexture.Width - num4);
				num4 = Math.Max(1, gameTexture.Width - num2);
			}
			if (shieldSize > 0 && num4 < 1)
			{
				num2 -= 1 - num4;
				num4 = 1;
			}
			int num5 = (int)Math.Ceiling(Origin.X);
			int num6 = (int)Math.Ceiling(Origin.Y);
			IGameTexture texture4 = Globals.ContentManager.GetTexture(TextureType.Entity, Sprite);
			if (texture4 != null)
			{
				num6 -= BodyHeight;
				num6 -= boundingTexture.Height + 2;
			}
			num6 += boundingTexture.Height / 2;
			if (texture != null)
			{
				Graphics.DrawGameTexture(texture, new FloatRect(0f, 0f, texture.Width, texture.Height), new FloatRect(num5 - texture.Width / 2, num6 - texture.Height / 2, texture.Width, texture.Height), Intersect.Color.White);
			}
			if (texture2 != null)
			{
				Graphics.DrawGameTexture(texture2, new FloatRect(0f, 0f, num2, texture2.Height), new FloatRect(num5 - gameTexture.Width / 2, num6 - texture2.Height / 2, num2, texture2.Height), Intersect.Color.White);
			}
			if (shieldSize > 0 && texture3 != null)
			{
				float num7 = (float)num4 / (float)gameTexture.Width;
				Graphics.DrawGameTexture(texture3, new FloatRect((float)texture3.Width * (1f - num7), 0f, (float)texture3.Width * num7, texture3.Height), new FloatRect(num5 - gameTexture.Width / 2 + num2, num6 - texture3.Height / 2, num4, texture3.Height), Intersect.Color.White);
			}
		}
	}

	public void DrawCastingBar()
	{
		if (!ShouldDrawCastingBar)
		{
			return;
		}
		SpellDescriptor spellDescriptor = DatabaseObject<SpellDescriptor>.Get(SpellCast);
		if (spellDescriptor != null)
		{
			IGameTexture texture = Globals.ContentManager.GetTexture(TextureType.Misc, "castbackground.png");
			IGameTexture texture2 = Globals.ContentManager.GetTexture(TextureType.Misc, "castbar.png");
			IGameTexture boundingTexture = GameTexture.GetBoundingTexture(BoundsComparison.Height, texture, texture2);
			float num = CastTime - Timing.Global.Milliseconds;
			float num2 = 1f - num / (float)Math.Max(1, (CastDuration > 0) ? CastDuration : spellDescriptor.CastDuration);
			int num3 = (int)Math.Ceiling(Origin.X);
			int num4 = (int)Math.Ceiling(Origin.Y);
			num4 += 2 + boundingTexture.Height / 2;
			if (texture != null)
			{
				Graphics.DrawGameTexture(texture, new FloatRect(0f, 0f, texture.Width, texture.Height), new FloatRect(num3 - texture.Width / 2, num4 - texture.Height / 2, texture.Width, texture.Height), Intersect.Color.White);
			}
			if (texture2 != null)
			{
				Graphics.DrawGameTexture(texture2, new FloatRect(0f, 0f, (float)texture2.Width * num2, texture2.Height), new FloatRect(num3 - texture2.Width / 2, num4 - texture2.Height / 2, (float)texture2.Width * num2, texture2.Height), Intersect.Color.White);
			}
		}
	}

	public void DrawTarget(int priority)
	{
		if (ShouldDrawTarget)
		{
			IGameTexture texture = Globals.ContentManager.GetTexture(TextureType.Misc, "target.png");
			if (texture != null)
			{
				FloatRect srcRectangle = new FloatRect((float)(priority * texture.Width) / 2f, 0f, (float)texture.Width / 2f, texture.Height);
				FloatRect targetRect = new FloatRect((float)Math.Ceiling(WorldPos.X + (WorldPos.Width - (float)(texture.Width / 2)) / 2f), (float)Math.Ceiling(WorldPos.Y + WorldPos.Height - (float)texture.Height / 2f - 4f), srcRectangle.Width, srcRectangle.Height);
				Graphics.DrawGameTexture(texture, srcRectangle, targetRect, Intersect.Color.White);
			}
		}
	}

	public void DrawAttackTelegraph()
	{
		if (this == Globals.Me || Type != EntityType.GlobalEntity || !ShouldDraw || LatestMap == null || Options.Instance.Combat.ArmementMs > 0)
		{
			return;
		}
		Guid? guid = Globals.Me?.TargetId;
		Guid id = Id;
		if (!guid.HasValue || guid.GetValueOrDefault() != id || Aggression == NpcAggression.Neutral || AttackTime <= 0)
		{
			return;
		}
		IGameTexture texture = Globals.ContentManager.GetTexture(TextureType.Misc, "castbackground.png");
		IGameTexture texture2 = Globals.ContentManager.GetTexture(TextureType.Misc, "castbar.png");
		if (texture != null && texture2 != null)
		{
			long milliseconds = Timing.Global.Milliseconds;
			float num = ((AttackTimer <= milliseconds) ? 1f : Math.Clamp(1f - (float)(AttackTimer - milliseconds) / (float)AttackTime, 0f, 1f));
			int a = ((num >= 1f) ? ((int)(200f + 55f * MathF.Sin((float)milliseconds / 120f))) : 255);
			Color renderColor = new Color(a, 255, 122, 26);
			int num2 = (int)Math.Ceiling(Origin.X);
			int num3 = (int)Math.Ceiling(Origin.Y);
			IGameTexture texture3 = Globals.ContentManager.GetTexture(TextureType.Entity, Sprite);
			if (texture3 != null)
			{
				num3 -= BodyHeight;
			}
			num3 += texture.Height / 2 + 1;
			Graphics.DrawGameTexture(texture, new FloatRect(0f, 0f, texture.Width, texture.Height), new FloatRect(num2 - texture.Width / 2, num3 - texture.Height / 2, texture.Width, texture.Height), Intersect.Color.White);
			Graphics.DrawGameTexture(texture2, new FloatRect(0f, 0f, (float)texture2.Width * num, texture2.Height), new FloatRect(num2 - texture2.Width / 2, num3 - texture2.Height / 2, (float)texture2.Width * num, texture2.Height), renderColor);
		}
	}

	public void DrawCastArea()
	{
		if (!IsCasting || !ShouldDraw || !(LatestMap is MapInstance mapInstance))
		{
			return;
		}
		SpellDescriptor spellDescriptor = DatabaseObject<SpellDescriptor>.Get(SpellCast);
		if (spellDescriptor == null || spellDescriptor.Combat.HitRadius <= 0)
		{
			return;
		}
		MapInstance mapInstance3;
		int num;
		int num2;
		switch (spellDescriptor.Combat.TargetType)
		{
		case SpellTargetType.AoE:
		{
			byte x2 = X;
			byte y2 = Y;
			mapInstance3 = mapInstance;
			num = y2;
			num2 = x2;
			break;
		}
		case SpellTargetType.Single:
		{
			if (this != Globals.Me || !(Globals.Me.TargetId != Guid.Empty) || !Globals.Entities.TryGetValue(Globals.Me.TargetId, out Entity value) || !(value.LatestMap is MapInstance mapInstance2))
			{
				return;
			}
			byte x = value.X;
			byte y = value.Y;
			mapInstance3 = mapInstance2;
			num = y;
			num2 = x;
			break;
		}
		default:
			return;
		}
		IGameTexture texture = Globals.ContentManager.GetTexture(TextureType.Misc, "aoe.png");
		if (texture == null)
		{
			return;
		}
		int hitRadius = spellDescriptor.Combat.HitRadius;
		int tileWidth = Options.Instance.Map.TileWidth;
		int tileHeight = Options.Instance.Map.TileHeight;
		int a = (int)(170f + 85f * MathF.Sin((float)Timing.Global.Milliseconds / 150f));
		Color renderColor = new Color(a, 255, 255, 255);
		for (int i = -hitRadius; i <= hitRadius; i++)
		{
			for (int j = -hitRadius; j <= hitRadius; j++)
			{
				int num3 = num2 + i;
				int num4 = num + j;
				if (num3 >= 0 && num4 >= 0 && num3 < Options.Instance.Map.MapWidth && num4 < Options.Instance.Map.MapHeight)
				{
					Graphics.DrawGameTexture(texture, new FloatRect(0f, 0f, texture.Width, texture.Height), new FloatRect(mapInstance3.X + num3 * tileWidth, mapInstance3.Y + num4 * tileHeight, tileWidth, tileHeight), renderColor);
				}
			}
		}
	}

	public void AddChatBubble(string text)
	{
		if (!string.IsNullOrEmpty(text))
		{
			mChatBubbles.Add(new ChatBubble(this, text));
		}
	}

	public bool StatusActive(Guid guid)
	{
		foreach (IStatus item in Status)
		{
			if (item.SpellId == guid && item.IsActive)
			{
				return true;
			}
		}
		return false;
	}

	public IStatus? GetStatus(Guid guid)
	{
		foreach (IStatus item in Status)
		{
			if (item.SpellId == guid && item.IsActive)
			{
				return item;
			}
		}
		return null;
	}

	public void SortStatuses()
	{
		List<IStatus> list = new List<IStatus>();
		list.AddRange(Status.OrderByDescending((IStatus x) => x.RemainingMs));
		Status = list;
	}

	private FloatRect WallBumpPose(FloatRect destination)
	{
		if (WallBumpAt < 0)
		{
			return destination;
		}
		long num = Timing.Global.Milliseconds - WallBumpAt;
		if (num >= 140)
		{
			WallBumpAt = -1L;
			return destination;
		}
		float num2 = ((num < 50) ? ((float)num / 50f) : (1f - (float)(num - 50) / 90f));
		Point deltaPoint = WallBumpDirection.GetDeltaPoint();
		float num3 = MathF.Round(2f * num2);
		return new FloatRect(destination.X + (float)deltaPoint.X * num3, destination.Y + (float)deltaPoint.Y * num3, destination.Width, destination.Height);
	}

	public void PlaySpellPose(long durationMs)
	{
		long milliseconds = Timing.Global.Milliseconds;
		if (milliseconds >= SpellPoseUntil)
		{
			_spellPoseStartedAt = milliseconds;
		}
		SpellPoseUntil = Math.Max(SpellPoseUntil, milliseconds + durationMs);
	}

	private bool IsCastingProjectileSpell()
	{
		if (IsCasting)
		{
			SpellDescriptor spellDescriptor = DatabaseObject<SpellDescriptor>.Get(SpellCast);
			if (spellDescriptor != null && spellDescriptor.SpellType == SpellType.CombatSpell)
			{
				return spellDescriptor.Combat.TargetType == SpellTargetType.Projectile;
			}
		}
		return false;
	}

	internal static int SheetColumns(IGameTexture? sheet, IGameTexture? baseSheet, int fallback)
	{
		if (sheet == null || sheet.Width <= 0 || sheet.Height <= 0 || baseSheet == null || baseSheet.Width <= 0 || baseSheet.Height <= 0)
		{
			return fallback;
		}
		int num = Math.Max(1, Options.Instance.Sprites.Directions);
		int num2 = baseSheet.Width / Math.Max(1, Options.Instance.Sprites.NormalFrames);
		int num3 = baseSheet.Height / num;
		int num4 = ((num3 > 0) ? (num2 * (sheet.Height / num) / num3) : num2);
		if (num4 <= 0 || sheet.Width % num4 != 0)
		{
			return fallback;
		}
		return Math.Max(1, sheet.Width / num4);
	}

	private int SpellPoseColumns()
	{
		if (!AnimatedTextures.TryGetValue(SpriteAnimations.Spell, out IGameTexture value) || value == null || Texture == null)
		{
			return 1;
		}
		int num = Math.Max(1, Options.Instance.Sprites.Directions);
		int num2 = Texture.Width / Math.Max(1, Options.Instance.Sprites.NormalFrames);
		int num3 = Texture.Height / num;
		int num4 = value.Height / num;
		int num5 = ((num3 > 0) ? (num2 * num4 / num3) : num2);
		if (num5 <= 0)
		{
			return 1;
		}
		return Math.Max(1, value.Width / num5);
	}

	internal void NoteRemoteStep()
	{
		long milliseconds = Timing.Global.Milliseconds;
		float movementTime = GetMovementTime();
		if (_remoteStepAt >= 0)
		{
			float num = (float)(milliseconds - _remoteStepAt) - movementTime;
			if (num < movementTime)
			{
				_remoteStepLateMs += (Math.Max(0f, num) - _remoteStepLateMs) * 0.2f;
			}
		}
		_remoteStepAt = milliseconds;
		_remoteStepDistance = Math.Max(Math.Abs(OffsetX), Math.Abs(OffsetY));
	}

	private void RollAnimationVariantsForNewAction()
	{
		if (AttackTimer != _variantsRolledForAttackTimer)
		{
			_variantsRolledForAttackTimer = AttackTimer;
			PickAnimationVariant(SpriteAnimations.Attack);
			PickAnimationVariant(SpriteAnimations.Weapon);
			PickAnimationVariant(SpriteAnimations.Shoot);
		}
		if (SpellCast != _variantsRolledForSpellCast)
		{
			_variantsRolledForSpellCast = SpellCast;
			PickAnimationVariant(SpriteAnimations.Cast);
		}
		long milliseconds = Timing.Global.Milliseconds;
		bool flag = !IsBlocking && !IsCasting;
		bool flag2 = flag && AnimatedTextures.ContainsKey(SpriteAnimations.Idle) && LastActionTime + Options.Instance.Sprites.IdleStartDelay < milliseconds;
		if (flag2 && !_wasIdle)
		{
			PickAnimationVariant(SpriteAnimations.Idle);
		}
		_wasIdle = flag2;
		bool flag3 = flag && AnimatedTextures.ContainsKey(SpriteAnimations.Afk) && DerniereActiviteAfk + Options.Instance.Sprites.AfkStartDelay < milliseconds;
		if (flag3 && !_wasAfk)
		{
			PickAnimationVariant(SpriteAnimations.Afk);
			SpriteFrame = 0;
			SpriteFrameTimer = milliseconds;
		}
		_wasAfk = flag3;
	}

	private void PickAnimationVariant(SpriteAnimations spriteAnimation)
	{
		bool flag = spriteAnimation == SpriteAnimations.Attack && (this is Player || PasDuCoup > 0);
		if (Sheet != null)
		{
			if (!_sheetVariants.TryGetValue(spriteAnimation, out List<SpriteSheetAnimation> value) || value.Count <= 0)
			{
				return;
			}
			if (flag)
			{
				int? num = PlancheDuPas(value.Count);
				if (num.HasValue)
				{
					int valueOrDefault = num.GetValueOrDefault();
					_sheetBands[spriteAnimation] = value[valueOrDefault];
					return;
				}
			}
			if (value.Count > 1)
			{
				_sheetBands[spriteAnimation] = value[Randomization.Next(0, value.Count)];
			}
		}
		else
		{
			if (!AnimationVariants.TryGetValue(spriteAnimation, out List<IGameTexture> value2) || value2.Count == 0)
			{
				return;
			}
			if (flag)
			{
				int? num = PlancheDuPas(value2.Count);
				if (num.HasValue)
				{
					int valueOrDefault2 = num.GetValueOrDefault();
					AnimatedTextures[spriteAnimation] = value2[valueOrDefault2];
					return;
				}
			}
			if (value2.Count >= 2)
			{
				AnimatedTextures[spriteAnimation] = value2[Randomization.Next(0, value2.Count)];
			}
		}
	}

	private long SprintPoseSlack()
	{
		if (this != Globals.Me)
		{
			return 90 + Math.Clamp((long)(_remoteStepLateMs * 1.5f), 0L, 250L);
		}
		if (DirectionMoving > Direction.None)
		{
			return (long)GetMovementTime() + Options.Instance.Sprites.RunFrameDuration;
		}
		return 90L;
	}

	private void UpdateSpriteAnimation()
	{
		if (AnimatedTextures.Count == 0)
		{
			SpriteAnimation = SpriteAnimations.Normal;
			return;
		}
		RollAnimationVariantsForNewAction();
		long milliseconds = Timing.Global.Milliseconds;
		if (IsTransforming)
		{
			LastActionTime = milliseconds;
			if (AnimatedTextures.ContainsKey(SpriteAnimations.Transform))
			{
				SpriteAnimation = SpriteAnimations.Transform;
				int num = Math.Max(1, Transformation.SequenceOf(this, TransformedSprite));
				long num2 = num - (TransformingUntil - milliseconds);
				SpriteFrame = Math.Clamp((int)(num2 * SpriteFrames / num), 0, SpriteFrames - 1);
				return;
			}
			if (AnimatedTextures.ContainsKey(SpriteAnimations.Ki) || Transformation.BaseKiPose(this) != null)
			{
				SpriteAnimation = SpriteAnimations.Ki;
				AdvanceTimedFrame(Options.Instance.Sprites.KiFrameDuration, loops: true, milliseconds);
				return;
			}
		}
		bool flag = !IsBlocking && !IsCasting;
		SpriteAnimation = SpriteAnimations.Normal;
		if (AnimatedTextures.TryGetValue(SpriteAnimations.Idle, out IGameTexture value) && LastActionTime + Options.Instance.Sprites.IdleStartDelay < milliseconds && flag)
		{
			SpriteAnimation = SpriteAnimations.Idle;
		}
		if (AnimatedTextures.ContainsKey(SpriteAnimations.Afk) && DerniereActiviteAfk + Options.Instance.Sprites.AfkStartDelay < milliseconds && flag)
		{
			SpriteAnimation = SpriteAnimations.Afk;
		}
		if (IsMoving && !IsAttacking && flag)
		{
			SpriteAnimation = SpriteAnimations.Normal;
			LastActionTime = milliseconds;
			_lastMovementTime = milliseconds;
		}
		if (Dashing != null && !IsAttacking && flag)
		{
			SpriteAnimation = SpriteAnimations.Normal;
			LastActionTime = milliseconds;
		}
		if (FlightMode == FlightMode.Cloud && !IsAttacking && flag && AnimatedTextures.ContainsKey(SpriteAnimations.Idle))
		{
			SpriteAnimation = SpriteAnimations.Idle;
		}
		if (IsSprinting && !IsAttacking && flag && FlightMode != FlightMode.Cloud && AnimatedTextures.ContainsKey(SpriteAnimations.Run) && milliseconds - _lastMovementTime <= SprintPoseSlack())
		{
			SpriteAnimation = SpriteAnimations.Run;
		}
		if (IsCharging && !IsAttacking && flag)
		{
			LastActionTime = milliseconds;
			if (AnimatedTextures.ContainsKey(SpriteAnimations.Ki))
			{
				SpriteAnimation = SpriteAnimations.Ki;
			}
			else if (AnimatedTextures.ContainsKey(SpriteAnimations.Idle))
			{
				SpriteAnimation = SpriteAnimations.Idle;
				SpriteFrame = 0;
			}
		}
		if (IsBlocking && Options.Instance.Combat.EnableGuard && AnimatedTextures.ContainsKey(SpriteAnimations.Block))
		{
			LastActionTime = milliseconds;
			SpriteAnimation = SpriteAnimations.Block;
		}
		bool hasValue = Recolte.PoseDePeche(this).HasValue;
		if (hasValue)
		{
			SpriteAnimation = SpriteAnimations.Normal;
			LastActionTime = milliseconds;
		}
		if (IsAttacking && flag && !hasValue)
		{
			if (AnimatedTextures.TryGetValue(SpriteAnimations.Normal, out value))
			{
				return;
			}
			long num3 = DureeDuGeste() - (AttackTimer - milliseconds);
			LastActionTime = Timing.Global.Milliseconds;
			if (AnimatedTextures.TryGetValue(SpriteAnimations.Attack, out value))
			{
				SpriteAnimation = SpriteAnimations.Attack;
			}
			if (Options.Instance.Equipment.WeaponSlot > -1 && Options.Instance.Equipment.WeaponSlot < Equipment.Length && ((Equipment[Options.Instance.Equipment.WeaponSlot] != Guid.Empty && this != Globals.Me) || MyEquipment[Options.Instance.Equipment.WeaponSlot] < Options.Instance.Player.MaxInventory))
			{
				Guid id = Guid.Empty;
				if (this == Globals.Me)
				{
					int num4 = MyEquipment[Options.Instance.Equipment.WeaponSlot];
					if (num4 > -1)
					{
						id = Inventory[num4].ItemId;
					}
				}
				else
				{
					id = Equipment[Options.Instance.Equipment.WeaponSlot];
				}
				ItemDescriptor itemDescriptor = DatabaseObject<ItemDescriptor>.Get(id);
				if (itemDescriptor != null)
				{
					if (AnimatedTextures.TryGetValue(SpriteAnimations.Weapon, out value))
					{
						SpriteAnimation = SpriteAnimations.Weapon;
					}
					if (AnimatedTextures.TryGetValue(SpriteAnimations.Shoot, out value) && itemDescriptor.ProjectileId != Guid.Empty && itemDescriptor.WeaponSpriteOverride == null)
					{
						SpriteAnimation = SpriteAnimations.Shoot;
					}
				}
			}
			switch (SpriteAnimation)
			{
			default:
			{
				int? num5 = Recolte.ImageDeFrappe(this);
				int spriteFrame;
				if (num5.HasValue)
				{
					int valueOrDefault = num5.GetValueOrDefault();
					spriteFrame = Math.Min(valueOrDefault, SpriteFrames - 1);
				}
				else
				{
					spriteFrame = (int)Math.Floor((float)num3 / ((float)DureeDuGeste() / (float)SpriteFrames));
				}
				SpriteFrame = spriteFrame;
				if (Heritage.Actif)
				{
					num5 = Heritage.ImageDuCoup(this, num3);
					if (num5.HasValue)
					{
						int valueOrDefault2 = num5.GetValueOrDefault();
						SpriteFrame = Math.Min(valueOrDefault2, SpriteFrames - 1);
					}
				}
				break;
			}
			case SpriteAnimations.Normal:
			case SpriteAnimations.Idle:
			case SpriteAnimations.Cast:
			case SpriteAnimations.Run:
			case SpriteAnimations.Afk:
			case SpriteAnimations.Ki:
			case SpriteAnimations.Block:
				break;
			}
		}
		if (Artisanat.EnTravail(this))
		{
			LastActionTime = milliseconds;
			int? num5 = Artisanat.ImageDeTravail(this, SpriteFrames);
			if (num5.HasValue)
			{
				int valueOrDefault3 = num5.GetValueOrDefault();
				if (AnimatedTextures.ContainsKey(SpriteAnimations.Attack))
				{
					SpriteAnimation = SpriteAnimations.Attack;
					SpriteFrame = valueOrDefault3;
				}
			}
		}
		if (!IsAttacking && AnimatedTextures.ContainsKey(SpriteAnimations.Attack) && this is Player player && player.PoseDeLAppui(out var pas))
		{
			ChoisirLaPlancheDuPas(pas);
			LastActionTime = milliseconds;
			SpriteAnimation = SpriteAnimations.Attack;
			SpriteFrame = 0;
		}
		if (!IsAttacking && AnimatedTextures.ContainsKey(SpriteAnimations.Attack) && Echanges.PoseDeCombat(this))
		{
			LastActionTime = milliseconds;
			SpriteAnimation = SpriteAnimations.Attack;
			SpriteFrame = Echanges.ImageDeCombat(this, SpriteFrames).GetValueOrDefault();
		}
		if (!IsAttacking && AnimatedTextures.ContainsKey(SpriteAnimations.Attack) && Telegraphe.EnCours(this))
		{
			LastActionTime = milliseconds;
			SpriteAnimation = SpriteAnimations.Attack;
			SpriteFrame = 0;
		}
		if (IsCasting)
		{
			SpellDescriptor spellDescriptor = DatabaseObject<SpellDescriptor>.Get(SpellCast);
			if (spellDescriptor != null)
			{
				int num6 = ((CastDuration > 0) ? CastDuration : spellDescriptor.CastDuration);
				long num7 = num6 - (CastTime - milliseconds);
				if (AnimatedTextures.TryGetValue(SpriteAnimations.Cast, out value))
				{
					SpriteAnimation = SpriteAnimations.Cast;
				}
				if (spellDescriptor.SpellType == SpellType.CombatSpell && spellDescriptor.Combat.TargetType == SpellTargetType.Projectile && spellDescriptor.CastSpriteOverride == null && AnimatedTextures.TryGetValue(SpriteAnimations.Shoot, out value))
				{
					SpriteAnimation = SpriteAnimations.Shoot;
				}
				SpriteFrame = (int)Math.Floor((float)num7 / ((float)num6 / (float)SpriteFrames));
			}
			LastActionTime = milliseconds;
		}
		if (AnimatedTextures.ContainsKey(SpriteAnimations.Spell) && (milliseconds < SpellPoseUntil || IsCastingProjectileSpell()))
		{
			SpriteAnimation = SpriteAnimations.Spell;
			int spriteFrames = SpriteFrames;
			if (spriteFrames > 1 && milliseconds < SpellPoseUntil)
			{
				long num8 = Math.Max(1L, SpellPoseUntil - _spellPoseStartedAt);
				SpriteFrame = Math.Clamp((int)((milliseconds - _spellPoseStartedAt) * spriteFrames / num8), 0, spriteFrames - 1);
			}
			else
			{
				SpriteFrame = 0;
			}
			LastActionTime = milliseconds;
		}
		if (this is Event evenement)
		{
			Levitation.Tenir(evenement, milliseconds);
		}
		if (SpriteAnimation == SpriteAnimations.Normal)
		{
			ResetSpriteFrame();
		}
		else if (SpriteAnimation == SpriteAnimations.Idle)
		{
			AdvanceTimedFrame(Options.Instance.Sprites.IdleFrameDuration, loops: true, milliseconds);
		}
		else if (SpriteAnimation == SpriteAnimations.Afk)
		{
			AdvanceTimedFrame(Options.Instance.Sprites.AfkFrameDuration, loops: false, milliseconds);
		}
		else if (SpriteAnimation == SpriteAnimations.Block)
		{
			AdvanceTimedFrame(Options.Instance.Sprites.BlockFrameDuration, loops: false, milliseconds);
		}
		else if (SpriteAnimation == SpriteAnimations.Ki)
		{
			AdvanceTimedFrame(Options.Instance.Sprites.KiFrameDuration, loops: true, milliseconds);
		}
	}

	private void AdvanceTimedFrame(int frameDuration, bool loops, long timingMilliseconds)
	{
		SpriteSheetAnimation currentBand = CurrentBand;
		if (currentBand != null)
		{
			frameDuration = currentBand.FrameTime ?? frameDuration;
			loops = currentBand.Loops || (loops && !currentBand.Holds);
		}
		if (SpriteFrame >= SpriteFrames)
		{
			SpriteFrame = 0;
			SpriteFrameTimer = timingMilliseconds;
		}
		else
		{
			if (SpriteFrameTimer + frameDuration >= timingMilliseconds)
			{
				return;
			}
			if (SpriteFrame < SpriteFrames - 1)
			{
				SpriteFrame++;
			}
			else
			{
				if (!loops)
				{
					return;
				}
				SpriteFrame = 0;
			}
			SpriteFrameTimer = timingMilliseconds;
		}
	}

	public void ResetSpriteFrame()
	{
		SpriteFrame = 0;
		SpriteFrameTimer = 0L;
	}

	public virtual void LoadTextures(string textureName)
	{
		AnimatedTextures.Clear();
		AnimationVariants.Clear();
		_sheetVariants.Clear();
		_sheetBands.Clear();
		Sheet = SpriteSheets.ForEntity(textureName);
		SpriteSheet sheet = Sheet;
		if (sheet != null)
		{
			Texture = Globals.ContentManager.GetTexture(TextureType.Entity, textureName);
			if (Texture != null)
			{
				LoadSheetBands(sheet, Texture);
				return;
			}
			Sheet = null;
		}
		foreach (SpriteAnimations value in Enum.GetValues(typeof(SpriteAnimations)))
		{
			if (value == SpriteAnimations.Normal)
			{
				Texture = Globals.ContentManager.GetTexture(TextureType.Entity, textureName);
			}
			else
			{
				LoadAnimationTexture(textureName, value);
			}
		}
	}

	private void LoadSheetBands(SpriteSheet sheet, IGameTexture texture)
	{
		SpriteAnimations[] values = Enum.GetValues<SpriteAnimations>();
		for (int i = 0; i < values.Length; i++)
		{
			SpriteAnimations spriteAnimations = values[i];
			List<SpriteSheetAnimation> list = sheet.For(spriteAnimations.ToString());
			if (list.Count == 0)
			{
				continue;
			}
			_sheetVariants[spriteAnimations] = list;
			_sheetBands[spriteAnimations] = list[0];
			if (spriteAnimations != SpriteAnimations.Normal)
			{
				AnimatedTextures[spriteAnimations] = texture;
				AnimationVariants[spriteAnimations] = list.Select((SpriteSheetAnimation _) => texture).ToList();
			}
		}
	}

	protected virtual void LoadAnimationTexture(string textureName, SpriteAnimations spriteAnimation)
	{
		if (Sheet != null)
		{
			return;
		}
		SpriteAnimations spriteAnimation2 = spriteAnimation;
		string text = string.Empty;
		int weaponSlot = Options.Instance.Equipment.WeaponSlot;
		Guid id = ((weaponSlot > -1 && weaponSlot < Equipment.Length) ? Equipment[weaponSlot] : Guid.Empty);
		switch (spriteAnimation)
		{
		case SpriteAnimations.Attack:
		{
			if (this is Player player && DatabaseObject<ClassDescriptor>.TryGet(player.Class, out var databaseObject2))
			{
				text = databaseObject2.AttackSpriteOverride;
			}
			break;
		}
		case SpriteAnimations.Shoot:
			if (Equipment.Length > Options.Instance.Equipment.WeaponSlot)
			{
				if (DatabaseObject<ItemDescriptor>.TryGet(id, out var databaseObject3))
				{
					text = databaseObject3.WeaponSpriteOverride;
				}
				if (!string.IsNullOrWhiteSpace(text))
				{
					spriteAnimation2 = SpriteAnimations.Shoot;
				}
			}
			break;
		case SpriteAnimations.Cast:
		{
			if (DatabaseObject<SpellDescriptor>.TryGet(SpellCast, out var databaseObject4))
			{
				text = databaseObject4.CastSpriteOverride;
			}
			if (!string.IsNullOrWhiteSpace(text))
			{
				spriteAnimation2 = SpriteAnimations.Cast;
			}
			break;
		}
		case SpriteAnimations.Weapon:
			if (Equipment.Length > Options.Instance.Equipment.WeaponSlot)
			{
				if (DatabaseObject<ItemDescriptor>.TryGet(id, out var databaseObject))
				{
					text = databaseObject.WeaponSpriteOverride;
				}
				if (!string.IsNullOrWhiteSpace(text))
				{
					spriteAnimation2 = SpriteAnimations.Weapon;
				}
			}
			break;
		default:
			throw new ArgumentOutOfRangeException("spriteAnimation");
		case SpriteAnimations.Normal:
		case SpriteAnimations.Idle:
		case SpriteAnimations.Run:
		case SpriteAnimations.Afk:
		case SpriteAnimations.Ki:
		case SpriteAnimations.Block:
		case SpriteAnimations.Transform:
		case SpriteAnimations.Spell:
		case SpriteAnimations.Levite:
		case SpriteAnimations.Eveil:
			break;
		}
		if (TryGetAnimationTextures(textureName, spriteAnimation2, text, out List<IGameTexture> textures))
		{
			AnimationVariants[spriteAnimation] = textures;
			AnimatedTextures[spriteAnimation] = textures[0];
		}
		else
		{
			AnimationVariants.Remove(spriteAnimation);
		}
	}

	protected virtual bool TryGetAnimationTextures(string textureName, SpriteAnimations spriteAnimation, string textureOverride, out List<IGameTexture> textures)
	{
		textures = new List<IGameTexture>();
		int num = Math.Max(1, Options.Instance.Sprites.MaxAnimationVariants);
		IGameTexture texture;
		for (int i = 1; i <= num && TryGetAnimationTexture(textureName, spriteAnimation, textureOverride, out texture, i); i++)
		{
			textures.Add(texture);
		}
		if (textures.Count > 0)
		{
			return true;
		}
		if (!TryGetAnimationTexture(textureName, spriteAnimation, textureOverride, out IGameTexture texture2))
		{
			return false;
		}
		textures.Add(texture2);
		return true;
	}

	protected virtual bool TryGetAnimationTexture(string textureName, SpriteAnimations spriteAnimation, string textureOverride, out IGameTexture texture, int variant = 0)
	{
		string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(textureName);
		string extension = Path.GetExtension(textureName);
		string text = fileNameWithoutExtension + "_" + (spriteAnimation.ToString()?.ToLowerInvariant() ?? string.Empty);
		if (!string.IsNullOrWhiteSpace(textureOverride))
		{
			text = text + "_" + textureOverride;
		}
		if (variant > 0)
		{
			text = $"{text}{variant}";
		}
		texture = Globals.ContentManager.GetTexture(TextureType.Entity, text + extension);
		return texture != null;
	}

	public Direction DirectionToTarget(Entity en)
	{
		if (en == null)
		{
			return DirectionFacing;
		}
		IMapInstance mapInstance = MapInstance;
		IMapInstance mapInstance2 = en.MapInstance;
		if (mapInstance == null || mapInstance2 == null)
		{
			return DirectionFacing;
		}
		int num = Y + mapInstance.GridY * MapHeight;
		int num2 = X + mapInstance.GridX * MapWidth;
		int num3 = en.Y + mapInstance2.GridY * MapHeight;
		int num4 = en.X + mapInstance2.GridX * MapWidth;
		int num5 = num - num3;
		int num6 = num2 - num4;
		if (num5 == 0)
		{
			if (num6 <= 0)
			{
				return Direction.Right;
			}
			return Direction.Left;
		}
		if (num6 == 0 || !Options.Instance.Map.EnableDiagonalMovement)
		{
			if (num5 <= 0)
			{
				return Direction.Down;
			}
			return Direction.Up;
		}
		bool flag = num6 > 0;
		bool flag2 = num5 > 0;
		if (flag)
		{
			if (!flag2)
			{
				return Direction.DownLeft;
			}
			return Direction.UpLeft;
		}
		if (!flag2)
		{
			return Direction.DownRight;
		}
		return Direction.UpRight;
	}

	public bool HoldsTile(int x, int y)
	{
		if (!IsMoving)
		{
			return false;
		}
		int num = X + ((OffsetX > 0f) ? 1 : ((OffsetX < 0f) ? (-1) : 0));
		int num2 = Y + ((OffsetY > 0f) ? 1 : ((OffsetY < 0f) ? (-1) : 0));
		if ((num != X || num2 != Y) && num == x)
		{
			return num2 == y;
		}
		return false;
	}

	public int IsTileBlocked(Point delta, int z, Guid mapId, ref IEntity? blockedBy, bool ignoreAliveResources = true, bool ignoreDeadResources = true, bool ignoreNpcAvoids = true, bool projectileTrigger = false)
	{
		MapInstance mapInstance = Intersect.Client.Maps.MapInstance.Get(mapId);
		if (mapInstance == null)
		{
			return -2;
		}
		int num = mapInstance.GridX;
		int num2 = mapInstance.GridY;
		try
		{
			int num3 = delta.X;
			int num4 = delta.Y;
			Guid empty = Guid.Empty;
			if (delta.X < 0)
			{
				num--;
				num3 = MapWidth - delta.X * -1;
			}
			if (delta.Y < 0)
			{
				num2--;
				num4 = MapHeight - delta.Y * -1;
			}
			if (delta.X > MapWidth - 1)
			{
				num++;
				num3 = delta.X - MapWidth;
			}
			if (delta.Y > MapHeight - 1)
			{
				num2++;
				num4 = delta.Y - MapHeight;
			}
			if (Globals.MapGrid == null || num < 0 || num2 < 0 || num >= Globals.MapGridWidth || num2 >= Globals.MapGridHeight)
			{
				return -2;
			}
			empty = Globals.MapGrid[num, num2];
			if (Houses.IsBlocked(empty, num3, num4))
			{
				return -2;
			}
			foreach (KeyValuePair<Guid, Entity> entity in Globals.Entities)
			{
				if (entity.Value == null || entity.Value == Globals.Me || !(entity.Value.MapId == empty) || entity.Value.Z != Z || ((entity.Value.X != num3 || entity.Value.Y != num4) && !entity.Value.HoldsTile(num3, num4)) || entity.Value is Projectile)
				{
					continue;
				}
				Entity value = entity.Value;
				if (!(value is Resource resource))
				{
					if (value is Player player && Intersect.Client.Maps.MapInstance.TryGet(player.MapId, out MapInstance instance) && Options.Instance.Passability.IsPassable(instance.ZoneType))
					{
						continue;
					}
				}
				else
				{
					ResourceDescriptor descriptor = resource.Descriptor;
					if (descriptor != null)
					{
						if (projectileTrigger)
						{
							bool isDead = resource.IsDead;
							if ((ignoreAliveResources || isDead) && (ignoreDeadResources || !isDead))
							{
								return -1;
							}
							blockedBy = entity.Value;
							return -6;
						}
						if ((descriptor.WalkableAfter && resource.IsDead) || (descriptor.WalkableBefore && !resource.IsDead))
						{
							continue;
						}
					}
				}
				blockedBy = entity.Value;
				return -6;
			}
			if (Intersect.Client.Maps.MapInstance.Get(empty) != null)
			{
				foreach (KeyValuePair<Guid, Entity> localEntity in Intersect.Client.Maps.MapInstance.Get(empty).LocalEntities)
				{
					if (localEntity.Value != null && localEntity.Value.MapId == empty && localEntity.Value.X == num3 && localEntity.Value.Y == num4 && localEntity.Value.Z == Z && !localEntity.Value.Passable)
					{
						blockedBy = localEntity.Value;
						return -4;
					}
				}
				foreach (KeyValuePair<Guid, Critter> localCritter in Intersect.Client.Maps.MapInstance.Get(empty).LocalCritters)
				{
					if (localCritter.Value != null && localCritter.Value.MapId == empty && localCritter.Value.X == num3 && localCritter.Value.Y == num4 && localCritter.Value.Z == Z && !localCritter.Value.Passable)
					{
						blockedBy = localCritter.Value;
						return -4;
					}
				}
			}
			MapInstance mapInstance2 = Intersect.Client.Maps.MapInstance.Get(Globals.MapGrid[num, num2]);
			if (mapInstance2 != null)
			{
				if (mapInstance2.Attributes[num3, num4] != null)
				{
					if (mapInstance2.Attributes[num3, num4].Type == MapAttributeType.Blocked || (mapInstance2.Attributes[num3, num4].Type == MapAttributeType.Animation && ((MapAnimationAttribute)mapInstance2.Attributes[num3, num4]).IsBlock))
					{
						return -2;
					}
					if (mapInstance2.Attributes[num3, num4].Type == MapAttributeType.ZDimension)
					{
						if (((MapZDimensionAttribute)mapInstance2.Attributes[num3, num4]).BlockedLevel - 1 == z)
						{
							return -3;
						}
					}
					else if (mapInstance2.Attributes[num3, num4].Type == MapAttributeType.NpcAvoid && !ignoreNpcAvoids)
					{
						return -2;
					}
				}
				return -1;
			}
			return -5;
		}
		catch
		{
			return -2;
		}
	}

	~Entity()
	{
		Dispose();
	}

	public bool PositionMonde(out float mondeX, out float mondeY)
	{
		if (!Intersect.Client.Maps.MapInstance.TryGet(MapId, out MapInstance instance))
		{
			mondeX = 0f;
			mondeY = 0f;
			return false;
		}
		mondeX = (float)(instance.GridX * DeplacementLibre.LargeurCarte + X * TileWidth) + (float)TileWidth / 2f + OffsetX;
		mondeY = (float)(instance.GridY * DeplacementLibre.HauteurCarte + Y * TileHeight) + (float)TileHeight / 2f + OffsetY;
		return true;
	}

	public bool PoserAuPointDuMonde(float mondeX, float mondeY, float marge, out bool carteChangee)
	{
		carteChangee = false;
		int largeurCarte = DeplacementLibre.LargeurCarte;
		int hauteurCarte = DeplacementLibre.HauteurCarte;
		Guid guid;
		int grilleX;
		int grilleY;
		if (Intersect.Client.Maps.MapInstance.TryGet(MapId, out MapInstance instance) && mondeX - (float)(instance.GridX * largeurCarte) >= 0f - marge && mondeX - (float)(instance.GridX * largeurCarte) < (float)largeurCarte + marge && mondeY - (float)(instance.GridY * hauteurCarte) >= 0f - marge && mondeY - (float)(instance.GridY * hauteurCarte) < (float)hauteurCarte + marge)
		{
			guid = MapId;
			grilleX = instance.GridX;
			grilleY = instance.GridY;
		}
		else
		{
			guid = DeplacementLibre.CarteAuPoint(mondeX, mondeY, out grilleX, out grilleY);
		}
		if (guid == Guid.Empty)
		{
			return false;
		}
		if (guid != MapId)
		{
			MapId = guid;
			carteChangee = true;
		}
		PoserSurLaCarte(mondeX - (float)(grilleX * largeurCarte), mondeY - (float)(grilleY * hauteurCarte));
		return true;
	}

	public void PoserSurLaCarte(float localX, float localY)
	{
		int num = Math.Clamp((int)MathF.Floor(localX / (float)TileWidth), 0, Options.Instance.Map.MapWidth - 1);
		int num2 = Math.Clamp((int)MathF.Floor(localY / (float)TileHeight), 0, Options.Instance.Map.MapHeight - 1);
		X = (byte)num;
		Y = (byte)num2;
		OffsetX = localX - ((float)(num * TileWidth) + (float)TileWidth / 2f);
		OffsetY = localY - ((float)(num2 * TileHeight) + (float)TileHeight / 2f);
	}

	public void RecevoirInstantaneLibre(long tempsServeur, EntiteLibre entite)
	{
		if ((Heritage.Actif && Heritage.Poussee.IgnoreLaPosition(this, tempsServeur)) || !Intersect.Client.Maps.MapInstance.TryGet(entite.MapId, out MapInstance instance))
		{
			return;
		}
		if (_instantanesLibres.Count > 0)
		{
			double num = tempsServeur;
			List<InstantaneLibre> instantanesLibres = _instantanesLibres;
			if (num <= instantanesLibres[instantanesLibres.Count - 1].Temps)
			{
				return;
			}
		}
		if (!EstPiloteLibrement)
		{
			_instantanesLibres.Clear();
			if (PositionMonde(out var mondeX, out var mondeY))
			{
				_instantanesLibres.Add(new InstantaneLibre(tempsServeur - 50, mondeX, mondeY, DirectionFacing, 0));
			}
			IsMoving = false;
			EstPiloteLibrement = true;
		}
		double num2 = DeplacementLibre.TempsServeur - (double)DeplacementLibre.RetardDe(this);
		int num3;
		if (DeplacementLibre.PredictionDe(this) > 0 && _instantanesLibres.Count > 0)
		{
			List<InstantaneLibre> instantanesLibres2 = _instantanesLibres;
			num3 = ((num2 > instantanesLibres2[instantanesLibres2.Count - 1].Temps) ? 1 : 0);
		}
		else
		{
			num3 = 0;
		}
		bool flag = (byte)num3 != 0;
		(float, float, InstantaneLibre, InstantaneLibre) tuple = (flag ? PointLibre(num2) : default((float, float, InstantaneLibre, InstantaneLibre)));
		_instantanesLibres.Add(new InstantaneLibre(tempsServeur, (float)(instance.GridX * DeplacementLibre.LargeurCarte) + entite.X, (float)(instance.GridY * DeplacementLibre.HauteurCarte) + entite.Y, (Direction)entite.Direction, entite.Etat));
		if (_instantanesLibres.Count > 12)
		{
			_instantanesLibres.RemoveAt(0);
		}
		_instantaneLibreRecuA = DeplacementLibre.MaintenantLocal;
		if (flag)
		{
			(float, float, InstantaneLibre, InstantaneLibre) tuple2 = PointLibre(num2);
			NoterCorrectionLibre(tuple.Item1 - tuple2.Item1, tuple.Item2 - tuple2.Item2);
		}
	}

	private void NoterCorrectionLibre(float ecartX, float ecartY)
	{
		ResorberCorrectionLibre();
		_correctionLibreX += ecartX;
		_correctionLibreY += ecartY;
		if (_correctionLibreX * _correctionLibreX + _correctionLibreY * _correctionLibreY > 2304f)
		{
			_correctionLibreX = 0f;
			_correctionLibreY = 0f;
		}
	}

	private void ResorberCorrectionLibre()
	{
		double maintenantPrecis = DeplacementLibre.MaintenantPrecis;
		float num = MathF.Exp((0f - (float)(maintenantPrecis - _correctionLibreA)) / 60f);
		_correctionLibreA = maintenantPrecis;
		_correctionLibreX *= num;
		_correctionLibreY *= num;
	}

	private (float X, float Y, InstantaneLibre Avant, InstantaneLibre Apres) PointLibre(double rendu)
	{
		InstantaneLibre instantaneLibre = _instantanesLibres[0];
		InstantaneLibre item = instantaneLibre;
		if (rendu > instantaneLibre.Temps)
		{
			int i;
			for (i = 1; i < _instantanesLibres.Count && _instantanesLibres[i].Temps < rendu; i++)
			{
			}
			if (i >= _instantanesLibres.Count)
			{
				List<InstantaneLibre> instantanesLibres = _instantanesLibres;
				instantaneLibre = instantanesLibres[instantanesLibres.Count - 1];
				item = instantaneLibre;
			}
			else
			{
				instantaneLibre = _instantanesLibres[i - 1];
				item = _instantanesLibres[i];
			}
		}
		float num = ((item.Temps > instantaneLibre.Temps) ? ((float)Math.Clamp((rendu - instantaneLibre.Temps) / (item.Temps - instantaneLibre.Temps), 0.0, 1.0)) : 1f);
		float item2 = instantaneLibre.MondeX + (item.MondeX - instantaneLibre.MondeX) * num;
		float item3 = instantaneLibre.MondeY + (item.MondeY - instantaneLibre.MondeY) * num;
		int num2 = DeplacementLibre.PredictionDe(this);
		if (num2 > 0 && rendu > item.Temps && _instantanesLibres.Count >= 2 && (item.Etat & 1) != 0)
		{
			List<InstantaneLibre> instantanesLibres2 = _instantanesLibres;
			InstantaneLibre instantaneLibre2 = instantanesLibres2[instantanesLibres2.Count - 2];
			(item2, item3) = DeplacementLibre.Predire(instantaneLibre2.MondeX, instantaneLibre2.MondeY, instantaneLibre2.Temps, item.MondeX, item.MondeY, item.Temps, rendu, num2);
		}
		return (X: item2, Y: item3, Avant: instantaneLibre, Apres: item);
	}

	public void RepasserEnCasesEnDouceur()
	{
		_instantanesLibres.Clear();
		EstPiloteLibrement = false;
		_recentrageLibre = OffsetX != 0f || OffsetY != 0f;
		IsMoving = _recentrageLibre;
	}

	public void RepasserEnCasesDepuis(float dessineX, float dessineY)
	{
		if (!PositionMonde(out var mondeX, out var mondeY) || MathF.Abs(dessineX - mondeX) > (float)(TileWidth * 2) || MathF.Abs(dessineY - mondeY) > (float)(TileHeight * 2))
		{
			OublierLeDeplacementLibre();
			OffsetX = 0f;
			OffsetY = 0f;
			IsMoving = false;
		}
		else
		{
			OffsetX += dessineX - mondeX;
			OffsetY += dessineY - mondeY;
			RepasserEnCasesEnDouceur();
		}
	}

	public void InterrompreLeRecentrage()
	{
		_recentrageLibre = false;
	}

	private void RecentrerEnDouceur(float ecoule)
	{
		float num = (float)TileWidth * ecoule / Math.Max(1f, GetMovementTime());
		float num2 = Math.Max(Math.Abs(OffsetX), Math.Abs(OffsetY));
		OffsetX = StepTowardsZero(OffsetX, num);
		OffsetY = StepTowardsZero(OffsetY, num);
		AvancerLaMarche(Math.Min(num, num2));
		if (OffsetX == 0f && OffsetY == 0f)
		{
			_recentrageLibre = false;
			IsMoving = false;
			FinishedStepOvershoot = Math.Max(0f, num - num2);
		}
		else
		{
			IsMoving = true;
		}
	}

	protected void AvancerLaMarche(float distance)
	{
		_walkedSinceFrame += distance;
	}

	public void OublierLeDeplacementLibre()
	{
		_instantanesLibres.Clear();
		EstPiloteLibrement = false;
	}

	private void InterpolerLibrement()
	{
		if (_instantanesLibres.Count == 0)
		{
			IsMoving = false;
			return;
		}
		double num = DeplacementLibre.TempsServeur - (double)DeplacementLibre.RetardDe(this);
		var (x, y, instantaneLibre, instantaneLibre2) = PointLibre(num);
		if (_correctionLibreX != 0f || _correctionLibreY != 0f)
		{
			ResorberCorrectionLibre();
			x += _correctionLibreX;
			y += _correctionLibreY;
		}
		float num2 = 0f;
		if (PositionMonde(out var mondeX, out var mondeY))
		{
			if (Heritage.Actif)
			{
				RetrancherLaPousseeHeritage(ref mondeX, ref mondeY);
			}
			num2 = MathF.Sqrt((x - mondeX) * (x - mondeX) + (y - mondeY) * (y - mondeY));
			if (num2 > (float)(TileWidth * 2))
			{
				num2 = 0f;
			}
		}
		if (Heritage.Actif)
		{
			AjouterLaPousseeHeritage(ref x, ref y);
		}
		float mondeX2;
		float mondeY2;
		bool flag = PositionMonde(out mondeX2, out mondeY2);
		bool carteChangee;
		bool flag2 = PoserAuPointDuMonde(x, y, 8f, out carteChangee);
		if (flag && PositionMonde(out var mondeX3, out var mondeY3))
		{
			float num3 = MathF.Sqrt((mondeX3 - mondeX2) * (mondeX3 - mondeX2) + (mondeY3 - mondeY2) * (mondeY3 - mondeY2));
			num2 = MathF.Min(num2, (num3 > (float)(TileWidth * 2)) ? 0f : num3);
		}
		else if (!flag2)
		{
			num2 = 0f;
		}
		if (!(this is Player) && ((instantaneLibre.Etat | instantaneLibre2.Etat) & 1) == 0)
		{
			num2 = 0f;
		}
		_walkedSinceFrame += num2;
		bool flag3 = DeplacementLibre.MaintenantLocal - _instantaneLibreRecuA < 250;
		IsMoving = num2 > 0.05f || (flag2 && flag3 && (instantaneLibre2.Etat & 1) != 0);
		if (this is Player && this != Globals.Me)
		{
			IsSprinting = (instantaneLibre2.Etat & 2) != 0;
		}
		if (!IsAttacking)
		{
			DirectionFacing = instantaneLibre2.Direction;
		}
		while (_instantanesLibres.Count > 2 && _instantanesLibres[1].Temps <= num)
		{
			_instantanesLibres.RemoveAt(0);
		}
	}

	internal void JouerLeGeste(int planche)
	{
		AttackTimer = Timing.Global.Milliseconds + CalculateAttackTime();
		_variantsRolledForAttackTimer = AttackTimer;
		if (Sheet == null && AnimationVariants.TryGetValue(SpriteAnimations.Attack, out List<IGameTexture> value) && value.Count > 0)
		{
			AnimatedTextures[SpriteAnimations.Attack] = value[Math.Clamp(planche, 0, value.Count - 1)];
		}
	}

	internal void NoterLeCoupHeritage(CoupHeritage coup, int avance, Guid cibleId)
	{
		_hCoup = coup;
		_hFinDuCoup = AttackTimer;
		_hAvance = avance;
		_hCibleId = cibleId;
	}

	private protected void RecalerLaFinDuCoupHeritage(long fin)
	{
		if (_variantsRolledForAttackTimer == AttackTimer)
		{
			_variantsRolledForAttackTimer = fin;
		}
		AttackTimer = fin;
		_hFinDuCoup = fin;
	}

	internal void RetenirLesReculsEnAttente(long millisecondes)
	{
		foreach (Dash item in DashQueue)
		{
			item.RetenirEnFile(millisecondes);
		}
	}

	internal void RecalculerLOrigine()
	{
		CalculateOrigin();
	}

	internal bool CoupHeritageRecent(long maintenant, int apresMs, out Guid cibleId)
	{
		cibleId = _hCibleId;
		if (_hCoup != CoupHeritage.Aucun && AttackTimer == _hFinDuCoup)
		{
			return maintenant < _hFinDuCoup + apresMs;
		}
		return false;
	}

	internal int? RangeesPlusBasPourLeTri(Entity autre, int maRangee)
	{
		if (autre.MapId != MapId || autre.Z != 0 != (Z != 0) || autre.mRenderPriority == 0 || mRenderPriority == 0)
		{
			return null;
		}
		int num = autre.RangeeDesPieds - RangeeDesPieds;
		HashSet<Entity> renderList = autre.RenderList;
		if (renderList != null)
		{
			HashSet<Entity>[,] renderingEntities = Graphics.RenderingEntities;
			if (renderingEntities != null)
			{
				int num2 = autre.mRenderPriority + ((autre.Z != 0) ? 3 : 0);
				int num3 = maRangee + num + 1;
				if (num2 < renderingEntities.GetLength(0) && num3 >= 0 && num3 < renderingEntities.GetLength(1) && renderingEntities[num2, num3] == renderList)
				{
					num++;
				}
			}
		}
		return num;
	}

	internal int? ImageDuCoupHeritage(long tempsDuGeste)
	{
		CoupHeritage coupEnCours = CoupEnCours;
		if (coupEnCours == CoupHeritage.Aucun || SpriteAnimation != SpriteAnimations.Attack || Sheet != null)
		{
			return null;
		}
		int ecoule = (int)Math.Clamp(tempsDuGeste, 0L, 2147483647L);
		(Heritage.PoseDuCoup, int, bool)? tuple = Heritage.Image(coupEnCours, ecoule, DureeDuGeste(), AvanceDuCoupEnCours, SpriteFrames);
		if (tuple.HasValue)
		{
			(Heritage.PoseDuCoup, int, bool) valueOrDefault = tuple.GetValueOrDefault();
			if (valueOrDefault.Item1 == Heritage.PoseDuCoup.Planche)
			{
				return valueOrDefault.Item2;
			}
			IGameTexture valueOrDefault2 = AnimatedTextures.GetValueOrDefault(SpriteAnimations.Attack);
			int colonnes;
			IGameTexture planche = PlancheDuCoupHeritage(coupEnCours, out colonnes);
			return Heritage.ImageDeRepli(coupEnCours, RepliDuCoupHeritage(coupEnCours, valueOrDefault2, planche, colonnes, valueOrDefault.Item2));
		}
		return null;
	}

	internal bool PoseDuCoupHeritage(int rangee, ref IGameTexture texture, ref FloatRect source)
	{
		CoupHeritage coupEnCours = CoupEnCours;
		bool flag = coupEnCours - 3 <= CoupHeritage.Direct;
		if (!flag || SpriteAnimation != SpriteAnimations.Attack || Sheet != null)
		{
			return false;
		}
		int ecoule = (int)Math.Clamp(Heritage.TempsDuGeste(this), 0L, 2147483647L);
		(Heritage.PoseDuCoup, int, bool)? tuple = Heritage.Image(coupEnCours, ecoule, DureeDuGeste(), AvanceDuCoupEnCours, SpriteFrames);
		if (tuple.HasValue)
		{
			(Heritage.PoseDuCoup, int, bool) valueOrDefault = tuple.GetValueOrDefault();
			if (valueOrDefault.Item1 != Heritage.PoseDuCoup.Planche)
			{
				int colonnes;
				IGameTexture gameTexture = PlancheDuCoupHeritage(coupEnCours, out colonnes);
				if (gameTexture == null || RepliDuCoupHeritage(coupEnCours, texture, gameTexture, colonnes, valueOrDefault.Item2) != Heritage.RepliDuCoup.Aucun)
				{
					return false;
				}
				int num = ((coupEnCours == CoupHeritage.Montant) ? Heritage.ColonneDuMontant(_hVolTenu, valueOrDefault.Item2) : valueOrDefault.Item2);
				int num2 = gameTexture.Width / colonnes;
				int num3 = gameTexture.Height / Math.Max(1, Options.Instance.Sprites.Directions);
				texture = gameTexture;
				source = new FloatRect(num * num2, rangee * num3, num2, num3);
				return true;
			}
		}
		return false;
	}

	private IGameTexture? PlancheDuCoupHeritage(CoupHeritage coup, out int colonnes)
	{
		if (coup == CoupHeritage.Montant)
		{
			colonnes = 16;
			return Flight.FlightSheet(this);
		}
		colonnes = Math.Max(1, Options.Instance.Sprites.RunFrames);
		return AnimatedTextures.GetValueOrDefault(SpriteAnimations.Run);
	}

	private Heritage.RepliDuCoup RepliDuCoupHeritage(CoupHeritage coup, IGameTexture? poings, IGameTexture? planche, int colonnes, int image)
	{
		if (poings == null || planche == null || image >= colonnes)
		{
			return Heritage.RepliDuCoup.SansImage;
		}
		bool flag = coup == CoupHeritage.Montant;
		int num = CleDuRepliHeritage(poings, planche);
		if (num == (flag ? _hRepliCleMontant : _hRepliCleEpaule))
		{
			if (!flag)
			{
				return _hRepliEpaule;
			}
			return _hRepliMontant;
		}
		Heritage.RepliDuCoup repliDuCoup = Heritage.RepliDuCoup.Aucun;
		string corps = CorpsDessineHeritage();
		if (flag)
		{
			_hVolTenu = Heritage.VolTenu(corps);
		}
		if (flag && Heritage.CorpsSansMontant(corps))
		{
			repliDuCoup = Heritage.RepliDuCoup.SansImage;
		}
		else if (!Heritage.Tenues.Suivent(CalquesPortesHeritage(), Path.GetFileNameWithoutExtension(Sprite) ?? string.Empty, Path.GetFileNameWithoutExtension(poings.Name) ?? string.Empty, DrawnForm(), Path.GetFileNameWithoutExtension(planche.Name) ?? string.Empty, planche.Width, planche.Height, TailleDuCalqueHeritage))
		{
			repliDuCoup = Heritage.RepliDuCoup.SansTenue;
		}
		if (flag)
		{
			_hRepliCleMontant = num;
			_hRepliMontant = repliDuCoup;
		}
		else
		{
			_hRepliCleEpaule = num;
			_hRepliEpaule = repliDuCoup;
		}
		return repliDuCoup;
	}

	private string? CorpsDessineHeritage()
	{
		if (!string.IsNullOrWhiteSpace(TransformedSprite))
		{
			return Transformation.TextureNameFor(Sprite, TransformedSprite);
		}
		return Sprite;
	}

	internal int? PlancheDuMontantHeritage(int planches)
	{
		if (CoupEnCours != CoupHeritage.Montant || Sheet != null || planches <= 1 || !Heritage.CorpsSansMontant(CorpsDessineHeritage()))
		{
			return null;
		}
		return 1;
	}

	private static (int Largeur, int Hauteur)? TailleDuCalqueHeritage(string fichier)
	{
		IGameTexture gameTexture = Globals.ContentManager?.GetTexture(TextureType.Paperdoll, fichier);
		if (gameTexture == null)
		{
			return null;
		}
		return (gameTexture.Width, gameTexture.Height);
	}

	private int CleDuRepliHeritage(IGameTexture poings, IGameTexture planche)
	{
		HashCode hashCode = default(HashCode);
		hashCode.Add(poings.Name);
		hashCode.Add(planche.Name);
		hashCode.Add(TransformedSprite);
		hashCode.Add(Gender);
		hashCode.Add(_lastDirection);
		hashCode.Add(Intersect.Client.Maps.MapInstance.Get(MapId)?.HideEquipment ?? false);
		for (int i = 0; i < Equipment.Length; i++)
		{
			hashCode.Add(ObjetPorteHeritage(i));
		}
		int num = hashCode.ToHashCode();
		if (num != 0)
		{
			return num;
		}
		return 1;
	}

	private Guid ObjetPorteHeritage(int emplacement)
	{
		if (emplacement < 0 || emplacement >= Equipment.Length || emplacement >= MyEquipment.Length)
		{
			return Guid.Empty;
		}
		if ((!(Equipment[emplacement] != Guid.Empty) || this == Globals.Me) && MyEquipment[emplacement] >= Options.Instance.Player.MaxInventory)
		{
			return Guid.Empty;
		}
		if (this != Globals.Me)
		{
			return Equipment[emplacement];
		}
		int num = MyEquipment[emplacement];
		if (num <= -1 || num >= Inventory.Length)
		{
			return Guid.Empty;
		}
		return Inventory[num]?.ItemId ?? Guid.Empty;
	}

	private List<string> CalquesPortesHeritage()
	{
		List<string> list = _hCalques ?? (_hCalques = new List<string>(8));
		list.Clear();
		List<string> slots = Options.Instance.Equipment.Slots;
		MapInstance mapInstance = Intersect.Client.Maps.MapInstance.Get(MapId);
		if (mapInstance == null || mapInstance.HideEquipment || (!string.IsNullOrEmpty(TransformedSprite) && !Transformation.KeepsPaperdolls(this)) || Equipment.Length != slots.Count)
		{
			return list;
		}
		List<string> list2 = Options.Instance.Equipment.Paperdoll.Directions[(int)_lastDirection];
		for (int i = 0; i < list2.Count; i++)
		{
			int num = slots.IndexOf(list2[i]);
			if (num >= 0 && DatabaseObject<ItemDescriptor>.TryGet(ObjetPorteHeritage(num), out var databaseObject))
			{
				string text = Paperdolls.ForClass((Gender == Gender.Male) ? databaseObject.MalePaperdoll : databaseObject.FemalePaperdoll, (this is Player player) ? player.Class : Guid.Empty);
				if (!string.IsNullOrEmpty(text))
				{
					list.Add(text);
				}
			}
		}
		return list;
	}

	internal void CommencerUnePousseeHeritage(float x, float y, long maintenantLocal, float dureeMs = 80f)
	{
		_hPoussee.Pousser(x, y, dureeMs);
		_hPousseeA = maintenantLocal;
	}

	internal void RetirerLaPousseeHeritage()
	{
		_hPoussee.Retirer();
	}

	internal void OublierLaPousseeHeritage()
	{
		_hPoussee = default(Heritage.Poussee.Decalage);
		_hPousseeX = 0f;
		_hPousseeY = 0f;
	}

	internal bool AvancerLaPousseeHeritage(float ecoule, long maintenantLocal, int attenteMs)
	{
		if (!_hPoussee.EnCours)
		{
			return false;
		}
		if (IsDying)
		{
			return true;
		}
		if (!EstPiloteLibrement && (IsMoving || SeRecentre))
		{
			OublierLaPousseeHeritage();
			return false;
		}
		if (!_hPoussee.EnRetrait && maintenantLocal - _hPousseeA >= attenteMs)
		{
			_hPoussee.Retirer();
		}
		_hPoussee.Avancer(ecoule);
		if (!EstPiloteLibrement)
		{
			PoserLaPousseeHeritage();
		}
		if (!_hPoussee.Resorbee)
		{
			return true;
		}
		_hPoussee = default(Heritage.Poussee.Decalage);
		return false;
	}

	private void PoserLaPousseeHeritage()
	{
		var (num, num2) = _hPoussee.Valeur;
		if ((num != _hPousseeX || num2 != _hPousseeY) && PositionMonde(out var mondeX, out var mondeY))
		{
			PoserAuPointDuMonde(mondeX + num - _hPousseeX, mondeY + num2 - _hPousseeY, 8f, out var _);
			_hPousseeX = num;
			_hPousseeY = num2;
		}
	}

	private void RetrancherLaPousseeHeritage(ref float dessineX, ref float dessineY)
	{
		dessineX -= _hPousseeX;
		dessineY -= _hPousseeY;
	}

	private void AjouterLaPousseeHeritage(ref float x, ref float y)
	{
		(_hPousseeX, _hPousseeY) = _hPoussee.Valeur;
		x += _hPousseeX;
		y += _hPousseeY;
	}

	internal void ReprendreSesPositionsSousLaPousseeHeritage(long tempsServeur)
	{
		if (!EstPiloteLibrement && _hPoussee.EnCours && PositionMonde(out var mondeX, out var mondeY))
		{
			_instantanesLibres.Clear();
			_instantanesLibres.Add(new InstantaneLibre(tempsServeur - 50, mondeX - _hPousseeX, mondeY - _hPousseeY, DirectionFacing, 0));
			IsMoving = false;
			EstPiloteLibrement = true;
		}
	}

	protected bool AttendreHorsDeVue()
	{
		if (this == Globals.Me)
		{
			return false;
		}
		if (!Globals.GridMaps.ContainsKey(MapId))
		{
			EntitesInconnues.NoterPerte(Id, 1);
			Globals.EntitiesToDispose.Add(Id);
			return false;
		}
		long milliseconds = Timing.Global.Milliseconds;
		if (_horsDeVueDepuis == 0L)
		{
			_horsDeVueDepuis = milliseconds;
			WorldPos.Reset();
			if (RenderList != null)
			{
				RenderList.Remove(this);
				RenderList = null;
			}
		}
		if (EntitesInconnues.SursisEcoule(_horsDeVueDepuis, milliseconds))
		{
			EntitesInconnues.NoterPerte(Id, 1);
			Globals.EntitiesToDispose.Add(Id);
		}
		else if (EstPiloteLibrement)
		{
			InterpolerLibrement();
		}
		return false;
	}

	protected bool LigneBarreMonPas(int versX, int versY)
	{
		if (Intersect.Client.Maps.MapInstance.TryGet(MapId, out MapInstance instance))
		{
			IndexDesLignes indexDesLignes = instance.IndexDesLignes;
			if (indexDesLignes != null && !indexDesLignes.Aucune)
			{
				return IndexDesLignes.BarreLePas(indexDesLignes, X, Y, indexDesLignes, versX, versY, IndexDesLignes.DuPas(versX - X, versY - Y));
			}
		}
		return false;
	}
}
