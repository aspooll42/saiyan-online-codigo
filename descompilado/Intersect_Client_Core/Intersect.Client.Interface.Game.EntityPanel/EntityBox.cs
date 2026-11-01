using System;
using System.Collections.Generic;
using System.Linq;
using Intersect.Client.Core;
using Intersect.Client.Core.Controls;
using Intersect.Client.Entities;
using Intersect.Client.Entities.Events;
using Intersect.Client.Framework.Content;
using Intersect.Client.Framework.Entities;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.Framework.Gwen;
using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.Framework.Gwen.Renderer;
using Intersect.Client.Framework.Gwen.Skin;
using Intersect.Client.Framework.Input;
using Intersect.Client.General;
using Intersect.Client.Interface.Game.Dungeons;
using Intersect.Client.Interface.Game.Enseignements;
using Intersect.Client.Interface.Game.Hotbar;
using Intersect.Client.Interface.Game.Techniques;
using Intersect.Client.Localization;
using Intersect.Config;
using Intersect.Enums;
using Intersect.Framework.Core;
using Intersect.Framework.Core.GameObjects.Items;
using Intersect.GameObjects;
using Intersect.Localization;
using Intersect.Models;

namespace Intersect.Client.Interface.Game.EntityPanel;

public class EntityBox
{
	private readonly record struct PortraitLayer(IGameTexture Texture, Rectangle Frame, Color Tint, bool IsFace, IGameTexture? Anchor = null, Rectangle AnchorFrame = default(Rectangle));

	private sealed class HudPanel : Intersect.Client.Framework.Gwen.Control.Base
	{
		private readonly EntityBox _owner;

		protected override bool ShouldClip => false;

		public HudPanel(Intersect.Client.Framework.Gwen.Control.Base parent, EntityBox owner)
			: base(parent, "HudPanel")
		{
			_owner = owner;
			base.MouseInputEnabled = false;
		}

		protected override void Render(Intersect.Client.Framework.Gwen.Skin.Base skin)
		{
			base.Render(skin);
			_owner.Draw(skin.Renderer);
		}
	}

	private readonly record struct Moment(long Now, bool LowHealth, float LowPulse, float Hit, float LevelUp, float FormChange);

	private readonly bool _isPlayerBox;

	private readonly HudPanel _panel;

	private readonly Intersect.Client.Framework.Gwen.Control.Base _hpHover;

	private readonly Intersect.Client.Framework.Gwen.Control.Base _mpHover;

	private readonly Intersect.Client.Framework.Gwen.Control.Base? _expHover;

	private string? _hpValeur;

	private string? _mpValeur;

	private string? _expValeur;

	private readonly Button? _contextMenuButton;

	private readonly ImagePanel? EntityStatusWindow;

	private readonly Dictionary<Guid, SpellStatus> mActiveStatuses = new Dictionary<Guid, SpellStatus>();

	private readonly GaugeMotion _hp = new GaugeMotion();

	private readonly GaugeMotion _mp = new GaugeMotion();

	private readonly GaugeMotion _exp = new GaugeMotion();

	private readonly List<PortraitLayer> _layers = new List<PortraitLayer>();

	private string? _portraitKey;

	private long _lastUpdateTime;

	private int _shownLevel = -1;

	private long _lastExp = -1L;

	private long _levelUpAt = long.MinValue;

	private long _expShineAt = long.MinValue;

	private long _shownPower;

	private string _nameText = string.Empty;

	private string _levelText = string.Empty;

	private string _rightLabel = string.Empty;

	private string _rightValue = string.Empty;

	private string _hpText = string.Empty;

	private string _mpText = string.Empty;

	private string _expText = string.Empty;

	private float _shieldRatio;

	public readonly ImagePanel EntityWindow;

	public EntityType EntityType;

	public IEntity? MyEntity;

	public RichLabel? EventDesc;

	public ScrollControl? EntityStatusPanel;

	public bool ShouldUpdateStatuses;

	private static readonly Color DashPlein = new Color(255, 255, 154, 58);

	private static readonly Color DashClair = new Color(255, 255, 214, 160);

	private static readonly Color DashFonce = new Color(255, 150, 70, 20);

	private const float GroupesEcart = 10f;

	private int _chargesDashVues = -1;

	private long _chargeDashRevenueA;

	private long _chargeDashPriseA;

	private string _toucheDash = string.Empty;

	private bool _toucheDashEspace;

	private long _toucheDashLueA;

	private static readonly Color JetonPlein = new Color(255, 150, 225, 255);

	private static readonly Color JetonClair = new Color(255, 214, 246, 255);

	private static readonly Color JetonFonce = new Color(255, 40, 120, 170);

	private static readonly Color ToucheDessus = new Color(255, 44, 51, 80);

	private static readonly Color ToucheLiseret = new Color(255, 89, 99, 140);

	private static readonly Color ToucheRefus = new Color(255, 201, 55, 44);

	private static readonly Color ToucheRefusLiseret = new Color(255, 255, 123, 107);

	private const float JetonLargeur = 16f;

	private const float JetonEcart = 4f;

	private const float ToucheEcart = 5f;

	private int _jetonsVus = -1;

	private long _jetonRevenuA;

	private long _jetonPrisA;

	private string _toucheEsquive = string.Empty;

	private long _toucheLueA;

	private const float DesignScreenWidth = 1920f;

	private const float DesignScreenHeight = 1080f;

	private const float PlateWidth = 480f;

	private const float OldPlateWidth = 334f;

	private const float CrestX = 62f;

	private const float CrestY = 66f;

	private const float CrestRadius = 56f;

	private const float PlateLeft = 60f;

	private const float PlateTop = 20f;

	private const float PlateRight = 480f;

	private const float PlateBottom = 112f;

	private const float PlateSlant = 24f;

	private const float PlateChamfer = 14f;

	private const float Lip = 5f;

	private const float NameX = 134f;

	private const float NameY = 38f;

	private const float HpX = 128f;

	private const float HpY = 54f;

	private const float HpWidth = 330f;

	private const float HpHeight = 26f;

	private const float KiX = 140f;

	private const float KiY = 85f;

	private const float KiWidth = 226f;

	private const float KiHeight = 14f;

	private const float KiGap = 3f;

	private const int KiCells = 10;

	private const float ExpX = 124f;

	private const float ExpY = 107f;

	private const float ExpWidth = 322f;

	private const float ExpHeight = 4f;

	private const float BadgeX = 102f;

	private const float BadgeY = 108f;

	private const float BadgeRadius = 18f;

	private const float BoxHeight = 130f;

	private const float StatusTop = 132f;

	private const int StatusHeight = 44;

	private const float LowHealthRatio = 0.25f;

	private const long ExpShineMs = 900L;

	private const long LevelUpMs = 900L;

	private const long FormChangeMs = 700L;

	private static readonly int[] FontSizes = new int[15]
	{
		8, 9, 10, 11, 12, 13, 14, 16, 18, 20,
		22, 24, 26, 28, 32
	};

	private static readonly SaiyanHud.Accent Hostile = new SaiyanHud.Accent(new Color(255, 232, 64, 64), new Color(255, 255, 128, 116));

	private static readonly SaiyanHud.Accent Neutral = new SaiyanHud.Accent(new Color(255, 245, 190, 90), new Color(255, 255, 226, 150));

	private static readonly SaiyanHud.Accent Ally = new SaiyanHud.Accent(new Color(255, 86, 156, 255), new Color(255, 172, 206, 255));

	private static readonly Color UltraViolet = new Color(255, 150, 132, 255);

	private static readonly Color Track = new Color(255, 5, 7, 13);

	private static readonly Color Black = new Color(255, 0, 0, 0);

	private const float Margin = 48f;

	private float _z = -1f;

	private Point _origin;

	private long _hitAt = long.MinValue;

	private bool _hitBig;

	private long _formAt = long.MinValue;

	private string? _lastForm;

	private float _lastHealth = -1f;

	private SaiyanHud.Accent _frame = SaiyanHud.BaseAccent;

	private SaiyanHud.Accent? _aura;

	private bool _auraIsUltra;

	private string _formLabel = string.Empty;

	private Guid _formSpellId;

	private float _tailleSocle = 1f;

	public bool IsVisible
	{
		get
		{
			return EntityWindow.IsVisibleInTree;
		}
		set
		{
			EntityWindow.IsVisibleInTree = value;
		}
	}

	public int ContentBottom => EntityWindow.Y + _origin.Y + L(130f);

	private Point CoinDuSocle => new Point(EntityWindow.X + _origin.X, EntityWindow.Y + _origin.Y);

	public EntityBox(Canvas gameCanvas, EntityType entityType, Entity? myEntity, bool isPlayerBox = false)
	{
		MyEntity = myEntity;
		EntityType = entityType;
		_isPlayerBox = isPlayerBox;
		EntityWindow = new ImagePanel(gameCanvas, isPlayerBox ? "PlayerBox" : "TargetBox")
		{
			ShouldDrawBackground = false,
			ShouldCacheToTexture = false,
			MouseInputEnabled = false
		};
		_panel = new HudPanel(EntityWindow, this);
		_hpHover = MakeHover("HpHover");
		_mpHover = MakeHover("MpHover");
		if (isPlayerBox)
		{
			_expHover = MakeHover("ExpHover");
		}
		else
		{
			EventDesc = new RichLabel(EntityWindow, "EventDescLabel")
			{
				Font = SaiyanHud.SmallFont,
				FontSize = 10,
				MouseInputEnabled = false
			};
			_contextMenuButton = new Button(EntityWindow, "ContextMenuButton");
			_contextMenuButton.SetStateTexture(ComponentState.Normal, "downarrownormal.png");
			_contextMenuButton.SetStateTexture(ComponentState.Hovered, "downarrowhover.png");
			_contextMenuButton.SetStateTexture(ComponentState.Active, "downarrowclicked.png");
			_contextMenuButton.Clicked += delegate
			{
				Interface.GameUi.TargetContextMenu.ToggleHidden(_contextMenuButton);
			};
			EntityStatusWindow = new ImagePanel(EntityWindow, "EntityStatusWindow")
			{
				ShouldDrawBackground = false,
				MouseInputEnabled = false
			};
			EntityStatusPanel = new ScrollControl(EntityStatusWindow, "StatusArea")
			{
				ShouldDrawBackground = false,
				OverflowX = OverflowBehavior.Hidden,
				OverflowY = OverflowBehavior.Hidden
			};
		}
		Place();
		SetEntity(myEntity);
		EntityWindow.Hide();
		_lastUpdateTime = Timing.Global.MillisecondsUtc;
	}

	private Intersect.Client.Framework.Gwen.Control.Base MakeHover(string name)
	{
		return new Intersect.Client.Framework.Gwen.Control.Base(EntityWindow, name)
		{
			MouseInputEnabled = true
		};
	}

	private void Place()
	{
		Canvas canvas = EntityWindow.Canvas;
		float num = ScaleFor(canvas);
		if (canvas == null)
		{
			PlaceContent(num, (int)MathF.Round(16f * num), (int)MathF.Round(14f * num));
			return;
		}
		if (_isPlayerBox)
		{
			PlaceSocle(canvas);
			return;
		}
		num *= Disposition.Taille(Disposition.Piece.Cible);
		int num2 = Math.Max(DungeonHud.ReservedBottom, TrialHud.ReservedBottom);
		Point point = Disposition.Poser(Disposition.Piece.Cible, (int)MathF.Round(485f * num), (int)MathF.Round(130f * num), Math.Max(0, (canvas.Width - (int)MathF.Round(480f * num)) / 2), (num2 > 0) ? (num2 + 8) : ((int)MathF.Round(14f * num)), canvas);
		PlaceContent(num, point.X, point.Y);
	}

