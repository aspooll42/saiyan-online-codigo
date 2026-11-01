using System;
using System.Collections.Generic;
using System.Numerics;
using Intersect.Client.Framework.GenericClasses;

namespace Intersect.Client.Framework.Graphics;

public interface IGameRenderer
{
	Resolution ActiveResolution { get; }

	string ResolutionAsString { get; }

	int ScreenHeight { get; }

	int ScreenWidth { get; }

	Resolution OverrideResolution { get; set; }

	Resolution PreferredResolution { get; set; }

	FloatRect CurrentView { get; set; }

	int FPS { get; }

	List<string> ValidVideoModes { get; }

	string? CursorName { get; set; }

	IGameTexture? Cursor { get; set; }

	ulong TextureCount { get; }

	ulong RenderTargetAllocations { get; }

	ulong TextureAllocations { get; }

	ulong IndexBufferAllocations { get; }

	ulong VertexBufferAllocations { get; }

	GameShader? ActiveShader { get; set; }

	IIndexBuffer? ActiveIndexBuffer { get; set; }

	event EventHandler<TextureEventArgs>? TextureAllocated;

	event EventHandler<TextureEventArgs>? TextureCreated;

	event EventHandler<TextureEventArgs>? TextureDisposed;

	event EventHandler<TextureEventArgs>? TextureFreed;

	event EventHandler<BufferEventArgs>? IndexBufferAllocated;

	event EventHandler<BufferEventArgs>? IndexBufferFreed;

	event EventHandler<BufferEventArgs>? VertexBufferAllocated;

	event EventHandler<BufferEventArgs>? VertexBufferFreed;

	void Clear(Color color);

	void SetActiveVertexBuffer(IVertexBuffer vertexBuffer, int bufferIndex = 0);

	IGameRenderTexture CreateRenderTexture(int width, int height);

	IIndexBuffer CreateIndexBuffer<TIndex>(int count, BufferUsage usage = BufferUsage.None, bool dynamic = false) where TIndex : struct;

	IVertexBuffer CreateVertexBuffer<TVertex>(int count, BufferUsage usage = BufferUsage.None, bool dynamic = false) where TVertex : struct;

	void DrawTexture(IGameTexture texture, float sourceX, float sourceY, float sourceWidth, float sourceHeight, float targetX, float targetY, float targetWidth, float targetHeight, Color renderColor, IGameRenderTexture? renderTarget = null, GameBlendModes blendMode = GameBlendModes.None, GameShader? shader = null, float rotationDegrees = 0f);

	void DrawString(string text, IFont font, int size, float x, float y, float fontScale, Color fontColor, bool worldPos = true, IGameRenderTexture? renderTexture = null, Color? borderColor = null);

	void DrawString(string text, IFont font, int size, float x, float y, float fontScale, Color fontColor, bool worldPos, IGameRenderTexture renderTexture, FloatRect clipRect, Color? borderColor = null);

	Vector2 MeasureText(string? text, IFont? font, int size, float fontScale);

	void RequestScreenshot(string? pathToScreenshots = null);

	void DrawTileBuffer(GameTileBuffer buffer);
}
