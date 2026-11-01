using System;
using System.Collections.Generic;
using System.Linq;
using Intersect.Client.Core;
using Intersect.Client.Framework.Content;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.Framework.Gwen;
using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.General;
using Intersect.Client.Interface.Shared;
using Intersect.Client.Localization;
using Intersect.Framework.Core;
using Intersect.Framework.Core.GameObjects.Achievements;
using Intersect.Framework.Core.GameObjects.Items;
using Intersect.Framework.Core.GameObjects.Ornaments;
using Intersect.Framework.Core.GameObjects.Titles;
using Intersect.Models;

namespace Intersect.Client.Interface.Game;

public class AchievementsWindow : WindowControl
{
	private readonly record struct RewardChip(IGameTexture? Icon, Rectangle Source, int IconWidth, string Text, Color Color, ItemDescriptor? Item, int Quantity);

	private const int WindowWidth = 860;

	private const int WindowHeight = 560;

	private const int HeaderHeight = 56;

	private const int SidebarX = 16;

	private const int SidebarWidth = 184;

	private const int TabHeight = 38;

	private const int TabGap = 6;

	private const int CardMinHeight = 100;

	private const int CardGap = 8;

	private new const int Margin = 16;

	private const int MedalSize = 56;

	private const int TextX = 86;

	private const int RewardsColumnX = 262;

	private const int RewardsRightMargin = 12;

	private const int ChipHeight = 26;

	private const int ChipGap = 5;

	private const int ChipRowGap = 5;

	private const int ChipIconHeight = 22;

	private const int ChipIconMaxWidth = 44;

	private const int ChipsTop = 34;

	private const int PointsBarWidth = 174;

	private const float FillAnimationMs = 650f;

	private static readonly Color OrangeLight = new Color(255, 255, 172, 82);

	private static readonly Color Gold = new Color(255, 245, 190, 90);

	private static readonly Color GoldSoft = new Color(255, 255, 226, 150);

	private static readonly Color White = new Color(255, 242, 242, 242);

	private static readonly Color EntryName = new Color(255, 196, 204, 220);

	private static readonly Color Muted = new Color(255, 176, 186, 206);

	private static readonly Color Dim = new Color(255, 150, 160, 182);

	private static readonly Color Rule = new Color(200, 42, 51, 73);

	private readonly Canvas _gameCanvas;

	private readonly IFont? _font;

	private readonly IFont? _displayFont;

	private readonly Label _title;

	private readonly Label _subtitle;

	private readonly ImagePanel _star;

	private readonly Label _summary;

	private readonly ImagePanel _pointsTrack;

	private readonly ImagePanel _pointsFill;

	private readonly Button _close;

	private readonly ScrollControl _list;

	private readonly List<Button> _tabs = new List<Button>();

	private readonly List<Base> _rows = new List<Base>();

	private readonly List<(ImagePanel Fill, int TargetWidth, long StartedAt)> _fills = new List<(ImagePanel, int, long)>();

	private string _selectedCategory = string.Empty;

	private bool _needsRebuild = true;

	private int _laidOutTitleBarHeight;

	public bool IsOpen => !base.IsHidden;

