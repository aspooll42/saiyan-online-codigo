using System;
using System.Collections.Generic;
using System.Linq;
using Intersect.Client.Entities;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Gwen;
using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.Framework.Gwen.Input;
using Intersect.Client.Framework.Gwen.Renderer;
using Intersect.Client.Framework.Gwen.Skin;
using Intersect.Client.Framework.Input;
using Intersect.Client.General;
using Intersect.Client.Interface.Game.Clan;
using Intersect.Client.Interface.Game.EntityPanel;
using Intersect.Client.Interface.Game.Friends;
using Intersect.Client.Interface.Game.Groupe;
using Intersect.Client.Interface.Game.Inventaire;
using Intersect.Client.Interface.Game.Inventory;
using Intersect.Client.Interface.Game.Personnage;
using Intersect.Client.Interface.Game.Quetes;
using Intersect.Client.Interface.Game.Techniques;
using Intersect.Client.Localization;
using Intersect.Client.Networking;
using Intersect.Framework.Core;
using Intersect.GameObjects;

namespace Intersect.Client.Interface.Game;

public class MenuContainer : Panel
{
	private readonly MenuDockCase _inventoryCase;

	private readonly InventoryWindow _inventoryWindow;

	private readonly PersonnageScreen _personnageScreen;

	private readonly FenetreInventaire _fenetreInventaire;

	private readonly MenuDockCase _spellsCase;

	private readonly TechniquesWindow _spellsWindow;

	private readonly FenetreTechniques _fenetreTechniques;

	private readonly MenuDockCase _questsCase;

	private readonly FenetreJournal _questsWindow;

	private readonly MenuDockCase _mapCase;

	private readonly MenuDockCase _progressionCase;

	private readonly MenuDockCase _teachingsCase;

	private readonly MenuDockCase _menuCase;

	private readonly ProgressionMenu _progressionMenu;

	private readonly SocialMenu _socialMenu;

	private readonly HashSet<MenuBadge> _pending = new HashSet<MenuBadge>();

	private readonly long _createdAt = Timing.Global.Milliseconds;

	private readonly MenuDockCase _socialCase;

	private readonly FenetreAmis _friendsWindow;

	private readonly FenetreGroupe _partyWindow;

	private readonly FenetreClan _guildWindow;

	private const int SideMargin = 5;

	private const int TopMargin = 5;

	private const int BottomMargin = 5;

	private const int Gap = 5;

	private const int RuleBlock = 12;

	private const int LabelSize = 13;

	private const float CombatAlpha = 0.55f;

	private const int WakeMargin = 40;

	private static readonly Color PlateInk = new Color(255, 8, 11, 19);

	private static readonly Color PlateEdge = new Color(255, 44, 57, 86);

	private static readonly Color PlateInnerEdge = new Color(255, 19, 25, 40);

	private static readonly Color PlateTopEdge = new Color(255, 62, 78, 112);

	private static readonly Color RuleInk = new Color(255, 74, 90, 124);

	private static readonly Color OrangeCore = new Color(255, 255, 172, 82);

	private static readonly Color Orange = new Color(255, 255, 122, 26);

	private static readonly Color LabelInk = new Color(255, 238, 242, 250);

	private static readonly Color ChipInk = new Color(242, 7, 10, 18);

	private static readonly Color ChipEdge = new Color(255, 51, 65, 95);

	private readonly List<MenuDockCase> _cases = new List<MenuDockCase>();

	private readonly List<int> _rules = new List<int>();

	private readonly Dictionary<MenuDockCase, Control> _shortcuts = new Dictionary<MenuDockCase, Control>();

	private float _alpha = 1f;

	private long _lastFrame;

	private long _keysReadAt;

