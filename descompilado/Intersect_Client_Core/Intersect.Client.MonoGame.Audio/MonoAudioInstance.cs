using System;
using Intersect.Client.Framework.Audio;
using Intersect.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Xna.Framework.Audio;

namespace Intersect.Client.MonoGame.Audio;

public abstract class MonoAudioInstance<TSource, TEffectInstance> : GameAudioInstance where TSource : GameAudioSource where TEffectInstance : SoundEffectInstance
{
	private bool _disposed;

	private readonly TEffectInstance? _effectInstance;

	public new TSource Source => (base.Source as TSource) ?? throw new InvalidOperationException();

	protected virtual bool UsePlatformLoop => true;

	public override AudioInstanceState State
	{
		get
		{
			if (_effectInstance == null || _effectInstance.IsDisposed)
			{
				return AudioInstanceState.Disposed;
			}
			return _effectInstance.State switch
			{
				SoundState.Playing => AudioInstanceState.Playing, 
				SoundState.Paused => AudioInstanceState.Paused, 
				SoundState.Stopped => AudioInstanceState.Stopped, 
				_ => AudioInstanceState.Disposed, 
			};
		}
	}

	protected MonoAudioInstance(GameAudioSource source, TEffectInstance? effectInstance)
		: base(source)
	{
		_effectInstance = effectInstance;
	}

	protected override void OnLoopingChanged(bool isLooping)
	{
		if (UsePlatformLoop && _effectInstance != null)
		{
			try
			{
				_effectInstance.IsLooped = isLooping;
			}
			catch (Exception exception)
			{
				ApplicationContext.Context.Value?.Logger.LogWarning(exception, "Exception thrown when setting IsLooped of an instance of {SourceType} {SourceName} to {IsLooped}", Source.GetType().Name, Source.Name, isLooping);
			}
		}
	}

	protected override void OnPitchChanged(float pitch)
	{
		TEffectInstance effectInstance = _effectInstance;
		if (effectInstance == null || effectInstance.IsDisposed)
		{
			return;
		}
		try
		{
			_effectInstance.Pitch = pitch;
		}
		catch (Exception exception)
		{
			ApplicationContext.Context.Value?.Logger.LogWarning(exception, "Exception thrown when setting Pitch of an instance of {SourceType} {SourceName} to {Pitch}", Source.GetType().Name, Source.Name, pitch);
		}
	}

	protected override void OnVolumeChanged(int oldVolume, int newVolume)
	{
		if (_effectInstance == null)
		{
			return;
		}
		int num = Math.Clamp(Source.TypeVolume, 0, 100);
		float volume = (float)(newVolume * num) / 10000f;
		try
		{
			_effectInstance.Volume = volume;
		}
		catch (NullReferenceException exception)
		{
			ApplicationContext.Context.Value?.Logger.LogWarning(exception, "Exception thrown (likely due to source change) while changing the volume of an instance of {SourceType} {SourceName}", Source.GetType().Name, Source.Name);
		}
		catch (Exception exception2)
		{
			ApplicationContext.Context.Value?.Logger.LogWarning(exception2, "Exception thrown (likely due to device not being ready) while changing the volume of an instance of {SourceType} {SourceName}", Source.GetType().Name, Source.Name);
		}
	}

	public override void Play()
	{
		TEffectInstance effectInstance = _effectInstance;
		if (effectInstance == null || effectInstance.IsDisposed)
		{
			return;
		}
		try
		{
			_effectInstance.Play();
		}
		catch (Exception exception)
		{
			ApplicationContext.Context.Value?.Logger.LogWarning(exception, "Exception thrown when calling Play() on instance of {SourceType} {SourceName}", Source.GetType().Name, Source.Name);
		}
	}

	public override void Pause()
	{
		TEffectInstance effectInstance = _effectInstance;
		if (effectInstance == null || effectInstance.IsDisposed)
		{
			return;
		}
		try
		{
			_effectInstance.Pause();
		}
		catch (Exception exception)
		{
			ApplicationContext.Context.Value?.Logger.LogWarning(exception, "Exception thrown when calling Play() on instance of {SourceType} {SourceName}", Source.GetType().Name, Source.Name);
		}
	}

	public override void Stop()
	{
		TEffectInstance effectInstance = _effectInstance;
		if (effectInstance == null || effectInstance.IsDisposed)
		{
			return;
		}
		try
		{
			_effectInstance.Stop();
		}
		catch (Exception exception)
		{
			ApplicationContext.Context.Value?.Logger.LogWarning(exception, "Exception thrown when calling Play() on instance of {SourceType} {SourceName}", Source.GetType().Name, Source.Name);
		}
	}

	public override void Dispose()
	{
		if (_disposed || _effectInstance == null)
		{
			return;
		}
		_disposed = true;
		if (_effectInstance.State != SoundState.Stopped)
		{
			_effectInstance.Stop();
		}
		try
		{
			Source.ReleaseInstance(this);
		}
		catch (Exception exception)
		{
			ApplicationContext.Context.Value?.Logger.LogWarning(exception, "Exception thrown while disposing instance of {SourceType} {SourceName}", Source.GetType().Name, Source.Name);
		}
	}

	~MonoAudioInstance()
	{
		Dispose();
	}
}
