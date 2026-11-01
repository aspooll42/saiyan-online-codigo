using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Intersect.Client.Framework.Content;
using Intersect.Client.Framework.File_Management;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.Framework.Gwen.ControlInternal;
using Intersect.Client.Framework.Gwen.Renderer;
using Intersect.Client.Framework.Gwen.Skin.Texturing;
using Intersect.Core;
using Intersect.Framework;
using Intersect.Framework.Reflection;
using Microsoft.Extensions.Logging;

namespace Intersect.Client.Framework.Gwen.Skin;

public class TexturedBase : Base
{
	protected readonly IGameTexture _texture;

	protected SkinTextures mTextures;

	public string TextureName { get; }

	public static TexturedBase FindSkin(Intersect.Client.Framework.Gwen.Renderer.Base renderer, GameContentManager contentManager, string skinName)
	{
		if (string.Equals("Intersect2021", skinName, StringComparison.InvariantCultureIgnoreCase))
		{
			return new Intersect2021(renderer, contentManager);
		}
		string[] source = skinName.Split(':');
		string text = source.FirstOrDefault() ?? "IntersectSkin";
		string text2 = source.Skip(1).FirstOrDefault();
		if (string.Equals("IntersectSkin", text, StringComparison.InvariantCultureIgnoreCase))
		{
			if (!string.IsNullOrWhiteSpace(text2))
			{
				return new IntersectSkin(renderer, contentManager, text2);
			}
			return new IntersectSkin(renderer, contentManager);
		}
		Dictionary<string, Type> dictionary = (from type in typeof(TexturedBase).Assembly.GetTypes()
			where !type.IsAbstract && typeof(TexturedBase).IsAssignableFrom(type)
			select type).ToDictionary((Type type) => type.Name.ToLowerInvariant(), (Type type) => type);
		if (!dictionary.TryGetValue(text.ToLowerInvariant(), out var value))
		{
			return new TexturedBase(renderer, contentManager, skinName);
		}
		try
		{
			if (Activator.CreateInstance(value, renderer, contentManager, text2) is TexturedBase result)
			{
				return result;
			}
			ApplicationContext.Context.Value?.Logger.LogError("Failed to create instance of '{SkinTypeName}'", value.GetName(qualified: true));
		}
		catch
		{
		}
		return new TexturedBase(renderer, contentManager, skinName);
	}

	public TexturedBase(Intersect.Client.Framework.Gwen.Renderer.Base renderer, IGameTexture texture)
		: base(renderer)
	{
		_texture = texture ?? throw new ArgumentNullException("texture");
		texture.Loaded += OnTextureLoaded;
		texture.Reload();
	}

	private void OnTextureLoaded(IAsset _)
	{
		InitializeColors();
		InitializeTextures();
	}

	public TexturedBase(Intersect.Client.Framework.Gwen.Renderer.Base renderer, GameContentManager contentManager)
		: this(renderer, contentManager, "defaultskin.png")
	{
	}

	protected TexturedBase(Intersect.Client.Framework.Gwen.Renderer.Base renderer, GameContentManager contentManager, string textureName)
		: this(renderer, contentManager?.GetTexture(TextureType.Gui, textureName))
	{
	}

	public override void Dispose()
	{
		_texture.Loaded -= OnTextureLoaded;
		base.Dispose();
	}

	protected virtual void InitializeColors()
	{
		Colors.Window.TitleActive = base.Renderer.PixelColor(_texture, 4u, 508u, Color.Red);
		Colors.Window.TitleInactive = base.Renderer.PixelColor(_texture, 12u, 508u, Color.Yellow);
		Colors.Button.Normal = base.Renderer.PixelColor(_texture, 20u, 508u, Color.Yellow);
		Colors.Button.Hover = base.Renderer.PixelColor(_texture, 28u, 508u, Color.Yellow);
		Colors.Button.Active = base.Renderer.PixelColor(_texture, 20u, 500u, Color.Yellow);
		Colors.Button.Disabled = base.Renderer.PixelColor(_texture, 28u, 500u, Color.Yellow);
		Colors.Tab.Active.Normal = base.Renderer.PixelColor(_texture, 20u, 508u, Color.Yellow);
		Colors.Tab.Active.Hover = base.Renderer.PixelColor(_texture, 28u, 508u, Color.Yellow);
		Colors.Tab.Active.Active = base.Renderer.PixelColor(_texture, 20u, 500u, Color.Yellow);
		Colors.Tab.Active.Disabled = base.Renderer.PixelColor(_texture, 28u, 500u, Color.Yellow);
		Colors.Tab.Inactive.Normal = base.Renderer.PixelColor(_texture, 20u, 508u, Color.Yellow);
		Colors.Tab.Inactive.Hover = base.Renderer.PixelColor(_texture, 28u, 508u, Color.Yellow);
		Colors.Tab.Inactive.Active = base.Renderer.PixelColor(_texture, 20u, 500u, Color.Yellow);
		Colors.Tab.Inactive.Disabled = base.Renderer.PixelColor(_texture, 28u, 500u, Color.Yellow);
		Colors.Label.Normal = base.Renderer.PixelColor(_texture, 68u, 508u, Color.Yellow);
		Colors.Label.Hover = base.Renderer.PixelColor(_texture, 76u, 508u, Color.Yellow);
		Colors.Label.Disabled = base.Renderer.PixelColor(_texture, 68u, 500u, Color.Yellow);
		Colors.Label.Active = base.Renderer.PixelColor(_texture, 76u, 500u, Color.Yellow);
		Colors.Tree.Lines = base.Renderer.PixelColor(_texture, 84u, 508u, Color.Yellow);
		Colors.Tree.Normal = base.Renderer.PixelColor(_texture, 92u, 508u, Color.Yellow);
		Colors.Tree.Hover = base.Renderer.PixelColor(_texture, 84u, 500u, Color.Yellow);
		Colors.Tree.Selected = base.Renderer.PixelColor(_texture, 92u, 500u, Color.Yellow);
		Colors.Properties.LineNormal = base.Renderer.PixelColor(_texture, 100u, 508u, Color.Yellow);
		Colors.Properties.LineSelected = base.Renderer.PixelColor(_texture, 108u, 508u, Color.Yellow);
		Colors.Properties.LineHover = base.Renderer.PixelColor(_texture, 100u, 500u, Color.Yellow);
		Colors.Properties.Title = base.Renderer.PixelColor(_texture, 108u, 500u, Color.Yellow);
		Colors.Properties.ColumnNormal = base.Renderer.PixelColor(_texture, 116u, 508u, Color.Yellow);
		Colors.Properties.ColumnSelected = base.Renderer.PixelColor(_texture, 124u, 508u, Color.Yellow);
		Colors.Properties.ColumnHover = base.Renderer.PixelColor(_texture, 116u, 500u, Color.Yellow);
		Colors.Properties.Border = base.Renderer.PixelColor(_texture, 124u, 500u, Color.Yellow);
		Colors.Properties.LabelNormal = base.Renderer.PixelColor(_texture, 132u, 508u, Color.Yellow);
		Colors.Properties.LabelSelected = base.Renderer.PixelColor(_texture, 140u, 508u, Color.Yellow);
		Colors.Properties.LabelHover = base.Renderer.PixelColor(_texture, 132u, 500u, Color.Yellow);
		Colors.ModalBackground = base.Renderer.PixelColor(_texture, 148u, 508u, Color.Yellow);
		Colors.TooltipText = base.Renderer.PixelColor(_texture, 76u, 508u, Color.Yellow);
		Colors.Category.Header = base.Renderer.PixelColor(_texture, 148u, 500u, Color.Yellow);
		Colors.Category.HeaderClosed = base.Renderer.PixelColor(_texture, 156u, 500u, Color.Yellow);
		Colors.Category.Line.Text = base.Renderer.PixelColor(_texture, 164u, 508u, Color.Yellow);
		Colors.Category.Line.TextHover = base.Renderer.PixelColor(_texture, 172u, 508u, Color.Yellow);
		Colors.Category.Line.TextSelected = base.Renderer.PixelColor(_texture, 164u, 500u, Color.Yellow);
		Colors.Category.Line.Button = base.Renderer.PixelColor(_texture, 172u, 500u, Color.Yellow);
		Colors.Category.Line.ButtonHover = base.Renderer.PixelColor(_texture, 180u, 508u, Color.Yellow);
		Colors.Category.Line.ButtonSelected = base.Renderer.PixelColor(_texture, 188u, 508u, Color.Yellow);
		Colors.Category.LineAlt.Text = base.Renderer.PixelColor(_texture, 180u, 500u, Color.Yellow);
		Colors.Category.LineAlt.TextHover = base.Renderer.PixelColor(_texture, 188u, 500u, Color.Yellow);
		Colors.Category.LineAlt.TextSelected = base.Renderer.PixelColor(_texture, 196u, 508u, Color.Yellow);
		Colors.Category.LineAlt.Button = base.Renderer.PixelColor(_texture, 204u, 508u, Color.Yellow);
		Colors.Category.LineAlt.ButtonHover = base.Renderer.PixelColor(_texture, 196u, 500u, Color.Yellow);
		Colors.Category.LineAlt.ButtonSelected = base.Renderer.PixelColor(_texture, 204u, 500u, Color.Yellow);
	}

