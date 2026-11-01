using System;
using System.Collections.Generic;
using Intersect.Client.Core;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.Framework.Gwen.Input;
using Intersect.Client.Framework.Gwen.Renderer;
using Intersect.Client.Framework.Input;
using Intersect.Client.Housing;
using Intersect.Client.Interface.Game.EntityPanel;
using Intersect.Client.Interface.Game.Techniques;
using Intersect.Client.Localization;
using Intersect.Framework.Core.GameObjects.Housing;
using Intersect.Framework.Core.GameObjects.Items;
using Intersect.Localization;
using Intersect.Models;

namespace Intersect.Client.Interface.Game.Housing;

internal sealed class DecorationScreen : CharteScreen
{
	private const float HeaderLeft = 288f;

	private const float HeaderTop = 28f;

	private const float HeaderWidth = 1024f;

	private const float WindowLeft = 1112f;

	private const float WindowTop = 118f;

	private const float WindowWidth = 456f;

	private const float WindowHeight = 684f;

	private const float CardsTop = 222f;

	private const float CardsBottom = 726f;

	private const float CardHeight = 80f;

	private Rectangle _window;

	private Rectangle _cardsArea;

	private int _scroll;

	public override bool IsBlockingInput => !base.IsHidden;

	public DecorationScreen(Canvas gameCanvas)
		: base(gameCanvas, "DecorationScreen")
	{
		Interface.InputBlockingComponents.Add(this);
		Input.KeyDown = (Input.HandleKeyEvent)Delegate.Combine(Input.KeyDown, new Input.HandleKeyEvent(OnKeyDown));
	}

	protected override void Dispose(bool disposing)
	{
		Input.KeyDown = (Input.HandleKeyEvent)Delegate.Remove(Input.KeyDown, new Input.HandleKeyEvent(OnKeyDown));
		base.Dispose(disposing);
	}

	private void OnKeyDown(Keys modifier, Keys key)
	{
		if (base.IsHidden || !Decoration.Active)
		{
			return;
		}
		Intersect.Client.Framework.Gwen.Control.Base keyboardFocus = InputHandler.KeyboardFocus;
		if (!(keyboardFocus is TextBox) || !keyboardFocus.IsVisibleInTree)
		{
			switch (key)
			{
			case Keys.R:
				Decoration.Rotate();
				break;
			case Keys.Delete:
				Decoration.StoreHovered();
				break;
			case Keys.H:
				Decoration.Stop();
				break;
			}
		}
	}

	protected override bool OnMouseWheeled(int delta)
	{
		Point point = LocalMouse();
		if (_cardsArea.Contains(point.X, point.Y))
		{
			_scroll = Math.Max(0, _scroll - Math.Sign(delta) * _d.L(88f));
			return true;
		}
		return base.OnMouseWheeled(delta);
	}

	protected override void OnEmptyClick(MouseButton mouseButton, Point local)
	{
		if (!_window.Contains(local.X, local.Y))
		{
			switch (mouseButton)
			{
			case MouseButton.Right:
				Decoration.Drop();
				break;
			case MouseButton.Left:
				Decoration.Click();
				break;
			}
		}
	}

	protected override void DrawScreen(Intersect.Client.Framework.Gwen.Renderer.Base renderer, long now)
	{
		if (Decoration.Active)
		{
			Point point = LocalMouse();
			_window = _d.R(1112f, 118f, 456f, 684f);
			Decoration.UpdatePointer(_window.Contains(point.X, point.Y) || IsOverHit(point));
			TechniqueArt.FillVertical(renderer, new Rectangle(0, 0, base.Width, _d.L(110f)), A(new Color(170, 4, 6, 11)), A(new Color(0, 4, 6, 11)));
			HouseHud.DrawHeader(renderer, _d, _d.X(288f), _d.Y(28f), _d.L(1024f), Strings.Housing.HudDecorating, _fade);
			DrawFurnitureWindow(renderer);
			DrawKeys(renderer);
			DrawBubble(renderer, point, now);
		}
	}

