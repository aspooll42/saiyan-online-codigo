using System;
using System.Collections.Generic;
using System.Linq;
using Intersect.Client.Entities;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.Framework.Gwen.Renderer;
using Intersect.Client.Framework.Gwen.Skin;
using Intersect.Client.General;
using Intersect.Client.Interface.Game.Enseignements;
using Intersect.Client.Interface.Game.EntityPanel;
using Intersect.Client.Localization;
using Intersect.Config;
using Intersect.Framework.Core;
using Intersect.Framework.Core.GameObjects.Items;
using Intersect.Framework.Core.GameObjects.NPCs;
using Intersect.Framework.Core.GameObjects.Quests;
using Intersect.GameObjects;
using Intersect.Localization;
using Intersect.Models;
using Intersect.Network.Packets.Server;
using Intersect.Utilities;

namespace Intersect.Client.Interface.Game;

public sealed class QuestTrackerPanel : Intersect.Client.Framework.Gwen.Control.Base
{
	private sealed class Card
	{
		public required Guid QuestId;

		public required ImagePanel Hit;

		public ImagePanel? Follow;

		public string FollowTip = string.Empty;

		public readonly GaugeMotion Gauge = new GaugeMotion();

		public Guid TaskId;

		public int Progress;

		public long FlashAt;

		public int Gain;

		public int Y;

		public int Height;

		public bool Story;
	}

	private sealed class ContractCard
	{
		public required ImagePanel Hit;

		public required WantedContractEntryPacket Entry;

		public int Y;
	}

	private const int PanelWidth = 268;

	private const int HeaderHeight = 24;

	private const int FirstCardY = 30;

	private const int TallCard = 66;

	private const int ShortCard = 50;

	private const int CardShadow = 3;

	private const int CardGap = 6;

	private const int StoryShift = 12;

	private const int StoryTall = 78;

	private const int StoryShort = 62;

	private const int BarX = 26;

	private const int BarOffsetY = 50;

	private const int BarMaxWidth = 228;

	private const int BarSlant = 5;

	private const int FollowX = 244;

	private const int FollowY = 7;

	private const int FollowHit = 20;

	private const long FlashMs = 900L;

	private const long FadeMs = 400L;

	private static readonly Color White = new Color(255, 242, 242, 242);

	private static readonly Color Muted = new Color(255, 176, 184, 200);

	private static readonly Color Gold = new Color(255, 245, 190, 90);

	private static readonly Color GoldLight = new Color(255, 255, 226, 150);

	private static readonly Color GreenLight = new Color(255, 140, 235, 170);

	private static readonly Color OrangeLight = new Color(255, 255, 172, 82);

	private Card? _storyCard;

	private readonly Dictionary<(WantedContractKind Kind, Guid TargetId), ContractCard> _contracts = new Dictionary<(WantedContractKind, Guid), ContractCard>();

	private ImagePanel? _ribbonHit;

	private int _ribbonY;

	private static readonly Color SmugglingAccent = new Color(255, 255, 180, 92);

	private static readonly Color SmugglingLight = new Color(255, 255, 226, 192);

	private static readonly Color SabotageAccent = new Color(255, 255, 122, 70);

	private static readonly Color Salmon = new Color(255, 255, 154, 134);

	private static readonly Color CriminalRed = new Color(255, 240, 80, 60);

	private static readonly Color PoliceBlue = new Color(255, 108, 184, 255);

	private static readonly Color Dim = new Color(255, 138, 150, 176);

	private readonly Intersect.Client.Framework.Gwen.Control.Base? _anchor;

	private readonly Intersect.Client.Framework.Gwen.Control.Base[] _obstacles;

	private float _top = -1f;

	private readonly ImagePanel _header;

	private readonly Dictionary<Guid, Card> _cards = new Dictionary<Guid, Card>();

	private readonly List<Card> _shown = new List<Card>();

	private long _lastUpdate;

	private Guid _lastFollowed;

	private bool _followedKnown;

	protected override bool ShouldClip => false;

	public QuestTrackerPanel(Canvas canvas, Intersect.Client.Framework.Gwen.Control.Base? anchor, params Intersect.Client.Framework.Gwen.Control.Base[] obstacles)
		: base(canvas, "QuestTrackerPanel")
	{
		_anchor = anchor;
		_obstacles = obstacles;
		base.MouseInputEnabled = false;
		_header = new ImagePanel(this, "Header");
		_header.SetBounds(0, 0, 268, 24);
		_header.Clicked += delegate
		{
			TrackedQuests.ToggleCollapsed();
		};
		SetSize(268, 24);
		base.IsHidden = true;
	}

