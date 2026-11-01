using System;
using System.Collections.Generic;
using System.Linq;
using Intersect.Client.Core;
using Intersect.Client.Entities;
using Intersect.Client.Entities.Events;
using Intersect.Client.Framework.Content;
using Intersect.Client.Framework.File_Management;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.Framework.Gwen;
using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.Framework.Gwen.Input;
using Intersect.Client.Framework.Gwen.Renderer;
using Intersect.Client.Framework.Gwen.Skin;
using Intersect.Client.Framework.Input;
using Intersect.Client.Framework.Items;
using Intersect.Client.General;
using Intersect.Client.Interface.Game.Dungeons;
using Intersect.Client.Interface.Game.EntityPanel;
using Intersect.Client.Interface.Game.Techniques;
using Intersect.Client.Interface.Shared;
using Intersect.Client.Localization;
using Intersect.Client.Maps;
using Intersect.Client.Networking;
using Intersect.Framework.Core;
using Intersect.Framework.Core.GameObjects.Events;
using Intersect.Framework.Core.GameObjects.Items;
using Intersect.GameObjects;
using Intersect.Localization;
using Intersect.Models;

namespace Intersect.Client.Interface.Game.Shop;

public class ShopWindow : WindowControl
{
	private readonly record struct Zone(Rectangle Rect, string Cle, Action Action);

	private readonly record struct Ligne(ItemDescriptor Objet, ItemDescriptor? Monnaie, int Prix, int Place, long Possede);

	private sealed class Corps : Intersect.Client.Framework.Gwen.Control.Base
	{
		private readonly ShopWindow _boutique;

		public Corps(ShopWindow boutique)
			: base(boutique, "Corps")
		{
			_boutique = boutique;
			base.MouseInputEnabled = true;
		}

		protected override void Render(Intersect.Client.Framework.Gwen.Skin.Base skin)
		{
			base.Render(skin);
			_boutique.Dessiner(skin.Renderer, this);
		}

		protected override void OnMouseClicked(MouseButton mouseButton, Point mousePosition, bool userAction = true)
		{
			base.OnMouseClicked(mouseButton, mousePosition, userAction);
			if (mouseButton == MouseButton.Left)
			{
				_boutique.Cliquer(CanvasPosToLocal(mousePosition));
			}
		}

		protected override bool OnMouseWheeled(int delta)
		{
			_boutique._defile += ((delta <= 0) ? 1 : (-1));
			return true;
		}

		public Point Souris()
		{
			return CanvasPosToLocal(InputHandler.MousePosition);
		}
	}

	private const int WindowWidth = 480;

	private const int WindowHeight = 600;

	private new const int Margin = 16;

	private const int TopY = 72;

	private const int BottomY = 584;

	private const int BodyWidth = 448;

	private const int BodyHeight = 512;

	private const int MerchantHeight = 56;

	private const int TabsTop = 64;

	private const int TabsHeight = 34;

	private const int ListTop = 106;

	private const int RowHeight = 54;

	private const int RowGap = 4;

	private const int VisibleRows = 3;

	private const int FootHeight = 50;

	private const int CoinFrames = 4;

	private const int CoinRestMs = 1600;

	private const int CoinFrameMs = 90;

	private static readonly Color White = new Color(255, 242, 242, 242);

	private static readonly Color Entry = new Color(255, 196, 204, 220);

	private static readonly Color Dark = new Color(255, 20, 13, 2);

	private static ShopWindow? _ouverte;

	private readonly Canvas _gameCanvas;

	private readonly Label _title;

	private readonly Button _close;

	private readonly Corps _corps;

	private readonly IGameTexture? _coinAnimation;

	private readonly string _marchand = string.Empty;

	private readonly string _planche = string.Empty;

	private long _lastZeni = -1L;

	private int _laidOutTitleBarHeight = -1;

	private int _onglet;

	private int _choisi;

	private int _quantite = 1;

	private int _defile;

	private Guid _infobulle;

	private readonly HashSet<Guid> _rachats = new HashSet<Guid>();

	private List<Zone> _zones = new List<Zone>();

	private List<Zone> _zonesEnCours = new List<Zone>();

