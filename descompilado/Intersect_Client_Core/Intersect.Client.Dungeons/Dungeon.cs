using System;
using System.Collections.Generic;
using System.Linq;
using Intersect.Client.Entities;
using Intersect.Client.Framework.Content;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.General;
using Intersect.Client.Interface.Game.Chat;
using Intersect.Client.Interface.Game.Dungeons;
using Intersect.Client.Interface.Game.Techniques;
using Intersect.Client.Localization;
using Intersect.Enums;
using Intersect.Framework.Core;
using Intersect.Framework.Core.GameObjects.Dungeons;
using Intersect.Framework.Core.GameObjects.NPCs;
using Intersect.Models;
using Intersect.Network.Packets.Server;

namespace Intersect.Client.Dungeons;

public static class Dungeon
{
	public readonly record struct Telegraph(DungeonTelegraphPacket Packet, long ReceivedAt)
	{
		public long FiresAt => ReceivedAt + Math.Max(1, Packet.DurationMs);

		public long LastFiresAt
		{
			get
			{
				long firesAt = FiresAt;
				DungeonTelegraphShape[] shapes = Packet.Shapes;
				return firesAt + ((shapes != null && shapes.Length > 0) ? shapes.Max((DungeonTelegraphShape shape) => shape.DelayMs) : 0);
			}
		}

		public BossAbilityKind Kind => (BossAbilityKind)Packet.Kind;

		public long EndsAt
		{
			get
			{
				switch (Kind)
				{
				case BossAbilityKind.EyeBeam:
					if (!Packet.Locked)
					{
						DungeonTelegraphShape[] shapes = Packet.Shapes;
						if (shapes == null || shapes.Length == 0)
						{
							return FiresAt;
						}
					}
					return LastFiresAt + 650 + 3200;
				case BossAbilityKind.Fissures:
					return LastFiresAt + 3500;
				case BossAbilityKind.GoldRain:
					return LastFiresAt + 1000;
				default:
					return LastFiresAt + 280;
				}
			}
		}
	}

	public const long ImpactMs = 280L;

	private static readonly Dictionary<Guid, (DungeonStatusInfo Info, long ReceivedAt)> Statuses = new Dictionary<Guid, (DungeonStatusInfo, long)>();

	private static readonly List<Telegraph> Telegraphs = new List<Telegraph>();

	private static long _nextInteract;

	private static IGameTexture? _icone;

	public static readonly Color Violet = new Color(255, 185, 140, 255);

	public static DungeonRunPacket? Run { get; private set; }

	public static long RunReceivedAt { get; private set; }

	public static DungeonCompletePacket? Completed { get; private set; }

	public static long CompletedAt { get; private set; }

	public static bool InRun
	{
		get
		{
			DungeonRunPacket run = Run;
			if (run != null)
			{
				Guid runId = run.RunId;
				return runId != Guid.Empty;
			}
			return false;
		}
	}

	public static DungeonDescriptor? Current
	{
		get
		{
			DungeonRunPacket run = Run;
			if (run == null)
			{
				return null;
			}
			return DatabaseObject<DungeonDescriptor>.Get(run.DungeonId);
		}
	}

	public static long ElapsedMs
	{
		get
		{
			DungeonRunPacket run = Run;
			if (run != null)
			{
				if (!run.Finished)
				{
					return run.ElapsedMs + Math.Max(0L, Timing.Global.MillisecondsUtc - RunReceivedAt);
				}
				return run.ElapsedMs;
			}
			return 0L;
		}
	}

	public static IReadOnlyList<Telegraph> ActiveTelegraphs => Telegraphs;

	public static IGameTexture? Icone => _icone ?? (_icone = Globals.ContentManager?.GetTexture(TextureType.Misc, "icone_entree_donjon_32x32.png"));

	public static DungeonRoom? CurrentRoom
	{
		get
		{
			DungeonRunPacket run = Run;
			if (run != null && run.RoomIndex >= 0)
			{
				DungeonDescriptor current = Current;
				if (current != null && run.RoomIndex < current.Rooms.Count)
				{
					return current.Rooms[run.RoomIndex];
				}
			}
			return null;
		}
	}