	public void Update()
	{
		Player me = Globals.Me;
		long millisecondsUtc = Timing.Global.MillisecondsUtc;
		float num = ((_lastUpdate == 0L) ? 0f : Math.Clamp((float)(millisecondsUtc - _lastUpdate) / 1000f, 0f, 0.25f));
		_lastUpdate = millisecondsUtc;
		TrackedQuests.Prune(millisecondsUtc);
		_shown.Clear();
		int num2 = 30;
		Guid storyQuestId = Globals.StoryQuestId;
		if (me != null && storyQuestId != Guid.Empty && TrackedQuests.StoryShown && DatabaseObject<QuestDescriptor>.TryGet(storyQuestId, out var databaseObject))
		{
			num2 = UpdateStoryCard(me, databaseObject, num2, num, millisecondsUtc);
		}
		else if (_storyCard != null)
		{
			RemoveChild(_storyCard.Hit, dispose: true);
			_storyCard = null;
		}
		if (me != null)
		{
			foreach (Guid questId in TrackedQuests.Pinned)
			{
				if (questId == storyQuestId || !DatabaseObject<QuestDescriptor>.TryGet(questId, out var databaseObject2))
				{
					continue;
				}
				QuestProgress progress;
				bool flag = me.QuestProgress.TryGetValue(questId, out progress) && progress.TaskId != Guid.Empty;
				long finishedAt;
				bool flag2 = !flag && TrackedQuests.TryGetFinishedAt(questId, out finishedAt);
				if (!flag && !flag2)
				{
					continue;
				}
				if (!_cards.TryGetValue(questId, out Card value))
				{
					ImagePanel imagePanel = new ImagePanel(this, $"Quest_{questId}");
					imagePanel.Clicked += delegate
					{
						Interface.GameUi?.GameMenu?.OpenQuest(questId);
					};
					value = new Card
					{
						QuestId = questId,
						Hit = imagePanel,
						Follow = FollowButton(imagePanel, delegate
						{
							TrackedQuests.Follow(questId);
						})
					};
					_cards[questId] = value;
				}
				QuestTaskDescriptor questTaskDescriptor = (flag2 ? null : databaseObject2.Tasks.FirstOrDefault((QuestTaskDescriptor t) => t.Id == progress.TaskId));
				if (flag2)
				{
					value.Gauge.Update(1f, num, millisecondsUtc);
					value.Height = 66;
				}
				else
				{
					bool flag3 = HasBar(questTaskDescriptor);
					float ratio = (flag3 ? ((float)progress.TaskProgress / (float)questTaskDescriptor.Quantity) : 0f);
					if (value.TaskId != progress.TaskId)
					{
						if (value.TaskId != Guid.Empty)
						{
							value.FlashAt = millisecondsUtc;
							value.Gain = 0;
						}
						value.TaskId = progress.TaskId;
						value.Progress = progress.TaskProgress;
						value.Gauge.Snap(ratio);
					}
					else if (value.Progress != progress.TaskProgress)
					{
						int num3 = progress.TaskProgress - value.Progress;
						if (num3 > 0)
						{
							value.Gain = ((millisecondsUtc - value.FlashAt < 900) ? (value.Gain + num3) : num3);
							value.FlashAt = millisecondsUtc;
						}
						value.Progress = progress.TaskProgress;
					}
					value.Gauge.Update(ratio, num, millisecondsUtc);
					value.Height = (flag3 ? 66 : 50);
				}
				value.Y = num2;
				num2 += value.Height + 6;
				_shown.Add(value);
			}
		}
		foreach (Card item in _cards.Values.Where((Card item) => !_shown.Contains(item)).ToList())
		{
			RemoveChild(item.Hit, dispose: true);
			_cards.Remove(item.QuestId);
		}
		num2 = UpdateContracts((me != null) ? Globals.WantedContracts : Array.Empty<WantedContractEntryPacket>(), num2);
		num2 = UpdateRibbon((me != null) ? Globals.RibbonContract : null, num2);
		Guid followed = TrackedQuests.Followed;
		if (_followedKnown && followed != _lastFollowed)
		{
			Card card = ((followed == Guid.Empty) ? _storyCard : _cards.GetValueOrDefault(followed));
			if (card != null)
			{
				card.FlashAt = millisecondsUtc;
				card.Gain = 0;
			}
		}
		_lastFollowed = followed;
		_followedKnown = me != null;
		bool collapsed = TrackedQuests.Collapsed;
		foreach (Card item2 in _shown)
		{
			item2.Hit.IsHidden = collapsed;
			item2.Hit.SetBounds(0, item2.Y, 268, item2.Height);
			UpdateFollow(item2);
		}
		foreach (ContractCard value2 in _contracts.Values)
		{
			value2.Hit.IsHidden = collapsed;
			value2.Hit.SetBounds(0, value2.Y, 268, 66);
		}
		if (_ribbonHit != null)
		{
			_ribbonHit.IsHidden = collapsed;
			_ribbonHit.SetBounds(0, _ribbonY, 268, 66);
		}
		bool flag4 = _shown.Count == 0 && _contracts.Count == 0 && _ribbonHit == null;
		base.IsHidden = flag4 || MasterTrial.InTrial || Disposition.Masquee(Disposition.Piece.Suivi);
		int num4 = ((collapsed || flag4) ? 24 : (num2 - 6 + 3));
		int num5 = ((_anchor != null) ? (_anchor.Bounds.Right - 268) : (base.Parent.Width - 268 - 8));
		int num6 = ((_anchor != null) ? (_anchor.Bounds.Bottom + 12) : 60);
		Intersect.Client.Framework.Gwen.Control.Base[] obstacles = _obstacles;
		foreach (Intersect.Client.Framework.Gwen.Control.Base obj in obstacles)
		{
			Rectangle bounds = obj.Bounds;
			if (!obj.IsHidden && bounds.X < num5 + 268 && num5 < bounds.Right)
			{
				num6 = Math.Max(num6, bounds.Bottom + 12);
			}
		}
		_top = ((_top < 0f || base.IsHidden) ? ((float)num6) : (_top + ((float)num6 - _top) * (1f - MathF.Exp((0f - num) * 14f))));
		if (Math.Abs((float)num6 - _top) < 0.5f)
		{
			_top = num6;
		}
		Point lieu = Disposition.Poser(Disposition.Piece.Suivi, 268, num4, num5, (int)MathF.Round(_top), base.Parent, !base.IsHidden);
		lieu = SousLaMiniCarte(lieu, num4);
		SetBounds(lieu.X, lieu.Y, 268, num4);
	}

