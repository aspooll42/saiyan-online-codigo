using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Intersect.Admin.Actions;
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
using Intersect.Client.Spells;
using Intersect.Framework.Core.GameObjects.Events;
using Intersect.Framework.Core.GameObjects.Items;
using Intersect.Framework.Core.GameObjects.Maps.MapList;
using Intersect.Framework.Core.GameObjects.Ornaments;
using Intersect.Framework.Core.GameObjects.PlayerClass;
using Intersect.Framework.Core.GameObjects.Titles;
using Intersect.GameObjects;
using Intersect.Localization;
using Intersect.Models;

namespace Intersect.Client.Interface.Game.Admin;

public class AdminWindow : WindowControl
{
	private sealed class Field
	{
		public required ImagePanel Back { get; init; }

		public required TextBox Box { get; init; }

		public required Intersect.Client.Framework.Gwen.Control.Label Hint { get; init; }
	}

	private sealed class Toggle
	{
		public required ImagePanel Back { get; init; }

		public required ImagePanel Pill { get; init; }

		public required Button Left { get; init; }

		public required Button Right { get; init; }

		public bool LeftActive { get; set; } = true;
	}

	private const int WindowWidth = 1000;

	private const int WindowHeight = 640;

	private const int ItemRowHeight = 35;

	private const int MapRowHeight = 29;

	private const int MaxItemResults = 60;

	private static readonly Color White = new Color(255, 242, 242, 242);

	private static readonly Color Muted = new Color(255, 176, 184, 200);

	private static readonly Color Dim = new Color(255, 110, 120, 145);

	private static readonly Color Night = new Color(255, 11, 14, 23);

	private static readonly Color Orange = new Color(255, 255, 122, 26);

	private static readonly Color OrangeLight = new Color(255, 255, 172, 82);

	private static readonly Color Gold = new Color(255, 245, 190, 90);

	private static readonly Color GoldLight = new Color(255, 255, 226, 150);

	private static readonly Color GreenLight = new Color(255, 140, 235, 170);

	private static readonly Color RedLight = new Color(255, 255, 120, 110);

	private static readonly Color Rule = new Color(160, 62, 74, 104);

	private readonly IFont? _font;

	private readonly IFont? _lightFont;

	private readonly IFont? _strongFont;

	private readonly IFont? _nameFont;

	private readonly Intersect.Client.Framework.Gwen.Control.Label _title;

	private readonly ImagePanel _accessChip;

	private readonly Intersect.Client.Framework.Gwen.Control.Label _accessChipText;

	private readonly Button _close;

	private readonly Field _nameField;

	private readonly HexPortrait _playerPortrait;

	private readonly Intersect.Client.Framework.Gwen.Control.Label _playerName;

	private readonly ImagePanel _playerLevel;

	private readonly Intersect.Client.Framework.Gwen.Control.Label _playerLevelText;

	private readonly Intersect.Client.Framework.Gwen.Control.Label _playerInfo;

	private string _shownPlayer = "?";

	private Guid _lookSyncedFor;

	private readonly Field _itemField;

	private readonly ScrollControl _itemList;

	private readonly List<(ImagePanel Row, Guid ItemId)> _itemRows = new List<(ImagePanel, Guid)>();

	private readonly TextBoxNumeric _quantity;

	private Guid _selectedItemId;

	private readonly Toggle _giveMode;

	private readonly Base[] _quantityControls;

	private readonly Button _takeSpell;

	private readonly List<(ImagePanel Chip, Intersect.Client.Framework.Gwen.Control.Label Folder, Guid SpellId)> _spellMarks = new List<(ImagePanel, Intersect.Client.Framework.Gwen.Control.Label, Guid)>();

	private Guid _selectedSpellId;

	private readonly Toggle _zeniMode;

	private readonly TextBoxNumeric _zeniAmount;

	private readonly Toggle _karmaMode;

	private readonly TextBoxNumeric _karmaAmount;

	private readonly Toggle _levelMode;

	private readonly TextBoxNumeric _levelAmount;

	private readonly ComboBox _titleDropdown;

	private readonly ComboBox _ornamentDropdown;

	private readonly HexPortrait _spritePreview;

	private readonly ComboBox _spriteDropdown;

	private readonly ComboBox _classDropdown;

	private readonly ImagePanel _facePreview;

	private readonly ComboBox _faceDropdown;

	private readonly ComboBox _accessDropdown;

	private readonly Field _mapField;

	private readonly Toggle _mapMode;

	private readonly ScrollControl _mapList;

	private readonly List<(ImagePanel Row, Guid MapId)> _mapRows = new List<(ImagePanel, Guid)>();

	private readonly HashSet<Guid> _collapsedFolders = new HashSet<Guid>();

	private readonly Button _warpButton;

	private Guid _selectedMapId;

	private string _selectedMapName = string.Empty;

	private BanMuteBox? _banOrMuteWindow;

	private int _laidOutTitleBarHeight = -1;

	private readonly Dictionary<Base, (int X, int Y)> _placed = new Dictionary<Base, (int, int)>();

	private readonly HashSet<string> _spriteNames;

	private static readonly HashSet<string> SheetWords = new HashSet<string>((from name in Enum.GetNames<SpriteAnimations>()
		select name.ToLowerInvariant()).Concat(new global::_003C_003Ez__ReadOnlyArray<string>(new string[11]
	{
		"attack", "ssj", "ssg", "ssb", "ui", "mui", "kaioken", "god", "rose", "blue",
		"vol"
	})), StringComparer.OrdinalIgnoreCase);

	private bool GivesSpells => !_giveMode.LeftActive;

	public string? PlayerName
	{
		get
		{
			return _nameField.Box.Text?.Trim();
		}
		set
		{
			_nameField.Box.SetText(value ?? string.Empty);
			_shownPlayer = "?";
		}
	}

