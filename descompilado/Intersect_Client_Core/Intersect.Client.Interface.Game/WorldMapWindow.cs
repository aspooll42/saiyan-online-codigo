using System;
using System.Collections.Generic;
using System.Linq;
using Intersect.Client.Entities;
using Intersect.Client.Framework.Content;
using Intersect.Client.Framework.File_Management;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.Framework.Gwen;
using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.General;
using Intersect.Client.Interface.Shared;
using Intersect.Client.Localization;
using Intersect.Client.Maps;
using Intersect.Client.Networking;
using Intersect.Framework.Core.GameObjects.WorldMap;
using Intersect.Localization;

namespace Intersect.Client.Interface.Game;

public class WorldMapWindow : WindowControl
{
	private const string BodyFont = "chakrapetch";

	private const int TopY = 72;

	private const int BottomMargin = 16;

	private const int ListWidth = 276;

	private const int RowHeight = 40;

	private const int HintHeight = 30;

	private static readonly Color Gold = new Color(255, 245, 190, 90);

	private static readonly Color White = new Color(255, 242, 242, 242);

	private static readonly Color Entry = new Color(255, 196, 204, 220);

	private static readonly Color Muted = new Color(255, 176, 186, 206);

	private static readonly Color Dim = new Color(255, 150, 160, 182);

	private static readonly Color Night = new Color(255, 11, 14, 23);

	private static readonly Color OrangeLight = new Color(255, 255, 172, 82);

	private readonly Canvas _gameCanvas;

	private readonly IFont? _font;

	private readonly IFont? _posterFont;

	private readonly ImagePanel _veil;

	private readonly Label _title;

	private readonly Button _close;

	private readonly ImagePanel _mapCard;

	private readonly CarteMondeVue _map;

	private readonly Label _hint;

	private readonly Label _here;

	private readonly ImagePanel _listCard;

	private readonly Label _listHeading;

	private readonly Label _listCount;

	private readonly ImagePanel _listRule;

	private readonly ScrollControl _list;

	private readonly ImagePanel _footRule;

	private readonly Label _selection;

	private readonly Button _travelButton;

	private readonly List<Base> _rows = new List<Base>();

	private WorldMapDestination[] _destinations = Array.Empty<WorldMapDestination>();

	private string _selectedId = string.Empty;

	private bool _needsRebuild = true;

	private bool _retourDEnma;

	private int _laidOutTitleBarHeight = -1;

	private Point _laidOutCanvas = Point.Empty;

	public bool IsOpen => !base.IsHidden;

	public WorldMapWindow(Canvas gameCanvas)
		: base(gameCanvas, string.Empty, modal: false, "WorldMapWindow")
	{
		_gameCanvas = gameCanvas;
		_font = GameContentManager.Current.GetFont("chakrapetch");
		_posterFont = GameContentManager.Current.GetFont("refuel");
		DisableResizing();
		base.IsClosable = false;
		if (base.Titlebar is Titlebar titlebar)
		{
			titlebar.Label.IsHidden = true;
		}
		_veil = MakeImage(this, "Veil", "saiyan_voile.png", 8, 8);
		_veil.TextureNinePatchMargin = new Margin(4);
		_veil.SendToBack();
		_title = MakeLabel(this, "Title", 20, White, _posterFont);
		_title.Text = Strings.WorldMap.Title.ToString().ToUpperInvariant();
		WindowTitle.Apply(_title);
		_close = new Button(this, "Close", disableText: true);
		_close.SetStateTexture(ComponentState.Normal, "saiyan_close.png");
		_close.SetStateTexture(ComponentState.Hovered, "saiyan_close_hover.png");
		_close.SetStateTexture(ComponentState.Active, "saiyan_close_clicked.png");
		_close.Clicked += delegate
		{
			Close();
		};
		_mapCard = MakeCard(this, "MapCard");
		_map = new CarteMondeVue(_mapCard);
		_map.EpingleCliquee += delegate(string id)
		{
			SelectPlace(id, centrer: false);
		};
		_hint = MakeLabel(_mapCard, "Hint", 9, Dim, _font);
		_hint.Text = Strings.WorldMap.Hint;
		_here = MakeLabel(_mapCard, "Here", 10, Gold, _font);
		_here.TextAlign = Pos.Right | Pos.CenterV;
		_listCard = MakeCard(this, "ListCard");
		_listHeading = MakeLabel(_listCard, "ListHeading", 9, Muted, _font);
		_listHeading.Text = Strings.WorldMap.Places.ToString().ToUpperInvariant();
		_listCount = MakeLabel(_listCard, "ListCount", 9, Muted, _font);
		_listCount.TextAlign = Pos.Right | Pos.CenterV;
		_listRule = MakeImage(_listCard, "ListRule", "hdv_filet.png", 256, 1);
		_list = new ScrollControl(_listCard, "PlaceList");
		StyleScroll(_list);
		_footRule = MakeImage(_listCard, "FootRule", "hdv_filet.png", 256, 1);
		_selection = MakeLabel(_listCard, "Selection", 10, Gold, _font);
		_selection.TextAlign = Pos.Center;
		_travelButton = MakeButton(_listCard, "Travel", 244, 34, cta: true);
		_travelButton.Text = Strings.WorldMap.TravelButton;
		_travelButton.Clicked += delegate
		{
			Travel(_selectedId);
		};
		base.Closed += delegate
		{
			RepondreAEnma();
		};
		Layout();
		Hide();
	}

