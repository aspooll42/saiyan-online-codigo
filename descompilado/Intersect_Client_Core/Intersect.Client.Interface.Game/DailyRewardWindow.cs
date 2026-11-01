using System;
using System.Collections.Generic;
using System.Linq;
using Intersect.Client.Framework.Content;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.Framework.Gwen;
using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.General;
using Intersect.Client.Interface.Shared;
using Intersect.Client.Localization;
using Intersect.Client.Networking;
using Intersect.Framework.Core;
using Intersect.Framework.Core.GameObjects.DailyRewards;
using Intersect.Framework.Core.GameObjects.Items;
using Intersect.Models;

namespace Intersect.Client.Interface.Game;

public class DailyRewardWindow : WindowControl
{
	private const int WindowWidth = 760;

	private const int WindowHeight = 640;

	private const int HeaderHeight = 56;

	private new const int Margin = 16;

	private const int Columns = 6;

	private const int CellWidth = 116;

	private const int CellHeight = 74;

	private const int CellGap = 6;

	private const int HeroHeight = 100;

	private const int StreakHeight = 34;

	private const int IconSize = 26;

	private const int MilestoneIconSize = 32;

	private static readonly Color OrangeLight = new Color(255, 255, 172, 82);

	private static readonly Color Gold = new Color(255, 245, 190, 90);

	private static readonly Color GoldSoft = new Color(255, 255, 226, 150);

	private static readonly Color GoldDark = new Color(255, 180, 130, 40);

	private static readonly Color White = new Color(255, 242, 242, 242);

	private static readonly Color Muted = new Color(255, 176, 186, 206);

	private static readonly Color Dim = new Color(255, 150, 160, 182);

	private static readonly Color DimDark = new Color(255, 110, 120, 145);

	private static readonly Color Night = new Color(255, 11, 14, 23);

	private static readonly Color KiLight = new Color(255, 120, 176, 255);

	private static readonly Color TakenTint = new Color(255, 140, 140, 150);

	private readonly Canvas _gameCanvas;

	private readonly IFont? _font;

	private readonly IFont? _displayFont;

	private readonly Label _title;

	private readonly Label _subtitle;

	private readonly Label _total;

	private readonly Button _close;

	private readonly ImagePanel _hero;

	private readonly ImagePanel _heroSlot;

	private readonly ImagePanel _heroIcon;

	private readonly Label _heroDay;

	private readonly Label _heroAmount;

	private readonly Label _heroNote;

	private readonly Button _claim;

	private readonly Label _streakLabel;

	private readonly ImagePanel _streakTrack;

	private readonly ImagePanel _streakFill;

	private readonly Label _streakProgress;

	private readonly List<Base> _streakMarks = new List<Base>();

	private readonly Base _grid;

	private readonly Label _footerNote;

	private readonly List<Base> _cells = new List<Base>();

	private ImagePanel? _todayCell;

	private bool _needsRebuild = true;

	private int _laidOutTitleBarHeight;

	private int _streakY;

	public bool IsOpen => !base.IsHidden;

