namespace Intersect.Framework.Core.GameObjects.Housing;

public enum FurnitureVerdict : byte
{
	Ok,
	OutsideRoom,
	WallOnly,
	FloorOnly,
	AgainstWallOnly,
	KeepEntranceFree,
	Occupied
}
