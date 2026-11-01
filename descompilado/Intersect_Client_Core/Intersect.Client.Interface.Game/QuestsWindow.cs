using System;
using System.Collections.Generic;
using System.Linq;
using Intersect.Client.Entities;
using Intersect.Client.Framework.Content;
using Intersect.Client.Framework.File_Management;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.Framework.Gwen;
using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.General;
using Intersect.Client.Interface.Game.Chat;
using Intersect.Client.Interface.Shared;
using Intersect.Client.Localization;
using Intersect.Client.Networking;
using Intersect.Enums;
using Intersect.Framework.Core.GameObjects.Items;
using Intersect.Framework.Core.GameObjects.NPCs;
using Intersect.Framework.Core.GameObjects.Quests;
using Intersect.GameObjects;
using Intersect.Localization;
using Intersect.Models;
using Intersect.Utilities;

namespace Intersect.Client.Interface.Game;

public class QuestsWindow : WindowControl
{
	private enum QuestState
	{
		InProgress,
		NotStarted,
		Completed
	}

	private const string BodyFont = "chakrapetch";

	private const int WindowWidth = 760;

	private const int WindowHeight = 600;

	private const int TopY = 72;

	private const int BottomY = 584;

	private const int ListX = 16;

	private const int ListWidth = 276;

	private const int DetailX = 304;

	private const int DetailWidth = 440;

	private const int RowHeight = 34;

	private static readonly Color Gold = new Color(255, 245, 190, 90);

	private static readonly Color White = new Color(255, 242, 242, 242);

	private static readonly Color Entry = new Color(255, 196, 204, 220);

	private static readonly Color Muted = new Color(255, 176, 186, 206);

	private static readonly Color Dim = new Color(255, 150, 160, 182);

	private static readonly Color Night = new Color(255, 11, 14, 23);

	private static readonly Color OrangeLight = new Color(255, 255, 172, 82);

	private static readonly Color Green = new Color(255, 120, 200, 140);

	private readonly Canvas _gameCanvas;

	private readonly IFont? _font;

	private readonly IFont? _strongFont;

	private readonly IFont? _posterFont;

	private readonly Label _title;

	private readonly Button _close;

	private readonly ImagePanel _listCard;

	private readonly ScrollControl _listContainer;

	private readonly List<Base> _rows = new List<Base>();

	private readonly ImagePanel _detailCard;

	private readonly ScrollControl _detailBody;

	private readonly Label _detailName;

	private readonly ImagePanel _detailChip;

	private readonly Label _detailChipLabel;

	private readonly Label _emptyHint;

	private readonly Button _abandonButton;

	private readonly Button _pinButton;

	private readonly ImagePanel _pinIcon;

	private readonly List<Base> _detailRows = new List<Base>();

	private Guid _selectedQuestId;

	private string _shownDetail = string.Empty;

	private string _shownList = string.Empty;

	private bool _shouldUpdateList;

	private int _laidOutTitleBarHeight = -1;