	public DailyRewardWindow(Canvas gameCanvas)
		: base(gameCanvas, string.Empty, modal: false, "DailyRewardWindow")
	{
		_gameCanvas = gameCanvas;
		_font = Globals.ContentManager.GetFont("chakrapetch");
		_displayFont = Globals.ContentManager.GetFont("rajdhanibold");
		DisableResizing();
		SetSize(760, 640);
		SetPosition((_gameCanvas.Width - 760) / 2, (_gameCanvas.Height - 640) / 2);
		SetImage(Globals.ContentManager.GetTexture(TextureType.Gui, "saiyan_window_recompenses.png"), "saiyan_window_recompenses.png", ControlState.Active);
		SetImage(Globals.ContentManager.GetTexture(TextureType.Gui, "saiyan_window_recompenses.png"), "saiyan_window_recompenses.png", ControlState.Inactive);
		base.IsClosable = false;
		if (base.Titlebar is Titlebar titlebar)
		{
			titlebar.Label.IsHidden = true;
		}
		_title = MakeLabel(this, "Title", 20, White, display: true);
		_title.Font = Globals.ContentManager.GetFont("refuel");
		_title.TextAlign = Pos.Left | Pos.CenterV;
		_title.Text = Strings.DailyRewards.Title;
		WindowTitle.Apply(_title);
		_subtitle = MakeLabel(this, "Subtitle", 10, Muted);
		_subtitle.TextAlign = Pos.Left | Pos.CenterV;
		_subtitle.Text = Strings.DailyRewards.Subtitle;
		_total = MakeLabel(this, "Total", 13, White, display: true);
		_total.TextAlign = Pos.Right | Pos.CenterV;
		_close = new Button(this, "Close", disableText: true);
		_close.SetStateTexture(ComponentState.Normal, "saiyan_close.png");
		_close.SetStateTexture(ComponentState.Hovered, "saiyan_close_hover.png");
		_close.SetStateTexture(ComponentState.Active, "saiyan_close_clicked.png");
		_close.Clicked += delegate
		{
			Close();
		};
		_hero = MakeImage(this, "Hero", "saiyan_rq_bandeau.png", 728, 100);
		_heroSlot = MakeImage(_hero, "HeroSlot", "saiyan_rq_slot.png", 64, 64);
		_heroSlot.SetPosition(20, 18);
		_heroIcon = MakeImage(_heroSlot, "HeroIcon", null, 36, 36);
		_heroIcon.SetPosition(14, 14);
		_heroDay = MakeLabel(_hero, "HeroDay", 10, OrangeLight);
		_heroDay.TextAlign = Pos.Left | Pos.CenterV;
		_heroDay.SetBounds(100, 10, 400, 18);
		_heroAmount = MakeLabel(_hero, "HeroAmount", 23, White, display: true);
		_heroAmount.TextAlign = Pos.Left | Pos.CenterV;
		_heroAmount.SetBounds(98, 30, 400, 40);
		_heroNote = MakeLabel(_hero, "HeroNote", 10, Muted);
		_heroNote.TextAlign = Pos.Left | Pos.CenterV;
		_heroNote.SetBounds(100, 70, 420, 18);
		_claim = new Button(_hero, "Claim")
		{
			Font = _font,
			FontSize = 12,
			Text = Strings.DailyRewards.Claim
		};
		_claim.SetStateTexture(ComponentState.Normal, "saiyan_rq_bouton.png");
		_claim.SetStateTexture(ComponentState.Hovered, "saiyan_rq_bouton_survol.png");
		_claim.SetStateTexture(ComponentState.Active, "saiyan_rq_bouton_clic.png");
		_claim.SetStateTexture(ComponentState.Disabled, "saiyan_rq_bouton_inactif.png");
		_claim.SetTextColor(Night, ComponentState.Normal);
		_claim.SetTextColor(Night, ComponentState.Hovered);
		_claim.SetTextColor(Night, ComponentState.Active);
		_claim.SetTextColor(Dim, ComponentState.Disabled);
		_claim.SetBounds(496, 25, 210, 50);
		_claim.Clicked += delegate
		{
			Claim();
		};
		_streakLabel = MakeLabel(this, "StreakLabel", 9, Muted);
		_streakLabel.TextAlign = Pos.Left | Pos.CenterV;
		_streakLabel.Text = Strings.DailyRewards.Streak.ToString().ToUpperInvariant();
		_streakTrack = MakeImage(this, "StreakTrack", "saiyan_bar_track.png", 10, 10);
		_streakTrack.TextureNinePatchMargin = new Margin(5);
		_streakFill = MakeImage(_streakTrack, "StreakFill", "saiyan_bar_fill_done.png", 10, 10);
		_streakFill.TextureNinePatchMargin = new Margin(5);
		_streakFill.IsHidden = true;
		_streakProgress = MakeLabel(this, "StreakProgress", 13, GoldSoft, display: true);
		_streakProgress.TextAlign = Pos.Right | Pos.CenterV;
		_grid = new Base(this, "Grid")
		{
			MouseInputEnabled = false
		};
		_footerNote = MakeLabel(this, "FooterNote", 10, Muted);
		_footerNote.TextAlign = Pos.Center;
		Layout();
		Hide();
	}

	public void Toggle()
	{
		if (IsOpen)
		{
			Close();
		}
		else
		{
			Open();
		}
	}

