using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Intersect.Client.Core;
using Intersect.Client.Entities;
using Intersect.Client.Framework.Content;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.Framework.Gwen;
using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.Framework.Gwen.Skin;
using Intersect.Client.Framework.Items;
using Intersect.Client.General;
using Intersect.Client.Interface.Shared;
using Intersect.Client.Localization;
using Intersect.Client.Networking;
using Intersect.Framework.Core;
using Intersect.Framework.Core.GameObjects.Crafting;
using Intersect.Framework.Core.GameObjects.Items;
using Intersect.Framework.Core.Market;
using Intersect.Localization;
using Intersect.Models;
using Intersect.Network.Packets.Server;

namespace Intersect.Client.Interface.Game.Crafting;

public sealed class WorkshopWindow : WindowControl
{
	private const int WindowWidth = 760;

	private const int WindowHeight = 600;

	private const int RowX = 24;

	private const int RowWidth = 168;

	private const int RowTop = 84;

	private const int RowHeight = 62;

	private const int RowStep = 70;

	private const int ListX = 216;

	private const int ListY = 234;

	private const int ListWidth = 244;

	private const int RecipeRow = 56;

	private const int VisibleRecipes = 6;

	private const int DetailX = 472;

	private const int CtaX = 486;

	private const int CtaY = 546;

	private static readonly Color Gold = new Color(255, 245, 190, 90);

	private static readonly Color White = new Color(255, 242, 242, 242);

	private static readonly Color Entry = new Color(255, 196, 204, 220);

	private static readonly Color Muted = new Color(255, 176, 186, 206);

	private static readonly Color Dim = new Color(255, 150, 160, 182);

	private static readonly Color Green = new Color(255, 142, 214, 124);

	private static readonly Color Red = new Color(255, 255, 123, 107);

	private static readonly Color Orange = new Color(255, 255, 172, 82);

	private static readonly Color Night = new Color(255, 11, 14, 23);

	private readonly Canvas _gameCanvas;

	private readonly bool _journal;

	private readonly IFont? _font;

	private readonly IFont? _mediumFont;

	private readonly IFont? _strongFont;

	private readonly IFont? _ctaFont;

	private readonly IFont? _posterFont;

	private readonly Label _title;

	private readonly Button _close;

	private readonly List<Intersect.Client.Framework.Gwen.Control.Base> _built = new List<Intersect.Client.Framework.Gwen.Control.Base>();

	private readonly List<(int Profession, List<CraftingRecipeDescriptor> Recipes)> _groups = new List<(int, List<CraftingRecipeDescriptor>)>();

	private int _group;

	private Guid _recipe;

	private bool _craftableOnly;

	private int _quantity = 1;

	private long _requestedAt = -10000L;

	private int _pricesVersion;

	private string _shown = string.Empty;

	private int _laidOutTitleBarHeight = -1;

	private ImagePanel? _ctaFill;

	private Label? _ctaLabel;

	private Label? _quantityLabel;

	private Button? _craftButton;

	private int _maxQuantity = 1;

	private readonly ScrollControl _recipeScroll;

	private bool _tooltipShown;

	private string _listeAffichee = string.Empty;

	public bool IsCrafting => false;

	private (int Profession, List<CraftingRecipeDescriptor> Recipes) Group
	{
		get
		{
			if (_group < 0 || _group >= _groups.Count)
			{
				return (Profession: -1, Recipes: new List<CraftingRecipeDescriptor>());
			}
			return _groups[_group];
		}
	}

	public WorkshopWindow(Canvas gameCanvas, bool journal)
		: base(gameCanvas, string.Empty, modal: false, "WorkshopWindow")
	{
		_gameCanvas = gameCanvas;
		_journal = journal;
		_font = Globals.ContentManager.GetFont("chakrapetch");
		_mediumFont = Globals.ContentManager.GetFont("chakrapetchmedium") ?? _font;
		_strongFont = Globals.ContentManager.GetFont("rajdhanibold");
		_ctaFont = Globals.ContentManager.GetFont("sairanom") ?? _strongFont;
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
		_title.Text = Strings.Workshop.Title.ToString().ToUpperInvariant();
		WindowTitle.Apply(_title);
		_close = new Button(this, "Close", disableText: true);
		_close.SetStateTexture(ComponentState.Normal, "saiyan_close.png");
		_close.SetStateTexture(ComponentState.Hovered, "saiyan_close_hover.png");
		_close.SetStateTexture(ComponentState.Active, "saiyan_close_clicked.png");
		_close.Clicked += delegate
		{
			Close();
		};
		_recipeScroll = new ScrollControl(this, "RecipeScroll");
		StyleScroll(_recipeScroll);
		_recipeScroll.IsHidden = true;
		BuildGroups();
		MarketPrices.Received += OnPrices;
		RequestPrices();
		Layout();
		Show();
		BringToFront();
	}

	public void Detach()
	{
		MarketPrices.Received -= OnPrices;
		Interface.GameUi?.ItemDescriptionWindow?.Hide();
		DelayedDelete();
	}

	private void OnPrices(MarketPricesResultPacket _)
	{
		_pricesVersion++;
	}

