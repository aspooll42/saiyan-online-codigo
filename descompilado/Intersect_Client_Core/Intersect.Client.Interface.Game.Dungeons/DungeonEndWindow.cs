using System;
using System.Collections.Generic;
using System.Linq;
using Intersect.Client.Dungeons;
using Intersect.Client.Framework.Content;
using Intersect.Client.Framework.File_Management;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.Framework.Gwen.Renderer;
using Intersect.Client.Interface.Game.EntityPanel;
using Intersect.Client.Interface.Game.Housing;
using Intersect.Client.Interface.Game.Techniques;
using Intersect.Client.Localization;
using Intersect.Client.Networking;
using Intersect.Framework.Core;
using Intersect.Framework.Core.GameObjects.Dungeons;
using Intersect.Framework.Core.GameObjects.Items;
using Intersect.Framework.Core.GameObjects.NPCs;
using Intersect.Framework.Core.GameObjects.Titles;
using Intersect.Localization;
using Intersect.Models;
using Intersect.Network.Packets.Server;

namespace Intersect.Client.Interface.Game.Dungeons;

internal sealed class DungeonEndWindow : CharteScreen
{
	private const float CardWidth = 820f;

	private static readonly Color KiLight = new Color(255, 255, 179, 92);

	public override bool IsBlockingInput => !base.IsHidden;

	private static float Left => 390f;

	public DungeonEndWindow(Canvas gameCanvas)
		: base(gameCanvas, "DungeonEndWindow")
	{
		Interface.InputBlockingComponents.Add(this);
	}

