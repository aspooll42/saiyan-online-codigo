using Intersect.Client.Core;
using Intersect.Client.Framework.File_Management;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.Framework.Gwen;
using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.Framework.Gwen.Skin;
using Intersect.Client.Interface.Menu;
using Intersect.Core;
using Intersect.Framework.Core.Security;

namespace Intersect.Client.Interface.Shared;

public class VersionPanel : Panel
{
	private readonly VersionLabel _engineVersionLabel;

	public bool IsVersionVisible => _engineVersionLabel.IsVisibleInParent;

	public VersionPanel(Intersect.Client.Framework.Gwen.Control.Base parent, string name = "VersionPanel")
		: base(parent, name)
	{
		ClientContext currentContext = ApplicationContext.GetCurrentContext<ClientContext>();
		base.Alignment = new Alignments[2]
		{
			Alignments.Bottom,
			Alignments.Right
		};
		base.BackgroundColor = new Color(160, 11, 14, 23);
		base.DockChildSpacing = new Padding(0, 4);
		base.RestrictToParent = true;
		IFont font = GameContentManager.Current.GetFont("chakrapetch");
		_engineVersionLabel = new VersionLabel(this, "_engineVersionLabel")
		{
			Font = font,
			FontSize = 10,
			Padding = new Padding(8, 4),
			Text = MenuContent.Current.Version,
			TextColor = new Color(255, 196, 204, 220),
			IsVisibleInParent = currentContext.IsDeveloper
		};
		currentContext.PermissionsChanged += ApplicationContextOnPermissionsChanged;
	}

	private void ApplicationContextOnPermissionsChanged(PermissionSet permissionSet)
	{
		_engineVersionLabel.IsVisibleInParent = permissionSet.IsGranted(Permission.EngineVersion) || ApplicationContext.CurrentContext.IsDeveloper;
	}

	protected override void Layout(Intersect.Client.Framework.Gwen.Skin.Base skin)
	{
		base.Layout(skin);
		SizeToChildren();
	}

	protected override void Dispose(bool disposing)
	{
		ClientContext currentContext = ApplicationContext.GetCurrentContext<ClientContext>();
		currentContext.PermissionsChanged -= ApplicationContextOnPermissionsChanged;
		base.Dispose(disposing);
	}
}
