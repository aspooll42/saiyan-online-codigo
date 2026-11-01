using System;
using System.Collections.Generic;
using Intersect.Client.Entities;
using Intersect.Client.Framework.Content;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.Framework.Gwen.Renderer;
using Intersect.Client.Framework.Gwen.Skin;
using Intersect.Client.General;
using Intersect.Enums;
using Intersect.Framework.Core.GameObjects.Items;
using Intersect.Models;

namespace Intersect.Client.Interface.Game.EntityPanel;

public sealed class HexPortrait : Intersect.Client.Framework.Gwen.Control.Base
{
	private readonly record struct Layer(IGameTexture Texture, Rectangle Frame, Color Tint, bool IsFace, IGameTexture? Anchor = null, Rectangle AnchorFrame = default(Rectangle));

	private readonly List<Layer> _layers = new List<Layer>();

	private readonly int _radius;

	private readonly int _centerX;

	private readonly int _centerY;

	private string? _key;

	public Entity? Entity { get; set; }

	public string? Sprite { get; set; }

	public string? Face { get; set; }

	public Color SpriteTint { get; set; } = new Color(255, 255, 255, 255);

	public IGameTexture? Icon { get; set; }

	public int IconSize { get; set; } = 64;

	public Color Accent { get; set; } = new Color(255, 255, 122, 26);

	protected override bool ShouldClip => false;

	public HexPortrait(Intersect.Client.Framework.Gwen.Control.Base parent, string name, int radius)
		: base(parent, name)
	{
		_radius = ((radius >= 30) ? 35 : 18);
		base.MouseInputEnabled = false;
		if (_radius == 35)
		{
			SetSize(84, 76);
			_centerX = 42;
			_centerY = 38;
		}
		else
		{
			SetSize(40, 36);
			_centerX = 20;
			_centerY = 18;
		}
	}

	protected override void Render(Intersect.Client.Framework.Gwen.Skin.Base skin)
	{
		base.Render(skin);
		Intersect.Client.Framework.Gwen.Renderer.Base renderer = skin.Renderer;
		Refresh();
		bool flag = _radius == 35;
		(int, int) tuple = (flag ? (_centerX - 42) : (_centerX - 20), flag ? (_centerY - 38) : (_centerY - 18));
		SaiyanHud.Draw(renderer, SaiyanHud.Texture(flag ? "hud_joueur_portrait_fond.png" : "saiyan_hex18_fond.png"), tuple.Item1, tuple.Item2, SaiyanHud.White);
		if (_layers.Count == 0)
		{
			if (Icon != null)
			{
				SaiyanHud.Draw(renderer, Icon, _centerX - IconSize / 2, _centerY - IconSize / 2, IconSize, IconSize, SaiyanHud.White);
			}
			else if (flag)
			{
				SaiyanHud.Draw(renderer, SaiyanHud.Texture("saiyan_portrait_silhouette.png"), tuple.Item1, tuple.Item2, SaiyanHud.White);
			}
		}
		else
		{
			if (flag)
			{
				SaiyanHud.Draw(renderer, SaiyanHud.Texture("hud_joueur_portrait_lueur.png"), tuple.Item1, tuple.Item2, Accent);
			}
			foreach (Layer layer in _layers)
			{
				DrawLayer(renderer, layer);
			}
			if (flag)
			{
				SaiyanHud.Draw(renderer, SaiyanHud.Texture("hud_joueur_portrait_verre.png"), tuple.Item1, tuple.Item2, SaiyanHud.White);
			}
		}
		SaiyanHud.Draw(renderer, SaiyanHud.Texture(flag ? "hud_joueur_portrait_cadre.png" : "saiyan_hex18_cadre.png"), tuple.Item1, tuple.Item2, Accent);
	}

