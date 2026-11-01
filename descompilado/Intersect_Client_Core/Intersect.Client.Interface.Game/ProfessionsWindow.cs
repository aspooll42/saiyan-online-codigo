using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Intersect.Client.Entities;
using Intersect.Client.Framework.Content;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.Framework.Gwen;
using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.General;
using Intersect.Client.Interface.Shared;
using Intersect.Client.Localization;
using Intersect.Framework.Core.GameObjects.Crafting;
using Intersect.Framework.Core.GameObjects.Items;
using Intersect.Framework.Core.GameObjects.NPCs;
using Intersect.Framework.Core.GameObjects.Resources;
using Intersect.Models;

namespace Intersect.Client.Interface.Game;

public class ProfessionsWindow : WindowControl
{
	private const int WindowWidth = 760;

	private const int WindowHeight = 600;

	private const int SidebarX = 16;

	private const int ContentX = 216;

	private const int ContentWidth = 528;

	private const int RowX = 24;

	private const int RowWidth = 168;

	private const int TabsTop = 80;

	private const int RowTop = 118;

	private const int RowHeight = 62;

	private const int RowStep = 70;

	private const int ResourceRows = 4;

	private static readonly Color Gold = new Color(255, 245, 190, 90);

	private static readonly Color White = new Color(255, 242, 242, 242);

	private static readonly Color Entry = new Color(255, 196, 204, 220);

	private static readonly Color Muted = new Color(255, 176, 186, 206);

	private static readonly Color Dim = new Color(255, 150, 160, 182);

	private static readonly Color Green = new Color(255, 142, 214, 124);

	private static readonly Color Night = new Color(255, 11, 14, 23);

	private static readonly Color OrangeLight = new Color(255, 255, 172, 82);

	private readonly Canvas _gameCanvas;

	private readonly IFont? _font;

	private readonly IFont? _strongFont;

	private readonly IFont? _posterFont;

	private readonly Label _title;

	private readonly Button _close;

	private readonly List<Base> _built = new List<Base>();

	private int _selected;

	private string _shown = string.Empty;

	private int _laidOutTitleBarHeight = -1;

	public bool IsOpen => !base.IsHidden;

	public ProfessionsWindow(Canvas gameCanvas)
		: base(gameCanvas, string.Empty, modal: false, "ProfessionsWindow")
	{
		_gameCanvas = gameCanvas;
		_font = Globals.ContentManager.GetFont("chakrapetch");
		_strongFont = Globals.ContentManager.GetFont("rajdhanibold");
		_posterFont = Globals.ContentManager.GetFont("refuel");
		DisableResizing();
		SetSize(760, 600);
		SetPosition((_gameCanvas.Width - 760) / 2, (_gameCanvas.Height - 600) / 2);
		IGameTexture texture = Globals.ContentManager.GetTexture(TextureType.Gui, "saiyan_window_marche.png");
		SetImage(texture, "saiyan_window_marche.png", ControlState.Active);
		SetImage(texture, "saiyan_window_marche.png", ControlState.Inactive);
		base.IsClosable = false;
		if (base.Titlebar is Titlebar titlebar)
		{
			titlebar.Label.IsHidden = true;
		}
		_title = MakeLabel(this, "Title", 20, White, _posterFont);
		_title.Text = Strings.Professions.Title.ToString().ToUpperInvariant();
		WindowTitle.Apply(_title);
		_close = new Button(this, "Close", disableText: true);
		_close.SetStateTexture(ComponentState.Normal, "saiyan_close.png");
		_close.SetStateTexture(ComponentState.Hovered, "saiyan_close_hover.png");
		_close.SetStateTexture(ComponentState.Active, "saiyan_close_clicked.png");
		_close.Clicked += delegate
		{
			Close();
		};
		Layout();
		Hide();
	}

	public void Toggle()
	{
		if (IsOpen)
		{
			Close();
			return;
		}
		Layout();
		_shown = string.Empty;
		Show();
		BringToFront();
	}

	public void Update()
	{
		if (base.IsHidden)
		{
			return;
		}
		Player me = Globals.Me;
		if (me != null)
		{
			if (base.TitleBarBounds.Height != _laidOutTitleBarHeight)
			{
				Layout();
				_shown = string.Empty;
			}
			string text = $"{me.ProfessionsVersion}:{_selected}:{DatabaseObject<ResourceDescriptor>.Lookup.Count}:{DatabaseObject<NPCDescriptor>.Lookup.Count}:{DatabaseObject<CraftingRecipeDescriptor>.Lookup.Count}";
			if (text != _shown)
			{
				_shown = text;
				Rebuild(me);
			}
		}
	}