	public QuestsWindow(Canvas gameCanvas)
		: base(gameCanvas, string.Empty, modal: false, "QuestsWindow")
	{
		_gameCanvas = gameCanvas;
		_font = GameContentManager.Current.GetFont("chakrapetch");
		_strongFont = GameContentManager.Current.GetFont("rajdhanibold");
		_posterFont = GameContentManager.Current.GetFont("refuel");
		DisableResizing();
		SetSize(760, 600);
		SetPosition((_gameCanvas.Width - 760) / 2, (_gameCanvas.Height - 600) / 2);
		IGameTexture texture = GameContentManager.Current.GetTexture(TextureType.Gui, "saiyan_window_marche.png");
		SetImage(texture, "saiyan_window_marche.png", ControlState.Active);
		SetImage(texture, "saiyan_window_marche.png", ControlState.Inactive);
		base.IsClosable = false;
		if (base.Titlebar is Titlebar titlebar)
		{
			titlebar.Label.IsHidden = true;
		}
		_title = MakeLabel(this, "Title", 20, White, _posterFont);
		_title.Text = Strings.QuestLog.Title.ToString().ToUpperInvariant();
		WindowTitle.Apply(_title);
		_close = new Button(this, "Close", disableText: true);
		_close.SetStateTexture(ComponentState.Normal, "saiyan_close.png");
		_close.SetStateTexture(ComponentState.Hovered, "saiyan_close_hover.png");
		_close.SetStateTexture(ComponentState.Active, "saiyan_close_clicked.png");
		_close.Clicked += delegate
		{
			Hide();
		};
		_listCard = MakeCard(this, "ListCard");
		_listContainer = new ScrollControl(_listCard, "QuestList");
		StyleScroll(_listContainer);
		_detailCard = MakeCard(this, "DetailCard");
		_detailName = MakeLabel(_detailCard, "DetailName", 13, Gold, _font);
		_detailChip = MakeImage(_detailCard, "DetailChip", null, 100, 22);
		_detailChip.TextureNinePatchMargin = new Margin(6);
		_detailChipLabel = MakeLabel(_detailCard, "DetailChipLabel", 9, Gold, _font);
		_detailChipLabel.TextAlign = Pos.Center;
		_detailBody = new ScrollControl(_detailCard, "DetailBody");
		StyleScroll(_detailBody);
		_detailBody.AutoHideBars = true;
		_detailBody.MouseInputEnabled = true;
		_detailBody.InnerPanel.MouseInputEnabled = true;
		_emptyHint = MakeLabel(_detailCard, "EmptyHint", 11, Dim, _font);
		_emptyHint.Text = Strings.QuestLog.PickOne;
		_emptyHint.WrappingBehavior = WrappingBehavior.Wrapped;
		_emptyHint.TextAlign = Pos.Left | Pos.Top;
		_abandonButton = MakeButton(_detailCard, "Abandon", 150, 28);
		_abandonButton.Text = Strings.QuestLog.Abandon;
		_abandonButton.Clicked += delegate
		{
			AskToAbandon();
		};
		_pinButton = MakeButton(_detailCard, "Pin", 176, 28);
		_pinButton.Padding = new Padding(24, 0, 0, 0);
		_pinButton.Clicked += delegate
		{
			TogglePin(_selectedQuestId);
		};
		_pinIcon = MakeImage(_pinButton, "PinIcon", "saiyan_epingle.png", 20, 20);
		_pinIcon.SetPosition(12, 4);
		Layout();
		Hide();
	}

	public bool IsVisible()
	{
		return !base.IsHidden;
	}

	public void ShowQuest(Guid questId)
	{
		_selectedQuestId = questId;
		Show();
	}

	private void TogglePin(Guid questId)
	{
		if (!(questId == Guid.Empty))
		{
			if (!TrackedQuests.Toggle(questId))
			{
				ChatboxMsg.AddMessage(new ChatboxMsg(Strings.QuestLog.TrackFull.ToString(5), CustomColors.Alerts.Error, ChatMessageType.Notice));
			}
			_shownList = string.Empty;
			_shownDetail = string.Empty;
		}
	}

	public new void Show()
	{
		Layout();
		_shownList = string.Empty;
		_shownDetail = string.Empty;
		base.Show();
		BringToFront();
	}

	public void Update(bool shouldUpdateList)
	{
		if (shouldUpdateList)
		{
			_shouldUpdateList = true;
		}
		if (!base.IsHidden)
		{
			if (base.TitleBarBounds.Height != _laidOutTitleBarHeight)
			{
				Layout();
				_shownList = string.Empty;
				_shownDetail = string.Empty;
			}
			if (_shouldUpdateList)
			{
				_shouldUpdateList = false;
				_shownList = string.Empty;
				_shownDetail = string.Empty;
			}
			BuildList();
			BuildDetail();
		}
	}

	private static QuestState? StateOf(QuestDescriptor quest)
	{
		Player me = Globals.Me;
		if (me == null)
		{
			return null;
		}
		if (me.QuestProgress.TryGetValue(quest.Id, out QuestProgress value))
		{
			if (value.TaskId != Guid.Empty)
			{
				return QuestState.InProgress;
			}
			if (value.Completed)
			{
				if (!quest.LogAfterComplete)
				{
					return null;
				}
				return QuestState.Completed;
			}
		}
		if (quest.LogBeforeOffer && !me.HiddenQuests.Contains(quest.Id))
		{
			return QuestState.NotStarted;
		}
		return null;
	}

	private static Color ColorOf(QuestState state)
	{
		return state switch
		{
			QuestState.InProgress => Gold, 
			QuestState.Completed => Green, 
			_ => Entry, 
		};
	}

