using System;
using Intersect.Client.Framework.Gwen.Control.EventArguments;

namespace Intersect.Client.Framework.Audio;

public abstract class GameAudioInstance
{
	public enum AudioInstanceState
	{
		Stopped,
		Playing,
		Paused,
		Disposed
	}

	private bool _looping;

	private int _volume = 10;

	private int _lastSourceTypeVolume;

	private float _pitch;

	public EventHandler<ValueChangedEventArgs<int>>? VolumeChanged;

	public GameAudioSource Source { get; }

	public bool IsLooping
	{
		get
		{
			return _looping;
		}
		set
		{
			if (value != _looping)
			{
				_looping = value;
				OnLoopingChanged(value);
			}
		}
	}

	public int Volume
	{
		get
		{
			return _volume;
		}
		set
		{
			int num = Math.Clamp(value, 0, 100);
			if (_volume != num)
			{
				int volume = _volume;
				_volume = num;
				OnVolumeChanged(volume, num);
				VolumeChanged?.Invoke(this, new ValueChangedEventArgs<int>
				{
					Value = num,
					OldValue = volume
				});
			}
		}
	}

	public float Pitch
	{
		get
		{
			return _pitch;
		}
		set
		{
			float num = Math.Clamp(value, -1f, 1f);
			if (num != _pitch)
			{
				_pitch = num;
				OnPitchChanged(num);
			}
		}
	}

	public abstract AudioInstanceState State { get; }

	protected GameAudioInstance(GameAudioSource source)
	{
		Source = source;
	}

	protected virtual void OnPitchChanged(float pitch)
	{
	}

	protected abstract void OnVolumeChanged(int oldVolume, int newVolume);

	protected abstract void OnLoopingChanged(bool isLooping);

	public abstract void Play();

	public abstract void Pause();

	public abstract void Stop();

	public abstract void Dispose();

	public void Update()
	{
		int typeVolume = Source.TypeVolume;
		if (typeVolume != _lastSourceTypeVolume)
		{
			OnVolumeChanged(Volume, Volume);
			_lastSourceTypeVolume = typeVolume;
		}
	}
}
