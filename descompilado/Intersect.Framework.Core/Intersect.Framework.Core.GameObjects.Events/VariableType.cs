using Intersect.Attributes;
using Intersect.Enums;

namespace Intersect.Framework.Core.GameObjects.Events;

public enum VariableType
{
	[RelatedTable(GameObjectType.PlayerVariable)]
	PlayerVariable,
	[RelatedTable(GameObjectType.ServerVariable)]
	ServerVariable,
	[RelatedTable(GameObjectType.GuildVariable)]
	GuildVariable,
	[RelatedTable(GameObjectType.UserVariable)]
	UserVariable
}