	public void SetEntity(IEntity? entity)
	{
		MyEntity = entity;
		if (MyEntity != null)
		{
			SetupEntityElements();
			ShouldUpdateStatuses = !_isPlayerBox;
			SetupEventDescription();
		}
	}

	public void SetEntity(IEntity? entity, EntityType type)
	{
		MyEntity = entity;
		EntityType = type;
		if (MyEntity != null)
		{
			SetupEntityElements();
			ShouldUpdateStatuses = !_isPlayerBox;
			SetupEventDescription();
		}
	}

	private void SetupEventDescription()
	{
		if (EventDesc != null)
		{
			if (EntityType == EntityType.Event && MyEntity is Event obj)
			{
				EventDesc.ClearText();
				EventDesc.AddText(obj.Desc, Color.White);
				EventDesc.SizeToChildren(resizeX: false);
				EventDesc.Show();
			}
			else
			{
				EventDesc.Hide();
			}
		}
	}

	public void ShowAllElements()
	{
		_hpHover.Show();
		_mpHover.Show();
		_expHover?.Show();
		_contextMenuButton?.Show();
	}

	public void SetupEntityElements()
	{
		ShowAllElements();
		_hp.Shown = -1f;
		_mp.Shown = -1f;
		_exp.Shown = -1f;
		_shownLevel = -1;
		_lastExp = -1L;
		_levelUpAt = long.MinValue;
		_expShineAt = long.MinValue;
		_shownPower = -1L;
		_portraitKey = null;
		_layers.Clear();
		switch (EntityType)
		{
		case EntityType.Player:
			if (Globals.Me != null && Globals.Me == MyEntity && !_isPlayerBox)
			{
				_contextMenuButton?.Hide();
			}
			break;
		case EntityType.GlobalEntity:
			_contextMenuButton?.Hide();
			break;
		case EntityType.Event:
			_hpHover.Hide();
			_mpHover.Hide();
			_contextMenuButton?.Hide();
			break;
		}
		_nameText = MyEntity?.Name ?? string.Empty;
		Refresh(0f, Timing.Global.MillisecondsUtc, instant: true);
	}

	public void Update()
	{
		IEntity? myEntity = MyEntity;
		if (myEntity == null || myEntity.IsDisposed)
		{
			if (!EntityWindow.IsHidden)
			{
				EntityWindow.Hide();
			}
			return;
		}
		if (Disposition.Masquee((!_isPlayerBox) ? Disposition.Piece.Cible : Disposition.Piece.VieEtKi))
		{
			if (!EntityWindow.IsHidden)
			{
				EntityWindow.Hide();
			}
			return;
		}
		if (!_isPlayerBox && DungeonHud.EstLeBossAffiche(MyEntity))
		{
			if (!EntityWindow.IsHidden)
			{
				EntityWindow.Hide();
			}
			return;
		}
		if (EntityWindow.IsHidden)
		{
			Show();
		}
		long millisecondsUtc = Timing.Global.MillisecondsUtc;
		float elapsed = Math.Clamp((float)(millisecondsUtc - _lastUpdateTime) / 1000f, 0f, 0.1f);
		_lastUpdateTime = millisecondsUtc;
		Place();
		Refresh(elapsed, millisecondsUtc, instant: false);
		if (MyEntity.Type == EntityType.Player && MyEntity != Globals.Me && _contextMenuButton != null)
		{
			if (MyEntity.Vitals[Vital.Health] <= 0)
			{
				_contextMenuButton.Hide();
			}
			else if (_contextMenuButton.IsHidden)
			{
				_contextMenuButton.Show();
			}
		}
		ShouldUpdateStatuses = !_isPlayerBox && !(MyEntity is Event);
		if (!ShouldUpdateStatuses || EntityStatusPanel == null || EntityStatusWindow == null)
		{
			return;
		}
		SpellStatus.UpdateSpellStatus(MyEntity, EntityStatusPanel, mActiveStatuses);
		EntityStatusWindow.IsHidden = mActiveStatuses.Count < 1;
		foreach (KeyValuePair<Guid, SpellStatus> mActiveStatus in mActiveStatuses)
		{
			mActiveStatus.Value.Update();
		}
		ShouldUpdateStatuses = false;
	}

	private void Refresh(float elapsed, long now, bool instant)
	{
		if (MyEntity == null)
		{
			return;
		}
		RefreshLook(now, instant);
		RefreshPortrait();
		RefreshPower();
		if (EntityType == EntityType.Event)
		{
			_nameText = MyEntity.Name ?? string.Empty;
			_levelText = string.Empty;
			_rightLabel = string.Empty;
			_rightValue = string.Empty;
			return;
		}
		if (!_isPlayerBox && MyEntity is Player player && player != Globals.Me && player.KarmaTier != KarmaTier.Neutral)
		{
			_nameText = Strings.Karma.TargetNameAndTier.ToString(player.Name, KarmaDisplay.TierName(player.KarmaTier));
		}
		else
		{
			_nameText = MyEntity.Name ?? string.Empty;
		}
		_levelText = MyEntity.Level.ToString();
		if (_isPlayerBox)
		{
			_rightLabel = string.Empty;
			_rightValue = string.Empty;
		}
		RefreshHealth(elapsed, now, instant);
		RefreshMana(elapsed, now, instant);
		if (_isPlayerBox && MyEntity == Globals.Me)
		{
			RefreshExperience(elapsed, now, instant);
		}
	}

	private void RefreshPower()
	{
		if (!_isPlayerBox)
		{
			long num = ((EntityType != EntityType.Event && MyEntity is Entity entity) ? ScouterReadout.Of(entity) : 0);
			if (num != _shownPower)
			{
				_shownPower = num;
				_rightLabel = ((num > 0) ? ((string)Strings.EntityBox.Power) : string.Empty);
				_rightValue = ((num > 0) ? SaiyanHud.Amount(num) : string.Empty);
			}
		}
	}

	private void RefreshHealth(float elapsed, long now, bool instant)
	{
		long num = MyEntity.Vitals[Vital.Health];
		long num2 = Math.Max(1L, MyEntity.MaxVitals[Vital.Health]);
		long num3 = Math.Max(0L, MyEntity.ShieldSize);
		if (num > 0 && num3 + num > num2)
		{
			num2 = num3 + num;
		}
		float num4 = ((num <= 0) ? 0f : ((float)num / (float)num2));
		_shieldRatio = ((num <= 0) ? 0f : ((float)num3 / (float)num2));
		string hpText = $"{num4 * 100f:0} %";
		string hpValeur = Strings.EntityBox.Vital0Value.ToString(SaiyanHud.Amount(Math.Max(0L, num)), SaiyanHud.Amount(MyEntity.MaxVitals[Vital.Health]));
		_hpText = hpText;
		_hpValeur = hpValeur;
		TrackHit(num4, now, instant);
		if (instant)
		{
			_hp.Snap(num4);
		}
		else
		{
			_hp.Update(num4, elapsed, now);
		}
	}

	private void RefreshMana(float elapsed, long now, bool instant)
	{
		long num = MyEntity.Vitals[Vital.Mana];
		long num2 = Math.Max(1L, MyEntity.MaxVitals[Vital.Mana]);
		float num3 = ((num <= 0) ? 0f : ((float)num / (float)num2));
		string mpText = $"{num3 * 100f:0} %";
		string mpValeur = Strings.EntityBox.Vital1Value.ToString(SaiyanHud.Amount(Math.Max(0L, num)), SaiyanHud.Amount(MyEntity.MaxVitals[Vital.Mana]));
		_mpText = mpText;
		_mpValeur = mpValeur;
		if (instant)
		{
			_mp.Snap(num3);
		}
		else
		{
			_mp.Update(num3, elapsed, now);
		}
	}

	private void RefreshExperience(float elapsed, long now, bool instant)
	{
		if (!(MyEntity is Player player))
		{
			return;
		}
		bool showExperienceAsPercentage = Globals.Database.ShowExperienceAsPercentage;
		long nextLevelExperience = player.GetNextLevelExperience();
		float num;
		if (nextLevelExperience > 0)
		{
			long experience = player.Experience;
			num = Math.Clamp((float)experience / (float)nextLevelExperience, 0f, 1f);
			string text = $"{num * 100f:0.##} %";
			string text2 = Strings.EntityBox.ExpValue.ToString(SaiyanHud.Amount(experience), SaiyanHud.Amount(nextLevelExperience));
			_expText = $"{num * 100f:0} %";
			_expValeur = text + "  (" + text2 + ")";
			if (!instant && _shownLevel >= 0 && player.Level > _shownLevel)
			{
				_levelUpAt = now;
				_expShineAt = now;
			}
			else if (!instant && _lastExp >= 0 && experience > _lastExp)
			{
				_expShineAt = now;
			}
			_lastExp = experience;
		}
		else
		{
			num = 1f;
			_expText = Strings.EntityBox.MaxLevel;
			_expValeur = Strings.EntityBox.MaxLevel;
			_lastExp = -1L;
		}
		_shownLevel = player.Level;
		if (instant)
		{
			_exp.Snap(num);
		}
		else
		{
			_exp.Update(num, elapsed, now);
		}
	}

	private void RefreshPortrait()
	{
		if (MyEntity == null)
		{
			return;
		}
		Guid[] array = EquipmentOf(MyEntity);
		string text = $"{MyEntity.Face}|{MyEntity.Sprite}|{MyEntity.TransformedSprite}|{MyEntity.Texture?.Name}|{string.Join(",", array)}";
		if (text == _portraitKey && _layers.Count > 0)
		{
			return;
		}
		_portraitKey = text;
		_layers.Clear();
		IGameTexture gameTexture = (string.IsNullOrWhiteSpace(MyEntity.Face) ? null : Globals.ContentManager.GetTexture(TextureType.Face, MyEntity.Face));
		Color tint = MyEntity.Color ?? SaiyanHud.White;
		if (gameTexture != null)
		{
			_layers.Add(new PortraitLayer(gameTexture, new Rectangle(0, 0, gameTexture.Width, gameTexture.Height), tint, IsFace: true));
			return;
		}
		if (MyEntity.Texture != null)
		{
			SpriteSheets.Pose? pose = SpriteSheets.StandingBody(MyEntity.Sprite, MyEntity.Texture.Name);
			if (pose.HasValue)
			{
				SpriteSheets.Pose valueOrDefault = pose.GetValueOrDefault();
				if (!valueOrDefault.IsEmpty)
				{
					IGameTexture texture = valueOrDefault.Texture;
					Rectangle rectangle = new Rectangle(valueOrDefault.Frame.X, valueOrDefault.Frame.Y, valueOrDefault.Frame.Width, valueOrDefault.Frame.Height);
					_layers.Add(new PortraitLayer(texture, rectangle, tint, IsFace: false));
					List<string> list = Options.Instance.Equipment.Paperdoll.Directions[1];
					List<string> slots = Options.Instance.Equipment.Slots;
					for (int i = 0; i < list.Count; i++)
					{
						int num = slots.IndexOf(list[i]);
						if (num < 0 || num >= array.Length || array[num] == Guid.Empty || !DatabaseObject<ItemDescriptor>.TryGet(array[num], out var databaseObject))
						{
							continue;
						}
						string text2 = Paperdolls.ForClass((MyEntity.Gender == Gender.Male) ? databaseObject.MalePaperdoll : databaseObject.FemalePaperdoll, (MyEntity is Player player) ? player.Class : Guid.Empty);
						text2 = ((MyEntity is Entity entity) ? Paperdolls.ForForm(entity, text2) : text2);
						if (string.IsNullOrWhiteSpace(text2))
						{
							continue;
						}
						pose = SpriteSheets.StandingOutfit(MyEntity.Sprite, text2, valueOrDefault);
						if (pose.HasValue)
						{
							SpriteSheets.Pose valueOrDefault2 = pose.GetValueOrDefault();
							if (!valueOrDefault2.IsEmpty)
							{
								(int, int, int, int) frame = valueOrDefault2.Frame;
								_layers.Add(new PortraitLayer(valueOrDefault2.Texture, new Rectangle(frame.Item1, frame.Item2, frame.Item3, frame.Item4), databaseObject.Color, IsFace: false, texture, rectangle));
							}
						}
					}
					return;
				}
			}
		}
		_portraitKey = null;
	}