	public ShopWindow(Canvas gameCanvas)
		: base(gameCanvas, string.Empty, modal: false, "ShopWindow")
	{
		_gameCanvas = gameCanvas;
		Interface.InputBlockingComponents.Add(this);
		DisableResizing();
		SetSize(480, 600);
		SetPosition(16, Math.Max(0, (_gameCanvas.Height - 600) / 2));
		IGameTexture texture = GameContentManager.Current.GetTexture(TextureType.Gui, "saiyan_window_boutique.png");
		SetImage(texture, "saiyan_window_boutique.png", ControlState.Active);
		SetImage(texture, "saiyan_window_boutique.png", ControlState.Inactive);
		base.IsClosable = false;
		if (base.Titlebar is Titlebar titlebar)
		{
			titlebar.Label.IsHidden = true;
		}
		_title = new Label(this, "Title")
		{
			Font = GameContentManager.Current.GetFont("refuel"),
			FontSize = 20,
			AutoSizeToContents = false,
			MouseInputEnabled = false,
			TextAlign = (Pos.Left | Pos.CenterV)
		};
		_title.SetTextColor(White, ComponentState.Normal);
		_title.Text = (Globals.GameShop?.Name ?? Strings.Shop.Title.ToString()).ToUpperInvariant();
		WindowTitle.Apply(_title);
		_close = new Button(this, "Close", disableText: true);
		_close.SetStateTexture(ComponentState.Normal, "saiyan_close.png");
		_close.SetStateTexture(ComponentState.Hovered, "saiyan_close_hover.png");
		_close.SetStateTexture(ComponentState.Active, "saiyan_close_clicked.png");
		_close.Clicked += delegate
		{
			Hide();
		};
		_coinAnimation = GameContentManager.Current.GetTexture(TextureType.Item, "zeni_anime.png");
		_corps = new Corps(this);
		Player me = Globals.Me;
		if (me != null && Timing.Global.Milliseconds - me.DernierInterlocuteurA < 10000)
		{
			foreach (MapInstance item in MapInstance.Lookup.Values.OfType<MapInstance>())
			{
				if (item.LocalEntities.TryGetValue(me.DernierInterlocuteur, out Entity value) && value is Event obj)
				{
					_marchand = obj.Name ?? string.Empty;
					EventGraphic graphic = obj.Graphic;
					_planche = ((graphic != null && graphic.Type == EventGraphicType.Sprite) ? (graphic.Filename ?? string.Empty) : string.Empty);
					break;
				}
			}
		}
		_ouverte = this;
		Layout();
	}

	public static bool Rachete(Guid objet)
	{
		ShopWindow ouverte = _ouverte;
		if (ouverte != null && !ouverte.IsHidden && ouverte._onglet == 1)
		{
			return ouverte._rachats.Contains(objet);
		}
		return false;
	}

	public override void Hide()
	{
		base.Hide();
		HideTooltip();
		if (_ouverte == this)
		{
			_ouverte = null;
		}
	}

	public void DockWith(Intersect.Client.Framework.Gwen.Control.Base inventory)
	{
		int num = Math.Max(0, (_gameCanvas.Width - 480 - inventory.Width) / 2);
		SetPosition(num, Math.Max(0, (_gameCanvas.Height - 600) / 2));
		inventory.SetPosition(Math.Max(0, Math.Min(num + 480, _gameCanvas.Width - inventory.Width)), Math.Max(0, (_gameCanvas.Height - inventory.Height) / 2));
	}

	public override void Think()
	{
		base.Think();
		if (!base.IsHidden)
		{
			if (base.TitleBarBounds.Height != _laidOutTitleBarHeight)
			{
				Layout();
			}
			PlayBuySoundOnSpend();
		}
	}

	private void PlayBuySoundOnSpend()
	{
		long num = Globals.Me?.Zeni ?? 0;
		if (_lastZeni >= 0 && num < _lastZeni)
		{
			Audio.AddGameSound("buy.wav", loop: false);
		}
		_lastZeni = num;
	}

	private void Layout()
	{
		_laidOutTitleBarHeight = base.TitleBarBounds.Height;
		_title.SetSize(400, 38);
		_title.SetPosition(26, 10 - _laidOutTitleBarHeight);
		_close.SetSize(24, 24);
		_close.SetPosition(440, 16 - _laidOutTitleBarHeight);
		_corps.SetSize(448, 512);
		_corps.SetPosition(16, 72 - _laidOutTitleBarHeight);
	}

	private static void HideTooltip()
	{
		Interface.GameUi?.ItemDescriptionWindow?.Hide();
	}

	private static long Owned(ItemDescriptor? cost)
	{
		if (cost != null)
		{
			Player me = Globals.Me;
			if (me != null)
			{
				if (cost.ItemType != ItemType.Currency)
				{
					return me.GetQuantityOfItemInInventory(cost.Id);
				}
				return me.Zeni;
			}
		}
		return 0L;
	}

	private static long DansLeSac(Player moi, Guid objet)
	{
		long num = 0L;
		for (int i = 0; i < moi.Inventory.Length; i++)
		{
			IItem item = moi.Inventory[i];
			if (item != null && item.ItemId == objet && Array.IndexOf(moi.MyEquipment, i) < 0)
			{
				num += Math.Max(1, item.Quantity);
			}
		}
		return num;
	}