	private Point SousLaMiniCarte(Point lieu, int height)
	{
		Intersect.Client.Framework.Gwen.Control.Base anchor = _anchor;
		if (anchor != null && !anchor.IsHidden)
		{
			Disposition.Reglage reglage = Disposition.De(Disposition.Piece.Suivi);
			if (reglage != null && reglage.Deplacee)
			{
				Rectangle bounds = anchor.Bounds;
				if (lieu.Y < bounds.Y + bounds.Height / 2 || !new Rectangle(lieu.X, lieu.Y, 268, height).IntersectsWith(bounds))
				{
					return lieu;
				}
				Point result = new Point(lieu.X, Math.Min(bounds.Bottom + 12, Math.Max(0, base.Parent.Height - height)));
				Disposition.VueDe(Disposition.Piece.Suivi).Bornes = new Rectangle(result.X, result.Y, 268, height);
				return result;
			}
		}
		return lieu;
	}

	private int UpdateStoryCard(Player me, QuestDescriptor quest, int y, float dt, long now)
	{
		if (_storyCard == null)
		{
			ImagePanel imagePanel = new ImagePanel(this, "StoryQuest");
			imagePanel.Clicked += delegate
			{
				Card storyCard2 = _storyCard;
				if (storyCard2 != null)
				{
					Player? me2 = Globals.Me;
					if (me2 != null && me2.QuestProgress.TryGetValue(storyCard2.QuestId, out QuestProgress value) && value.TaskId != Guid.Empty)
					{
						Interface.GameUi?.GameMenu?.OpenQuest(storyCard2.QuestId);
					}
				}
			};
			_storyCard = new Card
			{
				QuestId = quest.Id,
				Hit = imagePanel,
				Story = true,
				Follow = FollowButton(imagePanel, TrackedQuests.FollowStory)
			};
		}
		Card storyCard = _storyCard;
		if (storyCard.QuestId != quest.Id)
		{
			storyCard.QuestId = quest.Id;
			storyCard.TaskId = Guid.Empty;
			storyCard.Progress = 0;
			storyCard.FlashAt = 0L;
			storyCard.Gain = 0;
			storyCard.Gauge.Snap(0f);
		}
		if (me.QuestProgress.TryGetValue(quest.Id, out QuestProgress progress) && progress.TaskId != Guid.Empty)
		{
			QuestTaskDescriptor questTaskDescriptor = quest.Tasks.FirstOrDefault((QuestTaskDescriptor t) => t.Id == progress.TaskId);
			bool flag = HasBar(questTaskDescriptor);
			float ratio = (flag ? ((float)progress.TaskProgress / (float)questTaskDescriptor.Quantity) : 0f);
			if (storyCard.TaskId != progress.TaskId)
			{
				if (storyCard.TaskId != Guid.Empty)
				{
					storyCard.FlashAt = now;
					storyCard.Gain = 0;
				}
				storyCard.TaskId = progress.TaskId;
				storyCard.Progress = progress.TaskProgress;
				storyCard.Gauge.Snap(ratio);
			}
			else if (storyCard.Progress != progress.TaskProgress)
			{
				int num = progress.TaskProgress - storyCard.Progress;
				if (num > 0)
				{
					storyCard.Gain = ((now - storyCard.FlashAt < 900) ? (storyCard.Gain + num) : num);
					storyCard.FlashAt = now;
				}
				storyCard.Progress = progress.TaskProgress;
			}
			storyCard.Gauge.Update(ratio, dt, now);
			storyCard.Height = (flag ? 78 : 62);
		}
		else
		{
			storyCard.TaskId = Guid.Empty;
			storyCard.Height = (string.IsNullOrEmpty(Globals.StoryGiver) ? 62 : 78);
		}
		storyCard.Y = y;
		_shown.Add(storyCard);
		return y + storyCard.Height + 6;
	}

	private int UpdateContracts(WantedContractEntryPacket[] entries, int y)
	{
		HashSet<(WantedContractKind, Guid)> seen = new HashSet<(WantedContractKind, Guid)>();
		foreach (WantedContractEntryPacket wantedContractEntryPacket in entries)
		{
			(WantedContractKind, Guid) tuple = (wantedContractEntryPacket.Kind, wantedContractEntryPacket.TargetId);
			if (!seen.Add(tuple))
			{
				continue;
			}
			if (!_contracts.TryGetValue(tuple, out ContractCard value))
			{
				ImagePanel imagePanel = new ImagePanel(this, $"Contract_{wantedContractEntryPacket.TargetId}");
				string cle = $"c:{wantedContractEntryPacket.Kind}:{wantedContractEntryPacket.TargetId}";
				imagePanel.Clicked += delegate
				{
					Interface.GameUi?.GameMenu?.OpenContract(cle);
				};
				value = new ContractCard
				{
					Hit = imagePanel,
					Entry = wantedContractEntryPacket
				};
				_contracts[tuple] = value;
			}
			value.Entry = wantedContractEntryPacket;
			value.Y = y;
			y += 72;
		}
		foreach (var item in _contracts.Keys.Where(((WantedContractKind Kind, Guid TargetId) key) => !seen.Contains(key)).ToList())
		{
			RemoveChild(_contracts[item].Hit, dispose: true);
			_contracts.Remove(item);
		}
		return y;
	}