	public AdminWindow(Base gameCanvas)
		: base(gameCanvas, string.Empty, modal: false, "AdminWindow")
	{
		GameContentManager current = GameContentManager.Current;
		_font = current.GetFont("chakrapetch");
		_lightFont = current.GetFont("chakrapetchmedium") ?? _font;
		_strongFont = current.GetFont("rajdhanibold") ?? _font;
		_nameFont = current.GetFont("sairanom") ?? _font;
		DisableResizing();
		SetSize(1000, 640);
		IGameTexture texture = current.GetTexture(TextureType.Gui, "saiyan_admin_fenetre.png");
		SetImage(texture, "saiyan_admin_fenetre.png", ControlState.Active);
		SetImage(texture, "saiyan_admin_fenetre.png", ControlState.Inactive);
		base.IsClosable = false;
		if (base.Titlebar is Titlebar titlebar)
		{
			titlebar.Label.IsHidden = true;
		}
		_laidOutTitleBarHeight = base.TitleBarBounds.Height;
		_title = MakeLabel(this, "Title", 20, White, current.GetFont("refuel"));
		_title.Text = Strings.AdminWindow.Title.ToString().ToUpperInvariant();
		_title.SetSize(320, 40);
		Place(_title, 24, 12);
		WindowTitle.Apply(_title);
		Player? me = Globals.Me;
		LocalizedString localizedString = ((me != null && me.AccessLevel == Access.Moderator) ? Strings.AdminWindow.ChipModerator : Strings.AdminWindow.ChipAdmin);
		int num = MeasureWidth(current.GetFont("sairatitre") ?? _font, 22, _title.Text);
		(ImagePanel, Intersect.Client.Framework.Gwen.Control.Label) tuple = MakeChip(this, "AccessChip", localizedString, Gold, 24 + num + 18, 24);
		_accessChip = tuple.Item1;
		_accessChipText = tuple.Item2;
		Intersect.Client.Framework.Gwen.Control.Label label = MakeLabel(this, "Hint", 10, Dim, _lightFont);
		label.Text = Strings.AdminWindow.Hint;
		label.TextAlign = Pos.Right | Pos.CenterV;
		label.SetSize(480, 30);
		Place(label, 460, 18);
		_close = new Button(this, "Close", disableText: true);
		_close.SetStateTexture(ComponentState.Normal, "saiyan_close.png");
		_close.SetStateTexture(ComponentState.Hovered, "saiyan_close_hover.png");
		_close.SetStateTexture(ComponentState.Active, "saiyan_close_clicked.png");
		_close.SetSize(24, 24);
		Place(_close, 960, 18);
		_close.Clicked += delegate
		{
			Hide();
		};
		Section(Strings.AdminWindow.SectionPlayer, 20, 76, 320);
		_nameField = MakeField("Name", 34, 108, 292, 38, Strings.AdminWindow.NamePlaceholder, search: true);
		_nameField.Box.MaximumLength = 32;
		_nameField.Box.TextChanged += delegate
		{
			_shownPlayer = "?";
		};
		Button button = MakeButton(this, "Me", "saiyan_admin_bouton_142x30", 142, 30, 34, 154, Strings.AdminWindow.Me, Muted, White);
		button.Clicked += delegate
		{
			PlayerName = Globals.Me?.Name;
		};
		Button button2 = MakeButton(this, "MyTarget", "saiyan_admin_bouton_142x30", 142, 30, 184, 154, Strings.AdminWindow.MyTarget, Muted, White);
		button2.Clicked += delegate
		{
			Player me2 = Globals.Me;
			if (me2 != null)
			{
				Guid targetId = me2.TargetId;
				if (targetId != Guid.Empty && Globals.Entities.TryGetValue(targetId, out Entity value) && value is Player)
				{
					PlayerName = value.Name;
				}
			}
		};
		_playerPortrait = new HexPortrait(this, "PlayerPortrait", 18)
		{
			Accent = Orange
		};
		Place(_playerPortrait, 38, 208);
		_playerName = MakeLabel(this, "PlayerName", 14, White, _nameFont);
		Place(_playerName, 88, 199);
		tuple = MakeChip(this, "PlayerLevel", string.Empty, Gold, 200, 204);
		_playerLevel = tuple.Item1;
		_playerLevelText = tuple.Item2;
		_playerInfo = MakeLabel(this, "PlayerInfo", 10, Muted, _lightFont);
		_playerInfo.SetSize(240, 22);
		Place(_playerInfo, 88, 226);
		Section(Strings.AdminWindow.SectionActions, 20, 284, 320);
		MakeAction("WarpTo", "aller", Strings.AdminWindow.ActionWarpTo, danger: false, Muted, 34, 314, delegate(string name)
		{
			PacketSender.SendAdminAction(new WarpMeToAction(name));
		});
		MakeAction("Summon", "venir", Strings.AdminWindow.ActionSummon, danger: false, Muted, 184, 314, delegate(string name)
		{
			PacketSender.SendAdminAction(new WarpToMeAction(name));
		});
		MakeAction("Overworld", "monde", Strings.AdminWindow.ActionOverworld, danger: false, Muted, 34, 366, delegate(string name)
		{
			PacketSender.SendAdminAction(new ReturnToOverworldAction(name));
		});
		MakeAction("Unmute", "parole", Strings.AdminWindow.ActionUnmute, danger: false, GreenLight, 184, 366, AskToUnmute);
		MakeAction("Unban", "debannir", Strings.AdminWindow.ActionUnban, danger: false, GreenLight, 34, 418, AskToUnban);
		MakeAction("Mute", "muet", Strings.AdminWindow.ActionMute, danger: true, RedLight, 184, 418, AskToMute);
		MakeAction("Kick", "expulser", Strings.AdminWindow.ActionKick, danger: true, RedLight, 34, 470, AskToKick);
		MakeAction("Kill", "tuer", Strings.AdminWindow.ActionKill, danger: true, RedLight, 184, 470, AskToKill);
		MakeAction("Ban", "bannir", Strings.AdminWindow.ActionBan, danger: true, RedLight, 34, 522, AskToBan);
		Intersect.Client.Framework.Gwen.Control.Label label2 = MakeLabel(this, "DangerHint", 10, Dim, _lightFont);
		label2.Text = Strings.AdminWindow.DangerHint;
		label2.SetSize(292, 24);
		Place(label2, 34, 584);
		Section(Strings.AdminWindow.SectionGiveShort, 356, 76, 152);
		_giveMode = MakeToggle("GiveMode", 502, 80, Strings.AdminWindow.Objects, Strings.AdminWindow.Techniques, ChangeGiveMode, 170, 26);
		_itemField = MakeField("ItemSearch", 370, 112, 302, 38, Strings.AdminWindow.ItemSearch, search: true);
		_itemField.Box.TextChanged += delegate
		{
			BuildItemList();
		};
		ImagePanel imagePanel = MakeNinePatch(this, "ItemListBack", "saiyan_admin_liste.png", 6, 302, 106);
		Place(imagePanel, 370, 156);
		_itemList = new ScrollControl(imagePanel, "ItemList");
		StyleScroll(_itemList);
		_itemList.SetSize(296, 98);
		_itemList.SetPosition(3, 4);
		Button button3 = MakeButton(this, "QuantityMinus", "saiyan_admin_bouton_38x38", 38, 38, 370, 274, "-", Muted, White);
		ImagePanel imagePanel2 = MakeNinePatch(this, "QuantityBack", "saiyan_admin_champ.png", 6, 84, 38);
		Place(imagePanel2, 414, 274);
		_quantity = new TextBoxNumeric(imagePanel2, "Quantity")
		{
			Font = _strongFont,
			FontSize = 14,
			ShouldDrawBackground = false,
			TextAlign = Pos.Center,
			Minimum = 1.0,
			Maximum = 10000.0,
			Value = 1.0
		};
		_quantity.SetTextColor(White, ComponentState.Normal);
		_quantity.SetSize(76, 32);
		_quantity.SetPosition(4, 3);
		Button button4 = MakeButton(this, "QuantityPlus", "saiyan_admin_bouton_38x38", 38, 38, 504, 274, "+", Muted, White);
		button3.Clicked += delegate
		{
			_quantity.SetValue(Math.Max(1.0, _quantity.Value - 1.0));
		};
		button4.Clicked += delegate
		{
			_quantity.SetValue(Math.Min(10000.0, _quantity.Value + 1.0));
		};
		_quantityControls = new Base[3] { button3, imagePanel2, button4 };
		_takeSpell = MakeButton(this, "TakeSpell", "saiyan_admin_bouton_172x38", 172, 38, 370, 274, Strings.AdminWindow.TakeButton, Muted, White, _strongFont, 13);
		_takeSpell.Clicked += delegate
		{
			SendSpell(remove: true);
		};
		_takeSpell.IsHidden = true;
		Button button5 = MakeButton(this, "Give", "saiyan_admin_cta_120x38", 120, 38, 552, 274, Strings.AdminWindow.GiveButton, Night, Night, _strongFont, 13);
		button5.Clicked += delegate
		{
			GiveSelected();
		};
		Section(Strings.AdminWindow.SectionCurrency, 356, 326, 330);
		(_zeniMode, _zeniAmount) = MakeCurrencyRow("Zeni", Strings.AdminWindow.Zeni, 356, -1000000000.0, 1000000000.0, SendZeni);
		(_karmaMode, _karmaAmount) = MakeCurrencyRow("Karma", Strings.AdminWindow.Karma, 402, -2000.0, 2000.0, SendKarma);
		(_levelMode, _levelAmount) = MakeCurrencyRow("Level", Strings.AdminWindow.LevelLabel, 448, -1000.0, 1000.0, SendLevel);
		Section(Strings.AdminWindow.SectionRewards, 356, 492, 330);
		_titleDropdown = MakeDropdown("Titles", 370, 524, 164);
		foreach (TitleDescriptor item in DatabaseObject<TitleDescriptor>.Lookup.Values.OfType<TitleDescriptor>().OrderBy<TitleDescriptor, string>((TitleDescriptor title) => title.Name, StringComparer.CurrentCultureIgnoreCase))
		{
			_titleDropdown.AddItem(item.Name, null, item.Id);
		}
		MakeButton(this, "TitleGive", "saiyan_admin_accent_64x36", 64, 36, 540, 524, Strings.AdminWindow.Give, OrangeLight, GoldLight).Clicked += delegate
		{
			SendTitle(remove: false);
		};
		MakeButton(this, "TitleTake", "saiyan_admin_bouton_62x36", 62, 36, 610, 524, Strings.AdminWindow.TakeTitle, Muted, White).Clicked += delegate
		{
			SendTitle(remove: true);
		};
		_ornamentDropdown = MakeDropdown("Ornaments", 370, 570, 164);
		foreach (OrnamentDescriptor item2 in DatabaseObject<OrnamentDescriptor>.Lookup.Values.OfType<OrnamentDescriptor>().OrderBy<OrnamentDescriptor, string>((OrnamentDescriptor ornament) => ornament.Name, StringComparer.CurrentCultureIgnoreCase))
		{
			_ornamentDropdown.AddItem(item2.Name, null, item2.Id);
		}
		MakeButton(this, "OrnamentGive", "saiyan_admin_accent_64x36", 64, 36, 540, 570, Strings.AdminWindow.Give, OrangeLight, GoldLight).Clicked += delegate
		{
			SendOrnament(remove: false);
		};
		MakeButton(this, "OrnamentTake", "saiyan_admin_bouton_62x36", 62, 36, 610, 570, Strings.AdminWindow.TakeOrnament, Muted, White).Clicked += delegate
		{
			SendOrnament(remove: true);
		};
		Section(Strings.AdminWindow.SectionLook, 702, 76, 278);
		_spritePreview = new HexPortrait(this, "SpritePreview", 18)
		{
			Accent = new Color(255, 62, 74, 104)
		};
		Place(_spritePreview, 716, 104);
		_spriteDropdown = MakeDropdown("Sprites", 764, 104, 124);
		string[] textureNames = current.GetTextureNames(TextureType.Entity);
		_spriteNames = textureNames.Select((string sprite) => Path.GetFileNameWithoutExtension(sprite)).ToHashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (string item3 in textureNames.Where((string sprite) => SheetBase(Path.GetFileNameWithoutExtension(sprite)) == null).OrderBy<string, string>((string name) => name, StringComparer.CurrentCultureIgnoreCase))
		{
			_spriteDropdown.AddItem(Path.GetFileNameWithoutExtension(item3), null, item3);
		}
		_spriteDropdown.ItemSelected += delegate
		{
			_spritePreview.Sprite = _spriteDropdown.SelectedItem?.UserData as string;
		};
		_spritePreview.Sprite = _spriteDropdown.SelectedItem?.UserData as string;
		MakeButton(this, "SpriteApply", "saiyan_admin_accent_72x36", 72, 36, 894, 104, Strings.AdminWindow.Apply, OrangeLight, GoldLight).Clicked += delegate
		{
			WithPlayer(delegate(string name)
			{
				PacketSender.SendAdminAction(new SetSpriteAction(name, _spriteDropdown.SelectedItem?.UserData as string));
			});
		};
		ImagePanel imagePanel3 = MakeImage(this, "FaceBack", "saiyan_admin_vignette.png", 36, 36);
		Place(imagePanel3, 718, 150);
		_facePreview = MakeImage(imagePanel3, "FacePreview", null, 32, 32);
		_facePreview.SetPosition(2, 2);
		_faceDropdown = MakeDropdown("Faces", 764, 150, 124);
		_faceDropdown.AddItem(Strings.General.None, null, string.Empty);
		foreach (string item4 in current.GetTextureNames(TextureType.Face).OrderBy<string, string>((string name) => name, StringComparer.CurrentCultureIgnoreCase))
		{
			_faceDropdown.AddItem(Path.GetFileNameWithoutExtension(item4), null, item4);
		}
		_faceDropdown.ItemSelected += delegate
		{
			string text2 = _faceDropdown.SelectedItem?.UserData as string;
			_facePreview.Texture = (string.IsNullOrEmpty(text2) ? null : GameContentManager.Current.GetTexture(TextureType.Face, text2));
		};
		MakeButton(this, "FaceApply", "saiyan_admin_accent_72x36", 72, 36, 894, 150, Strings.AdminWindow.Apply, OrangeLight, GoldLight).Clicked += delegate
		{
			WithPlayer(delegate(string name)
			{
				string text2 = _faceDropdown.SelectedItem?.UserData as string;
				PacketSender.SendAdminAction(new SetFaceAction(name, string.IsNullOrEmpty(text2) ? null : text2));
			});
		};
		Intersect.Client.Framework.Gwen.Control.Label label3 = MakeLabel(this, "ClassLabel", 9, Dim, _font);
		label3.Text = Strings.AdminWindow.ClassLabel.ToString().ToUpperInvariant();
		label3.SetSize(46, 36);
		Place(label3, 716, 196);
		_classDropdown = MakeDropdown("Classes", 764, 196, 124);
		foreach (var (guid2, label4) in DatabaseObject<ClassDescriptor>.ItemPairs.OrderBy<KeyValuePair<Guid, string>, string>((KeyValuePair<Guid, string> pair) => pair.Value, StringComparer.CurrentCultureIgnoreCase))
		{
			_classDropdown.AddItem(label4, null, guid2);
		}
		MakeButton(this, "ClassApply", "saiyan_admin_accent_72x36", 72, 36, 894, 196, Strings.AdminWindow.Apply, OrangeLight, GoldLight).Clicked += delegate
		{
			WithPlayer(delegate(string name)
			{
				if (_classDropdown.SelectedItem?.UserData is Guid guid3 && guid3 != Guid.Empty)
				{
					PacketSender.SendAdminAction(new SetClassAction(name, guid3));
				}
			});
		};
		Intersect.Client.Framework.Gwen.Control.Label label5 = MakeLabel(this, "AccessLabel", 9, Dim, _font);
		label5.Text = Strings.AdminWindow.Access.ToString().ToUpperInvariant();
		label5.SetSize(46, 36);
		Place(label5, 716, 242);
		_accessDropdown = MakeDropdown("Access", 764, 242, 124);
		_accessDropdown.AddItem(Strings.General.None, null, "None");
		_accessDropdown.AddItem(Strings.AdminWindow.Access1, null, "Moderator");
		_accessDropdown.AddItem(Strings.AdminWindow.Access2, null, "Admin");
		MakeButton(this, "AccessSet", "saiyan_admin_accent_72x36", 72, 36, 894, 242, Strings.AdminWindow.SetPowerShort, OrangeLight, GoldLight).Clicked += delegate
		{
			WithPlayer(delegate(string name)
			{
				if (_accessDropdown.SelectedItem?.UserData is string { Length: >0 } text2)
				{
					PacketSender.SendAdminAction(new SetAccessAction(name, text2));
				}
			});
		};
		Section(Strings.AdminWindow.SectionWarp, 702, 302, 278);
		_mapField = MakeField("MapSearch", 716, 334, 250, 36, Strings.AdminWindow.MapSearch, search: true);
		_mapField.Box.TextChanged += delegate
		{
			BuildMapList();
		};
		_mapMode = MakeToggle("MapMode", 716, 378, Strings.AdminWindow.Folders, Strings.AdminWindow.AToZ, BuildMapList);
		ImagePanel imagePanel4 = MakeNinePatch(this, "MapListBack", "saiyan_admin_liste.png", 6, 250, 150);
		Place(imagePanel4, 716, 418);
		_mapList = new ScrollControl(imagePanel4, "MapList");
		StyleScroll(_mapList);
		_mapList.SetSize(244, 142);
		_mapList.SetPosition(3, 4);
		_warpButton = MakeButton(this, "Warp", "saiyan_admin_cta_250x34", 250, 34, 716, 576, Strings.AdminWindow.ChooseMap, Night, Night, _strongFont, 12);
		_warpButton.Clicked += delegate
		{
			Warp();
		};
		SaiyanDropdowns.Apply(this);
		BuildItemList();
		BuildMapList();
		SetPosition((gameCanvas.Width - 1000) / 2, Math.Max(0, (gameCanvas.Height - 640) / 2));
	}

