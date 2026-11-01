using System;
using System.Collections.Generic;
using System.Linq;
using Intersect.Client.Entities;
using Intersect.Client.Framework.Content;
using Intersect.Client.Framework.Entities;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.Framework.Gwen;
using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.General;
using Intersect.Client.Interface.Shared;
using Intersect.Client.Localization;
using Intersect.Client.Networking;
using Intersect.Localization;

namespace Intersect.Client.Interface.Game;

public class PartyWindow : WindowControl
{
	private readonly record struct VitalRow(ImagePanel Track, ImagePanel Fill, Intersect.Client.Framework.Gwen.Control.Label Value);

	private const int WindowWidth = 760;

	private const int WindowHeight = 600;

	private const int SidebarX = 16;

	private const int SidebarWidth = 184;

	private const int ContentX = 216;

	private const int ContentWidth = 528;

	private const int TopY = 72;

	private const int BottomY = 584;

	private const int CardHeight = 78;

	private const int CardGap = 8;

	private const int PortraitSize = 50;

	private const int FieldHeight = 28;

	private static readonly Color Gold = new Color(255, 245, 190, 90);

	private static readonly Color LightGold = new Color(255, 255, 226, 150);

	private static readonly Color White = new Color(255, 242, 242, 242);

	private static readonly Color Entry = new Color(255, 196, 204, 220);

	private static readonly Color Muted = new Color(255, 176, 186, 206);

	private static readonly Color Dim = new Color(255, 150, 160, 182);

	private static readonly Color Night = new Color(255, 11, 14, 23);

	private static readonly Color OrangeLight = new Color(255, 255, 172, 82);

	private static readonly Color Red = new Color(255, 225, 95, 95);

	private readonly Canvas _gameCanvas;

	private readonly IFont? _font;

	private readonly IFont? _strongFont;

	private readonly IFont? _posterFont;

	private readonly Intersect.Client.Framework.Gwen.Control.Label _title;

	private readonly Button _close;

	private readonly Intersect.Client.Framework.Gwen.Control.Label _sidebarHeading;

	private readonly ImagePanel _shareCard;

	private readonly Intersect.Client.Framework.Gwen.Control.Label _shareLabel;

	private readonly Intersect.Client.Framework.Gwen.Control.Label _shareValue;

	private readonly Intersect.Client.Framework.Gwen.Control.Label _shareHint;

	private readonly ImagePanel _rangeCard;

	private readonly Intersect.Client.Framework.Gwen.Control.Label _rangeLabel;

	private readonly Intersect.Client.Framework.Gwen.Control.Label _rangeValue;

	private readonly Intersect.Client.Framework.Gwen.Control.Label _rangeHint;

	private readonly Button _leaveButton;

	private readonly Intersect.Client.Framework.Gwen.Control.Label _membersHeading;

	private readonly Intersect.Client.Framework.Gwen.Control.Label _membersCount;

	private readonly Intersect.Client.Framework.Gwen.Control.Label _aloneTitle;

	private readonly Intersect.Client.Framework.Gwen.Control.Label _aloneHint;

	private readonly ImagePanel _inviteBack;

	private readonly TextBox _invite;

	private readonly Intersect.Client.Framework.Gwen.Control.Label _inviteHint;

	private readonly Button _inviteButton;

	private readonly List<Base> _rows = new List<Base>();

	private readonly List<(Guid Id, VitalRow[] Rows)> _vitalRows = new List<(Guid, VitalRow[])>();

	private string _shownParty = string.Empty;

	private int _laidOutTitleBarHeight = -1;

