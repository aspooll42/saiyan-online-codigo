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
using Intersect.Framework.Core;
using Intersect.Network.Packets.Server;

namespace Intersect.Client.Interface.Game;

public sealed class WantedTurnInWindow : WindowControl
{
	private const int WindowWidth = 640;

	private new const int Margin = 24;

	private const int RowsTop = 112;

	private const int RowWidth = 592;

	private const int RowHeight = 84;

	private const int RowStep = 92;

	private const int MaxRows = 3;

	private const int PromoHeight = 70;

	private const long CountMs = 900L;

	private static readonly Color White = new Color(255, 242, 242, 242);

	private static readonly Color Soft = new Color(255, 166, 177, 200);

	private static readonly Color Dim = new Color(255, 138, 150, 176);

	private static readonly Color Night = new Color(255, 11, 14, 23);

	private static readonly Color Gold = new Color(255, 255, 208, 106);

	private static readonly Color PoliceBlue = new Color(255, 108, 184, 255);

	private static readonly Color GreenLight = new Color(255, 140, 235, 170);

	private static WantedTurnInWindow? _open;

	private readonly IFont? _body;

	private readonly IFont? _strong;

	private readonly WantedTurnInPacket _turnIn;

	private readonly int _rows;

	private readonly bool _promotion;

	private readonly int _height;

	private readonly Label _title;

	private readonly Label _subtitle;

	private readonly ImagePanel _gradeChip;

	private readonly Label _gradeName;

	private readonly Label _gradeReputation;

	private readonly ImagePanel _gradeFill;

	private readonly Label _gradeNext;

	private readonly Button _close;

	private readonly List<Base> _lines = new List<Base>();

	private readonly Label? _totalLabel;

	private readonly Label? _totalValue;

	private readonly Label _walletLabel;

	private readonly Label _walletValue;

	private readonly Label _walletGain;

	private readonly ImagePanel? _promo;

	private readonly Label? _promoTitle;

	private readonly Label? _promoOpens;

	private readonly Label? _promoNext;

	private readonly Button _later;

	private readonly Button _cashIn;

	private readonly long _walletBefore;

	private long _walletAfter;

	private long _countStartedAt;

	private bool _cashed;

	private bool _waiting;

	private bool _finished;

	private int _laidOutTitleBarHeight = -1;

	public static WantedTurnInWindow? Current
	{
		get
		{
			WantedTurnInWindow open = _open;
			if (open == null || open.IsHidden || open._finished)
			{
				return null;
			}
			return open;
		}
	}

	private static bool Honest
	{
		get
		{
			KarmaTier? karmaTier = Globals.Me?.KarmaTier;
			if (karmaTier.HasValue)
			{
				KarmaTier valueOrDefault = karmaTier.GetValueOrDefault();
				if ((uint)valueOrDefault <= 1u)
				{
					return true;
				}
			}
			return false;
		}
	}