	public override void Hide()
	{
		_nameField?.Box.Blur();
		_itemField?.Box.Blur();
		_mapField?.Box.Blur();
		base.Hide();
	}

	public override void Think()
	{
		base.Think();
		if (base.TitleBarBounds.Height != _laidOutTitleBarHeight)
		{
			_laidOutTitleBarHeight = base.TitleBarBounds.Height;
			foreach (KeyValuePair<Base, (int, int)> item in _placed)
			{
				item.Deconstruct(out var key, out var value);
				(int, int) tuple = value;
				Base obj = key;
				var (x, num) = tuple;
				obj.SetPosition(x, num - _laidOutTitleBarHeight);
			}
		}
		Field[] array = new Field[3] { _nameField, _itemField, _mapField };
		foreach (Field field in array)
		{
			bool flag = InputHandler.KeyboardFocus == field.Box;
			field.Hint.IsHidden = !string.IsNullOrEmpty(field.Box.Text);
			string text = (flag ? "saiyan_admin_champ_focus.png" : "saiyan_admin_champ.png");
			if (field.Back.TextureFilename != text)
			{
				field.Back.TextureFilename = text;
			}
		}
		RefreshPlayerCard();
		RefreshKnownSpells();
		Guid selected = (GivesSpells ? _selectedSpellId : _selectedItemId);
		RefreshHover(_itemRows.Select<(ImagePanel, Guid), (ImagePanel, bool)>(((ImagePanel Row, Guid ItemId) row) => (Row: row.Row, row.ItemId == selected)), _itemList);
		RefreshHover(_mapRows.Select<(ImagePanel, Guid), (ImagePanel, bool)>(((ImagePanel Row, Guid MapId) row) => (Row: row.Row, row.MapId != Guid.Empty && row.MapId == _selectedMapId)), _mapList);
	}