	private void Rebuild(Player me)
	{
		foreach (Base item in _built)
		{
			item.Parent?.RemoveChild(item, dispose: true);
		}
		_built.Clear();
		BuildSidebar(me);
		BuildHeader(me);
		BuildBonuses(me);
		if (Professions.IsCrafting(_selected))
		{
			BuildRecipes(me);
		}
		else if (_selected == 4)
		{
			BuildGame(me);
		}
		else
		{
			BuildResources(me);
		}
	}

	private void BuildSidebar(Player me)
	{
		bool flag = Professions.IsCrafting(_selected);
		for (int i = 0; i < 2; i++)
		{
			bool flag2 = i == 1 == flag;
			Button button = Keep(new Button(this, $"Tab{i}")
			{
				Font = _font,
				FontSize = 11,
				TextAlign = Pos.Center
			});
			button.Text = ((i == 0) ? Strings.Professions.TabGathering : Strings.Professions.TabCrafting);
			button.SetStateTexture(ComponentState.Normal, flag2 ? "saiyan_btn_84x28_clicked.png" : "saiyan_btn_84x28.png");
			button.SetStateTexture(ComponentState.Hovered, flag2 ? "saiyan_btn_84x28_clicked.png" : "saiyan_btn_84x28_hover.png");
			button.SetStateTexture(ComponentState.Active, "saiyan_btn_84x28_clicked.png");
			button.SetTextColor(flag2 ? Night : White, ComponentState.Normal);
			button.SetTextColor(flag2 ? Night : OrangeLight, ComponentState.Hovered);
			button.SetTextColor(Night, ComponentState.Active);
			button.SetSize(84, 28);
			button.SetPosition(24 + i * 84, Y(80));
			bool toCrafting = i == 1;
			button.Clicked += delegate
			{
				if (toCrafting != Professions.IsCrafting(_selected))
				{
					_selected = (toCrafting ? 5 : 0);
					_shown = string.Empty;
				}
			};
		}
		int num = (flag ? 5 : 0);
		int num2 = (flag ? 9 : 5);
		for (int num3 = num; num3 < num2; num3++)
		{
			int num4 = 118 + (num3 - num) * 70;
			bool flag3 = num3 == _selected;
			if (flag3)
			{
				ImagePanel imagePanel = Keep(new ImagePanel(this, $"Veil{num3}")
				{
					Texture = Globals.ContentManager.GetTexture(TextureType.Gui, "hdv_ligne_survol.png"),
					TextureNinePatchMargin = new Margin(6),
					MouseInputEnabled = false
				});
				imagePanel.SetSize(172, 62);
				imagePanel.SetPosition(22, Y(num4));
			}
			Slot(this, $"Slot{num3}", 28, Y(num4 + 10), Professions.Icon(num3), flag3, locked: false);
			Label label = Keep(MakeLabel(this, $"Name{num3}", 11, flag3 ? White : Entry, _font));
			label.Text = Professions.Name(num3);
			label.SetSize(112, 20);
			label.SetPosition(80, Y(num4 + 6));
			Label label2 = Keep(MakeLabel(this, $"Level{num3}", 12, Professions.Tint(num3), _strongFont));
			label2.Text = Strings.Professions.Level.ToString(me.ProfessionLevel(num3));
			label2.SetSize(112, 20);
			label2.SetPosition(80, Y(num4 + 25));
			Bar(this, $"Bar{num3}", 80, Y(num4 + 47), 104, 8, Ratio(me, num3));
			if (num3 < num2 - 1)
			{
				Rule(this, $"Rule{num3}", 24, Y(num4 + 62 + 3), 168);
			}
			Button button2 = Keep(new Button(this, $"Pick{num3}", disableText: true)
			{
				ShouldDrawBackground = false
			});
			button2.SetSize(168, 62);
			button2.SetPosition(24, Y(num4));
			int choice = num3;
			button2.Clicked += delegate
			{
				_selected = choice;
				_shown = string.Empty;
			};
		}
		int num5 = Enumerable.Range(0, 9).Sum((Func<int, int>)me.ProfessionLevel);
		Rule(this, "TotalRule", 24, Y(520), 168);
		Label label3 = Keep(MakeLabel(this, "TotalLabel", 10, Muted, _font));
		label3.Text = Strings.Professions.TotalLevel;
		label3.SetSize(110, 20);
		label3.SetPosition(32, Y(534));
		Label label4 = Keep(MakeLabel(this, "TotalValue", 20, Gold, _strongFont));
		label4.Text = num5.ToString();
		label4.TextAlign = Pos.Right | Pos.CenterV;
		label4.SetSize(70, 30);
		label4.SetPosition(114, Y(526));
		Label label5 = Keep(MakeLabel(this, "TotalMax", 10, Dim, _font));
		label5.Text = Strings.Professions.OutOf.ToString(9 * me.ProfessionMaximumLevel);
		label5.TextAlign = Pos.Right | Pos.CenterV;
		label5.SetSize(100, 18);
		label5.SetPosition(84, Y(556));
	}