	protected virtual void InitializeTextures()
	{
		mTextures.Shadow = new Bordered(_texture, 448f, 0f, 31f, 31f, Margin.Eight);
		mTextures.Tooltip = new Bordered(_texture, 128f, 320f, 127f, 31f, Margin.Eight);
		mTextures.StatusBar = new Bordered(_texture, 128f, 288f, 127f, 31f, Margin.Eight);
		mTextures.Selection = new Bordered(_texture, 384f, 32f, 31f, 31f, Margin.Four);
		mTextures.Panel.Control = new Bordered(_texture, 256f, 0f, 63f, 63f, new Margin(16, 16, 16, 16));
		mTextures.Panel.Normal = new Bordered(_texture, 256f, 0f, 63f, 63f, new Margin(16, 16, 16, 16));
		mTextures.Panel.Bright = new Bordered(_texture, 320f, 0f, 63f, 63f, new Margin(16, 16, 16, 16));
		mTextures.Panel.Dark = new Bordered(_texture, 256f, 64f, 63f, 63f, new Margin(16, 16, 16, 16));
		mTextures.Panel.Highlight = new Bordered(_texture, 320f, 64f, 63f, 63f, new Margin(16, 16, 16, 16));
		mTextures.Window.Normal = new Bordered(_texture, 0f, 0f, 127f, 127f, new Margin(8, 32, 8, 8));
		mTextures.Window.Inactive = new Bordered(_texture, 128f, 0f, 127f, 127f, new Margin(8, 32, 8, 8));
		mTextures.CheckBox.Active_Baked.Checked = new Intersect.Client.Framework.Gwen.Skin.Texturing.Single(_texture, 448f, 32f, 15f, 15f);
		mTextures.CheckBox.Active_Baked.Normal = new Intersect.Client.Framework.Gwen.Skin.Texturing.Single(_texture, 464f, 32f, 15f, 15f);
		mTextures.CheckBox.Disabled_Baked.Normal = new Intersect.Client.Framework.Gwen.Skin.Texturing.Single(_texture, 448f, 48f, 15f, 15f);
		mTextures.CheckBox.Disabled_Baked.Normal = new Intersect.Client.Framework.Gwen.Skin.Texturing.Single(_texture, 464f, 48f, 15f, 15f);
		mTextures.RadioButton.Active_Baked.Checked = new Intersect.Client.Framework.Gwen.Skin.Texturing.Single(_texture, 448f, 64f, 15f, 15f);
		mTextures.RadioButton.Active_Baked.Normal = new Intersect.Client.Framework.Gwen.Skin.Texturing.Single(_texture, 464f, 64f, 15f, 15f);
		mTextures.RadioButton.Disabled_Baked.Normal = new Intersect.Client.Framework.Gwen.Skin.Texturing.Single(_texture, 448f, 80f, 15f, 15f);
		mTextures.RadioButton.Disabled_Baked.Normal = new Intersect.Client.Framework.Gwen.Skin.Texturing.Single(_texture, 464f, 80f, 15f, 15f);
		mTextures.TextBox.Normal = new Bordered(_texture, 0f, 150f, 127f, 21f, Margin.Four);
		mTextures.TextBox.Focus = new Bordered(_texture, 0f, 172f, 127f, 21f, Margin.Four);
		mTextures.TextBox.Disabled = new Bordered(_texture, 0f, 193f, 127f, 21f, Margin.Four);
		mTextures.Menu.Strip = new Bordered(_texture, 0f, 128f, 127f, 21f, Margin.One);
		mTextures.Menu.BackgroundWithMargin = new Bordered(_texture, 128f, 128f, 127f, 63f, new Margin(24, 8, 8, 8));
		mTextures.Menu.Background = new Bordered(_texture, 128f, 192f, 127f, 63f, Margin.Eight);
		mTextures.Menu.Hovered = new Bordered(_texture, 320f, 320f, 32f, 32f, Margin.Six);
		mTextures.Menu.RightArrow = new Intersect.Client.Framework.Gwen.Skin.Texturing.Single(_texture, 464f, 112f, 15f, 15f);
		mTextures.Menu.Check = new Intersect.Client.Framework.Gwen.Skin.Texturing.Single(_texture, 448f, 112f, 15f, 15f);
		mTextures.Tab.Control = new Bordered(_texture, 0f, 256f, 127f, 127f, Margin.Eight);
		mTextures.Tab.Bottom.Active = new Bordered(_texture, 0f, 416f, 63f, 31f, Margin.Eight);
		mTextures.Tab.Bottom.Inactive = new Bordered(_texture, 128f, 416f, 63f, 31f, Margin.Eight);
		mTextures.Tab.Top.Active = new Bordered(_texture, 0f, 384f, 63f, 31f, Margin.Eight);
		mTextures.Tab.Top.Inactive = new Bordered(_texture, 128f, 384f, 63f, 31f, Margin.Eight);
		mTextures.Tab.Left.Active = new Bordered(_texture, 64f, 384f, 31f, 63f, Margin.Eight);
		mTextures.Tab.Left.Inactive = new Bordered(_texture, 192f, 384f, 31f, 63f, Margin.Eight);
		mTextures.Tab.Right.Active = new Bordered(_texture, 96f, 384f, 31f, 63f, Margin.Eight);
		mTextures.Tab.Right.Inactive = new Bordered(_texture, 224f, 384f, 31f, 63f, Margin.Eight);
		mTextures.Tab.HeaderBar = new Bordered(_texture, 128f, 352f, 127f, 31f, Margin.Four);
		mTextures.Window.CloseButton = new SkinTextures._Input._Button
		{
			Normal = new Bordered(_texture, 0f, 224f, 24f, 24f, default(Margin)),
			Disabled = new Bordered(_texture, 32f, 224f, 24f, 24f, default(Margin)),
			Hovered = new Bordered(_texture, 64f, 224f, 24f, 24f, default(Margin)),
			Active = new Bordered(_texture, 96f, 224f, 24f, 24f, default(Margin))
		};
		mTextures.Scroller.TrackV = new Bordered(_texture, 384f, 208f, 15f, 127f, Margin.Four);
		mTextures.Scroller.BarV.Normal = new Bordered(_texture, 400f, 208f, 15f, 127f, Margin.Four);
		mTextures.Scroller.BarV.Hovered = new Bordered(_texture, 416f, 208f, 15f, 127f, Margin.Four);
		mTextures.Scroller.BarV.Active = new Bordered(_texture, 432f, 208f, 15f, 127f, Margin.Four);
		mTextures.Scroller.BarV.Disabled = new Bordered(_texture, 448f, 208f, 15f, 127f, Margin.Four);
		mTextures.Scroller.TrackH = new Bordered(_texture, 384f, 128f, 127f, 15f, Margin.Four);
		mTextures.Scroller.BarH.Normal = new Bordered(_texture, 384f, 144f, 127f, 15f, Margin.Four);
		mTextures.Scroller.BarH.Hovered = new Bordered(_texture, 384f, 160f, 127f, 15f, Margin.Four);
		mTextures.Scroller.BarH.Active = new Bordered(_texture, 384f, 176f, 127f, 15f, Margin.Four);
		mTextures.Scroller.BarH.Disabled = new Bordered(_texture, 384f, 192f, 127f, 15f, Margin.Four);
		mTextures.Scroller.Button.Normal = new Bordered[4];
		mTextures.Scroller.Button.Disabled = new Bordered[4];
		mTextures.Scroller.Button.Hovered = new Bordered[4];
		mTextures.Scroller.Button.Active = new Bordered[4];
		mTextures.Tree.Background = new Bordered(_texture, 256f, 128f, 127f, 127f, new Margin(16, 16, 16, 16));
		mTextures.Tree.Plus = new Intersect.Client.Framework.Gwen.Skin.Texturing.Single(_texture, 448f, 96f, 15f, 15f);
		mTextures.Tree.Minus = new Intersect.Client.Framework.Gwen.Skin.Texturing.Single(_texture, 464f, 96f, 15f, 15f);
		mTextures.Input.Button.Normal = new Bordered(_texture, 480f, 0f, 31f, 31f, Margin.Eight);
		mTextures.Input.Button.Hovered = new Bordered(_texture, 480f, 32f, 31f, 31f, Margin.Eight);
		mTextures.Input.Button.Disabled = new Bordered(_texture, 480f, 64f, 31f, 31f, Margin.Eight);
		mTextures.Input.Button.Active = new Bordered(_texture, 480f, 96f, 31f, 31f, Margin.Eight);
		for (int i = 0; i < 4; i++)
		{
			mTextures.Scroller.Button.Normal[i] = new Bordered(_texture, 464f, 208 + i * 16, 15f, 15f, Margin.Two);
			mTextures.Scroller.Button.Hovered[i] = new Bordered(_texture, 480f, 208 + i * 16, 15f, 15f, Margin.Two);
			mTextures.Scroller.Button.Active[i] = new Bordered(_texture, 464f, 272 + i * 16, 15f, 15f, Margin.Two);
			mTextures.Scroller.Button.Disabled[i] = new Bordered(_texture, 528f, 272 + i * 16, 15f, 15f, Margin.Two);
		}
		mTextures.Input.ListBox.Background = new Bordered(_texture, 256f, 256f, 63f, 127f, Margin.Six);
		mTextures.Input.ListBox.Hovered = new Bordered(_texture, 320f, 320f, 32f, 32f, Margin.Six);
		mTextures.Input.ListBox.EvenLine = new Bordered(_texture, 352f, 256f, 32f, 32f, Margin.Six);
		mTextures.Input.ListBox.EvenLineSelected = new Bordered(_texture, 320f, 256f, 32f, 32f, Margin.Six);
		mTextures.Input.ListBox.OddLine = new Bordered(_texture, 352f, 288f, 32f, 32f, Margin.Six);
		mTextures.Input.ListBox.OddLineSelected = new Bordered(_texture, 320f, 288f, 32f, 32f, Margin.Six);
		mTextures.Input.ComboBox.Normal = new Bordered(_texture, 385f, 336f, 127f, 31f, new Margin(8, 8, 32, 8));
		mTextures.Input.ComboBox.Hover = new Bordered(_texture, 385f, 368f, 127f, 31f, new Margin(8, 8, 32, 8));
		mTextures.Input.ComboBox.Down = new Bordered(_texture, 385f, 400f, 127f, 31f, new Margin(8, 8, 32, 8));
		mTextures.Input.ComboBox.Disabled = new Bordered(_texture, 385f, 432f, 127f, 31f, new Margin(8, 8, 32, 8));
		mTextures.Input.ComboBox.Button.Normal = new Intersect.Client.Framework.Gwen.Skin.Texturing.Single(_texture, 496f, 272f, 15f, 15f);
		mTextures.Input.ComboBox.Button.Hover = new Intersect.Client.Framework.Gwen.Skin.Texturing.Single(_texture, 496f, 288f, 15f, 15f);
		mTextures.Input.ComboBox.Button.Down = new Intersect.Client.Framework.Gwen.Skin.Texturing.Single(_texture, 496f, 304f, 15f, 15f);
		mTextures.Input.ComboBox.Button.Disabled = new Intersect.Client.Framework.Gwen.Skin.Texturing.Single(_texture, 496f, 320f, 15f, 15f);
		mTextures.Input.UpDown.Up.Normal = new Intersect.Client.Framework.Gwen.Skin.Texturing.Single(_texture, 384f, 112f, 7f, 7f);
		mTextures.Input.UpDown.Up.Hover = new Intersect.Client.Framework.Gwen.Skin.Texturing.Single(_texture, 392f, 112f, 7f, 7f);
		mTextures.Input.UpDown.Up.Down = new Intersect.Client.Framework.Gwen.Skin.Texturing.Single(_texture, 400f, 112f, 7f, 7f);
		mTextures.Input.UpDown.Up.Disabled = new Intersect.Client.Framework.Gwen.Skin.Texturing.Single(_texture, 408f, 112f, 7f, 7f);
		mTextures.Input.UpDown.Down.Normal = new Intersect.Client.Framework.Gwen.Skin.Texturing.Single(_texture, 384f, 120f, 7f, 7f);
		mTextures.Input.UpDown.Down.Hover = new Intersect.Client.Framework.Gwen.Skin.Texturing.Single(_texture, 392f, 120f, 7f, 7f);
		mTextures.Input.UpDown.Down.Down = new Intersect.Client.Framework.Gwen.Skin.Texturing.Single(_texture, 400f, 120f, 7f, 7f);
		mTextures.Input.UpDown.Down.Disabled = new Intersect.Client.Framework.Gwen.Skin.Texturing.Single(_texture, 408f, 120f, 7f, 7f);
		mTextures.ProgressBar.Back = new Bordered(_texture, 384f, 0f, 31f, 31f, Margin.Eight);
		mTextures.ProgressBar.Front = new Bordered(_texture, 416f, 0f, 31f, 31f, Margin.Eight);
		mTextures.Input.Slider.H.Normal = new Bordered(_texture, 416f, 32f, 15f, 15f, new Margin(7));
		mTextures.Input.Slider.H.Hover = new Bordered(_texture, 416f, 48f, 15f, 15f, new Margin(7));
		mTextures.Input.Slider.H.Active = new Bordered(_texture, 416f, 64f, 15f, 15f, new Margin(7));
		mTextures.Input.Slider.H.Disabled = new Bordered(_texture, 416f, 80f, 15f, 15f, new Margin(7));
		mTextures.Input.Slider.V.Normal = new Bordered(_texture, 432f, 32f, 15f, 15f, new Margin(7));
		mTextures.Input.Slider.V.Hover = new Bordered(_texture, 432f, 48f, 15f, 15f, new Margin(7));
		mTextures.Input.Slider.V.Active = new Bordered(_texture, 432f, 64f, 15f, 15f, new Margin(7));
		mTextures.Input.Slider.V.Disabled = new Bordered(_texture, 432f, 80f, 15f, 15f, new Margin(7));
		mTextures.CategoryList.Outer = new Bordered(_texture, 256f, 384f, 63f, 63f, Margin.Eight);
		mTextures.CategoryList.Inner = new Bordered(_texture, 320f, 384f, 63f, 63f, new Margin(8, 21, 8, 8));
		mTextures.CategoryList.Header = new Bordered(_texture, 320f, 352f, 63f, 31f, Margin.Eight);
	}