	private int UpdateRibbon(RibbonContractEntryPacket? entry, int y)
	{
		if (entry == null)
		{
			if (_ribbonHit != null)
			{
				RemoveChild(_ribbonHit, dispose: true);
				_ribbonHit = null;
			}
			return y;
		}
		if (_ribbonHit == null)
		{
			_ribbonHit = new ImagePanel(this, "RibbonContract");
			_ribbonHit.Clicked += delegate
			{
				RibbonContractEntryPacket ribbonContract = Globals.RibbonContract;
				if (ribbonContract != null)
				{
					Interface.GameUi?.GameMenu?.OpenContract("r:" + ribbonContract.Key);
				}
			};
		}
		_ribbonY = y;
		return y + 66 + 6;
	}

	private static void DrawRibbon(Intersect.Client.Framework.Gwen.Renderer.Base renderer, RibbonContractEntryPacket entry, int y, bool hovered, long now)
	{
		bool flag = entry.Kind == RibbonContractKind.Smuggling;
		bool flag2 = entry.Kind == RibbonContractKind.Sabotage;
		string text = (hovered ? "_survol" : string.Empty);
		SaiyanHud.Draw(renderer, SaiyanHud.Texture("saiyan_suivi_carte_haute" + text + ".png"), 0, y, SaiyanHud.White);
		SaiyanHud.Draw(renderer, SaiyanHud.Texture("panneau_blanc.png"), 0, y + 4, 2, 58, flag ? SmugglingAccent : (flag2 ? SabotageAccent : CriminalRed));
		int spacing = 0;
		string text2 = (flag ? Strings.RubanRouge.TrackerSmuggling.ToString(entry.Name) : (flag2 ? Strings.Recherche.SuiviSabotage.ToString(entry.Name) : Strings.RubanRouge.TrackerHead.ToString(entry.Name)));
		SaiyanHud.TextLeft(renderer, SaiyanHud.StrongFont, 13, SaiyanHud.Fit(renderer, SaiyanHud.StrongFont, 13, text2, 242, ref spacing), 13, y + 3, 24, flag ? SmugglingLight : Salmon);
		string text3 = WantedPlaces.ZoneOf(entry.MapId);
		object obj;
		if (text3.Length <= 0)
		{
			string text4 = entry.Place?.Trim();
			obj = ((text4 != null && text4.Length > 0) ? text4 : Strings.WantedBoard.UnknownPlace.ToString());
		}
		else
		{
			obj = text3;
		}
		string text5 = (string)obj;
		string text6;
		if (flag)
		{
			text6 = Strings.RubanRouge.TrackerDeliver.ToString(text5);
		}
		else if (flag2)
		{
			LocalizedString suiviSabotageOu = Strings.Recherche.SuiviSabotageOu;
			object[] array = new object[1];
			string text7 = entry.Place?.Trim();
			array[0] = ((text7 != null && text7.Length > 0) ? text7 : text5);
			text6 = suiviSabotageOu.ToString(array);
		}
		else
		{
			int num = Math.Max(0, (int)((now - Globals.RibbonContractAt) / 60000));
			text6 = Strings.RubanRouge.HeadSeen.ToString(text5, WantedPlaces.Elapsed(Math.Max(0, entry.Minutes) + num));
		}
		SaiyanHud.Draw(renderer, SaiyanHud.Texture("saiyan_suivi_puce.png"), 15, y + 32, Muted);
		int spacing2 = 0;
		SaiyanHud.TextLeft(renderer, SaiyanHud.SmallFont, 9, SaiyanHud.Fit(renderer, SaiyanHud.SmallFont, 9, text6, 230, ref spacing2), 26, y + 25, 20, White);
		string text8 = Strings.RubanRouge.TrackerPay.ToString(SaiyanHud.Amount(Math.Max(0L, entry.Zeni)), Math.Max(0, entry.Reputation));
		int spacing3 = 0;
		SaiyanHud.TextLeft(renderer, SaiyanHud.StrongFont, 11, SaiyanHud.Fit(renderer, SaiyanHud.StrongFont, 11, text8, 230, ref spacing3), 26, y + 41, 18, Gold);
	}

