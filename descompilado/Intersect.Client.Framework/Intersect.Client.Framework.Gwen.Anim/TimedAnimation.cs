using System;
using Intersect.Client.Framework.Gwen.Platform;

namespace Intersect.Client.Framework.Gwen.Anim;

public class TimedAnimation : Animation
{
	private float mEase;

	private float mEnd;

	private bool mFinished;

	private float mStart;

	private bool mStarted;

	public override bool Finished => mFinished;

	public TimedAnimation(float length, float delay = 0f, float ease = 1f)
	{
		mStart = Neutral.GetTimeInSeconds() + delay;
		mEnd = mStart + length;
		mEase = ease;
		mStarted = false;
		mFinished = false;
	}

	protected override void Think()
	{
		if (mFinished)
		{
			return;
		}
		float timeInSeconds = Neutral.GetTimeInSeconds();
		float num = timeInSeconds - mStart;
		if (!((double)num < 0.0))
		{
			if (!mStarted)
			{
				mStarted = true;
				OnStart();
			}
			float num2 = num / (mEnd - mStart);
			if (num2 < 0f)
			{
				num2 = 0f;
			}
			if (num2 > 1f)
			{
				num2 = 1f;
			}
			Run((float)Math.Pow(num2, mEase));
			if (num2 == 1f)
			{
				mFinished = true;
				OnFinish();
			}
		}
	}

	protected virtual void OnStart()
	{
	}

	protected virtual void Run(float delta)
	{
	}

	protected virtual void OnFinish()
	{
	}
}
