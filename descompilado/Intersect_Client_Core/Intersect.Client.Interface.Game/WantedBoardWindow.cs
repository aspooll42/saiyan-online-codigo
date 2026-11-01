using System;
using System.Collections.Generic;
using System.Linq;
using Intersect.Client.Core;
using Intersect.Client.Entities;
using Intersect.Client.Framework.Content;
using Intersect.Client.Framework.File_Management;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.Framework.Gwen;
using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.Framework.Gwen.Renderer;
using Intersect.Client.Framework.Gwen.Skin;
using Intersect.Client.General;
using Intersect.Client.Interface.Game.EntityPanel;
using Intersect.Client.Interface.Shared;
using Intersect.Client.Localization;
using Intersect.Client.Maps;
using Intersect.Client.Networking;
using Intersect.Config;
using Intersect.Framework.Core;
using Intersect.Framework.Core.GameObjects.Titles;
using Intersect.Localization;
using Intersect.Models;
using Intersect.Network.Packets.Server;

namespace Intersect.Client.Interface.Game;

public sealed class WantedBoardWindow : WindowControl
{
	private enum Tab
	{
		Criminals,
		Police,
		Grades,
		Rules
	}

	internal sealed class PosterPortrait : Intersect.Client.Framework.Gwen.Control.Base
	{
		private static readonly Dictionary<string, int> HeadCenters = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

		public string? Sprite { get; set; }

		public string? Face { get; set; }

		public Color Tint { get; set; } = SaiyanHud.White;

		public PosterPortrait(Intersect.Client.Framework.Gwen.Control.Base parent, string name)
			: base(parent, name)
		{
			base.MouseInputEnabled = false;
		}

		protected override void Render(Intersect.Client.Framework.Gwen.Skin.Base skin)
		{
			base.Render(skin);
			Intersect.Client.Framework.Gwen.Renderer.Base renderer = skin.Renderer;
			if (!string.IsNullOrWhiteSpace(Face))
			{
				IGameTexture texture = GameContentManager.Current.GetTexture(TextureType.Face, Face);
				if (texture != null && texture.Width > 0 && texture.Height > 0)
				{
					float num = Math.Min((float)base.Width / (float)texture.Width, (float)base.Height / (float)texture.Height);
					int num2 = (int)((float)texture.Width * num);
					int num3 = (int)((float)texture.Height * num);
					SaiyanHud.Draw(renderer, texture, (base.Width - num2) / 2, (base.Height - num3) / 2, num2, num3, Tint);
					return;
				}
			}
			SpriteSheets.Pose? pose = SpriteSheets.StandingBody(Sprite);
			if (!pose.HasValue)
			{
				return;
			}
			SpriteSheets.Pose valueOrDefault = pose.GetValueOrDefault();
			if (!valueOrDefault.IsEmpty)
			{
				IGameTexture texture2 = valueOrDefault.Texture;
				Rectangle frame = new Rectangle(valueOrDefault.Frame.X, valueOrDefault.Frame.Y, valueOrDefault.Frame.Width, valueOrDefault.Frame.Height);
				int num4 = Math.Clamp((int)Math.Round((float)frame.Width * 0.6f), 8, frame.Width);
				int num5 = Math.Max(1, Math.Min(base.Width, base.Height) / num4);
				int num6 = SaiyanHud.TopMargin(texture2, frame);
				int num7 = frame.Y + Math.Max(0, num6 - 1);
				int num8 = Math.Min(num4, frame.Y + frame.Height - num7);
				int num9 = HeadCenter(texture2, frame, num6);
				int num10 = Math.Clamp(num9 - num4 / 2, frame.X, frame.X + frame.Width - num4);
				if (num8 > 0)
				{
					int num11 = num4 * num5;
					SaiyanHud.Draw(renderer, texture2, (base.Width - num11) / 2, (base.Height - num11) / 2, num11, num8 * num5, Tint, (float)num10 / (float)texture2.Width, (float)num7 / (float)texture2.Height, (float)(num10 + num4) / (float)texture2.Width, (float)(num7 + num8) / (float)texture2.Height);
				}
			}
		}

		private static int HeadCenter(IGameTexture texture, Rectangle frame, int top)
		{
			string key = $"{texture.Name}:{frame.X},{frame.Y}";
			if (HeadCenters.TryGetValue(key, out var value))
			{
				return value;
			}
			int num = frame.X + frame.Width / 2;
			if (!PixelsPlanche.Lire(texture, out Color[] pixels) || pixels.Length < texture.Width * texture.Height)
			{
				return num;
			}
			int num2 = int.MaxValue;
			int num3 = int.MinValue;
			for (int i = top; i < Math.Min(frame.Height, top + 18); i++)
			{
				int num4 = (frame.Y + i) * texture.Width + frame.X;
				for (int j = 0; j < frame.Width; j++)
				{
					if (pixels[num4 + j].A > 8)
					{
						num2 = Math.Min(num2, j);
						num3 = Math.Max(num3, j);
					}
				}
			}
			if (num2 <= num3)
			{
				num = frame.X + (num2 + num3 + 1) / 2;
			}
			HeadCenters[key] = num;
			return num;
		}
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

	private const int PosterWidth = 266;

