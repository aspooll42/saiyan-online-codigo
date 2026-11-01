using System;
using System.Collections.Generic;
using System.Linq;
using Intersect.Client.Core;
using Intersect.Client.Entities;
using Intersect.Client.Framework.Content;
using Intersect.Client.Framework.Entities;
using Intersect.Client.Framework.File_Management;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.Framework.Gwen;
using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.Framework.Gwen.Input;
using Intersect.Client.General;
using Intersect.Client.Interface.Game.EntityPanel;
using Intersect.Client.Interface.Shared;
using Intersect.Client.Localization;
using Intersect.Client.Networking;

namespace Intersect.Client.Interface.Game;

public class FriendsWindow : WindowControl
{
	private sealed class FriendCard
	{
		public required ImagePanel Row { get; init; }

		public List<Button> Buttons { get; } = new List<Button>();

		public List<ImagePanel> QuietIcons { get; } = new List<ImagePanel>();
	}

	private const string BodyFont = "chakrapetch";

	private const string LightFont = "chakrapetchmedium";

	private const string NameFont = "sairanom";

	private const int WindowWidth = 620;

	private const int WindowHeight = 560;

	private const int ListX = 20;

	private const int ListY = 128;

	private const int ListWidth = 592;

	private const int ListHeight = 376;

	private const int RowWidth = 572;

	private const int RowHeight = 58;

	private const int RowGap = 6;

	private const int SectionGap = 14;

	private const int ButtonY = 13;

	private static readonly Color White = new Color(255, 242, 242, 242);

	private static readonly Color Muted = new Color(255, 176, 184, 200);

	private static readonly Color Dim = new Color(255, 110, 120, 145);

	private static readonly Color DimSoft = new Color(200, 110, 120, 145);

	private static readonly Color Night = new Color(255, 11, 14, 23);

	private static readonly Color Orange = new Color(255, 255, 122, 26);

	private static readonly Color GoldLight = new Color(255, 255, 226, 150);

	private static readonly Color Gold = new Color(255, 245, 190, 90);

	private static readonly Color Green = new Color(255, 78, 200, 120);

	private static readonly Color GreenLight = new Color(255, 140, 235, 170);

	private static readonly Color Rule = new Color(180, 62, 74, 104);

	private static readonly Color GreyFrame = new Color(255, 70, 78, 96);

	private static readonly Color AbsentTint = new Color(210, 150, 156, 168);

	private readonly Canvas _gameCanvas;

	private readonly IFont? _font;

	private readonly IFont? _lightFont;

	private readonly IFont? _nameFont;

	private readonly IFont? _posterFont;

	private readonly Intersect.Client.Framework.Gwen.Control.Label _title;

	private readonly ImagePanel _onlineChip;

	private readonly Intersect.Client.Framework.Gwen.Control.Label _onlineChipText;

	private readonly Button _close;

	private readonly ImagePanel _fieldBack;

	private readonly ImagePanel _fieldIcon;

	private readonly TextBox _field;

	private readonly Intersect.Client.Framework.Gwen.Control.Label _fieldHint;

	private readonly Button _addButton;

	private readonly ScrollControl _list;

	private readonly Intersect.Client.Framework.Gwen.Control.Label _counts;

	private readonly Intersect.Client.Framework.Gwen.Control.Label _tip;

	private readonly List<Base> _rows = new List<Base>();

	private readonly List<FriendCard> _cards = new List<FriendCard>();

	private FriendCard? _hovered;

	private string _shownList = "?";

	private int _laidOutTitleBarHeight = -1;

	public bool IsVisible => !base.IsHidden;

