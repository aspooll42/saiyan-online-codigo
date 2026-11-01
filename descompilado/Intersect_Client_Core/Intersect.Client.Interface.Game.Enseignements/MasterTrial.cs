using System;
using Intersect.Client.General;
using Intersect.Client.Interface.Game.Chat;
using Intersect.Enums;
using Intersect.Framework.Core;
using Intersect.Network.Packets.Server;

namespace Intersect.Client.Interface.Game.Enseignements;

internal static class MasterTrial
{
	private const int ResultMs = 6000;

	private static long _receivedAt;

	private static long _resultAt;

	public static TeachingTrialPacket? Current { get; private set; }

	public static TeachingTrialResultPacket? Result { get; private set; }

	public static string? NextStep { get; private set; }

	private static long ResultDurationMs => (NextStep == null) ? 6000 : 10000;

	public static bool InTrial => Current != null;

	public static long RemainingMs
	{
		get
		{
			if (Current != null)
			{
				return Math.Max(0L, Current.RemainingMs - (Timing.Global.MillisecondsUtc - _receivedAt));
			}
			return 0L;
		}
	}

	public static void OnState(TeachingTrialPacket packet)
	{
		Current = (string.IsNullOrEmpty(packet.TrialId) ? null : packet);
		_receivedAt = Timing.Global.MillisecondsUtc;
	}

	public static void OnResult(TeachingTrialResultPacket packet)
	{
		Result = packet;
		_resultAt = Timing.Global.MillisecondsUtc;
		Current = null;
		NextStep = (packet.Success ? TeachingTrialSteps.AfterSuccess(Globals.Me, packet.TrialId) : null);
		if (NextStep != null)
		{
			ChatboxMsg.AddMessage(new ChatboxMsg(NextStep, CustomColors.Alerts.Info, ChatMessageType.Notice));
		}
	}

	public static bool ResultVisible(out long elapsedMs)
	{
		elapsedMs = Timing.Global.MillisecondsUtc - _resultAt;
		if (Result != null)
		{
			return elapsedMs < ResultDurationMs;
		}
		return false;
	}

	public static float ResultFade(long elapsedMs)
	{
		return Math.Clamp(Math.Min((float)elapsedMs / 250f, (float)(ResultDurationMs - elapsedMs) / 600f), 0f, 1f);
	}

	public static string Clock(long milliseconds)
	{
		int num = (int)Math.Ceiling((double)milliseconds / 1000.0);
		return $"{num / 60}:{num % 60:00}";
	}
}