	public void UpdateMapList()
	{
		BuildMapList();
	}

	private void RefreshPlayerCard()
	{
		string name = PlayerName ?? string.Empty;
		Entity entity = ((name.Length == 0) ? null : Globals.Entities.Values.FirstOrDefault((Entity candidate) => candidate is Player && string.Equals(candidate.Name, name, StringComparison.OrdinalIgnoreCase)));
		string text = entity?.MapInstance?.Name ?? string.Empty;
		int num;
		if (entity != null)
		{
			Player me = Globals.Me;
			if (me != null && entity.MapId == me.MapId)
			{
				num = Math.Max(Math.Abs(entity.X - me.X), Math.Abs(entity.Y - me.Y));
				goto IL_00b9;
			}
		}
		num = -1;
		goto IL_00b9;
		IL_00b9:
		int num2 = num;
		string text2 = $"{name}|{entity?.Id}|{entity?.Level}|{text}|{num2}";
		if (text2 == _shownPlayer)
		{
			return;
		}
		_shownPlayer = text2;
		_playerPortrait.Entity = entity;
		if (entity != null && entity.Id != _lookSyncedFor)
		{
			_lookSyncedFor = entity.Id;
			if (!string.IsNullOrEmpty(entity.Sprite) && SelectCharacter(entity.Sprite))
			{
				_spritePreview.Sprite = _spriteDropdown.SelectedItem?.UserData as string;
			}
			if (entity is Player player)
			{
				_classDropdown.SelectByUserData(player.Class);
			}
			if (_faceDropdown.SelectByUserData(entity.Face?.ToLowerInvariant() ?? string.Empty))
			{
				_facePreview.Texture = (string.IsNullOrEmpty(entity.Face) ? null : GameContentManager.Current.GetTexture(TextureType.Face, entity.Face));
			}
		}
		_playerPortrait.Accent = ((entity != null) ? Orange : new Color(255, 70, 78, 96));
		if (name.Length == 0)
		{
			_playerName.Text = Strings.AdminWindow.NoPlayer;
			_playerName.SetTextColor(Dim, ComponentState.Normal);
			_playerName.SetSize(MeasureWidth(_nameFont, 14, _playerName.Text) + 8, 28);
			_playerLevel.IsHidden = true;
			_playerInfo.Text = string.Empty;
			return;
		}
		_playerName.Text = entity?.Name ?? name;
		_playerName.SetTextColor((entity != null) ? White : Muted, ComponentState.Normal);
		int num3 = MeasureWidth(_nameFont, 14, _playerName.Text);
		_playerName.SetSize(num3 + 8, 28);
		if (entity != null)
		{
			string text3 = Strings.Friends.Level.ToString(entity.Level);
			int width = MeasureWidth(_font, 8, text3) + 14;
			_playerLevel.IsHidden = false;
			_playerLevel.SetSize(width, 18);
			Place(_playerLevel, 88 + num3 + 10, 204);
			_playerLevelText.Text = text3;
			_playerLevelText.SetSize(width, 18);
			_playerInfo.Text = ((num2 > 0) ? Strings.AdminWindow.PlayerPlaceDistance.ToString(text, num2) : text);
		}
		else
		{
			_playerLevel.IsHidden = true;
			_playerInfo.Text = Strings.AdminWindow.NotOnScreen;
		}
	}

	private void WithPlayer(Action<string> action)
	{
		string playerName = PlayerName;
		if (playerName != null && playerName.Length > 0)
		{
			action(playerName);
		}
	}

