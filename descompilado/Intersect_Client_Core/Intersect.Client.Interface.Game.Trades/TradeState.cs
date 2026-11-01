using Intersect.Framework.Core;

namespace Intersect.Client.Interface.Game.Trades;

public static class TradeState
{
	public static long MyZeni { get; private set; }

	public static long TheirZeni { get; private set; }

	public static bool MyReady { get; private set; }

	public static bool TheirReady { get; private set; }

	public static int Revision { get; private set; }

	public static long RevisionChangedAt { get; private set; }

	public static void Reset()
	{
		MyZeni = 0L;
		TheirZeni = 0L;
		MyReady = false;
		TheirReady = false;
		Revision = 0;
		RevisionChangedAt = 0L;
	}

	public static void Apply(long myZeni, long theirZeni, bool myReady, bool theirReady, int revision)
	{
		if (revision != Revision)
		{
			RevisionChangedAt = Timing.Global.Milliseconds;
		}
		MyZeni = myZeni;
		TheirZeni = theirZeni;
		MyReady = myReady;
		TheirReady = theirReady;
		Revision = revision;
	}
}
