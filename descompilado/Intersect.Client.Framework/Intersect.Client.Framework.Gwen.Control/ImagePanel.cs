using System;
using System.IO;
using System.Linq;
using Intersect.Client.Framework.Content;
using Intersect.Client.Framework.File_Management;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.Framework.Gwen.Skin;
using Intersect.Client.Framework.Gwen.Skin.Texturing;
using Intersect.Client.Framework.Input;
using Newtonsoft.Json.Linq;

namespace Intersect.Client.Framework.Gwen.Control;

public class ImagePanel : Base
{
	private readonly float[] _uv;

	private string? _textureName;

	private Rectangle _textureSourceBounds;

	private float _textureAspectRatio;

	private Margin? _textureNinePatchMargin;

	private Bordered? _ninepatchRenderer;

	private bool _maintainAspectRatio;

	public string? HoverSound { get; set; }

	public string? LeftMouseClickSound { get; set; }

	public string? RightMouseClickSound { get; set; }

	private IGameTexture? _texture { get; set; }

	public bool MaintainAspectRatio
	{
		get
		{
			return _maintainAspectRatio;
		}
		set
		{
			SetAndDoIfChanged(ref _maintainAspectRatio, value, Base.Invalidate);
		}
	}

	public Margin? TextureNinePatchMargin
	{
		get
		{
			return _textureNinePatchMargin;
		}
		set
		{
			if (!(value == _textureNinePatchMargin))
			{
				_textureNinePatchMargin = value;
				_ninepatchRenderer = null;
			}
		}
	}

	public IGameTexture? Texture
	{
		get
		{
			return _texture;
		}
		set
		{
			if (value != _texture)
			{
				_texture = value;
				_textureName = Path.GetFileName(_texture?.Name);
				RecomputeTextureSourceBounds();
				if (_texture != null)
				{
					this.TextureLoaded?.Invoke(this, EventArgs.Empty);
				}
				_ninepatchRenderer = null;
				InvalidateParent();
			}
		}
	}

	public string? TextureFilename
	{
		get
		{
			return _textureName;
		}
		set
		{
			string text = value?.Trim();
			if (!string.Equals(text, _textureName, StringComparison.Ordinal))
			{
				_textureName = text;
				Texture = ((_textureName == null) ? null : GameContentManager.Current?.GetTexture(TextureType.Gui, _textureName));
			}
		}
	}

	public event GwenEventHandler<EventArgs>? TextureLoaded;

	public ImagePanel(Base parent, string? name = null)
		: base(parent, name)
	{
		_uv = new float[4] { 0f, 0f, 1f, 1f };
		base.MouseInputEnabled = true;
	}

	private void RecomputeTextureSourceBounds()
	{
		IGameTexture texture = _texture;
		if (texture == null)
		{
			_textureAspectRatio = 0f;
			_textureSourceBounds = default(Rectangle);
			return;
		}
		float[] array = _uv.ToArray();
		float val = array[0];
		float val2 = array[1];
		float val3 = array[2];
		float val4 = array[3];
		float num = Math.Min(val, val3);
		val3 = Math.Max(val, val3);
		val = num;
		float num2 = Math.Min(val2, val4);
		val4 = Math.Max(val2, val4);
		val2 = num2;
		Rectangle rectangle = (_textureSourceBounds = new Rectangle((int)(val * (float)texture.Width), (int)(val2 * (float)texture.Height), (int)((val3 - val) * (float)texture.Width), (int)((val4 - val2) * (float)texture.Height)));
		_textureAspectRatio = (float)rectangle.Width / (float)rectangle.Height;
	}

	protected override void OnMouseEntered()
	{
		base.OnMouseEntered();
		PlaySound(HoverSound);
	}

	protected override void OnMouseClicked(MouseButton mouseButton, Point mousePosition, bool userAction = true)
	{
		base.OnMouseClicked(mouseButton, mousePosition, userAction);
		PlaySound(mouseButton switch
		{
			MouseButton.Left => LeftMouseClickSound, 
			MouseButton.Right => RightMouseClickSound, 
			_ => null, 
		});
	}

