namespace Intersect.Framework.Core.GameObjects.Dungeons;

public enum DungeonResult : byte
{
	Entered,
	NotLeader,
	TooFar,
	AlreadyInRun,
	InvalidDungeon,
	DoorLocked,
	RoomCleared,
	BeanUsed,
	Failed,
	Left,
	Rejoined,
	LeftBehind,
	DifficultyLocked
}