	private void BuildList()
	{
		List<(QuestDescriptor Quest, QuestState State)> list = new List<(QuestDescriptor Quest, QuestState State)>();
		foreach (IDatabaseObject value in DatabaseObject<QuestDescriptor>.Lookup.Values)
		{
			if (value is QuestDescriptor questDescriptor)
			{
				QuestState? questState = StateOf(questDescriptor);
				if (questState.HasValue)
				{
					QuestState valueOrDefault = questState.GetValueOrDefault();
					list.Add((questDescriptor, valueOrDefault));
				}
			}
		}
		list = (from q in list
			orderby (int)q.State, q.Quest.OrderValue, q.Quest.Name
			select q).ToList();
		if (_selectedQuestId == Guid.Empty || list.All<(QuestDescriptor, QuestState)>(((QuestDescriptor Quest, QuestState State) q) => q.Quest.Id != _selectedQuestId))
		{
			Guid guided = QuestMarkers.GuidedQuest();
			(QuestDescriptor, QuestState) tuple = list.FirstOrDefault<(QuestDescriptor, QuestState)>(((QuestDescriptor Quest, QuestState State) q) => q.Quest.Id == guided && q.State == QuestState.InProgress);
			if (tuple.Item1 == null)
			{
				tuple = list.FirstOrDefault<(QuestDescriptor, QuestState)>(((QuestDescriptor Quest, QuestState State) q) => q.State == QuestState.InProgress);
			}
			if (tuple.Item1 != null)
			{
				_selectedQuestId = tuple.Item1.Id;
			}
		}
		string text = string.Join("|", list.Select<(QuestDescriptor, QuestState), string>(((QuestDescriptor Quest, QuestState State) q) => $"{q.Quest.Id}:{q.State}")) + $"#{_selectedQuestId}#{TrackedQuests.Version}";
		if (text == _shownList)
		{
			return;
		}
		_shownList = text;
		foreach (Base row in _rows)
		{
			row.Parent?.RemoveChild(row, dispose: true);
		}
		_rows.Clear();
		int num = 272;
		if (list.Count == 0)
		{
			Label label = MakeLabel(_listContainer, "Empty", 10, Dim, _font);
			label.Text = Strings.QuestLog.Empty;
			label.WrappingBehavior = WrappingBehavior.Wrapped;
			label.TextAlign = Pos.Left | Pos.Top;
			label.SetSize(num - 36, 60);
			label.SetPosition(14, 12);
			_rows.Add(label);
			return;
		}
		QuestState? questState2 = null;
		int y = 0;
		foreach (var item in list)
		{
			var (quest, state) = item;
			if (questState2 != state)
			{
				questState2 = state;
				y = AddSection(state, list.Count<(QuestDescriptor, QuestState)>(((QuestDescriptor Quest, QuestState State) q) => q.State == state), num, y);
			}
			y = AddQuestRow(quest, state, num, y);
		}
	}

	private int AddSection(QuestState state, int count, int width, int y)
	{
		if (y > 0)
		{
			y += 8;
			ImagePanel imagePanel = MakeImage(_listContainer, $"Rule{y}", "hdv_filet.png", width - 20, 1);
			imagePanel.SetPosition(10, y);
			_rows.Add(imagePanel);
			y += 2;
		}
		Label label = MakeLabel(_listContainer, $"Section{state}", 10, Muted, _font);
		Label label2 = label;
		label2.Text = state switch
		{
			QuestState.InProgress => Strings.QuestLog.SectionInProgress, 
			QuestState.Completed => Strings.QuestLog.SectionCompleted, 
			_ => Strings.QuestLog.SectionNotStarted, 
		};
		label.SetSize(160, 20);
		label.SetPosition(14, y + 8);
		_rows.Add(label);
		Label label3 = MakeLabel(_listContainer, $"Count{state}", 9, Muted, _font);
		label3.Text = count.ToString();
		label3.TextAlign = Pos.Right | Pos.CenterV;
		label3.SetSize(30, 20);
		label3.SetPosition(width - 48, y + 8);
		_rows.Add(label3);
		return y + 30;
	}