	public AchievementsWindow(Canvas gameCanvas)
		: base(gameCanvas, string.Empty, modal: false, "AchievementsWindow")
	{
		_gameCanvas = gameCanvas;
		_font = Globals.ContentManager.GetFont("chakrapetch");
		_displayFont = Globals.ContentManager.GetFont("rajdhanibold");
		DisableResizing();
		SetSize(860, 560);
		SetPosition((_gameCanvas.Width - 860) / 2, (_gameCanvas.Height - 560) / 2);
		SetImage(Globals.ContentManager.GetTexture(TextureType.Gui, "saiyan_window_succes.png"), "saiyan_window_succes.png", ControlState.Active);
		SetImage(Globals.ContentManager.GetTexture(TextureType.Gui, "saiyan_window_succes.png"), "saiyan_window_succes.png", ControlState.Inactive);
		base.IsClosable = false;
		if (base.Titlebar is Titlebar titlebar)
		{
			titlebar.Label.IsHidden = true;
		}
		_title = MakeLabel(this, "Title", 20, White, display: true);
		_title.Font = Globals.ContentManager.GetFont("refuel");
		_title.TextAlign = Pos.Left | Pos.CenterV;
		_title.Text = Strings.Achievements.Title;
		WindowTitle.Apply(_title);
		_subtitle = MakeLabel(this, "Subtitle", 10, Muted);
		_subtitle.TextAlign = Pos.Left | Pos.CenterV;
		_subtitle.Text = Strings.Achievements.Subtitle;
		_star = new ImagePanel(this, "Star")
		{
			Texture = Globals.ContentManager.GetTexture(TextureType.Gui, "saiyan_star.png"),
			MouseInputEnabled = false
		};
		_summary = MakeLabel(this, "Summary", 13, White, display: true);
		_summary.TextAlign = Pos.Right | Pos.CenterV;
		_pointsTrack = new ImagePanel(this, "PointsTrack")
		{
			Texture = Globals.ContentManager.GetTexture(TextureType.Gui, "saiyan_bar_track.png"),
			TextureNinePatchMargin = new Margin(5),
			MouseInputEnabled = false
		};
		_pointsFill = new ImagePanel(_pointsTrack, "PointsFill")
		{
			Texture = Globals.ContentManager.GetTexture(TextureType.Gui, "saiyan_bar_fill_done.png"),
			TextureNinePatchMargin = new Margin(5),
			MouseInputEnabled = false,
			IsHidden = true
		};
		_close = new Button(this, "Close", disableText: true);
		_close.SetStateTexture(ComponentState.Normal, "saiyan_close.png");
		_close.SetStateTexture(ComponentState.Hovered, "saiyan_close_hover.png");
		_close.SetStateTexture(ComponentState.Active, "saiyan_close_clicked.png");
		_close.Clicked += delegate
		{
			Close();
		};
		_list = new ScrollControl(this, "AchievementsList");
		_list.EnableScroll(horizontal: false, vertical: true);
		_list.VerticalScrollBar.SetBackgroundTemplate(null, "saiyan_scroll_bg.png");
		_list.VerticalScrollBar.SetScrollBarImage(null, "saiyan_scroll_bar.png", ComponentState.Normal);
		_list.VerticalScrollBar.SetScrollBarImage(null, "saiyan_scroll_bar_hover.png", ComponentState.Hovered);
		_list.VerticalScrollBar.SetScrollBarImage(null, "saiyan_scroll_bar_hover.png", ComponentState.Active);
		Layout();
		Hide();
	}

	public void Toggle()
	{
		if (IsOpen)
		{
			Close();
			return;
		}
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
				RebuildTabs();
				RebuildRows();
			}
			AnimateFills();
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

	private List<string> DescriptionLines(string? text, int width, int size)
	{
		List<string> list = new List<string>();
		string[] array = (text ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries);
		string text2 = string.Empty;
		string[] array2 = array;
		foreach (string text3 in array2)
		{
			string text4 = ((text2.Length == 0) ? text3 : (text2 + " " + text3));
			if (text2.Length > 0 && Measure(text4) > width)
			{
				list.Add(text2);
				text2 = text3;
				if (list.Count == 2)
				{
					break;
				}
			}
			else
			{
				text2 = text4;
			}
		}
		if (list.Count < 2)
		{
			list.Add(text2);
			return list;
		}
		string text5 = list[list.Count - 1];
		while (text5.Length > 0 && Measure(text5 + "…") > width)
		{
			int num = text5.LastIndexOf(' ');
			string text7;
			if (num <= 0)
			{
				string text6 = text5;
				text7 = text6.Substring(0, text6.Length - 1);
			}
			else
			{
				text7 = text5.Substring(0, num);
			}
			text5 = text7;
		}
		list[list.Count - 1] = text5 + "…";
		return list;
		int Measure(string value)
		{
			return (int)(Graphics.Renderer?.MeasureText(value, _font, size, 1f).X ?? 0f);
		}
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
		Base obj = _list.Parent ?? this;
		_laidOutTitleBarHeight = base.TitleBarBounds.Height;
		int num = Math.Min(obj.Height, 560 - _laidOutTitleBarHeight);
		int num2 = 56 - _laidOutTitleBarHeight;
		_title.SetSize(300, 38);
		_title.SetPosition(24, num2 - 54);
		_subtitle.SetSize(300, 20);
		_subtitle.SetPosition(26, num2 - 22);
		_close.SetSize(24, 24);
		_close.SetPosition(obj.Width - 16 - 24, num2 - 38);
		_star.SetSize(20, 20);
		_star.SetPosition(obj.Width - 16 - 24 - 14 - 20, num2 - 42);
		int num3 = _star.X - 8;
		_summary.SetSize(220, 24);
		_summary.SetPosition(num3 - 220, num2 - 46);
		_pointsTrack.SetSize(174, 10);
		_pointsTrack.SetPosition(num3 - 174, num2 - 19);
		int num4 = num2 + 16 + 8;
		int height = num - num4 - 16 - 8;
		int num5 = 216;
		_list.SetSize(obj.Width - num5 - 16, height);
		_list.SetPosition(num5, num4);
		for (int i = 0; i < _tabs.Count; i++)
		{
			_tabs[i].SetSize(168, 38);
			_tabs[i].SetPosition(24, num4 + i * 44);
		}
	}

