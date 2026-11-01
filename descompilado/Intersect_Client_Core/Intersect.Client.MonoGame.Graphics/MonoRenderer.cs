using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Intersect.Client.Classes.MonoGame.Graphics;
using Intersect.Client.Core;
using Intersect.Client.Framework.Database;
using Intersect.Client.Framework.File_Management;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.General;
using Intersect.Client.Interface;
using Intersect.Client.Localization;
using Intersect.Client.Maps;
using Intersect.Client.MonoGame.NativeInterop;
using Intersect.Client.ThirdParty;
using Intersect.Core;
using Intersect.Framework;
using Intersect.Framework.Core;
using Intersect.Framework.Reflection;
using Intersect.IO.Files;
using Intersect.Utilities;
using Microsoft.Extensions.Logging;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Intersect.Client.MonoGame.Graphics;

internal class MonoRenderer : GameRenderer
{
	private abstract class GPUBuffer : IGPUBuffer, IDisposable
	{
		private static readonly ConcurrentDictionary<Type, uint> NextIdLookup = new ConcurrentDictionary<Type, uint>();

		public uint Id { get; }

		public abstract int Count { get; }

		public abstract int SizeBytes { get; }

		protected GPUBuffer()
		{
			Id = NextIdLookup.AddOrUpdate(GetType(), (Type _) => 1u, (Type _, uint nextId) => nextId + 1);
		}

		protected virtual void ReleaseUnmanagedResources()
		{
		}

		protected virtual void ReleaseManagedResources()
		{
		}

		private void Dispose(bool disposing)
		{
			ReleaseUnmanagedResources();
			if (disposing)
			{
				ReleaseManagedResources();
			}
		}

		public void Dispose()
		{
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}

		~GPUBuffer()
		{
			Dispose(disposing: false);
		}
	}

	private sealed class IndexBuffer(Microsoft.Xna.Framework.Graphics.IndexBuffer platformBuffer, Type indexType) : GPUBuffer(), IIndexBuffer, IGPUBuffer, IDisposable
	{
		internal readonly Microsoft.Xna.Framework.Graphics.IndexBuffer PlatformBuffer = platformBuffer;

		public Type IndexType { get; } = indexType;

		public override int Count => platformBuffer.IndexCount;

		public override int SizeBytes => (int)MeasureDataSize(platformBuffer);

		protected override void ReleaseManagedResources()
		{
			base.ReleaseManagedResources();
			if (!platformBuffer.IsDisposed)
			{
				platformBuffer.Dispose();
			}
		}

		public bool GetData<TIndex>(TIndex[] destination) where TIndex : struct
		{
			return GetData(0, destination, 0, destination.Length);
		}

		public bool GetData<TIndex>(TIndex[] destination, int destinationOffset, int length) where TIndex : struct
		{
			return GetData(0, destination, destinationOffset, length);
		}

		public bool GetData<TIndex>(int bufferOffset, TIndex[] destination, int destinationOffset, int length) where TIndex : struct
		{
			try
			{
				platformBuffer.GetData(bufferOffset, destination, destinationOffset, length);
				return true;
			}
			catch (Exception exception)
			{
				ApplicationContext.CurrentContext.Logger.LogError(exception, "Failed to get data for {BufferType} {Id}", "IIndexBuffer", base.Id);
				return false;
			}
		}

		public bool SetData<TIndex>(TIndex[] data) where TIndex : struct
		{
			return SetData(0, data, 0, data.Length, BufferWriteMode.Overwrite);
		}

		public bool SetData<TIndex>(TIndex[] data, int sourceOffset, int length) where TIndex : struct
		{
			return SetData(0, data, sourceOffset, length, BufferWriteMode.Overwrite);
		}

		public bool SetData<TIndex>(int destinationOffset, TIndex[] data, int sourceOffset, int length) where TIndex : struct
		{
			return SetData(destinationOffset, data, sourceOffset, length, BufferWriteMode.Overwrite);
		}

		public bool SetData<TIndex>(int destinationOffset, TIndex[] data, int sourceOffset, int length, BufferWriteMode bufferWriteMode) where TIndex : struct
		{
			if (bufferWriteMode != BufferWriteMode.Overwrite)
			{
				if (!(platformBuffer is DynamicIndexBuffer dynamicIndexBuffer))
				{
					ApplicationContext.CurrentContext.Logger.LogError("{BufferWriteMode} is only supported on dynamic buffers", bufferWriteMode);
					return false;
				}
				dynamicIndexBuffer.SetData(destinationOffset, data, sourceOffset, length, bufferWriteMode switch
				{
					BufferWriteMode.Overwrite => SetDataOptions.None, 
					BufferWriteMode.Discard => SetDataOptions.Discard, 
					BufferWriteMode.NoOverwrite => SetDataOptions.None, 
					_ => throw Exceptions.UnreachableInvalidEnum(bufferWriteMode), 
				});
			}
			else
			{
				platformBuffer.SetData(destinationOffset, data, sourceOffset, length);
			}
			return true;
		}
	}

	private sealed class LectureDAvance(IGameTexture texture, string fichier)
	{
		public byte[]? Rgba;

		public int Largeur;

		public int Hauteur;

		public Texture2D? Image;

		public int LignesPosees;

		public IGameTexture Texture { get; } = texture;

		public string Fichier { get; } = fichier;
	}

	private class MonoRenderTexture : GameRenderTexture<RenderTarget2D, MonoRenderer>
	{
		public override int Width { get; }

		public override int Height { get; }

		internal MonoRenderTexture(MonoRenderer renderer, RenderTarget2D renderTarget2D)
			: base(renderer, renderTarget2D)
		{
			Width = renderTarget2D.Width;
			Height = renderTarget2D.Height;
		}

		protected override RenderTarget2D CreatePlatformTextureFromStream(Stream stream)
		{
			throw new NotSupportedException("Render targets cannot be created from streams");
		}

		public override Color GetPixel(int x, int y)
		{
			Microsoft.Xna.Framework.Color[] array = new Microsoft.Xna.Framework.Color[1];
			base.PlatformTexture?.GetData(0, new Microsoft.Xna.Framework.Rectangle(x, y, 1, 1), array, 0, 1);
			return new Color(array[0].A, array[0].R, array[0].G, array[0].B);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override bool Begin()
		{
			return true;
		}

		public override void End()
		{
			(Intersect.Client.Core.Graphics.Renderer as MonoRenderer)?.EndSpriteBatch();
		}

		public override void Clear(Color color)
		{
			Renderer.EndSpriteBatch();
			Renderer._graphicsDevice.SetRenderTarget(base.PlatformTexture);
			Renderer._graphicsDevice.Clear(ConvertColor(color));
			Renderer._graphicsDevice.SetRenderTarget(null);
		}
	}

	private class Shader : GameShader
	{
		private readonly MonoRenderer _renderer;

		private readonly Dictionary<string, Color> Colors = new Dictionary<string, Color>();

		private readonly Dictionary<string, float> Floats = new Dictionary<string, float>();

		private bool mValuesChanged;

		public Effect PlatformShader { get; }

		public override IGameTexture? Texture
		{
			get
			{
				Effect platformShader = PlatformShader;
				if (platformShader is BasicEffect basicEffect)
				{
					return _renderer._allocatedTextures.GetValueOrDefault(basicEffect.Texture);
				}
				throw new NotSupportedException(PlatformShader.ToString());
			}
			set
			{
				Effect platformShader = PlatformShader;
				if (platformShader is BasicEffect basicEffect)
				{
					basicEffect.Texture = value?.GetTexture<Texture2D>();
					return;
				}
				throw new NotSupportedException(PlatformShader.ToString());
			}
		}

		internal Shader(MonoRenderer renderer, string shaderName, ContentManager contentManager)
			: base(shaderName)
		{
			_renderer = renderer;
			using Stream stream = typeof(Shader).Assembly.GetManifestResourceStream(shaderName);
			string path = FileSystemHelper.WriteToTemporaryFolder(shaderName, stream);
			PlatformShader = contentManager.Load<Effect>(Path.ChangeExtension(path, null));
		}

		internal Shader(MonoRenderer renderer, Effect effect)
			: base(effect.ToString())
		{
			_renderer = renderer;
			PlatformShader = effect;
		}

		public override void SetFloat(string key, float val)
		{
			if (!Floats.ContainsKey(key))
			{
				Floats.Add(key, val);
				mValuesChanged = true;
			}
			else if (Floats[key] != val)
			{
				Floats[key] = val;
				mValuesChanged = true;
			}
		}

		public override void SetInt(string key, int val)
		{
		}

		public override void SetColor(string key, Color val)
		{
			if (!Colors.ContainsKey(key))
			{
				Colors.Add(key, val);
				mValuesChanged = true;
			}
			else if (Colors[key].A != val.A || Colors[key].R != val.R || Colors[key].G != val.G || Colors[key].B != val.B)
			{
				Colors[key] = val;
				mValuesChanged = true;
			}
		}

		public override void SetVector2(string key, System.Numerics.Vector2 val)
		{
		}

		public override bool ValuesChanged()
		{
			return mValuesChanged;
		}

		public override void ResetChanged()
		{
			mValuesChanged = false;
			foreach (KeyValuePair<string, Color> color in Colors)
			{
				Microsoft.Xna.Framework.Vector4 value = new Microsoft.Xna.Framework.Vector4((float)(int)color.Value.R / 255f, (float)(int)color.Value.G / 255f, (float)(int)color.Value.B / 255f, (float)(int)color.Value.A / 255f);
				PlatformShader.Parameters[color.Key].SetValue(value);
			}
			foreach (KeyValuePair<string, float> @float in Floats)
			{
				PlatformShader.Parameters[@float.Key].SetValue(@float.Value);
			}
		}

		public override object GetShader()
		{
			return PlatformShader;
		}
	}

	private class MonoTexture : GameTexture<Texture2D, MonoRenderer>
	{
		public override int Width
		{
			get
			{
				AtlasReference? atlasReference = base.AtlasReference;
				if ((object)atlasReference == null || !atlasReference.IsRotated)
				{
					return base.AtlasReference?.Bounds.Width ?? base.PlatformTexture?.Width ?? 0;
				}
				return base.AtlasReference.Bounds.Height;
			}
		}

		public override int Height
		{
			get
			{
				AtlasReference? atlasReference = base.AtlasReference;
				if ((object)atlasReference == null || !atlasReference.IsRotated)
				{
					return base.AtlasReference?.Bounds.Height ?? base.PlatformTexture?.Height ?? 0;
				}
				return base.AtlasReference.Bounds.Width;
			}
		}

		internal MonoTexture(MonoRenderer renderer, string? name, Texture2D platformTexture, Func<Stream>? streamFactory = null)
			: base(renderer, name, platformTexture, streamFactory)
		{
		}

		internal MonoTexture(MonoRenderer renderer, string name, Func<Stream> streamFactory, bool pinned = false)
			: base(renderer, name, streamFactory, pinned)
		{
		}

		internal MonoTexture(MonoRenderer renderer, string name, AtlasReference atlasReference)
			: base(renderer, name, atlasReference)
		{
		}

		internal bool Poser(Texture2D image)
		{
			if (!PoserLaTexture(image))
			{
				return false;
			}
			image.Disposing += delegate
			{
				base.PlatformTexture = null;
			};
			return true;
		}

		protected override Texture2D? CreatePlatformTextureFromStream(Stream stream)
		{
			Texture2D texture2D = Renderer.CreatePlatformTextureFromStream(this, stream);
			if (texture2D != null && !texture2D.IsDisposed)
			{
				texture2D.Disposing += delegate
				{
					base.PlatformTexture = null;
				};
			}
			return texture2D;
		}

		public override Color GetPixel(int x, int y)
		{
			Texture2D platformTexture = base.PlatformTexture;
			if (platformTexture == null)
			{
				return Color.White;
			}
			if ((object)base.AtlasReference != null)
			{
				if (base.AtlasReference.IsRotated)
				{
					int num = x;
					x = base.AtlasReference.Bounds.Right - y - base.AtlasReference.Bounds.Height;
					y = base.AtlasReference.Bounds.Top + num;
				}
				else
				{
					x += base.AtlasReference.Bounds.X;
					y += base.AtlasReference.Bounds.Y;
				}
			}
			Microsoft.Xna.Framework.Color[] array = new Microsoft.Xna.Framework.Color[1];
			Microsoft.Xna.Framework.Rectangle value = new Microsoft.Xna.Framework.Rectangle(x, y, 1, 1);
			platformTexture.GetData(0, value, array, 0, 1);
			Microsoft.Xna.Framework.Color color = array[0];
			return new Color(color.A, color.R, color.G, color.B);
		}

