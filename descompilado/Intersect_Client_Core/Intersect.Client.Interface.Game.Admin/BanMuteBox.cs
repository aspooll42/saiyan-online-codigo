using System;
using System.Collections.Generic;
using Intersect.Client.Framework.Content;
using Intersect.Client.Framework.File_Management;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.Framework.Gwen;
using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.Framework.Gwen.ControlInternal;
using Intersect.Client.Interface.Shared;
using Intersect.Client.Localization;

namespace Intersect.Client.Interface.Game.Admin;

public class BanMuteBox : WindowControl
{
	private const int Largeur = 440;

	private const int Hauteur = 240;

	private const int BarreDeTitre = 40;

	private const int BoutonLargeur = 128;

	private const int BoutonHauteur = 28;

	private static readonly Color White = new Color(255, 242, 242, 242);

	private static readonly Color Entry = new Color(255, 196, 204, 220);

	private static readonly Color Dim = new Color(255, 150, 160, 182);

	private static readonly Color Night = new Color(255, 11, 14, 23);

	private static readonly Color OrangeLight = new Color(255, 255, 172, 82);

	private static readonly Color GoldLight = new Color(255, 255, 226, 150);

	private readonly TextBox _textboxReason;

	private readonly ComboBox _comboboxDuration;

	private readonly ImagePanel _ipPill;

	private readonly Button _ipNo;

	private readonly Button _ipYes;

	private bool _banIp;

	private readonly Dictionary<string, int> _dayCount = new Dictionary<string, int>
	{
		{
			Strings.BanMute.OneDay,
			1
		},
		{
			Strings.BanMute.TwoDays,
			2
		},
		{
			Strings.BanMute.ThreeDays,
			3
		},
		{
			Strings.BanMute.FourDays,
			4
		},
		{
			Strings.BanMute.FiveDays,
			5
		},
		{
			Strings.BanMute.OneWeek,
			7
		},
		{
			Strings.BanMute.TwoWeeks,
			14
		},
		{
			Strings.BanMute.OneMonth,
			30
		},
		{
			Strings.BanMute.TwoMonths,
			60
		},
		{
			Strings.BanMute.SixMonths,
			180
		},
		{
			Strings.BanMute.OneYear,
			365
		},
		{
			Strings.BanMute.Forever,
			999999
		}
	};

	public BanMuteBox(string title, string prompt, EventHandler okayHandler)
		: base(Interface.GameUi.GameCanvas, title, modal: true, "BanMuteWindow")
	{
		BanMuteBox banMuteBox = this;
		DisableResizing();
		Interface.InputBlockingComponents.Add(this);
		GameContentManager current = GameContentManager.Current;
		IFont font = current.GetFont("chakrapetch");
		IFont font2 = current.GetFont("chakrapetchmedium") ?? font;
		SetSize(440, 240);
		base.InnerPanelPadding = new Padding(16, 10, 16, 16);
		IGameTexture texture = current.GetTexture(TextureType.Gui, "saiyan_dialogue.png");
		if (texture != null)
		{
			SetImage(texture, "saiyan_dialogue.png", ControlState.Active);
			SetImage(texture, "saiyan_dialogue.png", ControlState.Inactive);
		}
		base.IsClosable = false;
		base.Titlebar.ShouldDrawBackground = false;
		Dragger titlebar = base.Titlebar;
		Point minimumSize = base.Titlebar.MinimumSize;
		minimumSize.Y = 40;
		titlebar.MinimumSize = minimumSize;
		base.TitleLabel.IsHidden = false;
		base.TitleLabel.UsesHeadingFace = false;
		base.TitleLabel.Font = current.GetFont("refuel");
		base.TitleLabel.FontSize = 15;
		base.TitleLabel.TextColorOverride = White;
		base.TitleLabel.TextAlign = Pos.Left | Pos.CenterV;
		WindowTitle.Apply(base.TitleLabel);
		int num = 408;
		Label label = MakeLabel(this, "Prompt", 11, White, font);
		label.Text = prompt;
		label.SetSize(num, 22);
		label.SetPosition(0, 0);
		Label label2 = MakeLabel(this, "LabelReason", 11, Dim, font2);
		label2.Text = Strings.BanMute.Reason;
		label2.SetSize(92, 28);
		label2.SetPosition(0, 30);
		_textboxReason = new TextBox(this, "TextboxReason")
		{
			Font = font,
			FontSize = 11,
			MaximumLength = 80
		};
		_textboxReason.SetTextColor(White, ComponentState.Normal);
		IGameTexture texture2 = current.GetTexture(TextureType.Gui, "saiyan_field_260x28.png");
		if (texture2 != null)
		{
			_textboxReason.SetBackgroundTemplate(texture2, "saiyan_field_260x28.png");
		}
		_textboxReason.SetSize(num - 100, 28);
		_textboxReason.SetPosition(100, 30);
		Interface.FocusComponents.Add(_textboxReason);
		Label label3 = MakeLabel(this, "LabelDuration", 11, Dim, font2);
		label3.Text = Strings.BanMute.Duration;
		label3.SetSize(92, 28);
		label3.SetPosition(0, 68);
		_comboboxDuration = new ComboBox(this, "ComboBoxDuration");
		foreach (KeyValuePair<string, int> item in _dayCount)
		{
			_comboboxDuration.AddItem(item.Key, null, item.Value);
		}
		_comboboxDuration.SetSize(160, 28);
		_comboboxDuration.SetPosition(100, 68);
		Label label4 = MakeLabel(this, "LabelIp", 11, Dim, font2);
		label4.Text = Strings.BanMute.IncludeIp;
		label4.SetSize(92, 32);
		label4.SetPosition(0, 106);
		ImagePanel imagePanel = new ImagePanel(this, "IpBack")
		{
			MouseInputEnabled = true,
			Texture = current.GetTexture(TextureType.Gui, "saiyan_admin_bascule.png")
		};
		imagePanel.SetSize(130, 32);
		imagePanel.SetPosition(100, 106);
		_ipPill = new ImagePanel(imagePanel, "IpPill")
		{
			MouseInputEnabled = false,
			TextureNinePatchMargin = new Margin(5),
			TextureFilename = "saiyan_admin_bascule_actif.png"
		};
		_ipPill.SetSize(62, 26);
		_ipPill.SetPosition(3, 3);
		_ipNo = MakeHalf(imagePanel, "IpNo", Strings.BanMute.IpNo, 0, font);
		_ipYes = MakeHalf(imagePanel, "IpYes", Strings.BanMute.IpYes, 65, font);
		_ipNo.Clicked += delegate
		{
			banMuteBox._banIp = false;
			banMuteBox.ApplyIp();
		};
		_ipYes.Clicked += delegate
		{
			banMuteBox._banIp = true;
			banMuteBox.ApplyIp();
		};
		ApplyIp();
		Button button = MakeDialogButton("ButtonOkay", Strings.BanMute.Okay, num - 128, 148, font);
		button.Clicked += delegate
		{
			okayHandler?.Invoke(banMuteBox, EventArgs.Empty);
			banMuteBox.DelayedDelete();
		};
		Button button2 = MakeDialogButton("ButtonCancel", Strings.BanMute.Cancel, num - 256 - 8, 148, font);
		button2.Clicked += delegate
		{
			banMuteBox.DelayedDelete();
		};
		SaiyanDropdowns.Apply(this);
		Canvas gameCanvas = Interface.GameUi.GameCanvas;
		SetPosition((gameCanvas.Width - 440) / 2, (gameCanvas.Height - 240) / 2);
	}

