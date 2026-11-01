using System;
using System.Collections.Generic;
using System.Linq;
using Intersect.Client.Dungeons;
using Intersect.Client.Entities;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.Framework.Gwen.Renderer;
using Intersect.Client.General;
using Intersect.Client.Interface.Game.Dungeons;
using Intersect.Client.Interface.Game.EntityPanel;
using Intersect.Client.Interface.Game.Housing;
using Intersect.Client.Interface.Game.Techniques;
using Intersect.Client.Localization;
using Intersect.Client.Networking;
using Intersect.Framework.Core;
using Intersect.Framework.Core.GameObjects.Dungeons;
using Intersect.Framework.Core.GameObjects.PlayerClass;
using Intersect.Framework.Core.GameObjects.Rankings;
using Intersect.Framework.Core.GameObjects.Titles;
using Intersect.Localization;
using Intersect.Models;
using Intersect.Network.Packets.Server;

namespace Intersect.Client.Interface.Game.Rankings;

internal sealed class RankingsScreen : CharteScreen
{
	private readonly record struct RailEntry(string Family, Color Tint, string Label, RankingQuery Query);

	private const float BodyTop = 96f;

	private const float BodyHeight = 768f;

	private const float RailLeft = 36f;

	private const float RailWidth = 250f;

	private const float CenterLeft = 298f;

	private const float CenterWidth = 930f;

	private const float FicheLeft = 1240f;

	private const float FicheWidth = 324f;

	private const float RowHeight = 41f;

	private const int VisibleRows = 7;

	private static readonly Color Silver = new Color(255, 207, 216, 230);

	private static readonly Color Bronze = new Color(255, 224, 160, 112);

	private static readonly Color Violet = new Color(255, 185, 140, 255);

	private static readonly Color KiLight = new Color(255, 255, 179, 92);

	private static readonly Color BodyLight = new Color(255, 196, 204, 220);

	private RankingQuery _query = RankingQuery.Of(RankingBoard.Power, RankingPeriod.Week);

	private int _seenVersion = -1;

	private int _scroll;

	private Guid _selected;

	private Rectangle _listRect;

	private long _refreshAt;

	public override bool IsBlockingInput => !base.IsHidden;

	private RankingPacket? Packet
	{
		get
		{
			RankingPacket current = RankingState.Current;
			if (current == null || !(current.Query.Key == _query.Key))
			{
				return null;
			}
			return current;
		}
	}

	public RankingsScreen(Canvas gameCanvas)
		: base(gameCanvas, "RankingsScreen")
	{
		Interface.InputBlockingComponents.Add(this);
	}

	public override void Show()
	{
		base.Show();
		RankingState.Request(_query, force: true);
	}

	public void Toggle()
	{
		if (IsVisible())
		{
			Hide();
		}
		else
		{
			Show();
		}
	}

	public void Update()
	{
		if (base.IsHidden)
		{
			return;
		}
		FitCanvas();
		long millisecondsUtc = Timing.Global.MillisecondsUtc;
		if (RankingState.Version != _seenVersion)
		{
			_seenVersion = RankingState.Version;
			RankingPacket current = RankingState.Current;
			if (current != null && current.Query.Key == _query.Key)
			{
				_refreshAt = millisecondsUtc + (current.Ready ? ((long)(current.NextInSeconds + 5) * 1000L) : 15000);
			}
		}
		if (_refreshAt > 0 && millisecondsUtc >= _refreshAt)
		{
			_refreshAt = 0L;
			RankingState.Request(_query, force: true);
		}
	}

	private void Choose(RankingQuery query)
	{
		_query = query;
		_scroll = 0;
		_selected = Guid.Empty;
		RankingState.Request(query);
	}

	protected override bool OnMouseWheeled(int delta)
	{
		Point point = LocalMouse();
		if (!base.IsHidden && _listRect.Contains(point.X, point.Y))
		{
			RankingPacket packet = Packet;
			if (packet != null)
			{
				int max = Math.Max(0, packet.Entries.Length - 3 - 7);
				_scroll = Math.Clamp(_scroll + ((delta > 0) ? (-2) : 2), 0, max);
				return true;
			}
		}
		return base.OnMouseWheeled(delta);
	}

	protected override void DrawScreen(Intersect.Client.Framework.Gwen.Renderer.Base renderer, long now)
	{
		DrawBackdrop(renderer, TechniqueArt.Ki);
		RankingPacket packet = Packet;
		List<Gauge> list = new List<Gauge>();
		string label = Strings.Rankings.GaugePlace;
		RankingEntryInfo rankingEntryInfo = packet?.Me;
		list.Add(new Gauge(label, (rankingEntryInfo != null) ? Strings.Rankings.MenuPlace.ToString(rankingEntryInfo.Rank) : "—", TechniqueArt.Ki));
		List<Gauge> list2 = list;
		if (packet != null && packet.Ready)
		{
			long num = Math.Max(0L, packet.NextInSeconds - (Timing.Global.MillisecondsUtc - RankingState.ReceivedAt) / 1000);
			list2.Add(new Gauge(Strings.Rankings.GaugeNext, DungeonAchievementRules.Clock((int)num), TechniqueArt.Text));
		}
		DrawTopBar(renderer, Strings.Rankings.Title, Strings.Rankings.Thread, list2);
		DrawRail(renderer, packet);
		DrawCenter(renderer, packet);
		DrawFiche(renderer, packet);
	}

	private IEnumerable<RailEntry> RailEntries()
	{
		RankingPeriod period = _query.Period;
		yield return new RailEntry(Strings.Rankings.FamilyCharacter, TechniqueArt.Ki, Strings.Rankings.Level, Keep(RankingBoard.Level, period));
		yield return new RailEntry(Strings.Rankings.FamilyCharacter, TechniqueArt.Ki, Strings.Rankings.Power, Keep(RankingBoard.Power, period));
		foreach (DungeonDescriptor item in from dungeon in DatabaseObject<DungeonDescriptor>.Lookup.Values.OfType<DungeonDescriptor>()
			where dungeon.Rooms.Count > 0
			orderby dungeon.Name
			select dungeon)
		{
			bool flag = _query.Board == RankingBoard.Dungeon && _query.Target == item.Id;
			yield return new RailEntry(Strings.Rankings.FamilyDungeons, TechniqueArt.Red, item.Name, RankingQuery.Of(RankingBoard.Dungeon, period, item.Id, (!flag) ? 1 : _query.Option, flag && _query.Group));
		}
		yield return new RailEntry(Strings.Rankings.FamilyAchievements, TechniqueArt.Gold, Strings.Rankings.Achievements, Keep(RankingBoard.Achievements, period));
		yield return new RailEntry(Strings.Rankings.FamilyProfessions, TechniqueArt.Green, Strings.Rankings.Gathering, Keep(RankingBoard.Gathering, period));
		yield return new RailEntry(Strings.Rankings.FamilyProfessions, TechniqueArt.Green, Strings.Rankings.Crafting, Keep(RankingBoard.Crafting, period));
		yield return new RailEntry(Strings.Rankings.FamilyGuilds, TechniqueArt.Cyan, Strings.Rankings.Guilds, Keep(RankingBoard.Guilds, period));
		yield return new RailEntry(Strings.Rankings.FamilyKarma, Violet, Strings.Rankings.Heroes, Keep(RankingBoard.Heroes, period));
		yield return new RailEntry(Strings.Rankings.FamilyKarma, Violet, Strings.Rankings.Feared, Keep(RankingBoard.Feared, period));
	}

	private RankingQuery Keep(RankingBoard board, RankingPeriod period)
	{
		if (_query.Board == board)
		{
			return _query.With(period);
		}
		if (board - 4 <= RankingBoard.Power)
		{
			return RankingQuery.Of(board, period, default(Guid), -1);
		}
		return RankingQuery.Of(board, period);
	}

