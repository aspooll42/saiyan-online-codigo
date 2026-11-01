using Intersect.Client.Core;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.Framework.Gwen.Skin;
using Intersect.Client.Framework.Input;
using Intersect.Client.Interface.Game.Housing;
using Intersect.Client.Interface.Game.Techniques;
using Intersect.Client.Interface.Shared;
using Intersect.Client.Localization;
using Intersect.Client.Networking;

namespace Intersect.Client.Interface.Game.Enseignements;

internal sealed class TrialLeaveButton : Intersect.Client.Framework.Gwen.Control.Base
{
	protected override bool ShouldClip => false;

	public TrialLeaveButton(Canvas canvas)
		: base(canvas, "TrialLeaveButton")
	{
		base.MouseInputEnabled = true;
		base.KeyboardInputEnabled = false;
		base.IsHidden = true;
	}

	public void Place(Rectangle rect)
	{
		if (rect.X != base.X || rect.Y != base.Y || rect.Width != base.Width || rect.Height != base.Height)
		{
			SetBounds(rect.X, rect.Y, rect.Width, rect.Height);
		}
	}

	protected override void Render(Intersect.Client.Framework.Gwen.Skin.Base skin)
	{
		base.Render(skin);
		if (!base.IsHidden)
		{
			Intersect.Client.Framework.Gwen.Control.Base parent = base.Parent;
			if (parent != null)
			{
				CharteScreen.Design d = CharteScreen.Design.For(parent.Width, parent.Height);
				HouseArt.Hollow(skin.Renderer, d, new Rectangle(0, 0, base.Width, base.Height), Strings.TrialHud.Leave.ToString(), IsHovered, 1f, TechniqueArt.Red);
			}
		}
	}

	protected override void OnMouseClicked(MouseButton mouseButton, Point mousePosition, bool userAction = true)
	{
		base.OnMouseClicked(mouseButton, mousePosition, userAction);
		if (mouseButton == MouseButton.Left)
		{
			Audio.AddGameSound("menu_click.wav", loop: false);
			ConfirmCardWindow.Open(new ConfirmCardRequest
			{
				Title = Strings.TrialHud.Leave.ToString(),
				Heading = (MasterTrial.Current?.Name ?? string.Empty),
				Prompt = Strings.TrialHud.LeavePrompt.ToString(),
				Detail = Strings.TrialHud.LeaveDetail.ToString(),
				ConfirmLabel = Strings.TrialHud.Leave.ToString(),
				CancelLabel = Strings.TrialHud.Continue.ToString(),
				Danger = true,
				ShowMe = true
			}, PacketSender.SendTeachingTrialLeave);
		}
	}
}