	private void ApplyIp()
	{
		_ipPill.SetPosition(_banIp ? 65 : 3, 3);
		_ipNo.SetTextColor(_banIp ? Dim : GoldLight, ComponentState.Normal);
		_ipNo.SetTextColor(_banIp ? White : GoldLight, ComponentState.Hovered);
		_ipYes.SetTextColor(_banIp ? GoldLight : Dim, ComponentState.Normal);
		_ipYes.SetTextColor(_banIp ? GoldLight : White, ComponentState.Hovered);
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

	private static Button MakeHalf(Base parent, string name, string text, int x, IFont? font)
	{
		Button button = new Button(parent, name)
		{
			Font = font,
			FontSize = 10,
			TextAlign = Pos.Center,
			UsesHeadingFace = false,
			Text = text
		};
		ComponentState[] array = new ComponentState[3]
		{
			ComponentState.Normal,
			ComponentState.Hovered,
			ComponentState.Active
		};
		foreach (ComponentState componentState in array)
		{
			button.SetStateTexture(componentState, "saiyan_admin_transparent.png");
		}
		button.SetSize(65, 32);
		button.SetPosition(x, 0);
		return button;
	}

	private Button MakeDialogButton(string name, string text, int x, int y, IFont? font)
	{
		Button button = new Button(this, name)
		{
			Font = font,
			FontSize = 11,
			TextAlign = Pos.Center,
			UsesHeadingFace = false,
			AutoSizeToContents = false,
			Padding = Padding.Zero,
			Text = text
		};
		button.SetStateTexture(ComponentState.Normal, "saiyan_btn_128x28.png");
		button.SetStateTexture(ComponentState.Hovered, "saiyan_btn_128x28_hover.png");
		button.SetStateTexture(ComponentState.Active, "saiyan_btn_128x28_clicked.png");
		button.SetStateTexture(ComponentState.Disabled, "saiyan_btn_128x28_disabled.png");
		button.SetTextColor(Entry, ComponentState.Normal);
		button.SetTextColor(OrangeLight, ComponentState.Hovered);
		button.SetTextColor(Night, ComponentState.Active);
		button.SetTextColor(Dim, ComponentState.Disabled);
		button.SetSize(128, 28);
		button.SetPosition(x, y);
		return button;
	}

	protected override void Dispose(bool disposing)
	{
		Close();
		Interface.GameUi.GameCanvas.RemoveChild(this, dispose: false);
		base.Dispose(disposing);
	}

	public int GetDuration()
	{
		object obj = _comboboxDuration.SelectedItem?.UserData;
		if (obj is int)
		{
			return (int)obj;
		}
		return 1;
	}

	public string GetReason()
	{
		return _textboxReason.Text;
	}

	public bool BanIp()
	{
		return _banIp;
	}
}