	public FriendsWindow(Canvas gameCanvas)
		: base(gameCanvas, string.Empty, modal: false, "FriendsWindow")
	{
		_gameCanvas = gameCanvas;
		_font = GameContentManager.Current.GetFont("chakrapetch");
		_lightFont = GameContentManager.Current.GetFont("chakrapetchmedium") ?? _font;
		_nameFont = GameContentManager.Current.GetFont("sairanom") ?? _font;
		_posterFont = GameContentManager.Current.GetFont("refuel");
		DisableResizing();
		SetSize(620, 560);
		SetPosition((_gameCanvas.Width - 620) / 2, (_gameCanvas.Height - 560) / 2);
		IGameTexture texture = GameContentManager.Current.GetTexture(TextureType.Gui, "saiyan_amis_fenetre.png");
		SetImage(texture, "saiyan_amis_fenetre.png", ControlState.Active);
		SetImage(texture, "saiyan_amis_fenetre.png", ControlState.Inactive);
		base.IsClosable = false;
		if (base.Titlebar is Titlebar titlebar)
		{
			titlebar.Label.IsHidden = true;
		}
		_title = MakeLabel(this, "Title", 20, White, _posterFont);
		_title.Text = Strings.Friends.Title.ToString().ToUpperInvariant();
		WindowTitle.Apply(_title);
		_onlineChip = MakeImage(this, "OnlineChip", "saiyan_amis_puce.png", 60, 18);
		_onlineChip.TextureNinePatchMargin = new Margin(4);
		_onlineChipText = MakeLabel(_onlineChip, "OnlineChipText", 9, GreenLight, _font);
		_onlineChipText.TextAlign = Pos.Center;
		_close = new Button(this, "Close", disableText: true);
		_close.SetStateTexture(ComponentState.Normal, "saiyan_close.png");
		_close.SetStateTexture(ComponentState.Hovered, "saiyan_close_hover.png");
		_close.SetStateTexture(ComponentState.Active, "saiyan_close_clicked.png");
		_close.Clicked += delegate
		{
			Hide();
		};
		_fieldBack = MakeImage(this, "AddFieldBack", "saiyan_amis_champ.png", 440, 40);
		_fieldBack.MouseInputEnabled = true;
		_fieldIcon = MakeImage(_fieldBack, "AddFieldIcon", "saiyan_amis_icone_ajout.png", 22, 20);
		_fieldIcon.RenderColor = Dim;
		_fieldIcon.SetPosition(12, 10);
		_fieldHint = MakeLabel(_fieldBack, "AddFieldHint", 11, Dim, _lightFont);
		_fieldHint.Text = Strings.Friends.AddHint;
		_fieldHint.SetSize(380, 40);
		_fieldHint.SetPosition(44, 0);
		_field = new TextBox(_fieldBack, "AddField")
		{
			Font = _lightFont,
			FontSize = 11,
			ShouldDrawBackground = false,
			MaximumLength = 32
		};
		_field.SetTextColor(White, ComponentState.Normal);
		_field.SetSize(386, 34);
		_field.SetPosition(44, 3);
		_field.TextChanged += delegate
		{
			_fieldHint.IsHidden = !string.IsNullOrEmpty(_field.Text);
		};
		_field.SubmitPressed += delegate
		{
			SubmitAdd();
		};
		_addButton = new Button(this, "Add")
		{
			Font = (GameContentManager.Current.GetFont("rajdhanibold") ?? _font),
			FontSize = 13,
			TextAlign = Pos.Center,
			UsesHeadingFace = false
		};
		_addButton.SetStateTexture(ComponentState.Normal, "saiyan_amis_ajouter.png");
		_addButton.SetStateTexture(ComponentState.Hovered, "saiyan_amis_ajouter_hover.png");
		_addButton.SetStateTexture(ComponentState.Active, "saiyan_amis_ajouter_clicked.png");
		_addButton.SetTextColor(Night, ComponentState.Normal);
		_addButton.SetTextColor(Night, ComponentState.Hovered);
		_addButton.SetTextColor(Night, ComponentState.Active);
		_addButton.SetSize(128, 40);
		_addButton.Text = Strings.Friends.AddButton;
		_addButton.Clicked += delegate
		{
			SubmitAdd();
		};
		_list = new ScrollControl(this, "FriendList");
		StyleScroll(_list);
		_counts = MakeLabel(this, "Counts", 10, Dim, _lightFont);
		_tip = MakeLabel(this, "Tip", 10, DimSoft, _lightFont);
		_tip.TextAlign = Pos.Right | Pos.CenterV;
		_tip.Text = Strings.Friends.Tip;
		Layout();
		Hide();
	}