	private int AddQuestRow(QuestDescriptor quest, QuestState state, int width, int y)
	{
		bool flag = quest.Id == _selectedQuestId;
		Guid questId = quest.Id;
		ImagePanel row = new ImagePanel(_listContainer, $"Quest_{questId}")
		{
			TextureNinePatchMargin = new Margin(flag ? 6 : 4)
		};
		row.SetSize(width - 28, 34);
		row.SetPosition(10, y);
		IGameTexture texture = GameContentManager.Current.GetTexture(TextureType.Gui, "saiyan_slot_selected.png");
		IGameTexture lit = GameContentManager.Current.GetTexture(TextureType.Gui, "hdv_ligne_survol.png");
		row.Texture = (flag ? texture : null);
		if (!flag)
		{
			row.HoverEnter += delegate
			{
				row.Texture = lit;
			};
			row.HoverLeave += delegate
			{
				row.Texture = null;
			};
		}
		row.Clicked += delegate
		{
			_selectedQuestId = questId;
			_shownList = string.Empty;
			_shownDetail = string.Empty;
		};
		ImagePanel imagePanel = MakeImage(row, "Dot", "saiyan_puce.png", 6, 6);
		ImagePanel imagePanel2 = imagePanel;
		imagePanel2.RenderColor = state switch
		{
			QuestState.Completed => Green, 
			QuestState.InProgress => Gold, 
			_ => Dim, 
		};
		imagePanel.SetPosition(10, 14);
		Label label = MakeLabel(row, "Name", 10, flag ? Gold : ColorOf(state), _font);
		label.Text = quest.Name;
		label.SetSize(width - ((state == QuestState.InProgress) ? 100 : 72), 34);
		label.SetPosition(24, 0);
		if (state == QuestState.InProgress)
		{
			bool flag2 = TrackedQuests.IsPinned(questId);
			Button button = new Button(row, "Pin", disableText: true);
			button.SetStateTexture(ComponentState.Normal, flag2 ? "saiyan_epingle.png" : "saiyan_epingle_vide.png");
			button.SetStateTexture(ComponentState.Hovered, flag2 ? "saiyan_epingle_survol.png" : "saiyan_epingle_vide_survol.png");
			button.SetStateTexture(ComponentState.Active, flag2 ? "saiyan_epingle_survol.png" : "saiyan_epingle_vide_survol.png");
			button.SetSize(20, 20);
			button.SetPosition(width - 28 - 30, 7);
			button.SetToolTipText(flag2 ? Strings.QuestLog.TrackTooltipUnpin : (TrackedQuests.IsFull ? ((LocalizedString)Strings.QuestLog.TrackFull.ToString(5)) : Strings.QuestLog.TrackTooltipPin));
			button.Clicked += delegate
			{
				TogglePin(questId);
			};
		}
		_rows.Add(row);
		return y + 34 + 4;
	}

