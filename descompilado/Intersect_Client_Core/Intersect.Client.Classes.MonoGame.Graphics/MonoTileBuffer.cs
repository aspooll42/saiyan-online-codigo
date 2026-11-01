using System;
using System.Collections.Generic;
using Intersect.Client.Core;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.MonoGame.Graphics;
using Intersect.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Intersect.Client.Classes.MonoGame.Graphics;

internal class MonoTileBuffer : GameTileBuffer
{
	private readonly MonoRenderer _003Crenderer_003EP;

	public record struct AddedTile(IGameTexture Texture, int SrcX, int SrcY, int SrcW, int SrcH);

	public readonly Dictionary<AddedTile, int> _addedTileCount;

	private readonly List<short> _indices;

	private readonly Dictionary<int, int> _tileVertexOffset;

	private readonly List<VertexPositionTexture> _vertices;

	private bool _dirty;

	private bool _disposed;

	private Texture2D? _platformTexture;

	private int _vertexCount;

	private IIndexBuffer? _indexBuffer;

	private IVertexBuffer? _vertexBuffer;

	private IGameTexture? _texture;

	private readonly List<(IGameTexture Planche, int X, int Y, int SrcX, int SrcY, int SrcW, int SrcH)> _tuiles;

	private readonly Dictionary<int, int> _rangDesTuiles;

	public override IIndexBuffer IndexBuffer => _indexBuffer ?? throw new InvalidOperationException();

	public override IVertexBuffer VertexBuffer => _vertexBuffer ?? throw new InvalidOperationException();

	public override IGameTexture? Texture => _texture;

	public override bool Supported => true;

	public MonoTileBuffer(MonoRenderer renderer, GraphicsDevice device)
	{
		_003Crenderer_003EP = renderer;
		_addedTileCount = new Dictionary<AddedTile, int>();
		_indices = new List<short>();
		_tileVertexOffset = new Dictionary<int, int>();
		_vertices = new List<VertexPositionTexture>();
		_tuiles = new List<(IGameTexture, int, int, int, int, int, int)>();
		_rangDesTuiles = new Dictionary<int, int>();
	}

	public void DessinerTuileParTuile()
	{
		foreach (var (tex, num, num2, num3, num4, num5, num6) in _tuiles)
		{
			Intersect.Client.Core.Graphics.DrawGameTexture(tex, num, num2, num3, num4, num5, num6);
		}
	}

	public override bool TryAddTile(IGameTexture texture, int x, int y, int srcX, int srcY, int srcW, int srcH)
	{
		if (_vertexBuffer != null)
		{
			ApplicationContext.Context.Value?.Logger.LogError("Unable to add tile to null vertex buffer");
			return false;
		}
		Texture2D texture2D = ((texture == _texture) ? _platformTexture : null) ?? texture.GetTexture<Texture2D>();
		if (texture2D == null)
		{
			ApplicationContext.Context.Value?.Logger.LogError("Unable to add tile to vertex buffer because the platform texture is null");
			return false;
		}
		if (_texture == null)
		{
			_texture = texture;
			_platformTexture = texture2D;
		}
		else if (_platformTexture != texture2D)
		{
			ApplicationContext.Context.Value?.Logger.LogError("Mismatched platform texture");
			return false;
		}
		(IGameTexture, int, int, int, int, int, int) item = (texture, x, y, srcX, srcY, srcW, srcH);
		bool flag = false;
		AtlasReference atlasReference = texture.AtlasReference;
		if (atlasReference != null)
		{
			Intersect.Client.Framework.GenericClasses.Rectangle bounds = atlasReference.Bounds;
			if (atlasReference.IsRotated)
			{
				flag = true;
				int num = bounds.Right - srcY - srcH;
				srcY = bounds.Top + srcX;
				srcX = num;
				int num2 = srcH;
				srcH = srcW;
				srcW = num2;
			}
			else
			{
				srcX += bounds.X;
				srcY += bounds.Y;
			}
		}
		float num3 = 1f / (float)texture2D.Width;
		float num4 = 1f / (float)texture2D.Height;
		float x2 = (float)srcX * num3;
		float x3 = (float)(srcX + srcW) * num3;
		float y2 = (float)(srcY + srcH) * num4;
		float y3 = (float)srcY * num4;
		_tileVertexOffset.Add((x << 16) | y, _vertexCount);
		_rangDesTuiles[(x << 16) | y] = _tuiles.Count;
		_tuiles.Add(item);
		if (flag)
		{
			_vertices.Add(new VertexPositionTexture(new Vector3(x, y + srcH, 0f), new Vector2(x2, y3)));
			_vertices.Add(new VertexPositionTexture(new Vector3(x, y, 0f), new Vector2(x3, y3)));
			_vertices.Add(new VertexPositionTexture(new Vector3(x + srcW, y + srcH, 0f), new Vector2(x2, y2)));
			_vertices.Add(new VertexPositionTexture(new Vector3(x + srcW, y, 0f), new Vector2(x3, y2)));
		}
		else
		{
			_vertices.Add(new VertexPositionTexture(new Vector3(x, y + srcH, 0f), new Vector2(x2, y2)));
			_vertices.Add(new VertexPositionTexture(new Vector3(x, y, 0f), new Vector2(x2, y3)));
			_vertices.Add(new VertexPositionTexture(new Vector3(x + srcW, y + srcH, 0f), new Vector2(x3, y2)));
			_vertices.Add(new VertexPositionTexture(new Vector3(x + srcW, y, 0f), new Vector2(x3, y3)));
		}
		_indices.Add((short)_vertexCount);
		_indices.Add((short)(_vertexCount + 1));
		_indices.Add((short)(_vertexCount + 2));
		_indices.Add((short)(_vertexCount + 2));
		_indices.Add((short)(_vertexCount + 1));
		_indices.Add((short)(_vertexCount + 3));
		_vertexCount += 4;
		_dirty = true;
		return true;
	}