	public void Open()
	{
		Layout();
		_needsRebuild = true;
		Show();
		BringToFront();
	}

	public void Refresh()
	{
		_needsRebuild = true;
	}

	public void Update()
	{
		if (!base.IsHidden)
		{
			if (base.TitleBarBounds.Height != _laidOutTitleBarHeight)
			{
				Layout();
				_needsRebuild = true;
			}
			if (_needsRebuild)
			{
				_needsRebuild = false;
				Rebuild();
			}
			PulseToday();
		}
	}

	private void Claim()
	{
		if (Globals.DailyRewardCanClaim)
		{
			PacketSender.SendDailyRewardClaim();
			Globals.DailyRewardCanClaim = false;
			_needsRebuild = true;
		}
	}

	private Label MakeLabel(Base parent, string name, int size, Color color, bool display = false)
	{
		Label label = new Label(parent, name)
		{
			Font = (display ? _displayFont : _font),
			FontSize = size,
			AutoSizeToContents = false,
			MouseInputEnabled = false
		};
		label.SetTextColor(color, ComponentState.Normal);
		return label;
	}

	private static ImagePanel MakeImage(Base parent, string name, string? texture, int width, int height)
	{
		ImagePanel imagePanel = new ImagePanel(parent, name)
		{
			MouseInputEnabled = false
		};
		if (texture != null)
		{
			imagePanel.Texture = Globals.ContentManager.GetTexture(TextureType.Gui, texture);
		}
		imagePanel.SetSize(width, height);
		return imagePanel;
	}

	private void Layout()
	{
		Base obj = _grid.Parent ?? this;
		_laidOutTitleBarHeight = base.TitleBarBounds.Height;
		int num = Math.Min(obj.Height, 640 - _laidOutTitleBarHeight);
		int num2 = 56 - _laidOutTitleBarHeight;
		_title.SetSize(420, 38);
		_title.SetPosition(24, num2 - 54);
		_subtitle.SetSize(420, 18);
		_subtitle.SetPosition(26, num2 - 22);
		_close.SetSize(24, 24);
		_close.SetPosition(obj.Width - 16 - 24, num2 - 38);
		_total.SetSize(240, 26);
		_total.SetPosition(obj.Width - 16 - 24 - 14 - 240, num2 - 38);
		int num3 = obj.Width - 32;
		_hero.SetSize(num3, 100);
		_hero.SetPosition(16, num2 + 16);
		_claim.SetPosition(num3 - 232, 25);
		_streakY = num2 + 16 + 100 + 14;
		_streakLabel.SetSize(60, 16);
		_streakLabel.SetPosition(16, _streakY);
		_streakProgress.SetSize(80, 24);
		_streakProgress.SetPosition(obj.Width - 16 - 80, _streakY - 5);
		_streakTrack.SetSize(num3 - 52 - 90, 10);
		_streakTrack.SetPosition(68, _streakY + 3);
		int num4 = 726;
		int y = _streakY + 34;
		_grid.SetSize(num4, 394);
		_grid.SetPosition((obj.Width - num4) / 2, y);
		_footerNote.SetSize(obj.Width - 32, 18);
		_footerNote.SetPosition(16, num - 22);
	}

	private void Rebuild()
	{
		foreach (Base cell in _cells)
		{
			cell.Parent?.RemoveChild(cell, dispose: true);
		}
		foreach (Base streakMark in _streakMarks)
		{
			streakMark.Parent?.RemoveChild(streakMark, dispose: true);
		}
		_cells.Clear();
		_streakMarks.Clear();
		_todayCell = null;
		DailyRewardEntry[] dailyRewards = Globals.DailyRewards;
		if (dailyRewards.Length == 0)
		{
			Label label = MakeLabel(_grid, "DailyRewardsEmpty", 11, Dim);
			label.Text = Strings.DailyRewards.Empty;
			label.TextAlign = Pos.Left | Pos.CenterV;
			label.SetSize(_grid.Width, 20);
			label.SetPosition(0, 8);
			_cells.Add(label);
			_claim.IsDisabled = true;
			_hero.Hide();
			_footerNote.SetText(string.Empty);
			return;
		}
		_hero.Show();
		for (int i = 0; i < dailyRewards.Length; i++)
		{
			_cells.Add(BuildCell(dailyRewards[i], i));
		}
		BuildHero(dailyRewards);
		BuildStreak(dailyRewards);
		_total.SetText(Strings.DailyRewards.Total.ToString(Globals.DailyRewardTotal));
		_claim.IsDisabled = !Globals.DailyRewardCanClaim;
		_footerNote.SetText(Globals.DailyRewardCanClaim ? Strings.DailyRewards.Ready : Strings.DailyRewards.ComeBack);
	}