	protected override void DrawScreen(Intersect.Client.Framework.Gwen.Renderer.Base renderer, long now)
	{
		DungeonCompletePacket completed = Dungeon.Completed;
		if (completed == null)
		{
			return;
		}
		DungeonDescriptor dungeonDescriptor = DatabaseObject<DungeonDescriptor>.Get(completed.DungeonId);
		Rectangle rect = new Rectangle(0, 0, base.Width, base.Height);
		TechniqueArt.Fill(renderer, rect, A(new Color(255, 4, 6, 11), 0.7f));
		AddHit(rect, "voile", delegate
		{
		});
		DungeonAchievementInfo[] array = completed.Unlocked ?? Array.Empty<DungeonAchievementInfo>();
		DungeonAchievementInfo[] array2 = completed.Progress ?? Array.Empty<DungeonAchievementInfo>();
		int num = Math.Min(4, array.Length);
		int num2 = (num + 1) / 2;
		float num3 = ((num2 == 0) ? 24f : ((float)(num2 * 72 + (num2 - 1) * 8)));
		float num4 = ((array2.Length == 0) ? 0f : 64f);
		bool flag = completed.TitleId != Guid.Empty && DatabaseObject<TitleDescriptor>.Get(completed.TitleId) != null;
		float num5 = 226f + num3 + num4 + 12f + 70f + (float)(flag ? 56 : 0) + 22f + 48f + 18f;
		float num6 = MathF.Max(20f, (900f - num5) / 2f);
		Rectangle rect2 = _d.R(Left, num6, 820f, num5);
		HouseArt.Window(renderer, rect2, _fade);
		AddHit(rect2, "carte", delegate
		{
		});
		Color color = Dungeon.DifficultyColor(completed.Difficulty);
		string text = ((completed.Players <= 1) ? Strings.Dungeons.EndSolo.ToString() : Strings.Dungeons.EndGroup.ToString(completed.Players));
		string text2 = Strings.Dungeons.EndSubtitle.ToString(Dungeon.DifficultyName(completed.Difficulty), text).ToUpperInvariant();
		TechniqueArt.SpacedCenter(renderer, TechniqueArt.Label, Pt(12f), text2, rect2.X + rect2.Width / 2, _d.Y(num6 + 30f), _d.Em(0.22f, 12f), A(color));
		string text3 = (dungeonDescriptor?.Name ?? Strings.Dungeons.EndTitle.ToString()).ToUpperInvariant();
		Point point = TechniqueArt.Measure(renderer, TechniqueArt.Display, Pt(52f), text3);
		TechniqueArt.Styled(renderer, TechniqueArt.Display, Pt(52f), text3, rect2.X + (rect2.Width - point.X) / 2, _d.Y(num6 + 44f), new TechniqueArt.Stops(TechniqueArt.White, 0.3f, new Color(255, 255, 227, 154), 0.6f, TechniqueArt.Ki), _fade);
		DrawStats(renderer, completed, num6 + 114f);
		int num7 = array.Sum((DungeonAchievementInfo achievement) => achievement.Points);
		SectionTitle(renderer, Strings.Dungeons.EndAchievements, (num7 > 0) ? Strings.Dungeons.EndAchievementsPoints.ToString(num7) : null, num6 + 213f);
		float num8 = num6 + 226f;
		if (num2 == 0)
		{
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(13f), Strings.Dungeons.EndNoAchievement, _d.X(Left + 20f), _d.Y(num8 + 12f), A(TechniqueArt.Dim));
		}
		long num9 = Timing.Global.MillisecondsUtc - Dungeon.CompletedAt;
		float num10 = 386f;
		for (int num11 = 0; num11 < num; num11++)
		{
			float num12 = Math.Clamp((float)(num9 - 250 - num11 * 150) / 500f, 0f, 1f);
			Rectangle rect3 = _d.R(Left + 20f + (float)(num11 % 2) * (num10 + 8f), num8 + (float)(num11 / 2 * 80) + (1f - num12) * 10f, num10, 72f);
			DrawObtained(renderer, completed, array[num11], rect3, num12);
		}
		num8 += num3;
		if (array2.Length != 0)
		{
			num8 += 8f;
			for (int num13 = 0; num13 < Math.Min(2, array2.Length); num13++)
			{
				DrawSuite(renderer, completed, array2[num13], _d.R(Left + 20f + (float)num13 * (num10 + 8f), num8, num10, 56f));
			}
			num8 += 56f;
		}
		DrawChest(renderer, completed, num8 + 12f);
		num8 += 82f;
		if (flag)
		{
			DrawUnlock(renderer, DatabaseObject<TitleDescriptor>.Get(completed.TitleId).Name, num8);
			num8 += 56f;
		}
		long seconds = Math.Max(0L, completed.ExitInSeconds - (Timing.Global.MillisecondsUtc - Dungeon.CompletedAt) / 1000);
		float num14 = num8 + 22f;
		string text4 = Strings.Dungeons.EndExit.ToString();
		string text5 = Strings.Dungeons.EndStay.ToString();
		float num15 = (float)TechniqueArt.SpacedWidth(renderer, TechniqueArt.Heavy, Pt(20f), text4.ToUpperInvariant(), _d.Em(0.06f, 20f)) / _d.S + 60f;
		float num16 = (float)TechniqueArt.Measure(renderer, TechniqueArt.Label, Pt(14f), text5).X / _d.S + 40f;
		Rectangle rect4 = _d.R(Left + 820f - 20f - num15, num14, num15, 48f);
		Rectangle rect5 = _d.R(Left + 820f - 20f - num15 - 10f - num16, num14, num16, 48f);
		string text6 = Strings.Dungeons.EndFooter.ToString(Dungeon.Clock(seconds));
		int num17 = _d.X(Left + 20f);
		List<string> list = TechniqueArt.Wrap(renderer, TechniqueArt.Body, Pt(13f), text6, rect5.X - num17 - _d.L(16f), 2);
		int num18 = _d.Y(num14 + 24f) - (list.Count - 1) * _d.L(9f);
		foreach (string item in list)
		{
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(13f), item, num17, num18, A(TechniqueArt.Soft));
			num18 += _d.L(18f);
		}
		HouseArt.Hollow(renderer, _d, rect5, text5, IsHot("rester"), _fade);
		AddHit(rect5, "rester", Hide);
		HouseArt.Cta(renderer, _d, rect4, text4, enabled: true, IsHot("sortir"), _fade);
		AddHit(rect4, "sortir", delegate
		{
			PacketSender.SendDungeonLeave();
			Hide();
		});
	}

	private void SectionTitle(Intersect.Client.Framework.Gwen.Renderer.Base renderer, string title, string? note, float centerY)
	{
		TechniqueArt.Spaced(renderer, TechniqueArt.Label, Pt(12f), title.ToUpperInvariant(), _d.X(Left + 20f), _d.Y(centerY), _d.Em(0.18f, 12f), A(TechniqueArt.Dim));
		if (!string.IsNullOrEmpty(note))
		{
			TechniqueArt.TextRight(renderer, TechniqueArt.Heavy, Pt(14f), note, _d.X(Left + 820f - 20f), _d.Y(centerY), A(TechniqueArt.Gold));
		}
	}

	private void DrawStats(Intersect.Client.Framework.Gwen.Renderer.Base renderer, DungeonCompletePacket done, float top)
	{
		Rectangle rect = _d.R(Left + 20f, top, 780f, 82f);
		TechniqueArt.Outline(renderer, rect, A(TechniqueArt.Line));
		int num = Math.Max(0, done.BeansMax - done.BeansLeft);
		(string, string, string, Color)[] array = new(string, string, string, Color)[4]
		{
			(Strings.Dungeons.EndTime, DungeonAchievementRules.Clock(done.Seconds), done.NewRecord ? Strings.Dungeons.EndNewRecord.ToString() : Strings.Dungeons.EndBest.ToString(DungeonAchievementRules.Clock(done.BestSeconds)), done.NewRecord ? TechniqueArt.Green : TechniqueArt.Soft),
			(Strings.Dungeons.EndBeans, $"{done.BeansLeft} / {done.BeansMax}", (num == 0) ? Strings.Dungeons.EndNoKo.ToString() : Strings.Dungeons.EndBeansDetail.ToString(num), TechniqueArt.Soft),
			(Strings.Dungeons.EndKills, $"{done.Kills}", Strings.Dungeons.EndKillsDetail, TechniqueArt.Soft),
			(Strings.Dungeons.EndExperience, "+" + MarketWindow.FormatAmount(done.KillExperience), Strings.Dungeons.EndExperienceDetail.ToString(Dungeon.Multiplier(done.ExperiencePercent), Dungeon.DifficultyName(done.Difficulty)), TechniqueArt.Soft)
		};
		int num2 = rect.Width / array.Length;
		for (int i = 0; i < array.Length; i++)
		{
			(string, string, string, Color) tuple = array[i];
			string item = tuple.Item1;
			string item2 = tuple.Item2;
			string item3 = tuple.Item3;
			Color item4 = tuple.Item4;
			int num3 = rect.X + i * num2;
			if (i > 0)
			{
				TechniqueArt.Fill(renderer, new Rectangle(num3, rect.Y, 1, rect.Height), A(TechniqueArt.Line));
			}
			int x = num3 + _d.L(14f);
			TechniqueArt.Spaced(renderer, TechniqueArt.Label, Pt(12f), item.ToUpperInvariant(), x, rect.Y + _d.L(17f), _d.Em(0.16f, 12f), A(TechniqueArt.Dim));
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Heavy, Pt(28f), item2, x, rect.Y + _d.L(42f), A(TechniqueArt.Text));
			string text = TechniqueArt.Wrap(renderer, TechniqueArt.Body, Pt(12f), item3, num2 - _d.L(20f), 1).FirstOrDefault();
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(12f), text, x, rect.Y + _d.L(66f), A(item4));
		}
	}

	private void DrawObtained(Intersect.Client.Framework.Gwen.Renderer.Base renderer, DungeonCompletePacket done, DungeonAchievementInfo achievement, Rectangle rect, float appear)
	{
		if (!(appear <= 0f))
		{
			float num = _fade * appear;
			TechniqueArt.FillHorizontal(renderer, rect, TechniqueArt.Alpha(TechniqueArt.Gold, 0.14f * num), TechniqueArt.Alpha(TechniqueArt.Gold, 0.07f * num), TechniqueArt.Alpha(TechniqueArt.White, 0.02f * num));
			TechniqueArt.Outline(renderer, rect, TechniqueArt.Alpha(TechniqueArt.Gold, 0.45f * num));
			if (appear < 1f)
			{
				TechniqueArt.GlowRect(renderer, rect, TechniqueArt.Alpha(TechniqueArt.Gold, 0.25f * (1f - appear) * _fade));
			}
			Rectangle rect2 = new Rectangle(rect.X + _d.L(10f), rect.Y + (rect.Height - _d.L(52f)) / 2, _d.L(52f), _d.L(52f));
			DungeonMedal.Draw(renderer, _d, rect2, achievement, dimmed: false, hot: false, num);
			string text = $"+{achievement.Points}";
			int num2 = TechniqueArt.TextRight(renderer, TechniqueArt.Heavy, Pt(22f), text, rect.Right - _d.L(14f), rect.Y + rect.Height / 2, TechniqueArt.Alpha(TechniqueArt.Gold, num));
			int num3 = rect2.Right + _d.L(12f);
			int width = rect.Right - _d.L(14f) - num2 - _d.L(10f) - num3;
			string text2 = TechniqueArt.Wrap(renderer, TechniqueArt.Heavy, Pt(19f), achievement.Name, width, 1).FirstOrDefault();
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Heavy, Pt(19f), text2, num3, rect.Y + _d.L(27f), TechniqueArt.Alpha(TechniqueArt.Text, num));
			string text3 = TechniqueArt.Wrap(renderer, TechniqueArt.Body, Pt(12f), ObtainedDetail(done, achievement), width, 1).FirstOrDefault();
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(12f), text3, num3, rect.Y + _d.L(48f), TechniqueArt.Alpha(TechniqueArt.Soft, num));
		}
	}

	private static string ObtainedDetail(DungeonCompletePacket done, DungeonAchievementInfo achievement)
	{
		switch (achievement.Feat)
		{
		case DungeonFeat.CompleteNormal:
		case DungeonFeat.CompleteHard:
		case DungeonFeat.CompleteHeroic:
			if (!achievement.Custom)
			{
				return Strings.Dungeons.DetailComplete.ToString(Dungeon.DifficultyName(done.Difficulty));
			}
			break;
		case DungeonFeat.NoBeans:
			if (!achievement.Custom)
			{
				return Strings.Dungeons.DetailNoBeans;
			}
			break;
		case DungeonFeat.UnderTarget:
			if (!achievement.Custom)
			{
				return Strings.Dungeons.DetailUnderTarget.ToString(DungeonAchievementRules.Clock(done.Seconds), DungeonAchievementRules.Clock(achievement.Seconds));
			}
			break;
		case DungeonFeat.Solo:
			if (!achievement.Custom)
			{
				return Strings.Dungeons.DetailSolo.ToString(Dungeon.DifficultyName(done.Difficulty));
			}
			break;
		case DungeonFeat.Regular:
			if (!achievement.Custom)
			{
				return Strings.Dungeons.DetailRegular.ToString(10);
			}
			break;
		}
		return achievement.Description;
	}

	private void DrawSuite(Intersect.Client.Framework.Gwen.Renderer.Base renderer, DungeonCompletePacket done, DungeonAchievementInfo achievement, Rectangle rect)
	{
		TechniqueArt.Fill(renderer, rect, A(TechniqueArt.White, 0.03f));
		TechniqueArt.Outline(renderer, rect, A(TechniqueArt.Line));
		Rectangle rect2 = new Rectangle(rect.X + _d.L(8f), rect.Y + (rect.Height - _d.L(40f)) / 2, _d.L(40f), _d.L(40f));
		DungeonMedal.Draw(renderer, _d, rect2, achievement, dimmed: false, hot: false, _fade);
		int num = rect2.Right + _d.L(10f);
		int num2 = rect.Right - _d.L(12f);
		bool flag = achievement.Objective > 1;
		string text = (flag ? $"{achievement.Value} / {achievement.Objective}" : Strings.Dungeons.SuiteMissed.ToString());
		int num3 = TechniqueArt.TextRight(renderer, TechniqueArt.Label, Pt(12f), text, num2, rect.Y + _d.L(19f), A(TechniqueArt.Soft));
		string text2 = TechniqueArt.Wrap(renderer, TechniqueArt.Heavy, Pt(15f), achievement.Name, num2 - num3 - _d.L(8f) - num, 1).FirstOrDefault();
		TechniqueArt.TextMiddle(renderer, TechniqueArt.Heavy, Pt(15f), text2, num, rect.Y + _d.L(19f), A(TechniqueArt.Text));
		if (flag)
		{
			Rectangle rect3 = new Rectangle(num, rect.Y + _d.L(35f), num2 - num, Math.Max(2, _d.L(5f)));
			TechniqueArt.Fill(renderer, rect3, A(TechniqueArt.White, 0.08f));
			float num4 = Math.Clamp((float)achievement.Value / (float)Math.Max(1L, achievement.Objective), 0f, 1f);
			TechniqueArt.Fill(renderer, new Rectangle(rect3.X, rect3.Y, (int)((float)rect3.Width * num4), rect3.Height), A(TechniqueArt.Gold));
		}
		else
		{
			string text3 = TechniqueArt.Wrap(renderer, TechniqueArt.Body, Pt(12f), MissDetail(achievement), num2 - num, 1).FirstOrDefault();
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(12f), text3, num, rect.Y + _d.L(38f), A(KiLight));
		}
	}

	private static string MissDetail(DungeonAchievementInfo achievement)
	{
		Func<int, string> func = DungeonAchievementRules.Clock;
		switch (achievement.Feat)
		{
		case DungeonFeat.NpcUntouched:
			if (achievement.Actual > 0)
			{
				LocalizedString missHits = Strings.Dungeons.MissHits;
				object obj = achievement.Actual;
				string text = (BossAbilityKind)achievement.MissKind switch
				{
					BossAbilityKind.Charge => Strings.Dungeons.MissByCharge.ToString(), 
					BossAbilityKind.RockFall => Strings.Dungeons.MissByRocks.ToString(), 
					BossAbilityKind.EyeBeam => Strings.Dungeons.MissByEyeBeam.ToString(), 
					BossAbilityKind.Fissures => Strings.Dungeons.MissByFissures.ToString(), 
					BossAbilityKind.GoldRain => Strings.Dungeons.MissByGoldRain.ToString(), 
					_ => Strings.Dungeons.MissByShockwave.ToString(), 
				};
				return missHits.ToString(obj, text);
			}
			break;
		case DungeonFeat.RoomUnderSeconds:
			if (achievement.Actual > 0)
			{
				return Strings.Dungeons.MissRoomSeconds.ToString(func((int)achievement.Actual), func(achievement.Seconds));
			}
			break;
		case DungeonFeat.RoomWithoutKo:
			if (achievement.Actual > 0)
			{
				return Strings.Dungeons.MissRoomKo.ToString(achievement.Actual);
			}
			break;
		case DungeonFeat.UnderTarget:
			if (!achievement.Custom)
			{
				return Strings.Dungeons.MissUnderTarget.ToString(func((int)achievement.Actual), func(achievement.Seconds));
			}
			break;
		case DungeonFeat.NoBeans:
			if (!achievement.Custom)
			{
				return Strings.Dungeons.MissNoBeans.ToString(achievement.Actual);
			}
			break;
		}
		return Strings.Dungeons.MissGeneric.ToString();
	}

	private void DrawChest(Intersect.Client.Framework.Gwen.Renderer.Base renderer, DungeonCompletePacket done, float top)
	{
		Rectangle rect = _d.R(Left + 20f, top, 780f, 70f);
		TechniqueArt.Fill(renderer, rect, A(TechniqueArt.White, 0.03f));
		TechniqueArt.Outline(renderer, rect, A(done.ChestOpened ? TechniqueArt.Alpha(TechniqueArt.Gold, 0.4f) : TechniqueArt.Line));
		int num = rect.X + _d.L(14f);
		int num2 = rect.Y + rect.Height / 2;
		string text = Strings.Dungeons.ChestTitle.ToString();
		int num3 = TechniqueArt.TextMiddle(renderer, TechniqueArt.Heavy, Pt(17f), text, num, num2, A(done.ChestOpened ? TechniqueArt.Text : TechniqueArt.Dim));
		int num4 = num + num3 + _d.L(14f);
		if (!done.ChestOpened)
		{
			string text2 = Strings.Dungeons.EndChestClosed.ToString(Dungeon.Duration(done.ChestReadyInSeconds));
			string text3 = TechniqueArt.Wrap(renderer, TechniqueArt.Body, Pt(13f), text2, rect.Right - _d.L(14f) - num4, 1).FirstOrDefault();
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(13f), text3, num4, num2, A(TechniqueArt.Soft));
			return;
		}
		int num5 = _d.L(49f);
		int num6 = _d.L(6f);
		long num7 = Timing.Global.MillisecondsUtc - Dungeon.CompletedAt;
		List<(Action<Rectangle>, string, Color)> list = new List<(Action<Rectangle>, string, Color)>();
		if (done.Zeni > 0)
		{
			list.Add((delegate(Rectangle box)
			{
				HouseArt.Zeni(renderer, Inner(box), (int)(Timing.Global.MillisecondsUtc / 200), _fade);
			}, MarketWindow.FormatAmount(done.Zeni), TechniqueArt.Line2));
		}
		if (done.Experience > 0)
		{
			list.Add((delegate(Rectangle box)
			{
				TechniqueArt.TextCenter(renderer, TechniqueArt.Display, Pt(15f), "EXP", box.X + box.Width / 2, box.Y + box.Height / 2 - _d.L(4f), A(TechniqueArt.Cyan));
			}, MarketWindow.FormatAmount(done.Experience), TechniqueArt.Line2));
		}
		DungeonLootInfo[] array = done.Items ?? Array.Empty<DungeonLootInfo>();
		foreach (DungeonLootInfo dungeonLootInfo in array)
		{
			ItemDescriptor itemDescriptor = DatabaseObject<ItemDescriptor>.Get(dungeonLootInfo.ItemId);
			if (itemDescriptor != null)
			{
				IGameTexture icon = GameContentManager.Current.GetTexture(TextureType.Item, itemDescriptor.Icon);
				list.Add((delegate(Rectangle box)
				{
					SaiyanHud.Draw(renderer, icon, Inner(box).X, Inner(box).Y, Inner(box).Width, Inner(box).Height, A(TechniqueArt.White));
				}, (dungeonLootInfo.Quantity > 1) ? $"{dungeonLootInfo.Quantity}" : string.Empty, TechniqueArt.Line2));
			}
		}
		if (list.Count == 0)
		{
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(13f), Strings.Dungeons.EndNothing, num4, num2, A(TechniqueArt.Dim));
			return;
		}
		for (int num9 = 0; num9 < list.Count; num9++)
		{
			if (num4 + num5 > rect.Right - _d.L(14f))
			{
				break;
			}
			(Action<Rectangle>, string, Color) tuple = list[num9];
			Action<Rectangle> item = tuple.Item1;
			string item2 = tuple.Item2;
			Color item3 = tuple.Item3;
			float num10 = Math.Clamp((float)(num7 - 600 - num9 * 90) / 300f, 0f, 1f);
			Rectangle rectangle = new Rectangle(num4, num2 - num5 / 2 + (int)((1f - num10) * (float)_d.L(4f)), num5, num5);
			TechniqueArt.Fill(renderer, rectangle, A(new Color(255, 10, 18, 32), num10));
			TechniqueArt.Outline(renderer, rectangle, A(item3, num10));
			if (num10 > 0f)
			{
				item(rectangle);
			}
			if (!string.IsNullOrEmpty(item2))
			{
				TechniqueArt.TextRight(renderer, TechniqueArt.Label, Pt(12f), item2, rectangle.Right - _d.L(3f), rectangle.Bottom - _d.L(8f), A(TechniqueArt.Text, num10));
			}
			num4 += num5 + num6;
		}
	}

	private Rectangle Inner(Rectangle box)
	{
		return new Rectangle(box.X + (box.Width - _d.L(32f)) / 2, box.Y + (box.Height - _d.L(32f)) / 2, _d.L(32f), _d.L(32f));
	}

	private void DrawUnlock(Intersect.Client.Framework.Gwen.Renderer.Base renderer, string titleName, float top)
	{
		string text = Strings.Dungeons.EndTitleUnlocked.ToString().ToUpperInvariant();
		int val = TechniqueArt.SpacedWidth(renderer, TechniqueArt.Label, Pt(12f), text, _d.Em(0.14f, 12f));
		int x = TechniqueArt.Measure(renderer, TechniqueArt.Heavy, Pt(17f), titleName).X;
		int w = _d.L(44f) + Math.Max(val, x) + _d.L(20f);
		Rectangle rect = new Rectangle(_d.X(Left + 20f), _d.Y(top + 10f), w, _d.L(46f));
		TechniqueArt.Fill(renderer, rect, A(Dungeon.Violet, 0.08f));
		TechniqueArt.Outline(renderer, rect, A(Dungeon.Violet, 0.35f));
		TechniqueArt.Glow(renderer, rect.X + _d.L(22f), (float)rect.Y + (float)rect.Height / 2f, _d.L(18f), A(Dungeon.Violet, 0.5f));
		DungeonHud.Diamond(renderer, rect.X + _d.L(22f) - _d.L(8f), rect.Y + rect.Height / 2, _d.L(16f), A(Dungeon.Violet));
		TechniqueArt.Spaced(renderer, TechniqueArt.Label, Pt(12f), text, rect.X + _d.L(44f), rect.Y + _d.L(14f), _d.Em(0.14f, 12f), A(TechniqueArt.Dim));
		TechniqueArt.TextMiddle(renderer, TechniqueArt.Heavy, Pt(17f), titleName, rect.X + _d.L(44f), rect.Y + _d.L(32f), A(TechniqueArt.Text));
	}
}