		public override bool TryGetPixels(out Color[] pixels)
		{
			pixels = Array.Empty<Color>();
			Texture2D platformTexture = base.PlatformTexture;
			if (platformTexture == null)
			{
				return false;
			}
			AtlasReference atlasReference = base.AtlasReference;
			if ((object)atlasReference != null && atlasReference.IsRotated)
			{
				return false;
			}
			AtlasReference atlasReference2 = base.AtlasReference;
			Microsoft.Xna.Framework.Rectangle value = (((object)atlasReference2 != null) ? new Microsoft.Xna.Framework.Rectangle(atlasReference2.Bounds.X, atlasReference2.Bounds.Y, atlasReference2.Bounds.Width, atlasReference2.Bounds.Height) : new Microsoft.Xna.Framework.Rectangle(0, 0, Width, Height));
			Microsoft.Xna.Framework.Color[] array = new Microsoft.Xna.Framework.Color[value.Width * value.Height];
			platformTexture.GetData(0, value, array, 0, array.Length);
			pixels = new Color[array.Length];
			for (int i = 0; i < array.Length; i++)
			{
				Microsoft.Xna.Framework.Color color = array[i];
				pixels[i] = new Color(color.A, color.R, color.G, color.B);
			}
			return true;
		}
	}

	private class VertexBuffer(Microsoft.Xna.Framework.Graphics.VertexBuffer platformBuffer, Type vertexType) : GPUBuffer(), IVertexBuffer, IGPUBuffer, IDisposable
	{
		private Intersect.Client.Framework.Graphics.PrimitiveType _primitiveType;

		internal readonly Microsoft.Xna.Framework.Graphics.VertexBuffer PlatformBuffer = platformBuffer;

		internal Microsoft.Xna.Framework.Graphics.PrimitiveType PlatformPrimitiveType { get; private set; }

		public Type VertexType { get; } = vertexType;

		public override int Count => platformBuffer.VertexCount;

		public override int SizeBytes => (int)MeasureDataSize(platformBuffer);

		public Intersect.Client.Framework.Graphics.PrimitiveType PrimitiveType
		{
			get
			{
				return _primitiveType;
			}
			set
			{
				if (value != _primitiveType)
				{
					_primitiveType = value;
					PlatformPrimitiveType = value switch
					{
						Intersect.Client.Framework.Graphics.PrimitiveType.LineList => Microsoft.Xna.Framework.Graphics.PrimitiveType.LineList, 
						Intersect.Client.Framework.Graphics.PrimitiveType.TriangleList => Microsoft.Xna.Framework.Graphics.PrimitiveType.TriangleList, 
						Intersect.Client.Framework.Graphics.PrimitiveType.TriangleStrip => Microsoft.Xna.Framework.Graphics.PrimitiveType.TriangleStrip, 
						_ => throw Exceptions.UnreachableInvalidEnum(value), 
					};
				}
			}
		}

		protected override void ReleaseManagedResources()
		{
			base.ReleaseManagedResources();
			if (!platformBuffer.IsDisposed)
			{
				platformBuffer.Dispose();
			}
		}

		public bool GetData<TVertex>(TVertex[] destination) where TVertex : struct
		{
			return GetData(0, destination, 0, destination.Length);
		}

		public bool GetData<TVertex>(TVertex[] destination, int destinationOffset, int length) where TVertex : struct
		{
			return GetData(0, destination, destinationOffset, length);
		}

		public bool GetData<TVertex>(int bufferOffset, TVertex[] destination, int destinationOffset, int length) where TVertex : struct
		{
			try
			{
				platformBuffer.GetData(bufferOffset, destination, destinationOffset, length);
				return true;
			}
			catch (Exception exception)
			{
				ApplicationContext.CurrentContext.Logger.LogError(exception, "Failed to get data for {BufferType} {Id}", "IVertexBuffer", base.Id);
				return false;
			}
		}

		public bool SetData<TVertex>(TVertex[] data) where TVertex : struct
		{
			return SetData(0, data, 0, data.Length, BufferWriteMode.Overwrite);
		}

		public bool SetData<TVertex>(TVertex[] data, int sourceOffset, int length) where TVertex : struct
		{
			return SetData(0, data, sourceOffset, length, BufferWriteMode.Overwrite);
		}

		public bool SetData<TVertex>(int destinationOffset, TVertex[] data, int sourceOffset, int length) where TVertex : struct
		{
			return SetData(destinationOffset, data, sourceOffset, length, BufferWriteMode.Overwrite);
		}

		public bool SetData<TVertex>(int destinationOffset, TVertex[] data, int sourceOffset, int length, BufferWriteMode bufferWriteMode) where TVertex : struct
		{
			if (bufferWriteMode != BufferWriteMode.Overwrite)
			{
				if (!(platformBuffer is DynamicVertexBuffer dynamicVertexBuffer))
				{
					ApplicationContext.CurrentContext.Logger.LogError("{BufferWriteMode} is only supported on dynamic buffers", bufferWriteMode);
					return false;
				}
				SetDataOptions options = bufferWriteMode switch
				{
					BufferWriteMode.Overwrite => SetDataOptions.None, 
					BufferWriteMode.Discard => SetDataOptions.Discard, 
					BufferWriteMode.NoOverwrite => SetDataOptions.None, 
					_ => throw Exceptions.UnreachableInvalidEnum(bufferWriteMode), 
				};
				dynamicVertexBuffer.SetData(destinationOffset, data, sourceOffset, length, Marshal.SizeOf<TVertex>(), options);
			}
			else
			{
				platformBuffer.SetData(destinationOffset, data, sourceOffset, length, Marshal.SizeOf<TVertex>());
			}
			return true;
		}
	}

	private RenderTarget2D? _cibleCimes;

	private bool _cimesOuvertes;

	private readonly ContentManager _contentManager;

	private readonly Game _game;

	private readonly GameWindow _gameWindow;

	private readonly GraphicsDeviceManager _graphicsDeviceManager;

	private readonly BlendState _cutoutState;

	private readonly BlendState _multiplyState;

	private readonly BlendState _normalState;

	private readonly RasterizerState _rasterizerState = new RasterizerState
	{
		ScissorTestEnable = true
	};

	private GraphicsDevice? _graphicsDevice;

	private List<string>? _validVideoModes;

	private BasicEffect? _basicEffect;

	private GameBlendModes _currentBlendmode = GameBlendModes.None;

	private GameShader? _currentShader;

	private FloatRect _currentSpriteView;

	private IGameRenderTexture? _currentRenderTarget;

	private FloatRect _currentView;

	private int _displayHeight;

	private int _displayWidth;

	private bool _displayModeChanged;

	private int _fps;

	private int _frames;

	private long _nextFpsTime;

	private long _fullscreenChangedTimer = -1L;

	private bool _initialized;

	private bool _initializing;

	private DisplayMode? _previousDisplayMode;

	private int _screenHeight;

	private int _screenWidth;

	private RenderTarget2D? _screenshotRenderTarget;

	private SpriteBatch? _spriteBatch;

	private bool _spriteBatchBegan;

	private bool _currentLinearFiltering;

	private readonly Dictionary<SpriteFont, char> _defaultCharacterForSpriteFont = new Dictionary<SpriteFont, char>();

	private GameShader _basicShader;

	private readonly Dictionary<SpriteFont, ulong[]> _lettresDesPolices = new Dictionary<SpriteFont, ulong[]>();

	private readonly Dictionary<SpriteFont, Dictionary<char, SpriteFont.Glyph>> _glyphesDesPolices = new Dictionary<SpriteFont, Dictionary<char, SpriteFont.Glyph>>();

	private readonly ConcurrentDictionary<Microsoft.Xna.Framework.Graphics.IndexBuffer, IIndexBuffer> _allocatedIndexBuffers = new ConcurrentDictionary<Microsoft.Xna.Framework.Graphics.IndexBuffer, IIndexBuffer>();

	private long _allocatedIndexBuffersSize;

	private RenderTarget2D? _cibleInterface;

	private bool _interfaceReduite;

	private int _largeurInterface;

	private int _hauteurInterface;

	private static readonly BlendState NormalDansLImage = new BlendState
	{
		ColorSourceBlend = Blend.SourceAlpha,
		ColorDestinationBlend = Blend.InverseSourceAlpha,
		AlphaSourceBlend = Blend.One,
		AlphaDestinationBlend = Blend.InverseSourceAlpha,
		ColorBlendFunction = BlendFunction.Add,
		AlphaBlendFunction = BlendFunction.Add
	};

	private static readonly BlendState AdditifDansLImage = new BlendState
	{
		ColorSourceBlend = Blend.SourceAlpha,
		ColorDestinationBlend = Blend.One,
		AlphaSourceBlend = Blend.Zero,
		AlphaDestinationBlend = Blend.One,
		ColorBlendFunction = BlendFunction.Add,
		AlphaBlendFunction = BlendFunction.Add
	};

	private RenderTarget2D? _cibleDuMondeAgrandi;

	private bool _mondeAgrandi;

	private int _largeurDuMondeAgrandi;

	private int _hauteurDuMondeAgrandi;

	private const int OctetsPosesParImage = 1572864;

	private readonly object _verrouDesLectures = new object();

	private readonly Queue<LectureDAvance> _aLire = new Queue<LectureDAvance>();

	private readonly Queue<LectureDAvance> _aPoser = new Queue<LectureDAvance>();

	private LectureDAvance? _enCoursDePose;

	private int _generationDesLectures;

	private bool _lecteurActif;

	private static readonly Resolution ResolutionMinimale = new Resolution(1280L, 720L);

	private const string ListeDepuis1280 = "ResolutionsDepuis1280";

	private readonly ConcurrentDictionary<Texture2D, IGameTexture> _allocatedTextures = new ConcurrentDictionary<Texture2D, IGameTexture>();

	private long _allocatedTexturesSize;

	private readonly ConcurrentDictionary<Microsoft.Xna.Framework.Graphics.VertexBuffer, IVertexBuffer> _allocatedVertexBuffers = new ConcurrentDictionary<Microsoft.Xna.Framework.Graphics.VertexBuffer, IVertexBuffer>();

	private long _allocatedVertexBuffersSize;

	private static readonly ConcurrentDictionary<Type, VertexDeclaration> CachedVertexDeclarations = new ConcurrentDictionary<Type, VertexDeclaration>();

	private RenderTarget2D? _cibleMonde;

	private bool _mondeLisse;

	private int _largeurMonde;

	private int _hauteurMonde;

	private RenderTarget2D? CibleParDefaut
	{
		get
		{
			if (!_interfaceReduite)
			{
				if (!_cimesOuvertes)
				{
					if (!_mondeLisse)
					{
						return CibleDeLEcran;
					}
					return _cibleMonde;
				}
				return _cibleCimes;
			}
			return _cibleInterface;
		}
	}

	public override GameShader BasicShader => _basicShader;

	public override long UsedMemory => _allocatedTexturesSize + _allocatedIndexBuffersSize + _allocatedVertexBuffersSize;

	public override bool LinearFiltering { get; set; }

	public override int FPS => _fps;

	public override int ScreenHeight
	{
		get
		{
			if (!_interfaceReduite)
			{
				if (!_mondeLisse)
				{
					if (!_mondeAgrandi)
					{
						return _screenHeight;
					}
					return _hauteurDuMondeAgrandi;
				}
				return _hauteurMonde;
			}
			return _hauteurInterface;
		}
	}

	public override int ScreenWidth
	{
		get
		{
			if (!_interfaceReduite)
			{
				if (!_mondeLisse)
				{
					if (!_mondeAgrandi)
					{
						return _screenWidth;
					}
					return _largeurDuMondeAgrandi;
				}
				return _largeurMonde;
			}
			return _largeurInterface;
		}
	}

	public override List<string> ValidVideoModes
	{
		get
		{
			if (_validVideoModes != null)
			{
				return _validVideoModes;
			}
			_validVideoModes = ModesQuiTiennentALEcran(GetAllowedResolutions());
			if (_validVideoModes.Count == 0)
			{
				DisplayMode displayMode = _graphicsDevice.DisplayMode;
				if ((object)displayMode != null)
				{
					_validVideoModes.Add(new Resolution(displayMode.Width, displayMode.Height).ToString());
				}
			}
			return _validVideoModes;
		}
	}

	internal int LargeurReelle => _screenWidth;

	internal int HauteurReelle => _screenHeight;

