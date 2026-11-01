using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using Intersect.Client.Framework.Content;
using Intersect.Client.Framework.File_Management;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Core;
using Intersect.Framework.Collections;
using Intersect.Framework.Core;
using Intersect.Framework.SystemInformation;
using Microsoft.Extensions.Logging;

namespace Intersect.Client.Framework.Graphics;

public abstract class GameRenderer : IGameRenderer, ITextHelper
{
	private const long InactiveTimeCutoff = 30000L;

	private const long MinimumAvailableVRAM = 134217728L;

	private const long StaleTimeCutoff = 1800000L;

	private const long StaleVRAMCutoff = 1073741824L;

	private const long UnusedTimeCutoff = 21600000L;

	private const long UnusedVRAMCutoff = 8589934592L;

	private readonly ConcurrentDictionary<string, ScreenshotRequest> _screenshotRequestLookup = new ConcurrentDictionary<string, ScreenshotRequest>();

	private readonly ConcurrentConditionalDequeue<ScreenshotRequest> _screenshotRequests = new ConcurrentConditionalDequeue<ScreenshotRequest>();

	private readonly HashSet<IGameTexture> _textures = new HashSet<IGameTexture>();

	private readonly List<IGameTexture> _texturesSortedByAccessTime = new List<IGameTexture>();

	private readonly HashSet<IIndexBuffer> _indexBuffers = new HashSet<IIndexBuffer>();

	private readonly HashSet<IVertexBuffer> _vertexBuffers = new HashSet<IVertexBuffer>();

	private float _scale = 1f;

	private IGameTexture? _whitePixel;

	private GameShader? _activeShader;

	private IIndexBuffer? _activeIndexBuffer;

	private string? _cursorName;

	private IGameTexture? _cursor;

	private bool _rangementAFaire;

	private long _prochainRangementDeLaMemoire;

	public int DessinsDeLImage { get; protected set; }

	public int LotsDeLImage { get; protected set; }

	public long HorodatageDeLImage { get; protected set; } = Timing.Global.MillisecondsUtc;

	public int DessinsDeLImagePrecedente { get; protected set; }

	public int LotsDeLImagePrecedente { get; protected set; }

	public long EnvoisDeLImagePrecedente { get; protected set; }

	public long TexturesDeLImagePrecedente { get; protected set; }

	public IGameTexture[] Textures
	{
		get
		{
			try
			{
				IGameTexture[] array = new IGameTexture[_textures.Count];
				_textures.CopyTo(array);
				return array;
			}
			catch
			{
				return _textures.ToArray();
			}
		}
	}

	public ulong TextureCount => (ulong)Math.Max(0, _textures.Count);

	public ulong RenderTargetAllocations { get; private set; }

	public ulong TextureAllocations { get; private set; }

	public ulong IndexBufferAllocations { get; private set; }

	public ulong VertexBufferAllocations { get; private set; }

	public abstract GameShader BasicShader { get; }

	public GameShader? ActiveShader
	{
		get
		{
			return _activeShader;
		}
		set
		{
			if (value != _activeShader)
			{
				_activeShader = value;
				OnSetActiveShader(value);
			}
		}
	}

	public IIndexBuffer? ActiveIndexBuffer
	{
		get
		{
			return _activeIndexBuffer;
		}
		set
		{
			if (value != _activeIndexBuffer)
			{
				_activeIndexBuffer = value;
				OnSetActiveIndexBuffer(value);
			}
		}
	}

	public bool HasOverrideResolution => OverrideResolution != Resolution.Empty;

	public bool HasScreenshotRequests => _screenshotRequests.Count > 0;

	public float Scale
	{
		get
		{
			return _scale;
		}
		set
		{
			if (!((double)Math.Abs(_scale - value) < 0.001))
			{
				_scale = value;
				RecreateSpriteBatch();
			}
		}
	}

	public abstract long UsedMemory { get; }

	public abstract int FPS { get; }

	public IGameTexture WhitePixel => _whitePixel ?? (_whitePixel = CreateWhitePixel());