	public override void DrawSplitter(SplitterBar splitterBar)
	{
		throw new NotImplementedException();
	}

	protected virtual void DrawButton(Button button, SkinTextures._Input._Button textureGroup)
	{
		ComponentState componentState = button.ComponentState;
		IGameTexture stateTexture = button.GetStateTexture(componentState);
		if (stateTexture == null)
		{
			(componentState switch
			{
				ComponentState.Normal => textureGroup.Normal, 
				ComponentState.Hovered => textureGroup.Hovered, 
				ComponentState.Active => textureGroup.Active, 
				ComponentState.Disabled => textureGroup.Disabled, 
				_ => throw new UnreachableException(), 
			}).Draw(base.Renderer, button.RenderBounds, button.RenderColor);
		}
		else
		{
			base.Renderer.DrawColor = button.RenderColor;
			base.Renderer.DrawTexturedRect(stateTexture, button.RenderBounds, button.RenderColor);
		}
		if (button.HasFocus)
		{
			base.Renderer.PushDrawColor(Color.White);
			base.Renderer.PushLineThickness(1);
			base.Renderer.DrawLinedRect(button.RenderBounds);
			base.Renderer.PopLineThickness();
			base.Renderer.PopDrawColor();
		}
	}

	public override void DrawButton(Button button)
	{
		DrawButton(button, mTextures.Input.Button);
	}

	public override void DrawWindowCloseButton(CloseButton closeButton)
	{
		DrawButton(closeButton, mTextures.Window.CloseButton);
	}

	public override void DrawMenuRightArrow(Intersect.Client.Framework.Gwen.Control.Base control)
	{
		mTextures.Menu.RightArrow.Draw(base.Renderer, control.RenderBounds, control.RenderColor);
	}

	public override void DrawMenuItem(Intersect.Client.Framework.Gwen.Control.Base control, bool submenuOpen, bool isChecked)
	{
		if (control.ShouldDrawBackground)
		{
			if (submenuOpen || control.IsHovered)
			{
				mTextures.Menu.Hovered.Draw(base.Renderer, control.RenderBounds, control.RenderColor);
			}
			else if (isChecked)
			{
				mTextures.Menu.Check.Draw(base.Renderer, new Rectangle(control.RenderBounds.X + 4, control.RenderBounds.Y + 3, 15, 15), control.RenderColor);
			}
		}
	}

	public override void DrawMenuStrip(Intersect.Client.Framework.Gwen.Control.Base control)
	{
		mTextures.Menu.Strip.Draw(base.Renderer, control.RenderBounds, control.RenderColor);
	}

	public override void DrawMenu(Intersect.Client.Framework.Gwen.Control.Base control, bool paddingDisabled)
	{
		if (!control.ShouldDrawBackground || !(control is Menu menu))
		{
			return;
		}
		IGameTexture template = menu.GetTemplate();
		if (template == null)
		{
			if (paddingDisabled)
			{
				mTextures.Menu.Background.Draw(base.Renderer, control.RenderBounds, control.RenderColor);
			}
			else
			{
				mTextures.Menu.BackgroundWithMargin.Draw(base.Renderer, control.RenderBounds, control.RenderColor);
			}
			return;
		}
		base.Renderer.DrawTexturedRect(template, new Rectangle(control.RenderBounds.X, control.RenderBounds.Y, 2, 2), control.RenderColor, 0f, 0f, 2f / (float)template.Width, 2f / (float)template.Height);
		base.Renderer.DrawTexturedRect(template, new Rectangle(control.RenderBounds.X + 2, control.RenderBounds.Y, control.RenderBounds.Width - 4, 2), control.RenderColor, 2f / (float)template.Width, 0f, ((float)template.Width - 2f) / (float)template.Width, 2f / (float)template.Height);
		base.Renderer.DrawTexturedRect(template, new Rectangle(control.RenderBounds.Right - 2, control.RenderBounds.Y, 2, 2), control.RenderColor, ((float)template.Width - 2f) / (float)template.Width, 0f, 1f, 2f / (float)template.Height);
		base.Renderer.DrawTexturedRect(template, new Rectangle(control.RenderBounds.X, control.RenderBounds.Y + 2, 2, control.RenderBounds.Height - 4), control.RenderColor, 0f, 2f / (float)template.Height, 2f / (float)template.Width, ((float)template.Height - 2f) / (float)template.Height);
		base.Renderer.DrawTexturedRect(template, new Rectangle(control.RenderBounds.X + 2, control.RenderBounds.Y + 2, control.RenderBounds.Width - 4, control.RenderBounds.Height - 4), control.RenderColor, 2f / (float)template.Width, 2f / (float)template.Height, ((float)template.Width - 2f) / (float)template.Width, ((float)template.Height - 2f) / (float)template.Height);
		base.Renderer.DrawTexturedRect(template, new Rectangle(control.RenderBounds.Width - 2, control.RenderBounds.Y + 2, 2, control.RenderBounds.Height - 4), control.RenderColor, ((float)template.Width - 2f) / (float)template.Width, 2f / (float)template.Height, 1f, ((float)template.Height - 2f) / (float)template.Height);
		base.Renderer.DrawTexturedRect(template, new Rectangle(control.RenderBounds.X, control.RenderBounds.Bottom - 2, 2, 2), control.RenderColor, 0f, ((float)template.Height - 2f) / (float)template.Height, 2f / (float)template.Width);
		base.Renderer.DrawTexturedRect(template, new Rectangle(control.RenderBounds.X + 2, control.RenderBounds.Bottom - 2, control.RenderBounds.Width - 4, 2), control.RenderColor, 2f / (float)template.Width, ((float)template.Height - 2f) / (float)template.Height, ((float)template.Width - 2f) / (float)template.Width);
		base.Renderer.DrawTexturedRect(template, new Rectangle(control.RenderBounds.Right - 2, control.RenderBounds.Bottom - 2, 2, 2), control.RenderColor, ((float)template.Width - 2f) / (float)template.Width, ((float)template.Height - 2f) / (float)template.Height);
	}