	private RenderTarget2D? CibleDeLEcran
	{
		get
		{
			if (!_mondeAgrandi)
			{
				return _screenshotRenderTarget;
			}
			return _cibleDuMondeAgrandi;
		}
	}

	internal static string Synchro { get; private set; } = "ordinaire";

	private GraphicsDevice GraphicsDevice => _graphicsDevice ?? throw new InvalidOperationException("Graphics device not initialized");

	internal bool CommencerCimes()
	{
		if (!_cimesOuvertes)
		{
			GraphicsDevice graphicsDevice = _graphicsDevice;
			if (graphicsDevice != null && _spriteBatch != null)
			{
				int screenWidth = ScreenWidth;
				int screenHeight = ScreenHeight;
				if (screenWidth < 1 || screenHeight < 1)
				{
					return false;
				}
				EndSpriteBatch();
				if (_cibleCimes == null || _cibleCimes.IsDisposed || _cibleCimes.Width != screenWidth || _cibleCimes.Height != screenHeight)
				{
					_cibleCimes?.Dispose();
					_cibleCimes = new RenderTarget2D(graphicsDevice, screenWidth, screenHeight, mipMap: false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
				}
				_cimesOuvertes = true;
				graphicsDevice.SetRenderTarget(_cibleCimes);
				graphicsDevice.Clear(Microsoft.Xna.Framework.Color.Transparent);
				return true;
			}
		}
		return false;
	}

	internal void FinirCimes()
	{
		if (!_cimesOuvertes)
		{
			return;
		}
		EndSpriteBatch();
		_cimesOuvertes = false;
		if (_cibleCimes != null)
		{
			GraphicsDevice graphicsDevice = _graphicsDevice;
			if (graphicsDevice != null && _spriteBatch != null)
			{
				graphicsDevice.SetRenderTarget(CibleParDefaut);
				_spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone);
				_spriteBatch.Draw(_cibleCimes, Microsoft.Xna.Framework.Vector2.Zero, Microsoft.Xna.Framework.Color.White);
				_spriteBatch.End();
			}
		}
	}

	protected override void OnSetActiveShader(GameShader? shader)
	{
	}

	protected override void OnSetActiveIndexBuffer(IIndexBuffer? indexBuffer)
	{
		GraphicsDevice graphicsDevice = GraphicsDevice;
		if (indexBuffer == null)
		{
			graphicsDevice.Indices = null;
			return;
		}
		if (indexBuffer is IndexBuffer indexBuffer2)
		{
			Microsoft.Xna.Framework.Graphics.IndexBuffer platformBuffer = indexBuffer2.PlatformBuffer;
			if (platformBuffer != null)
			{
				graphicsDevice.Indices = platformBuffer;
				return;
			}
		}
		throw new ArgumentException("Expected a " + typeof(IndexBuffer).GetName(qualified: true) + " but received a " + indexBuffer.GetType().GetName(qualified: true), "indexBuffer");
	}

	public MonoRenderer(GraphicsDeviceManager graphicsDeviceManager, ContentManager contentManager, Game monoGame)
	{
		_game = monoGame;
		_graphicsDeviceManager = graphicsDeviceManager;
		_contentManager = contentManager;
		_normalState = new BlendState
		{
			ColorSourceBlend = Blend.SourceAlpha,
			AlphaSourceBlend = Blend.One,
			ColorDestinationBlend = Blend.InverseSourceAlpha,
			AlphaDestinationBlend = Blend.One,
			ColorBlendFunction = BlendFunction.Add,
			AlphaBlendFunction = BlendFunction.Add
		};
		_multiplyState = new BlendState
		{
			ColorBlendFunction = BlendFunction.Add,
			ColorSourceBlend = Blend.DestinationColor,
			ColorDestinationBlend = Blend.Zero
		};
		_cutoutState = new BlendState
		{
			ColorBlendFunction = BlendFunction.Add,
			ColorSourceBlend = Blend.Zero,
			ColorDestinationBlend = Blend.InverseSourceAlpha,
			AlphaBlendFunction = BlendFunction.Add,
			AlphaSourceBlend = Blend.Zero,
			AlphaDestinationBlend = Blend.InverseSourceAlpha
		};
		_gameWindow = monoGame.Window;
	}

	public void UpdateGraphicsState(int width, int height, bool initial = false)
	{
		DisplayMode currentDisplayMode = _graphicsDeviceManager.GraphicsDevice.Adapter.CurrentDisplayMode;
		if (Globals.Database.FullScreen)
		{
			bool flag = false;
			foreach (DisplayMode supportedDisplayMode in _graphicsDeviceManager.GraphicsDevice.Adapter.SupportedDisplayModes)
			{
				if (supportedDisplayMode.Width == width && supportedDisplayMode.Height == height)
				{
					flag = true;
				}
			}
			if (!flag)
			{
				Globals.Database.FullScreen = false;
				Globals.Database.SavePreferences();
				Intersect.Client.Interface.Interface.ShowAlert(Strings.Errors.DisplayNotSupportedError.ToString(Strings.Internals.ResolutionXByY.ToString(width, height)), Strings.Errors.DisplayNotSupported);
			}
		}
		bool fullScreen = Globals.Database.FullScreen;
		bool flag2 = initial || (_graphicsDeviceManager.IsFullScreen != fullScreen && !fullScreen);
		if (flag2)
		{
			_graphicsDeviceManager.IsFullScreen = fullScreen;
			_graphicsDeviceManager.HardwareModeSwitch = !fullScreen;
		}
#if !ANDROID
		SDL_Rect[] displayBounds = Sdl2.GetDisplayBounds();
		SDL_Rect sDL_Rect = displayBounds[0];
		int w = sDL_Rect.w;
		int h = sDL_Rect.h;
#else
		DisplayMode modoAndroid = _graphicsDeviceManager.GraphicsDevice.Adapter.CurrentDisplayMode;
		SDL_Rect sDL_Rect = default(SDL_Rect);
		int w = modoAndroid.Width;
		int h = modoAndroid.Height;
#endif
#if !ANDROID
		if (fullScreen)
		{
			width = w;
			height = h;
		}
#else
		width = w;
		height = h;
#endif
		_screenWidth = width;
		_screenHeight = height;
		_graphicsDeviceManager.PreferredBackBufferWidth = width;
		_graphicsDeviceManager.PreferredBackBufferHeight = height;
		_graphicsDeviceManager.SynchronizeWithVerticalRetrace = true;
		_game.IsFixedTimeStep = false;
		_graphicsDeviceManager.ApplyChanges();
		AppliquerLaSynchroAdaptative();
		_displayWidth = _graphicsDeviceManager.GraphicsDevice.Adapter.CurrentDisplayMode.Width;
		_displayHeight = _graphicsDeviceManager.GraphicsDevice.Adapter.CurrentDisplayMode.Height;
#if !ANDROID
		if (flag2)
		{
			_gameWindow.Position = new Microsoft.Xna.Framework.Point(sDL_Rect.x + (w - _screenWidth) / 2, sDL_Rect.y + (h - _screenHeight) / 2);
		}
#endif
		_previousDisplayMode = currentDisplayMode;
		if (flag2)
		{
			_fullscreenChangedTimer = Timing.Global.MillisecondsUtc + 1000;
		}
		if (flag2)
		{
			_displayModeChanged = true;
		}
	}

	public override bool Begin()
	{
		base.DessinsDeLImagePrecedente = base.DessinsDeLImage;
		base.LotsDeLImagePrecedente = base.LotsDeLImage;
		base.DessinsDeLImage = 0;
		base.LotsDeLImage = 0;
		base.HorodatageDeLImage = Timing.Global.MillisecondsUtc;
		if (_fullscreenChangedTimer > -1 && _fullscreenChangedTimer < Timing.Global.MillisecondsUtc)
		{
			_graphicsDeviceManager.PreferredBackBufferWidth--;
			_graphicsDeviceManager.ApplyChanges();
			_graphicsDeviceManager.PreferredBackBufferWidth++;
			_graphicsDeviceManager.ApplyChanges();
			_fullscreenChangedTimer = -1L;
		}
		if (_gameWindow.ClientBounds.Width != 0 && _gameWindow.ClientBounds.Height != 0 && (_gameWindow.ClientBounds.Width != _screenWidth || _gameWindow.ClientBounds.Height != _screenHeight) && !_graphicsDeviceManager.IsFullScreen)
		{
			if (_previousDisplayMode != _graphicsDeviceManager.GraphicsDevice.DisplayMode)
			{
				_displayModeChanged = true;
			}
			UpdateGraphicsState(_screenWidth, _screenHeight);
		}
		return RecreateSpriteBatch();
	}

	protected override bool RecreateSpriteBatch()
	{
		if (_spriteBatchBegan)
		{
			EndSpriteBatch();
		}
		StartSpritebatch(_currentView, GameBlendModes.None, null, null, forced: true);
		return true;
	}

	public Microsoft.Xna.Framework.Vector2 GetMouseOffset()
	{
		return new Microsoft.Xna.Framework.Vector2((float)_graphicsDeviceManager.PreferredBackBufferWidth / (float)_gameWindow.ClientBounds.Width, (float)_graphicsDeviceManager.PreferredBackBufferHeight / (float)_gameWindow.ClientBounds.Height);
	}

	private void StartSpritebatch(FloatRect view, GameBlendModes mode = GameBlendModes.None, GameShader? shader = null, IGameRenderTexture? target = null, bool forced = false, RasterizerState? rs = null, bool drawImmediate = false)
	{
		bool flag = view.X != _currentSpriteView.X || view.Y != _currentSpriteView.Y || view.Width != _currentSpriteView.Width || view.Height != _currentSpriteView.Height;
		if (!(mode != _currentBlendmode || shader != _currentShader || (shader != null && shader.ValuesChanged()) || target != _currentRenderTarget || flag || forced || drawImmediate) && LinearFiltering == _currentLinearFiltering && _spriteBatchBegan)
		{
			return;
		}
		if (_spriteBatchBegan)
		{
			_spriteBatch.End();
		}
		if (target != null)
		{
			_graphicsDevice?.SetRenderTarget((RenderTarget2D)target.GetTexture());
		}
		else
		{
			_graphicsDevice?.SetRenderTarget(CibleParDefaut);
		}
		BlendState blendState = _normalState;
		Effect effect = null;
		switch (mode)
		{
		case GameBlendModes.None:
			blendState = _normalState;
			break;
		case GameBlendModes.Alpha:
			blendState = BlendState.AlphaBlend;
			break;
		case GameBlendModes.Multiply:
			blendState = _multiplyState;
			break;
		case GameBlendModes.Add:
			blendState = BlendState.Additive;
			break;
		case GameBlendModes.Opaque:
			blendState = BlendState.Opaque;
			break;
		case GameBlendModes.Cutout:
			blendState = _cutoutState;
			break;
		}
		if (target == null)
		{
			BlendState blendState2 = MelangeDansLInterface(mode);
			if (blendState2 != null)
			{
				blendState = blendState2;
			}
		}
		if (shader != null)
		{
			effect = (Effect)shader.GetShader();
			shader.ResetChanged();
		}
		_spriteBatch.Begin(drawImmediate ? SpriteSortMode.Immediate : SpriteSortMode.Deferred, blendState, LinearFiltering ? SamplerState.LinearClamp : SamplerState.PointClamp, null, rs, effect, CreateViewMatrix(view));
		base.LotsDeLImage++;
		_currentLinearFiltering = LinearFiltering;
		_currentSpriteView = view;
		_currentBlendmode = mode;
		_currentShader = shader;
		_currentRenderTarget = target;
		_spriteBatchBegan = true;
	}

	public override bool DisplayModeChanged()
	{
		bool displayModeChanged = _displayModeChanged;
		_displayModeChanged = false;
		return displayModeChanged;
	}