	private WantedTurnInWindow(WantedTurnInPacket turnIn)
		: base(Interface.CurrentInterface.Root, string.Empty, modal: true, "WantedTurnInWindow")
	{
		_turnIn = turnIn;
		WantedTurnInEntryPacket[] entries = turnIn.Entries;
		_rows = Math.Clamp((entries != null) ? entries.Length : 0, 1, 3);
		_walletBefore = Globals.Me?.Zeni ?? 0;
		_walletAfter = _walletBefore;
		KarmaOptions karma = Options.Instance.Karma;
		int num = (Honest ? (turnIn.Entries ?? Array.Empty<WantedTurnInEntryPacket>()).Sum((WantedTurnInEntryPacket entry) => entry.Reputation) : 0);
		_promotion = num > 0 && karma.PoliceGradeOf(turnIn.Reputation + num) > karma.PoliceGradeOf(turnIn.Reputation);
		_height = HeightOf(_rows, _promotion);
		GameContentManager current = GameContentManager.Current;
		_body = current.GetFont("chakrapetch");
		_strong = current.GetFont("rajdhanibold");
		Interface.InputBlockingComponents.Add(this);
		base.DeleteOnClose = true;
		DisableResizing();
		base.IsClosable = false;
		base.Titlebar.ShouldDrawBackground = false;
		base.TitleLabel.IsHidden = true;
		SetSize(640, _height);
		string text = $"saiyan_encaisser_{_rows}{(_promotion ? "_promotion" : string.Empty)}.png";
		IGameTexture texture = current.GetTexture(TextureType.Gui, text);
		SetImage(texture, text, ControlState.Active);
		SetImage(texture, text, ControlState.Inactive);
		_title = WantedBoardWindow.MakeLabel(this, "Title", 20, White, current.GetFont("refuel"));
		Label title = _title;
		WantedTurnInEntryPacket[] entries2 = turnIn.Entries;
		title.Text = ((((entries2 != null) ? entries2.Length : 0) > 1) ? Strings.WantedTurnIn.TitleMany : Strings.WantedTurnIn.TitleOne).ToString().ToUpperInvariant();
		WindowTitle.Apply(_title);
		_subtitle = WantedBoardWindow.MakeLabel(this, "Subtitle", 10, Dim, _body);
		Label subtitle = _subtitle;
		string text2 = WantedPlaces.ZoneOf(Globals.Me?.MapId ?? Guid.Empty);
		subtitle.Text = ((text2 != null && text2.Length > 0) ? Strings.WantedBoard.StationIn.ToString(text2) : Strings.WantedBoard.Station.ToString());
		_gradeChip = new ImagePanel(this, "Grade")
		{
			MouseInputEnabled = false,
			Texture = current.GetTexture(TextureType.Gui, "panneau_grade.png")
		};
		_gradeName = WantedBoardWindow.MakeLabel(_gradeChip, "GradeName", 14, PoliceBlue, _strong);
		_gradeReputation = WantedBoardWindow.MakeLabel(_gradeChip, "GradeReputation", 12, Dim, _strong);
		_gradeReputation.TextAlign = Pos.Right | Pos.CenterV;
		ImagePanel imagePanel = new ImagePanel(_gradeChip, "GradeTrack")
		{
			MouseInputEnabled = false,
			Texture = current.GetTexture(TextureType.Gui, "panneau_grade_piste.png")
		};
		imagePanel.SetBounds(12, 28, 196, 4);
		_gradeFill = new ImagePanel(_gradeChip, "GradeFill")
		{
			MouseInputEnabled = false,
			Texture = current.GetTexture(TextureType.Gui, "panneau_grade_jauge.png")
		};
		_gradeNext = WantedBoardWindow.MakeLabel(_gradeChip, "GradeNext", 8, Dim, _body);
		_close = new Button(this, "Close", disableText: true);
		_close.SetStateTexture(ComponentState.Normal, "saiyan_close.png");
		_close.SetStateTexture(ComponentState.Hovered, "saiyan_close_hover.png");
		_close.SetStateTexture(ComponentState.Active, "saiyan_close_clicked.png");
		_close.Clicked += delegate
		{
			CloseTurnIn();
		};
		WantedTurnInEntryPacket[] array = turnIn.Entries ?? Array.Empty<WantedTurnInEntryPacket>();
		for (int num2 = 0; num2 < _rows && num2 < array.Length; num2++)
		{
			_lines.Add(BuildRow(array[num2]));
		}
		WantedTurnInEntryPacket[] entries3 = turnIn.Entries;
		if (((entries3 != null) ? entries3.Length : 0) > 1)
		{
			_totalLabel = WantedBoardWindow.MakeLabel(this, "TotalLabel", 9, Dim, _body);
			_totalLabel.Text = Strings.WantedTurnIn.Total.ToString().ToUpperInvariant();
			_totalValue = WantedBoardWindow.MakeLabel(this, "TotalValue", 16, Gold, _strong);
			_totalValue.TextAlign = Pos.Right | Pos.CenterV;
			_totalValue.Text = Strings.WantedBoard.Reward.ToString(SaiyanHud.Amount(array.Sum((WantedTurnInEntryPacket entry) => entry.Zeni)), SaiyanHud.Amount(array.Sum((WantedTurnInEntryPacket entry) => entry.Experience))) + "   " + (Honest ? Strings.WantedTurnIn.ReputationGain.ToString(num) : Strings.WantedTurnIn.ReputationFrozen.ToString());
		}
		_walletLabel = WantedBoardWindow.MakeLabel(this, "WalletLabel", 10, Dim, _body);
		_walletLabel.Text = Strings.WantedTurnIn.Wallet;
		_walletValue = WantedBoardWindow.MakeLabel(this, "WalletValue", 14, White, _strong);
		_walletValue.TextAlign = Pos.Right | Pos.CenterV;
		_walletGain = WantedBoardWindow.MakeLabel(this, "WalletGain", 13, GreenLight, _strong);
		_walletGain.TextAlign = Pos.Right | Pos.CenterV;
		_walletGain.IsHidden = true;
		if (_promotion)
		{
			_promo = new ImagePanel(this, "Promotion")
			{
				MouseInputEnabled = false,
				Texture = current.GetTexture(TextureType.Gui, "encaisser_promo.png"),
				TextureNinePatchMargin = new Margin(8),
				IsHidden = true
			};
			_promoTitle = WantedBoardWindow.MakeLabel(_promo, "PromotionTitle", 16, PoliceBlue, _strong);
			_promoOpens = WantedBoardWindow.MakeLabel(_promo, "PromotionOpens", 10, Soft, _body);
			_promoNext = WantedBoardWindow.MakeLabel(_promo, "PromotionNext", 9, Dim, _body);
		}
		_later = new Button(this, "Later")
		{
			Font = _body,
			FontSize = 10,
			TextAlign = Pos.Center,
			UsesHeadingFace = false
		};
		_later.SetStateTexture(ComponentState.Normal, "panneau_creux_150x30.png");
		_later.SetStateTexture(ComponentState.Hovered, "panneau_creux_150x30_hover.png");
		_later.SetStateTexture(ComponentState.Active, "panneau_creux_150x30_hover.png");
		_later.SetTextColor(Soft, ComponentState.Normal);
		_later.SetTextColor(White, ComponentState.Hovered);
		_later.SetTextColor(White, ComponentState.Active);
		_later.Text = Strings.WantedTurnIn.Later;
		_later.Clicked += delegate
		{
			ShowBoard();
		};
		_cashIn = new Button(this, "CashIn")
		{
			Font = _body,
			FontSize = 10,
			TextAlign = Pos.Center,
			UsesHeadingFace = false
		};
		_cashIn.SetStateTexture(ComponentState.Normal, "panneau_cta_150x30.png");
		_cashIn.SetStateTexture(ComponentState.Hovered, "panneau_cta_150x30_hover.png");
		_cashIn.SetStateTexture(ComponentState.Active, "panneau_cta_150x30_clicked.png");
		_cashIn.SetStateTexture(ComponentState.Disabled, "panneau_creux_150x30.png");
		_cashIn.SetTextColor(Night, ComponentState.Normal);
		_cashIn.SetTextColor(Night, ComponentState.Hovered);
		_cashIn.SetTextColor(Night, ComponentState.Active);
		_cashIn.SetTextColor(GreenLight, ComponentState.Disabled);
		Button cashIn = _cashIn;
		WantedTurnInEntryPacket[] entries4 = turnIn.Entries;
		cashIn.Text = ((((entries4 != null) ? entries4.Length : 0) > 1) ? Strings.WantedTurnIn.CashInAll : Strings.WantedTurnIn.CashIn);
		_cashIn.Clicked += delegate
		{
			CashIn();
		};
		Layout();
		SetPosition(Math.Max(0, (Interface.CurrentInterface.Root.Width - 640) / 2), Math.Max(0, (Interface.CurrentInterface.Root.Height - _height) / 2));
		ShowGrade(turnIn.Reputation);
		ShowWallet(_walletBefore);
		Show();
		BringToFront();
	}