	private void BuildDetail()
	{
		Player me = Globals.Me;
		QuestDescriptor databaseObject;
		QuestDescriptor questDescriptor = ((_selectedQuestId != Guid.Empty && DatabaseObject<QuestDescriptor>.TryGet(_selectedQuestId, out databaseObject)) ? databaseObject : null);
		QuestState? questState = ((questDescriptor == null) ? ((QuestState?)null) : StateOf(questDescriptor));
		if (!questState.HasValue)
		{
			questDescriptor = null;
		}
		QuestProgress value2;
		string value = ((questDescriptor != null && me != null && me.QuestProgress.TryGetValue(questDescriptor.Id, out value2)) ? $"{value2.TaskId}:{value2.TaskProgress}:{value2.Completed}" : "-");
		string text = ((questDescriptor == null) ? "vide" : $"{questDescriptor.Id}:{questState}:{value}:{TrackedQuests.Version}");
		if (text == _shownDetail)
		{
			return;
		}
		_shownDetail = text;
		foreach (Base detailRow in _detailRows)
		{
			detailRow.Parent?.RemoveChild(detailRow, dispose: true);
		}
		_detailRows.Clear();
		bool flag = questDescriptor != null && questState.HasValue;
		_detailName.IsHidden = !flag;
		_detailChip.IsHidden = !flag;
		_detailChipLabel.IsHidden = !flag;
		_abandonButton.IsHidden = !flag;
		_pinButton.IsHidden = !flag;
		_emptyHint.IsHidden = flag;
		if (questDescriptor != null && questState.HasValue)
		{
			_detailName.Text = questDescriptor.Name;
			ImagePanel detailChip = _detailChip;
			GameContentManager current2 = GameContentManager.Current;
			detailChip.Texture = current2.GetTexture(TextureType.Gui, questState switch
			{
				QuestState.InProgress => "saiyan_chip_quete_cours.png", 
				QuestState.Completed => "saiyan_chip_quete_finie.png", 
				_ => "saiyan_chip_quete_attente.png", 
			});
			Label detailChipLabel = _detailChipLabel;
			detailChipLabel.Text = questState switch
			{
				QuestState.InProgress => Strings.QuestLog.ChipInProgress, 
				QuestState.Completed => Strings.QuestLog.ChipCompleted, 
				_ => Strings.QuestLog.ChipNotStarted, 
			};
			_detailChipLabel.SetTextColor(ColorOf(questState.Value), ComponentState.Normal);
			_abandonButton.IsHidden = questState != QuestState.InProgress || !questDescriptor.Quitable;
			bool flag2 = TrackedQuests.IsPinned(questDescriptor.Id);
			_pinButton.IsHidden = questState != QuestState.InProgress;
			_pinButton.Text = (flag2 ? Strings.QuestLog.TrackUnpin : Strings.QuestLog.TrackPin);
			_pinButton.IsDisabled = !flag2 && TrackedQuests.IsFull;
			_pinButton.SetToolTipText(_pinButton.IsDisabled ? Strings.QuestLog.TrackFull.ToString(5) : null);
			_pinIcon.Texture = GameContentManager.Current.GetTexture(TextureType.Gui, flag2 ? "saiyan_epingle.png" : "saiyan_epingle_vide_survol.png");
			int y = 0;
			y = AddRule(y);
			y = AddParagraph(DescriptionOf(questDescriptor, questState.Value), y);
			if (questState == QuestState.InProgress && me != null && me.QuestProgress.TryGetValue(questDescriptor.Id, out QuestProgress value3))
			{
				y = AddObjective(questDescriptor, value3, y);
				y = AddStepReward(questDescriptor, value3, y);
				y = AddUpcoming(questDescriptor, value3, y);
			}
			AddRewards(questDescriptor, y);
			_detailBody.ScrollToTop();
		}
	}

	private static string DescriptionOf(QuestDescriptor quest, QuestState state)
	{
		switch (state)
		{
		case QuestState.InProgress:
			if (quest.InProgressDescription.Length > 0)
			{
				return quest.InProgressDescription;
			}
			return quest.StartDescription;
		case QuestState.Completed:
			return quest.EndDescription;
		default:
			return quest.BeforeDescription;
		}
	}

	private int AddObjective(QuestDescriptor quest, QuestProgress progress, int y)
	{
		QuestTaskDescriptor questTaskDescriptor = quest.Tasks.FirstOrDefault((QuestTaskDescriptor t) => t.Id == progress.TaskId);
		if (questTaskDescriptor == null)
		{
			return y;
		}
		y = AddRule(y);
		y = AddHeading(Strings.QuestLog.Objective, y);
		Label label = MakeLabel(_detailBody, $"Objective{y}", 11, White, _font);
		label.Text = TextOf(questTaskDescriptor);
		label.WrappingBehavior = WrappingBehavior.Wrapped;
		label.TextAlign = Pos.Left | Pos.Top;
		label.SetSize(412, 24);
		label.SetPosition(14, y);
		_detailRows.Add(label);
		y += 32;
		if (questTaskDescriptor.Objective == QuestObjective.EventDriven || questTaskDescriptor.Quantity <= 0)
		{
			return y;
		}
		int num = Math.Clamp(progress.TaskProgress, 0, questTaskDescriptor.Quantity);
		int num2 = 340;
		ImagePanel imagePanel = MakeImage(_detailBody, $"Track{y}", "saiyan_bar_track.png", num2, 12);
		imagePanel.TextureNinePatchMargin = new Margin(5);
		imagePanel.SetPosition(14, y);
		_detailRows.Add(imagePanel);
		int num3 = (int)((float)num2 * ((float)num / (float)questTaskDescriptor.Quantity));
		if (num3 > 0)
		{
			ImagePanel imagePanel2 = MakeImage(imagePanel, $"Fill{y}", "saiyan_bar_fill_exp.png", Math.Max(num3, 10), 12);
			imagePanel2.TextureNinePatchMargin = new Margin(5);
			imagePanel2.SetPosition(0, 0);
		}
		Label label2 = MakeLabel(_detailBody, $"Tally{y}", 13, Gold, _strongFont);
		label2.Text = Strings.QuestLog.Progress.ToString(num, questTaskDescriptor.Quantity);
		label2.TextAlign = Pos.Right | Pos.CenterV;
		label2.SetSize(66, 24);
		label2.SetPosition(360, y - 6);
		_detailRows.Add(label2);
		return y + 26;
	}

