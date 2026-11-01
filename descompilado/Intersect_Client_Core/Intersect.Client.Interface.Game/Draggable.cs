using System;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.Framework.Gwen.DragDrop;
using Intersect.Client.Framework.Gwen.Input;
using Intersect.Client.Framework.Gwen.Renderer;
using Intersect.Client.Framework.Gwen.Skin;
using Intersect.Client.Interface.Game.Techniques;

namespace Intersect.Client.Interface.Game;

public class Draggable : ImagePanel
{
	public bool IsDragging => DragAndDrop.CurrentPackage?.DrawControl == this;

	public Rectangle? ImageRect { get; set; }

	public Draggable(Intersect.Client.Framework.Gwen.Control.Base parent, string name)
		: base(parent, name)
	{
	}

	protected override void Render(Intersect.Client.Framework.Gwen.Skin.Base skin)
	{
		if (IsDragging)
		{
			IGameTexture texture = base.Texture;
			if (texture != null)
			{
				Rectangle rectangle = ImageRect ?? new Rectangle(0, 0, base.Width, base.Height);
				int num = (int)MathF.Round((float)rectangle.Width * 1.2f);
				int num2 = rectangle.X + rectangle.Width / 2;
				int num3 = rectangle.Y + rectangle.Height / 2;
				Rectangle targetBounds = new Rectangle(num2 - num / 2, num3 - num / 2, num, num);
				int num4 = Math.Max(4, num / 6);
				Rectangle rect = new Rectangle(targetBounds.X - num4, targetBounds.Y - num4, targetBounds.Width + num4 * 2, targetBounds.Height + num4 * 2);
				Intersect.Client.Framework.Gwen.Renderer.Base renderer = skin.Renderer;
				TechniqueArt.Glow(renderer, num2 + 3, num3 + 6, (float)rect.Width * 0.6f, TechniqueArt.Alpha(TechniqueArt.Black, 0.5f), additive: false);
				TechniqueArt.Glow(renderer, num2, num3, (float)rect.Width * 0.85f, TechniqueArt.Alpha(TechniqueArt.GoldPale, 0.35f));
				TechniqueArt.Fill(renderer, rect, new Color(235, 10, 14, 24));
				TechniqueArt.Outline(renderer, rect, TechniqueArt.Alpha(TechniqueArt.GoldPale, 0.8f));
				renderer.DrawColor = base.RenderColor;
				renderer.DrawTexturedRect(texture, targetBounds, base.RenderColor);
				return;
			}
		}
		Rectangle? imageRect = ImageRect;
		if (imageRect.HasValue)
		{
			Rectangle valueOrDefault = imageRect.GetValueOrDefault();
			IGameTexture texture2 = base.Texture;
			if (texture2 != null)
			{
				skin.Renderer.DrawColor = base.RenderColor;
				skin.Renderer.DrawTexturedRect(texture2, valueOrDefault, base.RenderColor);
				return;
			}
		}
		base.Render(skin);
	}

	public override bool DragAndDrop_Draggable()
	{
		return true;
	}

	public override bool DragAndDrop_CanAcceptPackage(Package package)
	{
		return true;
	}

	public override Package? DragAndDrop_GetPackage(int x, int y)
	{
		return new Package
		{
			IsDraggable = true,
			DrawControl = this,
			Name = base.Name,
			HoldOffset = ToLocal(InputHandler.MousePosition.X, InputHandler.MousePosition.Y)
		};
	}

	public override void DragAndDrop_StartDragging(Package package, int x, int y)
	{
		base.IsVisibleInParent = false;
	}

	public override void DragAndDrop_EndDragging(bool success, int x, int y)
	{
		base.IsVisibleInParent = true;
	}
}