	public static int HeightOf(int rows, bool promotion)
	{
		return 196 + 92 * Math.Clamp(rows, 1, 3) + ((rows > 1) ? 40 : 0) + (promotion ? 78 : 0);
	}

	public static void Open(WantedTurnInPacket turnIn)
	{
		_open?.CloseTurnIn();
		WantedBoardWindow.Current?.CloseBoard();
		_open = new WantedTurnInWindow(turnIn);
	}

	public void ShowResult(WantedTurnInResultPacket result)
	{
		_waiting = false;
		if (!result.Paid)
		{
			_cashIn.IsDisabled = false;
			return;
		}
		_cashed = true;
		_walletAfter = _walletBefore + Math.Max(0L, result.Zeni);
		_countStartedAt = Timing.Global.Milliseconds;
		_walletGain.Text = "+" + SaiyanHud.Amount(result.Zeni);
		_walletGain.IsHidden = false;
		ShowGrade(result.ReputationAfter);
		KarmaOptions karma = Options.Instance.Karma;
		int num = karma.PoliceGradeOf(result.ReputationAfter);
		if (_promo != null && num > karma.PoliceGradeOf(result.ReputationBefore))
		{
			int[] array = KarmaOptions.ParseList(karma.PoliceGradeThresholds);
			_promoTitle.Text = Strings.WantedTurnIn.Promotion.ToString(WantedBoardWindow.GradeName(num)).ToUpperInvariant();
			_promoOpens.Text = WantedBoardWindow.GradeOpens(num);
			_promoNext.Text = ((num + 1 < array.Length) ? Strings.WantedBoard.NextGrade.ToString(WantedBoardWindow.GradeName(num + 1), SaiyanHud.Amount(array[num + 1])) : Strings.WantedBoard.TopGrade.ToString());
			_promo.IsHidden = false;
		}
		_cashIn.Text = Strings.WantedTurnIn.Cashed;
		_cashIn.IsDisabled = true;
		_later.Text = Strings.WantedTurnIn.SeeBoard;
	}