	public MenuContainer(Canvas gameCanvas)
		: base(gameCanvas, "MenuContainer")
	{
		base.Alignment = new Alignments[2]
		{
			Alignments.Bottom,
			Alignments.Right
		};
		base.AlignmentPadding = new Padding
		{
			Bottom = 10,
			Right = 4
		};
		base.Padding = new Padding(0);
		base.RestrictToParent = true;
		base.ShouldCacheToTexture = false;
		_inventoryCase = Case("character", Strings.GameMenu.Character, Control.OpenInventory, ToggleCharacterCase);
		_spellsCase = Case("spell", Strings.GameMenu.Spells, Control.OpenSpells, ToggleSpellsWindow);
		_questsCase = Case("quests", Strings.GameMenu.Quest, Control.OpenQuests, ToggleQuestsWindow);
		_mapCase = Case("map", Strings.WorldMap.Title, Control.OpenWorldMap, delegate
		{
			Interface.GameUi?.ToggleWorldMapWindow();
		});
		_progressionCase = Case("progression", Strings.GameMenu.Progression, Control.OpenAchievements, delegate
		{
			_progressionMenu?.ToggleHidden(_progressionCase);
		});
		_teachingsCase = Case("teachings", Strings.MenuWheel.Teachings, Control.OpenTeachings, delegate
		{
			Interface.GameUi?.ToggleTeachingsScreen();
		});
		_socialCase = Case("social", Strings.GameMenu.Social, Control.OpenFriends, delegate
		{
			_socialMenu?.ToggleHidden(_socialCase);
		});
		_menuCase = Case("menu", Strings.EscapeMenu.Title, Control.OpenMenu, delegate
		{
			Interface.GameUi?.EscapeMenu.ToggleHidden();
		});
		LayoutDock(new global::_003C_003Ez__ReadOnlyArray<MenuDockCase[]>(new MenuDockCase[4][]
		{
			new MenuDockCase[2] { _inventoryCase, _spellsCase },
			new MenuDockCase[4] { _questsCase, _mapCase, _progressionCase, _teachingsCase },
			new MenuDockCase[1] { _socialCase },
			new MenuDockCase[1] { _menuCase }
		}));
		_partyWindow = new FenetreGroupe(gameCanvas);
		_friendsWindow = new FenetreAmis(gameCanvas);
		_inventoryWindow = new InventoryWindow(gameCanvas);
		_personnageScreen = new PersonnageScreen(gameCanvas);
		_fenetreInventaire = new FenetreInventaire(gameCanvas);
		_spellsWindow = new TechniquesWindow(gameCanvas);
		_fenetreTechniques = new FenetreTechniques(gameCanvas, _spellsWindow);
		_questsWindow = new FenetreJournal(gameCanvas);
		_guildWindow = new FenetreClan(gameCanvas);
		_progressionMenu = new ProgressionMenu(gameCanvas);
		_socialMenu = new SocialMenu(gameCanvas);
	}

	public void Update(bool updateQuestLog)
	{
		_inventoryWindow.Update();
		_personnageScreen.Update();
		if (_fenetreInventaire.IsVisible() && !Globals.CanCloseInventory)
		{
			_fenetreInventaire.Hide();
			if (Globals.InBag)
			{
				OpenInventory();
			}
		}
		_fenetreInventaire.Update();
		_spellsWindow.Update();
		_fenetreTechniques.Update();
		_partyWindow.Update();
		_friendsWindow.Update();
		_questsWindow.Update(updateQuestLog);
		_guildWindow.Update();
		_inventoryCase.IsOpen = _inventoryWindow.IsVisibleInTree || _personnageScreen.IsVisible() || _fenetreInventaire.IsVisible();
		_spellsCase.IsOpen = _spellsWindow.IsVisibleInTree || _fenetreTechniques.IsVisible();
		_questsCase.IsOpen = _questsWindow.IsVisible();
		_mapCase.IsOpen = Interface.GameUi?.IsWorldMapOpen ?? false;
		_progressionCase.IsOpen = Interface.GameUi?.IsProgressionOpen ?? false;
		_teachingsCase.IsOpen = Interface.GameUi?.IsTeachingsOpen ?? false;
		_socialCase.IsOpen = _friendsWindow.IsVisible() || _partyWindow.IsVisible() || !_guildWindow.IsHidden;
		_menuCase.IsOpen = Interface.GameUi?.EscapeMenu.IsVisibleInTree ?? false;
		ClearWhenOpen(MenuBadge.Quests, _questsWindow.IsVisible());
		ClearWhenOpen(MenuBadge.Friends, _friendsWindow.IsVisible());
		ClearWhenOpen(MenuBadge.Party, _partyWindow.IsVisible());
		ClearWhenOpen(MenuBadge.Guild, !_guildWindow.IsHidden);
		ClearWhenOpen(MenuBadge.Progression, Interface.GameUi?.IsProgressionOpen ?? false);
		ClearWhenOpen(MenuBadge.Character, (_personnageScreen.IsVisible() && _personnageScreen.OngletActif == PersonnageScreen.Onglet.Caracteristiques) || (_fenetreInventaire.IsVisible() && _fenetreInventaire.OngletActif == FenetreInventaire.Onglet.Caracteristiques));
		UpdateBadges();
		RefreshKeys();
		AdvanceDock();
	}