	private static Guid[] EquipmentOf(IEntity entity)
	{
		Guid[] array = (entity as Entity)?.Equipment ?? Array.Empty<Guid>();
		if (entity is Player player && player == Globals.Me)
		{
			array = new Guid[player.MyEquipment.Length];
			for (int i = 0; i < player.MyEquipment.Length; i++)
			{
				int num = player.MyEquipment[i];
				array[i] = ((num > -1 && num < Options.Instance.Player.MaxInventory) ? player.Inventory[num].ItemId : Guid.Empty);
			}
		}
		return array;
	}

	public void Dispose()
	{
		EntityWindow.Hide();
		EntityWindow.Canvas?.RemoveChild(EntityWindow, dispose: true);
	}

	public void Hide()
	{
		EntityWindow.Hide();
	}

	public void Show()
	{
		if ((Options.Instance.Combat.EnableTargetWindow || _isPlayerBox) && (_isPlayerBox || !DungeonHud.EstLeBossAffiche(MyEntity)))
		{
			EntityWindow.Show();
		}
	}

	private void DrawChargesDash(Intersect.Client.Framework.Gwen.Renderer.Base renderer, float droite)
	{
		long milliseconds = Timing.Global.Milliseconds;
		Player me = Globals.Me;
		if (me == null || !me.ChargesDuDash(milliseconds, out var charges, out var maximum, out var recharge))
		{
			return;
		}
		if (_chargesDashVues >= 0 && charges > _chargesDashVues)
		{
			_chargeDashRevenueA = milliseconds;
		}
		else if (_chargesDashVues >= 0 && charges < _chargesDashVues)
		{
			_chargeDashPriseA = milliseconds;
		}
		_chargesDashVues = charges;
		int num = L(14f);
		int num2 = L(38f) - num / 2;
		float num3 = (float)num / 2f;
		float num4 = LargeurDUnePastille(maximum);
		float num5 = droite - (num3 + (float)maximum * num4 + (float)(maximum - 1) * F(4f));
		HudShapes.Ramp ramp = new HudShapes.Ramp(DashClair, DashPlein, DashFonce);
		for (int i = 0; i < maximum; i++)
		{
			float num6 = num5 + (float)i * (num4 + F(4f));
			HudShapes.Slanted(renderer, num6 - 1f, num2, num3 + num4 + 2f, num, num3, HudShapes.Ramp.Solid(Track));
			if (i == charges && milliseconds - _chargeDashPriseA < 250)
			{
				float num7 = 1f - (float)(milliseconds - _chargeDashPriseA) / 250f;
				HudShapes.Slanted(renderer, num6, num2, num3 + num4, num, num3, HudShapes.Ramp.Solid(SaiyanHud.White), 0.75f * num7);
			}
			float num8 = ((i < charges) ? 1f : ((i == charges) ? recharge : 0f));
			if (!(num8 <= 0f))
			{
				HudShapes.Slanted(renderer, num6, num2, num3 + num8 * num4, num, num3, ramp, (num8 >= 1f) ? 1f : 0.55f);
				if (num8 >= 1f && i == charges - 1 && milliseconds - _chargeDashRevenueA < 350)
				{
					float num9 = 1f - (float)(milliseconds - _chargeDashRevenueA) / 350f;
					HudShapes.Glow(renderer, num6 + (num3 + num4) / 2f, (float)num2 + (float)num / 2f, num4, num, SaiyanHud.WithAlpha(SaiyanHud.White, 0.8f * num9));
				}
				if (i == maximum - 1 && milliseconds - me.NouvelleChargeDeDashA < 1500)
				{
					float num10 = (float)(milliseconds - me.NouvelleChargeDeDashA) / 1500f;
					float num11 = (1f - num10) * (0.6f + 0.4f * MathF.Sin(num10 * ((float)Math.PI * 2f) * 3f));
					HudShapes.Glow(renderer, num6 + (num3 + num4) / 2f, (float)num2 + (float)num / 2f, num4 * 2f, (float)num * 1.6f, SaiyanHud.WithAlpha(DashClair, 0.9f * num11));
				}
			}
		}
		DrawToucheDash(renderer, milliseconds, (int)MathF.Round(num5 - F(5f)), num2, num, me.DashRefuseA);
	}

	private float LargeurDash(Intersect.Client.Framework.Gwen.Renderer.Base renderer)
	{
		if (_isPlayerBox && MyEntity == Globals.Me)
		{
			Player me = Globals.Me;
			if (me != null && me.ChargesDuDash(Timing.Global.Milliseconds, out var _, out var maximum, out var _))
			{
				LireLaToucheDash(Timing.Global.Milliseconds);
				float num = (float)L(14f) / 2f;
				float num2 = num + (float)maximum * LargeurDUnePastille(maximum) + (float)(maximum - 1) * F(4f);
				return num2 + F(5f) + (float)LargeurDeLaToucheDash(renderer, L(14f)) + F(10f);
			}
		}
		return 0f;
	}

	private void LireLaToucheDash(long now)
	{
		if (now - _toucheDashLueA <= 1000 && (_toucheDashEspace || !string.IsNullOrEmpty(_toucheDash)))
		{
			return;
		}
		_toucheDashLueA = now;
		_toucheDash = string.Empty;
		_toucheDashEspace = false;
		if (!Controls.ActiveControls.TryGetMappingFor(Control.Dash, out ControlMapping mapping))
		{
			return;
		}
		ControlBinding controlBinding = mapping.Bindings.FirstOrDefault((ControlBinding binding) => binding.Key != Keys.None);
		if (controlBinding != null)
		{
			if (controlBinding.Key == Keys.Space)
			{
				_toucheDashEspace = true;
				return;
			}
			string text = controlBinding.Key.GetKeyId().ToLowerInvariant();
			_toucheDash = (Strings.Keys.KeyDictionary.TryGetValue(text, out LocalizedString value) ? value.ToString() : text.ToUpperInvariant());
		}
	}

	private int LargeurDeLaToucheDash(Intersect.Client.Framework.Gwen.Renderer.Base renderer, int height)
	{
		if (_toucheDashEspace)
		{
			return height * 2;
		}
		if (!string.IsNullOrEmpty(_toucheDash))
		{
			return Math.Max(height, SaiyanHud.Measure(renderer, SaiyanHud.StrongFont, Pt(9f), _toucheDash).X + L(6f));
		}
		return height;
	}

	private void DrawToucheDash(Intersect.Client.Framework.Gwen.Renderer.Base renderer, long now, int droite, int top, int height, long refuseA)
	{
		LireLaToucheDash(now);
		if (_toucheDashEspace || !string.IsNullOrEmpty(_toucheDash))
		{
			int num = LargeurDeLaToucheDash(renderer, height);
			int num2 = droite - num;
			int num3 = ((now - _chargeDashPriseA < 120) ? Math.Max(1, L(1f)) : 0);
			bool flag = now - refuseA < 400;
			HudShapes.Fill(renderer, num2, top + Math.Max(1, L(2f)), num, height - 1, SaiyanHud.WithAlpha(Black, 0.55f));
			HudShapes.Fill(renderer, num2, top + num3, num, height - 1, flag ? ToucheRefus : ToucheDessus);
			HudShapes.Fill(renderer, num2, top + num3, num, Math.Max(1, L(1f)), flag ? ToucheRefusLiseret : ToucheLiseret);
			if (_toucheDashEspace)
			{
				int num4 = Math.Max(1, L(1f));
				int num5 = num2 + num / 4;
				int num6 = num / 2;
				int num7 = top + num3 + (int)MathF.Round((float)(height - 1) * 0.68f);
				int num8 = Math.Max(2, (int)MathF.Round((float)height * 0.25f));
				HudShapes.Fill(renderer, num5, num7, num6, num4, SaiyanHud.White);
				HudShapes.Fill(renderer, num5, num7 - num8, num4, num8, SaiyanHud.White);
				HudShapes.Fill(renderer, num5 + num6 - num4, num7 - num8, num4, num8, SaiyanHud.White);
			}
			else
			{
				int size = Pt(9f);
				Point point = SaiyanHud.Measure(renderer, SaiyanHud.StrongFont, size, _toucheDash);
				SaiyanHud.Text(renderer, SaiyanHud.StrongFont, size, _toucheDash, num2 + (num - point.X) / 2, top + num3 + (height - 1 - point.Y) / 2, SaiyanHud.White, shadow: false);
			}
		}
	}