	private static bool SameBoard(RankingQuery a, RankingQuery b)
	{
		if (a.Board == b.Board)
		{
			if (a.Board == RankingBoard.Dungeon)
			{
				return a.Target == b.Target;
			}
			return true;
		}
		return false;
	}

	private void DrawRail(Intersect.Client.Framework.Gwen.Renderer.Base renderer, RankingPacket? packet)
	{
		Rectangle rect = _d.R(36f, 96f, 250f, 768f);
		Panel(renderer, rect);
		float num = 111f;
		for (int i = 0; i < 2; i++)
		{
			RankingPeriod period = ((i != 0) ? RankingPeriod.AllTime : RankingPeriod.Week);
			Rectangle rect2 = _d.R(50f + (float)i * num, 110f, num, 36f);
			bool flag = _query.Period == period;
			string key = $"periode-{i}";
			if (flag)
			{
				TechniqueArt.FillVertical(renderer, rect2, A(new Color(255, 255, 227, 154)), A(new Color(255, 216, 140, 28)));
			}
			else
			{
				TechniqueArt.Fill(renderer, rect2, A(new Color(204, 6, 8, 14)));
				TechniqueArt.Outline(renderer, rect2, A(IsHot(key) ? TechniqueArt.Soft : TechniqueArt.Line2));
			}
			string text = ((period == RankingPeriod.Week) ? Strings.Rankings.Week : Strings.Rankings.AllTime).ToString().ToUpperInvariant();
			string text2 = Strings.Rankings.AllTime.ToString().ToUpperInvariant();
			float num2 = 14f;
			while (num2 > 10f && TechniqueArt.SpacedWidth(renderer, TechniqueArt.Heavy, Pt(num2), text2, _d.Em(0.06f, num2)) > rect2.Width - _d.L(20f))
			{
				num2 -= 0.5f;
			}
			TechniqueArt.SpacedCenter(renderer, TechniqueArt.Heavy, Pt(num2), text, rect2.X + rect2.Width / 2, rect2.Y + rect2.Height / 2, _d.Em(0.06f, num2), A(flag ? HouseArt.Dark : TechniqueArt.Soft));
			AddHit(rect2, key, delegate
			{
				Choose(_query.With(period));
			});
		}
		float num3 = 160f;
		string text3 = string.Empty;
		foreach (RailEntry item in RailEntries())
		{
			if (item.Family != text3)
			{
				text3 = item.Family;
				num3 += 8f;
				int num4 = _d.Y(num3 + 8f);
				DungeonHud.Diamond(renderer, _d.X(54f), num4, _d.L(8f), A(item.Tint));
				int num5 = TechniqueArt.Spaced(renderer, TechniqueArt.Label, Pt(12f), text3.ToUpperInvariant(), _d.X(68f), num4, _d.Em(0.18f, 12f), A(TechniqueArt.Dim));
				TechniqueArt.Fill(renderer, new Rectangle(_d.X(76f) + num5, num4, _d.X(272f) - _d.X(76f) - num5, 1), A(TechniqueArt.Line));
				num3 += 20f;
			}
			Rectangle rect3 = _d.R(46f, num3, 230f, 32f);
			bool flag2 = SameBoard(item.Query, _query);
			string key2 = $"rail-{item.Query.Board}-{item.Query.Target:N}";
			if (flag2)
			{
				TechniqueArt.FillHorizontal(renderer, rect3, A(TechniqueArt.Ki, 0.18f), A(TechniqueArt.Ki, 0.08f), A(TechniqueArt.Ki, 0.02f));
				TechniqueArt.Outline(renderer, rect3, A(TechniqueArt.Ki, 0.45f));
				TechniqueArt.Fill(renderer, new Rectangle(rect3.X, rect3.Y, _d.L(3f), rect3.Height), A(TechniqueArt.Ki));
			}
			else if (IsHot(key2))
			{
				TechniqueArt.Fill(renderer, rect3, A(TechniqueArt.White, 0.04f));
				TechniqueArt.Outline(renderer, rect3, A(TechniqueArt.Line));
			}
			int centerY = rect3.Y + rect3.Height / 2;
			DungeonHud.Diamond(renderer, rect3.X + _d.L(12f), centerY, _d.L(7f), A(item.Tint, 0.8f));
			int num6 = PlaceIn(packet, item.Query);
			string text4 = ((num6 > 0) ? Strings.Rankings.MenuPlace.ToString(num6) : "—");
			int num7 = TechniqueArt.TextRight(renderer, TechniqueArt.Heavy, Pt(14f), text4, rect3.Right - _d.L(10f), centerY, A(flag2 ? KiLight : ((num6 > 0 && num6 <= 3) ? TechniqueArt.Gold : TechniqueArt.Dim)));
			string text5 = TechniqueArt.Wrap(renderer, TechniqueArt.Label, Pt(13.5f), item.Label, rect3.Right - _d.L(18f) - num7 - (rect3.X + _d.L(26f)), 1).FirstOrDefault();
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Label, Pt(13.5f), text5, rect3.X + _d.L(26f), centerY, A(flag2 ? TechniqueArt.Text : BodyLight));
			RankingQuery query = item.Query;
			AddHit(rect3, key2, delegate
			{
				Choose(query);
			});
			num3 += 34f;
		}
		if (packet == null || !packet.Ready)
		{
			return;
		}
		long num8 = (Timing.Global.MillisecondsUtc - RankingState.ReceivedAt) / 1000;
		string[] source = new string[2]
		{
			Strings.Rankings.Computed.ToString(Duration(packet.ComputedSecondsAgo + num8)),
			(_query.Period == RankingPeriod.Week) ? Strings.Rankings.WeekReset.ToString(Duration(packet.WeekResetInSeconds - num8)) : Strings.Rankings.SinceStart.ToString()
		};
		int num9 = _d.Y(848f);
		foreach (string item2 in source.Reverse())
		{
			List<string> list = TechniqueArt.Wrap(renderer, TechniqueArt.Body, Pt(12f), item2, _d.L(222f), 2);
			for (int num10 = list.Count - 1; num10 >= 0; num10--)
			{
				TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(12f), list[num10], _d.X(50f), num9 - _d.L(7f), A(TechniqueArt.Dim));
				num9 -= _d.L(15f);
			}
			num9 -= _d.L(4f);
		}
	}

	private static int PlaceIn(RankingPacket? packet, RankingQuery query)
	{
		if (packet == null)
		{
			return 0;
		}
		int[] array = (from place in packet.Places
			where place.Rank > 0 && SameBoard(place.Query, query)
			select place.Rank).ToArray();
		if (array.Length != 0)
		{
			return array.Min();
		}
		return 0;
	}

	private void DrawCenter(Intersect.Client.Framework.Gwen.Renderer.Base renderer, RankingPacket? packet)
	{
		Rectangle rect = _d.R(298f, 96f, 930f, 768f);
		Panel(renderer, rect);
		float num = 320f;
		float x = 1206f;
		bool flag = _query.Period == RankingPeriod.Week;
		string text = BoardName(_query).ToUpperInvariant();
		Point point = TechniqueArt.Measure(renderer, TechniqueArt.Heavy, Pt(32f), text);
		TechniqueArt.TextMiddle(renderer, TechniqueArt.Heavy, Pt(32f), text, _d.X(num), _d.Y(128f), A(TechniqueArt.Text));
		TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(13f), BoardDescription(_query), _d.X(num), _d.Y(156f), A(TechniqueArt.Soft));
		if (packet != null && packet.Ready)
		{
			TechniqueArt.TextRight(renderer, TechniqueArt.Heavy, Pt(22f), MarketWindow.FormatAmount(packet.Total), _d.X(x), _d.Y(126f), A(TechniqueArt.Text));
			TechniqueArt.TextRight(renderer, TechniqueArt.Body, Pt(12f), Strings.Rankings.Classified.ToString(string.Empty).Trim(), _d.X(x), _d.Y(148f), A(TechniqueArt.Soft));
		}
		float num2 = 170f;
		string text2 = RewardLine();
		if (text2 != null)
		{
			int centerY = _d.Y(num2 + 8f);
			DungeonHud.Diamond(renderer, _d.X(num), centerY, _d.L(9f), A(TechniqueArt.Gold));
			string text3 = TechniqueArt.Wrap(renderer, TechniqueArt.Body, Pt(12f), text2, _d.L(870f), 1).FirstOrDefault();
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(12f), text3, _d.X(num + 16f), centerY, A(TechniqueArt.Gold));
			num2 += 20f;
		}
		num2 = DrawFilters(renderer, num, num2 + 6f);
		if (packet == null)
		{
			Centered(renderer, Strings.Rankings.Loading, num2 + 180f);
			return;
		}
		if (!packet.Ready)
		{
			Centered(renderer, Strings.Rankings.NotReady, num2 + 180f);
			return;
		}
		if (packet.Entries.Length == 0)
		{
			Centered(renderer, flag ? Strings.Rankings.EmptyWeek : Strings.Rankings.Empty, num2 + 180f);
			DrawMe(renderer, packet, 796f);
			return;
		}
		DrawPodium(renderer, packet, num2 + 8f);
		float num3 = num2 + 8f + 236f;
		DrawColumns(renderer, num3);
		DrawList(renderer, packet, num3 + 28f);
		DrawMe(renderer, packet, 796f);
	}

	private void Centered(Intersect.Client.Framework.Gwen.Renderer.Base renderer, string text, float centerY)
	{
		TechniqueArt.TextCenter(renderer, TechniqueArt.Label, Pt(15f), text, _d.X(763f), _d.Y(centerY), A(TechniqueArt.Soft));
	}

	private string? RewardLine()
	{
		if (_query.Period != RankingPeriod.Week)
		{
			return null;
		}
		string text = DatabaseObject<TitleDescriptor>.Get(RankingRules.ChampionTitleId)?.Name ?? ((string)Strings.Rankings.CrownWeek);
		string text2 = DatabaseObject<TitleDescriptor>.Get(RankingRules.PodiumTitleId)?.Name ?? Strings.Rankings.PodiumWeek.ToString();
		if (_query.Board == RankingBoard.Dungeon)
		{
			return Strings.Rankings.RewardDungeon.ToString(text, text2);
		}
		RankingBoard board = _query.Board;
		RankingBoard board2 = _query.Board;
		bool flag = board2 - 4 <= RankingBoard.Power;
		RankingQuery query = RankingQuery.Of(board, RankingPeriod.Week, Guid.Empty, flag ? (-1) : 0);
		if (!RankingRules.IsRewarded(query))
		{
			return null;
		}
		return Strings.Rankings.Reward.ToString(text, text2);
	}

	private static string BoardName(RankingQuery query)
	{
		return query.Board switch
		{
			RankingBoard.Level => Strings.Rankings.Level, 
			RankingBoard.Power => Strings.Rankings.Power, 
			RankingBoard.Achievements => Strings.Rankings.Achievements, 
			RankingBoard.Gathering => (query.Option >= 0) ? Professions.Name(query.Option) : ((string)Strings.Rankings.Gathering), 
			RankingBoard.Crafting => (query.Option >= 0) ? Professions.Name(query.Option) : ((string)Strings.Rankings.Crafting), 
			RankingBoard.Guilds => Strings.Rankings.Guilds, 
			RankingBoard.Heroes => Strings.Rankings.Heroes, 
			RankingBoard.Feared => Strings.Rankings.Feared, 
			_ => DatabaseObject<DungeonDescriptor>.Get(query.Target)?.Name ?? string.Empty, 
		};
	}

	private static string BoardDescription(RankingQuery query)
	{
		bool flag = query.Period == RankingPeriod.Week;
		LocalizedString localizedString;
		switch (query.Board)
		{
		case RankingBoard.Level:
			localizedString = (flag ? Strings.Rankings.DescLevelWeek : Strings.Rankings.DescLevel);
			break;
		case RankingBoard.Power:
			localizedString = (flag ? Strings.Rankings.DescPowerWeek : Strings.Rankings.DescPower);
			break;
		case RankingBoard.Achievements:
			localizedString = (flag ? Strings.Rankings.DescAchievementsWeek : Strings.Rankings.DescAchievements);
			break;
		case RankingBoard.Gathering:
		case RankingBoard.Crafting:
			localizedString = (flag ? Strings.Rankings.DescProfessionWeek : Strings.Rankings.DescProfession);
			break;
		case RankingBoard.Guilds:
			localizedString = (flag ? Strings.Rankings.DescGuildsWeek : Strings.Rankings.DescGuilds);
			break;
		case RankingBoard.Heroes:
			localizedString = (flag ? Strings.Rankings.DescHeroesWeek : Strings.Rankings.DescHeroes);
			break;
		case RankingBoard.Feared:
			localizedString = (flag ? Strings.Rankings.DescFearedWeek : Strings.Rankings.DescFeared);
			break;
		default:
			localizedString = (flag ? Strings.Rankings.DescDungeonWeek : Strings.Rankings.DescDungeon);
			break;
		}
		return localizedString;
	}

	private float DrawFilters(Intersect.Client.Framework.Gwen.Renderer.Base renderer, float left, float top)
	{
		List<(string, Color, bool, RankingQuery)> list = new List<(string, Color, bool, RankingQuery)>();
		switch (_query.Board)
		{
		case RankingBoard.Level:
		case RankingBoard.Power:
		case RankingBoard.Achievements:
			list.Add((Strings.Rankings.AllClasses, TechniqueArt.Gold, _query.Target == Guid.Empty, RankingQuery.Of(_query.Board, _query.Period)));
			foreach (ClassDescriptor item8 in from playerClass in DatabaseObject<ClassDescriptor>.Lookup.Values.OfType<ClassDescriptor>()
				orderby playerClass.Name
				select playerClass)
			{
				list.Add((Traduction.Contenu(item8.Name), TechniqueArt.Gold, _query.Target == item8.Id, RankingQuery.Of(_query.Board, _query.Period, item8.Id)));
			}
			break;
		case RankingBoard.Dungeon:
		{
			for (int num3 = 0; num3 < 3; num3++)
			{
				list.Add((Dungeon.DifficultyName((DungeonDifficulty)num3), Dungeon.DifficultyColor((DungeonDifficulty)num3), _query.Option == num3, RankingQuery.Of(RankingBoard.Dungeon, _query.Period, _query.Target, num3, _query.Group)));
			}
			list.Add((string.Empty, TechniqueArt.Line, false, _query));
			list.Add((Strings.Rankings.Solo, TechniqueArt.Gold, !_query.Group, RankingQuery.Of(RankingBoard.Dungeon, _query.Period, _query.Target, _query.Option)));
			list.Add((Strings.Rankings.Group, TechniqueArt.Gold, _query.Group, RankingQuery.Of(RankingBoard.Dungeon, _query.Period, _query.Target, _query.Option, group: true)));
			break;
		}
		case RankingBoard.Gathering:
		case RankingBoard.Crafting:
		{
			int num = ((_query.Board != RankingBoard.Gathering) ? 5 : 0);
			int num2 = ((_query.Board == RankingBoard.Gathering) ? 5 : 4);
			list.Add((Strings.Rankings.AllProfessions, TechniqueArt.Green, _query.Option < 0, RankingQuery.Of(_query.Board, _query.Period, default(Guid), -1)));
			for (int i = num; i < num + num2; i++)
			{
				string item = Professions.Name(i);
				Color item2 = Professions.Tint(i);
				bool item3 = _query.Option == i;
				RankingBoard board = _query.Board;
				RankingPeriod period = _query.Period;
				int option = i;
				list.Add((item, item2, item3, RankingQuery.Of(board, period, default(Guid), option)));
			}
			break;
		}
		default:
			return top + 4f;
		}
		int num4 = _d.L(886f);
		float size = 12.5f;
		if (list.Sum<(string, Color, bool, RankingQuery)>(((string Label, Color Tint, bool Active, RankingQuery Query) chip) => Width(chip.Label, size) + _d.L(6f)) > num4)
		{
			size = 11f;
		}
		int num5 = _d.X(left);
		int num6 = _d.Y(top);
		int num7 = _d.L(30f);
		int num8 = 0;
		foreach (var item9 in list)
		{
			string item4 = item9.Item1;
			Color item5 = item9.Item2;
			bool item6 = item9.Item3;
			RankingQuery item7 = item9.Item4;
			int num9 = Width(item4, size);
			if (item4.Length == 0)
			{
				TechniqueArt.Fill(renderer, new Rectangle(num5 + num9 / 2, num6 + _d.L(5f), 1, num7 - _d.L(10f)), A(TechniqueArt.Line2));
				num5 += num9 + _d.L(6f);
				continue;
			}
			Rectangle rect = new Rectangle(num5, num6, num9, num7);
			string key = $"filtre-{num8++}";
			if (item6)
			{
				TechniqueArt.Fill(renderer, rect, A(item5, 0.14f));
				TechniqueArt.Outline(renderer, rect, A(item5));
			}
			else
			{
				TechniqueArt.Fill(renderer, rect, A(TechniqueArt.White, 0.03f));
				TechniqueArt.Outline(renderer, rect, A(IsHot(key) ? TechniqueArt.Line2 : TechniqueArt.Line));
			}
			TechniqueArt.TextCenter(renderer, TechniqueArt.Label, Pt(size), item4, rect.X + rect.Width / 2, rect.Y + rect.Height / 2, A(item6 ? TechniqueArt.Text : TechniqueArt.Soft));
			RankingQuery chosen = item7;
			AddHit(rect, key, delegate
			{
				Choose(chosen);
			});
			num5 += num9 + _d.L(6f);
		}
		return top + 30f + 6f;
		int Width(string label, float fontSize)
		{
			if (label.Length != 0)
			{
				return TechniqueArt.Measure(renderer, TechniqueArt.Label, Pt(fontSize), label).X + _d.L(22f);
			}
			return _d.L(13f);
		}
	}

	private static Color Medal(int rank)
	{
		return rank switch
		{
			1 => TechniqueArt.Gold, 
			2 => Silver, 
			3 => Bronze, 
			_ => TechniqueArt.Text, 
		};
	}

	private void DrawPodium(Intersect.Client.Framework.Gwen.Renderer.Base renderer, RankingPacket packet, float top)
	{
		float num = 12f;
		float num2 = (886f - num * 2f) / 3.18f;
		float[] array = new float[3]
		{
			num2,
			num2 * 1.18f,
			num2
		};
		int[] array2 = new int[3] { 1, 0, 2 };
		float[] array3 = new float[3] { 196f, 228f, 184f };
		float num3 = 320f;
		bool flag = _query.Period == RankingPeriod.Week;
		for (int i = 0; i < 3; i++)
		{
			int num4 = array2[i];
			float num5 = array[i];
			if (num4 >= packet.Entries.Length)
			{
				num3 += num5 + num;
				continue;
			}
			RankingEntryInfo rankingEntryInfo = packet.Entries[num4];
			Color color = Medal(rankingEntryInfo.Rank);
			Rectangle rect = _d.R(num3, top + 230f - array3[i], num5, array3[i]);
			string key = $"podium-{num4}";
			bool flag2 = SelectedId(packet) == rankingEntryInfo.Id;
			TechniqueArt.FillVertical(renderer, rect, A(color, IsHot(key) ? 0.24f : 0.16f), A(TechniqueArt.White, 0.02f));
			TechniqueArt.Outline(renderer, rect, A(color, flag2 ? 1f : 0.45f));
			if (flag2)
			{
				TechniqueArt.GlowRect(renderer, rect, A(color, 0.16f));
			}
			TechniqueArt.Styled(renderer, TechniqueArt.Display, Pt(44f), $"{rankingEntryInfo.Rank}", rect.X + _d.L(12f), rect.Y + _d.L(2f), TechniqueArt.Stops.Solid(color), _fade);
			if (rankingEntryInfo.Rank == 1)
			{
				string text = ((flag && RewardLine() != null) ? Strings.Rankings.CrownWeek : Strings.Rankings.Crown).ToString().ToUpperInvariant();
				int num6 = TechniqueArt.SpacedWidth(renderer, TechniqueArt.Label, Pt(12f), text, _d.Em(0.14f, 12f)) + _d.L(14f);
				Rectangle rect2 = new Rectangle(rect.Right - _d.L(12f) - num6, rect.Y + _d.L(12f), num6, _d.L(19f));
				TechniqueArt.Fill(renderer, rect2, A(color));
				TechniqueArt.SpacedCenter(renderer, TechniqueArt.Label, Pt(12f), text, rect2.X + rect2.Width / 2, rect2.Y + rect2.Height / 2, _d.Em(0.14f, 12f), A(HouseArt.Dark));
			}
			string sprite = ((_query.Board == RankingBoard.Guilds) ? rankingEntryInfo.LeaderSprite : rankingEntryInfo.Sprite);
			int height = ((rankingEntryInfo.Rank == 1) ? _d.L(118f) : _d.L(92f));
			int num7 = rect.Bottom - _d.L(84f);
			TechniqueArt.Glow(renderer, (float)rect.X + (float)rect.Width / 2f, num7, _d.L(46f), A(color, 0.25f));
			DrawStanding(renderer, sprite, rect.X + rect.Width / 2, num7, height);
			(string Name, (string Text, Color Color) Sub) tuple = NameLines(rankingEntryInfo);
			string item = tuple.Name;
			(string, Color) item2 = tuple.Sub;
			string text2 = TechniqueArt.Wrap(renderer, TechniqueArt.Heavy, Pt(19f), item, rect.Width - _d.L(16f), 1).FirstOrDefault();
			TechniqueArt.TextCenter(renderer, TechniqueArt.Heavy, Pt(19f), text2, rect.X + rect.Width / 2, rect.Bottom - _d.L(68f), A(TechniqueArt.Text));
			if (!string.IsNullOrEmpty(item2.Item1))
			{
				string text3 = TechniqueArt.Wrap(renderer, TechniqueArt.Body, Pt(12f), item2.Item1, rect.Width - _d.L(16f), 1).FirstOrDefault();
				TechniqueArt.TextCenter(renderer, TechniqueArt.Body, Pt(12f), text3, rect.X + rect.Width / 2, rect.Bottom - _d.L(50f), A(item2.Item2));
			}
			string item3 = ValueText(rankingEntryInfo).Value;
			TechniqueArt.TextCenter(renderer, TechniqueArt.Heavy, Pt(25f), item3, rect.X + rect.Width / 2, rect.Bottom - _d.L(24f), A(ValueColor(color)));
			Guid id = rankingEntryInfo.Id;
			AddHit(rect, key, delegate
			{
				_selected = id;
			});
			num3 += num5 + num;
		}
	}

	private Color ValueColor(Color fallback)
	{
		return _query.Board switch
		{
			RankingBoard.Heroes => TechniqueArt.Green, 
			RankingBoard.Feared => TechniqueArt.Red, 
			_ => fallback, 
		};
	}

	private void DrawColumns(Intersect.Client.Framework.Gwen.Renderer.Base renderer, float top)
	{
		Intersect.Client.Framework.Gwen.Renderer.Base renderer2 = renderer;
		RankingsScreen rankingsScreen = this;
		(float Rank, float Evolution, float Player, float Second, float Third, float Value) tuple = Columns();
		float item = tuple.Player;
		float item2 = tuple.Second;
		float item3 = tuple.Third;
		float item4 = tuple.Value;
		RankingBoard board = _query.Board;
		LocalizedString[] array;
		if (board != RankingBoard.Dungeon)
		{
			if (board != RankingBoard.Guilds)
			{
				goto IL_0099;
			}
			array = new LocalizedString[3]
			{
				Strings.Rankings.ColumnGuild,
				Strings.Rankings.ColumnLeader,
				Strings.Rankings.ColumnMembers
			};
		}
		else
		{
			if (!_query.Group)
			{
				goto IL_0099;
			}
			array = new LocalizedString[3]
			{
				Strings.Rankings.ColumnTeam,
				Strings.Rankings.ColumnLeader,
				Strings.Rankings.ColumnGuild
			};
		}
		goto IL_00b9;
		IL_0099:
		array = new LocalizedString[3]
		{
			Strings.Rankings.ColumnPlayer,
			Strings.Rankings.ColumnClass,
			Strings.Rankings.ColumnGuild
		};
		goto IL_00b9;
		IL_00b9:
		LocalizedString[] array2 = array;
		int center = _d.Y(top + 14f);
		Label(Strings.Rankings.ColumnRank, 334f);
		Label(Strings.Rankings.ColumnEvolution, 394f);
		Label(array2[0], item);
		Label(array2[1], item2);
		Label(array2[2], item3);
		LocalizedString localizedString = ((_query.Board == RankingBoard.Dungeon) ? Strings.Rankings.ColumnTime : ((_query.Period == RankingPeriod.Week) ? Strings.Rankings.ColumnGained : Strings.Rankings.ColumnValue));
		TechniqueArt.SpacedRight(renderer2, TechniqueArt.Label, Pt(12f), localizedString.ToString().ToUpperInvariant(), _d.X(item4), center, _d.Em(0.16f, 12f), A(TechniqueArt.Dim));
		TechniqueArt.Fill(renderer2, new Rectangle(_d.X(320f), _d.Y(top + 27f), _d.L(886f), 1), A(TechniqueArt.Line2));
		void Label(string text, float x)
		{
			TechniqueArt.Spaced(renderer2, TechniqueArt.Label, Pt(12f), text.ToUpperInvariant(), _d.X(x), center, _d.Em(0.16f, 12f), A(TechniqueArt.Dim));
		}
	}

	private static (float Rank, float Evolution, float Player, float Second, float Third, float Value) Columns()
	{
		return (Rank: 334f, Evolution: 394f, Player: 462f, Second: 818f, Third: 978f, Value: 1190f);
	}

	private void DrawList(Intersect.Client.Framework.Gwen.Renderer.Base renderer, RankingPacket packet, float top)
	{
		RankingEntryInfo[] array = packet.Entries.Skip(3).ToArray();
		_listRect = _d.R(320f, top, 886f, 287f);
		_scroll = Math.Clamp(_scroll, 0, Math.Max(0, array.Length - 7));
		Rectangle previous = TechniqueArt.BeginClip(renderer, _listRect);
		for (int i = 0; i < 7 && _scroll + i < array.Length; i++)
		{
			RankingEntryInfo entry = array[_scroll + i];
			Rectangle rect = _d.R(320f, top + (float)i * 41f, 886f, 41f);
			DrawRow(renderer, entry, rect, isMe: false, null);
		}
		TechniqueArt.EndClip(renderer, previous);
		if (array.Length > 7)
		{
			Rectangle rect2 = new Rectangle(_listRect.Right - _d.L(3f), _listRect.Y, _d.L(3f), _listRect.Height);
			TechniqueArt.Fill(renderer, rect2, A(TechniqueArt.White, 0.05f));
			int num = Math.Max(_d.L(24f), rect2.Height * 7 / array.Length);
			int y = rect2.Y + (rect2.Height - num) * _scroll / Math.Max(1, array.Length - 7);
			TechniqueArt.Fill(renderer, new Rectangle(rect2.X, y, rect2.Width, num), A(TechniqueArt.Line2));
		}
	}

	private void DrawRow(Intersect.Client.Framework.Gwen.Renderer.Base renderer, RankingEntryInfo entry, Rectangle rect, bool isMe, string? gap)
	{
		(float Rank, float Evolution, float Player, float Second, float Third, float Value) tuple = Columns();
		float item = tuple.Rank;
		float item2 = tuple.Evolution;
		float item3 = tuple.Player;
		float item4 = tuple.Second;
		float item5 = tuple.Third;
		float item6 = tuple.Value;
		string key = (isMe ? "moi" : $"ligne-{entry.Rank}");
		bool flag = !isMe && SelectedId(RankingState.Current) == entry.Id;
		if (!isMe)
		{
			Player me = Globals.Me;
			if (me != null && (entry.Id == me.Id || entry.Team.Any((RankingMemberInfo member) => member.PlayerId == me.Id)))
			{
				TechniqueArt.FillHorizontal(renderer, rect, A(TechniqueArt.Ki, 0.2f), A(TechniqueArt.Ki, 0.08f), A(TechniqueArt.Ki, 0.02f));
				TechniqueArt.Fill(renderer, new Rectangle(rect.X, rect.Y, _d.L(3f), rect.Height), A(TechniqueArt.Ki));
				goto IL_01f3;
			}
		}
		if (flag)
		{
			TechniqueArt.Fill(renderer, rect, A(TechniqueArt.White, 0.05f));
			TechniqueArt.Fill(renderer, new Rectangle(rect.X, rect.Y, _d.L(2f), rect.Height), A(TechniqueArt.Text));
		}
		else if (!isMe && IsHot(key))
		{
			TechniqueArt.FillHorizontal(renderer, rect, A(TechniqueArt.Ki, 0.12f), A(TechniqueArt.Ki, 0.05f), A(TechniqueArt.Ki, 0.01f));
		}
		goto IL_01f3;
		IL_01f3:
		if (!isMe)
		{
			TechniqueArt.Fill(renderer, new Rectangle(rect.X, rect.Bottom - 1, rect.Width, 1), A(TechniqueArt.White, 0.05f));
		}
		int num = rect.Y + rect.Height / 2;
		TechniqueArt.TextMiddle(renderer, TechniqueArt.Heavy, Pt(19f), MarketWindow.FormatAmount(entry.Rank), _d.X(item), num, A(isMe ? KiLight : Medal(entry.Rank)));
		DrawEvolution(renderer, entry, _d.X(item2), num);
		(string Name, (string Text, Color Color) Sub) tuple2 = NameLines(entry);
		string item7 = tuple2.Name;
		(string, Color) item8 = tuple2.Sub;
		int num2 = _d.X(item3);
		if (entry.Team.Length != 0)
		{
			for (int num3 = 0; num3 < Math.Min(4, entry.Team.Length); num3++)
			{
				DrawFace(renderer, entry.Team[num3].Sprite, new Rectangle(num2 + num3 * _d.L(22f), num - _d.L(15f), _d.L(30f), _d.L(30f)));
			}
			num2 += Math.Min(4, entry.Team.Length) * _d.L(22f) + _d.L(8f);
		}
		else if (_query.Board != RankingBoard.Guilds)
		{
			DrawFace(renderer, entry.Sprite, new Rectangle(num2, num - _d.L(15f), _d.L(30f), _d.L(30f)));
			num2 += _d.L(40f);
		}
		int num4 = _d.X(item4 - 12f) - num2;
		string text = (isMe ? (item7 + "  ") : item7);
		string text2 = TechniqueArt.Wrap(renderer, TechniqueArt.Label, Pt(14f), text, num4 - (isMe ? _d.L(40f) : 0), 1).FirstOrDefault() ?? string.Empty;
		int num5 = TechniqueArt.TextMiddle(renderer, TechniqueArt.Label, Pt(14f), text2, num2, num - _d.L(7f), A(TechniqueArt.Text));
		if (isMe)
		{
			string text3 = Strings.Rankings.Me.ToString().ToUpperInvariant();
			int w = TechniqueArt.SpacedWidth(renderer, TechniqueArt.Label, Pt(12f), text3, _d.Em(0.14f, 12f)) + _d.L(10f);
			Rectangle rect2 = new Rectangle(num2 + num5 + _d.L(4f), num - _d.L(15f), w, _d.L(15f));
			TechniqueArt.Fill(renderer, rect2, A(TechniqueArt.Ki));
			TechniqueArt.SpacedCenter(renderer, TechniqueArt.Label, Pt(12f), text3, rect2.X + rect2.Width / 2, rect2.Y + rect2.Height / 2, _d.Em(0.14f, 12f), A(HouseArt.Dark));
		}
		string? text4;
		if (!isMe)
		{
			(text4, _) = item8;
		}
		else
		{
			text4 = gap;
		}
		string text5 = text4;
		if (!string.IsNullOrEmpty(text5))
		{
			string text6 = TechniqueArt.Wrap(renderer, TechniqueArt.Body, Pt(12f), text5, num4, 1).FirstOrDefault();
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(12f), text6, num2, num + _d.L(9f), A(isMe ? KiLight : item8.Item2));
		}
		RankingBoard board = _query.Board;
		if (board != RankingBoard.Dungeon)
		{
			if (board != RankingBoard.Guilds)
			{
				goto IL_07f6;
			}
			DrawFace(renderer, entry.LeaderSprite, new Rectangle(_d.X(item4), num - _d.L(13f), _d.L(26f), _d.L(26f)));
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Label, Pt(13f), Clip(renderer, entry.LeaderName, 110f), _d.X(item4 + 34f), num, A(BodyLight));
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Label, Pt(13f), Strings.Rankings.Members.ToString(entry.Members), _d.X(item5), num, A(BodyLight));
		}
		else
		{
			if (entry.Team.Length == 0)
			{
				goto IL_07f6;
			}
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Label, Pt(13f), Clip(renderer, entry.Name, 140f), _d.X(item4), num - _d.L(7f), A(BodyLight));
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(12f), ClassName(entry.ClassId), _d.X(item4), num + _d.L(9f), A(TechniqueArt.Dim));
			DrawGuild(renderer, entry.GuildName, item5, num);
		}
		goto IL_08c0;
		IL_08c0:
		var (text7, text8) = ValueText(entry);
		TechniqueArt.TextRight(renderer, TechniqueArt.Heavy, Pt(19f), text7, _d.X(item6), string.IsNullOrEmpty(text8) ? num : (num - _d.L(6f)), A(ValueColor(isMe ? KiLight : TechniqueArt.Text)));
		if (!string.IsNullOrEmpty(text8))
		{
			TechniqueArt.TextRight(renderer, TechniqueArt.Body, Pt(12f), text8, _d.X(item6), num + _d.L(11f), A(TechniqueArt.Dim));
		}
		if (!isMe)
		{
			Guid id = entry.Id;
			AddHit(rect, key, delegate
			{
				_selected = id;
			});
		}
		return;
		IL_07f6:
		TechniqueArt.TextMiddle(renderer, TechniqueArt.Label, Pt(13f), Clip(renderer, ClassName(entry.ClassId), 140f), _d.X(item4), num - _d.L(7f), A(BodyLight));
		TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(12f), Strings.Rankings.LevelShort.ToString(entry.Level), _d.X(item4), num + _d.L(9f), A(TechniqueArt.Dim));
		DrawGuild(renderer, entry.GuildName, item5, num);
		goto IL_08c0;
	}

	private void DrawGuild(Intersect.Client.Framework.Gwen.Renderer.Base renderer, string guild, float x, int middle)
	{
		bool flag = string.IsNullOrWhiteSpace(guild);
		TechniqueArt.TextMiddle(renderer, TechniqueArt.Label, Pt(13f), flag ? ((string)Strings.Rankings.NoGuild) : Clip(renderer, guild, 150f), _d.X(x), middle, A(flag ? TechniqueArt.Dim : BodyLight));
	}

	private string Clip(Intersect.Client.Framework.Gwen.Renderer.Base renderer, string text, float width)
	{
		return TechniqueArt.Wrap(renderer, TechniqueArt.Label, Pt(13f), text, _d.L(width), 1).FirstOrDefault() ?? string.Empty;
	}

	private void DrawEvolution(Intersect.Client.Framework.Gwen.Renderer.Base renderer, RankingEntryInfo entry, int x, int middle)
	{
		if (entry.IsNew)
		{
			TechniqueArt.Spaced(renderer, TechniqueArt.Label, Pt(12f), Strings.Rankings.New.ToString().ToUpperInvariant(), x, middle, _d.Em(0.1f, 12f), A(TechniqueArt.Cyan));
			return;
		}
		if (entry.Evolution == 0)
		{
			TechniqueArt.Fill(renderer, new Rectangle(x, middle, _d.L(10f), Math.Max(1, _d.L(2f))), A(TechniqueArt.Dim));
			return;
		}
		bool flag = entry.Evolution > 0;
		Color color = (flag ? TechniqueArt.Green : TechniqueArt.Red);
		int num = _d.L(8f);
		for (int i = 0; i < num; i++)
		{
			int val = (flag ? (i * 2 + 1) : ((num - i) * 2 - 1));
			val = Math.Min(val, num * 2 - 1);
			TechniqueArt.Fill(renderer, new Rectangle(x + num - 1 - val / 2, middle - num / 2 + i, val, 1), A(color));
		}
		TechniqueArt.TextMiddle(renderer, TechniqueArt.Label, Pt(12f), $"{Math.Abs(entry.Evolution)}", x + num * 2 + _d.L(4f), middle, A(color));
	}

	private (string Name, (string Text, Color Color) Sub) NameLines(RankingEntryInfo entry)
	{
		if (_query.Board == RankingBoard.Guilds)
		{
			return (Name: entry.Name, Sub: (Strings.Rankings.GuildLeader.ToString(entry.LeaderName), BodyLight));
		}
		if (entry.Team.Length != 0)
		{
			return (Name: $"{entry.Name} +{entry.Team.Length - 1}", Sub: (Strings.Rankings.TeamWith.ToString(string.Join(", ", from member in entry.Team.Skip(1)
				select member.Name)), BodyLight));
		}
		TitleDescriptor titleDescriptor = DatabaseObject<TitleDescriptor>.Get(entry.TitleId);
		if (titleDescriptor != null)
		{
			Color color = titleDescriptor.Color;
			return (Name: entry.Name, Sub: (titleDescriptor.Name, new Color(255, color.R, color.G, color.B)));
		}
		return (Name: entry.Name, Sub: (string.Empty, TechniqueArt.Dim));
	}

	private static string ClassName(Guid classId)
	{
		return Traduction.Contenu(DatabaseObject<ClassDescriptor>.Get(classId)?.Name ?? string.Empty);
	}

	private (string Value, string Detail) ValueText(RankingEntryInfo entry)
	{
		bool flag = _query.Period == RankingPeriod.Week;
		switch (_query.Board)
		{
		case RankingBoard.Level:
			return flag ? (Value: Strings.Rankings.LevelsGained.ToString(entry.Value), Detail: Strings.Rankings.LevelShort.ToString(entry.Detail)) : (Value: Strings.Rankings.LevelShort.ToString(entry.Value), Detail: Strings.Rankings.ExperienceDetail.ToString(Amount(entry.Detail)));
		case RankingBoard.Power:
			return (Value: flag ? ("+" + Amount(entry.Value)) : Amount(entry.Value), Detail: string.Empty);
		case RankingBoard.Achievements:
			return (Value: flag ? Strings.Rankings.PointsGained.ToString(Amount(entry.Value)) : Strings.Rankings.Points.ToString(Amount(entry.Value)), Detail: string.Empty);
		case RankingBoard.Gathering:
		case RankingBoard.Crafting:
			return (Value: flag ? ("+" + Amount(entry.Value)) : Amount(entry.Value), Detail: (_query.Option >= 0) ? Strings.Rankings.LevelShort.ToString(entry.Detail) : Strings.Rankings.ProfessionLevels.ToString(entry.Detail));
		case RankingBoard.Guilds:
			return (Value: flag ? ("+" + Amount(entry.Value)) : Amount(entry.Value), Detail: string.Empty);
		case RankingBoard.Heroes:
			return (Value: "+" + Amount(entry.Value), Detail: Strings.Rankings.Karma);
		case RankingBoard.Feared:
			return (Value: "–" + Amount(Math.Abs(entry.Value)), Detail: Strings.Rankings.Karma);
		default:
			return (Value: DungeonAchievementRules.Clock((int)entry.Value), Detail: string.Empty);
		}
		static string Amount(long value)
		{
			return MarketWindow.FormatAmount(value);
		}
	}

	private void DrawMe(Intersect.Client.Framework.Gwen.Renderer.Base renderer, RankingPacket packet, float top)
	{
		Rectangle rect = _d.R(320f, top, 886f, 52f);
		TechniqueArt.FillHorizontal(renderer, rect, A(TechniqueArt.Ki, 0.2f), A(TechniqueArt.Ki, 0.09f), A(TechniqueArt.Ki, 0.04f));
		TechniqueArt.Outline(renderer, rect, A(TechniqueArt.Ki, 0.6f));
		RankingEntryInfo me = packet.Me;
		if (me == null)
		{
			LocalizedString localizedString = ((_query.Board == RankingBoard.Guilds) ? Strings.Rankings.NotRankedGuild : Strings.Rankings.NotRanked);
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(13f), localizedString, rect.X + _d.L(18f), rect.Y + rect.Height / 2, A(BodyLight));
			return;
		}
		DrawRow(gap: (me.Rank <= 1) ? ((string)Strings.Rankings.GapFirst) : ((_query.Board != RankingBoard.Dungeon) ? Strings.Rankings.GapMore.ToString(MarketWindow.FormatAmount(Math.Max(1L, packet.MeGap)), me.Rank - 1) : Strings.Rankings.GapTime.ToString(DungeonAchievementRules.Clock((int)Math.Max(1L, packet.MeGap)), me.Rank - 1)), renderer: renderer, entry: me, rect: rect, isMe: true);
	}

	private Guid SelectedId(RankingPacket? packet)
	{
		if (packet == null || packet.Query.Key != _query.Key)
		{
			return Guid.Empty;
		}
		if (!(_selected != Guid.Empty) || !packet.Entries.Any((RankingEntryInfo entry) => entry.Id == _selected))
		{
			return packet.Entries.FirstOrDefault()?.Id ?? packet.Me?.Id ?? Guid.Empty;
		}
		return _selected;
	}

	private void DrawFiche(Intersect.Client.Framework.Gwen.Renderer.Base renderer, RankingPacket? packet)
	{
		Rectangle rect = _d.R(1240f, 96f, 324f, 768f);
		Panel(renderer, rect);
		if (packet == null || !packet.Ready)
		{
			return;
		}
		Guid id = SelectedId(packet);
		RankingEntryInfo rankingEntryInfo = packet.Entries.FirstOrDefault((RankingEntryInfo candidate) => candidate.Id == id) ?? packet.Me;
		if (rankingEntryInfo == null)
		{
			List<string> list = TechniqueArt.Wrap(renderer, TechniqueArt.Label, Pt(14f), Strings.Rankings.FicheEmpty, _d.L(284f), 3);
			for (int num = 0; num < list.Count; num++)
			{
				TechniqueArt.TextCenter(renderer, TechniqueArt.Label, Pt(14f), list[num], _d.X(1402f), _d.Y(480f - (float)((list.Count - 1) * 10) + (float)(num * 20)), A(TechniqueArt.Soft));
			}
			return;
		}
		bool flag = _query.Board == RankingBoard.Guilds;
		RankingProfilePacket rankingProfilePacket = (flag ? null : RankingState.ProfileOf(rankingEntryInfo.Id));
		if (!flag)
		{
			RankingState.RequestProfile(rankingEntryInfo.Id);
		}
		Color color = Medal(rankingEntryInfo.Rank);
		Rectangle rect2 = _d.R(1254f, 110f, 296f, 250f);
		TechniqueArt.FillVertical(renderer, rect2, A(new Color(255, 10, 18, 32)), A(new Color(255, 13, 21, 36)));
		TechniqueArt.Glow(renderer, (float)rect2.X + (float)rect2.Width / 2f, rect2.Bottom - _d.L(60f), _d.L(120f), A(color, 0.3f));
		TechniqueArt.Outline(renderer, rect2, A(TechniqueArt.Line));
		string sprite = (flag ? rankingEntryInfo.LeaderSprite : (rankingProfilePacket?.Sprite ?? rankingEntryInfo.Sprite));
		DrawStanding(renderer, sprite, rect2.X + rect2.Width / 2, rect2.Bottom - _d.L(14f), _d.L(210f));
		string text = $"{rankingEntryInfo.Rank}";
		string text2 = BoardName(_query).ToUpperInvariant();
		int val = TechniqueArt.Measure(renderer, TechniqueArt.Display, Pt(26f), text).X + TechniqueArt.SpacedWidth(renderer, TechniqueArt.Label, Pt(12f), text2, _d.Em(0.1f, 12f)) + _d.L(28f);
		Rectangle rect3 = new Rectangle(rect2.X + _d.L(10f), rect2.Y + _d.L(10f), Math.Min(val, rect2.Width - _d.L(20f)), _d.L(34f));
		TechniqueArt.Fill(renderer, rect3, A(new Color(217, 6, 8, 14)));
		TechniqueArt.Outline(renderer, rect3, A(TechniqueArt.Line2));
		int x = TechniqueArt.Measure(renderer, TechniqueArt.Display, Pt(26f), text).X;
		TechniqueArt.Styled(renderer, TechniqueArt.Display, Pt(26f), text, rect3.X + _d.L(9f), rect3.Y + _d.L(2f), TechniqueArt.Stops.Solid(color), _fade);
		string text3 = TechniqueArt.Wrap(renderer, TechniqueArt.Label, Pt(12f), text2, rect3.Right - (rect3.X + _d.L(18f) + x) - _d.L(6f), 1).FirstOrDefault();
		TechniqueArt.Spaced(renderer, TechniqueArt.Label, Pt(12f), text3, rect3.X + _d.L(18f) + x, rect3.Y + rect3.Height / 2, _d.Em(0.1f, 12f), A(TechniqueArt.Soft));
		if (rankingProfilePacket != null && rankingProfilePacket.Found)
		{
			LocalizedString localizedString = (rankingProfilePacket.Online ? Strings.Rankings.Online : Strings.Rankings.Offline);
			TechniqueArt.SpacedRight(renderer, TechniqueArt.Label, Pt(12f), localizedString.ToString().ToUpperInvariant(), rect2.Right - _d.L(12f), rect2.Y + _d.L(24f), _d.Em(0.12f, 12f), A(rankingProfilePacket.Online ? TechniqueArt.Green : TechniqueArt.Dim));
		}
		int x2 = _d.X(1258f);
		int width = _d.L(288f);
		float num2 = 386f;
		string text4 = TechniqueArt.Wrap(renderer, TechniqueArt.Heavy, Pt(28f), rankingEntryInfo.Name, width, 1).FirstOrDefault();
		TechniqueArt.TextMiddle(renderer, TechniqueArt.Heavy, Pt(28f), text4, x2, _d.Y(num2), A(TechniqueArt.Text));
		num2 += 24f;
		if (flag)
		{
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(13f), Strings.Rankings.GuildLeader.ToString(rankingEntryInfo.LeaderName), x2, _d.Y(num2), A(BodyLight));
			num2 += 18f;
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(13f), Strings.Rankings.Members.ToString(rankingEntryInfo.Members), x2, _d.Y(num2), A(TechniqueArt.Soft));
		}
		else
		{
			(string, Color) item = NameLines(rankingEntryInfo).Sub;
			if (!string.IsNullOrEmpty(item.Item1) && rankingEntryInfo.Team.Length == 0)
			{
				TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(13f), item.Item1, x2, _d.Y(num2), A(item.Item2));
				num2 += 18f;
			}
			int num3 = rankingProfilePacket?.Level ?? rankingEntryInfo.Level;
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(13f), Strings.Rankings.ProfileMeta.ToString(ClassName(rankingProfilePacket?.ClassId ?? rankingEntryInfo.ClassId), num3), x2, _d.Y(num2), A(TechniqueArt.Soft));
			num2 += 18f;
			string text5 = rankingProfilePacket?.GuildName ?? rankingEntryInfo.GuildName;
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(13f), string.IsNullOrWhiteSpace(text5) ? ((string)Strings.Rankings.NoGuild) : text5, x2, _d.Y(num2), A(TechniqueArt.Soft));
		}
		num2 += 24f;
		if (flag)
		{
			return;
		}
		Rectangle rect4 = _d.R(1258f, num2, 288f, 52f);
		TechniqueArt.Outline(renderer, rect4, A(TechniqueArt.Line));
		(string, string)[] array = new(string, string)[3]
		{
			(Strings.Rankings.StatLevel.ToString(), $"{rankingProfilePacket?.Level ?? rankingEntryInfo.Level}"),
			(Strings.Rankings.StatPower.ToString(), (rankingProfilePacket != null && rankingProfilePacket.Found) ? MarketWindow.FormatAmount(rankingProfilePacket.Power) : "—"),
			(Strings.Rankings.StatPoints.ToString(), (rankingProfilePacket != null && rankingProfilePacket.Found) ? MarketWindow.FormatAmount(rankingProfilePacket.AchievementPoints) : "—")
		};
		int num4 = rect4.Width / 3;
		for (int num5 = 0; num5 < 3; num5++)
		{
			int num6 = rect4.X + num5 * num4;
			if (num5 > 0)
			{
				TechniqueArt.Fill(renderer, new Rectangle(num6, rect4.Y, 1, rect4.Height), A(TechniqueArt.Line));
			}
			TechniqueArt.Spaced(renderer, TechniqueArt.Label, Pt(12f), array[num5].Item1.ToUpperInvariant(), num6 + _d.L(10f), rect4.Y + _d.L(14f), _d.Em(0.14f, 12f), A(TechniqueArt.Dim));
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Heavy, Pt(18f), array[num5].Item2, num6 + _d.L(10f), rect4.Y + _d.L(35f), A(TechniqueArt.Text));
		}
		num2 += 66f;
		TechniqueArt.Spaced(renderer, TechniqueArt.Label, Pt(12f), Strings.Rankings.OtherPlaces.ToString().ToUpperInvariant(), x2, _d.Y(num2), _d.Em(0.18f, 12f), A(TechniqueArt.Dim));
		num2 += 14f;
		if (rankingProfilePacket == null)
		{
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(12f), Strings.Rankings.Loading, x2, _d.Y(num2 + 14f), A(TechniqueArt.Dim));
		}
		else if (rankingProfilePacket.Places.Length == 0)
		{
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(12f), Strings.Rankings.NoPlaces, x2, _d.Y(num2 + 14f), A(TechniqueArt.Dim));
		}
		else
		{
			RankingPlaceInfo[] places = rankingProfilePacket.Places;
			foreach (RankingPlaceInfo rankingPlaceInfo in places)
			{
				Rectangle rect5 = _d.R(1258f, num2, 288f, 30f);
				TechniqueArt.Fill(renderer, rect5, A(TechniqueArt.White, 0.025f));
				string text6 = Strings.Rankings.MenuPlace.ToString(rankingPlaceInfo.Rank);
				TechniqueArt.TextMiddle(renderer, TechniqueArt.Heavy, Pt(16f), text6, rect5.X + _d.L(8f), rect5.Y + rect5.Height / 2, A((rankingPlaceInfo.Rank <= 3) ? TechniqueArt.Gold : TechniqueArt.Text));
				string text7 = BoardName(rankingPlaceInfo.Query) + ((rankingPlaceInfo.Query.Board == RankingBoard.Dungeon) ? (" · " + Dungeon.DifficultyName((DungeonDifficulty)rankingPlaceInfo.Query.Option)) : string.Empty);
				string text8 = TechniqueArt.Wrap(renderer, TechniqueArt.Label, Pt(12.5f), text7, rect5.Width - _d.L(64f), 1).FirstOrDefault();
				TechniqueArt.TextMiddle(renderer, TechniqueArt.Label, Pt(12.5f), text8, rect5.X + _d.L(56f), rect5.Y + rect5.Height / 2, A(BodyLight));
				num2 += 31f;
			}
		}
		Player me = Globals.Me;
		if (me == null || !(rankingEntryInfo.Id != me.Id) || rankingEntryInfo.Team.Length != 0)
		{
			return;
		}
		float y = 806f;
		Rectangle rect6 = _d.R(1258f, y, 288f, 40f);
		if (rankingProfilePacket != null && rankingProfilePacket.Online)
		{
			float num8 = 140f;
			rect6 = _d.R(1258f, y, num8, 40f);
			Rectangle rect7 = _d.R(1258f + num8 + 8f, y, num8, 40f);
			HouseArt.Hollow(renderer, _d, rect7, Strings.Rankings.Invite, IsHot("inviter"), _fade);
			Guid targetId = rankingEntryInfo.Id;
			AddHit(rect7, "inviter", delegate
			{
				PacketSender.SendPartyInvite(targetId);
			});
		}
		HouseArt.Hollow(renderer, _d, rect6, Strings.Rankings.AddFriend, IsHot("ami"), _fade);
		string friendName = rankingEntryInfo.Name;
		AddHit(rect6, "ami", delegate
		{
			PacketSender.SendAddFriend(friendName);
		});
	}

	private void DrawStanding(Intersect.Client.Framework.Gwen.Renderer.Base renderer, string sprite, int centerX, int feetY, int height)
	{
		(IGameTexture, float, float, float, float)? tuple = DungeonEntryWindow.StandingFrame(sprite);
		if (tuple.HasValue)
		{
			(IGameTexture, float, float, float, float) valueOrDefault = tuple.GetValueOrDefault();
			(IGameTexture, float, float, float, float) tuple2 = valueOrDefault;
			IGameTexture item = tuple2.Item1;
			float item2 = tuple2.Item2;
			float item3 = tuple2.Item3;
			float item4 = tuple2.Item4;
			float item5 = tuple2.Item5;
			int num = Math.Max(1, (int)MathF.Round((float)height / item5));
			int num2 = (int)(item4 * (float)num);
			int num3 = (int)(item5 * (float)num);
			SaiyanHud.Draw(renderer, item, centerX - num2 / 2, feetY - num3, num2, num3, A(TechniqueArt.White), item2 / (float)item.Width, item3 / (float)item.Height, (item2 + item4) / (float)item.Width, (item3 + item5) / (float)item.Height);
		}
	}

	private void DrawFace(Intersect.Client.Framework.Gwen.Renderer.Base renderer, string sprite, Rectangle box)
	{
		TechniqueArt.Fill(renderer, box, A(new Color(255, 10, 18, 32)));
		TechniqueArt.Outline(renderer, box, A(TechniqueArt.Line));
		(IGameTexture, float, float, float, float)? tuple = DungeonEntryWindow.StandingFrame(sprite);
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
			Rectangle rectangle = new Rectangle(box.X + 1, box.Y + 1, box.Width - 2, box.Height - 2);
			SaiyanHud.Draw(renderer, item, rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height, A(TechniqueArt.White), num2, num3, num2 + num / (float)item.Width, num3 + num / (float)item.Height);
		}
	}

	private static string Duration(long seconds)
	{
		seconds = Math.Max(0L, seconds);
		if (seconds >= 86400)
		{
			return Strings.Rankings.DurationDays.ToString(seconds / 86400, seconds / 3600 % 24);
		}
		if (seconds < 3600)
		{
			return Strings.Rankings.DurationMinutes.ToString(Math.Max(1L, (seconds + 59) / 60));
		}
		return Strings.Rankings.DurationHours.ToString(seconds / 3600, $"{seconds / 60 % 60:00}");
	}
}