	public override void DrawShadow(Intersect.Client.Framework.Gwen.Control.Base control)
	{
		Rectangle renderBounds = control.RenderBounds;
		renderBounds.X -= 4;
		renderBounds.Y -= 4;
		renderBounds.Width += 10;
		renderBounds.Height += 10;
		mTextures.Shadow.Draw(base.Renderer, renderBounds, control.RenderColor);
	}

	public override void DrawRadioButton(Intersect.Client.Framework.Gwen.Control.Base control, bool selected, bool depressed)
	{
		if (TryGetOverrideTexture(control as Checkbox, selected, depressed, out IGameTexture overrideTexture))
		{
			base.Renderer.DrawColor = control.RenderColor;
			base.Renderer.DrawTexturedRect(overrideTexture, control.RenderBounds, control.RenderColor);
		}
		else if (selected)
		{
			if (control.IsDisabledByTree)
			{
				mTextures.RadioButton.Disabled_Baked.Checked.Draw(base.Renderer, control.RenderBounds, control.RenderColor);
			}
			else
			{
				mTextures.RadioButton.Active_Baked.Checked.Draw(base.Renderer, control.RenderBounds, control.RenderColor);
			}
		}
		else if (control.IsDisabledByTree)
		{
			mTextures.RadioButton.Disabled_Baked.Normal.Draw(base.Renderer, control.RenderBounds, control.RenderColor);
		}
		else
		{
			mTextures.RadioButton.Active_Baked.Normal.Draw(base.Renderer, control.RenderBounds, control.RenderColor);
		}
	}

	protected bool TryGetOverrideTexture(Checkbox control, bool selected, bool pressed, out IGameTexture overrideTexture)
	{
		Checkbox.ControlState state = Checkbox.ControlState.Normal;
		if (selected)
		{
			state = (control.IsDisabledByTree ? Checkbox.ControlState.CheckedDisabled : Checkbox.ControlState.CheckedNormal);
		}
		else if (control.IsDisabledByTree)
		{
			state = Checkbox.ControlState.Disabled;
		}
		overrideTexture = control.GetImage(state);
		return overrideTexture != null;
	}

	public override void DrawCheckBox(Intersect.Client.Framework.Gwen.Control.Base control, bool selected, bool depressed)
	{
		if (TryGetOverrideTexture(control as Checkbox, selected, depressed, out IGameTexture overrideTexture))
		{
			base.Renderer.DrawColor = control.RenderColor;
			base.Renderer.DrawTexturedRect(overrideTexture, control.RenderBounds, control.RenderColor);
		}
		else if (selected)
		{
			if (control.IsDisabledByTree)
			{
				mTextures.CheckBox.Disabled_Baked.Checked.Draw(base.Renderer, control.RenderBounds, control.RenderColor);
			}
			else
			{
				mTextures.CheckBox.Active_Baked.Checked.Draw(base.Renderer, control.RenderBounds, control.RenderColor);
			}
		}
		else if (control.IsDisabledByTree)
		{
			mTextures.CheckBox.Disabled_Baked.Normal.Draw(base.Renderer, control.RenderBounds, control.RenderColor);
		}
		else
		{
			mTextures.CheckBox.Active_Baked.Normal.Draw(base.Renderer, control.RenderBounds, control.RenderColor);
		}
	}

	public override void DrawGroupBox(Intersect.Client.Framework.Gwen.Control.Base control, int textStart, int textHeight, int textWidth)
	{
		Rectangle renderBounds = control.RenderBounds;
		renderBounds.Y += (int)((float)textHeight * 0.5f);
		renderBounds.Height -= (int)((float)textHeight * 0.5f);
		Color drawColor = Color.FromArgb(50, 0, 50, 60);
		Color drawColor2 = Color.FromArgb(150, 255, 255, 255);
		base.Renderer.DrawColor = drawColor2;
		base.Renderer.DrawFilledRect(new Rectangle(renderBounds.X + 1, renderBounds.Y + 1, textStart - 3, 1));
		base.Renderer.DrawFilledRect(new Rectangle(renderBounds.X + 1 + textStart + textWidth, renderBounds.Y + 1, renderBounds.Width - textStart + textWidth - 2, 1));
		base.Renderer.DrawFilledRect(new Rectangle(renderBounds.X + 1, renderBounds.Y + renderBounds.Height - 1, renderBounds.X + renderBounds.Width - 2, 1));
		base.Renderer.DrawFilledRect(new Rectangle(renderBounds.X + 1, renderBounds.Y + 1, 1, renderBounds.Height));
		base.Renderer.DrawFilledRect(new Rectangle(renderBounds.X + renderBounds.Width - 2, renderBounds.Y + 1, 1, renderBounds.Height - 1));
		base.Renderer.DrawColor = drawColor;
		base.Renderer.DrawFilledRect(new Rectangle(renderBounds.X + 1, renderBounds.Y, textStart - 3, 1));
		base.Renderer.DrawFilledRect(new Rectangle(renderBounds.X + 1 + textStart + textWidth, renderBounds.Y, renderBounds.Width - textStart - textWidth - 2, 1));
		base.Renderer.DrawFilledRect(new Rectangle(renderBounds.X + 1, renderBounds.Y + renderBounds.Height - 1, renderBounds.X + renderBounds.Width - 2, 1));
		base.Renderer.DrawFilledRect(new Rectangle(renderBounds.X, renderBounds.Y + 1, 1, renderBounds.Height - 1));
		base.Renderer.DrawFilledRect(new Rectangle(renderBounds.X + renderBounds.Width - 1, renderBounds.Y + 1, 1, renderBounds.Height - 1));
	}

	public override void DrawTextBox(Intersect.Client.Framework.Gwen.Control.Base control)
	{
		if (control.IsDisabledByTree)
		{
			mTextures.TextBox.Disabled.Draw(base.Renderer, control.RenderBounds, control.RenderColor);
		}
		else if (control.HasFocus)
		{
			mTextures.TextBox.Focus.Draw(base.Renderer, control.RenderBounds, control.RenderColor);
		}
		else
		{
			mTextures.TextBox.Normal.Draw(base.Renderer, control.RenderBounds, control.RenderColor);
		}
	}

	public override void DrawTabButton(Intersect.Client.Framework.Gwen.Control.Base control, bool active, Pos dir)
	{
		if (active)
		{
			DrawActiveTabButton(control, dir);
			return;
		}
		switch (dir)
		{
		case Pos.Top:
			mTextures.Tab.Top.Inactive.Draw(base.Renderer, control.RenderBounds, control.RenderColor);
			break;
		case Pos.Left:
			mTextures.Tab.Left.Inactive.Draw(base.Renderer, control.RenderBounds, control.RenderColor);
			break;
		case Pos.Bottom:
			mTextures.Tab.Bottom.Inactive.Draw(base.Renderer, control.RenderBounds, control.RenderColor);
			break;
		case Pos.Right:
			mTextures.Tab.Right.Inactive.Draw(base.Renderer, control.RenderBounds, control.RenderColor);
			break;
		}
	}

	private void DrawActiveTabButton(Intersect.Client.Framework.Gwen.Control.Base control, Pos dir)
	{
		switch (dir)
		{
		case Pos.Top:
			mTextures.Tab.Top.Active.Draw(base.Renderer, control.RenderBounds.Add(new Rectangle(0, 0, 0, 0)), control.RenderColor);
			break;
		case Pos.Left:
			mTextures.Tab.Left.Active.Draw(base.Renderer, control.RenderBounds.Add(new Rectangle(0, 0, 0, 0)), control.RenderColor);
			break;
		case Pos.Bottom:
			mTextures.Tab.Bottom.Active.Draw(base.Renderer, control.RenderBounds.Add(new Rectangle(0, 0, 0, 0)), control.RenderColor);
			break;
		case Pos.Right:
			mTextures.Tab.Right.Active.Draw(base.Renderer, control.RenderBounds.Add(new Rectangle(0, 0, 0, 0)), control.RenderColor);
			break;
		}
	}

	public override void DrawPanel(Panel panel)
	{
		mTextures.Panel.Control.Draw(base.Renderer, panel.RenderBounds, panel.RenderColor);
	}

	public override void DrawTabControl(Intersect.Client.Framework.Gwen.Control.Base control)
	{
		mTextures.Tab.Control.Draw(base.Renderer, control.RenderBounds, control.RenderColor);
	}

	public override void DrawTabTitleBar(Intersect.Client.Framework.Gwen.Control.Base control)
	{
		mTextures.Tab.HeaderBar.Draw(base.Renderer, control.RenderBounds, control.RenderColor);
	}

