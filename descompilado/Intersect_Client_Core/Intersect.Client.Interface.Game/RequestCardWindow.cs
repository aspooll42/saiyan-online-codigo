using System;
using System.Collections.Generic;
using System.Linq;
using Intersect.Client.Core;
using Intersect.Client.Entities;
using Intersect.Client.Framework.Content;
using Intersect.Client.Framework.File_Management;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.Framework.Gwen;
using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.General;
using Intersect.Client.Interface.Game.EntityPanel;
using Intersect.Client.Localization;
using Intersect.Client.Networking;
using Intersect.Framework.Core;
using Intersect.Framework.Core.GameObjects.PlayerClass;
using Intersect.Models;

namespace Intersect.Client.Interface.Game;

public sealed class RequestCardWindow : WindowControl
{
	public enum RequestKind
	{
		Party,
		Trade,
		Guild,
		Friend,
		Knock
	}

	private const int CardWidth = 460;

	private const int CardHeight = 244;

	private const int RestY = 110;

	private const long SlideMs = 220L;

	private static readonly Color White = new Color(255, 242, 242, 242);

	private static readonly Color Muted = new Color(255, 176, 184, 200);

	private static readonly Color Dim = new Color(255, 110, 120, 145);

	private static readonly Color Night = new Color(255, 11, 14, 23);

	private static readonly Color Orange = new Color(255, 255, 122, 26);

	private static readonly Color GoldLight = new Color(255, 255, 226, 150);

	private static RequestCardWindow? _open;

	private readonly RequestKind _kind;

	private readonly Guid _otherId;

	private readonly string _otherName;

	private readonly IFont? _body;

	private readonly IFont? _strong;

	private readonly ImagePanel _icon;

	private readonly Label _title;

	private readonly Button _close;

	private readonly HexPortrait _portrait;

	private readonly ImagePanel _levelBadge;

	private readonly Label _level;

	private readonly Label _name;

	private readonly ImagePanel _infoMark;

	private readonly Label _info;

	private readonly Label _prompt;

	private readonly Label _outOfSight;

	private readonly List<(ImagePanel Chip, ImagePanel Orb, Label Text)> _chips = new List<(ImagePanel, ImagePanel, Label)>();

	private readonly Button _decline;

	private readonly (ImagePanel Cap, Label Text) _declineKey;

	private readonly Button _accept;

	private readonly (ImagePanel Cap, Label Text) _acceptKey;

	private readonly long _openedAt;

	private bool _finished;

	private int _laidOutTitleBarHeight = -1;

	private string _shown = "?";

	public static RequestCardWindow? Current
	{
		get
		{
			RequestCardWindow open = _open;
			if (open == null || open.IsHidden || open._finished)
			{
				return null;
			}
			return open;
		}
	}