	private float DrawJetonsEsquive(Intersect.Client.Framework.Gwen.Renderer.Base renderer, long _)
	{
		long milliseconds = Timing.Global.Milliseconds;
		float num = BordDroitDesCharges(renderer);
		if (!Vanish.Jetons(milliseconds, out var jetons, out var maximum, out var recharge))
		{
			return num;
		}
		if (_jetonsVus >= 0 && jetons > _jetonsVus)
		{
			_jetonRevenuA = milliseconds;
		}
		else if (_jetonsVus >= 0 && jetons < _jetonsVus)
		{
			_jetonPrisA = milliseconds;
		}
		_jetonsVus = jetons;
		int num2 = L(14f);
		int num3 = L(38f) - num2 / 2;
		float num4 = (float)num2 / 2f;
		float num5 = LargeurDUnePastille(maximum);
		float num6 = num - (num4 + (float)maximum * num5 + (float)(maximum - 1) * F(4f));
		HudShapes.Ramp ramp = new HudShapes.Ramp(JetonClair, JetonPlein, JetonFonce);
		for (int i = 0; i < maximum; i++)
		{
			float num7 = num6 + (float)i * (num5 + F(4f));
			HudShapes.Slanted(renderer, num7 - 1f, num3, num4 + num5 + 2f, num2, num4, HudShapes.Ramp.Solid(Track));
			if (i == jetons && milliseconds - _jetonPrisA < 250)
			{
				float num8 = 1f - (float)(milliseconds - _jetonPrisA) / 250f;
				HudShapes.Slanted(renderer, num7, num3, num4 + num5, num2, num4, HudShapes.Ramp.Solid(SaiyanHud.White), 0.75f * num8);
			}
			float num9 = ((i < jetons) ? 1f : ((i == jetons) ? recharge : 0f));
			if (!(num9 <= 0f))
			{
				HudShapes.Slanted(renderer, num7, num3, num4 + num9 * num5, num2, num4, ramp, (num9 >= 1f) ? 1f : 0.55f);
				if (num9 >= 1f && i == jetons - 1 && milliseconds - _jetonRevenuA < 350)
				{
					float num10 = 1f - (float)(milliseconds - _jetonRevenuA) / 350f;
					HudShapes.Glow(renderer, num7 + (num4 + num5) / 2f, (float)num3 + (float)num2 / 2f, num5, num2, SaiyanHud.WithAlpha(SaiyanHud.White, 0.8f * num10));
				}
				if (i == maximum - 1 && milliseconds - Vanish.NouvelleChargeA < 1500)
				{
					float num11 = (float)(milliseconds - Vanish.NouvelleChargeA) / 1500f;
					float num12 = (1f - num11) * (0.6f + 0.4f * MathF.Sin(num11 * ((float)Math.PI * 2f) * 3f));
					HudShapes.Glow(renderer, num7 + (num4 + num5) / 2f, (float)num3 + (float)num2 / 2f, num5 * 2f, (float)num2 * 1.6f, SaiyanHud.WithAlpha(JetonClair, 0.9f * num12));
				}
			}
		}
		return (float)DrawToucheEsquive(renderer, milliseconds, (int)MathF.Round(num6 - F(5f)), num3, num2) - F(10f);
	}

	private float BordDroitDesCharges(Intersect.Client.Framework.Gwen.Renderer.Base renderer)
	{
		float num = F(450f);
		if (!string.IsNullOrEmpty(_rightValue))
		{
			num -= (float)(SaiyanHud.Measure(renderer, SaiyanHud.StrongFont, Pt(13f), _rightValue).X + L(28f));
		}
		return num;
	}

	private float LargeurDUnePastille(int maximum)
	{
		return F((maximum <= 2) ? 16f : 12f);
	}

	private float LargeurEsquive(Intersect.Client.Framework.Gwen.Renderer.Base renderer)
	{
		if (!_isPlayerBox || MyEntity != Globals.Me || !Vanish.Jetons(Timing.Global.Milliseconds, out var _, out var maximum, out var _))
		{
			return 0f;
		}
		float num = (float)L(14f) / 2f;
		float num2 = num + (float)maximum * LargeurDUnePastille(maximum) + (float)(maximum - 1) * F(4f);
		int num3 = (string.IsNullOrEmpty(_toucheEsquive) ? L(14f) : Math.Max(L(14f), SaiyanHud.Measure(renderer, SaiyanHud.StrongFont, Pt(9f), _toucheEsquive).X + L(6f)));
		return num2 + F(5f) + (float)num3 + (float)L(10f);
	}

	private int DrawToucheEsquive(Intersect.Client.Framework.Gwen.Renderer.Base renderer, long now, int droite, int top, int height)
	{
		if (now - _toucheLueA > 1000 || string.IsNullOrEmpty(_toucheEsquive))
		{
			_toucheLueA = now;
			_toucheEsquive = NomDeLaToucheDEsquive();
		}
		if (string.IsNullOrEmpty(_toucheEsquive))
		{
			return droite;
		}
		int size = Pt(9f);
		Point point = SaiyanHud.Measure(renderer, SaiyanHud.StrongFont, size, _toucheEsquive);
		int num = Math.Max(height, point.X + L(6f));
		int num2 = droite - num;
		int num3 = ((now - _jetonPrisA < 120) ? Math.Max(1, L(1f)) : 0);
		bool flag = now - Vanish.RefuseeA < 400;
		HudShapes.Fill(renderer, num2, top + Math.Max(1, L(2f)), num, height - 1, SaiyanHud.WithAlpha(Black, 0.55f));
		HudShapes.Fill(renderer, num2, top + num3, num, height - 1, flag ? ToucheRefus : ToucheDessus);
		HudShapes.Fill(renderer, num2, top + num3, num, Math.Max(1, L(1f)), flag ? ToucheRefusLiseret : ToucheLiseret);
		SaiyanHud.Text(renderer, SaiyanHud.StrongFont, size, _toucheEsquive, num2 + (num - point.X) / 2, top + num3 + (height - 1 - point.Y) / 2, SaiyanHud.White, shadow: false);
		return num2;
	}

	private static string NomDeLaToucheDEsquive()
	{
		if (!Controls.ActiveControls.TryGetMappingFor(Control.Dodge, out ControlMapping mapping))
		{
			return string.Empty;
		}
		ControlBinding controlBinding = mapping.Bindings.FirstOrDefault((ControlBinding binding) => binding.Key != Keys.None);
		if (controlBinding == null)
		{
			return string.Empty;
		}
		string text = controlBinding.Key.GetKeyId().ToLowerInvariant();
		if (!Strings.Keys.KeyDictionary.TryGetValue(text, out LocalizedString value))
		{
			return text.ToUpperInvariant();
		}
		return value.ToString();
	}

	private int L(float value)
	{
		return (int)MathF.Round(value * _z);
	}

	private float F(float value)
	{
		return value * _z;
	}

	private int Pt(float points)
	{
		float num = points * _z;
		int num2 = FontSizes[0];
		int[] fontSizes = FontSizes;
		foreach (int num3 in fontSizes)
		{
			if (MathF.Abs((float)num3 - num) < MathF.Abs((float)num2 - num))
			{
				num2 = num3;
			}
		}
		return num2;
	}

	private static float ScaleFor(Intersect.Client.Framework.Gwen.Control.Base? canvas)
	{
		if (canvas == null || canvas.Width <= 0 || canvas.Height <= 0)
		{
			return 1f;
		}
		float val = Math.Min((float)canvas.Width / 1920f, (float)canvas.Height / 1080f);
		return Math.Max(val, 0.6958333f);
	}

	private void PlaceContent(float scale, int x, int y)
	{
		int val = (int)MathF.Round(48f * scale);
		Point point = new Point(Math.Min(x, val), Math.Min(y, val));
		if (MathF.Abs(scale - _z) > 0.001f || point != _origin)
		{
			_z = scale;
			_origin = point;
			Layout();
		}
		EntityWindow.SetPosition(x - point.X, y - point.Y);
	}

	private void Layout()
	{
		if (_isPlayerBox)
		{
			LayoutSocle();
			return;
		}
		int x = _origin.X;
		int y = _origin.Y;
		int num = L(48f);
		int num2 = L(491f);
		int num3 = (_isPlayerBox ? L(130f) : (L(132f) + 44));
		EntityWindow.SetSize(x + num2 + num, y + num3 + num);
		_panel.SetBounds(0, 0, x + num2 + num, y + num3 + num);
		_hpHover.SetBounds(x + L(128f), y + L(54f), L(330f), L(26f));
		_mpHover.SetBounds(x + L(140f), y + L(85f), L(226f), L(14f));
		_expHover?.SetBounds(x + L(124f), y + L(107f) - L(5f), L(322f), L(4f) + L(10f));
		EventDesc?.SetBounds(x + L(134f), y + L(54f), L(330f), L(50f));
		_contextMenuButton?.SetBounds(x + L(386f), y + L(85f), 15, 15);
		EntityStatusWindow?.SetBounds(x + L(128f), y + L(132f), L(330f), 44);
		EntityStatusPanel?.SetBounds(0, 0, L(330f), 44);
	}

	private void RefreshLook(long now, bool instant)
	{
		if (MyEntity != null)
		{
			string transformedSprite = MyEntity.TransformedSprite;
			bool flag = !string.IsNullOrWhiteSpace(transformedSprite) && EntityType == EntityType.Player;
			SaiyanHud.Accent accent = SaiyanHud.AccentOf(MyEntity);
			_auraIsUltra = flag && Transformation.IsUltraForm(transformedSprite);
			_aura = (flag ? new SaiyanHud.Accent?(accent) : ((SaiyanHud.Accent?)null));
			bool flag2 = MyEntity == Globals.Me;
			SaiyanHud.Accent frame = ((!(_isPlayerBox || flag2)) ? (EntityType switch
			{
				EntityType.Player => Ally, 
				EntityType.GlobalEntity => Hostile, 
				_ => Neutral, 
			}) : accent);
			_frame = frame;
			if (!instant && _lastForm != null && !string.Equals(_lastForm, transformedSprite ?? string.Empty, StringComparison.OrdinalIgnoreCase))
			{
				_formAt = now;
			}
			_lastForm = transformedSprite ?? string.Empty;
			RefreshFormLabel(flag);
		}
	}

	private void RefreshFormLabel(bool transformed)
	{
		if (!transformed || MyEntity == null)
		{
			_formLabel = string.Empty;
			_formSpellId = Guid.Empty;
			return;
		}
		Guid guid = Guid.Empty;
		foreach (IStatus item in MyEntity.Status)
		{
			if (item.Type == SpellEffect.Transform)
			{
				guid = item.SpellId;
				break;
			}
		}
		if (!(guid == _formSpellId) || _formLabel.Length <= 0)
		{
			_formSpellId = guid;
			_formLabel = ((guid != Guid.Empty && DatabaseObject<SpellDescriptor>.TryGet(guid, out var databaseObject) && !string.IsNullOrWhiteSpace(databaseObject.Name)) ? databaseObject.Name.ToUpperInvariant() : string.Empty);
		}
	}

	private void TrackHit(float ratio, long now, bool instant)
	{
		if (!instant && _lastHealth >= 0f && ratio < _lastHealth - 0.001f)
		{
			_hitAt = now;
			_hitBig = _lastHealth - ratio >= 0.12f;
		}
		_lastHealth = ratio;
	}

	private static float Phase(long now, long period)
	{
		return (float)(now % period) / (float)period;
	}

	private static float Flash(long now, long since, long duration)
	{
		if (since == long.MinValue || now < since)
		{
			return 0f;
		}
		float num = (float)(now - since) / (float)duration;
		if (!(num >= 1f))
		{
			return 1f - num;
		}
		return 0f;
	}

