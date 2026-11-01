using System.Linq;
using System.Resources;
using Intersect.Client.Framework.Content;
using Intersect.Client.Framework.File_Management;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.Framework.Gwen.Renderer;
using Intersect.Client.Framework.Gwen.Skin.Texturing;
using Intersect.Core;
using Microsoft.Extensions.Logging;

namespace Intersect.Client.Framework.Gwen.Skin;

public class IntersectSkin : TexturedBase
{
	private static IGameTexture LoadEmbeddedSkinTexture(GameContentManager contentManager)
	{
		string skinResourceName = typeof(IntersectSkin).Namespace + ".skin-intersect.png";
		string[] manifestResourceNames = typeof(IntersectSkin).Assembly.GetManifestResourceNames();
		if (!manifestResourceNames.Contains(skinResourceName))
		{
			throw new MissingManifestResourceException("Missing embedded Intersect GWEN skin");
		}
		IGameTexture texture = contentManager.GetTexture(TextureType.Gui, "skin-intersect.png");
		if (texture != null)
		{
			ApplicationContext.Context.Value?.Logger.LogDebug("{SkinName} was found on disk, not using embedded version", "skin-intersect.png");
			return texture;
		}
		ApplicationContext.Context.Value?.Logger.LogDebug("{SkinName} was not found on disk, using embedded version", "skin-intersect.png");
		return contentManager.Load<IGameTexture>(ContentType.Interface, "skin-intersect.png", () => typeof(IntersectSkin).Assembly.GetManifestResourceStream(skinResourceName) ?? throw new MissingManifestResourceException("Missing '" + skinResourceName));
	}

	public IntersectSkin(Intersect.Client.Framework.Gwen.Renderer.Base renderer, GameContentManager contentManager)
		: base(renderer, LoadEmbeddedSkinTexture(contentManager))
	{
	}

	public IntersectSkin(Intersect.Client.Framework.Gwen.Renderer.Base renderer, GameContentManager contentManager, string textureName)
		: base(renderer, contentManager, textureName)
	{
	}

	public override void Dispose()
	{
		base.Dispose();
	}

	protected override void InitializeColors()
	{
		base.InitializeColors();
		Colors.Button.Normal = base.Renderer.PixelColor(_texture, 0u, 480u, Color.Yellow);
		Colors.Button.Disabled = base.Renderer.PixelColor(_texture, 8u, 480u, Color.Yellow);
		Colors.Button.Hover = base.Renderer.PixelColor(_texture, 16u, 480u, Color.Yellow);
		Colors.Button.Active = base.Renderer.PixelColor(_texture, 24u, 480u, Color.Yellow);
		Colors.Label.Normal = base.Renderer.PixelColor(_texture, 32u, 480u, Color.Yellow);
		Colors.Label.Disabled = base.Renderer.PixelColor(_texture, 40u, 480u, Color.Yellow);
		Colors.Label.Hover = base.Renderer.PixelColor(_texture, 48u, 480u, Color.Yellow);
		Colors.Label.Active = base.Renderer.PixelColor(_texture, 56u, 480u, Color.Yellow);
		Colors.Tab.Active.Normal = base.Renderer.PixelColor(_texture, 0u, 480u, Color.Yellow);
		Colors.Tab.Active.Disabled = base.Renderer.PixelColor(_texture, 8u, 480u, Color.Yellow);
		Colors.Tab.Active.Hover = base.Renderer.PixelColor(_texture, 16u, 480u, Color.Yellow);
		Colors.Tab.Active.Active = base.Renderer.PixelColor(_texture, 24u, 480u, Color.Yellow);
		Colors.Tab.Inactive.Normal = base.Renderer.PixelColor(_texture, 0u, 480u, Color.Yellow);
		Colors.Tab.Inactive.Disabled = base.Renderer.PixelColor(_texture, 8u, 480u, Color.Yellow);
		Colors.Tab.Inactive.Hover = base.Renderer.PixelColor(_texture, 16u, 480u, Color.Yellow);
		Colors.Tab.Inactive.Active = base.Renderer.PixelColor(_texture, 24u, 480u, Color.Yellow);
		Colors.Tree.Lines = base.Renderer.PixelColor(_texture, 64u, 480u, Color.Yellow);
		Colors.Tree.Normal = base.Renderer.PixelColor(_texture, 72u, 480u, Color.Yellow);
		Colors.Tree.Hover = base.Renderer.PixelColor(_texture, 80u, 480u, Color.Yellow);
		Colors.Tree.Selected = base.Renderer.PixelColor(_texture, 88u, 480u, Color.Yellow);
	}