	public void EndSpriteBatch()
	{
		if (_spriteBatchBegan)
		{
			_spriteBatch.End();
		}
		_spriteBatchBegan = false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Microsoft.Xna.Framework.Color ConvertColor(Color color)
	{
		return new Microsoft.Xna.Framework.Color(color.R, color.G, color.B, color.A);
	}

	public override void Clear(Color color)
	{
		_graphicsDevice.Clear(ConvertColor(color));
	}

	protected override void PreDrawTileBuffer(GameTileBuffer buffer)
	{
		base.PreDrawTileBuffer(buffer);
		EndSpriteBatch();
		GraphicsDevice graphicsDevice = GraphicsDevice;
		graphicsDevice.SetRenderTarget(CibleParDefaut);
		graphicsDevice.BlendState = _normalState;
		graphicsDevice.RasterizerState = RasterizerState.CullCounterClockwise;
		graphicsDevice.SamplerStates[0] = SamplerState.PointClamp;
		graphicsDevice.DepthStencilState = DepthStencilState.None;
	}

	public override void Close()
	{
	}

	protected override IGameTexture CreateWhitePixel()
	{
		Texture2D texture2D = new Texture2D(_graphicsDevice, 1, 1);
		texture2D.SetData(new Microsoft.Xna.Framework.Color[1] { Microsoft.Xna.Framework.Color.White });
		return CreateGameTextureFromPlatformTexture("system:white_1px_1px", texture2D);
	}

	public ContentManager GetContentManager()
	{
		return _contentManager;
	}

	public override void DrawString(string text, IFont? font, int size, float x, float y, float fontScale, Color? fontColor, bool worldPos = true, IGameRenderTexture? renderTexture = null, Color? borderColor = null)
	{
		if (font is Font<SpriteFont> font2)
		{
			FontSizeRenderer<SpriteFont> rendererFor = font2.GetRendererFor(size);
			SpriteFont platformObject = rendererFor.PlatformObject;
			text = SanitizeText(text, platformObject);
			StartSpritebatch(_currentView, GameBlendModes.None, null, renderTexture);
			if (borderColor != null && borderColor != Color.Transparent)
			{
				Microsoft.Xna.Framework.Color color = ConvertColor(borderColor);
				_spriteBatch.DrawString(platformObject, text, new Microsoft.Xna.Framework.Vector2(x, y - fontScale), color, 0f, Microsoft.Xna.Framework.Vector2.Zero, new Microsoft.Xna.Framework.Vector2(fontScale, fontScale), SpriteEffects.None, 0f);
				_spriteBatch.DrawString(platformObject, text, new Microsoft.Xna.Framework.Vector2(x - fontScale, y), color, 0f, Microsoft.Xna.Framework.Vector2.Zero, new Microsoft.Xna.Framework.Vector2(fontScale, fontScale), SpriteEffects.None, 0f);
				_spriteBatch.DrawString(platformObject, text, new Microsoft.Xna.Framework.Vector2(x + fontScale, y), color, 0f, Microsoft.Xna.Framework.Vector2.Zero, new Microsoft.Xna.Framework.Vector2(fontScale, fontScale), SpriteEffects.None, 0f);
				_spriteBatch.DrawString(platformObject, text, new Microsoft.Xna.Framework.Vector2(x, y + fontScale), color, 0f, Microsoft.Xna.Framework.Vector2.Zero, new Microsoft.Xna.Framework.Vector2(fontScale, fontScale), SpriteEffects.None, 0f);
			}
			_spriteBatch.DrawString(platformObject, text, new Microsoft.Xna.Framework.Vector2(x, y), ConvertColor(fontColor), 0f, Microsoft.Xna.Framework.Vector2.Zero, new Microsoft.Xna.Framework.Vector2(fontScale, fontScale), SpriteEffects.None, 0f);
		}
	}

	public override void DrawString(string text, IFont? font, int size, float x, float y, float fontScale, Color fontColor, bool worldPos, IGameRenderTexture renderTexture, FloatRect clipRect, Color? borderColor = null)
	{
		if (font is Font<SpriteFont> font2)
		{
			FontSizeRenderer<SpriteFont> rendererFor = font2.GetRendererFor(size);
			SpriteFont platformObject = rendererFor.PlatformObject;
			text = SanitizeText(text, platformObject);
			x += _currentView.X;
			y += _currentView.Y;
			Microsoft.Xna.Framework.Color color = ConvertColor(fontColor);
			bool flag = !TexteDansLaDecoupe(platformObject, text, x, y, fontScale, borderColor != null && borderColor != Color.Transparent, clipRect);
			Microsoft.Xna.Framework.Rectangle scissorRectangle = _spriteBatch.GraphicsDevice.ScissorRectangle;
			if (!flag)
			{
				StartSpritebatch(_currentView, GameBlendModes.None, null, renderTexture);
			}
			else
			{
				StartSpritebatch(_currentView, GameBlendModes.None, null, renderTexture, forced: false, _rasterizerState, drawImmediate: true);
				_spriteBatch.GraphicsDevice.ScissorRectangle = new Microsoft.Xna.Framework.Rectangle((int)clipRect.X, (int)clipRect.Y, (int)clipRect.Width, (int)clipRect.Height);
			}
			if (borderColor != null && borderColor != Color.Transparent)
			{
				Microsoft.Xna.Framework.Color color2 = ConvertColor(borderColor);
				_spriteBatch.DrawString(platformObject, text, new Microsoft.Xna.Framework.Vector2(x, y - fontScale), color2, 0f, Microsoft.Xna.Framework.Vector2.Zero, new Microsoft.Xna.Framework.Vector2(fontScale, fontScale), SpriteEffects.None, 0f);
				_spriteBatch.DrawString(platformObject, text, new Microsoft.Xna.Framework.Vector2(x - fontScale, y), color2, 0f, Microsoft.Xna.Framework.Vector2.Zero, new Microsoft.Xna.Framework.Vector2(fontScale, fontScale), SpriteEffects.None, 0f);
				_spriteBatch.DrawString(platformObject, text, new Microsoft.Xna.Framework.Vector2(x + fontScale, y), color2, 0f, Microsoft.Xna.Framework.Vector2.Zero, new Microsoft.Xna.Framework.Vector2(fontScale, fontScale), SpriteEffects.None, 0f);
				_spriteBatch.DrawString(platformObject, text, new Microsoft.Xna.Framework.Vector2(x, y + fontScale), color2, 0f, Microsoft.Xna.Framework.Vector2.Zero, new Microsoft.Xna.Framework.Vector2(fontScale, fontScale), SpriteEffects.None, 0f);
			}
			_spriteBatch.DrawString(platformObject, text, new Microsoft.Xna.Framework.Vector2(x, y), color, 0f, Microsoft.Xna.Framework.Vector2.Zero, new Microsoft.Xna.Framework.Vector2(fontScale, fontScale), SpriteEffects.None, 0f);
			if (flag)
			{
				EndSpriteBatch();
				_spriteBatch.GraphicsDevice.ScissorRectangle = scissorRectangle;
			}
		}
	}

	public override GameTileBuffer CreateTileBuffer()
	{
		return new MonoTileBuffer(this, _graphicsDevice);
	}

	public override void DrawTexture(IGameTexture tex, float sx, float sy, float sw, float sh, float tx, float ty, float tw, float th, Color renderColor, IGameRenderTexture? renderTarget = null, GameBlendModes blendMode = GameBlendModes.None, GameShader? shader = null, float rotationDegrees = 0f, bool isUi = false, bool drawImmediate = false)
	{
		object texture = tex?.GetTexture();
		if (texture == null)
		{
			return;
		}
		bool flag = false;
		AtlasReference atlasReference = tex.AtlasReference;
		if (atlasReference != null)
		{
			if (atlasReference.IsRotated)
			{
				rotationDegrees -= 90f;
				float num = tw;
				tw = th;
				th = num;
				num = sx;
				sx = (float)atlasReference.Bounds.Right - sy - sh;
				sy = (float)atlasReference.Bounds.Top + num;
				num = sw;
				sw = sh;
				sh = num;
				flag = true;
			}
			else
			{
				sx += atlasReference.Bounds.X;
				sy += atlasReference.Bounds.Y;
			}
		}
		Microsoft.Xna.Framework.Vector2 origin = Microsoft.Xna.Framework.Vector2.Zero;
		if ((double)Math.Abs(rotationDegrees) > 0.01)
		{
			rotationDegrees = (float)(Math.PI / 180.0 * (double)rotationDegrees);
			origin = new Microsoft.Xna.Framework.Vector2(sw / 2f, sh / 2f);
			Microsoft.Xna.Framework.Vector2 pnt = new Microsoft.Xna.Framework.Vector2(0f, 0f);
			Microsoft.Xna.Framework.Vector2 pnt2 = new Microsoft.Xna.Framework.Vector2(tw, 0f);
			Microsoft.Xna.Framework.Vector2 pnt3 = new Microsoft.Xna.Framework.Vector2(0f, th);
			Microsoft.Xna.Framework.Vector2 ctr = new Microsoft.Xna.Framework.Vector2(tw / 2f, th / 2f);
			Microsoft.Xna.Framework.Vector2 vector = Rotate(pnt, ctr, rotationDegrees);
			Microsoft.Xna.Framework.Vector2 vector2 = Rotate(pnt2, ctr, rotationDegrees);
			Microsoft.Xna.Framework.Vector2 vector3 = Rotate(pnt3, ctr, rotationDegrees);
			int num2 = (int)Math.Round(GetDistance(vector.X, vector.Y, vector2.X, vector2.Y));
			int num3 = (int)Math.Round(GetDistance(vector.X, vector.Y, vector3.X, vector3.Y));
			if (flag)
			{
				int num4 = num3;
				num3 = num2;
				num2 = num4;
			}
			tx += (float)num2 / 2f;
			ty += (float)num3 / 2f;
		}
		Microsoft.Xna.Framework.Color color = ConvertColor(renderColor);
		if (renderTarget == null)
		{
			if (isUi)
			{
				tx += _currentView.X;
				ty += _currentView.Y;
			}
			Draw(_currentView, null);
		}
		else
		{
			Draw(new FloatRect(0f, 0f, renderTarget.Width, renderTarget.Height), renderTarget);
		}
		void Draw(FloatRect currentView, IGameRenderTexture targetObject)
		{
			StartSpritebatch(currentView, blendMode, shader, targetObject, forced: false, null, drawImmediate);
			_spriteBatch.Draw((Texture2D)texture, new Microsoft.Xna.Framework.Vector2(tx, ty), new Microsoft.Xna.Framework.Rectangle((int)sx, (int)sy, (int)sw, (int)sh), color, rotationDegrees, origin, new Microsoft.Xna.Framework.Vector2(tw / sw, th / sh), SpriteEffects.None, 0f);
			base.DessinsDeLImage++;
		}
	}

	private static double GetDistance(double x1, double y1, double x2, double y2)
	{
		double num = Math.Pow(x2 - x1, 2.0);
		double num2 = Math.Pow(y2 - y1, 2.0);
		return Math.Sqrt(num + num2);
	}

	private Microsoft.Xna.Framework.Vector2 Rotate(Microsoft.Xna.Framework.Vector2 pnt, Microsoft.Xna.Framework.Vector2 ctr, float angle)
	{
		return new Microsoft.Xna.Framework.Vector2((float)((double)pnt.X + (double)ctr.X * Math.Cos(angle) - (double)ctr.Y * Math.Sin(angle)), (float)((double)pnt.Y + (double)ctr.X * Math.Sin(angle) + (double)ctr.Y * Math.Cos(angle)));
	}

	protected override void DoEnd()
	{
		EndSpriteBatch();
		GraphicsDevice graphicsDevice = _graphicsDevice;
		if (graphicsDevice != null)
		{
			base.EnvoisDeLImagePrecedente = graphicsDevice.Metrics.DrawCount;
			base.TexturesDeLImagePrecedente = graphicsDevice.Metrics.TextureCount;
		}
		_frames++;
		long millisecondsUtc = Timing.Global.MillisecondsUtc;
		if (_nextFpsTime <= millisecondsUtc)
		{
			long num = millisecondsUtc - _nextFpsTime;
			float num2 = (float)num / 1000f;
			int frames = _frames;
			int num3 = (int)((float)frames * num2);
			_fps = frames;
			_frames = 0;
			_nextFpsTime = millisecondsUtc + 1000;
			_gameWindow.Title = Strings.Main.GameName;
		}
	}

	public override string GetResolutionString()
	{
		return _screenWidth + "x" + _screenHeight;
	}

	public Resolution[] GetAllowedResolutions(bool anciennes = false)
	{
		Resolution plancher = (anciennes ? new Resolution(800L, 480L) : ResolutionMinimale);
		Resolution[] result = ((!anciennes) ? ((IEnumerable<Resolution>)Array.Empty<Resolution>()) : ((IEnumerable<Resolution>)new Resolution[3]
		{
			new Resolution(800L, 600L),
			new Resolution(1024L, 768L),
			new Resolution(1024L, 720L)
		})).Concat(new Resolution[10]
		{
			new Resolution(1280L, 720L),
			new Resolution(1280L, 768L),
			new Resolution(1280L, 1024L),
			new Resolution(1360L, 768L),
			new Resolution(1366L, 768L),
			new Resolution(1440L, 1050L),
			new Resolution(1440L, 900L),
			new Resolution(1600L, 900L),
			new Resolution(1680L, 1050L),
			new Resolution(1920L, 1080L)
		}).Concat(from displayMode in _graphicsDevice.Adapter.SupportedDisplayModes
			select new Resolution(displayMode.Width, displayMode.Height) into resolution
			where resolution.X >= plancher.X && resolution.Y >= plancher.Y
			select resolution).Distinct()
			.Order()
			.ToArray();
		if (!OperatingSystem.IsMacOS() && !OperatingSystem.IsAndroid() && Steam.SteamDeck)
		{
			result = new Resolution[1]
			{
				new Resolution(_graphicsDevice.DisplayMode.Width, _graphicsDevice.DisplayMode.Height)
			};
		}
		return result;
	}

	public override FloatRect GetView()
	{
		return _currentView;
	}

	public override void Init()
	{
		if (_initializing)
		{
			return;
		}
		_initializing = true;
		GameDatabase database = Globals.Database;
		List<string> validVideoModes = ValidVideoModes;
		TraduireLaResolutionEnregistree(database, validVideoModes);
		if (database.TargetResolution < 0)
		{
			Resolution[] allowedResolutions = GetAllowedResolutions();
			DisplayMode currentDisplayMode = _graphicsDevice.Adapter.CurrentDisplayMode;
			Resolution resolution = allowedResolutions.LastOrDefault((Resolution allowedResolution) => currentDisplayMode.Width != allowedResolution.X && currentDisplayMode.Width == allowedResolution.X * currentDisplayMode.Height / allowedResolution.Y);
			if (resolution == default(Resolution))
			{
				resolution = allowedResolutions.LastOrDefault((Resolution allowedResolution) => allowedResolution.X * 9 / 16 == allowedResolution.Y && allowedResolution.X < currentDisplayMode.Width && allowedResolution.Y < currentDisplayMode.Height);
			}
			if (resolution != default(Resolution))
			{
				database.TargetResolution = validVideoModes.IndexOf(resolution.ToString());
			}
		}
		int num = Intersect.Utilities.MathHelper.Clamp(database.TargetResolution, 0, validVideoModes.Count - 1);
		if (num != database.TargetResolution)
		{
			database.TargetResolution = num;
			database.SavePreference("Resolution", database.TargetResolution.ToString(CultureInfo.InvariantCulture));
		}
		string text = validVideoModes[num];
		if (!string.IsNullOrWhiteSpace(text) && Resolution.TryParse(text, out var resolution2))
		{
			base.PreferredResolution = resolution2;
		}
		_graphicsDeviceManager.PreferredBackBufferWidth = base.PreferredResolution.X;
		_graphicsDeviceManager.PreferredBackBufferHeight = base.PreferredResolution.Y;
		UpdateGraphicsState(base.ActiveResolution.X, base.ActiveResolution.Y, initial: true);
		_initializing = false;
	}

	public void Init(GraphicsDevice graphicsDevice)
	{
		ArgumentNullException.ThrowIfNull(graphicsDevice, "graphicsDevice");
		_graphicsDevice = graphicsDevice;
		_basicEffect = new BasicEffect(graphicsDevice);
		_basicEffect.LightingEnabled = false;
		_basicEffect.TextureEnabled = true;
		_spriteBatch = new SpriteBatch(graphicsDevice);
		_basicShader = new Shader(this, _basicEffect);
	}

	public override IFont LoadFont(string fontName, IDictionary<int, FileInfo> fontSourcesBySize)
	{
		return new MonoFont(fontName, fontSourcesBySize, _contentManager);
	}

	public override GameShader LoadShader(string shaderName)
	{
		return new Shader(this, shaderName, _contentManager);
	}

	public override System.Numerics.Vector2 MeasureText(string? text, IFont? font, int size, float fontScale)
	{
		if (string.IsNullOrWhiteSpace(text))
		{
			return default(System.Numerics.Vector2);
		}
		if (!(font is Font<SpriteFont> font2))
		{
			return default(System.Numerics.Vector2);
		}
		FontSizeRenderer<SpriteFont> rendererFor = font2.GetRendererFor(size);
		SpriteFont platformObject = rendererFor.PlatformObject;
		text = SanitizeText(text, platformObject);
		Microsoft.Xna.Framework.Vector2 vector = platformObject.MeasureString(text);
		return new System.Numerics.Vector2(vector.X * fontScale, vector.Y * fontScale);
	}

	public override (float Top, float Bottom) MeasureTextInk(string? text, IFont? font, int size, float fontScale)
	{
		if (string.IsNullOrWhiteSpace(text) || !(font is Font<SpriteFont> font2))
		{
			return base.MeasureTextInk(text, font, size, fontScale);
		}
		SpriteFont platformObject = font2.GetRendererFor(size).PlatformObject;
		text = SanitizeText(text, platformObject);
		Dictionary<char, SpriteFont.Glyph> glyphs = platformObject.GetGlyphs();
		float num = float.MaxValue;
		float num2 = float.MinValue;
		string text2 = text;
		foreach (char c in text2)
		{
			if (!char.IsWhiteSpace(c) && glyphs.TryGetValue(c, out var value) && value.BoundsInTexture.Height > 0)
			{
				num = Math.Min(num, value.Cropping.Y);
				num2 = Math.Max(num2, value.Cropping.Y + value.BoundsInTexture.Height);
			}
		}
		if (!(num > num2))
		{
			return (Top: num * fontScale, Bottom: num2 * fontScale);
		}
		return base.MeasureTextInk(text, font, size, fontScale);
	}

	public override System.Numerics.Vector2 MeasureTextScaled(string? text, IFont? font, int size, float scale, float extraSpacing = 0f)
	{
		if (string.IsNullOrEmpty(text) || !(font is Font<SpriteFont> font2))
		{
			return default(System.Numerics.Vector2);
		}
		SpriteFont platformObject = font2.GetRendererFor(size).PlatformObject;
		text = SanitizeText(text, platformObject);
		float spacing = platformObject.Spacing;
		platformObject.Spacing = spacing + extraSpacing;
		Microsoft.Xna.Framework.Vector2 vector = platformObject.MeasureString(text);
		platformObject.Spacing = spacing;
		return new System.Numerics.Vector2(vector.X * scale, vector.Y * scale);
	}

	public override void DrawStringScaled(string text, IFont? font, int size, float x, float y, float scale, Color color, float extraSpacing = 0f)
	{
		if (!string.IsNullOrEmpty(text) && font is Font<SpriteFont> font2 && _spriteBatch != null)
		{
			SpriteFont platformObject = font2.GetRendererFor(size).PlatformObject;
			text = SanitizeText(text, platformObject);
			StartSpritebatch(_currentView);
			float spacing = platformObject.Spacing;
			platformObject.Spacing = spacing + extraSpacing;
			_spriteBatch.DrawString(platformObject, text, new Microsoft.Xna.Framework.Vector2(x + _currentView.X, y + _currentView.Y), ConvertColor(color), 0f, Microsoft.Xna.Framework.Vector2.Zero, new Microsoft.Xna.Framework.Vector2(scale, scale), SpriteEffects.None, 0f);
			platformObject.Spacing = spacing;
		}
	}

	public override IGameTexture? CreateTextureFromPixels(string name, int width, int height, byte[] rgba, Func<Stream>? sourceFactory = null)
	{
		if (_graphicsDevice == null || width <= 0 || height <= 0 || rgba.Length < width * height * 4)
		{
			return null;
		}
		Texture2D texture2D = new Texture2D(_graphicsDevice, width, height);
		texture2D.SetData(rgba, 0, width * height * 4);
		return CreateGameTextureFromPlatformTexture(name, texture2D, sourceFactory);
	}

	public override bool TryReadPixels(IGameTexture texture, out byte[] rgba, out int width, out int height)
	{
		rgba = Array.Empty<byte>();
		width = 0;
		height = 0;
		Texture2D texture2 = texture.GetTexture<Texture2D>();
		if (texture2 != null)
		{
			AtlasReference atlasReference = texture.AtlasReference;
			if ((object)atlasReference == null || !atlasReference.IsRotated)
			{
				AtlasReference atlasReference2 = texture.AtlasReference;
				Microsoft.Xna.Framework.Rectangle value = (((object)atlasReference2 != null) ? new Microsoft.Xna.Framework.Rectangle(atlasReference2.Bounds.X, atlasReference2.Bounds.Y, atlasReference2.Bounds.Width, atlasReference2.Bounds.Height) : new Microsoft.Xna.Framework.Rectangle(0, 0, texture2.Width, texture2.Height));
				try
				{
					Microsoft.Xna.Framework.Color[] array = new Microsoft.Xna.Framework.Color[value.Width * value.Height];
					texture2.GetData(0, value, array, 0, array.Length);
					rgba = new byte[array.Length * 4];
					for (int i = 0; i < array.Length; i++)
					{
						Microsoft.Xna.Framework.Color color = array[i];
						rgba[i * 4] = color.R;
						rgba[i * 4 + 1] = color.G;
						rgba[i * 4 + 2] = color.B;
						rgba[i * 4 + 3] = color.A;
					}
				}
				catch
				{
					rgba = Array.Empty<byte>();
					return false;
				}
				width = value.Width;
				height = value.Height;
				return true;
			}
		}
		return false;
	}

	protected override void OnCursorChanged(IGameTexture? newCursor)
	{
		_game.IsMouseVisible = newCursor == null;
	}

	private string SanitizeText(string text, SpriteFont spriteFont)
	{
		if (!_defaultCharacterForSpriteFont.TryGetValue(spriteFont, out var value))
		{
			value = spriteFont.DefaultCharacter ?? (spriteFont.Characters.Contains(' ') ? ' ' : spriteFont.Characters.FirstOrDefault());
			_defaultCharacterForSpriteFont[spriteFont] = value;
		}
		return RemplacerLesLettresInconnues(text, LettresDe(spriteFont), value);
	}

	private Matrix CreateViewMatrix(FloatRect view)
	{
		return Matrix.CreateRotationZ(0f) * Matrix.CreateTranslation(0f - view.X, 0f - view.Y, 0f) * Matrix.CreateScale(new Microsoft.Xna.Framework.Vector3(base.Scale));
	}

	public override void SetView(FloatRect view)
	{
		_currentView = view;
		Matrix.CreateOrthographicOffCenter(0f, view.Width, view.Height, 0f, 0f, -1f, out var result);
		result.M41 += -0.5f * result.M11;
		result.M42 += -0.5f * result.M22;
		_basicEffect.Projection = result;
		_basicEffect.View = CreateViewMatrix(view);
	}

	public override bool BeginScreenshot()
	{
		GraphicsDevice graphicsDevice = GraphicsDevice;
		if (_screenWidth < 1 || _screenHeight < 1)
		{
			return false;
		}
		_screenshotRenderTarget = new RenderTarget2D(graphicsDevice, _screenWidth, _screenHeight, mipMap: false, graphicsDevice.PresentationParameters.BackBufferFormat, graphicsDevice.PresentationParameters.DepthStencilFormat, 0, RenderTargetUsage.PreserveContents);
		return true;
	}

	private bool WriteScreenshotRenderTargetAsPngTo(Stream stream, string? hint)
	{
		if (_screenshotRenderTarget.IsContentLost)
		{
			return false;
		}
		try
		{
			_screenshotRenderTarget.SaveAsPng(stream, _screenshotRenderTarget.Width, _screenshotRenderTarget.Height);
			return true;
		}
		catch (Exception exception)
		{
			ApplicationContext.CurrentContext.Logger.LogWarning(exception, "Exception when writing screenshot to {Target}", string.IsNullOrWhiteSpace(hint) ? "stream" : hint);
			return false;
		}
	}

	public override void EndScreenshot()
	{
		if (_screenshotRenderTarget != null)
		{
			ProcessScreenshots(WriteScreenshotRenderTargetAsPngTo);
			GraphicsDevice graphicsDevice = GraphicsDevice;
			RenderTarget2D screenshotRenderTarget = _screenshotRenderTarget;
			_screenshotRenderTarget = null;
			graphicsDevice.SetRenderTarget(null);
			if (Begin())
			{
				_spriteBatch?.Draw(screenshotRenderTarget, default(Microsoft.Xna.Framework.Rectangle), Microsoft.Xna.Framework.Color.White);
				End();
			}
		}
	}

	private ulong[] LettresDe(SpriteFont police)
	{
		if (!_lettresDesPolices.TryGetValue(police, out ulong[] value))
		{
			value = new ulong[1024];
			foreach (char character in police.Characters)
			{
				value[(int)character >> 6] |= (ulong)(1L << (int)character);
			}
			_lettresDesPolices[police] = value;
		}
		return value;
	}

	private static bool Connue(ulong[] lettres, char lettre)
	{
		return (lettres[(int)lettre >> 6] & (ulong)(1L << (int)lettre)) != 0;
	}

	private static string RemplacerLesLettresInconnues(string texte, ulong[] lettres, char parDefaut)
	{
		for (int i = 0; i < texte.Length; i++)
		{
			if (Connue(lettres, texte[i]))
			{
				continue;
			}
			char[] array = texte.ToCharArray();
			for (int j = i; j < array.Length; j++)
			{
				if (!Connue(lettres, array[j]))
				{
					array[j] = parDefaut;
				}
			}
			return new string(array);
		}
		return texte;
	}

	private bool TexteDansLaDecoupe(SpriteFont police, string texte, float x, float y, float echelle, bool contour, FloatRect decoupe)
	{
		if (!_glyphesDesPolices.TryGetValue(police, out Dictionary<char, SpriteFont.Glyph> value))
		{
			value = police.GetGlyphs();
			_glyphesDesPolices[police] = value;
		}
		float num = float.MaxValue;
		float num2 = float.MaxValue;
		float num3 = float.MinValue;
		float num4 = float.MinValue;
		float num5 = 0f;
		float num6 = 0f;
		bool flag = true;
		foreach (char c in texte)
		{
			switch (c)
			{
			case '\n':
				num5 = 0f;
				num6 += (float)police.LineSpacing;
				flag = true;
				continue;
			case '\r':
				continue;
			}
			if (!value.TryGetValue(c, out var value2))
			{
				char? defaultCharacter = police.DefaultCharacter;
				if (defaultCharacter.HasValue)
				{
					char valueOrDefault = defaultCharacter.GetValueOrDefault();
					if (value.TryGetValue(valueOrDefault, out value2))
					{
						goto IL_00c2;
					}
				}
				return false;
			}
			goto IL_00c2;
			IL_00c2:
			if (flag)
			{
				num5 = Math.Max(value2.LeftSideBearing, 0f);
				flag = false;
			}
			else
			{
				num5 += police.Spacing + value2.LeftSideBearing;
			}
			if (value2.BoundsInTexture.Width > 0 && value2.BoundsInTexture.Height > 0)
			{
				float num7 = num5 + (float)value2.Cropping.X;
				float num8 = num6 + (float)value2.Cropping.Y;
				num = Math.Min(num, num7);
				num2 = Math.Min(num2, num8);
				num3 = Math.Max(num3, num7 + (float)value2.BoundsInTexture.Width);
				num4 = Math.Max(num4, num8 + (float)value2.BoundsInTexture.Height);
			}
			num5 += value2.Width + value2.RightSideBearing;
		}
		if (num > num3)
		{
			return true;
		}
		float num9 = (contour ? echelle : 0f);
		float scale = base.Scale;
		float num10 = (x + num * echelle - num9 - _currentView.X) * scale;
		float num11 = (x + num3 * echelle + num9 - _currentView.X) * scale;
		float num12 = (y + num2 * echelle - num9 - _currentView.Y) * scale;
		float num13 = (y + num4 * echelle + num9 - _currentView.Y) * scale;
		int num14 = (int)decoupe.X;
		int num15 = (int)decoupe.Y;
		if (num10 >= (float)num14 && num12 >= (float)num15 && num11 <= (float)(num14 + (int)decoupe.Width))
		{
			return num13 <= (float)(num15 + (int)decoupe.Height);
		}
		return false;
	}

	private static Microsoft.Xna.Framework.Graphics.BufferUsage BufferUsageToMonoGameBufferUsage(Intersect.Client.Framework.Graphics.BufferUsage usage)
	{
		return usage switch
		{
			Intersect.Client.Framework.Graphics.BufferUsage.None => Microsoft.Xna.Framework.Graphics.BufferUsage.None, 
			Intersect.Client.Framework.Graphics.BufferUsage.WriteOnly => Microsoft.Xna.Framework.Graphics.BufferUsage.WriteOnly, 
			_ => throw Exceptions.UnreachableInvalidEnum(usage), 
		};
	}

	private bool AddAllocatedIndexBuffer(Microsoft.Xna.Framework.Graphics.IndexBuffer platformBuffer, IIndexBuffer buffer)
	{
		if (!_allocatedIndexBuffers.TryAdd(platformBuffer, buffer))
		{
			return false;
		}
		MarkAllocated(buffer);
		_allocatedIndexBuffersSize += MeasureDataSize(platformBuffer);
		return true;
	}

	private bool RemoveAllocatedIndexBuffer(Microsoft.Xna.Framework.Graphics.IndexBuffer platformBuffer, [NotNullWhen(true)] out IIndexBuffer? buffer)
	{
		if (!_allocatedIndexBuffers.TryRemove(platformBuffer, out buffer))
		{
			return false;
		}
		_allocatedIndexBuffersSize -= MeasureDataSize(platformBuffer);
		return true;
	}

	private static long MeasureDataSize(Microsoft.Xna.Framework.Graphics.IndexBuffer platformBuffer)
	{
		int indexCount = platformBuffer.IndexCount;
		return indexCount * platformBuffer.IndexElementSize switch
		{
			IndexElementSize.SixteenBits => 2, 
			IndexElementSize.ThirtyTwoBits => 4, 
			_ => throw Exceptions.UnreachableInvalidEnum(platformBuffer.IndexElementSize), 
		};
	}

	public override IIndexBuffer CreateIndexBuffer<TIndex>(int count, Intersect.Client.Framework.Graphics.BufferUsage usage = Intersect.Client.Framework.Graphics.BufferUsage.None, bool dynamic = false)
	{
		Microsoft.Xna.Framework.Graphics.BufferUsage usage2 = BufferUsageToMonoGameBufferUsage(usage);
		Microsoft.Xna.Framework.Graphics.IndexBuffer indexBuffer = ((!dynamic) ? new Microsoft.Xna.Framework.Graphics.IndexBuffer(_graphicsDevice, typeof(TIndex), count, usage2) : new DynamicIndexBuffer(_graphicsDevice, typeof(TIndex), count, usage2));
		IndexBuffer indexBuffer2 = new IndexBuffer(indexBuffer, typeof(TIndex));
		indexBuffer.Disposing += IndexBufferOnDisposing;
		if (AddAllocatedIndexBuffer(indexBuffer, indexBuffer2))
		{
			return indexBuffer2;
		}
		indexBuffer2.Dispose();
		throw new InvalidOperationException("Unable to track index buffer");
	}

	private void IndexBufferOnDisposing(object? sender, EventArgs args)
	{
		if (sender is Microsoft.Xna.Framework.Graphics.IndexBuffer platformBuffer)
		{
			OnPlatformIndexBufferDisposal(platformBuffer);
			return;
		}
		ApplicationContext.CurrentContext.Logger.LogError("Received disposal event but it was not from an instance of {ExpectedType} ({ActualType})", typeof(Microsoft.Xna.Framework.Graphics.IndexBuffer).GetName(qualified: true), sender?.GetType().GetName(qualified: true) ?? "null");
	}

	private void OnPlatformIndexBufferDisposal(Microsoft.Xna.Framework.Graphics.IndexBuffer platformBuffer)
	{
		if (RemoveAllocatedIndexBuffer(platformBuffer, out IIndexBuffer buffer))
		{
			MarkFreed(buffer);
			return;
		}
		ApplicationContext.CurrentContext.Logger.LogError("Failed to remove platform buffer from allocations, is it not tracked? '{BufferId}'", platformBuffer.ToString());
	}

	private BlendState? MelangeDansLInterface(GameBlendModes mode)
	{
		if (!_interfaceReduite)
		{
			return null;
		}
		return mode switch
		{
			GameBlendModes.None => NormalDansLImage, 
			GameBlendModes.Add => AdditifDansLImage, 
			_ => null, 
		};
	}

	internal bool CommencerInterfaceReduite(int largeur, int hauteur)
	{
		if (!_interfaceReduite && !_mondeLisse && !_cimesOuvertes)
		{
			GraphicsDevice graphicsDevice = _graphicsDevice;
			if (graphicsDevice != null && _spriteBatch != null && largeur >= 1 && hauteur >= 1)
			{
				EndSpriteBatch();
				if (_cibleInterface == null || _cibleInterface.IsDisposed || _cibleInterface.Width != largeur || _cibleInterface.Height != hauteur)
				{
					_cibleInterface?.Dispose();
					_cibleInterface = new RenderTarget2D(graphicsDevice, largeur, hauteur, mipMap: false, SurfaceFormat.Color, graphicsDevice.PresentationParameters.DepthStencilFormat, 0, RenderTargetUsage.PreserveContents);
				}
				_largeurInterface = largeur;
				_hauteurInterface = hauteur;
				_interfaceReduite = true;
				graphicsDevice.SetRenderTarget(_cibleInterface);
				graphicsDevice.Clear(Microsoft.Xna.Framework.Color.Transparent);
				return true;
			}
		}
		return false;
	}

	internal void FinirInterfaceReduite()
	{
		if (!_interfaceReduite)
		{
			return;
		}
		EndSpriteBatch();
		_interfaceReduite = false;
		if (_cibleInterface != null)
		{
			GraphicsDevice graphicsDevice = _graphicsDevice;
			if (graphicsDevice != null && _spriteBatch != null)
			{
				bool flag = _screenWidth > _largeurInterface && _screenWidth % _largeurInterface == 0 && _screenHeight % _hauteurInterface == 0 && _screenWidth / _largeurInterface == _screenHeight / _hauteurInterface;
				graphicsDevice.SetRenderTarget(_screenshotRenderTarget);
				_spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, flag ? SamplerState.PointClamp : SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone);
				_spriteBatch.Draw(_cibleInterface, new Microsoft.Xna.Framework.Rectangle(0, 0, _screenWidth, _screenHeight), Microsoft.Xna.Framework.Color.White);
				_spriteBatch.End();
			}
		}
	}