	private static List<Ligne> AVendre(ShopDescriptor boutique, Player moi)
	{
		List<Ligne> list = new List<Ligne>();
		for (int i = 0; i < boutique.SellingItems.Count; i++)
		{
			ShopItemDescriptor shopItemDescriptor = boutique.SellingItems[i];
			if (DatabaseObject<ItemDescriptor>.TryGet(shopItemDescriptor.ItemId, out var databaseObject))
			{
				list.Add(new Ligne(databaseObject, DatabaseObject<ItemDescriptor>.Get(shopItemDescriptor.CostItemId), shopItemDescriptor.CostItemQuantity, i, DansLeSac(moi, databaseObject.Id)));
			}
		}
		return list;
	}

	private static List<Ligne> ARacheter(ShopDescriptor boutique, Player moi)
	{
		List<Ligne> list = new List<Ligne>();
		HashSet<Guid> hashSet = new HashSet<Guid>();
		for (int i = 0; i < moi.Inventory.Length; i++)
		{
			IItem item = moi.Inventory[i];
			if (item != null && !(item.ItemId == Guid.Empty) && Array.IndexOf(moi.MyEquipment, i) < 0 && hashSet.Add(item.ItemId) && DatabaseObject<ItemDescriptor>.TryGet(item.ItemId, out var objet) && objet.CanSell)
			{
				ShopItemDescriptor shopItemDescriptor = boutique.BuyingItems.FirstOrDefault((ShopItemDescriptor achat) => achat.ItemId == objet.Id);
				if (!(boutique.BuyingWhitelist ? (shopItemDescriptor == null) : (shopItemDescriptor != null)))
				{
					list.Add((shopItemDescriptor != null) ? new Ligne(objet, DatabaseObject<ItemDescriptor>.Get(shopItemDescriptor.CostItemId), shopItemDescriptor.CostItemQuantity, i, DansLeSac(moi, objet.Id)) : new Ligne(objet, DatabaseObject<ItemDescriptor>.Get(boutique.DefaultCurrencyId), objet.Price, i, DansLeSac(moi, objet.Id)));
				}
			}
		}
		return list;
	}

	private static (int Maximum, bool TropCher, bool SacPlein) Plafond(Ligne ligne, Player moi)
	{
		int num = ((!ligne.Objet.IsStackable) ? 1 : Math.Max(1, ligne.Objet.MaxInventoryStack));
		int num2 = Math.Max(0, Player.CapaciteDuSac - moi.ObjetsDansLeSac());
		long num3 = (long)num2 * (long)num;
		if (ligne.Objet.IsStackable)
		{
			for (int i = 0; i < moi.Inventory.Length; i++)
			{
				IItem item = moi.Inventory[i];
				if (item != null && item.ItemId == ligne.Objet.Id && Array.IndexOf(moi.MyEquipment, i) < 0)
				{
					num3 += Math.Max(0, num - item.Quantity);
				}
			}
		}
		long num4 = ((ligne.Prix >= 1) ? (Owned(ligne.Monnaie) / ligne.Prix) : long.MaxValue);
		long num5 = Math.Min(num4, num3);
		if (!ligne.Objet.IsStackable)
		{
			num5 = Math.Min(num5, 1L);
		}
		return (Maximum: (int)Math.Clamp(num5, 0L, 999L), TropCher: num4 < 1, SacPlein: num3 < 1);
	}

	private static string Classification(ItemDescriptor? descriptor)
	{
		if (descriptor == null)
		{
			return string.Empty;
		}
		List<string> list = new List<string>();
		if (Strings.ItemDescription.ItemTypes.TryGetValue((int)descriptor.ItemType, out LocalizedString value) && value.ToString().Length > 0)
		{
			list.Add(value);
		}
		if (descriptor.Rarity > 0 && Options.Instance.Items.TryGetRarityName(descriptor.Rarity, out string rarityName) && Strings.ItemDescription.Rarity.TryGetValue(rarityName, out LocalizedString value2))
		{
			list.Add(value2);
		}
		return string.Join(Strings.ItemDescription.ClassificationSeparator, list);
	}

	private static Color RarityColor(ItemDescriptor? descriptor)
	{
		if (descriptor != null && CustomColors.Items.Rarities.TryGetValue(descriptor.Rarity, out var value) && value != null)
		{
			return value;
		}
		return Entry;
	}

	private static string FormatAmount(long value)
	{
		return MarketWindow.FormatAmount(value);
	}

	private void Zoner(Rectangle rect, string cle, Action action)
	{
		_zonesEnCours.Add(new Zone(rect, cle, action));
	}