	private void BuildHeader(Player me)
	{
		ImagePanel imagePanel = Keep(MakeCard(this, "HeaderCard"));
		imagePanel.SetSize(528, 124);
		imagePanel.SetPosition(216, Y(72));
		Slot(imagePanel, "HeaderSlot", 18, 18, Professions.Icon(_selected), selected: false, locked: false, keep: false);
		Label label = MakeLabel(imagePanel, "HeaderName", 14, White, _strongFont);
		label.Text = Professions.Name(_selected).ToUpperInvariant();
		label.SetSize(300, 26);
		label.SetPosition(74, 14);
		Label label2 = MakeLabel(imagePanel, "HeaderSubtitle", 10, Muted, _font);
		label2.Text = Professions.Subtitle(_selected);
		label2.SetSize(300, 20);
		label2.SetPosition(74, 42);
		Label label3 = MakeLabel(imagePanel, "LevelCaption", 9, Muted, _font);
		label3.Text = Strings.Professions.LevelCaps;
		label3.TextAlign = Pos.Right | Pos.CenterV;
		label3.SetSize(100, 18);
		label3.SetPosition(410, 10);
		int num = me.ProfessionLevel(_selected);
		Label label4 = MakeLabel(imagePanel, "LevelValue", 28, Gold, _posterFont);
		label4.Text = num.ToString();
		label4.TextAlign = Pos.Right | Pos.CenterV;
		label4.SetSize(100, 44);
		label4.SetPosition(410, 24);
		long num2 = me.ProfessionExperience[_selected];
		long num3 = me.ProfessionNextLevel[_selected];
		Label label5 = MakeLabel(imagePanel, "ExperienceHint", 10, Entry, _font);
		label5.Text = ((num3 > 0) ? Strings.Professions.ExperienceLeft.ToString(Number(num3 - num2), num + 1) : ((string)Strings.Professions.MaximumLevel));
		label5.SetSize(320, 20);
		label5.SetPosition(18, 74);
		if (num3 > 0)
		{
			Label label6 = MakeLabel(imagePanel, "ExperienceValue", 13, White, _strongFont);
			label6.Text = Strings.Professions.ExperienceValue.ToString(Number(num2), Number(num3));
			label6.TextAlign = Pos.Right | Pos.CenterV;
			label6.SetSize(200, 22);
			label6.SetPosition(310, 72);
		}
		Bar(imagePanel, "ExperienceBar", 18, 100, 492, 10, Ratio(me, _selected), keep: false);
	}

	private void BuildBonuses(Player me)
	{
		Label label = Keep(MakeLabel(this, "BonusHeading", 14, White, _strongFont));
		label.Text = Strings.Professions.WhatYourLevelGives;
		label.SetSize(528, 26);
		label.SetPosition(216, Y(206));
		int level = me.ProfessionLevel(_selected);
		(string, string)[] array = ((!Professions.IsCrafting(_selected)) ? new(string, string)[2]
		{
			(Professions.SpeedLabel(_selected), Strings.Professions.Bonus.ToString(Professions.Percent((double)level * me.ProfessionFirstBonusPerLevel(_selected)))),
			(Strings.Professions.RareChance, Strings.Professions.Bonus.ToString(Professions.Percent((double)level * me.ProfessionRarityPerLevel)))
		} : new(string, string)[2]
		{
			(Professions.SpeedLabel(_selected), Professions.Percent(me.PreciseChance(_selected)) + " %"),
			(Strings.Professions.RecipesUnlocked, Strings.Professions.Ratio.ToString(RecipesOf(_selected).Count((CraftingRecipeDescriptor recipe) => Math.Max(1, recipe.ProfessionLevel) <= level), RecipesOf(_selected).Count))
		});
		for (int num = 0; num < array.Length; num++)
		{
			int num2 = 240 + num * 30;
			Label label2 = Keep(MakeLabel(this, $"BonusKey{num}", 11, Entry, _font));
			label2.Text = array[num].Item1;
			label2.SetSize(360, 26);
			label2.SetPosition(232, Y(num2));
			Label label3 = Keep(MakeLabel(this, $"BonusValue{num}", 13, White, _strongFont));
			label3.Text = array[num].Item2;
			label3.TextAlign = Pos.Right | Pos.CenterV;
			label3.SetSize(120, 26);
			label3.SetPosition(608, Y(num2));
			Rule(this, $"BonusRule{num}", 232, Y(num2 + 29), 496);
		}
	}