	private void RebuildTabs()
	{
		foreach (Button tab in _tabs)
		{
			tab.Parent?.RemoveChild(tab, dispose: true);
		}
		_tabs.Clear();
		List<string> list = (from achievement in Globals.Achievements
			select achievement.Category?.Trim() ?? string.Empty into category
			where category.Length > 0
			select category).Distinct<string>(StringComparer.OrdinalIgnoreCase).OrderBy<string, string>((string category) => category, StringComparer.OrdinalIgnoreCase).ToList();
		if (!list.Any((string a) => string.Equals(a, _selectedCategory, StringComparison.OrdinalIgnoreCase)))
		{
			_selectedCategory = string.Empty;
		}
		AddTab(Strings.Achievements.AllCategories, string.Empty, Globals.Achievements.Length, Globals.Achievements.Count((AchievementProgress achievement) => achievement.Completed));
		foreach (string name in list)
		{
			AchievementProgress[] array = Globals.Achievements.Where((AchievementProgress achievement) => string.Equals(achievement.Category?.Trim(), name, StringComparison.OrdinalIgnoreCase)).ToArray();
			AddTab(name, name, array.Length, array.Count((AchievementProgress achievement) => achievement.Completed));
		}
		_summary.SetText(Strings.Achievements.PointsSummary.ToString(Globals.AchievementPointsEarned, Globals.AchievementPointsTotal));
		float num = ((Globals.AchievementPointsTotal <= 0) ? 0f : Math.Clamp((float)Globals.AchievementPointsEarned / (float)Globals.AchievementPointsTotal, 0f, 1f));
		int num2 = (int)(174f * num);
		_pointsFill.IsHidden = num2 <= 0;
		_pointsFill.SetSize(Math.Max(10, num2), 10);
		_pointsFill.SetPosition(0, 0);
		Layout();
	}

	private void AddTab(string label, string category, int total, int done)
	{
		Button button = new Button(this, "Tab_" + category)
		{
			Font = _font,
			FontSize = 11,
			Text = Strings.Achievements.TabLabel.ToString(label, done, total),
			TextAlign = Pos.Left | Pos.CenterV,
			Padding = new Padding(16, 0, 8, 0),
			UserData = category,
		};
		Button button2 = button;
		button2.Clicked += delegate
		{
			_selectedCategory = category;
			_needsRebuild = true;
		};
		StyleTab(button2, string.Equals(category, _selectedCategory, StringComparison.OrdinalIgnoreCase));
		_tabs.Add(button2);
	}

	private static void StyleTab(Button tab, bool selected)
	{
		tab.SetStateTexture(ComponentState.Normal, selected ? "saiyan_tab_selected.png" : "saiyan_tab.png");
		tab.SetStateTexture(ComponentState.Hovered, selected ? "saiyan_tab_selected.png" : "saiyan_tab_hover.png");
		tab.SetStateTexture(ComponentState.Active, "saiyan_tab_selected.png");
		tab.SetTextColor(selected ? White : Dim, ComponentState.Normal);
		tab.SetTextColor(OrangeLight, ComponentState.Hovered);
		tab.SetTextColor(OrangeLight, ComponentState.Active);
	}

