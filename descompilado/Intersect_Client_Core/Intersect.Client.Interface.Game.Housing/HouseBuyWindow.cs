using System;
using System.Collections.Generic;
using System.Linq;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.Framework.Gwen.Renderer;
using Intersect.Client.General;
using Intersect.Client.Housing;
using Intersect.Client.Interface.Game.EntityPanel;
using Intersect.Client.Interface.Game.Techniques;
using Intersect.Client.Localization;
using Intersect.Client.Networking;
using Intersect.Framework.Core;
using Intersect.Framework.Core.GameObjects.Housing;
using Intersect.Framework.Core.GameObjects.Maps.Attributes;
using Intersect.Localization;
using Intersect.Models;
using Intersect.Network.Packets.Server;

namespace Intersect.Client.Interface.Game.Housing;

internal sealed class HouseBuyWindow : CharteScreen
{
	private TerrainArea _upgradeTerrain;

	private CapsuleDescriptor? _current;

	private const float Left = 820f;

	private const float Top = 150f;

	private const float CardWidth = 560f;

	private const int MaxRows = 4;

	private Houses.Plot _plot;

	private Guid _capsuleId;

	private long _pendingUntil;

	private int _offsetX = -1;

	private int _offsetY = -1;

	public bool IsUpgrade { get; private set; }

	private TerrainArea Terrain
	{
		get
		{
			if (!IsUpgrade)
			{
				return _plot.Terrain;
			}
			return _upgradeTerrain;
		}
	}

	private CapsuleDescriptor? Folded
	{
		get
		{
			if (!IsUpgrade)
			{
				return Houses.FoldedCapsule;
			}
			return null;
		}
	}

	private bool FoldedBlocked
	{
		get
		{
			CapsuleDescriptor folded = Folded;
			if (folded != null)
			{
				if (!Prebuilt)
				{
					return !HousingRules.CapsuleFits(folded, Terrain.Width, Terrain.Height);
				}
				return true;
			}
			return false;
		}
	}

	public override bool IsBlockingInput => !base.IsHidden;

	private bool Prebuilt => Terrain.Kind == TerrainKind.Prebuilt;

	private CapsuleDescriptor? Selected => DatabaseObject<CapsuleDescriptor>.Get(_capsuleId);

	private long Total
	{
		get
		{
			if (!IsUpgrade)
			{
				return Math.Max(0L, Terrain.Price) + ((Prebuilt || Folded != null) ? 0 : Math.Max(0L, Selected?.Price ?? 0));
			}
			if (_current == null || Selected == null)
			{
				return 0L;
			}
			return HousingRules.UpgradePrice(_current, Selected);
		}
	}

	private bool CanBuy
	{
		get
		{
			CapsuleDescriptor selected = Selected;
			if (selected != null && HousingRules.CapsuleFits(selected, Terrain.Width, Terrain.Height) && !FoldedBlocked && (!IsUpgrade || (_current != null && HousingRules.IsUpgrade(_current, selected))) && (Globals.Me?.Zeni ?? 0) >= Total)
			{
				return Timing.Global.MillisecondsUtc >= _pendingUntil;
			}
			return false;
		}
	}

	public HouseBuyWindow(Canvas gameCanvas)
		: base(gameCanvas, "HouseBuyWindow")
	{
		Interface.InputBlockingComponents.Add(this);
	}

	public void Open(Houses.Plot plot)
	{
		IsUpgrade = false;
		_current = null;
		_plot = plot;
		_pendingUntil = 0L;
		_offsetX = -1;
		_offsetY = -1;
		List<CapsuleDescriptor> source = Capsules();
		_capsuleId = ((plot.Terrain.Kind == TerrainKind.Prebuilt) ? plot.Terrain.CapsuleId : (Folded?.Id ?? source.FirstOrDefault((CapsuleDescriptor capsule) => HousingRules.CapsuleFits(capsule, plot.Terrain.Width, plot.Terrain.Height))?.Id ?? Guid.Empty));
		Show();
	}