	public override void DrawWindow(Intersect.Client.Framework.Gwen.Control.Base control, int topHeight, bool inFocus)
	{
		IGameTexture gameTexture = null;
		if (((WindowControl)control).GetImage(WindowControl.ControlState.Active) != null)
		{
			gameTexture = ((WindowControl)control).GetImage(WindowControl.ControlState.Active);
		}
		if (((WindowControl)control).GetImage(WindowControl.ControlState.Inactive) != null)
		{
			gameTexture = ((WindowControl)control).GetImage(WindowControl.ControlState.Inactive);
		}
		if (gameTexture != null)
		{
			base.Renderer.DrawColor = control.RenderColor;
			base.Renderer.DrawTexturedRect(gameTexture, control.RenderBounds, control.RenderColor);
		}
		else if (inFocus)
		{
			mTextures.Window.Normal.Draw(base.Renderer, control.RenderBounds, control.RenderColor);
		}
		else
		{
			mTextures.Window.Inactive.Draw(base.Renderer, control.RenderBounds, control.RenderColor);
		}
	}

	public override void DrawHighlight(Intersect.Client.Framework.Gwen.Control.Base control)
	{
		Rectangle renderBounds = control.RenderBounds;
		base.Renderer.DrawColor = Color.FromArgb(255, 255, 100, 255);
		base.Renderer.DrawFilledRect(renderBounds);
	}

	public override void DrawScrollBar(Intersect.Client.Framework.Gwen.Control.Base control, bool horizontal, bool depressed)
	{
		if (!control.ShouldDrawBackground || !(control is ScrollBar scrollBar))
		{
			return;
		}
		IGameTexture template = scrollBar.GetTemplate();
		if (template == null)
		{
			if (horizontal)
			{
				mTextures.Scroller.TrackH.Draw(base.Renderer, control.RenderBounds, control.RenderColor);
			}
			else
			{
				mTextures.Scroller.TrackV.Draw(base.Renderer, control.RenderBounds, control.RenderColor);
			}
			return;
		}
		base.Renderer.DrawColor = control.RenderColor;
		base.Renderer.DrawTexturedRect(template, new Rectangle(control.RenderBounds.X, control.RenderBounds.Y, 2, 2), control.RenderColor, 0f, 0f, 2f / (float)template.Width, 2f / (float)template.Height);
		base.Renderer.DrawTexturedRect(template, new Rectangle(control.RenderBounds.X + 2, control.RenderBounds.Y, control.RenderBounds.Width - 4, 2), control.RenderColor, 2f / (float)template.Width, 0f, ((float)template.Width - 2f) / (float)template.Width, 2f / (float)template.Height);
		base.Renderer.DrawTexturedRect(template, new Rectangle(control.RenderBounds.Right - 2, control.RenderBounds.Y, 2, 2), control.RenderColor, ((float)template.Width - 2f) / (float)template.Width, 0f, 1f, 2f / (float)template.Height);
		base.Renderer.DrawTexturedRect(template, new Rectangle(control.RenderBounds.X, control.RenderBounds.Y + 2, 2, control.RenderBounds.Height - 4), control.RenderColor, 0f, 2f / (float)template.Height, 2f / (float)template.Width, ((float)template.Height - 2f) / (float)template.Height);
		base.Renderer.DrawTexturedRect(template, new Rectangle(control.RenderBounds.X + 2, control.RenderBounds.Y + 2, control.RenderBounds.Width - 4, control.RenderBounds.Height - 4), control.RenderColor, 2f / (float)template.Width, 2f / (float)template.Height, ((float)template.Width - 2f) / (float)template.Width, ((float)template.Height - 2f) / (float)template.Height);
		base.Renderer.DrawTexturedRect(template, new Rectangle(control.RenderBounds.Width - 2, control.RenderBounds.Y + 2, 2, control.RenderBounds.Height - 4), control.RenderColor, ((float)template.Width - 2f) / (float)template.Width, 2f / (float)template.Height, 1f, ((float)template.Height - 2f) / (float)template.Height);
		base.Renderer.DrawTexturedRect(template, new Rectangle(control.RenderBounds.X, control.RenderBounds.Bottom - 2, 2, 2), control.RenderColor, 0f, ((float)template.Height - 2f) / (float)template.Height, 2f / (float)template.Width);
		base.Renderer.DrawTexturedRect(template, new Rectangle(control.RenderBounds.X + 2, control.RenderBounds.Bottom - 2, control.RenderBounds.Width - 4, 2), control.RenderColor, 2f / (float)template.Width, ((float)template.Height - 2f) / (float)template.Height, ((float)template.Width - 2f) / (float)template.Width);
		base.Renderer.DrawTexturedRect(template, new Rectangle(control.RenderBounds.Right - 2, control.RenderBounds.Bottom - 2, 2, 2), control.RenderColor, ((float)template.Width - 2f) / (float)template.Width, ((float)template.Height - 2f) / (float)template.Height);
	}

	public override void DrawScrollBarBar(ScrollBarBar scrollBarBar)
	{
		IGameTexture image = scrollBarBar.GetImage(ComponentState.Normal);
		if (scrollBarBar.IsDisabledByTree)
		{
			image = scrollBarBar.GetImage(ComponentState.Disabled);
		}
		else if (scrollBarBar.IsActive)
		{
			image = scrollBarBar.GetImage(ComponentState.Active);
		}
		else if (scrollBarBar.IsHovered)
		{
			image = scrollBarBar.GetImage(ComponentState.Hovered);
		}
		if (image == null)
		{
			SkinTextures._Input._Button button = (scrollBarBar.IsHorizontal ? mTextures.Scroller.BarH : mTextures.Scroller.BarV);
			IAtlasDrawable atlasDrawable = button.Normal;
			if (scrollBarBar.IsDisabledByTree)
			{
				atlasDrawable = button.Disabled;
			}
			else if (scrollBarBar.IsActive)
			{
				atlasDrawable = button.Active;
			}
			else if (scrollBarBar.IsHovered)
			{
				atlasDrawable = button.Hovered;
			}
			atlasDrawable.Draw(base.Renderer, scrollBarBar.RenderBounds, scrollBarBar.RenderColor);
		}
		else
		{
			base.Renderer.DrawColor = scrollBarBar.RenderColor;
			_ = scrollBarBar.IsVertical;
			base.Renderer.DrawTexturedRect(image, new Rectangle(scrollBarBar.RenderBounds.X, scrollBarBar.RenderBounds.Y, 2, 2), scrollBarBar.RenderColor, 0f, 0f, 2f / (float)image.Width, 2f / (float)image.Height);
			base.Renderer.DrawTexturedRect(image, new Rectangle(scrollBarBar.RenderBounds.X + 2, scrollBarBar.RenderBounds.Y, scrollBarBar.RenderBounds.Width - 4, 2), scrollBarBar.RenderColor, 2f / (float)image.Width, 0f, ((float)image.Width - 2f) / (float)image.Width, 2f / (float)image.Height);
			base.Renderer.DrawTexturedRect(image, new Rectangle(scrollBarBar.RenderBounds.Right - 2, scrollBarBar.RenderBounds.Y, 2, 2), scrollBarBar.RenderColor, ((float)image.Width - 2f) / (float)image.Width, 0f, 1f, 2f / (float)image.Height);
			base.Renderer.DrawTexturedRect(image, new Rectangle(scrollBarBar.RenderBounds.X, scrollBarBar.RenderBounds.Y + 2, 2, scrollBarBar.RenderBounds.Height - 4), scrollBarBar.RenderColor, 0f, 2f / (float)image.Height, 2f / (float)image.Width, ((float)image.Height - 2f) / (float)image.Height);
			base.Renderer.DrawTexturedRect(image, new Rectangle(scrollBarBar.RenderBounds.X + 2, scrollBarBar.RenderBounds.Y + 2, scrollBarBar.RenderBounds.Width - 4, scrollBarBar.RenderBounds.Height - 4), scrollBarBar.RenderColor, 2f / (float)image.Width, 2f / (float)image.Height, ((float)image.Width - 2f) / (float)image.Width, ((float)image.Height - 2f) / (float)image.Height);
			base.Renderer.DrawTexturedRect(image, new Rectangle(scrollBarBar.RenderBounds.Width - 2, scrollBarBar.RenderBounds.Y + 2, 2, scrollBarBar.RenderBounds.Height - 4), scrollBarBar.RenderColor, ((float)image.Width - 2f) / (float)image.Width, 2f / (float)image.Height, 1f, ((float)image.Height - 2f) / (float)image.Height);
			base.Renderer.DrawTexturedRect(image, new Rectangle(scrollBarBar.RenderBounds.X, scrollBarBar.RenderBounds.Bottom - 2, 2, 2), scrollBarBar.RenderColor, 0f, ((float)image.Height - 2f) / (float)image.Height, 2f / (float)image.Width);
			base.Renderer.DrawTexturedRect(image, new Rectangle(scrollBarBar.RenderBounds.X + 2, scrollBarBar.RenderBounds.Bottom - 2, scrollBarBar.RenderBounds.Width - 4, 2), scrollBarBar.RenderColor, 2f / (float)image.Width, ((float)image.Height - 2f) / (float)image.Height, ((float)image.Width - 2f) / (float)image.Width);
			base.Renderer.DrawTexturedRect(image, new Rectangle(scrollBarBar.RenderBounds.Right - 2, scrollBarBar.RenderBounds.Bottom - 2, 2, 2), scrollBarBar.RenderColor, ((float)image.Width - 2f) / (float)image.Width, ((float)image.Height - 2f) / (float)image.Height);
		}
	}