	private void UpdateBadges()
	{
		bool flag = _pending.Contains(MenuBadge.Friends) || _pending.Contains(MenuBadge.Party) || _pending.Contains(MenuBadge.Guild);
		_socialCase.Badge = (flag ? (-1) : 0);
		_socialCase.BadgeIsGood = false;
		_questsCase.Badge = (_pending.Contains(MenuBadge.Quests) ? (-1) : 0);
		_questsCase.BadgeIsGood = false;
		_progressionCase.Badge = (_pending.Contains(MenuBadge.Progression) ? (-1) : 0);
		_progressionCase.BadgeIsGood = true;
		int num = Globals.Me?.StatPoints ?? 0;
		_inventoryCase.Badge = ((num > 0) ? num : (_pending.Contains(MenuBadge.Character) ? (-1) : 0));
		_inventoryCase.BadgeIsGood = num > 0;
		Player me = Globals.Me;
		int num2 = ((me != null) ? (Teachings.PointsAt(me.Level) - Teachings.Spent(me.TeachingRanks)) : 0);
		_teachingsCase.Badge = ((num2 > 0) ? num2 : (_pending.Contains(MenuBadge.Teachings) ? (-1) : 0));
		_teachingsCase.BadgeIsGood = num2 > 0;
	}

	public void Notify(MenuBadge badge)
	{
		if (Timing.Global.Milliseconds - _createdAt >= 5000)
		{
			_pending.Add(badge);
		}
	}

	public bool HasNews(MenuBadge badge)
	{
		return _pending.Contains(badge);
	}

	private void ClearWhenOpen(MenuBadge badge, bool open)
	{
		if (open)
		{
			_pending.Remove(badge);
		}
	}

	public void UpdateFriendsList()
	{
		_friendsWindow.UpdateList();
	}

	public void UpdateGuildList()
	{
		_guildWindow.UpdateList();
	}

	public void HideWindows()
	{
		if (Globals.Database.HideOthersOnWindowOpen)
		{
			_friendsWindow.Hide();
			_inventoryWindow.Hide();
			_personnageScreen.Hide();
			_fenetreInventaire.Hide();
			_partyWindow.Hide();
			_questsWindow.Hide();
			_spellsWindow.Hide();
			_fenetreTechniques.Hide();
			_guildWindow.Hide();
		}
	}

	public void ToggleCharacterWindow()
	{
		BasculerPersonnage(FenetreInventaire.Onglet.Caracteristiques);
	}

	private void BasculerPersonnage(FenetreInventaire.Onglet onglet)
	{
		if (Globals.CanCloseInventory)
		{
			if (!_fenetreInventaire.IsVisible())
			{
				HideWindows();
				_inventoryWindow.Hide();
				_personnageScreen.Hide();
			}
			_fenetreInventaire.Basculer(onglet);
		}
	}