	private void Draw(Intersect.Client.Framework.Gwen.Renderer.Base renderer)
	{
		if (MyEntity == null || _z <= 0f)
		{
			return;
		}
		long millisecondsUtc = Timing.Global.MillisecondsUtc;
		bool flag = EntityType == EntityType.Event;
		bool flag2 = !flag && _hp.Target > 0f && _hp.Target < 0.25f;
		Moment moment = new Moment(millisecondsUtc, flag2, flag2 ? (0.5f + 0.5f * MathF.Sin(Phase(millisecondsUtc, 900L) * ((float)Math.PI * 2f))) : 0f, Flash(millisecondsUtc, _hitAt, _hitBig ? 260 : 160), _isPlayerBox ? Flash(millisecondsUtc, _levelUpAt, 900L) : 0f, Flash(millisecondsUtc, _formAt, 700L));
		Point renderOffset = renderer.RenderOffset;
		int num = 0;
		int num2 = 0;
		if (moment.Hit > 0f && _hitBig)
		{
			float num3 = (1f - moment.Hit) * 260f;
			num = (int)MathF.Round(MathF.Sin(num3 * 0.32f) * F(3.2f) * moment.Hit);
			num2 = (int)MathF.Round(MathF.Cos(num3 * 0.45f) * F(1.6f) * moment.Hit);
		}
		renderer.RenderOffset = new Point(renderOffset.X + _origin.X + num, renderOffset.Y + _origin.Y + num2);
		try
		{
			if (_isPlayerBox)
			{
				ChronoImage.DebutDesTours();
				DrawSocle(renderer, moment);
				ChronoImage.Tour("plaque : vie et ki");
				return;
			}
			ChronoImage.DebutDesTours();
			DrawPlate(renderer, moment);
			ChronoImage.Tour("plaque : cadre");
			DrawHeader(renderer);
			ChronoImage.Tour("plaque : en-tete");
			if (!flag)
			{
				DrawHealth(renderer, moment);
				ChronoImage.Tour("plaque : vie");
				DrawKi(renderer, millisecondsUtc);
				ChronoImage.Tour("plaque : ki");
				if (_isPlayerBox && MyEntity == Globals.Me)
				{
					DrawChargesDash(renderer, DrawJetonsEsquive(renderer, millisecondsUtc));
					DrawExperience(renderer, millisecondsUtc, moment.LevelUp);
					ChronoImage.Tour("plaque : experience");
				}
			}
			DrawCrest(renderer, moment);
			ChronoImage.Tour("plaque : portrait");
			if (!flag)
			{
				DrawLevelBadge(renderer, moment.LevelUp);
				ChronoImage.Tour("plaque : ecusson de niveau");
			}
		}
		finally
		{
			renderer.RenderOffset = renderOffset;
		}
	}

	private float PlateLeftAt(float y)
	{
		if (!(y <= F(98f)))
		{
			return F(60f) + (y - F(98f));
		}
		return F(60f);
	}

	private float PlateRightAt(float y)
	{
		return F(480f) - F(24f) * ((y - F(20f)) / F(92f));
	}

	private void DrawPlate(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Moment moment)
	{
		int num = L(20f);
		int num2 = L(112f);
		float num3 = F(5f);
		SaiyanHud.Accent frame = _frame;
		bool hasValue = _aura.HasValue;
		Color color = _aura?.Light ?? frame.Light;
		HudShapes.Glow(renderer, F(270f), F(74f), F(250f), F(66f), SaiyanHud.WithAlpha(Black, 0.5f), additive: false);
		Color color2 = (moment.LowHealth ? SaiyanHud.Lerp(frame.Main, SaiyanHud.Red, 0.4f + 0.6f * moment.LowPulse) : frame.Main);
		Color color3 = SaiyanHud.Lerp(color2, SaiyanHud.Night, 0.55f);
		for (int i = num + L(5f); i < num2 + L(5f); i++)
		{
			float num4 = (float)i + 0.5f;
			float right = PlateRightAt(num4 - num3) + num3;
			if (i < num2)
			{
				HudShapes.Span(renderer, PlateRightAt(num4), right, i, 1, SaiyanHud.Lerp(color2, frame.Light, 0.5f));
			}
			else
			{
				HudShapes.SpanGradient(renderer, PlateLeftAt(num4 - num3) + num3, right, i, 1, color3, SaiyanHud.Lerp(color2, frame.Light, 0.5f), 8);
			}
		}
		HudShapes.Ramp ramp = new HudShapes.Ramp(new Color(255, 29, 36, 57), new Color(255, 16, 20, 34), new Color(255, 9, 11, 20));
		Color color4 = SaiyanHud.WithAlpha(SaiyanHud.White, 0.035f);
		Color color5 = SaiyanHud.WithAlpha(SaiyanHud.Red, 0.22f * moment.Hit);
		int num5 = L(50f);
		for (int j = num; j < num2; j += 2)
		{
			int num6 = Math.Min(2, num2 - j);
			float num7 = (float)j + (float)num6 / 2f;
			float t = (num7 - (float)num) / (float)Math.Max(1, num2 - num);
			float left = PlateLeftAt(num7);
			float num8 = PlateRightAt(num7);
			HudShapes.Span(renderer, left, num8 - F(14f), j, num6, ramp.At(t));
			if (j < num5)
			{
				float right2 = ((num7 < F(35f)) ? (num8 - F(14f)) : (F(350f) - (num7 - F(35f)) * 0.4f));
				HudShapes.Span(renderer, left, right2, j, num6, color4);
			}
			if (moment.Hit > 0f)
			{
				HudShapes.Span(renderer, left, num8 - F(14f), j, num6, color5);
			}
			HudShapes.Span(renderer, num8 - F(14f), num8 - F(10f), j, num6, frame.Main);
			HudShapes.Span(renderer, num8 - F(10f), num8 - F(7f), j, num6, SaiyanHud.Lerp(frame.Main, SaiyanHud.Night, 0.9f));
			HudShapes.Span(renderer, num8 - F(7f), num8 - F(2f), j, num6, frame.Main);
			HudShapes.Span(renderer, num8 - F(2f), num8, j, num6, SaiyanHud.Lerp(frame.Main, frame.Light, 0.9f));
		}
		Rectangle previous = TechniqueArt.BeginClip(renderer, new Rectangle(L(60f), num, L(406f), num2 - num));
		HudShapes.Glow(renderer, F(62f), F(66f), F(300f), F(120f), SaiyanHud.WithAlpha(_aura?.Main ?? frame.Main, hasValue ? 0.3f : 0.18f));
		if (hasValue)
		{
			for (int k = 0; k < 5; k++)
			{
				float num9 = (Phase(moment.Now, 3556L) * F(640f) + F(k * 139)) % F(640f) - F(90f);
				float centerY = F(24 + k * 41 % 84);
				HudShapes.Glow(renderer, num9 + F(55f), centerY, F(55f), Math.Max(1.5f, F(1.5f)), SaiyanHud.WithAlpha(color, 0.22f));
			}
		}
		if (moment.LevelUp > 0f)
		{
			HudShapes.Glow(renderer, F(-75f + (1f - moment.LevelUp) * 760f), F(66f), F(70f), F(90f), SaiyanHud.WithAlpha(new Color(255, 255, 220, 140), 0.6f * moment.LevelUp));
		}
		if (moment.FormChange > 0f)
		{
			HudShapes.Glow(renderer, F(-75f + (1f - moment.FormChange) * 760f), F(66f), F(80f), F(90f), SaiyanHud.WithAlpha(color, 0.45f * moment.FormChange));
		}
		TechniqueArt.EndClip(renderer, previous);
		HudShapes.Fill(renderer, L(112f), num, L(368f), 1, SaiyanHud.WithAlpha(SaiyanHud.White, 0.16f));
		HudShapes.SpanGradient(renderer, F(112f), F(420f), num - Math.Max(1, L(1f)), Math.Max(2, L(2f)), frame.Light, SaiyanHud.WithAlpha(frame.Main, 0f), 16);
		HudShapes.Glow(renderer, F(220f), F(20f), F(140f), F(6f), SaiyanHud.WithAlpha(frame.Main, 0.35f));
		Color color6 = (moment.LowHealth ? SaiyanHud.WithAlpha(SaiyanHud.Red, 0.3f + 0.5f * moment.LowPulse) : SaiyanHud.WithAlpha(SaiyanHud.White, 0.08f));
		HudShapes.Span(renderer, F(60f), F(480f), num, 1, color6);
		HudShapes.Span(renderer, F(74f), F(456f), num2 - 1, 1, color6);
		if (moment.LowHealth)
		{
			HudShapes.Glow(renderer, F(270f), F(66f), F(250f), F(70f), SaiyanHud.WithAlpha(SaiyanHud.Red, 0.3f * moment.LowPulse));
		}
	}

	private void DrawHeader(Intersect.Client.Framework.Gwen.Renderer.Base renderer)
	{
		int num = L(38f);
		IFont nameFont = SaiyanHud.NameFont;
		int size = Pt(20f);
		int num2 = L(450f);
		int size2 = Pt(13f);
		int num3 = 0;
		if (!string.IsNullOrEmpty(_rightValue))
		{
			num3 = SaiyanHud.Measure(renderer, SaiyanHud.StrongFont, size2, _rightValue).X;
			num2 -= num3 + L(28f);
		}
		num2 -= (int)MathF.Round(LargeurEsquive(renderer) + LargeurDash(renderer));
		int size3 = Pt(10f);
		float spacing = F(1.6f);
		int num4 = ((!string.IsNullOrEmpty(_formLabel)) ? TechniqueArt.SpacedWidth(renderer, TechniqueArt.Label, size3, _formLabel, spacing) : 0);
		int num5 = ((num4 > 0) ? (num4 + L(24f) + L(14f)) : 0);
		int spacing2 = 0;
		string text = SaiyanHud.Fit(renderer, nameFont, size, _nameText, Math.Max(L(60f), num2 - L(12f) - num5 - L(134f)), ref spacing2);
		Point point = SaiyanHud.Measure(renderer, nameFont, size, text);
		int num6 = num - point.Y / 2;
		SaiyanHud.Text(renderer, nameFont, size, text, L(134f), num6 + Math.Max(1, L(2f)), SaiyanHud.WithAlpha(Black, 0.75f), shadow: false);
		Color third = SaiyanHud.Lerp(_aura?.Light ?? _frame.Light, SaiyanHud.White, 0.35f);
		TechniqueArt.Styled(renderer, nameFont, size, text, L(134f), num6, new TechniqueArt.Stops(SaiyanHud.White, 0f, SaiyanHud.White, 0.55f, third), 1f, italic: false);
		int num7 = L(134f) + point.X;
		if (num4 > 0)
		{
			SaiyanHud.Accent? aura = _aura;
			if (aura.HasValue)
			{
				SaiyanHud.Accent valueOrDefault = aura.GetValueOrDefault();
				int num8 = num7 + L(14f);
				int num9 = L(29f);
				int num10 = L(18f);
				int num11 = num4 + L(24f);
				HudShapes.Slanted(renderer, num8, num9, num11, num10, F(6f), HudShapes.Ramp.Solid(valueOrDefault.Main), 0.2f);
				HudShapes.SlantedOutline(renderer, num8, num9, num11, num10, F(6f), SaiyanHud.WithAlpha(valueOrDefault.Main, 0.85f));
				TechniqueArt.Spaced(renderer, TechniqueArt.Label, size3, _formLabel, num8 + L(12f), num9 + num10 / 2, spacing, valueOrDefault.Light);
				num7 = num8 + num11;
			}
		}
		if (num3 > 0)
		{
			int num12 = L(450f);
			SaiyanHud.Text(renderer, SaiyanHud.StrongFont, size2, _rightValue, num12 - num3, num + L(1f) - SaiyanHud.Measure(renderer, SaiyanHud.StrongFont, size2, _rightValue).Y / 2, SaiyanHud.Power);
			int num13 = num12 - num3 - L(10f);
			int num14 = TechniqueArt.SpacedWidth(renderer, TechniqueArt.Label, size3, _rightLabel, spacing);
			bool flag = num13 - num14 - L(26f) > num7 + L(12f);
			if (flag)
			{
				TechniqueArt.SpacedRight(renderer, TechniqueArt.Label, size3, _rightLabel, num13, num + L(1f), spacing, SaiyanHud.Muted);
			}
			float num15 = (float)(num13 - (flag ? num14 : (-4))) - F(14f);
			float num16 = (float)num + F(1f);
			float num17 = Math.Max(1f, F(1.5f));
			HudShapes.Circle(renderer, num15, num16, F(5.5f), num17, SaiyanHud.WithAlpha(SaiyanHud.Power, 0.9f));
			HudShapes.Fill(renderer, (int)(num15 - F(9f)), (int)num16, L(5f), (int)num17, SaiyanHud.WithAlpha(SaiyanHud.Power, 0.9f));
			HudShapes.Fill(renderer, (int)(num15 + F(4f)), (int)num16, L(5f), (int)num17, SaiyanHud.WithAlpha(SaiyanHud.Power, 0.9f));
		}
	}