	private static void DrawContract(Intersect.Client.Framework.Gwen.Renderer.Base renderer, ContractCard card, long now)
	{
		WantedContractEntryPacket entry = card.Entry;
		int y = card.Y;
		bool flag = entry.Kind == WantedContractKind.Criminal;
		string text = (card.Hit.IsHovered ? "_survol" : string.Empty);
		SaiyanHud.Draw(renderer, SaiyanHud.Texture("saiyan_suivi_carte_haute" + text + ".png"), 0, y, SaiyanHud.White);
		if (!flag && entry.Completed)
		{
			SaiyanHud.Draw(renderer, SaiyanHud.Texture("panneau_blanc.png"), 0, y + 4, 2, 58, Gold);
			SaiyanHud.Draw(renderer, SaiyanHud.Texture("panneau_blanc.png"), 2, y + 4, 134, 58, SaiyanHud.WithAlpha(Gold, 0.1f));
			int spacing = 0;
			string text2 = SaiyanHud.Fit(renderer, SaiyanHud.StrongFont, 13, Strings.WantedBoard.TrackerCompleted.ToString(entry.Name), 242, ref spacing);
			SaiyanHud.TextLeft(renderer, SaiyanHud.StrongFont, 13, text2, 13, y + 3, 24, Gold);
			SaiyanHud.Draw(renderer, SaiyanHud.Texture("saiyan_suivi_puce.png"), 15, y + 32, GoldLight);
			SaiyanHud.TextLeft(renderer, SaiyanHud.SmallFont, 9, Strings.WantedBoard.TrackerGoBack, 26, y + 25, 20, White);
			string text3 = Strings.WantedBoard.Reward.ToString(SaiyanHud.Amount(entry.Zeni), SaiyanHud.Amount(entry.Experience));
			string text4 = Strings.WantedBoard.TrackerCompletedReward.ToString(text3, Math.Max(0, Options.Instance.Karma.ReputationPoliceBounty));
			int spacing2 = 0;
			SaiyanHud.TextLeft(renderer, SaiyanHud.StrongFont, 11, SaiyanHud.Fit(renderer, SaiyanHud.StrongFont, 11, text4, 230, ref spacing2), 26, y + 41, 18, Gold);
			return;
		}
		SaiyanHud.Draw(renderer, SaiyanHud.Texture("panneau_blanc.png"), 0, y + 4, 2, 58, flag ? CriminalRed : PoliceBlue);
		int num = Math.Max(0, (int)((now - Globals.WantedContractsAt) / 60000));
		int spacing3 = 0;
		string text5 = (flag ? Strings.WantedBoard.TrackerCriminal.ToString(entry.Name) : Strings.WantedBoard.TrackerPolice.ToString(entry.Name));
		string text6 = SaiyanHud.Fit(renderer, SaiyanHud.StrongFont, 13, text5, 242, ref spacing3);
		SaiyanHud.TextLeft(renderer, SaiyanHud.StrongFont, 13, text6, 13, y + 3, 24, flag ? Salmon : PoliceBlue);
		string text8;
		if (flag)
		{
			string text7 = WantedPlaces.ZoneOf(entry.MapId);
			text8 = ((entry.Minutes < 0 || entry.MapId == Guid.Empty) ? ((string)Strings.WantedBoard.NeverSeen) : Strings.WantedBoard.LastSeen.ToString((text7.Length > 0) ? text7 : Strings.WantedBoard.UnknownPlace.ToString(), WantedPlaces.Elapsed(entry.Minutes + num)));
		}
		else
		{
			string text9 = WantedPlaces.ZoneOf(entry.MapId);
			string text10 = entry.Place?.Trim() ?? string.Empty;
			string text11 = ((text9.Length == 0) ? text10 : ((text10.Length != 0 && !text10.Contains(text9, StringComparison.OrdinalIgnoreCase)) ? (text9 + ", " + text10) : ((text10.Length == 0) ? text9 : text10)));
			text8 = Strings.WantedBoard.Spotted.ToString((text11.Length > 0) ? text11 : Strings.WantedBoard.UnknownPlace.ToString());
		}
		SaiyanHud.Draw(renderer, SaiyanHud.Texture("saiyan_suivi_puce.png"), 15, y + 32, Muted);
		int spacing4 = 0;
		SaiyanHud.TextLeft(renderer, SaiyanHud.SmallFont, 9, SaiyanHud.Fit(renderer, SaiyanHud.SmallFont, 9, text8, 230, ref spacing4), 26, y + 25, 20, White);
		int num2 = 26;
		if (flag)
		{
			string text12 = (entry.Online ? Strings.WantedBoard.Online.ToString() : ((entry.OfflineMinutes >= 0) ? Strings.WantedBoard.OfflineSince.ToString(WantedPlaces.Elapsed(entry.OfflineMinutes + num)) : Strings.WantedBoard.Offline.ToString()));
			SaiyanHud.TextLeft(renderer, SaiyanHud.SmallFont, 8, text12, num2, y + 41, 18, entry.Online ? GreenLight : Dim);
			num2 += SaiyanHud.Measure(renderer, SaiyanHud.SmallFont, 8, text12).X + 8;
			SaiyanHud.TextLeft(renderer, SaiyanHud.StrongFont, 11, Strings.WantedBoard.TrackerBounty.ToString(SaiyanHud.Amount(entry.Zeni)), num2, y + 41, 18, Gold);
		}
		else
		{
			string text13 = Strings.WantedBoard.Reward.ToString(SaiyanHud.Amount(entry.Zeni), SaiyanHud.Amount(entry.Experience));
			SaiyanHud.TextLeft(renderer, SaiyanHud.StrongFont, 11, Strings.WantedBoard.TrackerReward.ToString(text13), num2, y + 41, 18, Gold);
		}
	}

	private static ImagePanel FollowButton(ImagePanel card, Action follow)
	{
		ImagePanel imagePanel = new ImagePanel(card, "Follow");
		imagePanel.SetSize(20, 20);
		imagePanel.Clicked += delegate
		{
			follow();
		};
		return imagePanel;
	}

	private static bool IsFollowed(Card card)
	{
		if (!card.Story)
		{
			return TrackedQuests.Followed == card.QuestId;
		}
		if (!(TrackedQuests.Followed == Guid.Empty))
		{
			return TrackedQuests.Followed == card.QuestId;
		}
		return true;
	}

	private static void UpdateFollow(Card card)
	{
		ImagePanel follow = card.Follow;
		if (follow != null)
		{
			follow.IsHidden = !card.Story && TrackedQuests.TryGetFinishedAt(card.QuestId, out var _);
			int num = (card.Story ? 12 : 0);
			follow.SetPosition(241, 15 + num - 10);
			string text = ((!IsFollowed(card)) ? Strings.QuestTracker.Follow : (card.Story ? Strings.QuestTracker.Following : Strings.QuestTracker.FollowingBack));
			if (text != card.FollowTip)
			{
				card.FollowTip = text;
				follow.SetToolTipText(text);
			}
		}
	}