	private void BuildResources(Player me)
	{
		Label label = Keep(MakeLabel(this, "ResourcesHeading", 14, White, _strongFont));
		label.Text = Strings.Professions.Resources;
		label.SetSize(300, 26);
		label.SetPosition(216, Y(308));
		int level = me.ProfessionLevel(_selected);
		List<ResourceDescriptor> list = (from resource in DatabaseObject<ResourceDescriptor>.Lookup.Values.OfType<ResourceDescriptor>()
			where Professions.Of(resource) == _selected
			orderby Math.Max(1, resource.ProfessionLevel), resource.Name
			select resource).ToList();
		int num = list.Count((ResourceDescriptor resource) => Math.Max(1, resource.ProfessionLevel) <= level);
		Label label2 = Keep(MakeLabel(this, "ResourcesCount", 10, Muted, _font));
		label2.Text = Strings.Professions.UnlockedCount.ToString(num, list.Count);
		label2.TextAlign = Pos.Right | Pos.CenterV;
		label2.SetSize(200, 24);
		label2.SetPosition(528, Y(310));
		if (list.Count == 0)
		{
			Label label3 = Keep(MakeLabel(this, "ResourcesEmpty", 11, Dim, _font));
			label3.Text = Strings.Professions.NoResource;
			label3.SetSize(528, 26);
			label3.SetPosition(232, Y(346));
			return;
		}
		List<ResourceDescriptor> list2 = ((list.Count <= 4) ? list : list.Take(num).TakeLast(Math.Max(4 - (list.Count - num), 2)).Concat(list.Skip(num))
			.Take(4)
			.ToList());
		for (int num2 = 0; num2 < list2.Count; num2++)
		{
			ResourceDescriptor resourceDescriptor = list2[num2];
			int num3 = Math.Max(1, resourceDescriptor.ProfessionLevel);
			bool flag = num3 <= level;
			int num4 = 342 + num2 * 58;
			ItemDescriptor databaseObject;
			List<ItemDescriptor> source = (from drop in resourceDescriptor.Drops
				select (!DatabaseObject<ItemDescriptor>.TryGet(drop.ItemId, out databaseObject)) ? null : databaseObject into item
				where item != null
				select item).Distinct().ToList();
			Slot(this, $"ResourceSlot{num2}", 232, Y(num4 + 6), source.FirstOrDefault()?.Icon ?? Professions.Icon(_selected), selected: false, !flag);
			Label label4 = Keep(MakeLabel(this, $"ResourceName{num2}", 11, flag ? Entry : Dim, _font));
			label4.Text = resourceDescriptor.Name;
			label4.SetSize(300, 22);
			label4.SetPosition(286, Y(num4 + 6));
			Label label5 = Keep(MakeLabel(this, $"ResourceDetail{num2}", 10, flag ? Muted : Dim, _font));
			label5.Text = string.Join(" · ", source.Select((ItemDescriptor item) => item.Name));
			label5.SetSize(300, 20);
			label5.SetPosition(286, Y(num4 + 27));
			Label label6 = Keep(MakeLabel(this, $"ResourceState{num2}", 13, flag ? Green : White, _strongFont));
			label6.Text = (flag ? ((string)Strings.Professions.Unlocked) : Strings.Professions.Level.ToString(num3));
			label6.TextAlign = Pos.Right | Pos.CenterV;
			label6.SetSize(160, 22);
			label6.SetPosition(568, Y(flag ? (num4 + 16) : (num4 + 6)));
			if (!flag)
			{
				Label label7 = Keep(MakeLabel(this, $"ResourceLeft{num2}", 10, Dim, _font));
				label7.Text = Strings.Professions.LevelsLeft.ToString(num3 - level);
				label7.TextAlign = Pos.Right | Pos.CenterV;
				label7.SetSize(160, 18);
				label7.SetPosition(568, Y(num4 + 28));
			}
			if (num2 < list2.Count - 1)
			{
				Rule(this, $"ResourceRule{num2}", 232, Y(num4 + 54), 496);
			}
		}
	}