	private void RebuildRows()
	{
		foreach (Base row in _rows)
		{
			row.Parent?.RemoveChild(row, dispose: true);
		}
		_rows.Clear();
		_fills.Clear();
		AchievementProgress[] array = (from achievementProgress in Globals.Achievements
			where _selectedCategory.Length == 0 || string.Equals(achievementProgress.Category?.Trim(), _selectedCategory, StringComparison.OrdinalIgnoreCase)
			orderby achievementProgress.Completed
			select achievementProgress).ThenBy<AchievementProgress, string>((AchievementProgress achievementProgress) => achievementProgress.Name, StringComparer.OrdinalIgnoreCase).ToArray();
		if (array.Length == 0)
		{
			Label label = MakeLabel(_list, "AchievementsEmpty", 11, Dim);
			label.Text = Strings.Achievements.Empty;
			label.TextAlign = Pos.Left | Pos.CenterV;
			label.SetSize(_list.Width - 16, 22);
			label.SetPosition(8, 8);
			_rows.Add(label);
			return;
		}
		int width = _list.Width - 12;
		int num = 0;
		AchievementProgress[] array2 = array;
		foreach (AchievementProgress achievement in array2)
		{
			var (item, num3) = BuildCard(achievement, num, width);
			_rows.Add(item);
			num += num3 + 8;
		}
	}

	private (Base Card, int Height) BuildCard(AchievementProgress achievement, int y, int width)
	{
		bool completed = achievement.Completed;
		ImagePanel card = new ImagePanel(_list, "Achievement_" + achievement.Id)
		{
			Texture = Globals.ContentManager.GetTexture(TextureType.Gui, completed ? "saiyan_card_done.png" : "saiyan_card.png"),
			TextureNinePatchMargin = new Margin(8)
		};
		if (!completed)
		{
			card.HoverEnter += delegate
			{
				card.Texture = Globals.ContentManager.GetTexture(TextureType.Gui, "saiyan_card_hover.png");
			};
			card.HoverLeave += delegate
			{
				card.Texture = Globals.ContentManager.GetTexture(TextureType.Gui, "saiyan_card.png");
			};
		}
		int num = width - 262;
		int num2 = BuildChips(card, achievement, num, width - 12, completed);
		int num3 = Math.Max(100, num2 + 6);
		card.SetSize(width, num3);
		card.SetPosition(0, y);
		ImagePanel imagePanel = MakeImage(card, "Medal", completed ? "saiyan_medal56_done.png" : "saiyan_medal56.png", 56, 56);
		imagePanel.SetPosition(14, (num3 - 56) / 2);
		if (!completed)
		{
			Label label = MakeLabel(imagePanel, "Points", 16, Gold, display: true);
			label.Text = achievement.Points.ToString();
			label.TextAlign = Pos.Center;
			label.SetSize(56, 38);
			label.SetPosition(0, 4);
			Label label2 = MakeLabel(imagePanel, "PointsUnit", 9, GoldSoft);
			label2.Text = Strings.Achievements.PointsShort;
			label2.TextAlign = Pos.Center;
			label2.SetSize(56, 14);
			label2.SetPosition(0, 34);
		}
		int width2 = num - 86 - 8;
		Label label3 = MakeLabel(card, "Name", 14, completed ? Gold : White, display: true);
		label3.Text = achievement.Name;
		label3.TextAlign = Pos.Left | Pos.CenterV;
		label3.SetSize(width2, 24);
		label3.SetPosition(86, 9);
		List<string> list = DescriptionLines(achievement.Description, width2, 10);
		for (int num4 = 0; num4 < list.Count; num4++)
		{
			Label label4 = MakeLabel(card, (num4 == 0) ? "Description" : $"Description{num4 + 1}", 10, Muted);
			label4.Text = list[num4];
			label4.TextAlign = Pos.Left | Pos.CenterV;
			label4.SetSize(width2, 18);
			label4.SetPosition(86, 34 + num4 * 16);
		}
		int num5 = num - 86 - 88;
		int num6 = num3 - 10 - 14;
		ImagePanel imagePanel2 = MakeImage(card, "Track", "saiyan_bar_track.png", num5, 10);
		imagePanel2.TextureNinePatchMargin = new Margin(5);
		imagePanel2.SetPosition(86, num6);
		float num7 = ((achievement.Objective <= 0) ? 0f : Math.Clamp((float)achievement.Value / (float)achievement.Objective, 0f, 1f));
		ImagePanel imagePanel3 = MakeImage(imagePanel2, "Fill", completed ? "saiyan_bar_fill_done.png" : "saiyan_bar_fill.png", 0, 10);
		imagePanel3.TextureNinePatchMargin = new Margin(5);
		imagePanel3.SetPosition(0, 0);
		int num8 = (int)((float)num5 * num7);
		imagePanel3.IsHidden = num8 <= 0;
		if (num8 > 0)
		{
			_fills.Add((imagePanel3, num8, Timing.Global.Milliseconds));
		}
		Label label5 = MakeLabel(card, "Progress", 12, completed ? Gold : White, display: true);
		label5.Text = Strings.Achievements.Progress.ToString(achievement.Value, achievement.Objective);
		label5.TextAlign = Pos.Right | Pos.CenterV;
		label5.SetSize(64, 24);
		label5.SetPosition(86 + num5 + 6, num6 + -7);
		ImagePanel imagePanel4 = MakeImage(card, "Rule", "inv_plein.png", 1, num3 - 24);
		imagePanel4.RenderColor = Rule;
		imagePanel4.SetPosition(num - 12, 12);
		Label label6 = MakeLabel(card, "RewardsHeading", 9, Muted);
		label6.Text = Strings.Achievements.Rewards.ToString().ToUpperInvariant();
		label6.TextAlign = Pos.Left | Pos.CenterV;
		label6.SetSize(150, 20);
		label6.SetPosition(num, 8);
		if (completed)
		{
			ImagePanel imagePanel5 = MakeImage(card, "Chip", "saiyan_chip_done.png", 71, 22);
			imagePanel5.TextureNinePatchMargin = new Margin(8);
			imagePanel5.SetPosition(width - 86, 10);
			Label label7 = MakeLabel(imagePanel5, "ChipText", 9, GoldSoft);
			label7.Text = Strings.Achievements.Obtained.ToString().ToUpperInvariant();
			label7.TextAlign = Pos.Center;
			label7.SetSize(71, 22);
			label7.SetPosition(0, 0);
		}
		return (Card: card, Height: num3);
	}

