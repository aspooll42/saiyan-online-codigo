using System;
using System.Collections.Generic;
using System.Linq;
using Intersect.Client.Framework.Content;
using Intersect.Client.Framework.File_Management;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.Framework.Gwen;
using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.Framework.Gwen.Renderer;
using Intersect.Client.Framework.Gwen.Skin;
using Intersect.Client.Interface.Game.EntityPanel;
using Intersect.Client.Interface.Shared;
using Intersect.Client.Localization;
using Intersect.Client.Networking;
using Intersect.Config;
using Intersect.Framework.Core;
using Intersect.Localization;
using Intersect.Network.Packets.Server;

namespace Intersect.Client.Interface.Game;

public sealed class TrackingRadarWindow : WindowControl
{
	private enum ScopeState
	{
		Idle,
		Searching,
		Found,
		Jammed
	}

	private sealed class RadarScope : Intersect.Client.Framework.Gwen.Control.Base
	{
		private static readonly Color White = new Color(255, 255, 255, 255);

		private Guid _target;

		public ScopeState State { get; private set; }

		public long Since { get; private set; }

		protected override bool ShouldClip => false;

		public RadarScope(Intersect.Client.Framework.Gwen.Control.Base parent, string name)
			: base(parent, name)
		{
			base.MouseInputEnabled = false;
		}

		public void Set(ScopeState state, Guid target)
		{
			State = state;
			_target = target;
			Since = Timing.Global.Milliseconds;
		}

		protected override void Render(Intersect.Client.Framework.Gwen.Skin.Base skin)
		{
			base.Render(skin);
			Intersect.Client.Framework.Gwen.Renderer.Base renderer = skin.Renderer;
			long milliseconds = Timing.Global.Milliseconds;
			int width = base.Width;
			int num = width / 2;
			SaiyanHud.Draw(renderer, SaiyanHud.Texture("radar_ecran.png"), 0, 0, width, width, White);
			if (renderer is IntersectRenderer intersectRenderer)
			{
				IGameTexture gameTexture = SaiyanHud.Texture("radar_balayage.png");
				if (gameTexture != null)
				{
					long num2 = ((State == ScopeState.Searching) ? 700 : 2600);
					intersectRenderer.DrawColor = White;
					intersectRenderer.DrawTextureRotated(gameTexture, 0f, 0f, width, width, (float)(milliseconds % num2) * 360f / (float)num2);
				}
			}
			SaiyanHud.Draw(renderer, SaiyanHud.Texture("radar_centre.png"), num - 6, num - 6, 12, 12, White);
			if (State == ScopeState.Jammed)
			{
				long value = milliseconds / 90 % 3;
				SaiyanHud.Draw(renderer, SaiyanHud.Texture($"radar_grain_{value}.png"), 0, 0, width, width, White);
			}
			if (State == ScopeState.Found)
			{
				int num3 = _target.GetHashCode() & 0x7FFFFFFF;
				float x = (float)(num3 % 360) * (float)Math.PI / 180f;
				float num4 = 0.35f + (float)(num3 / 360 % 45) / 100f;
				float num5 = (float)num + MathF.Cos(x) * (float)(num - 12) * num4;
				float num6 = (float)num + MathF.Sin(x) * (float)(num - 12) * num4;
				float num7 = (float)((milliseconds - Since) % 1200) / 1200f;
				int num8 = (int)(20f + 60f * num7);
				Color color = new Color((int)(255f * (1f - num7)), 255, 255, 255);
				SaiyanHud.Draw(renderer, SaiyanHud.Texture("radar_anneau.png"), (int)(num5 - (float)num8 / 2f), (int)(num6 - (float)num8 / 2f), num8, num8, color);
				Color color2 = (((milliseconds - Since) / 300 % 2 == 0L) ? White : new Color(255, 255, 190, 120));
				SaiyanHud.Draw(renderer, SaiyanHud.Texture("radar_point.png"), (int)num5 - 12, (int)num6 - 12, 24, 24, color2);
			}
		}
	}

	private const int WindowWidth = 620;

	private const int WindowHeight = 372;

	private const int ScopeX = 30;

	private const int ScopeY = 90;

	private const int ScopeSize = 220;

	private const int ListX = 280;

	private const int ListY = 112;

	private const int RowWidth = 316;

	private const int RowHeight = 44;

	private const int RowStep = 48;

	private const int RowsPerPage = 4;

	private const long SearchMs = 1600L;

	private const long SearchGiveUpMs = 6000L;