	protected override void OnPositionChanged(Point oldPosition, Point newPosition)
	{
		base.OnPositionChanged(oldPosition, newPosition);
	}

	protected override void Dispose(bool disposing)
	{
		base.Dispose(disposing);
	}

	public override JObject? GetJson(bool isRoot = false, bool onlySerializeIfNotEmpty = false)
	{
		JObject json = base.GetJson(isRoot, onlySerializeIfNotEmpty);
		if (json == null)
		{
			return null;
		}
		json.Add("Texture", TextureFilename);
		json.Add("TextureNinePatchMargin", TextureNinePatchMargin?.ToString());
		json.Add("HoverSound", HoverSound);
		json.Add("LeftMouseClickSound", LeftMouseClickSound);
		json.Add("RightMouseClickSound", RightMouseClickSound);
		return base.FixJson(json);
	}

	public override void LoadJson(JToken token, bool isRoot = false)
	{
		base.LoadJson(token);
		if (!(token is JObject jObject))
		{
			return;
		}
		if (jObject["Texture"] != null)
		{
			Texture = GameContentManager.Current.GetTexture(TextureType.Gui, (string?)jObject["Texture"]);
			TextureFilename = (string?)jObject["Texture"];
		}
		if (jObject.TryGetValue("TextureNinePatchMargin", out JToken value))
		{
			if (value is JValue { Type: JTokenType.String } jValue)
			{
				string text = jValue.Value<string>();
				if (text != null && !string.IsNullOrWhiteSpace(text))
				{
					TextureNinePatchMargin = Margin.FromString(text);
				}
				else
				{
					TextureNinePatchMargin = null;
				}
			}
			else
			{
				TextureNinePatchMargin = null;
			}
		}
		if (jObject["HoverSound"] is JValue { Type: JTokenType.String } jValue2)
		{
			string text2 = jValue2.Value<string>()?.Trim();
			if (text2 != null && text2.Length > 0)
			{
				HoverSound = text2;
			}
		}
		if (jObject["LeftMouseClickSound"] is JValue { Type: JTokenType.String } jValue3)
		{
			string text3 = jValue3.Value<string>()?.Trim();
			if (text3 != null && text3.Length > 0)
			{
				LeftMouseClickSound = text3;
			}
		}
		if (jObject["RightMouseClickSound"] is JValue { Type: JTokenType.String } jValue4)
		{
			string text4 = jValue4.Value<string>()?.Trim();
			if (text4 != null && text4.Length > 0)
			{
				RightMouseClickSound = text4;
			}
		}
	}

	public void ResetUVs()
	{
		SetUVs(0f, 0f, 1f, 1f);
	}

	public virtual void SetUVs(float u1, float v1, float u2, float v2)
	{
		_uv[0] = u1;
		_uv[1] = v1;
		_uv[2] = u2;
		_uv[3] = v2;
		RecomputeTextureSourceBounds();
		_ninepatchRenderer = null;
	}

	public virtual void SetTextureRect(int x, int y, int w, int h)
	{
		if (_texture != null)
		{
			if (x < 0)
			{
				x = 0;
			}
			if (y < 0)
			{
				y = 0;
			}
			if (w <= 0)
			{
				w = _texture.Width;
			}
			if (h <= 0)
			{
				h = _texture.Height;
			}
			if (x + w <= _texture.Width && y + h <= _texture.Height)
			{
				SetUVs((float)x / (float)_texture.Width, (float)y / (float)_texture.Height, (float)(x + w) / (float)_texture.Width, (float)(y + h) / (float)_texture.Height);
			}
		}
	}

	protected override void Layout(Intersect.Client.Framework.Gwen.Skin.Base skin)
	{
		base.Layout(skin);
		EnsureTextureLoaded();
	}