	private const int PosterHeight = 268;

	private const int PosterGap = 12;

	private const int Columns = 3;

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

	private static readonly Color PoliceLight = new Color(255, 214, 235, 255);

	private static readonly Color CriminalLight = new Color(255, 255, 217, 210);

	private static readonly Color CriminalRed = new Color(255, 240, 80, 60);

	private static readonly Color Salmon = new Color(255, 255, 154, 134);

	private static readonly Color Green = new Color(255, 78, 200, 120);

	private static readonly Color GreenLight = new Color(255, 140, 235, 170);

	private static readonly Color AbsentTint = new Color(210, 150, 156, 168);

	private static WantedBoardWindow? _open;

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

	private readonly List<Intersect.Client.Framework.Gwen.Control.Base> _items = new List<Intersect.Client.Framework.Gwen.Control.Base>();

	private readonly Label _footerCount;

	private readonly Label _footerHint;

	private WantedBoardPacket _board;

	private Tab _tab;

	private bool _needsRebuild = true;

	private bool _finished;

	private int _laidOutTitleBarHeight = -1;

	private static WantedBoardPacket? _stored;

	private static long _abandonDuJournalA = -4611686018427387904L;

	private static readonly Color SaboteurRed = new Color(255, 255, 122, 70);

	public static WantedBoardWindow? Current
	{
		get
		{
			WantedBoardWindow open = _open;
			if (open == null || open.IsHidden || open._finished)
			{
				return null;
			}
			return open;
		}
	}

	private bool ContractsFull => _board.Contracts >= Math.Max(1, _board.MaxContracts);

	private WantedBoardWindow(WantedBoardPacket board)
		: base(Interface.CurrentInterface.Root, string.Empty, modal: true, "WantedBoardWindow")
	{
		_board = board;
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
		IGameTexture texture = current.GetTexture(TextureType.Gui, "saiyan_panneau.png");
		SetImage(texture, "saiyan_panneau.png", ControlState.Active);
		SetImage(texture, "saiyan_panneau.png", ControlState.Inactive);
		_title = MakeLabel(this, "Title", 20, White, current.GetFont("refuel"));
		_title.Text = Strings.WantedBoard.Title.ToString().ToUpperInvariant();
		WindowTitle.Apply(_title);
		_subtitle = MakeLabel(this, "Subtitle", 10, Dim, _body);
		_gradeChip = new ImagePanel(this, "Grade")
		{
			MouseInputEnabled = false,
			Texture = current.GetTexture(TextureType.Gui, "panneau_grade.png")
		};
		_gradeName = MakeLabel(_gradeChip, "GradeName", 14, PoliceBlue, _strong);
		_gradeReputation = MakeLabel(_gradeChip, "GradeReputation", 12, Dim, _strong);
		_gradeReputation.TextAlign = Pos.Right | Pos.CenterV;
		_gradeTrack = new ImagePanel(_gradeChip, "GradeTrack")
		{
			MouseInputEnabled = false,
			Texture = current.GetTexture(TextureType.Gui, "panneau_grade_piste.png")
		};
		_gradeFill = new ImagePanel(_gradeChip, "GradeFill")
		{
			MouseInputEnabled = false,
			Texture = current.GetTexture(TextureType.Gui, "panneau_grade_jauge.png")
		};
		_gradeNext = MakeLabel(_gradeChip, "GradeNext", 8, Dim, _body);
		_close = new Button(this, "Close", disableText: true);
		_close.SetStateTexture(ComponentState.Normal, "saiyan_close.png");
		_close.SetStateTexture(ComponentState.Hovered, "saiyan_close_hover.png");
		_close.SetStateTexture(ComponentState.Active, "saiyan_close_clicked.png");
		_close.Clicked += delegate
		{
			CloseBoard();
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
		_list = new ScrollControl(this, "WantedContent");
		_list.EnableScroll(horizontal: false, vertical: true);
		_list.VerticalScrollBar.SetBackgroundTemplate(null, "saiyan_scroll_bg.png");
		_list.VerticalScrollBar.SetScrollBarImage(null, "saiyan_scroll_bar.png", ComponentState.Normal);
		_list.VerticalScrollBar.SetScrollBarImage(null, "saiyan_scroll_bar_hover.png", ComponentState.Hovered);
		_list.VerticalScrollBar.SetScrollBarImage(null, "saiyan_scroll_bar_hover.png", ComponentState.Active);
		_footerCount = MakeLabel(this, "FooterCount", 10, Muted, _body);
		_footerHint = MakeLabel(this, "FooterHint", 10, Dim, _body);
		_footerHint.TextAlign = Pos.Right | Pos.CenterV;
		_footerHint.Text = Strings.WantedBoard.RadarHint;
		Label subtitle = _subtitle;
		string text = WantedPlaces.ZoneOf(Globals.Me?.MapId ?? Guid.Empty);
		subtitle.Text = ((text != null && text.Length > 0) ? Strings.WantedBoard.StationIn.ToString(text) : Strings.WantedBoard.Station.ToString());
		Layout();
		SetPosition(Math.Max(0, (Interface.CurrentInterface.Root.Width - 880) / 2), Math.Max(0, (Interface.CurrentInterface.Root.Height - 600) / 2));
		RefreshHeader();
		Show();
		BringToFront();
	}

	internal static void IgnorerLeProchainPanneau()
	{
		_abandonDuJournalA = Timing.Global.Milliseconds;
	}

	public static void Open(WantedBoardPacket board)
	{
		if (board.Behind)
		{
			_stored = board;
			WantedBoardWindow current = Current;
			if (current != null)
			{
				current._board = board;
				current._needsRebuild = true;
				current.RefreshHeader();
			}
			return;
		}
		_stored = null;
		WantedBoardWindow current2 = Current;
		if (current2 != null)
		{
			current2._board = board;
			current2._needsRebuild = true;
			current2.RefreshHeader();
		}
		else if (Timing.Global.Milliseconds - _abandonDuJournalA < 5000)
		{
			_abandonDuJournalA = -4611686018427387904L;
		}
		else
		{
			_open?.CloseBoard();
			_open = new WantedBoardWindow(board);
		}
	}

	public static void OpenStored()
	{
		WantedBoardPacket stored = _stored;
		if (stored != null)
		{
			stored.Behind = false;
			Open(stored);
		}
	}

	public void CloseBoard()
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
		KarmaOptions karma = Options.Instance.Karma;
		int[] array = KarmaOptions.ParseList(karma.PoliceGradeThresholds);
		int num = Math.Max(0, _board.Reputation);
		int num2 = karma.PoliceGradeOf(num);
		_gradeName.Text = GradeName(num2).ToUpperInvariant();
		if (num2 + 1 < array.Length)
		{
			int num3 = array[Math.Clamp(num2, 0, array.Length - 1)];
			int num4 = array[num2 + 1];
			_gradeReputation.Text = Strings.WantedBoard.Reputation.ToString(SaiyanHud.Amount(num), SaiyanHud.Amount(num4));
			_gradeNext.Text = Strings.WantedBoard.NextGrade.ToString(GradeName(num2 + 1), SaiyanHud.Amount(num4));
			float num5 = ((num4 > num3) ? Math.Clamp((float)(num - num3) / (float)(num4 - num3), 0f, 1f) : 1f);
			_gradeFill.SetSize(Math.Max(0, (int)(196f * num5)), 4);
		}
		else
		{
			_gradeReputation.Text = SaiyanHud.Amount(num);
			_gradeNext.Text = Strings.WantedBoard.TopGrade;
			_gradeFill.SetSize(196, 4);
		}
		_gradeFill.IsHidden = _gradeFill.Width <= 0;
		Button obj = _tabs[0];
		LocalizedString tabCriminals = Strings.WantedBoard.TabCriminals;
		object[] array2 = new object[1];
		WantedCriminalEntryPacket[] criminals = _board.Criminals;
		array2[0] = ((criminals != null) ? criminals.Length : 0);
		obj.Text = tabCriminals.ToString(array2);
		Button obj2 = _tabs[1];
		LocalizedString tabPolice = Strings.WantedBoard.TabPolice;
		object[] array3 = new object[1];
		PoliceBountyEntryPacket[] bounties = _board.Bounties;
		array3[0] = ((bounties != null) ? bounties.Length : 0);
		obj2.Text = tabPolice.ToString(array3);
		_tabs[2].Text = Strings.WantedBoard.TabGrades;
		_tabs[3].Text = Strings.WantedBoard.TabRules;
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
		_footerCount.Text = Strings.WantedBoard.ContractsHeld.ToString(_board.Contracts, Math.Max(1, _board.MaxContracts));
	}