	public PartyWindow(Canvas gameCanvas)
		: base(gameCanvas, string.Empty, modal: false, "PartyWindow")
	{
		_gameCanvas = gameCanvas;
		_font = Globals.ContentManager.GetFont("chakrapetch");
		_strongFont = Globals.ContentManager.GetFont("rajdhanibold");
		_posterFont = Globals.ContentManager.GetFont("refuel");
		DisableResizing();
		SetSize(760, 600);
		SetPosition((_gameCanvas.Width - 760) / 2, (_gameCanvas.Height - 600) / 2);
		IGameTexture texture = Globals.ContentManager.GetTexture(TextureType.Gui, "saiyan_window_marche.png");
		SetImage(texture, "saiyan_window_marche.png", ControlState.Active);
		SetImage(texture, "saiyan_window_marche.png", ControlState.Inactive);
		base.IsClosable = false;
		if (base.Titlebar is Titlebar titlebar)
		{
			titlebar.Label.IsHidden = true;
		}
		_title = MakeLabel(this, "Title", 20, White, _posterFont);
		_title.Text = Strings.Parties.Title.ToString().ToUpperInvariant();
		WindowTitle.Apply(_title);
		_close = new Button(this, "Close", disableText: true);
		_close.SetStateTexture(ComponentState.Normal, "saiyan_close.png");
		_close.SetStateTexture(ComponentState.Hovered, "saiyan_close_hover.png");
		_close.SetStateTexture(ComponentState.Active, "saiyan_close_clicked.png");
		_close.Clicked += delegate
		{
			Hide();
		};
		_sidebarHeading = MakeLabel(this, "SidebarHeading", 10, Muted, _font);
		_sidebarHeading.Text = Strings.Parties.YourParty.ToString().ToUpperInvariant();
		_shareCard = MakeCard(this, "ShareCard");
		_shareLabel = MakeLabel(_shareCard, "ShareLabel", 10, Muted, _font);
		_shareLabel.Text = Strings.Parties.YourShare.ToString().ToUpperInvariant();
		_shareValue = MakeLabel(_shareCard, "ShareValue", 28, Gold, _posterFont);
		_shareHint = MakeLabel(_shareCard, "ShareHint", 10, Dim, _font);
		_rangeCard = MakeCard(this, "RangeCard");
		_rangeLabel = MakeLabel(_rangeCard, "RangeLabel", 10, Muted, _font);
		_rangeLabel.Text = Strings.Parties.ShareRange.ToString().ToUpperInvariant();
		_rangeValue = MakeLabel(_rangeCard, "RangeValue", 13, White, _strongFont);
		_rangeValue.Text = Strings.Parties.Tiles.ToString(Options.Instance.Party.SharedXpRange);
		_rangeHint = MakeLabel(_rangeCard, "RangeHint", 10, Dim, _font);
		_rangeHint.Text = Strings.Parties.RangeHint;
		_leaveButton = MakeButton(this, "LeaveButton", 150, 28);
		_leaveButton.Text = Strings.Parties.Leave;
		_leaveButton.SetToolTipText(Strings.Parties.LeaveTip);
		_leaveButton.Clicked += delegate
		{
			PacketSender.SendPartyLeave();
		};
		_membersHeading = MakeLabel(this, "MembersHeading", 10, Muted, _font);
		_membersHeading.Text = Strings.Parties.Members.ToString().ToUpperInvariant();
		_membersCount = MakeLabel(this, "MembersCount", 9, Muted, _font);
		_membersCount.TextAlign = Pos.Right | Pos.CenterV;
		_aloneTitle = MakeLabel(this, "AloneTitle", 13, Entry, _font);
		_aloneTitle.Text = Strings.Parties.Alone;
		_aloneHint = MakeLabel(this, "AloneHint", 10, Dim, _font);
		_aloneHint.Text = Strings.Parties.AloneHint;
		_aloneHint.WrappingBehavior = WrappingBehavior.Wrapped;
		_aloneHint.TextAlign = Pos.Left | Pos.Top;
		(_inviteBack, _invite) = MakeField(this, "Invite", 260);
		_invite.SubmitPressed += delegate
		{
			SubmitInvite();
		};
		_inviteHint = MakeLabel(_inviteBack, "InviteHint", 10, Dim, _font);
		_inviteHint.Text = Strings.Parties.InviteHint;
		_invite.TextChanged += delegate
		{
			_inviteHint.IsHidden = (_invite.Text?.Length ?? 0) > 0;
		};
		_inviteButton = MakeCtaButton(this, "InviteButton");
		_inviteButton.Text = Strings.Parties.InviteButton;
		_inviteButton.Clicked += delegate
		{
			SubmitInvite();
		};
		Layout();
		Hide();
	}