	internal bool CommencerMondeAgrandi(int largeur, int hauteur)
	{
		if (!_interfaceReduite && !_mondeLisse && !_cimesOuvertes)
		{
			GraphicsDevice graphicsDevice = _graphicsDevice;
			if (graphicsDevice != null && _spriteBatch != null && largeur >= 1 && hauteur >= 1)
			{
				EndSpriteBatch();
				if (_cibleDuMondeAgrandi == null || _cibleDuMondeAgrandi.IsDisposed || _cibleDuMondeAgrandi.Width != largeur || _cibleDuMondeAgrandi.Height != hauteur)
				{
					_cibleDuMondeAgrandi?.Dispose();
					_cibleDuMondeAgrandi = new RenderTarget2D(graphicsDevice, largeur, hauteur, mipMap: false, SurfaceFormat.Color, graphicsDevice.PresentationParameters.DepthStencilFormat, 0, RenderTargetUsage.PreserveContents);
				}
				_largeurDuMondeAgrandi = largeur;
				_hauteurDuMondeAgrandi = hauteur;
				_mondeAgrandi = true;
				graphicsDevice.SetRenderTarget(_cibleDuMondeAgrandi);
				graphicsDevice.Clear(Microsoft.Xna.Framework.Color.Black);
				return true;
			}
		}
		_mondeAgrandi = false;
		return false;
	}