	internal static string GradeName(int grade)
	{
		return grade switch
		{
			0 => Strings.WantedBoard.Grade0, 
			1 => Strings.WantedBoard.Grade1, 
			2 => Strings.WantedBoard.Grade2, 
			3 => Strings.WantedBoard.Grade3, 
			4 => Strings.WantedBoard.Grade4, 
			_ => Strings.WantedBoard.Grade5, 
		};
	}

	private void Rebuild(bool keepScroll = true)
	{
		int verticalScroll = (keepScroll ? _list.VerticalScroll : 0);
		foreach (Intersect.Client.Framework.Gwen.Control.Base item in _items)
		{
			item.Parent?.RemoveChild(item, dispose: true);
		}
		_items.Clear();
		switch (_tab)
		{
		case Tab.Criminals:
			BuildCriminals();
			break;
		case Tab.Police:
			BuildPolice();
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

	private void BuildCriminals()
	{
		int num = SaboteurAlerts();
		WantedCriminalEntryPacket[] array = _board.Criminals ?? Array.Empty<WantedCriminalEntryPacket>();
		if (array.Length == 0)
		{
			AddEmpty(Strings.WantedBoard.NoCriminal);
			if (num > 0)
			{
				_items.LastOrDefault()?.SetPosition(4, num + 6);
			}
			return;
		}
		for (int i = 0; i < array.Length; i++)
		{
			var (x, y) = PosterAt(i, num);
			_items.Add(BuildCriminalPoster(array[i], x, y));
		}
	}

	private int SaboteurAlerts()
	{
		int num = 0;
		Guid[] array = _board.Saboteurs ?? Array.Empty<Guid>();
		foreach (Guid mapId in array)
		{
			string text = WantedPlaces.ZoneOf(mapId);
			string text2 = ((text != null && text.Length > 0) ? text : Strings.WantedBoard.UnknownPlace.ToString());
			Label label = MakeLabel(_list, "SaboteurAlert", 10, SaboteurRed, _body);
			label.Text = Strings.Recherche.AlerteSaboteur.ToString(text2);
			label.SetSize(822, 20);
			label.SetPosition(0, num);
			_items.Add(label);
			num += 22;
		}
		if (num != 0)
		{
			return num + 6;
		}
		return 0;
	}

	private void BuildPolice()
	{
		PoliceBountyEntryPacket[] array = _board.Bounties ?? Array.Empty<PoliceBountyEntryPacket>();
		if (array.Length == 0)
		{
			AddEmpty(Strings.WantedBoard.NoBounty);
			return;
		}
		int num = Globals.Me?.Level ?? 1;
		Label label = MakeLabel(_list, "PoliceNote", 10, Dim, _body);
		label.Text = Strings.WantedBoard.DrawnAtLevel.ToString(num, array.Min((PoliceBountyEntryPacket bounty) => bounty.Level), array.Max((PoliceBountyEntryPacket bounty) => bounty.Level));
		label.SetSize(822, 20);
		label.SetPosition(0, 0);
		_items.Add(label);
		for (int num2 = 0; num2 < array.Length; num2++)
		{
			var (x, y) = PosterAt(num2, 28);
			_items.Add(BuildPolicePoster(array[num2], x, y));
		}
	}

	private static (int X, int Y) PosterAt(int index, int top)
	{
		return (X: index % 3 * 278, Y: top + index / 3 * 280);
	}

	private void AddEmpty(string text)
	{
		Label label = MakeLabel(_list, "Empty", 11, Dim, _body);
		label.Text = text;
		label.SetSize(822, 24);
		label.SetPosition(4, 6);
		_items.Add(label);
	}

	private Intersect.Client.Framework.Gwen.Control.Base BuildCriminalPoster(WantedCriminalEntryPacket entry, int x, int y)
	{
		ImagePanel imagePanel = MakePoster("panneau_affiche_criminel.png", x, y);
		Banner(imagePanel, Strings.WantedBoard.Wanted, CriminalLight, Strings.WantedBoard.Criminal, CriminalRed);
		PosterPortrait posterPortrait = new PosterPortrait(imagePanel, "Portrait")
		{
			Sprite = entry.Sprite,
			Face = entry.Face,
			Tint = (entry.Online ? SaiyanHud.White : AbsentTint)
		};
		posterPortrait.SetBounds(14, 40, 76, 76);
		Name(imagePanel, entry.Name);
		Who(imagePanel, string.IsNullOrWhiteSpace(entry.ClassName) ? Strings.WantedBoard.Level.ToString(entry.Level) : Strings.WantedBoard.ClassLevel.ToString(entry.ClassName, entry.Level));
		ImagePanel imagePanel2 = new ImagePanel(imagePanel, "Status")
		{
			MouseInputEnabled = false,
			Texture = GameContentManager.Current.GetTexture(TextureType.Gui, "saiyan_amis_pastille.png"),
			RenderColor = (entry.Online ? Green : Dim)
		};
		imagePanel2.SetBounds(102, 81, 8, 8);
		Label label = MakeLabel(imagePanel, "Presence", 9, entry.Online ? GreenLight : Dim, _body);
		label.Text = (entry.Online ? Strings.WantedBoard.Online : ((entry.OfflineMinutes >= 0) ? ((LocalizedString)Strings.WantedBoard.OfflineSince.ToString(WantedPlaces.Elapsed(entry.OfflineMinutes))) : Strings.WantedBoard.Offline));
		label.SetBounds(114, 76, 146, 18);
		Bounty(imagePanel, SaiyanHud.Amount(entry.Bounty), Strings.WantedBoard.BountyUnit, 20);
		List<(string, Color)> list = new List<(string, Color)>();
		string text = WantedPlaces.ZoneOf(entry.SightingMapId);
		list.Add(((entry.SightingMinutes < 0 || entry.SightingMapId == Guid.Empty) ? ((string)Strings.WantedBoard.NeverSeen) : Strings.WantedBoard.LastSeen.ToString((text.Length > 0) ? text : Strings.WantedBoard.UnknownPlace.ToString(), WantedPlaces.Elapsed(entry.SightingMinutes)), Soft));
		if (entry.Murders > 0)
		{
			list.Add(((entry.Murders == 1) ? ((string)Strings.WantedBoard.OneMurder) : Strings.WantedBoard.Murders.ToString(entry.Murders), Soft));
		}
		List<(string, Color)> list2 = list;
		int hunters = entry.Hunters;
		(string, Color) item = ((hunters <= 0) ? (Strings.WantedBoard.NoHunter.ToString(), Dim) : ((hunters != 1) ? (Strings.WantedBoard.Hunters.ToString(entry.Hunters), Salmon) : (Strings.WantedBoard.OneHunter.ToString(), Salmon)));
		list2.Add(item);
		KarmaOptions karma = Options.Instance.Karma;
		int num = (Globals.Me?.Level ?? 0) - entry.Level;
		if (karma.BountyLevelGap > 0 && num >= karma.BountyLevelGap)
		{
			long value = entry.Bounty * Math.Clamp(karma.BountyLevelGapPercent, 0, 100) / 100;
			list.Add((Strings.WantedBoard.LevelGap.ToString(num, SaiyanHud.Amount(value)), Ki));
		}
		Lines(imagePanel, list);
		KarmaTier? karmaTier = Globals.Me?.KarmaTier;
		bool flag;
		if (karmaTier.HasValue)
		{
			KarmaTier valueOrDefault = karmaTier.GetValueOrDefault();
			if ((uint)valueOrDefault <= 1u)
			{
				flag = true;
				goto IL_0410;
			}
		}
		flag = false;
		goto IL_0410;
		IL_0410:
		bool flag2 = flag;
		ContractButton(imagePanel, entry.Taken, entry.Taken || (flag2 && !ContractsFull), flag2 ? Strings.WantedBoard.Full : Strings.WantedBoard.HonestOnly, WantedContractKind.Criminal, entry.Id);
		return imagePanel;
	}

	private Intersect.Client.Framework.Gwen.Control.Base BuildPolicePoster(PoliceBountyEntryPacket entry, int x, int y)
	{
		ImagePanel imagePanel = MakePoster("panneau_affiche_police.png", x, y);
		Banner(imagePanel, Strings.WantedBoard.PoliceBounty, PoliceLight, Strings.WantedBoard.ExpiresIn.ToString(WantedPlaces.Duration(_board.ResetInMinutes)), PoliceBlue);
		PosterPortrait posterPortrait = new PosterPortrait(imagePanel, "Portrait")
		{
			Sprite = entry.Sprite,
			Tint = SaiyanHud.White
		};
		posterPortrait.SetBounds(14, 40, 76, 76);
		Name(imagePanel, entry.Name);
		Who(imagePanel, Strings.WantedBoard.EliteLevel.ToString(entry.Level));
		Bounty(imagePanel, Strings.WantedBoard.Reward.ToString(SaiyanHud.Amount(entry.Zeni), SaiyanHud.Amount(entry.Experience)), string.Empty, 17);
		string text = WantedPlaces.ZoneOf(entry.MapId);
		string text2 = entry.Place?.Trim() ?? string.Empty;
		string text3 = ((text.Length == 0) ? text2 : ((text2.Length != 0 && !text2.Contains(text, StringComparison.OrdinalIgnoreCase)) ? (text + ", " + text2) : ((text2.Length == 0) ? text : text2)));
		List<(string, Color)> list = new List<(string, Color)>();
		list.Add((Strings.WantedBoard.Spotted.ToString((text3.Length > 0) ? text3 : Strings.WantedBoard.UnknownPlace.ToString()), Soft));
		list.Add((Strings.WantedBoard.PaidByPolice.ToString(), Dim));
		List<(string, Color)> list2 = list;
		bool flag = entry.Done && entry.Taken;
		if (flag)
		{
			list2.Add((Strings.WantedBoard.FulfilledLine.ToString(), Gold));
		}
		else if (entry.Done)
		{
			list2.Add((Strings.WantedBoard.DoneToday.ToString(), GreenLight));
		}
		Lines(imagePanel, list2);
		KarmaTier? karmaTier = Globals.Me?.KarmaTier;
		bool flag2;
		if (karmaTier.HasValue)
		{
			KarmaTier valueOrDefault = karmaTier.GetValueOrDefault();
			if ((uint)valueOrDefault <= 1u)
			{
				flag2 = true;
				goto IL_0261;
			}
		}
		flag2 = false;
		goto IL_0261;
		IL_0261:
		bool flag3 = flag2;
		ContractButton(imagePanel, entry.Taken && !flag, (entry.Taken && !flag) || (!entry.Done && flag3 && !ContractsFull), flag ? Strings.WantedBoard.ToCashIn : (entry.Done ? Strings.WantedBoard.Done : (flag3 ? Strings.WantedBoard.Full : Strings.WantedBoard.HonestOnly)), WantedContractKind.Police, entry.NpcId);
		return imagePanel;
	}

	private ImagePanel MakePoster(string texture, int x, int y)
	{
		ImagePanel imagePanel = new ImagePanel(_list, "Poster")
		{
			Texture = GameContentManager.Current.GetTexture(TextureType.Gui, texture)
		};
		imagePanel.SetBounds(x, y, 266, 268);
		return imagePanel;
	}

	private void Banner(Intersect.Client.Framework.Gwen.Control.Base poster, string left, Color leftColor, string right, Color rightColor)
	{
		Label label = MakeLabel(poster, "Banner", 13, leftColor, _strong);
		label.Text = left.ToUpperInvariant();
		label.SetBounds(10, 0, 150, 26);
		Label label2 = MakeLabel(poster, "BannerRight", 8, rightColor, _body);
		label2.Text = right.ToUpperInvariant();
		label2.TextAlign = Pos.Right | Pos.CenterV;
		label2.SetBounds(126, 0, 130, 26);
	}

	private new void Name(Intersect.Client.Framework.Gwen.Control.Base poster, string name)
	{
		Label label = MakeLabel(poster, "Name", 15, White, _strong);
		label.Text = name;
		label.SetBounds(102, 36, 154, 22);
	}

	private void Who(Intersect.Client.Framework.Gwen.Control.Base poster, string text)
	{
		Label label = MakeLabel(poster, "Who", 9, Dim, _body);
		label.Text = text;
		label.SetBounds(102, 58, 154, 18);
	}

	private void Bounty(Intersect.Client.Framework.Gwen.Control.Base poster, string amount, string unit, int size)
	{
		ImagePanel imagePanel = new ImagePanel(poster, "Coin")
		{
			MouseInputEnabled = false,
			Texture = GameContentManager.Current.GetTexture(TextureType.Gui, "saiyan_echange_coin.png")
		};
		imagePanel.SetBounds(102, 101, 12, 12);
		Label label = MakeLabel(poster, "Amount", size, Gold, _strong);
		label.Text = amount;
		int num = Measure(_strong, size, amount) + 4;
		label.SetBounds(118, 92, Math.Min(num, 138), 30);
		if (unit.Length != 0)
		{
			Label label2 = MakeLabel(poster, "Unit", 8, Dim, _body);
			label2.Text = unit;
			label2.SetBounds(118 + num + 2, 96, Math.Max(0, 148 - num - 2 - 8), 24);
		}
	}

	private void Lines(Intersect.Client.Framework.Gwen.Control.Base poster, List<(string Text, Color Color)> lines)
	{
		int num = 124;
		int width = 240;
		foreach (var (text, color) in lines)
		{
			foreach (string item in Wrap(text, width, 9, _body))
			{
				Label label = MakeLabel(poster, "Line", 9, color, _body);
				label.Text = item;
				label.SetBounds(14, num, width, 17);
				num += 15;
			}
			num += 2;
		}
	}

	private void ContractButton(Intersect.Client.Framework.Gwen.Control.Base poster, bool taken, bool enabled, string disabledText, WantedContractKind kind, Guid targetId)
	{
		Button button = new Button(poster, "Contract")
		{
			Font = _body,
			FontSize = 10,
			TextAlign = Pos.Center,
			UsesHeadingFace = false
		};
		button.SetBounds(104, 226, 150, 30);
		if (taken)
		{
			button.SetStateTexture(ComponentState.Normal, "panneau_pris_150x30.png");
			button.SetStateTexture(ComponentState.Hovered, "panneau_pris_150x30_hover.png");
			button.SetStateTexture(ComponentState.Active, "panneau_pris_150x30_hover.png");
			button.Text = Strings.WantedBoard.Taken;
			button.SetTextColor(Gold, ComponentState.Normal);
			button.SetTextColor(Salmon, ComponentState.Hovered);
			button.SetTextColor(Salmon, ComponentState.Active);
			button.HoverEnter += delegate
			{
				button.Text = Strings.WantedBoard.Abandon;
			};
			button.HoverLeave += delegate
			{
				button.Text = Strings.WantedBoard.Taken;
			};
			button.Clicked += delegate
			{
				button.IsDisabled = true;
				PacketSender.SendWantedContract(kind, targetId, take: false);
			};
			return;
		}
		button.SetStateTexture(ComponentState.Normal, "panneau_cta_150x30.png");
		button.SetStateTexture(ComponentState.Hovered, "panneau_cta_150x30_hover.png");
		button.SetStateTexture(ComponentState.Active, "panneau_cta_150x30_clicked.png");
		button.SetStateTexture(ComponentState.Disabled, "panneau_cta_150x30_disabled.png");
		button.SetTextColor(Night, ComponentState.Normal);
		button.SetTextColor(Night, ComponentState.Hovered);
		button.SetTextColor(Night, ComponentState.Active);
		button.SetTextColor(Dim, ComponentState.Disabled);
		button.Text = (enabled ? ((string)Strings.WantedBoard.Take) : disabledText);
		button.IsDisabled = !enabled;
		button.Clicked += delegate
		{
			if (!button.IsDisabled)
			{
				button.IsDisabled = true;
				PacketSender.SendWantedContract(kind, targetId, take: true);
			}
		};
	}

	private void BuildGrades()
	{
		KarmaOptions karma = Options.Instance.Karma;
		int[] array = KarmaOptions.ParseList(karma.PoliceGradeThresholds);
		int num = karma.PoliceGradeOf(Math.Max(0, _board.Reputation));
		int num2 = 0;
		for (int i = 0; i < array.Length; i++)
		{
			string text = ((i < num) ? "fait" : ((i == num) ? "actuel" : "futur"));
			bool flag = i > num;
			ImagePanel imagePanel = new ImagePanel(_list, $"Grade_{i}")
			{
				MouseInputEnabled = false,
				Texture = GameContentManager.Current.GetTexture(TextureType.Gui, "panneau_grade_ligne_" + text + ".png")
			};
			imagePanel.SetBounds(0, num2, 812, 46);
			_items.Add(imagePanel);
			string text2 = GradeName(i).ToUpperInvariant();
			Label label = MakeLabel(imagePanel, "Name", 15, flag ? Dim : ((i == num) ? PoliceBlue : White), _strong);
			label.Text = text2;
			label.SetBounds(16, 0, 170, 46);
			if (i == num)
			{
				int x = 16 + Measure(_strong, 15, text2) + 10;
				ImagePanel imagePanel2 = new ImagePanel(imagePanel, "You")
				{
					MouseInputEnabled = false,
					Texture = GameContentManager.Current.GetTexture(TextureType.Gui, "panneau_toi.png")
				};
				imagePanel2.SetBounds(x, 15, 34, 16);
				Label label2 = MakeLabel(imagePanel2, "YouText", 8, Night, _body);
				label2.Text = Strings.WantedBoard.You;
				label2.TextAlign = Pos.Center;
				label2.SetBounds(0, 0, 34, 16);
			}
			Label label3 = MakeLabel(imagePanel, "Threshold", 14, flag ? Dim : Soft, _strong);
			label3.Text = SaiyanHud.Amount(array[i]);
			label3.TextAlign = Pos.Right | Pos.CenterV;
			label3.SetBounds(232, 0, 64, 46);
			string text3 = TitleNameOf(GradeTitles.PoliceTitle(i));
			Label label4 = MakeLabel(imagePanel, "Opens", 10, flag ? Dim : Soft, _body);
			label4.Text = GradeOpens(i);
			label4.SetBounds(318, (text3.Length > 0) ? 3 : 0, 478, (text3.Length > 0) ? 22 : 46);
			if (text3.Length > 0)
			{
				Label label5 = MakeLabel(imagePanel, "Title", 9, flag ? Dim : Gold, _body);
				label5.Text = Strings.WantedBoard.GradeTitle.ToString(text3);
				label5.SetBounds(318, 24, 478, 18);
			}
			num2 += 52;
		}
		num2 += 8;
		Label label6 = MakeLabel(_list, "GainsTitle", 10, Dim, _body);
		label6.Text = Strings.WantedBoard.GainsTitle;
		label6.SetBounds(0, num2, 812, 20);
		_items.Add(label6);
		num2 += 24;
		int num3 = 0;
		string[] array2 = new string[2]
		{
			Strings.WantedBoard.GainBounty.ToString(karma.ReputationPoliceBounty),
			Strings.WantedBoard.GainCriminal.ToString(karma.ReputationCriminalDown)
		};
		foreach (string text4 in array2)
		{
			int num4 = Measure(_body, 10, text4) + 24;
			ImagePanel imagePanel3 = new ImagePanel(_list, "Gain")
			{
				MouseInputEnabled = false,
				Texture = GameContentManager.Current.GetTexture(TextureType.Gui, "panneau_puce.png"),
				TextureNinePatchMargin = new Margin(5)
			};
			imagePanel3.SetBounds(num3, num2, num4, 30);
			Label label7 = MakeLabel(imagePanel3, "GainText", 10, Soft, _body);
			label7.Text = text4;
			label7.SetBounds(12, 0, num4 - 20, 30);
			_items.Add(imagePanel3);
			num3 += num4 + 8;
		}
		num2 += 40;
		Label label8 = MakeLabel(_list, "GainsNote", 10, Dim, _body);
		label8.Text = Strings.WantedBoard.GainsNote;
		label8.SetBounds(0, num2, 812, 20);
		_items.Add(label8);
	}

	internal static string TitleNameOf(Guid titleId)
	{
		if (!(titleId != Guid.Empty) || !DatabaseObject<TitleDescriptor>.TryGet(titleId, out var databaseObject))
		{
			return string.Empty;
		}
		return databaseObject.Name?.Trim() ?? string.Empty;
	}

	internal static string GradeOpens(int grade)
	{
		KarmaOptions karma = Options.Instance.Karma;
		List<string> list = new List<string>();
		int num = KarmaOptions.ForGrade(karma.PoliceGradeBountiesPerDay, grade);
		if (grade == 0 || num > KarmaOptions.ForGrade(karma.PoliceGradeBountiesPerDay, grade - 1))
		{
			list.Add(Strings.WantedBoard.GradeBounties.ToString(num));
		}
		int num2 = KarmaOptions.ForGrade(karma.PoliceGradeRadarDiscount, grade);
		if (num2 > 0 && (grade == 0 || num2 > KarmaOptions.ForGrade(karma.PoliceGradeRadarDiscount, grade - 1)))
		{
			list.Add(Strings.WantedBoard.GradeRadar.ToString(num2));
		}
		if (grade == karma.PoliceEliteBonusGrade && karma.PoliceEliteBonusPercent > 0)
		{
			list.Add(Strings.WantedBoard.GradeElite.ToString(karma.PoliceEliteBonusPercent));
		}
		if (list.Count == 0)
		{
			return Strings.WantedBoard.GradeRespect;
		}
		string text = string.Join(" · ", list);
		ReadOnlySpan<char> readOnlySpan = new ReadOnlySpan<char>(char.ToUpperInvariant(text[0]));
		string text2 = text;
		return string.Concat(readOnlySpan, text2.Substring(1, text2.Length - 1));
	}

	private void BuildRules()
	{
		KarmaOptions karma = Options.Instance.Karma;
		int[] array = KarmaOptions.ParseList(karma.PoliceGradeBountiesPerDay);
		int num = ((array.Length != 0) ? array[0] : 0);
		int num2 = ((array.Length != 0) ? array.Max() : 0);
		int num3 = Math.Max(1, karma.WantedContractHours);
		int width = 405;
		int num4 = 0;
		num4 += RulesRow(num4, width, (Key: "rouge", Color: CriminalRed, Title: Strings.WantedBoard.RulesCriminals, Rules: new string[6]
		{
			Strings.WantedBoard.RuleRace,
			Strings.WantedBoard.RuleShare.ToString(karma.BountyShareSeconds),
			Strings.WantedBoard.RuleZone,
			Strings.WantedBoard.RuleNobody,
			Strings.WantedBoard.RuleCloses.ToString(num3),
			Strings.WantedBoard.RuleGap.ToString(karma.BountyLevelGap, Math.Clamp(karma.BountyLevelGapPercent, 0, 100))
		}), (Key: "bleu", Color: PoliceBlue, Title: Strings.WantedBoard.RulesPolice, Rules: new string[4]
		{
			Strings.WantedBoard.RuleDaily.ToString(num, num2),
			Strings.WantedBoard.RuleOnce,
			Strings.WantedBoard.RuleTogether.ToString(karma.PoliceBountyMinSharePercent),
			Strings.WantedBoard.RuleNoContract
		}));
		num4 += RulesRow(num4, width, (Key: "ki", Color: Ki, Title: Strings.WantedBoard.RulesLogout, Rules: new string[4]
		{
			Strings.WantedBoard.RuleCombat,
			Strings.WantedBoard.RuleOutlaw.ToString(karma.OutlawLogoutSeconds),
			Strings.WantedBoard.RuleChase,
			Strings.WantedBoard.RuleOffline.ToString(num3)
		}), (Key: "vert", Color: GreenLight, Title: Strings.WantedBoard.RulesGrades, Rules: new string[3]
		{
			Strings.WantedBoard.RuleRise,
			Strings.WantedBoard.RuleGradeGives,
			Strings.WantedBoard.RuleReset
		}));
		RuleBlock(0, num4, 822, "or", Gold, Strings.WantedBoard.RulesGood, new string[2]
		{
			Strings.WantedBoard.RuleContracts.ToString(Math.Max(1, karma.WantedMaxContracts)),
			Strings.WantedBoard.RuleHour
		}, 0);
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
		return 40 + rules.Sum((string rule) => Wrap(rule, width - 28 - 14, 10, _body).Count * 16 + 6) + 4;
	}

	private int RuleBlock(int x, int y, int width, string key, Color color, string title, string[] rules, int height)
	{
		height = Math.Max(height, BlockHeight(width, rules));
		ImagePanel imagePanel = new ImagePanel(_list, "Rules_" + key)
		{
			MouseInputEnabled = false,
			Texture = GameContentManager.Current.GetTexture(TextureType.Gui, "panneau_bloc_" + key + ".png"),
			TextureNinePatchMargin = new Margin(8)
		};
		imagePanel.SetBounds(x, y, width, height);
		_items.Add(imagePanel);
		Label label = MakeLabel(imagePanel, "Title", 14, color, _strong);
		label.Text = title.ToUpperInvariant();
		label.SetBounds(14, 8, width - 28, 24);
		int num = 38;
		foreach (string text in rules)
		{
			ImagePanel imagePanel2 = new ImagePanel(imagePanel, "Diamond")
			{
				MouseInputEnabled = false,
				Texture = GameContentManager.Current.GetTexture(TextureType.Gui, "panneau_losange_" + key + ".png")
			};
			imagePanel2.SetBounds(14, num + 5, 7, 7);
			foreach (string item in Wrap(text, width - 28 - 14, 10, _body))
			{
				Label label2 = MakeLabel(imagePanel, "Rule", 10, Soft, _body);
				label2.Text = item;
				label2.SetBounds(28, num - 1, width - 28 - 14, 18);
				num += 16;
			}
			num += 6;
		}
		return height;
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
		_footerCount.SetSize(300, 22);
		_footerCount.SetPosition(24, FrameY(560));
		_footerHint.SetSize(522, 22);
		_footerHint.SetPosition(334, FrameY(560));
	}

	internal static Label MakeLabel(Intersect.Client.Framework.Gwen.Control.Base parent, string name, int size, Color color, IFont? font)
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

	internal static int Measure(IFont? font, int size, string text)
	{
		return (int)Math.Ceiling(Graphics.Renderer?.MeasureText(text, font, size, 1f).X ?? ((float)(text.Length * size) * 0.6f));
	}

	internal static List<string> Wrap(string text, int width, int size, IFont? font)
	{
		List<string> list = new List<string>();
		string text2 = string.Empty;
		string[] array = (text ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries);
		foreach (string text3 in array)
		{
			string text4 = ((text2.Length == 0) ? text3 : (text2 + " " + text3));
			if (text2.Length > 0 && Measure(font, size, text4) > width)
			{
				list.Add(text2);
				text2 = text3;
			}
			else
			{
				text2 = text4;
			}
		}
		if (text2.Length > 0 || list.Count == 0)
		{
			list.Add(text2);
		}
		return list;
	}
}