	public void OpenUpgrade()
	{
		HouseInteriorPacket interior = Houses.Interior;
		if (interior == null || !interior.IsOwner || !interior.CanUpgrade)
		{
			return;
		}
		CapsuleDescriptor interiorCapsule = Houses.InteriorCapsule;
		if (interiorCapsule != null)
		{
			IsUpgrade = true;
			_current = interiorCapsule;
			_plot = default(Houses.Plot);
			_upgradeTerrain = new TerrainArea(Guid.Empty, interior.TerrainName, TerrainKind.Buildable, 0L, Guid.Empty, 0, 0, interior.TerrainWidth, interior.TerrainHeight);
			_pendingUntil = 0L;
			_offsetX = interior.OffsetX;
			_offsetY = interior.OffsetY;
			_capsuleId = UpgradeCapsules().FirstOrDefault((CapsuleDescriptor capsule) => HousingRules.CapsuleFits(capsule, Terrain.Width, Terrain.Height))?.Id ?? Guid.Empty;
			Show();
		}
	}

	public void Refresh()
	{
		if (!base.IsHidden && !IsUpgrade)
		{
			Houses.Plot plot = Houses.PlotsOf(_plot.Map).FirstOrDefault((Houses.Plot plot2) => plot2.Terrain.Id == Terrain.Id);
			if (plot.State != null || plot.Terrain.Id == Guid.Empty)
			{
				Hide();
			}
		}
	}

	private static List<CapsuleDescriptor> Capsules()
	{
		return (from capsule in DatabaseObject<CapsuleDescriptor>.Lookup.Values.OfType<CapsuleDescriptor>()
			orderby capsule.Price, capsule.Name
			select capsule).ToList();
	}

	private List<CapsuleDescriptor> UpgradeCapsules()
	{
		CapsuleDescriptor current = _current;
		if (current == null)
		{
			return new List<CapsuleDescriptor>();
		}
		return (from capsule in Capsules()
			where HousingRules.IsUpgrade(current, capsule)
			select capsule).ToList();
	}

	public bool TryConfirm()
	{
		if (base.IsHidden)
		{
			return false;
		}
		Buy();
		return true;
	}

	private void Buy()
	{
		if (!CanBuy)
		{
			return;
		}
		_pendingUntil = Timing.Global.MillisecondsUtc + 2000;
		if (!Prebuilt)
		{
			CapsuleDescriptor selected = Selected;
			if (selected != null)
			{
				HouseFootprint houseFootprint = HousingRules.Footprint(Terrain with
				{
					X = 0,
					Y = 0
				}, selected, _offsetX, _offsetY);
				if (IsUpgrade)
				{
					PacketSender.SendHouseUpgrade(_capsuleId, houseFootprint.X, houseFootprint.Y);
				}
				else
				{
					PacketSender.SendHouseBuy(_plot.Map.Id, Terrain.Id, _capsuleId, houseFootprint.X, houseFootprint.Y);
				}
				return;
			}
		}
		PacketSender.SendHouseBuy(_plot.Map.Id, Terrain.Id, Guid.Empty, -1, -1);
	}