	private void ToggleCharacterCase()
	{
		if (_fenetreInventaire.IsVisible())
		{
			_fenetreInventaire.Hide();
		}
		else if (!Globals.CanCloseInventory)
		{
			ToggleInventoryWindow();
		}
		else
		{
			BasculerPersonnage(_fenetreInventaire.OngletActif);
		}
	}

	public bool ToggleFriendsWindow()
	{
		if (_friendsWindow.IsVisible())
		{
			_friendsWindow.Hide();
		}
		else
		{
			HideWindows();
			PacketSender.SendRequestFriends();
			_friendsWindow.UpdateList();
			_friendsWindow.Show();
		}
		return _friendsWindow.IsVisible();
	}

	public bool ToggleGuildWindow()
	{
		if (!_guildWindow.IsHidden)
		{
			_guildWindow.Hide();
		}
		else
		{
			HideWindows();
			PacketSender.SendRequestGuild();
			Player? me = Globals.Me;
			if (me == null || !me.IsInGuild)
			{
				PacketSender.SendGuildDirectory(string.Empty, 0);
			}
			_guildWindow.Open();
		}
		return !_guildWindow.IsHidden;
	}

	public void HideGuildWindow()
	{
		_guildWindow.Hide();
	}

	public void ToggleInventoryWindow()
	{
		if (!Globals.CanCloseInventory)
		{
			if (Globals.InShop || Globals.InBank || Globals.InBag)
			{
				OpenInventory();
			}
		}
		else
		{
			BasculerPersonnage(FenetreInventaire.Onglet.Sac);
		}
	}

	public void OpenInventory()
	{
		_fenetreInventaire.Hide();
		if (!_inventoryWindow.IsVisibleInTree)
		{
			_inventoryWindow.OpenBeside();
		}
	}

	public void CloseInventory()
	{
		if (_inventoryWindow.IsVisibleInTree)
		{
			_inventoryWindow.Hide();
		}
	}

	public InventoryWindow GetInventoryWindow()
	{
		return _inventoryWindow;
	}

	public void TogglePartyWindow()
	{
		if (_partyWindow.IsVisible())
		{
			_partyWindow.Hide();
			return;
		}
		HideWindows();
		_partyWindow.Show();
	}

	public void ToggleQuestsWindow()
	{
		if (_questsWindow.IsVisible())
		{
			_questsWindow.Hide();
			return;
		}
		HideWindows();
		_questsWindow.Show();
	}

	public void OpenQuest(Guid questId)
	{
		if (!_questsWindow.IsVisible())
		{
			HideWindows();
		}
		_questsWindow.ShowQuest(questId);
	}

	public void OpenContract(string cle)
	{
		if (!_questsWindow.IsVisible())
		{
			HideWindows();
		}
		_questsWindow.ShowContract(cle);
	}

	public void ToggleSpellsWindow()
	{
		if (_fenetreTechniques.IsVisible())
		{
			_fenetreTechniques.Hide();
			return;
		}
		if (_spellsWindow.IsVisibleInTree)
		{
			_spellsWindow.Hide();
			return;
		}
		HideWindows();
		_fenetreTechniques.Show();
	}

	public void CloseAllWindows()
	{
		_friendsWindow.Hide();
		_inventoryWindow.Hide();
		_personnageScreen.Hide();
		_fenetreInventaire.Hide();
		_questsWindow.Hide();
		_spellsWindow.Hide();
		_fenetreTechniques.Hide();
		_partyWindow.Hide();
		_guildWindow.Hide();
	}

	public bool HasWindowsOpen()
	{
		return _friendsWindow.IsVisible() || _inventoryWindow.IsVisibleInTree || _personnageScreen.IsVisible() || _fenetreInventaire.IsVisible() || _questsWindow.IsVisible() || _spellsWindow.IsVisibleInTree || _fenetreTechniques.IsVisible() || _partyWindow.IsVisible() || _guildWindow.IsVisibleInTree;
	}

