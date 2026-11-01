using System;
using System.Diagnostics;
using System.IO;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using Intersect.Client.Framework.Content;
using Intersect.Client.Framework.File_Management;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Compression;
using Intersect.Core;
using Intersect.Framework.Reflection;
using Microsoft.Extensions.Logging;

namespace Intersect.Client.Framework.Graphics;

public static class GameTexture
{
	public static IGameTexture? GetBoundingTexture(BoundsComparison boundsComparison, params IGameTexture[] textures)
	{
		IGameTexture gameTexture = null;
		foreach (IGameTexture gameTexture2 in textures)
		{
			if (gameTexture == null)
			{
				gameTexture = gameTexture2;
				continue;
			}
			bool flag = false;
			switch (boundsComparison)
			{
			case BoundsComparison.Width:
				flag = gameTexture2.Width > gameTexture.Width;
				break;
			case BoundsComparison.Height:
				flag = gameTexture2.Height > gameTexture.Height;
				break;
			case BoundsComparison.Dimensions:
				flag = gameTexture2.Width >= gameTexture.Width && gameTexture2.Height >= gameTexture.Height;
				break;
			case BoundsComparison.Area:
				flag = gameTexture2.Area > gameTexture.Area;
				break;
			default:
				ApplicationContext.Context.Value?.Logger.LogError(new ArgumentOutOfRangeException("boundsComparison", boundsComparison.ToString()), "Failed to get bounding texture");
				break;
			}
			if (flag)
			{
				gameTexture = gameTexture2;
			}
		}
		return gameTexture;
	}
}
public abstract class GameTexture<TPlatformTexture, TPlatformRenderer> : IGameTexture, IAsset, IComparable<IGameTexture>, IDisposable where TPlatformTexture : class, IDisposable where TPlatformRenderer : GameRenderer
{
	private static ulong _nextId;

	protected readonly ulong Id = ++_nextId;

	protected readonly TPlatformRenderer Renderer;

	private bool _disposed;

	private Func<Stream>? _streamFactory;

	private TPlatformTexture? _platformTexture;

	protected TPlatformTexture? PlatformTexture
	{
		get
		{
			if ((object)AtlasReference != null)
			{
				return AtlasReference.Texture.GetTexture<TPlatformTexture>();
			}
			if (_platformTexture == null)
			{
				LoadPlatformTexture();
			}
			else
			{
				UpdateAccessTime();
			}
			return _platformTexture;
		}
		set
		{
			if (_platformTexture != null && value != null)
			{
				throw new InvalidOperationException();
			}
			_platformTexture = value;
		}
	}

	public Color this[int x, int y] => GetPixel(x, y);

	public Color this[Point point] => GetPixel(point.X, point.Y);

	public long AccessTime { get; private set; }

	public bool IsDisposed => _disposed;

	public bool IsLoaded
	{
		get
		{
			if (!_disposed)
			{
				return _platformTexture != null;
			}
			return false;
		}
	}

	public bool IsMissingOrCorrupt { get; private set; }

	public bool IsPinned { get; }

	string IAsset.Id => Name;

	public string Name { get; }

	public int Area => Width * Height;

	public FloatRect Bounds => new FloatRect(0f, 0f, Width, Height);

	public Vector2 Dimensions => new Vector2(Width, Height);

	public Vector2 Center => Dimensions / 2f;