	private int AddStepReward(QuestDescriptor quest, QuestProgress progress, int y)
	{
		List<string> list = QuestRewardText.Parts(quest.Tasks.FirstOrDefault((QuestTaskDescriptor t) => t.Id == progress.TaskId)?.Rewards);
		if (list.Count == 0)
		{
			return y;
		}
		Label label = MakeLabel(_detailBody, $"StepReward{y}", 10, Gold, _font);
		label.Text = Strings.QuestLog.StepReward.ToString(string.Join(" · ", list));
		label.WrappingBehavior = WrappingBehavior.Wrapped;
		label.TextAlign = Pos.Left | Pos.Top;
		label.SetSize(412, 22);
		label.SetPosition(14, y);
		_detailRows.Add(label);
		return y + 26;
	}

	private int AddUpcoming(QuestDescriptor quest, QuestProgress progress, int y)
	{
		int num = quest.Tasks.FindIndex((QuestTaskDescriptor t) => t.Id == progress.TaskId);
		if (num < 0 || num + 1 >= quest.Tasks.Count)
		{
			return y;
		}
		y = AddRule(y);
		y = AddHeading(Strings.QuestLog.Upcoming, y);
		for (int num2 = num + 1; num2 < quest.Tasks.Count; num2++)
		{
			ImagePanel imagePanel = MakeImage(_detailBody, $"Next{num2}Dot", "saiyan_puce.png", 6, 6);
			imagePanel.RenderColor = Dim;
			imagePanel.SetPosition(18, y + 12);
			_detailRows.Add(imagePanel);
			Label label = MakeLabel(_detailBody, $"Next{num2}", 10, Dim, _font);
			label.Text = TextOf(quest.Tasks[num2]);
			label.SetSize(380, 30);
			label.SetPosition(34, y);
			_detailRows.Add(label);
			y += 28;
		}
		return y + 4;
	}

	private void AddRewards(QuestDescriptor quest, int y)
	{
		QuestReward rewards = quest.Rewards;
		if (rewards.IsEmpty)
		{
			return;
		}
		y = AddRule(y);
		y = AddHeading(Strings.QuestLog.Rewards, y);
		Guid guid = Globals.Me?.CurrencyItemId ?? Guid.Empty;
		long num = QuestRewardText.ZeniOf(rewards, guid);
		if (num > 0)
		{
			ItemDescriptor databaseObject;
			ItemDescriptor itemDescriptor = (DatabaseObject<ItemDescriptor>.TryGet(guid, out databaseObject) ? databaseObject : null);
			y = AddReward(null, (itemDescriptor == null) ? null : GameContentManager.Current.GetTexture(TextureType.Item, itemDescriptor.Icon), Strings.QuestLog.Zeni, Strings.QuestLog.ZeniValue.ToString(Amount(num)), Gold, y);
		}
		if (rewards.Experience > 0)
		{
			y = AddReward("saiyan_orbe_xp.png", null, Strings.QuestLog.Experience, Strings.QuestLog.ExperienceValue.ToString(Amount(rewards.Experience)), Gold, y);
		}
		foreach (QuestRewardItem item in rewards.Items)
		{
			if (!QuestRewardText.IsCurrency(item.ItemId, guid) && DatabaseObject<ItemDescriptor>.TryGet(item.ItemId, out var databaseObject2))
			{
				y = AddReward(null, GameContentManager.Current.GetTexture(TextureType.Item, databaseObject2.Icon), databaseObject2.Name, Strings.QuestLog.ItemQuantity.ToString(Amount(item.Quantity)), Entry, y);
			}
		}
		foreach (Guid spell in rewards.Spells)
		{
			if (DatabaseObject<SpellDescriptor>.TryGet(spell, out var databaseObject3))
			{
				y = AddReward(null, GameContentManager.Current.GetTexture(TextureType.Spell, databaseObject3.Icon), databaseObject3.Name, string.Empty, Entry, y);
			}
		}
	}

