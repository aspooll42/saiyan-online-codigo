using Intersect.Enums;
using MessagePack;

namespace Intersect.Admin.Actions;

[MessagePackObject(false)]
public class MuteAction : AdminAction
{
	[Key(1)]
	public override Intersect.Enums.AdminAction Action { get; } = Intersect.Enums.AdminAction.Mute;

	[Key(2)]
	public string Name { get; set; }

	[Key(3)]
	public int DurationDays { get; set; }

	[Key(4)]
	public string Reason { get; set; }

	[Key(5)]
	public bool BanIp { get; set; }

	public MuteAction()
	{
	}

	public MuteAction(string name, int durationDays, string reason, bool banIp)
	{
		Name = name;
		DurationDays = durationDays;
		Reason = reason;
		BanIp = banIp;
	}
}
