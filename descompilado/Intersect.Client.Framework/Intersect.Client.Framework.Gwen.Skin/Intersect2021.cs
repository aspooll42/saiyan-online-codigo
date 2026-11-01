using Intersect.Client.Framework.File_Management;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.Framework.Gwen.Renderer;
using Intersect.Client.Framework.Gwen.Skin.Texturing;

namespace Intersect.Client.Framework.Gwen.Skin;

public class Intersect2021 : TexturedBase
{
	public Intersect2021(Intersect.Client.Framework.Gwen.Renderer.Base renderer, GameContentManager contentManager)
		: base(renderer, contentManager, "intersect-2021.png")
	{
	}

	public override void Dispose()
	{
		base.Dispose();
	}

	protected override void InitializeTextures()
	{
		base.InitializeTextures();
		mTextures.Window.Normal = new Bordered(_texture, 0f, 24f, 16f, 16f, new Margin(4, 4, 4, 4));
		mTextures.Window.ActiveTitleBar = new Bordered(_texture, 0f, 0f, 16f, 24f, new Margin(4, 4, 4, 4));
		mTextures.Window.Inactive = new Bordered(_texture, 16f, 24f, 16f, 16f, new Margin(4, 4, 4, 4));
		mTextures.Window.InactiveTitleBar = new Bordered(_texture, 16f, 0f, 16f, 24f, new Margin(4, 4, 4, 4));
		mTextures.Window.CloseButton = new SkinTextures._Input._Button
		{
			Normal = new FivePatch(_texture, 60, 0, 24, 24, Margin.Two, Margin.One),
			Disabled = new FivePatch(_texture, 60, 48, 24, 24, Margin.Two, Margin.One),
			Hovered = new FivePatch(_texture, 60, 72, 24, 24, Margin.Two, Margin.One),
			Active = new FivePatch(_texture, 60, 24, 24, 24, Margin.Two, Margin.One)
		};
		mTextures.Panel.Control = new Bordered(_texture, 32f, 0f, 16f, 16f, Margin.Four);
		mTextures.Panel.Normal = new Bordered(_texture, 32f, 0f, 16f, 16f, Margin.Four);
		mTextures.Panel.Highlight = new Bordered(_texture, 32f, 16f, 16f, 16f, Margin.Four);
		mTextures.Panel.Bright = new Bordered(_texture, 32f, 32f, 16f, 16f, Margin.Four);
		mTextures.Panel.Dark = new Bordered(_texture, 32f, 48f, 16f, 16f, Margin.Four);
		mTextures.Input.Button.Normal = new Bordered(_texture, 48f, 0f, 12f, 12f, Margin.Four);
		mTextures.Input.Button.Disabled = new Bordered(_texture, 48f, 24f, 12f, 12f, Margin.Four);
		mTextures.Input.Button.Hovered = new Bordered(_texture, 48f, 12f, 12f, 12f, Margin.Four);
		mTextures.Input.Button.Active = new Bordered(_texture, 48f, 36f, 12f, 12f, Margin.Four);
		mTextures.Input.ComboBox.Normal = new Bordered(_texture, 385f, 336f, 126f, 31f, new Margin(8, 8, 32, 8));
		mTextures.Input.ComboBox.Hover = new Bordered(_texture, 385f, 368f, 126f, 31f, new Margin(8, 8, 32, 8));
		mTextures.Input.ComboBox.Down = new Bordered(_texture, 385f, 400f, 126f, 31f, new Margin(8, 8, 32, 8));
		mTextures.Input.ComboBox.Disabled = new Bordered(_texture, 385f, 432f, 126f, 31f, new Margin(8, 8, 32, 8));
		mTextures.CheckBox.Default.Box = new Single(_texture, 84f, 0f, 22f, 22f);
		mTextures.CheckBox.Default.Fill = new Single(_texture, 106f, 0f, 22f, 22f);
		mTextures.CheckBox.Active.Box = new Single(_texture, 84f, 22f, 22f, 22f);
		mTextures.CheckBox.Active.Fill = new Single(_texture, 106f, 22f, 22f, 22f);
		mTextures.CheckBox.Hovered.Box = new Single(_texture, 84f, 44f, 22f, 22f);
		mTextures.CheckBox.Hovered.Fill = new Single(_texture, 106f, 44f, 22f, 22f);
		mTextures.CheckBox.Disabled.Box = new Single(_texture, 84f, 66f, 22f, 22f);
		mTextures.CheckBox.Disabled.Fill = new Single(_texture, 106f, 66f, 22f, 22f);
		mTextures.RadioButton.Default.Box = new Single(_texture, 128f, 0f, 22f, 22f);
		mTextures.RadioButton.Default.Fill = new Single(_texture, 150f, 0f, 22f, 22f);
		mTextures.RadioButton.Active.Box = new Single(_texture, 128f, 22f, 22f, 22f);
		mTextures.RadioButton.Active.Fill = new Single(_texture, 150f, 22f, 22f, 22f);
		mTextures.RadioButton.Hovered.Box = new Single(_texture, 128f, 44f, 22f, 22f);
		mTextures.RadioButton.Hovered.Fill = new Single(_texture, 150f, 44f, 22f, 22f);
		mTextures.RadioButton.Disabled.Box = new Single(_texture, 128f, 66f, 22f, 22f);
		mTextures.RadioButton.Disabled.Fill = new Single(_texture, 150f, 66f, 22f, 22f);
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
		Rectangle r = windowControl.RenderBounds;
		bool flag = bordered != default(Bordered) && windowControl.Titlebar.ShouldDrawBackground;
		if (flag)
		{
			r = new Rectangle(0, windowControl.Titlebar.Bottom, control.RenderBounds.Width, control.RenderBounds.Height);
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
