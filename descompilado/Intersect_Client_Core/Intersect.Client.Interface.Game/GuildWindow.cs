using System;
using System.Collections.Generic;
using System.Linq;
using Intersect.Client.Entities;
using Intersect.Client.Framework.Content;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.Framework.Gwen;
using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.Framework.Gwen.Control.EventArguments;
using Intersect.Client.Framework.Gwen.Control.EventArguments.InputSubmissionEvent;
using Intersect.Client.Framework.Input;
using Intersect.Client.General;
using Intersect.Client.Interface.Shared;
using Intersect.Client.Localization;
using Intersect.Client.Networking;
using Intersect.Config.Guilds;
using Intersect.Localization;
using Intersect.Network.Packets.Server;

namespace Intersect.Client.Interface.Game;

public class GuildWindow : WindowControl
{
	private const int WindowWidth = 760;

	private const int WindowHeight = 600;

	private const int HeaderHeight = 56;

	private new const int Margin = 16;

	private const int RowHeight = 38;

	private const int RowGap = 4;

	private const int FieldHeight = 28;

	private const int FooterHeight = 44;

	private const int DirectoryPageSize = 8;

	private static readonly Color OrangeLight = new Color(255, 255, 172, 82);

	private static readonly Color Gold = new Color(255, 245, 190, 90);

	private static readonly Color White = new Color(255, 242, 242, 242);

	private static readonly Color Soft = new Color(255, 196, 204, 220);

	private static readonly Color Muted = new Color(255, 176, 186, 206);

	private static readonly Color Dim = new Color(255, 150, 160, 182);

	private static readonly Color Night = new Color(255, 11, 14, 23);

	private static readonly Color Green = new Color(255, 120, 200, 140);

	private static readonly Color Red = new Color(255, 225, 95, 95);

	private readonly Canvas _gameCanvas;

	private readonly IFont? _font;

	private readonly IFont? _displayFont;

	private readonly Label _title;

	private readonly Label _subtitle;

	private readonly Button _close;

	private readonly ImagePanel _createPanel;

	private readonly Label _createLabel;

	private readonly Label _createHint;

	private readonly ImagePanel _createFieldBack;

	private readonly TextBox _createField;

	private readonly Button _createButton;

	private readonly ImagePanel _motdPanel;

	private readonly Label _motdLabel;

	private readonly Label _motdText;

	private readonly Button _motdEditButton;

	private readonly Label _membersLabel;

	private readonly ImagePanel _inviteFieldBack;

	private readonly TextBox _inviteField;

	private readonly Button _inviteButton;

	private readonly ScrollControl _memberList;

	private readonly Label _journalLabel;

	private readonly ScrollControl _journalList;

	private readonly Label _directoryLabel;

	private readonly ImagePanel _searchFieldBack;

	private readonly TextBox _searchField;

	private readonly ScrollControl _directoryList;

	private readonly Label _directoryPageLabel;

	private readonly Button _directoryPrevious;

	private readonly Button _directoryNext;

	private readonly Button _leaveButton;

	private readonly Button _disbandButton;

	private readonly Button _directoryButton;

	private readonly Button _backButton;

	private readonly ContextMenu _contextMenu;

	private readonly MenuItem _privateMessageOption;

	private readonly MenuItem[] _promoteOptions;

	private readonly MenuItem[] _demoteOptions;

	private readonly MenuItem _kickOption;

	private readonly MenuItem _transferOption;

	private readonly List<Base> _rows = new List<Base>();

	private GuildMember? _selectedMember;

	private bool _browsing;

	private bool _needsRebuild = true;

	private int _laidOutTitleBarHeight;

	public bool IsOpen => !base.IsHidden;

	private new int InnerWidth => (_memberList.Parent ?? this).Width;

	private int HeaderBottom => 56 - _laidOutTitleBarHeight;

	private int ContentTop => HeaderBottom + 16 + 8;

	private int ContentBottom
	{
		get
		{
			Base obj = _memberList.Parent ?? this;
			int num = Math.Min(obj.Height, 600 - _laidOutTitleBarHeight);
			return num - 16 - 8;
		}
	}