	protected override void DrawScreen(Intersect.Client.Framework.Gwen.Renderer.Base renderer, long now)
	{
		Rectangle rect = new Rectangle(0, 0, base.Width, base.Height);
		TechniqueArt.Fill(renderer, rect, A(new Color(255, 4, 6, 11), 0.55f));
		TechniqueArt.Image(renderer, "tech_voile.png", rect, A(TechniqueArt.White));
		AddHit(rect, "voile", delegate
		{
		});
		object obj;
		if (!Prebuilt)
		{
			CapsuleDescriptor folded = Folded;
			obj = ((folded != null) ? new List<CapsuleDescriptor> { folded } : (IsUpgrade ? UpgradeCapsules() : Capsules()).OrderByDescending((CapsuleDescriptor capsule) => HousingRules.CapsuleFits(capsule, Terrain.Width, Terrain.Height)).Take(4).ToList());
		}
		else
		{
			obj = new List<CapsuleDescriptor>();
		}
		List<CapsuleDescriptor> list = (List<CapsuleDescriptor>)obj;
		int num = (Prebuilt ? 1 : Math.Max(1, list.Count));
		float num2 = 472f;
		float num3 = num2 + 8f + (float)(num * 64) + (float)((num - 1) * 6);
		float num4 = num3 + 10f;
		float num5 = num4 + 60f + 12f;
		float num6 = num5 + 46f + 14f;
		Rectangle rectangle = _d.R(820f, 150f, 560f, num6 - 150f);
		HouseArt.Window(renderer, rectangle, _fade);
		AddHit(rectangle, "carte", delegate
		{
		});
		HouseArt.WindowHeader(renderer, _d, rectangle, Terrain.Name, (!IsUpgrade) ? _plot.Map.Name : _current?.Name, _fade);
		DrawPreview(renderer, _d.R(838f, 220f, 524f, 190f));
		string text = Strings.Housing.BuySize.ToString(Terrain.Width, Terrain.Height);
		DrawFact(renderer, 838f, 424f, Prebuilt ? Strings.Housing.BuyHouse : Strings.Housing.BuyTerrain, (!Prebuilt) ? text : (Selected?.Name ?? string.Empty));
		if (IsUpgrade)
		{
			DrawFact(renderer, 1109f, 424f, Strings.Housing.UpgradeCurrent, _current?.Name ?? string.Empty);
		}
		else
		{
			DrawFact(renderer, 1109f, 424f, Prebuilt ? Strings.Housing.BuyPrice : Strings.Housing.BuyTerrainAlone, Strings.Housing.Amount.ToString(MarketWindow.FormatAmount(Terrain.Price)));
		}
		if (Prebuilt)
		{
			CapsuleDescriptor selected = Selected;
			if (selected != null)
			{
				DrawCapsuleRow(renderer, selected, num2 + 8f, selectable: false);
			}
		}
		else if (list.Count == 0)
		{
			LocalizedString localizedString = (IsUpgrade ? Strings.Housing.UpgradeNone : Strings.Housing.BuyNoCapsule);
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(13f), localizedString, _d.X(838f), _d.Y(num2 + 40f), A(TechniqueArt.Dim));
		}
		else
		{
			float num7 = num2 + 8f;
			foreach (CapsuleDescriptor item in list)
			{
				DrawCapsuleRow(renderer, item, num7, Folded == null);
				num7 += 70f;
			}
		}
		DrawPrice(renderer, num4);
		string text2 = Strings.Housing.BuyLater.ToString();
		float num8 = (float)TechniqueArt.Measure(renderer, TechniqueArt.Label, Pt(14f), text2).X / _d.S + 32f;
		Rectangle rect2 = _d.R(834f, num5, num8, 46f);
		HouseArt.Hollow(renderer, _d, rect2, text2, IsHot("plus-tard"), _fade);
		AddHit(rect2, "plus-tard", Hide);
		Rectangle rect3 = _d.R(834f + num8 + 10f, num5, 532f - num8 - 10f, 46f);
		bool canBuy = CanBuy;
		LocalizedString localizedString2 = (IsUpgrade ? Strings.Housing.UpgradeConfirm : ((Folded != null && !Prebuilt) ? Strings.Housing.BuyConfirmFolded : (Prebuilt ? Strings.Housing.BuyConfirmPrebuilt : Strings.Housing.BuyConfirm)));
		HouseArt.Cta(renderer, _d, rect3, localizedString2, canBuy, canBuy && IsHot("acheter"), _fade);
		AddHit(rect3, "acheter", Buy);
	}

	private void DrawFact(Intersect.Client.Framework.Gwen.Renderer.Base renderer, float x, float y, string label, string value)
	{
		TechniqueArt.Spaced(renderer, TechniqueArt.Label, Pt(12f), label.ToUpperInvariant(), _d.X(x), _d.Y(y + 6f), _d.Em(0.18f, 12f), A(TechniqueArt.Dim));
		TechniqueArt.TextMiddle(renderer, TechniqueArt.Label, Pt(16f), value, _d.X(x), _d.Y(y + 26f), A(TechniqueArt.Text));
	}

	private void DrawPreview(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Rectangle area)
	{
		TechniqueArt.Fill(renderer, area, A(new Color(255, 10, 18, 32)));
		TechniqueArt.Outline(renderer, area, A(TechniqueArt.Line2));
		TerrainArea terrain = Terrain;
		TerrainArea terrain2 = terrain with
		{
			X = 0,
			Y = 0
		};
		CapsuleDescriptor selected = Selected;
		CapsuleDescriptor capsule = ((selected != null && HousingRules.CapsuleFits(selected, terrain.Width, terrain.Height)) ? selected : null);
		int maxX;
		int maxY;
		if (capsule != null)
		{
			(maxX, maxY) = HousingRules.MaxOffsets(terrain2, capsule);
		}
		else
		{
			maxX = 0;
			maxY = 0;
		}
		bool flag = capsule != null && !Prebuilt && (maxX > 0 || maxY > 0);
		int num = (flag ? _d.L(34f) : 0);
		int num2 = Math.Max(4, Math.Min((area.Width - _d.L(40f)) / Math.Max(1, terrain.Width), (area.Height - _d.L(40f) - num) / Math.Max(1, terrain.Height)));
		int num3 = num2 * terrain.Width;
		int num4 = num2 * terrain.Height;
		int num5 = area.X + (area.Width - num3) / 2;
		int num6 = area.Y + (area.Height - num - num4) / 2 + _d.L(8f);
		TechniqueArt.Fill(renderer, new Rectangle(num5, num6, num3, num4), A(new Color(255, 255, 122, 26), 0.1f));
		TechniqueArt.Fill(renderer, new Rectangle(num5, num6 + num4 - num2, num3, num2), A(new Color(255, 62, 197, 255), 0.16f));
		for (int i = 0; i <= terrain.Width; i++)
		{
			TechniqueArt.Fill(renderer, new Rectangle(num5 + i * num2, num6, 1, num4), A(TechniqueArt.White, 0.08f));
		}
		for (int j = 0; j <= terrain.Height; j++)
		{
			TechniqueArt.Fill(renderer, new Rectangle(num5, num6 + j * num2, num3, 1), A(TechniqueArt.White, 0.08f));
		}
		if (IsUpgrade)
		{
			CapsuleDescriptor current = _current;
			if (current != null)
			{
				HouseInteriorPacket interior = Houses.Interior;
				if (interior != null && HousingRules.CapsuleFits(current, terrain.Width, terrain.Height))
				{
					HouseFootprint houseFootprint = HousingRules.Footprint(terrain2, current, interior.OffsetX, interior.OffsetY);
					Rectangle rectangle = new Rectangle(num5 + houseFootprint.X * num2, num6 + houseFootprint.Y * num2, houseFootprint.Width * num2, houseFootprint.Height * num2);
					for (int k = rectangle.X; k < rectangle.Right; k += _d.L(8f))
					{
						TechniqueArt.Fill(renderer, new Rectangle(k, rectangle.Y, Math.Min(_d.L(4f), rectangle.Right - k), 1), A(TechniqueArt.White, 0.55f));
						TechniqueArt.Fill(renderer, new Rectangle(k, rectangle.Bottom - 1, Math.Min(_d.L(4f), rectangle.Right - k), 1), A(TechniqueArt.White, 0.55f));
					}
					for (int l = rectangle.Y; l < rectangle.Bottom; l += _d.L(8f))
					{
						TechniqueArt.Fill(renderer, new Rectangle(rectangle.X, l, 1, Math.Min(_d.L(4f), rectangle.Bottom - l)), A(TechniqueArt.White, 0.55f));
						TechniqueArt.Fill(renderer, new Rectangle(rectangle.Right - 1, l, 1, Math.Min(_d.L(4f), rectangle.Bottom - l)), A(TechniqueArt.White, 0.55f));
					}
				}
			}
		}
		if (capsule != null)
		{
			HouseFootprint footprint = HousingRules.Footprint(terrain2, capsule, Prebuilt ? (-1) : _offsetX, Prebuilt ? (-1) : _offsetY);
			if (flag)
			{
				for (int m = 0; m < terrain.Width; m++)
				{
					for (int n = 0; n < terrain.Height; n++)
					{
						string key = $"place:{m}:{n}";
						Rectangle rect = new Rectangle(num5 + m * num2, num6 + n * num2, num2, num2);
						if (IsHot(key))
						{
							TechniqueArt.Fill(renderer, rect, A(TechniqueArt.White, 0.1f));
						}
						int num7 = m;
						int num8 = n;
						int cy = num8;
						int cx = num7;
						AddHit(rect, key, delegate
						{
							_offsetX = Math.Clamp(cx - capsule.Width / 2, 0, maxX);
							_offsetY = Math.Clamp(cy - capsule.Height / 2, 0, maxY);
						});
					}
				}
			}
			Rectangle rect2 = new Rectangle(num5 + footprint.X * num2, num6 + footprint.Y * num2, footprint.Width * num2, footprint.Height * num2);
			TechniqueArt.Fill(renderer, rect2, A(new Color(255, 255, 122, 26), 0.22f));
			TechniqueArt.Outline(renderer, rect2, A(new Color(255, 255, 122, 26), 0.85f));
			Rectangle rect3 = new Rectangle(num5 + footprint.DoorX * num2, num6 + footprint.DoorY * num2, num2, num2);
			TechniqueArt.Fill(renderer, rect3, A(new Color(255, 62, 197, 255), 0.55f));
			IGameTexture gameTexture = HouseWorld.CapsuleTexture(capsule);
			if (gameTexture != null && gameTexture.Width > 0)
			{
				float num9 = (float)num2 / (float)Options.Instance.Map.TileWidth;
				int num10 = (int)MathF.Round((float)gameTexture.Width * num9);
				int num11 = (int)MathF.Round((float)gameTexture.Height * num9);
				SaiyanHud.Draw(renderer, gameTexture, rect2.X + (rect2.Width - num10) / 2, rect2.Bottom - num11, num10, num11, A(TechniqueArt.White));
			}
			if (flag)
			{
				TechniqueArt.TextRight(renderer, TechniqueArt.Body, Pt(12f), Strings.Housing.PlaceHint, area.Right - _d.L(12f), area.Y + _d.L(24f), A(TechniqueArt.Soft));
				DrawPlacementChips(renderer, area, footprint, maxX, maxY);
			}
		}
		string text = (IsUpgrade ? Strings.Housing.UpgradeTag : Strings.Housing.BuyForSale).ToString().ToUpperInvariant();
		int w = TechniqueArt.SpacedWidth(renderer, TechniqueArt.Heavy, Pt(15f), text, _d.Em(0.1f, 15f)) + _d.L(20f);
		Rectangle rect4 = new Rectangle(area.X + _d.L(12f), area.Y + _d.L(12f), w, _d.L(25f));
		TechniqueArt.Fill(renderer, rect4, A(TechniqueArt.Gold));
		TechniqueArt.SpacedCenter(renderer, TechniqueArt.Heavy, Pt(15f), text, rect4.X + rect4.Width / 2, rect4.Y + rect4.Height / 2, _d.Em(0.1f, 15f), A(HouseArt.Dark));
	}

	private void DrawPlacementChips(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Rectangle area, HouseFootprint footprint, int maxX, int maxY)
	{
		List<(string, string, bool, Action, bool)> list = new List<(string, string, bool, Action, bool)>();
		if (maxX > 0)
		{
			list.Add(("gauche", Strings.Housing.PlaceLeft, footprint.X == 0, delegate
			{
				_offsetX = 0;
			}, false));
			list.Add(("centre", Strings.Housing.PlaceCenter, footprint.X == maxX / 2, delegate
			{
				_offsetX = -1;
			}, false));
			list.Add(("droite", Strings.Housing.PlaceRight, footprint.X == maxX, delegate
			{
				_offsetX = maxX;
			}, false));
		}
		if (maxY > 0)
		{
			list.Add(("fond", Strings.Housing.PlaceBack, footprint.Y == 0, delegate
			{
				_offsetY = 0;
			}, list.Count > 0));
			list.Add(("devant", Strings.Housing.PlaceFront, footprint.Y == maxY, delegate
			{
				_offsetY = -1;
			}, false));
		}
		int num = _d.L(24f);
		int num2 = _d.L(6f);
		List<int> list2 = list.Select<(string, string, bool, Action, bool), int>(((string Key, string Label, bool Active, Action Choose, bool NewGroup) chip) => TechniqueArt.Measure(renderer, TechniqueArt.Label, Pt(12.5f), chip.Label).X + _d.L(20f)).ToList();
		int num3 = list2.Sum() + num2 * Math.Max(0, list.Count - 1) + list.Count<(string, string, bool, Action, bool)>(((string Key, string Label, bool Active, Action Choose, bool NewGroup) chip) => chip.NewGroup) * _d.L(14f);
		int num4 = area.X + (area.Width - num3) / 2;
		int y = area.Bottom - _d.L(8f) - num;
		for (int num5 = 0; num5 < list.Count; num5++)
		{
			(string, string, bool, Action, bool) tuple = list[num5];
			var (text, text2, flag, action, _) = tuple;
			if (tuple.Item5)
			{
				num4 += _d.L(14f);
			}
			Rectangle rect = new Rectangle(num4, y, list2[num5], num);
			bool flag2 = IsHot("aligner:" + text);
			if (flag)
			{
				TechniqueArt.Fill(renderer, rect, A(TechniqueArt.Gold, 0.12f));
			}
			TechniqueArt.Outline(renderer, rect, A(flag ? TechniqueArt.Gold : (flag2 ? TechniqueArt.Line2 : TechniqueArt.Line)));
			TechniqueArt.TextCenter(renderer, TechniqueArt.Label, Pt(12.5f), text2, rect.X + rect.Width / 2, rect.Y + rect.Height / 2, A((flag || flag2) ? TechniqueArt.Text : TechniqueArt.Soft));
			AddHit(rect, "aligner:" + text, action);
			num4 += list2[num5] + num2;
		}
	}

	private void DrawCapsuleRow(Intersect.Client.Framework.Gwen.Renderer.Base renderer, CapsuleDescriptor capsule, float y, bool selectable)
	{
		bool flag = HousingRules.CapsuleFits(capsule, Terrain.Width, Terrain.Height);
		bool flag2 = capsule.Id == _capsuleId;
		string key = $"capsule:{capsule.Id}";
		Rectangle rect = _d.R(838f, y, 524f, 64f);
		float num = (flag ? 1f : 0.5f);
		bool flag3 = selectable && flag && IsHot(key);
		if (flag2)
		{
			TechniqueArt.FillHorizontal(renderer, rect, A(TechniqueArt.Gold, 0.14f), A(TechniqueArt.Gold, 0.07f), A(TechniqueArt.White, 0.02f));
		}
		else
		{
			TechniqueArt.Fill(renderer, rect, A(TechniqueArt.White, 0.03f * num));
		}
		TechniqueArt.Outline(renderer, rect, A(flag2 ? TechniqueArt.Gold : (flag3 ? TechniqueArt.Line2 : TechniqueArt.Line), num));
		Rectangle rect2 = _d.R(844f, y + 6f, 60f, 52f);
		TechniqueArt.Fill(renderer, rect2, A(new Color(255, 10, 18, 32), num));
		TechniqueArt.Outline(renderer, rect2, A(TechniqueArt.Line, num));
		HouseArt.CapsuleImage(renderer, capsule, new Rectangle(rect2.X + _d.L(2f), rect2.Y + _d.L(2f), rect2.Width - _d.L(4f), rect2.Height - _d.L(4f)), _fade * num);
		int x = rect2.Right + _d.L(12f);
		TechniqueArt.TextMiddle(renderer, TechniqueArt.Heavy, Pt(18f), capsule.Name, x, _d.Y(y + 23f), A(TechniqueArt.Text, num));
		string text = (flag ? Strings.Housing.BuyCapsuleRoom.ToString(capsule.RoomWidth, capsule.RoomHeight, capsule.MaxFurniture) : Strings.Housing.BuyTooBig.ToString(capsule.Width, capsule.Height));
		TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(12.5f), text, x, _d.Y(y + 44f), A(flag ? TechniqueArt.Soft : TechniqueArt.Red, flag ? 1f : 1f));
		if (!Prebuilt)
		{
			string text2 = ((IsUpgrade && _current != null) ? Strings.Housing.UpgradeRowPrice.ToString(MarketWindow.FormatAmount(HousingRules.UpgradePrice(_current, capsule))) : ((Folded != null) ? Strings.Housing.BuyFoldedOwned.ToString() : Strings.Housing.Amount.ToString(MarketWindow.FormatAmount(capsule.Price))));
			TechniqueArt.TextRight(renderer, TechniqueArt.Heavy, Pt(17f), text2, rect.Right - _d.L(12f), rect.Y + rect.Height / 2, A(TechniqueArt.Gold, num));
		}
		if (selectable && flag)
		{
			AddHit(rect, key, delegate
			{
				_capsuleId = capsule.Id;
			});
		}
	}

	private void DrawPrice(Intersect.Client.Framework.Gwen.Renderer.Base renderer, float top)
	{
		Rectangle rect = _d.R(838f, top, 524f, 60f);
		TechniqueArt.Fill(renderer, rect, A(TechniqueArt.Gold, 0.07f));
		TechniqueArt.Outline(renderer, rect, A(TechniqueArt.Gold, 0.3f));
		int num = rect.Y + rect.Height / 2;
		Rectangle rect2 = new Rectangle(rect.X + _d.L(14f), num - _d.L(14f), _d.L(28f), _d.L(28f));
		HouseArt.Zeni(renderer, rect2, (int)(Timing.Global.MillisecondsUtc / 200), _fade);
		string text = MarketWindow.FormatAmount(Total);
		Point point = TechniqueArt.Measure(renderer, TechniqueArt.Display, Pt(36f), text);
		TechniqueArt.Styled(renderer, TechniqueArt.Display, Pt(36f), text, rect2.Right + _d.L(10f), num - point.Y / 2, TechniqueArt.Stops.Solid(TechniqueArt.Gold), _fade);
		string text2 = (IsUpgrade ? Strings.Housing.UpgradeTotalDetail.ToString(_current?.Name ?? string.Empty) : ((Folded != null && !Prebuilt) ? Strings.Housing.BuyFoldedDetail.ToString() : (Prebuilt ? Strings.Housing.BuyPrebuiltDetail.ToString(Selected?.Name ?? string.Empty) : Strings.Housing.BuyTotalDetail.ToString(MarketWindow.FormatAmount(Terrain.Price), MarketWindow.FormatAmount(Selected?.Price ?? 0)))));
		long num2 = (Globals.Me?.Zeni ?? 0) - Total;
		string text3 = (FoldedBlocked ? Strings.Housing.BuyFoldedBlocked.ToString() : ((num2 < 0) ? Strings.Housing.BuyMissing.ToString(MarketWindow.FormatAmount(-num2)) : Strings.Housing.BuyRemaining.ToString(MarketWindow.FormatAmount(num2))));
		TechniqueArt.TextRight(renderer, TechniqueArt.Body, Pt(13f), text2, rect.Right - _d.L(14f), num - _d.L(9f), A(TechniqueArt.Soft));
		TechniqueArt.TextRight(renderer, TechniqueArt.Label, Pt(15f), text3, rect.Right - _d.L(14f), num + _d.L(11f), A((num2 < 0 || FoldedBlocked) ? TechniqueArt.Red : TechniqueArt.Text));
	}
}
