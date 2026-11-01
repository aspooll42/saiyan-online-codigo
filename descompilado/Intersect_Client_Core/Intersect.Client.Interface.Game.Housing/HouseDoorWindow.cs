using System;
using System.Linq;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.Framework.Gwen.Renderer;
using Intersect.Client.Housing;
using Intersect.Client.Interface.Game.Techniques;
using Intersect.Client.Localization;
using Intersect.Client.Networking;
using Intersect.Framework.Core;
using Intersect.Framework.Core.GameObjects.Housing;
using Intersect.Localization;
using Intersect.Network.Packets.Server;

namespace Intersect.Client.Interface.Game.Housing;

internal sealed class HouseDoorWindow : CharteScreen
{
	private const float Top = 118f;

	private const float CardWidth = 620f;

	private const int MaxVisitors = 5;

	private HouseAccess _access;

	public override bool IsBlockingInput => !base.IsHidden;

	public HouseDoorWindow(Canvas gameCanvas)
		: base(gameCanvas, "HouseDoorWindow")
	{
		Interface.InputBlockingComponents.Add(this);
	}

	public override void Show()
	{
		_access = Houses.Interior?.Access ?? HouseAccess.Open;
		base.Show();
	}

	private void Save()
	{
		HouseInteriorPacket interior = Houses.Interior;
		if (interior != null && interior.Access != _access)
		{
			PacketSender.SendHouseAccess(_access);
		}
		Hide();
	}