	protected override void InitializeTextures()
	{
		base.InitializeTextures();
		mTextures.CheckBox.Default.Box = new Single(_texture, 84f, 0f, 22f, 22f);
		mTextures.CheckBox.Default.Fill = new Single(_texture, 106f, 0f, 22f, 22f);
		mTextures.CheckBox.Active.Box = new Single(_texture, 84f, 22f, 22f, 22f);
		mTextures.CheckBox.Active.Fill = new Single(_texture, 106f, 22f, 22f, 22f);
		mTextures.CheckBox.Hovered.Box = new Single(_texture, 84f, 44f, 22f, 22f);
		mTextures.CheckBox.Hovered.Fill = new Single(_texture, 106f, 44f, 22f, 22f);
		mTextures.CheckBox.Disabled.Box = new Single(_texture, 84f, 66f, 22f, 22f);
		mTextures.CheckBox.Disabled.Fill = new Single(_texture, 106f, 66f, 22f, 22f);
		mTextures.Input.Button.Normal = new Bordered(_texture, 48f, 0f, 12f, 12f, Margin.Four);
		mTextures.Input.Button.Disabled = new Bordered(_texture, 48f, 12f, 12f, 12f, Margin.Four);
		mTextures.Input.Button.Hovered = new Bordered(_texture, 48f, 24f, 12f, 12f, Margin.Four);
		mTextures.Input.Button.Active = new Bordered(_texture, 48f, 36f, 12f, 12f, Margin.Four);
		mTextures.Input.ComboBox.Normal = new Bordered(_texture, 385f, 336f, 126f, 31f, new Margin(8, 8, 32, 8));
		mTextures.Input.ComboBox.Hover = new Bordered(_texture, 385f, 368f, 126f, 31f, new Margin(8, 8, 32, 8));
		mTextures.Input.ComboBox.Down = new Bordered(_texture, 385f, 400f, 126f, 31f, new Margin(8, 8, 32, 8));
		mTextures.Input.ComboBox.Disabled = new Bordered(_texture, 385f, 432f, 126f, 31f, new Margin(8, 8, 32, 8));
		mTextures.Input.Slider.H.Normal = new Bordered(_texture, 176f, 0f, 7f, 7f, new Margin(3, 2, 3, 4));
		mTextures.Input.Slider.H.Disabled = new Bordered(_texture, 176f, 8f, 7f, 7f, new Margin(3, 2, 3, 4));
		mTextures.Input.Slider.H.Hover = new Bordered(_texture, 176f, 16f, 7f, 7f, new Margin(3, 2, 3, 4));
		mTextures.Input.Slider.H.Active = new Bordered(_texture, 176f, 24f, 7f, 7f, new Margin(3, 2, 3, 4));
		mTextures.Input.Slider.V.Normal = new Bordered(_texture, 192f, 0f, 7f, 7f, new Margin(3, 2, 3, 4));
		mTextures.Input.Slider.V.Disabled = new Bordered(_texture, 192f, 8f, 7f, 7f, new Margin(3, 2, 3, 4));
		mTextures.Input.Slider.V.Hover = new Bordered(_texture, 192f, 16f, 7f, 7f, new Margin(3, 2, 3, 4));
		mTextures.Input.Slider.V.Active = new Bordered(_texture, 192f, 24f, 7f, 7f, new Margin(3, 2, 3, 4));
		mTextures.Menu.Strip = new Bordered(_texture, 0f, 128f, 127f, 21f, Margin.One);
		mTextures.Menu.BackgroundWithMargin = new Bordered(_texture, 96f, 96f, 32f, 16f, new Margin(24, 4, 4, 4));
		mTextures.Menu.Background = new Bordered(_texture, 96f, 112f, 16f, 16f, Margin.Four);
		mTextures.Menu.Hovered = new Bordered(_texture, 320f, 320f, 32f, 32f, Margin.Six);
		mTextures.Menu.RightArrow = new Single(_texture, 464f, 112f, 15f, 15f);
		mTextures.Menu.Check = new Single(_texture, 448f, 112f, 15f, 15f);
		mTextures.Panel.Control = new Bordered(_texture, 64f, 112f, 16f, 16f, Margin.Four);
		mTextures.Panel.Normal = new Bordered(_texture, 32f, 0f, 16f, 16f, Margin.Four);
		mTextures.Panel.Highlight = new Bordered(_texture, 32f, 16f, 16f, 16f, Margin.Four);
		mTextures.Panel.Bright = new Bordered(_texture, 32f, 32f, 16f, 16f, Margin.Four);
		mTextures.Panel.Dark = new Bordered(_texture, 32f, 48f, 16f, 16f, Margin.Four);
		mTextures.RadioButton.Default.Box = new Single(_texture, 128f, 0f, 22f, 22f);
		mTextures.RadioButton.Default.Fill = new Single(_texture, 150f, 0f, 22f, 22f);
		mTextures.RadioButton.Active.Box = new Single(_texture, 128f, 22f, 22f, 22f);
		mTextures.RadioButton.Active.Fill = new Single(_texture, 150f, 22f, 22f, 22f);
		mTextures.RadioButton.Hovered.Box = new Single(_texture, 128f, 44f, 22f, 22f);
		mTextures.RadioButton.Hovered.Fill = new Single(_texture, 150f, 44f, 22f, 22f);
		mTextures.RadioButton.Disabled.Box = new Single(_texture, 128f, 66f, 22f, 22f);
		mTextures.RadioButton.Disabled.Fill = new Single(_texture, 150f, 66f, 22f, 22f);
		mTextures.Scroller.TrackV = new Bordered(_texture, 176f, 32f, 15f, 15f, Margin.Four);
		mTextures.Scroller.BarV.Normal = new Bordered(_texture, 192f, 32f, 15f, 15f, Margin.Four);
		mTextures.Scroller.BarV.Disabled = new Bordered(_texture, 208f, 32f, 15f, 15f, Margin.Four);
		mTextures.Scroller.BarV.Hovered = new Bordered(_texture, 224f, 32f, 15f, 15f, Margin.Four);
		mTextures.Scroller.BarV.Active = new Bordered(_texture, 240f, 32f, 15f, 15f, Margin.Four);
		mTextures.Scroller.TrackH = new Bordered(_texture, 176f, 80f, 15f, 15f, Margin.Four);
		mTextures.Scroller.BarH.Normal = new Bordered(_texture, 192f, 80f, 15f, 15f, Margin.Four);
		mTextures.Scroller.BarH.Disabled = new Bordered(_texture, 208f, 80f, 15f, 15f, Margin.Four);
		mTextures.Scroller.BarH.Hovered = new Bordered(_texture, 224f, 80f, 15f, 15f, Margin.Four);
		mTextures.Scroller.BarH.Active = new Bordered(_texture, 240f, 80f, 15f, 15f, Margin.Four);
		int[] source = new int[4] { 96, 48, 112, 64 };
		mTextures.Scroller.Button.Normal = source.Select((int y) => new Bordered(_texture, 192f, y, 15f, 15f, Margin.Four)).ToArray();
		mTextures.Scroller.Button.Disabled = source.Select((int y) => new Bordered(_texture, 208f, y, 15f, 15f, Margin.Four)).ToArray();
		mTextures.Scroller.Button.Hovered = source.Select((int y) => new Bordered(_texture, 224f, y, 15f, 15f, Margin.Four)).ToArray();
		mTextures.Scroller.Button.Active = source.Select((int y) => new Bordered(_texture, 240f, y, 15f, 15f, Margin.Four)).ToArray();
		mTextures.Tab.Control = new Bordered(_texture, 0f, 48f, 16f, 16f, Margin.Four);
		mTextures.Tab.Top.Active = new Bordered(_texture, 0f, 64f, 16f, 10f, new Margin(4, 4, 4, 2));
		mTextures.Tab.Top.Inactive = new Bordered(_texture, 16f, 64f, 16f, 10f, new Margin(4, 4, 4, 2));
		mTextures.Tab.Bottom.Active = new Bordered(_texture, 0f, 80f, 16f, 10f, new Margin(4, 2, 4, 4));
		mTextures.Tab.Bottom.Inactive = new Bordered(_texture, 16f, 80f, 16f, 10f, new Margin(4, 2, 4, 4));
		mTextures.Tab.Right.Active = new Bordered(_texture, 0f, 96f, 10f, 16f, new Margin(2, 4, 4, 4));
		mTextures.Tab.Right.Inactive = new Bordered(_texture, 16f, 96f, 10f, 16f, new Margin(2, 4, 4, 4));
		mTextures.Tab.Left.Active = new Bordered(_texture, 0f, 112f, 10f, 16f, new Margin(4, 4, 2, 4));
		mTextures.Tab.Left.Inactive = new Bordered(_texture, 16f, 112f, 10f, 16f, new Margin(4, 4, 2, 4));
		mTextures.TextBox.Normal = new Bordered(_texture, 32f, 64f, 16f, 16f, Margin.Four);
		mTextures.TextBox.Focus = new Bordered(_texture, 32f, 80f, 16f, 16f, Margin.Four);
		mTextures.TextBox.Disabled = new Bordered(_texture, 32f, 96f, 16f, 16f, Margin.Four);
		mTextures.Tooltip = new Bordered(_texture, 64f, 96f, 16f, 16f, Margin.Four);
		mTextures.Tree.Background = new Bordered(_texture, 32f, 112f, 16f, 16f, Margin.Four);
		mTextures.Tree.Plus = new Single(_texture, 448f, 96f, 15f, 15f);
		mTextures.Tree.Minus = new Single(_texture, 464f, 96f, 15f, 15f);
		mTextures.Window.Normal = new Bordered(_texture, 0f, 24f, 16f, 16f, Margin.Four);
		mTextures.Window.ActiveTitleBar = new Bordered(_texture, 0f, 0f, 16f, 24f, Margin.Four);
		mTextures.Window.Inactive = new Bordered(_texture, 16f, 24f, 16f, 16f, Margin.Four);
		mTextures.Window.InactiveTitleBar = new Bordered(_texture, 16f, 0f, 16f, 24f, Margin.Four);
		mTextures.Window.CloseButton = new SkinTextures._Input._Button
		{
			Normal = new FivePatch(_texture, 60, 0, 24, 24, Margin.Two, Margin.Two),
			Disabled = new FivePatch(_texture, 60, 48, 24, 24, Margin.Two, Margin.Two),
			Hovered = new FivePatch(_texture, 60, 72, 24, 24, Margin.Two, Margin.Two),
			Active = new FivePatch(_texture, 60, 24, 24, 24, Margin.Two, Margin.Two)
		};
	}