	private bool IsMe(string name)
	{
		return string.Equals(name, Globals.Me?.Name, StringComparison.CurrentCultureIgnoreCase);
	}

	private void AskToKill(string name)
	{
		ConfirmCardWindow.Open(new ConfirmCardRequest
		{
			Title = Strings.AdminWindow.ActionKill,
			Heading = name,
			Prompt = Strings.AdminWindow.KillPrompt,
			ConfirmLabel = Strings.AdminWindow.ActionKill,
			Danger = true,
			PlayerName = name
		}, delegate
		{
			PacketSender.SendAdminAction(new KillAction(name));
		});
	}

	private void AskToKick(string name)
	{
		ConfirmCardWindow.Open(new ConfirmCardRequest
		{
			Title = Strings.AdminWindow.ActionKick,
			Heading = name,
			Prompt = Strings.AdminWindow.KickPrompt,
			ConfirmLabel = Strings.AdminWindow.ActionKick,
			Danger = true,
			PlayerName = name
		}, delegate
		{
			PacketSender.SendAdminAction(new KickAction(name));
		});
	}

	private void AskToUnban(string name)
	{
		ConfirmCardRequest confirmCardRequest = new ConfirmCardRequest()
		{
			Title = Strings.AdminWindow.ActionUnban,
			Heading = name,
			Prompt = Strings.AdminWindow.UnbanPrompt.ToString(name),
			ConfirmLabel = Strings.AdminWindow.ActionUnban,
			PlayerName = name,
		};
		ConfirmCardWindow.Open(confirmCardRequest, delegate
		{
			PacketSender.SendAdminAction(new UnbanAction(name));
		});
	}

	private void AskToUnmute(string name)
	{
		ConfirmCardRequest confirmCardRequest = new ConfirmCardRequest()
		{
			Title = Strings.AdminWindow.ActionUnmute,
			Heading = name,
			Prompt = Strings.AdminWindow.UnmutePrompt.ToString(name),
			ConfirmLabel = Strings.AdminWindow.ActionUnmute,
			PlayerName = name,
		};
		ConfirmCardWindow.Open(confirmCardRequest, delegate
		{
			PacketSender.SendAdminAction(new UnmuteAction(name));
		});
	}

	private void AskToBan(string name)
	{
		if (!IsMe(name))
		{
			_banOrMuteWindow = new BanMuteBox(Strings.AdminWindow.BanCaption.ToString(name), Strings.AdminWindow.BanPrompt.ToString(name), delegate
			{
				PacketSender.SendAdminAction(new BanAction(name, _banOrMuteWindow?.GetDuration() ?? 0, _banOrMuteWindow?.GetReason() ?? string.Empty, _banOrMuteWindow?.BanIp() ?? false));
				_banOrMuteWindow?.Dispose();
			});
		}
	}

	private void AskToMute(string name)
	{
		if (!IsMe(name))
		{
			_banOrMuteWindow = new BanMuteBox(Strings.AdminWindow.MuteCaption.ToString(name), Strings.AdminWindow.MutePrompt.ToString(name), delegate
			{
				PacketSender.SendAdminAction(new MuteAction(name, _banOrMuteWindow?.GetDuration() ?? 0, _banOrMuteWindow?.GetReason() ?? string.Empty, _banOrMuteWindow?.BanIp() ?? false));
				_banOrMuteWindow?.Dispose();
			});
		}
	}

	private void BuildItemList()
	{
		foreach (var itemRow in _itemRows)
		{
			ImagePanel item = itemRow.Row;
			item.Parent?.RemoveChild(item, dispose: true);
		}
		_itemRows.Clear();
		_spellMarks.Clear();
		string query = _itemField.Box.Text?.Trim() ?? string.Empty;
		if (GivesSpells)
		{
			BuildSpellList(query);
			return;
		}
		List<ItemDescriptor> list = (from itemDescriptor in DatabaseObject<ItemDescriptor>.Lookup.Values.OfType<ItemDescriptor>()
			where query.Length == 0 || itemDescriptor.Name.Contains(query, StringComparison.CurrentCultureIgnoreCase)
			select itemDescriptor).OrderBy<ItemDescriptor, string>((ItemDescriptor itemDescriptor) => itemDescriptor.Name, StringComparer.CurrentCultureIgnoreCase).ToList();
		if (list.Count == 1)
		{
			_selectedItemId = list[0].Id;
		}
		int num = 0;
		foreach (ItemDescriptor item2 in list.Take(60))
		{
			ImagePanel imagePanel = MakeNinePatch(_itemList, $"Item_{item2.Id}", null, 4, 280, 34);
			imagePanel.MouseInputEnabled = true;
			imagePanel.SetPosition(2, num);
			Guid itemId = item2.Id;
			imagePanel.Clicked += delegate
			{
				_selectedItemId = itemId;
			};
			ImagePanel imagePanel2 = MakeImage(imagePanel, "Icon", null, 28, 28);
			imagePanel2.Texture = GameContentManager.Current.GetTexture(TextureType.Item, item2.Icon);
			imagePanel2.SetPosition(8, 3);
			Intersect.Client.Framework.Gwen.Control.Label label = MakeLabel(imagePanel, "Name", 10, White, _lightFont);
			label.Text = item2.Name;
			label.SetSize(228, 34);
			label.SetPosition(44, 0);
			_itemRows.Add((imagePanel, itemId));
			num += 35;
		}
		if (list.Count == 0)
		{
			_itemRows.Add((MakeListNote(_itemList, "NoItem", Strings.AdminWindow.NoItemFound, 280, 90, 0), Guid.Empty));
		}
		else if (list.Count > 60)
		{
			string text = Strings.AdminWindow.MoreItems.ToString(list.Count - 60);
			_itemRows.Add((MakeListNote(_itemList, "More", text, 280, 26, num), Guid.Empty));
		}
	}