	public bool IsVisible()
	{
		return !base.IsHidden;
	}

	public new void Show()
	{
		Layout();
		_shownParty = string.Empty;
		base.Show();
		BringToFront();
	}

	public void Update()
	{
		if (!base.IsHidden && Globals.Me != null)
		{
			if (base.TitleBarBounds.Height != _laidOutTitleBarHeight)
			{
				Layout();
				_shownParty = string.Empty;
			}
			string text = PartySignature();
			if (text != _shownParty)
			{
				_shownParty = text;
				Rebuild();
			}
			else
			{
				RefreshVitals();
			}
		}
	}

	private static string PartySignature()
	{
		List<IPartyMember> list = Globals.Me?.Party;
		if (list == null || list.Count <= 0)
		{
			return "seul";
		}
		return string.Join('|', list.Select((IPartyMember member) => $"{member.Id}:{member.Level}"));
	}

	private void Rebuild()
	{
		foreach (Base row in _rows)
		{
			row.Parent?.RemoveChild(row, dispose: true);
		}
		_rows.Clear();
		_vitalRows.Clear();
		List<IPartyMember> list = Globals.Me?.Party ?? new List<IPartyMember>();
		bool flag = list.Count > 0;
		int num = Math.Max(1, Options.Instance.Party.MaximumMembers);
		_membersCount.Text = (flag ? Strings.Parties.Count.ToString(list.Count, num) : string.Empty);
		_membersCount.IsHidden = !flag;
		_membersHeading.IsHidden = !flag;
		_aloneTitle.IsHidden = flag;
		_aloneHint.IsHidden = flag;
		_leaveButton.IsHidden = !flag;
		_shareCard.IsHidden = !flag;
		_rangeCard.IsHidden = !flag;
		_sidebarHeading.IsHidden = !flag;
		UpdateShare(list.Count);
		if (flag)
		{
			Guid id = list[0].Id;
			Guid? obj = Globals.Me?.Id;
			bool iAmLeader = id == obj;
			int num2 = 106;
			for (int i = 0; i < list.Count; i++)
			{
				_rows.Add(BuildMember(list[i], i, num2, iAmLeader));
				num2 += 86;
			}
			if (list.Count < num)
			{
				_rows.Add(BuildFreeSlot(num2));
			}
			RefreshVitals();
		}
	}

	private void UpdateShare(int members)
	{
		if (members < 1)
		{
			members = 1;
		}
		float num = Options.Instance.Party.BonusExperiencePercentPerMember / 100f;
		int num2 = (int)Math.Round((1f + (float)members * num) / (float)members * 100f);
		int num3 = (int)Math.Round(100f / (float)members);
		_shareValue.Text = $"{num2} %";
		_shareHint.Text = Strings.Parties.WithoutBonus.ToString(num3);
		_shareHint.IsHidden = num2 <= num3;
	}