	public Resolution ActiveResolution => new Resolution(PreferredResolution, OverrideResolution);

	public Resolution OverrideResolution { get; set; }

	public Resolution PreferredResolution { get; set; }

	public string? CursorName
	{
		get
		{
			return _cursorName;
		}
		set
		{
			if (!(value == _cursorName))
			{
				_cursorName = value;
				_cursor = (string.IsNullOrWhiteSpace(_cursorName) ? null : GameContentManager.Current.GetTexture(TextureType.Misc, _cursorName));
				OnCursorChanged(_cursor);
			}
		}
	}

	public IGameTexture? Cursor
	{
		get
		{
			return _cursor;
		}
		set
		{
			if (value != _cursor)
			{
				_cursor = value;
				_cursorName = _cursor?.Name;
				OnCursorChanged(_cursor);
			}
		}
	}

	public FloatRect CurrentView
	{
		get
		{
			return GetView();
		}
		set
		{
			SetView(value);
		}
	}

	public string ResolutionAsString => GetResolutionString();

	public virtual bool LinearFiltering { get; set; }

	public abstract List<string> ValidVideoModes { get; }

	public abstract int ScreenWidth { get; }

	public abstract int ScreenHeight { get; }

	public event EventHandler<TextureEventArgs>? TextureAllocated;

	public event EventHandler<TextureEventArgs>? TextureCreated;

	public event EventHandler<TextureEventArgs>? TextureDisposed;

	public event EventHandler<TextureEventArgs>? TextureFreed;

	public event EventHandler<BufferEventArgs>? IndexBufferAllocated;

	public event EventHandler<BufferEventArgs>? IndexBufferFreed;

	public event EventHandler<BufferEventArgs>? VertexBufferAllocated;

	public event EventHandler<BufferEventArgs>? VertexBufferFreed;

	protected abstract void OnSetActiveShader(GameShader? shader);

	protected abstract void OnSetActiveIndexBuffer(IIndexBuffer? indexBuffer);

	protected abstract void OnCursorChanged(IGameTexture? newCursor);

	public abstract void Clear(Color color);

	void IGameRenderer.DrawTexture(IGameTexture tex, float sx, float sy, float sw, float sh, float tx, float ty, float tw, float th, Color renderColor, IGameRenderTexture? renderTarget, GameBlendModes blendMode, GameShader? shader, float rotationDegrees)
	{
		DrawTexture(tex, sx, sy, sw, sh, tx, ty, tw, th, renderColor, renderTarget, blendMode, shader, rotationDegrees);
	}

	public abstract void SetActiveVertexBuffer(IVertexBuffer vertexBuffer, int bufferIndex = 0);

	public abstract IGameRenderTexture CreateRenderTexture(int width, int height);

	public abstract IIndexBuffer CreateIndexBuffer<TIndex>(int count, BufferUsage usage = BufferUsage.None, bool dynamic = false) where TIndex : struct;

	public abstract IVertexBuffer CreateVertexBuffer<TVertex>(int count, BufferUsage usage = BufferUsage.None, bool dynamic = false) where TVertex : struct;

	public abstract Vector2 MeasureText(string? text, IFont? font, int size, float fontScale);

	public virtual (float Top, float Bottom) MeasureTextInk(string? text, IFont? font, int size, float fontScale)
	{
		return (Top: 0f, Bottom: MeasureText(text, font, size, fontScale).Y);
	}

	public abstract void DrawString(string text, IFont? gameFont, int size, float x, float y, float fontScale, Color? fontColor, bool worldPos = true, IGameRenderTexture? renderTexture = null, Color? borderColor = null);

	public abstract void DrawString(string text, IFont? gameFont, int size, float x, float y, float fontScale, Color fontColor, bool worldPos, IGameRenderTexture renderTexture, FloatRect clipRect, Color? borderColor = null);

	public virtual void DrawStringScaled(string text, IFont? font, int size, float x, float y, float scale, Color color, float extraSpacing = 0f)
	{
	}

	public virtual Vector2 MeasureTextScaled(string? text, IFont? font, int size, float scale, float extraSpacing = 0f)
	{
		return MeasureText(text, font, size, scale);
	}