	private static void DrawFollow(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Card card, int y)
	{
		ImagePanel follow = card.Follow;
		if (follow != null && !follow.IsHidden)
		{
			if (IsFollowed(card))
			{
				SaiyanHud.Draw(renderer, SaiyanHud.Texture("saiyan_suivi_losange.png"), 244, y + 7, SaiyanHud.White);
			}
			else
			{
				SaiyanHud.Draw(renderer, SaiyanHud.Texture("saiyan_suivi_losange_creux.png"), 244, y + 7, follow.IsHovered ? Gold : Muted);
			}
		}
	}

	private static bool HasBar(QuestTaskDescriptor? task)
	{
		if (task != null && task.Objective != QuestObjective.EventDriven)
		{
			return task.Quantity > 0;
		}
		return false;
	}

	internal static string ObjectiveOf(QuestTaskDescriptor? task)
	{
		if (task == null)
		{
			return string.Empty;
		}
		if (!string.IsNullOrWhiteSpace(task.Description) && !TextUtils.IsNone(task.Description))
		{
			return task.Description.Split('\n')[0].Trim();
		}
		return task.Objective switch
		{
			QuestObjective.KillNpcs => Strings.QuestTracker.KillNpcs.ToString(DatabaseObject<NPCDescriptor>.GetName(task.TargetId)), 
			QuestObjective.GatherItems => Strings.QuestTracker.GatherItems.ToString(DatabaseObject<ItemDescriptor>.GetName(task.TargetId)), 
			_ => Strings.QuestTracker.EventTask, 
		};
	}

	protected override void Render(Intersect.Client.Framework.Gwen.Skin.Base skin)
	{
		base.Render(skin);
		Intersect.Client.Framework.Gwen.Renderer.Base renderer = skin.Renderer;
		long millisecondsUtc = Timing.Global.MillisecondsUtc;
		Player me = Globals.Me;
		SaiyanHud.Draw(renderer, SaiyanHud.Texture("saiyan_suivi_entete.png"), 0, 0, SaiyanHud.White);
		string text = Strings.QuestTracker.Title.ToString();
		int num = SaiyanHud.SpacedWidth(renderer, SaiyanHud.SmallFont, 9, text, 2);
		SaiyanHud.TextSpaced(renderer, SaiyanHud.SmallFont, 9, text, 18 + num, 0, 24, 2, White);
		QuestProgress value;
		int num2 = TrackedQuests.Pinned.Count((Guid questId) => !TrackedQuests.IsStory(questId) && me != null && me.QuestProgress.TryGetValue(questId, out value) && value.TaskId != Guid.Empty);
		string text2 = Strings.QuestTracker.Count.ToString(num2, 5);
		SaiyanHud.TextRight(renderer, SaiyanHud.SmallFont, 8, text2, 240, 0, 24, Muted);
		IGameTexture texture = SaiyanHud.Texture(TrackedQuests.Collapsed ? "saiyan_suivi_chevron_bas.png" : "saiyan_suivi_chevron_haut.png");
		SaiyanHud.Draw(renderer, texture, 249, 8, _header.IsHovered ? SaiyanHud.White : Muted);
		if (TrackedQuests.Collapsed || me == null)
		{
			return;
		}
		foreach (Card item in _shown)
		{
			if (DatabaseObject<QuestDescriptor>.TryGet(item.QuestId, out var databaseObject))
			{
				if (item.Story && item.TaskId == Guid.Empty)
				{
					DrawStoryOffer(renderer, item, databaseObject);
				}
				else
				{
					DrawCard(renderer, item, databaseObject, me, millisecondsUtc);
				}
			}
		}
		foreach (ContractCard value2 in _contracts.Values)
		{
			DrawContract(renderer, value2, millisecondsUtc);
		}
		if (_ribbonHit != null)
		{
			RibbonContractEntryPacket ribbonContract = Globals.RibbonContract;
			if (ribbonContract != null)
			{
				DrawRibbon(renderer, ribbonContract, _ribbonY, _ribbonHit.IsHovered, millisecondsUtc);
			}
		}
	}