	protected override void DoPrelayout(Intersect.Client.Framework.Gwen.Skin.Base skin)
	{
		base.DoPrelayout(skin);
		EnsureTextureLoaded();
	}

	private void EnsureTextureLoaded()
	{
		if (_texture == null)
		{
			string textureName = _textureName;
			if (!string.IsNullOrWhiteSpace(textureName))
			{
				Texture = GameContentManager.Current?.GetTexture(TextureType.Gui, textureName);
			}
		}
	}

	protected override void Render(Intersect.Client.Framework.Gwen.Skin.Base skin)
	{
		base.Render(skin);
		IGameTexture texture = _texture;
		if (texture == null)
		{
			return;
		}
		skin.Renderer.DrawColor = base.RenderColor;
		Rectangle renderBounds = base.RenderBounds;
		Margin? textureNinePatchMargin = TextureNinePatchMargin;
		if (textureNinePatchMargin.HasValue)
		{
			Margin valueOrDefault = textureNinePatchMargin.GetValueOrDefault();
			Rectangle textureSourceBounds = _textureSourceBounds;
			if (!_ninepatchRenderer.HasValue || _ninepatchRenderer.Value.Margin != valueOrDefault)
			{
				_ninepatchRenderer = new Bordered(texture, textureSourceBounds.X, textureSourceBounds.Y, textureSourceBounds.Width, textureSourceBounds.Height, valueOrDefault);
			}
			_ninepatchRenderer?.Draw(skin.Renderer, renderBounds, base.RenderColor);
			return;
		}
		Rectangle textureSourceBounds2 = _textureSourceBounds;
		if (textureSourceBounds2.Width <= 0 || textureSourceBounds2.Height <= 0)
		{
			return;
		}
		if (_maintainAspectRatio)
		{
			float num = (float)renderBounds.Width / (float)textureSourceBounds2.Width;
			float num2 = (float)renderBounds.Height / (float)textureSourceBounds2.Height;
			if (num > num2)
			{
				renderBounds.Width = (int)((float)textureSourceBounds2.Width * num2);
			}
			else if (num2 > num)
			{
				renderBounds.Height = (int)((float)textureSourceBounds2.Height * num);
			}
		}
		skin.Renderer.DrawTexturedRect(texture, renderBounds, base.RenderColor, _uv[0], _uv[1], _uv[2], _uv[3]);
	}

	public virtual void SizeToContents()
	{
		SizeToChildren();
	}

	public override bool SetBounds(int x, int y, int width, int height)
	{
		int num = x;
		int num2 = width;
		int num3 = height;
		if (MaintainAspectRatio)
		{
			Rectangle bounds = base.Bounds;
			float textureAspectRatio = _textureAspectRatio;
			if (num2 == bounds.Width)
			{
				if (num3 != bounds.Height)
				{
					int num4 = (int)((float)num3 * textureAspectRatio);
					if (num4 > num2)
					{
						int num5 = num2 - num4;
						num += num5 / 2;
						num2 = num4;
						if (base.RestrictToParent)
						{
							num = Math.Max(0, num);
						}
					}
				}
			}
			else if (num3 == bounds.Height)
			{
				int num6 = (int)((float)num2 / textureAspectRatio);
				if (num6 > num3)
				{
					num3 = num6;
				}
			}
		}
		return base.SetBounds(num, y, num2, num3);
	}

	public override Point GetChildrenSize()
	{
		Point point = _textureSourceBounds.Size;
		if (TextureNinePatchMargin.HasValue)
		{
			point = default(Point);
		}
		Point childrenSize = base.GetChildrenSize();
		return new Point(Math.Max(childrenSize.X, point.X), Math.Max(childrenSize.Y, point.Y));
	}

	public override void Touch()
	{
		base.Touch();
	}

	protected override bool OnKeySpace(bool down)
	{
		if (down)
		{
			base.OnMouseDown(MouseButton.Left, default(Point));
		}
		return true;
	}
}