	public override void DrawProgressBar(Intersect.Client.Framework.Gwen.Control.Base control, bool horizontal, float progress)
	{
		Rectangle renderBounds = control.RenderBounds;
		if (horizontal)
		{
			mTextures.ProgressBar.Back.Draw(base.Renderer, renderBounds, control.RenderColor);
			renderBounds.Width = (int)((float)renderBounds.Width * progress);
			mTextures.ProgressBar.Front.Draw(base.Renderer, renderBounds, control.RenderColor);
		}
		else
		{
			mTextures.ProgressBar.Back.Draw(base.Renderer, renderBounds, control.RenderColor);
			renderBounds.Y = (int)((float)renderBounds.Y + (float)renderBounds.Height * (1f - progress));
			renderBounds.Height = (int)((float)renderBounds.Height * progress);
			mTextures.ProgressBar.Front.Draw(base.Renderer, renderBounds, control.RenderColor);
		}
	}

	public override void DrawListBox(Intersect.Client.Framework.Gwen.Control.Base control)
	{
		if (control.ShouldDrawBackground)
		{
			mTextures.Input.ListBox.Background.Draw(base.Renderer, control.RenderBounds, control.RenderColor);
		}
	}

	public override void DrawListBoxLine(Intersect.Client.Framework.Gwen.Control.Base control, bool selected, bool even)
	{
		if (!control.ShouldDrawBackground)
		{
			return;
		}
		if (control.IsHovered)
		{
			mTextures.Input.ListBox.Hovered.Draw(base.Renderer, control.RenderBounds, control.RenderColor);
		}
		else if (selected)
		{
			if (even)
			{
				mTextures.Input.ListBox.EvenLineSelected.Draw(base.Renderer, control.RenderBounds, control.RenderColor);
			}
			else
			{
				mTextures.Input.ListBox.OddLineSelected.Draw(base.Renderer, control.RenderBounds, control.RenderColor);
			}
		}
		else if (even)
		{
			mTextures.Input.ListBox.EvenLine.Draw(base.Renderer, control.RenderBounds, control.RenderColor);
		}
		else
		{
			mTextures.Input.ListBox.OddLine.Draw(base.Renderer, control.RenderBounds, control.RenderColor);
		}
	}

	public void DrawSliderNotches(Rectangle rect, double[]? notches, int numNotches, float thickness, float notchLength, Orientation orientation)
	{
		if (numNotches == 0 && (notches == null || notches.Length <= 0))
		{
			return;
		}
		int positionLimit = orientation switch
		{
			Orientation.LeftToRight => rect.Width, 
			Orientation.RightToLeft => rect.Width, 
			Orientation.TopToBottom => rect.Height, 
			Orientation.BottomToTop => rect.Height, 
			_ => throw Exceptions.UnreachableInvalidEnum(orientation), 
		};
		Rectangle rectangle = new Rectangle
		{
			X = rect.X,
			Y = rect.Y
		};
		switch (orientation)
		{
		case Orientation.LeftToRight:
		case Orientation.RightToLeft:
			rectangle.Width = (int)thickness;
			rectangle.Height = (int)(notchLength + thickness);
			break;
		case Orientation.TopToBottom:
		case Orientation.BottomToTop:
			rectangle.Width = (int)(notchLength + thickness);
			rectangle.Height = (int)thickness;
			break;
		default:
			throw Exceptions.UnreachableInvalidEnum(orientation);
		}
		if (notches == null)
		{
			float num = (float)positionLimit / (float)numNotches;
			for (int i = 0; i <= numNotches; i++)
			{
				Intersect.Client.Framework.Gwen.Renderer.Base renderer = base.Renderer;
				Rectangle rect2;
				if (!((uint)orientation <= 1u))
				{
					Rectangle rectangle2 = rectangle;
					rectangle2.Y = (int)((float)rectangle.Y + num * (float)i);
					rect2 = rectangle2;
				}
				else
				{
					Rectangle rectangle2 = rectangle;
					rectangle2.X = (int)((float)rectangle.X + num * (float)i);
					rect2 = rectangle2;
				}
				renderer.DrawFilledRect(rect2);
			}
			return;
		}
		double notchMin = notches.Min();
		double num2 = notches.Max();
		double notchRange = num2 - notchMin;
		if (notchRange == 0.0)
		{
			notchRange = 1.0;
		}
		double[] array = notches.Select((double notch) => (double)positionLimit * (notch - notchMin) / notchRange).ToArray();
		double[] array2 = array;
		foreach (double num4 in array2)
		{
			Rectangle rectangle2;
			switch (orientation)
			{
			case Orientation.LeftToRight:
			{
				Rectangle rectangle3 = rectangle;
				rectangle3.X = (int)((double)rectangle.X + num4);
				rectangle2 = rectangle3;
				break;
			}
			case Orientation.RightToLeft:
			{
				Rectangle rectangle3 = rectangle;
				rectangle3.X = (int)((double)(rectangle.X + positionLimit) - num4);
				rectangle2 = rectangle3;
				break;
			}
			case Orientation.TopToBottom:
			{
				Rectangle rectangle3 = rectangle;
				rectangle3.Y = (int)((double)rectangle.Y + num4);
				rectangle2 = rectangle3;
				break;
			}
			case Orientation.BottomToTop:
			{
				Rectangle rectangle3 = rectangle;
				rectangle3.Y = (int)((double)(rectangle.Y + positionLimit) - num4);
				rectangle2 = rectangle3;
				break;
			}
			default:
				throw Exceptions.UnreachableInvalidEnum(orientation);
			}
			Rectangle rect3 = rectangle2;
			base.Renderer.DrawFilledRect(rect3);
		}
	}

	public void DrawSliderNotchesH(Rectangle rect, double[] notches, int numNotches, float thickness, float notchLength)
	{
		if (numNotches == 0)
		{
			return;
		}
		double? notchMin = notches?.Min();
		double? notchRange = notches?.Max() - notchMin;
		if (notchRange == 0.0)
		{
			notchRange = 1.0;
		}
		double[] array = notches?.Select((double notch) => ((double)rect.Width * (notch - notchMin) / notchRange).Value).ToArray();
		if (array != null)
		{
			double[] array2 = array;
			foreach (double num2 in array2)
			{
				base.Renderer.DrawFilledRect(Util.FloatRect((float)rect.X + (float)num2, rect.Y, thickness, notchLength + thickness));
			}
		}
		else
		{
			float num3 = (float)rect.Width / (float)numNotches;
			for (int num4 = 0; num4 < numNotches + 1; num4++)
			{
				base.Renderer.DrawFilledRect(Util.FloatRect((float)rect.X + num3 * (float)num4, rect.Y, thickness, notchLength + thickness));
			}
		}
	}

	public void DrawSliderNotchesV(Rectangle rect, int numNotches, float dist)
	{
		if (numNotches != 0)
		{
			float num = (float)rect.Height / (float)numNotches;
			for (int i = 0; i < numNotches + 1; i++)
			{
				base.Renderer.DrawFilledRect(Util.FloatRect((float)rect.X + dist - 2f, (float)rect.Y + num * (float)i, 5f, 1f));
			}
		}
	}

	public override void DrawSlider(Intersect.Client.Framework.Gwen.Control.Base control, bool horizontal, double[]? notches, int numNotches, int barSize)
	{
		if (!(control is Slider slider))
		{
			return;
		}
		Rectangle renderBounds = control.RenderBounds;
		IGameTexture backgroundImage = slider.BackgroundImage;
		if (backgroundImage != null)
		{
			base.Renderer.DrawColor = control.RenderColor;
			base.Renderer.DrawTexturedRect(backgroundImage, renderBounds, control.RenderColor);
			if (control.DrawDebugOutlines)
			{
				DrawRectStroke(renderBounds, Color.Yellow);
			}
			return;
		}
		base.Renderer.DrawColor = (control.IsDisabled ? Colors.Button.Disabled : control.RenderColor);
		if (horizontal)
		{
			renderBounds.X += (int)((double)barSize * 0.5);
			renderBounds.Width -= barSize;
			renderBounds.Y += (int)((double)renderBounds.Height * 0.5 - 1.0);
			renderBounds.Height = 1;
			DrawSliderNotchesH(renderBounds, notches, numNotches, renderBounds.Height, (float)barSize * 0.5f);
		}
		else
		{
			renderBounds.Y += (int)((double)barSize * 0.5);
			renderBounds.Height -= barSize;
			renderBounds.X += (int)((double)renderBounds.Width * 0.5 - 1.0);
			renderBounds.Width = 1;
			DrawSliderNotchesV(renderBounds, numNotches, (float)barSize * 0.4f);
		}
		base.Renderer.DrawFilledRect(renderBounds);
	}

	public override void DrawSlider(Slider slider, Orientation orientation, double[]? notches, int numNotches, int barSize)
	{
		Rectangle renderBounds = slider.RenderBounds;
		Color color = slider.RenderColor;
		IGameTexture backgroundImage = slider.BackgroundImage;
		if (backgroundImage != null)
		{
			base.Renderer.DrawColor = color;
			base.Renderer.DrawTexturedRect(backgroundImage, renderBounds, color);
			if (slider.DrawDebugOutlines)
			{
				DrawRectStroke(renderBounds, Color.Yellow);
			}
			return;
		}
		if (slider.IsDisabledByTree)
		{
			color = Colors.Button.Disabled;
		}
		base.Renderer.DrawColor = color;
		float thickness;
		switch (orientation)
		{
		case Orientation.LeftToRight:
		case Orientation.RightToLeft:
			renderBounds.X += (int)((double)barSize * 0.5);
			renderBounds.Width -= barSize;
			renderBounds.Y += (int)((double)renderBounds.Height * 0.5 - 1.0);
			renderBounds.Height = 1;
			thickness = renderBounds.Height;
			break;
		case Orientation.TopToBottom:
		case Orientation.BottomToTop:
			renderBounds.Y += (int)((double)barSize * 0.5);
			renderBounds.Height -= barSize;
			renderBounds.X += (int)((double)renderBounds.Width * 0.5 - 1.0);
			renderBounds.Width = 1;
			thickness = renderBounds.Width;
			break;
		default:
			throw Exceptions.UnreachableInvalidEnum(orientation);
		}
		DrawSliderNotches(renderBounds, notches, numNotches, thickness, (float)barSize * 0.5f, orientation);
		base.Renderer.DrawFilledRect(renderBounds);
	}

