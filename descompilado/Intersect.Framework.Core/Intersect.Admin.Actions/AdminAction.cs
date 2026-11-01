using Intersect.Enums;
using MessagePack;

namespace Intersect.Admin.Actions;

[MessagePackObject(false)]
[Union(0, typeof(BanAction))]
[Union(1, typeof(KickAction))]
[Union(2, typeof(KillAction))]
[Union(3, typeof(MuteAction))]
[Union(4, typeof(SetAccessAction))]
[Union(5, typeof(SetFaceAction))]
[Union(6, typeof(SetSpriteAction))]
[Union(7, typeof(UnbanAction))]
[Union(8, typeof(UnmuteAction))]
[Union(9, typeof(WarpMeToAction))]
[Union(10, typeof(WarpToLocationAction))]
[Union(11, typeof(WarpToMapAction))]
[Union(12, typeof(WarpToMeAction))]
[Union(13, typeof(ReturnToOverworldAction))]
[Union(14, typeof(GiveItemAction))]
[Union(15, typeof(SetZeniAction))]
[Union(16, typeof(SetKarmaAction))]
[Union(17, typeof(GiveTitleAction))]
[Union(18, typeof(GiveOrnamentAction))]
[Union(19, typeof(SetClassAction))]
[Union(20, typeof(SetLevelAction))]
[Union(21, typeof(GiveSpellAction))]
public abstract class AdminAction
{
	[Key(0)]
	public abstract Intersect.Enums.AdminAction Action { get; }
}