	private Base BuildMember(IPartyMember member, int index, int y, bool iAmLeader)
	{
		ImagePanel imagePanel = MakeCard(this, $"Member{index}");
		imagePanel.SetSize(528, 78);
		imagePanel.SetPosition(216, Y(y));
		ImagePanel imagePanel2 = new ImagePanel(imagePanel, "Portrait")
		{
			Texture = Globals.ContentManager.GetTexture(TextureType.Gui, "saiyan_slot.png"),
			TextureNinePatchMargin = new Margin(6),
			MouseInputEnabled = false
		};
		imagePanel2.SetSize(50, 50);
		imagePanel2.SetPosition(14, 14);
		DrawPortrait(imagePanel2, member.Sprite);
		Guid id = member.Id;
		Guid? obj = Globals.Me?.Id;
		bool flag = id == obj;
		Intersect.Client.Framework.Gwen.Control.Label label = MakeLabel(imagePanel, "Name", 13, flag ? Gold : White, _font);
		label.Text = member.Name;
		label.SetSize(200, 24);
		label.SetPosition(76, 10);
		Intersect.Client.Framework.Gwen.Control.Label label2 = MakeLabel(imagePanel, "Level", 9, Muted, _font);
		label2.Text = Strings.Parties.LevelShort.ToString(member.Level);
		label2.SetSize(200, 18);
		label2.SetPosition(76, 32);
		if (index == 0)
		{
			ImagePanel imagePanel3 = MakeCard(imagePanel, "LeaderChip");
			imagePanel3.Texture = Globals.ContentManager.GetTexture(TextureType.Gui, "saiyan_chip_done.png");
			imagePanel3.SetSize(58, 20);
			imagePanel3.SetPosition(186, 12);
			Intersect.Client.Framework.Gwen.Control.Label label3 = MakeLabel(imagePanel, "LeaderText", 9, LightGold, _font);
			label3.Text = Strings.Parties.Leader.ToString().ToUpperInvariant();
			label3.TextAlign = Pos.Center;
			label3.SetSize(58, 20);
			label3.SetPosition(186, 12);
		}
		if (IsOutOfRange(member))
		{
			Intersect.Client.Framework.Gwen.Control.Label label4 = MakeLabel(imagePanel, "OutOfRange", 10, Red, _font);
			label4.Text = Strings.Parties.OutOfRange;
			label4.SetSize(140, 20);
			label4.SetPosition(254, 12);
		}
		VitalRow[] array = new VitalRow[2];
		LocalizedString[] array2 = new LocalizedString[2]
		{
			Strings.Parties.Vital0,
			Strings.Parties.Vital1
		};
		string[] array3 = new string[2] { "saiyan_bar_fill_pv.png", "saiyan_bar_fill_ki.png" };
		for (int i = 0; i < 2; i++)
		{
			Intersect.Client.Framework.Gwen.Control.Label label5 = MakeLabel(imagePanel, $"Vital{i}Label", 10, Muted, _font);
			label5.Text = array2[i].ToString().TrimEnd(' ', ':');
			label5.SetSize(24, 18);
			label5.SetPosition(76, 45 + i * 16);
			var (imagePanel4, imagePanel5) = MakeBar(imagePanel, $"Vital{i}", array3[i]);
			imagePanel4.SetSize(240, 12);
			imagePanel4.SetPosition(102, 48 + i * 16);
			imagePanel5.SetPosition(0, 0);
			Intersect.Client.Framework.Gwen.Control.Label label6 = MakeLabel(imagePanel, $"Vital{i}Value", 10, Entry, _font);
			label6.SetSize(110, 20);
			label6.SetPosition(350, 44 + i * 16);
			array[i] = new VitalRow(imagePanel4, imagePanel5, label6);
		}
		_vitalRows.Add((member.Id, array));
		if (iAmLeader && !flag)
		{
			Button button = MakeButton(imagePanel, "Kick", 84, 28);
			button.Text = Strings.Parties.KickLabel;
			button.SetToolTipText(Strings.Parties.Kick.ToString(member.Name));
			button.SetPosition(430, 12);
			Guid id2 = member.Id;
			button.Clicked += delegate
			{
				PacketSender.SendPartyKick(id2);
			};
		}
		return imagePanel;
	}

	private Base BuildFreeSlot(int y)
	{
		ImagePanel imagePanel = new ImagePanel(this, "FreeSlot")
		{
			Texture = Globals.ContentManager.GetTexture(TextureType.Gui, "saiyan_slot.png"),
			TextureNinePatchMargin = new Margin(6),
			MouseInputEnabled = false
		};
		imagePanel.SetSize(528, 78);
		imagePanel.SetPosition(216, Y(y));
		Intersect.Client.Framework.Gwen.Control.Label label = MakeLabel(imagePanel, "FreeTitle", 13, Dim, _font);
		label.Text = Strings.Parties.FreeSlot;
		label.SetSize(240, 24);
		label.SetPosition(76, 18);
		Intersect.Client.Framework.Gwen.Control.Label label2 = MakeLabel(imagePanel, "FreeHint", 10, Dim, _font);
		label2.Text = Strings.Parties.FreeSlotHint;
		label2.SetSize(438, 20);
		label2.SetPosition(76, 40);
		return imagePanel;
	}