	private void DrawHealth(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Moment moment)
	{
		float num = F(128f);
		int num2 = L(54f);
		float width = F(330f);
		int num3 = L(26f);
		float slant = (float)num3 / 2f;
		if (moment.LowHealth)
		{
			HudShapes.Glow(renderer, num + width / 2f, (float)num2 + (float)num3 / 2f, width / 2f + F(24f), (float)num3 / 2f + F(18f), SaiyanHud.WithAlpha(SaiyanHud.Red, 0.8f * moment.LowPulse));
		}
		float num4 = FillOf(_hp.Shown);
		float num5 = FillOf(_hp.Ghost);
		float num6 = FillOf(_hp.Target);
		float num7 = ((_shieldRatio > 0f && num4 > 0f) ? Math.Min(width, num4 + width * _shieldRatio) : 0f);
		Color color = (_hp.IsHolding(moment.Now) ? new Color(247, 255, 246, 226) : new Color(224, 255, 150, 80));
		Color color2 = new Color(199, 140, 245, 170);
		HudShapes.Ramp ramp = new HudShapes.Ramp(new Color(255, 255, 132, 108), new Color(255, 230, 58, 58), new Color(255, 120, 14, 30), 0.45f);
		Color color3 = SaiyanHud.WithAlpha(new Color(255, 255, 236, 226), 0.55f + 0.45f * _hp.Head);
		Color color4 = SaiyanHud.WithAlpha(SaiyanHud.KiLight, 0.85f);
		Color color5 = SaiyanHud.WithAlpha(Black, 0.4f);
		int num8 = Math.Max(2, L(5f));
		float num9 = Math.Max(1.5f, F(2f));
		for (int i = 0; i < num3; i++)
		{
			float num10 = ((float)i + 0.5f) / (float)num3;
			int y = num2 + i;
			float num11 = num + slant * (1f - num10);
			float num12 = slant * num10;
			HudShapes.Span(renderer, num11 - 1.5f, num + width - num12 + 1.5f, y, 1, Track);
			if (num7 > num4)
			{
				HudShapes.Span(renderer, num11, num + num7 - num12, y, 1, color4);
			}
			if (num5 > num4 + 0.5f)
			{
				HudShapes.Span(renderer, num11, num + num5 - num12, y, 1, color);
			}
			if (num6 > num4 + 0.5f)
			{
				HudShapes.Span(renderer, num11, num + num6 - num12, y, 1, color2);
			}
			if (num4 > 0f)
			{
				Color color6 = ramp.At(num10);
				if (num10 < 0.45f)
				{
					color6 = SaiyanHud.Lerp(color6, SaiyanHud.White, 0.35f * (1f - num10 / 0.45f));
				}
				float num13 = num + num4 - num12;
				HudShapes.Span(renderer, num11, num13, y, 1, color6);
				HudShapes.Span(renderer, Math.Max(num11, num13 - num9), num13, y, 1, color3);
			}
			if (i >= 2 && i < num3 - 3 && i % 2 == 0)
			{
				for (int j = 1; j < 10; j++)
				{
					float num14 = num + slant + (float)j / 10f * (width - slant) - (slant - 1f) * (num10 + 0.5f / (float)num3);
					HudShapes.Fill(renderer, (int)num14, y, 1, 2, color5);
				}
			}
			if (i < num8)
			{
				HudShapes.Span(renderer, num11, num + width - num12, y, 1, SaiyanHud.WithAlpha(Black, 0.5f * (1f - (float)i / (float)num8)));
			}
		}
		if (num4 > 0f)
		{
			HudShapes.Glow(renderer, num + num4 - slant / 2f, (float)num2 + (float)num3 / 2f, num3, (float)num3 / 2f + F(2f), SaiyanHud.WithAlpha(SaiyanHud.White, 0.2f + 0.6f * _hp.Head));
		}
		HudShapes.SlantedOutline(renderer, num + 0.5f, num2, width - 1f, num3, slant, SaiyanHud.WithAlpha(SaiyanHud.White, 0.14f));
		int num15 = num2 + num3 / 2;
		IFont smallFont = SaiyanHud.SmallFont;
		int size = Pt(11f);
		string text = Strings.EntityBox.ShortVital0.ToString();
		int y2 = SaiyanHud.Measure(renderer, smallFont, size, text).Y;
		SaiyanHud.Text(renderer, smallFont, size, text, (int)(num + slant) + L(8f), num15 - y2 / 2, SaiyanHud.WithAlpha(SaiyanHud.White, 0.92f));
		int size2 = Pt(13f);
		Point point = SaiyanHud.Measure(renderer, SaiyanHud.StrongFont, size2, _hpText);
		SaiyanHud.Text(renderer, SaiyanHud.StrongFont, size2, _hpText, (int)(num + width - slant) - L(8f) - point.X, num15 - point.Y / 2, SaiyanHud.White);
		float FillOf(float ratio)
		{
			if (!(ratio <= 0f))
			{
				return slant + Math.Clamp(ratio, 0f, 1f) * (width - slant);
			}
			return 0f;
		}
	}

	private void DrawKi(Intersect.Client.Framework.Gwen.Renderer.Base renderer, long now)
	{
		float num = F(140f);
		int num2 = L(85f);
		float num3 = F(226f);
		int num4 = L(14f);
		float num5 = (float)num4 / 2f;
		float num6 = F(3f);
		float num7 = (num3 - num5 - num6 * 9f) / 10f;
		bool flag = _mp.Target >= 0.999f;
		if (flag)
		{
			float num8 = 0.55f + 0.35f * MathF.Sin(Phase(now, 2200L) * ((float)Math.PI * 2f));
			HudShapes.Glow(renderer, num + num3 / 2f, (float)num2 + (float)num4 / 2f, num3 / 2f + F(18f), (float)num4 / 2f + F(14f), SaiyanHud.WithAlpha(SaiyanHud.KiLight, 0.6f * num8));
		}
		HudShapes.Ramp ramp = new HudShapes.Ramp(new Color(255, 170, 218, 255), new Color(255, 74, 146, 255), new Color(255, 30, 62, 184));
		Color color = (_mp.IsHolding(now) ? new Color(242, 235, 245, 255) : new Color(153, 130, 170, 255));
		IGameTexture gameTexture = SaiyanHud.Texture("hud_joueur_stries.png");
		for (int i = 0; i < 10; i++)
		{
			float num9 = num + (float)i * (num7 + num6);
			float num10 = Math.Clamp(_mp.Shown * 10f - (float)i, 0f, 1f);
			float num11 = Math.Clamp(_mp.Ghost * 10f - (float)i, 0f, 1f);
			HudShapes.Slanted(renderer, num9 - 1f, num2, num5 + num7 + 2f, num4, num5, HudShapes.Ramp.Solid(Track));
			if (num11 > num10 + 0.01f)
			{
				HudShapes.Slanted(renderer, num9, num2, num5 + num11 * num7, num4, num5, HudShapes.Ramp.Solid(color));
			}
			if (num10 <= 0f)
			{
				continue;
			}
			HudShapes.Slanted(renderer, num9, num2, num5 + num10 * num7, num4, num5, ramp);
			HudShapes.Fill(renderer, (int)MathF.Ceiling(num9 + num5), num2, (int)(num10 * num7), Math.Max(1, L(2f)), SaiyanHud.WithAlpha(SaiyanHud.White, 0.3f));
			if (gameTexture != null && num10 * num7 > num5)
			{
				int num12 = Math.Max(4, (int)MathF.Round((float)(gameTexture.Width * num4) / (float)gameTexture.Height));
				Rectangle previous = TechniqueArt.BeginClip(renderer, new Rectangle((int)MathF.Ceiling(num9 + num5), num2, (int)(num10 * num7 - num5), num4));
				int num13 = (int)((float)(now % 60000) / 1000f * 30f * _z % (float)num12);
				for (int j = (int)num9 - num13; (float)j < num9 + num5 + num7; j += num12)
				{
					SaiyanHud.Draw(renderer, gameTexture, j, num2, num12, num4, SaiyanHud.WithAlpha(SaiyanHud.White, 0.7f));
				}
				TechniqueArt.EndClip(renderer, previous);
			}
		}
		if (flag)
		{
			float num14 = (float)(now % 2200) / 2200f;
			float centerX = num - F(35f) + num14 * (num3 + F(70f));
			for (int k = 0; k < num4; k++)
			{
				float num15 = ((float)k + 0.5f) / (float)num4;
				int num16 = (int)MathF.Ceiling(num + num5 * (1f - num15));
				int num17 = (int)MathF.Floor(num + num3 - num5 * num15);
				if (num17 > num16)
				{
					Rectangle previous2 = TechniqueArt.BeginClip(renderer, new Rectangle(num16, num2 + k, num17 - num16, 1));
					HudShapes.Glow(renderer, centerX, (float)num2 + (float)num4 / 2f, F(35f), num4, SaiyanHud.WithAlpha(SaiyanHud.White, 0.6f));
					TechniqueArt.EndClip(renderer, previous2);
				}
			}
		}
		int num18 = num2 + num4 / 2;
		int size = Pt(10f);
		Point point = SaiyanHud.Measure(renderer, SaiyanHud.StrongFont, size, _mpText);
		SaiyanHud.Text(renderer, SaiyanHud.StrongFont, size, _mpText, (int)(num + num3 - num5) - L(6f) - point.X, num18 - point.Y / 2, SaiyanHud.White);
		int size2 = Pt(10f);
		string text = Strings.EntityBox.ShortVital1.ToString();
		Point point2 = SaiyanHud.Measure(renderer, SaiyanHud.SmallFont, size2, text);
		SaiyanHud.Text(renderer, SaiyanHud.SmallFont, size2, text, (int)num - L(6f) - point2.X, num18 - point2.Y / 2, new Color(255, 170, 212, 255));
	}