	private static List<CraftingRecipeDescriptor> RecipesOf(int profession)
	{
		return (from recipe in DatabaseObject<CraftingRecipeDescriptor>.Lookup.Values.OfType<CraftingRecipeDescriptor>()
			where recipe.Profession == profession
			orderby Math.Max(1, recipe.ProfessionLevel), recipe.Name
			select recipe).ToList();
	}

	private void BuildRecipes(Player me)
	{
		Label label = Keep(MakeLabel(this, "RecipesHeading", 14, White, _strongFont));
		label.Text = Strings.Professions.Recipes;
		label.SetSize(300, 26);
		label.SetPosition(216, Y(308));
		int level = me.ProfessionLevel(_selected);
		List<CraftingRecipeDescriptor> list = RecipesOf(_selected);
		int num = list.Count((CraftingRecipeDescriptor recipe) => Math.Max(1, recipe.ProfessionLevel) <= level);
		Label label2 = Keep(MakeLabel(this, "RecipesCount", 10, Muted, _font));
		label2.Text = Strings.Professions.UnlockedCount.ToString(num, list.Count);
		label2.TextAlign = Pos.Right | Pos.CenterV;
		label2.SetSize(200, 24);
		label2.SetPosition(528, Y(310));
		if (list.Count == 0)
		{
			Label label3 = Keep(MakeLabel(this, "RecipesEmpty", 11, Dim, _font));
			label3.Text = Strings.Professions.NoRecipe;
			label3.SetSize(528, 26);
			label3.SetPosition(232, Y(346));
			return;
		}
		List<CraftingRecipeDescriptor> list2 = ((list.Count <= 4) ? list : list.Take(num).TakeLast(Math.Max(4 - (list.Count - num), 2)).Concat(list.Skip(num))
			.Take(4)
			.ToList());
		for (int num2 = 0; num2 < list2.Count; num2++)
		{
			CraftingRecipeDescriptor craftingRecipeDescriptor = list2[num2];
			int num3 = Math.Max(1, craftingRecipeDescriptor.ProfessionLevel);
			bool flag = num3 <= level;
			int num4 = 342 + num2 * 58;
			ItemDescriptor itemDescriptor = DatabaseObject<ItemDescriptor>.Get(craftingRecipeDescriptor.ItemId);
			Slot(this, $"RecipeSlot{num2}", 232, Y(num4 + 6), itemDescriptor?.Icon ?? Professions.Icon(_selected), selected: false, !flag);
			Label label4 = Keep(MakeLabel(this, $"RecipeName{num2}", 11, flag ? Entry : Dim, _font));
			label4.Text = itemDescriptor?.Name ?? craftingRecipeDescriptor.Name;
			label4.SetSize(300, 22);
			label4.SetPosition(286, Y(num4 + 6));
			Label label5 = Keep(MakeLabel(this, $"RecipeDetail{num2}", 10, flag ? Muted : Dim, _font));
			label5.Text = string.Join(" · ", from ingredient in craftingRecipeDescriptor.Ingredients
				select (Item: DatabaseObject<ItemDescriptor>.Get(ingredient.ItemId), Quantity: ingredient.Quantity) into entry
				where entry.Item != null
				select $"{entry.Quantity} {entry.Item.Name}");
			label5.SetSize(300, 20);
			label5.SetPosition(286, Y(num4 + 27));
			Label label6 = Keep(MakeLabel(this, $"RecipeState{num2}", 13, flag ? Green : White, _strongFont));
			label6.Text = (flag ? ((string)Strings.Professions.Unlocked) : Strings.Professions.Level.ToString(num3));
			label6.TextAlign = Pos.Right | Pos.CenterV;
			label6.SetSize(160, 22);
			label6.SetPosition(568, Y(flag ? (num4 + 16) : (num4 + 6)));
			if (!flag)
			{
				Label label7 = Keep(MakeLabel(this, $"RecipeLeft{num2}", 10, Dim, _font));
				label7.Text = Strings.Professions.LevelsLeft.ToString(num3 - level);
				label7.TextAlign = Pos.Right | Pos.CenterV;
				label7.SetSize(160, 18);
				label7.SetPosition(568, Y(num4 + 28));
			}
			if (num2 < list2.Count - 1)
			{
				Rule(this, $"RecipeRule{num2}", 232, Y(num4 + 54), 496);
			}
		}
	}

