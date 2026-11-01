using System;
using System.Collections.Generic;
using Intersect.Client.Entities;
using Intersect.Client.Framework.Content;
using Intersect.Client.Framework.File_Management;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.Framework.Gwen;
using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.General;
using Intersect.Client.Interface.Shared;
using Intersect.Client.Localization;
using Intersect.Client.Networking;
using Intersect.Config;
using Intersect.Framework.Core;

namespace Intersect.Client.Interface.Game;

public sealed class HellBailWindow : WindowControl
{
	private const int WindowWidth = 440;

	private const int WindowHeight = 286;

	private const int Gutter = 24;

	private const int ContentWidth = 392;

	private const int CoinFrames = 4;

	private const int CoinRestMs = 1600;

	private const int CoinFrameMs = 90;

	private static readonly Color White = new Color(255, 242, 242, 242);

	private static readonly Color Entry = new Color(255, 196, 204, 220);

	private static readonly Color Muted = new Color(255, 176, 186, 206);

	private static readonly Color Dim = new Color(255, 150, 160, 182);

	private static readonly Color Night = new Color(255, 11, 14, 23);

	private static readonly Color OrangeLight = new Color(255, 255, 172, 82);

	private static readonly Color Gold = new Color(255, 245, 190, 90);

	private static readonly Color Alert = new Color(255, 255, 120, 110);

	private static HellBailWindow? _open;

	private readonly long _bail;

	private readonly IFont? _body;

	private readonly IGameTexture? _coinStrip;

	private readonly List<ImagePanel> _coins = new List<ImagePanel>();

	private readonly Label _title;

	private readonly Button _close;

	private readonly Label _counter;

	private readonly Label _hint;

	private readonly Label _sentenceCaption;

	private readonly Label _sentence;

	private readonly ImagePanel _rule;

	private readonly Label _bailCaption;

	private readonly Label _bailAmount;

	private readonly ImagePanel _bailMark;

	private readonly Label _balanceCaption;

	private readonly Label _balanceAmount;

	private readonly ImagePanel _balanceMark;

	private readonly Button _wait;

	private readonly Button _pay;

	private bool _finished;

	private int _laidOutTitleBarHeight = -1;

	private int _coinFrame = -1;

	private string _shown = "?";

	public static HellBailWindow? Current
	{
		get
		{
			HellBailWindow open = _open;
			if (open == null || open.IsHidden)
			{
				return null;
			}
			return open;
		}
	}