	private void DrawFurnitureWindow(Intersect.Client.Framework.Gwen.Renderer.Base renderer)
	{
		HouseArt.Window(renderer, _window, _fade);
		HouseArt.WindowHeader(renderer, _d, _window, Strings.Housing.Furniture, Strings.Housing.InYourBag, _fade);
		(FurnitureCategory?, string)[] array = new(FurnitureCategory?, string)[5]
		{
			(null, Strings.Housing.FilterAll),
			(FurnitureCategory.Comfort, Strings.Housing.FilterComfort),
			(FurnitureCategory.Storage, Strings.Housing.FilterStorage),
			(FurnitureCategory.Decor, Strings.Housing.FilterDecor),
			(FurnitureCategory.Wall, Strings.Housing.FilterWall)
		};
		int num = _d.X(1126f);
		int y = _d.Y(184f);
		(FurnitureCategory?, string)[] array2 = array;
		for (int i = 0; i < array2.Length; i++)
		{
			(FurnitureCategory?, string) tuple = array2[i];
			FurnitureCategory? category = tuple.Item1;
			string item = tuple.Item2;
			int num2 = TechniqueArt.Measure(renderer, TechniqueArt.Label, Pt(12.5f), item).X + _d.L(20f);
			Rectangle rect = new Rectangle(num, y, num2, _d.L(26f));
			bool flag = Decoration.Filter == category;
			string key = $"filtre:{category}";
			if (flag)
			{
				TechniqueArt.Fill(renderer, rect, A(TechniqueArt.Gold, 0.1f));
			}
			TechniqueArt.Outline(renderer, rect, A(flag ? TechniqueArt.Gold : (IsHot(key) ? TechniqueArt.Line2 : TechniqueArt.Line)));
			TechniqueArt.TextCenter(renderer, TechniqueArt.Label, Pt(12.5f), item, rect.X + rect.Width / 2, rect.Y + rect.Height / 2, A(flag ? TechniqueArt.Text : TechniqueArt.Soft));
			AddHit(rect, key, delegate
			{
				Decoration.Filter = category;
				_scroll = 0;
			});
			num += num2 + _d.L(4f);
		}
		_cardsArea = _d.R(1126f, 222f, 428f, 504f);
		List<(ItemDescriptor, FurnitureDescriptor, int)> list = Decoration.Catalog();
		int num3 = (_cardsArea.Width - _d.L(8f)) / 2;
		int num4 = (list.Count + 1) / 2;
		int num5 = num4 * _d.L(88f);
		_scroll = Math.Clamp(_scroll, 0, Math.Max(0, num5 - _cardsArea.Height));
		Rectangle previous = TechniqueArt.BeginClip(renderer, _cardsArea);
		for (int num6 = 0; num6 < list.Count; num6++)
		{
			int num7 = num6 % 2;
			int num8 = num6 / 2;
			Rectangle card = new Rectangle(_cardsArea.X + num7 * (num3 + _d.L(8f)), _cardsArea.Y + num8 * _d.L(88f) - _scroll, num3, _d.L(80f));
			if (card.Bottom >= _cardsArea.Y && card.Y <= _cardsArea.Bottom)
			{
				DrawCard(renderer, card, list[num6].Item1, list[num6].Item2, list[num6].Item3);
			}
		}
		TechniqueArt.EndClip(renderer, previous);
		float y2 = 742f;
		string text = Strings.Housing.StoreAll.ToString();
		float num9 = (float)TechniqueArt.Measure(renderer, TechniqueArt.Label, Pt(14f), text).X / _d.S + 32f;
		Rectangle rect2 = _d.R(1126f, y2, num9, 46f);
		HouseArt.Hollow(renderer, _d, rect2, text, IsHot("tout-ranger"), _fade);
		AddHit(rect2, "tout-ranger", Decoration.StoreAll);
		Rectangle rect3 = _d.R(1126f + num9 + 10f, y2, 428f - num9 - 10f, 46f);
		HouseArt.Cta(renderer, _d, rect3, Strings.Housing.Finish, enabled: true, IsHot("terminer"), _fade);
		AddHit(rect3, "terminer", Decoration.Stop);
	}

