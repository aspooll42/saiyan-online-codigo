using Intersect.Client.Networking;
using Intersect.Framework.Core;

namespace Intersect.Client.Core;

public static class Fade
{
	public enum FadeType
	{
		None,
		In,
		Out
	}

	private static FadeType sCurrentAction;

	private static float sFadeAmt;

	private static float sFadeDurationMs;

	private static long sLastUpdate;

	public static float Alpha => sFadeAmt * 255f;

	private static bool InformServer { get; set; }

	public static void FadeIn(float durationMs, bool informServer = false)
	{
		sFadeDurationMs = durationMs;
		sCurrentAction = FadeType.In;
		sFadeAmt = 1f;
		sLastUpdate = Timing.Global.MillisecondsUtc;
		InformServer = informServer;
	}

	public static void FadeOut(float durationMs, bool informServer = false)
	{
		sFadeDurationMs = durationMs;
		sCurrentAction = FadeType.Out;
		sFadeAmt = 0f;
		sLastUpdate = Timing.Global.MillisecondsUtc;
		InformServer = informServer;
	}

	public static void Cancel()
	{
		sCurrentAction = FadeType.None;
		sFadeAmt = 0f;
		InformServerOfCompletion(force: true);
	}

	public static bool DoneFading()
	{
		return sCurrentAction == FadeType.None;
	}

	public static void Update()
	{
		if (sCurrentAction == FadeType.In)
		{
			sFadeAmt -= (float)(Timing.Global.MillisecondsUtc - sLastUpdate) / sFadeDurationMs;
			if (sFadeAmt <= 0f)
			{
				sCurrentAction = FadeType.None;
				sFadeAmt = 0f;
				InformServerOfCompletion();
			}
		}
		else if (sCurrentAction == FadeType.Out)
		{
			sFadeAmt += (float)(Timing.Global.MillisecondsUtc - sLastUpdate) / sFadeDurationMs;
			if (sFadeAmt >= 1f)
			{
				sCurrentAction = FadeType.None;
				sFadeAmt = 1f;
				InformServerOfCompletion();
			}
		}
		sLastUpdate = Timing.Global.MillisecondsUtc;
	}

	private static void InformServerOfCompletion(bool force = false)
	{
		if (InformServer || force)
		{
			InformServer = false;
			PacketSender.SendFadeCompletePacket();
		}
	}
}