	internal void FinirMondeAgrandi()
	{
		if (!_mondeAgrandi)
		{
			return;
		}
		EndSpriteBatch();
		_mondeAgrandi = false;
		if (_cibleDuMondeAgrandi != null)
		{
			GraphicsDevice graphicsDevice = _graphicsDevice;
			if (graphicsDevice != null && _spriteBatch != null)
			{
				bool flag = _screenWidth > _largeurDuMondeAgrandi && _screenWidth % _largeurDuMondeAgrandi == 0 && _screenHeight % _hauteurDuMondeAgrandi == 0 && _screenWidth / _largeurDuMondeAgrandi == _screenHeight / _hauteurDuMondeAgrandi;
				graphicsDevice.SetRenderTarget(_screenshotRenderTarget);
				_spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Opaque, flag ? SamplerState.PointClamp : SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone);
				_spriteBatch.Draw(_cibleDuMondeAgrandi, new Microsoft.Xna.Framework.Rectangle(0, 0, _screenWidth, _screenHeight), Microsoft.Xna.Framework.Color.White);
				_spriteBatch.End();
			}
		}
	}

	internal IGameTexture CreerTexturePixels(string nom, int largeur, int hauteur, byte[] rgba)
	{
		Texture2D texture2D = new Texture2D(_graphicsDevice, largeur, hauteur, mipmap: false, SurfaceFormat.Color);
		texture2D.SetData(rgba);
		MonoTexture monoTexture = new MonoTexture(this, nom, texture2D);
		texture2D.Disposing += Texture2DOnDisposing;
		AddAllocatedTexture(texture2D, monoTexture);
		return monoTexture;
	}

