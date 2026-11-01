using Intersect.Client.Core;
using Intersect.Client.Dungeons;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.Framework.Gwen.Skin;
using Intersect.Client.Framework.Input;
using Intersect.Client.Interface.Game.Housing;
using Intersect.Client.Interface.Game.Techniques;
using Intersect.Client.Interface.Shared;
using Intersect.Client.Localization;
using Intersect.Client.Networking;

namespace Intersect.Client.Interface.Game.Dungeons;

internal sealed class DungeonLeaveButton : Intersect.Client.Framework.Gwen.Control.Base
{
	private bool _finished;

	protected override bool ShouldClip => false;

	public DungeonLeaveButton(Canvas canvas)
		: base(canvas, "DungeonLeaveButton")
	{
		base.MouseInputEnabled = true;
		base.KeyboardInputEnabled = false;
		base.IsHidden = true;
	}

	public void Place(Rectangle rect, bool finished)
	{
		_finished = finished;
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
				Rectangle rect = new Rectangle(0, 0, base.Width, base.Height);
				string label = (_finished ? Strings.Dungeons.EndExit.ToString() : Strings.Dungeons.LeaveHint.ToString());
				bool isHovered = IsHovered;
				HouseArt.Hollow(skin.Renderer, d, rect, WrapLabel(label), isHovered, 1f, _finished ? TechniqueArt.Gold : TechniqueArt.Red);
			}
		}
	}

	private static string WrapLabel(string label)
	{
		int num = label.IndexOf(' ');
		if (num <= 0)
		{
			return label;
		}
		return label.Substring(0, num);
	}

	protected override void OnMouseClicked(MouseButton mouseButton, Point mousePosition, bool userAction = true)
	{
		base.OnMouseClicked(mouseButton, mousePosition, userAction);
		if (mouseButton == MouseButton.Left)
		{
			Audio.AddGameSound("menu_click.wav", loop: false);
			if (_finished)
			{
				PacketSender.SendDungeonLeave();
				return;
			}
			ConfirmCardWindow.Open(new ConfirmCardRequest
			{
				Title = Strings.Dungeons.LeaveTitle,
				Heading = (Dungeon.Current?.Name ?? string.Empty),
				Prompt = Strings.Dungeons.LeavePrompt,
				Detail = Strings.Dungeons.LeaveDetail,
				ConfirmLabel = Strings.Dungeons.LeaveConfirm,
				CancelLabel = Strings.Dungeons.LeaveCancel,
				Danger = true,
				ShowMe = true
			}, PacketSender.SendDungeonLeave);
		}
	}
}