	private void DrawCard(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Rectangle card, ItemDescriptor item, FurnitureDescriptor furniture, int inBag)
	{
		string key = $"meuble:{item.Id}";
		bool flag = Decoration.HeldItemId == item.Id && Decoration.HeldFromRoom == null;
		bool flag2 = IsHot(key);
		float num = ((inBag == 0) ? 0.45f : 1f);
		TechniqueArt.Patch(renderer, "tech_chanfrein8.png", card, A(new Color(217, 12, 17, 28), num), 10);
		TechniqueArt.Patch(renderer, "tech_chanfrein8_degrade.png", card, A(flag ? TechniqueArt.Mix(TechniqueArt.Gold, new Color(255, 22, 30, 48), 0.18f) : new Color(230, 22, 30, 48), num), 10);
		TechniqueArt.Patch(renderer, "tech_chanfrein8_bord.png", card, A(flag ? TechniqueArt.Gold : ((flag2 && inBag > 0) ? TechniqueArt.Line2 : TechniqueArt.Line), num), 10);
		Rectangle rect = new Rectangle(card.X + _d.L(8f), card.Y + _d.L(8f), _d.L(64f), _d.L(64f));
		TechniqueArt.Fill(renderer, rect, A(new Color(255, 7, 10, 18), num));
		TechniqueArt.GlowRect(renderer, rect, A(new Color(255, 29, 41, 64), 0.9f * num), additive: false);
		TechniqueArt.Outline(renderer, rect, A(TechniqueArt.Line, num));
		HouseArt.FurnitureThumbnail(renderer, furniture, 0, new Rectangle(rect.X + _d.L(4f), rect.Y + _d.L(4f), _d.L(56f), _d.L(56f)), _fade * num);
		int num2 = rect.Right + _d.L(10f);
		FurnitureVariant furnitureVariant = furniture.VariantAt(0);
		LocalizedString localizedString = furniture.Placement switch
		{
			FurniturePlacement.AgainstWall => Strings.Housing.PlaceAgainstWall, 
			FurniturePlacement.Wall => Strings.Housing.PlaceWall, 
			FurniturePlacement.Rug => Strings.Housing.PlaceRug, 
			_ => Strings.Housing.PlaceFloor, 
		};
		string text = ((furniture.Variants.Count > 1) ? Strings.Housing.TurnsVariants.ToString(furniture.Variants.Count) : (furniture.NoMirror ? Strings.Housing.TurnsNot.ToString() : Strings.Housing.TurnsMirror.ToString()));
		string text2 = Houses.EffectText(furniture);
		int num3 = (string.IsNullOrEmpty(text2) ? 3 : 4);
		int num4 = card.Y + (card.Height - _d.L((num3 == 3) ? 52 : 66)) / 2;
		int width = card.Right - _d.L(34f) - num2;
		int spacing = 0;
		string text3 = SaiyanHud.Fit(renderer, TechniqueArt.Heavy, Pt(17f), item.Name, width, ref spacing);
		TechniqueArt.TextMiddle(renderer, TechniqueArt.Heavy, Pt(17f), text3, num2, num4 + _d.L(9f), A(TechniqueArt.Text, num));
		TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(12f), Strings.Housing.CardSize.ToString(furnitureVariant.Width, furnitureVariant.Height, localizedString), num2, num4 + _d.L(26f), A(TechniqueArt.Soft, num));
		TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(12f), text, num2, num4 + _d.L(41f), A((furniture.Variants.Count <= 1 && furniture.NoMirror) ? TechniqueArt.Dim : TechniqueArt.Cyan, num));
		if (!string.IsNullOrEmpty(text2))
		{
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(12f), text2, num2, num4 + _d.L(56f), A(TechniqueArt.Green, num));
		}
		TechniqueArt.TextRight(renderer, TechniqueArt.Heavy, Pt(14f), Strings.Housing.Quantity.ToString(inBag), card.Right - _d.L(8f), card.Y + _d.L(14f), A(TechniqueArt.Gold, num));
		Guid itemId = item.Id;
		if (card.Y >= _cardsArea.Y - _d.L(4f) && card.Bottom <= _cardsArea.Bottom + _d.L(4f))
		{
			AddHit(card, key, delegate
			{
				Decoration.PickFromBag(itemId);
			});
		}
	}

	private void DrawKeys(Intersect.Client.Framework.Gwen.Renderer.Base renderer)
	{
		(string, string)[] array = new(string, string)[5]
		{
			(Strings.Housing.KeyClick, Strings.Housing.HintPlace),
			(Strings.Housing.KeyRotate, Strings.Housing.HintRotate),
			(Strings.Housing.KeyDelete, Strings.Housing.HintStore),
			(Strings.Housing.KeyEscape, Strings.Housing.HintDrop),
			(Strings.Housing.KeyDecorate, Strings.Housing.HintQuit)
		};
		int num = _d.X(288f);
		int num2 = _d.Y(825f);
		Rectangle rect = new Rectangle(num - _d.L(14f), num2 - _d.L(22f), _d.L(1024f) - _d.L(420f) + _d.L(28f), _d.L(44f));
		TechniqueArt.Fill(renderer, rect, A(new Color(160, 4, 6, 11)));
		(string, string)[] array2 = array;
		for (int i = 0; i < array2.Length; i++)
		{
			(string, string) tuple = array2[i];
			string item = tuple.Item1;
			string item2 = tuple.Item2;
			int num3 = HouseArt.KeyCap(renderer, _d, num, num2, item, _fade);
			int num4 = TechniqueArt.TextMiddle(renderer, TechniqueArt.Label, Pt(14f), item2, num + num3 + _d.L(8f), num2, A(TechniqueArt.Soft));
			num += num3 + _d.L(8f) + num4 + _d.L(26f);
		}
	}

	private void DrawBubble(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Point mouse, long now)
	{
		if (!Decoration.PointerCell.HasValue)
		{
			return;
		}
		List<(string, Color)> list = new List<(string, Color)>();
		FurnitureDescriptor heldDescriptor = Decoration.HeldDescriptor;
		string text;
		if (heldDescriptor != null)
		{
			text = DatabaseObject<ItemDescriptor>.Get(Decoration.HeldDescriptorItemId)?.Name ?? string.Empty;
			FurnitureVerdict furnitureVerdict = Decoration.CurrentVerdict();
			list.Add((furnitureVerdict == FurnitureVerdict.Ok) ? ((string)Strings.Housing.BubbleClickToPlace, TechniqueArt.Green) : (Houses.VerdictText(furnitureVerdict), TechniqueArt.Red));
			list.Add((string.Concat(Strings.Housing.BubbleRotate, Decoration.HeldSenseSuffix()), TechniqueArt.Soft));
		}
		else
		{
			HouseFurnitureInfo hovered = Decoration.Hovered;
			if (hovered == null)
			{
				return;
			}
			ItemDescriptor itemDescriptor = DatabaseObject<ItemDescriptor>.Get(hovered.ItemId);
			if (itemDescriptor == null)
			{
				return;
			}
			FurnitureDescriptor furniture = itemDescriptor.Furniture;
			if (furniture == null)
			{
				return;
			}
			text = itemDescriptor.Name;
			string text2 = Houses.EffectText(furniture);
			if (!string.IsNullOrEmpty(text2))
			{
				list.Add((text2, TechniqueArt.Green));
			}
			list.Add((Strings.Housing.BubbleHover, TechniqueArt.Soft));
		}
		(string, bool, long)? note = Decoration.Note;
		if (note.HasValue)
		{
			(string, bool, long) valueOrDefault = note.GetValueOrDefault();
			if (valueOrDefault.Item3 > now)
			{
				list.Add((valueOrDefault.Item1, valueOrDefault.Item2 ? TechniqueArt.Green : TechniqueArt.Red));
			}
		}
		int num = TechniqueArt.Measure(renderer, TechniqueArt.Heavy, Pt(17f), text).X;
		foreach (var item in list)
		{
			num = Math.Max(num, TechniqueArt.Measure(renderer, TechniqueArt.Body, Pt(13.5f), item.Item1).X);
		}
		num += _d.L(24f);
		int num2 = _d.L(38f) + list.Count * _d.L(19f);
		Rectangle rect = new Rectangle(mouse.X + _d.L(18f), mouse.Y + _d.L(18f), num, num2);
		if (rect.Right > base.Width - _d.L(8f))
		{
			rect.X = mouse.X - _d.L(18f) - num;
		}
		if (rect.Bottom > base.Height - _d.L(8f))
		{
			rect.Y = mouse.Y - _d.L(18f) - num2;
		}
		TechniqueArt.Fill(renderer, rect, A(new Color(242, 6, 8, 14)));
		TechniqueArt.Outline(renderer, rect, A(TechniqueArt.Line2));
		TechniqueArt.TextMiddle(renderer, TechniqueArt.Heavy, Pt(17f), text, rect.X + _d.L(12f), rect.Y + _d.L(19f), A(TechniqueArt.Text));
		int num3 = rect.Y + _d.L(39f);
		foreach (var (text3, color) in list)
		{
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(13.5f), text3, rect.X + _d.L(12f), num3, A(color));
			num3 += _d.L(19f);
		}
	}
}