	public new void Show()
	{
		Layout();
		_shownList = "?";
		base.Show();
		BringToFront();
	}

	public override void Hide()
	{
		_field?.Blur();
		base.Hide();
	}

	public void Update()
	{
		if (!base.IsHidden)
		{
			if (base.TitleBarBounds.Height != _laidOutTitleBarHeight)
			{
				Layout();
				_shownList = "?";
			}
			BuildList();
			UpdateHover();
		}
	}

	public void UpdateList()
	{
		_shownList = "?";
	}

	private void SubmitAdd()
	{
		string text = _field.Text?.Trim();
		if (text != null && text.Length >= 3)
		{
			PacketSender.SendAddFriend(text);
			_field.SetText(string.Empty);
		}
	}

	private void BuildList()
	{
		List<IFriendInstance> source = Globals.Me?.Friends ?? new List<IFriendInstance>();
		List<IFriendInstance> list = (from friend in source
			where friend != null
			orderby friend.Online descending, friend.Name
			select friend).ToList();
		string text = string.Join("|", list.Select((IFriendInstance f) => $"{f.Name}:{f.Online}:{f.Map}:{f.Level}:{f.Sprite}:{f.Face}:{LastSeen(f)}"));
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
		_cards.Clear();
		_hovered = null;
		int num = list.Count((IFriendInstance friend) => friend.Online);
		UpdateHeader(num);
		_counts.Text = ((list.Count == 1) ? Strings.Friends.FooterOne.ToString(num) : Strings.Friends.FooterMany.ToString(list.Count, num));
		if (list.Count == 0)
		{
			AddEmpty(Strings.Friends.Empty, 0);
			return;
		}
		int num2 = AddSection(Strings.Friends.SectionOnline, num, GreenLight, 0);
		if (num == 0)
		{
			num2 = AddEmpty(Strings.Friends.NoneOnline, num2) + 14;
		}
		foreach (IFriendInstance item in list.Where((IFriendInstance friend) => friend.Online))
		{
			num2 = AddFriendRow(item, num2);
		}
		int num3 = list.Count - num;
		if (num3 <= 0)
		{
			return;
		}
		if (num > 0)
		{
			num2 += 8;
		}
		num2 = AddSection(Strings.Friends.SectionOffline, num3, Dim, num2);
		foreach (IFriendInstance item2 in list.Where((IFriendInstance friend) => !friend.Online))
		{
			num2 = AddFriendRow(item2, num2);
		}
	}

	private void UpdateHeader(int online)
	{
		string text = Strings.Friends.OnlineChip.ToString(online);
		Color color = ((online > 0) ? GreenLight : Dim);
		int width = MeasureWidth(_font, 8, text) + 14;
		int num = MeasureWidth(GameContentManager.Current.GetFont("sairatitre") ?? _posterFont, 22, _title.Text);
		_onlineChip.SetSize(width, 18);
		_onlineChip.SetPosition(24 + num + 16, Y(26));
		_onlineChip.RenderColor = color;
		_onlineChipText.Text = text;
		_onlineChipText.SetSize(width, 18);
		_onlineChipText.SetPosition(0, 0);
		_onlineChipText.SetTextColor(color, ComponentState.Normal);
	}

	private int AddSection(string caption, int count, Color tint, int y)
	{
		Intersect.Client.Framework.Gwen.Control.Label label = MakeLabel(_list, "Section" + caption, 10, Muted, _font);
		label.Text = caption;
		int num = MeasureWidth(_font, 9, caption);
		label.SetSize(num + 4, 18);
		label.SetPosition(2, y);
		_rows.Add(label);
		string text = count.ToString();
		int num2 = MeasureWidth(_font, 8, text) + 14;
		ImagePanel imagePanel = MakeImage(_list, "SectionCount" + caption, "saiyan_amis_puce.png", num2, 18);
		imagePanel.TextureNinePatchMargin = new Margin(4);
		imagePanel.RenderColor = tint;
		imagePanel.SetPosition(num + 10, y);
		_rows.Add(imagePanel);
		Intersect.Client.Framework.Gwen.Control.Label label2 = MakeLabel(imagePanel, "Text", 9, tint, _font);
		label2.Text = text;
		label2.TextAlign = Pos.Center;
		label2.SetSize(num2, 18);
		int num3 = num + 10 + num2 + 10;
		ImagePanel imagePanel2 = MakeImage(_list, "SectionRule" + caption, "saiyan_amis_filet.png", Math.Max(1, 572 - num3), 1);
		imagePanel2.RenderColor = Rule;
		imagePanel2.SetPosition(num3, y + 9);
		_rows.Add(imagePanel2);
		return y + 24;
	}