	public GuildWindow(Canvas gameCanvas)
		: base(gameCanvas, string.Empty, modal: false, "GuildWindow")
	{
		_gameCanvas = gameCanvas;
		_font = Globals.ContentManager.GetFont("chakrapetch");
		_displayFont = Globals.ContentManager.GetFont("rajdhanibold");
		DisableResizing();
		SetSize(760, 600);
		SetPosition((_gameCanvas.Width - 760) / 2, (_gameCanvas.Height - 600) / 2);
		SetImage(Globals.ContentManager.GetTexture(TextureType.Gui, "saiyan_window_clan.png"), "saiyan_window_clan.png", ControlState.Active);
		SetImage(Globals.ContentManager.GetTexture(TextureType.Gui, "saiyan_window_clan.png"), "saiyan_window_clan.png", ControlState.Inactive);
		base.IsClosable = false;
		if (base.Titlebar is Titlebar titlebar)
		{
			titlebar.Label.IsHidden = true;
		}
		_title = MakeLabel(this, "Title", 20, White, display: true);
		_title.Font = Globals.ContentManager.GetFont("refuel");
		WindowTitle.Apply(_title);
		_subtitle = MakeLabel(this, "Subtitle", 10, Muted);
		_close = new Button(this, "Close", disableText: true);
		_close.SetStateTexture(ComponentState.Normal, "saiyan_close.png");
		_close.SetStateTexture(ComponentState.Hovered, "saiyan_close_hover.png");
		_close.SetStateTexture(ComponentState.Active, "saiyan_close_clicked.png");
		_close.Clicked += delegate
		{
			Close();
		};
		_createPanel = MakeCard(this, "CreatePanel");
		_createLabel = MakeLabel(_createPanel, "CreateLabel", 14, White, display: true);
		_createLabel.Text = Strings.Guilds.Found.ToString().ToUpperInvariant();
		_createHint = MakeLabel(_createPanel, "CreateHint", 10, Muted);
		(_createFieldBack, _createField) = MakeField(_createPanel, "CreateField", 260);
		_createField.SubmitPressed += delegate
		{
			SubmitCreate();
		};
		_createButton = MakeCtaButton(_createPanel, "CreateButton");
		_createButton.Text = Strings.Guilds.FoundButton;
		_createButton.Clicked += delegate
		{
			SubmitCreate();
		};
		_motdPanel = MakeCard(this, "MotdPanel");
		_motdLabel = MakeLabel(_motdPanel, "MotdLabel", 14, White, display: true);
		_motdLabel.Text = Strings.Guilds.Motd.ToString().ToUpperInvariant();
		_motdText = MakeLabel(_motdPanel, "MotdText", 10, Muted);
		_motdText.WrappingBehavior = WrappingBehavior.Wrapped;
		_motdEditButton = MakeButton(_motdPanel, "MotdEdit", 84, 28);
		_motdEditButton.Text = Strings.Guilds.MotdEdit;
		_motdEditButton.Clicked += delegate
		{
			EditMotd();
		};
		_membersLabel = MakeLabel(this, "MembersLabel", 14, White, display: true);
		(_inviteFieldBack, _inviteField) = MakeField(this, "InviteField", 260);
		_inviteField.SubmitPressed += delegate
		{
			SubmitInvite();
		};
		_inviteButton = MakeButton(this, "InviteButton", 84, 28);
		_inviteButton.Text = Strings.Guilds.Invite;
		_inviteButton.Clicked += delegate
		{
			SubmitInvite();
		};
		_memberList = MakeList(this, "MemberList");
		_journalLabel = MakeLabel(this, "JournalLabel", 14, White, display: true);
		_journalLabel.Text = Strings.Guilds.Journal.ToString().ToUpperInvariant();
		_journalList = MakeList(this, "JournalList");
		_directoryLabel = MakeLabel(this, "DirectoryLabel", 14, White, display: true);
		_directoryLabel.Text = Strings.Guilds.Directory.ToString().ToUpperInvariant();
		(_searchFieldBack, _searchField) = MakeField(this, "SearchField", 260);
		_searchField.SubmitPressed += delegate
		{
			RequestDirectory(0);
		};
		_directoryList = MakeList(this, "DirectoryList");
		_directoryPageLabel = MakeLabel(this, "DirectoryPage", 10, Muted);
		_directoryPageLabel.TextAlign = Pos.Center;
		_directoryPrevious = MakeButton(this, "DirectoryPrevious", 28, 28);
		_directoryPrevious.Text = Strings.Guilds.DirectoryPrevious;
		_directoryPrevious.Clicked += delegate
		{
			RequestDirectory((Globals.Me?.GuildDirectoryPage ?? 0) - 1);
		};
		_directoryNext = MakeButton(this, "DirectoryNext", 28, 28);
		_directoryNext.Text = Strings.Guilds.DirectoryNext;
		_directoryNext.Clicked += delegate
		{
			RequestDirectory((Globals.Me?.GuildDirectoryPage ?? 0) + 1);
		};
		_leaveButton = MakeButton(this, "LeaveButton", 150, 28);
		_leaveButton.Text = Strings.Guilds.Leave;
		_leaveButton.Clicked += delegate
		{
			ConfirmLeave();
		};
		_disbandButton = MakeButton(this, "DisbandButton", 150, 28);
		_disbandButton.Text = Strings.Guilds.Disband;
		_disbandButton.SetTextColor(Red, ComponentState.Normal);
		_disbandButton.Clicked += delegate
		{
			ConfirmDisband();
		};
		_directoryButton = MakeButton(this, "DirectoryButton", 150, 28);
		_directoryButton.Text = Strings.Guilds.DirectoryButton;
		_directoryButton.Clicked += delegate
		{
			_browsing = true;
			RequestDirectory(0);
			Layout();
			_needsRebuild = true;
		};
		_backButton = MakeButton(this, "BackButton", 150, 28);
		_backButton.Text = Strings.Guilds.DirectoryBack;
		_backButton.Clicked += delegate
		{
			_browsing = false;
			Layout();
			_needsRebuild = true;
		};
		_contextMenu = new ContextMenu(gameCanvas, "GuildContextMenu")
		{
			IsHidden = true,
			IconMarginDisabled = true
		};
		_contextMenu.ClearChildren();
		_contextMenu.ItemFont = _font;
		_contextMenu.ItemFontSize = 11;
		_privateMessageOption = _contextMenu.AddItem(Strings.Guilds.PM);
		_privateMessageOption.Clicked += delegate
		{
			GuildMember? selectedMember = _selectedMember;
			if (selectedMember != null && selectedMember.Online && _selectedMember?.Id != Globals.Me?.Id)
			{
				Interface.GameUi.SetChatboxText("/pm " + _selectedMember.Name + " ");
			}
		};
		int num = Options.Instance.Guild.Ranks.Length;
		_promoteOptions = new MenuItem[num - 2];
		for (int num2 = 1; num2 < num - 1; num2++)
		{
			_promoteOptions[num2 - 1] = _contextMenu.AddItem(Strings.Guilds.Promote.ToString(Options.Instance.Guild.Ranks[num2].Title));
			_promoteOptions[num2 - 1].UserData = num2;
			_promoteOptions[num2 - 1].Clicked += PromoteClicked;
		}
		_demoteOptions = new MenuItem[num - 2];
		for (int num3 = 2; num3 < num; num3++)
		{
			_demoteOptions[num3 - 2] = _contextMenu.AddItem(Strings.Guilds.Demote.ToString(Options.Instance.Guild.Ranks[num3].Title));
			_demoteOptions[num3 - 2].UserData = num3;
			_demoteOptions[num3 - 2].Clicked += DemoteClicked;
		}
		_kickOption = _contextMenu.AddItem(Strings.Guilds.Kick);
		_kickOption.Clicked += KickClicked;
		_transferOption = _contextMenu.AddItem(Strings.Guilds.Transfer);
		_transferOption.Clicked += TransferClicked;
		foreach (MenuItem item in _contextMenu.Children.OfType<MenuItem>())
		{
			item.SetTextColor(White, ComponentState.Normal);
			item.SetTextColor(OrangeLight, ComponentState.Hovered);
		}
		Layout();
		Hide();
	}

