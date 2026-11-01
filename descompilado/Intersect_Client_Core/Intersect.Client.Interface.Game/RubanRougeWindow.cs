using System;
using System.Collections.Generic;
using System.Linq;
using Intersect.Client.Framework.Content;
using Intersect.Client.Framework.File_Management;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.Framework.Gwen;
using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.General;
using Intersect.Client.Interface.Game.EntityPanel;
using Intersect.Client.Interface.Shared;
using Intersect.Client.Localization;
using Intersect.Client.Networking;
using Intersect.Config;
using Intersect.Localization;
using Intersect.Network.Packets.Server;

namespace Intersect.Client.Interface.Game;

public sealed class RubanRougeWindow : WindowControl
{
	private enum Tab
	{
		Contracts,
		Market,
		Grades,
		Rules
	}

	private const int WindowWidth = 880;

	private const int WindowHeight = 600;

	private new const int Margin = 24;

	private const int TabsY = 76;

	private const int TabWidth = 160;

	private const int TabHeight = 30;

	private const int TabGap = 6;

	private const int ContentY = 120;

	private const int ContentHeight = 422;

	private const int FooterY = 560;

	private const int CardWidth = 266;

	private const int CardHeight = 212;

	private const int CardGap = 12;

	private const int Columns = 3;

	private const int BuyWidth = 136;

	private const int PosterWidth = 266;

	private const int PosterHeight = 268;

	private const int GradeRowWidth = 812;

	private const int GradeRowHeight = 46;

	private const int LineHeight = 15;

	private const int RowWidth = 822;

	private static readonly Color White = new Color(255, 242, 242, 242);

	private static readonly Color Soft = new Color(255, 166, 177, 200);

	private static readonly Color Muted = new Color(255, 176, 186, 206);

	private static readonly Color Dim = new Color(255, 138, 150, 176);

	private static readonly Color Night = new Color(255, 11, 14, 23);

	private static readonly Color Gold = new Color(255, 255, 208, 106);

	private static readonly Color Ki = new Color(255, 255, 122, 26);

	private static readonly Color PoliceBlue = new Color(255, 108, 184, 255);

	private static readonly Color RibbonRed = new Color(255, 240, 80, 60);

	private static readonly Color Salmon = new Color(255, 255, 154, 134);

	private static readonly Color GreenLight = new Color(255, 140, 235, 170);

	private static readonly Color VioletLight = new Color(255, 214, 178, 255);

	private static readonly Color SmugglingLight = new Color(255, 255, 226, 192);

	private static readonly Color SmugglingAccent = new Color(255, 255, 180, 92);

	private static readonly Color HeadLight = new Color(255, 255, 241, 200);

	private static readonly Color SabotageLight = new Color(255, 255, 214, 190);

	private static readonly Color SabotageAccent = new Color(255, 255, 122, 70);

	private static readonly Color LockedTint = new Color(128, 255, 255, 255);

	private static RubanRougeWindow? _open;

	private readonly IFont? _body;

	private readonly IFont? _strong;

	private readonly Label _title;

	private readonly Label _subtitle;

	private readonly ImagePanel _gradeChip;

	private readonly Label _gradeName;

	private readonly Label _gradeReputation;

	private readonly ImagePanel _gradeTrack;

	private readonly ImagePanel _gradeFill;

	private readonly Label _gradeNext;

	private readonly Button _close;

	private readonly Button[] _tabs = new Button[4];

	private readonly ScrollControl _list;

	private readonly List<Base> _items = new List<Base>();

	private readonly Label _footerLeft;

	private readonly Label _footerBounty;

	private RubanRougePacket _state;

	private Tab _tab;

	private bool _needsRebuild = true;

	private bool _finished;

	private int _laidOutTitleBarHeight = -1;

	public static RubanRougeWindow? Current
	{
		get
		{
			RubanRougeWindow open = _open;
			if (open == null || open.IsHidden || open._finished)
			{
				return null;
			}
			return open;
		}
	}

	private int Grade => Options.Instance.Karma.RibbonGradeOf(Math.Max(0, _state.Reputation));