	private void CashIn()
	{
		if (!_cashed && !_waiting && !_cashIn.IsDisabled)
		{
			_waiting = true;
			_cashIn.IsDisabled = true;
			PacketSender.SendWantedTurnInClaim();
		}
	}

	private void ShowBoard()
	{
		CloseTurnIn();
		WantedBoardWindow.OpenStored();
	}

	public void CloseTurnIn()
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
		if (_finished || base.IsHidden)
		{
			return;
		}
		if (base.TitleBarBounds.Height != _laidOutTitleBarHeight)
		{
			Layout();
		}
		if (_countStartedAt > 0)
		{
			float num = Math.Clamp((float)(Timing.Global.Milliseconds - _countStartedAt) / 900f, 0f, 1f);
			float num2 = 1f - MathF.Pow(1f - num, 3f);
			ShowWallet(_walletBefore + (long)MathF.Round((float)(_walletAfter - _walletBefore) * num2));
			if (num >= 1f)
			{
				_countStartedAt = 0L;
			}
		}
	}

	private void ShowWallet(long zeni)
	{
		_walletValue.Text = SaiyanHud.Amount(zeni) + " Z";
	}

	private void ShowGrade(int reputation)
	{
		KarmaOptions karma = Options.Instance.Karma;
		int[] array = KarmaOptions.ParseList(karma.PoliceGradeThresholds);
		reputation = Math.Max(0, reputation);
		int num = karma.PoliceGradeOf(reputation);
		_gradeName.Text = WantedBoardWindow.GradeName(num).ToUpperInvariant();
		if (num + 1 < array.Length)
		{
			int num2 = array[Math.Clamp(num, 0, array.Length - 1)];
			int num3 = array[num + 1];
			_gradeReputation.Text = Strings.WantedBoard.Reputation.ToString(SaiyanHud.Amount(reputation), SaiyanHud.Amount(num3));
			_gradeNext.Text = Strings.WantedBoard.NextGrade.ToString(WantedBoardWindow.GradeName(num + 1), SaiyanHud.Amount(num3));
			float num4 = ((num3 > num2) ? Math.Clamp((float)(reputation - num2) / (float)(num3 - num2), 0f, 1f) : 1f);
			_gradeFill.SetSize(Math.Max(0, (int)(196f * num4)), 4);
		}
		else
		{
			_gradeReputation.Text = SaiyanHud.Amount(reputation);
			_gradeNext.Text = Strings.WantedBoard.TopGrade;
			_gradeFill.SetSize(196, 4);
		}
		_gradeFill.IsHidden = _gradeFill.Width <= 0;
	}

	private Base BuildRow(WantedTurnInEntryPacket entry)
	{
		ImagePanel imagePanel = new ImagePanel(this, "Row")
		{
			MouseInputEnabled = false,
			Texture = GameContentManager.Current.GetTexture(TextureType.Gui, "encaisser_ligne.png")
		};
		imagePanel.SetSize(592, 84);
		WantedBoardWindow.PosterPortrait posterPortrait = new WantedBoardWindow.PosterPortrait(imagePanel, "Portrait")
		{
			Sprite = entry.Sprite,
			Tint = SaiyanHud.White
		};
		posterPortrait.SetBounds(13, 11, 62, 62);
		Label label = WantedBoardWindow.MakeLabel(imagePanel, "Name", 15, White, _strong);
		label.Text = entry.Name;
		label.SetBounds(88, 6, 300, 24);
		Label label2 = WantedBoardWindow.MakeLabel(imagePanel, "Who", 9, Dim, _body);
		label2.Text = Strings.WantedBoard.EliteLevel.ToString(entry.Level);
		label2.SetBounds(88, 30, 300, 18);
		string text = WantedPlaces.ZoneOf(entry.MapId);
		string text2 = entry.Place?.Trim() ?? string.Empty;
		string text3 = ((text.Length == 0) ? text2 : ((text2.Length != 0 && !text2.Contains(text, StringComparison.OrdinalIgnoreCase)) ? (text + " (" + text2 + ")") : ((text2.Length == 0) ? text : text2)));
		string text4 = Strings.WantedTurnIn.Killed.ToString((text3.Length > 0) ? text3 : Strings.WantedBoard.UnknownPlace.ToString(), WantedPlaces.Elapsed(entry.Minutes));
		List<string> list = WantedBoardWindow.Wrap(text4, 290, 9, _body);
		Label label3 = WantedBoardWindow.MakeLabel(imagePanel, "Where", 9, Soft, _body);
		label3.Text = ((list.Count > 1) ? (list[0] + "…") : list[0]);
		label3.SetBounds(88, 50, 300, 20);
		string text5 = Strings.WantedBoard.Reward.ToString(SaiyanHud.Amount(entry.Zeni), SaiyanHud.Amount(entry.Experience));
		Label label4 = WantedBoardWindow.MakeLabel(imagePanel, "Reward", 17, Gold, _strong);
		label4.Text = text5;
		label4.TextAlign = Pos.Right | Pos.CenterV;
		label4.SetBounds(390, 14, 190, 28);
		ImagePanel imagePanel2 = new ImagePanel(imagePanel, "Coin")
		{
			MouseInputEnabled = false,
			Texture = GameContentManager.Current.GetTexture(TextureType.Gui, "saiyan_echange_coin.png")
		};
		imagePanel2.SetBounds(580 - WantedBoardWindow.Measure(_strong, 17, text5) - 18, 22, 12, 12);
		Label label5 = WantedBoardWindow.MakeLabel(imagePanel, "Reputation", 12, PoliceBlue, _strong);
		label5.Text = (Honest ? Strings.WantedTurnIn.ReputationGain.ToString(entry.Reputation) : Strings.WantedTurnIn.ReputationFrozen.ToString());
		label5.TextAlign = Pos.Right | Pos.CenterV;
		label5.SetBounds(390, 44, 190, 22);
		return imagePanel;
	}

	private int FrameY(int windowY)
	{
		return windowY - _laidOutTitleBarHeight;
	}

	private void Layout()
	{
		_laidOutTitleBarHeight = base.TitleBarBounds.Height;
		_title.SetSize(340, 36);
		_title.SetPosition(24, FrameY(12));
		_subtitle.SetSize(340, 20);
		_subtitle.SetPosition(26, FrameY(52));
		_close.SetSize(24, 24);
		_close.SetPosition(596, FrameY(14));
		_gradeChip.SetSize(220, 58);
		_gradeChip.SetPosition(364, FrameY(12));
		_gradeName.SetBounds(12, 4, 120, 22);
		_gradeReputation.SetBounds(120, 4, 88, 22);
		_gradeFill.SetPosition(12, 28);
		_gradeNext.SetBounds(12, 34, 196, 18);
		for (int i = 0; i < _lines.Count; i++)
		{
			_lines[i].SetPosition(24, FrameY(112 + i * 92));
		}
		int num = 112 + _rows * 92 - 8 + 12;
		if (_totalLabel != null && _totalValue != null)
		{
			_totalLabel.SetBounds(36, FrameY(num), 120, 32);
			_totalValue.SetBounds(164, FrameY(num), 440, 32);
			num += 40;
		}
		_walletLabel.SetBounds(24, FrameY(num), 200, 24);
		_walletGain.SetBounds(526, FrameY(num), 90, 24);
		_walletValue.SetBounds(318, FrameY(num), 200, 24);
		num += 30;
		if (_promo != null)
		{
			_promo.SetBounds(24, FrameY(num), 592, 70);
			_promoTitle.SetBounds(14, 6, 564, 24);
			_promoOpens.SetBounds(14, 28, 564, 20);
			_promoNext.SetBounds(14, 46, 564, 18);
			num += 78;
		}
		_cashIn.SetSize(150, 30);
		_cashIn.SetPosition(466, FrameY(num + 4));
		_later.SetSize(150, 30);
		_later.SetPosition(306, FrameY(num + 4));
	}
}