	public static void OnStatuses(DungeonStatusesPacket packet)
	{
		long millisecondsUtc = Timing.Global.MillisecondsUtc;
		Statuses.Clear();
		DungeonStatusInfo[] array = packet.Statuses ?? Array.Empty<DungeonStatusInfo>();
		foreach (DungeonStatusInfo dungeonStatusInfo in array)
		{
			Statuses[dungeonStatusInfo.DungeonId] = (dungeonStatusInfo, millisecondsUtc);
		}
	}

	public static void OnRun(DungeonRunPacket packet)
	{
		bool inRun = InRun;
		Run = ((packet.RunId == Guid.Empty) ? null : packet);
		RunReceivedAt = Timing.Global.MillisecondsUtc;
		if (!InRun)
		{
			Telegraphs.Clear();
			if (inRun)
			{
				DungeonInterface.CloseInside();
			}
		}
		else
		{
			DungeonInterface.CloseEntry();
		}
	}

	public static void OnTelegraph(DungeonTelegraphPacket packet)
	{
		long now = Timing.Global.MillisecondsUtc;
		Telegraphs.RemoveAll((Telegraph telegraph) => now > telegraph.EndsAt || (packet.Locked && !telegraph.Packet.Locked && telegraph.Packet.BossId == packet.BossId && telegraph.Packet.Kind == packet.Kind));
		Telegraphs.Add(new Telegraph(packet, now));
		DungeonWorld.OnTelegraph(packet);
	}

	public static void OnComplete(DungeonCompletePacket packet)
	{
		Completed = packet;
		CompletedAt = Timing.Global.MillisecondsUtc;
		DungeonInterface.OpenEnd();
	}

	public static void OnResult(DungeonResultPacket packet)
	{
		switch (packet.Result)
		{
		case DungeonResult.Entered:
			Chat(Strings.Dungeons.ResultEntered.ToString(packet.Name), success: true);
			break;
		case DungeonResult.NotLeader:
			Chat(Strings.Dungeons.ResultNotLeader.ToString(packet.Name), success: false);
			break;
		case DungeonResult.TooFar:
			Chat(Strings.Dungeons.ResultTooFar, success: false);
			break;
		case DungeonResult.AlreadyInRun:
			Chat(Strings.Dungeons.ResultAlreadyInRun, success: false);
			break;
		case DungeonResult.InvalidDungeon:
			Chat(Strings.Dungeons.ResultInvalid, success: false);
			DungeonInterface.CloseEntry();
			break;
		case DungeonResult.DoorLocked:
			Chat(string.IsNullOrEmpty(packet.Name) ? Strings.Dungeons.ResultDoorLocked.ToString(packet.Amount) : Strings.Dungeons.ResultDoorLockedNpc.ToString(packet.Name), success: false);
			break;
		case DungeonResult.RoomCleared:
			Chat(Strings.Dungeons.ResultRoomCleared.ToString(packet.Name), success: true);
			break;
		case DungeonResult.BeanUsed:
			Chat(Strings.Dungeons.ResultBeanUsed.ToString(packet.Name, packet.Amount), success: false);
			break;
		case DungeonResult.Failed:
			Chat(Strings.Dungeons.ResultFailed.ToString(packet.Name), success: false);
			break;
		case DungeonResult.Left:
			Chat(Strings.Dungeons.ResultLeft.ToString(packet.Name), success: false);
			break;
		case DungeonResult.Rejoined:
			Chat(Strings.Dungeons.ResultRejoined.ToString(packet.Name), success: true);
			break;
		case DungeonResult.LeftBehind:
			Chat(Strings.Dungeons.ResultLeftBehind.ToString(packet.Name), success: false);
			break;
		case DungeonResult.DifficultyLocked:
			Chat(Strings.Dungeons.ResultDifficultyLocked.ToString(packet.Name), success: false);
			break;
		}
	}