	private void BuildHero(DailyRewardEntry[] calendar)
	{
		DailyRewardEntry current = calendar.FirstOrDefault((DailyRewardEntry entry) => entry.Day == Globals.DailyRewardDay) ?? calendar[0];
		bool dailyRewardCanClaim = Globals.DailyRewardCanClaim;
		_heroDay.Text = (dailyRewardCanClaim ? Strings.DailyRewards.Today : Strings.DailyRewards.Tomorrow).ToString(current.Day);
		var (gameTexture, renderColor) = RewardIcon(current, taken: false);
		_heroIcon.Texture = gameTexture;
		_heroIcon.RenderColor = renderColor;
		_heroIcon.IsHidden = gameTexture == null;
		var (text, text2, flag) = RewardLines(current);
		_heroAmount.Text = (flag ? text : (text + " " + text2));
		_heroAmount.SetTextColor(current.Milestone ? Gold : White, ComponentState.Normal);
		if (current.Milestone)
		{
			_heroNote.Text = Strings.DailyRewards.MilestoneToday.ToString(current.Label);
			return;
		}
		DailyRewardEntry dailyRewardEntry = (from entry in calendar
			where entry.Milestone && entry.Day > current.Day
			orderby entry.Day
			select entry).FirstOrDefault();
		if (dailyRewardEntry == null)
		{
			_heroNote.Text = string.Empty;
			return;
		}
		string text3 = (string.IsNullOrWhiteSpace(dailyRewardEntry.ItemName) ? dailyRewardEntry.Label : Traduction.Contenu(dailyRewardEntry.ItemName));
		int num = dailyRewardEntry.Day - current.Day;
		_heroNote.Text = ((num <= 1) ? Strings.DailyRewards.NextMilestoneTomorrow.ToString(text3) : Strings.DailyRewards.NextMilestone.ToString(num, text3));
	}

	private void BuildStreak(DailyRewardEntry[] calendar)
	{
		int num = Math.Max(1, calendar.Length);
		int num2 = Math.Clamp(Globals.DailyRewardDay - 1, 0, num);
		_streakProgress.Text = Strings.DailyRewards.CycleProgress.ToString(Math.Clamp(Globals.DailyRewardDay, 1, num), num);
		int width = _streakTrack.Width;
		int num3 = (int)((float)(width * num2) / (float)num);
		_streakFill.IsHidden = num3 <= 0;
		_streakFill.SetSize(Math.Max(10, num3), 10);
		_streakFill.SetPosition(0, 0);
		foreach (DailyRewardEntry item in calendar.Where((DailyRewardEntry entry) => entry.Milestone))
		{
			int num4 = _streakTrack.X + (int)((float)(width * item.Day) / (float)num);
			ImagePanel imagePanel = MakeImage(this, $"Milestone{item.Day}", "saiyan_rq_losange.png", 14, 14);
			imagePanel.RenderColor = ((item.Day <= num2) ? GoldSoft : GoldDark);
			imagePanel.SetPosition(num4 - 7, _streakY + 1);
			_streakMarks.Add(imagePanel);
			Label label = MakeLabel(this, $"MilestoneDay{item.Day}", 9, Muted);
			label.Text = item.Day.ToString();
			label.TextAlign = Pos.Center;
			label.SetSize(30, 14);
			label.SetPosition(num4 - 15, _streakY + 16);
			_streakMarks.Add(label);
		}
	}