	protected override void DrawScreen(Intersect.Client.Framework.Gwen.Renderer.Base renderer, long now)
	{
		HouseInteriorPacket interior = Houses.Interior;
		if (interior != null && interior.IsOwner)
		{
			float num = 490f;
			AddHit(new Rectangle(0, 0, base.Width, base.Height), "fond", delegate
			{
			});
			HouseVisitorInfo[] array = interior.Visitors.Take(5).ToArray();
			float num2 = 512f;
			float num3 = num2 + (float)((array.Length == 0) ? 34 : (array.Length * 50));
			float num4 = num3 + 12f;
			float num5 = num4 + 46f + 14f;
			Rectangle rectangle = _d.R(num, 118f, 620f, num5 - 118f);
			HouseArt.Window(renderer, rectangle, _fade);
			AddHit(rectangle, "carte", delegate
			{
			});
			HouseArt.WindowHeader(renderer, _d, rectangle, Strings.Housing.DoorTitle, Strings.Housing.DoorVisible, _fade);
			(HouseAccess, string, string, string)[] array2 = new(HouseAccess, string, string, string)[3]
			{
				(HouseAccess.Open, Strings.Housing.OptionOpen, Strings.Housing.OptionOpenHelp, Strings.Housing.TagOpen),
				(HouseAccess.FriendsAndGuild, Strings.Housing.OptionFriends, Strings.Housing.OptionFriendsHelp, Strings.Housing.TagFriends),
				(HouseAccess.Locked, Strings.Housing.OptionLocked, Strings.Housing.OptionLockedHelp, Strings.Housing.TagLocked)
			};
			for (int num6 = 0; num6 < array2.Length; num6++)
			{
				DrawOption(renderer, num + 18f, 190 + num6 * 66, 584f, array2[num6]);
			}
			Heading(renderer, num + 18f, 404f, 584f, Strings.Housing.PassersBy, null);
			Rectangle rectangle2 = _d.R(num + 18f, 418f, 584f, 56f);
			TechniqueArt.FillVertical(renderer, rectangle2, A(new Color(255, 30, 44, 34)), A(new Color(255, 18, 26, 22)));
			TechniqueArt.Outline(renderer, rectangle2, A(TechniqueArt.Line));
			HouseArt.PlateContent content = new HouseArt.PlateContent(Strings.Housing.PlateOwner.ToString(interior.OwnerName), TechniqueArt.Text, Houses.AccessText(_access), HouseArt.AccessColor(_access), ForSale: false, _access == HouseAccess.Locked);
			Rectangle previous = TechniqueArt.BeginClip(renderer, rectangle2);
			HouseArt.Plate(renderer, _d, rectangle2.X + rectangle2.Width / 2, rectangle2.Y + _d.L(51f), content, _fade);
			TechniqueArt.EndClip(renderer, previous);
			Heading(renderer, num + 18f, 496f, 584f, Strings.Housing.InYourHouse, null);
			if (array.Length == 0)
			{
				TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(12f), Strings.Housing.NobodyInside, _d.X(num + 18f), _d.Y(num2 + 16f), A(TechniqueArt.Dim));
			}
			long elapsedSeconds = (Timing.Global.MillisecondsUtc - Houses.InteriorReceivedAt) / 1000;
			for (int num7 = 0; num7 < array.Length; num7++)
			{
				DrawVisitor(renderer, num + 18f, num2 + (float)(num7 * 50), 584f, array[num7], elapsedSeconds);
			}
			Rectangle rect = _d.R(num + 14f, num4, 592f, 46f);
			HouseArt.Cta(renderer, _d, rect, Strings.Housing.Save, enabled: true, IsHot("enregistrer"), _fade);
			AddHit(rect, "enregistrer", Save);
		}
	}

	private void DrawOption(Intersect.Client.Framework.Gwen.Renderer.Base renderer, float x, float y, float width, (HouseAccess Access, string Title, string Help, string Tag) option)
	{
		string key = $"acces:{option.Access}";
		bool flag = _access == option.Access;
		bool flag2 = !flag && IsHot(key);
		Rectangle rect = _d.R(x, y, width, 58f);
		if (flag)
		{
			TechniqueArt.FillHorizontal(renderer, rect, A(TechniqueArt.Gold, 0.14f), A(TechniqueArt.Gold, 0.07f), A(TechniqueArt.White, 0.02f));
		}
		else
		{
			TechniqueArt.Fill(renderer, rect, A(TechniqueArt.White, 0.03f));
		}
		TechniqueArt.Outline(renderer, rect, A(flag ? TechniqueArt.Gold : (flag2 ? TechniqueArt.Line2 : TechniqueArt.Line)));
		int num = _d.X(x + 12f + 18f);
		int num2 = rect.Y + rect.Height / 2;
		Diamond(renderer, num, num2, _d.L(15f), A(flag ? TechniqueArt.Gold : TechniqueArt.Line2), _d.L(2f));
		if (flag)
		{
			TechniqueArt.Image(renderer, "tech_point.png", new Rectangle(num - _d.L(5f), num2 - _d.L(5f), _d.L(10f), _d.L(10f)), A(TechniqueArt.Gold));
		}
		int x2 = _d.X(x + 12f + 36f + 12f);
		TechniqueArt.TextMiddle(renderer, TechniqueArt.Heavy, Pt(19f), option.Title, x2, _d.Y(y + 21f), A(TechniqueArt.Text));
		TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(13f), option.Help, x2, _d.Y(y + 41f), A(TechniqueArt.Soft));
		TechniqueArt.SpacedRight(renderer, TechniqueArt.Label, Pt(12f), option.Tag, rect.Right - _d.L(12f), num2, _d.Em(0.08f, 12f), A(HouseArt.AccessColor(option.Access)));
		AddHit(rect, key, delegate
		{
			_access = option.Access;
		});
	}

	private static void Diamond(Intersect.Client.Framework.Gwen.Renderer.Base renderer, int centerX, int centerY, int radius, Color color, int thickness)
	{
		for (int i = -radius; i <= radius; i++)
		{
			int num = radius - Math.Abs(i);
			float num2 = (float)num - (float)thickness * 1.41f;
			if (num2 <= 0f)
			{
				TechniqueArt.Fill(renderer, new Rectangle(centerX - num, centerY + i, num * 2 + 1, 1), color);
				continue;
			}
			int num3 = Math.Max(1, (int)MathF.Ceiling((float)num - num2));
			TechniqueArt.Fill(renderer, new Rectangle(centerX - num, centerY + i, num3, 1), color);
			TechniqueArt.Fill(renderer, new Rectangle(centerX + num - num3 + 1, centerY + i, num3, 1), color);
		}
	}

	private void DrawVisitor(Intersect.Client.Framework.Gwen.Renderer.Base renderer, float x, float y, float width, HouseVisitorInfo visitor, long elapsedSeconds)
	{
		Rectangle rectangle = _d.R(x, y, width, 50f);
		TechniqueArt.Fill(renderer, new Rectangle(rectangle.X, rectangle.Y, rectangle.Width, 1), A(TechniqueArt.White, 0.05f));
		int num = rectangle.Y + rectangle.Height / 2;
		TechniqueArt.Glow(renderer, rectangle.X + _d.L(4f), num, _d.L(8f), A(TechniqueArt.Green, 0.5f));
		TechniqueArt.Image(renderer, "tech_point.png", new Rectangle(rectangle.X, num - _d.L(4f), _d.L(8f), _d.L(8f)), A(TechniqueArt.Green));
		LocalizedString localizedString = visitor.Relation switch
		{
			HouseVisitorRelation.Friend => visitor.Female ? Strings.Housing.VisitorFriendFemale : Strings.Housing.VisitorFriend, 
			HouseVisitorRelation.Guild => Strings.Housing.VisitorGuild, 
			_ => visitor.Female ? Strings.Housing.VisitorGuestFemale : Strings.Housing.VisitorGuest, 
		};
		long num2 = visitor.SecondsInside + Math.Max(0L, elapsedSeconds);
		string text = ((num2 < 60) ? Strings.Housing.VisitorJustNow.ToString(localizedString) : (visitor.Female ? Strings.Housing.VisitorEnteredFemale : Strings.Housing.VisitorEntered).ToString(localizedString, num2 / 60));
		int x2 = rectangle.X + _d.L(22f);
		TechniqueArt.TextMiddle(renderer, TechniqueArt.Label, Pt(16f), visitor.Name, x2, num - _d.L(8f), A(TechniqueArt.Text));
		TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(12.5f), text, x2, num + _d.L(10f), A(TechniqueArt.Soft));
		string key = $"raccompagner:{visitor.Id}";
		string text2 = Strings.Housing.ShowOut.ToString();
		int num3 = TechniqueArt.Measure(renderer, TechniqueArt.Label, Pt(13f), text2).X + _d.L(28f);
		Rectangle rect = new Rectangle(rectangle.Right - num3, num - _d.L(15f), num3, _d.L(30f));
		bool flag = IsHot(key);
		TechniqueArt.Outline(renderer, rect, A(TechniqueArt.Gold, flag ? 0.9f : 0.5f));
		TechniqueArt.TextCenter(renderer, TechniqueArt.Label, Pt(13f), text2, rect.X + rect.Width / 2, num, A(TechniqueArt.Gold));
		Guid visitorId = visitor.Id;
		AddHit(rect, key, delegate
		{
			PacketSender.SendHouseShowOut(visitorId);
		});
	}
}