	internal static bool RemplacerPixels(IGameTexture texture, byte[] rgba)
	{
		if (!(texture.GetTexture() is Texture2D { IsDisposed: false } texture2D) || rgba.Length != texture2D.Width * texture2D.Height * 4)
		{
			return false;
		}
		texture2D.SetData(rgba);
		return true;
	}

	internal static bool RemplacerLignes(IGameTexture texture, byte[] rgba, int lignes)
	{
		if (!(texture.GetTexture() is Texture2D { IsDisposed: false } texture2D) || rgba.Length != texture2D.Width * texture2D.Height * 4 || lignes <= 0)
		{
			return false;
		}
		lignes = Math.Min(lignes, texture2D.Height);
		texture2D.SetData(0, new Microsoft.Xna.Framework.Rectangle(0, 0, texture2D.Width, lignes), rgba, 0, texture2D.Width * lignes * 4);
		return true;
	}

	internal void LireDAvance(IEnumerable<(IGameTexture Texture, string Fichier)> textures)
	{
		_enCoursDePose?.Image?.Dispose();
		_enCoursDePose = null;
		lock (_verrouDesLectures)
		{
			_generationDesLectures++;
			_aLire.Clear();
			_aPoser.Clear();
			foreach (var (gameTexture, fichier) in textures)
			{
				if (gameTexture.AtlasReference == null)
				{
					_aLire.Enqueue(new LectureDAvance(gameTexture, fichier));
				}
			}
			if (!_lecteurActif && _aLire.Count > 0)
			{
				_lecteurActif = true;
				int generation = _generationDesLectures;
				Task.Run(delegate
				{
					Lire(generation);
				});
			}
			Monitor.PulseAll(_verrouDesLectures);
		}
	}

	private void Lire(int generation)
	{
		while (true)
		{
			LectureDAvance lectureDAvance;
			lock (_verrouDesLectures)
			{
				while (generation == _generationDesLectures && _aPoser.Count >= 2)
				{
					Monitor.Wait(_verrouDesLectures, 100);
				}
				if (generation != _generationDesLectures)
				{
					generation = _generationDesLectures;
				}
				if (_aLire.Count == 0)
				{
					_lecteurActif = false;
					break;
				}
				lectureDAvance = _aLire.Dequeue();
			}
			if (lectureDAvance.Texture.IsLoaded)
			{
				continue;
			}
			try
			{
				using Stream flux = Coffre.Ouvrir(lectureDAvance.Fichier);
				(byte[], int, int)? tuple = PngBrut.Lire(flux);
				if (!tuple.HasValue)
				{
					continue;
				}
				(lectureDAvance.Rgba, lectureDAvance.Largeur, lectureDAvance.Hauteur) = tuple.GetValueOrDefault();
				goto IL_0124;
			}
			catch (Exception exception)
			{
				ApplicationContext.Context.Value?.Logger.LogWarning(exception, "Lecture d'avance de {Fichier} impossible", lectureDAvance.Fichier);
			}
			continue;
			IL_0124:
			lock (_verrouDesLectures)
			{
				if (generation == _generationDesLectures)
				{
					_aPoser.Enqueue(lectureDAvance);
				}
			}
		}
	}

	internal void ServirLesLecturesDAvance()
	{
		if (_enCoursDePose == null)
		{
			lock (_verrouDesLectures)
			{
				if (_aPoser.Count == 0)
				{
					return;
				}
				_enCoursDePose = _aPoser.Dequeue();
				Monitor.PulseAll(_verrouDesLectures);
			}
		}
		LectureDAvance enCoursDePose = _enCoursDePose;
		if (enCoursDePose.Texture is MonoTexture { IsLoaded: false } monoTexture)
		{
			byte[] rgba = enCoursDePose.Rgba;
			if (rgba != null && _graphicsDevice != null)
			{
				using (ChronoImage.Etape("jeux de tuiles lus d'avance"))
				{
					LectureDAvance lectureDAvance = enCoursDePose;
					if (lectureDAvance.Image == null)
					{
						lectureDAvance.Image = new Texture2D(_graphicsDevice, enCoursDePose.Largeur, enCoursDePose.Hauteur, mipmap: false, SurfaceFormat.Color);
					}
					int num = enCoursDePose.Largeur * 4;
					int num2 = Math.Clamp(1572864 / num, 1, enCoursDePose.Hauteur - enCoursDePose.LignesPosees);
					enCoursDePose.Image.SetData(0, new Microsoft.Xna.Framework.Rectangle(0, enCoursDePose.LignesPosees, enCoursDePose.Largeur, num2), rgba, enCoursDePose.LignesPosees * num, num2 * num);
					enCoursDePose.LignesPosees += num2;
					if (enCoursDePose.LignesPosees >= enCoursDePose.Hauteur)
					{
						_enCoursDePose = null;
						enCoursDePose.Rgba = null;
						Texture2D image = enCoursDePose.Image;
						image.Disposing += Texture2DOnDisposing;
						if (!AddAllocatedTexture(image, monoTexture))
						{
							image.Disposing -= Texture2DOnDisposing;
							image.Dispose();
						}
						else if (!monoTexture.Poser(image))
						{
							image.Disposing -= Texture2DOnDisposing;
							RemoveAllocatedTexture(image, out IGameTexture _);
							image.Dispose();
						}
					}
					return;
				}
			}
		}
		enCoursDePose.Image?.Dispose();
		_enCoursDePose = null;
	}

	public override IGameRenderTexture CreateRenderTexture(int width, int height)
	{
		RenderTarget2D renderTarget2D = CreatePlatformRenderTexture(width, height);
		MonoRenderTexture monoRenderTexture = new MonoRenderTexture(this, renderTarget2D);
		if (!_allocatedTextures.TryAdd(renderTarget2D, monoRenderTexture))
		{
			throw new InvalidOperationException("Failed to record allocated texture");
		}
		return monoRenderTexture;
	}

	private RenderTarget2D CreatePlatformRenderTexture(int width, int height)
	{
		RenderTarget2D renderTarget2D = new RenderTarget2D(_graphicsDevice, width, height, mipMap: false, _graphicsDevice.PresentationParameters.BackBufferFormat, _graphicsDevice.PresentationParameters.DepthStencilFormat, 0, RenderTargetUsage.PreserveContents);
		renderTarget2D.Disposing += Texture2DOnDisposing;
		return renderTarget2D;
	}

	private List<string> ModesQuiTiennentALEcran(Resolution[] resolutions)
	{
		int? num = _graphicsDevice?.DisplayMode?.Width;
		int? num2 = _graphicsDevice?.DisplayMode?.Height;
		List<string> list = new List<string>();
		for (int i = 0; i < resolutions.Length; i++)
		{
			Resolution resolution = resolutions[i];
			if (!(resolution.X > num) && !(resolution.Y > num2))
			{
				list.Add(resolution.ToString());
			}
		}
		return list;
	}

	private void TraduireLaResolutionEnregistree(GameDatabase database, List<string> modes)
	{
		if (!database.LoadPreference("ResolutionsDepuis1280", defaultValue: false))
		{
			int targetResolution = database.TargetResolution;
			if (targetResolution >= 0)
			{
				List<string> list = ModesQuiTiennentALEcran(GetAllowedResolutions(anciennes: true));
				database.TargetResolution = ((targetResolution < list.Count) ? modes.IndexOf(list[targetResolution]) : (-1));
				database.SavePreference("TargetResolution", database.TargetResolution);
			}
			database.SavePreference("ResolutionsDepuis1280", value: true);
		}
	}

	private static void AppliquerLaSynchroAdaptative()
	{
		try
		{
			if (Sdl2.SDL_GL_SetSwapInterval(-1) == 0)
			{
				Synchro = "adaptative";
				return;
			}
			Sdl2.SDL_GL_SetSwapInterval(1);
			Synchro = "ordinaire (l'adaptative refusee par le pilote)";
		}
		catch (Exception ex)
		{
			Synchro = "ordinaire (" + ex.GetType().Name + ")";
		}
	}

	private bool AddAllocatedTexture(Texture2D platformTexture, IGameTexture gameTexture)
	{
		if (!_allocatedTextures.TryAdd(platformTexture, gameTexture))
		{
			return false;
		}
		_allocatedTexturesSize += MeasureDataSize(platformTexture);
		return true;
	}

	private bool RemoveAllocatedTexture(Texture2D platformTexture, [NotNullWhen(true)] out IGameTexture? gameTexture)
	{
		if (!_allocatedTextures.TryRemove(platformTexture, out gameTexture))
		{
			return false;
		}
		_allocatedTexturesSize -= MeasureDataSize(platformTexture);
		return true;
	}

	private static long MeasureDataSize(Texture2D platformTexture)
	{
		int width = platformTexture.Width;
		int height = platformTexture.Height;
		return width * height * 4;
	}

	private Texture2D? CreatePlatformTextureFromStream(MonoTexture gameTexture, Stream stream)
	{
		Texture2D texture2D = Texture2D.FromStream(_graphicsDevice, stream);
		if (texture2D == null)
		{
			return texture2D;
		}
		texture2D.Disposing += Texture2DOnDisposing;
		if (!AddAllocatedTexture(texture2D, gameTexture))
		{
			throw new InvalidOperationException("Failed to record allocated texture");
		}
		return texture2D;
	}

	private void Texture2DOnDisposing(object? sender, EventArgs args)
	{
		if (sender is Texture2D platformTexture)
		{
			OnPlatformTextureDisposal(platformTexture);
			return;
		}
		ApplicationContext.CurrentContext.Logger.LogError("Received disposal event but it was not from an instance of {ExpectedType} ({ActualType})", typeof(Texture2D).GetName(qualified: true), sender?.GetType().GetName(qualified: true) ?? "null");
	}

	private IGameTexture CreateGameTextureFromPlatformTexture(string assetName, Texture2D platformTexture, Func<Stream>? sourceFactory = null)
	{
		return new MonoTexture(this, assetName, platformTexture, sourceFactory);
	}

	protected override IGameTexture CreateGameTextureFromAtlasReference(string assetName, AtlasReference atlasReference)
	{
		return new MonoTexture(this, assetName, atlasReference);
	}

	public override IGameTexture CreateTextureFromStreamFactory(string assetName, Func<Stream> streamFactory)
	{
		return new MonoTexture(this, assetName, streamFactory);
	}

	private void OnPlatformTextureDisposal(Texture2D platformTexture)
	{
		if (RemoveAllocatedTexture(platformTexture, out IGameTexture gameTexture))
		{
			MarkFreed(gameTexture);
			return;
		}
		ApplicationContext.CurrentContext.Logger.LogError("Failed to remove platform texture from allocations, is it not tracked? '{TextureName}'", platformTexture.ToString());
	}

	private bool AddAllocatedVertexBuffer(Microsoft.Xna.Framework.Graphics.VertexBuffer platformBuffer, IVertexBuffer buffer)
	{
		if (!_allocatedVertexBuffers.TryAdd(platformBuffer, buffer))
		{
			return false;
		}
		MarkAllocated(buffer);
		_allocatedVertexBuffersSize += MeasureDataSize(platformBuffer);
		return true;
	}

	private bool RemoveAllocatedVertexBuffer(Microsoft.Xna.Framework.Graphics.VertexBuffer platformBuffer, [NotNullWhen(true)] out IVertexBuffer? buffer)
	{
		if (!_allocatedVertexBuffers.TryRemove(platformBuffer, out buffer))
		{
			return false;
		}
		_allocatedVertexBuffersSize -= MeasureDataSize(platformBuffer);
		return true;
	}

	private static long MeasureDataSize(Microsoft.Xna.Framework.Graphics.VertexBuffer platformBuffer)
	{
		return platformBuffer.VertexCount * platformBuffer.VertexDeclaration.VertexStride;
	}