	public void UpdateList()
	{
		_needsRebuild = true;
	}

	public void Open()
	{
		SetPosition((_gameCanvas.Width - 760) / 2, (_gameCanvas.Height - 600) / 2);
		Layout();
		_needsRebuild = true;
		Show();
		BringToFront();
	}

	public void Toggle()
	{
		if (IsOpen)
		{
			Hide();
		}
		else
		{
			Open();
		}
	}

	public override void Hide()
	{
		_contextMenu?.Close();
		base.Hide();
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
			if (!_createPanel.IsHidden)
			{
				RefreshFounding();
			}
		}
	}

	private void RefreshFounding()
	{
		int creationCost = Options.Instance.Guild.CreationCost;
		int creationLevel = Options.Instance.Guild.CreationLevel;
		bool flag = (Globals.Me?.Level ?? 0) < creationLevel;
		long num = ((creationCost > 0) ? Math.Max(0L, creationCost - (Globals.Me?.Zeni ?? 0)) : 0);
		string text = (flag ? Strings.Guilds.FoundNeedsLevel.ToString(creationLevel) : ((num <= 0) ? ((creationCost > 0) ? Strings.Guilds.FoundCost.ToString(MarketWindow.FormatAmount(creationCost), Strings.Guilds.Currency, creationLevel) : Strings.Guilds.FoundCostFree.ToString(creationLevel)) : Strings.Guilds.FoundNeedsZeni.ToString(MarketWindow.FormatAmount(num), Strings.Guilds.Currency)));
		bool flag2 = !flag && num == 0;
		if (_createHint.Text != text)
		{
			_createHint.Text = text;
		}
		_createHint.SetTextColor(flag2 ? Muted : Red, ComponentState.Normal);
		_createButton.IsDisabled = !flag2;
	}

	private void SubmitCreate()
	{
		if (_createButton.IsDisabled)
		{
			return;
		}
		string name = _createField.Text?.Trim();
		if (name != null && name.Length >= 3)
		{
			int creationCost = Options.Instance.Guild.CreationCost;
			ConfirmCardRequest confirmCardRequest = new ConfirmCardRequest()
			{
				Title = Strings.ConfirmCard.FoundTitle,
				Icon = "saiyan_carte_icone_clan.png",
				Heading = name,
				Prompt = Strings.ConfirmCard.FoundPrompt,
				Chip = Strings.ConfirmCard.FoundCost.ToString(creationCost, Strings.Guilds.Currency),
				ConfirmLabel = Strings.ConfirmCard.Found,
				Emblem = Globals.ContentManager.GetTexture(TextureType.Gui, "saiyan_embleme_clan.png"),
			};
			ConfirmCardWindow.Open(confirmCardRequest, delegate
			{
				PacketSender.SendGuildCreate(name);
			});
		}
	}

	private void SubmitInvite()
	{
		string text = _inviteField.Text?.Trim();
		if (text != null && text.Length >= 3)
		{
			PacketSender.SendInviteGuild(text);
			_inviteField.Text = string.Empty;
		}
	}

	private void EditMotd()
	{
		new InputBox(Strings.Guilds.MotdTitle, Strings.Guilds.MotdPrompt.ToString(Globals.Me?.Guild), InputType.TextInput, delegate(Base sender, InputSubmissionEventArgs args)
		{
			if (sender is InputBox && args.Value is StringSubmissionValue stringSubmissionValue)
			{
				PacketSender.SendGuildMotd(stringSubmissionValue.Value?.Trim() ?? string.Empty);
			}
		}).Focus();
	}

	private void ConfirmLeave()
	{
		ConfirmCardWindow.Open(new ConfirmCardRequest
		{
			Title = Strings.ConfirmCard.LeaveGuildTitle,
			Icon = "saiyan_carte_icone_clan.png",
			Heading = (Globals.Me?.Guild ?? string.Empty),
			Prompt = Strings.ConfirmCard.LeaveGuildPrompt,
			ConfirmLabel = Strings.ConfirmCard.LeaveGuild,
			Danger = true,
			Emblem = Globals.ContentManager.GetTexture(TextureType.Gui, "saiyan_embleme_clan.png")
		}, delegate
		{
			PacketSender.SendLeaveGuild();
		});
	}

	private void ConfirmDisband()
	{
		new InputBox(Strings.Guilds.DisbandTitle, Strings.Guilds.DisbandPrompt.ToString(Globals.Me?.Guild), InputType.TextInput, delegate(Base sender, InputSubmissionEventArgs args)
		{
			if (sender is InputBox && args.Value is StringSubmissionValue stringSubmissionValue && stringSubmissionValue.Value?.Trim() == Globals.Me?.Guild)
			{
				PacketSender.SendGuildDisband();
			}
		}).Focus();
	}

	private void RequestDirectory(int page)
	{
		int num = Globals.Me?.GuildDirectoryTotal ?? 0;
		int num2 = ((num > 0) ? ((num - 1) / 8) : 0);
		if (page >= 0 && page <= num2)
		{
			PacketSender.SendGuildDirectory(_searchField.Text?.Trim() ?? string.Empty, page);
		}
	}

	private void PromoteClicked(Base sender, MouseButtonState arguments)
	{
		ChangeRank(sender, promote: true);
	}

	private void DemoteClicked(Base sender, MouseButtonState arguments)
	{
		ChangeRank(sender, promote: false);
	}

	private void ChangeRank(Base sender, bool promote)
	{
		if (Globals.Me?.GuildRank == null || _selectedMember == null || (Globals.Me.Rank != 0 && !(promote ? Globals.Me.GuildRank.Permissions.Promote : Globals.Me.GuildRank.Permissions.Demote)) || _selectedMember.Rank <= Globals.Me.Rank)
		{
			return;
		}
		int newRank = (int)sender.UserData;
		GuildMember member = _selectedMember;
		ConfirmCardRequest confirmCardRequest = new ConfirmCardRequest()
		{
			Title = (promote ? Strings.ConfirmCard.PromoteTitle : Strings.ConfirmCard.DemoteTitle),
			Icon = "saiyan_carte_icone_clan.png",
			Heading = member.Name,
			Prompt = Strings.ConfirmCard.RankPrompt.ToString(Options.Instance.Guild.Ranks[newRank].Title),
			ConfirmLabel = (promote ? Strings.ConfirmCard.Promote : Strings.ConfirmCard.Demote),
			Danger = !promote,
			PlayerName = member.Name,
		};
		ConfirmCardWindow.Open(confirmCardRequest, delegate
		{
			if (promote)
			{
				PacketSender.SendPromoteGuildMember(member.Id, newRank);
			}
			else
			{
				PacketSender.SendDemoteGuildMember(member.Id, newRank);
			}
		});
	}

	private void KickClicked(Base sender, MouseButtonState arguments)
	{
		if (Globals.Me?.GuildRank != null && _selectedMember != null && (Globals.Me.GuildRank.Permissions.Kick || Globals.Me.Rank == 0) && _selectedMember.Rank > Globals.Me.Rank)
		{
			GuildMember kicked = _selectedMember;
			ConfirmCardWindow.Open(new ConfirmCardRequest
			{
				Title = Strings.ConfirmCard.KickTitle,
				Icon = "saiyan_carte_icone_clan.png",
				Heading = kicked.Name,
				Prompt = Strings.ConfirmCard.KickPrompt,
				ConfirmLabel = Strings.ConfirmCard.Kick,
				Danger = true,
				PlayerName = kicked.Name
			}, delegate
			{
				PacketSender.SendKickGuildMember(kicked.Id);
			});
		}
	}

	private void TransferClicked(Base sender, MouseButtonState arguments)
	{
		if (Globals.Me?.GuildRank == null || _selectedMember == null || Globals.Me.Rank != 0)
		{
			return;
		}
		new InputBox(Strings.Guilds.TransferTitle, Strings.Guilds.TransferToMemberPrompt.ToString(_selectedMember.Name, Globals.Me.GuildRank.Title, Globals.Me.Guild), InputType.TextInput, delegate(Base obj, InputSubmissionEventArgs args)
		{
			if (obj is InputBox inputBox && args.Value is StringSubmissionValue stringSubmissionValue && stringSubmissionValue.Value?.Trim() == Globals.Me?.Guild && inputBox.UserData is GuildMember guildMember)
			{
				PacketSender.SendTransferGuild(guildMember.Id);
			}
		}, null, _selectedMember);
	}

	private void MemberClicked(Base sender, MouseButtonState arguments)
	{
		if (!(sender.UserData is GuildMember guildMember) || Globals.Me == null || guildMember.Id == Globals.Me.Id)
		{
			return;
		}
		if (arguments.MouseButton == MouseButton.Left)
		{
			if (guildMember.Online)
			{
				Interface.GameUi.SetChatboxText("/pm " + guildMember.Name + " ");
			}
		}
		else
		{
			if (arguments.MouseButton != MouseButton.Right || Globals.Me.GuildRank == null)
			{
				return;
			}
			_selectedMember = guildMember;
			Base[] array = _contextMenu.Children.ToArray();
			foreach (Base child in array)
			{
				_contextMenu.RemoveChild(child, dispose: false);
			}
			GuildRank guildRank = Globals.Me.GuildRank;
			int rank = Globals.Me.Rank;
			bool flag = rank == 0;
			if (guildMember.Online)
			{
				_contextMenu.AddChild(_privateMessageOption);
			}
			MenuItem[] promoteOptions = _promoteOptions;
			foreach (MenuItem menuItem in promoteOptions)
			{
				if ((flag || guildRank.Permissions.Promote) && (int)menuItem.UserData > rank && (int)menuItem.UserData < guildMember.Rank && guildMember.Rank > rank)
				{
					_contextMenu.AddChild(menuItem);
				}
			}
			MenuItem[] demoteOptions = _demoteOptions;
			foreach (MenuItem menuItem2 in demoteOptions)
			{
				if ((flag || guildRank.Permissions.Demote) && (int)menuItem2.UserData > rank && (int)menuItem2.UserData > guildMember.Rank && guildMember.Rank > rank)
				{
					_contextMenu.AddChild(menuItem2);
				}
			}
			if ((guildRank.Permissions.Kick || flag) && guildMember.Rank > rank)
			{
				_contextMenu.AddChild(_kickOption);
			}
			if (flag)
			{
				_contextMenu.AddChild(_transferOption);
			}
			if (_contextMenu.Children.Count > 0)
			{
				_contextMenu.Open(Pos.None);
			}
		}
	}

	private Label MakeLabel(Base parent, string name, int size, Color color, bool display = false)
	{
		Label label = new Label(parent, name)
		{
			Font = (display ? _displayFont : _font),
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
			ShouldDrawBackground = false,
			MaximumLength = 32
		};
		textBox.SetTextColor(White, ComponentState.Normal);
		textBox.SetSize(width - 16, 22);
		textBox.SetPosition(8, 3);
		Interface.FocusComponents.Add(textBox);
		return (Back: imagePanel, Box: textBox);
	}

	private ScrollControl MakeList(Base parent, string name)
	{
		ScrollControl scrollControl = new ScrollControl(parent, name);
		scrollControl.EnableScroll(horizontal: false, vertical: true);
		scrollControl.VerticalScrollBar.SetBackgroundTemplate(null, "saiyan_scroll_bg.png");
		scrollControl.VerticalScrollBar.SetScrollBarImage(null, "saiyan_scroll_bar.png", ComponentState.Normal);
		scrollControl.VerticalScrollBar.SetScrollBarImage(null, "saiyan_scroll_bar_hover.png", ComponentState.Hovered);
		scrollControl.VerticalScrollBar.SetScrollBarImage(null, "saiyan_scroll_bar_hover.png", ComponentState.Active);
		return scrollControl;
	}

	private void Layout()
	{
		_laidOutTitleBarHeight = base.TitleBarBounds.Height;
		int innerWidth = InnerWidth;
		int headerBottom = HeaderBottom;
		int contentTop = ContentTop;
		int contentBottom = ContentBottom;
		int num = innerWidth - 32;
		_title.SetSize(460, 38);
		_title.SetPosition(24, headerBottom - 54);
		_subtitle.SetSize(460, 18);
		_subtitle.SetPosition(26, headerBottom - 20);
		_close.SetSize(24, 24);
		_close.SetPosition(innerWidth - 16 - 24, headerBottom - 38);
		bool flag = Globals.Me?.IsInGuild ?? false;
		int num2 = contentBottom - 44 + 12;
		_leaveButton.SetPosition(16, num2);
		_disbandButton.SetPosition(174, num2);
		_directoryButton.SetPosition(innerWidth - 16 - 150, num2);
		_backButton.SetPosition(innerWidth - 16 - 150, num2);
		_createPanel.SetSize(num, 104);
		_createPanel.SetPosition(16, contentTop);
		_createLabel.SetSize(300, 26);
		_createLabel.SetPosition(14, 8);
		_createHint.SetSize(num - 28, 18);
		_createHint.SetPosition(14, 34);
		_createFieldBack.SetPosition(14, 58);
		_createButton.SetPosition(286, 53);
		_motdPanel.SetSize(num, 84);
		_motdPanel.SetPosition(16, contentTop);
		_motdLabel.SetSize(300, 26);
		_motdLabel.SetPosition(14, 6);
		_motdEditButton.SetPosition(num - 84 - 14, 8);
		_motdText.SetSize(num - 28, 40);
		_motdText.SetPosition(14, 36);
		int num3 = contentTop + 96;
		int num4 = 396;
		int num5 = 16 + num4 + 16;
		int width = innerWidth - num5 - 16;
		_membersLabel.SetSize(300, 26);
		_membersLabel.SetPosition(18, num3);
		_inviteFieldBack.SetPosition(16, num3 + 28);
		_inviteButton.SetPosition(284, num3 + 28);
		_memberList.SetPosition(16, num3 + 28 + 28 + 10);
		_memberList.SetSize(num4, num2 - 14 - _memberList.Y);
		_journalLabel.SetSize(300, 26);
		_journalLabel.SetPosition(num5 + 2, num3);
		_journalList.SetPosition(num5, num3 + 28);
		_journalList.SetSize(width, num2 - 14 - _journalList.Y);
		int num6 = (flag ? contentTop : (contentTop + 116));
		_directoryLabel.SetSize(300, 26);
		_directoryLabel.SetPosition(18, num6);
		_searchFieldBack.SetPosition(16, num6 + 28);
		int num7 = num2 - 8;
		_directoryNext.SetPosition(innerWidth - 16 - 28, num7 - 28);
		_directoryPageLabel.SetSize(160, 28);
		_directoryPageLabel.SetPosition(innerWidth - 16 - 28 - 160, num7 - 28);
		_directoryPrevious.SetPosition(innerWidth - 16 - 28 - 160 - 28, num7 - 28);
		_directoryList.SetPosition(16, num6 + 28 + 28 + 10);
		_directoryList.SetSize(num, num7 - 36 - _directoryList.Y);
	}

	private void Rebuild()
	{
		foreach (Base row in _rows)
		{
			row.Parent?.RemoveChild(row, dispose: true);
		}
		_rows.Clear();
		bool flag = Globals.Me?.IsInGuild ?? false;
		int num;
		if (flag)
		{
			Player? me = Globals.Me;
			num = ((me != null && me.Rank == 0) ? 1 : 0);
		}
		else
		{
			num = 0;
		}
		bool flag2 = (byte)num != 0;
		bool flag3 = flag && !_browsing;
		bool flag4 = !flag && !_browsing;
		bool flag5 = _browsing || !flag;
		_title.Text = ((!flag) ? ((string)Strings.Guilds.NoGuildTitle) : Globals.Me?.Guild);
		Label subtitle = _subtitle;
		string text;
		if (!flag)
		{
			text = Strings.Guilds.DirectoryHint;
		}
		else
		{
			LocalizedString subtitle2 = Strings.Guilds.Subtitle;
			object[] obj = new object[2]
			{
				Globals.Me?.GuildRank?.Title,
				null
			};
			Player? me2 = Globals.Me;
			obj[1] = ((me2 != null) ? me2.GuildMembers.Length : 0);
			text = subtitle2.ToString(obj);
		}
		subtitle.Text = text;
		RefreshFounding();
		_createPanel.IsHidden = !flag4;
		_motdPanel.IsHidden = !flag3;
		_membersLabel.IsHidden = !flag3;
		_inviteFieldBack.IsHidden = !flag3 || !CanInvite();
		_inviteButton.IsHidden = !flag3 || !CanInvite();
		_memberList.IsHidden = !flag3;
		_journalLabel.IsHidden = !flag3;
		_journalList.IsHidden = !flag3;
		_leaveButton.IsHidden = !flag3 || flag2;
		_disbandButton.IsHidden = !flag3 || !flag2;
		_directoryLabel.IsHidden = !flag5;
		_searchFieldBack.IsHidden = !flag5;
		_directoryList.IsHidden = !flag5;
		_directoryPageLabel.IsHidden = !flag5;
		_directoryPrevious.IsHidden = !flag5;
		_directoryNext.IsHidden = !flag5;
		_directoryButton.IsHidden = !flag || _browsing;
		_backButton.IsHidden = !_browsing;
		Button motdEditButton = _motdEditButton;
		int isHidden;
		if (flag3)
		{
			if (!flag2)
			{
				Player? me3 = Globals.Me;
				isHidden = ((me3 == null || me3.GuildRank?.Permissions.Promote != true) ? 1 : 0);
			}
			else
			{
				isHidden = 0;
			}
		}
		else
		{
			isHidden = 1;
		}
		motdEditButton.IsHidden = (byte)isHidden != 0;
		if (flag3)
		{
			BuildMotd();
			BuildMembers();
			BuildJournal();
		}
		if (flag5)
		{
			BuildDirectory();
		}
	}

	private static bool CanInvite()
	{
		Player? me = Globals.Me;
		if (me == null || me.GuildRank?.Permissions.Invite != true)
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

	private void BuildMotd()
	{
		string text = Globals.Me?.GuildMotd ?? string.Empty;
		bool flag = string.IsNullOrWhiteSpace(text);
		_motdText.Text = (flag ? ((string)Strings.Guilds.MotdEmpty) : text);
		_motdText.FontSize = (flag ? 11 : 10);
		_motdText.SetTextColor(flag ? Dim : Muted, ComponentState.Normal);
	}

	private void BuildMembers()
	{
		GuildMember[] array = Globals.Me?.GuildMembers ?? Array.Empty<GuildMember>();
		_membersLabel.Text = Strings.Guilds.Members.ToString(array.Length).ToUpperInvariant();
		int num = _memberList.Width - 12;
		for (int i = 0; i < array.Length; i++)
		{
			GuildMember guildMember = array[i];
			ImagePanel card = new ImagePanel(_memberList, $"Member_{guildMember.Id}")
			{
				Texture = Globals.ContentManager.GetTexture(TextureType.Gui, "saiyan_card.png"),
				TextureNinePatchMargin = new Margin(8)
			};
			card.SetSize(num, 38);
			card.SetPosition(0, i * 42);
			card.UserData = guildMember;
			card.Clicked += MemberClicked;
			card.HoverEnter += delegate
			{
				card.Texture = Globals.ContentManager.GetTexture(TextureType.Gui, "saiyan_card_hover.png");
			};
			card.HoverLeave += delegate
			{
				card.Texture = Globals.ContentManager.GetTexture(TextureType.Gui, "saiyan_card.png");
			};
			card.TooltipFontSize = 10;
			card.TooltipFont = _font;
			card.TooltipTextColor = Soft;
			card.SetToolTipText(Strings.Guilds.Tooltip.ToString(guildMember.Level, guildMember.ClassName));
			ImagePanel imagePanel = new ImagePanel(card, "Online")
			{
				Texture = Globals.ContentManager.GetTexture(TextureType.Gui, "saiyan_card.png"),
				MouseInputEnabled = false,
				RenderColor = (guildMember.Online ? Green : Red)
			};
			imagePanel.SetSize(6, 6);
			imagePanel.SetPosition(12, 16);
			Label label = MakeLabel(card, "Name", 11, Soft);
			label.Text = guildMember.Name;
			label.SetSize(180, 20);
			label.SetPosition(26, 0);
			Label label2 = MakeLabel(card, "Place", 10, Muted);
			label2.Text = Traduction.Contenu(guildMember.MapName);
			label2.SetSize(180, 18);
			label2.SetPosition(26, 20);
			Label label3 = MakeLabel(card, "Rank", 10, (guildMember.Rank == 0) ? Gold : Muted);
			label3.Text = Options.Instance.Guild.Ranks[Math.Clamp(guildMember.Rank, 0, Options.Instance.Guild.Ranks.Length - 1)].Title;
			label3.TextAlign = Pos.Right | Pos.CenterV;
			label3.SetSize(150, 18);
			label3.SetPosition(num - 150 - 12, 10);
			_rows.Add(card);
		}
		if (array.Length == 0)
		{
			_rows.Add(EmptyNote(_memberList, Strings.Guilds.DirectoryEmpty));
		}
	}

	private void BuildJournal()
	{
		GuildActivityEntry[] array = Globals.Me?.GuildActivity ?? Array.Empty<GuildActivityEntry>();
		if (array.Length == 0)
		{
			_rows.Add(EmptyNote(_journalList, Strings.Guilds.JournalEmpty));
			return;
		}
		int num = _journalList.Width - 12;
		int num2 = 0;
		GuildActivityEntry[] array2 = array;
		foreach (GuildActivityEntry guildActivityEntry in array2)
		{
			Label label = MakeLabel(_journalList, "Age", 10, Muted);
			label.Text = DescribeAge(guildActivityEntry.TimeStamp);
			label.SetSize(72, 36);
			label.SetPosition(2, num2);
			_rows.Add(label);
			Label label2 = MakeLabel(_journalList, "Entry", 10, Muted);
			label2.WrappingBehavior = WrappingBehavior.Wrapped;
			label2.Text = Describe(guildActivityEntry);
			label2.SetSize(num - 78, 36);
			label2.SetPosition(76, num2);
			_rows.Add(label2);
			num2 += 36;
		}
	}

	private void BuildDirectory()
	{
		GuildDirectoryEntry[] array = Globals.Me?.GuildDirectory ?? Array.Empty<GuildDirectoryEntry>();
		int num = Globals.Me?.GuildDirectoryTotal ?? 0;
		int num2 = Globals.Me?.GuildDirectoryPage ?? 0;
		int num3 = ((num <= 0) ? 1 : ((num - 1) / 8 + 1));
		_directoryPageLabel.Text = Strings.Guilds.DirectoryPage.ToString(num, num2 + 1, num3);
		_directoryPrevious.IsDisabled = num2 <= 0;
		_directoryNext.IsDisabled = num2 >= num3 - 1;
		if (array.Length == 0)
		{
			_rows.Add(EmptyNote(_directoryList, Strings.Guilds.DirectoryEmpty));
			return;
		}
		int num4 = _directoryList.Width - 12;
		for (int i = 0; i < array.Length; i++)
		{
			GuildDirectoryEntry guildDirectoryEntry = array[i];
			ImagePanel card = new ImagePanel(_directoryList, $"Clan_{i}")
			{
				Texture = Globals.ContentManager.GetTexture(TextureType.Gui, "saiyan_card.png"),
				TextureNinePatchMargin = new Margin(8)
			};
			card.SetSize(num4, 44);
			card.SetPosition(0, i * 48);
			card.HoverEnter += delegate
			{
				card.Texture = Globals.ContentManager.GetTexture(TextureType.Gui, "saiyan_card_hover.png");
			};
			card.HoverLeave += delegate
			{
				card.Texture = Globals.ContentManager.GetTexture(TextureType.Gui, "saiyan_card.png");
			};
			Label label = MakeLabel(card, "Name", 11, Soft);
			label.Text = guildDirectoryEntry.Name;
			label.SetSize(280, 20);
			label.SetPosition(14, 3);
			Label label2 = MakeLabel(card, "Leader", 10, Muted);
			label2.Text = Strings.Guilds.DirectoryLeader.ToString(guildDirectoryEntry.Leader);
			label2.SetSize(280, 18);
			label2.SetPosition(14, 23);
			Label label3 = MakeLabel(card, "Members", 13, White, display: true);
			label3.Text = Strings.Guilds.DirectoryMembers.ToString(guildDirectoryEntry.Members);
			label3.TextAlign = Pos.Right | Pos.CenterV;
			label3.SetSize(160, 24);
			label3.SetPosition(num4 - 160 - 14, 1);
			Label label4 = MakeLabel(card, "Founded", 10, Muted);
			label4.Text = Strings.Guilds.DirectoryFounded.ToString(DescribeAge(guildDirectoryEntry.FoundedAt));
			label4.TextAlign = Pos.Right | Pos.CenterV;
			label4.SetSize(160, 18);
			label4.SetPosition(num4 - 160 - 14, 23);
			_rows.Add(card);
		}
	}

	private Label EmptyNote(Base parent, string text)
	{
		Label label = MakeLabel(parent, "Empty", 11, Dim);
		label.WrappingBehavior = WrappingBehavior.Wrapped;
		label.Text = text;
		label.SetSize(parent.Width - 16, 60);
		label.SetPosition(8, 8);
		return label;
	}

	private static string Describe(GuildActivityEntry entry)
	{
		return ((LocalizedString)(entry.Type switch
		{
			0 => Strings.Guilds.ActivityCreated, 
			1 => Strings.Guilds.ActivityDisbanded, 
			2 => Strings.Guilds.ActivityJoined, 
			3 => Strings.Guilds.ActivityLeft, 
			4 => Strings.Guilds.ActivityKicked, 
			5 => Strings.Guilds.ActivityPromoted, 
			6 => Strings.Guilds.ActivityDemoted, 
			7 => Strings.Guilds.ActivityTransfer, 
			8 => Strings.Guilds.ActivityRename, 
			9 => Strings.Guilds.ActivityMotd, 
			_ => null, 
		}))?.ToString(new object[3] { entry.Actor, entry.Target, entry.Meta }) ?? string.Empty;
	}

	private static string DescribeAge(long timeStamp)
	{
		double totalMinutes = (DateTime.UtcNow - DateTime.UnixEpoch.AddMilliseconds(timeStamp)).TotalMinutes;
		if (totalMinutes < 1.0)
		{
			return Strings.Guilds.JournalNow.ToString();
		}
		if (totalMinutes < 60.0)
		{
			return Strings.Guilds.JournalMinutes.ToString((int)totalMinutes);
		}
		if (totalMinutes < 1440.0)
		{
			return Strings.Guilds.JournalHours.ToString((int)(totalMinutes / 60.0));
		}
		return Strings.Guilds.JournalDays.ToString((int)(totalMinutes / 1440.0));
	}
}
