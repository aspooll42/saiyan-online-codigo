using System;
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
using Intersect.Config;
using Intersect.Framework.Core;

namespace Intersect.Client.Interface.Shared;

public sealed class ConfirmCardWindow : WindowControl
{
	private const int CardWidth = 460;

	private const int CardHeight = 244;

	private const int RestY = 150;

	private const long SlideMs = 220L;

	private static readonly Color White = new Color(255, 242, 242, 242);

	private static readonly Color Muted = new Color(255, 176, 184, 200);

	private static readonly Color Orange = new Color(255, 255, 122, 26);

	private static readonly Color Night = new Color(255, 11, 14, 23);

	private static readonly Color GoldLight = new Color(255, 255, 226, 150);

	private static readonly Color RedLight = new Color(255, 255, 150, 140);

	private static ConfirmCardWindow? _open;

	private readonly ConfirmCardRequest _request;

	private readonly Action _onConfirm;

	private readonly Action? _onCancel;

	private readonly IFont? _body;

	private readonly IFont? _strong;

	private readonly ImagePanel _icon;

	private readonly Label _title;

	private readonly Button _close;

	private readonly HexPortrait _portrait;

	private readonly Label _heading;

	private readonly Label _prompt;

	private readonly Label _detail;

	private readonly ImagePanel _chip;

	private readonly ImagePanel _chipOrb;

	private readonly Label _chipText;

	private readonly Button _cancel;

	private readonly (ImagePanel Cap, Label Text) _cancelKey;

	private readonly Button _confirm;

	private readonly (ImagePanel Cap, Label Text) _confirmKey;

	private readonly long _openedAt;

	private bool _finished;

	private int _laidOutTitleBarHeight = -1;

	public static ConfirmCardWindow? Current
	{
		get
		{
			ConfirmCardWindow open = _open;
			if (open == null || open.IsHidden || open._finished)
			{
				return null;
			}
			return open;
		}
	}

	private ConfirmCardWindow(ConfirmCardRequest request, Action onConfirm, Action? onCancel)
		: base(Interface.CurrentInterface.Root, string.Empty, modal: false, "ConfirmCardWindow")
	{
		_request = request;
		_onConfirm = onConfirm;
		_onCancel = onCancel;
		_openedAt = Timing.Global.MillisecondsUtc;
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
		_icon = MakeImage(this, "Icon", request.Icon, 28, 24);
		_title = MakeLabel(this, "Title", 9, GoldLight, _body);
		_title.Text = request.Title;
		_close = new Button(this, "Close", disableText: true);
		_close.SetStateTexture(ComponentState.Normal, "saiyan_close.png");
		_close.SetStateTexture(ComponentState.Hovered, "saiyan_close_hover.png");
		_close.SetStateTexture(ComponentState.Active, "saiyan_close_clicked.png");
		_close.Clicked += delegate
		{
			Cancel();
		};
		_portrait = new HexPortrait(this, "Portrait", 35)
		{
			Accent = Orange,
			Icon = request.Emblem
		};
		RefreshPortrait();
		_heading = MakeLabel(this, "Heading", 19, White, _strong);
		_heading.Text = request.Heading;
		_prompt = MakeLabel(this, "Prompt", 11, White, _body);
		_prompt.Text = request.Prompt;
		_detail = MakeLabel(this, "Detail", 10, Muted, _body);
		_detail.Text = request.Detail ?? string.Empty;
		_detail.IsHidden = string.IsNullOrWhiteSpace(request.Detail);
		bool chipDanger = request.ChipDanger;
		_chip = MakeImage(this, "Chip", chipDanger ? "saiyan_carte_puce_danger.png" : "saiyan_carte_puce.png", 24, 24);
		_chip.TextureNinePatchMargin = new Margin(5);
		_chipOrb = MakeImage(this, "ChipOrb", chipDanger ? "saiyan_carte_orbe_danger.png" : "saiyan_carte_orbe.png", 12, 12);
		_chipText = MakeLabel(this, "ChipText", 9, chipDanger ? RedLight : GoldLight, _body);
		_chipText.Text = request.Chip ?? string.Empty;
		bool flag = !string.IsNullOrWhiteSpace(request.Chip);
		_chip.IsHidden = !flag;
		_chipOrb.IsHidden = !flag;
		_chipText.IsHidden = !flag;
		_cancel = MakeButton("Cancel", "saiyan_carte_refuser", 150, 42, Muted, White);
		_cancel.Text = request.CancelLabel ?? ((string)Strings.ConfirmCard.Cancel);
		_cancel.Clicked += delegate
		{
			Cancel();
		};
		_cancelKey = MakeKeyCap(_cancel, "saiyan_touche_sombre.png", Strings.RequestCard.KeyDecline, Muted);
		_confirm = (request.Danger ? MakeButton("Confirm", "saiyan_carte_danger", 258, 42, White, White) : MakeButton("Confirm", "saiyan_carte_accepter", 258, 42, Night, Night));
		_confirm.Text = request.ConfirmLabel;
		_confirm.Clicked += delegate
		{
			Confirm();
		};
		_confirmKey = MakeKeyCap(_confirm, "saiyan_touche_claire.png", Strings.RequestCard.KeyAccept, request.Danger ? White : Night);
		_cancel.IsHidden = request.Notice;
		Layout();
		SetPosition(Math.Max(0, (Interface.CurrentInterface.Root.Width - 460) / 2), RestTop() - 24);
		Show();
		BringToFront();
	}