	private void RefreshVitals()
	{
		List<IPartyMember> list = Globals.Me?.Party;
		if (list == null)
		{
			return;
		}
		foreach (var vitalRow2 in _vitalRows)
		{
			Guid id = vitalRow2.Id;
			VitalRow[] item = vitalRow2.Rows;
			IPartyMember partyMember = list.FirstOrDefault((IPartyMember member) => member.Id == id);
			if (partyMember != null)
			{
				for (int num = 0; num < item.Length && num < partyMember.Vital.Length; num++)
				{
					long num2 = Math.Max(1L, partyMember.MaxVital[num]);
					long num3 = Math.Clamp(partyMember.Vital[num], 0L, num2);
					float num4 = (float)num3 / (float)num2;
					VitalRow vitalRow = item[num];
					vitalRow.Fill.SetSize(Math.Max(10, (int)((float)vitalRow.Track.Width * num4)), 12);
					vitalRow.Fill.IsHidden = num3 <= 0;
					vitalRow.Value.Text = $"{num3:N0} / {num2:N0}".Replace(',', ' ');
				}
			}
		}
	}

	private static bool IsOutOfRange(IPartyMember member)
	{
		Player me = Globals.Me;
		if (me == null || member.Id == me.Id)
		{
			return false;
		}
		if (!Globals.Entities.TryGetValue(member.Id, out Entity value) || value == null)
		{
			return false;
		}
		if (value.MapId != me.MapId)
		{
			return false;
		}
		int num = Math.Max(Math.Abs(value.X - me.X), Math.Abs(value.Y - me.Y));
		return num > Options.Instance.Party.SharedXpRange;
	}

	private static void DrawPortrait(Base slot, string sprite)
	{
		SpriteSheets.Pose? pose = SpriteSheets.StandingBody(sprite);
		if (pose.HasValue)
		{
			SpriteSheets.Pose valueOrDefault = pose.GetValueOrDefault();
			if (!valueOrDefault.IsEmpty)
			{
				int item = valueOrDefault.Frame.Width;
				int item2 = valueOrDefault.Frame.Height;
				ImagePanel imagePanel = new ImagePanel(slot, "Sprite")
				{
					MouseInputEnabled = false
				};
				imagePanel.Texture = valueOrDefault.Texture;
				imagePanel.SetTextureRect(valueOrDefault.Frame.X, valueOrDefault.Frame.Y, item, item2);
				imagePanel.SetSize(item, item2);
				imagePanel.SetPosition((50 - item) / 2, 46 - item2);
			}
		}
	}