	public override void DrawComboBox(Intersect.Client.Framework.Gwen.Control.Base control, bool down, bool open)
	{
		if (control.ShouldDrawBackground)
		{
			if (control.IsDisabledByTree)
			{
				mTextures.Input.ComboBox.Disabled.Draw(base.Renderer, control.RenderBounds, control.RenderColor);
			}
			else if (down || open)
			{
				mTextures.Input.ComboBox.Down.Draw(base.Renderer, control.RenderBounds, control.RenderColor);
			}
			else if (control.IsHovered)
			{
				mTextures.Input.ComboBox.Down.Draw(base.Renderer, control.RenderBounds, control.RenderColor);
			}
			else
			{
				mTextures.Input.ComboBox.Normal.Draw(base.Renderer, control.RenderBounds, control.RenderColor);
			}
		}
	}

	public override void DrawKeyboardHighlight(Intersect.Client.Framework.Gwen.Control.Base control, Rectangle r, int offset)
	{
		Rectangle rectangle = r;
		rectangle.X += offset;
		rectangle.Y += offset;
		rectangle.Width -= offset * 2;
		rectangle.Height -= offset * 2;
		bool flag = true;
		for (int i = 0; (double)i < (double)rectangle.Width * 0.5; i++)
		{
			mRenderer.DrawColor = Color.Black;
			if (!flag)
			{
				base.Renderer.DrawPixel(rectangle.X + i * 2, rectangle.Y);
				base.Renderer.DrawPixel(rectangle.X + i * 2, rectangle.Y + rectangle.Height - 1);
			}
			else
			{
				flag = false;
			}
		}
		for (int j = 0; (double)j < (double)rectangle.Height * 0.5; j++)
		{
			base.Renderer.DrawColor = Color.Black;
			base.Renderer.DrawPixel(rectangle.X, rectangle.Y + j * 2);
			base.Renderer.DrawPixel(rectangle.X + rectangle.Width - 1, rectangle.Y + j * 2);
		}
	}

	public override void DrawToolTip(Intersect.Client.Framework.Gwen.Control.Base control)
	{
		if (control is Label { ToolTipBackground: not null } label)
		{
			IGameTexture toolTipBackground = label.ToolTipBackground;
			base.Renderer.DrawTexturedRect(toolTipBackground, new Rectangle(control.RenderBounds.X, control.RenderBounds.Y, 8, 8), control.RenderColor, 0f, 0f, 8f / (float)toolTipBackground.Width, 8f / (float)toolTipBackground.Height);
			base.Renderer.DrawTexturedRect(toolTipBackground, new Rectangle(control.RenderBounds.X + 8, control.RenderBounds.Y, control.RenderBounds.Width - 16, 8), control.RenderColor, 8f / (float)toolTipBackground.Width, 0f, ((float)toolTipBackground.Width - 8f) / (float)toolTipBackground.Width, 8f / (float)toolTipBackground.Height);
			base.Renderer.DrawTexturedRect(toolTipBackground, new Rectangle(control.RenderBounds.Right - 8, control.RenderBounds.Y, 8, 8), control.RenderColor, ((float)toolTipBackground.Width - 8f) / (float)toolTipBackground.Width, 0f, 1f, 8f / (float)toolTipBackground.Height);
			base.Renderer.DrawTexturedRect(toolTipBackground, new Rectangle(control.RenderBounds.X, control.RenderBounds.Y + 8, 8, control.RenderBounds.Height - 16), control.RenderColor, 0f, 8f / (float)toolTipBackground.Height, 8f / (float)toolTipBackground.Width, ((float)toolTipBackground.Height - 8f) / (float)toolTipBackground.Height);
			base.Renderer.DrawTexturedRect(toolTipBackground, new Rectangle(control.RenderBounds.X + 8, control.RenderBounds.Y + 8, control.RenderBounds.Width - 16, control.RenderBounds.Height - 16), control.RenderColor, 8f / (float)toolTipBackground.Width, 8f / (float)toolTipBackground.Height, ((float)toolTipBackground.Width - 8f) / (float)toolTipBackground.Width, ((float)toolTipBackground.Height - 8f) / (float)toolTipBackground.Height);
			base.Renderer.DrawTexturedRect(toolTipBackground, new Rectangle(control.RenderBounds.Width - 8, control.RenderBounds.Y + 8, 8, control.RenderBounds.Height - 16), control.RenderColor, ((float)toolTipBackground.Width - 8f) / (float)toolTipBackground.Width, 8f / (float)toolTipBackground.Height, 1f, ((float)toolTipBackground.Height - 8f) / (float)toolTipBackground.Height);
			base.Renderer.DrawTexturedRect(toolTipBackground, new Rectangle(control.RenderBounds.X, control.RenderBounds.Bottom - 8, 8, 8), control.RenderColor, 0f, ((float)toolTipBackground.Height - 8f) / (float)toolTipBackground.Height, 8f / (float)toolTipBackground.Width);
			base.Renderer.DrawTexturedRect(toolTipBackground, new Rectangle(control.RenderBounds.X + 8, control.RenderBounds.Bottom - 8, control.RenderBounds.Width - 16, 8), control.RenderColor, 8f / (float)toolTipBackground.Width, ((float)toolTipBackground.Height - 8f) / (float)toolTipBackground.Height, ((float)toolTipBackground.Width - 8f) / (float)toolTipBackground.Width);
			base.Renderer.DrawTexturedRect(toolTipBackground, new Rectangle(control.RenderBounds.Right - 8, control.RenderBounds.Bottom - 8, 8, 8), control.RenderColor, ((float)toolTipBackground.Width - 8f) / (float)toolTipBackground.Width, ((float)toolTipBackground.Height - 8f) / (float)toolTipBackground.Height);
		}
		else
		{
			mTextures.Tooltip.Draw(base.Renderer, control.RenderBounds, control.RenderColor);
		}
	}

	public override void DrawScrollButton(Intersect.Client.Framework.Gwen.Control.Base control, Pos direction, bool depressed, bool hovered, bool disabled)
	{
		if (control is Button button)
		{
			int num = 0;
			if (direction == Pos.Top)
			{
				num = 1;
			}
			if (direction == Pos.Right)
			{
				num = 2;
			}
			if (direction == Pos.Bottom)
			{
				num = 3;
			}
			IGameTexture gameTexture = null;
			if (disabled && button.GetStateTexture(ComponentState.Disabled) != null)
			{
				gameTexture = button.GetStateTexture(ComponentState.Disabled);
			}
			else if (depressed && button.GetStateTexture(ComponentState.Active) != null)
			{
				gameTexture = button.GetStateTexture(ComponentState.Active);
			}
			else if (hovered && button.GetStateTexture(ComponentState.Hovered) != null)
			{
				gameTexture = button.GetStateTexture(ComponentState.Hovered);
			}
			else if (button.GetStateTexture(ComponentState.Normal) != null)
			{
				gameTexture = button.GetStateTexture(ComponentState.Normal);
			}
			if (gameTexture != null)
			{
				base.Renderer.DrawColor = button.RenderColor;
				base.Renderer.DrawTexturedRect(gameTexture, button.RenderBounds, button.RenderColor);
			}
			else if (disabled)
			{
				mTextures.Scroller.Button.Disabled[num].Draw(base.Renderer, button.RenderBounds, button.RenderColor);
			}
			else if (depressed)
			{
				mTextures.Scroller.Button.Active[num].Draw(base.Renderer, button.RenderBounds, button.RenderColor);
			}
			else if (hovered)
			{
				mTextures.Scroller.Button.Hovered[num].Draw(base.Renderer, button.RenderBounds, button.RenderColor);
			}
			else
			{
				mTextures.Scroller.Button.Normal[num].Draw(base.Renderer, button.RenderBounds, button.RenderColor);
			}
		}
	}

	public override void DrawComboBoxArrow(Intersect.Client.Framework.Gwen.Control.Base control, bool hovered, bool down, bool open, bool disabled)
	{
		if (disabled)
		{
			mTextures.Input.ComboBox.Button.Disabled.Draw(base.Renderer, control.RenderBounds, control.RenderColor);
		}
		else if (down || open)
		{
			mTextures.Input.ComboBox.Button.Down.Draw(base.Renderer, control.RenderBounds, control.RenderColor);
		}
		else if (hovered)
		{
			mTextures.Input.ComboBox.Button.Hover.Draw(base.Renderer, control.RenderBounds, control.RenderColor);
		}
		else
		{
			mTextures.Input.ComboBox.Button.Normal.Draw(base.Renderer, control.RenderBounds);
		}
	}