	public override bool TryUpdateTile(IGameTexture texture, int x, int y, int srcX, int srcY, int srcW, int srcH)
	{
		if (_vertexBuffer == null)
		{
			ApplicationContext.Context.Value?.Logger.LogError("Unable to update tile on null vertex buffer");
			return false;
		}
		if (!_tileVertexOffset.TryGetValue((x << 16) | y, out var value))
		{
			ApplicationContext.Context.Value?.Logger.LogError("Unable to update tile that has not been added to the vertex buffer before");
			return false;
		}
		Texture2D texture2D = ((texture == _texture) ? _platformTexture : null) ?? texture.GetTexture<Texture2D>();
		if (texture2D == null)
		{
			ApplicationContext.Context.Value?.Logger.LogError("Unable to add tile to vertex buffer because the platform texture is null");
			return false;
		}
		if (_texture == null)
		{
			_texture = texture;
			_platformTexture = texture2D;
		}
		else if (_platformTexture != texture2D)
		{
			ApplicationContext.Context.Value?.Logger.LogError("Mismatched platform texture");
			return false;
		}
		if (_rangDesTuiles.TryGetValue((x << 16) | y, out var value2))
		{
			_tuiles[value2] = (texture, x, y, srcX, srcY, srcW, srcH);
		}
		bool flag = false;
		AtlasReference atlasReference = texture.AtlasReference;
		if (atlasReference != null)
		{
			if (atlasReference.IsRotated)
			{
				flag = true;
				int num = srcX;
				srcX = atlasReference.Bounds.Right - srcY - srcH;
				srcY = atlasReference.Bounds.Top + num;
				num = srcW;
				srcW = srcH;
				srcH = num;
			}
			else
			{
				srcX += atlasReference.Bounds.X;
				srcY += atlasReference.Bounds.Y;
			}
		}
		float num2 = 1f / (float)texture2D.Width;
		float num3 = 1f / (float)texture2D.Height;
		float x2 = (float)srcX * num2;
		float x3 = (float)(srcX + srcW) * num2;
		float y2 = (float)(srcY + srcH) * num3;
		float y3 = (float)srcY * num3;
		if (flag)
		{
			_vertices[value++] = new VertexPositionTexture(new Vector3(x, y + srcH, 0f), new Vector2(x2, y3));
			_vertices[value++] = new VertexPositionTexture(new Vector3(x, y, 0f), new Vector2(x3, y3));
			_vertices[value++] = new VertexPositionTexture(new Vector3(x + srcW, y + srcH, 0f), new Vector2(x2, y2));
			_vertices[value] = new VertexPositionTexture(new Vector3(x + srcW, y, 0f), new Vector2(x3, y2));
		}
		else
		{
			_vertices[value++] = new VertexPositionTexture(new Vector3(x, y + srcH, 0f), new Vector2(x2, y2));
			_vertices[value++] = new VertexPositionTexture(new Vector3(x, y, 0f), new Vector2(x2, y3));
			_vertices[value++] = new VertexPositionTexture(new Vector3(x + srcW, y + srcH, 0f), new Vector2(x3, y2));
			_vertices[value] = new VertexPositionTexture(new Vector3(x + srcW, y, 0f), new Vector2(x3, y3));
		}
		_dirty = true;
		return true;
	}

	public override bool SetData()
	{
		if (_vertexBuffer != null && !_dirty)
		{
			return false;
		}
		if (_vertices.Count == 0)
		{
			return true;
		}
		GameTileBuffer.TileBufferCount++;
		if (_vertexBuffer == null)
		{
			_vertexBuffer = _003Crenderer_003EP.CreateVertexBuffer<VertexPositionTexture>(_vertices.Count, Intersect.Client.Framework.Graphics.BufferUsage.WriteOnly);
		}
		_vertexBuffer.SetData(_vertices.ToArray());
		if (_indexBuffer == null)
		{
			_indexBuffer = _003Crenderer_003EP.CreateIndexBuffer<short>(_indices.Count, Intersect.Client.Framework.Graphics.BufferUsage.WriteOnly);
		}
		if (_dirty)
		{
			_indexBuffer.SetData(_indices.ToArray());
		}
		_dirty = false;
		return true;
	}

	public override void Dispose()
	{
		_vertexBuffer?.Dispose();
		_vertexBuffer = null;
		_indexBuffer?.Dispose();
		_indexBuffer = null;
		GameTileBuffer.TileBufferCount--;
		_disposed = true;
	}
}