	private void BuildSpellList(string query)
	{
		List<SpellDescriptor> source = DatabaseObject<SpellDescriptor>.Lookup.Values.OfType<SpellDescriptor>().ToList();
		HashSet<string> hashSet = (from @group in source.GroupBy<SpellDescriptor, string>((SpellDescriptor spell) => spell.Name, StringComparer.CurrentCultureIgnoreCase)
			where @group.Count() > 1
			select @group.Key).ToHashSet<string>(StringComparer.CurrentCultureIgnoreCase);
		List<SpellDescriptor> list = source.Where((SpellDescriptor spell) => query.Length == 0 || spell.Name.Contains(query, StringComparison.CurrentCultureIgnoreCase) || (spell.Folder ?? string.Empty).Contains(query, StringComparison.CurrentCultureIgnoreCase)).OrderBy<SpellDescriptor, string>((SpellDescriptor spell) => spell.Name, StringComparer.CurrentCultureIgnoreCase).ThenBy<SpellDescriptor, string>((SpellDescriptor spell) => spell.Folder ?? string.Empty, StringComparer.CurrentCultureIgnoreCase)
			.ToList();
		if (list.Count == 1)
		{
			_selectedSpellId = list[0].Id;
		}
		int num = 0;
		foreach (SpellDescriptor item2 in list.Take(60))
		{
			ImagePanel imagePanel = MakeNinePatch(_itemList, $"Spell_{item2.Id}", null, 4, 280, 34);
			imagePanel.MouseInputEnabled = true;
			imagePanel.SetPosition(2, num);
			Guid spellId = item2.Id;
			imagePanel.Clicked += delegate
			{
				_selectedSpellId = spellId;
			};
			ImagePanel imagePanel2 = MakeImage(imagePanel, "Icon", null, 28, 28);
			imagePanel2.Texture = GameContentManager.Current.GetTexture(TextureType.Spell, item2.Icon);
			imagePanel2.SetPosition(8, 3);
			ImagePanel item = MakeChip(imagePanel, "Known", Strings.AdminWindow.Known, GreenLight, 0, 8).Chip;
			item.SetPosition(272 - item.Width, 8);
			item.IsHidden = true;
			string text = (hashSet.Contains(item2.Name) ? (item2.Folder ?? string.Empty) : string.Empty);
			int num2 = ((text.Length != 0) ? Math.Min(90, MeasureWidth(_lightFont, 9, text) + 4) : 0);
			Intersect.Client.Framework.Gwen.Control.Label label = MakeLabel(imagePanel, "Folder", 9, Dim, _lightFont);
			label.Text = text;
			label.TextAlign = Pos.Right | Pos.CenterV;
			label.SetSize(num2, 34);
			label.SetPosition(272 - num2, 0);
			Intersect.Client.Framework.Gwen.Control.Label label2 = MakeLabel(imagePanel, "Name", 10, White, _lightFont);
			label2.Text = item2.Name;
			label2.SetSize(228 - Math.Max(num2, item.Width) - 6, 34);
			label2.SetPosition(44, 0);
			_itemRows.Add((imagePanel, spellId));
			_spellMarks.Add((item, label, spellId));
			num += 35;
		}
		if (list.Count == 0)
		{
			_itemRows.Add((MakeListNote(_itemList, "NoSpell", Strings.AdminWindow.NoSpellFound, 280, 90, 0), Guid.Empty));
		}
		else if (list.Count > 60)
		{
			string text2 = Strings.AdminWindow.MoreItems.ToString(list.Count - 60);
			_itemRows.Add((MakeListNote(_itemList, "More", text2, 280, 26, num), Guid.Empty));
		}
	}

	private void RefreshKnownSpells()
	{
		if (_spellMarks.Count == 0)
		{
			return;
		}
		Player me = Globals.Me;
		int num;
		if (me != null)
		{
			string playerName = PlayerName;
			if (playerName != null && playerName.Length > 0)
			{
				num = (IsMe(playerName) ? 1 : 0);
				goto IL_0034;
			}
		}
		num = 0;
		goto IL_0034;
		IL_0034:
		bool flag = (byte)num != 0;
		foreach (var spellMark in _spellMarks)
		{
			ImagePanel item = spellMark.Chip;
			Intersect.Client.Framework.Gwen.Control.Label item2 = spellMark.Folder;
			Guid spellId = spellMark.SpellId;
			bool flag2 = flag && me.Spells.Any((Spell spell) => spell != null && spell.Id == spellId);
			if (item.IsHidden == flag2)
			{
				item.IsHidden = !flag2;
				item2.IsHidden = flag2;
			}
		}
	}

	private void ChangeGiveMode()
	{
		_itemField.Hint.Text = (GivesSpells ? Strings.AdminWindow.SpellSearch : Strings.AdminWindow.ItemSearch);
		Base[] quantityControls = _quantityControls;
		foreach (Base obj in quantityControls)
		{
			obj.IsHidden = GivesSpells;
		}
		_takeSpell.IsHidden = !GivesSpells;
		BuildItemList();
	}

	private ImagePanel MakeListNote(ScrollControl list, string name, string text, int width, int height, int y)
	{
		ImagePanel imagePanel = MakeImage(list, name, null, width, height);
		imagePanel.SetPosition(2, y);
		Intersect.Client.Framework.Gwen.Control.Label label = MakeLabel(imagePanel, "Text", 10, Dim, _lightFont);
		label.Text = text;
		label.TextAlign = Pos.Center;
		label.SetSize(width, height);
		return imagePanel;
	}

	private void GiveItem()
	{
		if (!(_selectedItemId == Guid.Empty))
		{
			int quantity = (int)Math.Max(1.0, _quantity.Value);
			WithPlayer(delegate(string name)
			{
				PacketSender.SendAdminAction(new GiveItemAction(name, _selectedItemId, quantity));
			});
		}
	}

	private void GiveSelected()
	{
		if (GivesSpells)
		{
			SendSpell(remove: false);
		}
		else
		{
			GiveItem();
		}
	}

	private void SendSpell(bool remove)
	{
		if (_selectedSpellId != Guid.Empty)
		{
			Guid spellId = _selectedSpellId;
			WithPlayer(delegate(string name)
			{
				PacketSender.SendAdminAction(new GiveSpellAction(name, spellId, remove));
			});
		}
	}

	private (Toggle Mode, TextBoxNumeric Amount) MakeCurrencyRow(string name, string caption, int y, double minimum, double maximum, Action<string> send)
	{
		Intersect.Client.Framework.Gwen.Control.Label label = MakeLabel(this, name + "Label", 10, Muted, _lightFont);
		label.Text = caption;
		label.SetSize(52, 36);
		Place(label, 370, y);
		Toggle item = MakeToggle(name + "Mode", 420, y + 2, Strings.AdminWindow.Add, Strings.AdminWindow.Set, null);
		ImagePanel imagePanel = MakeNinePatch(this, name + "AmountBack", "saiyan_admin_champ.png", 6, 70, 36);
		Place(imagePanel, 556, y);
		TextBoxNumeric textBoxNumeric = new TextBoxNumeric(imagePanel, name + "Amount")
		{
			Font = _strongFont,
			FontSize = 13,
			ShouldDrawBackground = false,
			TextAlign = Pos.Center,
			Minimum = minimum,
			Maximum = maximum,
			Value = 0.0
		};
		textBoxNumeric.SetTextColor(White, ComponentState.Normal);
		textBoxNumeric.SetSize(62, 30);
		textBoxNumeric.SetPosition(4, 3);
		MakeButton(this, name + "Ok", "saiyan_admin_accent_40x36", 40, 36, 632, y, Strings.AdminWindow.Ok, OrangeLight, GoldLight).Clicked += delegate
		{
			WithPlayer(send);
		};
		return (Mode: item, Amount: textBoxNumeric);
	}

	private void SendZeni(string name)
	{
		PacketSender.SendAdminAction(new SetZeniAction(name, (long)_zeniAmount.Value, !_zeniMode.LeftActive));
	}

	private void SendKarma(string name)
	{
		PacketSender.SendAdminAction(new SetKarmaAction(name, (int)_karmaAmount.Value, !_karmaMode.LeftActive));
	}

	private void SendLevel(string name)
	{
		PacketSender.SendAdminAction(new SetLevelAction(name, (int)_levelAmount.Value, !_levelMode.LeftActive));
	}

	private void SendTitle(bool remove)
	{
		object obj = _titleDropdown.SelectedItem?.UserData;
		if (!(obj is Guid))
		{
			return;
		}
		Guid titleId = (Guid)obj;
		if (titleId != Guid.Empty)
		{
			WithPlayer(delegate(string name)
			{
				PacketSender.SendAdminAction(new GiveTitleAction(name, titleId, remove));
			});
		}
	}