	private RubanRougeWindow(RubanRougePacket state)
		: base(Interface.CurrentInterface.Root, string.Empty, modal: true, "RubanRougeWindow")
	{
		_state = state;
		GameContentManager current = GameContentManager.Current;
		_body = current.GetFont("chakrapetch");
		_strong = current.GetFont("rajdhanibold");
		Interface.InputBlockingComponents.Add(this);
		base.DeleteOnClose = true;
		DisableResizing();
		base.IsClosable = false;
		base.Titlebar.ShouldDrawBackground = false;
		base.TitleLabel.IsHidden = true;
		SetSize(880, 600);
		IGameTexture texture = current.GetTexture(TextureType.Gui, "saiyan_ruban.png");
		SetImage(texture, "saiyan_ruban.png", ControlState.Active);
		SetImage(texture, "saiyan_ruban.png", ControlState.Inactive);
		_title = WantedBoardWindow.MakeLabel(this, "Title", 20, White, current.GetFont("refuel"));
		_title.Text = Strings.RubanRouge.Title.ToString().ToUpperInvariant();
		WindowTitle.Apply(_title);
		_subtitle = WantedBoardWindow.MakeLabel(this, "Subtitle", 10, Dim, _body);
		_gradeChip = new ImagePanel(this, "Grade")
		{
			MouseInputEnabled = false,
			Texture = current.GetTexture(TextureType.Gui, "panneau_grade.png")
		};
		_gradeName = WantedBoardWindow.MakeLabel(_gradeChip, "GradeName", 14, Salmon, _strong);
		_gradeReputation = WantedBoardWindow.MakeLabel(_gradeChip, "GradeReputation", 12, Dim, _strong);
		_gradeReputation.TextAlign = Pos.Right | Pos.CenterV;
		_gradeTrack = new ImagePanel(_gradeChip, "GradeTrack")
		{
			MouseInputEnabled = false,
			Texture = current.GetTexture(TextureType.Gui, "panneau_grade_piste.png")
		};
		_gradeFill = new ImagePanel(_gradeChip, "GradeFill")
		{
			MouseInputEnabled = false,
			Texture = current.GetTexture(TextureType.Gui, "ruban_grade_jauge.png")
		};
		_gradeNext = WantedBoardWindow.MakeLabel(_gradeChip, "GradeNext", 8, Dim, _body);
		_close = new Button(this, "Close", disableText: true);
		_close.SetStateTexture(ComponentState.Normal, "saiyan_close.png");
		_close.SetStateTexture(ComponentState.Hovered, "saiyan_close_hover.png");
		_close.SetStateTexture(ComponentState.Active, "saiyan_close_clicked.png");
		_close.Clicked += delegate
		{
			CloseWindow();
		};
		Tab[] values = Enum.GetValues<Tab>();
		foreach (Tab tab in values)
		{
			Button button = new Button(this, $"Tab_{tab}")
			{
				Font = _body,
				FontSize = 11,
				TextAlign = Pos.Center,
				UsesHeadingFace = false
			};
			button.SetSize(160, 30);
			button.Clicked += delegate
			{
				Select(tab);
			};
			_tabs[(int)tab] = button;
		}
		_list = new ScrollControl(this, "RubanRougeContent");
		_list.EnableScroll(horizontal: false, vertical: true);
		_list.VerticalScrollBar.SetBackgroundTemplate(null, "saiyan_scroll_bg.png");
		_list.VerticalScrollBar.SetScrollBarImage(null, "saiyan_scroll_bar.png", ComponentState.Normal);
		_list.VerticalScrollBar.SetScrollBarImage(null, "saiyan_scroll_bar_hover.png", ComponentState.Hovered);
		_list.VerticalScrollBar.SetScrollBarImage(null, "saiyan_scroll_bar_hover.png", ComponentState.Active);
		_footerLeft = WantedBoardWindow.MakeLabel(this, "FooterLeft", 10, Muted, _body);
		_footerBounty = WantedBoardWindow.MakeLabel(this, "FooterBounty", 10, Muted, _body);
		_footerBounty.TextAlign = Pos.Right | Pos.CenterV;
		Layout();
		SetPosition(Math.Max(0, (Interface.CurrentInterface.Root.Width - 880) / 2), Math.Max(0, (Interface.CurrentInterface.Root.Height - 600) / 2));
		RefreshHeader();
		Show();
		BringToFront();
	}

	public static void Open(RubanRougePacket state)
	{
		RubanRougeWindow current = Current;
		if (current != null)
		{
			current._state = state;
			current._needsRebuild = true;
			current.RefreshHeader();
		}
		else
		{
			_open?.CloseWindow();
			_open = new RubanRougeWindow(state);
		}
	}

	public void CloseWindow()
	{
		if (!_finished)
		{
			_finished = true;
			if (_open == this)
			{
				_open = null;
			}
			Interface.InputBlockingComponents.Remove(this);
			Close();
		}
	}

	protected override void Dispose(bool disposing)
	{
		Interface.InputBlockingComponents.Remove(this);
		if (_open == this)
		{
			_open = null;
		}
		base.Dispose(disposing);
	}