	private int BuildChips(ImagePanel card, AchievementProgress achievement, int left, int right, bool done)
	{
		List<RewardChip> list = RewardsOf(achievement);
		int num = left;
		int num2 = 34;
		if (list.Count == 0)
		{
			Label label = MakeLabel(card, "NoReward", 10, Dim);
			label.Text = Strings.Achievements.NoReward;
			label.TextAlign = Pos.Left | Pos.CenterV;
			label.SetSize(right - left, 26);
			label.SetPosition(left, num2);
			return num2 + 26;
		}
		for (int i = 0; i < list.Count; i++)
		{
			RewardChip rewardChip = list[i];
			ImagePanel imagePanel = new ImagePanel(card, $"Reward{i}")
			{
				Texture = Globals.ContentManager.GetTexture(TextureType.Gui, done ? "saiyan_chip_done.png" : "saiyan_chip.png"),
				TextureNinePatchMargin = new Margin(8),
				MouseInputEnabled = (rewardChip.Item != null)
			};
			int num3 = 10;
			if (rewardChip.Icon != null && rewardChip.IconWidth > 0)
			{
				ImagePanel imagePanel2 = new ImagePanel(imagePanel, "Icon")
				{
					MouseInputEnabled = false,
					Texture = rewardChip.Icon
				};
				imagePanel2.SetTextureRect(rewardChip.Source.X, rewardChip.Source.Y, rewardChip.Source.Width, rewardChip.Source.Height);
				imagePanel2.SetSize(rewardChip.IconWidth, 22);
				imagePanel2.SetPosition(8, 2);
				num3 += rewardChip.IconWidth + 5;
			}
			Label label2 = MakeLabel(imagePanel, "Text", 9, done ? GoldSoft : rewardChip.Color);
			label2.AutoSizeToContents = true;
			label2.Text = rewardChip.Text;
			label2.SizeToContents();
			int width = label2.Width;
			label2.AutoSizeToContents = false;
			label2.TextAlign = Pos.Left | Pos.CenterV;
			label2.SetSize(width + 2, 26);
			label2.SetPosition(num3, 0);
			int num4 = num3 + width + 10;
			if (num != left && num + num4 > right)
			{
				num = left;
				num2 += 31;
			}
			imagePanel.SetSize(num4, 26);
			imagePanel.SetPosition(num, num2);
			num += num4 + 5;
			ItemDescriptor item = rewardChip.Item;
			if (item == null)
			{
				continue;
			}
			int quantity = Math.Max(1, rewardChip.Quantity);
			imagePanel.HoverEnter += delegate
			{
				Interface.GameUi.ItemDescriptionWindow?.Show(item, quantity);
				if (!done)
				{
					card.Texture = Globals.ContentManager.GetTexture(TextureType.Gui, "saiyan_card_hover.png");
				}
			};
			imagePanel.HoverLeave += delegate
			{
				Interface.GameUi.ItemDescriptionWindow?.Hide();
			};
		}
		return num2 + 26;
	}