	private int AddEmpty(string text, int y)
	{
		ImagePanel imagePanel = MakeImage(_list, $"Empty{y}", "saiyan_amis_vide.png", 572, 58);
		imagePanel.SetPosition(0, y);
		_rows.Add(imagePanel);
		Intersect.Client.Framework.Gwen.Control.Label label = MakeLabel(imagePanel, "Text", 10, Dim, _lightFont);
		label.Text = text;
		label.TextAlign = Pos.Center;
		label.SetSize(572, 58);
		return y + 58;
	}

	private int AddFriendRow(IFriendInstance friend, int y)
	{
		string name = friend.Name;
		bool online = friend.Online;
		ImagePanel imagePanel = new ImagePanel(_list, "Friend_" + name)
		{
			Texture = GameContentManager.Current.GetTexture(TextureType.Gui, "saiyan_amis_carte.png")
		};
		imagePanel.SetSize(572, 58);
		imagePanel.SetPosition(0, y);
		_rows.Add(imagePanel);
		FriendCard friendCard = new FriendCard
		{
			Row = imagePanel
		};
		_cards.Add(friendCard);
		HexPortrait hexPortrait = new HexPortrait(imagePanel, "Portrait", 18)
		{
			Accent = (online ? Orange : GreyFrame),
			SpriteTint = (online ? new Color(255, 255, 255, 255) : AbsentTint)
		};
		if (online)
		{
			Entity entity = Globals.Entities.Values.FirstOrDefault((Entity entity2) => entity2 is Player && entity2.Name == name);
			if (entity != null)
			{
				hexPortrait.Entity = entity;
				goto IL_011d;
			}
		}
		hexPortrait.Sprite = friend.Sprite;
		hexPortrait.Face = friend.Face;
		goto IL_011d;
		IL_011d:
		hexPortrait.SetPosition(14, 11);
		ImagePanel imagePanel2 = MakeImage(imagePanel, "StatusBack", "saiyan_amis_pastille_fond.png", 12, 12);
		imagePanel2.SetPosition(39, 34);
		ImagePanel imagePanel3 = MakeImage(imagePanel, "Status", "saiyan_amis_pastille.png", 8, 8);
		imagePanel3.RenderColor = (online ? Green : Dim);
		imagePanel3.SetPosition(41, 36);
		int num = MeasureWidth(_nameFont, 14, name);
		Intersect.Client.Framework.Gwen.Control.Label label = MakeLabel(imagePanel, "Name", 14, online ? White : Muted, _nameFont);
		label.Text = name;
		label.SetSize(num + 6, 26);
		label.SetPosition(66, 4);
		if (friend.Level > 0)
		{
			string text = Strings.Friends.Level.ToString(friend.Level);
			int width = MeasureWidth(_font, 8, text) + 14;
			Color color = (online ? Gold : Dim);
			ImagePanel imagePanel4 = MakeImage(imagePanel, "Level", "saiyan_amis_puce.png", width, 18);
			imagePanel4.TextureNinePatchMargin = new Margin(4);
			imagePanel4.RenderColor = color;
			imagePanel4.SetPosition(66 + num + 10, 9);
			Intersect.Client.Framework.Gwen.Control.Label label2 = MakeLabel(imagePanel4, "Text", 9, color, _font);
			label2.Text = text;
			label2.TextAlign = Pos.Center;
			label2.SetSize(width, 18);
		}
		if (online)
		{
			ImagePanel imagePanel5 = MakeImage(imagePanel, "Pin", "saiyan_amis_icone_lieu.png", 10, 12);
			imagePanel5.RenderColor = GreenLight;
			imagePanel5.SetPosition(66, 36);
			Intersect.Client.Framework.Gwen.Control.Label label3 = MakeLabel(imagePanel, "Location", 10, Muted, _lightFont);
			label3.Text = (string.IsNullOrWhiteSpace(friend.Map) ? Strings.Friends.SectionOnline.ToString() : friend.Map);
			label3.SetSize(340, 20);
			label3.SetPosition(81, 32);
		}
		else
		{
			Intersect.Client.Framework.Gwen.Control.Label label4 = MakeLabel(imagePanel, "LastSeen", 10, Dim, _lightFont);
			label4.Text = LastSeen(friend);
			label4.SetSize(340, 20);
			label4.SetPosition(66, 32);
		}
		if (online)
		{
			Button button = MakeGesture(friendCard, "Message", "saiyan_amis_icone_message.png", 448, danger: false);
			button.SetToolTipText(Strings.Friends.MessageTip);
			button.Clicked += delegate
			{
				Interface.GameUi?.SetChatboxText("/pm " + name + " ");
			};
			Button button2 = MakeGesture(friendCard, "Party", "saiyan_amis_icone_groupe.png", 488, danger: false);
			button2.SetToolTipText(Strings.Friends.PartyTip);
			button2.Clicked += delegate
			{
				PacketSender.SendPartyInvite(name);
			};
			(string, int)[] array = new(string, int)[2]
			{
				("saiyan_amis_icone_message.png", 448),
				("saiyan_amis_icone_groupe.png", 488)
			};
			for (int num2 = 0; num2 < array.Length; num2++)
			{
				(string, int) tuple = array[num2];
				string item = tuple.Item1;
				int item2 = tuple.Item2;
				ImagePanel imagePanel6 = MakeImage(imagePanel, $"Quiet{item2}", item, 16, 16);
				imagePanel6.RenderColor = DimSoft;
				imagePanel6.SetPosition(item2 + 8, 21);
				friendCard.QuietIcons.Add(imagePanel6);
			}
		}
		Button button3 = MakeGesture(friendCard, "Remove", "saiyan_amis_icone_retirer.png", 528, danger: true);
		button3.SetToolTipText(Strings.Friends.RemoveFriend);
		button3.Clicked += delegate
		{
			AskToRemove(name);
		};
		ApplyHover(friendCard, hovered: false);
		return y + 58 + 6;
	}