	public void SetDestinations(WorldMapDestination[] destinations)
	{
		_destinations = destinations ?? Array.Empty<WorldMapDestination>();
		_needsRebuild = true;
	}

	public void Toggle()
	{
		if (IsOpen)
		{
			Close();
			return;
		}
		Layout();
		_needsRebuild = true;
		_map.Ouvrir();
		Show();
		BringToFront();
		Focus();
	}

	public void OuvrirPourLeRetour()
	{
		_retourDEnma = true;
		_selectedId = string.Empty;
		AfficherLeMode();
		Layout();
		_needsRebuild = true;
		if (base.IsHidden)
		{
			_map.Ouvrir();
			Show();
		}
		BringToFront();
		Focus();
	}

	private void RepondreAEnma()
	{
		if (_retourDEnma)
		{
			_retourDEnma = false;
			AfficherLeMode();
			PacketSender.SendWorldMapTeleport(string.Empty);
		}
	}

	private void AfficherLeMode()
	{
		LocalizedString localizedString = (_retourDEnma ? Strings.WorldMap.ReturnTitle : Strings.WorldMap.Title);
		_title.Text = localizedString.ToString().ToUpperInvariant();
		_hint.Text = (_retourDEnma ? Strings.WorldMap.ReturnHint : Strings.WorldMap.Hint);
	}

	public static bool AuRoyaumeDesMorts()
	{
		Player me = Globals.Me;
		if (me != null && MapInstance.TryGet(me.MapId, out MapInstance instance))
		{
			return Meteo.EstAuDela(instance);
		}
		return false;
	}

	public void Update()
	{
		if (base.IsHidden)
		{
			return;
		}
		if (AuRoyaumeDesMorts() && !_retourDEnma)
		{
			Close();
			return;
		}
		if (base.TitleBarBounds.Height != _laidOutTitleBarHeight || _laidOutCanvas != new Point(_gameCanvas.Width, _gameCanvas.Height))
		{
			Layout();
			_needsRebuild = true;
		}
		if (_needsRebuild)
		{
			RebuildList();
			_needsRebuild = false;
		}
		_map.Destinations = _destinations;
		_map.Choisie = _selectedId;
		string text = Globals.Me?.MapInstance?.Name;
		string text2 = (string.IsNullOrWhiteSpace(text) ? string.Empty : Strings.WorldMap.YouAreHere.ToString(text));
		if (_here.Text != text2)
		{
			_here.Text = text2;
		}
	}

	private void RebuildList()
	{
		foreach (Base row2 in _rows)
		{
			row2.Parent?.RemoveChild(row2, dispose: true);
		}
		_rows.Clear();
		int num = _destinations.Count((WorldMapDestination destination) => destination.Unlocked);
		_listCount.Text = Strings.WorldMap.Count.ToString(num, _destinations.Length);
		List<WorldMapDestination> list = (from destination in _destinations
			orderby destination.Unlocked descending, destination.Name
			select destination).ToList();
		IGameTexture texture = GameContentManager.Current.GetTexture(TextureType.Image, "tp_worldmap.png");
		IGameTexture lit = GameContentManager.Current.GetTexture(TextureType.Gui, "hdv_ligne_survol.png");
		IGameTexture texture2 = GameContentManager.Current.GetTexture(TextureType.Gui, "saiyan_slot_selected.png");
		int num2 = 0;
		foreach (WorldMapDestination item in list)
		{
			string id = item.Id;
			bool flag = id == _selectedId;
			ImagePanel row = new ImagePanel(_list, "Place_" + id)
			{
				TextureNinePatchMargin = new Margin(flag ? 6 : 4),
				Texture = (flag ? texture2 : null)
			};
			row.SetSize(244, 40);
			row.SetPosition(10, num2);
			if (item.Unlocked && !flag)
			{
				row.HoverEnter += delegate
				{
					row.Texture = lit;
				};
				row.HoverLeave += delegate
				{
					row.Texture = null;
				};
			}
			if (item.Unlocked)
			{
				row.Clicked += delegate
				{
					SelectPlace(id);
				};
				ImagePanel imagePanel = MakeImage(row, "Pin", null, 20, 17);
				imagePanel.Texture = texture;
				imagePanel.SetPosition(10, 11);
			}
			else
			{
				Label label = MakeLabel(row, "Unknown", 11, Dim, _font);
				label.Text = "?";
				label.TextAlign = Pos.Center;
				label.SetSize(20, 40);
				label.SetPosition(10, 0);
			}
			Label label2 = MakeLabel(row, "Name", 10, item.Unlocked ? (flag ? Gold : Entry) : Dim, _font);
			label2.Text = (item.Unlocked ? item.Name : Strings.WorldMap.LockedMarker.ToString());
			label2.SetSize(198, 40);
			label2.SetPosition(38, 0);
			_rows.Add(row);
			num2 += 40;
		}
		RefreshTravelButton();
	}