	public override void DrawCheckBox(Intersect.Client.Framework.Gwen.Control.Base control, bool selected, bool hovered, bool depressed)
	{
		if (!(control is Checkbox checkbox))
		{
			return;
		}
		if (TryGetOverrideTexture(checkbox, selected, depressed, out IGameTexture overrideTexture))
		{
			base.Renderer.DrawColor = checkbox.RenderColor;
			base.Renderer.DrawTexturedRect(overrideTexture, checkbox.RenderBounds, checkbox.RenderColor);
			return;
		}
		SkinTextures._FillableButton fillableButton = mTextures.CheckBox.Default;
		if (checkbox.IsDisabled)
		{
			fillableButton = mTextures.CheckBox.Disabled;
		}
		else if (depressed)
		{
			fillableButton = mTextures.CheckBox.Active;
		}
		else if (hovered)
		{
			fillableButton = mTextures.CheckBox.Hovered;
		}
		fillableButton.Box.Draw(base.Renderer, checkbox.RenderBounds, checkbox.RenderColor);
		if (checkbox.IsChecked)
		{
			fillableButton.Fill.Draw(base.Renderer, checkbox.RenderBounds, checkbox.RenderColor);
		}
	}

	public override void DrawRadioButton(Intersect.Client.Framework.Gwen.Control.Base control, bool selected, bool hovered, bool depressed)
	{
		if (!(control is RadioButton radioButton))
		{
			return;
		}
		if (TryGetOverrideTexture(radioButton, selected, depressed, out IGameTexture overrideTexture))
		{
			base.Renderer.DrawColor = radioButton.RenderColor;
			base.Renderer.DrawTexturedRect(overrideTexture, radioButton.RenderBounds, radioButton.RenderColor);
			return;
		}
		SkinTextures._FillableButton fillableButton = mTextures.RadioButton.Default;
		if (radioButton.IsDisabled)
		{
			fillableButton = mTextures.RadioButton.Disabled;
		}
		else if (depressed)
		{
			fillableButton = mTextures.RadioButton.Active;
		}
		else if (hovered)
		{
			fillableButton = mTextures.RadioButton.Hovered;
		}
		fillableButton.Box.Draw(base.Renderer, radioButton.RenderBounds, radioButton.RenderColor);
		if (radioButton.IsChecked)
		{
			fillableButton.Fill.Draw(base.Renderer, radioButton.RenderBounds, radioButton.RenderColor);
		}
	}