	private void DrawExperience(Intersect.Client.Framework.Gwen.Renderer.Base renderer, long now, float levelUp)
	{
		int num = L(124f);
		int num2 = L(107f);
		int num3 = L(322f);
		int num4 = Math.Max(2, L(4f));
		int num5 = (int)MathF.Round((float)num3 * Math.Clamp(_exp.Shown, 0f, 1f));
		HudShapes.Fill(renderer, num - 1, num2 - 1, num3 + 2, num4 + 2, SaiyanHud.WithAlpha(Black, 0.7f));
		if (num5 > 0)
		{
			TechniqueArt.FillVertical(renderer, new Rectangle(num, num2, num5, num4), new Color(255, 255, 238, 176), new Color(255, 232, 150, 40));
			float num6 = Flash(now, _expShineAt, 900L);
			if (num6 > 0f)
			{
				Rectangle previous = TechniqueArt.BeginClip(renderer, new Rectangle(num, num2, num5, num4));
				HudShapes.Glow(renderer, (float)num + (1f - num6) * (float)num5, (float)num2 + (float)num4 / 2f, F(30f), num4, SaiyanHud.WithAlpha(SaiyanHud.White, 0.95f * num6));
				TechniqueArt.EndClip(renderer, previous);
			}
		}
		if (levelUp > 0f)
		{
			HudShapes.Fill(renderer, num, num2, num3, num4, SaiyanHud.WithAlpha(SaiyanHud.White, 0.85f * levelUp));
		}
		for (int i = 1; i < 10; i++)
		{
			HudShapes.Fill(renderer, num + (int)MathF.Round((float)(num3 * i) / 10f), num2, 1, num4, SaiyanHud.WithAlpha(Black, 0.5f));
		}
		int num7 = num + num5;
		float num8 = (float)num2 + (float)num4 / 2f;
		HudShapes.Glow(renderer, num7, num8, F(20f), F(20f), SaiyanHud.WithAlpha(new Color(255, 255, 190, 80), 0.5f + 0.4f * _exp.Head));
		IGameTexture texture = SaiyanHud.Texture("hud_joueur_dragonball.png");
		int num9 = Math.Max(12, L(16f));
		SaiyanHud.Draw(renderer, texture, (int)MathF.Round((float)num7 - (float)num9 / 2f), (int)MathF.Round(num8 - (float)num9 / 2f), num9, num9, SaiyanHud.White);
		int num10 = L(92f);
		int size = Pt(10f);
		Point point = SaiyanHud.Measure(renderer, SaiyanHud.StrongFont, size, _expText);
		int num11 = L(444f);
		SaiyanHud.Text(renderer, SaiyanHud.StrongFont, size, _expText, num11 - point.X, num10 - point.Y / 2, SaiyanHud.GoldLight);
		if (_expText != Strings.EntityBox.MaxLevel.ToString())
		{
			int size2 = Pt(10f);
			string text = Strings.EntityBox.ShortExp.ToString();
			Point point2 = SaiyanHud.Measure(renderer, SaiyanHud.SmallFont, size2, text);
			SaiyanHud.Text(renderer, SaiyanHud.SmallFont, size2, text, num11 - point.X - L(5f) - point2.X, num10 - point2.Y / 2, SaiyanHud.Gold, shadow: false);
		}
	}

	private void DrawCrest(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Moment moment)
	{
		float num = F(62f);
		float num2 = F(66f);
		float num3 = F(56f);
		float num4 = num2 - num3 * 0.8660254f;
		SaiyanHud.Accent frame = _frame;
		Color color = _aura?.Main ?? frame.Main;
		if (_auraIsUltra)
		{
			color = UltraViolet;
		}
		Color color2 = (moment.LowHealth ? SaiyanHud.Lerp(frame.Main, SaiyanHud.Red, 0.35f + 0.65f * moment.LowPulse) : frame.Main);
		Color color3 = (moment.LowHealth ? SaiyanHud.Lerp(frame.Light, new Color(255, 255, 140, 130), 0.5f + 0.5f * moment.LowPulse) : frame.Light);
		float num5 = (_aura.HasValue ? (0.08f * MathF.Sin(Phase(moment.Now, 2640L) * ((float)Math.PI * 2f))) : 0f);
		if (!_isPlayerBox || _aura.HasValue)
		{
			HudShapes.Glow(renderer, num, num2, num3 * 1.6f, num3 * 1.6f, SaiyanHud.WithAlpha(color, 0.38f + num5));
		}
		if (moment.LowHealth)
		{
			HudShapes.Glow(renderer, num, num2, num3 * 1.55f, num3 * 1.55f, SaiyanHud.WithAlpha(SaiyanHud.Red, 0.6f * moment.LowPulse));
		}
		Color color4 = SaiyanHud.WithAlpha(color3, 0.6f);
		int thickness = Math.Max(1, L(1.5f));
		for (int i = 0; i < ((!_isPlayerBox) ? 4 : 0); i++)
		{
			var (num6, num7) = HudShapes.HexPoint(num, num2, num3 + F(7f), (float)i / 6f);
			var (x, y) = HudShapes.HexPoint(num, num2, num3 + F(7f), (float)i / 6f - 0.04f);
			var (x2, y2) = HudShapes.HexPoint(num, num2, num3 + F(7f), (float)i / 6f + 0.04f);
			HudShapes.Line(renderer, x, y, num6, num7, thickness, color4);
			HudShapes.Line(renderer, num6, num7, x2, y2, thickness, color4);
		}
		HudShapes.Hexagon(renderer, num, num2 + F(5f), num3 + F(2f), HudShapes.Ramp.Solid(Black), 0.45f, additive: false, 2);
		HudShapes.Hexagon(renderer, num, num2, num3, new HudShapes.Ramp(new Color(255, 7, 9, 16), SaiyanHud.Lerp(color, SaiyanHud.Night, 0.6f), SaiyanHud.Lerp(color, SaiyanHud.Night, 0.5f), 0.7f), 1f, additive: false, 2);
		float num8 = num3 * 0.8660254f;
		Rectangle previous = TechniqueArt.BeginClip(renderer, new Rectangle((int)(num - num3), (int)num4, (int)(num3 * 2f), (int)(num8 * 2f)));
		HudShapes.Glow(renderer, num, num2 + num3 * 0.3f, num3 * 0.8f, num3 * 0.8f, SaiyanHud.WithAlpha(color, 0.22f));
		TechniqueArt.EndClip(renderer, previous);
		SaiyanHud.Accent? aura = _aura;
		if (aura.HasValue)
		{
			SaiyanHud.Accent valueOrDefault = aura.GetValueOrDefault();
			HudShapes.Flames(renderer, num, num2, num3 - F(2f), valueOrDefault.Light, _auraIsUltra ? UltraViolet : null, moment.Now, _z);
		}
		int num9 = Math.Max(2, (int)MathF.Round((_isPlayerBox ? 4.5f : 4f) * _z));
		int bodyTop = (int)MathF.Round(num4 - F(8f) - (float)num9);
		foreach (PortraitLayer layer in _layers)
		{
			DrawPortraitLayer(renderer, layer, num, num2, num3, num9, bodyTop, popOut: false);
		}
		Color color5 = new Color(255, 7, 9, 16);
		for (int j = (int)(num2 + num3 * 0.35f); (float)j < num2 + num8; j += 2)
		{
			float distance = MathF.Abs((float)j + 1f - num2);
			float num10 = HudShapes.HexHalf(num3, distance) - 1f;
			float value = ((float)j + 0.5f - (num2 + num3 * 0.35f)) / (num8 - num3 * 0.35f);
			HudShapes.Span(renderer, num - num10, num + num10, j, 2, SaiyanHud.WithAlpha(color5, 0.9f * Math.Clamp(value, 0f, 1f)));
		}
		Color color6 = SaiyanHud.WithAlpha(SaiyanHud.White, 0.06f);
		for (int k = (int)MathF.Ceiling(num4); (float)k < num2 + num3 * 0.05f; k += 2)
		{
			float num11 = (float)k + 1f;
			float num12 = HudShapes.HexHalf(num3, MathF.Abs(num11 - num2)) - 1f;
			float right = Math.Min(num + num12, num + num3 * 0.25f - (num11 - num4) * 0.7f);
			HudShapes.Span(renderer, num - num12, right, k, 2, color6);
		}
		HudShapes.HexagonRing(renderer, num, num2, num3 + F(1.5f), 1f, HudShapes.Ramp.Solid(Black), 0.7f);
		HudShapes.HexagonRing(renderer, num, num2, num3 - F(1f), Math.Max(2f, F(4f)), new HudShapes.Ramp(color3, color2, SaiyanHud.Lerp(color2, SaiyanHud.Night, 0.35f)));
		if (moment.LowHealth)
		{
			HudShapes.Glow(renderer, num, num2, num3 * 1.15f, num3 * 1.15f, SaiyanHud.WithAlpha(SaiyanHud.Red, 0.45f * moment.LowPulse));
		}
		foreach (PortraitLayer layer2 in _layers)
		{
			DrawPortraitLayer(renderer, layer2, num, num2, num3, num9, bodyTop, popOut: true);
		}
		if (moment.Hit > 0f)
		{
			HudShapes.Hexagon(renderer, num, num2, num3 - F(2f), HudShapes.Ramp.Solid(new Color(255, 255, 90, 80)), 0.38f * moment.Hit);
		}
		if (moment.FormChange > 0f)
		{
			HudShapes.Hexagon(renderer, num, num2, num3 - F(2f), HudShapes.Ramp.Solid(SaiyanHud.White), 0.8f * moment.FormChange * moment.FormChange);
		}
		float num13 = (float)(moment.Now % 7000) / 1500f;
		if (num13 < 1f)
		{
			for (int l = 0; l < 16; l++)
			{
				float num14 = num13 - (float)l * 0.011f;
				if (!(num14 < 0f))
				{
					(float X, float Y) tuple4 = HudShapes.HexPoint(num, num2, num3 - F(1f), 0.05f + num14 * 0.9f);
					float item = tuple4.X;
					float item2 = tuple4.Y;
					float num15 = (1f - (float)l / 16f) * MathF.Sin((float)Math.PI * num13);
					float num16 = F(4.5f) * (1f - (float)l / 18f);
					HudShapes.Glow(renderer, item, item2, num16, num16, SaiyanHud.WithAlpha(SaiyanHud.White, 0.95f * num15));
				}
			}
		}
		if (moment.FormChange > 0f)
		{
			float num17 = 1f - moment.FormChange;
			HudShapes.HexagonRing(renderer, num, num2, num3 + num17 * F(44f), F(3f * moment.FormChange + 1f), HudShapes.Ramp.Solid(_aura?.Light ?? frame.Light), moment.FormChange);
		}
	}

