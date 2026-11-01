using System;
using System.Collections.Generic;
using System.Numerics;
using Intersect.Client.Core;
using Intersect.Client.Framework.Content;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.Framework.Maps;
using Intersect.Client.General;
using Intersect.Client.Maps;
using Intersect.Core;
using Intersect.Enums;
using Intersect.Framework.Core.GameObjects.Events;
using Intersect.Network.Packets.Server;
using Microsoft.Extensions.Logging;

namespace Intersect.Client.Entities.Events;

public class Event : Entity
{
	private bool _drawCompletedWithoutTexture;

	public string Desc { get; set; } = string.Empty;

	public bool DirectionFix { get; set; }

	public bool DisablePreview { get; set; }

	public EventGraphic Graphic { get; set; } = new EventGraphic();

	public int RenderLevel { get; set; } = 1;

	public bool WalkingAnim { get; set; } = true;

	public EventTrigger Trigger { get; set; }

	public byte QuestMarker { get; set; }

	protected override Vector2 CenterOffset
	{
		get
		{
			switch (Graphic.Type)
			{
			case EventGraphicType.None:
				if (!base.HasAnimations)
				{
					return Vector2.Zero;
				}
				return Vector2.UnitY * Options.Instance.Map.TileHeight / 2f;
			case EventGraphicType.Sprite:
				return base.CenterOffset;
			case EventGraphicType.Tileset:
				return Vector2.UnitY * Options.Instance.Map.TileHeight * (Graphic.Height + 1) / 2f;
			default:
				ApplicationContext.Context.Value?.Logger.LogError($"Unimplemented graphic type: {Graphic.Type}");
				return Vector2.Zero;
			}
		}
	}

	public Event(Guid id, EventEntityPacket packet)
		: base(id, packet, EntityType.Event)
	{
		mRenderPriority = 1;
	}

	public override void Load(EntityPacket? packet)
	{
		if (!(packet is EventEntityPacket eventEntityPacket))
		{
			ApplicationContext.Context.Value?.Logger.LogError("Received invalid packet for Event: " + packet?.GetType()?.FullName);
			return;
		}
		DirectionFix = eventEntityPacket.DirectionFix;
		WalkingAnim = eventEntityPacket.WalkingAnim;
		DisablePreview = eventEntityPacket.DisablePreview;
		Desc = eventEntityPacket.Description;
		Graphic = eventEntityPacket.Graphic;
		RenderLevel = eventEntityPacket.RenderLayer;
		Trigger = eventEntityPacket.Trigger;
		QuestMarker = eventEntityPacket.QuestMarker;
		_drawCompletedWithoutTexture = Graphic.Type != EventGraphicType.Tileset;
		base.Load(packet);
		if (!string.IsNullOrEmpty(Graphic?.Filename))
		{
			Sprite = Graphic.Filename;
		}
	}

	public override bool Update()
	{
		bool result = base.Update();
		if (!WalkingAnim)
		{
			base.WalkFrame = 0;
		}
		return result;
	}

	protected bool TryEnsureTexture(out IGameTexture? texture)
	{
		if (_drawCompletedWithoutTexture)
		{
			texture = base.Texture;
			return texture != null;
		}
		LoadTextures(Sprite);
		texture = base.Texture;
		_drawCompletedWithoutTexture = texture == null;
		return !_drawCompletedWithoutTexture;
	}

