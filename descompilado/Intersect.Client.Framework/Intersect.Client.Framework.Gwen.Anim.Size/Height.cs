namespace Intersect.Client.Framework.Gwen.Anim.Size;

internal class Height : TimedAnimation
{
	private int mDelta;

	private bool mHide;

	private int mStartSize;

	public Height(int startSize, int endSize, float length, bool hide = false, float delay = 0f, float ease = 1f)
		: base(length, delay, ease)
	{
		mStartSize = startSize;
		mDelta = endSize - mStartSize;
		mHide = hide;
	}

	protected override void OnStart()
	{
		base.OnStart();
		mControl.Height = mStartSize;
	}

	protected override void Run(float delta)
	{
		base.Run(delta);
		mControl.Height = (int)((float)mStartSize + (float)mDelta * delta);
	}

	protected override void OnFinish()
	{
		base.OnFinish();
		mControl.Height = mStartSize + mDelta;
		mControl.IsHidden = mHide;
	}
}