	public override void Think()
	{
		base.Think();
		if (!_finished && !base.IsHidden)
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
		}
	}

	private void Select(Tab tab)
	{
		if (tab != _tab)
		{
			_tab = tab;
			RefreshHeader();
			Rebuild(keepScroll: false);
		}
	}

	private void RefreshHeader()
	{
		string text = _state.Place?.Trim() ?? string.Empty;
		_subtitle.Text = ((text.Length > 0) ? Strings.RubanRouge.TraffickerIn.ToString(text) : Strings.RubanRouge.Trafficker.ToString());
		int[] array = KarmaOptions.ParseList(Options.Instance.Karma.RibbonGradeThresholds);
		int num = Math.Max(0, _state.Reputation);
		int grade = Grade;
		_gradeName.Text = GradeName(grade).ToUpperInvariant();
		if (grade + 1 < array.Length)
		{
			int num2 = array[Math.Clamp(grade, 0, array.Length - 1)];
			int num3 = array[grade + 1];
			_gradeReputation.Text = Strings.RubanRouge.Reputation.ToString(SaiyanHud.Amount(num), SaiyanHud.Amount(num3));
			_gradeNext.Text = Strings.RubanRouge.NextGrade.ToString(GradeName(grade + 1), SaiyanHud.Amount(num3));
			float num4 = ((num3 > num2) ? Math.Clamp((float)(num - num2) / (float)(num3 - num2), 0f, 1f) : 1f);
			_gradeFill.SetSize(Math.Max(0, (int)(196f * num4)), 4);
		}
		else
		{
			_gradeReputation.Text = SaiyanHud.Amount(num);
			_gradeNext.Text = Strings.RubanRouge.TopGrade;
			_gradeFill.SetSize(196, 4);
		}
		_gradeFill.IsHidden = _gradeFill.Width <= 0;
		int num5 = (_state.Contracts ?? Array.Empty<RibbonContractEntryPacket>()).Count((RibbonContractEntryPacket contract) => !contract.Done);
		_tabs[0].Text = ((num5 > 0) ? Strings.RubanRouge.TabContractsCount.ToString(num5) : Strings.RubanRouge.TabContracts.ToString());
		_tabs[1].Text = Strings.RubanRouge.TabMarket;
		_tabs[2].Text = Strings.RubanRouge.TabGrades;
		_tabs[3].Text = Strings.RubanRouge.TabRules;
		Tab[] values = Enum.GetValues<Tab>();
		foreach (Tab tab in values)
		{
			Button button = _tabs[(int)tab];
			bool flag = tab == _tab;
			button.SetStateTexture(ComponentState.Normal, flag ? "panneau_onglet_actif.png" : "panneau_onglet.png");
			button.SetStateTexture(ComponentState.Hovered, flag ? "panneau_onglet_actif.png" : "panneau_onglet_survol.png");
			button.SetStateTexture(ComponentState.Active, flag ? "panneau_onglet_actif.png" : "panneau_onglet_survol.png");
			button.SetTextColor(flag ? Night : Soft, ComponentState.Normal);
			button.SetTextColor(flag ? Night : White, ComponentState.Hovered);
			button.SetTextColor(flag ? Night : White, ComponentState.Active);
		}
		_footerLeft.Text = Strings.RubanRouge.FooterCount.ToString(Math.Max(0, _state.Held), Math.Max(1, _state.MaxHeld), Math.Max(0, _state.TakenToday), Math.Max(1, _state.PerDay));
		_footerBounty.Text = ((_state.Bounty > 0) ? Strings.RubanRouge.Bounty.ToString(SaiyanHud.Amount(_state.Bounty)) : Strings.RubanRouge.NoBounty.ToString());
		_footerBounty.SetTextColor((_state.Bounty > 0) ? Gold : Dim, ComponentState.Normal);
	}

	internal static string GradeName(int grade)
	{
		return grade switch
		{
			0 => Strings.RubanRouge.Grade0, 
			1 => Strings.RubanRouge.Grade1, 
			2 => Strings.RubanRouge.Grade2, 
			3 => Strings.RubanRouge.Grade3, 
			4 => Strings.RubanRouge.Grade4, 
			_ => Strings.RubanRouge.Grade5, 
		};
	}

	private static string GradeOpens(int grade)
	{
		return grade switch
		{
			0 => Strings.RubanRouge.Opens0, 
			1 => Strings.RubanRouge.Opens1, 
			2 => Strings.RubanRouge.Opens2, 
			3 => Strings.RubanRouge.Opens3, 
			4 => Strings.RubanRouge.Opens4, 
			_ => Strings.RubanRouge.Opens5, 
		};
	}

	private void Rebuild(bool keepScroll = true)
	{
		int verticalScroll = (keepScroll ? _list.VerticalScroll : 0);
		foreach (Base item in _items)
		{
			item.Parent?.RemoveChild(item, dispose: true);
		}
		_items.Clear();
		switch (_tab)
		{
		case Tab.Contracts:
			BuildContracts();
			break;
		case Tab.Market:
			BuildMarket();
			break;
		case Tab.Grades:
			BuildGrades();
			break;
		default:
			BuildRules();
			break;
		}
		_list.VerticalScroll = verticalScroll;
	}

	private void AddEmpty(string text)
	{
		Label label = WantedBoardWindow.MakeLabel(_list, "Empty", 11, Dim, _body);
		label.Text = text;
		label.SetSize(822, 24);
		label.SetPosition(4, 6);
		_items.Add(label);
	}

	private void BuildContracts()
	{
		RibbonContractEntryPacket[] array = _state.Contracts ?? Array.Empty<RibbonContractEntryPacket>();
		if (array.Length == 0)
		{
			AddEmpty(Strings.RubanRouge.NoContractYet);
			return;
		}
		for (int i = 0; i < array.Length; i++)
		{
			int x = i % 3 * 278;
			int y = i / 3 * 280;
			_items.Add(BuildContractPoster(array[i], x, y));
		}
	}

	private Base BuildContractPoster(RibbonContractEntryPacket entry, int x, int y)
	{
		GameContentManager current = GameContentManager.Current;
		bool flag = entry.Kind == RibbonContractKind.Smuggling;
		bool flag2 = entry.Kind == RibbonContractKind.Sabotage;
		bool flag3 = Grade < entry.Grade;
		ImagePanel imagePanel = new ImagePanel(_list, "Contract")
		{
			Texture = current.GetTexture(TextureType.Gui, flag ? "ruban_affiche_contrebande.png" : (flag2 ? "ruban_affiche_sabotage.png" : "ruban_affiche_heros.png"))
		};
		imagePanel.SetBounds(x, y, 266, 268);
		string text = entry.Kind switch
		{
			RibbonContractKind.Smuggling => Strings.RubanRouge.KindSmuggling.ToString(), 
			RibbonContractKind.HeroHead => Strings.RubanRouge.KindHead.ToString(), 
			_ => Strings.RubanRouge.KindSabotage.ToString(), 
		};
		Label label = WantedBoardWindow.MakeLabel(imagePanel, "Banner", 13, flag ? SmugglingLight : (flag2 ? SabotageLight : HeadLight), _strong);
		label.Text = text.ToUpperInvariant();
		label.SetBounds(10, 0, 150, 26);
		Label label2 = WantedBoardWindow.MakeLabel(imagePanel, "BannerRight", 8, flag ? SmugglingAccent : (flag2 ? SabotageAccent : Gold), _body);
		label2.Text = Strings.RubanRouge.GradeTag.ToString(GradeName(entry.Grade)).ToUpperInvariant();
		label2.TextAlign = Pos.Right | Pos.CenterV;
		label2.SetBounds(126, 0, 130, 26);
		Color renderColor = (flag3 ? LockedTint : SaiyanHud.White);
		if (flag)
		{
			IGameTexture gameTexture = (string.IsNullOrWhiteSpace(entry.Icon) ? null : current.GetTexture(TextureType.Item, entry.Icon));
			IGameTexture gameTexture2 = (string.IsNullOrWhiteSpace(entry.Icon2) ? null : current.GetTexture(TextureType.Item, entry.Icon2));
			if (gameTexture != null && gameTexture2 != null)
			{
				ImagePanel imagePanel2 = new ImagePanel(imagePanel, "Goods")
				{
					MouseInputEnabled = false,
					Texture = gameTexture,
					RenderColor = renderColor
				};
				imagePanel2.SetBounds(16, 54, 48, 48);
				ImagePanel imagePanel3 = new ImagePanel(imagePanel, "Goods2")
				{
					MouseInputEnabled = false,
					Texture = gameTexture2,
					RenderColor = renderColor
				};
				imagePanel3.SetBounds(40, 54, 48, 48);
			}
			else
			{
				IGameTexture gameTexture3 = gameTexture ?? gameTexture2;
				if (gameTexture3 != null)
				{
					ImagePanel imagePanel4 = new ImagePanel(imagePanel, "Goods")
					{
						MouseInputEnabled = false,
						Texture = gameTexture3,
						RenderColor = renderColor
					};
					imagePanel4.SetBounds(20, 46, 64, 64);
				}
			}
		}
		else if (!flag2)
		{
			WantedBoardWindow.PosterPortrait posterPortrait = new WantedBoardWindow.PosterPortrait(imagePanel, "Portrait")
			{
				Sprite = entry.Sprite,
				Face = entry.Face,
				Tint = ((entry.Online && !flag3) ? SaiyanHud.White : LockedTint)
			};
			posterPortrait.SetBounds(14, 40, 76, 76);
		}
		Label label3 = WantedBoardWindow.MakeLabel(imagePanel, "Name", 15, flag3 ? Dim : White, _strong);
		label3.Text = entry.Name;
		label3.SetBounds(102, 36, 154, 22);
		string text2 = WantedPlaces.ZoneOf(entry.MapId);
		object obj;
		if (text2.Length <= 0)
		{
			string text3 = entry.Place?.Trim();
			obj = ((text3 != null && text3.Length > 0) ? text3 : Strings.WantedBoard.UnknownPlace.ToString());
		}
		else
		{
			obj = text2;
		}
		string text4 = (string)obj;
		string text5 = entry.Place?.Trim();
		string text6 = ((text5 != null && text5.Length > 0) ? text5 : text4);
		Label label4 = WantedBoardWindow.MakeLabel(imagePanel, "Who", 9, Dim, _body);
		label4.Text = (flag ? Strings.RubanRouge.SmugglingWho.ToString(text4) : (flag2 ? Strings.Recherche.SabotageQui.ToString(text6) : Strings.RubanRouge.HeadWho.ToString(entry.Level)));
		label4.SetBounds(102, 58, 154, 18);
		ImagePanel imagePanel5 = new ImagePanel(imagePanel, "Coin")
		{
			MouseInputEnabled = false,
			Texture = current.GetTexture(TextureType.Gui, "saiyan_echange_coin.png")
		};
		imagePanel5.SetBounds(102, 101, 12, 12);
		string text7 = SaiyanHud.Amount(Math.Max(0L, entry.Zeni));
		Label label5 = WantedBoardWindow.MakeLabel(imagePanel, "Amount", 20, flag3 ? Dim : Gold, _strong);
		label5.Text = text7;
		int num = WantedBoardWindow.Measure(_strong, 20, text7) + 4;
		label5.SetBounds(118, 92, num, 30);
		string text8 = Strings.RubanRouge.RepChip.ToString(Math.Max(0, entry.Reputation));
		int num2 = Math.Min(256 - (118 + num + 6), WantedBoardWindow.Measure(_body, 8, text8) + 12);
		if (num2 > 20)
		{
			ImagePanel imagePanel6 = new ImagePanel(imagePanel, "Rep")
			{
				MouseInputEnabled = false,
				Texture = current.GetTexture(TextureType.Gui, "ruban_puce_rep.png"),
				TextureNinePatchMargin = new Margin(3)
			};
			imagePanel6.SetBounds(118 + num + 6, 98, num2, 18);
			Label label6 = WantedBoardWindow.MakeLabel(imagePanel6, "RepText", 8, flag3 ? Dim : Salmon, _body);
			label6.Text = text8;
			label6.TextAlign = Pos.Center;
			label6.SetBounds(0, 0, num2, 18);
		}
		List<(string, Color)> list = new List<(string, Color)>();
		if (flag)
		{
			list.Add((Strings.RubanRouge.SmugglingArrival.ToString(text4), Soft));
			list.Add((Strings.RubanRouge.SmugglingNoFly.ToString(), Soft));
			list.Add((Strings.RubanRouge.SmugglingSeized.ToString(), Dim));
		}
		else if (flag2)
		{
			list.Add((Strings.Recherche.SabotageLigne1.ToString(), Soft));
			list.Add((Strings.Recherche.SabotageLigne2.ToString(), Soft));
			list.Add((Strings.Recherche.SabotageLigne3.ToString(Math.Clamp(Options.Instance.Karma.RibbonSabotageBountyPercent, 0, 100)), Dim));
		}
		else
		{
			if (entry.MapId != Guid.Empty && entry.Minutes >= 0)
			{
				string text9 = ((text2.Length > 0) ? text2 : Strings.WantedBoard.UnknownPlace.ToString());
				list.Add(((entry.Minutes == 0) ? Strings.RubanRouge.HeadSeenNow.ToString(text9) : Strings.RubanRouge.HeadSeen.ToString(text9, WantedPlaces.Elapsed(entry.Minutes)), Soft));
			}
			List<(string, Color)> list2 = list;
			int criminalsDown = entry.CriminalsDown;
			string item = ((criminalsDown <= 0) ? Strings.RubanRouge.HeadDownNone.ToString() : ((criminalsDown != 1) ? Strings.RubanRouge.HeadDownMany.ToString(entry.CriminalsDown) : Strings.RubanRouge.HeadDownOne.ToString()));
			list2.Add((item, Soft));
			list.Add((Strings.RubanRouge.HeadGap.ToString(Globals.Me?.Level ?? 1, Math.Max(0, Options.Instance.Karma.RibbonHeroLevelGap)), Soft));
			list.Add((Strings.RubanRouge.HeadWarned.ToString(), Dim));
			if (!entry.Online && !entry.Done)
			{
				list.Add((Strings.RubanRouge.HeadOffline.ToString(), Salmon));
			}
		}
		int num3 = 124;
		foreach (var (text10, color) in list)
		{
			foreach (string item2 in WantedBoardWindow.Wrap(text10, 240, 9, _body))
			{
				Label label7 = WantedBoardWindow.MakeLabel(imagePanel, "Line", 9, flag3 ? Dim : color, _body);
				label7.Text = item2;
				label7.SetBounds(14, num3, 240, 17);
				num3 += 15;
			}
			num3 += 2;
		}
		ContractButton(imagePanel, entry, flag3);
		return imagePanel;
	}

	private void ContractButton(Base poster, RibbonContractEntryPacket entry, bool locked)
	{
		Button button = new Button(poster, "ContractButton")
		{
			Font = _body,
			FontSize = 10,
			TextAlign = Pos.Center,
			UsesHeadingFace = false
		};
		button.SetBounds(104, 226, 150, 30);
		string key = entry.Key;
		if (entry.Taken)
		{
			button.SetStateTexture(ComponentState.Normal, "panneau_pris_150x30.png");
			button.SetStateTexture(ComponentState.Hovered, "panneau_pris_150x30_hover.png");
			button.SetStateTexture(ComponentState.Active, "panneau_pris_150x30_hover.png");
			button.Text = Strings.RubanRouge.Taken;
			button.SetTextColor(Gold, ComponentState.Normal);
			button.SetTextColor(Salmon, ComponentState.Hovered);
			button.SetTextColor(Salmon, ComponentState.Active);
			button.HoverEnter += delegate
			{
				button.Text = Strings.RubanRouge.Abandon;
			};
			button.HoverLeave += delegate
			{
				button.Text = Strings.RubanRouge.Taken;
			};
			button.Clicked += delegate
			{
				button.IsDisabled = true;
				PacketSender.SendRibbonContract(key, take: false);
			};
			return;
		}
		bool flag = _state.Held > 0;
		bool flag2 = _state.TakenToday >= Math.Max(1, _state.PerDay);
		bool flag3 = entry.Kind == RibbonContractKind.HeroHead && !entry.Online;
		string text = (entry.Done ? Strings.RubanRouge.DoneToday : (locked ? ((LocalizedString)Strings.RubanRouge.GradeLocked.ToString(GradeName(entry.Grade))) : (flag ? Strings.RubanRouge.OneAtATime : (flag2 ? Strings.RubanRouge.NoMoreToday : (flag3 ? Strings.WantedBoard.Offline : null)))));
		button.SetStateTexture(ComponentState.Normal, "panneau_cta_150x30.png");
		button.SetStateTexture(ComponentState.Hovered, "panneau_cta_150x30_hover.png");
		button.SetStateTexture(ComponentState.Active, "panneau_cta_150x30_clicked.png");
		button.SetStateTexture(ComponentState.Disabled, "panneau_cta_150x30_disabled.png");
		button.SetTextColor(Night, ComponentState.Normal);
		button.SetTextColor(Night, ComponentState.Hovered);
		button.SetTextColor(Night, ComponentState.Active);
		button.SetTextColor(Dim, ComponentState.Disabled);
		button.Text = text ?? ((string)Strings.RubanRouge.Take);
		button.IsDisabled = text != null;
		button.Clicked += delegate
		{
			if (!button.IsDisabled)
			{
				button.IsDisabled = true;
				PacketSender.SendRibbonContract(key, take: true);
			}
		};
	}

	private void BuildMarket()
	{
		Label label = WantedBoardWindow.MakeLabel(_list, "Purse", 10, Dim, _body);
		label.Text = Strings.RubanRouge.Purse;
		int num = WantedBoardWindow.Measure(_body, 10, label.Text) + 8;
		label.SetBounds(0, 0, num, 26);
		_items.Add(label);
		Label label2 = WantedBoardWindow.MakeLabel(_list, "PurseAmount", 16, Gold, _strong);
		label2.Text = Strings.RubanRouge.ZeniShort.ToString(SaiyanHud.Amount(Math.Max(0L, _state.Zeni)));
		label2.SetBounds(num, 0, 822 - num, 26);
		_items.Add(label2);
		BlackMarketArticlePacket[] array = _state.Articles ?? Array.Empty<BlackMarketArticlePacket>();
		for (int i = 0; i < array.Length; i++)
		{
			int x = i % 3 * 278;
			int y = 36 + i / 3 * 224;
			_items.Add(BuildArticle(array[i], x, y));
		}
	}

	private Base BuildArticle(BlackMarketArticlePacket article, int x, int y)
	{
		GameContentManager current = GameContentManager.Current;
		bool flag = Grade < article.Grade;
		ImagePanel imagePanel = new ImagePanel(_list, "Article")
		{
			Texture = current.GetTexture(TextureType.Gui, "ruban_article.png")
		};
		imagePanel.SetBounds(x, y, 266, 212);
		if (!string.IsNullOrWhiteSpace(article.Icon))
		{
			IGameTexture texture = current.GetTexture(TextureType.Item, article.Icon);
			if (texture != null)
			{
				ImagePanel imagePanel2 = new ImagePanel(imagePanel, "Icon")
				{
					MouseInputEnabled = false,
					Texture = texture,
					RenderColor = (flag ? LockedTint : SaiyanHud.White)
				};
				imagePanel2.SetBounds(13, 15, 64, 64);
			}
		}
		Label label = WantedBoardWindow.MakeLabel(imagePanel, "Name", 14, flag ? Dim : White, _strong);
		label.Text = article.Name;
		label.SetBounds(90, 12, 164, 22);
		string text = GradeName(article.Grade).ToUpperInvariant();
		int width = WantedBoardWindow.Measure(_body, 8, text) + 14;
		ImagePanel imagePanel3 = new ImagePanel(imagePanel, "GradeChip")
		{
			MouseInputEnabled = false,
			Texture = current.GetTexture(TextureType.Gui, "ruban_puce_grade.png"),
			TextureNinePatchMargin = new Margin(3)
		};
		imagePanel3.SetBounds(90, 38, width, 18);
		Label label2 = WantedBoardWindow.MakeLabel(imagePanel3, "GradeChipText", 8, VioletLight, _body);
		label2.Text = text;
		label2.TextAlign = Pos.Center;
		label2.SetBounds(0, 0, width, 18);
		Label label3 = WantedBoardWindow.MakeLabel(imagePanel, "Duration", 9, Dim, _body);
		label3.Text = DurationOf(article.Article);
		label3.SetBounds(90, 60, 164, 18);
		int num = 90;
		foreach (string item in WantedBoardWindow.Wrap(EffectOf(article.Article), 242, 9, _body))
		{
			Label label4 = WantedBoardWindow.MakeLabel(imagePanel, "Effect", 9, flag ? Dim : Soft, _body);
			label4.Text = item;
			label4.SetBounds(12, num, 242, 17);
			num += 15;
		}
		if (article.Carried > 0)
		{
			Label label5 = WantedBoardWindow.MakeLabel(imagePanel, "Carried", 9, GreenLight, _body);
			label5.Text = Strings.RubanRouge.Carried.ToString(article.Carried);
			label5.SetBounds(12, num + 4, 242, 17);
		}
		int num2 = 170;
		ImagePanel imagePanel4 = new ImagePanel(imagePanel, "Coin")
		{
			MouseInputEnabled = false,
			Texture = current.GetTexture(TextureType.Gui, "saiyan_echange_coin.png")
		};
		imagePanel4.SetBounds(12, num2 + 9, 12, 12);
		string text2 = SaiyanHud.Amount(Math.Max(0L, article.Price));
		Label label6 = WantedBoardWindow.MakeLabel(imagePanel, "Price", 18, Gold, _strong);
		label6.Text = text2;
		int num3 = WantedBoardWindow.Measure(_strong, 18, text2) + 4;
		label6.SetBounds(28, num2, num3, 30);
		Label label7 = WantedBoardWindow.MakeLabel(imagePanel, "Unit", 8, Dim, _body);
		label7.Text = Strings.RubanRouge.Unit;
		label7.SetBounds(28 + num3 + 2, num2 + 4, Math.Max(0, 90 - num3 - 4), 24);
		bool flag2 = article.ItemId != Guid.Empty;
		bool flag3 = _state.Zeni >= article.Price;
		Button button = new Button(imagePanel, "Buy")
		{
			Font = _body,
			FontSize = 10,
			TextAlign = Pos.Center,
			UsesHeadingFace = false
		};
		button.SetBounds(118, num2, 136, 30);
		button.SetStateTexture(ComponentState.Normal, "ruban_cta_136x30.png");
		button.SetStateTexture(ComponentState.Hovered, "ruban_cta_136x30_hover.png");
		button.SetStateTexture(ComponentState.Active, "ruban_cta_136x30_clicked.png");
		button.SetStateTexture(ComponentState.Disabled, "ruban_cta_136x30_disabled.png");
		button.SetTextColor(Night, ComponentState.Normal);
		button.SetTextColor(Night, ComponentState.Hovered);
		button.SetTextColor(Night, ComponentState.Active);
		button.SetTextColor(Dim, ComponentState.Disabled);
		button.Text = ((!flag2) ? Strings.RubanRouge.Unavailable : (flag ? ((LocalizedString)Strings.RubanRouge.GradeRequired.ToString(GradeName(article.Grade))) : ((!flag3) ? Strings.RubanRouge.NoZeni : Strings.RubanRouge.Buy)));
		button.IsDisabled = !flag2 || flag || !flag3;
		button.Clicked += delegate
		{
			if (!button.IsDisabled)
			{
				button.IsDisabled = true;
				PacketSender.SendBlackMarketBuy(article.Article);
			}
		};
		return imagePanel;
	}

	private static string EffectOf(BlackMarketArticle article)
	{
		return article switch
		{
			BlackMarketArticle.SmokeBomb => Strings.RubanRouge.SmokeBombEffect, 
			BlackMarketArticle.Disguise => Strings.RubanRouge.DisguiseEffect, 
			_ => Strings.RubanRouge.JammerEffect, 
		};
	}

	private static string DurationOf(BlackMarketArticle article)
	{
		KarmaOptions karma = Options.Instance.Karma;
		return article switch
		{
			BlackMarketArticle.SmokeBomb => Strings.RubanRouge.UsedUp, 
			BlackMarketArticle.Disguise => Strings.RubanRouge.Minutes.ToString(Math.Max(1, (karma.DisguiseSeconds + 30) / 60)), 
			_ => Strings.RubanRouge.Minutes.ToString(Math.Max(1, (karma.JammerSeconds + 30) / 60)), 
		};
	}

	private void BuildGrades()
	{
		GameContentManager current = GameContentManager.Current;
		int[] array = KarmaOptions.ParseList(Options.Instance.Karma.RibbonGradeThresholds);
		int grade = Grade;
		int num = 0;
		for (int i = 0; i < array.Length; i++)
		{
			string text = ((i < grade) ? "fait" : ((i == grade) ? "actuel" : "futur"));
			bool flag = i > grade;
			ImagePanel imagePanel = new ImagePanel(_list, $"Grade_{i}")
			{
				MouseInputEnabled = false,
				Texture = current.GetTexture(TextureType.Gui, "ruban_grade_ligne_" + text + ".png")
			};
			imagePanel.SetBounds(0, num, 812, 46);
			_items.Add(imagePanel);
			string text2 = GradeName(i).ToUpperInvariant();
			Label label = WantedBoardWindow.MakeLabel(imagePanel, "Name", 15, flag ? Dim : ((i == grade) ? Salmon : White), _strong);
			label.Text = text2;
			label.SetBounds(16, 0, 170, 46);
			if (i == grade)
			{
				int x = 16 + WantedBoardWindow.Measure(_strong, 15, text2) + 10;
				ImagePanel imagePanel2 = new ImagePanel(imagePanel, "You")
				{
					MouseInputEnabled = false,
					Texture = current.GetTexture(TextureType.Gui, "ruban_toi.png")
				};
				imagePanel2.SetBounds(x, 15, 34, 16);
				Label label2 = WantedBoardWindow.MakeLabel(imagePanel2, "YouText", 8, Night, _body);
				label2.Text = Strings.RubanRouge.You;
				label2.TextAlign = Pos.Center;
				label2.SetBounds(0, 0, 34, 16);
			}
			Label label3 = WantedBoardWindow.MakeLabel(imagePanel, "Threshold", 14, flag ? Dim : Soft, _strong);
			label3.Text = SaiyanHud.Amount(array[i]);
			label3.TextAlign = Pos.Right | Pos.CenterV;
			label3.SetBounds(232, 0, 64, 46);
			string text3 = WantedBoardWindow.TitleNameOf(GradeTitles.RibbonTitle(i));
			Label label4 = WantedBoardWindow.MakeLabel(imagePanel, "Opens", 10, flag ? Dim : Soft, _body);
			label4.Text = GradeOpens(i);
			label4.SetBounds(318, (text3.Length > 0) ? 3 : 0, 478, (text3.Length > 0) ? 22 : 46);
			if (text3.Length > 0)
			{
				Label label5 = WantedBoardWindow.MakeLabel(imagePanel, "Title", 9, flag ? Dim : Gold, _body);
				label5.Text = Strings.WantedBoard.GradeTitle.ToString(text3);
				label5.SetBounds(318, 24, 478, 18);
			}
			num += 52;
		}
		num += 4;
		Label label6 = WantedBoardWindow.MakeLabel(_list, "GradesNote", 10, Dim, _body);
		label6.Text = Strings.RubanRouge.GradesNote;
		label6.SetBounds(0, num, 812, 20);
		_items.Add(label6);
	}

	private void BuildRules()
	{
		KarmaOptions karma = Options.Instance.Karma;
		int width = 405;
		int num = 0;
		num += RulesRow(num, width, (Key: "rouge", Color: RibbonRed, Title: Strings.RubanRouge.RulesContracts, Rules: new string[5]
		{
			Strings.RubanRouge.RuleOutlaws,
			Strings.RubanRouge.RuleDaily.ToString(Math.Max(1, karma.RibbonMaxContracts), Math.Max(1, karma.RibbonContractsPerDay)),
			Strings.RubanRouge.RuleSabotage,
			Strings.RubanRouge.RuleSmuggling,
			Strings.RubanRouge.RuleHero.ToString(Math.Max(0, karma.RibbonHeroLevelGap))
		}), (Key: "or", Color: Gold, Title: Strings.RubanRouge.RulesPay, Rules: new string[4]
		{
			Strings.RubanRouge.RuleZeniOnly,
			Strings.RubanRouge.RuleMore,
			Strings.RubanRouge.RuleShare,
			Strings.RubanRouge.RuleSpend
		}));
		RulesRow(num, width, (Key: "bleu", Color: PoliceBlue, Title: Strings.RubanRouge.RulesCaught, Rules: new string[3]
		{
			Strings.RubanRouge.RuleHunted,
			Strings.RubanRouge.RuleArrested.ToString(Math.Max(0, karma.RibbonArrestReputationLoss)),
			Strings.RubanRouge.RuleSeized
		}), (Key: "ki", Color: Ki, Title: Strings.RubanRouge.RulesGrades, Rules: new string[2]
		{
			Strings.RubanRouge.RuleRise,
			Strings.RubanRouge.RuleGradeGives
		}));
	}

	private int RulesRow(int y, int width, (string Key, Color Color, string Title, string[] Rules) left, (string Key, Color Color, string Title, string[] Rules) right)
	{
		int num = Math.Max(BlockHeight(width, left.Rules), BlockHeight(width, right.Rules));
		RuleBlock(0, y, width, left.Key, left.Color, left.Title, left.Rules, num);
		RuleBlock(width + 12, y, width, right.Key, right.Color, right.Title, right.Rules, num);
		return num + 12;
	}

	private int BlockHeight(int width, string[] rules)
	{
		return 40 + rules.Sum((string rule) => WantedBoardWindow.Wrap(rule, width - 28 - 14, 10, _body).Count * 16 + 6) + 4;
	}

	private void RuleBlock(int x, int y, int width, string key, Color color, string title, string[] rules, int height)
	{
		GameContentManager current = GameContentManager.Current;
		ImagePanel imagePanel = new ImagePanel(_list, "Rules_" + key)
		{
			MouseInputEnabled = false,
			Texture = current.GetTexture(TextureType.Gui, "panneau_bloc_" + key + ".png"),
			TextureNinePatchMargin = new Margin(8)
		};
		imagePanel.SetBounds(x, y, width, Math.Max(height, BlockHeight(width, rules)));
		_items.Add(imagePanel);
		Label label = WantedBoardWindow.MakeLabel(imagePanel, "Title", 14, color, _strong);
		label.Text = title.ToUpperInvariant();
		label.SetBounds(14, 8, width - 28, 24);
		int num = 38;
		foreach (string text in rules)
		{
			ImagePanel imagePanel2 = new ImagePanel(imagePanel, "Diamond")
			{
				MouseInputEnabled = false,
				Texture = current.GetTexture(TextureType.Gui, "panneau_losange_" + key + ".png")
			};
			imagePanel2.SetBounds(14, num + 5, 7, 7);
			foreach (string item in WantedBoardWindow.Wrap(text, width - 28 - 14, 10, _body))
			{
				Label label2 = WantedBoardWindow.MakeLabel(imagePanel, "Rule", 10, Soft, _body);
				label2.Text = item;
				label2.SetBounds(28, num - 1, width - 28 - 14, 18);
				num += 16;
			}
			num += 6;
		}
	}

	private int FrameY(int windowY)
	{
		return windowY - _laidOutTitleBarHeight;
	}

	private void Layout()
	{
		_laidOutTitleBarHeight = base.TitleBarBounds.Height;
		_title.SetSize(420, 36);
		_title.SetPosition(24, FrameY(12));
		_subtitle.SetSize(420, 20);
		_subtitle.SetPosition(26, FrameY(52));
		_close.SetSize(24, 24);
		_close.SetPosition(836, FrameY(14));
		_gradeChip.SetSize(220, 58);
		_gradeChip.SetPosition(604, FrameY(12));
		_gradeName.SetBounds(12, 4, 120, 22);
		_gradeReputation.SetBounds(120, 4, 88, 22);
		_gradeTrack.SetBounds(12, 28, 196, 4);
		_gradeFill.SetPosition(12, 28);
		_gradeNext.SetBounds(12, 34, 196, 18);
		for (int i = 0; i < _tabs.Length; i++)
		{
			_tabs[i].SetPosition(24 + i * 166, FrameY(76));
		}
		_list.SetSize(832, 422);
		_list.SetPosition(24, FrameY(120));
		_footerLeft.SetSize(300, 22);
		_footerLeft.SetPosition(24, FrameY(560));
		_footerBounty.SetSize(522, 22);
		_footerBounty.SetPosition(334, FrameY(560));
	}
}