	private Button MakeGesture(FriendCard card, string name, string icon, int x, bool danger)
	{
		Button button = new Button(card.Row, name, disableText: true);
		button.SetStateTexture(ComponentState.Normal, "saiyan_amis_geste.png");
		button.SetStateTexture(ComponentState.Hovered, danger ? "saiyan_amis_retrait_hover.png" : "saiyan_amis_geste_hover.png");
		button.SetStateTexture(ComponentState.Active, danger ? "saiyan_amis_retrait_clicked.png" : "saiyan_amis_geste_clicked.png");
		button.SetSize(32, 32);
		button.SetPosition(x, 13);
		ImagePanel glyph = MakeImage(button, "Glyph", icon, 16, 16);
		glyph.RenderColor = Muted;
		glyph.SetPosition(8, 8);
		Color hoverTint = (danger ? new Color(255, 255, 120, 110) : GoldLight);
		button.HoverEnter += delegate
		{
			glyph.RenderColor = hoverTint;
		};
		button.HoverLeave += delegate
		{
			glyph.RenderColor = Muted;
		};
		card.Buttons.Add(button);
		return button;
	}

	private void UpdateHover()
	{
		FriendCard friendCard = null;
		Point mousePosition = InputHandler.MousePosition;
		Point point = _list.ToCanvas(new Point(0, 0));
		if (mousePosition.X >= point.X && mousePosition.X < point.X + _list.Width && mousePosition.Y >= point.Y && mousePosition.Y < point.Y + _list.Height)
		{
			foreach (FriendCard card in _cards)
			{
				Point point2 = card.Row.ToCanvas(new Point(0, 0));
				if (mousePosition.X >= point2.X && mousePosition.X < point2.X + 572 && mousePosition.Y >= point2.Y && mousePosition.Y < point2.Y + 58)
				{
					friendCard = card;
					break;
				}
			}
		}
		if (friendCard != _hovered)
		{
			if (_hovered != null)
			{
				ApplyHover(_hovered, hovered: false);
			}
			_hovered = friendCard;
			if (friendCard != null)
			{
				ApplyHover(friendCard, hovered: true);
			}
		}
	}