	public override void DrawNumericUpDownButton(Intersect.Client.Framework.Gwen.Control.Base control, bool depressed, bool up)
	{
		if (up)
		{
			if (control.IsDisabledByTree)
			{
				mTextures.Input.UpDown.Up.Disabled.DrawCenter(base.Renderer, control.RenderBounds, control.RenderColor);
			}
			else if (depressed)
			{
				mTextures.Input.UpDown.Up.Down.DrawCenter(base.Renderer, control.RenderBounds, control.RenderColor);
			}
			else if (control.IsHovered)
			{
				mTextures.Input.UpDown.Up.Hover.DrawCenter(base.Renderer, control.RenderBounds, control.RenderColor);
			}
			else
			{
				mTextures.Input.UpDown.Up.Normal.DrawCenter(base.Renderer, control.RenderBounds, control.RenderColor);
			}
		}
		else if (control.IsDisabledByTree)
		{
			mTextures.Input.UpDown.Down.Disabled.DrawCenter(base.Renderer, control.RenderBounds, control.RenderColor);
		}
		else if (depressed)
		{
			mTextures.Input.UpDown.Down.Down.DrawCenter(base.Renderer, control.RenderBounds, control.RenderColor);
		}
		else if (control.IsHovered)
		{
			mTextures.Input.UpDown.Down.Hover.DrawCenter(base.Renderer, control.RenderBounds, control.RenderColor);
		}
		else
		{
			mTextures.Input.UpDown.Down.Normal.DrawCenter(base.Renderer, control.RenderBounds, control.RenderColor);
		}
	}

	public override void DrawStatusBar(Intersect.Client.Framework.Gwen.Control.Base control)
	{
		mTextures.StatusBar.Draw(base.Renderer, control.RenderBounds, control.RenderColor);
	}

	public override void DrawTreeButton(Intersect.Client.Framework.Gwen.Control.Base control, bool open)
	{
		Rectangle renderBounds = control.RenderBounds;
		if (open)
		{
			mTextures.Tree.Minus.Draw(base.Renderer, renderBounds);
		}
		else
		{
			mTextures.Tree.Plus.Draw(base.Renderer, renderBounds);
		}
	}

	public override void DrawTreeControl(Intersect.Client.Framework.Gwen.Control.Base control)
	{
		mTextures.Tree.Background.Draw(base.Renderer, control.RenderBounds, control.RenderColor);
	}

	public override void DrawTreeNode(Intersect.Client.Framework.Gwen.Control.Base ctrl, bool open, bool selected, int treeNodeHeight, int labelHeight, int labelWidth, int halfWay, int lastBranch, bool isRoot)
	{
		if (selected)
		{
			mTextures.Selection.Draw(base.Renderer, new Rectangle(16, 0, labelWidth + 4, labelHeight), ctrl.RenderColor);
		}
		base.DrawTreeNode(ctrl, open, selected, treeNodeHeight, labelHeight, labelWidth, halfWay, lastBranch, isRoot);
	}

	public override void DrawColorDisplay(Intersect.Client.Framework.Gwen.Control.Base control, Color color)
	{
		Rectangle renderBounds = control.RenderBounds;
		if (color.A != byte.MaxValue)
		{
			base.Renderer.DrawColor = Color.FromArgb(255, 255, 255, 255);
			base.Renderer.DrawFilledRect(renderBounds);
			base.Renderer.DrawColor = Color.FromArgb(128, 128, 128, 128);
			base.Renderer.DrawFilledRect(Util.FloatRect(0f, 0f, (float)renderBounds.Width * 0.5f, (float)renderBounds.Height * 0.5f));
			base.Renderer.DrawFilledRect(Util.FloatRect((float)renderBounds.Width * 0.5f, (float)renderBounds.Height * 0.5f, (float)renderBounds.Width * 0.5f, (float)renderBounds.Height * 0.5f));
		}
		base.Renderer.DrawColor = color;
		base.Renderer.DrawFilledRect(renderBounds);
		base.Renderer.DrawColor = Color.Black;
		base.Renderer.DrawLinedRect(renderBounds);
	}

	public override void DrawModalControl(Intersect.Client.Framework.Gwen.Control.Base control)
	{
		if (control.ShouldDrawBackground)
		{
			Rectangle renderBounds = control.RenderBounds;
			base.Renderer.DrawColor = new Color(200, 20, 20, 20);
			base.Renderer.DrawFilledRect(renderBounds);
		}
	}

	public override void DrawMenuDivider(Intersect.Client.Framework.Gwen.Control.Base control)
	{
		Rectangle renderBounds = control.RenderBounds;
		base.Renderer.DrawColor = Color.FromArgb(100, 0, 0, 0);
		base.Renderer.DrawFilledRect(renderBounds);
	}

	public override void DrawSliderButton(SliderBar sliderBar)
	{
		IGameTexture image = sliderBar.GetImage(ComponentState.Normal);
		if (sliderBar.IsDisabledByTree)
		{
			image = sliderBar.GetImage(ComponentState.Disabled);
		}
		else if (sliderBar.IsActive)
		{
			image = sliderBar.GetImage(ComponentState.Active);
		}
		else if (sliderBar.IsHovered)
		{
			image = sliderBar.GetImage(ComponentState.Hovered);
		}
		if (image != null)
		{
			base.Renderer.DrawColor = sliderBar.RenderColor;
			base.Renderer.DrawTexturedRect(image, sliderBar.RenderBounds, sliderBar.RenderColor);
			return;
		}
		Orientation orientation = sliderBar.Orientation;
		bool flag = (uint)orientation <= 1u;
		SkinTextures._Input._Slider._SliderSource sliderSource = (flag ? mTextures.Input.Slider.H : mTextures.Input.Slider.V);
		Bordered bordered = sliderSource.Normal;
		if (sliderBar.IsDisabledByTree)
		{
			bordered = sliderSource.Disabled;
		}
		else if (sliderBar.IsActive)
		{
			bordered = sliderSource.Active;
		}
		else if (sliderBar.IsHovered)
		{
			bordered = sliderSource.Hover;
		}
		bordered.Draw(base.Renderer, sliderBar.RenderBounds, sliderBar.RenderColor);
	}

	public override void DrawCategoryHolder(Intersect.Client.Framework.Gwen.Control.Base control)
	{
		mTextures.CategoryList.Outer.Draw(base.Renderer, control.RenderBounds, control.RenderColor);
	}

	public override void DrawCategoryInner(Intersect.Client.Framework.Gwen.Control.Base control, bool collapsed)
	{
		if (collapsed)
		{
			mTextures.CategoryList.Header.Draw(base.Renderer, control.RenderBounds, control.RenderColor);
		}
		else
		{
			mTextures.CategoryList.Inner.Draw(base.Renderer, control.RenderBounds, control.RenderColor);
		}
	}

	public override void DrawLabel(Intersect.Client.Framework.Gwen.Control.Base control)
	{
		if (control is Label label)
		{
			IGameTexture template = label.GetTemplate();
			if (template != null)
			{
				base.Renderer.DrawColor = control.RenderColor;
				base.Renderer.DrawTexturedRect(template, new Rectangle(control.RenderBounds.X, control.RenderBounds.Y, 2, 2), control.RenderColor, 0f, 0f, 2f / (float)template.Width, 2f / (float)template.Height);
				base.Renderer.DrawTexturedRect(template, new Rectangle(control.RenderBounds.X + 2, control.RenderBounds.Y, control.RenderBounds.Width - 4, 2), control.RenderColor, 2f / (float)template.Width, 0f, ((float)template.Width - 2f) / (float)template.Width, 2f / (float)template.Height);
				base.Renderer.DrawTexturedRect(template, new Rectangle(control.RenderBounds.Right - 2, control.RenderBounds.Y, 2, 2), control.RenderColor, ((float)template.Width - 2f) / (float)template.Width, 0f, 1f, 2f / (float)template.Height);
				base.Renderer.DrawTexturedRect(template, new Rectangle(control.RenderBounds.X, control.RenderBounds.Y + 2, 2, control.RenderBounds.Height - 4), control.RenderColor, 0f, 2f / (float)template.Height, 2f / (float)template.Width, ((float)template.Height - 2f) / (float)template.Height);
				base.Renderer.DrawTexturedRect(template, new Rectangle(control.RenderBounds.X + 2, control.RenderBounds.Y + 2, control.RenderBounds.Width - 4, control.RenderBounds.Height - 4), control.RenderColor, 2f / (float)template.Width, 2f / (float)template.Height, ((float)template.Width - 2f) / (float)template.Width, ((float)template.Height - 2f) / (float)template.Height);
				base.Renderer.DrawTexturedRect(template, new Rectangle(control.RenderBounds.Width - 2, control.RenderBounds.Y + 2, 2, control.RenderBounds.Height - 4), control.RenderColor, ((float)template.Width - 2f) / (float)template.Width, 2f / (float)template.Height, 1f, ((float)template.Height - 2f) / (float)template.Height);
				base.Renderer.DrawTexturedRect(template, new Rectangle(control.RenderBounds.X, control.RenderBounds.Bottom - 2, 2, 2), control.RenderColor, 0f, ((float)template.Height - 2f) / (float)template.Height, 2f / (float)template.Width);
				base.Renderer.DrawTexturedRect(template, new Rectangle(control.RenderBounds.X + 2, control.RenderBounds.Bottom - 2, control.RenderBounds.Width - 4, 2), control.RenderColor, 2f / (float)template.Width, ((float)template.Height - 2f) / (float)template.Height, ((float)template.Width - 2f) / (float)template.Width);
				base.Renderer.DrawTexturedRect(template, new Rectangle(control.RenderBounds.Right - 2, control.RenderBounds.Bottom - 2, 2, 2), control.RenderColor, ((float)template.Width - 2f) / (float)template.Width, ((float)template.Height - 2f) / (float)template.Height);
			}
		}
	}
}