	public static void Open(ConfirmCardRequest request, Action onConfirm, Action? onCancel = null)
	{
		_open?.Cancel();
		_open = new ConfirmCardWindow(request, onConfirm, onCancel);
	}

	public static void OpenNotice(string title, string heading, string prompt, IGameTexture? emblem = null, string? detail = null)
	{
		Open(new ConfirmCardRequest
		{
			Title = title,
			Heading = heading,
			Prompt = prompt,
			Detail = detail,
			ConfirmLabel = Strings.InputBox.Okay.ToString().ToUpperInvariant(),
			Emblem = emblem,
			Notice = true
		}, delegate
		{
		});
	}

	public static string? OutlawChip()
	{
		KarmaOptions karmaOptions = Options.Instance?.Karma;
		Player me = default(Player);
		int num;
		if (karmaOptions != null)
		{
			me = Globals.Me;
			if (me != null && karmaOptions.OutlawLogoutSeconds > 0)
			{
				num = (me.IsDetained ? 1 : 0);
				goto IL_0030;
			}
		}
		num = 1;
		goto IL_0030;
		IL_0030:
		bool flag = (byte)num != 0;
		bool flag2 = flag;
		if (!flag2)
		{
			KarmaTier karmaTier = me.KarmaTier;
			bool flag3 = (uint)(karmaTier - 2) <= 1u;
			flag2 = !flag3;
		}
		if (flag2 || karmaOptions.IsHideout(me.MapInstance?.Name))
		{
			return null;
		}
		if (me.KarmaTier != KarmaTier.Criminal)
		{
			return Strings.ConfirmCard.QuitSuspect.ToString(karmaOptions.OutlawLogoutSeconds);
		}
		return Strings.ConfirmCard.QuitCriminal.ToString(karmaOptions.OutlawLogoutSeconds);
	}

	public static void OpenQuit(bool inCombat, Action onConfirm, Action? onCancel = null)
	{
		string text = Globals.Me?.Name;
		ConfirmCardRequest obj = new ConfirmCardRequest
		{
			Title = Strings.ConfirmCard.QuitTitle,
			Icon = "saiyan_carte_icone_quitter.png",
			Heading = ((text != null && text.Length > 0) ? text : Strings.ConfirmCard.GameName.ToString()),
			Prompt = Strings.ConfirmCard.QuitPrompt,
			Detail = Strings.ConfirmCard.QuitGoodbye,
			Chip = (inCombat ? ((string)Strings.ConfirmCard.QuitInCombat) : (OutlawChip() ?? ((string)Strings.ConfirmCard.QuitSaved))),
			ChipDanger = inCombat || OutlawChip() != null,
			ConfirmLabel = Strings.ConfirmCard.Quit,
			CancelLabel = Strings.ConfirmCard.Stay,
			Danger = true,
			ShowMe = true,
			Centered = true
		};
		Open(obj, onConfirm, onCancel);
	}