	private MenuDockCase Case(string icon, string label, Control control, Action action)
	{
		MenuDockCase menuDockCase = new MenuDockCase(this, "_" + icon + "Case")
		{
			Icon = icon,
			Label = label,
			Action = action
		};
		_shortcuts[menuDockCase] = control;
		menuDockCase.Key = ShortKey(control);
		return menuDockCase;
	}

	private void RefreshKeys()
	{
		long milliseconds = Timing.Global.Milliseconds;
		if (milliseconds - _keysReadAt < 500)
		{
			return;
		}
		_keysReadAt = milliseconds;
		foreach (KeyValuePair<MenuDockCase, Control> shortcut in _shortcuts)
		{
			shortcut.Deconstruct(out var key, out var value);
			MenuDockCase menuDockCase = key;
			Control control = value;
			menuDockCase.Key = ShortKey(control);
		}
	}

	private static string ShortKey(Control control)
	{
		string text = DropdownPanel.KeyOf(control);
		if (string.IsNullOrWhiteSpace(text))
		{
			return string.Empty;
		}
		text = text.Trim().ToUpperInvariant();
		if (text.Length <= 6)
		{
			return text;
		}
		return string.Empty;
	}

	private void LayoutDock(IReadOnlyList<MenuDockCase[]> familles)
	{
		_cases.Clear();
		_rules.Clear();
		int num = 5;
		for (int i = 0; i < familles.Count; i++)
		{
			if (i > 0)
			{
				_rules.Add(num + 6);
				num += 12;
			}
			MenuDockCase[] array = familles[i];
			foreach (MenuDockCase menuDockCase in array)
			{
				menuDockCase.SetPosition(num, 5);
				_cases.Add(menuDockCase);
				num += 55;
			}
			num -= 5;
		}
		SetSize(num + 5, 69);
	}

	private void AdvanceDock()
	{
		long milliseconds = Timing.Global.Milliseconds;
		float num = ((_lastFrame == 0L) ? 0f : Math.Min(0.1f, (float)(milliseconds - _lastFrame) / 1000f));
		_lastFrame = milliseconds;
		Player me = Globals.Me;
		bool flag = me != null && me.CombatTimer > milliseconds;
		Point mousePosition = InputHandler.MousePosition;
		bool flag2 = new Rectangle(base.Bounds.X - 40, base.Bounds.Y - 40, base.Bounds.Width + 80, base.Bounds.Height + 80).Contains(mousePosition.X, mousePosition.Y);
		float num2 = ((!flag || flag2) ? 1f : 0.55f);
		_alpha += (num2 - _alpha) * (1f - MathF.Exp((0f - num) * 8f));
		foreach (MenuDockCase @case in _cases)
		{
			@case.DockAlpha = _alpha;
			@case.Advance(num);
		}
	}

	protected override void Render(Intersect.Client.Framework.Gwen.Skin.Base skin)
	{
		Intersect.Client.Framework.Gwen.Renderer.Base renderer = skin.Renderer;
		DrawPlate(renderer, base.Width, base.Height, _alpha);
		foreach (int rule in _rules)
		{
			DrawRule(renderer, new Rectangle(rule, 11, 1, 49), _alpha);
		}
	}

	protected override void RenderOver(Intersect.Client.Framework.Gwen.Skin.Base skin)
	{
		base.RenderOver(skin);
		MenuDockCase menuDockCase = _cases.FirstOrDefault((MenuDockCase slot) => slot.IsHovered && !slot.IsDimmed);
		if (menuDockCase != null && !string.IsNullOrEmpty(menuDockCase.Label))
		{
			Intersect.Client.Framework.Gwen.Renderer.Base renderer = skin.Renderer;
			string text = menuDockCase.Label.ToUpperInvariant();
			int num = SaiyanHud.SpacedWidth(renderer, SaiyanHud.StrongFont, 13, text, 1) + 20;
			int num2 = 24;
			int num3 = menuDockCase.X + 25;
			int num4 = Math.Clamp(num3 - num / 2, 2, Math.Max(2, base.Width - num - 2));
			int num5 = menuDockCase.PlateTop - num2 - 8;
			Rectangle rectangle = new Rectangle(num4, num5, num, num2);
			Fill(renderer, rectangle, ChipInk);
			Outline(renderer, rectangle, ChipEdge);
			Fill(renderer, new Rectangle(num4 + 1, num5 + num2 - 1, num - 2, 1), Orange);
			SaiyanHud.TextSpaced(renderer, SaiyanHud.StrongFont, 13, text, num4 + num / 2 + (num - 20) / 2, num5, num2, 1, A(LabelInk));
		}
	}