	private static void Chat(string text, bool success)
	{
		ChatboxMsg.AddMessage(new ChatboxMsg(text, success ? CustomColors.Alerts.Accepted : CustomColors.Alerts.Declined, ChatMessageType.Notice));
	}

	public static long ChestReadyInSeconds(Guid dungeonId)
	{
		if (!Statuses.TryGetValue(dungeonId, out (DungeonStatusInfo, long) value))
		{
			return 0L;
		}
		long num = (Timing.Global.MillisecondsUtc - value.Item2) / 1000;
		return Math.Max(0L, value.Item1.ChestReadyInSeconds - num);
	}

	public static DungeonStatusInfo? StatusOf(Guid dungeonId)
	{
		if (!Statuses.TryGetValue(dungeonId, out (DungeonStatusInfo, long) value))
		{
			return null;
		}
		return value.Item1;
	}

	public static DungeonDescriptor? EntranceNear(Player me)
	{
		foreach (DungeonDescriptor item in DatabaseObject<DungeonDescriptor>.Lookup.Values.OfType<DungeonDescriptor>())
		{
			if (item.EntranceMapId == me.MapId && item.Rooms.Count > 0 && Math.Max(Math.Abs(me.X - item.EntranceX), Math.Abs(me.Y - item.EntranceY)) <= 2)
			{
				return item;
			}
		}
		return null;
	}

	public static Entity? NearestBoss(Player me)
	{
		Entity result = null;
		int num = int.MaxValue;
		foreach (Entity value in Globals.Entities.Values)
		{
			if (value != null && value.BossEnragePercent > 0 && !(value.MapId != me.MapId) && value.Vital[0] > 0)
			{
				int num2 = Math.Max(Math.Abs(value.X - me.X), Math.Abs(value.Y - me.Y));
				if (num2 <= 16 && num2 < num)
				{
					num = num2;
					result = value;
				}
			}
		}
		return result;
	}

	public static Telegraph? CurrentAlert(Player me)
	{
		long millisecondsUtc = Timing.Global.MillisecondsUtc;
		for (int num = Telegraphs.Count - 1; num >= 0; num--)
		{
			Telegraph value = Telegraphs[num];
			if (value.Packet.MapId == me.MapId && millisecondsUtc < value.LastFiresAt)
			{
				return value;
			}
		}
		return null;
	}

	public static bool TryInteract(Player me)
	{
		if (!InRun)
		{
			DungeonDescriptor dungeonDescriptor = EntranceNear(me);
			if (dungeonDescriptor != null)
			{
				long millisecondsUtc = Timing.Global.MillisecondsUtc;
				if (millisecondsUtc >= _nextInteract)
				{
					_nextInteract = millisecondsUtc + 700;
					DungeonInterface.OpenEntry(dungeonDescriptor);
				}
				return true;
			}
		}
		return false;
	}

	public static void Reset()
	{
		Statuses.Clear();
		Telegraphs.Clear();
		Run = null;
		Completed = null;
		CompletedAt = 0L;
	}

	public static string DifficultyName(DungeonDifficulty difficulty)
	{
		return difficulty switch
		{
			DungeonDifficulty.Hard => Strings.Dungeons.DifficultyHard, 
			DungeonDifficulty.Heroic => Strings.Dungeons.DifficultyHeroic, 
			_ => Strings.Dungeons.DifficultyNormal, 
		};
	}

	public static Color DifficultyColor(DungeonDifficulty difficulty)
	{
		return difficulty switch
		{
			DungeonDifficulty.Hard => TechniqueArt.Gold, 
			DungeonDifficulty.Heroic => Violet, 
			_ => TechniqueArt.Green, 
		};
	}

	public static string Multiplier(int percent)
	{
		return TechniqueArt.Number((double)percent / 100.0);
	}