	private static void ApplyHover(FriendCard card, bool hovered)
	{
		card.Row.Texture = GameContentManager.Current.GetTexture(TextureType.Gui, hovered ? "saiyan_amis_carte_survol.png" : "saiyan_amis_carte.png");
		foreach (Button button in card.Buttons)
		{
			button.IsHidden = !hovered;
		}
		foreach (ImagePanel quietIcon in card.QuietIcons)
		{
			quietIcon.IsHidden = hovered;
		}
	}

	private static string LastSeen(IFriendInstance friend)
	{
		if (friend.Online)
		{
			return string.Empty;
		}
		int lastSeenMinutes = friend.LastSeenMinutes;
		if (lastSeenMinutes < 60)
		{
			if (lastSeenMinutes >= 0)
			{
				if (lastSeenMinutes < 2)
				{
					return Strings.Friends.SeenJustNow;
				}
				return Strings.Friends.SeenMinutes.ToString(lastSeenMinutes);
			}
			return Strings.Friends.Offline;
		}
		if (lastSeenMinutes < 2880)
		{
			if (lastSeenMinutes < 1440)
			{
				return Strings.Friends.SeenHours.ToString(lastSeenMinutes / 60);
			}
			return Strings.Friends.SeenYesterday;
		}
		if (lastSeenMinutes < 86400)
		{
			return Strings.Friends.SeenDays.ToString(lastSeenMinutes / 1440);
		}
		return Strings.Friends.SeenLongAgo;
	}

	private static void AskToRemove(string name)
	{
		ConfirmCardWindow.Open(new ConfirmCardRequest
		{
			Title = Strings.ConfirmCard.RemoveFriendTitle,
			Icon = "saiyan_carte_icone_ami.png",
			Heading = name,
			Prompt = Strings.ConfirmCard.RemoveFriendPrompt,
			ConfirmLabel = Strings.ConfirmCard.Remove,
			Danger = true,
			PlayerName = name
		}, delegate
		{
			PacketSender.SendRemoveFriend(name);
		});
	}

	private new int Y(int windowY)
	{
		return windowY - _laidOutTitleBarHeight;
	}

	private void Layout()
	{
		_laidOutTitleBarHeight = base.TitleBarBounds.Height;
		_title.SetSize(300, 40);
		_title.SetPosition(24, Y(12));
		_close.SetSize(24, 24);
		_close.SetPosition(580, Y(18));
		_fieldBack.SetPosition(20, Y(74));
		_addButton.SetPosition(472, Y(74));
		_list.SetSize(592, 376);
		_list.SetPosition(20, Y(128));
		_counts.SetSize(260, 30);
		_counts.SetPosition(22, Y(522));
		_tip.SetSize(330, 30);
		_tip.SetPosition(268, Y(522));
		int valueOrDefault = (Globals.Me?.Friends?.Count((IFriendInstance friend) => friend?.Online ?? false)).GetValueOrDefault();
		UpdateHeader(valueOrDefault);
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

	private static int MeasureWidth(IFont? font, int size, string? text)
	{
		if (font != null && !string.IsNullOrEmpty(text))
		{
			GameRenderer renderer = Graphics.Renderer;
			if (renderer != null)
			{
				return (int)MathF.Ceiling(renderer.MeasureText(text, font, size, 1f).X);
			}
		}
		return (text?.Length ?? 0) * size * 2 / 3;
	}
}