	private void BuildGame(Player me)
	{
		Label label = Keep(MakeLabel(this, "GameHeading", 14, White, _strongFont));
		label.Text = Strings.Professions.Game;
		label.SetSize(300, 26);
		label.SetPosition(216, Y(308));
		int level = me.ProfessionLevel(4);
		List<NPCDescriptor> list = (from npc in DatabaseObject<NPCDescriptor>.Lookup.Values.OfType<NPCDescriptor>()
			where npc.IsGame
			orderby Math.Max(1, npc.HunterLevel), npc.Level, npc.Name
			select npc).ToList();
		int num = list.Count((NPCDescriptor npc) => Math.Max(1, npc.HunterLevel) <= level);
		Label label2 = Keep(MakeLabel(this, "GameCount", 10, Muted, _font));
		label2.Text = Strings.Professions.GameUnlockedCount.ToString(num, list.Count);
		label2.TextAlign = Pos.Right | Pos.CenterV;
		label2.SetSize(200, 24);
		label2.SetPosition(528, Y(310));
		if (list.Count == 0)
		{
			Label label3 = Keep(MakeLabel(this, "GameEmpty", 11, Dim, _font));
			label3.Text = Strings.Professions.NoGame;
			label3.SetSize(512, 26);
			label3.SetPosition(232, Y(346));
		}
		List<NPCDescriptor> list2 = ((list.Count <= 3) ? list : list.Take(num).TakeLast(Math.Max(3 - (list.Count - num), 1)).Concat(list.Skip(num))
			.Take(3)
			.ToList());
		for (int num2 = 0; num2 < list2.Count; num2++)
		{
			NPCDescriptor nPCDescriptor = list2[num2];
			int num3 = Math.Max(1, nPCDescriptor.HunterLevel);
			bool flag = num3 <= level;
			int num4 = 342 + num2 * 58;
			List<Drop> list3 = nPCDescriptor.Drops.Where((Drop drop) => drop.Hunting).ToList();
			ItemDescriptor databaseObject;
			List<ItemDescriptor> source = (from drop in (list3.Count > 0) ? list3 : nPCDescriptor.Drops
				select (!DatabaseObject<ItemDescriptor>.TryGet(drop.ItemId, out databaseObject)) ? null : databaseObject into item
				where item != null
				select item).Distinct().ToList();
			Slot(this, $"GameSlot{num2}", 232, Y(num4 + 6), source.FirstOrDefault()?.Icon ?? Professions.Icon(4), selected: false, !flag);
			Label label4 = Keep(MakeLabel(this, $"GameName{num2}", 11, flag ? Entry : Dim, _font));
			label4.Text = nPCDescriptor.Name;
			label4.SetSize(300, 22);
			label4.SetPosition(286, Y(num4 + 6));
			Label label5 = Keep(MakeLabel(this, $"GameDetail{num2}", 10, flag ? Muted : Dim, _font));
			label5.Text = string.Join(" · ", source.Select((ItemDescriptor item) => item.Name));
			label5.SetSize(300, 20);
			label5.SetPosition(286, Y(num4 + 27));
			Label label6 = Keep(MakeLabel(this, $"GameState{num2}", 13, flag ? Green : White, _strongFont));
			label6.Text = (flag ? ((string)Strings.Professions.GameUnlocked) : Strings.Professions.Level.ToString(num3));
			label6.TextAlign = Pos.Right | Pos.CenterV;
			label6.SetSize(160, 22);
			label6.SetPosition(568, Y(flag ? (num4 + 16) : (num4 + 6)));
			if (!flag)
			{
				Label label7 = Keep(MakeLabel(this, $"GameLeft{num2}", 10, Dim, _font));
				label7.Text = Strings.Professions.LevelsLeft.ToString(num3 - level);
				label7.TextAlign = Pos.Right | Pos.CenterV;
				label7.SetSize(160, 18);
				label7.SetPosition(568, Y(num4 + 28));
			}
			Rule(this, $"GameRule{num2}", 232, Y(num4 + 54), 496);
		}
		Label label8 = Keep(MakeLabel(this, "GameNote", 10, Dim, _font));
		label8.Text = Strings.Professions.OtherMonsters;
		label8.SetSize(512, 22);
		label8.SetPosition(232, Y(342 + Math.Max(1, list2.Count) * 58 + 8));
	}