	internal static void DrawPlate(Intersect.Client.Framework.Gwen.Renderer.Base renderer, int width, int height, float alpha)
	{
		Rectangle rectangle = new Rectangle(0, 0, width, height);
		Trait(rectangle, PlateInk);
		Cadre(rectangle, PlateEdge);
		Cadre(new Rectangle(1, 1, width - 2, height - 2), PlateInnerEdge);
		Trait(new Rectangle(1, 0, width - 2, 1), PlateTopEdge);
		DrawTopHairline(renderer, width, alpha);
		void Cadre(Rectangle rectangle2, Color color)
		{
			Trait(new Rectangle(rectangle2.X, rectangle2.Y, rectangle2.Width, 1), color);
			Trait(new Rectangle(rectangle2.X, rectangle2.Bottom - 1, rectangle2.Width, 1), color);
			Trait(new Rectangle(rectangle2.X, rectangle2.Y + 1, 1, rectangle2.Height - 2), color);
			Trait(new Rectangle(rectangle2.Right - 1, rectangle2.Y + 1, 1, rectangle2.Height - 2), color);
		}
		void Trait(Rectangle rect, Color color)
		{
			renderer.DrawColor = SaiyanHud.WithAlpha(color, alpha);
			renderer.DrawFilledRect(rect);
		}
	}

	internal static void DrawRule(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Rectangle rule, float alpha)
	{
		renderer.DrawColor = SaiyanHud.WithAlpha(RuleInk, alpha);
		renderer.DrawFilledRect(rule);
	}

	private static void DrawTopHairline(Intersect.Client.Framework.Gwen.Renderer.Base renderer, int width, float alpha)
	{
		int num = width - 20;
		if (num > 96)
		{
			Trait(new Rectangle(34, 0, num - 48, 2), Orange);
			Trait(new Rectangle(58, 0, num - 96, 1), OrangeCore);
			for (int i = 0; i < 3; i++)
			{
				int num2 = 8;
				Color color = SaiyanHud.WithAlpha(Orange, (float)(i + 1) / 4f);
				Trait(new Rectangle(10 + i * num2, 0, num2, 2), color);
				Trait(new Rectangle(10 + num - (i + 1) * num2, 0, num2, 2), color);
			}
		}
		void Trait(Rectangle rectangle, Color color2)
		{
			renderer.DrawColor = SaiyanHud.WithAlpha(color2, alpha);
			renderer.DrawFilledRect(rectangle);
		}
	}

	private Color A(Color color)
	{
		return SaiyanHud.WithAlpha(color, _alpha);
	}

	private void Fill(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Rectangle rectangle, Color color)
	{
		renderer.DrawColor = A(color);
		renderer.DrawFilledRect(rectangle);
	}

	private void Outline(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Rectangle rectangle, Color color)
	{
		renderer.DrawColor = A(color);
		renderer.DrawFilledRect(new Rectangle(rectangle.X, rectangle.Y, rectangle.Width, 1));
		renderer.DrawFilledRect(new Rectangle(rectangle.X, rectangle.Bottom - 1, rectangle.Width, 1));
		renderer.DrawFilledRect(new Rectangle(rectangle.X, rectangle.Y + 1, 1, rectangle.Height - 2));
		renderer.DrawFilledRect(new Rectangle(rectangle.Right - 1, rectangle.Y + 1, 1, rectangle.Height - 2));
	}
}