	public AtlasReference? AtlasReference
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get;
	}

	public abstract int Width { get; }

	public abstract int Height { get; }

	public event Action<IAsset>? Disposed;

	public event Action<IAsset>? Loaded;

	public event Action<IAsset>? Unloaded;

	private GameTexture(TPlatformRenderer renderer, string? name, bool pinned)
	{
		ArgumentNullException.ThrowIfNull(renderer, "renderer");
		Renderer = renderer;
		Name = name ?? $"{GetType().GetName(qualified: true)}#{Id}";
		IsPinned = pinned;
		if (pinned)
		{
			AccessTime = long.MaxValue;
		}
		Renderer.MarkConstructed(this);
	}

	protected GameTexture(TPlatformRenderer renderer, string? name, TPlatformTexture platformTexture, Func<Stream>? streamFactory = null)
		: this(renderer, name, pinned: true)
	{
		ArgumentNullException.ThrowIfNull(platformTexture, "platformTexture");
		_platformTexture = platformTexture;
		_streamFactory = streamFactory;
		Renderer.MarkAllocated(this);
	}

	protected GameTexture(TPlatformRenderer renderer, string name, Func<Stream> streamFactory, bool pinned = false)
		: this(renderer, name, pinned)
	{
		ArgumentNullException.ThrowIfNull(streamFactory, "streamFactory");
		_streamFactory = streamFactory;
	}

	protected GameTexture(TPlatformRenderer renderer, string name, AtlasReference atlasReference)
		: this(renderer, name, pinned: true)
	{
		ArgumentNullException.ThrowIfNull(atlasReference, "atlasReference");
		AtlasReference = atlasReference;
	}

	public Stream? OuvrirSource()
	{
		if (!(AtlasReference != null))
		{
			Func<Stream> streamFactory = _streamFactory;
			if (streamFactory != null)
			{
				Stream stream = streamFactory();
				if (!Name.EndsWith(".asset"))
				{
					return stream;
				}
				using (stream)
				{
					using CryptoStream cryptoStream = GzipCompression.CreateDecompressedFileStream(stream);
					MemoryStream memoryStream = new MemoryStream();
					cryptoStream.CopyTo(memoryStream);
					memoryStream.Position = 0L;
					return memoryStream;
				}
			}
		}
		return null;
	}

	public int CompareTo(IGameTexture? other)
	{
		if (this == other)
		{
			return 0;
		}
		if (!(other is GameTexture<TPlatformTexture, TPlatformRenderer> gameTexture))
		{
			return -1;
		}
		int num = (int)Math.Clamp(AccessTime - other.AccessTime, -2147483648L, 2147483647L);
		if (num != 0)
		{
			return num;
		}
		if (Id != gameTexture.Id)
		{
			if (Id >= gameTexture.Id)
			{
				return 1;
			}
			return -1;
		}
		ApplicationContext.Context.Value?.Logger.LogError("Texture '{NameA}' and '{NameB}' both have the same Id '{Id}' which is unexpected", Name, other.Name, Id);
		return 0;
	}

	protected abstract TPlatformTexture? CreatePlatformTextureFromStream(Stream stream);

	private void LoadPlatformTexture(bool force = false)
	{
		if (!force && (_platformTexture != null || IsMissingOrCorrupt))
		{
			return;
		}
		if (AtlasReference != null)
		{
			if (!(AtlasReference.Texture is GameTexture<TPlatformTexture, TPlatformRenderer> gameTexture))
			{
				IsMissingOrCorrupt = true;
				throw new InvalidOperationException();
			}
			gameTexture.LoadPlatformTexture(force);
			IsMissingOrCorrupt = gameTexture.IsMissingOrCorrupt;
			if (!IsMissingOrCorrupt)
			{
				UpdateAccessTime();
				EmitLoaded();
			}
			return;
		}
		if (_streamFactory == null)
		{
			IsMissingOrCorrupt = true;
			throw new InvalidOperationException();
		}
		bool flag = false;
		try
		{
			_platformTexture = null;
			long timestamp = Stopwatch.GetTimestamp();
			using Stream stream = _streamFactory();
			if (Name.EndsWith(".asset"))
			{
				using CryptoStream stream2 = GzipCompression.CreateDecompressedFileStream(stream);
				_platformTexture = CreatePlatformTextureFromStream(stream2);
			}
			else
			{
				_platformTexture = CreatePlatformTextureFromStream(stream);
			}
			Renderer.MarkAllocated(this);
			UpdateAccessTime();
			EmitLoaded();
			double totalMilliseconds = Stopwatch.GetElapsedTime(timestamp).TotalMilliseconds;
			if (totalMilliseconds >= 2.0)
			{
				Chrono.Noter($"texture {Name} {totalMilliseconds:0.0} ms");
			}
		}
		catch (CoffreFermeException)
		{
			flag = true;
		}
		catch (Exception exception)
		{
			ApplicationContext.CurrentContext.Logger.LogError(exception, "Exception thrown while trying to load '{TextureName}'", Name);
		}
		finally
		{
			IsMissingOrCorrupt = _platformTexture == null && !flag;
		}
	}

	public bool Unload()
	{
		if (AccessTime == long.MaxValue)
		{
			ApplicationContext.CurrentContext.Logger.LogWarning("Tried to unload pinned texture {TextureId} ({TextureName})", Id, Name);
			return false;
		}
		_platformTexture?.Dispose();
		_platformTexture = null;
		AccessTime = 0L;
		EmitUnloaded();
		OnUnload();
		return true;
	}

	public object? GetTexture()
	{
		return PlatformTexture;
	}

	public void Reload()
	{
		InternalReload();
	}

	private protected virtual void InternalReload()
	{
		LoadPlatformTexture(force: true);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public TRequestedPlatformTexture? GetTexture<TRequestedPlatformTexture>() where TRequestedPlatformTexture : class
	{
		if (typeof(TRequestedPlatformTexture) != typeof(TPlatformTexture))
		{
			return null;
		}
		return GetTexture() as TRequestedPlatformTexture;
	}

	public abstract Color GetPixel(int x, int y);

	public virtual bool TryGetPixels(out Color[] pixels)
	{
		pixels = Array.Empty<Color>();
		return false;
	}

	public override string ToString()
	{
		return GetType().Name + " (" + Name + ")";
	}

	public void Dispose()
	{
		Dispose(disposing: true);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	protected void UpdateAccessTime()
	{
		if (AccessTime != long.MaxValue)
		{
			long horodatageDeLImage = Renderer.HorodatageDeLImage;
			if (AccessTime != horodatageDeLImage)
			{
				AccessTime = horodatageDeLImage;
				Renderer.UpdateAccessTime(this);
			}
		}
	}

	protected virtual void OnUnload()
	{
	}

	private void EmitDisposed()
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

	private void EmitUnloaded()
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

	~GameTexture()
	{
		Dispose(disposing: false);
	}

	private void Dispose(bool disposing)
	{
		if (disposing || !_disposed)
		{
			ObjectDisposedException.ThrowIf(_disposed, this);
			_disposed = true;
			try
			{
				_platformTexture?.Dispose();
				_platformTexture = null;
				Renderer.MarkDisposed(this);
			}
			catch
			{
				throw;
			}
			EmitDisposed();
			OnDispose(disposing: true);
		}
	}

	protected virtual void OnDispose(bool disposing)
	{
	}

	protected bool PoserLaTexture(TPlatformTexture image)
	{
		if (_platformTexture != null || AtlasReference != null)
		{
			return false;
		}
		_platformTexture = image;
		IsMissingOrCorrupt = false;
		Renderer.MarkAllocated(this);
		UpdateAccessTime();
		EmitLoaded();
		return true;
	}
}