	private Base BuildCell(DailyRewardEntry entry, int index)
	{
		bool flag = entry.Day == Globals.DailyRewardDay && Globals.DailyRewardCanClaim;
		bool flag2 = entry.Day < Globals.DailyRewardDay;
		string name = (flag2 ? "saiyan_rq_case_prise.png" : (flag ? "saiyan_rq_case_aujourdhui.png" : (entry.Milestone ? "saiyan_rq_case_palier.png" : "saiyan_rq_case.png")));
		ImagePanel cell = new ImagePanel(_grid, $"Day_{entry.Day}")
		{
			Texture = Globals.ContentManager.GetTexture(TextureType.Gui, name),
			MouseInputEnabled = true
		};
		cell.SetSize(116, 74);
		cell.SetPosition(index % 6 * 122, index / 6 * 80);
		if (!flag2 && !flag && !entry.Milestone)
		{
			cell.HoverEnter += delegate
			{
				cell.Texture = Globals.ContentManager.GetTexture(TextureType.Gui, "saiyan_rq_case_survol.png");
			};
			cell.HoverLeave += delegate
			{
				cell.Texture = Globals.ContentManager.GetTexture(TextureType.Gui, "saiyan_rq_case.png");
			};
		}
		if (flag)
		{
			_todayCell = cell;
		}
		Color color = (flag2 ? DimDark : (flag ? OrangeLight : (entry.Milestone ? GoldSoft : Muted)));
		Label label = MakeLabel(cell, "DayNumber", 9, color);
		label.Text = Strings.DailyRewards.Day.ToString(entry.Day).ToUpperInvariant();
		label.TextAlign = Pos.Left | Pos.CenterV;
		label.SetSize(76, 16);
		label.SetPosition(9, 5);
		if (entry.Milestone && !flag2)
		{
			ImagePanel imagePanel = MakeImage(cell, "Diamond", "hud_joueur_losange.png", 6, 6);
			imagePanel.RenderColor = Gold;
			imagePanel.SetPosition(101, 9);
		}
		int num = (entry.Milestone ? 32 : 26);
		(IGameTexture? Texture, Color Tint) tuple = RewardIcon(entry, flag2);
		IGameTexture item = tuple.Texture;
		Color item2 = tuple.Tint;
		ImagePanel imagePanel2 = MakeImage(cell, "Icon", null, num, num);
		imagePanel2.Texture = item;
		imagePanel2.RenderColor = item2;
		imagePanel2.IsHidden = item == null;
		imagePanel2.SetPosition(10, 74 - num - 10);
		int num2 = 10 + num + 7;
		int width = 116 - num2 - 2;
		(string Primary, string Secondary, bool IsItem) tuple2 = RewardLines(entry);
		string item3 = tuple2.Primary;
		string item4 = tuple2.Secondary;
		bool item5 = tuple2.IsItem;
		Color color2 = (flag2 ? DimDark : (entry.Milestone ? GoldSoft : White));
		Color color3 = (flag2 ? DimDark : (entry.Milestone ? Gold : Muted));
		if (item5)
		{
			Label label2 = MakeLabel(cell, "Primary", 9, color2);
			label2.Text = item3;
			label2.TextAlign = Pos.Left | Pos.Top;
			label2.WrappingBehavior = WrappingBehavior.Wrapped;
			label2.SetSize(width, 34);
			label2.SetPosition(num2, 24);
			Label label3 = MakeLabel(cell, "Secondary", 9, color3);
			label3.Text = item4;
			label3.TextAlign = Pos.Left | Pos.CenterV;
			label3.SetSize(width, 16);
			label3.SetPosition(num2, 52);
		}
		else
		{
			Label label4 = MakeLabel(cell, "Primary", 13, color2, display: true);
			label4.Text = item3;
			label4.TextAlign = Pos.Left | Pos.CenterV;
			label4.SetSize(width, 24);
			label4.SetPosition(num2, 28);
			Label label5 = MakeLabel(cell, "Secondary", 9, color3);
			label5.Text = item4;
			label5.TextAlign = Pos.Left | Pos.CenterV;
			label5.SetSize(width, 16);
			label5.SetPosition(num2, 50);
		}
		if (flag2)
		{
			ImagePanel imagePanel3 = MakeImage(cell, "Check", "saiyan_rq_coche.png", 18, 18);
			imagePanel3.SetPosition(90, 6);
		}
		return cell;
	}