	private static double Ratio(Player me, int profession)
	{
		long num = me.ProfessionNextLevel[profession];
		if (num > 0)
		{
			return Math.Clamp((double)me.ProfessionExperience[profession] / (double)num, 0.0, 1.0);
		}
		return 1.0;
	}

	private static string Number(long value)
	{
		return value.ToString("N0", CultureInfo.GetCultureInfo("fr-FR")).Replace('\u202f', ' ').Replace('\u00a0', ' ');
	}

	private new int Y(int windowY)
	{
		return windowY - _laidOutTitleBarHeight;
	}

	private void Layout()
	{
		_laidOutTitleBarHeight = base.TitleBarBounds.Height;
		_title.SetSize(400, 38);
		_title.SetPosition(26, Y(10));
		_close.SetSize(24, 24);
		_close.SetPosition(720, Y(16));
	}

	private T Keep<T>(T control) where T : Base
	{
		_built.Add(control);
		return control;
	}

	private void Slot(Base parent, string name, int x, int y, string icon, bool selected, bool locked, bool keep = true)
	{
		ImagePanel imagePanel = new ImagePanel(parent, name)
		{
			Texture = Globals.ContentManager.GetTexture(TextureType.Gui, selected ? "saiyan_slot_selected.png" : "saiyan_slot.png"),
			TextureNinePatchMargin = new Margin(6),
			MouseInputEnabled = false
		};
		imagePanel.SetSize(42, 42);
		imagePanel.SetPosition(x, y);
		if (keep)
		{
			_built.Add(imagePanel);
		}
		ImagePanel imagePanel2 = new ImagePanel(imagePanel, "Icon")
		{
			Texture = Globals.ContentManager.GetTexture(TextureType.Item, icon),
			MouseInputEnabled = false
		};
		imagePanel2.SetSize(32, 32);
		imagePanel2.SetPosition(5, 5);
		if (locked)
		{
			imagePanel2.RenderColor = new Color(90, 255, 255, 255);
			ImagePanel imagePanel3 = new ImagePanel(imagePanel, "Locked")
			{
				Texture = Globals.ContentManager.GetTexture(TextureType.Gui, "saiyan_slot_locked.png"),
				MouseInputEnabled = false
			};
			imagePanel3.SetSize(42, 42);
			imagePanel3.SetPosition(0, 0);
		}
	}

	private void Bar(Base parent, string name, int x, int y, int width, int height, double ratio, bool keep = true)
	{
		ImagePanel imagePanel = new ImagePanel(parent, name + "Track")
		{
			Texture = Globals.ContentManager.GetTexture(TextureType.Gui, "saiyan_bar_track.png"),
			TextureNinePatchMargin = new Margin(4),
			MouseInputEnabled = false
		};
		imagePanel.SetSize(width, height);
		imagePanel.SetPosition(x, y);
		if (keep)
		{
			_built.Add(imagePanel);
		}
		if (!(ratio <= 0.0))
		{
			ImagePanel imagePanel2 = new ImagePanel(imagePanel, name + "Fill")
			{
				Texture = Globals.ContentManager.GetTexture(TextureType.Gui, "saiyan_bar_fill_exp.png"),
				TextureNinePatchMargin = new Margin(4),
				MouseInputEnabled = false
			};
			imagePanel2.SetSize(Math.Max(10, (int)Math.Round((double)width * ratio)), height);
			imagePanel2.SetPosition(0, 0);
		}
	}

	private void Rule(Base parent, string name, int x, int y, int width)
	{
		ImagePanel imagePanel = Keep(new ImagePanel(parent, name)
		{
			Texture = Globals.ContentManager.GetTexture(TextureType.Gui, "hdv_filet.png"),
			MouseInputEnabled = false
		});
		imagePanel.SetSize(width, 1);
		imagePanel.SetPosition(x, y);
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
			Texture = Globals.ContentManager.GetTexture(TextureType.Gui, "saiyan_card.png"),
			TextureNinePatchMargin = new Margin(8),
			MouseInputEnabled = false
		};
	}
}