	protected override void DoPrelayout(Intersect.Client.Framework.Gwen.Skin.Base skin)
	{
		base.DoPrelayout(skin);
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
			float avancee;
			int index;
			int nombre;
			bool flag = Artisanat.Progression(me.Id, out avancee, out index, out nombre);
			bool flag2 = !flag && Timing.Global.Milliseconds - _requestedAt < 1500;
			string text = $"{me.ProfessionsVersion}:{InventorySignature(me)}:{_group}:{_recipe}:{_craftableOnly}:{(flag ? $"{index}/{nombre}" : (flag2 ? "w" : "-"))}:{_pricesVersion}";
			if (text != _shown)
			{
				_shown = text;
				Rebuild(me, flag, index, nombre);
			}
			if (_ctaFill != null)
			{
				int num = (flag ? ((int)Math.Round(244f * avancee)) : 0);
				_ctaFill.IsHidden = num <= 0;
				_ctaFill.SetSize(Math.Max(1, num), 34);
				_ctaFill.SetTextureRect(0, 0, Math.Max(1, num), 34);
				_ctaLabel?.SetTextColor((avancee > 0.5f) ? Night : White, ComponentState.Normal);
			}
		}
	}

	private void BuildGroups()
	{
		_groups.Clear();
		DbList<CraftingRecipeDescriptor> dbList = Globals.ActiveCraftingTable?.Crafts;
		if (dbList == null)
		{
			return;
		}
		List<CraftingRecipeDescriptor> source = (from recipe in dbList.Select(DatabaseObject<CraftingRecipeDescriptor>.Get)
			where recipe != null
			select recipe).Cast<CraftingRecipeDescriptor>().ToList();
		foreach (int profession in from p in source.Select((CraftingRecipeDescriptor recipe) => (!MetiersArtisanat.EstArtisanat(recipe.Profession)) ? (-1) : recipe.Profession).Distinct()
			orderby (p >= 0) ? p : 99
			select p)
		{
			_groups.Add((profession, (from recipe in source
				where (MetiersArtisanat.EstArtisanat(recipe.Profession) ? recipe.Profession : (-1)) == profession
				orderby Math.Max(1, recipe.ProfessionLevel), recipe.Name
				select recipe).ToList()));
		}
		if (_groups.Count > 4)
		{
			int num = _groups.FindIndex(((int Profession, List<CraftingRecipeDescriptor> Recipes) group) => group.Profession < 0);
			if (num >= 0)
			{
				List<CraftingRecipeDescriptor> item = _groups[num].Recipes;
				_groups.RemoveAt(num);
				_groups[0].Recipes.AddRange(item);
			}
		}
		_group = 0;
		_recipe = _groups.FirstOrDefault().Recipes?.FirstOrDefault()?.Id ?? Guid.Empty;
	}

	private void RequestPrices()
	{
		Guid[] array = _groups.SelectMany<(int, List<CraftingRecipeDescriptor>), CraftingRecipeDescriptor>(((int Profession, List<CraftingRecipeDescriptor> Recipes) group) => group.Recipes).SelectMany((CraftingRecipeDescriptor recipe) => recipe.Ingredients.Select((CraftingRecipeIngredient ingredient) => ingredient.ItemId).Append(recipe.ItemId)).Distinct()
			.ToArray();
		if (array.Length != 0)
		{
			MarketPrices.Request(array);
		}
	}

	private static string InventorySignature(Player me)
	{
		long num = 17L;
		IItem[] inventory = me.Inventory;
		foreach (IItem item in inventory)
		{
			if (item != null && item.ItemId != Guid.Empty)
			{
				num = num * 31 + item.ItemId.GetHashCode() * 7 + item.Quantity;
			}
		}
		return num.ToString();
	}

	private static int Owned(Player me, Guid itemId)
	{
		return me.GetQuantityOfItemInInventory(itemId);
	}

	private static bool Locked(Player me, CraftingRecipeDescriptor recipe)
	{
		if (MetiersArtisanat.EstArtisanat(recipe.Profession))
		{
			return Math.Max(1, recipe.ProfessionLevel) > me.ProfessionLevel(recipe.Profession);
		}
		return false;
	}

	private static int Craftable(Player me, CraftingRecipeDescriptor recipe)
	{
		if (recipe.Ingredients.Count != 0)
		{
			return recipe.Ingredients.Min((CraftingRecipeIngredient ingredient) => Owned(me, ingredient.ItemId) / Math.Max(1, ingredient.Quantity));
		}
		return 999;
	}

	private static int Missing(Player me, CraftingRecipeDescriptor recipe)
	{
		return recipe.Ingredients.Count((CraftingRecipeIngredient ingredient) => Owned(me, ingredient.ItemId) < ingredient.Quantity);
	}

	private static long Experience(Player me, CraftingRecipeDescriptor recipe)
	{
		if (recipe.ProfessionExperience <= 0)
		{
			return Math.Max(1L, (long)Math.Round((double)recipe.Time / 1000.0 * me.ProfessionCraftExperiencePerSecond));
		}
		return recipe.ProfessionExperience;
	}

	private static Color RarityColor(ItemDescriptor? item)
	{
		if (item == null || !CustomColors.Items.Rarities.TryGetValue(item.Rarity, out var value) || !(value != null))
		{
			return Entry;
		}
		return value;
	}

	private static string Number(long value)
	{
		return value.ToString("N0", CultureInfo.GetCultureInfo("fr-FR")).Replace('\u202f', ' ').Replace('\u00a0', ' ');
	}

	private static string Seconds(int milliseconds)
	{
		return Professions.Percent((double)milliseconds / 1000.0);
	}

	private void Rebuild(Player me, bool crafting, int index, int count)
	{
		if (_tooltipShown)
		{
			Interface.GameUi?.ItemDescriptionWindow?.Hide();
			_tooltipShown = false;
		}
		foreach (Intersect.Client.Framework.Gwen.Control.Base item in _built)
		{
			item.Parent?.RemoveChild(item, dispose: true);
		}
		string text = $"{_group}:{_craftableOnly}";
		float num = ((text == _listeAffichee && !_recipeScroll.IsHidden) ? _recipeScroll.VerticalScrollBar.ScrollAmount : 0f);
		_listeAffichee = text;
		_built.Clear();
		_recipeScroll.DeleteAll();
		_recipeScroll.IsHidden = true;
		_ctaFill = null;
		_ctaLabel = null;
		_quantityLabel = null;
		_craftButton = null;
		BuildSidebar(me);
		BuildHeader(me);
		BuildRecipes(me, crafting);
		if (num > 0f && !_recipeScroll.IsHidden)
		{
			_recipeScroll.UpdateScrollBars();
			_recipeScroll.VerticalScrollBar.SetScrollAmount(num, forceUpdate: true);
			_recipeScroll.UpdateScrollBars();
		}
		BuildDetail(me, crafting, index, count);
	}

	private void BuildSidebar(Player me)
	{
		for (int i = 0; i < Math.Min(4, _groups.Count); i++)
		{
			(int Profession, List<CraftingRecipeDescriptor> Recipes) tuple = _groups[i];
			int item = tuple.Profession;
			List<CraftingRecipeDescriptor> item2 = tuple.Recipes;
			int num = 84 + i * 70;
			bool flag = i == _group;
			if (flag)
			{
				Veil($"GroupVeil{i}", 22, num, 172, 62);
			}
			string icon = (MetiersArtisanat.EstArtisanat(item) ? Professions.Icon(item) : (DatabaseObject<ItemDescriptor>.Get(item2.FirstOrDefault()?.ItemId ?? Guid.Empty)?.Icon ?? "gants_combat.png"));
			Slot(this, $"GroupSlot{i}", 28, Y(num + 10), icon, flag, locked: false);
			Label label = Keep(MakeLabel(this, $"GroupName{i}", 11, flag ? White : Entry, _font));
			label.Text = (MetiersArtisanat.EstArtisanat(item) ? Professions.Name(item) : (Globals.ActiveCraftingTable?.Name ?? string.Empty));
			label.SetSize(112, 20);
			label.SetPosition(80, Y(num + 6));
			if (MetiersArtisanat.EstArtisanat(item))
			{
				Label label2 = Keep(MakeLabel(this, $"GroupLevel{i}", 12, Professions.Tint(item), _strongFont));
				label2.Text = Strings.Professions.Level.ToString(me.ProfessionLevel(item));
				label2.SetSize(112, 20);
				label2.SetPosition(80, Y(num + 25));
				Bar(this, $"GroupBar{i}", 80, Y(num + 47), 104, 8, Ratio(me, item));
			}
			if (i < Math.Min(4, _groups.Count) - 1)
			{
				Rule($"GroupRule{i}", 24, Y(num + 62 + 3), 168);
			}
			int pick = i;
			Hit($"GroupHit{i}", 24, Y(num), 168, 62, delegate
			{
				if (_group != pick)
				{
					_group = pick;
					_recipe = _groups[pick].Recipes.FirstOrDefault()?.Id ?? Guid.Empty;
					_quantity = 1;
				}
			});
		}
		Rule("BagRule", 24, Y(364), 168);
		Label label3 = Keep(MakeLabel(this, "BagLabel", 10, Muted, _font));
		label3.Text = Strings.Workshop.InYourBag;
		label3.SetSize(168, 18);
		label3.SetPosition(28, Y(370));
		List<Guid> list = Group.Recipes.SelectMany((CraftingRecipeDescriptor recipe) => recipe.Ingredients.Select((CraftingRecipeIngredient ingredient) => ingredient.ItemId)).Distinct().Take(12)
			.ToList();
		for (int num2 = 0; num2 < list.Count; num2++)
		{
			ItemDescriptor itemDescriptor = DatabaseObject<ItemDescriptor>.Get(list[num2]);
			if (itemDescriptor == null)
			{
				continue;
			}
			int x = 24 + num2 % 4 * 42;
			int num3 = 392 + num2 / 4 * 42;
			int num4 = Owned(me, itemDescriptor.Id);
			ImagePanel imagePanel = Slot(this, $"Material{num2}", x, Y(num3), itemDescriptor.Icon, selected: false, locked: false, keep: true, 40);
			if (num4 <= 0)
			{
				ImagePanel imagePanel2 = imagePanel.Children.OfType<ImagePanel>().FirstOrDefault();
				if (imagePanel2 != null)
				{
					imagePanel2.RenderColor = new Color(90, 255, 255, 255);
				}
			}
			Tooltip(imagePanel, itemDescriptor);
			Label label4 = Keep(MakeLabel(this, $"MaterialCount{num2}", 10, (num4 > 0) ? White : Dim, _strongFont));
			label4.Text = num4.ToString();
			label4.TextAlign = Pos.Right | Pos.Bottom;
			label4.SetSize(36, 20);
			label4.SetPosition(x, Y(num3 + 20));
		}
		Rule("WorkshopRule", 24, Y(528), 168);
		Label label5 = Keep(MakeLabel(this, "WorkshopCaption", 10, Muted, _font));
		label5.Text = Strings.Workshop.WorkshopCaps;
		label5.SetSize(80, 18);
		label5.SetPosition(28, Y(534));
		Label label6 = Keep(MakeLabel(this, "WorkshopReach", 10, _journal ? Dim : Green, _mediumFont));
		label6.Text = (_journal ? string.Empty : $"• {Strings.Workshop.InReach}");
		label6.TextAlign = Pos.Right | Pos.CenterV;
		label6.SetSize(90, 18);
		label6.SetPosition(100, Y(534));
		Label label7 = Keep(MakeLabel(this, "WorkshopName", 13, White, _strongFont));
		label7.Text = Globals.ActiveCraftingTable?.Name ?? string.Empty;
		label7.SetSize(168, 24);
		label7.SetPosition(28, Y(552));
	}

	private void BuildHeader(Player me)
	{
		(int Profession, List<CraftingRecipeDescriptor> Recipes) tuple = Group;
		int item = tuple.Profession;
		List<CraftingRecipeDescriptor> item2 = tuple.Recipes;
		ImagePanel imagePanel = Keep(MakeCard(this, "HeaderCard"));
		imagePanel.SetSize(528, 112);
		imagePanel.SetPosition(216, Y(72));
		bool flag = MetiersArtisanat.EstArtisanat(item);
		Slot(imagePanel, "HeaderSlot", 18, 16, flag ? Professions.Icon(item) : "gants_combat.png", selected: false, locked: false, keep: false);
		Label label = MakeLabel(imagePanel, "HeaderName", 18, White, _strongFont);
		label.Text = (flag ? Professions.Name(item) : (Globals.ActiveCraftingTable?.Name ?? string.Empty)).ToUpperInvariant();
		label.SetSize(320, 32);
		label.SetPosition(74, 8);
		int num = (flag ? me.ProfessionLevel(item) : 0);
		int num2 = item2.Count((CraftingRecipeDescriptor recipe) => !Locked(me, recipe));
		Label label2 = MakeLabel(imagePanel, "HeaderSubtitle", 10, Muted, _mediumFont);
		label2.Text = Strings.Workshop.RecipesOf.ToString(flag ? Professions.Subtitle(item) : Strings.Workshop.Title.ToString(), num2, item2.Count);
		label2.SetSize(360, 22);
		label2.SetPosition(74, 40);
		if (flag)
		{
			Label label3 = MakeLabel(imagePanel, "LevelCaption", 9, Muted, _font);
			label3.Text = Strings.Professions.LevelCaps;
			label3.TextAlign = Pos.Right | Pos.CenterV;
			label3.SetSize(100, 18);
			label3.SetPosition(410, 8);
			Label label4 = MakeLabel(imagePanel, "LevelValue", 32, Gold, _strongFont);
			label4.Text = num.ToString();
			label4.TextAlign = Pos.Right | Pos.CenterV;
			label4.SetSize(100, 60);
			label4.SetPosition(410, 14);
			long num3 = me.ProfessionExperience[item];
			long num4 = me.ProfessionNextLevel[item];
			Label label5 = MakeLabel(imagePanel, "ExperienceHint", 10, Entry, _mediumFont);
			label5.Text = ((num4 > 0) ? Strings.Professions.ExperienceLeft.ToString(Number(num4 - num3), num + 1) : ((string)Strings.Professions.MaximumLevel));
			label5.SetSize(320, 20);
			label5.SetPosition(18, 68);
			if (num4 > 0)
			{
				Label label6 = MakeLabel(imagePanel, "ExperienceValue", 12, White, _strongFont);
				label6.Text = Strings.Professions.ExperienceValue.ToString(Number(num3), Number(num4));
				label6.TextAlign = Pos.Right | Pos.CenterV;
				label6.SetSize(200, 22);
				label6.SetPosition(310, 66);
			}
			Bar(imagePanel, "ExperienceBar", 18, 94, 492, 8, Ratio(me, item), keep: false);
		}
	}

	private void BuildRecipes(Player me, bool crafting)
	{
		for (int i = 0; i < 2; i++)
		{
			bool flag = i == 1 == _craftableOnly;
			Button button = Keep(MakeButton(this, $"Filter{i}", 84, 28, "saiyan_btn"));
			button.FontSize = 10;
			button.Text = ((i == 0) ? Strings.Workshop.All : Strings.Workshop.Craftable);
			button.SetStateTexture(ComponentState.Normal, flag ? "saiyan_btn_84x28_clicked.png" : "saiyan_btn_84x28.png");
			button.SetStateTexture(ComponentState.Hovered, flag ? "saiyan_btn_84x28_clicked.png" : "saiyan_btn_84x28_hover.png");
			button.SetTextColor(flag ? Night : Entry, ComponentState.Normal);
			button.SetTextColor(flag ? Night : Orange, ComponentState.Hovered);
			button.SetPosition(216 + i * 88, Y(196));
			bool only = i == 1;
			button.Clicked += delegate
			{
				_craftableOnly = only;
			};
		}
		List<CraftingRecipeDescriptor> list = Group.Recipes.Where((CraftingRecipeDescriptor recipe) => !_craftableOnly || (!Locked(me, recipe) && Craftable(me, recipe) > 0)).ToList();
		Label label = Keep(MakeLabel(this, "RecipeCount", 10, Dim, _mediumFont));
		label.Text = ((list.Count == 1) ? ((string)Strings.Workshop.RecipeCountOne) : Strings.Workshop.RecipeCount.ToString(list.Count));
		label.TextAlign = Pos.Right | Pos.CenterV;
		label.SetSize(80, 28);
		label.SetPosition(380, Y(196));
		if (list.Count == 0)
		{
			Label label2 = Keep(MakeLabel(this, "RecipesEmpty", 10, Dim, _mediumFont));
			label2.Text = ((Group.Recipes.Count == 0) ? Strings.Workshop.NoRecipes : Strings.Workshop.NothingCraftable);
			label2.SetSize(244, 24);
			label2.SetPosition(222, Y(246));
			return;
		}
		Intersect.Client.Framework.Gwen.Control.Base obj = this;
		int num = Y(234);
		int num2 = 216;
		if (list.Count > 6)
		{
			_recipeScroll.IsHidden = false;
			_recipeScroll.SetSize(260, 336);
			_recipeScroll.SetPosition(214, Y(234));
			obj = _recipeScroll;
			num = 0;
			num2 = 2;
		}
		bool flag2 = crafting || Timing.Global.Milliseconds - _requestedAt < 1500;
		for (int num3 = 0; num3 < list.Count; num3++)
		{
			CraftingRecipeDescriptor craftingRecipeDescriptor = list[num3];
			ItemDescriptor itemDescriptor = DatabaseObject<ItemDescriptor>.Get(craftingRecipeDescriptor.ItemId);
			int num4 = num + num3 * 56;
			bool flag3 = craftingRecipeDescriptor.Id == _recipe;
			bool flag4 = Locked(me, craftingRecipeDescriptor);
			int num5 = Craftable(me, craftingRecipeDescriptor);
			if (flag3)
			{
				ImagePanel imagePanel = new ImagePanel(obj, $"RecipeVeil{num3}")
				{
					Texture = Globals.ContentManager.GetTexture(TextureType.Gui, "hdv_ligne_survol.png"),
					TextureNinePatchMargin = new Margin(6),
					MouseInputEnabled = false
				};
				imagePanel.SetSize(248, 54);
				imagePanel.SetPosition(num2 - 2, num4);
				KeepIfTop(obj, imagePanel);
			}
			ImagePanel control = Slot(obj, $"RecipeSlot{num3}", num2 + 4, num4 + 6, itemDescriptor?.Icon ?? string.Empty, selected: false, flag4, obj == this);
			Tooltip(control, itemDescriptor);
			Label label3 = MakeLabel(obj, $"RecipeName{num3}", 11, flag4 ? Dim : (flag3 ? White : Entry), _font);
			label3.Text = itemDescriptor?.Name ?? craftingRecipeDescriptor.Name;
			label3.SetSize(184, 22);
			label3.SetPosition(num2 + 56, num4 + 6);
			KeepIfTop(obj, label3);
			Label label4 = MakeLabel(obj, $"RecipeState{num3}", 10, Dim, _mediumFont);
			if (flag2 && flag3)
			{
				label4.Text = Strings.Workshop.StateCrafting;
				label4.SetTextColor(Orange, ComponentState.Normal);
			}
			else if (flag4)
			{
				label4.Text = Strings.Workshop.StateLevel.ToString(Math.Max(1, craftingRecipeDescriptor.ProfessionLevel));
			}
			else if (num5 > 0)
			{
				label4.Text = Strings.Workshop.StateCraftable.ToString(num5);
				label4.SetTextColor(Green, ComponentState.Normal);
			}
			else
			{
				int num6 = Missing(me, craftingRecipeDescriptor);
				label4.Text = ((num6 == 1) ? ((string)Strings.Workshop.StateMissingOne) : Strings.Workshop.StateMissing.ToString(num6));
				label4.SetTextColor(Red, ComponentState.Normal);
			}
			label4.SetSize(184, 20);
			label4.SetPosition(num2 + 56, num4 + 28);
			KeepIfTop(obj, label4);
			if (num3 < list.Count - 1)
			{
				ImagePanel imagePanel2 = new ImagePanel(obj, $"RecipeRule{num3}")
				{
					Texture = Globals.ContentManager.GetTexture(TextureType.Gui, "hdv_filet.png"),
					MouseInputEnabled = false
				};
				imagePanel2.SetSize(232, 1);
				imagePanel2.SetPosition(num2 + 6, num4 + 56 - 1);
				KeepIfTop(obj, imagePanel2);
			}
			Button button2 = new Button(obj, $"RecipeHit{num3}", disableText: true)
			{
				ShouldDrawBackground = false
			};
			button2.SetSize(194, 54);
			button2.SetPosition(num2 + 50, num4);
			Guid id = craftingRecipeDescriptor.Id;
			button2.Clicked += delegate
			{
				if (_recipe != id)
				{
					_recipe = id;
					_quantity = 1;
				}
			};
			KeepIfTop(obj, button2);
		}
	}

	private void BuildDetail(Player me, bool crafting, int index, int count)
	{
		ImagePanel imagePanel = Keep(MakeCard(this, "DetailCard"));
		imagePanel.SetSize(272, 388);
		imagePanel.SetPosition(472, Y(196));
		CraftingRecipeDescriptor craftingRecipeDescriptor = DatabaseObject<CraftingRecipeDescriptor>.Get(_recipe);
		ItemDescriptor itemDescriptor = ((craftingRecipeDescriptor != null) ? DatabaseObject<ItemDescriptor>.Get(craftingRecipeDescriptor.ItemId) : null);
		if (craftingRecipeDescriptor == null || itemDescriptor == null)
		{
			return;
		}
		bool flag = Locked(me, craftingRecipeDescriptor);
		int num = Craftable(me, craftingRecipeDescriptor);
		ImagePanel control = Slot(this, "ProductSlot", 488, Y(208), itemDescriptor.Icon, selected: true, locked: false);
		Tooltip(control, itemDescriptor);
		Label label = Keep(MakeLabel(this, "ProductName", 14, RarityColor(itemDescriptor), _strongFont));
		label.Text = itemDescriptor.Name;
		label.SetSize(200, 26);
		label.SetPosition(540, Y(202));
		Label label2 = Keep(MakeLabel(this, "ProductType", 10, Muted, _mediumFont));
		Strings.ItemDescription.ItemTypes.TryGetValue((int)itemDescriptor.ItemType, out LocalizedString value);
		string text = ((itemDescriptor.ItemType == ItemType.Equipment && itemDescriptor.EquipmentSlot >= 0 && itemDescriptor.EquipmentSlot < Options.Instance.Equipment.Slots.Count) ? Options.Instance.Equipment.Slots[itemDescriptor.EquipmentSlot] : null);
		label2.Text = string.Join(" · ", new string[2]
		{
			value?.ToString(),
			text
		}.Where((string part) => !string.IsNullOrWhiteSpace(part)));
		label2.SetSize(200, 20);
		label2.SetPosition(540, Y(224));
		int profession = craftingRecipeDescriptor.Profession;
		Label label3 = Keep(MakeLabel(this, "ProductTiming", 10, MetiersArtisanat.EstArtisanat(profession) ? Professions.Tint(profession) : Muted, _mediumFont));
		label3.Text = (MetiersArtisanat.EstArtisanat(profession) ? Strings.Workshop.TimeAndExperience.ToString(Seconds(craftingRecipeDescriptor.Time), Experience(me, craftingRecipeDescriptor)) : Strings.Workshop.TimeOnly.ToString(Seconds(craftingRecipeDescriptor.Time)));
		label3.SetSize(200, 20);
		label3.SetPosition(540, Y(241));
		int num2 = 0;
		foreach (var (stat, num3, num4) in JetDObjet.Fourchettes(itemDescriptor))
		{
			if (num2 >= 3)
			{
				break;
			}
			int num5 = (int)stat;
			int num6 = ((num5 < itemDescriptor.StatsGiven.Length) ? itemDescriptor.StatsGiven[num5] : 0);
			int windowY = 262 + num2 * 18;
			Label label4 = Keep(MakeLabel(this, $"RangeKey{num2}", 10, Entry, _mediumFont));
			label4.Text = ((Strings.ItemDescription.Stats.TryGetValue(num5, out LocalizedString value2) && value2 != null) ? value2.ToString() : stat.ToString());
			label4.SetSize(150, 20);
			label4.SetPosition(488, Y(windowY));
			Label label5 = Keep(MakeLabel(this, $"RangeValue{num2}", 12, White, _strongFont));
			label5.Text = Strings.Workshop.Range.ToString(num6 + num3, num6 + num4);
			label5.TextAlign = Pos.Right | Pos.CenterV;
			label5.SetSize(100, 20);
			label5.SetPosition(628, Y(windowY));
			num2++;
		}
		foreach (string item in Effects(itemDescriptor))
		{
			if (num2 >= 3)
			{
				break;
			}
			Label label6 = Keep(MakeLabel(this, $"Effect{num2}", 10, Entry, _mediumFont));
			label6.Text = Fit(item, _mediumFont, 10, 240);
			label6.SetSize(240, 20);
			label6.SetPosition(488, Y(262 + num2 * 18));
			num2++;
		}
		Rule("DetailRule1", 488, Y(320), 240);
		Label label7 = Keep(MakeLabel(this, "IngredientsLabel", 10, Muted, _font));
		label7.Text = Strings.Workshop.Ingredients;
		label7.SetSize(120, 18);
		label7.SetPosition(488, Y(326));
		Label label8 = Keep(MakeLabel(this, "OwnedLabel", 10, Dim, _font));
		label8.Text = Strings.Workshop.BagAndNeeded;
		label8.TextAlign = Pos.Right | Pos.CenterV;
		label8.SetSize(120, 18);
		label8.SetPosition(608, Y(326));
		for (int num7 = 0; num7 < Math.Min(3, craftingRecipeDescriptor.Ingredients.Count); num7++)
		{
			CraftingRecipeIngredient craftingRecipeIngredient = craftingRecipeDescriptor.Ingredients[num7];
			ItemDescriptor itemDescriptor2 = DatabaseObject<ItemDescriptor>.Get(craftingRecipeIngredient.ItemId);
			int windowY2 = 344 + num7 * 32;
			int num8 = Owned(me, craftingRecipeIngredient.ItemId);
			ImagePanel imagePanel2 = Keep(new ImagePanel(this, $"IngredientIcon{num7}")
			{
				Texture = Globals.ContentManager.GetTexture(TextureType.Item, itemDescriptor2?.Icon ?? string.Empty)
			});
			imagePanel2.SetSize(32, 32);
			imagePanel2.SetPosition(488, Y(windowY2));
			Tooltip(imagePanel2, itemDescriptor2);
			Label label9 = Keep(MakeLabel(this, $"IngredientName{num7}", 10, Entry, _mediumFont));
			label9.Text = Fit(itemDescriptor2?.Name ?? "?", _mediumFont, 10, 150);
			label9.SetSize(150, 32);
			label9.SetPosition(526, Y(windowY2));
			Label label10 = Keep(MakeLabel(this, $"IngredientRatio{num7}", 12, (num8 >= craftingRecipeIngredient.Quantity) ? Green : Red, _strongFont));
			label10.Text = Strings.Workshop.OwnedOfNeeded.ToString(num8, craftingRecipeIngredient.Quantity);
			label10.TextAlign = Pos.Right | Pos.CenterV;
			label10.SetSize(70, 32);
			label10.SetPosition(658, Y(windowY2));
		}
		Rule("DetailRule2", 488, Y(444), 240);
		long num9 = craftingRecipeDescriptor.Ingredients.Sum((CraftingRecipeIngredient ingredient) => PriceOf(ingredient.ItemId) * ingredient.Quantity);
		long num10 = PriceOf(craftingRecipeDescriptor.ItemId);
		bool flag2 = JetDObjet.ADesJets(itemDescriptor);
		MoneyRow("Cost", 450, Strings.Workshop.IngredientsCost, num9, White, 488, 728);
		MoneyRow("Price", 468, Strings.Workshop.MarketPrice, num10, Gold, 488, 728);
		if (flag2 && MarketPrices.TryGet(craftingRecipeDescriptor.ItemId, out MarketPriceInfo info) && info.PerfectRollMedian > 0)
		{
			Label label11 = Keep(MakeLabel(this, "PerfectRoll", 10, Dim, _mediumFont));
			label11.Text = Strings.Workshop.PerfectRoll.ToString(Number(info.PerfectRollMedian));
			label11.TextAlign = Pos.Right | Pos.CenterV;
			label11.SetSize(110, 18);
			label11.SetPosition(542, Y(468));
		}
		long num11 = ((num10 > 0) ? (num10 - num9) : 0);
		MoneyRow("Profit", 486, flag2 ? Strings.Workshop.AverageProfit : Strings.Workshop.Profit, num11, (num10 <= 0) ? Dim : ((num11 >= 0) ? Green : Red), 488, 728, num10 > 0, num10 <= 0);
		bool flag3 = !flag && num > 0 && !crafting && !_journal;
		_maxQuantity = Math.Max(1, Math.Min(999, num));
		_quantity = Math.Clamp(_quantity, 1, _maxQuantity);
		Button button = Keep(MakeButton(this, "QuantityMinus", 28, 28, "saiyan_btn"));
		button.FontSize = 13;
		button.Text = "–";
		button.SetPosition(488, Y(510));
		button.IsDisabled = !flag3;
		button.Clicked += delegate
		{
			SetQuantity(_quantity - 1);
		};
		Label label12 = Keep(MakeLabel(this, "QuantityValue", 14, flag3 ? White : Dim, _strongFont));
		label12.Text = (crafting ? count : _quantity).ToString();
		label12.TextAlign = Pos.Center;
		label12.SetSize(48, 28);
		label12.SetPosition(518, Y(510));
		_quantityLabel = (flag3 ? label12 : null);
		Button button2 = Keep(MakeButton(this, "QuantityPlus", 28, 28, "saiyan_btn"));
		button2.FontSize = 13;
		button2.Text = "+";
		button2.SetPosition(568, Y(510));
		button2.IsDisabled = !flag3;
		button2.Clicked += delegate
		{
			SetQuantity(_quantity + 1);
		};
		Button button3 = Keep(MakeButton(this, "QuantityMax", 84, 28, "saiyan_btn"));
		button3.FontSize = 10;
		button3.SetTextColor(Entry, ComponentState.Normal);
		button3.Text = Strings.Workshop.Max.ToString(Math.Max(0, Math.Min(999, num)));
		button3.SetPosition(644, Y(510));
		button3.IsDisabled = !flag3;
		button3.Clicked += delegate
		{
			SetQuantity(_maxQuantity);
		};
		Button button4 = Keep(MakeButton(this, "Craft", 244, 34, "saiyan_cta"));
		button4.Font = _ctaFont;
		button4.FontSize = 11;
		button4.SetPosition(486, Y(546));
		button4.SetTextColor(Night, ComponentState.Normal);
		button4.SetTextColor(Night, ComponentState.Hovered);
		button4.SetTextColor(Night, ComponentState.Active);
		button4.SetTextColor(Dim, ComponentState.Disabled);
		bool flag4 = !crafting && Timing.Global.Milliseconds - _requestedAt < 1500;
		if (_journal)
		{
			button4.Text = Strings.Workshop.Journal;
			button4.IsDisabled = true;
		}
		else if (crafting || flag4)
		{
			button4.Text = string.Empty;
			button4.SetStateTexture(ComponentState.Normal, "saiyan_cta_244x34_disabled.png");
			button4.SetStateTexture(ComponentState.Hovered, "saiyan_cta_244x34_disabled.png");
			button4.Clicked += delegate
			{
				if (Artisanat.Progression(me.Id, out var _, out var _, out var _) && Timing.Global.Milliseconds - _requestedAt >= 600)
				{
					PacketSender.SendCraftItem(default(Guid), 0);
					_requestedAt = -10000L;
				}
			};
			_ctaFill = Keep(new ImagePanel(this, "CraftFill")
			{
				Texture = Globals.ContentManager.GetTexture(TextureType.Gui, "saiyan_cta_244x34.png"),
				MouseInputEnabled = false
			});
			_ctaFill.SetPosition(486, Y(546));
			_ctaFill.SetSize(1, 34);
			_ctaFill.IsHidden = true;
			_ctaLabel = Keep(MakeLabel(this, "CraftProgress", 11, White, _ctaFont));
			_ctaLabel.Text = Strings.Workshop.CraftingProgress.ToString(Math.Min(index + 1, Math.Max(1, count)), Math.Max(1, count));
			_ctaLabel.TextAlign = Pos.Center;
			_ctaLabel.SetSize(244, 34);
			_ctaLabel.SetPosition(486, Y(546));
		}
		else if (flag)
		{
			button4.Text = Strings.Workshop.LevelRequired.ToString(Math.Max(1, craftingRecipeDescriptor.ProfessionLevel));
			button4.IsDisabled = true;
		}
		else if (num <= 0)
		{
			button4.Text = Strings.Workshop.MissingIngredients;
			button4.IsDisabled = true;
		}
		else
		{
			_craftButton = button4;
			button4.Text = CraftText();
			Guid id = craftingRecipeDescriptor.Id;
			button4.Clicked += delegate
			{
				PacketSender.SendCraftItem(id, _quantity);
				_requestedAt = Timing.Global.Milliseconds;
			};
		}
	}

	private void SetQuantity(int value)
	{
		_quantity = Math.Clamp(value, 1, _maxQuantity);
		if (_quantityLabel != null)
		{
			_quantityLabel.Text = _quantity.ToString();
		}
		if (_craftButton != null)
		{
			_craftButton.Text = CraftText();
		}
	}

	private string CraftText()
	{
		if (_quantity <= 1)
		{
			return Strings.Workshop.Craft.ToString();
		}
		return Strings.Workshop.CraftMany.ToString(_quantity);
	}

	private static string Fit(string text, IFont? font, int size, int width)
	{
		GameRenderer renderer = Graphics.Renderer;
		if (renderer == null || font == null || string.IsNullOrEmpty(text) || renderer.MeasureText(text, font, size, 1f).X <= (float)width)
		{
			return text;
		}
		for (int num = text.Length - 1; num > 0; num--)
		{
			string text2 = text.Substring(0, num).TrimEnd() + "…";
			if (renderer.MeasureText(text2, font, size, 1f).X <= (float)width)
			{
				return text2;
			}
		}
		return "…";
	}

	private static IEnumerable<string> Effects(ItemDescriptor item)
	{
		if (item.ItemType == ItemType.Consumable)
		{
			ConsumableData consumable = item.Consumable;
			if (consumable != null && consumable.Value > 0)
			{
				if (consumable.Type == ConsumableType.Health)
				{
					yield return Strings.Workshop.Heals.ToString(consumable.Value);
				}
				else if (consumable.Type == ConsumableType.Mana)
				{
					yield return Strings.Workshop.RestoresKi.ToString(consumable.Value);
				}
			}
		}
		if (item.ItemType == ItemType.Bag && item.SlotCount > 0)
		{
			yield return Strings.Workshop.Bag.ToString(item.SlotCount);
		}
		if (string.IsNullOrWhiteSpace(item.Description))
		{
			yield break;
		}
		foreach (string item2 in item.Description.Split(new char[2] { '\n', '.' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Take(2))
		{
			yield return item2;
		}
	}

	private static long PriceOf(Guid itemId)
	{
		if (MarketPrices.TryGet(itemId, out MarketPriceInfo info) && info.Median > 0)
		{
			return info.Median;
		}
		return DatabaseObject<ItemDescriptor>.Get(itemId)?.Price ?? 0;
	}

	private void MoneyRow(string name, int y, string label, long value, Color color, int left, int right, bool signed = false, bool noValue = false)
	{
		Label label2 = Keep(MakeLabel(this, name + "Key", 10, signed ? White : Muted, signed ? _font : _mediumFont));
		label2.Text = label;
		label2.SetSize(140, 18);
		label2.SetPosition(left, Y(y));
		Label label3 = Keep(MakeLabel(this, name + "Value", 12, color, _strongFont));
		label3.Text = (noValue ? "—" : (signed ? (((value >= 0) ? "+" : "–") + Number(Math.Abs(value))) : Number(value)));
		label3.TextAlign = Pos.Right | Pos.CenterV;
		label3.SetSize(70, 18);
		label3.SetPosition(right - 90, Y(y));
		string text = DatabaseObject<ItemDescriptor>.Lookup.Values.OfType<ItemDescriptor>().FirstOrDefault((ItemDescriptor entry) => entry.ItemType == ItemType.Currency)?.Icon;
		ImagePanel imagePanel = Keep(new ImagePanel(this, name + "Coin")
		{
			Texture = (string.IsNullOrEmpty(text) ? null : Globals.ContentManager.GetTexture(TextureType.Item, text)),
			MouseInputEnabled = false
		});
		imagePanel.SetSize(16, 16);
		imagePanel.SetPosition(right - 16, Y(y + 1));
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

	private T Keep<T>(T control) where T : Intersect.Client.Framework.Gwen.Control.Base
	{
		_built.Add(control);
		return control;
	}

	private void KeepIfTop(Intersect.Client.Framework.Gwen.Control.Base parent, Intersect.Client.Framework.Gwen.Control.Base control)
	{
		if (parent == this)
		{
			_built.Add(control);
		}
	}

	private void Veil(string name, int x, int y, int width, int height)
	{
		ImagePanel imagePanel = Keep(new ImagePanel(this, name)
		{
			Texture = Globals.ContentManager.GetTexture(TextureType.Gui, "hdv_ligne_survol.png"),
			TextureNinePatchMargin = new Margin(6),
			MouseInputEnabled = false
		});
		imagePanel.SetSize(width, height);
		imagePanel.SetPosition(x, Y(y));
	}

	private void Hit(string name, int x, int y, int width, int height, Action action)
	{
		Button button = Keep(new Button(this, name, disableText: true)
		{
			ShouldDrawBackground = false
		});
		button.SetSize(width, height);
		button.SetPosition(x, y);
		button.Clicked += delegate
		{
			action();
		};
	}

	private new void Tooltip(Intersect.Client.Framework.Gwen.Control.Base control, ItemDescriptor? item)
	{
		if (item != null)
		{
			control.MouseInputEnabled = true;
			control.HoverEnter += delegate
			{
				Interface.GameUi?.ItemDescriptionWindow?.Show(item, 1);
				_tooltipShown = true;
			};
			control.HoverLeave += delegate
			{
				Interface.GameUi?.ItemDescriptionWindow?.Hide();
				_tooltipShown = false;
			};
		}
	}

	private ImagePanel Slot(Intersect.Client.Framework.Gwen.Control.Base parent, string name, int x, int y, string icon, bool selected, bool locked, bool keep = true, int size = 42)
	{
		ImagePanel imagePanel = new ImagePanel(parent, name)
		{
			Texture = Globals.ContentManager.GetTexture(TextureType.Gui, selected ? "saiyan_slot_selected.png" : "saiyan_slot.png"),
			TextureNinePatchMargin = new Margin(6),
			MouseInputEnabled = false
		};
		imagePanel.SetSize(size, size);
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
		imagePanel2.SetPosition((size - 32) / 2, (size - 32) / 2);
		if (locked)
		{
			imagePanel2.RenderColor = new Color(90, 255, 255, 255);
			ImagePanel imagePanel3 = new ImagePanel(imagePanel, "Locked")
			{
				Texture = Globals.ContentManager.GetTexture(TextureType.Gui, "saiyan_slot_locked.png"),
				MouseInputEnabled = false
			};
			imagePanel3.SetSize(size, size);
			imagePanel3.SetPosition(0, 0);
		}
		return imagePanel;
	}

	private void Bar(Intersect.Client.Framework.Gwen.Control.Base parent, string name, int x, int y, int width, int height, double ratio, bool keep = true)
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

	private void Rule(string name, int x, int y, int width)
	{
		ImagePanel imagePanel = Keep(new ImagePanel(this, name)
		{
			Texture = Globals.ContentManager.GetTexture(TextureType.Gui, "hdv_filet.png"),
			MouseInputEnabled = false
		});
		imagePanel.SetSize(width, 1);
		imagePanel.SetPosition(x, y);
	}

	private Button MakeButton(Intersect.Client.Framework.Gwen.Control.Base parent, string name, int width, int height, string family)
	{
		Button button = new Button(parent, name)
		{
			Font = _font,
			FontSize = 11,
			TextAlign = Pos.Center
		};
		string text = $"{family}_{width}x{height}";
		button.SetStateTexture(ComponentState.Normal, text + ".png");
		button.SetStateTexture(ComponentState.Hovered, text + "_hover.png");
		button.SetStateTexture(ComponentState.Active, text + "_clicked.png");
		button.SetStateTexture(ComponentState.Disabled, text + "_disabled.png");
		button.SetTextColor(White, ComponentState.Normal);
		button.SetTextColor(Orange, ComponentState.Hovered);
		button.SetTextColor(Night, ComponentState.Active);
		button.SetTextColor(Dim, ComponentState.Disabled);
		button.SetSize(width, height);
		return button;
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

	private static ImagePanel MakeCard(Intersect.Client.Framework.Gwen.Control.Base parent, string name)
	{
		return new ImagePanel(parent, name)
		{
			Texture = Globals.ContentManager.GetTexture(TextureType.Gui, "saiyan_card.png"),
			TextureNinePatchMargin = new Margin(8),
			MouseInputEnabled = false
		};
	}

	private static void StyleScroll(ScrollControl scroll)
	{
		scroll.EnableScroll(horizontal: false, vertical: true);
		scroll.VerticalScrollBar.SetBackgroundTemplate(null, "saiyan_scroll_bg.png");
		IGameTexture texture = Globals.ContentManager.GetTexture(TextureType.Gui, "saiyan_scroll_bar.png");
		IGameTexture texture2 = Globals.ContentManager.GetTexture(TextureType.Gui, "saiyan_scroll_bar_hover.png");
		scroll.VerticalScrollBar.SetScrollBarImage(texture, "saiyan_scroll_bar.png", ComponentState.Normal);
		scroll.VerticalScrollBar.SetScrollBarImage(texture2, "saiyan_scroll_bar_hover.png", ComponentState.Hovered);
		scroll.VerticalScrollBar.SetScrollBarImage(texture2, "saiyan_scroll_bar_hover.png", ComponentState.Active);
	}
}