	private RequestCardWindow(RequestKind kind, Guid otherId, string otherName, string? detail)
		: base(Interface.CurrentInterface.Root, string.Empty, modal: false, "RequestCardWindow")
	{
		_kind = kind;
		_otherId = otherId;
		_otherName = otherName;
		_openedAt = Timing.Global.Milliseconds;
		GameContentManager current = GameContentManager.Current;
		_body = current.GetFont("chakrapetch");
		_strong = current.GetFont("rajdhanibold");
		base.DeleteOnClose = true;
		DisableResizing();
		base.IsClosable = false;
		base.Titlebar.ShouldDrawBackground = false;
		base.TitleLabel.IsHidden = true;
		SetSize(460, 244);
		IGameTexture texture = current.GetTexture(TextureType.Gui, "saiyan_carte_invitation.png");
		SetImage(texture, "saiyan_carte_invitation.png", ControlState.Active);
		SetImage(texture, "saiyan_carte_invitation.png", ControlState.Inactive);
		string text;
		switch (kind)
		{
		case RequestKind.Trade:
			text = "saiyan_carte_icone_echange.png";
			break;
		case RequestKind.Guild:
			text = "saiyan_carte_icone_clan.png";
			break;
		case RequestKind.Friend:
		case RequestKind.Knock:
			text = "saiyan_carte_icone_ami.png";
			break;
		default:
			text = "saiyan_carte_icone_groupe.png";
			break;
		}
		string texture2 = text;
		_icon = MakeImage(this, "Icon", texture2, 28, 24);
		_title = MakeLabel(this, "Title", 10, GoldLight, _body);
		Label title = _title;
		title.Text = (kind switch
		{
			RequestKind.Trade => Strings.RequestCard.TradeTitle, 
			RequestKind.Guild => Strings.RequestCard.GuildTitle, 
			RequestKind.Friend => Strings.RequestCard.FriendTitle, 
			RequestKind.Knock => Strings.RequestCard.KnockTitle, 
			_ => Strings.RequestCard.PartyTitle, 
		}).ToString();
		_close = new Button(this, "Close", disableText: true);
		_close.SetStateTexture(ComponentState.Normal, "saiyan_close.png");
		_close.SetStateTexture(ComponentState.Hovered, "saiyan_close_hover.png");
		_close.SetStateTexture(ComponentState.Active, "saiyan_close_clicked.png");
		_close.Clicked += delegate
		{
			Decline();
		};
		_portrait = new HexPortrait(this, "Portrait", 35)
		{
			Accent = Orange
		};
		_levelBadge = MakeImage(this, "LevelBadge", "saiyan_carte_niveau.png", 30, 30);
		_level = MakeLabel(this, "Level", 11, Night, _strong);
		_level.TextAlign = Pos.Center;
		_name = MakeLabel(this, "Name", 19, White, _strong);
		_name.Text = otherName;
		_infoMark = MakeImage(this, "InfoMark", "saiyan_suivi_puce.png", 7, 7);
		_infoMark.RenderColor = Orange;
		_info = MakeLabel(this, "Info", 10, Muted, _body);
		_prompt = MakeLabel(this, "Prompt", 11, White, _body);
		Label prompt = _prompt;
		prompt.Text = kind switch
		{
			RequestKind.Trade => Strings.RequestCard.TradePrompt, 
			RequestKind.Guild => Strings.RequestCard.GuildPrompt, 
			RequestKind.Friend => Strings.RequestCard.FriendPrompt, 
			RequestKind.Knock => Strings.RequestCard.KnockPrompt, 
			_ => Strings.RequestCard.PartyPrompt, 
		};
		_outOfSight = MakeLabel(this, "OutOfSight", 10, Dim, _body);
		_outOfSight.Text = Strings.RequestCard.OutOfSight;
		string[] array = kind switch
		{
			RequestKind.Trade => new string[2]
			{
				Strings.RequestCard.TradeItems,
				Strings.RequestCard.TradeDouble
			}, 
			RequestKind.Guild => (!string.IsNullOrWhiteSpace(detail)) ? new string[2]
			{
				Strings.RequestCard.GuildName.ToString(detail),
				Strings.RequestCard.GuildChat
			} : new string[1] { Strings.RequestCard.GuildChat }, 
			RequestKind.Friend => new string[2]
			{
				Strings.RequestCard.FriendMessages,
				Strings.RequestCard.FriendOnline
			}, 
			RequestKind.Knock => new string[2]
			{
				Strings.RequestCard.KnockEnters,
				Strings.RequestCard.KnockShowOut
			}, 
			_ => new string[2]
			{
				Strings.RequestCard.PartyBonus.ToString(Options.Instance.Party.BonusExperiencePercentPerMember),
				Strings.RequestCard.PartyMax.ToString(Options.Instance.Party.MaximumMembers)
			}, 
		};
		foreach (string text2 in array)
		{
			ImagePanel imagePanel = MakeImage(this, $"Chip{_chips.Count}", "saiyan_carte_puce.png", 24, 24);
			imagePanel.TextureNinePatchMargin = new Margin(5);
			ImagePanel item = MakeImage(this, $"ChipOrb{_chips.Count}", "saiyan_carte_orbe.png", 12, 12);
			Label label = MakeLabel(this, $"ChipText{_chips.Count}", 9, GoldLight, _body);
			label.Text = text2;
			_chips.Add((imagePanel, item, label));
		}
		_decline = MakeButton("Decline", "saiyan_carte_refuser", 150, 42, Muted, White);
		_decline.Text = ((kind == RequestKind.Knock) ? Strings.RequestCard.KnockIgnore : Strings.RequestCard.Decline);
		_decline.Clicked += delegate
		{
			Decline();
		};
		_declineKey = MakeKeyCap(_decline, "saiyan_touche_sombre.png", Strings.RequestCard.KeyDecline, Muted);
		_accept = MakeButton("Accept", "saiyan_carte_accepter", 258, 42, Night, Night);
		Button accept = _accept;
		accept.Text = kind switch
		{
			RequestKind.Trade => Strings.RequestCard.Trade, 
			RequestKind.Friend => Strings.RequestCard.Accept, 
			RequestKind.Knock => Strings.RequestCard.KnockOpen, 
			_ => Strings.RequestCard.Join, 
		};
		_accept.Clicked += delegate
		{
			Accept();
		};
		_acceptKey = MakeKeyCap(_accept, "saiyan_touche_claire.png", Strings.RequestCard.KeyAccept, Night);
		Layout();
		SetPosition(Math.Max(0, (Interface.CurrentInterface.Root.Width - 460) / 2), 86);
		Refresh();
		Show();
		BringToFront();
	}