	public virtual IGameTexture? CreateTextureFromPixels(string name, int width, int height, byte[] rgba, Func<Stream>? sourceFactory = null)
	{
		return null;
	}

	public virtual bool TryReadPixels(IGameTexture texture, out byte[] rgba, out int width, out int height)
	{
		rgba = Array.Empty<byte>();
		width = 0;
		height = 0;
		return false;
	}

	public void RequestScreenshot(string? pathToScreenshots = null)
	{
		if (string.IsNullOrWhiteSpace(pathToScreenshots))
		{
			string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures, Environment.SpecialFolderOption.Create);
			string path = Path.Combine(folderPath, ApplicationContext.CurrentContext.Name);
			pathToScreenshots = Path.Combine(path, "screenshots");
		}
		if (!Directory.Exists(pathToScreenshots))
		{
			Directory.CreateDirectory(pathToScreenshots);
		}
		int num = 1;
		string text = $"{DateTime.Now:yyyyMMdd-HHmmssfff}.png";
		string pathToScreenshotFile = Path.Combine(pathToScreenshots, text);
		while (ScreenshotOrRequestExistsFor(pathToScreenshotFile) && num < 100)
		{
			text = $"{DateTime.Now:yyyyMMdd-HHmmssfff}.{num++:000}.png";
			pathToScreenshotFile = Path.Combine(pathToScreenshots ?? string.Empty, text);
		}
		if (ScreenshotOrRequestExistsFor(pathToScreenshotFile))
		{
			ApplicationContext.CurrentContext.Logger.LogWarning("Failed to request screenshot");
			return;
		}
		ScreenshotRequest screenshotRequest = new ScreenshotRequest(() => File.OpenWrite(pathToScreenshotFile), text);
		if (_screenshotRequestLookup.TryAdd(text, screenshotRequest))
		{
			_screenshotRequests.Enqueue(screenshotRequest);
		}
	}

	protected internal void MarkConstructed(IGameTexture texture, bool markAllocated = false)
	{
		_textures.Add(texture);
		this.TextureCreated?.Invoke(this, new TextureEventArgs(texture));
		if (markAllocated)
		{
			MarkAllocated(texture);
		}
	}

	protected internal void MarkDisposed(IGameTexture texture)
	{
		_textures.Remove(texture);
		this.TextureDisposed?.Invoke(this, new TextureEventArgs(texture));
	}

	protected internal void MarkAllocated(IGameTexture texture)
	{
		if (!_textures.Contains(texture))
		{
			throw new InvalidOperationException("Texture must be added first");
		}
		if (!texture.IsPinned)
		{
			_texturesSortedByAccessTime.AddSorted(texture);
			_rangementAFaire = true;
		}
		ulong renderTargetAllocations;
		if (texture is IGameRenderTexture)
		{
			renderTargetAllocations = RenderTargetAllocations + 1;
			RenderTargetAllocations = renderTargetAllocations;
		}
		renderTargetAllocations = TextureAllocations + 1;
		TextureAllocations = renderTargetAllocations;
		this.TextureAllocated?.Invoke(this, new TextureEventArgs(texture));
	}

	protected internal void MarkFreed(IGameTexture texture)
	{
		if (!texture.IsPinned)
		{
			_texturesSortedByAccessTime.Remove(texture);
		}
		if (texture is IGameRenderTexture)
		{
			if (RenderTargetAllocations != 0)
			{
				ulong renderTargetAllocations = RenderTargetAllocations - 1;
				RenderTargetAllocations = renderTargetAllocations;
			}
			else
			{
				ApplicationContext.CurrentContext.Logger.LogCritical("RenderTextureAllocations out of sync");
			}
		}
		if (TextureAllocations != 0)
		{
			ulong renderTargetAllocations = TextureAllocations - 1;
			TextureAllocations = renderTargetAllocations;
		}
		else
		{
			ApplicationContext.CurrentContext.Logger.LogCritical("TextureAllocations out of sync");
		}
		this.TextureFreed?.Invoke(this, new TextureEventArgs(texture));
	}

	internal void UpdateAccessTime(IGameTexture gameTexture)
	{
		_rangementAFaire = true;
	}

	private void RangerParDateDAcces()
	{
		if (_rangementAFaire)
		{
			_rangementAFaire = false;
			_texturesSortedByAccessTime.Sort();
		}
	}

	protected internal void MarkAllocated(IIndexBuffer buffer)
	{
		if (!_indexBuffers.Add(buffer))
		{
			throw new InvalidOperationException($"Index buffer {buffer.Id} was already allocated");
		}
		ulong indexBufferAllocations = IndexBufferAllocations + 1;
		IndexBufferAllocations = indexBufferAllocations;
		this.IndexBufferAllocated?.Invoke(this, new BufferEventArgs(buffer));
	}

	protected internal void MarkFreed(IIndexBuffer buffer)
	{
		if (!_indexBuffers.Remove(buffer))
		{
			throw new InvalidOperationException($"Index buffer {buffer.Id} was already freed");
		}
		if (IndexBufferAllocations != 0)
		{
			ulong indexBufferAllocations = IndexBufferAllocations - 1;
			IndexBufferAllocations = indexBufferAllocations;
		}
		else
		{
			ApplicationContext.CurrentContext.Logger.LogCritical("IndexBufferAllocations out of sync");
		}
		this.IndexBufferFreed?.Invoke(this, new BufferEventArgs(buffer));
	}

	protected internal void MarkAllocated(IVertexBuffer buffer)
	{
		if (!_vertexBuffers.Add(buffer))
		{
			throw new InvalidOperationException($"Vertex buffer {buffer.Id} was already allocated");
		}
		ulong vertexBufferAllocations = VertexBufferAllocations + 1;
		VertexBufferAllocations = vertexBufferAllocations;
		this.VertexBufferAllocated?.Invoke(this, new BufferEventArgs(buffer));
	}

	protected internal void MarkFreed(IVertexBuffer buffer)
	{
		if (!_vertexBuffers.Remove(buffer))
		{
			throw new InvalidOperationException($"Vertex buffer {buffer.Id} was already freed");
		}
		if (VertexBufferAllocations != 0)
		{
			ulong vertexBufferAllocations = VertexBufferAllocations - 1;
			VertexBufferAllocations = vertexBufferAllocations;
		}
		else
		{
			ApplicationContext.CurrentContext.Logger.LogCritical("VertexBufferAllocations out of sync");
		}
		this.VertexBufferFreed?.Invoke(this, new BufferEventArgs(buffer));
	}

	public abstract void Init();

	public abstract bool Begin();

	public abstract bool BeginScreenshot();

	protected abstract bool RecreateSpriteBatch();

	public void End()
	{
		DoEnd();
		if (Timing.Global.MillisecondsUtc < _prochainRangementDeLaMemoire)
		{
			return;
		}
		_prochainRangementDeLaMemoire = Timing.Global.MillisecondsUtc + 1000;
		long usedMemory = UsedMemory;
		long num = PlatformStatistics.AvailableGPUMemory;
		long num2 = PlatformStatistics.TotalGPUMemory;
		if (num < 0)
		{
			num = ((num2 >= 0) ? (num2 - usedMemory) : PlatformStatistics.AvailableSystemMemory);
		}
		if (num2 < 0)
		{
			num2 = num + usedMemory;
		}
		long millisecondsUtc = Timing.Global.MillisecondsUtc;
		long num3 = millisecondsUtc - 21600000;
		long num4 = millisecondsUtc - 1800000;
		long num5 = millisecondsUtc - 30000;
		long num6 = ((8589934592L < num2) ? 8589934592L : (num2 - 134217728));
		long num7 = ((1073741824 < num2) ? 1073741824 : (num2 - 134217728));
		RangerParDateDAcces();
		while (true)
		{
			IGameTexture gameTexture = _texturesSortedByAccessTime.FirstOrDefault();
			if (gameTexture == null)
			{
				break;
			}
			long accessTime = gameTexture.AccessTime;
			if (num > 134217728)
			{
				if (num > num7)
				{
					if (num > num6 || gameTexture.AccessTime > num3)
					{
						break;
					}
					ApplicationContext.CurrentContext.Logger.LogTrace("Unloading {TextureName} because it's unused and we have {AvailableVRAM} bytes of VRAM available (cutoff is {CutoffVRAM})", gameTexture.Name, num, num6);
				}
				else if (gameTexture.AccessTime <= num4)
				{
					ApplicationContext.CurrentContext.Logger.LogTrace("Unloading {TextureName} because it's stale and we have {AvailableVRAM} bytes of VRAM available (cutoff is {CutoffVRAM})", gameTexture.Name, num, num7);
				}
			}
			else
			{
				if (accessTime > num5)
				{
					break;
				}
				ApplicationContext.CurrentContext.Logger.LogTrace("Unloading {TextureName} because it's inactive and we have {AvailableVRAM} bytes of VRAM available (cutoff is {CutoffVRAM})", gameTexture.Name, num, 134217728L);
			}
			if (gameTexture.Unload())
			{
				_texturesSortedByAccessTime.Remove(gameTexture);
			}
		}
	}

	protected abstract void DoEnd();

	public abstract void EndScreenshot();

	public abstract void SetView(FloatRect view);

	public abstract FloatRect GetView();

	public abstract IFont LoadFont(string fontName, IDictionary<int, FileInfo> fontSourcesBySize);

	public abstract void DrawTexture(IGameTexture tex, float sx, float sy, float sw, float sh, float tx, float ty, float tw, float th, Color renderColor, IGameRenderTexture? renderTarget = null, GameBlendModes blendMode = GameBlendModes.None, GameShader? shader = null, float rotationDegrees = 0f, bool isUi = false, bool drawImmediate = false);

	public abstract string GetResolutionString();

	public abstract bool DisplayModeChanged();

	public IGameTexture LoadTexture(string assetName, string filePath)
	{
		if (!AtlasReference.TryGet(assetName, out AtlasReference atlasReference))
		{
			return CreateTextureFromStreamFactory(assetName, delegate
			{
				try
				{
					return Coffre.Ouvrir(filePath);
				}
				catch
				{
					throw;
				}
			});
		}
		return CreateGameTextureFromAtlasReference(assetName, atlasReference);
	}

	protected abstract IGameTexture CreateGameTextureFromAtlasReference(string assetName, AtlasReference atlasReference);

	public abstract IGameTexture CreateTextureFromStreamFactory(string assetName, Func<Stream> streamFactory);

	protected abstract IGameTexture CreateWhitePixel();

	public abstract GameTileBuffer CreateTileBuffer();

	public abstract void DrawBuffer(IVertexBuffer vertexBuffer, IIndexBuffer? indexBuffer = null);

	protected virtual void PreDrawTileBuffer(GameTileBuffer buffer)
	{
	}

	public void DrawTileBuffer(GameTileBuffer buffer)
	{
		ArgumentNullException.ThrowIfNull(buffer, "buffer");
		PreDrawTileBuffer(buffer);
		ActiveShader = BasicShader;
		ActiveShader.Texture = buffer.Texture;
		DrawBuffer(buffer.VertexBuffer, buffer.IndexBuffer);
	}

	public abstract void Close();

	public abstract GameShader LoadShader(string shaderName);

	private bool ScreenshotOrRequestExistsFor(string pathToScreenshotFile)
	{
		if (!File.Exists(pathToScreenshotFile))
		{
			return _screenshotRequestLookup.ContainsKey(pathToScreenshotFile);
		}
		return true;
	}

	protected void ProcessScreenshots(WriteTextureToStreamDelegate writeTexture)
	{
		Func<ScreenshotRequest, bool> consumer = (ScreenshotRequest screenshotRequest) => writeTexture(screenshotRequest.StreamFactory(), screenshotRequest.Hint);
		while (_screenshotRequests.TryDequeueIf(consumer))
		{
		}
	}
}