	private HellBailWindow(long bail, DetentionPlace place)
		: base(Interface.CurrentInterface.Root, string.Empty, modal: true, "HellBailWindow")
	{
		_bail = bail;
		bool flag = place == DetentionPlace.Prison;
		GameContentManager current = GameContentManager.Current;
		_body = current.GetFont("chakrapetch");
		IFont font = current.GetFont("rajdhanibold");
		IFont font2 = current.GetFont("refuel");
		Interface.InputBlockingComponents.Add(this);
		base.DeleteOnClose = true;
		DisableResizing();
		base.IsClosable = false;
		base.Titlebar.ShouldDrawBackground = false;
		base.TitleLabel.IsHidden = true;
		SetSize(440, 286);
		IGameTexture texture = current.GetTexture(TextureType.Gui, "saiyan_caution.png");
		SetImage(texture, "saiyan_caution.png", ControlState.Active);
		SetImage(texture, "saiyan_caution.png", ControlState.Inactive);
		_coinStrip = current.GetTexture(TextureType.Item, "zeni_anime.png");
		_title = MakeLabel(this, "Title", 18, White, font2);
		_title.Text = (flag ? Strings.Hell.PrisonWindowTitle : Strings.Hell.WindowTitle).ToString().ToUpperInvariant();
		WindowTitle.Apply(_title);
		_close = new Button(this, "Close", disableText: true);
		_close.SetStateTexture(ComponentState.Normal, "saiyan_close.png");
		_close.SetStateTexture(ComponentState.Hovered, "saiyan_close_hover.png");
		_close.SetStateTexture(ComponentState.Active, "saiyan_close_clicked.png");
		_close.Clicked += delegate
		{
			Cancel();
		};
		_counter = MakeLabel(this, "Counter", 12, White, _body);
		_counter.Text = (flag ? Strings.Hell.PrisonCounterName : Strings.Hell.CounterName);
		_hint = MakeLabel(this, "Hint", 10, Muted, _body);
		_hint.Text = Strings.Hell.CounterHint;
		_sentenceCaption = MakeLabel(this, "SentenceCaption", 10, Muted, _body);
		_sentenceCaption.Text = Strings.Hell.SentenceLeft.ToString().ToUpperInvariant();
		_sentenceCaption.TextAlign = Pos.Right | Pos.CenterV;
		_sentence = MakeLabel(this, "Sentence", 14, White, font);
		_sentence.TextAlign = Pos.Right | Pos.CenterV;
		_rule = MakeImage(this, "Rule", "hdv_filet.png");
		_bailCaption = MakeLabel(this, "BailCaption", 12, Entry, _body);
		_bailCaption.Text = Strings.Hell.BailCaption;
		_bailAmount = MakeLabel(this, "BailAmount", 20, Gold, font);
		_bailAmount.Text = MarketWindow.FormatAmount(bail);
		_bailAmount.TextAlign = Pos.Right | Pos.CenterV;
		_bailMark = MakeCoin("BailMark", 22);
		_balanceCaption = MakeLabel(this, "BalanceCaption", 10, Muted, _body);
		_balanceAmount = MakeLabel(this, "BalanceAmount", 13, Muted, font);
		_balanceAmount.TextAlign = Pos.Right | Pos.CenterV;
		_balanceMark = MakeCoin("BalanceMark", 16);
		_wait = MakeButton("Wait", "saiyan_btn_128x34", Entry);
		_wait.Text = Strings.Hell.Wait;
		_wait.Clicked += delegate
		{
			Cancel();
		};
		_pay = MakeButton("Pay", "saiyan_cta_244x34", Night);
		_pay.SetTextColor(Night, ComponentState.Hovered);
		_pay.Text = Strings.Hell.Pay;
		_pay.Clicked += delegate
		{
			Confirm();
		};
		Layout();
		SetPosition(Math.Max(0, (Interface.CurrentInterface.Root.Width - 440) / 2), Math.Max(0, (Interface.CurrentInterface.Root.Height - 286) / 2));
		Refresh();
		Show();
		BringToFront();
	}

	public static void Open(long bail, DetentionPlace place)
	{
		_open?.Cancel();
		_open = new HellBailWindow(bail, place);
	}

	public void Confirm()
	{
		if (!_finished && CanAfford())
		{
			Finish();
			PacketSender.SendPayHellBail();
		}
	}

	public void Cancel()
	{
		if (!_finished)
		{
			Finish();
		}
	}

	private void Finish()
	{
		_finished = true;
		if (_open == this)
		{
			_open = null;
		}
		Interface.InputBlockingComponents.Remove(this);
		Close();
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
		Player me = Globals.Me;
		if (me == null || !me.IsDetained)
		{
			Cancel();
			return;
		}
		if (base.TitleBarBounds.Height != _laidOutTitleBarHeight)
		{
			Layout();
		}
		Refresh();
		AnimateCoins();
	}

	private bool CanAfford()
	{
		Player me = Globals.Me;
		if (me != null)
		{
			return me.Zeni >= _bail;
		}
		return false;
	}

	private void Refresh()
	{
		Player me = Globals.Me;
		if (me != null)
		{
			long hellSentenceRemainingMs = me.HellSentenceRemainingMs;
			long zeni = me.Zeni;
			string text = $"{hellSentenceRemainingMs / 1000}:{zeni}";
			if (!(text == _shown))
			{
				_shown = text;
				_sentence.Text = $"{hellSentenceRemainingMs / 60000:00}:{hellSentenceRemainingMs / 1000 % 60:00}";
				bool flag = zeni >= _bail;
				_bailAmount.SetTextColor(flag ? Gold : Alert, ComponentState.Normal);
				_balanceCaption.Text = (flag ? Strings.Hell.BalanceAfter : Strings.Hell.Missing);
				_balanceAmount.Text = MarketWindow.FormatAmount(flag ? (zeni - _bail) : (_bail - zeni));
				_balanceAmount.SetTextColor(flag ? Muted : Alert, ComponentState.Normal);
				_pay.IsDisabled = !flag;
			}
		}
	}

