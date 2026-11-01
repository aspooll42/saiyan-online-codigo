using Intersect.Client.Framework.Audio;
using Intersect.Client.Framework.Core.Sounds;
using Intersect.Client.Framework.File_Management;
using Intersect.Client.General;
using Intersect.Framework.Core;

namespace Intersect.Client.Core.Sounds;

public class Sound : ISound
{
	protected string? mFilename;

	protected bool mLoop;

	protected int mLoopInterval;

	protected GameAudioInstance? mSound;

	protected float mVolume;

	private long mStoppedTime = -1L;

	public bool Loaded { get; set; }

	public string? Filename => mFilename;

	public float Pitch
	{
		get
		{
			return mSound?.Pitch ?? 0f;
		}
		set
		{
			if (mSound != null)
			{
				mSound.Pitch = value;
			}
		}
	}

	public bool Loop
	{
		get
		{
			return mLoop;
		}
		set
		{
			mLoop = value;
			if (mSound != null)
			{
				mSound.IsLooping = mLoop;
			}
		}
	}

	public Sound(string filename, bool loop, int loopInterval)
	{
		if (!string.IsNullOrWhiteSpace(filename))
		{
			mFilename = GameContentManager.RemoveExtension(filename).ToLower();
			mLoop = loop;
			mLoopInterval = loopInterval;
			GameAudioSource sound = Globals.ContentManager.GetSound(mFilename);
			if (sound != null)
			{
				mSound = sound.CreateInstance();
				mSound.IsLooping = mLoop && mLoopInterval <= 0;
				mSound.Volume = 100;
				mSound.Play();
				Loaded = true;
			}
			else
			{
				SonsAbsents.Signaler(mFilename);
			}
		}
	}

	public virtual bool Update()
	{
		if (!Loaded)
		{
			return false;
		}
		mSound?.Update();
		if (mLoop && mLoopInterval > 0)
		{
			GameAudioInstance? gameAudioInstance = mSound;
			if (gameAudioInstance != null && gameAudioInstance.State == GameAudioInstance.AudioInstanceState.Stopped)
			{
				if (mStoppedTime == -1)
				{
					mStoppedTime = Timing.Global.MillisecondsUtc;
				}
				else if (mStoppedTime + mLoopInterval < Timing.Global.MillisecondsUtc)
				{
					mSound.Play();
					mStoppedTime = -1L;
				}
				return true;
			}
		}
		if (!mLoop)
		{
			GameAudioInstance? gameAudioInstance2 = mSound;
			if (gameAudioInstance2 != null && gameAudioInstance2.State == GameAudioInstance.AudioInstanceState.Stopped)
			{
				Stop();
				return false;
			}
		}
		return true;
	}

	public virtual void Stop()
	{
		if (Loaded)
		{
			mSound?.Dispose();
			Loaded = false;
		}
	}
}
