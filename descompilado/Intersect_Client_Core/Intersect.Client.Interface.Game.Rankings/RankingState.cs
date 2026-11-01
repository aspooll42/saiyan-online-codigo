using System;
using System.Collections.Generic;
using System.Linq;
using Intersect.Client.Networking;
using Intersect.Framework.Core;
using Intersect.Framework.Core.GameObjects.Rankings;
using Intersect.Network.Packets.Server;

namespace Intersect.Client.Interface.Game.Rankings;

internal static class RankingState
{
	private const long ProfileLifetimeMs = 300000L;

	private static readonly Dictionary<Guid, (RankingProfilePacket Packet, long ReceivedAt)> Profiles = new Dictionary<Guid, (RankingProfilePacket, long)>();

	private static readonly HashSet<Guid> Pending = new HashSet<Guid>();

	private static string _lastKey = string.Empty;

	private static long _lastRequestAt;

	public static RankingPacket? Current { get; private set; }

	public static long ReceivedAt { get; private set; }

	public static int Version { get; private set; }

	public static int PowerPlace { get; private set; }

	public static void Request(RankingQuery query, bool force = false)
	{
		long millisecondsUtc = Timing.Global.MillisecondsUtc;
		if (force || !(query.Key == _lastKey) || millisecondsUtc - _lastRequestAt >= 1000)
		{
			_lastKey = query.Key;
			_lastRequestAt = millisecondsUtc;
			PacketSender.SendRankingRequest(query);
		}
	}

	public static void RequestProfile(Guid playerId)
	{
		long millisecondsUtc = Timing.Global.MillisecondsUtc;
		if (!(playerId == Guid.Empty) && !Pending.Contains(playerId) && (!Profiles.TryGetValue(playerId, out (RankingProfilePacket, long) value) || millisecondsUtc - value.Item2 >= 300000))
		{
			Pending.Add(playerId);
			PacketSender.SendRankingProfileRequest(playerId);
		}
	}

	public static RankingProfilePacket? ProfileOf(Guid playerId)
	{
		if (!Profiles.TryGetValue(playerId, out (RankingProfilePacket, long) value))
		{
			return null;
		}
		return value.Item1;
	}

	public static void OnRanking(RankingPacket packet)
	{
		Current = packet;
		ReceivedAt = Timing.Global.MillisecondsUtc;
		Version++;
		RankingPlaceInfo rankingPlaceInfo = packet.Places.FirstOrDefault((RankingPlaceInfo place) => place.Query.Board == RankingBoard.Power && place.Query.Period == RankingPeriod.Week);
		if (rankingPlaceInfo != null)
		{
			PowerPlace = rankingPlaceInfo.Rank;
		}
	}

	public static void OnProfile(RankingProfilePacket packet)
	{
		Pending.Remove(packet.PlayerId);
		Profiles[packet.PlayerId] = (packet, Timing.Global.MillisecondsUtc);
		Version++;
	}
}
