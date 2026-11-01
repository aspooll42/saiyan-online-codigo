using System;
using Intersect.Client.Core;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.Framework.Gwen.Input;
using Intersect.Client.Framework.Gwen.Renderer;
using Intersect.Client.Framework.Gwen.Skin;
using Intersect.Client.Framework.Input;
using Intersect.Client.Housing;
using Intersect.Client.Interface.Game.Techniques;
using Intersect.Client.Interface.Shared;
using Intersect.Client.Localization;
using Intersect.Client.Networking;
using Intersect.Framework.Core.GameObjects.Housing;
using Intersect.Network.Packets.Server;

namespace Intersect.Client.Interface.Game.Housing;

internal sealed class HouseHud : Intersect.Client.Framework.Gwen.Control.Base
{
	private const float DesignLeft = 288f;

	private const float DesignTop = 20f;

	private const float DesignWidth = 1024f;

	private const float DesignHeight = 118f;

	private Rectangle _decorate;

	private Rectangle _door;

	private Rectangle _sell;

	private Rectangle _upgrade;

	private bool _doorOpen;

	protected override bool ShouldClip => false;

	public HouseHud(Canvas canvas)
		: base(canvas, "HouseHud")
	{
		base.MouseInputEnabled = true;
		base.KeyboardInputEnabled = false;
		base.IsHidden = true;
	}

	public void Update(bool doorOpen)
	{
		_doorOpen = doorOpen;
		bool flag = Houses.InHouse && Houses.Interior.IsOwner && !Decoration.Active;
		base.IsHidden = !flag;
		if (!flag)
		{
			return;
		}
		Intersect.Client.Framework.Gwen.Control.Base parent = base.Parent;
		if (parent != null)
		{
			Rectangle rectangle = CharteScreen.Design.For(parent.Width, parent.Height).R(288f, 20f, 1024f, 118f);
			if (rectangle.X != base.X || rectangle.Y != base.Y || rectangle.Width != base.Width || rectangle.Height != base.Height)
			{
				SetBounds(rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height);
			}
		}
	}

	public static void DrawHeader(Intersect.Client.Framework.Gwen.Renderer.Base renderer, CharteScreen.Design d, int left, int top, int width, string title, float alpha = 1f)
	{
		HouseInteriorPacket interior = Houses.Interior;
		if (interior != null)
		{
			CapsuleDescriptor interiorCapsule = Houses.InteriorCapsule;
			TechniqueArt.Spaced(renderer, TechniqueArt.Label, d.Pt(12f), Strings.Housing.HudOwner.ToString(interior.OwnerName).ToUpperInvariant(), left, top + d.L(6f), d.Em(0.3f, 12f), TechniqueArt.Alpha(HouseArt.KiLight, alpha));
			string text = title.ToUpperInvariant();
			Point point = TechniqueArt.Measure(renderer, TechniqueArt.Display, d.Pt(42f), text);
			TechniqueArt.Styled(renderer, TechniqueArt.Display, d.Pt(42f), text, left, top + d.L(39f) - point.Y / 2, new TechniqueArt.Stops(TechniqueArt.White, 0.35f, new Color(255, 255, 194, 122), 0.75f, TechniqueArt.Ki), alpha);
			int num = left + width;
			(string, string, string)[] array = new(string, string, string)[2]
			{
				(Strings.Housing.CounterFurniture, interior.Furniture.Length.ToString(), $" / {interiorCapsule?.MaxFurniture ?? 0}"),
				(Strings.Housing.CounterRoom, $"{interiorCapsule?.RoomWidth ?? 0}", $" × {interiorCapsule?.RoomHeight ?? 0}")
			};
			for (int num2 = array.Length - 1; num2 >= 0; num2--)
			{
				(string, string, string) tuple = array[num2];
				string item = tuple.Item1;
				string item2 = tuple.Item2;
				string item3 = tuple.Item3;
				string text2 = item.ToUpperInvariant();
				int val = TechniqueArt.SpacedWidth(renderer, TechniqueArt.Label, d.Pt(12f), text2, d.Em(0.2f, 12f));
				int x = TechniqueArt.Measure(renderer, TechniqueArt.Heavy, d.Pt(18f), item3).X;
				int x2 = TechniqueArt.Measure(renderer, TechniqueArt.Heavy, d.Pt(26f), item2).X;
				TechniqueArt.SpacedRight(renderer, TechniqueArt.Label, d.Pt(12f), text2, num, top + d.L(22f), d.Em(0.2f, 12f), TechniqueArt.Alpha(TechniqueArt.Dim, alpha));
				TechniqueArt.TextRight(renderer, TechniqueArt.Heavy, d.Pt(18f), item3, num, top + d.L(48f), TechniqueArt.Alpha(TechniqueArt.Dim, alpha));
				TechniqueArt.TextRight(renderer, TechniqueArt.Heavy, d.Pt(26f), item2, num - x, top + d.L(46f), TechniqueArt.Alpha(TechniqueArt.Text, alpha));
				num -= Math.Max(val, x2 + x) + d.L(22f);
			}
		}
	}