	private void DrawCard(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Card card, QuestDescriptor quest, Player me, long now)
	{
		int y = card.Y;
		long finishedAt;
		QuestProgress value;
		bool flag = TrackedQuests.TryGetFinishedAt(card.QuestId, out finishedAt) && !card.Story && (!me.QuestProgress.TryGetValue(card.QuestId, out value) || !(value.TaskId != Guid.Empty));
		float num = 1f;
		if (flag)
		{
			long num2 = 4000 - (now - finishedAt);
			num = Math.Clamp((float)num2 / 400f, 0f, 1f);
		}
		bool flag2 = card.Height == 66 || card.Height == 78;
		int num3 = (card.Story ? 12 : 0);
		string text = (flag2 ? "haute" : "basse");
		object obj;
		if (!flag)
		{
			if (!card.Hit.IsHovered)
			{
				ImagePanel? follow = card.Follow;
				if (follow == null || !follow.IsHovered)
				{
					obj = string.Empty;
					goto IL_00f7;
				}
			}
			obj = "_survol";
		}
		else
		{
			obj = "_finie";
		}
		goto IL_00f7;
		IL_00f7:
		string text2 = (string)obj;
		string name = (card.Story ? ("saiyan_suivi_histoire_" + text + text2 + ".png") : ("saiyan_suivi_carte_" + text + text2 + ".png"));
		SaiyanHud.Draw(renderer, SaiyanHud.Texture(name), 0, y, SaiyanHud.WithAlpha(SaiyanHud.White, num));
		float num4 = ((card.FlashAt > 0) ? Math.Clamp(1f - (float)(now - card.FlashAt) / 900f, 0f, 1f) : 0f);
		if (flag)
		{
			num4 = Math.Clamp(1f - (float)(now - finishedAt) / 900f, 0f, 1f);
		}
		if (num4 > 0f)
		{
			Color color = (flag ? GreenLight : GoldLight);
			string name2 = (card.Story ? ("saiyan_suivi_histoire_lueur_" + text + ".png") : ("saiyan_suivi_lueur_" + text + ".png"));
			SaiyanHud.Draw(renderer, SaiyanHud.Texture(name2), 0, y, SaiyanHud.WithAlpha(color, num4 * 0.6f * num));
		}
		QuestProgress progress = (flag ? null : me.QuestProgress.GetValueOrDefault(card.QuestId));
		int num5 = ((progress == null) ? (-1) : quest.Tasks.FindIndex((QuestTaskDescriptor t) => t.Id == progress.TaskId));
		int num6 = 0;
		int num7 = ((!flag) ? 16 : 0);
		if (card.Story)
		{
			DrawStoryHeader(renderer, quest, y);
		}
		else if (!flag && num5 >= 0 && quest.Tasks.Count > 1)
		{
			string text3 = Strings.QuestTracker.Step.ToString(num5 + 1, quest.Tasks.Count);
			num6 = SaiyanHud.TextSpaced(renderer, SaiyanHud.SmallFont, 8, text3, 256 - num7 - 2, y + 3, 24, 1, Fade(Muted, num));
		}
		int spacing = 0;
		string text4 = SaiyanHud.Fit(renderer, SaiyanHud.StrongFont, 13, quest.Name, 238 - num6 - num7, ref spacing);
		SaiyanHud.TextLeft(renderer, SaiyanHud.StrongFont, 13, text4, 13, y + 3 + num3, 24, Fade(flag ? GreenLight : (card.Story ? GoldLight : Gold), num));
		if (!flag)
		{
			DrawFollow(renderer, card, y + num3);
		}
		if (flag)
		{
			SaiyanHud.Draw(renderer, SaiyanHud.Texture("saiyan_suivi_coche.png"), 13, y + 30 + num3, Fade(GreenLight, num));
			SaiyanHud.TextLeft(renderer, SaiyanHud.SmallFont, 9, Strings.QuestTracker.Completed, 28, y + 25 + num3, 20, Fade(GreenLight, num));
		}
		else
		{
			QuestTaskDescriptor task = ((num5 >= 0) ? quest.Tasks[num5] : null);
			SaiyanHud.Draw(renderer, SaiyanHud.Texture("saiyan_suivi_puce.png"), 15, y + 32 + num3, SaiyanHud.Lerp(Muted, GoldLight, num4));
			int spacing2 = 0;
			string text5 = SaiyanHud.Fit(renderer, SaiyanHud.SmallFont, 9, ObjectiveOf(task), 230, ref spacing2);
			SaiyanHud.TextLeft(renderer, SaiyanHud.SmallFont, 9, text5, 26, y + 25 + num3, 20, White);
		}
		if (!flag2)
		{
			return;
		}
		int num8 = y + 50 + num3;
		int num9 = 256;
		int num10 = 228;
		string text6 = string.Empty;
		string text7 = string.Empty;
		int num11 = 0;
		if (!flag && progress != null && num5 >= 0)
		{
			QuestTaskDescriptor questTaskDescriptor = quest.Tasks[num5];
			text6 = SaiyanHud.Amount(Math.Clamp(progress.TaskProgress, 0, questTaskDescriptor.Quantity));
			text7 = Strings.QuestTracker.Rest.ToString(SaiyanHud.Amount(questTaskDescriptor.Quantity));
			num11 = SaiyanHud.Measure(renderer, SaiyanHud.StrongFont, 11, text7).X;
			int x = SaiyanHud.Measure(renderer, SaiyanHud.StrongFont, 11, text6).X;
			num10 = Math.Clamp(224 - num11 - x - 10, 40, 228);
		}
		Color color2 = SaiyanHud.WithAlpha(SaiyanHud.White, num);
		SaiyanHud.DrawSlanted(renderer, SaiyanHud.Texture("saiyan_suivi_piste.png"), 26, num8, num10, 5, color2);
		float num12 = Math.Max(card.Gauge.Shown, 0f);
		int num13 = ((num12 > 0f) ? Math.Max((int)((float)num10 * num12), 6) : 0);
		if (num13 > 0)
		{
			IGameTexture texture = SaiyanHud.Texture(flag ? "saiyan_suivi_jauge_finie.png" : "saiyan_suivi_jauge.png");
			SaiyanHud.DrawSlanted(renderer, texture, 26, num8, num13, 5, color2);
			if (card.Gauge.Head > 0f)
			{
				int num14 = 26 + num13;
				SaiyanHud.Draw(renderer, SaiyanHud.Texture("saiyan_suivi_traine.png"), num14 - 30, num8, SaiyanHud.WithAlpha(SaiyanHud.White, card.Gauge.Head * 0.8f * num));
				SaiyanHud.Draw(renderer, SaiyanHud.Texture("saiyan_suivi_eclat.png"), num14 - 18, num8 - 13, SaiyanHud.WithAlpha(GoldLight, card.Gauge.Head * 0.7f * num));
			}
		}
		if (text6.Length != 0)
		{
			SaiyanHud.TextRight(renderer, SaiyanHud.StrongFont, 11, text7, num9, num8 - 7, 20, Muted);
			if (num4 > 0f)
			{
				Point point = SaiyanHud.Measure(renderer, SaiyanHud.StrongFont, 11, text6);
				SaiyanHud.Draw(renderer, SaiyanHud.Texture("saiyan_suivi_eclat.png"), num9 - num11 - point.X / 2 - 16, num8 - 13, SaiyanHud.WithAlpha(Gold, num4 * 0.55f));
			}
			SaiyanHud.TextRight(renderer, SaiyanHud.StrongFont, 11, text6, num9 - num11, num8 - 7, 20, SaiyanHud.Lerp(White, GoldLight, num4));
			if (num4 > 0f && card.Gain > 0)
			{
				int num15 = (int)((1f - num4) * 8f);
				SaiyanHud.TextRight(renderer, SaiyanHud.StrongFont, 11, Strings.QuestTracker.Gain.ToString(card.Gain), num9 - num11, num8 - 24 - num15, 16, SaiyanHud.WithAlpha(GoldLight, Math.Min(1f, num4 * 1.6f)));
			}
		}
	}