	private void DrawPortraitLayer(Intersect.Client.Framework.Gwen.Renderer.Base renderer, PortraitLayer layer, float cx, float cy, float radius, int bodyScale, int bodyTop, bool popOut)
	{
		if (!(renderer is IntersectRenderer intersectRenderer))
		{
			return;
		}
		IGameTexture texture = layer.Texture;
		Rectangle frame = layer.Frame;
		float num = radius * 0.8660254f;
		float num2 = cy - num;
		int num3;
		int num4;
		int num5;
		int num6;
		if (layer.IsFace)
		{
			if (popOut)
			{
				return;
			}
			num3 = Math.Max(1, (int)MathF.Floor(Math.Max(radius * 1.886f / (float)frame.Width, radius * 1.657f / (float)frame.Height)));
			num4 = frame.Y;
			num5 = frame.Height;
			num6 = (int)MathF.Round(cy - (float)(frame.Height * num3) / 2f);
		}
		else
		{
			num3 = bodyScale;
			IGameTexture anchor = layer.Anchor;
			int num7 = ((anchor != null) ? SaiyanHud.TopMargin(anchor, layer.AnchorFrame) : SaiyanHud.TopMargin(texture, frame));
			num4 = frame.Y + Math.Max(0, num7 - 1);
			int val = frame.Y + frame.Height - num4;
			num5 = Math.Clamp((int)MathF.Ceiling((cy + num - (float)bodyTop) / (float)num3), 1, Math.Max(1, val));
			num6 = bodyTop;
		}
		int num8 = (int)MathF.Round(cx - (float)(frame.Width * num3) / 2f);
		int num9 = num5 * num3;
		if (num9 < 1)
		{
			return;
		}
		float num10 = Math.Max(1.5f, F(1.5f));
		int num11 = frame.Width * num3;
		float num12 = cy - F(2f);
		intersectRenderer.DrawColor = layer.Tint;
		for (int i = 0; i < num9; i++)
		{
			int num13 = num6 + i;
			float num14 = (float)num13 + 0.5f;
			float num15;
			if (popOut)
			{
				if (num14 >= num12)
				{
					break;
				}
				num15 = num11;
			}
			else
			{
				float num16 = MathF.Abs(num14 - cy);
				if (num16 > num - num10)
				{
					continue;
				}
				num15 = HudShapes.HexHalf(radius, num16) - num10;
			}
			if (!(num15 <= 0f))
			{
				int num17 = Math.Max(0, (int)MathF.Ceiling((cx - num15 - (float)num8) / (float)num3));
				int num18 = Math.Min(frame.Width, (int)MathF.Floor((cx + num15 - (float)num8) / (float)num3));
				if (num18 > num17)
				{
					int num19 = num18 - num17;
					int num20 = num4 + i / num3;
					intersectRenderer.DrawTextureBand(texture, (float)(frame.X + num17) + 0.5f, (float)num20 + 0.5f, (float)num19 + 0.49f, 1.49f, num8 + num17 * num3, num13, ((float)num19 + 0.49f) * (float)num3, 1.49f);
				}
			}
		}
	}

	private void DrawLevelBadge(Intersect.Client.Framework.Gwen.Renderer.Base renderer, float levelUp)
	{
		float num = F(102f);
		float num2 = F(108f);
		float num3 = ((levelUp > 0f) ? (1f - levelUp) : 1f);
		float num4 = ((levelUp > 0f) ? (1f + 0.4f * MathF.Sin((float)Math.PI * Math.Min(1f, num3 * 1.8f))) : 1f);
		float num5 = F(18f) * num4;
		if (levelUp > 0f)
		{
			HudShapes.HexagonRing(renderer, num, num2, F(18f) + num3 * F(36f), Math.Max(2f, F(2.5f)), HudShapes.Ramp.Solid(new Color(255, 255, 220, 140)), levelUp);
		}
		HudShapes.Hexagon(renderer, num, num2 + F(3f), num5, HudShapes.Ramp.Solid(Black), 0.6f);
		HudShapes.Hexagon(renderer, num, num2, num5, HudShapes.Ramp.Two((levelUp > 0f) ? new Color(255, 255, 246, 214) : _frame.Light, _frame.Main));
		HudShapes.HexagonRing(renderer, num, num2, num5, Math.Max(2f, F(2.5f)), HudShapes.Ramp.Solid(SaiyanHud.Night), 0.95f);
		HudShapes.HexagonRing(renderer, num, num2, num5 + F(2.5f), 1f, HudShapes.Ramp.Solid(_frame.Light), 0.5f);
		int size = Pt(13f * num4);
		Point point = SaiyanHud.Measure(renderer, SaiyanHud.StrongFont, size, _levelText);
		SaiyanHud.Text(renderer, SaiyanHud.StrongFont, size, _levelText, (int)MathF.Round(num - (float)point.X / 2f), (int)MathF.Round(num2 - (float)point.Y / 2f), SaiyanHud.Night, shadow: false);
	}

	public Point LieuDesEffets(int hauteur)
	{
		Point coinDuSocle = CoinDuSocle;
		return new Point(coinDuSocle.X, coinDuSocle.Y - 4 - hauteur);
	}

	private void PlaceSocle(Intersect.Client.Framework.Gwen.Control.Base canvas)
	{
		_tailleSocle = Disposition.Taille(Disposition.Piece.VieEtKi);
		int num = (int)MathF.Round(562f * _tailleSocle);
		int num2 = (int)MathF.Round(86f * _tailleSocle);
		int val;
		int val2;
		if (Interface.HasInGameUI)
		{
			HotBarWindow hotbar = Interface.GameUi.Hotbar;
			if (hotbar != null && !hotbar.IsHidden && hotbar.Width > 0)
			{
				val = hotbar.X + (hotbar.Width - num) / 2;
				val2 = hotbar.Y - num2;
				goto IL_0093;
			}
		}
		val = (canvas.Width - num) / 2;
		val2 = canvas.Height - 80 - num2;
		goto IL_0093;
		IL_0093:
		Point point = Disposition.Poser(Disposition.Piece.VieEtKi, num, num2, Math.Max(0, val), Math.Max(0, val2), canvas);
		PlaceContent(2f / 3f * _tailleSocle, point.X, point.Y);
	}

	private void LayoutSocle()
	{
		int x = _origin.X;
		int y = _origin.Y;
		int num = L(48f);
		int num2 = S(562f);
		int num3 = S(86f);
		EntityWindow.SetSize(x + num2 + num, y + num3 + num);
		_panel.SetBounds(0, 0, x + num2 + num, y + num3 + num);
		_hpHover.SetBounds(x + S(19f), y + S(53f), S(239f), S(22f));
		_mpHover.SetBounds(x + S(334f), y + S(55f), S(210f), S(18f));
		_expHover?.SetBounds(x + S(20f), y + S(77f), S(522f), S(9f));
		int S(float cote)
		{
			return (int)MathF.Round(cote * _tailleSocle);
		}
	}

	private void DrawSocle(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Moment moment)
	{
		long milliseconds = Timing.Global.Milliseconds;
		SocleVieEtKi.Etat etat = new SocleVieEtKi.Etat
		{
			Echelle = _tailleSocle,
			Accent = _frame.Main,
			AccentClair = _frame.Light,
			Vie = Jauge(_hp, moment.Now),
			Ki = Jauge(_mp, moment.Now),
			Experience = Jauge(_exp, moment.Now),
			Bouclier = _shieldRatio,
			TexteVie = ((_hpHover.IsHovered && _hpValeur != null) ? _hpValeur : _hpText),
			TexteKi = ((_mpHover.IsHovered && _mpValeur != null) ? _mpValeur : _mpText)
		};
		Intersect.Client.Framework.Gwen.Control.Base expHover = _expHover;
		etat.TexteExperience = ((expHover != null && expHover.IsHovered && _expValeur != null) ? _expValeur : _expText);
		etat.LibelleVie = Strings.EntityBox.ShortVital0.ToString();
		etat.LibelleKi = Strings.EntityBox.ShortVital1.ToString();
		etat.LibelleExperience = ((_expText == Strings.EntityBox.MaxLevel.ToString()) ? string.Empty : Strings.EntityBox.ShortExp.ToString());
		etat.Dash = LireLeDash(milliseconds);
		etat.Esquive = LireLEsquive(milliseconds);
		etat.VieBasse = moment.LowHealth;
		etat.Battement = moment.LowPulse;
		etat.Coup = moment.Hit;
		etat.NiveauGagne = moment.LevelUp;
		etat.EclatExperience = Flash(moment.Now, _expShineAt, 900L);
		etat.Maintenant = moment.Now;
		SocleVieEtKi.Etat etat2 = etat;
		SocleVieEtKi.Dessiner(renderer, in etat2);
		Point renderOffset = renderer.RenderOffset;
		renderer.RenderOffset = new Point(renderOffset.X + (int)MathF.Round(281f * _tailleSocle - F(62f)), renderOffset.Y + (int)MathF.Round(39.2f * _tailleSocle - F(66f)));
		try
		{
			DrawCrest(renderer, moment);
			DrawLevelBadge(renderer, moment.LevelUp);
		}
		finally
		{
			renderer.RenderOffset = renderOffset;
		}
	}

	private static SocleVieEtKi.Jauge Jauge(GaugeMotion mouvement, long now)
	{
		return new SocleVieEtKi.Jauge
		{
			Montre = mouvement.Shown,
			Ombre = mouvement.Ghost,
			Cible = mouvement.Target,
			Tete = mouvement.Head,
			Tient = mouvement.IsHolding(now)
		};
	}

	private SocleVieEtKi.Charges LireLEsquive(long now)
	{
		if (MyEntity != Globals.Me || !Vanish.Jetons(now, out var jetons, out var maximum, out var recharge))
		{
			return default(SocleVieEtKi.Charges);
		}
		if (_jetonsVus >= 0 && jetons > _jetonsVus)
		{
			_jetonRevenuA = now;
		}
		else if (_jetonsVus >= 0 && jetons < _jetonsVus)
		{
			_jetonPrisA = now;
		}
		_jetonsVus = jetons;
		if (now - _toucheLueA > 1000 || string.IsNullOrEmpty(_toucheEsquive))
		{
			_toucheLueA = now;
			_toucheEsquive = NomDeLaToucheDEsquive();
		}
		return new SocleVieEtKi.Charges
		{
			Visibles = true,
			Pleines = jetons,
			Maximum = maximum,
			Recharge = recharge,
			Touche = _toucheEsquive,
			Prise = Eclat(now, _jetonPrisA, 250L),
			Revenue = Eclat(now, _jetonRevenuA, 350L),
			Nouvelle = Pulse(now, Vanish.NouvelleChargeA),
			Enfoncee = (now - _jetonPrisA < 120),
			Refusee = (now - Vanish.RefuseeA < 400)
		};
	}

	private SocleVieEtKi.Charges LireLeDash(long now)
	{
		if (MyEntity == Globals.Me)
		{
			Player me = Globals.Me;
			if (me != null && me.ChargesDuDash(now, out var charges, out var maximum, out var recharge))
			{
				if (_chargesDashVues >= 0 && charges > _chargesDashVues)
				{
					_chargeDashRevenueA = now;
				}
				else if (_chargesDashVues >= 0 && charges < _chargesDashVues)
				{
					_chargeDashPriseA = now;
				}
				_chargesDashVues = charges;
				LireLaToucheDash(now);
				return new SocleVieEtKi.Charges
				{
					Visibles = true,
					Pleines = charges,
					Maximum = maximum,
					Recharge = recharge,
					Espace = _toucheDashEspace,
					Touche = _toucheDash,
					Prise = Eclat(now, _chargeDashPriseA, 250L),
					Revenue = Eclat(now, _chargeDashRevenueA, 350L),
					Nouvelle = Pulse(now, me.NouvelleChargeDeDashA),
					Enfoncee = (now - _chargeDashPriseA < 120),
					Refusee = (now - me.DashRefuseA < 400)
				};
			}
		}
		return default(SocleVieEtKi.Charges);
	}

	private static float Eclat(long now, long depuis, long duree)
	{
		if (depuis > 0 && now >= depuis && now - depuis < duree)
		{
			return 1f - (float)(now - depuis) / (float)duree;
		}
		return 0f;
	}

	private static float Pulse(long now, long depuis)
	{
		if (depuis <= 0 || now < depuis || now - depuis >= 1500)
		{
			return 0f;
		}
		float num = (float)(now - depuis) / 1500f;
		return (1f - num) * (0.6f + 0.4f * MathF.Sin(num * ((float)Math.PI * 2f) * 3f));
	}
}