	private static string Amount(long value)
	{
		return value.ToString("N0").Replace(',', ' ');
	}

	private int AddReward(string? gui, IGameTexture? icon, string caption, string value, Color valueColor, int y)
	{
		if (gui != null)
		{
			ImagePanel imagePanel = MakeImage(_detailBody, $"Reward{y}Orb", gui, 26, 26);
			imagePanel.SetPosition(16, y + 1);
			_detailRows.Add(imagePanel);
		}
		else
		{
			ImagePanel imagePanel2 = MakeImage(_detailBody, $"Reward{y}Slot", "saiyan_slot.png", 26, 26);
			imagePanel2.TextureNinePatchMargin = new Margin(6);
			imagePanel2.SetPosition(16, y + 1);
			_detailRows.Add(imagePanel2);
			ImagePanel imagePanel3 = MakeImage(imagePanel2, $"Reward{y}Icon", null, 16, 16);
			imagePanel3.Texture = icon;
			imagePanel3.SetPosition(5, 5);
		}
		Label label = MakeLabel(_detailBody, $"Reward{y}Name", 10, White, _font);
		label.Text = caption;
		label.SetSize(214, 28);
		label.SetPosition(50, y);
		_detailRows.Add(label);
		if (value.Length > 0)
		{
			Label label2 = MakeLabel(_detailBody, $"Reward{y}Value", 13, valueColor, _strongFont);
			label2.Text = value;
			label2.TextAlign = Pos.Right | Pos.CenterV;
			label2.SetSize(138, 28);
			label2.SetPosition(270, y);
			_detailRows.Add(label2);
		}
		return y + 30;
	}

	private static string TextOf(QuestTaskDescriptor task)
	{
		if (!string.IsNullOrWhiteSpace(task.Description) && !TextUtils.IsNone(task.Description))
		{
			return task.Description;
		}
		return task.Objective switch
		{
			QuestObjective.GatherItems => Strings.QuestLog.TaskItem.ToString(0, task.Quantity, DatabaseObject<ItemDescriptor>.GetName(task.TargetId)), 
			QuestObjective.KillNpcs => Strings.QuestLog.TaskNpc.ToString(0, task.Quantity, DatabaseObject<NPCDescriptor>.GetName(task.TargetId)), 
			_ => string.Empty, 
		};
	}

	private int AddRule(int y)
	{
		ImagePanel imagePanel = MakeImage(_detailBody, $"Rule{y}", "hdv_filet.png", 420, 1);
		imagePanel.SetPosition(10, y);
		_detailRows.Add(imagePanel);
		return y + 8;
	}

	private int AddHeading(string text, int y)
	{
		Label label = MakeLabel(_detailBody, $"Heading{y}", 10, Muted, _font);
		label.Text = text;
		label.SetSize(412, 20);
		label.SetPosition(14, y);
		_detailRows.Add(label);
		return y + 24;
	}

	private int AddParagraph(string text, int y)
	{
		if (string.IsNullOrWhiteSpace(text))
		{
			return y;
		}
		Label label = MakeLabel(_detailBody, $"Text{y}", 10, White, _font);
		label.Text = text;
		label.WrappingBehavior = WrappingBehavior.Wrapped;
		label.TextAlign = Pos.Left | Pos.Top;
		label.SetSize(412, 56);
		label.SetPosition(14, y + 2);
		_detailRows.Add(label);
		return y + 66;
	}

	private void AskToAbandon()
	{
		if (DatabaseObject<QuestDescriptor>.TryGet(_selectedQuestId, out var databaseObject))
		{
			Guid questId = databaseObject.Id;
			ConfirmCardWindow.Open(new ConfirmCardRequest
			{
				Title = Strings.ConfirmCard.AbandonTitle,
				Heading = databaseObject.Name,
				Prompt = Strings.ConfirmCard.AbandonPrompt,
				Detail = Strings.ConfirmCard.AbandonDetail,
				ConfirmLabel = Strings.ConfirmCard.Abandon,
				Danger = true,
				Emblem = GameContentManager.Current.GetTexture(TextureType.Gui, "saiyan_embleme_quete.png")
			}, delegate
			{
				PacketSender.SendAbandonQuest(questId);
			});
		}
	}