	public static void Open(RequestKind kind, Guid otherId, string otherName, string? detail = null)
	{
		_open?.Decline();
		_open = new RequestCardWindow(kind, otherId, otherName, detail);
	}

	public void Accept()
	{
		Answer(accepted: true);
	}

	public void Decline()
	{
		Answer(accepted: false);
	}

	private void Answer(bool accepted)
	{
		if (!_finished)
		{
			_finished = true;
			if (_open == this)
			{
				_open = null;
			}
			switch (_kind)
			{
			case RequestKind.Trade:
				PacketSender.SendTradeRequestResponse(_otherId, accepted);
				break;
			case RequestKind.Guild:
				PacketSender.SendGuildInviteResponse(accepted);
				break;
			case RequestKind.Friend:
				PacketSender.SendFriendRequestResponse(_otherId, accepted);
				break;
			case RequestKind.Knock:
				PacketSender.SendHouseKnockAnswer(_otherId, accepted);
				break;
			default:
				PacketSender.SendPartyInviteResponse(_otherId, accepted);
				break;
			}
			Close();
		}
	}

	protected override void Dispose(bool disposing)
	{
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
			}
			long num = Timing.Global.Milliseconds - _openedAt;
			if (num <= 240)
			{
				float num2 = Math.Clamp((float)num / 220f, 0f, 1f);
				float num3 = 1f - (1f - num2) * (1f - num2) * (1f - num2);
				SetPosition(Math.Max(0, (Interface.CurrentInterface.Root.Width - 460) / 2), 110 - (int)MathF.Round(24f * (1f - num3)));
			}
			Refresh();
		}
	}

	private void Refresh()
	{
		Entity value;
		Player player = ((_otherId != Guid.Empty && Globals.Entities.TryGetValue(_otherId, out value)) ? (value as Player) : Globals.Entities.Values.OfType<Player>().FirstOrDefault((Player player2) => player2 != Globals.Me && player2.Name == _otherName));
		string text = ((player == null) ? "absent" : $"{player.Level}:{player.Class}");
		if (!(text == _shown))
		{
			_shown = text;
			_portrait.Entity = player;
			bool flag = player != null;
			_levelBadge.IsHidden = !flag;
			_level.IsHidden = !flag;
			_infoMark.IsHidden = !flag;
			_info.IsHidden = !flag;
			_outOfSight.IsHidden = flag;
			if (player != null)
			{
				_level.Text = player.Level.ToString();
				ClassDescriptor databaseObject;
				string text2 = (DatabaseObject<ClassDescriptor>.TryGet(player.Class, out databaseObject) ? Traduction.Contenu(databaseObject.Name) : string.Empty);
				_info.Text = (string.IsNullOrWhiteSpace(text2) ? Strings.RequestCard.Level.ToString(player.Level) : Strings.RequestCard.LevelClass.ToString(player.Level, text2));
			}
			Layout();
		}
	}

	private int FrameY(int cardY)
	{
		return cardY - _laidOutTitleBarHeight;
	}

	private void Layout()
	{
		_laidOutTitleBarHeight = base.TitleBarBounds.Height;
		_icon.SetPosition(12, FrameY(8));
		_title.SetSize(300, 24);
		_title.SetPosition(46, FrameY(8));
		_close.SetSize(24, 24);
		_close.SetPosition(424, FrameY(8));
		_portrait.SetPosition(24, FrameY(66));
		_levelBadge.SetPosition(73, FrameY(114));
		_level.SetSize(30, 28);
		_level.SetPosition(73, FrameY(114));
		_name.SetSize(320, 32);
		_name.SetPosition(124, FrameY(52));
		bool flag = !_info.IsHidden;
		_infoMark.SetPosition(125, FrameY(97));
		_info.SetSize(300, 20);
		_info.SetPosition(137, FrameY(90));
		_prompt.SetSize(326, 22);
		_prompt.SetPosition(124, FrameY(flag ? 112 : 90));
		_outOfSight.SetSize(300, 20);
		_outOfSight.SetPosition(124, FrameY(112));
		int num = 124;
		foreach (var chip in _chips)
		{
			ImagePanel item = chip.Chip;
			ImagePanel item2 = chip.Orb;
			Label item3 = chip.Text;
			int num2 = Math.Min(444 - num, TextWidth(item3) + 30);
			item.SetSize(num2, 24);
			item.SetPosition(num, FrameY(144));
			item2.SetPosition(num + 7, FrameY(150));
			item3.SetSize(num2 - 22, 24);
			item3.SetPosition(num + 22, FrameY(144));
			num += num2 + 8;
		}
		_decline.SetPosition(20, FrameY(186));
		_accept.SetPosition(182, FrameY(186));
		PlaceKeyCap(_decline, _declineKey);
		PlaceKeyCap(_accept, _acceptKey);
	}

	private void PlaceKeyCap(Button button, (ImagePanel Cap, Label Text) key)
	{
		int num = MeasureWidth(button.Font, button.FontSize, button.Text);
		int num2 = MeasureWidth(_body, key.Text.FontSize, key.Text.Text) + 12;
		int num3 = (button.Width - num - 10 - num2) / 2;
		button.Padding = new Padding(0, 0, num2 + 10, 0);
		key.Cap.SetSize(num2, 18);
		key.Cap.SetPosition(num3 + num + 10, (button.Height - 18) / 2);
		key.Text.SetSize(num2, 18);
		key.Text.SetPosition(num3 + num + 10, (button.Height - 18) / 2);
	}

	private static int TextWidth(Label label)
	{
		return MeasureWidth(label.Font, label.FontSize, label.Text);
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
		return 0;
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

	private static ImagePanel MakeImage(Base parent, string name, string texture, int width, int height)
	{
		ImagePanel imagePanel = new ImagePanel(parent, name)
		{
			MouseInputEnabled = false,
			Texture = GameContentManager.Current.GetTexture(TextureType.Gui, texture)
		};
		imagePanel.SetSize(width, height);
		return imagePanel;
	}

	private (ImagePanel Cap, Label Text) MakeKeyCap(Button button, string texture, string text, Color ink)
	{
		ImagePanel imagePanel = MakeImage(button, "KeyCap", texture, 20, 18);
		imagePanel.TextureNinePatchMargin = new Margin(4);
		Label label = MakeLabel(button, "KeyText", 9, ink, _body);
		label.Text = text;
		label.TextAlign = Pos.Center;
		return (Cap: imagePanel, Text: label);
	}

	private Button MakeButton(string name, string key, int width, int height, Color ink, Color hoverInk)
	{
		Button button = new Button(this, name)
		{
			Font = _strong,
			FontSize = 13,
			TextAlign = Pos.Center,
			UsesHeadingFace = false
		};
		button.SetStateTexture(ComponentState.Normal, key + ".png");
		button.SetStateTexture(ComponentState.Hovered, key + "_hover.png");
		button.SetStateTexture(ComponentState.Active, key + "_clicked.png");
		button.SetTextColor(ink, ComponentState.Normal);
		button.SetTextColor(hoverInk, ComponentState.Hovered);
		button.SetTextColor(ink, ComponentState.Active);
		button.SetSize(width, height);
		return button;
	}
}