	private static readonly Color White = new Color(255, 242, 242, 242);

	private static readonly Color Muted = new Color(255, 176, 186, 206);

	private static readonly Color Dim = new Color(255, 138, 150, 176);

	private static readonly Color Night = new Color(255, 11, 14, 23);

	private static readonly Color OrangeLight = new Color(255, 255, 172, 82);

	private static readonly Color Gold = new Color(255, 255, 208, 106);

	private static readonly Color Alert = new Color(255, 255, 154, 134);

	private static readonly Color Green = new Color(255, 78, 200, 120);

	private static readonly Color GreenLight = new Color(255, 140, 235, 170);

	private static readonly Color CriminalRed = new Color(255, 232, 64, 64);

	private static readonly Color AbsentTint = new Color(210, 150, 156, 168);

	private static TrackingRadarWindow? _open;

	private readonly IFont? _body;

	private readonly IFont? _strong;

	private readonly TrackingRadarEntryPacket[] _entries;

	private readonly Label _title;

	private readonly Button _close;

	private readonly Label _stock;

	private readonly RadarScope _scope;

	private readonly ImagePanel _jamBand;

	private readonly Label _jamText;

	private readonly Label _line1;

	private readonly Label _line2;

	private readonly Label _caption;

	private readonly Label _empty;

	private readonly Button _pageUp;

	private readonly Button _pageDown;

	private readonly Button _locate;

	private readonly List<Button> _rows = new List<Button>();

	private int _radars;

	private int _page;

	private TrackingRadarEntryPacket? _chosen;

	private (Guid Target, TrackingRadarResult Result, string Map, int Radars)? _pending;

	private bool _finished;

	private int _laidOutTitleBarHeight = -1;

	public static TrackingRadarWindow? Current
	{
		get
		{
			TrackingRadarWindow open = _open;
			if (open == null || open.IsHidden || open._finished)
			{
				return null;
			}
			return open;
		}
	}

	private TrackingRadarWindow(TrackingRadarEntryPacket[] entries, int radars)
		: base(Interface.CurrentInterface.Root, string.Empty, modal: true, "TrackingRadarWindow")
	{
		_entries = entries;
		_radars = radars;
		_chosen = entries.FirstOrDefault((TrackingRadarEntryPacket entry) => entry.Online) ?? entries.FirstOrDefault();
		GameContentManager current = GameContentManager.Current;
		_body = current.GetFont("chakrapetch");
		_strong = current.GetFont("rajdhanibold");
		IFont font = current.GetFont("refuel");
		Interface.InputBlockingComponents.Add(this);
		base.DeleteOnClose = true;
		DisableResizing();
		base.IsClosable = false;
		base.Titlebar.ShouldDrawBackground = false;
		base.TitleLabel.IsHidden = true;
		SetSize(620, 372);
		IGameTexture texture = current.GetTexture(TextureType.Gui, "saiyan_radar.png");
		SetImage(texture, "saiyan_radar.png", ControlState.Active);
		SetImage(texture, "saiyan_radar.png", ControlState.Inactive);
		_title = MakeLabel(this, "Title", 18, White, font);
		_title.Text = Strings.TrackingRadar.Title.ToString().ToUpperInvariant();
		WindowTitle.Apply(_title);
		_close = new Button(this, "Close", disableText: true);
		_close.SetStateTexture(ComponentState.Normal, "saiyan_close.png");
		_close.SetStateTexture(ComponentState.Hovered, "saiyan_close_hover.png");
		_close.SetStateTexture(ComponentState.Active, "saiyan_close_clicked.png");
		_close.Clicked += delegate
		{
			CloseRadar();
		};
		_stock = MakeLabel(this, "Stock", 10, Muted, _body);
		_scope = new RadarScope(this, "Scope");
		_jamBand = new ImagePanel(this, "JamBand")
		{
			MouseInputEnabled = false,
			Texture = current.GetTexture(TextureType.Gui, "radar_bandeau.png")
		};
		_jamText = MakeLabel(this, "JamText", 12, Alert, _body);
		_jamText.TextAlign = Pos.Center;
		_jamText.Text = Strings.TrackingRadar.JammedScreen;
		_line1 = MakeLabel(this, "Line1", 11, White, _body);
		_line1.TextAlign = Pos.Center;
		_line2 = MakeLabel(this, "Line2", 10, Muted, _body);
		_line2.TextAlign = Pos.Center;
		_caption = MakeLabel(this, "Caption", 10, Muted, _body);
		_caption.Text = Strings.TrackingRadar.Wanted.ToString().ToUpperInvariant();
		_empty = MakeLabel(this, "Empty", 11, Dim, _body);
		_empty.TextAlign = Pos.Center;
		_empty.Text = Strings.TrackingRadar.NoneWanted;
		_pageUp = MakePageButton("PageUp", "radar_page_haut");
		_pageUp.Clicked += delegate
		{
			TurnPage(-1);
		};
		_pageDown = MakePageButton("PageDown", "radar_page_bas");
		_pageDown.Clicked += delegate
		{
			TurnPage(1);
		};
		_locate = new Button(this, "Locate")
		{
			Font = _body,
			FontSize = 11,
			TextAlign = Pos.Center,
			UsesHeadingFace = false
		};
		_locate.SetStateTexture(ComponentState.Normal, "saiyan_cta_244x34.png");
		_locate.SetStateTexture(ComponentState.Hovered, "saiyan_cta_244x34_hover.png");
		_locate.SetStateTexture(ComponentState.Active, "saiyan_cta_244x34_clicked.png");
		_locate.SetStateTexture(ComponentState.Disabled, "saiyan_cta_244x34_disabled.png");
		_locate.SetTextColor(Night, ComponentState.Normal);
		_locate.SetTextColor(Night, ComponentState.Hovered);
		_locate.SetTextColor(Night, ComponentState.Active);
		_locate.SetTextColor(Dim, ComponentState.Disabled);
		_locate.SetSize(244, 34);
		_locate.Clicked += delegate
		{
			Locate();
		};
		if (_chosen != null)
		{
			_page = Math.Max(0, Array.IndexOf<TrackingRadarEntryPacket>(_entries, _chosen)) / 4;
		}
		BuildRows();
		Layout();
		SetPosition(Math.Max(0, (Interface.CurrentInterface.Root.Width - 620) / 2), Math.Max(0, (Interface.CurrentInterface.Root.Height - 372) / 2));
		ShowIdle();
		Refresh();
		Show();
		BringToFront();
	}