	public static DungeonDifficulty HighestUnlocked(Guid dungeonId)
	{
		byte clearedMask = StatusOf(dungeonId)?.ClearedMask ?? 0;
		DungeonDifficulty result = DungeonDifficulty.Normal;
		DungeonDifficulty dungeonDifficulty = DungeonDifficulty.Normal;
		while ((int)dungeonDifficulty <= 2)
		{
			if (DungeonRules.IsUnlocked(clearedMask, dungeonDifficulty))
			{
				result = dungeonDifficulty;
			}
			dungeonDifficulty++;
		}
		return result;
	}

	private static string AbilityName(BossAbilityKind kind)
	{
		return kind switch
		{
			BossAbilityKind.Charge => Strings.Dungeons.AbilityCharge, 
			BossAbilityKind.RockFall => Strings.Dungeons.AbilityRocks, 
			BossAbilityKind.EyeBeam => Strings.Dungeons.AbilityEyeBeam, 
			BossAbilityKind.Fissures => Strings.Dungeons.AbilityFissures, 
			BossAbilityKind.GoldRain => Strings.Dungeons.AbilityGoldRain, 
			_ => Strings.Dungeons.AbilityShockwave, 
		};
	}

	private static string JoinAbilities(IEnumerable<byte> abilities)
	{
		List<string> list = abilities.Select((byte code) => AbilityName((BossAbilityKind)(code & 0x7F))).Distinct().ToList();
		if (list.Count == 0)
		{
			return string.Empty;
		}
		string result;
		if (list.Count != 1)
		{
			result = string.Concat(string.Join(", ", list.Take(list.Count - 1)), Strings.Dungeons.AbilityAnd, list[list.Count - 1]);
		}
		else
		{
			result = list[0];
		}
		return result;
	}

	private static string Capitalize(string text)
	{
		if (!string.IsNullOrEmpty(text))
		{
			return char.ToUpperInvariant(text[0]) + text.Substring(1, text.Length - 1);
		}
		return text;
	}

	public static string RoomModifier(DungeonDescriptor dungeon, int roomIndex, DungeonDifficulty difficulty)
	{
		byte[] array = StatusOf(dungeon.Id)?.RoomAbilities.ElementAtOrDefault(roomIndex);
		if (array == null || array.Length <= 0)
		{
			return string.Empty;
		}
		DungeonDifficultySettings dungeonDifficultySettings = dungeon.Settings(difficulty);
		string text = JoinAbilities(array.Where((byte code) => (code & 0x80) == 0));
		string text2 = JoinAbilities(array.Where((byte code) => (code & 0x80) != 0));
		string text3 = ((string.IsNullOrEmpty(text2) || dungeonDifficultySettings.EnragedAbilities == DungeonEnragedAbilities.Never) ? text : ((dungeonDifficultySettings.EnragedAbilities == DungeonEnragedAbilities.FromStart) ? Strings.Dungeons.ModFromStart.ToString(text2) : Strings.Dungeons.ModEnragedLater.ToString(text2)));
		string text4 = ((dungeonDifficultySettings.TelegraphPercent < 100) ? Strings.Dungeons.ModShorter.ToString() : ((dungeonDifficultySettings.TelegraphPercent > 100) ? Strings.Dungeons.ModLonger.ToString() : string.Empty));
		if (!string.IsNullOrEmpty(text4))
		{
			text3 = (string.IsNullOrEmpty(text3) ? text4 : (text3 + " · " + text4));
		}
		return Capitalize(text3);
	}

	public static string Clock(long seconds)
	{
		seconds = Math.Max(0L, seconds);
		if (seconds >= 3600)
		{
			return $"{seconds / 3600}:{seconds / 60 % 60:00}:{seconds % 60:00}";
		}
		return $"{seconds / 60:00}:{seconds % 60:00}";
	}

	public static string Duration(long seconds)
	{
		seconds = Math.Max(0L, seconds);
		if (seconds < 3600)
		{
			return Strings.Dungeons.DurationMinutes.ToString(Math.Max(1L, (seconds + 59) / 60));
		}
		return Strings.Dungeons.Duration.ToString(seconds / 3600, $"{seconds / 60 % 60:00}");
	}
}