	public override void Draw()
	{
		base.WorldPos.Reset();
		if (base.MapInstance == null || !Globals.GridMaps.ContainsKey(MapId) || !TryEnsureTexture(out IGameTexture texture))
		{
			return;
		}
		switch (Graphic.Type)
		{
		case EventGraphicType.Sprite:
			base.Draw();
			break;
		case EventGraphicType.Tileset:
		{
			int num = (Graphic.Width + 1) * Options.Instance.Map.TileWidth;
			int num2 = (Graphic.Height + 1) * Options.Instance.Map.TileHeight;
			FloatRect srcRectangle = new FloatRect(Graphic.X * Options.Instance.Map.TileWidth, Graphic.Y * Options.Instance.Map.TileHeight, num, num2);
			MapInstance mapInstance = Intersect.Client.Maps.MapInstance.Get(MapId);
			FloatRect floatRect = new FloatRect
			{
				X = (float)(mapInstance.X + base.X * Options.Instance.Map.TileWidth) + base.OffsetX,
				Y = (float)(mapInstance.Y + base.Y * Options.Instance.Map.TileHeight) + base.OffsetY,
				Width = Math.Max(Options.Instance.Map.TileWidth, srcRectangle.Width),
				Height = Math.Max(Options.Instance.Map.TileHeight, srcRectangle.Height)
			};
			if (srcRectangle.Width > (float)Options.Instance.Map.TileWidth)
			{
				floatRect.X -= (srcRectangle.Width - (float)Options.Instance.Map.TileWidth) / 2f;
			}
			if (srcRectangle.Height > (float)Options.Instance.Map.TileHeight)
			{
				floatRect.Y -= srcRectangle.Height - (float)Options.Instance.Map.TileHeight;
			}
			floatRect.X = (int)Math.Ceiling(floatRect.X);
			floatRect.Y = (int)Math.Ceiling(floatRect.Y);
			base.WorldPos = floatRect;
			if (texture != null)
			{
				Graphics.DrawGameTexture(texture, srcRectangle, floatRect, Intersect.Color.White);
			}
			break;
		}
		}
	}

	public override void LoadTextures(string textureName)
	{
		switch (Graphic.Type)
		{
		case EventGraphicType.Tileset:
			base.Texture = Globals.ContentManager.GetTexture(TextureType.Tileset, textureName);
			break;
		case EventGraphicType.Sprite:
			base.LoadTextures(textureName);
			break;
		}
	}

	public override HashSet<Entity>? DetermineRenderOrder(HashSet<Entity>? renderList, IMapInstance? map)
	{
		if (RenderLevel == 1)
		{
			return base.DetermineRenderOrder(renderList, map);
		}
		renderList?.Remove(this);
		if (map == null || Globals.Me == null || Globals.Me.MapInstance == null || Globals.MapGrid == null)
		{
			return null;
		}
		int gridX = Globals.Me.MapInstance.GridX;
		int gridY = Globals.Me.MapInstance.GridY;
		for (int i = gridX - 1; i <= gridX + 1; i++)
		{
			for (int j = gridY - 1; j <= gridY + 1; j++)
			{
				if (i >= 0 && i < Globals.MapGridWidth && j >= 0 && j < Globals.MapGridHeight && Globals.MapGrid[i, j] != Guid.Empty && !(Globals.MapGrid[i, j] != MapId))
				{
					if (RenderLevel == 0)
					{
						j--;
					}
					else if (RenderLevel == 2)
					{
						j++;
					}
					byte b = mRenderPriority;
					if (base.Z != 0)
					{
						b += 3;
					}
					int num = j - (gridY - 2);
					HashSet<Entity> hashSet = Graphics.RenderingEntities[b, Options.Instance.Map.MapHeight * num + base.Y];
					hashSet?.Add(this);
					if (hashSet != null)
					{
						renderList = hashSet;
					}
					return renderList;
				}
			}
		}
		return renderList;
	}

	public override float GetTop(int overrideHeight = -1)
	{
		float num;
		switch (Graphic.Type)
		{
		case EventGraphicType.Sprite:
			return base.GetTop(overrideHeight);
		case EventGraphicType.Tileset:
			num = Graphic.Height + 1;
			break;
		case EventGraphicType.None:
			num = (base.HasAnimations ? 1f : 0.5f);
			break;
		default:
			throw new IndexOutOfRangeException($"Unsupported {"EventGraphicType"} '{Graphic.Type}'");
		}
		float top = base.GetTop(0);
		float num2 = num * (float)Options.Instance.Map.TileHeight;
		return top - num2;
	}

	public override void DrawName(Color? textColor, Color? borderColor, Color? backgroundColor)
	{
		base.DrawName(textColor ?? new Color(in CustomColors.Names.Events.Name), borderColor ?? new Color(in CustomColors.Names.Events.Outline), backgroundColor ?? new Color(in CustomColors.Names.Events.Background));
	}

	~Event()
	{
		Dispose();
	}
}
