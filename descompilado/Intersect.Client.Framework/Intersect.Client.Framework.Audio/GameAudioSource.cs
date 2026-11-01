using System;
using Intersect.Client.Framework.Content;
using Intersect.Core;
using Microsoft.Extensions.Logging;

namespace Intersect.Client.Framework.Audio;

public abstract class GameAudioSource : IAsset
{
	private static ulong _nextId;

	private readonly ulong _id = ++_nextId;

	public virtual bool IsDisposed => false;

	public abstract bool IsLoaded { get; }

	public string Id => Name ?? _id.ToString();

	public string Name { get; set; }

	public abstract int TypeVolume { get; }

	public event Action<IAsset>? Disposed;

	public event Action<IAsset>? Loaded;

	public event Action<IAsset>? Unloaded;

	public abstract GameAudioInstance CreateInstance();

	protected void EmitDisposed()
	{
		try
		{
			this.Disposed?.Invoke(this);
		}
		catch (Exception exception)
		{
			ApplicationContext.Context.Value?.Logger.LogWarning(exception, "Exception thrown in event handlers registered for {EventName}()", "Disposed");
		}
	}

	protected void EmitLoaded()
	{
		try
		{
			this.Loaded?.Invoke(this);
		}
		catch (Exception exception)
		{
			ApplicationContext.Context.Value?.Logger.LogWarning(exception, "Exception thrown in event handlers registered for {EventName}()", "Loaded");
		}
	}

	protected void EmitUnloaded()
	{
		try
		{
			this.Unloaded?.Invoke(this);
		}
		catch (Exception exception)
		{
			ApplicationContext.Context.Value?.Logger.LogWarning(exception, "Exception thrown in event handlers registered for {EventName}()", "Unloaded");
		}
	}

	public abstract void ReleaseInstance(GameAudioInstance? audioInstance);
}