	private static VertexDeclaration CreateVertexDeclaration<TVertex>()
	{
		Type typeFromHandle = typeof(TVertex);
		if (CachedVertexDeclarations.TryGetValue(typeFromHandle, out VertexDeclaration value))
		{
			return value;
		}
		if (typeof(TVertex).Extends<IVertexType>())
		{
			TVertex val = Activator.CreateInstance<TVertex>();
			if (val == null)
			{
				throw new InvalidOperationException("Failed to create an instance of " + typeFromHandle.GetName(qualified: true));
			}
			if (!(val is IVertexType { VertexDeclaration: var vertexDeclaration }))
			{
				throw new InvalidOperationException(typeFromHandle.GetName(qualified: true) + " supposedly extends from " + typeof(IVertexType).GetName(qualified: true) + " but the cast failed?");
			}
			if (!CachedVertexDeclarations.TryAdd(typeFromHandle, vertexDeclaration))
			{
				ApplicationContext.CurrentContext.Logger.LogWarning("Failed to cache vertex declaration for {VertexType}", typeFromHandle.GetName(qualified: true));
			}
			return vertexDeclaration;
		}
		VertexComponentAttribute[] source = typeFromHandle.GetCustomAttributes<VertexComponentAttribute>().ToArray();
		VertexElement[] elements = source.Select(delegate(VertexComponentAttribute componentAttribute)
		{
			VertexElementUsage vertexElementUsageForComponent = GetVertexElementUsageForComponent(componentAttribute.Component);
			VertexElementFormat vertexElementFormatForComponentFormat = GetVertexElementFormatForComponentFormat(componentAttribute.Format);
			return new VertexElement(componentAttribute.Offset, vertexElementFormatForComponentFormat, vertexElementUsageForComponent, componentAttribute.UsageIndex);
		}).ToArray();
		value = new VertexDeclaration(elements);
		if (!CachedVertexDeclarations.TryAdd(typeFromHandle, value))
		{
			ApplicationContext.CurrentContext.Logger.LogWarning("Failed to cache vertex declaration for {VertexType}", typeFromHandle.GetName(qualified: true));
		}
		return value;
	}

	private static VertexElementFormat GetVertexElementFormatForComponentFormat(VertexComponentFormat componentAttributeFormat)
	{
		return componentAttributeFormat switch
		{
			VertexComponentFormat.Float32 => VertexElementFormat.Single, 
			VertexComponentFormat.Vector2f32 => VertexElementFormat.Vector2, 
			VertexComponentFormat.Vector3f32 => VertexElementFormat.Vector3, 
			VertexComponentFormat.Vector4f32 => VertexElementFormat.Vector4, 
			VertexComponentFormat.PackedARGBu32 => VertexElementFormat.Color, 
			VertexComponentFormat.Vector4u8 => VertexElementFormat.Byte4, 
			VertexComponentFormat.Vector2u16 => VertexElementFormat.Short2, 
			VertexComponentFormat.Vector4u16 => VertexElementFormat.Short4, 
			VertexComponentFormat.Normalized2u16 => VertexElementFormat.NormalizedShort2, 
			VertexComponentFormat.Normalized4u16 => VertexElementFormat.NormalizedShort4, 
			VertexComponentFormat.Vector2f16 => VertexElementFormat.HalfVector2, 
			VertexComponentFormat.Vector4f16 => VertexElementFormat.HalfVector4, 
			_ => throw Exceptions.UnreachableInvalidEnum(componentAttributeFormat), 
		};
	}

	private static VertexElementUsage GetVertexElementUsageForComponent(VertexComponent component)
	{
		return component switch
		{
			VertexComponent.Position => VertexElementUsage.Position, 
			VertexComponent.Color => VertexElementUsage.Color, 
			VertexComponent.TextureUV => VertexElementUsage.TextureCoordinate, 
			VertexComponent.Normal => VertexElementUsage.Normal, 
			VertexComponent.Binormal => VertexElementUsage.Binormal, 
			VertexComponent.Tangent => VertexElementUsage.Tangent, 
			VertexComponent.BlendIndices => VertexElementUsage.BlendIndices, 
			VertexComponent.BlendWeight => VertexElementUsage.BlendWeight, 
			VertexComponent.Depth => VertexElementUsage.Depth, 
			VertexComponent.Fog => VertexElementUsage.Fog, 
			VertexComponent.PointSize => VertexElementUsage.PointSize, 
			_ => throw Exceptions.UnreachableInvalidEnum(component), 
		};
	}

	public override void SetActiveVertexBuffer(IVertexBuffer vertexBuffer, int bufferIndex = 0)
	{
		ArgumentOutOfRangeException.ThrowIfNegative(bufferIndex, "bufferIndex");
		if (vertexBuffer is VertexBuffer vertexBuffer2)
		{
			Microsoft.Xna.Framework.Graphics.VertexBuffer platformBuffer = vertexBuffer2.PlatformBuffer;
			if (platformBuffer != null)
			{
				GraphicsDevice.SetVertexBuffer(platformBuffer, bufferIndex);
				return;
			}
		}
		throw new ArgumentException("Expected a " + typeof(VertexBuffer).GetName(qualified: true) + " but received a " + vertexBuffer.GetType().GetName(qualified: true), "vertexBuffer");
	}

	public override IVertexBuffer CreateVertexBuffer<TVertex>(int count, Intersect.Client.Framework.Graphics.BufferUsage usage = Intersect.Client.Framework.Graphics.BufferUsage.None, bool dynamic = false)
	{
		VertexDeclaration vertexDeclaration = CreateVertexDeclaration<TVertex>();
		Microsoft.Xna.Framework.Graphics.BufferUsage bufferUsage = BufferUsageToMonoGameBufferUsage(usage);
		Microsoft.Xna.Framework.Graphics.VertexBuffer vertexBuffer = ((!dynamic) ? new Microsoft.Xna.Framework.Graphics.VertexBuffer(_graphicsDevice, vertexDeclaration, count, bufferUsage) : new DynamicVertexBuffer(_graphicsDevice, vertexDeclaration, count, bufferUsage));
		VertexBuffer vertexBuffer2 = new VertexBuffer(vertexBuffer, typeof(TVertex));
		vertexBuffer.Disposing += VertexBufferOnDisposing;
		if (AddAllocatedVertexBuffer(vertexBuffer, vertexBuffer2))
		{
			return vertexBuffer2;
		}
		vertexBuffer2.Dispose();
		throw new InvalidOperationException("Unable to track vertex buffer");
	}

	private void VertexBufferOnDisposing(object? sender, EventArgs args)
	{
		if (sender is Microsoft.Xna.Framework.Graphics.VertexBuffer platformBuffer)
		{
			OnPlatformVertexBufferDisposal(platformBuffer);
			return;
		}
		ApplicationContext.CurrentContext.Logger.LogError("Received disposal event but it was not from an instance of {ExpectedType} ({ActualType})", typeof(Microsoft.Xna.Framework.Graphics.VertexBuffer).GetName(qualified: true), sender?.GetType().GetName(qualified: true) ?? "null");
	}

	private void OnPlatformVertexBufferDisposal(Microsoft.Xna.Framework.Graphics.VertexBuffer platformBuffer)
	{
		if (RemoveAllocatedVertexBuffer(platformBuffer, out IVertexBuffer buffer))
		{
			MarkFreed(buffer);
			return;
		}
		ApplicationContext.CurrentContext.Logger.LogError("Failed to remove platform buffer from allocations, is it not tracked? '{BufferId}'", platformBuffer.ToString());
	}

	public override void DrawBuffer(IVertexBuffer vertexBuffer, IIndexBuffer? indexBuffer = null)
	{
		ArgumentNullException.ThrowIfNull(vertexBuffer, "vertexBuffer");
		Effect platformShader;
		Microsoft.Xna.Framework.Graphics.VertexBuffer platformBuffer;
		Microsoft.Xna.Framework.Graphics.PrimitiveType platformPrimitiveType;
		Microsoft.Xna.Framework.Graphics.IndexBuffer indexBuffer3;
		int primitiveCount;
		if (base.ActiveShader is Shader shader)
		{
			platformShader = shader.PlatformShader;
			if (platformShader != null)
			{
				EffectTechnique currentTechnique = platformShader.CurrentTechnique;
				if (currentTechnique != null)
				{
					EffectPassCollection passes = currentTechnique.Passes;
					if (passes != null && passes.Count > 0)
					{
						if (vertexBuffer is VertexBuffer vertexBuffer2)
						{
							platformBuffer = vertexBuffer2.PlatformBuffer;
							if (platformBuffer != null)
							{
								Intersect.Client.Framework.Graphics.PrimitiveType primitiveType = vertexBuffer2.PrimitiveType;
								platformPrimitiveType = vertexBuffer2.PlatformPrimitiveType;
								if (indexBuffer is IndexBuffer indexBuffer2)
								{
									indexBuffer3 = indexBuffer2.PlatformBuffer;
									if (indexBuffer3 != null)
									{
										primitiveCount = primitiveType switch
										{
											Intersect.Client.Framework.Graphics.PrimitiveType.LineList => indexBuffer3.IndexCount / 2, 
											Intersect.Client.Framework.Graphics.PrimitiveType.TriangleList => indexBuffer3.IndexCount / 3, 
											Intersect.Client.Framework.Graphics.PrimitiveType.TriangleStrip => indexBuffer3.IndexCount - 2, 
											_ => throw Exceptions.UnreachableInvalidEnum(platformPrimitiveType), 
										};
										goto IL_016a;
									}
								}
								if (indexBuffer != null)
								{
									throw new ArgumentException("Invalid index buffer type " + indexBuffer.GetType().GetName(qualified: true), "vertexBuffer");
								}
								indexBuffer3 = null;
								primitiveCount = primitiveType switch
								{
									Intersect.Client.Framework.Graphics.PrimitiveType.LineList => platformBuffer.VertexCount / 2, 
									Intersect.Client.Framework.Graphics.PrimitiveType.TriangleList => platformBuffer.VertexCount / 3, 
									Intersect.Client.Framework.Graphics.PrimitiveType.TriangleStrip => platformBuffer.VertexCount - 2, 
									_ => throw Exceptions.UnreachableInvalidEnum(platformPrimitiveType), 
								};
								goto IL_016a;
							}
						}
						throw new ArgumentException("Invalid vertex buffer type " + vertexBuffer.GetType().GetName(qualified: true), "vertexBuffer");
					}
				}
			}
		}
		throw new InvalidOperationException("Rendering buffers requires active shader with at least 1 pass");
		IL_016a:
		GraphicsDevice graphicsDevice = GraphicsDevice;
		graphicsDevice.Indices = indexBuffer3;
		graphicsDevice.SetVertexBuffer(platformBuffer, 0);
		foreach (EffectPass pass in platformShader.CurrentTechnique.Passes)
		{
			pass.Apply();
			if (indexBuffer3 == null)
			{
				graphicsDevice.DrawPrimitives(platformPrimitiveType, 0, primitiveCount);
			}
			else
			{
				graphicsDevice.DrawIndexedPrimitives(platformPrimitiveType, 0, 0, primitiveCount);
			}
		}
		graphicsDevice.SetVertexBuffer(null, 0);
		graphicsDevice.Indices = null;
	}

	internal bool CommencerMondeLisse(int largeur, int hauteur)
	{
		GraphicsDevice graphicsDevice = _graphicsDevice;
		if (graphicsDevice == null || _spriteBatch == null || largeur < 1 || hauteur < 1)
		{
			return false;
		}
		EndSpriteBatch();
		if (_cibleMonde == null || _cibleMonde.IsDisposed || _cibleMonde.Width != largeur || _cibleMonde.Height != hauteur)
		{
			_cibleMonde?.Dispose();
			_cibleMonde = new RenderTarget2D(graphicsDevice, largeur, hauteur, mipMap: false, graphicsDevice.PresentationParameters.BackBufferFormat, graphicsDevice.PresentationParameters.DepthStencilFormat, 0, RenderTargetUsage.PreserveContents);
		}
		_largeurMonde = largeur;
		_hauteurMonde = hauteur;
		_mondeLisse = true;
		graphicsDevice.SetRenderTarget(_cibleMonde);
		graphicsDevice.Clear(Microsoft.Xna.Framework.Color.Black);
		return true;
	}

	internal void FinirMondeLisse(float reduction, float decalageX, float decalageY)
	{
		if (!_mondeLisse)
		{
			return;
		}
		EndSpriteBatch();
		_mondeLisse = false;
		if (_cibleMonde != null)
		{
			GraphicsDevice graphicsDevice = _graphicsDevice;
			if (graphicsDevice != null && _spriteBatch != null)
			{
				graphicsDevice.SetRenderTarget(CibleDeLEcran);
				_spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Opaque, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone);
				_spriteBatch.Draw(_cibleMonde, new Microsoft.Xna.Framework.Vector2(decalageX, decalageY), null, Microsoft.Xna.Framework.Color.White, 0f, Microsoft.Xna.Framework.Vector2.Zero, reduction, SpriteEffects.None, 0f);
				_spriteBatch.End();
			}
		}
	}
}