	private void Refresh()
	{
		Entity entity = Entity;
		if (entity == null)
		{
			RefreshFromSprite();
			return;
		}
		Guid[] array = EquipmentOf(entity);
		string text = $"{entity.Id}|{entity.Face}|{entity.Sprite}|{entity.TransformedSprite}|{entity.Texture?.Name}|{string.Join(",", array)}";
		if (text == _key && _layers.Count > 0)
		{
			return;
		}
		_key = text;
		_layers.Clear();
		IGameTexture gameTexture = (string.IsNullOrWhiteSpace(entity.Face) ? null : Globals.ContentManager.GetTexture(TextureType.Face, entity.Face));
		Color color = entity.Color;
		if (gameTexture != null)
		{
			_layers.Add(new Layer(gameTexture, new Rectangle(0, 0, gameTexture.Width, gameTexture.Height), color, IsFace: true));
			return;
		}
		if (entity.Texture != null)
		{
			SpriteSheets.Pose? pose = SpriteSheets.StandingBody(entity.Sprite, entity.Texture.Name);
			if (pose.HasValue)
			{
				SpriteSheets.Pose valueOrDefault = pose.GetValueOrDefault();
				if (!valueOrDefault.IsEmpty)
				{
					IGameTexture texture = valueOrDefault.Texture;
					Rectangle rectangle = new Rectangle(valueOrDefault.Frame.X, valueOrDefault.Frame.Y, valueOrDefault.Frame.Width, valueOrDefault.Frame.Height);
					_layers.Add(new Layer(texture, rectangle, color, IsFace: false));
					List<string> list = Options.Instance.Equipment.Paperdoll.Directions[1];
					List<string> slots = Options.Instance.Equipment.Slots;
					for (int i = 0; i < list.Count; i++)
					{
						int num = slots.IndexOf(list[i]);
						if (num < 0 || num >= array.Length || array[num] == Guid.Empty || !DatabaseObject<ItemDescriptor>.TryGet(array[num], out var databaseObject))
						{
							continue;
						}
						string paperdoll = Paperdolls.ForClass((entity.Gender == Gender.Male) ? databaseObject.MalePaperdoll : databaseObject.FemalePaperdoll, (entity is Player player) ? player.Class : Guid.Empty);
						paperdoll = Paperdolls.ForForm(entity, paperdoll);
						if (string.IsNullOrWhiteSpace(paperdoll))
						{
							continue;
						}
						pose = SpriteSheets.StandingOutfit(entity.Sprite, paperdoll, valueOrDefault);
						if (pose.HasValue)
						{
							SpriteSheets.Pose valueOrDefault2 = pose.GetValueOrDefault();
							if (!valueOrDefault2.IsEmpty)
							{
								(int, int, int, int) frame = valueOrDefault2.Frame;
								_layers.Add(new Layer(valueOrDefault2.Texture, new Rectangle(frame.Item1, frame.Item2, frame.Item3, frame.Item4), databaseObject.Color, IsFace: false, texture, rectangle));
							}
						}
					}
					return;
				}
			}
		}
		_key = null;
	}

	private void RefreshFromSprite()
	{
		if (string.IsNullOrWhiteSpace(Sprite) && string.IsNullOrWhiteSpace(Face))
		{
			_layers.Clear();
			_key = null;
			return;
		}
		string text = $"sprite|{Face}|{Sprite}|{SpriteTint.A},{SpriteTint.R},{SpriteTint.G},{SpriteTint.B}";
		if (text == _key && _layers.Count > 0)
		{
			return;
		}
		_key = text;
		_layers.Clear();
		IGameTexture gameTexture = (string.IsNullOrWhiteSpace(Face) ? null : Globals.ContentManager.GetTexture(TextureType.Face, Face));
		if (gameTexture != null)
		{
			_layers.Add(new Layer(gameTexture, new Rectangle(0, 0, gameTexture.Width, gameTexture.Height), SpriteTint, IsFace: true));
			return;
		}
		SpriteSheets.Pose? pose = SpriteSheets.StandingBody(Sprite);
		if (pose.HasValue)
		{
			SpriteSheets.Pose valueOrDefault = pose.GetValueOrDefault();
			if (!valueOrDefault.IsEmpty)
			{
				(int, int, int, int) frame = valueOrDefault.Frame;
				_layers.Add(new Layer(valueOrDefault.Texture, new Rectangle(frame.Item1, frame.Item2, frame.Item3, frame.Item4), SpriteTint, IsFace: false));
			}
		}
		else
		{
			_key = null;
		}
	}