	public static void Open(TrackingRadarEntryPacket[] entries, int radars)
	{
		_open?.CloseRadar();
		_open = new TrackingRadarWindow(entries, radars);
	}

	public void ShowResult(Guid target, TrackingRadarResult result, string map, int radars)
	{
		_pending = (target, result, map ?? string.Empty, radars);
	}

	public void CloseRadar()
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
		if (_scope.State != ScopeState.Searching)
		{
			return;
		}
		long num = Timing.Global.Milliseconds - _scope.Since;
		(Guid, TrackingRadarResult, string, int)? pending = _pending;
		if (pending.HasValue)
		{
			(Guid, TrackingRadarResult, string, int) valueOrDefault = pending.GetValueOrDefault();
			if (num >= 1600)
			{
				_pending = null;
				Apply(valueOrDefault.Item1, valueOrDefault.Item2, valueOrDefault.Item3, valueOrDefault.Item4);
				return;
			}
		}
		if (num >= 6000)
		{
			_pending = null;
			ShowIdle();
			Refresh();
		}
	}

	private void Locate()
	{
		TrackingRadarEntryPacket chosen = _chosen;
		if (chosen != null && chosen.Online && _radars > 0 && _scope.State != ScopeState.Searching)
		{
			_pending = null;
			_scope.Set(ScopeState.Searching, chosen.Id);
			SetLines(Strings.TrackingRadar.SearchingSignal, string.Empty, White);
			Refresh();
			PacketSender.SendTrackingRadarLocate(chosen.Id);
		}
	}

	private void Apply(Guid target, TrackingRadarResult result, string map, int radars)
	{
		_radars = radars;
		string text = _entries.FirstOrDefault((TrackingRadarEntryPacket entry) => entry.Id == target)?.Name ?? string.Empty;
		switch (result)
		{
		case TrackingRadarResult.Found:
			_scope.Set(ScopeState.Found, target);
			SetLines(Strings.TrackingRadar.Found.ToString(text, map), Strings.TrackingRadar.Used, Gold);
			break;
		case TrackingRadarResult.Jammed:
			_scope.Set(ScopeState.Jammed, target);
			SetLines(Strings.TrackingRadar.Jammed, Strings.TrackingRadar.Kept, Alert);
			break;
		case TrackingRadarResult.Offline:
			MarkOffline(target);
			_scope.Set(ScopeState.Idle, Guid.Empty);
			SetLines(Strings.TrackingRadar.WentOffline.ToString(text), Strings.TrackingRadar.Kept, White);
			break;
		case TrackingRadarResult.NotWanted:
			_scope.Set(ScopeState.Idle, Guid.Empty);
			SetLines(Strings.TrackingRadar.NotWanted.ToString(text), Strings.TrackingRadar.Kept, White);
			break;
		case TrackingRadarResult.NotAllowed:
			_scope.Set(ScopeState.Idle, Guid.Empty);
			SetLines(Strings.TrackingRadar.NotAllowed, string.Empty, Alert);
			break;
		default:
			_scope.Set(ScopeState.Idle, Guid.Empty);
			SetLines(Strings.TrackingRadar.NoRadar, string.Empty, Alert);
			break;
		}
		Refresh();
	}

	private void MarkOffline(Guid target)
	{
		TrackingRadarEntryPacket trackingRadarEntryPacket = _entries.FirstOrDefault((TrackingRadarEntryPacket candidate) => candidate.Id == target);
		if (trackingRadarEntryPacket != null)
		{
			trackingRadarEntryPacket.Online = false;
			trackingRadarEntryPacket.OfflineMinutes = 0;
			BuildRows();
			Layout();
		}
	}

	private void ShowIdle()
	{
		_scope.Set(ScopeState.Idle, Guid.Empty);
		if (_chosen == null)
		{
			SetLines(Strings.TrackingRadar.NoneWanted, string.Empty, White);
		}
		else if (!_chosen.Online)
		{
			SetLines(Strings.TrackingRadar.TargetOffline.ToString(_chosen.Name), Strings.TrackingRadar.Kept, White);
		}
		else if (_radars <= 0)
		{
			SetLines(Strings.TrackingRadar.NoRadar, string.Empty, Alert);
		}
		else
		{
			SetLines(Strings.TrackingRadar.Target.ToString(_chosen.Name), Strings.TrackingRadar.Warned, White);
		}
	}

	private void SetLines(string first, string second, Color color)
	{
		_line1.Text = first;
		_line1.SetTextColor(color, ComponentState.Normal);
		_line2.Text = second;
	}

	private void Refresh()
	{
		_stock.Text = ((_radars == 1) ? ((string)Strings.TrackingRadar.StockOne) : Strings.TrackingRadar.Stock.ToString(_radars));
		bool flag = _scope.State == ScopeState.Searching;
		bool flag2 = _chosen?.Online ?? false;
		_locate.Text = (flag ? Strings.TrackingRadar.Searching : ((flag2 || _chosen == null) ? Strings.TrackingRadar.Locate : Strings.TrackingRadar.Offline));
		_locate.IsDisabled = flag || !flag2 || _radars <= 0;
		int num = Math.Max(1, (_entries.Length + 4 - 1) / 4);
		_pageUp.IsHidden = num <= 1;
		_pageDown.IsHidden = num <= 1;
		_pageUp.IsDisabled = _page <= 0;
		_pageDown.IsDisabled = _page >= num - 1;
		bool flag3 = _scope.State == ScopeState.Jammed;
		_jamBand.IsHidden = !flag3;
		_jamText.IsHidden = !flag3;
		_empty.IsHidden = _entries.Length != 0;
	}

	private void TurnPage(int delta)
	{
		int num = Math.Max(1, (_entries.Length + 4 - 1) / 4);
		int num2 = Math.Clamp(_page + delta, 0, num - 1);
		if (num2 != _page)
		{
			_page = num2;
			BuildRows();
			Layout();
			Refresh();
		}
	}

	private void Choose(TrackingRadarEntryPacket entry)
	{
		if (_scope.State != ScopeState.Searching && entry != _chosen)
		{
			_chosen = entry;
			_pending = null;
			BuildRows();
			Layout();
			ShowIdle();
			Refresh();
		}
	}

	private void BuildRows()
	{
		foreach (Button row in _rows)
		{
			row.Parent?.RemoveChild(row, dispose: true);
		}
		_rows.Clear();
		foreach (TrackingRadarEntryPacket entry in _entries.Skip(_page * 4).Take(4))
		{
			bool flag = entry == _chosen;
			Button button = new Button(this, "Row", disableText: true);
			string text = (flag ? "radar_ligne_choisie.png" : "radar_ligne.png");
			button.SetStateTexture(ComponentState.Normal, text);
			button.SetStateTexture(ComponentState.Hovered, flag ? text : "radar_ligne_survol.png");
			button.SetStateTexture(ComponentState.Active, text);
			button.SetSize(316, 44);
			button.Clicked += delegate
			{
				Choose(entry);
			};
			HexPortrait hexPortrait = new HexPortrait(button, "Portrait", 18)
			{
				Accent = (entry.Online ? CriminalRed : Dim),
				SpriteTint = (entry.Online ? new Color(255, 255, 255, 255) : AbsentTint),
				Sprite = entry.Sprite,
				Face = entry.Face,
				MouseInputEnabled = false
			};
			hexPortrait.SetPosition(8, 4);
			Label label = MakeLabel(button, "Name", 12, White, _body);
			label.Text = entry.Name;
			label.SetSize(170, 22);
			label.SetPosition(58, 2);
			Label label2 = MakeLabel(button, "Level", 9, Dim, _body);
			label2.Text = Strings.TrackingRadar.Level.ToString(entry.Level);
			label2.SetSize(70, 18);
			label2.SetPosition(58, 23);
			ImagePanel imagePanel = new ImagePanel(button, "Status")
			{
				MouseInputEnabled = false,
				Texture = GameContentManager.Current.GetTexture(TextureType.Gui, "saiyan_amis_pastille.png"),
				RenderColor = (entry.Online ? Green : Dim)
			};
			imagePanel.SetSize(8, 8);
			imagePanel.SetPosition(130, 28);
			Label label3 = MakeLabel(button, "Presence", 9, entry.Online ? GreenLight : Dim, _body);
			label3.Text = (entry.Online ? Strings.TrackingRadar.Online : ((entry.OfflineMinutes >= 0) ? ((LocalizedString)Strings.TrackingRadar.OfflineSince.ToString(Elapsed(entry.OfflineMinutes))) : Strings.TrackingRadar.Offline));
			label3.SetSize(150, 18);
			label3.SetPosition(142, 23);
			Label label4 = MakeLabel(button, "Bounty", 13, Gold, _strong);
			label4.Text = SaiyanHud.Amount(entry.Bounty) + " Z";
			label4.TextAlign = Pos.Right | Pos.CenterV;
			label4.SetSize(110, 44);
			label4.SetPosition(194, 0);
			_rows.Add(button);
		}
	}

	private static string Elapsed(int minutes)
	{
		if (minutes >= 60)
		{
			if (minutes < 2880)
			{
				return $"{minutes / 60} h";
			}
			return $"{minutes / 1440} j";
		}
		return $"{minutes} min";
	}

	private int FrameY(int windowY)
	{
		return windowY - _laidOutTitleBarHeight;
	}

	private void Layout()
	{
		_laidOutTitleBarHeight = base.TitleBarBounds.Height;
		_title.SetSize(520, 34);
		_title.SetPosition(24, FrameY(9));
		_close.SetSize(24, 24);
		_close.SetPosition(576, FrameY(14));
		_stock.SetSize(360, 20);
		_stock.SetPosition(24, FrameY(48));
		_scope.SetSize(220, 220);
		_scope.SetPosition(30, FrameY(90));
		_jamBand.SetSize(190, 38);
		_jamBand.SetPosition(45, FrameY(181));
		_jamText.SetSize(190, 38);
		_jamText.SetPosition(45, FrameY(181));
		_line1.SetSize(252, 20);
		_line1.SetPosition(14, FrameY(316));
		_line2.SetSize(252, 18);
		_line2.SetPosition(14, FrameY(336));
		_caption.SetSize(220, 20);
		_caption.SetPosition(280, FrameY(88));
		_pageUp.SetPosition(546, FrameY(88));
		_pageDown.SetPosition(572, FrameY(88));
		_empty.SetSize(316, 44);
		_empty.SetPosition(280, FrameY(112));
		for (int i = 0; i < _rows.Count; i++)
		{
			_rows[i].SetPosition(280, FrameY(112 + i * 48));
		}
		_locate.SetPosition(352, FrameY(316));
	}

	private static Label MakeLabel(Intersect.Client.Framework.Gwen.Control.Base parent, string name, int size, Color color, IFont? font)
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

	private Button MakePageButton(string name, string key)
	{
		Button button = new Button(this, name, disableText: true);
		button.SetStateTexture(ComponentState.Normal, key + ".png");
		button.SetStateTexture(ComponentState.Hovered, key + "_survol.png");
		button.SetStateTexture(ComponentState.Active, key + "_survol.png");
		button.SetStateTexture(ComponentState.Disabled, key + ".png");
		button.SetSize(24, 20);
		return button;
	}
}