	private static void DrawStoryHeader(Intersect.Client.Framework.Gwen.Renderer.Base renderer, QuestDescriptor quest, int y)
	{
		string text = Strings.Story.Tag.ToString();
		int num = SaiyanHud.SpacedWidth(renderer, SaiyanHud.NameFont, 8, text, 2);
		SaiyanHud.TextSpaced(renderer, SaiyanHud.NameFont, 8, text, 13 + num, y + 3, 16, 2, OrangeLight);
		string text2 = StoryThread.ChapterLabel(quest);
		if (text2.Length > 0)
		{
			SaiyanHud.Draw(renderer, SaiyanHud.Texture("saiyan_suivi_puce.png"), 13 + num + 5, y + 9, 5, 5, Muted);
			int num2 = SaiyanHud.SpacedWidth(renderer, SaiyanHud.SmallFont, 8, text2, 1);
			SaiyanHud.TextSpaced(renderer, SaiyanHud.SmallFont, 8, text2, 13 + num + 15 + num2, y + 3, 16, 1, Muted);
		}
		(int, int)? tuple = StoryThread.RankOf(quest);
		if (tuple.HasValue)
		{
			(int, int) valueOrDefault = tuple.GetValueOrDefault();
			SaiyanHud.TextSpaced(renderer, SaiyanHud.SmallFont, 8, Strings.QuestTracker.Step.ToString(valueOrDefault.Item1, valueOrDefault.Item2), 256, y + 3, 16, 1, Muted);
		}
	}

	private static void DrawStoryOffer(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Card card, QuestDescriptor quest)
	{
		int y = card.Y;
		bool flag = card.Height == 78;
		SaiyanHud.Draw(renderer, SaiyanHud.Texture(flag ? "saiyan_suivi_histoire_haute.png" : "saiyan_suivi_histoire_basse.png"), 0, y, SaiyanHud.White);
		DrawStoryHeader(renderer, quest, y);
		int spacing = 0;
		string text = SaiyanHud.Fit(renderer, SaiyanHud.StrongFont, 13, quest.Name, 226, ref spacing);
		SaiyanHud.TextLeft(renderer, SaiyanHud.StrongFont, 13, text, 13, y + 3 + 12, 24, GoldLight);
		DrawFollow(renderer, card, y + 12);
		string storyGiver = Globals.StoryGiver;
		int spacing2 = 0;
		if (string.IsNullOrEmpty(storyGiver))
		{
			SaiyanHud.Draw(renderer, SaiyanHud.Texture("saiyan_suivi_puce.png"), 15, y + 32 + 12, Muted);
			string text2 = SaiyanHud.Fit(renderer, SaiyanHud.SmallFont, 9, Strings.Story.Find, 230, ref spacing2);
			SaiyanHud.TextLeft(renderer, SaiyanHud.SmallFont, 9, text2, 26, y + 25 + 12, 20, White);
			return;
		}
		SaiyanHud.Draw(renderer, SaiyanHud.Texture("saiyan_suivi_marque.png"), 13, y + 28 + 12, SaiyanHud.White);
		int num = StoryThread.NiveauRequis(quest);
		Player me = Globals.Me;
		bool flag2 = me != null && num > me.Level;
		string text3 = (flag2 ? Strings.Story.LevelRequired.ToString(num, storyGiver) : Strings.Story.Talk.ToString(storyGiver));
		string text4 = SaiyanHud.Fit(renderer, SaiyanHud.SmallFont, 9, text3, 228, ref spacing2);
		SaiyanHud.TextLeft(renderer, SaiyanHud.SmallFont, 9, text4, 28, y + 25 + 12, 20, flag2 ? OrangeLight : White);
		string storyPlace = Globals.StoryPlace;
		int num2 = QuestMarkers.TilesToTarget(quest.Id);
		string text5 = ((num2 >= 0 && storyPlace.Length > 0) ? Strings.Story.PlaceDistance.ToString(storyPlace, num2) : ((num2 >= 0) ? Strings.Parties.Tiles.ToString(num2) : storyPlace));
		if (text5.Length != 0)
		{
			SaiyanHud.Draw(renderer, SaiyanHud.Texture("saiyan_suivi_puce.png"), 16, y + 47 + 12, 5, 5, Muted);
			int spacing3 = 0;
			string text6 = SaiyanHud.Fit(renderer, SaiyanHud.SmallFont, 8, text5, 228, ref spacing3);
			SaiyanHud.TextLeft(renderer, SaiyanHud.SmallFont, 8, text6, 28, y + 41 + 12, 18, Muted);
		}
	}

	private static Color Fade(Color color, float alpha)
	{
		if (!(alpha >= 1f))
		{
			return SaiyanHud.WithAlpha(color, alpha);
		}
		return color;
	}
}
