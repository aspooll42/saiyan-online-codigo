using System;
using Intersect.Framework.Core;

namespace Intersect.Client.General;

public static class Time
{
	private static long sColorUpdate;

	private static ColorF sCurrentColor = ColorF.White;

	private static float sRate = 1f;

	private static DateTime sServerTime = DateTime.Now;

	private static Color sTargetColor = Color.Transparent;

	private static long sUpdateTime;

	public static DateTime TempsDuJeu
	{
		get
		{
			long num = Math.Clamp(1000 - (sUpdateTime - Timing.Global.Milliseconds), 0L, 1000L);
			return sServerTime.AddMilliseconds((double)(num - 1000) * (double)sRate);
		}
	}

	public static float HeureDuJour => (float)TempsDuJeu.TimeOfDay.TotalHours;

	public static float Vitesse => Math.Max(0.01f, sRate);

	public static void LoadTime(DateTime timeUpdate, Color clr, float rate)
	{
		sServerTime = timeUpdate;
		sTargetColor = clr;
		sUpdateTime = 0L;
		sRate = rate;
	}

	public static void Update()
	{
		if (sUpdateTime < Timing.Global.Milliseconds)
		{
			TimeSpan value = new TimeSpan(0, 0, 0, 0, (int)(1000f * sRate));
			sServerTime = sServerTime.Add(value);
			sUpdateTime = Timing.Global.Milliseconds + 1000;
		}
		float num = Timing.Global.MillisecondsUtc - sColorUpdate;
		float amt = 255f * num / 10000f;
		sCurrentColor = new ColorF(LerpVal(sCurrentColor.R, (int)sTargetColor.R, amt), LerpVal(sCurrentColor.G, (int)sTargetColor.G, amt), LerpVal(sCurrentColor.B, (int)sTargetColor.B, amt), LerpVal(sCurrentColor.A, (int)sTargetColor.A, amt));
		sColorUpdate = Timing.Global.MillisecondsUtc;
	}

	private static float LerpVal(float val, float target, float amt)
	{
		if (val < target)
		{
			val = ((!(val + amt > target)) ? (val + amt) : target);
		}
		if (val > target)
		{
			val = ((!(val - amt < target)) ? (val - amt) : target);
		}
		return val;
	}

	public static string GetTime()
	{
		DateTime now = sServerTime;
		if (Globals.GameState != GameStates.InGame)
		{
			now = DateTime.Now;
		}
		return now.ToString("h:mm:ss tt");
	}

	public static ColorF GetTintColor()
	{
		return sCurrentColor;
	}
}