	private void SendOrnament(bool remove)
	{
		object obj = _ornamentDropdown.SelectedItem?.UserData;
		if (!(obj is Guid))
		{
			return;
		}
		Guid ornamentId = (Guid)obj;
		if (ornamentId != Guid.Empty)
		{
			WithPlayer(delegate(string name)
			{
				PacketSender.SendAdminAction(new GiveOrnamentAction(name, ornamentId, remove));
			});
		}
	}

	private void BuildMapList()
	{
		foreach (var mapRow in _mapRows)
		{
			ImagePanel item = mapRow.Row;
			item.Parent?.RemoveChild(item, dispose: true);
		}
		_mapRows.Clear();
		string query = _mapField.Box.Text?.Trim() ?? string.Empty;
		int y = 0;
		if (query.Length > 0 || !_mapMode.LeftActive)
		{
			foreach (MapListMap item2 in MapList.OrderedMaps.Where((MapListMap map) => query.Length == 0 || map.Name.Contains(query, StringComparison.CurrentCultureIgnoreCase)).OrderBy<MapListMap, string>((MapListMap map) => map.Name, StringComparer.CurrentCultureIgnoreCase))
			{
				y = AddMapRow(item2, 0, y);
			}
		}
		else
		{
			y = AddFolder(MapList.List, 0, y);
		}
		if (_mapRows.Count == 0)
		{
			_mapRows.Add((MakeListNote(_mapList, "NoMap", Strings.AdminWindow.NoMapFound, 228, 120, 0), Guid.Empty));
		}
		RefreshWarpButton();
	}

	private int AddFolder(MapList list, int depth, int y)
	{
		foreach (MapListItem item in list.Items)
		{
			if (!(item is MapListFolder mapListFolder))
			{
				if (item is MapListMap map)
				{
					y = AddMapRow(map, depth, y);
				}
				continue;
			}
			bool flag = _collapsedFolders.Contains(mapListFolder.FolderId);
			ImagePanel imagePanel = MakeNinePatch(_mapList, $"Folder_{mapListFolder.FolderId}", null, 4, 228, 28);
			imagePanel.MouseInputEnabled = true;
			imagePanel.SetPosition(2, y);
			Guid folderId = mapListFolder.FolderId;
			imagePanel.Clicked += delegate
			{
				if (!_collapsedFolders.Remove(folderId))
				{
					_collapsedFolders.Add(folderId);
				}
				BuildMapList();
			};
			ImagePanel imagePanel2 = MakeImage(imagePanel, "Chevron", flag ? "saiyan_admin_chevron_droite.png" : "saiyan_admin_chevron_bas.png", 10, 10);
			imagePanel2.RenderColor = Muted;
			imagePanel2.SetPosition(8 + depth * 12, 9);
			Intersect.Client.Framework.Gwen.Control.Label label = MakeLabel(imagePanel, "Name", 10, Muted, _font);
			label.Text = mapListFolder.Name;
			label.SetSize(200 - depth * 12, 28);
			label.SetPosition(22 + depth * 12, 0);
			_mapRows.Add((imagePanel, Guid.Empty));
			y += 29;
			if (!flag)
			{
				y = AddFolder(mapListFolder.Children, depth + 1, y);
			}
		}
		return y;
	}

	private int AddMapRow(MapListMap map, int depth, int y)
	{
		ImagePanel imagePanel = MakeNinePatch(_mapList, $"Map_{map.MapId}", null, 4, 228, 28);
		imagePanel.MouseInputEnabled = true;
		imagePanel.SetPosition(2, y);
		Guid mapId = map.MapId;
		string mapName = map.Name;
		imagePanel.Clicked += delegate
		{
			_selectedMapId = mapId;
			_selectedMapName = mapName;
			RefreshWarpButton();
		};
		imagePanel.DoubleClicked += delegate
		{
			_selectedMapId = mapId;
			_selectedMapName = mapName;
			Warp();
		};
		Player? me = Globals.Me;
		bool flag = me != null && me.MapId == mapId;
		Intersect.Client.Framework.Gwen.Control.Label label = MakeLabel(imagePanel, "Name", 10, White, _lightFont);
		label.Text = mapName;
		label.SetSize(flag ? (150 - depth * 12) : (196 - depth * 12), 28);
		label.SetPosition(20 + depth * 12, 0);
		if (flag)
		{
			MakeChip(imagePanel, "Here", Strings.AdminWindow.Here, GreenLight, 180, 5);
		}
		_mapRows.Add((imagePanel, mapId));
		return y + 29;
	}

	private void RefreshWarpButton()
	{
		_warpButton.Text = ((_selectedMapId == Guid.Empty) ? ((string)Strings.AdminWindow.ChooseMap) : Strings.AdminWindow.GoTo.ToString(_selectedMapName));
	}

	private void Warp()
	{
		if (!(_selectedMapId == Guid.Empty) && !(Globals.Me?.MapId == _selectedMapId))
		{
			PacketSender.SendAdminAction(new WarpToMapAction(_selectedMapId));
		}
	}

	private void Place(Base control, int x, int y)
	{
		_placed[control] = (x, y);
		control.SetPosition(x, y - _laidOutTitleBarHeight);
	}

	private void Section(string text, int x, int y, int width)
	{
		Intersect.Client.Framework.Gwen.Control.Label label = MakeLabel(this, $"Section{x}_{y}", 10, Muted, _font);
		label.Text = text;
		int num = MeasureWidth(_font, 9, text);
		label.SetSize(num + 6, 20);
		Place(label, x + 14, y + 8);
		int num2 = x + 14 + num + 12;
		ImagePanel imagePanel = MakeImage(this, $"SectionRule{x}_{y}", "saiyan_amis_filet.png", Math.Max(1, x + width - 14 - num2), 1);
		imagePanel.RenderColor = Rule;
		Place(imagePanel, num2, y + 18);
	}

	private Field MakeField(string name, int x, int y, int width, int height, string hintText, bool search)
	{
		ImagePanel imagePanel = MakeNinePatch(this, name + "Back", "saiyan_admin_champ.png", 6, width, height);
		imagePanel.MouseInputEnabled = true;
		Place(imagePanel, x, y);
		int num = 12;
		if (search)
		{
			ImagePanel imagePanel2 = MakeImage(imagePanel, name + "Loupe", "inv_loupe.png", 14, 14);
			imagePanel2.RenderColor = Muted;
			imagePanel2.SetPosition(12, (height - 14) / 2);
			num = 34;
		}
		Intersect.Client.Framework.Gwen.Control.Label label = MakeLabel(imagePanel, name + "Hint", 11, Dim, _lightFont);
		label.Text = hintText;
		label.SetSize(width - num - 8, height);
		label.SetPosition(num, 0);
		TextBox textBox = new TextBox(imagePanel, name)
		{
			Font = _lightFont,
			FontSize = 11,
			ShouldDrawBackground = false,
			MaximumLength = 40
		};
		textBox.SetTextColor(White, ComponentState.Normal);
		textBox.SetSize(width - num - 8, height - 6);
		textBox.SetPosition(num, 3);
		return new Field
		{
			Back = imagePanel,
			Box = textBox,
			Hint = label
		};
	}