	private void SelectPlace(string id)
	{
		SelectPlace(id, centrer: true);
	}

	private void SelectPlace(string id, bool centrer)
	{
		_selectedId = id;
		_needsRebuild = true;
		if (centrer)
		{
			_map.Centrer(id);
		}
	}

	private void RefreshTravelButton()
	{
		WorldMapDestination worldMapDestination = _destinations.FirstOrDefault((WorldMapDestination destination) => destination.Id == _selectedId && destination.Unlocked);
		_travelButton.IsDisabled = worldMapDestination == null;
		_selection.Text = worldMapDestination?.Name ?? Strings.WorldMap.NoSelection.ToString();
		_selection.SetTextColor((worldMapDestination == null) ? Dim : Gold, ComponentState.Normal);
	}

	private void Travel(string destinationId)
	{
		if (!string.IsNullOrEmpty(destinationId))
		{
			_retourDEnma = false;
			AfficherLeMode();
			PacketSender.SendWorldMapTeleport(destinationId);
			Close();
		}
	}

	private new int Y(int windowY)
	{
		return windowY - _laidOutTitleBarHeight;
	}

	private void Layout()
	{
		int width = _gameCanvas.Width;
		int height = _gameCanvas.Height;
		SetSize(width, height);
		SetPosition(0, 0);
		_laidOutTitleBarHeight = base.TitleBarBounds.Height;
		_laidOutCanvas = new Point(width, height);
		_veil.SetSize(width, height);
		_veil.SetPosition(0, Y(0));
		_title.SetSize(500, 38);
		_title.SetPosition(26, Y(10));
		_close.SetSize(24, 24);
		_close.SetPosition(width - 40, Y(16));
		int y = Y(72);
		int num = height - 72 - 16;
		int num2 = width - 276 - 16;
		int num3 = num2 - 32;
		_mapCard.SetSize(num3, num);
		_mapCard.SetPosition(16, y);
		int num4 = num3 - 20;
		int height2 = num - 20 - 30;
		_map.SetSize(num4, height2);
		_map.SetPosition(10, 10);
		_hint.SetSize(num4, 22);
		_hint.SetPosition(14, num - 30);
		_here.SetSize(num4 / 2, 22);
		_here.SetPosition(14 + num4 - num4 / 2 - 4, num - 30);
		_listCard.SetSize(276, num);
		_listCard.SetPosition(num2, y);
		_listHeading.SetSize(160, 20);
		_listHeading.SetPosition(14, 8);
		_listCount.SetSize(60, 20);
		_listCount.SetPosition(202, 8);
		_listRule.SetSize(256, 1);
		_listRule.SetPosition(10, 32);
		_list.SetSize(272, num - 40 - 88);
		_list.SetPosition(2, 40);
		_footRule.SetSize(256, 1);
		_footRule.SetPosition(10, num - 80);
		_selection.SetSize(244, 22);
		_selection.SetPosition(16, num - 74);
		_travelButton.SetPosition(16, num - 44);
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

	private static ImagePanel MakeCard(Base parent, string name)
	{
		return new ImagePanel(parent, name)
		{
			Texture = GameContentManager.Current.GetTexture(TextureType.Gui, "saiyan_card.png"),
			TextureNinePatchMargin = new Margin(8),
			MouseInputEnabled = false
		};
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

	private Button MakeButton(Base parent, string name, int width, int height, bool cta = false)
	{
		Button button = new Button(parent, name)
		{
			Font = _font,
			FontSize = (cta ? 11 : 10),
			TextAlign = Pos.Center
		};
		string text = (cta ? $"saiyan_cta_{width}x{height}" : $"saiyan_btn_{width}x{height}");
		button.SetStateTexture(ComponentState.Normal, text + ".png");
		button.SetStateTexture(ComponentState.Hovered, text + "_hover.png");
		button.SetStateTexture(ComponentState.Active, text + "_clicked.png");
		button.SetStateTexture(ComponentState.Disabled, text + "_disabled.png");
		button.SetTextColor(cta ? Night : Entry, ComponentState.Normal);
		button.SetTextColor(cta ? Night : OrangeLight, ComponentState.Hovered);
		button.SetTextColor(Night, ComponentState.Active);
		button.SetTextColor(Dim, ComponentState.Disabled);
		button.SetSize(width, height);
		return button;
	}
}