	private bool Survole(string cle)
	{
		if (!_corps.IsHovered)
		{
			return false;
		}
		Point point = _corps.Souris();
		for (int num = _zones.Count - 1; num >= 0; num--)
		{
			if (_zones[num].Rect.Contains(point.X, point.Y))
			{
				return _zones[num].Cle == cle;
			}
		}
		return false;
	}

	private void Cliquer(Point local)
	{
		for (int num = _zones.Count - 1; num >= 0; num--)
		{
			if (_zones[num].Rect.Contains(local.X, local.Y))
			{
				Audio.AddGameSound("menu_click.wav", loop: false);
				_zones[num].Action();
				break;
			}
		}
	}

	private void Choisir(int index)
	{
		_choisi = index;
		_quantite = 1;
	}

	private void Dessiner(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Corps corps)
	{
		_zonesEnCours = new List<Zone>();
		_rachats.Clear();
		ShopDescriptor gameShop = Globals.GameShop;
		if (gameShop != null)
		{
			Player me = Globals.Me;
			if (me != null)
			{
				List<Ligne> list = AVendre(gameShop, me);
				List<Ligne> list2 = ARacheter(gameShop, me);
				if (_onglet == 1)
				{
					foreach (Ligne item in list2)
					{
						_rachats.Add(item.Objet.Id);
					}
				}
				List<Ligne> list3 = ((_onglet == 0) ? list : list2);
				_choisi = Math.Clamp(_choisi, 0, Math.Max(0, list3.Count - 1));
				DessinerLeMarchand(renderer, gameShop, list.Count);
				DessinerLesOnglets(renderer, list.Count, list2.Count);
				int num = 106;
				Guid guid = Guid.Empty;
				if (list3.Count > 1)
				{
					int num2 = Math.Min(3, list3.Count);
					_defile = Math.Clamp(_defile, 0, list3.Count - num2);
					for (int i = 0; i < num2; i++)
					{
						int index = _defile + i;
						Ligne ligne = list3[index];
						Rectangle rect = new Rectangle(0, num + i * 58, 448, 54);
						if (DessinerLaLigne(renderer, rect, ligne, index))
						{
							guid = ligne.Objet.Id;
						}
					}
					if (list3.Count > num2)
					{
						Rectangle rect2 = new Rectangle(445, num, 3, num2 * 58 - 4);
						TechniqueArt.Fill(renderer, rect2, TechniqueArt.Alpha(TechniqueArt.White, 0.05f));
						int num3 = Math.Max(18, rect2.Height * num2 / list3.Count);
						TechniqueArt.Fill(renderer, new Rectangle(rect2.X, rect2.Y + (rect2.Height - num3) * _defile / (list3.Count - num2), 3, num3), TechniqueArt.Line2);
					}
					num += num2 * 58 + 4;
				}
				int num4 = 454;
				Rectangle rect3 = new Rectangle(0, num, 448, num4 - num);
				if (list3.Count == 0)
				{
					TechniqueArt.Fill(renderer, rect3, TechniqueArt.Alpha(TechniqueArt.Black, 0.25f));
					TechniqueArt.Outline(renderer, rect3, TechniqueArt.Line);
					string text = ((_onglet == 0) ? Strings.Shop.Empty.ToString() : ((gameShop.BuyingWhitelist && gameShop.BuyingItems.Count == 0) ? Strings.Boutique.BuysNothing : Strings.Boutique.NothingToSell).ToString());
					TechniqueArt.TextCenter(renderer, TechniqueArt.Label, 11, text, rect3.X + rect3.Width / 2, rect3.Y + rect3.Height / 2, TechniqueArt.Soft);
				}
				else if (DessinerLaFiche(renderer, rect3, list3[_choisi], me))
				{
					guid = list3[_choisi].Objet.Id;
				}
				DessinerLePied(renderer, me);
				if (guid != _infobulle)
				{
					_infobulle = guid;
					if (guid != Guid.Empty && DatabaseObject<ItemDescriptor>.TryGet(guid, out var databaseObject))
					{
						Interface.GameUi.ItemDescriptionWindow?.Show(databaseObject, 1);
					}
					else
					{
						HideTooltip();
					}
				}
				_zones = _zonesEnCours;
				return;
			}
		}
		_zones = _zonesEnCours;
	}