	private new int Y(int windowY)
	{
		return windowY - _laidOutTitleBarHeight;
	}

	private void Layout()
	{
		_laidOutTitleBarHeight = base.TitleBarBounds.Height;
		_title.SetSize(400, 38);
		_title.SetPosition(26, Y(10));
		_close.SetSize(24, 24);
		_close.SetPosition(720, Y(16));
		int num = 512;
		_listCard.SetSize(276, num);
		_listCard.SetPosition(16, Y(72));
		_listContainer.SetSize(272, num - 12);
		_listContainer.SetPosition(2, 6);
		_detailCard.SetSize(440, num);
		_detailCard.SetPosition(304, Y(72));
		_detailName.SetSize(310, 26);
		_detailName.SetPosition(14, 14);
		_detailChip.SetPosition(324, 16);
		_detailChipLabel.SetSize(100, 22);
		_detailChipLabel.SetPosition(324, 16);
		_emptyHint.SetSize(412, 60);
		_emptyHint.SetPosition(14, 20);
		_detailBody.SetSize(440, num - 48 - 50);
		_detailBody.SetPosition(0, 48);
		_abandonButton.SetPosition(276, num - 42);
		_pinButton.SetPosition(14, num - 42);
	}

	private static Label MakeLabel(Base parent, string name, int size, Color color, IFont? font)
	{
		Label label = new Label(parent, name)
		{
			Font = font,
			FontSize = size,
			AutoSizeToContents = false,
			MouseInputEnabled = false,
			TextAlign = (Pos.Left | Pos.CenterV)
		};
		label.SetTextColor(color, ComponentState.Normal);
		return label;
	}

	private static void StyleScroll(ScrollControl scroll)
	{
		scroll.EnableScroll(horizontal: false, vertical: true);
		scroll.VerticalScrollBar.SetBackgroundTemplate(null, "saiyan_scroll_bg.png");
		IGameTexture texture = GameContentManager.Current.GetTexture(TextureType.Gui, "saiyan_scroll_bar.png");
		IGameTexture texture2 = GameContentManager.Current.GetTexture(TextureType.Gui, "saiyan_scroll_bar_hover.png");
		scroll.VerticalScrollBar.SetScrollBarImage(texture, "saiyan_scroll_bar.png", ComponentState.Normal);
		scroll.VerticalScrollBar.SetScrollBarImage(texture2, "saiyan_scroll_bar_hover.png", ComponentState.Hovered);
		scroll.VerticalScrollBar.SetScrollBarImage(texture2, "saiyan_scroll_bar_hover.png", ComponentState.Active);
	}

	private static ImagePanel MakeCard(Base parent, string name)
	{
		return new ImagePanel(parent, name)
		{
			Texture = GameContentManager.Current.GetTexture(TextureType.Gui, "saiyan_card.png"),
			TextureNinePatchMargin = new Margin(8),
			MouseInputEnabled = false
		};
	}

	private static ImagePanel MakeImage(Base parent, string name, string? texture, int width, int height)
	{
		ImagePanel imagePanel = new ImagePanel(parent, name)
		{
			MouseInputEnabled = false
		};
		if (texture != null)
		{
			imagePanel.Texture = GameContentManager.Current.GetTexture(TextureType.Gui, texture);
		}
		imagePanel.SetSize(width, height);
		return imagePanel;
	}

	private Button MakeButton(Base parent, string name, int width, int height)
	{
		Button button = new Button(parent, name)
		{
			Font = _font,
			FontSize = 11,
			TextAlign = Pos.Center
		};
		string text = $"saiyan_btn_{width}x{height}";
		button.SetStateTexture(ComponentState.Normal, text + ".png");
		button.SetStateTexture(ComponentState.Hovered, text + "_hover.png");
		button.SetStateTexture(ComponentState.Active, text + "_clicked.png");
		button.SetStateTexture(ComponentState.Disabled, text + "_disabled.png");
		button.SetTextColor(Entry, ComponentState.Normal);
		button.SetTextColor(OrangeLight, ComponentState.Hovered);
		button.SetTextColor(Night, ComponentState.Active);
		button.SetTextColor(Dim, ComponentState.Disabled);
		button.SetSize(width, height);
		return button;
	}
}