	private Toggle MakeToggle(string name, int x, int y, string left, string right, Action? changed, int width = 130, int height = 32)
	{
		ImagePanel back = MakeNinePatch(this, name + "Back", "saiyan_admin_bascule.png", 5, width, height);
		back.MouseInputEnabled = true;
		Place(back, x, y);
		int half = width / 2;
		ImagePanel imagePanel = MakeNinePatch(back, name + "Pill", "saiyan_admin_bascule_actif.png", 5, half - 3, height - 6);
		imagePanel.SetPosition(3, 3);
		Toggle toggle = new Toggle
		{
			Back = back,
			Pill = imagePanel,
			Left = Half(name + "Left", left, 0),
			Right = Half(name + "Right", right, half)
		};
		toggle.Left.Clicked += delegate
		{
			toggle.LeftActive = true;
			Apply();
			changed?.Invoke();
		};
		toggle.Right.Clicked += delegate
		{
			toggle.LeftActive = false;
			Apply();
			changed?.Invoke();
		};
		Apply();
		return toggle;
		void Apply()
		{
			toggle.Pill.SetPosition(toggle.LeftActive ? 3 : half, 3);
			toggle.Left.SetTextColor(toggle.LeftActive ? GoldLight : Dim, ComponentState.Normal);
			toggle.Left.SetTextColor(toggle.LeftActive ? GoldLight : White, ComponentState.Hovered);
			toggle.Right.SetTextColor(toggle.LeftActive ? Dim : GoldLight, ComponentState.Normal);
			toggle.Right.SetTextColor(toggle.LeftActive ? White : GoldLight, ComponentState.Hovered);
		}
		Button Half(string halfName, string text, int halfX)
		{
			Button button = new Button(back, halfName)
			{
				Font = _font,
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
			button.SetSize(half, height);
			button.SetPosition(halfX, 0);
			return button;
		}
	}

	private void MakeAction(string name, string icon, string text, bool danger, Color tint, int x, int y, Action<string> action)
	{
		string text2 = (danger ? "saiyan_admin_danger" : "saiyan_admin_action");
		Button button = new Button(this, name, disableText: true);
		button.SetStateTexture(ComponentState.Normal, text2 + ".png");
		button.SetStateTexture(ComponentState.Hovered, text2 + "_hover.png");
		button.SetStateTexture(ComponentState.Active, text2 + "_clicked.png");
		button.SetSize(142, 44);
		Place(button, x, y);
		button.Clicked += delegate
		{
			WithPlayer(action);
		};
		ImagePanel imagePanel = MakeImage(button, "Glyph", "saiyan_admin_icone_" + icon + ".png", 22, 22);
		imagePanel.RenderColor = tint;
		imagePanel.SetPosition(11, 11);
		Intersect.Client.Framework.Gwen.Control.Label label = MakeLabel(button, "Text", 10, danger ? RedLight : White, _font);
		label.Text = text;
		label.SetSize(100, 44);
		label.SetPosition(40, 0);
	}

	private Button MakeButton(Base parent, string name, string key, int width, int height, int x, int y, string text, Color ink, Color hoverInk, IFont? font = null, int fontSize = 10)
	{
		Button button = new Button(parent, name)
		{
			Font = (font ?? _font),
			FontSize = fontSize,
			TextAlign = Pos.Center,
			UsesHeadingFace = false,
			Text = text
		};
		button.SetStateTexture(ComponentState.Normal, key + ".png");
		button.SetStateTexture(ComponentState.Hovered, key + "_hover.png");
		button.SetStateTexture(ComponentState.Active, key + "_clicked.png");
		button.SetTextColor(ink, ComponentState.Normal);
		button.SetTextColor(hoverInk, ComponentState.Hovered);
		button.SetTextColor(ink, ComponentState.Active);
		button.SetSize(width, height);
		Place(button, x, y);
		return button;
	}

	private static bool IsSheetWord(string word)
	{
		if (word.Length > 0)
		{
			return SheetWords.Contains(word.TrimEnd('0', '1', '2', '3', '4', '5', '6', '7', '8', '9'));
		}
		return false;
	}

	private string? SheetBase(string? name)
	{
		if (string.IsNullOrEmpty(name))
		{
			return null;
		}
		string[] array = name.Split('_');
		int num = array.Length - 1;
		while (num >= 1 && IsSheetWord(array[num]))
		{
			string text = string.Join('_', array, 0, num);
			if (_spriteNames.Contains(text))
			{
				return text;
			}
			num--;
		}
		return null;
	}

	private bool SelectCharacter(string sprite)
	{
		string text = Path.GetFileNameWithoutExtension(sprite);
		string extension = Path.GetExtension(sprite);
		while (true)
		{
			string text2 = SheetBase(text);
			if (text2 == null)
			{
				break;
			}
			text = text2;
		}
		return _spriteDropdown.SelectByUserData((text + extension).ToLowerInvariant());
	}

	private ComboBox MakeDropdown(string name, int x, int y, int width)
	{
		ComboBox comboBox = new ComboBox(this, name)
		{
			AutoSizeToContents = false
		};
		comboBox.SetSize(width, 36);
		Place(comboBox, x, y);
		comboBox.SetMenuMaxSize(Math.Max(width, 220), 280);
		return comboBox;
	}

	private (ImagePanel Chip, Intersect.Client.Framework.Gwen.Control.Label Text) MakeChip(Base parent, string name, string text, Color tint, int x, int y)
	{
		int width = MeasureWidth(_font, 8, text) + 14;
		ImagePanel imagePanel = MakeNinePatch(parent, name, "saiyan_amis_puce.png", 4, width, 18);
		imagePanel.RenderColor = tint;
		if (parent == this)
		{
			Place(imagePanel, x, y);
		}
		else
		{
			imagePanel.SetPosition(x, y);
		}
		Intersect.Client.Framework.Gwen.Control.Label label = MakeLabel(imagePanel, "Text", 9, tint, _font);
		label.Text = text;
		label.TextAlign = Pos.Center;
		label.SetSize(width, 18);
		return (Chip: imagePanel, Text: label);
	}

	private static void RefreshHover(IEnumerable<(ImagePanel Row, bool Selected)> rows, ScrollControl list)
	{
		Point mousePosition = InputHandler.MousePosition;
		Point point = list.ToCanvas(new Point(0, 0));
		bool flag = mousePosition.X >= point.X && mousePosition.X < point.X + list.Width && mousePosition.Y >= point.Y && mousePosition.Y < point.Y + list.Height;
		foreach (var (imagePanel, flag2) in rows)
		{
			if (imagePanel.MouseInputEnabled)
			{
				bool flag3 = false;
				if (flag)
				{
					Point point2 = imagePanel.ToCanvas(new Point(0, 0));
					flag3 = mousePosition.X >= point2.X && mousePosition.X < point2.X + imagePanel.Width && mousePosition.Y >= point2.Y && mousePosition.Y < point2.Y + imagePanel.Height;
				}
				string text = (flag2 ? "saiyan_admin_selection.png" : (flag3 ? "saiyan_admin_ligne_survol.png" : null));
				if (imagePanel.TextureFilename != text)
				{
					imagePanel.TextureFilename = text;
				}
			}
		}
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

	private static ImagePanel MakeNinePatch(Base parent, string name, string? texture, int margin, int width, int height)
	{
		ImagePanel imagePanel = MakeImage(parent, name, null, width, height);
		imagePanel.TextureNinePatchMargin = new Margin(margin);
		if (texture != null)
		{
			imagePanel.TextureFilename = texture;
		}
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