	private static List<RewardChip> RewardsOf(AchievementProgress achievement)
	{
		List<RewardChip> list = new List<RewardChip>();
		if (achievement.ExperienceReward > 0)
		{
			IGameTexture texture = Globals.ContentManager.GetTexture(TextureType.Gui, "saiyan_orbe_xp.png");
			list.Add(new RewardChip(texture, FullRect(texture), (texture != null) ? 22 : 0, Strings.Achievements.RewardExperience.ToString(MarketWindow.FormatAmount(achievement.ExperienceReward)), GoldSoft, null, 0));
		}
		Guid guid = Globals.Me?.CurrencyItemId ?? Guid.Empty;
		foreach (AchievementRewardItem item in achievement.ItemRewards ?? new List<AchievementRewardItem>())
		{
			if (!(item.ItemId == Guid.Empty) && DatabaseObject<ItemDescriptor>.TryGet(item.ItemId, out var databaseObject))
			{
				int num = Math.Max(1, item.Quantity);
				IGameTexture texture2 = Globals.ContentManager.GetTexture(TextureType.Item, databaseObject.Icon);
				bool flag = guid != Guid.Empty && databaseObject.Id == guid;
				list.Add(new RewardChip(texture2, FullRect(texture2), (texture2 != null) ? 22 : 0, flag ? Strings.Achievements.RewardZeni.ToString(MarketWindow.FormatAmount(num)) : Strings.Achievements.RewardItem.ToString(databaseObject.Name, MarketWindow.FormatAmount(num)), flag ? GoldSoft : EntryName, flag ? null : databaseObject, num));
			}
		}
		if (achievement.TitleRewardId != Guid.Empty && DatabaseObject<TitleDescriptor>.TryGet(achievement.TitleRewardId, out var databaseObject2))
		{
			list.Add(new RewardChip(null, default(Rectangle), 0, Strings.Achievements.RewardTitle.ToString(databaseObject2.Name), databaseObject2.Color ?? EntryName, null, 0));
		}
		if (achievement.OrnamentRewardId != Guid.Empty && DatabaseObject<OrnamentDescriptor>.TryGet(achievement.OrnamentRewardId, out var databaseObject3))
		{
			var (icon, source, iconWidth) = OrnamentIcon(databaseObject3.Style);
			list.Add(new RewardChip(icon, source, iconWidth, databaseObject3.Name, EntryName, null, 0));
		}
		return list;
	}

	private static Rectangle FullRect(IGameTexture? texture)
	{
		if (texture != null)
		{
			return new Rectangle(0, 0, texture.Width, texture.Height);
		}
		return default(Rectangle);
	}

	private static (IGameTexture? Texture, Rectangle Source, int Width) OrnamentIcon(string style)
	{
		string[] array = new string[4] { "sommet", "embleme", "gauche", "cadre" };
		foreach (string part in array)
		{
			IGameTexture texture = Globals.ContentManager.GetTexture(TextureType.Gui, OrnamentDescriptor.TextureName(style, part));
			if (texture != null && texture.Width > 0 && texture.Height > 0)
			{
				int num = Math.Min(texture.Width, texture.Height * 2);
				Rectangle item = new Rectangle((texture.Width - num) / 2, 0, num, texture.Height);
				int item2 = Math.Clamp((int)MathF.Round((float)(22 * num) / (float)texture.Height), 8, 44);
				return (Texture: texture, Source: item, Width: item2);
			}
		}
		return (Texture: null, Source: default(Rectangle), Width: 0);
	}

	private void AnimateFills()
	{
		if (_fills.Count == 0)
		{
			return;
		}
		long milliseconds = Timing.Global.Milliseconds;
		for (int num = _fills.Count - 1; num >= 0; num--)
		{
			(ImagePanel Fill, int TargetWidth, long StartedAt) tuple = _fills[num];
			ImagePanel item = tuple.Fill;
			int item2 = tuple.TargetWidth;
			long item3 = tuple.StartedAt;
			float num2 = Math.Clamp((float)(milliseconds - item3) / 650f, 0f, 1f);
			float num3 = 1f - MathF.Pow(1f - num2, 3f);
			item.SetSize(Math.Max(10, (int)((float)item2 * num3)), item.Height);
			if (num2 >= 1f)
			{
				_fills.RemoveAt(num);
			}
		}
	}
}