	private void AnimateCoins()
	{
		IGameTexture coinStrip = _coinStrip;
		if (coinStrip == null || coinStrip.Width <= 0)
		{
			return;
		}
		int num = 1870;
		long num2 = Timing.Global.Milliseconds % num;
		int num3 = ((num2 >= 1600) ? Math.Min(3, 1 + (int)((num2 - 1600) / 90)) : 0);
		if (num3 == _coinFrame)
		{
			return;
		}
		_coinFrame = num3;
		int num4 = coinStrip.Width / 4;
		foreach (ImagePanel coin in _coins)
		{
			int width = coin.Width;
			coin.SetTextureRect(num3 * num4, 0, num4, coinStrip.Height);
			coin.SetSize(width, width);
		}
	}

	private int FrameY(int windowY)
	{
		return windowY - _laidOutTitleBarHeight;
	}

	private void Layout()
	{
		_laidOutTitleBarHeight = base.TitleBarBounds.Height;
		_title.SetSize(340, 34);
		_title.SetPosition(24, FrameY(9));
		_close.SetSize(24, 24);
		_close.SetPosition(396, FrameY(14));
		_counter.SetSize(252, 24);
		_counter.SetPosition(24, FrameY(68));
		_hint.SetSize(280, 20);
		_hint.SetPosition(24, FrameY(92));
		_sentenceCaption.SetSize(130, 20);
		_sentenceCaption.SetPosition(286, FrameY(68));
		_sentence.SetSize(130, 26);
		_sentence.SetPosition(286, FrameY(90));
		_rule.SetSize(392, 1);
		_rule.SetPosition(24, FrameY(128));
		_bailCaption.SetSize(160, 36);
		_bailCaption.SetPosition(24, FrameY(140));
		_bailAmount.SetSize(170, 36);
		_bailAmount.SetPosition(220, FrameY(140));
		_bailMark.SetPosition(394, FrameY(147));
		_balanceCaption.SetSize(220, 24);
		_balanceCaption.SetPosition(24, FrameY(176));
		_balanceAmount.SetSize(140, 24);
		_balanceAmount.SetPosition(256, FrameY(176));
		_balanceMark.SetPosition(400, FrameY(180));
		int windowY = 234;
		_wait.SetPosition(24, FrameY(windowY));
		_pay.SetPosition(172, FrameY(windowY));
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

	private static ImagePanel MakeImage(Base parent, string name, string texture)
	{
		return new ImagePanel(parent, name)
		{
			MouseInputEnabled = false,
			Texture = GameContentManager.Current.GetTexture(TextureType.Gui, texture)
		};
	}

	private ImagePanel MakeCoin(string name, int size)
	{
		ImagePanel imagePanel = new ImagePanel(this, name)
		{
			MouseInputEnabled = false,
			Texture = _coinStrip
		};
		IGameTexture coinStrip = _coinStrip;
		if (coinStrip != null && coinStrip.Width > 0)
		{
			imagePanel.SetTextureRect(0, 0, coinStrip.Width / 4, coinStrip.Height);
		}
		imagePanel.SetSize(size, size);
		_coins.Add(imagePanel);
		return imagePanel;
	}

	private Button MakeButton(string name, string key, Color ink)
	{
		string[] array = key.Split('_')[^1].Split('x');
		int width = int.Parse(array[0]);
		int height = int.Parse(array[1]);
		Button button = new Button(this, name)
		{
			Font = _body,
			FontSize = 11,
			TextAlign = Pos.Center,
			UsesHeadingFace = false
		};
		button.SetStateTexture(ComponentState.Normal, key + ".png");
		button.SetStateTexture(ComponentState.Hovered, key + "_hover.png");
		button.SetStateTexture(ComponentState.Active, key + "_clicked.png");
		button.SetStateTexture(ComponentState.Disabled, key + "_disabled.png");
		button.SetTextColor(ink, ComponentState.Normal);
		button.SetTextColor(OrangeLight, ComponentState.Hovered);
		button.SetTextColor(Night, ComponentState.Active);
		button.SetTextColor(Dim, ComponentState.Disabled);
		button.SetSize(width, height);
		return button;
	}
}