	private void SubmitInvite()
	{
		string text = _invite.Text?.Trim();
		if (!string.IsNullOrEmpty(text))
		{
			PacketSender.SendPartyInvite(text);
			_invite.SetText(string.Empty);
			_inviteHint.IsHidden = false;
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
		_sidebarHeading.SetSize(156, 20);
		_sidebarHeading.SetPosition(30, Y(80));
		_shareCard.SetSize(168, 104);
		_shareCard.SetPosition(24, Y(104));
		_shareLabel.SetSize(140, 18);
		_shareLabel.SetPosition(14, 8);
		_shareValue.SetSize(140, 44);
		_shareValue.SetPosition(14, 26);
		_shareHint.SetSize(140, 20);
		_shareHint.SetPosition(14, 72);
		_rangeCard.SetSize(168, 82);
		_rangeCard.SetPosition(24, Y(220));
		_rangeLabel.SetSize(140, 18);
		_rangeLabel.SetPosition(14, 8);
		_rangeValue.SetSize(140, 26);
		_rangeValue.SetPosition(14, 28);
		_rangeHint.SetSize(140, 20);
		_rangeHint.SetPosition(14, 54);
		_leaveButton.SetPosition(33, Y(550));
		_membersHeading.SetSize(200, 20);
		_membersHeading.SetPosition(216, Y(80));
		_membersCount.SetSize(100, 20);
		_membersCount.SetPosition(644, Y(80));
		_aloneTitle.SetSize(528, 26);
		_aloneTitle.SetPosition(216, Y(112));
		_aloneHint.SetSize(488, 60);
		_aloneHint.SetPosition(216, Y(140));
		_inviteBack.SetPosition(216, Y(550));
		_inviteHint.SetSize(240, 22);
		_inviteHint.SetPosition(12, 3);
		_inviteButton.SetPosition(488, Y(547));
	}

	private static Intersect.Client.Framework.Gwen.Control.Label MakeLabel(Base parent, string name, int size, Color color, IFont? font)
	{
		Intersect.Client.Framework.Gwen.Control.Label label = new Intersect.Client.Framework.Gwen.Control.Label(parent, name)
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

	private static ImagePanel MakeCard(Base parent, string name)
	{
		return new ImagePanel(parent, name)
		{
			Texture = Globals.ContentManager.GetTexture(TextureType.Gui, "saiyan_card.png"),
			TextureNinePatchMargin = new Margin(8),
			MouseInputEnabled = false
		};
	}

	private static (ImagePanel Track, ImagePanel Fill) MakeBar(Base parent, string name, string fillTexture)
	{
		ImagePanel imagePanel = new ImagePanel(parent, name + "Track")
		{
			TextureFilename = "saiyan_bar_track.png",
			TextureNinePatchMargin = new Margin(5),
			MouseInputEnabled = false
		};
		ImagePanel item = new ImagePanel(imagePanel, name + "Fill")
		{
			TextureFilename = fillTexture,
			TextureNinePatchMargin = new Margin(5),
			MouseInputEnabled = false
		};
		return (Track: imagePanel, Fill: item);
	}

	private Button MakeCtaButton(Base parent, string name)
	{
		Button button = MakeButton(parent, name, 180, 34, "saiyan_cta");
		button.FontSize = 12;
		button.SetTextColor(Night, ComponentState.Normal);
		button.SetTextColor(Night, ComponentState.Hovered);
		button.SetTextColor(Night, ComponentState.Active);
		button.SetTextColor(Dim, ComponentState.Disabled);
		return button;
	}

	private Button MakeButton(Base parent, string name, int width, int height, string famille = "saiyan_btn")
	{
		Button button = new Button(parent, name)
		{
			Font = _font,
			FontSize = 11,
			TextAlign = Pos.Center
		};
		string text = $"{famille}_{width}x{height}";
		button.SetStateTexture(ComponentState.Normal, text + ".png");
		button.SetStateTexture(ComponentState.Hovered, text + "_hover.png");
		button.SetStateTexture(ComponentState.Active, text + "_clicked.png");
		button.SetStateTexture(ComponentState.Disabled, text + "_disabled.png");
		button.SetTextColor(White, ComponentState.Normal);
		button.SetTextColor(OrangeLight, ComponentState.Hovered);
		button.SetTextColor(Night, ComponentState.Active);
		button.SetTextColor(Dim, ComponentState.Disabled);
		button.SetSize(width, height);
		return button;
	}

	private (ImagePanel Back, TextBox Box) MakeField(Base parent, string name, int width)
	{
		ImagePanel imagePanel = new ImagePanel(parent, name + "Back")
		{
			Texture = Globals.ContentManager.GetTexture(TextureType.Gui, $"saiyan_field_{width}x{28}.png")
		};
		imagePanel.SetSize(width, 28);
		TextBox textBox = new TextBox(imagePanel, name)
		{
			Font = _font,
			FontSize = 11,
			ShouldDrawBackground = false
		};
		textBox.SetTextColor(White, ComponentState.Normal);
		textBox.SetSize(width - 20, 22);
		textBox.SetPosition(10, 3);
		textBox.MaximumLength = 32;
		return (Back: imagePanel, Box: textBox);
	}
}