	private void DessinerLeMarchand(Intersect.Client.Framework.Gwen.Renderer.Base renderer, ShopDescriptor boutique, int articles)
	{
		Rectangle rect = new Rectangle(0, 0, 448, 56);
		TechniqueArt.FillHorizontal(renderer, rect, TechniqueArt.Alpha(TechniqueArt.Ki, 0.12f), TechniqueArt.Alpha(TechniqueArt.Ki, 0.04f), TechniqueArt.Alpha(TechniqueArt.Ki, 0f));
		TechniqueArt.Outline(renderer, rect, TechniqueArt.Line);
		Rectangle rect2 = new Rectangle(8, 6, 44, 44);
		TechniqueArt.Fill(renderer, rect2, new Color(255, 10, 18, 32));
		TechniqueArt.Outline(renderer, rect2, TechniqueArt.Line2);
		(IGameTexture, float, float, float, float)? tuple = DungeonEntryWindow.StandingFrame(_planche);
		if (tuple.HasValue)
		{
			(IGameTexture, float, float, float, float) valueOrDefault = tuple.GetValueOrDefault();
			(IGameTexture, float, float, float, float) tuple2 = valueOrDefault;
			IGameTexture item = tuple2.Item1;
			float item2 = tuple2.Item2;
			float item3 = tuple2.Item3;
			float item4 = tuple2.Item4;
			float item5 = tuple2.Item5;
			float num = item4 * 0.56f;
			float num2 = (item2 + item4 * 0.22f) / (float)item.Width;
			float num3 = (item3 + ((item5 > item4) ? (item5 * 0.33f) : (item5 * 0.1f))) / (float)item.Height;
			SaiyanHud.Draw(renderer, item, rect2.X + 1, rect2.Y + 1, rect2.Width - 2, rect2.Height - 2, TechniqueArt.White, num2, num3, num2 + num / (float)item.Width, num3 + num / (float)item.Height);
		}
		else
		{
			Piece(renderer, rect2.X + 8, rect2.Y + 8, 28, animee: false);
		}
		string text = ((_planche.Length > 0 && _marchand.Length > 0) ? _marchand : Strings.Boutique.Counter.ToString());
		int width = 374;
		TechniqueArt.TextMiddle(renderer, TechniqueArt.Heavy, 15, TechniqueArt.Wrap(renderer, TechniqueArt.Heavy, 15, text.ToUpperInvariant(), width, 1).FirstOrDefault(), 64, 19, TechniqueArt.Text);
		string text2 = ((!boutique.BuyingWhitelist) ? Strings.Boutique.SummaryBuysAll.ToString() : (boutique.BuyingItems.Count switch
		{
			0 => Strings.Boutique.SummaryBuysNothing.ToString(), 
			1 => Strings.Boutique.SummaryBuysOne.ToString(), 
			_ => Strings.Boutique.SummaryBuys.ToString(boutique.BuyingItems.Count), 
		}));
		string text3 = text2;
		string text4 = Strings.Boutique.SummarySells.ToString(articles) + " · " + text3;
		TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, 10, TechniqueArt.Wrap(renderer, TechniqueArt.Body, 10, text4, width, 1).FirstOrDefault(), 64, 39, TechniqueArt.Soft);
	}

	private void DessinerLesOnglets(Intersect.Client.Framework.Gwen.Renderer.Base renderer, int articles, int rachats)
	{
		int num = 224;
		for (int i = 0; i < 2; i++)
		{
			Rectangle rect = new Rectangle(i * num, 64, (i == 0) ? num : (448 - num), 34);
			bool flag = _onglet == i;
			string cle = $"onglet-{i}";
			if (flag)
			{
				TechniqueArt.FillVertical(renderer, rect, new Color(255, 255, 227, 154), new Color(255, 216, 140, 28));
			}
			else
			{
				TechniqueArt.Fill(renderer, rect, new Color(204, 6, 8, 14));
				TechniqueArt.Outline(renderer, rect, Survole(cle) ? TechniqueArt.Soft : TechniqueArt.Line2);
			}
			string text = $"{((i == 0) ? Strings.Boutique.TabBuy : Strings.Boutique.TabSell).ToString().ToUpperInvariant()}  {((i == 0) ? articles : rachats)}";
			TechniqueArt.SpacedCenter(renderer, TechniqueArt.Heavy, 12, text, rect.X + rect.Width / 2, rect.Y + rect.Height / 2, 1f, flag ? Dark : (Survole(cle) ? TechniqueArt.Text : TechniqueArt.Soft));
			int choix = i;
			Zoner(rect, cle, delegate
			{
				_onglet = choix;
				_choisi = 0;
				_quantite = 1;
				_defile = 0;
			});
		}
	}

	private static void Case(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Rectangle rect, ItemDescriptor objet)
	{
		Color color = RarityColor(objet);
		TechniqueArt.Fill(renderer, rect, new Color(255, 10, 18, 32));
		TechniqueArt.Glow(renderer, (float)rect.X + (float)rect.Width / 2f, (float)rect.Y + (float)rect.Height / 2f, (float)rect.Width * 0.62f, TechniqueArt.Alpha(color, 0.16f));
		TechniqueArt.Outline(renderer, rect, TechniqueArt.Mix(color, TechniqueArt.Line, 0.6f));
		IGameTexture texture = GameContentManager.Current.GetTexture(TextureType.Item, objet.Icon);
		int num = Math.Max(32, (rect.Width - 8) / 32 * 32);
		SaiyanHud.Draw(renderer, texture, rect.X + (rect.Width - num) / 2, rect.Y + (rect.Height - num) / 2, num, num, objet.Color ?? TechniqueArt.White);
	}

	private void Piece(Intersect.Client.Framework.Gwen.Renderer.Base renderer, int x, int y, int cote, bool animee, ItemDescriptor? monnaie = null)
	{
		if (monnaie != null && monnaie.ItemType != ItemType.Currency)
		{
			SaiyanHud.Draw(renderer, GameContentManager.Current.GetTexture(TextureType.Item, monnaie.Icon), x, y, cote, cote, TechniqueArt.White);
			return;
		}
		IGameTexture coinAnimation = _coinAnimation;
		if (coinAnimation != null && coinAnimation.Width > 0)
		{
			int num = 0;
			if (animee)
			{
				int num2 = 1870;
				long num3 = Timing.Global.Milliseconds % num2;
				num = ((num3 >= 1600) ? Math.Min(3, 1 + (int)((num3 - 1600) / 90)) : 0);
			}
			SaiyanHud.Draw(renderer, coinAnimation, x, y, cote, cote, TechniqueArt.White, (float)num / 4f, 0f, (float)(num + 1) / 4f);
		}
	}

	private int Prix(Intersect.Client.Framework.Gwen.Renderer.Base renderer, int droite, int milieu, long montant, ItemDescriptor? monnaie, Color couleur, int corps)
	{
		string text = FormatAmount(montant);
		int num = TechniqueArt.TextRight(renderer, TechniqueArt.Heavy, corps, text, droite, milieu, couleur);
		int num2 = corps + 5;
		Piece(renderer, droite - num - 5 - num2, milieu - num2 / 2, num2, animee: false, monnaie);
		return num + 5 + num2;
	}

	private bool DessinerLaLigne(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Rectangle rect, Ligne ligne, int index)
	{
		string cle = $"ligne-{index}";
		bool flag = index == _choisi;
		bool flag2 = Survole(cle);
		if (flag)
		{
			TechniqueArt.FillHorizontal(renderer, rect, TechniqueArt.Alpha(TechniqueArt.Ki, 0.16f), TechniqueArt.Alpha(TechniqueArt.Ki, 0.07f), TechniqueArt.Alpha(TechniqueArt.Ki, 0.03f));
			TechniqueArt.Outline(renderer, rect, TechniqueArt.Ki);
			TechniqueArt.Fill(renderer, new Rectangle(rect.X, rect.Y, 3, rect.Height), TechniqueArt.Ki);
		}
		else
		{
			TechniqueArt.Fill(renderer, rect, TechniqueArt.Alpha(TechniqueArt.White, flag2 ? 0.05f : 0.025f));
			TechniqueArt.Outline(renderer, rect, flag2 ? TechniqueArt.Line2 : TechniqueArt.Line);
		}
		Rectangle rect2 = new Rectangle(rect.X + 7, rect.Y + (rect.Height - 42) / 2, 42, 42);
		Case(renderer, rect2, ligne.Objet);
		bool flag3 = _onglet == 1;
		bool flag4 = !flag3 && ligne.Prix > Owned(ligne.Monnaie);
		int milieu = rect.Y + rect.Height / 2;
		int num = Prix(renderer, rect.Right - 12, milieu, ligne.Prix, ligne.Monnaie, flag3 ? TechniqueArt.Green : (flag4 ? TechniqueArt.Red : TechniqueArt.Gold), 13);
		int num2 = rect2.Right + 12;
		int width = rect.Right - 12 - num - 12 - num2;
		TechniqueArt.TextMiddle(renderer, TechniqueArt.Label, 11, TechniqueArt.Wrap(renderer, TechniqueArt.Label, 11, ligne.Objet.Name, width, 1).FirstOrDefault(), num2, rect.Y + 19, RarityColor(ligne.Objet));
		string text = (flag3 ? Strings.Boutique.YouHave.ToString(FormatAmount(ligne.Possede)) : ((ligne.Possede > 0) ? (Classification(ligne.Objet) + " · " + Strings.Boutique.YouHave.ToString(FormatAmount(ligne.Possede)).ToLowerInvariant()) : Classification(ligne.Objet)));
		TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, 9, TechniqueArt.Wrap(renderer, TechniqueArt.Body, 9, text, width, 1).FirstOrDefault(), num2, rect.Y + 37, TechniqueArt.Soft);
		Zoner(rect, cle, delegate
		{
			Choisir(index);
		});
		Point point = _corps.Souris();
		if (_corps.IsHovered)
		{
			return rect2.Contains(point.X, point.Y);
		}
		return false;
	}

	private bool DessinerLaFiche(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Rectangle rect, Ligne ligne, Player moi)
	{
		Color color = RarityColor(ligne.Objet);
		TechniqueArt.Fill(renderer, rect, TechniqueArt.Alpha(TechniqueArt.Black, 0.25f));
		TechniqueArt.Glow(renderer, rect.X + 60, rect.Y + 56, 150f, TechniqueArt.Alpha(color, 0.1f));
		TechniqueArt.Outline(renderer, rect, TechniqueArt.Line);
		bool vendre = _onglet == 1;
		int maximum;
		bool flag;
		bool flag2;
		if (!vendre)
		{
			(maximum, flag, flag2) = Plafond(ligne, moi);
		}
		else
		{
			int num = (int)Math.Clamp(ligne.Possede, 0L, 999L);
			flag2 = false;
			flag = false;
			maximum = num;
		}
		_quantite = Math.Clamp(_quantite, 1, Math.Max(1, maximum));
		int num2 = rect.Bottom - 14 - 36;
		int num3 = num2 - 10;
		TechniqueArt.Fill(renderer, new Rectangle(rect.X + 14, num3, rect.Width - 28, 1), TechniqueArt.Line);
		if (!vendre && (flag || flag2))
		{
			Rectangle rect2 = new Rectangle(rect.X + 14, num2, rect.Width - 28, 36);
			TechniqueArt.Fill(renderer, rect2, TechniqueArt.Alpha(TechniqueArt.Red, 0.08f));
			TechniqueArt.Outline(renderer, rect2, TechniqueArt.Alpha(TechniqueArt.Red, 0.4f));
			string text = (flag ? Strings.Boutique.Missing.ToString(FormatAmount(ligne.Prix - Owned(ligne.Monnaie)), ligne.Monnaie?.Name ?? string.Empty) : Strings.Boutique.BagFull.ToString());
			TechniqueArt.TextCenter(renderer, TechniqueArt.Label, 11, text, rect2.X + rect2.Width / 2, rect2.Y + rect2.Height / 2, TechniqueArt.Red);
		}
		else
		{
			int num4 = rect.X + 14;
			Pas(renderer, new Rectangle(num4, num2, 30, 36), "moins", "-", delegate
			{
				_quantite = Math.Max(1, _quantite - 1);
			});
			Rectangle rect3 = new Rectangle(num4 + 30, num2, 46, 36);
			TechniqueArt.Fill(renderer, rect3, new Color(255, 10, 18, 32));
			TechniqueArt.Outline(renderer, rect3, TechniqueArt.Line2);
			TechniqueArt.TextCenter(renderer, TechniqueArt.Heavy, 15, $"{_quantite}", rect3.X + rect3.Width / 2, rect3.Y + rect3.Height / 2, TechniqueArt.Text);
			Pas(renderer, new Rectangle(num4 + 76, num2, 30, 36), "plus", "+", delegate
			{
				_quantite = Math.Min(Math.Max(1, maximum), _quantite + 1);
			});
			string text2 = Strings.Boutique.Max.ToString().ToUpperInvariant();
			int w = TechniqueArt.Measure(renderer, TechniqueArt.Label, 9, text2).X + 18;
			Pas(renderer, new Rectangle(num4 + 112, num2, w, 36), "max", text2, delegate
			{
				_quantite = Math.Max(1, maximum);
			}, 9);
			Rectangle rect4 = new Rectangle(rect.Right - 14 - 132, num2, 132, 36);
			bool flag3 = Survole("agir");
			TechniqueArt.FillVertical(renderer, rect4, flag3 ? new Color(255, 255, 238, 190) : new Color(255, 255, 227, 154), new Color(255, 216, 140, 28));
			string text3 = (vendre ? Strings.Boutique.TabSell : Strings.Boutique.TabBuy).ToString().ToUpperInvariant();
			TechniqueArt.SpacedCenter(renderer, TechniqueArt.Heavy, 13, text3, rect4.X + rect4.Width / 2, rect4.Y + rect4.Height / 2, 1.4f, Dark);
			int place = ligne.Place;
			Zoner(rect4, "agir", delegate
			{
				if (vendre)
				{
					PacketSender.SendSellItem(place, _quantite);
				}
				else
				{
					PacketSender.SendBuyItem(place, _quantite);
				}
				_quantite = 1;
			});
			long montant = (long)ligne.Prix * (long)_quantite;
			TechniqueArt.SpacedRight(renderer, TechniqueArt.Label, 8, (vendre ? Strings.Boutique.YouGet : Strings.Boutique.Total).ToString().ToUpperInvariant(), rect4.X - 12, num2 + 7, 1.2f, TechniqueArt.Dim);
			Prix(renderer, rect4.X - 12, num2 + 24, montant, ligne.Monnaie, vendre ? TechniqueArt.Green : TechniqueArt.Gold, 14);
		}
		int num5 = num3 - 8 - (rect.Y + 14);
		int num6 = ((num5 >= 200) ? 136 : ((num5 >= 120) ? 104 : ((num5 >= 72) ? 72 : 40)));
		Rectangle rect5 = new Rectangle(rect.X + 14, rect.Y + 14, num6, num6);
		Case(renderer, rect5, ligne.Objet);
		int num7 = rect5.Right + 14;
		int width = rect.Right - 14 - num7;
		int num8 = rect.Y + 14 + 11;
		TechniqueArt.TextMiddle(renderer, TechniqueArt.Heavy, 18, TechniqueArt.Wrap(renderer, TechniqueArt.Heavy, 18, ligne.Objet.Name, width, 1).FirstOrDefault(), num7, num8, color);
		num8 += 20;
		TechniqueArt.Spaced(renderer, TechniqueArt.Label, 8, Classification(ligne.Objet).ToUpperInvariant(), num7, num8, 1.2f, TechniqueArt.Soft);
		num8 += 17;
		int num9 = num3 - 8;
		string text4 = ligne.Objet.Description ?? string.Empty;
		int val = Math.Max(0, (num9 - num8 + 7) / 15);
		List<string> list = TechniqueArt.Wrap(renderer, TechniqueArt.Body, 10, text4, width, Math.Min(8, val));
		foreach (string item in list)
		{
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, 10, item, num7, num8, Entry);
			num8 += 15;
		}
		if (num8 + 14 <= num9)
		{
			string text5 = ((ligne.Possede > 0) ? Strings.Boutique.InYourBag.ToString(FormatAmount(ligne.Possede)) : Strings.Boutique.NotOwned.ToString());
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, 9, text5, num7, num8 + 6, TechniqueArt.Dim);
		}
		Point point = _corps.Souris();
		if (_corps.IsHovered)
		{
			return rect5.Contains(point.X, point.Y);
		}
		return false;
	}

	private void Pas(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Rectangle rect, string cle, string texte, Action action, int corps = 13)
	{
		bool flag = Survole(cle);
		TechniqueArt.Fill(renderer, rect, new Color(242, 10, 14, 24));
		TechniqueArt.Outline(renderer, rect, flag ? TechniqueArt.Soft : TechniqueArt.Line2);
		Color color = (flag ? TechniqueArt.Text : Entry);
		int num = rect.X + rect.Width / 2;
		int num2 = rect.Y + rect.Height / 2;
		if ((texte == "-" || texte == "+") ? true : false)
		{
			TechniqueArt.Fill(renderer, new Rectangle(num - 5, num2 - 1, 10, 2), color);
			if (texte == "+")
			{
				TechniqueArt.Fill(renderer, new Rectangle(num - 1, num2 - 5, 2, 10), color);
			}
		}
		else
		{
			TechniqueArt.TextCenter(renderer, TechniqueArt.Label, corps, texte, num, num2, color);
		}
		Zoner(rect, cle, action);
	}

	private void DessinerLePied(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Player moi)
	{
		Rectangle rect = new Rectangle(0, 462, 448, 50);
		TechniqueArt.Fill(renderer, rect, new Color(153, 6, 8, 14));
		TechniqueArt.Outline(renderer, rect, TechniqueArt.Line);
		int num = rect.Y + rect.Height / 2;
		Piece(renderer, rect.X + 12, num - 12, 24, animee: true);
		int num2 = TechniqueArt.TextMiddle(renderer, TechniqueArt.Heavy, 13, FormatAmount(moi.Zeni), rect.X + 44, num, TechniqueArt.Gold);
		string text = ((_onglet == 1) ? Strings.Boutique.HintSell : Strings.Shop.SellHint).ToString();
		int width = rect.Right - 12 - (rect.X + 44 + num2 + 16);
		List<string> list = TechniqueArt.Wrap(renderer, TechniqueArt.Body, 9, text, width, 2);
		for (int i = 0; i < list.Count; i++)
		{
			TechniqueArt.TextRight(renderer, TechniqueArt.Body, 9, list[i], rect.Right - 12, num - (list.Count - 1) * 7 + i * 14, TechniqueArt.Dim);
		}
	}
}