	public override void DrawWindow(Intersect.Client.Framework.Gwen.Control.Base control, int topHeight, bool inFocus)
	{
		if (!(control is WindowControl windowControl))
		{
			return;
		}
		IGameTexture gameTexture = null;
		if (windowControl.TryGetTexture(WindowControl.ControlState.Active, out IGameTexture texture))
		{
			gameTexture = texture;
		}
		if (windowControl.TryGetTexture(WindowControl.ControlState.Inactive, out IGameTexture texture2))
		{
			gameTexture = texture2;
		}
		if (gameTexture != null)
		{
			base.Renderer.DrawColor = control.RenderColor;
			base.Renderer.DrawTexturedRect(gameTexture, control.RenderBounds, control.RenderColor);
			return;
		}
		Bordered bordered;
		Bordered bordered2;
		if (inFocus)
		{
			bordered = mTextures.Window.ActiveTitleBar;
			bordered2 = mTextures.Window.Normal;
		}
		else
		{
			bordered = mTextures.Window.InactiveTitleBar;
			bordered2 = mTextures.Window.Inactive;
		}
		Rectangle r = windowControl.Bounds;
		bool flag = bordered != default(Bordered) && windowControl.Titlebar.ShouldDrawBackground;
		if (flag)
		{
			int bottom = windowControl.Titlebar.Bottom;
			r = new Rectangle(0, bottom, control.RenderBounds.Width, control.RenderBounds.Height - bottom);
		}
		if (bordered2 != default(Bordered) && windowControl.ShouldDrawBackground)
		{
			bordered2.Draw(base.Renderer, r, windowControl.RenderColor);
		}
		if (flag)
		{
			bordered.Draw(base.Renderer, windowControl.Titlebar.Bounds, windowControl.RenderColor);
		}
	}
}