	private static (IGameTexture? Texture, Color Tint) RewardIcon(DailyRewardEntry entry, bool taken)
	{
		Color item = (taken ? TakenTint : new Color(255, 255, 255, 255));
		if (!string.IsNullOrWhiteSpace(entry.ItemName))
		{
			ItemDescriptor itemDescriptor = ItemByName(entry.ItemName);
			return (Texture: (itemDescriptor == null) ? null : Globals.ContentManager.GetTexture(TextureType.Item, itemDescriptor.Icon), Tint: item);
		}
		if (entry.BonusPercent > 0)
		{
			return (Texture: Globals.ContentManager.GetTexture(TextureType.Gui, "saiyan_rq_eclair.png"), Tint: taken ? DimDark : KiLight);
		}
		if (entry.Experience > 0 && entry.Zeni <= 0)
		{
			return (Texture: Globals.ContentManager.GetTexture(TextureType.Gui, "saiyan_orbe_xp.png"), Tint: item);
		}
		return (Texture: CurrencyIcon(), Tint: item);
	}

	private static (string Primary, string Secondary, bool IsItem) RewardLines(DailyRewardEntry entry)
	{
		if (!string.IsNullOrWhiteSpace(entry.ItemName))
		{
			string text = Traduction.Contenu(entry.ItemName);
			string item = ((entry.ItemQuantity > 1) ? Strings.DailyRewards.ItemQuantity.ToString(text, entry.ItemQuantity) : text);
			string item2 = ((entry.Zeni > 0) ? Strings.DailyRewards.ZeniExtra.ToString(MarketWindow.FormatAmount(entry.Zeni)) : ((entry.Experience > 0) ? $"+{MarketWindow.FormatAmount(entry.Experience)} {Strings.DailyRewards.ExperienceUnit}" : string.Empty));
			return (Primary: item, Secondary: item2, IsItem: true);
		}
		if (entry.BonusPercent > 0)
		{
			return (Primary: Strings.DailyRewards.BonusValue.ToString(entry.BonusPercent), Secondary: Strings.DailyRewards.BonusUnit.ToString(entry.BonusMinutes), IsItem: false);
		}
		if (entry.Experience > 0 && entry.Zeni <= 0)
		{
			return (Primary: MarketWindow.FormatAmount(entry.Experience), Secondary: Strings.DailyRewards.ExperienceUnit, IsItem: false);
		}
		return (Primary: MarketWindow.FormatAmount(entry.Zeni), Secondary: Strings.DailyRewards.ZeniUnit, IsItem: false);
	}

	private static ItemDescriptor? ItemByName(string name)
	{
		string translated = Traduction.Contenu(name.Trim());
		return DatabaseObject<ItemDescriptor>.Lookup.Values.OfType<ItemDescriptor>().FirstOrDefault((ItemDescriptor item) => string.Equals(item.Name?.Trim(), name.Trim(), StringComparison.OrdinalIgnoreCase) || string.Equals(item.Name?.Trim(), translated, StringComparison.OrdinalIgnoreCase));
	}

	private static IGameTexture? CurrencyIcon()
	{
		Guid guid = Globals.Me?.CurrencyItemId ?? Guid.Empty;
		if (guid != Guid.Empty && DatabaseObject<ItemDescriptor>.TryGet(guid, out var databaseObject) && !string.IsNullOrWhiteSpace(databaseObject.Icon))
		{
			IGameTexture texture = Globals.ContentManager.GetTexture(TextureType.Item, databaseObject.Icon);
			if (texture != null)
			{
				return texture;
			}
		}
		return Globals.ContentManager.GetTexture(TextureType.Item, "zeni_anime.png") ?? Globals.ContentManager.GetTexture(TextureType.Item, "Zéni.png");
	}

	private void PulseToday()
	{
		ImagePanel todayCell = _todayCell;
		if (todayCell != null)
		{
			int value = 210 + (int)(45f * MathF.Sin((float)Timing.Global.Milliseconds / 320f));
			todayCell.RenderColor = new Color(Math.Clamp(value, 0, 255), 255, 255, 255);
		}
	}
}