	private static Guid[] EquipmentOf(Entity entity)
	{
		Guid[] array = entity.Equipment ?? Array.Empty<Guid>();
		if (entity is Player player && player == Globals.Me)
		{
			array = new Guid[player.MyEquipment.Length];
			for (int i = 0; i < player.MyEquipment.Length; i++)
			{
				int num = player.MyEquipment[i];
				array[i] = ((num > -1 && num < Options.Instance.Player.MaxInventory) ? player.Inventory[num].ItemId : Guid.Empty);
			}
		}
		return array;
	}

	private void DrawLayer(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Layer layer)
	{
		if (!(renderer is IntersectRenderer intersectRenderer))
		{
			return;
		}
		int radius = _radius;
		IGameTexture texture = layer.Texture;
		Rectangle frame = layer.Frame;
		int num;
		int num2;
		int num3;
		int num4;
		int num5;
		if (layer.IsFace)
		{
			num = Math.Max(1, (int)MathF.Floor(Math.Max(((float)radius * 2f - 4f) / (float)frame.Width, ((float)radius * 2f - 12f) / (float)frame.Height)));
			num2 = frame.Y;
			num3 = frame.Height;
			num4 = _centerX - frame.Width * num / 2;
			num5 = _centerY - frame.Height * num / 2;
		}
		else
		{
			num = Math.Max(1, (radius * 2 + 4) / frame.Width);
			IGameTexture anchor = layer.Anchor;
			int num6 = ((anchor != null) ? SaiyanHud.TopMargin(anchor, layer.AnchorFrame) : SaiyanHud.TopMargin(texture, frame));
			num2 = frame.Y + Math.Max(0, num6 - 1);
			int val = frame.Y + frame.Height - num2;
			num5 = _centerY - (int)((float)radius * 0.714f);
			num3 = Math.Clamp((_centerY + (int)((float)radius * 0.886f) - num5) / num, 1, Math.Max(1, val));
			num4 = _centerX - frame.Width * num / 2;
		}
		int num7 = frame.Width * num;
		int num8 = num3 * num;
		if (num7 < 1 || num8 < 1)
		{
			return;
		}
		float num9 = ((radius >= 30) ? 1.5f : 1f);
		intersectRenderer.DrawColor = layer.Tint;
		for (int i = 0; i < num8; i++)
		{
			int num10 = num5 + i;
			float num11 = MathF.Abs((float)num10 + 0.5f - (float)_centerY);
			if (num11 > (float)radius * 0.866f - num9)
			{
				continue;
			}
			float num12 = (float)radius - num9 - num11 / 1.732f;
			if (!(num12 <= 0f))
			{
				int num13 = Math.Max(0, (int)MathF.Ceiling(((float)_centerX - num12 - (float)num4) / (float)num));
				int num14 = Math.Min(frame.Width, (int)MathF.Floor(((float)_centerX + num12 - (float)num4) / (float)num));
				if (num14 > num13)
				{
					int num15 = num14 - num13;
					int num16 = num2 + i / num;
					intersectRenderer.DrawTextureBand(texture, (float)(frame.X + num13) + 0.5f, (float)num16 + 0.5f, (float)num15 + 0.49f, 1.49f, num4 + num13 * num, num10, ((float)num15 + 0.49f) * (float)num, 1.49f);
				}
			}
		}
	}
}