	protected override void Render(Intersect.Client.Framework.Gwen.Skin.Base skin)
	{
		base.Render(skin);
		if (base.IsHidden)
		{
			return;
		}
		Intersect.Client.Framework.Gwen.Control.Base parent = base.Parent;
		if (parent != null)
		{
			Intersect.Client.Framework.Gwen.Renderer.Base renderer = skin.Renderer;
			CharteScreen.Design d = CharteScreen.Design.For(parent.Width, parent.Height);
			Rectangle rect = new Rectangle(0, 0, base.Width, d.L(76f));
			TechniqueArt.FillVertical(renderer, rect, new Color(150, 4, 6, 11), new Color(0, 4, 6, 11));
			DrawHeader(renderer, d, 0, d.L(8f), base.Width, _doorOpen ? Strings.Housing.DoorTitle : Strings.Housing.HudHome);
			Point point = CanvasPosToLocal(InputHandler.MousePosition);
			int num = d.L(96f);
			int num2 = d.L(10f);
			CapsuleDescriptor interiorCapsule = Houses.InteriorCapsule;
			string text = Strings.Housing.HudDecorate.ToString();
			string text2 = Strings.Housing.DoorTitle.ToString();
			string text3 = Strings.Housing.SellButton.ToString();
			string text4 = Strings.Housing.UpgradeButton.ToString();
			bool flag = Houses.Interior?.CanUpgrade ?? false;
			int num3 = TechniqueArt.Measure(renderer, TechniqueArt.Label, d.Pt(14f), text2).X + d.L(32f);
			int num4 = TechniqueArt.Measure(renderer, TechniqueArt.Label, d.Pt(14f), text3).X + d.L(32f);
			int num5 = (flag ? (TechniqueArt.Measure(renderer, TechniqueArt.Label, d.Pt(14f), text4).X + d.L(32f)) : 0);
			int num6 = d.L(10f) + num3 + d.L(10f) + num4 + d.L(10f) + (flag ? (num5 + d.L(10f)) : 0);
			if (interiorCapsule != null && interiorCapsule.OwnerCanDecorate)
			{
				num6 += HouseArt.KeyCapWidth(renderer, d, Strings.Housing.KeyDecorate) + d.L(8f) + TechniqueArt.Measure(renderer, TechniqueArt.Label, d.Pt(14f), text).X + d.L(26f);
			}
			Rectangle rect2 = new Rectangle(0, num - d.L(21f), num6, d.L(42f));
			TechniqueArt.Patch(renderer, "tech_chanfrein8.png", rect2, new Color(235, 8, 11, 19), 10);
			TechniqueArt.Patch(renderer, "tech_chanfrein8_bord.png", rect2, TechniqueArt.Line2, 10);
			_decorate = default(Rectangle);
			if (interiorCapsule != null && interiorCapsule.OwnerCanDecorate)
			{
				int num7 = HouseArt.KeyCap(renderer, d, num2, num, Strings.Housing.KeyDecorate);
				bool flag2 = IsHovered && new Rectangle(num2, num - d.L(14f), num7 + d.L(90f), d.L(28f)).Contains(point.X, point.Y);
				int num8 = TechniqueArt.TextMiddle(renderer, TechniqueArt.Label, d.Pt(14f), text, num2 + num7 + d.L(8f), num, flag2 ? TechniqueArt.Text : TechniqueArt.Soft);
				_decorate = new Rectangle(num2, num - d.L(14f), num7 + d.L(8f) + num8, d.L(28f));
				num2 = _decorate.Right + d.L(26f);
			}
			_door = new Rectangle(num2, num - d.L(15f), num3, d.L(30f));
			HouseArt.Hollow(renderer, d, _door, text2, IsHovered && _door.Contains(point.X, point.Y));
			_upgrade = default(Rectangle);
			if (flag)
			{
				_upgrade = new Rectangle(_door.Right + d.L(10f), num - d.L(15f), num5, d.L(30f));
				HouseArt.Hollow(renderer, d, _upgrade, text4, IsHovered && _upgrade.Contains(point.X, point.Y), 1f, TechniqueArt.Gold);
			}
			_sell = new Rectangle((flag ? _upgrade.Right : _door.Right) + d.L(10f), num - d.L(15f), num4, d.L(30f));
			bool hot = IsHovered && _sell.Contains(point.X, point.Y);
			HouseArt.Hollow(renderer, d, _sell, text3, hot, 1f, TechniqueArt.Red);
		}
	}

	private static void AskSell()
	{
		HouseInteriorPacket interior = Houses.Interior;
		if (interior != null && interior.IsOwner)
		{
			string text = MarketWindow.FormatAmount(interior.ResalePrice);
			ConfirmCardRequest confirmCardRequest = new ConfirmCardRequest()
			{
				Title = Strings.Housing.SellTitle,
				Heading = Houses.InteriorCapsule?.Name ?? Strings.Housing.HudOwner.ToString(interior.OwnerName),
				Prompt = Strings.Housing.SellPrompt.ToString(text),
				Detail = Strings.Housing.SellDetail,
				Chip = Strings.Housing.SellChip.ToString(text),
				ConfirmLabel = Strings.Housing.SellConfirm,
				CancelLabel = Strings.Housing.SellCancel,
				Danger = true,
				ShowMe = true,
			};
			ConfirmCardWindow.Open(confirmCardRequest, PacketSender.SendHouseSell);
		}
	}

	protected override void OnMouseClicked(MouseButton mouseButton, Point mousePosition, bool userAction = true)
	{
		base.OnMouseClicked(mouseButton, mousePosition, userAction);
		if (mouseButton == MouseButton.Left)
		{
			Point point = CanvasPosToLocal(mousePosition);
			if (_decorate.Contains(point.X, point.Y))
			{
				Audio.AddGameSound("menu_click.wav", loop: false);
				Decoration.Start();
			}
			else if (_door.Contains(point.X, point.Y))
			{
				Audio.AddGameSound("menu_click.wav", loop: false);
				HouseInterface.ToggleDoor();
			}
			else if (_upgrade.Contains(point.X, point.Y))
			{
				Audio.AddGameSound("menu_click.wav", loop: false);
				HouseInterface.OpenUpgrade();
			}
			else if (_sell.Contains(point.X, point.Y))
			{
				Audio.AddGameSound("menu_click.wav", loop: false);
				AskSell();
			}
		}
	}
}