	public static void OpenLogoutInCombat(bool toCharacterSelect, Action onConfirm)
	{
		Open(new ConfirmCardRequest
		{
			Title = (toCharacterSelect ? Strings.ConfirmCard.CharacterSelectTitle : Strings.ConfirmCard.LogoutTitle),
			Icon = "saiyan_carte_icone_quitter.png",
			Heading = (Globals.Me?.Name ?? string.Empty),
			Prompt = (toCharacterSelect ? Strings.ConfirmCard.CharacterSelectPrompt : Strings.ConfirmCard.LogoutPrompt),
			Chip = ((Globals.Me?.CombatTimer > Timing.Global.Milliseconds) ? ((string)Strings.ConfirmCard.QuitInCombat) : (OutlawChip() ?? ((string)Strings.ConfirmCard.QuitInCombat))),
			ChipDanger = true,
			ConfirmLabel = (toCharacterSelect ? Strings.ConfirmCard.CharacterSelect : Strings.ConfirmCard.Logout),
			Danger = true,
			ShowMe = true,
			Centered = true
		}, onConfirm);
	}

	public void Confirm()
	{
		Finish(confirmed: true);
	}

	public void Cancel()
	{
		Finish(confirmed: false);
	}

	private void Finish(bool confirmed)
	{
		if (!_finished)
		{
			_finished = true;
			if (_open == this)
			{
				_open = null;
			}
			Close();
			if (confirmed)
			{
				_onConfirm();
			}
			else
			{
				_onCancel?.Invoke();
			}
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
			RefreshPortrait();
			long num = Timing.Global.MillisecondsUtc - _openedAt;
			if (num <= 240)
			{
				float num2 = Math.Clamp((float)num / 220f, 0f, 1f);
				float num3 = 1f - (1f - num2) * (1f - num2) * (1f - num2);
				SetPosition(Math.Max(0, (Interface.CurrentInterface.Root.Width - 460) / 2), RestTop() - (int)MathF.Round(24f * (1f - num3)));
			}
		}
	}

	private int RestTop()
	{
		if (!_request.Centered)
		{
			return 150;
		}
		return Math.Max(0, (Interface.CurrentInterface.Root.Height - 244) / 2);
	}

	private void RefreshPortrait()
	{
		if (_request.Emblem != null)
		{
			return;
		}
		if (_request.ShowMe)
		{
			_portrait.Entity = Globals.Me;
		}
		else if (!string.IsNullOrWhiteSpace(_request.PlayerName))
		{
			_portrait.Entity = Globals.Entities.Values.OfType<Player>().FirstOrDefault((Player player) => player.Name == _request.PlayerName);
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
		_title.SetSize(360, 24);
		_title.SetPosition(46, FrameY(8));
		_close.SetSize(24, 24);
		_close.SetPosition(424, FrameY(8));
		_portrait.SetPosition(24, FrameY(66));
		_heading.SetSize(320, 32);
		_heading.SetPosition(124, FrameY(52));
		_prompt.SetSize(326, 22);
		_prompt.SetPosition(124, FrameY(90));
		_detail.SetSize(326, 20);
		_detail.SetPosition(124, FrameY(112));
		int num = Math.Min(320, MeasureWidth(_body, _chipText.FontSize, _chipText.Text) + 30);
		_chip.SetSize(num, 24);
		_chip.SetPosition(124, FrameY(144));
		_chipOrb.SetPosition(131, FrameY(150));
		_chipText.SetSize(num - 22, 24);
		_chipText.SetPosition(146, FrameY(144));
		_cancel.SetPosition(20, FrameY(186));
		_confirm.SetPosition(_request.Notice ? ((460 - _confirm.Width) / 2) : 182, FrameY(186));
		PlaceKeyCap(_cancel, _cancelKey);
		PlaceKeyCap(_confirm, _confirmKey);
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
		Label label = MakeLabel(button, "KeyText", 8, ink, _body);
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
